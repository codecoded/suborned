// The capstone programme (brief 026, Sky 2026-10-20: the old DecoKit city is the floor, only move up): the Handover block and the streets that approach it get the old kit's ground, its robots,
// vending machines, booths, boards and planting, placed BY RULE over plan data (plot edges, road kerbs, frontage trades, crossings, trees), sited where a real city puts them.
// A rule that finds no place for its piece does not force one: the miss is logged (CapLog.gaps) and goes to Sky. The rules are one table (capstone-rules.ts) read by the generator here and by
// the world rules CAP-1..CAP-5 (tools/world/rules/capstone.ts). Deterministic: same plan, same block, same kit, same items; every choice is seeded by an id.
//
// Ground (flat, no street-layer tests; z in capstone-rules LAYER): kerb stones on every kerbed plot edge, flag paving in a grid anchored to each plot, a pit under every pavement tree, drain
// gratings in the gutter, manholes down the lanes and the walks, tactile paving at each crossing landing, terrazzo aprons at lobby and bureau doors.
// Standing pieces (through Layer.fits, the tests every street item passes): robots by job, vending pairs and the Automat on frontage walls by trade, bike racks, info lightboxes, boards, baskets,
// lockers, planting at doors, drinking fountains, ring benches. Civic-block robots are placed after the civic masses exist (capstone-civic.ts).
import type { KitAsset } from '../../../../contracts/world/kit.schema';
import type { Crossing, Frontage, KitRef, StreetItem } from '../../../../contracts/world/world.schema';
import { toPlanFn } from '../../../plan/civic';
import { type V, dist, inPoly, lineDist, segDist } from '../../../plan/geom';
import { WINDOW } from '../../../plan/walk-grid';
import { boxRing, ringGap, roadAt, samples9, walkBlock } from '../../../plan/street-zones';
import { handoverPoints } from '../../civic/handover';
import { type Corner, junctionCorners } from '../../civic/siting';
import { type Ctx, r3, r4, rng } from '../common';
import { plotRing, kindOf, idOf } from '../edges';
import { areaOf, bbox, edgeN, pointInRing } from '../geo';
import { adoptedBy, forUse } from '../kitrefs';
import { overlaps } from './crossings';
import { lawn } from './ground';
import type { Layer } from './kit';
import { FLAT_SET as FLAT_KINDS_SET, LAYER, MARGIN, PAVE, RULES, type Rect4, inRect4 } from './capstone-rules';

export type CapGap = { rule: string; where: string; why: string };
export type CapLog = { zone: Rect4; counts: Record<string, number>; pieces: Record<string, number>; gaps: CapGap[]; kerb: { edgeM: number; stoneM: number; droppedM: number } };

const norm2 = (a: V): V => { const l = Math.hypot(a[0], a[1]) || 1; return [a[0] / l, a[1] / l]; };
const perp = (a: V): V => [-a[1], a[0]];
const ang = (a: V) => Math.atan2(a[1], a[0]);
/** The kit yaw whose front (+Z, yaw + 90°) faces direction `d`. */
export const facing = (d: V): number => { const y = ang(d) - Math.PI / 2; return r4(Math.atan2(Math.sin(y), Math.cos(y))); };
const PLAYSORT = (a: KitAsset, b: KitAsset) => a.tris - b.tris;
/** The lightest adopted piece of a robot role (a `_Play` copy before the 15-20k-triangle model): the cast pieces carry `robot:<role>`. */
export const robotPiece = (role: string): KitAsset | undefined => forUse(`robot:${role}`).slice().sort(PLAYSORT)[0];

/** The capstone zone in the plan frame: the walk window (block frame) grown by MARGIN m. The block frame is a quarter turn of the plan's, so the grown window is a plan rectangle. */
export function zoneOf(ctx: Ctx): Rect4 {
  const toPlan = toPlanFn(ctx.plan.civic), w = WINDOW, m = MARGIN;
  const c = ([[w.x0 - m, w.y0 - m], [w.x1 + m, w.y0 - m], [w.x1 + m, w.y1 + m], [w.x0 - m, w.y1 + m]] as V[]).map(toPlan);
  const b = bbox(c);
  return [r3(b[0]), r3(b[1]), r3(b[2]), r3(b[3])];
}

type Run = { a: V; b: V; len: number; u: V; n: V; road: string; cls: string; plot: string; use: string; trimA: boolean; trimB: boolean };

/** The kerbed runs of the plots in the zone: each maximal collinear stretch of plot edges facing one road (edges.ts plotRing classes), with its outward (road-ward) normal. */
export function kerbRuns(ctx: Ctx, zone: Rect4): Run[] {
  const out: Run[] = [];
  for (const pl of ctx.d.plots) {
    const bb = bbox(pl.polyV); if (bb[1] < zone[0] || bb[0] > zone[1] || bb[3] < zone[2] || bb[2] > zone[3]) continue;
    const r = plotRing(ctx, pl), n = r.p.length, kerbed = (c: string) => ['primary', 'district', 'access'].includes(kindOf(c));
    const edges = r.p.map((a, i) => ({ a, b: r.p[(i + 1) % n]!, c: r.c[i]! }));
    // merge consecutive collinear edges of one class into runs
    const runs: { a: V; b: V; c: string; i0: number; i1: number }[] = [];
    edges.forEach((e, i) => {
      if (!kerbed(e.c)) return;
      const last = runs[runs.length - 1], u = norm2([e.b[0] - e.a[0], e.b[1] - e.a[1]]);
      if (last && last.i1 === i - 1 && last.c === e.c) { const lu = norm2([last.b[0] - last.a[0], last.b[1] - last.a[1]]); if (Math.abs(lu[0] * u[1] - lu[1] * u[0]) < 1e-3) { last.b = e.b; last.i1 = i; return; } }
      runs.push({ a: e.a, b: e.b, c: e.c, i0: i, i1: i });
    });
    // the run that wraps past the ring's start joins its predecessor
    const first = runs[0], last = runs[runs.length - 1];
    if (runs.length > 1 && first && last && first.i0 === 0 && last.i1 === n - 1 && first.c === last.c) {
      const lu = norm2([last.b[0] - last.a[0], last.b[1] - last.a[1]]), fu = norm2([first.b[0] - first.a[0], first.b[1] - first.a[1]]);
      if (Math.abs(lu[0] * fu[1] - lu[1] * fu[0]) < 1e-3) { last.b = first.b; last.i1 = first.i1; runs.shift(); }
    }
    for (const q of runs) {
      const len = dist(q.a, q.b), u = norm2([q.b[0] - q.a[0], q.b[1] - q.a[1]]), nn = edgeN(q.a, q.b);
      const prev = edges[(q.i0 + n - 1) % n]!, next = edges[(q.i1 + 1) % n]!;
      const turn = (e: { a: V; b: V }) => { const v = norm2([e.b[0] - e.a[0], e.b[1] - e.a[1]]); return Math.abs(u[0] * v[0] + u[1] * v[1]) < 0.1; }; // a right-angled neighbour
      out.push({ a: q.a, b: q.b, len, u, n: nn, road: idOf(q.c), cls: kindOf(q.c), plot: pl.id, use: pl.use, trimA: kerbed(prev.c) && turn(prev), trimB: kerbed(next.c) && turn(next) });
    }
  }
  return out;
}

