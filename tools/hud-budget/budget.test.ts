// The HUD budget (brief 014, Glyph). Synthetic images prove the geometry; fixtures prove the verdicts: the play HUD of brief 013 fails (coverage and running text), the redesign passes.
// Fixtures sit on a flat ground so they stay small: tools/hud-budget/fixtures/ (after-*: the HUD layer, the composite and the manifest; before-*: the old HUD pasted from briefs/013/play by its rectangles).
import { describe, expect, test } from 'bun:test';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { analyse, footprint, LIMIT, maskOf, rectMask, stack, type Manifest, type Rect } from './budget';
import { decodePng, encodePng, type Image } from './png';

const fx = (f: string) => join(import.meta.dir, 'fixtures', f);
const png = (f: string) => decodePng(readFileSync(fx(f)));
const json = <T>(f: string) => JSON.parse(readFileSync(fx(f), 'utf8')) as T;
const flat = (w: number, h: number, v = 200): Image => { const data = new Uint8Array(w * h * 4); for (let i = 0; i < w * h; i++) data.set([v, v, v, 255], i * 4); return { w, h, data }; };
const fill = (im: Image, r: Rect, rgb: [number, number, number], a = 255) => { for (let y = r.y; y < r.y + r.h; y++) for (let x = r.x; x < r.x + r.w; x++) im.data.set([...rgb, a], (y * im.w + x) * 4); };

describe('png', () => {
  test('encode then decode gives the same pixels', () => {
    const im = flat(7, 5); im.data.set([1, 2, 3, 4], 8); const back = decodePng(encodePng(im));
    expect(back.w).toBe(7); expect(back.h).toBe(5); expect(Array.from(back.data)).toEqual(Array.from(im.data));
  });
  test('rejects what is not a PNG', () => { expect(() => decodePng(new Uint8Array([1, 2, 3, 4, 5, 6, 7, 8, 9]))).toThrow(); });
});

describe('coverage', () => {
  test('a rectangle list covers exactly its area, and overlaps count once', () => {
    const m = rectMask(100, 100, [{ x: 0, y: 0, w: 50, h: 20 }, { x: 25, y: 0, w: 50, h: 20 }]);
    expect(m.reduce((a, v) => a + v, 0)).toBe(75 * 20);
  });
  test('a HUD layer: alpha is the mask, and the share is of the whole screen', () => {
    const shot = flat(200, 100), hud: Image = { w: 200, h: 100, data: new Uint8Array(200 * 100 * 4) };
    fill(hud, { x: 0, y: 0, w: 100, h: 10 }, [10, 10, 10]);
    const r = analyse(shot, { state: 'rest', hud });
    expect(r.coveragePct).toBeCloseTo(5, 5); expect(r.mask).toBe('hud'); expect(r.pass).toBe(true);
  });
  test('a world capture: any pixel that differs is HUD, a 1 px pin-hole is closed', () => {
    const shot = flat(100, 100), world = flat(100, 100); fill(shot, { x: 10, y: 10, w: 30, h: 30 }, [20, 40, 40]); shot.data.set([200, 200, 200, 255], (25 * 100 + 25) * 4);
    const { mask, how } = maskOf(shot, { state: 'rest', world }); expect(how).toBe('world'); expect(mask[25 * 100 + 25]).toBe(1);
    expect(analyse(shot, { state: 'rest', world }).coveragePct).toBeGreaterThan(8.9);
  });
  test('over the limit fails, and the limit is per state: rest 15, active 22, alt 30, log 18, briefing 18', () => {
    expect(LIMIT).toEqual({ rest: 15, active: 22, alt: 30, log: 18, briefing: 18 });
    const shot = flat(100, 100), rects = [{ x: 0, y: 0, w: 100, h: 18 }];
    expect(analyse(shot, { state: 'rest', rects }).pass).toBe(false);
    expect(analyse(shot, { state: 'active', rects }).pass).toBe(true);
    expect(analyse(shot, { state: 'rest', rects: [{ x: 0, y: 0, w: 100, h: 12 }] }).warnings.length).toBe(1); // over 10%: a warning, not a failure
  });
  test('the footprint is never below the pixel share, and two far parts stay two', () => {
    const m = rectMask(400, 400, [{ x: 0, y: 0, w: 40, h: 40 }, { x: 300, y: 300, w: 40, h: 40 }]);
    expect(footprint(m, 400, 400)).toBeGreaterThanOrEqual(2 * 1600 / 160000 * 100 - 0.01); expect(footprint(m, 400, 400)).toBeLessThan(5);
  });
  test('no mask is an error, a mismatched capture is an error', () => {
    expect(() => analyse(flat(10, 10), { state: 'rest' })).toThrow(); expect(() => analyse(flat(10, 10), { state: 'rest', hud: flat(9, 9) })).toThrow();
  });
});

