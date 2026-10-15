// SF-22 and the crossing coverage rules (plan crossings.coverage.rules; Datum's proposal, Sky's approval 2026-10-15; written by Splice): the generator places the rules' sites by geometry, drops twins across a
// side road's mouth or round a bend, moves the furniture in the way and says so in the export's crossingRules; SF-22 holds the export to it. Each failing example puts one thing back.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { World } from '../../../contracts/world/world.schema';
import { civic } from '../../plan/civic';
import type { V } from '../../plan/geom';
import { derive, type Plan } from '../../plan/plan';
import { ctxOf, generated } from '../../plan/world-ctx';
import { extraRules } from './index';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block)), base = generated(plan, d, civ);
type W = World;
const run = (mutate: (w: W) => void = () => {}, p: Plan = plan) => {
  const w = structuredClone(base.w) as W; mutate(w);
  return extraRules(ctxOf(p, p === plan ? d : derive(p), civ, { w, gen: base.gen })).find(c => c.id === 'SF-22');
};
const isSite = (c: NonNullable<W['crossings']>[number]) => c.landings.some(l => /^street\/(arm|island|mall|mid)\//.test(l.id));
const mid = (c: { line: [V, V] }): V => [(c.line[0][0] + c.line[1][0]) / 2, (c.line[0][1] + c.line[1][1]) / 2];

test('the rules the plan states: arms 8 m off the junction, the circus island, the mall, a mid-block crossing in any district gap over 120 m; twins within 30 m of a mouth or 20 m of a bend', () => {
  const r = plan.standards.streetscape.crossings.coverage.rules;
  expect(r).toMatchObject({ reach: 4, arms: { offset: 8 }, island: { ring: 'crown-circus' }, midBlock: { gap: 120, class: ['district'], reach: 15 }, dedupe: { mouth: 30, bend: 20, turn: 45 } });
});

test('the live export: 25 rule sites (12 arms, 8 island, 2 mall, 3 mid-block), 11 twins dropped, 6 pieces of furniture moved, 1 site left out, and SF-22 passes', () => {
  const log = base.w.crossingRules!;
  expect(log.sites).toEqual({ arm: 12, island: 8, mall: 2, mid: 3 });
  expect(log.twins).toHaveLength(11);
  expect(log.twins.every(t => /: a twin (across a side road's mouth|round a bend) \(\d+ m from the crossing it keeps\)$/.test(t))).toBe(true);
  expect(log.moved).toHaveLength(6);
  expect(log.left).toHaveLength(1);
  expect(log.left[0]).toMatch(/^arm\/crown-road\/nw\/after: no free spot for the landing at kerb b/);
  expect(base.w.crossings!.filter(isSite)).toHaveLength(25);
  const r = run()!;
  expect(r.ok, r.detail).toBe(true);
});

test('furniture moved for a site is movable furniture and is no longer in the street; a bench, a bin and a tree are, a lamp post of the civic block is not', () => {
  const ids = new Set(base.w.street.map(s => s.id));
  for (const m of base.w.crossingRules!.moved) { expect(m).toMatch(/^street\/.*\/(tree|lamp|bench|bin|hydrant|phone-booth|mailbox|tub-palm|planter|advertising-column)-\d+$/); expect(ids.has(m), m).toBe(false); }
});

test('a site with no crossing within reach, and not named as left out, fails: Crown Road\'s arm crossing taken out of the export', () => {
  const r = run(w => { const x = w.crossings!.find(c => c.landings.some(l => /^street\/arm\//.test(l.id)))!; w.crossings = w.crossings!.filter(c => c !== x); })!;
  expect(r.ok).toBe(false);
  expect(r.detail).toMatch(/^1 fail: arm\/.* has no crossing within 4 m and is not named as left out/);
});

test('a site the export names as left out passes, and fails when the log forgets it; a name no rule makes fails', () => {
  const left = base.w.crossingRules!.left[0]!;
  const gone = run(w => { w.crossingRules!.left = []; })!; // the log forgets why the arm was left out: the arm has no crossing and no reason
  expect(gone.ok).toBe(false);
  expect(gone.detail).toMatch(new RegExp(`arm/crown-road/nw/after .* has no crossing within 4 m and is not named as left out`));
  const stray = run(w => { w.crossingRules!.left.push('arm/nowhere/x: no free spot'); })!;
  expect(stray.ok).toBe(false);
  expect(stray.detail).toContain("the export names 'arm/nowhere/x' as left out, and no rule makes that site");
  expect(left).toContain('no free spot');
});

test('a twin across a side road\'s mouth fails: a copy of the crossing the rules keep, put back where the rules dropped its twin', () => {
  const twin = base.w.crossingRules!.twins.find(t => t.startsWith('market-street at '))!, [, x, y] = /at (-?\d+),(-?\d+):/.exec(twin)!.map(Number) as [number, number, number];
  const r = run(w => {
    const kerb = w.crossings!.filter(c => c.kind === 'kerb' && c.road === 'market-street' && !isSite(c)).sort((a, b) => Math.hypot(mid(a)[0] - x!, mid(a)[1] - y!) - Math.hypot(mid(b)[0] - x!, mid(b)[1] - y!))[0]!;
    const m = mid(kerb), dx = x! - m[0], dy = y! - m[1], clone = structuredClone(kerb);
    clone.id = 'crossing/test/twin'; clone.line = [[kerb.line[0][0] + dx, kerb.line[0][1] + dy], [kerb.line[1][0] + dx, kerb.line[1][1] + dy]];
    clone.landings = kerb.landings.map((l, i) => ({ ...l, id: `street/zz-twin/${i}` })) as never;
    w.crossings!.push(clone);
  })!;
  expect(r.ok).toBe(false);
  expect(r.detail).toMatch(/crossing\/test\/twin is a twin across a side road's mouth \(\d+ m from the crossing it keeps\)/);
});

test('a plan that states no coverage rules has no SF-22; the export carries no crossingRules either', () => {
  const p2: Plan = structuredClone(plan); delete p2.standards.streetscape.crossings.coverage.rules;
  expect(run(() => {}, p2)).toBeUndefined();
  expect(run(w => { delete w.crossingRules; })).toBeUndefined();
});
