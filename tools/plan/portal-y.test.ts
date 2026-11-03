// A signature's sky rail portal sits on its line's axis (brief 075, SpaceHQ "ours": S1 runs east through the drum on y 244). portalsOf reads `portal.y`
// (default 0, the axis every earlier portal stood on); the failing example is the drum's portal taken on y 0, which left S-P1 with no pylon ground over x -54..48.
import { expect, test } from 'bun:test';
import { type Plan, portalsOf } from './plan';

const planOf = (portal: Record<string, unknown>) => ({ standards: { skyrail: {} }, signatures: [{ id: 'drum', name: 'Drum', c: [0, 244], ground: [-40, 40, 204, 284], portal: { line: 'S1', path: 'trunk', clearHalfWidth: 20, soffit: 30, openings: { at: [-40, 40], clearHalfWidth: 6, z: [21.5, 29] }, ...portal } }], megastructures: [] }) as unknown as Plan;

test('a portal with y stands on that axis: rect y ± clearHalfWidth, and its y carried for the opening corridor (P-1)', () => {
  const [p] = portalsOf(planOf({ y: 244 }));
  expect(p!.rect).toEqual([-40, 40, 224, 264]);
  expect(p!.y).toBe(244);
});

test('without y a portal stays on y 0, as before', () => {
  const [p] = portalsOf(planOf({}));
  expect(p!.rect).toEqual([-40, 40, -20, 20]);
  expect(p!.y).toBe(0);
});