describe('text blocks', () => {
  test('stacked lines join, a far line does not', () => {
    const r = (y: number) => ({ x: 10, y, w: 80, h: 12, comps: 10, words: 3, kind: 'hud' as const });
    expect(stack([r(0), r(16), r(32)]).map(b => b.length)).toEqual([3]); expect(stack([r(0), r(200)]).map(b => b.length)).toEqual([1, 1]);
  });
  test('a flat image has no text', () => { expect(analyse(flat(200, 100), { state: 'rest', rects: [{ x: 0, y: 0, w: 200, h: 100 }] }).runs).toEqual([]); });
});

describe('the 013 play HUD (before) and the 014 redesign (after)', () => {
  test('before, an order card open: over its coverage limit, many lines of running text, stacked blocks', () => {
    const j = json<{ state: 'active'; rects: Rect[] }>('before-04.rects.json'), r = analyse(png('before-04.png'), { state: j.state, rects: j.rects });
    expect(r.pass).toBe(false); expect(r.coveragePct).toBeGreaterThan(LIMIT.active); expect(r.sentences.length).toBeGreaterThanOrEqual(10); expect(r.blocks.length).toBeGreaterThanOrEqual(5);
  });
  test('before, at rest, carrying: the feed and the chip are sentences', () => {
    const j = json<{ state: 'rest'; rects: Rect[] }>('before-07.rects.json'), r = analyse(png('before-07.png'), { state: j.state, rects: j.rects });
    expect(r.pass).toBe(false); expect(r.sentences.length).toBeGreaterThanOrEqual(3);
  });
  for (const k of ['04', '08', '12']) {
    test(`after ${k}: within budget, no running text outside the manifest`, () => {
      const m = json<Manifest>(`after-${k}.manifest.json`), r = analyse(png(`after-${k}.png`), { state: m.state!, hud: png(`after-${k}.hud.png`), manifest: m });
      expect(r.failures).toEqual([]); expect(r.pass).toBe(true); expect(r.coveragePct).toBeLessThan(LIMIT[m.state!]); expect(r.sentences).toEqual([]);
    });
  }
  test('after 08 without its manifest: the beats are running text, so it fails (a screenshot cannot tell speech from a rule)', () => {
    const m = json<Manifest>('after-08.manifest.json'), r = analyse(png('after-08.png'), { state: 'rest', hud: png('after-08.hud.png'), manifest: { elements: m.elements.filter(e => e.kind === 'image') } });
    expect(r.beats).toEqual([]); expect(r.sentences.length + r.blocks.length).toBeGreaterThan(0);
  });
  test('after 08: beats are counted, and a third one fails', () => {
    const m = json<Manifest>('after-08.manifest.json'), beat = m.elements.find(e => e.kind === 'beat')!;
    const r = analyse(png('after-08.png'), { state: 'rest', hud: png('after-08.hud.png'), manifest: { elements: [...m.elements, { ...beat, y: beat.y + 400 }] } });
    expect(r.failures.some(f => /story beats on screen/.test(f))).toBe(true);
  });
  test('the Alt state allows fragments but its coverage is still measured', () => {
    const m = json<Manifest>('after-12.manifest.json'), r = analyse(png('after-12.png'), { state: 'alt', hud: png('after-12.hud.png'), manifest: m });
    expect(r.state).toBe('alt'); expect(r.pass).toBe(true); expect(r.coveragePct).toBeGreaterThan(5);
  });
});

