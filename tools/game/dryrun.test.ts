// `bun run batch --dry-run` starts nothing and writes nothing (Splice, 2026-10-26, with cli.test.ts for `tools/game/cli.ts --dry-run`). Bun.spawn and Bun.spawnSync are stubbed
// and the real steps are planned: the only process a dry run may ask for is the read-only pgrep that says whether the Editor is open.
import { afterEach, expect, test } from 'bun:test';
import { existsSync, mkdirSync, mkdtempSync, readdirSync, rmSync } from 'node:fs';
import { join } from 'node:path';
import { runSteps, STEPS, type Ctx } from './batch';
import { DEFAULT_FILE, ROOT } from './shots';

const realSpawn = Bun.spawn, realSpawnSync = Bun.spawnSync;
afterEach(() => { (Bun as any).spawn = realSpawn; (Bun as any).spawnSync = realSpawnSync; });
function stubSpawns(): string[][] {
  const asked: string[][] = [];
  const stub = (cmd: any) => { const c: string[] = Array.isArray(cmd) ? cmd : cmd.cmd; asked.push(c); if (c[0] !== 'pgrep') throw new Error(`a dry run started ${c.join(' ')}`); return { stdout: Buffer.from('') } as any; };
  (Bun as any).spawn = stub; (Bun as any).spawnSync = stub;
  return asked;
}
const ctx = (over: Partial<Ctx> = {}): Ctx => ({ dry: true, mode: 'batchmode', shotsFile: DEFAULT_FILE, shotsOut: join(ROOT, 'out', 'batch', 'shots'), facts: {}, say: () => {}, ...over });

test('every real step, planned on a dry run (rebuild and perf064 asked for, both Unity modes): no process started, every step says what it would do', async () => {
  const asked = stubSpawns();
  for (const mode of ['batchmode', 'editor'] as const) {
    const rows = await runSteps(STEPS, ctx({ mode, rebuild: true, perf064: true }), STEPS.map(s => s.id));
    expect(rows.map(r => r.status)).toEqual(STEPS.map(() => 'ok'));
    for (const r of rows) expect(r.detail.length).toBeGreaterThan(0);
  }
  expect(asked.filter(c => c[0] !== 'pgrep')).toEqual([]);
  expect(asked).toEqual([]); // the steps themselves do not even look
});

test('the game-build step on a dry run names the build and does not run it', async () => {
  const asked = stubSpawns();
  const rows = await runSteps(STEPS, ctx({ rebuild: true }), ['game-build']);
  expect(rows.find(r => r.id === 'game-build')!.detail).toContain('bun tools/game/cli.ts --build');
  expect(asked).toEqual([]);
});

test('bun tools/game/batch.ts --dry-run: exit 0, the plan printed, nothing written to its output folder, Unity never mentioned as started', () => {
  const scratch = join(ROOT, 'out', 'scratch', 'splice'); mkdirSync(scratch, { recursive: true });
  const out = mkdtempSync(join(scratch, 'batch-'));
  try {
    const r = Bun.spawnSync([process.execPath, join(ROOT, 'tools', 'game', 'batch.ts'), '--dry-run', '--rebuild', '--perf064'], { cwd: ROOT, env: { ...process.env, SUBORNED_BATCH_OUT: out } });
    const text = r.stdout.toString();
    expect(r.exitCode).toBe(0);
    expect(text).toContain('bun run batch --dry-run');
    expect(text).toContain('game-build');
    expect(text).toContain('bun tools/game/cli.ts --build');
    expect(readdirSync(out)).toEqual([]);
    expect(existsSync(join(out, 'logs'))).toBe(false);
  } finally { rmSync(out, { recursive: true, force: true }); }
});

test('bun tools/game/batch.ts --sky --then-full --dry-run: exit 0, only the --sky steps planned, the smoke named, the behind command printed, nothing started or written', () => {
  const scratch = join(ROOT, 'out', 'scratch', 'splice'); mkdirSync(scratch, { recursive: true });
  const out = mkdtempSync(join(scratch, 'batch-'));
  try {
    const r = Bun.spawnSync([process.execPath, join(ROOT, 'tools', 'game', 'batch.ts'), '--sky', '--then-full', '--dry-run'], { cwd: ROOT, env: { ...process.env, SUBORNED_BATCH_OUT: out } });
    const text = r.stdout.toString();
    expect(r.exitCode).toBe(0);
    expect(text).toContain('--sky: steps sim, words, compile, hostlogic, kit, world, unity-world, game-build');
    expect(text).toContain('-proofOnly base'); expect(text).toContain('then behind: SUBORNED_PLAYER_DIR=out/build/full');
    for (const s of ['unity-match', 'game-check', 'shots', 'budget', 'late-fight', 'tp-long', 'perf064']) expect(text).toMatch(new RegExp(`\\n${s}  \\(skipped`));
    expect(text).not.toContain('proof: FULL');
    expect(readdirSync(out)).toEqual([]);
    expect(text).not.toContain('behind   the full batch, pid'); // nothing started behind
  } finally { rmSync(out, { recursive: true, force: true }); }
});

test('--then-full without --sky, and --sky with --full, are refused before anything runs', () => {
  for (const a of [['--then-full'], ['--sky', '--full']]) {
    const r = Bun.spawnSync([process.execPath, join(ROOT, 'tools', 'game', 'batch.ts'), ...a, '--dry-run'], { cwd: ROOT });
    expect(r.exitCode, a.join(' ')).toBe(2); expect(r.stderr.toString(), a.join(' ')).toContain('conflicting argument');
  }
});
