// The raster's reading of a column (brief 055): the floor a walker stands on, what a step can climb, what is a wall, what is head room.
import { expect, test } from 'bun:test';
import { BRIDGE, HEAD, Raster, STEP } from './raster';
import type { V } from '../plan/geom';
import { walkFloorAt, type WalkFile } from './walk-export';

const make = () => { const R = new Raster(0, 0, 10, 10); R.ground.fill(0.18); return R; };
const box = (x0: number, y0: number, x1: number, y1: number): V[] => [[x0, y0], [x1, y0], [x1, y1], [x0, y1]];

test('a stair of thin treads with open risers is a floor, and the landing is where the treads end', () => {
  const R = make();
  R.prism(box(2, 2, 6, 8), 0.33, 0.43, 'tread-1'); R.prism(box(3, 2, 6, 8), 0.58, 0.68, 'tread-2'); R.prism(box(4, 2, 6, 8), 0.83, 0.93, 'landing'); // the treads run on under the landing, a plate to each step, as the Records Entrance is modelled
  R.finish();
  const [i, j] = R.cellOf([5, 5]), c = R.column(i, j);
  expect(c.top).toBeCloseTo(0.93, 2); expect(c.owner).toBe('landing'); expect(c.walkable).toBe(true);
  expect(R.column(...R.cellOf([2.5, 5])).top).toBeCloseTo(0.43, 2);
});

test('a bed or a bench more than a step high is a wall to a walker, a kerb stone is not', () => {
  const R = make();
  R.prism(box(2, 2, 4, 4), 0.18, 0.7, 'bed'); R.prism(box(6, 2, 8, 4), 0.18, 0.36, 'kerb');
  R.finish();
  expect(R.column(...R.cellOf([3, 3])).walkable).toBe(false);
  expect(R.column(...R.cellOf([7, 3])).walkable).toBe(true);
  expect(0.7 - 0.18).toBeGreaterThan(STEP);
});

test('head room: an awning at 1.5 m is no floor and no way through, one at 2.6 m is', () => {
  const R = make();
  R.prism(box(2, 2, 4, 4), 1.68, 1.78, 'low'); R.prism(box(6, 2, 8, 4), 2.78, 2.88, 'high');
  R.finish();
  const low = R.column(...R.cellOf([3, 3])), high = R.column(...R.cellOf([7, 3]));
  expect(low.walkable).toBe(false); expect(low.roof - low.top).toBeLessThan(HEAD);
  expect(high.walkable).toBe(true); expect(high.roof - high.top).toBeGreaterThan(HEAD);
});

test('a floor over nothing for more than a bridge is a table top, not a floor', () => {
  const R = make();
  R.prism(box(2, 2, 4, 4), 0.18 + BRIDGE + 0.1, 0.18 + BRIDGE + 0.15, 'table');
  R.finish();
  const c = R.column(...R.cellOf([3, 3]));
  expect(c.top).toBeCloseTo(0.18, 2);
});

test('a platform is reached only by steps: a plateau with a sheer face is not walkable', () => {
  const R = make();
  R.prism(box(4, 2, 8, 8), 0.18, 0.93, 'terrace');
  R.finish();
  expect(R.column(...R.cellOf([6, 5])).walkable).toBe(false);
  const R2 = make();
  R2.prism(box(4.5, 2, 8, 8), 0.18, 0.93, 'terrace'); R2.prism(box(3.5, 2, 4, 8), 0.18, 0.43, 'step-a'); R2.prism(box(3, 2, 3.5, 8), 0.18, 0.28, 'step-0'); R2.prism(box(4, 2, 4.5, 8), 0.18, 0.68, 'step-b');
  R2.finish();
  expect(R2.column(...R2.cellOf([6, 5])).walkable).toBe(true);
});

test('the walk file reads back the floor it was written with', () => {
  const nx = 4, ny = 4, floor = new Int16Array(nx * ny); floor[1 * ny + 2] = 93; floor[0] = 18;
  const f = { schema: 'suborned.walk/1', plan: 'p', world: 'w', window: { x0: -2, y0: -2, x1: -1, y1: -1 }, cell: 0.25, nx, ny, frame: { origin: [0, 0], x: [0, -1], y: [1, 0] }, floor: Buffer.from(floor.buffer).toString('base64'), kind: '', counts: {} } as const;
  const at = walkFloorAt(f as unknown as WalkFile);
  expect(at([-1.7, -1.4])).toBeCloseTo(0.93, 3); // i = 1, j = 2
  expect(at([-1.9, -1.9])).toBeCloseTo(0.18, 3);
  expect(at([5, 5])).toBe(0);
});
