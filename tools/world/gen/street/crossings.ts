// Kerb-to-kerb crossings (streetscape.crossings.strip; Sky's walk of 2026-10-14, note 1). dressRoads puts a landing patch at the
// ends of every district and access plot edge, which left 82 of 168 with nothing opposite. A crossing is now one thing: two
// landings on opposite kerbs of one road and the strip across the carriageway between them, a refuge where the strip crosses a
// median of refuge width. Landings that already stand opposite each other (within `stagger` along the road) are paired; a lone
// landing gets its partner on the opposite pavement where the spot is free (SF-2 keeps its 2 m, the solids and keep-clears
// hold) and is dropped, with its bollards and beacons, where it cannot. A pair whose strip would cross a tram track or a bay
// the plan does not allow is dropped. Tram stop and station bank patches are strip-only crossings: the way to the platform.
// Brief 009: a crossing lays no slab. The patches the pairing works with (a landing's footprint, SF-2's keep-clear) leave the street layer and become the crossing's `landings`; what is drawn is its parts
// (parts.ts: light bars or dots, signal orbs, kerb studs, inlay arcs), placed from the plan's standard.
import type { Connector, Crossing, Landing as Foot, StreetItem, World } from '../../../../contracts/world/world.schema';
import { type V, add, dist, dot, inPoly, mul, norm, polyline, segDist, segHit, sub } from '../../../plan/geom';
import { boxRing, quayBlock, ringGap, roadAt, samples9 } from '../../../plan/street-zones';
import { type Ctx, r3, r4, rv } from '../common';
import { idOf, kindOf, plotRing } from '../edges';
import { bbox, edgeN } from '../geo';
import type { Landing, Layer } from './kit';
import { ROAD_Z, lawn, levelAt, onRoad } from './ground';
import { type CoverageLog, type Pair, acrossRoad, armSeeds, dedupe, islandSeeds, mallSeeds, midBlockGaps, midOffsets, place, rulesOf, variants } from './coverage';
import { type End, type Site, bandGap, lookOf, partsOf } from './parts';
import { landing } from './roads';
import { toPlanFn } from '../../../plan/civic';
import type { Dressing } from '../../civic/dress';

type Edge = { key: string; a: V; u: V; len: number; n: V; road: string; cls: string; civic: boolean };
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

/** The landings of a world: a kerb crossing's two ends and a stop or bank crossing's one patch (its keep-clear footprints, not drawn), or, in a plan without streetscape.crossings.strip, the landing slabs that are street items. */
export const landingsOf = (w: Pick<World, 'crossings' | 'street'>): Foot[] => w.crossings ? w.crossings.flatMap(c => c.landings) : w.street.filter(s => s.kind === 'crossing');

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

/** What pairCrossings did, for the export's log: kerb landings found, pairs standing already, partners added, each landing dropped with the reason, and each signal orb left out for want of clear ground under its halo. */
export type CrossingLog = { landings: number; paired: number; added: number; dropped: string[]; skipped: string[]; coverage?: CoverageLog };

/** The landings the civic block names (`dressing.crossings.at`, block/4), on its own kerb: the patch alone, no bollards or beacons (the block states none). They are placed after
 *  every district item so no id or seeded choice moves, and pair with the district landing the generator dropped for lack of a partner (pairCrossings, step 1). */
export function civicLandings(ctx: Ctx, L: Layer): string[] {
  const at = (ctx.civ.dressing as Dressing | undefined)?.crossings.at ?? [], toPlan = toPlanFn(ctx.plan.civic), edges = [...edgesOf(ctx).values()], placed: string[] = [];
  for (const c of at) {
    const k = toPlan(c.at), e = edges.find(x => x.key.startsWith(`${c.plot}/e`) && x.road === c.road && segDist(k, x.a, add(x.a, mul(x.u, x.len))) < 0.05);
    if (!e) throw new Error(`civic crossing ${c.id}: no edge of ${c.plot} on ${c.road} through ${c.at.join(', ')}`);
    const inward = sub(toPlan([c.at[0] + c.inward[0], c.at[1] + c.inward[1]]), k);
    if (dot(inward, e.n) > -0.9) throw new Error(`civic crossing ${c.id}: inward does not run from the kerb into the plot`);
    const s = dot(sub(k, e.a), e.u), pt = (q: number, d: number): V => [e.a[0] + e.u[0] * q - e.n[0] * d, e.a[1] + e.u[1] * q - e.n[1] * d];
    if (landing(ctx, L, pt, s, Math.atan2(e.u[1], e.u[0]), e.key, e.cls, e.road, true, false, `street/${e.key}/crossing-${c.id}`)) placed.push(L.items[L.items.length - 1]!.id);
  }
  return placed;
}

