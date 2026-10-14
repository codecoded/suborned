// What a street patch lies on (Sky's walk of 2026-10-14, Latch's rebuild notes 2 and 3): a patch stands at the level of the surface under it,
// and a landing stands on pavement, a path or a platform, never on a lawn. Shared by the generator and by SF-20.
import { type V, lineDist } from '../../../plan/geom';
import { roadAt } from '../../../plan/street-zones';
import type { Ctx } from '../common';
import { pointInRing } from '../geo';

/** The carriageway's level: roads are drawn at 0 (build.ts), a pavement top is ctx.pav above it. */
export const ROAD_Z = 0;

/** True if p lies on a carriageway (within half a road's width of its centre line). */
export const onRoad = (ctx: Ctx, p: V): boolean => { const r = roadAt(ctx, p, ''); return !!r && r.dk < -0.01; };

/** The level of the surface under p: a carriageway, else the pavement top. */
export const levelAt = (ctx: Ctx, p: V): number => (onRoad(ctx, p) ? ROAD_Z : ctx.pav);

/** True if p lies on a park lawn: inside a park plot, past the street pavement along its edge, and off every foot path. */
export function lawn(ctx: Ctx, p: V): boolean {
  if (!ctx.d.plots.some(pl => pl.use === 'park' && !pl.civic && pointInRing(p, pl.polyV))) return false;
  if (ctx.plan.foot.some(f => lineDist(p, f.pts) <= f.w / 2)) return false;
  const r = roadAt(ctx, p, '');
  return !(r && r.dk >= -0.01 && r.dk <= r.sec.pavement);
}
