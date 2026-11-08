import { isDeepStrictEqual } from 'node:util';
import { z } from 'zod';
import { GuideNatural } from './short-natural';
import { GuideAi } from './short-natural14';
import { AuthorizationSchema, type Authorization, type Call, type Evidence, type Limits, type Source } from './types';

/** Short lines (Sky 2026-11-07, asked whether the loop covers more than the Inez conversations: "sure", so nothing ships unscored): the barks, the why block and the debrief page, written by
 *  Fable and scored by Motive in the same loop as the conversations, a family per run. */
export const SHORT_KINDS = ['bark', 'why', 'debrief', 'label'] as const;
export type ShortKind = (typeof SHORT_KINDS)[number];
/** A kind's rubric: where its words show, and the six rules. `label` exists from guide /10 on, so a /1–/9 guide has only the first three. */
export type ShortKindRules = { use: string; rules: ShortRule[] };
/** The rubric's rules: the brief's five (plain, true, short, useful, named as known) and the voice of who says it. */
export const SHORT_RULES = ['USEFUL', 'TRUE', 'PLAIN', 'SHORT', 'NAMES', 'VOICE'] as const;
/** The /1–/11 evaluation's rules: kept as they were, so a saved run's recorded schema still hashes the same. */
export const ShortRuleId = z.enum(SHORT_RULES);
/** Guide /12's bark rules (brief 099 voices): WORTH and VOICE lead, natural speech, real variants and no mechanics talk follow, and each is scored on its own. */
export const BARK_RULES_12 = ['WORTH', 'VOICE', 'NATURAL', 'VARIANTS', 'MECHANICS', 'TRUE', 'NAMES', 'SHORT'] as const;
/** Every rule a guide of any version can hold or a code check can raise. */
export const AnyRuleId = z.enum([...SHORT_RULES, 'WORTH', 'NATURAL', 'VARIANTS', 'MECHANICS']);
export type ShortRuleId = z.infer<typeof AnyRuleId>;
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
/** /9 (Aster's review, 2026-11-07, after host logic ran brief 100's HUD tests on the /5–/7 why words): a police card shows its title over its short, so a police title names
 *  who or what is coming (Police coming, More police, Robots coming, Lockdown) and the cause goes in the short; the meter's head keeps its level; the words those tests read
 *  are held (HostLogic.Why100.cs). The debrief page: the 13 keys Latch's page reads that had no item (the four other folds, the withdrawal's head and beat, a killing's
 *  note, the list's joiner), Spent's help and pay name who ({who}), its both and none told true, and an item's today is what the page shows now, kept where it holds. */
/** /10 (brief 101, Sky 2026-11-08: the HUD sweep's mix, the top right in Street labels' style, the bottom right Corner tokens' news lozenge): a fourth kind, `label`,
 *  the words the new HUD draws in play at a glance (the first shot's row, the next key event, the lozenge's cards), held to Sky's rule on numbers in play; a word the
 *  file already has is briefed with today's words and kept where it holds. The other kinds read as at /9. */
/** /11 (host logic on the /10 words, 2026-11-08: the hurt-carry line lost the health left and the vale-out line the time the police close in, both read by brief 096's
 *  tests): a fill is how the game draws a number, so a count, the health left or a key event's time comes in through it; the lines a host test reads are held by pattern;
 *  the lozenge's line slot is 32 (Latch's lozenge clips a line at 300 px). */
/** /12 (brief 099 voices; Sky 2026-11-08, playing: "a lot of poor wording stil throughout. Especially agents talk to player"): the /4–/11 bark rubric rewarded plain,
 *  short and true and passed status codes at 10/10. A bark is now scored WORTH and VOICE first, then NATURAL, VARIANTS, MECHANICS, TRUE, NAMES and SHORT, each rule
 *  on its own (`scores`), and only a 10 on every rule is accepted. Fable's voice sheet and calibration (today's flat lines failing, a gold set passing) come from
 *  briefs/099/fable/voices/sheet.json, staged there until Sky's yes; only the rows the host plays under Fable's frequency (the sheet's `speech`) are written, the rest
 *  silent with their reason. The why block, the debrief page and the labels read as at /11, and the labels keep Sky's own words. No /12 bark run applies before Sky's yes. */
