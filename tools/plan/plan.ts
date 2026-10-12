// Loads a suborned.plan/1 file and derives what the checks and drawings share: road geometry, plot polygons, the
// pylon zones and the sky rail pylons.
import { type Rect, type Sample, type Seg, type V, chainage, dist, filleted, inPoly, notched, pathLength, polyline, rectPoly, ring, sample } from './geom';

export type Road = { id: string; name: string; class: 'primary' | 'district' | 'access'; w: number; median?: number; reservation?: number; gyratory?: boolean; arc?: V; cw?: boolean; path: string[] };
export type Plot = { id: string; name?: string; use: string; area: string; rect?: Rect; poly?: V[]; notch?: [number, number, number]; civic?: string };
export type Path = { id: string; role: string; double?: boolean; pts: number[][] };
export type Stop = { id: string; name: string; path: string; at: V; length?: number; interchange?: string; fronts?: string };
export type Line = { id: string; name: string; beyond: string; paths: Path[]; order?: string[]; stops?: Stop[]; stations?: Stop[] };
/** A sited megastructure (brief 004 A1). Footprints and bridges are rects [x0, x1, y0, y1]; levels name standards.levels ids. */
export type MegaLevel = { level: string; z?: number; what: string; mix?: Record<string, number> };
export type MegaBridge = { id: string; level: string; rect: Rect; joins: string; clearSpan?: number; supports?: string[] };
export type Megastructure = {
  id: string; name: string; kind: string; areas: string[]; footprint: Rect[];
  spans: { plots: string[]; roads?: string[]; foot?: string[]; zones?: string[]; skyrail?: string };
  height: number; levels: MegaLevel[]; bridges?: MegaBridge[]; connectors?: unknown; supports: { zones: string[]; [k: string]: unknown };
  why: string; source: string; [k: string]: unknown;
};
/** A landmark disc: r its ground, `top` the height cap (above the datum), `kit` the piece standing on it; the skyport has a caisson and `pads` (c, yaw in degrees) for its kit pieces. */
export type Landmark = { id: string; name: string; c: V; r: number; kit?: string; top?: number; caisson?: { r: number; deck: number; how?: string }; pads?: { c: V; yaw: number }[]; [k: string]: unknown };
export type Plan = {
  id: string; name: string; version: string; date: string; status: string;
  sheet: { drawing: string; x: V; y: V; grid: number; scale: string; inset: { drawing: string; name: string; x: V; y: V; scale: string } };
  standards: any; nodes: Record<string, V>; links: { node: string; to: string; via: string }[];
  roads: Road[]; gates: { id: string; name: string; node: string; road: string; area: string }[];
  areas: { id: string; name: string; rect: Rect; notch?: [number, number, number]; label?: V; size?: number }[]; plots: Plot[];
  foot: { id: string; name: string; kind: string; w: number; pts: V[] }[];
  circus: { c: V; island: number; garden: number; ring: V; pavement: number };
  water: V[][]; quay: V[][]; beyond: V[][]; landmarks: Landmark[]; megastructures?: Megastructure[];
  tram: Line[]; skyrail: Line[]; routes?: { id: string; name: string; from: string; to: string[]; toName?: string }[];
  civic: any; notes: { why: string[]; concept: string[]; questions: string[]; sources: string[] };
};

export type RoadGeo = Road & { segs: Seg[]; line: V[]; length: number };
export type PathGeo = Path & { line: string; segs: Seg[]; samples: Sample[]; length: number };
export type Pylon = { p: V; line: string; path: string; s: number; span: number };
export type Derived = {
  roads: RoadGeo[]; plots: (Plot & { polyV: V[] })[]; areas: { id: string; name: string; polyV: V[]; label?: V; size?: number }[];
  zones: { kind: string; poly?: V[]; c?: V; r?: number }[];
  tram: PathGeo[]; sky: PathGeo[]; pylons: Pylon[]; pylonGaps: { line: string; path: string; from: number; to: number }[];
  offsheet: Set<string>;
};

const shape = (x: { rect?: Rect; poly?: V[]; notch?: [number, number, number] }): V[] =>
  x.poly ?? (x.notch ? notched(x.rect!, [x.notch[0], x.notch[1]], x.notch[2]) : rectPoly(x.rect!));

function roadSegs(plan: Plan, r: Road): Seg[] {
  const pts = r.path.map(id => { const p = plan.nodes[id]; if (!p) throw new Error(`road ${r.id}: unknown node ${id}`); return p; });
  return r.arc ? ring(pts, r.arc, !!r.cw) : filleted(pts);
}

/** Medians as zones, cut back at every junction by the crossing road's half width + 1 m. */
function medians(plan: Plan, roads: RoadGeo[]) {
  const zones: Derived['zones'] = [];
  for (const r of roads) {
    if (!r.median || r.arc) continue;
    for (let i = 1; i < r.path.length; i++) {
      const a = plan.nodes[r.path[i - 1]], b = plan.nodes[r.path[i]], d = dist(a, b);
      const cut = (id: string) => Math.max(0, ...roads.filter(o => o !== r && o.path.includes(id)).map(o => o.w / 2 + 1));
      const c0 = cut(r.path[i - 1]), c1 = cut(r.path[i]);
      if (d - c0 - c1 <= 0) continue;
      const u: V = [(b[0] - a[0]) / d, (b[1] - a[1]) / d], n: V = [-u[1] * r.median / 2, u[0] * r.median / 2];
      const p0: V = [a[0] + u[0] * c0, a[1] + u[1] * c0], p1: V = [b[0] - u[0] * c1, b[1] - u[1] * c1];
      zones.push({ kind: 'median', poly: [[p0[0] + n[0], p0[1] + n[1]], [p1[0] + n[0], p1[1] + n[1]], [p1[0] - n[0], p1[1] - n[1]], [p0[0] - n[0], p0[1] - n[1]]] });
    }
  }
  return zones;
}

