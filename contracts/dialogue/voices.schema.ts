// suborned.voices/1: the tables the model writes (Fable, brief 012), one file per scope (contracts/dialogue/dialogue.rules.json `scopes`), and
// suborned.lines/1: what seeded code makes of them for each person (tools/dialogue/expand.ts). The model writes how a KIND of person sounds,
// by voice group × temperament × situation, and the shared words; it never writes a person. Which groups, scopes, slots and tone limits
// exist is dialogue.rules.json's; the checker (tools/dialogue/check.ts, D-rules) rejects anything outside it. Both files are build-time
// content (GDD §01: no AI at runtime): the host reads suborned.lines/1 and picks with the same pickIndex it uses for suborned.text/1.
//
// Voices (the proposal):
//   groups[].cells   event × verb × outcome × temperament, plus the optional qualifiers `need` and `secret` (the lookup is text.schema's:
//                    the cell naming the most fields wins, so a qualified cell is a better answer for the person who has that need or secret)
//   groups[].reads   the Scout's Watch read-out for the group
//   scenes           the overheard and called scenes: Listen by tie kind × temperament, the lovers' Watch and call, each group's Watch
//   lexicon          needs as phrases, positions as a job and a noun, the Scout's Listen read-outs, the debrief lines, the summons lines
// Lines (the expansion): per person, entries keyed as text.json's `dialogue` is (speaker implicit: the person), their scenes with the
// partner chosen from their ties, and the shared debrief lines.
import { z } from 'zod';
import { SECRETS, TEMPERAMENTS, TIE_KINDS } from '../people/people.rules.schema.ts';

const word = z.string().regex(/^[a-z][a-z0-9-]*$/);
/** One template: one line, no markup. `{slot}` words are filled per person; the filled line is held to 80 characters (the rules `limits`). */
const template = z.string().min(1).max(110).regex(/^[^\n<>]+$/);
const temperament = z.enum(TEMPERAMENTS);
const lines = (min: number, max: number) => z.array(template).min(min).max(max);

export const Cell = z.strictObject({
  event: word.describe('reply · confirm · questioned · saw-gunfire · gunfire'),
  verb: word.optional().describe('reply: ask · pay · help · pressure. confirm: call'),
  outcome: word.optional().describe('reply: turned · refused · not-yet · kept · suspicious. questioned: gives · lies · nothing'),
  temperament: temperament.optional().describe('the voice; reply and confirm have one cell per temperament the group can have'),
  need: word.optional().describe('a qualified cell: only for a person with this need (the rules\' list)'),
  secret: z.enum(SECRETS).optional().describe('a qualified cell: only for a person who holds this secret'),
  lines: lines(2, 4),
});
export type Cell = z.infer<typeof Cell>;

export const BEAT_WHO = ['focus', 'other', 'bystander', 'scout'] as const;
export const Beat = z.strictObject({
  who: z.enum(BEAT_WHO).describe('focus: the person overheard. other: their partner on the tie, by name. bystander: an unnamed colleague, customer or passer-by. scout: the Scout\'s aside (Watch scenes only)'),
  line: template,
});
export const SceneT = z.strictObject({
  verb: z.enum(['listen', 'watch', 'call']),
  tie: z.enum([...TIE_KINDS, 'none']).optional().describe('listen and call: the tie kind between focus and other; none: focus talks with a bystander'),
  group: word.optional().describe('watch: the voice group of the person watched'),
  temperament: temperament.optional().describe('listen and call: the temperament of the focus'),
  variants: z.array(z.strictObject({ beats: z.array(Beat).min(3).max(6) })).min(2).max(3),
});
export type SceneT = z.infer<typeof SceneT>;

export const Group = z.strictObject({
  group: word,
  voice: z.string().min(1).max(200).describe('how this group talks, one line'),
  address: z.array(z.string().min(1).max(16).regex(/^[^\n<>{}]+$/)).min(3).max(6).describe('how they address a stranger: honey, sir, citizen. {address} is one of these, chosen per person'),
  cells: z.array(Cell).min(1),
  reads: z.strictObject({ watch: lines(2, 4) }).describe('the Scout\'s read-out after Watching one of them'),
});

