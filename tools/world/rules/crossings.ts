// SF-19: a crossing is streetscape.crossings.w wide along the way it crosses. Latch's walk found
// street/avenue-court/e18/crossing-1076, size [4, 1.8, 0.02], empty: the generator wrote the width into the box's first
// axis, which runs along the item's yaw, and the yaw is across the road, so the 4 m ran out over the carriageway and
// the pavement while the 1.8 m deep patch stood sideways along the kerb. The export's size is [depth along yaw, width
// across it, thickness]; the rule measures the box along the kerb its plot edge stands on, the tram platform it ends
// or the sky rail line its bank stands against, from the plan and the export, not from the item's own yaw.
import type { Check } from '../../plan/checks';
import type { V } from '../../plan/geom';
import { add, dist, inPoly, mul } from '../../plan/geom';
import { samples9 } from '../../plan/street-zones';
import { TOL, type WCtx, itemRing, lineDist, ringGap, rule } from '../../plan/world-ctx';
import { ctxOf } from '../gen/common';
import { ringWidth } from '../gen/kerbreturn';
import { areaOf, clipConvex, landingsOf, mayCross, overlaps, stripBlock } from '../gen/street/crossings';
import { FOOT_SERVED, type Pair, crossroadsSeeds, dedupe, footSeeds, islandSeeds, junctionSeeds, rulesOf } from '../gen/street/coverage';
import { junctionPoints } from '../gen/street/siting';
import { kerbReturnStd, returnsOf } from '../gen/kerbreturn';
import { mouthsOf } from '../gen/street/footway';
import { lawn, levelAt } from '../gen/street/ground';
import { lookOf } from '../gen/street/parts';
import { groundItems, partProblem, partsOfWorld } from './crossing-parts';
import { roadYaw } from './roadyaw';

const SOURCE = 'streetscape.crossings.w (4 m, near edge ≥ 3 m past the corner tangent; bollards flank at ± 2.4) · '
  + 'brief 007 round 3 walk, note 5 (street/avenue-court/e18/crossing-1076)';

export const crossingRules = (c: WCtx): Check[] => [...sizeRule(c), ...(c.S.crossings.strip ? stripRule(c) : []), ...(c.S.crossings.strip && c.w.crossingRules ? coverageRule(c) : [])];

function sizeRule(c: WCtx): Check[] {
  const crossings = landingsOf(c.w), W = c.S.crossings.w as number, bad: string[] = [];
  for (const s of crossings) {
    const yaw = roadYaw(c, s);
    if (yaw === null) { bad.push(`${s.id} has no road, platform or line to measure against`); continue; }
    const along = itemRing(s).map(p => p[0] * Math.cos(yaw) + p[1] * Math.sin(yaw));
    const width = Math.max(...along) - Math.min(...along);
    if (Math.abs(width - W) > TOL) {
      bad.push(`${s.id} is ${width.toFixed(2)} m wide along its way (size ${s.size[0]} × ${s.size[1]}), needs ${W}`);
    }
  }
  return [rule('SF-19', 'Streetscape', 'A crossing is crossings.w wide along the road, platform or line it crosses',
    crossings.length, bad, 'none', SOURCE)];
}

/** The part kinds brief 061 retired. */
const RETIRED = /^crossing-(edge|field|stud|arc)$/;

const SOURCE20 = 'streetscape.crossings.strip (stagger, refuge, control, across, lights, signal incl. cycle) · Sky\'s walk of 2026-10-14, note 1 (a crossing is kerb to kerb: '
  + 'two landings and a strip across the carriageway between them) · brief 009 (no zebra, no slab: light bars or dots, signal heads) · brief 061 (Sky 2026-10-25: head A with a cycle; no brass edge lines, kerb studs, inlay arcs or lit field)';

/** SF-20, on a plan that carries streetscape.crossings.strip: every landing belongs to one crossing and no slab is left in the street; a kerb crossing is two landings on opposite
 *  kerbs of its road and the strip between their kerb points; the strip crosses no tram track and no bay unless the plan allows its kind; it has a refuge where it crosses a median
 *  of refuge width, and the control the plan gives the road's class; its parts (crossing-parts.ts) are the light bars or dots and the signal heads the plan's standard gives it, each in
 *  exactly one crossing; a signal crossing carries the plan's cycle; no edge line, stud, arc or lit field stands (brief 061). */
