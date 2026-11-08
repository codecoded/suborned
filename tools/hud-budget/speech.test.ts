// Speech one bubble at a time, and the person's slip counted apart from the news (brief 101; Latch's r5: briefs/101/latch/r5.html, "The HUD budget" and "Open"). Real frames of
// 0.2.290, each the HUD layer and manifest as shot, the world 8 px clear of the HUD flattened (the capture's own verdict):
// - fixtures/talk-123.* = r123-fight-ground: two scenes-off Listen street lines one above the other, Milena's (15 words) and Inez's (17), both lines of listen-inez in text.json.
//   The budget stacked them (Inez's face makes a 30 px run under Milena's line, within 1.3 x its height) and summed one 32-word conversation line, over the cutaway's 25.
// - fixtures/slip-085.* = r085-conv-cards: Inez's card as Glyph's slip on her, which the build writes as a `card`. No proof frame shows it with the news up, so the tests lay
//   the news lozenge in from talk-123: the same build's dock, where slip-085 draws nothing.
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { analyse, CARD_MAX, recognise, SLIP_MAX, SPEECH_MAX, speechWords, stack, type Element, type Manifest, type Run } from './budget';
import { conversationCards, conversationLines, isConversationCard } from './speech';
import { decodePng, type Image } from './png';

const fx = (f: string) => join(import.meta.dir, 'fixtures', f);
const load = (k: string) => ({ man: JSON.parse(readFileSync(fx(`${k}.manifest.json`), 'utf8')) as Manifest, shot: decodePng(readFileSync(fx(`${k}.png`))), hud: decodePng(readFileSync(fx(`${k}.hud.png`))) });
const words = (n: number, w = 'w') => Array.from({ length: n }, (_, i) => `${w}${i + 1}`).join(' ');
const inside = (r: Run, e: Element) => r.x + r.w / 2 >= e.x && r.x + r.w / 2 <= e.x + e.w && r.y + r.h / 2 >= e.y && r.y + r.h / 2 <= e.y + e.h;

describe('speech, one bubble at a time: 0.2.290\'s r123-fight-ground (fixtures/talk-123)', () => {
  const { man, shot, hud } = load('talk-123'), run = (m: Manifest) => analyse(shot, { state: m.state!, hud, manifest: m });
  const [inez, milena] = man.elements.filter(e => e.kind === 'speech') as [Element, Element]; // as the manifest lists them: Inez's line (the lower bubble), then Milena's
  const say = (a: string, b: string) => run({ ...man, elements: man.elements.map(e => (e === inez ? { ...e, text: a } : e === milena ? { ...e, text: b } : e)) });
  test('two lines of listen-inez, 17 and 15 words: each within the cutaway\'s 25, together 32', () => {
    const said = conversationLines()!;
    expect([said.has(inez.text!), said.has(milena.text!)]).toEqual([true, true]);
    expect([speechWords(inez.text!), speechWords(milena.text!)]).toEqual([17, 15]);
  });
  test('as shot: within budget, though its runs still stack into one block across both bubbles (the frame keeps proving the count is per bubble)', () => {
    const r = run(man); expect(r.failures).toEqual([]); expect(r.pass).toBe(true); expect(r.speech.length).toBe(7);
    expect(stack(r.speech).some(b => b.some(x => inside(x, inez)) && b.some(x => inside(x, milena)))).toBe(true);
  });
  test('failing example: one bubble over its cap still fails, alone and once, whichever it is', () => {
    for (const [a, b] of [[`${words(12)} thirteen`, milena.text!], [inez.text!, `${words(12)} thirteen`]] as const) {
      const f = say(a, b).failures; expect(f.length).toBe(1); expect(f[0]).toContain(`runs 13 words (a bubble at most ${SPEECH_MAX.words}: text.schema MAX_WORDS)`);
    }
  });
  test('two short bubbles at once pass (7 and 7 words, 14 together, over a bubble\'s 12); the 14 in one bubble fail', () => {
    expect(say(words(7, 'a'), words(7, 'b')).failures).toEqual([]);
    const f = say(words(14, 'a'), words(7, 'b')).failures; expect(f.length).toBe(1); expect(f[0]).toContain(`runs 14 words (a bubble at most ${SPEECH_MAX.words}`);
  });
});

