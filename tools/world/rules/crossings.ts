// SF-19: a crossing is streetscape.crossings.w wide along the way it crosses. Latch's walk found
// street/avenue-court/e18/crossing-1076, size [4, 1.8, 0.02], empty: the generator wrote the width into the box's first
// axis, which runs along the item's yaw, and the yaw is across the road, so the 4 m ran out over the carriageway and
// the pavement while the 1.8 m deep patch stood sideways along the kerb. The export's size is [depth along yaw, width
// across it, thickness]; the rule measures the box along the kerb its plot edge stands on, the tram platform it ends
// or the sky rail line its bank stands against, from the plan and the export, not from the item's own yaw.
import type { Check } from '../../plan/checks';
import type { V } from '../../plan/geom';
import { dist, inPoly } from '../../plan/geom';
import { samples9 } from '../../plan/street-zones';
import { TOL, type WCtx, itemRing, lineDist, rule } from '../../plan/world-ctx';
import { ctxOf } from '../gen/common';
import { areaOf, clipConvex, landingsOf, stripBlock } from '../gen/street/crossings';
import { type Pair, armSeeds, dedupe, islandSeeds, mallSeeds, rulesOf } from '../gen/street/coverage';
import { lawn, levelAt } from '../gen/street/ground';
import { lookOf } from '../gen/street/parts';
import { arcOverlaps, groundItems, partProblem, partsOfWorld } from './crossing-parts';
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

const SOURCE20 = 'streetscape.crossings.strip (stagger, refuge, control, across, lights, signal, studs, arc) · Sky\'s walk of 2026-10-14, note 1 (a crossing is kerb to kerb: '
  + 'two landings and a strip across the carriageway between them) · brief 009 (no zebra, no slab: light bars or dots, signal orbs, studs, inlay arcs)';

/** SF-20, on a plan that carries streetscape.crossings.strip: every landing belongs to one crossing and no slab is left in the street; a kerb crossing is two landings on opposite
 *  kerbs of its road and the strip between their kerb points; the strip crosses no tram track and no bay unless the plan allows its kind; it has a refuge where it crosses a median
 *  of refuge width, and the control the plan gives the road's class; its parts (crossing-parts.ts) are the light bars or dots, signal orbs, studs and arcs the plan's standard gives it,
 *  each in exactly one crossing; no two crossings' arcs overlap. */
function stripRule(c: WCtx): Check[] {
  const std = c.S.crossings.strip as { refuge: number; control?: Record<string, string>; across?: { track?: string[]; bay?: string[] } }, W = c.S.crossings.w as number, look = lookOf(c.S.crossings.strip);
  const crossings = c.w.crossings ?? [], landings = landingsOf(c.w), parts = partsOfWorld(c.w);
  const uses = new Map<string, number>(), seen = new Map<string, number>(), bad: string[] = [];
  for (const x of crossings) for (const id of x.items) uses.set(id, (uses.get(id) ?? 0) + 1);
  for (const x of crossings) for (const l of x.landings) seen.set(l.id, (seen.get(l.id) ?? 0) + 1);
  for (const s of c.w.street) if (s.kind === 'crossing') bad.push(`${s.id} is a landing slab in the street: a crossing lays none (its landing is a keep-clear footprint)`);
  for (const s of parts.values()) if ((uses.get(s.id) ?? 0) !== 1) bad.push(`${s.id} is a part of ${uses.get(s.id) ?? 0} crossings, needs 1`);
  for (const [id, n] of seen) if (n !== 1) bad.push(`${id} is a landing of ${n} crossings, needs 1`);
  const gc = ctxOf(c.plan, c.d, c.civ), ground = groundItems(c), arcsAll = [...parts.values()].filter(s => s.kind === 'crossing-arc');
  for (const s of landings) { // a landing lies at the level of the surface under it and never on a lawn (Latch's rebuild, notes 2 and 3)
    const level = levelAt(gc, s.at);
    if (Math.abs(s.z - level) > TOL) bad.push(`${s.id} is at z ${s.z} over a surface at ${level}`);
    const on = samples9(itemRing(s)).find(p => lawn(gc, p));
    if (on) bad.push(`${s.id} stands on the lawn at (${on[0].toFixed(1)}, ${on[1].toFixed(1)}): a landing stands on pavement, a path or a platform`);
  }
  const envelope = c.plan.standards.tram.trackCentres / 2 + 0.5, tracks = c.w.transit.tram.map(t => t.line), bays = c.w.ground.flats.filter(f => f.surface === 'bay').map(f => f.ring);
  const medians = c.w.ground.medians.filter(m => dist(m.ring[1]!, m.ring[2]!) >= std.refuge - TOL);
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
      const block = stripBlock(x.strip, tracks, envelope, bays);
      if (block && !(block === 'a tram track' ? std.across?.track : std.across?.bay)?.includes(x.kind)) return `strip crosses ${block} and the plan does not say a ${x.kind} crossing may`;
      if (block === 'a tram track' && x.kind === 'kerb' && x.control !== 'signal') return 'strip crosses a tram track and the crossing has no signal control (the signal stops the tram)';
      const want = x.kind === 'kerb' ? medians.map(m => clipConvex(x.strip, m.ring)).filter(r => areaOf(r) > 0.25) : [];
      const sum = (rs: number[][][]) => rs.reduce((t, r) => t + areaOf(r as [number, number][]), 0);
      if (want.length !== x.refuges.length || Math.abs(sum(want) - sum(x.refuges)) > 0.05) return `has ${x.refuges.length} refuges, the median it crosses needs ${want.length}`;
      return partProblem(c, gc, look, x, parts, ground, arcsAll);
    })();
    if (why) bad.push(`${x.id} ${why}`);
  }
  bad.push(...arcOverlaps(arcsAll));
  return [rule('SF-20', 'Streetscape', 'A crossing is two landings and a strip between their kerbs, a refuge where it crosses a median of refuge width, no strip across a tram track or a bay the plan does not allow, '
    + 'and the parts the plan gives it (light bars or dots, a signal orb at each kerb end and refuge, studs on the kerb edge, an inlay arc), each in one crossing, on pavement or road and never on a lawn', landings.length + crossings.length + parts.size, bad, 'none', SOURCE20)];
}

