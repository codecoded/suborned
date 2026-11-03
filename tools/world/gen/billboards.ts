// Billboards, posters and signage (brief 075-F category 1, Sky: "the old town had billboards / posters well placed"). The old town hand-placed them in
// DecoKitDistrictDressing on readable rules; these are those rules, run over the plan and the export, seeded by id, never placed one by one:
//   BB-1 rooftop: a roof the plan gives the `sign-frame` programme takes a Billboard_Rooftop in place of the procedural frame, standing at the roof's
//        street edge and turned to face the longest street its building fronts (old: boards on the low roofs, "one to the vista, one to the walk").
//        Inside the play streets no board may lift a roof past the cap (standards.buildings.caps[area].m): it is left out and counted (a Sky question).
//   BB-2 wall: a party wall that stands clear above a lower neighbour (the blank wall the old town stacked its wall boards on, 8 m up the crescent's
//        chords) takes one Billboard_Wall near its street end, its foot clear of the neighbour's roof and what stands on it, its top under the wall's.
//   BB-3 pylon: a pair of Billboard_Pylon frames the Parade's vista at its circus mouth (old: ±17 m at the parade); pylons stand along the quay facing
//        inland (old: one on the promenade) and at an open plot's or park's junction mouth (old: the plaza mouths), each through the street layer's tests.
//   BB-4 column: advertising columns at junction mouths on district edges and at the circus's arm mouths (old: kerb + 3 m at the circus mouths),
//        >= 60 m apart (the plan's catalogue.advertising-column), in the kerb zone, through the street layer's tests (SF-1..SF-8).
//   Blade signs and neon stars on frontages by trade are kitrefs.signKit's (brief 006; 167 placed): not repeated here.
// Nothing here stands inside the Handover's window (the capstone zone), so the site and its replays ship unchanged.
import type { KitAsset } from '../../../contracts/world/kit.schema';
import type { Facade, KitRef, Mass, Prim, Roof, StreetItem } from '../../../contracts/world/world.schema';
import { type V, dist, dot, lineDist, norm, sub } from '../../plan/geom';
import { boxRing, ringGap, roadAt, samples9 } from '../../plan/street-zones';
import { footOf } from '../civic/foot';
import { type Ctx, r3, r4, rng, rv } from './common';
import { idOf, kindOf, plotRing } from './edges';
import { areaOf, edgeN, pointInRing } from './geo';
import { forUse } from './kitrefs';
import type { Bld } from './masses';
import { inRect4 } from './street/capstone-rules';
import { zoneOf } from './street/capstone';
import type { Layer } from './street/kit';
import { returnAt } from './kerbreturn';
import { footwayTangentAt } from './street/footway';
import type { DeckRec, MegaOut } from './types';

/** The street kinds the BB rules add (StreetItem.kind): a pylon board on the ground, a wall board on a party wall (its `z` = its foot, `edge` = `<mass id>#<edge>`). */
export const BOARD_KINDS = new Set(['billboard-pylon', 'billboard-wall']);
/** The rule parameters, in one place (each with where it comes from). */
export const BB = {
  /** BB-1: the board stands this far in from the roof rect's street side (m). */
  roofInset: 0.3,
  /** BB-2: m above the neighbour's roof slab its foot clears at least (the parapet 0.6, standards.buildings.roofs.kit.always.parapet.other, + 0.3). */
  wallFoot: 0.9,
  /** BB-2: m it stays under the top of the wall it hangs on. */
  wallTop: 0.3,
  /** BB-2: its street end within this many m of a street (else nobody sees it). */
  wallSeen: 12,
  /** BB-2: m between two wall boards; one per building (the old town: 4 wall boards, three of them on one crescent). */
  wallGap: 200,
  /** BB-3: m between quay pylons, on a quay's water edge of at least `quayMin` m (the harbour quay, not the piers' short sides). */
  quayGap: 300, quayMin: 300,
  /** BB-4: m between advertising columns (plan streetscape.catalogue.advertising-column.count). */
  columnGap: 60,
  /** Seen from the street: m between two boards that would show the same piece (neighbour contrast, as kitrefs.choose). */
  contrast: 120,
};

export type BoardLog = {
  rooftop: { placed: number; roofs: number; left: { roof: string; why: string }[]; /** inside the play streets, the roofs where the board would lift the roof past the cap: Sky's question */ cap: { roof: string; area: string; z: number; top: number; cap: number }[] };
  wall: { placed: number; candidates: number };
  pylon: { placed: number; parade: number; quay: number; plaza: number };
  column: { placed: number; before: number };
};

