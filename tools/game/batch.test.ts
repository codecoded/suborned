// bun run batch (brief 034 item 3): the orchestration, with stub steps (no Unity, no build): the order, the stop at the first failure, the
// summary screen, the step picker, the shot judgement, Unity's match file, and the shot runner's install. Run: bun test tools/game
import { describe, expect, test } from 'bun:test';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, writeFileSync, utimesSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { onlyShots, timeLines, appCurrent, frameLines, gateWords, nextCommand, resumeFrom, sessionWords, type LastRun, type ProofGate, installRunner, judgeProofSets, judgeShots, logProblems, matchResult, onlyRows, pick, playerCommand, playerHasTpLong, rotateShots, runSteps, screen, STEPS, type Ctx, type Report, type Step } from './batch';
import { resolveShots, type Resolver } from './shots';

const ctx = (over: Partial<Ctx> = {}): Ctx => ({ dry: false, mode: 'batchmode', shotsFile: '', shotsOut: '', facts: {}, say: () => {}, ...over });
const stub = (id: string, ok: boolean, log: string[]): Step => ({ id, title: `title ${id}`, plan: () => [`do ${id}`], run: async () => { log.push(id); return { ok, detail: ok ? `${id} fine` : `${id} broke\nline two\nline three` }; } });
const tmp = () => mkdtempSync(join(tmpdir(), 'batch-'));

