// Kerb returns (brief 028, Sky 2026-10-24: "make sure the corners of pavements are rounded like in testbed, not sharp - modern retro future world is curvy and smooth, not harsh lines and strong angles").
// The plan's plots are rectangles that run to the carriageway edge, so every pavement corner where two kerbs meet was square. The testbed drew one rounded outline (civic-road-layout.js civicRoadBoundaries:
// road-loop corners R 9.8, junction fillets R 1.2-2.6, bay corners R 1.4). This is that rule for the generator: ONE list of the corners the kerb side of a plot rounds, read by everything that lies on a kerb
// (the slab, the road's carriageway fill, the kerb stones, the light ribbon, the pinstripe, the crossings, the furniture) so they all follow the same arc.
//
//   convex   a plot vertex where two carriageway edges meet at a corner (a junction corner, the bend of a road): the pavement corner is cut by a circular arc tangent to both kerb faces, of the radius
//            standards.streetscape.kerbReturn.byClass[the minor road's class] (10 / 8 / 6 m: primary / district / access). The region between the old corner and the arc is carriageway now (`cut`).
//   concave  a plot vertex that turns the other way between two carriageway edges (the end of a dead-end lane): a fillet of kerbReturn.concave (1.2 m) adds pavement in the inside corner.
//   Never the building side: only the vertex's two kerb edges move, the building line (edges.ts buildingLine) and the buildings' own cornerArc stay as they were.
//   A return is shrunk, never squeezed, when an edge is too short for both its ends (tangents together leave 0.5 m of straight kerb at least).
//   The arc is drawn with SEGMENTS per quarter turn straight chords (16; 8 under a 4 m radius): the sagitta of a 10 m return is 0.012 m, of a 6 m 0.007.
//
// Rules over plan data only (plotRing's classes, the plot polygons, standards.streetscape.kerbReturn), no coordinates; deterministic. A plan without kerbReturn takes the defaults below (the same numbers), so
// a pinned older plan rounds too; `kerbReturn.off: true` is the escape hatch (a pinned replay that must keep square corners).
import { type V, add, dist, dot, inPoly, lineDist, mul, norm, segDist, sub } from '../../plan/geom';
import { toPlanFn } from '../../plan/civic';
import type { Ctx } from './common';
import { kindOf, idOf, plotRing } from './edges';
import { areaOf } from './geo';
import { isFootwayCorner } from './street/footway';

export type KerbReturnStd = { byClass: Record<string, number>; concave: number; bay: number; /** the civic block's corners, and the corners across the road that hold its crossings' partner landings (plan: kerbReturn.civic) */ civic: number; /** the outside of a bend is concentric with its inside return (plan kerbReturn.bend.outer: 'concentric'); false: square */ bendOuter?: boolean; /** every median end is a bullnose (plan kerbReturn.nose) */ nose?: boolean; /** a crossing landing's near edge stands this far past a convex return's tangent point (plan: kerbReturn.landings.clear) */ clear: number; off?: boolean };
export const KERB_RETURN_DEFAULT: KerbReturnStd = { byClass: { primary: 6, district: 8, access: 6 }, concave: 2.0, bay: 1.4, civic: 2.0, clear: 3 }; // standards.streetscape.kerbReturn, Datum 2026-10-24; a plan without the key takes these
/** Chords per quarter turn of an arc: 16 (5.6 degrees each) from 4 m radius up, 8 below it. */
export const SEGMENTS = { quarter: 16, small: 8, smallBelow: 4 };
/** The shortest straight kerb a return leaves on an edge, m. */
export const KEEP_STRAIGHT = 0.5;
const RANK: Record<string, number> = { access: 1, district: 2, primary: 3 };
const ROADS = ['primary', 'district', 'access'];

export const kerbReturnStd = (S: { kerbReturn?: Partial<KerbReturnStd> } | undefined): KerbReturnStd => {
  const k = S?.kerbReturn;
  return { byClass: { ...KERB_RETURN_DEFAULT.byClass, ...(k?.byClass ?? {}) }, concave: k?.concave ?? KERB_RETURN_DEFAULT.concave, bay: k?.bay ?? KERB_RETURN_DEFAULT.bay, civic: k?.civic ?? KERB_RETURN_DEFAULT.civic, bendOuter: ((k as unknown as { bend?: { outer?: string } } | undefined)?.bend?.outer ?? 'concentric') === 'concentric', nose: (k as unknown as { nose?: unknown } | undefined)?.nose !== false, clear: (k as { landings?: { clear?: number } } | undefined)?.landings?.clear ?? KERB_RETURN_DEFAULT.clear, off: k?.off };
};

