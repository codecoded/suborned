// Street zones, measured from the plan: which section a point is in and how far past the kerb face; the clear walking zones
// (SF-1) and the tram's own space (SF-3). Shared by the SF rules (street-checks.ts) and by the street generator, which uses
// the same tests to skip an item that would break them ("skip, never squeeze", SF-6) instead of placing it and failing a check.
import type { Ctx } from '../world/gen/common';
import { plotRing } from '../world/gen/edges';
import { type V, lineDist, polyline, segDist } from './geom';
import { speciesOf } from './trees';

export type Sec = { cls: 'primary' | 'district' | 'access'; kerb: number; walk: number; frontage: number; pavement: number };
export const TOL = 0.011;
/** Kinds lying flush in the carriageway or the paving, or hanging overhead: they never count as standing in a zone. `crossing` is the landing slab of a plan without streetscape.crossings.strip; the rest are a crossing's light bars, dot rows, kerb studs, inlay arcs and signal orbs
 *  (brief 009; every gen/street/parts.ts PART_KINDS, which crossings.test.ts holds). An orb's pivot is on the ground, but its body hangs 2.3 m up on a beam of light: only its halo is on the ground, and SF-20 holds that clear of furniture. */
export const FLAT = new Set(['crossing', 'crossing-bar', 'crossing-dots', 'crossing-edge', 'crossing-field', 'crossing-stud', 'crossing-arc', 'signal-orb']);

export function boxRing(c: V, a: number, b: number, yaw: number): V[] {
  const cs = Math.cos(yaw), sn = Math.sin(yaw);
  return ([[-a / 2, -b / 2], [a / 2, -b / 2], [a / 2, b / 2], [-a / 2, b / 2]] as V[]).map(([x, y]) => [c[0] + x * cs - y * sn, c[1] + x * sn + y * cs] as V);
}
/** A footprint's corners, edge midpoints and centre: the points it is tested at (a rotated box is tested on its own corners, not its bounding box's). */
export function samples9(r: V[]): V[] {
  const n = r.length, mid = (a: V, b: V): V => [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2], cx = r.reduce((t, p) => t + p[0], 0) / n, cy = r.reduce((t, p) => t + p[1], 0) / n;
  return [...r, ...r.map((p, i) => mid(p, r[(i + 1) % n]!)), [cx, cy]];
}
/** Shortest distance between two rings' boundaries (0 if they overlap); small rings only. */
export function ringGap(a: V[], b: V[]): number {
  const ins = (p: V, r: V[]) => { let c = false; for (let i = 0, j = r.length - 1; i < r.length; j = i++) if ((r[i]![1] > p[1]) !== (r[j]![1] > p[1]) && p[0] < ((r[j]![0] - r[i]![0]) * (p[1] - r[i]![1])) / (r[j]![1] - r[i]![1]) + r[i]![0]) c = !c; return c; };
  if (a.some(p => ins(p, b)) || b.some(p => ins(p, a))) return 0;
  const cl = (r: V[]) => [...r, r[0]!];
  return Math.min(...a.map(p => lineDist(p, cl(b))), ...b.map(p => lineDist(p, cl(a))));
}

/** Footprint of an item: a tree stands in its pit (the species' pit size), everything else in its size. */
export function footprintOf(S: any, kind: string, variant: string | undefined, at: V, size: [number, number, number], yaw: number): V[] {
  if (kind === 'tree') { const pit = speciesOf(S, variant)?.pit as V | undefined; if (pit) return boxRing(at, pit[0], pit[1], yaw); }
  return boxRing(at, size[0], size[1], yaw);
}

