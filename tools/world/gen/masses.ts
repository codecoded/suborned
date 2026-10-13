// Building masses: each built plot's building line is split into buildings by facade bay; each building gets a podium,
// a body, a setback and, at standards.buildings.towers.sites, a tower. Heights come from H(n), the bands and the caps.
import { type V, add, cross, dist, dot, len, mul, norm, sub } from '../../plan/geom';
import type { Mass } from '../../../contracts/world/world.schema';
import { type Ctx, type Rng, heightOf, r3, rng, rv, storeysUnder } from './common';
import { buildingLine, kindOf, plotRing } from './edges';
import { type Rect, type Ring, areaOf, fit, insetBoth, insetRing, isSimple, bbox, clipHalf, growRect, inset, offsetPts, rectPts, subtractRect } from './geo';
import { signatureGrounds } from './signatures';
import { GRAMMAR, isStreetClass, roundRing } from './grammar';
import { type FacadeOut, adjacent, generateFacades } from './facades';

/** A generated building, kept for roofs and frontages. `ring` = ground footprint with edge classes; `top` = highest roof polygon at z. */
export type Bld = { id: string; plot: string; area: string; use: string; kind: string; n: number; ring: Ring; top: V[]; z: number; tower: boolean; open: boolean };
export type Site = { id: string; plot: string; rect: Rect; storeys: number; podium: Rect };

const BUILT = ['residential', 'mixed', 'commercial', 'industrial', 'port'];
export const isBuilt = (use: string) => BUILT.includes(use);
const lerp = (a: V, b: V, s: number): V => [a[0] + (b[0] - a[0]) * s, a[1] + (b[1] - a[1]) * s];

/** floor of storey n above the pavement: plinth + ground storey + (n-1) upper storeys. */
export const floorTop = (B: any, n: number) => B.plinth + B.groundStorey + (n - 1) * B.upperStorey;

/** Tower sites: each rect and its podium (the rect grown one bay) clipped to the plot's building line (ground floors stand on it). */
export function towerSites(ctx: Ctx): Site[] {
  const clip = (r: Rect, b: Rect): Rect => [Math.max(r[0], b[0]), Math.min(r[1], b[1]), Math.max(r[2], b[2]), Math.min(r[3], b[3])];
  return ctx.B.towers.sites.map((s: any) => {
    const plot = ctx.d.plots.find(p => p.id === s.plot)!, line = buildingLine(ctx, plotRing(ctx, plot)), lb = line ? bbox(line.p) : bbox(plot.polyV);
    const rect = clip(s.rect, lb), podium = clip(growRect(rect, ctx.B.towers.podiumGrow), lb);
    return { id: s.id, plot: s.plot, rect, storeys: s.storeys, podium };
  });
}

/** Rects no plot building may stand on: megastructure footprints, tower complexes and signature grounds (SG-1). */
export const reservedRects = (ctx: Ctx, sites: Site[]): Rect[] => [...(ctx.plan.megastructures ?? []).flatMap(m => m.footprint as Rect[]), ...sites.map(s => s.podium), ...signatureGrounds(ctx)];

function cuts(ctx: Ctx, L: number, r: Rng, corners: number[] = []): number[] {
  const bay = ctx.B.bay as number, [wMin, wMax] = ctx.B.massing.width as [number, number], out: number[] = [0];
  let at = 0;
  while (L - at > wMax) { const w = bay * r.int(wMin / bay, wMax / bay); at += w; out.push(at); }
  if (L - at < wMin && out.length > 1) out.pop(); // fold a short last building into its neighbour
  out.push(L);
  // a cut never falls within a corner's keep-out (GRAMMAR.cornerKeep m): the corner stands inside one building, so its street corner can be rounded or take a kit corner
  for (let i = 1; i < out.length - 1; i++) for (const c of corners) if (Math.abs(out[i]! - c) < GRAMMAR.cornerKeep) out[i] = out[i]! < c ? c - GRAMMAR.cornerKeep : c + GRAMMAR.cornerKeep;
  return out.filter((x, i) => i === 0 || i === out.length - 1 || (x - out[i - 1]! >= wMin * 0.6 && x > 0 && x < L && out[out.length - 1]! - x >= wMin * 0.6));
}