export type Return = {
  id: string; plot: string; /** index of the vertex in the plot's polyV, counter-clockwise */ at: V;
  convex: boolean; /** a corner of the civic block, or across the road from a civic crossing's partner landing: kerbReturn.civic, and its landings stay where the block puts them */ civic: boolean; /** the minor road's class (the radius is that class's) */ cls: string; roads: [string, string];
  /** radius as built (m) and tangent length (m): a shrunk return has less than the standard's */ r: number; want: number; t: number;
  a: V; b: V; c: V; /** the vertex's deflection, radians, |.| */ theta: number;
  /** a .. b along the arc, inclusive: `n + 1` points */ arc: V[]; n: number;
  /** the region between the old corner and the arc (convex only), counter-clockwise or clockwise as made: vertex, then a .. b along the arc */ cut: V[];
  /** unit vectors: travel along the incoming edge, then the outgoing one (counter-clockwise ring) */ u1: V; u2: V;
  /** signed sweep from a to b about c (radians): + counter-clockwise */ sweep: number;
  /** the outward (carriageway-ward) normals of the two kerb faces the arc joins (convex only) */ n1: V; n2: V;
  /** the level the carriageway fill lies at: the road's 0, a kerbed bay's floor 0.02 */ level: number;
  /** the kerbed bay this return rounds (a tip of its mouth, or a back corner) */ bay?: string;
  /** the circus mouth face (plan roads.kerbs.circusMouths.faces[].id) whose kerb line this return joins to the ring's outer kerb */ mouth?: string;
  /** a median's end (kerbReturn.nose): the bullnose, a half round of half the island's width, its centre inside the pavement; no carriageway fill, no cut: the road runs under the median */ nose?: boolean;
  /** a road bend's outside kerb (kerbReturn.bend): the id of the inside return it is concentric with; the region `cut` is pavement laid into the road's corner (a slab), not carriageway */ bend?: string;
  /** a bend where two roads meet end to end (no mitre): the uncovered square at the outside of the turn, up to the outside kerb arc: the node, then the arc a .. b (the carriageway there, laid as a flat) */ hole?: V[];
  /** a corner of a junction box on the sheet's edge, beside a beyond block or the quay (no plot has a vertex there) */ edge?: boolean;
};
/** A kerbed bay (standards.streetscape.roads.kerbs.bays.kerbed) with its corners rounded by kerbReturn.bay: the lay-by in the road, mouth open on the carriageway, its pavement corners at the mouth (convex) and its back corners (concave) arcs of one radius.
 *  `notch` is the outline the plot slab takes round it, from the mouth's first point `M[0]` to the second `M[1]`, `ring` the bay flat's own (convex) ring with its back corners rounded, `faces` the straight kerb faces between the arcs (end, back, end), `n` into the bay. */
export type BayShape = { id: string; plot: string; road: string; cls: string; M: [V, V]; u: V; nin: V; width: number; depth: number; r: number; notch: V[]; ring: V[]; faces: { a: V; b: V; n: V }[]; returns: Return[] };

const key = (p: V) => `${Math.round(p[0] * 20)},${Math.round(p[1] * 20)}`;

/** The corners that take kerbReturn.civic instead of their class's radius: the civic block's own plot corners, and the corners across the road within CIVIC_REACH m (along the kerb) of the kerb point where a civic crossing's partner landing stands
 *  (the block's dressing, civic-block.json dressing.crossings.at, sits 0.16 to 5 m from those corners and is laid to a square corner: a bigger return would put its landings, planters and cypresses over the kerb). Returns `plot|vertex` keys. */
export const CIVIC_REACH = 6;
function civicCorners(ctx: Pick<Ctx, 'plan' | 'd' | 'civ'>, ring: (plot: string) => V[]): Set<string> {
  const out = new Set<string>();
  for (const pl of ctx.d.plots) if (pl.civic) for (const v of pl.polyV) out.add(`${pl.id}|${key(v)}`);
  const at = (ctx.civ?.dressing as { crossings?: { at: { at: V; inward: V; road: string; plot: string }[] } } | undefined)?.crossings?.at ?? [];
  if (!at.length) return out;
  const toPlan = toPlanFn(ctx.plan.civic);
  for (const c of at) {
    const k = toPlan(c.at), road = ctx.d.roads.find(r => r.id === c.road); if (!road) continue;
    const inward = norm(sub(toPlan([c.at[0] + c.inward[0], c.at[1] + c.inward[1]]), k)), partner = sub(k, mul(inward, road.w)); // the kerb point across the carriageway
    for (const pl of ctx.d.plots) if (!pl.civic) for (const v of ring(pl.id)) if (dist(v, partner) <= CIVIC_REACH) out.add(`${pl.id}|${key(v)}`);
  }
  return out;
}
type Idx = { list: Return[]; byVertex: Map<string, Return>; grid: Map<string, Return[]>; std: KerbReturnStd; bays: BayShape[] };
/** Keyed on the derived plan, then the streetscape standards, then the civic block (the result reads all three: Redline brief 064 #10), then a fingerprint of the keys it reads most: kerbReturn and the kerbed bays, so a standard edited in place on the same objects is
 *  another list too. The rest of the standards (the circus mouths, the sections) are read by identity: replace `S`, do not edit them in place. A missing key stands as a shared empty one. */
const cache = new WeakMap<object, WeakMap<object, WeakMap<object, Map<string, Idx>>>>(), NONE = {};
const fingerprint = (S: Ctx['S'] | undefined): string => JSON.stringify([S?.kerbReturn, S?.roads?.kerbs?.bays?.kerbed, !!S?.sections]);

const CELL = 32;
const cellsOf = (r: Return): string[] => {
  const xs = r.arc.map(p => p[0]).concat([r.at[0]]), ys = r.arc.map(p => p[1]).concat([r.at[1]]), out: string[] = [];
  for (let x = Math.floor((Math.min(...xs) - 2) / CELL); x <= Math.floor((Math.max(...xs) + 2) / CELL); x++) for (let y = Math.floor((Math.min(...ys) - 2) / CELL); y <= Math.floor((Math.max(...ys) + 2) / CELL); y++) out.push(`${x},${y}`);
  return out;
};

export function arcPoints(c: V, r: number, from: V, sweep: number, n: number): V[] {
  const a0 = Math.atan2(from[1] - c[1], from[0] - c[0]);
  return Array.from({ length: n + 1 }, (_, k) => { const a = a0 + (sweep * k) / n; return [c[0] + r * Math.cos(a), c[1] + r * Math.sin(a)] as V; });
}
export const segmentsFor = (r: number, theta: number): number => Math.max(2, Math.ceil(theta / ((Math.PI / 2) / (r < SEGMENTS.smallBelow ? SEGMENTS.small : SEGMENTS.quarter))));

