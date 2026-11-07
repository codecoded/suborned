import { z } from 'zod';
import type { Authorization, Call, Evidence, Limits, Source } from './types';

/** Short lines (Sky 2026-11-07, asked whether the loop covers more than the Inez conversations: "sure", so nothing ships unscored): the barks, the why block and the debrief page, written by
 *  Fable and scored by Motive in the same loop as the conversations, a family per run. */
export const SHORT_KINDS = ['bark', 'why', 'debrief'] as const;
export type ShortKind = (typeof SHORT_KINDS)[number];
/** The rubric's rules: the brief's five (plain, true, short, useful, named as known) and the voice of who says it. */
export const SHORT_RULES = ['USEFUL', 'TRUE', 'PLAIN', 'SHORT', 'NAMES', 'VOICE'] as const;
export const ShortRuleId = z.enum(SHORT_RULES);
export type ShortRuleId = z.infer<typeof ShortRuleId>;
/** /1 the first live run's protocol; /2 (2026-11-07, after it): the evaluator sees its own last findings, the reviser changes only the lines they name;
 *  /3: the barks also carry the words text.test.ts pins on them (Orbit Walk, "outside", no Alert, no "reported"…). A run replays with its version. */
/** /4 (after the first chain read like tooltips: "I'll make the camera repeat pictures for two minutes."): a bark says what its item says, in voice; an order's price is on
 *  its card and is never read back; a line another speaker could say fails VOICE; today's lines that already work are kept; acknowledgements are scored by verb, all four agents together. */
/** /5: a bark is also checked against the other rows' lines in the text it ran on, so two families never ship the same line ("Going in." was the Enforcer's ack and became his arrive). */
/** /6: the witnesses' family in two (its evaluation ran past the 3-minute call limit twice); the page's SHORT rule counts a fill as one word, as the why rule does since /5; Popovic's Listen reads say how he takes being taken (the cell has no approach on him); a missed check-in the cell hasn't watched never says check-in. */
/** /7: a why text keeps the fills today's has for who saw it, whom, the agent, the report point and the seconds (the /4 and /5 families dropped them: "the desk saw
 *  payment on tape", a plate with no countdown), and a plate that counts down ends " · {s} s", as brief 100 made it. */
/** /8: the guard's call and the plain acknowledgements carry conversations.test.ts's banned words (the spill, an envelope, a bonus, a promise, a time such as
 *  "a minute"), and the guard at the café is no longer briefed as "unguarded for about a minute" (the /4 bark/guard-call run wrote the GDD's 60 s into the lines). */
export const SHORT_GUIDES = { 1: 'suborned.style-short-guide/1', 2: 'suborned.style-short-guide/2', 3: 'suborned.style-short-guide/3', 4: 'suborned.style-short-guide/4', 5: 'suborned.style-short-guide/5', 6: 'suborned.style-short-guide/6', 7: 'suborned.style-short-guide/7', 8: 'suborned.style-short-guide/8' } as const;
export type ShortGuideVersion = keyof typeof SHORT_GUIDES;
/** The version new runs use; a saved run replays with its own. */
export const SHORT_NOW: ShortGuideVersion = 8;
export const shortGuideVersion = (g: Pick<ShortGuide, 'schema'>): ShortGuideVersion => (Number(Object.entries(SHORT_GUIDES).find(([, v]) => v === g.schema)?.[0] ?? 4)) as ShortGuideVersion;
export const SHORT_RUN = 'suborned.style-short-run/1';

/** How a line is held: speech (a bubble), the why block's title, short, line and plate, a page label, a note after a page row. */
export const FORMS = ['speech', 'title', 'short', 'line', 'plate', 'label', 'note'] as const;
export type Form = (typeof FORMS)[number];
export type Fill = { id: string; means: string; widest: string };
/** A rule a check holds one line to: `must` match it, or never match it (a pinned proof word, a test's ban), with the reason. */
export type Pattern = { re: string; flags: string; must: boolean; why: string };
/** One thing to write for an item: `count` variants (a bark keeps today's count), each at most `words` and `chars` at its widest fills;
 *  `keep` lines a test pins word for word, which stay among the variants. */
