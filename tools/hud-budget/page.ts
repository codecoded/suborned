// The briefing page's own checks (state `page`; GDD v0.27.0 §10 briefing: "Outside play, so the reading limits don't apply"; Glyph's limits, briefs/070/glyph-page-budget.md).
// The page is full screen and opaque by design, so coverage, the running-text and block rules and the old street briefing's reading cap do not apply. What it is held to instead is
// whether it is legible, complete and tidy. The rule that does not move: every word on the page sits in a manifest element with its text, and is counted.
//   2.1 opaque      no street shows: the manifest's `paper` (and `band`) rects cover the frame, or, without them, the composite outside the HUD layer is flat paper
//   2.2 listed      every text stroke the reader finds lies in a text element; an `image` covers one picture (one that holds other pieces hides them); the plan's words are elements
//   2.3 inside      every element inside the safe frame (48 px at 1080; the foot band's pieces 3 % of the height, the bands themselves bleed), a caption under its own still, a piece inside its `parent`
//   2.4 apart       no two text, `pin` or `image` pieces overlap, each 4 px apart at 1080 (text by its ink); a pin on its own still, and the plan's labels and pins on the plan, are the exceptions
//   2.5 type size   body lines at least 20 px tall at 1080; headings taller than the body
//   2.6 pins        each still one pin; each still's pin once on the plan and once in the legend; numbered 1..n in way order, the ending's the car
//   2.7 words       caps per part (glyph-budget §0 counting), the page at most 220 words (Aster 2026-10-26: Sky approved the page as mocked; it reads 214 counted this way)
//   2.8 widows      a wrapped text element never ends on a line of one word
// Today's manifests (0.2.130) carry the page's words as `hud` and its pictures as `image` with no `part`: the parts are read from their text and layout (pageParts) until the host tags them.
import type { Image } from './png';
import { glyphWords, isClock, textRuns, type Element, type Rect } from './budget';

export type PagePart =
  | 'title' | 'hook' | 'objective' | 'place' | 'heading'
  | 'target-photo' | 'target-name' | 'target-fact'
  | 'plan' | 'plan-label' | 'legend'
  | 'way-name' | 'way-when' | 'way-text' | 'still' | 'caption' | 'ending'
  | 'clock-time' | 'clock-label'
  | 'cell-head' | 'cell' | 'face' | 'purse' | 'heat-head' | 'heat' | 'begin'
  | 'band' | 'paper' | 'pin' | 'text' | 'image';

/** Glyph's caps for the page (glyph-page-budget §2), [S] starting values. Sizes are at 1080 and scale with the frame's height. */
export const PAGE = {
  safe: 48, footSafePct: 3, gap: 4, bodyLine: 20, words: 220, flatPct: 5,
  title: { lines: 1, words: 3 }, hook: { sentences: 1, words: 8 }, objective: { sentences: 1, words: 10 },
  targetName: { words: 3 }, fact: { n: 2, lines: 1, words: 8 },
  wayName: { words: 3 }, when: { words: 2 }, way: { sentences: 2, words: 30 }, ways: { min: 1, max: 3 }, stills: { min: 1, max: 3 },
  caption: { lines: 1, words: 5 }, ending: { sentences: 1, words: 16 },
  clock: { min: 3, max: 7, rows: 2, words: 12 }, cell: { faces: 4, rows: 2, words: 16 },
  heat: ['Unknown', 'Noticed', 'Wanted', 'Infamous'], heatHead: 'Heat',
} as const;
/** Body text (2.5) and the parts that are headings over it. The mission name is a kicker (small spaced capitals), not a heading. */
export const BODY: PagePart[] = ['way-text', 'target-fact', 'cell', 'clock-label', 'caption', 'ending'];
export const HEADINGS: PagePart[] = ['hook', 'way-name', 'target-name'];
/** The foot band's pieces (the cell, the purse, Heat, Begin): the band bleeds, its pieces keep PAGE.footSafePct of the height from the screen's edge. */
const FOOT: PagePart[] = ['cell-head', 'cell', 'face', 'purse', 'heat-head', 'heat', 'begin'];
const PICTURES: PagePart[] = ['still', 'target-photo', 'plan', 'face', 'image'];

