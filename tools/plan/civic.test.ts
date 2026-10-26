// C-0: the civic block holds its city scale (plan.civic.scale: the café's footprint is the Oct 9 source's; its height is the plan's, 34.8 from the
// source or 16.8 under brief 066's play-street cap, so nothing here names either); a smaller café or a lower office fails the check.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { resolve } from 'node:path';
import { check } from './checks';
import { civic } from './civic';
import { derive, type Plan } from './plan';

setDefaultTimeout(30000);
const root = resolve(import.meta.dir, '../..'), read = async (p: string) => JSON.parse(await Bun.file(resolve(root, p)).text());
const plan: Plan = await read('contracts/world/harbour-district.plan.json'), block = await read(plan.civic.block);
const c0 = (b: any) => check(plan, derive(plan), civic(plan.civic, b)).checks.find(c => c.id === 'C-0')!;

test('C-0 passes on the committed civic block', () => expect(c0(block).ok).toBe(true));

test('C-0 holds the café at the plan\'s height, whichever it states (34.8 from the source, 16.8 under brief 066)', () => {
  for (const h of [34.8, 16.8]) {
    const p = structuredClone(plan), b = structuredClone(block);
    p.civic.scale.cafe.height = h; b.buildings.find((x: any) => x.id === 'cafe').height = h;
    expect(check(p, derive(p), civic(p.civic, b)).checks.find(c => c.id === 'C-0')!.ok).toBe(true);
    b.buildings.find((x: any) => x.id === 'cafe').height = h === 34.8 ? 16.8 : 34.8;
    expect(check(p, derive(p), civic(p.civic, b)).checks.find(c => c.id === 'C-0')!.ok).toBe(false);
  }
});

test('C-0 fails when the café shrinks (the Oct 5 18.5 × 10.5 m café)', () => {
  const b = structuredClone(block), cafe = b.buildings.find((x: any) => x.id === 'cafe');
  cafe.polygon = cafe.polygon.map(([x, y]: number[]) => [x * 0.84, y]);
  expect(c0(b).ok).toBe(false);
});

test('C-0 fails when the café or office loses height', () => {
  const low = structuredClone(block); low.buildings.find((x: any) => x.id === 'cafe').height = plan.civic.scale.cafe.height / 4; // a quarter of whatever height the plan states
  expect(c0(low).ok).toBe(false);
  const flat = structuredClone(block); flat.buildings.find((x: any) => x.id === 'civic-office').heights.crown = 12;
  expect(c0(flat).ok).toBe(false);
});

test('an old suborned.block/1 file is refused, not drawn', () => {
  expect(() => civic(plan.civic, { ...block, schema: 'suborned.block/1' })).toThrow('suborned.block/4');
});

test('the world block holds no mission: no destinations, no crew, bays named by place', () => {
  expect(block.destinations).toBeUndefined(); expect(block.crew).toBeUndefined();
  expect(block.bays.map((b: any) => b.id)).toEqual(['officeBay', 'squareBay', 'serviceBay']); // serviceBay: the Office's east yard (Sky 2026-10-19, door option b), reached by the service-drive
});

test('W-4 fails when a walk starts away from any building', () => {
  const b = structuredClone(block); b.buildings[0].entrances[0].at = [-60, -29];
  const R = check(plan, derive(plan), civic(plan.civic, b)).checks.find(c => c.id === 'W-4')!;
  expect(R.ok).toBe(false);
});