type Chain = { k0: number; k1: number; corners?: number[] }; // edges k0 .. k1 inclusive of the piece ring

/** Group edges into chains: a long edge stands alone, short consecutive edges (an arc) of one class share a chain; stubs fold backwards. */
function chains(ctx: Ctx, p: V[], c: string[]): Chain[] {
  const wMin = ctx.B.massing.width[0] as number, n = p.length, L = (i: number) => dist(p[i]!, p[(i + 1) % n]!), out: Chain[] = [];
  for (let i = 0; i < n; i++) {
    const last = out[out.length - 1], short = L(i) < 8;
    const q = p[i]!, a = p[(i + n - 1) % n]!, b = p[(i + 1) % n]!, t0 = norm(sub(q, a)), t1 = norm(sub(b, q));
    // a convex right-angle street corner between two long faces stays inside one building (grammar §1: corners are rounded, never notched by two buildings)
    const corner = last && last.k1 === i - 1 && cross(t0, t1) > 0 && Math.abs(dot(t0, t1)) < 0.26 && isStreetClass(c[i - 1]!) && isStreetClass(c[i]!) && L(i - 1) >= GRAMMAR.cornerEdge && L(i) >= GRAMMAR.cornerEdge;
    if (last && (short || L(i) < wMin / 2) && (c[i] === c[last.k1] || L(i) < wMin / 2)) last.k1 = i;
    else if (corner) { let at = 0; for (let k = last!.k0; k < i; k++) at += L(k); (last!.corners ??= []).push(at); last!.k1 = i; }
    else out.push({ k0: i, k1: i });
  }
  return out;
}

/** The strip of the band between outer pts P and inner pts Q over chainage t0..t1 of a chain, as a ring (outer edges keep their class). */
function strip(P: V[], Q: V[], c: string[], ch: Chain, t0: number, t1: number): Ring | null {
  const n = P.length, ks: number[] = [];
  for (let k = ch.k0; k <= ch.k1; k++) ks.push(k);
  const lens = ks.map(k => dist(P[k]!, P[(k + 1) % n]!));
  const outer: V[] = [], inner: V[] = [], oc: string[] = [];
  const at = (t: number): [V, V, number] => {
    let a = 0;
    for (let i = 0; i < ks.length; i++) { if (t <= a + lens[i]! + 1e-9 || i === ks.length - 1) { const s = Math.min(1, Math.max(0, (t - a) / lens[i]!)), k = ks[i]!; return [lerp(P[k]!, P[(k + 1) % n]!, s), lerp(Q[k]!, Q[(k + 1) % n]!, s), i]; } a += lens[i]!; }
    throw new Error('unreachable');
  };
  const [p0, q0, i0] = at(t0), [p1, q1, i1] = at(t1);
  outer.push(p0); inner.push(q0);
  for (let i = i0; i < i1; i++) { const k = ks[i + 1]!; outer.push(P[k]!); inner.push(Q[k]!); }
  outer.push(p1); inner.push(q1);
  for (let i = i0; i <= i1; i++) oc.push(c[ks[i]!]!);
  inner.reverse();
  const pts = [...outer, ...inner], cls = [...oc, 'party', ...inner.slice(1).map(() => 'court'), 'party'];
  // drop repeated points (and their edges)
  const rp: V[] = [], rc: string[] = [];
  pts.forEach((q, i) => { if (dist(q, pts[(i + 1) % pts.length]!) > 1e-3) { rp.push(q); rc.push(cls[i]!); } });
  return rp.length >= 3 && areaOf(rp) > 4 && isSimple(rp) ? { p: rp, c: rc } : null;
}

