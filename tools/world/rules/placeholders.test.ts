// NP-1 no placeholders (brief 075). The live world passes; each failing example puts one stand-in back.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { World } from '../../../contracts/world/world.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { ctxOf, generated } from '../../plan/world-ctx';
import { catalogued, landmarkKit } from '../gen/kitrefs';
import { placeholderFaults, placeholderRules } from './placeholders';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block)), base = generated(plan, d, civ);
const clone = (w: World): World => structuredClone(w);
const faults = (w: World, p: Plan = plan) => placeholderFaults(p, w).faults;

test('the live world: NP-1 passes', () => {
  const r = placeholderRules(ctxOf(plan, d, civ, base))[0]!;
  expect(r.ok, r.detail).toBe(true);
});

test('failing example: a gate tower whose DiscTower does not fit its disc is drawn as a cylinder (the "ugly blue buildings")', () => {
  const w = clone(base.w), pl = plan.landmarks.find(l => l.id === 'disc-west')!, i = w.landmarks.findIndex(l => l.id === 'landmark/disc-west');
  const m = landmarkKit({ ...w.landmarks[i]!, source: undefined, kit: undefined, height: 108 }, { ...pl, r: 4 }); // the generator's own fit, on a disc 8 m across
  expect(m.source).toBe('procedural');
  w.landmarks[i] = m;
  expect(faults(w).join(' ')).toContain('landmark/disc-west: procedural stand-in for DiscTower');
});

test('failing example: a station whose piece is not adopted (the generator skips it) fails, not silence', () => {
  const w = clone(base.w); w.signatures = w.signatures.filter(s => s.id !== 'station/S1/gardens');
  expect(faults(w)).toContain('station/S1/gardens: no piece placed for SkyRailStation_Twin (not adopted)');
});

test('failing example: a gateway arch the plan names but the export does not place', () => {
  const w = clone(base.w); delete w.landmarks.find(l => l.id === 'landmark/disc-west')!.gateway;
  expect(faults(w).join(' ')).toContain('landmark/disc-west: its gateway ParabolicArch@crown-avenue is not placed');
});

test('failing example: a pier tower megastructure with no adopted piece tagged for it', () => {
  const p = structuredClone(plan); p.megastructures = [...(p.megastructures ?? []), { ...p.megastructures![0]!, id: 'no-such-pier', kind: 'pier tower' }];
  expect(faults(base.w, p).join(' ')).toContain('mega/no-such-pier: a pier tower with no adopted piece tagged landmark:no-such-pier');
});

test('failing example: a facade naming a candidate piece, and a wall left undressed', () => {
  const cand = 'TrafficLight';
  expect(catalogued(cand)?.status).toBe('candidate');
  const w = clone(base.w); w.facades[0]!.kit.id = cand;
  expect(faults(w).join(' ')).toContain(`${cand}: 1 ref to a piece that is not adopted (candidate)`);
  const w2 = clone(base.w), b = w2.facades[0]!.building; w2.facades = w2.facades.filter(f => f.building !== b);
  expect(faults(w2).some(f => f.startsWith(`${b}/`) && f.includes('undressed'))).toBe(true);
});

test('allowed: a signature standing as its adopted base piece while its regenerated variant is pending', () => {
  const w = clone(base.w), s = w.signatures.find(x => x.id === 'signature/space-hq')!; s.pending = 'SpaceHQ_Portal@x';
  const r = placeholderFaults(plan, w);
  expect(r.faults).toEqual([]);
  expect(r.pending[0]).toContain('signature/space-hq');
});
