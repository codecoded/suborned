// The book's checks (state `book`, book.ts; brief 098, Splice): the start page and the debrief are the book's pages, held to the book's grid and word cap, not the 070
// briefing page's parts. A drawn book page proves each rule with its failing example; Latch's committed debrief manifests (briefs/099/latch/evidence) prove the book is
// told from the 070 page, and BookLayout.cs and HudToolkit.Book.cs hold the grid's numbers.
import { describe, expect, test } from 'bun:test';
import { readdirSync, readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { analyse, READING, type Element, type Manifest } from './budget';
import { BOOK, isBook, LISTED } from './book';
import { PAGE } from './page';
import type { Image } from './png';

const ROOT = resolve(import.meta.dir, '../..'), read = (f: string) => readFileSync(join(ROOT, f), 'utf8');
const t = (text: string, x: number, y: number, w: number, h: number): Element => ({ kind: 'hud', text, x, y, w, h });
const pic = (text: string, x: number, y: number, w: number, h: number): Element => ({ kind: 'image', text, x, y, w, h });
/** A debrief laid out on the book's grid: the running head and the clock chip above the body, the body inside the 48 px margins, the ways on below the floor, the signs right. */
function book(): Element[] {
  const kicker = [...'THE HANDOVER'].flatMap((c, i) => (c === ' ' ? [] : [t(c, 1010 + i * 20, 44, 14, 16)])); // spaced capitals, a letter at a time (as HudToolkit.BookStart sets them)
  return [...kicker, pic('picture', 1771, 25, 18, 18), t('5:53', 1795, 20, 47, 24), pic('book-hero', 640, 70, 330, 330),
    t('Their lookout got there first', 1010, 70, 600, 60), t('Their lookout saw the car and ran after it.', 1010, 150, 600, 30),
    t('Pickup', 1056, 243, 89, 30), t('The driver is turned by 5:20', 1052, 291, 282, 30), t('Spent', 1063, 577, 50, 28), t('Trail', 1230, 577, 36, 28),
    ...['Cell', 'People', 'Job', 'Journal', 'Menu'].flatMap((s, i) => [pic('picture', 1809, 166 + i * 176, 42, 42), t(s, 1806, 208 + i * 176, 48, 24)]),
    pic('picture', 1192, 1018, 30, 30), t('Watch this run', 1233, 1018, 153, 24), t('Back to start', 1483, 1018, 132, 24), t('Try again', 1680, 1012, 143, 32)];
}
/** The light page: cream, each word as dark strokes across the middle of its box (a stroke every 10 px, as sparse as lettering, so the reader takes them for letters), the pictures grey. */
function draw(els: Element[]): Image {
  const W = 1920, H = 1080, data = new Uint8Array(W * H * 4);
  for (let i = 0; i < W * H; i++) data.set([236, 232, 220, 255], i * 4);
  const fill = (x0: number, y0: number, x1: number, y1: number, v: number) => { for (let y = Math.max(0, y0); y < Math.min(H, y1); y++) for (let x = Math.max(0, x0); x < Math.min(W, x1); x++) data.set([v, v, v, 255], (y * W + x) * 4); };
  for (const e of els) if (e.kind === 'image') fill(e.x, e.y, e.x + e.w, e.y + e.h, 150);
  for (const e of els) if (e.kind === 'hud' && e.text) { const y0 = Math.round(e.y + e.h * 0.25), y1 = Math.round(e.y + e.h * 0.75); for (let x = e.x + 2; x < e.x + Math.min(e.w, 8 * (e.text.length + 1)) - 2; x += 10) fill(x, y0, x + 2, y1, 40); }
  return { w: W, h: H, data };
}
const judge = (els: Element[], drawn = els, state: 'book' | 'page' = 'book') => analyse(draw(drawn), { state, rects: drawn, manifest: { state, elements: els } }); // the HUD layer: what is drawn; the manifest: what it lists
const fails = (els: Element[], drawn = els) => judge(els, drawn).failures.join(' | ');

describe('the book state', () => {
  test('a reading surface with no coverage limit; the drawn debrief passes every book rule', () => {
    expect(READING).toContain('book');
    const r = judge(book()); expect(r.failures).toEqual([]); expect(r.state).toBe('book'); expect(r.limitPct).toBeNull(); expect(r.pageWords!).toBeLessThanOrEqual(PAGE.words);
  });
  test('a page manifest is the book when it carries the book\'s section signs: every debrief Latch shot is, the 070 briefing page is not', () => {
    const dir = join(ROOT, 'briefs/099/latch/evidence'), shots = readdirSync(dir).filter(f => f.endsWith('.manifest.json'));
    expect(shots.length).toBeGreaterThanOrEqual(9);
    for (const f of shots) { const m = JSON.parse(readFileSync(join(dir, f), 'utf8')) as Manifest; expect(m.state, f).toBe('page'); expect(isBook(m.elements, 1920, 1080), f).toBe(true); }
    expect(isBook((JSON.parse(read('tools/hud-budget/fixtures/page-130.manifest.json')) as Manifest).elements, 1920, 1080)).toBe(false);
    const r = judge(book(), book(), 'page'); expect(r.state).toBe('book'); expect(r.warnings.join()).toContain('read as the book');
  });
  test('the grid\'s numbers are the book\'s own: BookLayout.cs and HudToolkit.Book.cs', () => {
    const layout = read('unity/Assets/Suborned/Runtime/Play/BookLayout.cs'), debrief = read('unity/Assets/Suborned/Runtime/Play/BookLayout.Debrief.cs'), draw = read('unity/Assets/Suborned/Runtime/Play/HudToolkit.Book.cs');
    expect(layout).toMatch(new RegExp(`\\bTop = ${BOOK.top}f\\b`)); expect(layout).toMatch(new RegExp(`\\bSignX = ${BOOK.signX}f\\b`));
    expect(debrief).toMatch(new RegExp(`\\bAgainFloor = ${BOOK.floor}f\\b`));
    expect(draw).toContain(`float d = cur ? ${BOOK.signHalf * 2}f`); expect(draw).toContain(`chip.style.top = ${BOOK.edge}f`);
  });
});

describe('the book rules, each with a failing example', () => {
  test('B1 listed: a word the manifest leaves out fails; a run its listed words mostly cover (a kicker set a letter at a time, two words joined across a gap) does not', () => {
    const els = book();
    expect(fails(els, [...els, t('Unlisted words here', 1010, 700, 300, 26)])).toContain('unlisted words at');
    expect(fails(els.filter(e => e.text !== 'Trail'), els)).toContain('unlisted words at');
    expect(LISTED).toBe(0.6);
  });
  test('B1 listed: a picture over a word hides it', () => {
    expect(fails([...book(), pic('badge', 1050, 285, 300, 40)])).toContain('holds 1 word ("The driver is turned by ")');
  });
  test('B2 words: the page reads at most 220', () => {
    const els = book(), n = judge(els).pageWords!, filler = t(Array.from({ length: PAGE.words + 1 - n }, () => 'word').join(' '), 1010, 700, 600, 26);
    expect(fails([...els, filler])).toContain(`the page reads ${PAGE.words + 1} words (at most ${PAGE.words}`);
  });
  test('B3 grid: a body word in the 48 px margin fails (the start page\'s dial at x 44); the frame in its margins passes; anything within 14 px of the edge fails', () => {
    expect(fails([...book(), t('Guards', 44, 872, 63, 15)])).toContain('the text "Guards" at 46,876');
    expect(fails([...book(), t('Guards', 44, 872, 63, 15)])).toContain('is in the grid\'s 48 px margin');
    expect(fails(book().map(e => (e.text === 'Try again' ? { ...e, y: 1052 } : e)))).toContain('the text "Try again"'); // the foot row pushed to the edge
    expect(fails(book().map(e => (e.text === '5:53' ? { ...e, y: 6 } : e)))).toContain('within 14 px of the screen\'s edge');
    expect(fails(book().map(e => (e.text === 'Spent' ? { ...e, x: 1880 } : e)))).toContain('within 14 px');
  });
});
