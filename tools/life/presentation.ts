// Presentation life for the flyby and free-fly scenes (brief 075 golden view, Pulse): cars on the carriageway lanes and in the kerbside bays,
// trams and sky rail trains on the plan's lines to the plan timetable, pedestrians on the pavements and at the stops, a few aircars.
// NEVER SIM DATA: the sim's site builder, the engine and the mission tools never read the output (presentation.test.ts checks it).
// Reads the world export (out/world/<plan>.world.json), the plan, the life rules (contracts/world/<plan>.life.json, the density model in
// model.ts) and tools/figures/figures.json; writes out/world/<plan>.presentation.json (contracts/world/presentation.schema.ts).
// Deterministic: every random draw is seeded by an id (fnv1a -> mulberry32); same inputs, same bytes.
//   bun run life:presentation [--world <file>] [--out <file>] [--hour 14.5] [--people 3000] [--cars 220]
import { createHash } from 'node:crypto';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join, relative } from 'node:path';
import { streetDensity } from './model';
import { CAT, CATEGORY, DRAW, PROM, bandOf } from './rules';
import { sectionOf } from '../plan/sections';
import type { Presentation, Model, Loop, Route, Timed, Looping, Still, Paint, PresentationLod } from '../../contracts/world/presentation.schema';

const ROOT = join(import.meta.dir, '..', '..');
export const PRESENTATION_FILE = 'out/world/harbour-district.presentation.json';
export const WORLD_FILE = 'out/world/harbour-district.world.json';
const PLAN_FILE = 'contracts/world/harbour-district.plan.json', LIFE_FILE = 'contracts/world/harbour-district.life.json', FIG_FILE = 'tools/figures/figures.json';
export const PERIOD = 720; // s: one Handover-length loop; every timetable headway must divide it (checked in build: the plan's 180 and 120 do)

// ---------- seeded randomness
const fnv = (s: string) => { let h = 0x811c9dc5; for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 0x01000193); } return h >>> 0; };
export function rng(id: string) { let a = fnv(id); return () => { a = (a + 0x6d2b79f5) | 0; let t = Math.imul(a ^ (a >>> 15), 1 | a); t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t; return ((t ^ (t >>> 14)) >>> 0) / 4294967296; }; }
const pick = <T>(r: () => number, xs: T[]) => xs[Math.floor(r() * xs.length) % xs.length];
const pickW = <T>(r: () => number, xs: [T, number][]) => { const tot = xs.reduce((s, x) => s + x[1], 0); let u = r() * tot; for (const [x, w] of xs) { if ((u -= w) <= 0) return x; } return xs[xs.length - 1][0]; };
const r2 = (n: number) => Math.round(n * 100) / 100, r3 = (n: number) => Math.round(n * 1000) / 1000;

// ---------- geometry (plan frame, x east, y north)
export type P = [number, number];
const dist = (a: P, b: P) => Math.hypot(a[0] - b[0], a[1] - b[1]);
const unit = (a: P, b: P): P => { const d = dist(a, b) || 1; return [(b[0] - a[0]) / d, (b[1] - a[1]) / d]; };
const right = (d: P): P => [d[1], -d[0]];
const add = (a: P, d: P, k = 1): P => [a[0] + d[0] * k, a[1] + d[1] * k];
export const cumul = (pts: P[]) => { const c = [0]; for (let i = 1; i < pts.length; i++) c.push(c[i - 1] + dist(pts[i - 1], pts[i])); return c; };
export function at(pts: P[], cum: number[], s: number): { p: P; dir: P } {
  const n = pts.length; if (s <= 0) return { p: pts[0], dir: unit(pts[0], pts[1]) };
  if (s >= cum[n - 1]) return { p: pts[n - 1], dir: unit(pts[n - 2], pts[n - 1]) };
  let lo = 0, hi = n - 1; while (hi - lo > 1) { const m = (lo + hi) >> 1; if (cum[m] <= s) lo = m; else hi = m; }
  const k = (s - cum[lo]) / ((cum[hi] - cum[lo]) || 1), a = pts[lo], b = pts[hi];
  return { p: [a[0] + (b[0] - a[0]) * k, a[1] + (b[1] - a[1]) * k], dir: unit(a, b) };
}
function slice(pts: P[], s0: number, s1: number): P[] {
  const c = cumul(pts), out: P[] = [at(pts, c, s0).p];
  for (let i = 0; i < pts.length; i++) if (c[i] > s0 + 1e-6 && c[i] < s1 - 1e-6) out.push(pts[i]);
  out.push(at(pts, c, s1).p); return out;
}
function resample(pts: P[], step: number): P[] {
  const c = cumul(pts), L = c[c.length - 1], n = Math.max(1, Math.ceil(L / step)), out: P[] = [];
  // keep corners: sample uniformly but also every original vertex
  const ss = new Set<number>(); for (let i = 0; i <= n; i++) ss.add(r3(L * i / n)); for (const x of c) ss.add(r3(x));
  for (const s of [...ss].sort((a, b) => a - b)) out.push(at(pts, c, s).p);
  return dedupe(out);
}
const dedupe = (pts: P[]) => pts.filter((p, i) => i === 0 || dist(p, pts[i - 1]) > 1e-4);
/** offset to the right of travel by off(s) (negative = left), miter at vertices (capped x2) */
function offsetLine(pts: P[], off: (s: number) => number): P[] {
  const c = cumul(pts), n = pts.length;
  return pts.map((p, i) => {
    const d0 = unit(pts[Math.max(0, i - 1)], pts[Math.max(1, i)]), d1 = unit(pts[Math.min(n - 2, i)], pts[Math.min(n - 1, i + 1)]);
    const n0 = right(d0), n1 = right(d1); let m: P = [n0[0] + n1[0], n0[1] + n1[1]]; const ml = Math.hypot(m[0], m[1]) || 1; m = [m[0] / ml, m[1] / ml];
    const cos = Math.max(0.5, m[0] * n0[0] + m[1] * n0[1]); return add(p, m, off(c[i]) / cos);
  });
}
function project(pts: P[], q: P): { s: number; d: number } {
  const c = cumul(pts); let best = { s: 0, d: Infinity };
  for (let i = 1; i < pts.length; i++) {
    const a = pts[i - 1], b = pts[i], L = c[i] - c[i - 1]; if (L <= 0) continue;
    const k = Math.max(0, Math.min(1, ((q[0] - a[0]) * (b[0] - a[0]) + (q[1] - a[1]) * (b[1] - a[1])) / (L * L)));
    const p: P = [a[0] + (b[0] - a[0]) * k, a[1] + (b[1] - a[1]) * k], d = dist(p, q);
    if (d < best.d) best = { s: c[i - 1] + k * L, d };
  }
  return best;
}
export const inPoly = (q: P[], x: number, y: number) => { let c = false; for (let i = 0, j = q.length - 1; i < q.length; j = i++) if ((q[i][1] > y) !== (q[j][1] > y) && x < (q[j][0] - q[i][0]) * (y - q[i][1]) / (q[j][1] - q[i][1]) + q[i][0]) c = !c; return c; };
const centroid = (r: P[]): P => [r.reduce((s, p) => s + p[0], 0) / r.length, r.reduce((s, p) => s + p[1], 0) / r.length];
const heading = (d: P) => Math.atan2(d[1], d[0]);
/** quadratic bezier from p0 (direction d0) to p1 (direction d1) through the tangents' crossing; a U-turn or parallel offset uses a cubic */
function connector(p0: P, d0: P, p1: P, d1: P): P[] {
  const D = dist(p0, p1); if (D < 0.05) return [p0];
  const den = d0[0] * d1[1] - d0[1] * d1[0]; let pts: P[] = [];
  const n = Math.max(3, Math.ceil(D / 1.0));
  const u = Math.abs(den) > 0.05 ? ((p1[0] - p0[0]) * d1[1] - (p1[1] - p0[1]) * d1[0]) / den : NaN;
  const v = Math.abs(den) > 0.05 ? ((p1[0] - p0[0]) * d0[1] - (p1[1] - p0[1]) * d0[0]) / den : NaN;
  if (u > 0 && v > 0 && u < 3 * D && v < 3 * D) {
    const c = add(p0, d0, u); for (let i = 0; i <= n; i++) { const t = i / n, a = (1 - t) * (1 - t), b = 2 * t * (1 - t), e = t * t; pts.push([a * p0[0] + b * c[0] + e * p1[0], a * p0[1] + b * c[1] + e * p1[1]]); }
  } else {
    const k = D * 0.45, c0 = add(p0, d0, k), c1 = add(p1, d1, -k);
    for (let i = 0; i <= n; i++) { const t = i / n, a = (1 - t) ** 3, b = 3 * t * (1 - t) ** 2, e = 3 * t * t * (1 - t), f = t ** 3; pts.push([a * p0[0] + b * c0[0] + e * c1[0] + f * p1[0], a * p0[1] + b * c0[1] + e * c1[1] + f * p1[1]]); }
  }
  return pts;
}
const endDir = (pts: P[]) => unit(pts[pts.length - 2], pts[pts.length - 1]);
const startDir = (pts: P[]) => unit(pts[0], pts[1]);
const extend = (pts: P[], back: number, fwd: number): P[] => [add(pts[0], startDir(pts), -back), ...pts, add(pts[pts.length - 1], endDir(pts), fwd)];