export interface PageEl extends Element { part: PagePart }
const box = (e: Rect) => `${Math.round(e.x)},${Math.round(e.y)} ${Math.round(e.w)}x${Math.round(e.h)}`;
const name = (e: Element) => `the ${e.part ?? e.kind} "${(e.text ?? '').slice(0, 32)}"`;
const onScreen = (e: Rect, W: number, H: number) => e.w >= 1 && e.h >= 1 && e.x + e.w > 0 && e.y + e.h > 0 && e.x < W && e.y < H;
const within = (a: Rect, b: Rect, slack = 0) => a.x >= b.x - slack && a.y >= b.y - slack && a.x + a.w <= b.x + b.w + slack && a.y + a.h <= b.y + b.h + slack;
const centreIn = (a: Rect, b: Rect) => { const cx = a.x + a.w / 2, cy = a.y + a.h / 2; return cx >= b.x && cx <= b.x + b.w && cy >= b.y && cy <= b.y + b.h; };
const isText = (e: Element) => e.kind !== 'image' && e.kind !== 'pin' && e.kind !== 'paper' && e.kind !== 'band' && !!e.text?.trim();
const sentences = (t: string) => Math.max(t.trim() ? 1 : 0, (t.match(/[.!?](?=\s|$)/g) ?? []).length);
const WHEN = /^(Before|From|After|Until|By|At)\b/i;

/** The page's parts: the manifest's own `part` wins; an untagged element is read from its text and layout (the 0.2.130 page). */
export function pageParts(els: Element[], W: number, H: number): PageEl[] {
  const out: PageEl[] = els.map(e => ({ ...e, part: e.part ?? (e.kind === 'pin' ? 'pin' : e.kind === 'paper' ? 'paper' : e.kind === 'band' ? 'band' : (undefined as unknown as PagePart)) }));
  const free = (e: PageEl) => !e.part, on = out.filter(e => onScreen(e, W, H));
  for (const e of on) if (free(e) && e.kind === 'image') { const t = (e.text ?? '').toLowerCase(); e.part = /photo/.test(t) ? 'target-photo' : t === 'plan' ? 'plan' : /^still\b/.test(t) ? 'still' : /^face\b/.test(t) ? 'face' : 'image'; }
  const plan = on.find(e => e.part === 'plan'), photo = on.find(e => e.part === 'target-photo'), stills = on.filter(e => e.part === 'still');
  const texts = on.filter(e => free(e) && isText(e));
  for (const e of texts) {
    const t = e.text!.trim();
    if ((PAGE.heat as readonly string[]).includes(t)) e.part = 'heat';
    else if (t.toLowerCase() === PAGE.heatHead.toLowerCase()) e.part = 'heat-head';
    else if (/^\$\d{1,3}(,\d{3})*$/.test(t)) e.part = 'purse';
    else if (/^begin$/i.test(t)) e.part = 'begin';
    else if (isClock(t)) e.part = 'clock-time';
    else if (plan && centreIn(e, plan)) e.part = 'plan-label';
    else if (/·/.test(t) && e.y < 0.12 * H) e.part = 'place';
  }
  const times = on.filter(e => e.part === 'clock-time');
  for (const e of texts) if (free(e) && times.some(c => Math.abs(c.x - e.x) <= 6 && e.y >= c.y + c.h - 4 && e.y - (c.y + c.h) <= 14)) e.part = 'clock-label';
  for (const e of texts) if (free(e)) { const s = stills.find(s => e.x >= s.x - 2 && e.x < s.x + s.w && e.y >= s.y + s.h - 2 && e.y - (s.y + s.h) <= 20); if (s) e.part = 'caption'; }
  for (const e of texts) if (free(e) && on.some(c => c.part === 'caption' && Math.abs(c.x - e.x) <= 6 && e.y >= c.y + c.h - 4 && e.y - (c.y + c.h) <= 12)) e.part = 'ending'; // a sentence under the last still's caption: the shared ending
  if (photo) { // the target's name under the photograph, then its facts stacked under the name
    const below = texts.filter(e => free(e) && e.x >= photo.x - 4 && e.x < photo.x + photo.w && e.y >= photo.y + photo.h - 2 && (!plan || e.y < plan.y)).sort((a, b) => a.y - b.y);
    below.forEach((e, i) => { e.part = i === 0 ? 'target-name' : 'target-fact'; });
  }
  for (const e of texts) if (free(e) && FOOTLIKE(e, H) && /[.!?]$/.test(e.text!.trim())) e.part = 'cell';
  if (plan) for (const e of texts) if (free(e) && !FOOTLIKE(e, H) && e.y >= plan.y + plan.h - 2 && e.x >= plan.x - 40 && e.x < plan.x + plan.w) e.part = 'legend';
  const pics = on.filter(x => x.kind === 'image' && x.w * x.h < 0.25 * W * H), top = texts.filter(e => free(e) && (!pics.length || e.y + e.h <= Math.min(...pics.map(x => x.y)) + 4)).sort((a, b) => a.y - b.y);
  (['title', 'hook', 'objective'] as const).forEach((p, i) => { if (top[i]) top[i]!.part = p; });
  const rest = texts.filter(free).sort((a, b) => a.y - b.y); // the way column: a name, its when tag, its sentences
  for (let i = 0; i < rest.length; i++) { const e = rest[i]!; if (WHEN.test(e.text!.trim()) && /\d/.test(e.text!)) { e.part = 'way-when'; const n = rest.slice(0, i).reverse().find(x => free(x) && Math.abs(x.x - e.x) <= 8); if (n) n.part = 'way-name'; } }
  for (const e of rest) if (free(e)) e.part = on.some(w => w.part === 'way-name' && Math.abs(w.x - e.x) <= 8 && w.y < e.y) ? 'way-text' : 'text';
  for (const e of out) if (!e.part) e.part = isText(e) ? 'text' : 'image';
  return out;
}
const FOOTLIKE = (e: Rect, H: number) => e.y > 0.88 * H;

