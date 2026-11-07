// The end of a game in words (Fable): the end card and the debrief by the ids of sim Debrief.cs (briefs 013, 014, 018, 096), and the debrief page
// (brief 099, concept A: the front page Sky picked, 2026-11-07). Moved out of text.schema.ts by topic (brief 084: files stay under 500 lines);
// text.schema.ts re-exports it. Host content, read only: nothing here changes a run.
import { z } from 'zod';

const code = z.string().regex(/^[a-z][a-z0-9-]*$/);
/** text.schema.ts's line: at most 80 characters (MAX_LINE), one line, no markup. */
const Line = z.string().min(1).max(80).regex(/^[^\n<>]+$/);
const Short = (max: number) => z.string().min(1).max(max).regex(/^[^\n<>]+$/);
/** The page's slots: a label or a note is a fragment (no full stop, no exclamation mark); a sentence is one line ending in a full stop. A slot's size is
 *  at its widest fills ({cameras} is one character's worth of "2"), so the raw text is held to one line here and the style agent's checks held each
 *  text to its slot (tools/style-agent/short-checks.ts); text.test.ts holds every one to 80 at its widest. */
const Frag = (slot: number) => z.string().min(1).max(80).regex(/^[^\n<>.!]+$/, 'a fragment: no full stop, no exclamation mark').describe(`a fragment for a slot of ${slot} characters at its widest fills`);
const Sentence = z.string().min(1).max(80).regex(/^[A-Z{][^\n<>!]*\.$/, 'one line, a capital, ending in a full stop');
const Two = (head: number, line: number) => z.strictObject({ head: Frag(head), line: Frag(line) });
/** A number's fold (brief 099, round 2: the four Latch's page reads besides Trail's): a word or two beside the fold's title, and what the number is. */
const Fold = z.strictObject({ head: Frag(20).describe('beside the fold\'s title ("Time 5:53"), lower case, as the Trail fold\'s byWhat'), means: Sentence });

/** brief 099, concept A; its rule is the last part of debrief.rule (the text checks walk every other debrief string as a line). */
export const DebriefPage = z.strictObject({
  head: z.strictObject({ win: Frag(40), fail: Frag(40), withdraw: Frag(40) }).describe('the small caps over the headline ({clock}: when the car left, when the run was lost, or when the cell pulled out: the Withdraw order, or Go before Popovic was aboard); the host sets them in capitals'),
  way: z.strictObject({ yours: Frag(16) }).describe('beside the way\'s name (less.briefing.ways) over the start page\'s rows: the way this run took (the pickup if the driver was turned, else the front door)'),
  how: z.record(code, Frag(24)).describe('a note after a start-page row (book.job.needs.ways), before the time it came true: how it came true, by the log line that made it so (called call.waitress, knocked a takedown, shot guard.down, killed takedown.dead, looped loop.on, cut camera.cut, shot-out fire.camera-dark, in enter.in, paid/helped/pressed/asked turn.<approach>.turned on the driver); taped: the pickup camera recorded the Pay (the row failed); never: a row that never came true'),
  numbers: z.strictObject({ time: Frag(10), cash: Frag(10), trail: Frag(10), alert: Frag(10), standing: Frag(10) }).describe('the five numbers\' labels'),
  reasons: z.strictObject({
    time: z.strictObject({ win: Frag(40) }).describe('{clock}: the time to spare against Popovic\'s car leaving at 5:50; a fail\'s reason is next.by[fail][why].time'),
    cash: z.record(code, Frag(40)).describe('what the money went on, by the turns that landed: help and pay (one, {who} the person helped or paid), both (two or more, no {who}), none'),
    trail: z.strictObject({ shooting: Frag(40), none: Frag(40), list: Frag(40) }).describe('shooting when it made half the Trail or more ({n} of {trail}); none at 0; list ({list}) the biggest of trail.rows[].short joined'),
    alert: z.strictObject({ none: Frag(40) }).describe('at Alert 0; a rise shows its cause\'s short from the why block, with {where}'),
  }).describe('the one line under each number; Standing\'s is standing.causes above (the run\'s main cause)'),
  trail: z.strictObject({
    means: Sentence, law: Sentence, byWhat: Frag(16), byWho: Frag(16),
    rows: z.record(code, z.strictObject({ row: Frag(48), short: Frag(24) })).describe('one per engine Trail cause (Game.AddTrail): ask, pay (a person or one of the rival crew), help, pressure (turn.<approach>:<role>), call, recorded (a camera\'s tape), loop, disable, reroute, mislead, shot (gunfire, hit and down on one target, grouped), knockout, kill, seen (witnessed:*), warned (warning-heeded), car-seen, lent-seen, car-lost, take-unturned; row is the fold\'s row ({op} the agent as the book\'s cell page names them, {who} the person or camera, {by} who saw), short the Trail number\'s list word'),
  }).describe('the Trail explainer, opened from its number as the book\'s fold'),
  city: z.strictObject({ case: Two(40, 40), watch: Two(40, 40), hunt: Two(40, 40), kept: Frag(16), word: Two(40, 40), face: Frag(40) })
    .describe('what the city does now, from the debrief: nextMission.cases ({who} {missions}), areaWatch ({guards} {cameras} {place} {missions}), investigators.hunted ({by}), assets (kept, over the assets line), the word lines ({n}; face: {n} {op}, the Fixer\'s face described)'),
  beats: z.record(code, Frag(18)).describe('the run along the foot, one word or two under each beat, by the log line that marks it (watched, listened, turned {who}, loop, reroute, call, in, out, rivals, lookout, alert {alert}, taped, vale-out, fight, shots, report, car-seen, away, lost, withdrew); the end\'s beat also ends the Time fold'),
  next: z.strictObject({ head: Frag(16), by: z.record(code, z.record(code, z.strictObject({ line: Sentence, time: Frag(40) }))) })
    .describe('a fail\'s Next time box and the reason under its Time number, by the fail camera\'s keys (less.failcam): line says what to deal with and why, never the order to give'),
  ways: z.strictObject({ again: Frag(20), new: Frag(20), same: Frag(20), replay: Frag(20), start: Frag(20) }).describe('the buttons at the foot: Try again (new start or same start, its seed after it), watch this run again, back to the start page'),
  folds: z.strictObject({ time: Fold, cash: Fold, alert: Fold, standing: Fold, rest: Frag(20) })
    .describe('round 2 (Latch\'s notes): the folds of Time, Spent, Alert and Standing (Trail\'s is trail above); rest: the words of a Trail, Alert or Standing fold\'s last row when its rows do not all fit, after the sum of the rest ("+3")'),
  and: Frag(5).describe('the word that joins the last two of a list: the Trail reason\'s {list} ("the pay, the loop and her call") and a fold row\'s shooters'),
});
export type DebriefPage = z.infer<typeof DebriefPage>;

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
  fightWhy: z.record(code, Line).describe('engine 0.33.0 (brief 059): one clause for each id the debrief\'s finalFight.why can carry (sim FightWhy.Add), each a plain sentence that follows the Final fight line on the end card and in the history'),
  cause: z.record(code, z.record(code, z.strictObject({
    head: Short(32).describe('the end card\'s title for this cause (the card wraps it to two lines at most: HudToolkit EndCardAudit)'),
    line: Line.describe('the line under it: what happened and why, in everyday words; {by} {agent} {holders} as less.failcam and names.holders'),
    story: z.array(code).optional().describe('keys of debrief.story, in order: the debrief\'s route lines after "Popovic reached the getaway car.", so the story runs to the end'),
  }))).describe('brief 096 (Sky 2026-11-04: "the End card needs to be plain english and clear but not too formal"): the end card by cause, not by code: cause[Game.FailCode][key], the key FailCause.Why, except `sealed`, keyed by FailCause.Car (held, ready, empty; `unknown` when none), and `withdraw`, keyed by the withdrawal\'s why. A cause with no entry (why `unresolved`) falls back to less.end.fail and briefing.fails'),
  story: z.record(code, Line).describe('brief 096: the lines that finish the story after "Popovic reached the getaway car." on the debrief, named by debrief.cause[..].story; {holders} is names.holders as holdersPhrase says Game.CarHolders() at the end (a line with {holders} and no holders is left out)'),
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
  page: DebriefPage.optional().describe('brief 099 (concept A, the front page Sky picked): the debrief page\'s own words; the page also reads headline, result, cause, investigation, causes, assets and standing above, and the why block'),
});