describe('the log state and the line check (Glyph\'s review, 2026-10-18)', () => {
  test('log: the open history panel gets 18%, sentences allowed; rest still stops at 15%', () => {
    const shot = flat(100, 100), a = [{ x: 0, y: 0, w: 100, h: 17 }], b = [{ x: 0, y: 0, w: 100, h: 19 }];
    expect(analyse(shot, { state: 'log', rects: a }).pass).toBe(true);
    expect(analyse(shot, { state: 'log', rects: b }).pass).toBe(false);
    expect(analyse(shot, { state: 'rest', rects: a }).pass).toBe(false);
    expect(analyse(shot, { state: 'log', rects: a }).warnings).toEqual([]); // the 10% warning is for rest only
    expect(analyse(shot, { state: 'briefing', rects: a }).pass).toBe(true);
    expect(analyse(shot, { state: 'briefing', rects: b }).pass).toBe(false);
  });
  const play = (f: string) => join(import.meta.dir, '../../briefs', f), has = (f: string) => existsSync(play(f + '.png')) && existsSync(play(f + '.hud.png'));
  const run = (f: string, state: 'rest' | 'log') => analyse(decodePng(readFileSync(play(f + '.png'))), { state, hud: decodePng(readFileSync(play(f + '.hud.png'))), manifest: JSON.parse(readFileSync(play(f + '.manifest.json'), 'utf8')) });
  test('a one-line plate over foliage is one line: its lit rim fragment is not a second line', () => {
    for (const f of ['022/latch/inez-serving-close', '021/latch-022/N125', '021/03-enforcer-shot-N125']) if (has(f)) expect(run(f, 'rest').failures).toEqual([]);
  });
  test('the open history panel: as log it has no sentence or block failure and fits 18%', () => {
    for (const f of ['022/latch/history-top', '022/latch/history-scrolled']) if (has(f)) { const r = run(f, 'log'); expect(r.sentences).toEqual([]); expect(r.blocks).toEqual([]); expect(r.failures.filter(x => /HUD covers|running text|stacked/.test(x))).toEqual([]); expect(run(f, 'rest').pass).toBe(false); }
  });
});

describe('a line drawn on the HUD layer is not text', () => {
  // Latch's N125 (brief 021): the shot's tracer, a long shallow diagonal on the HUD layer, broke into runs that stacked into a "block of text".
  const f = join(import.meta.dir, '../../briefs/021/latch-r4/N125');
  test('the tracer of N125 makes no block of stacked text', () => {
    if (!existsSync(f + '.png') || !existsSync(f + '.hud.png')) return;
    const r = analyse(decodePng(readFileSync(f + '.png')), { state: 'rest', hud: decodePng(readFileSync(f + '.hud.png')), manifest: JSON.parse(readFileSync(f + '.manifest.json', 'utf8')) });
    expect(r.blocks).toEqual([]); expect(r.pass).toBe(true);
  });
});

describe('the briefing card (a reading surface before play)', () => {
  const f = join(import.meta.dir, '../../briefs/022/latch/briefing-turn-keys');
  test('its title and copy are not in-play running text: as briefing it passes, as rest it fails', () => {
    if (!existsSync(f + '.png') || !existsSync(f + '.hud.png')) return;
    const run = (state: 'rest' | 'briefing') => analyse(decodePng(readFileSync(f + '.png')), { state, hud: decodePng(readFileSync(f + '.hud.png')), manifest: JSON.parse(readFileSync(f + '.manifest.json', 'utf8')) });
    expect(run('briefing').pass).toBe(true); expect(run('rest').pass).toBe(false);
  });
});
