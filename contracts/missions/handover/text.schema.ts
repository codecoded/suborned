// suborned.text/1: the words of one mission, contracts/missions/<id>/text.json (Fable, brief 011). Build-time content the Unity
// host reads; nothing is generated at runtime (GDD §01 no AI at runtime, §14 Fable: built at build time, never live).
//
// What is in the file:
//   briefing   the brief card, objective, win, each fail in plain words, clock and close, purse, two hints, the objective tracker,
//              the timeline as the status pill words it, one line per agent
//   hints · toasts · goingLoud   the training hints H1–H11 (Motive's triggers, copied), the first-run toasts, the going-loud card's words
//   targets    each thing the cell can right click, in tiers of knowledge: `about` is what the cell was TOLD at the start (always shown);
//              `watched` what a Scout Watch teaches (a person's routine, a camera's or the patrol's sight), `routine` the patrol's rounds,
//              `heard` what a Scout Listen teaches (need, temperament); a tier shows only once the cell has learned it (GDD §04)
//   actions    every verb the action panel can show: label, who gives it, one plain line with its trade-off, and a line for the
//              same verb on a particular target
//   cancelCosts · activity   what cancelling an order costs or keeps; what each agent is doing now, in words for the roster
//   locks      one line for every refusal code the engine gives (Game.Issue returns Answer { ok, code, why, m, cm, usd, tick });
//              the line says what unlocks it and carries {who} {op} {m} {usd} {tick}; `fillLock` is the reference for filling them
//   cards      the feed cards for engine events no scene covers (title, result in the player's words, variants by engine state)
//   debrief    the end card's words: headline, tally, security, investigation, causes, the route and the assets, by the debrief's ids
//   events     the situations that can speak: what triggers each (an order, the engine's log code, the clock) and who may speak
//   exchange   the order in which an order's events play, for the collapsible dialogue
//   scenes     short conversations for the update feed: overheard (Watch, Listen), approach (Ask, Pay, Help, Pressure), the call, the city;
//              beats are written lines or refs into `dialogue`, with a result line saying what the player now knows or has
//   dialogue   the lines: event × verb × speaker × outcome × temperament, 2–4 variants each; `lookup` and `pickLine` are the
//              reference for how the host finds and picks one (deterministic: same seed and same run, same words)
//
// Rules the checker (text.test.ts) holds: every line ≤ 80 characters; a spoken line 1–12 words; no one named but the cast; no
// modern speech; no strong profanity; every engine verb, refusal and fail has text; every reply the rulebook can produce has a line
// for each Handover role and each temperament the role allows; every line naming Halcyon is marked pending.
import { z } from 'zod';
import { TEMPERAMENTS } from '../../people/people.rules.schema.ts';
import { fnv1a32 } from '../../rules/rulebook.ref.ts';

export const AGENTS = ['scout', 'fixer', 'tech', 'enforcer'] as const;
export const PEOPLE = ['waitress', 'driver', 'guard', 'councillor'] as const;
export const OTHERS = ['rivals', 'patrol', 'security', 'watcher', 'crowd'] as const;
/** Things a Scout can Watch besides a person (rulebook sight.kinds learn: watch): `targets` keys. The guard is watched as a person. */
export const WATCHED = ['camera-entrance', 'camera-pickup', 'patrol'] as const;
export const SPEAKERS = [...AGENTS, ...PEOPLE, ...OTHERS] as const;
/** The action panel's verbs: one id each (the host's Slots ids; `ask`…`pressure` are the Fixer's `turn` with an approach). */
export const VERBS = ['watch', 'listen', 'mislead', 'ask', 'pay', 'help', 'pressure', 'call', 'loop', 'reroute', 'enter', 'take',
  'extract', 'fire', 'draw', 'holster', 'cease', 'stance', 'panic', 'carry', 'takedown', 'drop', 'stash', 'restrain', 'move', 'run', 'cancel', 'withdraw'] as const;
/** `turn` stands for any of the four approaches (the Fixer's reaction to how it went). */
export const DIALOGUE_VERBS = [...VERBS, 'turn'] as const;
/** A target's reply: the rulebook's turn outcomes, and `suspicious` when the person reports the approach (rulebook `reports`). */
export const OUTCOMES = ['turned', 'refused', 'not-yet', 'kept', 'suspicious'] as const;
/** The engine's fail codes (Game.Fail), `withdraw` (an order) and `engine-error`. */
export const FAILS = ['abduction-seen-camera', 'driver-shot', 'enforcer-down', 'entry-seen-camera', 'entry-seen-guard', 'entry-seen-patrol',
  'rivals-reach-vale', 'sealed', 'vale-dead', 'vale-leaves', 'engine-error'] as const;
