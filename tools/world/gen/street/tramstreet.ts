// Brief 075, the 1950s tram street (Sky 2026-10-28: "how are 1950s cities with trams? ... the same city design with just flying / 1950s space concepts attached
// to everyday items"; Datum's section: plan standards.streetscape.sections.tramStreet, read whole). What the generator lays on every road a tram line runs on
// (roads[].tram), by rule over plan data only (the tram paths' segments, the road lines and widths, the plot edges, the stops, the crossings, the buildings):
//   setts     granite setts under both tram lanes: a band |offset| <= setts.band[1] (3.6) about each double path's line (a single path: about its own line), wherever
//             the path runs in a carriageway (a road, a junction box, the circus ring's carriageway) on the sheet; a flat per segment (ground.flats, surface `setts`)
//   square    where two lines' paths cross on the flat (T1 x T2 at s_boulevard), the junction box of their two roads in setts
//   rails     Vela's adopted track tiles flush in the setts (ground.pieces, kind `tram-rail`): Tile_TramRail_Flush_12m on straights (every whole 12 m), Tile_TramRail_Flush on curves
//             (the track-bed Tile_TramRail_12m / Tile_TramRail until the flush pair is adopted)
//             and for a straight's remainder (2 m, stretched along by under 5 % so a run closes on whole tiles); rail heads at +-0.5 = the track centre line
//   kerbside  the kerbside strip of each tram street (sections.tramStreet.<class>.kerbside): the Boulevard's parallel bays 6.0 x 2.4 and loading bays (12 m) at
//             trades the frontage catalogue gives a serviceBay; the primaries' 30-degree angle stalls 2.6 x 4.8 with a kerbed palm island (2.0 x 4.8, pavement top) every
//             24 m, one coconut palm on each and a district lamp on alternate ones. No bay within 10 m of a kerb return's tangent, 5 m of a crossing, along a stop's
//             island and 15 m before / 10 m after it (by the kerb's own direction of travel, right-hand traffic). Parked cars are life's (Keel, Pulse): only the bays.
//   wires     none (Sky 2026-10-28, brief 075: "no overhead wires though!"): no span or contact wire, hanger, rosette or span pole anywhere; the trams draw power another
//             way. The plan's sections.tramStreet.wires (and its 'Overhead wires on span wires' text) is ignored here until Datum deletes it; world rule TS-W0
//             (tools/world/rules/overhead.ts) fails an export that carries any wire piece or span item. Vela's TramWire_* pieces are rejected in the catalogue.
//   trams     one parked Tram per line at its first stop (plan order), on the right-hand track facing along the path: presentation dressing (world.dressing), never
//             sim data (the sim reads the plan, the mission and the civic block, never the world export; live trams are Pulse's timetable and Latch's mover).
// A plan without sections.tramStreet gets none of this (a pinned plan replays as it was made). Deterministic: no random choice.
import type { Flat, KerbBay, Mass, StreetItem, ParkedTram } from '../../../../contracts/world/world.schema';
import { type V, type Seg, dist, inPoly, lineDist } from '../../../plan/geom';
import { type Ctx, r3, r4, rv } from '../common';
import { adoptedBy } from '../kitrefs';
import { idOf, kindOf, plotRing } from '../edges';
import { edgeN } from '../geo';
import { returnAt } from '../kerbreturn';
import { avenueVariant, speciesOf } from '../../../plan/trees';
import type { Layer } from './kit';

type Section = { roads: string[]; w: number; kerbside: { w: number }; island?: { span: [number, number] } };
export type TramStreetStd = { band: number; district?: Section; primary?: Section; roads: Set<string> };
export type TramStreetLog = { setts: { flats: number; m: number; squares: number }; rails: { tiles12: number; tiles2: number; m: number; flush: boolean }; kerbside: { parallel: number; angle: number; loading: number; islands: number; palms: number; lamps: number }; trams: number; gaps: { rule: string; where: string; why: string; at?: V }[] };

export function tramStreetStd(ctx: Ctx): TramStreetStd | undefined {
  const t = (ctx.S as any).sections?.tramStreet; if (!t) return undefined;
  const roads = new Set<string>([...(t.district?.roads ?? []), ...(t.primary?.roads ?? [])]);
  return { band: t.setts?.band?.[1] ?? 3.6, district: t.district, primary: t.primary, roads }; // t.wires is never read (no overhead, Sky 2026-10-28)
}

