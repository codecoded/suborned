// The getaway car's stands and the rival car's way in (brief 059 §5, Splice): `getaway.stands[]` and `rivals.car / rivals.posts` in sim/data/handover.site.json.
// Derived, never authored, like the exit lane (tools/sim/getaway.ts): the stands' cars, spots and rival stops are the mission's points (mission.json points, Vantage), each stand's exit lane is the
// carriageway ahead of its car along a mission route (`getaway` for the square, `getaway-service` for the service drive), and the rival car's route is the mission route `rivals` (off_seagates ->
// Orbit Walk -> Rocket Lane -> Apron Road, through its `via` nodes) cut to the walk window. The engine reads the site; it routes the rival car on the drive grid between the points, and does not read the route (Glyph
// draws the approach line from it).
// Rule SX-2 (stands): each stand's car is on a drive-grid cell and its exit lane holds SX-1 (tools/sim/getaway.ts checkExit); it has as many ambush spots and police stands as the square's
//   (the fight posts five and four, brief 018), each on a free walk cell, beyond fight-hold-m of the car and outside the exit lane (MC-11 reads the same); its rival stop is a drive cell the lane and the hold ring leave alone.
// Rule SX-3 (rival route): the route reaches its end, passes the roads the brief names in order, starts at `rival-car-entry` on the window edge; `rival-drop`, `rival-park`, `rival-tail` and `rival-front-stop` lie on its roads
//   (within the carriageway's half width) and the first three in that order along it; every car point is a drive-grid cell and every foot point a free walk cell; the car's entry tick (the park tick less its drive and the drop stop) is after 0:00.
// Unknown = reject: a missing route, point or stand throws or fails with the reason; nothing falls back.
import { toPlanFn } from '../plan/civic';
import { edges, routeVia } from '../plan/graph';
import { dist, sample, segDist, type Sample, type V } from '../plan/geom';
import type { Derived, Plan } from '../plan/plan';
import { WINDOW } from '../plan/walk-grid';
import { at as atChain, buildExit, checkExit, blockM, nearestRoad, project, thin, toBlockFn, type Exit } from './getaway';

const cm = (m: number) => Math.round(m * 100);
const STEP = 0.5; // m between samples of a road's centreline (getaway.ts)
type Place = { at: number[]; stand?: number[] };
type Rulebook = { values: Record<string, { v: number }> };
type Mission = { routes: { id: string; from: string; via?: string[]; to: string[] }[]; destinations: { name: string; at: V }[] };

/** One getaway stand: where the car stands, the mission route its exit lane follows, the pattern of its spots in the mission's points, and the point the rival car stops at for it. */
export type StandSpec = { id: string; car: string; route: string; ambush: RegExp; police: RegExp; rivalStop: string };
/** The Handover's stands (brief 059 §1.4): the square bay where the car starts, and the Office's service drive behind Vale's car. The spots' ids follow the mission points (`svc-` keeps the yard's off the square's fight, which reads ambush-n / police-n). */
export const STANDS: StandSpec[] = [
  { id: 'square', car: 'd:Getaway bay', route: 'getaway', ambush: /^ambush-\d+$/, police: /^police-\d+$/, rivalStop: 'rival-drop' },
  { id: 'service-drive', car: 'service-drive', route: 'getaway-service', ambush: /^svc-ambush-\d+$/, police: /^svc-police-\d+$/, rivalStop: 'rival-tail' },
];
/** The rival car's points on the route, and the foot points of the crew (brief 059 §2.2; Vantage's mission points). */
export const RIVAL_CAR = { entry: 'rival-car-entry', drop: 'rival-drop', park: 'rival-park', tail: 'rival-tail', front: 'rival-front-stop' } as const;

export const RIVAL_FOOT = { lookout: 'rival-lookout', lookoutExit: 'rival-lookout-exit', valeDoor: 'rival-vale-door' } as const;
/** Pulse's drop stop (rivals-drop-s 8 [S], briefs/059/pulse): used for the entry tick until the rulebook carries it. */
const DROP_S = 8;

