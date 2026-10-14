// Brief 010 B: the scope, the expander and the checker, on the real plan, civic block, life.json and people rules, with the stand-in cast.
// Each rule has a failing example here: a change to the expansion (or the cast) that makes exactly that rule fail.
import { beforeAll, describe, expect, test } from 'bun:test';
import { Cast } from '../../../contracts/people/cast.schema';
import { People } from '../../../contracts/people/people.schema';
import { checkPeople, RULES_SRC } from '../check';
import { expand, quota, fold, type Ctx } from '../expand';
import { readTexts, world, type World } from '../inputs';
import { castHandover } from '../handover';
import { standInCast } from '../standin';
import { buildScope, shiftsOf } from '../scope';

let w: World, ctx: Ctx, gen: ReturnType<typeof expand>;
const clone = <T>(x: T): T => JSON.parse(JSON.stringify(x));
const failed = (people: People, c: Ctx = ctx, mission = w.mission) => checkPeople(c, mission, w.plan, { people, gaps: [] }).checks.filter(x => !x.ok).map(x => x.id);
beforeAll(async () => {
  w = world(await readTexts('contracts/world/harbour-district.plan.json'), 'civic-block');
  ctx = w.ctx(standInCast(w.scope, w.rules)); gen = expand(ctx);
});

describe('scope', () => {
  test('civic-block is defined from the block and the plan', () => {
    expect(w.scope.areas).toEqual(['records']);
    expect(w.scope.plots.map(p => p.id).sort()).toEqual(['records-office', 'records-square']);
    expect(w.scope.stops.map(s => s.place)).toEqual(['t1-records', 's1-gardens']);
    expect(w.scope.units.map(u => u.id)).toEqual(['cafe', 'records-office', 's1-gardens-kiosk', 't1-records-kiosk']);
    expect(w.scope.units.find(u => u.id === 'cafe')).toMatchObject({ trade: 'diner', staffPerShift: 8 });
  });
  test('an unknown scope is rejected', () => {
    expect(() => buildScope(w.plan, {}, {}, w.rules, 'harbour')).toThrow(/unknown scope/);
  });
  test('shifts tile the opening hours', () => {
    expect(shiftsOf([6, 24], 8)).toEqual([{ from: 360, to: 720 }, { from: 720, to: 1080 }, { from: 1080, to: 0 }]);
  });
});

describe('expander', () => {
  test('quota gives every position one first, then by weight', () => {
    expect([...quota(10, [{ key: 'a', weight: 8 }, { key: 'b', weight: 1 }, { key: 'c', weight: 1 }])]).toEqual([['a', 6], ['b', 2], ['c', 2]]);
    expect([...quota(1, [{ key: 'a', weight: 1 }, { key: 'b', weight: 3 }])]).toEqual([['b', 1]]);
  });
  test('fold splits a cycle at midnight', () => {
    expect(fold([{ place: 'h', doing: 'home', from: 1380, to: 1500 }])).toEqual([{ place: 'h', doing: 'home', from: 0, to: 60 }, { place: 'h', doing: 'home', from: 1380, to: 1440 }]);
  });
  test('the same inputs give the same bytes', () => {
    expect(JSON.stringify(expand(ctx).people)).toBe(JSON.stringify(gen.people));
  });
  test('the output parses and is small', () => {
    expect(People.safeParse(gen.people).success).toBe(true);
    expect(gen.people.persons.length).toBeLessThan(200);
  });
  test('the stand-in cast parses', () => expect(Cast.safeParse(ctx.cast).success).toBe(true));
});

describe('checker', () => {
  test('every rule has a source', () => {
    const r = checkPeople(ctx, w.mission, w.plan, gen);
    expect(r.checks.map(c => c.id)).toEqual(Object.keys(RULES_SRC));
    expect(r.checks.every(c => c.source)).toBe(true);
    expect(r.ok).toBe(true);
  });
  test('P-0: a cast without a row for a unit fails', () => {
    const cast = clone(ctx.cast); cast.rows = cast.rows.filter(r => r.occupation !== 'diner');
    const c = { ...ctx, cast };
    expect(checkPeople(c, w.mission, w.plan).checks.filter(x => !x.ok).map(x => x.id)).toContain('P-0');
  });
  test('P-1: a routine with no time to walk fails', () => {
    const p = clone(gen.people), x = p.persons.find(q => q.work && q.routine.stops.length > 3)!;
    const i = x.routine.stops.findIndex((s, k) => k > 0 && s.place !== x.routine.stops[k - 1]!.place && s.place !== 'beyond' && x.routine.stops[k - 1]!.place !== 'beyond');
    x.routine.stops[i]!.from = x.routine.stops[i - 1]!.to;
    expect(failed(p)).toContain('P-1');
  });
  test('P-2: a need outside the rules fails', () => {
    const p = clone(gen.people); p.persons[0]!.need = 'wanderlust';
    expect(failed(p)).toEqual(['P-2', 'P-3']);
  });
  test('P-3: a guard who is an idealist fails', () => {
    const p = clone(gen.people); p.persons.find(q => q.work?.role === 'guard')!.temperament = 'idealist';
    expect(failed(p)).toContain('P-3');
  });
  test('P-4: a missing staff member fails', () => {
    const p = clone(gen.people), i = p.persons.findIndex(q => q.work?.unit === 'cafe'); p.persons.splice(i, 1);
    expect(failed(p)).toContain('P-4');
  });
  test('P-5: an evening in a keep-clear zone fails', () => {
    const p = clone(gen.people), x = p.persons.find(q => q.routine.stops.some(s => s.doing === 'evening'))!;
    x.routine.stops.find(s => s.doing === 'evening')!.place = 'cafe-entrance';
    expect(failed(p)).toContain('P-5');
  });
  test('P-6: no guard on the Office door fails the Handover', () => {
    const p = clone(gen.people); p.persons = p.persons.filter(q => q.work?.role !== 'guard');
    expect(failed(p)).toContain('P-6');
  });
  test('P-7: a two-way tie that is not returned fails', () => {
    const p = clone(gen.people), x = p.persons.find(q => q.ties.some(t => t.kind === 'workmate'))!, t = x.ties.find(y => y.kind === 'workmate')!;
    const o = p.persons.find(q => q.id === t.with)!; o.ties = o.ties.filter(y => !(y.kind === 'workmate' && y.with === x.id));
    expect(failed(p)).toContain('P-7');
  });
  test('P-8: a name outside the pool fails', () => {
    const p = clone(gen.people); p.persons[3]!.name = 'Zaphod Beeblebrox';
    expect(failed(p)).toContain('P-8');
  });
  test('P-9: a person with a memory fails', () => {
    const p = clone(gen.people); (p.persons[0]!.memory as unknown[]).push('x');
    expect(failed(p)).toContain('P-9');
  });
});

describe('the Handover', () => {
  test('casts a waitress at the café at the mission start, the driver at the bay, the guard at the Office door, and Vale\'s office', () => {
    const c = castHandover(gen.people.persons, w.scope, w.mission, w.plan);
    expect(c.map(x => `${x.role}:${x.ok}`)).toEqual(['waitress:true', 'driver:true', 'guard:true', 'councillor:true']);
    const inez = gen.people.persons.filter(p => p.name.startsWith('Inez '));
    expect(inez).toHaveLength(1);
    expect(c[0]!.who).toContain(inez[0]!.id);
    expect(gen.people.persons.filter(p => p.name.endsWith(' Vale') && p.work?.role === 'councillor')).toHaveLength(1);
  });
});
