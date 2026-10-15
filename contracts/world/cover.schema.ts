// suborned.cover/1: the cover of a block, as the engine's fight AI reads it (brief 011, Pulse; briefs/011/pulse-fight-ai.md).
// Written by tools/cover (`bun run cover`) from the civic block, the world's own civic masses (what is drawn where) and the engine's site
// (its walk grid and its fight points); held by the cover rules CV-1 and CV-2 (tools/cover/check.ts). Block frame (x south, y east), integer
// centimetres, as sim/data/handover.site.json. Nothing here is authored: a cover point is a place beside an object a body can stand, its
// facing and arc are the object's own geometry seen from that place, and its quality is a pure function of height and arc.
import { z } from 'zod';

const id = z.string().regex(/^[a-z][a-z0-9:/_.-]*$/);
/** A place of the engine's site (handover.site.json `places`): ids such as "d:Getaway bay" or "ambush-1". */
const place = z.string().min(1);
const cm = z.number().int();
const v2 = z.tuple([cm, cm]);

/** Where a cover point stands in the mission, in mission seconds: a parked car is cover only while it is there. */
const Present = z.tuple([z.number().int().min(0), z.number().int().min(1)]);

export const CoverPoint = z.object({
  id: z.string().regex(/^cv-\d{3}$/),
  at: v2.describe('where a body stands, on free ground of the site grid (body clearance kept), cm'),
  faces: v2.describe('unit vector from `at` to the object, per mille (|v| within 3 of 1000): the direction the point shields against, from where a shooter would stand'),
  arc: z.number().int().min(5).max(89).describe('half-angle, degrees, either side of `faces`: 85% of the narrower side of the object\'s own angular span seen from `at`'),
  cos: z.number().int().min(0).max(1000).describe('floor(1000 × cos(arc)), so the engine tests a shooter with integers: dot(unit(shooter − at), faces) ≥ cos'),
  h: cm.describe('height of the object, cm: 90 and up'),
  against: z.enum(['fire', 'fire+sight']).describe('fire: a body beside it is harder to hit (the cover modifier); fire+sight: the object is also at least eye height (eye-cm), so it blocks sight'),
  q: z.number().int().min(350).max(1000).describe('quality per mille: height class × arc (tools/cover/gen.ts `quality`); ≥ 600 is full cover, 350–599 partial'),
  kind: z.enum(['planter-run', 'pier', 'cabinet', 'robot', 'fountain', 'wall', 'car']),
  of: id.describe('the object it stands beside: a civic mass id, a terrace wall piece, or the car'),
  present: Present.optional().describe('mission seconds [from, to) the point exists in (vehicles); absent: always'),
});

export const FightArea = z.object({
  id: id,
  anchor: place.describe('the place both sides fight over: the getaway car'),
  hostile: z.array(place).min(1).describe('the posts the hostile side takes (site places), in the order the crew takes them'),
  cell: z.array(place).min(1).describe('the places the cell holds (the crew at the car)'),
  allies: z.array(place).describe('posts of a friendly third party (the guard who joins the safe fight)'),
  hostileCover: z.array(z.object({ post: place, points: z.array(z.string()) })).describe('per hostile post, the cover points within hostileReach of it that shield it from the anchor'),
  cellCover: z.array(z.object({ post: place, points: z.array(z.string()) })).describe('per hostile post, the cover points within cellReach of the anchor that shield the cell from a shooter at it'),
});

export const Cover = z.object({
  schema: z.literal('suborned.cover/1'),
  mission: z.literal('handover'),
  frame: z.string(),
  from: z.object({ block: z.string(), site: z.string(), kit: z.string(), plan: z.string() }).describe('sha256 of each input, so a stale file is seen'),
  rules: z.object({
    eyeCm: cm, minHeightCm: cm, minWidthCm: cm, standoffCm: cm, qFull: cm, qPartial: cm, snapCm: cm, minRangeCm: cm, hostileReachCm: cm, cellReachCm: cm,
  }).describe('the constants the generator and the checker used, each with its reason in tools/cover/gen.ts'),
  points: z.array(CoverPoint),
  blockers: z.array(z.object({ id: id, z: cm, h: cm, poly: z.array(v2).min(3) })).describe('the cover objects at least eye height (eye-cm) tall: the engine adds them to its sight occluders, as `against: fire+sight` says. They are not in the site\'s occluders today (it holds the buildings and the plots), so a fight behind the fountain or a pavilion pier is seen through'),
  refuge: z.array(z.object({ id: place, at: v2, kind: z.enum(['door', 'mouth']), src: z.string() })).describe('where the crowd runs to leave the street: entrances and the street mouths'),
  areas: z.array(FightArea),
});
export type Cover = z.infer<typeof Cover>;
export type CoverPoint = z.infer<typeof CoverPoint>;
export type FightArea = z.infer<typeof FightArea>;
