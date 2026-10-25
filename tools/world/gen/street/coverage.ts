// Crossing coverage rules (brief 009 Datum's proposal of 2026-10-15; reworked by brief 061, Sky 2026-10-24/25 "there's way too many crossings"): the crossings that stand, as rules over the plan, not a list.
// A plot edge gives no landing (rules.edges false: roads.ts places none), so every crossing is a site some rule calls for. standards.streetscape.crossings.coverage.rules says which:
//   stops      one crossing per tram stop, at the platform end toward the nearer junction or footway end (transit.ts places the patch; siting.ts picks the end)
//   island     one crossing per road into the circus (`per` road): that road's `side` footway line seen leaving the circus, by the radial through the point where it meets the ring's outer edge, to the island
//   foot       a mall or walk that meets or crosses a road continues across it with one crossing, on its axis, or (a mall) on the middle of its walk band, or at the nearest free spot (`reach`)
//   crossroads access lanes meeting a district road at one node (two or more roads of the access class): the district road takes one crossing on its arm before the node, its near edge `kerbReturn.landings.clear` past the
//              side road's mouth (the kerb return's tangent point, or the side of a continuous footway's table: streetscape.continuousFootway)
//   junction   two district roads meeting: each takes one crossing on its arm before the node, unless a tram stop's crossing of that road stands within `stopWithin` m of the node
//   dedupe     two kerb crossings of one road less than `mouth` m apart across a side road's mouth, or `bend` m apart round a bend, are one: the one on the longer plot edge stays
// A site is two landings on the pavement and the strip between their kerbs (a signal crossing like any other: orbs at the kerb ends, a refuge where it crosses a median); it is placed after the
// furniture, so no existing id moves, and left out where either landing has no free spot (named in the log). A strip across a tram track stands where the plan lets a kerb crossing cross
// one (strip.across.track names `kerb`). The civic block's crossings and their partner landings are placed by pairCrossings.
import { type V, add, dist, dot, inPoly, lineDist, mul, norm, sub } from '../../../plan/geom';
import { boxRing, quayBlock, samples9 } from '../../../plan/street-zones';
import type { Ctx } from '../common';
import { rv } from '../common';
import type { Landing, Layer } from './kit';
import { landing } from './roads';
import { type CoverageRules, rulesOf } from './siting';
import { footwayTangents } from './footway';
import { kerbReturnStd, returnsOf } from '../kerbreturn';
import { discRing } from '../box';
import { clearReach, lookOf } from './parts';

const QUARTER = Math.PI / 2;

/** One crossing the rules call for, before it is placed: the kerb points of its two ends (a, b), the road it crosses and that road's class (its control comes from the class). */
export type Seed = {
  id: string; rule: 'island' | 'foot' | 'crossroads' | 'junction'; road: string; cls: string; ka: V; kb: V; why: string;
  /** how the seed moves to find a free spot: along its road (chainage s; `dir` = the one way it may move, away from a kerb return, else either) or round the circus ring (the radial's angle from the ring's centre) */
  via: { s: number; dir?: 1 | -1 } | { ring: V; r0: number; r1: number; angle: number };
  /** seeds of one group are alternatives for one site (a mall's axis, then its walk bands): the first that can stand wins and the others are not tried */
  group?: string;
};
/** Two landings on opposite kerbs and the kerb points the strip runs between (pairCrossings' own type). */
export type Pair = { a: Landing; b: Landing; ka: V; kb: V };

export { type CoverageRules, rulesOf };

/** What the rules did, for the export's log. */
export type CoverageLog = { dedupe: string[]; sites: Record<string, number>; skipped: string[]; /** footway sites an existing crossing serves, `<site>: <crossing>` */ served: string[]; /** furniture ids a site took out of the street */ moved: string[] };

