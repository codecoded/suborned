// The continuous footway at a gate's lane mouth (brief 061; Sky 2026-10-25 "lane mouths at the gates cross on a continuous footway: yes"; Vela's sketch briefs/061/footway-sketch.png; the plan's standards.streetscape.continuousFootway).
// A gate (plan gates[]) is the end of an access lane on a district road. Where the lane meets the road the pavement runs on across the mouth at kerb height and the lane's traffic gives way: ONE table at the pavement top fills the
// lane between the two plot corners, the lane's traffic rides up a ramp at its back edge and down a pearl lip at its front edge to the carriageway, two pearl threshold bands mark the ramp's foot and the table's edge, and the lane's two
// bollards stand at the table's back corners. The mouth's two kerb returns are not laid (nothing is left to round) and no crossing or crossing part of the lane stands there; the higher road's light ribbon runs straight on across.
// Rules over the plan only (gates, roads, nodes, standards): a new gate gets its footway, a gate removed loses it; no coordinates. Pure in the context (cached): the kerb return index (gen/kerbreturn.ts) asks `isFootwayCorner` for the corners it does not round,
// and the world builder asks `footwaysOf` for the table, ramp, lip and bands.
import type { Footway } from '../../../../contracts/world/world.schema';
import { type V, add, dist, dot, inPoly, mul, norm, segDist, sub } from '../../../plan/geom';
import type { Ctx } from '../common';
import { r3, r4, rv } from '../common';
import { kerbReturnStd, returnsOf } from '../kerbreturn';
import type { Layer } from './kit';

const RANK: Record<string, number> = { access: 1, district: 2, primary: 3 };

/** The plan's standard (streetscape.continuousFootway), the numbers the generator and SF-24 read. */
export type FootwayStd = { table: { z: number }; ramp: { run: number; rise: number }; lip: { d: number; drop: number }; bands: { count: number; w: number }; posts: { off: number } };
export const footwayStd = (S: { continuousFootway?: any } | undefined): FootwayStd | undefined => {
  const f = S?.continuousFootway; if (!f?.table || !f.ramp || !f.lip || !f.bands) return undefined;
  return { table: { z: f.table.z }, ramp: { run: f.ramp.run, rise: f.ramp.rise }, lip: { d: f.lip.d, drop: f.lip.drop }, bands: { count: f.bands.count, w: f.bands.w }, posts: { off: f.posts?.off ?? POST_OFF } };
};
/** m: a lane post stands this far outside the lane's kerb face (SF-5's 0.45 m and the post's own radius), on the pavement beside the ramp's top. */
export const POST_OFF = 0.6;

/** One gate's mouth in the kerb line's frame: `p` = the point of the higher road's near kerb line on the lane's axis, `along` the kerb line's direction, `into` the unit vector from the kerb into the lane (away from the carriageway). */
export type Mouth = {
  id: string; gate: string; lane: string; road: string; node: string; laneCls: string; p: V; along: V; into: V;
  /** half the lane's carriageway width (its kerb faces stand at ± half along `along`) */ half: number;
  /** the kerb return radius and tangent length the mouth's corners would have (kerbReturn.byClass of the lane's class; 90 degrees): the table's depth, and what a crossing beside it keeps its clear from */ tangent: number;
  /** the plot corners at the mouth, on the higher road's kerb line, either side of the lane */ corners: [V, V];
};
const cache = new WeakMap<object, WeakMap<object, Mouth[]>>();

/** The mouth of every gate: the lane (gates[].road) ends at the gate's node on a road of a higher class (gates[].node); a gate whose node does not end its road on a higher road has none. */
export function mouthsOf(ctx: Pick<Ctx, 'plan' | 'd' | 'S'>): Mouth[] {
  if (!footwayStd(ctx.S)) return [];
  const byD = cache.get(ctx.d) ?? cache.set(ctx.d, new WeakMap()).get(ctx.d)!, hit = byD.get(ctx.S); if (hit) return hit;
  const out: Mouth[] = [], R = kerbReturnStd(ctx.S).byClass;
  for (const g of ctx.plan.gates as { id: string; node: string; road: string }[]) {
    const lane = ctx.d.roads.find(r => r.id === g.road), N = ctx.plan.nodes[g.node] as V | undefined; if (!lane || !N) continue;
    const ends = lane.path[0] === g.node || lane.path[lane.path.length - 1] === g.node; if (!ends) continue;
    const higher = ctx.d.roads.filter(r => r !== lane && !r.gyratory && r.path.includes(g.node) && (RANK[r.class] ?? 0) > (RANK[lane.class] ?? 0)).sort((a, b) => (RANK[b.class] ?? 0) - (RANK[a.class] ?? 0))[0]; if (!higher) continue;
    const next = ctx.plan.nodes[lane.path[0] === g.node ? lane.path[1]! : lane.path[lane.path.length - 2]!] as V, into = norm(sub(next, N));
    // the higher road's direction at the node: its nearest segment
    let best = Infinity, h: V = [1, 0];
    higher.line.forEach((a, i) => { const b = higher.line[i + 1]; if (!b) return; const d = segDist(N, a, b); if (d < best) { best = d; h = norm(sub(b, a)); } });
    if (Math.abs(dot(h, into)) > 0.05) continue; // a lane that meets its road square (the plan's junctions are right angles)
    const p = add(N, mul(into, higher.w / 2)), half = lane.w / 2, t = R[lane.class] ?? R.access!;
    out.push({ id: `footway/${g.id}`, gate: g.id, lane: lane.id, road: higher.id, node: g.node, laneCls: lane.class, p, along: h, into, half, tangent: t, corners: [sub(p, mul(h, half)), add(p, mul(h, half))] });
  }
  byD.set(ctx.S, out);
  cache.set(ctx.d, byD);
  return out;
}