describe('a bubble\'s rows are its own', () => {
  const W = 1920, H = 1080;
  /** Rows of letter-sized strokes (8 px tall, 16 px apart) drawn at these tops on the frame and its HUD layer, judged with these elements. */
  const drawn = (tops: number[], els: Element[]) => {
    const shot: Image = { w: W, h: H, data: new Uint8Array(W * H * 4) }, hud: Image = { w: W, h: H, data: new Uint8Array(W * H * 4) };
    for (let i = 0; i < W * H; i++) shot.data.set([120, 120, 120, 255], i * 4);
    for (const y of tops) { let x = 620; for (let i = 0; i < 30; i++) { const w = [3, 2, 4, 2, 5, 3][i % 6]!; for (let yy = y; yy < y + 8; yy++) for (let xx = x; xx < x + w; xx++) { shot.data.set([245, 240, 225, 255], (yy * W + xx) * 4); hud.data.set([245, 240, 225, 255], (yy * W + xx) * 4); } x += w + 2; } }
    return analyse(shot, { state: 'rest', hud, manifest: { elements: els } }).failures;
  };
  const bubble = (y: number, h: number, text: string): Element => ({ kind: 'speech', x: 610, y, w: 300, h, text });
  test('failing example: one bubble over three rows fails', () => {
    expect(drawn([400, 416, 432], [bubble(392, 52, words(8))])).toEqual([`the speech bubble "${words(8)}" runs 3 rows (a bubble at most ${SPEECH_MAX.lines})`]);
  });
  test('two bubbles of two rows each, one just under the other, pass: their four rows stack as one, but they are two bubbles', () => {
    expect(drawn([400, 416, 432, 448], [bubble(392, 36, words(8, 'a')), bubble(428, 36, words(8, 'b'))])).toEqual([]);
  });
});

describe('the person\'s slip, counted apart from the news: 0.2.290\'s r085-conv-cards (fixtures/slip-085)', () => {
  const s = load('slip-085'), t = load('talk-123'), slip = s.man.elements.find(e => e.kind === 'card')!, lozenge = t.man.elements.find(e => e.kind === 'card')!;
  const news = [lozenge, t.man.elements.find(e => e.text === 'card badge')!];
  /** The frame with the news up as well: the lozenge's pixels, the frame's and the HUD layer's, 8 px round it (1468..1829 x 999..1066). */
  const up = (() => {
    const shot = { ...s.shot, data: new Uint8Array(s.shot.data) }, hud = { ...s.hud, data: new Uint8Array(s.hud.data) };
    for (let y = 991; y <= 1074; y++) { const a = (y * shot.w + 1460) * 4, b = (y * shot.w + 1838) * 4; shot.data.set(t.shot.data.subarray(a, b), a); hud.data.set(t.hud.data.subarray(a, b), a); }
    return { shot, hud };
  })();
  const run = (els: Element[], f: { shot: Image; hud: Image } = s) => analyse(f.shot, { state: 'rest', hud: f.hud, manifest: { state: 'rest', elements: els } });
  const withNews = (els: Element[]) => run([...els, ...news], up);
  const as = (kind: Element['kind'], text = slip.text) => s.man.elements.map(e => (e === slip ? { ...e, kind, text } : e));
  test('the build writes the slip as a card; its words are listen-inez\'s cards in text.json, so it is read as a slip; the news stays a card', () => {
    expect(slip.kind).toBe('card'); expect(isConversationCard(slip.text, conversationCards())).toBe(true); expect(isConversationCard(slip.text!.replace(/ /g, '  '), conversationCards())).toBe(true);
    expect(recognise(slip, 'rest').kind).toBe('slip'); expect(recognise(lozenge, 'rest').kind).toBe('card');
    expect(isConversationCard(slip.text, null)).toBe(false); // no text.json to read: it stays a card, the stricter count
    expect(SLIP_MAX).toEqual({ rows: CARD_MAX.rows, words: CARD_MAX.words, onScreen: 1 });
  });
  test('as shot: within budget; with the news up too, one slip and one card, still within budget', () => {
    const a = run(s.man.elements), b = withNews(s.man.elements);
    expect(a.failures).toEqual([]); expect(b.failures).toEqual([]); expect(b.coveragePct).toBeGreaterThan(a.coveragePct + 0.5);
  });
  test('the host\'s tag `slip` gives the same verdict', () => { expect(withNews(as('slip')).failures).toEqual([]); });
  test('failing examples: a second slip; a second news card; a slip over the cards\' words; a card that is no conversation\'s beside the news', () => {
    expect(withNews([...s.man.elements, { ...slip, y: slip.y + 300 }]).failures).toEqual([`2 slips on screen (a person's card, on them: at most ${SLIP_MAX.onScreen})`]);
    expect(withNews([...s.man.elements, { ...lozenge, y: lozenge.y - 80 }]).failures).toEqual([`2 card panels on screen (at most ${CARD_MAX.onScreen})`]);
    expect(run(as('slip', words(SLIP_MAX.words + 1))).failures).toEqual([`the slip "${words(SLIP_MAX.words + 1).slice(0, 40)}…" runs ${SLIP_MAX.words + 1} words (at most ${SLIP_MAX.words})`]);
    expect(withNews(as('card', 'Loan paid. Inez is with us. Call Inez')).failures).toEqual([`2 card panels on screen (at most ${CARD_MAX.onScreen})`]); // 0.2.24x's words, gone from text.json
  });
});