// ---- polylines
/** The point and tangent at chainage s of a polyline. */
export function atChainage(line: V[], s: number): { p: V; t: V } {
  let acc = 0;
  for (let i = 1; i < line.length; i++) {
    const a = line[i - 1]!, b = line[i]!, l = dist(a, b);
    if (acc + l >= s - 1e-9 || i === line.length - 1) { const u = norm(sub(b, a)), f = Math.max(0, Math.min(l, s - acc)); return { p: add(a, mul(u, f)), t: u }; }
    acc += l;
  }
  return { p: line[0]!, t: [1, 0] };
}
/** The chainage of the point of a polyline nearest to p, and how far p is from it. */
export function project(line: V[], p: V): { s: number; d: number } {
  let acc = 0, best = { s: 0, d: Infinity };
  for (let i = 1; i < line.length; i++) {
    const a = line[i - 1]!, b = line[i]!, l = dist(a, b), u = l ? norm(sub(b, a)) : ([1, 0] as V), t = Math.max(0, Math.min(l, dot(sub(p, a), u))), d = dist(p, add(a, mul(u, t)));
    if (d < best.d) best = { s: acc + t, d };
    acc += l;
  }
  return best;
}
export const lengthOf = (line: V[]) => line.reduce((n, p, i) => (i ? n + dist(line[i - 1]!, p) : 0), 0);
export const left = (t: V): V => [-t[1], t[0]];

/** A seed for the road `road` across its width at chainage s: the left kerb is a, the right b. */
export function acrossRoad(ctx: Ctx, rule: Seed['rule'], id: string, road: string, s: number, why: string): Seed | undefined {
  const r = ctx.d.roads.find(x => x.id === road)!;
  if (s < 0 || s > lengthOf(r.line)) return undefined;
  const { p, t } = atChainage(r.line, s), n = left(t);
  return { id, rule, road, cls: r.class, ka: rv(add(p, mul(n, r.w / 2))), kb: rv(add(p, mul(n, -r.w / 2))), why, via: { s } };
}

// ---- the sites the rules give outright
/** The longest kerb return tangent (m) at `node` on road `P`: the returns of the junction that touch P within 40 m (a gate mouth's footway has none; its table's side stands where the tangent point was: footway.ts). */
export function tangentAt(ctx: Ctx, P: { id: string }, node: string): number {
  const at = ctx.plan.nodes[node]!;
  return Math.max(0, footwayTangents(ctx, P.id, node), ...returnsOf(ctx).filter(r => r.convex && !r.nose && r.roads.includes(P.id) && dist(r.at, at) < 40).map(r => r.t));
}
/** One crossing of road `P` on its arm `before` (lower chainage) or `after` node `node`: the near edge stands kerbReturn.landings.clear past the side roads' mouths, i.e. past the kerb return's tangent point (centre = half the widest side road + t + clear + w / 2).
 *  It may only move away from the node (a crossing never slides toward the corner it keeps its clear from). */
function armSeed(ctx: Ctx, rule: 'crossroads' | 'junction', P: { id: string; name: string; line: V[] }, node: string, sides: { w: number; name: string }[], arm: 'before' | 'after'): Seed | undefined {
  const W = ctx.S.crossings.w as number, t = tangentAt(ctx, P, node), half = Math.max(...sides.map(q => q.w)) / 2, reach = half + t + kerbReturnStd(ctx.S).clear + W / 2, s0 = project(P.line, ctx.plan.nodes[node]!).s, sg = arm === 'before' ? -1 : 1;
  const seed = acrossRoad(ctx, rule, `${rule}/${P.id}/${node}/${arm}`, P.id, s0 + sg * reach, `${P.name}, ${reach.toFixed(0)} m ${arm} the node of ${sides.map(q => q.name).join(' and ')}`);
  if (seed) seed.via = { s: (seed.via as { s: number }).s, dir: sg as 1 | -1 };
  return seed;
}
const roadsAtNode = (ctx: Ctx, node: string) => ctx.d.roads.filter(r => r.path.includes(node));

