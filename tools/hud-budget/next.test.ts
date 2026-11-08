// The next-event row's own kind (brief 101, Glyph's limit: briefs/101/glyph/r3/notes.md "The next event's limit"; next.ts). Builds to 0.2.287 write it as a `marker`, whose two-word
// cap failed every known name with its time ("Popovic exits 5:30", "Turn driver by 5:20": r067 30/32 on 0.2.286). fixtures/next-067.* = 0.2.286's r067-official-car as shot
// (the HUD layer and manifest; the world 8 px clear of the HUD flattened, the same verdict as the capture).
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { analyse, MARKER_MAX, NEXT_MAX, recognise, type Element, type Manifest, type State } from './budget';
import { isNextText, nextFaults, nextParts } from './next';
import { decodePng, type Image } from './png';

const fx = (f: string) => join(import.meta.dir, 'fixtures', f);
const flat = (w: number, h: number, v = 120): Image => { const data = new Uint8Array(w * h * 4); for (let i = 0; i < w * h; i++) data.set([v, v, v, 255], i * 4); return { w, h, data }; };
const row = (text: string, kind: Element['kind'] = 'next', y = 94): Element => ({ kind, x: 1742, y, w: 158, h: 28, text });
/** The faults of a frame with these rows and nothing drawn (the words alone). */
const judge = (els: Element[], state: State = 'rest') => analyse(flat(1920, 1080), { state, rects: [{ x: 0, y: 0, w: 10, h: 10 }], manifest: { state, elements: els } }).failures;
const timeline = (JSON.parse(readFileSync(join(import.meta.dir, '../../contracts/missions/handover/text.json'), 'utf8')).briefing.timeline as { at: string; label: string; short?: string }[]);