/** One ring of buildings round `piece`, depth D: the buildings, and the inner ring left over (null if none). */
function band(ctx: Ctx, ring: Ring, D: number, r: Rng): { blds: Ring[]; inner: Ring | null } {
  const both = insetBoth(ring, ring.p.map(() => D));
  if (!both) return { blds: [], inner: null };
  const qb = bbox(both.inner.p);
  if (Math.min(qb[1] - qb[0], qb[3] - qb[2]) < ctx.B.massing.court + 2) return { blds: [], inner: null }; // no real court inside: fill the piece instead
  const piece = both.outer, Q = both.inner.p, blds: Ring[] = [];
  for (const ch of chains(ctx, piece.p, piece.c)) {
    const n = piece.p.length;
    let total = 0; for (let k = ch.k0; k <= ch.k1; k++) total += dist(piece.p[k]!, piece.p[(k + 1) % n]!);
    const ct = cuts(ctx, total, r, ch.corners);
    for (let i = 1; i < ct.length; i++) { const s = strip(piece.p, Q, piece.c, ch, ct[i - 1]!, ct[i]!); if (s) blds.push(s); }
  }
  return { blds, inner: { p: Q, c: Q.map(() => 'court') } };
}

/** A piece too small for a band: slice it into slabs <= max wide along its long axis. */
function slabs(ctx: Ctx, piece: Ring, r: Rng): Ring[] {
  const b = bbox(piece.p), w = b[1] - b[0], h = b[3] - b[2], alongX = w >= h, L = alongX ? w : h;
  if (L <= ctx.B.massing.width[1] * 1.3) return [piece];
  const out: Ring[] = [], ct = cuts(ctx, L, r), o = alongX ? b[0] : b[2];
  for (let i = 1; i < ct.length; i++) {
    const nrm: V = alongX ? [1, 0] : [0, 1], a0 = alongX ? [o + ct[i - 1]!, 0] as V : [0, o + ct[i - 1]!] as V, a1 = alongX ? [o + ct[i]!, 0] as V : [0, o + ct[i]!] as V;
    const s = clipHalf(piece, a0, nrm), t = s && clipHalf(s, a1, mul(nrm, -1));
    if (t && areaOf(t.p) > 20) out.push(t);
  }
  return out;
}

function storeysFor(ctx: Ctx, use: string, area: string, ring: Ring, r: Rng): number {
  const band = ctx.B.bands[use], [lo, hi] = band.storeys as [number, number];
  const cap = ctx.B.caps?.[area]?.m as number | undefined, parkEdge = ring.c.some(c => kindOf(c) === 'park');
  let n = r.weighted(Array.from({ length: hi - lo + 1 }, (_, i) => lo + i), k => 1 + 5 * Math.exp(-((k - band.mode) ** 2) / 2));
  if (use === 'residential' && parkEdge) n = Math.min(hi, n + 1);
  return cap ? Math.min(n, storeysUnder(ctx.B, cap)) : n;
}

type Tier = { part: string; ring: V[]; base: number; height: number; surface: string };

/** The space-age grammar (contracts/world/harbour-district.grammar.md) on a building of >= roundFrom storeys without a kit facade: a sharp
 *  podium (the shopfronts stand on it), the body with streamline corners (r 5.3, 1 in 3 wrap corners 9.6), a setback (§5: >= 7 storeys on a
 *  street corner steps back one bay at n - 2; 4-6 storeys step 3 at the top on 1 in 2; the plan's 6 m above storey 9), speed-band slabs at every
 *  2nd floor line from storey 2. Buildings with a kit facade keep the plain tiers (the kit piece is the wall). */
