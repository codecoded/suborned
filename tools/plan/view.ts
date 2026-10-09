// V-1: can the player see each Handover mission point from the play camera? Orthographic, so the view direction is
// constant: a ray from the point (1 m above ground) toward the camera, tested against built plot prisms and the
// Handover buildings and structures.
import { type V, dist, inPoly, segHit } from './geom';
import type { Derived, Plan } from './plan';
import type { Civic } from './civic';

export const EYE = 1, PAVEMENT = .18, UNBUILT = ['park', 'square', 'open'];
export type Hit = { id: string; margin: number };
export type Seen = { name: string; at: V; plot?: Hit; others: Hit[] };

/** Ray from p along unit u (plan frame), rising at tan(pitch) per metre; margin of its height over the prism top at the
 *  point it enters the prism (negative = hidden behind it). null when it never meets the prism's height band. */
function margin(p: V, u: V, poly: V[], base: number, top: number, tan: number): number | null {
  const far = 1e4, q: V = [p[0] + u[0] * far, p[1] + u[1] * far];
  const ts = poly.flatMap((a, i) => { const x = segHit(p, q, a, poly[(i + 1) % poly.length]); return x ? [dist(p, x)] : []; }).sort((a, b) => a - b);
  const inside = inPoly(p, poly);
  const t0 = inside ? 0 : ts[0], t1 = inside ? (ts[0] ?? 0) : ts[ts.length - 1];
  if (t0 === undefined) return null;
  if (EYE + t1 * tan < base) return null; // passes under it
  return EYE + t0 * tan - top;
}

export function visibility(plan: Plan, d: Derived, civ: Civic): Seen[] {
  const yaw = (plan.view.yaw * Math.PI) / 180, tan = Math.tan((plan.view.pitch * Math.PI) / 180);
  const u: V = [-Math.sin(yaw), -Math.cos(yaw)]; // toward the camera: opposite the look direction (yaw compass degrees from north)
  const plots = d.plots.filter(p => !p.civic && !UNBUILT.includes(p.use) && p.height).map(p => ({ id: p.id, poly: p.polyV, base: PAVEMENT, top: PAVEMENT + p.height! }));
  const things = [...civ.buildings, ...civ.structures].map(b => ({ id: b.id, poly: b.poly, base: PAVEMENT + b.z, top: PAVEMENT + b.z + b.height }));
  return civ.points.map(pt => {
    const hits = (cs: typeof plots) => cs.flatMap(c => { const m = margin(pt.p, u, c.poly, c.base, c.top, tan); return m === null ? [] : [{ id: c.id, margin: m }]; }).sort((a, b) => a.margin - b.margin);
    return { name: pt.name, at: pt.p, plot: hits(plots)[0], others: hits(things).filter(h => h.margin < 0) };
  });
}
