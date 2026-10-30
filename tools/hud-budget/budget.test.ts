// The HUD budget (brief 014, Glyph). Synthetic images prove the geometry; fixtures prove the verdicts: the play HUD of brief 013 fails (coverage and running text), the redesign passes.
// Fixtures sit on a flat ground so they stay small: tools/hud-budget/fixtures/ (after-*: the HUD layer, the composite and the manifest; before-*: the old HUD pasted from briefs/013/play by its rectangles).
import { describe, expect, test } from 'bun:test';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { isPipRow, ALTLINE_MAX, analyse, BAR_MAX, bands, barFromParts, BRIEFING_READING, CHIP_MAX, chipsFromParts, footprint, fromParts, glyphWords, isClock, LIMIT, LOCK_MAX, MARKER_MAX, maskOf, PILL_MAX, POP_MAX, popsFromParts, recognise, rectMask, stack, type Element, type Manifest, type Rect, type Run, type State } from './budget';
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
      expect(r.failures).toEqual([]); expect(r.pass).toBe(true); expect(r.coveragePct).toBeLessThan(LIMIT[m.state as keyof typeof LIMIT]); expect(r.sentences).toEqual([]);
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

// RETIRED rule (GDD v0.27.0 §10: the briefing is a page now, judged as `page` below). The `briefing` state stays only for these archived street-overlay captures, so the
// old card and the 062 street briefing still judge as they were shot; no build emits it. Kept, not deleted: they prove the retired state did not loosen the in-play rules.
describe('RETIRED: the briefing card (a reading surface before play)', () => {
  const f = join(import.meta.dir, '../../briefs/022/latch/briefing-turn-keys');
  test('its title and copy are not in-play running text: as briefing it passes, as rest it fails', () => {
    if (!existsSync(f + '.png') || !existsSync(f + '.hud.png')) return;
    const run = (state: 'rest' | 'briefing') => analyse(decodePng(readFileSync(f + '.png')), { state, hud: decodePng(readFileSync(f + '.hud.png')), manifest: JSON.parse(readFileSync(f + '.manifest.json', 'utf8')) });
    expect(run('briefing').pass).toBe(true); expect(run('rest').pass).toBe(false);
  });
});

describe('RETIRED: the street briefing (brief 062 B: a modal reading surface, scrim outside the HUD layer)', () => {
  // fixtures/briefing-b.manifest.json = batch 0.2.122's briefing-day.manifest.json (state briefing; hook 6 words and objective 7 as `hud`, a hidden place label off screen)
  const man = JSON.parse(readFileSync(join(import.meta.dir, 'fixtures/briefing-b.manifest.json'), 'utf8')) as Manifest, shot = flat(1920, 1080), rects = [{ x: 1448, y: 132, w: 408, h: 748 }, { x: 64, y: 58, w: 349, h: 108 }];
  const run = (m: Manifest, state: 'briefing' | 'rest' = 'briefing') => analyse(shot, { state, rects, manifest: m });
  test('as shipped it is within the reading cap: two sentences, thirteen words; the off-screen label is not counted', () => {
    expect(man.state).toBe('briefing'); expect(run(man).failures).toEqual([]);
    expect(man.elements.some(e => e.text === 'Records Office' && e.x > 1920)).toBe(true);
  });
  test('the dossier objective is reading copy (`hud`), not a goal line: its seven words count toward the reading cap, never toward the six-word `line` rule', () => {
    const obj = man.elements.find(e => /^Take him alive/.test(e.text ?? ''))!; expect(obj.kind).toBe('hud'); expect(obj.text!.split(/\s+/).length).toBe(7);
  });
  test('the cap bites: a third paragraph, or a longer one, fails; rest is untouched by it', () => {
    const para = (text: string, y: number) => ({ kind: 'hud' as const, x: 64, y, w: 349, h: 28, text });
    const more: Manifest = { ...man, elements: [...man.elements, para('He keeps it in the left drawer.', 170), para('Inez knows which one it is.', 200)] };
    expect(run(more).failures.join()).toContain('the briefing reads 4 sentences, 26 words');
    const long: Manifest = { ...man, elements: [...man.elements, para('Take him alive before it reaches Security or the whole block will know who sent you here today.', 170)] };
    expect(run(long).failures.join()).toContain(`at most ${BRIEFING_READING.lines} and ${BRIEFING_READING.words}`);
    expect(run(more, 'rest').failures.join()).not.toContain('the briefing reads');
  });
});

