// The walk reads the civic block's ground solids (brief 008 B3, Waymark's measurement): the terrace wall, stair cheeks, pilasters, beds and tables are walked
// around, by their own edges and not by the 2 m grid's cell centres. A block without a dressing has no solids and walks as it always did.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic, toPlanFn } from './civic';
import { derive, type Plan } from './plan';
import { reach, walks } from './walk';
import { walkBetween } from './walk-fine';
import { Solids } from './walk-solids';

setDefaultTimeout(60000);
const root = resolve(import.meta.dir, '../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), block = read(plan.civic.block), toPlan = toPlanFn(plan.civic);
const live = civic(plan.civic, block), { dressing: _d, ...bareBlock } = block, bare = civic(plan.civic, bareBlock);
const d = derive(plan);
const crewLine = toPlan([2.968, -49.666]); // the middle of the four crew marks (mission.json crew: x 1.168 … 4.768, y -49.666)

test('the solids are the dressing\'s wall, cheeks and pilasters, the objects and the pillars, cabinets and monument; the overhead marquee and the steps are not', () => {
  expect(live.solids!.length).toBe(44); // 10 of the pieces (4 wall, 4 cheeks and piers, 2 pilasters; not the overhead marquee) + 28 objects + 6 pillars, cabinets and the monument
  expect(bare.solids).toBeUndefined();
  const wall = block.dressing.pieces.find((p: any) => p.piece === 'CafeBay').solids[0].polygon.map(toPlan);
  expect(live.solids!.some(s => JSON.stringify(s) === JSON.stringify(wall))).toBe(true);
});

test('Crew line → Between tables is 26.2 m round the terrace wall (17.7 m with no wall); Service lane → Office entrance is 25.5 m (17.5 m with no cheeks or beds)', () => {
  const between = toPlan([-7.1, -35.45]), service = toPlan([62.5, -43.2]), office = toPlan([51.75, -30]);
  expect(walkBetween(plan, d, live, crewLine, between)).toBeCloseTo(26.2, 0);
  expect(walkBetween(plan, d, live, service, office)).toBeCloseTo(25.5, 0);
  expect(walkBetween(plan, d, bare, crewLine, between)).toBeCloseTo(17.65, 1);
  expect(walkBetween(plan, d, bare, service, office)).toBeCloseTo(17.5, 0);
});

test('a step is blocked by a wall 0.66 m thick that no cell centre of a 2 m grid holds, and by the clearance round it', () => {
  const wall = new Solids([[[0, 0], [0.66, 0], [0.66, 6], [0, 6]]]);
  expect(wall.clear([-3, 3], [4, 3])).toBe(false); // through it
  expect(wall.clear([-0.5, 3], [-0.5, 8])).toBe(false); // along it, 0.5 m off
  expect(wall.clear([-0.7, 3], [-0.7, 8])).toBe(true); // along it, 0.7 m off
  expect(wall.clear([-3, 7], [4, 7])).toBe(true); // past its end
});

test('the coarse walk reads the solids too: every stop and station is still reached, and a walk through the block is no shorter', () => {
  const r1 = reach(plan, derive(plan), live), r0 = reach(plan, derive(plan), bare);
  expect(r1.filter(r => r.m === Infinity)).toEqual([]);
  r1.forEach((r, i) => expect(r.m).toBeGreaterThanOrEqual(r0[i]!.m - 1e-9));
  const w1 = walks(plan, derive(plan), live), w0 = walks(plan, derive(plan), bare);
  expect(w1.length).toBe(w0.length);
  for (const w of w1) expect(w.m / w.straight).toBeLessThanOrEqual(plan.standards.walking.directness);
});