/** /13 (brief 102, Sky 2026-11-08: "Maybe even drop them? They will feel repetitive"; on "He bought it. He'll be back, and he knows my face.": "that's exactly the kind of
 *  scripting that bugs me. 'I will remember you.' lines etc - so unnatural"): the agents go quiet in play, so their acknowledgements, arrivals, landings and reads leave the
 *  loop, and a bark is the world's people or the cell in a scene with a person (text.schema.ts SCENE_ROWS); every other agent row is `silent` in text.json. NATURAL and TRUE
 *  hold the no-mechanics rule (no line tells the player a rule or a consequence, whoever says it), with brief 102's failing examples, Sky's two kept lines and a calibration
 *  from briefs/102/fable/natural.json; a reply's facts are split from what the person says; a code check holds the family Sky named (remember, a face, a search, notice).
 *  The why block, the debrief page and the labels read as at /12. No /13 bark run applies before Sky's yes. */
/** /14 (brief 102's sweep; Sky 2026-11-08 said yes to the /13 sample, "yes - rewrites are much better", and on the four lines it kept on purpose: "not into those e.g.
 *  'Citizen. Walking is encouraged. Running is noted.' - that noted is AI sounding"): NATURAL fails AI-sounding words whoever says them (noted, understood, proceeding,
 *  acknowledged, affirmative, rest assured and their kind; briefs/102/fable/ai-words.json), with a code check; Sky's four go into the failing lines and the /13 pass Sky
 *  turned down leaves the bar; the Mislead is a scene with a person (`bark/mislead`: the Scout's tip and the rival's answer); rows marked `sky` (Sky's approved rewrites)
 *  are not items, and the family's context gives them; one new label family, the news card for the rivals following our car out. The rest reads as at /13. */
/** /15 (brief 102's sweep): /14, and the evaluator gives a finding for every rule it scores below 10 (Opus scored a family 9 throughout with no finding, so the writer had
 *  nothing to revise). The rubric and the families read as at /14, so a /15 run may continue a /14 run of the same family (`sameRubric`). */
/** /16 (brief 102's sweep): /15, and a cut item's field with no lines counts as no lines (the writer cut alarm/watcher as `fields: [{ lines: [] }]` four rounds running and
 *  the check "a cut item has no lines" blocked a 10 every time). A /16 run may continue a /14 or /15 run of the same family. */
/** /17 (brief 102's sweep, Fable 2026-11-08: bark/witnesses-crowd and bark/summons didn't reach 10 in six passes each, so Fable wrote their 13 failing lines by hand): rows
 *  marked `hand` in text.json (Fable's hand edits, briefs/102/fable/hand-barks.json) are not items, as Sky's rows; the family's context gives them and `apply` never replaces
 *  them; and the summons' no-visit rows are told as the engine plays them (only a call on the turned guard gets that code: Inez, his tie, has no post to leave, so his answer
 *  goes to the Fixer, and the host shows Inez's line after his). A run may also score a hand-written draft once (`hand`: no writer call). The rubric reads as at /16. */
/** /18 (brief 102, Fable 2026-11-08: the judge's other 21 findings on the two hand-written families were on rows already marked `hand`, which /17 leaves out of the items):
 *  a hand run scores its family as it plays, so the family's hand rows are items again in a hand run (a writer run keeps them out, as at /17); and the rows written this
 *  round are told as the engine plays them (short-hand18.ts): the guard's radio report is about one of the cell in his sight, and the summons' busy, too-far, alert and
 *  saw-violence refusals say what the engine checks. The rubric reads as at /17. */
/** /19 (brief 101, Latch's r4, 2026-11-09: the news lozenge drew the sweep's loiter why at 430 px in its 296 px box, "the square's mast camera saw the Fixer stay too
 *  long beside the café · 2:46", with an ellipsis): SHORT for every why the lozenge shows is the box. The lozenge reads "{short} {where} · m:ss" on one line of Jost
 *  Medium at 13 px, measured as the HUD draws it (short-lozenge.ts) at the widest each fill takes in the glossary; a place has 105 px of it. The four cause families write
 *  only the short (the title and the line, on hover and in the Journal, stay as they are and are the item's facts); the places are a family of their own, `why/places`;
 *  AI-sounding words fail a why as they fail a bark. The rest reads as at /18. */
/** /20 (brief 101, the /19 places run): /19 with a place's room at 108.8 px, the width of "at the service drive". The HUD's own stand names, "the square's bay" and "the
 *  service drive", don't fit 105 px with any preposition, and the judge rightly wouldn't rename them (one glossary), so a place keeps the HUD's name and a short with a
 *  place has 142.9 px. The rest reads as at /19, and /19's runs rebuild with /19's room. */
