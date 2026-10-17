// Brief 010 B: the scope, the expander and the checker, on the real plan, civic block, life.json and people rules, with the stand-in cast.
// Each rule has a failing example here: a change to the expansion (or the cast) that makes exactly that rule fail.
import { beforeAll, describe, expect, test } from 'bun:test';
import { Cast } from '../../../contracts/people/cast.schema';
import { People } from '../../../contracts/people/people.schema';
import { checkPeople, RULES_SRC } from '../check';
import { fold, quota } from '../day';
import { expand, type Ctx } from '../expand';
import { readTexts, world, type World } from '../inputs';
import { castHandover } from '../handover';
import { standInCast } from '../standin';
import { buildScope, shiftsOf } from '../scope';
import { deriveKnowledge } from '../knowledge';
import { pinnedByRole } from '../roles';
import { requirements, reviewHtml, stepWord } from '../review';

let w: World, ctx: Ctx, gen: ReturnType<typeof expand>;
const clone = <T>(x: T): T => JSON.parse(JSON.stringify(x));
const failed = (people: People, c: Ctx = ctx, mission = w.mission) => checkPeople(c, mission, w.plan, { people, gaps: [] }).checks.filter(x => !x.ok).map(x => x.id);
beforeAll(async () => {
  w = world(await readTexts('contracts/world/harbour-district.plan.json'), 'civic-block');
  ctx = w.ctx(standInCast(w.scope, w.rules, { lovers: true })); gen = expand(ctx);
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
    expect(failed(p)).toEqual(['P-2', 'P-3', 'P-9', 'N-1']); // an edited file is no longer the expansion of its cast either (P-9 re-expands); and their record is of another kind (N-1)
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

describe('mix, knowledge and fields read (P3, P5, P2)', () => {
  test('P-10: a row with one temperament over 60% fails, and so does a unit of 3+ staff of one kind', () => {
    const cast = clone(ctx.cast), row = cast.rows.find(r => r.occupation === 'diner')!;
    row.temperaments = [{ temperament: 'idealist', weight: 9 }, { temperament: 'mercenary', weight: 1 }];
    const bad = checkPeople({ ...ctx, cast }, w.mission, w.plan).checks.find(x => x.id === 'P-10')!;
    expect(bad.ok).toBe(false); expect(bad.detail).toContain('records/diner: idealist is 90% of the row');
    const p = clone(gen.people); for (const q of p.persons.filter(q => q.work?.unit === 'cafe')) q.temperament = 'mercenary';
    expect(failed(p)).toContain('P-10');
  });
  test('P-10: the expander repairs a unit that the draw left with one kind', () => {
    const cast = clone(ctx.cast), row = cast.rows.find(r => r.occupation === 'diner')!;
    row.temperaments = [{ temperament: 'mercenary', weight: 99 }, { temperament: 'idealist', weight: 1 }]; // far over the cap: the draw alone gives one kind
    const g = expand({ ...ctx, cast }), cafe = g.people.persons.filter(q => q.work?.unit === 'cafe');
    expect(new Set(cafe.map(q => q.temperament)).size).toBeGreaterThanOrEqual(2);
  });
  test('P-11: the keeper\'s known needs derive from workmate and regular ties; the cell is face only', () => {
    expect(gen.people.knowledge).toEqual(deriveKnowledge(w.rules, gen.people.persons));
    const cafe = gen.people.knowledge!.needsKnown.find(k => gen.people.persons.find(q => q.id === k.keeper)!.work!.unit === 'cafe')!;
    expect(cafe.knows.length).toBeGreaterThan(0);
    expect(gen.people.knowledge).toMatchObject({ cellAtStart: 'nothing', identity: ['face'] });
    const p = clone(gen.people); p.knowledge!.needsKnown[0]!.knows.push('p-cafe-01');
    expect(failed(p)).toContain('P-11');
    const q = clone(gen.people); delete q.knowledge;
    expect(failed(q)).toContain('P-11');
  });
  test('P-12: a field no rule reads fails', () => {
    const p = clone(gen.people); (p.persons[0] as Record<string, unknown>).bio = 'grew up by the docks';
    expect(failed(p)).toContain('P-12');
  });
});

describe('names are given by role, not by luck', () => {
  test('Inez and Vale are the people the Handover casts in those roles', () => {
    const by = pinnedByRole(gen.people.persons, ctx.casting, w.rules), names = w.rules.names.pinned;
    expect(by.every(x => x.who)).toBe(true);
    for (const [i, x] of by.entries()) expect(gen.people.persons.find(q => q.id === x.who)!.name.split(' ')[names[i]!.part === 'given' ? 0 : 1]).toBe(names[i]!.name);
    expect(by.map(x => x.role)).toEqual(['waitress', 'councillor']);
  });
  test('a mission that pins another waitress moves the name to her (it breaks if the expander goes back to choosing by order)', () => {
    const cast = castHandover(gen.people.persons, w.scope, w.mission, w.plan, w.rules), other = cast[0]!.who.find(id => id !== cast[0]!.pinned)!;
    expect(other).toBeTruthy();
    const mission = clone(w.mission); mission.roles.find(r => r.id === 'waitress')!.pin = other;
    const c = { ...ctx, casting: { ...ctx.casting!, mission } }, g = expand(c);
    expect(g.people.persons.find(q => q.name.startsWith('Inez '))!.id).toBe(other);
    expect(g.people.persons.filter(q => q.name.startsWith('Inez '))).toHaveLength(1);
    expect(g.gaps).toEqual([]);
    expect(checkPeople(c, mission, w.plan, g).ok).toBe(true);
  });
  test('P-8: a pinned name on someone the Handover does not cast fails', () => {
    const p = clone(gen.people), a = p.persons.find(q => q.name.startsWith('Inez '))!, b = p.persons.find(q => q.work?.position === 'server' && q.id !== a.id)!;
    const t = a.name; a.name = b.name; b.name = t;
    expect(failed(p)).toContain('P-8');
  });
});

describe('cast requirements and the review page', () => {
  test('the stand-in meets the Handover\'s cast requirements, read from the mission', () => {
    const r = requirements(ctx, gen.people);
    expect(r.filter(x => !x.ok)).toEqual([]);
    expect(r.map(x => x.id)).toEqual(expect.arrayContaining(['waitress-weights-debt', 'driver-weights-cash', 'guard-regular-of-cafe', 'keeper-cafe']));
  });
  test('a cast that weights nothing for the role\'s need does not', () => {
    const cast = clone(ctx.cast);
    for (const r of cast.rows) r.needs = r.needs.filter(n => n.need !== 'debt');
    const g = expand({ ...ctx, cast }), r = requirements({ ...ctx, cast }, g.people);
    expect(r.find(x => x.id === 'waitress-weights-debt')!.ok).toBe(false);
  });
  test('the guard requirement is a share of guards holding a regular tie into the café, and the ties are aimed at the café staff', () => {
    const cafeStaff = new Set(gen.people.persons.filter(q => q.work?.employer === 'cafe').map(q => q.id));
    const guards = gen.people.persons.filter(q => q.work?.role === 'guard' && q.work.employer === 'records-office');
    const aimed = guards.filter(q => q.ties.some(t => t.kind === 'regular' && cafeStaff.has(t.with)));
    expect(aimed.length / guards.length).toBeGreaterThanOrEqual(0.5); // a pre-shift stop is aimed at the café's staff, not the Office's own
    for (const q of guards) for (const t of q.ties.filter(x => x.kind === 'regular')) expect(gen.people.persons.find(o => o.id === t.with)!.work!.employer).not.toBe('records-office');
    // failing example: preShift cafe on every variant and a stop at the café, but no guard holds the tie: NOT MET (the old rule read ties > 0 or a stop)
    const p = clone(gen.people);
    for (const q of p.persons) q.ties = q.ties.filter(t => !(t.kind === 'regular' && cafeStaff.has(t.with)));
    const line = requirements(ctx, p).find(x => x.id === 'guard-regular-of-cafe')!;
    expect(line.ok).toBe(false); expect(line.detail).toContain('0 of');
    // one tie in eleven is not a share
    const one = clone(p), g1 = one.persons.find(q => q.id === aimed[0]!.id)!; g1.ties.push({ kind: 'regular', with: [...cafeStaff][0]! });
    expect(requirements(ctx, one).find(x => x.id === 'guard-regular-of-cafe')!.ok).toBe(false);
  });
  test('the page shows step words, can introduce and knows the needs of, byte-identically', () => {
    const checks = checkPeople(ctx, w.mission, w.plan, gen).checks, html = reviewHtml(ctx, gen.people, checks, 'test');
    expect(html).toBe(reviewHtml(ctx, gen.people, checks, 'test'));
    for (const m of ['Can introduce:', 'Knows the needs of:', 'informant', stepWord(w.rules, 'fear', 0), 'Inez ', 'Cast requirements']) expect(html).toContain(m);
    expect(html).not.toContain('NOT MET'); expect(html).not.toContain('FAIL');
  });
});

describe('the Handover', () => {
  test('casts a waitress at the café at the mission start, the driver at the bay, the guard at the Office door, and Vale\'s office', () => {
    const c = castHandover(gen.people.persons, w.scope, w.mission, w.plan, w.rules);
    expect(c.map(x => `${x.role}:${x.ok}`)).toEqual(['waitress:true', 'driver:true', 'guard:true', 'councillor:true']);
    const inez = gen.people.persons.filter(p => p.name.startsWith('Inez '));
    expect(inez).toHaveLength(1);
    expect(c[0]!.who).toContain(inez[0]!.id);
    expect(gen.people.persons.filter(p => p.name.endsWith(' Vale') && p.work?.role === 'councillor')).toHaveLength(1);
  });
});
