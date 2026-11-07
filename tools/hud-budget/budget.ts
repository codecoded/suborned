// The HUD budget (brief 014 rule 5, Glyph): how much of a play screenshot the HUD covers, and whether it carries running text.
// Needs to tell HUD from world. Three ways, best first:
//   hud     an RGBA capture of the HUD layer alone (UI Toolkit panel drawn to a transparent target): alpha >= HUD_ALPHA is HUD.
//   world   the same frame captured with the HUD hidden: any pixel that differs is HUD.
//   rects   a list of screen rectangles (the UI tree's worldBound, or hand-drawn for an old shot): the union is HUD. Coarse (bounding boxes).
// The text check looks only inside the HUD mask. Speech and story beats are whitelisted by a manifest (rects with a kind), because a screenshot cannot tell
// a spoken line from a rule printed on a card. Without a manifest every running line fails, which is the safe answer.
import type { Image } from './png';
import { checkPage, type PagePart } from './page';
import { checkBook, isBook } from './book';
import { BUBBLE, CARD_MAX as CARDS, conversationLines, speechFault } from './speech';

/** `page`: a full-screen page outside play (the briefing page, GDD v0.27.0 §10), judged by its own checks (page.ts), never by coverage or the reading caps. `book`: the book's
 *  pages (the start page, the debrief), on their own checks (book.ts); a `page` manifest with the book's section signs is read as the book.
 *  `briefing`: RETIRED with the street overlay (brief 062 B, gone in v0.27.0); kept only so the archived street-briefing fixtures still judge as they were shot. */
export type State = 'rest' | 'active' | 'alt' | 'log' | 'briefing' | 'page' | 'book';
export interface Rect { x: number; y: number; w: number; h: number }
export type Kind = 'speech' | 'beat' | 'line' | 'pop' | 'marker' | 'image' | 'hud' | 'pill' | 'lock' | 'chip' | 'altline' | 'bar' | 'pin' | 'paper' | 'band' | 'card'; // pin, paper, band: the page's (page.ts); card: a conversation's cards
/** The kinds whose words the reader takes from the manifest and holds to their own caps (never as running text). A `bar`'s figures stay `hud`: they still face the running-text rules. */
export type TextKind = 'speech' | 'beat' | 'line' | 'pop' | 'marker' | 'pill' | 'lock' | 'chip' | 'altline' | 'card';
export const TEXT_KINDS: readonly Kind[] = ['speech', 'beat', 'line', 'pop', 'marker', 'pill', 'lock', 'chip', 'altline', 'card'];
export interface Element extends Rect {
  kind: Kind; text?: string; facts?: string[]; verbs?: string[];
  /** The page's pieces (state `page`, page.ts): its part (read from text and layout when absent), a pin's number or `car`, the rows as wrapped, and an id a child names as its `parent`. */
  part?: PagePart; n?: number | string; rows?: string[]; id?: string; parent?: string;
} // image: art with no text by construction (portraits, the plot scope): its strokes are not read. pop: a refusal pop (an icon and at most POP_MAX.words words, GDD §10). marker: a window marker (the seal: a lock and m:ss under the clock bar, GDD §10), never a goal or hint line pill, lock, chip, altline, bar: the order pill, the lock pill, the done chip (`facts`, `verbs`), the Alt line and the clock bar (glyph-budget.md; PILL_MAX and the rest below)
export interface Manifest { state?: State; elements: Element[] }
export interface Options { state: State; hud?: Image; world?: Image; rects?: Rect[]; manifest?: Manifest }
export interface Run { x: number; y: number; w: number; h: number; comps: number; words: number; kind: 'hud' | TextKind }
export interface Report {
  width: number; height: number; state: State; mask: 'hud' | 'world' | 'rects';
  coveragePct: number; footprintPct: number; /** null: the state has no coverage limit (`page`) */ limitPct: number | null;
  runs: Run[]; sentences: Run[]; blocks: Run[][]; speech: Run[]; beats: Run[]; lines: Run[]; pops: Run[]; markers: Run[];
  pills: Run[]; locks: Run[]; chips: Run[]; altlines: Run[]; bars: Run[];
  failures: string[]; warnings: string[]; pass: boolean;
  /** state `page`: the words the page reads (glyph-budget §0), its cap is page.ts PAGE.words */
  pageWords?: number;
}

/** Fail above this share of the screen at rest (brief 014: about 15%); an open order card and the Alt layer get more room. `log`: the history panel
 *  the player opens to read (Glyph; a reading surface, only while open): 18% (the open panel measures 17.0 to 17.5%), and its lines may be sentences.
 *  `briefing` (retired: the street overlay): the briefing card before play (title and copy): the same room and the same freedom. `page` has no limit: a full-screen page
 *  is 100% by design (glyph-page-budget §1). */
export const LIMIT: Record<Exclude<State, 'page' | 'book'>, number> = { rest: 15, active: 22, alt: 30, log: 18, briefing: 18 };
/** The states that are reading surfaces the player opens or meets before play (the history log; the briefing card): their lines are sentences by design. */
export const READING: State[] = ['log', 'briefing', 'page', 'book'];
/** The briefing's reading copy (its hook and objective: manifest `hud` elements of three or more words, on screen) is exempt from the running-text rule, so it gets its own cap: it is the one
 *  place the game may read a sentence, and a modal screen is where "the reading is too much" would creep back. Shipped 0.2.122: 2 lines, 13 words (hook 6, objective 7). [S] starting value. The
 *  objective is NOT a `line` here: a `line` is the in-play goal or hint (at most six words, one at a time); the briefing's objective is the dossier's sentence, and the play goal that follows Begin is the short line. */
export const BRIEFING_READING = { lines: 3, words: 20 };
/** Warn above this at rest: the goal is a bare screen. */
export const WARN_REST = 10;
export const HUD_ALPHA = 24, WORLD_DIFF = 10;
/** A line of three or more words is running text; a block is two or more stacked lines of them. Alt allows fragments of up to ALT_WORDS words. */
export const SENTENCE_WORDS = 3, SENTENCE_COMPS = 14, ALT_WORDS = 5;
export const SPEECH_MAX = BUBBLE, BEAT_MAX = { lines: 1, words: 6, onScreen: 2 }, LINE_MAX = { lines: 1, words: 6, onScreen: 1 }; // line = the objective or the Next Step hint: one imperative, at most six words, one at a time
/** A refusal pop (GDD §10 "Refusals: one pop of an icon and ≤ 4 words"): one line, at most four words, never a sentence of running text. The host keeps a pool of three, so three at once is the most a frame can show. [S] */
export const POP_MAX = { lines: 1, words: 4, onScreen: 3 };
/** A window marker (the seal clock, GDD §10: "a lock and m:ss under the clock bar"): a clock reading, not a line. At most MARKER_MAX.words words of text; it never counts as a goal or hint line. */
export const MARKER_MAX = { words: 2 };
/** A bare clock reading (m:ss or h:mm:ss). A `line` element whose text is only this is the seal's window marker emitted under the old kind: it is read as a marker, never as a second goal line. */
export const isClock = (t: string | undefined) => /^\s*\d{1,2}:\d{2}(:\d{2})?\s*$/.test(t ?? '');

