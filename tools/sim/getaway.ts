// The getaway car's exit lane (brief 018 §1, Splice): `getaway.exit` in sim/data/handover.site.json.
// Derived, never authored: the mission's routes.getaway (a plan-graph route), the car's bay projected onto the route's first road, then the
// route walked forward `fight-block-m` (30 m) along the carriageway, at the plan road's own width w. The engine reads only this field.
// Shape (Keel reads it): { polyline: [[x, y], ...] in the site frame (block frame, integer cm, like every point in the file), w: metres, src }.
// Unknown = reject: no getaway route, a route that reaches no end, a bay off every road, or a lane crossing roads of different widths throws.
// Rule SX-1 (checkExit): the lane exists, is as long as the rulebook's fight-block-m, starts at the car and lies on the carriageway.
import type { Derived, Plan } from '../plan/plan';
import { edges, dijkstra } from '../plan/graph';
import { toPlanFn } from '../plan/civic';
import { sample, segDist, dist, type Sample, type V } from '../plan/geom';

/** The rulebook's fight-block-m, in metres; brief 018 §1 sets 30 and Keel adds the entry. Until it is there, 30 and `fromRulebook: false`. */
export const FIGHT_BLOCK_M = 30;
export function blockM(rulebook: { values: Record<string, { v: number }> }): { m: number; fromRulebook: boolean } {
  const e = rulebook.values['fight-block-m'];
  return e ? { m: e.v, fromRulebook: true } : { m: FIGHT_BLOCK_M, fromRulebook: false };
}

export type Exit = { polyline: number[][]; w: number; src: string };
type Mission = { routes: { id: string; from: string; to: string[] }[]; destinations: { name: string; at: V }[] };

const cm = (m: number) => Math.round(m * 100);
const STEP = 0.5; // m between samples of a road's centreline
const LANE_EPS = 0.02; // m: the polyline may leave the centreline by this much when thinned (below the cm the file keeps)

/** Plan → block frame (the inverse of civic.blockToPlan), in metres. */
function toBlockFn(spec: { blockToPlan: { origin: V; x: V; y: V } }) {
  const o = spec.blockToPlan.origin, ax = spec.blockToPlan.x, ay = spec.blockToPlan.y, det = ax[0] * ay[1] - ax[1] * ay[0];
  return (p: V): V => { const dx = p[0] - o[0], dy = p[1] - o[1]; return [(dx * ay[1] - dy * ay[0]) / det, (ax[0] * dy - ax[1] * dx) / det]; };
}

/** Nearest point of a sampled centreline to p: chainage and distance. Projects onto the straight pieces between samples. */
function project(ss: Sample[], p: V): { s: number; d: number } {
  let best = { s: 0, d: Infinity };
  for (let i = 1; i < ss.length; i++) {
    const a = ss[i - 1]!, b = ss[i]!, abx = b.p[0] - a.p[0], aby = b.p[1] - a.p[1];
    const t = Math.max(0, Math.min(1, ((p[0] - a.p[0]) * abx + (p[1] - a.p[1]) * aby) / (abx * abx + aby * aby || 1)));
    const d = segDist(p, a.p, b.p);
    if (d < best.d) best = { s: a.s + t * (b.s - a.s), d };
  }
  return best;
}
/** The point at chainage s of a sampled centreline. */
function at(ss: Sample[], s: number): V {
  const k = Math.min(ss.length - 1, Math.max(1, ss.findIndex((x) => x.s >= s)));
  const a = ss[k - 1]!, b = ss[k]!, t = b.s > a.s ? Math.max(0, Math.min(1, (s - a.s) / (b.s - a.s))) : 0;
  return [a.p[0] + (b.p[0] - a.p[0]) * t, a.p[1] + (b.p[1] - a.p[1]) * t];
}
/** Drops the points that lie within `eps` m of the chord between their kept neighbours (a straight stretch is two points). */
function thin(pts: V[], eps: number): V[] {
  if (pts.length < 3) return pts;
  let worst = 0, wi = 0;
  for (let i = 1; i < pts.length - 1; i++) { const d = segDist(pts[i]!, pts[0]!, pts.at(-1)!); if (d > worst) { worst = d; wi = i; } }
  if (worst <= eps) return [pts[0]!, pts.at(-1)!];
  return [...thin(pts.slice(0, wi + 1), eps).slice(0, -1), ...thin(pts.slice(wi), eps)];
}

