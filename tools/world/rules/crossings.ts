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
import { plotRing } from '../gen/edges';
import { areaOf, clipConvex, stripBlock } from '../gen/street/crossings';
import { lawn, levelAt } from '../gen/street/ground';
import { nearestLine } from './line';

const SOURCE = 'streetscape.crossings.w (4 m, near edge ≥ 3 m past the corner tangent; bollards flank at ± 2.4) · '
  + 'brief 007 round 3 walk, note 5 (street/avenue-court/e18/crossing-1076)';

/** The direction (radians) of the way a crossing crosses, or null where the export holds nothing to measure against. */
function wayYaw(c: WCtx, s: { edge: string; at: V }): number | null {
  const kerb = /^(.+)\/e(\d+)$/.exec(s.edge);
  if (kerb) { // a kerb: the plot edge it stands on, which runs along the road
    const plot = c.d.plots.find(p => p.id === kerb[1]);
    if (!plot) return null;
    const ring = plotRing(ctxOf(c.plan, c.d, c.civ), plot).p, i = Number(kerb[2]);
    const a = ring[i], b = ring[(i + 1) % ring.length];
    return a && b ? Math.atan2(b[1] - a[1], b[0] - a[0]) : null;
  }
  const stop = c.w.transit.stops.find(m => m.id === s.edge); // a tram stop: the platform's long axis
  if (stop) {
    const xs = stop.ring.map(p => p[0]), ys = stop.ring.map(p => p[1]);
    return Math.max(...xs) - Math.min(...xs) > Math.max(...ys) - Math.min(...ys) ? 0 : Math.PI / 2;
  }
  const rails = c.w.transit.skyRail.map(r => r.line); // a station bank: the sky rail line it stands against
  return rails.length ? nearestLine(rails, s.at).yaw : null;
}

export const crossingRules = (c: WCtx): Check[] => [...sizeRule(c), ...(c.S.crossings.strip ? stripRule(c) : [])];

function sizeRule(c: WCtx): Check[] {
  const crossings = c.w.street.filter(s => s.kind === 'crossing'), W = c.S.crossings.w as number, bad: string[] = [];
  for (const s of crossings) {
    const yaw = wayYaw(c, s);
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

const SOURCE20 = 'streetscape.crossings.strip (stagger, refuge, control, across) · Sky\'s walk of 2026-10-14, note 1 (a crossing is kerb to kerb: '
  + 'two landings and a strip across the carriageway between them)';

/** SF-20, on a plan that carries streetscape.crossings.strip: every crossing patch belongs to one crossing; a kerb crossing is two landings on opposite
 *  kerbs of its road and the strip between their kerb edges; the strip crosses no tram track and no bay unless the plan allows its kind; it has a refuge
 *  where it crosses a median of refuge width, and the control the plan gives the road's class. */
function stripRule(c: WCtx): Check[] {
  const std = c.S.crossings.strip as { refuge: number; control?: Record<string, string>; across?: { track?: string[]; bay?: string[] } }, W = c.S.crossings.w as number;
  const crossings = c.w.crossings ?? [], patches = c.w.street.filter(s => s.kind === 'crossing'), byId = new Map(patches.map(s => [s.id, s]));
  const uses = new Map<string, number>(), bad: string[] = [];
  for (const x of crossings) for (const id of x.items) uses.set(id, (uses.get(id) ?? 0) + 1);
  for (const s of patches) {
    const n = uses.get(s.id) ?? 0;
    if (n !== 1) bad.push(`${s.id} is a landing with no partner (in ${n} crossings)`);
  }
  const gc = ctxOf(c.plan, c.d, c.civ);
  for (const s of patches) { // a patch lies at the level of the surface under it and never on a lawn (Latch's rebuild, notes 2 and 3)
    const level = levelAt(gc, s.at);
    if (Math.abs(s.z - level) > TOL) bad.push(`${s.id} is at z ${s.z} over a surface at ${level}`);
    const on = samples9(itemRing(s)).find(p => lawn(gc, p));
    if (on) bad.push(`${s.id} stands on the lawn at (${on[0].toFixed(1)}, ${on[1].toFixed(1)}): a landing stands on pavement, a path or a platform`);
  }
  const envelope = c.plan.standards.tram.trackCentres / 2 + 0.5, tracks = c.w.transit.tram.map(t => t.line), bays = c.w.ground.flats.filter(f => f.surface === 'bay').map(f => f.ring);
  const medians = c.w.ground.medians.filter(m => dist(m.ring[1]!, m.ring[2]!) >= std.refuge - TOL);
  for (const x of crossings) {
    const its = x.items.map(id => byId.get(id)), why = ((): string => {
      if (its.some(s => !s)) return 'names a street item that is not a crossing patch';
      if (x.kind === 'kerb') {
        if (its.length !== 2) return `has ${its.length} landings, needs 2`;
        const off = its.map((s, i) => { const r = itemRing(s!); return inPoly(x.line[i]!, r) ? 0 : lineDist(x.line[i]!, [...r, r[0]!]); }); // the strip starts on the landing
        if (off.some(d => d > 0.02)) return `strip does not start on its landings (${off.map(d => d.toFixed(2)).join(', ')} m off)`;
        const road = c.d.roads.find(r => r.id === x.road);
        if (!road) return `crosses ${x.road || 'no road'}, which is not in the plan`;
        if (Math.abs(dist(x.line[0], x.line[1]) - road.w) > 1.5) return `strip is ${dist(x.line[0], x.line[1]).toFixed(1)} m long on a ${road.w} m road`;
        const control = std.control?.[road.class];
        if (x.control !== control) return `control is ${x.control ?? 'none'}, the plan gives ${road.class} roads ${control ?? 'none'}`;
      } else if (its.length !== 1) return `has ${its.length} patches, needs 1`;
      if (Math.abs(x.width - W) > TOL) return `strip is ${x.width} m wide, needs ${W}`;
      const block = stripBlock(x.strip, tracks, envelope, bays);
      if (block && !(block === 'a tram track' ? std.across?.track : std.across?.bay)?.includes(x.kind)) return `strip crosses ${block} and the plan does not say a ${x.kind} crossing may`;
      const want = x.kind === 'kerb' ? medians.map(m => clipConvex(x.strip, m.ring)).filter(r => areaOf(r) > 0.25) : [];
      const sum = (rs: number[][][]) => rs.reduce((t, r) => t + areaOf(r as [number, number][]), 0);
      if (want.length !== x.refuges.length || Math.abs(sum(want) - sum(x.refuges)) > 0.05) return `has ${x.refuges.length} refuges, the median it crosses needs ${want.length}`;
      return '';
    })();
    if (why) bad.push(`${x.id} ${why}`);
  }
  return [rule('SF-20', 'Streetscape', 'A crossing is two landings and a strip between their kerbs, a refuge where it crosses a median of refuge width, '
    + 'a patch at the level of the surface it lies on and never on a lawn, and no strip across a tram track or a bay the plan does not allow', patches.length + crossings.length, bad, 'none', SOURCE20)];
}
