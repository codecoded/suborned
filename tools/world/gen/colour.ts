// Colour life (grammar §2a, brief 007 round 3; Sky: "too much beige"). The cream body stays; colour rides on many small elements at once, so that
// accent covers 20-30% of a street facade's lower storeys (ground + first two upper storeys, where the play camera looks):
//   1 faience ground storey (plinth, ground-top band, shop bulkheads in the family's plinth swatch; accent piers between shop bays on 1 in 2),
//   2 speed bands (the role colour on the lower line), 3 accent spandrels under ribbon windows (1 in 3), 3a a 1.5 m accent panel behind every porthole, 4 coral or teal fins (every procedural street
//   corner, every lobby entrance) with gold beads, 5 gold trim, 6 balcony awnings (1 per 2 upper bays on 1 in 3 residential buildings) and banners
//   (every lobby frontage, 1 in 3 street corners, above the deck line).
// The seeded choices (piers 1 in 2, spandrels 1 in 3) are the start; a building whose measured lower share falls outside 20-30% takes the nearest
// configuration (piers on/off, spandrels none / 0.9 / deep 1.9, bulkheads 0.6 / 0.9) that lands it in the band, so every facade lands there where it can. Kit-faced spans are the kit's own colour and are
// not measured. Everything is seeded by element id; no Math.random.
import type { Awning, Banner, Facade, Fin, Flag, Frontage, Mass, Skin, SkinFace } from '../../../contracts/world/world.schema';
import { type V, cross, dist, dot, norm, sub } from '../../plan/geom';
import { type Ctx, r3, r4, rng } from './common';
import { edgeN, pointInRing } from './geo';
import { isStreetClass } from './grammar';
import { type Bld, floorTop } from './masses';
import type { BaseSkin } from './skins';

/** §2 accent roles and weights (look.json surfaces); gold is the trim on every building, never its accent. */
export const ACCENT_ROLES: [string, number][] = [['accent_teal', 35], ['accent_coral', 20], ['accent_turquoise', 15], ['accent_mint', 10], ['accent_flamingo', 10], ['accent_peach', 10]];
export const GOLD = 'accent_gold', CANVAS = 'canvas';
/** Fabric never takes the cone red; these are the only fabric colours (accent roles + canvas). */
export const FABRIC_SURFACES = new Set([...ACCENT_ROLES.map(r => r[0]), GOLD, CANVAS]);

export const COLOUR = {
  zoneStoreys: 3, share: [0.2, 0.3] as const,
  minTarget: 12, // m of procedural street face below which a facade is a remnant beside kit pieces: measured, not held to the band
  faience: { plinth: 0.5, band: 0.4, bulkhead: 0.6, pier: 1.05, piersShare: 0.5 },
  bandLine: 0.25, // the role-coloured lower line of a speed band pair (the upper is chrome)
  /** h: the panel from the sill down (0.9 = floor to sill); deep 1.9 = from the head of the storey below to the sill (the whole wall between ribbons);
   *  balcony: the solid front of a balcony bay (B), w x h. */
  spandrel: { h: 0.9, deep: 1.9, share: 1 / 3, glassShare: 0.55, maxPane: 4, bay: 6, balcony: { w: 2.4, h: 0.9 } },
  shopHeight: 4.6,
  /** §2a item 3a (Vela, round 3 wrap-up): a w x h accent panel behind every porthole, centred on it, its chrome ring proud of it; the porthole centre is the
   *  storey's sill-to-head midpoint (row + 1.75), under an eave at most `d/2 + ring + 0.1` below the top (ProcSkin.Overlay). */
  portholePanel: { w: 1.5, h: 1.5 },
  fin: { w: 1.2, d: 0.6, bead: 0.08, roles: ['accent_coral', 'accent_teal'] as const, minH: 3 },
  awning: { w: 2.4, d: 0.9, fall: 0.45, valance: 0.3, stripe: 0.4, share: 1 / 3, every: 2, at: 3.25 }, // `at`: back edge above the storey floor (head 2.6)
  banner: { w: 1.2, h: 6, border: 0.12, arm: 0.6, cornerShare: 1 / 3, over: 0.4 }, // `over`: bottom this far above the deck line
  flag: { h: 8, pennant: [1.2, 1.8] as [number, number], pitch: 9, roles: ['accent_coral', 'accent_teal', 'accent_gold', 'accent_flamingo'], r: 0.25 },
};