const ownerOf = (id: string) => id.split('/').slice(0, id.startsWith('tower/') ? 2 : 3).join('/');
const kitYawFacing = (n: V) => r4(Math.atan2(n[1], n[0]) - Math.PI / 2); // the piece's +Z front faces yaw + 90° (kitrefs.reachYaw)
/** The pivot that puts a piece's footprint centre on `c` (footOf: centre = pivot + rot(pivotOffset × scale)). */
const pivotFor = (a: KitAsset, c: V, yaw: number, s: number): V => { const o = a.pivotOffset ?? [0, 0], cs = Math.cos(yaw), sn = Math.sin(yaw); return [c[0] - (o[0] * s * cs - o[1] * s * sn), c[1] - (o[0] * s * sn + o[1] * s * cs)]; };
/** Seeded piece with neighbour contrast: none within BB.contrast m that shows the same piece, if any is left. */
function pick(id: string, pieces: KitAsset[], near: { at: V; id: string }[], at: V): KitAsset | undefined {
  if (!pieces.length) return undefined;
  const taken = new Set(near.filter(o => dist(o.at, at) < BB.contrast).map(o => o.id)), fresh = pieces.filter(a => !taken.has(a.id));
  return rng(`bb/${id}`).pick(fresh.length ? fresh : pieces);
}
const boxOf = (p: Prim): V[] => boxRing(p.c as V, p.size[0], p.size[1], p.yaw);
const overlaps = (a: V[], b: V[]) => ringGap(a, b) < 1e-6;

// ---------------------------------------------------------------- BB-1 rooftop boards on sign-frame roofs

/** The building's street edges (ring classes, edges.ts), longest first: [a, b, outward normal, length]. */
export function streetEdgesOf(b: Pick<Bld, 'ring'>): { a: V; b: V; n: V; len: number; kind: string }[] {
  const p = b.ring.p, n = p.length, cw = areaOf(p) < 0, out: { a: V; b: V; n: V; len: number; kind: string }[] = [];
  for (let i = 0; i < n; i++) {
    const k = kindOf(b.ring.c[i] ?? ''); if (!['primary', 'district', 'access', 'circus', 'mall', 'walk', 'quay', 'park'].includes(k)) continue;
    const a = p[i]!, e = p[(i + 1) % n]!, nn = edgeN(a, e);
    out.push({ a, b: e, n: cw ? [-nn[0], -nn[1]] : nn, len: dist(a, e), kind: k });
  }
  const rank = (k: string) => (['primary', 'district', 'circus', 'mall'].includes(k) ? 0 : k === 'access' || k === 'quay' ? 1 : 2);
  return out.sort((x, y) => rank(x.kind) - rank(y.kind) || y.len - x.len);
}

const FRAME = /\/(leg-0|leg-1|frame)$/, VENT = /\/rocket-vent-\d+-/;

