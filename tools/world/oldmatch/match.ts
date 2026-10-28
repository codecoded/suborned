// The plan-match check (brief 075 remap step 2, Splice; Sky 2026-10-29: "we build a world with the old code, and then we map and take it from there"). The old
// DecoKit builders build the look; the plan stays the source for missions. This compares the old-built scene's geometry (briefs/075/oldbuild/built.json,
// contracts/world/oldbuilt.schema.ts, exported by Latch's swap) with the plan as our world export realises it (the same generated() world the checker reads):
//   - footprints: the outline of the union of building footprints, both ways, within MATCH.footprint (0.5 m);
//   - kerbs: the carriageway edges (derived from roads and circuses on both sides; and the built kerb lines when exported) within MATCH.kerb (0.5 m);
//   - pavements: the outline of pavement ∪ footprints (the kerb-side edge of every block) within MATCH.kerb, and each built pavement's top equal to ours there;
//   - entrances: each door within MATCH.entrance (1 m) of one on the other side, at the same level;
//   - levels: the built levels equal to plan standards.levels.list (MATCH.level, 1 mm: "exact"), decks at a plan level, deck outlines within 0.5 m.
// Every mismatch is reported; any mismatch fails. Inside the Handover zone (plan standards.blocks.zone) a mismatch is a hard failure (the zone stays as it is);
// outside it the old-built world wins (Sky's workflow), so its mismatches become a proposed plan patch for Datum (patchOf), never an edit to the plan here.
// Levels, decks and pavement tops are our network (brief 075 "levels and heights": kept as planned), so those go to the builder (Latch), not the patch.
import type { OldBuilt } from '../../../contracts/world/oldbuilt.schema';
import type { World } from '../../../contracts/world/world.schema';
import { type V, inPoly, segDist } from '../../plan/geom';
import { hostOf } from '../gen/colourways';
import { footprintOverlaps, type OverlapFault } from '../rules/overlap';

export const MATCH = {
  /** m: a footprint outline may sit this far from the other side's */
  footprint: 0.5,
  /** m: a kerb line or pavement edge likewise */
  kerb: 0.5,
  /** m: a door likewise (same level) */
  entrance: 1,
  /** m: levels, deck and pavement tops are exact (float noise only) */
  level: 0.001,
  /** m: a door belongs to the nearest plan level within this (a door at the head of a few steps, the Records Office's at 0.75) */
  door: 1,
  /** m: outline sampling step */
  step: 0.25,
  /** m: an outward probe this far closes a gap between neighbours (a gap within the footprint tolerance is a touch) */
  probe: 0.5,
  /** m: nothing on the other side within this = "none" */
  far: 40,
  /** m: a footprint at the foot: masses standing on the ground (base at or under this) */
  ground: 1,
};

type R4 = [number, number, number, number]; // x0 x1 y0 y1
export type Poly = { id: string; ring: V[]; kind?: string };
export type Chain = { id: string; pts: V[] };
export type Side = {
  foot: Poly[]; ground: Poly[]; kerbs: Chain[]; roads: { id: string; line: V[]; width: number; level: number }[];
  pavTops: { id: string; ring: V[]; top: number }[]; entrances: { id: string; building: string; at: V; z: number }[];
  levels: { id: string; z: number; walk?: number }[]; decks: { id: string; ring: V[]; z: number }[]; skipped: string[];
  /** the built kerb lines as exported (built side only) */ kerbLines?: Chain[];
  heights: { id: string; kind: string; height: number }[];
};
export type Kind = 'footprint' | 'kerb' | 'pavement' | 'entrance' | 'level' | 'deck' | 'pavement-top';
export type Mismatch = {
  id: string; kind: Kind; /** built-only: on the old-built side with nothing on ours within tolerance; plan-only: the reverse; differ: both exist, values differ */
  side: 'built-only' | 'plan-only' | 'differ'; el: string; near: string | null; dev: number | null; at: V; from?: V; to?: V; length?: number; zone: boolean; detail?: string;
};