type Span = [number, number];
const union = (xs: Span[]): Span[] => { const s = xs.filter(x => x[1] - x[0] > 1e-6).sort((a, b) => a[0] - b[0]), o: Span[] = []; for (const x of s) { const l = o[o.length - 1]; if (l && x[0] <= l[1] + 1e-6) l[1] = Math.max(l[1], x[1]); else o.push([...x]); } return o; };
const covered = (sp: Span[], s: number) => sp.some(([a, b]) => s >= a - 1e-6 && s <= b + 1e-6);
const coverLen = (sp: Span[]) => sp.reduce((n, [a, b]) => n + b - a, 0);
const overlapZ = (a0: number, a1: number, b0: number, b1: number) => Math.max(0, Math.min(a1, b1) - Math.max(a0, b0));

/** The spans of a face a→z (metres along it) that kit facade pieces of the building stand on or hide (the kit bays stand to the building's full height). */
function kitSpans(a: V, z: V, fac: Facade[]): Span[] {
  const L = dist(a, z), u = norm(sub(z, a)), out: Span[] = [];
  for (const f of fac) {
    const v = sub(f.b, f.a), fl = Math.hypot(v[0], v[1]); if (fl < 0.1) continue;
    // parallel, and on the piece's face line or up to its 14 m body behind it (a body set back behind a kit bay is hidden by it)
    const par = Math.abs(cross(u, v)) / fl < 0.05, fn = edgeN(f.a, f.b), off = dot(sub(a, f.a), fn);
    if (par && off < 1.2 && off > -14) { const s0 = dot(sub(f.a, a), u), s1 = dot(sub(f.b, a), u); out.push([Math.max(0, Math.min(s0, s1)), Math.min(L, Math.max(s0, s1))]); continue; }
    if (f.form === 'corner' && dist(a, f.b) < 1.5) out.push([0, Math.min(L, 8 * f.kit.scale[1])]); // a corner's side cell runs along the leaving edge
  }
  return union(out);
}

/** Where the inward ray from p along -n first meets a ring edge (the wall behind a building-line point); p itself if none within 15 m. */
function wallAt(p: V, n: V, ring: V[]): V {
  let best = Infinity;
  for (let i = 0; i < ring.length; i++) {
    const a = ring[i]!, b = ring[(i + 1) % ring.length]!, e = sub(b, a), d: V = [-n[0], -n[1]], den = cross(d, e); if (Math.abs(den) < 1e-9) continue;
    const t = cross(sub(a, p), e) / den, s = cross(sub(a, p), d) / den;
    if (t > -1.5 && t < 15 && s >= -1e-6 && s <= 1 + 1e-6 && t < best) best = t;
  }
  return Number.isFinite(best) ? [p[0] - n[0] * best, p[1] - n[1] * best] : p;
}

/** Top of the stack of masses standing at q (contiguous from the ground up). */
function topAt(q: V, ms: Mass[]): number {
  const here = ms.filter(m => pointInRing(q, m.ring)).sort((a, b) => a.base - b.base); let top = -Infinity;
  for (const m of here) { if (top > -Infinity && m.base > top + 0.05) break; top = Math.max(top, m.base + m.height); }
  return top;
}

const PP = COLOUR.portholePanel;

export type ColourOut = { skins: Skin[]; awnings: Awning[]; banners: Banner[] };