/** One arc as a Return: vertex `v`, tangent points `a` and `b` in path order, centre `c`, radius r. */
function arcReturn(id: string, plot: string, bay: string, road: string, cls: string, v: V, a: V, b: V, c: V, r: number, convex: boolean, n1: V, n2: V): Return {
  const ua = sub(a, c), ub = sub(b, c), sweep = Math.atan2(ua[0] * ub[1] - ua[1] * ub[0], ua[0] * ub[0] + ua[1] * ub[1]), theta = Math.abs(sweep), n = segmentsFor(r, theta), arc = arcPoints(c, r, a, sweep, n);
  arc[0] = a; arc[n] = b;
  return { id, plot, at: v, convex, civic: false, cls, roads: [road, road], r, want: r, t: r, a, b, c, theta, arc, n, cut: [v, ...arc], u1: norm(sub(v, a)), u2: norm(sub(b, v)), sweep, n1, n2, level: 0.02, bay };
}
/** A kerbed bay's rounded outline (kerbReturn.bay): read from the block's bay polygon (plan frame), never copied: its edge on the road's carriageway edge is the mouth, the other three are the ends and the back. */
function bayShape(ctx: Pick<Ctx, 'plan' | 'd' | 'civ'>, kb: { bay: string; road: string }, std: KerbReturnStd): BayShape | undefined {
  const bay = ctx.civ?.bays.find(b => b.id === kb.bay), road = ctx.d.roads.find(r => r.id === kb.road); if (!bay || !road || !(std.bay > 0)) return undefined;
  const P = bay.poly as V[], onEdge = (q: V) => Math.abs(lineDist(q, road.line) - road.w / 2) <= 0.1, m = P.findIndex((q, i) => onEdge(q) && onEdge(P[(i + 1) % P.length]!));
  if (m < 0 || P.length !== 4) return undefined;
  const M0 = P[m]!, M1 = P[(m + 1) % 4]!, u = norm(sub(M1, M0)), c: V = [P.reduce((t, q) => t + q[0], 0) / 4, P.reduce((t, q) => t + q[1], 0) / 4];
  let nin: V = [-u[1], u[0]]; if ((c[0] - M0[0]) * nin[0] + (c[1] - M0[1]) * nin[1] < 0) nin = [-nin[0], -nin[1]];
  const width = dist(M0, M1), depth = Math.max(...P.map(q => (q[0] - M0[0]) * nin[0] + (q[1] - M0[1]) * nin[1])), r = Math.min(std.bay, depth / 2 - 0.05, width / 2 - 0.05);
  if (!(r >= 0.3)) return undefined;
  const mid = add(add(M0, mul(u, width / 2)), mul(nin, 0.5)), pl = ctx.d.plots.find(p => inPoly(mid, p.polyV)); if (!pl) return undefined;
  const at = (p: V, a: number, b: number): V => add(add(p, mul(u, a)), mul(nin, b)), id = `layby/${kb.bay}`, cls = road.class; // not `bay/`: Layers.IsBay reads that as a slab cut
  const Bk0 = at(M0, 0, depth), Bk1 = at(M1, 0, depth), out: V = mul(nin, -1);
  const t0 = arcReturn(`${id}/a`, pl.id, kb.bay, road.id, cls, M0, at(M0, -r, 0), at(M0, 0, r), at(M0, -r, r), r, true, out, u);
  const k0 = arcReturn(`${id}/c`, pl.id, kb.bay, road.id, cls, Bk0, at(Bk0, 0, -r), at(Bk0, r, 0), at(Bk0, r, -r), r, false, out, u);
  const k1 = arcReturn(`${id}/d`, pl.id, kb.bay, road.id, cls, Bk1, at(Bk1, -r, 0), at(Bk1, 0, -r), at(Bk1, -r, -r), r, false, out, mul(u, -1));
  const t1 = arcReturn(`${id}/b`, pl.id, kb.bay, road.id, cls, M1, at(M1, 0, r), at(M1, r, 0), at(M1, r, r), r, true, out, mul(u, -1));
  return {
    id: kb.bay, plot: pl.id, road: road.id, cls, M: [M0, M1], u, nin, width, depth, r, notch: [...t0.arc, ...k0.arc, ...k1.arc, ...t1.arc], ring: [M0, ...k0.arc, ...k1.arc, M1],
    faces: [{ a: t0.b, b: k0.a, n: u }, { a: k0.b, b: k1.a, n: out }, { a: k1.b, b: t1.a, n: mul(u, -1) }], returns: [t0, t1, k0, k1],
  };
}
/** The outside of a road's bend (kerbReturn.bend): where a road turns on itself at a node nothing else meets (the U lanes' ten bends, the ring's south-east corner), the inside corner takes its return and the outside kerb is concentric with it,
 *  R = inside R + the carriageway's width, so the carriageway keeps its width round the turn and both tangent points lie on one line across the road (landings pair square). The outside corner is two plots' corners (or a beyond block and the quay) meeting
 *  on a party line: the fillet is pavement laid into the road's corner (`cut` is the slab), never a plot change. */
