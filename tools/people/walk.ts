// The walk graph the people are checked on (brief 010 B, P-1): the plan checker's blockers (built plots, the block's
// footprints, water, landmarks: tools/plan/walk.ts blockersOf) on a 2 m grid, 16 neighbours, shortest paths from each place by
// Dijkstra. The grid is the scope's bounding box, so a run costs a few thousand cells. Minutes = metres / walk speed.
// District scale (Redline M6, 2026-10-15; for Pulse's district plan, not fixed here): every source keeps a full Float64Array (`memo`, never freed), about 1.6 MB a source on a
// 616 x 976 m grid, so ~1,150 sources need ~1.9 GB and about a minute; a district run needs a per-scope grid, a bounded search or a shared field per source cell.
import { type V, dist, inPoly } from '../plan/geom';
import type { Civic } from '../plan/civic';
import type { Derived, Plan } from '../plan/plan';
import { blockersOf } from '../plan/walk';

export const CELL = 2;
const STEPS: [number, number][] = [[1, 0], [-1, 0], [0, 1], [0, -1], [1, 1], [1, -1], [-1, 1], [-1, -1], [1, 2], [2, 1], [-1, 2], [-2, 1], [1, -2], [2, -1], [-1, -2], [-2, -1]];

export type Walker = {
  /** Walkable ground at p (outside every blocker). */
  free(p: V): boolean;
  /** Metres of the shortest walk a → b, Infinity if there is none. */
  m(a: V, b: V): number;
  /** Whole minutes to walk a → b at `speed` m/s (rounded up, plus one to step off and on), Infinity if there is none. */
  min(a: V, b: V): number;
};

/** `points`: every place a person can stand; the grid covers them with 60 m to spare. */
export function walker(plan: Plan, d: Derived, civ: Civic, points: V[], speed: number): Walker {
  const { polys, rounds } = blockersOf(plan, d, civ), pad = 60;
  const x0 = Math.min(...points.map(p => p[0])) - pad, y0 = Math.min(...points.map(p => p[1])) - pad;
  const nx = Math.ceil((Math.max(...points.map(p => p[0])) + pad - x0) / CELL), ny = Math.ceil((Math.max(...points.map(p => p[1])) + pad - y0) / CELL);
  const boxed = polys.map(b => ({ b, a: Math.min(...b.map(v => v[0])), z: Math.max(...b.map(v => v[0])), c: Math.min(...b.map(v => v[1])), e: Math.max(...b.map(v => v[1])) }))
    .filter(k => k.z >= x0 && k.a <= x0 + nx * CELL && k.e >= y0 && k.c <= y0 + ny * CELL);
  const inBlocker = (p: V) => boxed.some(k => p[0] >= k.a && p[0] <= k.z && p[1] >= k.c && p[1] <= k.e && inPoly(p, k.b)) || rounds.some(l => dist(p, l.c) < l.r);
  const blocked = new Uint8Array(nx * ny), centre = (i: number, j: number): V => [x0 + (i + .5) * CELL, y0 + (j + .5) * CELL];
  for (let j = 0; j < ny; j++) for (let i = 0; i < nx; i++) blocked[j * nx + i] = inBlocker(centre(i, j)) ? 1 : 0;
  const free = (i: number, j: number) => i >= 0 && j >= 0 && i < nx && j < ny && !blocked[j * nx + i];
  const cellOf = (p: V): [number, number] => [Math.floor((p[0] - x0) / CELL), Math.floor((p[1] - y0) / CELL)];
  /** The nearest free cell within 8 m of p (a door stands on the face of its building), with the metres to reach it. */
  const snap = (p: V): { k: number; extra: number } | undefined => {
    const [ci, cj] = cellOf(p);
    let best: { k: number; extra: number } | undefined;
    for (let r = 0; r <= 4 && !best; r++) {
      for (let j = cj - r; j <= cj + r; j++) for (let i = ci - r; i <= ci + r; i++) {
        if (Math.max(Math.abs(i - ci), Math.abs(j - cj)) !== r || !free(i, j)) continue;
        const extra = dist(p, centre(i, j));
        if (!best || extra < best.extra) best = { k: j * nx + i, extra };
      }
    }
    return best;
  };
  const passes = (i: number, j: number, dx: number, dy: number) => { // no corner cutting
    const sx = Math.sign(dx), sy = Math.sign(dy), ax = Math.abs(dx), ay = Math.abs(dy);
    if (ax + ay === 1) return true;
    if (ax === 1 && ay === 1) return free(i + dx, j) && free(i, j + dy);
    return ay === 2 ? free(i, j + sy) && free(i + dx, j + sy) : free(i + sx, j) && free(i + sx, j + dy);
  };
  const memo = new Map<string, Float64Array>();
  const from = (p: V) => {
    const key = `${p[0]},${p[1]}`, hit = memo.get(key); if (hit) return hit;
    const D = new Float64Array(nx * ny).fill(Infinity), s = snap(p);
    if (s) {
      const heap: [number, number][] = [[s.extra, s.k]]; D[s.k] = s.extra;
      const push = (v: number, k: number) => { heap.push([v, k]); let c = heap.length - 1; while (c) { const q = (c - 1) >> 1; if (heap[q]![0] <= heap[c]![0]) break; [heap[q], heap[c]] = [heap[c]!, heap[q]!]; c = q; } };
      const pop = () => { const top = heap[0]!, last = heap.pop()!; if (heap.length) { heap[0] = last; let c = 0; for (;;) { const l = 2 * c + 1, r = l + 1; let m = c; if (l < heap.length && heap[l]![0] < heap[m]![0]) m = l; if (r < heap.length && heap[r]![0] < heap[m]![0]) m = r; if (m === c) break; [heap[m], heap[c]] = [heap[c]!, heap[m]!]; c = m; } } return top; };
      while (heap.length) {
        const [v, k] = pop(); if (v > D[k]!) continue;
        const i = k % nx, j = Math.floor(k / nx);
        for (const [dx, dy] of STEPS) {
          if (!free(i + dx, j + dy) || !passes(i, j, dx, dy)) continue;
          const nk = (j + dy) * nx + i + dx, nv = v + Math.hypot(dx, dy) * CELL;
          if (nv < D[nk]!) { D[nk] = nv; push(nv, nk); }
        }
      }
    }
    memo.set(key, D); return D;
  };
  const m = (a: V, b: V) => {
    if (a[0] === b[0] && a[1] === b[1]) return 0;
    const t = snap(b); if (!t || !snap(a)) return Infinity;
    return from(a)[t.k]! + t.extra;
  };
  return { free: p => !inBlocker(p), m, min: (a, b) => { const x = m(a, b); return x === Infinity ? Infinity : x === 0 ? 0 : Math.ceil(x / speed / 60) + 1; } };
}
