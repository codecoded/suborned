// The tp-long row (tplong.ts, brief 048): the file, what a row resolves to, and the gates. Run: bun test tools/game
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { GATES, judgeTpLong, resolveTpLong, type TpLongReport, type TpLongReportRow, type TpLongResolvedFile } from './tplong';
import { ROOT } from './shots';

const file = () => JSON.parse(readFileSync(join(ROOT, 'tools/game/tplong.json'), 'utf8'));

describe('tools/game/tplong.json', () => {
  test('is Redline\'s three views at the Air\'s size, 1.5 s settle and a 6 s window', () => {
    const r = resolveTpLong(file(), '/o');
    expect(r.rows.map(x => x.name)).toEqual(['play-opening', 'tp-street', 'tp-worst']);
    expect(r.rows.map(x => x.kind)).toEqual(['play', 'third', 'third']);
    for (const x of r.rows) expect(x).toMatchObject({ w: 2880, h: 1864, settleSeconds: 1.5, seconds: 6, clockRunning: true, hud: true, vsync: false });
    expect(r.rows[1]).toMatchObject({ agent: 'scout', walk: [0, -20], faceHeading: 180 });
    expect(r.rows[2]).toMatchObject({ walk: [-10, -25], faceHeading: 90 });
    expect(r.rows[0]).toMatchObject({ yaw: 30, zoom: 14, faceHeading: -1 });
    expect(r.rows.reduce((a, x) => a + x.settleSeconds + x.seconds, 0)).toBeLessThan(40);
  });
  test('says which row is wrong and why', () => {
    const f = file();
    expect(() => resolveTpLong({ ...f, rows: [{ name: 'a', view: 'follow' }] }, '/o')).toThrow('a: a follow view needs faceHeading');
    expect(() => resolveTpLong({ ...f, rows: [{ name: 'a', view: 'overhead', faceHeading: 90 }] }, '/o')).toThrow('go with the follow view');
    expect(() => resolveTpLong({ ...f, rows: [{ name: 'a', view: 'follow', faceHeading: 90, yaw: 30 }] }, '/o')).toThrow('go with the overhead');
    expect(() => resolveTpLong({ ...f, rows: [{ name: 'A b', view: 'overhead' }] }, '/o')).toThrow('kebab-case');
    expect(() => resolveTpLong({ ...f, rows: [{ name: 'a', view: 'overhead' }, { name: 'a', view: 'overhead' }] }, '/o')).toThrow('used twice');
    expect(() => resolveTpLong({ ...f, rows: [{ name: 'a', view: 'overhead', extra: 1 }] }, '/o')).toThrow();
  });
});

const resolved: TpLongResolvedFile = resolveTpLong(file(), '/o');
const good = (name: string): TpLongReportRow => {
  const r = resolved.rows.find(x => x.name === name)!, third = r.kind === 'third';
  return {
    name, status: 'ok', screen: [2880, 1864], faceHeading: third ? r.faceHeading : undefined, frames: { count: 800, meanMs: third ? 6 : 4.5, p99Ms: third ? 9 : 6, maxMs: 14 },
    gpuMs: 4, mainMs: 5, renderMs: 2, counters: { triangles: third ? 5_000_000 : 1_700_000, setPass: third ? 200 : 105, shadowCasters: third ? 1000 : 600 }, drawn: { renderers: third ? 2400 : 370, beyond150: third ? 700 : 226 },
  };
};
const report = (...rows: TpLongReportRow[]): TpLongReport => ({ schema: 'suborned.tplong.report/1', status: 'ok', rows });
const all = () => report(good('play-opening'), good('tp-street'), good('tp-worst'));

