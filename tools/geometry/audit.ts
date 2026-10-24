// The three audits of brief 055 (Splice): G-1 ground height, G-2 walk against what is drawn, G-3 sight and fire against what is drawn.
// The sim side speaks block frame metres (x south, y east); the drawn side plan frame; `sim.frame` converts.
import { inPoly, segDist, type V } from '../plan/geom';
import { CELL, FLUSH, HEAD, Raster, STEP } from './raster';
import type { Visible } from './visible';
import type { Sim } from './sim';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT } from './pieces';
import type { World } from '../../contracts/world/world.schema';
import { type Solid, drawnSolids } from '../sim/solids';
import { cameraPoles, fixedLegs } from '../missions/handover/legs';
import { floorAt } from '../plan/civic';

export const TOL = 0.05;   // m: a person within this of the drawn floor stands on it
export const BODY_R = 0.3; // m: a walker's body radius for the brush test
export const CHEST = 1.2;  // m: the height a shot travels at (HostFightLab draws its lines at chest height)

export type Cell = { g: [number, number]; b: V; delta: number; host: number; top: number; ground: number; owner: string; rise: number };
export type Group = { id: string; kind: string; instances: number; count: number; area: number; max: number; mean: number; worst: V; bbox: [number, number, number, number]; note?: string };

const r2 = (n: number) => Math.round(n * 100) / 100;
const group = (rows: { id: string; v: number; b: V; own?: string }[], kind: (id: string) => string): Group[] => {
  const m = new Map<string, { n: number; sum: number; max: number; worst: V; x0: number; x1: number; y0: number; y1: number; own: Set<string> }>();
  for (const r of rows) {
    const g = m.get(r.id) ?? { n: 0, sum: 0, max: 0, worst: r.b, x0: Infinity, x1: -Infinity, y0: Infinity, y1: -Infinity, own: new Set<string>() };
    g.own.add(r.own ?? r.id);
    g.n++; g.sum += r.v; if (Math.abs(r.v) > Math.abs(g.max) || g.n === 1) { g.max = r.v; g.worst = r.b; }
    g.x0 = Math.min(g.x0, r.b[0]); g.x1 = Math.max(g.x1, r.b[0]); g.y0 = Math.min(g.y0, r.b[1]); g.y1 = Math.max(g.y1, r.b[1]); m.set(r.id, g);
  }
  return [...m].map(([id, g]) => ({ id, kind: '', instances: g.own.size, count: g.n, area: r2(g.n * 0.25), max: r2(g.max), mean: r2(g.sum / g.n), worst: [r2(g.worst[0]), r2(g.worst[1])] as V, bbox: [r2(g.x0), r2(g.y0), r2(g.x1), r2(g.y1)] as [number, number, number, number] })).sort((a, b) => b.count - a.count);
};

export type Ctx = { sim: Sim; vis: Visible };
/** Elements the world repeats by the hundred (kerb stones, bollards, trees) are reported by kind: `street:kerb`; the civic block's pieces, facade bays and buildings one by one. */
export const keyOf = (vis: Visible) => (id: string): string => {
  const m = vis.meta.get(id); if (!m) return id.replace(/^(road\/[^/]+)\/kerb$/, 'kerb strip $1').replace(/^(plot|road|foot)\/.*$/, 'ground layer');
  return m.layer === 'street' || m.layer === 'ground' ? `${m.layer}:${m.kind}` : m.layer === 'frontage' ? `frontage:${m.kind}` : m.layer === 'facade' ? `facade:${m.kind}` : id;
};
const withKind = (vis: Visible, gs: Group[]): Group[] => gs.map(g => ({ ...g, kind: vis.meta.get(g.id)?.kind ?? (g.id.includes(':') ? g.id.split(':')[1]! : '') }));

/** The raster column under a block-frame point (the cell the point is in). A walk cell's centre lies on the corner of four raster cells; 5 cm south-east of it is in one of them, and the walk file is read at the same point. */
export const NUDGE = 0.05;
/** The legs' read of the cell under a route point: 1 mm south-east, so a point on a cell edge takes one cell by a fixed rule, not by rounding. */
export const LEG_NUDGE = 0.001;
export function colAt(c: Ctx, b: V) {
  const p = c.sim.frame.toPlan([b[0] + NUDGE, b[1] + NUDGE]), [i, j] = c.vis.R.cellOf(p);
  return c.vis.R.inside(i, j) ? c.vis.R.column(i, j) : undefined;
}