/** BB-1: each sign-frame roof takes a rooftop board (Roof.kit) in place of its procedural frame, or keeps the frame and says why. Mutates `roofs`. */
export function rooftopBoards(ctx: Ctx, roofs: Roof[], blds: Bld[], masses: Mass[]): BoardLog['rooftop'] {
  const log: BoardLog['rooftop'] = { placed: 0, roofs: 0, left: [], cap: [] }, pieces = forUse('billboard:rooftop').sort((a, b) => a.id.localeCompare(b.id));
  const byId = new Map(blds.map(b => [b.id, b])), tip = (ctx.B.towers.crownTip as number) ?? Infinity, placed: { at: V; id: string }[] = [];
  for (const [ri, r] of roofs.entries()) {
    if (r.programme !== 'sign-frame' || r.form) continue;
    log.roofs++;
    const b = byId.get(r.building); if (!b || !pieces.length) { log.left.push({ roof: r.id, why: b ? 'no adopted billboard:rooftop piece' : 'no building' }); continue; }
    const cap = ctx.B.caps?.[b.area] as { m?: number; play?: boolean } | undefined, maxTop = cap?.play && cap.m ? cap.m : tip;
    const a0 = pick(r.id, pieces, placed, r.c as V)!;
    if (r.z + a0.size[1] > maxTop + 1e-6) { // the play-street cap: never lifted past it (Sky's question whether a board counts)
      if (cap?.play) log.cap.push({ roof: r.id, area: b.area, z: r.z, top: r3(r.z + a0.size[1]), cap: maxTop }); else log.left.push({ roof: r.id, why: `top ${r3(r.z + a0.size[1])} m over the crown tip ${tip}` });
      continue;
    }
    const U: V = [Math.cos(r.yaw), Math.sin(r.yaw)], Vv: V = [-U[1], U[0]], W = r.size[0], D = r.size[1], keep = r.parts.filter(p => !FRAME.test(p.id));
    const solid = keep.filter(p => !VENT.test(p.id) && !/\/parapet/.test(p.id)).map(boxOf); // the plant house: the board never stands in it (the vents give way, as they do to a pad)
    // a court building fronts no street: its board faces its plot's street (plan: "the longest street view (... or the plot's longest edge)") where it stands clear over the buildings between
    const own = streetEdgesOf(b), plot = ctx.d.plots.find(p => p.id === b.plot), edges = own.length ? own : plot ? streetEdgesOf({ ring: plotRing(ctx, plot) }) : [];
    let done: { kit: KitRef & { at: V }; ring: V[] } | undefined, hidden = '';
    for (const e of edges) {
      const nU = dot(e.n, U), nV = dot(e.n, Vv), hd = a0.size[2] / 2, ext = Math.abs(nU) * W / 2 + Math.abs(nV) * D / 2, t: V = [-e.n[1], e.n[0]], span = Math.abs(dot(t, U)) * W + Math.abs(dot(t, Vv)) * D;
      const y = kitYawFacing(e.n);
      for (const [back, f] of [0, 7].flatMap(k => [0, 0.25, -0.25, 0.4, -0.4, 0.5, -0.5].map(f => [k, f] as const))) {
        const slide = f * Math.max(0, span - a0.size[0]), off = ext - hd - BB.roofInset - back, c: V = [r.c[0] + e.n[0] * off + t[0] * slide, r.c[1] + e.n[1] * off + t[1] * slide];
        const at = pivotFor(a0, c, y, 1), ring = footOf(a0, { scale: [1, 1, 1], yaw: y, at });
        const inside = ring.every(q => { const dx = q[0] - r.c[0], dy = q[1] - r.c[1]; return Math.abs(dx * U[0] + dy * U[1]) <= W / 2 + 1e-6 && Math.abs(dx * Vv[0] + dy * Vv[1]) <= D / 2 + 1e-6; });
        if (!inside || solid.some(s => overlaps(s, ring))) continue;
        if (!own.length) { const eye = r.z + a0.size[1] / 2, by = masses.find(m => !m.id.startsWith(`${b.id}/`) && m.base + m.height > eye && [6, 12, 18, 24, 30, 36].some(k => pointInRing([c[0] + e.n[0] * k, c[1] + e.n[1] * k], m.ring))); if (by) { hidden = by.id; continue; } }
        done = { kit: { id: a0.id, scale: [1, 1, 1], yaw: y, at: rv(at) }, ring }; break;
      }
      if (done) break;
    }
    if (!done) { log.left.push({ roof: r.id, why: !edges.length ? 'neither the building nor its plot fronts a street' : hidden ? `a court building: its board would face the street behind ${hidden}, taller than the board's middle` : 'no street side of the roof holds the board clear of the plant' }); continue; }
    const ring = done.ring;
    roofs[ri] = { ...r, parts: keep.filter(p => !(VENT.test(p.id) && overlaps(boxOf(p), ring))), kit: done.kit };
    placed.push({ at: r.c as V, id: a0.id }); log.placed++;
  }
  return log;
}

// ---------------------------------------------------------------- BB-2 wall boards on party walls above a lower neighbour

type WallCand = { id: string; owner: string; edge: string; at: V; yaw: number; base: number; a: KitAsset; ring: V[]; c: V };

/** Distance from p to the nearest street (a road's kerb, a walk or mall's edge, the circus pavement). */
function toStreet(ctx: Ctx, p: V): number {
  let d = Infinity;
  for (const r of ctx.d.roads) if (!r.gyratory) d = Math.min(d, lineDist(p, r.line) - r.w / 2);
  for (const f of ctx.plan.foot) d = Math.min(d, lineDist(p, f.pts as V[]) - f.w / 2);
  const c = ctx.plan.circus; d = Math.min(d, Math.abs(dist(p, c.c as V) - c.pavement));
  return d;
}

