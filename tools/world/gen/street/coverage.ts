// Crossing coverage rules (brief 009; Datum's proposal of 2026-10-15, approved by Sky): the crossings the plot edges do not give, as rules over the plan, not a list.
// A plot-edge landing (roads.ts, pairCrossings steps 1-2) stands at both ends of every district and access frontage; that left the Crown Circus ring with no way to its island, the
// primary roads with none at their junctions, and long district roads with none between. standards.streetscape.crossings.coverage.rules says where else a crossing stands:
//   arms      a primary through road, at each junction with another road, takes a crossing on each arm, past the side road's mouth by its half width and `offset` (8 m: a district
//             road's pavement), where a plot's pavement stands against at least one kerb (an arm toward the map's edge leads nowhere)
//   island    every footway line that meets the circus ring from outside (a road's pavement middle, a mall's walk band middle) crosses the ring by the radial through the point where it
//             meets the ring's outer edge, to the island
//   mall      a mall that ends at a road's kerb carries its walk bands across it, kerb to kerb
//   midBlock  a district road with more than `gap` m between two crossings (or a junction and a crossing) takes one at the middle of the gap, moved by up to `reach` m to the nearest spot
//             where both landings stand free
//   dedupe    two kerb crossings of one road less than `mouth` m apart across a side road's mouth, or `bend` m apart round a bend, are one: the one on the longer plot edge stays
// A site is two landings on the pavement and the strip between their kerbs (a signal crossing like any other: orbs at the kerb ends, a refuge where it crosses a median); it is placed after the
// furniture, so no existing id moves, and left out where either landing has no free spot (named in the log). A strip across a tram track stands where the plan lets a kerb crossing cross
// one (strip.across.track names `kerb`): the signal stops the tram.
import { type V, add, dist, dot, lineDist, mul, norm, segDist, sub } from '../../../plan/geom';
import { boxRing, quayBlock, ringGap, samples9 } from '../../../plan/street-zones';
import type { Ctx } from '../common';
import { rv } from '../common';
import { idOf, kindOf, plotRing } from '../edges';
import type { Landing, Layer } from './kit';
import { landing } from './roads';

const QUARTER = Math.PI / 2;

/** One crossing the rules call for, before it is placed: the kerb points of its two ends (a, b), the road it crosses and that road's class (its control comes from the class). */
export type Seed = { id: string; rule: 'arm' | 'island' | 'mall' | 'mid'; road: string; cls: string; ka: V; kb: V; why: string; /** how the seed moves to find a free spot: along its road (chainage s) or round the circus ring (the radial's angle from the ring's centre) */ via: { s: number } | { ring: V; r0: number; r1: number; angle: number } };
/** Two landings on opposite kerbs and the kerb points the strip runs between (pairCrossings' own type). */
export type Pair = { a: Landing; b: Landing; ka: V; kb: V };

export type CoverageRules = {
  /** How far (m) an arm, island or mall site may move along its road, or round the ring, to the nearest spot where both its landings stand free. */
  reach?: number;
  arms?: { offset: number }; island?: { ring: string }; mall?: Record<string, never>;
  midBlock?: { gap: number; class: string[]; reach: number };
  dedupe?: { mouth: number; bend: number; turn: number };
};
export const rulesOf = (ctx: Ctx): CoverageRules | undefined => (ctx.S.crossings.coverage as { rules?: CoverageRules } | undefined)?.rules;

/** What the rules did, for the export's log. */
export type CoverageLog = { dedupe: string[]; sites: Record<string, number>; skipped: string[]; /** furniture ids a site took out of the street */ moved: string[] };

