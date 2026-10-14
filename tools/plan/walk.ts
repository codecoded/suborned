// Walking on foot: players cross any road anywhere (no crossings, kerbs, medians or tram reservation stop them).
// Only buildings, built plots, landmarks and water block (and, on a block with a dressing, its ground solids: walk-solids.ts). Dijkstra on a grid
// with 16 neighbours, then string-pulled to an any-angle route.
import { type V, dist, inPoly } from './geom';
import type { Derived, Plan } from './plan';
import { type Civic, footprints } from './civic';
import { type Edge, Solids } from './walk-solids';

export type Walk = { from: string; to: string; kind: string; m: number; straight: number; path: V[] };
/** One tram stop or sky rail station and its shortest walk (m) from the nearest origin; Infinity = no walk at all. */
export type Reach = { line: string; id: string; kind: string; name: string; at: V; m: number };

const OPEN = new Set(['park', 'open', 'square', 'civic']); // civic plots: only their building footprints block

const memo = new WeakMap<Derived, { walks: Walk[]; reach: Reach[] }>();
export const walks = (plan: Plan, d: Derived, civ: Civic): Walk[] => solve(plan, d, civ).walks;
/** Every stop and station against the walkable ground reached from the walking origins (water, buildings and landmarks block). */
export const reach = (plan: Plan, d: Derived, civ: Civic): Reach[] => solve(plan, d, civ).reach;

function solve(plan: Plan, d: Derived, civ: Civic) {
  const hit = memo.get(d); if (hit) return hit;
  const r = run(plan, d, civ); memo.set(d, r); return r;
}

/** What blocks a walker: the built plots, the block's buildings and entrance towers, the water (polygons), and the landmarks (discs; the skyport is walked under). */
export function blockersOf(plan: Plan, d: Derived, civ: Civic) {
  return {
    polys: [...d.plots.filter(p => !OPEN.has(p.use)).map(p => p.polyV), ...footprints(civ), ...plan.water] as V[][],
    rounds: plan.landmarks.filter(l => l.id !== 'skyport'),
  };
}