export function colourLife(ctx: Ctx, blds: Bld[], masses: Mass[], skins: BaseSkin[], facades: Facade[], frontages: Frontage[]): ColourOut {
  const B = ctx.B, pav = ctx.pav, C = COLOUR, zone1 = pav + floorTop(B, C.zoneStoreys), deck = pav + floorTop(B, 2);
  const byB = new Map<string, Mass[]>(), facB = new Map<string, Facade[]>(), frB = new Map<string, Frontage[]>(), bld = new Map(blds.map(b => [b.id, b]));
  for (const m of masses) { const id = m.id.slice(0, m.id.lastIndexOf('/')); (byB.get(id) ?? byB.set(id, []).get(id)!).push(m); }
  for (const f of facades) (facB.get(f.building) ?? facB.set(f.building, []).get(f.building)!).push(f);
  for (const f of frontages) if (f.level === 'street') (frB.get(f.host) ?? frB.set(f.host, []).get(f.host)!).push(f);
  const awnings: Awning[] = [], banners: Banner[] = [], out: Skin[] = [];
  for (const sk of skins) {
    const b = bld.get(sk.building)!, ms = (byB.get(b.id) ?? []).filter(m => !/\/band-\d+$/.test(m.id)), bands = (byB.get(b.id) ?? []).filter(m => /\/band-\d+$/.test(m.id));
    const massOf = new Map(ms.map(m => [m.id, m])), fac = facB.get(b.id) ?? [], r = rng(`colour/${b.id}`);
    const accent2 = r.weighted(ACCENT_ROLES.filter(x => x[0] !== sk.accent), x => x[1])[0];
    let piers = r.chance(C.faience.piersShare), spandrel = r.chance(C.spandrel.share);
    const resi = b.use === 'residential' && r.chance(C.awning.share), finRole = C.fin.roles[r.int(0, 1)]!;
    const street = sk.faces.filter(f => /[RPB]/.test(f.bays));
    // per face: its kit spans, the procedural length, the zone height it carries (lower storeys) and its whole height
    const fx = street.map(f => { const m = massOf.get(f.mass)!, L = dist(f.a, f.b), sp = kitSpans(f.a, f.b, fac), Lp = Math.max(0, L - coverLen(sp)), top = m.base + m.height;
      return { f, m, L, sp, Lp, hz: overlapZ(m.base, top, pav, zone1), hw: overlapZ(m.base, top, pav, top), ground: m.base < 1 }; });
    // shopfront metres per face (street frontages of this building not under a kit facade, on the face's line)
    const shopW = (f: SkinFace, sp: Span[]) => {
      const u = norm(sub(f.b, f.a)); let w = 0;
      for (const t of frB.get(b.id) ?? []) { if (t.facade || t.lobby) continue; const mid: V = [(t.a[0] + t.b[0]) / 2, (t.a[1] + t.b[1]) / 2], s = dot(sub(mid, f.a), u);
        if (Math.abs(cross(u, sub(mid, f.a))) < 1.2 && s > 0 && s < dist(f.a, f.b) && !covered(sp, s)) w += t.w; }
      return w;
    };
    // fins: corners of the building line (convex, both edges street, not a kit corner) and lobby entrances
    const fins: Fin[] = [], ground = ms.filter(m => m.base < 1).sort((a, c) => c.height - a.height)[0] ?? ms[0]!;
    const P = b.ring.p, N = P.length, corners: { at: V; n: V; i: number }[] = [];
    for (let i = 0; i < N; i++) {
      const a = P[(i + N - 1) % N]!, v = P[i]!, c = P[(i + 1) % N]!, u1 = norm(sub(v, a)), u2 = norm(sub(c, v));
      if (!(cross(u1, u2) > 0 && Math.abs(dot(u1, u2)) < 0.26 && isStreetClass(b.ring.c[(i + N - 1) % N]!) && isStreetClass(b.ring.c[i]!))) continue;
      if (fac.some(f => f.form === 'corner' && dist(f.b, v) < 1.5)) continue; // the kit corner piece is the corner
      const n = norm([edgeN(a, v)[0] + edgeN(v, c)[0], edgeN(a, v)[1] + edgeN(v, c)[1]]), w = wallAt(v, n, ground.ring), z1 = topAt([w[0] - n[0] * 0.5, w[1] - n[1] * 0.5], ms);
      if (!(z1 - (pav + C.faience.plinth) >= C.fin.minH)) continue;
      corners.push({ at: w, n, i });
      fins.push({ id: `${b.id}/fin/corner-${i}`, kind: 'corner', at: [r3(w[0]), r3(w[1])], normal: [r4(n[0]), r4(n[1])], w: C.fin.w, d: C.fin.d, z0: r3(pav + C.faience.plinth), z1: r3(z1), surface: finRole, bead: { surface: GOLD, w: C.fin.bead } });
    }
    const lobbies = (frB.get(b.id) ?? []).filter(t => t.lobby && !t.facade);
    for (const t of lobbies) {
      const mid: V = [(t.a[0] + t.b[0]) / 2, (t.a[1] + t.b[1]) / 2], n = t.normal, w = wallAt(mid, n, ground.ring), z1 = topAt([w[0] - n[0] * 0.5, w[1] - n[1] * 0.5], ms), z0 = t.canopy.base + t.canopy.size[2] + 0.3;
      if (z1 - z0 < C.fin.minH) continue;
      fins.push({ id: `${t.id}/fin`, kind: 'entrance', at: [r3(w[0]), r3(w[1])], normal: [r4(n[0]), r4(n[1])], w: C.fin.w, d: C.fin.d, z0: r3(z0), z1: r3(z1), surface: finRole, bead: { surface: GOLD, w: C.fin.bead } });
    }
    // banners: beside every lobby entrance and off 1 in 3 corner fins, from just above the deck line, where the wall stands that high
    const bz0 = r3(deck + C.banner.over), bTop = bz0 + C.banner.h;
    const myBanners: Banner[] = [];
    for (const t of lobbies) {
      const u = norm(sub(t.b, t.a)), mid: V = [(t.a[0] + t.b[0]) / 2 + u[0] * (C.fin.w / 2 + 1.0), (t.a[1] + t.b[1]) / 2 + u[1] * (C.fin.w / 2 + 1.0)], w = wallAt(mid, t.normal, ground.ring);
      if (topAt([w[0] - t.normal[0] * 0.5, w[1] - t.normal[1] * 0.5], ms) < bTop + 0.5) continue;
      myBanners.push({ id: `${t.id}/banner`, building: b.id, kind: 'lobby', at: [r3(w[0]), r3(w[1])], normal: [r4(t.normal[0]), r4(t.normal[1])], yaw: r4(Math.atan2(t.normal[1], t.normal[0]) - Math.PI / 2), z0: bz0, w: C.banner.w, h: C.banner.h, arm: C.banner.arm, surface: accent2, border: { surface: GOLD, w: C.banner.border } });
    }
    for (const c of corners) {
      if (!rng(`colour/${b.id}/corner-banner/${c.i}`).chance(C.banner.cornerShare)) continue;
      if (topAt([c.at[0] - c.n[0] * 0.5, c.at[1] - c.n[1] * 0.5], ms) < bTop + 0.5) continue;
      const at: V = [c.at[0] + c.n[0] * C.fin.d, c.at[1] + c.n[1] * C.fin.d]; // hung off the fin's outer face
      myBanners.push({ id: `${b.id}/banner/corner-${c.i}`, building: b.id, kind: 'corner', at: [r3(at[0]), r3(at[1])], normal: [r4(c.n[0]), r4(c.n[1])], yaw: r4(Math.atan2(c.n[1], c.n[0]) - Math.PI / 2), z0: bz0, w: C.banner.w, h: C.banner.h, arm: C.banner.arm, surface: accent2, border: { surface: GOLD, w: C.banner.border } });
    }
    // balcony awnings: 1 per 2 upper-storey glazed bays (R or B) on the procedural spans of 1 in 3 residential buildings
    const myAwnings: Awning[] = [];
    if (resi) for (const { f, sp, L } of fx) {
      const n = f.bays.length, w = L / n, u = norm(sub(f.b, f.a)), nr = edgeN(f.a, f.b), off = rng(`colour/${f.mass}/${f.edge}/awning`).int(0, 1);
      if (w < C.awning.w + 0.4) continue;
      for (const row of f.rows) for (let j = 0; j < n; j++) {
        if ((j + off) % C.awning.every || !/[RB]/.test(f.bays[j]!) || covered(sp, (j + 0.5) * w)) continue;
        const c: V = [f.a[0] + u[0] * (j + 0.5) * w, f.a[1] + u[1] * (j + 0.5) * w], z = row + C.awning.at;
        if (z > massOf.get(f.mass)!.base + massOf.get(f.mass)!.height - 0.3) continue;
        myAwnings.push({ id: `${f.mass}/e${f.edge}/awning-${j}-${Math.round(row * 100)}`, building: b.id, face: `${f.mass}#${f.edge}`, at: [r3(c[0]), r3(c[1])], normal: [r4(nr[0]), r4(nr[1])], yaw: r4(Math.atan2(nr[1], nr[0]) - Math.PI / 2), z: r3(z), w: C.awning.w, d: C.awning.d, fall: C.awning.fall, valance: C.awning.valance, stripe: C.awning.stripe, stripes: [sk.accent, CANVAS] });
      }
    }
    // the accent area: [lower zone, whole height], for a pier switch, a spandrel depth (0 = none) and a bulkhead height
    const measure = (piersOn: boolean, sp: number, bulk: number) => {
      const wall = [0, 0], acc = [0, 0], parts: Record<string, number> = {};
      let tag = '';
      const add = (k: 0 | 1, a: number) => { acc[k] += a; if (k === 0) parts[tag] = (parts[tag] ?? 0) + a; };
      for (const x of fx) {
        if (x.Lp <= 0) continue;
        const frac = x.Lp / x.L, top = x.m.base + x.m.height; wall[0] += x.Lp * x.hz; wall[1] += x.Lp * x.hw;
        if (x.ground) {
          tag = 'faience'; const fa = x.Lp * (C.faience.plinth + C.faience.band) + shopW(x.f, x.sp) * bulk; add(0, fa); add(1, fa);
          tag = 'piers'; if (piersOn) { const pa = x.Lp * (C.faience.pier / C.spandrel.bay) * C.shopHeight; add(0, pa); add(1, pa); }
        }
        tag = 'bands'; for (const bm of bands) { const z = bm.base; if (z >= x.m.base && z < top) { const ba = x.Lp * C.bandLine; add(1, ba); if (z < zone1) add(0, ba); } }
        tag = 'spandrel'; if (sp > 0) {
          const nR = [...x.f.bays].filter(k => k === 'R').length, nB = [...x.f.bays].filter(k => k === 'B').length, rib = Math.min(C.spandrel.glassShare * (x.L / x.f.bays.length), C.spandrel.maxPane);
          for (const row of x.f.rows) {
            const h = Math.min(sp, row + 0.9 - x.m.base), sa = (nR * rib * h + nB * C.spandrel.balcony.w * C.spandrel.balcony.h) * frac; // never below the mass
            add(1, sa); if (row + 0.9 <= zone1) add(0, sa);
          }
        }
      }
      tag = 'portholes';
      for (const x of fx) {
        if (x.Lp <= 0) continue;
        const nP = [...x.f.bays].filter(k => k === 'P').length * sk.porthole.perBay, top = x.m.base + x.m.height; if (!nP) continue;
        for (const row of x.f.rows) {
          const zc = Math.min(row + (sk.glass.sill + sk.glass.head) / 2, top - sk.porthole.d / 2 - sk.porthole.ring - 0.1), z0 = zc - PP.h / 2, z1 = zc + PP.h / 2, n = nP * (x.Lp / x.L);
          add(0, n * PP.w * overlapZ(z0, z1, pav, zone1)); add(1, n * PP.w * (z1 - z0));
        }
      }
      tag = 'fins'; for (const f of fins) { add(0, f.w * overlapZ(f.z0, f.z1, pav, zone1)); add(1, f.w * (f.z1 - f.z0)); }
      tag = 'banners'; for (const x of myBanners) { add(0, x.w * overlapZ(x.z0, x.z0 + x.h, pav, zone1)); add(1, x.w * x.h); }
      tag = 'awnings'; for (const a of myAwnings) { const s = a.w * (a.fall + a.valance) * 0.5; add(1, s); if (a.z <= zone1) add(0, s); }
      if (process.env.COLOUR_DEBUG && b.id.includes(process.env.COLOUR_DEBUG)) console.error(b.id, b.use, b.n, sk.family, piersOn, sp, bulk, 'zone', wall[0].toFixed(0), Object.entries(parts).map(([k, v]) => `${k} ${(v / wall[0]).toFixed(3)}`).join(' '));
      return { lower: wall[0] > 1 ? acc[0] / wall[0] : null, whole: wall[1] > 1 ? acc[1] / wall[1] : null };
    };
    // the seeded choice first; if its lower share is outside 20-30%, the nearest configuration (fewest changes from the seed, then nearest 25%) that is inside,
    // else the one nearest the band. Spandrels need ribbon or balcony bays.
    const hasSp = fx.some(x => x.Lp > 0 && /[RB]/.test(x.f.bays)), [lo, hi] = C.share, S = C.spandrel;
    const seedSp = spandrel && hasSp ? S.h : 0, configs: { piers: boolean; sp: number; bulk: number; cost: number }[] = [];
    for (const pz of [piers, !piers]) for (const sp of hasSp ? [seedSp, ...[0, S.h, S.deep].filter(v => v !== seedSp)] : [0]) for (const bulk of [C.faience.bulkhead, 0.9])
      configs.push({ piers: pz, sp, bulk, cost: (pz !== piers ? 1 : 0) + (sp !== seedSp ? (sp === S.deep ? 2 : 1) : 0) + (bulk !== C.faience.bulkhead ? 1 : 0) });
    const scored = configs.map(c => ({ c, sh: measure(c.piers, c.sp, c.bulk) })), miss = (x: number | null) => (x === null ? 0 : x < lo ? lo - x : x > hi ? x - hi : 0);
    const best = scored.slice().sort((a, b2) => miss(a.sh.lower) - miss(b2.sh.lower) || a.c.cost - b2.c.cost || Math.abs((a.sh.lower ?? 0.25) - 0.25) - Math.abs((b2.sh.lower ?? 0.25) - 0.25))[0]!;
    piers = best.c.piers; const spH = best.c.sp, bulk = best.c.bulk, sh = best.sh; spandrel = spH > 0;
    const panels = sk.faces.some(f => f.bays.includes('P') || f.portholes.length);
    const elements = ['faience', ...(bands.length ? ['speed-bands'] : []), ...(spandrel ? ['spandrels'] : []), ...(panels ? ['porthole-panels'] : []), ...(fins.length ? ['fins'] : []), 'gold-trim', ...(myAwnings.length ? ['awnings'] : []), ...(myBanners.length ? ['banners'] : []), ...(piers ? ['piers'] : [])];
    out.push({ ...sk, porthole: { ...sk.porthole, panel: { w: PP.w, h: PP.h, surface: sk.accent } }, accent2, trim: GOLD, faience: { swatch: 'family_p', plinth: C.faience.plinth, band: C.faience.band, bulkhead: bulk, piers, pier: C.faience.pier },
      spandrel: spandrel ? { h: spH, surface: sk.accent, balcony: C.spandrel.balcony } : null, fins, share: { lower: sh.lower === null ? null : r3(sh.lower), whole: sh.whole === null ? null : r3(sh.whole), elements, target: sh.lower !== null && sk.family !== 'shed' && fx.reduce((n, x) => n + x.Lp, 0) >= C.minTarget } });
    awnings.push(...myAwnings); banners.push(...myBanners);
  }
  return { skins: out, awnings, banners };
}