/** Crossroads: where two or more access roads (the rule's `class`es are the district roads that take the crossing; the side roads are the access lanes) meet a district road at one node, the district road takes one crossing on its arm `arm` the node. */
export function crossroadsSeeds(ctx: Ctx, rules: CoverageRules): Seed[] {
  const R = rules.crossroads; if (!R) return [];
  const out: Seed[] = [];
  for (const P of ctx.d.roads.filter(r => R.class.includes(r.class) && !r.gyratory)) for (const node of P.path) {
    const others = roadsAtNode(ctx, node).filter(q => q !== P), lanes = others.filter(q => q.class === 'access'); if (lanes.length < 2) continue;
    const seed = armSeed(ctx, 'crossroads', P, node, others, R.arm); if (seed) out.push(seed);
  }
  return out;
}
/** Junction: where two district roads meet (and no crossroads of lanes), each takes one crossing on its arm `arm` the node, unless a tram stop's crossing of that road stands within `stopWithin` m of the node (`stops`: the stop crossings so far, with the road each crosses). */
export function junctionSeeds(ctx: Ctx, rules: CoverageRules, stops: { road: string; mid: V }[]): Seed[] {
  const R = rules.junction; if (!R) return [];
  const out: Seed[] = [];
  for (const P of ctx.d.roads.filter(r => R.class.includes(r.class) && !r.gyratory)) for (const node of P.path) {
    const others = roadsAtNode(ctx, node).filter(q => q !== P), lanes = others.filter(q => q.class === 'access'), twin = others.filter(q => R.class.includes(q.class) && !q.gyratory);
    if (lanes.length >= 2 || !twin.length) continue;
    const at = ctx.plan.nodes[node]!, s0 = project(P.line, at).s;
    if (stops.some(x => x.road === P.id && Math.abs(project(P.line, x.mid).s - s0) <= R.stopWithin)) continue;
    const seed = armSeed(ctx, 'junction', P, node, others, R.arm); if (seed) out.push(seed);
  }
  return out;
}

