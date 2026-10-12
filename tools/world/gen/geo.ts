// Polygon helpers for the generators. A Ring is a simple polygon, counter-clockwise, with a class per edge (the street
// kind it faces: primary, district, access, mall, walk, quay, park, party, none). Clipping keeps the classes; edges the
// clip creates are 'party'.
import { type V, add, dist, dot, len, mul, norm, sub, cross } from '../../plan/geom';

export type Ring = { p: V[]; c: string[] };
export type Rect = [number, number, number, number];

export const areaOf = (p: V[]) => p.reduce((s, a, i) => s + cross(a, p[(i + 1) % p.length]!), 0) / 2;
export const bbox = (p: V[]): Rect => [Math.min(...p.map(a => a[0])), Math.max(...p.map(a => a[0])), Math.min(...p.map(a => a[1])), Math.max(...p.map(a => a[1]))];
export const rectCentre = (r: Rect): V => [(r[0] + r[1]) / 2, (r[2] + r[3]) / 2];
export const rectPts = ([x0, x1, y0, y1]: Rect): V[] => [[x0, y0], [x1, y0], [x1, y1], [x0, y1]];
export const inRect = (p: V, r: Rect, m = 0) => p[0] >= r[0] - m && p[0] <= r[1] + m && p[1] >= r[2] - m && p[1] <= r[3] + m;
export const rectsTouch = (a: Rect, b: Rect, m = 0) => a[0] <= b[1] + m && b[0] <= a[1] + m && a[2] <= b[3] + m && b[2] <= a[3] + m;
export const growRect = (r: Rect, d: number): Rect => [r[0] - d, r[1] + d, r[2] - d, r[3] + d];
export const rectArea = (r: Rect) => (r[1] - r[0]) * (r[3] - r[2]);
export const ccw = (p: V[]): V[] => (areaOf(p) < 0 ? [...p].reverse() : p);
export const edgeN = (a: V, b: V): V => { const u = norm(sub(b, a)); return [u[1], -u[0]]; }; // outward normal of a CCW ring edge

function insetOnce(p: V[], d: number[]): V[] {
  const n = p.length;
  const L = p.map((a, i) => { const b = p[(i + 1) % n]!, u = norm(sub(b, a)); return { o: add(a, mul([-u[1], u[0]], d[i]!)), u }; });
  return p.map((q, i) => {
    const A = L[(i + n - 1) % n]!, B = L[i]!, den = cross(A.u, B.u);
    if (Math.abs(den) < 1e-6) return add(q, mul([-B.u[1], B.u[0]], d[i]!));
    return add(A.o, mul(A.u, cross(sub(B.o, A.o), B.u) / den));
  });
}

/** Ring with `d[i]` metres taken off edge i: mitred where neighbours meet; an edge the offset swallows (it would turn round) is dropped.
 *  Edge classes follow the surviving edges. Null if it collapses. */
export const insetRing = (r: Ring, d: number[]) => insetBoth(r, d)?.inner ?? null;
/** As insetRing, with the outer ring as it stands once swallowed edges are dropped (vertex i of each is the same corner). */
export function insetBoth(r: Ring, d: number[]): { outer: Ring; inner: Ring } | null {
  let p = [...r.p], c = [...r.c], dd = [...d];
  for (let it = 0; it < 40 && p.length >= 3; it++) {
    const out = insetOnce(p, dd), n = p.length;
    const flip = out.findIndex((q, i) => dot(sub(out[(i + 1) % n]!, q), sub(p[(i + 1) % n]!, p[i]!)) < -1e-6);
    if (flip < 0) { const a0 = areaOf(r.p), a1 = areaOf(out); return a1 > 1 && a1 <= a0 + 1e-6 && isSimple(out) ? { outer: { p, c }, inner: { p: out, c } } : null; }
    const j = (flip + 1) % n; // drop edge `flip`: its end vertex goes, the next edge starts at its start
    p = p.filter((_, i) => i !== j); c = c.filter((_, i) => i !== flip); dd = dd.filter((_, i) => i !== flip);
  }
  return null;
}
/** Points-only form of insetRing. */
export const inset = (p: V[], d: number[]): V[] | null => insetRing({ p, c: p.map(() => '') }, d)?.p ?? null;

/** Keep the side of the line through `a` with normal `nrm` where dot(p - a, nrm) >= 0. */
export function clipHalf(r: Ring, a: V, nrm: V, cut = 'party'): Ring | null {
  const n = r.p.length, side = (q: V) => dot(sub(q, a), nrm), segs: { a: V; b: V; c: string }[] = [];
  for (let i = 0; i < n; i++) {
    const s = r.p[i]!, e = r.p[(i + 1) % n]!, ds = side(s), de = side(e), c = r.c[i]!;
    if (ds >= 0 && de >= 0) segs.push({ a: s, b: e, c });
    else if (ds >= 0 || de >= 0) {
      const t = ds / (ds - de), x = add(s, mul(sub(e, s), t));
      segs.push(ds >= 0 ? { a: s, b: x, c } : { a: x, b: e, c });
    }
  }
  const segs2 = segs.filter(s => dist(s.a, s.b) > 1e-6);
  if (segs2.length < 3) return null;
  const p: V[] = [], c: string[] = [];
  segs2.forEach((s, i) => {
    p.push(s.a); c.push(s.c);
    const nx = segs2[(i + 1) % segs2.length]!;
    if (dist(s.b, nx.a) > 1e-4) { p.push(s.b); c.push(cut); }
  });
  return areaOf(p) > 1e-3 ? { p, c } : null;
}