// ---------- motion: a speed profile along a polyline, then (t) per point, then a time-exact simplification
type Stop = { s: number; dwell: number; id: string };
function profile(pts0: P[], vmax: (s: number, i: number) => number, o: { acc: number; dec: number; latAcc: number; v0: number; v1: number; stops: Stop[]; step: number }) {
  // resample at `step`, insert each stop's exact s as a vertex
  const c0 = cumul(pts0), L = c0[c0.length - 1], ss = new Set<number>();
  const n = Math.ceil(L / o.step); for (let i = 0; i <= n; i++) ss.add(r3(L * i / n)); for (const st of o.stops) ss.add(r3(st.s));
  const sv = [...ss].sort((a, b) => a - b), pts = sv.map(s => at(pts0, c0, s).p), N = pts.length;
  const vm = sv.map((s, i) => vmax(s, i));
  for (let i = 1; i < N - 1; i++) { // curvature: v <= sqrt(latAcc * R)
    const a = unit(pts[i - 1], pts[i]), b = unit(pts[i], pts[i + 1]), th = Math.acos(Math.max(-1, Math.min(1, a[0] * b[0] + a[1] * b[1])));
    if (th > 1e-3) { const R = ((sv[i + 1] - sv[i - 1]) / 2) / th; vm[i] = Math.min(vm[i], Math.sqrt(o.latAcc * R)); }
  }
  const stopAt = new Map<number, Stop>(); for (const st of o.stops) stopAt.set(sv.findIndex(s => Math.abs(s - r3(st.s)) < 1e-6), st);
  const v = vm.slice(); v[0] = Math.min(v[0], o.v0); v[N - 1] = Math.min(v[N - 1], o.v1); for (const i of stopAt.keys()) v[i] = 0;
  for (let i = 1; i < N; i++) v[i] = Math.min(v[i], Math.sqrt(v[i - 1] ** 2 + 2 * o.acc * (sv[i] - sv[i - 1])));
  for (let i = N - 2; i >= 0; i--) v[i] = Math.min(v[i], Math.sqrt(v[i + 1] ** 2 + 2 * o.dec * (sv[i + 1] - sv[i])));
  const outP: P[] = [], outT: number[] = [], stops: { stop: string; tArrive: number; tDepart: number }[] = []; let t = 0;
  for (let i = 0; i < N; i++) {
    if (i > 0) { const ds = sv[i] - sv[i - 1], vv = v[i] + v[i - 1]; t += vv > 1e-6 ? 2 * ds / vv : 0; }
    outP.push(pts[i]); outT.push(t);
    const st = stopAt.get(i); if (st) { stops.push({ stop: st.id, tArrive: r2(t), tDepart: r2(t + st.dwell) }); t += st.dwell; outP.push(pts[i]); outT.push(t); }
  }
  return { pts: outP, t: outT, stops, len: L };
}
/** keep the fewest points such that linear-in-time interpolation stays within tol metres of every dropped point */
function simplify(pts: P[], t: number[], tol = 0.1) {
  const keep = [0]; let i = 0; const n = pts.length;
  while (i < n - 1) {
    let best = i + 1;
    for (let j = i + 2; j < n && j - i < 400; j++) {
      let ok = t[j] > t[i];
      for (let k = i + 1; k < j && ok; k++) {
        const f = (t[k] - t[i]) / (t[j] - t[i]), x = pts[i][0] + (pts[j][0] - pts[i][0]) * f, y = pts[i][1] + (pts[j][1] - pts[i][1]) * f;
        if (Math.hypot(x - pts[k][0], y - pts[k][1]) > tol) ok = false;
      }
      if (!ok) break; best = j;
    }
    keep.push(best); i = best;
  }
  return { xy: keep.flatMap(k => [r2(pts[k][0]), r2(pts[k][1])]), t: keep.map(k => r2(t[k])) };
}

// ---------- inputs
const sha = (f: string) => createHash('sha256').update(readFileSync(f)).digest('hex');
type Inputs = { world: any; plan: any; life: any; figures: string[]; files: Record<string, string> };
export function loadInputs(worldFile = join(ROOT, WORLD_FILE)): Inputs {
  if (!existsSync(worldFile)) throw new Error(`no world export at ${worldFile}: run bun run world`);
  const json = (f: string) => JSON.parse(readFileSync(f, 'utf8'));
  const figs = json(join(ROOT, FIG_FILE)).figures.filter((f: any) => f.role === 'townsperson').map((f: any) => f.id as string);
  return { world: json(worldFile), plan: json(join(ROOT, PLAN_FILE)), life: json(join(ROOT, LIFE_FILE)), figures: figs, files: { world: worldFile, plan: join(ROOT, PLAN_FILE), life: join(ROOT, LIFE_FILE), figures: join(ROOT, FIG_FILE) } };
}

// ---------- obstacles: a point a walker or a parked car may not stand in
export function obstacles(W: any) {
  const rings: { ring: P[]; box: number[] }[] = [];
  const addRing = (ring: P[]) => { const xs = ring.map(p => p[0]), ys = ring.map(p => p[1]); rings.push({ ring, box: [Math.min(...xs), Math.max(...xs), Math.min(...ys), Math.max(...ys)] }); };
  for (const b of W.buildings) if (b.base < 1.5 && b.ring) addRing(b.ring);
  for (const m of W.megastructures) for (const p of m.parts) if (p.base < 1.5 && p.ring && p.height > 1) addRing(p.ring);
  for (const c of W.civic) if (c.base < 1.5 && c.ring && c.height > 0.9 && !c.hidden) addRing(c.ring);
  for (const w of W.ground.water) addRing(w.ring);
  const cyl = W.landmarks.filter((l: any) => l.base < 1.5 && l.centre && l.radius && l.height > 2).map((l: any) => ({ c: l.centre as P, r: l.radius as number }));
  return (p: P) => rings.some(o => p[0] >= o.box[0] && p[0] <= o.box[1] && p[1] >= o.box[2] && p[1] <= o.box[3] && inPoly(o.ring, p[0], p[1])) || cyl.some((o: any) => dist(o.c, p) < o.r);
}
/** sky obstacles: anything tall whose footprint holds p and whose top is above z - clear */
export function skyObstacles(W: any, kit: Record<string, any>) {
  const items: { test: (p: P) => boolean; lo: number; hi: number }[] = [];
  const box = (ring: P[]) => { const xs = ring.map(p => p[0]), ys = ring.map(p => p[1]); return [Math.min(...xs), Math.max(...xs), Math.min(...ys), Math.max(...ys)]; };
  for (const b of [...W.buildings, ...W.megastructures.flatMap((m: any) => m.parts)]) if (b.ring) { const bx = box(b.ring); items.push({ test: p => p[0] >= bx[0] && p[0] <= bx[1] && p[1] >= bx[2] && p[1] <= bx[3] && inPoly(b.ring, p[0], p[1]), lo: b.base, hi: b.base + b.height }); }
  for (const l of W.landmarks) if (l.centre) items.push({ test: p => dist(p, l.centre) < l.radius, lo: l.base, hi: l.base + l.height });
  for (const s of W.signatures) {
    if (s.ground) items.push({ test: p => p[0] >= s.ground[0] && p[0] <= s.ground[1] && p[1] >= s.ground[2] && p[1] <= s.ground[3], lo: s.base, hi: s.tip });
    else if (s.kit?.at) { const k = kit[s.kit.id] ?? kit[s.kit.id.split('@')[0]]; const r = k ? Math.max(k.size[0], k.size[2]) / 2 : 15; items.push({ test: p => dist(p, s.kit.at) < r, lo: s.base, hi: s.tip }); }
  }
  return (p: P, z: number, clear = 4) => items.some(o => o.hi > z - clear && o.lo < z + clear && o.test(p));
}

// ---------- the road graph (plan nodes + world road lines)
type Edge = { id: string; road: string; cls: string; w: number; median: number; tram: boolean; a: string; b: string; pts: P[]; oneway: boolean };
type DEdge = { e: Edge; fwd: boolean; from: string; to: string; pts: P[] };
const TRAM_HALF = 1.825; // a tram's swept half width off its track (half car 1.325 + 0.5: sections.tramStreet.tracks.sweptWhy)
/** the gyratory's car lane radius: midway between the swept edge of a tram loop round the island (if a line loops there) and the ring's outer kerb; else the ring's middle */
function ringLane(W: any, PL: any) {
  const c: P = PL.circus.c, [r0, r1] = PL.circus.ring as [number, number];
  const rs = W.transit.tram.filter((t: any) => /\/loop$/.test(t.id)).flatMap((t: any) => t.line.map((p: P) => dist(p, c))).filter((r: number) => r > r0 && r < r1).sort((a: number, b: number) => a - b);
  return rs.length ? (Math.max(r0, rs[Math.floor(rs.length / 2)] + TRAM_HALF) + r1) / 2 : (r0 + r1) / 2;
}
function roadGraph(W: any, PL: any) {
  const nodes: Record<string, P> = PL.nodes, edges: Edge[] = [];
  const line = (id: string) => W.ground.roads.find((r: any) => r.id === `road/${id}`)?.line as P[] | undefined;
  for (const r of PL.roads) {
    if (r.gyratory) continue;
    const wl = line(r.id); if (!wl) throw new Error(`road ${r.id}: no line in the world export`);
    const idx = (n: string) => { let bi = 0, bd = Infinity; wl.forEach((p, i) => { const d = dist(p, nodes[n]); if (d < bd) { bd = d; bi = i; } }); if (bd > 2.5) throw new Error(`node ${n} is ${bd.toFixed(1)} m off road ${r.id}`); return bi; };
    for (let i = 0; i + 1 < r.path.length; i++) {
      const a = r.path[i], b = r.path[i + 1], ia = idx(a), ib = idx(b);
      const seg = ia <= ib ? wl.slice(ia, ib + 1) : wl.slice(ib, ia + 1).reverse();
      edges.push({ id: `${r.id}/${i}`, road: r.id, cls: r.class, w: r.w, median: r.median ?? 0, tram: !!r.tram && !!r.tram.runs?.some((u: any) => u.centre), a, b, pts: seg, oneway: false });
    }
  }
  const c: P = PL.circus.c, ang = (n: string) => Math.atan2(nodes[n][1] - c[1], nodes[n][0] - c[0]), RING_R = ringLane(W, PL);
  const arc = (a: string, b: string) => { let t0 = ang(a), t1 = ang(b); while (t1 <= t0) t1 += 2 * Math.PI; const n = Math.ceil((t1 - t0) * RING_R / 2), pts: P[] = []; for (let i = 0; i <= n; i++) { const t = t0 + (t1 - t0) * i / n; pts.push([r3(c[0] + RING_R * Math.cos(t)), r3(c[1] + RING_R * Math.sin(t))]); } return pts; };
  // right-hand traffic: the gyratory runs anticlockwise, node to node by angle
  const gy = PL.roads.find((r: any) => r.gyratory), ringNodes: string[] = gy ? [...new Set<string>(gy.path)].sort((a, b) => ang(a) - ang(b)) : [];
  ringNodes.forEach((a, i) => { const b = ringNodes[(i + 1) % ringNodes.length]; edges.push({ id: `${gy.id}/${a}-${b}`, road: gy.id, cls: 'ring', w: gy.w, median: 0, tram: false, a, b, pts: arc(a, b), oneway: true }); });
  const out = new Map<string, DEdge[]>();
  for (const e of edges) {
    const f: DEdge = { e, fwd: true, from: e.a, to: e.b, pts: e.pts }; (out.get(e.a) ?? out.set(e.a, []).get(e.a)!).push(f);
    if (!e.oneway) { const r: DEdge = { e, fwd: false, from: e.b, to: e.a, pts: e.pts.slice().reverse() }; (out.get(e.b) ?? out.set(e.b, []).get(e.b)!).push(r); }
  }
  const degree = (n: string) => edges.filter(e => e.a === n || e.b === n).length;
  return { nodes, edges, out, degree, ringNodes, ringRoad: gy?.id as string | undefined };
}

