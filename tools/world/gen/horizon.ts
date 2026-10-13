// Horizon band (grammar §9, brief 007 C hand-off): derived, seeded by hash(plan.id, ring, index). Polar rings round the district centre, leaving the
// sea sector (azimuth within ±60° of +X, plan frame) empty except the far shore; every piece >= 150 m outside the sheet and >= 60 m from the others,
// its top under the ring's elevation limit. Rockets and ring azimuths are in the plan frame (degrees from +X toward +Y); sky bodies use compass azimuth
// (0 = north, 90 = east, "ESE over the bay") as the spec's wording requires. Static; nothing animates.
// The land (brief 007 round 3): a seeded natural coast (a curved bay near the district, a mole, headlands further out), suburban ground with parks and
// tree clumps, a coastal promenade along the shore, and low terraced hills rising toward the far ring. Every ground piece stands on its land or terrace
// height over its whole footprint (never on water, never straddling a terrace edge).
import type { Flat, Horizon, HorizonPiece, Mass } from '../../../contracts/world/world.schema';
import { type V, add, cross, dist, dot, mul, norm, sub } from '../../plan/geom';
import type { Plan } from '../../plan/plan';
import { type Rng, r3, r4, rng } from './common';
import { Grid } from '../../plan/street-geo';
import { adoptedBy } from './kitrefs';
import { ccw, pointInRing } from './geo';

const RAD = Math.PI / 180, HAZE = '#f3e8d4', CENTRE: V = [170, -230];
type Ring = { id: 'A' | 'B' | 'C' | 'shore'; r: [number, number]; count: number; mix: [string, number][]; scale: [number, number]; elev: number; haze: number };
/** Rockets (grammar §9, revised round 3: the B-ring pad at az 135 r 1900 stood behind SpaceHQ in the flyby and read as a white spire inside the district).
 *  Every rocket stands at r >= minR from C, hazed >= haze, and outside ±clear° of the azimuth from C to SpaceHQ and of its opposite. Plan-frame azimuths.
 *  Vela's pads (az 200 r 3600, az 245 r 3400, az 165 r 4200); the second moved to az 235: from the north edge of the sheet (170, 320) az 245 stood 8.6° from
 *  SpaceHQ's axis, inside its silhouette (half-width 9.8°). Tested from the play camera and eight eyes round the sheet (wave2.test.ts). */
export const ROCKETS = {
  pads: [[200, 3600], [235, 3400], [165, 4200]] as [number, number][], ascents: [[210, 3800, 700], [170, 4300, 900]] as [number, number, number][],
  minR: 3200, haze: 0.7, clear: 35, spaceHQ: [96, 0] as V,
};
export const HORIZON_CENTRE = CENTRE;
const SPIRES = ['Horizon_Spire_A', 'Horizon_Spire_B', 'Horizon_Spire_C', 'Horizon_Spire_D'];
const RINGS: Ring[] = [
  { id: 'A', r: [750, 1250], count: 40, mix: [['Horizon_Spire_A', 20], ['Horizon_Spire_B', 20], ['Horizon_Spire_C', 30], ['Horizon_Spire_D', 30]], scale: [0.6, 1.0], elev: 9, haze: 0.15 },
  { id: 'B', r: [1400, 2600], count: 60, mix: [...SPIRES.map((s): [string, number] => [s, 20]), ['Horizon_Massif', 20]], scale: [1.0, 1.5], elev: 7, haze: 0.4 },
  { id: 'C', r: [3000, 5000], count: 80, mix: [['Horizon_Massif', 50], ...SPIRES.map((s): [string, number] => [s, 12.5])], scale: [1.8, 2.6], elev: 5.5, haze: 0.7 },
  { id: 'shore', r: [5500, 8000], count: 24, mix: [['Horizon_Massif', 50], ['Horizon_Spire_B', 25], ['Horizon_Spire_C', 25]], scale: [3, 4], elev: 3, haze: 0.85 },
];
/** Density of the "downtown" cluster by azimuth: three lobes, the strongest NW (135°). */
const lobes = (az: number) => 1 + 2.5 * [[135, 40, 1], [200, 30, 0.5], [75, 25, 0.4]].reduce((s, [c, w, a]) => s + a! * Math.exp(-((((az - c! + 540) % 360) - 180) ** 2) / (2 * w! * w!)), 0);
const inSea = (az: number) => Math.abs((((az + 540) % 360) - 180)) <= 60; // plan-frame azimuth within ±60° of +X
const rectGap = (p: V, x: V, y: V) => Math.hypot(Math.max(x[0] - p[0], 0, p[0] - x[1]), Math.max(y[0] - p[1], 0, p[1] - y[1]));
const smooth = (x: number) => { const t = Math.min(1, Math.max(0, x)); return t * t * (3 - 2 * t); };
const segDist = (p: V, a: V, b: V) => { const ab = sub(b, a), t = Math.min(1, Math.max(0, dot(sub(p, a), ab) / dot(ab, ab))); return dist(p, add(a, mul(ab, t))); };
const lineDist = (p: V, pts: V[]) => { let m = Infinity; for (let i = 1; i < pts.length; i++) m = Math.min(m, segDist(p, pts[i - 1]!, pts[i]!)); return m; };