export type Stand = { id: string; car: string; facing: number[]; exit: Exit; ambush: string[]; police: string[]; rivalStop: string; src: string };
export type RivalRoute = { polyline: number[][]; roads: { id: string; w: number }[]; length: number; chain: Record<string, number>; src: string };
export type Rivals = {
  car: { entry: string; drop: string; park: string; tail: string; front: string; facing: Record<string, number[]>; route: RivalRoute; src: string };
  posts: { lookout: string; lookoutExit: string; valeDoor: string; src: string };
};

const ids = (places: Record<string, Place>, re: RegExp) => Object.keys(places).filter((k) => re.test(k)).sort((a, b) => Number(a.replace(/\D+/g, '')) - Number(b.replace(/\D+/g, '')));
const pt = (p: Place): V => [p.stand?.[0] ?? p.at[0]!, p.stand?.[1] ?? p.at[1]!].map((v) => v / 100) as V;
const at = (p: Place): V => [p.at[0]! / 100, p.at[1]! / 100];
const bbox = (poly: V[]) => ({ x0: Math.min(...poly.map((p) => p[0])), x1: Math.max(...poly.map((p) => p[0])), y0: Math.min(...poly.map((p) => p[1])), y1: Math.max(...poly.map((p) => p[1])) });
const centre = (poly: V[]): V => { const b = bbox(poly); return [(b.x0 + b.x1) / 2, (b.y0 + b.y1) / 2]; };
const inBox = (p: V, poly: V[]) => { const b = bbox(poly); return p[0] >= b.x0 && p[0] <= b.x1 && p[1] >= b.y0 && p[1] <= b.y1; };
const unit100 = (v: V): number[] => { const l = Math.hypot(v[0], v[1]) || 1; return [Math.round((v[0] / l) * 100), Math.round((v[1] / l) * 100)].map((x) => (x === 0 ? 0 : x)); };

/** The block-frame heading of a stand's car (integer, 100 = one axis step): a car on a drive stands nose out, along the drive's long axis away from the bay it enters; a car in a bay stands along the bay's long axis,
 *  the way its exit lane runs from it. A bay whose axis is across its lane has no heading. */
export function standFacing(block: { bays: { id: string; polygon: V[] }[]; drives?: { id: string; polygon: V[] }[] }, car: V, exitPolyline: number[][]): number[] {
  const drive = (block.drives ?? []).find((x) => inBox(car, x.polygon));
  const axisOf = (poly: V[]): V => { const b = bbox(poly); return b.x1 - b.x0 >= b.y1 - b.y0 ? [1, 0] : [0, 1]; };
  if (drive) {
    const bay = block.bays.find((b) => { const d = bbox(drive.polygon), e = bbox(b.polygon); return e.x0 <= d.x1 + 0.01 && d.x0 <= e.x1 + 0.01 && e.y0 <= d.y1 + 0.01 && d.y0 <= e.y1 + 0.01; });
    if (!bay) throw new Error(`the drive ${drive.id} meets no bay: the car on it has no way to face`);
    const a = axisOf(drive.polygon), dc = centre(drive.polygon), bc = centre(bay.polygon), s = Math.sign((dc[0] - bc[0]) * a[0] + (dc[1] - bc[1]) * a[1]) || 1;
    return unit100([a[0] * s, a[1] * s]);
  }
  const bay = block.bays.find((b) => inBox(car, b.polygon));
  if (!bay) throw new Error(`the stand's car at (${car}) lies in no bay and on no drive of the civic block`);
  const a = axisOf(bay.polygon), l: V = [(exitPolyline[1]![0]! - exitPolyline[0]![0]!) / 100, (exitPolyline[1]![1]! - exitPolyline[0]![1]!) / 100], s = Math.sign(l[0] * a[0] + l[1] * a[1]);
  if (!s) throw new Error(`the bay ${bay.id}'s long axis is across its exit lane: the car has no way to face`);
  return unit100([a[0] * s, a[1] * s]);
}

