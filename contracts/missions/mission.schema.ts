// suborned.mission/2: one mission's layer over the world, contracts/missions/<id>/mission.json. The world holds none of it.
// /1 = id, note, destinations and crew (block frame: contracts/world/civic-block.json frame.toPlan) and routes (plan node ids),
// read by the mission's overlay on the plan checker (tools/missions/<id>/overlay.ts: C-3, M-1, walk origins).
// /2 adds the schedule, beside /1's keys and without changing them: status, clock, timeline, events, stands.
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
const point = z.strictObject({ name: z.string().min(1), at: z.tuple([z.number(), z.number()]), surface: z.string().min(1) });

export const Mission = z.strictObject({
  schema: z.literal('suborned.mission/2'),
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
  destinations: z.array(point).min(1),
  crew: z.array(point),
  /** Road routes over the plan graph: from a node to the nearest of `to`. */
  routes: z.array(z.strictObject({ id, name: z.string().min(1), from: z.string().min(1), to: z.array(z.string().min(1)).min(1), toName: z.string().min(1).optional() })),
})
  .refine(m => m.timeline.every((e, i, a) => i === 0 || offsetS(a[i - 1]!.at) <= offsetS(e.at)), { path: ['timeline'], error: 'timeline offsets never go back' })
  .refine(m => m.events.every(e => m.timeline.some(t => t.at === e.at)), { path: ['events'], error: 'every event starts on a timeline entry' });
export type Mission = z.infer<typeof Mission>;
