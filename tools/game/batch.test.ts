// bun run batch (brief 034 item 3): the orchestration, with stub steps (no Unity, no build): the order, the stop at the first failure, the
// summary screen, the step picker, the shot judgement, Unity's match file, and the shot runner's install. Run: bun test tools/game
import { describe, expect, test } from 'bun:test';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, writeFileSync, utimesSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { appCurrent, frameLines, installRunner, judgeShots, logProblems, matchResult, pick, runSteps, screen, STEPS, type Ctx, type Report, type Step } from './batch';
import { resolveShots, type Resolver } from './shots';

const ctx = (over: Partial<Ctx> = {}): Ctx => ({ dry: false, mode: 'batchmode', shotsFile: '', shotsOut: '', facts: {}, say: () => {}, ...over });
const stub = (id: string, ok: boolean, log: string[]): Step => ({ id, title: `title ${id}`, plan: () => [`do ${id}`], run: async () => { log.push(id); return { ok, detail: ok ? `${id} fine` : `${id} broke\nline two\nline three` }; } });
const tmp = () => mkdtempSync(join(tmpdir(), 'batch-'));

describe('the steps are the ones Sky named, in order', () => {
  test('sim, kit, world, Unity, build, check, shots, budget', () => {
    const ids = STEPS.map(s => s.id);
    expect(ids).toEqual(['sim', 'compile', 'kit', 'world', 'unity-world', 'unity-match', 'game-build', 'game-check', 'shots', 'budget']);
    expect(STEPS.find(s => s.id === 'sim')!.plan(ctx())).toEqual(['bun tools/sim/unity.ts --tree']);
    expect(STEPS.find(s => s.id === 'game-build')!.plan(ctx({ rebuild: true }))[0]).toContain('bun tools/game/cli.ts --build');
  });
  test('Unity is driven through its menus with the Editor open and by batchmode otherwise', () => {
    const w = STEPS.find(s => s.id === 'unity-world')!;
    expect(w.plan(ctx({ mode: 'editor' })).join('\n')).toContain('Suborned/World/Build Harbour District (Export)');
    expect(w.plan(ctx({ mode: 'batchmode' })).join('\n')).toMatch(/-batchmode .*HarbourWorldBuilder\.Build -world out\/world\/harbour-district\.world\.json/);
    expect(STEPS.find(s => s.id === 'unity-match')!.plan(ctx({ mode: 'batchmode' })).join('\n')).toContain('SimScenarios.RunBatch');
  });
});

describe('running them', () => {
  test('all pass: every row ok, in order', async () => {
    const log: string[] = [], rows = await runSteps([stub('a', true, log), stub('b', true, log)], ctx(), ['a', 'b']);
    expect(log).toEqual(['a', 'b']); expect(rows.map(r => r.status)).toEqual(['ok', 'ok']);
  });
  test('a failing step stops the batch: later steps do not run, and say so', async () => {
    const log: string[] = [], rows = await runSteps([stub('a', true, log), stub('b', false, log), stub('c', true, log)], ctx(), ['a', 'b', 'c']);
    expect(log).toEqual(['a', 'b']); expect(rows.map(r => r.status)).toEqual(['ok', 'FAILED', 'not run']);
    const text = screen(rows, { when: 'now', mode: 'batchmode', shotsOut: '/o' });
    expect(text).toContain('STOPPED at b: title b'); expect(text).toContain('line two'); expect(text).toContain('out/batch/logs/b.log'); expect(text).not.toContain('BATCH OK');
  });
  test('a step that throws is a failure, not a crash', async () => {
    const boom: Step = { id: 'x', title: 't', plan: () => [], run: async () => { throw new Error('no dotnet'); } };
    const rows = await runSteps([boom], ctx(), ['x']);
    expect(rows[0]).toMatchObject({ status: 'FAILED', detail: 'threw: no dotnet' });
  });
  test('dry run plans every step and runs none', async () => {
    const log: string[] = [], rows = await runSteps([stub('a', true, log), stub('b', true, log)], ctx({ dry: true }), ['a', 'b']);
    expect(log).toEqual([]); expect(rows.map(r => r.detail)).toEqual(['do a', 'do b']);
  });
  test('--from, --only, --skip, and a name that is not a step', () => {
    const ids = ['a', 'b', 'c', 'd'];
    expect(pick(ids, { from: 'c' }).run).toEqual(['c', 'd']);
    expect(pick(ids, { only: ['b', 'd'] }).run).toEqual(['b', 'd']);
    expect(pick(ids, { skip: ['a'], from: 'b' }).run).toEqual(['b', 'c', 'd']);
    expect(pick(ids, { only: ['zzz'] }).unknown).toEqual(['zzz']);
  });
  test('the one-screen summary: ok line, build, frame times', () => {
    const rows = [{ id: 'sim', title: 't', status: 'ok' as const, seconds: 4.2, detail: 'headless 28/28' }, { id: 'kit', title: 't', status: 'skipped' as const, seconds: 0, detail: '' }];
    const proof = { frames: { fps: 143.2, p99Ms: 9.1, byYaw: [{ yaw: 30, fps: 140, p99Ms: 9 }], follow: [{ spot: 'cafe', fps: 96.4, p99Ms: 12.3, gpuMs: 7.7 }] } };
    const text = screen(rows, { when: '2026-10-21 14:00', mode: 'editor', build: { version: '0.2.18', recording: 'c026', sha: 'abc', dirty: true }, proof, shotsOut: '/o' });
    expect(text).toContain('BATCH OK: 1 steps'); expect(text).toContain('0.2.18 (c026, abc+)'); expect(text).toContain('slowest of 2 spots: cafe 96.4 fps (p99 12.3 ms, gpu 7.7 ms)'); expect(text).toContain('Editor open');
    expect(frameLines(undefined, undefined)).toEqual([]);
  });
});

