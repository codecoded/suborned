// plan → world (suborned.world/1). Everything geometric comes from tools/plan (derive, placements, civic); this file only
// turns it into the export's shapes and picks heights from standards. Numbers the plan does not hold yet are in PROV, each
// with the standard it is waiting for.
import { type V, centroid, dist, norm, polyline, rectPoly, segDist, sub } from '../plan/geom';
import type { Derived, Plan } from '../plan/plan';
import { type Civic, toPlanFn } from '../plan/civic';
import { placements, modeOf } from '../plan/transit';
import type { Circus, Flat, Landmark, Level, Mass, Road, SkyRail, Track, World } from '../../contracts/world/world.schema';
import { ctxOf } from './gen/common';
import { type Generated, generate, toDeck } from './gen';
import type { DeckRec } from './gen/types';
import { adoptedBy, landmarkKit } from './gen/kitrefs';
import { horizonOf } from './gen/horizon';
import { WATER_LEVEL } from './gen/signatures';
import { dressCivic, dressingOf, footOf } from './civic/dress';
import { seededYaw } from './civic/cafe';
import { look } from './civic/look';

/** Provisional numbers: TODO each moves into the plan's standards (Datum) and is then read from there. */
export const PROV = {
  view: { yaw: 30, pitch: 38, zoom: 14 }, // play camera, brief 002 B: zoom 14 at 38°
  pavement: 0.18, // standards.buildings.datum: pavement top; plots and civic stand on it
  kerb: { width: 0.3, height: 0.18 }, // TODO standards.roads.kerb
  water: WATER_LEVEL, quayBase: -2.5, // TODO standards.harbour
  foot: 0.02, ground: -0.03, beyond: -0.02, // flat layers kept apart in height so they never z-fight
  track: { width: 1.6, level: 0.04, depth: 0.04 }, // TODO standards.tram.track
  beam: { width: 1.5, depth: 1.6, pylon: 1.6, cap: 1 }, // beam half-width 0.75 is standards.transit's; TODO standards.skyrail.beam
  platform: { tram: { width: 3, height: 0.3 }, sky: { width: 4, length: 50 } }, // sky 4 m from standards.levels.station; TODO standards.tram.platformHeight
  tower: 6, // civic entrance tower footprint when the block states none (block/2, /3): one facade bay (standards.buildings.bay)
  gate: { size: 2.4, height: 10, off: 2 }, // TODO standards.gates
};

const r3 = (n: number) => Math.round(n * 1000) / 1000, r4 = (n: number) => Math.round(n * 10000) / 10000;
const rv = (p: V): V => [r3(p[0]), r3(p[1])];
const ring = (poly: V[]) => poly.map(rv);

function ground(plan: Plan, d: Derived, civ: Civic): World['ground'] {
  const wide = (r: Derived['roads'][number]): Road => ({ id: `road/${r.id}`, surface: 'asphalt', kerbSurface: 'kerb', line: ring(r.line), width: r.w, level: 0, kerbWidth: PROV.kerb.width, kerbHeight: PROV.kerb.height });
  const c = plan.circus;
  const quayX = Math.min(...plan.quay.flat().map(p => p[0]));
  const flats: Flat[] = [
    { id: 'ground/land', surface: 'pavement', ring: rectPoly([plan.sheet.x[0], quayX, plan.sheet.y[0], plan.sheet.y[1]]), level: PROV.ground },
    ...plan.beyond.map((b, i): Flat => ({ id: `ground/beyond-${i}`, surface: 'beyond', ring: ring(b), level: PROV.beyond })),
    ...civ.bays.map((b): Flat => ({ id: `civic/bay/${b.id}`, surface: 'bay', ring: ring(b.poly), level: PROV.foot })),
  ];
  const circus: Circus = { id: 'circus/crown', surface: 'asphalt', islandSurface: 'circus/island', centre: rv(c.c), radius: c.ring[1], islandRadius: c.ring[0], level: 0, islandHeight: PROV.pavement };
  return {
    roads: d.roads.filter(r => !r.gyratory).map(wide), // the gyratory is the circus ring
    foot: plan.foot.map((f): Road => ({ id: `foot/${f.id}`, surface: 'foot', kerbSurface: 'kerb', line: f.pts.map(rv), width: f.w, level: PROV.foot, kerbWidth: 0, kerbHeight: 0 })),
    medians: d.zones.filter(z => z.kind === 'median').map((z, i): Mass => ({ id: `median/${i}`, surface: 'median', ring: ring(z.poly!), base: 0, height: PROV.pavement })),
    circuses: [circus],
    water: plan.water.map((w, i): Flat => ({ id: `water/${i}`, surface: 'water', ring: ring(w), level: PROV.water })),
    quays: plan.quay.map((q, i): Mass => ({ id: `quay/${i}`, surface: 'quay', ring: ring(q), base: PROV.quayBase, height: r3(PROV.pavement - PROV.quayBase) })),
    flats, plots: [],
  };
}

