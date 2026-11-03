// tools/world/occlusion.ts: the play-view occlusion measure against shadows worked by hand.
import { describe, expect, test } from 'bun:test';
import type { V } from '../../contracts/world/world.schema';
import { measure } from './occlusion';

const rect = (x0: number, x1: number, y0: number, y1: number): V[] => [[x0, y0], [x1, y0], [x1, y1], [x0, y1]];
const T = Math.tan((38 * Math.PI) / 180);
// a 100 x 100 m pavement at 0.18 with a 20 x 20 m block of height h in the middle
const world = (h: number, base = 0) => ({ occluders: [{ ring: rect(40, 60, 40, 60), base, top: base + h }], layers: { pavement: [{ ring: rect(0, 100, 0, 100), z: 0.18 }] }, areas: [{ id: 'west', name: 'West', ring: rect(0, 50, 0, 100) }], handover: rect(40, 60, 0, 40) });

describe('play-view occlusion', () => {
  test('no buildings: nothing hidden', () => {
    const o = measure({ occluders: [], layers: { pavement: [{ ring: rect(0, 100, 0, 100), z: 0.18 }] }, areas: [] }, 0.5);
    expect(o.layers.pavement!.district.area).toBe(10000);
    expect(o.layers.pavement!.district.hidden.mean).toBe(0);
  });

  test('yaw 0 looks north: the shadow falls north of the block, (h − z) / tan 38° deep', () => {
    const h = 16.48, o = measure(world(h), 0.25, [0]), p = o.layers.pavement!;
    expect(p.district.area).toBe(10000 - 400); // the footprint is not pavement
    const want = (20 * ((h - 0.18) / T)) / 9600, figure = (20 * ((h - 0.18 - 1.8) / T)) / 9600;
    expect(Math.abs(p.district.hidden[0]! - want)).toBeLessThan(0.004);
    expect(Math.abs(p.district.figure[0]! - figure)).toBeLessThan(0.004);
    expect(p.handover!.hidden[0]).toBe(0); // the strip south of the block faces the camera
  });

  test('yaw 180 looks south: the strip south of the block is hidden', () => {
    const p = measure(world(16.48), 0.25, [180]).layers.pavement!;
    expect(Math.abs(p.handover!.hidden[180]! - (16.3 / T) / 40)).toBeLessThan(0.01);
  });

  test('lower buildings hide less; four yaws average', () => {
    const lo = measure(world(16.48), 0.5).layers.pavement!.district.hidden.mean!, hi = measure(world(35), 0.5).layers.pavement!.district.hidden.mean!;
    expect(lo).toBeGreaterThan(0);
    expect(hi).toBeGreaterThan(lo * 1.8);
  });

  test('ground under a raised deck-like occluder counts as pavement, hidden under it', () => {
    const p = measure(world(2, 9.43), 0.5, [0]).layers.pavement!;
    expect(p.district.area).toBe(10000);
    expect(p.district.hidden[0]!).toBeGreaterThan(400 / 10000 - 0.001);
  });
});
