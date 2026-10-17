// Brief 017 phase 1 (open needs, structure only): the kinds and the catalogue, the records the people draw, and the checker rules N-1..N-8, each with a failing example.
// The proof that nothing moved: the same cast expanded with and without the catalogue gives the same people apart from the record ids.
import { beforeAll, describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { Cast } from '../../../contracts/people/cast.schema';
import { Catalogue, NeedsRules } from '../../../contracts/people/needs.schema';
import type { People } from '../../../contracts/people/people.schema';
import { checkPeople } from '../check';
import { expand, type Ctx } from '../expand';
import { readTexts, readTextsSync, world, type World } from '../inputs';
import { drawNeedRecord, parseNeeds, type Needs } from '../needs';
import { standInCast } from '../standin';

const PLAN = 'contracts/world/harbour-district.plan.json';
let w: World, ctx: Ctx, gen: ReturnType<typeof expand>, promoted: Ctx, pgen: ReturnType<typeof expand>;
const clone = <T>(x: T): T => JSON.parse(JSON.stringify(x));
const strip = (p: People['persons'][number]) => { const { needRecord: _a, secretRecord: _b, ...rest } = p; return rest; };
/** The checks that fail when the catalogue (or the kinds) is changed by `edit`; the people stay as drawn. */
const failedWith = (edit: (n: Needs) => void, people: People = gen.people, c: Ctx = ctx) => {
  const n = clone(c.needs!); edit(n);
  return checkPeople({ ...c, needs: n }, w.mission, w.plan, { people, gaps: [] }).checks.filter(x => !x.ok).map(x => x.id);
};
const record = (n: Needs, id: string) => n.catalogue.needs.find(r => r.id === id)!;
const kindsOf = (people: People) => Object.fromEntries(people.persons.map(p => [p.id, [p.need, p.needRecord]]));

beforeAll(async () => {
  w = world(await readTexts(PLAN), 'civic-block');
  ctx = w.ctx(standInCast(w.scope, w.rules, { lovers: true })); gen = expand(ctx);
  promoted = w.ctx(Cast.parse(JSON.parse(readFileSync('contracts/people/harbour-district.cast.json', 'utf8')))); pgen = expand(promoted);
});

describe('nothing moved', () => {
  test('the files are read: kinds and a catalogue', () => {
    expect(w.needs).toBeDefined();
    expect(Object.keys(w.needs!.rules.kinds).sort()).toEqual(['advancement', 'care', 'cash', 'debt', 'housing', 'kin', 'papers', 'protection']);
    expect(w.needs!.rules.secretKinds).toHaveProperty('affair');
    expect(w.needs!.rules.secretKinds).toHaveProperty('record');
  });
  test('the promoted cast: the same people with and without the catalogue, apart from the record ids', () => {
    const bare = expand({ ...promoted, needs: undefined }).people;
    expect(JSON.stringify(pgen.people.persons.map(strip))).toBe(JSON.stringify(bare.persons));
    expect(pgen.gaps).toEqual([]);
    expect(bare.persons.some(p => 'needRecord' in p || 'secretRecord' in p)).toBe(false);
    for (const k of ['places', 'units', 'knowledge', 'background'] as const) expect(JSON.stringify(pgen.people[k])).toBe(JSON.stringify(bare[k]));
  });
  test('the stand-in cast (every kind offered): the same', () => {
    const bare = expand({ ...ctx, needs: undefined }).people;
    expect(JSON.stringify(gen.people.persons.map(strip))).toBe(JSON.stringify(bare.persons));
    expect(gen.gaps).toEqual([]);
  });
  test('the kinds are the engine\'s own table, less secret: same approaches, help and requirement', () => {
    for (const [k, v] of Object.entries(w.needs!.rules.kinds)) {
      const d = w.rules.needs.list[k]!;
      expect([...v.metBy].sort()).toEqual([...d.metBy].sort()); expect(v.help).toBe(d.help); expect(v.requires).toBe(d.requires);
    }
    expect(Object.keys(w.needs!.rules.kinds).sort()).toEqual(Object.keys(w.rules.needs.list).filter(k => k !== 'secret').sort());
  });
  test('the record ids are the only difference in the written people file', () => {
    const only = JSON.parse(readFileSync('out/people/harbour-district.people.json', 'utf8')) as People;
    if (only.persons.some(p => p.needRecord)) expect(JSON.stringify(only.persons.map(strip))).toBe(JSON.stringify(pgen.people.persons.map(strip)));
  });
});

describe('records', () => {
  test('every person draws one record of their kind; a secret draws one of its kind', () => {
    for (const p of pgen.people.persons) {
      const r = w.needs!.catalogue.needs.find(x => x.id === p.needRecord);
      expect(r?.kind).toBe(p.need);
      if (p.secret) expect(w.needs!.catalogue.secrets.find(x => x.id === p.secretRecord)?.kind).toBe(p.secret); else expect(p.secretRecord).toBeUndefined();
    }
  });
  test('Inez is a Bureau loan (debt, standard), the driver a Lean month (cash, standard)', () => {
    const c = pgen.people.persons, inez = c.find(p => p.id === 'p-cafe-14')!, driver = c.find(p => p.id === 'p-records-office-31')!;
    expect([inez.need, inez.needRecord]).toEqual(['debt', 'bureau-loan']); expect([driver.need, driver.needRecord]).toEqual(['cash', 'lean-month']);
    const labels = Object.fromEntries(w.needs!.catalogue.needs.map(r => [r.id, [r.label, r.size, r.holder]]));
    expect(labels['bureau-loan']).toEqual(['Bureau loan', 'standard', undefined]); expect(labels['lean-month']).toEqual(['Lean month', 'standard', undefined]);
  });
  test('the draw is seeded by the person: the same bytes twice; a stream no other draw shares', () => {
    expect(JSON.stringify(expand(promoted).people)).toBe(JSON.stringify(pgen.people));
    const n = clone(w.needs!); n.catalogue.needs.push({ ...record(n, 'bureau-loan'), id: 'second-loan', label: 'Second loan', metBy: ['help'] });
    const picks = new Set(Array.from({ length: 40 }, (_, i) => drawNeedRecord(n, w.rules, `p-x-${i}`, 'records', 'debt', 'idealist')!.id));
    expect([...picks].sort()).toEqual(['bureau-loan', 'second-loan']);
    expect(drawNeedRecord(n, w.rules, 'p-x-3', 'records', 'debt', 'idealist')).toEqual(drawNeedRecord(n, w.rules, 'p-x-3', 'records', 'debt', 'idealist'));
  });
  test('a catalogue with no record of a person\'s kind fails the expansion naming them (P-0)', () => {
    const n = clone(promoted.needs!); n.catalogue.needs = n.catalogue.needs.filter(r => r.kind !== 'debt');
    const g = expand({ ...promoted, needs: n });
    expect(g.gaps.length).toBeGreaterThan(0); expect(g.gaps[0]).toMatch(/no record of need kind debt/);
    expect(checkPeople({ ...promoted, needs: n }, w.mission, w.plan, g).checks.find(x => x.id === 'P-0')!.ok).toBe(false);
  });
  test('a run pinned before records (no kinds, no catalogue) has no record fields and no N-rules, and passes', () => {
    const texts = readTextsSync(JSON.parse(readFileSync(PLAN, 'utf8')), JSON.parse(readFileSync('contracts/missions/handover/mission.json', 'utf8')));
    delete texts.needs;
    const old = world(texts, 'civic-block'), c = old.ctx(Cast.parse(JSON.parse(readFileSync('contracts/people/harbour-district.cast.json', 'utf8')))), g = expand(c);
    expect(g.people.persons.some(p => 'needRecord' in p)).toBe(false); expect('needs' in g.people.inputs).toBe(false);
    const r = checkPeople(c, old.mission, old.plan, g);
    expect(r.checks.some(x => x.id.startsWith('N-'))).toBe(false); expect(r.checks.filter(x => !x.ok).map(x => x.id)).toEqual([]);
  });
  test('the written inputs name the kinds and the catalogue', () => {
    expect(pgen.people.inputs.needs).toMatch(/^[0-9a-f]{64}$/);
  });
});

describe('schemas', () => {
  test('the rules and the catalogue parse; a stray key, a missing standard size or a lax label field is rejected', () => {
    const rules = JSON.parse(readFileSync('contracts/people/needs.rules.json', 'utf8')), cat = JSON.parse(readFileSync('contracts/people/harbour-district.needs.json', 'utf8'));
    expect(NeedsRules.safeParse(rules).success).toBe(true); expect(Catalogue.safeParse(cat).success).toBe(true);
    expect(NeedsRules.safeParse({ ...rules, stray: 1 }).success).toBe(false);
    expect(NeedsRules.safeParse({ ...rules, sizes: { small: 'v:help-small-usd' } }).success).toBe(false);
    expect(NeedsRules.safeParse({ ...rules, sizes: { ...rules.sizes, standard: 5 } }).success).toBe(false);
    const k = clone(rules); k.kinds.debt.metBy = ['barter']; expect(NeedsRules.safeParse(k).success).toBe(false); // an unknown approach
    const r = clone(cat); r.needs[0].colour = 'red'; expect(Catalogue.safeParse(r).success).toBe(false);
    const nosize = clone(cat); delete nosize.needs[0].size; expect(Catalogue.safeParse(nosize).success).toBe(false);
    expect(() => parseNeeds({ rules: JSON.stringify(rules), catalogue: JSON.stringify({ ...cat, schema: 'suborned.needs/2' }) })).toThrow();
  });
});

describe('checker rules N-1..N-8 pass on the real inputs and fail on their examples', () => {
  test('the promoted cast and the stand-in cast pass them all', () => {
    for (const [c, g] of [[promoted, pgen], [ctx, gen]] as const) {
      const r = checkPeople(c, w.mission, w.plan, g);
      expect(r.checks.filter(x => x.id.startsWith('N-')).map(x => x.id)).toEqual(['N-1', 'N-2', 'N-3', 'N-4', 'N-5', 'N-6', 'N-7', 'N-8']);
      expect(r.checks.filter(x => !x.ok).map(x => x.id + ' ' + x.detail)).toEqual([]);
    }
  });
  test('every N-rule has a source', () => {
    const r = checkPeople(promoted, w.mission, w.plan, pgen);
    for (const x of r.checks.filter(c => c.id.startsWith('N-'))) expect(x.source).toBeTruthy();
  });
  const f = (edit: (n: Needs) => void) => failedWith(edit, pgen.people, promoted);
  test('N-1: a record of a kind the rules do not have; a kind that is not the engine\'s; a person\'s record of another kind', () => {
    expect(f(n => { record(n, 'bureau-loan').kind = 'luck'; })).toContain('N-1');
    expect(f(n => { n.catalogue.secrets[1]!.kind = 'rumour'; })).toContain('N-1');
    expect(f(n => { n.rules.kinds.debt!.metBy = ['help', 'pay']; })).toContain('N-1');
    expect(f(n => { n.rules.kinds.cash!.help = 'favour'; })).toContain('N-1');
    expect(f(n => { delete n.rules.kinds.kin; })).toContain('N-1');
    expect(f(n => { record(n, 'lean-month').kind = 'papers'; })).toContain('N-1'); // the driver's person draws cash and their record says papers
    const p = clone(pgen.people); p.persons[0]!.needRecord = 'no-such-record';
    expect(failedWith(() => {}, p, promoted)).toContain('N-1');
  });
  test('N-2: a size that is priced by a rulebook value it does not have, an unknown size, a holder that binds to nothing, a subject its kind does not take', () => {
    expect(f(n => { record(n, 'bureau-loan').size = 'small'; })).toContain('N-2'); // v:help-small-usd is phase 2
    expect(f(n => { record(n, 'bureau-loan').size = 'huge'; })).toContain('N-2');
    expect(f(n => { record(n, 'bureau-loan').holder = 'trade:pawnbroker'; })).toContain('N-2'); // no such trade, and the kind lists no holders
    expect(f(n => { record(n, 'bureau-loan').holder = 'trade:diner'; })).toContain('N-2'); // a trade, but the kind does not allow it
    expect(f(n => { record(n, 'bureau-loan').holder = 'guild:diner' as never; })).toContain('N-2'); // no such binding
    expect(f(n => { record(n, 'bureau-loan').subject = 'tie:family'; })).toContain('N-2'); // debt requires nothing
    expect(f(n => { n.catalogue.needs[0]!.area = 'elsewhere'; })).toContain('N-2'); // the people stand in `records`
    // a holder the kind allows and that binds is accepted
    expect(f(n => { n.rules.kinds.debt!.holders = ['trade:diner']; record(n, 'bureau-loan').holder = 'trade:diner'; })).not.toContain('N-2');
  });
  test('N-3: label length, sentence, digits, era, gendered words, names, case, duplicates', () => {
    const lab = (label: string) => f(n => { record(n, 'bureau-loan').label = label; });
    expect(lab('Three word label')).toContain('N-3');
    expect(lab('Extraordinarily lengthy')).toContain('N-3');
    expect(lab('Pay it back.')).toContain('N-3');
    expect(lab('Loan 7')).toContain('N-3');
    expect(lab('Brother held')).toContain('N-3'); // gendered
    expect(lab('Wartime loan')).toContain('N-3'); // era
    expect(lab(`${promoted.cast.names.given[0]!.name} loan`)).toContain('N-3'); // a name from the pool
    expect(lab('Pinned Inez')).toContain('N-3'); // a pinned name
    expect(lab('bureau loan')).toContain('N-3'); // case
    expect(lab('')).toContain('N-3');
    expect(lab('Day shift')).toContain('N-3'); // taken by day-shift
    expect(f(n => { record(n, 'lean-month').id = 'bureau-loan'; })).toContain('N-3'); // an id twice
  });
  test('N-4: tone: harm, a child, a pressable care record', () => {
    const lab = (label: string) => f(n => { record(n, 'nurse-robot').label = label; });
    expect(lab('Funeral costs')).toContain('N-4');
    expect(lab('Child minder')).toContain('N-4');
    expect(lab('Murder file')).toContain('N-4');
    expect(f(n => { n.rules.kinds.care!.metBy = ['help', 'pressure']; })).toContain('N-4');
  });
  test('N-5: a record that widens its kind; a row whose temperament reaches no record', () => {
    expect(f(n => { record(n, 'bureau-loan').metBy = ['help', 'pressure', 'ask']; })).toContain('N-5');
    expect(f(n => { n.catalogue.secrets[1]!.metBy = ['pressure']; })).toContain('N-5');
    // every record that Pressure acts on narrowed to Help alone: a Fearful (answers to Pressure only) reaches none
    expect(f(n => { for (const r of n.catalogue.needs) if (n.rules.kinds[r.kind]!.metBy.includes('pressure')) r.metBy = ['help']; })).toContain('N-5');
  });
  test('N-6: two records that differ only in wording', () => {
    expect(f(n => { n.catalogue.needs.push({ ...record(n, 'bureau-loan'), id: 'friends-loan', label: 'Friend loan' }); })).toContain('N-6');
    expect(f(n => { n.catalogue.secrets.push({ ...n.catalogue.secrets[1]!, id: 'old-offence', label: 'Old offence' }); })).toContain('N-6');
    // a different size, holder or kind is a difference in play (the other rules may still object to the phase-1 values)
    expect(f(n => { n.rules.kinds.debt!.holders = ['trade:diner']; n.catalogue.needs.push({ ...record(n, 'bureau-loan'), id: 'friends-loan', label: 'Friend loan', holder: 'trade:diner' }); })).not.toContain('N-6');
  });
  test('N-7: an orphan record; a kind with no record in an area', () => {
    expect(f(n => { n.catalogue.needs.push({ ...record(n, 'bureau-loan'), id: 'far-loan', label: 'Far loan', area: 'elsewhere', size: 'standard', holder: 'beyond' }); })).toContain('N-7');
    expect(f(n => { n.catalogue.needs = n.catalogue.needs.filter(r => r.kind !== 'protection'); })).toContain('N-7');
    expect(f(n => { n.catalogue.secrets = n.catalogue.secrets.filter(r => r.kind !== 'affair'); })).toContain('N-7');
  });
  test('N-8: the Handover pins: Inez debt, standard, beyond; the driver cash, standard, beyond', () => {
    expect(f(n => { record(n, 'bureau-loan').kind = 'cash'; })).toContain('N-8');
    expect(f(n => { n.rules.sizes.large = 4000000; record(n, 'bureau-loan').size = 'large'; })).toContain('N-8'); // priced, so N-2 holds; but the Handover's Help is standard
    expect(f(n => { n.rules.kinds.cash!.holders = ['role:clerk']; record(n, 'lean-month').holder = 'role:clerk'; })).toContain('N-8');
    expect(f(n => { n.rules.kinds.debt!.holders = ['trade:diner']; record(n, 'bureau-loan').holder = 'trade:diner'; })).toContain('N-8');
    expect(checkPeople(promoted, undefined, w.plan, pgen).checks.find(x => x.id === 'N-8')!.ok).toBe(false); // no mission to cast
  });
});
