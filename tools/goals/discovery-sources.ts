import { readFile, readdir, realpath } from 'node:fs/promises';
import { hash } from '../rag/corpus';
import { localPath, sourceFiles } from './files';
import { compareVersions, selectLatestGdd } from '../rag/gdd-sources';
export { compareVersions } from '../rag/gdd-sources';

export type GddSource = { path: string; version: string; sha256: string };
export type GddSection = { id: string; title: string; path: string; from: number; to: number; sha256: string; text: string };
export type GddSnapshot = {
  agent: GddSource & { text: string };
  human: GddSource;
  sections: GddSection[];
};
export type WorktreeComparison = { root: string; version: string; agentHash: string; humanHash: string; status: 'same' | 'older' };

export async function resolveLatestGdd(root: string): Promise<GddSnapshot> {
  const names = await readdir(await localPath(root, 'GDD'));
  const chosen = selectLatestGdd(names);
  const latest = async (kind: 'agent' | 'human'): Promise<GddSource & { text: string }> => {
    const top = chosen[kind];
    const text = await readFile(await localPath(root, top.path), 'utf8');
    return { ...top, text, sha256: hash(text) };
  };
  const [agent, humanText] = await Promise.all([latest('agent'), latest('human')]);
  const lines = agent.text.split('\n');
  const starts = lines.flatMap((line, i) => { const m = /^##\s+§(\d+)\s+(.+?)\s*$/.exec(line); return m ? [{ id: m[1]!.padStart(2, '0'), title: m[2]!, start: i }] : []; });
  if (!starts.length || new Set(starts.map(s => s.id)).size !== starts.length) throw new Error('Agent GDD must have unique numbered ## § sections');
  const sections = starts.map((s, i) => {
    const end = starts[i + 1]?.start ?? lines.length;
    const text = lines.slice(s.start, end).join('\n');
    return { id: s.id, title: s.title, path: agent.path, from: s.start + 1, to: end, sha256: hash(text), text };
  });
  const { text: _humanText, ...human } = humanText;
  return { agent, human, sections };
}

export function mainWorktreeRoots(porcelain: string): string[] {
  return porcelain.split(/\n\n+/).flatMap(block => {
    const lines = block.split('\n');
    if (!lines.includes('branch refs/heads/main')) return [];
    const path = lines.find(line => line.startsWith('worktree '))?.slice('worktree '.length);
    return path ? [path] : [];
  });
}

/** Compare only locally registered main checkouts; this intentionally makes no claim about remote freshness. */
export async function assertLatestGdd(root: string, mainRoots?: string[]): Promise<GddSnapshot & { comparisons: WorktreeComparison[] }> {
  const snapshot = await resolveLatestGdd(root);
  if (!mainRoots) {
    const process = Bun.spawn(['git', 'worktree', 'list', '--porcelain'], { cwd: root, stdout: 'pipe', stderr: 'pipe' });
    const [code, output, error] = await Promise.all([process.exited, new Response(process.stdout).text(), new Response(process.stderr).text()]);
    if (code !== 0) throw new Error(`Cannot establish local main GDD freshness: ${error.trim()}`);
    mainRoots = mainWorktreeRoots(output);
  }
  const current = await realpath(root), comparisons: WorktreeComparison[] = [];
  for (const mainRoot of mainRoots) {
    const otherRoot = await realpath(mainRoot);
    if (otherRoot === current) continue;
    const other = await resolveLatestGdd(otherRoot);
    const order = compareVersions(other.agent.version, snapshot.agent.version);
    const drift = other.agent.sha256 !== snapshot.agent.sha256 || other.human.sha256 !== snapshot.human.sha256;
    if (order > 0 || (order === 0 && drift)) {
      throw new Error(`Stale worktree GDD: ${root} has v${snapshot.agent.version}, but main at ${otherRoot} has ${order > 0 ? `newer v${other.agent.version}` : `different content at the same v${other.agent.version}`}. Synchronize the GDD before discovery; no checkout was modified.`);
    }
    comparisons.push({ root: otherRoot, version: other.agent.version, agentHash: other.agent.sha256, humanHash: other.human.sha256, status: order === 0 ? 'same' : 'older' });
  }
  return { ...snapshot, comparisons };
}

export type CodeIndexEntry = { path: string; sha256: string; lines: number; symbols: string[]; summary: string };
export type CodeSource = { path: string; sha256: string; from: number; to: number; text: string };
export type CodeMatch = { path: string; sha256: string; line: number; text: string; terms: string[] };

/** This is a navigation index, not a parser or evidence that a behavior is absent. */
export async function buildCodeIndex(root: string): Promise<CodeIndexEntry[]> {
  const sim = (await sourceFiles(root, ['sim'])).filter(path => path.endsWith('.cs'));
  // The headless host's readers/contracts are inspection context, never writable.
  const consumers: string[] = [];
  const walkConsumers = async (directory: string) => {
    const entries = await readdir(await localPath(root, directory), { withFileTypes: true }).catch(error => { if (error.code === 'ENOENT') return []; throw error; });
    for (const entry of entries) {
      if (['node_modules', 'bin', 'obj', '.git'].includes(entry.name)) continue;
      const path = `${directory}/${entry.name}`;
      if (entry.isSymbolicLink()) throw new Error(`Source symlink refused: ${path}`);
      if (entry.isDirectory()) await walkConsumers(path);
      else if (/\.(ts|json)$/.test(entry.name)) consumers.push(path);
    }
  };
  await walkConsumers('tools/sim');
  const rulesPath = await localPath(root, 'contracts/rules');
  const rules = await readdir(rulesPath).catch(error => { if (error.code === 'ENOENT') return []; throw error; });
  const paths = [...sim, ...consumers, ...rules.filter(name => name.endsWith('.json')).map(name => `contracts/rules/${name}`)].sort();
  return Promise.all(paths.map(async path => {
    const text = await readFile(await localPath(root, path), 'utf8'), lines = text.split('\n');
    const declarations = lines.filter(line => /^\s*(?:(?:public|private|protected|internal|static|partial|sealed|abstract|readonly|virtual|override|async)\s+)+(?:[^=;]+\([^;]*|(?:class|struct|interface|enum|record)\s+\w+)/.test(line));
    const symbols = [...new Set(declarations.flatMap(line => {
      const type = /\b(?:class|struct|interface|enum|record)\s+(\w+)/.exec(line);
      const method = /\b(\w+)\s*(?:<[^>]*>)?\s*\(/.exec(line);
      return type ? [type[1]!] : method ? [method[1]!] : [];
    }))].slice(0, 100);
    const comments = lines.filter(line => /^\s*\/\//.test(line)).slice(0, 4);
    if (path.endsWith('.json')) {
      const keys = Object.keys(JSON.parse(text));
      return { path, sha256: hash(text), lines: lines.length, symbols: keys.slice(0, 100), summary: `Rule contract JSON; top-level keys: ${keys.join(', ')}`.slice(0, 1200) };
    }
    return { path, sha256: hash(text), lines: lines.length, symbols, summary: [...comments, ...declarations].map(s => s.trim()).join('\n').slice(0, 1200) };
  }));
}

/** Source hashes and full summaries stay in the archived index; models only need navigation. */
export const navigationIndex = (index: CodeIndexEntry[]) => index.map(({ path, lines, symbols }) => ({ path, lines, symbols }));

async function indexedSource(root: string, entry: CodeIndexEntry): Promise<CodeSource> {
  const text = await readFile(await localPath(root, entry.path), 'utf8');
  if (hash(text) !== entry.sha256) throw new Error(`Code changed since discovery index: ${entry.path}; rebuild the index`);
  return { path: entry.path, sha256: entry.sha256, from: 1, to: text.split('\n').length, text };
}

export async function readCodeFiles(root: string, index: CodeIndexEntry[], paths: string[], maxChars = 80_000): Promise<CodeSource[]> {
  if (!Number.isSafeInteger(maxChars) || maxChars < 1 || maxChars > 200_000) throw new Error('Invalid code context character limit');
  if (paths.length !== new Set(paths).size) throw new Error('Duplicate code read path');
  const entries = paths.map(path => {
    const entry = index.find(e => e.path === path);
    if (!entry) throw new Error(`Code path not in discovery index: ${path}`);
    return entry;
  });
  const sources = await Promise.all(entries.map(entry => indexedSource(root, entry)));
  if (sources.reduce((total, source) => total + source.text.length, 0) > maxChars) throw new Error(`Code reads exceed ${maxChars} characters; request fewer files`);
  return sources;
}

export async function readCodeRanges(root: string, index: CodeIndexEntry[], requests: { path: string; from: number; to: number }[], maxChars = 120_000): Promise<CodeSource[]> {
  if (!Number.isSafeInteger(maxChars) || maxChars < 1 || maxChars > 200_000) throw new Error('Invalid code context character limit');
  if (requests.length > 10) throw new Error('At most 10 code ranges may be read at once');
  for (const request of requests) {
    if (!Number.isSafeInteger(request.from) || !Number.isSafeInteger(request.to) || request.from < 1 || request.to < request.from || request.to - request.from + 1 > 500) {
      throw new Error(`Invalid code line range for ${request.path}: use positive ordered lines, at most 500 per request`);
    }
  }
  const sources = await Promise.all(requests.map(async request => {
    const entry = index.find(item => item.path === request.path);
    if (!entry) throw new Error(`Code path not in discovery index: ${request.path}`);
    const source = await indexedSource(root, entry), lines = source.text.split('\n');
    if (request.from > lines.length || request.to > lines.length) throw new Error(`Code line range exceeds ${lines.length} lines: ${request.path}`);
    return { ...source, from: request.from, to: request.to, text: lines.slice(request.from - 1, request.to).join('\n') };
  }));
  if (sources.reduce((total, source) => total + source.text.length, 0) > maxChars) throw new Error(`Code reads exceed ${maxChars} characters; request smaller ranges`);
  return sources;
}

/** Case-insensitive literal search; every result is pinned to the index snapshot. */
export async function searchCode(root: string, index: CodeIndexEntry[], terms: string[], options: { maxMatches?: number } = {}): Promise<{ matches: CodeMatch[]; truncated: boolean; searchedFiles: number }> {
  if (!terms.length || terms.length > 20 || terms.some(term => !term.trim() || term.length > 120)) throw new Error('Search requires 1–20 nonempty literal terms of at most 120 characters');
  const maxMatches = options.maxMatches ?? 100;
  if (!Number.isSafeInteger(maxMatches) || maxMatches < 1 || maxMatches > 500) throw new Error('Invalid search match limit');
  const groups: CodeMatch[][] = [];
  for (const entry of index) {
    const source = await indexedSource(root, entry);
    const group: CodeMatch[] = [];
    for (const [i, line] of source.text.split('\n').entries()) {
      const found = terms.filter(term => line.toLowerCase().includes(term.toLowerCase()));
      if (!found.length) continue;
      // Center the excerpt on the first match, keeping a useful literal source quote.
      const start = Math.max(0, line.toLowerCase().indexOf(found[0]!.toLowerCase()) - 100);
      group.push({ path: entry.path, sha256: entry.sha256, line: i + 1, text: line.slice(start, start + 500), terms: found });
    }
    if (group.length) groups.push(group);
  }
  // Round-robin across files prevents a large early file from hiding tests and consumers.
  const matches: CodeMatch[] = [];
  for (let depth = 0; matches.length < maxMatches && groups.some(g => g.length > depth); depth++)
    for (const group of groups) if (group[depth] && matches.length < maxMatches) matches.push(group[depth]!);
  const truncated = groups.reduce((n, group) => n + group.length, 0) > matches.length;
  return { matches, truncated, searchedFiles: index.length };
}
