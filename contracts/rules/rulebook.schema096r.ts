// Brief 096's rivals round (engine 0.41.0, Keel; Motive briefs/096/motive/rivals.html, rivals.json; Sky 2026-11-04 "all recommended"): the
// rulebook's new shapes, placed by rulebook.schema.ts (that file is over the 500-line mark): `rivals.block` (the block on arrival),
// `rivals.lookout` (the lookout is the cell's job), `rivals.turn` (the crew's temperaments and Pay), `security.unclaimed` (the gang shooting),
// `car.target.blastStanding` (a blast near city people). Optional in the schema only so runs pinned before them parse; the engine requires them
// and rulebook096r.test.ts holds the live rulebook to having them.
import { z } from 'zod';

const text = z.string().min(1);
const Ref = z.string().regex(/^(v:[a-z][a-z0-9-]*|p:[a-zA-Z][a-zA-Z0-9.-]*)$/);
const grounded = { tag: z.enum(['B', 'S', 'L']), src: text, proposed: z.literal(true).optional(), reason: text.optional() };
const ground = <T extends z.ZodRawShape>(shape: T) =>
  z.strictObject({ ...shape, ...grounded }).refine((v) => { const o = v as { proposed?: true; reason?: string }; return !o.proposed || !!o.reason; }, { message: 'a proposed rule needs a reason' });

/** The block point's stand-off, the wheelman's step out, and (engine 0.42.0, Motive F2) a broken man's run off the map; the wheelman's gunfire
 * ring is retired by 0.42.0 (F1), optional only for runs pinned before it. */
export const RivalsBlock = ground({ standOff: Ref, wheelmanNear: Ref.optional(), wheelmanOut: Ref, brokenPace: Ref.optional(), rule: text });
/** The lookout's run after the target's car, and how long a misled member watches. */
export const RivalsLookout = ground({ follow: Ref, misled: Ref, rule: text });
/** Pay on a member: price, time, Trail; the cap on one temperament; the kinds, in draw order. */
export const RivalsTurn = ground({ pay: Ref, s: Ref, trail: Ref, maxShare: Ref, kinds: z.tuple([z.literal('mercenary'), z.literal('loyal')]), needs: z.enum(['watched', 'none']).optional(), rule: text }); // needs: engine 0.43.0 (a Watch first)
/** The unclaimed shooting: how long the response holds the scene, and (engine 0.42.0, Motive V2 b) how near the scene it places the cell. */
export const Unclaimed = ground({ hold: Ref, scene: Ref.optional(), fired: z.enum(['placed', 'always']).optional(), rule: text }); // fired: engine 0.43.0 (Motive V3)
/** A blast near city people: the Standing step and its reach. */
export const BlastStanding = ground({ v: Ref, near: Ref, rule: text });
