// The HUD budget (brief 014 rule 5, Glyph): how much of a play screenshot the HUD covers, and whether it carries running text.
// Needs to tell HUD from world. Three ways, best first:
//   hud     an RGBA capture of the HUD layer alone (UI Toolkit panel drawn to a transparent target): alpha >= HUD_ALPHA is HUD.
//   world   the same frame captured with the HUD hidden: any pixel that differs is HUD.
//   rects   a list of screen rectangles (the UI tree's worldBound, or hand-drawn for an old shot): the union is HUD. Coarse (bounding boxes).
// The text check looks only inside the HUD mask. Speech and story beats are whitelisted by a manifest (rects with a kind), because a screenshot cannot tell
// a spoken line from a rule printed on a card. Without a manifest every running line fails, which is the safe answer.
import type { Image } from './png';

export type State = 'rest' | 'active' | 'alt' | 'log' | 'briefing';
export interface Rect { x: number; y: number; w: number; h: number }
export interface Element extends Rect { kind: 'speech' | 'beat' | 'line' | 'image' | 'hud'; text?: string } // image: art with no text by construction (portraits, the plot scope): its strokes are not read
export interface Manifest { state?: State; elements: Element[] }
export interface Options { state: State; hud?: Image; world?: Image; rects?: Rect[]; manifest?: Manifest }
export interface Run { x: number; y: number; w: number; h: number; comps: number; words: number; kind: 'hud' | 'speech' | 'beat' | 'line' }
export interface Report {
  width: number; height: number; state: State; mask: 'hud' | 'world' | 'rects';
  coveragePct: number; footprintPct: number; limitPct: number;
  runs: Run[]; sentences: Run[]; blocks: Run[][]; speech: Run[]; beats: Run[]; lines: Run[];
  failures: string[]; warnings: string[]; pass: boolean;
}

/** Fail above this share of the screen at rest (brief 014: about 15%); an open order card and the Alt layer get more room. `log`: the history panel
 *  the player opens to read (Glyph; a reading surface, only while open): 18% (the open panel measures 17.0 to 17.5%), and its lines may be sentences.
 *  `briefing`: the full-screen briefing card before play (title and copy): the same room and the same freedom. */
export const LIMIT: Record<State, number> = { rest: 15, active: 22, alt: 30, log: 18, briefing: 18 };
/** The states that are reading surfaces the player opens or meets before play (the history log; the briefing card): their lines are sentences by design. */
export const READING: State[] = ['log', 'briefing'];
/** Warn above this at rest: the goal is a bare screen. */
export const WARN_REST = 10;
export const HUD_ALPHA = 24, WORLD_DIFF = 10;
/** A line of three or more words is running text; a block is two or more stacked lines of them. Alt allows fragments of up to ALT_WORDS words. */
export const SENTENCE_WORDS = 3, SENTENCE_COMPS = 14, ALT_WORDS = 5;
export const SPEECH_MAX = { lines: 2, words: 10 }, BEAT_MAX = { lines: 1, words: 6, onScreen: 2 }, LINE_MAX = { lines: 1, words: 6, onScreen: 1 }; // line = the objective or the Next Step hint: one imperative, at most six words, one at a time

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

export function analyse(shot: Image, o: Options): Report {
  const { w, h } = shot, { mask, how } = maskOf(shot, o); let n = 0; for (const v of mask) n += v;
  const coveragePct = (n / (w * h)) * 100, limitPct = LIMIT[o.state], failures: string[] = [], warnings: string[] = [];
  const all = textRuns(shot, mask), els = o.manifest?.elements ?? [];
  const owner = new Map<Run, Element>(); // the manifest element a run lies in: a bubble's words are counted once, however many runs it breaks into (a pin's stem crossing it made two)
  const wordsIn = (b: Run[]) => { const seen = new Set<Element>(); let n = 0; for (const r of b) { const e = owner.get(r); if (e) { if (!seen.has(e)) { seen.add(e); n += r.words; } } else n += r.words; } return n; };
  for (const r of all) { const e = els.find(e => (e.kind === 'speech' || e.kind === 'beat' || e.kind === 'line') && inside(r, e)); if (e) { r.kind = e.kind as Run['kind']; } }
  for (const e of els) { if (e.text && (e.kind === 'speech' || e.kind === 'beat' || e.kind === 'line')) for (const r of all) if (inside(r, e)) { r.words = wordsOf(e.text); owner.set(r, e); } }
  const art = els.filter(e => e.kind === 'image'), hudRuns = all.filter(r => r.kind === 'hud' && !art.some(e => inside(r, e))), speech = all.filter(r => r.kind === 'speech'), beats = all.filter(r => r.kind === 'beat'), lineRuns = all.filter(r => r.kind === 'line');
  const isSentence = (r: Run) => (o.state === 'alt' ? r.words > ALT_WORDS : r.words >= SENTENCE_WORDS && r.comps >= SENTENCE_COMPS);
  const sentences = READING.includes(o.state) ? [] : hudRuns.filter(isSentence); // the open history is read, so its lines are sentences by design
  const blocks = READING.includes(o.state) ? [] : stack(hudRuns.filter(r => r.words >= 2 && r.comps >= 6)).filter(b => b.length >= 2 && b.reduce((a, r) => a + r.words, 0) >= 4);
  if (coveragePct > limitPct) failures.push(`HUD covers ${coveragePct.toFixed(1)}% of the screen (limit ${limitPct}% ${o.state})`);
  else if (o.state === 'rest' && coveragePct > WARN_REST) warnings.push(`HUD covers ${coveragePct.toFixed(1)}% at rest (aim under ${WARN_REST}%)`);
  if (sentences.length) failures.push(`${sentences.length} line${sentences.length > 1 ? 's' : ''} of running text that is not speech`);
  if (blocks.length) failures.push(`${blocks.length} block${blocks.length > 1 ? 's' : ''} of stacked text`);
  // Text rows of a line or beat: runs whose centres lie within 8 px share a row. A row of a few strokes beside a row of a whole line is the panel's rim
  // meeting a busy picture behind it (dark foliage under a bright plate edge breaks into letter-sized strokes), not a second line: it needs a quarter of
  // the strongest row's strokes, and at least three.
  const lines = (all: Run[]) => {
    const tall = Math.max(0, ...all.map(r => r.h)), es = all.filter(r => r.h >= 0.6 * tall); // a run under 0.6 of the tallest is a fragment of the rim, not a line (Glyph's review)
    const rs = [...es].sort((a, b) => a.y + a.h / 2 - (b.y + b.h / 2)), rows: { y: number; comps: number }[] = [];
    for (const r of rs) { const y = r.y + r.h / 2, last = rows[rows.length - 1]; if (last && y - last.y <= 8) { last.comps += r.comps; last.y = y; } else rows.push({ y, comps: r.comps }); }
    const top = Math.max(0, ...rows.map(r => r.comps));
    return rows.filter(r => r.comps >= Math.max(3, 0.25 * top)).length;
  };
  if (speech.length && stack(speech).some(b => b.length > SPEECH_MAX.lines || wordsIn(b) > SPEECH_MAX.words)) failures.push(`a speech line is longer than ${SPEECH_MAX.lines} lines or ${SPEECH_MAX.words} words`);
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
  return { width: w, height: h, state: o.state, mask: how, coveragePct, footprintPct: footprint(mask, w, h), limitPct, runs: hudRuns, sentences, blocks, speech, beats, lines: lineRuns, failures, warnings, pass: failures.length === 0 };
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
