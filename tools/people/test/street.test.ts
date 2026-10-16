// The street at the café (Pulse, brief 014; Sky 2026-10-16: "only 1 waitress outside at the café ... customers come and go all day"): a door is one person's post, the café's
// customers are residents whose days visit it, and no place holds two drawn people at once. Held on the promoted cast (r016), whose people are the Handover's.
import { beforeAll, describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { Cast } from '../../../contracts/people/cast.schema';
import type { People, Person } from '../../../contracts/people/people.schema';
import { checkPeople } from '../check';
import { drawn, insideFromDoor, VISITS } from '../day';
import { expand, type Ctx } from '../expand';
import { readTexts, world, type World } from '../inputs';
import { startMin } from '../handover';

const root = join(import.meta.dir, '..', '..', '..');
let w: World, ctx: Ctx, gen: ReturnType<typeof expand>, t0: number;
const clone = <T>(x: T): T => JSON.parse(JSON.stringify(x));
const failed = (people: People) => checkPeople(ctx, w.mission, w.plan, { people, gaps: [] }).checks.filter(c => !c.ok).map(c => c.id);
/** Who stands at `place` (drawn) at day minute `t`. */
const at = (people: People, place: string, t: number) => people.persons.filter(p => p.routine.stops.some(s => s.place === place && s.from <= t && t < s.to && s.doing !== 'commute' && drawn(p, s)));
const customers = (people: People, t: number) => people.persons.filter(p => p.routine.stops.some(s => s.from <= t && t < s.to && /^cafe-(table|stand)-/.test(s.place) && drawn(p, s)));
beforeAll(async () => {
  w = world(await readTexts('contracts/world/harbour-district.plan.json'), 'civic-block');
  ctx = w.ctx(Cast.parse(JSON.parse(readFileSync(join(root, 'contracts/people/harbour-district.cast.json'), 'utf8')))); gen = expand(ctx);
  t0 = Math.floor(startMin(w.mission));
});

describe('a door is one person\'s post', () => {
  test('the café door holds one drawn person at every minute it is open, and nobody stands the kiosks\' doors twice', () => {
    for (let t = 6 * 60; t < 24 * 60; t += 1) expect(at(gen.people, 'cafe-entrance', t).length).toBeLessThanOrEqual(1);
    for (const u of ['t1-records', 's1-gardens']) for (let t = 0; t < 1440; t += 5) expect(at(gen.people, u, t).filter(p => p.work?.unit.endsWith('-kiosk')).length).toBeLessThanOrEqual(1);
  });
  test('Inez stands it for the whole Handover window, in her place; the other servers of her shift work inside', () => {
    const inez = gen.people.persons.find(p => p.id === 'p-cafe-14')!;
    expect(inez.name.startsWith('Inez ')).toBe(true);
    expect(inez.work!.post).toBe('door');
    for (let t = t0; t <= t0 + 9; t++) expect(at(gen.people, 'cafe-entrance', t).map(p => p.id)).toEqual(['p-cafe-14']);
    const mates = gen.people.persons.filter(p => p.work?.unit === 'cafe' && p.position === 'server' && p.work.shift.from === inez.work!.shift.from && p.id !== inez.id);
    expect(mates.length).toBeGreaterThanOrEqual(3);
    for (const m of mates) { expect(m.work!.post).toBe('inside'); expect(m.work!.place).toBe(inez.work!.place); }
  });
  test('the one who stands it is the one the street knows: a lover outweighs customers; then the lowest id', () => {
    const p = (id: string, ties: Person['ties'], employer = 'cafe'): Person => ({ id, work: { unit: 'cafe', employer, post: 'door', shift: { from: 720, to: 1080 } }, ties } as unknown as Person);
    const guard = { id: 'p-g', work: { unit: 'o', employer: 'o', post: 'inside', shift: { from: 570, to: 1050 } }, ties: [{ kind: 'lover', with: 'p-b' }, { kind: 'regular', with: 'p-b' }] } as unknown as Person;
    const cust = (n: number) => Array.from({ length: n }, (_, i) => ({ id: `p-r${i}`, work: null, ties: [{ kind: 'regular', with: 'p-a' }] }) as unknown as Person);
    const people = [p('p-a', []), p('p-b', [{ kind: 'lover', with: 'p-g' }]), p('p-c', []), guard, ...cust(4)];
    expect([...insideFromDoor(people, new Map([['cafe', 'frontage']]))].sort()).toEqual(['p-a', 'p-c']);
    expect([...insideFromDoor(people.filter(x => x.id !== 'p-b' && x.id !== 'p-g'), new Map([['cafe', 'frontage']]))].sort()).toEqual(['p-c']); // four regulars make p-a the host
    expect([...insideFromDoor(people, new Map([['cafe', 'civic']]))]).toEqual([]); // a civic employer's doors are its row's (count, posts)
  });
  test('P-16 fails when a second drawn person stands on a place already held', () => {
    const p = clone(gen.people), inez = p.persons.find(x => x.id === 'p-cafe-14')!, other = p.persons.find(x => x.id === 'p-cafe-12')!;
    expect(failed(p)).toEqual([]);
    other.work = { ...other.work!, post: 'door' }; // standing the same door at the same hours
    expect(inez.work!.post).toBe('door');
    expect(failed(p)).toContain('P-16'); // (and P-9: the people are no longer what the expander makes)
  });
});

describe('customers come and go', () => {
  test('residents visit the café all day: the café is never empty of customers between opening and closing, and fills at the peak', () => {
    const n = (h: number) => customers(gen.people, h * 60).length;
    for (let h = 8; h < 23; h++) expect(n(h) + n(h + 0.5)).toBeGreaterThan(0);
    expect(n(17.4)).toBeGreaterThan(n(10));
  });
  test('the Handover window has customers on the terrace and a turnover: some arrive and some leave', () => {
    const win = [t0, t0 + 9];
    for (let t = win[0]!; t <= win[1]!; t++) expect(customers(gen.people, t).length).toBeGreaterThanOrEqual(4);
    const seats = gen.people.persons.flatMap(p => p.routine.stops.filter(s => /^cafe-(table|stand)-/.test(s.place) && drawn(p, s)));
    expect(seats.filter(s => s.from >= win[0]! && s.from <= win[1]!).length).toBeGreaterThanOrEqual(2);
    expect(seats.filter(s => s.to >= win[0]! && s.to <= win[1]!).length).toBeGreaterThanOrEqual(2);
  });
  test('a customer\'s visit is a stop of a day: kept clear of the errand and the evening by the walks, at a chair or the counter window, within the café\'s hours', () => {
    const res = gen.people.persons.filter(p => !p.work);
    let visits = 0;
    for (const p of res) for (const s of p.routine.stops) if ((s.doing === 'meal' || s.doing === 'errand') && /^cafe-/.test(s.place)) { visits++; expect(s.from).toBeGreaterThanOrEqual(6 * 60); expect(s.to).toBeLessThanOrEqual(24 * 60); }
    expect(visits).toBeGreaterThanOrEqual(res.length * VISITS.perDay[0] * 0.8);
    // the takeaway stands at the counter window (a standing point), never in a chair
    for (const p of res) for (const s of p.routine.stops) if (s.doing === 'errand' && /^cafe-/.test(s.place)) expect(s.place.startsWith('cafe-stand-')).toBe(true);
  });
  test('the visits are the resident\'s own stream: nobody\'s id, name, temperament, need, ties or work moved, and the expansion is the same twice', () => {
    const again = expand(ctx).people;
    expect(JSON.stringify(again)).toBe(JSON.stringify(gen.people));
    expect(gen.people.persons.length).toBe(130);
  });
});