type Ctx2 = { ctx: Ctx; L: Layer; zone: Rect4; log: CapLog; items: StreetItem[]; n: Record<string, number>; points: { name: string; p: V }[]; tiles: V[][]; strips: V[][]; drives: V[][]; civicPlots: V[][]; flat: StreetItem[] };

const inZone = (c: Ctx2, p: V) => inRect4(p, c.zone);
const gap = (c: Ctx2, rule: string, where: string, why: string) => { c.log.gaps.push({ rule, where, why }); };
const bump = (c: Ctx2, kind: string, id: string) => { c.log.counts[kind] = (c.log.counts[kind] ?? 0) + 1; c.log.pieces[id] = (c.log.pieces[id] ?? 0) + 1; };
const nextId = (c: Ctx2, kind: string) => `street/capstone/${kind}-${(c.n[kind] = (c.n[kind] ?? 0) + 1) - 1}`;
const clearOfPoints = (c: Ctx2, ring: V[], m = RULES.pointClear) => !c.points.some(h => (inPoly(h.p, ring) ? 0 : Math.min(...ring.map((q, i) => segDist(h.p, q, ring[(i + 1) % ring.length]!)))) < m - 0.011);
/** True if p stands on a plot the block keeps as built (the civic square and office plots). */
const onCivic = (c: Ctx2, p: V) => c.civicPlots.some(r => pointInRing(p, r));

/** The pivot (what Unity places) for a footprint centre `at` of a piece turned to `yaw`: pivot + R(yaw)·pivotOffset = footprint centre. */
export function pivotFor(a: KitAsset, at: V, yaw: number, scale: [number, number, number] = [1, 1, 1]): V {
  const off = a.pivotOffset; if (!off) return at;
  const ox = off[0] * scale[0], oz = off[1] * scale[2], cs = Math.cos(yaw), sn = Math.sin(yaw);
  return [r3(at[0] - (ox * cs - oz * sn)), r3(at[1] - (ox * sn + oz * cs))];
}

/** Push one flat ground piece (never registered with the street layer: nothing is kept clear of paving). */
function flatPiece(c: Ctx2, kind: string, a: KitAsset, at: V, yaw: number, z: number, scale: [number, number, number], edge: string, ring?: V[]): StreetItem {
  const piv = pivotFor(a, at, yaw, scale), size: [number, number, number] = [r3(a.size[0] * scale[0]), r3(a.size[2] * scale[2]), r3(a.size[1] * scale[1])];
  const it: StreetItem = { id: nextId(c, kind), kind, at: piv, yaw: r4(yaw), z: r3(z), size, surface: 'trim', edge, sight: 'open', source: 'kit', kit: { id: a.id, scale } as KitRef };
  void ring; c.items.push(it); c.flat.push(it); bump(c, kind, a.id);
  return it;
}

/** A standing piece through Layer.fits and Layer.add: `at` the footprint centre, the pivot derived. Undefined if it does not fit (the caller logs the miss). */
function standing(c: Ctx2, kind: string, a: KitAsset | undefined, at: V, yaw: number, z: number, edge: string, o: { variant?: string; blocks?: boolean; r?: number; scale?: [number, number, number]; check?: boolean; anyZone?: boolean; force?: boolean } = {}): StreetItem | undefined {
  if (!a) return undefined;
  const scale = o.scale ?? [1, 1, 1], size: [number, number, number] = [r3(a.size[0] * scale[0]), r3(a.size[2] * scale[2]), r3(a.size[1] * scale[1])], ring = boxRing(at, size[0], size[1], yaw);
  if ((!o.anyZone && !inZone(c, at)) || onCivic(c, at) || !clearOfPoints(c, ring)) return undefined;
  const r = o.r ?? Math.max(size[0], size[1]) / 2 * 0.92, opts = { size, variant: o.variant, r, blocks: o.blocks ?? false, force: o.force }; // force: a tram platform's own ground (the stop kit's items are forced too): the walking zone and the tram envelope are its business, so free() is asked here instead
  if (o.force && !c.L.free(at, r, false)) return undefined;
  const piv = pivotFor(a, at, yaw, scale); // the export carries the pivot: the street rules (SF-1, SF-2, SF-8) read a footprint about it, so the spot must pass about the footprint centre and about the pivot
  if (o.check !== false && (!c.L.fits(kind, at, yaw, z, edge, opts) || (dist(piv, at) > 0.01 && !c.L.fits(kind, piv, yaw, z, edge, { ...opts, r: 0 })))) return undefined;
  const clash = (x: StreetItem) => x.z < 1 && !x.crossing && !['crossing', 'bollard', 'beacon', 'tree', 'signal-orb'].includes(x.kind) && !FLAT_KINDS_SET.has(x.kind) && dist(x.at, at) < 6 && ringGap(boxRing(x.at, x.size[0], x.size[1], x.yaw), ring) < 0.005;
  if (c.L.items.some(clash) || (dist(piv, at) > 0.01 && false)) return undefined; // no shared footprint with anything already standing (CAP-7)
  const id = nextId(c, kind);
  if (!c.L.add(kind, at, yaw, z, edge, { ...opts, id, uncounted: true, kit: { id: a.id, scale }, pivot: pivotFor(a, at, yaw, scale) })) return undefined;
  const it = c.L.items[c.L.items.length - 1]!; c.items.push(it); bump(c, kind, a.id);
  return it;
}

// ---- tenancies: the street-level frontages in the zone, their wall frame
type Wall = { f: Frontage; u: V; n: V; /** m along the wall of the door's centre */ sd: number; at: (s: number, out: number) => V; zoneOK: boolean };
const WALL_DEPTH = (f: Frontage) => f.front.size[1];
function wallOf(c: Ctx2, f: Frontage): Wall {
  const u = norm2([f.b[0] - f.a[0], f.b[1] - f.a[1]]), n: V = [f.normal[0], f.normal[1]], e = f.entrance.c as V;
  const sd = (e[0] - f.a[0]) * u[0] + (e[1] - f.a[1]) * u[1], d0 = WALL_DEPTH(f);
  const at = (s: number, out: number): V => [f.a[0] + u[0] * s + n[0] * (d0 + out), f.a[1] + u[1] * s + n[1] * (d0 + out)];
  return { f, u, n, sd, at, zoneOK: inZone(c, at(f.w / 2, 0)) };
}
const tenancies = (c: Ctx2, o: Frontage[]): Wall[] => o.filter(f => f.level === 'street' && !f.service && f.w >= 3).map(f => wallOf(c, f)).filter(w => w.zoneOK);

/** The s values (m along the wall) tried for a piece `half` wide that must stand clear of the door by the street rules: from the tenancy's two ends inward, then around. Both sides of the door, nearer-the-end first. */
function scan(w: Wall, half: number, step = 0.25): number[] {
  const out: number[] = [];
  for (let s = half + 0.1; s <= w.f.w - half - 0.1 + 1e-9; s += step) out.push(r3(s));
  return out;
}

