// R-F1's square clause (brief 075 ask 3, standards.roads.frontageSquare, opt-in): a built plot may front an open square instead of an access road when the square
// fronts an access road and every point of the plot is within reach (45 m, Approved Document B B5) of that road's kerb. Each failing example is the committed
// plan with test plots added (R-F1 reads frontage only; R-O1 judges their overlaps separately).
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic } from './civic';
import { check } from './checks';
import { derive, type Plan } from './plan';

const root = resolve(import.meta.dir, '../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), block = read(plan.civic.block);
const rf1 = (mutate: (p: any) => void) => { const p: any = structuredClone(plan); mutate(p); return check(p, derive(p), civic(p.civic, block)).checks.find(c => c.id === 'R-F1')!; };
const REACH = { reach: 45, source: 'Approved Document B B5' };
// a square on Sunset Way east's south kerb (y -258) and a host plot behind it (both on n2's ground: R-F1 does not read overlaps)
const add = (p: any, host: [number, number, number, number], use = 'square') => p.plots.push({ id: 'test-square', use, area: 'southside', rect: [12, 44, -290, -258] }, { id: 'test-host', use: 'mixed', area: 'southside', rect: host });

describe('R-F1 square frontage', () => {
  test('the committed plan passes with and without the clause', () => {
    expect(rf1(() => {}).ok).toBe(true);
    expect(rf1(p => { p.standards.roads.frontageSquare = REACH; }).ok).toBe(true);
  });
  test('a plot behind a square on an access road, all within 45 m of its kerb, passes only with the clause', () => {
    expect(rf1(p => add(p, [12, 44, -300, -290])).detail).toMatch(/test-host/);
    const ok = rf1(p => { p.standards.roads.frontageSquare = REACH; add(p, [12, 44, -300, -290]); });
    expect([ok.ok, ok.detail]).toEqual([true, undefined]);
  });
  test('failing: a plot reaching 62 m from the kerb, a park instead of a square, and old E1 behind Orbit Square East (which fronts only the Boulevard)', () => {
    expect(rf1(p => { p.standards.roads.frontageSquare = REACH; add(p, [12, 44, -320, -290]); }).detail).toMatch(/test-host/);
    expect(rf1(p => { p.standards.roads.frontageSquare = REACH; add(p, [12, 44, -300, -290], 'park'); }).detail).toMatch(/test-host/);
    expect(rf1(p => { p.standards.roads.frontageSquare = REACH; p.plots.push({ id: 'square-east', use: 'mixed', area: 'southside', rect: [48, 74, -356, -322] }); }).detail).toBe('square-east');
  });
});