describe('the gates', () => {
  test('are the ones in brief 048', () => {
    expect(GATES.third).toEqual({ triangles: 6e6, setPass: 375, shadowCasters: 1200, drawn: 2500, beyond150: 1500, meanMs: 8.3, p99Ms: 16.7 });
    expect(GATES.play).toEqual({ triangles: 2.5e6, meanMs: 6 });
  });
  test('a build inside every gate passes', () => {
    const v = judgeTpLong(resolved, all()); expect(v.problems).toEqual([]); expect(v.ok).toBe(true); expect(v.lines.length).toBe(3);
  });
  test('today\'s tp-worst (Redline\'s numbers) fails every count, and says each', () => {
    const w = good('tp-worst'); w.counters = { triangles: 17.5e6, setPass: 426, shadowCasters: 3446 }; w.drawn = { renderers: 6081, beyond150: 4377 }; w.frames.meanMs = 8.3;
    const v = judgeTpLong(resolved, report(good('play-opening'), good('tp-street'), w));
    expect(v.ok).toBe(false); expect(v.problems.length).toBe(1);
    for (const t of ['triangles 17.5 M (limit 6 M)', 'SetPass 426 (limit 375)', 'shadow casters 3,446 (limit 1,200)', 'renderers drawn 6,081 (limit 2,500)', 'beyond 150 m 4,377 (limit 1,500)']) expect(v.problems[0]).toContain(t);
    expect(v.problems[0]).toMatch(/^tp-worst:/);
  });
  test('the play view is held to its triangles and its mean only', () => {
    const p = good('play-opening'); p.counters.triangles = 2.6e6; p.counters.setPass = 900; p.frames.meanMs = 6.5; p.frames.p99Ms = 30;
    const v = judgeTpLong(resolved, report(p, good('tp-street'), good('tp-worst')));
    const t = v.problems.join('\n'); expect(v.problems.length).toBe(2); expect(t).toContain('triangles 2.6 M (limit 2.5 M)'); expect(t).toContain('mean 6.5 ms (limit 6)'); expect(t).not.toContain('SetPass'); expect(t).not.toContain('p99');
  });
  test('frame times: failed on a quiet machine, noted on a noisy one; counts are judged either way', () => {
    const s = good('tp-street'); s.frames.meanMs = 9; s.frames.p99Ms = 20;
    const quiet = judgeTpLong(resolved, report(good('play-opening'), s, good('tp-worst')), { ncpu: 18, start: 3, peak: 5, end: 4, top: [] });
    expect(quiet.ok).toBe(false); expect(quiet.problems.join('\n')).toContain('mean 9 ms'); expect(quiet.problems.join('\n')).toContain('p99 20 ms');
    const noisy = judgeTpLong(resolved, report(good('play-opening'), s, good('tp-worst')), { ncpu: 18, start: 3, peak: 14, end: 4, top: ['yes 99%'] });
    expect(noisy.ok).toBe(true); expect(noisy.notes[0]).toContain('NOISY MACHINE');
    s.counters.setPass = 400;
    expect(judgeTpLong(resolved, report(good('play-opening'), s, good('tp-worst')), { ncpu: 18, start: 3, peak: 14, end: 4, top: [] }).ok).toBe(false);
  });
});

describe('a drive that was not the row\'s', () => {
  test('wrong size, wrong heading (wrapping at 360), too few frames, a row missing, a failed row, no report', () => {
    const s = good('tp-street'); s.screen = [1920, 1080]; expect(judgeTpLong(resolved, report(good('play-opening'), s, good('tp-worst'))).problems[0]).toContain('not the Air\'s size');
    const h = good('tp-worst'); h.faceHeading = 100; expect(judgeTpLong(resolved, report(good('play-opening'), good('tp-street'), h)).problems[0]).toContain('faced 100 degrees, the row says 90');
    const n = good('tp-worst'); n.faceHeading = undefined; expect(judgeTpLong(resolved, report(good('play-opening'), good('tp-street'), n)).problems[0]).toContain('no heading reported');
    const w = good('tp-street'); w.faceHeading = 181.5; expect(judgeTpLong(resolved, report(good('play-opening'), w, good('tp-worst'))).ok).toBe(true);
    const f = good('tp-street'); f.frames.count = 40; expect(judgeTpLong(resolved, report(good('play-opening'), f, good('tp-worst'))).problems[0]).toContain('only 40 frames');
    expect(judgeTpLong(resolved, report(good('play-opening'), good('tp-street'))).problems[0]).toContain('tp-worst: not run');
    const x = good('tp-street'); x.status = 'failed'; x.error = 'no walkable path'; expect(judgeTpLong(resolved, report(good('play-opening'), x, good('tp-worst'))).problems[0]).toBe('tp-street: no walkable path');
    expect(judgeTpLong(resolved, undefined).problems[0]).toContain('no suborned.tplong.report/1');
    const wrap: TpLongResolvedFile = { ...resolved, rows: [{ ...resolved.rows[1]!, faceHeading: 359 }] };
    const e = good('tp-street'); e.faceHeading = 1; expect(judgeTpLong(wrap, report(e)).ok).toBe(true);
  });
});
