// The derived world for Unity: the checker's own geometry (derive, civic, report) reshaped into suborned.world/1.
import type { World, P } from '../../contracts/world/world.schema';
import { type V, add, boxAt, centroid, circlePoly, inPoly, isClosed, left, mul, notch, ribbon, ringRibbon } from './geom';
import { type Derived, type Plan, type PathGeo, at, gatePos } from './plan';
import { circusPavement, medianPoly, reservationPoly, ribbonToRing } from './circus';
import type { Civic } from './civic';
import type { Report } from './checks';

const PAVEMENT = .18, KERB = .8, FOOT_KERB = .6;
/** Gates stand this far in from their junction node, along their road (as the CAD sheet draws them). */
const GATE_SET = 11;
const UNBUILT = ['park', 'square', 'open'];
const r3 = (x: number) => Math.round(x * 1000) / 1000;
const pts = (a: V[]): P[] => a.map(p => [r3(p[0]), r3(p[1])]);
const pt = (p: V): P => [r3(p[0]), r3(p[1])];

function track(p: PathGeo, gauge: number) {
  const s = p.samples.filter((_, i) => i % 4 === 0 || i === p.samples.length - 1);
  const tracks = p.double ? [-1, 1].map(k => pts(s.map(x => add(x.p, mul(left(x.dir), (k * gauge) / 2))))) : [pts(s.map(x => x.p))];
  return { line: p.line, path: p.id, role: p.role, double: !!p.double, centreline: pts(s.map(x => x.p)), tracks, gauge };
}

/** A closed road's carriageway and kerb as annuli: carriageway/kerb = outer ring, hole = the inner ring of each. */
function ringBoth(line: V[], hw: number) {
  const c = ringRibbon(line, hw), k = ringRibbon(line, hw + KERB);
  return { carriageway: pts(c.outer), kerb: pts(k.outer), hole: { carriageway: pts(c.hole), kerb: pts(k.hole) } };
}

