// Kerb-to-kerb crossings (streetscape.crossings.strip; Sky's walk of 2026-10-14, note 1). dressRoads puts a landing patch at the
// ends of every district and access plot edge, which left 82 of 168 with nothing opposite. A crossing is now one thing: two
// landings on opposite kerbs of one road and the strip across the carriageway between them, a refuge where the strip crosses a
// median of refuge width. Landings that already stand opposite each other (within `stagger` along the road) are paired; a lone
// landing gets its partner on the opposite pavement where the spot is free (SF-2 keeps its 2 m, the solids and keep-clears
// hold) and is dropped, with its bollards and beacons, where it cannot. A pair whose strip would cross a tram track or a bay
// the plan does not allow is dropped. Tram stop and station bank patches are strip-only crossings: the way to the platform.
import type { Crossing, StreetItem } from '../../../../contracts/world/world.schema';
import { type V, add, dist, dot, inPoly, mul, polyline, segDist, segHit, sub } from '../../../plan/geom';
import { boxRing, quayBlock, roadAt, samples9 } from '../../../plan/street-zones';
import { type Ctx, r3, rv } from '../common';
import { idOf, kindOf, plotRing } from '../edges';
import { edgeN } from '../geo';
import type { Landing, Layer } from './kit';
import { landing } from './roads';

type Edge = { key: string; a: V; u: V; len: number; n: V; road: string; cls: string; civic: boolean };
type Pair = { a: Landing; b: Landing; ka: V; kb: V };
const QUARTER = Math.PI / 2;

/** The district and access plot edges, by `<plot>/e<i>` (the street item's `edge`). Civic plots are listed: the pavement is theirs, but nothing is placed on it. */
function edgesOf(ctx: Ctx): Map<string, Edge> {
  const out = new Map<string, Edge>();
  for (const plot of ctx.d.plots) {
    const ring = plotRing(ctx, plot), n = ring.p.length;
    ring.p.forEach((a, i) => {
      const cls = kindOf(ring.c[i]!); if (cls !== 'district' && cls !== 'access') return;
      const b = ring.p[(i + 1) % n]!, len = dist(a, b);
      out.set(`${plot.id}/e${i}`, { key: `${plot.id}/e${i}`, a, u: [(b[0] - a[0]) / len, (b[1] - a[1]) / len], len, n: edgeN(a, b), road: idOf(ring.c[i]!), cls, civic: !!plot.civic });
    });
  }
  return out;
}

/** Convex subject clipped to a convex clip polygon (Sutherland-Hodgman). */
export function clipConvex(subject: V[], clip: V[]): V[] {
  const sgn = clip.reduce((t, p, i) => t + p[0] * clip[(i + 1) % clip.length]![1] - clip[(i + 1) % clip.length]![0] * p[1], 0) >= 0 ? 1 : -1;
  let out = subject;
  for (let i = 0; i < clip.length && out.length; i++) {
    const a = clip[i]!, b = clip[(i + 1) % clip.length]!, side = (p: V) => sgn * ((b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0])), inp = out; out = [];
    inp.forEach((p, j) => {
      const q = inp[(j + 1) % inp.length]!, sp = side(p), sq = side(q);
      if (sp >= 0) out.push(p);
      if ((sp >= 0) !== (sq >= 0)) out.push([p[0] + (q[0] - p[0]) * sp / (sp - sq), p[1] + (q[1] - p[1]) * sp / (sp - sq)]);
    });
  }
  return out;
}
export const areaOf = (p: V[]) => Math.abs(p.reduce((t, a, i) => t + a[0] * p[(i + 1) % p.length]![1] - p[(i + 1) % p.length]![0] * a[1], 0)) / 2;

/** True if two rings overlap (a vertex inside the other, or an edge crossing an edge). */
export function overlaps(a: V[], b: V[]): boolean {
  const ins = (p: V, r: V[]) => { let c = false; for (let i = 0, j = r.length - 1; i < r.length; j = i++) if ((r[i]![1] > p[1]) !== (r[j]![1] > p[1]) && p[0] < ((r[j]![0] - r[i]![0]) * (p[1] - r[i]![1])) / (r[j]![1] - r[i]![1]) + r[i]![0]) c = !c; return c; };
  if (a.some(p => ins(p, b)) || b.some(p => ins(p, a))) return true;
  return a.some((p, i) => b.some((q, j) => segHit(p, a[(i + 1) % a.length]!, q, b[(j + 1) % b.length]!)));
}