// ------------------------------------------------------------------ G-1 ground height
export type PlaceRow = { id: string; at: V; src: string; people: number; host: number; top: number; delta: number; owner: string; rise: number; walkable: boolean; blockedBy: string };
export type GroundAudit = {
  cells: Cell[];
  walkCells: number; walkableCells: number; sunk: number; floating: number; bands: Record<string, number>;
  byOwner: Group[]; byCause: Record<string, number>;
  places: PlaceRow[];
  persons: { id: string; role: string; at: V; place: string; host: number; top: number; delta: number; owner: string; rise: number; walkable: boolean }[];
  hostCheck: { cells: number; hostOnSlab: number; hostZero: number };
};

export function auditGround(c: Ctx): GroundAudit {
  const { sim, vis } = c, g = sim.grid;
  const cells: Cell[] = [], rows: { id: string; v: number; b: V; own?: string }[] = [], byCause: Record<string, number> = {};
  const bands: Record<string, number> = { 'sunk 5-15 cm': 0, 'sunk 15-50 cm': 0, 'sunk 0.5 m or more': 0, 'floats 5-15 cm': 0, 'floats 15 cm or more': 0 };
  let walk = 0, walkable = 0, sunk = 0, floating = 0;
  for (let i = 0; i < g.nx; i++) for (let j = 0; j < g.ny; j++) {
    if (!g.free(i, j)) continue; walk++;
    const b = g.centre(i, j), col = colAt(c, b); if (!col || !col.walkable) continue; walkable++;
    const host = sim.host.at(b), delta = col.top - host;
    if (Math.abs(delta) <= TOL) continue;
    cells.push({ g: [i, j], b, delta, host, top: col.top, ground: col.ground, owner: col.owner, rise: col.rise });
    rows.push({ id: keyOf(vis)(col.owner), own: col.owner, v: delta, b });
    if (delta > 0) { sunk++; bands[delta <= 0.15 ? 'sunk 5-15 cm' : delta < 0.5 ? 'sunk 15-50 cm' : 'sunk 0.5 m or more']!++; } else { floating++; bands[delta >= -0.15 ? 'floats 5-15 cm' : 'floats 15 cm or more']!++; }
    const cause = col.rise > FLUSH ? 'a drawn piece (stair, platform, kerb stone) raises the floor' : col.ground > host ? 'ground layer drawn above the host height (kerb strip, quantised edge)' : 'ground layer drawn below the host height (quantised edge)';
    byCause[cause] = (byCause[cause] ?? 0) + 1;
  }
  const row = (p: { id: string; at: V; src?: string; people?: number }): PlaceRow => {
    const col = colAt(c, p.at), host = sim.host.at(p.at), bl = sim.blockedBy(p.at);
    return { id: p.id, at: p.at, src: p.src ?? '', people: p.people ?? 0, host, top: r2(col?.top ?? NaN), delta: r2((col?.top ?? NaN) - host), owner: col?.owner ?? '?', rise: r2(col?.rise ?? NaN), walkable: col?.walkable ?? false, blockedBy: bl.kind === 'free' ? '' : `${bl.kind}:${bl.id}` };
  };
  const places = sim.places.map(p => row(p));
  const persons = sim.persons.map(p => { const r = row({ id: p.id, at: p.at }); return { id: p.id, role: p.role, at: p.at, place: p.place, host: r.host, top: r.top, delta: r.delta, owner: r.owner, rise: r.rise, walkable: r.walkable }; });
  let onSlab = 0, zero = 0; for (const h of sim.host.heights) { if (h > 0.1) onSlab++; else zero++; }
  return { cells, walkCells: walk, walkableCells: walkable, sunk, floating, bands, byOwner: withKind(vis, group(rows, id => id)), byCause, places, persons, hostCheck: { cells: sim.host.heights.length, hostOnSlab: onSlab, hostZero: zero } };
}

// ------------------------------------------------------------------ G-2 walk against what is drawn
export type WalkAudit = {
  freeCells: number; throughSolid: number; lowCeiling: number; brush: number; invisibleWall: number;
  through: Group[]; ceiling: Group[]; brushed: Group[]; invisible: Group[]; solidsCover: { id: string; area: number; covered: number; share: number; drawnAs: string }[];
  throughCells: { g: [number, number]; b: V; owner: string; h: number }[]; ceilingCells: { g: [number, number]; b: V; owner: string; h: number }[]; invisibleCells: { b: V; id: string; kind: string }[];
  buildingGaps: { id: string; area: number; open: number; share: number }[];
};

