// Speech and cards, held to the caps the text schema keeps (brief 098, Splice): a bubble over the live street to text.schema's MAX_WORDS, a conversation's line to the
// cutaway's own (conversations.schema CUTAWAY_MAX_*: Sky 2026-11-05, "Full sentences": at most 25 words, two sentences and one question), the cards to their rows
// (CARD_WORDS a row, as many rows as the schema's `learned` and `opens` allow). Read from the schema files, never copied here, so a cap moves in one place.
// A speech element is a conversation's line when its words are one of text.json's scenes.conversations beats (the words the build plays); anything else is a bubble.
// A person's card on them (Glyph's slip, brief 101 round 3) is a conversation's cards in the street: a card element whose words are one of them is a slip.
//
// The bubble rule, for GDD v0.40.0 §10 (Glyph, briefs/101/glyph/r5-review.md, "Splice's questions" 2):
//   "Speech bubbles at the speaker: at most two on screen at once, one per speaker (the newest), 3 s, twelve words or fewer. A conversation's street line keeps the cutaway's caps."
// Round 4 (briefs/101/glyph/r4/notes.md, "For Splice"; Sky 2026-11-09: option B, "two rows") holds every frame to it:
// 1. at most two speech elements on screen in play, the docked line among them: manifest elements, not the reader's runs;
// 2. at most two rows a bubble, a conversation's street line too (the cutaway's own line is no bubble: its column, one line at a time);
// 3. each bubble's box 14 px inside the screen (HudToolkit.Bubbles' clamp);
// 4. a one-row bubble at most 460 px wide at 1080; past that it takes two rows, and only a line two rows of 460 can't hold is wider.
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { Less, MAX_WORDS } from '../../contracts/missions/handover/text.schema';
import { CARD_WORDS, CUTAWAY_MAX_QUESTIONS, CUTAWAY_MAX_SENTENCES, CUTAWAY_MAX_WORDS, Conversation, questionCount, sentenceCount } from '../../contracts/missions/handover/conversations.schema';

/** A zod array's max_length, as the schema states it. */
function maxLength(schema: unknown): number {
  const checks = (schema as { _zod?: { def?: { checks?: { _zod: { def: { check: string; maximum?: number } } }[] } } })._zod?.def?.checks ?? [];
  const m = checks.map(c => c._zod.def).find(d => d.check === 'max_length')?.maximum;
  if (m === undefined) throw new Error('hud-budget: the conversation schema states no max length here: read the cap from it again');
  return m;
}
/** A zod number's maximum, as the schema states it. */
function maxValue(schema: unknown): number {
  const m = (schema as { maxValue?: number | null }).maxValue;
  if (m === undefined || m === null) throw new Error('hud-budget: the text schema states no maximum here: read the cap from it again');
  return m;
}
/** A bubble: two rows on screen (the speaker's name may take a row of its own above), MAX_WORDS words (text.schema: bubbles in play). `onScreen`: the most the text schema lets
 *  `less.feed.bubblesAtOnce` say, the docked line among them. `wrap`: a one-row bubble's widest at 1080, its face included (Glyph's option B). `margin`: its box inside the screen.
 *  `oneRow`: a box this tall at 1080 or less holds one row (a bubble is 28 px, the old docked plate 25, two rows 47). */
