// Brief 061 · desire lines for the crossing count (Datum, 2026-10-24). The walks of brief 009's method (briefs/009/crossings-proposal/make.ts, copied, not changed),
// on the world as exported today (out/world/harbour-district.world.json) instead of generating it: Splice is changing the generator. Writes the walks and the
// places they cross a carriageway or the circus ring to out/scratch/datum/061/walks.json, which make.ts reads.
// usage (repo root): bun briefs/061/datum/walks.ts
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { type V, dist, inPoly, nearLine, segDist } from '../../../tools/plan/geom';
import { derive, type Plan } from '../../../tools/plan/plan';
import { civic } from '../../../tools/plan/civic';
import { blockersOf } from '../../../tools/plan/walk';
import { type Edge, Solids } from '../../../tools/plan/walk-solids';

const root = resolve(import.meta.dir, '../../..');
const json = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = json('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, json(plan.civic.block));
const w: any = json('out/world/harbour-district.world.json');
const CELL = 2, ROADW = 3, NEAR = 500, AROUND = 40;

// ---- nodes
type Node = { id: string; kind: 'door' | 'platform' | 'bank'; group: string; p: V };
const cen = (r: V[]): V => [r.reduce((s, p) => s + p[0], 0) / r.length, r.reduce((s, p) => s + p[1], 0) / r.length];
const nodes: Node[] = [
  ...civ.walkFrom.map(o => ({ id: o.name, kind: 'door' as const, group: 'civic', p: o.p })),
  ...w.transit.stops.map(s => ({ id: s.id, kind: 'platform' as const, group: s.id.split('/').slice(0, 3).join('/'), p: cen(s.ring as V[]) })),
  ...w.connectors.filter(k => k.kind === 'stairTower').map(k => ({ id: k.id.replace('/tower', ''), kind: 'bank' as const, group: k.id.split('/').slice(0, 3).join('/'), p: k.foot as V })),
];

// ---- the walk grid
const [x0, X1] = plan.sheet.x, [y0, Y1] = plan.sheet.y, nx = Math.ceil((X1 - x0) / CELL), ny = Math.ceil((Y1 - y0) / CELL);
const centre = (k: number): V => [x0 + ((k % nx) + .5) * CELL, y0 + (Math.floor(k / nx) + .5) * CELL];
const idx = (p: V) => Math.max(0, Math.min(ny - 1, Math.floor((p[1] - y0) / CELL))) * nx + Math.max(0, Math.min(nx - 1, Math.floor((p[0] - x0) / CELL)));
const { polys, rounds } = blockersOf(plan, d, civ);
const boxed = polys.map(b => ({ b, a: Math.min(...b.map(v => v[0])), z: Math.max(...b.map(v => v[0])), c: Math.min(...b.map(v => v[1])), e: Math.max(...b.map(v => v[1])) }));
const blocked = new Uint8Array(nx * ny), weight = new Float32Array(nx * ny).fill(1);
type Road = { id: string; line: V[]; half: number };
const roads: Road[] = w.ground.roads.map(r => ({ id: r.id.replace(/^road\//, ''), line: r.line as V[], half: r.width / 2 }));
const circ = w.ground.circuses[0]!, plats = w.transit.stops.map(s => s.ring as V[]);
for (let k = 0; k < nx * ny; k++) {
  const p = centre(k), rr = dist(p, circ.centre as V);
  blocked[k] = boxed.some(q => p[0] >= q.a && p[0] <= q.z && p[1] >= q.c && p[1] <= q.e && inPoly(p, q.b)) || rounds.some(l => dist(p, l.c) < l.r) ? 1 : 0;
  if (!plats.some(r => inPoly(p, r)) && ((rr >= circ.islandRadius && rr <= circ.radius) || roads.some(r => nearLine(p, r.line, r.half)))) weight[k] = ROADW;
}
const free = (i: number, j: number) => i >= 0 && j >= 0 && i < nx && j < ny && !blocked[j * nx + i];
const passes = (i: number, j: number, dx: number, dy: number) => {
  const sx = Math.sign(dx), sy = Math.sign(dy), ax = Math.abs(dx), ay = Math.abs(dy);
  if (ax + ay === 1) return true;
  if (ax === 1 && ay === 1) return free(i + dx, j) && free(i, j + dy);
  return ay === 2 ? free(i, j + sy) && free(i + dx, j + sy) : free(i + sx, j) && free(i + sx, j + dy);
};
const STEPS: [number, number][] = [[1, 0], [-1, 0], [0, 1], [0, -1], [1, 1], [1, -1], [-1, 1], [-1, -1], [1, 2], [2, 1], [-1, 2], [-2, 1], [1, -2], [2, -1], [-1, -2], [-2, -1]];
const solids = new Solids(civ.solids, plan.standards.walking.body?.r), edgesAt = new Map<number, Edge[]>(), grow = solids.r + 2.5 * CELL;
for (const e of solids.list) for (let j = Math.max(0, Math.floor((e.y0 - grow - y0) / CELL)); j <= Math.min(ny - 1, Math.floor((e.y1 + grow - y0) / CELL)); j++) for (let i = Math.max(0, Math.floor((e.x0 - grow - x0) / CELL)); i <= Math.min(nx - 1, Math.floor((e.x1 + grow - x0) / CELL)); i++) { const k = j * nx + i; (edgesAt.get(k) ?? edgesAt.set(k, []).get(k)!).push(e); }
const clearSeg = (a: V, b: V) => !solids.any || solids.clear(a, b, solids.near(Math.min(a[0], b[0]), Math.max(a[0], b[0]), Math.min(a[1], b[1]), Math.max(a[1], b[1])));
const sight = (a: V, b: V) => { const n = Math.ceil(dist(a, b) / (CELL * .4)); for (let k = 1; k < n; k++) { const t = k / n; if (blocked[idx([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t])]) return false; } return clearSeg(a, b); };
const segCost = (a: V, b: V) => { const n = Math.max(1, Math.ceil(dist(a, b) / 0.5)); let c = 0; for (let k = 0; k < n; k++) { const t = (k + .5) / n; c += weight[idx([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t])]!; } return c * dist(a, b) / n; };
const snap = (p: V) => { let best = -1, bd = Infinity; const ci = Math.floor((p[0] - x0) / CELL), cj = Math.floor((p[1] - y0) / CELL); for (let r = 0; r <= 4 && best < 0; r++) for (let j = cj - r; j <= cj + r; j++) for (let i = ci - r; i <= ci + r; i++) { if (!free(i, j)) continue; const dd = dist(p, centre(j * nx + i)); if (dd < bd) { bd = dd; best = j * nx + i; } } return best; };

function tree(src: V) {
  const D = new Float64Array(nx * ny).fill(Infinity), prev = new Int32Array(nx * ny).fill(-1), heap: [number, number][] = [];
  const push = (k: number, v: number) => { heap.push([v, k]); let c = heap.length - 1; while (c) { const q = (c - 1) >> 1; if (heap[q]![0] <= heap[c]![0]) break; [heap[q], heap[c]] = [heap[c]!, heap[q]!]; c = q; } };
  const pop = () => { const top = heap[0]!, last = heap.pop()!; if (heap.length) { heap[0] = last; let c = 0; for (;;) { const l = 2 * c + 1, r = l + 1; let m = c; if (l < heap.length && heap[l]![0] < heap[m]![0]) m = l; if (r < heap.length && heap[r]![0] < heap[m]![0]) m = r; if (m === c) break; [heap[m], heap[c]] = [heap[c]!, heap[m]!]; c = m; } } return top; };
  const s = snap(src); D[s] = dist(src, centre(s)); push(s, D[s]!);
  while (heap.length) {
    const [v, k] = pop(); if (v > D[k]!) continue;
    const i = k % nx, j = Math.floor(k / nx);
    for (const [dx, dy] of STEPS) {
      const a = i + dx, b = j + dy; if (!free(a, b) || !passes(i, j, dx, dy)) continue;
      const es = edgesAt.get(k), nk = b * nx + a; if (es && !solids.clear(centre(k), centre(nk), es)) continue;
      const nv = v + Math.hypot(dx, dy) * CELL * (weight[k]! + weight[nk]!) / 2; if (nv < D[nk]!) { D[nk] = nv; prev[nk] = k; push(nk, nv); }
    }
  }
  return { D, prev, s };
}
function walk(src: V, tgt: V, f: ReturnType<typeof tree>): V[] | null {
  const t = snap(tgt); if (t < 0 || f.D[t] === Infinity) return null;
  const raw: V[] = [tgt], cost: number[] = [f.D[t]! + segCost(centre(t), tgt)];
  for (let c = f.prev[t]!; c >= 0 && c !== f.s; c = f.prev[c]!) { raw.push(centre(c)); cost.push(f.D[c]!); }
  raw.push(src); cost.push(0); raw.reverse(); cost.reverse();
  const path: V[] = [raw[0]!]; let a = 0;
  while (a < raw.length - 1) { let b = raw.length - 1; while (b > a + 1 && !(sight(raw[a]!, raw[b]!) && segCost(raw[a]!, raw[b]!) <= cost[b]! - cost[a]! + 0.5)) b--; path.push(raw[b]!); a = b; }
  return path;
}

// ---- where a walk crosses a carriageway
const lineDistTo = (p: V, line: V[]) => { let m = Infinity; for (let i = 1; i < line.length; i++) { const a = line[i - 1]!, b = line[i]!; if (Math.min(a[0], b[0]) - p[0] > m || p[0] - Math.max(a[0], b[0]) > m || Math.min(a[1], b[1]) - p[1] > m || p[1] - Math.max(a[1], b[1]) > m) continue; m = Math.min(m, segDist(p, a, b)); } return m; };
const side = (p: V, line: V[]) => { let m = Infinity, s = 0; for (let i = 1; i < line.length; i++) { const a = line[i - 1]!, b = line[i]!, dd = segDist(p, a, b); if (dd < m) { m = dd; s = Math.sign((b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0])); } } return s; };
type Ev = { road: string; at: V; a: V; b: V; walk: number };
function events(path: V[], walkNo: number): Ev[] {
  const pts: V[] = []; for (let i = 1; i < path.length; i++) { const a = path[i - 1]!, b = path[i]!, n = Math.max(1, Math.ceil(dist(a, b) / 0.5)); for (let k = i === 1 ? 0 : 1; k <= n; k++) pts.push([a[0] + (b[0] - a[0]) * k / n, a[1] + (b[1] - a[1]) * k / n]); }
  const ev = (road: string, k: number): Ev => ({ road, at: pts[k]!, a: pts[Math.max(0, k - AROUND * 2)]!, b: pts[Math.min(pts.length - 1, k + AROUND * 2)]!, walk: walkNo });
  const out: Ev[] = [];
  for (const r of roads) { let last = 0; pts.forEach((p, k) => { if (lineDistTo(p, r.line) > r.half) { last = 0; return; } const sd = side(p, r.line); if (last && sd && sd !== last) out.push(ev(r.id, k)); if (sd) last = sd; }); }
  let inside: boolean | null = null;
  pts.forEach((p, k) => { const rr = dist(p, circ.centre as V); if (rr > circ.radius + 0.5) { if (inside === true) out.push(ev('crown-circus', k)); inside = false; } else if (rr < circ.islandRadius - 0.5) { if (inside === false) out.push(ev('crown-circus', k)); inside = true; } });
  return out;
}

// ---- run
const t0 = Date.now();
// A station is one place with two stair banks (its bridge crosses the road above): a walk to it goes to the nearer bank. A tram stop's two platforms are two places (one per direction).
type Place = { id: string; group: string; kind: Node['kind']; ends: Node[] };
const places: Place[] = [];
for (const n of nodes) { const key = n.kind === 'bank' ? n.group : n.id, pl = places.find(p => p.id === key); if (pl) pl.ends.push(n); else places.push({ id: key, group: n.group, kind: n.kind, ends: [n] }); }
const pairs: [Place, Place][] = [], doors = places.filter(n => n.kind === 'door'), transit = places.filter(n => n.kind !== 'door');
const far = (a: Place, b: Place) => Math.min(...a.ends.flatMap(x => b.ends.map(y => dist(x.p, y.p))));
for (let i = 0; i < doors.length; i++) for (let j = i + 1; j < doors.length; j++) pairs.push([doors[i]!, doors[j]!]);
for (const a of doors) for (const b of transit) pairs.push([a, b]);
for (let i = 0; i < transit.length; i++) for (let j = i + 1; j < transit.length; j++) if (transit[i]!.group !== transit[j]!.group && far(transit[i]!, transit[j]!) <= NEAR) pairs.push([transit[i]!, transit[j]!]);
const trees = new Map<string, ReturnType<typeof tree>>(), walks: { from: string; to: string; m: number; path: V[] }[] = [], evs: Ev[] = [];
const lenOf = (path: V[]) => path.slice(1).reduce((s, p, i) => s + dist(path[i]!, p), 0);
for (const [A, B] of pairs) {
  let best: { path: V[]; m: number; a: Node; b: Node } | undefined;
  for (const a of A.ends) for (const b of B.ends) {
    const f = trees.get(a.id) ?? trees.set(a.id, tree(a.p)).get(a.id)!, path = walk(a.p, b.p, f);
    if (!path) throw new Error(`no walk ${a.id} → ${b.id}`);
    const m = lenOf(path); if (!best || m < best.m) best = { path, m, a, b };
  }
  walks.push({ from: best!.a.id, to: best!.b.id, m: +best!.m.toFixed(1), path: best!.path });
  evs.push(...events(best!.path, walks.length - 1));
}
const r1 = (v: V): V => [+v[0].toFixed(1), +v[1].toFixed(1)];
mkdirSync(resolve(root, 'out/scratch/datum/061'), { recursive: true });
writeFileSync(resolve(root, 'out/scratch/datum/061/walks.json'), JSON.stringify({ nodes, walks: walks.map(x => ({ ...x, path: x.path.map(r1) })), events: evs.map(e => ({ ...e, at: r1(e.at), a: r1(e.a), b: r1(e.b) })) }));
console.log(`${nodes.length} places, ${walks.length} desire lines, ${evs.length} carriageway crossings (${((Date.now() - t0) / 1000).toFixed(1)} s)`);
