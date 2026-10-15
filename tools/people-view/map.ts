// The civic block as the map draws it: footprints, doors, café tables with their chairs and standing points, kerb bays and crossings, from the plan's block
// (contracts/world/civic-block.json through tools/plan/civic.ts, the same reading the checker and the expander use) and the scope the people were made on,
// with the mission's points on top. Every coordinate here is already in map frame: S turns the plan frame (x east, y north) a quarter turn so the block's long
// axis runs across the page, the café square on the left and the Records Office on the right (block x runs south, to the right; block y runs east, up the page).
import type { V } from '../plan/geom';
import { centroid } from '../plan/geom';
import { toPlanFn, type Civic } from '../plan/civic';
import type { Scope } from '../people/scope';

export const S = (p: readonly number[]): V => [Math.round(-p[1]! * 100) / 100, Math.round(-p[0]! * 100) / 100];
const r2 = (n: number) => Math.round(n * 100) / 100;

export type MPoly = { layer: string; id: string; name: string; kind: string; pts: V[]; h?: number };
export type MapData = {
  bounds: [number, number, number, number];
  polys: MPoly[];
  circles: { layer: string; id: string; name: string; kind: string; c: V; r: number; dashed?: boolean }[];
  /** the mission's points: destinations, the points it adds, the cameras, the cell's start */
  marks: { id: string; name: string; kind: 'dest' | 'point' | 'camera' | 'cell' | 'door'; at: V; src?: string }[];
  cones: { id: string; apex: V; a: V; b: V; name: string }[];
  labels: { text: string; at: V; size: number }[];
  /** chairs: place id → table id */
  chairs: { id: string; table: string; at: V }[];
  stands: { id: string; at: V }[];
};

type MissionIn = { destinations: { name: string; at: number[] }[]; crew?: { name: string; at: number[] }[]; cameras?: { id: string; at: number[]; looksAt: string; reachM: number; halfM: number }[]; points?: { id: string; at?: number[]; structure?: string; reason?: string; src?: string }[] };