function run(plan: Plan, d: Derived, civ: Civic) {
  const W = plan.standards.walking, cell = W.grid as number;
  const origins = civ.walkFrom;
  const targets = [
    ...plan.tram.flatMap(l => (l.stops ?? []).map(s => ({ kind: 'tram', line: l.id, id: s.id, name: s.name, p: s.at }))),
    ...plan.skyrail.flatMap(l => (l.stations ?? []).map(s => ({ kind: 'sky rail', line: l.id, id: s.id, name: s.name, p: s.at }))),
  ];
  const all = [...origins.map(o => o.p), ...targets.map(t => t.p)], pad = 80;
  const x0 = Math.min(...all.map(p => p[0])) - pad, y0 = Math.min(...all.map(p => p[1])) - pad;
  const nx = Math.ceil((Math.max(...all.map(p => p[0])) + pad - x0) / cell), ny = Math.ceil((Math.max(...all.map(p => p[1])) + pad - y0) / cell);
  const { polys: blockers, rounds } = blockersOf(plan, d, civ);
  const boxed = blockers.map(b => ({ b, x0: Math.min(...b.map(v => v[0])), x1: Math.max(...b.map(v => v[0])), y0: Math.min(...b.map(v => v[1])), y1: Math.max(...b.map(v => v[1])) }));
  const blocked = new Uint8Array(nx * ny);
  for (let j = 0; j < ny; j++) for (let i = 0; i < nx; i++) {
    const p: V = [x0 + (i + .5) * cell, y0 + (j + .5) * cell];
    blocked[j * nx + i] = boxed.some(k => p[0] >= k.x0 && p[0] <= k.x1 && p[1] >= k.y0 && p[1] <= k.y1 && inPoly(p, k.b)) || rounds.some(l => dist(p, l.c) < l.r) ? 1 : 0;
  }
  const idx = (p: V) => Math.max(0, Math.min(ny - 1, Math.floor((p[1] - y0) / cell))) * nx + Math.max(0, Math.min(nx - 1, Math.floor((p[0] - x0) / cell)));
  const centre = (k: number): V => [x0 + ((k % nx) + .5) * cell, y0 + (Math.floor(k / nx) + .5) * cell];
  const free = (i: number, j: number) => i >= 0 && j >= 0 && i < nx && j < ny && !blocked[j * nx + i];
  /** No corner cutting: a diagonal or knight step needs the cells it straddles clear. */
  const passes = (i: number, j: number, dx: number, dy: number) => {
    const sx = Math.sign(dx), sy = Math.sign(dy), ax = Math.abs(dx), ay = Math.abs(dy);
    if (ax + ay === 1) return true;
    if (ax === 1 && ay === 1) return free(i + dx, j) && free(i, j + dy);
    return ay === 2 ? free(i, j + sy) && free(i + dx, j + sy) : free(i + sx, j) && free(i + sx, j + dy);
  };
  const steps: [number, number][] = [[1, 0], [-1, 0], [0, 1], [0, -1], [1, 1], [1, -1], [-1, 1], [-1, -1], [1, 2], [2, 1], [-1, 2], [-2, 1], [1, -2], [2, -1], [-1, -2], [-2, -1]];
  // the block's ground solids block a step by their own edges, not by cell centres: each cell lists the edges a step from it can meet
  const solids = new Solids(civ.solids), edgesAt = new Map<number, Edge[]>(), grow = solids.r + 2.5 * cell; // a step reaches 2 cells along one axis and 1 across
  for (const e of solids.list) {
    const i0 = Math.max(0, Math.floor((e.x0 - grow - x0) / cell)), i1 = Math.min(nx - 1, Math.floor((e.x1 + grow - x0) / cell));
    const j0 = Math.max(0, Math.floor((e.y0 - grow - y0) / cell)), j1 = Math.min(ny - 1, Math.floor((e.y1 + grow - y0) / cell));
    for (let j = j0; j <= j1; j++) for (let i = i0; i <= i1; i++) { const k = j * nx + i, l = edgesAt.get(k); if (l) l.push(e); else edgesAt.set(k, [e]); }
  }
  const clearSeg = (a: V, b: V) => !solids.any || solids.clear(a, b, solids.near(Math.min(a[0], b[0]), Math.max(a[0], b[0]), Math.min(a[1], b[1]), Math.max(a[1], b[1])));
  const sight = (a: V, b: V) => { const n = Math.ceil(dist(a, b) / (cell * .4)); for (let k = 1; k < n; k++) { const t = k / n; if (blocked[idx([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t])]) return false; } return clearSeg(a, b); };

  const shortest = targets.map(() => Infinity);
  const walks = origins.flatMap(o => {
    const D = new Float64Array(nx * ny).fill(Infinity), prev = new Int32Array(nx * ny).fill(-1), heap: [number, number][] = [];
    const push = (k: number, v: number) => { heap.push([v, k]); let c = heap.length - 1; while (c) { const p = (c - 1) >> 1; if (heap[p][0] <= heap[c][0]) break; [heap[p], heap[c]] = [heap[c], heap[p]]; c = p; } };
    const pop = () => { const top = heap[0], last = heap.pop()!; if (heap.length) { heap[0] = last; let c = 0; for (;;) { const l = 2 * c + 1, r = l + 1; let m = c; if (l < heap.length && heap[l][0] < heap[m][0]) m = l; if (r < heap.length && heap[r][0] < heap[m][0]) m = r; if (m === c) break; [heap[m], heap[c]] = [heap[c], heap[m]]; c = m; } } return top; };
    const s = idx(o.p); D[s] = 0; push(s, 0);
    if (solids.any) { // the origin is a point, not its cell's centre: it starts every cell it can walk straight to (a door beside a planter may have a centre inside the planter's clearance)
      const [oi, oj] = [s % nx, Math.floor(s / nx)];
      for (let j = oj - 1; j <= oj + 1; j++) for (let i = oi - 1; i <= oi + 1; i++) {
        const k = j * nx + i; if (k === s || !free(i, j) || !clearSeg(o.p, centre(k))) continue;
        D[k] = dist(o.p, centre(k)); push(k, D[k]!);
      }
    }
    while (heap.length) {
      const [v, k] = pop(); if (v > D[k]) continue;
      const i = k % nx, j = Math.floor(k / nx);
      for (const [dx, dy] of steps) {
        const a = i + dx, b = j + dy;
        if (!free(a, b) || !passes(i, j, dx, dy)) continue;
        const es = edgesAt.get(k);
        if (es && !solids.clear(centre(k), centre(b * nx + a), es)) continue;
        const nk = b * nx + a, nv = v + Math.hypot(dx, dy) * cell;
        if (nv < D[nk]) { D[nk] = nv; prev[nk] = k; push(nk, nv); }
      }
    }
    const best = new Map<string, Walk>();
    for (const [ti, t] of targets.entries()) {
      const k = idx(t.p); if (D[k] === Infinity) continue;
      shortest[ti] = Math.min(shortest[ti]!, D[k]!);
      const raw: V[] = [t.p]; for (let c = prev[k]; c >= 0 && c !== s; c = prev[c]) raw.push(centre(c)); raw.push(o.p); raw.reverse();
      const path: V[] = [raw[0]]; let a = 0;
      while (a < raw.length - 1) { let b = raw.length - 1; while (b > a + 1 && !sight(raw[a], raw[b])) b--; path.push(raw[b]); a = b; }
      const m = path.slice(1).reduce((sum, p, i) => sum + dist(path[i], p), 0);
      const w = { from: o.name, to: `${t.kind} ${t.name}`, kind: t.kind, m, straight: dist(o.p, t.p), path };
      if (!best.has(t.kind) || m < best.get(t.kind)!.m) best.set(t.kind, w);
    }
    return [...best.values()];
  });
  const reach: Reach[] = targets.map((t, i) => ({ line: t.line, id: t.id, kind: t.kind, name: t.name, at: t.p, m: shortest[i]! }));
  return { walks, reach };
}
