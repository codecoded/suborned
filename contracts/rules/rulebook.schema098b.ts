// Brief 098, law L9 Bodies (engine 0.46.0, Keel; Sky 2026-11-07: "ensuring NPC's don't overlap each, they have real boundaries"; give and take,
// engine 0.47.0, Sky 2026-11-07: "Do all but the jumping back to spot should be better to dealt with"): the
// rulebook's `bodies`, placed by rulebook.schema.ts (that file is over the 500-line mark). Optional in the schema only so runs pinned before it
// parse; the engine requires it and rulebook098b.test.ts holds the live rulebook to having it.
import { z } from 'zod';

const text = z.string().min(1);
const Ref = z.string().regex(/^(v:[a-z][a-z0-9-]*|p:[a-zA-Z][a-zA-Z0-9.-]*)$/);
const grounded = { tag: z.enum(['B', 'S', 'L']), src: text, proposed: z.literal(true).optional(), reason: text.optional() };
const ground = <T extends z.ZodRawShape>(shape: T) =>
  z.strictObject({ ...shape, ...grounded }).refine((v) => { const o = v as { proposed?: true; reason?: string }; return !o.proposed || !!o.reason; }, { message: 'a proposed rule needs a reason' });

/** L9 Bodies: the switch, a body's radius, personal space at rest, a queue's step, an armed crew's spread, the pace back, the most a shove moves
 * a body in a second, and how far round its place a body looks for room; engine 0.47.0's give and take: the half-width of a body turned sideways
 * to squeeze past, how fast it squeezes, and how long a person of the city is held before changing plans (optional only for runs pinned to 0.46.0;
 * the engine requires them and rulebook098b.test.ts holds the live rulebook to having them). */
export const Bodies = ground({ on: Ref, radius: Ref, space: Ref, queue: Ref, crew: Ref, back: Ref, shove: Ref, search: Ref, squeeze: Ref.optional(), squeezePace: Ref.optional(), replan: Ref.optional(), rule: text });