/** The plan road whose centreline is nearest p, and how far. */
const nearestRoad = (d: Derived, p: V) => d.roads.map((r) => ({ road: r, d: r.line.slice(1).reduce((m, b, i) => Math.min(m, segDist(p, r.line[i]!, b)), Infinity) })).reduce((m, c) => (c.d < m.d ? c : m));

/** The exit lane of the mission's getaway route (plan frame points, in metres, with the roads it lies on and its width). */
export function exitLane(plan: Plan, d: Derived, mission: Mission, bay: string, blockLen: number): { line: V[]; w: number; route: { id: string; from: string; to: string }; roads: string[]; length: number } {
  const r = mission.routes.find((x) => x.id === 'getaway');
  if (!r) throw new Error('the mission gives no routes.getaway: the exit lane has nothing to follow (brief 018 §1)');
  const dest = mission.destinations.find((x) => x.name === bay);
  if (!dest) throw new Error(`the mission has no destination "${bay}" for the car`);
  const E = edges(plan, d), { D, trace } = dijkstra(E, r.from);
  const to = r.to.reduce((b, t) => ((D.get(t) ?? Infinity) < (D.get(b) ?? Infinity) ? t : b));
  if (!Number.isFinite(D.get(to) ?? Infinity)) throw new Error(`routes.getaway from ${r.from} reaches none of ${r.to.join(', ')}`);
  const path = trace(to);
  if (!path.length) throw new Error(`routes.getaway starts at its end (${r.from}): no road to follow`);
  // the route as nodes in travel order, and as runs of one road
  const nodes = [r.from]; for (const e of path) nodes.push(e.a === nodes.at(-1) ? e.b : e.a);
  const runs: { road: string; from: string; to: string }[] = [];
  path.forEach((e, i) => { const last = runs.at(-1); if (last && last.road === e.road) last.to = nodes[i + 1]!; else runs.push({ road: e.road, from: nodes[i]!, to: nodes[i + 1]! }); });
  const road = (id: string) => d.roads.find((x) => x.id === id)!;
  const ss = new Map<string, Sample[]>(); const samples = (id: string) => ss.get(id) ?? (ss.set(id, sample(road(id).segs, STEP)), ss.get(id)!);
  const chain = (id: string, n: string) => project(samples(id), plan.nodes[n] as V).s;

  const bayPlan = toPlanFn(plan.civic)(dest.at);
  const first = project(samples(runs[0]!.road), bayPlan);
  const near = nearestRoad(d, bayPlan); // the car stands at the kerb beside its road, so the bay need not lie on the carriageway; it must be this road's
  if (near.road.id !== runs[0]!.road) throw new Error(`the car's bay (${bay}) is nearest ${near.road.id} (${near.d.toFixed(1)} m), not ${runs[0]!.road}, the getaway route's first road`);

  const line: V[] = []; let left = blockLen, s = first.s; const roads: string[] = [], widths = new Set<number>();
  for (const [i, run] of runs.entries()) {
    const sa = chain(run.road, run.from), sb = chain(run.road, run.to), dir = sb >= sa ? 1 : -1, ssr = samples(run.road);
    const s0 = i === 0 ? s : sa; // the first run starts at the car; later runs at their junction
    const room = dir * (sb - s0);
    if (room < -STEP) throw new Error(`the car's bay lies past the end of the getaway route's first run on ${run.road}`);
    const use = Math.min(left, Math.max(0, room)), s1 = s0 + dir * use;
    const seg: V[] = [at(ssr, s0)];
    for (const x of dir > 0 ? ssr : [...ssr].reverse()) if (dir * (x.s - s0) > 1e-9 && dir * (s1 - x.s) > 1e-9) seg.push(x.p);
    seg.push(at(ssr, s1));
    line.push(...(line.length ? seg.slice(1) : seg));
    roads.push(run.road); widths.add(road(run.road).w);
    left -= use; s = s1;
    if (left <= 1e-9) break;
  }
  if (widths.size > 1) throw new Error(`the exit lane crosses roads of different widths (${[...widths].join(', ')} m): one lane, one width`);
  const w = [...widths][0]!, thinned = thin(line, LANE_EPS);
  return { line: thinned, w, route: { id: r.id, from: r.from, to }, roads, length: blockLen - left };
}