/** BB-2: wall boards (street items, kind billboard-wall) on the party walls that stand clear above a lower neighbour. */
export function wallBoards(ctx: Ctx, o: { masses: Mass[]; facades: Facade[]; roofs: Roof[]; catalogue: (id: string) => KitAsset | undefined }): { items: StreetItem[]; log: BoardLog['wall'] } {
  const zone = zoneOf(ctx);
  const pieces = forUse('billboard:wall').sort((a, b) => a.id.localeCompare(b.id)), out: StreetItem[] = [], log = { placed: 0, candidates: 0 };
  if (!pieces.length) return { items: out, log };
  const H = Math.max(...pieces.map(a => a.size[1])), Wd = Math.max(...pieces.map(a => a.size[0])), Dp = Math.max(...pieces.map(a => a.size[2]));
  const ms = o.masses.filter(m => !/\/band-\d+$/.test(m.id) && !(m as Mass & { hidden?: boolean }).hidden), top = (m: Mass) => m.base + m.height;
  const bx = ms.map(m => { const xs = m.ring.map(q => q[0]), ys = m.ring.map(q => q[1]); return [Math.min(...xs), Math.max(...xs), Math.min(...ys), Math.max(...ys)] as const; });
  const at = (q: V) => ms.filter((m, i) => q[0] >= bx[i]![0] && q[0] <= bx[i]![1] && q[1] >= bx[i]![2] && q[1] <= bx[i]![3] && pointInRing(q, m.ring));
  const facesOf = new Map<string, Facade[]>(); for (const f of o.facades) if (f.face) (facesOf.get(f.face) ?? facesOf.set(f.face, []).get(f.face)!).push(f);
  const roofsOf = new Map<string, Roof[]>(); for (const r of o.roofs) (roofsOf.get(r.building) ?? roofsOf.set(r.building, []).get(r.building)!).push(r);
  const cands: WallCand[] = [];
  for (const A of ms) {
    const me = ownerOf(A.id), p = A.ring, n = p.length, cw = areaOf(p) < 0, tA = top(A);
    for (let i = 0; i < n; i++) {
      const a = p[i]!, b = p[(i + 1) % n]!, L = dist(a, b); if (L < Wd + 2) continue;
      const u = norm(sub(b, a)), n0 = edgeN(a, b), nn: V = cw ? [-n0[0], -n0[1]] : n0;
      // the neighbour along the wall, metre by metre: another building's mass just outside it, lower by the board and its clearances
      const runs: { s0: number; s1: number; low: number; who: string }[] = [];
      let cur: { s0: number; s1: number; low: number; who: string } | null = null;
      for (let s = 0.5; s < L; s += 1) {
        const q: V = [a[0] + u[0] * s + nn[0] * 0.4, a[1] + u[1] * s + nn[1] * 0.4], hit = at(q);
        const own = hit.some(m => ownerOf(m.id) === me), other = hit.filter(m => ownerOf(m.id) !== me), low = other.length ? Math.max(...other.map(top)) : -1;
        const ok = !own && other.length > 0 && tA - low >= BB.wallFoot + H + BB.wallTop;
        if (ok && cur && Math.abs(cur.low - low) < 0.01) cur.s1 = s; else { if (cur) runs.push(cur); cur = ok ? { s0: s, s1: s, low, who: ownerOf(other[0]!.id) } : null; }
      }
      if (cur) runs.push(cur);
      for (const run of runs) {
        if (run.s1 - run.s0 + 1 < Wd + 1) continue;
        // its street end: the run's end nearer a street, within BB.wallSeen m of one
        const e0: V = [a[0] + u[0] * run.s0, a[1] + u[1] * run.s0], e1: V = [a[0] + u[0] * run.s1, a[1] + u[1] * run.s1], d0 = toStreet(ctx, e0), d1 = toStreet(ctx, e1);
        if (Math.min(d0, d1) > BB.wallSeen) continue;
        const sc = d0 <= d1 ? run.s0 - 0.5 + Wd / 2 + 1 : run.s1 + 0.5 - Wd / 2 - 1, mid: V = [a[0] + u[0] * sc, a[1] + u[1] * sc];
        if (inRect4(mid, zone)) continue; // the Handover window ships as it is
        const face = `${A.id}#${i}`, id = `street/${A.id}/e${i}/billboard-wall`;
        const a0 = pick(id, pieces, cands.map(c => ({ at: c.c, id: c.a.id })), mid)!, y = kitYawFacing(nn);
        // proud of the wall modules on this face (their footprint in front of the wall line, over the board's span)
        let proud = 0;
        for (const f of facesOf.get(face) ?? []) { const k = o.catalogue(f.kit.id); if (!k) continue; for (const q of footOf(k, f.kit)) { const s = dot(sub(q, a), u), dd = dot(sub(q, a), nn); if (s > sc - Wd / 2 - 0.5 && s < sc + Wd / 2 + 0.5 && dd > proud && dd < 3) proud = dd; } }
        const c: V = [mid[0] + nn[0] * (proud + 0.05 + a0.size[2] / 2), mid[1] + nn[1] * (proud + 0.05 + a0.size[2] / 2)], piv = pivotFor(a0, c, y, 1), ring = footOf(a0, { scale: [1, 1, 1], yaw: y, at: piv });
        // its foot clears the neighbour's roof slab and parapet, and whatever stands on that roof under it (plant, vents, pads, crowns)
        let foot = run.low + BB.wallFoot;
        for (const r of roofsOf.get(run.who) ?? []) {
          for (const pr of r.parts) if (overlaps(boxOf(pr), ring)) foot = Math.max(foot, pr.base + pr.size[2] + 0.3);
          for (const k of [r.kit, r.crown]) { const ka = k && o.catalogue(k.id); if (k && ka && overlaps(footOf(ka, k), ring)) foot = Math.max(foot, r.z + ka.size[1] * k.scale[1] + 0.3); }
        }
        if (foot + a0.size[1] > tA - BB.wallTop) continue;
        // nothing else stands in its box (a third building, a tower, a megastructure part rising past its foot)
        if (samples9(ring).some(q => at(q).some(m => top(m) > foot && ownerOf(m.id) !== run.who))) continue;
        cands.push({ id, owner: me, edge: face, at: rv(piv), yaw: y, base: r3(foot), a: a0, ring, c });
      }
    }
  }
  log.candidates = cands.length;
  // seeded order, then one per building and BB.wallGap m apart
  const order = rng('bb/wall').shuffle(cands.sort((x, y) => x.id.localeCompare(y.id))), chosen: WallCand[] = [], owners = new Set<string>();
  for (const c of order) { if (owners.has(c.owner) || chosen.some(o => dist(o.c, c.c) < BB.wallGap)) continue; chosen.push(c); owners.add(c.owner); }
  for (const c of chosen.sort((x, y) => x.id.localeCompare(y.id))) {
    out.push({ id: c.id, kind: 'billboard-wall', at: c.at, yaw: c.yaw, z: c.base, size: [r3(c.a.size[0]), r3(c.a.size[2]), r3(c.a.size[1])], surface: 'trim', edge: c.edge, sight: 'open', source: 'kit', kit: { id: c.a.id, scale: [1, 1, 1], yaw: c.yaw } });
    log.placed++;
  }
  void Dp;
  return { items: out, log };
}

