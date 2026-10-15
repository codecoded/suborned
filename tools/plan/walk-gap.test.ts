// W-5's measures (walk-gap.ts): the walkers keep the plan's body.r from a ground solid, and the walks between points pass no gap the standard does not allow. Each failing example is a
// gap or a walk made on purpose in the open ground south-west of the square, on the live plan's block; the closed gap and the allowed one sit beside them.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic, toPlanFn } from './civic';
import type { V } from './geom';
import { derive, type Plan } from './plan';
import { bodyOf } from './walk-solids';
import { bodyStd, gapWalks, walkClearance } from './walk-gap';

setDefaultTimeout(60000);
const root = resolve(import.meta.dir, '../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), block = read(plan.civic.block), live = civic(plan.civic, block), d = derive(plan), toPlan = toPlanFn(plan.civic), body = bodyStd(plan)!;

test('the plan states the body: r 0.65, 2.0 m clear, 1.5 m for 6 m where constrained, a 1.3 m aisle in seating, 1.5 m not judged at the ends; both walkers read r from it', () => {
  expect(body).toMatchObject({ r: 0.65, clearWidth: 2, constrained: { w: 1.5, run: 6 }, aisle: 1.3, ends: 1.5 });
  expect(bodyOf(plan)).toBe(0.65);
  expect(bodyOf({ standards: {} })).toBe(0.65); // a plan that states none walks with BODY
  expect(bodyOf({ standards: { walking: { body: { r: 0.4 } } } })).toBe(0.4);
  expect(bodyStd({ ...plan, standards: { ...plan.standards, walking: undefined } } as never)).toBeUndefined();
});

test('walkClearance: a walk 0.3 m from a solid fails, naming it; one 0.7 m off passes; within `ends` of either end nothing is judged', () => {
  const solid: V[] = [[0, 0], [0.5, 0], [0.5, 10], [0, 10]], civ = { ...live, solids: [solid], solidIds: ['test/wall'] };
  const walk = (x: number, y0 = -5, y1 = 15) => [{ from: 'A', to: 'B', path: [[x, y0], [x, y1]] as V[] }];
  const near = walkClearance(walk(0.8), civ, body); // 0.3 m off its east face
  expect(near).toHaveLength(1);
  expect(near[0]).toMatchObject({ walk: 'A → B', solid: 'test/wall' });
  expect(near[0]!.gap).toBeCloseTo(0.3, 2);
  expect(walkClearance(walk(1.2), civ, body)).toEqual([]); // 0.7 m off
  expect(walkClearance(walk(0.8, 4, 6), civ, body)).toEqual([]); // a walk that starts and ends within `ends` (1.5 m) of its points, all of it inside them
  expect(walkClearance(walk(0.8, 5, 14), civ, body)).toHaveLength(1); // 4 m of it is judged
});

/** Two walls across the walk between A and B, `g` m apart, `len` m deep along it; the open ground south-west of the square, block frame. */
const gap = (g: number, len: number, seating = false) => {
  const box = (cx: number, cy: number, hx: number, hy: number): V[] => ([[cx - hx, cy - hy], [cx + hx, cy - hy], [cx + hx, cy + hy], [cx - hx, cy + hy]] as V[]).map(toPlan);
  const mid = -46, solids = [box(13, mid - g / 2 - 3, len / 2, 3), box(13, mid + g / 2 + 3, len / 2, 3)], civ = { ...live, solids, solidIds: ['wall-a', 'wall-b'] };
  return gapWalks(plan, d, civ, [{ name: 'A', p: toPlan([6, -46]) }, { name: 'B', p: toPlan([20, -46]) }], seating ? [box(13, mid, 6, 6)] : [], body);
};

test('gapWalks: no solids, the walk is clear', () => {
  const r = gap(5, 3);
  expect(r).toMatchObject({ walks: 1, stranded: [], allowed: 0, bad: [] });
});

test('gapWalks: a gap of 2r or less is closed (no walk passes it); 2.2 m passes clean', () => {
  expect(gap(1.0, 3).walks).toBe(0);
  expect(gap(2.2, 8)).toMatchObject({ walks: 1, allowed: 0, bad: [] });
});

test('gapWalks: 1.8 m is allowed for 3 m of the walk (constrained: at least 1.5 m over at most 6 m), and fails for 8 m, naming the walls', () => {
  expect(gap(1.8, 3)).toMatchObject({ walks: 1, allowed: 1, bad: [] });
  const r = gap(1.8, 8);
  expect(r.allowed).toBe(0);
  expect(r.bad).toHaveLength(1);
  expect(r.bad[0]).toMatchObject({ walk: 'A → B', width: 1.8 });
  expect(r.bad[0]!.why).toMatch(/^1\.80 m for 8\.\d m of the walk, past the 6 m a constrained gap may run/);
  expect(r.bad[0]!.between).toBe('wall-b and wall-a');
});

test('gapWalks: a 1.6 m gap that fails in the open is an aisle of the standard inside a seating area', () => {
  expect(gap(1.6, 8).bad).toHaveLength(1);
  expect(gap(1.6, 8, true)).toMatchObject({ walks: 1, bad: [] });
});
