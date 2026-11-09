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
  // 0.2.290 drew both lines one row, 710 and 643 px wide. Option B (Glyph's round 4, Sky's pick) wraps a bubble at 460 px into two rows, so the frame fails on the two widths
  // (Latch's piece 5, the wrap), and on nothing else: the word tests below keep those two standing and add their own.
  const q = (t: string) => `"${t.trim().slice(0, 40)}${t.trim().length > 40 ? '…' : ''}"`, wide = (t: string, w: number) => `the speech bubble ${q(t)} is one row ${w} px wide (a bubble wraps at ${SPEECH_MAX.wrap} px)`;
  const own = (f: string[]) => f.filter(x => !/ is one row (710|643) px wide /.test(x));
  test('as shot: two one-row lines over the wrap\'s 460 px; its runs still stack into one block across both bubbles, yet the words are counted per bubble', () => {
    const r = run(man); expect([...r.failures].sort()).toEqual([wide(inez.text!, 710), wide(milena.text!, 643)].sort());
    expect(r.speech.length).toBe(7); expect(r.bubbles).toBe(2);
    expect(stack(r.speech).some(b => b.some(x => inside(x, inez)) && b.some(x => inside(x, milena)))).toBe(true);
  });
  test('failing example: one bubble over its cap still fails, alone and once, whichever it is', () => {
    for (const [a, b] of [[`${words(12)} thirteen`, milena.text!], [inez.text!, `${words(12)} thirteen`]] as const) {
      const all = say(a, b).failures, f = own(all); expect(all.length - f.length).toBe(2);
      expect(f.length).toBe(1); expect(f[0]).toContain(`runs 13 words (a bubble at most ${SPEECH_MAX.words}: text.schema MAX_WORDS)`);
    }
  });
  test('two short bubbles at once pass (7 and 7 words, 14 together, over a bubble\'s 12); the 14 in one bubble fail', () => {
    expect(own(say(words(7, 'a'), words(7, 'b')).failures)).toEqual([]);
    const f = own(say(words(14, 'a'), words(7, 'b')).failures); expect(f.length).toBe(1); expect(f[0]).toContain(`runs 14 words (a bubble at most ${SPEECH_MAX.words}`);
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
  // rule 2 (Glyph's round 4): in the street a conversation's line is a bubble, two rows at most; the cutaway's own line (its column, beside the speaker's face) has no row cap
  const line = [...conversationLines()!].find(l => speechWords(l) > 12 && speechWords(l) <= 25)!, face: Element = { kind: 'image', x: 470, y: 392, w: 136, h: 136, text: "speaker's face" };
  test('failing example: a conversation\'s street line over two rows fails as a bubble does; two rows pass; three in the cutaway pass', () => {
    const q = `"${line.slice(0, 40)}…"`;
    expect(drawn([400, 416, 432], [bubble(392, 52, line)])).toEqual([`the conversation line ${q} runs 3 rows (in the street a bubble, at most ${SPEECH_MAX.lines})`]);
    expect(drawn([400, 416], [bubble(392, 36, line)])).toEqual([]);
    expect(drawn([400, 416, 432], [bubble(392, 52, line), face])).toEqual([]);
  });
});

