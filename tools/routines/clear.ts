// What a round leg must keep clear of (Pulse, brief 022 follow-up; Sky 2026-10-24: "Inez seems to be walking on an invisible ledge").
// The site's walk grid (sim/data/handover.site.json) is 50 cm cells blocked by cell centre, and a polyline string-pulled across it cuts the corners of the blocked cells,
// so a leg can pass 2 to 20 cm from a drawn wall or 3 cm inside a chair's body clearance. This file measures a baked leg against the drawn solids (the ones the geometry
// audit G-1d reads: tools/sim/solids.ts drawnSolids, the collision volumes of the kit) and gives rounds.ts its measure:
//   gap     : the distance from a point or a leg to what a walker cannot step onto or walk under at that floor (zero inside)
// Brief 101 (the bake test's tripwire): the router that kept a leg further off (`inflate`, `pushes`) is retired with rules.rounds.clear's walker: since engine 0.49.0's
// walk law the grid keeps a body clear by itself. The measure stays: the bake's test holds every leg to it.
// Nothing here is the engine: Keel's engine walks the polyline the file carries. Integer centimetres in, metres inside (the solids' frame is the block frame, x south, y east).
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { derive } from '../plan/plan';
import { civic } from '../plan/civic';
import { CELL, WINDOW } from '../plan/walk-grid';
import { inPoly, segDist, segHit, type V } from '../plan/geom';
import { simGrid, HEAD, STEP, COLLIDE_FILE, type Solid } from '../sim/solids';
import type { Pt } from './walk';

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
}
