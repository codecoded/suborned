// Brief 096's lend round (engine 0.42.0, Keel; Motive briefs/096/motive/lend.html, lend.json; Sky 2026-11-05 "build the lend now"): the
// rulebook's new shapes, placed by rulebook.schema.ts (that file is over the 500-line mark): `car.lend` (the turned driver's car as a second
// cell car, two Go's, the police clock after vale.out; `split` is the switch). Optional in the schema only so runs pinned before it parse;
// the engine requires it and rulebook096l.test.ts holds the live rulebook to having it. security.unclaimed.scene and rivals.block.brokenPace
// live on the 0.41.0 shapes (rulebook.schema096r.ts), optional there for the same reason.
import { z } from 'zod';

const text = z.string().min(1);
const Ref = z.string().regex(/^(v:[a-z][a-z0-9-]*|p:[a-zA-Z][a-zA-Z0-9.-]*)$/);
const grounded = { tag: z.enum(['B', 'S', 'L']), src: text, proposed: z.literal(true).optional(), reason: text.optional() };
const ground = <T extends z.ZodRawShape>(shape: T) =>
  z.strictObject({ ...shape, ...grounded }).refine((v) => { const o = v as { proposed?: true; reason?: string }; return !o.proposed || !!o.reason; }, { message: 'a proposed rule needs a reason' });

/** The lend: the switch, the lent car's seats, the police clock after vale.out, and the engine's refusal codes. */
export const CarLend = ground({
  split: Ref, seats: Ref, police: Ref,
  refuse: z.strictObject({ board: z.literal('board-lent-full'), drive: z.literal('drive-lent'), gone: z.literal('car-left') }),
  rule: text,
});
