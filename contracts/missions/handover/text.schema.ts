// suborned.text/1: the words of one mission, contracts/missions/<id>/text.json (Fable, brief 011). Build-time content the Unity
// host reads; nothing is generated at runtime (GDD §01 no AI at runtime, §14 Fable: built at build time, never live).
//
// What is in the file:
//   briefing   the brief card, objective, win, each fail in plain words, clock and close, purse, two hints, the objective tracker,
//              the timeline as the status pill words it, one line per agent
//   hints · toasts · goingLoud   the training hints H1–H11 (Motive's triggers, copied), H12 (the run toggle), H13 (turn the view), H14–H16 (the end steps: Enter, Extract, Take at the pickup) and H17 (the fight hint: right click a hostile to fire), the first-run toasts, the going-loud card's words
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
//   less       brief 014 (less reading; Sky 2026-10-16: "It's the reading, it's too much"): the short forms the play HUD shows at rest, kept beside
//              the long ones. Every long line stays and is what Alt (hold) and the debrief show; the short forms are `short` on a lock, a hint and a
//              timeline mark, `fate` and `beat` on a card, `beat` on a scene, and the `less` block (the one-line price grammar, the feed, the
//              objective line, the end card's one line, pops that are not locks, cancel costs in a few words, the Alt words for the outcome marks)
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
export const WATCHED = ['camera-entrance', 'camera-pickup', 'camera-mast-314', 'patrol'] as const;
export const SPEAKERS = [...AGENTS, ...PEOPLE, ...OTHERS] as const;
/** The action panel's verbs: one id each (the host's Slots ids; `ask`…`pressure` are the Fixer's `turn` with an approach). */
export const VERBS = ['watch', 'listen', 'mislead', 'ask', 'pay', 'help', 'pressure', 'call', 'loop', 'reroute', 'enter', 'take',
  'board', 'go', 'fire', 'draw', 'holster', 'cease', 'stance', 'panic', 'carry', 'takedown', 'drop', 'stash', 'restrain', 'move', 'run', 'cancel', 'withdraw'] as const;
/** `turn` stands for any of the four approaches (the Fixer's reaction to how it went). `turn-view` is the camera hint H13 (brief 016): a control, not an order, so it is no engine verb. */
/** `extract` is brief 018's retired order, kept as a dialogue verb only so the texts pinned in recorded dialogue runs (r017 to r019, which predate Board and Go) still parse on replay; no live text uses it. */
export const DIALOGUE_VERBS = [...VERBS, 'turn', 'turn-view', 'extract'] as const;
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
/** brief 014: a short form is a fragment of a few words (a pop, a beat, a mark), at most `max` characters and never a sentence. */
export const SHORT_MAX = 24;
export const BEAT_MAX = 24;
/** brief 014: the price line's fields, in the order the line shows them (canonical). `learns` = the icons of what a Watch or Listen teaches;
 *  `pace` = a carry's walking pace; `mark` = the outcome mark (works, refuse, half, unknown). */
export const PRICE_FIELDS = ['name', 'usd', 'time', 'trail', 'alert', 'eye', 'learns', 'pace', 'mark'] as const;

const clock = z.string().regex(/^\d{1,2}:[0-5]\d$/);
const code = z.string().regex(/^[a-z][a-z0-9-]*$/);
/** One line of text: ≤ 80 characters, one line, no markup. `{who} {op} {m} {usd} {target}` are filled by the host (the checker tests the widest fill). */
const Line = z.string().min(1).max(MAX_LINE).regex(/^[^\n<>]+$/);
const Speaker = z.enum(SPEAKERS);
const Verb = z.enum(VERBS);
const Short = (max: number = SHORT_MAX) => z.string().min(1).max(max).regex(/^[^\n<>]+$/);
const PriceField = z.enum(PRICE_FIELDS);
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
  onTargetBy: z.partialRecord(z.enum(AGENTS), z.record(z.string(), Line)).optional()
    .describe('Sky 2026-10-18: the same verb on one target in one agent\'s voice, when that agent\'s card differs from the plain `onTarget` (the Tech\'s Watch on a machine); read the agent\'s line first, `onTarget` otherwise'),
  ways: z.record(code, z.strictObject({ label: z.string().min(1).max(24), does: Line })).optional()
    .describe('brief 013: an order with a choice (Order.How: takedown knockout or kill): the panel row for each way and its price in words'),
});

