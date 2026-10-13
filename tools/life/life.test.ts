import { test, expect } from 'bun:test';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

const run = (out: string) => Bun.spawnSync(['bun', join(import.meta.dir, 'cli.ts'), out]);

test('life generator is deterministic: two runs, byte-identical', async () => {
  const a = join(tmpdir(), `life-a-${process.pid}.json`), b = join(tmpdir(), `life-b-${process.pid}.json`);
  expect(run(a).exitCode).toBe(0);
  expect(run(b).exitCode).toBe(0);
  const [ta, tb] = [await Bun.file(a).text(), await Bun.file(b).text()];
  expect(ta.length).toBeGreaterThan(1000);
  expect(ta).toBe(tb);
});