/** A point, the unit tangent and the left normal at s along a segment. */
function at(sg: Seg, s: number): { p: V; t: V; n: V } {
  if (sg.kind === 'line') { const u: V = [(sg.b[0] - sg.a[0]) / sg.len, (sg.b[1] - sg.a[1]) / sg.len]; return { p: [sg.a[0] + u[0] * s, sg.a[1] + u[1] * s], t: u, n: [-u[1], u[0]] }; }
  const sgn = Math.sign(sg.sweep) || 1, phi = sg.a0 + sgn * s / sg.r, t: V = [-Math.sin(phi) * sgn, Math.cos(phi) * sgn];
  return { p: [sg.c[0] + sg.r * Math.cos(phi), sg.c[1] + sg.r * Math.sin(phi)], t, n: [-t[1], t[0]] };
}
const off = (q: { p: V; n: V }, d: number): V => [q.p[0] + q.n[0] * d, q.p[1] + q.n[1] * d];
/** Length of a segment's offset line (left of travel +d). */
const offLen = (sg: Seg, d: number) => (sg.kind === 'line' ? sg.len : (sg.r - (Math.sign(sg.sweep) || 1) * d) * Math.abs(sg.sweep));

/** In a carriageway on the sheet: a road's band, or the circus ring's carriageway (not its island). */
function inCarriageway(ctx: Ctx, p: V): boolean {
  const sh = ctx.plan.sheet; if (p[0] < sh.x[0] || p[0] > sh.x[1] || p[1] < sh.y[0] || p[1] > sh.y[1]) return false;
  const c = ctx.plan.circus, rc = dist(p, c.c);
  if (rc < c.ring[0]) return false;
  if (rc <= c.ring[1]) return true;
  return ctx.d.roads.some(r => !r.gyratory && lineDist(p, r.line) <= r.w / 2 + 1e-6);
}
const onSheet = (ctx: Ctx, p: V) => { const sh = ctx.plan.sheet; return p[0] >= sh.x[0] && p[0] <= sh.x[1] && p[1] >= sh.y[0] && p[1] <= sh.y[1]; };