export function transit(plan: Plan, d: Derived): World['transit'] {
  const tstd = plan.standards.tram, sstd = plan.standards.skyrail, lv = plan.standards.levels;
  const tram: Track[] = d.tram.map(p => ({ id: `tram/${p.line}/${p.id}`, surface: 'track', line: ring(polyline(p.segs, 2)), width: PROV.track.width, level: PROV.track.level, depth: PROV.track.depth, spacing: p.double ? tstd.trackCentres : 0 }));
  const skyRail: SkyRail[] = d.sky.map(p => ({
    id: `skyrail/${p.line}/${p.id}`, surface: 'beam', pylonSurface: 'pylon', line: ring(polyline(p.segs, 2)),
    pylons: d.pylons.filter(x => x.line === p.line && x.path === p.id).map(x => rv(x.p)),
    height: sstd.height, width: PROV.beam.width, depth: PROV.beam.depth, pylonSize: PROV.beam.pylon, ground: PROV.quayBase,
    spacing: p.double ? sstd.beamCentres : 0, capDepth: p.double ? PROV.beam.cap : 0,
  }));
  const sides = (mode: 'tram' | 'sky') => modeOf(plan).find(m => m[0] === mode)![1].flatMap(line => placements(plan, d, line, mode).items.map(s => ({ line, s })));
  const platform = (id: string, s: ReturnType<typeof sides>[number]['s'], side: 1 | -1, off: number, w: number, len: number, base: number, h: number): Mass => {
    const smp = s.path.samples.reduce((b, x) => (dist(x.p, s.at) < dist(b.p, s.at) ? x : b)), u = smp.dir, n: V = [-u[1] * side, u[0] * side];
    const c: V = [smp.p[0] + n[0] * off, smp.p[1] + n[1] * off], a: V = [u[0] * len / 2, u[1] * len / 2], b: V = [n[0] * w / 2, n[1] * w / 2];
    return { id, surface: 'platform', ring: ring([[c[0] - a[0] - b[0], c[1] - a[1] - b[1]], [c[0] + a[0] - b[0], c[1] + a[1] - b[1]], [c[0] + a[0] + b[0], c[1] + a[1] + b[1]], [c[0] - a[0] + b[0], c[1] - a[1] + b[1]]]), base: r3(base), height: r3(h) };
  };
  const stops = sides('tram').flatMap(({ line, s }) => ([1, -1] as const).map(side =>
    platform(`stop/${line.id}/${s.id}/${side > 0 ? 'l' : 'r'}`, s, side, tstd.trackCentres / 2 + PROV.track.width / 2 + PROV.platform.tram.width / 2 + 0.5, PROV.platform.tram.width, s.length ?? tstd.platform, 0, PROV.platform.tram.height)));
  const edge = sstd.beamCentres / 2 + PROV.beam.width / 2, sw = PROV.platform.sky.width;
  const stations = sides('sky').flatMap(({ line, s }) => ([1, -1] as const).map(side =>
    platform(`station/${line.id}/${s.id}/${side > 0 ? 'l' : 'r'}`, s, side, edge + sw / 2, sw, s.length ?? PROV.platform.sky.length, sstd.height - lv.structureDepth.deck, lv.structureDepth.deck)));
  return { tram, skyRail, stops, stations };
}

function levelsOf(plan: Plan, decks: DeckRec[]): Level[] {
  return plan.standards.levels.list.map((l: { id: string; z: number }): Level => ({
    id: `level/${l.id}`, name: l.id, height: l.z, decks: decks.filter(d => d.level === l.id).map(toDeck),
    stairs: [], // stairs, ramps, lifts and escalators are `connectors`
  }));
}