/** A station bank whose patch lies on lawn (Sphere Gardens, left bank: 11.5 m out from the tower, 20 m from any street): a landing stands on pavement, a path or a platform, so the
 *  patch moves to the nearest such spot at the foot of the tower that the furniture left free (1 m off the tower, 2 m clear of the rest, off the quay). It keeps its id and
 *  its place in the list, so nothing placed with it moves. None free: it stays, and SF-20 names it. */
export function bankFeet(ctx: Ctx, L: Layer, towers: Connector[]): void {
  const W = ctx.S.crossings.w as number;
  for (const t of towers) {
    const i = L.items.findIndex(x => x.kind === 'crossing' && x.edge === t.host), it = L.items[i];
    if (!it || !samples9(boxRing(it.at, it.size[0], it.size[1], it.yaw)).some(p => lawn(ctx, p))) continue;
    const bb = bbox(t.ring), c: V = [(bb[0] + bb[1]) / 2, (bb[2] + bb[3]) / 2], found: { p: V; gap: number; dk: number }[] = [];
    for (let dx = -14; dx <= 14; dx += 0.5) for (let dy = -14; dy <= 14; dy += 0.5) {
      const p: V = [c[0] + dx, c[1] + dy], ring = boxRing(p, it.size[0], W, it.yaw), gap = ringGap(ring, t.ring);
      if (gap < 1 || samples9(ring).some(q => lawn(ctx, q)) || quayBlock(ctx, samples9(ring)) || !L.fixed(p, 2) || !L.clearOf(ring, p, 2)) continue;
      found.push({ p, gap, dk: roadAt(ctx, p, t.host)?.dk ?? 99 });
    }
    const best = found.sort((a, b) => a.gap - b.gap || a.dk - b.dk || a.p[0] - b.p[0] || a.p[1] - b.p[1])[0];
    if (best) L.items[i] = { ...it, at: rv(best.p), z: r3(levelAt(ctx, best.p)) };
  }
}

/** A street item as the landing it was: the footprint alone (its look is gone with the slab). */
const landingOf = (it: StreetItem): Foot => ({ id: it.id, edge: it.edge, at: it.at, yaw: it.yaw, z: it.z, size: it.size });

