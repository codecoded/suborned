// The four point moves Vantage wrote for the Handover (civic-block.adjust.json `pending.missionMoves`, brief 008 B2/B3) applied IN MEMORY:
// the mission file and its preservation lock are Waymark's to change (brief 003) and stay as they are. With the moves, the overlay passes
// on the block that states the tower plans; without them the file's own points are what mission.test.ts checks.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { resolve } from 'node:path';
import { check } from '../../plan/checks';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { legacyOverlay } from './overlay';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), json = async (p: string) => JSON.parse(await Bun.file(resolve(root, p)).text());
const plan: Plan = await json('contracts/world/harbour-district.plan.json'), block = await json(plan.civic.block);
const mission = await json('contracts/missions/handover/mission.json'), moves = (await json('contracts/world/civic-block.adjust.json')).pending.missionMoves.destinations;

test('the four pending point moves each name a destination at the place they move it from', () => {
  expect(moves.map((m: any) => m.name)).toEqual(['Office entrance', 'Office approach', 'Official entrance', 'Meet Vale']);
  for (const m of moves) expect(mission.destinations.find((d: any) => d.name === m.name && d.at[0] === m.from[0] && d.at[1] === m.from[1]), m.name).toBeDefined();
});

const run = (b: any, destinations: unknown[]) => {
  const o = legacyOverlay({ ...plan, routes: mission.routes }, { destinations })!;
  return check(plan, derive(plan), civic(plan.civic, b), false, o);
};
const moved = () => mission.destinations.map((d: any) => {
  const m = moves.find((x: any) => x.name === d.name && x.from[0] === d.at[0] && x.from[1] === d.at[1]);
  return m ? { ...d, at: m.to } : d;
});

test('with the four moves applied in memory the Handover overlay passes: W-4 included, every route reaches its end', () => {
  const R = run(block, moved()), by = (id: string) => R.checks.find(c => c.id === id)!;
  expect(R.checks.filter(c => !c.ok).map(c => `${c.id} ${c.detail ?? ''}`)).toEqual([]);
  expect(by('W-4').value).toBe('2/2'); // Café door, Office entrance (the overlay's walk origins)
  expect(by('C-3').value).toBe(`${mission.destinations.length}/${mission.destinations.length}`);
  expect(by('M-1').ok).toBe(true);
});

test('the moves need the tower plans: on a block that states none, W-4 fails the Office entrance (Vantage\'s only failure)', () => {
  const bare = structuredClone(block); for (const t of bare.buildings.find((x: any) => x.id === 'civic-office').towers) delete t.polygon;
  const R = run(bare, moved());
  expect(R.checks.filter(c => !c.ok).map(c => `${c.id} ${c.detail}`)).toEqual(['W-4 Office entrance']);
});
