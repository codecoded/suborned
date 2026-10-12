// The road graph the checks walk: directed-free edges per road segment, and Dijkstra over them.
import { type V, dist } from './geom';
import type { Derived, Plan } from './plan';

export type Edge = { a: string; b: string; road: string; len: number };

export function edges(plan: Plan, d: Derived): Edge[] {
  return d.roads.flatMap(r => r.path.slice(1).map((b, i) => ({ a: r.path[i], b, road: r.id, len: r.segs[i]?.len ?? dist(plan.nodes[r.path[i]], plan.nodes[b]) })));
}

export function dijkstra(E: Edge[], from: string) {
  const D = new Map<string, number>([[from, 0]]), prev = new Map<string, Edge>(), done = new Set<string>();
  for (;;) {
    let u: string | undefined, best = Infinity;
    for (const [n, v] of D) if (!done.has(n) && v < best) { best = v; u = n; }
    if (u === undefined) break; done.add(u);
    for (const e of E) for (const [x, y] of [[e.a, e.b], [e.b, e.a]]) if (x === u && best + e.len < (D.get(y) ?? Infinity)) { D.set(y, best + e.len); prev.set(y, e); }
  }
  const trace = (to: string) => { const out: Edge[] = []; let n = to; while (prev.has(n)) { const e = prev.get(n)!; out.unshift(e); n = e.a === n ? e.b : e.a; } return out; };
  return { D, trace };
}