// ---- polylines
/** The point and tangent at chainage s of a polyline. */
function atChainage(line: V[], s: number): { p: V; t: V } {
  let acc = 0;
  for (let i = 1; i < line.length; i++) {
    const a = line[i - 1]!, b = line[i]!, l = dist(a, b);
    if (acc + l >= s - 1e-9 || i === line.length - 1) { const u = norm(sub(b, a)), f = Math.max(0, Math.min(l, s - acc)); return { p: add(a, mul(u, f)), t: u }; }
    acc += l;
  }
  return { p: line[0]!, t: [1, 0] };
}
/** The chainage of the point of a polyline nearest to p, and how far p is from it. */
function project(line: V[], p: V): { s: number; d: number } {
  let acc = 0, best = { s: 0, d: Infinity };
  for (let i = 1; i < line.length; i++) {
    const a = line[i - 1]!, b = line[i]!, l = dist(a, b), u = l ? norm(sub(b, a)) : ([1, 0] as V), t = Math.max(0, Math.min(l, dot(sub(p, a), u))), d = dist(p, add(a, mul(u, t)));
    if (d < best.d) best = { s: acc + t, d };
    acc += l;
  }
  return best;
}
const lengthOf = (line: V[]) => line.reduce((n, p, i) => (i ? n + dist(line[i - 1]!, p) : 0), 0);
const left = (t: V): V => [-t[1], t[0]];

/** A seed for the road `road` across its width at chainage s: the left kerb is a, the right b. */
export function acrossRoad(ctx: Ctx, rule: Seed['rule'], id: string, road: string, s: number, why: string): Seed | undefined {
  const r = ctx.d.roads.find(x => x.id === road)!;
  if (s < 0 || s > lengthOf(r.line)) return undefined;
  const { p, t } = atChainage(r.line, s), n = left(t);
  return { id, rule, road, cls: r.class, ka: rv(add(p, mul(n, r.w / 2))), kb: rv(add(p, mul(n, -r.w / 2))), why, via: { s } };
}

/** True if a built plot's pavement stands against the road's kerb at `k` (a plot edge of that road within 1.5 m of it). */
function plotBeside(ctx: Ctx, k: V, road: string): boolean {
  return ctx.d.plots.some(pl => {
    const ring = plotRing(ctx, pl);
    return ring.p.some((a, i) => { const c = ring.c[i]!; return (kindOf(c) === 'primary' || kindOf(c) === 'district' || kindOf(c) === 'access') && idOf(c) === road && segDist(k, a, ring.p[(i + 1) % ring.p.length]!) < 1.5; });
  });
}

// ---- the sites the rules give outright
/** Arms: see the file header. */
export function armSeeds(ctx: Ctx, rules: CoverageRules): Seed[] {
  const offset = rules.arms?.offset; if (offset === undefined) return [];
  const out: Seed[] = [];
  for (const P of ctx.d.roads.filter(r => r.class === 'primary')) {
    P.path.forEach((node, i) => {
      if (i === 0 || i === P.path.length - 1) return; // a road that ends at a node has no arm beyond it
      const sides = ctx.d.roads.filter(q => q !== P && q.path.includes(node)); if (!sides.length) return;
      const reach = Math.max(...sides.map(q => q.w)) / 2 + offset, s0 = project(P.line, ctx.plan.nodes[node]!).s;
      for (const sg of [-1, 1]) {
        const seed = acrossRoad(ctx, 'arm', `arm/${P.id}/${node}/${sg < 0 ? 'before' : 'after'}`, P.id, s0 + sg * reach, `${P.name}, past ${sides.map(q => q.name).join(' and ')} (${reach.toFixed(0)} m from the junction)`);
        if (seed && (plotBeside(ctx, seed.ka, P.id) || plotBeside(ctx, seed.kb, P.id))) out.push(seed);
      }
    });
  }
  return out;
}

