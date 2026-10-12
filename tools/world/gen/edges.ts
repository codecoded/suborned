// Which street each plot edge faces, and the plot's building line: the plot edge set back by the street section's
// pavement (standards.streetscape.sections[class].pavement). Edge class strings are `kind` or `kind:id`; kinds are
// primary | district | access (roads), circus, mall | walk (foot paths), quay, park (public ground), party (a neighbour's wall), none.
import { type V, add, dist, lineDist, mul, segDist } from '../../plan/geom';
import type { Plot } from '../../plan/plan';
import type { Ctx } from './common';
import { type Ring, areaOf, ccw, clipHalf, edgeN, pointInRing } from './geo';

export const kindOf = (c: string) => c.split(':')[0]!;
export const idOf = (c: string) => c.split(':')[1] ?? '';
/** Edge kinds a trade may front onto (standards.frontage.placement.streets); 'circus' fronts as a district road. */
export const frontKind = (c: string) => { const k = kindOf(c); return k === 'circus' ? 'district' : k; };
export const STREET_KINDS = ['district', 'circus', 'access', 'mall', 'walk', 'quay', 'park'];

const TOL = 1.5;
const PUBLIC = new Set(['park', 'open', 'square']);

type Probe = (p: V, self?: string) => string;
const probes = new WeakMap<Ctx, Probe>();

/** Classify a point on a plot edge by what lies against it. `self` is the plot being classified (never its own neighbour). */
function probeFor(ctx: Ctx): Probe {
  const hit = probes.get(ctx); if (hit) return hit;
  const { plan, d } = ctx, c = plan.circus, quay = plan.quay;
  const fn: Probe = (p, self = '') => {
    let best = TOL, out = '';
    const take = (gap: number, c: string) => { if (Math.abs(gap) <= best) { best = Math.abs(gap); out = c; } };
    for (const r of d.roads) if (!r.gyratory) take(lineDist(p, r.line) - r.w / 2, `${r.class}:${r.id}`);
    take(dist(p, c.c) - c.pavement, 'circus:crown-circus'); // notched plots stop at the circus pavement edge (r 51)
    for (const f of plan.foot) take(lineDist(p, f.pts) - f.w / 2, `${f.kind === 'mall' ? 'mall' : 'walk'}:${f.id}`);
    for (const q of quay) take(Math.min(...q.map((a, i) => segDist(p, a, q[(i + 1) % q.length]!))), 'quay');
    if (out) return out;
    for (const pl of d.plots) if (pl.id !== self && pointInRing(p, pl.polyV)) return PUBLIC.has(pl.use) || pl.civic ? `park:${pl.id}` : `party:${pl.id}`;
    return 'none';
  };
  probes.set(ctx, fn);
  return fn;
}

/** The plot as a CCW ring with every edge classified; edges are split where the facing street changes. */
const rings = new WeakMap<Ctx, Map<string, Ring>>();
export function plotRing(ctx: Ctx, plot: Plot & { polyV: V[] }): Ring {
  const m = rings.get(ctx) ?? rings.set(ctx, new Map()).get(ctx)!, hit = m.get(plot.id); // pure in (ctx, plot): classified once
  if (hit) return hit;
  const r = plotRingOf(ctx, plot); m.set(plot.id, r);
  return r;
}
function plotRingOf(ctx: Ctx, plot: Plot & { polyV: V[] }): Ring {
  const classify = probeFor(ctx), pts = ccw(plot.polyV), p: V[] = [], c: string[] = [];
  const n = pts.length;
  for (let i = 0; i < n; i++) {
    const a = pts[i]!, b = pts[(i + 1) % n]!, L = dist(a, b), k = Math.max(1, Math.ceil(L / 12)), nrm = edgeN(a, b);
    const cls: string[] = [];
    for (let s = 0; s < k; s++) {
      const m = add(a, mul([b[0] - a[0], b[1] - a[1]], (s + 0.5) / k));
      cls.push(classify(add(m, mul(nrm, 0.05)), plot.id));
    }
    // runs of one class become one edge; a run shorter than 8 m joins its predecessor
    const runs: { c: string; n: number }[] = [];
    for (const x of cls) { const last = runs[runs.length - 1]; if (last && last.c === x) last.n++; else runs.push({ c: x, n: 1 }); }
    for (let r = 1; r < runs.length; r++) if ((runs[r]!.n * L) / k < 8) { runs[r - 1]!.n += runs[r]!.n; runs.splice(r, 1); r--; }
    let at = 0;
    for (const run of runs) { p.push(add(a, mul([b[0] - a[0], b[1] - a[1]], at / k))); c.push(run.c); at += run.n; }
  }
  return { p, c };
}

/** Pavement width at an edge: the section's pavement for roads; the circus is carved already, malls, walks and quays carry their own bands. */
export function pavementOf(ctx: Ctx, cls: string): number {
  const k = kindOf(cls), s = ctx.S.sections;
  return k === 'primary' || k === 'district' || k === 'access' ? s[k].pavement : 0;
}

/** The building line: the plot cut back along each street edge by that edge's pavement (half-plane clips, so arcs and
 *  edges against neighbours stay as they are). The edge the cut makes keeps the class of the street it faces. */
export function buildingLine(ctx: Ctx, ring: Ring): Ring | null {
  let r: Ring | null = ring;
  ring.p.forEach((a, i) => {
    const d = pavementOf(ctx, ring.c[i]!);
    if (!r || d <= 0) return;
    const b = ring.p[(i + 1) % ring.p.length]!, o = edgeN(a, b);
    r = clipHalf(r, add(a, mul(o, -d)), mul(o, -1), ring.c[i]);
  });
  return r && areaOf(r.p) > 20 ? r : null;
}
