// `--help`, `-h` and unknown flags print the usage and do nothing (the kit copy once ran from `--help`). --catalogue rides along so a regression of the
// gate could write kit.json at most, never copy; the test then checks kit.json is untouched.
import { expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';

const ROOT = join(import.meta.dir, '..', '..');
const kitJson = join(ROOT, 'contracts', 'world', 'kit.json');
const run = (...a: string[]) => Bun.spawnSync([process.execPath, join(ROOT, 'tools', 'kit', 'cli.ts'), ...a, '--catalogue'], { cwd: ROOT });

for (const flag of ['--help', '-h', '--bogus']) {
  test(`kit cli ${flag}: usage, exit 0, nothing written or copied`, () => {
    const before = readFileSync(kitJson, 'utf8');
    const r = run(flag), out = r.stdout.toString();
    expect(r.exitCode).toBe(0);
    expect(out).toContain('usage: bun run kit');
    expect(out).not.toMatch(/kit\.json (written|unchanged)|copied \d+ files/);
    if (flag === '--bogus') expect(r.stderr.toString()).toContain('unknown option --bogus');
    expect(readFileSync(kitJson, 'utf8')).toBe(before);
  });
}