/** Island: see the file header. */
export function islandSeeds(ctx: Ctx, rules: CoverageRules): Seed[] {
  if (!rules.island) return [];
  const road = ctx.d.roads.find(r => r.id === rules.island!.ring), c = ctx.plan.circus, [r0, r1] = c.ring, S = ctx.S, mid = (r0 + r1) / 2;
  if (!road) return [];
  const out: Seed[] = [], line = (id: string, o: number, u: V, why: string) => { // the footway line `o` m left of the way out `u` through the mouth meets the ring's outer edge at one point
    const nl = left(u), t = Math.sqrt(r1 * r1 - o * o), pOut = add(add(c.c, mul(nl, o)), mul(u, t)), e = norm(sub(pOut, c.c));
    out.push({ id, rule: 'island', road: road.id, cls: road.class, ka: rv(add(c.c, mul(e, r1))), kb: rv(add(c.c, mul(e, r0))), why, via: { ring: c.c, r0, r1, angle: Math.atan2(e[1], e[0]) } });
  };
  for (const q of ctx.d.roads.filter(x => x !== road && !x.gyratory)) { // a road with a node on the ring's middle line
    const node = q.path.find(n => Math.abs(dist(ctx.plan.nodes[n]!, c.c) - mid) < 1); if (!node) continue;
    const j = project(q.line, ctx.plan.nodes[node]!).s, u = norm(sub(atChainage(q.line, j < lengthOf(q.line) / 2 ? j + 10 : j - 10).p, ctx.plan.nodes[node]!)), off = q.w / 2 + S.sections[q.class].pavement / 2;
    for (const sg of [-1, 1]) line(`island/${q.id}/${sg < 0 ? 'r' : 'l'}`, sg * off, u, `${q.name}'s footway, ${off.toFixed(1)} m off its axis, to the island`);
  }
  for (const f of ctx.plan.foot.filter(x => x.kind === 'mall')) {
    for (const end of [f.pts[0]!, f.pts[f.pts.length - 1]!]) {
      if (Math.abs(dist(end, c.c) - r1) > 1) continue;
      const other = f.pts[f.pts.length - 1] === end ? f.pts[f.pts.length - 2]! : f.pts[1]!, u = norm(sub(other, end)), band = S.sections.mall.bands.walk as [number, number], off = (band[0] + band[1]) / 2;
      for (const sg of [-1, 1]) line(`island/${f.id}/${sg < 0 ? 'r' : 'l'}`, sg * off, u, `${f.name}'s walk band, ${off} m off its axis, to the island`);
    }
  }
  return out;
}

/** Mall: see the file header. */
export function mallSeeds(ctx: Ctx, rules: CoverageRules): Seed[] {
  if (!rules.mall) return [];
  const out: Seed[] = [], c = ctx.plan.circus, band = ctx.S.sections.mall.bands.walk as [number, number], off = (band[0] + band[1]) / 2;
  for (const f of ctx.plan.foot.filter(x => x.kind === 'mall')) for (const end of [f.pts[0]!, f.pts[f.pts.length - 1]!]) {
    if (dist(end, c.c) < c.ring[1] + 1) continue; // the ring end is the island rule's
    const road = ctx.d.roads.filter(r => !r.gyratory).map(r => ({ r, d: lineDist(end, r.line) })).filter(x => Math.abs(x.d - x.r.w / 2) <= 1.5).sort((p, q) => p.d - q.d)[0]?.r; if (!road) continue;
    const j = project(road.line, end).s, tg = atChainage(road.line, j).t, other = f.pts[f.pts.length - 1] === end ? f.pts[f.pts.length - 2]! : f.pts[1]!;
    if (Math.abs(dot(tg, norm(sub(end, other)))) > 0.2) continue; // the mall must meet the road square
    for (const sg of [-1, 1]) { const seed = acrossRoad(ctx, 'mall', `mall/${f.id}/${road.id}/${sg < 0 ? 'r' : 'l'}`, road.id, j + sg * off, `${f.name}'s walk band, ${off} m off its axis, across ${road.name}`); if (seed) out.push(seed); }
  }
  return out;
}

