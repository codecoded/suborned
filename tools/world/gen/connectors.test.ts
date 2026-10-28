// Station banks on a north-south line (brief 075 remap: every old town station stands on one). The bank is laid in the line's own frame, so a north-south station
// gets the east-west bank turned a quarter: stair tower, two escalator runs round the mezzanine and a lift on each side, outside the platform, along the line.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { worldOf } from '../build';

setDefaultTimeout(180000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const live: Plan = read('contracts/world/harbour-district.plan.json');
// the old main loop's west half as the plan stores it (oldtown.skyrail.planned.S1): Circus Road (east-west) and The Sphere (north-south, x -218)
const plan: Plan = structuredClone(live); (plan as any).skyrail = [(live as any).oldtown.skyrail.planned.S1];
const { world: w } = worldOf(plan, derive(plan), civic(plan.civic, read(plan.civic.block)));
const bank = (st: string, side: 'l' | 'r') => w.connectors.filter(c => c.id.startsWith(`bank/S1/${st}/${side}/`));
const bbox = (r: number[][]) => [Math.min(...r.map(p => p[0]!)), Math.max(...r.map(p => p[0]!)), Math.min(...r.map(p => p[1]!)), Math.max(...r.map(p => p[1]!))];

test('a north-south station gets its banks: tower, run A, run B and lift on both sides, turned a quarter (yaw π/2), along the line', () => {
  for (const side of ['l', 'r'] as const) {
    const b = bank('sphere', side);
    expect(b.map(c => c.id.split('/').pop()).sort()).toEqual(['lift', 'run-a', 'run-b', 'tower']);
    for (const c of b) expect(c.yaw).toBeCloseTo(Math.PI / 2, 6);
    const t = bbox(bank('sphere', side).find(c => c.id.endsWith('/tower'))!.ring as number[][]);
    expect(t[3]! - t[2]!).toBeGreaterThan(t[1]! - t[0]!); // the tower's long side runs north-south, along the line
    const a = bank('sphere', side).find(c => c.id.endsWith('/run-a'))!;
    expect(Math.abs(a.foot[0] - a.head[0])).toBeLessThan(0.01); // the escalator climbs along the line (north-south)
    expect(Math.abs(a.foot[1] - a.head[1])).toBeGreaterThan(5);
  }
  // the two sides stand on opposite sides of the line (x -218)
  const xl = bbox(bank('sphere', 'l').find(c => c.id.endsWith('/tower'))!.ring as number[][]), xr = bbox(bank('sphere', 'r').find(c => c.id.endsWith('/tower'))!.ring as number[][]);
  expect(Math.sign((xl[0]! + xl[1]!) / 2 + 218)).toBe(-Math.sign((xr[0]! + xr[1]!) / 2 + 218));
});

test('an east-west station on the same line keeps its unturned bank (yaw 0)', () => {
  for (const side of ['l', 'r'] as const) { const b = bank('circus-road', side); expect(b.length).toBe(4); for (const c of b) expect(c.yaw).toBe(0); }
});