export function world(plan: Plan, d: Derived, civ: Civic, R: Report, planHash: string): World {
  const std = plan.standards, half = std.tram.trackCentres / 2, beam = std.skyrail.height, c = plan.circus;
  const sky = std.skyrail.beamCentres as number;
  const platform = (line: string, kind: 'tram' | 'sky', s: any) => {
    const l = (kind === 'tram' ? plan.tram : plan.skyrail).find(x => x.id === line)!;
    const def = (kind === 'tram' ? l.stops : l.stations)!.find(x => x.id === s.id)!;
    const path = (kind === 'tram' ? d.tram : d.sky).find(x => x.line === line && x.id === def.path)!, x = at(path, s.chainage);
    const platforms = kind === 'tram' ? [-1, 1].map(k => boxAt(x.p, x.dir, std.tram.platform, 3, k * (half + 3))) : [boxAt(x.p, x.dir, def.length ?? 50, sky + 8)];
    return { line, id: s.id, name: s.name, at: pt(x.p), dir: pt(x.dir), chainage: r3(s.chainage), platforms: platforms.map(pts), ...(def.interchange ? { interchange: def.interchange } : {}) };
  };
  // Ground under a footprint: carriageway level on a road, the circus ring or a lay-by, else pavement. The manifest's z is
  // height above that ground, so base = ground + z.
  const carriage = [...d.roads.flatMap(r => (isClosed(r.line) ? [] : [ribbon(r.body, r.w / 2)])), ...civ.bays.map(b => b.poly)];
  const ring = d.roads.filter(r => isClosed(r.line)).map(r => ringRibbon(r.line, r.w / 2));
  const onRoad = (p: V) => carriage.some(c => inPoly(p, c)) || ring.some(g => inPoly(p, g.outer) && !inPoly(p, g.hole));
  const base = (poly: V[], z: number) => r3((onRoad(centroid(poly)) ? 0 : PAVEMENT) + z);
  return {
    schema: 'suborned.world/1', source: { planHash }, id: plan.id, name: plan.name, planVersion: plan.version, planDate: plan.date,
    view: { yaw: plan.view.yaw, pitch: plan.view.pitch },
    frame: { units: 'm', x: 'east', y: 'north', unity: '(x, height, y)' },
    heights: { carriageway: 0, pavement: PAVEMENT, skyRailBeam: beam },
    roads: d.roads.map(r => {
      const hw = r.w / 2, closed = isClosed(r.line), road = {
        id: r.id, name: r.name, class: r.class, width: r.w, gyratory: !!r.gyratory, closed,
        centreline: pts(r.line),
        ...(closed ? ringBoth(r.line, hw) : { carriageway: pts(ribbon(r.body, hw)), kerb: pts(ribbon(r.body, hw + KERB)) }),
      };
      return { ...road, ...(r.reservation ? { reservation: { width: r.reservation, polygon: pts(reservationPoly(plan, r)) } } : {}) };
    }),
    medians: d.zones.filter(z => z.kind === 'median').map((z, _, all) => ({ id: `${z.road}-${all.filter(o => o.road === z.road).indexOf(z)}`, road: z.road!, polygon: pts(medianPoly(plan, z.poly!)) })),
    foot: plan.foot.map(f => ({ id: f.id, name: f.name, kind: f.kind as 'mall' | 'walk', width: f.w, centreline: pts(f.pts), polygon: pts(ribbonToRing(plan, f.pts, f.w / 2, 0, c.pavement - c.ring[1])), kerb: pts(ribbon(f.pts, f.w / 2 + FOOT_KERB)) })),
    plots: d.plots.map(p => ({ id: p.id, ...(p.name ? { name: p.name } : {}), use: p.use, area: p.area, ...(p.civic ? { civic: p.civic as 'square' | 'office' } : {}), built: !UNBUILT.includes(p.use), ...(p.height !== undefined ? { height: p.height } : {}), polygon: pts(p.polyV) })),
    areas: d.areas.map(a => ({ id: a.id, name: a.name, polygon: pts(a.polyV), ...(a.label ? { label: pt(a.label) } : {}) })),
    circus: {
      centre: pt(c.c), garden: pts(circlePoly(c.c, c.garden)), island: pts(circlePoly(c.c, c.island)),
      carriageway: { inner: pts(circlePoly(c.c, c.ring[0])), outer: pts(circlePoly(c.c, c.ring[1])) }, pavement: circusPavement(plan, d, KERB).map(pts),
      radii: { garden: c.garden, island: c.island, carriagewayInner: c.ring[0], carriagewayOuter: c.ring[1], pavement: c.pavement },
    },
    water: { water: plan.water.map(pts), quay: plan.quay.map(pts), beyond: plan.beyond.map(pts) },
    tram: d.tram.map(p => track(p, std.tram.trackCentres)),
    stops: R.stops.map(s => ({ kind: 'tram' as const, ...platform(s.line, 'tram', s) })),
    skyRail: d.sky.map(p => ({ ...track(p, sky), height: beam })),
    pylons: d.pylons.map((p, _, all) => ({ id: `${p.line}.${p.path}.pylon-${all.filter(o => o.line === p.line && o.path === p.path).indexOf(p)}`, line: p.line, path: p.path, at: pt(p.p), span: r3(p.span), height: beam })),
    stations: R.stations.map(s => ({ kind: 'sky' as const, ...platform(s.line, 'sky', s), height: beam })),
    gates: plan.gates.map(g => {
      const { at: p, dir } = gatePos(plan, g, GATE_SET);
      return { id: g.id, name: g.name, at: pt(p), dir: pt(dir), road: g.road, area: g.area };
    }),
    landmarks: plan.landmarks.map(l => ({ id: l.id, name: l.name, centre: pt(l.c), radius: l.r })),
    handover: {
      note: 'Exact footprints and heights from contracts/world/handover-block.json (suborned.block/2, Oct 9 city-scale sources + Vantage adjustments). base = ground under the footprint centre (carriageway or lay-by 0, else pavement 0.18) + the manifest z (height above that ground).',
      blocks: civ.blocks.map(b => ({ id: b.id, outer: pts(civ.bays.reduce((o, bay) => notch(o, bay.poly), b.outer)), core: pts(b.core) })),
      buildings: civ.buildings.map(b => ({ id: b.id, name: b.name, source: b.source, polygon: pts(b.poly), base: base(b.poly, b.z), height: b.height, ...(b.heights ? { heights: b.heights } : {}), ...(b.towers ? { towers: b.towers.map((x: any) => ({ id: x.id, at: pt(x.at), height: x.height })) } : {}) })),
      structures: civ.structures.map(b => ({ id: b.id, name: b.name, category: b.category, polygon: pts(b.poly), base: base(b.poly, b.z), height: b.height })),
      objects: civ.objects.map(o => ({ id: o.id, kind: o.kind, polygon: pts(o.poly), base: base(o.poly, o.z), height: o.height })),
      bays: civ.bays.map(b => ({ id: b.id, polygon: pts(b.poly), level: 0 })),
      crossing: pts(civ.crossing),
      points: civ.points.map(p => ({ name: p.name, at: pt(p.p), surface: p.surface })),
      crew: civ.crew.map(p => ({ name: p.name, at: pt(p.p), surface: p.surface })),
    },
  };
}