describe('round 4: two lines at once, each box inside the screen, one row at most 460 px (Glyph, briefs/101/glyph/r4/notes.md "For Splice" 1, 3 and 4)', () => {
  const blank = (w: number, h: number): Image => { const data = new Uint8Array(w * h * 4); for (let i = 0; i < w * h; i++) data.set([120, 120, 120, 255], i * 4); return { w, h, data }; };
  /** The faults of a frame with these elements and nothing drawn: the boxes alone. */
  const judge = (els: Element[], state: 'rest' | 'active' | 'alt' | 'log' = 'rest', [w, h] = [1920, 1080]) => analyse(blank(w, h), { state, rects: [{ x: 0, y: 0, w: 10, h: 10 }], manifest: { state, elements: els } }).failures;
  const b = (x: number, y: number, w: number, text = 'Two short words.', h = 28): Element => ({ kind: 'speech', x, y, w, h, text });
  const q = (t: string) => `"${t.trim().slice(0, 40)}${t.trim().length > 40 ? '…' : ''}"`, edge = '(inside 14 px of the edge)';
  // 0.2.295's r123-fight-safe: the Fixer's call line, a rival's line, and the guard's line docked beside his face (the old plate, 25 px)
  const fixer = b(663, 326, 430, "It's time, Miss Nikolaou.", 47), rival = b(946, 669, 302, "The job's not worth this. Get to the car."), docked = b(857, 28, 207, "There's no running past this post.", 25);
  const callout: Element = { kind: 'image', x: 1084, y: 12, w: 56, h: 56, text: 'attention callout' };
  test('the caps: two on screen (text.schema\'s most for less.feed.bubblesAtOnce), two rows, 460 px a row at 1080, 14 px inside the screen', () => {
    expect([SPEECH_MAX.onScreen, SPEECH_MAX.lines, SPEECH_MAX.wrap, SPEECH_MAX.margin]).toEqual([2, 2, 460, 14]);
    const text = JSON.parse(readFileSync(join(import.meta.dir, '../../contracts/missions/handover/text.json'), 'utf8')); expect(text.less.feed.bubblesAtOnce).toBeLessThanOrEqual(SPEECH_MAX.onScreen);
  });
  test('rule 1: two pass; a third fails with Glyph\'s fault, the docked line one of them, in every play state; one off the frame is not on screen; outside play none is counted', () => {
    expect(judge([fixer, rival, callout])).toEqual([]);
    for (const s of ['rest', 'active', 'alt'] as const) expect(judge([fixer, rival, docked, callout], s)).toEqual(['3 speech bubbles on screen (at most 2; a docked line counts)']);
    expect(judge([fixer, rival, { ...docked, x: -300 }])).toEqual([]); expect(judge([fixer, rival, docked], 'log')).toEqual([]);
  });
  test('the cutaway\'s line counts toward the two; its column, beside the speaker\'s face, is no bubble: no place or width rule', () => {
    const face: Element = { kind: 'image', x: 67, y: 890, w: 136, h: 136, text: "speaker's face" }, col = b(227, 887, 1020, "Inez I didn't want anyone to know.", 139);
    expect(judge([face, col])).toEqual([]); expect(judge([face, { ...col, x: 5, w: 1910, h: 30 }])).toEqual([]);
    expect(judge([face, col, fixer, rival])).toEqual(['3 speech bubbles on screen (at most 2; a docked line counts)']);
  });
  test('rule 3: 14 px inside the screen on every side, a pixel for the manifest\'s rounding; 0.2.290\'s line to x 2120 fails as Glyph wrote it', () => {
    const t = 'The line two rows of 460 px hold.', two = (x: number, y: number, w = 432) => b(x, y, w, t, 47);
    expect(judge([two(1474, 952)])).toEqual([]); expect(judge([two(1475, 952)])).toEqual([]); // to x 1906, and 1907
    expect(judge([two(1476, 952)])).toEqual([`the speech bubble ${q(t)} runs to x 1908 ${edge}`]);
    expect(judge([two(13, 400)])).toEqual([]); expect(judge([two(12, 400)])).toEqual([`the speech bubble ${q(t)} starts at x 12 ${edge}`]);
    expect(judge([two(600, 12)])).toEqual([`the speech bubble ${q(t)} starts at y 12 ${edge}`]); expect(judge([two(600, 1020)])).toEqual([]);
    expect(judge([two(600, 1021)])).toEqual([`the speech bubble ${q(t)} runs to y 1068 ${edge}`]);
    expect(judge([b(-225, 532, 287, t, 47)])).toEqual([`the speech bubble ${q(t)} starts at x -225 ${edge}`]); // briefs/014's 01-tech-loop-targets: half off the left edge
    const fx2 = "It's time, Miss Nikolaou. Call Arthur across now, the way we said, and quietly.";
    expect(judge([b(1530, 952, 590, fx2)])).toEqual([`the speech bubble ${q(fx2)} runs to x 2120 ${edge}`, `the speech bubble ${q(fx2)} is one row 590 px wide (a bubble wraps at 460 px)`]);
  });
  test('rule 4 (option B): one row at most 460 px, a pixel for rounding; Glyph\'s 712 px row fails; two rows are wider only for a line two rows of 460 can\'t hold (490 px passes); the old docked plate passes', () => {
    expect(judge([b(600, 400, 460)])).toEqual([]); expect(judge([b(600, 400, 461)])).toEqual([]);
    expect(judge([b(600, 400, 462)])).toEqual([`the speech bubble ${q('Two short words.')} is one row 462 px wide (a bubble wraps at 460 px)`]);
    expect(judge([b(600, 400, 712)])).toEqual([`the speech bubble ${q('Two short words.')} is one row 712 px wide (a bubble wraps at 460 px)`]);
    expect(judge([b(600, 400, 490, 'Two rows.', 47)])).toEqual([]); expect(judge([docked])).toEqual([]);
  });
  test('at 3840 x 2160 the caps double: 920 px a row, 28 px inside the screen; the width is said at 1080', () => {
    const size: [number, number] = [3840, 2160];
    expect(judge([b(1200, 800, 920, 'Two short words.', 56)], 'rest', size)).toEqual([]); expect(judge([b(2634, 1904, 1178, 'Two short words.', 94)], 'rest', size)).toEqual([]); // t101-window-2x as clamped, as two rows
    expect(judge([b(2634, 1904, 1178, 'Two short words.', 56)], 'rest', size)).toEqual([`the speech bubble ${q('Two short words.')} is one row 589 px wide (a bubble wraps at 460 px)`]);
    expect(judge([b(2637, 1904, 1178, 'Two short words.', 94)], 'rest', size)).toEqual([`the speech bubble ${q('Two short words.')} runs to x 3815 ${edge}`]);
  });
});