/** Island: see the file header. */
export function islandSeeds(ctx: Ctx, rules: CoverageRules): Seed[] {
  if (!rules.island) return [];
  const road = ctx.d.roads.find(r => r.id === rules.island!.ring), c = ctx.plan.circus, [r0, r1] = c.ring, S = ctx.S, mid = (r0 + r1) / 2;
  if (!road) return [];
  const sides: (1 | -1)[] = rules.island.per === 'road' ? [rules.island.side === 'r' ? -1 : 1] : [-1, 1]; // brief 061: one footway line per road, the `side` one seen leaving the circus (l = the left); without `per`, both
  const out: Seed[] = [], mouths = returnsOf(ctx).filter(r => r.mouth), wrapA = (a: number) => Math.atan2(Math.sin(a), Math.cos(a));
  const line = (id: string, o: number, u: V, why: string, roadId = '') => { // the footway line `o` m left of the way out `u` through the mouth meets the ring's outer edge at one point
    const nl = left(u), t = Math.sqrt(r1 * r1 - o * o), pOut = add(add(c.c, mul(nl, o)), mul(u, t));
    let e = norm(sub(pOut, c.c));
    // brief 028 (kerbReturn.landings): the mouth's kerb return takes the ring's kerb up to its tangent point; the crossing's near edge stands `clear` m past it, round the ring and away from the road: the seed's base position steps round the ring to there (7 to 10 m), not by coverage.rules.reach
    const ret = mouths.filter(r => r.roads[0] === roadId).map(r => ({ r, aT: Math.atan2(r.a[1] - c.c[1], r.a[0] - c.c[0]) })).sort((x, y) => Math.abs(wrapA(Math.atan2(e[1], e[0]) - x.aT)) - Math.abs(wrapA(Math.atan2(e[1], e[0]) - y.aT)))[0];
    if (ret) {
      const mine = mouths.filter(r => r.roads[0] === roadId), axis = Math.atan2(mine.reduce((t, r) => t + r.at[1] - c.c[1], 0), mine.reduce((t, r) => t + r.at[0] - c.c[0], 0)), side = Math.sign(wrapA(ret.aT - axis)) || 1, need = (kerbReturnStd(ctx.S).clear + (ctx.S.crossings.w as number) / 2) / r1, ae = Math.atan2(e[1], e[0]);
      if (side * wrapA(ae - ret.aT) < need) e = [Math.cos(ret.aT + side * need), Math.sin(ret.aT + side * need)];
    }
    out.push({ id, rule: 'island', road: road.id, cls: road.class, ka: rv(add(c.c, mul(e, r1))), kb: rv(add(c.c, mul(e, r0))), why, via: { ring: c.c, r0, r1, angle: Math.atan2(e[1], e[0]) } });
  };
  for (const q of ctx.d.roads.filter(x => x !== road && !x.gyratory)) { // a road with a node on the ring's middle line
    const node = q.path.find(n => Math.abs(dist(ctx.plan.nodes[n]!, c.c) - mid) < 1); if (!node) continue;
    const j = project(q.line, ctx.plan.nodes[node]!).s, u = norm(sub(atChainage(q.line, j < lengthOf(q.line) / 2 ? j + 10 : j - 10).p, ctx.plan.nodes[node]!)), off = q.w / 2 + S.sections[q.class].pavement / 2;
    for (const sg of sides) line(`island/${q.id}/${sg < 0 ? 'r' : 'l'}`, sg * off, u, `${q.name}'s footway, ${off.toFixed(1)} m off its axis, to the island`, q.id);
  }
  for (const f of ctx.plan.foot.filter(x => x.kind === 'mall')) {
    for (const end of [f.pts[0]!, f.pts[f.pts.length - 1]!]) {
      if (Math.abs(dist(end, c.c) - r1) > 1) continue;
      const other = f.pts[f.pts.length - 1] === end ? f.pts[f.pts.length - 2]! : f.pts[1]!, u = norm(sub(other, end)), band = S.sections.mall.bands.walk as [number, number], off = (band[0] + band[1]) / 2;
      for (const sg of sides) line(`island/${f.id}/${sg < 0 ? 'r' : 'l'}`, sg * off, u, `${f.name}'s walk band, ${off} m off its axis, to the island`);
    }
  }
  return out;
}

/** Foot: see the file header. A footway of `kinds` (plan foot[].kind) meets or crosses a road where its axis comes within half the carriageway's width + 0.6 m of the road's centre line; it continues across with one crossing at that point (the axis point nearest the line), square to the road.
 *  Where the axis cannot stand, a mall's walk band middles (nearest the axis first) are the alternatives of the same site (one `group`). The circus ring is the island rule's; two footways meeting one road at one place are one site (the first in the plan's order names it). */
