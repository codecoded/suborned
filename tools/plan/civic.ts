// The civic block in plan coordinates: block frame (contracts/world/civic-block.json, suborned.block/4) → plan by blockToPlan.
// A pinned suborned.block/2 or /3 (recorded runs) reads the same: its destinations and crew belong to an overlay, not here, and a tower
// without a stated footprint (`polygon`, /4) is a point `at`: the checker then knows no tower plan, as when the run was made.
import { type Rect, type V, rectPoly } from './geom';

/** An entrance tower: where it stands and, from block/4, its plan (`poly`, plan frame). */
export type Tower = { id: string; at: V; height: number; poly?: V[] };

export type Civic = {
  buildings: { id: string; name: string; poly: V[]; height: number; z: number; towers?: Tower[]; [k: string]: any }[];
  structures: { id: string; name: string; category: string; poly: V[]; height: number; z: number }[];
  objects: { id: string; kind: string; poly: V[]; height: number; z: number }[];
  bays: { id: string; poly: V[] }[];
  entrances: { id: string; name: string; building: string; p: V }[];
  /** Where walks start: the building entrances (an overlay may replace them). */
  walkFrom: { name: string; p: V }[];
  blocks: { id: string; outer: V[]; core: V[] }[];
  crossing: V[];
  /** The block's `dressing` key (block/4), unchanged and in block frame: keep-clear zones, lamps, bollards, planters, parasols, piece pivots. Nothing reads it yet. */
  dressing?: Record<string, unknown>;
};

export function toPlanFn(spec: any) {
  const o: V = spec.blockToPlan.origin, ax: V = spec.blockToPlan.x, ay: V = spec.blockToPlan.y;
  return ([bx, by]: V): V => [o[0] + bx * ax[0] + by * ay[0], o[1] + bx * ax[1] + by * ay[1]];
}

export function civic(spec: any, block: any): Civic {
  if (!['suborned.block/2', 'suborned.block/3', 'suborned.block/4'].includes(block.schema)) throw new Error(`civic block is ${block.schema}, expected suborned.block/4 (bun run block)`);
  const toPlan = toPlanFn(spec);
  const blockRect = (r: Rect) => rectPoly(r).map(toPlan);
  const w = spec.walk;
  const entrances = block.buildings.flatMap((b: any) => (b.entrances ?? []).map((e: any) => ({ id: e.id as string, name: e.name as string, building: b.id as string, p: toPlan(e.at) })));

  return {
    buildings: block.buildings.map(({ polygon, towers, entrances: _e, ...b }: any) => ({ ...b, poly: polygon.map(toPlan), ...(towers ? { towers: towers.map(({ polygon, ...x }: any) => ({ ...x, at: toPlan(x.at), ...(polygon ? { poly: polygon.map(toPlan) } : {}) })) } : {}) })),
    structures: block.structures.map(({ polygon, ...s }: any) => ({ ...s, poly: polygon.map(toPlan) })),
    objects: block.objects.map(({ polygon, ...x }: any) => ({ ...x, poly: polygon.map(toPlan) })),
    bays: block.bays.map((b: any) => ({ id: b.id, poly: b.polygon.map(toPlan) })),
    entrances,
    walkFrom: entrances.map((e: any) => ({ name: e.name, p: e.p })),
    blocks: Object.entries(spec.blocks as Record<string, Rect>).map(([id, r]) => ({ id, outer: blockRect(r), core: blockRect([r[0] + w, r[1] - w, r[2] + w, r[3] - w]) })),
    crossing: blockRect(spec.crossing),
    ...(block.dressing ? { dressing: block.dressing } : {}),
  };
}

/** The block rect in plan coordinates as [x0, x1, y0, y1], for comparing with the plan's own plot. */
export function blockPlanRect(spec: any, id: string): Rect {
  const pts = rectPoly(spec.blocks[id]).map(([bx, by]) => [spec.blockToPlan.origin[0] + bx * spec.blockToPlan.x[0] + by * spec.blockToPlan.y[0], spec.blockToPlan.origin[1] + bx * spec.blockToPlan.x[1] + by * spec.blockToPlan.y[1]]);
  const xs = pts.map(p => p[0]), ys = pts.map(p => p[1]);
  return [Math.min(...xs), Math.max(...xs), Math.min(...ys), Math.max(...ys)];
}

/** The footprints a walker cannot enter or must stand at: every building's and every stated tower plan, together. */
export const footprints = (civ: Civic): V[][] => civ.buildings.flatMap(b => [b.poly, ...(b.towers ?? []).flatMap(t => (t.poly ? [t.poly] : []))]);