export function auditWalk(c: Ctx): WalkAudit {
  const { sim, vis } = c, g = sim.grid, R = vis.R, key = keyOf(vis);
  const throughCells: { g: [number, number]; b: V; owner: string; h: number }[] = [], ceilingCells: { g: [number, number]; b: V; owner: string; h: number }[] = [];
  const rows: { id: string; v: number; b: V; own?: string }[] = [], ceilRows: { id: string; v: number; b: V; own?: string }[] = [], brushRows: { id: string; v: number; b: V; own?: string }[] = [];
  let free = 0;
  const reach = Math.ceil(BODY_R / R.cell);
  for (let i = 0; i < g.nx; i++) for (let j = 0; j < g.ny; j++) {
    if (!g.free(i, j)) continue; free++;
    const b = g.centre(i, j), p = sim.frame.toPlan(b), [ci, cj] = R.cellOf(p); if (!R.inside(ci, cj)) continue;
    const col = R.column(ci, cj);
    if (!col.walkable) {
      if (col.rise > FLUSH) { throughCells.push({ g: [i, j], b, owner: col.owner, h: col.rise }); rows.push({ id: key(col.owner), own: col.owner, v: col.rise, b }); }
      else if (col.roof - col.top < HEAD) { ceilingCells.push({ g: [i, j], b, owner: col.roofOwner, h: col.roof - col.top }); ceilRows.push({ id: key(col.roofOwner), own: col.roofOwner, v: col.roof - col.top, b }); }
      continue;
    }
    // the body brushes a wall: a cell within the body radius that is solid to a walker
    let brush: { owner: string; h: number } | undefined;
    for (let a = ci - reach; a <= ci + reach && !brush; a++) for (let d = cj - reach; d <= cj + reach && !brush; d++) {
      if (!R.inside(a, d)) continue; const q = R.centre(a, d); if (Math.hypot(q[0] - p[0], q[1] - p[1]) > BODY_R) continue;
      const cc = R.column(a, d); if (!cc.walkable && cc.rise > STEP) brush = { owner: cc.owner, h: cc.rise };
    }
    if (brush) brushRows.push({ id: key(brush.owner), own: brush.owner, v: brush.h, b });
  }
  // blocked by a named solid (BODY from its edge): is anything drawn within BODY + a body of it?
  const invisibleCells: { b: V; id: string; kind: string }[] = [], invRows: { id: string; v: number; b: V }[] = [];
  const near = Math.ceil((sim.body + BODY_R) / R.cell);
  for (let i = 0; i < g.nx; i++) for (let j = 0; j < g.ny; j++) {
    if (g.free(i, j)) continue;
    const b = g.centre(i, j), why = sim.blockedBy(b); if (why.kind !== 'solid') continue;
    const p = sim.frame.toPlan(b), [ci, cj] = R.cellOf(p); let seen = false;
    for (let a = ci - near; a <= ci + near && !seen; a++) for (let d = cj - near; d <= cj + near && !seen; d++) {
      if (!R.inside(a, d)) continue; const q = R.centre(a, d); if (Math.hypot(q[0] - p[0], q[1] - p[1]) > sim.body + BODY_R) continue;
      const cc = R.column(a, d); if ((!cc.walkable && cc.rise > FLUSH) || R.inBody(a, d, cc.top + 0.1, cc.top + HEAD, 0.05)) seen = true;
    }
    if (!seen) { invisibleCells.push({ b, id: why.id, kind: why.kind }); invRows.push({ id: why.id, v: 0, b }); }
  }
  // every ground solid the sim keeps, against the drawn: the share of its footprint with something solid drawn over it
  const solidsCover = sim.solids.map(s => {
    const xs = s.poly.map(q => q[0]), ys = s.poly.map(q => q[1]); let n = 0, cov = 0; const kinds = new Map<string, number>();
    for (let x = Math.min(...xs) + 0.125; x < Math.max(...xs); x += R.cell) for (let y = Math.min(...ys) + 0.125; y < Math.max(...ys); y += R.cell) {
      if (!inPoly([x, y], s.poly)) continue; n++;
      const [ci, cj] = R.cellOf(sim.frame.toPlan([x, y])); if (!R.inside(ci, cj)) continue;
      const cc = R.column(ci, cj), own = (!cc.walkable && cc.rise > FLUSH) ? cc.owner : R.inBody(ci, cj, cc.top + 0.1, cc.top + HEAD, 0.05)?.owner;
      if (own) { cov++; kinds.set(own, (kinds.get(own) ?? 0) + 1); }
    }
    const drawn = [...kinds].sort((a, b) => b[1] - a[1]).slice(0, 2).map(k => k[0]).join(', ');
    return { id: s.id, area: r2(n * 0.0625), covered: r2(cov * 0.0625), share: n ? r2(cov / n) : 0, drawnAs: drawn };
  }).sort((a, b) => a.share - b.share);
  // building blockers (plots behind their building lines): is anything drawn at head height inside?
  const open = new Map<string, { n: number; open: number }>();
  for (let i = 0; i < g.nx; i++) for (let j = 0; j < g.ny; j++) {
    if (g.free(i, j)) continue; const b = g.centre(i, j), why = sim.blockedBy(b); if (why.kind !== 'building') continue;
    const [ci, cj] = R.cellOf(sim.frame.toPlan(b)); if (!R.inside(ci, cj)) continue;
    const e = open.get(why.id) ?? { n: 0, open: 0 }; e.n++; if (!R.solidRel(ci, cj, 1.0) && !R.solidRel(ci, cj, 1.6)) e.open++; open.set(why.id, e);
  }
  const buildingGaps = [...open].map(([id, e]) => ({ id, area: r2(e.n * 0.25), open: r2(e.open * 0.25), share: r2(e.open / e.n) })).sort((a, b) => b.open - a.open);
  return {
    freeCells: free, throughSolid: throughCells.length, lowCeiling: ceilingCells.length, brush: brushRows.length, invisibleWall: invisibleCells.length,
    through: withKind(vis, group(rows, id => id)), ceiling: withKind(vis, group(ceilRows, id => id)), brushed: withKind(vis, group(brushRows, id => id)), invisible: group(invRows, () => 'sim solid'),
    solidsCover, throughCells, ceilingCells, invisibleCells, buildingGaps,
  };
}

