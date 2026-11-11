// Brief 110 (engine 0.55.0, Keel; Sky 2026-11-11: "I think we need to reduce the time things take to happen"): the rulebook's `pace`, one dial on the
// time of every act the cell orders, placed by rulebook.schema.ts (that file is over the 500-line mark). Optional in the schema only so runs pinned
// before it parse; rulebook110.test.ts holds the live rulebook to having it. The engine reads it in sim/Suborned.Sim/RulesPace.cs.
import { z } from 'zod';

const text = z.string().min(1);
const Ref = z.string().regex(/^(v:[a-z][a-z0-9-]*|p:[a-zA-Z][a-zA-Z0-9.-]*)$/);
/** An act's seconds: a v:…-s value, or an approach's time in people.rules.json. */
const ActRef = z.string().regex(/^(v:[a-z][a-z0-9-]*-s|p:approaches\.[a-z]+\.timeS)$/);
const grounded = { tag: z.enum(['B', 'S', 'L']), src: text, proposed: z.literal(true).optional(), reason: text.optional() };

/** pace: `act` (a pct value, 10 to 100) scales every act `acts` names, no act twice. */
export const Pace = z.strictObject({ act: Ref, acts: z.array(ActRef).min(1).refine((a) => new Set(a).size === a.length, { message: 'pace.acts names an act twice' }), rule: text, ...grounded })
  .refine((v) => !v.proposed || !!v.reason, { message: 'a proposed rule needs a reason' });