// ---------------------------------------------------------------- the pieces of GDD v0.26.0 §10 with their own kinds (Glyph's limits, briefs/067/glyph-budget.md, 2026-10-26)
// Each keeps its words in front of the reader (glyph-r4 §4: never hidden as `hud` or `image`) and is held to its own cap, as `pop` and `marker` are.
/** The order pill (kind `pill`): the hold's one line on the doer, `<head> · <way> · <time>` plus price marks ("Hold · Drive into the service drive · 29 s"). One line, at most 8 words
 *  (head 1 or 2, way at most 5, exactly one time figure, at most 2 price marks), `active` only. It shares one slot on screen with the lock pill. [S] */
export const PILL_MAX = { lines: 1, words: 8, way: 5, times: 1, marks: 2 };
/** The lock pill (kind `lock`, brief 067 E3): the shut way on hover, `<way> · <reason>` ("Listen to the guard · Already heard"). One line, at most 7 words (way 5, reason 2), `active` only. [S] */
export const LOCK_MAX = { lines: 1, words: 7, way: 5, reason: 2 };
/** One pointer, one held way: at most one `pill` or `lock` on screen, in total. */
export const PILL_SLOT = 1;
/** The done chip (kind `chip`): the learn chip (face, facts, ›, verb pills). Facts at most 3 rows of at most 2 words; verbs one row of at most 3 one-word pills; at most 9 words a chip; at most 2 chips
 *  on screen; `rest` and `active`, and in `alt` never over a ring (an element whose text names a ring). Its rows are a list, not running text: inside a chip the sentence and block rules give way to these caps. [S] */
/** A conversation's cards (kind `card`, brief 085, conversations.schema.ts): what the player now knows and can do, as written: at most 4 rows of at most 6 words, one panel on screen. */
export const CARD_MAX = CARDS; // conversations.schema's rows and CARD_WORDS (speech.ts)
export const CHIP_MAX = { facts: 3, factWords: 2, verbs: 3, verbWords: 1, words: 9, onScreen: 2, states: ['rest', 'active'] as State[] };
/** A chip the manifest does not spell (`facts`/`verbs`; today's builds name it only in the parts, `gain-chip-N`): the reader counts its rows, at most 4 (3 facts and the verbs). Its words are
 *  not capped by the reader: at 1080 the verb pills' rims and the plate's screws read as letters (a two-pill row of "Help", "Pressure" reads 10 words), so a reader cap would fail every chip.
 *  Such a chip warns until the host spells it; spelled, its caps bite (CHIP_MAX). */
export const CHIP_READ = { rows: 4 };
/** The Alt line (kind `altline`): the consequence line under a hovered ring wedge in the Alt state. One line, at most 6 words, a fragment (no full stop inside it), one on screen, `alt` only,
 *  and it never overlaps other HUD (any other manifest rect but its own ring). [S] */
export const ALTLINE_MAX = { lines: 1, words: 6, onScreen: 1 };
/** The clock bar (kind `bar`, the status pill top right): one row of figures and icons, no word of letters, one on screen, in every state, drawn at play size (48 px tall at 1080, the rim
 *  included; the conversation cutaway too, Glyph §5). Its figures stay `hud` for the running-text rules; the bar adds its own. */
export const BAR_MAX = { rows: 1, onScreen: 1, h: 48, slack: 1.1, cap: 24 }; // cap: the end caps' width at 1080 (the pill's 24 px padding, HudToolkit.cs), rim art

const UNIT = /^(s|ms|m|%|px|x)$/;
/** Words as Glyph counts them (glyph-budget §0) for the new kinds: as shown, a keycap one word, the separator `·` not a word, and a figure with its unit one word (`8 s`, `1:55`, `$20,000`,
 *  `+2`); a segment that is only a label and a figure is one price mark (`siren 1`, `draws 1.0 s`). */
export function glyphWords(text: string): number { return segments(text).reduce((a, g) => a + segWords(g), 0); }
/** A pill's fields: its text split at the separator dots. */
export const segments = (text: string) => text.replace(/<[A-Za-z]+>/g, 'K').split(/\s*·\s*/).map(t => t.trim()).filter(Boolean);
export function segWords(seg: string): number {
  const t = seg.split(/\s+/).filter(x => /[\p{L}\p{N}]/u.test(x)), fig = (x: string) => /\d/.test(x);
  if (t.length >= 2 && t.length <= 3 && !fig(t[0]!) && fig(t[1]!) && (t.length === 2 || UNIT.test(t[2]!))) return 1; // `siren 1`, `draws 1.0 s`: one mark
  let n = 0; for (let i = 0; i < t.length; i++) { n++; if (fig(t[i]!) && i + 1 < t.length && UNIT.test(t[i + 1]!)) i++; } return n;
}
/** A time figure as a pill shows it: `8 s`, `29 s`, `1:55`. */
export const isTime = (seg: string) => /^\d+(\.\d+)?\s*s$/.test(seg.trim()) || isClock(seg);
const HEAD = /^(Hold|Let go)$/;

/** Today's manifests (0.2.125) carry the pills and the Alt line as `line`: they are recognised from their text shape until the host tags them (Latch, next build).
 *  - an order pill: a `·`-separated line headed `Hold` or `Let go`, or ending in a figure ("Hold · Board · 8 s", the walk-in's "to Inez · Listen · 13 s", "Aiming · 1 s");
 *  - a lock pill: any other `·`-separated line, ending in its reason ("Listen to the guard · Already heard"); the goal line and the hints never carry the dot;
 *  - an Alt line: in the `alt` state, a line longer than the goal line's six words or holding a full stop before more text ("Go without Vale calls off the attempt. …").
 *  A clock alone is the seal's marker (as before). Anything else stays a `line`. Its words are never hidden: each recognised piece keeps its text and its caps. */
export function recognise(e: Element, state: State): Element {
  if (e.kind !== 'line') return e;
  const t = e.text ?? '';
  if (isClock(t)) return { ...e, kind: 'marker' };
  if (/·/.test(t)) { const g = segments(t); return { ...e, kind: HEAD.test(g[0] ?? '') || /\d/.test(g[g.length - 1] ?? '') ? 'pill' : 'lock' }; } // a lock ends in its reason, words ("Already heard"); a pill ends in its time or a price mark
  if (state === 'alt' && (glyphWords(t) > LINE_MAX.words || /[.!?]\s+\S/.test(t))) return { ...e, kind: 'altline' };
  return e;
}