const SOURCE22 = 'streetscape.crossings.coverage.rules (arms, island, mall, midBlock, dedupe) · briefs/009/crossings-proposal (Datum 2026-10-15, approved by Sky) · Global Street Design Guide, pedestrian crossings (a crossing where there is a desire line, every 80-100 m, never over 200 m)';

/** SF-22, on a plan that carries streetscape.crossings.coverage.rules: the rules hold in the export. (a) No twins: no two kerb crossings the plot edges give, of one road, less than `mouth` m apart across a side road's mouth or `bend` m apart round a bend. (b) Every site the
 *  rules name (a primary through road's arms, the circus ring's footway lines to the island, a mall's walk bands across the road it ends at) is a crossing of its road within `reach` m of where the rule puts it, or is named in the export's `crossingRules.left` with
 *  why no ground stood free for it. (c) The export's log names only sites the rules make. A rule that cannot be met is seen here, never silent. */
function coverageRule(c: WCtx): Check[] {
  const gc = ctxOf(c.plan, c.d, c.civ), rules = rulesOf(gc), log = c.w.crossingRules; if (!rules || !log) return [];
  const bad: string[] = [], xs = c.w.crossings ?? [], kerb = xs.filter(x => x.kind === 'kerb'), plain = kerb.filter(x => x.landings.length === 2 && !x.landings.some(l => /^street\/(arm|island|mall|mid)\//.test(l.id))); // a site is the rules' own: the island's two crossings either side of a mouth are two footways, not twins
  const pairs = plain.map(x => ({ a: { id: x.landings[0]!.id, road: x.road, cls: '', flank: [] }, b: { id: x.landings[1]!.id, road: x.road, cls: '', flank: [] }, ka: x.line[0], kb: x.line[1], x }) as Pair & { x: typeof kerb[number] });
  for (const [p, why] of dedupe(gc, pairs, () => 0, rules)) bad.push(`${(p as Pair & { x: { id: string } }).x.id} is ${why}`);
  const mid = (x: { line: V[] }): V => [(x.line[0]![0] + x.line[1]![0]) / 2, (x.line[0]![1] + x.line[1]![1]) / 2], reach = (rules.reach ?? 4) + 0.5;
  const seeds = [...armSeeds(gc, rules), ...islandSeeds(gc, rules), ...mallSeeds(gc, rules)];
  for (const s of seeds) {
    const m: V = [(s.ka[0] + s.kb[0]) / 2, (s.ka[1] + s.kb[1]) / 2];
    if (!kerb.some(x => x.road === s.road && dist(mid(x), m) <= reach) && !log.left.some(l => l.startsWith(`${s.id}:`))) bad.push(`${s.id} (${s.why}) has no crossing within ${reach - 0.5} m and is not named as left out`);
  }
  for (const l of log.left) if (!l.startsWith('mid/') && !seeds.some(s => l.startsWith(`${s.id}:`))) bad.push(`the export names '${l.split(':')[0]}' as left out, and no rule makes that site`);
  return [rule('SF-22', 'Streetscape', 'The crossing coverage rules hold: no twin crossings across a side road\'s mouth or round a bend, and every arm, island and mall site the rules name is built or named as left out',
    kerb.length + seeds.length, bad, 'none', SOURCE22)];
}