// ============================================================ G1 kerbs
function kerbs(c: Ctx2, runs: Run[]): void {
  const R = RULES.kerb, stones = R.pieces.map(adoptedBy);
  if (stones.some(s => !s)) { gap(c, 'G1', 'kerb', 'Kerb_Granite_* is a candidate: the kerb and gutter are DK_Ground_Kerb / DK_Ground_Gutter on the road ribbon (Vela round 1, 4096 sheets not adopted)'); return; }
  const ground = stones[0]!, scaleY = r3((PAVE + LAYER.kerb) / ground.size[1]);
  for (const r of runs) {
    if (!inZone(c, [(r.a[0] + r.b[0]) / 2, (r.a[1] + r.b[1]) / 2])) continue;
    const road = c.ctx.d.roads.find(x => x.id === r.road); if (!road) continue;
    const t0 = r.trimA && Math.abs(r.u[0]) < Math.abs(r.u[1]) ? R.halfDepth : 0, t1 = r.trimB && Math.abs(r.u[0]) < Math.abs(r.u[1]) ? R.halfDepth : 0; // x-dominant runs run through the corner, y-dominant stop at the other's face
    const eff = r.len - t0 - t1; if (eff < R.minEdge) continue;
    const m = Math.max(1, Math.round(eff / R.module)), sx = r3(eff / m / ground.size[0]), yaw = facing(r.n);
    for (let k = 0; k < m; k++) {
      const s = t0 + (k + 0.5) * (eff / m), at: V = [r.a[0] + r.u[0] * s - r.n[0] * R.inset, r.a[1] + r.u[1] * s - r.n[1] * R.inset], len = eff / m;
      if (!inZone(c, at) || Math.abs(lineDist(at, road.line) - road.w / 2 - R.inset) > 0.2) continue; // in the zone, and on the road's kerb face (the plot edge runs on past a road that ends)
      const ring = boxRing(at, Math.max(0.1, len - 0.1), R.halfDepth * 2, ang(r.u));
      if (c.strips.some(p => overlaps(ring, p)) || c.drives.some(p => overlaps(ring, p))) { c.log.kerb.droppedM += len; continue; } // the kerb drops at a crossing and a drive
      const a = stones[k & 1]!;
      flatPiece(c, 'kerb', a, at, yaw + 0, 0, [sx, scaleY, 1], `cap/kerb/${r.road}`);
      c.log.kerb.stoneM += len;
    }
  }
}

// ============================================================ G2 flags and G3 pits
function flags(c: Ctx2): void {
  const R = RULES.flags, pieces = R.pieces.map(adoptedBy);
  if (pieces.some(p => !p)) { gap(c, 'G2', 'flags', 'Pave_Flags_* is a candidate: the pavement is DK_Ground_Pavement in world-space UV on the plot slab (Vela round 1)'); return; }
  const solids = c.L.solids, boxes = solids.map(s => bbox(s));
  const under = (p: V) => solids.some((s, i) => p[0] >= boxes[i]![0] && p[0] <= boxes[i]![1] && p[1] >= boxes[i]![2] && p[1] <= boxes[i]![3] && pointInRing(p, s));
  for (const pl of c.ctx.d.plots) {
    if (R.skipUse.includes(pl.use) || pl.civic) continue;
    const b = bbox(pl.polyV); if (b[1] < c.zone[0] || b[0] > c.zone[1] || b[3] < c.zone[2] || b[2] > c.zone[3]) continue;
    const rect = Math.abs(areaOf(pl.polyV)) - (b[1] - b[0]) * (b[3] - b[2]);
    if (Math.abs(rect) > 0.5) { gap(c, 'G2', pl.id, 'the plot is not a rectangle: no flag grid'); continue; }
    const nx = Math.max(1, Math.round((b[1] - b[0]) / R.cell)), ny = Math.max(1, Math.round((b[3] - b[2]) / R.cell)), cx = (b[1] - b[0]) / nx, cy = (b[3] - b[2]) / ny;
    for (let i = 0; i < nx; i++) for (let j = 0; j < ny; j++) {
      const p: V = [b[0] + (i + 0.5) * cx, b[2] + (j + 0.5) * cy];
      if (!inZone(c, p) || under(p)) continue;
      const at = roadAt(c.ctx, p, '');
      if (!at || at.dk < -0.01 || at.dk > at.sec.pavement + R.reach) continue; // the pavement: kerb face to the building line (and a half cell past)
      const ring = boxRing(p, cx, cy, 0);
      if (c.drives.some(d => overlaps(ring, d))) continue;
      const v = 0; // Pave_Slab is one 2 x 2 m limestone slab in world-space UV (testbed flooring, brief 026), no sheet quadrants
      flatPiece(c, 'pave-flags', pieces[v]!, p, -Math.PI / 2, PAVE + LAYER.flags, [r3(cy / pieces[v]!.size[0]), 1, r3(cx / pieces[v]!.size[2])], `cap/flags/${pl.id}`);
    }
  }
}

function pits(c: Ctx2): void {
  const pieces = RULES.pit.pieces.map(adoptedBy);
  if (pieces.some(p => !p)) { gap(c, 'G3', 'tree-pit', 'TreePit_Flags_* is a candidate: it draws the 4096 pave_flags and granite sheets; needs a Vela remap onto DK_Ground_Pavement / DK_Ground_Kerb before it is adopted'); return; }
  for (const t of c.L.items.slice()) {
    const planted = t.kind === 'tub-palm' && c.L.items.some(o => o.kind === 'planter' && dist(o.at, t.at) < 0.3); // a palm planted in a bed has no pit
    if ((t.kind !== 'tree' && t.kind !== 'tub-palm') || planted || t.z > 1 || !inZone(c, t.at) || t.edge.startsWith('lawn/') || lawn(c.ctx, t.at) || onCivic(c, t.at)) continue;
    const a = pieces[0]!;
    const it = flatPiece(c, 'tree-pit', a, t.at, -Math.PI / 2, PAVE + LAYER.tile - a.size[1], [1, 1, 1], t.edge);
    c.tiles.push(boxRing(t.at, a.size[0], a.size[2], 0)); void it;
  }
}

/** An urn or a potted piece at street level stands in a pit (the old city's pits and plinths): TreePit_Flags under each, centred on it. */
function potPits(c: Ctx2): void {
  const pieces = RULES.pit.pieces.map(adoptedBy); if (pieces.some(p => !p)) return;
  for (const t of c.L.items.slice()) {
    const pot = (t.kind === 'planter-bed' && /^(PottedTopiary|TopiaryCone|Planter_Urn|PottedPalm)/.test(t.kit?.id ?? '')) || (t.kind === 'planter' && /^Planter_Urn/.test(t.kit?.id ?? ''));
    if (!pot || t.z > 1 || !inZone(c, t.at) || onCivic(c, t.at) || c.L.solids.some(s => pointInRing(t.at, s))) continue;
    const a = pieces[0]!;
    flatPiece(c, 'tree-pit', a, t.at, -Math.PI / 2, PAVE + LAYER.tile - a.size[1], [1, 1, 1], t.edge);
  }
}

