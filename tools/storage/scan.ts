// Disk scan for `bun run storage` (brief 050). Sizes are what the disk holds (allocated blocks, like `du`), not the sum of file lengths: 600,000 two-kilobyte
// files take 2.5 GB. Symlinks are never followed; a hard-linked file is counted once.
import { lstatSync, readdirSync } from 'node:fs';
import { join, relative } from 'node:path';

export type Agg = { bytes: number; files: number; newest: number; oldest: number };
export type FileRec = { path: string; bytes: number; mtime: number };
export type Node = Agg & { path: string; kids: Map<string, Node>; names: string[]; list: FileRec[] };

export const emptyAgg = (): Agg => ({ bytes: 0, files: 0, newest: 0, oldest: Infinity });
const add = (a: Agg, bytes: number, mtime: number) => { a.bytes += bytes; a.files++; if (mtime > a.newest) a.newest = mtime; if (mtime < a.oldest) a.oldest = mtime; };
const fold = (a: Agg, b: Agg) => { a.bytes += b.bytes; a.files += b.files; if (b.newest > a.newest) a.newest = b.newest; if (b.oldest < a.oldest) a.oldest = b.oldest; };

export type ScanOpts = {
  /** directories (repo-relative) whose files are kept one by one in `files` (everywhere else only the totals) */
  fileLevel?: (relDir: string) => boolean;
  /** directories not entered (repo-relative); they appear as a node with no totals */
  skip?: (relDir: string) => boolean;
  /** how many directory levels below the start get a node of their own (deeper ones are folded into their ancestor) */
  depth?: number;
  /** called once for every file counted */
  onFile?: (path: string, bytes: number, mtime: number) => void;
  /** repo root, for the relative paths */
  base: string;
};

/** Walk `dir`; the node tree has a node per directory down to `depth`, every file is counted in all its ancestors. */
export function scan(dir: string, o: ScanOpts): Node {
  const seen = new Set<string>(); // dev:ino of hard-linked files already counted
  const walk = (d: string, level: number): Node => {
    const rel = relative(o.base, d), n: Node = { path: d, ...emptyAgg(), kids: new Map(), names: [], list: [] };
    let ents; try { ents = readdirSync(d, { withFileTypes: true }); } catch { return n; }
    const keep = o.fileLevel?.(rel) ?? false;
    for (const e of ents) {
      const p = join(d, e.name);
      if (e.isSymbolicLink()) continue;
      if (e.isDirectory()) {
        if (o.skip?.(relative(o.base, p))) continue;
        n.names.push(e.name);
        const k = walk(p, level + 1);
        fold(n, k);
        if (level < (o.depth ?? 99)) n.kids.set(e.name, k);
        continue;
      }
      let st; try { st = lstatSync(p); } catch { continue; }
      if (!st.isFile()) continue;
      if (st.nlink > 1) { const id = `${st.dev}:${st.ino}`; if (seen.has(id)) continue; seen.add(id); }
      const bytes = st.blocks * 512;
      add(n, bytes, st.mtimeMs);
      o.onFile?.(p, bytes, st.mtimeMs);
      n.names.push(e.name);
      if (keep) n.list.push({ path: p, bytes, mtime: st.mtimeMs });
    }
    return n;
  };
  return walk(dir, 0);
}

/** One file or directory's totals, by lstat (a file) or a scan (a directory). */
export function measure(p: string, o: ScanOpts): Node | undefined {
  let st; try { st = lstatSync(p); } catch { return undefined; }
  if (st.isDirectory()) return scan(p, o);
  if (!st.isFile()) return undefined;
  const a = emptyAgg(); add(a, st.blocks * 512, st.mtimeMs);
  return { path: p, ...a, kids: new Map(), names: [], list: [{ path: p, bytes: a.bytes, mtime: st.mtimeMs }] };
}

export const DAY = 86400000;
export const days = (ms: number, now: number) => Math.floor((now - ms) / DAY);
export function human(b: number): string {
  if (b >= 1 << 30) return (b / (1 << 30)).toFixed(1) + ' GB';
  if (b >= 1 << 20) return (b / (1 << 20)).toFixed(0) + ' MB';
  if (b >= 1 << 10) return (b / (1 << 10)).toFixed(0) + ' KB';
  return b + ' B';
}
export const day = (ms: number) => (Number.isFinite(ms) && ms > 0 ? new Date(ms).toISOString().slice(5, 10) : '-');
