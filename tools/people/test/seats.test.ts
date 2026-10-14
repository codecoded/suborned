// Café seating (brief 010 follow-up, Aster 2026-10-15): the seats are the chairs the world draws, people are given them by person id, and P-13 holds them.
// Each rule change has a failing example here. The measured fixture is the built café set's chairs read off the Unity mesh.
import { beforeAll, describe, expect, test } from 'bun:test';
import { Cast } from '../../../contracts/people/cast.schema';
import type { People } from '../../../contracts/people/people.schema';
import { civic, toPlanFn } from '../../plan/civic';
import { civicMasses } from '../../world/build';
import { checkPeople } from '../check';
import { expand, needFits, needsOf, rowOf, type Ctx } from '../expand';
import { readTexts, world, type World } from '../inputs';
import { requirements } from '../review';
import { castFileOf } from '../promoted';
import { CAFE_SET, Holds, SEAT_TOL, STAND, cafeSets, dayParts, seatPicker, seatPoints, type Table } from '../seats';
import measured from './cafe-set.measured.json';

let w: World, ctx: Ctx, gen: ReturnType<typeof expand>;
const clone = <T>(x: T): T => JSON.parse(JSON.stringify(x));
/** A scope with its own copy of the places (the zones keep their test functions). */
const scopeCopy = () => ({ ...w.scope, places: clone(w.scope.places) });
const failed = (people: People, c: Ctx = ctx) => checkPeople(c, w.mission, w.plan, { people, gaps: [] }).checks.filter(x => !x.ok).map(x => x.id);
const kindOf = (id: string) => w.scope.places.find(p => p.id === id)?.kind;
const dist = (a: number[], b: number[]) => Math.hypot(a[0]! - b[0]!, a[1]! - b[1]!);
beforeAll(async () => {
  w = world(await readTexts('contracts/world/harbour-district.plan.json'), 'civic-block');
  ctx = w.ctx(Cast.parse(JSON.parse(await Bun.file(castFileOf('harbour-district')).text()))); gen = expand(ctx);
});

describe('seats are the chairs the world draws', () => {
  test('the chair geometry is what was measured on the built prefab', () => {
    expect(CAFE_SET.radius).toBe(measured.radius);
    const kit = CAFE_SET.anglesDeg.map(a => [CAFE_SET.radius * Math.cos((a * Math.PI) / 180), CAFE_SET.radius * Math.sin((a * Math.PI) / 180)]);
    for (const [i, m] of measured.chairsKit.entries()) expect(dist(kit[i]!, m)).toBeLessThan(measured.tolerance);
  });
  test('each table of the block seats three, where the placer puts the measured chairs at the world\'s own pivot and yaw', () => {
    const block = JSON.parse(readTextsSync('contracts/world/civic-block.json')), plan = w.plan, civ = civic(plan.civic, block);
    const masses = civicMasses(civ, toPlanFn(plan.civic)).filter(m => /^civic\/cafe-table-\d+$/.test(m.id));
    expect(masses.length).toBe(8);
    // the people tools place the sets from tools/world/civic/cafe.ts alone: the export's own masses say the same pivot, yaw and piece
    const sets = cafeSets(civ);
    expect(sets.map(x => x.id)).toEqual(masses.map(m => m.id.replace('civic/', '')).sort());
    for (const m of masses) { const s = sets.find(x => `civic/${x.id}` === m.id)!; expect(s.at).toEqual(m.kit!.at); expect(s.yaw).toBe(m.kit!.yaw!); expect(CAFE_SET.pieces).toContain(m.kit!.id); }
    const seats = w.scope.places.filter(p => p.kind === 'seat');
    expect(seats.length).toBe(24);
    for (const m of masses) {
      const k = m.kit!, [c, s] = [Math.cos(k.yaw!), Math.sin(k.yaw!)];
      expect(k.id).toBe(measured.piece);
      for (const [i, [x, z]] of measured.chairsKit.entries()) {
        const at = [k.at[0] + x! * c - z! * s, k.at[1] + x! * s + z! * c], place = seats.find(p => p.id === `${m.id.replace('civic/', '')}-seat-${i}`)!;
        expect(place).toBeDefined(); expect(dist(place.at, at)).toBeLessThan(0.01);
      }
    }
  });
  test('a table the world draws no chairs on seats one person, at the table', () => {
    expect(seatPoints({ id: 't', at: [1, 2], yaw: 0.3 })).toEqual([[1, 2]]);
    expect(seatPoints({ id: 't', piece: 'CafeSet_Tulip_Play', at: [0, 0], yaw: 0 }).map(p => p.join(','))).toEqual(['0.36,-0.624', '0.36,0.624', '-0.72,0']);
  });
  test('no keep-clear zone bars a table, so every chair is a seat', () => {
    expect(w.scope.zones.every(z => !z.bars.includes('table'))).toBe(true);
    expect(w.scope.seats.length).toBe(24);
  });
});