function stripRule(c: WCtx): Check[] {
  const std = c.S.crossings.strip as { refuge: number; control?: Record<string, string>; across?: { track?: string[]; bay?: string[] } }, W = c.S.crossings.w as number, look = lookOf(c.S.crossings.strip);
  const crossings = c.w.crossings ?? [], landings = landingsOf(c.w), parts = partsOfWorld(c.w);
  const uses = new Map<string, number>(), seen = new Map<string, number>(), bad: string[] = [];
  for (const x of crossings) for (const id of x.items) uses.set(id, (uses.get(id) ?? 0) + 1);
  for (const x of crossings) for (const l of x.landings) seen.set(l.id, (seen.get(l.id) ?? 0) + 1);
  for (const s of c.w.street) if (s.kind === 'crossing') bad.push(`${s.id} is a landing slab in the street: a crossing lays none (its landing is a keep-clear footprint)`);
  for (const s of parts.values()) if ((uses.get(s.id) ?? 0) !== 1) bad.push(`${s.id} is a part of ${uses.get(s.id) ?? 0} crossings, needs 1`);
  for (const [id, n] of seen) if (n !== 1) bad.push(`${id} is a landing of ${n} crossings, needs 1`);
  const gc = ctxOf(c.plan, c.d, c.civ), ground = groundItems(c);
  for (const s of c.w.street) if (RETIRED.test(s.kind)) bad.push(`${s.id} is a ${s.kind}: the plan gives a crossing no brass edge line, kerb stud, inlay arc or lit field any more (brief 061)`);
  for (const s of landings) { // a landing lies at the level of the surface under it and never on a lawn (Latch's rebuild, notes 2 and 3)
    const level = levelAt(gc, s.at);
    if (Math.abs(s.z - level) > TOL) bad.push(`${s.id} is at z ${s.z} over a surface at ${level}`);
    const dr = (c.w.ground.drives ?? []).find(x => overlaps(itemRing(s), x.ring)); if (dr) bad.push(`${s.id} stands on the drive ${dr.id}: nothing stands on a drive`); // brief 023
    const on = samples9(itemRing(s)).find(p => lawn(gc, p));
    if (on) bad.push(`${s.id} stands on the lawn at (${on[0].toFixed(1)}, ${on[1].toFixed(1)}): a landing stands on pavement, a path or a platform`);
  }
  const envelope = c.plan.standards.tram.trackCentres / 2 + 0.5, tracks = c.w.transit.tram.map(t => t.line), bays = c.w.ground.flats.filter(f => f.surface === 'bay').map(f => f.ring), drives = (c.w.ground.drives ?? []).map(f => f.ring);
  const medians = c.w.ground.medians.filter(m => ringWidth(m.ring as V[]) >= std.refuge - TOL); // brief 028: a median's ends are bullnoses, so its width is read across its length
  for (const x of crossings) {
    const its = x.landings, why = ((): string => {
      if (x.kind === 'kerb') {
        if (its.length !== 2) return `has ${its.length} landings, needs 2`;
        if (its[0]!.edge === its[1]!.edge) return 'has both landings on one kerb';
        const off = its.map((s, i) => { const r = itemRing(s); return inPoly(x.line[i]!, r) ? 0 : lineDist(x.line[i]!, [...r, r[0]!]); }); // the strip starts on the landing
        if (off.some(d => d > 0.02)) return `strip does not start on its landings (${off.map(d => d.toFixed(2)).join(', ')} m off)`;
        const road = c.d.roads.find(r => r.id === x.road);
        if (!road) return `crosses ${x.road || 'no road'}, which is not in the plan`;
        if (Math.abs(dist(x.line[0], x.line[1]) - road.w) > 1.5) return `strip is ${dist(x.line[0], x.line[1]).toFixed(1)} m long on a ${road.w} m road`;
        const control = std.control?.[road.class];
        if (x.control !== control) return `control is ${x.control ?? 'none'}, the plan gives ${road.class} roads ${control ?? 'none'}`;
      } else if (its.length !== 1) return `has ${its.length} landings, needs 1`;
      if (Math.abs(x.width - W) > TOL) return `strip is ${x.width} m wide, needs ${W}`;
      const block = stripBlock(x.strip, tracks, envelope, bays, drives);
      if (block && !mayCross(std.across, block)?.includes(x.kind)) return `strip crosses ${block} and the plan does not say a ${x.kind} crossing may`;
      if (block === 'a tram track' && x.kind === 'kerb' && x.control !== 'signal') return 'strip crosses a tram track and the crossing has no signal control (the signal stops the tram)';
      const want = x.kind === 'kerb' ? medians.map(m => clipConvex(x.strip, m.ring)).filter(r => areaOf(r) > 0.25) : [];
      const sum = (rs: number[][][]) => rs.reduce((t, r) => t + areaOf(r as [number, number][]), 0);
      if (want.length !== x.refuges.length || Math.abs(sum(want) - sum(x.refuges)) > 0.05) return `has ${x.refuges.length} refuges, the median it crosses needs ${want.length}`;
      return partProblem(c, gc, look, x, parts, ground);
    })();
    if (why) bad.push(`${x.id} ${why}`);
  }
  return [rule('SF-20', 'Streetscape', 'A crossing is two landings and a strip between their kerbs, a refuge where it crosses a median of refuge width, no strip across a tram track or a bay the plan does not allow, none across a drive, no landing on one, '
    + 'and the parts the plan gives it (light bars or dots, a signal head at each kerb end and refuge, and its cycle), each in one crossing, on pavement or road and never on a lawn', landings.length + crossings.length + parts.size, bad, 'none', SOURCE20)];
}