/** One wind for the district: pennants fly toward this plan-frame yaw (an onshore breeze off the bay, from the east). */
export const WIND_YAW = r4(Math.PI + 0.35);

/** Flag masts at `pitch` along a line, in the kerb band; `add` places one if it fits (the street layer's keep-clear and walk-zone rules). */
export function flagRow(id: string, kind: Flag['kind'], host: string, a: V, b: V, z: number, add: (at: V, r: number) => boolean): Flag[] {
  const C = COLOUR.flag, L = dist(a, b), u = norm(sub(b, a)), n = Math.floor(L / C.pitch), s0 = (L - (n - 1) * C.pitch) / 2, out: Flag[] = [], r = rng(`flags/${id}`), k0 = r.int(0, C.roles.length - 1);
  for (let k = 0; k < n; k++) {
    const at: V = [a[0] + u[0] * (s0 + k * C.pitch), a[1] + u[1] * (s0 + k * C.pitch)];
    if (!add(at, C.r)) continue;
    out.push({ id: `${id}/${k}`, kind, host, at: [r3(at[0]), r3(at[1])], z: r3(z), h: C.h, mast: 'chrome', pennant: C.pennant, surface: C.roles[(k0 + k) % C.roles.length]!, yaw: WIND_YAW });
  }
  return out;
}