/** `getaway.stands[]` for the site: each stand's car, heading, exit lane (derived from its mission route), spots and rival stop. */
export function buildStands(plan: Plan, d: Derived, mission: Mission, block: { bays: { id: string; polygon: V[] }[]; drives?: { id: string; polygon: V[] }[] }, rulebook: Rulebook, places: Record<string, Place>): Stand[] {
  return STANDS.map((s) => {
    const car = places[s.car]; if (!car) throw new Error(`stand ${s.id}: the mission has no place ${s.car} for its car`);
    const ambush = ids(places, s.ambush), police = ids(places, s.police);
    if (!places[s.rivalStop]) throw new Error(`stand ${s.id}: the mission has no point ${s.rivalStop} for the rival car's stop`);
    const exit = buildExit(plan, d, mission, s.car.startsWith('d:') ? s.car.slice(2) : at(car), rulebook, s.route);
    return {
      id: s.id, car: s.car, facing: standFacing(block, at(car), exit.polyline), exit, ambush, police, rivalStop: s.rivalStop,
      src: `brief 059 §1.4 and §5: the getaway car's stand ${s.id} (${s.car}); exit = the carriageway ahead of the car along mission route "${s.route}" (fight-block-m); ambush and police = the mission points ${s.ambush} and ${s.police}, in number order; rivalStop = where the rival car stops for it (${s.rivalStop}); facing = nose ${s.car.startsWith('d:') ? 'along the bay\'s long axis the way the exit lane runs' : 'out along the drive\'s long axis, away from the bay'} (block frame, 100 = one axis step)`,
    };
  });
}

/** The first thing wrong with the stands, or none (SX-2). `free(p)` = a free walk cell, `drivable(p)` = a drive-grid cell, both block frame, metres. */
export function checkStands(stands: Stand[] | undefined, plan: Plan, d: Derived, places: Record<string, Place>, rulebook: Rulebook, free: (p: V) => boolean, drivable: (p: V) => boolean): string[] {
  const bad: string[] = [];
  if (!stands || stands.length < 2) return ['getaway.stands is missing or lists fewer than the two stands (the square and the service drive)'];
  const toPlan = toPlanFn(plan.civic), bm = blockM(rulebook).m, holdM = rulebook.values['fight-hold-m']?.v ?? 8;
  const nAmb = stands[0]!.ambush.length, nPol = stands[0]!.police.length;
  for (const s of stands) {
    const car = places[s.car]; if (!car) { bad.push(`stand ${s.id}: no place ${s.car}`); continue; }
    const c = at(car), at2 = (id: string) => places[id] && pt(places[id]!);
    if (!drivable(c)) bad.push(`stand ${s.id}: its car ${s.car} (${c}) is not on a drive-grid cell`);
    if (!(Math.abs(Math.hypot(s.facing[0]!, s.facing[1]!) - 100) < 1)) bad.push(`stand ${s.id}: its facing ${s.facing} is not a heading of 100`);
    bad.push(...checkExit(s.exit, plan, d, toPlan(c), bm).map((x) => `stand ${s.id}: exit lane: ${x}`));
    if (s.ambush.length !== nAmb || s.police.length !== nPol) bad.push(`stand ${s.id}: ${s.ambush.length} ambush spots and ${s.police.length} police stands, the square's are ${nAmb} and ${nPol}`);
    const lane = s.exit.polyline.map((p) => [p[0]! / 100, p[1]! / 100] as V), half = s.exit.w / 2;
    const inLane = (p: V) => { for (let i = 1; i < lane.length; i++) { const a = lane[i - 1]!, b = lane[i]!, abx = b[0] - a[0], aby = b[1] - a[1], ab2 = abx * abx + aby * aby, dot = (p[0] - a[0]) * abx + (p[1] - a[1]) * aby; if (dot < 0 || dot > ab2) continue; if (Math.abs((p[0] - a[0]) * aby - (p[1] - a[1]) * abx) <= half * Math.sqrt(ab2)) return true; } return lane.slice(1, -1).some((q) => dist(p, q) <= half); };
    for (const id of [...s.ambush, ...s.police]) {
      const p = at2(id)!;
      if (!free(p)) bad.push(`stand ${s.id}: ${id} (${p}) is not on a free walk cell`);
      if (dist(p, c) <= holdM) bad.push(`stand ${s.id}: ${id} is ${dist(p, c).toFixed(1)} m from the car, inside fight-hold-m ${holdM} m`);
      if (inLane(p)) bad.push(`stand ${s.id}: ${id} (${p}) is in the exit lane`);
    }
    const rs = at2(s.rivalStop);
    if (!rs) bad.push(`stand ${s.id}: no rival stop ${s.rivalStop}`);
    else {
      if (!drivable(rs)) bad.push(`stand ${s.id}: its rival stop ${s.rivalStop} (${rs}) is not on a drive-grid cell`);
      if (inLane(rs)) bad.push(`stand ${s.id}: its rival stop ${s.rivalStop} is in the exit lane`);
      if (dist(rs, c) <= holdM) bad.push(`stand ${s.id}: its rival stop ${s.rivalStop} is inside fight-hold-m of the car`);
    }
  }
  return bad;
}

