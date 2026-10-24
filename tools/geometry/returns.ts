// The kerb returns as the walk model reads them (brief 028, Sky 2026-10-24: rounded pavement corners). The world export lays a carriageway flat (`ground/return/<plot>/v<i>`) where a return cuts a pavement corner. The builder's kerb strip
// (Layers.Ground: a 0.3 m strip along every carriageway edge, 0.184 m high) runs straight on past a return's tangent points; Layers.Ground drops the strip where it lies inside such a flat, inside a bay flat (`civic/bay/*`: a lay-by's mouth is open carriageway, no kerb across it) or inside a bend's pad slab (`plot/bend/*`), grown by STRIP_GROW, and the kerb
// stones of the arc (ground.pieces kind `kerb-return`) stand there instead. tools/geometry/visible.ts and tools/sim/solids.ts (floorGrid) mirror Layers.Ground, so they drop the same strip: one definition, here.
import type { World } from '../../contracts/world/world.schema';
import { type V, inPoly, segDist } from '../plan/geom';

/** m: the fill's ring grown this much takes the kerb strip's 0.3 m with it. */
export const STRIP_GROW = 0.35;
export const FILL_PREFIX = 'ground/return/';
/** The pavement a road bend's concentric outside lays into the road's corner (a slab of its own): the strip it covers is dropped too. */
export const BEND_PREFIX = 'plot/bend/';

export type Fills = { has: boolean; inside: (p: V) => boolean };
/** Tests a point against the return fills of a world, in the world's own plan frame. */
export function returnFills(w: Pick<World, 'ground'>): Fills {
  const rings = [...w.ground.flats.filter(f => f.id.startsWith(FILL_PREFIX) || f.id.includes('/bay/')), ...w.ground.plots.filter(p => p.id.startsWith(BEND_PREFIX))].map(f => { const r = f.ring as V[]; return { r, x0: Math.min(...r.map(p => p[0])) - STRIP_GROW, x1: Math.max(...r.map(p => p[0])) + STRIP_GROW, y0: Math.min(...r.map(p => p[1])) - STRIP_GROW, y1: Math.max(...r.map(p => p[1])) + STRIP_GROW }; });
  return {
    has: rings.length > 0,
    inside: p => { for (const k of rings) if (p[0] >= k.x0 && p[0] <= k.x1 && p[1] >= k.y0 && p[1] <= k.y1 && (inPoly(p, k.r) || k.r.some((q, i) => segDist(p, q, k.r[(i + 1) % k.r.length]!) <= STRIP_GROW))) return true; return false; },
  };
}
