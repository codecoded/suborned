// The road finish set switch (brief 055 road test): one key, default = today's enamel; a set the plan does not list is refused.
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { finishSet, finishSets, withFinishSet } from './finish-set';

const plan = JSON.parse(readFileSync(resolve(import.meta.dir, '../../contracts/world/harbour-district.plan.json'), 'utf8'));
const R = plan.standards.streetscape.roads;

describe('the finish set', () => {
  test('the committed plan lays the asphalt (brief 075, Sky: the old asphalt road back; Datum plan 0.5.2), keeping the light ribbon; the enamel is still the base set', () => {
    expect(R.finish.set).toBe('asphalt');
    const s = finishSet(R);
    expect(s.name).toBe('asphalt'); expect(s.marks).toBe('lanes'); expect(s.drop).toEqual(['shoulder']); // SF-24: every road class carries the light ribbon (GDD §14 Datum, §15), so the asphalt keeps it
    const base = finishSet({ ...R, finish: { ...R.finish, set: 'enamel' } });
    expect(base.name).toBe('enamel'); expect(base.marks).toBe('lozenges'); expect(base.byClass).toEqual(R.finish.byClass);
  });
  test('a plan with no `set` reads as the enamel (a pinned plan replays as it was made)', () => {
    const r = structuredClone(R); delete r.finish.set; delete r.finish.sets;
    expect(finishSet(r)).toEqual({ name: 'enamel', byClass: r.finish.byClass, marks: 'lozenges', drop: [] });
  });
  test('the asphalt set: road-asphalt on every class, white lane marks', () => {
    const s = finishSet(withFinishSet(plan, 'asphalt').standards.streetscape.roads);
    expect(s.marks).toBe('lanes'); expect(s.drop).toEqual(['shoulder']); // plan 0.5.2 (Datum, SF-24): the asphalt keeps the light ribbon; the pearl shoulder band is dropped
    expect(Object.values(s.byClass)).toEqual(['road-asphalt', 'road-asphalt', 'road-asphalt']);
    expect(R.finish.keys['road-asphalt'].surface).toBe('road-asphalt');
  });
  test('the switch does not touch the plan it is given; no switch is the same plan', () => {
    const p = withFinishSet(plan, 'asphalt');
    expect(p).not.toBe(plan); expect(R.finish.set).toBe('asphalt');
    const e = withFinishSet(plan, 'enamel'); expect(e).not.toBe(plan); expect(R.finish.set).toBe('asphalt'); expect(e.standards.streetscape.roads.finish.set).toBe('enamel');
    expect(withFinishSet(plan, undefined)).toBe(plan);
  });
  test('a set the plan does not list is refused, naming the ones it has', () => {
    expect(() => withFinishSet(plan, 'gravel')).toThrow(/does not list.*asphalt/);
  });
  test('finishSets lists the base first, then each alternative', () => {
    expect(finishSets(R).map(s => s.name)).toEqual(['enamel', 'asphalt']);
  });
});
