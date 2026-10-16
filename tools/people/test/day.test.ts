// The day (tools/people/day.ts) and the Handover fix (Pulse, brief 010, 2026-10-15): the posts, the watch and the official car of the civic office, the aimed ties, and
// the cast the fix is made of (tools/people/test/handover-fix.cast.json, made by make-handover-fix.ts from the promoted cast), held to the Handover's own conditions (M-2, with
// the playability checks of tools/missions/handover/cast.ts) and to the people checker. The r013 cast (the promoted cast until r016, read from its run by test/r013.ts) does not play the Handover, and a test says so; the promoted cast now does, and a test says that.
import { beforeAll, describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { Cast } from '../../../contracts/people/cast.schema';
import type { People, Person } from '../../../contracts/people/people.schema';
import { sha256 } from '../../gen/sha';
import { overlay } from '../../missions/handover/overlay';
import { civic } from '../../plan/civic';
import type { V } from '../../plan/geom';
import { derive } from '../../plan/plan';
import { checkPeople } from '../check';
import { STEP_OFF, staffing, stepMin, workPlaceOf } from '../day';
import { expand, type Ctx } from '../expand';
import { readTexts, world, type World } from '../inputs';
import { rng } from '../../world/gen/common';
import { legacyRules } from './legacy';
import { r013Cast } from './r013';

const root = join(import.meta.dir, '..', '..', '..');
const read = (f: string) => JSON.parse(readFileSync(join(root, f), 'utf8'));
let w: World, texts: Awaited<ReturnType<typeof readTexts>>, fix: Ctx, promoted: Ctx, r013: Ctx, fixGen: ReturnType<typeof expand>, promGen: ReturnType<typeof expand>, r013Gen: ReturnType<typeof expand>;
const clone = <T>(x: T): T => JSON.parse(JSON.stringify(x));
const hm = (t: number) => `${String(Math.floor(t / 60)).padStart(2, '0')}:${String(t % 60).padStart(2, '0')}`;
const day = (h: number, m = 0) => h * 60 + m;
beforeAll(async () => {
  texts = await readTexts('contracts/world/harbour-district.plan.json');
  w = world(texts, 'civic-block');
  fix = w.ctx(Cast.parse(read('tools/people/test/handover-fix.cast.json'))); fixGen = expand(fix);
  promoted = w.ctx(Cast.parse(read('contracts/people/harbour-district.cast.json'))); promGen = expand(promoted);
  r013 = w.ctx(r013Cast()); r013Gen = expand(r013);
});
/** M-2 on these people, the check the plan checker's overlay runs (it expands the promoted cast itself; here the people are given). */
const m2 = (people: People, mission: object = JSON.parse(texts.mission)) => {
  const block = JSON.parse(texts.block);
  return overlay(w.plan, block, people as never, mission as never).civic!({ d: derive(w.plan), civ: civic(w.plan.civic, block) } as never).find(c => c.id === 'M-2')!;
};
const failed = (people: People) => checkPeople(fix, w.mission, w.plan, { people, gaps: [] }).checks.filter(c => !c.ok).map(c => c.id);
const by = (people: People, f: (p: Person) => boolean) => people.persons.filter(f);
const role = (people: People, r: string) => by(people, p => p.work?.role === r);
const stopAt = (p: Person, t: number) => p.routine.stops.find(s => s.from <= t && t < s.to);
const ate = (p: Person, from: number, to: number) => p.routine.stops.filter(s => s.to > from && s.from < to);

describe('the day, split out of the expander', () => {
  test('the r013 cast makes the same people it always did', () => {
    // the vocabulary below is opt-in: a cast row that does not use it expands exactly as before. This hash is of the persons of the promoted cast (r013) at the commit that
    // moved the day into day.ts; a deliberate change to the expansion updates it, with its cause declared in tools/levels/people/drift.json.
    // under the rulebook of that commit (no lover tie, no secrets: legacyRules) it is the same bytes; under the current rules the persons carry a secret and the need `secret`
    // is retired into the row's record share, so the needs of the people who drew it differ (brief 011; declared in drift.json)
    expect(sha256(JSON.stringify(expand({ ...r013, rules: legacyRules(w.rules) }).people.persons))).toBe('ed8704178887d70149d27806b6f0d7eb54798689ca38d9be68a39024b4ab6703');
    expect(sha256(JSON.stringify(r013Gen.people.persons))).toBe('992c0a44a92565698d98a7e2976fdacc7b989d820663b5be6b7c865d43998319');
  });
  test('a hop of STEP_OFF m or less costs no minute; a longer one is the walk graph\'s', () => {
    const at = (id: string) => w.scope.places.find(p => p.id === id)!.at as V;
    expect(stepMin(fix.walk, at('official-entrance'), at('office-bay'))).toBe(0); // 2.5 m: the door and the bay beside it
    expect(Math.hypot(...([0, 1] as const).map(i => at('official-entrance')[i]! - at('office-bay')[i]!) as [number, number])).toBeLessThanOrEqual(STEP_OFF);
    expect(stepMin(fix.walk, at('office-entrance'), at('office-bay'))).toBe(fix.walk.min(at('office-entrance'), at('office-bay')));
    expect(stepMin(fix.walk, at('office-entrance'), at('office-bay'))).toBeGreaterThan(1);
  });
});

describe('staffing: counts, posts, watch', () => {
  const unit = () => w.scope.units.find(u => u.id === 'records-office')!;
  const rowOf = (c: Ctx) => c.cast.rows.find(r => r.occupation === 'records-office')!;
  const plan = (c: Ctx) => {
    const u = unit(), row = rowOf(c), gaps: string[] = [];
    return { plans: staffing(u, row, row.positions.filter(p => p.position !== 'keeper'), false, 'keeper', si => rng(`${u.plot}/${u.id}/shift${si}`), gaps), gaps };
  };
  test('the Office has one day shift of 72 and a watch of two 8 h shifts that close the day', () => {
    expect(unit().staffPerShift).toBe(72);
    expect(unit().shifts).toEqual([{ from: day(9, 30), to: day(17, 30) }]);
    expect(unit().watch).toEqual([{ from: day(17, 30), to: day(1, 30) }, { from: day(1, 30), to: day(9, 30) }]);
    expect(unit().official).toBe('official-entrance');
  });
  test('exact counts: 66 clerks, 4 guards, one councillor, one driver; a watch of 2 guards on each watch shift', () => {
    const { plans, gaps } = plan(fix), count = (i: number, p: string) => plans[i]!.slots.filter(s => s.position === p).length;
    expect(gaps).toEqual([]);
    expect(plans.map(p => p.slots.length)).toEqual([72, 2, 2]);
    expect(['clerk', 'guard', 'councillor', 'official-driver'].map(p => count(0, p))).toEqual([66, 4, 1, 1]);
    expect(plans.slice(1).map(p => p.watch)).toEqual([true, true]);
    expect(plans[1]!.slots.every(s => s.position === 'guard')).toBe(true);
  });
  test('posts: of the four guards one stands the public door, one the service door, two are inside; the watch\'s two stand the same two', () => {
    const { plans } = plan(fix), posts = (i: number) => plans[i]!.slots.filter(s => s.position === 'guard').map(s => s.post).sort();
    expect(posts(0)).toEqual(['door', 'inside', 'inside', 'official']);
    expect(posts(1)).toEqual(['door', 'official']);
  });
  test('a position with no count, posts or watch is shared out by weight as it always was', () => {
    const { plans } = plan(r013);
    expect(plans[0]!.slots.length).toBe(72);
    expect(plans.slice(1).every(p => p.watch && p.slots.length === 0)).toBe(true); // a row that names no watch makes nobody for the watch shifts
  });
  test('exact counts that ask for more than the shift has are a gap, not a silent cut', () => {
    const c = clone(fix.cast), r = c.rows.find(x => x.occupation === 'records-office')!;
    r.positions.find(p => p.position === 'clerk')!.count = 70;
    const gaps = expand({ ...fix, cast: c }).gaps;
    expect(gaps.some(g => /exact counts ask for \d+ of 72/.test(g))).toBe(true);
  });
  test('the place a person works: inside people are counted at the side their position names', () => {
    const u = unit(), row = rowOf(fix);
    expect(workPlaceOf(u, row, 'clerk', 'inside')).toBe('office-entrance');
    expect(workPlaceOf(u, row, 'guard', 'inside')).toBe('official-entrance');
    expect(workPlaceOf(u, row, 'guard', 'door')).toBe('office-entrance');
    expect(workPlaceOf(u, row, 'guard', 'official')).toBe('official-entrance');
    expect(workPlaceOf(u, row, 'official-driver', 'bay')).toBe('office-bay');
    expect(workPlaceOf(u, row, 'councillor', 'inside')).toBe('official-entrance');
  });
});

describe('the Handover fix', () => {
  test('the fixture expands without a gap and the people checker passes every rule', () => {
    expect(fixGen.gaps).toEqual([]);
    const r = checkPeople(fix, w.mission, w.plan, fixGen);
    expect(r.checks.filter(c => !c.ok).map(c => `${c.id} ${c.detail}`)).toEqual([]);
    expect(fixGen.people.persons.length).toBe(130); // 126 and the watch's four
  });
  test('M-2 casts all four roles, with the playability conditions (alone, tied, leaves, waits)', () => {
    const c = m2(fixGen.people);
    expect(c.detail).toBeDefined();
    expect(c.ok, c.detail).toBe(true);
    expect(c.value).toMatch(/^waitress p-cafe-\d+, driver p-records-office-\d+, guard p-records-office-\d+, councillor p-records-office-\d+$/);
  });
  test('the promoted cast (whatever is promoted now; r016 at the lovers) plays the Handover: M-2 casts all four roles and the people checker passes', () => {
    const c = m2(promGen.people);
    expect(c.ok, c.detail).toBe(true);
    expect(checkPeople(promoted, w.mission, w.plan, promGen).checks.filter(x => !x.ok).map(x => `${x.id} ${x.detail}`)).toEqual([]);
  });
  test('the r013 cast cannot: the driver has no tie to a councillor, 11 guards stand the door, the councillor goes to a café table', () => {
    const c = m2(r013Gen.people);
    expect(c.ok).toBe(false);
    expect(c.detail).toMatch(/driver: none of the 7/);
    expect(c.detail).toMatch(/at most one guard of records-office at Office entrance in 1:52-5:30: 11 at 17:26:22/);
    expect(c.detail).toMatch(/councillor: none of the 11 who fit .* has a routine that reaches Meet driver by 6:10/);
  });

  test('one guard at the public door every minute from 17:20 to 17:35: the day\'s until 17:30, the watch\'s from 17:30', () => {
    const at = (t: number) => by(fixGen.people, p => p.work?.role === 'guard' && p.work.place === 'office-entrance' && p.work.post === 'door' && stopAt(p, t)?.place === 'office-entrance' && stopAt(p, t)?.doing === 'work').map(p => p.id);
    for (let t = day(17, 20); t <= day(17, 35); t++) expect(at(t), hm(t)).toHaveLength(1);
    expect(at(day(17, 29))[0]).not.toBe(at(day(17, 30))[0]);
    const out = by(fixGen.people, p => p.id === at(day(17, 29))[0])[0]!, inn = by(fixGen.people, p => p.id === at(day(17, 30))[0])[0]!;
    expect(out.work!.shift).toEqual({ from: day(9, 30), to: day(17, 30) });
    expect(inn.work!.shift).toEqual({ from: day(17, 30), to: day(1, 30) });
    // the outgoing guard crosses to a café seat; the incoming one comes from the café (his stop before the shift is at its customers' side, 17:13 to 17:28)
    expect(stopAt(out, day(17, 33))!.doing).toBe('evening');
    expect(w.scope.cafeSide).toContain(stopAt(out, day(17, 33))!.place);
    const pre = inn.routine.stops.find(s => s.doing === 'pre-shift')!;
    expect(w.scope.cafeSide).toContain(pre.place);
    expect([pre.from, pre.to]).toEqual([day(17, 13), day(17, 28)]);
  });
  test('the visible people at the Office at 17:27: the door guard, the service door guard and the driver at the bay; the rounds are inside', () => {
    const shown = by(fixGen.people, p => !!p.work && p.work.unit === 'records-office' && p.work.post !== 'inside' && stopAt(p, day(17, 27))?.doing === 'work');
    expect(shown.map(p => `${p.work!.role}@${p.work!.place}`).sort()).toEqual(['guard@office-entrance', 'guard@official-entrance', 'official-driver@office-bay']);
    const rounds = role(fixGen.people, 'guard').filter(p => p.work!.post === 'inside' && p.work!.shift.from === day(9, 30));
    expect(rounds.map(p => p.work!.place)).toEqual(['official-entrance', 'official-entrance']); // counted at the service side, so the public door's `alone` holds
  });
  test('Vale: arrives by official car, works inside at the official entrance, is in the car at the bay at 17:30, then leaves the sheet; never at a table or on the tram', () => {
    const [vale] = role(fixGen.people, 'councillor');
    expect(role(fixGen.people, 'councillor')).toHaveLength(1);
    expect(vale!.name.endsWith(' Vale')).toBe(true);
    expect(vale!.routine.variant).toBe('official-car');
    expect(vale!.home).toEqual({ plot: 'beyond', place: 'beyond' });
    expect(vale!.work!.place).toBe('official-entrance');
    expect(stopAt(vale!, day(9, 29))).toMatchObject({ place: 'office-bay', doing: 'commute' });
    expect(stopAt(vale!, day(17, 29))).toMatchObject({ place: 'official-entrance', doing: 'work' });
    expect(stopAt(vale!, day(17, 30))).toMatchObject({ place: 'office-bay', doing: 'commute' });
    expect(stopAt(vale!, day(17, 31))).toMatchObject({ place: 'office-bay', doing: 'commute' });
    expect(stopAt(vale!, day(17, 32))).toMatchObject({ place: 'beyond', doing: 'home' });
    expect(ate(vale!, day(17, 29), day(18, 30)).every(s => !/table|stand|seat/.test(s.place))).toBe(true);
    expect(fixGen.people.places.find(p => p.id === 'office-bay')!.kind).toBe('bay');
  });
  test('the driver is the councillor\'s: a workmate tie both ways, at the bay without a break from before the chime to the car leaving', () => {
    const [drv] = role(fixGen.people, 'official-driver'), [vale] = role(fixGen.people, 'councillor');
    expect(role(fixGen.people, 'official-driver')).toHaveLength(1);
    expect(drv!.ties).toContainEqual({ kind: 'workmate', with: vale!.id });
    expect(vale!.ties).toContainEqual({ kind: 'workmate', with: drv!.id });
    expect(drv!.need).toBe('cash');
    for (let t = day(17, 24); t <= day(17, 31); t++) expect(stopAt(drv!, t)?.place, hm(t)).toBe('office-bay');
    expect(stopAt(drv!, day(17, 32))!.place).toBe('beyond');
  });
  test('the door guard is a regular of a server who is on at his arrival at the café before the shift, and of one who is on when he comes after it (the afternoon\'s)', () => {
    const door = by(fixGen.people, p => p.work?.role === 'guard' && p.work.post === 'door' && p.work.shift.from === day(9, 30))[0]!;
    const servers = door.ties.filter(t => t.kind === 'regular').map(t => fixGen.people.persons.find(p => p.id === t.with)!);
    expect(servers).toHaveLength(2);
    expect(servers.every(s => s.work!.unit === 'cafe' && s.work!.position === 'server')).toBe(true);
    const pre = door.routine.stops.find(s => s.doing === 'pre-shift')!, eve = door.routine.stops.find(s => s.doing === 'evening')!;
    const on = (p: Person, t: number) => (p.work!.shift.from <= p.work!.shift.to ? p.work!.shift.from <= t && t < p.work!.shift.to : t >= p.work!.shift.from || t < p.work!.shift.to);
    expect(servers.some(s => on(s, pre.from))).toBe(true);
    expect(servers.some(s => on(s, eve.from) && s.work!.shift.from === day(12))).toBe(true); // the 12:00 to 18:00 shift, which holds Inez
  });
  test('the watch\'s door guard is a regular of the afternoon\'s server too (his stop before the shift is at 17:13)', () => {
    const night = by(fixGen.people, p => p.work?.role === 'guard' && p.work.post === 'door' && p.work.shift.from === day(17, 30))[0]!;
    const tied = night.ties.filter(t => t.kind === 'regular').map(t => fixGen.people.persons.find(p => p.id === t.with)!);
    expect(tied.length).toBeGreaterThan(0);
    expect(tied.some(s => s.work!.shift.from === day(12))).toBe(true);
  });
  test('the watch has no café to stop at after it closes: its second shift starts at home', () => {
    const late = by(fixGen.people, p => p.work?.role === 'guard' && p.work.shift.from === day(1, 30));
    expect(late).toHaveLength(2);
    expect(late.every(p => !p.routine.stops.some(s => s.doing === 'pre-shift'))).toBe(true);
  });

  // a failing example for each condition M-2 reads, made by editing the people (the check bites)
  test('M-2 fails when a second guard stands the public door', () => {
    const p = clone(fixGen.people), g = role(p, 'guard').find(x => x.work!.post === 'official' && x.work!.shift.from === day(9, 30))!;
    g.work!.post = 'door'; g.work!.place = 'office-entrance'; for (const s of g.routine.stops) if (s.doing === 'work' || s.doing === 'meal') s.place = 'office-entrance';
    expect(m2(p).detail).toMatch(/at most one guard of records-office at Office entrance/);
  });
  test('M-2 fails when the driver has no workmate tie to the councillor', () => {
    const p = clone(fixGen.people), [d] = role(p, 'official-driver'), [v] = role(p, 'councillor');
    d!.ties = d!.ties.filter(t => t.with !== v!.id); v!.ties = v!.ties.filter(t => t.with !== d!.id);
    expect(m2(p).detail).toMatch(/driver: .*workmate tie/);
  });
  test('M-2 fails when the door guard has no regular tie to a server on the afternoon shift', () => {
    const p = clone(fixGen.people), g = role(p, 'guard').find(x => x.work!.post === 'door' && x.work!.shift.from === day(9, 30))!;
    g.ties = g.ties.filter(t => t.kind !== 'regular' || p.persons.find(q => q.id === t.with)!.work!.shift.from !== day(12));
    expect(m2(p).detail).toMatch(/guard: .*regular tie/);
  });
  test('M-2 fails when the councillor\'s next stop is a café table, not the bay', () => {
    const p = clone(fixGen.people), [v] = role(p, 'councillor'), s = v!.routine.stops.find(x => x.place === 'office-bay' && x.from === day(17, 30))!;
    s.place = 'cafe-stand-00'; s.doing = 'evening';
    expect(m2(p).detail).toMatch(/councillor: .*reaches Meet driver/);
  });
  test('M-2 fails when the driver is gone from the bay when the councillor arrives (in place to the chime, then away)', () => {
    const p = clone(fixGen.people), [d] = role(p, 'official-driver'), s = d!.routine.stops.find(x => x.place === 'office-bay' && x.doing === 'commute' && x.from === day(17, 30))!;
    s.place = 'beyond'; s.doing = 'home';
    expect(m2(p).detail).toMatch(/driver: .*waits at .* for councillor/);
  });
  test('M-2 fails when the driver is off the bay inside the window', () => {
    const p = clone(fixGen.people), [d] = role(p, 'official-driver'), s = d!.routine.stops.find(x => x.doing === 'work' && x.to === day(17, 30))!;
    s.to = day(17, 20);
    expect(m2(p).detail).toMatch(/driver: nobody is a official-driver/);
  });

  test('P-4 counts the watch: a person missing from it fails', () => {
    const p = clone(fixGen.people); p.persons = p.persons.filter(x => !(x.work?.role === 'guard' && x.work.shift.from === day(17, 30)));
    expect(failed(p)).toContain('P-4');
  });
  test('P-5: sitting in the official car at its bay is not standing in the zone; any other stop there off shift is', () => {
    expect(failed(fixGen.people)).not.toContain('P-5');
    const p = clone(fixGen.people), [v] = role(p, 'councillor'), s = v!.routine.stops.find(x => x.place === 'office-bay' && x.doing === 'commute' && x.from === day(17, 30))!;
    s.doing = 'evening';
    expect(failed(p)).toContain('P-5');
  });
  test('P-1: the hop from the service door to the bay needs no minute, a longer one still does', () => {
    expect(failed(fixGen.people)).not.toContain('P-1');
    const p = clone(fixGen.people), g = role(p, 'guard').find(x => x.work!.post === 'door' && x.work!.shift.from === day(9, 30))!, s = g.routine.stops.find(x => x.doing === 'evening')!;
    s.from = s.from - 2; // the café is a walk of two minutes from the door: one minute is not enough
    g.routine.stops.find(x => x.doing === 'work' && x.to === day(17, 30))!.to = s.from - 1;
    expect(failed(p)).toContain('P-1');
  });
});

describe('the staffing vocabulary in the cast schema', () => {
  test('a row without it still parses (absent means what it meant)', () => {
    expect(Cast.safeParse(read('contracts/people/harbour-district.cast.json')).success).toBe(true);
  });
  test('the schema rejects what it must', () => {
    const bad = (edit: (c: any) => void) => { const c = clone(fix.cast) as any; edit(c); return Cast.safeParse(c).success; };
    expect(bad(() => {})).toBe(true);
    expect(bad(c => { c.rows[1].positions[1].posts[0].post = 'roof'; })).toBe(false);
    expect(bad(c => { c.rows[1].positions[1].count = 0; })).toBe(false);
    expect(bad(c => { c.rows[1].positions[1].watch = { count: 9 }; })).toBe(false);
    expect(bad(c => { c.rows[1].routines[3].commute = 'boat'; })).toBe(false);
    expect(bad(c => { c.rows[1].ties[2].at = 'lunch'; })).toBe(false);
  });
  test('an aimed tie to a position nobody holds makes no tie (the pool is empty)', () => {
    const c = clone(fix.cast), r = c.rows.find(x => x.occupation === 'records-office')!;
    r.ties.find(t => t.with === 'councillor')!.with = 'watcher'; // the Office has no watcher
    const g = expand({ ...fix, cast: c });
    const [d] = role(g.people, 'official-driver');
    expect(d!.ties.every(t => g.people.persons.find(p => p.id === t.with)!.work?.role !== 'councillor')).toBe(true);
  });
});

// Live run r014 (the corrected tables, 2026-10-15) was rejected in three rounds; round 1's faults were the table's, rounds 2 and 3 were the generator's: a busy square that
// double-booked a chair, and a day guard who drew the variant made for the night watch and so had no café to be a regular of. The run's own tables are the fixtures (the run
// record is committed): they must now expand clean and play the Handover.
/** The mission as r014 and the runs before the lovers' canon saw it: the waitress needs no lover tie and no secret (their tables carry no lover pattern; brief 011). */
const unlovedMission = () => {
  const m = JSON.parse(texts.mission) as { roles: { tiedTo?: { kind: string }[]; secret?: string }[] };
  for (const r of m.roles) { if (r.tiedTo) r.tiedTo = r.tiedTo.filter(t => t.kind !== 'lover'); delete r.secret; }
  return m;
};
describe('generator gaps found by r014', () => {
  let wo: World; // the world with the mission as r014 saw it (casting, and so the pinned names, follow the mission)
  beforeAll(() => { wo = world({ ...texts, mission: JSON.stringify(unlovedMission()) }, 'civic-block'); });
  const run = (n: number) => Cast.parse(JSON.parse(readFileSync(join(root, `runs/levels/harbour-district/r014/round-${n}/pulse.json`), 'utf8')).cast);
  const checks = (c: Ctx, g: ReturnType<typeof expand>) => checkPeople(c, wo.mission, w.plan, g).checks.filter(x => !x.ok).map(x => `${x.id} ${x.detail}`);
  for (const n of [2, 3]) test(`the tables of r014 round ${n} expand without a fault and play the Handover (round 2: P-0 and P-13, round 3: P-6)`, () => {
    const c = wo.ctx(run(n)), g = expand(c);
    expect(g.gaps).toEqual([]);
    expect(checks(c, g)).toEqual([]);
    expect(m2(g.people, unlovedMission()).ok, m2(g.people, unlovedMission()).detail).toBe(true);
  });
  test('round 2: the one person with no seat at 13:41 eats on site, and is noted, not faulted; nobody holds a place another holds', () => {
    const g = expand(w.ctx(run(2)));
    // since brief 014 the residents' visits to the café share the lunch hour's chairs, so others are noted too; the person of this test is still among them
    expect(g.notes).toContain('p-records-office-72: no free seat or standing point for the stop at 13:41; the stop is not made');
    const p = g.people.persons.find(x => x.id === 'p-records-office-72')!, meal = p.routine.stops.find(s => s.doing === 'meal')!;
    expect(meal.place).toBe(p.work!.place);
  });
  test('a square with more diners than places never double-books: the rest eat on site', () => {
    const cast = clone(fix.cast), r = cast.rows.find(x => x.occupation === 'records-office')!;
    for (const v of r.routines) if (v.positions?.includes('clerk')) v.meal = 'square'; // all 66 clerks to 48 places
    const c = wo.ctx(cast), g = expand(c);
    expect(g.gaps).toEqual([]);
    expect(checks(c, g)).toEqual([]);
    expect(g.notes.length).toBeGreaterThan(0);
    const held = new Map<string, number>();
    for (const p of g.people.persons) for (const s of p.routine.stops) if (p.work && /seat|stand/.test(s.place) && s.doing === 'meal') held.set(s.place, (held.get(s.place) ?? 0) + 1);
    expect(Math.max(...held.values())).toBeLessThanOrEqual(2); // the same place at two different times, never at once (P-13 holds the times)
  });
  test('a person whose tie names a stop is drawn only among the variants that make it: a night-watch variant with a heavy weight does not take the day door guard\'s café', () => {
    const cast = clone(fix.cast), r = cast.rows.find(x => x.occupation === 'records-office')!;
    r.routines.push({ ...r.routines[0]!, id: 'guard-night', weight: 100, preShift: 'none', evening: 'home', positions: ['guard'] });
    r.routines = r.routines.filter(v => v.id !== 'staff-tram'); // the row keeps at most 4 variants
    const c = { ...fix, cast }, g = expand(c);
    expect(g.gaps).toEqual([]);
    const guards = role(g.people, 'guard');
    expect(guards.filter(p => p.work!.shift.from === day(9, 30)).every(p => p.routine.variant === 'guard-cafe-both-ends')).toBe(true);
    expect(guards.filter(p => p.work!.shift.from === day(17, 30)).every(p => p.routine.variant === 'guard-cafe-both-ends')).toBe(true); // his stop before the shift is open to him at 17:13
    expect(m2(g.people).ok, m2(g.people).detail).toBe(true);
  });
  test('a tie the day cannot carry is said plainly (P-0), not left to P-6: no variant makes the stop the pattern names', () => {
    const cast = clone(fix.cast), r = cast.rows.find(x => x.occupation === 'records-office')!;
    r.routines.find(v => v.id === 'guard-cafe-both-ends')!.evening = 'home';
    const g = expand({ ...fix, cast });
    expect(g.gaps.some(x => /carries a regular tie with a server at evening, but its day \(guard-cafe-both-ends\) has no such stop/.test(x))).toBe(true);
  });
  test('a stop the day cannot make is no fault: the 01:30 watch is not asked for a stop before its shift (the café is shut)', () => {
    const g = expand(fix);
    expect(g.gaps).toEqual([]);
    expect(role(g.people, 'guard').filter(p => p.work!.shift.from === day(1, 30)).every(p => !p.routine.stops.some(s => s.doing === 'pre-shift'))).toBe(true);
  });
});
