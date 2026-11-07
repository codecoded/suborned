// The Handover zone's traffic lanes (brief 098 step 1, law L8; Keel): sim/data/handover.lanes.json (suborned.sim.lanes/1), block frame, integer cm.
// Derived, never authored, from the plan's roads (their node paths, widths and classes) through the walk window, under right-hand traffic:
//  · a road leaving the window runs on `runway` m past its edge (a mouth), so a car drives in and out of the zone rather than appearing on it;
//  · a junction is a plan node two or more roads share inside the window, its box cut back along every road by the widest other road's half
//    width plus a margin; two boxes closer than `minLink` along a road are one junction (Orbit Walk's two T's with Garden Row and Pylon Row);
//  · each road between junctions (or a junction and a mouth) carries, per direction, life.json's lanes (floor((w - median) / 2 / laneWidth),
//    at least 1), each the middle of its share of the half carriageway, on the driver's right (the block frame's right of a heading h is
//    (h.y, -h.x): the site's own rule, Game.BesideCar);
//  · a turn joins every lane into a junction to every lane out of it but its own road's other way (no U-turns): along the lane, a corner of
//    radius `turnRadius` (or less where the lanes leave less) at each crossing of two lane lines, on along the next.
// Speeds by class from life.json traffic.byClass (m/s); each lane says how far its line is from the road's centreline (offset) and the half
// carriageway's width (half), cm: a driver easing round something keeps within its own half. Every lane and turn point inside the window, and a sedan's corners there, lie on the
// drive grid (unknown = reject): the engine checks the same (Lanes.cs). Deterministic: the same plan, life and drive grid give the same bytes.
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import type { Plan } from '../plan/plan';
import { WINDOW, toBlockFn } from '../plan/walk-grid';
import type { V } from '../plan/geom';
import { pretty } from './site';

export const LANES_FILE = 'sim/data/handover.lanes.json';
export const PLAN_FILE = 'contracts/world/harbour-district.plan.json', LIFE_FILE = 'contracts/world/harbour-district.life.json', SITE_FILE = 'sim/data/handover.site.json';
/** m: how far a lane runs past the window's edge; a box's margin past the widest other road's half width; the shortest road between two
 *  boxes (a van, 6.56 m, and its gap) before they are one junction; a turn's corner radius. */
export const RUNWAY = 30, MARGIN = 0.5, MIN_LINK = 10, TURN_R = 6;
const SEDAN = { len: 4.73, w: 1.82 }; // rulebook car-len-cm, car-w-cm: the footprint every lane must carry on the drive grid

type Road = { id: string; cls: string; w: number; median: number; nodes: string[]; pts: V[] };
type End = { kind: 'edge' | 'node'; node?: string; at: V };
type Run = { road: Road; pts: V[]; ids: (string | null)[]; s: number[]; a: End; b: End };
type Link = { id: string; road: Road; run: Run; i: number; sa: number; sb: number; a: string; b: string; internal: boolean };
export type Lane = { id: string; road: string; class: string; dir: '+' | '-'; k: number; from: string; to: string; speed: number; offset: number; half: number; line: [number, number][] };
export type Turn = { id: string; from: string; to: string; line: [number, number][] };
export type Junction = { id: string; nodes: string[]; box: [number, number][]; turns: Turn[] };
export type Mouth = { id: string; road: string; gate: [number, number]; in: string[]; out: string[] };
export type LanesFile = { schema: 'suborned.sim.lanes/1'; from: { plan: string; sha256: string; life: string; lifeSha256: string }; frame: string; window: { x0: number; y0: number; x1: number; y1: number };
  runwayCm: number; laneWidthCm: number; marginCm: number; minLinkCm: number; turnRadiusCm: number; src: string; lanes: Lane[]; junctions: Junction[]; mouths: Mouth[] };

const sub = (a: V, b: V): V => [a[0] - b[0], a[1] - b[1]], add = (a: V, b: V): V => [a[0] + b[0], a[1] + b[1]], mul = (a: V, k: number): V => [a[0] * k, a[1] * k];
const len = (a: V) => Math.hypot(a[0], a[1]), unit = (a: V): V => mul(a, 1 / (len(a) || 1)), cross = (a: V, b: V) => a[0] * b[1] - a[1] * b[0];
const right = (h: V): V => [h[1], -h[0]];
const cm = (p: V): [number, number] => [Math.round(p[0] * 100), Math.round(p[1] * 100)];