/** The shortest distance from a ring to a polyline (0 where they cross or the line runs inside the ring). */
export function ringToLine(ring: V[], line: V[]): number {
  let m = Infinity;
  for (let i = 1; i < line.length; i++) {
    const c = line[i - 1]!, d = line[i]!;
    for (let j = 0; j < ring.length; j++) {
      const a = ring[j]!, b = ring[(j + 1) % ring.length]!;
      m = segHit(a, b, c, d) ? 0 : Math.min(m, segDist(a, c, d), segDist(b, c, d), segDist(c, a, b), segDist(d, a, b));
    }
  }
  return line.some(p => inPoly(p, ring)) ? 0 : m;
}

/** Why a strip may not stand: it crosses a tram track's swept envelope (the plan's SF-3 envelope: centre ± trackCentres/2 + 0.5) or a bay. '' if it may. */
export function stripBlock(strip: V[], tracks: V[][], envelope: number, bays: V[][]): string {
  if (tracks.some(t => ringToLine(strip, t) < envelope - 0.011)) return 'a tram track';
  return bays.some(b => overlaps(strip, b)) ? 'a bay' : '';
}

/** The strip between two kerb points: `width` wide, from kerb to kerb. */
export const stripRing = (ka: V, kb: V, width: number): V[] => boxRing([(ka[0] + kb[0]) / 2, (ka[1] + kb[1]) / 2], dist(ka, kb), width, Math.atan2(kb[1] - ka[1], kb[0] - ka[0])).map(rv);

/** What pairCrossings did, for the export's log: kerb landings found, pairs standing already, partners added, and each landing dropped with the reason. */
export type CrossingLog = { landings: number; paired: number; added: number; dropped: string[] };

