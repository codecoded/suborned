// The civic block's ground solids as a walker meets them (brief 008 B3, Waymark's walk measurement): the terrace wall (0.66 m), the stair cheeks (0.26 m),
// the pilasters, the planting beds, the tables, the fountain. The walk's 2 m grid marks a cell blocked by its centre, which a 0.66 m wall almost never holds,
// so a step is tested against the solids' own edges: it may not touch a solid, nor pass within BODY of one. Only a block with a `dressing` has solids (civic.ts),
// so every other plan, and every recorded run, walks as it always did.
import { type V, inPoly, segDist, segHit } from './geom';

/** The body clearance the plan states (standards.walking.body.r, W-5): one value for every walk the game measures. A plan that states none walks with BODY. */
export const bodyOf = (plan: { standards: { walking?: { body?: { r?: number } } } }): number => plan.standards.walking?.body?.r ?? BODY;

/** The clearance (m) a walker keeps from a solid. Calibrated on the two walks Waymark measured in the game, Crew line → Between tables 26.2 m and
 *  Service lane → Office entrance 25.5 m: one clearance fits both (0.65 gives 26.1 and 25.5; the walls and beds alone, with none, give 24.7 and 24.4). */
export const BODY = 0.65;

export type Edge = { a: V; b: V; x0: number; x1: number; y0: number; y1: number };

export class Solids {
  private edges: Edge[] = [];
  private boxes: { poly: V[]; x0: number; x1: number; y0: number; y1: number }[] = [];
  constructor(polys: V[][] = [], readonly r = BODY) {
    for (const poly of polys) {
      const xs = poly.map(p => p[0]), ys = poly.map(p => p[1]);
      this.boxes.push({ poly, x0: Math.min(...xs), x1: Math.max(...xs), y0: Math.min(...ys), y1: Math.max(...ys) });
      poly.forEach((a, i) => { const b = poly[(i + 1) % poly.length]!; this.edges.push({ a, b, x0: Math.min(a[0], b[0]), x1: Math.max(a[0], b[0]), y0: Math.min(a[1], b[1]), y1: Math.max(a[1], b[1]) }); });
    }
  }
  get any(): boolean { return this.edges.length > 0; }
  get list(): Edge[] { return this.edges; }

  /** The edges that can touch a box once it is grown by `reach` (and the clearance). */
  near(x0: number, x1: number, y0: number, y1: number, reach = 0): Edge[] {
    const g = this.r + reach;
    return this.edges.filter(e => e.x1 >= x0 - g && e.x0 <= x1 + g && e.y1 >= y0 - g && e.y0 <= y1 + g);
  }

  /** True if p stands inside a solid. */
  inside(p: V): boolean { return this.boxes.some(b => p[0] >= b.x0 && p[0] <= b.x1 && p[1] >= b.y0 && p[1] <= b.y1 && inPoly(p, b.poly)); }

  /** True if the segment a→b keeps BODY clear of the edges `es` and does not run inside a solid. */
  clear(a: V, b: V, es: Edge[] = this.edges): boolean {
    const x0 = Math.min(a[0], b[0]) - this.r, x1 = Math.max(a[0], b[0]) + this.r, y0 = Math.min(a[1], b[1]) - this.r, y1 = Math.max(a[1], b[1]) + this.r;
    for (const e of es) {
      if (e.x1 < x0 || e.x0 > x1 || e.y1 < y0 || e.y0 > y1) continue;
      if (segHit(a, b, e.a, e.b) || Math.min(segDist(a, e.a, e.b), segDist(b, e.a, e.b), segDist(e.a, a, b), segDist(e.b, a, b)) < this.r) return false;
    }
    return !this.inside(a) && !this.inside(b);
  }
}