describe('the standing points', () => {
  test('there are STAND.count, walkable, outside every zone, off every chair, nearest the café door first', () => {
    const stands = w.scope.places.filter(p => p.kind === 'stand'), door = w.scope.places.find(p => p.id === 'cafe-entrance')!.at;
    expect(stands.length).toBe(STAND.count);
    for (const s of stands) { expect(w.scope.zones.some(z => z.test(s.at as [number, number]))).toBe(false); expect(ctx.walk.free(s.at as [number, number])).toBe(true); }
    for (const s of stands) for (const c of w.scope.places.filter(p => p.kind === 'seat')) expect(dist(s.at, c.at)).toBeGreaterThanOrEqual(STAND.chair);
    const d = w.scope.stands.map(id => dist(w.scope.places.find(p => p.id === id)!.at, door));
    expect(d).toEqual([...d].sort((a, b) => a - b));
  });
});

describe('who holds which seat', () => {
  test('Holds: a place is held by one at a time, across midnight too', () => {
    const h = new Holds();
    expect(dayParts(1380, 1500)).toEqual([[1380, 1440], [0, 60]]);
    h.take('a', 1380, 1500);
    expect(h.free('a', 30, 90)).toBe(false); expect(h.free('a', 60, 90)).toBe(true); expect(h.free('a', 1200, 1380)).toBe(true); expect(h.free('b', 1380, 1500)).toBe(true);
  });
  test('the picker gives the person\'s own table first, then the nearest tables, then the standing points, and reports when nothing is free', () => {
    const tables: Table[] = [{ id: 't0', at: [0, 0], seats: ['t0a', 't0b'] }, { id: 't1', at: [5, 0], seats: ['t1a'] }, { id: 't2', at: [20, 0], seats: ['t2a'] }];
    const gaps: string[] = [], holds = new Holds(), pick = seatPicker(tables, ['s0', 's1'], holds, gaps), fit = () => ({ from: 100, to: 130 });
    const got = Array.from({ length: 6 }, (_, i) => pick(`p${i}`)(tables[0]!, fit)!.place);
    expect(got).toEqual(['t0a', 't0b', 't1a', 't2a', 's0', 's1']);
    expect(gaps).toEqual([]);
    expect(pick('p6')(tables[0]!, fit)!.place).toBe('t0a'); // nothing free: the own table's first chair, and a gap
    expect(gaps[0]).toContain('p6');
    expect(pick('p7')(tables[0]!, () => undefined)).toBeUndefined();
  });
  test('the people are given seats: none held twice, and the guards do not stack', () => {
    const held = new Map<string, { who: string; from: number; to: number }[]>();
    for (const p of gen.people.persons) for (const s of p.routine.stops) if (kindOf(s.place) === 'seat' || kindOf(s.place) === 'stand') held.set(s.place, [...(held.get(s.place) ?? []), { who: p.id, from: s.from, to: s.to }]);
    expect(held.size).toBeGreaterThan(24);
    for (const [, l] of held) for (const a of l) for (const b of l) if (a !== b) expect(a.from < b.to && b.from < a.to).toBe(false);
    const pre = gen.people.persons.filter(p => p.routine.stops.some(s => s.doing === 'pre-shift')).map(p => p.routine.stops.find(s => s.doing === 'pre-shift')!.place);
    expect(pre.length).toBeGreaterThanOrEqual(8); expect(new Set(pre).size).toBe(pre.length);
    expect(new Set(pre.map(s => s.replace(/-seat-\d$/, ''))).size).toBeGreaterThanOrEqual(3);
  });
  test('a standing point is taken only while every chair is held', () => {
    const chairs = new Set(gen.people.places.filter(p => p.kind === 'seat').map(p => p.id));
    const stopsAt = new Map<string, { from: number; to: number }[]>();
    for (const p of gen.people.persons) for (const s of p.routine.stops) if (chairs.has(s.place)) stopsAt.set(s.place, [...(stopsAt.get(s.place) ?? []), s]);
    let stands = 0;
    for (const p of gen.people.persons) for (const s of p.routine.stops) {
      if (kindOf(s.place) !== 'stand') continue;
      stands++;
      for (const c of chairs) expect((stopsAt.get(c) ?? []).some(x => x.from < s.to && s.from < x.to)).toBe(true);
    }
    expect(stands).toBeGreaterThan(0);
  });
  test('seating moves nobody\'s id, name, job or home, and the routines keep their stop kinds', () => {
    const again = expand(ctx).people;
    expect(again.persons.map(p => p.id)).toEqual(gen.people.persons.map(p => p.id));
    expect(gen.people.persons.every(p => p.routine.stops.every(s => s.to > s.from))).toBe(true);
  });
});

