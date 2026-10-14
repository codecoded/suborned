// The carriageway axis at a crossing: the direction of the road, tram platform or sky rail line it crosses. SF-19 measures a crossing's width along it; SF-20 holds every light piece's long axis off it by 30 degrees or more.
import type { V } from '../../plan/geom';
import type { WCtx } from '../../plan/world-ctx';
import { ctxOf } from '../gen/common';
import { plotRing } from '../gen/edges';
import { nearestLine } from './line';

/** The direction (radians) of the carriageway a crossing crosses (the axis of the road, platform or line it crosses; the walk runs across it), or null where the export holds nothing to measure against. */
export function roadYaw(c: WCtx, s: { edge: string; at: V }): number | null {
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
