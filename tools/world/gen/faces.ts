// The every-face pass (brief 072-C; Sky 2026-10-27: "every" face of every building in the district is DecoKit kit, side walls "a)").
// After the wave-2 street bays (facades.ts), every wall face of every plot building and tower complex that no kit piece dresses yet and no
// neighbour's wall stands against gets kit:
//   - SideBay_3 / _4: the calm frontage-style side bay, on every wall the public sees;
//   - AlleyWall_3 / _4 (one module in a run of two or more takes the dock variant _3D / _4D, seeded by the face id): only on service backs (SERVICE below);
//   - SideBayStack_Base / _Mid / _Top: one module per storey band, on walls taller than 4 storeys (towers and the tall city beyond the play streets), on a
//     tier that starts above the street (a setback's upper storeys) and on the part of a party wall that stands above a lower neighbour;
//   - CornerPier_Out / _In (_3, _4, or _Base / _Mid / _Top per band): at every outside and inside corner between two dressed faces;
//   - ShopFill_3 / _4 and ShopStack_Base (Vela's placement rule, briefs/072/pieces/pieces.md "Pass 2"): a side run that an own-shopfront street tenancy
//     (FacesIn.tenancies) overlaps by more than 0.5 m is a shop run: its whole-wall column takes ShopFill_n for SideBay_n, its stacked column from the
//     pavement lays a ShopStack_Base row in place of the base band; either tiles k fillers of 3 m nearest R / 3 (no pilaster); the generator then links
//     each tenancy to the filler holding its midpoint (index.ts linkFacades), so its procedural shopfront is not drawn and its sign stays.
// Modules are 6 m on plan, tiled at the piece's width scale (tags.scale, 0.85-1.15); a whole-wall piece's height scale puts its top on H(n), a stack
// module's on its storey band. Only adopted pieces are placed (kitrefs.adopted): a face the adopted pieces cannot dress is logged as a gap with the
// pieces that would dress it, and keeps its procedural wall; the coverage check (tools/world/faces.ts, `bun run world:faces`) fails on every such face.
// Deterministic: same plan and catalogue, same modules (array order, ids from the building id, the dock pick seeded by the face id).
//
// STREAMLINE (brief 075-E, Sky 2026-10-28: "we've still lost all the lovely curved buildings"): the old DecoKit street's grammar is the default on the faces
// the public sees (briefs/075/composition.md; stream.ts):
//   - a public run of a building's whole wall (from the pavement to its top) takes the building's streamline class: the bay its street front carries
//     (facades.ts), else one drawn by the old recipe (StreamBay the majority, PortholeBay, BalconyBay, DecoBay, ArcadeBay for rhythm), one class per
//     building; its bay must fit the wall's height (0.9-1.1; storey-true in the play streets) and its 14 m body stand inside the building (a shallow wing,
//     an acute corner or a shop run keeps the side pieces);
//   - an outside corner between two such runs takes the class's rounded corner (StreamCorner_n), at a street junction DecoCorner_10 where the height fits
//     code 10, or CornerTower_12 where it is adopted outside the play streets on a building of 9 storeys or more;
//   - an outside corner where only one of the two runs is streamline (a public face meeting a service back) takes the class's StreamEnd, its front on the
//     face that leaves the corner and its rounded return on the one that enters it (the old pieces have no mirror: stream.ts KIT_X);
//   - walls the whole-wall pieces cannot reach (taller than DecoBay_10, an upper tier, a party wall above a lower neighbour) take Vela's stacked streamline
//     pieces once adopted (stream.ts STREAM_STACK), else SideBayStack; SideBay and AlleyWall stay on the rest (service backs, blank party walls, shallow wings).
//
// SERVICE, the rule for a "true service back" (Sky's "service walls only on true service backs"), from the plan only: a wall face is a service back when it
// faces no street (its building-line edge is classed party, court or none by tools/world/gen/edges.ts) and
//   (a) it is a court face: the inner ring of a perimeter block, the block's service yard; or
//   (b) its building is a port or industrial building (the docks' yards); or
//   (c) another plot building's non-street wall faces it within SERVICE_GAP m (two backs across a yard or lane).
// Every other face (street, quay and park faces, and non-street faces that look onto open ground, a square or a street-facing wall) is one the public sees.
//
// Piece frame (contract with Vela, brief 072; kit axes): front +Z out of the wall, the wall plane on the pivot (z 0), +X along the wall, pivot at the
// module's foot; the export point `kit.at` is the footprint centre as for every facade piece (the catalogue's pivotOffset, else the centre is the pivot).
// A corner pier's pivot is the corner (the two wall planes' meeting line), its front (+Z) the outward normal of the face that ends at the corner (face A, the
// incoming edge of the CCW ring), face A running from the pivot along +X; an outside pier's face B runs along -Z, an inside pier's along +Z (tools/decokit/sides.py).
// Its footprint box covers exactly its leg of each face, so the modules start where a clip of the box against the face line ends.
import type { Facade, Mass } from '../../../contracts/world/world.schema';
import type { KitAsset } from '../../../contracts/world/kit.schema';
import { type V, cross, dist, dot, norm, sub } from '../../plan/geom';
import { type Ctx, r3, r4, rng } from './common';
import { adopted } from './kitrefs';
import { type Ring, ccw, edgeN, pointInRing } from './geo';
import { isStreetClass } from './grammar';
import { kindOf } from './edges';
import { CODE_H, JUNCTION, STREAM_ROWS, STREAM_STACK, type StreamClass, type StreamRow, capBody, cornerPlace, endPlace, recipePick, rowOf } from './stream';

/** The piece names (brief 072; Vela's briefs/072/pieces/pieces.md has the final list). */
export const FACE_KIT = {
  side: { 3: ['SideBay_3'], 4: ['SideBay_4'] } as Record<number, string[]>,
  service: { 3: ['AlleyWall_3', 'AlleyWall_3D'], 4: ['AlleyWall_4', 'AlleyWall_4D'] } as Record<number, string[]>,
  stack: { base: 'SideBayStack_Base', mid: 'SideBayStack_Mid', top: 'SideBayStack_Top' },
  /** A narrow pilaster (Splice's request to Vela, brief 072-C): takes the rest of a run no whole number of 6 m modules at 0.85-1.15 fills (runs under 5.1 m,
   *  6.9-10.2 m, 13.8-15.3 m: tails beside the street bays, short returns), one or two per run at its ends; whole-wall _3 / _4 or per band. */
  pilaster: { whole: { 3: 'SidePilaster_3', 4: 'SidePilaster_4' } as Record<number, string>, base: 'SidePilaster_Base', mid: 'SidePilaster_Mid', top: 'SidePilaster_Top' },
  pier: {
    out: { whole: { 3: 'CornerPier_Out_3', 4: 'CornerPier_Out_4' } as Record<number, string>, base: 'CornerPier_Out_Base', mid: 'CornerPier_Out_Mid', top: 'CornerPier_Out_Top' },
    in: { whole: { 3: 'CornerPier_In_3', 4: 'CornerPier_In_4' } as Record<number, string>, base: 'CornerPier_In_Base', mid: 'CornerPier_In_Mid', top: 'CornerPier_In_Top' },
  },
  landmark: 'RecordsTower',
  /** The shop fillers (Vela, briefs/072/pieces/pieces.md "Pass 2"): a shop run's whole-wall column (ShopFill_n for SideBay_n) and a stacked shop run's ground row. */
  shop: { whole: { 3: 'ShopFill_3', 4: 'ShopFill_4' } as Record<number, string>, base: 'ShopStack_Base' },
};
/** A shop filler piece (linked to the tenancy in front of it as the street bays are). */
export const SHOP_PIECE = /^(ShopFill_[34]|ShopStack_Base)$/;
/** The rule's numbers. */
export const FACES = {
  module: 6, // m: module width on plan (the pieces' 6 m bay)
  minFace: 0.5, // m: a wall span shorter than this is a joint, not a face
  serviceGap: 14, // m: SERVICE (c), two backs facing within this
  shareTol: 0.3, // m: a neighbour's wall within this of a face's line stands against it
  chainTurn: 15, // deg: faces meeting at a smaller turn are one run (a gentle bend or an arc is tiled along)
  pierTurn: 60, // deg: a corner pier at a turn of at least this
  pierMax: 120, // deg: and at most this (the pier pieces are built for a square corner; at a shallower or sharper turn the modules meet)
  hiddenTop: 1.0, // m: a face whose exposed part above a neighbour is shorter than this is shared
  bandMin: 1.0, // m: a storey line within this of a face's foot or top starts no band
  topTol: 0.05, // m: heights that are the same
  hlodCell: 32, // m: Redline's step-2 HLOD cell (briefs/072/redline/plan.md §3.2)
  shopOverlap: 0.5, // m: an own-shopfront tenancy overlapping a side run by more than this makes it a shop run
  shopModule: 3, // m: the shop fillers' own width (k nearest R / 3, never FACES.module)
  shopMin: 1.5, // m: a run shorter than this is too narrow for a shopfront and keeps its side pieces
  acuteTol: 0.5, // deg: a corner within this of square is square (K-6: no pull-back)
  shopTol: 0.6, // m: a run's part lies on a tenancy's line within this (shareTol, widened for a chord across an arc: avenue-court/19 stands 0.45 off)
  /** brief 075-E: the part of a wall standing above a lower neighbour takes the stacked streamline bays (the old street's taller segment wrapped its ribbons
   *  round the side it showed above its neighbour: StreamEnd's 14 m return); false keeps SideBayStack there (a "blank party wall"). Aster's call. */
  streamParty: true,
  capSquare: 0.05, // brief 075-E: |cos| of the turn under which a streamline corner or end wraps a corner (3°: the cell and the 14 m return are square; a slanted face keeps its pier)
};

