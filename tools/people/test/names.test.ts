// Brief 089 (Pulse): the city's names from real lists. The promoted cast on the real plan, block, life.json and people rules: every name drawn by the rules
// (P-8), families sharing one name and one country, the five pinned names where the Handover casts them (and their kin under the pinned surname), sex the
// figure's body, and failing examples for each part of P-8. Run: bun test tools/people/test/names.test.ts
import { beforeAll, describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { Cast } from '../../../contracts/people/cast.schema';
import type { People, Person } from '../../../contracts/people/people.schema';
import { checkPeople } from '../check';
import { expand, type Ctx } from '../expand';
import { readTexts, world, type World } from '../inputs';
import { FORMA_WOMEN, HOST_WOMEN } from '../looks';
import { familiesOf, familyForm, familyPool, feminine, parseLists, womansForm } from '../names';

const root = join(import.meta.dir, '..', '..', '..');
type P = Person & { sex: 'f' | 'm'; age: number; origin: string };
let w: World, ctx: Ctx, people: People, persons: P[];
const clone = <T>(x: T): T => JSON.parse(JSON.stringify(x));
const p8 = (pp: People) => checkPeople(ctx, w.mission, w.plan, { people: pp, gaps: [] }).checks.find(c => c.id === 'P-8')!;
const surname = (p: Person) => p.name.split(' ')[1]!;
const man = (s: string, c?: string) => familyForm(ctx.names!, c ?? 'pl', s);
beforeAll(async () => {
  w = world(await readTexts('contracts/world/harbour-district.plan.json'), 'civic-block');
  ctx = w.ctx(Cast.parse(JSON.parse(readFileSync(join(root, 'contracts/people/harbour-district.cast.json'), 'utf8'))));
  people = expand(ctx).people; persons = people.persons as P[];
});

describe('names from the lists (brief 089)', () => {
  test('P-8 holds on the promoted cast; every person has a sex, an age and a country of the rules', () => {
    expect(p8(people).ok).toBe(true);
    for (const p of persons) { expect(['f', 'm']).toContain(p.sex); expect(w.rules.names.origins).toContain(p.origin); expect(p.age).toBeGreaterThanOrEqual(16); }
  });
  test('the five pinned names, on the people the Handover casts (Milena by her id)', () => {
    const by = (id: string) => persons.find(p => p.id === id)!.name;
    expect(by('p-cafe-14')).toBe('Inez Nikolaou'); expect(by('p-cafe-12')).toBe('Milena Rask');
    const role = (r: string) => persons.find(p => p.work?.role === r && w.rules.names.pinned.some(x => x.role === r && p.name.includes(x.name)))!.name;
    expect([role('councillor'), role('guard'), role('official-driver')]).toEqual(['Uros Popovic', 'Arthur Reilly', 'Aleksander Lukac']);
  });
  test('kin share one family name and one country (a Polish woman carries its woman\'s form); a pinned surname is its family\'s', () => {
    const byId = new Map(persons.map(p => [p.id, p])), fams = familiesOf(persons).filter(f => f.length > 1);
    expect(fams.length).toBeGreaterThan(10);
    for (const f of fams) {
      expect(new Set(f.map(id => man(surname(byId.get(id)!), byId.get(id)!.origin))).size).toBe(1);
      expect(new Set(f.map(id => byId.get(id)!.origin)).size).toBe(1);
    }
    let ends = 0, same = 0;
    for (const p of persons) for (const t of p.ties) if (t.kind === 'family') { ends++; if (man(surname(p), p.origin) === man(surname(byId.get(t.with)!), byId.get(t.with)!.origin)) same++; }
    expect(same).toBe(ends); // before brief 089: 2 of 66
    for (const pin of w.rules.names.pinned.filter(x => x.part === 'family')) {
      const carriers = persons.filter(p => surname(p) === pin.name), fam = familiesOf(persons).find(f => f.includes(carriers[0]!.id))!;
      expect(carriers.every(p => fam.includes(p.id))).toBe(true); // only the pinned person and their kin
    }
    expect(feminine('Kowalski')).toBe('Kowalska'); expect(feminine('Wójcik')).toBe('Wójcik');
    const L = ctx.names!; expect([womansForm(L, 'Szczęsny'), womansForm(L, 'Oleksy'), womansForm(L, 'Nowak')]).toEqual(['Szczęsna', 'Oleksy', 'Nowak']);
  });
  test('unrelated families share a surname only about as often as the lists make likely (a real city, less its three commonest names)', () => {
    const fams = familiesOf(persons), byId = new Map(persons.map(p => [p.id, p])), seen = new Map<string, number>();
    for (const f of fams) { const k = `${byId.get(f[0]!)!.origin}/${man(surname(byId.get(f[0]!)!), byId.get(f[0]!)!.origin)}`; seen.set(k, (seen.get(k) ?? 0) + 1); }
    const pairs = [...seen.values()].reduce((n, k) => n + (k * (k - 1)) / 2, 0);
    // expected pairs: for each country, C(families, 2) × Σ p² over its list (less the top 3)
    const L = ctx.names!, by = new Map<string, number>();
    for (const f of fams) by.set(byId.get(f[0]!)!.origin, (by.get(byId.get(f[0]!)!.origin) ?? 0) + 1);
    let expected = 0;
    for (const [c, k] of by) { const xs = familyPool(L, c), t = xs.reduce((n, x) => n + x[1], 0); expected += ((k * (k - 1)) / 2) * xs.reduce((n, x) => n + (x[1] / t) ** 2, 0); }
    expect(pairs).toBeLessThanOrEqual(Math.ceil(expected * 2) + 2);
  });
  test('the given name fits the figure: women\'s figures carry women\'s names; the host\'s list of women is mirrored', () => {
    const cs = readFileSync(join(root, 'unity/Assets/Suborned/Editor/World/People/FigureBuild.cs'), 'utf8');
    const host = /static readonly HashSet<string> Women = new\(\) \{([^}]*)\}/.exec(cs)![1]!.match(/"([^"]+)"/g)!.map(s => s.slice(1, -1));
    expect([...host].sort()).toEqual([...HOST_WOMEN].sort());
    expect(FORMA_WOMEN.some(f => HOST_WOMEN.includes(f))).toBe(false); // the two Forma names that the host does not walk as women (yet)
    expect(persons.find(p => p.id === 'p-cafe-14')!.sex).toBe('f');
    for (const id of ['p-records-office-07', 'p-records-office-01', 'p-records-office-31']) expect(persons.find(p => p.id === id)!.sex).toBe('m');
  });
});

