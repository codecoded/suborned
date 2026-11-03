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

// --dry-run never builds, packs, signs, sends or opens (Splice, 2026-10-26: `--send --dry-run` started a real Unity build). The CLI runs as a subprocess with
// its build folder pointed at a scratch folder (SUBORNED_GAME_OUT), every process it asks for logged (SUBORNED_GAME_SPAWN_LOG) and none but the read-only ones
// really started (SUBORNED_GAME_SPAWN_STUB), so a regression fails this test and cannot launch Unity. The CLI itself also refuses (exit 3) any other spawn on a dry run.
import { mkdirSync, mkdtempSync, readdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { basename } from 'node:path';

const SCRATCH = join(ROOT, 'out', 'scratch', 'splice');
const READ_ONLY = ['git', 'pgrep', 'sysctl'];
/** Every file under a folder with its size and mtime: a dry run leaves this unchanged. */
const tree = (d: string): string[] => !existsSync(d) ? ['(absent)'] : readdirSync(d, { recursive: true, withFileTypes: true } as any).map((e: any) => { const p = join(e.parentPath ?? e.path, e.name); const s = statSync(p); return `${p} ${s.size} ${s.mtimeMs}`; }).sort();
const sha = Bun.spawnSync(['git', 'rev-parse', 'HEAD'], { cwd: ROOT }).stdout.toString().trim();

type Fixture = 'no app' | 'old app, no record' | 'old app, inputs changed';
function fixture(kind: Fixture): string {
  mkdirSync(SCRATCH, { recursive: true });
  const dir = mkdtempSync(join(SCRATCH, 'game-'));
  if (kind === 'no app') return dir;
  mkdirSync(join(dir, 'The Suborned.app', 'Contents', 'MacOS'), { recursive: true });
  writeFileSync(join(dir, 'The Suborned.app', 'Contents', 'MacOS', 'The Suborned'), '#!/bin/sh\n');
  writeFileSync(join(dir, 'build.json'), JSON.stringify({ result: 'Succeeded', version: '0.2.1', build: 1, sha, at: new Date().toISOString() }));
  if (kind === 'old app, inputs changed') writeFileSync(join(dir, 'inputs.json'), JSON.stringify({ schema: 'suborned.build-inputs/1', fingerprint: 'not-the-current-one', files: {} }));
  return dir;
}
function dry(dir: string, ...a: string[]) {
  const log = join(dir, '..', `${basename(dir)}.spawns.log`);
  rmSync(log, { force: true });
  const before = tree(dir), builds = join(ROOT, 'runs', 'builds', 'builds.json'), version = join(ROOT, 'tools', 'game', 'version.json');
  const files = (f: string) => (existsSync(f) ? readFileSync(f, 'utf8') : '(absent)');
  const [b0, v0] = [files(builds), files(version)];
  const r = Bun.spawnSync([process.execPath, join(ROOT, 'tools', 'game', 'cli.ts'), ...a, '--dry-run'], {
    cwd: ROOT, env: { ...process.env, SUBORNED_GAME_OUT: dir, SUBORNED_GAME_SPAWN_LOG: log, SUBORNED_GAME_SPAWN_STUB: '1' },
  });
  const spawned: string[][] = existsSync(log) ? readFileSync(log, 'utf8').split('\n').filter(Boolean).map(l => JSON.parse(l)) : [];
  rmSync(log, { force: true });
  return { r, out: r.stdout.toString(), err: r.stderr.toString(), spawned, unchanged: JSON.stringify(tree(dir)) === JSON.stringify(before) && files(builds) === b0 && files(version) === v0 };
}
const isReadOnly = (c: string[]) => READ_ONLY.includes(basename(c[0]!)) || (basename(c[0]!) === 'security' && c[1] === 'find-identity');

const MODES: string[][] = [['--send'], ['--send', 'skys-macbook-air-1'], ['--send', '--build'], ['--pack'], ['--pack', '--build'], ['--check'], ['--build'], ['--build', '--check'], [], ['--no-open'], ['--windowed'], ['--signing-check']];
for (const kind of ['no app', 'old app, no record', 'old app, inputs changed'] as Fixture[]) {
  for (const mode of MODES) {
    if (kind !== 'no app' && mode.length > 0 && !['--send', '--pack', '--build'].includes(mode[0]!) && mode[0] !== '--check') continue; // the other folders: the paths that wrote or built before
    test(`--dry-run ${mode.join(' ') || '(no flag)'} with ${kind}: exit 0, the plan printed, nothing spawned that builds, nothing written`, () => {
      const dir = fixture(kind);
      try {
        const d = dry(dir, ...mode);
        expect(d.err).not.toContain('refused to start');
        expect(d.r.exitCode).toBe(0);
        expect(d.spawned.filter(c => !isReadOnly(c))).toEqual([]);
        expect(d.unchanged).toBe(true);
        if (!mode.includes('--signing-check')) {
          expect(d.out).toContain('dry run: nothing is built');
          if (mode.includes('--send')) expect(d.out).toMatch(/would run: .*tailscale/i);
          if (mode.includes('--pack') || mode.includes('--send')) expect(d.out).toMatch(/would (sign|zip) /);
          if (mode.includes('--send') && mode[1] && !mode[1].startsWith('--')) expect(d.out).toContain(`${mode[1]}:`);
          if (kind === 'no app') expect(d.out).toContain('would build 0.2.');
          if (mode.length === 0 || mode[0] === '--windowed') expect(d.out).toMatch(/would open|would not open/);
          expect(d.out).not.toMatch(/game: (building|sending|signing|notarising)/);
        }
      } finally { rmSync(dir, { recursive: true, force: true }); }
    });
  }
}

test('the stub is a real control: without --dry-run the same harness sees the notary call logged and not started', () => {
  const dir = fixture('no app'), log = join(dir, '..', `${basename(dir)}.control.log`);
  try {
    rmSync(log, { force: true });
    const r = Bun.spawnSync([process.execPath, join(ROOT, 'tools', 'game', 'cli.ts'), '--signing-check'], { cwd: ROOT, env: { ...process.env, SUBORNED_GAME_OUT: dir, SUBORNED_GAME_SPAWN_LOG: log, SUBORNED_GAME_SPAWN_STUB: '1' } });
    const spawned = readFileSync(log, 'utf8').split('\n').filter(Boolean).map(l => JSON.parse(l) as string[]);
    expect(r.exitCode).toBeGreaterThanOrEqual(0);
    expect(spawned.some(c => c[0] === 'xcrun' && c[1] === 'notarytool')).toBe(true);
  } finally { rmSync(dir, { recursive: true, force: true }); rmSync(log, { force: true }); }
});

test('a dry run that tried to start a builder would be refused by the CLI itself (exit 3), not run', () => {
  // the wrapper's rule, read from the source: every spawn in cli.ts goes through it, and it refuses anything that is not read-only while --dry-run is on
  const src = readFileSync(join(ROOT, 'tools', 'game', 'cli.ts'), 'utf8');
  expect(src).toContain("if (DRY && !ro)");
  expect(src.match(/realSpawnSync\(/g)!.length).toBe(1);
  expect(src).not.toMatch(/\bBun\.spawn/);
});