function bendReturns(ctx: Pick<Ctx, 'plan' | 'd'>, inner: Return[]): Return[] {
  const out: Return[] = [];
  for (const r of inner) {
    if (!r.convex || r.bay || r.mouth || r.bend || r.civic) continue; // a civic corner (2 m) keeps the outside of its turn square: a 14 m concentric arc would stand over the block's crossings and dressing (Vantage's adjustment first)
    const A = ctx.d.roads.find(x => x.id === r.roads[0]), B = ctx.d.roads.find(x => x.id === r.roads[1]); if (!A || !B || A.gyratory || B.gyratory) continue;
    const w = (A.w + B.w) / 2; if (Math.abs(A.w - B.w) > 0.01) continue;
    // a bend: a node of one road it passes through (never its ends) that no other road meets, or the shared end of two roads and nothing else
    const isEnd = (rd: typeof A, n: string) => rd.path[0] === n || rd.path[rd.path.length - 1] === n;
    const node = A.path.find(n => {
      if (dist(ctx.plan.nodes[n]!, r.at) >= w) return false;
      const users = ctx.d.roads.filter(x => x.path.includes(n));
      return A === B ? users.length === 1 && !isEnd(A, n) : users.length === 2 && users.includes(A) && users.includes(B) && isEnd(A, n) && isEnd(B, n);
    });
    if (!node) continue;
    // the path turns at the node by about the corner's own angle
    const pts = (rd: typeof A) => rd.path.map(n => ctx.plan.nodes[n]!), pa = pts(A), pb = pts(B), here = ctx.plan.nodes[node]!, ia = A.path.indexOf(node), ib = B.path.indexOf(node);
    const before = A === B ? pa[ia - 1]! : (ia === 0 ? pa[1]! : pa[pa.length - 2]!), after = A === B ? pa[ia + 1]! : (ib === 0 ? pb[1]! : pb[pb.length - 2]!);
    const t1 = norm(sub(here, before)), t2 = norm(sub(after, here)), turn = Math.abs(Math.atan2(t1[0] * t2[1] - t1[1] * t2[0], t1[0] * t2[0] + t1[1] * t2[1])); if (Math.abs(turn - r.theta) > 0.12) continue;
    const R = r.r + w, V = add(r.c, mul(add(r.n1, r.n2), R)), a = add(r.c, mul(r.n1, R)), b = add(r.c, mul(r.n2, R)), n = segmentsFor(R, r.theta), arc = arcPoints(r.c, R, a, r.sweep, n); arc[0] = a; arc[n] = b;
    out.push({ id: `bend/${r.id}`, plot: r.plot, at: V, convex: false, civic: false, cls: r.cls, roads: r.roads, r: R, want: R, t: R * Math.tan(r.theta / 2), a, b, c: r.c, theta: r.theta, arc, n, cut: [V, ...arc], u1: r.u1, u2: r.u2, sweep: r.sweep, n1: r.n1, n2: r.n2, level: 0, bend: r.id, ...(A !== B ? { hole: [ctx.plan.nodes[node]!, ...arc] } : {}) });
  }
  return out;
}

/** The medians (and tram-free island reservations are not here): every median's two ends are bullnoses (kerbReturn.nose): a half round of half the island's width (a 4 m median R 2, a 6 m R 3), its tip where the end is today, so the median is never shortened. */
function noseReturns(ctx: Pick<Ctx, 'plan' | 'd'>): Return[] {
  const out: Return[] = [];
  ctx.d.zones.filter(z => z.kind === 'median' && z.poly).forEach((z, zi) => {
    const P = z.poly as V[]; if (P.length !== 4) return;
    const e = [0, 1, 2, 3].map(i => dist(P[i]!, P[(i + 1) % 4]!)), short = e[0]! < e[1]! ? [0, 2] : [1, 3], r = Math.min(e[0]!, e[1]!) / 2;
    short.forEach((i, k) => {
      const p = P[i]!, q = P[(i + 1) % 4]!, m: V = [(p[0] + q[0]) / 2, (p[1] + q[1]) / 2], cz: V = [(P[0]![0] + P[2]![0]) / 2, (P[0]![1] + P[2]![1]) / 2], dir = norm(sub(m, cz)), c = sub(m, mul(dir, r)), tip = add(m, mul(dir, 0)), perp: V = [-dir[1], dir[0]];
      const a = add(c, mul(perp, r)), b = sub(c, mul(perp, r)), ua = sub(a, c), ut = sub(tip, c), sweep = Math.PI * (Math.sign(ua[0] * ut[1] - ua[1] * ut[0]) || 1), n = segmentsFor(r, Math.PI), arc = arcPoints(c, r, a, sweep, n); arc[0] = a; arc[n] = b;
      out.push({ id: `nose/${zi}/${k}`, plot: `median/${zi}`, at: tip, convex: true, civic: false, cls: 'district', roads: ['', ''], r, want: r, t: 0, a, b, c, theta: Math.PI, arc, n, cut: [], u1: dir, u2: dir, sweep, n1: dir, n2: dir, level: 0, nose: true });
    });
  });
  return out;
}
/** A median's ring with its two ends made bullnoses (the long edges stop `r` short of each end and a half round turns between them). */
export function noseRing(ctx: Pick<Ctx, 'plan' | 'd' | 'S'>, zi: number, ring: V[]): V[] {
  const mine = indexOf(ctx).list.filter(r => r.nose && r.plot === `median/${zi}`); if (mine.length !== 2 || ring.length !== 4) return ring;
  const e = [0, 1, 2, 3].map(i => dist(ring[i]!, ring[(i + 1) % 4]!)), short = e[0]! < e[1]! ? [0, 2] : [1, 3], out: V[] = [];
  for (let i = 0; i < 4; i++) {
    const k = short.indexOf(i);
    if (k >= 0) { // this vertex starts a short edge: its two corners give way to the arc whose tip lies on that edge, in the order the ring runs
      const p = ring[i]!, q = ring[(i + 1) % 4]!, m: V = [(p[0] + q[0]) / 2, (p[1] + q[1]) / 2], nose = mine.reduce((b, x) => (dist(x.at, m) < dist(b.at, m) ? x : b));
      out.push(...(dist(nose.a, p) < dist(nose.a, q) ? nose.arc : [...nose.arc].reverse()));
    } else if (!short.includes((i + 3) % 4)) out.push(ring[i]!); // a vertex that ends a short edge is the arc's
  }
  return out;
}

/** The circus mouths (plan roads.kerbs.circusMouths, kerbReturn.applies): each radial road's kerb face runs from the ring's outer kerb (circle, the carriageway inside it) out to the plot corner at the circus pavement; the pavement corner it makes with the ring is 102 or 109 degrees,
 *  and the arc of the minor road's radius is tangent to the face's line and to the ring (externally: its centre stands R from the line and ring + R from the ring's centre). The circus pavement band has no plot: nothing to cut but the ground it stands on. */