describe('P-13 and P-5 with seats', () => {
  test('the real expansion passes', () => {
    const r = checkPeople(ctx, w.mission, w.plan, gen);
    expect(r.checks.find(c => c.id === 'P-13')!.ok).toBe(true);
    expect(r.ok).toBe(true);
  });
  test('P-13: two people on one chair at once fails', () => {
    const p = clone(gen.people), [a, b] = p.persons.filter(q => q.routine.stops.some(s => s.doing === 'pre-shift'));
    const sa = a!.routine.stops.find(s => s.doing === 'pre-shift')!, sb = b!.routine.stops.find(s => s.doing === 'pre-shift')!;
    sb.place = sa.place; sb.from = sa.from; sb.to = sa.to;
    const bad = checkPeople(ctx, w.mission, w.plan, { people: p, gaps: [] }).checks.find(c => c.id === 'P-13')!;
    expect(bad.ok).toBe(false); expect(bad.detail).toContain(`both hold ${sa.place}`);
  });
  test('P-13: a seat that is on no chair fails', () => {
    const scope = scopeCopy(), seat = scope.places.find(p => p.kind === 'seat')!; seat.at = [seat.at[0] + 1, seat.at[1]];
    const bad = checkPeople({ ...ctx, scope }, w.mission, w.plan, { people: gen.people, gaps: [] }).checks.find(c => c.id === 'P-13')!;
    expect(bad.ok).toBe(false); expect(bad.detail).toContain(`seat ${seat.id} is at`); expect(SEAT_TOL).toBeLessThan(1);
    const scope2 = scopeCopy(); scope2.places.push({ id: 'cafe-table-9-seat-0', kind: 'seat', name: 'x', at: [0, 0], area: 'records' });
    expect(checkPeople({ ...ctx, scope: scope2 }, w.mission, w.plan, { people: gen.people, gaps: [] }).checks.find(c => c.id === 'P-13')!.detail).toContain('on no table or chair');
  });
  test('P-13: a standing point inside a keep-clear zone fails', () => {
    const scope = scopeCopy(), stand = scope.places.find(p => p.kind === 'stand')!, door = scope.places.find(p => p.id === 'cafe-entrance')!; stand.at = door.at;
    const bad = checkPeople({ ...ctx, scope }, w.mission, w.plan, { people: gen.people, gaps: [] }).checks.find(c => c.id === 'P-13')!;
    expect(bad.ok).toBe(false); expect(bad.detail).toContain('is in');
  });
  test('P-5: someone seated in the café terrace is not standing in its zone; a standing point there is', () => {
    const terrace = w.scope.places.find(p => p.kind === 'seat' && w.scope.zones.some(z => z.id === 'cafe-terrace' && z.test(p.at as [number, number])))!;
    expect(terrace).toBeDefined();
    const mk = (place: string) => { const p = clone(gen.people), x = p.persons.find(q => q.routine.stops.some(s => s.doing === 'evening'))!; x.routine.stops.find(s => s.doing === 'evening')!.place = place; return p; };
    expect(checkPeople(ctx, w.mission, w.plan, { people: mk(terrace.id), gaps: [] }).checks.find(c => c.id === 'P-5')!.ok).toBe(true);
    const scope = scopeCopy(), stand = scope.places.find(p => p.kind === 'stand')!; stand.at = terrace.at;
    expect(checkPeople({ ...ctx, scope }, w.mission, w.plan, { people: mk(stand.id), gaps: [] }).checks.find(c => c.id === 'P-5')!.ok).toBe(false);
  });
});