describe('the shots step reads what the player left', () => {
  const R: Resolver = { script: () => undefined, checkpoint: () => false, scenario: () => undefined };
  const resolved = resolveShots({ schema: 'suborned.shots/1', shots: [{ name: 'a' }, { name: 'b', hud: false }, { name: 'c', clip: { seconds: 1 } }] }, '/o', R);
  const dir = tmp();
  const rep = (...s: Partial<Report['shots'][0]>[]): Report => ({ status: 'ok', screen: '1920x1080', shots: s.map(x => ({ name: '', status: 'ok', error: '', files: [], tick: 0, ...x })) });
  const touch = (...f: string[]) => { for (const x of f) { mkdirSync(join(dir, x, '..'), { recursive: true }); writeFileSync(join(dir, x), 'x'); } };
  test('every file in place: ok, counted', () => {
    touch('a.png', 'a.hud.png', 'a.manifest.json', 'b.jpg', 'c/f0000.jpg');
    const r = judgeShots(resolved, rep({ name: 'a' }, { name: 'b' }, { name: 'c' }), dir, 12);
    expect(r).toMatchObject({ ok: true }); expect(r.detail).toContain('3 shots (1 with HUD layers, 1 clip)');
  });
  test('a PNG that is not the size the row says is named (a native-resolution capture would be 6016x3384)', () => {
    const png = (w: number, h: number) => { const b = new Uint8Array(32); b.set([0x89, 0x50, 0x4e, 0x47, 13, 10, 26, 10, 0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52]); const v = new DataView(b.buffer); v.setUint32(16, w); v.setUint32(20, h); return b; };
    const d = tmp(); mkdirSync(join(d, 'c')); writeFileSync(join(d, 'a.png'), png(6016, 3384)); writeFileSync(join(d, 'a.hud.png'), png(1920, 1080)); writeFileSync(join(d, 'a.manifest.json'), '{}'); writeFileSync(join(d, 'b.jpg'), 'x'); writeFileSync(join(d, 'c/f0000.jpg'), 'x');
    const r = judgeShots(resolved, rep({ name: 'a' }, { name: 'b' }, { name: 'c' }), d, 1);
    expect(r.ok).toBe(false); expect(r.detail).toContain('a.png is 6016x3384, the row says 1920x1080'); expect(r.detail).not.toContain('a.hud.png');
    writeFileSync(join(d, 'a.png'), png(1920, 1080)); expect(judgeShots(resolved, rep({ name: 'a' }, { name: 'b' }, { name: 'c' }), d, 1).ok).toBe(true);
  });
  test('a failed shot, a shot never taken and a missing file are each named', () => {
    const r = judgeShots(resolved, rep({ name: 'a', status: 'failed', error: 'InvalidOperationException: no view waitress' }, { name: 'b' }), dir, 1);
    expect(r.ok).toBe(false); expect(r.detail).toContain('a: InvalidOperationException: no view waitress'); expect(r.detail).toContain('c: not taken');
    expect(judgeShots(resolved, rep({ name: 'a' }, { name: 'b' }, { name: 'c' }), mkdtempSync(join(tmpdir(), 'empty-')), 1).detail).toContain('a.png is missing');
  });
});