export const BUBBLE = { lines: 2, words: MAX_WORDS, onScreen: maxValue(Less.shape.feed.shape.bubblesAtOnce), wrap: 460, margin: 14, oneRow: 37.5 } as const;
/** A conversation's line in the cutaway: the clock is held, so full sentences (conversations.schema). It has no row cap of its own: the cutaway shows one line at a time. */
export const CUTAWAY = { words: CUTAWAY_MAX_WORDS, sentences: CUTAWAY_MAX_SENTENCES, questions: CUTAWAY_MAX_QUESTIONS } as const;
/** The cutaway's face (HudToolkit.Convo, the speaker's face beside the line's column): a speech element level with it is the cutaway's line, not a bubble. */
export const CUTAWAY_FACE = "speaker's face";
type Box = { x: number; y: number; w: number; h: number; kind?: string; text?: string };
type Size = { w: number; h: number };
/** Is this speech element the cutaway's line? Only when the cutaway's face is drawn level with it. */
export const isCutawayLine = (e: Box, els: readonly Box[]): boolean => els.some(f => f.kind === 'image' && f.text === CUTAWAY_FACE && f.w > 0 && f.y < e.y + e.h && e.y < f.y + f.h);
/** The manifest rounds a box's place and its size apart (HudToolkit.Capture), so its far edge may be a pixel off. */
const ROUNDING = 1;
const quote = (text: string) => `"${text.trim().slice(0, 40)}${text.trim().length > 40 ? '…' : ''}"`;
/** A conversation's cards: what the player now knows and can do, each row CARD_WORDS words, the rows the schema's `learned` and `opens` allow; one panel on screen. */
const ROWS = maxLength(Conversation.shape.learned) + maxLength(Conversation.shape.opens);
export const CARD_MAX = { rows: ROWS, rowWords: CARD_WORDS, words: ROWS * CARD_WORDS, onScreen: 1 } as const;
/** A person's card as Glyph's round 3 draws it (kind `slip`): "a slip of the book's cream page on a fine stem to her head, the same family as the bubbles", on her in the street and
 *  never beside the news. It shows a conversation's cards, so it keeps their caps, one on screen. The news lozenge (the slim feed's one crucial card, GDD §10; "the bottom right holds
 *  only the news and the Book") stays a `card`: a slip on her and the news in its corner are one each, counted apart. */
export const SLIP_MAX = { rows: CARD_MAX.rows, words: CARD_MAX.words, onScreen: 1 } as const;

const TEXT = resolve(import.meta.dir, '../../contracts/missions/handover/text.json');
type Said = { lines: Set<string>; cards: Set<string> } | null;
let read: Said | undefined;
const flat = (t: string) => t.replace(/\s+/g, ' ').trim();
/** text.json's conversations: the lines they play, and their cards as the host writes a person's card (its learned rows, then its opens rows, one space between: HudToolkit.Card.cs). */
function said(file: string): Said {
  if (read !== undefined && file === TEXT) return read;
  let out: Said;
  try {
    const items = (JSON.parse(readFileSync(file, 'utf8')).scenes?.conversations?.items ?? []) as { beats: { line: string }[]; learned?: string[]; opens?: string[] }[];
    out = { lines: new Set(items.flatMap(c => c.beats.map(b => b.line.trim()))), cards: new Set(items.map(c => flat([...(c.learned ?? []), ...(c.opens ?? [])].join(' '))).filter(Boolean)) };
  } catch { out = null; }
  if (file === TEXT) read = out;
  return out;
}
/** Every conversation line text.json plays (null: no text.json to read, so every speech element is judged as a bubble, the stricter answer). */
export const conversationLines = (file = TEXT): Set<string> | null => said(file)?.lines ?? null;
/** Every person's card text.json can show (null: no text.json to read, so no card is read as a slip: the stricter count). */
export const conversationCards = (file = TEXT): Set<string> | null => said(file)?.cards ?? null;
/** A card element's words are a conversation's cards: the person's slip. */
export const isConversationCard = (text: string | undefined, cards: Set<string> | null): boolean => !!cards && !!text && cards.has(flat(text));
/** The words a speech element shows, less a speaker's label ("Inez: ...", or the name on its own row). */
export const isConversationLine = (text: string, said: Set<string> | null): boolean => !!said && [text.trim(), text.replace(/^\s*[^:\n]{1,24}:\s+/, '').trim(), text.replace(/^\s*\S+\s+/, '').trim()].some(t => said.has(t));
/** What is wrong with one bubble (one speech element, judged alone): `text` its words, `rows` its rows on screen (the name's row counted out), `words` its words as the budget counts them.
 *  `cutaway`: the element is the cutaway's line (isCutawayLine), which keeps a conversation line's caps and has no row cap; in the street a conversation's line is a bubble (rule 2). */
