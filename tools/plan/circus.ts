// The Crown Circus pavement: the ring between the gyratory's outer edge and the plot arcs, with each road entry cut out
// (at the arm's kerb line), as the CAD sheet draws it. One simple polygon per sector between arms.
import { type V, add, dist, dot, isClosed, left, mul, norm, offsetLine, polar, ribbon, sub, circlePoly, unfold } from './geom';
import type { Derived, Plan } from './plan';

const TAU = 2 * Math.PI, EPS = 1e-3;
const ang = (c: V, p: V) => { const a = Math.atan2(p[1] - c[1], p[0] - c[0]); return a < 0 ? a + TAU : a; };
/** Angle of p ahead of a, counter-clockwise, in [0, 2π). */
const ahead = (a: number, p: number) => { const d = (p - a) % TAU; return d < 0 ? d + TAU : d; };

/** Plot arcs on the circle of radius r: [from, to] ccw angle ranges of each run of on-circle vertices. */
function plotArcs(c: V, r: number, plots: V[][]): { pts: V[]; a0: number; span: number }[] {
  const out: { pts: V[]; a0: number; span: number }[] = [];
  for (const poly of plots) {
    const on = poly.map(p => Math.abs(dist(p, c) - r) < EPS), n = poly.length;
    if (!on.some(Boolean) || on.every(Boolean)) continue;
    const s = on.findIndex((v, i) => v && !on[(i + n - 1) % n]); // first vertex of the run
    const run: V[] = [];
    for (let k = 0; on[(s + k) % n]; k++) run.push(poly[(s + k) % n]);
    const [a, b] = [ang(c, run[0]), ang(c, run[run.length - 1])];
    // the run may be listed either way round; orient it ccw
    const ccw = ahead(a, b) < Math.PI;
    const pts = ccw ? run : [...run].reverse();
    out.push({ pts, a0: ang(c, pts[0]), span: ahead(ang(c, pts[0]), ang(c, pts[pts.length - 1])) });
  }
  return out;
}

/** Pavement pieces: for every road that enters the ring, cut its mouth (half width `hw + kerb`). */
export function circusPavement(plan: Plan, d: Derived, kerb: number): V[][] {
  const { c, ring, pavement: R } = plan.circus, rIn = ring[1], N = 48;
  const inner = circlePoly(c, rIn, N);
  const arms = d.roads.filter(r => !isClosed(r.line)).flatMap(r => {
    const [a, b] = [rIn, R].map(k => r.line.find(p => dist(p, c) >= k - EPS && dist(p, c) <= k + 2.5)) as (V | undefined)[];
    // an arm: its centreline crosses the whole band, i.e. has points inside the ring and beyond the pavement
    if (!a || !b || !r.line.some(p => dist(p, c) < rIn)) return [];
    const d0 = norm(sub(b, a)), u = dot(d0, sub(a, c)) >= 0 ? d0 : mul(d0, -1); // outward
    return [{ u, n: left(u), h: r.w / 2 + kerb, th: ang(c, add(c, u)) }];
  }).sort((p, q) => p.th - q.th);
  const arcs = plotArcs(c, R, d.plots.map(p => p.polyV));
  const inArc = (a: number) => arcs.some(x => ahead(x.a0, a) > EPS && ahead(x.a0, a) < x.span - EPS);
  const outerAt = (ar: { u: V; n: V; h: number }, side: 1 | -1): V => add(c, add(mul(ar.u, Math.sqrt(R * R - ar.h * ar.h)), mul(ar.n, side * ar.h)));
  const innerAt = (ar: { u: V; n: V; h: number }, side: 1 | -1): V => {
    for (let i = 0; i < N; i++) {
      const p = inner[i], q = inner[(i + 1) % N], sp = dot(sub(p, c), ar.n) - side * ar.h, sq = dot(sub(q, c), ar.n) - side * ar.h;
      if (sp * sq > 0 || sp === sq) continue;
      const x = add(p, mul(sub(q, p), sp / (sp - sq)));
      if (dot(sub(x, c), ar.u) > 0) return x;
    }
    throw new Error('circus arm does not cross the gyratory edge');
  };
  if (!arms.length) return [[...circlePoly(c, R, N)]];
  return arms.map((a, i) => {
    const b = arms[(i + 1) % arms.length];
    const aIn = innerAt(a, 1), aOut = outerAt(a, 1), bIn = innerAt(b, -1), bOut = outerAt(b, -1);
    const lo = ang(c, aOut), span = ahead(lo, ang(c, bOut));
    const within = (p: V, from: number, to: number) => ahead(from, ang(c, p)) > EPS && ahead(from, ang(c, p)) < ahead(from, to) - EPS;
    // inner edge ccw from aIn to bIn, outer edge ccw from aOut to bOut (plot vertices where plots stand, else a regular sampling)
    const inPts = inner.filter(p => within(p, ang(c, aIn), ang(c, bIn))).sort((p, q) => ahead(ang(c, aIn), ang(c, p)) - ahead(ang(c, aIn), ang(c, q)));
    const outPts: V[] = [
      ...circlePoly(c, R, 192).filter(p => within(p, lo, lo + span) && !inArc(ang(c, p))),
      ...arcs.flatMap(x => x.pts).filter(p => within(p, lo, lo + span)),
    ].sort((p, q) => ahead(lo, ang(c, p)) - ahead(lo, ang(c, q)));
    return [aIn, ...inPts, bIn, bOut, ...outPts.reverse(), aOut];
  });
}

