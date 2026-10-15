// tools/missions/sight.ts: the play camera's parallel rays (yaw 30, pitch 38) against extruded rings.
import { expect, test } from 'bun:test';
import type { V } from '../plan/geom';
import { type Blocker, PLAY, blocks, project, seen, towardCamera } from './sight';

const box = (x0: number, x1: number, y0: number, y1: number, z1: number, fade = true, kind: Blocker['kind'] = 'architecture'): Blocker => ({ id: `box ${x0},${y0}`, ring: [[x0, y0], [x1, y0], [x1, y1], [x0, y1]] as V[], z0: 0, z1, fade, kind });

test('the ray runs toward compass 210 and climbs tan 38°', () => {
  const { h, rise } = towardCamera(PLAY);
  expect(h[0]).toBeCloseTo(-0.5, 6); expect(h[1]).toBeCloseTo(-Math.sqrt(3) / 2, 6); expect(rise).toBeCloseTo(0.7813, 4);
});

test('a 38 m tower hides the door on its far face; the door on its near face is seen', () => {
  const tower = box(-3, 3, -3, 3, 38);
  expect(seen([0, 3.75], 0.75, [tower]).pastArchitecture).toBe(0); // the north face, as the Records Office's public door
  expect(seen([0, 3.75], 0.75, [tower]).onlyFade).toBe(1); // a ghost through the fade, no more
  expect(seen([0, -3.75], 0, [tower]).pastArchitecture).toBe(1); // the south face, as the official door
});

test('the hidden band behind a mass ends where the ray clears its top', () => {
  const wall = box(-50, 50, 0, 1, 15.5); // a 15.5 m wing: the ray clears it 15.5 / 0.78 = 19.8 m of travel, 17.2 m north of its face
  expect(blocks([0, 1 + 17], 0.1, wall)).toBe(true);
  expect(blocks([0, 1 + 17.5], 0.1, wall)).toBe(false);
});

test('dressing is reported but does not count against architecture; a point under an overhead slab is cut above its soffit only', () => {
  const tree = { ...box(-0.5, 0.5, -3, -2, 16, false, 'dressing') };
  const s = seen([0, 0], 0, [tree]);
  expect(s.pastArchitecture).toBe(1); expect(s.clear).toBeLessThan(1);
  const canopy: Blocker = { id: 'canopy', ring: [[-3, -3], [3, -3], [3, 0.5], [-3, 0.5]], z0: 4.05, z1: 4.62, fade: true, kind: 'architecture' };
  expect(blocks([0, 0], 0.1, canopy)).toBe(false); // the feet: the ray leaves the canopy's footprint below its soffit
});

test('project matches Latch\'s plate (office plate camera, a guard\'s feet at 17:24:30)', () => {
  const [x, y] = project([-157.1, -282], 0.18, [-159, -219.7]); // briefs/011/ui/plates/plates.json: cafe-front-172430, p-records-office-02 feet (2225, 1792.9)
  expect(x).toBeCloseTo(2225, 0); expect(y).toBeCloseTo(1792.9, 0);
});
