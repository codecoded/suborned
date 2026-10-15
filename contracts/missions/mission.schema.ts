// suborned.mission/3: one mission's layer over the world, contracts/missions/<id>/mission.json. The world holds none of it.
// /1 = id, note, destinations and crew (block frame: contracts/world/civic-block.json frame.toPlan) and routes (plan node ids),
// read by the mission's overlay on the plan checker (tools/missions/<id>/overlay.ts: C-3, M-1, walk origins).
// /2 adds the schedule, beside /1's keys and without changing them: status, clock, timeline, events, stands.
// /3 (2026-10-15, brief 010 D) adds `roles`: the people the mission needs, named by role (employer, position or role, the mission
// point they stand at, the shift window), never by person (GDD §01 systems > scripts; brief 010 Fixed). The overlay casts each
// role from the generated people (rule M-2, tools/missions/handover/cast.ts) and the people checker reads the same list (P-6).
// Migration /2 → /3: set `schema` to 'suborned.mission/3' and add `roles` (may be empty) between `stands` and `destinations`
// (the Handover's places lock hashes the file from "destinations" on); no /2 key changes. `pin` stays unset until a cast is
// accepted (brief 010 C); a set pin must still fit its role or M-2 fails. Later on /3 (2026-10-15, brief 010 follow-up; Motive §3,
// Sky Q4), optional and additive: a role's `need`, `approach` and `regularOf`, which M-2 checks against people.rules.json. Later
// still (2026-10-15, Sky's clock ruling and Suture's review of r013), optional and additive: `clock.ruling` (the start is settled;
// `contradiction` is then gone), a role's window `shift` read as "there when the mission needs them" (in place at its point, or on
// shift without one), the playability conditions M-2 reads (`tiedTo`, `leavesTo`, `waitsFor`, `alone`; tools/missions/handover/
// cast.ts) and `windows` (a route's timed window restated against the world, with the GDD's figure and the reason). Later still
// (2026-10-15, brief 011 B; Suture O-6): `points` and `cameras`, the mission's own places the engine plays on (the Tech's cabinet,
// the Listen spot, the rivals' and the response unit's ways in, the patrol's sweep, the pickup camera's blocking spot, the two
// cameras), each with its reason; tools/sim/site.ts reads them into sim/data/handover.site.json. Then (2026-10-15, engine 0.4.0):
// the final fight's posts join `points` (ambush-*, police-*, guard-ally), `arms` carries §07's Arms 3, and `prep` restates §07's
// route prep times against the rulebook (Watch 20 s).
// A recorded run's pinned block carries its own /1 layer and is not read through this schema.
// Times: every `at` is a mission-clock offset m:ss (GDD §01 units). The one day time is clock.startsAt; a day time is
// startsAt + offset (1 mission s = 1 day s, life.clock.scale), never stored. Every entry cites its source in `src`;
// `note` records where the entry differs from the data it came from. Zod, because the file is written by hand and by
// generators and must fail loudly: Mission.parse(json). Test: tools/missions/handover/mission.test.ts.
import { z } from 'zod';
import { APPROACHES } from '../people/people.rules.schema';

/** Seconds of a mission-clock offset: "5:30" → 330. */
export const offsetS = (at: string) => { const [m, s] = at.split(':').map(Number); return m! * 60 + s!; };

