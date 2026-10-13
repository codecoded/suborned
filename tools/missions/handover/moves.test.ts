// Standing check: the Handover's door points are the civic block's entrances (contracts/world/civic-block.json). Doors are
// world (W-4: every walk starts at a building entrance); the mission names them and stands on them. It fails, naming the point,
// when a door moves in the block and the mission has not followed, when a door point drifts off its door, and when the block
// gains an entrance the mission does not know (a new way in or out is a route change: Waymark).
// It replaces the check of Vantage's four pending moves (brief 008 B2), applied to mission.json on 2026-10-14.
import { expect, test } from 'bun:test';
import { resolve } from 'node:path';
import { Mission } from '../../../contracts/missions/mission.schema';

type P = [number, number];
const root = resolve(import.meta.dir, '../../..'), json = async (p: string) => JSON.parse(await Bun.file(resolve(root, p)).text());
const plan = await json('contracts/world/harbour-district.plan.json'), block = await json(plan.civic.block);
const mission = Mission.parse(await json('contracts/missions/handover/mission.json'));
/** Mission door point → block entrance id. 'Meet Vale' is where Vale comes out: the official door. */
const DOORS: Record<string, string> = { 'Café door': 'cafe-entrance', 'Office entrance': 'office-entrance', 'Official entrance': 'official-entrance', 'Meet Vale': 'official-entrance' };
const entrances = new Map<string, P>(block.buildings.flatMap((b: any) => (b.entrances ?? []).map((e: any) => [e.id, e.at])));
const at = (name: string) => mission.destinations.find(d => d.name === name)?.at;
const dist = (a: P, b: P) => Math.hypot(a[0] - b[0], a[1] - b[1]);

test('every Handover door point stands on the civic block entrance it names', () => {
  expect(Object.values(DOORS).filter(id => !entrances.has(id))).toEqual([]);
  expect(Object.fromEntries(Object.keys(DOORS).map(n => [n, at(n)]))).toEqual(Object.fromEntries(Object.entries(DOORS).map(([n, id]) => [n, entrances.get(id)])));
});

test('every civic block entrance has a Handover door point, and every walk origin is one', () => {
  expect([...entrances.keys()].filter(id => !Object.values(DOORS).includes(id))).toEqual([]);
  expect((plan.standards.walking.from as string[]).filter(n => !(n in DOORS))).toEqual([]);
});

test('Office approach waits at the foot of the public door steps: on their axis, within 1 m before the first riser', () => {
  const door = block.buildings.flatMap((b: any) => b.entrances ?? []).find((e: any) => e.id === DOORS['Office entrance']);
  const steps = block.structures.find((s: any) => s.category === 'entrance-steps' && s.landing === door.on) as { foot: P; top: P };
  const p = at('Office approach')!, flight = dist(steps.foot, steps.top), out = dist(p, steps.foot);
  expect(out).toBeGreaterThan(0);
  expect(out).toBeLessThanOrEqual(1);
  expect(dist(p, steps.top)).toBeCloseTo(flight + out, 6); // straight out from the top through the foot: on the axis, off the flight
});