function grammarTiers(ctx: Ctx, b: { id: string; use: string; ring: Ring; n: number }): { tiers: Tier[]; top: V[]; z: number } {
  const B = ctx.B, pav = ctx.pav, n = b.n, r = rng(`grammar/${b.id}`), G = GRAMMAR, out: Tier[] = [];
  const podium = B.bands[b.use].podium as number | undefined, streetD = (c: string, d: number) => (isStreetClass(c) ? d : 0);
  const podTop = pav + floorTop(B, 2), planStep = B.setback.above as number, topZ = pav + heightOf(B, n);
  out.push({ part: 'podium', ring: b.ring.p, base: 0, height: r3(podTop), surface: 'limestone' });
  let ring: Ring = podium && n > podium ? insetRing(b.ring, b.ring.c.map(c => streetD(c, B.halfBay))) ?? b.ring : b.ring;
  const corner = ring.p.some((v, i) => { const q = ring.p.length, a = ring.p[(i + q - 1) % q]!, c2 = ring.p[(i + 1) % q]!, u1 = norm(sub(v, a)), u2 = norm(sub(c2, v)); return cross(u1, u2) > 0 && Math.abs(dot(u1, u2)) < 0.26 && isStreetClass(ring.c[(i + q - 1) % q]!) && isStreetClass(ring.c[i]!); });
  const wrap = ring.p.map((_, i) => rng(`${b.id}/corner/${i}`).chance(G.wrapShare));
  const line = roundRing(ring, i => (wrap[i] ? G.wrapRadius : G.radius));
  ring = insetRing(line, line.c.map(c => streetD(c, G.band.out))) ?? line; // the wall stands 0.35 m behind the building line; the bands reach the line (B-3: nothing stands past it)
  // one setback: where (storey it starts at) and how far in on the street faces
  let from = 0, by = 0;
  if (n > planStep) { from = planStep; by = B.setback.by; }
  else if (n >= G.stepCorner && corner) { from = n - 2; by = B.bay; }
  else if (G.stepLow.includes(n) && r.chance(G.stepLowShare)) { from = n - 1; by = G.stepLowBy; }
  const stepZ = from ? pav + floorTop(B, from) : topZ;
  out.push({ part: 'body', ring: ring.p, base: r3(podTop), height: r3(stepZ - podTop), surface: b.use });
  let top = ring, z = stepZ;
  if (from) {
    if (n > planStep) { // the plan's rule keeps its part name and its all-round 6 m inset
      const up = inset(ring.p, ring.p.map(() => by)) ?? inset(ring.p, ring.p.map(() => B.halfBay)) ?? ring.p;
      out.push({ part: 'upper', ring: up, base: r3(stepZ), height: r3(topZ - stepZ), surface: b.use }); top = { p: up, c: up.map(() => 'district') };
    } else {
      const st = insetRing(ring, ring.c.map(c => streetD(c, by)));
      if (st) { out.push({ part: 'step', ring: st.p, base: r3(stepZ), height: r3(topZ - stepZ), surface: b.use }); top = st; } else out[1]!.height = r3(topZ - podTop);
    }
    z = topZ;
  }
  // speed bands: slabs 0.35 m proud of the street faces at every 2nd floor line from storey 2, below the setback (and never above the plan's step)
  const last = (from ? from : n) - 1;
  for (let k = 2; k <= last; k += G.band.pitch) {
    const zb = pav + floorTop(B, k) - G.band.thick;
    if (zb < podTop - G.band.thick - 1e-6 || zb + G.band.thick > stepZ) continue;
    out.push({ part: `band-${k}`, ring: (k === 2 ? b.ring : line).p, base: r3(zb), height: G.band.thick, surface: 'enamel' });
  }
  return { tiers: out, top: top.p, z };
}