export function rectMask(w: number, h: number, rects: Rect[]): Uint8Array {
  const m = new Uint8Array(w * h);
  for (const r of rects) {
    const x0 = Math.max(0, Math.floor(r.x)), y0 = Math.max(0, Math.floor(r.y)), x1 = Math.min(w, Math.ceil(r.x + r.w)), y1 = Math.min(h, Math.ceil(r.y + r.h));
    for (let y = y0; y < y1; y++) m.fill(1, y * w + x0, y * w + x1);
  }
  return m;
}

/** Closing with a (2r+1) square: fills pin-holes where a panel's pixel happens to match the world behind it. */
function close(m: Uint8Array, w: number, h: number, r: number): Uint8Array {
  const pass = (src: Uint8Array, dil: boolean): Uint8Array => {
    const tmp = new Uint8Array(w * h), out = new Uint8Array(w * h);
    const want = dil ? 1 : 0;
    for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) { let hit = dil ? 0 : 1; for (let k = -r; k <= r; k++) { const xx = x + k; if (xx < 0 || xx >= w) continue; const v = src[y * w + xx]!; if (dil ? v : !v) { hit = want; break; } } tmp[y * w + x] = hit; }
    for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) { let hit = dil ? 0 : 1; for (let k = -r; k <= r; k++) { const yy = y + k; if (yy < 0 || yy >= h) continue; const v = tmp[yy * w + x]!; if (dil ? v : !v) { hit = want; break; } } out[y * w + x] = hit; }
    return out;
  };
  return pass(pass(m, true), false);
}

/** The refusal pops a picture's parts name (`pop-N`) that its manifest does not: `pop` elements, left out when off screen or already named. */
export function popsFromParts(parts: Record<string, number[]>, m: Manifest, size: { w: number; h: number }): Element[] {
  const named = m.elements.filter(e => e.kind === 'pop');
  return Object.entries(parts).filter(([k]) => /^pop-\d+$/.test(k)).map(([, r]) => ({ kind: 'pop' as const, x: r[0]!, y: r[1]!, w: r[2]!, h: r[3]! }))
    .filter(e => e.w >= 1 && e.h >= 1 && e.x + e.w > 0 && e.y + e.h > 0 && e.x < size.w && e.y < size.h && !named.some(n => Math.abs(n.x - e.x) < 2 && Math.abs(n.y - e.y) < 2));
}
const onScreen = (e: Rect, size: { w: number; h: number }) => e.w >= 1 && e.h >= 1 && e.x + e.w > 0 && e.y + e.h > 0 && e.x < size.w && e.y < size.h;
const overlaps = (a: Rect, b: Rect) => a.x < b.x + b.w && b.x < a.x + a.w && a.y < b.y + b.h && b.y < a.y + a.h;
/** The done chips a picture's parts name (`gain-chip-N`) that its manifest does not: `chip` elements with no `facts`/`verbs` (the reader counts their rows and words). Today's builds leave
 *  the chips out of the manifest; until the host names them, their words are read, never as running text (glyph-budget §3). */
export function chipsFromParts(parts: Record<string, number[]>, m: Manifest, size: { w: number; h: number }): Element[] {
  const named = m.elements.filter(e => e.kind === 'chip');
  return Object.entries(parts).filter(([k]) => /^gain-chip-\d+$/.test(k)).map(([, r]) => ({ kind: 'chip' as const, x: r[0]!, y: r[1]!, w: r[2]!, h: r[3]! }))
    .filter(e => onScreen(e, size) && !named.some(n => overlaps(n, e)));
}
/** The clock bar the parts name (`pill`, the status pill) when the manifest has no `bar`: one `bar` element, held to one row at play size. */
export function barFromParts(parts: Record<string, number[]>, m: Manifest, size: { w: number; h: number }): Element[] {
  const r = parts.pill; if (!r || m.elements.some(e => e.kind === 'bar')) return [];
  const e = { kind: 'bar' as const, x: r[0]!, y: r[1]!, w: r[2]!, h: r[3]! }; return onScreen(e, size) ? [e] : [];
}
/** Everything a picture's parts add to its manifest: the refusal pops, the done chips and the clock bar it does not name. */
export const fromParts = (parts: Record<string, number[]>, m: Manifest, size: { w: number; h: number }): Element[] => [...popsFromParts(parts, m, size), ...chipsFromParts(parts, m, size), ...barFromParts(parts, m, size)];
export function maskOf(shot: Image, o: Options): { mask: Uint8Array; how: Report['mask'] } {
  const { w, h } = shot, n = w * h;
  if (o.hud) {
    if (o.hud.w !== w || o.hud.h !== h) throw new Error(`HUD capture is ${o.hud.w}x${o.hud.h}, screenshot is ${w}x${h}`);
    const m = new Uint8Array(n); for (let i = 0; i < n; i++) m[i] = o.hud.data[i * 4 + 3]! >= HUD_ALPHA ? 1 : 0; return { mask: m, how: 'hud' };
  }
  if (o.world) {
    if (o.world.w !== w || o.world.h !== h) throw new Error(`world capture is ${o.world.w}x${o.world.h}, screenshot is ${w}x${h}`);
    const m = new Uint8Array(n);
    for (let i = 0; i < n; i++) { const a = i * 4; m[i] = Math.max(Math.abs(shot.data[a]! - o.world.data[a]!), Math.abs(shot.data[a + 1]! - o.world.data[a + 1]!), Math.abs(shot.data[a + 2]! - o.world.data[a + 2]!)) > WORLD_DIFF ? 1 : 0; }
    return { mask: close(m, w, h, 1), how: 'world' };
  }
  if (o.rects) return { mask: rectMask(w, h, o.rects), how: 'rects' };
  throw new Error('need one of: a HUD capture, a world capture, or a list of rects');
}

/** The screen the HUD blocks, as the union of the bounding boxes of its parts (parts closer than 6% of the width join). A second figure beside the pixel count. */
export function footprint(mask: Uint8Array, w: number, h: number): number {
  const cell = 8, gw = Math.ceil(w / cell), gh = Math.ceil(h / cell), g = new Uint8Array(gw * gh);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) if (mask[y * w + x]) g[(y >> 3) * gw + (x >> 3)] = 1;
  const lab = new Int32Array(gw * gh), join = Math.max(1, Math.round(w * 0.01 / cell)); let next = 0; const boxes: number[][] = [];
  for (let s = 0; s < gw * gh; s++) {
    if (!g[s] || lab[s]) continue; const id = ++next, st = [s]; lab[s] = id; const b = [gw, gh, -1, -1];
    while (st.length) {
      const c = st.pop()!, cx = c % gw, cy = (c / gw) | 0; b[0] = Math.min(b[0]!, cx); b[1] = Math.min(b[1]!, cy); b[2] = Math.max(b[2]!, cx); b[3] = Math.max(b[3]!, cy);
      for (let dy = -join; dy <= join; dy++) for (let dx = -join; dx <= join; dx++) { const x = cx + dx, y = cy + dy; if (x < 0 || y < 0 || x >= gw || y >= gh) continue; const t = y * gw + x; if (g[t] && !lab[t]) { lab[t] = id; st.push(t); } }
    }
    boxes.push(b);
  }
  const cover = new Uint8Array(gw * gh); for (const b of boxes) for (let y = b[1]!; y <= b[3]!; y++) cover.fill(1, y * gw + b[0]!, y * gw + b[2]! + 1);
  let n = 0; for (const v of cover) n += v; return (n * cell * cell) / (w * h) * 100;
}

