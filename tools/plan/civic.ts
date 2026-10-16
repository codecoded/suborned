// The civic block in plan coordinates: block frame (contracts/world/civic-block.json, suborned.block/4) → plan by blockToPlan.
// A pinned suborned.block/2 or /3 (recorded runs) reads the same: its destinations and crew belong to an overlay, not here, and a tower
// without a stated footprint (`polygon`, /4) is a point `at`: the checker then knows no tower plan, as when the run was made.
import { type Rect, type V, rectPoly } from './geom';

/** What an entrance is for (block/4): public = the door a visitor uses, official = the councillor's door, service = staff and goods. */
export type EntranceKind = 'public' | 'official' | 'service';

/** An entrance tower: where it stands and, from block/4, its plan (`poly`, plan frame). */
export type Tower = { id: string; at: V; height: number; poly?: V[] };

export type Civic = {
  buildings: { id: string; name: string; poly: V[]; height: number; z: number; towers?: Tower[]; [k: string]: any }[];
  structures: { id: string; name: string; category: string; poly: V[]; height: number; z: number }[];
  objects: { id: string; kind: string; poly: V[]; height: number; z: number }[];
  bays: { id: string; poly: V[] }[];
  /** The doors people use: public and official (block/4 `kind`, absent = public). The consumers of "the door" (the cast's places, the crowd's refuges, the people tools) read this list only. */
  entrances: { id: string; name: string; building: string; p: V; z: number; kind: EntranceKind }[];
  /** The service doors (brief 013: `kind: 'service'`, staff and goods): stashes for the checker and the engine, a walk origin so the checker proves them reachable, but not a place the cast or the crowd uses. */
  serviceDoors: { id: string; name: string; building: string; p: V; z: number; kind: 'service' }[];
  /** Where walks start: every door, service doors included (an overlay may replace them). */
  walkFrom: { name: string; p: V }[];
  blocks: { id: string; outer: V[]; core: V[] }[];
  crossing: V[];
  /** The block's `dressing` key (block/4), unchanged and in block frame: keep-clear zones, lamps, bollards, planters, parasols, piece pivots. The world generator places it (tools/world/civic/dress.ts); rules CD-1..4 hold it. */
  dressing?: Record<string, unknown>;
  /** From block/4 with a `dressing`: the ground solids a walker cannot cross, plan frame: the pieces' wall, cheeks and pilasters (not the overhead marquee), the objects (beds, tables, fountain), the pillars, cabinets and monument, and the dressing's planter runs, pots, bollards and lamp bases (plan.civic.groundPieces). Absent on a block without one, so recorded walks read what they always did. */
  solids?: V[][];
  /** The name of each solid, in the same order (a piece's solid is `<piece>/<id>`): for the rules that say which solids a walk passes between. */
  solidIds?: string[];
};

/** The ground furniture of the block's dressing a walker cannot cross, as footprints in the plan frame: every planter run module, topiary pot, bollard and lamp base (civic-block.json `dressing`),
 *  each a rectangle of the size plan.civic.groundPieces gives its piece (along its yaw, across it; the pivot is the centre: the lamp's is the pole), turned by the dressing's yawDeg (block frame: from +x toward +y).
 *  The pieces' own sizes are Vela's (kit.json); the plan states the walker's version of them so a kit re-hash does not move a walk. A piece the plan gives no size is an error, never a gap in the floor. */
function furniture(spec: any, dressing: any, toPlan: (p: V) => V): { poly: V[]; id: string }[] {
  const sizes: Record<string, [number, number]> | undefined = spec.groundPieces?.sizes;
  if (!sizes) return [];
  const out: { poly: V[]; id: string }[] = [];
  const put = (id: string, piece: string, at: V, yawDeg = 0) => {
    const size = sizes[piece]; if (!size) throw new Error(`civic dressing: ${id} is ${piece}, which plan.civic.groundPieces.sizes gives no footprint (add it, or the walkers walk through it)`);
    const y = (yawDeg * Math.PI) / 180, u: V = [Math.cos(y), Math.sin(y)], v: V = [-Math.sin(y), Math.cos(y)], hl = size[0] / 2, hw = size[1] / 2;
    out.push({ id, poly: ([[-hl, -hw], [hl, -hw], [hl, hw], [-hl, hw]] as V[]).map(([a, b]) => toPlan([at[0] + a * u[0] + b * v[0], at[1] + a * u[1] + b * v[1]])) });
  };
  for (const run of dressing.planterRuns?.runs ?? []) (run.modules ?? []).forEach((m: any, i: number) => put(`${run.id}/${i}`, m.piece, m.at, run.yawDeg));
  for (const t of dressing.topiary?.at ?? []) put(t.id, dressing.topiary.piece, t.at, t.yawDeg);
  for (const b of dressing.bollards?.at ?? []) put(b.id, dressing.bollards.piece, b.at, b.yawDeg);
  for (const l of dressing.lamps?.at ?? []) put(l.id, dressing.lamps.piece, l.at, l.yawDeg);
  return out;
}

