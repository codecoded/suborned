// Brief 059 §5 (Splice): getaway.stands[] and rivals.car / rivals.posts in sim/data/handover.site.json, derived from the mission's points and routes. SX-2 (the stands) and SX-3 (the rival car's route and points) pass on the
// shipped site and fail, each with its reason, on deliberately wrong ones. Run: bun test tools/sim/stands.test.ts
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { derive } from '../plan/plan';
import { civic } from '../plan/civic';
import type { V } from '../plan/geom';
import { driveFree, type DriveGrid } from './drive';
import { buildSite, optionsOfSite, readStands, sourceOfSite, SITE_FILE, STANDS_FILE } from './site';
import { buildRivalRoute, buildRivals, buildStands, checkRivals, checkStands, standFacing, STANDS } from './stands';

const root = join(import.meta.dir, '..', '..'), read = (f: string) => JSON.parse(readFileSync(join(root, f), 'utf8'));
const plan = read('contracts/world/harbour-district.plan.json'), mission = read('contracts/missions/handover/mission.json'), rulebook = read('contracts/rules/rulebook.json'), block = read(plan.civic.block), site = read(SITE_FILE);
const d = derive(plan), places = site.places, places059 = places, g = site.grid, s059 = readStands(); // the getaway stands and the rival car's way in, folded into the site by Keel's 059 round
const free = (q: V) => { const i = Math.floor((Math.round(q[0] * 100) - g.x0) / g.cell), j = Math.floor((Math.round(q[1] * 100) - g.y0) / g.cell); return site.grid.rows[i]?.[j] === '.'; };
const drivable = driveFree(site.drive as DriveGrid);
const cp = <T>(x: T): T => JSON.parse(JSON.stringify(x));
/** A cell the walk grid blocks, well inside the window (the middle of a blocked run), as an `at` in cm: a wall or a solid for a foot point to stand in. */
const blocked = (() => { for (let i = 20; i < g.nx - 20; i++) for (let j = 20; j < g.ny - 20; j++) if (site.grid.rows[i][j] === '#' && site.grid.rows[i][j - 2] === '#' && site.grid.rows[i][j + 2] === '#' && site.grid.rows[i - 2]?.[j] === '#' && site.grid.rows[i + 2]?.[j] === '#') return [g.x0 + i * g.cell + g.cell / 2, g.y0 + j * g.cell + g.cell / 2]; throw new Error('no blocked cell'); })();
const stands = () => cp(s059.getaway.stands) as ReturnType<typeof buildStands>;
const rivals = () => cp({ car: s059.rivals.car, posts: s059.rivals.posts }) as ReturnType<typeof buildRivals>;
const sx2 = (s: unknown, pl = places) => checkStands(s as never, plan, d, pl, rulebook, free, drivable);
const sx3 = (r: unknown, pl = places059, arrive = site.schedule.rivalsArrive) => checkRivals(r as never, plan, d, pl, rulebook, arrive, free, drivable);

describe('the second file (the engine reads the site, not this, until Keel\'s 059 round)', () => {
  test('sim/data/handover.stands.json is what the site build makes today, and the site keeps the shape the engine reads: no getaway.stands, no rivals.car / posts', () => {
    const { stands: text } = buildSite(sourceOfSite(), { ...optionsOfSite(), foldStands: false });
    if (!site.getaway.stands) expect(readFileSync(join(root, STANDS_FILE), 'utf8')).toBe(text);
    const f = JSON.parse(text);
    expect(f.schema).toBe('suborned.sim.stands/1'); expect(Object.keys(f)).toEqual(['schema', 'mission', 'frame', 'from', 'src', 'getaway', 'rivals']);
    if (!site.getaway.stands) { expect(Object.keys(site.getaway)).toEqual(['car', 'exit', 'src']); expect(Object.keys(site.rivals)).toEqual(['park', 'entry', 'front', 'public', 'src']); }
  }, 60000);
  test('folded into the site (bun run sim:site --fold-stands) the same data stands under getaway.stands, rivals.car and rivals.posts', () => {
    const { site: folded } = buildSite(sourceOfSite(), { ...optionsOfSite(), foldStands: true }) as { site: any };
    expect(folded.getaway.stands).toEqual(s059.getaway.stands); expect(folded.rivals.car).toEqual(s059.rivals.car); expect(folded.rivals.posts).toEqual(s059.rivals.posts);
  }, 60000);
});