export const inZone = (p: V, z: Derived['zones'][number]) => (z.poly ? inPoly(p, z.poly) : dist(p, z.c!) <= z.r!);

/** Span limit at radius r (Alweg: straight, gentle curve, tight curve). */
export function spanLimit(std: any, r: number) {
  return r === Infinity ? std.spanStraight : r >= std.tightBelow ? std.spanCurve : std.spanTight;
}

/** Greedy pylons: each as far along as the span limit allows, backed off until it stands in an allowed zone. A loop's
 *  ends hang off the trunk's end pylon, so they are not placed again. */
function placePylons(std: any, path: PathGeo, allowed: (p: V) => boolean) {
  const out: Pylon[] = [], gaps: Derived['pylonGaps'] = [], S = path.samples, L = path.length;
  const at = (s: number) => S.reduce((b, x) => (Math.abs(x.s - s) < Math.abs(b.s - s) ? x : b));
  const limitOver = (s0: number, s1: number) => Math.min(...S.filter(x => x.s > s0 - .01 && x.s <= s1 + .01).map(x => spanLimit(std, x.r)));
  const hung = path.role === 'loop';
  let s = 0;
  if (!hung) { const first = S.find(x => allowed(x.p)); s = first?.s ?? 0; out.push({ p: first?.p ?? S[0].p, line: path.line, path: path.id, s, span: 0 }); }
  while (s < L - .01) {
    let lim = spanLimit(std, at(s).r);
    for (let k = 0; k < 3; k++) lim = limitOver(s, Math.min(L, s + lim));
    if (s + lim >= L - .01) {
      if (hung || allowed(S[S.length - 1].p)) { if (!hung) out.push({ p: S[S.length - 1].p, line: path.line, path: path.id, s: L, span: L - s }); break; }
    }
    let placed = false;
    for (let t = Math.min(L, s + lim); t >= s + 6; t -= .5) {
      const x = at(t);
      if (x.s - s <= lim + 1e-6 && allowed(x.p)) { out.push({ p: x.p, line: path.line, path: path.id, s: x.s, span: x.s - s }); s = x.s; placed = true; break; }
    }
    if (!placed) {
      const next = S.find(x => x.s > s + lim && allowed(x.p));
      gaps.push({ line: path.line, path: path.id, from: s, to: next?.s ?? L });
      if (!next) break;
      out.push({ p: next.p, line: path.line, path: path.id, s: next.s, span: next.s - s }); s = next.s;
    }
  }
  return { out, gaps };
}

export function derive(plan: Plan): Derived {
  const roads: RoadGeo[] = plan.roads.map(r => { const segs = roadSegs(plan, r); return { ...r, segs, line: polyline(segs, 2), length: pathLength(segs) }; });
  const paths = (lines: Line[]) => lines.flatMap(l => l.paths.map(p => {
    const segs = filleted(p.pts), samples = sample(segs, .5);
    return { ...p, line: l.id, segs, samples, length: pathLength(segs) };
  }));
  const c = plan.circus;
  const zones: Derived['zones'] = [
    ...medians(plan, roads),
    ...plan.foot.filter(f => f.kind === 'mall').map(f => ({ kind: 'mall', poly: rectPoly([Math.min(f.pts[0][0], f.pts[1][0]), Math.max(f.pts[0][0], f.pts[1][0]), f.pts[0][1] - f.w / 2, f.pts[0][1] + f.w / 2]) })),
    ...plan.plots.filter(p => p.use === 'park').map(p => ({ kind: 'park', poly: shape(p) })),
    ...plan.quay.map(poly => ({ kind: 'quay', poly })),
    ...plan.water.map(poly => ({ kind: 'water', poly })),
    { kind: 'island', c: c.c, r: c.garden },
  ];
  const sky = paths(plan.skyrail), tram = paths(plan.tram);
  const kinds = new Set(plan.standards.skyrail.pylonZones as string[]);
  const allowed = (p: V) => zones.some(z => kinds.has(z.kind) && inZone(p, z));
  const pylons: Pylon[] = [], pylonGaps: Derived['pylonGaps'] = [];
  for (const p of sky) { const r = placePylons(plan.standards.skyrail, p, allowed); pylons.push(...r.out); pylonGaps.push(...r.gaps); }
  return {
    roads, tram, sky, pylons, pylonGaps, zones,
    plots: plan.plots.map(p => ({ ...p, polyV: shape(p) })),
    areas: plan.areas.map(a => ({ id: a.id, name: a.name, polyV: shape(a), label: a.label, size: a.size })),
    offsheet: new Set(plan.links.map(l => l.node)),
  };
}
