// Brief 098 step 0 (Keel): the dial's rules, contracts/rules/population.json (suborned.rules.population/1), as the engine reads them; step 1, the cars kind and traffic (law L8);
// step 1b, jams (ordinary traffic boxes, a wreck is shoved aside) and the people kind counted on the street
// (sim/Suborned.Sim/Population.cs PopulationRules: unknown = reject). Read by the engine only when a start sets counts (start.counts), so
// the rulebook keeps its own bytes and no recorded run moves.
import { z } from 'zod';

const text = z.string().min(1);
const why = { src: text, proposed: z.literal(true).optional(), reason: text.optional() };
const band = z.tuple([z.number().int(), z.number().int()]).refine(([a, b]) => b >= a, { message: 'a band is [min, max]' });
const proposed = (v: { proposed?: true; reason?: string }) => !v.proposed || !!v.reason;

export const DialKind = z.strictObject({
  of: z.union([z.array(text).min(1), z.literal('rest')]), role: text, job: text,
  id: text.refine((s) => !s.startsWith('p-'), { message: "an added person's id prefix is not the people file's (p-)" }),
  name: text, max: z.number().int().min(0).max(100000),
  /** Brief 098 step 1b (Sky 2026-11-07: "people counter is for street"): what the count counts: everyone of the kind in the zone, or people on the street at a time on average. */
  measure: z.enum(['zone', 'street']).optional(), ...why,
}).refine(proposed, { message: 'a proposed rule needs a reason' });

/** Brief 098 step 1 (law L8): the cars kind (no people kind: no role or job; its id is the slot ids' prefix) and the traffic's rules. */
export const CarsKind = z.strictObject({ id: text.refine((s) => !s.includes(':'), { message: "a car's id prefix has no colon (a car is car:<prefix><slot>-<trip>)" }), max: z.number().int().min(0).max(1000), ...why }).refine(proposed, { message: 'a proposed rule needs a reason' });
const n = (min: number, max = Number.MAX_SAFE_INTEGER) => z.number().int().min(min).max(max);
const temperament = z.enum(['idealist', 'mercenary', 'fearful', 'loyal']);
export const Traffic = z.strictObject({
  lanes: text, mix: z.record(text, n(0)), size: z.record(text, z.tuple([n(50, 2000), n(30, 400)])), cover: z.array(text), ...why,
  drive: z.strictObject({ accelCmS2: n(10), brakeCmS2: n(10), hardBrakeCmS2: n(10), gapCm: n(50), headwayDs: n(0), lookCm: n(500, 10000), clearCm: n(0, 200), peopleGapCm: n(50), ...why }).refine(proposed),
  trips: z.strictObject({ offS: band, warmS: n(0, 600), ...why }).refine(proposed),
  stand: z.strictObject({ standS: n(0), settleS: n(0), ...why }).refine(proposed).refine((x) => x.settleS >= x.standS, { message: 'settleS no less than standS' }),
  pass: z.strictObject({ waitS: n(0), latCmS: n(1), passCmS: n(10), backCm: n(0, 2000), ...why }).refine(proposed),
  yield: z.strictObject({ aheadS: n(0, 30), entryS: n(0, 60), ...why }).refine(proposed),
  /** Brief 098 step 1b (Sky 2026-11-07: "shot cars can get past, others block"): how a mission car pushes a wreck in its way aside. */
  shove: z.strictObject({ cmS: n(10, 1000), maxCm: n(50, 2000), ...why }).refine(proposed),
  react: z.strictObject({ duck: z.array(temperament), flee: z.array(temperament), fleePm: n(1000, 4000), ...why }).refine(proposed).refine((x) => !x.duck.some((t) => x.flee.includes(t)), { message: 'a nature ducks or flees, never both' }),
  lockdown: z.strictObject({ alert: n(1, 3), then: z.enum(['leave', 'stop']), ...why }).refine(proposed),
  driver: z.strictObject({ role: text, job: text, id: text.refine((s) => !s.startsWith('p-'), { message: "a driver's id prefix is not the people file's (p-)" }), name: text, ...why }).refine(proposed),
}).refine(proposed).refine((t) => Object.keys(t.mix).every((k) => k in t.size) && Object.keys(t.size).every((k) => k in t.mix), { message: 'every kind in the mix has a size, and every size a kind' })
  .refine((t) => t.cover.every((k) => k in t.mix), { message: 'cover names kinds of the mix' });

export const PopulationRules = z.strictObject({
  schema: z.literal('suborned.rules.population/1'), author: text, gdd: text, rule: text,
  kinds: z.strictObject({ guards: DialKind, people: DialKind, cars: CarsKind }),
  traffic: Traffic,
  fewer: z.strictObject({ salt: text, ...why }).refine(proposed),
  beat: z.strictObject({
    points: z.array(z.enum(['door', 'post', 'call-box'])).min(1), size: z.number().int().min(1).max(8), pauseS: band,
    doorOutCm: z.number().int().min(0).max(1000), shiftBeforeS: z.number().int().min(0), shiftAfterS: z.number().int().min(0), ...why,
  }).refine(proposed),
  passers: z.strictObject({
    enterS: band, dest: z.strictObject({ seat: z.number().int().min(0), door: z.number().int().min(0), edge: z.number().int().min(1) }),
    lingerS: z.strictObject({ seat: band, door: band }), edgeMinCells: z.number().int().min(1), edgeSpanCells: z.number().int().min(1), ...why,
  }).refine(proposed).refine((p) => p.edgeSpanCells >= p.edgeMinCells, { message: 'edgeSpanCells no less than edgeMinCells' }),
}).refine((r) => [r.kinds.guards, r.kinds.people].filter((k) => k.of === 'rest').length === 1, { message: 'exactly one kind is "rest"' });
export type PopulationRules = z.infer<typeof PopulationRules>;
