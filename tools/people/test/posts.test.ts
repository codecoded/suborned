// A post is one person's (Pulse, brief 067 L; Keel and Suture, Sky's 0.2.122 play: the "x2" over Vale's car was two to four guards on one point): the Records Office's public door and
// official entrance each have waiting points beside them; whoever relieves a post waits at one for the minute before her shift, and a post a cast person holds through the mission (the
// door guard, whom the Handover plays by his role past his shift's end) is never taken: its relief stands beside it for her shift. Held on the promoted cast (r016) and on the engine's own file.
import { beforeAll, describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { Cast } from '../../../contracts/people/cast.schema';
import type { People } from '../../../contracts/people/people.schema';
import { checkPeople } from '../check';
import { drawn } from '../day';
import { expand, type Ctx } from '../expand';
import { readTexts, world, type World } from '../inputs';
import { earlyMin, heldPlaces, PACE_SPREAD_PCT } from '../posts';
import { POST_STAND } from '../seats';
import { inPoly, type V } from '../../plan/geom';

const root = join(import.meta.dir, '..', '..', '..');
let w: World, ctx: Ctx, gen: ReturnType<typeof expand>;
const failed = (c: Ctx, people: People) => checkPeople(c, w.mission, w.plan, { people, gaps: [] }).checks.filter(x => !x.ok);
const stands = () => w.scope.places.filter(p => p.kind === 'stand' && p.beside);
const hm = (t: number) => `${Math.floor(t / 60)}:${String(t % 60).padStart(2, '0')}`;
const day = (h: number, m: number) => h * 60 + m;
beforeAll(async () => {
  w = world(await readTexts('contracts/world/harbour-district.plan.json'), 'civic-block');
  ctx = w.ctx(Cast.parse(JSON.parse(readFileSync(join(root, 'contracts/people/harbour-district.cast.json'), 'utf8')))); gen = expand(ctx);
});

describe('the waiting points beside the Office\'s doors', () => {
  test('each staffed door of the civic employer has two, beside it along its building\'s face, within reach', () => {
    const office = w.scope.units.find(u => u.kind === 'civic')!;
    for (const door of [office.door, office.official!]) {
      const here = stands().filter(s => s.beside === door), at = w.scope.places.find(p => p.id === door)!.at as V;
      expect(here.map(s => s.id)).toEqual([`${door}-stand-00`, `${door}-stand-01`]);
      for (const s of here) {
        const d = Math.hypot(s.at[0] - at[0], s.at[1] - at[1]);
        expect(d).toBeGreaterThanOrEqual(POST_STAND.spacing - 0.01); expect(d).toBeLessThanOrEqual(POST_STAND.reach + 0.01);
        expect(s.plot).toBe(w.scope.places.find(p => p.id === door)!.plot);
      }
      // one each side of the door, on a line through it (the building's face)
      const [a, b] = here.map(s => [s.at[0] - at[0], s.at[1] - at[1]]) as [number[], number[]];
      expect(a![0]! * b![0]! + a![1]! * b![1]!).toBeLessThan(0);
      expect(Math.abs(a![0]! * b![1]! - a![1]! * b![0]!)).toBeLessThan(0.01);
      expect(w.scope.postStands[door]!.standing).toEqual(here.map(s => s.id));
    }
  });
  test('they keep the door\'s own keep-clear zone, no other zone and no bay; the café\'s standing points are untouched', () => {
    for (const s of stands()) {
      const door = w.scope.places.find(p => p.id === s.beside)!, own = w.scope.zones.filter(z => z.test(door.at as V)).map(z => z.id);
      expect(own.length).toBeGreaterThan(0);
      for (const z of w.scope.zones) if (z.test(s.at as V)) expect(own, `${s.id} is in ${z.id}`).toContain(z.id);
      for (const [bay, poly] of Object.entries(w.scope.bayPolys)) expect(inPoly(s.at as V, poly), `${s.id} is in ${bay}`).toBe(false);
    }
    expect(w.scope.stands.every(id => !w.scope.places.find(p => p.id === id)!.beside)).toBe(true);
    expect(w.scope.cafeSide.some(id => id.includes('entrance-stand'))).toBe(false);
  });
  test('the checker passes the promoted cast, P-13 and P-16 among the rules', () => {
    expect(failed(ctx, gen.people).map(c => `${c.id} ${c.detail}`)).toEqual([]);
    const r = checkPeople(ctx, w.mission, w.plan, gen).checks;
    expect(r.find(c => c.id === 'P-16')!.ok).toBe(true);
    expect(Number(r.find(c => c.id === 'P-16')!.value!.split(' ')[0])).toBeGreaterThan(3); // the Office's two doors are counted now (it was three places: the café's door and the kiosks')
  });
});

describe('who waits where', () => {
  const onStands = () => gen.people.persons.filter(p => p.routine.stops.some(s => stand(s.place)?.beside));
  test('only the people who stand a post of the Office\'s door or official entrance are given a waiting point; nobody else\'s day moved', () => {
    for (const p of onStands()) { expect(p.work?.employer).toBe('records-office'); expect(['door', 'official']).toContain(p.work!.post); expect(p.position).toBe('guard'); }
    expect(onStands().length).toBe(gen.people.persons.filter(p => p.work?.employer === 'records-office' && (p.work.post === 'door' || p.work.post === 'official')).length);
    expect(gen.gaps).toEqual([]);
  });
  test('the official entrance: the watch\'s guard waits a minute beside the door (17:29), steps on at the chime (17:30), the day\'s guard is on it until then; never two on the post', () => {
    const post = 'official-entrance', pers = gen.people.persons.filter(p => p.work?.post === 'official' && p.work.shift.from === day(17, 30))[0]!;
    const out = gen.people.persons.filter(p => p.work?.post === 'official' && p.work.shift.to === day(17, 30))[0]!;
    const wait = pers.routine.stops.find(s => stand(s.place)?.beside === post)!;
    expect([wait.from, wait.to, wait.doing]).toEqual([day(17, 29), day(17, 30), 'work']);
    expect(pers.routine.stops.find(s => s.place === post && s.from === day(17, 30))).toBeDefined();
    expect(pers.work!.place).toBe(post);
    expect(out.routine.stops.some(s => s.place === post && s.from < day(17, 30) && s.to === day(17, 30))).toBe(true);
    expect(wait.to - wait.from).toBe(1);
  });
  test('the public door: the cast guard holds it through the mission, so its relief stands beside it for her whole shift, from the chime', () => {
    const post = 'office-entrance', cast = gen.people.persons.find(p => p.id === 'p-records-office-01')!;
    expect(cast.work!.post).toBe('door');
    const held = heldPlaces(w.scope, ctx.casting);
    expect([...held.keys()].sort()).toContain(post);
    const relief = gen.people.persons.filter(p => p.work?.post === 'door' && p.work.unit === cast.work!.unit && p.work.shift.from === day(17, 30))[0]!;
    expect(stand(relief.work!.place).beside).toBe(post); // her work place is the point she stands at
    const work = relief.routine.stops.filter(s => s.doing === 'work' || s.doing === 'meal');
    expect(work.length).toBeGreaterThan(0);
    for (const s of work) expect(stand(s.place).beside).toBe(post); // never on the post itself
    expect(work.find(s => s.from <= day(17, 30) && day(17, 30) < s.to)).toBeDefined();
    expect(drawn(relief, work[0]!)).toBe(true);
    // the day's guard (the cast guard) stands the post itself
    expect(cast.routine.stops.some(s => s.place === post && s.doing === 'work')).toBe(true);
  });
  test('the chosen point is the first free: two reliefs at one post never share one at the same minutes', () => {
    const by = new Map<string, { who: string; from: number; to: number }[]>();
    for (const p of gen.people.persons) for (const s of p.routine.stops) if (stand(s.place)?.beside) by.set(s.place, [...(by.get(s.place) ?? []), { who: p.id, from: s.from, to: s.to }]);
    expect(by.size).toBeGreaterThanOrEqual(2);
    for (const [place, list] of by) for (let i = 0; i < list.length; i++) for (let j = i + 1; j < list.length; j++) {
      const a = list[i]!, b = list[j]!;
      expect(a.from < b.to && b.from < a.to, `${a.who} and ${b.who} at ${place} ${hm(a.from)}-${hm(a.to)} / ${hm(b.from)}-${hm(b.to)}`).toBe(false);
    }
  });
});

const stand = (id: string) => w.scope.places.find(p => p.id === id)!;
describe('P-16 holds the posts', () => {
  const without = () => { const bare = { ...ctx, scope: { ...ctx.scope, postStands: {} } }; return { bare, g: expand(bare) }; };
  test('without waiting points the relief is on the official entrance with the guard she relieves from 5:11, and on the cast guard\'s own post for the mission: P-16 fails on both doors', () => {
    const { bare, g } = without();
    const p16 = failed(bare, g.people).find(c => c.id === 'P-16');
    expect(p16, 'P-16 should fail').toBeDefined();
    expect(p16!.detail).toMatch(/both drawn at official-entrance/);
    expect(p16!.detail).toMatch(/holds it through the mission/);
    expect(p16!.detail).toMatch(/office-entrance/);
  });
  test('the early walk is counted: a leg timed 3 min ends up to the crowd pace\'s share early, and the share is the rulebook\'s', () => {
    expect(PACE_SPREAD_PCT).toBe(12);
    expect(earlyMin(3)).toBeCloseTo(3 * 12 / 112, 6);
    expect(earlyMin(0)).toBe(0);
  });
  test('two people on a post at the minute of the change fail, though their stops only touch: take the relief\'s waiting minute away and she is on the post early', () => {
    const people: People = JSON.parse(JSON.stringify(gen.people));
    const relief = people.persons.find(p => p.work?.post === 'official' && p.work.shift.from === day(17, 30))!;
    expect(relief.routine.stops.some(s => stand(s.place)?.beside)).toBe(true);
    relief.routine.stops = relief.routine.stops.filter(s => !stand(s.place)?.beside);
    const p16 = failed(ctx, people).find(c => c.id === 'P-16');
    expect(p16?.detail ?? '').toMatch(new RegExp(`${relief.id} and p-records-office-\\d+ are both drawn at official-entrance|p-records-office-\\d+ and ${relief.id} are both drawn at official-entrance`));
  });
  test('a cast person\'s hold is the mission\'s window from its start, a mission\'s longest (life.json clock.mission.windowMin)', () => {
    expect(w.scope.missionMin).toBe(12);
  });
});

describe('the engine\'s file (sim/data/handover.people.json) says the same', () => {
  const file = JSON.parse(readFileSync(join(root, 'sim/data/handover.people.json'), 'utf8')) as { startsAt: number; places: Record<string, number[]>; faces: Record<string, unknown>; people: { id: string; job: string; stops: [string, number, number, number][] }[] };
  const site = JSON.parse(readFileSync(join(root, 'sim/data/handover.site.json'), 'utf8')) as { cast: Record<string, { id: string }> };
  const posts = ['office-entrance', 'official-entrance'];
  test('the waiting points are places of it, and face the kerb', () => {
    for (const id of ['office-entrance-stand-00', 'office-entrance-stand-01', 'official-entrance-stand-00', 'official-entrance-stand-01']) { expect(file.places[id], id).toBeDefined(); expect(file.faces[id], id).toBe('kerb'); }
  });
  test('at no second of a mission does anyone stand at a post with another, counting the early walk, and nobody but the cast guard stands the public door through the mission', () => {
    const early = (leg: number) => (leg * PACE_SPREAD_PCT) / (100 + PACE_SPREAD_PCT), win = 12 * 60;
    const held: Record<string, { who: string; from: number; to: number }[]> = {};
    for (const p of file.people) p.stops.forEach(([place, from, to, out], i, st) => {
      if (!posts.includes(place) || !out) return;
      const prev = st[(i + st.length - 1) % st.length]!, leg = prev[0] === 'beyond' || prev[0] === place ? 0 : from - prev[2];
      (held[place] ??= []).push({ who: p.id, from: from - early(leg), to });
    });
    const guard = site.cast.guard!.id;
    for (const [place, list] of Object.entries(held)) {
      const mine = list.filter(x => x.to > -60 && x.from < win); // the mission and the minute before it (seconds from 0:00)
      for (let i = 0; i < mine.length; i++) for (let j = i + 1; j < mine.length; j++) {
        const a = mine[i]!, b = mine[j]!;
        if (a.who !== b.who) expect(a.from < b.to && b.from < a.to, `${a.who} ${a.from.toFixed(0)}-${a.to} and ${b.who} ${b.from.toFixed(0)}-${b.to} at ${place}`).toBe(false);
      }
      if (place === 'office-entrance') for (const x of mine) if (x.who !== guard) expect(x.from < win && x.to > 0, `${x.who} at ${place} while ${guard} holds it`).toBe(false);
    }
  });
});
