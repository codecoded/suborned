// The walk grid the engine and the world rules share: the civic block's window in block frame, 0.5 m cells, a cell free when its centre is outside every
// walk blocker (tools/plan/walk.ts blockersOf: built plots, the civic footprints and towers, water, landmarks) and BODY clear of every ground solid
// (walk-solids.ts). tools/sim/site.ts writes it for the engine (sim/data/handover.site.json `grid`); tools/world/civic/quiet.ts measures report
// and response reach on it (brief 013 C), so a call box covers what a walker in the engine walks, not a second opinion of the ground.
import type { Civic } from './civic';
import { toPlanFn } from './civic';
import { type V, dist, inPoly } from './geom';
import type { Derived, Plan } from './plan';
import { blockersOf } from './walk';
import { Solids } from './walk-solids';

/** Block frame, m: the square, the café, the Office, Apron Road and the street mouths. */
export const WINDOW = { x0: -52, x1: 112, y0: -66, y1: 16 };
export const CELL = 0.5; // m

export type Window = typeof WINDOW;
export type WalkGrid = {
  win: Window; cell: number; nx: number; ny: number; rows: string[]; solids: Solids;
  /** The centre of cell (i, j), block frame. */
  centre(i: number, j: number): V;
  /** True if the block-frame point stands on a free cell. */
  free(q: V): boolean;
  /** The nearest free cell centre to q (ties: lowest i, then j), for a point that stands on a face or beside a solid. */
  nearestFree(q: V): V;
  /** The same, or undefined where no free ground lies within 6 m (a plan that has built over the door): the rules report that, they do not crash on it. */
  tryNearestFree(q: V): V | undefined;
  /** The cell (i, j) the block-frame point q is in (may lie outside the grid), and whether cell (i, j) is free. */
  cellOf(q: V): [number, number];
  cellFree(i: number, j: number): boolean;
};

export function walkGrid(plan: Plan, d: Derived, civ: Civic, win: Window = WINDOW, cell = CELL): WalkGrid {
  const toPlan = toPlanFn(plan.civic);
  const { polys, rounds } = blockersOf(plan, d, civ);
  const solids = new Solids(civ.solids);
  const nx = Math.round((win.x1 - win.x0) / cell), ny = Math.round((win.y1 - win.y0) / cell);
  const centre = (i: number, j: number): V => [win.x0 + (i + 0.5) * cell, win.y0 + (j + 0.5) * cell];
  const boxed = polys.map((b) => ({ b, x0: Math.min(...b.map((v) => v[0])), x1: Math.max(...b.map((v) => v[0])), y0: Math.min(...b.map((v) => v[1])), y1: Math.max(...b.map((v) => v[1])) }));
  const blockedAt = (q: V) => {
    const p = toPlan(q);
    if (boxed.some((k) => p[0] >= k.x0 && p[0] <= k.x1 && p[1] >= k.y0 && p[1] <= k.y1 && inPoly(p, k.b))) return true;
    if (rounds.some((l) => dist(p, l.c) < l.r)) return true;
    return !solids.clear(p, p, solids.near(p[0], p[0], p[1], p[1]));
  };
  const rows: string[] = [];
  for (let i = 0; i < nx; i++) { let r = ''; for (let j = 0; j < ny; j++) r += blockedAt(centre(i, j)) ? '#' : '.'; rows.push(r); }
  const free = (q: V) => { const i = Math.floor((q[0] - win.x0) / cell), j = Math.floor((q[1] - win.y0) / cell); return i >= 0 && j >= 0 && i < nx && j < ny && rows[i]![j] === '.'; };
  const tryNearestFree = (q: V): V | undefined => {
    const ci = Math.floor((q[0] - win.x0) / cell), cj = Math.floor((q[1] - win.y0) / cell);
    let best: V | undefined, bd = Infinity;
    for (let r = 0; r <= 12; r++) {
      for (let i = ci - r; i <= ci + r; i++) for (let j = cj - r; j <= cj + r; j++) {
        if (i < 0 || j < 0 || i >= nx || j >= ny || rows[i]![j] !== '.') continue;
        const c = centre(i, j), dd = dist(c, q);
        if (dd < bd) { bd = dd; best = c; }
      }
      if (best && bd <= (r + 0.5) * cell) break;
    }
    return best;
  };
  const nearestFree = (q: V): V => { const b = tryNearestFree(q); if (!b) throw new Error(`no free ground within 6 m of ${q}`); return b; };
  const cellOf = (q: V): [number, number] => [Math.floor((q[0] - win.x0) / cell), Math.floor((q[1] - win.y0) / cell)];
  const cellFree = (i: number, j: number) => i >= 0 && j >= 0 && i < nx && j < ny && rows[i]![j] === '.';
  return { win, cell, nx, ny, rows, solids, centre, free, nearestFree, tryNearestFree, cellOf, cellFree };
}

