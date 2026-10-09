// Plan geometry: 2D vectors, filleted polylines (lines + arcs), sampling, polygons and distances. Plan frame: x east, y north.
export type V = [number, number];
export type Rect = [number, number, number, number]; // x0, x1, y0, y1

export const add = (a: V, b: V): V => [a[0] + b[0], a[1] + b[1]];
export const sub = (a: V, b: V): V => [a[0] - b[0], a[1] - b[1]];
export const mul = (a: V, k: number): V => [a[0] * k, a[1] * k];
export const len = (a: V) => Math.hypot(a[0], a[1]);
export const dist = (a: V, b: V) => len(sub(a, b));
export const norm = (a: V): V => mul(a, 1 / len(a));
export const cross = (a: V, b: V) => a[0] * b[1] - a[1] * b[0];
export const dot = (a: V, b: V) => a[0] * b[0] + a[1] * b[1];
export const left = (a: V): V => [-a[1], a[0]];
export const polar = (c: V, r: number, a: number): V => [c[0] + r * Math.cos(a), c[1] + r * Math.sin(a)];

/** Straight or circular arc. Arc: centre c, radius r, start angle a0, signed sweep (+ = counter-clockwise, a left turn). */
export type Seg =
  | { kind: 'line'; a: V; b: V; len: number }
  | { kind: 'arc'; a: V; b: V; len: number; c: V; r: number; a0: number; sweep: number };

const line = (a: V, b: V): Seg => ({ kind: 'line', a, b, len: dist(a, b) });
export function arc(c: V, r: number, a0: number, sweep: number): Seg {
  return { kind: 'arc', c, r, a0, sweep, a: polar(c, r, a0), b: polar(c, r, a0 + sweep), len: r * Math.abs(sweep) };
}

/** A polyline whose interior vertices may carry a fillet radius ([x, y, r]). The fillet's centre lies on the inside
 *  of the turn (the side the path turns toward), so the sweep has the turn's sign. Throws where fillets overlap. */
export function filleted(pts: number[][]): Seg[] {
  const P = pts.map(p => [p[0], p[1]] as V), segs: Seg[] = [];
  let cur = P[0];
  for (let i = 1; i < P.length; i++) {
    const r = i < P.length - 1 ? pts[i][2] ?? 0 : 0;
    if (!r) { if (dist(cur, P[i]) > 1e-6) segs.push(line(cur, P[i])); cur = P[i]; continue; }
    const din = norm(sub(P[i], P[i - 1])), dout = norm(sub(P[i + 1], P[i]));
    const turn = Math.atan2(cross(din, dout), dot(din, dout)), t = r * Math.tan(Math.abs(turn) / 2);
    const t1 = sub(P[i], mul(din, t)), t2 = add(P[i], mul(dout, t));
    if (dot(sub(t1, cur), din) < -1e-6) throw new Error(`fillet overlap before vertex ${i} (${P[i]})`);
    if (dist(P[i], P[i + 1]) < t - 1e-6) throw new Error(`fillet at vertex ${i} (${P[i]}) runs past the next vertex`);
    const c = add(t1, mul(left(din), Math.sign(turn) * r));
    if (dist(cur, t1) > 1e-6) segs.push(line(cur, t1));
    const s = arc(c, r, Math.atan2(t1[1] - c[1], t1[0] - c[0]), turn);
    if (dist(s.b, t2) > 1e-3) throw new Error(`fillet at vertex ${i} does not meet its exit tangent`);
    segs.push(s); cur = t2;
  }
  return segs;
}

/** Arcs between consecutive points on one circle (a gyratory), clockwise or not. */
export function ring(pts: V[], c: V, cw: boolean): Seg[] {
  const segs: Seg[] = [], tau = 2 * Math.PI;
  for (let i = 1; i < pts.length; i++) {
    const a0 = Math.atan2(pts[i - 1][1] - c[1], pts[i - 1][0] - c[0]), a1 = Math.atan2(pts[i][1] - c[1], pts[i][0] - c[0]);
    const ccw = ((a1 - a0) % tau + tau) % tau || tau;
    segs.push(arc(c, dist(pts[i - 1], c), a0, cw ? ccw - tau || -tau : ccw));
  }
  return segs;
}

export const pathLength = (segs: Seg[]) => segs.reduce((s, g) => s + g.len, 0);

