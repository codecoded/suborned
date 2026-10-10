// What an agent is shown about the plan besides retrieved passages: the geometry it must place things on, compactly.
import { centroid } from '../plan/geom';
import { derive, type Plan } from '../plan/plan';

const r = (v: number) => Math.round(v);
const bbox = (poly: number[][]) => [r(Math.min(...poly.map(p => p[0]!))), r(Math.max(...poly.map(p => p[0]!))), r(Math.min(...poly.map(p => p[1]!))), r(Math.max(...poly.map(p => p[1]!)))];

export const plots = (plan: Plan) => derive(plan).plots.map(p => ({ id: p.id, use: p.use, centre: centroid(p.polyV).map(r) as [number, number] }));

export function geometry(plan: Plan) {
  const nodes = plan.nodes;
  return JSON.stringify({
    sheet: { x: plan.sheet.x, y: plan.sheet.y, note: 'a line end on or beyond this edge leaves the sheet' },
    roads: plan.roads.map(x => ({ id: x.id, name: x.name, class: x.class, w: x.w, median: x.median, via: x.path.map(n => nodes[n]!.map(r)) })),
    foot: plan.foot.map(w => ({ id: w.id, kind: w.kind, w: w.w, pts: w.pts })),
    circus: { c: plan.circus.c, island: plan.circus.island },
    quay: plan.quay.map(bbox), water: plan.water.map(w => w.map(p => p.map(r))),
    waterNote: 'water is the polygon in water (x z points) and blocks walking; quay boxes [xmin, xmax, zmin, zmax] are walkable and carved out of it (the causeway and apron over the water); foot paths (e.g. skyport-pier) are walkable too', plotCentres: plots(plan),
  });
}

/** Lines of the plan as the proposals see them. */
export const transit = (plan: Plan) => JSON.stringify({ tram: plan.tram, skyrail: plan.skyrail });