/** block frame → plan: the inverse of plan = o + bx·ax + by·ay. */
export function toBlockFn(spec: { blockToPlan: { origin: V; x: V; y: V } }): (p: V) => V {
  const o = spec.blockToPlan.origin, ax = spec.blockToPlan.x, ay = spec.blockToPlan.y;
  const det = ax[0] * ay[1] - ax[1] * ay[0];
  return (p) => { const dx = p[0] - o[0], dy = p[1] - o[1]; return [(dx * ay[1] - dy * ay[0]) / det, (ax[0] * dy - ax[1] * dx) / det]; };
}

/** The walk distance (cm, integer) from the nearest source over the grid: 8 neighbours, no corner cutting, 50 / 70 cm steps for a 0.5 m cell, exactly the engine's A*
 *  costs (sim/Suborned.Sim/Grid.cs) without its string-pulling, so a field is an upper bound on what a walker walks. `max` stops the search (cells farther are `INF`). */
export const INF = 0x7fffffff;
export function reachField(g: WalkGrid, sources: V[], max = INF): Int32Array {
  const { nx, ny } = g, orth = Math.round(g.cell * 100), diag = Math.floor((orth * 1414) / 1000), out = new Int32Array(nx * ny).fill(INF);
  const heap: [number, number][] = []; // [cost, cell]
  const push = (c: number, k: number) => { heap.push([c, k]); let i = heap.length - 1; while (i > 0) { const p = (i - 1) >> 1; if (heap[p]![0] <= heap[i]![0]) break; [heap[p], heap[i]] = [heap[i]!, heap[p]!]; i = p; } };
  const pop = (): [number, number] => { const top = heap[0]!, last = heap.pop()!; if (heap.length) { heap[0] = last; let i = 0; for (;;) { const l = 2 * i + 1, r = l + 1; let m = i; if (l < heap.length && heap[l]![0] < heap[m]![0]) m = l; if (r < heap.length && heap[r]![0] < heap[m]![0]) m = r; if (m === i) break; [heap[m], heap[i]] = [heap[i]!, heap[m]!]; i = m; } } return top; };
  for (const q of sources) { const f = g.free(q) ? q : g.tryNearestFree(q); if (!f) continue; const [i, j] = g.cellOf(f); if (g.cellFree(i, j) && out[i * ny + j]! > 0) { out[i * ny + j] = 0; push(0, i * ny + j); } }
  const DI = [1, -1, 0, 0, 1, 1, -1, -1], DJ = [0, 0, 1, -1, 1, -1, 1, -1];
  while (heap.length) {
    const [c, k] = pop(); if (c > out[k]!) continue;
    const i = Math.floor(k / ny), j = k % ny;
    for (let n = 0; n < 8; n++) {
      const a = i + DI[n]!, b = j + DJ[n]!; if (!g.cellFree(a, b)) continue;
      if (n >= 4 && !(g.cellFree(i + DI[n]!, j) && g.cellFree(i, j + DJ[n]!))) continue; // no corner cutting
      const nc = c + (n < 4 ? orth : diag); if (nc > max || nc >= out[a * ny + b]!) continue;
      out[a * ny + b] = nc; push(nc, a * ny + b);
    }
  }
  return out;
}