/** Surface roles of the land beyond the district (Vela colours them apart from the haze; Latch draws them). The promenade reuses the district quay. */
export const LAND_ROLES = { ground: 'beyond_ground', green: 'beyond_green', hill: 'beyond_hill', promenade: 'quay' } as const;
/** Coast shape: the bay bows at most BAY_MAX m into the land (the sector's ±60° edge is ~218 m away, so the sea never leaves it); headlands reach into the sea. */
const COAST = { bay: [900, 1400], bayDepth: [110, 160], BAY_MAX: 175, headlands: 3, head: [220, 650], sigma: [220, 420], wiggle: [35, 20] } as const;
const MOLE = { at: [0.35, 0.5], heading: [72, 82], length: [320, 420], halfWidth: 11, bend: [10, 20], segs: 8 } as const;
const PROMENADE = { setback: 1.5, width: 18, beyondBay: 300, step: 20 } as const;
const HILLS = { count: 30, r: [2200, 7600], R: [300, 750], tiers: [3, 5], H: [25, 135], elev: 1.8, top: 0.38 } as const;
const GREEN = { parks: 60, parkR: [60, 220], trees: 40, treeR: [35, 110], r: [450, 4500] } as const;
/** Overlay heights above the land plane: parks and tree clumps, the promenade (both drawn over the ground, never under a piece's base). */
const OVER = { green: 0.1, promenade: 0.06 } as const;

type CoastLine = { pts: V[]; at: (s: number) => V; L: number; bay: number; mole?: V[] };
/** One coast from the plan-water corner O to the far-shore point P: a graph over the straight ray O-P (offset along its sea normal), so it never crosses
 *  itself; the bay (s < bay) bows into the land, headlands push into the sea, a low wiggle between; tapered to 0 at both ends so it meets O and P. */