export const NAMED = ['Inez', 'Vale'] as const; // the only people the words may name (people.rules names.pinned); others by role
/** Speakers only a scene has: the people around the target (the café's owner, a customer, an Office clerk). */
export const SCENE_ONLY = ['keeper', 'clerk', 'customer'] as const;
export const SCENE_SPEAKERS = [...SPEAKERS, ...SCENE_ONLY] as const;
export const MAX_LINE = 80;
export const MAX_WORDS = 12;

const clock = z.string().regex(/^\d{1,2}:[0-5]\d$/);
const code = z.string().regex(/^[a-z][a-z0-9-]*$/);
/** One line of text: ≤ 80 characters, one line, no markup. `{who} {op} {m} {usd} {target}` are filled by the host (the checker tests the widest fill). */
const Line = z.string().min(1).max(MAX_LINE).regex(/^[^\n<>]+$/);
const Speaker = z.enum(SPEAKERS);
const Verb = z.enum(VERBS);
const DVerb = z.enum(DIALOGUE_VERBS);
const Temperament = z.enum(TEMPERAMENTS);

export const Action = z.strictObject({
  label: z.string().min(1).max(24).describe('the row label in the panel and on the verb plate'),
  by: z.array(z.enum(AGENTS)).min(1).describe('the agents who can give it'),
  engine: z.strictObject({ do: code, approach: code.optional() }).describe('the engine order it becomes (Order.Do, Order.Approach)'),
  does: Line.describe('one plain line: what it does and what it costs'),
  withTarget: Line.optional().describe('the row or card label once a target is chosen, e.g. "Listen to {target}"; {target} is names.targets[key]. Absent = the verb takes no target'),
  then: Line.optional().describe('the queued clause after a walk, e.g. "listens to {target}": "{agent} walks to {target}, then {then}"'),
  onTarget: z.record(z.string(), Line).optional().describe('the same verb on one target (a target key from `targets`), when the plain line can say more'),
  ways: z.record(code, z.strictObject({ label: z.string().min(1).max(24), does: Line })).optional()
    .describe('brief 013: an order with a choice (Order.How: takedown knockout or kill): the panel row for each way and its price in words'),
});

export const Lock = z.strictObject({
  line: Line.describe('what is wrong and what unlocks it; {who} {op} come from the order, {m} {usd} {tick} from the engine\'s Answer'),
  verbs: z.array(z.union([Verb, z.literal('*')])).min(1).describe('the verbs that can return it'),
});

/** What an agent is doing now (sim Game.ActivityOf): the roster and verb plate words. `{who}` is the activity's target. */
export const Activity = z.strictObject({
  idle: Line, down: Line, walking: Line, running: Line, drawing: Line, holstering: Line, firing: Line,
  holding: z.strictObject({ loop: Line, carry: Line, take: Line }),
  acting: z.strictObject({ watch: Line, listen: Line, ask: Line, pay: Line, help: Line, pressure: Line, loop: Line, reroute: Line, mislead: Line, enter: Line, take: Line, carry: Line,
    takedown: Line, drop: Line, stash: Line, restrain: Line, body: Line.describe('any other work with a body (the rulebook cancel verb `body`: lift, drop, stash or tie)') }),
});

/** A training hint line fits Glyph's 110 characters (briefs/011/motive-fights.md §8). */
export const HINT_MAX = 110;
const HintLine = z.string().min(1).max(HINT_MAX).regex(/^[^\n<>]+$/);
export const Hint = z.strictObject({
  priority: z.number().int().min(1),
  agent: z.enum([...AGENTS, 'any']).describe('the agent the line names; `any` = "another agent" (H11: any agent can carry)'),
  verb: DVerb,
  target: z.string().min(1).describe('what the hint rings, in words'),
  line: HintLine,
  trigger: z.string().min(1).describe('copied from Motive: the game state that shows it'),
  rings: z.string().min(1),
  goneWhen: z.string().min(1),
});

