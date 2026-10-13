// Horizon band (grammar §9, brief 007 C hand-off): derived, seeded by hash(plan.id, ring, index). Polar rings round the district centre, leaving the
// sea sector (azimuth within ±60° of +X, plan frame) empty except the far shore; every piece >= 150 m outside the sheet and >= 60 m from the others,
// its top under the ring's elevation limit. Rockets and ring azimuths are in the plan frame (degrees from +X toward +Y); sky bodies use compass azimuth
// (0 = north, 90 = east, "ESE over the bay") as the spec's wording requires. Static; nothing animates.
import type { Flat, Horizon, HorizonPiece, Mass } from '../../../contracts/world/world.schema';
import type { V } from '../../plan/geom';
import type { Plan } from '../../plan/plan';
import { type Rng, r3, r4, rng } from './common';
import { Grid } from '../../plan/street-geo';
import { adoptedBy } from './kitrefs';
import { pointInRing } from './geo';

const RAD = Math.PI / 180, HAZE = '#f3e8d4', CENTRE: V = [170, -230];
type Ring = { id: 'A' | 'B' | 'C' | 'shore'; r: [number, number]; count: number; mix: [string, number][]; scale: [number, number]; elev: number; haze: number };
/** Ascent haze mix (near, far): lighter than the rings' 0.4 / 0.7 so the plumes read (Vela, brief 007 round 2). */
const ASCENT_HAZE = [0.25, 0.45] as const;
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

/** Land and sea of the horizon (brief 007 round 2): the visual sea is the bay sector only; everything else outside the sheet is land at the district
 *  edge's ground level (surface 'beyond', the plan's own land beyond the district). The coast leaves the plan water's corners at the district edge
 *  ((sheet.x[1], sheet.y[1]) and (sheet.x[1], sheet.y[0])) along the bay sector's +-60° directions, out to the far shore, an arc r SHORE_R0 about C;
 *  the sea is the sector between them. Land = west (beyond sheet.x[0]), north and south (each reaching round the bay to the far shore and meeting on
 *  y = C.y east of it). The pieces partition the square (no overlap, no gap), so nothing z-fights; the inland rings (A-C, pads) all stand on land.
 *  Banks: thin walls on the land side of every land/sea edge from bankBase to just under the land plane, so the step down to the sea is a face. */
const SEA_HALF = 60, SHORE_R0 = 5000, ARC_STEP = 3;
export function landSea(plan: Plan, sea: { centre: V; size: number }, land: { level: number; bankBase: number; bankDepth: number }) {
  const [cx, cy] = sea.centre, h = sea.size / 2, x0 = r3(cx - h), x1 = r3(cx + h), y0 = r3(cy - h), y1 = r3(cy + h), [sx0, sx1] = plan.sheet.x, [sy0, sy1] = plan.sheet.y;
  // where the coast ray from a plan-water corner at azimuth a (plan frame) meets the far-shore circle
  const meet = (o: V, a: number): V => {
    const u: V = [Math.cos(a * RAD), Math.sin(a * RAD)], w: V = [o[0] - cx, o[1] - cy], b = u[0] * w[0] + u[1] * w[1], c = w[0] * w[0] + w[1] * w[1] - SHORE_R0 * SHORE_R0;
    const t = -b + Math.sqrt(b * b - c); return [r3(o[0] + u[0] * t), r3(o[1] + u[1] * t)];
  };
  const NC: V = [sx1, sy1], SC: V = [sx1, sy0], PN = meet(NC, SEA_HALF), PS = meet(SC, -SEA_HALF);
  const azOf = (p: V) => Math.atan2(p[1] - cy, p[0] - cx) / RAD, aN = azOf(PN), aS = azOf(PS);
  const arc = (from: number, to: number): V[] => { // arc points strictly between `from` and `to` (degrees), stepping ARC_STEP
    const n = Math.max(1, Math.ceil(Math.abs(to - from) / ARC_STEP)), out: V[] = [];
    for (let k = 1; k < n; k++) { const a = (from + ((to - from) * k) / n) * RAD; out.push([r3(cx + SHORE_R0 * Math.cos(a)), r3(cy + SHORE_R0 * Math.sin(a))]); }
    return out;
  };
  const E0: V = [r3(cx + SHORE_R0), r3(cy)];
  const seaRing: V[] = [SC, PS, ...arc(aS, aN), PN, NC]; // CCW: out along the south coast, round the far shore, back along the north coast, down the district edge
  const landRings: [string, V[]][] = [
    ['west', [[x0, y0], [sx0, y0], [sx0, y1], [x0, y1]]],
    ['north', [[sx0, sy1], NC, PN, ...arc(aN, 0), E0, [x1, r3(cy)], [x1, y1], [sx0, y1]]],
    ['south', [[sx0, y0], [x1, y0], [x1, r3(cy)], E0, ...arc(0, aS), PS, SC, [sx0, sy0]]],
  ];
  const flat = (id: string, surface: string, ring: V[], level: number): Flat => ({ id, surface, ring, level });
  // a bank on the land side of edge a-b (the side away from the sea ring)
  const wall = (id: string, a: V, b: V): Mass => {
    const dx = b[0] - a[0], dy = b[1] - a[1], L = Math.hypot(dx, dy), m: V = [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2];
    let nx = (-dy / L) * land.bankDepth, ny = (dx / L) * land.bankDepth;
    if (pointInRing([m[0] + nx * 0.25, m[1] + ny * 0.25], seaRing) || pointInRing([m[0] + nx * 0.25, m[1] + ny * 0.25], plan.water[0]!)) { nx = -nx; ny = -ny; }
    return { id, surface: 'quay', ring: [a, b, [r3(b[0] + nx), r3(b[1] + ny)], [r3(a[0] + nx), r3(a[1] + ny)]], base: land.bankBase, height: r3(land.level - 0.02 - land.bankBase) };
  };
  const wx = Math.min(...plan.water.flat().map(p => p[0])), shore = [PS, ...arc(aS, aN), PN];
  const banks = [wall('horizon/bank/north-bay', [wx, sy1], NC), wall('horizon/bank/north-coast', NC, PN), wall('horizon/bank/south-bay', [wx, sy0], SC), wall('horizon/bank/south-coast', SC, PS),
    ...shore.slice(1).map((p, i) => wall(`horizon/bank/far-shore-${i}`, shore[i]!, p))];
  return { sectors: [flat('horizon/sea/bay', 'water', seaRing, 0)], land: landRings.map(([n, r]) => flat(`horizon/land/${n}`, 'beyond', r, land.level)), banks, coast: { north: [NC, PN] as [V, V], south: [SC, PS] as [V, V], shoreR: SHORE_R0 } };
}