function mouthReturns(ctx: Pick<Ctx, 'plan' | 'd'>, std: KerbReturnStd): Return[] {
  const out: Return[] = [], cc = ctx.plan.circus, faces = ((ctx as { S?: { roads?: { kerbs?: { circusMouths?: { faces: { id: string; road: string; a: V; b: V; toward: V }[] } } } } }).S?.roads?.kerbs?.circusMouths?.faces) ?? [];
  const rc = cc.ring[1];
  for (const f of faces) {
    const road = ctx.d.roads.find(r => r.id === f.road); if (!road) continue;
    const u = norm(sub(f.b, f.a)), n = norm(f.toward), r = std.byClass[road.class === 'primary' || road.class === 'district' || road.class === 'access' ? road.class : 'district'] ?? 0; if (!(r > 0)) continue;
    // the arc's centre: R off the line on the pavement side (-n), and rc + R from the circus centre: |f.a + u s - n R - cc| = rc + R
    const P0 = sub(f.a, mul(n, r)), d0 = sub(P0, cc.c), B = 2 * (d0[0] * u[0] + d0[1] * u[1]), C = d0[0] * d0[0] + d0[1] * d0[1] - (rc + r) * (rc + r), disc = B * B - 4 * C;
    if (disc < 0) continue;
    const s1 = (-B + Math.sqrt(disc)) / 2, s2 = (-B - Math.sqrt(disc)) / 2, s = s1 >= 0 ? (s2 >= 0 ? Math.min(s1, s2) : s1) : s2; // the root nearer the corner, outward along the face
    if (!(s > 0) || s > dist(f.a, f.b) - 0.5) continue;
    const c = add(P0, mul(u, s)), a = add(f.a, mul(u, s)), radial = norm(sub(c, cc.c)), b = add(cc.c, mul(radial, rc)), toCirc = mul(radial, -1);
    const ret = arcReturn(`circus-mouth/${f.id}`, 'crown-circus', '', f.road, road.class, f.a, b, a, c, r, true, toCirc, n); // path order: from the ring's tangent point out along the face
    out.push({ ...ret, bay: undefined, level: 0, t: s, mouth: f.id, civic: false, roads: [f.road, 'crown-circus'], u1: radial, u2: u });
  }
  return out;
}

/** The corners of a junction box no plot has a vertex at (kerbReturn.applies: "the beyond blocks' corners where a road runs on to the sheet edge"): where two roads meet at a node, each corner of the box whose three quadrants are carriageway and whose fourth is not
 *  (the pavement quadrant: a beyond block, the quay) and where no return stands already. Same rule as a plot's corner: the minor road's class, an arc tangent to both kerb lines, the carriageway fill. Straight roads at any angle (the plan's junctions are right angles). */
function edgeReturns(ctx: Pick<Ctx, 'plan' | 'd'>, std: KerbReturnStd, have: Return[]): Return[] {
  const out: Return[] = [], roads = ctx.d.roads.filter(r => !r.gyratory), cc = ctx.plan.circus;
  const road = (p: V) => roads.some(r => lineDist(p, r.line) < r.w / 2 - 0.02) || dist(p, cc.c) < cc.ring[1];
  const dirAt = (r: (typeof roads)[number], N: V): V => { let best = Infinity, u: V = [1, 0]; r.line.forEach((a, i) => { const b = r.line[i + 1]; if (!b) return; const d = segDist(N, a, b); if (d < best) { best = d; u = norm(sub(b, a)); } }); return u; };
  const seen = new Set<string>();
  for (const A of roads) for (const B of roads) {
    if (A.id >= B.id) continue;
    for (const n of A.path.filter(x => B.path.includes(x))) {
      const N = ctx.plan.nodes[n]!, tA = dirAt(A, N), tB = dirAt(B, N), det = tA[0] * tB[1] - tA[1] * tB[0]; if (Math.abs(det) < 0.5) continue;
      const nA: V = [-tA[1], tA[0]], nB: V = [-tB[1], tB[0]];
      for (const sA of [-1, 1]) for (const sB of [-1, 1]) {
        // the corner: the point sA·w_A/2 off A's axis and sB·w_B/2 off B's: N + a·nA = N' ... solve for the intersection of the two edge lines
        const oA = add(N, mul(nA, sA * A.w / 2)), oB = add(N, mul(nB, sB * B.w / 2)), r = sub(oB, oA), k = (r[0] * tB[1] - r[1] * tB[0]) / det, P = add(oA, mul(tA, k));
        const key = `${Math.round(P[0] * 10)},${Math.round(P[1] * 10)}`; if (seen.has(key)) continue; seen.add(key);
        const ends = ([[1, 1], [1, -1], [-1, 1], [-1, -1]] as [number, number][]).map(([a, b]) => ({ e1: mul(tA, a), e2: mul(tB, b) }));
        const free = ends.filter(({ e1, e2 }) => !road(add(P, mul(norm(add(e1, e2)), 0.6))));
        const roadQ = ends.filter(({ e1, e2 }) => road(add(P, mul(norm(add(e1, e2)), 0.6))));
        if (free.length !== 1 || roadQ.length !== 3) continue;
        if (have.some(h => dist(h.at, P) < 0.3)) continue;
        if (isFootwayCorner(ctx as Pick<Ctx, 'plan' | 'd' | 'S'>, [A.id, B.id], P)) continue; // brief 061: a gate's lane mouth corner is no junction box corner either
        const { e1, e2 } = free[0]!, alpha = Math.acos(Math.max(-1, Math.min(1, dot(e1, e2)))), cls = RANK[A.class]! <= RANK[B.class]! ? A.class : B.class, R = std.byClass[cls]; if (!(R > 0)) continue;
        const t = R / Math.tan(alpha / 2), c = add(P, mul(norm(add(e1, e2)), R / Math.sin(alpha / 2)));
        const n1 = norm(sub([0, 0], sub(e2, mul(e1, dot(e1, e2))))), n2 = norm(sub([0, 0], sub(e1, mul(e2, dot(e1, e2)))));
        out.push(arcReturn(`edge/${key}`, 'edge', '', `${A.id}`, cls, P, add(P, mul(e1, t)), add(P, mul(e2, t)), c, R, true, n1, n2));
        const o = out[out.length - 1]!; o.bay = undefined; o.edge = true; o.level = 0; o.roads = [A.id, B.id]; o.t = t; o.theta = Math.PI - alpha;
      }
    }
  }
  return out;
}