export type Occ = { owner: string; ring: V[]; base: number; top: number; box: [number, number, number, number] };
export type Cover = { quad: V[]; lo: number; hi: number; owner: string };
export type FaceBld = { id: string; plot: string; area: string; use: string; kind: string; n: number; ring: Ring; tower: boolean; play: boolean; top: number };
export type FaceLog = { face: string; building: string; area: string; play: boolean; family: 'side' | 'service' | 'stack'; length: number; from: number; to: number; modules: number; /** brief 075-E: the streamline bay the run takes */ stream?: string; /** why a public run keeps the side pieces */ streamWhy?: string; why?: string; blocked?: string[]; /** the module ids that dress it (Redline's per-face coverage) */ dressedBy?: string[]; /** a shop run: the shop filler modules on it (ShopFill_n or the ShopStack_Base row) */ shop?: number; /** a shop run given back to its side pieces: too short once pulled back from an acute corner */ shopCut?: string; /** stretches left bare at an acute corner (m): narrower than any piece that fits there */ bare?: number[] };
export type FacesOut = { facades: Facade[]; log: FaceLog[]; piers: { placed: number; skipped: number; why: Record<string, number> }; /** brief 075-E: the streamline corners and ends placed, by piece */ stream: { corners: Record<string, number>; ends: Record<string, number>; runs: number; kept: Record<string, number>; /** outside corners left to a pier, by reason */ missed: Record<string, number> } };

const boxOf = (p: V[]): [number, number, number, number] => [Math.min(...p.map(q => q[0])), Math.max(...p.map(q => q[0])), Math.min(...p.map(q => q[1])), Math.max(...p.map(q => q[1]))];
export const occOf = (owner: string, ring: V[], base: number, top: number): Occ => ({ owner, ring, base, top, box: boxOf(ring) });
const deg = (r: number) => (r * 180) / Math.PI;
const turnDeg = (t0: V, t1: V) => deg(Math.atan2(cross(t0, t1), dot(t0, t1)));

/** Intervals of the wall a→b (metres along it) and the height each is exposed from: z0 where nothing stands against it, a neighbour's top where a lower
 *  neighbour's wall stands against it. Parts a neighbour hides to within FACES.hiddenTop of the wall's top are left out (shared walls). */
export function exposure(a: V, b: V, z0: number, z1: number, occ: Occ[], self: string): { s0: number; s1: number; from: number }[] {
  const L = dist(a, b), u = norm(sub(b, a)), t = FACES.shareTol, hits: { s0: number; s1: number; top: number }[] = [];
  const bx = [Math.min(a[0], b[0]) - t, Math.max(a[0], b[0]) + t, Math.min(a[1], b[1]) - t, Math.max(a[1], b[1]) + t];
  for (const o of occ) {
    if (o.owner === self || o.base > z0 + 0.5 || o.top <= z0 + 0.5) continue;
    if (o.box[1] < bx[0]! || o.box[0] > bx[1]! || o.box[3] < bx[2]! || o.box[2] > bx[3]!) continue;
    for (let j = 0; j < o.ring.length; j++) {
      const p = o.ring[j]!, q = o.ring[(j + 1) % o.ring.length]!;
      if (Math.abs(cross(u, sub(p, a))) > t || Math.abs(cross(u, sub(q, a))) > t) continue;
      const sp = dot(u, sub(p, a)), sq = dot(u, sub(q, a)), lo = Math.max(0, Math.min(sp, sq)), hi = Math.min(L, Math.max(sp, sq));
      if (hi - lo > 0.05) hits.push({ s0: lo, s1: hi, top: o.top });
    }
  }
  const cuts = [...new Set([0, L, ...hits.flatMap(h => [h.s0, h.s1])])].sort((x, y) => x - y), out: { s0: number; s1: number; from: number }[] = [];
  for (let i = 0; i + 1 < cuts.length; i++) {
    const s0 = cuts[i]!, s1 = cuts[i + 1]!; if (s1 - s0 < 0.02) continue;
    const m = (s0 + s1) / 2, top = Math.max(-Infinity, ...hits.filter(h => h.s0 <= m && h.s1 >= m).map(h => h.top));
    const from = top > z0 ? top : z0;
    if (from >= z1 - FACES.hiddenTop) continue;
    const last = out[out.length - 1];
    if (last && Math.abs(last.s1 - s0) < 0.02 && Math.abs(last.from - from) < 0.01) last.s1 = s1; else out.push({ s0, s1, from });
  }
  return out;
}

/** Cyrus–Beck: the part (t0, t1 in 0..1) of segment p + t·d inside convex quad q (either winding), or null. */
export function clip(q: V[], p: V, d: V): [number, number] | null {
  let t0 = 0, t1 = 1;
  const sgn = q.reduce((s, a, i) => s + cross(a, q[(i + 1) % q.length]!), 0) >= 0 ? 1 : -1;
  for (let i = 0; i < q.length; i++) {
    const a = q[i]!, e = sub(q[(i + 1) % q.length]!, a), n: V = [-e[1] * sgn, e[0] * sgn], num = dot(sub(p, a), n), den = dot(d, n);
    if (Math.abs(den) < 1e-9) { if (num < 0) return null; continue; }
    const t = -num / den;
    if (den > 0) t0 = Math.max(t0, t); else t1 = Math.min(t1, t);
    if (t0 >= t1) return null;
  }
  return [t0, t1];
}

/** A placed piece's footprint quad (footprint centre `at`, size x/z at its scale, turned by yaw; kit +X along yaw, +Z = yaw + 90°), grown by `g` m. */
export function quadOf(at: V, size: [number, number, number], scale: [number, number, number], yaw: number, g = 0): V[] {
  const hx = (size[0] * scale[0]) / 2 + g, hz = (size[2] * scale[2]) / 2 + g, kx: V = [Math.cos(yaw), Math.sin(yaw)], kz: V = [-Math.sin(yaw), Math.cos(yaw)];
  return [[-1, -1], [1, -1], [1, 1], [-1, 1]].map(([sx, sz]) => [at[0] + kx[0] * hx * sx! + kz[0] * hz * sz!, at[1] + kx[1] * hx * sx! + kz[1] * hz * sz!] as V);
}

/** Intervals of wall a→b whose height from..z1 the covers stand over (the union of the covers' heights over each part reaches from the foot to the top,
 *  within 0.5 m at the foot and 0.6 m at the top: a parapet line). */
export function coveredSpans(a: V, b: V, from: number, z1: number, covers: Cover[]): [number, number][] {
  const L = dist(a, b), d = sub(b, a), parts: { s0: number; s1: number; lo: number; hi: number }[] = [];
  for (const c of covers) {
    if (c.hi <= from + 0.05 || c.lo >= z1 - 0.05) continue;
    const t = clip(c.quad, a, d); if (t && (t[1] - t[0]) * L > 0.05) parts.push({ s0: t[0] * L, s1: t[1] * L, lo: c.lo, hi: c.hi });
  }
  if (!parts.length) return [];
  const cuts = [...new Set([0, L, ...parts.flatMap(p => [p.s0, p.s1])])].sort((x, y) => x - y), out: [number, number][] = [];
  for (let i = 0; i + 1 < cuts.length; i++) {
    const s0 = cuts[i]!, s1 = cuts[i + 1]!, m = (s0 + s1) / 2;
    const zs = parts.filter(p => p.s0 <= m && p.s1 >= m).map(p => [p.lo, p.hi] as [number, number]).sort((x, y) => x[0] - y[0]);
    let reach = from + 0.5, ok = false;
    for (const [lo, hi] of zs) { if (lo > reach + 0.05) break; reach = Math.max(reach, hi); if (reach >= z1 - 0.6) { ok = true; break; } }
    if (!ok) continue;
    const last = out[out.length - 1];
    if (last && Math.abs(last[1] - s0) < 0.02) last[1] = s1; else out.push([s0, s1]);
  }
  return out;
}

const minus = (s0: number, s1: number, cut: [number, number][]): [number, number][] => {
  let out: [number, number][] = [[s0, s1]];
  for (const [c0, c1] of cut) out = out.flatMap(([x0, x1]) => (c1 <= x0 || c0 >= x1 ? [[x0, x1]] : [...(c0 > x0 ? [[x0, c0]] : []), ...(c1 < x1 ? [[c1, x1]] : [])] as [number, number][]));
  return out;
};