function coastLine(id: string, O: V, P: V, north: boolean, cy: number, mole: boolean): CoastLine {
  const r = rng(id), L = dist(O, P), u = norm(sub(P, O)), nS: V = north ? [u[1], -u[0]] : [-u[1], u[0]];
  const span = (a: readonly [number, number]) => a[0] + r.f() * (a[1] - a[0]);
  const bay = span(COAST.bay), depth = span(COAST.bayDepth), from = bay + 350, to = L - 600;
  const heads = Array.from({ length: COAST.headlands }, (_, j) => ({ c: from + ((j + 0.2 + 0.6 * r.f()) / COAST.headlands) * (to - from), A: span(COAST.head), sg: span(COAST.sigma) }));
  const l1 = 500 + 400 * r.f(), p1 = r.f() * 2 * Math.PI, l2 = 230 + 150 * r.f(), p2 = r.f() * 2 * Math.PI;
  const off = (s: number) => {
    let o = (s > 0 && s < bay ? -depth * Math.sin((Math.PI * s) / bay) : 0) + heads.reduce((n, h) => n + h.A * Math.exp(-((s - h.c) ** 2) / (2 * h.sg * h.sg)), 0);
    o += smooth((s - 0.5 * bay) / 400) * (COAST.wiggle[0] * Math.sin((2 * Math.PI * s) / l1 + p1) + COAST.wiggle[1] * Math.sin((2 * Math.PI * s) / l2 + p2));
    const toMid = Math.abs(O[1] + u[1] * s - cy), cap = (0.6 * toMid) / Math.abs(nS[1]); // stays on its own side of the bay's midline
    return smooth(s / 80) * smooth((L - s) / 400) * Math.min(cap, Math.max(-COAST.BAY_MAX, o));
  };
  const at = (s: number): V => (s <= 0 ? O : s >= L ? P : [r3(O[0] + u[0] * s + nS[0] * off(s)), r3(O[1] + u[1] * s + nS[1] * off(s))]);
  const ss: number[] = [];
  for (let s = 0; s < L; s += s < 1500 ? 25 : s < 3000 ? 60 : 120) ss.push(s);
  ss.push(L);
  if (!mole) return { pts: ss.map(at), at, L, bay };
  // the mole: a breakwater arm leaving the coast in the bay, heading north-north-east and bending toward the harbour mouth, with a rounded head
  const sm = bay * (MOLE.at[0] + r.f() * (MOLE.at[1] - MOLE.at[0])), M = at(sm), th = span(MOLE.heading), Lm = span(MOLE.length), bend = -span(MOLE.bend), hw = MOLE.halfWidth;
  const tan = norm(sub(at(sm + 1), at(sm - 1))), d0: V = [Math.cos(th * RAD), Math.sin(th * RAD)], sinA = Math.abs(cross(tan, d0)), sA = sm - hw / sinA, sB = sm + hw / sinA;
  const side = dot([-d0[1], d0[0]], tan) < 0 ? 1 : -1; // rotate +90 (1) or -90 (-1) to reach the A side (toward smaller s)
  const cl: { c: V; d: V }[] = []; let c = M;
  for (let k = 1; k <= MOLE.segs; k++) { const a = (th + (bend * k) / MOLE.segs) * RAD, d: V = [Math.cos(a), Math.sin(a)]; c = add(c, mul(d, Lm / MOLE.segs)); cl.push({ c, d }); }
  const pA = (d: V): V => (side > 0 ? [-d[1], d[0]] : [d[1], -d[0]]), last = cl[cl.length - 1]!;
  const edgeA = cl.map(k => add(k.c, mul(pA(k.d), hw))), edgeB = cl.map(k => add(k.c, mul(pA(k.d), -hw))).reverse();
  const head = [1, 2, 3, 4, 5].map(j => add(last.c, add(mul(pA(last.d), hw * Math.cos((Math.PI * j) / 6)), mul(last.d, hw * Math.sin((Math.PI * j) / 6)))));
  const arm = [at(sA), ...edgeA, ...head, ...edgeB, at(sB)].map((p): V => [r3(p[0]), r3(p[1])]);
  return { pts: [...ss.filter(s => s < sA).map(at), ...arm, ...ss.filter(s => s > sB).map(at)], at, L, bay, mole: arm };
}

/** A star-shaped blob of radius R about c (two seeded harmonics), n vertices, CCW; `rmax` bounds it. */
function blob(r: Rng, c: V, R: number, n: number, wob: number) {
  const a2 = r.f() * wob, a3 = r.f() * wob * 0.6, p2 = r.f() * 2 * Math.PI, p3 = r.f() * 2 * Math.PI;
  const at = (k: number): V[] => Array.from({ length: n }, (_, i) => { const t = (2 * Math.PI * i) / n, rr = k * R * (1 + a2 * Math.cos(2 * t + p2) + a3 * Math.cos(3 * t + p3)); return [r3(c[0] + rr * Math.cos(t)), r3(c[1] + rr * Math.sin(t))]; });
  return { at, rmax: R * (1 + a2 + a3) };
}

