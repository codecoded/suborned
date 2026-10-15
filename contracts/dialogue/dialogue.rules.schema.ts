// suborned.dialogue.rules/1: the rules the generated words obey, contracts/dialogue/dialogue.rules.json (Fable, brief 012). The model writes
// tables (contracts/dialogue/voices.schema.ts) by voice group × temperament × situation; seeded code expands them into each person's
// lines and scenes (tools/dialogue/expand.ts); the checker (tools/dialogue/check.ts, D-rules) holds both to this file. Anything the
// file does not list is rejected (GDD §01: unknown = reject). Which positions, roles, temperaments, needs and ties exist is
// people.rules.json's; which replies a temperament can give is the rulebook's (derived, never typed here).
import { z } from 'zod';
import { TEMPERAMENTS, TIE_KINDS } from '../people/people.rules.schema.ts';

const id = z.string().regex(/^[a-z][a-z0-9-]*$/);
const text = z.string().min(1);
const slot = z.string().regex(/^[a-z][a-z0-9_]*$/);
const range = z.strictObject({ min: z.number().int().min(1), max: z.number().int().min(1) }).refine((r) => r.min <= r.max, { message: 'min > max' });

export const BLOCKS = ['lexicon', 'groups', 'scenes'] as const;
/** The words a template may carry between braces, by where it stands. */
export const SLOT_CONTEXTS = ['cell', 'reads', 'scene', 'summons'] as const;
export const SCENE_VERBS = ['listen', 'watch', 'call'] as const;