// ------------------------------------------------------------------ G-3 sight and fire against what is drawn
export type Wall = { id: string; z: number; h: number; perimeter: number; walled: number; share: number; gaps: { from: V; to: V; len: number }[]; area: number };
export type SightAudit = {
  eye: number; chest: number;
  walls: Wall[];
  drawnEye: Group[]; drawnChest: Group[]; eyeMissing: number; chestMissing: number;
  cells: { b: V; kind: 'eye' | 'chest' }[];
};

const distPoly = (p: V, poly: V[]) => { let d = Infinity; for (let i = 0; i < poly.length; i++) d = Math.min(d, segDist(p, poly[i]!, poly[(i + 1) % poly.length]!)); return d; };

export function auditSight(c: Ctx): SightAudit {
  const { sim, vis } = c, R = vis.R, key = keyOf(vis);
  const occAt = (z: number) => sim.occluders.filter(o => z >= o.z && z <= o.z + o.h);
  const eyeOcc = occAt(sim.eye), chestOcc = occAt(CHEST);
  const polyArea = (p: V[]) => Math.abs(p.reduce((s, q, i) => s + (q[0] * p[(i + 1) % p.length]![1] - p[(i + 1) % p.length]![0] * q[1]), 0)) / 2;
  const bbx = (o: { poly: V[] }) => ({ x0: Math.min(...o.poly.map(p => p[0])), x1: Math.max(...o.poly.map(p => p[0])), y0: Math.min(...o.poly.map(p => p[1])), y1: Math.max(...o.poly.map(p => p[1])) });
  const eyeBox = eyeOcc.map(o => ({ o, b: bbx(o) })), chestBox = chestOcc.map(o => ({ o, b: bbx(o) }));
  const inBoxed = (list: typeof eyeBox, b: V) => list.some(({ o, b: bb }) => b[0] >= bb.x0 && b[0] <= bb.x1 && b[1] >= bb.y0 && b[1] <= bb.y1 && inPoly(b, o.poly));
  const nearBoxed = (list: typeof eyeBox, b: V, d: number) => list.some(({ o, b: bb }) => b[0] >= bb.x0 - d && b[0] <= bb.x1 + d && b[1] >= bb.y0 - d && b[1] <= bb.y1 + d && (inPoly(b, o.poly) || distPoly(b, o.poly) <= d));
  const cells: { b: V; kind: 'eye' | 'chest' }[] = [], eyeRows: { id: string; v: number; b: V; own?: string }[] = [], chestRows: { id: string; v: number; b: V; own?: string }[] = [];
  const win = sim.window, ground = (own: string) => { const m = vis.meta.get(own); return !m || m.layer === 'ground'; };
  const FRINGE = 0.6; // a drawn thing this near an occluder's edge is the same wall, a few centimetres out
  for (let i = 0; i < R.nx; i++) for (let j = 0; j < R.ny; j++) {
    const p = R.centre(i, j), b = sim.frame.toBlock(p);
    if (b[0] < win.x0 || b[0] > win.x1 || b[1] < win.y0 || b[1] > win.y1) continue;
    const eyeS = R.solidRel(i, j, sim.eye), chestS = R.solidRel(i, j, CHEST);
    if (eyeS && !ground(eyeS) && !inBoxed(eyeBox, b) && !nearBoxed(eyeBox, b, FRINGE)) { eyeRows.push({ id: key(eyeS), own: eyeS, v: 0, b }); cells.push({ b, kind: 'eye' }); }
    if (chestS && !ground(chestS) && !inBoxed(chestBox, b) && !nearBoxed(chestBox, b, FRINGE)) { chestRows.push({ id: key(chestS), own: chestS, v: 0, b }); cells.push({ b, kind: 'chest' }); }
  }
  // each occluder that spans the eye: how much of its outline has something drawn at eye height within a body of it (a line that crosses the outline is stopped there)
  const walls: Wall[] = sim.occluders.filter(o => o.z <= sim.eye && o.z + o.h >= sim.eye).map(o => {
    let per = 0, wal = 0; const gaps: { from: V; to: V; len: number }[] = []; let run: { from: V; to: V; len: number } | undefined;
    for (let k = 0; k < o.poly.length; k++) {
      const a = o.poly[k]!, e = o.poly[(k + 1) % o.poly.length]!, L = Math.hypot(e[0] - a[0], e[1] - a[1]), n = Math.max(1, Math.ceil(L / 0.25));
      for (let m = 0; m < n; m++) {
        const t = (m + 0.5) / n, q: V = [a[0] + (e[0] - a[0]) * t, a[1] + (e[1] - a[1]) * t], step = L / n;
        if (q[0] < win.x0 + 0.5 || q[0] > win.x1 - 0.5 || q[1] < win.y0 + 0.5 || q[1] > win.y1 - 0.5) { if (run) { gaps.push(run); run = undefined; } continue; } // outside the window nothing is drawn here
        per += step;
        let hit = false; const pp = sim.frame.toPlan(q);
        for (let di = -2; di <= 2 && !hit; di++) for (let dj = -2; dj <= 2 && !hit; dj++) { const [ci, cj] = R.cellOf([pp[0] + di * R.cell, pp[1] + dj * R.cell]); if (R.inside(ci, cj) && R.solidRel(ci, cj, sim.eye) && !ground(R.solidRel(ci, cj, sim.eye)!)) hit = true; }
        if (hit) { wal += step; if (run) { gaps.push(run); run = undefined; } } else if (run) { run.to = q; run.len += step; } else run = { from: q, to: q, len: step };
      }
    }
    if (run) gaps.push(run);
    return { id: o.id, z: o.z, h: o.h, perimeter: r2(per), walled: r2(wal), share: per ? r2(wal / per) : 0, gaps: gaps.filter(g => g.len >= 1).sort((a, b) => b.len - a.len).slice(0, 6).map(g => ({ from: [r2(g.from[0]), r2(g.from[1])] as V, to: [r2(g.to[0]), r2(g.to[1])] as V, len: r2(g.len) })), area: r2(polyArea(o.poly)) };
  });
  return { eye: sim.eye, chest: CHEST, walls, drawnEye: withKind(vis, group(eyeRows, id => id)), drawnChest: withKind(vis, group(chestRows, id => id)), eyeMissing: eyeRows.length, chestMissing: chestRows.length, cells };
}

