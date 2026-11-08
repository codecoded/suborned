// Speech and cards, held to the caps the text schema keeps (brief 098, Splice): a bubble over the live street to text.schema's MAX_WORDS, a conversation's line to the
// cutaway's own (conversations.schema CUTAWAY_MAX_*: Sky 2026-11-05, "Full sentences": at most 25 words, two sentences and one question), the cards to their rows
// (CARD_WORDS a row, as many rows as the schema's `learned` and `opens` allow). Read from the schema files, never copied here, so a cap moves in one place.
// A speech element is a conversation's line when its words are one of text.json's scenes.conversations beats (the words the build plays); anything else is a bubble.
// A person's card on them (Glyph's slip, brief 101 round 3) is a conversation's cards in the street: a card element whose words are one of them is a slip.
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { MAX_WORDS } from '../../contracts/missions/handover/text.schema';
import { CARD_WORDS, CUTAWAY_MAX_QUESTIONS, CUTAWAY_MAX_SENTENCES, CUTAWAY_MAX_WORDS, Conversation, questionCount, sentenceCount } from '../../contracts/missions/handover/conversations.schema';

/** A zod array's max_length, as the schema states it. */
function maxLength(schema: unknown): number {
  const checks = (schema as { _zod?: { def?: { checks?: { _zod: { def: { check: string; maximum?: number } } }[] } } })._zod?.def?.checks ?? [];
  const m = checks.map(c => c._zod.def).find(d => d.check === 'max_length')?.maximum;
  if (m === undefined) throw new Error('hud-budget: the conversation schema states no max length here: read the cap from it again');
  return m;
}
/** A bubble: two rows on screen (Glyph's bubble; the speaker's name may take a row of its own above), MAX_WORDS words (text.schema: bubbles in play). */
export const BUBBLE = { lines: 2, words: MAX_WORDS } as const;
/** A conversation's line in the cutaway: the clock is held, so full sentences (conversations.schema). It has no row cap of its own: the cutaway shows one line at a time. */
export const CUTAWAY = { words: CUTAWAY_MAX_WORDS, sentences: CUTAWAY_MAX_SENTENCES, questions: CUTAWAY_MAX_QUESTIONS } as const;
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
/** What is wrong with one bubble (one speech element, judged alone): `text` its words, `rows` its rows on screen (the name's row counted out), `words` its words as the budget counts them. */
export function speechFault(text: string, rows: number, words: number, said: Set<string> | null): string | undefined {
  const quote = `"${text.trim().slice(0, 40)}${text.trim().length > 40 ? '…' : ''}"`;
  if (isConversationLine(text, said)) {
    const t = text.trim(), s = sentenceCount(t), q = questionCount(t);
    if (words > CUTAWAY.words) return `the conversation line ${quote} runs ${words} words (a conversation's line at most ${CUTAWAY.words}: conversations.schema CUTAWAY_MAX_WORDS)`;
    if (s > CUTAWAY.sentences) return `the conversation line ${quote} is ${s} sentences (at most ${CUTAWAY.sentences}: CUTAWAY_MAX_SENTENCES)`;
    if (q > CUTAWAY.questions) return `the conversation line ${quote} asks ${q} questions (at most ${CUTAWAY.questions}: CUTAWAY_MAX_QUESTIONS)`;
    return undefined;
  }
  if (words > BUBBLE.words) return `the speech bubble ${quote} runs ${words} words (a bubble at most ${BUBBLE.words}: text.schema MAX_WORDS)`;
  if (rows > BUBBLE.lines) return `the speech bubble ${quote} runs ${rows} rows (a bubble at most ${BUBBLE.lines})`;
  return undefined;
}