/** True if the kerb return of a plot corner at `at` between roads `roads` is the corner of a gate's lane mouth: not rounded, the pavement runs on across (gen/kerbreturn.ts asks before it makes a return). */
export function isFootwayCorner(ctx: Pick<Ctx, 'plan' | 'd' | 'S'>, roads: readonly [string, string], at: V): boolean {
  for (const m of mouthsOf(ctx)) if (roads.includes(m.road) && m.corners.some(c => dist(c, at) < 0.3)) return true; // the corner's place and the higher road: a junction box corner the generator derives for another pair of roads at the same node (the lane across) is the same corner
  return false;
}
/** The longest tangent length (m) of the mouths of `node` on `road` (0: none), what a crossing of the higher road beside the mouth keeps its kerbReturn.landings.clear past. */
export const footwayTangents = (ctx: Pick<Ctx, 'plan' | 'd' | 'S'>, road: string, node: string): number => Math.max(0, ...mouthsOf(ctx).filter(m => m.road === road && m.node === node).map(m => m.tangent));

/** A rectangle in a mouth's frame: `a0`..`a1` along the kerb line from the lane's axis, `d0`..`d1` into the lane from the kerb line, counter-clockwise. */
function rect(m: Mouth, a0: number, a1: number, d0: number, d1: number): V[] {
  const at = (a: number, d: number): V => add(add(m.p, mul(m.along, a)), mul(m.into, d)), ring = [at(a0, d0), at(a1, d0), at(a1, d1), at(a0, d1)];
  const area = ring.reduce((t, p, i) => t + p[0] * ring[(i + 1) % 4]![1] - ring[(i + 1) % 4]![0] * p[1], 0);
  return (area < 0 ? ring.reverse() : ring).map(rv);
}
/** The pieces of a mouth's footway as the export lists them. `surface` = the plots' paving (the table's look); `kerbSurface` = the pearl kerb's (the lip and the bands). */
export function footwayOf(m: Mouth, std: FootwayStd, surface: string, kerbSurface: string): Footway {
  const D = m.tangent, h = m.half, run = std.ramp.run, z = std.table.z;
  const bands: Footway['bands'] = [{ ring: rect(m, -h, h, D + run, D + run + std.bands.w), level: 0, surface: kerbSurface }, { ring: rect(m, -h, h, std.lip.d, std.lip.d + std.bands.w), level: z, surface: kerbSurface }].slice(0, std.bands.count);
  return {
    id: m.id, gate: m.gate, lane: m.lane, road: m.road, level: z, frame: { origin: rv(m.p), along: [r4(m.along[0]), r4(m.along[1])], into: [r4(m.into[0]), r4(m.into[1])] }, half: r3(h), depth: r3(D),
    table: { id: `ground/${m.id}/table`, ring: rect(m, -h, h, 0, D), surface },
    ramp: { ring: rect(m, -h, h, D, D + run), run, rise: std.ramp.rise, surface: kerbSurface },
    lip: { ring: rect(m, -h, h, 0, std.lip.d), d: std.lip.d, drop: std.lip.drop, surface: kerbSurface },
    bands, posts: [-1, 1].map(sg => rv(add(add(m.p, mul(m.along, sg * (h + std.posts.off))), mul(m.into, D)))),
  };
}
/** The ring the road finish must keep off: the table and the ramp to the foot band, whole lane width (the lane's light ribbon stops short of it). */
export const blockRing = (m: Mouth, std: FootwayStd): V[] => rect(m, -m.half - 0.2, m.half + 0.2, 0, m.tangent + std.ramp.run + std.bands.w);
/** The face of the higher road's kerb across the mouth, `a`..`b`, and its unit normal into the carriageway: the ribbon runs straight on along it. */
export const kerbFace = (m: Mouth): { a: V; b: V; toward: V } => ({ a: m.corners[0], b: m.corners[1], toward: mul(m.into, -1) });