describe('the steps are the ones Sky named, in order', () => {
  test('sim, words, compile, host logic, kit, world, Unity, build, check, shots, budget, late-fight, tp-long, perf064', () => {
    const ids = STEPS.map(s => s.id);
    expect(ids).toEqual(['sim', 'words', 'compile', 'hostlogic', 'kit', 'world', 'unity-world', 'unity-match', 'game-build', 'game-check', 'shots', 'budget', 'late-fight', 'tp-long', 'perf064']);
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

describe('the load beside the perf rows (Redline R7)', () => {
  const perf = (id: string, ok = true): Step => ({ id, title: `title ${id}`, perf: true, plan: () => [`do ${id}`], run: async () => ({ ok, detail: `${id} fine` }) });
  test('a perf step carries the load at its start and end; other steps and dry runs do not', async () => {
    const rows = await runSteps([stub('a', true, []), perf('p')], ctx(), ['a', 'p']);
    expect(rows[0]!.load).toBeUndefined(); expect(rows[1]!.load!.start).toHaveLength(3); expect(rows[1]!.load!.end).toHaveLength(3); expect(rows[1]!.load!.ncpu).toBeGreaterThan(0);
    expect((await runSteps([perf('p')], ctx({ dry: true }), ['p']))[0]!.load).toBeUndefined();
  });
  test('a skipped perf step has no load row', async () => {
    const s: Step = { ...perf('p'), run: async () => ({ ok: true, skipped: true, detail: 'opt-in' }) };
    expect((await runSteps([s], ctx(), ['p']))[0]!.load).toBeUndefined();
  });
  test('the screen prints the load under the row, and says NOISY MACHINE when it was', () => {
    const calm = { id: 'tp-long', title: 't', status: 'ok' as const, seconds: 40, detail: 'ok', load: { ncpu: 18, start: [3, 4, 5] as [number, number, number], end: [4, 4.2, 5] as [number, number, number] } };
    const loud = { ...calm, id: 'shots', load: { ncpu: 18, start: [3, 4, 5] as [number, number, number], end: [14.2, 8, 5.5] as [number, number, number] } };
    const text = screen([calm, loud], { when: 'now', mode: 'batchmode', shotsOut: '/o' });
    expect(text).toContain('load 1/5/15 min 3/4/5 at start -> 4/4.2/5 at end, 18 cores\n'); expect(text).toContain('-> 14.2/8/5.5 at end, 18 cores · NOISY MACHINE');
    expect(text.match(/NOISY MACHINE/g)).toHaveLength(1);
  });
  test('the batch has a perf064 step, opt-in', async () => {
    const p = STEPS.find(s => s.id === 'perf064')!;
    expect(p.perf).toBe(true); expect(p.plan(ctx()).join('\n')).toContain('SKIPPED unless --perf064');
    expect((await runSteps([p], ctx(), ['perf064']))[0]).toMatchObject({ status: 'skipped', detail: expect.stringContaining('opt-in') });
    for (const id of ['game-check', 'shots', 'late-fight', 'tp-long']) expect(STEPS.find(s => s.id === id)!.perf).toBe(true);
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

describe('tp-long (brief 048): skipped with the reason until the player has the hook', () => {
  test('a player without the -tplong marker has no hook; with it, it has', () => {
    const d = tmp(), dll = join(d, 'Suborned.Runtime.dll');
    expect(playerHasTpLong(dll)).toBe(false); // no file
    writeFileSync(dll, Buffer.from('plain code with suborned.late.report/1', 'utf16le')); expect(playerHasTpLong(dll)).toBe(false);
    writeFileSync(dll, Buffer.concat([Buffer.from('MZ'), Buffer.from('suborned.tplong.report/1', 'utf16le')])); expect(playerHasTpLong(dll)).toBe(true);
  });
  test('a skipped step shows as skipped with its reason, does not stop the batch and is not a failure', async () => {
    const log: string[] = [];
    const skip: Step = { id: 's', title: 'ts', plan: () => [], run: async () => { log.push('s'); return { ok: true, skipped: true, detail: 'the built player has no -tplong mode yet' }; } };
    const rows = await runSteps([skip, stub('b', true, log)], ctx(), ['s', 'b']);
    expect(rows.map(r => r.status)).toEqual(['skipped', 'ok']); expect(rows[0]!.detail).toBe('skipped: the built player has no -tplong mode yet');
    const text = screen(rows, { when: 'now', mode: 'batchmode', shotsOut: '/o' });
    expect(text).toContain('skipped: the built player has no -tplong mode yet'); expect(text).toContain('BATCH OK');
  });
  test('its lines reach the screen', () => {
    const text = screen([{ id: 'tp-long', title: 't', status: 'ok', seconds: 30, detail: 'x' }], { when: 'now', mode: 'batchmode', shotsOut: '/o', tplong: ['tp-worst: mean 6 / p99 9 ms'] });
    expect(text).toContain('tp-long  tp-worst: mean 6 / p99 9 ms');
  });
});

describe('Sky is playing (playing.ts): the batch asks before each step', () => {
  test('a guard that holds back stops the batch at that step: nothing runs, the text is shown', async () => {
    const log: string[] = [], held = 'Sky was playing (pid 7) for the whole 1200 s wait';
    const rows = await runSteps([stub('a', true, log), stub('b', true, log), stub('c', true, log)], ctx({ guard: async id => (id === 'b' ? held : undefined) }), ['a', 'b', 'c']);
    expect(log).toEqual(['a']); expect(rows.map(r => r.status)).toEqual(['ok', 'FAILED', 'not run']); expect(rows[1]!.detail).toBe(held);
  });
  test('the guard is asked for every step that runs, in order, and not on a dry run or for a step that is not run', async () => {
    const asked: string[] = [], log: string[] = [], g = async (id: string) => { asked.push(id); return undefined; };
    await runSteps([stub('a', true, log), stub('b', true, log), stub('c', true, log)], ctx({ guard: g }), ['a', 'c']);
    expect(asked).toEqual(['a', 'c']);
    await runSteps([stub('a', true, log)], ctx({ guard: g, dry: true }), ['a']); expect(asked).toEqual(['a', 'c']);
  });
});

describe('the batch shots rotate in one place (brief 050)', () => {
  const run = (root: string, n: number) => { const d = join(root, 'shots'); rotateShots(d, 2, root); mkdirSync(d, { recursive: true }); writeFileSync(join(d, 'shots.json'), String(n)); };
  test('newest in shots, two older beside it, the third gone', () => {
    const root = tmp();
    for (const n of [1, 2, 3, 4]) run(root, n);
    const at = (d: string) => (existsSync(join(root, d, 'shots.json')) ? readFileSync(join(root, d, 'shots.json'), 'utf8') : '-');
    expect([at('shots'), at('shots-prev1'), at('shots-prev2'), at('shots-prev3')]).toEqual(['4', '3', '2', '-']);
  });
  test('a folder outside the batch\'s own is cleared, not kept', () => {
    const root = tmp(), elsewhere = tmp(); writeFileSync(join(elsewhere, 'old.png'), 'x');
    rotateShots(elsewhere, 2, root);
    expect(existsSync(elsewhere)).toBe(false); expect(existsSync(elsewhere + '-prev1')).toBe(false);
  });
});

// briefs/067/splice.md item 3 and follow-up 2: the proof's r059 and r067 pictures are in the HUD budget (tools/game/proofset.json), judged by the batch's budget step.
describe('the proof sets in the budget step', () => {
  const ok = (name: string) => ({ ok: true, detail: `proof set ${name}: 13 of 13 pictures within budget`, problems: [], judged: 13, pass: 13 });
  const older = (name: string) => ({ name, ok: true, judged: 0, detail: `proof set ${name}: not in this proof (an older build)` });
  test('the plan names the sets and where their pictures are', () => {
    const plan = STEPS.find(s => s.id === 'budget')!.plan(ctx({ shotsOut: '/o/shots' })).join('\n');
    expect(plan).toContain('tools/game/proofset.json'); expect(plan).toContain('r059'); expect(plan).toContain('r067'); expect(plan).toContain('out/build/play/proof'); expect(plan).toContain('1920x1080');
  });
  test('a proof with an r059 section has its set judged, at the proof folder, under the stage folder; the set whose section it lacks is an older build\'s', () => {
    const seen: string[][] = [];
    const rows = judgeProofSets({ frames: { r059: { ok: true } } }, '/p/proof', '/o/stage', (dir, name, stage) => { seen.push([dir, name, stage]); return ok(name); });
    expect(seen).toEqual([['/p/proof', 'r059', '/o/stage/r059']]);
    expect(rows).toMatchObject([{ name: 'r059', ok: true, judged: 13 }, older('r067'), older('r123')]);
  });
  test('a proof with both sections has both judged, r059 then r067, each under its own stage folder', () => {
    const seen: string[][] = [];
    const rows = judgeProofSets({ frames: { r059: { ok: true }, r067: { ok: true } } }, '/p/proof', '/o/stage', (dir, name, stage) => { seen.push([dir, name, stage]); return ok(name); });
    expect(seen).toEqual([['/p/proof', 'r059', '/o/stage/r059'], ['/p/proof', 'r067', '/o/stage/r067']]);
    expect(rows).toMatchObject([{ name: 'r059', ok: true, judged: 13 }, { name: 'r067', ok: true, judged: 13 }, older('r123')]);
  });
  test('a proof from a build with neither section is an older build: said, not judged, not failed', () => {
    let ran = false;
    const rows = judgeProofSets({ frames: { r118: { ok: true } } }, '/p/proof', '/o/stage', (_d, name) => { ran = true; return ok(name); });
    expect(ran).toBe(false);
    expect(rows).toMatchObject([older('r059'), older('r067'), older('r123')]);
    expect(judgeProofSets(undefined, '/p', '/o', () => ok('x')).every(r => r.ok)).toBe(true);
  });
  test('a set the judge fails fails its row with its problems, and only its row', () => {
    const rows = judgeProofSets({ frames: { r059: { ok: true }, r067: { ok: true } } }, '/p/proof', '/o/stage', (_d, name) => name === 'r067'
      ? { ok: false, detail: `proof set ${name}: 2 faults`, problems: ['r067-patrol-post: not taken by the proof'], judged: 0, pass: 0 } : ok(name));
    expect(rows[0]).toMatchObject({ name: 'r059', ok: true });
    expect(rows[1]).toMatchObject({ name: 'r067', ok: false, problems: ['r067-patrol-post: not taken by the proof'] });
  });
});

describe('one player launch per step (brief 042, Splice 2026-10-26)', () => {
  test('the player opens windowed at the step\'s size from its first frame; --behind goes through open -g and waits', () => {
    const d = playerCommand(['-late', 'f.json', 'out', '-novsync'], [1920, 1080], false, '/a/G.app');
    expect(d).toEqual(['/a/G.app/Contents/MacOS/The Suborned', '-screen-fullscreen', '0', '-screen-width', '1920', '-screen-height', '1080', '-late', 'f.json', 'out', '-novsync']);
    const b = playerCommand(['-tplong', 'f.json'], [2880, 1864], true, '/a/G.app');
    expect(b.slice(0, 6)).toEqual(['open', '-g', '-n', '-W', '/a/G.app', '--args']); expect(b.slice(6)).toEqual(d.slice(1, 3).concat(['-screen-width', '2880', '-screen-height', '1864', '-tplong', 'f.json']));
  });
  test('each player step names the whole list once in its plan', () => {
    for (const id of ['shots', 'late-fight', 'tp-long', 'perf064']) expect(STEPS.find(s => s.id === id)!.plan(ctx({ perf064: true })).filter(l => l.includes('MacOS/The Suborned')).length).toBe(1);
  });
  test('the screen says how many launches each player step made', () => {
    const t = screen([], { when: 'w', mode: 'batchmode', shotsOut: '/o', launches: { 'late-fight': 1, 'tp-long': 1 } });
    expect(t).toContain('players late-fight 1 launch · tp-long 1 launch');
    expect(screen([], { when: 'w', mode: 'batchmode', shotsOut: '/o', launches: {} })).not.toContain('players');
  });
  test('--rows cuts a list to the named rows in file order, and refuses a name the file lacks', () => {
    const f = { schema: 'x', rows: [{ name: 'a' }, { name: 'b' }, { name: 'c' }] };
    expect(onlyRows(f).rows.length).toBe(3);
    expect(onlyRows(f, ['c', 'a']).rows.map(r => r.name)).toEqual(['a', 'c']);
    expect(() => onlyRows(f, ['z'])).toThrow('no row z');
  });
});

describe('delta proofs in the batch (brief 078)', () => {
  const delta: ProofGate = { mode: 'delta', why: '6 of 20', ran: ['r29'], skips: { r118: 'not run, unchanged since build 0.2.146 (b00cace3)', leash: 'not run, unchanged since build 0.2.146 (b00cace3)' }, failed: [], notReached: [] };
  test('the game-check row reads what the check prints, not "driven session ?" (0.2.146\'s row)', () => {
    const log = 'game: driveSession replays headless on every checkpoint (12 checkpoints; x)\ngame: fireSession replays headless on every checkpoint (9 checkpoints; y)\n';
    expect(sessionWords(log)).toBe('driven session replays, fire session replays');
    expect(sessionWords('game: driveSession DOES NOT replay (0 checkpoints; x)\ngame: fireSession: not run, unchanged since build 0.2.146 (b00cace3)')).toBe('driven session DOES NOT replay, fire session not run');
    expect(sessionWords('')).toBe('driven session ?, fire session ?');
  });
  test('the screen names what a delta did not run, and never as passed', () => {
    const text = screen([{ id: 'game-check', title: 't', status: 'ok', seconds: 1, detail: gateWords(delta) }], { when: 'now', mode: 'batchmode', shotsOut: '/o', gate: delta });
    expect(text).toContain('proof   delta proof: 1 section ran, 2 not run, unchanged since build 0.2.146 (b00cace3)');
    expect(text).toContain('proof   not run, unchanged since build 0.2.146 (b00cace3): r118, leash');
    expect(text).not.toMatch(/r118.*(passed|\bok\b)/);
  });
  test('a failed batch prints the command that goes on from it, naming the proof sections it re-runs first', () => {
    const rows = [{ id: 'game-check', title: 'player proof', status: 'FAILED' as const, seconds: 1, detail: 'game:check failed' }];
    const text = screen(rows, { when: 'now', mode: 'batchmode', shotsOut: '/o', gate: { ...delta, failed: ['r29'] } });
    expect(text).toContain('next    bun run batch --from game-check');
    expect(text).toContain('re-runs r29 alone');
    expect(nextCommand('budget')).not.toContain('alone first');
    expect(screen([{ id: 'a', title: 't', status: 'ok', seconds: 1, detail: '' }], { when: 'now', mode: 'batchmode', shotsOut: '/o' })).not.toContain('next ');
  });
  test('a bare batch goes on from the failed step only on the same tree and the same player', () => {
    const last: LastRun = { failedStep: 'game-check', failedSections: ['r29'], build: '0.2.146', snapshot: 's1', at: 'now' };
    expect(resumeFrom(last, 's1', '0.2.146')).toBe('game-check');
    expect(resumeFrom(last, 's2', '0.2.146')).toBeUndefined();
    expect(resumeFrom(last, 's1', '0.2.147')).toBeUndefined();
    expect(resumeFrom(undefined, 's1', '0.2.146')).toBeUndefined();
  });
  test('the budget step does not judge the old pictures of a proof set the delta did not run', () => {
    let ran = false;
    const rows = judgeProofSets({ frames: { r059: null, r067: null, r123: null } }, '/p/proof', '/o/stage', (_d, name) => { ran = true; return { ok: false, detail: name, problems: ['stale'], judged: 1, pass: 0 }; }, { r059: 'not run, unchanged since build 0.2.146 (b00cace3)', r067: 'not run, unchanged since build 0.2.146 (b00cace3)', r123: 'not run, unchanged since build 0.2.146 (b00cace3)' });
    expect(ran).toBe(false); expect(rows.every(r => r.ok && r.judged === 0)).toBe(true);
    expect(rows[0]!.detail).toBe('proof set r059: not run, unchanged since build 0.2.146 (b00cace3)');
  });
  test('failing example: a proof set whose section is null with no skip is judged', () => {
    let ran = 0;
    judgeProofSets({ frames: { r059: null } }, '/p/proof', '/o/stage', (_d, name) => { ran++; return { ok: true, detail: name, problems: [], judged: 1, pass: 1 }; });
    expect(ran).toBe(1);
  });
  test('a step whose when says no is not run, says why, and the batch goes on; --only runs it anyway', async () => {
    const log: string[] = [], a = { ...stub('a', true, log), when: () => ({ run: false, why: 'engine, world and kit unchanged since build 0.2.146' }) }, b = stub('b', true, log);
    const rows = await runSteps([a, b], ctx(), ['a', 'b']);
    expect(log).toEqual(['b']); expect(rows[0]).toMatchObject({ status: 'skipped', why: 'engine, world and kit unchanged since build 0.2.146' });
    const log2: string[] = [], a2 = { ...stub('a', true, log2), when: () => ({ run: false, why: 'no' }) };
    expect((await runSteps([a2], ctx({ explicit: ['a'] }), ['a']))[0]).toMatchObject({ status: 'ok', why: 'asked for' }); expect(log2).toEqual(['a']);
  });
  test('the time budget is one line: step, seconds, why it ran or was skipped; steps not asked for are left out', () => {
    const rows: Parameters<typeof timeLines>[0] = [
      { id: 'compile', title: '', status: 'ok', seconds: 8, detail: '', why: 'always' }, { id: 'game-build', title: '', status: 'ok', seconds: 200, detail: '', why: 'proof 170 s; 7 of 20 sections' },
      { id: 'sim', title: '', status: 'skipped', seconds: 0, detail: '', why: 'unchanged' }, { id: 'tp-long', title: '', status: 'skipped', seconds: 0, detail: '', why: 'unchanged' }, { id: 'perf064', title: '', status: 'skipped', seconds: 0, detail: '' }];
    const l = timeLines(rows, 'delta');
    expect(l).toEqual(['time    208 s: compile 8 s (always) · game-build 200 s (proof 170 s; 7 of 20 sections) · sim, tp-long skipped (unchanged)']);
  });
  test('a delta over 5 minutes warns and names its slowest step; failing example: a full batch never warns', () => {
    const rows: Parameters<typeof timeLines>[0] = [{ id: 'game-build', title: '', status: 'ok', seconds: 290, detail: '', why: 'proof 250 s' }, { id: 'shots', title: '', status: 'ok', seconds: 20, detail: '' }];
    expect(timeLines(rows, 'delta')[1]).toBe('WARNING this delta took 310 s, over the 300 s target; slowest: game-build 290 s (proof 250 s)');
    expect(timeLines(rows, 'full')).toHaveLength(1);
    expect(screen(rows, { when: 'now', mode: 'batchmode', shotsOut: '/o', gate: { ...delta } })).toContain('WARNING this delta took 310 s');
  });
  test('only the shots the change can affect; failing example: a name the file lacks is an error', () => {
    const raw = { shots: [{ name: 'a' }, { name: 'b' }, { name: 'c' }] };
    expect(onlyShots(raw, ['c', 'a']).shots.map(s => s.name)).toEqual(['a', 'c']);
    expect(onlyShots(raw, 'all')).toBe(raw); expect(onlyShots(raw)).toBe(raw);
    expect(() => onlyShots(raw, ['z'])).toThrow('shots.json lacks: z');
  });
});