export function speechFault(text: string, rows: number, words: number, said: Set<string> | null, cutaway = false): string | undefined {
  const q = quote(text);
  if (isConversationLine(text, said)) {
    const t = text.trim(), s = sentenceCount(t), n = questionCount(t);
    if (words > CUTAWAY.words) return `the conversation line ${q} runs ${words} words (a conversation's line at most ${CUTAWAY.words}: conversations.schema CUTAWAY_MAX_WORDS)`;
    if (s > CUTAWAY.sentences) return `the conversation line ${q} is ${s} sentences (at most ${CUTAWAY.sentences}: CUTAWAY_MAX_SENTENCES)`;
    if (n > CUTAWAY.questions) return `the conversation line ${q} asks ${n} questions (at most ${CUTAWAY.questions}: CUTAWAY_MAX_QUESTIONS)`;
    if (!cutaway && rows > BUBBLE.lines) return `the conversation line ${q} runs ${rows} rows (in the street a bubble, at most ${BUBBLE.lines})`;
    return undefined;
  }
  if (words > BUBBLE.words) return `the speech bubble ${q} runs ${words} words (a bubble at most ${BUBBLE.words}: text.schema MAX_WORDS)`;
  if (rows > BUBBLE.lines) return `the speech bubble ${q} runs ${rows} rows (a bubble at most ${BUBBLE.lines})`;
  return undefined;
}
/** What is wrong with the speech on screen together and with each bubble's box (rules 1, 3 and 4): `talk` the speech elements on screen, `els` the frame's elements (for the cutaway's face),
 *  `play` whether the frame is play (rest, active, alt). The cutaway's line counts toward the two; it is no bubble, so its box has no place or width rule. */
export function bubbleFaults(talk: readonly Box[], els: readonly Box[], size: Size, play: boolean): string[] {
  const s = size.h / 1080, m = BUBBLE.margin * s, edge = `(inside ${BUBBLE.margin} px of the edge)`, out: string[] = [];
  if (play && talk.length > BUBBLE.onScreen) out.push(`${talk.length} speech bubbles on screen (at most ${BUBBLE.onScreen}; a docked line counts)`);
  for (const e of talk) {
    if (isCutawayLine(e, els)) continue;
    const what = `the speech bubble ${quote(e.text ?? '')}`;
    if (e.x + e.w > size.w - m + ROUNDING) out.push(`${what} runs to x ${e.x + e.w} ${edge}`);
    if (e.x < m - ROUNDING) out.push(`${what} starts at x ${e.x} ${edge}`);
    if (e.y < m - ROUNDING) out.push(`${what} starts at y ${e.y} ${edge}`);
    if (e.y + e.h > size.h - m + ROUNDING) out.push(`${what} runs to y ${e.y + e.h} ${edge}`);
    if (e.h <= BUBBLE.oneRow * s && e.w > BUBBLE.wrap * s + ROUNDING) out.push(`${what} is one row ${Math.round(e.w / s)} px wide (a bubble wraps at ${BUBBLE.wrap} px)`);
  }
  return out;
}

/** Where a slip may not sit (Glyph's r5 review, Splice's question 1): the news and Book box, the bottom right (round 3's rule 5: it holds only the news and the Book), and the clock
 *  stack, the top right (its time and timed tabs, the clock bar, the next event and the key people). Each is the corner its pieces stand in, from their inner edges to the screen's:
 *  the news lozenge, its badge and the Book's key; the bar, the next-event row and the key people (the time and the tabs over the bar lie inside that corner). A piece not on screen
 *  marks nothing. `slips` and `els` are on screen. */
export const NEWS_BOOK = ['card badge', 'book key'], CLOCK_STACK = ['known people'];
export function slipPlaceFaults(slips: readonly Box[], els: readonly Box[], size: Size): string[] {
  const art = (names: string[]) => (e: Box) => e.kind === 'image' && names.includes(e.text ?? '');
  const corner = (ps: Box[], top: boolean): Box | undefined => {
    if (!ps.length) return undefined; const x = Math.min(...ps.map(p => p.x));
    if (!top) { const y = Math.min(...ps.map(p => p.y)); return { x, y, w: size.w - x, h: size.h - y }; }
    return { x, y: 0, w: size.w - x, h: Math.max(...ps.map(p => p.y + p.h)) };
  };
  const news = corner(els.filter(e => e.kind === 'card' || art(NEWS_BOOK)(e)), false), clock = corner(els.filter(e => e.kind === 'bar' || e.kind === 'next' || art(CLOCK_STACK)(e)), true);
  const hits = (a: Box, b: Box | undefined) => !!b && a.x < b.x + b.w && b.x < a.x + a.w && a.y < b.y + b.h && b.y < a.y + a.h, out: string[] = [];
  for (const e of slips) {
    if (hits(e, news)) out.push(`the slip ${quote(e.text ?? '')} sits in the news and Book box (the bottom right holds only the news and the Book)`);
    if (hits(e, clock)) out.push(`the slip ${quote(e.text ?? '')} sits over the clock stack (the top right: the clock, the next event and the key people)`);
  }
  return out;
}