describe("Unity's match file", () => {
  const write = (o: object) => { const f = join(tmp(), 'unity-editor.json'); writeFileSync(f, JSON.stringify(o)); return f; };
  test('a passing match', () => { expect(matchResult(write({ passed: true, matching: 150, scenarios: 150, runtime: 'Mono', dllSha256: 'a'.repeat(64) }), 0)).toMatchObject({ ok: true, detail: expect.stringContaining('150/150') }); });
  test('a scenario that differs from headless is named', () => {
    const r = matchResult(write({ passed: false, matching: 149, scenarios: 150, rows: [{ id: 'N10', matchesHeadless: false, replayIdentical: true, why: 'checkpoint 3 differs' }] }), 0);
    expect(r.ok).toBe(false); expect(r.detail).toContain('149/150'); expect(r.detail).toContain('N10: checkpoint 3 differs');
  });
  test('a file older than the step, or an error from the player, fails', () => {
    const f = write({ passed: true, matching: 1, scenarios: 1 }); utimesSync(f, new Date(2020, 1, 1), new Date(2020, 1, 1));
    expect(matchResult(f, Date.now()).detail).toContain('older than this step');
    expect(matchResult(write({ error: 'InvalidDataException: Sim: 2 Suborned.Sim.dll plugins' }), 0).detail).toContain('2 Suborned.Sim.dll plugins');
  });
});

test('the Editor log: only what went wrong', () => {
  expect(logProblems('World: built 12000 nodes\nAssets/X.cs(3,4): error CS1061: nope\nArgumentException: bad\nfine')).toEqual(['Assets/X.cs(3,4): error CS1061: nope', 'ArgumentException: bad']);
  expect(logProblems('all quiet')).toEqual([]);
});

describe('the shot runner is moved into the Unity project once', () => {
  test('staged → installed; already installed stays; neither fails', () => {
    const d = tmp(), staged = join(d, 'staged.cs'), installed = join(d, 'installed.cs');
    writeFileSync(staged, 'class A {}');
    expect(installRunner(staged, installed)).toContain('installed now'); expect(readFileSync(installed, 'utf8')).toBe('class A {}'); expect(existsSync(staged)).toBe(false);
    expect(installRunner(staged, installed)).toBe('installed');
    expect(() => installRunner(staged, join(d, 'nope.cs'))).toThrow(/neither/);
  });
});

describe('the player is not rebuilt, and no number spent, when nothing it is made from is newer', () => {
  const setup = () => {
    const d = tmp(), app = join(d, 'The Suborned.app'), stamp = join(d, 'build.json'), assets = join(d, 'Assets');
    mkdirSync(app); mkdirSync(assets); writeFileSync(join(assets, 'a.cs'), 'x'); writeFileSync(stamp, JSON.stringify({ result: 'Succeeded', version: '0.2.18' }));
    const at = (f: string, t: number) => utimesSync(f, new Date(t), new Date(t));
    return { app, stamp, roots: [assets], file: join(assets, 'a.cs'), at };
  };
  test('current: says so, naming the build', () => {
    const s = setup(); s.at(s.file, Date.now() - 60000); s.at(s.stamp, Date.now() - 1000);
    expect(appCurrent(s)).toContain('0.2.18 is newer than every file');
  });
  test('a script newer than the build, a missing app, or a failed build: needs building', () => {
    const s = setup(); s.at(s.stamp, Date.now() - 60000); s.at(s.file, Date.now() - 1000);
    expect(appCurrent(s)).toBeUndefined();
    expect(appCurrent({ ...s, app: join(tmpdir(), 'no-such.app') })).toBeUndefined();
    writeFileSync(s.stamp, JSON.stringify({ result: 'Failed' })); s.at(s.file, Date.now() - 120000); s.at(s.stamp, Date.now());
    expect(appCurrent(s)).toBeUndefined();
  });
  test('the plan says when it skips, --rebuild does not skip, and the screen says NOT REBUILT', () => {
    const step = STEPS.find(x => x.id === 'game-build')!;
    expect(step.plan(ctx())[0]).toContain('skipped when nothing');
    expect(step.plan(ctx({ rebuild: true }))[0]).toContain('--rebuild');
    const text = screen([{ id: 'game-build', title: 't', status: 'ok', seconds: 0, detail: 'NOT REBUILT' }], { when: 'n', mode: 'batchmode', shotsOut: '/o', skipped: '0.2.18 is newer than every file' });
    expect(text).toContain('build   NOT REBUILT: 0.2.18');
  });
});

test('frame times are labelled with the size they were taken at (the Air-sized run), and the pictures\' size is not what they are', () => {
  const proof = { screen: '1920x1080', framesAt: '2880x1864', framesNote: 'at 2880x1864: an Air-sized proxy on Mac14,13, not the Air\'s own GPU', frames: { fps: 96.2, p99Ms: 12.1 } };
  expect(frameLines(proof, undefined)[0]).toBe('play zoom 96.2 fps mean (p99 12.1 ms) at 2880x1864: an Air-sized proxy on Mac14,13, not the Air\'s own GPU');
  expect(frameLines({ screen: '1920x1080', frames: { fps: 150, p99Ms: 8 } }, undefined)[0]).toContain('at 1920x1080');
});
