// tools/missions/sight.ts: the play camera's parallel rays (yaw 30, pitch 38) against extruded rings.
import { expect, test } from 'bun:test';
import type { V } from '../plan/geom';
import type { World } from '../../contracts/world/world.schema';
import { type Blocker, PLAY, blockersOf, blocks, project, seen, towardCamera } from './sight';

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
  const tree = { ...box(-1.9, -1, -3, -2, 16, false, 'dressing') }; // on the ray: from (0,0) it runs (-0.5,-0.87) per metre, so across y -2..-3 it is at x -1.15..-1.73
  const s = seen([0, 0], 0, [tree]);
  expect(s.pastArchitecture).toBe(1); expect(s.clear).toBeLessThan(1);
  const canopy: Blocker = { id: 'canopy', ring: [[-3, -3], [3, -3], [3, 0.5], [-3, 0.5]], z0: 4.05, z1: 4.62, fade: true, kind: 'architecture' };
  expect(blocks([0, 0], 0.1, canopy)).toBe(false); // the feet: the ray leaves the canopy's footprint below its soffit
});

test('project matches Latch\'s plate (office plate camera, a guard\'s feet at 17:24:30)', () => {
  const [x, y] = project([-157.1, -282], 0.18, [-159, -219.7]); // briefs/011/ui/plates/plates.json: cafe-front-172430, p-records-office-02 feet (2225, 1792.9)
  expect(x).toBeCloseTo(2225, 0); expect(y).toBeCloseTo(1792.9, 0);
});

test('a building drawn by kit pieces blocks sight as its mass, whatever the pieces are called (the café body under CafeUpper_4 was dropped, 2026-10-27)', () => {
  const ring: V[] = [[-11, -6], [11, -6], [11, 6], [-11, 6]], kit = (id: string, base: number) => ({ id, scale: [1, 1, 1] as [number, number, number], yaw: 0, at: [0, 0] as V, base });
  const w = {
    buildings: [], gates: [], megastructures: [], levels: [], street: [], transit: { stations: [], stops: [] },
    civic: [
      { id: 'civic/cafe', surface: 'limestone', ring, base: 4.88, height: 15.5, groundBy: 'civic/cafe/ground', source: 'kit', kit: kit('CafeUpper_4', 4.88), fade: true },
      { id: 'civic/cafe/ground', surface: 'trim', ring, base: 0.18, height: 4.7, source: 'kit', kit: kit('CafeBay_v9', 0.18), fade: true }, // a ground piece no rule names
      { id: 'civic/lamp', surface: 'trim', ring: [[20, 0], [20.3, 0], [20.3, 0.3], [20, 0.3]], base: 0.18, height: 4.6, source: 'kit', kit: kit('SomeNewLamp', 0.18) }, // dressing stays out
    ],
  } as unknown as World;
  const b = blockersOf(w, [-100, 100, -100, 100]);
  expect(b.map(x => `${x.id} ${x.kind} ${x.z0}-${x.z1}`).sort()).toEqual(['civic/cafe architecture 4.88-20.38', 'civic/cafe/ground architecture 0.18-4.88']);
  // a figure 10 m north of the north face: the ray clears the ground storey's 4.9 m by 6.3 m out, so only the body above it hides the figure
  const p: V = [0, 6 + 10];
  expect(seen(p, 0.18, b).pastArchitecture).toBe(0);
  expect(seen(p, 0.18, b.filter(x => x.id !== 'civic/cafe')).pastArchitecture).toBe(1);
});
