// The hover lines' own kind (brief 101; Glyph: briefs/101/glyph/r5-review.md item 1, r4/notes.md "For Splice" 5; hover.ts). fixtures/hover-101.* = 0.2.295's r101-lozenge-hover as
// shot (the HUD layer and manifest; the world 8 px clear of the HUD flattened, the same verdict and runs as the capture): the police card's lozenge hovered, its full line 10 px over
// the card, "Shots were heard at the Office door, so the police are coming." The build leaves that line out of the manifest, so the reader takes it as four short HUD runs.
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { analyse, HOVER_MAX, VERB_WORDS, type Element, type Manifest, type State } from './budget';
import { isKey, verbParts, VERB_ROW } from './hover';
import { decodePng, type Image } from './png';
import { MAX_LINE } from '../../contracts/missions/handover/text.schema';
import { whyProblems, type Why } from '../../contracts/missions/handover/why.schema';

const W = 1920, H = 1080, TEXT = join(import.meta.dir, '../../contracts/missions/handover/text.json'), text = JSON.parse(readFileSync(TEXT, 'utf8'));
const blank = (): Image => { const data = new Uint8Array(W * H * 4); for (let i = 0; i < W * H; i++) data.set([120, 120, 120, 255], i * 4); return { w: W, h: H, data }; };
/** The faults of a frame with these elements and nothing drawn (the words alone). */
const judge = (els: Element[], state: State = 'rest') => analyse(blank(), { state, rects: [{ x: 0, y: 0, w: 10, h: 10 }], manifest: { state, elements: els } }).failures;
/** Rows of letter-sized strokes (8 px tall, 16 px apart) drawn from x0 at these tops, on the frame and its HUD layer, judged with these elements. */
const drawn = (tops: number[], els: Element[], x0 = 620) => {
  const shot = blank(), hud: Image = { w: W, h: H, data: new Uint8Array(W * H * 4) };
  for (const y of tops) { let x = x0; for (let i = 0; i < 30; i++) { const w = [3, 2, 4, 2, 5, 3][i % 6]!; for (let yy = y; yy < y + 8; yy++) for (let xx = x; xx < x + w; xx++) { shot.data.set([245, 240, 225, 255], (yy * W + xx) * 4); hud.data.set([245, 240, 225, 255], (yy * W + xx) * 4); } x += w + 2; } }
  return analyse(shot, { state: 'active', hud, manifest: { elements: els } });
};
const tip = (t: string, x = 610, y = 392, w = 300, h = 22): Element => ({ kind: 'hover', x, y, w, h, text: t });
const row: Element = { kind: 'image', x: 144, y: 30, w: 200, h: 60, text: VERB_ROW };