/** Segments of many polylines in a uniform grid: nearest distance within a radius without scanning every road's every chord. */
export class SegIndex {
  private m = new Map<string, number[]>(); private segs: { a: V; b: V; id: string }[] = [];
  constructor(lines: { id: string; line: V[] }[], private cell = 24) {
    for (const { id, line } of lines) for (let i = 1; i < line.length; i++) {
      const a = line[i - 1]!, b = line[i]!, k = this.segs.push({ a, b, id }) - 1;
      for (let x = Math.floor(Math.min(a[0], b[0]) / cell); x <= Math.floor(Math.max(a[0], b[0]) / cell); x++) for (let y = Math.floor(Math.min(a[1], b[1]) / cell); y <= Math.floor(Math.max(a[1], b[1]) / cell); y++) (this.m.get(`${x},${y}`) ?? this.m.set(`${x},${y}`, []).get(`${x},${y}`)!).push(k);
    }
  }
  /** Nearest distance from p to each line within r of it. */
  nearest(p: V, r: number): Map<string, number> {
    const out = new Map<string, number>(), seen = new Set<number>(), c = this.cell;
    for (let x = Math.floor((p[0] - r) / c); x <= Math.floor((p[0] + r) / c); x++) for (let y = Math.floor((p[1] - r) / c); y <= Math.floor((p[1] + r) / c); y++) for (const k of this.m.get(`${x},${y}`) ?? []) {
      if (seen.has(k)) continue; seen.add(k);
      const sg = this.segs[k]!, d = segDist(p, sg.a, sg.b); if (d < (out.get(sg.id) ?? Infinity)) out.set(sg.id, d);
    }
    return out;
  }
  /** Nearest distance to any line within r (Infinity if none). */
  dist(p: V, r: number): number { let m = Infinity; for (const d of this.nearest(p, r).values()) if (d < m) m = d; return m; }
}

type Idx = { roads: SegIndex; sky: SegIndex; tram: SegIndex; by: Map<string, Ctx['d']['roads'][number]>; edges: Map<string, string[]> };
const cache = new WeakMap<object, Idx>();
/** Per derived plan: the road, sky rail and tram track indexes, and the lazily classified plot edges. */
export function indexes(c: Pick<Ctx, 'd'>): Idx {
  let x = cache.get(c.d);
  if (!x) cache.set(c.d, x = {
    roads: new SegIndex(c.d.roads.filter(r => !r.gyratory).map(r => ({ id: r.id, line: r.line }))),
    sky: new SegIndex(c.d.sky.map(p => ({ id: p.id, line: polyline(p.segs, 2) as V[] }))),
    tram: new SegIndex(c.d.tram.map(p => ({ id: `${p.line}/${p.id}`, line: polyline(p.segs, 2) as V[] }))),
    by: new Map(c.d.roads.map(r => [r.id, r])), edges: new Map(),
  });
  return x;
}

/** The street a plot edge `<plot>/e<i>` faces (`kind:id` as plotRing classifies it), or ''. */
function ownStreet(c: Ctx, edge: string): string {
  const m = /^(.+)\/e(\d+)$/.exec(edge); if (!m) return '';
  const x = indexes(c), plot = c.d.plots.find(p => p.id === m[1]); if (!plot) return '';
  let cs = x.edges.get(plot.id); if (!cs) x.edges.set(plot.id, cs = plotRing(c, plot).c);
  return cs[Number(m[2])] ?? '';
}

export type At = { dk: number; sec: Sec; road: string };
/** The section p belongs to: the road the item's own plot edge faces (else the nearest road), the circus ring for circus items; dk = distance past the kerb face. */
export function roadAt(c: Ctx, p: V, edge: string): At | null {
  const S = c.S, cc = c.plan.circus, x = indexes(c);
  const mk = (r: { class: Sec['cls'] }, dk: number, road: string): At => {
    const b = S.sections[r.class] as Sec & { avenue?: Sec }, area = c.plan.plots.find(q => q.id === edge.split('/e')[0])?.area;
    const av = r.class === 'access' && area && (S.trees.avenues.access.avenue as string[]).includes(area) ? b.avenue : undefined;
    return { dk, road, sec: { cls: r.class, pavement: b.pavement, kerb: (av ?? b).kerb, walk: (av ?? b).walk, frontage: (av ?? b).frontage } };
  };
  const rc = Math.hypot(p[0] - cc.c[0], p[1] - cc.c[1]), ring = (): At | null => (rc >= cc.ring[1] - 0.01 ? mk({ class: 'district' }, rc - cc.ring[1], 'crown-circus') : null);
  if (edge === 'circus') return ring();
  const own = ownStreet(c, edge), [kind, id] = own.split(':');
  if ((kind === 'primary' || kind === 'district' || kind === 'access') && id) { const r = x.by.get(id); if (r) { const dl = x.roads.nearest(p, 40).get(id); if (dl !== undefined) return mk(r, dl - r.w / 2, id); } }
  if (kind === 'circus') return ring();
  let best: At | null = null;
  for (const [id2, dl] of x.roads.nearest(p, 40)) { const r = x.by.get(id2)!, dk = dl - r.w / 2; if (!best || dk < best.dk) best = mk(r, dk, id2); }
  // a point on the circus pavement belongs to the ring unless a road is nearer its kerb
  if (rc >= cc.ring[1] - 0.01 && rc <= cc.pavement + 0.01 && (!best || best.dk > rc - cc.ring[1])) return ring();
  return best;
}

