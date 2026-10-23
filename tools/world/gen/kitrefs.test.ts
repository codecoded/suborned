// Kit references (brief 006 A): every kit piece fits the slot it fills (a lamp in the kerb zone), signs never repeat on neighbours,
// only adopted pieces appear, and the references are deterministic.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Kit } from '../../../contracts/world/kit.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { fit, scaled } from '../../kit/fit';
import { worldOf } from '../build';
import { ctxOf } from './common';
import { kerbZone, slotOf } from './kitrefs';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const kit: Kit = read('contracts/world/kit.json'), by = new Map(kit.assets.map(a => [a.id, a]));
const { world: w } = worldOf(plan, d, civ), ctx = ctxOf(plan, d, civ);

test('every street kit piece stays in its slot and its scale range', () => {
  let n = 0;
  for (const s of w.street) {
    if (!s.kit) { expect(s.source).toBe('procedural'); continue; }
    if (s.crossing) continue; // a crossing's part is bound to its piece by its own size (street/partkit.test.ts), not by a slot
    const a = by.get(s.kit.id)!; n++;
    expect(a.status).toBe('adopted');
    const [lo, hi] = a.tags.scale, [sx, sy, sz] = s.kit.scale;
    expect(sy).toBeGreaterThanOrEqual(lo - 1e-6); expect(sy).toBeLessThanOrEqual(hi + 1e-6);
    expect(Math.min(sx, sz) / sy).toBeGreaterThanOrEqual(a.tags.squash - 1e-3);
    const slot = slotOf(ctx, s)!, [w3, h3, d3] = scaled(a, s.kit.scale);
    if (slot.along !== undefined) expect(w3).toBeLessThanOrEqual(slot.along + 1e-2);
    if (slot.depth !== undefined) expect(d3).toBeLessThanOrEqual(slot.depth + 1e-2);
    if (slot.maxH !== undefined) expect(h3).toBeLessThanOrEqual(slot.maxH + 1e-2);
  }
  expect(n).toBeGreaterThan(1000);
});

test('a lamp fits the kerb zone (the narrowest in the plan)', () => {
  const lamps = w.street.filter(s => s.kind === 'lamp' && s.kit);
  expect(lamps.length).toBeGreaterThan(100);
  for (const l of lamps) expect(scaled(by.get(l.kit!.id)!, l.kit!.scale)[2]).toBeLessThanOrEqual(kerbZone(ctx) + 1e-2);
  expect(fit(by.get('Streetlamp')!, { h: 6, depth: 0.5 })).toBeNull(); // a slot too shallow is refused, never squeezed past the squash limit
});

test('no adjacent duplicate sign', () => {
  const signed = w.frontages.filter(f => f.signKit);
  expect(signed.length).toBeGreaterThan(100);
  for (let i = 0; i < signed.length; i++) for (let j = i + 1; j < signed.length; j++) {
    const a = signed[i]!, b = signed[j]!; if (a.signKit!.id !== b.signKit!.id) continue;
    expect(Math.hypot(a.sign.c[0]! - b.sign.c[0]!, a.sign.c[1]! - b.sign.c[1]!)).toBeGreaterThanOrEqual((a.w + b.w) / 2 + 2);
  }
});