export function footSeeds(ctx: Ctx, rules: CoverageRules): Seed[] {
  const R = rules.foot; if (!R) return [];
  const out: Seed[] = [], c = ctx.plan.circus, sites: { road: string; s: number }[] = [];
  for (const f of ctx.plan.foot.filter(x => R.kinds.includes(x.kind))) {
    const ax = f.pts as V[], pts: { p: V; u: V }[] = [];
    for (let i = 1; i < ax.length; i++) { const a = ax[i - 1]!, b = ax[i]!, l = dist(a, b), u = norm(sub(b, a)); for (let t = 0; t <= l + 1e-9; t += 0.25) pts.push({ p: add(a, mul(u, Math.min(t, l))), u }); }
    for (const road of ctx.d.roads.filter(r => !r.gyratory)) {
      const near = pts.map(x => ({ ...x, d: lineDist(x.p, road.line) })).filter(x => x.d <= road.w / 2 + 0.6).sort((p, q) => p.d - q.d)[0]; if (!near) continue;
      if (dist(near.p, c.c) < c.ring[1] + 1) continue; // the circus ring is the island rule's
      const j = project(road.line, near.p).s, tg = atChainage(road.line, j).t;
      if (Math.abs(dot(tg, near.u)) > 0.2) continue; // the footway must meet the road square
      if (sites.some(x => x.road === road.id && Math.abs(x.s - j) < 5)) continue; // another footway already crosses here
      sites.push({ road: road.id, s: j });
      const group = `foot/${f.id}/${road.id}`, walk = (ctx.S.sections as Record<string, { bands?: { walk?: [number, number] } }>)[f.kind === 'mall' ? 'mall' : 'walk']?.bands?.walk, nl = left(near.u);
      const where: { id: string; off: number; why: string }[] = [{ id: `${group}/axis`, off: 0, why: `${f.name}'s axis across ${road.name}` }];
      if (f.kind === 'mall' && walk) for (const sg of [1, -1]) where.push({ id: `${group}/${sg < 0 ? 'r' : 'l'}`, off: sg * (walk[0] + walk[1]) / 2, why: `${f.name}'s walk band, ${((walk[0] + walk[1]) / 2)} m off its axis, across ${road.name}` });
      for (const w of where) { const seed = acrossRoad(ctx, 'foot', w.id, road.id, j + w.off * dot(tg, nl), w.why); if (seed) out.push({ ...seed, group }); }
    }
  }
  return out;
}

// ---- placing a site
/** The places a seed may stand, nearest its ideal first: along the road by up to `reach` m (1 m steps), or round the ring by the angle that moves the outer landing the same distance. */
export function variants(ctx: Ctx, seed: Seed, reach: number): Seed[] {
  const v = seed.via, out: Seed[] = [seed];
  for (let k = 1; k <= reach; k++) for (const sg of [1, -1]) {
    if ('s' in v && v.dir !== undefined && sg !== v.dir) continue; // an arm crossing only moves away from the corner it keeps its clear from
    if ('s' in v) { const moved = acrossRoad(ctx, seed.rule, seed.id, seed.road, v.s + sg * k, seed.why); if (moved) out.push({ ...moved, via: { s: v.s + sg * k, ...(v.dir !== undefined ? { dir: v.dir } : {}) }, ...(seed.group ? { group: seed.group } : {}) }); }
    else { const a = v.angle + (sg * k) / v.r1, e: V = [Math.cos(a), Math.sin(a)]; out.push({ ...seed, ka: rv(add(v.ring, mul(e, v.r1))), kb: rv(add(v.ring, mul(e, v.r0))), via: { ...v, angle: a } }); }
  }
  return out;
}
/** The furniture a coverage site may move: street furniture of the pavement. Kiosks, transit kits and watchers stay where they are. */
export const MOVABLE: ReadonlySet<string> = new Set(['tree', 'lamp', 'bench', 'bin', 'hydrant', 'phone-booth', 'mailbox', 'tub-palm', 'planter', 'advertising-column']);

