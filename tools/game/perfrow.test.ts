// The load beside the perf numbers and the perf064 reader (Redline R7, brief 064). Run: bun test tools/game
import { describe, expect, test } from 'bun:test';
import { perf064Lines, readLoad3, spanNoisy, spanText, type Perf064Report } from './perfrow';

describe('a load span', () => {
  const span = (a: [number, number, number], b: [number, number, number], ncpu = 18) => ({ ncpu, start: a, end: b });
  test('noisy when the 1-minute average at either end reached 0.6 of the cores', () => {
    expect(spanNoisy(span([3, 4, 5], [4, 4, 5]))).toBe(false);
    expect(spanNoisy(span([3, 4, 5], [10.8, 6, 5]))).toBe(true);
    expect(spanNoisy(span([11, 6, 5], [3, 5, 5]))).toBe(true);
    expect(spanNoisy(span([5, 5, 5], [5, 5, 5], 8))).toBe(true);
    expect(spanNoisy(undefined)).toBe(false);
  });
  test('the text', () => {
    expect(spanText(span([3.04, 4, 5], [4, 4.26, 5]))).toBe('load 1/5/15 min 3/4/5 at start -> 4/4.3/5 at end, 18 cores');
    expect(spanText(span([12, 4, 5], [4, 4, 5]))).toContain('NOISY MACHINE');
    expect(spanText(undefined)).toBe('load not recorded');
  });
  test('a reading in any of the shapes the player may write', () => {
    expect(readLoad3([1, 2, 3])).toEqual([1, 2, 3]); expect(readLoad3({ l1: 1, l5: 2, l15: 3 })).toEqual([1, 2, 3]); expect(readLoad3({ m1: 1, m5: 2, m15: 3 })).toEqual([1, 2, 3]);
    expect(readLoad3([1, 2])).toBeUndefined(); expect(readLoad3('x')).toBeUndefined(); expect(readLoad3([1, 'a', 3])).toBeUndefined(); expect(readLoad3(undefined)).toBeUndefined();
  });
});

describe('the perf064 report', () => {
  const win = (name: string, legacy: boolean, extra: object = {}) => ({ name, legacy, seconds: 20, frameMs: { n: 4000, mean: 4.6, p99: 9.1, max: 20.4, over16_7: 1 }, conesMs: { mean: 0.118, p99: 3.5, max: 4.7 }, ...extra });
  test("Latch's current report (no split, no per-window load): the lines, and what is missing", () => {
    const rep: Perf064Report = { build: 'Build 0.2.122', screen: '2880x1864', device: 'Mac17,14', windows: [
      win('before-1', true, { frameMs: { mean: 4.72, p99: 14.069, max: 18.464 } }), win('after-1', false), win('before-2', true), win('after-2', false)] };
    const v = perf064Lines(rep);
    expect(v.lines).toHaveLength(5); expect(v.lines[1]).toContain('before-1 (legacy) · frame 4.7/14.1/18.5'); expect(v.lines[2]).toContain('after-1 · frame 4.6/9.1/20.4'); expect(v.lines[2]).toContain('cones 0.1/3.5/4.7');
    expect(v.notes.join('\n')).toContain('no load per window'); expect(v.notes.join('\n')).toContain('no CPU/GPU split'); expect(v.noisy).toBe(false);
  });
  test('with the split and per-window load: printed, and a noisy window makes the report noisy', () => {
    const st = (m: number) => ({ n: 100, mean: m, p99: m * 2, max: m * 3 });
    const rep: Perf064Report = { windows: [win('after-1', false, { mainMs: st(3), renderMs: st(1), gpuMs: st(2), loadStart: [3, 4, 5], loadEnd: [4, 4, 5] }), win('after-2', false, { mainMs: st(3), renderMs: st(1), gpuMs: st(2), loadStart: [4, 4, 5], loadEnd: [13, 6, 5] })] };
    const v = perf064Lines(rep);
    expect(v.lines[1]).toContain('main 3/6/9 · render 1/2/3 · gpu 2/4/6 · 3/4/5 -> 4/4/5'); expect(v.lines[1]).not.toContain('NOISY'); expect(v.lines[2]).toContain('NOISY');
    expect(v.noisy).toBe(true); expect(v.notes.join('\n')).toContain('NOISY MACHINE'); expect(v.notes.join('\n')).not.toContain('no CPU/GPU split');
  });
  test('a noisy step makes it noisy; no windows says why', () => {
    expect(perf064Lines({ windows: [win('after-1', false)] }, true).noisy).toBe(true);
    expect(perf064Lines({ status: 'no host' }).notes[0]).toContain('no host'); expect(perf064Lines(undefined).lines).toEqual([]);
  });
});