// ---------------------------------------------------------------- BB-3 pylons, BB-4 advertising columns (on the street layer)

/** Is anything over this footprint lower than `top` + 1.5 (plan keepClear.underDeckOrBridge): a deck, a bridge, a megastructure part above the ground? */
function overhead(ring: V[], top: number, decks: DeckRec[], megas: MegaOut[], L: Layer): boolean {
  const zOf = (lv: string) => { const x = (L.ctx.L.list as { id: string; walk?: number; z?: number }[] | undefined)?.find(q => q.id === lv); return x?.walk ?? x?.z ?? 0; }; // a plan whose levels carry no `walk` (before brief 055) gives the level's z, as BB-3 reads it
  for (const d of decks) if (zOf(d.level) > 1 && zOf(d.level) < top + 1.5 && ringGap(ring, d.ring as V[]) < 0.5) return true;
  for (const m of megas) for (const p of m.parts) if (p.base > 1 && p.base < top + 1.5 && ringGap(ring, p.ring) < 0.5) return true;
  return false;
}

/** A pylon of `a` at footprint centre `c`, front facing `n`, tried on the layer: placed (true) or not. */
function tryPylon(L: Layer, a: KitAsset, c: V, n: V, edge: string, decks: DeckRec[], megas: MegaOut[], zone: [number, number, number, number], dry = false): boolean {
  const y = kitYawFacing(n), piv = pivotFor(a, c, y, 1), ring = footOf(a, { scale: [1, 1, 1], yaw: y, at: piv });
  if (ring.some(q => inRect4(q, zone)) || overhead(ring, L.ctx.pav + a.size[1], decks, megas, L)) return false;
  const size: [number, number, number] = [r3(a.size[0]), r3(a.size[2]), r3(a.size[1])], o = { size, r: Math.hypot(a.size[0], a.size[2]) / 2, blocks: true, kit: { id: a.id, scale: [1, 1, 1] as [number, number, number], yaw: y }, pivot: piv };
  return dry ? L.fits('billboard-pylon', c, y, L.ctx.pav, edge, o) : L.add('billboard-pylon', c, y, L.ctx.pav, edge, o);
}