/** /21 (brief 101, the /20 places run: 9 for four rounds on "by the cabinet", since the hover says "the control cabinet" and the facts never said the HUD has a short name):
 *  the cabinet's facts say the HUD gives it two names, the hover's and the act's ("at the cabinet", HostActivity.ActPlaceName). Only `why/places` changes; the rubric reads
 *  as at /20, so a /20 run continues under /21. */
/** /22 (brief 101, Fable 2026-11-09: the five shorts the lozenge round left weakest, rewritten by hand to brief 102's bar and scored once by the judge): a why cause
 *  marked `hand` in text.json (why.schema.ts WHY_HAND; briefs/101/fable/hand-whys.json) is not an item of a writer run, as a hand row of the dialogue isn't at /17: its
 *  family's context gives its shorts, and `apply` never replaces it; a hand run scores the family as it plays, hand causes and all, as at /18. The rubric reads as at /21. */
export const SHORT_GUIDES = { 1: 'suborned.style-short-guide/1', 2: 'suborned.style-short-guide/2', 3: 'suborned.style-short-guide/3', 4: 'suborned.style-short-guide/4', 5: 'suborned.style-short-guide/5', 6: 'suborned.style-short-guide/6', 7: 'suborned.style-short-guide/7', 8: 'suborned.style-short-guide/8', 9: 'suborned.style-short-guide/9', 10: 'suborned.style-short-guide/10', 11: 'suborned.style-short-guide/11', 12: 'suborned.style-short-guide/12', 13: 'suborned.style-short-guide/13', 14: 'suborned.style-short-guide/14', 15: 'suborned.style-short-guide/15', 16: 'suborned.style-short-guide/16', 17: 'suborned.style-short-guide/17', 18: 'suborned.style-short-guide/18', 19: 'suborned.style-short-guide/19', 20: 'suborned.style-short-guide/20', 21: 'suborned.style-short-guide/21', 22: 'suborned.style-short-guide/22' } as const;
export type ShortGuideVersion = keyof typeof SHORT_GUIDES;
/** The version new runs use; a saved run replays with its own. */
export const SHORT_NOW: ShortGuideVersion = 22;
/** Two guides with the same rubric, sources and families, the later on a newer protocol (from /15 a run may continue a /14 run's last round). */
export const sameRubric = (earlier: ShortGuide, later: ShortGuide) => shortGuideVersion(earlier) <= shortGuideVersion(later) && shortGuideVersion(earlier) >= 14
  && isDeepStrictEqual(JSON.parse(JSON.stringify({ ...earlier, schema: later.schema })), JSON.parse(JSON.stringify(later)));
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
export type Field = { id: string; form: Form; words: number; chars: number; count: number; fills: Fill[]; keep: string[]; patterns: Pattern[];
  /** Guide /19 (brief 101): the px the line may take on the news lozenge at its widest fills, the place and the clock already taken off (short-lozenge.ts). */
  lozenge?: number };
/** One key of text.json to write. `tells` is what the line must tell the player and what the engine did: the evaluator's truth.
 *  `stays` says why the item may not be cut (the host or a check needs it); null = a bark the loop may cut, with its reason. */
export type ShortItem = { key: string; where: string; tells: string; speaker: string | null; fields: Field[]; current: Record<string, string[]>; stays: string | null };
export type ShortBrief = { id: string; kind: ShortKind; title: string; context: string; items: ShortItem[] };
export type ShortRule = { id: ShortRuleId; kind: string; instruction: string; sources: string[] };
export type ShortGuide = {
  schema: (typeof SHORT_GUIDES)[ShortGuideVersion]; game: 'The Suborned'; gdd: string; sources: Source[];
  kinds: Record<Exclude<ShortKind, 'label'>, ShortKindRules> & { label?: ShortKindRules };
  voices: Record<string, string>; avoid: string[]; profanity: string[]; named: string[]; cell: string[];
  /** /12: the cell's voice sheet and the calibration the rubric is held to. */
  sheet?: GuideSheet;
  /** /13: Sky's kept lines and the no-mechanics calibration (briefs/102/fable/natural.json). */
  natural?: GuideNatural;
  /** /14: the AI-sounding words, Sky's four rejected lines and today's lines with such a word (briefs/102/fable/ai-words.json). */
  ai?: GuideAi;
};