export interface Comp { x0: number; y0: number; x1: number; y1: number; area: number }

/** Strokes: pixels of the HUD that differ strongly from their neighbourhood (text, glyph lines, rims), then 8-connected components of those. */
export function components(shot: Image, mask: Uint8Array, s: number): Comp[] {
  const { w, h } = shot, n = w * h, L = new Float32Array(n);
  for (let i = 0; i < n; i++) L[i] = 0.299 * shot.data[i * 4]! + 0.587 * shot.data[i * 4 + 1]! + 0.114 * shot.data[i * 4 + 2]!;
  const r = Math.max(3, Math.round(5 * s)), W = w + 1, I = new Float64Array(W * (h + 1));
  for (let y = 0; y < h; y++) { let row = 0; for (let x = 0; x < w; x++) { row += L[y * w + x]!; I[(y + 1) * W + x + 1] = I[y * W + x + 1]! + row; } }
  const bin = new Uint8Array(n);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    if (!mask[y * w + x]) continue;
    const x0 = Math.max(0, x - r), x1 = Math.min(w, x + r + 1), y0 = Math.max(0, y - r), y1 = Math.min(h, y + r + 1);
    const mean = (I[y1 * W + x1]! - I[y0 * W + x1]! - I[y1 * W + x0]! + I[y0 * W + x0]!) / ((x1 - x0) * (y1 - y0));
    if (Math.abs(L[y * w + x]! - mean) > 45) bin[y * w + x] = 1;
  }
  // panel rims, card outlines, bars: long straight runs are never letters; cut them so they cannot swallow the text beside them
  const RUN = Math.round(26 * s);
  for (let y = 0; y < h; y++) { let x = 0; while (x < w) { if (!bin[y * w + x]) { x++; continue; } let e = x; while (e < w && bin[y * w + e]) e++; if (e - x >= RUN) bin.fill(0, y * w + x, y * w + e); x = e; } }
  for (let x = 0; x < w; x++) { let y = 0; while (y < h) { if (!bin[y * w + x]) { y++; continue; } let e = y; while (e < h && bin[e * w + x]) e++; if (e - y >= RUN) for (let t = y; t < e; t++) bin[t * w + x] = 0; y = e; } }
  const lab = new Int32Array(n), comps: Comp[] = []; const st: number[] = [];
  for (let i = 0; i < n; i++) {
    if (!bin[i] || lab[i]) continue; const id = comps.length + 1; lab[i] = id; st.length = 0; st.push(i);
    const c: Comp = { x0: w, y0: h, x1: -1, y1: -1, area: 0 };
    while (st.length) {
      const p = st.pop()!, px = p % w, py = (p / w) | 0; c.area++; if (px < c.x0) c.x0 = px; if (px > c.x1) c.x1 = px; if (py < c.y0) c.y0 = py; if (py > c.y1) c.y1 = py;
      for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) { const x = px + dx, y = py + dy; if (x < 0 || y < 0 || x >= w || y >= h) continue; const t = y * w + x; if (bin[t] && !lab[t]) { lab[t] = id; st.push(t); } }
    }
    comps.push(c);
  }
  return comps;
}

/** A row of pips: three or more small filled marks of one size at one pitch (a fighter's health, HudToolkit.Less). Lettering is never this even: its strokes differ in width or spacing by more than a pixel. */
export function isPipRow(cs: Comp[], s: number): boolean {
  if (cs.length < 3) return false;
  const wd = (c: Comp) => c.x1 - c.x0 + 1, ht = (c: Comp) => c.y1 - c.y0 + 1, spread = (v: number[]) => Math.max(...v) - Math.min(...v);
  if (ht(cs[0]!) > 9 * s || wd(cs[0]!) > 9 * s) return false;
  if (spread(cs.map(wd)) > 1 || spread(cs.map(ht)) > 1 || spread(cs.map(c => c.y0)) > 1) return false;
  if (cs.some(c => c.area / (wd(c) * ht(c)) < 0.5)) return false;
  return spread(cs.slice(1).map((c, i) => c.x0 - cs[i]!.x0)) <= 1;
}

/** Text-height strokes chained into runs. At 1080p letters touch, so a stroke blob is a letter or a whole word; a run is cut into words at the gaps wider than a
 *  letter-space, and its letters are estimated from the blobs' widths. Icons (taller, squarer) and bars (solid fill) are not text. */
