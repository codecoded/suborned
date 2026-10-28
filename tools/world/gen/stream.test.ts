// Brief 075-E: the streamline grammar's data and frame (stream.ts). The recipe is the old street's (briefs/075/composition.json); the corner and end frames
// put each piece's footprint where the old pieces stand (the cell behind both faces at the vertex; an end's front from the vertex along the run).
import { expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { KitAsset } from '../../../contracts/world/kit.schema';
import { KIT_X, RECIPE, STREAM_ROWS, cornerPlace, endPlace, recipePick } from './stream';

const root = resolve(import.meta.dir, '../../..');
const comp = JSON.parse(readFileSync(resolve(root, 'briefs/075/composition.json'), 'utf8'));
const kit = JSON.parse(readFileSync(resolve(root, 'contracts/world/kit.json'), 'utf8')) as { assets: KitAsset[] }, by = new Map(kit.assets.map(a => [a.id, a]));

test('the recipe is the old street\'s: buildings per class family, the civic hall left out', () => {
  const b = comp.recipe.buildings as Record<string, number>, n = b.S! + b.P! + b.B! + b.D! + b.A!;
  for (const c of ['S', 'P', 'B', 'D', 'A'] as const) expect(RECIPE[c]).toBeCloseTo(b[c]! / n, 9);
  expect(RECIPE.S).toBeGreaterThan(0.5); // streamline the majority
  // the draw follows the weights: over many seeds a class's share is near its weight
  const items = ['S', 'P', 'B', 'D', 'A'] as const, hits: Record<string, number> = {};
  for (let i = 0; i < 10000; i++) { const c = recipePick([...items], x => x, (i + 0.5) / 10000); hits[c] = (hits[c] ?? 0) + 1; }
  for (const c of items) expect(hits[c]! / 10000).toBeCloseTo(RECIPE[c], 2);
});

test('every row of the class table names catalogue pieces', () => {
  for (const r of STREAM_ROWS) for (const id of [r.bay, r.end, r.corner]) expect(by.has(id), id).toBe(true);
});

test('a corner\'s footprint is the cell behind both faces at the vertex (plus its canopy out of both); an end\'s front runs from the vertex along the run', () => {
  expect(KIT_X).toBe(-1);
  // a square outside corner at the origin: incoming face along +x (normal -y), leaving face along +y (normal +x)
  const v: [number, number] = [0, 0], n0: [number, number] = [0, -1], n1: [number, number] = [1, 0];
  for (const id of ['StreamCorner_4', 'StreamCornerStack_Base', 'DecoCorner_10']) {
    const a = by.get(id)!, cp = cornerPlace(v, n0, n1, a, 1);
    // the footprint box (size about `at`) reaches the canopy past both faces and 8 m (the cell) back from the vertex along both
    expect(cp.at[0] + a.size[0] / 2, id).toBeCloseTo(a.size[0] - 8, 1); // canopy out of the leaving face (+x)
    expect(cp.at[0] - a.size[0] / 2, id).toBeCloseTo(-8, 1);
    expect(cp.at[1] - a.size[2] / 2, id).toBeCloseTo(-(a.size[2] - 8), 1); // canopy out of the incoming face (-y)
    expect(cp.at[1] + a.size[2] / 2, id).toBeCloseTo(8, 1);
    expect(cp.onFace, id).toBeCloseTo(8, 1);
    expect(cp.yaw).toBeCloseTo(-Math.PI / 2, 4); // fronting the leaving face (+x normal): yaw = atan2(n1) - 90°
  }
  // an end at the tail of an edge along +x (normal -y): its 6 m front from x 0 to 6, its return (and canopy) past the vertex at x < 0, its body 14 m behind (+y)
  const a = by.get('StreamEnd_4')!, ep = endPlace([0, 0], [1, 0], [0, -1], a, 1);
  expect(ep.front).toBe(6);
  expect(ep.at[0] + a.size[0] / 2).toBeCloseTo(6, 1);
  expect(ep.at[0] - a.size[0] / 2).toBeCloseTo(-(a.size[0] - 6), 1);
  expect(ep.at[1] + a.size[2] / 2).toBeCloseTo(14, 1);
});