/** The setts, the flat crossing's square and the rails (ground.flats and ground.pieces). */
export function tramGround(ctx: Ctx, log: TramStreetLog, level: number): { flats: Flat[]; rails: StreetItem[] } | undefined {
  const T = tramStreetStd(ctx); if (!T) return undefined;
  const flats: Flat[] = [], rails: StreetItem[] = [];
  // ---- setts: per segment of each path, the stretches whose samples lie in a carriageway, as a ring (the left offsets, then the right ones back)
  for (const path of ctx.d.tram) {
    const hw = path.double ? T.band : T.band / 2;
    path.segs.forEach((sg, si) => {
      const n = Math.max(1, Math.ceil(sg.len / 1)), pts = Array.from({ length: n + 1 }, (_, k) => at(sg, (sg.len * k) / n));
      let run: typeof pts = [], k = 0;
      const flush = () => {
        if (run.length >= 2) {
          const simple = sg.kind === 'line' ? [run[0]!, run[run.length - 1]!] : run.filter((_, i) => i % 2 === 0 || i === run.length - 1);
          const ring: V[] = [...simple.map(q => rv(off(q, hw))), ...simple.slice().reverse().map(q => rv(off(q, -hw)))];
          flats.push({ id: `ground/tram/setts/${path.line}/${path.id}/${si}.${k++}`, surface: 'setts', ring, level });
          log.setts.flats++; log.setts.m = r3(log.setts.m + dist(run[0]!.p, run[run.length - 1]!.p) * (sg.kind === 'line' ? 1 : 1));
        }
        run = [];
      };
      for (const q of pts) { if (inCarriageway(ctx, q.p)) run.push(q); else flush(); }
      flush();
    });
  }
  // ---- the flat crossing: where two lines' double paths cross, the box of the two roads they run on, in setts
  const doubles = ctx.d.tram.filter(p => p.double);
  for (let i = 0; i < doubles.length; i++) for (let j = i + 1; j < doubles.length; j++) {
    const A = doubles[i]!, B = doubles[j]!; if (A.line === B.line) continue;
    for (const sa of A.segs) for (const sb of B.segs) {
      if (sa.kind !== 'line' || sb.kind !== 'line') continue;
      const x = segX(sa.a, sa.b, sb.a, sb.b); if (!x) continue;
      const ra = roadUnder(ctx, sa, x), rb = roadUnder(ctx, sb, x); if (!ra || !rb) continue;
      const ua: V = [(sa.b[0] - sa.a[0]) / sa.len, (sa.b[1] - sa.a[1]) / sa.len], ub: V = [(sb.b[0] - sb.a[0]) / sb.len, (sb.b[1] - sb.a[1]) / sb.len];
      const ha = rb.w / 2, hb = ra.w / 2; // along A, the box spans B's road; along B, A's road
      const ring: V[] = [[-1, -1], [1, -1], [1, 1], [-1, 1]].map(([p, q]) => rv([x[0] + ua[0] * ha * p! + ub[0] * hb * q!, x[1] + ua[1] * ha * p! + ub[1] * hb * q!]));
      flats.push({ id: `ground/tram/square/${A.line}-${B.line}`, surface: 'setts', ring, level: r3(level + 0.001) });
      log.setts.squares++;
    }
  }
  // ---- rails: per track (a double path: +-trackCentres / 2; a single: its line), per segment
  // Vela's flush tiles (brief 075: Tile_TramRail_Flush / _Flush_12m, rails set in the setts, same gauge and pivots) where adopted at the same lengths; the track-bed tiles
  // (Tile_TramRail / _12m) only until then: a tram street has no separate track bed.
  const f12 = adoptedBy('Tile_TramRail_Flush_12m'), f2 = adoptedBy('Tile_TramRail_Flush'), flush = !!f2 && f2.size[0] === 2 && (!f12 || f12.size[0] === 12);
  if (f2 && !flush) log.gaps.push({ rule: 'TS-rail', where: 'kit', why: `the flush tiles are not 2 m / 12 m long (${f2.size[0]} / ${f12?.size[0]}): the track-bed tiles laid` });
  const t12 = flush ? f12 : adoptedBy('Tile_TramRail_12m'), t2 = flush ? f2 : adoptedBy('Tile_TramRail');
  log.rails.flush = flush;
  if (!t2) { log.gaps.push({ rule: 'TS-rail', where: 'kit', why: 'Tile_TramRail is not adopted: no rails laid' }); return { flats, rails }; }
  const half = ctx.plan.standards.tram.trackCentres / 2;
  let nr = 0;
  const put = (a: { id: string; size: number[] }, p: V, t: V, k: number, edge: string) => {
    if (!onSheet(ctx, p)) return;
    rails.push({ id: `ground/tram/rail-${nr++}`, kind: 'tram-rail', at: rv(p), yaw: r4(Math.atan2(t[1], t[0])), z: r3(levelOf(ctx, p, level)), size: [r3(a.size[0]! * k), r3(a.size[2]!), r3(a.size[1]!)], surface: 'track', edge, sight: 'open', source: 'kit', kit: { id: a.id, scale: [r4(k), 1, 1] } });
    if (a.size[0] === 12) log.rails.tiles12++; else log.rails.tiles2++;
    log.rails.m = r3(log.rails.m + a.size[0]! * k);
  };
  for (const path of ctx.d.tram) for (const d of path.double ? [half, -half] : [0]) path.segs.forEach((sg, si) => {
    const L = offLen(sg, d), edge = `tram/${path.line}/${path.id}`, ptAt = (s: number) => { const q = at(sg, (s / L) * sg.len); return { p: off(q, d), t: q.t }; };
    if (sg.kind === 'line' && t12) {
      // the run in whole 2 m periods (N, all stretched by k = L / 2N: under 1 % on a long straight), six at a time as a 12 m tile, the rest as 2 m tiles
      const N = Math.max(1, Math.round(L / 2)), k = L / (2 * N), n12 = Math.floor(N / 6), n2 = N - 6 * n12;
      for (let i = 0; i < n12; i++) { const q = ptAt((i * 12 + 6) * k); put(t12, q.p, q.t, k, edge); }
      for (let i = 0; i < n2; i++) { const q = ptAt((n12 * 12 + (i + 0.5) * 2) * k); put(t2, q.p, q.t, k, edge); }
      return;
    }
    const n = Math.max(1, Math.round(L / 2)), k = L / (2 * n);
    for (let i = 0; i < n; i++) { const q = ptAt((i + 0.5) * 2 * k); put(t2, q.p, q.t, k, edge); }
    void si;
  });
  return { flats, rails };
}
/** The surface a rail lies on: the setts on a carriageway, the island's ring band (the circus loop) at the pavement top. */
const levelOf = (ctx: Ctx, p: V, setts: number) => (inCarriageway(ctx, p) ? setts : ctx.pav);
function segX(a: V, b: V, c: V, d: V): V | null {
  const r: V = [b[0] - a[0], b[1] - a[1]], s: V = [d[0] - c[0], d[1] - c[1]], den = r[0] * s[1] - r[1] * s[0]; if (Math.abs(den) < 1e-9) return null;
  const t = ((c[0] - a[0]) * s[1] - (c[1] - a[1]) * s[0]) / den, u = ((c[0] - a[0]) * r[1] - (c[1] - a[1]) * r[0]) / den;
  return t > 0 && t < 1 && u > 0 && u < 1 ? [a[0] + r[0] * t, a[1] + r[1] * t] : null;
}
/** The road a straight path segment runs along at x (its line within 0.5 m of the segment, parallel). */
function roadUnder(ctx: Ctx, sg: Seg, x: V) {
  const u: V = [(sg.b[0] - sg.a[0]) / sg.len, (sg.b[1] - sg.a[1]) / sg.len];
  return ctx.d.roads.find(r => !r.gyratory && lineDist(x, r.line) < 0.5 && r.line.some((p, i) => { const q = r.line[i + 1]; if (!q) return false; const v = [q[0] - p[0], q[1] - p[1]], l = Math.hypot(v[0]!, v[1]!) || 1; return Math.abs((u[0] * v[0]! + u[1] * v[1]!) / l) > 0.99 && lineDist(x, [p, q]) < 0.5; }));
}