/** The kerbed bays, with their rounded outlines. */
export const baysOf = (ctx: Pick<Ctx, 'plan' | 'd' | 'S'> & Partial<Ctx>): BayShape[] => indexOf(ctx).bays;

/** Every kerb return of the district, once. Cached per derived plan, standards and civic block. */
export function returnsOf(ctx: Pick<Ctx, 'plan' | 'd' | 'S'> & Partial<Ctx>): Return[] { return indexOf(ctx).list; }
function indexOf(ctx: Pick<Ctx, 'plan' | 'd' | 'S'> & Partial<Ctx>): Idx {
  const sKey = (ctx.S as object | undefined) ?? NONE, cKey = (ctx.civ as object | undefined) ?? NONE, fp = fingerprint(ctx.S);
  const bySd = cache.get(ctx.d) ?? cache.set(ctx.d, new WeakMap()).get(ctx.d)!, byCiv = bySd.get(sKey) ?? bySd.set(sKey, new WeakMap()).get(sKey)!, byFp = byCiv.get(cKey) ?? byCiv.set(cKey, new Map()).get(cKey)!;
  const hit = byFp.get(fp); if (hit) return hit;
  const std = kerbReturnStd(ctx.S), list: Return[] = [], bays: BayShape[] = [];
  if (!std.off && ctx.S?.sections) {
    const civicKeys = civicCorners(ctx as Pick<Ctx, 'plan' | 'd' | 'civ'>, id => plotRing(ctx as Ctx, ctx.d.plots.find(p => p.id === id)!).p);
    for (const pl of ctx.d.plots) {
      const ring = plotRing(ctx as Ctx, pl), n = ring.p.length, vs = ring.p;
      type Want = { i: number; convex: boolean; civic: boolean; cls: string; roads: [string, string]; theta: number; r: number; t: number; u1: V; u2: V };
      const want: (Want | undefined)[] = vs.map((v, i): Want | undefined => {
        const cin = ring.c[(i + n - 1) % n]!, cout = ring.c[i]!;
        if (!ROADS.includes(kindOf(cin)) || !ROADS.includes(kindOf(cout))) return undefined;
        if (isFootwayCorner(ctx as Ctx, [idOf(cin), idOf(cout)], v)) return undefined; // brief 061: a gate's lane mouth is a continuous footway (street/footway.ts): its two corners stay square, the pavement runs on across
        const prev = vs[(i + n - 1) % n]!, next = vs[(i + 1) % n]!, u1 = norm(sub(v, prev)), u2 = norm(sub(next, v));
        const cr = u1[0] * u2[1] - u1[1] * u2[0], dt = u1[0] * u2[0] + u1[1] * u2[1], theta = Math.abs(Math.atan2(cr, dt));
        if (theta < 0.35 || theta > 2.6) return undefined; // a straight run split by a class change (0 degrees), or a hairpin
        const convex = cr > 0, isCivic = civicKeys.has(`${pl.id}|${key(v)}`), cls = (RANK[kindOf(cin)]! <= RANK[kindOf(cout)]! ? kindOf(cin) : kindOf(cout));
        const r = convex ? (isCivic ? std.civic : std.byClass[cls]) : std.concave;
        if (!(r > 0)) return undefined;
        return { i, convex, civic: isCivic && convex, cls, roads: [idOf(cin), idOf(cout)], theta, r, t: r * Math.tan(theta / 2), u1, u2 };
      });
      // an edge too short for both of its ends' tangents: both shrink in proportion (never below nothing), leaving KEEP_STRAIGHT m of straight kerb
      const tan = want.map(w => (w ? w.t : 0));
      for (let i = 0; i < n; i++) {
        const j = (i + 1) % n, len = dist(vs[i]!, vs[j]!), tot = tan[i]! + tan[j]!;
        if (tot > 0 && tot > len - KEEP_STRAIGHT) { const f = Math.max(0, len - KEEP_STRAIGHT) / tot; tan[i] = tan[i]! * Math.min(1, f); tan[j] = tan[j]! * Math.min(1, f); }
      }
      want.forEach((w, i) => {
        if (!w) return;
        const t = tan[i]!; if (t < 0.4) return;
        const r = t / Math.tan(w.theta / 2), v = vs[i]!, a = sub(v, mul(w.u1, t)), b = add(v, mul(w.u2, t)), sgn = w.convex ? 1 : -1;
        const nl: V = [-w.u1[1], w.u1[0]], c = add(a, mul(nl, sgn * r)), nSeg = segmentsFor(r, w.theta), arc = arcPoints(c, r, a, sgn * w.theta, nSeg);
        arc[0] = a; arc[nSeg] = b; // exact ends: the straight kerb meets the arc on the very point
        list.push({ id: `${pl.id}/v${i}`, plot: pl.id, at: v, convex: w.convex, civic: w.civic, cls: w.cls, roads: w.roads, r, want: w.r, t, a, b, c, theta: w.theta, arc, n: nSeg, cut: [v, ...arc], u1: w.u1, u2: w.u2, sweep: sgn * w.theta, n1: [w.u1[1], -w.u1[0]], n2: [w.u2[1], -w.u2[0]], level: 0 });
      });
    }
    for (const kb of (ctx.S?.roads?.kerbs?.bays?.kerbed ?? []) as { bay: string; road: string }[]) { const b = bayShape(ctx as Pick<Ctx, 'plan' | 'd' | 'civ'>, kb, std); if (b) { bays.push(b); list.push(...b.returns); } }
    list.push(...mouthReturns(ctx as Pick<Ctx, 'plan' | 'd' | 'S'>, std));
    list.push(...edgeReturns(ctx as Pick<Ctx, 'plan' | 'd'>, std, list.slice()));
    if (std.nose !== false) list.push(...noseReturns(ctx as Pick<Ctx, 'plan' | 'd'>));
    if (std.bendOuter !== false) list.push(...bendReturns(ctx as Pick<Ctx, 'plan' | 'd'>, list.slice()));
  }
  const byVertex = new Map<string, Return>(), grid = new Map<string, Return[]>();
  for (const r of list) { if (!r.bay && !r.bend) byVertex.set(`${r.plot}|${key(r.at)}`, r); for (const k of cellsOf(r)) (grid.get(k) ?? grid.set(k, []).get(k)!).push(r); }
  const out: Idx = { list, byVertex, grid, std, bays };
  byFp.set(fp, out);
  return out;
}