export function pairCrossings(ctx: Ctx, L: Layer, civicIds: string[] = []): { crossings: Crossing[]; log: CrossingLog; parts: StreetItem[] } {
  const std = ctx.S.crossings.strip as { stagger: number; refuge: number; control?: Record<string, 'signal'>; across?: { track?: string[]; bay?: string[] } }, W = ctx.S.crossings.w as number, look = lookOf(ctx.S.crossings.strip);
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

  // 3b. the coverage rules (streetscape.crossings.coverage.rules; coverage.ts): twins go, then the crossings the plot edges do not give are placed after the furniture, so no existing id moves
  const rules = rulesOf(ctx), cov: CoverageLog = { dedupe: [], sites: {}, skipped: [], moved: [] }, lateIds = new Set<string>(civicIds);
  const twinPairs: Pair[] = []; // the twins the dedupe rule drops: they keep their crossing numbers, so no other crossing's id moves
  if (rules) {
    const edgeLen = (p: Pair) => Math.max(...[p.a, p.b].map(l => edges.get(items.get(l.id)?.edge ?? '')?.len ?? 0));
    const twins = dedupe(ctx, ok.filter(p => !civicIds.includes(p.a.id) && !civicIds.includes(p.b.id)), edgeLen, rules);
    for (const [p, why] of twins) { twinPairs.push(p); for (const l of [p.a, p.b]) drop.set(l.id, why); cov.dedupe.push(`${p.a.road} at ${p.ka.map(v => v.toFixed(0)).join(',')}: ${why}`); }
    for (let i = ok.length - 1; i >= 0; i--) if (twins.has(ok[i]!)) ok.splice(i, 1);
    const sites: Pair[] = [], reach = rules.reach ?? 4;
    let blockedWhy = '';
    /** Place a seed at the nearest of its variants where its strip may stand and both landings are free; failing that, at the nearest where the furniture can be moved out of the way. */
    const put = (seed: import('./coverage').Seed): boolean => {
      let why = '';
      for (const claim of [false, true]) for (const v of variants(ctx, seed, reach)) {
        if ((blockedWhy = blocked(v.ka, v.kb))) { why ||= blockedWhy; continue; }
        const r = place(ctx, L, v, W, claim);
        if ('why' in r) { why ||= r.why; continue; }
        sites.push(r.pair); lateIds.add(r.pair.a.id); lateIds.add(r.pair.b.id); cov.sites[seed.rule] = (cov.sites[seed.rule] ?? 0) + 1; cov.moved.push(...r.moved);
        return true;
      }
      cov.skipped.push(`${seed.id}: ${why} (nor within ${reach} m either way)`);
      return false;
    };
    for (const seed of [...armSeeds(ctx, rules), ...islandSeeds(ctx, rules), ...mallSeeds(ctx, rules)]) put(seed);
    // mid-block: the gaps between what stands now (kerb pairs, sites, and the stop and bank patches the platforms give), one site at the middle of each wide gap
    const landedIds = new Set(L.landings.map(l => l.id)), mids = (ps: Pair[]) => ps.map(p => ({ road: p.a.road, mid: [(p.ka[0] + p.kb[0]) / 2, (p.ka[1] + p.kb[1]) / 2] as V }));
    const patches = L.items.filter(x => x.kind === 'crossing' && !landedIds.has(x.id)).map(x => ({ road: roadAt(ctx, x.at, x.edge)?.road ?? '', mid: x.at }));
    for (const g of midBlockGaps(ctx, rules, [...mids(ok), ...mids(sites), ...patches])) {
      const mb = rules.midBlock!;
      let done = false;
      for (const claim of [false, true]) {
        for (const o of midOffsets(mb.reach)) {
          if (g.s + o <= g.from + 6 || g.s + o >= g.to - 6) continue; // never nearer a neighbour than 6 m
          const seed = acrossRoad(ctx, 'mid', `mid/${g.road}/${Math.round(g.s)}`, g.road, g.s + o, `${ctx.d.roads.find(r => r.id === g.road)!.name}, the middle of a ${Math.round(g.to - g.from)} m gap`);
          if (!seed || blocked(seed.ka, seed.kb)) continue;
          const r = place(ctx, L, seed, W, claim);
          if ('why' in r) continue;
          sites.push(r.pair); lateIds.add(r.pair.a.id); lateIds.add(r.pair.b.id); cov.sites.mid = (cov.sites.mid ?? 0) + 1; cov.moved.push(...r.moved); done = true; break;
        }
        if (done) break;
      }
      if (!done) cov.skipped.push(`mid/${g.road}/${Math.round(g.s)}: no spot within ${mb.reach} m of the middle of the gap where both landings stand free`);
    }
    ok.push(...sites);
  }

  // 4. the crossings: kerb pairs in landing order, then the stop and bank patches; each with the parts the plan's standard gives it
  const out: Crossing[] = [], parts: StreetItem[] = [], skipped: string[] = [], n = new Map<string, number>(), zones = ctx.d.zones.filter(z => z.kind === 'median'), medians = zones.filter(z => z.poly);
  const nextId = (road: string) => { const k = n.get(road) ?? 0; n.set(road, k + 1); return `crossing/${road}/${k}`; };
  const order = new Map(L.landings.map((l, i) => [l.id, i])), placed = new Map(L.items.map(x => [x.id, x])); // placed: with the partners step 2 added
  const isCivic = (p: Pair) => lateIds.has(p.a.id) || lateIds.has(p.b.id); // civic pairs and the coverage sites number after the district's, so no district crossing id moves
  const bands: V[][] = [], site: Site = { // the arcs placed so far claim their ground: a later crossing's arc whose band would overlap one is left out (its corner is already rounded)
    clear: (ring, at) => L.clearGround(ring, at, 0.001), pave: p => !lawn(ctx, p) && !onRoad(ctx, p),
    take: (pts, w) => { if (bands.some(b => bandGap(b, pts) < w - 0.011)) return false; bands.push(pts); return true; },
  };
  const eyed = new Set<string>(); // landings with a signal eye over them: the eye is the signal, so their beacons go (brief 009, Aster); the bollards stay
  const dress = (x: Omit<Crossing, 'walk'>, ends: End[], level: number, refugeEdges: string[] = []) => {
    const ps = partsOf(x, look, level, ends, refugeEdges, ctx.pav, site, skipped), walk = r4(Math.atan2(x.line[1][1] - x.line[0][1], x.line[1][0] - x.line[0][0]));
    out.push({ ...x, items: ps.map(q => q.id), walk }); parts.push(...ps);
    ends.forEach((e, i) => { if (ps.some(q => q.id === `${x.id}/signal/${'ab'[i]}`)) eyed.add(e.landing.id); }); // a signal eye stands at this kerb end
  };
  for (const p of [...ok, ...twinPairs].sort((x, y) => +isCivic(x) - +isCivic(y) || order.get(x.a.id)! - order.get(y.a.id)!)) {
    if (twinPairs.includes(p)) { nextId(p.a.road); continue; } // numbered, never built
    const strip = stripRing(p.ka, p.kb, W), refuged = medians.filter(z => dist(z.poly![1]!, z.poly![2]!) >= std.refuge - 1e-9).map(z => ({ z, ring: clipConvex(strip, z.poly!) })).filter(r => areaOf(r.ring) > 0.25);
    const control = std.control?.[p.a.cls], its = [placed.get(p.a.id)!, placed.get(p.b.id)!], ks = [p.ka, p.kb];
    const ends: End[] = its.map((it, i) => ({ landing: landingOf(it), k: rv(ks[i]!), inward: norm(sub(it.at, ks[i]!)) }));
    dress({ id: nextId(p.a.road), kind: 'kerb', road: p.a.road, items: [], landings: ends.map(e => e.landing), line: [rv(p.ka), rv(p.kb)], strip, width: W, ...(control ? { control } : {}), refuges: refuged.map(r => r.ring.map(rv)) }, ends, ROAD_Z, refuged.map(r => `median/${zones.indexOf(r.z)}`));
  }
  const landed = new Set(L.landings.map(l => l.id));
  for (const it of L.items.filter(x => x.kind === 'crossing' && !landed.has(x.id))) {
    const u: V = [Math.cos(it.yaw), Math.sin(it.yaw)], h = it.size[0] / 2, road = roadAt(ctx, it.at, it.edge)?.road ?? '';
    const kind = it.edge.startsWith('stop/') ? 'stop' : 'bank';
    dress({ id: nextId(it.edge), kind, road, items: [], landings: [landingOf(it)], line: [rv(sub(it.at, mul(u, h))), rv(add(it.at, mul(u, h)))], strip: boxRing(it.at, it.size[0], it.size[1], it.yaw).map(rv), width: r3(it.size[1]), refuges: [] }, [], it.z);
  }

  // 5. what could not be paired goes, with the bollards and beacons that flanked it
  const gone = new Set<string>();
  for (const l of L.landings) {
    const why = drop.get(l.id); if (!why) continue;
    gone.add(l.id); for (const f of l.flank) gone.add(f);
    noted.push(`${l.id}: ${why}`);
  }
  const byId = new Map(L.items.map(x => [x.id, x]));
  for (const l of L.landings) if (eyed.has(l.id)) for (const f of l.flank) if (byId.get(f)?.kind === 'beacon') gone.add(f);
  L.items = L.items.filter((x: StreetItem) => !gone.has(x.id) && x.kind !== 'crossing'); // no slab: every patch left is now some crossing's landing
  return { crossings: out, log: { landings: own.length, paired: pairs0, added, dropped: noted, skipped, ...(rules ? { coverage: cov } : {}) }, parts };
}