/** The civic block as export masses: buildings, structures and objects, with their kit pieces (grammar §11) and the block's dressing. The export's `civic`, and what
 *  the people tools read the café sets from (tools/people/seats.ts), so a seat stands where the world draws a chair. */
export function civicMasses(civ: Civic, toPlan: (p: V) => V): Mass[] {
  const base = (z: number) => r3(PROV.pavement + z);
  const out: Mass[] = [];
  for (const b of civ.buildings) {
    out.push({ id: `civic/${b.id}`, surface: 'limestone', ring: ring(b.poly), base: base(b.z), height: b.height });
    for (const t of b.towers ?? []) { // the block's own plan (block/4 `towerFootprint`), else the provisional 6 m square on `at`
      const h = PROV.tower / 2;
      out.push({ id: `civic/${b.id}/tower-${t.id}`, surface: 'limestone', ring: ring(t.poly ?? rectPoly([t.at[0] - h, t.at[0] + h, t.at[1] - h, t.at[1] + h])), base: base(b.z), height: t.height });
    }
  }
  const sfc: Record<string, string> = { 'pavilion-support': 'column', 'service-cabinet': 'column', 'civic-monument': 'trim', 'boarding-canopy': 'enamel' };
  for (const s of civ.structures) out.push({ id: `civic/${s.id}`, surface: sfc[s.category] ?? 'limestone', ring: ring(s.poly), base: base(s.z), height: s.height });
  const ofk: Record<string, string> = { fountain: 'enamel', planter: 'park', table: 'trim' };
  for (const o of civ.objects) out.push({ id: `civic/${o.id}`, surface: ofk[o.kind] ?? 'limestone', ring: ring(o.poly), base: base(o.z), height: o.height });
  const fountain = out.find(m => /\/atom-fountain$/.test(m.id)), fc = fountain ? boxOf(fountain.ring).c : undefined;
  const dress = dressingOf(civ) ? look : undefined; // the block's dressing (brief 008): the pieces it swaps in, grounds and scales
  const kitted = out.map(m => civicKit(m, fc, dress));
  return dressCivic(civ, toPlan, clearWalls(kitted, new Set(civ.buildings.map(b => `civic/${b.id}`))), PROV.pavement);
}

/** Grammar §11: civic objects and structures a kit piece stands for. The extrusion stays for ids and footprint.
 *  fit: uniform scale within the piece's tags so its footprint fills the polygon's box (yaw turns the long axis); a piece a little smaller than the box takes
 *  its largest scale if it still fills `FILL` of the box's long side (the planters). person: furniture and the steward robot stand at person scale (1.0,
 *  Latch round 3: the café sets came out 0.72 m tall when fitted to their footprints); the footprint is then only a clearance check (civicClearance).
 *  plinth: a monument piece shorter than its mass stands on a plinth of the mass ring up to kit.base. */
const CIVIC_KIT: [RegExp, string, 'fit' | 'person' | 'plinth' | 'arc'][] = [[/\/atom-fountain$/, 'AtomFountain', 'fit'], [/\/fountain-garden-[a-z-]+$/, 'PlanterArc', 'arc'], [/\/cafe-table-\d+$/, 'CafeSet_Tulip', 'person'], [/\/cafe-palm-\d+$/, 'PottedPalm_Small', 'person'],
  [/\/office-boarding-pavilion$/, 'BoardingPavilion_Office', 'fit'], [/\/square-boarding-pavilion$/, 'BoardingPavilion_Square', 'fit'],
  [/\/office-pavilion-support-\d+$/, 'PavilionPier_Office', 'fit'], [/\/square-pavilion-support-\d+$/, 'PavilionPier_Square', 'fit'], [/\/civic-(control|service)-cabinet/, 'UtilityCabinet', 'fit'],
  [/\/civic-monument-(west|east)$/, 'OvalPlanter', 'fit'], [/\/foreground-civic-robot$/, 'RobotButler', 'plinth']];