/** Land and sea of the horizon: the visual sea is the bay only; everything else outside the sheet is land at the district edge's ground level (surface
 *  beyond_ground). The coasts leave the plan water's corners at the district edge ((sheet.x[1], sheet.y[1]) and (sheet.x[1], sheet.y[0])), shaped by
 *  coastLine round the bay sector's ±60° rays, out to the far shore, an arc r SHORE_R0 about C; the sea lies between them. Land = west (beyond sheet.x[0]),
 *  north and south (each reaching round the bay to the far shore and meeting on y = C.y east of it). The pieces partition the square (no overlap, no
 *  gap), so nothing z-fights. Banks: thin walls on the land side of every land/sea edge from bankBase to just under the land plane.
 *  Over the land: terraced hills (beyond_hill prisms), parks and tree clumps (beyond_green flats), the coastal promenade and the mole top (quay flats). */
const SHORE_R0 = 5000, ARC_STEP = 3;
export function landSea(plan: Plan, sea: { centre: V; size: number }, land: { level: number; bankBase: number; bankDepth: number }, keepClear: { at: V; r: number }[] = []) {
  const [cx, cy] = sea.centre, h = sea.size / 2, x0 = r3(cx - h), x1 = r3(cx + h), y0 = r3(cy - h), y1 = r3(cy + h), [sx0, sx1] = plan.sheet.x, [sy0, sy1] = plan.sheet.y;
  // where the straight coast ray from a plan-water corner at azimuth a (plan frame) meets the far-shore circle
  const meet = (o: V, a: number): V => {
    const u: V = [Math.cos(a * RAD), Math.sin(a * RAD)], w: V = [o[0] - cx, o[1] - cy], b = u[0] * w[0] + u[1] * w[1], c = w[0] * w[0] + w[1] * w[1] - SHORE_R0 * SHORE_R0;
    const t = -b + Math.sqrt(b * b - c); return [r3(o[0] + u[0] * t), r3(o[1] + u[1] * t)];
  };
  const NC: V = [sx1, sy1], SC: V = [sx1, sy0], PN = meet(NC, 60), PS = meet(SC, -60);
  const azOf = (p: V) => Math.atan2(p[1] - cy, p[0] - cx) / RAD, aN = azOf(PN), aS = azOf(PS);
  const arc = (from: number, to: number): V[] => { // arc points strictly between `from` and `to` (degrees), stepping ARC_STEP
    const n = Math.max(1, Math.ceil(Math.abs(to - from) / ARC_STEP)), out: V[] = [];
    for (let k = 1; k < n; k++) { const a = (from + ((to - from) * k) / n) * RAD; out.push([r3(cx + SHORE_R0 * Math.cos(a)), r3(cy + SHORE_R0 * Math.sin(a))]); }
    return out;
  };
  const cN = coastLine(`${plan.id}/horizon/coast/north`, NC, PN, true, cy, false), cS = coastLine(`${plan.id}/horizon/coast/south`, SC, PS, false, cy, true);
  const nIn = cN.pts.slice(1, -1), sIn = cS.pts.slice(1, -1); // the shaped coast between its ends (NC/SC and PN/PS)
  const E0: V = [r3(cx + SHORE_R0), r3(cy)];
  const seaRing: V[] = [SC, ...sIn, PS, ...arc(aS, aN), PN, ...[...nIn].reverse(), NC]; // CCW: out along the south coast, round the far shore, back along the north coast
  const landRings: [string, V[]][] = [
    ['west', [[x0, y0], [sx0, y0], [sx0, y1], [x0, y1]]],
    ['north', [[sx0, sy1], NC, ...nIn, PN, ...arc(aN, 0), E0, [x1, r3(cy)], [x1, y1], [sx0, y1]]],
    ['south', [[sx0, y0], [x1, y0], [x1, r3(cy)], E0, ...arc(0, aS), PS, ...[...sIn].reverse(), SC, [sx0, sy0]]],
  ];
  for (const [n, rg] of [['sea', seaRing], ...landRings] as [string, V[]][]) if (!simple(rg)) throw new Error(`horizon: ${n} ring crosses itself`);
  const flat = (id: string, surface: string, ring: V[], level: number): Flat => ({ id, surface, ring, level });
  // a bank on the land side of edge a-b (the side away from the sea ring)
  const wall = (id: string, a: V, b: V): Mass => {
    const dx = b[0] - a[0], dy = b[1] - a[1], L = Math.hypot(dx, dy), m: V = [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2];
    let nx = (-dy / L) * land.bankDepth, ny = (dx / L) * land.bankDepth;
    if (pointInRing([m[0] + nx * 0.25, m[1] + ny * 0.25], seaRing) || pointInRing([m[0] + nx * 0.25, m[1] + ny * 0.25], plan.water[0]!)) { nx = -nx; ny = -ny; }
    return { id, surface: 'quay', ring: [a, b, [r3(b[0] + nx), r3(b[1] + ny)], [r3(a[0] + nx), r3(a[1] + ny)]], base: land.bankBase, height: r3(land.level - 0.02 - land.bankBase) };
  };
  const wx = Math.min(...plan.water.flat().map(p => p[0])), far = [PS, ...arc(aS, aN), PN], edges = (n: string, pts: V[]) => pts.slice(1).map((p, i) => wall(`horizon/bank/${n}-${i}`, pts[i]!, p));
  const banks = [wall('horizon/bank/north-bay', [wx, sy1], NC), wall('horizon/bank/south-bay', [wx, sy0], SC), ...edges('north-coast', cN.pts), ...edges('south-coast', cS.pts), ...edges('far-shore', far)];

  // over the land: what keeps clear of the coast (the sea, the banks, the promenade) and of the district
  const shoreLines = [cN.pts, cS.pts, far], toShore = (p: V) => Math.min(...shoreLines.map(l => lineDist(p, l)));
  const wet = (p: V) => pointInRing(p, seaRing), inSquare = (p: V, m: number) => Math.abs(p[0] - cx) <= h - m && Math.abs(p[1] - cy) <= h - m;
  const at = (az: number, r: number): V => [cx + Math.cos(az * RAD) * r, cy + Math.sin(az * RAD) * r];
  // terraced hills: rising toward the far ring, nested tiers about one centre (a tier's base sits inside the tier below, so no riser floats)
  const hills: { c: V; rmax: number; tiers: { ring: V[]; top: number }[] }[] = [], hillMasses: Mass[] = [];
  for (let i = 0; i < HILLS.count; i++) {
    const r = rng(`${plan.id}/horizon/hill/${i}`);
    for (let tries = 0; tries < 60; tries++) {
      const rr = HILLS.r[0] + r.f() * (HILLS.r[1] - HILLS.r[0]), k = (rr - HILLS.r[0]) / (HILLS.r[1] - HILLS.r[0]), c = at(r.f() * 360, rr);
      const b = blob(r, c, (HILLS.R[0] + k * (HILLS.R[1] - HILLS.R[0])) * (0.8 + 0.4 * r.f()), 16, 0.16), m = b.rmax;
      if (!inSquare(c, m + 50) || wet(c) || toShore(c) < m * 1.1 + 150 || rectGap(c, plan.sheet.x, plan.sheet.y) < m + 600) continue;
      if (hills.some(o => dist(o.c, c) < m + o.rmax + 80) || keepClear.some(o => dist(o.at, c) < m + o.r + 60)) continue;
      const n = r.int(HILLS.tiers[0], HILLS.tiers[1]), H = Math.min((HILLS.H[0] + k * (HILLS.H[1] - HILLS.H[0])) * (0.85 + 0.3 * r.f()), Math.tan(HILLS.elev * RAD) * rr);
      const tiers = Array.from({ length: n }, (_, t) => ({ ring: ccw(b.at(1 - (t * (1 - HILLS.top)) / (n - 1))), top: r3(land.level + (H * (t + 1)) / n) }));
      tiers.forEach((t, j) => { const base = j === 0 ? r3(land.level - 1) : r3(tiers[j - 1]!.top - 0.5); hillMasses.push({ id: `horizon/hill/${i}/tier-${j}`, surface: LAND_ROLES.hill, ring: t.ring, base, height: r3(t.top - base) }); });
      hills.push({ c, rmax: m, tiers }); break;
    }
  }
  /** Ground height at p: the top of the highest terrace it stands on, else the land plane (overlays excluded: they sit above it). */
  const terrain = (p: V) => {
    for (const hl of hills) { if (dist(p, hl.c) > hl.rmax + 1) continue; for (let t = hl.tiers.length - 1; t >= 0; t--) if (pointInRing(p, hl.tiers[t]!.ring)) return hl.tiers[t]!.top; }
    return land.level;
  };
  /** The centre of the hill under p, if any (pieces that land on a terrace edge try its summit instead). */
  const hillAt = (p: V): V | null => hills.find(hl => dist(p, hl.c) <= hl.rmax + 1 && pointInRing(p, hl.tiers[0]!.ring))?.c ?? null;
  // the coastal promenade: a band on the land side of each coast, from the district edge round the bay (the smooth coast: it passes behind the mole's root)
  const band = (n: string, cl: CoastLine, north: boolean): Flat => {
    const inner: V[] = [], outer: V[] = [], end = Math.min(cl.L - 400, cl.bay + PROMENADE.beyondBay);
    for (let s = 0; s <= end + 1e-6; s += PROMENADE.step) {
      const t = norm(sub(cl.at(s + 1), cl.at(s - 1))), nL: V = north ? [-t[1], t[0]] : [t[1], -t[0]], p = cl.at(s);
      inner.push([r3(p[0] + nL[0] * PROMENADE.setback), r3(p[1] + nL[1] * PROMENADE.setback)]);
      outer.push([r3(p[0] + nL[0] * (PROMENADE.setback + PROMENADE.width)), r3(p[1] + nL[1] * (PROMENADE.setback + PROMENADE.width))]);
    }
    return flat(`horizon/promenade/${n}`, LAND_ROLES.promenade, ccw([...inner, ...outer.reverse()]), r3(land.level + OVER.promenade));
  };
  const patches: Flat[] = [band('north', cN, true), band('south', cS, false)];
  if (cS.mole) patches.push(flat('horizon/promenade/mole', LAND_ROLES.promenade, ccw(cS.mole), r3(land.level + OVER.promenade)));
  // parks and tree clumps on the flat suburban ground: clear of the shore and promenade, the district, the hills and each other
  const greens: { c: V; rmax: number }[] = [];
  const green = (kind: 'park' | 'trees', count: number, R: readonly [number, number]) => {
    for (let i = 0; i < count; i++) {
      const r = rng(`${plan.id}/horizon/${kind}/${i}`);
      for (let tries = 0; tries < 40; tries++) {
        const rr = GREEN.r[0] + r.f() * (GREEN.r[1] - GREEN.r[0]), c = at(r.f() * 360, rr), b = blob(r, c, R[0] + r.f() * (R[1] - R[0]), kind === 'park' ? 12 : 10, 0.25), m = b.rmax;
        if (wet(c) || toShore(c) < m + PROMENADE.setback + PROMENADE.width + 20 || rectGap(c, plan.sheet.x, plan.sheet.y) < m + 40) continue;
        if (hills.some(o => dist(o.c, c) < m + o.rmax + 30) || greens.some(o => dist(o.c, c) < m + o.rmax + 20)) continue;
        greens.push({ c, rmax: m }); patches.push(flat(`horizon/${kind}/${i}`, LAND_ROLES.green, ccw(b.at(1)), r3(land.level + OVER.green))); break;
      }
    }
  };
  green('park', GREEN.parks, GREEN.parkR); green('trees', GREEN.trees, GREEN.treeR);
  return {
    sectors: [flat('horizon/sea/bay', 'water', seaRing, 0)], land: landRings.map(([n, r]) => flat(`horizon/land/${n}`, LAND_ROLES.ground, r, land.level)), banks, patches, hills: hillMasses, terrain, wet, hillAt,
    coast: { north: cN.pts, south: cS.pts, mole: cS.mole ?? [], shoreR: SHORE_R0 },
  };
}