/** Where a polyline first enters the circle (c, r) walking from its start: the crossing point and the index of the segment's far end. */
function enter(line: V[], c: V, r: number): { p: V; i: number } | null {
  for (let i = 1; i < line.length; i++) {
    const [a, b] = [line[i - 1], line[i]];
    if (dist(a, c) < r - EPS || dist(b, c) >= r - EPS) continue;
    const u = sub(b, a), f = sub(a, c), A = dot(u, u), B = 2 * dot(f, u), K = dot(f, f) - r * r;
    const t = (-B - Math.sqrt(B * B - 4 * A * K)) / (2 * A);
    return { p: add(a, mul(u, t)), i };
  }
  return null;
}

/** A strip of half width `hw` along `line`, ended where it meets the circus outer carriageway edge (circle radius ring[1]):
 *  each side is cut at the circle and the end follows the circle. An end lying outside the circle by at most `reach` is
 *  first extended to it. `rim` keeps the end that far outside the edge (so rounded exports stay clear). Lines that do not reach the ring keep the plain ribbon. */
export function ribbonToRing(plan: Plan, pts: V[], hw: number, reach = 0, rim = 0): V[] {
  const { c, ring: rg } = plan.circus, R = rg[1] + rim;
  const inside = (p: V) => dist(p, c) < R - EPS;
  const near = (p: V) => dist(p, c) >= R - EPS && dist(p, c) <= R + reach;
  const push = (line: V[]): V[] => { // extend the last point into the ring along the last segment
    const [a, b] = [line[line.length - 2], line[line.length - 1]];
    return [...line, add(b, mul(norm(sub(b, a)), dist(b, c) - R + 1))];
  };
  let line = inside(pts[pts.length - 1]) && !inside(pts[0]) ? pts : inside(pts[0]) && !inside(pts[pts.length - 1]) ? [...pts].reverse() : null;
  if (!line && reach > 0) {
    const last = near(pts[pts.length - 1]), first = near(pts[0]);
    if (last !== first) line = last ? push(pts) : push([...pts].reverse());
  }
  if (!line) return ribbon(pts, hw);
  const sideL = unfold(offsetLine(line, hw)), sideR = unfold(offsetLine(line, -hw));
  const [eL, eR] = [enter(sideL, c, R), enter(sideR, c, R)];
  if (!eL || !eR) return ribbon(pts, hw);
  // the arc's vertices sit on the same absolute 192-gon as the circus pavement's outer edge (circlePoly(c, r, 192)), so a
  // strip ended here shares its arc with the neighbouring pavement instead of leaving chord slivers between two samplings
  const a0 = ang(c, eL.p), span = ahead(a0, ang(c, eR.p)), signed = span > Math.PI ? span - TAU : span, step = TAU / 192, dir = Math.sign(signed);
  const [lo, hi] = dir > 0 ? [a0, a0 + signed] : [a0 + signed, a0], ks: number[] = [];
  for (let k = Math.ceil(lo / step - 1e-9); k * step < hi - 1e-9; k++) ks.push(k);
  const arc = (dir > 0 ? ks : ks.reverse()).map(k => polar(c, R, k * step)).filter(p => dist(p, eL.p) > EPS && dist(p, eR.p) > EPS);
  return [...sideL.slice(0, eL.i), eL.p, ...arc, eR.p, ...sideR.slice(0, eR.i).reverse()];
}

/** A road's reservation (tram strip) polygon, ended at the circus outer carriageway edge. */
export const reservationPoly = (plan: Plan, r: { line: V[]; reservation?: number }): V[] => ribbonToRing(plan, r.line, (r.reservation ?? 0) / 2);

/** A median zone quad ([p0+n, p1+n, p1-n, p0-n]) ended at the circus outer carriageway edge when it stops within 3 m of it. */
export function medianPoly(plan: Plan, q: V[]): V[] {
  const mid = (a: V, b: V): V => [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2];
  return ribbonToRing(plan, [mid(q[0], q[3]), mid(q[1], q[2])], dist(q[0], q[3]) / 2, 3, 0.002);
}