/** Ink: pixels of the composite that stand out from their element's own ground (the median of its rect). */
function lum(shot: Image, i: number) { return 0.299 * shot.data[i * 4]! + 0.587 * shot.data[i * 4 + 1]! + 0.114 * shot.data[i * 4 + 2]!; }
export interface Ink { box?: Rect; rows: { y0: number; y1: number; x0: number; x1: number }[] }
/** The ink of each text element: in its rect, owned by it where rects overlap (the nearest rect centre), never a pin's or a picture's pixels laid over it. */
export function inkOf(shot: Image, els: PageEl[]): Map<PageEl, Ink> {
  const W = shot.w, H = shot.h, texts = els.filter(e => isText(e) && onScreen(e, W, H)), blockers = els.filter(e => e.part === 'pin' || (e.kind === 'image' && e.part !== 'plan' && e.part !== 'image' && e.w * e.h < 0.25 * W * H));
  const res = new Map<PageEl, Ink>();
  for (const e of texts) {
    const x0 = Math.max(0, Math.floor(e.x)), y0 = Math.max(0, Math.floor(e.y)), x1 = Math.min(W, Math.ceil(e.x + e.w)), y1 = Math.min(H, Math.ceil(e.y + e.h));
    const ls: number[] = []; for (let y = y0; y < y1; y++) for (let x = x0; x < x1; x++) ls.push(lum(shot, y * W + x));
    if (!ls.length) { res.set(e, { rows: [] }); continue; }
    const ground = ls.sort((a, b) => a - b)[ls.length >> 1]!, cy = e.y + e.h / 2;
    const own = (x: number, y: number) => !blockers.some(b => x >= b.x && x < b.x + b.w && y >= b.y && y < b.y + b.h && !within(e, b)) && !texts.some(o => o !== e && x >= o.x && x < o.x + o.w && y >= o.y && y < o.y + o.h && Math.abs(o.y + o.h / 2 - y) < Math.abs(cy - y));
    const rowInk: number[] = [], colMin: number[] = [], colMax: number[] = [];
    let bx0 = W, by0 = H, bx1 = -1, by1 = -1;
    for (let y = y0; y < y1; y++) { let n = 0, a = W, b = -1; for (let x = x0; x < x1; x++) if (Math.abs(lum(shot, y * W + x) - ground) > 60 && own(x, y)) { n++; if (x < a) a = x; if (x > b) b = x; } rowInk.push(n); colMin.push(a); colMax.push(b); if (n) { bx0 = Math.min(bx0, a); bx1 = Math.max(bx1, b); by0 = Math.min(by0, y); by1 = y; } }
    const rows: Ink['rows'] = []; let s = -1;
    for (let i = 0; i <= rowInk.length; i++) { const v = i < rowInk.length ? rowInk[i]! : 0; if (v && s < 0) s = i; if (!v && s >= 0) { let a = W, b = -1; for (let k = s; k < i; k++) { a = Math.min(a, colMin[k]!); b = Math.max(b, colMax[k]!); } rows.push({ y0: y0 + s, y1: y0 + i - 1, x0: a, x1: b }); s = -1; } }
    const tall = Math.max(0, ...rows.map(r => r.y1 - r.y0 + 1)), lines = rows.filter(r => r.y1 - r.y0 + 1 >= Math.max(3, 0.4 * tall)); // a rule or an underline is not a line of text
    res.set(e, { box: bx1 >= 0 ? { x: bx0, y: by0, w: bx1 - bx0 + 1, h: by1 - by0 + 1 } : undefined, rows: lines });
  }
  return res;
}
/** The words on a wrapped element's last row, from its text and its rows' ink widths (characters spread evenly over the ink), or from the host's `rows`. */
export function lastRowWords(text: string, rows: { x0: number; x1: number }[], given?: string[]): number {
  if (given?.length) return glyphWords(given[given.length - 1]!);
  const words = text.trim().split(/\s+/).filter(Boolean); if (rows.length < 2 || !words.length) return words.length;
  const chars = words.reduce((a, w) => a + w.length, 0) + words.length - rows.length, ink = rows.reduce((a, r) => a + r.x1 - r.x0 + 1, 0), last = rows[rows.length - 1]!, cap = (last.x1 - last.x0 + 1) * chars / ink * 1.15;
  let n = 1, c = words[words.length - 1]!.length; while (n < words.length && c + 1 + words[words.length - 1 - n]!.length <= cap) { c += 1 + words[words.length - 1 - n]!.length; n++; }
  return n;
}

