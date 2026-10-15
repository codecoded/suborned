// A seeded, deterministic force layout for the tie graph (Fruchterman-Reingold on 130 nodes). Every start position is a hash of the person's id inside the
// cell of their group, the sum is plain arithmetic in a fixed order, and the result is rounded to 0.1, so the same people give the same bytes and the same
// id starts in the same place in two casts (a compare reads as the same map with different dots).
import { rng } from '../world/gen/common';

export type LNode = { id: string; group: number };
export type LEdge = { a: string; b: string; w: number };
export const GRAPH_W = 1000, GRAPH_H = 680;

export function layout(nodes: LNode[], edges: LEdge[], seed = 'people-view/graph/1'): Map<string, [number, number]> {
  const n = nodes.length, W = GRAPH_W, H = GRAPH_H;
  const out = new Map<string, [number, number]>();
  if (!n) return out;
  const groups = Math.max(1, ...nodes.map(x => x.group + 1));
  // group cells on a 3-wide grid (no trig): the layout is arithmetic only
  const cell = (g: number): [number, number] => [W / 2 + ((g % 3) - 1) * (W * 0.28), H / 2 + (Math.floor(g / 3) - (Math.ceil(groups / 3) - 1) / 2) * (H * 0.34)];
  const idx = new Map(nodes.map((x, i) => [x.id, i]));
  const X = new Float64Array(n), Y = new Float64Array(n);
  nodes.forEach((x, i) => { const r = rng(`${seed}/${x.id}`), [cx, cy] = cell(x.group); X[i] = cx + (r.f() - 0.5) * 220; Y[i] = cy + (r.f() - 0.5) * 160; });
  const E = edges.filter(e => idx.has(e.a) && idx.has(e.b)).map(e => ({ a: idx.get(e.a)!, b: idx.get(e.b)!, w: e.w }));
  const k = Math.sqrt((W * H) / n) * 0.6, iters = 400;
  const dx = new Float64Array(n), dy = new Float64Array(n);
  for (let it = 0; it < iters; it++) {
    const t = (W / 12) * (1 - it / iters) + 0.5;
    dx.fill(0); dy.fill(0);
    for (let i = 0; i < n; i++) for (let j = i + 1; j < n; j++) {
      let ex = X[i]! - X[j]!, ey = Y[i]! - Y[j]!, d2 = ex * ex + ey * ey;
      if (d2 < 0.01) { ex = (i % 7) - 3 + 0.5; ey = (j % 5) - 2 + 0.5; d2 = ex * ex + ey * ey; }
      const f = (k * k) / d2; // repulsion k^2/d along the unit vector (ex/d, ey/d) = (ex, ey) * k^2 / d^2
      dx[i]! += ex * f; dy[i]! += ey * f; dx[j]! -= ex * f; dy[j]! -= ey * f;
    }
    for (const e of E) {
      const ex = X[e.a]! - X[e.b]!, ey = Y[e.a]! - Y[e.b]!, d = Math.sqrt(ex * ex + ey * ey) || 0.01, f = ((d * d) / k) * e.w;
      dx[e.a]! -= (ex / d) * f; dy[e.a]! -= (ey / d) * f; dx[e.b]! += (ex / d) * f; dy[e.b]! += (ey / d) * f;
    }
    for (let i = 0; i < n; i++) {
      const [cx, cy] = cell(nodes[i]!.group);
      dx[i]! += (cx - X[i]!) * 0.35; dy[i]! += (cy - Y[i]!) * 0.35; // keep each group near its cell, so units read as clusters
      dx[i]! += (W / 2 - X[i]!) * 0.05; dy[i]! += (H / 2 - Y[i]!) * 0.05;
      const m = Math.sqrt(dx[i]! * dx[i]! + dy[i]! * dy[i]!) || 0.01, s = Math.min(m, t) / m;
      X[i]! += dx[i]! * s; Y[i]! += dy[i]! * s;
    }
  }
  const x0 = Math.min(...X), x1 = Math.max(...X), y0 = Math.min(...Y), y1 = Math.max(...Y), pad = 36;
  const sx = (W - 2 * pad) / Math.max(1, x1 - x0), sy = (H - 2 * pad) / Math.max(1, y1 - y0), s = Math.min(sx, sy);
  const ox = (W - (x1 - x0) * s) / 2, oy = (H - (y1 - y0) * s) / 2;
  nodes.forEach((x, i) => out.set(x.id, [Math.round((ox + (X[i]! - x0) * s) * 10) / 10, Math.round((oy + (Y[i]! - y0) * s) * 10) / 10]));
  return out;
}