// ============================================================ G4 drains and manholes
function nearOtherRoad(c: Ctx2, p: V, own: string, m: number): boolean {
  return c.ctx.d.roads.some(r => !r.gyratory && r.id !== own && lineDist(p, r.line) < r.w / 2 + m);
}
function gutters(c: Ctx2, runs: Run[]): void {
  const D = RULES.drain, M = RULES.manhole, dr = adoptedBy(D.piece), mh = adoptedBy(M.piece);
  if (!dr) gap(c, 'G4', 'drain', `${D.piece} is not adopted`);
  if (!mh) gap(c, 'G4', 'manhole', `${M.piece} is not adopted`);
  const okRoad = (p: V, ring: V[], own: string) => inZone(c, p) && !nearOtherRoad(c, p, own, 0.6) && !c.strips.some(q => overlaps(ring, q)) && !c.drives.some(q => overlaps(ring, q)) && clearOfPoints(c, ring);
  for (const r of runs) {
    for (let s = D.first; dr && s < r.len - 2; s += D.every) {
      const at: V = [r.a[0] + r.u[0] * s + r.n[0] * D.off, r.a[1] + r.u[1] * s + r.n[1] * D.off], yaw = ang(r.u), ring = boxRing(at, dr.size[0], dr.size[2], yaw);
      if (okRoad(at, ring, r.road)) flatPiece(c, 'drain', dr, at, yaw, LAYER.gutter, [1, 1, 1], `cap/drain/${r.road}`);
    }
    // a manhole in the middle of each pavement's walk zone (a sec: kerb + walk/2 from the kerb face)
    const sec = c.ctx.S.sections[r.cls] as { kerb: number; walk: number };
    for (let s = M.walk / 2 + (r.len % M.walk) / 2; mh && s < r.len - 2; s += M.walk) {
      const at: V = [r.a[0] + r.u[0] * s - r.n[0] * (sec.kerb + sec.walk / 2), r.a[1] + r.u[1] * s - r.n[1] * (sec.kerb + sec.walk / 2)], ring = boxRing(at, mh.size[0], mh.size[2], 0);
      if (!inZone(c, at) || onCivic(c, at) || !clearOfPoints(c, ring) || c.strips.some(q => overlaps(ring, q)) || c.tiles.some(q => ringGap(q, ring) < 0.3) || c.L.nearby(at, 0.8).length) continue;
      flatPiece(c, 'manhole', mh, at, (Math.floor(at[0] * 7) % 4) * (Math.PI / 2), PAVE + LAYER.flags + 0.0005, [1, 1, 1], `cap/manhole/${r.road}`);
    }
  }
  // lanes: down each road's centre line, alternate lanes, clear of every other road's mouth
  for (const road of c.ctx.d.roads) {
    if (road.gyratory || !mh) continue;
    let acc = 0, next = M.lane / 2 + (rng(`cap/manhole/${road.id}`).int(0, 8)), k = 0;
    for (let i = 1; i < road.line.length; i++) {
      const a = road.line[i - 1]!, b = road.line[i]!, seg = dist(a, b); if (seg < 1e-6) continue;
      const u = norm2([b[0] - a[0], b[1] - a[1]]), nl = perp(u);
      while (next <= acc + seg) {
        const t = next - acc, side = k++ % 2 ? 1 : -1, at: V = [a[0] + u[0] * t + nl[0] * side * road.w / 4, a[1] + u[1] * t + nl[1] * side * road.w / 4], ring = boxRing(at, mh.size[0], mh.size[2], 0);
        if (okRoad(at, ring, road.id) && !nearOtherRoad(c, at, road.id, 3) && !(road as any).reservation) flatPiece(c, 'manhole', mh, at, (Math.floor(at[1] * 7) % 4) * (Math.PI / 2), LAYER.gutter, [1, 1, 1], `cap/manhole/${road.id}`);
        next += M.lane;
      }
      acc += seg;
    }
  }
}

// ============================================================ G5 tactile paving and terrazzo aprons
function tactiles(c: Ctx2, crossings: Crossing[], fronts: Frontage[]): void {
  const t = adoptedBy(RULES.tactile.piece), z = adoptedBy(RULES.terrazzo.piece);
  if (!t) gap(c, 'G5', 'tactile', `${RULES.tactile.piece} is not adopted`);
  const plotUse = (edge: string) => c.ctx.d.plots.find(p => p.id === edge.split('/e')[0])?.use;
  for (const x of crossings) {
    if (x.kind !== 'kerb') continue;
    for (const l of x.landings) {
      if (!t || !inZone(c, l.at) || ['square', 'civic'].includes(plotUse(l.edge) ?? '')) continue;
      const d: V = [Math.cos(l.yaw), Math.sin(l.yaw)], pp = perp(d);
      for (const side of [-1, 1]) {
        const at: V = [l.at[0] + d[0] * 0.1 + pp[0] * side, l.at[1] + d[1] * 0.1 + pp[1] * side], ring = boxRing(at, t.size[0], t.size[2], l.yaw);
        if (onCivic(c, at) || c.L.solids.some(s => pointInRing(at, s)) || samples9(ring).some(p => { const r = roadAt(c.ctx, p, l.edge); return !r || r.dk < -0.01; })) continue;
        flatPiece(c, 'tactile', t, at, l.yaw, PAVE + LAYER.tile - t.size[1], [1, 1, 1], `cap/tactile/${x.road}`); c.tiles.push(ring);
      }
    }
  }
  const R = RULES.terrazzo;
  for (const w of tenancies(c, fronts).filter(w => R.trades.includes(w.f.trade))) {
    if (!z) { gap(c, 'G5', 'terrazzo', `${R.piece} is not adopted`); break; }
    for (const side of [-1, 1]) {
      const at = w.at(w.sd + side * R.across / 2, z.size[2] / 2), ring = boxRing(at, z.size[0], z.size[2], ang(w.u));
      if (onCivic(c, at) || c.L.solids.some(s => pointInRing(at, s)) || c.drives.some(d => overlaps(ring, d))) continue;
      flatPiece(c, 'terrazzo', z, at, ang(w.u), PAVE + LAYER.tile - z.size[1], [1, 1, 1], `cap/terrazzo/${w.f.street}`); c.tiles.push(ring);
    }
  }
}

// ============================================================ standing pieces by trade
const corners = (c: Ctx2): Corner[] => junctionCorners(c.ctx.d.roads as any, c.ctx.plan.nodes as any);

function wallPiece(c: Ctx2, kind: string, a: KitAsset | undefined, w: Wall, s: number, o: { back?: number; out?: number; face?: V; blocks?: boolean; variant?: string; anyZone?: boolean } = {}): StreetItem | undefined {
  if (!a) return undefined;
  const depth = a.size[2], out = o.out ?? (o.back ?? 0.06) + depth / 2, at = w.at(s, out), yaw = facing(o.face ?? w.n);
  return standing(c, kind, a, at, yaw, PAVE, `cap/${w.f.street}`, { blocks: o.blocks ?? true, variant: o.variant, anyZone: o.anyZone });
}
const trade = (w: Wall, list: readonly string[]) => list.includes(w.f.trade);
const nearCorner = (c: Ctx2, p: V, cs: Corner[]) => Math.min(...cs.map(k => dist(k.at, p)), Infinity);

