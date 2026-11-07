// The book's own checks (state `book`; brief 098, Splice; GDD §10 "the book": a full-screen page over the real frozen street, in the light-page look, opened with the clock
// stopped, so the reading limits for play don't apply). The start page and the debrief are the book's pages, laid out on BookLayout's 1920 x 1080 grid: not the 070
// briefing page, so none of its parts (title, hook, objective, ways, stills, pins, Heat, Begin), and the street shows through the light page by design.
//   B1 listed  every word on the page is a manifest element with its text (the rule that does not move); a picture may hold a picture (a face's badge), never a word
//   B2 words   the page reads at most PAGE.words (220, glyph-budget §0 counting; Latch, briefs/099/latch/notes.md)
//   B3 grid    every piece on the screen, BOOK.edge clear of each edge; the body inside the grid's PAGE.safe (48 px) margins. The book's frame sits in those margins by the
//              grid's design (Latch's notes: the kicker 3 px in, the ways on and the clock chip where Glyph put them): the running head and the folio (the clock chip) above the
//              body's top, the ways on below its floor, the thumb index (the section signs) right of the text column. Anything else in a margin fails.
// Two words' ink over each other is the book's own audit (HudToolkit.BookAudit, exact from the labels), judged with the shot's audit, not re-read from pixels here.
// Sizes at 1080, scaled with the frame's height. Today's manifests say `page` for the book (HudToolkit.BookManifest's default): read as the book by its thumb index (isBook).
import type { Image } from './png';
import { glyphWords, textRuns, type Element, type Rect } from './budget';
import { PAGE, inkOf, type PageEl } from './page';

/** The book's grid (unity/Assets/Suborned/Runtime/Play/BookLayout.cs, HudToolkit.Book.cs; held to them by book.test.ts): the body's top (BookLayout.Top) and floor above the ways on
 *  (BookLayout.Debrief AgainFloor), the signs' column (BookLayout.SignX) less the current sign's half (108 px across), and the clock chip's inset from the top (HudToolkit.Book `top`). */
export const BOOK = { top: 60, floor: 952, signX: 1830, signHalf: 54, edge: 14, signs: ['Cell', 'People', 'Job', 'Journal', 'Menu'] } as const;
/** The share of a reader's run that listed words must cover for it to count as listed (B1). */
export const LISTED = 0.6;
const isText = (e: Element) => e.kind !== 'image' && !!e.text?.trim();
const within = (a: Rect, b: Rect) => a.x >= b.x && a.y >= b.y && a.x + a.w <= b.x + b.w && a.y + a.h <= b.y + b.h;
const centreIn = (a: Rect, b: Rect) => { const cx = a.x + a.w / 2, cy = a.y + a.h / 2; return cx >= b.x && cx <= b.x + b.w && cy >= b.y && cy <= b.y + b.h; };
const box = (e: Rect) => `${Math.round(e.x)},${Math.round(e.y)} ${Math.round(e.w)}x${Math.round(e.h)}`;
const name = (e: Element) => `the ${e.kind === 'image' ? 'picture' : 'text'} "${(e.text ?? '').slice(0, 32)}"`;