describe('the seal marker and the refusal pop (batch 0.2.123 r123-fight-loud; GDD v0.26.0 §10)', () => {
  // fixtures/loud-123.* = Latch's round-4 r123-fight-loud (the HUD layer, its manifest and parts as shot; the world outside the HUD flattened to keep the file small).
  // The frame shows the goal line "Get Vale to the car.", the seal clock "1:55" under the clock bar (the manifest has it as a `line`), and the refusal pop "Pick when to fire" (stance-unknown) over the Enforcer.
  const man = json<Manifest>('loud-123.manifest.json'), parts = json<Record<string, number[]>>('loud-123.parts.json'), shot = png('loud-123.png'), hud = png('loud-123.hud.png');
  const run = (m: Manifest, withParts = true) => analyse(shot, { state: 'rest', hud, manifest: withParts ? { ...m, elements: [...m.elements, ...popsFromParts(parts, m, shot)] } : m });
  const clock = man.elements.find(e => e.text === '1:55')!, goal = man.elements.find(e => /^Get Vale/.test(e.text ?? ''))!;
  test('the manifest as shot: the clock is emitted as a `line` beside the goal line, and its pop is not named', () => {
    expect(clock.kind).toBe('line'); expect(goal.kind).toBe('line'); expect(man.elements.some(e => e.kind === 'pop')).toBe(false); expect(Object.keys(parts).filter(k => /^pop-\d+$/.test(k)).length).toBe(3);
  });
  test('the seal clock is a window marker, not a second goal line: one goal line on screen, no "second line" failure', () => {
    const r = run(man); expect(r.failures).toEqual([]); expect(r.pass).toBe(true);
    expect(r.lines.map(l => l.words)).toEqual([5]); expect(r.markers.length).toBe(1); expect(r.markers[0]!.words).toBe(1); expect(r.coveragePct).toBeLessThan(LIMIT.rest);
  });
  test('a clock reading is only a clock: "1:55" is one, a line that holds a time is not', () => {
    expect(['1:55', '0:09', ' 12:30 ', '1:02:03'].every(isClock)).toBe(true); expect(['Get there by 1:55', '1:5', 'Take Vale', '', undefined].some(isClock)).toBe(false);
  });
  test('the marker kind works as emitted: the same frame with the clock as `marker` passes the same', () => {
    const r = run({ ...man, elements: man.elements.map(e => (e === clock ? { ...e, kind: 'marker' as const } : e)) }); expect(r.failures).toEqual([]); expect(r.markers.length).toBe(1);
  });
  test('a real second goal line still fails (the rule is not loosened)', () => {
    const second = { kind: 'line' as const, x: clock.x, y: clock.y, w: clock.w, h: clock.h, text: 'Reach the car' };
    expect(run({ ...man, elements: [...man.elements.filter(e => e !== clock), second] }).failures.join()).toContain('2 objective or hint lines on screen');
  });
  test('a marker is a lock and m:ss: one that reads a sentence fails', () => {
    const sentence = { ...clock, kind: 'marker' as const, text: 'Police hold the car now' };
    expect(MARKER_MAX.words).toBe(2); expect(run({ ...man, elements: man.elements.map(e => (e === clock ? sentence : e)) }).failures.join()).toContain('a window marker reads');
  });
  test('the refusal pop "Pick when to fire" is four words in one line: allowed as a pop, running text when it is not named', () => {
    const r = run(man); expect(r.pops.length).toBe(1); expect(r.pops[0]!.words).toBe(4); expect(r.sentences).toEqual([]);
    const un = run(man, false); expect(un.sentences.length).toBe(1); expect(un.failures.join()).toContain('running text'); // without the parts the pop is read as text, the old verdict
  });
  test('the three pops drawn on one spot are one place; a fifth word, or four places, fail', () => {
    const named = man.elements.concat(popsFromParts(parts, man, shot)), pop = named.find(e => e.kind === 'pop')!;
    expect(popsFromParts(parts, man, shot).length).toBe(3); expect(run(man).failures).toEqual([]); expect(POP_MAX).toEqual({ lines: 1, words: 4, onScreen: 3 });
    const long = analyse(shot, { state: 'rest', hud, manifest: { ...man, elements: [...man.elements, { ...pop, text: 'Pick when you want to fire' }] } });
    expect(long.failures.join()).toContain('a refusal pop is longer than 4 words');
    const four = analyse(shot, { state: 'rest', hud, manifest: { ...man, elements: [...man.elements, ...[0, 1, 2, 3].map(i => ({ ...pop, x: pop.x + i * 400 }))] } });
    expect(four.failures.join()).toContain('refusal pops on screen');
  });
  test('a pop the parts name off the screen, or the manifest already names, is not added twice', () => {
    expect(popsFromParts({ 'pop-1': [908, -310, 104, 36] }, man, shot)).toEqual([]);
    const named: Manifest = { ...man, elements: [...man.elements, { kind: 'pop', x: 608, y: 202, w: 150, h: 36, text: 'Pick when to fire' }] }; expect(popsFromParts(parts, named, shot)).toEqual([]);
    expect(run(named, false).failures).toEqual([]);
  });
});