describe('P-8 fails on', () => {
  const edit = (f: (ps: P[]) => void) => { const pp = clone(people); f(pp.persons as P[]); return p8(pp); };
  const loner = () => persons.find(p => familiesOf(persons).find(f => f.includes(p.id))!.length === 1 && p.origin === 'us' && !w.rules.names.pinned.some(x => p.name.includes(x.name)))!;
  test('a top-3 name of the list (Smith)', () => { const id = loner().id; expect(edit(ps => { ps.find(p => p.id === id)!.name = `${ps.find(p => p.id === id)!.name.split(' ')[0]} Smith`; }).detail).toMatch(/Smith is not a drawable family name of us/); });
  test('a banned name (Okafor)', () => { const id = loner().id; expect(edit(ps => { ps.find(p => p.id === id)!.name = `${ps.find(p => p.id === id)!.name.split(' ')[0]} Okafor`; }).ok).toBe(false); });
  test('a given name of another country', () => { const id = loner().id; expect(edit(ps => { ps.find(p => p.id === id)!.name = `Zbigniew ${surname(ps.find(p => p.id === id)!)}`; }).detail).toMatch(/Zbigniew is not a drawable/); });
  test('a man\'s name on a woman\'s figure', () => { const p = persons.find(q => q.sex === 'f' && q.origin === 'us' && !w.rules.names.pinned.some(x => q.name.includes(x.name)))!; expect(edit(ps => { ps.find(q => q.id === p.id)!.sex = 'm'; }).detail).toMatch(/the host dresses them in/); });
  test('kin with two family names', () => {
    const f = familiesOf(persons).find(x => x.length > 1 && x.every(id => !w.rules.names.pinned.some(pin => persons.find(p => p.id === id)!.name.includes(pin.name))))!;
    expect(edit(ps => { const p = ps.find(q => q.id === f[1])!; p.name = `${p.name.split(' ')[0]} ${p.origin === 'us' ? 'Johnson' : 'Zzz'}`; }).detail).toMatch(/family names/);
  });
  test('a pinned given name on a second person', () => { const id = loner().id; expect(edit(ps => { ps.find(p => p.id === id)!.name = `Arthur ${surname(ps.find(p => p.id === id)!)}`; }).detail).toMatch(/also carries the pinned given name Arthur/); });
});

describe('the read-through holds still', () => {
  test('an exclusion moves only the people who drew that name', async () => {
    const t = await readTexts('contracts/world/harbour-district.plan.json'), l = w.rules.names.lists!, victim = persons.find(p => p.origin === 'us' && familiesOf(persons).find(f => f.includes(p.id))!.length === 1 && !w.rules.names.pinned.some(x => p.name.includes(x.name)))!;
    const ex = JSON.parse(t.names![l.exclusions]!); ex.names.push({ full: victim.name, why: 'test' });
    const w2 = world({ ...t, names: { ...t.names!, [l.exclusions]: JSON.stringify(ex) } }, 'civic-block'), c2 = w2.ctx(ctx.cast), after = expand(c2).people.persons;
    const moved = after.filter((p, i) => p.name !== persons[i]!.name).map(p => p.id);
    expect(moved).toEqual([victim.id]);
    expect(parseLists(w2.rules, { ...t.names!, [l.exclusions]: JSON.stringify(ex) }).excluded.full.has(victim.name)).toBe(true);
  });
});
