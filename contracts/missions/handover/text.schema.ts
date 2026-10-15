// suborned.text/1: the words of one mission, contracts/missions/<id>/text.json (Fable, brief 011). Build-time content the Unity
// host reads; nothing is generated at runtime (GDD §01 no AI at runtime, §14 Fable: built at build time, never live).
//
// What is in the file:
//   briefing   the brief card, objective, win, each fail in plain words, clock and close, purse, two hints, the objective tracker,
//              the timeline as the status pill words it, one line per agent
//   hints · toasts · goingLoud   the training hints H1–H11 (Motive's triggers, copied), the first-run toasts, the going-loud card's words
//   targets    one line on each thing the cell can right click: who or what it is, in what the cell may know at the start
//   actions    every verb the action panel can show: label, who gives it, one plain line with its trade-off, and a line for the
//              same verb on a particular target
//   cancelCosts · activity   what cancelling an order costs or keeps; what each agent is doing now, in words for the roster
//   locks      one line for every refusal code the engine gives (Game.Issue returns Answer { ok, code, why, m, cm, usd, tick });
//              the line says what unlocks it and carries {who} {op} {m} {usd} {tick}; `fillLock` is the reference for filling them
//   events     the situations that can speak: what triggers each (an order, the engine's log code, the clock) and who may speak
//   exchange   the order in which an order's events play, for the collapsible dialogue
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
export const SPEAKERS = [...AGENTS, ...PEOPLE, ...OTHERS] as const;
/** The action panel's verbs: one id each (the host's Slots ids; `ask`…`pressure` are the Fixer's `turn` with an approach). */
export const VERBS = ['watch', 'listen', 'mislead', 'ask', 'pay', 'help', 'pressure', 'call', 'loop', 'reroute', 'enter', 'take',
  'extract', 'fire', 'draw', 'holster', 'cease', 'stance', 'panic', 'carry', 'move', 'run', 'cancel', 'withdraw'] as const;
/** `turn` stands for any of the four approaches (the Fixer's reaction to how it went). */
export const DIALOGUE_VERBS = [...VERBS, 'turn'] as const;
/** A target's reply: the rulebook's turn outcomes, and `suspicious` when the person reports the approach (rulebook `reports`). */
export const OUTCOMES = ['turned', 'refused', 'not-yet', 'kept', 'suspicious'] as const;
/** The engine's fail codes (Game.Fail), `withdraw` (an order) and `engine-error`. */
export const FAILS = ['abduction-seen-camera', 'driver-shot', 'enforcer-down', 'entry-seen-camera', 'entry-seen-guard', 'entry-seen-patrol',
  'rivals-reach-vale', 'sealed', 'vale-dead', 'vale-leaves', 'engine-error'] as const;
export const NAMED = ['Inez', 'Vale'] as const; // the only people the words may name (people.rules names.pinned); others by role
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
});

export const Lock = z.strictObject({
  line: Line.describe('what is wrong and what unlocks it; {who} {op} come from the order, {m} {usd} {tick} from the engine\'s Answer'),
  verbs: z.array(z.union([Verb, z.literal('*')])).min(1).describe('the verbs that can return it'),
});

/** What an agent is doing now (sim Game.ActivityOf): the roster and verb plate words. `{who}` is the activity's target. */
export const Activity = z.strictObject({
  idle: Line, down: Line, walking: Line, running: Line, drawing: Line, holstering: Line, firing: Line,
  holding: z.strictObject({ loop: Line, carry: Line, take: Line }),
  acting: z.strictObject({ watch: Line, listen: Line, ask: Line, pay: Line, help: Line, pressure: Line, loop: Line, reroute: Line, mislead: Line, enter: Line, take: Line, carry: Line }),
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

export const Target = z.strictObject({ name: z.string().min(1).max(32), about: Line });

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
  about: z.enum(PEOPLE).optional().describe('read: whom the Scout is reading'),
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
  events: z.record(code, EventDef),
  exchange: z.record(Verb, z.array(code)).describe('verb → the events an order plays through, in order; the collapsed view shows the last'),
  silentStates: z.record(z.string(), z.string()).describe('hostile AI states (Gun.St) that speak no line of `ai-state`, with the reason'),
  silentLogs: z.record(z.string(), z.string()).describe('engine log codes (or prefixes ending in .) that speak no line, with the reason'),
  dialogue: z.array(Entry).min(1),
});
export type TextFile = z.infer<typeof TextFile>;

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
