// What a round leg must keep clear of (Pulse, brief 022 follow-up; Sky 2026-10-24: "Inez seems to be walking on an invisible ledge").
// The site's walk grid (sim/data/handover.site.json) is 50 cm cells blocked by cell centre, and a polyline string-pulled across it cuts the corners of the blocked cells,
// so a leg can pass 2 to 20 cm from a drawn wall or 3 cm inside a chair's body clearance. This file measures a baked leg against the drawn solids (the ones the geometry
// audit G-1d reads: tools/sim/solids.ts drawnSolids, the collision volumes of the kit) and gives rounds.ts the two things it needs:
//   gap     : the distance from a point or a leg to what a walker cannot step onto or walk under at that floor (zero inside)
//   inflate : the site grid with every cell blocked whose centre is closer than `need + extra` to such a solid, so the router keeps further off
// Nothing here is the engine: Keel's engine walks the polyline the file carries. Integer centimetres in, metres inside (the solids' frame is the block frame, x south, y east).
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { derive } from '../plan/plan';
import { civic } from '../plan/civic';
import { CELL, WINDOW } from '../plan/walk-grid';
import { inPoly, segDist, segHit, type V } from '../plan/geom';
import { simGrid, HEAD, STEP, COLLIDE_FILE, type Solid } from '../sim/solids';
import { Grid, type Pt } from './walk';

const root = join(import.meta.dir, '..', '..');
const FLUSH = 0.05; // a stand top within this of the floor is that floor (tools/sim/solids.ts)

/** The body clearance the walk grid keeps from a solid (engine 0.49.0: the rulebook's bodies.radius + margin; was the plan's standards.walking.body.r), in cm. */
export type Ground = { bodyCm: number; solids: Solid[]; floorAt(q: V): number; sha: string };

export function loadGround(): Ground {
  const read = (f: string) => readFileSync(join(root, f), 'utf8');
  const plan = JSON.parse(read('contracts/world/harbour-district.plan.json')), block = JSON.parse(read(plan.civic.block));
  const d = derive(plan), civ = civic(plan.civic, block), G = simGrid(plan, d, civ, 1.6, WINDOW, CELL, true);
  const floorAt = (q: V) => { const i = Math.floor((q[0] - G.win.x0) / G.cell), j = Math.floor((q[1] - G.win.y0) / G.cell); return G.floor[i * G.ny + j] ?? 0; };
  return { bodyCm: Math.round(G.clear * 100), solids: G.drawn.solids, floorAt, sha: new Bun.CryptoHasher('sha256').update(read(COLLIDE_FILE)).digest('hex') }; // engine 0.49.0 (brief 098, Keel): the walk's clearance (the rulebook's bodies.radius + margin), one value everywhere; was the plan's design r (65 cm), which stays the plan's
}

/** Distance (m) from the segment a-b to the solid; zero when they touch or the segment is inside. */
function segToSolid(s: Solid, a: V, b: V): number {
  if (s.circle) return Math.max(0, segDist(s.circle.c, a, b) - s.circle.r);
  if (inPoly(a, s.poly) || inPoly(b, s.poly)) return 0;
  let best = Infinity;
  for (let k = 0; k < s.poly.length; k++) {
    const c = s.poly[k]!, e = s.poly[(k + 1) % s.poly.length]!;
    if (segHit(a, b, c, e)) return 0;
    best = Math.min(best, segDist(a, c, e), segDist(b, c, e), segDist(c, a, b), segDist(e, a, b));
  }
  return best;
}

const onSeg = (p: V, a: V, b: V): V => { const dx = b[0] - a[0], dy = b[1] - a[1], l2 = dx * dx + dy * dy, t = l2 ? Math.max(0, Math.min(1, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dy) / l2)) : 0; return [a[0] + dx * t, a[1] + dy * t]; };
/** The closest pair between the segment a-b and the solid: the distance (zero when they touch), the point on the segment and the point on the solid. */
function closest(s: Solid, a: V, b: V): { g: number; q: V; r: V } {
  if (s.circle) { const q = onSeg(s.circle.c, a, b), d = Math.hypot(q[0] - s.circle.c[0], q[1] - s.circle.c[1]) || 1; return { g: Math.max(0, d - s.circle.r), q, r: [s.circle.c[0] + ((q[0] - s.circle.c[0]) / d) * s.circle.r, s.circle.c[1] + ((q[1] - s.circle.c[1]) / d) * s.circle.r] }; }
  if (inPoly(a, s.poly) || inPoly(b, s.poly)) return { g: 0, q: a, r: a };
  let best = { g: Infinity, q: a, r: a };
  for (let k = 0; k < s.poly.length; k++) {
    const c = s.poly[k]!, e = s.poly[(k + 1) % s.poly.length]!;
    if (segHit(a, b, c, e)) return { g: 0, q: a, r: a };
    for (const [q, r] of [[a, onSeg(a, c, e)], [b, onSeg(b, c, e)], [onSeg(c, a, b), c], [onSeg(e, a, b), e]] as [V, V][]) { const g = Math.hypot(q[0] - r[0], q[1] - r[1]); if (g < best.g) best = { g, q, r }; }
  }
  return best;
}

export type Clearance = { bodyCm: number; marginCm: number; faceCm: number };