describe('the hover kind (hover.ts)', () => {
  test('Glyph\'s caps: one under the pointer, at most two rows; the characters the text schema\'s line; the verb row\'s words one row, three words and a key, one at a time', () => {
    expect(HOVER_MAX).toEqual({ lines: 2, chars: MAX_LINE, onScreen: 1 }); expect(VERB_WORDS).toEqual({ lines: 1, words: 3, keys: 1, onScreen: 1 });
  });
  test('the characters are the why schema\'s own line cap (the longest a hover carries): a line of 80 passes its check there, 81 fails it', () => {
    const probe = (n: number) => { const w = structuredClone(text.why) as Why; w.alert.seen.line = `A${'b'.repeat(n - 2)}.`; return whyProblems(w).filter(p => p.startsWith('alert.seen.line:') && /characters at its widest/.test(p)); };
    expect(probe(HOVER_MAX.chars)).toEqual([]); expect(probe(HOVER_MAX.chars + 1).length).toBe(1);
  });
  test('the tips the build shows pass: every next-event tip ("Next · " and a timeline label, or "?"), the eye tips (HudModel.EyeTip, RememberTip), the take-control tab\'s words', () => {
    const labels = (text.briefing.timeline as { label: string }[]).map(m => `Next · ${m.label}`);
    expect(labels.length).toBeGreaterThan(0);
    for (const t of [...labels, 'Next · ?', 'Seen by a watcher.', 'Seen by the entrance camera.', 'Unknown. Watch first.', 'Unseen.', 'A passer-by would remember your face.', '12 passers-by would remember your face.', 'Take control · T']) expect([t, judge([tip(t)])]).toEqual([t, []]);
  });
  test('failing examples: two tips at once; a line over the characters (a head beside it is a label); a tip with no words. A tip beside the armed verb\'s name passes: the name is counted apart', () => {
    expect(judge([tip('Unseen.'), tip('Next · Chime', 1700, 120)])).toEqual([`2 hover lines on screen (one pointer: at most ${HOVER_MAX.onScreen})`]);
    const long = `S${'o'.repeat(MAX_LINE - 1)}.`;
    expect(judge([tip(long)])).toEqual([`the hover line "${long.slice(0, 40)}…": its line is ${MAX_LINE + 1} characters (at most ${MAX_LINE}: text.schema MAX_LINE)`]);
    expect(judge([tip(`Next · ${long.slice(1)}`)])).toEqual([]); // a head beside its line is a label: the longest timeline label behind "Next · " runs 86 as shown
    expect(judge([tip('')])).toEqual(['the hover line "" has no words (the host writes the line it shows)']);
    expect(judge([row, tip('Knock out', 150, 66, 80, 20), tip('Unseen.')])).toEqual([]);
  });
  test('the verb row\'s words (a hover on the verb row): "Fire · ⇧2", "Knock out", "Take control · T" pass; four words, two keys, two names at once fail', () => {
    expect([isKey('⇧2'), isKey('T'), isKey('Space'), isKey('Num 5'), isKey('<Verb1>'), isKey('Fire'), isKey('Knock out')]).toEqual([true, true, true, true, true, false, false]);
    expect(verbParts('Fire · ⇧2')).toEqual({ words: 1, keys: 1 }); expect(verbParts('Knock out')).toEqual({ words: 2, keys: 0 });
    const on = (t: string) => tip(t, 150, 66, 120, 20);
    for (const t of ['Fire · ⇧2', 'Knock out', 'Kill · ⇧3', 'Take control · T']) expect([t, judge([row, on(t)])]).toEqual([t, []]);
    expect(judge([row, on('Talk them down quietly · ⇧3')])).toEqual([`the verb row's words "Talk them down quietly · ⇧3" are 4 words (a verb's name at most ${VERB_WORDS.words}, and its key)`]);
    expect(judge([row, on('Fire · ⇧2 · ⇧3')])).toEqual([`the verb row's words "Fire · ⇧2 · ⇧3" show 2 keys (at most ${VERB_WORDS.keys})`]);
    expect(judge([row, on('Fire · ⇧2'), on('Knock out')])).toEqual([`2 verb names on the verb row (one at a time)`]);
  });
  test('rows, read from the strokes: a tip of two rows passes, three fail; the verb row\'s words in their image (today\'s build) pass in one row and fail in two', () => {
    const t = tip('Shots were heard at the Office door, so the police are coming.', 610, 392, 300, 52);
    expect(drawn([400, 416], [t]).failures).toEqual([]); expect(drawn([400, 416], [t]).sentences).toEqual([]);
    expect(drawn([400, 416, 432], [t]).failures).toEqual([`the hover line "${t.text!.slice(0, 40)}…" runs 3 rows (at most ${HOVER_MAX.lines})`]);
    const img: Element = { kind: 'image', x: 610, y: 392, w: 300, h: 52, text: VERB_ROW };
    expect(drawn([400], [img]).failures).toEqual([]); expect(drawn([400, 416], [img]).failures).toEqual(["the verb row's words read as 2 rows (one row: a verb's name and its key, one at a time)"]);
  });
});

describe('0.2.295\'s r101-lozenge-hover (fixtures/hover-101): the lozenge\'s full line', () => {
  const fx = (f: string) => join(import.meta.dir, 'fixtures', f), man = JSON.parse(readFileSync(fx('hover-101.manifest.json'), 'utf8')) as Manifest;
  const shot = decodePng(readFileSync(fx('hover-101.png'))), hud = decodePng(readFileSync(fx('hover-101.hud.png'))), run = (els: Element[]) => analyse(shot, { state: man.state!, hud, manifest: { ...man, elements: els } });
  const line = 'Shots were heard at the Office door, so the police are coming.', said = tip(line, 1414, 969, 420, 30); // as the host will write it: the ink is 1418..1831 x 973..994
  const band = (r: { runs: { y: number }[] }) => r.runs.filter(x => x.y > 960 && x.y < 1000).length;
  test('the line is the police card\'s why, filled: text.json why.police.heard with {where} at the Office door', () => {
    expect(text.why.police.heard.line.replace('{where}', text.why.places['d:Office entrance'])).toBe(line); expect(man.elements.some(e => e.kind === 'hover')).toBe(false);
  });
  test('as shot: within budget, the line read as four short HUD runs, none a sentence (the gap: nothing holds it)', () => {
    const r = run(man.elements); expect(r.failures).toEqual([]); expect(band(r)).toBe(4); expect(r.sentences).toEqual([]);
  });
  test('tagged `hover`: its runs are the hover\'s, never HUD text, one row of one line: within budget', () => {
    const r = run([...man.elements, said]); expect(r.failures).toEqual([]); expect(band(r)).toBe(0);
  });
  test('failing examples on the real line: a second tip at once; the line\'s words over the characters', () => {
    expect(run([...man.elements, said, tip('Unseen.', 300, 300)]).failures).toEqual([`2 hover lines on screen (one pointer: at most ${HOVER_MAX.onScreen})`]);
    const long = `${line} ${line}`;
    expect(run([...man.elements, { ...said, text: long }]).failures).toEqual([`the hover line "${long.slice(0, 40)}…": its line is ${[...long].length} characters (at most ${MAX_LINE}: text.schema MAX_LINE)`]);
  });
});