const SOURCE22 = 'streetscape.crossings.coverage.rules (edges, stops, banks, island, foot, crossroads, junction, dedupe) · brief 061 (Datum 2026-10-25, Sky 2026-10-24/25: "way too many crossings"; one tram stop end; lane mouths on a continuous footway) · Global Street Design Guide, pedestrian crossings (a crossing where there is a desire line) · kerbReturn.landings';

/** The id of a coverage site's landings: `street/<site id>/a|b`. */
const SITE = /^street\/(island|foot|crossroads|junction)\//;

/** SF-22, on a plan that carries streetscape.crossings.coverage.rules: the rules hold in the export. (a) No twins: no two kerb crossings the rules give, of one road, less than `mouth` m apart across a side road's mouth or `bend` m apart round a bend.
 *  (b) Every site the rules name (the circus island's footway lines, a footway's crossing of a road, the district roads' crossroads and junction arms) is a crossing of its road within `reach` m of where the rule puts it, is served by an existing crossing
 *  (the export's `crossingRules.served`) or is named in `crossingRules.left` with why no ground stood free for it. (c) The export's log names only sites the rules make. (d) `edges` false: every kerb crossing is a site, the civic block's own or the partner landing of one;
 *  `banks` false: no bank crossing; `stops`: a tram stop has `ends` crossing(s), at the platform end(s) nearest a junction or footway end. (e) Every kerb landing's near edge stands at least kerbReturn.landings.clear past the tangent point of a kerb return of its road
 *  and past the side of a continuous footway's table (the civic block's own landings keep the block's: kerbReturn.civic). A rule that cannot be met is seen here, never silent. */
