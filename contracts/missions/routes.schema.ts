// suborned.mission.routes/1: a mission's route scripts and what each must show, contracts/missions/<id>/routes/routes.json (brief 011 B).
// Each script is an order script in the engine's own format (suborned.sim.script/1, sim/Suborned.Sim/Script.cs: a start state, timed
// orders, `until`), played headless by the mission checker (tools/missions/check). This file says what every script is for:
// - kind: quiet (a route that wins with no shot), loud (wins with gunfire), fail (reaches one §07 fail), outcome (withdraw);
// - route: which of the mission's routes it plays (quiet routes are counted by route, not by script: a variant is the same route);
// - primary: the route's reference script (the one the §09 conditions compare and vary);
// - expects: the outcome and fail code the engine must report (sim/Suborned.Sim/Game*.cs Fail codes);
// - pending: why it can't be played yet (the final fight, N10/N11, is not in the engine); a pending script is listed, never played.
// `fails` is the mission's fail list (GDD §07), each with the engine codes that reach it; `contacts` the roles a route may turn.
// `closing` is the mission's closing event; each primary quiet route names its own (`closes`): the fully informed player secures
// Vale on it ≥ `informedSpareS` before (§09). Zod, loud on
// failure: Routes.parse(json). Test: tools/missions/check/check.test.ts.
import { z } from 'zod';

const id = z.string().regex(/^[a-z][a-z0-9-]*$/);
const at = z.string().regex(/^\d{1,2}:[0-5]\d$/).describe('mission-clock offset m:ss');
const src = z.string().min(1);

export const RouteScript = z.strictObject({
  id, file: z.string().regex(/^[a-z0-9-]+\.json$/),
  kind: z.enum(['quiet', 'loud', 'fail', 'outcome']),
  route: z.string().min(1).optional().describe('the route it plays: A, B, …; quiet and loud scripts name one'),
  primary: z.boolean().optional(),
  /** The route's closing event (§09 template: the event after which the route can no longer take Vale) and the engine log code that
   *  marks Vale secured on it (enter.in: the Enforcer is inside, Vale no longer leaves; take.done: Vale is with the Enforcer). */
  closes: z.strictObject({ at, what: z.string().min(1), secured: z.string().min(1) }).optional(),
  expects: z.strictObject({ outcome: z.enum(['success', 'failure', 'withdrawn']), fail: id.optional() }),
  reaches: id.optional().describe('the `fails` entry a fail script reaches'),
  pending: z.string().min(1).optional(),
  src, note: z.string().min(1).optional(),
}).refine(s => s.kind !== 'fail' || (s.expects.outcome === 'failure' && !!s.expects.fail && !!s.reaches), { error: 'a fail script expects a failure with its code and names the fail it reaches' })
  .refine(s => !['quiet', 'loud'].includes(s.kind) || (!!s.route && s.expects.outcome === 'success'), { error: 'a quiet or loud script names its route and expects a win' });

export const Routes = z.strictObject({
  schema: z.literal('suborned.mission.routes/1'),
  mission: id,
  note: z.string().min(1),
  closing: z.strictObject({ at, what: z.string().min(1), src }),
  informedSpareS: z.number().int().positive().describe('§09: the fully informed player finishes this long before the closing event'),
  contacts: z.array(z.strictObject({ role: id, who: z.string().min(1) })).min(1),
  fails: z.array(z.strictObject({ id, gdd: z.string().min(1), codes: z.array(id).min(1) })).min(1),
  scripts: z.array(RouteScript).min(1),
})
  .refine(r => new Set(r.scripts.map(s => s.id)).size === r.scripts.length, { path: ['scripts'], error: 'script ids are unique' })
  .refine(r => r.scripts.every(s => !s.reaches || r.fails.some(f => f.id === s.reaches && f.codes.includes(s.expects.fail!))), { path: ['scripts'], error: 'a fail script reaches a listed fail by one of its codes' })
  .refine(r => [...new Set(r.scripts.filter(s => s.kind === 'quiet').map(s => s.route))].every(rt => r.scripts.filter(s => s.kind === 'quiet' && s.route === rt && s.primary).length === 1), { path: ['scripts'], error: 'each quiet route has one primary script' })
  .refine(r => r.scripts.filter(s => s.kind === 'loud' && s.primary).length === 1, { path: ['scripts'], error: 'one primary loud script' })
  .refine(r => r.scripts.every(s => !(s.kind === 'quiet' && s.primary) || !!s.closes), { path: ['scripts'], error: 'a primary quiet route names its closing event' });
export type Routes = z.infer<typeof Routes>;
export type RouteScript = z.infer<typeof RouteScript>;
