// Transit's code hash covers what its verdict rests on, the world generator included, and the transit code moves like skyline's (a world rule's result is
// a function of code that changes every day): a rerun that still matches is labelled drift, a rule whose result differs needs a declared cause.
import { expect, test } from 'bun:test';
import { cp, mkdtemp, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { root } from '../../rag/config';
import { skylineFiles } from '../skyline/hash';
import { transit } from '../transit';
import { toolsHash, transitFiles } from '../toolhash';
import { reached } from './imports';

test('the hash covers the plan checker, the world generator and the checker plumbing, and no test', async () => {
  const files = transitFiles();
  for (const f of ['tools/plan/checks.ts', 'tools/plan/world-rules.ts', 'tools/levels/apply.ts', 'tools/levels/checker.ts', 'tools/world/gen/signatures.ts', 'tools/world/civic/look.ts', 'tools/kit/fit.ts']) expect(files, f).toContain(f);
  expect(files.filter(f => f.endsWith('.test.ts'))).toEqual([]);
  expect(await toolsHash()).toMatch(/^[0-9a-f]{64}$/);
  expect(files).not.toEqual(skylineFiles()); // the two objectives keep their own ledgers
});

test('every file the transit verdict code imports, however far, is in the hash', async () => {
  const hashed = new Set(transitFiles()), seen = await reached(['tools/levels/apply.ts', 'tools/levels/checker.ts']);
  const missing = [...seen].filter(f => !hashed.has(f));
  expect(missing).toEqual([]);
  expect([...seen].length).toBeGreaterThan(30);
});

test('the hash moves when a world generator file does (a copy of the covered files, one comment added), and not otherwise', async () => {
  const copy = await mkdtemp(join(tmpdir(), 'toolhash-'));
  for (const f of transitFiles()) await cp(resolve(root, f), join(copy, f), { recursive: true });
  const was = await toolsHash(copy);
  expect(was).toBe(await toolsHash());
  const file = join(copy, 'tools/world/gen/signatures.ts');
  await writeFile(file, `${await Bun.file(file).text()}\n// a comment\n`);
  expect(await toolsHash(copy)).not.toBe(was);
});

test('the transit code moves and records its state when a run ends', () => {
  expect(transit.code.moves).toBe(true);
  expect(typeof transit.code.record).toBe('function');
});