// ------------------------------------------------------------------ routes: the people's paths over the grid, to count who crosses what
export type Route = { id: string; who: string; from: string; to: string; cells: number; length: number; sunk: number; sunkMax: number; through: number; ceiling: number; throughOwners: string[] };
export function routes(c: Ctx, gr: GroundAudit, wk: WalkAudit): { list: Route[]; pathCells: Map<string, number>; paths: Map<string, [number, number][]> } {
  const sunkAt = new Map(gr.cells.map(x => [`${x.g[0]},${x.g[1]}`, x.delta])), thr = new Map(wk.throughCells.map(x => [`${x.g[0]},${x.g[1]}`, x.owner])), ceil = new Map(wk.ceilingCells.map(x => [`${x.g[0]},${x.g[1]}`, x.owner]));
  const { sim } = c, g = sim.grid, at = (id: string): V | undefined => sim.places.find(p => p.id === id)?.at;
  const specs: [string, string, string, string][] = [];
  const add = (who: string, a: string, b: string, id = `${who}: ${a} to ${b}`) => specs.push([who, a, b, id]);
  add('guard', 'd:Office entrance', 'd:Café door'); add('guard', 'd:Café door', 'd:Office entrance');
  add('patrol', 'patrol-a', 'patrol-b'); add('patrol', 'patrol-a', 'patrol-reroute');
  add('councillor', 'd:Official entrance', 'd:Official drop-off'); add('councillor', 'd:Official entrance', 'd:Service lane');
  add('driver', 'd:Meet driver', 'd:Official drop-off'); add('driver', 'd:Meet driver', 'd:Service lane');
  for (const cr of ['fixer', 'tech', 'scout', 'enforcer']) for (const dest of ['d:Office entrance', 'd:Café door', 'cabinet', 'd:Meet Vale', 'd:Getaway bay', 'd:Meet driver', 'd:Under parasol']) add(`crew-${cr}`, `crew:${cr}`, dest);
  add('rivals', 'rival-entry', 'd:Office approach'); add('security', 'security-entry', 'd:Office approach'); add('security', 'd:Office approach', 'd:Service lane');
  for (const p of sim.places) if (/^cafe-table-\d-seat-0$/.test(p.id)) add('waitress', 'd:Café door', p.id);
  for (const p of sim.places) if (/^ambush-|^police-/.test(p.id)) add('final fight', p.id, 'd:Office approach');
  const out: Route[] = [], pathCells = new Map<string, number>(), paths = new Map<string, [number, number][]>();
  const nearestFree = (b: V): [number, number] | undefined => {
    const [ci, cj] = g.cellOf(b); for (let r = 0; r < 12; r++) for (let i = ci - r; i <= ci + r; i++) for (let j = cj - r; j <= cj + r; j++) if (g.free(i, j)) return [i, j]; return undefined;
  };
  for (const [who, a, b, id] of specs) {
    const pa = at(a), pb = at(b); if (!pa || !pb) continue;
    const sa = nearestFree(pa), sb = nearestFree(pb); if (!sa || !sb) continue;
    const path = astar(g, sa, sb); if (!path) continue;
    let sunk = 0, sunkMax = 0, through = 0, ceiling = 0; const owners = new Set<string>();
    for (const [i, j] of path) { const k = `${i},${j}`, d = sunkAt.get(k); if (d !== undefined && Math.abs(d) > TOL) { sunk++; if (Math.abs(d) > Math.abs(sunkMax)) sunkMax = d; } if (thr.has(k)) { through++; owners.add(thr.get(k)!); } if (ceil.has(k)) ceiling++; }
    paths.set(id, path);
    out.push({ id, who, from: a, to: b, cells: path.length, length: r2(path.length * g.cell), sunk, sunkMax: r2(sunkMax), through, ceiling, throughOwners: [...owners].slice(0, 4) });
    for (const [i, j] of path) { const k = `${i},${j}`; pathCells.set(k, (pathCells.get(k) ?? 0) + 1); }
  }
  return { list: out, pathCells, paths };
}