const FILL = 0.85;
/** arc: a curved bed about the fountain (the four `fountain-garden-*`, Vela brief 007 round 3). PlanterArc is built to the bed's band (r 5.36-7.06): its pivot
 *  is the footprint centre, its outer curved side is the front (Blender -Y = kit +Z) and the fountain centre lies `r` m behind the pivot (Blender +Y = kit -Z).
 *  So the pivot stands `r` m out from the fountain centre toward the bed, the front faces away from the fountain: yaw = atan2(out) - pi/2 (front = yaw + 90).
 *  Vela's block-frame yaws (Blender rotation about z in the block frame, x south, y east) map to these by plan yaw = block yaw + 90 deg. */
export const ARC = { r: 6.12 };
const boxOf = (ring: V[]) => { const xs = ring.map(p => p[0]), ys = ring.map(p => p[1]); return { w: Math.max(...xs) - Math.min(...xs), d: Math.max(...ys) - Math.min(...ys), c: [(Math.max(...xs) + Math.min(...xs)) / 2, (Math.max(...ys) + Math.min(...ys)) / 2] as V }; };
export function civicKit(m: Mass, fountain?: V, dress?: Pick<typeof look, 'swaps' | 'grounded' | 'scales'>): Mass {
  const row = CIVIC_KIT.find(([re]) => re.test(m.id)), id = row ? (dress?.swaps[row[1]] ?? row[1]) : undefined, a = adoptedBy(id); if (!row || !id || !a) return m;
  const mode = row[2], { w: bw, d: bd, c } = boxOf(m.ring);
  if (mode === 'arc') {
    if (!fountain) return { ...m, source: 'procedural' };
    const u = norm(sub(c, fountain)), at: V = [fountain[0] + u[0] * ARC.r, fountain[1] + u[1] * ARC.r];
    return { ...m, source: 'kit', kit: { id, scale: [1, 1, 1], yaw: r4(Math.atan2(-u[0], u[1])), at: rv(at), base: m.base } };
  }
  const turn = (bw >= bd) !== (a.size[0] >= a.size[2]) && Math.abs(bw - bd) > 0.05 && Math.abs(a.size[0] - a.size[2]) > 0.05, [pw, pd] = turn ? [a.size[2], a.size[0]] : [a.size[0], a.size[2]];
  const [lo, hi] = a.tags.scale, box = Math.min(bw / pw, bd / pd);
  let s = r3(box);
  if (mode !== 'fit') s = 1;
  else if (s > hi + 1e-6 && (hi * Math.max(pw, pd)) / Math.max(bw, bd) >= FILL) s = hi;
  if (dress?.scales[id]) s = dress.scales[id]!; // the block states this piece's scale (the piers under the v2 shelter)
  if (s < lo - 1e-6 || s > hi + 1e-6) return { ...m, source: 'procedural' };
  const canopy = /boarding-pavilion$/.test(m.id) && !dress?.grounded.includes(id), top = m.base + m.height; // a canopy-only piece hangs at the top of its mass
  const yaw = turn ? Math.PI / 2 : 0, seeded = /cafe-(table|palm)/.test(m.id) ? seededYaw(m.id) : yaw;
  const base = canopy ? top - a.size[1] * s : mode === 'plinth' ? Math.max(m.base, top - a.size[1] * s) : m.base;
  return { ...m, source: 'kit', kit: { id, scale: [s, s, s], yaw: seeded, at: rv(c), base: r3(base) } };
}

/** A kit piece's footprint at its scale (turned by its yaw, about its footprint centre): kit x -> plan along yaw, kit z -> plan across (front = yaw + 90°);
 *  the footprint centre sits at the pivot + pivotOffset. */
const kitFoot = (m: Mass): V[] => footOf(adoptedBy(m.kit!.id)!, m.kit!);
/** The person-scale pieces, with the swap the look names for each. Read from `look` at the call: a run's pinned look (withLook) swaps it. */
export const personPieces = () => new Set(CIVIC_KIT.filter(r => r[2] === 'person').flatMap(r => [r[1], look.swaps[r[1]] ?? r[1]]));
/** Person-scale pieces whose footprint reaches a building wall (the café palms' fronds, brief 007 round 3): the piece and its mass slide straight out from the
 *  nearest wall edge in `step` m steps until the footprint is `clear` m off the wall, at most `max` m; the move is kept only if the footprint then overlaps no other
 *  civic mass or person-scale piece (civicClearance), so it stays in the terrace between the wall and the table row. Deterministic: same block, same result. */