export const DialogueRules = z.strictObject({
  schema: z.literal('suborned.dialogue.rules/1'),
  author: text, gdd: text, rule: text,
  limits: z.strictObject({
    maxLine: z.number().int().positive(), minWords: z.number().int().min(1), maxWords: z.number().int().positive(),
    cellLines: range, sceneVariants: range, sceneBeats: range, addressTerms: range,
    debriefMax: z.number().int().positive().describe('a debrief line carries its cause, so it may run longer than a spoken line; the host fills each field at its widest (lexicon.debriefFill)'),
    qualifiedCellsPerGroup: z.number().int().min(0),
    templateMax: z.number().int().positive().describe('a template may be longer than a line by its slots; the filled line is held to maxLine'),
  }),
  groups: z.record(id, z.strictObject({
    covers: z.array(id).min(1).describe('positions of the people rules (a trade category\'s) and roles of its civic employers; each belongs to exactly one group'),
    without: z.array(slot).optional().describe('cell slots this group may not use, because some of its people lack what fills them (a resident has no workplace)'),
    why: text,
  })),
  scopes: z.record(id, z.strictObject({
    blocks: z.array(z.enum(BLOCKS)).min(1),
    groups: z.array(id).optional(),
    why: text,
  })),
  slots: z.strictObject({
    cell: z.array(slot).describe('in a person\'s own lines: address (how the group addresses a stranger), job, noun, work, need, own, met'),
    reads: z.array(slot).describe('in the Scout\'s read-outs about a person: job, noun, work, need, own, met'),
    scene: z.array(slot).describe('in a scene beat: focus, other (given names), work, need, own, met; a Watch scene surfaces no need'),
    summons: z.array(slot).describe('in a lover\'s summons lines: other (the lover\'s given name)'),
    needIn: z.array(z.string()).describe('where a need slot (need, own, met) may stand: a person\'s reply to these `event/verb`, to questioning, and in scenes. A cold Ask or a Pay never surfaces the need (§04: Help and Pressure are what need known unlocks)'),
    needSlots: z.array(slot),
    familyNoun: text.describe('what {other_noun} says for a family tie, where the partner\'s job would be wrong'),
  }),
  situations: z.strictObject({
    reply: z.strictObject({ event: id, verbs: z.array(id), derive: z.literal('rulebook turn events × temperament × band') }),
    confirm: z.strictObject({ event: id, verb: id }),
    questioned: z.strictObject({ event: id, outcomes: z.array(id).min(1) }),
    world: z.array(id).describe('events any person may speak, one cell each, no temperament'),
  }),
  lexicon: z.strictObject({
    readsListen: z.literal('one line set per temperament'),
    summonKeys: z.array(id).min(1).describe('the engine\'s summons refusal codes, and the visit\'s end; each has the caller\'s lines and the summoned\'s'),
    summonCodesFrom: z.string().describe('the engine file the codes are read from when it is present (a drift test)'),
    debriefFill: z.record(id, text).describe('the widest the host fills each rulebook debrief field (rulebook debrief.lines[*].fields), so a debrief line is held to its width before it is shown'),
  }),
  listen: z.strictObject({
    tieByNeed: z.record(id, id).describe('the tie kind a Listen scene on a person with this need uses first, when they hold one'),
    tieOrder: z.array(id).describe('then these, first present'),
    secretFirst: z.literal(true).describe('a person with the affair is overheard with their lover'),
    sides: z.record(id, text).describe('who is who in a directed tie, so one template reads right'),
    needFacts: z.record(id, text).describe('what the Learned line says of a need: "is in debt". Words for a mechanic, so they live here, not in the model\'s tables'),
    results: z.strictObject({ listen: text, secretAffair: text, secretRecord: text, watch: text, watchLovers: text, call: text }),
  }),
  tone: z.strictObject({
    world: text,
    avoid: z.array(z.string()).describe('modern speech, internet words, period-slang overload: rejected as whole words'),
    profanity: z.array(z.string()).describe('strong profanity, rejected as word starts; mild (damn) is allowed'),
    rating: z.strictObject({ explicit: z.array(z.string()), gore: z.array(z.string()) }).describe('the rating filter (GDD §09): nothing explicit, nothing gory; a secret is written plainly and with tact'),
    gendered: z.array(z.string()).describe('no one is gendered: people carry no gender, and the Fixer is not gendered in any line. A line says "they", a name or a post, never he, she, sir, madam, brother or doorman (whole words, case-insensitive)'),
    signal: z.array(z.string()).describe('the game\'s own words: a person never says the cell\'s names, a temperament\'s label, Trail, Heat or a mission word'),
    pending: z.array(z.string()).describe('names awaiting Sky (Story): no generated line names them'),
    payload: z.array(z.string()).describe('civilians are never the payload: no line asks for a human shield, a suicide or a rigged person (regular expressions, case-insensitive)'),
    capitals: z.array(z.string()).describe('capitalised words a template may carry mid-sentence besides the cast\'s names'),
  }),
  pick: z.strictObject({ rule: text }),
}).superRefine((r, ctx) => {
  const bad = (path: (string | number)[], message: string) => ctx.addIssue({ code: 'custom', path, message });
  const seen = new Map<string, string>();
  for (const [g, v] of Object.entries(r.groups)) for (const c of v.covers) { if (seen.has(c)) bad(['groups', g, 'covers'], `${c} is covered by ${seen.get(c)} and ${g}`); seen.set(c, g); }
  const inScope = new Map<string, string>();
  for (const [s, v] of Object.entries(r.scopes)) {
    const needsGroups = v.blocks.includes('groups');
    if (needsGroups !== !!v.groups?.length) bad(['scopes', s], 'a scope with the groups block lists its groups, and only such a scope does');
    for (const g of v.groups ?? []) { if (!r.groups[g]) bad(['scopes', s, 'groups'], `group ${g} is unknown`); if (inScope.has(g)) bad(['scopes', s, 'groups'], `${g} is in scope ${inScope.get(g)} and ${s}`); inScope.set(g, s); }
  }
  for (const g of Object.keys(r.groups)) if (!inScope.has(g)) bad(['groups', g], 'a group no scope writes');
  const blocks = Object.values(r.scopes).flatMap((s) => s.blocks);
  for (const b of ['lexicon', 'scenes'] as const) if (blocks.filter((x) => x === b).length !== 1) bad(['scopes'], `the ${b} block is written by exactly one scope`);
  const cells = new Set([...r.slots.cell, ...r.slots.reads]);
  for (const s of r.slots.needSlots) if (!cells.has(s) && !r.slots.scene.includes(s)) bad(['slots', 'needSlots'], `${s} is not a slot`);
  for (const g of Object.entries(r.groups)) for (const w of g[1].without ?? []) if (!r.slots.cell.includes(w)) bad(['groups', g[0], 'without'], `${w} is not a cell slot`);
  for (const t of Object.keys(r.listen.tieByNeed)) void t;
  for (const k of [...Object.values(r.listen.tieByNeed), ...r.listen.tieOrder]) if (!(TIE_KINDS as readonly string[]).includes(k)) bad(['listen'], `tie kind ${k} is unknown`);
  for (const re of r.tone.payload) { try { new RegExp(re, 'i'); } catch { bad(['tone', 'payload'], `${re} does not compile`); } }
});
export type DialogueRules = z.infer<typeof DialogueRules>;
export type Temperament = (typeof TEMPERAMENTS)[number];
