// R-O1 on L-shaped plots: plots that only share an edge do not overlap; plots that do, do. The inset points must lie in the true interior.
import { expect, test } from 'bun:test';
import { inset } from './checks';
import { inPoly, type V } from './geom';

const L: V[] = [[0, 0], [20, 0], [20, 10], [10, 10], [10, 20], [0, 20]]; // L, notch at the top right
const fills: V[] = [[10, 10], [20, 10], [20, 20], [10, 20]]; // fills the notch, sharing two edges with L
const overlaps: V[] = [[8, 8], [14, 8], [14, 14], [8, 14]]; // pokes 2 m into L
const hit = (a: V[], b: V[]) => inset(a).some(v => inPoly(v, b)) || inset(b).some(v => inPoly(v, a));
const shaped = (ring: V[]) => [ring, [...ring].reverse()];

test('inset points lie inside the polygon, either winding', () => {
  for (const r of shaped(L)) for (const v of inset(r)) expect(inPoly(v, r)).toBe(true);
});
test('an L and the rectangle that fills its notch share edges, not area', () => {
  for (const a of shaped(L)) for (const b of shaped(fills)) expect(hit(a, b)).toBe(false);
});
test('an L and a rectangle poking into it overlap', () => {
  for (const a of shaped(L)) for (const b of shaped(overlaps)) expect(hit(a, b)).toBe(true);
});