// ---------- car paints: 1950s two-tones (brief 075 roll-out). The old reel (the old DecoKitCitizens) gave every car one of eight enamels over an
// ivory body, seeded per car; ours keep the enamels and pair them with ivory, both ways round, plus the two sedans' own liveries. Violets and
// lilacs are left out: violet is the rivals' car. When Vela lands her variants as kit.json `carPaints` ({ keys: { <key>: { name?, body, roof,
// hex: { body, roof } } } }: body and roof are material names), those keys replace this table and carry their materials.
const IVORY = '#EFE8D6';
export const PAINTS: [string, string, string, string][] = [ // key, name, body hex, roof hex
  ['coral-seafoam', 'coral, seafoam roof (the getaway sedan\'s own)', '#F28A70', '#72B5A7'],
  ['teal-ivory', 'deep teal, ivory roof (the official sedan\'s own)', '#1F6C67', IVORY],
  ['turquoise-ivory', 'turquoise, ivory roof', '#0A6D82', IVORY],
  ['tomato-ivory', 'tomato red, ivory roof', '#E25E47', IVORY],
  ['canary-ivory', 'canary yellow, ivory roof', '#E7B84C', IVORY],
  ['seafoam-ivory', 'seafoam, ivory roof', '#78C4A2', IVORY],
  ['cherry-ivory', 'cherry red, ivory roof', '#B53A31', IVORY],
  ['sky-ivory', 'sky blue, ivory roof', '#5F9FC6', IVORY],
  ['navy-ivory', 'navy, ivory roof', '#27406A', IVORY],
  ['green-ivory', 'Kelly green, ivory roof', '#2F7D5C', IVORY],
  ['flamingo-ivory', 'flamingo pink, ivory roof', '#E59AA6', IVORY],
  ['ivory-turquoise', 'ivory, turquoise roof', IVORY, '#0A6D82'],
  ['ivory-tomato', 'ivory, tomato roof', IVORY, '#E25E47'],
  ['ivory-canary', 'ivory, canary roof', IVORY, '#E7B84C'],
];
const OLD = 'the old reel: DecoKitCitizens.Enamels (#0A6D82 #E25E47 #E7B84C #78C4A2 #B53A31 #5F9FC6 #27406A #2F7D5C) over ivory, one per car, seeded';
export function carPaints(kitJson: any): Record<string, Paint> {
  const vela = kitJson.carPaints?.keys as Record<string, any> | undefined;
  if (vela && Object.keys(vela).length) return Object.fromEntries(Object.keys(vela).sort().map(k => { const e = vela[k]; return [k, { name: e.name ?? k, hex: { body: e.hex?.body ?? IVORY, roof: e.hex?.roof ?? IVORY }, materials: { body: e.body, roof: e.roof }, source: `kit.json carPaints (${kitJson.carPaints.source ?? 'Vela'})` } as Paint]; }));
  return Object.fromEntries(PAINTS.map(([k, name, body, roof]) => [k, { name, hex: { body, roof }, source: k.endsWith('-seafoam') || k === 'teal-ivory' ? 'the adopted sedan\'s own livery (tools/decokit/cars070.py)' : OLD } as Paint]));
}
/** every adopted kit car (Vehicles, car-sized, not a tram, robot, cart or scooter): the cars traffic and the bays draw from */
export function adoptedCars(kit: Record<string, any>) {
  return Object.values(kit).filter((a: any) => a.category === 'Vehicles' && a.status === 'adopted' && !/Tram|Robot|Cart|Scooter|Truck|Van/i.test(a.id) && a.size?.[2] >= 3 && a.size[2] <= 6.5 && a.size[1] < 2.5).map((a: any) => a.id as string).sort();
}
const RIVAL_SHARE = 0.04; // the rivals' violet sedan: rare in traffic and the bays, so it does not read as the rival car

// ---------- presentation-only draw tiers (never play): screen-height fractions of a 1.75 m figure
export const LOD: PresentationLod = {
  scope: 'Presentation scenes only (Flyby074, the golden view, the free-fly camera). Play, the missions and the figure library keep their own LOD heights and cull (the library culls a figure below 1.2 % of screen height, about 130 m at fov 60).',
  figures: [
    { from: 0.012, draw: 'library', note: 'the figure library\'s LODs and their heights, animated every frame, as now' },
    { from: 0.005, draw: 'last-lod-slow', note: 'the last LOD (1,500 tris), its pose stepped every 4th frame (13 px down to 5 px at 1080p: a walk still reads, the swap cost is a quarter)' },
    { from: 0.0015, draw: 'far-card', note: 'a far card: one GPU-instanced camera-facing quad (2 tris) per figure, 2 x 1 frames of the figure\'s last LOD baked once at install (walk mid-stride / idle) into a shared atlas, or failing that a 2-colour capsule (coat = the figure texture\'s mean, head = skin); per-instance colour, no per-frame mesh swap (5 px down to 1.6 px)' },
  ],
  cars: [
    { from: 0.004, draw: 'kit-lods', note: 'the kit piece and its own LODs (the sedans are full detail, nolod)' },
    { from: 0.0012, draw: 'far-box', note: 'a far box: one instanced rounded box in the body hex with a roof-hex top (12 tris); car screen height = 1.3 m / viewport' },
  ],
  distances: 'figures at fov 60: library to 130 m, slow last LOD to 300 m, far card to 1,010 m; at fov 40: 200 / 480 / 1,600 m. Cars (1.3 m) at fov 60: kit to 280 m, far box to 940 m.',
};

// ---------- the build
export type Options = { hour: number; people: number; cars: number; worldFile?: string };
export const DEFAULTS: Options = { hour: 14.5, people: 3000, cars: 220 };

