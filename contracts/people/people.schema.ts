// suborned.people/1: the expanded people (brief 010 B), written by `bun run people` to out/people/<plan id>.people.json.
// Individuals made by code from the cast tables, the plan, the civic block and life.json, seeded by plot and unit id.
// Times are minutes of the day clock (0..1440); a stop is where the person stands from `from` to `to`, the gaps are walking.
// Background people carry only a routine (rules `tiers`): the file lists them as counts per home plot, never as persons.
import { z } from 'zod';

const id = z.string().regex(/^[a-z][a-z0-9-]*$/);
const minute = z.number().int().min(0).max(1440);
const v2 = z.array(z.number()).length(2);

export const Place = z.object({
  id, kind: z.enum(['door', 'bay', 'stop', 'seat', 'stand', 'home']).describe('seat: a chair of a café set the world draws; stand: a standing point beside the café that keeps every keep-clear zone clear (tools/people/seats.ts)'), name: z.string().min(1), at: v2.describe('plan frame, metres'),
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
  post: z.enum(['door', 'inside', 'bay', 'official']), place: id.describe('the place the person stands at work'),
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
  position: id.describe('the person\'s position in their unit, or their role at a civic employer; "resident" for a resident (brief 010, keel-rulebook.md point 8)'),
  needWeights: z.record(id, z.number().int().positive()).describe('the needs the person\'s own cast row offers their position, as integer per mille (largest remainder, sum 1000): the weights the rulebook draws a replacement need from (Sky Q2). `need` is one of them'),
});
/** Rules knowledge (P5), derived from the people at the start: what a person knows besides their own day. */
export const Knowledge = z.object({
  cellAtStart: z.literal('nothing'), identity: z.array(z.string()).describe('how the cell is known: "face" only'),
  needsKnown: z.array(z.object({ keeper: z.string(), knows: z.array(z.string()) })).describe('a keeper knows the need of each person listed (workmates and unit regulars)'),
});
export const People = z.object({
  schema: z.literal('suborned.people/1'),
  plan: z.object({ id: z.string(), version: z.string() }), scope: id,
  inputs: z.object({ cast: z.string(), life: z.string(), block: z.string(), rules: z.string(), mission: z.string(), plan: z.string(), tools: z.string().optional() })
    .describe('sha256 of each input: the cast (of its parsed JSON), life.json, the civic block, the people rules, the mission and the plan text. `tools` (written by `bun run people`): the people tools hash (tools/levels/people/hash.ts), so a file made by other code is known. A reader (M-2, the Unity people layer) refuses a file whose hashes are not the current inputs\''),
  places: z.array(Place),
  units: z.array(z.object({ id, plot: id, area: id, trade: id, employer: id, name: z.string(), staffPerShift: z.number().int(), hours: v2, shifts: z.array(z.object({ from: minute, to: minute })) })),
  persons: z.array(Person),
  knowledge: Knowledge.optional().describe('derived from the rules and the ties (tools/people/knowledge.ts)'),
  background: z.array(z.object({ plot: id, area: id, residents: z.number().int(), simulated: z.number().int() })),
});
export type Knowledge = z.infer<typeof Knowledge>;
export type Place = z.infer<typeof Place>;
export type Stop = z.infer<typeof Stop>;
export type Person = z.infer<typeof Person>;
export type People = z.infer<typeof People>;