/** Storey bands of a wall from `from` to `to`: the storey lines of standards.buildings between them; role base (from the pavement), top (the building's
 *  crown band to its top), mid (every other). A foot above the pavement (a lower neighbour's top) starts on the storey line under it (within 0.5 m). */
export function bandsOf(ctx: Ctx, from: number, to: number, bTop: number): { z: number; h: number; role: 'base' | 'mid' | 'top' }[] {
  const B = ctx.B, pav = ctx.pav, line = (k: number) => pav + B.plinth + B.groundStorey + (k - 1) * B.upperStorey;
  let start = from;
  if (from > pav + FACES.topTol) { let k = 1; while (line(k + 1) <= from + 0.5) k++; if (line(k) <= from + 0.5 && line(k) > pav) start = line(k); }
  const zs = [start];
  for (let k = 1; line(k) < to - FACES.bandMin; k++) if (line(k) > start + FACES.bandMin) zs.push(line(k));
  zs.push(to);
  return zs.slice(0, -1).map((z, i) => {
    const h = zs[i + 1]! - z, role = z <= pav + FACES.topTol ? 'base' : Math.abs(zs[i + 1]! - bTop) < FACES.topTol && h <= B.roofCrown + 1.2 && h >= 2 ? 'top' : 'mid';
    return { z: r3(z), h: r3(h), role } as const;
  });
}

type Pieces = Map<string, KitAsset>;
type Module = { id: string; z: number; sy: number; a: KitAsset };
const inRange = (a: KitAsset, s: number) => s >= a.tags.scale[0] - 1e-6 && s <= a.tags.scale[1] + 1e-6;

type Names = { base: string; mid: string; top: string };
type Column = { mods: Module[] } | { why: string; blocked: string[] };

/** One column of stack modules from `foot` to `top` (a missing base or top piece falls back to the mid piece):
 *  1. one module per storey band (bandsOf), each at its band's height scale, a band too tall for one module split into equal mids;
 *  2. else (a shed's height, a strip above a neighbour shorter than a storey) one evenly scaled column: base (from the pavement), k mids, top (at the
 *     building's top), all at one height scale; the foot may sink down to `minFoot` behind a lower neighbour (hidden there) to reach a piece's height. */
function column(ctx: Ctx, foot: number, top: number, minFoot: number, bTop: number, P: Pieces, names: Names): Column {
  const role = (r: 'base' | 'mid' | 'top') => P.get(names[r]) ?? P.get(names.mid);
  const missing = (['base', 'mid', 'top'] as const).filter(r => !P.get(names[r]));
  if (!P.get(names.mid)) return { why: `not adopted: ${missing.map(r => names[r]).join(', ')}`, blocked: missing.map(r => names[r]) };
  const mods: Module[] = [];
  let ok = true, why = '';
  for (const b of bandsOf(ctx, foot, top, bTop)) {
    const a = role(b.role)!, m = a.size[1], k = Math.max(1, Math.round(b.h / m)), sy = b.h / (k * m);
    if (inRange(a, sy) && (k === 1 || b.role !== 'top')) { for (let j = 0; j < k; j++) mods.push({ id: a.id, z: r3(b.z + j * sy * m), sy: r3(sy), a }); continue; }
    ok = false; why = `${b.role} band ${b.h} m: ${a.id} (${m} m at ${a.tags.scale.join('-')}) does not fit`; break;
  }
  if (ok && mods.length) return { mods };
  // the even column
  const pav = ctx.pav, base = foot <= pav + FACES.topTol ? role('base')! : null, cap = Math.abs(top - bTop) < FACES.topTol ? role('top')! : null, mid = role('mid')!;
  for (let k = 0; k < 64; k++) {
    const parts = [...(base ? [base] : []), ...Array<KitAsset>(k).fill(mid), ...(cap ? [cap] : [])]; if (!parts.length) continue;
    const N = parts.reduce((s, a) => s + a.size[1], 0), [lo, hi] = parts.reduce(([l, h], a) => [Math.max(l, a.tags.scale[0]), Math.min(h, a.tags.scale[1])], [0, Infinity]);
    if (N * lo > top - minFoot + 1e-6) break; // too tall even sunk to minFoot
    const f = base ? foot : Math.max(minFoot, Math.min(foot, top - N)), sy = (top - f) / N;
    if (sy < lo - 1e-6 || sy > hi + 1e-6) continue;
    let z = f; const out: Module[] = [];
    for (const a of parts) { out.push({ id: a.id, z: r3(z), sy: r3(sy), a }); z += a.size[1] * sy; }
    return { mods: out };
  }
  return { why, blocked: [] };
}

/** Widths along a run of length R: k modules of width w at a width scale within the piece's range, or (when no whole number fits) the same with one or two
 *  pilasters (width pw at the pilaster's range) taking the rest, at the run's ends. Null when nothing fits. Prefers the fewest pilasters, then modules nearest 6 m. */
export function tile(R: number, w: number, range: [number, number], pil?: { w: number; range: [number, number] }): { k: number; sx: number; pil: number[] } | null {
  const [lo, hi] = range, kMin = Math.ceil(R / (w * hi) - 1e-9), kMax = Math.floor(R / (w * lo) + 1e-9);
  if (kMin <= kMax && kMax > 0) { let k = kMin; for (let q = kMin; q <= kMax; q++) if (Math.abs(R / q - FACES.module) < Math.abs(R / k - FACES.module)) k = q; return { k, sx: R / (k * w), pil: [] }; }
  if (!pil) return null;
  const [plo, phi] = [pil.w * pil.range[0], pil.w * pil.range[1]];
  for (const np of [1, 2]) {
    let best: { k: number; sx: number; pil: number[] } | null = null, score = Infinity;
    for (let k = 0; k <= Math.floor(R / (w * lo)) + 1; k++) {
      // the pilasters take what k modules leave, the modules as near scale 1 as the pilasters' range allows
      const rem0 = R - k * w, p = Math.min(phi, Math.max(plo, rem0 / np)), sx = k ? (R - np * p) / (k * w) : 1;
      if (k && (sx < lo - 1e-6 || sx > hi + 1e-6)) continue;
      if (!k && Math.abs(np * p - R) > 1e-6) continue;
      const sc = Math.abs(sx - 1) * 10 + Math.abs(p - pil.w) / pil.w;
      if (sc < score) { score = sc; best = { k, sx, pil: Array(np).fill(p) }; }
    }
    if (best) return best;
  }
  return null;
}

/** Redline's module family (briefs/072/redline/plan.md §6) from the piece and its form. */
const familyOf = (id: string, form: string): Facade['family'] => (form === 'pilaster' ? 'pilaster' : /Stack_Base$/.test(id) ? 'stack-base' : /Stack_Mid$/.test(id) ? 'stack-mid' : /Stack_Top$/.test(id) ? 'stack-top' : form === 'service' ? 'alley' : 'side');

/** The piece's footprint centre from its pivot, in kit axes (x, z). */
const centreOf = (a: KitAsset): [number, number] => a.pivotOffset ?? [0, 0];
/** How far the piece reaches behind its wall plane (kit -Z from the pivot; the catalogue's footprint: size and pivotOffset). */
export const backOf = (a: KitAsset): number => Math.max(0, a.size[2] / 2 - (a.pivotOffset?.[1] ?? 0));
/** cot of the interior angle at a corner of a CCW ring between edge directions t0 → t1, where it is sharper than square (a turn over 90°); else 0. */
export const cotAcute = (t0: V, t1: V): number => { const tn = turnDeg(t0, t1); return tn > 90 + FACES.acuteTol ? Math.tan(((tn - 90) * Math.PI) / 180) : 0; };

/** An own-shopfront street tenancy (a street-level frontage of a plot building not linked to a street piece): host = its building id, a→b its shopfront line. */
export type Tenancy = { host: string; a: V; b: V };
export type FacesIn = { blds: FaceBld[]; masses: Mass[]; occ: Occ[]; facades: Facade[]; catalogue: (id: string) => KitAsset | undefined; tenancies?: Tenancy[] };

/** The shop rule's widths: k fillers of the piece's own 3 m nearest R / 3, at sx = R / (3k) (always within 0.5-1.7 for R ≥ 1.5). */
export const shopTile = (R: number): { k: number; sx: number } => { const k = Math.max(1, Math.round(R / FACES.shopModule)); return { k, sx: R / (k * FACES.shopModule) }; };

/** Metres of the polyline parts (each a→b on one wall line) that own-shopfront tenancies overlap: a part counts where it lies on the tenancy's line (both
 *  its ends within FACES.shopTol of it, parallel within FACES.chainTurn; a tenancy's chord across an arc stands off the arc's segments), by its projection. */