function coverageRule(c: WCtx): Check[] {
  const gc = ctxOf(c.plan, c.d, c.civ), rules = rulesOf(gc), log = c.w.crossingRules; if (!rules || !log) return [];
  const bad: string[] = [], xs = c.w.crossings ?? [], kerb = xs.filter(x => x.kind === 'kerb'), isSite = (x: typeof kerb[number]) => x.landings.some(l => SITE.test(l.id)), isCivic = (x: typeof kerb[number]) => x.landings.some(l => /crossing-x-/.test(l.id));
  const plain = kerb.filter(x => x.landings.length === 2 && !isSite(x)); // a site is the rules' own: the island's crossings on the radials of one circus are not twins
  const pairs = plain.map(x => ({ a: { id: x.landings[0]!.id, road: x.road, cls: '', flank: [] }, b: { id: x.landings[1]!.id, road: x.road, cls: '', flank: [] }, ka: x.line[0], kb: x.line[1], x }) as Pair & { x: typeof kerb[number] });
  for (const [p, why] of dedupe(gc, pairs, () => 0, rules)) bad.push(`${(p as Pair & { x: { id: string } }).x.id} is ${why}`);
  const mid = (x: { line: V[] }): V => [(x.line[0]![0] + x.line[1]![0]) / 2, (x.line[0]![1] + x.line[1]![1]) / 2], reach = (rules.reach ?? 4) + 0.5;
  // (d) the switches
  if (rules.banks === false) for (const x of xs) if (x.kind === 'bank') bad.push(`${x.id} is a station bank patch and the rules say banks false`);
  if (rules.stops) { // one crossing per tram stop, at the platform end nearer a junction or footway end
    const pts = junctionPoints(gc), near = (p: V) => Math.min(...pts.map(q => dist(p, q))), byStop = new Map<string, typeof xs>();
    for (const x of xs.filter(q => q.kind === 'stop')) { const k = x.id.split('/').slice(0, 4).join('/'); (byStop.get(k) ?? byStop.set(k, []).get(k)!).push(x); }
    for (const [k, list] of byStop) {
      if (list.length !== rules.stops.ends) { bad.push(`${k} has ${list.length} crossings, the rules give a stop ${rules.stops.ends}`); continue; }
      const m = c.w.transit.stops.find(q => q.id === list[0]!.landings[0]!.edge); if (!m) continue;
      const xsr = (m.ring as V[]).map(p => p[0]), ysr = (m.ring as V[]).map(p => p[1]), centre: V = [(Math.min(...xsr) + Math.max(...xsr)) / 2, (Math.min(...ysr) + Math.max(...ysr)) / 2];
      for (const x of list) { const p = mid(x), other: V = [2 * centre[0] - p[0], 2 * centre[1] - p[1]]; if (near(p) > near(other) + 0.01) bad.push(`${x.id} stands at the platform end ${near(p).toFixed(0)} m from a junction, the other end is ${near(other).toFixed(0)} m`); }
    }
  }
  // (b) the sites
  const stopCross = xs.filter(x => x.kind === 'stop').map(x => ({ road: x.road, mid: mid(x) }));
  const seeds = [...islandSeeds(gc, rules), ...footSeeds(gc, rules), ...crossroadsSeeds(gc, rules), ...junctionSeeds(gc, rules, stopCross)];
  const served = log.served ?? [], named = (id: string, group?: string) => log.left.some(l => l.startsWith(`${id}:`) || (group && l.startsWith(`${group}:`))) || served.some(l => l.startsWith(`${id}:`));
  const groups = new Map<string, typeof seeds>();
  for (const s of seeds) if (s.group) (groups.get(s.group) ?? groups.set(s.group, []).get(s.group)!).push(s);
  const stands = (s: typeof seeds[number]) => { const m: V = [(s.ka[0] + s.kb[0]) / 2, (s.ka[1] + s.kb[1]) / 2]; return kerb.some(x => x.road === s.road && dist(mid(x), m) <= (s.rule === 'foot' ? FOOT_SERVED : reach)); };
  for (const s of seeds.filter(q => !q.group)) if (!stands(s) && !named(s.id)) bad.push(`${s.id} (${s.why}) has no crossing within ${reach - 0.5} m and is not named as left out`);
  for (const [g, list] of groups) if (!list.some(stands) && !named(list[0]!.id, g)) bad.push(`${g} (${list[0]!.why}) has no crossing within ${reach - 0.5} m of its axis or its walk bands and is not named as left out or served`);
  // edges false: a landing pair of a plot edge stands only where a rule keeps it (a crossroads, junction or foot site it coincides with) or is the civic block's own
  const serves = (sd: typeof seeds[number], x: typeof kerb[number]) => { const m: V = [(sd.ka[0] + sd.kb[0]) / 2, (sd.ka[1] + sd.kb[1]) / 2]; return x.road === sd.road && dist(mid(x), m) <= (sd.rule === 'foot' ? FOOT_SERVED : reach); };
  if (rules.edges === false) for (const x of kerb) if (!isSite(x) && !isCivic(x) && !seeds.some(sd => sd.rule !== 'island' && serves(sd, x))) bad.push(`${x.id} is a plot-edge crossing and no rule keeps it (rules.edges is false)`);
  // (c) the log
  const known = new Set<string>([...seeds.map(s => s.id), ...groups.keys()]);
  for (const l of log.left) if (!known.has(l.split(':')[0]!)) bad.push(`the export names '${l.split(':')[0]}' as left out, and no rule makes that site`);
  for (const l of served) if (!known.has(l.split(':')[0]!)) bad.push(`the export names '${l.split(':')[0]}' as served, and no rule makes that site`);
  // (e) the landings keep their clear: past each kerb return's tangent points on their road, and past the side of a continuous footway's table (where the return's tangent point would be)
  const clear = kerbReturnStd(gc.S).clear, tp: { p: V; roads: readonly string[]; id: string }[] = [];
  for (const r of returnsOf(gc)) if (r.convex && !r.nose && !r.bay && !r.civic) for (const p of [r.a, r.b]) tp.push({ p, roads: r.roads, id: r.id });
  for (const m of mouthsOf(gc)) for (const sg of [-1, 1]) tp.push({ p: add(m.p, mul(m.along, sg * (m.half + m.tangent))), roads: [m.road, m.lane], id: m.id });
  for (const x of kerb) {
    if (isCivic(x)) continue;
    for (const l of x.landings) { const ring = itemRing(l); for (const t of tp) if (t.roads.includes(x.road) && ringGap(ring, [t.p]) < clear - 0.011) bad.push(`${l.id} stands ${ringGap(ring, [t.p]).toFixed(2)} m from the tangent point of ${t.id}, kerbReturn.landings.clear asks ${clear}`); }
  }
  return [rule('SF-22', 'Streetscape', 'The crossing coverage rules hold: no twin crossings across a side road\'s mouth or round a bend, every island, foot, crossroads and junction site the rules name is built, served or named as left out, a tram stop has its one crossing at the end toward a junction, a plot edge and a station bank have none',
    kerb.length + seeds.length, bad, 'none', SOURCE22)];
}