/** The return at a plot's vertex, if it has one. */
export const returnAt = (ctx: Pick<Ctx, 'plan' | 'd' | 'S'>, plot: string, v: V): Return | undefined => indexOf(ctx).byVertex.get(`${plot}|${key(v)}`);
/** The tangent length (m) a plot vertex's return takes off each of its two edges (0: no return there). */
export const tangentAt = (ctx: Pick<Ctx, 'plan' | 'd' | 'S'>, plot: string, v: V): number => returnAt(ctx, plot, v)?.t ?? 0;

/** The plot's slab ring with its returns cut (convex) and filleted (concave): the plot polygon with each corner vertex replaced by its arc, and a kerbed bay's mouth replaced by the bay's rounded notch. The ring keeps the plot's orientation and every other vertex. */
export function slabRing(ctx: Pick<Ctx, 'plan' | 'd' | 'S'>, plot: { id: string; polyV: V[] }): V[] {
  const idx = indexOf(ctx); if (!idx.list.length) return plot.polyV;
  const cw = areaOf(plot.polyV) < 0, pts = cw ? [...plot.polyV].reverse() : plot.polyV, mine = idx.bays.filter(b => b.plot === plot.id);
  if (!mine.length && !pts.some(p => idx.byVertex.has(`${plot.id}|${key(p)}`))) return plot.polyV;
  const out: V[] = [];
  pts.forEach((p, i) => {
    const r = idx.byVertex.get(`${plot.id}|${key(p)}`); if (r) out.push(...r.arc); else out.push(p);
    const q = pts[(i + 1) % pts.length]!;
    for (const b of mine) {
      if (!(segDist(b.M[0], p, q) < 0.05 && segDist(b.M[1], p, q) < 0.05)) continue;
      const fwd = (q[0] - p[0]) * b.u[0] + (q[1] - p[1]) * b.u[1] > 0; // the ring runs along the mouth from M[0] to M[1], or back
      out.push(...(fwd ? b.notch : [...b.notch].reverse()));
    }
  });
  return cw ? out.reverse() : out;
}

/** The carriageway fill of a convex return: the region between the old corner and the arc, grown `lap` m under the plot slab (the arc side) and `lap` m over the carriageway (the two kerb lines) so no crack shows. */
export function fillRing(r: Return, lap = 0.05): V[] {
  if (!r.convex) return [];
  const n1 = r.n1, n2 = r.n2; // outward (carriageway-ward) normals of the two kerb faces
  const toward = (p: V): V => { const d = sub(r.c, p), l = Math.hypot(d[0], d[1]) || 1; return [d[0] / l, d[1] / l]; };
  const inner = r.arc.map(p => add(p, mul(toward(p), lap)));
  inner[0] = add(r.a, mul(n1, lap)); inner[r.n] = add(r.b, mul(n2, lap)); // the ends sit on the kerb lines, pushed onto the carriageway
  return [add(r.at, mul(add(n1, n2), lap)), ...inner];
}

const gridAt = (idx: Idx, x: number, y: number) => idx.grid.get(`${Math.floor(x / CELL)},${Math.floor(y / CELL)}`) ?? [];

/** True if p lies in the carriageway a convex return takes (inside the old corner and outside the arc), or within `g` m of its kerb arc. */
export function inCut(ctx: Pick<Ctx, 'plan' | 'd' | 'S'>, p: V, g = 0): boolean {
  const idx = indexOf(ctx); if (!idx.list.length) return false;
  for (const r of gridAt(idx, p[0], p[1])) {
    if (!r.convex || r.nose) continue;
    if (inPoly(p, r.cut) || segDist(p, r.at, r.a) < 1e-6 + g * 0.5 || segDist(p, r.at, r.b) < 1e-6 + g * 0.5) return true; // inside, or on the old kerb line between the corner and a tangent point (half the margin: the line's end is the tangent point)
    if (g > 0) for (let i = 0; i < r.n; i++) if (segDist(p, r.arc[i]!, r.arc[i + 1]!) < g) return true;
  }
  return false;
}

/** The returns whose region (grown by g) a ring's bounding box touches. */
function near(idx: Idx, ring: V[], g: number): Return[] {
  const xs = ring.map(p => p[0]), ys = ring.map(p => p[1]), x0 = Math.min(...xs) - g, x1 = Math.max(...xs) + g, y0 = Math.min(...ys) - g, y1 = Math.max(...ys) + g, seen = new Set<Return>();
  for (let x = Math.floor(x0 / CELL); x <= Math.floor(x1 / CELL); x++) for (let y = Math.floor(y0 / CELL); y <= Math.floor(y1 / CELL); y++) for (const r of idx.grid.get(`${x},${y}`) ?? []) seen.add(r);
  return [...seen].filter(r => r.convex && !r.nose);
}

