import { z } from 'zod';

/** The style guide's constraint types. Each rule cites the game sources it is drawn from. Guide /2 (brief 054) has the first four; /3 (brief 096, cutaway
 *  conversations in full sentences) adds NATURAL; /4 (brief 102, Sky 2026-11-08) holds NATURAL to the no-mechanics rule: nobody explains the game.
 *  A saved run keeps its guide's version, so it replays with the rules and evaluation schema it ran with. */
const RULES_V2 = ['REVEAL', 'VOICE', 'WORLD', 'FORM'] as const;
export const RuleId = z.enum([...RULES_V2, 'NATURAL']);
export type RuleId = z.infer<typeof RuleId>;
export const GUIDE_VERSIONS = { 2: 'suborned.style-guide/2', 3: 'suborned.style-guide/3', 4: 'suborned.style-guide/4' } as const;
export type GuideVersion = keyof typeof GUIDE_VERSIONS;
export const guideVersion = (g: Pick<Guide, 'schema'>): GuideVersion => g.schema === GUIDE_VERSIONS[2] ? 2 : g.schema === GUIDE_VERSIONS[4] ? 4 : 3;
/** The version new conversation runs use; a saved run replays with its own. */
export const GUIDE_NOW: GuideVersion = 4;
/** Crew cards (generated from GDD §14). Fable writes and refines; Motive reviews Fable's lines ("Motive reviews", §14 Fable). */
export const Role = z.enum(['dialogue-designer', 'systems-designer']);
export type Role = z.infer<typeof Role>;
export type Stage = 'generate' | 'evaluate' | 'refine';

export type Source = { id: string; file: string; locator: string; sha256: string; excerpt: string };
export type Rule = { id: RuleId; kind: string; instruction: string; sources: string[] };
export type Guide = {
  schema: (typeof GUIDE_VERSIONS)[GuideVersion]; game: 'The Suborned'; gdd: string;
  sources: Source[]; rules: Rule[];
  voices: Record<string, string>; avoid: string[]; profanity: string[];
  bubble: { seconds: number; maxWords: number; maxChars: number };
  /** /3 only: a cutaway conversation's limits (contracts/missions/handover/conversations.schema.ts). */
  cutaway?: { maxWords: number; maxChars: number; maxSentences: number; maxLines: number };
};

/** One part of a conversation: who Inez talks to and what that exchange must make plain. */
export type Part = { with: string; reveals: string[]; lines: [number, number] };
/** A conversation to write, built from an existing Handover scene: its act, its people and what it must teach. */
export type Brief = {
  id: string; scene: number; kind: 'overheard' | 'approach' | 'call'; verb: string;
  temperament: string | null; outcome: string | null;
  cast: string[]; parts: Part[];
  seconds: number; lines: [number, number];
  reveals: string[]; withholds: string[]; leadsTo: string;
  result: string | null; current: { speaker: string; line: string }[];
};

export const Line = z.object({ speaker: z.string().min(1), line: z.string().min(1).max(400) }).strict();
export type Line = z.infer<typeof Line>;
export const Conversation = z.object({ lines: z.array(Line).min(1).max(40) }).strict();
export type Conversation = z.infer<typeof Conversation>;
const evaluation = <R extends z.ZodType>(rule: R) => z.object({
  score: z.number().int().min(1).max(10),
  reason: z.string().min(1).max(3000),
  findings: z.array(z.object({
    rule,
    quote: z.string().max(2000).describe('An exact excerpt of one line; empty only for something missing from the whole conversation.'),
    reason: z.string().min(1).max(1200),
    fix: z.string().min(1).max(1200),
  }).strict()).max(20),
}).strict();
export const Evaluation = evaluation(RuleId);
export type Evaluation = z.infer<typeof Evaluation>;
/** The schema the evaluator answers in: its guide's rules only, so a /2 run's recorded schema hash still matches. */
export const evaluationFor = (g: Pick<Guide, 'schema'>): z.ZodType<Evaluation> => guideVersion(g) === 2 ? evaluation(z.enum(RULES_V2)) : Evaluation;
export type HardIssue = { rule: RuleId; reason: string };

