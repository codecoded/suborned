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

test('the live export: 22 rule sites (11 arms, 7 island, 2 mall, 2 mid-block), no twins (the landings stand 3 m past the kerb returns), furniture moved, 3 sites left out, and SF-22 passes', () => {
  // brief 028: every junction landing's near edge stands 3 m past the kerb return's tangent point (kerbReturn.landings), so no two kerb crossings of one road stand within 30 m across a side road's mouth any more
  const log = base.w.crossingRules!;
  expect(log.sites).toEqual({ arm: 11, island: 7, mall: 2, mid: 2 });
  expect(log.twins).toHaveLength(0);
  expect(log.moved.length).toBeGreaterThan(0);
  expect(log.left.map(l => l.split(':')[0])).toEqual(['arm/crown-road/nw/after', 'arm/crown-road/n_avenue/before', 'island/circus-road/r']); // the Crown Terraces podium stands at the kerb: no landing fits on it; a flag mast and a tree stand where the shifted island crossing would
  expect(base.w.crossings!.filter(isSite)).toHaveLength(22);
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

test('a twin across a side road\'s mouth fails: a copy of a kerb crossing put across a side road\'s mouth, 20 m from the one it copies', () => {
  const kerb = base.w.crossings!.filter(c => c.kind === 'kerb' && !isSite(c)).map(c => { const r = d.roads.find(q => q.id === c.road)!; return { c, r, node: r.path.map(n => plan.nodes[n]!).find(n => Math.hypot(n[0] - mid(c)[0], n[1] - mid(c)[1]) < 14 && d.roads.some(q => q !== r && q.path.some(m => plan.nodes[m] === n))) }; }).find(x => x.node && x.r.class !== 'primary')!;
  const m = mid(kerb.c), n = kerb.node!, dx = (n[0] - m[0]) * 2, dy = (n[1] - m[1]) * 2;
  const r = run(w => {
    const clone = structuredClone(w.crossings!.find(c => c.id === kerb.c.id)!);
    clone.id = 'crossing/test/twin'; clone.line = [[clone.line[0][0] + dx, clone.line[0][1] + dy], [clone.line[1][0] + dx, clone.line[1][1] + dy]];
    clone.landings = clone.landings.map((l, i) => ({ ...l, id: `street/zz-twin/${i}` })) as never;
    w.crossings!.push(clone);
  })!;
  expect(r.ok).toBe(false);
  expect(r.detail).toMatch(/crossing\/test\/twin is a twin across a side road's mouth \(\d+ m from the crossing it keeps\)/);
});
