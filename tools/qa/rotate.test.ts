// Rotation of out/qa (brief 060, Splice): the last 5 passes and the last 2 dev passes stay, a KEEP file holds a run, nothing else is touched.
import { expect, test } from 'bun:test';
import { existsSync, mkdirSync, mkdtempSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { rotate, stale } from './rotate';

const ids = (n: number) => Array.from({ length: n }, (_, i) => `202610${String(10 + i).padStart(2, '0')}-020000`);

test('stale: all but the newest 5 passes and the newest 2 dev passes', () => {
  const names = [...ids(8), 'dev-20261011-100000', 'dev-20261012-100000', 'dev-20261013-100000'];
  expect(stale(names).sort()).toEqual([...ids(3), 'dev-20261011-100000'].sort());
  expect(stale(ids(5))).toEqual([]);
  expect(stale([...ids(5), 'engine', 'host', 'sky-pick'])).toEqual([]); // the lanes' caches and a named run are not passes
});
test('rotate removes the stale folders, holds one with a KEEP file, and a dry run removes nothing', () => {
  const root = mkdtempSync(join(tmpdir(), 'qa-rot-'));
  for (const n of ids(7)) { mkdirSync(join(root, 'out/qa', n), { recursive: true }); writeFileSync(join(root, 'out/qa', n, 'report.html'), 'x'.repeat(100)); }
  writeFileSync(join(root, 'out/qa', ids(7)[0]!, 'KEEP'), '');
  const dry = rotate(root, { dry: true });
  expect(dry.removed).toEqual([ids(7)[1]!]); expect(dry.held).toEqual([ids(7)[0]!]); expect(existsSync(join(root, 'out/qa', ids(7)[1]!))).toBe(true);
  const real = rotate(root);
  expect(real.removed).toEqual([ids(7)[1]!]); expect(existsSync(join(root, 'out/qa', ids(7)[1]!))).toBe(false); expect(existsSync(join(root, 'out/qa', ids(7)[0]!))).toBe(true);
  expect(real.kept.length).toBe(5);
});