export const Lock = z.strictObject({
  line: Line.describe('what is wrong and what unlocks it; {who} {op} come from the order, {m} {usd} {tick} from the engine\'s Answer. Brief 014: this is the Alt line (hold Alt on the locked row) and the history; the pop at the click is `short`'),
  short: Short().describe('brief 014: the pop at the click, a few words, no sentence, no {who} or {op} (the pop sits on what it names); every number the line carries ({m} {usd} {tick}) stays, e.g. "Too far · {m} m"'),
  verbs: z.array(z.union([Verb, z.literal('*')])).min(1).describe('the verbs that can return it'),
  byTarget: z.partialRecord(z.enum(['machine']), z.strictObject({ line: Line, short: Short() })).optional()
    .describe('brief 019 (plain copy; Sky 2026-10-18): the same refusal when the order is on a machine (a camera or the patrol) rather than a person. only-scout: a person is "Scout only", a camera or the patrol is "Scout or Tech". Additive: a host that does not read it shows the base line and short'),
});

/** What an agent is doing now (sim Game.ActivityOf): the roster and verb plate words. `{who}` is the activity's target. */
export const Activity = z.strictObject({
  idle: Line, down: Line, walking: Line, running: Line, drawing: Line, holstering: Line, firing: Line,
  holding: z.strictObject({ loop: Line, carry: Line, take: Line }),
  acting: z.strictObject({ board: Line, watch: Line, listen: Line, ask: Line, pay: Line, help: Line, pressure: Line, loop: Line, reroute: Line, mislead: Line, enter: Line, take: Line, carry: Line,
    takedown: Line, drop: Line, stash: Line, restrain: Line, body: Line.describe('any other work with a body (the rulebook cancel verb `body`: lift, drop, stash or tie)') }),
});