/** A page manifest that is the book's: three or more of its section signs as words in the thumb index, right of the text column. */
export function isBook(els: Element[], W: number, H: number): boolean {
  const s = H / 1080, x0 = (BOOK.signX - BOOK.signHalf) * s + (W - 1920 * s); // the page is anchored right
  return els.filter(e => isText(e) && (BOOK.signs as readonly string[]).includes(e.text!.trim()) && e.x >= x0).length >= 3;
}
export interface BookReport { failures: string[]; warnings: string[]; words: number }
/** The book's checks (B1 to B3). `mask` is the HUD layer's: the reader looks for words in it, outside the pictures. */
export function checkBook(shot: Image, mask: Uint8Array, elements: Element[]): BookReport {
  const W = shot.w, H = shot.h, s = H / 1080, dx = W - 1920 * s, failures: string[] = [], warnings: string[] = [];
  const els: PageEl[] = elements.filter(e => e.w >= 1 && e.h >= 1).map(e => ({ ...e, part: e.part ?? (e.kind === 'image' ? 'face' : 'text') }));
  const texts = els.filter(isText), pics = els.filter(e => e.kind === 'image'), ink = inkOf(shot, els);
  // B1 listed
  for (const p of pics) { const held = texts.filter(t => within(t, p)); if (held.length) failures.push(`${name(p)} at ${box(p)} holds ${held.length} word${held.length === 1 ? '' : 's'} ("${held[0]!.text!.slice(0, 24)}"): a picture over words hides them from the reader`); }
  const read = new Uint8Array(mask); for (const e of pics) { const x0 = Math.max(0, Math.floor(e.x)), x1 = Math.min(W, Math.ceil(e.x + e.w)); for (let y = Math.max(0, Math.floor(e.y)); y < Math.min(H, Math.ceil(e.y + e.h)); y++) read.fill(0, y * W + x0, y * W + x1); }
  // a run is listed when listed words cover most of it along its row: the reader joins two listed words across a narrow gap ("2:35 Rivals in"), and a kicker of spaced
  // capitals is listed a letter at a time ("N", "E", "X", "T"), so a run's centre can fall between them; a word no element covers stays unlisted
  const covered = (r: Rect) => { const cy = r.y + r.h / 2, xs = texts.filter(e => cy >= e.y && cy <= e.y + e.h).map(e => [Math.max(r.x, e.x), Math.min(r.x + r.w, e.x + e.w)] as const).filter(([a, b]) => b > a).sort((p, q) => p[0] - q[0]); let n = 0, end = r.x; for (const [a, b] of xs) { if (b > end) { n += b - Math.max(a, end); end = b; } } return n / Math.max(1, r.w); };
  for (const r of textRuns(shot, read).filter(r => r.h >= 7 * s && !texts.some(e => centreIn(r, e)) && covered(r) < LISTED)) failures.push(`unlisted words at ${r.x},${r.y} (${r.w}x${r.h}, about ${r.words} word${r.words === 1 ? '' : 's'}): every word on the page is a manifest element with its text`);
  // B2 words
  const words = texts.reduce((a, e) => a + glyphWords(e.text!), 0);
  if (words > PAGE.words) failures.push(`the page reads ${words} words (at most ${PAGE.words}, about a minute's reading)`);
  // B3 grid: a word by its ink (its label's box when the reader finds none), a picture by its rect
  const edge = BOOK.edge * s, safe = PAGE.safe * s, top = BOOK.top * s, floor = BOOK.floor * s, rail = (BOOK.signX - BOOK.signHalf) * s + dx;
  const frame = (r: Rect) => r.y + r.h / 2 < top || r.y >= floor || r.x + r.w / 2 >= rail; // the running head and folio, the ways on, the thumb index
  for (const e of [...texts, ...pics]) {
    const r = (isText(e) ? ink.get(e)?.box : undefined) ?? e;
    if (r.x + r.w <= 0 || r.y + r.h <= 0 || r.x >= W || r.y >= H) continue; // off the page (a hidden label): the host's audit says when that is wrong
    if (r.x < edge || r.y < edge || r.x + r.w > W - edge || r.y + r.h > H - edge) failures.push(`${name(e)} at ${box(r)} is within ${BOOK.edge} px of the screen's edge (the book's frame keeps that much)`);
    else if (!frame(r) && (r.x < safe || r.y < safe || r.x + r.w > W - safe || r.y + r.h > H - safe)) failures.push(`${name(e)} at ${box(r)} is in the grid's ${PAGE.safe} px margin: only the frame sits there (the running head and clock above the body, the ways on below it, the signs at the right)`);
  }
  if (!texts.length) failures.push('the page has no words in its manifest');
  return { failures, warnings, words };
}