describe('getaway.stands (SX-2)', () => {
  test('the site lists the square and the service drive, each with its derived exit lane, five ambush spots, four police stands and a rival stop', () => {
    const s = s059.getaway.stands as any[];
    expect(s.map((x: { id: string }) => x.id)).toEqual(['square', 'service-drive']);
    expect(s[0].car).toBe(site.getaway.car); expect(s[0].exit).toEqual(site.getaway.exit); // the square's lane is the one getaway.exit has always been
    expect(s[1].exit.polyline).toEqual([[8600, -1500], [8600, 1500]]); // Apron Road east from the drive's foot (86, -15), fight-block-m 30 m, 12 m wide
    expect(s[1].exit.w).toBe(12); expect(s[1].exit.src).toContain('routes "getaway-service"');
    expect(s.map((x: { facing: number[] }) => x.facing)).toEqual([[100, 0], [100, 0]]); // the square's car along its bay toward Rocket Lane's lane; the yard's nose out of the drive toward Apron Road
    expect(s.map((x: { ambush: string[] }) => x.ambush.length)).toEqual([5, 5]); expect(s.map((x: { police: string[] }) => x.police.length)).toEqual([4, 4]);
    expect(s[1].ambush).toEqual(['svc-ambush-1', 'svc-ambush-2', 'svc-ambush-3', 'svc-ambush-4', 'svc-ambush-5']);
    expect(s.map((x: { rivalStop: string }) => x.rivalStop)).toEqual(['rival-drop', 'rival-tail']);
    expect(site.fight.ambush).toEqual(s[0].ambush); // the square's fight posts are the engine's old lists: the yard's svc- points stay off them
  });
  test('is what buildStands makes from the plan, the mission and the site\'s places today', () => expect(buildStands(plan, d, mission, block, rulebook, places)).toEqual(s059.getaway.stands as never));
  test('SX-2 holds on the shipped stands', () => expect(sx2(stands())).toEqual([]));

  test('SX-2 fails on a missing stand, a lane that is wrong, a spot in the hold ring or in the lane or off the free cells, a car off the drive grid, a stop in the lane', () => {
    expect(sx2(undefined)[0]).toMatch(/missing or lists fewer/);
    expect(sx2([stands()[0]])[0]).toMatch(/fewer than the two stands/);
    const short = stands(); short[1]!.exit.polyline[1]![1] = 500; // 20 m
    expect(sx2(short).join()).toMatch(/stand service-drive: exit lane: the lane is 20\.00 m long/);
    const away = stands(); away[1]!.exit.polyline = away[1]!.exit.polyline.map((p) => [p[0]! + 3000, p[1]!]); // 30 m south of Apron Road's centreline: a plot
    expect(sx2(away).join()).toMatch(/exit lane: .*(leaves the carriageway|starts)/);
    const hold = stands(), near = JSON.parse(JSON.stringify(places)); near['svc-ambush-1'] = { at: [places['service-drive'].at[0] - 300, places['service-drive'].at[1] - 300] }; // 4.2 m from the car
    expect(sx2(hold, near).join()).toMatch(/svc-ambush-1 is \d\.\d m from the car, inside fight-hold-m 8 m/);
    const lane = JSON.parse(JSON.stringify(places)); lane['svc-police-2'] = { at: [8600, 0] }; // on Apron Road's centreline, in the exit lane
    expect(sx2(stands(), lane).join()).toMatch(/svc-police-2 \(86,0\) is in the exit lane/);
    const wall = JSON.parse(JSON.stringify(places)); wall['svc-ambush-3'] = { at: blocked }; // inside a wall or a solid
    expect(sx2(stands(), wall).join()).toMatch(/svc-ambush-3 .* is not on a free walk cell/);
    const less = stands(); less[1]!.ambush.pop();
    expect(sx2(less).join()).toMatch(/stand service-drive: 4 ambush spots and 4 police stands, the square's are 5 and 4/);
    const road = JSON.parse(JSON.stringify(places)); road['service-drive'] = { at: [6935, -3000] }; // a plot cell, no drive
    expect(sx2(stands(), road).join()).toMatch(/its car service-drive .* is not on a drive-grid cell/);
    const stop = stands(); stop[1]!.rivalStop = 'rival-drop'; const inLane = JSON.parse(JSON.stringify(places)); inLane['rival-drop'] = { at: [8600, 0] };
    expect(sx2(stop, inLane).join()).toMatch(/its rival stop rival-drop is in the exit lane/);
    const face = stands(); face[0]!.facing = [100, 100];
    expect(sx2(face).join()).toMatch(/its facing 100,100 is not a heading of 100/);
  });

  test('the build rejects what has no way to stand: a mission with no service route, a point that is missing, a bay across its lane', () => {
    const noRoute = { ...mission, routes: mission.routes.filter((r: { id: string }) => r.id !== 'getaway-service') };
    expect(() => buildStands(plan, d, noRoute, block, rulebook, places)).toThrow(/no routes\.getaway-service/);
    const noPoint = cp(places); delete noPoint['svc-ambush-2']; expect(buildStands(plan, d, mission, block, rulebook, noPoint)[1]!.ambush).toHaveLength(4); // a missing spot is the count SX-2 names, not a crash
    const noCar = cp(places); delete noCar['service-drive']; expect(() => buildStands(plan, d, mission, block, rulebook, noCar)).toThrow(/no place service-drive/);
    const noStop = cp(places); delete noStop['rival-tail']; expect(() => buildStands(plan, d, mission, block, rulebook, noStop)).toThrow(/no point rival-tail/);
    // a drive facing: nose out along the drive, away from the bay it enters; a bay: along its long axis the way its lane runs
    expect(standFacing(block, [69.35, -15], [[8600, -1500], [8600, 1500]])).toEqual([100, 0]);
    expect(standFacing(block, [8.5, -48.2], [[850, -5600], [3850, -5600]])).toEqual([100, 0]);
    expect(() => standFacing(block, [8.5, -48.2], [[850, -5600], [850, -2600]])).toThrow(/long axis is across its exit lane/); // a lane that runs along y: the bay's x axis is across it, no heading
  });
  test('the stands are the ones the brief names, in that order', () => expect(STANDS.map((s) => [s.id, s.route, s.rivalStop])).toEqual([['square', 'getaway', 'rival-drop'], ['service-drive', 'getaway-service', 'rival-tail']]));
});

describe('rivals.car and rivals.posts (SX-3)', () => {
  test('the route is the mission\'s "rivals", cut to the window: Orbit Walk west to Rocket Lane, south to Apron Road, east; the car\'s points stand on it in order', () => {
    const r = (s059.rivals.car as any).route;
    expect(r.polyline).toEqual([[3750, 1600], [3750, -5600], [8600, -5600], [8600, 1000]]);
    expect(r.roads.map((x: { id: string }) => x.id)).toEqual(['orbit-walk', 'rocket-lane', 'apron-road']);
    expect(r.src).toContain('routes "rivals"');
    expect(r.chain.entry).toBeLessThan(r.chain.drop); expect(r.chain.drop).toBeLessThan(r.chain.park); expect(r.chain.park).toBeLessThan(r.chain.tail);
    expect((s059.rivals.car as any).facing).toEqual({ entry: [0, -100], drop: [0, -100], park: [0, 100], tail: [0, 100], front: [100, 0] });
    expect(places['rival-park'].at).toEqual([9050, -3100]); // Vantage's 059 park (brief 059 §5): Apron Road's south kerb, the eastbound lane, moved in Keel's 059 round
  });
  test('is what buildRivals makes from the plan, the mission and the site\'s places', () => expect(buildRivals(plan, d, mission, places059)).toEqual({ car: s059.rivals.car, posts: s059.rivals.posts } as never));
  test('SX-3 holds on the shipped route and points', () => expect(sx3(rivals())).toEqual([]));

  test('SX-3 fails on a route that is missing or not cut to the window, points off it or out of order, a foot point on a wall, a car point off the drive grid, an entry before 0:00', () => {
    expect(sx3(undefined)[0]).toMatch(/missing or has fewer than two/);
    const uncut = rivals(); uncut.car.route.polyline.unshift([3750, 5000]);
    expect(sx3(uncut).join()).toMatch(/not on the walk window's edge: it is not cut to the window|leaves the walk window/);
    const out = rivals(); out.car.route.polyline[0] = [3750, 2200];
    expect(sx3(out).join()).toMatch(/leaves the walk window/);
    const off = cp(places); off['rival-car-entry'] = { at: [3950, 1575] }; // 2 m off Orbit Walk's centreline, still a road cell
    expect(sx3(rivals(), off).join()).toMatch(/rival-car-entry\) is 2\.00 m off the route's centreline/);
    const late = cp(places); late['rival-car-entry'] = { at: [3750, 1000] }; // on the route, 6 m in
    expect(sx3(rivals(), late).join()).toMatch(/is 6\.00 m along the route: it is not where the route enters the window/);
    const far = cp(places); far['rival-park'] = { at: [9050 + 1200, -3100] }; // 12 m off Apron Road's centreline
    expect(sx3(rivals(), far).join()).toMatch(/rivals\.car\.park \(rival-park\) is \d+\.\d m from the route, off the 12 m carriageway|not on a drive-grid cell/);
    const swap = cp(places); swap['rival-drop'] = { at: [8700, -3100] }; swap['rival-park'] = { at: [3400, -3800] };
    expect(sx3(rivals(), swap).join()).toMatch(/are not in order along the route/);
    const wall = cp(places); wall['rival-lookout'] = { at: blocked };
    expect(sx3(rivals(), wall).join()).toMatch(/rivals\.posts\.lookout .* is not on a free walk cell/);
    const grass = cp(places); grass['rival-tail'] = { at: [10200, -1700] }; // the south pavement, not the carriageway
    expect(sx3(rivals(), grass).join()).toMatch(/rivals\.car\.tail .* is not on a drive-grid cell/);
    expect(sx3(rivals(), places, 30).join()).toMatch(/the rival car would have to enter at -[\d.]+ s/); // a 30 s park cannot hold a 145 m drive at 5 m/s and an 8 s drop
    const bad = rivals(); bad.car.facing.park = [100, 100];
    expect(sx3(bad).join()).toMatch(/rivals\.car\.facing\.park 100,100 is not a heading of 100/);
  });

  test('the build rejects a mission with no "rivals" route, a route that never enters the window, and (without its via nodes) a route that does not come by Orbit Walk', () => {
    expect(() => buildRivalRoute(plan, d, { ...mission, routes: mission.routes.filter((r: { id: string }) => r.id !== 'rivals') }, places)).toThrow(/no routes\.rivals/);
    const north = { ...mission, routes: mission.routes.map((r: { id: string }) => (r.id === 'rivals' ? { ...r, from: 'off_west', via: undefined, to: ['off_circus_west'] } : r)) };
    expect(() => buildRivalRoute(plan, d, north, places)).toThrow(/never enters the walk window/);
    const plain = { ...mission, routes: mission.routes.map((r: { id: string }) => (r.id === 'rivals' ? { ...r, via: undefined } : r)) }; // the shortest road from the Sea Gates comes down Satellite Street and along Apron Road westbound: Orbit Walk is not on it
    const rr = buildRivalRoute(plan, d, plain, places);
    expect(sx3({ car: { ...rivals().car, route: { ...rivals().car.route, polyline: rr.polyline, roads: rr.roads } }, posts: rivals().posts }).join()).toMatch(/not in order along the route|from the route|route starts on/);
    expect(() => buildRivals(plan, d, mission, (() => { const p = cp(places); delete p['rival-front-stop']; return p; })())).toThrow(/no point rival-front-stop/);
  });
});

describe('the mission routes (M-1 with via)', () => {
  test('getaway-service and rivals are mission routes over the plan graph; the rival route goes through its via nodes in order', () => {
    const ids = mission.routes.map((r: { id: string }) => r.id);
    expect(ids).toEqual(['getaway', 'official', 'getaway-service', 'rivals']);
    const r = mission.routes.find((x: { id: string }) => x.id === 'rivals');
    expect(r.via).toEqual(['r_orbit_e', 'r_orbit_w', 'r_apron_w']);
    expect(civic(plan.civic, block).bays.length).toBeGreaterThan(0);
  });
});
