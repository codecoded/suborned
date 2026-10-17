// The Handover's ground as the C# engine reads it (brief 011 A, Keel): sim/data/handover.site.json, block frame, integer centimetres.
// Built from the plan's walk blockers and the civic block's solids (the same ground tools/plan/walk.ts and walk-fine.ts walk on), the
// civic buildings as sight occluders, the mission's points, timeline and window, and the engine's own points (cameras, the Tech's
// cabinet, the patrol's sweep, the rivals' and the response unit's entries), each with the reason it stands where it does. Those
// are Waymark's to adopt into mission.json (Suture O-6); until then they live here. Deterministic: the same inputs give the same
// bytes. The file names its sources but holds no hash of them: an edit to the plan or the mission that leaves this ground as it
// was leaves the file, and every replay's start hash, as they were; tools/sim/sim.test.ts fails on any edit that changes it.
// Run: bun run sim:site (writes the file, the people extract, the takedown places (tools/sim/takedown.ts) and the provisional file with the security level (below)); bun test tools/sim (checks them).
import { readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { derive } from '../plan/plan';
import { civic, toPlanFn } from '../plan/civic';
import { blockersOf } from '../plan/walk';
import { CELL, WINDOW, walkGrid } from '../plan/walk-grid';
import type { V } from '../plan/geom';
import { castRoles } from '../missions/handover/cast';
import { castRules } from '../missions/handover/overlay';
import { summonRules } from '../missions/handover/summon';
import { createHash } from 'node:crypto';
import { buildPeople, PEOPLE_OUT } from './people';
import { buildTakedown, TAKEDOWN_FILE } from './takedown';

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

export function buildSite(src: Source = { people: PROMOTED_PEOPLE }): { json: string; site: unknown } {
  const read = (f: string) => readFileSync(join(root, f), 'utf8');
  const planText = read(PLAN), plan = JSON.parse(planText);
  const blockText = read(plan.civic.block), block = JSON.parse(blockText);
  const missionText = read(MISSION), mission = JSON.parse(missionText);
  const d = derive(plan), civ = civic(plan.civic, block), toPlan = toPlanFn(plan.civic);
  const { polys } = blockersOf(plan, d, civ);
  const G = walkGrid(plan, d, civ, WINDOW, CELL), { solids, nx, ny, rows, free, nearestFree } = G;

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
  const cameras = (mission.cameras as { id: string; at: V; looksAt: string; reachM: number; halfM: number; src: string; reason: string }[]).map((c) => {
    const to = dest(c.looksAt);
    return { id: c.id, at: P(c.at), facing: P([to[0] - c.at[0], to[1] - c.at[1]]), reach: cm(c.reachM), half: cm(c.halfM), src: c.src, reason: c.reason };
  });

  // ---- the mission's schedule, read from mission.json by its words, so a retimed entry (Waymark's close, 8:00 → 8:30) moves the engine
  const at = (_was: string, word: RegExp) => {
    const es = mission.timeline.filter((x: { what: string }) => word.test(x.what));
    if (es.length !== 1) throw new Error(`mission.json timeline: ${es.length} entries read "${word}", the engine needs exactly one`);
    const [m, s] = String(es[0].at).split(':').map(Number); return m! * 60 + s!;
  };
  // the guard's visit (mission.json windows rule summon) is the engine's: its grid walk and the rulebook's stay; the site writes neither
  const schedule = {
    rivalsArrive: at('3:10', /rival car/), chime: at('5:30', /chime/), driverLast: at('5:40', /turn driver/), valeExits: at('5:50', /Vale exits/), valeAtCar: at('6:10', /Vale at car/), over: at('8:00', /= over$/),
    blockMoveS: 8, blockHoldS: 150,
    src: 'mission.json timeline (GDD §07) and windows.front-door; blockHoldS: §07 route B, driver blocks pickup camera 2:30; blockMoveS: the top-down test, the car takes 8 s to block the camera. over: the timeline\'s closing entry, read by its words (§07 8:00; Sky 2026-10-15 moves it to 8:30, Waymark retimes mission.json)',
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
    rivals: { park: 'rival-park', entry: 'rival-entry', front: 'd:Office approach', ...(mission.rivals ? { public: mission.rivals.public } : {}), src: 'GDD §05 rivals; the top-down test: drawn by gunfire they make for the front of the Office' + (mission.rivals ? `; public: mission.json rivals (${mission.rivals.src})` : '') },
    security: { entry: 'security-entry', via: 'd:Office approach', to: 'd:Service lane', src: 'the top-down test: the response unit crosses to the Office front, then the service side' },
    getaway: { car: 'd:Getaway bay', src: 'GDD §07 win: Vale alive at the getaway car; mission.json destinations Getaway bay' },
    fight: { ambush: ambush.map((p) => p.id), police: police.map((p) => p.id), entry: 'security-entry', guard: 'guard-ally', src: 'GDD §07 final fight [S]; rulebook fight' },
    schedule,
    purse: 100000,
    purseSrc: 'GDD §07: Purse $100,000',
    arms: mission.arms.sidearms,
    armsSrc: `mission.json arms: ${mission.arms.src}`,
    cast,
    cover: coverOf(),
  };
  const json = pretty(site) + '\n';
  return { json, site };
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
 *  blockers, with the file's sha256, so the engine reads one site file and a stale copy fails the site test. The file itself is Pulse's (`bun run cover`). */
export const COVER_FILE = 'contracts/world/civic-block.cover.json';
function coverOf() {
  const text = readFileSync(join(import.meta.dir, '../..', COVER_FILE), 'utf8'), c = JSON.parse(text);
  if (c.schema !== 'suborned.cover/1') throw new Error(`${COVER_FILE} is not suborned.cover/1`);
  return {
    from: COVER_FILE, sha256: createHash('sha256').update(text).digest('hex'),
    src: 'brief 011 fight AI (Pulse): cover points (where a body stands, the direction it shields against, the cone, the quality, present for cars) and the objects at eye height and up that block sight',
    points: c.points, blockers: c.blockers, refuge: c.refuge.map((r: { id: string; at: number[]; kind: string }) => ({ id: r.id, at: r.at, kind: r.kind })),
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
  const { json } = buildSite(src);
  writeFileSync(join(root, SITE_FILE), json);
  const people = buildPeople(src);
  writeFileSync(join(root, PEOPLE_OUT), people);
  const take = buildTakedown().json; // brief 013: stashes, report points, origins and watchers, derived from the world; a second file, so the site (and every replay's input hash) stays as the engine reads it
  writeFileSync(join(root, TAKEDOWN_FILE), take);
  writeFileSync(join(root, PROVISIONAL_FILE), buildProvisional()); // the security level, from the plan and the mission
  console.log(`wrote ${SITE_FILE} (${json.length} bytes), ${PEOPLE_OUT} (${people.length} bytes) from ${src.people}${src.fixture ? ' (FIXTURE: ' + src.fixture + ')' : ''} and ${TAKEDOWN_FILE} (${take.length} bytes)`);
}
