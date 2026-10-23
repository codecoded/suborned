// The late-fight check (brief 045, Splice): late.json resolves, the window is the last 60 s of the engine's mission, and the verdict fails on exactly the three
// things the brief names (a frame over 50 ms, p99 over 16.7 ms, over 100 ms at the end) plus a drive that did not reach the engine's end. No Unity, no engine. Run: bun test tools/game
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { FREEZE_MS, LIMITS, findLate, judgeLate, noisy, resolveLate, runLate, scriptPath, type End, type LateReport, type LateReportRow, type LateResolvedFile, type LoadSample } from './late';
import { ROOT } from './shots';

const FILE = join(ROOT, 'tools/game/late.json'), raw = JSON.parse(readFileSync(FILE, 'utf8'));
const ends: Record<string, End> = { 'loud-fight.json': { tick: 3124, outcome: 'success', fail: '' }, 'sky-0.2.58-session1.script.json': { tick: 4232, outcome: 'failure', fail: 'rivals-reach-vale' } };
const endOf = (p: string) => ends[p.split('/').pop()!] ?? (() => { throw new Error(`no end for ${p}`); })();

describe('tools/game/late.json', () => {
  test('names loud-fight and Sky\'s session 1, which exist', () => {
    expect(raw.rows.map((r: { name: string }) => r.name)).toEqual(['loud-fight', 'sky-session1']);
    for (const r of raw.rows) expect(readFileSync(scriptPath(r.script), 'utf8')).toContain('suborned.sim.script/1');
  });
  test('Sky\'s recorded end is what its script says it reaches (the engine\'s word, from the script\'s own expect)', () => {
    const j = JSON.parse(readFileSync(scriptPath('briefs/045/runs/sky-0.2.58-session1.script.json'), 'utf8'));
    expect(j.expect.checkpoints.at(-1)[0]).toBe(ends['sky-0.2.58-session1.script.json']!.tick);
    expect(j.expect.outcome).toBe('failure');
  });
  test('resolves to the last 60 s at 1080p, warmed 3 s before', () => {
    const r = resolveLate(raw, '/o', endOf);
    expect(r.schema).toBe('suborned.late.resolved/1');
    expect(r.rows[0]).toMatchObject({ name: 'loud-fight', endTick: 3124, startTick: 2524, warmTick: 2494, outcome: 'success', tailSeconds: 2, w: 1920, h: 1080 });
    expect(r.rows[1]).toMatchObject({ name: 'sky-session1', endTick: 4232, startTick: 3632, warmTick: 3602, outcome: 'failure', failCode: 'rivals-reach-vale' });
    expect(r.rows[0]!.scriptPath).toMatch(/^\/.*routes\/loud-fight\.json$/);
  });
  test('a bad file, a repeated name, a mission shorter than the window: refused with the row\'s name', () => {
    expect(() => resolveLate({ schema: 'suborned.late/1', rows: [{ name: 'A b', script: 'loud-fight' }] }, '/o', endOf)).toThrow(/kebab-case/);
    expect(() => resolveLate({ schema: 'suborned.late/1', rows: [{ name: 'a', script: 'loud-fight' }, { name: 'a', script: 'loud-fight' }] }, '/o', endOf)).toThrow(/used twice/);
    expect(() => resolveLate({ schema: 'suborned.late/1', rows: [{ name: 'a', script: 'loud-fight', seconds: 300 }] }, '/o', () => ({ tick: 2000, outcome: 'success', fail: '' }))).toThrow(/before a 300 s window/);
    expect(() => resolveLate({ schema: 'suborned.late/1', rows: [{ name: 'a', script: 'nope-nope' }] }, '/o', endOf)).toThrow(/a: no end/);
  });
});