const SOLID_STRUCTURES = new Set(['pavilion-support', 'service-cabinet', 'civic-monument']); // the steps, landings and canopies of the others are walked on or under

export function toPlanFn(spec: any) {
  const o: V = spec.blockToPlan.origin, ax: V = spec.blockToPlan.x, ay: V = spec.blockToPlan.y;
  return ([bx, by]: V): V => [o[0] + bx * ax[0] + by * ay[0], o[1] + bx * ax[1] + by * ay[1]];
}

export function civic(spec: any, block: any): Civic {
  if (!['suborned.block/2', 'suborned.block/3', 'suborned.block/4'].includes(block.schema)) throw new Error(`civic block is ${block.schema}, expected suborned.block/4 (bun run block)`);
  const toPlan = toPlanFn(spec);
  const blockRect = (r: Rect) => rectPoly(r).map(toPlan);
  const w = spec.walk;
  const ground = block.dressing ? furniture(spec, block.dressing, toPlan) : [];
  const doors = block.buildings.flatMap((b: any) => (b.entrances ?? []).map((e: any) => ({ id: e.id as string, name: e.name as string, building: b.id as string, p: toPlan(e.at), z: (e.z ?? 0) as number, kind: (e.kind ?? 'public') as EntranceKind })));
  const entrances = doors.filter((e: any) => e.kind !== 'service'), serviceDoors = doors.filter((e: any) => e.kind === 'service');

  return {
    buildings: block.buildings.map(({ polygon, towers, entrances: _e, ...b }: any) => ({ ...b, poly: polygon.map(toPlan), ...(towers ? { towers: towers.map(({ polygon, ...x }: any) => ({ ...x, at: toPlan(x.at), ...(polygon ? { poly: polygon.map(toPlan) } : {}) })) } : {}) })),
    structures: block.structures.map(({ polygon, ...s }: any) => ({ ...s, poly: polygon.map(toPlan) })),
    objects: block.objects.map(({ polygon, ...x }: any) => ({ ...x, poly: polygon.map(toPlan) })),
    bays: block.bays.map((b: any) => ({ id: b.id, poly: b.polygon.map(toPlan) })),
    entrances, serviceDoors,
    walkFrom: doors.map((e: any) => ({ name: e.name, p: e.p })),
    blocks: Object.entries(spec.blocks as Record<string, Rect>).map(([id, r]) => ({ id, outer: blockRect(r), core: blockRect([r[0] + w, r[1] - w, r[2] + w, r[3] - w]) })),
    crossing: blockRect(spec.crossing),
    ...(block.dressing ? {
      dressing: block.dressing,
      solids: [
        ...(block.dressing.pieces ?? []).flatMap((p: any) => (p.solids ?? []).filter((x: any) => x.z === undefined).map((x: any) => x.polygon.map(toPlan))),
        ...block.objects.map((o: any) => o.polygon.map(toPlan)),
        ...block.structures.filter((x: any) => SOLID_STRUCTURES.has(x.category)).map((x: any) => x.polygon.map(toPlan)),
        ...ground.map(g => g.poly),
      ] as V[][],
      solidIds: [
        ...(block.dressing.pieces ?? []).flatMap((p: any) => (p.solids ?? []).filter((x: any) => x.z === undefined).map((x: any) => `${p.piece}/${x.id}`)),
        ...block.objects.map((o: any) => o.id as string),
        ...block.structures.filter((x: any) => SOLID_STRUCTURES.has(x.category)).map((x: any) => x.id as string),
        ...ground.map(g => g.id),
      ] as string[],
    } : {}),
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
