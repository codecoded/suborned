// The walk graph the people are checked on (brief 010 B, P-1): the plan checker's blockers (built plots, the block's
// footprints, water, landmarks: tools/plan/walk.ts blockersOf) on a 2 m grid, 16 neighbours, shortest paths from each place by
// Dijkstra. The grid is the scope's bounding box, so a run costs a few thousand cells. Minutes = metres / walk speed. A walker keeps the plan's body clearance from the block's ground solids
// (standards.walking.body.r, W-5: tools/plan/walk-solids.ts, the same class the plan checker's walks use): a step is tested against the solids' own edges, never against cell centres alone, and a walk
// starts and ends within `ends` of its place, where nothing is judged (a chair at its table, a door in its reveal).
// District scale (Redline M6, 2026-10-15; for Pulse's district plan, not fixed here): every source keeps a full Float64Array (`memo`, never freed), about 1.6 MB a source on a
// 616 x 976 m grid, so ~1,150 sources need ~1.9 GB and about a minute; a district run needs a per-scope grid, a bounded search or a shared field per source cell.
import { type V, dist, inPoly } from '../plan/geom';
import type { Civic } from '../plan/civic';
import type { Derived, Plan } from '../plan/plan';
import { blockersOf } from '../plan/walk';
import { type Edge, Solids, bodyOf } from '../plan/walk-solids';
import { bodyStd } from '../plan/walk-gap';

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
  // the block's ground solids block a step by their own edges, a body's clearance away (the plan checker's walks do the same: tools/plan/walk.ts); each cell lists the edges a step from it can meet
  const solids = new Solids(civ.solids ?? [], bodyOf(plan)), edgesAt = new Map<number, Edge[]>(), grow = solids.r + 2.5 * CELL, ends = bodyStd(plan)?.ends ?? 1.5;
  for (const e of solids.list) {
    const i0 = Math.max(0, Math.floor((e.x0 - grow - x0) / CELL)), i1 = Math.min(nx - 1, Math.floor((e.x1 + grow - x0) / CELL)), j0 = Math.max(0, Math.floor((e.y0 - grow - y0) / CELL)), j1 = Math.min(ny - 1, Math.floor((e.y1 + grow - y0) / CELL));
    for (let j = j0; j <= j1; j++) for (let i = i0; i <= i1; i++) { const k = j * nx + i, l = edgesAt.get(k); if (l) l.push(e); else edgesAt.set(k, [e]); }
  }
  const stepOk = (k: number, nk: number) => { const es = edgesAt.get(k); return !es || solids.clear(centre(k % nx, Math.floor(k / nx)), centre(nk % nx, Math.floor(nk / nx)), es); };
  const inSolid = (p: V) => solids.any && solids.inside(p);
  /** The cells a walk from or to p starts on: free ground a body's clearance from every solid, within `ends` + one cell of p and in sight of it (a chair at its table, a door in its reveal: nothing is judged within `ends`), with the metres
   *  to reach each; else the nearest free cell within 8 m (a door stands on the face of its building). */
  const seeds = (p: V): { k: number; extra: number }[] => {
    const [ci, cj] = cellOf(p), r = Math.ceil((ends + CELL) / CELL), out: { k: number; extra: number }[] = [];
    for (let j = cj - r; j <= cj + r; j++) for (let i = ci - r; i <= ci + r; i++) {
      if (!free(i, j)) continue;
      const c = centre(i, j), extra = dist(p, c), k = j * nx + i;
      if (extra > ends + CELL || (edgesAt.get(k) && !solids.clear(c, c, edgesAt.get(k)!))) continue;
      let sees = true; for (let n = 1, m = Math.ceil(extra / 0.25); n < m && sees; n++) { const q: V = [p[0] + ((c[0] - p[0]) * n) / m, p[1] + ((c[1] - p[1]) * n) / m]; sees = !inBlocker(q) && !inSolid(q); }
      if (sees) out.push({ k, extra });
    }
    if (out.length) return out;
    let best: { k: number; extra: number } | undefined;
    for (let rr = 0; rr <= 4 && !best; rr++) for (let j = cj - rr; j <= cj + rr; j++) for (let i = ci - rr; i <= ci + rr; i++) {
      if (Math.max(Math.abs(i - ci), Math.abs(j - cj)) !== rr || !free(i, j)) continue;
      const extra = dist(p, centre(i, j)); if (!best || extra < best.extra) best = { k: j * nx + i, extra };
    }
    return best ? [best] : [];
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
    const D = new Float64Array(nx * ny).fill(Infinity), s = seeds(p);
    if (s.length) {
      const heap: [number, number][] = s.map(x => [x.extra, x.k]); for (const x of s) D[x.k] = x.extra;
      const push = (v: number, k: number) => { heap.push([v, k]); let c = heap.length - 1; while (c) { const q = (c - 1) >> 1; if (heap[q]![0] <= heap[c]![0]) break; [heap[q], heap[c]] = [heap[c]!, heap[q]!]; c = q; } };
      const pop = () => { const top = heap[0]!, last = heap.pop()!; if (heap.length) { heap[0] = last; let c = 0; for (;;) { const l = 2 * c + 1, r = l + 1; let m = c; if (l < heap.length && heap[l]![0] < heap[m]![0]) m = l; if (r < heap.length && heap[r]![0] < heap[m]![0]) m = r; if (m === c) break; [heap[m], heap[c]] = [heap[c]!, heap[m]!]; c = m; } } return top; };
      while (heap.length) {
        const [v, k] = pop(); if (v > D[k]!) continue;
        const i = k % nx, j = Math.floor(k / nx);
        for (const [dx, dy] of STEPS) {
          if (!free(i + dx, j + dy) || !passes(i, j, dx, dy)) continue;
          const nk = (j + dy) * nx + i + dx;
          if (!stepOk(k, nk)) continue;
          const nv = v + Math.hypot(dx, dy) * CELL;
          if (nv < D[nk]!) { D[nk] = nv; push(nv, nk); }
        }
      }
    }
    memo.set(key, D); return D;
  };
  const m = (a: V, b: V) => {
    if (a[0] === b[0] && a[1] === b[1]) return 0;
    const t = seeds(b); if (!t.length || !seeds(a).length) return Infinity;
    const D = from(a);
    return Math.min(...t.map(x => D[x.k]! + x.extra));
  };
  return { free: p => !inBlocker(p) && !inSolid(p), m, min: (a, b) => { const x = m(a, b); return x === Infinity ? Infinity : x === 0 ? 0 : Math.ceil(x / speed / 60) + 1; } };
}
