// DR-1..DR-3 (brief 023): the drive rules, each with a failing example, and the drive in the committed block. Failing examples are the committed block with one thing changed.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { check } from './checks';
import { civic } from './civic';
import { derive, type Plan } from './plan';

setDefaultTimeout(60000);
const root = resolve(import.meta.dir, '../..'), json = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = json('contracts/world/harbour-district.plan.json'), block = json(plan.civic.block), d = derive(plan);
const run = (mutate: (b: any) => void = () => {}) => {
  const b = structuredClone(block); mutate(b);
  return Object.fromEntries(check(plan, d, civic(plan.civic, b)).checks.filter(c => c.id.startsWith('DR-')).map(c => [c.id, c]));
};
const drive = (b: any) => b.drives[0];

describe('the committed block\'s drive', () => {
  test('is the service-drive: apron-road to serviceBay, kerb x 80, y -16.5..-13.5, in block frame with the shape tools/sim/drive.ts reads ({ id, polygon })', () => {
    expect(block.drives).toHaveLength(1);
    expect(block.drives[0]).toMatchObject({ id: 'service-drive', road: 'apron-road', kerb: [[80, -16.5], [80, -13.5]], to: 'serviceBay', polygon: [[67, -16.5], [80, -16.5], [80, -13.5], [67, -13.5]] });
    expect(block.drives[0].reason).toMatch(/Reserved|quiet back way/);
    expect(block.dressing.keepClear.some((z: { id: string }) => z.id === 'service-drive')).toBe(false); // the keep-clear zone it replaces
  });
  test('the three rules pass; the two lay-bys that touch a road need no drive', () => {
    const r = run();
    expect(Object.keys(r)).toEqual(['DR-1', 'DR-2', 'DR-3']);
    expect(Object.values(r).every(c => c.ok)).toBe(true);
    expect(r['DR-1']!.value).toBe('1 bays out of a road\'s reach, 1 drives');
  });
  test('a block with no drive and no bay out of a road\'s reach passes all three (every recorded run\'s pinned block)', () => {
    const r = run(b => { b.drives = []; b.bays = b.bays.filter((x: { id: string }) => x.id !== 'serviceBay'); });
    expect(Object.values(r).every(c => c.ok)).toBe(true);
  });
});

describe('DR-1: a bay that touches no road is reached by a drive', () => {
  test('fails with the drive taken away', () => { expect(run(b => { b.drives = []; })['DR-1']).toMatchObject({ ok: false, detail: expect.stringMatching(/bay serviceBay touches no road and no drive reaches it/) }); });
  test('fails with the drive reaching a bay the block does not have', () => { expect(run(b => { drive(b).to = 'noBay'; })['DR-1']!.detail).toMatch(/reaches bay noBay, which the block does not have/); });
  test('fails with a gap between the drive and the bay', () => { expect(run(b => { drive(b).polygon = [[69, -16.5], [80, -16.5], [80, -13.5], [69, -13.5]]; })['DR-1']!.detail).toMatch(/does not meet bay serviceBay/); });
  test('fails with a kerb segment off the road\'s carriageway edge', () => { expect(run(b => { drive(b).kerb = [[78, -16.5], [78, -13.5]]; })['DR-1']!.detail).toMatch(/kerb segment is 2\.00 m off Apron Road's carriageway edge/); });
  test('fails with a kerb segment that is not on the drive', () => { expect(run(b => { drive(b).polygon = [[67, -16.5], [79, -16.5], [79, -13.5], [67, -13.5]]; })['DR-1']!.detail).toMatch(/kerb segment is not on the drive/); });
  test('fails with a road the plan does not have', () => { expect(run(b => { drive(b).road = 'no-such-road'; })['DR-1']!.detail).toMatch(/leaves road no-such-road, which the plan does not have/); });
});

describe('DR-2: a drive crosses only its own plot\'s pavement', () => {
  test('fails with a drive that runs out of its plot into the carriageway', () => { expect(run(b => { drive(b).polygon = [[67, -16.5], [84, -16.5], [84, -13.5], [67, -13.5]]; })['DR-2']!.detail).toMatch(/leaves its own plot/); });
  test('fails with a drive that reaches across a street into another plot (the square\'s)', () => {
    const r = run(b => { drive(b).polygon = [[28, -16.5], [80, -16.5], [80, -13.5], [28, -13.5]]; })['DR-2']!;
    expect(r.ok).toBe(false); expect(r.detail).toMatch(/crosses the pavement of plot .*, not its own/); expect(r.detail).toMatch(/lies in the carriageway of/);
  });
  test('fails with a kerb segment that is not on its plot\'s edge', () => { expect(run(b => { drive(b).kerb = [[79, -16.5], [79, -13.5]]; })['DR-2']!.detail).toMatch(/kerb segment is not on the edge of its own plot/); });
});

describe('DR-3: nothing of the block stands on a drive', () => {
  test('fails with a planter on it', () => { expect(run(b => { b.objects.push({ id: 'stray-planter', kind: 'planter', polygon: [[70, -16], [72, -16], [72, -14], [70, -14]], z: 0, height: 0.6 }); })['DR-3']!.detail).toMatch(/stray-planter stands on drive service-drive/); });
  test('fails with a structure on it', () => { expect(run(b => { b.structures.push({ id: 'stray-cabinet', name: 'Cabinet', category: 'service-cabinet', polygon: [[74, -15.5], [75, -15.5], [75, -14.5], [74, -14.5]], z: 0, height: 1.5 }); })['DR-3']!.detail).toMatch(/stray-cabinet stands on drive service-drive/); });
  test('fails with a building footprint over it', () => { expect(run(b => { const o = b.buildings.find((x: { id: string }) => x.id === 'civic-office'); o.polygon = o.polygon.map((p: number[]) => [p[0], p[1] === -19.5 ? -14 : p[1]]); })['DR-3']!.ok).toBe(false); });
});