/** The kerbside strip of every tram street, after the street layer has placed everything else (so no other street id moves). */
export function tramKerbside(ctx: Ctx, L: Layer, log: TramStreetLog, o: { crossings: V[][]; stops: Mass[]; frontages: { street: string; trade: string; a: V; b: V; level: string }[] }): { bays: KerbBay[]; islands: Mass[] } | undefined {
  const T = tramStreetStd(ctx); if (!T) return undefined;
  const bays: KerbBay[] = [], islands: Mass[] = [], z = ctx.pav, S = ctx.S;
  const trades = (ctx.plan.standards as any).frontage?.trades ?? {};
  const loaders = o.frontages.filter(f => f.level === 'street' && trades[f.trade]?.serviceBay === true);
  const nearCrossing = (p: V, g: number) => o.crossings.some(r => inPoly(p, r) || r.some((q, i) => { const b = r[(i + 1) % r.length]!; return segD(p, q, b) < g; }));
  const variant = avenueVariant(S), top = speciesOf(S, variant)?.top ?? 9;
  let nb = 0, ni = 0;
  for (const plot of ctx.d.plots) {
    if (plot.civic) continue;
    const ring = plotRing(ctx, plot), n = ring.p.length;
    for (let i = 0; i < n; i++) {
      const road = idOf(ring.c[i]!), cls = kindOf(ring.c[i]!); if (!T.roads.has(road) || (cls !== 'district' && cls !== 'primary')) continue;
      const sec = cls === 'district' ? T.district : T.primary; if (!sec) continue;
      const a = ring.p[i]!, b = ring.p[(i + 1) % n]!, len = dist(a, b); if (len < 20) continue;
      const u: V = [(b[0] - a[0]) / len, (b[1] - a[1]) / len], nrm = edgeN(a, b), id = `${plot.id}/e${i}`;
      // a point s along the kerb, k m out from the kerb face into the carriageway
      const pt = (s: number, k: number): V => [a[0] + u[0] * s + nrm[0] * k, a[1] + u[1] * s + nrm[1] * k];
      const travel: V = [nrm[1], -nrm[0]], fwd = travel[0] * u[0] + travel[1] * u[1] > 0 ? 1 : -1; // right-hand traffic: the kerb on the driver's right; fwd = +1 if traffic runs a -> b
      const rA = returnAt(ctx, plot.id, a), rB = returnAt(ctx, plot.id, b), s0 = (rA?.t ?? 0) + 10, s1 = len - (rB?.t ?? 0) - 10;
      if (s1 - s0 < 6) continue;
      // blocked stretches along this kerb: stop islands (15 m before, 10 m after, in the traffic's direction)
      const cuts: [number, number][] = [];
      for (const m of o.stops) {
        const ss = m.ring.map(p => (p[0] - a[0]) * u[0] + (p[1] - a[1]) * u[1]), dd = m.ring.map(p => (p[0] - a[0]) * nrm[0] + (p[1] - a[1]) * nrm[1]);
        if (Math.min(...dd) > sec.kerbside.w + 8 || Math.max(...dd) < 0) continue;
        const lo = Math.min(...ss), hi = Math.max(...ss); cuts.push(fwd > 0 ? [lo - 15, hi + 10] : [lo - 10, hi + 15]);
      }
      const free = (sA: number, sB: number, depth: number) => {
        if (sA < s0 || sB > s1) return false;
        if (cuts.some(([x, y]) => sB > x && sA < y)) return false;
        for (let s = sA; s <= sB + 1e-9; s += 0.5) for (const k of [0.2, depth / 2, depth]) if (nearCrossing(pt(s, k), 5)) return false;
        for (const dr of ctx.civ.drives ?? []) for (let s = sA; s <= sB + 1e-9; s += 0.5) if (inPoly(pt(s, 0.2), dr.poly as V[]) || inPoly(pt(s, -0.5), dr.poly as V[])) return false;
        return true;
      };
      const rect = (sA: number, sB: number, k0: number, k1: number): V[] => [pt(sA, k0), pt(sB, k0), pt(sB, k1), pt(sA, k1)].map(rv);
      if (cls === 'district') {
        // the Boulevard: parallel bays 6.0 x 2.4 against the kerb (0.4 m off the traffic lane), a 12 m loading bay at each serviceBay frontage on this kerb
        const depth = 2.4, loads: [number, number][] = [];
        for (const f of loaders) {
          if (f.street !== road && idOf(f.street) !== road) continue;
          const m: V = [(f.a[0] + f.b[0]) / 2, (f.a[1] + f.b[1]) / 2], sm = (m[0] - a[0]) * u[0] + (m[1] - a[1]) * u[1], dm = (m[0] - a[0]) * nrm[0] + (m[1] - a[1]) * nrm[1];
          if (dm > 1 || dm < -14 || sm < s0 + 6 || sm > s1 - 6) continue;
          if (loads.some(([x, y]) => sm + 6 > x && sm - 6 < y) || !free(sm - 6, sm + 6, depth)) continue;
          loads.push([sm - 6, sm + 6]); bays.push({ id: `ground/kerbside/${road}/${nb++}`, kind: 'loading', road, ring: rect(sm - 6, sm + 6, 0, depth), yaw: r4(Math.atan2(u[1], u[0])), level: 0 }); log.kerbside.loading++;
        }
        for (let s = s0; s + 6 <= s1 + 1e-9; s += 6) {
          if (loads.some(([x, y]) => s + 6 > x && s < y)) { const nxt = loads.find(([x, y]) => s + 6 > x && s < y)!; s = nxt[1] - 6; continue; }
          if (!free(s, s + 6, depth)) continue;
          bays.push({ id: `ground/kerbside/${road}/${nb++}`, kind: 'parallel', road, ring: rect(s, s + 6, 0, depth), yaw: r4(Math.atan2(u[1], u[0])), level: 0 }); log.kerbside.parallel++;
        }
        continue;
      }
      // the primaries: a palm island every 24 m (2.0 along x the strip, kerbed at the pavement top), 30-degree stalls between them nosing in with the traffic
      const W = sec.kerbside.w, pitch = 2.6 / Math.sin(Math.PI / 6), foot = 4.8 * Math.cos(Math.PI / 6) + 2.6 * Math.sin(Math.PI / 6);
      const axis: V = [travel[0] * Math.cos(Math.PI / 6) - nrm[0] * Math.sin(Math.PI / 6), travel[1] * Math.cos(Math.PI / 6) - nrm[1] * Math.sin(Math.PI / 6)];
      const across: V = [-axis[1], axis[0]];
      let alt = 0;
      for (let s = s0; s + 2 <= s1 + 1e-9; s += 24) {
        if (!free(s, s + 2, W)) continue;
        const iid = `ground/island/${road}/${ni++}`;
        islands.push({ id: iid, surface: 'pavement', ring: rect(s, s + 2, 0, W), base: 0, height: r3(z) });
        log.kerbside.islands++;
        if (L.add('tree', pt(s + 1, W - 1.0), 0, z, iid, { size: [0.5, 0.5, top], variant, r: 1.5, id: `street/${iid}/palm` })) log.kerbside.palms++;
        else log.gaps.push({ rule: 'TS-island', where: iid, why: 'the palm does not pass the street layer here' });
        if (alt++ % 2 === 0 && L.add('lamp', pt(s + 1, 0.6), Math.atan2(nrm[1], nrm[0]), z, iid, { size: [0.3, 0.3, 8], variant: 'atom-ring', r: 0.6, id: `street/${iid}/lamp` })) log.kerbside.lamps++;
        // the stalls in the 22 m to the next island
        const e0 = s + 2 + 0.3, e1 = Math.min(s + 24, s1) - 0.3, nS = e1 - e0 >= foot ? Math.floor((e1 - e0 - foot) / pitch) + 1 : 0;
        for (let k = 0; k < nS; k++) {
          const sc = e0 + foot / 2 + k * pitch; if (!free(sc - foot / 2, sc + foot / 2, W)) continue;
          const c = pt(sc, W / 2), hl = 2.4, hw = 1.3;
          const ringS: V[] = [[-1, -1], [1, -1], [1, 1], [-1, 1]].map(([p, q]) => rv([c[0] + axis[0] * hl * p! + across[0] * hw * q!, c[1] + axis[1] * hl * p! + across[1] * hw * q!]));
          bays.push({ id: `ground/kerbside/${road}/${nb++}`, kind: 'angle', road, ring: ringS, yaw: r4(Math.atan2(axis[1], axis[0])), level: 0 }); log.kerbside.angle++;
        }
      }
      void id;
    }
  }
  return { bays, islands };
}
const segD = (p: V, a: V, b: V) => { const ab: V = [b[0] - a[0], b[1] - a[1]], l2 = ab[0] * ab[0] + ab[1] * ab[1] || 1, t = Math.max(0, Math.min(1, ((p[0] - a[0]) * ab[0] + (p[1] - a[1]) * ab[1]) / l2)); return Math.hypot(p[0] - a[0] - ab[0] * t, p[1] - a[1] - ab[1] * t); };

