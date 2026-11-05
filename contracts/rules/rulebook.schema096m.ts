// Brief 096's misses round (engine 0.43.0, Keel; Motive briefs/096/motive/misses.html, misses.json; Sky 2026-11-05 "all recommended"): the
// rulebook's new shape, placed by rulebook.schema.ts (that file is over the 500-line mark): `car.go` (G1, a Go waits for a Board under way).
// V3 (security.unclaimed.fired) and Pay's Watch first (rivals.turn.needs) live on the 0.41.0 shapes (rulebook.schema096r.ts). Optional in the
// schema only so runs pinned before 0.43.0 parse; the engine requires them and rulebook096m.test.ts holds the live rulebook to having them.
import { z } from 'zod';

const text = z.string().min(1);
const grounded = { tag: z.enum(['B', 'S', 'L']), src: text, proposed: z.literal(true).optional(), reason: text.optional() };
const ground = <T extends z.ZodRawShape>(shape: T) =>
  z.strictObject({ ...shape, ...grounded }).refine((v) => { const o = v as { proposed?: true; reason?: string }; return !o.proposed || !!o.reason; }, { message: 'a proposed rule needs a reason' });

/** G1: a Go given while an operative climbs into the car waits for that Board ('board'); 'none' is 0.42.0's Go, at once. */
export const CarGo = ground({ waits: z.enum(['board', 'none']), rule: text });
