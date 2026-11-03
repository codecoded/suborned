// Brief 075 roll-out (palms district-wide): which pavement edges take the old town's palm grid (oldstreet.ts OLD_STREET.palms: 10 m, staggered, 1.2 m off the kerb).
// One predicate for the generator (roads.ts) and the checker (tools/plan/street-checks.ts SF-7), so what is placed is what is judged.
import { bbox } from '../geo';
import type { Ctx } from '../common';
import { OLD_STREET } from '../oldstreet';
import { zoneOf } from './capstone';

const zones = new WeakMap<object, [number, number, number, number]>();
/** In the capstone zone (the Handover's walk window grown by 30 m, capstone.ts zoneOf): its plots keep the 12 m grid (OLD_STREET.palms.keep 'capstone'). The
 *  Handover's site (bun run sim:site) draws the street furniture there as walk solids and sites its call boxes between it, so the 10 m grid would move the
 *  Handover and c041 would need re-recording; the Handover is the one protected thing (brief 075, Sky 2026-10-28). */
export function inCapstoneZone(ctx: Ctx, plotId: string): boolean {
  const z = zones.get(ctx.plan) ?? zones.set(ctx.plan, zoneOf(ctx)).get(ctx.plan)!;
  const pl = ctx.d.plots.find(p => p.id === plotId); if (!pl) return false;
  const b = bbox(pl.polyV);
  return !(b[1] < z[0] || b[0] > z[1] || b[3] < z[2] || b[2] > z[3]);
}

/** Do this plot edge's pavement palms (road id, its class) stand on the old 10 m grid? The old-street roads always; the classes OLD_STREET.palms.classes
 *  outside the capstone zone (while OLD_STREET.palms.keep holds 'capstone'). */
export function oldPalmEdge(ctx: Ctx, plotId: string, road: string, cls: string): boolean {
  if (OLD_STREET.roads.includes(road)) return true;
  if (!OLD_STREET.palms.classes.includes(cls)) return false;
  return !(OLD_STREET.palms.keep.includes('capstone') && inCapstoneZone(ctx, plotId));
}
