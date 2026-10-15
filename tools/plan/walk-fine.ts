// One walk between two points on a fine grid (0.1 m), for measuring a short walk where the 2 m grid of walk.ts is too coarse to hold a wall: the same blockers,
// and the block's ground solids with the body clearance (walk-solids.ts). Exact to a few centimetres; use it for one pair at a time (a call takes a fraction of a second).
import { type V, dist, inPoly } from './geom';
import type { Derived, Plan } from './plan';
import type { Civic } from './civic';
import { Solids, bodyOf } from './walk-solids';
import { blockersOf } from './walk';

/** The length (m) of the shortest walk from a to b, or Infinity if there is none. */
export function walkBetween(plan: Plan, d: Derived, civ: Civic, a: V, b: V, cell = 0.1): number {
  const margin = Math.max(25, dist(a, b) / 2), x0 = Math.min(a[0], b[0]) - margin, y0 = Math.min(a[1], b[1]) - margin;
  const nx = Math.ceil((Math.max(a[0], b[0]) + margin - x0) / cell), ny = Math.ceil((Math.max(a[1], b[1]) + margin - y0) / cell);
  const { polys, rounds } = blockersOf(plan, d, civ), solids = new Solids(civ.solids, bodyOf(plan)), bad = new Uint8Array(nx * ny);
  const cx = (i: number) => x0 + (i + 0.5) * cell, cy = (j: number) => y0 + (j + 0.5) * cell;
  const cellOf = (p: V) => Math.floor((p[1] - y0) / cell) * nx + Math.floor((p[0] - x0) / cell);
  for (const poly of polys) {
    const xs = poly.map(p => p[0]), ys = poly.map(p => p[1]);
    if (Math.max(...xs) < x0 || Math.min(...xs) > x0 + nx * cell || Math.max(...ys) < y0 || Math.min(...ys) > y0 + ny * cell) continue;
    const i0 = Math.max(0, Math.floor((Math.min(...xs) - x0) / cell)), i1 = Math.min(nx - 1, Math.ceil((Math.max(...xs) - x0) / cell));
    const j0 = Math.max(0, Math.floor((Math.min(...ys) - y0) / cell)), j1 = Math.min(ny - 1, Math.ceil((Math.max(...ys) - y0) / cell));
    for (let j = j0; j <= j1; j++) for (let i = i0; i <= i1; i++) if (inPoly([cx(i), cy(j)], poly)) bad[j * nx + i] = 1;
  }
  for (const l of rounds) for (let j = Math.max(0, Math.floor((l.c[1] - l.r - y0) / cell)); j <= Math.min(ny - 1, Math.ceil((l.c[1] + l.r - y0) / cell)); j++) {
    for (let i = Math.max(0, Math.floor((l.c[0] - l.r - x0) / cell)); i <= Math.min(nx - 1, Math.ceil((l.c[0] + l.r - x0) / cell)); i++) if (dist([cx(i), cy(j)], l.c) < l.r) bad[j * nx + i] = 1;
  }
  for (const e of solids.list) { // a cell within BODY of a solid edge, or inside a solid, is not stood on
    const g = solids.r + cell;
    for (let j = Math.max(0, Math.floor((e.y0 - g - y0) / cell)); j <= Math.min(ny - 1, Math.ceil((e.y1 + g - y0) / cell)); j++) {
      for (let i = Math.max(0, Math.floor((e.x0 - g - x0) / cell)); i <= Math.min(nx - 1, Math.ceil((e.x1 + g - x0) / cell)); i++) {
        if (!bad[j * nx + i] && !solids.clear([cx(i), cy(j)], [cx(i), cy(j)], [e])) bad[j * nx + i] = 1;
      }
    }
  }
  const D = new Float64Array(nx * ny).fill(Infinity), s = cellOf(a), t = cellOf(b), heap: [number, number][] = [[0, s]];
  D[s] = 0; bad[s] = 0; bad[t] = 0; // the two ends are stood on, whatever the clearance says
  const push = (v: number, k: number) => { heap.push([v, k]); for (let c = heap.length - 1; c > 0;) { const p = (c - 1) >> 1; if (heap[p]![0] <= heap[c]![0]) break; [heap[p], heap[c]] = [heap[c]!, heap[p]!]; c = p; } };
  const pop = () => {
    const top = heap[0]!, last = heap.pop()!;
    if (heap.length) { heap[0] = last; for (let c = 0;;) { const l = 2 * c + 1, r = l + 1; let m = c; if (l < heap.length && heap[l]![0] < heap[m]![0]) m = l; if (r < heap.length && heap[r]![0] < heap[m]![0]) m = r; if (m === c) break; [heap[m], heap[c]] = [heap[c]!, heap[m]!]; c = m; } }
    return top;
  };
  const steps: [number, number][] = [];
  for (let u = -3; u <= 3; u++) for (let v = -3; v <= 3; v++) if ((u || v) && Math.abs(u) + Math.abs(v) <= 4 && !(Math.abs(u) === 3 && Math.abs(v) === 2) && !(Math.abs(u) === 2 && Math.abs(v) === 3)) steps.push([u, v]);
  while (heap.length) {
    const [v, k] = pop(); if (v > D[k]!) continue; if (k === t) break;
    const i = k % nx, j = (k - i) / nx;
    for (const [u, w] of steps) {
      const ii = i + u, jj = j + w; if (ii < 0 || jj < 0 || ii >= nx || jj >= ny || bad[jj * nx + ii]) continue;
      const n = Math.max(Math.abs(u), Math.abs(w)); let ok = true; // no corner cutting: every cell the step passes through is free
      for (let q = 1; q < n && ok; q++) ok = !bad[(j + Math.round((w * q) / n)) * nx + i + Math.round((u * q) / n)];
      if (!ok) continue;
      const nv = v + Math.hypot(u, w) * cell; if (nv < D[jj * nx + ii]!) { D[jj * nx + ii] = nv; push(nv, jj * nx + ii); }
    }
  }
  return D[t]!;
}