// ---- placing a site
/** The places a seed may stand, nearest its ideal first: along the road by up to `reach` m (1 m steps), or round the ring by the angle that moves the outer landing the same distance. */
export function variants(ctx: Ctx, seed: Seed, reach: number): Seed[] {
  const v = seed.via, out: Seed[] = [seed];
  for (let k = 1; k <= reach; k++) for (const sg of [1, -1]) {
    if ('s' in v) { const moved = acrossRoad(ctx, seed.rule, seed.id, seed.road, v.s + sg * k, seed.why); if (moved) out.push(moved); }
    else { const a = v.angle + (sg * k) / v.r1, e: V = [Math.cos(a), Math.sin(a)]; out.push({ ...seed, ka: rv(add(v.ring, mul(e, v.r1))), kb: rv(add(v.ring, mul(e, v.r0))), via: { ...v, angle: a } }); }
  }
  return out;
}
/** The furniture a coverage site may move: street furniture of the pavement. Kiosks, transit kits and watchers stay where they are. */
export const MOVABLE: ReadonlySet<string> = new Set(['tree', 'lamp', 'bench', 'bin', 'hydrant', 'phone-booth', 'mailbox', 'tub-palm', 'planter', 'advertising-column']);

/** Both landings of a seed, or why not: each kerb end stands on pavement 0.9 m beyond the kerb with SF-2's 2 m clear, the quay edge and the fixed things kept off (the same tests pairCrossings step 2 makes).
 *  With `claim`, furniture in the way (MOVABLE) is taken out of the street to make room (its ids are listed in `moved`); without it a landing needs ground the furniture left free. */
export function place(ctx: Ctx, L: Layer, seed: Seed, W: number, claim = false): { pair: Pair; moved: string[] } | { why: string } {
  const t = norm(sub(seed.kb, seed.ka)), along = left(t), yaw = Math.atan2(along[1], along[0]);
  const ends = [{ k: seed.ka, inward: mul(t, -1) as V, who: 'a' }, { k: seed.kb, inward: t, who: 'b' }].map(e => ({ ...e, at: (s: number, d: number): V => add(add(e.k, mul(along, s)), mul(e.inward, d)) }));
  const moved: string[] = [];
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

// ---- mid-block
/** The seeds of the mid-block rule: for each district road, the middle of every gap wider than `gap` m between consecutive crossings of it (junctions with other roads count as crossings), one seed per gap.
 *  `crossings`: where the crossings of each road stand (the midpoint of each strip). The seed's chainage is the middle; `place` moves it by up to `reach` m (see midOffsets). */
export function midBlockGaps(ctx: Ctx, rules: CoverageRules, crossings: { road: string; mid: V }[]): { road: string; s: number; from: number; to: number }[] {
  const m = rules.midBlock; if (!m) return [];
  const out: { road: string; s: number; from: number; to: number }[] = [];
  for (const r of ctx.d.roads.filter(x => m.class.includes(x.class) && !x.gyratory)) {
    const len = lengthOf(r.line), junctions = r.path.map(n => ({ n, s: project(r.line, ctx.plan.nodes[n]!).s })).filter(x => ctx.d.roads.some(q => q !== r && q.path.includes(x.n))).map(x => x.s);
    const at = [...junctions, ...crossings.filter(c => c.road === r.id).map(c => project(r.line, c.mid).s)].filter(s => s >= -1 && s <= len + 1).sort((a, b) => a - b);
    for (let i = 1; i < at.length; i++) if (at[i]! - at[i - 1]! > m.gap) out.push({ road: r.id, s: (at[i]! + at[i - 1]!) / 2, from: at[i - 1]!, to: at[i]! });
  }
  return out;
}
/** The chainages to try for a mid-block site: the middle first, then 1 m steps either side up to `reach`. */
export const midOffsets = (reach: number): number[] => { const o = [0]; for (let k = 1; k <= reach; k++) o.push(k, -k); return o; };
