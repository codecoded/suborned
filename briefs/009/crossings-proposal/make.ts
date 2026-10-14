// Brief 009 · the crossing proposal for Sky (Datum, 2026-10-15): desire lines, coverage now and with the proposal, and the pictures.
// Reads the plan (standards.streetscape.crossings.coverage: add[].lines, remove[].ids, callouts; the generator reads none of it) and the
// world the plan generates (its crossings). Writes, next to this file: desire-lines.json (the walks, the clusters, the numbers) and
// district.png, circus.png, civic-block.png, sunset-market.png (the plan sheet's district drawing with the proposal layer, a legend and the
// numbers), rendered by rsvg-convert (brew install librsvg). No browser.
// usage (repo root): bun briefs/009/crossings-proposal/make.ts
//
// Desire lines: the shortest walks between the civic block's 3 doors, the 20 tram stop platforms and the 8 station banks (stair tower
// feet): door to door, door to every platform and bank, and platform or bank to every other within 500 m. Walked cross-anywhere
// (standards.walking) on a 2 m grid, 16 neighbours, the plan checker's blockers and the block's ground solids kept walking.body.r away;
// a metre of carriageway costs 3 of footway, so people keep to the pavement and cross square; string-pulled where the straight line
// costs no more. Each walk is cut where it crosses a carriageway's centre line or the circus ring; those places cluster within 20 m.
// A place is served by the crossing of that road that adds the least walk (from 40 m of walk before it to 40 m after, via the
// crossing's two ends, less the straight line); served = a detour of 30 m or less (21 s at 1.4 m/s).
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { type V, dist, inPoly, nearLine, segDist } from '../../../tools/plan/geom';
import { derive, type Plan } from '../../../tools/plan/plan';
import { civic } from '../../../tools/plan/civic';
import { check } from '../../../tools/plan/checks';
import { blockersOf } from '../../../tools/plan/walk';
import { type Edge, Solids } from '../../../tools/plan/walk-solids';
import { generated } from '../../../tools/plan/world-ctx';
import { district } from '../../../tools/plan/draw';
import { CSS } from '../../../tools/plan/sheet';

