// The Handover's ground as the C# engine reads it (brief 011 A, Keel): sim/data/handover.site.json, block frame, integer centimetres.
// Built from the plan's walk blockers and the civic block's solids (the same ground tools/plan/walk.ts and walk-fine.ts walk on), the
// civic buildings as sight occluders, the mission's points, timeline and window, and the engine's own points (cameras, the Tech's
// cabinet, the patrol's sweep, the rivals' and the response unit's entries), each with the reason it stands where it does. Those
// are Waymark's to adopt into mission.json (Suture O-6); until then they live here. Deterministic: the same inputs give the same
// bytes. The file names its sources but holds no hash of them: an edit to the plan or the mission that leaves this ground as it
// was leaves the file, and every replay's start hash, as they were; tools/sim/sim.test.ts fails on any edit that changes it.
// Run: bun run sim:site (writes the file); bun test tools/sim (checks it).
import { readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { derive } from '../plan/plan';
import { civic, toPlanFn } from '../plan/civic';
import { blockersOf } from '../plan/walk';
import { Solids } from '../plan/walk-solids';
import { type V, dist, inPoly } from '../plan/geom';

const root = join(import.meta.dir, '..', '..');
export const SITE_FILE = 'sim/data/handover.site.json';
const PLAN = 'contracts/world/harbour-district.plan.json';
const MISSION = 'contracts/missions/handover/mission.json';
const CELL = 0.5; // m
const WINDOW = { x0: -52, x1: 112, y0: -66, y1: 16 }; // block frame, m: the square, the café, the Office, Apron Road and the street mouths

const cm = (m: number) => Math.round(m * 100);
const P = (p: V) => [cm(p[0]), cm(p[1])];

type Place = { at: number[]; src: string; reason?: string };

export function buildSite(): { json: string; site: unknown } {
  const read = (f: string) => readFileSync(join(root, f), 'utf8');
  const planText = read(PLAN), plan = JSON.parse(planText);
  const blockText = read(plan.civic.block), block = JSON.parse(blockText);
  const missionText = read(MISSION), mission = JSON.parse(missionText);
  const d = derive(plan), civ = civic(plan.civic, block), toPlan = toPlanFn(plan.civic);
  const { polys, rounds } = blockersOf(plan, d, civ);
  const solids = new Solids(civ.solids);

  // ---- the walk grid: a cell is free when its centre is outside every blocker and BODY clear of every ground solid
  const nx = Math.round((WINDOW.x1 - WINDOW.x0) / CELL), ny = Math.round((WINDOW.y1 - WINDOW.y0) / CELL);
  const centre = (i: number, j: number): V => [WINDOW.x0 + (i + 0.5) * CELL, WINDOW.y0 + (j + 0.5) * CELL];
  const boxed = polys.map((b) => ({ b, x0: Math.min(...b.map((v) => v[0])), x1: Math.max(...b.map((v) => v[0])), y0: Math.min(...b.map((v) => v[1])), y1: Math.max(...b.map((v) => v[1])) }));
  const blockedAt = (q: V) => {
    const p = toPlan(q);
    if (boxed.some((k) => p[0] >= k.x0 && p[0] <= k.x1 && p[1] >= k.y0 && p[1] <= k.y1 && inPoly(p, k.b))) return true;
    if (rounds.some((l) => dist(p, l.c) < l.r)) return true;
    return !solids.clear(p, p, solids.near(p[0], p[0], p[1], p[1]));
  };
  const rows: string[] = [];
  for (let i = 0; i < nx; i++) { let r = ''; for (let j = 0; j < ny; j++) r += blockedAt(centre(i, j)) ? '#' : '.'; rows.push(r); }
  const free = (q: V) => { const i = Math.floor((q[0] - WINDOW.x0) / CELL), j = Math.floor((q[1] - WINDOW.y0) / CELL); return i >= 0 && j >= 0 && i < nx && j < ny && rows[i]![j] === '.'; };
  /** The nearest free cell centre to q (ties: lowest i, then j), for a point that stands on a face or beside a solid. */
  const nearestFree = (q: V): V => {
    const ci = Math.floor((q[0] - WINDOW.x0) / CELL), cj = Math.floor((q[1] - WINDOW.y0) / CELL);
    let best: V | undefined, bd = Infinity;
    for (let r = 0; r <= 12; r++) {
      for (let i = ci - r; i <= ci + r; i++) for (let j = cj - r; j <= cj + r; j++) {
        if (i < 0 || j < 0 || i >= nx || j >= ny || rows[i]![j] !== '.') continue;
        const c = centre(i, j), dd = dist(c, q);
        if (dd < bd) { bd = dd; best = c; }
      }
      if (best && bd <= (r + 0.5) * CELL) break;
    }
    if (!best) throw new Error(`no free ground within 6 m of ${q}`);
    return best;
  };

  // ---- sight: the civic buildings and their towers at their heights, and the built plots around the square
  const toBlock = (p: V): V => { // invert plan = o + bx·ax + by·ay
    const o: V = plan.civic.blockToPlan.origin, ax: V = plan.civic.blockToPlan.x, ay: V = plan.civic.blockToPlan.y;
    const det = ax[0] * ay[1] - ax[1] * ay[0], dx = p[0] - o[0], dy = p[1] - o[1];
    return [(dx * ay[1] - dy * ay[0]) / det, (ax[0] * dy - ax[1] * dx) / det];
  };
  const inWindow = (poly: V[]) => poly.some((p) => p[0] >= WINDOW.x0 - 20 && p[0] <= WINDOW.x1 + 20 && p[1] >= WINDOW.y0 - 20 && p[1] <= WINDOW.y1 + 20);
  const occluders: { id: string; z: number; h: number; poly: number[][]; src: string }[] = [];
  for (const b of block.buildings) {
    occluders.push({ id: b.id, z: cm(b.z ?? 0), h: cm(b.height), poly: b.polygon.map(P), src: `civic-block.json buildings.${b.id}` });
    for (const t of b.towers ?? []) if (t.polygon) occluders.push({ id: `${b.id}/${t.id}`, z: cm(b.z ?? 0), h: cm(t.height), poly: t.polygon.map(P), src: `civic-block.json buildings.${b.id}.towers.${t.id}` });
  }
  const civicPolys = new Set(civ.buildings.flatMap((b: { poly: V[]; towers?: { poly?: V[] }[] }) => [b.poly, ...(b.towers ?? []).flatMap((t) => (t.poly ? [t.poly] : []))]));
  let plotN = 0;
  for (const poly of polys) {
    if (civicPolys.has(poly) || plan.water.includes(poly)) continue;
    const bp = poly.map(toBlock);
    if (!inWindow(bp)) continue;
    occluders.push({ id: `plot-${String(++plotN).padStart(2, '0')}`, z: 0, h: 1500, poly: bp.map(P), src: 'plan built plot (tools/plan/walk.ts blockersOf); no height in the plan, stood at 15 m: any built plot blocks an eye-level sightline' });
  }

  // ---- places: the mission's points as they stand, then the engine's own (O-6, for Waymark)
  const dest = (name: string): V => { const x = mission.destinations.find((d: { name: string }) => d.name === name); if (!x) throw new Error(`mission.json has no destination ${name}`); return x.at; };
  const places: Record<string, Place> = {};
  const put = (id: string, at: V, src: string, reason?: string) => { places[id] = { at: P(at), src, ...(reason ? { reason } : {}) }; };
  for (const d of mission.destinations) put(`d:${d.name}`, d.at, `mission.json destinations "${d.name}"`);
  for (const c of mission.crew) put(`crew:${c.name.toLowerCase()}`, c.at, `mission.json crew ${c.name}: §07 cell at getaway car`);
  const cab = block.structures.find((s: { id: string }) => s.id === 'civic-control-cabinet');
  const cabC: V = [cab.polygon.reduce((a: number, p: V) => a + p[0], 0) / cab.polygon.length, cab.polygon.reduce((a: number, p: V) => a + p[1], 0) / cab.polygon.length];
  put('cabinet', cabC, 'civic-block.json structures civic-control-cabinet (centre)', 'the Tech\'s cabinet (§03 Loop, Reroute): the block\'s one control cabinet, in the square 28 m from the public door and outside every cone, as the top-down test\'s cabinet stood about 31 units from the door.');
  put('beside-inez', [-8.2, -29.0], 'Keel', 'Listen needs the Scout within 3 m of Inez (rulebook listen-reach-m [B]); the Courtyard point is 3.5 m from the café door, so this stands 2.1 m from her, between the café palms.');
  put('rival-park', [84.0, -31.0], 'GDD §07: 3:10 rival car parks near pickup', 'on Apron Road, 5.8 m south of the official car in the official bay.');
  put('rival-entry', [36.0, 10.0], 'GDD §05 rivals: own schedule; the top-down test: the rivals enter from the east street', 'the middle east street (block x 30–42 open to the east), the nearest street mouth to the Office on the east side.');
  put('security-entry', [-46.0, -32.0], 'GDD §07 final fight, loud: police from west street', '§07\'s "west street" is the top-down test\'s compass. The real block is built on its west side (y < −62); its one street on the café and getaway side is the north street (block x < −38, y −36 to −28), so the response unit enters there.');
  put('patrol-a', [40.75, -34.0], 'the top-down test: the patrol sweeps 11 units in front of the public door, from 4 to one side of it to 16 to the other', 'the same offsets from the real public door (51.75, −30): 11 m north of it, 4 m west to 16 m east.');
  put('patrol-b', [40.75, -14.0], 'as patrol-a', 'the far end of the sweep.');
  put('patrol-reroute', [20.0, -58.0], 'the top-down test: a rerouted patrol waits at the far corner from the door', 'the square\'s west edge, 33 m from the public door and out of its sightlines.');
  const blockSpot: V = [75.7, -36.4];
  put('block-spot', blockSpot, 'GDD §07 route B: driver blocks pickup camera 2:30', 'on the sightline from the pickup camera to the official car, 40% of the way from the camera, on the pavement between the Office and the bay.');
  for (const [id, p] of Object.entries(places)) if (!free([p.at[0]! / 100, p.at[1]! / 100])) (p as Place & { stand?: number[] }).stand = P(nearestFree([p.at[0]! / 100, p.at[1]! / 100]));

  // ---- cameras (O-6): mount, facing, reach and half-width, all cm; a point is watched inside the cone on a clear sightline
  const cameras = [
    { id: 'entrance', at: P([49.5, -41.5]), facing: P([51.75 - 49.5, -30 - -41.5]), reach: cm(22), half: cm(9), src: 'GDD §05 cameras: fixed visible cones; §07 route A: Tech loops the entrance camera', reason: 'a pole at the forecourt\'s west corner, looking at the public door across the foot of its steps: the door, the steps and the Office approach in the cone, the tower beside the door clear of the sightline.' },
    { id: 'pickup', at: P([74.0, -40.0]), facing: P([78.2 - 74.0, -31 - -40.0]), reach: cm(18), half: cm(9), src: 'GDD §07 route B: the pickup camera', reason: 'a pole at the Office\'s south-west corner looking at the official car in the bay; the Office and its official tower stand outside the sightline.' },
  ];

  // ---- the mission's schedule, read from mission.json by time and checked by its words
  const at = (t: string, word: RegExp) => {
    const e = mission.timeline.find((x: { at: string }) => x.at === t);
    if (!e || !word.test(e.what)) throw new Error(`mission.json timeline ${t} is not "${word}" (it reads ${e?.what})`);
    const [m, s] = t.split(':').map(Number); return m! * 60 + s!;
  };
  const win = mission.windows.find((w: { id: string }) => w.id === 'front-door');
  const schedule = {
    rivalsArrive: at('3:10', /rival car/), chime: at('5:30', /chime/), driverLast: at('5:40', /turn driver/), valeExits: at('5:50', /Vale exits/), valeAtCar: at('6:10', /Vale at car/), over: at('8:00', /over/),
    guardWalkS: win.walk.s, guardStayS: win.stayS,
    blockMoveS: 8, blockHoldS: 150,
    src: 'mission.json timeline (GDD §07) and windows.front-door; blockHoldS: §07 route B, driver blocks pickup camera 2:30; blockMoveS: the top-down test, the car takes 8 s to block the camera. over: §07 8:00 (the top-down test ran to 9:50; Suture M-1, the rulebook takes §07)',
  };

  // ---- the cast the 28 scenarios play: the r013 people M-2 pinned at the 17:00 start; mission.json carries no pins yet (Waymark)
  const cast = {
    waitress: { id: 'p-cafe-11', name: 'Inez Bermudez', temperament: 'idealist', need: 'debt' },
    driver: { id: 'p-records-office-13', name: 'Alba Ferrer', temperament: 'idealist', need: 'cash' },
    guard: { id: 'p-records-office-02', name: 'Ulrich Sandvik', temperament: 'mercenary', need: 'debt' },
    councillor: { id: 'p-records-office-01', name: 'Bogdan Vale', temperament: 'mercenary', need: 'advancement' },
    src: 'people run r013 (accepted, brief 010), as M-2 cast it at the 17:00 start (Keel, 2026-10-15). Sky 2026-10-15: Inez is an Idealist and Pay is the lesson. mission.json has no pins yet; when Waymark pins, the site takes them.',
  };

  const site = {
    schema: 'suborned.sim.site/1',
    mission: 'handover',
    frame: 'civic-block.json block frame in integer centimetres: x south, y east. Mission clock in seconds from 0:00 (the engine ticks at 10 Hz).',
    from: [PLAN, plan.civic.block, MISSION, 'tools/plan/walk.ts blockersOf', 'tools/plan/walk-solids.ts'],
    grid: { x0: cm(WINDOW.x0), y0: cm(WINDOW.y0), cell: cm(CELL), nx, ny, body: cm(solids.r), rows, src: 'tools/plan/walk.ts blockersOf (built plots, civic footprints and towers, water, landmarks) and walk-solids.ts (the block\'s ground solids, BODY clearance): a cell is free when its centre is' },
    occluders,
    places,
    cameras,
    patrol: { a: 'patrol-a', b: 'patrol-b', facing: [100, 0], reroute: 'patrol-reroute', rerouteFacing: [0, -100], src: 'the top-down test: the patrol faces the building it guards; here south, toward the public door' },
    guard: { post: 'd:Office entrance', facing: [-100, 0], cafe: 'd:Café door', cafeFacing: [-100, 0], src: 'GDD §05 guard on door routine (café stop pre-shift); mission.json windows.front-door; he faces the square from the door, and the café at it' },
    vale: { exit: 'd:Official entrance', car: 'd:Official drop-off', service: 'd:Service lane', src: 'GDD §07 5:50 Vale exits, 6:10 at car (service lane if the driver is turned); mission.json roles.councillor leavesTo' },
    car: { pickup: 'd:Official drop-off', driver: 'd:Meet driver', service: 'd:Service lane', block: 'block-spot', src: 'GDD §07; mission.json roles.driver' },
    rivals: { park: 'rival-park', entry: 'rival-entry', front: 'd:Office approach', src: 'GDD §05 rivals; the top-down test: drawn by gunfire they make for the front of the Office' },
    security: { entry: 'security-entry', via: 'd:Office approach', to: 'd:Service lane', src: 'the top-down test: the response unit crosses to the Office front, then the service side' },
    getaway: { car: 'd:Getaway bay', src: 'GDD §07 win: Vale alive at the getaway car; mission.json destinations Getaway bay' },
    schedule,
    purse: 100000,
    purseSrc: 'GDD §07: Purse $100,000',
    cast,
  };
  const json = pretty(site) + '\n';
  return { json, site };
}

/** JSON with one key per line, and arrays of numbers (points, polygons) kept on one line. */
function pretty(v: unknown, ind = ''): string {
  const flat = (x: unknown): boolean => typeof x === 'number' || (Array.isArray(x) && x.every(flat));
  if (Array.isArray(v)) {
    if (v.every(flat)) return JSON.stringify(v);
    return `[\n${v.map((x) => ind + ' ' + pretty(x, ind + ' ')).join(',\n')}\n${ind}]`;
  }
  if (v && typeof v === 'object') {
    const es = Object.entries(v as Record<string, unknown>);
    return `{\n${es.map(([k, x]) => `${ind} ${JSON.stringify(k)}: ${pretty(x, ind + ' ')}`).join(',\n')}\n${ind}}`;
  }
  return JSON.stringify(v);
}

if (import.meta.main) {
  const { json } = buildSite();
  writeFileSync(join(root, SITE_FILE), json);
  console.log(`wrote ${SITE_FILE} (${json.length} bytes)`);
}
