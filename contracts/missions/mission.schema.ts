// suborned.mission/3: one mission's layer over the world, contracts/missions/<id>/mission.json. The world holds none of it.
// /1 = id, note, destinations and crew (block frame: contracts/world/civic-block.json frame.toPlan) and routes (plan node ids),
// read by the mission's overlay on the plan checker (tools/missions/<id>/overlay.ts: C-3, M-1, walk origins).
// /2 adds the schedule, beside /1's keys and without changing them: status, clock, timeline, events, stands.
// /3 (2026-10-15, brief 010 D) adds `roles`: the people the mission needs, named by role (employer, position or role, the mission
// point they stand at, the shift window), never by person (GDD §01 systems > scripts; brief 010 Fixed). The overlay casts each
// role from the generated people (rule M-2, tools/missions/handover/cast.ts) and the people checker reads the same list (P-6).
// Migration /2 → /3: set `schema` to 'suborned.mission/3' and add `roles` (may be empty) between `stands` and `destinations`
// (the Handover's places lock hashes the file from "destinations" on); no /2 key changes. `pin` stays unset until a cast is
// accepted (brief 010 C); a set pin must still fit its role or M-2 fails. A recorded run's pinned block carries its own /1 layer
// and is not read through this schema.
// Times: every `at` is a mission-clock offset m:ss (GDD §01 units). The one day time is clock.startsAt; a day time is
// startsAt + offset (1 mission s = 1 day s, life.clock.scale), never stored. Every entry cites its source in `src`;
// `note` records where the entry differs from the data it came from. Zod, because the file is written by hand and by
// generators and must fail loudly: Mission.parse(json). Test: tools/missions/handover/mission.test.ts.
import { z } from 'zod';

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
  shift: z.strictObject({ from: at, to: at }).describe('on shift for this mission-clock window'),
  pin: personId.optional().describe('the person cast, once a cast is accepted; unset = first who fits by id'),
  src, note,
}).refine(r => (r.position === undefined) !== (r.role === undefined), { error: 'exactly one of position and role' })
  .refine(r => offsetS(r.shift.from) <= offsetS(r.shift.to), { path: ['shift'], error: 'shift.from is not after shift.to' });

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
  destinations: z.array(point).min(1),
  crew: z.array(point),
  /** Road routes over the plan graph: from a node to the nearest of `to`. */
  routes: z.array(z.strictObject({ id, name: z.string().min(1), from: z.string().min(1), to: z.array(z.string().min(1)).min(1), toName: z.string().min(1).optional() })),
})
  .refine(m => m.timeline.every((e, i, a) => i === 0 || offsetS(a[i - 1]!.at) <= offsetS(e.at)), { path: ['timeline'], error: 'timeline offsets never go back' })
  .refine(m => m.events.every(e => m.timeline.some(t => t.at === e.at)), { path: ['events'], error: 'every event starts on a timeline entry' })
  .refine(m => m.roles.every(r => !r.at || m.destinations.some(d => d.name === r.at)), { path: ['roles'], error: 'a role stands at a destination the mission names' })
  .refine(m => new Set(m.roles.map(r => r.id)).size === m.roles.length, { path: ['roles'], error: 'role ids are unique' })
  .refine(m => { const p = m.roles.flatMap(r => r.pin ?? []); return new Set(p).size === p.length; }, { path: ['roles'], error: 'one person fills one role' });
export type Mission = z.infer<typeof Mission>;
