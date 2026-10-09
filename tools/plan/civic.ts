// The Handover block in plan coordinates: block frame (contracts/world/handover-block.json, suborned.block/2) → plan by blockToPlan.
import { type Rect, type V, rectPoly } from './geom';

export type Civic = {
  buildings: { id: string; name: string; poly: V[]; height: number; z: number; [k: string]: any }[];
  structures: { id: string; name: string; category: string; poly: V[]; height: number; z: number }[];
  objects: { id: string; kind: string; poly: V[]; height: number; z: number }[];
  bays: { id: string; poly: V[] }[];
  points: { name: string; p: V; surface: string }[];
  crew: { name: string; p: V; surface: string }[];
  blocks: { id: string; outer: V[]; core: V[] }[];
  crossing: V[];
};

export function civic(spec: any, block: any): Civic {
  if (block.schema !== 'suborned.block/2') throw new Error(`handover block is ${block.schema}, expected suborned.block/2 (bun run block)`);
  const o: V = spec.blockToPlan.origin, ax: V = spec.blockToPlan.x, ay: V = spec.blockToPlan.y;
  const toPlan = ([bx, by]: V): V => [o[0] + bx * ax[0] + by * ay[0], o[1] + bx * ax[1] + by * ay[1]];
  const blockRect = (r: Rect) => rectPoly(r).map(toPlan);
  const w = spec.walk;
  const place = (d: any) => ({ name: d.name as string, p: toPlan(d.at), surface: d.surface as string });

  return {
    buildings: block.buildings.map(({ polygon, towers, ...b }: any) => ({ ...b, poly: polygon.map(toPlan), ...(towers ? { towers: towers.map((x: any) => ({ ...x, at: toPlan(x.at) })) } : {}) })),
    structures: block.structures.map(({ polygon, ...s }: any) => ({ ...s, poly: polygon.map(toPlan) })),
    objects: block.objects.map(({ polygon, ...x }: any) => ({ ...x, poly: polygon.map(toPlan) })),
    bays: block.bays.map((b: any) => ({ id: b.id, poly: b.polygon.map(toPlan) })),
    points: block.destinations.map(place),
    crew: block.crew.map(place),
    blocks: Object.entries(spec.blocks as Record<string, Rect>).map(([id, r]) => ({ id, outer: blockRect(r), core: blockRect([r[0] + w, r[1] - w, r[2] + w, r[3] - w]) })),
    crossing: blockRect(spec.crossing),
  };
}

/** The block rect in plan coordinates as [x0, x1, y0, y1], for comparing with the plan's own plot. */
export function blockPlanRect(spec: any, id: string): Rect {
  const pts = rectPoly(spec.blocks[id]).map(([bx, by]) => [spec.blockToPlan.origin[0] + bx * spec.blockToPlan.x[0] + by * spec.blockToPlan.y[0], spec.blockToPlan.origin[1] + bx * spec.blockToPlan.x[1] + by * spec.blockToPlan.y[1]]);
  const xs = pts.map(p => p[0]), ys = pts.map(p => p[1]);
  return [Math.min(...xs), Math.max(...xs), Math.min(...ys), Math.max(...ys)];
}
