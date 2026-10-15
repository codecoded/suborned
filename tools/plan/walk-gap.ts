// W-5, the walking clearance (standards.walking.body; Datum 2026-10-15, written by Splice): walks keep body.r from the civic block's ground solids, and pass no gap narrower than body.clearWidth,
// except a gap of at least body.constrained.w over no more than body.constrained.run metres of the walk, or an aisle of at least body.aisle inside a seating area; within body.ends of a door, a
// seat or a Handover point nothing is judged. One body for every walk the game measures, on any grid.
//
// `gapWalks` measures it the way Datum did: every shortest walk between a set of points (the doors, the Handover points, the crossing landings round the block) on a fine grid (0.25 m) where a
// cell is free if no blocker (a built plot, a building or tower footprint, water, a landmark) covers it and no ground solid is nearer than body.r; each walk is string-pulled, then sampled every
// 0.25 m; the clear width at a sample is the distance to the nearest obstacle edge on either side of the walk, measured square across it (3 m a side at most). A walker is 2r wide: it keeps r from every
// obstacle's edge (a solid, a building, a tower, a built plot, water), so a gap of 2r or less is closed and no walk passes it. Pure: nothing is read here.
import { type V, dist, inPoly, segDist } from './geom';
import type { Civic } from './civic';
import type { Derived, Plan } from './plan';
import { OPEN } from './walk';

export type Body = { r: number; clearWidth: number; constrained: { w: number; run: number }; aisle: number; ends: number };
export const bodyStd = (plan: Plan): Body | undefined => plan.standards.walking?.body as Body | undefined;

export type Pt = { name: string; p: V };
export type Gap = { walk: string; at: V; width: number; run: number; between: string; why: string };
export type GapReport = {
  /** walks measured, points that no walk starts from (no free ground within `ends`), the narrow places walks pass that the standard allows, and the ones it does not */
  walks: number; stranded: string[]; allowed: number; bad: Gap[];
};

const CELL = 0.25, CAP = 3, STEP = 0.25;
const left = (t: V): V => [-t[1], t[0]];

/** Where a W-1 walk (tools/plan/walk.ts) comes nearer than `r` to a ground solid, except within `ends` of its two ends (a door in its reveal): the walkers read the clearance from the plan, and this holds them to it. */
export function walkClearance(walks: { from: string; to: string; path: V[] }[], civ: Civic, body: Body): { walk: string; at: V; gap: number; solid: string }[] {
  const solids = civ.solids ?? [], names = civ.solidIds ?? [], out: { walk: string; at: V; gap: number; solid: string }[] = [];
  for (const w of walks) {
    const a = w.path[0]!, b = w.path[w.path.length - 1]!;
    let worst: { at: V; gap: number; solid: string } | undefined;
    for (let i = 1; i < w.path.length; i++) {
      const p = w.path[i - 1]!, q = w.path[i]!, n = Math.max(1, Math.ceil(dist(p, q) / STEP));
      for (let k = 0; k <= n; k++) {
        const x: V = [p[0] + ((q[0] - p[0]) * k) / n, p[1] + ((q[1] - p[1]) * k) / n];
        if (dist(x, a) < body.ends || dist(x, b) < body.ends) continue;
        solids.forEach((s, si) => { const d = inPoly(x, s) ? 0 : Math.min(...s.map((u, j) => segDist(x, u, s[(j + 1) % s.length]!))); if (d < body.r - 0.01 && (!worst || d < worst.gap)) worst = { at: x, gap: d, solid: names[si] ?? `solid ${si}` }; });
      }
    }
    if (worst) out.push({ walk: `${w.from} → ${w.to}`, ...worst });
  }
  return out;
}

