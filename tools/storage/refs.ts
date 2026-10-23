// What the repo itself names: every out/... path in a tracked file, sorted by who names it (brief 050). A recorded run or a contract that names a path is a PIN
// (never pruned); code that names it only says where an output is written or read (the output is regenerated, the rule for its kind decides); a doc that names it
// is evidence a reader may look for (reported, not protected).
import { relative } from 'node:path';

export type Who = 'pin' | 'code' | 'doc';
export type Ref = { path: string; who: Who; by: string };

export const whoNames = (file: string): Who =>
  /^(runs|contracts)\//.test(file) || /^sim\/data\//.test(file) ? 'pin'
    : /^(tools|sim|unity|testbed|services|package\.json|tsconfig\.json)/.test(file) ? 'code' : 'doc';

function git(root: string, ...args: string[]): string {
  const r = Bun.spawnSync(['git', ...args], { cwd: root, stdout: 'pipe', stderr: 'pipe', maxBuffer: 512 * 1024 * 1024 });
  return r.stdout.toString();
}

/** Tracked files and every directory above them (repo-relative). */
export function tracked(root: string): { files: Set<string>; dirs: Set<string> } {
  const files = new Set(git(root, 'ls-files', '-z').split('\0').filter(Boolean)), dirs = new Set<string>();
  for (const f of files) { let i = f.lastIndexOf('/'); while (i > 0) { const d = f.slice(0, i); if (dirs.has(d)) break; dirs.add(d); i = d.lastIndexOf('/'); } }
  return { files, dirs };
}

const clean = (s: string) => s.replace(/[.,:;)\]'"`*]+$/, '').replace(/\/+$/, '');

/** Every `out/...` path a tracked text file mentions (generated GDD html left out). */
export function outRefs(root: string): Ref[] {
  const text = git(root, 'grep', '-I', '-o', '-E', 'out/[A-Za-z0-9_.@+-]+(/[A-Za-z0-9_.@+-]+)*', '--', '.', ':!*.html', ':!.claude', ':!.codex');
  const seen = new Set<string>(), out: Ref[] = [];
  for (const line of text.split('\n')) {
    const i = line.indexOf(':out/'); if (i < 0) continue;
    const by = line.slice(0, i), p = clean(line.slice(i + 1));
    if (p === 'out' || p.length < 5 || p.endsWith('/.') || p === 'out/.') continue;
    const k = p + '\0' + by; if (seen.has(k)) continue; seen.add(k);
    out.push({ path: p, who: whoNames(by), by });
  }
  return out;
}

/** Image and video names a tracked text file mentions: the whole `briefs/...` path, and the bare file name. */
export function imageRefs(root: string): { paths: Set<string>; names: Set<string> } {
  const text = git(root, 'grep', '-I', '-h', '-o', '-E', '[A-Za-z0-9_@+/.-]+\\.(png|jpe?g|webp|mp4|mov)', '--', '.', ':!*.html', ':!.claude', ':!.codex');
  const paths = new Set<string>(), names = new Set<string>();
  for (const m of text.split('\n')) { if (!m) continue; names.add(m.slice(m.lastIndexOf('/') + 1)); if (m.startsWith('briefs/')) paths.add(m); }
  return { paths, names };
}

/** The references that touch `rel` (a repo-relative path): a ref to it, to something inside it, or to a folder above it. */
export function touching(refs: Ref[], rel: string): Ref[] {
  return refs.filter(r => r.path === rel || r.path.startsWith(rel + '/') || rel.startsWith(r.path + '/'));
}
export const summarise = (rs: Ref[]) => {
  const by = (w: Who) => [...new Set(rs.filter(r => r.who === w).map(r => r.by))];
  return { pin: by('pin'), code: by('code'), doc: by('doc') };
};
export const rel = (root: string, p: string) => relative(root, p);

/** Engine builds (first 12 characters of the commit) that a recorded mission run replays through: `"commit": "<40 hex>"` in a tracked file under runs/. */
export function pinnedEngines(root: string): Set<string> {
  const out = new Set<string>();
  for (const m of git(root, 'grep', '-I', '-h', '-o', '-E', '"commit": *"[0-9a-f]{40}"', '--', 'runs').split('\n')) { const h = m.match(/[0-9a-f]{40}/)?.[0]; if (h) out.add(h.slice(0, 12)); }
  return out;
}
export const commitExists = (root: string, sha: string): boolean => Bun.spawnSync(['git', 'cat-file', '-e', `${sha}^{commit}`], { cwd: root, stdout: 'ignore', stderr: 'ignore' }).exitCode === 0;