export function shopOverlap(parts: [V, V][], ts: Tenancy[]): number {
  let sum = 0;
  for (const t of ts) {
    const Lt = dist(t.a, t.b); if (Lt < 1e-6) continue;
    const u = norm(sub(t.b, t.a)), off = (p: V) => Math.abs(cross(u, sub(p, t.a))), al = (p: V) => dot(u, sub(p, t.a));
    for (const [a, b] of parts) {
      if (dist(a, b) < 1e-6 || off(a) > FACES.shopTol || off(b) > FACES.shopTol || Math.abs(cross(u, norm(sub(b, a)))) > Math.sin((FACES.chainTurn * Math.PI) / 180)) continue;
      const lo = Math.max(0, Math.min(al(a), al(b))), hi = Math.min(Lt, Math.max(al(a), al(b)));
      if (hi > lo) sum += hi - lo;
    }
  }
  return sum;
}

/** Is the every-face kit adopted (the side bay or its stack)? masses.ts then builds plain tiers for every building: the kit is the wall. */
export function faceKitReady(): boolean {
  const ids = new Set(adopted().map(a => a.id));
  return [...FACE_KIT.side[3]!, ...FACE_KIT.side[4]!, FACE_KIT.stack.mid].some(id => ids.has(id));
}

export function dressFaces(ctx: Ctx, inp: FacesIn): FacesOut {
  const P: Pieces = new Map(adopted().map(a => [a.id, a])), pav = ctx.pav, out: Facade[] = [], log: FaceLog[] = [];
  const piers = { placed: 0, skipped: 0, why: {} as Record<string, number> }, skip = (w: string) => { piers.skipped++; piers.why[w] = (piers.why[w] ?? 0) + 1; };
  const streamOn = faceKitReady();
  const stream = { corners: {} as Record<string, number>, ends: {} as Record<string, number>, runs: 0, kept: {} as Record<string, number>, missed: {} as Record<string, number> }, inc = (o: Record<string, number>, k: string) => { o[k] = (o[k] ?? 0) + 1; };
  const byB = new Map<string, Facade[]>(); for (const f of inp.facades) (byB.get(f.building) ?? byB.set(f.building, []).get(f.building)!).push(f);
  const tenB = new Map<string, Tenancy[]>(); for (const t of inp.tenancies ?? []) (tenB.get(t.host) ?? tenB.set(t.host, []).get(t.host)!).push(t);
  const coversOf = (fs: Facade[]): Cover[] => fs.flatMap(f => { const a = inp.catalogue(f.kit.id); return a ? [{ quad: quadOf(f.kit.at, a.size, f.kit.scale, f.kit.yaw ?? 0, 0.05), lo: f.base, hi: f.base + a.size[1] * f.kit.scale[1], owner: f.building }] : []; });
  const footprints = inp.blds.filter(b => !b.tower).map(b => ({ b, p: b.ring.p }));
  const yawOf = (n: V) => r4(Math.atan2(n[1], n[0]) - Math.PI / 2);

  /** The building-line class of a tier edge: the footprint edge it lies on, else none (an upper tier's inset edge). */
  const clsOf = (b: FaceBld, a: V, z: V): string => {
    const u = norm(sub(z, a)), p = b.ring.p;
    for (let j = 0; j < p.length; j++) { const q0 = p[j]!, q1 = p[(j + 1) % p.length]!; if (Math.abs(cross(u, sub(q0, a))) < 0.2 && Math.abs(cross(u, sub(q1, a))) < 0.2 && dot(norm(sub(q1, q0)), u) > 0.95) return b.ring.c[j]!; }
    return 'none';
  };
  /** SERVICE (c): the nearest plot-building wall along the outward normal within serviceGap is a non-street wall. */
  const backToBack = (b: FaceBld, m: V, n: V): boolean => {
    let best = FACES.serviceGap, cls = '';
    const o: V = [m[0] + n[0] * 0.05, m[1] + n[1] * 0.05];
    for (const { b: x, p } of footprints) {
      if (x.id === b.id) continue;
      for (let j = 0; j < p.length; j++) {
        const q0 = p[j]!, e = sub(p[(j + 1) % p.length]!, q0), den = cross(n, e); if (Math.abs(den) < 1e-9) continue;
        const t = cross(sub(q0, o), e) / den, s = cross(sub(q0, o), n) / den;
        if (t > 0 && t < best && s >= 0 && s <= 1) { best = t; cls = x.ring.c[j]!; }
      }
    }
    return !!cls && !isStreetClass(cls);
  };
  /** SERVICE: the rule that makes a face a service back ('' = a face the public sees). */
  const serviceOf = (b: FaceBld, cls: string, m: V, n: V): '' | 'court' | 'yard' | 'back-to-back' => isStreetClass(cls) ? '' : kindOf(cls) === 'court' ? 'court' : b.use === 'industrial' || b.use === 'port' ? 'yard' : backToBack(b, m, n) ? 'back-to-back' : '';

  for (const b of inp.blds) {
    const tiers = inp.masses.filter(m => m.id.startsWith(b.id + '/') && !/\/band-\d+$/.test(m.id) && m.height > 1);
    const covers = coversOf(byB.get(b.id) ?? []), placed: Facade[] = [];
    // brief 075-E: the building's one streamline class: its street front's bay (facades.ts), else drawn by the old recipe
    const frontRow = rowOf((byB.get(b.id) ?? []).find(f => f.form === 'bay')?.kit.id ?? '');
    const bCls: StreamClass = frontRow?.cls ?? recipePick<StreamClass>(['S', 'P', 'B', 'D', 'A'], c => c, rng(`faces/stream/${b.id}`).f());
    const coping = (code: number) => CODE_H[code] ?? 6.1 + 3.4 * code;
    /** The streamline bay for a whole wall of height h: the front's own bay, else the class's, else a StreamBay; storey-true in the play streets. */
    const streamRow = (h: number): { row: StreamRow; a: KitAsset; sy: number } | null => {
      const fits = (row: StreamRow) => { const a = P.get(row.bay); if (!a) return null; const sy = h / a.size[1]; if (!inRange(a, sy)) return null;
        if (b.play ? row.code !== b.n || Math.abs(a.size[1] - coping(row.code)) > 0.4 : /^PortholeBay_\d+L$/.test(row.bay)) return null; return { row, a, sy }; };
      const best = (rows: StreamRow[], seed: string) => { const ok = rows.map(fits).filter((x): x is { row: StreamRow; a: KitAsset; sy: number } => !!x); if (!ok.length) return null;
        const d = Math.min(...ok.map(o => Math.abs(o.sy - 1))), near = ok.filter(o => Math.abs(o.sy - 1) < d + 0.02); return near[Math.floor(rng(seed).f() * near.length)]!; };
      return (frontRow ? fits(frontRow) : null) ?? best(STREAM_ROWS.filter(r => r.cls === bCls), `faces/stream/${b.id}/${bCls}`) ?? best(STREAM_ROWS.filter(r => r.cls === 'S'), `faces/stream/${b.id}/S`);
    };
    const push = (f: Omit<Facade, 'id' | 'building' | 'storeys'>) => placed.push({ id: `${b.id}/k${placed.length}`, building: b.id, storeys: b.n, ...f });
    for (const m of tiers) {
      const ring = ccw(m.ring), N = ring.length, z0 = m.base, z1 = r3(m.base + m.height);
      const whole = z0 <= pav + FACES.topTol && Math.abs(z1 - b.top) < FACES.topTol; // the tier is the building's whole wall
      // spans: per edge, exposed and not under a piece already
      type Span = { e: number; s0: number; s1: number; from: number; fam: 'side' | 'service'; svc: '' | 'court' | 'yard' | 'back-to-back' };
      const spans: Span[] = [], L = (i: number) => dist(ring[i]!, ring[(i + 1) % N]!), T = (i: number) => norm(sub(ring[(i + 1) % N]!, ring[i]!));
      // the wall edge before / after edge i (an edge shorter than a face is a joint: the corner is read across it)
      const edgeBefore = (i: number) => { let k = (i + N - 1) % N; for (let g = 0; g < 3 && L(k) < FACES.minFace; g++) k = (k + N - 1) % N; return k; };
      const edgeAfter = (i: number) => { let k = (i + 1) % N; for (let g = 0; g < 3 && L(k) < FACES.minFace; g++) k = (k + 1) % N; return k; };
      for (let i = 0; i < N; i++) {
        const a = ring[i]!, z = ring[(i + 1) % N]!; if (L(i) < FACES.minFace) continue;
        const cls = clsOf(b, a, z), n = edgeN(a, z);
        for (const x of exposure(a, z, z0, z1, inp.occ, b.id)) {
          const mid: V = [a[0] + (z[0] - a[0]) * ((x.s0 + x.s1) / 2 / L(i)), a[1] + (z[1] - a[1]) * ((x.s0 + x.s1) / 2 / L(i))];
          const svc = serviceOf(b, cls, mid, n), fam = svc ? 'service' : 'side';
          const pa: V = [a[0] + T(i)[0] * x.s0, a[1] + T(i)[1] * x.s0], pz: V = [a[0] + T(i)[0] * x.s1, a[1] + T(i)[1] * x.s1];
          for (const [c0, c1] of minus(x.s0, x.s1, coveredSpans(pa, pz, x.from, z1, covers).map(([c0, c1]) => [x.s0 + c0, x.s0 + c1] as [number, number])))
            if (c1 - c0 >= FACES.minFace) spans.push({ e: i, s0: c0, s1: c1, from: x.from, fam, svc });
        }
      }
      // runs: spans that meet at a vertex at a small turn, with one foot and one family
      type Run = { parts: Span[]; from: number; fam: 'side' | 'service'; startPier: number; endPier: number; st?: { row: StreamRow; a: KitAsset; sy: number }; sk?: Module[]; stWhy?: string };
      const runs: Run[] = [];
      const joins = (x: Span, y: Span) => Math.abs(x.s1 - L(x.e)) < 0.02 && y.e === (x.e + 1) % N && y.s0 < 0.02 && Math.abs(turnDeg(T(x.e), T(y.e))) < FACES.chainTurn && Math.abs(x.from - y.from) < 0.01 && x.fam === y.fam;
      for (const s of spans) { const r = runs[runs.length - 1], last = r?.parts[r.parts.length - 1]; if (r && last && joins(last, s)) r.parts.push(s); else runs.push({ parts: [s], from: s.from, fam: s.fam, startPier: 0, endPier: 0 }); }
      if (runs.length > 1) { const f = runs[0]!, l = runs[runs.length - 1]!; if (joins(l.parts[l.parts.length - 1]!, f.parts[0]!)) { f.parts.unshift(...l.parts); runs.pop(); } }
      // brief 075-E: each run's geometry (the polyline of its parts, a point and the outward normal s m along it) and its streamline bay
      const geoOf = (r: Run) => {
        const pts: V[] = [], lens: number[] = [], ns: V[] = [];
        for (const s of r.parts) { const a = ring[s.e]!, t = T(s.e); if (!pts.length) pts.push([a[0] + t[0] * s.s0, a[1] + t[1] * s.s0]); pts.push([a[0] + t[0] * s.s1, a[1] + t[1] * s.s1]); lens.push(s.s1 - s.s0); ns.push([t[1], -t[0]]); }
        const total = lens.reduce((x, y) => x + y, 0);
        const seg = (s: number) => { let acc = 0; for (let i = 0; i < lens.length; i++) { if (s <= acc + lens[i]! + 1e-9 || i === lens.length - 1) return { i, k: Math.min(1, Math.max(0, (s - acc) / (lens[i]! || 1))) }; acc += lens[i]!; } return { i: lens.length - 1, k: 1 }; };
        const at = (s: number): V => { const { i, k } = seg(s); return [pts[i]![0] + (pts[i + 1]![0] - pts[i]![0]) * k, pts[i]![1] + (pts[i + 1]![1] - pts[i]![1]) * k]; };
        return { total, at, nAt: (s: number): V => ns[seg(s).i]! };
      };
      /** Does a body `depth` m deep behind the run from s0 to s1 stand inside the tier (sampled every metre, 5 cm in)? */
      const bodyIn = (r: Run, s0: number, s1: number, depth: number): boolean => {
        const g = geoOf(r), k = Math.max(2, Math.ceil((s1 - s0) / 1));
        for (let i = 0; i <= k; i++) { const s = s0 + 0.05 + ((s1 - s0 - 0.1) * i) / k, p = g.at(s), n = g.nAt(s);
          for (const d of [0.3, depth / 2, depth - 0.05]) if (!pointInRing([p[0] - n[0] * d, p[1] - n[1] * d], ring)) return false; }
        return true;
      };
      const tierFoot = (r: Run) => Math.abs(r.from - z0) < 0.01; // not the part of a party wall above a lower neighour (a blank party wall keeps the side pieces)
      const wholeFoot = (r: Run) => whole && tierFoot(r) && z0 <= pav + FACES.topTol;
      const streamy = (r: Run) => !!(r.st || r.sk);
      for (const r of runs) {
        if (r.fam !== 'side' || !streamOn) continue; // the grammar dresses with the every-face pass (faceKitReady), not without it
        if (!tierFoot(r) && !FACES.streamParty) { r.stWhy = 'a party wall above a lower neighbour'; continue; }
        const parts = r.parts.map(s => { const a = ring[s.e]!, t = T(s.e); return [[a[0] + t[0] * s.s0, a[1] + t[1] * s.s0], [a[0] + t[0] * s.s1, a[1] + t[1] * s.s1]] as [V, V]; });
        const shop = tenB.has(b.id) && shopOverlap(parts, tenB.get(b.id)!) > FACES.shopOverlap;
        if (shop && wholeFoot(r) && b.n <= 4) { r.stWhy = 'shop run'; continue; } // the whole-wall shop fillers (ShopFill_n) stay; a taller shop run takes the shop row under streamline storeys
        // at a corner sharper than square the body pulls back d·cot (K-6, as the tiling below); the side pieces take the strip it leaves (a shop run keeps them all)
        const p0 = r.parts[0]!, pz = r.parts[r.parts.length - 1]!, gap0 = p0.s0, gap1 = Math.max(0, L(pz.e) - pz.s1), c0 = cotAcute(T(edgeBefore(p0.e)), T(p0.e)), c1 = cotAcute(T(pz.e), T(edgeAfter(pz.e)));
        if (shop && (c0 > 0 || c1 > 0)) { r.stWhy = 'acute corner (shop run)'; continue; }
        const total = geoOf(r).total, foot = Math.max(r.from, pav);
        const fitsBody = (d: number) => { const q0 = Math.max(0, d * c0 - gap0), q1 = Math.max(0, d * c1 - gap1); return total - q0 - q1 >= FACES.module * 0.9 && bodyIn(r, q0, total - q1, d); };
        if (wholeFoot(r) && !shop) { // the whole-wall streamline bay
          const st = streamRow(z1 - foot);
          if (st && fitsBody(backOf(st.a))) { r.st = st; continue; }
          r.stWhy = st ? `the ${r3(backOf(st.a))} m body leaves the building` : `no whole-wall streamline bay fits ${r3(z1 - foot)} m${b.play ? ` at ${b.n} storeys` : ''}`;
        }
        if (!P.get(STREAM_STACK.bay.mid)) continue; // Vela's stacked streamline bays (stream.ts STREAM_STACK), once adopted
        const c = column(ctx, foot, z1, tierFoot(r) ? foot : Math.max(z0, pav), b.top, P, STREAM_STACK.bay);
        if (!('mods' in c)) { r.stWhy = `streamline stack: ${c.why}`; continue; }
        if (!fitsBody(Math.max(...c.mods.map(x => backOf(x.a))))) { r.stWhy = 'the stack body leaves the building'; continue; }
        r.sk = c.mods; r.stWhy = undefined;
      }
      // the corner and end stacks are cut at the plan's bands: a Top module taller than its band (StreamCornerStack_Top's fin) stands on the Top band's height
      const PB: Pieces = new Map(P);
      for (const nm of [STREAM_STACK.corner.top, STREAM_STACK.end.top]) { const a = P.get(nm); if (a && a.size[1] > 3.95 + 0.05) PB.set(nm, { ...a, size: [a.size[0], 3.95, a.size[2]] }); }
      // corner piers: where one run ends at a vertex and the next starts there at a turn of pierTurn or more, both from the tier's foot
      const vertexOf = (r: Run, end: boolean) => { const s = end ? r.parts[r.parts.length - 1]! : r.parts[0]!; return end ? (Math.abs(s.s1 - L(s.e)) < 0.02 ? (s.e + 1) % N : -1) : s.s0 < 0.02 ? s.e : -1; };
      for (const r of runs) {
        const v = vertexOf(r, true); if (v < 0) continue;
        const nx = runs.find(o => vertexOf(o, false) === v); if (!nx) continue;
        const e0 = (v + N - 1) % N, tn = turnDeg(T(e0), T(v)); if (Math.abs(tn) < FACES.pierTurn) continue;
        if (Math.abs(tn) > FACES.pierMax) { skip('turn over pierMax: the modules meet'); continue; }
        const kind = tn > 0 ? 'out' : 'in', names = FACE_KIT.pier[kind], vtx = ring[v]!, n0 = edgeN(ring[e0]!, vtx), n1 = edgeN(vtx, ring[(v + 1) % N]!), bis = norm([n0[0] + n1[0], n0[1] + n1[1]]);
        // brief 075-E: a streamline corner or end in place of the pier
        if (kind === 'out' && Math.abs(r.from - nx.from) < 0.01 && (streamy(r) || streamy(nx)) && Math.abs(dot(T(e0), T(v))) < FACES.capSquare) { // both faces from one foot (a party strip's corner too)
          const foot = Math.max(r.from, pav), h = z1 - foot, face = `${m.id}#v${v}`, gr = geoOf(r).total, gn = geoOf(nx).total;
          const fitCap = (id: string, sc?: number) => { const a = P.get(id); if (!a) return null; const s = sc ?? h / capBody(a); return inRange(a, s) ? [{ a, z: foot, sy: s, s }] : null; };
          const stackCap = (names: Names) => { if (!P.get(names.mid)) return null; const c = column(ctx, foot, z1, tierFoot(r) ? foot : Math.max(z0, pav), b.top, PB, names); if (!('mods' in c)) return null; const s = Math.max(1, ...c.mods.map(md => md.sy)); return c.mods.map(md => ({ a: P.get(md.id)!, z: md.z, sy: md.sy, s })); }; // one plan scale for the column, never narrower than tall (squash 1)
          type Cap = { a: KitAsset; z: number; sy: number; s: number }[];
          const capPush = (form: 'corner' | 'end', cap: Cap, place: (a: KitAsset, s: number) => { at: V; yaw: number }, pa: V, pb: V, edge: number, normal: V) => {
            for (const md of cap) { const q = place(md.a, md.s);
              push({ form, kit: { id: md.a.id, scale: [r3(md.s), r3(md.sy), r3(md.s)], yaw: q.yaw, at: [r3(q.at[0]), r3(q.at[1])] }, base: r3(md.z), a: [r3(pa[0]), r3(pa[1])], b: [r3(pb[0]), r3(pb[1])], edge, face,
                family: form === 'corner' ? 'corner-out' : 'side', normal: [r4(normal[0]), r4(normal[1])], column: { id: face, storeys: cap.length }, casts: 'proxy-l2' }); }
          };
          let capped = false;
          if (streamy(r) && streamy(nx)) { // the rounded corner: the junction pieces, the classes' whole-wall corners, else the corner stack
            const junction = isStreetClass(clsOf(b, ring[e0]!, vtx)) && isStreetClass(clsOf(b, vtx, ring[(v + 1) % N]!)), street0 = z0 <= pav + FACES.topTol;
            const tries: (Cap | null)[] = [...(junction && street0 && wholeFoot(r) && wholeFoot(nx) && !b.play && b.n >= JUNCTION.towerMin ? [fitCap(JUNCTION.tower, 1)] : []), ...(junction && wholeFoot(r) && wholeFoot(nx) && (!b.play || b.n === 10) ? [fitCap(JUNCTION.deco)] : []),
              ...(r.st && nx.st ? [r.st.row, nx.st.row].filter(row => !b.play || row.code === b.n).map(row => fitCap(row.corner)) : []), stackCap(STREAM_STACK.corner)];
            for (const cap of tries) {
              if (!cap?.length) continue;
              const cell = cornerPlace(vtx, n0, n1, cap[0]!.a, cap[0]!.s).onFace;
              if (gr < cell - 0.02 || gn < cell - 0.02) continue;
              let ok = true; // the cell behind both faces inside the tier
              for (let i = 0; i <= 3 && ok; i++) for (let j = 0; j <= 3 && ok; j++) { const x = 0.05 + ((cell - 0.1) * i) / 3, y = 0.05 + ((cell - 0.1) * j) / 3; ok = pointInRing([vtx[0] - n0[0] * x - n1[0] * y, vtx[1] - n0[1] * x - n1[1] * y], ring); }
              if (!ok) continue;
              capPush('corner', cap, (a, s) => cornerPlace(vtx, n0, n1, a, s), [vtx[0] - T(e0)[0] * cell, vtx[1] - T(e0)[1] * cell], vtx, e0, bis);
              r.endPier = cell; nx.startPier = cell; inc(stream.corners, cap[0]!.a.id); capped = true; break;
            }
          }
          if (!capped) { // the rounded end: its front on the leaving face, its return on the entering one (stream.ts KIT_X: no mirror)
            const row = (nx.st ?? r.st)?.row, tries: (Cap | null)[] = [...(row && wholeFoot(r) && wholeFoot(nx) && (!b.play || row.code === b.n) ? [fitCap(row.end)] : []), stackCap(STREAM_STACK.end)];
            for (const cap of tries) {
              if (!cap?.length) continue;
              const c0 = cap[0]!, ep = endPlace(vtx, T(v), n1, c0.a, c0.s), depth = backOf(c0.a) * c0.s;
              let ok = gn >= ep.front - 0.02 && gr >= depth - 0.02;
              for (let i = 0; i <= 3 && ok; i++) for (let j = 0; j <= 5 && ok; j++) { const u = 0.05 + ((ep.front - 0.1) * i) / 3, z = 0.05 + ((depth - 0.1) * j) / 5; ok = pointInRing([vtx[0] + T(v)[0] * u - n1[0] * z, vtx[1] + T(v)[1] * u - n1[1] * z], ring); }
              if (!ok) continue;
              capPush('end', cap, (a, s) => endPlace(vtx, T(v), n1, a, s), vtx, [vtx[0] + T(v)[0] * ep.front, vtx[1] + T(v)[1] * ep.front], v, n1);
              r.endPier = depth; nx.startPier = ep.front; inc(stream.ends, c0.a.id); capped = true; break;
            }
          }
          if (capped) continue;
          inc(stream.missed, streamy(r) && streamy(nx) ? 'no corner fits (room, cell or height)' : 'no end fits (room or height)');
        } else if (kind === 'out') inc(stream.missed, Math.abs(r.from - nx.from) >= 0.01 ? 'the faces stand from different feet (a lower neighbour against one)' : !(streamy(r) || streamy(nx)) ? 'neither face streamline' : 'not square');
        let mods: Module[] | null = null;
        const from = Math.max(r.from, nx.from), foot = Math.max(from, pav), full = whole && Math.abs(from - z0) < 0.01; // a corner beside a party wall stands from the higher foot
        if (full) { for (const code of b.kind === 'shed' ? [3, 4] : b.n <= 4 ? [b.n, 3, 4] : []) { const a = P.get(names.whole[code] ?? ''); if (a && inRange(a, (z1 - foot) / a.size[1])) { mods = [{ id: a.id, z: r3(foot), sy: r3((z1 - foot) / a.size[1]), a }]; break; } } }
        if (!mods) { const c = column(ctx, foot, z1, Math.max(z0, pav), b.top, P, names); if ('mods' in c) mods = c.mods; else { skip(c.why); continue; } }
        const yaw = yawOf(n0), kx: V = [Math.cos(yaw), Math.sin(yaw)], kz: V = [-Math.sin(yaw), Math.cos(yaw)];
        let rIn = 0, rOut = 0; // how far the pier's footprint runs along the face that ends at the corner and the one that leaves it: the modules stop there
        for (const md of mods) {
          const s = full && mods.length === 1 ? md.sy : 1, [cx, cz] = centreOf(md.a), at: V = [vtx[0] + kx[0] * cx * s + kz[0] * cz * s, vtx[1] + kx[1] * cx * s + kz[1] * cz * s];
          const q = quadOf(at, md.a.size, [s, md.sy, s], yaw), back = clip(q, vtx, [-T(e0)[0] * L(e0), -T(e0)[1] * L(e0)]), on = clip(q, vtx, [T(v)[0] * L(v), T(v)[1] * L(v)]);
          if (back) rIn = Math.max(rIn, back[1] * L(e0)); if (on) rOut = Math.max(rOut, on[1] * L(v));
          push({ form: 'pier', kit: { id: md.id, scale: [r3(s), md.sy, r3(s)], yaw, at: [r3(at[0]), r3(at[1])] }, base: md.z, a: [r3(vtx[0]), r3(vtx[1])], b: [r3(vtx[0]), r3(vtx[1])], edge: v, face: `${m.id}#v${v}`,
            family: kind === 'out' ? 'corner-out' : 'corner-in', normal: [r4(bis[0]), r4(bis[1])], column: { id: `${m.id}#v${v}`, storeys: mods.length }, casts: 'proxy-l2' });
        }
        r.endPier = r.from >= from - 0.01 ? rIn : 0; nx.startPier = nx.from >= from - 0.01 ? rOut : 0; piers.placed++; // the lower face of a corner beside a party wall runs on to the corner under the pier
      }
      // tile each run (a second run on the same edge, split by street pieces between, takes the face id `#e.1`, so faces and columns stay unique)
      const onEdge = new Map<number, number>();
      for (const r of runs) {
        const pts: V[] = [], lens: number[] = [];
        for (const s of r.parts) { const a = ring[s.e]!, t = T(s.e); if (!pts.length) pts.push([a[0] + t[0] * s.s0, a[1] + t[1] * s.s0]); pts.push([a[0] + t[0] * s.s1, a[1] + t[1] * s.s1]); lens.push(s.s1 - s.s0); }
        const total = lens.reduce((x, y) => x + y, 0), s0 = Math.min(r.startPier, total / 2), s1 = Math.max(s0, total - r.endPier), R = s1 - s0;
        const at = (s: number): V => { let acc = 0; for (let i = 0; i < lens.length; i++) { if (s <= acc + lens[i]! + 1e-9 || i === lens.length - 1) { const k = Math.min(1, Math.max(0, (s - acc) / (lens[i]! || 1))); return [pts[i]![0] + (pts[i + 1]![0] - pts[i]![0]) * k, pts[i]![1] + (pts[i + 1]![1] - pts[i]![1]) * k]; } acc += lens[i]!; } return pts[pts.length - 1]!; };
        const e0 = r.parts[0]!.e, nth = onEdge.get(e0) ?? 0; onEdge.set(e0, nth + 1);
        const face = `${m.id}#${e0}${nth ? `.${nth}` : ''}`, ent: FaceLog = { face, building: b.id, area: b.area, play: b.play, family: r.fam, length: r3(total), from: r3(r.from), to: z1, modules: 0 };
        log.push(ent);
        if (R < FACES.minFace) continue; // the corner piers cover it
        // the corners at the run's ends: cot of the interior angle where it is sharper than square (K-6 below), else 0, and how far short of it the run stops
        const p0 = r.parts[0]!, pz = r.parts[r.parts.length - 1]!, gap0 = p0.s0, gap1 = Math.max(0, L(pz.e) - pz.s1);
        const cot0 = cotAcute(T(edgeBefore(p0.e)), T(p0.e)), cot1 = cotAcute(T(pz.e), T(edgeAfter(pz.e)));
        // the column: a whole-wall piece (3 or 4 storeys from the street), else the stack's bands
        let col: Module[] | null = null, variants: KitAsset[] = [], why = '', blocked: string[] = [], stacked = false, code = 0;
        const foot = Math.max(r.from, pav), minFoot = r.from > z0 + 0.01 ? Math.max(z0, pav) : foot, full = whole && Math.abs(r.from - z0) < 0.01;
        if (full) {
          const codes = b.kind === 'shed' ? [3, 4] : b.n <= 4 ? [b.n, 3, 4].filter((x, i, a) => a.indexOf(x) === i) : [];
          for (const c of codes) {
            const ids = (r.fam === 'service' ? FACE_KIT.service : FACE_KIT.side)[c] ?? []; if (!ids.length) continue;
            const a = P.get(ids[0]!);
            if (!a) { blocked.push(ids[0]!); continue; }
            if (inRange(a, (z1 - foot) / a.size[1])) { code = c; col = [{ id: a.id, z: r3(foot), sy: r3((z1 - foot) / a.size[1]), a }]; variants = ids.slice(1).map(id => P.get(id)).filter((x): x is KitAsset => !!x && Math.abs(x.size[1] - a.size[1]) < 0.05); break; }
          }
        }
        if (!col) {
          const c = column(ctx, foot, z1, minFoot, b.top, P, FACE_KIT.stack);
          if ('mods' in c) { col = c.mods; stacked = true; } else { blocked = [...new Set([...blocked, ...c.blocked])]; why = blocked.length ? `not adopted: ${blocked.join(', ')}` : c.why; }
        }
        if (!col) { ent.why = why; if (blocked.length) ent.blocked = blocked; continue; }
        // brief 075-E: a streamline run takes its bay as the whole-wall column, or the stacked streamline bays (no dock variants; the pilasters of the wall take what
        // no whole number fills, the plain column the strip an acute corner leaves); `plain` is the side column it replaces
        const plain = col, plainCode = code;
        if (r.st) { col = [{ id: r.st.a.id, z: r3(foot), sy: r3(r.st.sy), a: r.st.a }]; variants = []; stacked = false; ent.stream = r.st.a.id; stream.runs++; }
        else if (r.sk) { col = r.sk; stacked = true; variants = []; code = 0; ent.stream = r.sk[0]!.id; stream.runs++; }
        else if (r.fam === 'side') { ent.streamWhy = r.stWhy ?? 'no streamline bay'; inc(stream.kept, (r.stWhy ?? 'none').replace(/[\d.]+ m/g, 'n m').replace(/ at \d+ storeys/, '')); }
        ent.family = stacked ? 'stack' : r.fam;
        // the shop rule (pieces.md Pass 2): a side run an own-shopfront tenancy overlaps by more than shopOverlap is a shop run; a whole-wall column takes
        // ShopFill_n for SideBay_n, a stacked column from the pavement its ground row as a ShopStack_Base row (the base-band modules dropped)
        let shopCol: Module | null = null, shopRow: Module | null = null;
        if (r.fam === 'side' && R >= FACES.shopMin && tenB.has(b.id)) {
          const parts = r.parts.map(s => { const a = ring[s.e]!, t = T(s.e); return [[a[0] + t[0] * s.s0, a[1] + t[1] * s.s0], [a[0] + t[0] * s.s1, a[1] + t[1] * s.s1]] as [V, V]; });
          if (shopOverlap(parts, tenB.get(b.id)!) > FACES.shopOverlap) {
            const c0 = col[0]!;
            if (!stacked && code) { const a = P.get(FACE_KIT.shop.whole[code] ?? ''); if (a && Math.abs(a.size[1] - c0.a.size[1]) < 0.05 && inRange(a, c0.sy)) shopCol = { ...c0, id: a.id, a }; }
            else if (stacked && (c0.id === FACE_KIT.stack.base || c0.id === STREAM_STACK.bay.base) && c0.z <= pav + FACES.topTol) { const a = P.get(FACE_KIT.shop.base); if (a && Math.abs(a.size[1] - c0.a.size[1]) < 0.05 && inRange(a, c0.sy)) shopRow = { ...c0, id: a.id, a }; }
          }
        }
        // the pilaster column of the same wall: takes what no whole number of modules fills (a stretch's ends) and a shop run's ends at an acute corner
        const PL = FACE_KIT.pilaster, pw = code ? P.get(PL.whole[code]!) : undefined;
        let pcol: Module[] | null = null;
        if (pw && inRange(pw, (z1 - foot) / pw.size[1])) pcol = [{ id: pw.id, z: r3(foot), sy: r3((z1 - foot) / pw.size[1]), a: pw }];
        else { const c = column(ctx, col[0]!.z, z1, col[0]!.z, b.top, P, PL); if ('mods' in c) pcol = c.mods; }
        // K-6, footprint containment (brief 072): where the run ends at a corner sharper than square, the neighbouring wall cuts in behind it; a piece that
        // reaches D m behind its wall stops D·cot(interior angle) short of the corner, so no part of it stands outside the building. The side pieces (0.4 m)
        // leave a sliver; a shop filler's 2.15 m recess pulls the shop back further, and the side pieces (pilasters) take the corner again.
        const sideCol = col, deep = (ms: Module[] | null) => Math.max(0, ...(ms ?? []).map(m => backOf(m.a)));
        const fit = (d: number): [number, number] => [Math.max(s0, d * cot0 - gap0), Math.min(s1, total - Math.max(0, d * cot1 - gap1))];
        const [a0, a1] = fit(Math.max(deep(sideCol), deep(pcol)));
        let [h0, h1] = [a0, a1];
        if (shopCol || shopRow) {
          [h0, h1] = fit(backOf((shopCol ?? shopRow)!.a));
          // an end narrower than the narrowest pilaster could not be dressed: the shop pulls back to the pilaster's width
          const pmin = pcol ? pcol[0]!.a.size[0] * pcol[0]!.a.tags.scale[0] + 1e-4 : 0;
          if (pmin && h0 - a0 > 0.01 && h0 - a0 < pmin) h0 = a0 + pmin;
          if (pmin && a1 - h1 > 0.01 && a1 - h1 < pmin) h1 = a1 - pmin;
          if (h1 - h0 < FACES.shopMin) { shopCol = shopRow = null; [h0, h1] = [a0, a1]; ent.shopCut = 'acute corner'; } }
        if (shopCol) col = [shopCol];
        // the shop row: k fillers of 3 m nearest R / 3 along the run, each its own column (the Mid / Top columns above keep their 6 m joints)
        if (shopRow) {
          const st = shopTile(h1 - h0), sw = FACES.shopModule * st.sx, [cx, cz] = centreOf(shopRow.a);
          for (let i = 0; i < st.k; i++) {
            const pa = at(h0 + i * sw), pb = at(h0 + (i + 1) * sw), t = norm(sub(pb, pa)), n: V = [t[1], -t[0]], c: V = [(pa[0] + pb[0]) / 2, (pa[1] + pb[1]) / 2], sx = r3(st.sx);
            const p: V = [c[0] - t[0] * cx * sx + n[0] * cz, c[1] - t[1] * cx * sx + n[1] * cz];
            push({ form: 'stack', kit: { id: shopRow.id, scale: [sx, shopRow.sy, 1], yaw: yawOf(n), at: [r3(p[0]), r3(p[1])] }, base: shopRow.z, a: [r3(pa[0]), r3(pa[1])], b: [r3(pb[0]), r3(pb[1])], edge: r.parts[0]!.e, face,
              family: familyOf(shopRow.id, 'stack'), normal: [r4(n[0]), r4(n[1])], column: { id: `${face}/s${i}`, storeys: 1 }, casts: 'proxy-l2' });
            (ent.dressedBy ??= []).push(placed[placed.length - 1]!.id);
            ent.modules++;
          }
          ent.shop = st.k;
        }
        const dropBase = (cl: Module[]) => (shopRow && cl[0] && cl[0].z <= pav + FACES.topTol && (cl[0].id === FACE_KIT.stack.base || cl[0].id === STREAM_STACK.bay.base || cl[0].id === FACE_KIT.pilaster.base) ? cl.slice(1) : cl);
        const svc = r.parts.find(x => x.svc)?.svc;
        let q = 0;
        /** Tile from..to with column cl (its first module's width and range; a whole number of modules, else with pilasters pc at the ends; `fixed` = the
         *  shop fillers' own tiling) and place each width's column as `pick` gives it. False when nothing fits. */
        const lay = (from: number, to: number, cl: Module[], pc: Module[] | null, pick: (c: Module[]) => Module[], fixed?: { k: number; sx: number }, dockable = false): boolean => {
          const len = to - from, w = cl[0]!.a.size[0], range: [number, number] = [Math.max(cl[0]!.a.tags.scale[0], Math.max(...cl.map(m => m.sy)) * cl[0]!.a.tags.squash), cl[0]!.a.tags.scale[1]]; // a module no narrower than its squash share of its height scale (wave-2's rule)
          let tl = fixed ? { ...fixed, pil: [] as number[] } : tile(len, w, range);
          if (!tl && pc) tl = tile(len, w, range, { w: pc[0]!.a.size[0], range: pc[0]!.a.tags.scale });
          if (!tl) return false;
          // the widths in order: a pilaster at each end (two) or at the head (one), the modules between
          const widths: { w: number; pil: boolean }[] = [...(tl.pil.length === 2 ? [{ w: tl.pil[0]!, pil: true }] : []), ...Array.from({ length: tl.k }, () => ({ w: w * tl!.sx, pil: false })), ...(tl.pil.length ? [{ w: tl.pil[tl.pil.length - 1]!, pil: true }] : [])];
          const dock = dockable && variants.length && tl.k >= 2 ? Math.floor(rng(`${face}/dock`).f() * tl.k) : -1;
          let sAt = from, j = 0;
          for (const seg of widths) {
            const pa = at(sAt), pb = at(sAt + seg.w), t = norm(sub(pb, pa)), n: V = [t[1], -t[0]], c0: V = [(pa[0] + pb[0]) / 2, (pa[1] + pb[1]) / 2], yaw = yawOf(n);
            // a module whose chord spans a concave bend of the run (an inward arc) steps back by the bend's depth behind the chord, so no part of it stands in the
            // air between the chord and the wall (K-6; brief 075-E: the street pass's ends moved to the runs' tails and laid new joints along avenue-court/19's arc)
            let sag = 0, acc = 0;
            for (let i = 0; i + 1 < lens.length; i++) { acc += lens[i]!; if (acc > sAt + 0.02 && acc < sAt + seg.w - 0.02) sag = Math.max(sag, -dot(sub(pts[i + 1]!, pa), n)); }
            const c: V = sag > 0.005 ? [c0[0] - n[0] * sag, c0[1] - n[1] * sag] : c0;
            const cs = pick(seg.pil ? pc! : cl), sx = cs.length ? r3(seg.w / cs[0]!.a.size[0]) : 1;
            for (const md0 of cs) {
              const md = !seg.pil && j === dock && cs.length === 1 ? { ...md0, id: variants[0]!.id, a: variants[0]! } : md0, [cx, cz] = centreOf(md.a);
              const p: V = [c[0] - t[0] * cx * sx + n[0] * cz, c[1] - t[1] * cx * sx + n[1] * cz]; // kit +X runs along -t (against the ring), +Z out along n
              const form = seg.pil ? 'pilaster' : ent.family === 'stack' ? 'stack' : r.fam;
              push({ form, kit: { id: md.id, scale: [sx, md.sy, 1], yaw, at: [r3(p[0]), r3(p[1])] }, base: md.z, a: [r3(pa[0]), r3(pa[1])], b: [r3(pb[0]), r3(pb[1])], edge: r.parts[0]!.e, face,
                family: familyOf(md.id, form), ...(r.fam === 'service' && svc ? { service: svc } : {}), normal: [r4(n[0]), r4(n[1])], column: { id: `${face}/c${q}`, storeys: cs.length }, casts: 'proxy-l2' });
              (ent.dressedBy ??= []).push(placed[placed.length - 1]!.id);
            }
            if (!seg.pil) j++;
            ent.modules++; sAt += seg.w; q++;
          }
          return true;
        };
        /** A shop run's end at an acute corner, between where the shop fillers stop and where the side pieces may reach: the side column's pieces (a shop row's
         *  ground band only: the storeys above run on over it), left bare when narrower than a pilaster. */
        const ends = (from: number, to: number) => {
          if (to - from < 1e-3) return;
          const base = shopRow ? [sideCol[0]!] : sideCol, pb = pcol ? (shopRow ? [pcol[0]!] : pcol) : null;
          if (!lay(from, to, base, pb, x => x)) (ent.bare ??= []).push(r3(to - from));
        };
        if (shopCol) {
          ends(a0, h0);
          lay(h0, h1, col, null, x => x, shopTile(h1 - h0)); ent.shop = shopTile(h1 - h0).k;
          ends(h1, a1);
          continue;
        }
        if (shopRow) { ends(a0, h0); ends(h1, a1); if (dropBase(col).length === 0) continue; } // the run was one base band: the shop row is all of it
        // the plain column's pilasters (as before brief 075-E): a streamline run's acute-corner strips and a run no whole number of streamline bays tiles
        const ppw = plainCode ? P.get(PL.whole[plainCode]!) : undefined;
        let ppc: Module[] | null = null;
        if (r.st || r.sk) { if (ppw && inRange(ppw, (z1 - foot) / ppw.size[1])) ppc = [{ id: ppw.id, z: r3(foot), sy: r3((z1 - foot) / ppw.size[1]), a: ppw }]; else { const c = column(ctx, plain[0]!.z, z1, plain[0]!.z, b.top, P, PL); if ('mods' in c) ppc = c.mods; } }
        if (r.st || r.sk) { // the strips an acute corner leaves the streamline body: the plain column (and its pilasters)
          const [p0, p1] = fit(Math.max(deep(plain), deep(ppc)));
          const strip = (from: number, to: number) => { if (to - from < 1e-3) return; if (to - from < FACES.minFace || !lay(from, to, plain, ppc, x => x)) (ent.bare ??= []).push(r3(to - from)); };
          if (a0 > p0 + 1e-3) strip(p0, a0);
          if (a1 < p1 - 1e-3) strip(a1, p1);
        }
        if (a1 - a0 < FACES.minFace) { (ent.bare ??= []).push(r3(a1 - a0)); continue; }
        const laid = lay(a0, a1, col, pcol, dropBase, undefined, true) || ((r.st || r.sk) && lay(a0, a1, plain, ppc, dropBase) && (delete ent.stream, ent.streamWhy = 'no whole number of streamline bays fits the run', stream.runs--, true));
        if (!laid) {
          const len = r3(a1 - a0), w = col[0]!.a.size[0], range = col[0]!.a.tags.scale;
          ent.why = `face ${len} m: no whole number of ${w} m modules at ${range.join('-')}` + (pcol ? ' even with pilasters' : `; a pilaster would fill it (not adopted: ${code ? PL.whole[code] : PL.mid})`); if (!pcol) ent.blocked = [code ? PL.whole[code]! : PL.mid];
        }
      }
    }
    out.push(...placed);
  }
  return { facades: out, log, piers, stream };
}

/** Redline's LOD, combine and instancing fields (briefs/072/redline/plan.md §6) on every facade piece: the LOD set (the piece's own L0..L2 chain, the
 *  catalogue's switch category and, for the every-face pieces, its distance class: kit.json lodClass / lodClasses, tools/kit/lod.ts), the combine groups (the block = the plot or tower
 *  site; the 32 m HLOD cell [floor(x / 32), floor(y / 32)]; the column, set by the pass), the colourway (the building's wall family,
 *  brief 074; `base` where it has none) and the instancing key (piece + colourway: transforms vary). */
export function dressFields(f: Facade, a: KitAsset | undefined, plotOf: (building: string) => string): Facade {
  const c = FACES.hlodCell, colourway = f.colourway ?? 'base', cls = a?.lodClass;
  return { ...f, lod: { set: f.kit.id, levels: 1 + (a?.lods?.length ?? 0), category: a?.lodCategory ?? 'facades', ...(cls ? { class: cls } : {}) }, combine: { block: plotOf(f.building), cell: [Math.floor(f.kit.at[0] / c), Math.floor(f.kit.at[1] / c)] }, colourway, instance: `${f.kit.id}|${colourway}` };
}