test('the nearest earlier tree or lamp differs where fitting alternatives exist', () => {
  const covered: string[] = [];
  // tub palms are left out: 186 of 198 stand in a planter's bed and take the one bare palm (FanPalm; brief 020), so there is nothing to contrast
  for (const [kind, reach] of [['tree', 15], ['lamp', 45]] as [string, number][]) {
    const useOf = (s: typeof w.street[number]) => (kind === 'tree' && s.variant && s.variant !== 'phoenix' ? `tree:${s.variant}` : kind); // an avenue palm's slot names its species (tree:coconut, tree:coconut-median): one piece each; the phoenix slots take any `tree` piece
    const choices = (s: typeof w.street[number]) => kit.assets.filter(a => a.status === 'adopted' && a.tags.use.includes(useOf(s)) && fit(a, slotOf(ctx, s)!)).length;
    const xs = w.street.filter(s => s.kind === kind && s.kit); // pairs count only where more than one piece fits (else nothing to contrast)
    let same = 0, pairs = 0;
    xs.forEach((a, i) => {
      let best: typeof a | undefined, bd = reach;
      for (let j = 0; j < i; j++) { if (useOf(xs[j]!) !== useOf(a)) continue; const dd = Math.hypot(a.at[0] - xs[j]!.at[0], a.at[1] - xs[j]!.at[1]); if (dd < bd) { bd = dd; best = xs[j]; } } // the choice contrasts within one slot (a grid per use)
      if (best && choices(a) > 1) { pairs++; if (best.kit!.id === a.kit!.id) same++; }
    });
    if (!xs.length) continue; // a kind with no fitting adopted piece stays procedural, so has no kit pair to compare
    covered.push(kind);
    expect(pairs).toBeGreaterThan(50); expect(same / pairs).toBeLessThan(0.15); // ring closures and 6 m deck lamp pairs with two pieces cannot all alternate
  }
  expect(covered).toContain('tree'); expect(covered).toContain('lamp');
});

test('landmarks: a kit piece fits the plan disc and height, or says why not', () => {
  for (const l of w.landmarks) {
    if (l.kit) { const a = by.get(l.kit.id)!, [x, y, z] = scaled(a, l.kit.scale); expect(Math.max(x, z)).toBeLessThanOrEqual(2 * plan.landmarks.find(p => `landmark/${p.id}` === l.id)!.r + 1e-2); const pl = plan.landmarks.find(q => `landmark/${q.id}` === l.id)!; expect(y).toBeLessThanOrEqual((pl.caisson && pl.top ? pl.top - pl.caisson.deck : l.height) + 1e-2); } // a caisson landmark's piece rises from the caisson deck to the plan's top
    else if (l.kits) expect(l.kits.length).toBe(plan.landmarks.find(p => `landmark/${p.id}` === l.id)!.pads!.length);
    else { expect(l.source).toBe('procedural'); expect(l.why).toBeTruthy(); }
  }
});

test('landmarks, roofs and the gateway come from the plan', () => {
  const lm = (id: string) => w.landmarks.find(l => l.id === `landmark/${id}`)!;
  expect(lm('sphere').kit).toMatchObject({ id: 'SphereStation', scale: [0.425, 0.425, 0.425] });
  expect(lm('monument').kit!.id).toBe('CircusMonument'); expect(lm('monument').radius).toBe(14); expect(lm('monument').height).toBe(30);
  expect(lm('skyport').kits!.map(k => k.id)).toEqual(['SkyPad_Roof', 'SkyPad_Roof', 'SkyPad_Roof']);
  expect(lm('skyport').kits!.map(k => k.at)).toEqual(plan.landmarks.find(l => l.id === 'skyport')!.pads!.map(p => p.c));
  const tower = w.megastructures.find(m => m.id === 'mega/skyport-tower')!;
  expect(tower.gateway).toMatchObject({ id: 'ParabolicArch', scale: [1, 1, 1], at: [482.5, 0] });
  expect(w.roofs.filter(r => r.programme === 'hover-pad').every(r => r.kit?.id === 'SkyPad_Roof' && !r.parts.some(p => p.id.endsWith('/pad')))).toBe(true);
});

test('a street lamp\'s arm points over its road (kerb normal), not world x', () => {
  const lamps = w.street.filter(s => s.kind === 'lamp' && s.edge.includes('/e') && s.yaw !== 0);
  expect(lamps.length).toBeGreaterThan(50);
  const road = (p: [number, number]) => Math.min(...d.roads.map(r => Math.min(...r.line.map(q => Math.hypot(q[0] - p[0], q[1] - p[1])))));
  const toward = lamps.filter(l => road([l.at[0] + Math.cos(l.yaw) * 3, l.at[1] + Math.sin(l.yaw) * 3]) < road([l.at[0] - Math.cos(l.yaw) * 3, l.at[1] - Math.sin(l.yaw) * 3]));
  expect(toward.length / lamps.length).toBeGreaterThan(0.95);
});

test('the references are deterministic', () => {
  expect(JSON.stringify(worldOf(plan, d, civ).world)).toBe(JSON.stringify(w));
});
