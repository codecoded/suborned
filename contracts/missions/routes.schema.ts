// suborned.mission.routes/1: a mission's route scripts and what each must show, contracts/missions/<id>/routes/routes.json (brief 011 B).
// Each script is an order script in the engine's own format (suborned.sim.script/1, sim/Suborned.Sim/Script.cs: a start state, timed
// orders, `until`), played headless by the mission checker (tools/missions/check). This file says what every script is for:
// - kind: quiet (a route that wins with no shot), loud (wins with gunfire), fail (reaches one §07 fail), outcome (withdraw),
//   careless (brief 013: a takedown done carelessly, seen or found or reported; brief 031, careless: loiter, an agent left standing in a
//   watcher's sight until reported; brief 058, careless: timing, a window that runs out before the act it covers; `twin` names the careful script it is compared with);
// - route: which of the mission's routes it plays (quiet routes are counted by route, not by script: a variant is the same route);
// - primary: the route's reference script (the one the §09 conditions compare and vary);
// - expects: the outcome and fail code the engine must report (sim/Suborned.Sim/Game*.cs Fail codes), and (brief 018) who the car
//   left behind, with fate and why, and (brief 031) the Alert the run ends at;
// - pending: why it can't be played yet (the final fight, N10/N11, is not in the engine; brief 067: the site places no watcher its fail
//   needs); a pending script is listed, never played;
// - holds (brief 059, MC-13): the getaway stand at which this loud run's cell waits in the car while the Enforcer brings Vale out.
// `fails` is the mission's fail list (GDD §07), each with the engine codes that reach it and (brief 067) the site fact it `needs`, if any
// (MC-3 counts a line only where its fact holds) or (brief 039) the engine scenario that reaches it; `contacts` the roles a route may turn.
// `closing` is the mission's closing event; each primary quiet route names its own (`closes`): the fully informed player secures
// Vale on it ≥ `informedSpareS` before (§09). Zod, loud on
// failure: Routes.parse(json). Test: tools/missions/check/check.test.ts.
import { z } from 'zod';

const id = z.string().regex(/^[a-z][a-z0-9-]*$/);
const at = z.string().regex(/^\d{1,2}:[0-5]\d$/).describe('mission-clock offset m:ss');
const src = z.string().min(1);

export const RouteScript = z.strictObject({
  id, file: z.string().regex(/^[a-z0-9-]+\.json$/),
  plan: z.string().regex(/^[a-z0-9-]+\.plan\.json$/).optional().describe('routes/plans/<file>: the plan the script is composed from (bun run missions:check compose)'),
  adaptive: id.optional().describe('brief 080: the adaptive plan (<mission>/adaptive/<id>.adaptive.json) this reference is judged from, across seeds: its script file is not played'),
  kind: z.enum(['quiet', 'loud', 'fail', 'outcome', 'careless']),
  route: z.string().min(1).optional().describe('the route it plays: A, B, …; quiet and loud scripts name one'),
  primary: z.boolean().optional(),
  /** The route's closing event (§09 template: the event after which the route can no longer take Vale) and the engine log code that
   *  marks Vale secured on it (enter.in: the Enforcer is inside, Vale no longer leaves; take.done: Vale is with the Enforcer). */
  closes: z.strictObject({ at, what: z.string().min(1), secured: z.string().min(1) }).optional(),
  expects: z.strictObject({
    outcome: z.enum(['success', 'failure', 'withdrawn']), fail: id.optional(),
    /** brief 018 §3: everyone the car left behind, by op, with fate and why (the debrief's left-behind lines), exactly. */
    left: z.partialRecord(z.enum(['scout', 'fixer', 'tech', 'enforcer']), z.enum(['captured downed', 'captured fighting', 'captured sealed', 'walked'])).optional(),
    /** brief 031: the Alert the run ends at, exactly (a loiterer reported is a found fault, Alert at least 1, with no Trail: the outcome alone can't show it). */
    alert: z.number().int().min(0).max(3).optional(),
  }),
  /** A careless script's fault (default violence, brief 013 D, MC-7): `loiter` (brief 031, MC-12) is its careful twin with an agent left standing in a watcher's sight;
   *  `timing` (brief 058, Loop v2) is its careful twin with a window planted too early, so it runs out before the act it was for (judged by its expects, MC-0). */
  careless: z.enum(['violence', 'loiter', 'timing']).optional(),
  reaches: id.optional().describe('the `fails` entry a fail script reaches'),
  /** brief 059 M059-5.3 (MC-13): the getaway stand (the site's getaway.stands[] id) at which this loud run's cell waits in the car while the
   *  Enforcer brings Vale out: its Go with Vale aboard must be refused (the car held), so no stand opens an unfought loud exit. */
  holds: id.optional(),
  twin: id.optional().describe('a careless script: the careful script it is the careless version of (brief 013 D, MC-7)'),
  pending: z.string().min(1).optional(),
  src, note: z.string().min(1).optional(),
}).refine(s => s.kind !== 'fail' || (s.expects.outcome === 'failure' && !!s.expects.fail && !!s.reaches), { error: 'a fail script expects a failure with its code and names the fail it reaches' })
  .refine(s => !['quiet', 'loud'].includes(s.kind) || (!!s.route && s.expects.outcome === 'success'), { error: 'a quiet or loud script names its route and expects a win' })
  .refine(s => (s.kind === 'careless') === !!s.twin, { error: 'a careless script, and only a careless script, names its careful twin' })
  .refine(s => !s.careless || s.kind === 'careless', { error: 'only a careless script names its fault' });