/** The part of segment a→b inside the box (Liang-Barsky): [t0, t1] or null. */
function clip(a: V, b: V, x0: number, x1: number, y0: number, y1: number): [number, number] | null {
  let t0 = 0, t1 = 1; const d = sub(b, a);
  for (const [p, q] of [[-d[0], a[0] - x0], [d[0], x1 - a[0]], [-d[1], a[1] - y0], [d[1], y1 - a[1]]] as const) {
    if (p === 0) { if (q < 0) return null; continue; }
    const t = q / p; if (p < 0) { if (t > t1) return null; if (t > t0) t0 = t; } else { if (t < t0) return null; if (t < t1) t1 = t; }
  }
  return [t0, t1];
}
const inBox = (p: V, x0: number, x1: number, y0: number, y1: number, e = 1e-9) => p[0] >= x0 - e && p[0] <= x1 + e && p[1] >= y0 - e && p[1] <= y1 + e;

/** Each road's runs through the window grown by the runway: the stretches inside it, with the plan nodes on them (and their chainage). */
function runsOf(road: Road, W: typeof WINDOW): Run[] {
  const E = { x0: W.x0 - RUNWAY, x1: W.x1 + RUNWAY, y0: W.y0 - RUNWAY, y1: W.y1 + RUNWAY };
  const runs: Run[] = []; let cur: { pts: V[]; ids: (string | null)[]; a: End } | null = null;
  const close = (at: V, end: End) => { if (!cur) return; if (len(sub(cur.pts[cur.pts.length - 1]!, at)) > 1e-9) { cur.pts.push(at); cur.ids.push(end.node ?? null); } runs.push(finish(road, cur.pts, cur.ids, cur.a, end)); cur = null; };
  for (let i = 1; i < road.pts.length; i++) {
    const a = road.pts[i - 1]!, b = road.pts[i]!, t = clip(a, b, E.x0, E.x1, E.y0, E.y1);
    if (!t) { continue; }
    const pa = add(a, mul(sub(b, a), t[0])), pb = add(a, mul(sub(b, a), t[1]));
    if (!cur) cur = { pts: [pa], ids: [t[0] === 0 ? road.nodes[i - 1]! : null], a: t[0] === 0 ? { kind: 'node', node: road.nodes[i - 1]!, at: pa } : { kind: 'edge', at: pa } };
    if (t[1] < 1) { close(pb, { kind: 'edge', at: pb }); continue; }
    cur.pts.push(b); cur.ids.push(road.nodes[i]!);
    if (i === road.pts.length - 1) { const c = cur as { pts: V[]; ids: (string | null)[]; a: End }; runs.push(finish(road, c.pts, c.ids, c.a, { kind: 'node', node: road.nodes[i]!, at: b })); cur = null; }
  }
  return runs.filter((r) => r.pts.some((p, k) => k > 0 && clip(r.pts[k - 1]!, p, W.x0, W.x1, W.y0, W.y1) !== null));
}
function finish(road: Road, pts: V[], ids: (string | null)[], a: End, b: End): Run {
  const s = [0]; for (let k = 1; k < pts.length; k++) s.push(s[k - 1]! + len(sub(pts[k]!, pts[k - 1]!)));
  return { road, pts, ids, s, a, b };
}
/** The point and heading `at` m along a polyline with chainages s. */
function along(pts: V[], s: number[], at: number): { p: V; h: V } {
  let k = 1; while (k < pts.length - 1 && s[k]! < at) k++;
  const a = pts[k - 1]!, b = pts[k]!, h = unit(sub(b, a)), t = Math.max(0, Math.min(1, (at - s[k - 1]!) / ((s[k]! - s[k - 1]!) || 1)));
  return { p: add(a, mul(sub(b, a), t)), h };
}
/** The stretch [from, to] m of a run's centreline, its vertices kept. */
function stretch(r: Run, from: number, to: number): V[] {
  const out: V[] = [along(r.pts, r.s, from).p];
  for (let k = 1; k < r.pts.length - 1; k++) if (r.s[k]! > from + 1e-9 && r.s[k]! < to - 1e-9) out.push(r.pts[k]!);
  out.push(along(r.pts, r.s, to).p); return out;
}
/** A polyline offset `o` m to the right of its travel (vertices on the bisector). */
function offset(pts: V[], o: number): V[] {
  return pts.map((p, k) => {
    const h0 = k > 0 ? unit(sub(p, pts[k - 1]!)) : unit(sub(pts[1]!, p)), h1 = k < pts.length - 1 ? unit(sub(pts[k + 1]!, p)) : h0;
    const n = unit(add(right(h0), right(h1))), c = Math.max(0.2, n[0] * right(h0)[0] + n[1] * right(h0)[1]);
    return add(p, mul(n, o / c));
  });
}
/** Where two lines (point, direction) cross, or null when parallel. */
function meet(p: V, h: V, q: V, g: V): V | null { const d = cross(h, g); if (Math.abs(d) < 1e-9) return null; const t = cross(sub(q, p), g) / d; return add(p, mul(h, t)); }

