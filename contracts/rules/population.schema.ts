// Brief 098 step 0 (Keel): the dial's rules, contracts/rules/population.json (suborned.rules.population/1), as the engine reads them
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
  name: text, max: z.number().int().min(0).max(100000), ...why,
}).refine(proposed, { message: 'a proposed rule needs a reason' });

export const PopulationRules = z.strictObject({
  schema: z.literal('suborned.rules.population/1'), author: text, gdd: text, rule: text,
  kinds: z.strictObject({ guards: DialKind, people: DialKind }),
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
