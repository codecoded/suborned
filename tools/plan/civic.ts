// The Handover block in plan coordinates: testbed frame → block frame (HandoverSite.Zones) → plan (blockToPlan).
import { type Rect, type V, add, rectPoly } from './geom';

export type Civic = {
  buildings: { id: string; name: string; poly: V[] }[];
  structures: { id: string; name: string; poly: V[] }[];
  objects: { id: string; kind: string; poly: V[] }[];
  bays: { id: string; poly: V[] }[];
  points: { name: string; p: V; zone: string }[];
  blocks: { id: string; outer: V[]; core: V[] }[];
  crossing: V[];
};

const turn = ([x, y]: V, t: number): V => [[x, y], [-y, x], [-x, -y], [y, -x]][t & 3] as V;

export function civic(spec: any, manifest: any, destinations: any): Civic {
  const o: V = spec.blockToPlan.origin, ax: V = spec.blockToPlan.x, ay: V = spec.blockToPlan.y;
  const toPlan = ([bx, by]: V): V => [o[0] + bx * ax[0] + by * ay[0], o[1] + bx * ax[1] + by * ay[1]];
  const zoneOf = (p: V) => (spec.zones as any[]).find(z => p[0] >= z.from[0] && p[0] <= z.from[1] && p[1] >= z.from[2] && p[1] <= z.from[3]);
  const map = (p: V, z = zoneOf(p)): V | null => (z ? toPlan(add(turn(p, z.turns), z.shift)) : null);
  const mapAll = (pts: V[]) => { const z = zoneOf(centre(pts)); return z ? pts.map(p => map(p, z)!) : null; };
  const centre = (pts: V[]): V => [pts.reduce((s, p) => s + p[0], 0) / pts.length, pts.reduce((s, p) => s + p[1], 0) / pts.length];
  const box = (pos: any, dim: any, deg = 0): V[] => {
    const a = (deg * Math.PI) / 180, hw = dim.width / 2, hd = dim.depth / 2;
    return ([[-hw, -hd], [hw, -hd], [hw, hd], [-hw, hd]] as V[]).map(([x, y]) => [pos.x + x * Math.cos(a) - y * Math.sin(a), pos.y + x * Math.sin(a) + y * Math.cos(a)] as V);
  };
  const blockRect = (r: Rect) => rectPoly(r).map(toPlan);
  const w = spec.walk;
  const keep = <T,>(x: (T | null)[]) => x.filter(Boolean) as T[];

  return {
    buildings: keep(manifest.buildings.map((b: any) => {
      const poly = mapAll(b.footprint.vertices.map((v: any) => [v.x, v.y] as V));
      return poly && { id: b.id, name: b.name, poly };
    })),
    structures: keep(manifest.structures.map((s: any) => { const poly = mapAll(box(s.position, s.dimensions, s.authoringHeadingDegrees ?? 0)); return poly && { id: s.id, name: s.name, poly }; })),
    objects: keep(manifest.sceneObjects.filter((x: any) => ['fountain', 'planter', 'table'].includes(x.category)).map((x: any) => {
      const poly = mapAll(box(x.position, x.dimensions, x.authoringHeadingDegrees ?? 0)); return poly && { id: x.id, kind: x.category, poly };
    })),
    bays: keep(['officialBay', 'getawayBay'].map(k => { const b = manifest.roads[k]; const poly = b && mapAll(rectPoly([b.x[0], b.x[1], b.y[0], b.y[1]])); return poly && { id: k, poly }; })),
    points: keep(destinations.destinations.map((d: any) => { const z = zoneOf([d.x, d.y]); return z && { name: d.name, p: map([d.x, d.y], z)!, zone: z.name }; })),
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