/** A training hint line fits Glyph's 110 characters (briefs/011/motive-fights.md §8). */
export const HINT_MAX = 110;
const HintLine = z.string().min(1).max(HINT_MAX).regex(/^[^\n<>]+$/);
export const Hint = z.strictObject({
  priority: z.number().int().min(-9).describe('the order hints are offered in, lowest first; 0 = ahead of the training line (H13, the view); -1 = the fight hint (H17), behind the end steps; below -1 = the Handover\'s end steps (H14 Enter, H16 Take at the pickup, H15 Board), which win over every other hint when their trigger holds'),
  agent: z.enum([...AGENTS, 'any']).describe('the agent the line names; `any` = "another agent" (H11: any agent can carry)'),
  verb: DVerb,
  target: z.string().min(1).describe('what the hint rings, in words'),
  line: HintLine.describe('brief 014: the long line, shown on Alt and by the relearn key; the line at rest is `short`'),
  short: Short(36).describe('brief 014: the Next Step line, an imperative of at most six words with the one noun that is ringed in [brackets] (the host colours it as its marker); {who} is the person; it names no agent (the agent\'s portrait pulses) and no approach'),
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
  beat: Short(BEAT_MAX).optional().describe('brief 014: the feed line when the scene starts and none of its speakers is on screen (its beats play as speech bubbles in the world, not as a card); at most four words'),
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
export const CARD_FATES = ['beat', 'icon', 'alt', 'debrief'] as const;
/** brief 014: where a piece of text goes. speech = a person talking, in the world; beat = a one-line feed beat; icon = an icon, number, ring or mark in the street (Glyph draws it); alt = full detail on hold (Alt) and in the history key; debrief = after the run; cut = not shown; label = a name or a word or two the HUD needs as it is. */
export const FATES = ['speech', 'beat', 'icon', 'alt', 'debrief', 'cut', 'label'] as const;
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
  fate: z.enum(CARD_FATES).describe('brief 014: where the card goes now. beat = a one-line feed beat (`beat`); icon = the street shows it (a marker, a ring, a lock, a pip) and the card text is history only; alt = history only (behind L) and Alt; debrief = the debrief says it'),
  beat: z.union([Short(BEAT_MAX), z.record(code, Short(BEAT_MAX))]).optional().describe('brief 014: the feed line of a beat card (at most four words), or one per `pick` key (a key with no line shows none); only on fate beat; shown when no bubble of the event\'s speech is on screen'),
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
  between: z.record(code, Line).describe('brief 013 and brief 018: the between-missions lines a takedown or the getaway leaves (rulebook debrief.lines: volunteer, statement, missing, grief, worry, area-watch, case, left-behind, captive, car-seen), keyed by the rulebook id; each line carries every field the rulebook lists for it'),
  getaway: z.strictObject({
    holdsOut: Line.describe('brief 018: the `captive` line for a captive who does not talk inside the rescue window (the projection has no talk); the host picks it instead of `between.captive`. Fields {who} {held-at} {rescue-until}'),
    fate: z.record(code, Line).describe('brief 018 {fate} of `left-behind`: captured, walked (the engine\'s LeftBehind fate)'),
    why: z.record(code, Line).describe('brief 018 {why} of `left-behind`: downed, fighting, sealed, walked (the engine\'s why), as a clause that follows ": "'),
    by: z.record(code, Line).describe('brief 018 {by} of `car-seen`, by the role of the id the engine names (roleOfId): officer, robot, patrol'),
    post: Line.describe('brief 018 {held-at} of `captive` when the host has no better name for the district\'s police post'),
  }).describe('brief 018 (the getaway; rulebook debrief.lines left-behind, captive, car-seen): the words the host puts in those three lines. {fate} and {why} follow the engine\'s LeftBehind fate and why; {by} the role of the id CarSeenBy names; {held-at} is the host\'s name for the district\'s police post, else `post`; {holds} and {rescue-until} are a number with its unit ("1 mission", "3 missions"); {held-at} is at most 8 characters, "the post" by default'),
  standing: z.strictObject({
    line: Line.describe('brief 014: the Standing line (rulebook debrief.lines.standing). Placeholders {district} {band} {from} {to} {cause}; the host fills {from} and {to} signed (+3, 0, -1) and {cause} from `causes` with {who} filled'),
    bands: z.strictObject({ hero: Line, neutral: Line, menace: Line }).describe('{band}: the band id as a word'),
    causes: z.record(code, Line).describe('one short sentence per rulebook standing.causes id; the word causes are rumour (told, not learned) and carry {who}'),
  }),
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

/** An Intel fragment: one or two words (a `{name}` does not count) naming a fact, 14 characters at most, never a sentence. */
const Fragment = z.string().min(1).max(16).regex(/^[A-Z][A-Za-z {}]*$/);

/** brief 014: the short forms and the grammar the play HUD reads at rest. Nothing here is a rule; every number a line carries comes from the engine. */
export const Less = z.strictObject({
  rule: z.string().min(1),
  price: z.strictObject({
    rule: z.string().min(1),
    order: z.array(PriceField).describe('the canonical field order; each verb\'s list below is a subsequence of it'),
    fields: z.record(PriceField, z.strictObject({ shows: z.string().min(1), hiddenWhen: z.string().min(1) })),
    verbs: z.record(Verb, z.array(PriceField).min(1)).describe('the fields each verb\'s one-line price shows, in order; `name` first, always'),
    examples: z.record(code, z.string().min(1).max(60)).describe('worked lines with the engine\'s numbers from the play screenshots; `\u{1F441}` is the eye mark, `\u25B2` the Alert mark'),
  }),
  feed: z.strictObject({
    rule: z.string().min(1),
    maxOnScreen: z.number().int().min(1).max(2),
    seconds: z.number().int().min(2).max(6),
    dedupeSeconds: z.number().int().min(1),
    bubblesAtOnce: z.number().int().min(1).max(2).describe('speech bubbles on screen at once'),
    bubbleSeconds: z.number().int().min(2).max(5),
    cityBeats: z.number().int().min(1).max(4).describe('a call or city scene plays this many of its first beats as bubbles in play; the rest are history. Overheard and approach scenes play every beat (what the cell learns is in them)'),
  }),
  objective: z.strictObject({
    rule: z.string().min(1),
    take: Short(32), carry: Short(32), go: Short(32).describe('brief 018: the goal line once Vale is in the car and the car has not left'), showSeconds: z.number().int().min(2).max(10),
    bring: Short(32).describe('the goal line while the Enforcer is inside for Vale (after Enter, before he comes out)'),
    steps: z.array(z.strictObject({ key: z.enum(['take', 'bring', 'carry', 'go']), when: z.string().min(1).describe('the game state in which the host shows this line as the goal') })).min(3)
      .describe('the goal line in order: take, then bring, then carry; each `key` names the line above, `when` is the state the host reads (Latch builds the trigger)'),
  }),
  briefing: z.strictObject({ rule: z.string().min(1), goal: Short(32) }),
  end: z.strictObject({
    rule: z.string().min(1),
    win: Short(), withdraw: Short(),
    fail: z.record(code, Short()).describe('one line per fail code (the headline); the full sentence is briefing.fails, shown in the debrief'),
  }),
  pop: z.record(code, Short()).describe('refusals and notices the host builds itself (not engine codes): one pop each'),
  cancel: z.record(code, Short(20)).describe('what a cancel costs, in a few words, by the same ids as cancelCosts; a costly one tints the cancel mark'),
  alt: z.strictObject({
    rule: z.string().min(1),
    outcome: z.record(code, Line).describe('the outcome marks in words, for Alt: ok works, worse works one step worse, no refuses, half not yet, unk unknown until the Scout learns who they are'),
  }),
  fates: z.record(z.string(), z.enum(FATES)).describe('every player-facing group of this file by its path, with where it goes (the inventory in briefs/014/fable-less.md, as data)'),
  hint: z.strictObject({
    rule: z.string().min(1),
    showSeconds: z.number().int().min(2).max(10),
    returnSeconds: z.number().int().min(5),
    maxShows: z.number().int().min(1).max(3),
  }),
  go: z.strictObject({
    rule: z.string().min(1),
    none: Short().describe('the Go card when nobody would be left'),
    leaves: Short(30).describe('the Go card when someone would be: {list} is "Tech (down), Scout", each name with its why in brackets when the fate is captured'),
    why: z.record(code, Short(16)).describe('the bracket after a name in {list}, by the engine\'s why (downed, fighting, sealed, walked)'),
    seen: Short().describe('the Go card\'s extra price when a security body would see the car leave'),
    flag: Short().describe('the roster flag on every agent not in the car while the Go card is open'),
    fate: z.record(code, Line).describe('Alt on a name\'s fate glyph, by the engine\'s fate (captured, walked); {who} is the agent'),
  }),
  intel: z.strictObject({
    rule: z.string().min(1),
    need: z.record(code, Fragment).describe('one fragment per need value the rulebook allows (people.rules.json needs.list); a new need without one fails the test'),
    temperament: z.record(code, Fragment).describe('one fragment per temperament (people.rules.json temperaments)'),
    secret: z.record(code, Fragment).describe('one fragment per secret value (people.rules.json secrets), `affair-named` with {name} (the lover, cast) and `none` (nothing hidden)'),
    verb: z.record(code, Fragment).describe('the chip word and the unknown-slot word: watch, listen, turn'),
  }),
});
export type Less = z.infer<typeof Less>;

/** The nouns a hint or objective line marks in [brackets]. */
export const marked = (s: string): string[] => [...s.matchAll(/\[([^\]]+)\]/g)].map((m) => m[1]!);

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
    holders: z.record(code, z.strictObject({ one: z.string().min(1).max(10), many: z.string().min(1).max(12) }))
      .describe('brief 018 (the getaway): what holds the car, by the role of an id the engine names in Answer.ids (rival-1, officer-1, security-1, robot-1, patrol, guard); `one` for a single holder, `many` with {n} for several of that role, and the key `mixed` when the roles differ. Each phrase is at most 10 characters so the pop "Blocked · down {holders}" fits. `holdersPhrase` is the reference'),
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
    timeline: z.array(z.strictObject({ at: clock, label: Line, short: Short(16).describe('brief 014: the mark in a word or two (the pill shows its icon and time; the word is on Alt)'), known: z.boolean().describe('false = the pill shows "?" until it happens') })),
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
  less: Less.describe('brief 014 (less reading): the short forms and grammar the HUD reads at rest; the long lines above are the Alt and debrief text'),
});
export type TextFile = z.infer<typeof TextFile>;

