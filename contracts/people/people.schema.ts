// suborned.people/1: the expanded people (brief 010 B), written by `bun run people` to out/people/<plan id>.people.json.
// Individuals made by code from the cast tables, the plan, the civic block and life.json, seeded by plot and unit id.
// Times are minutes of the day clock (0..1440); a stop is where the person stands from `from` to `to`, the gaps are walking.
// Background people carry only a routine (rules `tiers`): the file lists them as counts per home plot, never as persons.
import { z } from 'zod';

const id = z.string().regex(/^[a-z][a-z0-9-]*$/);
const minute = z.number().int().min(0).max(1440);
const v2 = z.array(z.number()).length(2);

export const Place = z.object({
  id, kind: z.enum(['door', 'bay', 'stop', 'seat', 'home']), name: z.string().min(1), at: v2.describe('plan frame, metres'),
  area: id, plot: id.optional(),
});
export const Stop = z.object({
  place: z.union([id, z.literal('beyond')]).describe('"beyond" = off the sheet (lives beyond it, between the tram and home)'),
  doing: z.enum(['home', 'commute', 'work', 'meal', 'pre-shift', 'evening', 'errand']), from: minute, to: minute,
});
export const Work = z.object({
  unit: id, plot: id, area: id, trade: id, employer: id,
  position: id.optional().describe('the unit category\'s position (keeper, server, ...); exactly one of position and role'),
  role: id.optional().describe('a role of the civic employer (guard, official-driver, ...)'),
  post: z.enum(['door', 'inside', 'bay']), place: id.describe('the place the person stands at work'),
  shift: z.object({ from: minute, to: minute }).describe('to < from: the shift runs past midnight'),
});
export const Person = z.object({
  id: z.string().regex(/^p-[a-z0-9-]+-\d{2,3}$/), name: z.string().min(3),
  tier: z.literal('simulated'),
  home: z.object({ plot: id, place: z.union([id, z.literal('beyond')]) }), work: Work.nullable(),
  routine: z.object({ variant: id, stops: z.array(Stop).min(1) }),
  temperament: z.string(), need: z.string(),
  institution: z.string().nullable().describe('a Loyal person\'s institution: their employer, or "civic" with no work; null for the others'),
  start: z.object({ bravery: z.number(), resentment: z.number(), fear: z.number(), loyalty: z.number() }),
  memory: z.tuple([]).describe('empty at the start (rules person.memoryAtStart)'),
  ties: z.array(z.object({ kind: z.string(), with: z.string() })).describe('two-way ties stand on both people; a directed tie stands on its source'),
  look: z.object({ block: z.string().describe('job colour block key, e.g. job/food') }),
});
export const People = z.object({
  schema: z.literal('suborned.people/1'),
  plan: z.object({ id: z.string(), version: z.string() }), scope: id,
  inputs: z.object({ cast: z.string(), life: z.string(), block: z.string(), rules: z.string() }).describe('sha256 of each input'),
  places: z.array(Place),
  units: z.array(z.object({ id, plot: id, area: id, trade: id, employer: id, name: z.string(), staffPerShift: z.number().int(), hours: v2, shifts: z.array(z.object({ from: minute, to: minute })) })),
  persons: z.array(Person),
  background: z.array(z.object({ plot: id, area: id, residents: z.number().int(), simulated: z.number().int() })),
});
export type Place = z.infer<typeof Place>;
export type Stop = z.infer<typeof Stop>;
export type Person = z.infer<typeof Person>;
export type People = z.infer<typeof People>;