const phrase = z.string().min(1).max(40).regex(/^[^\n<>{}]+$/);
export const Lexicon = z.strictObject({
  needs: z.array(z.strictObject({ need: word, noun: phrase.describe('"debt": the bare noun, lower case'), own: phrase.describe('"my debt": the first person'), met: phrase.describe('a clause for a met need: "the debt is gone"') })),
  positions: z.array(z.strictObject({ position: word, job: phrase.describe('what they do, after "I": "pour the coffee"'), noun: phrase.describe('what the cell calls them, with its article: "the server", "the guard"') })),
  reads: z.array(z.strictObject({ temperament, lines: lines(2, 4) })).describe('the Scout after a Listen: one set per temperament; {need} {own} {met} allowed'),
  debrief: z.array(z.strictObject({ id: word, lines: lines(2, 4) })).describe('the debrief line of each rulebook debrief id, with its fields as {slots}'),
  summons: z.array(z.strictObject({ key: word, caller: lines(2, 3).describe('the turned contact who called, saying why the visit will not happen'), summoned: lines(2, 3).describe('the one called, saying it') })),
});
export type Lexicon = z.infer<typeof Lexicon>;

export const Voices = z.strictObject({
  schema: z.literal('suborned.voices/1'),
  scope: word,
  groups: z.array(Group),
  scenes: z.array(SceneT),
  lexicon: Lexicon.optional(),
});
export type Voices = z.infer<typeof Voices>;
export type Group = z.infer<typeof Group>;

// ---------------------------------------------------------------- suborned.lines/1: the expansion

const person = z.string().regex(/^p-[a-z0-9-]+-\d{2,3}$/);
/** A text.json `dialogue` entry for one person (the speaker is the person): the host finds it with text.schema's lookup and picks with pickIndex. */
export const LineEntry = z.strictObject({
  event: word, verb: word.optional(), outcome: word.optional(), temperament: temperament.optional(),
  side: z.enum(['caller', 'summoned']).optional().describe('summon entries: which end of the call speaks'),
  speaker: z.enum(['person', 'scout']).describe('person: the person speaks. scout: the Scout speaks about them'),
  lines: z.array(z.string().min(1).max(80)).min(2).max(4),
});
export type LineEntry = z.infer<typeof LineEntry>;
export const PlayedBeat = z.strictObject({ speaker: z.union([person, z.literal('bystander'), z.literal('scout')]), line: z.string().min(1).max(80) });
export const PersonScene = z.strictObject({
  verb: z.enum(['listen', 'watch', 'call']), tie: z.enum([...TIE_KINDS, 'none']), with: person.optional().describe('the partner on the tie'),
  learns: z.array(z.enum(['routine', 'need', 'temperament', 'secret'])),
  result: z.string().min(1).max(160).describe('what the player now knows; {temperament} is filled by the host from engine state'),
  variants: z.array(z.strictObject({ beats: z.array(PlayedBeat).min(3).max(6) })).min(2).max(3),
});
export const PersonLines = z.strictObject({
  id: person, group: word, temperament, need: word, secret: z.enum(SECRETS).nullable(), address: z.string(),
  entries: z.array(LineEntry), scenes: z.array(PersonScene),
});
export type PersonLines = z.infer<typeof PersonLines>;
export const Lines = z.strictObject({
  schema: z.literal('suborned.lines/1'),
  plan: z.strictObject({ id: z.string(), version: z.string() }), seed: z.string(),
  inputs: z.strictObject({ people: z.string(), rules: z.string(), tables: z.record(z.string(), z.string()), tools: z.string().optional() }),
  persons: z.array(PersonLines),
  debrief: z.array(z.strictObject({ id: word, lines: z.array(z.string()).min(2).max(4) })),
});
export type Lines = z.infer<typeof Lines>;

/** suborned.voices.promotion/1: the record beside a promoted scope file (contracts/dialogue/<plan id>.<scope>.voices.json), as contracts/people's cast promotion is. The voices file is
 *  the run's accepted `voices` byte for byte (JSON.stringify(voices, null, 1), no trailing newline); tools/dialogue/promoted.ts holds the file to this record and to the run on disk. */
export const VoicesPromotion = z.strictObject({
  schema: z.literal('suborned.voices.promotion/1'),
  plan: word, scope: word,
  voices: z.strictObject({ file: z.string(), sha256: z.string().regex(/^[0-9a-f]{64}$/), bytes: z.number().int().positive() }),
  source: z.strictObject({
    run: z.string().regex(/^r\d{3}$/), round: z.number().int().min(1), version: z.string(), file: z.string(), path: z.literal('voices'),
    sha256: z.string().regex(/^[0-9a-f]{64}$/), runFile: z.string(), outcome: z.literal('ACCEPTED'),
  }),
  promoted: z.string().describe('who promoted it and when, as Sky\'s ruling was given: "Sky, 2026-10-16"'),
  why: z.string(),
});
export type VoicesPromotion = z.infer<typeof VoicesPromotion>;
