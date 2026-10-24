// Rotation of out/qa/<run>/ (brief 060, Splice): a pass writes a report, its rows and the exemplars' scripts, a few MB; the nightly run would pile them up. Keep the last KEEP_RUNS passes and the last
// KEEP_DEV small `--dev` passes, and anything with a file called KEEP in it (or in out/qa itself). Run ids sort by time (YYYYMMDD-HHMMSS, `dev-` before it for a dev pass).
// The summary JSON in briefs/060/qa/ is the committed record and is never rotated. The storage policy (tools/storage/policy.ts rule `qa-runs`) reads the same function, so `bun run storage --prune` agrees.
import { existsSync, readdirSync, rmSync, statSync } from 'node:fs';
import { join } from 'node:path';

export const KEEP_RUNS = 5, KEEP_DEV = 2;
const isDev = (n: string) => n.startsWith('dev-');
/** A pass folder: `YYYYMMDD-HHMMSS` or `dev-YYYYMMDD-HHMMSS` (a `--run id` of Sky's own choosing is not rotated). out/qa/engine and out/qa/host are the lanes' build caches, not passes. */
export const isRun = (n: string) => /^(dev-)?\d{8}-\d{6}$/.test(n);

/** The run folders to delete: all but the newest `keep` passes and the newest `keepDev` dev passes (names only; the caller holds back any folder with a KEEP file). */
export function stale(names: string[], keep = KEEP_RUNS, keepDev = KEEP_DEV): string[] {
  const runs = names.filter(n => isRun(n) && !isDev(n)).sort().reverse(), dev = names.filter(n => isRun(n) && isDev(n)).sort().reverse();
  return [...runs.slice(keep), ...dev.slice(keepDev)];
}

export type Rotation = { kept: string[]; removed: string[]; held: string[]; bytes: number };
const size = (p: string): number => { let n = 0; for (const e of readdirSync(p, { withFileTypes: true })) { const q = join(p, e.name); n += e.isDirectory() ? size(q) : statSync(q).size; } return n; };

/** Rotate `<root>/out/qa`: delete the stale runs (not with `dry`), say what was kept, removed and held back by a KEEP file. */
export function rotate(root: string, o: { keep?: number; keepDev?: number; dry?: boolean } = {}): Rotation {
  const dir = join(root, 'out/qa'); if (!existsSync(dir)) return { kept: [], removed: [], held: [], bytes: 0 };
  const names = readdirSync(dir, { withFileTypes: true }).filter(e => e.isDirectory() && isRun(e.name)).map(e => e.name), all = existsSync(join(dir, 'KEEP'));
  const out: Rotation = { kept: [], removed: [], held: [], bytes: 0 }, gone = new Set(stale(names, o.keep, o.keepDev));
  for (const n of names.sort()) {
    if (!gone.has(n)) { out.kept.push(n); continue; }
    if (all || existsSync(join(dir, n, 'KEEP'))) { out.held.push(n); continue; }
    out.bytes += size(join(dir, n)); out.removed.push(n);
    if (!o.dry) rmSync(join(dir, n), { recursive: true, force: true });
  }
  return out;
}
