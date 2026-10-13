// SF-19: a crossing is streetscape.crossings.w wide along the way it crosses. Latch's walk found
// street/avenue-court/e18/crossing-1076, size [4, 1.8, 0.02], empty: the generator wrote the width into the box's first
// axis, which runs along the item's yaw, and the yaw is across the road, so the 4 m ran out over the carriageway and
// the pavement while the 1.8 m deep patch stood sideways along the kerb. The export's size is [depth along yaw, width
// across it, thickness]; the rule measures the box along the kerb its plot edge stands on, the tram platform it ends
// or the sky rail line its bank stands against, from the plan and the export, not from the item's own yaw.
import type { Check } from '../../plan/checks';
import type { V } from '../../plan/geom';
import { TOL, type WCtx, itemRing, rule } from '../../plan/world-ctx';
import { ctxOf } from '../gen/common';
import { plotRing } from '../gen/edges';
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

export function crossingRules(c: WCtx): Check[] {
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