// ------------------------------------------------------------------ geometry
const r3 = (x: number) => Math.round(x * 1000) / 1000;
const rv = (p: V): V => [r3(p[0]), r3(p[1])];
const signed = (r: V[]) => r.reduce((s, p, i) => { const q = r[(i + 1) % r.length]!; return s + p[0] * q[1] - q[0] * p[1]; }, 0) / 2;
const bbOf = (pts: V[]): R4 => pts.reduce<R4>((b, p) => [Math.min(b[0], p[0]), Math.max(b[1], p[0]), Math.min(b[2], p[1]), Math.max(b[3], p[1])], [Infinity, -Infinity, Infinity, -Infinity]);
const inR = (p: V, r: R4, g = 0) => p[0] >= r[0] - g && p[0] <= r[1] + g && p[1] >= r[2] - g && p[1] <= r[3] + g;
export const circle = (c: V, r: number, n = Math.max(24, Math.ceil((2 * Math.PI * r) / 1))): V[] => Array.from({ length: n }, (_, i) => [c[0] + r * Math.cos((2 * Math.PI * i) / n), c[1] + r * Math.sin((2 * Math.PI * i) / n)] as V);
/** A polyline buffered by `half` as one quad per segment (footways and carriageway strips). */
const strips = (id: string, line: V[], half: number): Poly[] => line.slice(1).map((b, i) => {
  const a = line[i]!, l = Math.hypot(b[0] - a[0], b[1] - a[1]) || 1, n: V = [(-(b[1] - a[1]) / l) * half, ((b[0] - a[0]) / l) * half];
  return { id, ring: [[a[0] + n[0], a[1] + n[1]], [b[0] + n[0], b[1] + n[1]], [b[0] - n[0], b[1] - n[1]], [a[0] - n[0], a[1] - n[1]]] as V[] };
});

/** A uniform grid of items by box. */
function grid<T>(items: T[], box: (t: T) => R4, cell = 8) {
  const g = new Map<string, number[]>();
  items.forEach((t, i) => { const b = box(t); for (let a = Math.floor(b[0] / cell); a <= Math.floor(b[1] / cell); a++) for (let c = Math.floor(b[2] / cell); c <= Math.floor(b[3] / cell); c++) (g.get(`${a},${c}`) ?? g.set(`${a},${c}`, []).get(`${a},${c}`)!).push(i); });
  return (p: V, r: number): number[] => {
    const s = new Set<number>();
    for (let a = Math.floor((p[0] - r) / cell); a <= Math.floor((p[0] + r) / cell); a++) for (let c = Math.floor((p[1] - r) / cell); c <= Math.floor((p[1] + r) / cell); c++) for (const i of g.get(`${a},${c}`) ?? []) s.add(i);
    return [...s];
  };
}

/** The outline of the union of polygons as chains of samples (MATCH.step apart): a sample of an edge is on the outline when a disc MATCH.probe across, standing
 *  just outside it, is clear of every polygon (so a gap or slot narrower than the probe between neighbours closes: it counts as touching). */
export function outline(polys: Poly[]): Chain[] {
  const P = polys.filter(p => p.ring.length >= 3).map(p => ({ ...p, bb: bbOf(p.ring) })), near = grid(P, p => p.bb, 16), out: Chain[] = [];
  const rad = MATCH.probe / 2;
  const blocked = (q: V) => near(q, rad).some(i => { const x = P[i]!; if (!inR(q, x.bb, rad)) return false; if (inPoly(q, x.ring)) return true; for (let k = 0; k < x.ring.length; k++) if (segDist(q, x.ring[k]!, x.ring[(k + 1) % x.ring.length]!) < rad - 1e-6) return true; return false; });
  for (const p of P) {
    const r = p.ring, ccw = signed(r) > 0;
    let cur: V[] = [];
    const flush = () => { if (cur.length) out.push({ id: p.id, pts: cur }); cur = []; };
    for (let i = 0; i < r.length; i++) {
      const a = r[i]!, b = r[(i + 1) % r.length]!, l = Math.hypot(b[0] - a[0], b[1] - a[1]);
      if (l < 1e-6) continue;
      const t: V = [(b[0] - a[0]) / l, (b[1] - a[1]) / l], n: V = ccw ? [t[1], -t[0]] : [-t[1], t[0]], k = Math.max(1, Math.ceil(l / MATCH.step));
      for (let j = 0; j < k; j++) {
        const s = (l * (j + 0.5)) / k, q: V = [a[0] + t[0] * s, a[1] + t[1] * s];
        if (blocked([q[0] + n[0] * (rad + 0.01), q[1] + n[1] * (rad + 0.01)])) flush(); else cur.push(q);
      }
    }
    flush();
  }
  return out;
}

/** A polyline with a point every MATCH.step at most. */
export const densify = (line: V[]): V[] => line.length < 2 ? line : line.slice(1).flatMap((b, i) => { const a = line[i]!, k = Math.max(1, Math.ceil(Math.hypot(b[0] - a[0], b[1] - a[1]) / MATCH.step)); return Array.from({ length: k + (i === line.length - 2 ? 1 : 0) }, (_, j) => [a[0] + ((b[0] - a[0]) * j) / k, a[1] + ((b[1] - a[1]) * j) / k] as V); });

