// A road's own pavement (brief 075 ask 1, roads[].pavement; tools/plan/sections.ts) and R-PV1. `bare` is the committed plan without any road's own pavement
// (the plan has carried two since 075's plan-match, Sunset Way east and Harbour Market: re-pinned 2026-11-07, brief 098), so every reader takes the class
// section exactly; with Sunset Way east at the old 3.5 m flags the building line, the street zones and the spill follow that road only; each failing example
// is the bare plan with one thing wrong.
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic } from './civic';
import { check } from './checks';
import { derive, type Plan } from './plan';
import { sectionOf } from './sections';
import { roadAt } from './street-zones';
import { ctxOf } from '../world/gen/common';
import { buildingLine, pavementOf, plotRing } from '../world/gen/edges';

const root = resolve(import.meta.dir, '../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), block = read(plan.civic.block);
const bare: Plan = (() => { const p: any = structuredClone(plan); for (const r of p.roads) delete r.pavement; return p as Plan; })();
const OLD = { w: 3.5, kerb: 1.5, walk: 2, frontage: 0, why: 'the old side-street flags (DecoKitStreetLayout)' };
const withPv = (pv: object = OLD, id = 'sunset-east') => { const p: any = structuredClone(bare); p.roads.find((r: any) => r.id === id).pavement = pv; return p as Plan; };
const pv1 = (p: Plan) => check(p, derive(p), civic(p.civic, block)).checks.find(c => c.id === 'R-PV1');
const ctx = (p: Plan) => { const d = derive(p); return ctxOf(p, d, civic(p.civic, block), 0.18); };
const S = (plan.standards as any).streetscape.sections;

describe('roads[].pavement', () => {
  test('a plan without it reads the class sections exactly and has no R-PV1', () => {
    expect(sectionOf(S, bare, 'access', 'sunset-east')).toBe(S.access);
    expect(pv1(bare)).toBeUndefined();
  });
  test('the committed plan\'s own pavements (Sunset Way east 3.25, Harbour Market 2.75) hold R-PV1', () => {
    expect(pv1(plan)?.ok).toBe(true);
    expect([pavementOf(ctx(plan), 'access:sunset-east'), pavementOf(ctx(plan), 'access:harbour-market')]).toEqual([3.25, 2.75]);
  });
  test('Sunset Way east at 3.5 m: its section, building line and zones follow; the other access roads keep 6.5', () => {
    const p = withPv(), c = ctx(p);
    expect(pv1(p)?.ok).toBe(true);
    expect(pavementOf(c, 'access:sunset-east')).toBe(3.5);
    expect(pavementOf(c, 'access:harbour-market')).toBe(6.5);
    expect(pavementOf(ctx(bare), 'access:sunset-east')).toBe(6.5);
    // n2's north edge faces Sunset Way east (kerb y -258): its building line steps from y -264.5 to -261.5
    const top = (cc: ReturnType<typeof ctx>) => { const n2 = cc.d.plots.find(x => x.id === 'n2')!; return Math.max(...buildingLine(cc, plotRing(cc, n2))!.p.map(v => v[1])); };
    expect(top(ctx(bare))).toBeCloseTo(-264.5, 3);
    expect(top(c)).toBeCloseTo(-261.5, 3);
    const at = roadAt(c, [30, -260], 'n2/e2');
    expect(at?.road).toBe('sunset-east');
    expect([at?.sec.pavement, at?.sec.kerb, at?.sec.walk, at?.sec.frontage]).toEqual([3.5, 1.5, 2, 0]);
  });
  test('R-PV1 failing examples: zones that do not sum, a walk under the class\'s, a kerb zone under 0.75, no why, and a Handover street', () => {
    expect(pv1(withPv({ ...OLD, frontage: 1 }))?.detail).toMatch(/sunset-east: kerb 1.5 \+ walk 2 \+ frontage 1 ≠ w 3.5/);
    expect(pv1(withPv({ ...OLD, w: 3, walk: 1.5 }))?.detail).toMatch(/walk 1.5 m is narrower than the access section's clear walk 2 m/);
    expect(pv1(withPv({ ...OLD, w: 2.5, kerb: 0.5 }))?.detail).toMatch(/kerb zone 0.5 m/);
    expect(pv1(withPv({ ...OLD, why: '' }))?.detail).toMatch(/no why/);
    expect(pv1(withPv(OLD, 'orbit-walk'))?.detail).toMatch(/orbit-walk: enters the Handover zone/);
  });
});