const SceneSpeaker = z.enum(SCENE_SPEAKERS);
const Beat = z.union([
  z.strictObject({ speaker: SceneSpeaker, line: Line }),
  z.strictObject({ speaker: SceneSpeaker, ref: z.enum(['ack', 'arrive', 'reply', 'done', 'confirm']).describe('a line picked from `dialogue` for this scene\'s verb, outcome and temperament') }),
]);
export const SceneVariant = z.strictObject({ beats: z.array(Beat).min(3).max(6) });
export const Scene = z.strictObject({
  kind: z.enum(['overheard', 'approach', 'call', 'city']),
  title: z.string().min(1).max(40).optional().describe('the card\'s title; required on city scenes (the event as the player would name it)'),
  verb: Verb.optional(), about: z.enum(PEOPLE).optional(), outcome: z.string().optional(), temperament: Temperament.optional(),
  event: code.optional().describe('city: the world event it plays with'),
  sight: z.enum(['always', 'in-sight']).optional().describe('city: shown always, or only when an agent is within sight'),
  learns: z.array(z.enum(['routine', 'need', 'temperament', 'secret'])).optional(),
  result: Line.describe('one plain line of what the player now knows or has; placeholders {who} {need} {temperament} {usd} {alert}'),
  variants: z.array(SceneVariant).min(2).max(4),
});
export type Scene = z.infer<typeof Scene>;

/** `watched` and `routine` are shown after the card's "Learned: " (so ≤ 70). `heard` carries {need} and {temperament}. */
const Learned = Line.max(70);
export const Target = z.strictObject({
  name: z.string().min(1).max(32),
  about: Line.describe('what the cell was told at the start (the briefing, rumour): shown always; never a fact only a Watch or Listen teaches'),
  watched: Learned.optional().describe('after a completed Scout Watch: a person\'s routine; a camera\'s or the patrol\'s sight'),
  routine: Learned.optional().describe('the patrol only: its rounds, once the Watches total sight.watch.routine seconds'),
  heard: Line.optional().describe('after a completed Scout Listen: what the person needs and who they are ({need} {temperament})'),
});

/** A feed card for an engine log code no scene covers (Game.Note). The result is the player's words with the engine's numbers, never its sentence.
 *  Placeholders: {who} the watcher or person as names.targets says it ("the guard"), {agent} "the Scout", {alert}, {n}, {clock}. The host
 *  capitalises the first letter after filling. A result that is a record is picked by the engine state `pick` names. */
export const CARD_KINDS = ['city', 'warning', 'fight'] as const;
/** Where an in-sight card looks (brief 013): the engine position the sight check uses. The log carries no position; the host reads it from the game. */
export const CARD_AT = ['victim', 'stash', 'reporter', 'scene', 'person', 'guard', 'unit', 'door', 'patrol'] as const;
export const Card = z.strictObject({
  kind: z.enum(CARD_KINDS),
  title: z.string().min(1).max(40),
  sight: z.enum(['in-sight']).optional().describe('the cell hears of it only if an agent is within 13 m with a clear line (as a city scene)'),
  at: z.enum(CARD_AT).optional().describe('with `sight`: the place the check looks at: victim (the downed person), stash (the bin or door), reporter (who is making the report), scene (where the report sent the response), person (who is acting), guard, unit (the response member), door (the Office door), patrol'),
  needs: z.enum(['routine']).optional().describe('the card shows only once the cell holds the routine of the person it is about (a completed Watch); until then the city\'s answer (report.landed) speaks alone'),
  result: z.union([Line, z.record(code, Line)]).optional(),
  pick: z.string().optional().describe('when `result` is a record: the engine state that chooses the key'),
});

/** The end card's words by the debrief's ids (sim Debrief.cs). Placeholders: {clock} {usd} {trail} {n} {hits} {who} {cause} {list}. */
export const Debrief = z.strictObject({
  rule: z.string().min(1),
  headline: z.strictObject({ win: Line, withdraw: Line, fail: Line }).describe('a failure with a text in briefing.fails uses that line\'s first sentence instead'),
  result: z.strictObject({ win: Line }),
  tally: Line,
  security: z.strictObject({ calm: Line, raised: Line, high: Line }),
  investigation: z.strictObject({ little: Line, early: Line, active: Line, activeBecause: Line }),
  causes: z.record(code, Line).describe('Debrief.InvestigationCause (and the engine\'s named-witness:<role>, whose {who} is the role as names.targets says it)'),
  fight: z.strictObject({ loud: Line, safe: Line }),
  kept: Line,
  route: z.record(code, Line),
  assets: z.record(code, Line),
  between: z.record(code, Line).describe('brief 013: the between-missions lines a takedown leaves (rulebook debrief.lines: volunteer, statement, missing, grief, worry, area-watch, case), keyed by the rulebook id; each line carries every field the rulebook lists for it'),
});