/** BB-3 and BB-4 on the street layer, after everything else has taken its ground (so no other street id moves). */
export function dressAdverts(ctx: Ctx, L: Layer, o: { decks: DeckRec[]; megas: MegaOut[] }): { pylon: BoardLog['pylon']; column: BoardLog['column'] } {
  const zone = zoneOf(ctx), z = ctx.pav, pylons = forUse('billboard:pylon').sort((a, b) => a.id.localeCompare(b.id)), log = { pylon: { placed: 0, parade: 0, quay: 0, plaza: 0 }, column: { placed: 0, before: L.items.filter(i => i.kind === 'advertising-column').length } };
  const used: { at: V; id: string }[] = [];
  const pylon = (id: string, c: V, n: V, edge: string, dry = false) => { const a = pick(id, pylons, used, c); if (!a) return false; const ok = tryPylon(L, a, c, n, edge, o.decks, o.megas, zone, dry); if (ok && !dry) used.push({ at: c, id: a.id }); return ok; };
  if (pylons.length) {
    // BB-3 the Parade's vista: a pair in the centre band at the circus mouth, facing out toward the circus (the old ±17 m pair); both or neither
    for (const f of ctx.plan.foot.filter(q => q.kind === 'mall')) {
      const a = f.pts[0] as V, b = f.pts[f.pts.length - 1] as V, u = norm(sub(b, a)), nl: V = [-u[1], u[0]], back: V = [-u[0], -u[1]], off = 7.5, edge = `foot/${f.id}`;
      const end = dist(a, ctx.plan.circus.c as V) < dist(b, ctx.plan.circus.c as V) ? a : b, fwd: V = end === a ? u : back, face: V = [-fwd[0], -fwd[1]];
      for (const s of [16, 20, 24, 28, 32, 36]) {
        const p1: V = [end[0] + fwd[0] * s + nl[0] * off, end[1] + fwd[1] * s + nl[1] * off], p2: V = [end[0] + fwd[0] * s - nl[0] * off, end[1] + fwd[1] * s - nl[1] * off];
        if (pylon(`${edge}/vista-n`, p1, face, edge, true) && pylon(`${edge}/vista-s`, p2, face, edge, true) && pylon(`${edge}/vista-n`, p1, face, edge) && pylon(`${edge}/vista-s`, p2, face, edge)) { log.pylon.parade += 2; break; }
      }
    }
    // BB-3 the quay: along each quay's long water edge, facing inland, BB.quayGap m apart, 1.5 m (SF-14) + its depth off the edge
    for (const [qi, qr] of ctx.plan.quay.entries()) {
      const ring = qr as V[], m = ring.length, sea = (ctx.plan.water ?? []) as V[][];
      for (let i = 0; i < m; i++) {
        const a = ring[i]!, b = ring[(i + 1) % m]!, Lq = dist(a, b); if (Lq < BB.quayMin) continue;
        const u = norm(sub(b, a)), n0 = edgeN(a, b), out: V = areaOf(ring) < 0 ? [-n0[0], -n0[1]] : n0, inl: V = [-out[0], -out[1]];
        let last = -Infinity;
        for (let s = 30; s < Lq - 30; s += 6) {
          if (s - last < BB.quayGap) continue;
          const e: V = [a[0] + u[0] * s, a[1] + u[1] * s]; if (!sea.some(w => pointInRing([e[0] + out[0] * 2, e[1] + out[1] * 2], w))) continue; // this stretch of edge faces the plan's water
          const c: V = [e[0] + inl[0] * (1.5 + 1.2 + 0.3), e[1] + inl[1] * (1.5 + 1.2 + 0.3)];
          if (pylon(`quay/${qi}/${i}/${s}`, c, inl, `quay/${qi}`)) { last = s; log.pylon.quay++; }
        }
      }
    }
    // BB-3 the plaza mouths: an open plot or park (not civic, not a station bank's) takes one pylon 10-14 m in from a corner where two road edges meet, facing the junction
    for (const pl of ctx.d.plots) {
      if (pl.civic || !['open', 'park', 'square'].includes(pl.use) || (pl as { bank?: unknown }).bank) continue;
      const r = plotRing(ctx, pl), n = r.p.length, roadK = (c: string) => ['primary', 'district', 'access'].includes(kindOf(c));
      let done = false;
      for (const i of rng(`bb/plaza/${pl.id}`).shuffle([...Array(n).keys()])) {
        if (done || !roadK(r.c[(i + n - 1) % n]!) || !roadK(r.c[i]!)) continue;
        const v = r.p[i]!, pa = r.p[(i + n - 1) % n]!, pb = r.p[(i + 1) % n]!, ina = norm(sub(pa, v)), inb = norm(sub(pb, v)), bis = norm([ina[0] + inb[0], ina[1] + inb[1]]);
        for (const k of [10, 12, 14]) if (pylon(`${pl.id}/plaza-${i}`, [v[0] + bis[0] * k, v[1] + bis[1] * k], [-bis[0], -bis[1]], `${pl.id}/plaza`)) { done = true; log.pylon.plaza++; break; }
      }
    }
    log.pylon.placed = log.pylon.parade + log.pylon.quay + log.pylon.plaza;
  }
  // BB-4 advertising columns at junction mouths: district edges' ends (past the crossing landing and SF-8's 6 m) and the circus's arm mouths
  const cols = () => L.items.filter(i => i.kind === 'advertising-column').map(i => i.at), groups: { key: string; spots: { at: (d: number) => V; edge: string }[] }[] = [];
  for (const plot of ctx.d.plots) {
    if (plot.civic) continue;
    const ring = plotRing(ctx, plot), n = ring.p.length;
    for (let i = 0; i < n; i++) {
      if (kindOf(ring.c[i]!) !== 'district') continue;
      const a = ring.p[i]!, b = ring.p[(i + 1) % n]!, len = dist(a, b); if (len < 36) continue;
      const u: V = [(b[0] - a[0]) / len, (b[1] - a[1]) / len], nrm = edgeN(a, b), id = `${plot.id}/e${i}`, me = idOf(ring.c[i]!);
      const at = (s: number, d: number): V => [a[0] + u[0] * s - nrm[0] * d, a[1] + u[1] * s - nrm[1] * d];
      const rA = returnAt(ctx, plot.id, a), rB = returnAt(ctx, plot.id, b);
      const tA = rA?.t ?? footwayTangentAt(ctx, [idOf(ring.c[(i + n - 1) % n]!), me], a), tB = rB?.t ?? footwayTangentAt(ctx, [me, idOf(ring.c[(i + 1) % n]!)], b);
      const steps = [10, 11.5, 13, 14.5, 16, 18, 20];
      groups.push({ key: `${id}/a`, spots: steps.map(s => ({ at: (d: number) => at(tA + s, d), edge: id })) }, { key: `${id}/b`, spots: steps.map(s => ({ at: (d: number) => at(len - tB - s, d), edge: id })) });
    }
  }
  const c = ctx.plan.circus;
  for (const rd of ctx.d.roads.filter(r => !r.gyratory)) {
    const p0 = rd.line[0] as V, p1 = rd.line[rd.line.length - 1] as V, near = dist(p0, c.c as V) < dist(p1, c.c as V) ? p0 : p1;
    if (dist(near, c.c as V) > c.pavement + 30) continue; // an arm: one end at the circus
    const th = Math.atan2(near[1] - c.c[1], near[0] - c.c[0]);
    for (const side of [1, -1]) groups.push({ key: `circus/${rd.id}/${side}`, spots: [8, 10, 12, 14].map(g => ({ at: (d: number): V => { const rk = c.ring[1] + d, ph = th + side * (rd.w / 2 + g) / rk; return [c.c[0] + rk * Math.cos(ph), c.c[1] + rk * Math.sin(ph)]; }, edge: 'circus' })) });
  }
  // the piece is fitted to the kerb zone it stands in (SF-5 0.45 m off the kerb face, SF-6 inside the zone, measured on the piece: the fitter would grow it to the plan's slot, 1.82 m, past a 2 m zone)
  const col = forUse('advertising-column').sort((a, b) => a.id.localeCompare(b.id))[0];
  for (const g of col ? rng('bb/column').shuffle(groups.sort((x, y) => x.key.localeCompare(y.key))) : []) {
    for (const sp of g.spots) {
      const kerb = roadAt(ctx, sp.at(1), sp.edge)?.sec.kerb ?? 0, sc = Math.min(col!.tags.scale[1], r3((kerb - 0.45 - 0.05) / Math.max(col!.size[0], col!.size[2])));
      if (sc < col!.tags.scale[0]) continue;
      const D = Math.max(col!.size[0], col!.size[2]) * sc, p = sp.at(0.45 + 0.025 + D / 2);
      if (inRect4(p, zone) || cols().some(q => dist(q, p) < BB.columnGap)) continue;
      if (L.add('advertising-column', p, 0, z, sp.edge, { size: [r3(D), r3(D), r3(col!.size[1] * sc)], r: D / 2, kit: { id: col!.id, scale: [sc, sc, sc] }, pivot: pivotFor(col!, p, 0, sc) })) { log.column.placed++; break; }
    }
  }
  return log;
}

