// The fight range's ground (brief 029, Keel): sim/data/range.site.json (layout "open") and sim/data/range-dense.site.json (layout "dense"),
// in the site format the engine already loads (suborned.sim.site/1, mission "range"). A flat graybox floor, 45 m along the firing line (x)
// and 40 m across (y), with a fixed cover layout of four kinds: low wall (1.0 m, shoot over it), high wall (2.5 m, blocks sight), pillar
// (3 m, blocks sight) and a car-sized box (1.45 m, shoot over it). Integer centimetres, x along the range from the cell's end, y across.
//
// Nothing about cover is authored here: the cover points, their arcs and their quality come from tools/cover/gen.ts `pointsOf`, the same
// rule as the Handover's (quality = height class × arc; ≥ cover-q-full full, ≥ cover-q-partial partial). Under that rule a 1.6 m low wall
// is partial cover and a car box full; a pillar is partial and blocks sight; a high wall blocks sight (full cover behind its long faces).
//
// The cell stands at the near end on a line across the range; the range's mark (range:mark) is its centre, and every spawn distance is
// measured from it. Spawn slots (spawn:<placement>:1..6) are data: near 8 m, eff 15 m (the effective ring), beyond 18 m (past every
// hostile's 15 m effective reach and the lab sidearm's 16 m, inside today's pistol 25 m), on an arc of ±25° about the firing line, each on
// the nearest free cell; cover = the six cover points a body fires over (low walls, boxes: against fire, not sight) that shield against the
// mark 16–26 m out, nearest 20 m first.
//
// The loader's Handover anchors (guard post, Vale, the official car, patrol, cameras, the exit lane, the cast) are required keys of the
// site format; here they are parked in the wings, a strip beyond the floor that no walk reaches, with ids "wings:*". The engine's range
// mode never reads them (sim/Suborned.Sim/GameRange.cs); the host never draws them.
//
// Deterministic: the same script gives the same bytes. Run: bun tools/sim/range.ts (writes both files; --check exits 1 if either differs).
import { readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { createHash } from 'node:crypto';
import { pointsOf, gridOf, RULES } from '../cover/gen';
import { protects, dist } from '../cover/select';

const root = join(import.meta.dir, '..', '..');
export const RANGE_FILES = { open: 'sim/data/range.site.json', dense: 'sim/data/range-dense.site.json', lane: 'sim/data/range-lane.site.json' } as const;

const CELL = 50, BODY = 65;
/** The floor's length (cm along x) by layout: open and dense 45 m; lane (Motive tuning.md §10.4, F8-BREAK) the open layout with the floor
 * run on to 90 m, a clear straight lane of at least 60 m behind a cell standing 14 m out from the near slots, so the far wall no longer ends
 * a run away. The wings (and the anchors parked there) move with the floor's end. */
const FLOOR_X1: Record<keyof typeof RANGE_FILES, number> = { open: 4500, dense: 4500, lane: 9000 };
const MARK: [number, number] = [300, 2000];
const CREW: Record<string, [number, number]> = { scout: [300, 1775], fixer: [300, 1925], tech: [300, 2075], enforcer: [300, 2225] };
const ENTRY: [number, number] = [4300, 2000];  // the response unit walks in from here (the far end)
const RETREAT: [number, number] = [4400, 3600]; // broken rivals run here (rivals.park): the far corner

type Kind = 'low-wall' | 'high-wall' | 'pillar' | 'car-box';
/** Each kind's size: along the face (m), depth (m), height (m). A low wall is 1.6 m long so its middle point is partial cover (q < 600). */
const SIZE: Record<Kind, { len: number; depth: number; h: number; cover: 'wall' | 'pier' | 'car'; label: string }> = {
  'low-wall': { len: 1.6, depth: 0.4, h: 1.0, cover: 'wall', label: 'low wall: shoot over it; partial cover' },
  'high-wall': { len: 4.0, depth: 0.3, h: 2.5, cover: 'wall', label: 'high wall: blocks sight and fire; full cover behind its faces' },
  'pillar': { len: 0.8, depth: 0.8, h: 3.0, cover: 'pier', label: 'pillar: blocks sight; partial cover at its faces' },
  'car-box': { len: 4.6, depth: 1.9, h: 1.45, cover: 'car', label: 'car-sized box: shoot over it; full cover' },
};
/** A piece: its kind, its centre (m, x along the range, y across) and which way its face runs ("y": a face across the firing line). */
type Piece = { kind: Kind; at: [number, number]; along: 'x' | 'y' };
const P = (kind: Kind, x: number, y: number, along: 'x' | 'y' = 'y'): Piece => ({ kind, at: [x, y], along });

/** Layout "open": a row of cover in front of the cell (2–3 m walk, out of the hostiles' reach of their row), a forward row 7 m out (in it),
 * two high walls and two pillars mid-field, a row at 20 m for the hostiles. */
const OPEN: Piece[] = [
  // the cell's row, x 6.5–7 m: the near faces' points stand 2–3 m in front of the cell line
  P('pillar', 7, 9.5), P('low-wall', 6.5, 14.5), P('car-box', 7, 20), P('low-wall', 6.5, 25.5), P('pillar', 7, 30.5),
  // a forward row, x 11 m, off the near slots: low walls whose near faces' points stand inside the hostiles' 15 m reach of their row (cover in contact)
  P('low-wall', 11, 13), P('low-wall', 11, 27),
  // mid-field, x 14 m
  P('high-wall', 14, 8), P('pillar', 14, 15), P('pillar', 14, 25), P('high-wall', 14, 32),
  // the hostiles' row, x 22.5–23.25 m (cover slots are the points a body can fire over: low walls and boxes, never behind a high wall or a pillar, which hide it): the far faces' points stand about 20 m from the mark
  P('low-wall', 22.5, 7), P('car-box', 23.25, 12), P('low-wall', 22.5, 17), P('low-wall', 22.5, 23), P('car-box', 23.25, 28), P('low-wall', 22.5, 33),
  P('pillar', 26, 20),
];
/** Layout "dense": the open layout and more: low walls and boxes mid-field, a second hostile row at 30 m, high walls on the flanks. */
const DENSE: Piece[] = [
  ...OPEN,
  P('car-box', 18, 5.5, 'x'), P('car-box', 18, 34.5, 'x'),
  P('low-wall', 17.5, 17.5), P('low-wall', 17.5, 22.5), P('pillar', 19.5, 12), P('pillar', 19.5, 28),
  P('high-wall', 29, 5), P('high-wall', 29, 35), P('low-wall', 30, 16), P('car-box', 30.75, 20.5), P('low-wall', 30, 25), P('pillar', 34, 12), P('pillar', 34, 28),
];

const SLOT_ANGLES = [5, -5, 15, -15, 25, -25]; // degrees off the firing line, slot 1..6: the middle first
const PLACEMENTS = { near: 800, eff: 1500, beyond: 1800 } as const; // cm from the mark
const COVER_BAND = { min: 1600, max: 2600, want: 2000 } as const;

type V2 = [number, number];
const rectM = (p: Piece): V2[] => {
  const s = SIZE[p.kind], [hx, hy] = p.along === 'y' ? [s.depth / 2, s.len / 2] : [s.len / 2, s.depth / 2];
  const [x, y] = p.at; return [[x - hx, y - hy], [x + hx, y - hy], [x + hx, y + hy], [x - hx, y + hy]];
};
const box = (r: V2[]) => ({ x0: Math.min(...r.map(q => q[0])), x1: Math.max(...r.map(q => q[0])), y0: Math.min(...r.map(q => q[1])), y1: Math.max(...r.map(q => q[1])) });
const dRect = (p: V2, r: V2[]) => { const b = box(r), dx = Math.max(b.x0 - p[0], 0, p[0] - b.x1), dy = Math.max(b.y0 - p[1], 0, p[1] - b.y1); return Math.hypot(dx, dy); };

export function buildRange(layout: keyof typeof RANGE_FILES): { json: string; site: any } {
  const pieces = layout === 'dense' ? DENSE : OPEN;
  const FLOOR = { x0: 0, x1: FLOOR_X1[layout], y0: 0, y1: 4000 };                  // cm
  const WINGS = { x0: FLOOR.x1 + 400, x1: FLOOR.x1 + 1600, y0: 0, y1: 4000 };      // cm: unreachable from the floor (a 4 m gap of blocked cells)
  const WINDOW = { x0: 0, y0: 0, nx: (WINGS.x1 - 0) / CELL, ny: (FLOOR.y1 - 0) / CELL };
  const wx = FLOOR.x1 - 4500;                                                       // the wings' anchors, moved with the floor's end (0 for open and dense)
  const count: Record<string, number> = {};
  const objs = pieces.map(p => { count[p.kind] = (count[p.kind] ?? 0) + 1; return { id: `range/${p.kind}-${count[p.kind]}`, kind: SIZE[p.kind].cover, poly: rectM(p), h: SIZE[p.kind].h, piece: p }; });
  // every piece on the floor, a body's width (1.3 m) apart from every other, so the walk between them stays open
  for (const o of objs) {
    const b = box(o.poly); if (b.x0 * 100 < FLOOR.x0 + 100 || b.x1 * 100 > FLOOR.x1 - 100 || b.y0 * 100 < FLOOR.y0 + 100 || b.y1 * 100 > FLOOR.y1 - 100) throw new Error(`${layout}: ${o.id} is off the floor`);
    for (const q of objs) if (q !== o) { const qb = box(q.poly), gx = Math.max(qb.x0 - b.x1, b.x0 - qb.x1, 0), gy = Math.max(qb.y0 - b.y1, b.y0 - qb.y1, 0); if (Math.hypot(gx, gy) < 1.3) throw new Error(`${layout}: ${o.id} and ${q.id} are ${Math.hypot(gx, gy).toFixed(2)} m apart (want 1.3 m)`); }
  }
  // the walk grid: a cell is free when its centre is on the floor (a body's clearance off its edge) and a body's clearance off every piece; the wings are all free
  const rows: string[] = [], drive: string[] = [];
  for (let i = 0; i < WINDOW.nx; i++) {
    let r = '', d = '';
    for (let j = 0; j < WINDOW.ny; j++) {
      const c: V2 = [(i + 0.5) * CELL, (j + 0.5) * CELL];
      const onFloor = c[0] >= FLOOR.x0 + BODY && c[0] <= FLOOR.x1 - BODY && c[1] >= FLOOR.y0 + BODY && c[1] <= FLOOR.y1 - BODY;
      const inWings = c[0] >= WINGS.x0 && c[0] <= WINGS.x1 && c[1] >= WINGS.y0 && c[1] <= WINGS.y1;
      const clear = onFloor && objs.every(o => dRect([c[0] / 100, c[1] / 100], o.poly) * 100 >= BODY);
      r += clear || inWings ? '.' : '#'; d += inWings ? 'y' : '#';
    }
    rows.push(r); drive.push(d);
  }
  const grid = { x0: 0, y0: 0, cell: CELL, nx: WINDOW.nx, ny: WINDOW.ny, body: BODY, rows, src: `tools/sim/range.ts: the floor ${FLOOR.x1 / 100} × ${FLOOR.y1 / 100} m, a cell free when its centre is ${BODY} cm clear of the floor's edge and of every piece; the wings x ${WINGS.x0 / 100}–${WINGS.x1 / 100} m all free and unreachable from the floor` };
  const g = gridOf({ grid }), free = (p: V2) => g.free(Math.floor(p[0] / CELL), Math.floor(p[1] / CELL));
  const nearestFree = (p: V2): V2 => {
    let best: V2 | null = null, bd = Infinity;
    const ci = Math.floor(p[0] / CELL), cj = Math.floor(p[1] / CELL);
    for (let i = ci - 6; i <= ci + 6; i++) for (let j = cj - 6; j <= cj + 6; j++) {
      if (!g.free(i, j)) continue; const c: V2 = [i * CELL + CELL / 2, j * CELL + CELL / 2], d = Math.hypot(c[0] - p[0], c[1] - p[1]);
      if (d < bd - 1e-9) { bd = d; best = c; }
    }
    if (!best || bd > 100) throw new Error(`${layout}: no free ground within 1 m of ${p}`);
    return best;
  };
  const cover = pointsOf(objs.map(({ id, kind, poly, h }) => ({ id, kind, poly, h })), g).map((c, i) => ({ id: `cv-${String(i + 1).padStart(3, '0')}`, ...c }));

  type Place = { at: number[]; stand?: number[]; src: string; reason?: string };
  const places: Record<string, Place> = {};
  for (const [op, at] of Object.entries(CREW)) places[`crew:${op}`] = { at, src: 'brief 029: the cell of four on a line across the range at its near end, 1.5 m apart' };
  places['range:mark'] = { at: MARK, src: 'brief 029: the centre of the cell\'s line; every spawn distance is measured from it, and the fight AI reads it as its objective (site getaway.car: no car in the range)' };
  places['range:entry'] = { at: ENTRY, src: 'brief 029: the far end, where the response unit walks in from' };
  places['range:retreat'] = { at: RETREAT, src: 'brief 029: the far corner, where broken rivals run (site rivals.park)' };
  const R = (a: V2, b: V2) => Math.round(Math.hypot(a[0] - b[0], a[1] - b[1]));
  for (const [name, r] of Object.entries(PLACEMENTS)) SLOT_ANGLES.forEach((deg, k) => {
    const a = (deg * Math.PI) / 180, ideal: V2 = [Math.round(MARK[0] + r * Math.cos(a)), Math.round(MARK[1] + r * Math.sin(a))], s = nearestFree(ideal);
    places[`spawn:${name}:${k + 1}`] = { at: ideal, ...(s[0] !== ideal[0] || s[1] !== ideal[1] ? { stand: s } : {}), src: `brief 029 spawn ${name}: ${r / 100} m from the mark, ${deg}° off the firing line`, reason: `stands ${(R(s, MARK) / 100).toFixed(2)} m from the mark` };
  });
  const shield = cover.filter(c => c.against === 'fire' && c.q >= RULES.qPartial && protects(c, MARK) && dist(c.at, MARK) >= COVER_BAND.min && dist(c.at, MARK) <= COVER_BAND.max)
    .sort((a, b) => Math.abs(dist(a.at, MARK) - COVER_BAND.want) - Math.abs(dist(b.at, MARK) - COVER_BAND.want) || (a.id < b.id ? -1 : 1));
  if (shield.length < 6) throw new Error(`${layout}: only ${shield.length} cover points shield against the mark ${COVER_BAND.min / 100}–${COVER_BAND.max / 100} m out (want 6)`);
  shield.slice(0, 6).forEach((c, k) => { places[`spawn:cover:${k + 1}`] = { at: c.at, src: `brief 029 spawn cover: the cover point ${c.id} (${c.of}, q ${c.q}, ${c.q >= RULES.qFull ? 'full' : 'partial'}) that shields against the mark`, reason: `stands ${(dist(c.at, MARK) / 100).toFixed(2)} m from the mark` }; });
  // the wings: the loader's Handover anchors, parked where no walk from the floor reaches (the engine's range mode never reads them)
  const wing = (k: number, y: number): V2 => [5500 + wx + (k % 3) * 200 - 200, y];
  const W: Record<string, V2> = {
    'wings:guard-post': wing(0, 300), 'wings:guard-cafe': wing(1, 300), 'wings:vale-exit': wing(2, 300), 'wings:vale-car': wing(0, 700), 'wings:vale-service': wing(1, 700),
    'wings:car-pickup': [5500 + wx, 1200], 'wings:car-driver': [5100 + wx, 1200], 'wings:car-service': [5500 + wx, 2400], 'wings:car-block': [5500 + wx, 1800],
    'wings:rivals-entry': wing(0, 3000), 'wings:rivals-front': wing(1, 3000), 'wings:security-entry': wing(0, 3400), 'wings:security-via': wing(1, 3400), 'wings:security-to': wing(2, 3400),
    'wings:fight-entry': wing(0, 3800), 'wings:fight-guard': wing(1, 3800), 'wings:ambush-1': wing(2, 3800), 'wings:police-1': wing(2, 3000),
    'wings:patrol-a': [5900 + wx, 400], 'wings:patrol-b': [5900 + wx, 1000], 'wings:patrol-reroute': [5900 + wx, 1600],
  };
  for (const [id, at] of Object.entries(W)) places[id] = { at, src: 'brief 029: a Handover anchor the site format requires, parked in the wings; the range never reads it' };
  const wings = 'brief 029: the site format\'s Handover key, parked in the wings; the range never reads it';
  const sorted = Object.fromEntries(Object.entries(places).sort(([a], [b]) => (a < b ? -1 : 1)));
  const pointsJson = JSON.stringify(cover);
  const site = {
    schema: 'suborned.sim.site/1', mission: 'range',
    frame: `the fight range (brief 029): integer centimetres, x along the range from the cell's end, y across; the floor x 0–${FLOOR.x1 / 100} m, y 0–40 m; the wings x ${WINGS.x0 / 100}–${WINGS.x1 / 100} m. No clock: the range has no schedule (the schedule below is the format's, never read).`,
    from: [`tools/sim/range.ts layout ${layout}`, 'tools/cover/gen.ts pointsOf (cover points, arcs, quality)'],
    grid,
    occluders: objs.map(o => ({ id: o.id, z: 0, h: Math.round(SIZE[o.piece.kind].h * 100), poly: o.poly.map(([x, y]) => [Math.round(x * 100), Math.round(y * 100)]), src: SIZE[o.piece.kind].label })),
    places: sorted,
    cameras: [
      { id: 'entrance', at: [6000 + wx, 100], facing: [0, -100], reach: 100, half: 0, src: wings },
      { id: 'pickup', at: [6000 + wx, 3900], facing: [0, 100], reach: 100, half: 0, src: wings },
    ],
    patrol: { a: 'wings:patrol-a', b: 'wings:patrol-b', facing: [100, 0], reroute: 'wings:patrol-reroute', rerouteFacing: [100, 0], src: wings },
    guard: { post: 'wings:guard-post', facing: [100, 0], cafe: 'wings:guard-cafe', cafeFacing: [100, 0], src: wings },
    vale: { exit: 'wings:vale-exit', car: 'wings:vale-car', service: 'wings:vale-service', src: wings },
    car: { pickup: 'wings:car-pickup', driver: 'wings:car-driver', service: 'wings:car-service', block: 'wings:car-block', serviceFacing: [0, 100], blockFacing: [0, 100], src: wings },
    rivals: { park: 'range:retreat', entry: 'wings:rivals-entry', front: 'wings:rivals-front', src: 'brief 029: park is where broken rivals run (range:retreat); entry and front are the format\'s, in the wings' },
    security: { entry: 'wings:security-entry', via: 'wings:security-via', to: 'wings:security-to', src: wings },
    getaway: { car: 'range:mark', exit: { polyline: [[5500 + wx, 400], [5500 + wx, 3600]], w: 6, src: wings }, src: 'brief 029: no car; the fight AI\'s objective (the car it fights over in the Handover) is the range\'s mark' },
    fight: { ambush: ['wings:ambush-1'], police: ['wings:police-1'], entry: 'wings:fight-entry', guard: 'wings:fight-guard', src: wings },
    schedule: { rivalsArrive: 3600, chime: 3600, driverLast: 3597, valeExits: 3598, valeAtCar: 3599, over: 3600, blockMoveS: 1, blockHoldS: 1, src: 'brief 029: the format\'s; the range has no schedule. over 3600 s: a script without until plays an hour' },
    purse: 0, purseSrc: 'brief 029: the range buys nothing',
    arms: 3, armsSrc: 'brief 029: as the Handover (GDD v0.22.0 §07: all four armed, Arms 3), so the lab loadouts check the same',
    cast: Object.fromEntries(['waitress', 'driver', 'guard', 'councillor'].map((role, k) => [role, { id: `wings-${k + 1}`, name: '(none)', temperament: 'mercenary', need: 'cash', secret: '', lover: '' }]).concat([['src', wings] as any])),
    cover: { from: 'tools/sim/range.ts (tools/cover/gen.ts pointsOf)', sha256: createHash('sha256').update(pointsJson).digest('hex'), src: 'brief 029: the range\'s cover points, by the Handover\'s rule (quality = height class × arc)', points: cover, blockers: [], refuge: [] },
    drive: { x0: 0, y0: 0, cell: CELL, nx: WINDOW.nx, ny: WINDOW.ny, lane: 165, rows: drive, src: wings },
  };
  return { json: JSON.stringify(site, null, 1) + '\n', site };
}

if (import.meta.main) {
  const check = process.argv.includes('--check'); let bad = 0;
  for (const [layout, file] of Object.entries(RANGE_FILES) as [keyof typeof RANGE_FILES, string][]) {
    const { json, site } = buildRange(layout);
    const path = join(root, file);
    if (check) { let old = ''; try { old = readFileSync(path, 'utf8'); } catch {} if (old !== json) { console.error(`${file} is stale: bun tools/sim/range.ts`); bad++; } continue; }
    writeFileSync(path, json);
    const pts = site.cover.points as { q: number }[];
    console.log(`${file}: ${site.occluders.length} pieces, ${pts.length} cover points (${pts.filter(p => p.q >= RULES.qFull).length} full, ${pts.filter(p => p.q < RULES.qFull).length} partial)`);
  }
  process.exit(bad ? 1 : 0);
}
