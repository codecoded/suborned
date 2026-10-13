// Facade wave 2 (brief 007 B): kit bays, ends and corners on the street faces of generated buildings, by storeys, use and corner.
// A building takes ONE bay piece for its whole frontage (chosen from the adopted bays that fit H(n), seeded by id), with the matching
// corner / end pieces of its storey code. Adjacent buildings never carry the same bay piece (neighbour contrast); if no contrasting piece
// fits, the building keeps its procedural facade. A piece is placed only where its body stands inside the building ring.
// Frame (tools/decokit, facades.py): the bay is 6 m wide (x), its body 14 m deep behind the facade plane and a canopy (size z − 14) in front;
// the piece's front (+Z) faces yaw + 90°, so +X runs clockwise along the outside of a CCW ring (against the ring's direction).
// A corner (8 × 8 m cell, quarter round) takes the head of the edge entering a convex 90° vertex and the tail of the one leaving it;
// an end (6 m front, return on −X) closes the head of an edge whose next face is a party wall or court.
import type { Facade } from '../../../contracts/world/world.schema';
import type { KitAsset } from '../../../contracts/world/kit.schema';
import { type V, cross, dist, dot, norm, sub } from '../../plan/geom';
import { type Ctx, r3, r4, rng } from './common';
import { adopted } from './kitrefs';
import { type Ring, edgeN, pointInRing } from './geo';
import { isStreetClass } from './grammar';

/** What the generator knows of a building before its masses are built. */
export type FacadeBld = { id: string; use: string; n: number; ring: Ring };
export type FacadeOut = { facades: Facade[]; kitFaced: Set<string>; metres: Map<string, { street: number; kit: number }>; why: Map<string, string> };

const BODY = 14, BAY = 6, CELL = 8; // m: body depth, bay width, corner cell (kit frame)
const TOL = 0.08, MIN_SY = 0.9, CORNER_SPARE = 0.3; // CORNER_SPARE: a face takes a corner cell when it is at least the cell plus this long (the 9 m faces cornerKeep leaves at a corner) // height tolerance against H(n); the pieces' own tags.scale range is read from the kit
/** The bay height each storey code was built to (the end and corner pieces stand to their code's bay height, their masts and piers aside). */
const CODE: Record<string, number> = { '2': 12.9, '3': 16.3, '4': 19.7, '6': 26.5, '9': 36.7, '10': 43.6 };

type Piece = { a: KitAsset; form: 'bay' | 'end' | 'corner'; code: string; body: number; canopy: number };
const codeOf = (id: string): string => (/^(Stream|Deco)(Bay|End|Corner)_(\d+)/.exec(id)?.[3]) ?? '';

/** The adopted facade pieces, grouped: bays (any family), ends and corners by storey code. */
function catalogue(): { bays: Piece[]; ends: Map<string, Piece[]>; corners: Map<string, Piece[]> } {
  const bays: Piece[] = [], ends = new Map<string, Piece[]>(), corners = new Map<string, Piece[]>();
  for (const a of adopted()) {
    const form = a.tags.form; if (!form || !(a.tags.use.includes('facade') || a.tags.use.includes('skyline'))) continue;
    if (a.id.startsWith('CornerTower')) continue; // 67 m junction landmark: it stands at no plot building (see report)
    const bay = /Bay_/.test(a.id), code = bay && !/^(Stream|Deco)Bay_/.test(a.id) ? /_(\d+)/.exec(a.id)![1]! : codeOf(a.id); // Porthole/Balcony/Arcade bays: their own height decides
    const piece: Piece = { a, form, code, body: form === 'bay' ? a.size[1] : CODE[codeOf(a.id)] ?? a.size[1], canopy: form === 'corner' ? a.size[0] - CELL : a.size[2] - BODY };
    if (form === 'bay') bays.push(piece);
    else { const m = form === 'end' ? ends : corners, k = codeOf(a.id); if (k) (m.get(k) ?? m.set(k, []).get(k)!).push(piece); }
  }
  return { bays, ends, corners };
}