export const Routes = z.strictObject({
  schema: z.literal('suborned.mission.routes/1'),
  mission: id,
  note: z.string().min(1),
  closing: z.strictObject({ at, what: z.string().min(1), src }),
  informedSpareS: z.number().int().positive().describe('§09: the fully informed player finishes this long before the closing event'),
  contacts: z.array(z.strictObject({ role: id, who: z.string().min(1) })).min(1),
  /** A fail entry may need a fact of the site (brief 067, Motive briefs/067/motive-square.md §5.3): `patrol-watches-entry` (the engine's
   *  `suborned-sim facts` patrolWatchesEntry: the patrol's sweep, or its reroute wait, sees the public door) counts the line only where it holds
   *  (§09: a fail that needs a watcher the site doesn't place is not counted).
   *  A fail entry may instead be covered by one of the engine's scenarios (brief 039, Keel's spec; Sky 2026-10-27 "Sure": the cell down is
   *  K3-CELL-DOWN, not a route): `scenario` names it, and MC-3 counts the line as reached when `suborned-sim check <scenario>` prints
   *  "<scenario>: ok" (its checks and its replay hold). A route script that reaches the line still counts too. */
  fails: z.array(z.strictObject({ id, gdd: z.string().min(1), codes: z.array(id).min(1), needs: z.enum(['patrol-watches-entry']).optional(),
    scenario: z.string().regex(/^[A-Z0-9][A-Za-z0-9-]*$/).optional().describe('a suborned-sim check scenario id that reaches this fail') })).min(1),
  scripts: z.array(RouteScript).min(1),
})
  .refine(r => new Set(r.scripts.map(s => s.id)).size === r.scripts.length, { path: ['scripts'], error: 'script ids are unique' })
  .refine(r => r.scripts.every(s => !s.reaches || r.fails.some(f => f.id === s.reaches && f.codes.includes(s.expects.fail!))), { path: ['scripts'], error: 'a fail script reaches a listed fail by one of its codes' })
  .refine(r => r.scripts.every(s => !s.twin || r.scripts.some(t => t.id === s.twin && t.kind === 'quiet')), { path: ['scripts'], error: 'a careless script\'s twin is a quiet script of the manifest' })
  .refine(r => [...new Set(r.scripts.filter(s => s.kind === 'quiet').map(s => s.route))].every(rt => r.scripts.filter(s => s.kind === 'quiet' && s.route === rt && s.primary).length === 1), { path: ['scripts'], error: 'each quiet route has one primary script' })
  .refine(r => r.scripts.filter(s => s.kind === 'loud' && s.primary).length === 1, { path: ['scripts'], error: 'one primary loud script' })
  .refine(r => r.scripts.every(s => !(s.kind === 'quiet' && s.primary) || !!s.closes), { path: ['scripts'], error: 'a primary quiet route names its closing event' })
  .refine(r => r.scripts.every(s => !s.holds || ['fail', 'careless', 'outcome'].includes(s.kind)), { path: ['scripts'], error: 'a loud-wait run (holds) is a fail, careless or outcome script, never a reference win' });
export type Routes = z.infer<typeof Routes>;
export type RouteScript = z.infer<typeof RouteScript>;