/** Where along a polyline (m) a point projects, and how far off it is. */
function along(poly: V[], p: V): { s: number; d: number; dir: V } {
  let best = { s: 0, d: Infinity, dir: [1, 0] as V }, acc = 0;
  for (let i = 1; i < poly.length; i++) {
    const a = poly[i - 1]!, b = poly[i]!, abx = b[0] - a[0], aby = b[1] - a[1], len = Math.hypot(abx, aby) || 1e-9, t = Math.max(0, Math.min(1, ((p[0] - a[0]) * abx + (p[1] - a[1]) * aby) / (len * len))), dd = segDist(p, a, b);
    if (dd < best.d - 1e-9) best = { s: acc + t * len, d: dd, dir: [abx / len, aby / len] };
    acc += len;
  }
  return best;
}

/** The mission route `rivals` as a polyline in the site frame, cut to the walk window: the road centrelines through the route's nodes, from where it enters the window to its end. */
export function buildRivalRoute(plan: Plan, d: Derived, mission: Mission, places: Record<string, Place>, routeId = 'rivals'): RivalRoute & { block: V[] } {
  const r = mission.routes.find((x) => x.id === routeId);
  if (!r) throw new Error(`the mission gives no routes.${routeId}: the rival car has no way in (brief 059 §5)`);
  const rp = (() => { try { return routeVia(edges(plan, d), r); } catch (e) { throw new Error(`routes.${routeId}: ${(e as Error).message}`); } })();
  if (!rp.edges.length) throw new Error(`routes.${routeId} starts at its end (${r.from})`);
  const runs: { road: string; from: string; to: string }[] = [];
  rp.edges.forEach((e, i) => { const last = runs.at(-1); if (last && last.road === e.road) last.to = rp.nodes[i + 1]!; else runs.push({ road: e.road, from: rp.nodes[i]!, to: rp.nodes[i + 1]! }); });
  const road = (id: string) => d.roads.find((x) => x.id === id)!, ss = new Map<string, Sample[]>(), samples = (id: string) => ss.get(id) ?? (ss.set(id, sample(road(id).segs, STEP)), ss.get(id)!);
  const line: V[] = [];
  for (const run of runs) {
    const ssr = samples(run.road), sa = project(ssr, plan.nodes[run.from] as V).s, sb = project(ssr, plan.nodes[run.to] as V).s, dir = sb >= sa ? 1 : -1, seg: V[] = [atChain(ssr, sa)];
    for (const x of dir > 0 ? ssr : [...ssr].reverse()) if (dir * (x.s - sa) > 1e-9 && dir * (sb - x.s) > 1e-9) seg.push(x.p);
    seg.push(atChain(ssr, sb));
    line.push(...(line.length ? seg.slice(1) : seg));
  }
  const toBlock = toBlockFn(plan.civic as never), W = WINDOW, inside = (p: V) => p[0] >= W.x0 && p[0] <= W.x1 && p[1] >= W.y0 && p[1] <= W.y1;
  const dense: V[] = []; // the long off-sheet leg is walked at the road's own samples (0.5 m); the polyline is cut where it first stands in the window
  for (const p of line) dense.push(toBlock(p));
  const i0 = dense.findIndex(inside);
  if (i0 < 0) throw new Error(`routes.${routeId} never enters the walk window`);
  const clip: V[] = [];
  if (i0 > 0) { // the boundary crossing between the last sample outside and the first inside (Liang-Barsky on that one segment), clamped onto the window's edge
    const a = dense[i0 - 1]!, b = dense[i0]!, lo = [W.x0, W.y0], hi = [W.x1, W.y1];
    let t0 = 0;
    for (const k of [0, 1] as const) { const dd = b[k] - a[k]; if (Math.abs(dd) < 1e-12) continue; t0 = Math.max(t0, Math.min((lo[k]! - a[k]) / dd, (hi[k]! - a[k]) / dd)); }
    clip.push([Math.min(W.x1, Math.max(W.x0, a[0] + (b[0] - a[0]) * t0)), Math.min(W.y1, Math.max(W.y0, a[1] + (b[1] - a[1]) * t0))]);
  }
  for (let i = i0; i < dense.length && inside(dense[i]!); i++) clip.push(dense[i]!);
  const poly = thin(clip, 0.02);
  const roadsIn: { id: string; w: number }[] = [];
  for (let i = 1; i < poly.length; i++) { const m: V = [(poly[i - 1]![0] + poly[i]![0]) / 2, (poly[i - 1]![1] + poly[i]![1]) / 2], rd = nearestRoad(d, toPlanFn(plan.civic)(m)).road; if (roadsIn.at(-1)?.id !== rd.id) roadsIn.push({ id: rd.id, w: rd.w }); }
  let length = 0; for (let i = 1; i < poly.length; i++) length += dist(poly[i - 1]!, poly[i]!);
  const chain: Record<string, number> = {};
  for (const [k, id] of Object.entries(RIVAL_CAR)) if (places[id]) chain[k] = cm(along(poly, at(places[id]!)).s);
  return {
    polyline: poly.map((p) => [cm(p[0]), cm(p[1])]), roads: roadsIn, length: Math.round(length * 100) / 100, chain, block: poly,
    src: `mission.json routes "${routeId}" (${r.from} through ${(r.via ?? []).join(', ') || 'no via node'} to ${r.to}): the plan road centrelines in travel order, cut to the walk window where it enters (rival-car-entry's edge); points: site frame, cm; chain: where each rival point projects, cm along the polyline from its start. Brief 059 §5, tools/sim/stands.ts`,
  };
}