/** Fable's voice sheet (guide /12's source): each agent's voice, the calibration lines, and the speech tiers the families are cut by. */
const Voice = z.object({ short: z.array(z.string().min(1)).length(3), traits: z.string().min(1), habits: z.string().min(1), commander: z.string().min(1), cares: z.string().min(1),
  never: z.array(z.string().min(1)).min(2) }).strict();
const Calibrated = z.object({ key: z.string().min(1), line: z.string().min(1), why: z.string().min(1) }).strict();
const Keys = z.array(z.string().regex(/^[a-z-]+(\/[a-z-]+)?$/));
export const VoiceSheet = z.object({
  schema: z.literal('suborned.voice-sheet/1'), status: z.string().min(1), why: z.string().min(1),
  voices: z.object({ scout: Voice, fixer: Voice, tech: Voice, enforcer: Voice }).strict(),
  fail: z.array(Calibrated).min(8), pass: z.array(Calibrated).min(8),
  speech: z.object({ rule: z.string().min(1), keys: z.string().min(1), ack: z.object({ always: Keys, first: Keys, never: Keys }).strict(), arrive: Keys, done: Keys,
    events: z.object({ agents: z.number().int().min(1), end: z.number().int().min(1) }).strict() }).strict(),
}).strict();
export type VoiceSheet = z.infer<typeof VoiceSheet>;
/** What a /12 guide carries of the sheet: the voices and the calibration (the speech tiers shape the families, not the rubric). */
export const GuideSheet = VoiceSheet.pick({ voices: true, fail: true, pass: true });
export type GuideSheet = z.infer<typeof GuideSheet>;

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
const Score = z.number().int().min(1).max(10);
/** Guide /12's evaluation of a bark family: every rule scored on its own, so a true, short, plain family that says nothing or sounds like anyone can't pass on the rest. */
export const ShortEvaluation12 = z.object({
  scores: z.object({ WORTH: Score, VOICE: Score, NATURAL: Score, VARIANTS: Score, MECHANICS: Score, TRUE: Score, NAMES: Score, SHORT: Score }).strict()
    .describe('Each rule of the rubric scored on its own, 1–10.'),
  score: Score.describe('The lowest of the rule scores.'),
  reason: z.string().min(1).max(3000),
  findings: z.array(z.object({
    rule: z.enum(BARK_RULES_12),
    key: z.string().max(80).describe('The item key the finding is about; empty only for something about the whole family.'),
    quote: z.string().max(400).describe('An exact excerpt of one line; empty only for something missing or for a cut.'),
    reason: z.string().min(1).max(1200),
    fix: z.string().min(1).max(1200),
  }).strict()).max(24),
}).strict();
export type ShortEvaluation12 = z.infer<typeof ShortEvaluation12>;
export type AnyEvaluation = ShortEvaluation | ShortEvaluation12;
/** The evaluation a family is scored in: /12's for a bark family from guide /12, else the /1–/11 one (its recorded schema hash unchanged). */
export const evaluationFor = (g: Pick<ShortGuide, 'schema'>, kind: ShortKind) => shortGuideVersion(g) >= 12 && kind === 'bark' ? ShortEvaluation12 : ShortEvaluation;
export type ShortIssue = { rule: ShortRuleId; reason: string };
export type ShortRound = { set: ShortSet; evaluation: AnyEvaluation; hardIssues: ShortIssue[] };
/** A run that continues an earlier run's last round (brief 102: Opus judging, three revisions often end on an 8 or a 9): the earlier run by its folder, and that round. */
export type ShortContinue = ShortRound & { run: string };
/** A draft written by hand (brief 102, guide /17): who wrote it and why, and the set itself, which the run scores as its first round with no writer call. */
export type ShortHand = { by: string; why: string; set: ShortSet };
export type ShortResult = {
  schema: typeof SHORT_RUN; mode: 'live' | 'test-fixture';
  status: 'ACCEPTED' | 'INCOMPLETE' | 'ERROR'; stopReason: string;
  startedAt: number; finishedAt: number | null;
  evidence: Evidence | null; authorization: Authorization | null;
  brief: ShortBrief; guide: ShortGuide; limits: Limits; rounds: ShortRound[]; calls: Call[];
  continues?: ShortContinue;
  hand?: ShortHand;
};

