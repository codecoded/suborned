// What is walkable is what you see (brief 098, Sky's calls on walking, 2026-11-08: "Gap between planter and fountain should be walkable, don't be
// one of those game engines where you can't get past even though visibly it's clear you have a lot of space either side. We can do better"; engine
// 0.49.0, Keel). The engine's walk raster (site grid.fine): every solid a walker meets keeps the walk clearance from a walker's centre, one value for
// every solid, a building's face included: the rulebook's body radius and its margin (bodies.radius + bodies.margin, 30 + 5 cm). The solids are the
// plan's walk blockers (built plots behind their building lines, the civic footprints and towers, water, landmarks: tools/plan/walk.ts blockersOf), the
// civic block's ground solids (civic.ts solids), the drawn solids where they block a walker at the floor (tools/sim/solids.ts) and the mission cameras'
// poles. Cut into FINE cells: a cell is free when its centre clears every solid. The 50 cm grid (grid.rows) is this raster read at each 50 cm cell's
// centre (the engine's route topology and every tool's coarse read), so nothing reads a second opinion of the ground.
// Was (engine 0.30.0 to 0.48.0): the plan's design r (standards.walking.body.r, 0.65 m, Datum's layout standard for W-5, calibrated so two measured
// walks came out the right length) grown round every ground solid, a face at 0.3 m, each 50 cm cell blocked by its centre: the fountain's four 2.05 m
// planter gaps were one diagonal line of cells, and the engine's A* never cuts a corner. The plan keeps its r for its own W-rules (walk-solids.ts).
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import type { V } from '../plan/geom';
import { inPoly } from '../plan/geom';

const RULEBOOK = join(import.meta.dir, '..', '..', 'contracts/rules/rulebook.json');
/** The walk clearance (m): the rulebook's bodies.radius + bodies.margin (a value ref each), what the engine checks the site's grid.body against. */
export function walkClear(rulebook: { values: Record<string, { v: number }>; bodies?: { radius?: string; margin?: string } } = JSON.parse(readFileSync(RULEBOOK, 'utf8'))): number {
  const v = (ref: string | undefined, what: string) => { const k = ref?.startsWith('v:') ? ref.slice(2) : undefined, x = k ? rulebook.values[k]?.v : undefined; if (typeof x !== 'number') throw new Error(`rulebook bodies.${what} is not a value ref (${ref})`); return x; };
  return (v(rulebook.bodies?.radius, 'radius') + v(rulebook.bodies?.margin, 'margin')) / 100;
}

/** The walk raster's cell (m): 5 cm. A walker's centre in a free cell is within 3.5 cm of a centre that clears the clearance, and a gap two clearances
 *  and one cell wide (75 cm at 35 cm) always holds a line of free cells across it. */
export const FINE = 0.05;

/** One solid as the raster reads it, block frame, metres: a polygon (its inside blocks) or a disc, and whether it blocks a walker at a 50 cm cell's floor. */
export type WalkShape = { id: string; poly?: V[]; circle?: { c: V; r: number }; drawn: boolean; blocks?: (coarse: number) => boolean };

export type FineRaster = {
  cell: number; nx: number; ny: number;
  /** Per fine cell (index i·ny + j, i along x): 0 free, 1 blocked by the plan or the civic block's solids, 2 by a drawn solid or a pole only. */
  data: Uint8Array;
  free(q: V): boolean;
};