function astar(g: Sim['grid'], s: [number, number], t: [number, number]): [number, number][] | undefined {
  const N = g.nx * g.ny, G = new Float64Array(N).fill(Infinity), prev = new Int32Array(N).fill(-1), done = new Uint8Array(N);
  const key = (i: number, j: number) => i * g.ny + j, h = (i: number, j: number) => Math.hypot(i - t[0], j - t[1]);
  const heap: [number, number][] = [];
  const push = (f: number, k: number) => { heap.push([f, k]); let i = heap.length - 1; while (i > 0) { const p = (i - 1) >> 1; if (heap[p]![0] <= heap[i]![0]) break; [heap[p], heap[i]] = [heap[i]!, heap[p]!]; i = p; } };
  const pop = () => { const top = heap[0]!; const last = heap.pop()!; if (heap.length) { heap[0] = last; let i = 0; for (;;) { const l = 2 * i + 1, r = l + 1; let m = i; if (l < heap.length && heap[l]![0] < heap[m]![0]) m = l; if (r < heap.length && heap[r]![0] < heap[m]![0]) m = r; if (m === i) break; [heap[m], heap[i]] = [heap[i]!, heap[m]!]; i = m; } } return top; };
  const ks = key(s[0], s[1]), kt = key(t[0], t[1]); G[ks] = 0; push(h(s[0], s[1]), ks);
  const D = [[1, 0], [-1, 0], [0, 1], [0, -1], [1, 1], [1, -1], [-1, 1], [-1, -1]] as const;
  while (heap.length) {
    const [, k] = pop(); if (done[k]) continue; done[k] = 1; if (k === kt) break;
    const i = Math.floor(k / g.ny), j = k % g.ny;
    for (const [di, dj] of D) {
      const ni = i + di, nj = j + dj; if (!g.free(ni, nj)) continue;
      if (di !== 0 && dj !== 0 && (!g.free(i + di, j) || !g.free(i, j + dj))) continue;
      const nk = key(ni, nj), ng = G[k]! + (di !== 0 && dj !== 0 ? 1.414 : 1);
      if (ng < G[nk]!) { G[nk] = ng; prev[nk] = k; push(ng + h(ni, nj), nk); }
    }
  }
  if (!Number.isFinite(G[kt]!)) return undefined;
  const path: [number, number][] = []; for (let k = kt; k !== -1; k = prev[k]!) path.push([Math.floor(k / g.ny), k % g.ny]);
  return path.reverse();
}