/** `rivals.car` and `rivals.posts` for the site. */
export function buildRivals(plan: Plan, d: Derived, mission: Mission, places: Record<string, Place>): Rivals {
  for (const id of [...Object.values(RIVAL_CAR), ...Object.values(RIVAL_FOOT)]) if (!places[id]) throw new Error(`rivals: the mission has no point ${id} (brief 059, Vantage)`);
  const { block, ...route } = buildRivalRoute(plan, d, mission, places), facing: Record<string, number[]> = {};
  for (const [k, id] of Object.entries(RIVAL_CAR)) { const a = along(block, at(places[id]!)); facing[k] = unit100(a.dir); }
  return {
    car: {
      ...RIVAL_CAR, facing, route,
      src: 'brief 059 §2.2 and §5, Pulse\'s rival routine (briefs/059/pulse/rival-routine.md §9, K10) and Vantage\'s mission points: the rival car enters at entry, stops at drop to drop three, parks at park; tail is where it stops when it follows Vale\'s car (and the service-drive stand\'s rival stop), front where a crew drawn by gunfire stops; facing = the route\'s direction at each point (block frame, 100 = one axis step; the front stop\'s heading is the engine\'s, from the route it rides); route is derived from mission route "rivals". Values are place ids (mission.json points)',
    },
    posts: { ...RIVAL_FOOT, src: 'the crew on foot (mission.json points): the lookout\'s post, where a warned lookout leaves the map, and the wheelman\'s place at Vale\'s car. Place ids' },
  };
}