export const NUDGE = { step: 0.05, clear: 0.1, max: 0.6 };
function clearWalls(civic: Mass[], walls: Set<string>): Mass[] {
  const bld = civic.filter(m => walls.has(m.id)), out = civic.slice(), person = personPieces();
  const near = (f: V[], r: V[]) => overlap(f, r) || f.some(p => r.some((q, i) => segDist(p, q, r[(i + 1) % r.length]!) < NUDGE.clear)) || r.some(p => f.some((q, i) => segDist(p, q, f[(i + 1) % f.length]!) < NUDGE.clear));
  out.forEach((m, k) => {
    if (!m.kit || !person.has(m.kit.id)) return;
    const w = bld.find(b => b.base <= m.base + 1.2 && near(kitFoot(m), b.ring)); if (!w) return;
    let bi = 0, bd = Infinity; w.ring.forEach((q, i) => { const d = segDist(m.kit!.at, q, w.ring[(i + 1) % w.ring.length]!); if (d < bd) { bd = d; bi = i; } });
    const e0 = w.ring[bi]!, e1 = w.ring[(bi + 1) % w.ring.length]!, t = norm(sub(e1, e0)), mid: V = [(e0[0] + e1[0]) / 2, (e0[1] + e1[1]) / 2];
    let n: V = [t[1], -t[0]]; if ((m.kit.at[0] - mid[0]) * n[0] + (m.kit.at[1] - mid[1]) * n[1] < 0) n = [-n[0], -n[1]]; // out of the wall, toward the piece
    for (let i = 1; i * NUDGE.step <= NUDGE.max + 1e-9; i++) {
      const d = i * NUDGE.step;
      const mv = (p: V): V => [r3(p[0] + n[0] * d), r3(p[1] + n[1] * d)], c: Mass = { ...m, ring: m.ring.map(mv), kit: { ...m.kit, at: mv(m.kit.at) } };
      if (near(kitFoot(c), w.ring)) continue;
      const trial = out.slice(); trial[k] = c;
      if (!civicClearance(trial).some(x => x.startsWith(`${m.id} `) || x.endsWith(` ${m.id}`))) out[k] = c;
      return;
    }
  });
  return out;
}

/** Person-scale civic pieces whose footprint at scale 1 (turned by its yaw, about its footprint centre) overlaps another civic mass or person-scale piece. */
export function civicClearance(civic: Mass[]): string[] {
  const person = personPieces(), out: string[] = [], foot = kitFoot;
  const pieces = civic.filter(m => m.kit && person.has(m.kit.id));
  for (const m of pieces) {
    const f = foot(m);
    for (const o of civic) {
      if (o === m || o.base > m.base + 1.2 || o.base + o.height < m.base + 0.05) continue; // only what stands in the same body of air
      if (o.kit && look.overhead.includes(o.kit.id)) continue; // a parasol stands over its table: only its pole is on the ground
      if (o.kit && look.frame.includes(o.kit.id)) continue; // the terrace's box is open ground inside its wall: the tables and palms belong there
      const ring = o.kit && person.has(o.kit.id) ? foot(o) : o.ring;
      if (pieces.indexOf(o) >= 0 && pieces.indexOf(o) < pieces.indexOf(m)) continue; // each pair once
      if (overlap(f, ring)) out.push(`${m.id} (${m.kit!.id} x1) overlaps ${o.id}`);
    }
  }
  return out;
}
/** Two simple polygons overlap (an edge crosses, or one has a vertex inside the other). */
function overlap(a: V[], b: V[]): boolean {
  const inside = (p: V, r: V[]) => { let c = false; for (let i = 0, j = r.length - 1; i < r.length; j = i++) { const [xi, yi] = r[i]!, [xj, yj] = r[j]!; if ((yi > p[1]) !== (yj > p[1]) && p[0] < ((xj - xi) * (p[1] - yi)) / (yj - yi) + xi) c = !c; } return c; };
  const cr = (o: V, p: V, q: V) => (p[0] - o[0]) * (q[1] - o[1]) - (p[1] - o[1]) * (q[0] - o[0]);
  for (let i = 0; i < a.length; i++) for (let j = 0; j < b.length; j++) {
    const p1 = a[i]!, p2 = a[(i + 1) % a.length]!, q1 = b[j]!, q2 = b[(j + 1) % b.length]!;
    if (cr(p1, p2, q1) * cr(p1, p2, q2) < 0 && cr(q1, q2, p1) * cr(q1, q2, p2) < 0) return true;
  }
  return inside(a[0]!, b) || inside(b[0]!, a);
}