/** Why a footprint stands in a clear walking zone (SF-1): the road's walk zone, a mall's walk band or a walk's clear middle. '' if it does not. */
export function walkBlock(c: Ctx, pts: V[], edge: string): string {
  const S = c.S;
  for (const p of pts) {
    const f = c.plan.foot.find(q => lineDist(p, q.pts) <= q.w / 2 + 0.005);
    if (f) {
      const off = lineDist(p, f.pts);
      if (f.kind === 'mall' ? off > S.sections.mall.bands.walk[0] + TOL && off < S.sections.mall.bands.walk[1] - TOL : off < f.w / 2 - S.sections.walk.edgeBand - TOL) return `${f.id}'s walk zone`;
      continue;
    }
    const r = roadAt(c, p, edge); if (!r || r.dk < -0.01 || r.dk > r.sec.pavement) continue;
    if (r.dk > r.sec.kerb + TOL && r.dk < r.sec.kerb + r.sec.walk - TOL) return `${r.road}'s walk zone (${r.dk.toFixed(1)} m from the kerb)`;
  }
  return '';
}

/** Why a footprint stands on the tram's own space (SF-3): a reservation or a track's swept envelope. '' if it does not. */
export function tramBlock(c: Ctx, pts: V[]): string {
  const sw = c.plan.standards.tram.trackCentres / 2 + 0.5, x = indexes(c);
  for (const p of pts) {
    for (const [id, dl] of x.roads.nearest(p, 20)) { const r = x.by.get(id)!; if (r.reservation && dl < r.reservation / 2 - TOL) return `${r.id}'s reservation`; }
    if (x.tram.dist(p, sw + 1) < sw - TOL) return 'a track\'s swept envelope';
  }
  return '';
}

/** Why a tree stands too near the sky rail beam (SF-4): its pit inside the envelope's plan footprint, or its canopy within 5 m (3D) of the envelope. */
export function beamBlock(c: Ctx, at: V, pit: V[], variant: string | undefined, z: number): string {
  const env = c.S.skyrailEnvelope as { half: number; underside: number }, sp = speciesOf(c.S, variant) as { crownBase: number; top: number; spread: number }, x = indexes(c);
  if (pit.some(p => x.sky.dist(p, env.half + 1) < env.half - TOL)) return 'its pit inside the beam envelope';
  const gap = x.sky.dist(at, 60), h = Math.max(0, gap - env.half - sp.spread / 2), v = Math.max(0, env.underside - (z + sp.top), z + sp.crownBase - c.plan.standards.skyrail.height), d3 = Math.hypot(h, v);
  return d3 < 5 - TOL ? `canopy ${d3.toFixed(1)} m from the beam` : '';
}

const qe = new WeakMap<object, [V, V][]>();
/** The quay edges that face water (SF-14): a quay boundary segment with water just outside it and no quay. */
export function quayEdges(c: Pick<Ctx, 'plan' | 'd'>): [V, V][] {
  let r = qe.get(c.d); if (r) return r;
  r = [];
  const inside = (p: V, ring: V[]) => { let k = false; for (let i = 0, j = ring.length - 1; i < ring.length; j = i++) if ((ring[i]![1] > p[1]) !== (ring[j]![1] > p[1]) && p[0] < ((ring[j]![0] - ring[i]![0]) * (p[1] - ring[i]![1])) / (ring[j]![1] - ring[i]![1]) + ring[i]![0]) k = !k; return k; };
  for (const q of c.plan.quay) q.forEach((a, i) => {
    const b = q[(i + 1) % q.length]!, m: V = [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2], len = Math.hypot(b[0] - a[0], b[1] - a[1]), n: V = [(b[1] - a[1]) / len, -(b[0] - a[0]) / len];
    for (const g of [1, -1]) { const t: V = [m[0] + n[0] * 0.6 * g, m[1] + n[1] * 0.6 * g]; if (c.plan.water.some(wp => inside(t, wp)) && !c.plan.quay.some(qq => inside(t, qq))) { r!.push([a, b]); break; } }
  });
  qe.set(c.d, r);
  return r;
}
/** Why a footprint is too near the water's edge (SF-14): within 1.5 m of a quay edge. '' if it is not. */
export function quayBlock(c: Pick<Ctx, 'plan' | 'd'>, pts: V[]): string {
  for (const [a, b] of quayEdges(c)) for (const p of pts) if (segDist(p, a, b) < 1.5 - TOL) return 'the quay edge';
  return '';
}
