// Speech and cards, held to the caps the text schema keeps (brief 098, Splice): a bubble over the live street to text.schema's MAX_WORDS, a conversation's line to the
// cutaway's own (conversations.schema CUTAWAY_MAX_*: Sky 2026-11-05, "Full sentences": at most 25 words, two sentences and one question), the cards to their rows
// (CARD_WORDS a row, as many rows as the schema's `learned` and `opens` allow). Read from the schema files, never copied here, so a cap moves in one place.
// A speech element is a conversation's line when its words are one of text.json's scenes.conversations beats (the words the build plays); anything else is a bubble.
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

const TEXT = resolve(import.meta.dir, '../../contracts/missions/handover/text.json');
let lines: Set<string> | null | undefined;
/** Every conversation line text.json plays (null: no text.json to read, so every speech element is judged as a bubble, the stricter answer). */
export function conversationLines(file = TEXT): Set<string> | null {
  if (lines !== undefined && file === TEXT) return lines;
  let out: Set<string> | null;
  try { const t = JSON.parse(readFileSync(file, 'utf8')); out = new Set(((t.scenes?.conversations?.items ?? []) as { beats: { line: string }[] }[]).flatMap(c => c.beats.map(b => b.line.trim()))); } catch { out = null; }
  if (file === TEXT) lines = out;
  return out;
}
/** The words a speech element shows, less a speaker's label ("Inez: ...", or the name on its own row). */
export const isConversationLine = (text: string, said: Set<string> | null): boolean => !!said && [text.trim(), text.replace(/^\s*[^:\n]{1,24}:\s+/, '').trim(), text.replace(/^\s*\S+\s+/, '').trim()].some(t => said.has(t));
/** What is wrong with one stacked speech block: `text` its element's words, `rows` its rows on screen (the name's row counted out), `words` its words as the budget counts them. */
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