// ---------------------------------------------------------------- the ad each board shows (Vela 075-F1, kit.json `posters`, suborned.posters/1)

/** One placed board or poster piece and the ad it shows: `key` = an ad of kit posters.adOrder (a billboard), or the sheet 'A' | 'B' (AdColumn, SandwichBoard, Marquee_Cinema);
 *  `back` = the ad on a pylon's back face (posters.backFace[key]); `slots` = base material → the key's material (empty for sheet A: the base stays). Latch swaps per instance, as the liveries. */
export type PosterPlaced = { host: string; piece: string; key: string; back?: string; slots: Record<string, string> };
type KitPosters = { adOrder: string[]; backFace: Record<string, string>; adPieces: Record<string, string>; sheetPieces: Record<string, string>; ads: Record<string, { materials: Record<string, string> }>; sheets: { B: { materials: Record<string, string> } } };
/** m: two boards within this see each other (Vela: "≈ 60 m, or the same street run and vista"). */
export const POSTER_VIEW = 60;

/** Vela's proposed pick: key = adOrder[hash(host) mod 16], stepping on while it (or a pylon's back face) is already shown within POSTER_VIEW m or on the same street edge; a sheet piece takes
 *  sheet hash(host) mod 2, flipped where the nearest sheet piece within POSTER_VIEW m on its edge shows the same sheet. In element id order: deterministic. */
