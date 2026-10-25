// `--help`, `-h` and unknown flags print the usage and do nothing (a batch build once started from `--help`). --dry-run --no-open ride along so that
// even a regression of the gate would only say "would build" and never build or open the app.
import { expect, test } from 'bun:test';
import { existsSync, statSync } from 'node:fs';
import { join } from 'node:path';

const ROOT = join(import.meta.dir, '..', '..');
const stamp = join(ROOT, 'out', 'build', 'play', 'build.json');
const run = (...a: string[]) => Bun.spawnSync([process.execPath, join(ROOT, 'tools', 'game', 'cli.ts'), ...a, '--dry-run', '--no-open'], { cwd: ROOT });

for (const flag of ['--help', '-h', '--bogus']) {
  test(`game cli ${flag}: usage, exit 0, no build`, () => {
    const before = existsSync(stamp) ? statSync(stamp).mtimeMs : 0;
    const r = run(flag), out = r.stdout.toString();
    expect(r.exitCode).toBe(0);
    expect(out).toContain('usage: bun tools/game/cli.ts');
    expect(out).not.toMatch(/game: (building|would build|the app is current)/);
    if (flag === '--bogus') expect(r.stderr.toString()).toContain('unknown option --bogus');
    expect(existsSync(stamp) ? statSync(stamp).mtimeMs : 0).toBe(before);
  });
}
