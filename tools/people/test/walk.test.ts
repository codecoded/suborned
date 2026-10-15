// The people's walk graph reads the block's ground solids with the plan's body clearance (W-5; Splice 2026-10-15), planter runs, pots, bollards and lamp bases included (Vantage's finding): the walk goes
// round a planter run, never through it, and no place a person can stand is inside one.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic, toPlanFn } from '../../plan/civic';
import type { V } from '../../plan/geom';
import { derive, type Plan } from '../../plan/plan';
import { walker } from '../walk';

setDefaultTimeout(60000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), block = read(plan.civic.block), toPlan = toPlanFn(plan.civic), d = derive(plan);
const live = civic(plan.civic, block), bare = civic({ ...plan.civic, groundPieces: undefined }, block); // the block as civic.ts read it before: no planter runs, pots, bollards or lamp bases
// run-sq-south/0 and /1 (block frame, x 20.5, y -46.2 .. -37.8 together); two places 3 m apart, one each side of them (block x 19, x 22)
const west = toPlan([18.9, -42]), east = toPlan([22.1, -42]), speed = 1.4;

test('a planter run is not walked through: the places either side of run-sq-south are 3.2 m apart on the open ground and 9 m or more apart round the two modules', () => {
  const pts: V[] = [west, east], w1 = walker(plan, d, live, pts, speed), w0 = walker(plan, d, bare, pts, speed);
  expect(w0.m(west, east)).toBeLessThan(4.5); // without the planters: a step across
  expect(w1.m(west, east)).toBeGreaterThan(9); // with them: round the 8.4 m run, a body's width off its ends
});

test('no one stands inside a planter run, a pot, a bollard or a lamp base: those places are not free ground', () => {
  const w = walker(plan, d, live, [west, east], speed);
  for (const id of ['run-sq-south/0', 'run-sq-west/0', 'run-east-cafe/1', 'top-office-w', 'bol-cross-1', 'lamp-fore-w']) {
    const poly = live.solids![live.solidIds!.indexOf(id)]!, c: V = [poly.reduce((t, p) => t + p[0], 0) / poly.length, poly.reduce((t, p) => t + p[1], 0) / poly.length];
    expect(w.free(c), id).toBe(false);
  }
  const o = walker(plan, d, bare, [west, east], speed), poly = live.solids![live.solidIds!.indexOf('run-sq-south/0')]!;
  expect(o.free([poly.reduce((t, p) => t + p[0], 0) / 4, poly.reduce((t, p) => t + p[1], 0) / 4])).toBe(true); // the block as it was left the module as open ground
});

test('standing points keep a body\'s clearance from every ground solid, planters, pots, bollards and lamp bases included: near each door the first points out are inside none and 0.65 m or more from all', async () => {
  const { standingPoints } = await import('../seats');
  const { segDist, inPoly } = await import('../../plan/geom');
  const solids = live.solids!;
  for (const door of live.entrances) {
    const pts = standingPoints(live, door.p, [], () => true, () => true);
    expect(pts.length, door.name).toBeGreaterThan(0);
    for (const p of pts) {
      expect(solids.some(s => inPoly(p, s)), `${door.name} ${p}`).toBe(false);
      expect(Math.min(...solids.flatMap(s => s.map((a, i) => segDist(p, a, s[(i + 1) % s.length]!)))), `${door.name} ${p}`).toBeGreaterThanOrEqual(0.65 - 1e-9);
    }
  }
});
