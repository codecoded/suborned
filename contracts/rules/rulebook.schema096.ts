// Brief 096 (engine 0.40.0, Keel; Motive's rules page, Sky 2026-11-04 "all. recommended"): the rulebook's getaway rules, as zod shapes that
// rulebook.schema.ts places (that file is over the 500-line mark): `camera` (bribes on camera), `car.box` (boxed in), `car.kerb` (the kerbside
// stop), `attention.parked` (the parked car noticed). Optional in the schema only so runs pinned before them parse; the engine requires them
// and rulebook.test.ts holds the live rulebook to having them. The grounding (tag, src, proposed needs a reason) is the schema's own.
import { z } from 'zod';

const text = z.string().min(1);
const Ref = z.string().regex(/^(v:[a-z][a-z0-9-]*|p:[a-zA-Z][a-zA-Z0-9.-]*)$/);
const grounded = { tag: z.enum(['B', 'S', 'L']), src: text, proposed: z.literal(true).optional(), reason: text.optional() };
const ground = <T extends z.ZodRawShape>(shape: T) =>
  z.strictObject({ ...shape, ...grounded }).refine((v) => { const o = v as { proposed?: true; reason?: string }; return !o.proposed || !!o.reason; }, { message: 'a proposed rule needs a reason' });

/** The approaches a live camera records, its Trail and Alert, the desk's scan and call, the lead (place-time and subject always; the Fixer's face). */
export const Camera = ground({
  approaches: z.array(z.enum(['ask', 'pay', 'help', 'pressure'])).nonempty(),
  window: z.literal('any-second'),
  trail: Ref, alert: Ref,
  report: z.strictObject({ scan: Ref, call: Ref }),
  standing: z.literal(0),
  lead: z.array(z.enum(['place-time', 'subject', 'face:fixer'])).nonempty(),
  rule: text,
});
/** Boxed in: the way out's keep-off each side, and the refusal codes the engine gives. */
export const CarBox = ground({ side: Ref, refuse: z.strictObject({ go: z.literal('car-boxed'), drive: z.literal('drive-boxed'), into: z.literal('stand-boxed') }), rule: text });
/** The kerbside stop: the clearance all round and the footprint's edge off the kerb line. */
export const CarKerb = ground({ clear: Ref, edge: Ref, rule: text });
/** The parked car noticed: what is private ground, and the height over the car's centre it is seen at. */
export const AttnParked = ground({ ground: z.array(z.enum(['drive', 'official', 'service'])).nonempty(), sight: Ref, rule: text });