function landmarks(plan: Plan): Landmark[] {
  return plan.landmarks.map((l): Landmark => {
    const id = `landmark/${l.id}`, top = l.top ?? 0; // top: the plan's height cap; the sphere and monument stand on the pavement, the skyport's body is its caisson
    const m: Landmark = l.caisson
      ? { id, surface: 'landmark', shape: 'cylinder', centre: rv(l.c), radius: l.caisson.r, base: PROV.quayBase, height: r3(l.caisson.deck - PROV.quayBase) }
      : { id, surface: 'landmark', shape: l.id === 'sphere' ? 'sphere' : 'cylinder', centre: rv(l.c), radius: l.r, base: PROV.pavement, height: top };
    return landmarkKit(m, l); // the plan's disc (its r) and height (its top); the kit piece is scaled to fit them
  });
}

function gates(plan: Plan, d: Derived): Mass[] {
  const g = PROV.gate;
  return plan.gates.map(gt => {
    const p = plan.nodes[gt.node], road = d.roads.find(r => r.id === gt.road)!;
    let best = Infinity, dir: V = [1, 0];
    road.line.slice(1).forEach((b, i) => { const a = road.line[i], dd = segDist(p, a, b); if (dd < best) { best = dd; dir = norm(sub(b, a)); } });
    const off = road.w / 2 + g.off, c: V = [p[0] - dir[1] * off, p[1] + dir[0] * off], h = g.size / 2;
    return { id: `gate/${gt.id}`, surface: 'gate', ring: ring(rectPoly([c[0] - h, c[0] + h, c[1] - h, c[1] + h])), base: 0, height: g.height };
  });
}

export function worldOf(plan: Plan, d: Derived, civ: Civic): { world: World; gen: Generated } {
  const g = ground(plan, d, civ), t = transit(plan, d), gen = generate(ctxOf(plan, d, civ, PROV.pavement), t.stops, t.stations);
  g.plots = gen.slabs;
  const sq = d.plots.find(p => p.civic === 'square')!;
  const w: Omit<World, 'counts'> = {
    schema: 'suborned.world/2', source: `${plan.id} v${plan.version} (${plan.date})`,
    view: { ...PROV.view, focus: rv(centroid(sq.polyV)) },
    ground: g, transit: t, buildings: gen.buildings, roofs: gen.roofs, skins: gen.skins, fabric: gen.fabric, facades: gen.facades, frontages: gen.frontages, connectors: gen.connectors, street: gen.street, ...(gen.crossings ? { crossings: gen.crossings } : {}),
    levels: levelsOf(plan, gen.decks), megastructures: gen.megas, civic: civicMasses(civ, toPlanFn(plan.civic)), landmarks: landmarks(plan), signatures: gen.signatures, gates: gates(plan, d), horizon: horizonOf(plan, PROV.water, { level: PROV.beyond, bankBase: PROV.quayBase, bankDepth: 4 }),
  };
  const counts = {
    ground: g.roads.length + g.foot.length + g.medians.length + g.circuses.length + g.water.length + g.quays.length + g.flats.length + g.plots.length,
    transit: t.tram.length + t.skyRail.length + t.stops.length + t.stations.length,
    buildings: w.buildings.length, roofs: w.roofs.length, skins: w.skins.length, fabric: w.fabric.awnings.length + w.fabric.banners.length + w.fabric.flags.length, facades: w.facades.length, frontages: w.frontages.length, connectors: w.connectors.length, street: w.street.length,
    levels: w.levels.length, ...(w.crossings ? { crossings: w.crossings.length } : {}), decks: gen.decks.length, megastructures: w.megastructures.length, civic: w.civic.length, landmarks: w.landmarks.length, gates: w.gates.length, signatures: w.signatures.length, horizon: w.horizon.pieces.length,
  };
  return { world: { ...w, counts }, gen };
}