export type { Raster };
export { CELL };

// ------------------------------------------------------------------ legs: what is walked against what is drawn (Sky 2026-10-24, playing 0.2.99: "Inez seems to be walking on an invisible ledge sometimes"; "the robot patrol is able to go through the camera pole")
/** m: a leg standing more than this above the pavement is on a ledge; a kerb stone (0.19) or a stair tread is a step and fine. */
export const LEDGE = 0.25;
/** One stretch of one leg that fails the leg rule (G-1d): standing on a ledge (a walkable floor more than LEDGE over the pavement), inside a drawn solid a walker cannot climb, under less than 1.8 m of ceiling, or brushing a drawn solid or a camera pole nearer than the body
 *  (standards.walking.body.r, 0.65 m: the engine blocks the cells within it of every drawn solid, so a leg laid without the drawn solids walks through what the grid would refuse). `rise` is the height over the pavement; for a brush, `clear` is how near the body's edge came. */
export type LegStretch = { who: string; leg: string; from: V; to: V; length: number; cause: 'ledge' | 'solid' | 'ceiling' | 'brush'; owner: string; rise: number; clear?: number; samples: number };
export type LegAudit = { legs: number; metres: number; body: number; stretches: LegStretch[] };

/** Every leg the people walk: the rounds' waypoint legs (sim/data/handover.rounds.json: the waitress to her tables), the mission's fixed legs (the patrol's sweep and where the rerouted patrol waits: tools/missions/handover/legs.ts, Pulse), the door guard's stops, and the
 *  cast routes (the grid's own A* paths between the site's places, as G-1b walks them, for the floor). Each is sampled every 0.25 m. The floor is read from the raster the walk file is made from; the clearance from the drawn solids the engine blocks cells with (tools/sim/solids.ts)
 *  and the mission's camera poles, which the world does not place (the host stands them: legs.ts cameraPoles). */