export type Request = { stage: Stage; role: Role; prompt: string; schema: z.ZodType; timeoutMs: number };
/** The loop owns accounting and validation. Adapters never write files or touch the game. */
export type Invoke = (request: Request) => Promise<{ output: unknown; tokens: number | null; raw?: unknown }>;
export type Limits = { maxCalls: number; maxTokens: number; maxRevisions: number; maxMs: number };
export type Call = {
  stage: Stage; role: Role; prompt: string; startedAt: number; finishedAt: number | null;
  timeoutMs: number; schemaSha256: string; output?: unknown; raw?: unknown; tokens: number | null; error?: string;
};
export type Round = { conversation: Conversation; evaluation: Evaluation; hardIssues: HardIssue[] };
/** Sources are stored once under runs/style-agent/sources/<sha256>; a run names them by hash. */
export type Evidence = { sources: { file: string; sha256: string }[]; tooling: { file: string; sha256: string }[]; toolingSha256: string };
export type Authorization = { provider: 'codex'; model: string; approvedBy: string };
export type Result = {
  schema: 'suborned.style-run/2'; mode: 'live' | 'test-fixture';
  status: 'ACCEPTED' | 'INCOMPLETE' | 'ERROR'; stopReason: string;
  startedAt: number; finishedAt: number | null;
  evidence: Evidence | null; authorization: Authorization | null;
  brief: Brief; steer: string | null; guide: Guide; limits: Limits;
  rounds: Round[]; calls: Call[];
};

const Sha = z.string().regex(/^[a-f0-9]{64}$/);
const Time = z.number().int().nonnegative();
const Range = z.tuple([z.number().int().nonnegative(), z.number().int().positive()]);
export const BriefSchema = z.object({
  id: z.string().min(1), scene: z.number().int().nonnegative(), kind: z.enum(['overheard', 'approach', 'call']), verb: z.string(),
  temperament: z.string().nullable(), outcome: z.string().nullable(), cast: z.array(z.string()).min(2),
  parts: z.array(z.object({ with: z.string(), reveals: z.array(z.string()), lines: Range }).strict()).min(1),
  seconds: z.number().int().positive(), lines: Range, reveals: z.array(z.string()), withholds: z.array(z.string()),
  leadsTo: z.string(), result: z.string().nullable(), current: z.array(Line),
}).strict();
export const GuideSchema = z.object({
  schema: z.enum([GUIDE_VERSIONS[2], GUIDE_VERSIONS[3], GUIDE_VERSIONS[4]]), game: z.literal('The Suborned'), gdd: z.string(),
  sources: z.array(z.object({ id: z.string(), file: z.string(), locator: z.string(), sha256: Sha, excerpt: z.string() }).strict()).min(1),
  rules: z.array(z.object({ id: RuleId, kind: z.string(), instruction: z.string(), sources: z.array(z.string()) }).strict()).min(3),
  voices: z.record(z.string(), z.string()), avoid: z.array(z.string()), profanity: z.array(z.string()),
  bubble: z.object({ seconds: z.number().positive(), maxWords: z.number().int().positive(), maxChars: z.number().int().positive() }).strict(),
  cutaway: z.object({ maxWords: z.number().int().positive(), maxChars: z.number().int().positive(), maxSentences: z.number().int().positive(), maxLines: z.number().int().positive() }).strict().optional(),
}).strict();
export const ResultSchema = z.object({
  schema: z.literal('suborned.style-run/2'), mode: z.enum(['live', 'test-fixture']),
  status: z.enum(['ACCEPTED', 'INCOMPLETE', 'ERROR']), stopReason: z.string(),
  startedAt: Time, finishedAt: Time.nullable(),
  evidence: z.object({
    sources: z.array(z.object({ file: z.string(), sha256: Sha }).strict()).min(1).max(20),
    tooling: z.array(z.object({ file: z.string(), sha256: Sha }).strict()).min(1).max(60), toolingSha256: Sha,
  }).strict().nullable(),
  authorization: z.object({ provider: z.literal('codex'), model: z.string().min(1), approvedBy: z.string().trim().min(1) }).strict().nullable(),
  brief: BriefSchema, steer: z.string().nullable(), guide: GuideSchema,
  limits: z.object({ maxCalls: z.number(), maxTokens: z.number(), maxRevisions: z.number(), maxMs: z.number() }).strict(),
  rounds: z.array(z.object({ conversation: Conversation, evaluation: Evaluation,
    hardIssues: z.array(z.object({ rule: RuleId, reason: z.string() }).strict()) }).strict()).max(4),
  calls: z.array(z.object({
    stage: z.enum(['generate', 'evaluate', 'refine']), role: Role, prompt: z.string(), startedAt: Time, finishedAt: Time.nullable(),
    timeoutMs: z.number().int().positive().max(180000), schemaSha256: Sha,
    output: z.unknown().optional(), raw: z.unknown().optional(), tokens: z.number().nullable(), error: z.string().optional(),
  }).strict()).max(20),
}).strict();
