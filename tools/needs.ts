// A test that reads a generated, git-ignored input (out/world, out/people, out/decokit, ...) skips with its reason when the checkout lacks it, rather
// than failing (brief 098 fast loop, Splice: a fresh worktree failed 21 tests and 3 files this way). `needs` registers one skipped test that names
// the file and the command that makes it, so `bun test` shows why; guard the rest with `if (has) { ... }` or `test.skipIf(!has)`.
import { test } from 'bun:test';
import { existsSync } from 'node:fs';
import { isAbsolute, join, resolve } from 'node:path';

export const ROOT = resolve(import.meta.dir, '..');

/** True when every path exists (relative to the repo, or absolute); otherwise one skipped test says what is missing and how to make it. */
export function needs(paths: string | string[], make: string): boolean {
  const missing = (Array.isArray(paths) ? paths : [paths]).filter(p => !existsSync(isAbsolute(p) ? p : join(ROOT, p)));
  if (!missing.length) return true;
  const why = `needs ${missing.join(', ')} (git-ignored; \`${make}\` makes it)`, file = new Error().stack?.match(/([\w./-]+\.test\.ts)/)?.[1]?.replace(`${ROOT}/`, '');
  test.skip(why, () => {}); // listed among the skips
  console.warn(`${file ?? 'a test'}: skipped what ${why.replace('needs', 'reads')}`); // and said, as bun's default report names no skipped test
  return false;
}