/** SX-3: the first things wrong with the rival car's route and points. */
export function checkRivals(r: Rivals | undefined, plan: Plan, d: Derived, places: Record<string, Place>, rulebook: Rulebook, rivalsArrive: number, free: (p: V) => boolean, drivable: (p: V) => boolean): string[] {
  const bad: string[] = [];
  if (!r?.car?.route?.polyline || r.car.route.polyline.length < 2) return ['rivals.car.route is missing or has fewer than two points'];
  const route = r.car.route, poly = route.polyline.map((p) => [p[0]! / 100, p[1]! / 100] as V), W = WINDOW;
  const first = poly[0]!, onEdge = Math.abs(first[0] - W.x0) < 1e-6 || Math.abs(first[0] - W.x1) < 1e-6 || Math.abs(first[1] - W.y0) < 1e-6 || Math.abs(first[1] - W.y1) < 1e-6;
  if (!onEdge) bad.push(`the route starts at (${first}), not on the walk window's edge: it is not cut to the window`);
  if (poly.some((p) => p[0] < W.x0 - 1e-6 || p[0] > W.x1 + 1e-6 || p[1] < W.y0 - 1e-6 || p[1] > W.y1 + 1e-6)) bad.push('the route leaves the walk window');
  const need = ['entry', 'drop', 'park', 'tail', 'front'] as const, toPlan = toPlanFn(plan.civic), p: Record<string, V> = {};
  for (const k of need) { const pl = places[r.car[k]]; if (!pl) { bad.push(`rivals.car.${k}: no point ${r.car[k]}`); continue; } p[k] = pt(pl); if (!drivable(p[k]!)) bad.push(`rivals.car.${k} (${r.car[k]}, ${p[k]}) is not on a drive-grid cell`); }
  for (const [k, id] of Object.entries(r.posts)) { if (k === 'src') continue; const pl = places[id]; if (!pl) { bad.push(`rivals.posts.${k}: no point ${id}`); continue; } if (!free(pt(pl))) bad.push(`rivals.posts.${k} (${id}, ${pt(pl)}) is not on a free walk cell`); }
  if (bad.length) return bad;
  const a = Object.fromEntries(need.map((k) => [k, along(poly, p[k]!)])) as Record<(typeof need)[number], ReturnType<typeof along>>;
  // the entry stands on the route's centreline at the window edge (within a cell of its start)
  if (a.entry.d > 0.05) bad.push(`rivals.car.entry (${r.car.entry}) is ${a.entry.d.toFixed(2)} m off the route's centreline: the car appears on it`);
  if (a.entry.s > 0.5) bad.push(`rivals.car.entry (${r.car.entry}) is ${a.entry.s.toFixed(2)} m along the route: it is not where the route enters the window`);
  // the other points stand on the carriageway the route follows (half the road's width; a car keeps to its lane, not the centreline) and in order along it
  for (const k of ['drop', 'park', 'tail', 'front'] as const) {
    const w = nearestRoad(d, toPlan(p[k]!)).road.w;
    if (a[k].d > w / 2 + 0.05) bad.push(`rivals.car.${k} (${r.car[k]}) is ${a[k].d.toFixed(1)} m from the route, off the ${w} m carriageway`);
  }
  if (!(a.entry.s < a.drop.s && a.drop.s < a.park.s && a.park.s < a.tail.s)) bad.push(`the rival car's points are not in order along the route: entry ${a.entry.s.toFixed(1)}, drop ${a.drop.s.toFixed(1)}, park ${a.park.s.toFixed(1)}, tail ${a.tail.s.toFixed(1)} m`);
  const seq = route.roads.map((x) => x.id), want = [nearestRoad(d, toPlan(p.entry!)).road.id, nearestRoad(d, toPlan(p.park!)).road.id];
  if (seq[0] !== want[0]) bad.push(`the route starts on ${seq[0]}, rival-car-entry stands on ${want[0]}`);
  if (seq.at(-1) !== want[1]) bad.push(`the route ends on ${seq.at(-1)}, rival-park stands on ${want[1]}`);
  if (seq.length < 3) bad.push(`the route runs on ${seq.join(', ')} in the window: Orbit Walk, Rocket Lane and Apron Road are three roads`);
  // tEntry: the car's entry tick = the park tick less its drive (entry to drop to park, at the road pace) and the drop stop; it must be after 0:00
  const v = (rulebook.values['car-road-cm-s']?.v ?? 500) / 100, dropS = rulebook.values['rivals-drop-s']?.v ?? DROP_S, tEntry = rivalsArrive - (a.park.s - a.entry.s) / v - dropS;
  if (!(tEntry > 0)) bad.push(`the rival car would have to enter at ${tEntry.toFixed(1)} s: ${(a.park.s - a.entry.s).toFixed(0)} m at ${v} m/s and a ${dropS} s drop before the ${rivalsArrive} s park`);
  // the facings are the route's own direction
  for (const k of need) { const f = r.car.facing[k]; if (!f || Math.abs(Math.hypot(f[0]!, f[1]!) - 100) > 1.5) bad.push(`rivals.car.facing.${k} ${f} is not a heading of 100`); }
  return bad;
}
