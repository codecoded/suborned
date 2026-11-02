// The Handover's ground as the C# engine reads it (brief 011 A, Keel): sim/data/handover.site.json, block frame, integer centimetres.
// Built from the plan's walk blockers and the civic block's solids (the same ground tools/plan/walk.ts and walk-fine.ts walk on), the
// civic buildings as sight occluders, the mission's points, timeline and window, and the engine's own points (cameras, the Tech's
// cabinet, the patrol's sweep, the rivals' and the response unit's entries), each with the reason it stands where it does. Those
// are Waymark's to adopt into mission.json (Suture O-6); until then they live here. Deterministic: the same inputs give the same
// bytes. The file names its sources but holds no hash of them: an edit to the plan or the mission that leaves this ground as it
// was leaves the file, and every replay's start hash, as they were; tools/sim/sim.test.ts fails on any edit that changes it.
// Run: bun run sim:site (writes the file, the people extract, the takedown places (tools/sim/takedown.ts) and the provisional file with the security level (below)); bun test tools/sim (checks them).
import { existsSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { derive } from '../plan/plan';
import { civic, toPlanFn } from '../plan/civic';
import { blockersOf } from '../plan/walk';
import { CELL, WINDOW } from '../plan/walk-grid';
import { COLLIDE_FILE, floorRows, simGrid } from './solids';
import { inPoly, type V } from '../plan/geom';
import { castRoles } from '../missions/handover/cast';
import { castRules } from '../missions/handover/overlay';
import { summonRules } from '../missions/handover/summon';
import { createHash } from 'node:crypto';
import { buildPeople, PEOPLE_OUT } from './people';
import { buildTakedown, TAKEDOWN_FILE } from './takedown';
import { buildExit, blockM, checkExit } from './getaway';
import { driveGrid, driveFree, type Drive } from './drive';
import { buildRivals, buildStands, checkRivals, checkStands } from './stands';

const root = join(import.meta.dir, '..', '..');
export const SITE_FILE = 'sim/data/handover.site.json';
export const PROVISIONAL_FILE = 'sim/data/handover.provisional.json';
/** The people the Handover is cast from: the people file `bun run people` writes from the promoted cast. */
export const PROMOTED_PEOPLE = 'out/people/harbour-district.people.json';
/** Where the cast comes from. A fixture is labelled, in the site and the people extract alike. */
export type Source = { people: string; fixture?: string };
const PLAN = 'contracts/world/harbour-district.plan.json';
const MISSION = 'contracts/missions/handover/mission.json';

const cm = (m: number) => Math.round(m * 100);
const P = (p: V) => [cm(p[0]), cm(p[1])];

type Place = { at: number[]; src: string; reason?: string };

/** Brief 055 (Keel 2026-10-23): what of the drawn solids (tools/sim/solids.ts) the site holds. Both are built and measured; both are held off until their adoption:
 *  `drawnWalk`: the drawn solids block the walk grid. Off: the grid is the block-only grid (buildings, plots, the block's own ground solids), with the floor per cell.
 *    Held for (kit.collide.json v7, 2026-10-23): PottedPalm_Small is measured as a solid of r 0.81 m from the pavement up and its fronds as boxes to 1.73 m, so the
 *    café's palms close Inez's way to tables 3 and 6 and put 23 to 44 m on her walks to 0, 1 and 5 (her rounds are walked on the block-only grid). With the palm
 *    as its pot only (r 0.35, probe out/scratch/keel/b055/palmfix.ts) every table stays open and her walks grow 0.3 to 6 m: Vela's piece to fix, not the grid.
 *  `drawnOccluders`: the drawn volumes that span eye height are sight occluders. Held for: under the rulebook's one eye height (eye-cm) a camera on a pole or a mast
 *    sees at 1.6 m like a person, so the two 2 m topiaries at the Office door cut the entrance camera's line to the door and the café's planting cuts the Records
 *    Square mast; 10 scenarios move.
 *  On: `bun run sim:site --drawn-walk --drawn-occluders` (then `bun run cover`, `bun run sim:site`, re-record). The site records which (grid.drawn); a plain `bun run sim:site` keeps what the committed site has. */
export type SiteOptions = { drawnWalk?: boolean; drawnOccluders?: boolean;
  /** Brief 059 (Splice): fold the getaway stands and the rival car's way in into the site itself (`getaway.stands`, `rivals.car`, `rivals.posts`). Off, they are the second file sim/data/handover.stands.json and the site keeps the shape the engine reads (Site.cs rejects a key it does not know).
   *  Keel's 059 round turns it on (`bun run sim:site --fold-stands`) when the engine reads them from the site. */
  foldStands?: boolean;
  /** Brief 077 (Keel; Sky 2026-10-30, planting is see-through): grid.collide is solidDigest (tools/sim/solids.ts: only the pieces that stand a solid in the
   *  window; a render-only piece never moves it) and grid.drawn says so (`digest: "solid"`). Off, it is the digest every site before 077 carries (every piece placed,
   *  render-only ones included), so a plain rebuild stays byte-identical. Turned on once, at the 077 adoption (`bun run sim:site --solid-digest`), then kept. */
  solidDigest?: boolean };
/** The options the committed site was built with (grid.drawn), so a check rebuilds it the same way. */
export const optionsOfSite = (): SiteOptions => {
  const site = JSON.parse(readFileSync(join(root, SITE_FILE), 'utf8')), d = site.grid?.drawn;
  return { drawnWalk: !!d?.walk, drawnOccluders: !!d?.sight, foldStands: !!site.getaway?.stands, solidDigest: d?.digest === 'solid' };
};
/** Brief 059: the getaway stands and the rival car's way in, as the committed files hold them: in the site when folded, else the second file. */
export const STANDS_FILE = 'sim/data/handover.stands.json';
export const readStands = (): { getaway: { stands: unknown[] }; rivals: { car: Record<string, unknown>; posts: Record<string, unknown> } } => {
  const site = JSON.parse(readFileSync(join(root, SITE_FILE), 'utf8'));
  if (site.getaway?.stands) return { getaway: { stands: site.getaway.stands }, rivals: { car: site.rivals.car, posts: site.rivals.posts } };
  return JSON.parse(readFileSync(join(root, STANDS_FILE), 'utf8'));
};
export function buildSite(src: Source = { people: PROMOTED_PEOPLE }, opts: SiteOptions = {}): { json: string; site: unknown; stands: string } {
  const read = (f: string) => readFileSync(join(root, f), 'utf8');
  const planText = read(PLAN), plan = JSON.parse(planText);
  const blockText = read(plan.civic.block), block = JSON.parse(blockText);
  const missionText = read(MISSION), mission = JSON.parse(missionText);
  const d = derive(plan), civ = civic(plan.civic, block), toPlan = toPlanFn(plan.civic);
  const { polys } = blockersOf(plan, d, civ);
  const rulebook = JSON.parse(read('contracts/rules/rulebook.json')), eyeCm: number = rulebook.values['eye-cm'].v;
  // brief 055: the walk grid with the drawn solids (tools/sim/solids.ts), a floor per cell and the drawn occluders
  const G = simGrid(plan, d, civ, eyeCm / 100, WINDOW, CELL, !!opts.drawnWalk), { solids, nx, ny, rows, free, nearestFree } = G;
  // brief 077: a kit piece the window places with no kit.collide.json entry is unmeasured, never taken as render-only nor guessed: no site until it is declared
  if (G.drawn.unmeasured.length) throw new Error(`${COLLIDE_FILE} has no entry for ${G.drawn.unmeasured.join(', ')}, placed in the Handover window: measure it (bun run collide) or declare it render-only (volumes [])`);

  // ---- sight: the civic buildings and their towers at their heights, and the built plots around the square (each behind its building line: the pavement is not a wall)
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
    occluders.push({ id: `plot-${String(++plotN).padStart(2, '0')}`, z: 0, h: 1500, poly: bp.map(P), src: 'plan built plot behind its building line (tools/plan/walk.ts blockersOf); no height in the plan, stood at 15 m: any built plot blocks an eye-level sightline' });
  }

  // the drawn solids at eye height on pieces that stop sight (brief 055 step 2): a building's own footprint is already above, so its drawn copy is not repeated
  const civicIds = new Set(occluders.map((o) => `civic/${o.id}`));
  for (const o of opts.drawnOccluders ? G.occluders : []) {
    if (civicIds.has(o.id.replace(/#\d+$/, ''))) continue; // brief 072: a building's drawn volumes come as id#k when it keeps more than one
    occluders.push({ id: `solid:${o.id}`, z: cm(o.z), h: cm(o.h), poly: o.poly.map(P), src: `${COLLIDE_FILE} volumes spanning eye height (rulebook eye-cm ${eyeCm}), placed as the world places the piece` });
  }

  // ---- places: the mission's points as they stand, then the engine's own (O-6, for Waymark)
  const dest = (name: string): V => { const x = mission.destinations.find((d: { name: string }) => d.name === name); if (!x) throw new Error(`mission.json has no destination ${name}`); return x.at; };
  const places: Record<string, Place> = {};
  const put = (id: string, at: V, src: string, reason?: string) => { places[id] = { at: P(at), src, ...(reason ? { reason } : {}) }; };
  for (const d of mission.destinations) put(`d:${d.name}`, d.at, `mission.json destinations "${d.name}"`);
  for (const c of mission.crew) put(`crew:${c.name.toLowerCase()}`, c.at, `mission.json crew ${c.name}: §07 cell at getaway car`);
  // the mission's own points (mission.json `points`, Waymark adopted them from here, brief 011 B, Suture O-6), in their order
  const centreOf = (sid: string): V => {
    const st = block.structures.find((s: { id: string }) => s.id === sid);
    if (!st) throw new Error(`mission.json points: civic-block.json has no structure ${sid}`);
    return [st.polygon.reduce((a: number, p: V) => a + p[0], 0) / st.polygon.length, st.polygon.reduce((a: number, p: V) => a + p[1], 0) / st.polygon.length];
  };
  for (const pt of mission.points as { id: string; at?: V; structure?: string; src: string; reason: string }[]) put(pt.id, pt.at ?? centreOf(pt.structure!), pt.src, pt.reason);
  // the final fight (§07 [S]): its posts are mission.json points too (ambush-*, police-*, guard-ally; Waymark moved them off the road onto cover)
  const ambush = (mission.points as { id: string }[]).filter((p) => /^ambush-\d+$/.test(p.id)), police = (mission.points as { id: string }[]).filter((p) => /^police-\d+$/.test(p.id));
  for (const [id, p] of Object.entries(places)) if (!free([p.at[0]! / 100, p.at[1]! / 100])) (p as Place & { stand?: number[] }).stand = P(nearestFree([p.at[0]! / 100, p.at[1]! / 100]));

  // ---- cameras (O-6): mount, facing, reach and half-width, all cm; a point is watched inside the cone on a clear sightline
  // mission.json `cameras`: each faces the destination it looks at
  // brief 055 §7: each camera's kind sets its lens height (rulebook perception.mount.cameras); the mission's cameras are pole cameras (the plan's
  // watchers.types.pole-camera) until mission.json names a kind on each (Waymark)
  const cameras = (mission.cameras as { id: string; kind?: string; at: V; looksAt: string; reachM: number; halfM: number; src: string; reason: string }[]).map((c) => {
    const to = dest(c.looksAt);
    return { id: c.id, kind: c.kind ?? 'pole-camera', at: P(c.at), facing: P([to[0] - c.at[0], to[1] - c.at[1]]), reach: cm(c.reachM), half: cm(c.halfM), src: c.src, reason: c.reason };
  });

  // ---- the mission's schedule, read from mission.json by its words, so a retimed entry (Waymark's close, 8:00 → 8:30) moves the engine
  const at = (_was: string, word: RegExp) => {
    const es = mission.timeline.filter((x: { what: string }) => word.test(x.what));
    if (es.length !== 1) throw new Error(`mission.json timeline: ${es.length} entries read "${word}", the engine needs exactly one`);
    const [m, s] = String(es[0].at).split(':').map(Number); return m! * 60 + s!;
  };
  // the guard's visit (mission.json windows rule summon) is the engine's: its grid walk and the rulebook's stay; the site writes neither
  const schedule = {
    rivalsArrive: at('3:10', /rival car parks/), chime: at('4:30', /chime/), driverLast: at('4:40', /turn the driver/), valeExits: at('4:50', /comes out of the official door/), valeAtCar: at('5:10', /at his car, in by/), over: at('7:30', /\(over\)$/), guardMark: 60, // GDD v0.25.0 §07's words (brief 059): each pattern matches one entry
    blockMoveS: 8, blockHoldS: 150,
    src: 'guardMark: the door guard\'s first check-in mark, 1:00, fixed when the shift came forward a minute (Sky 2026-10-30, brief 080 pacing: marks 1:00, 2:30, 4:00); mission.json timeline (GDD §07) and windows.front-door; blockHoldS: §07 route B, driver blocks pickup camera 2:30; blockMoveS: the top-down test, the car takes 8 s to block the camera. over: the timeline\'s closing entry, read by its words (§07 8:00; Sky 2026-10-15 moves it to 8:30, Waymark retimes mission.json)',
  };

  // ---- the cast: M-2's own casting (tools/missions/handover/cast.ts castRoles, { play: true }) on the people file, at the mission's
  // startsAt. If M-2 can't cast a role this fails, naming why; it never falls back to a person written here.
  const peopleText = readFileSync(join(root, src.people), 'utf8'), peopleIn = JSON.parse(peopleText);
  const placeOf = new Map<string, { at: V; kind: string; name: string; plot?: string }>(peopleIn.places.map((p: { id: string; at: V; kind: string; name: string; plot?: string }) => [p.id, p]));
  const casting = castRoles(peopleIn.persons, mission, {
    toPlan, placeAt: (id) => placeOf.get(id)?.at,
    polyOf: (id) => { const p = placeOf.get(id); return p?.kind === 'bay' ? civ.bays.find((b: { id: string }) => b.id === p.name)?.poly : undefined; },
    plotOf: (id) => placeOf.get(id)?.plot,
  }, castRules(), { play: true, summon: { maxWalkS: summonRules().walkMaxS } }); // the walk itself is the engine's, checked by M-2 (overlay.ts)
  const failed = casting.filter((c) => !c.ok);
  if (failed.length) throw new Error(`M-2 can't cast the Handover from ${src.people} at ${mission.clock.startsAt}: ${failed.map((c) => `${c.role}: ${c.why}`).join('; ')}`);
  type CastPerson = { id: string; name: string; temperament: string; need: string; secret?: string; ties?: { kind: string; with: string }[] };
  const byId = new Map<string, CastPerson>(peopleIn.persons.map((p: CastPerson) => [p.id, p]));
  const roleOf = new Map(casting.map((c) => [c.pinned!, c.role]));
  const cast: Record<string, unknown> = {};
  // the cast's secret and lover (Motive, briefs/011/motive-lovers.md): read from the people file's ties, never from the mission; the lover is
  // named by role when the other end is cast too ("" when none), so the engine's summons reads the city's ties
  for (const c of casting) {
    const p = byId.get(c.pinned!)!, lover = (p.ties ?? []).find((t) => t.kind === 'lover');
    cast[c.role] = { id: p.id, name: p.name, temperament: p.temperament, need: p.need, secret: p.secret ?? '', lover: lover ? (roleOf.get(lover.with) ?? '') : '' };
  }
  cast.from = src.people;
  cast.sha256 = createHash('sha256').update(peopleText).digest('hex');
  cast.startsAt = mission.clock.startsAt;
  if (src.fixture) cast.fixture = src.fixture;
  cast.src = 'tools/missions/handover/cast.ts castRoles with { play: true }, as rule M-2 casts it (brief 011, Aster): every role there when needed, tied and placed as mission.json asks';

  // ---- the car's exit lane (brief 018 §1): derived from routes.getaway and the plan road, checked here (SX-1) so a lane off the carriageway never ships
  const exitLane = buildExit(plan, d, mission, 'Getaway bay', rulebook);
  const exitBad = checkExit(exitLane, plan, d, toPlan(dest('Getaway bay')), blockM(rulebook).m);
  if (exitBad.length) throw new Error(`getaway.exit (SX-1): ${exitBad.join('; ')}`);

  // ---- where a car may be (brief 023, engine 0.8.2: rulebook car.drive): carriageways, the block's bays and its drives. The drives are the world's
  // vehicle crossings, civic-block.json drives[] (Splice, brief 023; block frame polygons): the Handover's one is service-drive, Apron Road to
  // serviceBay. Required: a block without drives has no way into serviceBay, so the site is not written (no keep-clear stand-in)
  const drives: Drive[] = ((block.drives ?? []) as { id: string; polygon: V[] }[]).map((x) => ({ id: x.id, polygon: x.polygon }));
  if (!drives.length) throw new Error('car.drive: the civic block has no drives[]: the turned driver has no way into serviceBay (brief 023)');
  // brief 023 (car.pace): the lane a car keeps to on a carriageway is the city's lane (life.json traffic.laneWidth, the traffic model's [S])
  const laneWidth = JSON.parse(read('contracts/world/harbour-district.life.json')).traffic?.laneWidth;
  if (typeof laneWidth !== 'number') throw new Error('car.pace: harbour-district.life.json has no traffic.laneWidth (the lane a car keeps to)');
  const drive = driveGrid(plan, d, block.bays, drives, laneWidth);
  // the car's heading parked in the service bay: the bay's long axis, pointing away from the drive (or the carriageway) it is entered by
  const sb = places['service-bay'];
  if (!sb) throw new Error('mission.json has no point service-bay: the turned driver has nowhere to park (brief 023)');
  const sbAt: V = [sb.at[0]! / 100, sb.at[1]! / 100];
  const bay = (block.bays as { id: string; polygon: V[] }[]).find((b) => inPoly(sbAt, b.polygon));
  if (!bay) throw new Error(`service-bay (${sbAt}) lies in no bay of the civic block`);
  const bx = bay.polygon.map((p) => p[0]), by = bay.polygon.map((p) => p[1]), lx = Math.max(...bx) - Math.min(...bx), ly = Math.max(...by) - Math.min(...by);
  const bc: V = [(Math.max(...bx) + Math.min(...bx)) / 2, (Math.max(...by) + Math.min(...by)) / 2];
  const entry = drives.find((x) => x.polygon.some((p) => inPoly(p, bay.polygon)) || bay.polygon.some((p) => inPoly(p, x.polygon)));
  const ec: V | undefined = entry && [entry.polygon.reduce((a, p) => a + p[0], 0) / entry.polygon.length, entry.polygon.reduce((a, p) => a + p[1], 0) / entry.polygon.length];
  const serviceFacing = lx >= ly ? [ec && ec[0] > bc[0] ? -100 : 100, 0] : [0, ec && ec[1] > bc[1] ? -100 : 100];
  // the car's heading parked at block-spot (Aster 2026-10-19, Latch's editor-car-block-spot.png): along the long axis of the bay it stands in (the
  // official bay's kerb), nose the way it pulled out from the pickup, so it never stands angled across the pavement
  const bs = places['block-spot'], pu = places['d:Official drop-off'];
  if (!bs || !pu) throw new Error('mission.json has no block-spot or Official drop-off: the car has nowhere to block from');
  const bsAt: V = [bs.at[0]! / 100, bs.at[1]! / 100], puAt: V = [pu.at[0]! / 100, pu.at[1]! / 100];
  const blockBay = (block.bays as { id: string; polygon: V[] }[]).find((b) => inPoly(bsAt, b.polygon));
  if (!blockBay) throw new Error(`block-spot (${bsAt}) lies in no bay of the civic block: the car would stand across the pavement`);
  const kx = blockBay.polygon.map((p) => p[0]), ky = blockBay.polygon.map((p) => p[1]);
  const blockFacing = Math.max(...kx) - Math.min(...kx) >= Math.max(...ky) - Math.min(...ky) ? [bsAt[0] >= puAt[0] ? 100 : -100, 0] : [0, bsAt[1] >= puAt[1] ? 100 : -100];

  // ---- brief 059 §5 (Splice): the getaway car's stands (each with its exit lane, spots and rival stop) and the rival car's way in, derived from the mission's points and routes and held by SX-2 and SX-3
  const driveOk = driveFree(drive), stands = buildStands(plan, d, mission, block, rulebook, places);
  const standsBad = checkStands(stands, plan, d, places, rulebook, (q) => free(q), driveOk);
  if (standsBad.length) throw new Error(`getaway.stands (SX-2): ${standsBad.join('; ')}`);
  const rivalsNew = buildRivals(plan, d, mission, places), rivalsBad = checkRivals(rivalsNew, plan, d, places, rulebook, schedule.rivalsArrive, (q) => free(q), driveOk);
  if (rivalsBad.length) throw new Error(`rivals (SX-3): ${rivalsBad.join('; ')}`);

  const site = {
    schema: 'suborned.sim.site/1',
    mission: 'handover',
    frame: 'civic-block.json block frame in integer centimetres: x south, y east. Mission clock in seconds from 0:00 (the engine ticks at 10 Hz).',
    from: [PLAN, plan.civic.block, MISSION, 'tools/plan/walk.ts blockersOf', 'tools/plan/walk-solids.ts', COLLIDE_FILE, 'tools/sim/solids.ts'],
    grid: {
      x0: cm(WINDOW.x0), y0: cm(WINDOW.y0), cell: cm(CELL), nx, ny, body: cm(solids.r), rows,
      floor: floorRows(G.floor, nx, ny),
      collide: opts.solidDigest ? G.drawn.solidSha : G.drawn.collideSha,
      drawn: { walk: !!opts.drawnWalk, sight: !!opts.drawnOccluders, walkCells: G.added.size, sightOccluders: G.occluders.length, ...(opts.solidDigest ? { digest: 'solid' } : {}) },
      src: 'tools/plan/walk.ts blockersOf (built plots behind their building lines, civic footprints and towers, water, landmarks) and walk-solids.ts (the block\'s ground solids, BODY clearance), and, with drawn.walk, the drawn solids (tools/sim/solids.ts: every piece the world places, with its kit.collide.json volumes; a building\'s face by the cell centre, every other solid BODY clear, where it blocks a walker at the cell\'s floor; drawn.walkCells counts the cells they block, held or not), and the mission cameras\' poles the host stands (tools/missions/handover/legs.ts cameraPoles, kit.collide.json CCTVPole at the host\'s scale; engine 0.30.0): a cell is free when its centre is. floor: per row, run-length pairs [cm, cells], block z (the pavement 0, the carriageway -18): the ground drawn there or the floor civic-block.json declares (tools/plan/civic.ts floorAt)',
    },
    occluders,
    places,
    cameras,
    patrol: { a: 'patrol-a', b: 'patrol-b', facing: [0, -100], reroute: 'patrol-reroute', rerouteFacing: [-100, 0], src: 'the top-down test: the patrol faces the building it guards. Sky 2026-10-26 (brief 067 A, playing 0.2.122: "maybe to the side of the service entrance"; Vantage\'s points): the sweep runs across the service yard, parallel to the Office wing\'s east face, in a straight line between patrol-a and patrol-b (mission.json points, read there: Game.SweepPos walks the line between them), and the patrol faces west, toward that face and the service door; rerouted, it waits at patrol-reroute on the office plot\'s north edge facing north, away from the yard (2026-10-19 to 10-26: the forecourt, facing east toward the public door, the reroute south)' },
    guard: { post: 'd:Office entrance', facing: [0, -100], cafe: 'd:Café door', cafeFacing: [-100, 0], src: 'GDD §05 guard on door routine (café stop pre-shift); mission.json windows.front-door; he faces out over the forecourt from the door, which is on the Office\'s west face (Sky 2026-10-19, option b), so west; at the café he faces the café' },
    vale: { exit: 'd:Official entrance', car: 'd:Official drop-off', service: 'd:Service lane', src: 'GDD §07 5:50 Vale exits, 6:10 at car (service lane if the driver is turned); mission.json roles.councillor leavesTo' },
    car: { pickup: 'd:Official drop-off', driver: 'd:Meet driver', service: 'service-bay', block: 'block-spot', serviceFacing: serviceFacing, blockFacing: blockFacing, src: 'GDD §07; mission.json roles.driver; blockFacing: parked at block-spot the car lies along its bay\'s long axis (the official bay\'s kerb), nose the way it pulled out from the pickup (tools/sim/site.ts); the turned driver parks in the Office\'s east yard bay (mission.json point service-bay, brief 023), nose in: serviceFacing is the bay\'s long axis pointing away from the drive it is entered by (tools/sim/site.ts)' },
    drive,
    rivals: { park: 'rival-park', front: 'd:Office approach', // engine 0.33.0: rivals.entry (rival-entry) is retired: the crew comes by car (rivals.car.entry)
      ...(mission.rivals ? { public: mission.rivals.public } : {}), ...(opts.foldStands ? { car: rivalsNew.car, posts: rivalsNew.posts } : {}), src: 'GDD §05 rivals; the top-down test: drawn by gunfire they make for the front of the Office' + (mission.rivals ? `; public: mission.json rivals (${mission.rivals.src})` : '') },
    security: { entry: 'security-entry', via: 'd:Office approach', to: 'd:Service lane', src: 'the top-down test: the response unit crosses to the Office front, then the service side' },
    getaway: { car: 'd:Getaway bay', exit: exitLane, ...(opts.foldStands ? { stands } : {}), src: 'GDD §07 win: Vale alive at the getaway car; mission.json destinations Getaway bay' },
    fight: { ambush: ambush.map((p) => p.id), police: police.map((p) => p.id), entry: 'security-entry', guard: 'guard-ally', src: 'GDD §07 final fight [S]; rulebook fight' },
    schedule,
    purse: 100000,
    purseSrc: 'GDD §07: Purse $100,000',
    arms: mission.arms.sidearms,
    armsSrc: `mission.json arms: ${mission.arms.src}`,
    // brief 031 §2.1: the cell's Heat at the start (the attention band), from mission.json start.heat (Waymark) once it carries it; absent, the engine reads 0
    ...(typeof mission.start?.heat === 'number' ? { heat: mission.start.heat, heatSrc: `mission.json start.heat${mission.start.src ? `: ${mission.start.src}` : ''}` } : {}),
    cast,
    cover: coverOf(),
  };
  const json = pretty(site) + '\n';
  // brief 059: the second file (the engine does not read it yet): the same stands and rival data, with the frame and its sources, in the site's own units
  const standsFile = pretty({
    schema: 'suborned.sim.stands/1', mission: 'handover', frame: site.frame,
    from: [PLAN, plan.civic.block, MISSION, 'sim/data/handover.site.json (places, grid, drive)', 'tools/sim/stands.ts', 'tools/sim/getaway.ts'],
    src: 'brief 059 §5 (Splice): getaway.stands[] and rivals.car / rivals.posts, derived from the mission points (Vantage), the mission routes getaway, getaway-service and rivals, and the plan, held by SX-2 and SX-3 (tools/sim/stands.ts). A second file so the site keeps the shape the engine reads (Site.cs rejects a key it does not know): `bun run sim:site --fold-stands` moves it into the site (getaway.stands, rivals.car, rivals.posts) when Keel\'s 059 round reads it there, and this file is then not written',
    getaway: { stands }, rivals: { car: rivalsNew.car, posts: rivalsNew.posts },
  }) + '\n';
  return { json, site, stands: standsFile };
}

/** The security level the engine runs at (GDD v0.23 §05, briefs/013/motive-takedowns.md §5.3), written into sim/data/handover.provisional.json from the data that owns it:
 *  `base` = the securityBase of the plan area the civic plots stand in (plan.areas[*].securityBase, Datum), `bonus` = mission.json securityBonus (Waymark), `district` = the plan's id.
 *  The engine's reader (Site.AddProvisional, Keel) still takes the file in its first shape, and it hashes the file's canonical text into every replay's start, so the texts
 *  below are pinned byte for byte: they say "until the plan carries it", which it now does. Rewording them, or retiring the file for a reader that takes the site, is Keel's change
 *  and re-records the replays; until then this writes the same bytes the file always had, so the engine and every recorded run are untouched. */
export function buildProvisional(): string {
  const read = (f: string) => JSON.parse(readFileSync(join(root, f), 'utf8')), plan = read(PLAN), mission = read(MISSION);
  const areas = [...new Set((plan.plots as { civic?: string; area: string }[]).filter(p => p.civic).map(p => p.area))];
  if (areas.length !== 1) throw new Error(`plan: the civic plots stand in ${areas.length} areas (${areas.join(', ')}); the Handover needs one`);
  const area = (plan.areas as { id: string; securityBase?: number }[]).find(a => a.id === areas[0]);
  if (area?.securityBase === undefined) throw new Error(`plan.areas.${areas[0]} sets no securityBase (rule SEC-1)`);
  if (typeof mission.securityBonus !== 'number') throw new Error('mission.json sets no securityBonus');
  return pretty({
    schema: 'suborned.sim.provisional/1',
    mission: 'handover',
    note: "Brief 013 B (Keel, 2026-10-16): the security level the Handover runs at, until the district plan (plan.areas[*].securityBase, Datum) and mission.json (securityBonus, Waymark) carry it. The world's takedown facts (stashes, call boxes, robot docks, police posts) came here first from Vantage's survey; since Splice's export (sim/data/handover.takedown.json, tools/sim/takedown.ts) carries them, they are read from there and this file holds the level only.",
    security: {
      base: area.securityBase,
      bonus: mission.securityBonus,
      src: 'briefs/013/motive-takedowns.md §5.3 (Sky 2026-10-16 merged call 3): the civic district\'s base level is 2 (watchful), the Handover\'s own bonus 0, no open cases. Replaced by plan.areas[*].securityBase (Datum) and mission.json securityBonus (Waymark).',
    },
    district: {
      id: plan.id,
      src: "Brief 014 (Keel, 2026-10-16): Standing is per district (§04; rulebook standing). The Handover plays in one district, the plan's own (contracts/world/harbour-district.plan.json id). Replaced by the plan and the campaign layer when they carry it.",
    },
    stashes: [], callBoxes: [], docks: [], posts: [],
    srcEmpty: 'stashes, call boxes, docks and posts: sim/data/handover.takedown.json (Splice, 2026-10-16).',
  }) + '\n';
}

/** The cover the engine's fight AI reads (brief 011, Pulse's briefs/011/pulse-fight-ai.md §5): a copy of contracts/world/civic-block.cover.json's points and sight
 *  blockers, so the engine reads one site file and a stale copy fails the site test. The file itself is Pulse's (`bun run cover`).
 *  sha256: of what is copied (points, blockers, refuge), as tools/sim/range.ts does. Brief 072 (Keel): was the whole file's, whose `from` holds kit.json's
 *  sha256, so any kit piece added anywhere changed the site's bytes and every recorded run's inputs while the cover stood as it was. */
export const COVER_FILE = 'contracts/world/civic-block.cover.json';
function coverOf() {
  const text = readFileSync(join(import.meta.dir, '../..', COVER_FILE), 'utf8'), c = JSON.parse(text);
  if (c.schema !== 'suborned.cover/1') throw new Error(`${COVER_FILE} is not suborned.cover/1`);
  const points = c.points, blockers = c.blockers, refuge = c.refuge.map((r: { id: string; at: number[]; kind: string }) => ({ id: r.id, at: r.at, kind: r.kind }));
  return {
    from: COVER_FILE, sha256: createHash('sha256').update(JSON.stringify({ points, blockers, refuge })).digest('hex'),
    src: 'brief 011 fight AI (Pulse): cover points (where a body stands, the direction it shields against, the cone, the quality, present for cars) and the objects at eye height and up that block sight; sha256 of the points, blockers and refuge as copied here',
    points, blockers, refuge,
  };
}

/** JSON with one key per line, and arrays of numbers (points, polygons) kept on one line. */
export function pretty(v: unknown, ind = ''): string {
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

/** The source the committed site was built from (its cast.from and cast.fixture), so a check rebuilds it from the same people. */
export const sourceOfSite = (): Source => {
  const c = JSON.parse(readFileSync(join(root, SITE_FILE), 'utf8')).cast;
  return { people: c.from, ...(c.fixture ? { fixture: c.fixture } : {}) };
};

if (import.meta.main) {
  // bun run sim:site [--people FILE --fixture "why this file and not the promoted cast"]: writes the site and the people extract, from one people file
  const a = process.argv.slice(2), at = (k: string) => { const i = a.indexOf(k); return i >= 0 ? a[i + 1] : undefined; };
  const src: Source = { people: at('--people') ?? PROMOTED_PEOPLE, ...(at('--fixture') ? { fixture: at('--fixture')! } : {}) };
  if (src.people !== PROMOTED_PEOPLE && !src.fixture) throw new Error('a people file other than the promoted one is a fixture: say why with --fixture');
  // the drawn solids stay as the committed site has them (grid.drawn: engine 0.30 adopted both); --drawn-walk / --drawn-occluders force one on, --no-drawn-walk / --no-drawn-occluders off. Without a site file both default to off.
  const had: SiteOptions = existsSync(join(root, SITE_FILE)) ? optionsOfSite() : {};
  const flag = (on: string, off: string, was?: boolean) => (a.includes(off) ? false : a.includes(on) || !!was);
  const { json, stands } = buildSite(src, { drawnWalk: flag('--drawn-walk', '--no-drawn-walk', had.drawnWalk), drawnOccluders: flag('--drawn-occluders', '--no-drawn-occluders', had.drawnOccluders), foldStands: flag('--fold-stands', '--no-fold-stands', had.foldStands), solidDigest: flag('--solid-digest', '--no-solid-digest', had.solidDigest) });
  writeFileSync(join(root, SITE_FILE), json);
  if (!JSON.parse(json).getaway.stands) writeFileSync(join(root, STANDS_FILE), stands); else rmSync(join(root, STANDS_FILE), { force: true }); // folded into the site: no second file
  const people = buildPeople(src);
  writeFileSync(join(root, PEOPLE_OUT), people);
  const take = buildTakedown().json; // brief 013: stashes, report points, origins and watchers, derived from the world; a second file, so the site (and every replay's input hash) stays as the engine reads it
  writeFileSync(join(root, TAKEDOWN_FILE), take);
  writeFileSync(join(root, PROVISIONAL_FILE), buildProvisional()); // the security level, from the plan and the mission
  console.log(`wrote ${SITE_FILE} (${json.length} bytes)${JSON.parse(json).getaway.stands ? " with the 059 stands folded in" : `, ${STANDS_FILE} (${stands.length} bytes)`}, ${PEOPLE_OUT} (${people.length} bytes) from ${src.people}${src.fixture ? ' (FIXTURE: ' + src.fixture + ')' : ''} and ${TAKEDOWN_FILE} (${take.length} bytes)`);
}
