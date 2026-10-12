// plan → world (suborned.world/1). Everything geometric comes from tools/plan (derive, placements, civic); this file only
// turns it into the export's shapes and picks heights from standards. Numbers the plan does not hold yet are in PROV, each
// with the standard it is waiting for.
import { type V, centroid, dist, norm, polyline, rectPoly, segDist, sub } from '../plan/geom';
import type { Derived, Plan } from '../plan/plan';
import type { Civic } from '../plan/civic';
import { placements, modeOf } from '../plan/transit';
import type { Circus, Flat, Landmark, Level, Mass, Road, SkyRail, Track, World } from '../../contracts/world/world.schema';
import { ctxOf } from './gen/common';
import { type Generated, generate, toDeck } from './gen';
import type { DeckRec } from './gen/types';
import { landmarkKit } from './gen/kitrefs';

/** Provisional numbers: TODO each moves into the plan's standards (Datum) and is then read from there. */
export const PROV = {
  view: { yaw: 30, pitch: 38, zoom: 14 }, // play camera, brief 002 B: zoom 14 at 38°
  pavement: 0.18, // standards.buildings.datum: pavement top; plots and civic stand on it
  kerb: { width: 0.3, height: 0.18 }, // TODO standards.roads.kerb
  water: -1, quayBase: -2.5, // TODO standards.harbour
  foot: 0.02, ground: -0.03, beyond: -0.02, // flat layers kept apart in height so they never z-fight
  track: { width: 1.6, level: 0.04, depth: 0.04 }, // TODO standards.tram.track
  beam: { width: 1.5, depth: 1.6, pylon: 1.6, cap: 1 }, // beam half-width 0.75 is standards.transit's; TODO standards.skyrail.beam
  platform: { tram: { width: 3, height: 0.3 }, sky: { width: 4, length: 50 } }, // sky 4 m from standards.levels.station; TODO standards.tram.platformHeight
  tower: 6, // civic entrance tower footprint: the block gives none; one facade bay (standards.buildings.bay)
  gate: { size: 2.4, height: 10, off: 2 }, // TODO standards.gates
};

const r3 = (n: number) => Math.round(n * 1000) / 1000;
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

function civicMasses(civ: Civic): Mass[] {
  const base = (z: number) => r3(PROV.pavement + z);
  const out: Mass[] = [];
  for (const b of civ.buildings) {
    out.push({ id: `civic/${b.id}`, surface: 'limestone', ring: ring(b.poly), base: base(b.z), height: b.height });
    for (const t of (b.towers ?? []) as { id: string; at: V; height: number }[]) {
      const h = PROV.tower / 2;
      out.push({ id: `civic/${b.id}/tower-${t.id}`, surface: 'limestone', ring: ring(rectPoly([t.at[0] - h, t.at[0] + h, t.at[1] - h, t.at[1] + h])), base: base(b.z), height: t.height });
    }
  }
  const sfc: Record<string, string> = { 'pavilion-support': 'column', 'service-cabinet': 'column', 'civic-monument': 'trim', 'boarding-canopy': 'enamel' };
  for (const s of civ.structures) out.push({ id: `civic/${s.id}`, surface: sfc[s.category] ?? 'limestone', ring: ring(s.poly), base: base(s.z), height: s.height });
  const ofk: Record<string, string> = { fountain: 'enamel', planter: 'park', table: 'trim' };
  for (const o of civ.objects) out.push({ id: `civic/${o.id}`, surface: ofk[o.kind] ?? 'limestone', ring: ring(o.poly), base: base(o.z), height: o.height });
  return out;
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
    ground: g, transit: t, buildings: gen.buildings, roofs: gen.roofs, frontages: gen.frontages, connectors: gen.connectors, street: gen.street,
    levels: levelsOf(plan, gen.decks), megastructures: gen.megas, civic: civicMasses(civ), landmarks: landmarks(plan), gates: gates(plan, d),
  };
  const counts = {
    ground: g.roads.length + g.foot.length + g.medians.length + g.circuses.length + g.water.length + g.quays.length + g.flats.length + g.plots.length,
    transit: t.tram.length + t.skyRail.length + t.stops.length + t.stations.length,
    buildings: w.buildings.length, roofs: w.roofs.length, frontages: w.frontages.length, connectors: w.connectors.length, street: w.street.length,
    levels: w.levels.length, decks: gen.decks.length, megastructures: w.megastructures.length, civic: w.civic.length, landmarks: w.landmarks.length, gates: w.gates.length,
  };
  return { world: { ...w, counts }, gen };
}
