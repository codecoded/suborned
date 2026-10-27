// K-6, no facade piece leaves its building (brief 072; Keel's lamplight-south-w/13/k6). The live export passes; each failing example puts one fault back.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Facade, World } from '../../../contracts/world/world.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { ctxOf, generated } from '../../plan/world-ctx';
import { cotAcute } from '../gen/faces';
import { adopted } from '../gen/kitrefs';
import { containFaults, containRules } from './contain';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block)), base = generated(plan, d, civ);
const by = new Map(adopted().map(a => [a.id, a])), cat = (id: string) => by.get(id);
const faults = (w: World) => containFaults(w, cat, 0.18).faults;
const k6 = (w: World) => containRules(ctxOf(plan, d, civ, { w, gen: base.gen }))[0]!;
const one = (w: World, f: Facade) => ({ ...w, facades: [f] }) as World;

test('the live export: every facade piece stands inside its building (K-6 passes, 0 faults at any height)', () => {
  const r = k6(base.w);
  expect(r.ok, r.detail).toBe(true);
  expect(faults(base.w)).toEqual([]);
});

test('cot of the interior angle: 0 at a square or obtuse corner, 1 at 45°, tan(10°) at 80°', () => {
  expect(cotAcute([1, 0], [0, 1])).toBe(0); // a 90° left turn (square)
  expect(cotAcute([1, 0], [1, 1].map(x => x / Math.SQRT2) as [number, number])).toBe(0); // 45° turn: 135° inside
  expect(cotAcute([1, 0], [-1, 1].map(x => x / Math.SQRT2) as [number, number])).toBeCloseTo(1, 6); // 135° turn: 45° inside
  expect(cotAcute([1, 0], [-Math.sin((10 * Math.PI) / 180), Math.cos((10 * Math.PI) / 180)])).toBeCloseTo(Math.tan((10 * Math.PI) / 180), 6);
});

test('the shop filler at lamplight-south-w/13 now stops 2.15 m short of its 45° corner and a pilaster takes the corner', () => {
  const ground = base.w.facades.filter(f => f.building === 'bld/lamplight-south-w/13' && f.face === 'bld/lamplight-south-w/13/body#0' && f.base < 1);
  const shops = ground.filter(f => f.kit.id === 'ShopFill_4'), last = shops.reduce((m, f) => (Math.max(f.a[0], f.b[0]) > Math.max(m.a[0], m.b[0]) ? f : m));
  expect(-164 - Math.max(last.a[0], last.b[0])).toBeGreaterThan(2.15 - 0.01); // the vertex is (-164, -185.5)
  expect(ground.some(f => f.form === 'pilaster' && Math.max(f.a[0], f.b[0]) > Math.max(last.a[0], last.b[0]))).toBe(true);
});

test('failing example: that ShopFill_4 put back at its corner (v3 export, a→b ending on the vertex) stands 2.15 m out', () => {
  const host = base.w.facades.find(f => f.building === 'bld/lamplight-south-w/13' && f.kit.id === 'ShopFill_4')!;
  const bad: Facade = { ...host, id: 'bld/lamplight-south-w/13/k6', kit: { id: 'ShopFill_4', scale: [1.081, 1.036, 1], yaw: -3.1416, at: [-165.621, -185.24] }, base: 0.18, a: [-167.242, -185.5], b: [-164, -185.5], normal: [0, -1] };
  const w = one(base.w, bad), f = faults(w);
  expect(f).toHaveLength(1);
  expect(f[0]!.out).toBeGreaterThan(1.5);
  expect(f[0]!.ground).toBe(true);
  const r = k6(w);
  expect(r.ok).toBe(false);
  expect(r.detail).toContain('lamplight-south-w/13/k6 ShopFill_4');
});

test('failing example: a SideBayStack_Mid run to the 80° corner of parade-north/2 (no pull-back) stands its back 0.06 m out', () => {
  // body ring [[136,40],[136,64],[116,67.304],[116,60]]: the west face meets the slanted north face at 80.6°; the module ran to the vertex in the v3 export
  const host = base.w.facades.find(f => f.building === 'bld/parade-north/2' && f.kit.id === 'SideBayStack_Mid')!;
  const bad: Facade = { ...host, id: 'bld/parade-north/2/k5', kit: { id: 'SideBayStack_Mid', scale: [1, 1, 1], yaw: -4.7124, at: [116, 64.304] }, base: 5.83, a: [116, 67.304], b: [116, 61.304], normal: [-1, 0] };
  const f = faults(one(base.w, bad));
  expect(f).toHaveLength(1);
  expect(f[0]!.out).toBeGreaterThan(0.05);
  expect(f[0]!.ground).toBe(false);
});

test('failing example: a street bay pushed 0.5 m deeper than its plot leaves through the back; its front canopy, proud of its own wall, never counts', () => {
  const bay = base.w.facades.find(f => f.form === 'bay' && f.building === 'bld/lamplight-south-w/13')!;
  expect(faults(one(base.w, bay))).toEqual([]);
  const n = bay.normal ?? [0, -1], deeper: Facade = { ...bay, kit: { ...bay.kit, at: [bay.kit.at[0] - n[0] * 0.5, bay.kit.at[1] - n[1] * 0.5] }, a: [bay.a[0] - n[0] * 0.5, bay.a[1] - n[1] * 0.5], b: [bay.b[0] - n[0] * 0.5, bay.b[1] - n[1] * 0.5] };
  // moving the wall line too: the whole piece shifts into the building, so its back (14 m × sz deep) passes the far wall (16 m deep plot)
  const f = faults(one(base.w, { ...deeper, kit: { ...deeper.kit, scale: [deeper.kit.scale[0], deeper.kit.scale[1], 1.2] } }));
  expect(f).toHaveLength(1);
  expect(f[0]!.out).toBeGreaterThan(0.05);
});