/** One parked Tram per line at its first stop (plan order), on the right-hand track of the path, facing along it: presentation dressing only. */
export function parkedTrams(ctx: Ctx, log: TramStreetLog, level: number): ParkedTram[] {
  if (!tramStreetStd(ctx)) return [];
  const a = adoptedBy('Tram'); if (!a) { log.gaps.push({ rule: 'TS-tram', where: 'kit', why: 'Tram is not adopted' }); return []; }
  const out: ParkedTram[] = [], half = ctx.plan.standards.tram.trackCentres / 2, pivot = (a as { pivotOffset?: number[] }).pivotOffset?.[1] ?? 0.108;
  for (const line of ctx.plan.tram as unknown as { id: string; stops?: { id: string; path: string; at: V }[] }[]) {
    const st = line.stops?.[0], path = ctx.d.tram.find(p => p.line === line.id && p.id === st?.path); if (!st || !path) continue;
    const smp = path.samples.reduce((b, x) => (dist(x.p, st.at) < dist(b.p, st.at) ? x : b)), t = smp.dir, n: V = [-t[1], t[0]], d = path.double ? -half : 0;
    const c: V = [smp.p[0] + n[0] * d, smp.p[1] + n[1] * d], p: V = [c[0] - t[0] * pivot, c[1] - t[1] * pivot];
    out.push({ id: `dressing/tram/${line.id}/${st.id}`, line: line.id, stop: st.id, kit: { id: a.id, scale: [1, 1, 1], yaw: r4(Math.atan2(t[1], t[0]) - Math.PI / 2), at: rv(p) }, z: r3(level) });
    log.trams++;
  }
  return out;
}

export const newTramLog = (): TramStreetLog => ({ setts: { flats: 0, m: 0, squares: 0 }, rails: { tiles12: 0, tiles2: 0, m: 0, flush: false }, kerbside: { parallel: 0, angle: 0, loading: 0, islands: 0, palms: 0, lamps: 0 }, trams: 0, gaps: [] });