/** The raster over the window (x0, y0 its corner; nx, ny coarse cells of `coarse` m), each solid keeping `clear` (m) from a free cell's centre. */
export function fineRaster(win: { x0: number; y0: number }, nxC: number, nyC: number, coarse: number, clear: number, shapes: WalkShape[], cell = FINE): FineRaster {
  const per = Math.round(coarse / cell), nx = nxC * per, ny = nyC * per, data = new Uint8Array(nx * ny);
  const fx = (i: number) => win.x0 + (i + 0.5) * cell, fy = (j: number) => win.y0 + (j + 0.5) * cell;
  const coarseOf = (i: number, j: number) => Math.floor(i / per) * nyC + Math.floor(j / per);
  // the shapes' edges listed in every 50 cm cell they can reach (within the clearance of any of its fine cells' centres)
  const reach = clear + coarse * Math.SQRT1_2 + 1e-6, lists = new Map<number, number[]>(); // coarse cell → [shape, edge] pairs, flat
  const edgesOf = (s: WalkShape): [V, V][] => (s.poly ? s.poly.map((a, k) => [a, s.poly![(k + 1) % s.poly!.length]!]) : []);
  const allEdges: [V, V][][] = shapes.map(edgesOf);
  const segD2 = (px: number, py: number, a: V, b: V) => {
    const abx = b[0] - a[0], aby = b[1] - a[1], l2 = abx * abx + aby * aby, t = l2 > 0 ? Math.max(0, Math.min(1, ((px - a[0]) * abx + (py - a[1]) * aby) / l2)) : 0;
    const dx = px - (a[0] + abx * t), dy = py - (a[1] + aby * t); return dx * dx + dy * dy;
  };
  const listIn = (s: number, e: number, x0: number, x1: number, y0: number, y1: number, near: (cx: number, cy: number) => boolean) => {
    const i0 = Math.max(0, Math.floor((x0 - reach - win.x0) / coarse)), i1 = Math.min(nxC - 1, Math.floor((x1 + reach - win.x0) / coarse));
    const j0 = Math.max(0, Math.floor((y0 - reach - win.y0) / coarse)), j1 = Math.min(nyC - 1, Math.floor((y1 + reach - win.y0) / coarse));
    for (let i = i0; i <= i1; i++) for (let j = j0; j <= j1; j++) {
      if (!near(win.x0 + (i + 0.5) * coarse, win.y0 + (j + 0.5) * coarse)) continue;
      const k = i * nyC + j, l = lists.get(k); if (l) l.push(s, e); else lists.set(k, [s, e]);
    }
  };
  shapes.forEach((s, si) => {
    if (s.circle) { const { c, r } = s.circle; listIn(si, -1, c[0] - r, c[0] + r, c[1] - r, c[1] + r, (cx, cy) => Math.hypot(cx - c[0], cy - c[1]) < r + reach); return; }
    allEdges[si]!.forEach(([a, b], ei) => listIn(si, ei, Math.min(a[0], b[0]), Math.max(a[0], b[0]), Math.min(a[1], b[1]), Math.max(a[1], b[1]), (cx, cy) => segD2(cx, cy, a, b) < reach * reach));
  });
  const c2 = clear * clear;
  // two passes, the plan's and the block's solids first, so a cell the drawn solids alone block is told apart (the site's drawn.walkCells)
  for (const drawnPass of [false, true]) {
    const mark = drawnPass ? 2 : 1;
    // inside: each polygon's own cells, a scanline per fine row (even-odd, half-open at vertices)
    shapes.forEach((s, si) => {
      if (s.drawn !== drawnPass || !s.poly) return;
      const xs = s.poly.map((p) => p[0]), lo = Math.max(0, Math.floor((Math.min(...xs) - win.x0) / cell)), hi = Math.min(nx - 1, Math.floor((Math.max(...xs) - win.x0) / cell));
      for (let i = lo; i <= hi; i++) {
        const x = fx(i), ys: number[] = [];
        for (const [a, b] of allEdges[si]!) if ((a[0] <= x && x < b[0]) || (b[0] <= x && x < a[0])) ys.push(a[1] + ((x - a[0]) * (b[1] - a[1])) / (b[0] - a[0]));
        ys.sort((p, q) => p - q);
        for (let k = 0; k + 1 < ys.length; k += 2) {
          const j0 = Math.max(0, Math.ceil((ys[k]! - win.y0) / cell - 0.5)), j1 = Math.min(ny - 1, Math.floor((ys[k + 1]! - win.y0) / cell - 0.5));
          for (let j = j0; j <= j1; j++) { const q = i * ny + j; if (data[q] === 0 && (!s.blocks || s.blocks(coarseOf(i, j)))) data[q] = mark; }
        }
      }
    });
    // within the clearance of an edge or a disc
    for (const [k, l] of lists) {
      const ci = Math.floor(k / nyC), cj = k % nyC, pairs: [number, number][] = [];
      for (let n = 0; n < l.length; n += 2) { const s = shapes[l[n]!]!; if (s.drawn === drawnPass && (!s.blocks || s.blocks(k))) pairs.push([l[n]!, l[n + 1]!]); }
      if (!pairs.length) continue;
      for (let a = 0; a < per; a++) for (let b = 0; b < per; b++) {
        const i = ci * per + a, j = cj * per + b, q = i * ny + j; if (data[q] !== 0) continue;
        const x = fx(i), y = fy(j);
        for (const [si, ei] of pairs) {
          const s = shapes[si]!;
          const hit = ei < 0 ? Math.hypot(x - s.circle!.c[0], y - s.circle!.c[1]) < s.circle!.r + clear : segD2(x, y, allEdges[si]![ei]![0], allEdges[si]![ei]![1]) < c2;
          if (hit) { data[q] = mark; break; }
        }
      }
    }
  }
  const free = (q: V) => { const i = Math.floor((q[0] - win.x0) / cell + 1e-9), j = Math.floor((q[1] - win.y0) / cell + 1e-9); return i >= 0 && j >= 0 && i < nx && j < ny && data[i * ny + j] === 0; };
  return { cell, nx, ny, data, free };
}

/** A polygon's inside test kept for callers that hold one shape (the scanline above is the raster's own). */
export const insideShape = (s: WalkShape, q: V) => (s.circle ? Math.hypot(q[0] - s.circle.c[0], q[1] - s.circle.c[1]) < s.circle.r : !!s.poly && inPoly(q, s.poly));

/** The raster as the site writes it: per fine row (fixed x), run lengths of free and blocked cells, starting with free (a row that starts blocked opens with 0). */
export function fineRows(r: FineRaster): number[][] {
  const out: number[][] = [];
  for (let i = 0; i < r.nx; i++) {
    const row: number[] = []; let free = true, n = 0;
    for (let j = 0; j < r.ny; j++) { const f = r.data[i * r.ny + j] === 0; if (f === free) n++; else { row.push(n); free = f; n = 1; } }
    row.push(n); out.push(row);
  }
  return out;
}
