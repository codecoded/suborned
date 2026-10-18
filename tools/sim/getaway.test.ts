// Brief 018 §1 (Splice): getaway.exit in sim/data/handover.site.json, from routes.getaway and the plan road. Rule SX-1 passes on the shipped lane and fails
// on deliberately wrong ones. Run: bun test tools/sim/getaway.test.ts
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { derive } from '../plan/plan';
import { toPlanFn } from '../plan/civic';
import { buildExit, blockM, checkExit, exitLane } from './getaway';
import { SITE_FILE } from './site';

const root = join(import.meta.dir, '..', '..'), read = (f: string) => JSON.parse(readFileSync(join(root, f), 'utf8'));
const plan = read('contracts/world/harbour-district.plan.json'), mission = read('contracts/missions/handover/mission.json'), rulebook = read('contracts/rules/rulebook.json');
const d = derive(plan), bm = blockM(rulebook).m;
const bay = toPlanFn(plan.civic)(mission.destinations.find((x: { name: string }) => x.name === 'Getaway bay').at);
const shipped = () => read(SITE_FILE).getaway.exit;
const cp = <T>(x: T): T => JSON.parse(JSON.stringify(x));

describe('getaway.exit', () => {
  test('is what the plan, the mission and the rulebook give today (bun run sim:site): Rocket Lane, 30 m south from the car\'s bay, 12 m wide', () => {
    const e = shipped();
    expect(e).toEqual(buildExit(plan, d, mission, 'Getaway bay', rulebook));
    expect(Object.keys(e)).toEqual(['polyline', 'w', 'src']);
    expect(e.w).toBe(plan.roads.find((r: { id: string }) => r.id === 'rocket-lane').w);
    expect(e.polyline).toEqual([[850, -5600], [3850, -5600]]);
    expect(e.src).toContain('routes "getaway"');
    expect(e.src).toContain('rocket-lane');
  });
  test('SX-1 holds on the shipped lane', () => expect(checkExit(shipped(), plan, d, bay, bm)).toEqual([]));

  test('SX-1 fails on a lane that is missing, too short, too long, off the carriageway, the wrong width or away from the car', () => {
    expect(checkExit(undefined, plan, d, bay, bm)[0]).toMatch(/missing/);
    expect(checkExit({ ...shipped(), polyline: [[850, -5600]] }, plan, d, bay, bm)[0]).toMatch(/fewer than two/);
    const short = cp(shipped()); short.polyline[1][0] = 2850; // 20 m
    expect(checkExit(short, plan, d, bay, bm).join()).toMatch(/20\.00 m long/);
    const long = cp(shipped()); long.polyline[1][0] = 4850; // 40 m
    expect(checkExit(long, plan, d, bay, bm).join()).toMatch(/longer than/);
    const off = cp(shipped()); for (const p of off.polyline) p[1] += 1500; // 15 m east: a plot, not the carriageway
    expect(checkExit(off, plan, d, bay, bm).join()).toMatch(/leaves the carriageway/);
    const wrongW = { ...cp(shipped()), w: 24 };
    expect(checkExit(wrongW, plan, d, bay, bm).join()).toMatch(/not the width of the road/);
    const away = cp(shipped()); for (const p of away.polyline) p[0] += 3000; // starts 30 m down the road, still on it
    expect(checkExit(away, plan, d, bay, bm).join()).toMatch(/starts .* from the car's bay/);
    expect(checkExit({ ...shipped(), w: 0 }, plan, d, bay, bm)[0]).toMatch(/not a width/);
  });

  test('the site build rejects a mission with no getaway route, a route that reaches no end, or a bay beside another road', () => {
    const noRoute = { ...mission, routes: mission.routes.filter((r: { id: string }) => r.id !== 'getaway') };
    expect(() => buildExit(plan, d, noRoute, 'Getaway bay', rulebook)).toThrow(/no routes\.getaway/);
    const noEnd = { ...mission, routes: mission.routes.map((r: { id: string }) => (r.id === 'getaway' ? { ...r, to: ['no_such_node'] } : r)) };
    expect(() => buildExit(plan, d, noEnd, 'Getaway bay', rulebook)).toThrow();
    expect(() => buildExit(plan, d, mission, 'Nowhere', rulebook)).toThrow(/no destination/);
    const other = { ...mission, routes: mission.routes.map((r: { id: string }) => (r.id === 'getaway' ? { ...r, from: 'sw' } : r)) }; // starts on Sunset Way, not the road the car stands by
    expect(() => buildExit(plan, d, other, 'Getaway bay', rulebook)).toThrow(/nearest rocket-lane/);
  });

  test('the lane follows the rulebook\'s fight-block-m once it is there', () => {
    const rb = { values: { ...rulebook.values, 'fight-block-m': { v: 20 } } };
    const e = buildExit(plan, d, mission, 'Getaway bay', rb);
    expect(e.polyline).toEqual([[850, -5600], [2850, -5600]]);
    expect(e.src).toContain('(rulebook)');
    expect(exitLane(plan, d, mission, 'Getaway bay', 30).length).toBeCloseTo(30, 6);
  });
});
