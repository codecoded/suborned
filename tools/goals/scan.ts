import { readFile } from 'node:fs/promises';
import { hash } from '../rag/corpus';
import { fileHashes, localPath, sourceFiles } from './files';
import type { Analysis, CheckResult, Scope } from './schema';

export type Source = { path: string; from: number; to: number; sha256: string; text: string };
export type Scan = { inventory: Record<string, string>; sources: Source[]; checks: CheckResult[] };

/** The scanner opens source on every pass. A stale model assertion or filename cannot mark a feature complete. */
export async function scan(root: string, scope: Scope, checks: CheckResult[]): Promise<Scan> {
  const sources: Source[] = [];
  for (const c of scope.context) {
    const text = await readFile(await localPath(root, c.path), 'utf8'), lines = text.split('\n');
    const from = c.from ?? 1, to = Math.min(c.to ?? lines.length, lines.length);
    if (from > lines.length) throw new Error(`Context starts beyond ${c.path}`);
    sources.push({ path: c.path, from, to, sha256: hash(text), text: lines.slice(from - 1, to).join('\n') });
  }
  // Newly created implementation files are shown in full on the next pass.
  const paths = await sourceFiles(root, ['sim']);
  for (const path of scope.writable.filter(p => paths.includes(p) && !scope.context.some(c => c.path === p))) {
    const text = await readFile(await localPath(root, path), 'utf8');
    sources.push({ path, from: 1, to: text.split('\n').length, sha256: hash(text), text });
  }
  for (const s of scope.sources) {
    const text = await readFile(await localPath(root, s.path), 'utf8');
    const at = text.indexOf(s.quote);
    if (at < 0) throw new Error(`Pinned requirement no longer appears in ${s.path}`);
    const from = text.slice(0, at).split('\n').length;
    sources.push({ path: s.path, from, to: from + s.quote.split('\n').length - 1, sha256: hash(text), text: s.quote });
  }
  return { inventory: await fileHashes(root, paths), sources, checks };
}

export type Ranked = {
  id: string; state: 'verified' | 'missing' | 'partial' | 'blocked' | 'unknown'; score: number;
  value: number; unblocks: number; effort: number; risk: number; reason: string; eligible: boolean;
};

export function rank(scope: Scope, scan: Scan, analysis: Analysis): Ranked[] {
  const ids = analysis.features.map(f => f.id);
  if (ids.length !== scope.requirements.length || new Set(ids).size !== ids.length || scope.requirements.some(r => !ids.includes(r.id)))
    throw new Error('Analysis must cover every scoped requirement exactly once');
  const rows: Ranked[] = scope.requirements.map(r => {
    const f = analysis.features.find(f => f.id === r.id)!;
    for (const e of f.evidence) if (!scan.sources.some(s => s.path === e.path && s.text.includes(e.quote))) throw new Error(`Unseen evidence for ${r.id}: ${e.path}`);
    const passed = r.checks.every(id => scan.checks.some(c => c.id === id && c.ok));
    // Passing tests plus a source-grounded assessment, never model confidence alone.
    const state = f.state === 'satisfied' ? passed ? 'verified' : 'unknown' : f.state;
    const unblocks = scope.requirements.filter(other => other.dependsOn.includes(r.id)).length;
    return { id: r.id, state, score: 4 * f.value + 2 * unblocks - f.effort - f.risk,
      value: f.value, unblocks, effort: f.effort, risk: f.risk, reason: f.reason, eligible: false };
  });
  for (const row of rows) {
    const req = scope.requirements.find(r => r.id === row.id)!;
    row.eligible = ['missing', 'partial'].includes(row.state) && req.dependsOn.every(d => rows.find(r => r.id === d)?.state === 'verified');
  }
  return rows.sort((a, b) => b.score - a.score || a.id.localeCompare(b.id));
}

export function scanPrompt(scope: Scope, scanned: Scan): string {
  return `Declared scope (requirements and acceptance checks were fixed before this run):\n${JSON.stringify(scope)}\n\n`
    + `File inventory (sha256; inventory is not proof of implementation):\n${JSON.stringify(scanned.inventory)}\n\n`
    + scanned.sources.map(s => `${s.path}:${s.from}-${s.to} (full-file sha256 ${s.sha256})\n${s.text}`).join('\n\n')
    + `\n\nExecuted behavioural checks:\n${JSON.stringify(scanned.checks)}\n`;
}