export function buildMap(plan: { civic: unknown }, civ: Civic, scope: Scope, mission: MissionIn): MapData {
  const toPlan = toPlanFn(plan.civic), polys: MPoly[] = [], circles: MapData['circles'] = [], marks: MapData['marks'] = [], cones: MapData['cones'] = [], labels: MapData['labels'] = [];
  const poly = (layer: string, id: string, name: string, kind: string, p: V[], h?: number) => polys.push({ layer, id, name, kind, pts: p.map(S), ...(h !== undefined ? { h } : {}) });
  for (const b of civ.blocks) poly('ground', b.id, `${b.id} block`, 'block', b.outer);
  for (const b of civ.bays) poly('bays', b.id, b.id, 'bay', b.poly);
  poly('crossing', 'crossing', 'the block\'s crossing', 'crossing', civ.crossing);
  // crossing landings: a strip 4 m wide, 1.8 m deep inside the kerb (plan standards.streetscape.crossings), drawn from the block's own `at` and `inward`
  const dress = (civ.dressing ?? {}) as { crossings?: { at?: { id: string; at: number[]; inward: number[] }[] }; keepClear?: { id: string; polygon?: number[][]; ring?: { centre: number[]; r0: number; r1: number }; reason?: string }[] };
  for (const c of dress.crossings?.at ?? []) {
    const u = c.inward as V, n: V = [-u[1], u[0]], a = c.at as V;
    const corner = (s: number, d: number): V => toPlan([a[0] + n[0] * 2 * s + u[0] * d, a[1] + n[1] * 2 * s + u[1] * d]);
    poly('crossing', c.id, `crossing landing ${c.id}`, 'landing', [corner(1, 0), corner(-1, 0), corner(-1, 1.8), corner(1, 1.8)]);
  }
  for (const z of dress.keepClear ?? []) {
    if (z.polygon) poly('keepclear', z.id, `keep-clear ${z.id}`, 'zone', z.polygon.map(p => toPlan(p as V)));
    else if (z.ring) { const c = S(toPlan(z.ring.centre as V)); circles.push({ layer: 'keepclear', id: `${z.id}/outer`, name: `keep-clear ${z.id}`, kind: 'zone', c, r: z.ring.r1 }, { layer: 'keepclear', id: `${z.id}/inner`, name: `keep-clear ${z.id}`, kind: 'zone', c, r: z.ring.r0, dashed: true }); }
  }
  for (const b of civ.buildings) {
    poly('buildings', b.id, b.name, 'building', b.poly, b.height);
    labels.push({ text: b.name, at: S(centroid(b.poly)), size: 2.4 });
    for (const t of b.towers ?? []) if (t.poly) poly('buildings', `${b.id}/${t.id}`, `${b.name} tower ${t.id}`, 'tower', t.poly, t.height);
  }
  for (const s of civ.structures) poly('structures', s.id, s.name, s.category, s.poly, s.height);
  for (const o of civ.objects) {
    if (o.kind === 'table') continue; // the café's tables are drawn with their chairs
    poly('objects', o.id, o.id, o.kind, o.poly);
  }
  const chairs: MapData['chairs'] = [];
  for (const set of scope.sets) {
    const o = civ.objects.find(x => x.id === set.id);
    if (o) poly('tables', set.id, set.id, 'table', o.poly);
    for (const s of set.seats) chairs.push({ id: s.place, table: set.id, at: S(s.at) });
  }
  const stands = scope.places.filter(p => p.kind === 'stand').map(p => ({ id: p.id, at: S(p.at) }));
  for (const e of civ.entrances) marks.push({ id: e.id, name: e.name, kind: 'door', at: S(e.p) });
  for (const d of mission.destinations) marks.push({ id: `dest/${d.name}`, name: d.name, kind: 'dest', at: S(toPlan(d.at as V)) });
  for (const pt of mission.points ?? []) {
    const at = pt.at ? toPlan(pt.at as V) : pt.structure ? (() => { const s = civ.structures.find(x => x.id === pt.structure); return s ? centroid(s.poly) : undefined; })() : undefined;
    if (at) marks.push({ id: `point/${pt.id}`, name: pt.id, kind: 'point', at: S(at), ...(pt.src ? { src: pt.src } : {}) });
  }
  for (const c of mission.crew ?? []) marks.push({ id: `cell/${c.name}`, name: c.name, kind: 'cell', at: S(toPlan(c.at as V)) });
  for (const cam of mission.cameras ?? []) {
    const at = toPlan(cam.at as V), look = mission.destinations.find(d => d.name === cam.looksAt); if (!look) continue;
    const to = toPlan(look.at as V), dx = to[0] - at[0], dy = to[1] - at[1], d = Math.hypot(dx, dy) || 1, ux = dx / d, uy = dy / d;
    const far: V = [at[0] + ux * cam.reachM, at[1] + uy * cam.reachM];
    marks.push({ id: `camera/${cam.id}`, name: `camera ${cam.id}`, kind: 'camera', at: S(at) });
    cones.push({ id: cam.id, name: `camera ${cam.id}, looks at ${cam.looksAt}`, apex: S(at), a: S([far[0] - uy * cam.halfM, far[1] + ux * cam.halfM]), b: S([far[0] + uy * cam.halfM, far[1] - ux * cam.halfM]) });
  }
  const pts = [...polys.filter(p => p.layer !== 'ground').flatMap(p => p.pts), ...chairs.map(c => c.at), ...stands.map(s => s.at), ...marks.filter(m => m.kind === 'dest' || m.kind === 'door').map(m => m.at)];
  const bounds: [number, number, number, number] = [Math.min(...pts.map(p => p[0])) - 6, Math.min(...pts.map(p => p[1])) - 6, Math.max(...pts.map(p => p[0])) + 6, Math.max(...pts.map(p => p[1])) + 6].map(r2) as [number, number, number, number];
  return { bounds, polys, circles, marks, cones, labels, chairs, stands };
}
