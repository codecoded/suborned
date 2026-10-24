// The road finish set switch (brief 055 road test): one key, default = today's enamel; a set the plan does not list is refused.
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { finishSet, finishSets, withFinishSet } from './finish-set';

const plan = JSON.parse(readFileSync(resolve(import.meta.dir, '../../contracts/world/harbour-district.plan.json'), 'utf8'));
const R = plan.standards.streetscape.roads;

describe('the finish set', () => {
  test('the committed plan lays the enamel: byClass as before, lozenges', () => {
    expect(R.finish.set).toBe('enamel');
    const s = finishSet(R);
    expect(s.name).toBe('enamel'); expect(s.marks).toBe('lozenges'); expect(s.byClass).toEqual(R.finish.byClass);
  });
  test('a plan with no `set` reads as the enamel (a pinned plan replays as it was made)', () => {
    const r = structuredClone(R); delete r.finish.set; delete r.finish.sets;
    expect(finishSet(r)).toEqual({ name: 'enamel', byClass: r.finish.byClass, marks: 'lozenges', drop: [] });
  });
  test('the asphalt set: road-asphalt on every class, white lane marks', () => {
    const s = finishSet(withFinishSet(plan, 'asphalt').standards.streetscape.roads);
    expect(s.marks).toBe('lanes'); expect(s.drop).toEqual(['ribbon', 'shoulder', 'pinstripe']); // the clean asphalt: the pearl kerb and the road
    expect(Object.values(s.byClass)).toEqual(['road-asphalt', 'road-asphalt', 'road-asphalt']);
    expect(R.finish.keys['road-asphalt'].surface).toBe('road-asphalt');
  });
  test('the switch does not touch the plan it is given; no switch is the same plan', () => {
    const p = withFinishSet(plan, 'asphalt');
    expect(p).not.toBe(plan); expect(R.finish.set).toBe('enamel');
    expect(withFinishSet(plan, undefined)).toBe(plan);
  });
  test('a set the plan does not list is refused, naming the ones it has', () => {
    expect(() => withFinishSet(plan, 'gravel')).toThrow(/does not list.*asphalt/);
  });
  test('finishSets lists the base first, then each alternative', () => {
    expect(finishSets(R).map(s => s.name)).toEqual(['enamel', 'asphalt']);
  });
});