export function textRuns(shot: Image, mask: Uint8Array): Run[] {
  const s = shot.h / 1080, minH = 5 * s, maxH = 34 * s, iconH = 12 * s, wordGap = Math.max(2.6 * s, 2.6);
  const all = components(shot, mask, s), isIcon = (c: Comp) => { const h = c.y1 - c.y0 + 1, w = c.x1 - c.x0 + 1; return h >= iconH && h <= 44 * s && w >= 0.7 * h && w <= 1.35 * h; };
  const icons = all.filter(isIcon), within = (c: Comp, i: Comp) => c !== i && c.x0 >= i.x0 - 2 && c.x1 <= i.x1 + 2 && c.y0 >= i.y0 - 2 && c.y1 <= i.y1 + 2;
  const bl = all.filter(c => {
    const h = c.y1 - c.y0 + 1, w = c.x1 - c.x0 + 1;
    if (isIcon(c) || icons.some(i => within(c, i))) return false; // a glyph and the strokes inside it are not letters
    if (w >= 40 * s && c.area / (w * h) < 0.18) return false; // a long, sparse diagonal is a line drawn on the HUD layer (a tracer, a sightline), never a word: lettering fills a fifth of its box or more
    return h >= minH && h <= maxH && w <= 16 * h && c.area >= 6 && (w <= 2 * h || c.area / (w * h) <= 0.75);
  }).sort((a, b) => a.x0 - b.x0);
  const letters = (c: Comp) => Math.max(1, Math.round((c.x1 - c.x0 + 1) / (0.75 * (c.y1 - c.y0 + 1))));
  const runs: { cs: Comp[] }[] = [];
  for (const c of bl) {
    const h = c.y1 - c.y0 + 1, cy = (c.y0 + c.y1) / 2; let best: { cs: Comp[] } | undefined, bg = Infinity;
    for (const r of runs) {
      const l = r.cs[r.cs.length - 1]!, lh = l.y1 - l.y0 + 1, ly = (l.y0 + l.y1) / 2, gap = c.x0 - l.x1 - 1;
      if (Math.abs(cy - ly) <= 0.6 * Math.max(h, lh) && gap >= -2 && gap <= 0.9 * Math.max(h, lh) && gap < bg) { best = r; bg = gap; }
    }
    if (best) best.cs.push(c); else runs.push({ cs: [c] });
  }
  const out: Run[] = [];
  for (const r of runs) {
    if (isPipRow(r.cs, s)) continue; // a health row (brief 078): 8 dots over a fighter read as three words at (929,468) of fight-loud-checkpoint
    const cs = r.cs, x0 = Math.min(...cs.map(c => c.x0)), x1 = Math.max(...cs.map(c => c.x1)), y0 = Math.min(...cs.map(c => c.y0)), y1 = Math.max(...cs.map(c => c.y1)), h = y1 - y0 + 1, w = x1 - x0 + 1;
    let words = 1; for (let i = 1; i < cs.length; i++) if (cs[i]!.x0 - cs[i - 1]!.x1 - 1 >= wordGap) words++;
    const comps = cs.reduce((a, c) => a + letters(c), 0);
    words = Math.max(words, Math.ceil(comps / 6)); // touching letters hide word gaps: about six letters to a word
    // an icon that fell apart into small strokes is not text: about as wide as tall, few strokes
    if (comps < 2 || (w / h < 1.25 && comps < 4) || (cs.length === 1 && w / h < 1.8)) continue;
    out.push({ x: x0, y: y0, w, h, comps, words, kind: 'hud' });
  }
  return out.sort((a, b) => a.y - b.y || a.x - b.x);
}

const inside = (r: Run, e: Rect) => { const cx = r.x + r.w / 2, cy = r.y + r.h / 2; return cx >= e.x && cx <= e.x + e.w && cy >= e.y && cy <= e.y + e.h; };
const wordsOf = (t: string) => t.trim().split(/\s+/).filter(Boolean).length;
/** The words a speech element says: its speaker's name is a label, not a word of the line (GDD v0.35.0 §10, bubbles at most 12 words). The manifest joins name and line two ways: the feed `Inez: Hush.`, the
 *  conversation bubble `Inez Hush.` (HudToolkit.Convo). The bare form is told by two capitalised words at the start, so a line that opens so counts one word short; that errs by one word only, never by a name. */