function robots(c: Ctx2, walls: Wall[]): void {
  const R = RULES.robots, cs = corners(c);
  // waiters: one per terrace run (consecutive refreshment tenancies on one wall), at the run's open end, on the terrace, facing along it
  const wait = walls.filter(w => trade(w, R.waiter.trades) && w.f.w >= R.waiter.minW);
  const runs: Wall[][] = [];
  for (const w of wait) { const r = runs.find(q => q[q.length - 1]!.f.street === w.f.street && q[q.length - 1]!.f.host === w.f.host && dist(q[q.length - 1]!.f.b as V, w.f.a as V) < 0.6); if (r) r.push(w); else runs.push([w]); }
  const waiter = robotPiece('waiter');
  for (const run of runs) {
    const first = run[0]!, last = run[run.length - 1]!, ends = [{ w: first, s: 1.2, face: first.u }, { w: last, s: last.f.w - 1.2, face: [-last.u[0], -last.u[1]] as V }];
    const order = rng(`cap/waiter/${first.f.id}`).chance(0.5) ? ends : [...ends].reverse();
    const got = order.some(e => !!wallPiece(c, 'service-robot', waiter, e.w, e.s, { out: R.waiter.inFront, face: e.face, blocks: true, variant: 'waiter' }));
    if (!got) gap(c, 'R-waiter', first.f.id, `no place at either open end of the ${first.f.trade} terrace on ${first.f.street} that passes the street rules (door 2 m, walk zone, corner sight)`);
  }
  // shopkeepers: beside a shop's own wall, never in the doorway
  const shopkeeper = robotPiece('shopkeeper');
  for (const w of walls.filter(w => trade(w, R.shopkeeper.trades))) {
    const half = (shopkeeper?.size[0] ?? 1) / 2, ss = scan(w, half), order = rng(`cap/shop/${w.f.id}`).chance(0.5) ? ss : ss.slice().reverse();
    const got = order.some(s => !!wallPiece(c, 'service-robot', shopkeeper, w, s, { out: R.shopkeeper.inFront, blocks: true, variant: 'shopkeeper' }));
    if (!got) gap(c, 'R-shopkeeper', w.f.id, `${w.f.trade} ${w.f.w} m: no standing place beside the door that keeps SF-2's 2 m off the jambs`);
  }
  // butlers: at the door of an office or a hotel, one per tenancy
  const butler = robotPiece('butler');
  for (const w of walls.filter(w => trade(w, R.butler.trades))) {
    const half = (butler?.size[0] ?? 1) / 2, ss = scan(w, half), order = rng(`cap/butler/${w.f.id}`).chance(0.5) ? ss : ss.slice().reverse();
    const got = order.some(s => !!wallPiece(c, 'service-robot', butler, w, s, { out: R.butler.inFront, blocks: true, variant: 'butler' }));
    if (!got) gap(c, 'R-butler', w.f.id, `${w.f.trade} ${w.f.w} m: no standing place at the door`);
  }
  void cs;
}