describe('the verdict', () => {
  const resolved = resolveLate(raw, '/o', endOf);
  const row = (name: string, over: Partial<LateReportRow> = {}): LateReportRow => {
    const r = resolved.rows.find(x => x.name === name)!;
    return { name, status: 'ok', endTick: r.endTick, outcome: r.outcome, ordersGiven: 40, window: { count: 7000, meanMs: 8, p99Ms: 12, maxMs: 20, over50: 0 }, tail: { count: 240, maxMs: 30, over50: 0, over100: 0 }, ...over };
  };
  const rep = (...rows: LateReportRow[]): LateReport => ({ schema: 'suborned.late.report/1', status: 'ok', screen: '1920x1080', rows });
  const both = (a: Partial<LateReportRow> = {}, b: Partial<LateReportRow> = {}) => judgeLate(resolved, rep(row('loud-fight', a), row('sky-session1', b)));
  test('the limits are the brief\'s: 50 ms, 16.7 ms, 100 ms', () => expect(LIMITS).toEqual({ maxFrameMs: 50, p99Ms: 16.7, endFrameMs: 100 }));
  test('smooth frames and an end under 100 ms pass', () => { const j = both(); expect(j.problems).toEqual([]); expect(j.ok).toBe(true); expect(j.lines[0]).toContain('worst 30 ms'); });
  test('one frame over 50 ms fails, however good p99 is (a save stall on an order)', () => {
    const j = both({ window: { count: 7000, meanMs: 8, p99Ms: 12, maxMs: 273, over50: 1 } });
    expect(j.ok).toBe(false); expect(j.problems.join('\n')).toContain('loud-fight: a frame took 273 ms (a freeze is 250 ms or more');
  });
  test('p99 over 16.7 ms fails', () => expect(both({}, { window: { count: 7000, meanMs: 12, p99Ms: 16.8, maxMs: 40, over50: 0 } }).problems).toEqual([expect.stringContaining('sky-session1: p99 16.8 ms (limit 16.7)')]));
  test('the end of the mission: 50 to 100 ms is a stall (over 50); over 100 ms is named as the end', () => {
    expect(both({}, { tail: { count: 240, maxMs: 80, over50: 1, over100: 0 } }).problems).toEqual([expect.stringContaining('1 stray frame(s) over 50 ms')]);
    const j = both({ tail: { count: 240, maxMs: 1900, over50: 1, over100: 1 } });
    expect(j.problems.join('\n')).toContain('1900 ms at the end of the mission (limit 100)');
    expect(j.problems.join('\n')).toContain('a frame took 1900 ms');
  });
  test('a run that ended elsewhere, a row not run, a failed row, no report: each named', () => {
    expect(both({ endTick: 3000 }).problems[0]).toContain('ended at tick 3000 (success), the engine\'s replay ends at 3124 (success): the drive diverged');
    expect(both({}, { outcome: 'success' }).problems[0]).toContain('sky-session1: ended at tick 4232 (success)');
    expect(judgeLate(resolved, rep(row('loud-fight'))).problems).toEqual(['sky-session1: not run (the player stopped before it)']);
    expect(both({ status: 'failed', error: 'no host' }).problems).toEqual(['loud-fight: no host']);
    expect(judgeLate(resolved, undefined).ok).toBe(false);
  });
  test('too few frames timed is not a pass', () => expect(both({ window: { count: 20, meanMs: 8, p99Ms: 9, maxMs: 9, over50: 0 } }).problems[0]).toContain('only 20 frames timed'));
});