export const EventDef = z.strictObject({
  kind: z.enum(['order', 'reply', 'read', 'world', 'end']),
  trigger: z.string().min(1).describe('what makes it speak'),
  log: z.array(z.string().min(1)).optional().describe('the engine log codes that trigger it (Game.Note)'),
  clock: clock.optional().describe('or the mission clock'),
  speakers: z.array(Speaker).min(1),
  verbs: z.array(DVerb).optional(),
  note: z.string().optional(),
});

export const Entry = z.strictObject({
  event: code,
  verb: DVerb.optional(),
  speaker: Speaker,
  outcome: z.string().optional().describe('reply: turned · refused · not-yet · kept · suspicious. fail: the fail code. cliffhanger: named · unnamed'),
  temperament: Temperament.optional(),
  about: z.enum([...PEOPLE, ...WATCHED]).optional().describe('read: whom or what the Scout is reading (a person, a camera or the patrol)'),
  pending: z.string().optional().describe('the line waits on a decision; the host ships the `unnamed` alternative until it is made'),
  lines: z.array(z.string().min(1).max(MAX_LINE).regex(/^[^\n<>]+$/)).min(2).max(4),
});
export type Entry = z.infer<typeof Entry>;

export const TextFile = z.strictObject({
  schema: z.literal('suborned.text/1'),
  mission: code,
  author: z.string().min(1),
  gdd: z.string().min(1),
  rule: z.string().min(1),
  tone: z.strictObject({
    world: z.string().min(1),
    voices: z.record(z.string(), z.string()).describe('speaker → how they talk'),
    avoid: z.array(z.string()).describe('words the checker rejects: modern speech, internet words, strong profanity, period-slang overload'),
    profanity: z.array(z.string()).describe('strong profanity; mild (damn) is allowed'),
  }),
  names: z.strictObject({
    named: z.array(z.string()).describe('the only people the words may name'),
    speakers: z.record(z.string(), z.string()).describe('the speaker label the dialogue shows'),
    targets: z.record(z.string(), z.string()).describe('how the cell refers to each target in a sentence'),
  }),
  pick: z.strictObject({
    rule: z.string().min(1),
    vectors: z.array(z.strictObject({ seed: z.string(), key: z.string(), n: z.number().int().min(0), count: z.number().int().min(1), index: z.number().int().min(0) })),
  }),
  briefing: z.strictObject({
    title: z.string().min(1).max(32),
    card: z.array(Line).min(3).max(5),
    objective: Line,
    win: Line,
    fails: z.record(z.string(), Line),
    clock: z.strictObject({ close: clock, line: Line }),
    purse: z.strictObject({ usd: z.number().int().positive(), line: Line }),
    hints: z.strictObject({ 'front-door': Line, pickup: Line }),
    tracker: Line,
    timeline: z.array(z.strictObject({ at: clock, label: Line, known: z.boolean().describe('false = the pill shows "?" until it happens') })),
    cell: z.record(z.enum(AGENTS), Line),
  }),
  targets: z.record(z.string(), Target),
  hints: z.strictObject({ rules: z.record(z.string(), z.string()), items: z.record(z.string().regex(/^H\d+$/), Hint) }),
  toasts: z.record(code, z.strictObject({ trigger: z.string().min(1), line: HintLine })),
  goingLoud: z.strictObject({ captured: Line }).describe('lines on the going-loud card beyond its numbers'),
  actions: z.record(Verb, Action),
  cancelCosts: z.record(code, Line).describe('what a cancel order costs or keeps, by the rulebook `cancel` line id (and the engine\'s: quiet, keeps-vale, nothing, down, none)'),
  activity: Activity,
  locks: z.record(code, Lock).describe('keyed by the engine\'s refusal code (Answer.Codes, `suborned-sim codes`)'),
  cards: z.strictObject({ rule: z.string().min(1), items: z.record(z.string().regex(/^[a-z][a-z0-9.-]*$/), Card) }),
  debrief: Debrief,
  events: z.record(code, EventDef),
  exchange: z.record(Verb, z.array(code)).describe('verb → the events an order plays through, in order; the collapsed view shows the last'),
  silentStates: z.record(z.string(), z.string()).describe('hostile AI states (Gun.St) that speak no line of `ai-state`, with the reason'),
  silentLogs: z.record(z.string(), z.string()).describe('engine log codes (or prefixes ending in .) that speak no line, with the reason'),
  dialogue: z.array(Entry).min(1),
  scenes: z.strictObject({ rules: z.record(z.string(), z.union([z.string(), z.record(z.string(), z.string())])), items: z.array(Scene).min(1) }),
});
export type TextFile = z.infer<typeof TextFile>;

