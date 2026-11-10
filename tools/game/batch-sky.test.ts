// bun run batch --sky (batch-sky.ts): the steps it picks, the smoke's judgement, the builds.json entry, the behind batch's verdict and its stop. No Unity, no player.
import { describe, expect, test } from 'bun:test';
import { mkdtempSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { pick, runSteps, STEPS, type Ctx, type Step } from './batch';
import { SKY_STEPS, behindCommand, behindEnv, goesOn, judgeSmoke, logBuilt, skyLines, skySkips, smokePlan, stopBehind, verdict, type SmokeInput } from './batch-sky';
import { AFTER_WORLD } from './batch-paths';
import { loadMap, sectionIds } from './proofdelta';

const ids = STEPS.map(s => s.id);
describe('the steps a --sky runs', () => {
  test('sim, words, compile, hostlogic, kit, world, unity-world and the build with its smoke; never the match, the proof, the shots, the frame times', () => {
    expect(pick(ids, { skip: skySkips(ids) }).run).toEqual(['sim', 'words', 'compile', 'hostlogic', 'kit', 'world', 'unity-world', 'game-build']);
    expect(SKY_STEPS.every(s => ids.includes(s))).toBe(true);
    expect(skySkips(ids)).toEqual(['unity-match', 'game-check', 'shots', 'budget', 'late-fight', 'tp-long', 'perf064']);
    expect(pick(ids, { from: 'world', skip: skySkips(ids) }).run).toEqual(['world', 'unity-world', 'game-build']); // --from composes
  });
  test('the gated ones still run only when their gate says (sim when the engine changed, the scene when the kit or world did)', () => {
    for (const s of ['sim', 'kit', 'world', 'unity-world']) expect(STEPS.find(x => x.id === s)!.when, s).toBeDefined();
    expect(AFTER_WORLD).toEqual(['unity-world', 'unity-match', 'late-fight', 'tp-long']);
  });
  test('the build\'s plan is the proof\'s base alone: every section left to the full batch, and said', () => {
    const m = loadMap(), p = smokePlan(m, 'snap1');
    expect(p).toMatchObject({ mode: 'delta', only: [], snapshot: 'snap1', seconds: m.baseSeconds });
    expect(Object.keys(p.skips)).toEqual(sectionIds(m)); expect(Object.values(p.skips)[0]).toContain('the full batch proves it');
  });
  test('its game-build plan says the smoke on a dry run', () => {
    const c: Ctx = { dry: true, mode: 'batchmode', shotsFile: '', shotsOut: '', facts: {}, say: () => {}, sky: true };
    const plan = STEPS.find(s => s.id === 'game-build')!.plan(c).join('\n');
    expect(plan).toContain('-proofOnly base'); expect(plan).toContain('--proof-plan'); expect(plan).not.toContain('--check');
  });
});

describe('the smoke', () => {
  const ok = (o: Partial<SmokeInput> = {}): SmokeInput => ({
    proof: { status: 'ok', log: ['briefing up: True, modal Book', 'rendered: True', 'play: modal None, paused False, tick 40, zoom 10'] }, log: 'Game: display 1920x1080\n',
    contents: { 'Assets/Scenes/OldTown.unity': 126 }, since: 100, files: { 'play.png': 200, 'play.hud.png': 200, 'play.manifest.json': 200 },
    manifest: { elements: [{ kind: 'image' }, { kind: 'bar' }] }, seconds: 31, ...o }); // play's kinds (0.2.312: image, bar, next, marker)
  test('a build that draws, plays and shows its HUD passes, and says so', () => {
    const j = judgeSmoke(ok()); expect(j.ok).toBe(true); expect(j.detail).toContain('played to tick 40'); expect(j.detail).toContain('the HUD drew 2 elements');
  });
  test('each failure is named: blank frame, no play, no HUD, an Exception, an old shot, no OldTown, no proof at all', () => {
    const why = (o: Partial<SmokeInput>) => judgeSmoke(ok(o)).problems.join(' | ');
    expect(why({ proof: { status: 'blank', log: ok().proof.log } })).toContain('pipeline did not start');
    expect(why({ proof: { status: 'ok', log: ['briefing up: True', 'play: modal None, paused True, tick 0, zoom 10'] } })).toContain('play did not start');
    expect(why({ manifest: { elements: [] } })).toContain('the HUD drew no element');
    expect(why({ log: 'NullReferenceException: Object reference not set\n  at HostFeed.Tick ()\n' })).toContain('1 Exception line');
    expect(why({ files: { 'play.png': 50, 'play.hud.png': 200, 'play.manifest.json': 200 } })).toContain('play.png was not written');
    expect(why({ contents: { 'Assets/Scenes/HarbourDistrict.unity': 1 } })).toContain('OldTown.unity');
    expect(why({ proof: undefined })).toContain('wrote no proof.json');
  });
});

describe('what it records', () => {
  test('builds.json: one entry per build number, sent [] until sent; a scratch build is not logged', () => {
    const f = join(mkdtempSync(join(tmpdir(), 'sky-')), 'builds.json'), st = { result: 'Succeeded', build: 312, version: '0.2.312', sha: 'abc', dirty: true, at: 'then' };
    expect(logBuilt(st, f)).toContain('logged'); expect(logBuilt(st, f)).toContain('already');
    const j = JSON.parse(readFileSync(f, 'utf8')); expect(j.builds).toHaveLength(1); expect(j.builds[0]).toMatchObject({ build: 312, version: '0.2.312', sent: [], zip: null, whatsNew: '' });
    expect(logBuilt({ result: 'Succeeded', version: '0.2.312-scratch' }, f)).toContain('not logged'); expect(logBuilt({ result: 'Succeeded', build: 313, version: '0.2.313-scratch' }, f)).toContain('not logged'); // GameBuild.CliScratch stamps the next number
  });
  test('a --sky over 10 minutes warns with its slowest step', () => {
    expect(skyLines([{ id: 'hostlogic', status: 'ok', seconds: 100 }, { id: 'game-build', status: 'ok', seconds: 90 }])).toEqual([]);
    expect(skyLines([{ id: 'unity-world', status: 'ok', seconds: 680 }, { id: 'game-build', status: 'ok', seconds: 60 }])[0]).toContain('slowest: unity-world 680 s');
  });
});

describe('the full batch behind', () => {
  test('its own player and folder, --full, --force past the Sky-is-playing wait, its players behind, a verdict; never Sky\'s app', () => {
    const cmd = behindCommand('/v.json'), e = behindEnv();
    expect(cmd).toEqual(['bun', 'tools/game/batch.ts', '--full', '--force', '--behind', '--batchmode', '--verdict', '/v.json']);
    expect(e.SUBORNED_PLAYER_DIR).toBe('out/build/full'); expect(e.SUBORNED_BATCH_OUT).toContain('out/batch/full-behind');
  });
  test('the verdict: a frame-time failure on a NOISY machine is noisy, not a finding; a real failure is a finding; a stop for the next build is neither', () => {
    const noisy = { ncpu: 10, start: [9, 5, 3] as [number, number, number], end: [8, 5, 3] as [number, number, number] }, quiet = { ncpu: 10, start: [1, 1, 1] as [number, number, number], end: [1, 1, 1] as [number, number, number] };
    const v = verdict([{ id: 'late-fight', status: 'FAILED', seconds: 300, detail: 'p99 18 ms', perf: true, load: noisy }, { id: 'tp-long', status: 'ok', seconds: 40, detail: 'fine', perf: true, load: quiet }]);
    expect(v.ok).toBe(true); expect(v.findings).toEqual([]); expect(v.noisy.map(n => n.step)).toEqual(['late-fight']); expect(v.noisy[0]!.load).toContain('NOISY');
    const w = verdict([{ id: 'late-fight', status: 'FAILED', seconds: 300, detail: 'p99 18 ms', perf: true, load: quiet }, { id: 'game-check', status: 'FAILED', seconds: 9, detail: 'game:check failed (r104)', perf: true, load: noisy, lateNoisy: false }]);
    expect(w.findings.map(f => f.step)).toEqual(['late-fight']); expect(w.ok).toBe(false);
    const s = verdict([{ id: 'shots', status: 'FAILED', seconds: 0, detail: 'stopped for the next build for Sky (now)' }], { stopped: 'stopped for the next build for Sky (now)' });
    expect(s.findings).toEqual([]); expect(s.ok).toBe(false); expect(s.stopped).toContain('next build');
  });
  test('with --verdict a frame-time step that fails on a NOISY machine does not stop the batch; without it, or quiet, or not a frame-time step, it does', async () => {
    const noisy = { ncpu: 10, start: [9, 5, 3] as [number, number, number], end: [8, 5, 3] as [number, number, number] }, quiet = { ncpu: 10, start: [1, 1, 1] as [number, number, number], end: [1, 1, 1] as [number, number, number] };
    expect(goesOn(true, true, noisy)).toBe(true); expect(goesOn(false, true, noisy)).toBe(false); expect(goesOn(true, true, quiet)).toBe(false); expect(goesOn(true, false, noisy)).toBe(false); expect(goesOn(true, true, undefined)).toBe(false);
    const st = (id: string, ok: boolean): Step => ({ id, title: id, plan: () => [], run: async () => ({ ok, detail: ok ? 'fine' : 'broke' }) }); // not a frame-time step: it stops, --verdict or not
    const c: Ctx = { dry: false, mode: 'batchmode', shotsFile: '', shotsOut: '', facts: {}, say: () => {}, goOnNoisy: true };
    expect((await runSteps([st('a', false), st('b', true)], c, ['a', 'b'])).map(r => r.status)).toEqual(['FAILED', 'not run']);
  });
  test('a batch on Sky\'s player asks a running behind batch to stop and waits for it; none running: no wait', async () => {
    const stop = join(mkdtempSync(join(tmpdir(), 'sky-')), 'stop');
    expect(await stopBehind(() => {}, { running: () => undefined })).toBe(0);
    let n = 0; const said: string[] = [];
    const w = await stopBehind(m => said.push(m), { running: () => (n++ < 2 ? { pid: 99999, at: '2026-11-11T01:00:00Z' } : undefined), sleep: async () => {}, stop });
    expect(w).toBeGreaterThanOrEqual(0); expect(said[0]).toContain('asked to stop at its next step'); expect(readFileSync(stop, 'utf8')).toContain('the next build for Sky');
    await expect(stopBehind(() => {}, { running: () => ({ pid: 99999, at: '2026-11-11T01:00:00Z' }), sleep: async () => {}, timeoutS: 0, stop })).rejects.toThrow('did not stop');
  });
});