/** True if no two non-adjacent edges of the closed ring cross. */
export function simple(r: V[]): boolean {
  const n = r.length, o = (a: V, b: V, c: V) => Math.sign(cross(sub(b, a), sub(c, a)));
  const box = r.map((a, i) => { const b = r[(i + 1) % n]!; return [Math.min(a[0], b[0]), Math.max(a[0], b[0]), Math.min(a[1], b[1]), Math.max(a[1], b[1])]; });
  for (let i = 0; i < n; i++) for (let j = i + 2; j < n; j++) {
    if (i === 0 && j === n - 1) continue;
    const A = box[i]!, B = box[j]!; if (A[0] > B[1] || B[0] > A[1] || A[2] > B[3] || B[2] > A[3]) continue;
    const a = r[i]!, b = r[(i + 1) % n]!, c = r[j]!, d = r[(j + 1) % n]!;
    if (o(a, b, c) * o(a, b, d) < 0 && o(c, d, a) * o(c, d, b) < 0) return false;
  }
  return true;
}

/** Points a piece of footprint radius `rad` at p covers: its centre, a ring at rad and one at rad / 2. */
export const footprint = (p: V, rad: number): V[] => [p, ...Array.from({ length: 12 }, (_, i): V => [p[0] + rad * Math.cos((i * Math.PI) / 6), p[1] + rad * Math.sin((i * Math.PI) / 6)]), ...Array.from({ length: 6 }, (_, i): V => [p[0] + (rad / 2) * Math.cos((i * Math.PI) / 3), p[1] + (rad / 2) * Math.sin((i * Math.PI) / 3)])];
/** Footprint radius of a kit piece at a uniform scale (its plan diagonal / 2: any yaw). */
const radiusOf = (id: string, scale: number) => { const s = adoptedBy(id)!.size; return (Math.hypot(s[0], s[2]) / 2) * scale; };