// ---- the row under load: stray frames are noted, event-tied and freeze frames recur or do not, the load is recorded (synthetic reports; the numbers are the real 0.2.65 run's)
describe('runLate: a rerun, and what it fails on', () => {
  const resolved = resolveLate(raw, '/o', endOf);
  const base = (name: string, over: Partial<LateReportRow> = {}): LateReportRow => {
    const r = resolved.rows.find(x => x.name === name)!;
    return { name, status: 'ok', endTick: r.endTick, outcome: r.outcome, ordersGiven: 40, window: { count: 13000, meanMs: 4.6, p99Ms: 6.5, maxMs: 16, over50: 0 }, tail: { count: 500, maxMs: 19, over50: 0, over100: 0 }, worst: [{ ms: 19, tick: 3124, what: 'order+save+end' }], ...over };
  };
  const rep = (...rows: LateReportRow[]): LateReport => ({ schema: 'suborned.late.report/1', status: 'ok', screen: '1920x1080', rows });
  const calm: LoadSample = { ncpu: 18, start: 3, peak: 5, end: 4, top: [] }, loud: LoadSample = { ncpu: 18, start: 12, peak: 15, end: 14, top: ['ffmpeg 640%'] };
  /** a player whose answer for (rows, attempt n) is given; records which rows it was asked for */
  const player = (answer: (names: string[], n: number) => LateReportRow[], load = calm) => {
    const asked: string[][] = [];
    return { asked, play: async (r: LateResolvedFile, n: number) => { asked.push(r.rows.map(x => x.name)); return { report: rep(...answer(r.rows.map(x => x.name), n)), load }; } };
  };
  const clean = (names: string[]) => names.map(n => base(n));
  const stray = (n: string): LateReportRow => base(n, { window: { count: 13000, meanMs: 4.6, p99Ms: 6.5, maxMs: 97, over50: 1 }, worst: [{ ms: 97, tick: 3300, what: '' }, { ms: 19, tick: 3124, what: 'order+save+end' }] });
  const saveStall = (n: string, ms = 150): LateReportRow => base(n, { window: { count: 13000, meanMs: 4.6, p99Ms: 6.5, maxMs: ms, over50: 4 }, worst: [1, 2, 3, 4].map(i => ({ ms: ms - i * 10, tick: 3000 + i * 50, what: 'order+save' })) });

  test('a clean run is one run', async () => {
    const p = player(clean), v = await runLate(resolved, p.play);
    expect(v.ok).toBe(true); expect(p.asked).toEqual([['loud-fight', 'sky-session1']]); expect(v.noisy).toBe(false);
  });
  test('a lone stray over 50 ms with no event in it is noted, never failed, and costs no rerun', async () => {
    const p = player(names => names.map(n => n === 'loud-fight' ? stray(n) : base(n))), v = await runLate(resolved, p.play);
    expect(v.ok).toBe(true); expect(p.asked.length).toBe(1);
    expect(v.notes.join('\n')).toContain('loud-fight: run 1 had 1 stray frame(s) over 50 ms with no save, order or end (97 ms)');
  });
  test('a save stall that does not come back on the rerun is noise: passes, says so, reruns only that row', async () => {
    const p = player((names, n) => names.map(x => x === 'sky-session1' && n === 1 ? saveStall(x, 63) : base(x))), v = await runLate(resolved, p.play);
    expect(v.ok).toBe(true); expect(p.asked).toEqual([['loud-fight', 'sky-session1'], ['sky-session1']]);
    expect(v.notes.join('\n')).toContain('sky-session1: first run saw event');
    expect(v.notes.join('\n')).toContain('the rerun did not repeat it: not counted');
  });
  test('the old per-save replay (68-273 ms on saves) recurs on the rerun: FAILS, naming the row, both runs and the frames', async () => {
    const p = player((names, n) => names.map(x => x === 'sky-session1' ? saveStall(x, 273 - (n - 1) * 20) : base(x))), v = await runLate(resolved, p.play);
    expect(v.ok).toBe(false); expect(p.asked.length).toBe(2);
    const text = v.problems.join('\n');
    expect(text).toContain('sky-session1: 4 frames over 50 ms held a save, order or the end'); expect(text).toContain('seen on both runs');
    expect(text).toContain('a frame took 273 ms'); expect(v.problems.every(x => x.startsWith('sky-session1:'))).toBe(true);
  });
  test('the 1.9 s end freeze recurs: fails as an end and as a freeze; one that does not recur is noise', async () => {
    const frozen = (n: string) => base(n, { tail: { count: 300, maxMs: 1900, over50: 1, over100: 1 }, worst: [{ ms: 1900, tick: 4232, what: 'order+save+end' }] });
    const bad = await runLate(resolved, player(names => names.map(n => n === 'sky-session1' ? frozen(n) : base(n))).play);
    expect(bad.ok).toBe(false); expect(bad.problems.join('\n')).toContain('1900 ms at the end of the mission'); expect(bad.problems.join('\n')).toContain('a frame took 1900 ms');
    const once = await runLate(resolved, player((names, k) => names.map(n => n === 'sky-session1' && k === 1 ? frozen(n) : base(n))).play);
    expect(once.ok).toBe(true);
  });
  test('an event frame on one run and a stall with no event on the other: a freeze recurs as a freeze', async () => {
    const hang = (n: string) => base(n, { window: { count: 13000, meanMs: 4.6, p99Ms: 6.5, maxMs: 400, over50: 1 }, worst: [{ ms: 400, tick: 3300, what: '' }] });
    const v = await runLate(resolved, player(names => names.map(n => n === 'loud-fight' ? hang(n) : base(n))).play);
    expect(v.ok).toBe(false); expect(v.problems.join('\n')).toContain(`loud-fight: a frame took 400 ms (a freeze is ${FREEZE_MS} ms or more`);
  });
  test('three or more strays that come back on both runs are a hitch, not load', async () => {
    const hitchy = (n: string) => base(n, { window: { count: 13000, meanMs: 4.6, p99Ms: 6.5, maxMs: 80, over50: 5 }, worst: [60, 61, 62, 63, 80].map((ms, i) => ({ ms, tick: 3100 + i, what: '' })) });
    const v = await runLate(resolved, player(names => names.map(n => n === 'loud-fight' ? hitchy(n) : base(n))).play);
    expect(v.ok).toBe(false); expect(v.problems.join('\n')).toContain('5 frames over 50 ms with no save, order or end');
  });
  test('p99 over the limit must come back too; a drive that diverges, or a rerun with no report, fails', async () => {
    const slowP99 = (n: string) => base(n, { window: { count: 13000, meanMs: 12, p99Ms: 18, maxMs: 30, over50: 0 } });
    expect((await runLate(resolved, player((names, k) => names.map(n => n === 'loud-fight' && k === 1 ? slowP99(n) : base(n))).play)).ok).toBe(true);
    expect((await runLate(resolved, player(names => names.map(n => n === 'loud-fight' ? slowP99(n) : base(n))).play)).problems.join('\n')).toContain('loud-fight: p99 18 ms');
    expect((await runLate(resolved, player(names => names.map(n => base(n, { endTick: 3000 }))).play)).ok).toBe(false);
    let calls = 0; const v = await runLate(resolved, async () => ({ report: calls++ ? undefined : rep(stray('loud-fight'), base('sky-session1', { endTick: 1 })), load: calm }));
    expect(v.ok).toBe(false);
  });
  test('machine load is recorded for every run and a high one is flagged "noisy machine"', async () => {
    expect(noisy(calm)).toBe(false); expect(noisy(loud)).toBe(true);
    const v = await runLate(resolved, player((names, n) => names.map(x => n === 1 ? saveStall(x, 63) : base(x)), loud).play);
    expect(v.noisy).toBe(true); expect(v.attempts.length).toBe(2); expect(v.attempts.every(a => a.load?.peak === 15)).toBe(true);
    expect(v.notes.join('\n')).toContain('NOISY MACHINE'); expect(v.notes.join('\n')).toContain('ffmpeg 640%'); expect(v.notes.join('\n')).toContain('and the machine was noisy');
  });
  test('on a noisy machine a p99 or a crowd of strays is noted, not judged; a save stall or a freeze still fails', async () => {
    const hitchy = (n: string) => base(n, { window: { count: 9000, meanMs: 8, p99Ms: 23.5, maxMs: 83, over50: 31 }, worst: [83, 77, 75, 73, 73].map((ms, i) => ({ ms, tick: 2700 + i, what: '' })) });
    const p = player(names => names.map(n => hitchy(n)), loud), v = await runLate(resolved, p.play);
    expect(v.ok).toBe(true); expect(p.asked.length).toBe(1); expect(v.noisy).toBe(true);
    expect(v.notes.join('\n')).toContain('p99 23.5 ms (limit 16.7) (run 1): NOISY MACHINE, not judged');
    const bad = await runLate(resolved, player(names => names.map(n => n === 'loud-fight' ? saveStall(n, 120) : hitchy(n)), loud).play);
    expect(bad.ok).toBe(false); expect(bad.problems.join('\n')).toContain('loud-fight: 4 frames over 50 ms held a save');
  });
  test('findLate attributes a frame by what the host says it held', () => {
    const f = findLate(resolved, rep(saveStall('loud-fight', 90), base('sky-session1')));
    expect(f.findings.map(x => x.klass)).toEqual(['event']); expect(f.findings[0]!.msg).toContain('80 ms at tick');
  });
});