/** The mouths whose corners the kerb return index leaves square (gen/kerbreturn.ts asks `isFootwayCorner` before it makes a return): the footways the world lays. A mouth whose corners still carry a kerb return keeps its returns and gets no footway:
 *  the table, the ramp and the returns cannot stand on one ground. */
export function activeMouths(ctx: Pick<Ctx, 'plan' | 'd' | 'S' | 'civ'>): Mouth[] {
  const ret = returnsOf(ctx).filter(r => r.convex && !r.nose);
  return mouthsOf(ctx).filter(m => !ret.some(r => r.roads.includes(m.road) && m.corners.some(c => dist(c, r.at) < 0.3)));
}
/** The footways of the active mouths as the export lists them. `look`: the table takes the use of the plot beside the mouth (the plots' own paving), the ramp the lane's carriageway finish, the lip and the bands the pearl kerb's. */
export function footwaysOf(ctx: Pick<Ctx, 'plan' | 'd' | 'S' | 'civ'>, look: { road: (cls: string) => string; kerb: string }): Footway[] {
  const std = footwayStd(ctx.S); if (!std) return [];
  return activeMouths(ctx).map(m => {
    const probe = add(add(m.corners[0], mul(m.along, -1)), mul(m.into, 1)), plot = ctx.d.plots.find(p => inPoly(probe, p.polyV)) ?? ctx.d.plots.find(p => inPoly(add(add(m.corners[1], m.along), mul(m.into, 1)), p.polyV));
    const f = footwayOf(m, std, plot?.use ?? 'pavement', look.kerb);
    return { ...f, ramp: { ...f.ramp, surface: look.road(m.laneCls) } };
  });
}
/** The lane's two posts (bollards) at each active mouth's table back corners, placed with explicit ids so no other id moves: `street/footway/<gate>/post-a|b`. */
export function dressFootways(ctx: Ctx, L: Layer): void {
  const std = footwayStd(ctx.S); if (!std) return;
  for (const m of activeMouths(ctx)) {
    const f = footwayOf(m, std, '', ''), yaw = Math.atan2(m.into[1], m.into[0]);
    f.posts.forEach((p, i) => { L.add('bollard', p, yaw, ctx.pav, m.id, { size: ctx.S.bollards.size, r: 0.2, force: true, id: `street/${m.id}/post-${'ab'[i]}`, uncounted: true }); });
  }
}

/** The tangent length (m) the kerb return of a footway corner would have had (0: the corner is no footway's). Where the return index leaves a gate's corner square, the landings, kiosks and sight lines that were measured from the return's tangent point keep their distance from here. */
export function footwayTangentAt(ctx: Pick<Ctx, 'plan' | 'd' | 'S'>, roads: readonly [string, string], at: V): number {
  for (const m of mouthsOf(ctx)) if (roads.includes(m.road) && m.corners.some(c => dist(c, at) < 0.3)) return m.tangent;
  return 0;
}
/** The two tangent points a footway corner's return would have had: on the higher road's kerb line, and on the lane's kerb line (SF-8 keeps sight-blocking items 6 m from them). Empty for a corner that is no footway's. */
export function footwayTangentPoints(ctx: Pick<Ctx, 'plan' | 'd' | 'S'>, roads: readonly [string, string], at: V): V[] {
  for (const m of mouthsOf(ctx)) {
    const k = m.corners.findIndex(c => dist(c, at) < 0.3); if (k < 0 || !roads.includes(m.road)) continue;
    const sg = k === 0 ? -1 : 1;
    return [add(at, mul(m.along, sg * m.tangent)), add(at, mul(m.into, m.tangent))];
  }
  return [];
}

/** The floor under a plan point `q` on a footway's ramp or lip (m above the carriageway datum), or undefined off both: the ramp falls from the table's level at its top (`depth` into the lane) to 0 at its foot (`run` further on), the lip falls from the table's level `d` into the lane to 0 at the kerb line.
 *  The table itself is a flat in ground.flats (floor at its level); a point on none of the three is no footway's. */
export function slopeAt(f: Footway, q: V): number | undefined {
  const s = (q[0] - f.frame.origin[0]) * f.frame.into[0] + (q[1] - f.frame.origin[1]) * f.frame.into[1], a = (q[0] - f.frame.origin[0]) * f.frame.along[0] + (q[1] - f.frame.origin[1]) * f.frame.along[1];
  if (Math.abs(a) > f.half + 1e-9) return undefined;
  if (s >= 0 && s <= f.lip.d) return f.level * (s / f.lip.d);
  if (s >= f.depth && s <= f.depth + f.ramp.run) return f.level * (1 - (s - f.depth) / f.ramp.run);
  return undefined;
}