export function horizonOf(plan: Plan, level: number, land: { level: number; bankBase: number; bankDepth: number } = { level: -0.02, bankBase: -2.5, bankDepth: 4 }): Horizon {
  const pieces: HorizonPiece[] = [], grid = new Grid<{ at: V }>(100, o => o.at), size = (id: string) => adoptedBy(id)!.size;
  const at = (az: number, r: number): V => [CENTRE[0] + Math.cos(az * RAD) * r, CENTRE[1] + Math.sin(az * RAD) * r];
  // rockets (grammar §9 round 3): pads (plan-frame azimuth, ring radius) and ascents (altitude, leaning 6° downwind = away from the centre); the hills keep clear of the pads
  const { pads, ascents: asc } = ROCKETS, padScale = (rr: number) => (rr > 3000 ? 2.2 : 1.5);
  const ls = landSea(plan, { centre: CENTRE, size: 16000 }, land, pads.map(([az, rr]) => ({ at: at(az, rr), r: radiusOf('Horizon_RocketPad', padScale(rr)) })));
  /** The ground under the whole footprint: one height (never across a terrace edge), never water. Null if it does not hold. */
  const ground = (p: V, rad: number) => { const hs = footprint(p, rad).map(q => (ls.wet(q) ? NaN : ls.terrain(q))); return hs.every(z => z === hs[0]) ? hs[0]! : null; };
  const free = (p: V) => grid.near(p, 60).every(o => Math.hypot(o.at[0] - p[0], o.at[1] - p[1]) >= 60) && rectGap(p, plan.sheet.x, plan.sheet.y) >= 150;
  const place = (ring: HorizonPiece['ring'], i: number, id: string, p: V, scale: number, haze: number, r: Rng, base: number, extra: Partial<HorizonPiece> = {}) => {
    pieces.push({ id: `horizon/${ring}/${i}`, ring, kit: { id, scale: [r3(scale), r3(scale), r3(scale)], yaw: r4(r.f() * Math.PI * 2), at: [r3(p[0]), r3(p[1])] }, base, haze, ...extra });
    grid.add({ at: p });
  };
  pads.forEach(([az, rr], i) => {
    const r = rng(`${plan.id}/horizon/pad/${i}`), p = at(az, rr), z = ground(p, radiusOf('Horizon_RocketPad', padScale(rr)));
    if (z === null) throw new Error(`horizon: rocket pad ${i} does not stand on one ground height`);
    place('rocket-pad', i, 'Horizon_RocketPad', p, padScale(rr), ROCKETS.haze, r, z);
  });
  asc.forEach(([az, rr, alt], i) => { const r = rng(`${plan.id}/horizon/ascent/${i}`); place('rocket-ascent', i, 'Horizon_RocketAscent', at(az, rr), rr > 3000 ? 2 : 1.2, ROCKETS.haze, r, alt, { tilt: { deg: 6, yaw: r4(az * RAD) } }); });
  for (const R of RINGS) {
    let clump: V = [0, 0], left = 0;
    for (let i = 0; i < R.count; i++) {
      const r = rng(`${plan.id}/horizon/${R.id}/${i}`);
      for (let tries = 0; tries < 40; tries++) {
        let p: V;
        if (R.id === 'shore') { // clumps of 3-5 in the sea sector
          if (left === 0) { const az = (r.f() * 2 - 1) * 55, rr = R.r[0] + r.f() * (R.r[1] - R.r[0]); clump = at(az, rr); left = r.int(3, 5); }
          p = [clump[0] + (r.f() * 2 - 1) * 260, clump[1] + (r.f() * 2 - 1) * 260];
          const az = Math.atan2(p[1] - CENTRE[1], p[0] - CENTRE[0]) / RAD; if (!inSea(az)) continue;
          if (Math.max(Math.abs(p[0] - CENTRE[0]), Math.abs(p[1] - CENTRE[1])) > 8000 - 400) continue; // on the far-shore land, inside the horizon square
        } else {
          let az = 0; for (let k = 0; k < 20; k++) { az = r.f() * 360; if (r.f() * 3.5 < lobes(az)) break; }
          if (inSea(az)) continue; // the bay stays empty
          p = at(az, R.r[0] + r.f() * (R.r[1] - R.r[0]));
        }
        const id = r.weighted(R.mix, m => m[1])[0], h = size(id)[1];
        if (ls.wet(p)) continue;
        const summit = ls.hillAt(p); if (summit && !ground(p, radiusOf(id, R.scale[0]))) p = [summit[0], summit[1]]; // on a hillside: try the summit
        const az1 = Math.atan2(p[1] - CENTRE[1], p[0] - CENTRE[0]) / RAD; if (inSea(az1) !== (R.id === 'shore')) continue; // the summit keeps the ring's sector
        const dist = Math.hypot(p[0] - CENTRE[0], p[1] - CENTRE[1]);
        const lift = ls.terrain(p) - land.level; // a terrace raises the piece: its top still keeps under the ring's elevation limit
        const smax = Math.min(R.scale[1], (Math.tan(R.elev * RAD) * dist - lift) / h);
        // the far shore's scale 3-4 and its 3° limit cannot both hold for 190 m massifs (5.9° at 5.5 km): the elevation limit wins, down to scale 1.0
        const lo = R.id === 'shore' ? Math.min(R.scale[0], smax) : R.scale[0];
        if (smax < lo || smax < (R.id === 'shore' ? 1 : 0)) continue; // too tall for this distance at the ring's smallest scale: draw again
        if (!free(p)) continue;
        const scale = lo + r.f() * (smax - lo), z = ground(p, radiusOf(id, scale));
        if (z === null) continue; // straddles a terrace edge or the shore: draw again
        place(R.id, i, id, p, scale, R.haze, r, z);
        left = Math.max(0, left - 1); break;
      }
    }
  }
  // planet and moons: compass azimuth + elevation at distance 6000 from the centre, hazed 25%
  const sky = (id: string, ring: 'planet' | 'moon', i: number, az: number, el: number, scale: number) => {
    const d = 6000, h = d * Math.cos(el * RAD), a = az * RAD, r = rng(`${plan.id}/horizon/${ring}/${i}`);
    place(ring, i, id, [CENTRE[0] + Math.sin(a) * h, CENTRE[1] + Math.cos(a) * h], scale, 0.25, r, r3(d * Math.sin(el * RAD)));
  };
  sky('Horizon_PlanetRinged', 'planet', 0, 110, 24, 4.8); sky('Horizon_Moon', 'moon', 0, 150, 36, 0.9); sky('Horizon_Moon', 'moon', 1, 60, 48, 0.7);
  const sectors = ls.sectors.map(f => ({ ...f, level: r3(level - 0.05) }));
  return { centre: CENTRE, haze: HAZE, sea: { surface: 'water', centre: CENTRE, size: 16000, level: r3(level - 0.05), fadeFrom: 4000, sectors }, land: ls.land, patches: ls.patches, hills: ls.hills, banks: ls.banks, pieces };
}