describe('the next-event row (kind `next`)', () => {
  test('Glyph\'s caps: one line, a name of at most 3 words and 16 characters, exactly one clock, one on screen; the window marker keeps its 2 words', () => {
    expect(NEXT_MAX).toEqual({ lines: 1, nameWords: 3, nameChars: 16, times: 1, onScreen: 1 }); expect(MARKER_MAX.words).toBe(2);
  });
  test('every timeline short with its time passes, as `next` and as the marker builds write today; the time alone (an event the cell can\'t know) passes', () => {
    expect(timeline.length).toBeGreaterThan(0);
    for (const m of timeline) {
      const text = `${m.short ?? m.label} ${m.at}`;
      expect([text, nextFaults(text)]).toEqual([text, []]); expect(judge([row(text)])).toEqual([]); expect(judge([row(text, 'marker')])).toEqual([]);
      expect(judge([row(m.at)])).toEqual([]); expect(recognise(row(m.at, 'marker'), 'rest').kind).toBe('marker');
    }
  });
  test('failing example: the full label as the name ("Popovic leaves the Office 5:30") fails on words', () => {
    expect(timeline.find(m => m.at === '5:30')?.label).toBe('Popovic leaves the Office'); // what the host shows for a mark with no short (HudToolkit.Clock.cs: TimelineShort, else NextLabel)
    const f = judge([row('Popovic leaves the Office 5:30')]).join(' | ');
    expect(f).toContain(`its name is 4 words (at most ${NEXT_MAX.nameWords})`);
  });
  test('failing example: "Councillor Popovic exits 5:30" is three words but 24 characters: it fails on characters only', () => {
    const f = judge([row('Councillor Popovic exits 5:30')]);
    expect(f).toEqual([`the next-event row "Councillor Popovic exits 5:30": its name is 24 characters (at most ${NEXT_MAX.nameChars}, spaces included)`]);
  });
  test('failing example: two rows on screen fail; one off the frame is not on screen', () => {
    expect(judge([row('Chime 5:10'), row('Popovic exits 5:30', 'next', 130)]).join(' | ')).toContain(`2 next-event rows on screen (at most ${NEXT_MAX.onScreen})`);
    expect(judge([row('Chime 5:10'), { ...row('Popovic exits 5:30'), x: 1990 }])).toEqual([]);
    expect(judge([row('Chime 5:10', 'marker'), row('Popovic exits 5:30', 'marker', 130)]).join(' | ')).toContain('2 next-event rows on screen'); // as today's builds write them
  });
  test('failing example: not exactly one clock at the end, and a full stop, fail; the name never counts the clock', () => {
    expect(nextParts('Popovic exits 5:30')).toEqual({ name: 'Popovic exits', clocks: 1, atEnd: true }); expect(nextParts('5:30')).toEqual({ name: '', clocks: 1, atEnd: true });
    expect(nextFaults('Popovic exits')).toEqual(['the next-event row "Popovic exits" shows 0 clocks (exactly 1 m:ss, at the end)']);
    expect(nextFaults('5:30 Popovic exits')).toEqual(['the next-event row "5:30 Popovic exits" shows 1 clock, not at its end (exactly 1 m:ss, at the end)']);
    expect(nextFaults('Chime 5:10 5:30')).toEqual(['the next-event row "Chime 5:10 5:30" shows 2 clocks (exactly 1 m:ss, at the end)']);
    expect(nextFaults('Popovic exits. 5:30')).toEqual(['the next-event row "Popovic exits. 5:30" has a full stop (a name and its time, never a sentence)']);
    expect(judge([{ ...row(''), text: undefined }]).join(' | ')).toContain('shows 0 clocks'); // a row the host left without words says nothing: it fails
  });
  test('failing example: a row whose words wrap onto a second line fails; the same words on one line pass', () => {
    const drawn = (rows: number[]) => {
      const shot = flat(1920, 1080), hud: Image = { w: 1920, h: 1080, data: new Uint8Array(1920 * 1080 * 4) };
      for (const y of rows) { let x = 1748; for (let i = 0; i < 14; i++) { const w = [3, 2, 4, 2, 5, 3][i % 6]!; for (let yy = y; yy < y + 8; yy++) for (let xx = x; xx < x + w; xx++) { shot.data.set([245, 240, 225, 255], (yy * 1920 + xx) * 4); hud.data.set([245, 240, 225, 255], (yy * 1920 + xx) * 4); } x += w + 2; } }
      return analyse(shot, { state: 'rest', hud, manifest: { elements: [{ kind: 'next', x: 1742, y: 90, w: 158, h: 40, text: 'Popovic exits 5:30' }] } });
    };
    const one = drawn([100]), two = drawn([100, 116]);
    expect(one.nexts.length).toBe(1); expect(one.failures).toEqual([]); expect(one.sentences).toEqual([]);
    expect(two.failures).toEqual(['the next-event row "Popovic exits 5:30" wraps over more than one line']); expect(two.sentences).toEqual([]); // its words are its own, never running text
  });
  test('a marker is read as the next row only with letters before a trailing clock; a bare clock and a marker\'s words stay markers, and only a marker is re-read', () => {
    const k = (t: string, kind: Element['kind'] = 'marker') => recognise(row(t, kind), 'rest').kind;
    for (const t of ['Chime 5:10', 'Close 8:10', 'Turn driver by 5:20', 'Popovic exits 5:30', 'Driver cutoff 5:20', 'Popovic at car 5:50']) expect(k(t)).toBe('next');
    // every other marker text with letters in the 2,580 manifests captured from 2026-11-04 to 0.2.287, and bare clocks and the police row's unknown
    for (const t of ['1:55', '5:21', '?', 'taking aim', 'done', 'reported', 'wary', 'Back to map · T', 'noticed', 'walking in', 'stop first', 'waiting', 'no line', 'In range', 'no gun', 'Goes loud', 'cancelled', 'busy', 'aiming']) expect(k(t)).toBe('marker');
    expect(k('Get there by 1:55', 'line')).toBe('line'); expect(k('Chime 5:10', 'next')).toBe('next'); expect(k('Popovic exits 5:30', 'speech')).toBe('speech');
    expect(isNextText('Chime 5:10')).toBe(true); expect(isNextText('5:10')).toBe(false); expect(isNextText('Chime 5:105')).toBe(false); expect(isNextText(undefined)).toBe(false);
  });
});

describe('0.2.286\'s r067-official-car as shot (fixtures/next-067): the next row "Popovic exits 5:30"', () => {
  const man = JSON.parse(readFileSync(fx('next-067.manifest.json'), 'utf8')) as Manifest, shot = decodePng(readFileSync(fx('next-067.png'))), hud = decodePng(readFileSync(fx('next-067.hud.png')));
  const run = (m: Manifest) => analyse(shot, { state: m.state!, hud, manifest: m });
  const next = man.elements.find(e => e.text === 'Popovic exits 5:30')!;
  test('written as a `marker`, over the marker\'s two words: the reason it failed as one', () => {
    expect(next.kind).toBe('marker'); expect(next.text!.split(/\s+/).length).toBeGreaterThan(MARKER_MAX.words);
  });
  test('read as the next row: within budget, its name and time one line, the clock\'s own time still a marker', () => {
    const r = run(man); expect(r.failures).toEqual([]); expect(r.pass).toBe(true);
    expect(r.nexts.length).toBeGreaterThan(0); expect(r.sentences).toEqual([]); expect(r.markers.length).toBeGreaterThan(0); // "5:21", the clock row's own marker
  });
  test('the host\'s tag gives the same verdict, and the full label in its place fails on its words', () => {
    const tag = (text: string) => ({ ...man, elements: man.elements.map(e => (e === next ? { ...e, kind: 'next' as const, text } : e)) });
    expect(run(tag('Popovic exits 5:30')).failures).toEqual([]);
    expect(run(tag('Popovic leaves the Office 5:30')).failures.join(' | ')).toContain('its name is 4 words');
  });
});
