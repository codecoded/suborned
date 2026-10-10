// Walking on foot: players cross any road anywhere (no crossings, kerbs, medians or tram reservation stop them).
// Only buildings, built plots, landmarks and water block. Dijkstra on a grid with 16 neighbours, then string-pulled to
// an any-angle route.
import { type V, dist, inPoly } from './geom';
import type { Derived, Plan } from './plan';
import type { Civic } from './civic';

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

function run(plan: Plan, d: Derived, civ: Civic) {
  const W = plan.standards.walking, cell = W.grid as number;
  const origins = (W.from as string[]).map(name => ({ name, p: civ.points.find(x => x.name === name)!.p }));
  const targets = [
    ...plan.tram.flatMap(l => (l.stops ?? []).map(s => ({ kind: 'tram', line: l.id, id: s.id, name: s.name, p: s.at }))),
    ...plan.skyrail.flatMap(l => (l.stations ?? []).map(s => ({ kind: 'sky rail', line: l.id, id: s.id, name: s.name, p: s.at }))),
  ];
  const all = [...origins.map(o => o.p), ...targets.map(t => t.p)], pad = 80;
  const x0 = Math.min(...all.map(p => p[0])) - pad, y0 = Math.min(...all.map(p => p[1])) - pad;
  const nx = Math.ceil((Math.max(...all.map(p => p[0])) + pad - x0) / cell), ny = Math.ceil((Math.max(...all.map(p => p[1])) + pad - y0) / cell);
  const blockers: V[][] = [...d.plots.filter(p => !OPEN.has(p.use)).map(p => p.polyV), ...civ.buildings.map(b => b.poly), ...plan.water];
  const rounds = plan.landmarks.filter(l => l.id !== 'skyport');
  const blocked = new Uint8Array(nx * ny);
  for (let j = 0; j < ny; j++) for (let i = 0; i < nx; i++) {
    const p: V = [x0 + (i + .5) * cell, y0 + (j + .5) * cell];
    blocked[j * nx + i] = blockers.some(b => inPoly(p, b)) || rounds.some(l => dist(p, l.c) < l.r) ? 1 : 0;
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
  const sight = (a: V, b: V) => { const n = Math.ceil(dist(a, b) / (cell * .4)); for (let k = 1; k < n; k++) { const t = k / n; if (blocked[idx([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t])]) return false; } return true; };

  const shortest = targets.map(() => Infinity);
  const walks = origins.flatMap(o => {
    const D = new Float64Array(nx * ny).fill(Infinity), prev = new Int32Array(nx * ny).fill(-1), heap: [number, number][] = [];
    const push = (k: number, v: number) => { heap.push([v, k]); let c = heap.length - 1; while (c) { const p = (c - 1) >> 1; if (heap[p][0] <= heap[c][0]) break; [heap[p], heap[c]] = [heap[c], heap[p]]; c = p; } };
    const pop = () => { const top = heap[0], last = heap.pop()!; if (heap.length) { heap[0] = last; let c = 0; for (;;) { const l = 2 * c + 1, r = l + 1; let m = c; if (l < heap.length && heap[l][0] < heap[m][0]) m = l; if (r < heap.length && heap[r][0] < heap[m][0]) m = r; if (m === c) break; [heap[m], heap[c]] = [heap[c], heap[m]]; c = m; } } return top; };
    const s = idx(o.p); D[s] = 0; push(s, 0);
    while (heap.length) {
      const [v, k] = pop(); if (v > D[k]) continue;
      const i = k % nx, j = Math.floor(k / nx);
      for (const [dx, dy] of steps) {
        const a = i + dx, b = j + dy;
        if (!free(a, b) || !passes(i, j, dx, dy)) continue;
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