const segsHit = (p1: V, p2: V, q1: V, q2: V): boolean => {
  const cr = (o: V, a: V, b: V) => (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0]);
  return cr(p1, p2, q1) * cr(p1, p2, q2) < 0 && cr(q1, q2, p1) * cr(q1, q2, p2) < 0;
};
/** The first convex return whose carriageway region a ring (a footprint, a strip, a flat piece) overlaps, or whose kerb arc it comes within `g` m of; undefined if it stands clear of every one. */
export function cutHits(ctx: Pick<Ctx, 'plan' | 'd' | 'S'>, ring: V[], g = 0): Return | undefined {
  const idx = indexOf(ctx); if (!idx.list.length || !ring.length) return undefined;
  for (const r of near(idx, ring, g)) {
    if (ring.some(p => inPoly(p, r.cut)) || r.cut.some(q => inPoly(q, ring))) return r;
    for (let i = 0; i < ring.length; i++) {
      const a = ring[i]!, b = ring[(i + 1) % ring.length]!;
      for (let k = 0; k < r.n; k++) { if (segsHit(a, b, r.arc[k]!, r.arc[k + 1]!)) return r; if (g > 0 && (segDist(a, r.arc[k]!, r.arc[k + 1]!) < g || segDist(r.arc[k]!, a, b) < g)) return r; }
    }
    if (g > 0) for (const q of r.arc) if (ring.some((p, i) => segDist(q, p, ring[(i + 1) % ring.length]!) < g)) return r;
  }
  return undefined;
}

/** Straight chords along a return's arc, for a family laid `offset` m off the kerb face (+ toward the carriageway): the circle at r + offset (convex) or r - offset (concave), `step` rad per chord at most, never shorter than `minLen` m.
 *  Each chord is the piece's pivot (the chord's middle, on the circle compensated so the chord is wrong by half its sagitta each way), its direction normal `n` (toward the carriageway) and its length. */
/** The most a chord may fall inside its arc (m): a 6 m return keeps its 7.5 degree chords (sagitta 0.0129 at R 6.1); a bend's 18 or 30 m outer arc gets finer ones, so a third-person camera sees a curve, not a polygon (Glyph 0.2.113 nit 4). */
const SAGITTA = 0.0135;
export function chordsOf(r: Return, offset: number, maxStep = Math.min(Math.PI / 24, 2 * Math.acos(Math.max(0, 1 - SAGITTA / Math.max(r.r + (r.convex ? 1 : -1) * offset, 0.5)))), minLen = 0.6): { at: V; n: V; len: number; mid: number; from: V; to: V }[] {
  const sg = r.convex ? 1 : -1, rad = r.r + sg * offset; if (!(rad > 0.05)) return [];
  const phi0 = Math.atan2(r.a[1] - r.c[1], r.a[0] - r.c[0]), sweep = r.sweep, arcLen = rad * r.theta;
  const nChord = Math.max(1, Math.min(Math.ceil(r.theta / maxStep), Math.floor(arcLen / minLen) || 1));
  const out: { at: V; n: V; len: number; mid: number; from: V; to: V }[] = [];
  for (let k = 0; k < nChord; k++) {
    const p0 = phi0 + (sweep * k) / nChord, p1 = phi0 + (sweep * (k + 1)) / nChord, pm = (p0 + p1) / 2, half = Math.abs(p1 - p0) / 2;
    const A: V = [r.c[0] + rad * Math.cos(p0), r.c[1] + rad * Math.sin(p0)], B: V = [r.c[0] + rad * Math.cos(p1), r.c[1] + rad * Math.sin(p1)];
    const rm = rad * Math.cos(half), radial: V = [Math.cos(pm), Math.sin(pm)];
    out.push({ at: [r.c[0] + rm * radial[0], r.c[1] + rm * radial[1]], n: [sg * radial[0], sg * radial[1]], len: 2 * rad * Math.sin(half), mid: pm, from: A, to: B });
  }
  return out;
}


/** The width of a median's ring across its length (its short side, or a half-rounded one's diameter): the extent along the normal of its longest edge. */
export function ringWidth(ring: V[]): number {
  let best = 0, u: V = [1, 0];
  ring.forEach((p, i) => { const q = ring[(i + 1) % ring.length]!, l = dist(p, q); if (l > best) { best = l; u = [(q[0] - p[0]) / l, (q[1] - p[1]) / l]; } });
  const n: V = [-u[1], u[0]], d = ring.map(p => p[0] * n[0] + p[1] * n[1]);
  return Math.max(...d) - Math.min(...d);
}

/** True if p lies on a median's bullnose (within `g` m of the half round's centre disc) or on the pavement a bend's outside fillet lays into the road's corner (inside its `cut`, or within `g` of the old kerb lines between the corner and the tangent points):
 *  the old straight kerb line is not a kerb edge there any more. */
export function onRoundedEnd(ctx: Pick<Ctx, 'plan' | 'd' | 'S'> & Partial<Ctx>, p: V, g = 0): boolean {
  for (const r of indexOf(ctx).list) {
    if (r.nose) { const along = (p[0] - r.c[0]) * r.u1[0] + (p[1] - r.c[1]) * r.u1[1], across = Math.abs(-(p[0] - r.c[0]) * r.u1[1] + (p[1] - r.c[1]) * r.u1[0]); if (along >= -g && along <= r.r + g && across <= r.r + g) return true; } // the square the half round replaces
    else if (r.bend && (inPoly(p, r.cut) || segDist(p, r.at, r.a) < 1e-6 + g || segDist(p, r.at, r.b) < 1e-6 + g)) return true;
  }
  return false;
}
