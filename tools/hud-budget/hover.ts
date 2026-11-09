// The hover lines (kind `hover`, brief 101; Glyph: briefs/101/glyph/r5-review.md item 1, r4/notes.md "For Splice" 5): the words the pointer opens on a piece, and the verb row's words.
// Builds to 0.2.295 write none of them, so the budget never reads them (Glyph's r5 review): the news lozenge's full line reads as four short `hud` runs, none of them a sentence's
// 14 strokes (r101-lozenge-hover), and the next-event and eye tips are not in the manifest at all. Tagged `hover`, each is held here:
// - one under the pointer, so one on screen; the verb row's words are counted apart (the armed verb's name shows with no pointer on the row), and they show one at a time;
// - at most two rows: the lozenge's line is right-aligned to its end, 10 px over the card, and a second row grows upward (HudToolkit.Crucial);
// - its line at most text.schema's MAX_LINE characters as shown: a hover carries one line of the words (the why's line, 80 characters at its widest fills: why.schema; its 16-word
//   cap counts a fill such as {where} as one word, so a shown line may run past 16 words, and the characters are the cap; a timeline label, 80 too), and a head ("Next") or a key
//   beside it, each its own field at a `·`. The next-event tip is "Next · " and the label; the eye tips are six words at most (HudModel.EyeTip, RememberTip);
// - the verb row's words (a hovered tab's name and key, or the armed verb's name): one row, at most 3 words and one key. Until the host writes them as `hover` they sit inside the
//   "verb row" image element, as the take-control tab's word does (HudToolkit.VerbRow), and the reader holds that image's words to one row.
import { MAX_LINE } from '../../contracts/missions/handover/text.schema';
import { segWords } from './words';

/** [S] Glyph, 2026-11-09 (rows, one at a time); the characters are the text schema's. */
export const HOVER_MAX = { lines: 2, chars: MAX_LINE, onScreen: 1 } as const;
/** The verb row's words, one name at a time: "Fire · ⇧2", "Knock out". [S] Glyph, round 4. */
export const VERB_WORDS = { lines: 1, words: 3, keys: 1, onScreen: 1 } as const;
/** The verb row's manifest element (an `image`: the tabs, and their words while hovered or armed). */
export const VERB_ROW = 'verb row';

type Box = { x: number; y: number; w: number; h: number; kind?: string; text?: string };
const KEY_NAMES = /^(?:Space|Return|Tab|Shift|Ctrl|Alt|Cmd|R (?:Shift|Ctrl|Alt|Cmd)|Num (?:Enter|[0-9+−])|F\d{1,2})$/;
/** A key as the build names it (PlayControls.ShortLabel): a modifier's sign and a key's name ("⇧2", "T", "Space", "Num 5"), or a text token (`<Verb1>`). */
export const isKey = (seg: string): boolean => /^<[A-Za-z]+\d*>$/.test(seg) || /^[⇧⌃⌥]?(?:[A-Z0-9]|[↑↓←→`−=\[\];',./\\])$/u.test(seg) || KEY_NAMES.test(seg.replace(/^[⇧⌃⌥]/u, ''));
/** A hover's fields, split at its separator dots: its line, and a head or a key beside it. */
const fields = (text: string) => text.split(/\s+·\s+/).map(x => x.trim()).filter(Boolean);
/** A verb row hover's parts: its words and its keys (the key is its own field after the name: "Fire · ⇧2"). */
export function verbParts(text: string): { words: number; keys: number } {
  const g = fields(text), keys = g.filter(isKey).length;
  return { words: g.filter(x => !isKey(x)).reduce((a, x) => a + segWords(x), 0), keys };
}
const overlaps = (a: Box, b: Box) => a.x < b.x + b.w && b.x < a.x + a.w && a.y < b.y + b.h && b.y < a.y + a.h;
const sOf = (e: Box) => `"${(e.text ?? '').trim().slice(0, 40)}${(e.text ?? '').trim().length > 40 ? '…' : ''}"`;

/** What is wrong with the hover lines and the verb row's words on screen. `els`: the frame's elements on screen; `rowsIn(e, kind)`: the reader's rows of that kind's runs inside `e`. */
export function hoverFaults(els: readonly Box[], rowsIn: (e: Box, kind: 'hover' | 'hud') => number): string[] {
  const out: string[] = [], rows = els.filter(e => e.kind === 'image' && e.text === VERB_ROW), hovers = els.filter(e => e.kind === 'hover');
  const onRow = (e: Box) => rows.some(r => overlaps(e, r)), verbs = hovers.filter(onRow), tips = hovers.filter(e => !onRow(e));
  if (tips.length > HOVER_MAX.onScreen) out.push(`${tips.length} hover lines on screen (one pointer: at most ${HOVER_MAX.onScreen})`);
  if (verbs.length > VERB_WORDS.onScreen) out.push(`${verbs.length} verb names on the verb row (one at a time)`);
  for (const e of tips) {
    const t = e.text ?? '', r = rowsIn(e, 'hover'), chars = Math.max(0, ...fields(t).map(x => [...x].length)), what = `the hover line ${sOf(e)}`;
    if (r > HOVER_MAX.lines) out.push(`${what} runs ${r} rows (at most ${HOVER_MAX.lines})`);
    if (chars > HOVER_MAX.chars) out.push(`${what}: its line is ${chars} characters (at most ${HOVER_MAX.chars}: text.schema MAX_LINE)`);
    if (!t.trim()) out.push(`${what} has no words (the host writes the line it shows)`);
  }
  for (const e of verbs) {
    const { words, keys } = verbParts(e.text ?? ''), what = `the verb row's words ${sOf(e)}`;
    if (rowsIn(e, 'hover') > VERB_WORDS.lines) out.push(`${what} wrap over more than one line`);
    if (words > VERB_WORDS.words) out.push(`${what} are ${words} words (a verb's name at most ${VERB_WORDS.words}, and its key)`);
    if (keys > VERB_WORDS.keys) out.push(`${what} show ${keys} keys (at most ${VERB_WORDS.keys})`);
  }
  for (const r of rows) { const n = rowsIn(r, 'hud'); if (n > VERB_WORDS.lines) out.push(`the verb row's words read as ${n} rows (one row: a verb's name and its key, one at a time)`); }
  return out;
}