export type Sample = { s: number; p: V; r: number; dir: V };
/** Points every `step` metres (and at every segment end); r = Infinity on straights. */
export function sample(segs: Seg[], step = 1): Sample[] {
  const out: Sample[] = []; let s0 = 0;
  for (const g of segs) {
    const n = Math.max(1, Math.ceil(g.len / step));
    for (let k = out.length ? 1 : 0; k <= n; k++) {
      const u = k / n;
      if (g.kind === 'line') out.push({ s: s0 + u * g.len, p: add(g.a, mul(sub(g.b, g.a), u)), r: Infinity, dir: norm(sub(g.b, g.a)) });
      else {
        const a = g.a0 + u * g.sweep, sg = Math.sign(g.sweep);
        out.push({ s: s0 + u * g.len, p: polar(g.c, g.r, a), r: g.r, dir: [-Math.sin(a) * sg, Math.cos(a) * sg] });
      }
    }
    s0 += g.len;
  }
  return out;
}
/** Chainage of the sample nearest p. */
export const chainage = (samples: Sample[], p: V) => samples.reduce((b, x) => (dist(x.p, p) < dist(b.p, p) ? x : b)).s;
/** A polyline through the samples, for drawing and distances. */
export const polyline = (segs: Seg[], step = 2): V[] => sample(segs, step).map(x => x.p);

// ---- polygons ----
export const rectPoly = ([x0, x1, y0, y1]: Rect): V[] => [[x0, y0], [x1, y0], [x1, y1], [x0, y1]];
/** A rect with the one corner inside circle (c, r) cut away along the circle (a crescent façade). */
export function notched(rect: Rect, c: V, r: number, step = 4): V[] {
  const poly = rectPoly(rect), i = poly.findIndex(p => dist(p, c) < r);
  if (i < 0) return poly;
  const prev = poly[(i + 3) % 4], corner = poly[i], next = poly[(i + 1) % 4];
  const hit = (a: V, b: V): V => { // where edge corner→a leaves the circle
    const d = norm(sub(a, b)), f = sub(b, c), B = dot(f, d), C = dot(f, f) - r * r;
    return add(b, mul(d, -B + Math.sqrt(B * B - C)));
  };
  const p0 = hit(prev, corner), p1 = hit(next, corner);
  let a0 = Math.atan2(p0[1] - c[1], p0[0] - c[0]), a1 = Math.atan2(p1[1] - c[1], p1[0] - c[0]);
  let sw = a1 - a0; if (sw > Math.PI) sw -= 2 * Math.PI; if (sw < -Math.PI) sw += 2 * Math.PI;
  const n = Math.max(2, Math.ceil((Math.abs(sw) * r) / step)), arcPts: V[] = [];
  for (let k = 0; k <= n; k++) arcPts.push(polar(c, r, a0 + (sw * k) / n));
  return [...poly.slice(0, i), ...arcPts, ...poly.slice(i + 1)];
}
export function inPoly(p: V, poly: V[]) {
  let inside = false;
  for (let i = 0, j = poly.length - 1; i < poly.length; j = i++) {
    const [xi, yi] = poly[i], [xj, yj] = poly[j];
    if (yi > p[1] !== yj > p[1] && p[0] < ((xj - xi) * (p[1] - yi)) / (yj - yi) + xi) inside = !inside;
  }
  return inside;
}
export const area = (poly: V[]) => Math.abs(poly.reduce((s, p, i) => s + cross(p, poly[(i + 1) % poly.length]), 0)) / 2;
export const centroid = (poly: V[]): V => mul(poly.reduce(add, [0, 0] as V), 1 / poly.length);

export function segDist(p: V, a: V, b: V) {
  const ab = sub(b, a), t = Math.max(0, Math.min(1, dot(sub(p, a), ab) / (dot(ab, ab) || 1)));
  return dist(p, add(a, mul(ab, t)));
}
function segsCross(a: V, b: V, c: V, d: V) {
  const o = (p: V, q: V, r: V) => Math.sign(cross(sub(q, p), sub(r, p)));
  return o(a, b, c) * o(a, b, d) < 0 && o(c, d, a) * o(c, d, b) < 0;
}
/** Segment intersection point, or null. */
export function segHit(a: V, b: V, c: V, d: V): V | null {
  if (!segsCross(a, b, c, d)) return null;
  const r = sub(b, a), s = sub(d, c), t = cross(sub(c, a), s) / cross(r, s);
  return add(a, mul(r, t));
}
/** Shortest distance from a polyline to a polygon (0 when they cross or the line is inside). */
export function lineToPoly(line: V[], poly: V[]) {
  let best = Infinity;
  for (let i = 1; i < line.length; i++) {
    const a = line[i - 1], b = line[i];
    if (inPoly(a, poly)) return 0;
    for (let j = 0; j < poly.length; j++) {
      const c = poly[j], d = poly[(j + 1) % poly.length];
      if (segsCross(a, b, c, d)) return 0;
      best = Math.min(best, segDist(a, c, d), segDist(b, c, d), segDist(c, a, b));
    }
  }
  return best;
}
export const lineDist = (p: V, line: V[]) => line.slice(1).reduce((m, b, i) => Math.min(m, segDist(p, line[i], b)), Infinity);