export function postersOf(w: { roofs: Roof[]; street: StreetItem[]; civic: Mass[] }, kp: KitPosters | undefined): { placed: PosterPlaced[] } | undefined {
  if (!kp) return undefined;
  type Inst = { host: string; piece: string; at: V; edge: string };
  const all: Inst[] = [
    ...w.roofs.flatMap(r => (r.kit ? [{ host: r.id, piece: r.kit.id, at: r.kit.at as V, edge: r.building }] : [])),
    ...w.street.flatMap(s => (s.kit ? [{ host: s.id, piece: s.kit.id, at: s.at, edge: s.edge }] : [])),
    ...w.civic.flatMap(m => (m.kit ? [{ host: m.id, piece: m.kit.id, at: m.kit.at, edge: 'civic' }] : [])),
  ].filter(i => kp.adPieces[i.piece] || kp.sheetPieces[i.piece]).sort((a, b) => a.host.localeCompare(b.host));
  const shown: { at: V; edge: string; ads: string[] }[] = [], sheets: { at: V; edge: string; sheet: string }[] = [], placed: PosterPlaced[] = [], n = kp.adOrder.length;
  for (const i of all) {
    const h = rng(`poster/${i.host}`).int(0, 1 << 30);
    if (kp.adPieces[i.piece]) {
      const base = kp.adPieces[i.piece]!, lit = /_Lit$/.test(base), seen = new Set(shown.filter(o => o.edge === i.edge || dist(o.at, i.at) < POSTER_VIEW).flatMap(o => o.ads));
      let k = h % n, step = 0;
      while (step < n && (seen.has(kp.adOrder[k]!) || (lit && seen.has(kp.backFace[kp.adOrder[k]!]!)))) { k = (k + 1) % n; step++; }
      const key = kp.adOrder[k]!, back = lit ? kp.backFace[key] : undefined, mat = kp.ads[key]?.materials[base];
      if (!mat) continue;
      shown.push({ at: i.at, edge: i.edge, ads: back ? [key, back] : [key] });
      placed.push({ host: i.host, piece: i.piece, key, ...(back ? { back } : {}), slots: { [base]: mat } });
    } else {
      const base = kp.sheetPieces[i.piece]!, near = sheets.filter(o => o.edge === i.edge && dist(o.at, i.at) < POSTER_VIEW).sort((a, b) => dist(a.at, i.at) - dist(b.at, i.at))[0];
      let sheet = h % 2 ? 'B' : 'A'; if (near && near.sheet === sheet) sheet = sheet === 'A' ? 'B' : 'A';
      sheets.push({ at: i.at, edge: i.edge, sheet });
      placed.push({ host: i.host, piece: i.piece, key: sheet, slots: sheet === 'B' && kp.sheets.B.materials[base] ? { [base]: kp.sheets.B.materials[base]! } : {} });
    }
  }
  return { placed };
}