const Sha = z.string().regex(/^[a-f0-9]{64}$/);
const Time = z.number().int().nonnegative();
const FillSchema = z.object({ id: z.string(), means: z.string(), widest: z.string() }).strict();
export const ShortBriefSchema = z.object({
  id: z.string().regex(/^(bark|why|debrief|label)\/[a-z0-9-]+$/), kind: z.enum(SHORT_KINDS), title: z.string().min(1), context: z.string().min(1),
  items: z.array(z.object({
    key: z.string().min(1), where: z.string().min(1), tells: z.string().min(1), speaker: z.string().nullable(),
    fields: z.array(z.object({ id: z.string().min(1), form: z.enum(FORMS), words: z.number().int().positive(), chars: z.number().int().positive(),
      count: z.number().int().min(1).max(4), fills: z.array(FillSchema), keep: z.array(z.string()),
      patterns: z.array(z.object({ re: z.string(), flags: z.string(), must: z.boolean(), why: z.string() }).strict()), lozenge: z.number().positive().optional() }).strict()).min(1),
    current: z.record(z.string(), z.array(z.string())), stays: z.string().nullable(),
  }).strict()).min(1).max(60),
}).strict();
const RuleSchema = z.object({ id: AnyRuleId, kind: z.string(), instruction: z.string(), sources: z.array(z.string()) }).strict();
const KindSchema = z.object({ use: z.string(), rules: z.array(RuleSchema).min(5) }).strict();
export const ShortGuideSchema = z.object({
  schema: z.enum(Object.values(SHORT_GUIDES) as [string, ...string[]]), game: z.literal('The Suborned'), gdd: z.string(),
  sources: z.array(z.object({ id: z.string(), file: z.string(), locator: z.string(), sha256: Sha, excerpt: z.string() }).strict()).min(1),
  kinds: z.object({ bark: KindSchema, why: KindSchema, debrief: KindSchema, label: KindSchema.optional() }).strict(),
  voices: z.record(z.string(), z.string()), avoid: z.array(z.string()), profanity: z.array(z.string()), named: z.array(z.string()), cell: z.array(z.string()),
  sheet: GuideSheet.optional(), natural: GuideNatural.optional(), ai: GuideAi.optional(),
}).strict();
export const ShortResultSchema = z.object({
  schema: z.literal(SHORT_RUN), mode: z.enum(['live', 'test-fixture']),
  status: z.enum(['ACCEPTED', 'INCOMPLETE', 'ERROR']), stopReason: z.string(),
  startedAt: Time, finishedAt: Time.nullable(),
  evidence: z.object({
    sources: z.array(z.object({ file: z.string(), sha256: Sha }).strict()).min(1).max(30),
    tooling: z.array(z.object({ file: z.string(), sha256: Sha }).strict()).min(1).max(80), toolingSha256: Sha,
  }).strict().nullable(),
  authorization: AuthorizationSchema.nullable(),
  brief: ShortBriefSchema, guide: ShortGuideSchema,
  limits: z.object({ maxCalls: z.number(), maxTokens: z.number(), maxRevisions: z.number(), maxMs: z.number() }).strict(),
  rounds: z.array(z.object({ set: ShortSet, evaluation: z.union([ShortEvaluation, ShortEvaluation12]),
    hardIssues: z.array(z.object({ rule: AnyRuleId, reason: z.string() }).strict()) }).strict()).max(4),
  continues: z.object({ run: z.string().regex(/^[0-9T:.Z-]+-(bark|why|debrief|label)-[a-z0-9-]+$/), set: ShortSet, evaluation: z.union([ShortEvaluation, ShortEvaluation12]),
    hardIssues: z.array(z.object({ rule: AnyRuleId, reason: z.string() }).strict()) }).strict().optional(),
  hand: z.object({ by: z.string().trim().min(1), why: z.string().trim().min(1), set: ShortSet }).strict().optional(),
  calls: z.array(z.object({
    stage: z.enum(['generate', 'evaluate', 'refine']), role: z.enum(['dialogue-designer', 'systems-designer']), prompt: z.string(),
    startedAt: Time, finishedAt: Time.nullable(), timeoutMs: z.number().int().positive().max(180000), schemaSha256: Sha,
    output: z.unknown().optional(), raw: z.unknown().optional(), tokens: z.number().nullable(), error: z.string().optional(),
  }).strict()).max(20),
}).strict();
