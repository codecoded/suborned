// K-1: the generated world passes; a kit piece too big for its slot, a gateway foot on a pylon, and a tower tip over the ceiling each fail.
// K-2: the placed tower pieces stand to the tips their orders state; a height cut the pieces were not regenerated for fails, naming the sites.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic } from './civic';
import { derive, type Plan } from './plan';
import { worldOf } from '../world/build';
import { kitFitChecks } from './kit-fit';
import { TIP_TOL, ctxOf } from './world-ctx';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const { world, gen } = worldOf(plan, d, civ), v = { w: world, gen }, all = (p: Plan = plan, x = v) => kitFitChecks(ctxOf(p, d, civ, x));
const run = (p: Plan = plan, x = v) => all(p, x)[0]!, k2 = (p: Plan = plan, x = v) => all(p, x).find(c => c.id === 'K-2')!;
/** A clone of the plan with the named tower sites' stated tips moved by `by` metres (the world, and so the placed pieces, stay as built). */
const tips = (by: Record<string, number>): Plan => {
  const p: Plan = structuredClone(plan);
  for (const s of p.standards.buildings.towers.sites as { id: string; regen: { tip: number } }[]) if (s.id in by) s.regen.tip += by[s.id]!;
  return p;
};

test('K-1 passes on the plan', () => { const k = run(); expect(k.id).toBe('K-1'); expect(k.ok).toBe(true); });

test('K-1 fails a landmark piece scaled past its disc, a gateway on the beam and a tower tip over crownTip', () => {
  const w = structuredClone(v.w); w.landmarks.find(l => l.id === 'landmark/sphere')!.kit!.scale = [0.6, 0.6, 0.6];
  expect(run(plan, { ...v, w }).detail).toContain('landmark/sphere');
  const p: Plan = structuredClone(plan), g = (p.megastructures!.find(m => m.id === 'skyport-tower') as any).gateway;
  g.feet[0] = [479.1, 485.9, 0.5, 7];
  expect(run(p).detail).toContain('gateway/foot-0');
  const q: Plan = structuredClone(plan); q.standards.buildings.towers.crownTip = 70;
  expect(run(q).detail).toContain('tip 75');
});

test('K-2 passes on the plan: every placed tower piece stands to its order\'s tip', () => {
  const k = k2();
  expect(k.ok).toBe(true); expect(k.detail).toBeUndefined();
  expect(Number(k.value.split('/')[1])).toBeGreaterThan(0); // it judged placed pieces, not an empty list
});

test('K-2 fails the heights-cut variant: the needle and both avenue gates, and nothing else', () => {
  // out/plan/variants/heights-cut.plan.json: avenue-needle 30 -> 26 storeys (tip -14.4), the avenue gates 33 -> 30 (-10.8); the pieces are fixed-size, so the world is unchanged
  const k = k2(tips({ 'avenue-needle': -14.4, 'avenue-gate-w': -10.8 }));
  expect(k.ok).toBe(false);
  expect(k.detail).toContain('3 fail');
  expect(k.detail).toMatch(/avenue-needle: Tower_Needle@avenue-needle built to [\d.]+ m, regen.tip [\d.]+ \(\+14\.\d m\)/);
  expect(k.detail).toMatch(/avenue-gate-w: DiscTower@avenue-gate-w .*\(\+10\.\d m\)/);
  expect(k.detail).toMatch(/avenue-gate-e: DiscTower@avenue-gate-e .*its pair avenue-gate-w's regen\.tip/); // the matched partner takes the tip of its pair
  expect(k.detail).not.toContain('crescent-terrace');
  expect(run(tips({ 'avenue-needle': -14.4 })).ok).toBe(true); // K-1 (the ceiling) is not what caught it
});

test('K-2 holds the piece to the tolerance either way, and does not judge a site whose piece is not adopted', () => {
  expect(k2(tips({ 'avenue-needle': TIP_TOL - 0.1 })).ok).toBe(true);
  expect(k2(tips({ 'avenue-needle': TIP_TOL + 0.1 })).ok).toBe(false);
  expect(k2(tips({ 'avenue-needle': -(TIP_TOL + 0.1) })).ok).toBe(false); // a piece shorter than its order is as wrong as a taller one
  const w = structuredClone(v.w), i = w.signatures.findIndex(s => s.id === 'tower/avenue-needle');
  w.signatures[i] = { id: 'tower/avenue-needle', kind: 'tower', source: 'procedural', base: w.signatures[i]!.base, tip: 0, pending: 'Tower_Needle@avenue-needle' };
  expect(k2(tips({ 'avenue-needle': -14.4 }), { ...v, w }).ok).toBe(true); // pending: the procedural tower stands, nothing built to hold against the order
});