export function buildLanes(plan: Plan, life: any, drive: { x0: number; y0: number; cell: number; nx: number; ny: number; rows: string[] }, sources: { plan: string; life: string }): LanesFile {
  const W = WINDOW, toBlock = toBlockFn(plan.civic), laneW: number = life.traffic?.laneWidth;
  if (!(laneW > 0)) throw new Error('lanes: life.json traffic.laneWidth is missing');
  const roads: Road[] = plan.roads.map((r) => ({ id: r.id, cls: r.class, w: r.w, median: r.median ?? 0, nodes: r.path, pts: r.path.map((n) => { const p = plan.nodes[n]; if (!p) throw new Error(`lanes: road ${r.id} names node ${n}`); return toBlock(p); }) }));
  const runs = roads.flatMap((r) => runsOf(r, W));
  // junctions: a node on two runs or more, inside the window (a node in the runway band is no junction the engine models: unknown = reject)
  const at = new Map<string, Run[]>(); for (const r of runs) for (const id of r.ids) if (id) at.set(id, [...(at.get(id) ?? []), r]);
  const jn = [...at.entries()].filter(([, rs]) => new Set(rs.map((r) => r.road.id)).size >= 2).map(([id]) => id).sort();
  for (const id of jn) { const p = toBlock(plan.nodes[id]!); if (!inBox(p, W.x0, W.x1, W.y0, W.y1)) throw new Error(`lanes: junction ${id} lies outside the window, on a runway (not modelled)`); }
  const isJ = new Set(jn), cutOf = (node: string, road: string) => Math.max(...at.get(node)!.filter((r) => r.road.id !== road).map((r) => r.road.w / 2)) + MARGIN;
  // links: each run cut at its junctions; mouths at its window-edge ends; a dead end inside the window is rejected
  const links: Link[] = [], mouthAt = new Map<string, { road: Road; at: V; inward: V }>();
  for (const r of runs) {
    const st: { s: number; ref: string }[] = [];
    const endRef = (e: End, s: number, inward: V, tag: string) => {
      if (e.kind === 'node' && isJ.has(e.node!)) return e.node!;
      if (e.kind === 'node' && inBox(e.at, W.x0, W.x1, W.y0, W.y1)) throw new Error(`lanes: road ${r.road.id} ends at ${e.node} inside the window with no junction (a dead end: unknown = reject)`);
      const id = `mouth:${r.road.id}:${tag}`; mouthAt.set(id, { road: r.road, at: e.at, inward }); void s; return id;
    };
    st.push({ s: 0, ref: endRef(r.a, 0, along(r.pts, r.s, 0).h, 'a') });
    for (let k = 1; k < r.pts.length - 1; k++) if (r.ids[k] && isJ.has(r.ids[k]!)) st.push({ s: r.s[k]!, ref: r.ids[k]! });
    const L = r.s[r.s.length - 1]!; st.push({ s: L, ref: endRef(r.b, L, mul(along(r.pts, r.s, L).h, -1), 'b') });
    for (let k = 1; k < st.length; k++) links.push({ id: `${r.road.id}.${links.filter((x) => x.road.id === r.road.id).length}`, road: r.road, run: r, i: k, sa: st[k - 1]!.s, sb: st[k]!.s, a: st[k - 1]!.ref, b: st[k]!.ref, internal: false });
  }
  // merge boxes closer than MIN_LINK along a road
  const parent = new Map<string, string>(jn.map((j) => [j, j])); const find = (j: string): string => { const p = parent.get(j)!; return p === j ? j : find(p); };
  for (const l of links) if (isJ.has(l.a) && isJ.has(l.b) && l.sb - l.sa - cutOf(l.a, l.road.id) - cutOf(l.b, l.road.id) < MIN_LINK) { l.internal = true; const [x, y] = [find(l.a), find(l.b)].sort(); parent.set(y!, x!); }
  const groups = new Map<string, string[]>(); for (const j of jn) groups.set(find(j), [...(groups.get(find(j)) ?? []), j]);
  const jid = (node: string) => 'j:' + groups.get(find(node))!.join('+');
  const ref = (r: string) => (isJ.has(r) ? jid(r) : r);
  // lanes
  const lanes: Lane[] = [], speedOf = (cls: string) => { const v = life.traffic?.byClass?.[cls]?.speed; if (!(v > 0)) throw new Error(`lanes: life.json traffic.byClass.${cls}.speed is missing`); return Math.round(v * 100); };
  for (const l of links) {
    if (l.internal) continue;
    const from = isJ.has(l.a) ? l.sa + cutOf(l.a, l.road.id) : l.sa, to = isJ.has(l.b) ? l.sb - cutOf(l.b, l.road.id) : l.sb;
    if (to - from < 1) throw new Error(`lanes: ${l.id} is ${(to - from).toFixed(1)} m between its boxes`);
    const centre = stretch(l.run, from, to), n = Math.max(1, Math.floor((l.road.w - l.road.median) / 2 / laneW)), share = (l.road.w - l.road.median) / 2 / n;
    for (const dir of ['+', '-'] as const)
      for (let k = 0; k < n; k++) {
        const pts = dir === '+' ? centre : [...centre].reverse(), o = l.road.median / 2 + (k + 0.5) * share;
        lanes.push({ id: `${l.id}${dir}${k}`, road: l.road.id, class: l.road.cls, dir, k, from: ref(dir === '+' ? l.a : l.b), to: ref(dir === '+' ? l.b : l.a), speed: speedOf(l.road.cls), offset: Math.round(o * 100), half: Math.round((l.road.w / 2) * 100), line: offset(pts, o).map(cm) });
      }
  }
  // junctions: boxes and turns
  const m = (p: [number, number]): V => [p[0] / 100, p[1] / 100];
  const junctions: Junction[] = [];
  for (const [root, nodes] of [...groups.entries()].sort(([a], [b]) => (a < b ? -1 : 1))) {
    void root; const id = 'j:' + nodes.join('+');
    const pts: V[] = [];
    for (const node of nodes) for (const l of links) for (const [end, s] of [[l.a, l.sa], [l.b, l.sb]] as const) {
      if (end !== node) continue; const c = cutOf(node, l.road.id), q = along(l.run.pts, l.run.s, end === l.a ? s + c : s - c), w2 = l.road.w / 2;
      const o = along(l.run.pts, l.run.s, s).p, back = sub(mul(o, 2), q.p); // and mirrored through the node: a road that ends there still spans the other's width
      pts.push(add(q.p, mul(right(q.h), w2)), add(q.p, mul(right(q.h), -w2)), add(back, mul(right(q.h), w2)), add(back, mul(right(q.h), -w2)));
    }
    const box = hull(pts).map(cm);
    const ins = lanes.filter((x) => x.to === id), outs = lanes.filter((x) => x.from === id), turns: Turn[] = [];
    for (const a of ins) for (const b of outs) {
      const la = links.find((x) => a.id.startsWith(x.id) && a.id.length === x.id.length + 2)!, lb = links.find((x) => b.id.startsWith(x.id) && b.id.length === x.id.length + 2)!;
      if (la === lb) continue; // its own road's other way: no U-turns
      const lines = route(la, a, lb, b, links, nodes, laneW); if (!lines) continue;
      turns.push({ id: `${id}:${a.id}>${b.id}`, from: a.id, to: b.id, line: corner(lines, m(a.line[a.line.length - 1]!), m(b.line[0]!)).map(cm) });
    }
    junctions.push({ id, nodes, box, turns });
  }
  // mouths
  const mouths: Mouth[] = [...mouthAt.entries()].sort(([a], [b]) => (a < b ? -1 : 1)).map(([id, x]) => {
    const t = clip(x.at, add(x.at, mul(x.inward, 2 * RUNWAY)), W.x0, W.x1, W.y0, W.y1); const gate = t ? add(x.at, mul(mul(x.inward, 2 * RUNWAY), t[0])) : x.at;
    return { id, road: x.road.id, gate: cm(gate), in: lanes.filter((l) => l.from === id).map((l) => l.id), out: lanes.filter((l) => l.to === id).map((l) => l.id) };
  });
  check(lanes, junctions, drive);
  const sha = (f: string) => createHash('sha256').update(f).digest('hex');
  return {
    schema: 'suborned.sim.lanes/1', from: { plan: PLAN_FILE, sha256: sha(sources.plan), life: LIFE_FILE, lifeSha256: sha(sources.life) },
    frame: 'civic-block.json block frame in integer centimetres: x south, y east (the site file\'s frame)',
    window: { x0: W.x0 * 100, y0: W.y0 * 100, x1: W.x1 * 100, y1: W.y1 * 100 },
    runwayCm: RUNWAY * 100, laneWidthCm: Math.round(laneW * 100), marginCm: MARGIN * 100, minLinkCm: MIN_LINK * 100, turnRadiusCm: TURN_R * 100,
    src: 'brief 098 step 1 (law L8; Keel): the plan\'s roads through the walk window, right-hand traffic: per road between junctions and mouths, life.json traffic\'s lanes per direction (floor((w - median)/2/laneWidth), at least 1), each the middle of its share of the half carriageway on the driver\'s right; speeds by class (life.json traffic.byClass); junctions the plan nodes two roads share, boxes cut back the widest other road\'s half width + margin, boxes nearer than minLink merged; turns every lane in to every lane out but a U-turn, corners of turnRadius; mouths where a road leaves the window, its lanes running runway past the edge. tools/sim/lanes.ts',
    lanes, junctions, mouths,
  };
}