export function build(I: Inputs, opt: Options = DEFAULTS): Presentation {
  const W = I.world, PL = I.plan, LF = I.life, band = bandOf(opt.hour);
  const kitJson = JSON.parse(readFileSync(join(ROOT, 'contracts/world/kit.json'), 'utf8'));
  const kit: Record<string, any> = Array.isArray(kitJson.assets) ? Object.fromEntries(kitJson.assets.map((a: any) => [a.id, a])) : kitJson.assets;
  const blocked = obstacles(W), skyBlocked = skyObstacles(W, kit);
  const G = roadGraph(W, PL);
  const sec = PL.standards.streetscape.sections, ts = sec.tramStreet;

  // ---- models
  const km = (key: string, kind: Model['kind'], id: string, fallback: string | null, note?: string): [string, Model] => {
    const k = kit[id]; if (!k) throw new Error(`kit piece ${id} not in kit.json`);
    return [key, { kind, kit: id, status: k.status === 'adopted' ? 'adopted' : 'candidate', tris: [k.tris, ...(k.lods ?? []).map((l: any) => l.tris)], size: k.size, pivot: k.pivot, ...(k.pivotOffset ? { pivotOffset: k.pivotOffset } : {}), fallback, ...(note ? { note } : {}) }];
  };
  const carIds = adoptedCars(kit), carKey = (id: string) => ({ Sedan_Official: 'car/official', Sedan_Getaway: 'car/getaway', Sedan_Rival: 'car/rival' } as Record<string, string>)[id] ?? `car/${id}`;
  if (!carIds.some(id => !/Rival/.test(id))) throw new Error('no adopted kit car to draw traffic with');
  const paints = carPaints(kitJson), paintKeys = Object.keys(paints);
  const models: Record<string, Model> = Object.fromEntries([
    ...carIds.map(id => km(carKey(id), 'car', id, null, /Rival/.test(id) ? `the rivals' violet sedan: kept rare (${RIVAL_SHARE * 100} %), never repainted, so it does not read as the rival car` : 'repainted per car from `paints` (picked by seed; neighbours differ)')),
    km('van/box', 'van', 'BoxTruck_Play', 'car/official', 'loading bays and 8 % of traffic (life.traffic mix: van)'),
    km('scooter/hover', 'scooter', 'HoverScooter', null, '4 % of traffic, hovering 0.35 m (life.traffic mix: hover-car, the future attached to an everyday thing)'),
    km('tram', 'tram', 'Tram', null, 'pivot on the track centreline, +Z the nose'),
    km('skyrail/head', 'skyrail', 'SkyRail_Car_Head', null, 'two head cars back to back (consist); platforms are 50 m'),
    km('aircar/coral', 'aircar', 'AirCar_Coral', null), km('aircar/teal', 'aircar', 'AirCar_Teal', null), km('aircar/saucer', 'aircar', 'AirCar_Saucer', null),
    ...I.figures.map(f => [`figure/${f}`, { kind: 'figure', figure: f, status: 'figure', tris: [3000, 1500], fallback: null, note: 'tools/figures/figures.json townsperson (crowd LODs, under the crowd budget 12k / 4.5k / 1.5k)' } as Model] as [string, Model]),
  ]);
  for (const m of Object.values(models)) { // paint slots: the kit's first two car enamels (body; roof and flash), as cars070 builds them
    if (m.kind !== 'car' && m.kind !== 'van') continue;
    const en = (kit[m.kit!].materials ?? []).filter((x: string) => x.startsWith('DK_CarEnamel_'));
    if (en.length >= 2) m.paintSlots = { body: en[0], roof: en[1] };
  }
  const plainCars = carIds.filter(id => !/Rival/.test(id)).map(carKey), rivalKey = carIds.some(id => /Rival/.test(id)) ? 'car/rival' : null;
  /** a car model by weight (rival rare, the rest shared equally), then a paint: seeded by id, never the same paint as any of `avoid` */
  const carPick = (r: () => number, other: [string, number][], avoid: (string | undefined)[]) => {
    const share = 1 - RIVAL_SHARE - other.reduce((s, x) => s + x[1], 0);
    const model = pickW(r, [...plainCars.map(k => [k, share / plainCars.length] as [string, number]), ...(rivalKey ? [[rivalKey, RIVAL_SHARE] as [string, number]] : []), ...other]);
    const drawn = models[model].paintSlots ? model : models[model].fallback; // a candidate van draws as its fallback: paint that
    if (!drawn || !models[drawn]?.paintSlots) return { model };
    let paint = pick(r, paintKeys); for (let i = 0; i < 12 && avoid.includes(paint); i++) paint = pick(r, paintKeys);
    return { model, paint };
  };
  const figureKeys = I.figures.map(f => `figure/${f}`);
  const loops: Loop[] = [], routes: Route[] = [], timed: Timed[] = [], looping: Looping[] = [], still: Still[] = [];
  const dropped: Record<string, number> = {};
  const drop = (why: string) => { dropped[why] = (dropped[why] ?? 0) + 1; };

  // ---- 1. trams and sky rail on the plan's lines (plan.tram, plan.skyrail), to the plan timetable, through-running off the sheet's edge.
  // Every line is read from the plan and the world export, never named here: a line with a `loop` path runs out along its trunk, round the
  // loop and back (one round route, departing its first stop every headway); a line without one runs through both ways (the reverse half a
  // headway later, life.transit.schedule). Stops are ordered by where they stand along the trunk.
  const tt: any[] = PL.timetable.lines, svc = (l: string) => LF.transit.services.find((s: any) => s.line === l);
  const track = (id: string) => (W.transit.tram.find((t: any) => t.id === id)?.line ?? W.transit.skyRail.find((t: any) => t.id === id)?.line) as P[] | undefined;
  type Leg = { pts: P[]; vmax: number };
  const runCruise = (D: number, T: number, a: number, cap: number) => { const disc = a * a * T * T - 4 * a * D; return Math.min(cap, disc >= 0 ? (a * T - Math.sqrt(disc)) / 2 : Math.sqrt(a * D)); };
  function transitRoute(id: string, kind: 'tram' | 'skyrail', legs: Leg[], stopsAt: { id: string; at: P }[], line: string, o: { acc: number; latAcc: number; cap: number; z: number; ext: number }) {
    let pts: P[] = []; for (const l of legs) pts = pts.length ? [...pts, ...l.pts.slice(dist(pts[pts.length - 1], l.pts[0]) < 0.05 ? 1 : 0)] : l.pts.slice();
    pts = dedupe(extend(dedupe(pts), o.ext, o.ext));
    const c = cumul(pts), legEnd: number[] = []; { let s = o.ext; for (const l of legs) { s += cumul(l.pts).at(-1)!; legEnd.push(s); } }
    const T = tt.find(x => x.line === line); if (!T) throw new Error(`${id}: line ${line} has no timetable in the plan`);
    // each stop's s, searched in order along the route (a loop line passes the same stop twice)
    const stops: Stop[] = []; let from = 0;
    for (const st of stopsAt) { let best = { s: 0, d: Infinity }; for (let s = from; s < c.at(-1)!; s += 0.5) { const d = dist(at(pts, c, s).p, st.at); if (d < best.d) best = { s, d }; } if (best.d > 2.5) throw new Error(`${id}: stop ${st.id} ${best.d.toFixed(1)} m off the track`); stops.push({ s: best.s, dwell: T.dwellSec, id: st.id }); from = best.s + 20; }
    const run = (a: string, b: string) => T.runs.find((r: any) => (r.from === `${line}/${a}` && r.to === `${line}/${b}`) || (r.from === `${line}/${b}` && r.to === `${line}/${a}`))?.sec as number | undefined;
    const legOf = (s: number) => { let i = legEnd.findIndex(e => s <= e); return i < 0 ? legs.length - 1 : i; };
    const vmax = (s: number) => {
      let v = legs[legOf(s)].vmax; const k = stops.findIndex(st => st.s > s);
      if (k > 0) { const a = stops[k - 1], b = stops[k], T0 = run(a.id, b.id); if (T0 && a.id !== b.id) v = runCruise(b.s - a.s, T0, o.acc, o.cap); } // a timetabled run: the cruise that makes the run its timetable seconds
      return v;
    };
    const pr = profile(pts, vmax, { acc: o.acc, dec: o.acc, latAcc: o.latAcc, v0: legs[0].vmax, v1: legs.at(-1)!.vmax, stops, step: 1 });
    const sm = simplify(pr.pts, pr.t, 0.05);
    routes.push({ id, kind, z: o.z, xy: sm.xy, t: sm.t, stops: pr.stops, len: r2(pr.len) });
    return { index: routes.length - 1, stops: pr.stops, dur: sm.t.at(-1)! };
  }
  const departures = (line: string, phase: number) => { const T = tt.find(x => x.line === line), out: number[] = []; for (let t = phase; t < PERIOD; t += T.headwaySec) out.push(t); return out; };
  const tramZ = W.dressing?.trams?.[0]?.z ?? 0.02, trackAt = PL.standards.streetscape.sections.tramStreet.tracks.at as number;
  const onTrack = (pts: P[], d: number, p: P) => { const pr = project(pts, p), a = at(pts, cumul(pts), pr.s); return add(a.p, right(a.dir), d); };
  const lines: { l: any; kind: 'tram' | 'skyrail'; stops: any[] }[] = [...(PL.tram ?? []).map((l: any) => ({ l, kind: 'tram' as const, stops: l.stops as any[] })), ...(PL.skyrail ?? []).map((l: any) => ({ l, kind: 'skyrail' as const, stops: l.stations as any[] }))];
  for (const T of tt) if (PERIOD % T.headwaySec) throw new Error(`timetable ${T.line}: headway ${T.headwaySec} s does not divide the ${PERIOD} s loop`);
  for (const { l, kind, stops: pst } of lines) {
    const pre = kind === 'tram' ? 'tram' : 'skyrail', trunk = track(`${pre}/${l.id}/trunk`), loop = track(`${pre}/${l.id}/loop`);
    if (!trunk) throw new Error(`${pre} ${l.id}: no trunk in the world export`);
    if (!tt.some(x => x.line === l.id)) throw new Error(`${pre} ${l.id}: no timetable line in the plan`);
    const sky = kind === 'skyrail' ? W.transit.skyRail.find((t: any) => t.id === `skyrail/${l.id}/trunk`) : null;
    const off = sky ? (sky.spacing ?? 6) / 2 : trackAt, o = sky ? { acc: 1.2, latAcc: 1.2, cap: 20, z: sky.height as number, ext: 60 } : { acc: 1.0, latAcc: 1.0, cap: 14, z: tramZ, ext: 40 }, vT = sky ? 16 : 8, vL = sky ? 10 : 6;
    const model = sky ? 'skyrail/head' : 'tram', consist = sky ? [{ model: 'skyrail/head', back: r2(kit['SkyRail_Car_Head'].size[2] + 0.4), flip: true }] : undefined;
    for (const s of pst) if (s.path !== undefined && s.path !== 'trunk') drop(`transit: ${l.id}/${s.id} stands off the trunk (path ${s.path}), not served here`);
    const order = pst.filter(s => s.path === undefined || s.path === 'trunk').map(s => ({ id: s.id as string, s: project(trunk, s.at).s, at: s.at as P })).sort((a, b) => a.s - b.s);
    const fwd = offsetLine(trunk, () => off), back = offsetLine(trunk.slice().reverse(), () => off), rev = trunk.slice().reverse();
    const sF = order.map(s => ({ id: s.id, at: onTrack(trunk, off, s.at) })), sB = order.slice().reverse().map(s => ({ id: s.id, at: onTrack(rev, off, s.at) }));
    const push = (r: { index: number; stops: { tDepart: number }[] }, phase: number, tag: string) => { const dep = r.stops[0].tDepart; departures(l.id, phase).forEach((d, k) => timed.push({ id: `${pre}/${l.id}/${tag}${k}`, model, route: r.index, t0: r2(((d - dep) % PERIOD + PERIOD) % PERIOD), ...(consist ? { consist } : {}) })); };
    if (loop) { // out on the trunk, round the loop the way that joins the outbound track, back on the other track
      const lp = dist(loop[0], fwd.at(-1)!) <= dist(loop.at(-1)!, fwd.at(-1)!) ? loop : loop.slice().reverse();
      push(transitRoute(`${pre}/${l.id}/round`, kind, [{ pts: fwd, vmax: vT }, { pts: lp, vmax: vL }, { pts: back, vmax: vT }], [...sF, ...sB], l.id, o), 0, '');
    } else { // through-running both ways
      const T = tt.find(x => x.line === l.id);
      push(transitRoute(`${pre}/${l.id}/out`, kind, [{ pts: fwd, vmax: vT }], sF, l.id, o), 0, 'out/');
      push(transitRoute(`${pre}/${l.id}/back`, kind, [{ pts: back, vmax: vT }], sB, l.id, o), T.headwaySec / 2, 'back/');
    }
  }

  // ---- 2. moving cars: trips from a sheet-edge gate through the road graph to another, on the right-hand lane
  const SPEED: Record<string, number> = { primary: LF.traffic.byClass.primary.speed, district: LF.traffic.byClass.district.speed, access: LF.traffic.byClass.access.speed, ring: 7 };
  const stopsOn = (road: string) => PL.tram.flatMap((t: any) => t.stops.filter((s: any) => s.fronts === road).map((s: any) => s.at as P));
  const laneCount = (e: Edge) => { if (e.tram || e.oneway) return 1; const half = (e.w - e.median) / 2; return Math.max(1, Math.min(2, Math.floor(half / 3.3))); };
  const laneCache = new Map<string, P[]>();
  const lanePath = (d: DEdge, lane: number): P[] => {
    const key = `${d.e.id}|${d.fwd}|${lane}`; const hit = laneCache.get(key); if (hit) return hit;
    let pts: P[];
    if (d.e.oneway) pts = d.pts;
    else if (d.e.tram) { // the traffic lane (3.6-7.2), out round each stop's boarding island into the passing lane (sections.tramStreet.*.island.passing)
      const tsc = ts.district.roads.includes(d.e.road) ? ts.district : ts.primary, base = (tsc.lanes.traffic[0] + tsc.lanes.traffic[1]) / 2, pass = (tsc.island.passing[0] + tsc.island.passing[1]) / 2, half = tsc.island.length / 2;
      const ss = (stopsOn(d.e.road) as P[]).map(p => project(d.pts, p)).filter(q => q.d < 3).map(q => q.s);
      const sm = (x: number) => x <= 0 ? 0 : x >= 1 ? 1 : x * x * (3 - 2 * x);
      pts = offsetLine(resample(d.pts, 2), s => { let w = 0; for (const s0 of ss) { const a = s0 - half - 2, b = s0 + half + 2; w = Math.max(w, s < a ? sm(1 - (a - s) / 15) : s > b ? sm(1 - (s - b) / 10) : 1); } return base + (pass - base) * w; });
    } else { const n = laneCount(d.e), half = (d.e.w - d.e.median) / 2, off = n === 1 ? d.e.median / 2 + half / 2 : d.e.median / 2 + 1.65 + 3.3 * Math.min(lane, n - 1); pts = offsetLine(resample(d.pts, 4), () => off); }
    laneCache.set(key, pts); return pts;
  };
  const roadW = (n: string) => Math.max(...G.edges.filter(e => e.a === n || e.b === n).map(e => e.w));
  const carTrim = (n: string, a: DEdge, b: DEdge) => {
    if (a.e.road === b.e.road && G.degree(n) === 2) { const d0 = endDir(a.pts), d1 = startDir(b.pts); if (d0[0] * d1[0] + d0[1] * d1[1] > 0.95) return 2; }
    return Math.max(4, roadW(n) / 2 * 0.9);
  };
  const exits = Object.keys(G.nodes).filter(n => n.startsWith('off_'));
  const deadEnd = (n: string) => G.degree(n) === 1 && !n.startsWith('off_');
  // distance to the nearest exit over the directed graph (Dijkstra from the exits backwards)
  const toExit = new Map<string, number>(exits.map(x => [x, 0])); { let changed = true; while (changed) { changed = false; for (const [n, ds] of G.out) for (const d of ds) { const v = (toExit.get(d.to) ?? Infinity) + cumul(d.pts).at(-1)!; if (v < (toExit.get(n) ?? Infinity) - 1e-6) { toExit.set(n, v); changed = true; } } } }
  const weight = (d: DEdge) => d.e.tram ? 6 : d.e.cls === 'primary' ? 3 : d.e.cls === 'district' || d.e.cls === 'ring' ? 3 : 1;
  function trip(id: string) {
    const r = rng(id), entry = pick(r, exits), target = 500 + r() * 2200, seq: DEdge[] = [];
    let node = entry, prev: DEdge | null = null, len = 0;
    for (let step = 0; step < 60; step++) {
      let cand = (G.out.get(node) ?? []).filter(d => !(prev && d.e === prev.e && d.fwd !== prev.fwd) && !deadEnd(d.to));
      if (!cand.length) return null;
      let d: DEdge;
      if (len < target) { const inner = cand.filter(c => !c.to.startsWith('off_')); d = pickW(r, (inner.length ? inner : cand).map(c => [c, weight(c)] as [DEdge, number])); }
      else d = cand.reduce((b, c) => (cumul(c.pts).at(-1)! + (toExit.get(c.to) ?? 1e9)) < (cumul(b.pts).at(-1)! + (toExit.get(b.to) ?? 1e9)) ? c : b);
      seq.push(d); len += cumul(d.pts).at(-1)!; prev = d; node = d.to;
      if (node.startsWith('off_') && step > 0) break;
    }
    if (!node.startsWith('off_') || len < 300) return null;
    const lane = r() < 0.5 ? 0 : 1;
    // geometry: lane paths trimmed at each node, joined by connectors; vmax by class
    const legs: { pts: P[]; v: number }[] = [];
    for (let i = 0; i < seq.length; i++) {
      let p = lanePath(seq[i], lane); const L = cumul(p).at(-1)!;
      const t0 = i > 0 ? carTrim(seq[i].from, seq[i - 1], seq[i]) : 0, t1 = i < seq.length - 1 ? carTrim(seq[i].to, seq[i], seq[i + 1]) : 0;
      if (t0 + t1 >= L - 0.5) return null;
      p = slice(p, t0, L - t1);
      if (i > 0) { const prevP = legs[legs.length - 1].pts; const cn = connector(prevP.at(-1)!, endDir(prevP), p[0], startDir(p)); legs.push({ pts: cn, v: Math.min(SPEED[seq[i - 1].e.cls], SPEED[seq[i].e.cls], 6) }); }
      legs.push({ pts: p, v: SPEED[seq[i].e.cls] });
    }
    let pts: P[] = [], vs: number[] = [];
    for (const l of legs) { const q = resample(l.pts, 2); for (const x of q) { if (pts.length && dist(pts.at(-1)!, x) < 0.05) continue; pts.push(x); vs.push(l.v); } }
    pts = extend(pts, 60, 60); vs = [vs[0], ...vs, vs.at(-1)!];
    const c = cumul(pts), vAt = (s: number) => { let lo = 0, hi = c.length - 1; while (hi - lo > 1) { const m = (lo + hi) >> 1; if (c[m] <= s) lo = m; else hi = m; } return Math.min(vs[lo], vs[hi]); };
    const pr = profile(pts, vAt, { acc: 1.8, dec: 2.2, latAcc: 2.5, v0: vs[0], v1: vs.at(-1)!, stops: [], step: 2 });
    if (pr.t.at(-1)! > PERIOD - 5) return null;
    return { pr, entry, r };
  }
  // departures: jittered per entry gate; accept a trip only if it never closes within 4.5 m behind/beside another car going the same way
  type Car = { pts: P[]; t: number[]; t0: number };
  const occ = new Map<string, { x: number; y: number; h: number }[]>();
  const posAt = (c: Car, tau: number) => { const t = c.t; if (tau < 0 || tau > t.at(-1)!) return null; let lo = 0, hi = t.length - 1; while (hi - lo > 1) { const m = (lo + hi) >> 1; if (t[m] <= tau) lo = m; else hi = m; } const f = (tau - t[lo]) / ((t[hi] - t[lo]) || 1), a = c.pts[lo], b = c.pts[hi]; return { x: a[0] + (b[0] - a[0]) * f, y: a[1] + (b[1] - a[1]) * f, h: heading(unit(a, b)) }; };
  const cell = (x: number, y: number, s: number) => `${s}|${Math.floor(x / 6)}|${Math.floor(y / 6)}`;
  let busy = 0, k = 0, accepted = 0;
  const lastAt = new Map<string, number>(), lastPaint = new Map<string, (string | undefined)[]>();
  while (busy < opt.cars * PERIOD && k < opt.cars * 30) {
    const id = `car/${k++}`, tr = trip(id); if (!tr) { drop('car trip: no route'); continue; }
    const r = rng(id + '/t0'), t0 = Math.floor(r() * PERIOD * 10) / 10, car: Car = { pts: tr.pr.pts, t: tr.pr.t, t0 };
    const samples: { s: number; x: number; y: number; h: number }[] = []; let clash = false;
    for (let tau = 0; tau <= car.t.at(-1)! && !clash; tau += 1) {
      const p = posAt(car, tau)!, s = Math.floor((t0 + tau) % PERIOD);
      for (let dx = -1; dx <= 1 && !clash; dx++) for (let dy = -1; dy <= 1 && !clash; dy++) for (const o of occ.get(`${s}|${Math.floor(p.x / 6) + dx}|${Math.floor(p.y / 6) + dy}`) ?? []) { if (Math.hypot(o.x - p.x, o.y - p.y) < 4.5 && Math.cos(o.h - p.h) > 0.3) { clash = true; break; } }
      samples.push({ s, ...p });
    }
    if (clash) { drop('car trip: clashes with an earlier car'); continue; }
    const recent = lastPaint.get(tr.entry) ?? [], look = carPick(tr.r, [['van/box', 0.08], ['scooter/hover', 0.04]], recent); // not the paint of the last three cars in at this gate
    lastPaint.set(tr.entry, [look.paint, ...recent].slice(0, 3));
    for (const q of samples) (occ.get(cell(q.x, q.y, q.s)) ?? occ.set(cell(q.x, q.y, q.s), []).get(cell(q.x, q.y, q.s))!).push({ x: q.x, y: q.y, h: q.h });
    const sm = simplify(car.pts, car.t, 0.1);
    routes.push({ id: `route/${id}`, kind: 'car', z: look.model === 'scooter/hover' ? 0.35 : 0, xy: sm.xy, t: sm.t, len: r2(tr.pr.len) });
    timed.push({ id, model: look.model, route: routes.length - 1, t0, ...(look.paint ? { paint: look.paint } : {}) });
    busy += car.t.at(-1)!; accepted++; lastAt.set(tr.entry, t0);
  }

  // ---- 3. parked cars in the world's kerbside bays (brief 075 tram street): about four in five bays taken
  const roadLine = (id: string) => W.ground.roads.find((r: any) => r.id === `road/${id}`).line as P[];
  const parked: { p: P; model: string; paint?: string }[] = [], NEAR = 9, RIVAL_GAP = 40; // neighbours: any parked car within 9 m (the next bay or two, the bay across an angle row)
  for (const b of W.ground.kerbside) {
    const r = rng(b.id), c = centroid(b.ring), take = b.kind === 'loading' ? 0.5 : 0.8;
    if (r() > take) continue;
    let h: number;
    if (b.kind === 'angle') h = b.yaw;
    else { const ln = roadLine(b.road), pr = project(ln, c), a = at(ln, cumul(ln), pr.s), side = a.dir[0] * (c[1] - a.p[1]) - a.dir[1] * (c[0] - a.p[0]); h = heading(side > 0 ? [-a.dir[0], -a.dir[1]] : a.dir); }
    const near = parked.filter(q => dist(q.p, c) < NEAR), rivalNear = parked.some(q => q.model === rivalKey && dist(q.p, c) < RIVAL_GAP);
    const rp = rng(b.id + '/paint');
    let look = b.kind === 'loading' ? carPick(rp, [['van/box', 1 - RIVAL_SHARE]], near.map(q => q.paint)) : carPick(rp, [], near.map(q => q.paint));
    for (let i = 0; i < 6 && (look.model === rivalKey && rivalNear || (!look.paint && near.some(q => q.model === look.model))); i++) look = carPick(rp, b.kind === 'loading' ? [['van/box', 1 - RIVAL_SHARE]] : [], near.map(q => q.paint)); // no two rival sedans within 40 m, no two unpainted twins side by side
    if (look.model === rivalKey && rivalNear) look = carPick(rng(b.id + '/plain'), [], near.map(q => q.paint));
    const model = look.model;
    const po = models[model].pivotOffset?.[1] ?? 0, p = add(c, [Math.cos(h), Math.sin(h)], -po);
    if (blocked(p)) { drop('parked car: bay inside an obstacle'); continue; }
    parked.push({ p: c, model, paint: look.paint });
    still.push({ id: `parked/${b.id.split('/').slice(2).join('/')}`, model, at: [r2(p[0]), r2(p[1])], z: b.level ?? 0, heading: r3(h), clip: 'parked', where: b.id, ...(look.paint ? { paint: look.paint } : {}) });
  }

  // ---- 4. pedestrians: the life density model (tools/life/model.ts streetDensity) at `hour`, scaled to `people` figures in all
  const dens = streetDensity(opt.hour), byId = new Map<string, any>(dens.map(d => [d.id, d]));
  const CIRCUS_WALK = (PL.circus.ring[1] + PL.circus.pavement) / 2; // the circus pavement's middle, between the ring's outer kerb and the pavement's edge
  const circusLen = 2 * Math.PI * CIRCUS_WALK, circusMove = PROM.square[band] * 8 * circusLen;
  const rawMove = dens.reduce((s, d) => s + (d.walk + d.through + d.prom) * d.len, 0) + circusMove, rawSpill = dens.reduce((s, d) => s + d.spill * d.len, 0);
  const scale = opt.people / (rawMove + rawSpill);
  const walkV = LF.pedestrians.walkSpeed as number;
  const fig = (r: () => number) => pick(r, figureKeys);
  const walkerSet = (loopIdx: number, L: number, n: number, halfW: number, id: string) => {
    for (let i = 0; i < n; i++) {
      const r = rng(`${id}/${i}`); const target = Math.max(0.9, Math.min(1.7, walkV + (r() - 0.5) * 0.5)) * (r() < 0.5 ? 1 : -1);
      // an integer number of laps per period, so the loop is seamless; the closest lap count whose speed stays in 0.9-1.75 m/s
      const laps = Math.max(1, Math.round(Math.abs(target) * PERIOD / L)); let v = laps * L / PERIOD; if (v > 1.75 && laps > 1) v = (laps - 1) * L / PERIOD; if (v < 0.9) v = (laps + 1) * L / PERIOD;
      if (v > 2.0) { drop('walker: loop too long to lap'); continue; }
      const lat = 0.15 + r() * Math.max(0.05, halfW - 0.45); // keep to your own right: lat is to the walker's left, so it is stored negative
      looping.push({ id: `walker/${id}/${i}`, model: fig(r), loop: loopIdx, s0: r2(r() * L), v: r3(Math.sign(target) * v), lat: r2(-lat), clip: 'walk' });
    }
  };
  // round a fractional count deterministically
  const count = (x: number, id: string) => Math.floor(x) + (rng(id + '/n')() < x - Math.floor(x) ? 1 : 0);
  const pushLoop = (id: string, xyz: number[], where: string, kind: Loop['kind'] = 'walk') => { const pts: P[] = []; for (let i = 0; i < xyz.length; i += 3) pts.push([xyz[i], xyz[i + 1]]); const L = cumul([...pts, pts[0]]).at(-1)!; loops.push({ id, kind, len: r2(L), xyz: xyz.map(r2), where }); return { index: loops.length - 1, L }; };
  const zPave = 0.18;
  const walkOff = (e: Edge) => { const s = sectionOf(sec, PL, e.cls === 'primary' || e.cls === 'district' ? e.cls : 'access', e.road); return { o: e.w / 2 + s.kerb + s.walk / 2, half: s.walk / 2 }; }; // pavement line offsets (and trims): kerb + walk/2 from the kerb face of the road's section as the world generator and the checker read it (sectionOf: its class's, or its own pavement, brief 075; the class's put Sunset Way east's walkers in its buildings)
  const circus = PL.circus.c as P;
  const walkTrim = (n: string, e: Edge, pts: P[], atEnd: boolean) => {
    if (n.startsWith('off_')) return 0;
    if (G.ringNodes.includes(n)) { // stop where both pavement lines reach the circus pavement's walk (its middle + 2)
      const c = cumul(pts), L = c.at(-1)!, wo = walkOff(e).o;
      for (let s = 0; s < L; s += 0.5) { const q = at(pts, c, atEnd ? L - s : s); if ([1, -1].every(sgn => dist(add(q.p, right(q.dir), sgn * wo), circus) >= CIRCUS_WALK + 2)) return s; }
      return L;
    }
    const others = G.edges.filter(x => x !== e && x.road !== G.ringRoad && (x.a === n || x.b === n));
    return others.length ? Math.max(...others.map(x => walkOff(x).o)) : 0;
  };
  let loopsDropped = 0;
  for (const e of G.edges) {
    if (e.oneway) continue;
    const d = byId.get(e.road); if (!d) continue;
    const perM = (d.walk + d.through + d.prom) * scale; // walkers per m of street, both sides
    const { o, half } = walkOff(e), pts = resample(e.pts, 2), L = cumul(pts).at(-1)!;
    const s0 = walkTrim(e.a, e, pts, false), s1 = L - walkTrim(e.b, e, pts, true); if (s1 - s0 < 20) continue;
    const k = Math.max(1, Math.ceil((s1 - s0) / 260));
    for (let j = 0; j < k; j++) {
      const a = s0 + (s1 - s0) * j / k, b = s0 + (s1 - s0) * (j + 1) / k, mid = resample(slice(pts, a, b), 4);
      const rs = offsetLine(mid, () => o), ls = offsetLine(mid, () => -o).reverse();
      const id = `${e.id}/${j}`;
      const clear = (side: P[]) => [0, half - 0.15, -(half - 0.15)].every(d => !offsetLine(side, () => d).some(blocked)); // the walkers' whole lateral spread, not just the walk's middle
      const rOk = clear(rs), lOk = clear(ls);
      if (!rOk && !lOk) { loopsDropped++; drop(`walk loop: both pavements blocked (${id})`); continue; }
      if (!rOk || !lOk) { // one pavement only (a megastructure or the civic block stands on the other): there and back on the clear one
        const side = rOk ? rs : ls; drop(`walk loop: one pavement only (${id})`);
        const lp = pushLoop(`walk/${id}`, [...side, ...side.slice(1, -1).reverse()].flatMap(p => [p[0], p[1], zPave]), `${e.road} pavement (one side)`);
        walkerSet(lp.index, lp.L, count(perM * (b - a) / 2, id), half, id); continue;
      }
      const kerbR0 = offsetLine(mid, () => e.w / 2), kerbL0 = offsetLine(mid, () => -e.w / 2);
      const xyz: number[] = [];
      for (const p of rs) xyz.push(p[0], p[1], zPave);
      xyz.push(kerbR0.at(-1)![0], kerbR0.at(-1)![1], 0, kerbL0.at(-1)![0], kerbL0.at(-1)![1], 0);
      for (const p of ls) xyz.push(p[0], p[1], zPave);
      xyz.push(kerbL0[0][0], kerbL0[0][1], 0, kerbR0[0][0], kerbR0[0][1], 0);
      const lp = pushLoop(`walk/${id}`, xyz, `${e.road} pavements`);
      walkerSet(lp.index, lp.L, count(perM * (b - a), id), half, id);
    }
  }
  // foot streets (plan foot: the mall, the walks) and the circus pavement ring
  for (const f of PL.foot) {
    const d = byId.get(f.id); if (!d) continue;
    const pts = resample(f.pts, 4), L = cumul(pts).at(-1)!, perM = (d.walk + d.through + d.prom) * scale;
    const lanes: { o: number; half: number; share: number }[] = f.kind === 'mall' ? [{ o: (sec.mall.bands.walk[0] + sec.mall.bands.walk[1]) / 2, half: (sec.mall.bands.walk[1] - sec.mall.bands.walk[0]) / 2, share: 0.55 }, { o: sec.mall.bands.centre[1] / 2, half: sec.mall.bands.centre[1] / 2 - 0.5, share: 0.45 }]
      : [{ o: Math.min(2, (f.w - 2 * sec.walk.edgeBand) / 4), half: Math.min(2, (f.w - 2 * sec.walk.edgeBand) / 4) - 0.2, share: 1 }];
    const k = Math.max(1, Math.ceil(L / 260));
    for (const [li, ln] of lanes.entries()) for (let j = 0; j < k; j++) {
      const mid = resample(slice(pts, L * j / k, L * (j + 1) / k), 4), rs = offsetLine(mid, () => ln.o), ls = offsetLine(mid, () => -ln.o).reverse(), id = `${f.id}/${li}/${j}`;
      const bad = [...rs, ...ls].filter(blocked).length; if (bad) { drop(`walk loop: a foot-street point inside an obstacle (${id})`); continue; }
      const lp = pushLoop(`walk/${id}`, [...rs, ...ls].flatMap(p => [p[0], p[1], zPave]), `${f.id}`);
      walkerSet(lp.index, lp.L, count(perM * (L / k) * ln.share, id), ln.half, id);
    }
  }
  { const n = Math.ceil(circusLen / 3), xyz: number[] = []; for (let i = 0; i < n; i++) { const t = 2 * Math.PI * i / n; xyz.push(circus[0] + CIRCUS_WALK * Math.cos(t), circus[1] + CIRCUS_WALK * Math.sin(t), zPave); }
    const pts: P[] = []; for (let i = 0; i < xyz.length; i += 3) pts.push([xyz[i], xyz[i + 1]]);
    if (!pts.some(blocked)) { const lp = pushLoop(`walk/${G.ringRoad ?? 'circus'}`, xyz, 'the circus pavement'); walkerSet(lp.index, lp.L, count(circusMove * scale, 'circus'), 1.2, G.ringRoad ?? 'circus'); } else drop('walk loop: circus ring blocked'); }

  // ---- 5. people standing: spill at open frontages (the density model's spill term, per street), waiting at stops and platforms, benches, kiosks
  const stand = (id: string, p: P, z: number, h: number, clip: Still['clip'], where: string, r: () => number) => {
    if (blocked(p)) { drop('standing: inside an obstacle'); return false; }
    still.push({ id, model: fig(r), at: [r2(p[0]), r2(p[1])], z: r2(z), heading: r3(h), clip, where }); return true;
  };
  const open = (h: [number, number] | undefined) => { if (!h) return true; const [a, b] = h, x = opt.hour; return b > 24 ? x >= a || x < b - 24 : x >= a && x < b; };
  const fronts = new Map<string, any[]>(); for (const f of W.frontages) if (f.trade && open(f.hours)) (fronts.get(f.street) ?? fronts.set(f.street, []).get(f.street)!).push(f);
  for (const d of dens) {
    const fs = (fronts.get(d.id) ?? []).map(f => { const c = CATEGORY[f.trade]; return { f, w: f.w * (c ? CAT[c].spillRho * DRAW[c][band] : 0) }; }).filter(x => x.w > 0);
    const want = d.spill * d.len * scale, tot = fs.reduce((s, x) => s + x.w, 0); if (!tot) continue;
    for (const { f, w } of fs) {
      const n = count(want * w / tot, f.id), r = rng(f.id + '/spill'), nrm: P = f.normal, along = unit(f.a, f.b), depth = Math.min(f.spill?.depth ?? 2.5, 2.8);
      for (let i = 0; i < n; i++) {
        const base = add(add(f.a, along, (0.1 + 0.8 * r()) * f.w), nrm, 0.7 + r() * Math.max(0.3, depth - 0.9));
        if (i + 1 < n && r() < 0.45) { // a pair, talking
          const h = heading(along) + (r() - 0.5) * 0.6, q = add(base, [Math.cos(h), Math.sin(h)], 0.85);
          stand(`stand/${f.id}/${i}`, base, f.z, h, 'talk', f.id, r); stand(`stand/${f.id}/${i + 1}`, q, f.z, h + Math.PI, 'talk', f.id, r); i++;
        } else stand(`stand/${f.id}/${i}`, base, f.z, heading([-nrm[0], -nrm[1]]) + (r() - 0.5) * 1.2, pickW(r, [['idle', 3], ['idle_alt', 2], ['look_around', 1], ['phone', 1]] as [Still['clip'], number][]), f.id, r);
      }
    }
  }
  // the line's own track nearest a stop or platform (its trunk or loop, from the world export): the waiting face the track and spread along it
  const nearTrack = (pre: string, line: string, c: P) => { let best: { pts: P[]; s: number; d: number } | null = null; for (const t of [...W.transit.tram, ...W.transit.skyRail]) { if (!t.id.startsWith(`${pre}/${line}/`)) continue; const q = project(t.line, c); if (!best || q.d < best.d) best = { pts: t.line, ...q }; } if (!best) throw new Error(`${pre} ${line}: no track in the world export`); return at(best.pts, cumul(best.pts), best.s); };
  for (const st of W.transit.stops) { // boarding islands: the load per service (life.transit) waiting on the track side, facing the track
    const [, line, stop] = st.id.split('/'), s = svc(line), load = (s?.loadPerService?.[stop] ?? 4) * (LF.transit.loadByBand[band] ?? 0.6) / 2;
    const ring: P[] = st.ring, c = centroid(ring), a = nearTrack('tram', line, c), toward = unit(c, a.p), r = rng(st.id);
    const n = count(load + 1, st.id);
    for (let i = 0; i < n; i++) { const p = add(add(c, toward, 0.55 + r() * 0.3), a.dir, (r() - 0.5) * 24); stand(`wait/${st.id}/${i}`, p, st.base + st.height, heading(toward) + (r() - 0.5) * 0.8, r() < 0.2 ? 'phone' : 'idle', st.id, r); }
  }
  for (const st of W.transit.stations) { // sky rail platforms: along the platform, a step back from its track edge, facing the track
    const ring: P[] = st.ring, c = centroid(ring), r = rng(st.id), [, line, stn] = st.id.split('/'), load = (svc(line)?.loadPerService?.[stn] ?? 12) * (LF.transit.loadByBand[band] ?? 0.6) / 4;
    const a = nearTrack('skyrail', line, c), toward = unit(c, a.p), len = Math.max(...ring.map(p => Math.abs((p[0] - c[0]) * a.dir[0] + (p[1] - c[1]) * a.dir[1]))) * 2;
    for (let i = 0, n = count(load, st.id); i < n; i++) { const p = add(add(c, a.dir, (r() - 0.5) * (len - 4)), toward, 0.3 + r() * 1.0); stand(`wait/${st.id}/${i}`, p, st.base + st.height, heading(toward) + (r() - 0.5) * 0.8, 'idle', st.id, r); }
  }
  for (const it of W.street) {
    const r = rng(it.id + '/use'), front: P = [Math.cos(it.yaw + Math.PI / 2), Math.sin(it.yaw + Math.PI / 2)];
    if (it.kind === 'bench' || it.kind === 'round-bench') { if (r() < 0.22) stand(`sit/${it.id}`, add(it.at, front, 0.25), it.z, heading(front), 'sit', it.id, r); }
    else if (['newsstand', 'kiosk-food', 'kiosk-pavilion', 'kiosk-tickets'].includes(it.kind)) {
      const n = (r() < 0.75 ? 1 : 0) + (r() < 0.35 ? 1 : 0), dpt = (it.size?.[1] ?? 1.5) / 2;
      for (let i = 0; i < n; i++) stand(`queue/${it.id}/${i}`, add(add(it.at, front, dpt + 0.7 + i * 0.8), [front[1], -front[0]], (r() - 0.5) * 0.6), it.z, heading(front) + Math.PI, 'idle', it.id, r);
    } else if (it.kind === 'phone-booth' && r() < 0.15) stand(`phone/${it.id}`, add(it.at, front, 0.9), it.z, heading(front) + Math.PI, 'phone', it.id, r);
  }

  // ---- 6. aircars: two sky lanes round the sheet (anticlockwise low, clockwise high) and one along the main tram street, each routed clear of
  // every structure (skyObstacles, 4 m clear): the lanes come from the plan (its sheet, its tram-carrying district road), never from fixed
  // coordinates, and the first inset and height that clear every point win (deterministic search). A lane nothing clears is dropped and counted.
  const sky = (id: string, pts: P[], z: number, n: number, vT: number, models3: string[], where: string, dir = 1) => {
    const xyz = pts.flatMap(p => [p[0], p[1], z]);
    const lp = pushLoop(`sky/${id}`, xyz, where, 'sky'), laps = Math.max(1, Math.round(vT * PERIOD / lp.L)), v = laps * lp.L / PERIOD;
    for (let i = 0; i < n; i++) { const r = rng(`aircar/${id}/${i}`); looping.push({ id: `aircar/${id}/${i}`, model: pick(r, models3), loop: lp.index, s0: r2(lp.L * (i + r() * 0.6) / n), v: r3(dir * v), lat: r2((r() - 0.5) * 3), clip: 'fly', bob: 0.25 }); }
  };
  const rounded = (corners: P[], R: number) => { const out: P[] = []; for (let i = 0; i < corners.length; i++) { const p0 = corners[(i + corners.length - 1) % corners.length], p = corners[i], p1 = corners[(i + 1) % corners.length]; const a = add(p, unit(p, p0), R), b = add(p, unit(p, p1), R); out.push(...connector(a, unit(p0, p), b, unit(p, p1)).filter((_, j, xs) => j < xs.length)); } return resample([...out, out[0]], 6).slice(0, -1); };
  const LANE_HALF = 1.5; // an aircar's lateral wander (lat +-1.5) is kept clear too
  const laneClear = (pts: P[], z: number) => !pts.some(p => skyBlocked(p, z, 4)) && !offsetLine([...pts, pts[0]], () => LANE_HALF).some(p => skyBlocked(p, z, 4)) && !offsetLine([...pts, pts[0]], () => -LANE_HALF).some(p => skyBlocked(p, z, 4));
  const zFrom = (pts: P[], lo: number, hi: number) => { for (let z = lo; z <= hi; z += 2) if (laneClear(pts, z)) return z; return undefined; };
  { // the ring: a rounded rectangle (corners 40 m) inside the sheet, at the lowest height (36-56 m) where some inset clears; from the innermost clear
    // inset each side then steps out 20 m at a time (west, south, east, north in turn) while the lane stays clear, so it is as wide as the
    // structures let it be. The high lane flies the same ring the other way, at least 8 m above.
    const [x0, x1] = PL.sheet.x as [number, number], [y0, y1] = PL.sheet.y as [number, number], STEP = 20, MIN = 40, maxK = Math.min(x1 - x0, y1 - y0) / 2 - 60;
    const ring = (k: number[]) => rounded([[x0 + k[0], y0 + k[1]], [x1 - k[2], y0 + k[1]], [x1 - k[2], y1 - k[3]], [x0 + k[0], y1 - k[3]]], 40);
    let done = false;
    for (let z = 36; z <= 56 && !done; z += 2) {
      let k0: number | undefined; for (let k = maxK; k >= MIN; k -= STEP) { if (laneClear(ring([k, k, k, k]), z)) k0 = k; else if (k0 !== undefined) break; }
      if (k0 === undefined) continue;
      const k = [k0, k0, k0, k0]; for (let grew = true; grew;) { grew = false; for (let i = 0; i < 4; i++) { if (k[i] - STEP < MIN) continue; const t = k.slice(); t[i] -= STEP; if (laneClear(ring(t), z)) { k[i] = t[i]; grew = true; } } }
      const ringPts = ring(k), hi = zFrom(ringPts, z + 8, z + 24); if (hi === undefined) continue;
      const where = `inside the sheet (in ${k.join(' / ')} m from the west / south / east / north edges)`;
      sky('ring-low', ringPts, z, 9, 18, ['aircar/coral', 'aircar/teal'], `${where}, anticlockwise, ${z} m`);
      sky('ring-high', ringPts.slice().reverse(), hi, 7, 20, ['aircar/coral', 'aircar/teal', 'aircar/saucer'], `${where}, clockwise, ${hi} m`);
      done = true;
    }
    if (!done) drop('aircar lane ring: no ring and height clear of every structure');
  }
  { // the spine: up and down the longest tram-carrying district road, 7 m each side of its line, turning at its ends; 34-56 m
    const tr = G.edges.filter(e => e.tram && e.cls === 'district'), road = [...new Set(tr.map(e => e.road))].map(id => ({ id, L: tr.filter(e => e.road === id).reduce((s, e) => s + cumul(e.pts).at(-1)!, 0) })).sort((a, b) => b.L - a.L || (a.id < b.id ? -1 : 1))[0];
    const ln = road ? (W.ground.roads.find((r: any) => r.id === `road/${road.id}`)?.line as P[] | undefined) : undefined;
    if (!ln) drop('aircar lane spine: no tram-carrying district road');
    else {
      const base = resample(ln, 8), up = offsetLine(base, () => 7), down = offsetLine(base.slice().reverse(), () => 7);
      const spine = dedupe([...up, ...connector(up.at(-1)!, endDir(up), down[0], startDir(down)).slice(1, -1), ...down, ...connector(down.at(-1)!, endDir(down), up[0], startDir(up)).slice(1, -1)]);
      const z = zFrom(spine, 34, 56);
      if (z === undefined) drop(`aircar lane spine: no height clear over ${road.id}`);
      else sky('spine', spine, z, 8, 16, ['aircar/coral', 'aircar/teal'], `over ${road.id}, both ways, ${z} m`);
    }
  }

  // ---- counts and the file
  const by = (f: (m: Model) => boolean) => [...timed, ...looping, ...still].filter(x => f(models[x.model])).length;
  const movingCarsAt = (tau: number) => timed.filter(x => routes[x.route].kind === 'car').filter(x => { const d = ((tau - x.t0) % PERIOD + PERIOD) % PERIOD; return d <= routes[x.route].t.at(-1)!; }).length;
  const tally = (xs: string[]) => Object.fromEntries([...xs.reduce((m, x) => m.set(x, (m.get(x) ?? 0) + 1), new Map<string, number>())].sort((a, b) => a[0] < b[0] ? -1 : 1));
  const meanMoving = Math.round([0, 90, 180, 270, 360, 450, 540, 630].reduce((s, t) => s + movingCarsAt(t), 0) / 8);
  const counts = {
    figures: by(m => m.kind === 'figure'), walkers: looping.filter(x => x.clip === 'walk').length, standing: still.filter(x => x.clip !== 'parked').length,
    carTrips: timed.filter(x => routes[x.route].kind === 'car').length, carsMovingMean: meanMoving, carsParked: still.filter(x => x.clip === 'parked').length,
    trams: timed.filter(x => models[x.model].kind === 'tram').length, skyrailTrains: timed.filter(x => models[x.model].kind === 'skyrail').length, aircars: looping.filter(x => x.clip === 'fly').length,
    walkLoops: loops.filter(l => l.kind === 'walk').length, routes: routes.length, walkLoopsDropped: loopsDropped, dropped,
    carModels: tally([...timed.filter(x => routes[x.route].kind === 'car'), ...still.filter(x => x.clip === 'parked')].map(x => x.model)),
    carPaints: tally([...timed, ...still].map(x => x.paint).filter((x): x is string => !!x)),
    parkedNeighboursSamePaint: still.filter(x => x.clip === 'parked' && x.paint).reduce((n, x, i, xs) => n + xs.slice(0, i).filter(y => y.paint === x.paint && dist(y.at, x.at) < NEAR).length, 0),
  };
  const rel = (f: string) => relative(ROOT, f);
  return {
    schema: 'suborned.presentation/1',
    notSim: 'Presentation only, for scenes that are not play (Flyby074, the free-fly camera). Never sim data: the sim, its site builder and the mission tools never read this file; play and the Handover keep their own crowd.',
    source: {
      generator: 'tools/life/presentation.ts (Pulse, brief 075 golden: life)',
      world: { file: rel(I.files.world), sha256: sha(I.files.world) }, plan: { id: PL.id, version: PL.version, sha256: sha(I.files.plan) },
      life: { file: rel(I.files.life), sha256: sha(I.files.life) }, figures: { file: rel(I.files.figures), sha256: sha(I.files.figures) },
    },
    frame: 'plan frame: x east, y north, metres; Unity position = (x, z, y); z is a hint, snap to the drawn ground',
    heading: 'radians, 0 = +x (east), pi/2 = +y (north); models front +Z: Unity eulerY = 90 - deg(heading)',
    clock: { periodSec: PERIOD, hour: opt.hour, band, sample: 'tau = t mod periodSec; timed: d = (tau - t0) mod periodSec, active while d <= route.t[last], position = linear in t between route points; looping: s = (s0 + v * tau) mod loop.len, offset lat to the walker\'s left; still: fixed. See briefs/075/notes.md "Golden: life" and tools/life/presentation-sample.ts.' },
    models, paints, lod: LOD, loops, routes, timed, looping, still, counts,
    laterForma: [
      'The old RetroFutureMobility cars, vans and aircars (the old reel\'s traffic) are over the vehicle budget: a civilian set at budget (sedan colourways, coupe, delivery van, hover-car) is a later Forma item; until then traffic uses Sedan_Official / Sedan_Getaway / Sedan_Rival, BoxTruck_Play and HoverScooter.',
      'AirCar_Coral / AirCar_Teal / AirCar_Saucer, SkyRail_Car_Head, BoxTruck_Play and HoverScooter are kit candidates (not copied into Unity): adoption is Vela\'s, with Sky\'s yes.',
    ],
  };
}

if (import.meta.main) {
  const arg = (k: string) => { const i = process.argv.indexOf(k); return i > 0 ? process.argv[i + 1] : undefined; };
  const opt: Options = { hour: +(arg('--hour') ?? DEFAULTS.hour), people: +(arg('--people') ?? DEFAULTS.people), cars: +(arg('--cars') ?? DEFAULTS.cars) };
  const worldFile = arg('--world') ? join(process.cwd(), arg('--world')!) : join(ROOT, WORLD_FILE), out = arg('--out') ? join(process.cwd(), arg('--out')!) : join(ROOT, PRESENTATION_FILE);
  const p = build(loadInputs(worldFile), opt);
  mkdirSync(dirname(out), { recursive: true }); writeFileSync(out, JSON.stringify(p) + '\n');
  console.log(`wrote ${relative(ROOT, out)} (${(readFileSync(out).length / 1e6).toFixed(1)} MB)`); console.log(JSON.stringify(p.counts));
}