/** Kerb lines of carriageways (each road's two edges and each circus's outer and island circle), the parts that lie inside no other carriageway. */
export function kerbsOf(roads: Side['roads'], circuses: { id: string; centre: V; radius: number; islandRadius: number }[]): Chain[] {
  const carriage = roads.flatMap(r => strips(r.id, r.line, r.width / 2 - MATCH.probe)), cg = grid(carriage, p => bbOf(p.ring), 16);
  const inside = (q: V, self: string) => cg(q, 0).some(i => carriage[i]!.id !== self && inPoly(q, carriage[i]!.ring)) || circuses.some(c => Math.hypot(q[0] - c.centre[0], q[1] - c.centre[1]) < c.radius - MATCH.probe);
  const out: Chain[] = [], keep = (id: string, line: V[]) => {
    let cur: V[] = [];
    for (const q of line) { if (inside(q, id)) { if (cur.length > 1) out.push({ id, pts: cur }); cur = []; } else cur.push(q); }
    if (cur.length > 1) out.push({ id, pts: cur });
  };
  const dense = densify;
  for (const r of roads) {
    if (r.line.length < 2) continue;
    for (const s of [1, -1]) {
      const off = r.line.map((p, i) => {
        const a = r.line[Math.max(0, i - 1)]!, b = r.line[Math.min(r.line.length - 1, i + 1)]!, l = Math.hypot(b[0] - a[0], b[1] - a[1]) || 1;
        return [p[0] - ((b[1] - a[1]) / l) * (r.width / 2) * s, p[1] + ((b[0] - a[0]) / l) * (r.width / 2) * s] as V;
      });
      keep(r.id, dense(off));
    }
  }
  for (const c of circuses) { keep(c.id, dense([...circle(c.centre, c.radius), circle(c.centre, c.radius)[0]!])); if (c.islandRadius > 0) out.push({ id: `${c.id}/island`, pts: dense([...circle(c.centre, c.islandRadius), circle(c.centre, c.islandRadius)[0]!]) }); }
  return out;
}

/** Distance from p to the nearest segment of `chains` (or Infinity beyond MATCH.far), with that chain's id. */
function nearestOn(chains: Chain[]) {
  const segs: { id: string; a: V; b: V }[] = [];
  for (const c of chains) { if (c.pts.length === 1) segs.push({ id: c.id, a: c.pts[0]!, b: c.pts[0]! }); for (let i = 1; i < c.pts.length; i++) segs.push({ id: c.id, a: c.pts[i - 1]!, b: c.pts[i]! }); }
  const g = grid(segs, s => bbOf([s.a, s.b]), 4);
  return (p: V): { d: number; id: string | null } => {
    for (const r of [2, 8, MATCH.far]) {
      let d = Infinity, id: string | null = null;
      for (const i of g(p, r)) { const s = segs[i]!, e = segDist(p, s.a, s.b); if (e < d) { d = e; id = s.id; } }
      if (d <= r) return { d, id };
    }
    return { d: Infinity, id: null };
  };
}

// ------------------------------------------------------------------ the two sides
/** Our side: the plan as the world export realises it. */
export function worldSide(w: World, plan: { standards: any }): Side {
  const foot: Poly[] = [], heights: Side['heights'] = [];
  const addM = (id: string, ring: V[] | undefined, base: number, h: number, kind: string) => { if (ring && ring.length >= 3 && base <= MATCH.ground && h > 0) { foot.push({ id, ring, kind }); heights.push({ id, kind, height: h }); } };
  for (const m of w.buildings) addM(m.id, m.ring, m.base, m.height, 'building');
  // the civic block's buildings (café, the Records Office and its wings and porches), not its furniture: stands >= 3 m with a plan area >= 20 m²
  for (const m of w.civic) if (!m.hidden && m.ring && m.height >= 3 && Math.abs(signed(m.ring)) >= 20) addM(m.id, m.ring, m.base ?? 0, m.height, 'civic');
  for (const g of w.megastructures) for (const p of g.parts) addM(p.id, p.ring, p.base ?? 0, p.height ?? 0, 'megastructure');
  for (const l of w.landmarks) if (l.base <= MATCH.ground && l.base >= 0) addM(l.id, circle(l.centre, l.radius), l.base, l.height, 'landmark');
  for (const s of w.signatures as any[]) if (s.ground && s.base >= 0) { const [x0, x1, y0, y1] = s.ground as R4; addM(s.id, [[x0, y0], [x1, y0], [x1, y1], [x0, y1]], s.base, s.tip - s.base, 'signature'); }
  const g = w.ground as World['ground'];
  const pav: Side['pavTops'] = [...g.plots.map(p => ({ id: p.id, ring: p.ring, top: r3(p.base + p.height) })), ...g.foot.flatMap(f => strips(f.id, f.line, f.width / 2).map(s => ({ id: f.id, ring: s.ring, top: f.level })))];
  const roads = g.roads.map(r => ({ id: r.id, line: r.line, width: r.width, level: r.level }));
  const entrances = [
    ...w.frontages.map(f => ({ id: f.id, building: f.host, at: [f.entrance.c[0], f.entrance.c[1]] as V, z: f.z })),
    ...(w.takedown?.entrances ?? []).map(e => ({ id: `civic/${e.id}`, building: `civic/${e.building}`, at: e.at, z: e.z })),
  ];
  return {
    foot, heights, roads, pavTops: pav, entrances,
    ground: [...pav.map(p => ({ id: p.id, ring: p.ring })), ...foot],
    kerbs: kerbsOf(roads, g.circuses.map(c => ({ id: c.id, centre: c.centre, radius: c.radius, islandRadius: c.islandRadius }))),
    levels: (plan.standards.levels.list as { id: string; z: number; walk?: number }[]).map(l => ({ id: l.id, z: l.z, walk: l.walk })),
    decks: w.levels.flatMap(l => l.decks.map(d => ({ id: d.id, ring: d.ring, z: l.height }))),
    skipped: [],
  };
}