/** The refuge a kerb-to-kerb strip makes where it crosses a median wide enough for one (std.refuge): where the orb's halo may stand (partsOf's candidates: from the refuge's middle out, every 0.5 m, the whole disc inside the strip and the median), the disc's radius, the refuge's middle and length along the strip. Undefined: no refuge, or a road with no signal. */
function refugeOf(ctx: Ctx, ka: V, kb: V, W: number): { cands: V[]; r: number; c: V; span: number } | undefined {
  const strip = ctx.S.crossings.strip as { refuge: number; control?: Record<string, string> };
  const road = ctx.d.roads.find(r => !r.gyratory && Math.abs(lineDist(ka, r.line) - r.w / 2) < 1.5 && Math.abs(lineDist(kb, r.line) - r.w / 2) < 1.5);
  if (!road || strip.control?.[road.class] !== 'signal') return undefined;
  const len = dist(ka, kb), u = norm(sub(kb, ka)), nrm = left(u), look = lookOf(ctx.S.crossings.strip), halo = clearReach(look); // the halo and the clock ring round it (parts.ts RING_OUTER)
  for (const z of ctx.d.zones.filter(q => q.kind === 'median' && q.poly)) {
    const P = z.poly as V[]; if (dist(P[1]!, P[2]!) < strip.refuge - 1e-9) continue;
    let lo = Infinity, hi = -Infinity;
    for (let s = 0; s <= len + 1e-9; s += 0.05) if (inPoly(add(ka, mul(u, s)), P)) { lo = Math.min(lo, s); hi = Math.max(hi, s); }
    if (!(hi - lo > 0.3)) continue;
    const mid = (lo + hi) / 2, along = Math.max(0, (hi - lo) / 2 - halo), across = W / 2 - halo, cands: V[] = [];
    for (let ds = 0; ds <= along + 1e-9; ds += 0.5) for (const sg of ds ? [1, -1] : [1]) for (let dl = 0; dl <= across + 1e-9; dl += 0.5) for (const lg of dl ? [1, -1] : [1]) cands.push(add(add(ka, mul(u, mid + sg * ds)), mul(nrm, lg * dl)));
    return { cands, r: halo, c: add(ka, mul(u, mid)), span: hi - lo };
  }
  return undefined;
}

/** Both landings of a seed, or why not: each kerb end stands on pavement 0.9 m beyond the kerb with SF-2's 2 m clear, the quay edge and the fixed things kept off (the same tests pairCrossings step 2 makes).
 *  With `claim`, furniture in the way (MOVABLE) is taken out of the street to make room (its ids are listed in `moved`); without it a landing needs ground the furniture left free. */
export function place(ctx: Ctx, L: Layer, seed: Seed, W: number, claim = false): { pair: Pair; moved: string[] } | { why: string } {
  const t = norm(sub(seed.kb, seed.ka)), along = left(t), yaw = Math.atan2(along[1], along[0]);
  const ends = [{ k: seed.ka, inward: mul(t, -1) as V, who: 'a' }, { k: seed.kb, inward: t, who: 'b' }].map(e => ({ ...e, at: (s: number, d: number): V => add(add(e.k, mul(along, s)), mul(e.inward, d)) }));
  const moved: string[] = [];
  // a signal crossing needs ground for the orb's halo on the refuge where its strip crosses a median of refuge width (parts.ts: an orb left out fails SF-20): the site is refused where none stands free, and with `claim` the movable furniture on the refuge goes
  const refuge = refugeOf(ctx, seed.ka, seed.kb, W);
  if (refuge && !refuge.cands.some(p => L.clearGround(discRing(p, refuge.r), p, 0.001))) {
    const b = claim ? L.blockers(boxRing(refuge.c, W, refuge.span, yaw), refuge.c, 0.1, MOVABLE) : undefined;
    if (!b) return { why: `no clear ground for the orb's halo on the refuge${claim ? ' (something that stays is in the way)' : ''}` };
    moved.push(...b);
  }
  for (const e of ends) {
    const c = e.at(0, 0.9), ring = boxRing(c, 1.8, W, yaw + QUARTER), near = (m: string) => { const x = L.items.filter(it => dist(it.at, c) < 4.5 && !['crossing', 'bollard', 'beacon'].includes(it.kind)).map(it => `${it.kind} ${dist(it.at, c).toFixed(1)} m`); return `no free spot for the landing at kerb ${e.who} (${e.k.join(', ')})${m}${x.length ? `: ${x.slice(0, 3).join(', ')}` : ''}`; };
    if (!L.fixed(c, 2.2) || quayBlock(ctx, samples9(ring))) return { why: near('') };
    if (L.clearOf(ring, c, 2)) continue;
    const b = claim ? L.blockers(ring, c, 2, MOVABLE) : undefined;
    if (!b) return { why: near(claim ? ' (something that stays is in the way)' : '') };
    moved.push(...b);
  }
  if (moved.length) L.evict([...new Set(moved)]);
  const placed: Landing[] = [];
  for (const e of ends) {
    if (!landing(ctx, L, e.at, 0, yaw, `road/${seed.road}/${e.who}`, seed.cls, seed.road, true, true, `street/${seed.id}/${e.who}`)) return { why: `its landing at kerb ${e.who} was refused` };
    placed.push(L.landings[L.landings.length - 1]!);
  }
  return { pair: { a: placed[0]!, b: placed[1]!, ka: seed.ka, kb: seed.kb }, moved: [...new Set(moved)] };
}

