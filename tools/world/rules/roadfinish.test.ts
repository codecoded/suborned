// RF-3 on the live world (brief 028 Road): it passes on the world the generator makes and fails on one thing put wrong, on a copy of it.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { StreetItem, World } from '../../../contracts/world/world.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { ctxOf, generated } from '../../plan/world-ctx';
import { roadFinishRules } from './roadfinish';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block)), base = generated(plan, d, civ);
const failing = (mutate: (w: World) => void = () => {}): string => {
  const w = structuredClone(base.w) as World; mutate(w);
  const rf = roadFinishRules(ctxOf(plan, d, civ, { w, gen: base.gen })).find(c => c.id === 'RF-3')!;
  return rf.ok ? '' : (rf.detail ?? '');
};
const first = (w: World, kind: string): StreetItem => w.ground.pieces!.find(s => s.kind === kind)!;

describe('the road finish rule RF-3', () => {
  test('the live world passes, and the finish is there: five kinds, thousands of pieces', () => {
    expect(failing()).toBe('');
    const n = (k: string) => base.w.ground.pieces!.filter(s => s.kind === k).length;
    for (const k of ['road-ribbon', 'road-shoulder', 'road-pinstripe', 'road-lozenge', 'road-seam']) expect(n(k), k).toBeGreaterThan(1000);
    expect(base.gen.roadFinish!.gaps.every(g => g.rule && g.where && g.why)).toBe(true);
  });
  test('it fails on a ribbon piece laid in the middle of a crossing strip', () => {
    expect(failing(w => { const x = w.crossings![0]!; first(w, 'road-ribbon').at = [(x.strip[0]![0] + x.strip[2]![0]) / 2, (x.strip[0]![1] + x.strip[2]![1]) / 2]; })).toMatch(/lies on or within 0\.3 m of a crossing strip/);
  });
  test('it fails on a shoulder in a tram platform and on a seam in a drive', () => {
    expect(failing(w => { const r = w.transit.stops[0]!.ring, s = first(w, 'road-shoulder'); s.at = [(r[0]![0] + r[2]![0]) / 2, (r[0]![1] + r[2]![1]) / 2]; })).toMatch(/tram platform/); // the platform's middle
    expect(failing(w => { const r = w.ground.drives![0]!.ring; first(w, 'road-seam').at = [(r[0]![0] + r[2]![0]) / 2, (r[0]![1] + r[2]![1]) / 2]; })).toMatch(/a drive/);
  });
  test('it fails on a piece that is not the family\'s kit piece, one that is not adopted, and one at the wrong level', () => {
    expect(failing(w => { first(w, 'road-lozenge').kit = { id: 'Bench', scale: [1, 1, 1] }; })).toMatch(/Bench is not an adopted ground:road-lozenge piece/);
    expect(failing(w => { first(w, 'road-pinstripe').kit = { id: 'WashingtoniaPalm_Street', scale: [1, 1, 1] }; })).toMatch(/not an adopted ground:pave-pinstripe piece/);
    expect(failing(w => { first(w, 'road-ribbon').z = 0.18; })).toMatch(/road-ribbon at z 0\.18, not 0 or 0\.02/);
  });
  test('it fails on two lozenges of one carriageway closer than their length and stopShort', () => {
    expect(failing(w => { const a = first(w, 'road-lozenge'), b = w.ground.pieces!.find(s => s.kind === 'road-lozenge' && s.edge === a.edge && s !== a)!; b.at = [a.at[0] + 1, a.at[1]]; })).toMatch(/two lozenges 1\.00 m apart/);
  });
  test('every piece of the finish stands at the same place twice (deterministic)', () => {
    const again = generated(plan, d, civ), ids = (w: World) => w.ground.pieces!.filter(s => s.id.startsWith('street/roadfinish/')).map(s => `${s.id}@${s.at}@${s.kit?.scale}`);
    expect(ids(again.w)).toEqual(ids(base.w));
  });
});
