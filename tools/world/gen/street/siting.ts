// Where the coverage rules read the plan (streetscape.crossings.coverage.rules, brief 061): the rules block, the switches the other generators read (plot-edge landings, the stop and bank patches) and the junction
// points a tram stop's crossing faces. Plan data only, no coordinates; shared by roads.ts (edges), transit.ts (stops, banks), coverage.ts (the sites) and the world rules. Kept apart from coverage.ts, which imports roads.ts.
import { type V, dist } from '../../../plan/geom';
import type { Ctx } from '../common';

export type CoverageRules = {
  /** How far (m) a site may move along its road, or round the ring, to the nearest spot where both its landings stand free. */
  reach?: number;
  /** false: a plot edge gives no crossing landing at either end, on any road class. */
  edges?: boolean;
  /** A tram stop's crossing: `ends` of the platform's two ends, the one `toward` the nearer junction (a district or access road comes in) or footway end. */
  stops?: { ends: number; toward: 'junction' };
  /** false: no patch at a station bank's stair foot. */
  banks?: boolean;
  /** One crossing per road into the circus (`per` road), on its `side` footway line seen leaving the circus. */
  island?: { ring: string; per?: 'road'; side?: 'l' | 'r' };
  /** A footway of these kinds that meets or crosses a road continues across it with one crossing, `at` its axis. */
  foot?: { kinds: string[]; at: 'axis' };
  /** Access lanes meeting a district road from both sides at one node: one crossing of the district road, on its `arm` before or after the node. */
  crossroads?: { class: string[]; arm: 'before' | 'after' };
  /** Two district roads meeting: one crossing of each on its arm before the node, unless a tram stop's crossing of that road stands within `stopWithin` m of the node. */
  junction?: { class: string[]; arm: 'before' | 'after'; stopWithin: number };
  dedupe?: { mouth: number; bend: number; turn: number };
};
export const rulesOf = (ctx: Pick<Ctx, 'S'>): CoverageRules | undefined => (ctx.S.crossings.coverage as { rules?: CoverageRules } | undefined)?.rules;
/** The places a tram stop's crossing faces (stops.toward 'junction'): every plan node where two roads meet and at least one is not a primary (two primaries meeting at the district's corner bring no one; `off_` nodes lead off the sheet),
 *  and the two ends of every footway (plan.foot: a mall, a walk, the pier). The end of the platform nearer one of these is where people walk in from. */
export function junctionPoints(ctx: Pick<Ctx, 'plan'>): V[] {
  const roads = ctx.plan.roads as { path: string[]; class: string }[], out: V[] = [];
  for (const n of Object.keys(ctx.plan.nodes)) {
    if (n.startsWith('off_')) continue;
    const at = roads.filter(r => r.path.includes(n));
    if (at.length >= 2 && at.some(r => r.class !== 'primary')) out.push(ctx.plan.nodes[n] as V);
  }
  for (const f of ctx.plan.foot) out.push(f.pts[0] as V, f.pts[f.pts.length - 1] as V);
  return out;
}
/** Which of the points `ends` (a platform's end crossings, in order) the rule keeps: all of them without `stops.ends`, else the `ends` nearest a junction point (the first of a tie). */
export function stopEnds(ctx: Pick<Ctx, 'S' | 'plan'>, ends: V[]): number[] {
  const all = ends.map((_, i) => i), r = rulesOf(ctx)?.stops; if (!r || r.ends >= ends.length) return all;
  const pts = junctionPoints(ctx), near = (p: V) => Math.min(...pts.map(q => dist(p, q)));
  return [...all].sort((a, b) => near(ends[a]!) - near(ends[b]!) || a - b).slice(0, r.ends).sort((a, b) => a - b);
}