/** The values a card or debrief line may carry; `fillCard` is the reference for filling them. */
export type CardVals = { who?: string; agent?: string; alert?: number; n?: number; clock?: string; usd?: number; trail?: number; hits?: number; cause?: string; list?: string;
  /** brief 014's Standing line (rulebook debrief.lines.standing): district name, band word, signed from and to */
  district?: string; band?: string; from?: string; to?: string;
  /** brief 013's between-missions fields (the rulebook's debrief.lines): the words the host fills from the debrief */
  fact?: string; facts?: string; place?: string; of?: string; move?: string; fear?: string; guards?: number; cameras?: number; missions?: number; kind?: string; closes?: string;
  /** brief 018's getaway lines: {fate} {why} {held-at} {holds} {rescue-until} {by} */
  fate?: string; why?: string; heldAt?: string; holds?: number; rescueUntil?: number; by?: string };
export function fillCard(line: string, v: CardVals): string {
  const out = line.replace(/\{who\}/g, v.who ?? 'someone').replace(/\{agent\}/g, v.agent ?? 'an agent').replace(/\{alert\}/g, String(v.alert ?? '?'))
    .replace(/\{n\}/g, String(v.n ?? '?')).replace(/\{clock\}/g, v.clock ?? '?').replace(/\{usd\}/g, v.usd === undefined ? '?' : '$' + v.usd.toLocaleString('en-US'))
    .replace(/\{trail\}/g, String(v.trail ?? '?')).replace(/\{hits\}/g, String(v.hits ?? '?')).replace(/\{cause\}/g, v.cause ?? '?').replace(/\{list\}/g, v.list ?? '').replace(/\{district\}/g, v.district ?? 'the district').replace(/\{band\}/g, v.band ?? '?').replace(/\{from\}/g, v.from ?? '?').replace(/\{to\}/g, v.to ?? '?')
    .replace(/\{fact\}/g, v.fact ?? '?').replace(/\{facts\}/g, v.facts ?? '?').replace(/\{place\}/g, v.place ?? 'somewhere').replace(/\{of\}/g, v.of ?? 'someone')
    .replace(/\{move\}/g, v.move ?? '?').replace(/\{fear\}/g, v.fear ?? '?').replace(/\{guards\}/g, String(v.guards ?? '?')).replace(/\{cameras\}/g, String(v.cameras ?? '?'))
    .replace(/\{missions\}/g, String(v.missions ?? '?')).replace(/\{kind\}/g, v.kind ?? '?').replace(/\{closes\}/g, v.closes ?? '?')
    .replace(/\{fate\}/g, v.fate ?? '?').replace(/\{why\}/g, v.why ?? '?').replace(/\{held-at\}/g, v.heldAt ?? 'the police post').replace(/\{holds\}/g, String(v.holds ?? '?'))
    .replace(/\{rescue-until\}/g, String(v.rescueUntil ?? '?')).replace(/\{by\}/g, v.by ?? 'Security');
  return out.charAt(0).toUpperCase() + out.slice(1);
}
/** The result line of a card: a record result is chosen by `variant`. */
export function cardResult(t: Pick<TextFile, 'cards'>, code: string, variant?: string): string | undefined {
  const r = t.cards.items[code]?.result;
  return typeof r === 'string' || r === undefined ? r : variant === undefined ? undefined : r[variant];
}

