// Brief 072-C (Aster's ruling 2): an own shopfront box stops 2 cm under the fascia foot of the every-face piece behind it.
import { expect, test } from 'bun:test';
import type { Facade, Frontage, Prim } from '../../../contracts/world/world.schema';
import { FASCIA_CLEAR, fitShopfronts } from './shopfit';

const prim = (id: string, base: number, h: number): Prim => ({ id, shape: 'box', surface: 'frontage', c: [3, 0.3], base, size: [6, 0.6, h], yaw: 0 });
const tenancy = (id: string, extra: Partial<Frontage> = {}): Frontage => ({
  id, host: 'bld/x/0', level: 'street', z: 0.18, trade: 'diner', area: 'harbour-east', street: 's', a: [0, 0], b: [6, 0], w: 6, normal: [0, 1], yaw: 0,
  hours: [8, 20], staffed: true, spill: { kind: 'none', depth: 0 }, lobby: false, anchor: false,
  front: prim(`${id}/front`, 0.18, 4.6), entrance: prim(`${id}/door`, 0.18, 2.4), canopy: { ...prim(`${id}/canopy`, 3.98, 0.25), surface: 'awning' },
  sign: { ...prim(`${id}/sign`, 5.08, 1.2), motif: 'm', icon: 'i', palette: 'p' }, ...extra,
});
const piece = (id: string, kit: string, sy: number, a: [number, number], b: [number, number], form: Facade['form'] = 'side'): Facade =>
  ({ id, building: 'bld/x/0', form, storeys: 3, kit: { id: kit, scale: [1, sy, 1], yaw: 0, at: [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2] }, base: 0.18, a, b, edge: 0 } as Facade);

test('the box top sits 2 cm under a SideBay_3 fascia foot (KIT 4.0 x sy), the canopy moves down with it', () => {
  const { frontages: [t], log } = fitShopfronts([tenancy('t')], [piece('p', 'SideBay_3', 0.956, [0, 0], [6, 0])]);
  const top = t!.front.base + t!.front.size[2];
  expect(top).toBeCloseTo(0.18 + 4.0 * 0.956 - FASCIA_CLEAR, 3);
  expect(t!.canopy.base + 0.8).toBeCloseTo(top, 3);
  expect(log.clamped).toBe(1);
});

test('a PLAN base module (fascia foot 4.65 x sy) above the box leaves it alone; a pier leg reaching the tenancy clamps it', () => {
  expect(fitShopfronts([tenancy('t')], [piece('p', 'SideBayStack_Base', 1, [0, 0], [6, 0], 'stack')]).log.clamped).toBe(0);
  const { frontages: [t] } = fitShopfronts([tenancy('t')], [piece('k', 'CornerPier_Out_3', 0.92, [6.5, 0], [6.5, 0], 'pier')]);
  expect(t!.front.base + t!.front.size[2]).toBeCloseTo(0.18 + 4.0 * 0.92 - FASCIA_CLEAR, 3);
});

test('linked tenancies, upper modules and pieces on another line are ignored; a clamp under 3.2 m is logged, never forced', () => {
  const linked = tenancy('l', { facade: 'street-piece' });
  const fs = [piece('m', 'SideBayStack_Mid', 1, [0, 0], [6, 0], 'stack'), piece('o', 'SideBay_3', 0.8, [0, 5], [6, 5])];
  expect(fitShopfronts([linked, tenancy('t')], fs).log.clamped).toBe(0);
  const { frontages: [t], log } = fitShopfronts([tenancy('t')], [piece('p', 'SideBay_3', 0.6, [0, 0], [6, 0])]);
  expect(t!.front.size[2]).toBe(4.6);
  expect(log.low).toEqual([{ id: 't', piece: 'SideBay_3', height: 2.38 }]);
});