const root = resolve(import.meta.dir, '../../..'), here = import.meta.dir;
const json = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = json('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, json(plan.civic.block));
const w = generated(plan, d, civ).w, cov = plan.standards.streetscape.crossings.coverage;
const CELL = 2, ROADW = 3, NEAR = 500, SERVED = 30, AROUND = 40, CLUSTER = 20;

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

// ---- crossings now and with the proposal
type X = { id: string; road?: string; line: V[]; mid: V };
const now: X[] = (w.crossings ?? []).map(c => ({ id: c.id, line: c.line as V[], mid: cen(c.line as V[]) }));
const gone = new Set<string>((cov.remove ?? []).flatMap((r: { ids?: string[] }) => r.ids ?? []));
const added: X[] = (cov.add ?? []).flatMap((a: { id: string; lines?: V[][] }) => (a.lines ?? []).map((l, i) => ({ id: `${a.id}/${i}`, line: l, mid: cen(l) })));
const proposed: X[] = [...now.filter(x => !gone.has(x.id)), ...added];
const serves = (x: X, road: string) => road === 'crown-circus' ? Math.abs(dist(x.mid, circ.centre as V) - (circ.radius + circ.islandRadius) / 2) <= (circ.radius - circ.islandRadius) / 2 + 0.5 : lineDistTo(x.mid, roads.find(q => q.id === road)!.line) <= roads.find(q => q.id === road)!.half + 0.5;
const detour = (e: Ev, x: X) => { const p = x.line[0]!, q = x.line[x.line.length - 1]!; return Math.min(dist(e.a, p) + dist(p, q) + dist(q, e.b), dist(e.a, q) + dist(q, p) + dist(p, e.b)) - dist(e.a, e.b); };
const best = (e: Ev, xs: X[]) => xs.filter(x => serves(x, e.road)).map(x => ({ id: x.id, m: detour(e, x) })).sort((a, b) => a.m - b.m)[0];

// ---- run
const t0 = Date.now();
const pairs: [Node, Node][] = [], doors = nodes.filter(n => n.kind === 'door'), transit = nodes.filter(n => n.kind !== 'door');
for (let i = 0; i < doors.length; i++) for (let j = i + 1; j < doors.length; j++) pairs.push([doors[i]!, doors[j]!]);
for (const a of doors) for (const b of transit) pairs.push([a, b]);
for (let i = 0; i < transit.length; i++) for (let j = i + 1; j < transit.length; j++) if (transit[i]!.group !== transit[j]!.group && dist(transit[i]!.p, transit[j]!.p) <= NEAR) pairs.push([transit[i]!, transit[j]!]);
const trees = new Map<string, ReturnType<typeof tree>>(), walks: { from: string; to: string; m: number; path: V[]; hot?: boolean }[] = [], evs: Ev[] = [];
for (const [a, b] of pairs) {
  const f = trees.get(a.id) ?? trees.set(a.id, tree(a.p)).get(a.id)!, path = walk(a.p, b.p, f);
  if (!path) throw new Error(`no walk ${a.id} → ${b.id}`);
  walks.push({ from: a.id, to: b.id, m: +path.slice(1).reduce((s, p, i) => s + dist(path[i]!, p), 0).toFixed(1), path });
  evs.push(...events(path, walks.length - 1));
}
type Cl = { road: string; at: V; ev: Ev[] };
const clusters: Cl[] = [];
for (const e of evs) { const c = clusters.find(c => c.road === e.road && dist(c.at, e.at) < CLUSTER); if (c) { c.ev.push(e); c.at = [(c.at[0] * (c.ev.length - 1) + e.at[0]) / c.ev.length, (c.at[1] * (c.ev.length - 1) + e.at[1]) / c.ev.length]; } else clusters.push({ road: e.road, at: e.at, ev: [e] }); }
const median = (xs: number[]) => { const s = [...xs].sort((a, b) => a - b); return s[Math.floor(s.length / 2)]!; };
const judge = (xs: X[]) => {
  const per = evs.map(e => best(e, xs)?.m ?? Infinity), cl = clusters.map(c => median(c.ev.map(e => best(e, xs)?.m ?? Infinity)));
  const finite = cl.filter(isFinite);
  return { crossings: xs.length, served: per.filter(m => m <= SERVED).length, of: per.length, share: Math.round((100 * per.filter(m => m <= SERVED).length) / per.length),
    medianDetour: Math.round(median(cl)), longestDetour: Math.round(Math.max(...finite)), noCrossing: cl.filter(m => !isFinite(m)).length,
    noCrossingWalkCrossings: clusters.filter((_, i) => !isFinite(cl[i]!)).reduce((s, c) => s + c.ev.length, 0),
    over60: cl.filter(m => m > 60).length, over60WalkCrossings: clusters.filter((_, i) => cl[i]! > 60).reduce((s, c) => s + c.ev.length, 0), per, cl };
};
const A = judge(now), B = judge(proposed);
evs.forEach((e, i) => { if (A.per[i]! > SERVED && B.per[i]! <= SERVED) walks[e.walk]!.hot = true; });
const numbers = Object.fromEntries((['now', 'proposed'] as const).map(k => { const { per: _p, cl: _c, ...n } = k === 'now' ? A : B; return [k, n]; }));
const r1 = (v: V): V => [+v[0].toFixed(1), +v[1].toFixed(1)];
writeFileSync(resolve(here, 'desire-lines.json'), JSON.stringify({
  note: 'Written by briefs/009/crossings-proposal/make.ts from the plan (crossings.coverage) and the world it generates. walks: the desire lines (hot: one the proposal serves that is not served now); clusters: where they cross a carriageway, with the best detour now and with the proposal; numbers: the coverage.',
  method: { cell: CELL, carriagewayCost: ROADW, nearM: NEAR, servedM: SERVED, aroundM: AROUND, clusterM: CLUSTER, body: solids.r },
  numbers,
  walks: walks.map(x => ({ ...x, path: x.path.map(r1) })),
  clusters: clusters.map((c, i) => ({ road: c.road, at: r1(c.at), n: c.ev.length, now: isFinite(A.cl[i]!) ? Math.round(A.cl[i]!) : null, proposed: isFinite(B.cl[i]!) ? Math.round(B.cl[i]!) : null })).sort((a, b) => (b.now ?? 1e9) - (a.now ?? 1e9)),
}, null, 1));
console.log(`${walks.length} desire lines, ${evs.length} carriageway crossings in ${clusters.length} clusters (${((Date.now() - t0) / 1000).toFixed(1)} s)`);
console.log('now', JSON.stringify(numbers.now)); console.log('proposed', JSON.stringify(numbers.proposed));

// ---- pictures: the sheet's district drawing with the proposal layer, a legend and the numbers, rendered by rsvg-convert
const R = check(plan, d, civ);
const map = district(plan, d, R, { crossings: now.map(x => ({ id: x.id, line: x.line })), walks: walks.map(x => ({ path: x.path, hot: x.hot })) });
const vb = /viewBox="0 0 ([\d.]+) ([\d.]+)"/.exec(map)!, W0 = +vb[1]!, H0 = +vb[2]!, M = 34; // draw.ts district: margin 34 round the sheet frame
const X = (x: number) => x - plan.sheet.x[0] + M, Y = (y: number) => plan.sheet.y[1] - y + M;
const defs = '<defs><pattern id="hatch" width="8" height="8" patternUnits="userSpaceOnUse" patternTransform="rotate(45)"><rect width="8" height="8" fill="#ebe3cf"/><line x1="0" y1="0" x2="0" y2="8" stroke="#2b272340" stroke-width="1.2"/></pattern></defs>';
const pathOf = (poly: V[]) => 'M' + poly.map(p => `${X(p[0]).toFixed(2)},${Y(p[1]).toFixed(2)}`).join('L') + 'Z';
const feet = '<g data-layer="civic-feet">' + civ.objects.map(o => `<path class="obj ${o.kind}" d="${pathOf(o.poly)}"/>`).join('') + civ.structures.map(x => `<path class="struct" d="${pathOf(x.poly)}"/>`).join('')
  + civ.buildings.flatMap(b => [`<path class="bldg ${b.id}" d="${pathOf(b.poly)}"/>`, ...(b.towers ?? []).filter(t => t.poly).map(t => `<path class="bldg" d="${pathOf(t.poly!)}"/>`)]).join('') + '</g>'; // the block's buildings, not on the 1:2500 sheet, so the close-ups show what the walks go round
const inner = map.replace(/^<svg[^>]*>/, '').replace(/<\/svg>$/, '').replace('<g data-layer="proposal">', feet + '<g data-layer="proposal">');
const esc = (s: string) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;');
const nNow = A, nNew = B, kept = now.length - gone.size;
const legendLines: [string, string?][] = [
  ['Crossings · proposal for Sky (brief 009)', 'h'],
  ['Datum 2026-10-15 · not built: the generator', 's'], ['does not read it until Sky approves', 's'], ['', ''],
  [`kept (${kept})`, 'kept'], [`proposed (${added.length})`, 'add'],
  ...(cov.add as { id: string; count: number; where: string }[]).map(a => [`  ${a.id} ${({ X1: 'Crown Circus: to the island', X2: 'primary junctions, Parade mouth', X3: 'mid-block (rule not built yet)', X4: 'Apron Road at Rocket Lane' } as Record<string, string>)[a.id] ?? ''} · ${a.count}`, 's'] as [string, string]),
  [`dropped (${gone.size})`, 'gone'],
  ...(cov.remove as { id: string; count: number }[]).map(r => [`  ${r.id} ${({ R1: 'twin across a T-junction', R2: 'both arms of a bend' } as Record<string, string>)[r.id] ?? ''} · ${r.count}`, 's'] as [string, string]),
  [`desire lines (${walks.length})`, 'walk'], ['  doors, tram platforms, station stairs', 's'], ['  darker: served only with the proposal', 's'], ['', ''],
  ['Walk crossings within a 30 m detour', 'b'], [`  ${nNow.share} % → ${nNew.share} %  (${nNow.served} → ${nNew.served} of ${nNow.of})`, 'n'],
  ['Longest detour', 'b'], [`  ${nNow.longestDetour} m → ${nNew.longestDetour} m; none over 60 m`, 'n'],
  [`  now: ${nNow.over60} places over 60 m, ${nNow.noCrossing} of them`, 's'], [`  on the circus ring, which has no crossing`, 's'],
  ['Median detour', 'b'], [`  ${nNow.medianDetour} m → ${nNew.medianDetour} m`, 'n'],
  ['Crossings', 'b'], [`  ${now.length} → ${proposed.length}`, 'n'], ['', ''],
  ['People still cross anywhere; crossings', 's'], ['calm traffic and carry the signal eyes.', 's'],
];
function legend(x: number, y: number, s: number) {
  let out = `<rect x="${x}" y="${y}" width="${30 * s}" height="${(legendLines.length + 1.2) * 1.55 * s}" fill="#f8f2e4" stroke="#2b2723" stroke-width="1.5"/>`;
  legendLines.forEach(([t0, k], i) => {
    let t = t0;
    const ty = y + (i + 1.4) * 1.55 * s, tx = x + s * (k === 'kept' || k === 'add' || k === 'gone' || k === 'walk' ? 3.6 : t.startsWith('  ') ? 2.2 : 1); t = t.trim();
    if (k === 'kept') out += `<line x1="${x + s}" y1="${ty - s * .35}" x2="${x + s * 3}" y2="${ty - s * .35}" stroke="#1f7a6a" stroke-width="${s * .3}"/>`;
    if (k === 'add') out += `<line x1="${x + s}" y1="${ty - s * .35}" x2="${x + s * 3}" y2="${ty - s * .35}" stroke="#c0157a" stroke-width="${s * .45}" stroke-linecap="round"/>`;
    if (k === 'gone') out += `<line x1="${x + s}" y1="${ty - s * .35}" x2="${x + s * 3}" y2="${ty - s * .35}" stroke="#a39a90" stroke-width="${s * .3}"/><path d="M${x + s * 1.5} ${ty - s * .85}L${x + s * 2.5} ${ty + s * .15}M${x + s * 1.5} ${ty + s * .15}L${x + s * 2.5} ${ty - s * .85}" stroke="#c0392b" stroke-width="${s * .18}"/>`;
    if (k === 'walk') out += `<line x1="${x + s}" y1="${ty - s * .35}" x2="${x + s * 3}" y2="${ty - s * .35}" stroke="#4a3d78" stroke-opacity=".45" stroke-width="${s * .15}"/>`;
    const font = k === 'h' ? `font-weight="700" font-size="${s * 1.05}"` : k === 'b' ? `font-weight="700" font-size="${s * .9}"` : k === 'n' ? `font-weight="700" font-size="${s * .95}" fill="#c0157a"` : `font-size="${s * .85}"`;
    out += `<text x="${tx}" y="${ty}" font-family="Menlo, monospace" ${font}>${esc(t)}</text>`;
  });
  return out;
}
type View = { file: string; title?: string; x: [number, number]; y: [number, number]; px: number; hide?: string[] };
const views: View[] = [
  { file: 'district.png', x: [plan.sheet.x[0] - M, plan.sheet.x[1] + M], y: [plan.sheet.y[0] - M, plan.sheet.y[1] + M], px: 2600, hide: ['walks'] },
  { file: 'circus.png', title: 'Crown Circus: X1, the island garden; X3 on Crown Avenue; the Parade to SpaceHQ', x: [-135, 165], y: [-115, 105], px: 1500, hide: ['walks', 'grid'] },
  { file: 'civic-block.png', title: 'The civic block (Records Precinct): X4 on Apron Road, R1 at Rocket Gate, X3 on Market Street', x: [-300, -30], y: [-450, -185], px: 1500, hide: ['walks', 'grid'] },
  { file: 'sunset-market.png', title: 'Sunset Way at Market Street (primary junction): X2 on both arms, X3 mid-block', x: [-405, -195], y: [-480, -270], px: 1500, hide: ['walks', 'grid'] },
];
mkdirSync(here, { recursive: true });
const out = resolve(root, 'out/datum/crossings-proposal'); mkdirSync(out, { recursive: true });
for (const v of views) {
  const vw = v.x[1] - v.x[0], vh = v.y[1] - v.y[0], mw = v.px, mh = Math.round((mw * vh) / vw), s = 22, lw = 31 * s, head = v.title ? 46 : 0;
  const box = `${X(v.x[0])} ${Y(v.y[1])} ${vw} ${vh}`, hide = (v.hide ?? []).map(l => `[data-layer="${l}"]{display:none}`).join('');
  const lh = (legendLines.length + 1.2) * 1.55 * s + 24, H = Math.max(mh + head, lh);
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${mw + lw + 24}" height="${H}" viewBox="0 0 ${mw + lw + 24} ${H}"><style>${CSS}${hide}</style>${defs}<rect width="100%" height="100%" fill="#e9e1cd"/>`
    + (v.title ? `<text x="12" y="31" font-family="Menlo, monospace" font-weight="700" font-size="22" fill="#2b2723">${esc(v.title)}</text>` : '')
    + `<svg id="district" x="0" y="${head}" width="${mw}" height="${mh}" viewBox="${box}" preserveAspectRatio="xMidYMid meet"><rect x="${X(v.x[0])}" y="${Y(v.y[1])}" width="${vw}" height="${vh}" fill="#f4ecda"/>${inner}</svg>`
    + legend(mw + 12, head + 12, s) + '</svg>';
  const svgFile = resolve(out, v.file.replace('.png', '.svg'));
  writeFileSync(svgFile, svg);
  const r = Bun.spawnSync(['rsvg-convert', svgFile, '-o', resolve(here, v.file)]);
  if (r.exitCode !== 0) throw new Error(`rsvg-convert ${v.file}: ${r.stderr.toString()}`);
  console.log(`${v.file}: ${mw + lw + 24} × ${H} px`);
}
void W0; void H0;