/** Buildings share a wall: a party edge of one lies on an edge of the other for more than 2 m. */
export function adjacent(a: Ring, b: Ring): boolean {
  for (let i = 0; i < a.p.length; i++) {
    if (isStreetClass(a.c[i]!)) continue;
    const a0 = a.p[i]!, a1 = a.p[(i + 1) % a.p.length]!, u = norm(sub(a1, a0)), L = dist(a0, a1);
    for (let j = 0; j < b.p.length; j++) {
      const b0 = b.p[j]!, b1 = b.p[(j + 1) % b.p.length]!;
      if (Math.abs(cross(u, sub(b0, a0))) > 0.15 || Math.abs(cross(u, sub(b1, a0))) > 0.15) continue;
      const s0 = dot(u, sub(b0, a0)), s1 = dot(u, sub(b1, a0)), lo = Math.max(0, Math.min(s0, s1)), hi = Math.min(L, Math.max(s0, s1));
      if (hi - lo > 2) return true;
    }
  }
  return false;
}

const turn = (t0: V, t1: V) => ({ left: cross(t0, t1) > 0, square: Math.abs(dot(t0, t1)) < 0.26 });

export function generateFacades(ctx: Ctx, blds: FacadeBld[], pav: number, near: (id: string) => string[]): FacadeOut {
  const { bays, ends, corners } = catalogue(), B = ctx.B, H = (n: number) => B.plinth + B.groundStorey + (n - 1) * B.upperStorey + B.roofCrown;
  const why = new Map<string, string>(), facades: Facade[] = [], kitFaced = new Set<string>(), metres = new Map<string, { street: number; kit: number }>(), chosen = new Map<string, string>();
  const yawOf = (n: V) => r4(Math.atan2(n[1], n[0]) - Math.PI / 2);
  for (const b of blds) {
    const p = b.ring.p, c = b.ring.c, N = p.length, h = H(b.n), street = c.map(isStreetClass);
    const edgeLen = (i: number) => dist(p[i]!, p[(i + 1) % N]!);
    metres.set(b.id, { street: p.reduce((s, _, i) => s + (street[i] ? edgeLen(i) : 0), 0), kit: 0 });
    if (b.n < 2 || b.n > 11 || !street.some(Boolean)) { why.set(b.id, b.n < 2 || b.n > 11 ? `storeys ${b.n}` : 'no street face'); continue; }
    // a piece's scale: height from H(n) within TOL, width at least the height scale times the squash tag
    const scaleOf = (x: Piece): [number, number, number] | null => {
      const [lo, hi] = x.a.tags.scale, sy = Math.min(hi, Math.max(lo, MIN_SY, h / x.body));
      if (Math.abs(sy * x.body - h) > TOL * h) return null;
      return [r3(Math.min(hi, Math.max(sy * x.a.tags.squash, sy))), r3(sy), r3(sy)];
    };
    const fit = bays.map(x => ({ x, k: scaleOf(x) })).filter((o): o is { x: Piece; k: [number, number, number] } => !!o.k);
    if (!fit.length) { why.set(b.id, `no bay fits H(${b.n})`); continue; }
    const taken = new Set(near(b.id).map(id => chosen.get(id)).filter((x): x is string => !!x));
    const fresh = fit.filter(o => !taken.has(o.x.a.id));
    if (!fresh.length) { why.set(b.id, 'no contrasting bay'); continue; } // no contrasting piece: this frontage stays procedural
    const pick = fresh[Math.floor(rng(`facade/${b.id}`).f() * fresh.length)]!, bay = pick.x, k = pick.k;
    const code = Object.keys(CODE).filter(q => ends.has(q) || corners.has(q)).sort((x, y) => Math.abs(h / CODE[x]! - 1) - Math.abs(h / CODE[y]! - 1))[0] ?? ''; // the end and corner code whose bay height is nearest H(n)
    const endP = (ends.get(code) ?? []).find(e => e.a.id.endsWith('W') === bay.a.id.endsWith('W')) ?? (ends.get(code) ?? [])[0];
    const cornP = (corners.get(code) ?? [])[0], kc = cornP ? scaleOf(cornP) : null, ke = endP ? scaleOf(endP) : null;
    const inside = (q: V) => pointInRing(q, p);
    const rect = (v: V, t: V, nrm: V, w: number, d: number): boolean => [[0.3, 0.3], [w - 0.3, 0.3], [w - 0.3, d - 0.3], [0.3, d - 0.3]].every(([s, e]) => inside([v[0] + t[0] * s! - nrm[0] * e!, v[1] + t[1] * s! - nrm[1] * e!]));
    // vertices: a corner where two street faces meet at a convex right angle with room for the cell on both
    const cornerAt: (boolean)[] = p.map((v, i) => {
      if (!kc || !cornP) return false;
      const e0 = (i + N - 1) % N, t0 = norm(sub(v, p[e0]!)), t1 = norm(sub(p[(i + 1) % N]!, v)), tn = turn(t0, t1), cell = CELL * kc[1];
      if (!tn.left || !tn.square || !street[e0] || !street[i] || edgeLen(e0) < cell + CORNER_SPARE || edgeLen(i) < cell + CORNER_SPARE) return false; // the corner cell fits both faces (the bay run may be empty: the short face is the corner's)
      const n0 = edgeN(p[e0]!, v);
      return rect([v[0] - t0[0] * cell, v[1] - t0[1] * cell], t0, n0, cell, cell);
    });
    // two corners on one face need room for both cells; else the face's head keeps its corner only if the tail has none
    for (let e = 0; e < N; e++) if (cornerAt[e] && cornerAt[(e + 1) % N] && edgeLen(e) < 2 * CELL * kc![1] + CORNER_SPARE) cornerAt[(e + 1) % N] = false;
    const endAt = (i: number): boolean => { // head of edge e: next face is a party wall or court, square, convex
      if (!ke) return false;
      const e = (i + N - 1) % N, v = p[i]!, t0 = norm(sub(v, p[e]!)), t1 = norm(sub(p[(i + 1) % N]!, v)), tn = turn(t0, t1);
      return street[e] && !street[i] && tn.left && tn.square && edgeLen(e) >= BAY * ke[1] + 3;
    };
    const lay = (cornerAt: boolean[]) => {
      let kitM = 0, placed = 0;
      const out: Facade[] = [];
      const push = (f: Omit<Facade, 'id'>) => { out.push({ id: `${b.id}/f${out.length}`, ...f }); };
      for (let e = 0; e < N; e++) {
        if (!street[e]) continue;
        const a = p[e]!, z = p[(e + 1) % N]!, L = edgeLen(e), t = norm(sub(z, a)), nrm = edgeN(a, z), head = (e + 1) % N;
        const tailCorner = cornerAt[e], headCorner = cornerAt[head], headEnd = !headCorner && endAt(head) && L - (tailCorner ? CELL * kc![1] : 0) >= BAY * ke![1] + 3;
        const r0 = tailCorner ? CELL * kc![1] : 0, r1 = headCorner ? CELL * kc![1] : headEnd ? BAY * ke![1] : 0;
        const at = (s: number): V => [a[0] + t[0] * s, a[1] + t[1] * s];
        if (headCorner && cornP) { // corner centre: from the vertex back along the edge and inward; the cell is 8 × 8 under the canopy ring
          const v = z, off = (CELL / 2 - cornP.canopy / 2) * kc![1];
          push({ building: b.id, form: 'corner', storeys: b.n, kit: { id: cornP.a.id, scale: kc!, yaw: yawOf(nrm), at: [r3(v[0] - (t[0] + nrm[0]) * off), r3(v[1] - (t[1] + nrm[1]) * off)] as V }, base: r3(pav), a: at(L - r1).map(r3) as V, b: r3v(v), edge: e });
          kitM += r1; placed++;
        }
        if (headEnd && endP) { // the return on −X is the piece's head end; its footprint centre sits canopy/2 toward the vertex from the 6 m facade centre
          const sc = ke![1], fc = at(L - (BAY / 2) * sc), shift = (endP.canopy / 2) * sc, off = (endP.a.size[2] / 2 - endP.canopy) * sc;
          const c: V = [fc[0] + t[0] * shift - nrm[0] * off, fc[1] + t[1] * shift - nrm[1] * off];
          if (rect(at(L - BAY * sc), t, nrm, BAY * sc, BODY * sc)) { push({ building: b.id, form: 'end', storeys: b.n, kit: { id: endP.a.id, scale: ke!, yaw: yawOf(nrm), at: r3v(c) }, base: r3(pav), a: r3v(at(L - BAY * sc)), b: r3v(z), edge: e }); kitM += BAY * sc; placed++; }
        }
        // the run of bays between the reserved ends: tile it with k bays of width BAY × x, x within the piece's scale range and at least its squash share of the height scale
        const R = L - r0 - r1, [lo, hi] = bay.a.tags.scale, wmin = BAY * Math.max(lo, k[1] * bay.a.tags.squash), wmax = BAY * hi;
        if (R < wmin - 1e-6) continue;
        let s0 = r0, count: number, stride: number;
        const kMin = Math.ceil(R / wmax - 1e-9), kMax = Math.floor(R / wmin + 1e-9);
        if (kMin <= kMax) { count = kMin; for (let q = kMin; q <= kMax; q++) if (Math.abs(R / q - BAY * k[1]) < Math.abs(R / count - BAY * k[1])) count = q; stride = R / count; }
        else { count = kMax; stride = wmax; s0 += R - count * stride; } // not tileable: the gap at the tail stays procedural
        const sx = r3(stride / BAY), off = (bay.a.size[2] / 2 - bay.canopy) * k[2];
        for (let j = 0; j < count; j++) {
          const sa = s0 + j * stride, cc = at(sa + stride / 2);
          if (!rect(at(sa), t, nrm, stride, BODY * k[2])) continue;
          push({ building: b.id, form: 'bay', storeys: b.n, kit: { id: bay.a.id, scale: [sx, k[1], k[2]], yaw: yawOf(nrm), at: [r3(cc[0] - nrm[0] * off), r3(cc[1] - nrm[1] * off)] as V }, base: r3(pav), a: r3v(at(sa)), b: r3v(at(sa + stride)), edge: e });
          kitM += stride; placed++;
        }
      }
      // a corner's tail side covers the next edge's first cell too
      for (let i = 0; i < N; i++) if (cornerAt[i] && kc) kitM += CELL * kc[1];
      return { out, kitM, placed };
    };
    // corners first; a building whose corners leave no room for a single bay keeps its bays and loses the corners instead
    let { out, kitM, placed } = lay(cornerAt);
    if (!out.some(f => f.form === 'bay') && cornerAt.some(Boolean)) ({ out, kitM, placed } = lay(cornerAt.map(() => false)));
    if (!placed || !out.some(f => f.form === 'bay')) { why.set(b.id, `no bay body fits inside the ring (${bay.a.id}, depth ${r3(BODY * k[2])} m)`); continue; } // corner or end alone is no facade
    facades.push(...out); kitFaced.add(b.id); chosen.set(b.id, bay.a.id);
    metres.get(b.id)!.kit = Math.min(metres.get(b.id)!.street, kitM);
  }
  return { facades, kitFaced, metres, why };
}
const r3v = (v: V): V => [r3(v[0]), r3(v[1])];