export function horizonOf(plan: Plan, level: number, land: { level: number; bankBase: number; bankDepth: number } = { level: -0.02, bankBase: -2.5, bankDepth: 4 }): Horizon {
  const pieces: HorizonPiece[] = [], grid = new Grid<{ at: V }>(100, o => o.at), size = (id: string) => adoptedBy(id)!.size;
  const free = (p: V) => grid.near(p, 60).every(o => Math.hypot(o.at[0] - p[0], o.at[1] - p[1]) >= 60) && rectGap(p, plan.sheet.x, plan.sheet.y) >= 150;
  const at = (az: number, r: number): V => [CENTRE[0] + Math.cos(az * RAD) * r, CENTRE[1] + Math.sin(az * RAD) * r];
  const place = (ring: HorizonPiece['ring'], i: number, id: string, p: V, scale: number, haze: number, r: Rng, extra: Partial<HorizonPiece> = {}) => {
    pieces.push({ id: `horizon/${ring}/${i}`, ring, kit: { id, scale: [r3(scale), r3(scale), r3(scale)], yaw: r4(r.f() * Math.PI * 2), at: [r3(p[0]), r3(p[1])] }, base: 0, haze, ...extra });
    grid.add({ at: p });
  };
  // rockets: pads (plan-frame azimuth, ring radius) and ascents (altitude, leaning 6° downwind = away from the centre)
  const pads: [number, number][] = [[135, 1900], [255, 2300], [190, 3600]], asc: [number, number, number][] = [[140, 2300, 450], [200, 3700, 800]];
  pads.forEach(([az, rr], i) => { const r = rng(`${plan.id}/horizon/pad/${i}`); place('rocket-pad', i, 'Horizon_RocketPad', at(az, rr), rr > 3000 ? 2.2 : 1.5, rr > 3000 ? 0.7 : 0.4, r); });
  asc.forEach(([az, rr, alt], i) => { const r = rng(`${plan.id}/horizon/ascent/${i}`); place('rocket-ascent', i, 'Horizon_RocketAscent', at(az, rr), rr > 3000 ? 2 : 1.2, rr > 3000 ? ASCENT_HAZE[1] : ASCENT_HAZE[0], r, { base: alt, tilt: { deg: 6, yaw: r4(az * RAD) } }); });
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
        const id = r.weighted(R.mix, m => m[1])[0], dist = Math.hypot(p[0] - CENTRE[0], p[1] - CENTRE[1]), h = size(id)[1];
        const smax = Math.min(R.scale[1], (Math.tan(R.elev * RAD) * dist) / h);
        // the far shore's scale 3-4 and its 3° limit cannot both hold for 190 m massifs (5.9° at 5.5 km): the elevation limit wins, down to scale 1.0
        const lo = R.id === 'shore' ? Math.min(R.scale[0], smax) : R.scale[0];
        if (smax < lo || smax < (R.id === 'shore' ? 1 : 0)) continue; // too tall for this distance at the ring's smallest scale: draw again
        if (!free(p)) continue;
        place(R.id, i, id, p, lo + r.f() * (smax - lo), R.haze, r);
        left = Math.max(0, left - 1); break;
      }
    }
  }
  // planet and moons: compass azimuth + elevation at distance 6000 from the centre, hazed 25%
  const sky = (id: string, ring: 'planet' | 'moon', i: number, az: number, el: number, scale: number) => {
    const d = 6000, h = d * Math.cos(el * RAD), a = az * RAD, r = rng(`${plan.id}/horizon/${ring}/${i}`);
    place(ring, i, id, [CENTRE[0] + Math.sin(a) * h, CENTRE[1] + Math.cos(a) * h], scale, 0.25, r, { base: r3(d * Math.sin(el * RAD)) });
  };
  sky('Horizon_PlanetRinged', 'planet', 0, 110, 24, 4.8); sky('Horizon_Moon', 'moon', 0, 150, 36, 0.9); sky('Horizon_Moon', 'moon', 1, 60, 48, 0.7);
  const ls = landSea(plan, { centre: CENTRE, size: 16000 }, land), sectors = ls.sectors.map(f => ({ ...f, level: r3(level - 0.05) }));
  return { centre: CENTRE, haze: HAZE, sea: { surface: 'water', centre: CENTRE, size: 16000, level: r3(level - 0.05), fadeFrom: 4000, sectors }, land: ls.land, banks: ls.banks, pieces };
}
