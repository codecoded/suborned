// The walk reads the civic block's ground solids (brief 008 B3, Waymark's measurement): the terrace wall, stair cheeks, pilasters, beds and tables are walked
// around, by their own edges and not by the 2 m grid's cell centres. A block without a dressing has no solids and walks as it always did.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic, toPlanFn } from './civic';
import { derive, type Plan } from './plan';
import { reach, walks } from './walk';
import { walkBetween } from './walk-fine';
import type { V } from './geom';
import { Solids, bodyOf } from './walk-solids';

setDefaultTimeout(60000);
const root = resolve(import.meta.dir, '../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), block = read(plan.civic.block), toPlan = toPlanFn(plan.civic);
const live = civic(plan.civic, block), { dressing: _d, ...bareBlock } = block, bare = civic(plan.civic, bareBlock);
const d = derive(plan);
const crewLine = toPlan([2.968, -49.666]); // the middle of the four crew marks (mission.json crew: x 1.168 … 4.768, y -49.666)

test('the solids are the dressing\'s wall, cheeks and pilasters, the objects and the pillars and cabinets, and its planter runs, pots, bollards, lamp bases and ground cypresses; the overhead marquee and the steps are not', () => {
  expect(live.solids!.length).toBe(83); // 10 of the pieces (4 wall, 4 cheeks and piers, 2 pilasters; not the overhead marquee) + 26 objects + 5 pillars and cabinets (the steward monument left the block on 2026-10-26, Sky: briefs/067 J; was 87) + 38 ground furniture (12 planter modules: the square's odd three were removed the same day, Sky, briefs/067 K; was 41) + 4 cypresses on the ground (brief 055 step 5; the two in facade beds stand in the beds' solids) (was 85 with 28 objects: the two beds either side of the old north door are gone since the public door moved to the west face, 2026-10-19)
  expect(live.solidIds!.length).toBe(83);
  expect(live.solidIds).not.toContain('foreground-civic-robot'); // removed in civic-block.adjust.json: no walk, sight or cover treats the spot as a solid
  for (const id of ['cyp-office-w', 'cyp-office-e', 'cyp-sq-nw', 'cyp-sq-ne']) expect(live.solidIds).toContain(id);
  expect(live.solidIds).not.toContain('cyp-facade-w'); // planted on civic-facade-southeast, which is a solid already
  expect(live.solidIds).not.toContain('civic-entrance-west'); // the north door's beds went with the door
  expect(live.solidIds).not.toContain('civic-entrance-east');
  expect(bare.solids).toBeUndefined();
  const wall = block.dressing.pieces.find((p: any) => p.piece === 'CafeBay').solids[0].polygon.map(toPlan);
  expect(live.solids!.some(s => JSON.stringify(s) === JSON.stringify(wall))).toBe(true);
});

describe('the dressing\'s ground furniture is a walk solid (Vantage, 2026-10-15: civic.ts had left the planter modules out; 15 then, 12 since the square\'s three went on 2026-10-26)', () => {
  const ids = live.solidIds!, at = (id: string) => live.solids![ids.indexOf(id)]!;
  const size = (poly: V[]) => { const a = poly[0]!, b = poly[1]!, c = poly[2]!; return [Math.hypot(b[0] - a[0], b[1] - a[1]), Math.hypot(c[0] - b[0], c[1] - b[1])].map(x => Math.round(x * 100) / 100); };
  const centre = (poly: V[]): V => [poly.reduce((t, p) => t + p[0], 0) / poly.length, poly.reduce((t, p) => t + p[1], 0) / poly.length];
  const near = (a: V, b: V) => Math.hypot(a[0] - b[0], a[1] - b[1]) < 1e-6;

  test('every placement the block makes of a planter module, a pot, a bollard or a lamp is one, named by its id, and sized and turned as the plan and the dressing say', () => {
    const dr = block.dressing, n = dr.planterRuns.runs.reduce((t: number, r: any) => t + r.modules.length, 0);
    expect(n).toBe(12); // run-sq-south (2) and run-sq-west (1) were removed on 2026-10-26 (Vantage, brief 067 K; Sky: "some odd planters in the fountain area near the car"): the block keeps them under planterRuns.removed
    expect(ids.filter(i => /^run-/.test(i)).length).toBe(12);
    expect(ids.filter(i => /^run-sq-/.test(i))).toEqual([]);
    expect(dr.planterRuns.removed.map((r: any) => r.id)).toEqual(['run-sq-south', 'run-sq-west']);
    expect(ids.filter(i => /^top-/.test(i)).length).toBe(dr.topiary.at.length);
    expect(ids.filter(i => /^bol-/.test(i)).length).toBe(dr.bollards.at.length);
    expect(ids.filter(i => /^lamp-/.test(i)).length).toBe(dr.lamps.at.length);
    // run-east-south/0: PlanterRun_4_B at block (12.5, -8.5), yaw 0 (along block x, the square's east kerb): 4.37 long, 0.68 across, centred on the pivot
    const m = at('run-east-south/0');
    expect(near(centre(m), toPlan([12.5, -8.5]))).toBe(true);
    expect(size(m)).toEqual([4.37, 0.68]);
    const xs = m.map(p => p[0]), ys = m.map(p => p[1]); // plan frame: block x runs -y, block y runs +x, so a run along block x lies along plan y
    expect([Math.max(...xs) - Math.min(...xs), Math.max(...ys) - Math.min(...ys)].map(x => Math.round(x * 100) / 100)).toEqual([0.68, 4.37]);
    expect(size(at('top-office-w'))).toEqual([0.72, 0.72]);
    expect(size(at('bol-cross-1'))).toEqual([0.42, 0.42]);
    expect(size(at('lamp-fore-w'))).toEqual([0.52, 0.52]);
  });

  test('a walk keeps r from a planter run: the module in the south of the square that Vantage removed on 2026-10-26 (the block records it under planterRuns.removed), put back, is gone round and a step through it is refused; as the block stands the ground is open', () => {
    const back = structuredClone(block); back.dressing.planterRuns.runs.push(...back.dressing.planterRuns.removed); // the 15 modules of 2026-10-15, from the block's own record
    const was = civic(plan.civic, back), m = was.solids![was.solidIds!.indexOf('run-sq-south/0')]!, c = centre(m), r = plan.standards.walking.body.r;
    expect(was.solids!.length).toBe(live.solids!.length + 3);
    const solids = new Solids(was.solids!, bodyOf(plan)), open = new Solids(live.solids!, bodyOf(plan));
    const across: V = [0, 1], a: V = [c[0] - across[0] * 3, c[1] - across[1] * 3], b: V = [c[0] + across[0] * 3, c[1] + across[1] * 3];
    expect(solids.clear(a, b)).toBe(false); // straight through the module
    const side = (d: number): V[] => [[c[0] + d, c[1] - 3], [c[0] + d, c[1] + 3]];
    expect(solids.clear(...(side(-(2.185 + r + 0.05)) as [V, V]))).toBe(true); // past its west end (its neighbour run-sq-south/1 lies east), 0.05 m clear of the body's reach
    expect(solids.clear(...(side(-(2.185 + r - 0.05)) as [V, V]))).toBe(false); // 0.05 m inside it
    expect(open.clear(a, b)).toBe(true); // as the block stands nothing is there: the walk goes straight across the south-west quarter
    expect(open.clear(...(side(-(2.185 + r - 0.05)) as [V, V]))).toBe(true);
  });

  test('a piece the plan gives no footprint fails civic() by name; a plan with no groundPieces walks as it always did (41 solids; 42 before the steward monument left the block on 2026-10-26)', () => {
    const p2 = structuredClone(plan.civic); delete p2.groundPieces.sizes.PlanterRun_4_C; // the first run with a C module names it
    expect(() => civic(p2, block)).toThrow(/run-[a-z-]+\/\d is PlanterRun_4_C, which plan.civic.groundPieces.sizes gives no footprint/);
    const p3 = structuredClone(plan.civic); delete p3.groundPieces;
    expect(civic(p3, block).solids!.length).toBe(41);
  });
});

test('Crew line → Between tables is 26.2 m round the terrace wall (17.7 m with no wall); the lane by the Office west face → Office entrance is 9.1 m (5.3 m with no cheeks or beds)', () => {
  const between = toPlan([-7.1, -35.45]), service = toPlan([69.4, -41.1]), office = toPlan([64, -41.25]); // the old service door's spot in the lane, and the public door on the Office's west face (2026-10-19, option b)
  expect(walkBetween(plan, d, live, crewLine, between)).toBeCloseTo(26.2, 0);
  expect(walkBetween(plan, d, live, service, office)).toBeCloseTo(9.1, 0);
  expect(walkBetween(plan, d, bare, crewLine, between)).toBeCloseTo(17.65, 1);
  expect(walkBetween(plan, d, bare, service, office)).toBeCloseTo(5.3, 0);
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
  // no shorter, to the grid: a walk with solids starts at the door's own point, a bare one at its cell's centre, so the two differ by under a cell where a route turns (the pavements walk since 2026-10-19: one stop reads 0.8 m under its bare walk)
  r1.forEach((r, i) => expect(r.m).toBeGreaterThanOrEqual(r0[i]!.m - (plan.standards.walking.grid as number)));
  const w1 = walks(plan, derive(plan), live), w0 = walks(plan, derive(plan), bare);
  expect(w1.length).toBe(w0.length);
  for (const w of w1) expect(w.m / w.straight).toBeLessThanOrEqual(plan.standards.walking.directness);
});