/** The measured walks. `seating`: the seating areas' rings (plan frame), inside which an aisle of body.aisle passes. */
export function gapWalks(plan: Plan, d: Derived, civ: Civic, points: Pt[], seating: V[][], body: Body): GapReport {
  const rep: GapReport = { walks: 0, stranded: [], allowed: 0, bad: [] };
  if (points.length < 2) return rep;
  const margin = 6, xs = points.map(p => p.p[0]), ys = points.map(p => p.p[1]);
  const x0 = Math.min(...xs) - margin, y0 = Math.min(...ys) - margin, nx = Math.ceil((Math.max(...xs) + margin - x0) / CELL), ny = Math.ceil((Math.max(...ys) + margin - y0) / CELL);
  const bx = [x0, x0 + nx * CELL, y0, y0 + ny * CELL] as const, meets = (poly: V[]) => Math.max(...poly.map(p => p[0])) >= bx[0] && Math.min(...poly.map(p => p[0])) <= bx[1] && Math.max(...poly.map(p => p[1])) >= bx[2] && Math.min(...poly.map(p => p[1])) <= bx[3];
  // what stands in the way, named: built plots, building and tower footprints, water, then the ground solids
  type Ob = { name: string; poly: V[]; solid: boolean };
  const obs: Ob[] = [
    ...d.plots.filter(p => !OPEN.has(p.use)).map(p => ({ name: `plot ${p.id}`, poly: p.polyV, solid: false })),
    ...civ.buildings.flatMap(b => [{ name: b.id, poly: b.poly, solid: false }, ...(b.towers ?? []).filter(t => t.poly).map(t => ({ name: `${b.id} tower ${t.id}`, poly: t.poly!, solid: false }))]),
    ...plan.water.map((w, i) => ({ name: `water ${i}`, poly: w, solid: false })),
    ...(civ.solids ?? []).map((s, i) => ({ name: civ.solidIds?.[i] ?? `solid ${i}`, poly: s, solid: true })),
  ].filter(o => meets(o.poly));
  const rounds = plan.landmarks.filter(l => l.id !== 'skyport' && l.c[0] + l.r >= bx[0] && l.c[0] - l.r <= bx[1] && l.c[1] + l.r >= bx[2] && l.c[1] - l.r <= bx[3]);

  // the mask: 1 = covered by a blocker, 2 = within r of a ground solid (or inside one)
  const blocked = new Uint8Array(nx * ny), cx = (i: number) => x0 + (i + 0.5) * CELL, cy = (j: number) => y0 + (j + 0.5) * CELL;
  const cellRange = (lo: number, hi: number, o: number, n: number, pad = 0): [number, number] => [Math.max(0, Math.floor((lo - pad - o) / CELL)), Math.min(n - 1, Math.floor((hi + pad - o) / CELL))];
  for (const o of obs) {
    const px = o.poly.map(p => p[0]), py = o.poly.map(p => p[1]), [i0, i1] = cellRange(Math.min(...px), Math.max(...px), x0, nx, body.r), [j0, j1] = cellRange(Math.min(...py), Math.max(...py), y0, ny, body.r);
    for (let j = j0; j <= j1; j++) for (let i = i0; i <= i1; i++) {
      const c: V = [cx(i), cy(j)], k = j * nx + i;
      if (inPoly(c, o.poly)) { blocked[k] = 1; continue; }
      if (!blocked[k] && o.poly.some((a, e) => segDist(c, a, o.poly[(e + 1) % o.poly.length]!) < body.r)) blocked[k] = 2;
    }
  }
  for (const l of rounds) { const [i0, i1] = cellRange(l.c[0] - l.r, l.c[0] + l.r, x0, nx), [j0, j1] = cellRange(l.c[1] - l.r, l.c[1] + l.r, y0, ny); for (let j = j0; j <= j1; j++) for (let i = i0; i <= i1; i++) if (dist([cx(i), cy(j)], l.c) < l.r) blocked[j * nx + i] = 1; }
  const idx = (p: V) => Math.max(0, Math.min(ny - 1, Math.floor((p[1] - y0) / CELL))) * nx + Math.max(0, Math.min(nx - 1, Math.floor((p[0] - x0) / CELL)));
  const centre = (k: number): V => [cx(k % nx), cy(Math.floor(k / nx))];
  const sees = (a: V, b: V, exempt: V[]) => { const n = Math.max(1, Math.ceil(dist(a, b) / (CELL * 0.4))); for (let s = 1; s < n; s++) { const p: V = [a[0] + ((b[0] - a[0]) * s) / n, a[1] + ((b[1] - a[1]) * s) / n]; if (blocked[idx(p)] && !exempt.some(e => dist(e, p) < body.ends)) return false; } return true; };

  // the edges rays are cast against, in a 2 m hash
  type Ed = { a: V; b: V; name: string };
  const edges: Ed[] = obs.flatMap(o => o.poly.map((a, i) => ({ a, b: o.poly[(i + 1) % o.poly.length]!, name: o.name }))), H = 2, hash = new Map<string, number[]>();
  edges.forEach((e, n) => { for (let i = Math.floor((Math.min(e.a[0], e.b[0]) - CAP) / H); i <= Math.floor((Math.max(e.a[0], e.b[0]) + CAP) / H); i++) for (let j = Math.floor((Math.min(e.a[1], e.b[1]) - CAP) / H); j <= Math.floor((Math.max(e.a[1], e.b[1]) + CAP) / H); j++) { const k = `${i},${j}`; (hash.get(k) ?? hash.set(k, []).get(k)!).push(n); } });
  const rayTo = (q: V, u: V): { t: number; name: string } => {
    let best = { t: CAP, name: '' };
    const seen = new Set<number>();
    for (let s = 0; s <= CAP; s += H / 2) { const p: V = [q[0] + u[0] * s, q[1] + u[1] * s]; for (const n of hash.get(`${Math.floor(p[0] / H)},${Math.floor(p[1] / H)}`) ?? []) {
      if (seen.has(n)) continue; seen.add(n);
      const e = edges[n]!, ex = e.b[0] - e.a[0], ey = e.b[1] - e.a[1], den = u[0] * ey - u[1] * ex; if (Math.abs(den) < 1e-9) continue;
      const t = ((e.a[0] - q[0]) * ey - (e.a[1] - q[1]) * ex) / den, sp = ((e.a[0] - q[0]) * u[1] - (e.a[1] - q[1]) * u[0]) / den;
      if (t >= -1e-6 && sp >= -1e-6 && sp <= 1 + 1e-6 && t < best.t) best = { t: Math.max(0, t), name: e.name };
    } }
    return best;
  };

  // each point's Dijkstra, then each pair's walk, string-pulled and sampled
  const STEPS: [number, number][] = [[1, 0], [-1, 0], [0, 1], [0, -1], [1, 1], [1, -1], [-1, 1], [-1, -1]];
  const free = (i: number, j: number) => i >= 0 && j >= 0 && i < nx && j < ny && !blocked[j * nx + i];
  const near = (p: V): number[] => { const out: number[] = [], r = Math.ceil(body.ends / CELL); const ci = Math.floor((p[0] - x0) / CELL), cj = Math.floor((p[1] - y0) / CELL); for (let j = cj - r; j <= cj + r; j++) for (let i = ci - r; i <= ci + r; i++) if (free(i, j) && dist(p, [cx(i), cy(j)]) <= body.ends && sees(p, [cx(i), cy(j)], [p])) out.push(j * nx + i); return out; };
  const ends = points.map(p => near(p.p));
  const all = points.map(p => p.p);
  const bad = new Map<string, Gap>();
  let allowed = 0;
  for (let s = 0; s < points.length; s++) {
    if (!ends[s]!.length) { rep.stranded.push(points[s]!.name); continue; }
    const D = new Float64Array(nx * ny).fill(Infinity), prev = new Int32Array(nx * ny).fill(-1), heap: [number, number][] = [];
    const push = (v: number, k: number) => { heap.push([v, k]); for (let c = heap.length - 1; c > 0;) { const q = (c - 1) >> 1; if (heap[q]![0] <= heap[c]![0]) break; [heap[q], heap[c]] = [heap[c]!, heap[q]!]; c = q; } };
    const pop = () => { const top = heap[0]!, last = heap.pop()!; if (heap.length) { heap[0] = last; for (let c = 0;;) { const l = 2 * c + 1, r = l + 1; let m = c; if (l < heap.length && heap[l]![0] < heap[m]![0]) m = l; if (r < heap.length && heap[r]![0] < heap[m]![0]) m = r; if (m === c) break; [heap[m], heap[c]] = [heap[c]!, heap[m]!]; c = m; } } return top; };
    for (const k of ends[s]!) { D[k] = dist(points[s]!.p, centre(k)); push(D[k]!, k); }
    while (heap.length) {
      const [v, k] = pop(); if (v > D[k]!) continue;
      const i = k % nx, j = Math.floor(k / nx);
      for (const [di, dj] of STEPS) {
        const a = i + di, b = j + dj; if (!free(a, b) || (di && dj && (!free(i + di, j) || !free(i, j + dj)))) continue;
        const nk = b * nx + a, nv = v + (di && dj ? Math.SQRT2 : 1) * CELL; if (nv < D[nk]!) { D[nk] = nv; prev[nk] = k; push(nv, nk); }
      }
    }
    for (let t = s + 1; t < points.length; t++) {
      const target = ends[t]!; let kt = -1, best = Infinity;
      for (const k of target) { const m = D[k]! + dist(centre(k), points[t]!.p); if (m < best) { best = m; kt = k; } }
      if (kt < 0 || best === Infinity) continue;
      const raw: V[] = [points[t]!.p]; for (let c = kt; c >= 0; c = prev[c]!) raw.push(centre(c)); raw.push(points[s]!.p); raw.reverse();
      const path: V[] = [raw[0]!]; let a = 0;
      while (a < raw.length - 1) { let b = raw.length - 1; while (b > a + 1 && !sees(raw[a]!, raw[b]!, [points[s]!.p, points[t]!.p])) b--; path.push(raw[b]!); a = b; }
      rep.walks++;
      // the clear width along the smoothed walk
      type Smp = { at: V; w: number; thr: number; seat: boolean; exempt: boolean; between: string };
      const samples: Smp[] = [];
      for (let i = 1; i < path.length; i++) {
        const p = path[i - 1]!, q = path[i]!, len = dist(p, q), u: V = [(q[0] - p[0]) / (len || 1), (q[1] - p[1]) / (len || 1)], n = left(u);
        for (let m = 0; m <= len + 1e-9; m += STEP) {
          const at: V = [p[0] + u[0] * m, p[1] + u[1] * m];
          if (i > 1 && m === 0) continue;
          const exempt = all.some(e => dist(e, at) < body.ends), l = rayTo(at, n), r = rayTo(at, [-n[0], -n[1]]), seat = seating.some(ring => inPoly(at, ring));
          samples.push({ at, w: l.t + r.t, thr: seat ? body.aisle : body.clearWidth, seat, exempt, between: `${l.name || 'open ground'} and ${r.name || 'open ground'}` });
        }
      }
      for (let i = 0; i < samples.length;) {
        if (samples[i]!.exempt || samples[i]!.w >= samples[i]!.thr - 1e-6) { i++; continue; }
        let j = i, minw = Infinity, mi = i; while (j < samples.length && !samples[j]!.exempt && samples[j]!.w < samples[j]!.thr - 1e-6) { if (samples[j]!.w < minw) { minw = samples[j]!.w; mi = j; } j++; }
        const run = (j - i) * STEP, s0 = samples[mi]!, constrained = !s0.seat && minw >= body.constrained.w - 1e-6 && run <= body.constrained.run + 1e-6;
        if (s0.seat ? minw < body.aisle - 1e-6 : !constrained) {
          const key = `${s0.between}@${Math.round(s0.at[0] / 2)},${Math.round(s0.at[1] / 2)}`;
          if (!bad.has(key)) bad.set(key, { walk: `${points[s]!.name} → ${points[t]!.name}`, at: [Math.round(s0.at[0] * 100) / 100, Math.round(s0.at[1] * 100) / 100], width: Math.round(minw * 100) / 100, run: Math.round(run * 100) / 100,
            between: s0.between, why: s0.seat ? `an aisle of ${minw.toFixed(2)} m, under ${body.aisle} m` : minw < body.constrained.w - 1e-6 ? `${minw.toFixed(2)} m, under ${body.constrained.w} m` : `${minw.toFixed(2)} m for ${run.toFixed(1)} m of the walk, past the ${body.constrained.run} m a constrained gap may run` });
        } else allowed++;
        i = j;
      }
    }
  }
  rep.allowed = allowed; rep.bad = [...bad.values()];
  return rep;
}
