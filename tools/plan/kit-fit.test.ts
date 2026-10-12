// K-1: the generated world passes; a kit piece too big for its slot, a gateway foot on a pylon, and a tower tip over the ceiling each fail.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic } from './civic';
import { derive, type Plan } from './plan';
import { worldOf } from '../world/build';
import { kitFitChecks } from './kit-fit';
import { ctxOf } from './world-ctx';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const { world, gen } = worldOf(plan, d, civ), v = { w: world, gen }, run = (p: Plan = plan, x = v) => kitFitChecks(ctxOf(p, d, civ, x))[0]!;

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