// ---------------------------------------------------------------- the reference lookups (Latch ports these; the checker runs them)

/** What the engine sends with a refusal (sim Answer): -1 = none. */
export type Answer = { code: string; m?: number; cm?: number; usd?: number; tick?: number; /** brief 018: the ids a refusal names, comma-joined (car-held: its holders, in id order) */ ids?: string };

/** The role of an engine id (rival-1 → rival; security-1 and officer-1 → officer; robot-1 → robot; patrol; guard); anything else is `other`. */
export const roleOfId = (id: string): string => (/^rivals?(-\d+)?$/.test(id) ? 'rival' : /^(officer|security)(-\d+)?$/.test(id) ? 'officer' : /^robot(-\d+)?$/.test(id) ? 'robot' : id === 'patrol' ? 'patrol' : id === 'guard' ? 'guard' : 'other');
/** What holds the car, in words, for `{holders}` in a refusal: "a rival", "2 robots", "police", "patrol", or "3 hostiles" when the roles differ. `ids` is Answer.ids. */
export function holdersPhrase(t: Pick<TextFile, 'names'>, ids: string): string {
  const list = ids.split(',').map((x) => x.trim()).filter(Boolean);
  if (list.length === 0) return 'something';
  const roles = [...new Set(list.map(roleOfId))];
  const h = t.names.holders;
  const w = roles.length === 1 ? h[roles[0]!] : undefined;
  if (!w) return h['mixed']!.many.replace('{n}', String(list.length));
  return list.length === 1 ? w.one : w.many.replace('{n}', String(list.length));
}
/** The refusal line for an answer. `who` is the target's name ("Inez", "the driver"), `op` the agent ("Scout"), `clock` turns an engine tick into m:ss.
 *  {m} is Answer.m, or Answer.cm / 100 when the engine sends cm; {usd} is "$" and the thousands; {tick} is the mission clock. Unknown code = undefined. */
export function fillLock(t: Pick<TextFile, 'locks' | 'names'>, a: Answer, ctx: { who?: string; op?: string; clock?: (tick: number) => string } = {}): string | undefined {
  const l = t.locks[a.code];
  if (!l) return undefined;
  const m = a.m !== undefined && a.m >= 0 ? a.m : a.cm !== undefined && a.cm >= 0 ? Math.round(a.cm / 100) : undefined;
  return l.line
    .replace(/\{who\}/g, ctx.who ?? 'them').replace(/\{op\}/g, ctx.op ?? 'The agent').replace(/\{holders\}/g, holdersPhrase(t, a.ids ?? ''))
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