function tiers(ctx: Ctx, b: { use: string; ring: Ring; n: number; shed: number }): { tiers: Tier[]; top: V[]; z: number } {
  const B = ctx.B, pav = ctx.pav, use = b.use, p = b.ring.p, out: Tier[] = [];
  if (b.shed) { // industrial and port sheds: one body of heightM
    out.push({ part: 'body', ring: p, base: 0, height: r3(pav + b.shed), surface: use });
    return { tiers: out, top: p, z: pav + b.shed };
  }
  const n = b.n, podium = B.bands[use].podium as number | undefined, streetD = (c: string) => (['party', 'court', 'none'].includes(kindOf(c)) ? 0 : 1);
  const podTop = pav + floorTop(B, 2), step = B.setback.above as number, full = pav + floorTop(B, Math.min(n, step));
  let base = 0, ring = p;
  if (podium && n > podium) {
    out.push({ part: 'podium', ring: p, base: 0, height: r3(podTop), surface: 'limestone' });
    base = podTop;
    const body = inset(p, b.ring.c.map(c => streetD(c) * B.halfBay));
    if (body) ring = body;
  }
  const bodyTop = n > step ? full : pav + heightOf(B, n);
  out.push({ part: 'body', ring, base: r3(base), height: r3(bodyTop - base), surface: use });
  let top = ring, z = bodyTop;
  if (n > step) {
    const up = inset(ring, ring.map(() => B.setback.by)) ?? inset(ring, ring.map(() => B.halfBay)) ?? ring;
    z = pav + heightOf(B, n);
    out.push({ part: 'upper', ring: up, base: r3(bodyTop), height: r3(z - bodyTop), surface: use });
    top = up;
  }
  return { tiers: out, top, z };
}

type Pend = { id: string; plot: string; area: string; use: string; kind: string; n: number; ring: Ring; shed: number; open: boolean };
export type MassOut = { masses: Mass[]; blds: Bld[]; facades: FacadeOut };