export const speechWords = (t: string) => wordsOf(t.replace(/^\s*(?:\p{Lu}[\p{L}'’.-]*\s+)?\p{Lu}[\p{L}'’-]*:\s+/u, '').replace(/^\s*\p{Lu}[\p{L}'’-]*\s+(?=\p{Lu})/u, ''));
/** The words of a hint or goal line as the budget counts them (brief 016): the line as shown, a keycap counting as a word (`Turn twice: Q Q` is four). `short` is text.json's form, with [ringed nouns] and <KeyTokens>; a key token is one word. text.test.ts counts with this, so the two cannot disagree. */
export const shownWords = (short: string) => wordsOf(short.replace(/\{[a-z]+\}/g, 'them').replace(/<[A-Za-z]+>/g, 'K').replace(/[[\]]/g, ''));

/** Stacked runs that belong together: a line below another, close, overlapping in x. */
export function stack(runs: Run[]): Run[][] {
  const used = new Set<Run>(), blocks: Run[][] = [];
  for (const r of runs) {
    if (used.has(r)) continue; const b = [r]; used.add(r); let grew = true;
    while (grew) { grew = false; for (const q of runs) { if (used.has(q)) continue; const last = b[b.length - 1]!, gap = q.y - (last.y + last.h), ov = Math.min(q.x + q.w, last.x + last.w) - Math.max(q.x, last.x); if (gap >= -2 && gap <= 1.3 * Math.max(q.h, last.h) && ov >= 0.4 * Math.min(q.w, last.w)) { b.push(q); used.add(q); grew = true; } } }
    blocks.push(b);
  }
  return blocks;
}

/** Text rows of a piece: runs whose centres lie within 8 px (at 1080) share a row. A row of a few strokes beside a row of a whole line is the panel's rim meeting a busy picture behind it
 *  (dark foliage under a bright plate edge breaks into letter-sized strokes), not a second line: it needs a quarter of the strongest row's strokes, and at least three. A run under 0.6 of the
 *  tallest is a fragment of the rim, not a line (Glyph's review). */
export function rowsOf(runs: Run[], rim = true): { y: number; comps: number; words: number }[] {
  const tall = Math.max(0, ...runs.map(r => r.h)), es = rim ? runs.filter(r => r.h >= 0.6 * tall) : runs; // rim = false: rows of different sizes (a chip's fact rows under its taller verb pills) all count
  const rs = [...es].sort((a, b) => a.y + a.h / 2 - (b.y + b.h / 2)), rows: { y: number; comps: number; words: number }[] = [];
  for (const r of rs) { const y = r.y + r.h / 2, last = rows[rows.length - 1]; if (last && y - last.y <= 8) { last.comps += r.comps; last.words += r.words; last.y = y; } else rows.push({ y, comps: r.comps, words: r.words }); }
  const top = Math.max(0, ...rows.map(r => r.comps));
  return rows.filter(r => r.comps >= Math.max(3, 0.25 * top));
}
/** Rows as bands: runs whose vertical extents overlap (chained) are one band. The bar's figures differ in size (the clock is larger) and its screws sit beside them, so a band, not a
 *  centre within 8 px, is a row there; a wrapped bar has two bands with a clear gap between them. Runs of fewer than three strokes are not counted. */
export function bands(runs: Run[]): number {
  const rs = runs.filter(r => r.comps >= 3).sort((a, b) => a.y - b.y); let n = 0, end = -Infinity;
  for (const r of rs) { if (r.y > end) { n++; end = r.y + r.h; } else end = Math.max(end, r.y + r.h); }
  return n;
}
/** A done chip's words when the manifest spells it (facts and verbs); undefined when only the reader can count it. */
export const chipWords = (e: Element): number | undefined => e.facts || e.verbs ? [...(e.facts ?? []), ...(e.verbs ?? [])].reduce((a, x) => a + glyphWords(x), 0) : undefined;

export function analyse(shot: Image, o: Options): Report {
  const { w, h } = shot, { mask, how } = maskOf(shot, o); let n = 0; for (const v of mask) n += v;
  const coveragePct = (n / (w * h)) * 100, failures: string[] = [], warnings: string[] = [];
  const all = textRuns(shot, mask);
  if (o.state === 'page' || o.state === 'book') { // the page's or the book's own checks (page.ts, book.ts): no coverage limit, no running-text, block or reading cap
    const book = o.state === 'book' || isBook(o.manifest?.elements ?? [], w, h), pg = book ? checkBook(shot, mask, o.manifest?.elements ?? []) : checkPage(shot, mask, o.manifest?.elements ?? [], () => paperTexture(shot, mask));
    if (book && o.state === 'page') pg.warnings.unshift('the manifest says `page`; read as the book by its section signs (HudToolkit.BookManifest\'s default; `book` once the host writes it)');
    if (!o.manifest) pg.failures.unshift('a page needs its manifest: every word on it is a manifest element');
    return { width: w, height: h, state: book ? 'book' : 'page', mask: how, coveragePct, footprintPct: footprint(mask, w, h), limitPct: null, runs: all, sentences: [], blocks: [], speech: [], beats: [], lines: [], pops: [], markers: [], pills: [], locks: [], chips: [], altlines: [], bars: [], failures: pg.failures, warnings: pg.warnings, pass: pg.failures.length === 0, pageWords: pg.words };
  }
  const limitPct = LIMIT[o.state], els = (o.manifest?.elements ?? []).map(e => recognise(e, o.state)); // today's pills, Alt line and seal clock arrive as `line`: read by their text shape (recognise)
  const owner = new Map<Run, Element>(); // the manifest element a run lies in: a bubble's words are counted once, however many runs it breaks into (a pin's stem crossing it made two)
  const wordsIn = (b: Run[]) => { const seen = new Set<Element>(); let n = 0; for (const r of b) { const e = owner.get(r); if (e) { if (!seen.has(e)) { seen.add(e); n += r.words; } } else n += r.words; } return n; };
  for (const r of all) { const e = els.find(e => TEXT_KINDS.includes(e.kind) && inside(r, e)); if (e) { r.kind = e.kind as TextKind; } }
  for (const e of els) { if (e.text && (e.kind === 'speech' || e.kind === 'beat' || e.kind === 'line' || e.kind === 'pop' || e.kind === 'marker')) for (const r of all) if (inside(r, e)) { r.words = e.kind === 'speech' ? speechWords(e.text) : wordsOf(e.text); owner.set(r, e); } }
  for (const e of els) { const n = e.kind === 'chip' ? chipWords(e) : (e.kind === 'pill' || e.kind === 'lock' || e.kind === 'altline') && e.text ? glyphWords(e.text) : undefined; if (n !== undefined) for (const r of all) if (r.kind === e.kind && inside(r, e)) { r.words = n; owner.set(r, e); } } // the new kinds count as Glyph counts (glyph-budget §0)
  const art = els.filter(e => e.kind === 'image'), pillRuns = all.filter(r => r.kind === 'pill'), lockRuns = all.filter(r => r.kind === 'lock'), chipRuns = all.filter(r => r.kind === 'chip'), altRuns = all.filter(r => r.kind === 'altline'), hudRuns = all.filter(r => r.kind === 'hud' && !art.some(e => inside(r, e))), speech = all.filter(r => r.kind === 'speech'), beats = all.filter(r => r.kind === 'beat'), lineRuns = all.filter(r => r.kind === 'line'), popRuns = all.filter(r => r.kind === 'pop'), markerRuns = all.filter(r => r.kind === 'marker');
  const isSentence = (r: Run) => (o.state === 'alt' ? r.words > ALT_WORDS : r.words >= SENTENCE_WORDS && r.comps >= SENTENCE_COMPS);
  const sentences = READING.includes(o.state) ? [] : hudRuns.filter(isSentence); // the open history is read, so its lines are sentences by design
  const blocks = READING.includes(o.state) ? [] : stack(hudRuns.filter(r => r.words >= 2 && r.comps >= 6)).filter(b => b.length >= 2 && b.reduce((a, r) => a + r.words, 0) >= 4);
  if (coveragePct > limitPct) failures.push(`HUD covers ${coveragePct.toFixed(1)}% of the screen (limit ${limitPct}% ${o.state})`);
  else if (o.state === 'rest' && coveragePct > WARN_REST) warnings.push(`HUD covers ${coveragePct.toFixed(1)}% at rest (aim under ${WARN_REST}%)`);
  if (o.state === 'briefing') warnings.push('the `briefing` state is retired with the street overlay (GDD v0.27.0 §10): the briefing page is tagged `page` and judged by its own checks');
  if (o.state === 'briefing' && o.manifest) { // the reading surface's own budget (see BRIEFING_READING); an element outside the screen (a hidden place label) is not on show
    const read = els.filter(e => e.kind === 'hud' && e.text && wordsOf(e.text) >= SENTENCE_WORDS && e.x < w && e.y < h && e.x + e.w > 0 && e.y + e.h > 0), words = read.reduce((a, e) => a + wordsOf(e.text!), 0);
    if (read.length > BRIEFING_READING.lines || words > BRIEFING_READING.words) failures.push(`the briefing reads ${read.length} sentence${read.length === 1 ? '' : 's'}, ${words} words (at most ${BRIEFING_READING.lines} and ${BRIEFING_READING.words})`);
  }
  if (sentences.length) failures.push(`${sentences.length} line${sentences.length > 1 ? 's' : ''} of running text that is not speech`);
  if (blocks.length) failures.push(`${blocks.length} block${blocks.length > 1 ? 's' : ''} of stacked text`);
  // Text rows of a line or beat: runs whose centres lie within 8 px share a row. A row of a few strokes beside a row of a whole line is the panel's rim
  // meeting a busy picture behind it (dark foliage under a bright plate edge breaks into letter-sized strokes), not a second line: it needs a quarter of
  // the strongest row's strokes, and at least three.
  const lines = (rs: Run[]) => rowsOf(rs).length;
  const named = (b: Run[]) => b.some(r => { const t = owner.get(r)?.text; return !!t && speechWords(t) < wordsOf(t) && !/^\s*[^:]*:\s/.test(t); }) ? 1 : 0; // the conversation bubble sets its speaker's name on a row of its own: a label, not a line of speech
  const said = conversationLines(); // a conversation's line has the cutaway's caps, a bubble the bubble's (speech.ts, from the text schema)
  for (const b of speech.length ? stack(speech) : []) { const f = speechFault(b.map(r => owner.get(r)?.text).find(Boolean) ?? '', b.length - named(b), wordsIn(b), said); if (f) failures.push(f); }
  { const cards = els.filter(e => e.kind === 'card' && e.text && e.w > 0); if (cards.length > CARD_MAX.onScreen) failures.push(`${cards.length} card panels on screen (at most ${CARD_MAX.onScreen})`); if (cards.some(e => wordsOf(e.text!) > CARD_MAX.words)) failures.push(`the cards run over ${CARD_MAX.words} words`); }
  if (beats.length) {
    const bs = els.filter(e => e.kind === 'beat'); if (bs.length > BEAT_MAX.onScreen) failures.push(`${bs.length} story beats on screen (at most ${BEAT_MAX.onScreen})`);
    if (beats.some(r => r.words > BEAT_MAX.words)) failures.push(`a story beat is longer than ${BEAT_MAX.words} words`);
    if (bs.some(e => beats.filter(r => inside(r, e)).length > BEAT_MAX.lines && lines(beats.filter(r => inside(r, e))) > BEAT_MAX.lines)) failures.push('a story beat runs over more than one line');
  }
  if (lineRuns.length) {
    const ls = els.filter(e => e.kind === 'line'); if (ls.length > LINE_MAX.onScreen) failures.push(`${ls.length} objective or hint lines on screen (at most ${LINE_MAX.onScreen})`);
    if (lineRuns.some(r => r.words > LINE_MAX.words)) failures.push(`the objective or hint line is longer than ${LINE_MAX.words} words`);
    if (lines(lineRuns) > LINE_MAX.lines) failures.push('the objective or hint line runs over more than one line');
  }
  if (popRuns.length) {
    const ps = els.filter(e => e.kind === 'pop'), seen = new Set(ps.map(e => `${e.x},${e.y},${e.w},${e.h}`)); // pops drawn on one spot are one place to read
    if (seen.size > POP_MAX.onScreen) failures.push(`${seen.size} refusal pops on screen (at most ${POP_MAX.onScreen})`);
    if (ps.some(e => wordsIn(popRuns.filter(r => inside(r, e))) > POP_MAX.words)) failures.push(`a refusal pop is longer than ${POP_MAX.words} words`);
    if (ps.some(e => popRuns.filter(r => inside(r, e)).length > POP_MAX.lines && lines(popRuns.filter(r => inside(r, e))) > POP_MAX.lines)) failures.push('a refusal pop runs over more than one line');
  }
  const bigMarker = els.find(e => e.kind === 'marker' && e.text && wordsOf(e.text) > MARKER_MAX.words);
  if (bigMarker) failures.push(`a window marker reads "${bigMarker.text}": a lock and m:ss, at most ${MARKER_MAX.words} words`);
  // the order pill and the lock pill (glyph-budget §1, §2): one slot, one line each, active only
  const pills = els.filter(e => e.kind === 'pill'), locks = els.filter(e => e.kind === 'lock'), sOf = (e: Element) => `"${e.text ?? '(no text)'}"`;
  if (pills.length + locks.length > PILL_SLOT) failures.push(`${pills.length + locks.length} pills on screen (an order pill and a lock pill share one slot: at most ${PILL_SLOT})`);
  for (const e of pills) {
    const rs = pillRuns.filter(r => inside(r, e)), what = `the order pill ${sOf(e)}`;
    if (o.state !== 'active') failures.push(`${what} shows in a ${o.state} frame (a pill is active only)`);
    if (rs.length && lines(rs) > PILL_MAX.lines) failures.push(`${what} wraps over more than one line`);
    if (e.text) {
      const g = segments(e.text), head = HEAD.test(g[0] ?? '') ? 1 : 0, way = g[head], rest = g.slice(head + 1), times = rest.filter(isTime).length, marks = rest.length - times;
      if (glyphWords(e.text) > PILL_MAX.words) failures.push(`${what} is ${glyphWords(e.text)} words (at most ${PILL_MAX.words})`);
      if (way && segWords(way) > PILL_MAX.way) failures.push(`${what}: its way is ${segWords(way)} words (at most ${PILL_MAX.way})`);
      if (times > PILL_MAX.times) failures.push(`${what} shows its time ${times} times (once only)`);
      if (marks > PILL_MAX.marks) failures.push(`${what} carries ${marks} price marks (at most ${PILL_MAX.marks})`);
    } else if (rs.some(r => r.words > PILL_MAX.words)) failures.push(`${what} is longer than ${PILL_MAX.words} words`);
  }
  for (const e of locks) {
    const rs = lockRuns.filter(r => inside(r, e)), what = `the lock pill ${sOf(e)}`;
    if (o.state !== 'active') failures.push(`${what} shows in a ${o.state} frame (a pill is active only)`);
    if (rs.length && lines(rs) > LOCK_MAX.lines) failures.push(`${what} wraps over more than one line`);
    if (e.text) {
      const g = segments(e.text), n = glyphWords(e.text), way = g.length > 1 ? g.slice(0, -1).reduce((a, x) => a + segWords(x), 0) : n, reason = g.length > 1 ? segWords(g[g.length - 1]!) : 0;
      if (n > LOCK_MAX.words) failures.push(`${what} is ${n} words (at most ${LOCK_MAX.words})`);
      if (way > LOCK_MAX.way) failures.push(`${what}: its way is ${way} words (at most ${LOCK_MAX.way})`);
      if (reason > LOCK_MAX.reason) failures.push(`${what}: its reason is ${reason} words (at most ${LOCK_MAX.reason})`);
    } else if (rs.some(r => r.words > LOCK_MAX.words)) failures.push(`${what} is longer than ${LOCK_MAX.words} words`);
  }
  // the done chips (glyph-budget §3): a list of facts and verbs, never running text, held to their own caps
  const chips = els.filter(e => e.kind === 'chip');
  if (chips.length > CHIP_MAX.onScreen) failures.push(`${chips.length} done chips on screen (at most ${CHIP_MAX.onScreen})`);
  const rings = els.filter(e => e.kind !== 'chip' && /\bring\b/i.test(e.text ?? '') && onScreen(e, { w, h }));
  if (chips.length && o.state !== 'alt' && !CHIP_MAX.states.includes(o.state)) failures.push(`a done chip shows in a ${o.state} frame (rest and active only)`);
  if (o.state === 'alt' && chips.some(c => rings.some(r => overlaps(c, r)))) failures.push('a done chip shows over a ring in the Alt state (never in alt over a ring)');
  for (const e of chips) {
    const at = `the done chip at ${e.x},${e.y}`;
    if (e.facts || e.verbs) {
      const f = e.facts ?? [], v = e.verbs ?? [];
      if (f.length > CHIP_MAX.facts) failures.push(`${at} has ${f.length} fact rows (at most ${CHIP_MAX.facts})`);
      for (const x of f) if (glyphWords(x) > CHIP_MAX.factWords) failures.push(`${at}: the fact "${x}" is ${glyphWords(x)} words (at most ${CHIP_MAX.factWords})`);
      if (v.length > CHIP_MAX.verbs) failures.push(`${at} has ${v.length} verb pills (at most ${CHIP_MAX.verbs})`);
      for (const x of v) if (glyphWords(x) > CHIP_MAX.verbWords) failures.push(`${at}: the verb pill "${x}" is ${glyphWords(x)} words (at most ${CHIP_MAX.verbWords})`);
      const n = chipWords(e) ?? 0; if (n > CHIP_MAX.words) failures.push(`${at} is ${n} words (at most ${CHIP_MAX.words})`);
    } else {
      const rows = rowsOf(chipRuns.filter(r => inside(r, e)), false);
      if (rows.length > CHIP_READ.rows) failures.push(`${at} reads as ${rows.length} rows (at most ${CHIP_MAX.facts} facts and the verbs)`);
      warnings.push(`${at} is not spelled in the manifest (no facts or verbs): its rows are counted, its word caps wait for the host to name them`);
    }
  }
  // the Alt line (glyph-budget §4): alt only, one line, a fragment of at most six words, never over other HUD
  const alts = els.filter(e => e.kind === 'altline');
  if (alts.length > ALTLINE_MAX.onScreen) failures.push(`${alts.length} Alt lines on screen (at most ${ALTLINE_MAX.onScreen}, for the hovered wedge)`);
  for (const e of alts) {
    const rs = altRuns.filter(r => inside(r, e)), what = `the Alt line ${sOf(e)}`;
    if (o.state !== 'alt') failures.push(`${what} shows in a ${o.state} frame (Alt only)`);
    if (rs.length && lines(rs) > ALTLINE_MAX.lines) failures.push(`${what} wraps over more than one line`);
    if (e.text && glyphWords(e.text) > ALTLINE_MAX.words) failures.push(`${what} is ${glyphWords(e.text)} words (at most ${ALTLINE_MAX.words})`);
    else if (!e.text && rs.some(r => r.words > ALTLINE_MAX.words)) failures.push(`${what} is longer than ${ALTLINE_MAX.words} words`);
    if (e.text && /[.!?]\s*\S/.test(e.text)) failures.push(`${what} is a sentence: a full stop inside it (a fragment only)`);
    const hit = els.filter(x => x !== e && !/\bring\b/i.test(x.text ?? '') && onScreen(x, { w, h }) && overlaps(x, e));
    if (hit.length) failures.push(`${what} overlaps ${hit.map(x => `the ${x.kind} "${(x.text ?? '').slice(0, 30)}"`).join(', ')}`);
  }
  // the clock bar (glyph-budget §5): one row of figures, at play size, one on screen
  const cap = BAR_MAX.cap * (h / 1080), inBar = (r: Run, e: Rect) => inside(r, e) && r.x + r.w / 2 > e.x + cap && r.x + r.w / 2 < e.x + e.w - cap; // the end caps' rims and screws are art, not figures
  const bars = els.filter(e => e.kind === 'bar'), barRuns = all.filter(r => bars.some(e => inBar(r, e)) && !art.some(e => inside(r, e))); // the lamps and pause glyph are art too (the host's image rect)
  if (bars.length > BAR_MAX.onScreen) failures.push(`${bars.length} clock bars on screen (at most ${BAR_MAX.onScreen})`);
  for (const e of bars) {
    const rows = bands(barRuns.filter(r => inBar(r, e))), scale = e.h / (BAR_MAX.h * (h / 1080));
    if (scale > BAR_MAX.slack) failures.push(`the clock bar is drawn at ${scale.toFixed(1)}x play size (${e.h} px tall; play size is ${BAR_MAX.h} px at 1080)`);
    if (rows > BAR_MAX.rows) failures.push(`the clock bar reads as ${rows} rows (one row of figures)`);
    const word = e.text?.split(/\s+/).find(x => /\p{L}{2,}/u.test(x)); if (word) failures.push(`the clock bar carries the word "${word}" (figures and icons only)`);
  }
  return { width: w, height: h, state: o.state, mask: how, coveragePct, footprintPct: footprint(mask, w, h), limitPct, runs: hudRuns, sentences, blocks, speech, beats, lines: lineRuns, pops: popRuns, markers: markerRuns, pills: pillRuns, locks: lockRuns, chips: chipRuns, altlines: altRuns, bars: barRuns, failures, warnings, pass: failures.length === 0 };
}

/** The share (%) of the frame outside the HUD layer (and 2 px clear of it) that is textured, not flat: a page's paper reads about 1%, the street 25 to 45% (0.2.130). Sampled every 2 px. */
export function paperTexture(shot: Image, mask: Uint8Array): number {
  const W = shot.w, H = shot.h, L = (i: number) => 0.299 * shot.data[i * 4]! + 0.587 * shot.data[i * 4 + 1]! + 0.114 * shot.data[i * 4 + 2]!; let n = 0, tex = 0;
  for (let y = 2; y < H - 2; y += 2) for (let x = 2; x < W - 2; x += 2) {
    let hud = false; for (let dy = -2; dy <= 2 && !hud; dy++) for (let dx = -2; dx <= 2; dx++) if (mask[(y + dy) * W + x + dx]) { hud = true; break; }
    if (hud) continue; n++; const i = y * W + x, l = L(i); let mx = 0; for (const d of [1, -1, W, -W, 2, -2, 2 * W, -2 * W]) mx = Math.max(mx, Math.abs(L(i + d) - l)); if (mx > 6) tex++;
  }
  return n ? (tex / n) * 100 : 0;
}

/** Draw the check onto a copy of the screenshot: HUD mask tinted blue, text runs boxed (red = sentence, green = whitelisted speech or beat, yellow = short text). */
export function debugImage(shot: Image, rep: Report, mask: Uint8Array): Image {
  const out = { w: shot.w, h: shot.h, data: new Uint8Array(shot.data) };
  for (let i = 0; i < mask.length; i++) if (mask[i]) { out.data[i * 4] = (out.data[i * 4]! * 0.6) | 0; out.data[i * 4 + 1] = (out.data[i * 4 + 1]! * 0.6 + 40) | 0; out.data[i * 4 + 2] = Math.min(255, out.data[i * 4 + 2]! * 0.6 + 100) | 0; }
  const box = (r: Run, c: [number, number, number]) => { for (let x = r.x - 1; x <= r.x + r.w; x++) for (const y of [r.y - 1, r.y + r.h]) { if (x < 0 || y < 0 || x >= out.w || y >= out.h) continue; const a = (y * out.w + x) * 4; out.data[a] = c[0]; out.data[a + 1] = c[1]; out.data[a + 2] = c[2]; } for (let y = r.y - 1; y <= r.y + r.h; y++) for (const x of [r.x - 1, r.x + r.w]) { if (x < 0 || y < 0 || x >= out.w || y >= out.h) continue; const a = (y * out.w + x) * 4; out.data[a] = c[0]; out.data[a + 1] = c[1]; out.data[a + 2] = c[2]; } };
  for (const r of rep.runs) box(r, rep.sentences.includes(r) ? [255, 40, 40] : [255, 220, 0]);
  for (const r of [...rep.speech, ...rep.beats]) box(r, [40, 220, 90]);
  return out;
}
