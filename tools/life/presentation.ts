// Presentation life for the flyby and free-fly scenes (brief 075 golden view, Pulse): cars on the carriageway lanes and in the kerbside bays,
// trams on T1 and T2 to the plan timetable, sky rail trains on S1, pedestrians on the pavements and at the stops, a few aircars.
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
import type { Presentation, Model, Loop, Route, Timed, Looping, Still } from '../../contracts/world/presentation.schema';

const ROOT = join(import.meta.dir, '..', '..');
export const PRESENTATION_FILE = 'out/world/harbour-district.presentation.json';
export const WORLD_FILE = 'out/world/harbour-district.world.json';
const PLAN_FILE = 'contracts/world/harbour-district.plan.json', LIFE_FILE = 'contracts/world/harbour-district.life.json', FIG_FILE = 'tools/figures/figures.json';
export const PERIOD = 720; // s: T1 180, T2 240, S1 120 headways all divide it; one Handover-length loop

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
function skyObstacles(W: any, kit: Record<string, any>) {
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
const RING_R = 40.5; // the circus car lane: outside the T1 loop's corners (r 39.6 - half a tram), inside the ring's outer kerb (43)
function roadGraph(W: any, PL: any) {
  const nodes: Record<string, P> = PL.nodes, edges: Edge[] = [];
  const line = (id: string) => W.ground.roads.find((r: any) => r.id === `road/${id}`)?.line as P[] | undefined;
  for (const r of PL.roads) {
    if (r.id === 'crown-circus') continue;
    const wl = line(r.id); if (!wl) throw new Error(`road ${r.id}: no line in the world export`);
    const idx = (n: string) => { let bi = 0, bd = Infinity; wl.forEach((p, i) => { const d = dist(p, nodes[n]); if (d < bd) { bd = d; bi = i; } }); if (bd > 2.5) throw new Error(`node ${n} is ${bd.toFixed(1)} m off road ${r.id}`); return bi; };
    for (let i = 0; i + 1 < r.path.length; i++) {
      const a = r.path[i], b = r.path[i + 1], ia = idx(a), ib = idx(b);
      const seg = ia <= ib ? wl.slice(ia, ib + 1) : wl.slice(ib, ia + 1).reverse();
      edges.push({ id: `${r.id}/${i}`, road: r.id, cls: r.class, w: r.w, median: r.median ?? 0, tram: !!r.tram && !!r.tram.runs?.some((u: any) => u.centre), a, b, pts: seg, oneway: false });
    }
  }
  const c: P = PL.circus.c, ang = (n: string) => Math.atan2(nodes[n][1] - c[1], nodes[n][0] - c[0]);
  const arc = (a: string, b: string) => { let t0 = ang(a), t1 = ang(b); while (t1 <= t0) t1 += 2 * Math.PI; const n = Math.ceil((t1 - t0) * RING_R / 2), pts: P[] = []; for (let i = 0; i <= n; i++) { const t = t0 + (t1 - t0) * i / n; pts.push([r3(c[0] + RING_R * Math.cos(t)), r3(c[1] + RING_R * Math.sin(t))]); } return pts; };
  // right-hand traffic: the circus runs anticlockwise (c_s -> east -> c_n -> c_w -> c_s)
  for (const [a, b] of [['c_s', 'c_n'], ['c_n', 'c_w'], ['c_w', 'c_s']]) edges.push({ id: `crown-circus/${a}-${b}`, road: 'crown-circus', cls: 'ring', w: 12, median: 0, tram: false, a, b, pts: arc(a, b), oneway: true });
  const out = new Map<string, DEdge[]>();
  for (const e of edges) {
    const f: DEdge = { e, fwd: true, from: e.a, to: e.b, pts: e.pts }; (out.get(e.a) ?? out.set(e.a, []).get(e.a)!).push(f);
    if (!e.oneway) { const r: DEdge = { e, fwd: false, from: e.b, to: e.a, pts: e.pts.slice().reverse() }; (out.get(e.b) ?? out.set(e.b, []).get(e.b)!).push(r); }
  }
  const degree = (n: string) => edges.filter(e => e.a === n || e.b === n).length;
  return { nodes, edges, out, degree };
}

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
  const models: Record<string, Model> = Object.fromEntries([
    km('car/official', 'car', 'Sedan_Official', null), km('car/getaway', 'car', 'Sedan_Getaway', null), km('car/rival', 'car', 'Sedan_Rival', null, 'the rivals\' violet sedan: kept rare (8 %) so it does not read as the rival car'),
    km('van/box', 'van', 'BoxTruck_Play', 'car/official', 'loading bays and 8 % of traffic (life.traffic mix: van)'),
    km('scooter/hover', 'scooter', 'HoverScooter', null, '4 % of traffic, hovering 0.35 m (life.traffic mix: hover-car, the future attached to an everyday thing)'),
    km('tram', 'tram', 'Tram', null, 'pivot on the track centreline, +Z the nose'),
    km('skyrail/head', 'skyrail', 'SkyRail_Car_Head', null, 'two head cars back to back (consist); platforms are 50 m'),
    km('aircar/coral', 'aircar', 'AirCar_Coral', null), km('aircar/teal', 'aircar', 'AirCar_Teal', null), km('aircar/saucer', 'aircar', 'AirCar_Saucer', null),
    ...I.figures.map(f => [`figure/${f}`, { kind: 'figure', figure: f, status: 'figure', tris: [3000, 1500], fallback: null, note: 'tools/figures/figures.json townsperson (crowd LODs, under the crowd budget 12k / 4.5k / 1.5k)' } as Model] as [string, Model]),
  ]);
  const figureKeys = I.figures.map(f => `figure/${f}`);
  const loops: Loop[] = [], routes: Route[] = [], timed: Timed[] = [], looping: Looping[] = [], still: Still[] = [];
  const dropped: Record<string, number> = {};
  const drop = (why: string) => { dropped[why] = (dropped[why] ?? 0) + 1; };

  // ---- 1. trams (T1, T2) and sky rail (S1) to the plan timetable, through-running off the sheet's edge
  const tt: any[] = PL.timetable.lines, svc = (l: string) => LF.transit.services.find((s: any) => s.line === l);
  const track = (id: string) => (W.transit.tram.find((t: any) => t.id === id)?.line ?? W.transit.skyRail.find((t: any) => t.id === id)?.line) as P[];
  type Leg = { pts: P[]; vmax: number };
  const runCruise = (D: number, T: number, a: number, cap: number) => { const disc = a * a * T * T - 4 * a * D; return Math.min(cap, disc >= 0 ? (a * T - Math.sqrt(disc)) / 2 : Math.sqrt(a * D)); };
  function transitRoute(id: string, kind: 'tram' | 'skyrail', legs: Leg[], stopsAt: { id: string; at: P }[], line: string, o: { acc: number; latAcc: number; cap: number; z: number; ext: number }) {
    let pts: P[] = []; for (const l of legs) pts = pts.length ? [...pts, ...l.pts.slice(dist(pts[pts.length - 1], l.pts[0]) < 0.05 ? 1 : 0)] : l.pts.slice();
    pts = dedupe(extend(dedupe(pts), o.ext, o.ext));
    const c = cumul(pts), legEnd: number[] = []; { let s = o.ext; for (const l of legs) { s += cumul(l.pts).at(-1)!; legEnd.push(s); } }
    // each stop's s, searched in order along the route (a loop line passes the same stop twice)
    const stops: Stop[] = []; let from = 0;
    for (const st of stopsAt) { let best = { s: 0, d: Infinity }; for (let s = from; s < c.at(-1)!; s += 0.5) { const d = dist(at(pts, c, s).p, st.at); if (d < best.d) best = { s, d }; } if (best.d > 2.5) throw new Error(`${id}: stop ${st.id} ${best.d.toFixed(1)} m off the track`); stops.push({ s: best.s, dwell: tt.find(x => x.line === line).dwellSec, id: st.id }); from = best.s + 20; }
    const T = tt.find(x => x.line === line), run = (a: string, b: string) => T.runs.find((r: any) => (r.from === `${line}/${a}` && r.to === `${line}/${b}`) || (r.from === `${line}/${b}` && r.to === `${line}/${a}`))?.sec as number | undefined;
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
  const departures = (line: string, phase: number) => { const s = svc(line), out: number[] = []; for (let t = phase; t < PERIOD; t += s.headwaySec) out.push(t); return out; };
  const tramZ = W.dressing?.trams?.[0]?.z ?? 0.02, trackAt = PL.standards.streetscape.sections.tramStreet.tracks.at as number;
  const plTram = (l: string) => PL.tram.find((t: any) => t.id === l), stopPt = (l: string, s: string) => (plTram(l)?.stops.find((x: any) => x.id === s) ?? PL.skyrail.find((x: any) => x.id === l).stations.find((x: any) => x.id === s)).at as P;
  const onTrack = (pts: P[], d: number, p: P) => { const pr = project(pts, p), a = at(pts, cumul(pts), pr.s); return add(a.p, right(a.dir), d); };
  { // T1: north on the trunk (east track), round the circus loop anticlockwise, south on the west track; departs Comet Row northbound every headway
    const trunk = track('tram/T1/trunk'), loop = track('tram/T1/loop'), north = offsetLine(trunk, () => trackAt), south = offsetLine(trunk.slice().reverse(), () => trackAt);
    const order = ['comet', 'records', 'circus'], sN = order.map(s => ({ id: s, at: onTrack(trunk, trackAt, stopPt('T1', s)) })), sS = order.slice().reverse().map(s => ({ id: s, at: onTrack(trunk.slice().reverse(), trackAt, stopPt('T1', s)) }));
    const r = transitRoute('tram/T1/round', 'tram', [{ pts: north, vmax: 8 }, { pts: loop.slice().reverse(), vmax: 6 }, { pts: south, vmax: 8 }], [...sN, ...sS], 'T1', { acc: 1.0, latAcc: 1.0, cap: 14, z: tramZ, ext: 40 });
    const dep = r.stops[0].tDepart; departures('T1', 0).forEach((d, k) => timed.push({ id: `tram/T1/${k}`, model: 'tram', route: r.index, t0: r2(((d - dep) % PERIOD + PERIOD) % PERIOD) }));
  }
  { // T2: through-running both ways; harbour -> terraces departs Harbour Gate every headway, the reverse half a headway later (life.transit.schedule)
    const ln = track('tram/T2/harbour'), order = svc('T2').stops as string[];
    for (const [dir, pts, stops, phase] of [['south', ln, order, 0], ['north', ln.slice().reverse(), order.slice().reverse(), svc('T2').headwaySec / 2]] as [string, P[], string[], number][]) {
      const lane = offsetLine(pts, () => trackAt), sp = stops.map(s => ({ id: s, at: onTrack(pts, trackAt, stopPt('T2', s)) }));
      const r = transitRoute(`tram/T2/${dir}`, 'tram', [{ pts: lane, vmax: 8 }], sp, 'T2', { acc: 1.0, latAcc: 1.0, cap: 14, z: tramZ, ext: 40 });
      const dep = r.stops[0].tDepart; departures('T2', phase).forEach((d, k) => timed.push({ id: `tram/T2/${dir}/${k}`, model: 'tram', route: r.index, t0: r2(((d - dep) % PERIOD + PERIOD) % PERIOD) }));
    }
  }
  { // S1: east on the south beam, round the skyport loop, west on the north beam; two head cars back to back
    const trunk = track('skyrail/S1/trunk'), loop = track('skyrail/S1/loop'), half = (W.transit.skyRail[0].spacing ?? 6) / 2;
    const east = offsetLine(trunk, () => half), west = offsetLine(trunk.slice().reverse(), () => half), order = svc('S1').stops as string[];
    const sE = order.map(s => ({ id: s, at: onTrack(trunk, half, stopPt('S1', s)) })), sW = order.slice().reverse().map(s => ({ id: s, at: onTrack(trunk.slice().reverse(), half, stopPt('S1', s)) }));
    const r = transitRoute('skyrail/S1/round', 'skyrail', [{ pts: east, vmax: 16 }, { pts: loop.slice().reverse(), vmax: 10 }, { pts: west, vmax: 16 }], [...sE, ...sW], 'S1', { acc: 1.2, latAcc: 1.2, cap: 20, z: W.transit.skyRail[0].height, ext: 60 });
    const dep = r.stops[0].tDepart, car = kit['SkyRail_Car_Head'].size[2];
    departures('S1', 0).forEach((d, k) => timed.push({ id: `skyrail/S1/${k}`, model: 'skyrail/head', route: r.index, t0: r2(((d - dep) % PERIOD + PERIOD) % PERIOD), consist: [{ model: 'skyrail/head', back: r2(car + 0.4), flip: true }] }));
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
  const weight = (d: DEdge) => d.e.road === 'boulevard' ? 6 : d.e.cls === 'primary' ? 3 : d.e.cls === 'district' || d.e.cls === 'ring' ? 3 : 1;
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
    const lane = r() < 0.5 ? 0 : 1, model = pickW(r, [['car/official', 40], ['car/getaway', 40], ['car/rival', 8], ['van/box', 8], ['scooter/hover', 4]] as [string, number][]);
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
    return { pr, model, entry };
  }
  // departures: jittered per entry gate; accept a trip only if it never closes within 4.5 m behind/beside another car going the same way
  type Car = { pts: P[]; t: number[]; t0: number };
  const occ = new Map<string, { x: number; y: number; h: number }[]>();
  const posAt = (c: Car, tau: number) => { const t = c.t; if (tau < 0 || tau > t.at(-1)!) return null; let lo = 0, hi = t.length - 1; while (hi - lo > 1) { const m = (lo + hi) >> 1; if (t[m] <= tau) lo = m; else hi = m; } const f = (tau - t[lo]) / ((t[hi] - t[lo]) || 1), a = c.pts[lo], b = c.pts[hi]; return { x: a[0] + (b[0] - a[0]) * f, y: a[1] + (b[1] - a[1]) * f, h: heading(unit(a, b)) }; };
  const cell = (x: number, y: number, s: number) => `${s}|${Math.floor(x / 6)}|${Math.floor(y / 6)}`;
  let busy = 0, k = 0, accepted = 0;
  const lastAt = new Map<string, number>();
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
    for (const q of samples) (occ.get(cell(q.x, q.y, q.s)) ?? occ.set(cell(q.x, q.y, q.s), []).get(cell(q.x, q.y, q.s))!).push({ x: q.x, y: q.y, h: q.h });
    const sm = simplify(car.pts, car.t, 0.1);
    routes.push({ id: `route/${id}`, kind: 'car', z: tr.model === 'scooter/hover' ? 0.35 : 0, xy: sm.xy, t: sm.t, len: r2(tr.pr.len) });
    timed.push({ id, model: tr.model, route: routes.length - 1, t0 });
    busy += car.t.at(-1)!; accepted++; lastAt.set(tr.entry, t0);
  }

  // ---- 3. parked cars in the world's kerbside bays (brief 075 tram street): about four in five bays taken
  const roadLine = (id: string) => W.ground.roads.find((r: any) => r.id === `road/${id}`).line as P[];
  for (const b of W.ground.kerbside) {
    const r = rng(b.id), c = centroid(b.ring), take = b.kind === 'loading' ? 0.5 : 0.8;
    if (r() > take) continue;
    let h: number;
    if (b.kind === 'angle') h = b.yaw;
    else { const ln = roadLine(b.road), pr = project(ln, c), a = at(ln, cumul(ln), pr.s), side = a.dir[0] * (c[1] - a.p[1]) - a.dir[1] * (c[0] - a.p[0]); h = heading(side > 0 ? [-a.dir[0], -a.dir[1]] : a.dir); }
    const model = b.kind === 'loading' ? 'van/box' : pickW(r, [['car/official', 45], ['car/getaway', 45], ['car/rival', 10]] as [string, number][]);
    const po = models[model].pivotOffset?.[1] ?? 0, p = add(c, [Math.cos(h), Math.sin(h)], -po);
    if (blocked(p)) { drop('parked car: bay inside an obstacle'); continue; }
    still.push({ id: `parked/${b.id.split('/').slice(2).join('/')}`, model, at: [r2(p[0]), r2(p[1])], z: b.level ?? 0, heading: r3(h), clip: 'parked', where: b.id });
  }

  // ---- 4. pedestrians: the life density model (tools/life/model.ts streetDensity) at `hour`, scaled to `people` figures in all
  const dens = streetDensity(opt.hour), byId = new Map<string, any>(dens.map(d => [d.id, d]));
  const circusLen = 2 * Math.PI * 46.5, circusMove = PROM.square[band] * 8 * circusLen;
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
  // pavement line offsets and trims (sections.<class>: kerb + walk/2 from the kerb face)
  const walkOff = (e: Edge) => { const s = e.cls === 'primary' ? sec.primary : e.cls === 'district' ? sec.district : sec.access; return { o: e.w / 2 + s.kerb + s.walk / 2, half: s.walk / 2 }; };
  const circus = PL.circus.c as P;
  const walkTrim = (n: string, e: Edge, pts: P[], atEnd: boolean) => {
    if (n.startsWith('off_')) return 0;
    if (['c_s', 'c_w', 'c_n'].includes(n)) { // stop where both pavement lines reach the circus pavement's walk (r 46.5 + 2)
      const c = cumul(pts), L = c.at(-1)!, wo = walkOff(e).o;
      for (let s = 0; s < L; s += 0.5) { const q = at(pts, c, atEnd ? L - s : s); if ([1, -1].every(sgn => dist(add(q.p, right(q.dir), sgn * wo), circus) >= 48.5)) return s; }
      return L;
    }
    const others = G.edges.filter(x => x !== e && x.road !== 'crown-circus' && (x.a === n || x.b === n));
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
      const rOk = !rs.some(blocked), lOk = !ls.some(blocked);
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
  { const n = Math.ceil(circusLen / 3), xyz: number[] = []; for (let i = 0; i < n; i++) { const t = 2 * Math.PI * i / n; xyz.push(circus[0] + 46.5 * Math.cos(t), circus[1] + 46.5 * Math.sin(t), zPave); }
    const pts: P[] = []; for (let i = 0; i < xyz.length; i += 3) pts.push([xyz[i], xyz[i + 1]]);
    if (!pts.some(blocked)) { const lp = pushLoop('walk/crown-circus', xyz, 'the Crown Circus pavement'); walkerSet(lp.index, lp.L, count(circusMove * scale, 'circus'), 1.2, 'crown-circus'); } else drop('walk loop: circus ring blocked'); }

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
  for (const st of W.transit.stops) { // boarding islands: the load per service (life.transit) waiting on the track side, facing the track
    const [, line, stop, side] = st.id.split('/'), s = svc(line), load = (s?.loadPerService?.[stop] ?? 4) * (LF.transit.loadByBand[band] ?? 0.6) / 2;
    const ring: P[] = st.ring, c = centroid(ring), trk = track(line === 'T1' ? 'tram/T1/trunk' : 'tram/T2/harbour'), q = project(trk, c), a = at(trk, cumul(trk), q.s), toward = unit(c, a.p), r = rng(st.id);
    const n = count(load + 1, st.id);
    for (let i = 0; i < n; i++) { const p = add(add(c, toward, 0.55 + r() * 0.3), a.dir, (r() - 0.5) * 24); stand(`wait/${st.id}/${i}`, p, st.base + st.height, heading(toward) + (r() - 0.5) * 0.8, r() < 0.2 ? 'phone' : 'idle', st.id, r); }
    void side;
  }
  for (const st of W.transit.stations) { // sky rail platforms
    const ring: P[] = st.ring, c = centroid(ring), r = rng(st.id), [, , stn] = st.id.split('/'), load = (svc('S1')?.loadPerService?.[stn] ?? 12) * (LF.transit.loadByBand[band] ?? 0.6) / 4;
    const xs = ring.map(p => p[0]), ys = ring.map(p => p[1]), lenX = Math.max(...xs) - Math.min(...xs), towardTrack: P = [0, c[1] > 0 ? -1 : 1];
    for (let i = 0, n = count(load, st.id); i < n; i++) { const p: P = [c[0] + (r() - 0.5) * (lenX - 4), c[1] + towardTrack[1] * (0.3 + r() * 1.0)]; stand(`wait/${st.id}/${i}`, p, st.base + st.height, heading(towardTrack) + (r() - 0.5) * 0.8, 'idle', st.id, r); }
  }
  for (const it of W.street) {
    const r = rng(it.id + '/use'), front: P = [Math.cos(it.yaw + Math.PI / 2), Math.sin(it.yaw + Math.PI / 2)];
    if (it.kind === 'bench' || it.kind === 'round-bench') { if (r() < 0.22) stand(`sit/${it.id}`, add(it.at, front, 0.25), it.z, heading(front), 'sit', it.id, r); }
    else if (['newsstand', 'kiosk-food', 'kiosk-pavilion', 'kiosk-tickets'].includes(it.kind)) {
      const n = (r() < 0.75 ? 1 : 0) + (r() < 0.35 ? 1 : 0), dpt = (it.size?.[1] ?? 1.5) / 2;
      for (let i = 0; i < n; i++) stand(`queue/${it.id}/${i}`, add(add(it.at, front, dpt + 0.7 + i * 0.8), [front[1], -front[0]], (r() - 0.5) * 0.6), it.z, heading(front) + Math.PI, 'idle', it.id, r);
    } else if (it.kind === 'phone-booth' && r() < 0.15) stand(`phone/${it.id}`, add(it.at, front, 0.9), it.z, heading(front) + Math.PI, 'phone', it.id, r);
  }

  // ---- 6. aircars: two sky lanes over the primary ring (anticlockwise low, clockwise high) and the Boulevard / Crown Avenue spine
  const sky = (id: string, pts: P[], z: number, n: number, vT: number, models3: string[], where: string, dir = 1) => {
    const xyz = pts.flatMap(p => [p[0], p[1], z]), clash = pts.filter(p => skyBlocked(p, z, 4)).length;
    if (clash) { drop(`aircar lane ${id}: ${clash} points under a structure`); return; }
    const lp = pushLoop(`sky/${id}`, xyz, where, 'sky'), laps = Math.max(1, Math.round(vT * PERIOD / lp.L)), v = laps * lp.L / PERIOD;
    for (let i = 0; i < n; i++) { const r = rng(`aircar/${id}/${i}`); looping.push({ id: `aircar/${id}/${i}`, model: pick(r, models3), loop: lp.index, s0: r2(lp.L * (i + r() * 0.6) / n), v: r3(dir * v), lat: r2((r() - 0.5) * 3), clip: 'fly', bob: 0.25 }); }
  };
  const rounded = (corners: P[], R: number) => { const out: P[] = []; for (let i = 0; i < corners.length; i++) { const p0 = corners[(i + corners.length - 1) % corners.length], p = corners[i], p1 = corners[(i + 1) % corners.length]; const a = add(p, unit(p, p0), R), b = add(p, unit(p, p1), R); out.push(...connector(a, unit(p0, p), b, unit(p, p1)).filter((_, j, xs) => j < xs.length)); } return resample([...out, out[0]], 6).slice(0, -1); };
  const ringPts = rounded([[-340, -720], [300, -720], [300, 280], [-340, 280]], 40);
  sky('ring-low', ringPts, 38, 9, 18, ['aircar/coral', 'aircar/teal'], 'over the primary ring, anticlockwise');
  sky('ring-high', ringPts.slice().reverse(), 46, 7, 20, ['aircar/coral', 'aircar/teal', 'aircar/saucer'], 'over the primary ring, clockwise');
  const spine: P[] = [...resample([[7, -880], [7, 268]], 8), ...connector([7, 268], [0, 1], [-7, 268], [0, -1]).slice(1, -1), ...resample([[-7, 268], [-7, -880]], 8), ...connector([-7, -880], [0, -1], [7, -880], [0, 1]).slice(1, -1)];
  sky('spine', dedupe(spine), 36, 8, 16, ['aircar/coral', 'aircar/teal'], 'over the Boulevard and Crown Avenue, through the Crown Avenue arch');

  // ---- counts and the file
  const by = (f: (m: Model) => boolean) => [...timed, ...looping, ...still].filter(x => f(models[x.model])).length;
  const movingCarsAt = (tau: number) => timed.filter(x => routes[x.route].kind === 'car').filter(x => { const d = ((tau - x.t0) % PERIOD + PERIOD) % PERIOD; return d <= routes[x.route].t.at(-1)!; }).length;
  const meanMoving = Math.round([0, 90, 180, 270, 360, 450, 540, 630].reduce((s, t) => s + movingCarsAt(t), 0) / 8);
  const counts = {
    figures: by(m => m.kind === 'figure'), walkers: looping.filter(x => x.clip === 'walk').length, standing: still.filter(x => x.clip !== 'parked').length,
    carTrips: timed.filter(x => routes[x.route].kind === 'car').length, carsMovingMean: meanMoving, carsParked: still.filter(x => x.clip === 'parked').length,
    trams: timed.filter(x => models[x.model].kind === 'tram').length, skyrailTrains: timed.filter(x => models[x.model].kind === 'skyrail').length, aircars: looping.filter(x => x.clip === 'fly').length,
    walkLoops: loops.filter(l => l.kind === 'walk').length, routes: routes.length, walkLoopsDropped: loopsDropped, dropped,
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
    models, loops, routes, timed, looping, still, counts,
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