describe('the kinds of GDD v0.26.0 §10 (Glyph\'s limits, briefs/067/glyph-budget.md; batch 0.2.123 stopped on 0.2.125)', () => {
  // fixtures/lock-067.*, alt-067.*, ground-123.* = build 0.2.125's r067-lock-pill-guard, r067-car-ring-go-alt and r123-fight-ground (HUD layer, manifest and parts as shot; the world outside
  // the HUD flattened). Their manifests carry the pills and the Alt line as `line` and leave the done chips out: the tool reads them by text shape and parts until the host tags them.
  const fix = (n: string) => ({ shot: png(`${n}.png`), hud: png(`${n}.hud.png`), man: json<Manifest>(`${n}.manifest.json`), parts: json<Record<string, number[]>>(`${n}.parts.json`) });
  const judge = (n: string, edit: (m: Manifest) => Manifest = m => m, opts: { parts?: boolean; state?: State } = {}) => {
    const f = fix(n), m0 = edit(f.man), m = opts.parts === false ? m0 : { ...m0, elements: [...m0.elements, ...fromParts(f.parts, m0, f.shot)] };
    return analyse(f.shot, { state: opts.state ?? m.state!, hud: f.hud, manifest: m });
  };
  const fails = (r: { failures: string[] }) => r.failures.join(' | ');
  const flatShot = flat(1920, 1080), noText = (els: Element[], state: State) => analyse(flatShot, { state, rects: [{ x: 0, y: 0, w: 10, h: 10 }], manifest: { state, elements: els } });
  const el = (kind: Element['kind'], text: string | undefined, x = 800, y = 400, w = 300, h = 38, more: Partial<Element> = {}): Element => ({ kind, x, y, w, h, text, ...more });

  test('words as Glyph counts them: the dot is no word, a figure with its unit is one, a label and its figure one mark, a keycap one', () => {
    expect(glyphWords('Hold · Drive into the service drive · 29 s')).toBe(7); expect(glyphWords('Hold · Board · 8 s')).toBe(3);
    expect(glyphWords('Listen to the guard · Already heard')).toBe(6); expect(glyphWords('siren 1')).toBe(1); expect(glyphWords('draws 1.0 s')).toBe(1);
    expect(glyphWords('$20,000')).toBe(1); expect(glyphWords('+2')).toBe(1); expect(glyphWords('1:55')).toBe(1); expect(glyphWords('Turn twice: <TurnLeft> <TurnLeft>')).toBe(4);
    expect(glyphWords('Go without Vale calls off the attempt. The cell gets away to try again.')).toBe(14);
  });
  test('today\'s `line` pieces are told by their text shape; the goal line, the hints and the seal clock keep theirs', () => {
    const k = (t: string, st: State = 'active') => recognise(el('line', t), st).kind;
    expect(k('Hold · Board · 8 s')).toBe('pill'); expect(k('Let go · Board · 8 s')).toBe('pill'); expect(k('Hold · Drive into the service drive · 29 s')).toBe('pill');
    expect(k('to Inez · Listen · 13 s')).toBe('pill'); expect(k('Aiming · 1 s')).toBe('pill'); // a pill ends in its time
    expect(k('Listen to the guard · Already heard')).toBe('lock'); expect(k('Call Inez · Alarm up')).toBe('lock'); // a lock ends in its reason
    expect(k('Take Vale alive.')).toBe('line'); expect(k('Turn twice: <TurnLeft> <TurnLeft>')).toBe('line'); expect(k('Scroll to switch.')).toBe('line'); expect(k('1:55')).toBe('marker');
    expect(k('Go without Vale calls off the attempt. The cell gets away to try again.', 'alt')).toBe('altline'); expect(k('Take Vale alive.', 'alt')).toBe('line');
    expect(k('Go without Vale calls off the attempt. The cell gets away to try again.', 'active')).toBe('line'); // outside Alt a long line stays a line, and fails as one
    expect(recognise(el('speech', 'Hold · Board · 8 s'), 'active').kind).toBe('speech'); // only `line` is re-read
  });

  test('r067-lock-pill-guard as shot (0.2.125): the lock pill and the goal line share the frame, the two done chips are a list: within budget', () => {
    const r = judge('lock-067'); expect(r.failures).toEqual([]); expect(r.pass).toBe(true);
    expect(r.locks.length).toBeGreaterThan(0); expect(r.locks[0]!.words).toBe(6); expect(r.lines.every(l => l.words === 3)).toBe(true); // "Take Vale alive." alone is the goal line
    expect(r.chips.length).toBeGreaterThanOrEqual(5); expect(r.sentences).toEqual([]); expect(r.blocks).toEqual([]);
    expect(r.warnings.filter(w => /not spelled in the manifest/.test(w)).length).toBe(2); // the chips' word caps wait for the host to spell them
  });
  test('its chips are read, never hidden: without the parts that name them their rows are running text again (the old verdict)', () => {
    const r = judge('lock-067', m => m, { parts: false }); expect(r.chips).toEqual([]); expect(fails(r)).toContain('running text'); expect(fails(r)).toContain('stacked text');
  });
  test('the pill slot holds one: a lock and an order pill together fail; a lock in a rest frame fails; a long lock fails', () => {
    expect(fails(judge('lock-067', m => ({ ...m, elements: [...m.elements, el('line', 'Hold · Watch the guard · 9 s', 786, 600)] })))).toContain('2 pills on screen');
    expect(fails(judge('lock-067', m => m, { state: 'rest' }))).toContain('shows in a rest frame (a pill is active only)');
    const re = (t: string) => judge('lock-067', m => ({ ...m, elements: m.elements.map(e => (e.text === 'Listen to the guard · Already heard' ? { ...e, kind: 'lock' as const, text: t } : e)) }));
    expect(re('Listen to the guard · Already heard').failures).toEqual([]); // tagged `lock` by the host: the same verdict
    expect(fails(re('Listen to the guard · Already heard him once'))).toContain(`its reason is 4 words (at most ${LOCK_MAX.reason})`);
    expect(fails(re('Listen to the guard at the public door · Already heard'))).toContain(`at most ${LOCK_MAX.words}`);
  });
  test('a spelled chip is held to its caps: facts of two words at most, three facts, three one-word verbs, nine words, two chips on screen', () => {
    const chip = (facts: string[], verbs: string[], x = 100) => el('chip', undefined, x, 100, 250, 132, { facts, verbs });
    const ok = chip(['Bureau loan', 'For hire', 'Loves Inez'], ['Help', 'Pressure']);
    expect(noText([ok], 'active').failures).toEqual([]); expect(noText([ok], 'rest').failures).toEqual([]);
    expect(fails(noText([chip(['Owes the Bureau money', 'For hire'], ['Help'])], 'active'))).toContain('the fact "Owes the Bureau money" is 4 words');
    expect(fails(noText([chip(['Bureau loan', 'For hire', 'Loves Inez', 'Rounds'], ['Help'])], 'active'))).toContain(`4 fact rows (at most ${CHIP_MAX.facts})`);
    expect(fails(noText([chip(['Rounds'], ['Help', 'Pay', 'Ask', 'Pressure'])], 'active'))).toContain('4 verb pills');
    expect(fails(noText([chip(['Rounds'], ['Help him'])], 'active'))).toContain('the verb pill "Help him" is 2 words');
    expect(fails(noText([chip(['Bureau loan', 'For hire', 'Loves Inez'], ['Help', 'Pay', 'Ask', 'Lie'])], 'active'))).toContain(`10 words (at most ${CHIP_MAX.words})`);
    expect(fails(noText([ok, chip(['Rounds'], ['Listen'], 500), chip(['Cone'], ['Pay'], 900)], 'active'))).toContain('3 done chips on screen (at most 2)');
    expect(fails(noText([ok], 'log'))).toContain('a done chip shows in a log frame');
    expect(noText([ok], 'alt').failures).toEqual([]); // Alt held while a learn lives (r059-watch-alt): no ring under it
    expect(fails(noText([ok, el('image', 'the car ring', 150, 150, 200, 200)], 'alt'))).toContain('over a ring in the Alt state');
  });
  test('the done chips the parts name are added once; one the manifest already names is not added again', () => {
    const f = fix('lock-067'); expect(chipsFromParts(f.parts, f.man, f.shot).length).toBe(2);
    const named = { ...f.man, elements: [...f.man.elements, el('chip', undefined, 835, 77, 250, 132, { facts: ['Bureau loan'], verbs: ['Help'] })] }; expect(chipsFromParts(f.parts, named, f.shot).length).toBe(1);
  });

  test('r123-fight-ground (0.2.125): the Board pill shows its time twice in a rest frame, two real faults, named', () => {
    const r = judge('ground-123'); expect(r.pass).toBe(false);
    expect(fails(r)).toContain('the order pill "Hold · Board · 1 s · 1 s" shows its time 2 times (once only)'); expect(fails(r)).toContain('shows in a rest frame');
    const fixed = judge('ground-123', m => ({ ...m, state: 'active', elements: m.elements.map(e => (e.text === 'Hold · Board · 1 s · 1 s' ? { ...e, kind: 'pill' as const, text: 'Hold · Board · 1 s' } : e)) }));
    expect(fixed.failures).toEqual([]); expect(fixed.pills.length).toBeGreaterThan(0);
  });
  test('an order pill: one line of at most eight words, a way of five, one time, two price marks; the goal line beside it is still one line', () => {
    const pill = (t: string) => fails(noText([el('pill', t), el('line', 'Take Vale alive.', 890, 1026, 141, 40)], 'active'));
    expect(pill('Hold · Drive into the service drive · 29 s')).toBe(''); expect(pill('Hold · Board · 8 s')).toBe(''); expect(pill('Let go · Knock out · 7 s · +4 · siren 1')).toBe('');
    expect(pill('Hold · Drive round the back of the office · 29 s')).toContain(`its way is 7 words (at most ${PILL_MAX.way})`);
    expect(pill('Hold · Knock out · 7 s · +4 · siren 1 · draws 0.6 s')).toContain('3 price marks');
    expect(pill('Hold · Drive into the service drive · 29 s · +1 · siren 1')).toContain(`is 9 words (at most ${PILL_MAX.words})`);
  });

  test('r067-car-ring-go-alt (0.2.125): the Alt line is 14 words in two sentences over the feed card, real faults, named', () => {
    const r = judge('alt-067'), f = fails(r); expect(r.pass).toBe(false);
    expect(f).toContain(`is 14 words (at most ${ALTLINE_MAX.words})`); expect(f).toContain('a full stop inside it'); expect(f).toContain('overlaps the beat "In the car"');
    const short = fails(judge('alt-067', m => ({ ...m, elements: m.elements.map(e => (/^Go without Vale/.test(e.text ?? '') ? { ...e, kind: 'altline' as const, text: 'Calls off the take; you escape' } : e)) })));
    expect(short).not.toContain('words (at most'); expect(short).not.toContain('full stop'); expect(short).toContain('overlaps the beat'); // Fable's six words fix the words; the overlap is Latch's
  });
  test('an Alt line only in Alt, one on screen', () => {
    const a = el('altline', 'Calls off the take; you escape'), b = el('altline', 'Leaves Vale behind', 800, 600);
    expect(noText([a], 'alt').failures).toEqual([]); expect(fails(noText([a], 'active'))).toContain('shows in a active frame (Alt only)'); expect(fails(noText([a, b], 'alt'))).toContain('2 Alt lines on screen');
  });

  test('the clock bar: one row of figures at play size; the cutaway\'s 1.8x bar (0.2.125 r067-convo-*) fails, the play bar passes', () => {
    const bar = (h: number, text?: string) => el('bar', text, 1422, 14, 484, h);
    expect(noText([bar(44)], 'rest').failures).toEqual([]); expect(BAR_MAX.h).toBe(48);
    expect(fails(noText([bar(88)], 'rest'))).toContain('the clock bar is drawn at 1.8x play size');
    expect(fails(noText([bar(44, '1:38 2:35 $100,000 0')], 'rest'))).toBe(''); expect(fails(noText([bar(44, '1:38 Chime 5:30 $100,000')], 'rest'))).toContain('carries the word "Chime"');
    expect(fails(noText([bar(44), bar(44)], 'rest'))).toContain('2 clock bars on screen');
    const run = (y: number, h: number): Run => ({ x: 1500, y, w: 60, h, comps: 6, words: 2, kind: 'hud' });
    expect(bands([run(21, 24), run(39, 15), run(27, 18)])).toBe(1); expect(bands([run(20, 18), run(50, 18)])).toBe(2); // a screw beside the clock is the same row; a wrapped bar is two
  });
  test('the bar the parts name joins once, and the batch shots of 0.2.125 stay within budget with it', () => {
    const f = fix('lock-067'); expect(barFromParts(f.parts, f.man, f.shot).length).toBe(1); expect(barFromParts(f.parts, { ...f.man, elements: [...f.man.elements, el('bar', undefined)] }, f.shot)).toEqual([]);
    expect(judge('lock-067').bars.length).toBeGreaterThan(0);
  });
});

