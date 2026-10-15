// Brief 011: the lover tie and the secret in generation (Motive briefs/011/motive-lovers.md; Pulse). The pair is made from the cast table's own aimed pattern, never invented
// by a mission; the checker holds both (P-14, P-15); a pinned older rulebook without them still reads its tables as it always did.
import { beforeAll, describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { Cast } from '../../../contracts/people/cast.schema';
import { People, type Person } from '../../../contracts/people/people.schema';
import { checkPeople } from '../check';
import { expand, routineMeet, type Ctx } from '../expand';
import { readTexts, world, type World } from '../inputs';
import { requirements } from '../review';
import { legacyRules } from './legacy';
import { r013Cast } from './r013';
import { legacySecretShare, withSecrets } from '../secrets';

const root = join(import.meta.dir, '../../..');
let w: World, ctx: Ctx, gen: ReturnType<typeof expand>, fix: Cast;
const clone = <T>(x: T): T => JSON.parse(JSON.stringify(x));
const failed = (people: People, c: Ctx = ctx) => checkPeople(c, w.mission, w.plan, { people, gaps: [] }).checks.filter(x => !x.ok).map(x => x.id);
const loversOf = (ps: readonly Person[]) => ps.filter(p => p.ties.some(t => t.kind === 'lover'));
const byId = (ps: readonly Person[]) => new Map(ps.map(p => [p.id, p]));
beforeAll(async () => {
  w = world(await readTexts('contracts/world/harbour-district.plan.json'), 'civic-block');
  fix = Cast.parse(JSON.parse(readFileSync(join(root, 'tools/people/test/handover-fix.cast.json'), 'utf8')));
  ctx = w.ctx(fix); gen = expand(ctx);
});

describe('the pair is made from the table', () => {
  test('the door guards carry a lover tie to a server and nobody else carries one (the table names it, nothing invents it)', () => {
    const L = loversOf(gen.people.persons), by = byId(gen.people.persons);
    expect(L.length).toBeGreaterThanOrEqual(2);
    for (const p of L) {
      const t = p.ties.filter(x => x.kind === 'lover'); expect(t).toHaveLength(1);
      const q = by.get(t[0]!.with)!;
      expect(q.ties.some(x => x.kind === 'lover' && x.with === p.id)).toBe(true);
      const guard = [p, q].find(x => x.work?.role === 'guard')!, server = [p, q].find(x => x.work?.position === 'server')!;
      expect(guard.work!.post).toBe('door'); expect(server.work!.unit).toBe('cafe');
      expect(routineMeet(w.scope, p, q)).toBe(true);
    }
    expect(L.every(p => ['guard', 'server'].includes((p.work?.role ?? p.work?.position)!))).toBe(true);
  });
  test('the guards on rounds, the clerks and the residents carry none (Tie.posts)', () => {
    for (const p of gen.people.persons) if (p.work?.role === 'guard' && p.work.post !== 'door') expect(p.ties.some(t => t.kind === 'lover')).toBe(false);
  });
  test('a lover is drawn along a regular tie first: the guard\'s lover is a server he is already a regular of', () => {
    const by = byId(gen.people.persons);
    for (const g of gen.people.persons.filter(p => p.work?.role === 'guard' && p.ties.some(t => t.kind === 'lover'))) {
      const l = g.ties.find(t => t.kind === 'lover')!.with;
      if (g.ties.some(t => t.kind === 'regular' && by.get(t.with)?.work?.position === 'server' && by.get(t.with)?.work?.shift.from === by.get(l)?.work?.shift.from)) expect(g.ties.some(t => t.kind === 'regular' && t.with === l)).toBe(true);
    }
  });
  test('a table with no lover pattern makes no lovers, however the mission asks (the stand-in cast)', () => {
    const bare = clone(fix); for (const r of bare.rows) r.ties = r.ties.filter(t => t.kind !== 'lover');
    const c = w.ctx(bare), g = expand(c);
    expect(loversOf(g.people.persons)).toHaveLength(0);
    expect(g.people.persons.every(p => p.secret === null || p.secret === 'record')).toBe(true);
  });
  test('the same inputs give the same bytes', () => expect(JSON.stringify(expand(ctx).people)).toBe(JSON.stringify(gen.people)));
  test('every lover holds the affair, and nobody else does', () => {
    for (const p of gen.people.persons) expect(p.secret === 'affair').toBe(p.ties.some(t => t.kind === 'lover'));
  });
  test('the fixture passes the people checker, P-14 and P-15 among them', () => {
    const r = checkPeople(ctx, w.mission, w.plan, { people: gen.people, gaps: gen.gaps }).checks;
    expect(r.filter(c => !c.ok).map(c => `${c.id}: ${c.detail}`)).toEqual([]);
    expect(r.find(c => c.id === 'P-14')!.value).toMatch(/^\d+ pairs/);
  });
});

describe('the review reads the role\'s lover requirement', () => {
  test('the waitress needs a row that names a lover pattern; the fixture meets it and a table without one does not', () => {
    const met = requirements(ctx, gen.people).find(r => r.id === 'waitress-lover-guard');
    if (!met) return; // a mission whose waitress asks for no lover (an older one) has no such line
    expect(met.ok, met.detail).toBe(true);
    const bare = clone(fix); for (const r of bare.rows) r.ties = r.ties.filter(t => t.kind !== 'lover');
    const bc = w.ctx(bare), none = requirements(bc, expand(bc).people).find(r => r.id === 'waitress-lover-guard')!;
    expect(none.ok).toBe(false); expect(none.detail).toMatch(/no cast row names a lover pattern/);
  });
});

describe('ambient lovers and the share', () => {
  test('a pattern every clerk and server carries is held to the rules\' share of the people, and the checker agrees', () => {
    const c = clone(fix); for (const r of c.rows) if (r.area !== 'records' || r.occupation !== 'resident') r.ties.push({ kind: 'lover', share: 1 });
    const cx = w.ctx(Cast.parse(c)), g = expand(cx), cap = Math.floor(w.rules.ties.kinds.lover!.maxShare! * g.people.persons.length), n = loversOf(g.people.persons).length;
    expect(n).toBeGreaterThan(2); expect(n).toBeLessThanOrEqual(cap);
    const r = checkPeople(cx, w.mission, w.plan, { people: g.people, gaps: g.gaps }).checks;
    expect(r.find(x => x.id === 'P-14')!.ok).toBe(true); expect(r.find(x => x.id === 'P-15')!.ok).toBe(true);
    for (const p of loversOf(g.people.persons)) expect(p.ties.filter(t => t.kind === 'lover')).toHaveLength(1);
  });
  test('a named pattern nobody can be tied to is a gap that says so', () => {
    const c = clone(fix), row = c.rows.find(r => r.occupation === 'records-office')!;
    row.ties = row.ties.map(t => (t.kind === 'lover' ? { ...t, with: 'councillor', at: 'evening' as const } : t));
    expect(expand(w.ctx(Cast.parse(c))).gaps.join('\n')).toMatch(/carries a lover tie with a councillor at evening/);
  });
});

describe('secrets', () => {
  test('the need secret retires: its weight is the row\'s record share, and nobody draws it', () => {
    const base = r013Cast(); // r013 still carries the need `secret` the new rulebook retires; the promoted cast (r016) was made under the rules that retired it
    const moved = withSecrets(base, w.rules);
    for (const r of base.rows) if (r.needs.some(n => n.need === 'secret')) {
      const m = moved.rows.find(x => x.area === r.area && x.occupation === r.occupation)!;
      expect(m.needs.some(n => n.need === 'secret')).toBe(false);
      expect(m.secrets!.find(s => s.secret === 'record')!.share).toBe(legacySecretShare(r));
    }
    expect(withSecrets(moved, w.rules)).toEqual(moved); // idempotent
    const g = expand(w.ctx(base));
    expect(g.people.persons.some(p => p.need === 'secret')).toBe(false);
    expect(g.people.persons.some(p => p.secret === 'record')).toBe(true);
  });
  test('a pinned older rulebook (no secrets) reads its tables as written: no field, no lover, the need as it was', () => {
    const base = r013Cast(); // a table of the older rulebook: it draws the need `secret`, which the promoted cast (r016) does not
    const oc = { ...w.ctx(base), rules: legacyRules(w.rules) }, g = expand(oc);
    expect(g.people.persons.some(p => 'secret' in p)).toBe(false);
    expect(g.people.persons.some(p => p.need === 'secret')).toBe(true);
    expect(failed(g.people, oc).filter(id => ['P-12', 'P-14', 'P-15', 'P-7', 'P-3'].includes(id))).toEqual([]);
  });
});

describe('P-14 and P-15 fail on their examples', () => {
  const lov = () => { const p = clone(gen.people); const a = loversOf(p.persons)[0]!, b = p.persons.find(x => x.id === a.ties.find(t => t.kind === 'lover')!.with)!; return { p, a, b }; };
  test('P-7 and P-14: a lover tie that is not returned fails', () => {
    const { p, b } = lov(); b.ties = b.ties.filter(t => t.kind !== 'lover');
    expect(failed(p)).toContain('P-7');
  });
  test('P-14: two lovers fail', () => {
    const { p, a } = lov(), c = p.persons.find(x => x.id !== a.id && !x.ties.some(t => t.kind === 'lover') && x.ties.length < 8)!;
    a.ties.push({ kind: 'lover', with: c.id }); c.ties.push({ kind: 'lover', with: a.id });
    expect(failed(p)).toContain('P-7');
  });
  test('P-14: a lover who is family fails', () => {
    const { p, a, b } = lov(); a.ties.push({ kind: 'family', with: b.id }); b.ties.push({ kind: 'family', with: a.id });
    expect(checkPeople(ctx, w.mission, w.plan, { people: p, gaps: [] }).checks.find(c => c.id === 'P-14')!.detail).toMatch(/is family/);
  });
  test('P-7 and P-14: a lover met on no routine fails', () => {
    const { p, a, b } = lov(); b.work = a.work ? { ...b.work!, place: 'beyond', unit: b.work!.unit } : b.work; b.routine = { ...b.routine, stops: b.routine.stops.map(s => ({ ...s, place: s.doing === 'home' ? s.place : b.home.place })) };
    a.routine = { ...a.routine, stops: a.routine.stops.map(s => ({ ...s, place: s.doing === 'home' ? s.place : a.home.place })) };
    const r = checkPeople(ctx, w.mission, w.plan, { people: p, gaps: [] }).checks;
    expect(r.find(c => c.id === 'P-14')!.ok).toBe(false);
  });
  test('P-14: more lovers than the share fails', () => {
    const p = clone(gen.people), free = p.persons.filter(x => !x.ties.some(t => t.kind === 'lover') && x.ties.length < 8);
    for (let i = 0; i + 1 < free.length && i < 80; i += 2) { free[i]!.ties.push({ kind: 'lover', with: free[i + 1]!.id }); free[i + 1]!.ties.push({ kind: 'lover', with: free[i]!.id }); free[i]!.secret = free[i + 1]!.secret = 'affair'; }
    expect(checkPeople(ctx, w.mission, w.plan, { people: p, gaps: [] }).checks.find(c => c.id === 'P-14')!.detail).toMatch(/share 0.15 allows/);
  });
  test('P-15: the affair without a lover tie fails', () => {
    const p = clone(gen.people), x = p.persons.find(q => !q.ties.some(t => t.kind === 'lover'))!; x.secret = 'affair';
    expect(failed(p)).toContain('P-15');
  });
  test('P-15: a lover without the affair fails', () => {
    const { p, a } = lov(); a.secret = null;
    expect(failed(p)).toContain('P-15');
  });
  test('P-15: a secret outside the list, a missing field and the retired need fail', () => {
    const p = clone(gen.people); p.persons[0]!.secret = 'shame' as never;
    expect(failed(p)).toContain('P-15');
    const q = clone(gen.people); delete (q.persons[1] as Partial<Person>).secret;
    expect(failed(q)).toContain('P-15');
    const r = clone(gen.people); r.persons[2]!.need = 'secret';
    expect(failed(r)).toContain('P-15');
  });
  test('P-15: a table that names the affair, or a secret outside the list, fails', () => {
    const c = clone(fix); c.rows[0]!.secrets = [{ secret: 'affair', share: 0.1 }, { secret: 'shame', share: 0.1 }];
    const cx = w.ctx(Cast.parse(c)), r = checkPeople(cx, w.mission, w.plan).checks.find(x => x.id === 'P-15')!;
    expect(r.ok).toBe(false); expect(r.detail).toMatch(/never written in a table/); expect(r.detail).toMatch(/shame is not in the rules/);
  });
  test('P-15: a record held by a row that does not name one fails', () => {
    const p = clone(gen.people), x = p.persons.find(q => q.secret === null)!; x.secret = 'record';
    const c = clone(fix); for (const r of c.rows) { r.secrets = undefined; r.needs = r.needs.filter(n => n.need !== 'secret'); }
    const cx = w.ctx(Cast.parse(c));
    expect(checkPeople(cx, w.mission, w.plan, { people: p, gaps: [] }).checks.find(r => r.id === 'P-15')!.detail).toMatch(/holds a record, which its row does not name/);
  });
});