export class Clear {
  /** the solids that stop or hinder a walker on their floor, with the distance each must be kept at (m) */
  private readonly obs: { s: Solid; need: number }[];
  constructor(readonly ground: Ground, readonly c: Clearance) {
    this.obs = [];
    for (const s of ground.solids) {
      const mid: V = [(s.x0 + s.x1) / 2, (s.y0 + s.y1) / 2], f = ground.floorAt(mid);
      if (!(s.z0 < f + HEAD && s.z1 > f + STEP)) continue; // under a step or over her head
      if (s.stand && Math.abs(s.z1 - f) <= FLUSH) continue; // a top that is the floor
      this.obs.push({ s, need: (s.face ? c.faceCm : c.bodyCm + c.marginCm) / 100 });
    }
  }
  /** How far short of its keep-off distance the segment a-b comes (cm, 0 = clear), over every solid near it. */
  shortfall(a: Pt, b: Pt): number {
    const A: V = [a[0] / 100, a[1] / 100], B: V = [b[0] / 100, b[1] / 100];
    const x0 = Math.min(A[0], B[0]), x1 = Math.max(A[0], B[0]), y0 = Math.min(A[1], B[1]), y1 = Math.max(A[1], B[1]);
    let worst = 0;
    for (const { s, need } of this.obs) {
      if (s.x1 < x0 - need || s.x0 > x1 + need || s.y1 < y0 - need || s.y0 > y1 + need) continue;
      const g = segToSolid(s, A, B); if (g < need) worst = Math.max(worst, need - g);
    }
    return Math.round(worst * 100);
  }
  /** The worst shortfall along a polyline, skipping the first `skipCm` of its length (the post is on the door's own face). */
  pathShortfall(pts: Pt[], skipCm = 0): number {
    let worst = 0, run = 0;
    for (let i = 1; i < pts.length; i++) {
      let a = pts[i - 1]!; const b = pts[i]!, L = Math.hypot(b[0] - a[0], b[1] - a[1]);
      if (run + L <= skipCm) { run += L; continue; }
      if (run < skipCm && L > 0) { const t = (skipCm - run) / L; a = [Math.round(a[0] + (b[0] - a[0]) * t), Math.round(a[1] + (b[1] - a[1]) * t)]; }
      run += L; worst = Math.max(worst, this.shortfall(a, b));
    }
    return worst;
  }
  /** For each segment of the polyline that comes short (past the first `skipCm`): the way to move its ends to clear its worst solid and how far (cm): away from the closest point of the solid, by the shortfall and 1 cm. */
  pushes(pts: Pt[], skipCm = 0): { k: number; n: V; cm: number }[] {
    const out: { k: number; n: V; cm: number }[] = []; let run = 0;
    for (let i = 1; i < pts.length; i++) {
      let a = pts[i - 1]!; const b = pts[i]!, L = Math.hypot(b[0] - a[0], b[1] - a[1]);
      if (run + L <= skipCm) { run += L; continue; }
      if (run < skipCm && L > 0) { const t = (skipCm - run) / L; a = [Math.round(a[0] + (b[0] - a[0]) * t), Math.round(a[1] + (b[1] - a[1]) * t)]; }
      run += L;
      const A: V = [a[0] / 100, a[1] / 100], B: V = [b[0] / 100, b[1] / 100];
      let w: { n: V; cm: number } | undefined;
      for (const { s, need } of this.obs) {
        if (s.x1 < Math.min(A[0], B[0]) - need || s.x0 > Math.max(A[0], B[0]) + need || s.y1 < Math.min(A[1], B[1]) - need || s.y0 > Math.max(A[1], B[1]) + need) continue;
        const c = closest(s, A, B); if (c.g >= need || c.g === 0) continue;
        const d = Math.hypot(c.q[0] - c.r[0], c.q[1] - c.r[1]) || 1, cm = Math.round((need - c.g) * 100) + 1;
        if (!w || cm > w.cm) w = { n: [(c.q[0] - c.r[0]) / d, (c.q[1] - c.r[1]) / d], cm };
      }
      if (w) out.push({ k: i - 1, ...w });
    }
    return out;
  }
  /** The site grid with every cell blocked whose centre is within `need + extraCm` of a solid, but the cells in `keep` (the ends of a leg) left as they were. */
  inflate(grid: ConstructorParameters<typeof Grid>[0], extraCm: number, keep: Pt[]): Grid {
    const rows = grid.rows.map((r) => r.split('')), cell = grid.cell, ex = extraCm / 100;
    const ci = (x: number) => Math.floor((x * 100 - grid.x0) / cell), cj = (y: number) => Math.floor((y * 100 - grid.y0) / cell);
    for (const { s, need } of this.obs) {
      const r = need + ex;
      const i0 = Math.max(0, ci(s.x0 - r)), i1 = Math.min(grid.nx - 1, ci(s.x1 + r)), j0 = Math.max(0, cj(s.y0 - r)), j1 = Math.min(grid.ny - 1, cj(s.y1 + r));
      for (let i = i0; i <= i1; i++) for (let j = j0; j <= j1; j++) {
        if (rows[i]![j] !== '.') continue;
        const c: V = [(grid.x0 + i * cell + Math.floor(cell / 2)) / 100, (grid.y0 + j * cell + Math.floor(cell / 2)) / 100];
        if (segToSolid(s, c, c) < r) rows[i]![j] = '#';
      }
    }
    // the ends of the leg stand where they stand: the cell of each stays free if it was
    const orig = new Grid(grid);
    for (const p of keep) if (orig.isFree(p)) rows[orig.ci(p[0])]![orig.cj(p[1])] = '.';
    return new Grid({ ...grid, rows: rows.map((r) => r.join('')), fine: undefined }); // engine 0.49.0 (Keel): routed on these inflated cells, not the site's walk raster (the grid's own walk keeps only the body's clearance)
  }
}