export function pairCrossings(ctx: Ctx, L: Layer): { crossings: Crossing[]; log: CrossingLog } {
  const std = ctx.S.crossings.strip as { stagger: number; refuge: number; control?: Record<string, 'zebra' | 'signal'>; across?: { track?: string[]; bay?: string[] } }, W = ctx.S.crossings.w as number;
  const edges = edgesOf(ctx), roads = new Map(ctx.d.roads.map(r => [r.id, r])), items = new Map(L.items.map(x => [x.id, x]));
  const info = (l: Landing) => { // the landing's edge, and its place on the kerb line
    const it = items.get(l.id)!, e = edges.get(it.edge)!;
    return { it, e, k: add(e.a, mul(e.u, dot(sub(it.at, e.a), e.u))) };
  };
  // a strip may not cross a tram track's swept envelope or a bay unless the plan allows it for kerb crossings
  const envelope = ctx.plan.standards.tram.trackCentres / 2 + 0.5, tracks = ctx.d.tram.map(p => polyline(p.segs, 2) as V[]), bays = ctx.civ.bays.map(b => b.poly);
  const across = (why: string) => (why === 'a tram track' ? std.across?.track : std.across?.bay)?.includes('kerb');
  const blocked = (ka: V, kb: V) => { const why = stripBlock(stripRing(ka, kb, W), tracks, envelope, bays); return why && !across(why) ? `its strip crosses ${why}` : ''; };
  const opposite = (ea: Edge, eb: Edge) => ea.road === eb.road && dot(ea.n, eb.n) < -0.7;
  const fits = (road: string, ka: V, kb: V, ea: Edge) => Math.abs(dist(ka, kb) - roads.get(road)!.w) <= 1.5 && Math.abs(dot(sub(kb, ka), ea.u)) <= std.stagger;

  // 1. landings that already stand opposite each other
  const pairs: Pair[] = [], taken = new Set<string>(), lone: Landing[] = [];
  const own = L.landings.filter(l => items.has(l.id) && edges.has(items.get(l.id)!.edge));
  for (const a of own) {
    if (taken.has(a.id)) continue;
    const ia = info(a); let best: { b: Landing; kb: V; t: number } | undefined;
    for (const b of own) {
      if (b === a || taken.has(b.id)) continue;
      const ib = info(b); if (!opposite(ia.e, ib.e) || !fits(a.road, ia.k, ib.k, ia.e)) continue;
      const t = Math.abs(dot(sub(ib.k, ia.k), ia.e.u)); if (!best || t < best.t) best = { b, kb: ib.k, t };
    }
    if (best) { taken.add(a.id); taken.add(best.b.id); pairs.push({ a, b: best.b, ka: ia.k, kb: best.kb }); } else lone.push(a);
  }

  // 2. a partner for each lone landing: the nearest free spot on an opposite plot edge of the same road, within the stagger
  const drop = new Map<string, string>(), noted: string[] = [], pairs0 = pairs.length;
  let added = 0;
  const offsets = [0]; for (let o = 0.25; o <= std.stagger + 1e-9; o += 0.25) offsets.push(o, -o); // nearest the mirror point first
  for (const a of lone) {
    const ia = info(a), road = roads.get(a.road)!; let found: Pair | undefined, seen = false, why = '';
    for (const e of edges.values()) {
      if (found) break;
      if (e.civic || !opposite(ia.e, e)) continue;
      const s0 = dot(sub(ia.k, e.a), e.u);
      for (const o of offsets) {
        const s = s0 + o; if (s < 2 || s > e.len - 2) continue;
        const kb = add(e.a, mul(e.u, s)); if (!fits(a.road, ia.k, kb, ia.e)) continue;
        if ((why = blocked(ia.k, kb))) continue;
        seen = true;
        const yaw = Math.atan2(e.u[1], e.u[0]), at = (q: number, d: number): V => [e.a[0] + e.u[0] * q - e.n[0] * d, e.a[1] + e.u[1] * q - e.n[1] * d];
        const ring = boxRing(at(s, 0.9), 1.8, W, yaw + QUARTER);
        if (!L.fixed(at(s, 0.9), 2.2) || !L.clearOf(ring, at(s, 0.9), 2) || quayBlock(ctx, samples9(ring))) continue;
        if (!landing(ctx, L, at, s, yaw, e.key, e.cls, e.road)) continue;
        found = { a, b: L.landings[L.landings.length - 1]!, ka: ia.k, kb };
        break;
      }
    }
    if (found) { pairs.push(found); added++; }
    else drop.set(a.id, seen ? 'the spot opposite is taken' : why || `no pavement opposite on ${road.name}`);
  }

  // 3. the pairs that stood already: no strip across a track or a bay
  const ok: Pair[] = [];
  for (const p of pairs) {
    const why = blocked(p.ka, p.kb);
    if (why) { for (const l of [p.a, p.b]) drop.set(l.id, why); } else ok.push(p);
  }

  // 4. the crossings: kerb pairs in landing order, then the stop and bank patches
  const out: Crossing[] = [], n = new Map<string, number>(), medians = ctx.d.zones.filter(z => z.kind === 'median' && z.poly);
  const nextId = (road: string) => { const k = n.get(road) ?? 0; n.set(road, k + 1); return `crossing/${road}/${k}`; };
  const order = new Map(L.landings.map((l, i) => [l.id, i]));
  for (const p of [...ok].sort((x, y) => order.get(x.a.id)! - order.get(y.a.id)!)) {
    const strip = stripRing(p.ka, p.kb, W), refuges = medians
      .filter(z => dist(z.poly![1]!, z.poly![2]!) >= std.refuge - 1e-9).map(z => clipConvex(strip, z.poly!)).filter(r => areaOf(r) > 0.25).map(r => r.map(rv));
    const control = std.control?.[p.a.cls];
    out.push({ id: nextId(p.a.road), kind: 'kerb', road: p.a.road, items: [p.a.id, p.b.id], line: [rv(p.ka), rv(p.kb)], strip, width: W, ...(control ? { control } : {}), refuges });
  }
  const landed = new Set(L.landings.map(l => l.id));
  for (const it of L.items.filter(x => x.kind === 'crossing' && !landed.has(x.id))) {
    const u: V = [Math.cos(it.yaw), Math.sin(it.yaw)], h = it.size[0] / 2, road = roadAt(ctx, it.at, it.edge)?.road ?? '';
    const kind = it.edge.startsWith('stop/') ? 'stop' : 'bank';
    out.push({ id: nextId(it.edge), kind, road, items: [it.id], line: [rv(sub(it.at, mul(u, h))), rv(add(it.at, mul(u, h)))], strip: boxRing(it.at, it.size[0], it.size[1], it.yaw).map(rv), width: r3(it.size[1]), refuges: [] });
  }

  // 5. what could not be paired goes, with the bollards and beacons that flanked it
  const gone = new Set<string>();
  for (const l of L.landings) {
    const why = drop.get(l.id); if (!why) continue;
    gone.add(l.id); for (const f of l.flank) gone.add(f);
    noted.push(`${l.id}: ${why}`);
  }
  L.items = L.items.filter((x: StreetItem) => !gone.has(x.id));
  return { crossings: out, log: { landings: own.length, paired: pairs0, added, dropped: noted } };
}
