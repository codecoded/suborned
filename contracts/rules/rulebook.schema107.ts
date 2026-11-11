// Brief 107 (engine 0.54.0, Keel; Sky 2026-11-11: "Yes to all three"): the rulebook's `look`, the weather and light dial, placed by
// rulebook.schema.ts (that file is over the 500-line mark). A look only: no rule reads it, so `readBy` must stay empty (the engine rejects a reader
// too). Optional in the schema only so runs pinned before it parse; the engine requires it and rulebook107.test.ts holds the live rulebook to having it.
import { z } from 'zod';

const text = z.string().min(1);
const Ref = z.string().regex(/^(v:[a-z][a-z0-9-]*|p:[a-zA-Z][a-zA-Z0-9.-]*)$/);
const grounded = { tag: z.enum(['B', 'S', 'L']), src: text, proposed: z.literal(true).optional(), reason: text.optional() };
const ground = <T extends z.ZodRawShape>(shape: T) =>
  z.strictObject({ ...shape, ...grounded }).refine((v) => { const o = v as { proposed?: true; reason?: string }; return !o.proposed || !!o.reason; }, { message: 'a proposed rule needs a reason' });

/** The kinds, in the dial's order: the first is the neutral start's (seed 0). The same lists as the engine's LookRules (sim GameLook.cs). */
export const WEATHERS = ['clear', 'overcast', 'rain', 'mist'] as const;
export const LIGHTS = ['day', 'night'] as const;
/** look: each kind's weight (a v: ref), every kind named, no other; readBy is empty while the look is a look only. */
export const Look = ground({
  weather: z.strictObject(Object.fromEntries(WEATHERS.map((k) => [k, Ref])) as Record<(typeof WEATHERS)[number], typeof Ref>),
  light: z.strictObject(Object.fromEntries(LIGHTS.map((k) => [k, Ref])) as Record<(typeof LIGHTS)[number], typeof Ref>),
  readBy: z.array(text).max(0, 'no rule reads the look yet (brief 107): a reader is a design change for Sky'),
  rule: text,
});