/** The old-built side (suborned.oldbuilt/1), its points moved into the plan frame. */
export function builtSide(b: OldBuilt): Side {
  if (b.schema !== 'suborned.oldbuilt/1') throw new Error(`built.json: schema ${b.schema}, want suborned.oldbuilt/1`);
  const [ox, oy] = b.frame?.offset ?? [0, 0], mv = (p: V): V => [p[0] + ox, p[1] + oy], mr = (r: V[]) => r.map(mv), skipped: string[] = [];
  const foot = b.buildings.filter(x => x.base <= MATCH.ground).map(x => ({ id: x.id, ring: mr(x.ring), kind: x.kind }));
  const roads = b.roads.map(r => ({ id: r.id, line: mr(r.line), width: r.width, level: r.level }));
  const circ = (b.circuses ?? []).map(c => ({ id: c.id, centre: mv(c.centre), radius: c.radius, islandRadius: c.islandRadius }));
  const pav = b.pavements.map(p => ({ id: p.id, ring: mr(p.ring), top: p.top }));
  if (!b.circuses) skipped.push('circuses not exported: no roundabout kerbs on the built side');
  if (!b.decks) skipped.push('decks not exported: deck outlines and levels not compared');
  return {
    foot, roads, pavTops: pav,
    heights: b.buildings.map(x => ({ id: x.id, kind: x.kind, height: x.height })),
    ground: [...pav.map(p => ({ id: p.id, ring: p.ring })), ...foot],
    kerbs: kerbsOf(roads, circ),
    entrances: b.entrances.map(e => ({ id: e.id, building: e.building, at: mv(e.at), z: e.z })),
    levels: b.levels,
    decks: (b.decks ?? []).map(d => ({ id: d.id, ring: mr(d.ring), z: d.z })),
    kerbLines: b.kerbs?.map(k => ({ id: k.id, pts: densify(mr(k.line)) })),
    skipped,
  };
}

