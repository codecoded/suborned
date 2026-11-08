// The zone-wide gap check (brief 098, Sky 2026-11-08: "don't be one of those game engines where you can't get past even though visibly it's clear you
// have a lot of space either side. We can do better"; engine 0.49.0, Keel): every pair of the walk's solids whose true gap fits a body, its margin and
// the walk raster's cell either side (FITS: 2 × the walk clearance + 2 × the raster's cell, 80 cm today) is a gap the engine's walk must cross, both
// ways. Found here on the solids' own geometry (never the raster), written to sim/data/handover.gaps.json with `bun run sim:site`; the engine proves
// each (sim/cli/Gaps.cs, `suborned-sim gaps`; sim/Suborned.Sim.Tests/Gaps.cs): its route from one side to the other passes between the two solids and
// is no detour. A gap:
//   · p, q: the two solids' nearest points; the gap is |pq|, from FITS to MAX (wider ground holds whole 50 cm cells and is open by construction);
//   · open: no third solid stands within the clearance and a raster cell of the free part of pq (one that does splits it into gaps of its own);
//   · crossed along the perpendicular through pq's middle m, from the farthest free point of a straight run of free ground (true geometry, the clearance
//     and a raster cell from every solid) up to RUN on one side to the same on the other; a run under NOTCH is no way on (a notch, not a passage).
// Drawn solids count where they block a walker at the floor under m (kit.collide.json's rule, as the raster cuts them).
import type { V } from '../plan/geom';
import { inPoly } from '../plan/geom';
import type { SimGrid } from './solids';
import type { WalkShape } from './walkfine';

export const MAX = 3, RUN = 1.5, NOTCH = 0.2, STEP_M = 0.05;
export type Gap = { a: string; b: string; gap: number; p: number[]; q: number[]; m: number[]; from: number[]; to: number[] };
export type GapFile = { schema: 'suborned.sim.gaps/1'; rule: string; clear: number; fits: number; max: number; gaps: Gap[]; skipped: { split: number; notch: number; offWindow: number } };