export function generateMasses(ctx: Ctx, sites: Site[]): MassOut {
  const reserved = reservedRects(ctx, sites), masses: Mass[] = [], blds: Bld[] = [], pend: Pend[] = [];
  const emit = (id: string, plot: string, area: string, use: string, kind: string, n: number, ring: Ring, shed: number, open = false) => {
    const t = tiers(ctx, { use, ring, n, shed });
    const f = fit(t.top, ctx.B.halfBay, false);
    if (f.W < 6 || f.D < 6) return; // a sliver: no roof could carry even the smallest programme (antenna mast 6 x 6)
    pend.push({ id, plot, area, use, kind, n, ring, shed, open });
  };
  for (const plot of ctx.d.plots) {
    if (!isBuilt(plot.use) || plot.civic) continue;
    const r = rng(`plot/${plot.id}`), line = buildingLine(ctx, plotRing(ctx, plot));
    if (!line) continue;
    let pieces: Ring[] = [line];
    for (const rc of reserved) pieces = pieces.flatMap(pc => subtractRect(pc, rc));
    let k = 0;
    const mine: Pend[] = []; // this plot's buildings so far: a building never repeats the storeys of one it shares a wall with
    const add1 = (ring: Ring, open: boolean) => {
      const use = plot.use, id = `bld/${plot.id}/${k++}`, br = rng(id);
      let shed = 0, n = 0, kind = use;
      if (use === 'industrial') { shed = br.pick([9, 10.5, 12, 13.5, 15, 16.5, 18]); kind = 'shed'; }
      else if (use === 'port' && br.chance(0.55)) { shed = br.pick([12, 13.5, 15, 16.5, 18]); kind = 'shed'; }
      else {
        n = storeysFor(ctx, use, plot.area, ring, br);
        const same = mine.filter(m => !m.shed && m.n === n && adjacent(m.ring, ring));
        if (same.length) { // neighbour contrast: re-draw from the same weights without the neighbours' storeys (kept if the band offers nothing else)
          const band = ctx.B.bands[use], [lo, hi] = band.storeys as [number, number], cap = ctx.B.caps?.[plot.area]?.m as number | undefined, top = cap ? Math.min(hi, storeysUnder(ctx.B, cap)) : hi;
          const others = Array.from({ length: top - lo + 1 }, (_, i) => lo + i).filter(x => !mine.some(m => !m.shed && adjacent(m.ring, ring) && m.n === x));
          if (others.length) n = rng(`${id}/contrast`).weighted(others, x => 1 + 5 * Math.exp(-((x - band.mode) ** 2) / 2));
        }
      }
      if (use === 'port' && !shed) kind = 'office';
      const before = pend.length;
      emit(id, plot.id, plot.area, use, kind, n, ring, shed, open);
      if (pend.length > before) mine.push(pend[pend.length - 1]!);
    };
    for (const piece of pieces) {
      const D = ctx.B.massing.bandDepth[plot.use] as number, { blds: outer, inner } = band(ctx, piece, D, r);
      if (!outer.length) { for (const s of slabs(ctx, piece, r)) add1(s, false); continue; }
      for (const b of outer) add1(b, false);
      const R = inner && inset(inner.p, inner.p.map(() => ctx.B.massing.court as number));
      if (R && Math.min(bbox(R)[1] - bbox(R)[0], bbox(R)[3] - bbox(R)[2]) >= ctx.B.massing.infillMin) {
        const ring = { p: R, c: R.map(() => 'court') }, { blds: ib } = band(ctx, ring, D, r);
        for (const b of ib) add1(b, true);
      }
    }
  }
  // wave 2: kit facades on the street faces, then each building's tiers (plain behind a kit facade, the space-age grammar otherwise)
  const solid = pend.filter(p => !p.shed), nearOf = (id: string) => { const me = solid.find(p => p.id === id)!; return solid.filter(p => p.id !== id && p.plot === me.plot && adjacent(me.ring, p.ring)).map(p => p.id); };
  const fac = generateFacades(ctx, solid.map(p => ({ id: p.id, use: p.use, n: p.n, ring: p.ring })), ctx.pav, nearOf);
  for (const p of pend) {
    const kit = fac.kitFaced.has(p.id), t = !kit && !p.shed && p.n >= GRAMMAR.roundFrom ? grammarTiers(ctx, p) : tiers(ctx, { use: p.use, ring: p.ring, n: p.n, shed: p.shed });
    for (const x of t.tiers) masses.push({ id: `${p.id}/${x.part}`, surface: x.surface, ring: x.ring.map(rv), base: x.base, height: x.height });
    blds.push({ id: p.id, plot: p.plot, area: p.area, use: p.use, kind: p.kind, n: p.n, ring: p.ring, top: t.top, z: r3(t.z), tower: false, open: p.open });
  }
  for (const s of sites) { // tower complexes
    const B = ctx.B, pav = ctx.pav, id = `tower/${s.id}`, r = s.rect, podTop = pav + floorTop(B, B.towers.podium);
    masses.push({ id: `${id}/podium`, surface: 'limestone', ring: rectPts(s.podium).map(rv), base: 0, height: r3(podTop) });
    const lowN = Math.min(s.storeys, B.setback.above), lowTop = pav + floorTop(B, lowN), top = pav + heightOf(B, s.storeys);
    masses.push({ id: `${id}/lower`, surface: 'tower', ring: rectPts(r).map(rv), base: r3(podTop), height: r3(lowTop - podTop) });
    const up: Rect = growRect(r, -B.halfBay);
    masses.push({ id: `${id}/upper`, surface: 'tower', ring: rectPts(up).map(rv), base: r3(lowTop), height: r3(top - lowTop) });
    const ring: Ring = { p: rectPts(s.podium), c: ['party', 'party', 'party', 'party'] };
    blds.push({ id, plot: s.plot, area: ctx.d.plots.find(p => p.id === s.plot)!.area, use: 'tower', kind: 'tower', n: s.storeys, ring, top: rectPts(up), z: r3(top), tower: true, open: false });
  }
  return { masses, blds, facades: fac };
}