export type Field = { id: string; form: Form; words: number; chars: number; count: number; fills: Fill[]; keep: string[]; patterns: Pattern[] };
/** One key of text.json to write. `tells` is what the line must tell the player and what the engine did: the evaluator's truth.
 *  `stays` says why the item may not be cut (the host or a check needs it); null = a bark the loop may cut, with its reason. */
export type ShortItem = { key: string; where: string; tells: string; speaker: string | null; fields: Field[]; current: Record<string, string[]>; stays: string | null };
export type ShortBrief = { id: string; kind: ShortKind; title: string; context: string; items: ShortItem[] };
export type ShortRule = { id: ShortRuleId; kind: string; instruction: string; sources: string[] };
export type ShortGuide = {
  schema: (typeof SHORT_GUIDES)[ShortGuideVersion]; game: 'The Suborned'; gdd: string; sources: Source[];
  kinds: Record<ShortKind, { use: string; rules: ShortRule[] }>;
  voices: Record<string, string>; avoid: string[]; profanity: string[]; named: string[]; cell: string[];
};

/** What Fable returns: every item of the family once, each field with its lines, or the item cut with its reason. */
export const ShortSet = z.object({ items: z.array(z.object({
  key: z.string().min(1), cut: z.boolean(), reason: z.string().max(400).describe('Why it is cut, in one sentence; empty when it is kept.'),
  fields: z.array(z.object({ id: z.string().min(1), lines: z.array(z.string().min(1).max(200)).max(4) }).strict()).max(4),
}).strict()).min(1).max(60) }).strict();
export type ShortSet = z.infer<typeof ShortSet>;
export const shortSetSchema = (b: ShortBrief) => {
  const keys = b.items.map(i => i.key) as [string, ...string[]], ids = [...new Set(b.items.flatMap(i => i.fields.map(f => f.id)))] as [string, ...string[]];
  return ShortSet.extend({ items: z.array(z.object({
    key: z.enum(keys), cut: z.boolean(), reason: z.string().max(400).describe('Why it is cut, in one sentence; empty when it is kept.'),
    fields: z.array(z.object({ id: z.enum(ids), lines: z.array(z.string().min(1).max(200)).max(4) }).strict()).max(4),
  }).strict()).min(1).max(60) });
};
export const ShortEvaluation = z.object({
  score: z.number().int().min(1).max(10),
  reason: z.string().min(1).max(3000),
  findings: z.array(z.object({
    rule: ShortRuleId,
    key: z.string().max(80).describe('The item key the finding is about; empty only for something about the whole family.'),
    quote: z.string().max(400).describe('An exact excerpt of one line; empty only for something missing or for a cut.'),
    reason: z.string().min(1).max(1200),
    fix: z.string().min(1).max(1200),
  }).strict()).max(24),
}).strict();
export type ShortEvaluation = z.infer<typeof ShortEvaluation>;
export type ShortIssue = { rule: ShortRuleId; reason: string };
export type ShortRound = { set: ShortSet; evaluation: ShortEvaluation; hardIssues: ShortIssue[] };
export type ShortResult = {
  schema: typeof SHORT_RUN; mode: 'live' | 'test-fixture';
  status: 'ACCEPTED' | 'INCOMPLETE' | 'ERROR'; stopReason: string;
  startedAt: number; finishedAt: number | null;
  evidence: Evidence | null; authorization: Authorization | null;
  brief: ShortBrief; guide: ShortGuide; limits: Limits; rounds: ShortRound[]; calls: Call[];
};