type Seg = [V, V];
const sub = (a: V, b: V): V => [a[0] - b[0], a[1] - b[1]];
const dot = (a: V, b: V) => a[0] * b[0] + a[1] * b[1];
/** The nearest points of two segments (p on the first, q on the second). */
function segSeg(a: Seg, b: Seg): [V, V] {
  const d1 = sub(a[1], a[0]), d2 = sub(b[1], b[0]), r = sub(a[0], b[0]), A = dot(d1, d1), E = dot(d2, d2), F = dot(d2, r);
  let s: number, t: number;
  if (A <= 1e-12 && E <= 1e-12) { s = 0; t = 0; }
  else if (A <= 1e-12) { s = 0; t = Math.min(1, Math.max(0, F / E)); }
  else {
    const C = dot(d1, r);
    if (E <= 1e-12) { t = 0; s = Math.min(1, Math.max(0, -C / A)); }
    else {
      const B = dot(d1, d2), den = A * E - B * B;
      s = den > 1e-12 ? Math.min(1, Math.max(0, (B * F - C * E) / den)) : 0;
      t = (B * s + F) / E;
      if (t < 0) { t = 0; s = Math.min(1, Math.max(0, -C / A)); } else if (t > 1) { t = 1; s = Math.min(1, Math.max(0, (B - C) / A)); }
    }
  }
  return [[a[0][0] + d1[0] * s, a[0][1] + d1[1] * s], [b[0][0] + d2[0] * t, b[0][1] + d2[1] * t]];
}
const edges = (poly: V[]): Seg[] => poly.map((a, k) => [a, poly[(k + 1) % poly.length]!]);
const nearestOn = (q: V, poly: V[]): V => { let best: V = poly[0]!, bd = Infinity; for (const [a, b] of edges(poly)) { const [p] = segSeg([a, b], [q, q]); const d = Math.hypot(p[0] - q[0], p[1] - q[1]); if (d < bd) { bd = d; best = p; } } return best; };
/** Distance from q to a shape (0 inside it). */
export function shapeDist(s: WalkShape, q: V): number {
  if (s.circle) return Math.max(0, Math.hypot(q[0] - s.circle.c[0], q[1] - s.circle.c[1]) - s.circle.r);
  if (inPoly(q, s.poly!)) return 0;
  const p = nearestOn(q, s.poly!); return Math.hypot(p[0] - q[0], p[1] - q[1]);
}
/** The two shapes' nearest points and their gap (0 when they touch or overlap). */
function nearest(a: WalkShape, b: WalkShape): { p: V; q: V; g: number } {
  if (a.circle && b.circle) {
    const v = sub(b.circle.c, a.circle.c), d = Math.hypot(v[0], v[1]) || 1e-9, g = d - a.circle.r - b.circle.r;
    return { p: [a.circle.c[0] + (v[0] / d) * a.circle.r, a.circle.c[1] + (v[1] / d) * a.circle.r], q: [b.circle.c[0] - (v[0] / d) * b.circle.r, b.circle.c[1] - (v[1] / d) * b.circle.r], g: Math.max(0, g) };
  }
  if (a.circle || b.circle) {
    const [c, poly, flip] = a.circle ? [a.circle, b.poly!, false] : [b.circle!, a.poly!, true];
    if (inPoly(c.c, poly)) return { p: c.c, q: c.c, g: 0 };
    const n = nearestOn(c.c, poly), v = sub(n, c.c), d = Math.hypot(v[0], v[1]) || 1e-9, on: V = [c.c[0] + (v[0] / d) * c.r, c.c[1] + (v[1] / d) * c.r];
    return flip ? { p: n, q: on, g: Math.max(0, d - c.r) } : { p: on, q: n, g: Math.max(0, d - c.r) };
  }
  if (a.poly!.some((v) => inPoly(v, b.poly!)) || b.poly!.some((v) => inPoly(v, a.poly!))) return { p: a.poly![0]!, q: a.poly![0]!, g: 0 };
  let best = { p: a.poly![0]!, q: b.poly![0]!, g: Infinity };
  for (const ea of edges(a.poly!)) for (const eb of edges(b.poly!)) { const [p, q] = segSeg(ea, eb), g = Math.hypot(p[0] - q[0], p[1] - q[1]); if (g < best.g) best = { p, q, g }; }
  return best;
}
const box = (s: WalkShape) => s.circle ? [s.circle.c[0] - s.circle.r, s.circle.c[0] + s.circle.r, s.circle.c[1] - s.circle.r, s.circle.c[1] + s.circle.r] : [Math.min(...s.poly!.map((p) => p[0])), Math.max(...s.poly!.map((p) => p[0])), Math.min(...s.poly!.map((p) => p[1])), Math.max(...s.poly!.map((p) => p[1]))];