export function auditLegs(c: Ctx, world: World, route?: { list: Route[]; paths: Map<string, [number, number][]> }): LegAudit {
  const { sim, vis } = c, R = vis.R, key = keyOf(vis), body = sim.body, TOL_C = 0.01;
  const rounds = JSON.parse(readFileSync(process.env.GEOMETRY_ROUNDS ?? join(ROOT, 'sim/data/handover.rounds.json'), 'utf8')) as { servers: Record<string, { name: string; spans: { excursions?: { table: string; path: number[][] }[] }[] }>; guard?: { stops: { at: number[] }[] } };
  const mission = JSON.parse(readFileSync(join(ROOT, 'contracts/missions/handover/mission.json'), 'utf8')), collide = JSON.parse(readFileSync(join(ROOT, 'contracts/world/kit.collide.json'), 'utf8'));
  const legs: { who: string; id: string; pts: V[]; fixed?: boolean }[] = [], seen = new Set<string>();
  for (const [pid, v] of Object.entries(rounds.servers)) for (const sp of v.spans) for (const e of sp.excursions ?? []) {
    const k = `${pid}|${e.table}|${JSON.stringify(e.path)}`; if (seen.has(k)) continue; seen.add(k);
    legs.push({ who: `${v.name} (${pid})`, id: `to ${e.table}`, pts: e.path.map(q => [q[0]! / 100, q[1]! / 100] as V) });
  }
  // where the engine stands a place: Site.Stand, the site's `stand` of the place when it has one, else the point itself (the engine refuses a point on blocked ground; it does not move it to a cell centre)
  const sitePlaces = (JSON.parse(readFileSync(join(ROOT, 'sim/data/handover.site.json'), 'utf8')) as { places: Record<string, { at: number[]; stand?: number[] }> }).places, standOf = new Map<string, V>();
  for (const pl of Object.values(sitePlaces)) standOf.set(`${pl.at[0]},${pl.at[1]}`, [(pl.stand ?? pl.at)[0]! / 100, (pl.stand ?? pl.at)[1]! / 100]);
  const stand = (p: V): V => standOf.get(`${Math.round(p[0] * 100)},${Math.round(p[1] * 100)}`) ?? p;
  for (const f of fixedLegs(mission, stand)) legs.push({ who: 'patrol', id: f.id, pts: f.a[0] === f.b[0] && f.a[1] === f.b[1] ? [f.a, [f.a[0], f.a[1] + 0.01]] : [f.a, f.b], fixed: true });
  for (const [k, g] of (rounds.guard?.stops ?? []).entries()) legs.push({ who: 'door guard', id: `round stop ${k}`, pts: [[g.at[0]! / 100, g.at[1]! / 100], [g.at[0]! / 100, g.at[1]! / 100 + 0.01]] });
  for (const r of route?.list ?? []) { const path = route!.paths.get(r.id); if (path) legs.push({ who: r.who, id: `${r.from} to ${r.to}`, pts: path.map(([i, j]) => sim.grid.centre(i, j)) }); }
  const poles = cameraPoles(mission, collide), drawn = drawnSolids(sim.plan, sim.civ, world);
  const distTo = (s: Solid, q: V) => { if (s.circle) return Math.max(0, Math.hypot(q[0] - s.circle.c[0], q[1] - s.circle.c[1]) - s.circle.r); if (inPoly(q, s.poly)) return 0; let d = Infinity; for (let k = 0; k < s.poly.length; k++) d = Math.min(d, segDist(q, s.poly[k]!, s.poly[(k + 1) % s.poly.length]!)); return d; };
  const out: LegStretch[] = []; let metres = 0;
  for (const leg of legs) {
    let run: { cause: LegStretch['cause']; owner: string; from: V; to: V; len: number; rise: number; clear?: number; n: number } | undefined;
    const flush = () => { if (run) out.push({ who: leg.who, leg: leg.id, from: [r2(run.from[0]), r2(run.from[1])], to: [r2(run.to[0]), r2(run.to[1])], length: r2(run.len), cause: run.cause, owner: run.owner, rise: r2(run.rise), ...(run.clear !== undefined ? { clear: r2(run.clear) } : {}), samples: run.n }); run = undefined; };
    for (let k = 0; k + 1 < leg.pts.length; k++) {
      const a = leg.pts[k]!, b = leg.pts[k + 1]!, L = Math.hypot(b[0] - a[0], b[1] - a[1]); if (L < 0.005) continue;
      const n = Math.max(1, Math.ceil(L / 0.25)); metres += L;
      for (let m = 0; m <= n; m++) {
        const t = m / n, q: V = [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t], [ci, cj] = R.cellOf(sim.frame.toPlan([q[0] + LEG_NUDGE, q[1] + LEG_NUDGE])); if (!R.inside(ci, cj)) { flush(); continue; } // the cell under the point, read 1 mm south-east: a route point exactly on a raster cell edge (the café door's walk to its tables, x -10.25) is not in the wall's cell (brief 028, Vantage 2026-10-24: the engine's collide boxes leave it free)
        const col = R.column(ci, cj), rise = col.top - col.ground, floor = (col.walkable ? col.top : col.ground) - 0.18, declared = !!floorAt(sim.civ, sim.frame.toPlan(q)).on; // block z: the pavement is 0; a floor the civic block declares (the Office landing and its steps) is a stair, not a ledge
        let cause: LegStretch['cause'] | undefined = !col.walkable && rise > FLUSH ? 'solid' : col.walkable && rise > LEDGE && !declared ? 'ledge' : col.walkable && col.roof - col.top < HEAD ? 'ceiling' : undefined, owner = key(col.owner), clear: number | undefined;
        if (!cause) { // the body's clearance: a drawn solid that blocks a walker at this floor (above a step, under head room), a building's face by the point itself, or a camera pole
          for (const s of drawn.solids) {
            if (!(s.z0 < floor + HEAD && s.z1 > floor + STEP)) continue;
            if (s.stand && Math.abs(s.z1 - floor) <= 0.05) continue;
            if (q[0] < s.x0 - body - 0.1 || q[0] > s.x1 + body + 0.1 || q[1] < s.y0 - body - 0.1 || q[1] > s.y1 + body + 0.1) continue;
            const d = distTo(s, q), need = s.face ? 0 : body; if (d < need - TOL_C && (clear === undefined || d < clear)) { clear = d; cause = 'brush'; owner = s.piece ?? s.id; }
          }
          for (const p of poles) { const d = Math.max(0, Math.hypot(q[0] - p.c[0], q[1] - p.c[1]) - p.r); if (d < body - TOL_C && (clear === undefined || d < clear)) { clear = d; cause = 'brush'; owner = p.id; } }
        }
        if (!cause) { flush(); continue; }
        if (run && run.cause === cause && (cause !== 'brush' || run.owner === owner)) { run.to = q; run.len += 0.25; run.n++; run.rise = Math.max(run.rise, rise); if (clear !== undefined) run.clear = Math.min(run.clear ?? clear, clear); } else { flush(); run = { cause, owner, from: q, to: q, len: 0.25, rise, ...(clear !== undefined ? { clear } : {}), n: 1 }; }
      }
    }
    flush();
  }
  return { legs: legs.length, metres: r2(metres), body, stretches: out };
}
