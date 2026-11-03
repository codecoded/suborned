// Brief 079: tools/decokit/run.ts refuses an unknown flag before it does anything (a stray --version ran a full export into the shared out/decokit, 2026-10-30).
import { expect, test } from 'bun:test';
import { existsSync, mkdtempSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';

const RUN = resolve(import.meta.dir, 'run.ts');
const run = (...args: string[]) => { const out = join(mkdtempSync(join(tmpdir(), 'decokitrun-')), 'out'), p = Bun.spawnSync(['bun', RUN, '--out', out, ...args], { stdout: 'pipe', stderr: 'pipe' }); return { out, text: p.stdout.toString() + p.stderr.toString() }; };

for (const args of [['--version'], ['-v'], ['stray'], ['--only'], ['--help']]) test(`run.ts ${args.join(' ')}: usage, nothing built, nothing written`, () => {
  const r = run(...args);
  expect(r.text).toContain('usage: bun run decokit');
  expect(existsSync(r.out)).toBe(false); // the gate stands before the output folder is made, the texture passes and Blender
});