/** Every gap of the zone (the file's head), from the site's own grid and its solids. */
export function findGaps(G: SimGrid): GapFile {
  const c = G.clear, f = G.fine.cell, fits = 2 * c + 2 * f, win = G.win, shapes = G.shapes, boxes = shapes.map(box);
  const coarseOf = (q: V) => { const [i, j] = G.cellOf(q); return i >= 0 && j >= 0 && i < G.nx && j < G.ny ? i * G.ny + j : -1; };
  const stands = (s: WalkShape, k: number) => k >= 0 && (!s.blocks || s.blocks(k));
  // a 2 m hash of the shapes' boxes, for the pairs and for "is this point clear of every solid"
  const H = 2, hash = new Map<string, number[]>(), key = (x: number, y: number) => `${Math.floor(x / H)},${Math.floor(y / H)}`;
  boxes.forEach((b, i) => { for (let x = Math.floor((b[0]! - MAX) / H); x <= Math.floor((b[1]! + MAX) / H); x++) for (let y = Math.floor((b[2]! - MAX) / H); y <= Math.floor((b[3]! + MAX) / H); y++) { const k = `${x},${y}`, l = hash.get(k); if (l) l.push(i); else hash.set(k, [i]); } });
  const near = (q: V) => hash.get(key(q[0], q[1])) ?? [];
  const clearAt = (q: V, k: number, but: Set<number>, by: number) => near(q).every((i) => but.has(i) || !stands(shapes[i]!, k) || shapeDist(shapes[i]!, q) >= by);
  const inWin = (q: V) => q[0] > win.x0 + 1 && q[0] < win.x1 - 1 && q[1] > win.y0 + 1 && q[1] < win.y1 - 1;
  const gaps: Gap[] = [], skipped = { split: 0, notch: 0, offWindow: 0 }, seen = new Set<string>();
  const cm = (v: V) => [Math.round(v[0] * 100), Math.round(v[1] * 100)];
  for (const [, list] of hash) for (const ia of list) for (const ib of list) {
    if (ib <= ia) continue;
    const pk = `${ia}/${ib}`; if (seen.has(pk)) continue; seen.add(pk);
    const A = boxes[ia]!, B = boxes[ib]!;
    if (A[0]! - B[1]! > MAX || B[0]! - A[1]! > MAX || A[2]! - B[3]! > MAX || B[2]! - A[3]! > MAX) continue;
    const { p, q, g } = nearest(shapes[ia]!, shapes[ib]!);
    if (g < fits || g > MAX) continue;
    const m: V = [(p[0] + q[0]) / 2, (p[1] + q[1]) / 2], k = coarseOf(m);
    if (!stands(shapes[ia]!, k) || !stands(shapes[ib]!, k)) continue; // one of the two does not block a walker on this floor (a canopy, a step)
    if (!inWin(m)) { skipped.offWindow++; continue; }
    const u: V = [(q[0] - p[0]) / g, (q[1] - p[1]) / g], t: V = [-u[1], u[0]], pair = new Set([ia, ib]);
    // open: the free part of pq clear of every third solid by the clearance and a cell
    let split = false;
    for (let s = c + f; s <= g - c - f + 1e-9 && !split; s += f / 2) { const x: V = [p[0] + u[0] * s, p[1] + u[1] * s]; if (!clearAt(x, k, pair, c + f)) split = true; }
    if (split) { skipped.split++; continue; }
    // the run across: free ground (every solid, the two included) both ways along t, up to RUN
    const run = (dir: number): V => { let last = m; for (let s = STEP_M; s <= RUN + 1e-9; s += STEP_M) { const x: V = [m[0] + t[0] * s * dir, m[1] + t[1] * s * dir]; if (!inWin(x) || !clearAt(x, coarseOf(x), new Set(), c + f)) break; last = x; } return last; };
    const from = run(1), to = run(-1);
    if (Math.hypot(from[0] - m[0], from[1] - m[1]) < NOTCH || Math.hypot(to[0] - m[0], to[1] - m[1]) < NOTCH) { skipped.notch++; continue; }
    gaps.push({ a: shapes[ia]!.id, b: shapes[ib]!.id, gap: Math.round(g * 100), p: cm(p), q: cm(q), m: cm(m), from: cm(from), to: cm(to) });
  }
  gaps.sort((x, y) => x.a.localeCompare(y.a) || x.b.localeCompare(y.b) || x.m[0]! - y.m[0]! || x.m[1]! - y.m[1]! || x.gap - y.gap);
  const one = gaps.filter((g, k) => k === 0 || JSON.stringify(g) !== JSON.stringify(gaps[k - 1])); // a piece's volumes stacked on one footprint make one gap, not one each
  return {
    schema: 'suborned.sim.gaps/1',
    rule: `brief 098 (Sky 2026-11-08, what is walkable is what you see): every pair of the walk's solids (the site's grid.src) whose true gap is from ${Math.round(fits * 100)} cm (two walk clearances and two raster cells) to ${MAX * 100} cm, open (no third solid within the clearance and a cell of its free part), crossed along the perpendicular through its middle from the end of a straight run of free ground on one side (up to ${RUN * 100} cm; under ${NOTCH * 100} cm is a notch, not a passage) to the other: the engine's walk must route from \`from\` to \`to\` and back, between p and q, no more than 1 m longer than straight (tools/sim/gaps.ts; sim/cli/Gaps.cs). Block frame, cm.`,
    clear: Math.round(c * 100), fits: Math.round(fits * 100), max: MAX * 100, gaps: one, skipped,
  };
}

/** The file as the site writes it: one gap a line, so a diff names what moved. */
export function gapsJson(f: GapFile): string {
  const head = JSON.stringify({ ...f, gaps: [] }, null, 1).replace(/\n "gaps": \[\],?/, '');
  return head.replace(/\n}$/, ',\n "gaps": [\n' + f.gaps.map((g) => '  ' + JSON.stringify(g)).join(',\n') + '\n ]\n}\n');
}