/** `r` minus the rect: up to four pieces (west, east, south, north of it), classes kept; cut edges are 'party'. */
export function subtractRect(r: Ring, [x0, x1, y0, y1]: Rect): Ring[] {
  const bb = bbox(r.p);
  if (bb[1] <= x0 + 1e-6 || bb[0] >= x1 - 1e-6 || bb[3] <= y0 + 1e-6 || bb[2] >= y1 - 1e-6) return [r]; // misses the rect: untouched
  const out: Ring[] = []; let rest: Ring | null = r;
  const cuts: [V, V, V, V][] = [[[x0, 0], [-1, 0], [x0, 0], [1, 0]], [[x1, 0], [1, 0], [x1, 0], [-1, 0]], [[0, y0], [0, -1], [0, y0], [0, 1]], [[0, y1], [0, 1], [0, y1], [0, -1]]];
  for (const [a, out_n, b, in_n] of cuts) {
    if (!rest) break;
    const piece = clipHalf(rest, a, out_n); if (piece) out.push(piece);
    rest = clipHalf(rest, b, in_n);
  }
  return out.filter(q => areaOf(q.p) > 0.5);
}

export function pointInRing(q: V, p: V[]) {
  let inside = false;
  for (let i = 0, j = p.length - 1; i < p.length; j = i++) {
    const [xi, yi] = p[i]!, [xj, yj] = p[j]!;
    if (yi > q[1] !== yj > q[1] && q[0] < ((xj - xi) * (q[1] - yi)) / (yj - yi) + xi) inside = !inside;
  }
  return inside;
}

/** Oriented frame of an edge: unit u along it, n outward. */
export const frame = (a: V, b: V) => ({ u: norm(sub(b, a)), n: edgeN(a, b), len: len(sub(b, a)) });
export const yawOf = (u: V) => Math.atan2(u[1], u[0]);
/** Axis-aligned rect of centre c and half sizes. */
export const rectAt = (c: V, w: number, d: number): Rect => [c[0] - w / 2, c[0] + w / 2, c[1] - d / 2, c[1] + d / 2];

/** No two non-adjacent edges cross. */
export function isSimple(p: V[]): boolean {
  const n = p.length, o = (a: V, b: V, c: V) => Math.sign(cross(sub(b, a), sub(c, a)));
  for (let i = 0; i < n; i++) for (let j = i + 2; j < n; j++) {
    if (i === 0 && j === n - 1) continue;
    const a = p[i]!, b = p[(i + 1) % n]!, c = p[j]!, d = p[(j + 1) % n]!;
    if (o(a, b, c) * o(a, b, d) < 0 && o(c, d, a) * o(c, d, b) < 0) return false;
  }
  return true;
}

export type Frame = { c: V; yaw: number; W: number; D: number };
/** Largest rect in the frame of the polygon's longest edge (or world axes) that sits inside it, inset by `margin`. */
export function fit(poly: V[], margin: number, axis: boolean): Frame {
  let best = 0, yaw = 0;
  poly.forEach((a, i) => { const b = poly[(i + 1) % poly.length]!, l = dist(a, b); if (l > best) { best = l; yaw = Math.atan2(b[1] - a[1], b[0] - a[0]); } });
  if (axis) yaw = 0;
  const U: V = [Math.cos(yaw), Math.sin(yaw)], Vv: V = [-U[1], U[0]];
  const us = poly.map(p => p[0] * U[0] + p[1] * U[1]), vs = poly.map(p => p[0] * Vv[0] + p[1] * Vv[1]);
  const u0 = Math.min(...us), u1 = Math.max(...us), v0 = Math.min(...vs), v1 = Math.max(...vs), cu = (u0 + u1) / 2, cv = (v0 + v1) / 2;
  const at = (u: number, v: number): V => [u * U[0] + v * Vv[0], u * U[1] + v * Vv[1]];
  const W0 = u1 - u0 - 2 * margin, D0 = v1 - v0 - 2 * margin;
  let W = 0, D = 0;
  if (W0 > 0 && D0 > 0) for (let i = 0; i <= 16; i++) for (let j = 0; j <= 16; j++) { // the largest scaled rect whose corners all sit inside
    const w = W0 * (1 - i * 0.05), d = D0 * (1 - j * 0.05);
    if (w * d > W * D && [[-1, -1], [1, -1], [1, 1], [-1, 1]].every(([a, b]) => pointInRing(at(cu + a! * w / 2, cv + b! * d / 2), poly))) { W = w; D = d; }
  }
  return { c: at(cu, cv), yaw, W: Math.max(0, W), D: Math.max(0, D) };
}