const id = z.string().regex(/^[a-z][a-z0-9-]*$/);
const at = z.string().regex(/^\d{1,2}:[0-5]\d$/).describe('mission-clock offset m:ss');
const dayTime = z.string().regex(/^([01]\d|2[0-3]):[0-5]\d(:[0-5]\d)?$/).describe('day clock HH:MM or HH:MM:SS');
const src = z.string().min(1).describe('where the entry comes from: GDD §, plan, [S] starting value');
const note = z.string().min(1).optional();
/** A named point in the block frame [x, y] on a walk surface. */
/** A generated person's id (contracts/people/people.schema.ts). */
const personId = z.string().regex(/^p-[a-z0-9-]+-\d{2,3}$/);
/** A role the mission casts from the city's people: exactly one of position (a unit's position) and role (a civic employer's role). */
const role = z.strictObject({
  id, what: z.string().min(1), employer: id, position: id.optional(), role: id.optional(),
  at: z.string().min(1).optional().describe('the destination the person stands at, at their work place, at shift.from; absent = anywhere at work'),
  near: z.number().positive().optional().describe('m from `at` a door place may stand (a bay place holds `at` in its polygon); default 4 [S]'),
  shift: z.strictObject({ from: at, to: at }).describe('the mission-clock window the mission needs them: in place at `at` throughout, or on shift without `at`'),
  pin: personId.optional().describe('the person cast, once a cast is accepted; unset = first who fits by id'),
  need: id.optional().describe('the one need the person must have (people.rules.json needs.list; unknown fails M-2)'),
  approach: z.enum(APPROACHES).optional().describe('the planned approach: the person\'s temperament must not refuse it, and it must act on `need`'),
  regularOf: id.optional().describe('an employer the person is a regular of: a regular tie to its staff'),
  tiedTo: z.array(z.strictObject({ kind: id, role: id, cast: z.boolean().optional() })).optional()
    .describe('M-2 play: the person holds a tie of `kind` to someone who fits `role` in this role\'s window, or (`cast`) to the one cast in it'),
  leavesTo: z.strictObject({ at: z.string().min(1), by: at }).optional()
    .describe('M-2 play: after the window, the routine\'s next stop off the work place stands at destination `at`, begun by `by`'),
  waitsFor: id.optional().describe('M-2 play: a role with `leavesTo`; this person is still in place when its person arrives'),
  alone: z.boolean().optional().describe('M-2 play: at most one person of this employer and position or role in place at `at` at any moment of the window'),
  src, note,
}).refine(r => (r.position === undefined) !== (r.role === undefined), { error: 'exactly one of position and role' })
  .refine(r => offsetS(r.shift.from) <= offsetS(r.shift.to), { path: ['shift'], error: 'shift.from is not after shift.to' })
  .refine(r => !r.waitsFor || !!r.at, { path: ['waitsFor'], error: 'a role that waits stands at a point' })
  .refine(r => !r.alone || !!r.at, { path: ['alone'], error: 'a role alone at a point names the point' });

/** A place the engine plays on that the mission owns (block frame, m): at a point, or at the centre of a civic-block structure. */
const sitePoint = z.strictObject({
  id, at: z.tuple([z.number(), z.number()]).optional(), structure: id.optional().describe('civic-block.json structures id: the point is its centre'),
  src, reason: z.string().min(1),
}).refine(p => (p.at === undefined) !== (p.structure === undefined), { error: 'exactly one of at and structure' });
/** A camera (§05 fixed visible cones): mount, the destination it looks at, reach and cone half-width at the reach, m. */
const camera = z.strictObject({ id, at: z.tuple([z.number(), z.number()]), looksAt: z.string().min(1), reachM: z.number().positive(), halfM: z.number().positive(), src, reason: z.string().min(1) });

/** A route's timed window, restated against the world: the GDD's figure, the measured walk, and the result. */
const window = z.strictObject({
  id, route: z.string().min(1), what: z.string().min(1),
  role: id.describe('the role the window moves (its `shift` is the entry window)'),
  walk: z.strictObject({ from: z.string().min(1), to: z.string().min(1), m: z.number().positive(), speed: z.number().positive(), s: z.number().int().positive() })
    .describe('the walk on the people walk graph between two destinations, m at life.json walkSpeed m/s, s rounded'),
  stayS: z.number().int().nonnegative(), windowS: z.number().int().positive().describe('walk out + stay + walk back'),
  gdd: z.string().min(1).describe('the GDD\'s figure, verbatim'),
  src, note: z.string().min(1).describe('why the window moved'),
}).refine(w => w.walk.s === Math.round(w.walk.m / w.walk.speed), { path: ['walk', 's'], error: 'walk.s is walk.m / walk.speed, rounded' })
  .refine(w => w.windowS === 2 * w.walk.s + w.stayS, { path: ['windowS'], error: 'windowS is the walk out, the stay and the walk back' });

const point = z.strictObject({ name: z.string().min(1), at: z.tuple([z.number(), z.number()]), surface: z.string().min(1) });

