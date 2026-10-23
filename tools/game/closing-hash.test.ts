// Brief 045 (Redline's headless check, wired by Splice): while a public fight is open the host saves with the closing checkpoint, which was a full replay
// (Engine.Play from tick 0, 70-270 ms late in a mission). Keel's Game.ClosingHash() hashes the state as Finish() would leave it, without mutating the live game.
// For loud-fight and Sky's session 1, at every order and 5-second save while a fight is open, ClosingHash() must equal the replay's closing checkpoint,
// and at the run's last fight tick it must cost under 1 ms (a full replay cannot).
//
// The harness is tools/game/closing-hash/ (a small .NET program over sim/Suborned.Sim; Keel's CLI is not touched): it plays each script as the host does, saves on every
// order and every 5 s of clock, and at each save with a fight open takes g.ClosingHash() and the replay's closing checkpoint. One JSON line per script:
//   {"script":"<path>","result":"Success|Failure","tick":N,"rows":[{"tick":N,"why":"order|clock","cheap":"<hex>","replay":"<hex>"}…],"lastFightTick":N,"cheapMs":x}
// cheapMs is the best of 20 calls at lastFightTick after a warm-up call. Skipped only while Game.ClosingHash is not in the engine's source (it is, from engine 0.25.0).
import { describe, expect, test } from 'bun:test';
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT } from './shots';

const env = { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1', ...(process.env.DOTNET_ROOT || !existsSync('/opt/homebrew/opt/dotnet/libexec') ? {} : { DOTNET_ROOT: '/opt/homebrew/opt/dotnet/libexec' }) };
export const SCRIPTS = ['contracts/missions/handover/routes/loud-fight.json', 'briefs/045/runs/sky-0.2.58-session1.script.json'];
export const LIMIT_MS = 1;

/** Is there a Game.ClosingHash in the engine's source? */
export function apiPresent(root = ROOT): boolean {
  const dir = join(root, 'sim/Suborned.Sim');
  return existsSync(dir) && readdirSync(dir).filter(f => f.endsWith('.cs')).some(f => /public\s+ulong\s+ClosingHash\s*\(\s*\)/.test(readFileSync(join(dir, f), 'utf8')));
}
export type ClosingRows = { script: string; result: string; tick: number; rows: { tick: number; why: string; cheap: string; replay: string }[]; lastFightTick: number; cheapMs: number };
/** What is wrong with one script's line, in words; empty when it is right. */
export function problemsOf(r: ClosingRows): string[] {
  const out: string[] = [];
  if (r.rows.length === 0) out.push(`${r.script}: no save while a fight was open, so nothing was checked`);
  const diff = r.rows.filter(x => x.cheap !== x.replay);
  if (diff.length) out.push(`${r.script}: ClosingHash differs from the replay's closing checkpoint at ${diff.length} of ${r.rows.length} saves, first at tick ${diff[0]!.tick} (${diff[0]!.why}): ${diff[0]!.cheap} vs ${diff[0]!.replay}`);
  if (!(r.cheapMs < LIMIT_MS)) out.push(`${r.script}: ClosingHash costs ${r.cheapMs} ms at tick ${r.lastFightTick} (limit ${LIMIT_MS} ms; a full replay is 70-270 ms)`);
  return out;
}

describe('the verdict on a line from the engine', () => {
  const row = (o: Partial<ClosingRows> = {}): ClosingRows => ({ script: 's', result: 'Success', tick: 3124, rows: [{ tick: 1000, why: 'order', cheap: 'aa', replay: 'aa' }], lastFightTick: 3100, cheapMs: 0.02, ...o });
  test('equal hashes and under 1 ms pass', () => expect(problemsOf(row())).toEqual([]));
  test('a differing hash is named by tick; a replay-cost call fails; no fight is not a pass', () => {
    expect(problemsOf(row({ rows: [{ tick: 1000, why: 'order', cheap: 'aa', replay: 'aa' }, { tick: 1200, why: 'clock', cheap: 'aa', replay: 'bb' }] }))[0]).toContain('differs from the replay\'s closing checkpoint at 1 of 2 saves, first at tick 1200 (clock)');
    expect(problemsOf(row({ cheapMs: 188 }))[0]).toContain('costs 188 ms at tick 3100');
    expect(problemsOf(row({ rows: [] }))[0]).toContain('nothing was checked');
  });
});

describe('Game.ClosingHash against the replay (loud-fight, Sky\'s session 1)', () => {
  const present = apiPresent();
  test.skipIf(!present || !Bun.which('dotnet'))('equal at every save while a fight is open, under 1 ms at the last fight tick', () => {
    const out = join(ROOT, 'out/closing-hash');
    const b = Bun.spawnSync(['dotnet', 'build', join(ROOT, 'tools/game/closing-hash'), '-c', 'Release', '-o', out, '-v', 'q', '-nologo'], { cwd: ROOT, env, stdout: 'pipe', stderr: 'pipe' });
    if (b.exitCode !== 0) throw new Error(`tools/game/closing-hash does not build against sim/Suborned.Sim:\n${b.stdout}${b.stderr}`);
    const r = Bun.spawnSync(['dotnet', join(out, 'closing-hash.dll'), ROOT, ...SCRIPTS], { cwd: ROOT, env, stdout: 'pipe', stderr: 'pipe' });
    const lines = r.stdout.toString().split('\n').filter(l => l.startsWith('{'));
    if (r.exitCode !== 0 || lines.length !== SCRIPTS.length) throw new Error(`closing-hash gave ${lines.length} of ${SCRIPTS.length} lines (exit ${r.exitCode}): ${r.stderr.toString().slice(0, 400)}`);
    const all = lines.map(l => JSON.parse(l) as ClosingRows);
    expect(all.flatMap(problemsOf)).toEqual([]);
  }, 120000);
  test('says why it is skipped', () => { if (!present) console.log('closing-hash.test: skipped, no `public ulong ClosingHash()` in sim/Suborned.Sim yet (Keel, brief 045 F3)'); expect(true).toBe(true); });
});