/** The lane lines a turn follows: the lane in, the lanes of the junction's own short roads between (a merged junction), the lane out. */
function route(la: Link, a: Lane, lb: Link, b: Lane, links: Link[], nodes: string[], laneW: number): { p: V; h: V }[] | null {
  const lineOf = (l: Lane) => { const p = [l.line[0]!, l.line[l.line.length - 1]!].map((q) => [q[0] / 100, q[1] / 100] as V); return { p: p[0]!, h: unit(sub(p[1]!, p[0]!)) }; };
  const node = (l: Link, toward: boolean, dir: '+' | '-') => (dir === '+') === toward ? l.b : l.a; // the end the lane runs to (toward) or from
  const start = node(la, true, a.dir), goal = node(lb, false, b.dir);
  // the internal links between start and goal, breadth first (nodes of this junction only)
  const prev = new Map<string, { link: Link; from: string } | null>([[start, null]]); const q = [start];
  while (q.length) { const n = q.shift()!; if (n === goal) break; for (const l of links) if (l.internal && (l.a === n || l.b === n)) { const o = l.a === n ? l.b : l.a; if (!nodes.includes(o) || prev.has(o)) continue; prev.set(o, { link: l, from: n }); q.push(o); } }
  if (!prev.has(goal)) return null;
  const mid: { p: V; h: V }[] = []; for (let n = goal; prev.get(n); n = prev.get(n)!.from) {
    const { link, from } = prev.get(n)!, fwd = link.a === from, o = link.road.median / 2 + 0.5 * ((link.road.w - link.road.median) / 2 / Math.max(1, Math.floor((link.road.w - link.road.median) / 2 / laneW)));
    const c = along(link.run.pts, link.run.s, (link.sa + link.sb) / 2), h = fwd ? c.h : mul(c.h, -1);
    mid.unshift({ p: add(c.p, mul(right(h), o)), h });
  }
  return [lineOf(a), ...mid, lineOf(b)];
}
/** From P along the lines to Q: straight on each, a quadratic corner (control point the lines' crossing) of radius r where two lines meet. */
function corner(lines: { p: V; h: V }[], P: V, Q: V): V[] {
  const out: V[] = [P]; let cur = P;
  const cs: { c: V; ha: V; hb: V }[] = [];
  for (let k = 1; k < lines.length; k++) { const a = lines[k - 1]!, b = lines[k]!, c = meet(a.p, a.h, b.p, b.h); if (c) cs.push({ c, ha: a.h, hb: b.h }); else if (Math.abs(cross(sub(b.p, a.p), a.h)) > 0.05) throw new Error('lanes: two parallel lane lines in a turn are not one line (a lane shift is not modelled)'); }
  for (let k = 0; k < cs.length; k++) {
    const { c, ha, hb } = cs[k]!, next = k + 1 < cs.length ? cs[k + 1]!.c : Q;
    const r = Math.min(TURN_R, len(sub(c, cur)), k + 1 < cs.length ? len(sub(next, c)) / 2 : len(sub(next, c)));
    const A = sub(c, mul(ha, r)), B = add(c, mul(hb, r)), n = Math.max(4, Math.ceil((2 * r) / 0.5));
    if (len(sub(A, cur)) > 1e-6) out.push(A);
    for (let i = 1; i <= n; i++) { const t = i / n, u = 1 - t; out.push(add(add(mul(A, u * u), mul(c, 2 * u * t)), mul(B, t * t))); }
    cur = B;
  }
  if (len(sub(Q, cur)) > 1e-6) out.push(Q);
  return out;
}
function hull(ps: V[]): V[] {
  const p = [...ps].sort((a, b) => a[0] - b[0] || a[1] - b[1]); if (p.length < 3) return p;
  const lo: V[] = [], up: V[] = [], turn = (o: V, a: V, b: V) => cross(sub(a, o), sub(b, o));
  for (const q of p) { while (lo.length >= 2 && turn(lo[lo.length - 2]!, lo[lo.length - 1]!, q) <= 0) lo.pop(); lo.push(q); }
  for (const q of [...p].reverse()) { while (up.length >= 2 && turn(up[up.length - 2]!, up[up.length - 1]!, q) <= 0) up.pop(); up.push(q); }
  return [...lo.slice(0, -1), ...up.slice(0, -1)];
}
/** Every point of every lane and turn inside the window, and a sedan's four corners there, on a drivable cell (unknown = reject). */
function check(lanes: Lane[], junctions: Junction[], g: { x0: number; y0: number; cell: number; nx: number; ny: number; rows: string[] }) {
  const bad: string[] = [];
  const free = (x: number, y: number) => { const i = Math.floor((x - g.x0) / g.cell), j = Math.floor((y - g.y0) / g.cell); if (i < 0 || j < 0 || i >= g.nx || j >= g.ny) return null; return g.rows[i]![j] !== '#'; };
  const walk = (id: string, line: [number, number][]) => {
    for (let k = 1; k < line.length; k++) {
      const a = line[k - 1]!, b = line[k]!, d = Math.hypot(b[0] - a[0], b[1] - a[1]); if (d === 0) continue; const h: V = [(b[0] - a[0]) / d, (b[1] - a[1]) / d], r = right(h);
      for (let s = 0; s <= d; s += 25) {
        const c: V = [a[0] + h[0] * s, a[1] + h[1] * s];
        for (const [u, v] of [[0, 0], [1, 1], [1, -1], [-1, 1], [-1, -1]] as const) {
          const p: V = [c[0] + h[0] * u * SEDAN.len * 50 + r[0] * v * SEDAN.w * 50, c[1] + h[1] * u * SEDAN.len * 50 + r[1] * v * SEDAN.w * 50];
          if (free(p[0], p[1]) === false) { bad.push(`${id} at (${(c[0] / 100).toFixed(2)}, ${(c[1] / 100).toFixed(2)})`); return; }
        }
      }
    }
  };
  for (const l of lanes) walk(l.id, l.line); for (const j of junctions) for (const t of j.turns) walk(t.id, t.line);
  if (bad.length) throw new Error(`lanes: off the drive grid (a sedan's footprint leaves the carriageway): ${bad.slice(0, 8).join('; ')}${bad.length > 8 ? ` and ${bad.length - 8} more` : ''}`);
}

export function buildLanesFile(root: string): string {
  const read = (f: string) => readFileSync(join(root, f), 'utf8');
  const planText = read(PLAN_FILE), lifeText = read(LIFE_FILE), site = JSON.parse(read(SITE_FILE));
  return pretty(buildLanes(JSON.parse(planText) as Plan, JSON.parse(lifeText), site.drive, { plan: planText, life: lifeText })) + '\n';
}

if (import.meta.main) {
  const root = join(import.meta.dir, '..', '..'), text = buildLanesFile(root);
  if (process.argv.includes('--check')) {
    const now = readFileSync(join(root, LANES_FILE), 'utf8');
    if (now !== text) { console.error(`${LANES_FILE} is not what tools/sim/lanes.ts makes now: run bun tools/sim/lanes.ts`); process.exit(1); }
    console.log(`${LANES_FILE}: up to date`);
  } else { writeFileSync(join(root, LANES_FILE), text); const f = JSON.parse(text) as LanesFile; console.log(`${LANES_FILE}: ${f.lanes.length} lanes, ${f.junctions.length} junctions (${f.junctions.reduce((s, j) => s + j.turns.length, 0)} turns), ${f.mouths.length} mouths`); }
}