// ---- dedupe (R1, R2)
/** Which of the kerb crossings in `pairs` to drop: two of one road closer than `mouth` m across a side road's mouth, or `bend` m round a bend, are one, and the one on the shorter plot edge goes. Returns the pair and why. */
export function dedupe(ctx: Ctx, pairs: Pair[], edgeLen: (p: Pair) => number, rules: CoverageRules): Map<Pair, string> {
  const d = rules.dedupe, out = new Map<Pair, string>(); if (!d) return out;
  const byRoad = new Map<string, { p: Pair; s: number; mid: V; len: number }[]>();
  for (const p of pairs) {
    const r = ctx.d.roads.find(x => x.id === p.a.road); if (!r) continue;
    const mid: V = [(p.ka[0] + p.kb[0]) / 2, (p.ka[1] + p.kb[1]) / 2];
    (byRoad.get(r.id) ?? byRoad.set(r.id, []).get(r.id)!).push({ p, s: project(r.line, mid).s, mid, len: edgeLen(p) });
  }
  for (const [id, xs] of byRoad) {
    const r = ctx.d.roads.find(x => x.id === id)!, nodes = r.path.map((n, i) => ({ n, i, s: project(r.line, ctx.plan.nodes[n]!).s }));
    const mouths = nodes.filter(x => ctx.d.roads.some(q => q !== r && q.path.includes(x.n))).map(x => x.s);
    const bends = nodes.filter(x => {
      if (x.i === 0 || x.i === r.path.length - 1) return false;
      const a = ctx.plan.nodes[r.path[x.i - 1]!]!, b = ctx.plan.nodes[x.n]!, c = ctx.plan.nodes[r.path[x.i + 1]!]!, u = norm(sub(b, a)), v = norm(sub(c, b));
      return (Math.acos(Math.max(-1, Math.min(1, dot(u, v)))) * 180) / Math.PI >= d.turn;
    }).map(x => x.s);
    const sorted = [...xs].sort((a, b) => a.s - b.s);
    for (let i = 0; i < sorted.length; i++) for (let j = i + 1; j < sorted.length; j++) {
      const a = sorted[i]!, b = sorted[j]!, gap = dist(a.mid, b.mid);
      if (gap >= Math.max(d.mouth, d.bend) || out.has(a.p) || out.has(b.p)) continue;
      const between = (xs2: number[]) => xs2.some(s => s > a.s + 0.01 && s < b.s - 0.01);
      const why = gap < d.mouth && between(mouths) ? `a twin across a side road's mouth (${gap.toFixed(0)} m from the crossing it keeps)` : gap < d.bend && between(bends) ? `a twin round a bend (${gap.toFixed(0)} m from the crossing it keeps)` : '';
      if (!why) continue;
      const loser = a.len < b.len || (a.len === b.len && a.p.a.id > b.p.a.id) ? a : b;
      out.set(loser.p, why);
    }
  }
  return out;
}

/** m: a foot site is served by a crossing of its road that stands this near the footway's axis (the civic block's own crossing, Garden Walk's at Starline Street, stands 9 m off the axis: the walk is 8 m wide and a crossing 4). */
export const FOOT_SERVED = 10;