/** Our world as an oldbuilt file (the identity fixture, and an example of the format for Latch). */
export function builtOf(w: World, plan: { standards: any }, offset: V = [0, 0]): OldBuilt {
  const s = worldSide(w, plan), mv = (p: V): V => rv([p[0] - offset[0], p[1] - offset[1]]), mr = (r: V[]) => r.map(mv);
  const kindOf: Record<string, OldBuilt['buildings'][number]['kind']> = { building: 'domestic', civic: 'civic', megastructure: 'megastructure', landmark: 'landmark', signature: 'signature' };
  const g = w.ground as World['ground'];
  return {
    schema: 'suborned.oldbuilt/1',
    source: { scene: 'our world export (identity)', builder: 'tools/world/oldmatch builtOf' },
    frame: { units: 'm', offset },
    levels: s.levels,
    buildings: s.foot.map(f => ({ id: f.id, building: f.kind === 'civic' ? 'civic' : f.kind === 'megastructure' ? f.id.split('/').slice(0, 2).join('/') : hostOf(f.id), ring: mr(f.ring), base: 0.18, height: s.heights.find(h => h.id === f.id)!.height, kind: kindOf[f.kind ?? 'building'] ?? 'other', origin: 'ours' as const })),
    entrances: s.entrances.map(e => ({ id: e.id, building: e.building, at: mv(e.at), z: e.z })),
    roads: s.roads.map(r => ({ id: r.id, line: mr(r.line), width: r.width, level: r.level })),
    circuses: g.circuses.map(c => ({ id: c.id, centre: mv(c.centre), radius: c.radius, islandRadius: c.islandRadius, level: c.level })),
    pavements: s.pavTops.map((p, i) => ({ id: `${p.id}#${i}`, ring: mr(p.ring), top: p.top })),
    decks: s.decks.map(d => ({ id: d.id, ring: mr(d.ring), z: d.z, level: w.levels.find(l => l.height === d.z)?.id.replace(/^level\//, '') ?? '?' })),
  };
}

// ------------------------------------------------------------------ comparison
/** Chains of A against chains of B: runs of samples further than `tol` from B, one mismatch per run. */
function chainDiff(kind: Kind, side: Mismatch['side'], A: Chain[], B: Chain[], tol: number, zone: (p: V) => boolean): Mismatch[] {
  const near = nearestOn(B), out: Mismatch[] = [];
  for (const c of A) {
    let run: { p: V; d: number; id: string | null }[] = [];
    const flush = () => {
      if (!run.length) return;
      const worst = run.reduce((m, x) => (x.d > m.d ? x : m)), len = run.length * MATCH.step, ids = new Set(run.map(x => x.id).filter(Boolean));
      // a run shorter than a sample step either side of a corner is a probe artefact at a convex joint, not a feature
      if (len >= MATCH.step * 2 || worst.d > tol * 2) out.push({ id: '', kind, side, el: c.id, near: worst.id ?? (ids.size ? [...ids][0]! : null), dev: Number.isFinite(worst.d) ? r3(worst.d) : null, at: rv(worst.p), from: rv(run[0]!.p), to: rv(run[run.length - 1]!.p), length: r3(len), zone: run.some(x => zone(x.p)) });
      run = [];
    };
    for (const p of c.pts) { const n = near(p); if (n.d > tol) run.push({ p, ...n }); else flush(); }
    flush();
  }
  return out;
}

const levelOf = (z: number, levels: Side['levels']) => levels.map(l => ({ l, d: Math.min(Math.abs(z - l.z), Math.abs(z - (l.walk ?? l.z))) })).sort((a, b) => a.d - b.d)[0];

export type Report = {
  schema: 'suborned.oldmatch/1'; tolerance: typeof MATCH; zone: R4; counts: Record<string, number>; inZone: number; outside: number;
  mismatches: Mismatch[]; skipped: string[]; overlap: { checked: number; faults: OverlapFault[] };
  heights: { tall: number; low: number; byKind: Record<string, { n: number; max: number }> };
};

export function compare(built: Side, ours: Side, plan: { standards: any }, overlap: { checked: number; faults: OverlapFault[] } = { checked: 0, faults: [] }): Report {
  const zr = plan.standards.blocks.zone as R4, zone = (p: V) => inR(p, zr), M: Mismatch[] = [];
  // footprints
  const fb = outline(built.foot), fo = outline(ours.foot);
  M.push(...chainDiff('footprint', 'built-only', fb, fo, MATCH.footprint, zone), ...chainDiff('footprint', 'plan-only', fo, fb, MATCH.footprint, zone));
  // kerbs: derived both sides; the built kerb lines too when exported (against ours and our pavement edges, which carry the rounded returns)
  M.push(...chainDiff('kerb', 'built-only', built.kerbs, ours.kerbs, MATCH.kerb, zone), ...chainDiff('kerb', 'plan-only', ours.kerbs, built.kerbs, MATCH.kerb, zone));
  if (built.kerbLines) M.push(...chainDiff('kerb', 'built-only', built.kerbLines, [...ours.kerbs, ...outline(ours.ground)], MATCH.kerb, zone).map(m => ({ ...m, detail: 'built kerb line' })));
  // pavements: the kerb-side outline of pavement ∪ footprints
  const gb = outline(built.ground), go = outline(ours.ground);
  M.push(...chainDiff('pavement', 'built-only', gb, go, MATCH.kerb, zone), ...chainDiff('pavement', 'plan-only', go, gb, MATCH.kerb, zone));
  // pavement tops: a built pavement's top equals the top of ours under its first inside sample
  for (const p of built.pavTops) {
    const q = insidePoint(p.ring);
    if (!q) continue;
    const under = ours.pavTops.filter(o => inPoly(q, o.ring));
    if (under.length && !under.some(o => Math.abs(o.top - p.top) <= MATCH.level)) M.push({ id: '', kind: 'pavement-top', side: 'differ', el: p.id, near: under[0]!.id, dev: r3(Math.min(...under.map(o => Math.abs(o.top - p.top)))), at: rv(q), zone: zone(q), detail: `top ${p.top}, ours ${[...new Set(under.map(o => o.top))].join('/')}` });
  }
  // entrances, at the same level
  const ent = (A: Side['entrances'], B: Side['entrances'], side: Mismatch['side']) => {
    const g = grid(B, e => bbOf([e.at]), 8);
    for (const e of A) {
      const la = levelOf(e.z, ours.levels);
      if (!la || la.d > MATCH.door) { M.push({ id: '', kind: 'entrance', side, el: e.id, near: null, dev: null, at: rv(e.at), zone: zone(e.at), detail: `z ${e.z} is on no plan level` }); continue; }
      let best = Infinity, bid: string | null = null;
      for (const i of g(e.at, MATCH.far)) { const o = B[i]!; if (levelOf(o.z, ours.levels)?.l.id !== la.l.id) continue; const d = Math.hypot(o.at[0] - e.at[0], o.at[1] - e.at[1]); if (d < best) { best = d; bid = o.id; } }
      if (best > MATCH.entrance) M.push({ id: '', kind: 'entrance', side, el: e.id, near: bid, dev: Number.isFinite(best) ? r3(best) : null, at: rv(e.at), zone: zone(e.at), detail: `${la.l.id} door of ${e.building}` });
    }
  };
  ent(built.entrances, ours.entrances, 'built-only'); ent(ours.entrances, built.entrances, 'plan-only');
  // levels, exact
  for (const l of ours.levels) {
    const b = built.levels.find(x => x.id === l.id);
    if (!b) M.push({ id: '', kind: 'level', side: 'plan-only', el: l.id, near: null, dev: null, at: [0, 0], zone: false, detail: `plan level ${l.id} (z ${l.z}) not in built.levels` });
    else if (Math.abs(b.z - l.z) > MATCH.level || (b.walk !== undefined && l.walk !== undefined && Math.abs(b.walk - l.walk) > MATCH.level)) M.push({ id: '', kind: 'level', side: 'differ', el: l.id, near: l.id, dev: r3(Math.max(Math.abs(b.z - l.z), Math.abs((b.walk ?? l.walk ?? 0) - (l.walk ?? 0)))), at: [0, 0], zone: false, detail: `built z ${b.z} walk ${b.walk}, plan z ${l.z} walk ${l.walk}` });
  }
  for (const b of built.levels) if (!ours.levels.some(l => l.id === b.id)) M.push({ id: '', kind: 'level', side: 'built-only', el: b.id, near: null, dev: null, at: [0, 0], zone: false, detail: `built level ${b.id} (z ${b.z}) is not a plan level` });
  // decks: at a plan level, outlines within the kerb tolerance
  if (built.decks.length || !built.skipped.some(s => s.startsWith('decks'))) {
    for (const d of built.decks) { const q = insidePoint(d.ring) ?? d.ring[0]!; if (!ours.levels.some(l => Math.abs(d.z - l.z) <= MATCH.level || Math.abs(d.z - (l.walk ?? l.z)) <= MATCH.level)) M.push({ id: '', kind: 'deck', side: 'differ', el: d.id, near: null, dev: null, at: rv(q), zone: zone(q), detail: `deck z ${d.z} is on no plan level` }); }
    const zs = [...new Set([...built.decks, ...ours.decks].map(d => d.z))];
    for (const z of zs) {
      const db = outline(built.decks.filter(d => Math.abs(d.z - z) <= MATCH.level)), dov = outline(ours.decks.filter(d => Math.abs(d.z - z) <= MATCH.level));
      M.push(...chainDiff('deck', 'built-only', db, dov, MATCH.kerb, zone), ...chainDiff('deck', 'plan-only', dov, db, MATCH.kerb, zone));
    }
  }
  const order: Kind[] = ['level', 'footprint', 'kerb', 'pavement', 'pavement-top', 'entrance', 'deck'];
  M.sort((a, b) => order.indexOf(a.kind) - order.indexOf(b.kind) || Number(b.zone) - Number(a.zone) || (b.dev ?? 1e9) - (a.dev ?? 1e9) || a.at[0] - b.at[0] || a.at[1] - b.at[1]);
  M.forEach((m, i) => { m.id = `M${String(i + 1).padStart(4, '0')}`; });
  const counts: Record<string, number> = {};
  for (const m of M) counts[`${m.kind} ${m.side}`] = (counts[`${m.kind} ${m.side}`] ?? 0) + 1;
  const byKind: Report['heights']['byKind'] = {};
  for (const h of built.heights) { const k = (byKind[h.kind] ??= { n: 0, max: 0 }); k.n++; k.max = Math.max(k.max, r3(h.height)); }
  return {
    schema: 'suborned.oldmatch/1', tolerance: MATCH, zone: zr, counts, inZone: M.filter(m => m.zone).length, outside: M.filter(m => !m.zone).length, mismatches: M,
    skipped: built.skipped, overlap,
    heights: { tall: built.heights.filter(h => h.height > 17).length, low: built.heights.filter(h => h.height <= 17).length, byKind },
  };
}

/** A point inside a ring (its centroid if inside, else the first grid point that is). */
function insidePoint(r: V[]): V | null {
  if (r.length < 3) return null;
  const c: V = [r.reduce((s, p) => s + p[0], 0) / r.length, r.reduce((s, p) => s + p[1], 0) / r.length];
  if (inPoly(c, r)) return c;
  const b = bbOf(r);
  for (let i = 1; i < 8; i++) for (let j = 1; j < 8; j++) { const q: V = [b[0] + ((b[1] - b[0]) * i) / 8, b[2] + ((b[3] - b[2]) * j) / 8]; if (inPoly(q, r)) return q; }
  return null;
}

/** OV-1 on the old-built footprints (should pass trivially: the old builders lay party walls flush). Hosts: `building` else id. Landmarks are left out, as OV-1
 *  leaves them out of our world (solidsOf): a landmark's plan ring is its reserve (the Skylon's 34.5 m is its legs' spread), not a wall. */
export const builtOverlap = (b: OldBuilt) => {
  const [ox, oy] = b.frame?.offset ?? [0, 0];
  const xs = b.buildings.filter(x => x.kind !== 'landmark').map(x => ({ host: x.building ?? x.id, id: x.id, ring: x.ring.map(p => [p[0] + ox, p[1] + oy] as V), base: x.base, height: x.height }));
  return { checked: xs.length, faults: footprintOverlaps(xs) };
};

// ------------------------------------------------------------------ the proposed plan patch (outside the zone; for Datum)
export type PatchOp = {
  op: 'set' | 'add' | 'remove' | 'review'; path: string; owner: 'datum' | 'latch'; from?: unknown; to?: unknown; why: string; evidence: string[];
};
export type PlanPatch = {
  schema: 'suborned.plan-patch/1'; plan: string; planVersion: string; built: string; note: string; tolerance: typeof MATCH;
  ops: PatchOp[]; builderFixes: PatchOp[]; zoneFailures: string[];
};

export function patchOf(rep: Report, plan: any, built: OldBuilt, builtPath: string, planPath: string): PlanPatch {
  const [ox, oy] = built.frame?.offset ?? [0, 0], mv = (p: V): V => [p[0] + ox, p[1] + oy];
  const blocks = ((plan.blocks ?? []) as { id: string; plot: string; rect?: R4 }[]).filter((b): b is { id: string; plot: string; rect: R4 } => Array.isArray(b.rect)), plots = ((plan.plots ?? []) as { id: string; rect?: R4 }[]).filter((p): p is { id: string; rect: R4 } => Array.isArray(p.rect));
  const ops: PatchOp[] = [], fixes: PatchOp[] = [];
  const out = rep.mismatches.filter(m => !m.zone);
  const targetOf = (p: V): string | null => { const b = blocks.find(x => inR(p, x.rect, 1)); if (b) return `blocks[id=${b.id}]`; const q = plots.find(x => inR(p, x.rect, 1)); return q ? `plots[id=${q.id}]` : null; };
  const rectOf = (t: string): R4 | undefined => { const id = t.match(/id=([^\]]+)/)?.[1]; return (t.startsWith('blocks') ? blocks : plots).find(x => x.id === id)?.rect; };
  const bFoot = built.buildings.filter(x => x.base <= MATCH.ground).map(x => ({ id: x.id, ring: x.ring.map(mv), kind: x.kind }));
  // footprints, grouped by the plan block or plot they fall in
  const byT = new Map<string, Mismatch[]>(), orphan: Mismatch[] = [];
  for (const m of out.filter(x => x.kind === 'footprint')) { const t = targetOf(m.at); if (t) (byT.get(t) ?? byT.set(t, []).get(t)!).push(m); else orphan.push(m); }
  for (const [t, ms] of [...byT].sort((a, b) => (a[0] < b[0] ? -1 : 1))) {
    const r = rectOf(t)!, inside = bFoot.filter(f => { const c = insidePoint(f.ring); return c && inR(c, r, 2); });
    if (!inside.length) { ops.push({ op: 'remove', path: t, owner: 'datum', from: r, why: 'the old-built scene has no building on this block or plot', evidence: ms.map(m => m.id) }); continue; }
    const bb = bbOf(inside.flatMap(f => f.ring)).map(r3) as R4;
    ops.push({ op: 'set', path: `${t}.rect`, owner: 'datum', from: r, to: bb, why: `footprints differ by up to ${Math.max(...ms.map(m => m.dev ?? MATCH.far))} m; to = the box of the old-built footprints standing in it (${inside.length}); re-lay runs and the plot rect to suit`, evidence: ms.map(m => m.id) });
  }
  // built buildings on no plan block or plot: new plots
  const seen = new Set<string>();
  for (const m of orphan) {
    if (m.side === 'built-only' && !seen.has(m.el)) {
      seen.add(m.el);
      const f = bFoot.find(x => x.id === m.el);
      if (f) ops.push({ op: 'add', path: 'plots', owner: 'datum', to: { rect: bbOf(f.ring).map(r3), from: f.id, kind: f.kind }, why: 'an old-built building on no plan block or plot', evidence: orphan.filter(x => x.el === m.el).map(x => x.id) });
    } else if (m.side === 'plan-only') ops.push({ op: 'review', path: m.el, owner: 'datum', why: `our footprint has no old-built counterpart within ${MATCH.footprint} m (not on a plan block: a landmark, megastructure or signature)`, evidence: [m.id] });
  }
  // kerbs and pavement edges, grouped by the plan road they are nearest (world id road/<id>)
  const roadOf = (m: Mismatch) => { const id = (m.side === 'plan-only' ? m.el : m.near) ?? m.el; return id.replace(/\/island$/, ''); };
  const byR = new Map<string, Mismatch[]>();
  for (const m of out.filter(x => x.kind === 'kerb')) { const k = roadOf(m); (byR.get(k) ?? byR.set(k, []).get(k)!).push(m); }
  for (const [rid, ms] of [...byR].sort((a, b) => (a[0] < b[0] ? -1 : 1))) {
    const pid = rid.replace(/^road\//, '').replace(/^circus\//, ''), isC = rid.startsWith('circus/');
    const pr = isC ? undefined : (plan.roads ?? []).find((r: any) => r.id === pid);
    const br = built.roads.find(r => r.id === rid || r.id === pid) ?? nearestBuiltRoad(built, ms[0]!.at);
    ops.push({ op: 'review', path: isC ? 'circus' : pr ? `roads[id=${pid}]` : rid, owner: 'datum', from: pr ? { w: pr.w, path: pr.path } : undefined, to: br ? { w: br.width, line: simplify(br.line.map(mv)) } : undefined, why: `kerb lines differ by up to ${Math.max(...ms.map(m => m.dev ?? MATCH.far))} m over ${r3(ms.reduce((s, m) => s + (m.length ?? 0), 0))} m`, evidence: ms.map(m => m.id) });
  }
  const pav = out.filter(x => x.kind === 'pavement'), byP = new Map<string, Mismatch[]>();
  for (const m of pav) { const t = targetOf(m.at) ?? 'pavements'; (byP.get(t) ?? byP.set(t, []).get(t)!).push(m); }
  for (const [t, ms] of byP) ops.push({ op: 'review', path: t, owner: 'datum', why: `the kerb-side edge (pavement and buildings) differs by up to ${Math.max(...ms.map(m => m.dev ?? MATCH.far))} m`, evidence: ms.map(m => m.id) });
  const byE = new Map<string, Mismatch[]>();
  for (const m of out.filter(x => x.kind === 'entrance')) { const t = targetOf(m.at) ?? 'entrances'; (byE.get(t) ?? byE.set(t, []).get(t)!).push(m); }
  for (const [t, ms] of byE) ops.push({ op: 'review', path: t === 'entrances' ? t : `${t}.runs`, owner: 'datum', to: { builtOnly: ms.filter(m => m.side === 'built-only').map(m => m.at), planOnly: ms.filter(m => m.side === 'plan-only').map(m => m.at) }, why: 'doors differ (the old frontage places them; our frontage follows the runs)', evidence: ms.map(m => m.id) });
  for (const m of out.filter(x => ['level', 'deck', 'pavement-top'].includes(x.kind))) fixes.push({ op: 'review', path: m.el, owner: 'latch', why: `${m.kind}: ${m.detail ?? m.side} (levels, decks and pavement tops are our network, kept as planned)`, evidence: [m.id] });
  return {
    schema: 'suborned.plan-patch/1', plan: planPath, planVersion: plan.version, built: builtPath,
    note: 'Proposed by the plan-match check (tools/world/oldmatch) from the mismatches outside the Handover zone: the old-built world wins there (Sky 2026-10-29). Datum applies or rejects each op in the plan; nothing here edits the plan. Zone mismatches are hard failures, listed, never patched.',
    tolerance: MATCH, ops, builderFixes: fixes, zoneFailures: rep.mismatches.filter(m => m.zone).map(m => `${m.id} ${m.kind} ${m.side} ${m.el} at ${m.at.join(',')}`),
  };
}

function nearestBuiltRoad(b: OldBuilt, p: V) {
  const [ox, oy] = b.frame?.offset ?? [0, 0];
  let best = Infinity, r: OldBuilt['roads'][number] | undefined;
  for (const x of b.roads) for (let i = 1; i < x.line.length; i++) { const d = segDist(p, [x.line[i - 1]![0] + ox, x.line[i - 1]![1] + oy], [x.line[i]![0] + ox, x.line[i]![1] + oy]); if (d < best) { best = d; r = x; } }
  return best < MATCH.far ? r : undefined;
}
/** Collinear points dropped (the patch carries a readable centreline). */
function simplify(l: V[]): V[] {
  if (l.length < 3) return l.map(rv);
  const out: V[] = [l[0]!];
  for (let i = 1; i < l.length - 1; i++) { const a = out[out.length - 1]!, b = l[i]!, c = l[i + 1]!; if (Math.abs((b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])) > 0.01) out.push(b); }
  out.push(l[l.length - 1]!);
  return out.map(rv);
}