/** The values a card or debrief line may carry; `fillCard` is the reference for filling them. */
export type CardVals = { who?: string; agent?: string; alert?: number; n?: number; clock?: string; usd?: number; trail?: number; hits?: number; cause?: string; list?: string;
  /** brief 013's between-missions fields (the rulebook's debrief.lines): the words the host fills from the debrief */
  fact?: string; facts?: string; place?: string; of?: string; move?: string; fear?: string; guards?: number; cameras?: number; missions?: number; kind?: string; closes?: string };
export function fillCard(line: string, v: CardVals): string {
  const out = line.replace(/\{who\}/g, v.who ?? 'someone').replace(/\{agent\}/g, v.agent ?? 'an agent').replace(/\{alert\}/g, String(v.alert ?? '?'))
    .replace(/\{n\}/g, String(v.n ?? '?')).replace(/\{clock\}/g, v.clock ?? '?').replace(/\{usd\}/g, v.usd === undefined ? '?' : '$' + v.usd.toLocaleString('en-US'))
    .replace(/\{trail\}/g, String(v.trail ?? '?')).replace(/\{hits\}/g, String(v.hits ?? '?')).replace(/\{cause\}/g, v.cause ?? '?').replace(/\{list\}/g, v.list ?? '')
    .replace(/\{fact\}/g, v.fact ?? '?').replace(/\{facts\}/g, v.facts ?? '?').replace(/\{place\}/g, v.place ?? 'somewhere').replace(/\{of\}/g, v.of ?? 'someone')
    .replace(/\{move\}/g, v.move ?? '?').replace(/\{fear\}/g, v.fear ?? '?').replace(/\{guards\}/g, String(v.guards ?? '?')).replace(/\{cameras\}/g, String(v.cameras ?? '?'))
    .replace(/\{missions\}/g, String(v.missions ?? '?')).replace(/\{kind\}/g, v.kind ?? '?').replace(/\{closes\}/g, v.closes ?? '?');
  return out.charAt(0).toUpperCase() + out.slice(1);
}
/** The result line of a card: a record result is chosen by `variant`. */
export function cardResult(t: Pick<TextFile, 'cards'>, code: string, variant?: string): string | undefined {
  const r = t.cards.items[code]?.result;
  return typeof r === 'string' || r === undefined ? r : variant === undefined ? undefined : r[variant];
}

// ---------------------------------------------------------------- the reference lookups (Latch ports these; the checker runs them)

/** What the engine sends with a refusal (sim Answer): -1 = none. */
export type Answer = { code: string; m?: number; cm?: number; usd?: number; tick?: number };
/** The refusal line for an answer. `who` is the target's name ("Inez", "the driver"), `op` the agent ("Scout"), `clock` turns an engine tick into m:ss.
 *  {m} is Answer.m, or Answer.cm / 100 when the engine sends cm; {usd} is "$" and the thousands; {tick} is the mission clock. Unknown code = undefined. */
export function fillLock(t: Pick<TextFile, 'locks'>, a: Answer, ctx: { who?: string; op?: string; clock?: (tick: number) => string } = {}): string | undefined {
  const l = t.locks[a.code];
  if (!l) return undefined;
  const m = a.m !== undefined && a.m >= 0 ? a.m : a.cm !== undefined && a.cm >= 0 ? Math.round(a.cm / 100) : undefined;
  return l.line
    .replace(/\{who\}/g, ctx.who ?? 'them').replace(/\{op\}/g, ctx.op ?? 'The agent')
    .replace(/\{m\}/g, m === undefined ? '?' : String(m))
    .replace(/\{usd\}/g, a.usd !== undefined && a.usd >= 0 ? '$' + a.usd.toLocaleString('en-US') : '?')
    .replace(/\{tick\}/g, a.tick !== undefined && a.tick >= 0 ? (ctx.clock ?? String)(a.tick) : '?');
}

export type Query = { event: string; verb?: string; speaker: string; outcome?: string; temperament?: string; about?: string };

/** The entry for a query: every field an entry names must equal the query's; the entry naming the most fields wins; file order breaks a tie.
 *  An entry that leaves a field out answers any value of it (so `reply/pay/guard/turned` with no temperament serves every temperament). */