/** Porters: at the tram stop that serves the block (a stop within `reach` m of the zone), on the platform beside its ticket machine, facing along the platform: where a porter waits for luggage in a real city. */
function porters(c: Ctx2, stops: { id: string; ring: V[] }[]): void {
  const a = robotPiece('porter'), R = RULES.robots.porter;
  if (!a) { gap(c, 'R-porter', 'porter', 'no porter robot adopted'); return; }
  const dz = (p: V) => Math.hypot(Math.max(c.zone[0] - p[0], 0, p[0] - c.zone[1]), Math.max(c.zone[2] - p[1], 0, p[1] - c.zone[3]));
  for (const st of stops.filter(m => dz(m.ring[0]!) <= R.reach)) {
    const edge = st.id.replace(/^stop\//, 'stop/'), tm = c.L.items.find(i => i.kind === 'ticket-machine' && i.edge === edge); if (!tm) continue;
    const ys = st.ring.map(p => p[1]), xs = st.ring.map(p => p[0]), axis: V = Math.max(...ys) - Math.min(...ys) > Math.max(...xs) - Math.min(...xs) ? [0, 1] : [1, 0], mid: V = [(Math.min(...xs) + Math.max(...xs)) / 2, (Math.min(...ys) + Math.max(...ys)) / 2];
    const sgn = [1, -1], base = (tm.at[0] - mid[0]) * axis[0] + (tm.at[1] - mid[1]) * axis[1];
    let done = false;
    for (let d = 2; d <= 8 && !done; d += 0.5) for (const sg of sgn) {
      const t = base + sg * d, at: V = [mid[0] + axis[0] * t, mid[1] + axis[1] * t];
      if (standing(c, 'service-robot', a, at, facing([axis[0] * -sg, axis[1] * -sg]), tm.z, edge, { variant: 'porter', blocks: true, anyZone: true, force: true })) { done = true; break; }
    }
    if (!done) gap(c, 'R-porter', st.id, 'no place on the platform beside the ticket machine clear of the shelter and benches');
  }
}

/** Vending pairs and the Automat: wall-backed, by trade. */
function vending(c: Ctx2, walls: Wall[]): void {
  const V = RULES.vending, cs = corners(c), pair = V.pair.map(adoptedBy);
  walls.filter(w => trade(w, V.trades) && w.f.w >= V.minW).forEach((w, k) => {
    if (pair.some(p => !p)) { gap(c, 'V-pair', w.f.id, 'VendingMachine_* is not adopted'); return; }
    const [m1, m2] = k % 2 ? [pair[1]!, pair[0]!] : [pair[0]!, pair[1]!], ends = [0, 1].map(e => ({ e, near: nearCorner(c, w.at(e ? w.f.w : 0, 0), cs) })).sort((p, q) => p.near - q.near || (rng(`cap/vend/${w.f.id}`).chance(0.5) ? 1 : -1));
    for (const { e } of ends) {
      const dir = e ? -1 : 1, w1 = m1.size[0], w2 = m2.size[0];
      for (let s = w1 / 2 + 0.1; s < w.f.w / 2; s += 0.25) {
        const s1 = e ? w.f.w - s : s, s2 = s1 + dir * (w1 / 2 + w2 / 2 + 0.05), a1 = wallPiece(c, 'vending-machine', m1, w, s1, { back: V.backGap });
        if (!a1) continue;
        const a2 = wallPiece(c, 'vending-machine', m2, w, s2, { back: V.backGap });
        if (a2) return;
        c.L.evict([a1.id]); c.items.splice(c.items.indexOf(a1), 1); c.log.counts['vending-machine']!--; c.log.pieces[m1.id]!--;
      }
    }
    gap(c, 'V-pair', w.f.id, `${w.f.trade} ${w.f.w} m: no pair fits at either end clear of the door (2 m), the walk zone and the corner sight line`);
  });
  const am = adoptedBy(RULES.automat.piece);
  for (const w of walls.filter(w => trade(w, RULES.automat.trades))) {
    const got = am && scan(w, am.size[0] / 2).some(s => !!wallPiece(c, 'automat', am, w, s, { back: RULES.automat.backGap }));
    if (!got) gap(c, 'V-automat', w.f.id, `automat ${w.f.w} m: the ${am?.size[0]} m wall unit does not fit beside the door; SF-2 keeps 2 m off the jambs and the door leaves ${r3(w.f.w / 2 - 1.2 - 2)} m each side`);
  }
}

/** The Automat on every automat tenancy outside the zone too (brief 026, Sky: the old city placed it by hand; systems > scripts): the same wall rule, the same street tests, the whole district. The zone's own are placed by vending(). */
function automatsOutside(c: Ctx2, fronts: Frontage[]): void {
  const am = adoptedBy(RULES.automat.piece); if (!am) return;
  const outside = fronts.filter(f => f.level === 'street' && !f.service && f.w >= 3 && RULES.automat.trades.includes(f.trade)).map(f => wallOf(c, f)).filter(w => !w.zoneOK);
  for (const w of outside) {
    const got = scan(w, am.size[0] / 2).some(s => !!wallPiece(c, 'automat', am, w, s, { back: RULES.automat.backGap, anyZone: true }));
    if (!got) gap(c, 'V-automat', w.f.id, `automat ${w.f.w} m: the ${am.size[0]} m wall unit does not fit beside the door; SF-2 keeps 2 m off the jambs`);
  }
}

/** What stands in front of a machine stays clear (Vela's review: a sandwich board stood before the Cola machine): the 1 m in front of each vending machine, Automat and locker bank is a keep-clear zone for everything placed after, and CAP-7 holds it. */
export const FRONT_CLEAR = 1.0;
function frontZones(c: Ctx2): void {
  for (const it of c.items.filter(i => ['vending-machine', 'automat', 'lockers'].includes(i.kind))) {
    const f: V = [-Math.sin(it.yaw), Math.cos(it.yaw)], ring = boxRing([it.at[0] + f[0] * (it.size[1] / 2 + FRONT_CLEAR / 2), it.at[1] + f[1] * (it.size[1] / 2 + FRONT_CLEAR / 2)], it.size[0], FRONT_CLEAR, it.yaw), b = bbox(ring);
    c.L.keep.push([b[0] - 0.15, b[1] + 0.15, b[2] - 0.15, b[3] + 0.15]); // a little proud: the layer tests circles, the rule footprints
  }
}

/** Boards, baskets, lockers, planting at doors. */
function frontageFurniture(c: Ctx2, walls: Wall[]): void {
  const B = RULES.board, board = adoptedBy(B.piece);
  for (const w of walls.filter(w => trade(w, B.trades) && w.f.w >= B.minW)) {
    const ss = scan(w, (board?.size[0] ?? 0.6) / 2), order = rng(`cap/board/${w.f.id}`).chance(0.5) ? ss : ss.slice().reverse();
    if (!order.some(s => !!wallPiece(c, 'sandwich-board', board, w, s, { out: B.out, blocks: false }))) gap(c, 'B-board', w.f.id, `${w.f.trade} ${w.f.w} m: no place for the board clear of the door`);
  }
  const H = RULES.basket, hb = adoptedBy(H.piece);
  for (const w of walls.filter(w => trade(w, H.trades) && w.f.w >= H.minW)) {
    if (!hb) { gap(c, 'B-basket', w.f.id, `${H.piece} is not adopted`); break; }
    const cp = w.f.canopy, depth = cp.size[1], fromLine = (cp.c[0] - w.f.a[0]) * w.n[0] + (cp.c[1] - w.f.a[1]) * w.n[1] + depth / 2 - 0.35 - WALL_DEPTH(w.f);
    for (let s = H.pitch / 2; s < w.f.w; s += H.pitch) {
      const at = w.at(s, fromLine), z = cp.base - hb.size[1];
      if (!inZone(c, at)) continue;
      const it: StreetItem = { id: nextId(c, 'hanging-basket'), kind: 'hanging-basket', at: pivotFor(hb, at, facing(w.n)), yaw: facing(w.n), z: r3(z), size: [hb.size[0], hb.size[2], hb.size[1]], surface: 'trim', edge: `cap/${w.f.street}`, sight: 'open', source: 'kit', kit: { id: hb.id, scale: [1, 1, 1] } };
      c.items.push(it); c.L.items.push(it); bump(c, 'hanging-basket', hb.id); // overhead: not registered with the layer (nothing stands under it)
    }
  }
  const K = RULES.lockers, lk = adoptedBy(K.piece);
  for (const w of walls.filter(w => trade(w, K.trades))) {
    const ss = scan(w, (lk?.size[0] ?? 2.5) / 2), order = rng(`cap/lockers/${w.f.id}`).chance(0.5) ? ss : ss.slice().reverse();
    if (!order.some(s => !!wallPiece(c, 'lockers', lk, w, s, { back: 0.06 }))) gap(c, 'B-lockers', w.f.id, `${w.f.trade} ${w.f.w} m: no wall run for the lockers clear of the door`);
  }
  const P = RULES.planters, pots = P.pieces.map(adoptedBy);
  for (const w of walls.filter(w => trade(w, P.trades) && w.f.w >= P.minW)) {
    pots.forEach((p, i) => {
      if (!p) return;
      const side = i === 0 ? -1 : 1, half = p.size[0] / 2, ss = scan(w, half).filter(s => (s - w.sd) * side > 0), order = side < 0 ? ss.slice().reverse() : ss; // nearest the door first
      if (!order.some(s => !!wallPiece(c, 'planter-bed', p, w, s, { out: 0.2 + p.size[2] / 2, blocks: false }))) gap(c, 'B-planters', w.f.id, `${w.f.trade} ${w.f.w} m: no room for ${p.id} beside the door`);
    });
  }
}


/** Planting by the old city's siting (Vela round 2): a flower trough and a topiary cone at each frontage run end, urns flanking every fourth tenancy seam, window boxes on shop sills, shrub mounds in a garden's border. */
function planting(c: Ctx2, walls: Wall[], all: Frontage[]): void {
  const E = RULES.runEnd, bed = adoptedBy(E.bed), cone = adoptedBy(E.cone), urn = adoptedBy(RULES.urn.piece), WB = RULES.windowBox, wb = adoptedBy(WB.piece), sm = adoptedBy(RULES.shrub.piece);
  const hasNext = (f: Frontage, end: 'a' | 'b') => all.some(o => o !== f && o.level === 'street' && o.street === f.street && dist(o[end === 'b' ? 'a' : 'b'] as V, f[end] as V) < 0.6);
  for (const w of walls) {
    for (const end of ['a', 'b'] as const) {
      if (hasNext(w.f, end)) continue; // a run end: nothing continues this wall
      const from = end === 'a' ? 1 : -1;
      if (bed) { const half = bed.size[0] / 2, s = end === 'a' ? half + E.in : w.f.w - half - E.in; if (!wallPiece(c, 'planter-bed', bed, w, s, { out: 0.2 + bed.size[2] / 2, blocks: false })) gap(c, 'P-runend', w.f.id, `no place for the flower trough at the ${end === 'a' ? 'start' : 'end'} of the run`); }
      const palm = adoptedBy(RULES.runEnd.palm), top = trade(w, RULES.planters.trades) && palm ? palm : cone; // a tub palm marks the end of a hotel, lobby or bureau run, topiary the rest
      if (top) { const cone = top; const s = end === 'a' ? (bed?.size[0] ?? 2) + 0.5 + cone.size[0] / 2 : w.f.w - (bed?.size[0] ?? 2) - 0.5 - cone.size[0] / 2; void from; if (!wallPiece(c, 'planter-bed', cone, w, s, { out: 0.2 + cone.size[2] / 2, blocks: false })) gap(c, 'P-runend', w.f.id, 'no place for the topiary beside the trough'); }
    }
  }
  // urns flanking every fourth tenancy seam (a seam is the line between two tenancies of one wall)
  walls.forEach((w, k) => {
    if (!urn || k % RULES.urn.everyNth !== RULES.urn.everyNth - 1 || !hasNext(w.f, 'b')) return;
    const half = urn.size[0] / 2, ok = [w.f.w - half - 0.05, w.f.w + half + 0.05].map(s => !!wallPiece(c, 'planter-bed', urn, w, s, { out: 0.2 + urn.size[2] / 2, blocks: false })).filter(Boolean).length;
    if (!ok) gap(c, 'P-urn', w.f.id, 'no urn fits at the seam (SF-2 keeps 2 m off the doors either side)');
  });
  // window boxes: overhead of the pavement (z over 1 m), on the shopfront's sill, clear of the door
  for (const w of walls.filter(w => trade(w, WB.trades) && w.f.w >= WB.minW)) {
    if (!wb) { gap(c, 'P-windowbox', w.f.id, `${WB.piece} is not adopted`); break; }
    for (let s = WB.pitch / 2; s < w.f.w; s += WB.pitch) {
      if (Math.abs(s - w.sd) < WB.doorClear + wb.size[0] / 2) continue;
      const at = w.at(s, wb.size[2] / 2), yaw = facing(w.n);
      if (!inZone(c, at)) continue;
      const it: StreetItem = { id: nextId(c, 'window-box'), kind: 'window-box', at: pivotFor(wb, at, yaw), yaw, z: r3(PAVE + WB.sill), size: [wb.size[0], wb.size[2], wb.size[1]], surface: 'trim', edge: `cap/${w.f.street}`, sight: 'open', source: 'kit', kit: { id: wb.id, scale: [1, 1, 1] } };
      c.items.push(it); c.L.items.push(it); bump(c, 'window-box', wb.id);
    }
  }
  // shrub mounds: in the border of a garden (a park plot's lawn side of its kerbed pavement), every `every` m
  if (sm) {
    const gardens = new Set(c.ctx.d.plots.filter(p => (RULES.robots.nanny.plots as readonly string[]).includes(p.use)).map(p => p.id));
    for (const r of kerbRuns(c.ctx, c.zone).filter(r => gardens.has(r.plot))) {
      const sec = c.ctx.S.sections[r.cls] as { pavement: number }, off = sec.pavement + RULES.shrub.beyond;
      for (let s = RULES.shrub.every / 2; s < r.len - 1; s += RULES.shrub.every) {
        const at: V = [r.a[0] + r.u[0] * s - r.n[0] * off, r.a[1] + r.u[1] * s - r.n[1] * off];
        if (!standing(c, 'planter-bed', sm, at, ang(r.u), PAVE, `cap/garden/${r.plot}`, { blocks: false })) continue;
      }
    }
  }
}

/** Bike racks: in the kerb zone beside a frontage people leave a bike at, one per `every` m of street side. */
function bikeRacks(c: Ctx2, walls: Wall[]): void {
  const B = RULES.bikeRack, rack = adoptedBy(B.piece); if (!rack) { gap(c, 'K-bike', 'bike rack', `${B.piece} is not adopted`); return; }
  const placed: { at: V; street: string }[] = [];
  for (const w of walls.filter(w => trade(w, B.trades))) {
    const road = c.ctx.d.roads.find(r => r.id === w.f.street); if (!road) continue;
    const mid = w.at(w.f.w / 2, 0), sec = c.ctx.S.sections[road.class] as { kerb: number };
    // the road's nearest foot to the tenancy and the way away from the road
    let best = Infinity, foot: V = mid, tan: V = [1, 0];
    for (let i = 1; i < road.line.length; i++) { const a = road.line[i - 1]!, b = road.line[i]!, d = segDist(mid, a, b); if (d < best) { best = d; const u = norm2([b[0] - a[0], b[1] - a[1]]), t = Math.max(0, Math.min(dist(a, b), (mid[0] - a[0]) * u[0] + (mid[1] - a[1]) * u[1])); foot = [a[0] + u[0] * t, a[1] + u[1] * t]; tan = u; } }
    const away = norm2([mid[0] - foot[0], mid[1] - foot[1]]), off = road.w / 2 + B.off + rack.size[2] / 2 + 0.02;
    if (off + rack.size[2] / 2 > road.w / 2 + sec.kerb) continue; // the kerb zone must hold it
    if (placed.some(p => p.street === w.f.street && dist(p.at, foot) < B.every)) continue;
    let ok = false;
    for (const dx of [0, 2, -2, 4, -4, 6, -6, 8, -8, 10, -10]) {
      const at: V = [foot[0] + tan[0] * dx + away[0] * off, foot[1] + tan[1] * dx + away[1] * off];
      if (standing(c, 'bike-rack', rack, at, ang(tan), PAVE, `cap/${w.f.street}`, { blocks: false })) { placed.push({ at, street: w.f.street }); ok = true; break; }
    }
    if (!ok) gap(c, 'K-bike', w.f.id, `${w.f.trade} on ${w.f.street}: no kerb-zone place within 10 m of the tenancy passes the street rules`);
  }
}

/** Info lightboxes at junction corners (seeded: about half), 8-20 m up the street from the corner, in the kerb zone, opening on the footway. */
function lightboxes(c: Ctx2): void {
  const Lb = RULES.lightbox, a = adoptedBy(Lb.piece); if (!a) { gap(c, 'K-lightbox', 'lightbox', `${Lb.piece} is not adopted`); return; }
  const nodes = c.ctx.plan.nodes as Record<string, V>, placed: V[] = [];
  for (const k of corners(c)) {
    if (!inZone(c, k.at) || !rng(`cap/lightbox/${k.node}/${Math.round(k.at[0])},${Math.round(k.at[1])}`).chance(0.55)) continue;
    let done = false;
    for (const rid of k.roads) {
      const road = c.ctx.d.roads.find(r => r.id === rid); if (!road || done) continue;
      const n = nodes[k.node]!; let t: V = [1, 0], best = Infinity;
      for (let i = 1; i < road.line.length; i++) { const p = road.line[i - 1]!, q = road.line[i]!, d = segDist(k.at, p, q); if (d < best) { best = d; t = norm2([q[0] - p[0], q[1] - p[1]]); } }
      const toK: V = [k.at[0] - n[0], k.at[1] - n[1]], along = toK[0] * t[0] + toK[1] * t[1], nl = perp(t), side = toK[0] * nl[0] + toK[1] * nl[1] >= 0 ? 1 : -1;
      if (Math.abs(along) < 0.1) continue;
      const dir: V = [t[0] * Math.sign(along), t[1] * Math.sign(along)], off = 0.45 + a.size[2] / 2 + 0.05;
      for (let d = Lb.from; d <= Lb.to && !done; d += 1) {
        const p: V = [k.at[0] + dir[0] * d + nl[0] * side * off, k.at[1] + dir[1] * d + nl[1] * side * off];
        if (placed.some(q => dist(q, p) < Lb.apart)) continue;
        if (standing(c, 'info-lightbox', a, p, facing([nl[0] * side, nl[1] * side]), PAVE, `cap/${rid}`, { blocks: false })) { placed.push(p); done = true; }
      }
    }
    if (!done) gap(c, 'K-lightbox', `corner ${k.node}`, 'no kerb-zone place 8-20 m up either street passes the street rules');
  }
}

/** Nannies, drinking fountains and ring benches: the garden. */
function garden(c: Ctx2): void {
  const nanny = robotPiece('nanny'), fountain = adoptedBy(RULES.fountain.piece), ring = adoptedBy(RULES.roundBench.piece);
  const gardens = new Set(c.ctx.d.plots.filter(p => (RULES.robots.nanny.plots as readonly string[]).includes(p.use)).map(p => p.id)), benches = c.L.items.filter(i => i.kind === 'bench' && inZone(c, i.at) && (/^foot\//.test(i.edge) || gardens.has(i.edge.split('/e')[0]!))); // the benches of a garden: on its walk or on its edge
  benches.forEach((b, k) => {
    if (k % 2) return; // every other bench has a nanny
    const u: V = [Math.cos(b.yaw), Math.sin(b.yaw)], side = rng(`cap/nanny/${b.id}`).chance(0.5) ? 1 : -1;
    const half = (nanny?.size[0] ?? 1) / 2 + b.size[0] / 2 + 0.3;
    for (const sd of [side, -side]) {
      const at: V = [b.at[0] + u[0] * sd * half, b.at[1] + u[1] * sd * half];
      if (standing(c, 'service-robot', nanny, at, facing(u), PAVE, b.edge, { variant: 'nanny', blocks: true })) return;
    }
    for (const sd of [1, -1]) for (const dd of [1.6, 2.2, 3]) { // else on the lawn behind or before the bench, facing along it (the pram beside her)
      const nl: V = [-u[1] * sd, u[0] * sd], at: V = [b.at[0] + nl[0] * dd, b.at[1] + nl[1] * dd];
      if (standing(c, 'service-robot', nanny, at, facing(u), PAVE, b.edge, { variant: 'nanny', blocks: true })) return;
    }
    gap(c, 'R-nanny', b.id, 'no place beside the bench clear of the walking line');
  });
  for (const f of c.ctx.plan.foot.filter((q: any) => q.kind === 'walk')) {
    const a = f.pts[0] as V, b = f.pts[f.pts.length - 1] as V, len = dist(a, b), u = norm2([b[0] - a[0], b[1] - a[1]]), nl = perp(u), edge = `foot/${f.id}`;
    if (!fountain) { gap(c, 'G-fountain', f.id, `${RULES.fountain.piece} is not adopted`); continue; }
    for (let s = RULES.fountain.every / 2; s < len - 6; s += RULES.fountain.every) {
      const off = f.w / 2 - 0.5 - fountain.size[2] / 2;
      const ok = [1, -1].some(side => [0, 3, -3, 6, -6].some(dx => !!standing(c, 'drinking-fountain', fountain, [a[0] + u[0] * (s + dx) + nl[0] * side * off, a[1] + u[1] * (s + dx) + nl[1] * side * off], facing([-nl[0] * side, -nl[1] * side]), PAVE, edge, { blocks: false })));
      if (!ok && inZone(c, [a[0] + u[0] * s, a[1] + u[1] * s])) gap(c, 'G-fountain', f.id, `no place near ${Math.round(s)} m along ${f.id}`);
    }
  }
  // a ring bench round every third lawn tree that stands within reach of a path or a kerb: the ring is the tree's seat
  if (ring) {
    const trees = c.L.items.filter(i => i.kind === 'tree' && i.edge.startsWith('lawn/') && inZone(c, i.at));
    trees.forEach((t, k) => {
      if (k % RULES.roundBench.everyTree) return;
      const r = ring.size[0] / 2, ringPoly = boxRing(t.at, ring.size[0], ring.size[2], 0);
      if (c.L.nearby(t.at, r + 0.2).some(o => o.id !== t.id)) { gap(c, 'G-ring', t.id, 'something else stands within the ring'); return; }
      if (!clearOfPoints(c, ringPoly)) return;
      const it = c.L.add('round-bench', t.at, 0, PAVE, t.edge, { size: [ring.size[0], ring.size[2], ring.size[1]], r: 0.1, id: nextId(c, 'round-bench'), uncounted: true, kit: { id: ring.id, scale: [1, 1, 1] }, force: true });
      if (it) { c.items.push(c.L.items[c.L.items.length - 1]!); bump(c, 'round-bench', ring.id); }
    });
  }
}

/** The programme: call after every other street layer, so it takes what the furniture, the crossings and the quiet places left. Returns the log (counts and the places no rule could fill). */
export function dressCapstone(ctx: Ctx, L: Layer, o: { frontages: Frontage[]; crossings: Crossing[]; stops?: { id: string; ring: V[] }[] }): { items: StreetItem[]; ground: StreetItem[]; log: CapLog } | undefined {
  if (!ctx.civ.dressing || !adoptedBy('DrainGrate')) return undefined; // a plan without the civic block (or a kit run pinned before brief 026) has no capstone
  const zone = zoneOf(ctx), toPlan = toPlanFn(ctx.plan.civic);
  const c: Ctx2 = {
    ctx, L, zone, items: [], n: {}, tiles: [], flat: [],
    log: { zone, counts: {}, pieces: {}, gaps: [], kerb: { edgeM: 0, stoneM: 0, droppedM: 0 } },
    points: handoverPoints(toPlan),
    strips: o.crossings.flatMap(x => [x.strip as V[], ...x.landings.map(l => boxRing(l.at, l.size[0], l.size[1], l.yaw))]).filter(p => p.length >= 3),
    drives: (ctx.civ.drives ?? []).map(d => d.poly as V[]),
    civicPlots: ctx.d.plots.filter(p => p.civic || ['square', 'civic'].includes(p.use)).map(p => p.polyV),
  };
  // the strips alone for the kerb drop (a landing is a keep-clear footprint on the pavement: the kerb stands, the crossing's own strip is what drops it)
  const stripOnly = o.crossings.map(x => x.strip as V[]);
  const runs = kerbRuns(ctx, zone), walls = tenancies(c, o.frontages);
  const all = c.strips; c.strips = stripOnly; kerbs(c, runs); c.strips = all;
  flags(c); pits(c); tactiles(c, o.crossings, o.frontages); gutters(c, runs);
  robots(c, walls); vending(c, walls); automatsOutside(c, o.frontages); frontZones(c); frontageFurniture(c, walls); planting(c, walls, o.frontages); bikeRacks(c, walls); lightboxes(c); garden(c); porters(c, o.stops ?? []); potPits(c);
  // the pieces no rule here can place: said, not forced
  if (!walls.some(w => w.f.trade === 'cinema')) gap(c, 'S-marquee', 'Marquee_Cinema', 'no cinema tenancy in the zone: a marquee is a cinema canopy');
  gap(c, 'S-billboard', 'Billboard_Wall/Pylon/Rooftop', 'the plan states no blank flank wall, pylon plot or roof billboard programme (a billboard stands where the plan says the city\'s lit signs go); Datum to say, or Sky');
  gap(c, 'G-corner', 'Kerb_GraniteCorner_*, Kerb_GraniteNose', 'the plot slabs have square corners; a corner piece needs a slab rounded to its 2 m return and its orientation verified in Unity. Latch draws the extruded kerb at junction corners only (within 2.4 m of a plot corner); the straight runs are kit stone');
  for (const k of Object.keys(c.log.counts)) c.log.counts[k] = c.items.filter(i => i.kind === k).length;
  c.log.kerb = { edgeM: r3(c.log.kerb.stoneM + c.log.kerb.droppedM), stoneM: r3(c.log.kerb.stoneM), droppedM: r3(c.log.kerb.droppedM) };
  void walkBlock; void samples9;
  return { items: c.items, ground: c.flat, log: c.log };
}
