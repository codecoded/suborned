// bun run block (brief 008 B3): the adjustment ops towerFootprint, moveEntrance, addEntrance, remove and dressing; W-4 and the walk
// blockers reading buildings and towers together; a block without stated tower plans (block/2, /3: recorded runs) read as before.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { createHash } from 'node:crypto';
import { existsSync, mkdtempSync, readFileSync, writeFileSync } from 'node:fs';
import { homedir, tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { check } from './checks';
import { civic, footprints } from './civic';
import { derive, type Plan } from './plan';

setDefaultTimeout(60000);
const root = resolve(import.meta.dir, '../..'), json = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = json('contracts/world/harbour-district.plan.json'), block = json(plan.civic.block), ADJ = 'contracts/world/civic-block.adjust.json';
const adjust = json(ADJ), office = (b: any) => b.buildings.find((x: any) => x.id === 'civic-office');
const w4 = (b: any) => check(plan, derive(plan), civic(plan.civic, b)).checks.find(c => c.id === 'W-4')!;
const sources = existsSync(join(homedir(), 'Projects/the-suborned/game/Assets/TheSuborned/Data/World/handover-navigation.json'));

describe('the committed block', () => {
  test('is block/4 with the tower plans, the moved and the added entrance, the supports gone and the dressing carried', () => {
    expect(block.schema).toBe('suborned.block/4');
    expect(office(block).towers.map((t: any) => [t.id, t.polygon])).toEqual([
      ['public', [[52.5, -33], [58.5, -33], [58.5, -27], [52.5, -27]]], ['official', [[69.5, -33], [75.5, -33], [75.5, -27], [69.5, -27]]]]);
    expect(office(block).entrances).toMatchObject([
      { id: 'office-entrance', kind: 'public', at: [64, -41.25], z: 0.75 }, { id: 'official-entrance', kind: 'official', at: [76.25, -30], z: 0 },
      { id: 'office-service-door', kind: 'service', at: [62.5, -18.9], z: 0 }]); // brief 013: the service door, with its kind; 2026-10-19 (Sky, door option b): the public door is on the wing's west face, the service door on its east face
    expect(block.buildings.find((x: any) => x.id === 'cafe').entrances).toMatchObject([{ id: 'cafe-entrance', kind: 'public' }, { id: 'cafe-kitchen-door', kind: 'service', at: [-22.4, -26] }]);
    expect(block.structures.map((s: any) => s.id).filter((id: string) => id.startsWith('office-pavilion-support'))).toEqual([]);
    expect(block.dressing).toEqual(adjust.dressing);
    expect(adjust.pending.ops).toEqual([]); // each op moved out of pending, its reason kept
    expect(block.adjustments.applied.filter((a: any) => ['towerFootprint', 'moveEntrance', 'addEntrance', 'remove', 'dressing'].includes(a.op)).map((a: any) => a.op))
      .toEqual(['towerFootprint', 'towerFootprint', 'moveEntrance', 'addEntrance', 'remove', 'remove', 'addEntrance', 'addEntrance', 'moveEntrance', 'moveEntrance', 'remove', 'remove', 'dressing']);
    expect(block.adjustments.applied.every((a: any) => a.reason.length > 20)).toBe(true);
  });

  test('is the block the adjust file makes (run bun run block after editing it)', () => {
    expect(block.adjustments.sha256).toBe(createHash('sha256').update(readFileSync(resolve(root, ADJ))).digest('hex'));
  });
});

describe('towers in W-4 and the walk blockers', () => {
  const bare = (b: any) => { const c = structuredClone(b); for (const t of office(c).towers) delete t.polygon; c.schema = 'suborned.block/3'; return c; };

  test('footprints (the walk grid\'s blockers and W-4\'s doors) hold the buildings and the towers together; a block without tower plans holds the buildings only', () => {
    expect(footprints(civic(plan.civic, block)).length).toBe(4);
    expect(footprints(civic(plan.civic, bare(block))).length).toBe(2);
  });

  test('W-4: the public door stands on the wing\'s west face and passes with or without the tower plan; the official door, 0.75 m off a tower face, is stray without it', () => {
    expect(w4(block).ok).toBe(true);
    expect(w4(bare(block))).toMatchObject({ ok: false, detail: 'Official entrance' });
  });

  test('W-4 still fails an entrance away from every footprint, a tower\'s included', () => {
    const b = structuredClone(block); office(b).entrances[0].at = [51.75, -20];
    expect(w4(b)).toMatchObject({ ok: false, detail: 'Office entrance' });
  });
});

describe.skipIf(!sources)('bun tools/plan/block.ts --adjust (needs the Oct 9 sources)', () => {
  const dir = mkdtempSync(join(tmpdir(), 'block-ops-')), run = (edit: (a: any) => void) => {
    const a = structuredClone(adjust); edit(a);
    writeFileSync(join(dir, 'adjust.json'), JSON.stringify(a)); // compact is fine: the block records the file's hash
    const p = Bun.spawnSync(['bun', 'tools/plan/block.ts', '--adjust', join(dir, 'adjust.json'), '--out', join(dir, 'block.json')], { cwd: root });
    return { code: p.exitCode, err: p.stderr.toString(), out: () => JSON.parse(readFileSync(join(dir, 'block.json'), 'utf8')) };
  };
  const ops = (a: any) => a.adjustments;

  test('the committed adjust file makes the committed block (apart from the file hash)', () => {
    const r = run(() => {});
    expect(r.code).toBe(0);
    const { adjustments: x, ...made } = r.out(), { adjustments: y, ...had } = block;
    expect(made).toEqual(had);
    expect(x.applied).toEqual(y.applied);
  });

  test('towerFootprint for a missing tower, moveEntrance of a missing entrance, remove of a missing item and a duplicate addEntrance each stop the build', () => {
    const bad = (op: object) => run(a => ops(a).push({ reason: 'test', ...op }));
    expect(bad({ op: 'towerFootprint', building: 'civic-office', tower: 'north', polygon: [[0, 0], [1, 0], [1, 1]] }).err).toContain('has no tower north');
    expect(bad({ op: 'towerFootprint', building: 'civic-office', tower: 'public' }).err).toContain('needs a polygon');
    expect(bad({ op: 'moveEntrance', id: 'back-door', to: [1, 1] }).err).toContain('no such entrance');
    expect(bad({ op: 'remove', id: 'no-such-pier' }).err).toContain('no such item');
    expect(bad({ op: 'addEntrance', building: 'civic-office', item: { id: 'office-entrance', name: 'x', at: [0, 0] } }).err).toContain('exists');
    expect(bad({ op: 'addEntrance', building: 'civic-hall', item: { id: 'x', name: 'x', at: [0, 0] } }).err).toContain('no building civic-hall');
    expect(bad({ op: 'addEntrance', building: 'civic-office', item: { id: 'back-door', name: 'x', kind: 'trade', at: [0, 0] } }).err).toContain('kind trade is none of public, official, service');
    expect(bad({ op: 'twist', id: 'x' }).err).toContain('unknown adjustment op');
    expect(bad({ op: 'remove', id: 'square-pavilion-support-0', reason: ' ' }).err).toContain('without a reason');
    expect(run(a => { delete a.dressing; }).err).toContain('no `dressing` key');
  });

  test('the ops do what they say, and C-0 still locks the scale', () => {
    const r = run(a => { ops(a).push({ op: 'remove', id: 'square-pavilion-support-1', reason: 'test' }); ops(a).push({ op: 'moveEntrance', id: 'cafe-entrance', to: [-10, -29], reason: 'test' }); });
    expect(r.code).toBe(0);
    const b = r.out();
    expect(b.structures.some((s: any) => s.id === 'square-pavilion-support-1')).toBe(false);
    expect(b.buildings.find((x: any) => x.id === 'cafe').entrances[0].at).toEqual([-10, -29]);
    expect(check(plan, derive(plan), civic(plan.civic, b)).checks.find(c => c.id === 'C-0')!.ok).toBe(true);
  });
});
