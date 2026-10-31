// suborned.text/1, scenes.conversations (Fable, brief 085, GDD v0.34.0 §10): whole conversations Sky reads. The act lands, its outcome known, and the cutaway plays the
// conversation once, its outcome included. A conversation doesn't replace a person's `scenes.items` entry: those lines stay for whatever no conversation covers, and for bubbles.
//
// A conversation is written for one person in one situation, so `when` names that situation and every field it names must equal the query's (an absent field answers any value).
// Nothing here falls back: no match is undefined, and the host plays the act's `scenes.items` scene as it does today. So a different person never gets these words.
// lookupConversation is the reference Latch ports, as text.schema's lookupScene.
import { z } from 'zod';
import { TEMPERAMENTS } from '../../people/people.rules.schema.ts';

/** The people a conversation can hear: the cell, the cast, and `milena`, a voice from inside the café with no figure in the shot. */
export const CONVERSATION_SPEAKERS = ['scout', 'fixer', 'waitress', 'driver', 'guard', 'milena'] as const;
export const CONVERSATION_VERBS = ['listen', 'ask', 'pay', 'help', 'pressure', 'call', 'mislead'] as const;
/** The approaches that can turn a person: how `turnedBy` reads. */
export const TURNED_BY = ['ask', 'pay', 'help', 'pressure'] as const;
/** agreed: Sky read these words and agreed them. awaiting-sky: written through the generator, not yet read by him. */
export const CONVERSATION_STATUS = ['agreed', 'awaiting-sky'] as const;
export const CONVERSATION_MAX_BEATS = 10;
export const CARD_WORDS = 6;

const Spoken = z.string().min(1).max(80).regex(/^[^\n<>{}!]+$/);
const CardLine = z.string().min(1).max(44).regex(/^[^\n<>{}!]+$/).refine((x) => x.trim().split(/\s+/).length <= CARD_WORDS, `a card is at most ${CARD_WORDS} words`);

export const ConversationWhen = z.strictObject({
  verb: z.enum(CONVERSATION_VERBS),
  about: z.enum(['waitress', 'driver', 'guard', 'councillor']),
  temperament: z.enum(TEMPERAMENTS).describe('required: these words are written for one temperament and must not play over another'),
  outcome: z.string().optional().describe('how the approach landed (TurnOutcome: turned, not-yet, kept, refused, suspicious). The engine\'s outcome, not a count, decides which Help visit plays'),
  turned: z.boolean().optional().describe('the person was already turned when this act landed'),
  helped: z.boolean().optional().describe('an earlier Help on this person has already landed (log turn.help.* before this act\'s own)'),
  turnedBy: z.enum(TURNED_BY).optional().describe('call: the approach that turned this person (log turn.<approach>.turned)'),
});
export const Conversation = z.strictObject({
  id: z.string().regex(/^[a-z][a-z0-9-]*$/).describe('the scene\'s id: First time for each scene (§10) counts by it'),
  title: z.string().min(1).max(40),
  status: z.enum(CONVERSATION_STATUS),
  from: z.string().min(1).describe('where the words came from'),
  when: ConversationWhen,
  beats: z.array(z.strictObject({ speaker: z.enum(CONVERSATION_SPEAKERS), line: Spoken })).min(4).max(CONVERSATION_MAX_BEATS),
  street: z.array(z.strictObject({
    on: z.string().regex(/^[a-z][a-z0-9.-]*$/).describe('the engine log code that plays them'),
    beat: z.string().min(1).max(24).describe('the feed line when no one is near enough to hear: at most four words'),
    beats: z.array(z.strictObject({ speaker: z.enum(CONVERSATION_SPEAKERS), line: Spoken })).min(1).max(3),
  })).optional().describe('what follows the cutaway in play, clock running, as speech bubbles, when the engine logs `on`. It belongs to the route, so it plays whether or not the cutaway did, and in place of that event\'s usual lines'),
  price: z.strictObject({ on: z.string().min(1).describe('the text.json path the line replaces'), line: Spoken })
    .optional().describe('a price line that goes live with this scene, not before'),
  learned: z.array(CardLine).min(1).max(2).describe('the cards: what the player now knows'),
  opens: z.array(CardLine).min(1).max(2).describe('the cards: what the player can now do, or where to be'),
});
export type Conversation = z.infer<typeof Conversation>;
export const Conversations = z.strictObject({
  rule: z.string().min(1),
  names: z.record(z.enum(CONVERSATION_SPEAKERS), z.string().min(1).max(12)).describe('the speaker label a conversation shows. The rest of the game still says Guard and Driver (names.speakers)'),
  items: z.array(Conversation).min(1),
});
export type Conversations = z.infer<typeof Conversations>;

export type ConversationQuery = { verb: string; about: string; temperament: string; outcome?: string; turned?: boolean; helped?: boolean; turnedBy?: string };

/** The conversation for an act that has landed, or undefined: then the act plays its `scenes.items` scene. The file holds at most one match for any query (conversations.test.ts). */
export function lookupConversation(c: Conversations | undefined, q: ConversationQuery): Conversation | undefined {
  return c?.items.find(({ when: w }) => w.verb === q.verb && w.about === q.about && w.temperament === q.temperament
    && (w.outcome === undefined || w.outcome === q.outcome) && (w.turned === undefined || w.turned === q.turned)
    && (w.helped === undefined || w.helped === q.helped) && (w.turnedBy === undefined || w.turnedBy === q.turnedBy));
}

/** A conversation's lines in order, each with its speaker key (where the bubble goes) and the name it shows. */
export const conversationLines = (c: Conversations, sc: Conversation): { speaker: string; name: string; text: string }[] =>
  sc.beats.map((b) => ({ speaker: b.speaker, name: c.names[b.speaker] ?? b.speaker, text: b.line }));