describe('the slip\'s place (Glyph\'s r5 review, Splice\'s question 1): never in the news and Book box, never over the clock stack', () => {
  const t = load('talk-123'), s = load('slip-085'), slip0 = { ...s.man.elements.find(e => e.kind === 'card')!, kind: 'slip' as const }; // 0.2.290's dock: the clock stack, the news lozenge and the Book
  const blank: Image = (() => { const data = new Uint8Array(1920 * 1080 * 4); for (let i = 0; i < 1920 * 1080; i++) data.set([120, 120, 120, 255], i * 4); return { w: 1920, h: 1080, data }; })();
  const dock = t.man.elements.filter(e => e.kind !== 'speech'), noNews = dock.filter(e => e.kind !== 'card' && e.text !== 'card badge');
  const judge = (x: number, y: number, els = dock) => analyse(blank, { state: 'rest', rects: [{ x: 0, y: 0, w: 10, h: 10 }], manifest: { state: 'rest', elements: [...els, { ...slip0, x, y }] } }).failures;
  const q = `"${slip0.text!.trim().slice(0, 40)}…"`, inBox = `the slip ${q} sits in the news and Book box (the bottom right holds only the news and the Book)`, overClock = `the slip ${q} sits over the clock stack (the top right: the clock, the next event and the key people)`;
  test('the dock as 0.2.290 draws it: the news, its badge and the Book at the bottom right; the bar, the next event and the key people at the top right', () => {
    for (const n of ['card badge', 'book key', 'known people']) expect(dock.some(e => e.text === n)).toBe(true);
    expect(['card', 'bar', 'next'].every(k => dock.some(e => e.kind === k))).toBe(true);
  });
  test('where r085 drew it, over her head, it passes; so does a slip just clear of either corner', () => {
    expect(judge(slip0.x, slip0.y)).toEqual([]); expect(judge(1100, 850)).toEqual([]); expect(judge(1500, 840)).toEqual([]); expect(judge(1290, 170)).toEqual([]);
  });
  test('failing examples: in the news corner; over the clock stack; with the news down, over the Book alone', () => {
    expect(judge(1500, 900)).toEqual([inBox]); expect(judge(1560, 100)).toEqual([overClock]); expect(judge(1560, 0)).toEqual([overClock]);
    expect(judge(1500, 900, noNews)).toEqual([inBox]); expect(judge(1450, 900, noNews)).toEqual([]); // the news down: only the Book's corner, 1844 on
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