export interface PageReport { failures: string[]; warnings: string[]; words: number; parts: PageEl[] }
/** The page's checks (2.1 to 2.8). `mask` is the HUD layer's: the reader looks for words in it, outside the pictures (a photograph's strokes are not words) and the pins. */
export function checkPage(shot: Image, mask: Uint8Array, elements: Element[], paperFlat: () => number): PageReport {
  const W = shot.w, H = shot.h, s = H / 1080, failures: string[] = [], warnings: string[] = [];
  const all = pageParts(elements, W, H), els = all.filter(e => onScreen(e, W, H)), texts = els.filter(isText), ink = inkOf(shot, els);
  const by = (p: PagePart) => els.filter(e => e.part === p);
  // 2.1 opaque
  const paper = els.filter(e => e.part === 'paper' || e.part === 'band');
  if (paper.length) { let open = 0; for (let y = 0; y < H; y += 4) for (let x = 0; x < W; x += 4) if (!paper.some(p => x >= p.x && x < p.x + p.w && y >= p.y && y < p.y + p.h)) open++; if (open) failures.push(`the page shows the street (its paper leaves ${(open * 1600 / (W * H)).toFixed(1)}% of the frame open)`); }
  else { const t = paperFlat(); if (t > PAGE.flatPct) failures.push(`the page shows the street (${t.toFixed(1)}% of the frame outside the HUD layer is textured, not flat paper; at most ${PAGE.flatPct}%)`); }
  // 2.2 listed: one picture an image; the reader's runs inside text elements
  const big = els.filter(e => e.kind === 'image' && (e.w * e.h >= 0.25 * W * H || els.some(o => o !== e && (isText(o) || o.part === 'pin' || o.kind === 'image') && within(o, e) && !(o.part === 'pin' && e.part === 'still') && !(e.part === 'plan' && (o.part === 'plan-label' || o.part === 'pin')))));
  for (const e of big) failures.push(`the image "${e.text ?? ''}" at ${box(e)} holds other pieces of the page (${els.filter(o => o !== e && within(o, e)).length} of them): an image covers one picture, and this one hides the page from the reader`);
  const exempt = els.filter(e => (e.kind === 'image' && !big.includes(e) && e.part !== 'plan') || e.part === 'pin'); // a picture's strokes are not words; the plan's are (its labels are elements)
  const read = new Uint8Array(mask); for (const e of exempt) { const x0 = Math.max(0, Math.floor(e.x)), x1 = Math.min(W, Math.ceil(e.x + e.w)); for (let y = Math.max(0, Math.floor(e.y)); y < Math.min(H, Math.ceil(e.y + e.h)); y++) read.fill(0, y * W + x0, y * W + x1); }
  const unlisted = textRuns(shot, read).filter(r => r.h >= 7 * s && !texts.some(e => centreIn(r, e))); // under 7 px at 1080 is a drawn mark broken up (a station dot, a plan tick), never a word on this page
  for (const r of unlisted) failures.push(`unlisted words at ${r.x},${r.y} (${r.w}x${r.h}, about ${r.words} word${r.words === 1 ? '' : 's'}): every word on the page is a manifest element with its text`);
  if (by('plan').length && !els.some(e => e.part === 'plan-label')) warnings.push('the plan has no label elements: its words are read as unlisted until the host emits them');
  // 2.3 inside
  const safe = PAGE.safe * s, foot = PAGE.footSafePct / 100 * H;
  for (const e of els) {
    if (e.part === 'paper' || e.part === 'band' || big.includes(e)) continue;
    const m = FOOT.includes(e.part) ? foot : safe;
    if (e.x < m || e.y < m || e.x + e.w > W - m || e.y + e.h > H - m) failures.push(`${name(e)} at ${box(e)} is outside the safe frame (${Math.round(m)} px in from each edge${FOOT.includes(e.part) ? ', the foot band' : ''})`);
    const parent = e.parent ? els.find(p => p.id === e.parent) : undefined;
    if (e.parent && !parent) warnings.push(`${name(e)} names a parent "${e.parent}" that is not on the page`);
    if (parent && !within(e, parent)) failures.push(`${name(e)} is clipped: it runs outside its parent "${parent.id}"`);
  }
  for (const c of by('caption')) { const st = by('still').find(t => c.x >= t.x - 2 && c.x < t.x + t.w && c.y >= t.y + t.h - 2); if (st && c.x + c.w > st.x + st.w + 2) failures.push(`${name(c)} is clipped: it runs past its still`); }
  for (const l of by('plan-label')) { const p = by('plan')[0]!; if (!within(l, p)) failures.push(`${name(l)} is clipped: it runs outside the plan`); }
  // 2.4 apart (text by its ink, pins and pictures by their rects)
  const shape = (e: PageEl): Rect | undefined => (isText(e) ? ink.get(e)?.box : e.part === 'pin' || e.kind === 'image' ? e : undefined);
  const pieces = els.filter(e => !big.includes(e) && e.part !== 'paper' && e.part !== 'band' && shape(e)), gap = PAGE.gap * s;
  const allowed = (a: PageEl, b: PageEl) => [[a, b], [b, a]].some(([p, q]) => (p!.part === 'pin' || p!.part === 'plan-label') && (q!.part === 'still' || q!.part === 'plan') && within(p!, q!) && (p!.part === 'pin' || q!.part === 'plan'));
  for (let i = 0; i < pieces.length; i++) for (let j = i + 1; j < pieces.length; j++) {
    const a = pieces[i]!, b = pieces[j]!; if (allowed(a, b)) continue;
    const A = shape(a)!, B = shape(b)!, dx = Math.max(A.x - (B.x + B.w), B.x - (A.x + A.w)), dy = Math.max(A.y - (B.y + B.h), B.y - (A.y + A.h)), d = Math.max(dx, dy);
    if (d < gap) failures.push(d < 0 ? `${name(a)} and ${name(b)} overlap` : `${name(a)} and ${name(b)} touch (${Math.round(Math.max(0, d))} px apart; at least ${PAGE.gap} at 1080)`);
  }
  // 2.5 type size
  const rowsOf = (e: PageEl) => (e.rows?.length ?? ink.get(e)?.rows.length) || 1, lineH = (e: PageEl) => Math.round(e.h / rowsOf(e));
  const body = texts.filter(e => BODY.includes(e.part)), bodyMax = Math.max(0, ...body.map(lineH));
  for (const e of body) if (lineH(e) < PAGE.bodyLine * s) failures.push(`${name(e)} is set at ${lineH(e)} px a line (body text at least ${PAGE.bodyLine} px at 1080)`);
  for (const e of texts.filter(e => HEADINGS.includes(e.part))) if (e.h <= bodyMax) failures.push(`${name(e)} is a heading no taller than the body (${e.h} px, body up to ${bodyMax} px)`);
  // 2.6 pins
  const pins = by('pin'), stills = by('still').sort((a, b) => a.y - b.y || a.x - b.x);
  if (!pins.length) warnings.push('no `pin` elements in the manifest: the pins agree check (2.6) waits for the host to emit them');
  else {
    const plan = by('plan')[0], onPlan = pins.filter(p => plan && within(p, plan)), legend = pins.filter(p => !stills.some(t => within(p, t)) && !onPlan.includes(p)), key = (p: Element) => String(p.n ?? '?');
    for (const t of stills) { const ps = pins.filter(p => within(p, t)); if (ps.length !== 1) failures.push(`the still "${t.text ?? ''}" carries ${ps.length} pins (exactly one)`); }
    for (const t of stills) for (const p of pins.filter(p => within(p, t))) for (const [where, list] of [['the plan', onPlan], ['the legend', legend]] as const) { const k = list.filter(q => key(q) === key(p)).length; if (k !== 1) failures.push(`pin ${key(p)} on a still, ${k ? `${k} times` : 'missing'} on ${where}`); }
    const ways = by('way-name').sort((a, b) => a.y - b.y), ending = stills.find(t => by('ending').some(e => e.x >= t.x - 4 && e.x < t.x + t.w)), seq: string[] = [];
    ways.forEach((w, i) => { const y1 = ways[i + 1]?.y ?? H; for (const t of stills.filter(t => t !== ending && t.y + t.h / 2 >= w.y && t.y + t.h / 2 < y1).sort((a, b) => a.x - b.x)) seq.push(key(pins.find(p => within(p, t)) ?? { n: '?' } as Element)); });
    if (seq.length && seq.join() !== seq.map((_, i) => String(i + 1)).join()) failures.push(`the stills' pins read ${seq.join(', ')} in way order (1..${seq.length})`);
    if (ending) { const p = pins.find(q => within(q, ending)); if (p && key(p) !== 'car') failures.push(`the ending's pin is ${key(p)} (the car glyph)`); }
  }
  // 2.7 words
  const words = (e: Element) => glyphWords(e.text ?? ''), one = (p: PagePart) => by(p)[0], cap = (e: PageEl | undefined, what: string, max: number) => { if (e && words(e) > max) failures.push(`${what} "${e.text}" is ${words(e)} words (at most ${max})`); };
  const lines = (e: PageEl, max: number, what: string) => { if (rowsOf(e) > max) failures.push(`${what} "${(e.text ?? '').slice(0, 40)}" runs over ${rowsOf(e)} lines (at most ${max})`); };
  const sent = (e: PageEl | undefined, what: string, max: number) => { if (e && sentences(e.text!) > max) failures.push(`${what} is ${sentences(e.text!)} sentences (at most ${max})`); };
  const t = one('title'); if (!t) failures.push('the page has no mission name'); else { cap(t, 'the mission name', PAGE.title.words); lines(t, PAGE.title.lines, 'the mission name'); }
  const hook = one('hook'), obj = one('objective'); if (!hook) failures.push('the page has no hook'); if (!obj) failures.push('the page has no objective');
  cap(hook, 'the hook', PAGE.hook.words); sent(hook, 'the hook', PAGE.hook.sentences); cap(obj, 'the objective', PAGE.objective.words); sent(obj, 'the objective', PAGE.objective.sentences);
  cap(one('target-name'), 'the target\'s name', PAGE.targetName.words);
  const facts = by('target-fact'); if (facts.length > PAGE.fact.n) failures.push(`the target has ${facts.length} facts (at most ${PAGE.fact.n})`);
  for (const f of facts) { cap(f, 'a target fact', PAGE.fact.words); lines(f, PAGE.fact.lines, 'a target fact'); }
  const ways = by('way-name').sort((a, b) => a.y - b.y);
  if (ways.length < PAGE.ways.min || ways.length > PAGE.ways.max) failures.push(`${ways.length} ways on the page (${PAGE.ways.min} to ${PAGE.ways.max})`);
  ways.forEach((w, i) => {
    const y1 = ways[i + 1]?.y ?? H, row = (p: PagePart) => by(p).filter(e => e.y >= w.y && e.y < y1), label = `the way "${w.text}"`;
    cap(w, 'a way\'s name', PAGE.wayName.words);
    const when = row('way-when'); if (when.length !== 1) failures.push(`${label} has ${when.length} when tags (one plate)`);
    for (const e of when) { const n = words(e) - (e.text!.match(/\d{1,2}:\d{2}/g) ?? []).length; if (!/\d{1,2}:\d{2}/.test(e.text!)) failures.push(`${label}: its when tag "${e.text}" has no time`); if (n > PAGE.when.words) failures.push(`${label}: its when tag "${e.text}" is ${n} words and a time (at most ${PAGE.when.words})`); }
    const tx = row('way-text').filter(e => Math.abs(e.x - w.x) <= 8), n = tx.reduce((a, e) => a + words(e), 0), k = tx.reduce((a, e) => a + sentences(e.text!), 0);
    if (n > PAGE.way.words) failures.push(`${label} reads ${n} words (at most ${PAGE.way.words})`); if (k > PAGE.way.sentences) failures.push(`${label} reads ${k} sentences (at most ${PAGE.way.sentences})`);
    const end = stills.find(t => by('ending').some(e => e.x >= t.x - 4 && e.x < t.x + t.w)), st = stills.filter(t => t !== end && t.y + t.h / 2 >= w.y && t.y + t.h / 2 < y1).length;
    if (stills.length && (st < PAGE.stills.min || st > PAGE.stills.max)) failures.push(`${label} has ${st} stills (${PAGE.stills.min} to ${PAGE.stills.max})`);
  });
  for (const c of by('caption')) { cap(c, 'a still\'s caption', PAGE.caption.words); lines(c, PAGE.caption.lines, 'a still\'s caption'); }
  for (const e of by('ending')) { cap(e, 'the ending', PAGE.ending.words); sent(e, 'the ending', PAGE.ending.sentences); }
  const clock = by('clock-time').sort((a, b) => a.x - b.x), secs = clock.map(c => c.text!.trim().split(':').reduce((a, x) => a * 60 + +x, 0));
  if (clock.length < PAGE.clock.min || clock.length > PAGE.clock.max) failures.push(`the clock has ${clock.length} stations (${PAGE.clock.min} to ${PAGE.clock.max})`);
  if (secs.some((v, i) => i > 0 && v <= secs[i - 1]!)) failures.push(`the clock's times do not ascend left to right (${clock.map(c => c.text).join(', ')})`);
  for (const l of by('clock-label')) { cap(l, 'a clock label', PAGE.clock.words); lines(l, PAGE.clock.rows, 'a clock label'); }
  const cells = by('cell'), faces = by('face');
  if (cells.length !== PAGE.cell.faces) failures.push(`${cells.length} cell lines (one for each of the ${PAGE.cell.faces} faces)`);
  if (faces.length && faces.length !== PAGE.cell.faces) failures.push(`${faces.length} cell faces (${PAGE.cell.faces})`);
  for (const c of cells) { cap(c, 'a cell line', PAGE.cell.words); lines(c, PAGE.cell.rows, 'a cell line'); }
  const purse = by('purse'); if (purse.length !== 1) failures.push(`${purse.length} purse figures (one)`);
  const heat = by('heat'); if (heat.length !== 1) failures.push(`${heat.length} Heat words (one of ${PAGE.heat.join(', ')})`);
  if (!by('heat-head').length) failures.push(`the Heat head word is missing ("${PAGE.heatHead}" beside "${heat[0]?.text ?? '?'}")`);
  const begin = by('begin'); if (begin.length !== 1) failures.push(`${begin.length} Begin buttons (one)`); for (const b of begin) cap(b, 'Begin', 1);
  const total = texts.reduce((a, e) => a + words(e), 0); if (total > PAGE.words) failures.push(`the page reads ${total} words (at most ${PAGE.words}, about a minute's reading)`);
  for (const e of texts.filter(e => e.part === 'text')) warnings.push(`${name(e)} at ${box(e)} is not a part the page knows: its words count, its caps wait for the host's \`part\``);
  // 2.8 widows
  for (const e of texts) { const r = ink.get(e)?.rows ?? [], n = e.rows?.length ?? r.length; if (n >= 2 && lastRowWords(e.text!, r, e.rows) === 1) failures.push(`${name(e)} ends on a widow: its last line is one word`); }
  return { failures, warnings, words: total, parts: all };
}