// Brief 078: 0.2.146's fight-loud-checkpoint flagged one line of running text at (929,468), 62x5 px: the row of 8 health pips over a fighter (seen in the frame; 8 marks of 6x5 px at an 8 px pitch).
describe('a row of health pips is not running text', () => {
  const dots = (marks: { x: number; w: number; h: number }[]) => {
    const shot = flat(1920, 1080, 120), hud: Image = { w: 1920, h: 1080, data: new Uint8Array(1920 * 1080 * 4) }; // the reader's sizes are defined at 1080
    for (const m of marks) { const r = { x: m.x, y: 468, w: m.w, h: m.h }; fill(shot, r, [245, 240, 225]); fill(hud, r, [245, 240, 225]); }
    return { shot, hud };
  };
  const eight = Array.from({ length: 8 }, (_, i) => ({ x: 929 + i * 8, w: 6, h: 5 }));
  test('8 even dots: no sentence, and the shot passes', () => {
    const { shot, hud } = dots(eight), r = analyse(shot, { state: 'rest', hud });
    expect(r.sentences).toEqual([]); expect(r.pass).toBe(true);
  });
  test('failing example: marks of one height at uneven widths, as lettering is, still read as a line of running text', () => {
    const letters: { x: number; w: number; h: number }[] = []; let x = 100;
    for (let i = 0; i < 18; i++) { const w = [3, 2, 4, 2, 5, 3][i % 6]!; letters.push({ x, w, h: 8 }); x += w + 2; }
    const { shot, hud } = dots(letters), r = analyse(shot, { state: 'rest', hud });
    expect(r.sentences.length).toBe(1); expect(r.pass).toBe(false);
  });
  test('failing example: the reader\'s rule: too big, too few, uneven in width or pitch, or hollow is not a pip row', () => {
    const c = (x0: number, w = 6, h = 5, area = w * h) => ({ x0, y0: 10, x1: x0 + w - 1, y1: 10 + h - 1, area });
    const row = [0, 8, 16, 24].map(x => c(x));
    expect(isPipRow(row, 1)).toBe(true);
    expect(isPipRow(row.slice(0, 2), 1)).toBe(false);
    expect(isPipRow([0, 14, 28, 42].map(x => c(x, 11, 11)), 1)).toBe(false);
    expect(isPipRow([c(0), c(8, 9), c(19), c(27)], 1)).toBe(false);
    expect(isPipRow([c(0), c(8), c(19), c(27)], 1)).toBe(false);
    expect(isPipRow([0, 8, 16, 24].map(x => c(x, 6, 5, 8)), 1)).toBe(false);
  });
});