export function lookup(t: Pick<TextFile, 'dialogue'>, q: Query): Entry | undefined {
  let best: Entry | undefined, bestScore = -1;
  for (const e of t.dialogue) {
    if (e.event !== q.event || e.speaker !== q.speaker) continue;
    let score = 0, ok = true;
    for (const f of ['verb', 'outcome', 'temperament', 'about'] as const) {
      const have = e[f];
      if (have === undefined) continue;
      if (have !== q[f]) { ok = false; break; }
      score++;
    }
    if (ok && score > bestScore) { best = e; bestScore = score; }
  }
  return best;
}

/** The key a use of an entry is counted under: its fields in a fixed order, absent ones skipped. */
export const lineKey = (q: Query): string => [q.event, q.verb, q.speaker, q.outcome, q.temperament, q.about].filter((x) => x !== undefined).join('/');

/** The variant for the n-th use (0-based) of a key in a run: fnv1a32(seed|key|i) mod count, and a repeat of the previous use's variant moves on one. */
export function pickIndex(seed: string, key: string, n: number, count: number): number {
  let prev = -1, at = 0;
  for (let i = 0; i <= n; i++) {
    at = fnv1a32(`${seed}|${key}|${i}`) % count;
    if (count > 1 && at === prev) at = (at + 1) % count;
    prev = at;
  }
  return at;
}

export function pickLine(t: Pick<TextFile, 'dialogue'>, seed: string, q: Query, n: number): string | undefined {
  const e = lookup(t, q);
  return e ? e.lines[pickIndex(seed, lineKey(q), n, e.lines.length)] : undefined;
}

/** The words in a spoken line. */
export const wordCount = (s: string): number => s.trim().split(/\s+/).filter(Boolean).length;

// ---------------------------------------------------------------- scenes

export type SceneQuery = { kind: string; verb?: string; about?: string; outcome?: string; temperament?: string; event?: string };
export const sceneKey = (q: SceneQuery & { sight?: string }): string => [q.kind, q.verb, q.about, q.outcome, q.temperament, q.event, q.sight].filter((x) => x !== undefined).join('/');

/** The scene for a query: every field a scene names must equal the query's; the one naming the most fields wins; file order breaks a tie. */
export function lookupScene(t: Pick<TextFile, 'scenes'>, q: SceneQuery): Scene | undefined {
  let best: Scene | undefined, bestScore = -1;
  for (const sc of t.scenes.items) {
    if (sc.kind !== q.kind) continue;
    let score = 0, ok = true;
    for (const f of ['verb', 'about', 'outcome', 'temperament', 'event'] as const) {
      const have = sc[f];
      if (have === undefined) continue;
      if (have !== q[f]) { ok = false; break; }
      score++;
    }
    if (ok && score > bestScore) { best = sc; bestScore = score; }
  }
  return best;
}

export type PlayedScene = { beats: { speaker: string; text: string }[]; result: string; sight?: string };

/** Every city scene for a world event (one may be heard anywhere, another seen only within sight): the host plays each whose `sight` holds. */
export const scenesFor = (t: Pick<TextFile, 'scenes'>, kind: string, event: string): Scene[] => t.scenes.items.filter((sc) => sc.kind === kind && sc.event === event);

/** One scene played: the variant is pickIndex(seed, sceneKey, n, variants), each `ref` beat is pickLine() of its dialogue entry with the same seed and n.
 *  The result keeps its placeholders for the host to fill from engine state. A ref with no dialogue line is a hole (the checker rejects it). */
export function playScene(t: Pick<TextFile, 'scenes' | 'dialogue'>, sc: Scene, seed: string, n: number): PlayedScene {
  const v = sc.variants[pickIndex(seed, sceneKey(sc), n, sc.variants.length)]!;
  const beats = v.beats.map((b) => {
    if ('line' in b) return { speaker: b.speaker, text: b.line };
    const q: Query = b.ref === 'done' ? { event: 'done', verb: 'turn', speaker: b.speaker, outcome: sc.outcome }
      : b.ref === 'reply' ? { event: 'reply', verb: sc.verb, speaker: b.speaker, outcome: sc.outcome, temperament: sc.temperament }
        : b.ref === 'confirm' ? { event: 'confirm', verb: 'call', speaker: b.speaker, temperament: sc.temperament }
          : { event: b.ref, verb: sc.verb, speaker: b.speaker };
    return { speaker: b.speaker, text: pickLine(t, seed, q, n) ?? '' };
  });
  return { beats, result: sc.result, sight: sc.sight };
}
