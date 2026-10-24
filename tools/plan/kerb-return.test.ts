// KR-1 (brief 028): the committed plan passes; each failing example is the committed plan with one thing changed.
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic } from './civic';
import { derive, type Plan } from './plan';
import { kerbReturnChecks } from './kerb-return';

const root = resolve(import.meta.dir, '../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), block = read(plan.civic.block);
const run = (mutate: (p: any) => void = () => {}) => { const p: Plan = structuredClone(plan); mutate(p); return kerbReturnChecks(p, derive(p), civic(p.civic, block)); };

describe('KR-1', () => {
  test('the committed plan passes: 153 returns (76 junction corners, 1 concave, 8 lay-by arcs, 6 circus mouths, 12 bend outsides, 50 median ends), the radii Datum set, the pavement holds its walk at every square building corner', () => {
    const c = run(); expect(c.length).toBe(1); expect(c[0]!.id).toBe('KR-1'); expect(c[0]!.ok).toBe(true); expect(c[0]!.value).toBe('153 returns');
  });
  test('a plan without the key has no check (a pinned plan replays square)', () => { expect(run(p => { delete p.standards.streetscape.kerbReturn; })).toEqual([]); });
  test('a district return of 12 m takes the walk away at the building corner: it fails and names the corner', () => {
    const c = run(p => { p.standards.streetscape.kerbReturn.byClass.district = 12; })[0]!;
    expect(c.ok).toBe(false); expect(c.detail).toMatch(/m of pavement on the corner's bisector, the sections need \d(\.\d)? m \(kerb \+ walk\)/);
  });
  test('a missing class radius, a zero concave fillet and a bay radius under 0.3 m fail', () => {
    expect(run(p => { delete p.standards.streetscape.kerbReturn.byClass.access; })[0]!.detail).toMatch(/byClass\.access/);
    expect(run(p => { p.standards.streetscape.kerbReturn.concave = 0; })[0]!.detail).toMatch(/concave is 0/);
    expect(run(p => { p.standards.streetscape.kerbReturn.bay = 0.1; })[0]!.detail).toMatch(/bay is 0\.1/);
  });
  test('a return too big for its edge is shrunk by the generator, and KR-1 says so: a civic radius of 40 m', () => {
    const c = run(p => { p.standards.streetscape.kerbReturn.civic = 40; })[0]!;
    expect(c.ok).toBe(false); expect(c.detail).toMatch(/radius \d+\.\d\d m, the plan asks 40 m \(an edge is too short/);
  });
});