export const Mission = z.strictObject({
  schema: z.literal('suborned.mission/3'),
  id,
  note: z.string().min(1),
  /** preserved = carried over as found, not yet sited or timed against the current world; `adjustable` = what Sky may still change. */
  status: z.strictObject({ state: z.literal('preserved'), adjustable: z.array(z.enum(['timings', 'placements'])), note: z.string().min(1) }),
  clock: z.strictObject({
    unit: z.literal('m:ss'), scale: z.string().min(1),
    startsAt: dayTime, provisional: z.boolean(), // provisional: the start is not settled; nothing may depend on the day time yet
    band: z.enum(['night', 'early', 'morning', 'afternoon', 'peak', 'evening', 'late']), // life.clock.bands
    src,
    transit: z.string().min(1).optional(), // what the start time does to the timetable
    /** The start, once ruled: who ruled, when, and what it rests on. */
    ruling: z.strictObject({ by: z.string().min(1), on: z.string().regex(/^\d{4}-\d{2}-\d{2}$/), what: z.string().min(1), was: dayTime }).optional(),
    /** Sources that disagree on a day time: each reading with the fields it rests on, and who decides. */
    contradiction: z.strictObject({
      what: z.string().min(1), decides: z.string().min(1),
      readings: z.array(z.strictObject({ startsAt: dayTime, chime: dayTime, from: z.array(z.string().min(1)).min(1) })).min(2),
    }).optional(),
  }),
  /** The scheduled changes a player can see coming, in order; the last one closes the mission. */
  timeline: z.array(z.strictObject({ at, what: z.string().min(1), src, note })).min(1),
  /** Crowd events the mission starts (the world's own recurring ones are life.events.list): `surge` × the area's footfall for `windowS` from `at`. */
  events: z.array(z.strictObject({ id, area: id, at, windowS: z.number().positive(), surge: z.number().positive(), who: z.string().min(1), src, note })),
  /** Vehicles standing for the mission. */
  stands: z.array(z.strictObject({ what: z.string().min(1), where: z.string().min(1), src, note })),
  /** The people the mission needs, by role (/3). */
  roles: z.array(role),
  /** Route windows restated against the world (/3, later). */
  windows: z.array(window).optional(),
  /** The cell's issue of arms (GDD §07 "Arms 3"): read into the site. */
  arms: z.strictObject({ sidearms: z.number().int().nonnegative(), src, note }).optional(),
  /** §07's route prep times restated against the rulebook (the Scout's Watch and Listen): `from` names the rulebook values summed. */
  prep: z.array(z.strictObject({ route: z.string().min(1), what: z.string().min(1), at, from: z.array(z.string().min(1)).min(1), gdd: z.string().min(1), src, note: z.string().min(1) })).optional(),
  /** The engine's places and cameras that are mission content (/3, later; Suture O-6). */
  points: z.array(sitePoint).optional(),
  cameras: z.array(camera).optional(),
  destinations: z.array(point).min(1),
  crew: z.array(point),
  /** Road routes over the plan graph: from a node to the nearest of `to`. */
  routes: z.array(z.strictObject({ id, name: z.string().min(1), from: z.string().min(1), to: z.array(z.string().min(1)).min(1), toName: z.string().min(1).optional() })),
})
  .refine(m => m.timeline.every((e, i, a) => i === 0 || offsetS(a[i - 1]!.at) <= offsetS(e.at)), { path: ['timeline'], error: 'timeline offsets never go back' })
  .refine(m => m.events.every(e => m.timeline.some(t => t.at === e.at)), { path: ['events'], error: 'every event starts on a timeline entry' })
  .refine(m => m.roles.every(r => !r.at || m.destinations.some(d => d.name === r.at)), { path: ['roles'], error: 'a role stands at a destination the mission names' })
  .refine(m => new Set(m.roles.map(r => r.id)).size === m.roles.length, { path: ['roles'], error: 'role ids are unique' })
  .refine(m => { const p = m.roles.flatMap(r => r.pin ?? []); return new Set(p).size === p.length; }, { path: ['roles'], error: 'one person fills one role' })
  .refine(m => m.roles.every(r => (r.tiedTo ?? []).every(t => t.role !== r.id && m.roles.some(x => x.id === t.role))), { path: ['roles'], error: 'a tie names another role of the mission' })
  .refine(m => m.roles.every(r => !r.waitsFor || !!m.roles.find(x => x.id === r.waitsFor)?.leavesTo), { path: ['roles'], error: 'waitsFor names a role with leavesTo' })
  .refine(m => m.roles.every(r => !r.leavesTo || m.destinations.some(d => d.name === r.leavesTo!.at)), { path: ['roles'], error: 'leavesTo names a destination' })
  .refine(m => (m.windows ?? []).every(w => m.roles.some(r => r.id === w.role) && [w.walk.from, w.walk.to].every(n => m.destinations.some(d => d.name === n))), { path: ['windows'], error: 'a window names a role and two destinations of the mission' })
  .refine(m => !m.clock.ruling || (!m.clock.provisional && !m.clock.contradiction), { path: ['clock'], error: 'a ruled start is not provisional and holds no contradiction' })
  .refine(m => { const ids = [...(m.points ?? []), ...(m.cameras ?? [])].map(p => p.id); return new Set(ids).size === ids.length; }, { path: ['points'], error: 'point and camera ids are unique' })
  .refine(m => (m.cameras ?? []).every(c => m.destinations.some(d => d.name === c.looksAt)), { path: ['cameras'], error: 'a camera looks at a destination' });
export type Mission = z.infer<typeof Mission>;