/** `getaway.exit` for the site file: the lane in the site frame (cm), its width in metres, and where it comes from. */
export function buildExit(plan: Plan, d: Derived, mission: Mission, bay: string, rulebook: { values: Record<string, { v: number }> }): Exit {
  const bm = blockM(rulebook), lane = exitLane(plan, d, mission, bay, bm.m), toBlock = toBlockFn(plan.civic as any);
  return {
    polyline: lane.line.map((p) => { const b = toBlock(p); return [cm(b[0]), cm(b[1])]; }),
    w: lane.w,
    src: `mission.json routes "${lane.route.id}" (${lane.route.from} to ${lane.route.to}), its first road ${lane.roads.join(' then ')} (plan.roads w ${lane.w} m); the car's bay "${bay}" projected onto it, walked forward fight-block-m ${bm.m} m (${bm.fromRulebook ? 'rulebook' : 'rulebook has no fight-block-m yet: brief 018 §1 sets 30'}). points: site frame, cm; w: metres. Brief 018 §1, tools/sim/getaway.ts`,
  };
}

/** Rule SX-1 (brief 018 §8): the lane exists, is `fight-block-m` long, starts at the car's bay and lies on the carriageway of a plan road at that road's width.
 *  Returns the problems; none = holds. Reads only the exit as the engine does, the plan and the bay. */
export function checkExit(exit: unknown, plan: Plan, d: Derived, bay: V, blockLen: number): string[] {
  const x = exit as Exit | undefined, bad: string[] = [];
  if (!x || !Array.isArray(x.polyline) || x.polyline.length < 2) return ['getaway.exit is missing or has fewer than two points'];
  if (!(typeof x.w === 'number' && x.w > 0)) return ['getaway.exit.w is not a width in metres'];
  const toPlan = toPlanFn(plan.civic), pts = x.polyline.map((p) => toPlan([p[0]! / 100, p[1]! / 100]));
  // length
  let len = 0; for (let i = 1; i < pts.length; i++) len += dist(pts[i - 1]!, pts[i]!);
  if (len < blockLen - 0.1) bad.push(`the lane is ${len.toFixed(2)} m long, the rulebook's fight-block-m is ${blockLen} m`);
  if (len > blockLen + 0.1) bad.push(`the lane is ${len.toFixed(2)} m long, longer than fight-block-m ${blockLen} m`);
  // starts at the car: the lane's first point is the foot of the car's bay on the road beside it (no road centreline is nearer the bay)
  const startD = dist(pts[0]!, bay), nearD = nearestRoad(d, bay).d;
  if (startD > nearD + 0.1) bad.push(`the lane starts ${startD.toFixed(1)} m from the car's bay; the road beside it is ${nearD.toFixed(1)} m away`);
  // on the carriageway: every point (the polyline densified to 0.5 m) lies on one plan road, and the road's width is the lane's
  const onRoad = (p: V) => d.roads.map((r) => ({ r, d: r.line.slice(1).reduce((m, b, i) => Math.min(m, segDist(p, r.line[i]!, b)), Infinity) })).filter((c) => c.d <= c.r.w / 2 + 0.05);
  const off: string[] = [], widths = new Set<number>();
  for (let i = 1; i < pts.length; i++) {
    const a = pts[i - 1]!, b = pts[i]!, n = Math.max(1, Math.ceil(dist(a, b) / 0.5));
    for (let k = i === 1 ? 0 : 1; k <= n; k++) {
      const p: V = [a[0] + ((b[0] - a[0]) * k) / n, a[1] + ((b[1] - a[1]) * k) / n], on = onRoad(p);
      if (!on.length) { if (off.length < 3) off.push(`(${p[0].toFixed(1)}, ${p[1].toFixed(1)})`); continue; }
      // the road this point is best on: the one whose centreline is nearest
      widths.add(on.reduce((m, c) => (c.d < m.d ? c : m)).r.w);
    }
  }
  if (off.length) bad.push(`the lane leaves the carriageway at ${off.join(', ')}`);
  if (!off.length && [...widths].some((w) => w !== x.w)) bad.push(`the lane's width ${x.w} m is not the width of the road it lies on (${[...widths].join(', ')} m)`);
  return bad;
}