const Sha = z.string().regex(/^[a-f0-9]{64}$/);
const Time = z.number().int().nonnegative();
const FillSchema = z.object({ id: z.string(), means: z.string(), widest: z.string() }).strict();
export const ShortBriefSchema = z.object({
  id: z.string().regex(/^(bark|why|debrief)\/[a-z0-9-]+$/), kind: z.enum(SHORT_KINDS), title: z.string().min(1), context: z.string().min(1),
  items: z.array(z.object({
    key: z.string().min(1), where: z.string().min(1), tells: z.string().min(1), speaker: z.string().nullable(),
    fields: z.array(z.object({ id: z.string().min(1), form: z.enum(FORMS), words: z.number().int().positive(), chars: z.number().int().positive(),
      count: z.number().int().min(1).max(4), fills: z.array(FillSchema), keep: z.array(z.string()),
      patterns: z.array(z.object({ re: z.string(), flags: z.string(), must: z.boolean(), why: z.string() }).strict()) }).strict()).min(1),
    current: z.record(z.string(), z.array(z.string())), stays: z.string().nullable(),
  }).strict()).min(1).max(60),
}).strict();
const RuleSchema = z.object({ id: ShortRuleId, kind: z.string(), instruction: z.string(), sources: z.array(z.string()) }).strict();
const KindSchema = z.object({ use: z.string(), rules: z.array(RuleSchema).min(5) }).strict();
export const ShortGuideSchema = z.object({
  schema: z.enum([SHORT_GUIDES[1], SHORT_GUIDES[2], SHORT_GUIDES[3], SHORT_GUIDES[4], SHORT_GUIDES[5], SHORT_GUIDES[6], SHORT_GUIDES[7], SHORT_GUIDES[8]]), game: z.literal('The Suborned'), gdd: z.string(),
  sources: z.array(z.object({ id: z.string(), file: z.string(), locator: z.string(), sha256: Sha, excerpt: z.string() }).strict()).min(1),
  kinds: z.object({ bark: KindSchema, why: KindSchema, debrief: KindSchema }).strict(),
  voices: z.record(z.string(), z.string()), avoid: z.array(z.string()), profanity: z.array(z.string()), named: z.array(z.string()), cell: z.array(z.string()),
}).strict();
export const ShortResultSchema = z.object({
  schema: z.literal(SHORT_RUN), mode: z.enum(['live', 'test-fixture']),
  status: z.enum(['ACCEPTED', 'INCOMPLETE', 'ERROR']), stopReason: z.string(),
  startedAt: Time, finishedAt: Time.nullable(),
  evidence: z.object({
    sources: z.array(z.object({ file: z.string(), sha256: Sha }).strict()).min(1).max(30),
    tooling: z.array(z.object({ file: z.string(), sha256: Sha }).strict()).min(1).max(80), toolingSha256: Sha,
  }).strict().nullable(),
  authorization: z.object({ provider: z.literal('codex'), model: z.string().min(1), approvedBy: z.string().trim().min(1) }).strict().nullable(),
  brief: ShortBriefSchema, guide: ShortGuideSchema,
  limits: z.object({ maxCalls: z.number(), maxTokens: z.number(), maxRevisions: z.number(), maxMs: z.number() }).strict(),
  rounds: z.array(z.object({ set: ShortSet, evaluation: ShortEvaluation,
    hardIssues: z.array(z.object({ rule: ShortRuleId, reason: z.string() }).strict()) }).strict()).max(4),
  calls: z.array(z.object({
    stage: z.enum(['generate', 'evaluate', 'refine']), role: z.enum(['dialogue-designer', 'systems-designer']), prompt: z.string(),
    startedAt: Time, finishedAt: Time.nullable(), timeoutMs: z.number().int().positive().max(180000), schemaSha256: Sha,
    output: z.unknown().optional(), raw: z.unknown().optional(), tokens: z.number().nullable(), error: z.string().optional(),
  }).strict()).max(20),
}).strict();