describe('unknown = reject: needs, regulars, mix (Suture QA 2026-10-15)', () => {
  test('every person carries a need their own row offers their position', () => {
    for (const p of gen.people.persons) {
      const row = p.work ? rowOf(ctx.cast, p.work.area, p.work.trade) : rowOf(ctx.cast, w.scope.homes.find(h => h.plot === p.home.plot)!.area, 'resident');
      expect(needsOf(row, p.work ? (p.work.position ?? p.work.role) : 'resident').map(n => n.need)).toContain(p.need);
    }
  });
  test('P-3: a driver with debt, a need the driver row never offers, fails', () => {
    const p = clone(gen.people), d = p.persons.find(q => q.work?.role === 'official-driver')!;
    expect(d.need).not.toBe('debt'); d.need = 'debt';
    const bad = checkPeople(ctx, w.mission, w.plan, { people: p, gaps: [] }).checks.find(c => c.id === 'P-3')!;
    expect(bad.ok).toBe(false); expect(bad.detail).toContain(`${d.id}: need debt is not one its row`);
  });
  test('a temperament that reaches none of a row\'s needs is drawn again from the row\'s weights', () => {
    const drivers = gen.people.persons.filter(q => q.work?.role === 'official-driver');
    expect(drivers.length).toBeGreaterThan(0);
    for (const d of drivers) expect(needFits(w.rules, d.need, d.temperament, { home: true, work: true, family: false, rival: false })).toBe(true);
  });
  test('the repair: a row that draws Fearful almost always still gives every driver a temperament that can meet cash', () => {
    const cast = clone(ctx.cast), row = cast.rows.find(r => r.occupation === 'records-office')!;
    row.temperaments = [{ temperament: 'fearful', weight: 59 }, { temperament: 'idealist', weight: 41 }];
    const g = expand({ ...ctx, cast }), drivers = g.people.persons.filter(q => q.work?.role === 'official-driver');
    expect(drivers.length).toBe(7);
    for (const d of drivers) { expect(d.temperament).toBe('idealist'); expect(d.need).toBe('cash'); }
    expect(g.gaps.filter(x => drivers.some(d => x.startsWith(d.id)))).toEqual([]);
  });
  test('no need the row can reach for anyone in it is an expansion error, never a draw from the whole list', () => {
    const cast = clone(ctx.cast), row = cast.rows.find(r => r.occupation === 'diner')!;
    row.temperaments = [{ temperament: 'fearful', weight: 100 }]; row.needs = [{ need: 'cash', weight: 100 }]; // a Fearful person answers to Pressure, which cannot meet cash
    const g = expand({ ...ctx, cast }), bad = g.gaps.filter(x => x.includes('cash'));
    expect(bad.length).toBeGreaterThan(0); expect(bad[0]).toContain('can be met by an approach a fearful answers to');
    expect(checkPeople({ ...ctx, cast }, w.mission, w.plan).checks.find(c => c.id === 'P-0')!.ok).toBe(false);
  });
  test('no regular tie points at the person\'s own employer', () => {
    const by = new Map(gen.people.persons.map(p => [p.id, p]));
    let regular = 0;
    for (const p of gen.people.persons) for (const t of p.ties) if (t.kind === 'regular') { regular++; if (p.work) expect(by.get(t.with)!.work!.employer).not.toBe(p.work.employer); }
    expect(regular).toBeGreaterThan(0);
  });
  test('P-7: a regular tie to a colleague of one\'s own employer fails', () => {
    const p = clone(gen.people), a = p.persons.find(q => q.work?.role === 'clerk')!, b = p.persons.filter(q => q.work?.role === 'clerk')[1]!;
    a.ties.push({ kind: 'regular', with: b.id });
    const bad = checkPeople(ctx, w.mission, w.plan, { people: p, gaps: [] }).checks.find(c => c.id === 'P-7')!;
    expect(bad.ok).toBe(false); expect(bad.detail).toContain('same employer');
  });
  test('the people\'s share of a row is reported beside the weights, and the cap stays on the row', () => {
    const r = requirements(ctx, gen.people).filter(x => x.id.startsWith('mix-'));
    expect(r.length).toBe(ctx.cast.rows.length);
    for (const x of r) { expect(x.detail).toMatch(/of the \d+ people \d+%/); expect(x.detail).toContain('counted, not held'); expect(x.ok).toBe(true); }
  });
});

function readTextsSync(f: string) { return require('node:fs').readFileSync(require('node:path').resolve(import.meta.dir, '../../..', f), 'utf8') as string; }
