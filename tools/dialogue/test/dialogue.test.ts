// The dialogue generator, offline (Fable, brief 012): no model, no index. The Handover's hand-written text is the fixture the tables are held to; a stand-in made by code fills the
// rest, so every rule has a passing case (the whole district) and a failing one (a mutation of the tables). Run: bun test tools/dialogue
import { beforeAll, describe, expect, test } from 'bun:test';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import { z } from 'zod';
import { Cast } from '../../../contracts/people/cast.schema';
import { People, type Person } from '../../../contracts/people/people.schema';
import { Entry } from '../../../contracts/missions/handover/text.schema';
import { Lines, Voices } from '../../../contracts/dialogue/voices.schema';
import { fnv1a32 } from '../../../contracts/rules/rulebook.ref';
import { pickIndex } from '../../../contracts/missions/handover/text.schema';
import { sha256 } from '../../gen/sha';
import { expand as expandPeople } from '../../people/expand';
import { readTexts, world } from '../../people/inputs';
import { schemaProblems } from '../../levels/test/schema-lint';
import { bindHandover, compareToReference } from '../bind';
import { checkDialogue, templates } from '../check';
import { ALL, chooseTie, expandAll, findCell, linesFile, merge, otherNoun, type ExpandCtx } from '../expand';
import { fill, sentenceCase, slotsIn, wordCount } from '../fill';
import { REFERENCE, referenceOf } from '../fixture';
import { lookupLine, personKey, pickPersonLine, playPersonScene } from '../lookup';
import { promoteRun, checkPromotion, voicesBytes } from '../promoted';
import { coversOf, debriefFields, groupOf, groupTemperaments, lexiconNeeds } from '../rules';
import { replyCells, requiredCells, requiredScenes } from '../situations';
import { standIn } from '../standin';
import { parseTexts, promotionFileOf, readText, readTexts_, tablesFileOf } from '../texts';

const root = join(import.meta.dir, '..', '..', '..'), PLAN = 'contracts/world/harbour-district.plan.json';
type Parsed = ReturnType<typeof parseTexts>;
// The people the tests expand are the stand-in cast's (tools/people/test/handover-fix.cast.json: a fixture, with the Handover's lovers), not the promoted cast, so a new promotion changes no test here.
let P: { text: string; pool: string[] }, T: Parsed, text: Awaited<ReturnType<typeof readText>>, LOVERS: People;
const clone = <X>(x: X): X => JSON.parse(JSON.stringify(x));
beforeAll(async () => {
  const w = world(await readTexts(PLAN), 'civic-block'), cast = Cast.parse(JSON.parse(readFileSync(join(root, 'tools/people/test/handover-fix.cast.json'), 'utf8')));
  LOVERS = People.parse(JSON.parse(JSON.stringify(expandPeople(w.ctx(cast)).people)));
  P = { text: JSON.stringify(LOVERS, null, 1) + '\n', pool: [...cast.names.given, ...cast.names.family].map(n => n.name) };
  T = parseTexts(await readTexts_({ people: P.text, names: P.pool }));
  text = await readText();
});

type Tables = Record<string, Voices>;
const standAll = (o: { keepNames?: boolean } = {}): Tables => Object.fromEntries(Object.keys(T.dr.scopes).map(s => [s, standIn(s, T.dr, T.rb, T.prules, text, o)]));
/** The checker on the whole district's tables (`all`), optionally after `mut` has changed them. */
function run(mut?: (v: Tables) => void, o: { people?: People; keepNames?: boolean; named?: string[]; scope?: string; only?: string[]; reference?: boolean } = {}) {
  const v = standAll({ keepNames: o.keepNames });
  mut?.(v);
  const voices = o.only ? o.only.map(s => v[s]!) : Object.values(v);
  return checkDialogue({ dr: T.dr, rules: T.prules, rb: T.rb, people: o.people ?? T.people, voices, scope: o.scope ?? 'all', seed: 'test', pool: [...P.pool, ...T.people.persons.flatMap(x => x.name.split(/\s+/))], named: o.named, reference: o.reference === false ? undefined : text });
}
const failed = (r: ReturnType<typeof run>) => r.checks.filter(c => !c.ok).map(c => c.id);
const detail = (r: ReturnType<typeof run>, id: string) => r.checks.find(c => c.id === id)?.detail ?? '';
const cell = (v: Tables, scope: string, group: string, pred: (c: Voices['groups'][number]['cells'][number]) => boolean) => v[scope]!.groups.find(g => g.group === group)!.cells.find(pred)!;
const person = (people: People, pred: (p: Person) => boolean) => people.persons.find(pred)!;

describe('the rules', () => {
  test('the groups cover every position and role the people rules know, each once; every scope names real groups', () => {
    const covers = Object.values(T.dr.groups).flatMap(g => g.covers).sort();
    expect(covers).toEqual(coversOf(T.prules));
    expect(new Set(covers).size).toBe(covers.length);
    for (const s of Object.values(T.dr.scopes)) for (const g of s.groups ?? []) expect(T.dr.groups[g]).toBeTruthy();
    expect(groupOf(T.dr, 'server')).toBe('counter');
    expect(groupOf(T.dr, 'clerk')).toBe('clerk');
    expect(groupOf(T.dr, 'astronaut')).toBeUndefined();
  });
  test('the temperaments a group can have follow the people rules: a guard is loyal or mercenary, a watcher may be fearful, a councillor is never an idealist', () => {
    expect(groupTemperaments(T.dr, T.prules, 'counter')).toEqual(['idealist', 'mercenary', 'fearful', 'loyal']);
    expect(groupTemperaments(T.dr, T.prules, 'security')).toEqual(['mercenary', 'fearful', 'loyal']);
    expect(groupTemperaments(T.dr, T.prules, 'official')).toEqual(['mercenary', 'fearful', 'loyal']);
  });
  test('every need has the words the Learned line says, every rulebook debrief field has a widest fill, the engine\'s summons codes are the lexicon\'s keys', () => {
    for (const n of lexiconNeeds(T.prules)) expect(T.dr.listen.needFacts[n], n).toBeTruthy();
    for (const f of Object.values(debriefFields(T.rb)).flat()) expect(T.dr.lexicon.debriefFill[f], f).toBeTruthy();
    expect(Object.keys(T.dr.listen.tieByNeed).every(n => n in T.prules.needs.list)).toBe(true);
    const model = join(root, 'sim/Suborned.Sim/Model.cs');
    if (existsSync(model)) {
      const src = readFileSync(model, 'utf8'), list = src.slice(src.indexOf('public static readonly string[] Codes'), src.indexOf('};', src.indexOf('public static readonly string[] Codes')));
      const engine = [...list.matchAll(/"(summon-[a-z-]+)"/g)].map(m => m[1]!).sort();
      expect(engine.length).toBeGreaterThanOrEqual(7);
      expect(T.dr.lexicon.summonKeys.filter(k => k !== 'recalled').sort()).toEqual(engine);
    }
  });
  test('the tone filters are the Handover text\'s, word for word (the generator and the hand-written text hold one line)', () => {
    expect(T.dr.tone.avoid).toEqual(text.tone.avoid);
    expect(T.dr.tone.profanity).toEqual(text.tone.profanity);
    expect(T.dr.tone.world).toBe(text.tone.world);
  });
  test('the output schemas pass the strict validator', () => {
    expect(schemaProblems(z.toJSONSchema(Voices))).toEqual([]);
    expect(schemaProblems(z.toJSONSchema(Lines))).toEqual([]);
  });
});

describe('the situations are derived from the rulebook', () => {
  test('30 replies in all: approach × temperament × outcome, and the Handover\'s waitress has exactly the cells of the four temperaments', () => {
    const cells = replyCells(T.rb, T.prules);
    expect(cells.length).toBe(30);
    const have = new Set(text.dialogue.filter(e => e.speaker === 'waitress' && e.event === 'reply').map(e => `${e.verb}/${e.temperament}/${e.outcome}`));
    expect([...have].sort()).toEqual(cells.map(c => `${c.verb}/${c.temperament}/${c.outcome}`).sort());
  });
  test('a group\'s cells: its temperaments\' replies and confirmations, questioning, gunfire; a three-temperament group has fewer', () => {
    expect(requiredCells(T.dr, T.rb, T.prules, 'counter').length).toBe(30 + 4 + 3 + 2);
    expect(requiredCells(T.dr, T.rb, T.prules, 'security').length).toBe(20 + 3 + 3 + 2);
    expect(requiredCells(T.dr, T.rb, T.prules, 'counter').some(k => k.event === 'reply' && k.verb === 'pay' && k.outcome === 'turned' && k.temperament === 'idealist')).toBe(false); // an Idealist refuses a bribe
  });
  test('the scenes asked for: Listen by tie (and none) × temperament, the lovers\' call and Watch', () => {
    const s = requiredScenes(T.prules);
    expect(s.filter(x => x.verb === 'listen').length).toBe(7 * 4);
    expect(s.filter(x => x.verb === 'call').length).toBe(4);
    expect(s.filter(x => x.verb === 'watch' && x.tie === 'lover').length).toBe(1);
  });
});

describe('the Handover\'s hand-written text as fixture tables', () => {
  test('it is the reference for four voice groups; a stand-in built on it passes every rule on the whole district', () => {
    expect(Object.keys(REFERENCE).sort()).toEqual(['counter', 'driver', 'official', 'security']);
    const r = run();
    expect(r.checks.filter(c => !c.ok).map(c => `${c.id}: ${c.detail}`)).toEqual([]);
    expect(r.counts.persons).toBe(T.people.persons.length);
    expect(r.checks.map(c => c.id)).toEqual(['D-0', 'D-1', 'D-2', 'D-3', 'D-4', 'D-5', 'D-6', 'D-7', 'D-8', 'D-9', 'D-10', 'D-11', 'D-12']);
  });
  test('the hand-written text itself, taken whole, is held to the same rules: it passes all but one, the generator\'s bar is stricter (it genders no one)', () => {
    const r = run(undefined, { keepNames: true, named: ['Inez', 'Vale'] });
    expect(failed(r)).toEqual(['D-5']);
    const faults = detail(r, 'D-5').split('; ').filter(x => !x.startsWith('+'));
    expect(faults.length).toBeGreaterThan(5);
    for (const f of faults) expect(f).toContain('(gendered)');
    expect(run(undefined, { keepNames: true }).checks.find(c => c.id === 'D-4')!.ok).toBe(false); // without naming Inez and Vale it fails the names rule too
    for (const id of ['D-2', 'D-3', 'D-4', 'D-6', 'D-7', 'D-12']) expect(r.checks.find(c => c.id === id)!.ok, id).toBe(true);
  });
  test('the real lines are in the tables: Inez\'s Help-turned for an Idealist is the counter group\'s cell', () => {
    const v = standAll({ keepNames: true }), c = cell(v, 'street', 'counter', x => x.event === 'reply' && x.verb === 'help' && x.outcome === 'turned' && x.temperament === 'idealist');
    expect(c.lines).toEqual(text.dialogue.find(e => e.speaker === 'waitress' && e.event === 'reply' && e.verb === 'help' && e.outcome === 'turned' && e.temperament === 'idealist')!.lines);
    expect(v.street!.groups.find(g => g.group === 'counter')!.reads.watch).toEqual(referenceOf(text, 'counter')!.watch);
  });
  test('the stand-in without keepNames carries no name and no gendered word', () => {
    const all = JSON.stringify(standAll());
    expect(all).not.toMatch(/Inez|Vale\b/);
    expect(all).not.toMatch(/\b(he|him|she|her|sir|madam)\b/i);
  });
  test('dropping a reference cell fails D-12, and so does a cell with fewer variants than the Handover\'s', () => {
    const r = run(v => { const g = v.street!.groups.find(x => x.group === 'counter')!; g.cells = g.cells.filter(c => !(c.event === 'reply' && c.verb === 'help' && c.outcome === 'turned' && c.temperament === 'mercenary')); });
    expect(failed(r)).toContain('D-12'); expect(detail(r, 'D-12')).toContain('counter');
    const r2 = run(v => { cell(v, 'street', 'counter', c => c.event === 'reply' && c.verb === 'pay' && c.outcome === 'turned' && c.temperament === 'mercenary').lines.length = 2; }, { keepNames: true });
    expect(detail(r2, 'D-12')).toMatch(/2 variants, the Handover has 3/);
  });
});

describe('the expansion', () => {
  const ctx = (people: People = T.people, tables = standAll(), seed = 's'): ExpandCtx => ({ people, rules: T.prules, dr: T.dr, rb: T.rb, tables: merge(Object.values(tables)).merged, seed });
  test('every person speaks: every cell of their group for their temperament, the Scout\'s two read-outs, a Listen scene, a Watch scene', () => {
    const c = ctx(), { persons, gaps } = expandAll(c, ALL(T.dr));
    expect(gaps).toEqual([]);
    expect(persons.length).toBe(T.people.persons.length);
    for (const p of persons) {
      const q = T.people.persons.find(x => x.id === p.id)!, want = requiredCells(T.dr, T.rb, T.prules, p.group).filter(k => k.temperament === undefined || k.temperament === q.temperament);
      expect(p.entries.filter(e => e.speaker === 'person' && e.event !== 'summon').length).toBe(want.length);
      expect(p.entries.filter(e => e.speaker === 'scout').map(e => e.verb).sort()).toEqual(['listen', 'watch']);
      expect(p.scenes.map(s => `${s.verb}`).sort()).toEqual(q.ties.some(t => t.kind === 'lover') ? ['call', 'listen', 'watch', 'watch'] : ['listen', 'watch']); // a lover also has the call and the lovers' Watch
      expect(p.address.length).toBeGreaterThan(0);
    }
  });
  test('the same inputs give the same bytes; another seed changes who says what (the address), not what can be said', () => {
    const a = expandAll(ctx(), ALL(T.dr)).persons, b = expandAll(ctx(), ALL(T.dr)).persons, c = expandAll(ctx(T.people, standAll(), 'other'), ALL(T.dr)).persons;
    expect(JSON.stringify(a)).toBe(JSON.stringify(b));
    expect(JSON.stringify(a)).not.toBe(JSON.stringify(c));
    expect(a.map(p => p.entries.length)).toEqual(c.map(p => p.entries.length));
    for (const p of a) expect(T.dr.groups[p.group]).toBeTruthy();
    const addr = (x: typeof a) => x.map(p => p.address).join();
    expect(addr(a)).not.toBe(addr(c));
  });
  test('the address is a term of the person\'s group, picked by fnv1a32(seed|id|address)', () => {
    const tables = standAll(), c = ctx(T.people, tables, 'pin'), { persons } = expandAll(c, ALL(T.dr));
    const g = merge(Object.values(tables)).merged.groups;
    for (const p of persons) expect(p.address).toBe(g.get(p.group)!.address[fnv1a32(`pin|${p.id}|address`) % g.get(p.group)!.address.length]!);
  });
  test('slots are filled from the person: their post, their need, their workplace; the first letter of a sentence is capitalised', () => {
    const tables = standAll(), g = tables.street!.groups.find(x => x.group === 'counter')!;
    cell(tables, 'street', 'counter', c => c.event === 'reply' && c.verb === 'help' && c.outcome === 'turned' && c.temperament === 'fearful').lines = ['{own}, {address}? Is {met}?', 'I {job} at {work}.', '{noun} owes you for {need}.'];
    void g;
    const c = ctx(T.people, tables), server = person(T.people, p => p.position === 'server' && p.temperament === 'fearful' && p.need === 'debt');
    const mine = expandAll(c, ALL(T.dr)).persons.find(p => p.id === server.id)!, e = mine.entries.find(x => x.event === 'reply' && x.verb === 'help' && x.outcome === 'turned')!;
    expect(e.lines[0]).toBe(`My debt, ${mine.address}? Is the debt is gone?`);
    expect(e.lines[1]).toBe('I serve the tables at the Corner café.');
    expect(e.lines[2]).toBe('The server owes you for debt.');
    expect(fill('{own} and {need}', { own: 'my debt', need: 'debt' }).text).toBe('My debt and debt');
    expect(fill('Hello {nobody}.', {}).missing).toEqual(['nobody']);
    expect(sentenceCase('one. two! three? four')).toBe('One. Two! Three? Four');
    expect(slotsIn('a {b} c {d_e} {f-g}')).toEqual(['b', 'd_e', 'f-g']);
    expect(wordCount(' a  b c ')).toBe(3);
  });
  test('the most specific cell wins: a need-qualified or secret-qualified cell answers the person who has it, the plain cell everyone else', () => {
    const tables = standAll(), g = tables.street!.groups.find(x => x.group === 'counter')!;
    g.cells.push({ event: 'reply', verb: 'help', outcome: 'turned', temperament: 'idealist', need: 'debt', lines: ['You cleared my debt? Truly?', 'The whole debt? I am yours.'] });
    g.cells.push({ event: 'questioned', outcome: 'gives', secret: 'record', lines: ['I have a past on file, officer.', 'It is all in the record, officer.'] });
    const c = ctx(T.people, tables), { persons } = expandAll(c, ALL(T.dr)), plain = g.cells.find(x => x.event === 'reply' && x.verb === 'help' && x.outcome === 'turned' && x.temperament === 'idealist' && !x.need)!;
    const debtor = person(T.people, p => groupOf(T.dr, p.position) === 'counter' && p.temperament === 'idealist' && p.need === 'debt'), other = person(T.people, p => groupOf(T.dr, p.position) === 'counter' && p.temperament === 'idealist' && p.need !== 'debt');
    const said = (id: string) => persons.find(p => p.id === id)!.entries.find(e => e.event === 'reply' && e.verb === 'help' && e.outcome === 'turned')!.lines;
    expect(said(debtor.id)).toEqual(['You cleared my debt? Truly?', 'The whole debt? I am yours.']);
    expect(said(other.id).length).toBe(plain.lines.length);
    const rec = T.people.persons.find(p => p.secret === 'record' && groupOf(T.dr, p.position) === 'counter'), none = person(T.people, p => !p.secret && groupOf(T.dr, p.position) === 'counter');
    if (rec) expect(persons.find(p => p.id === rec.id)!.entries.find(e => e.event === 'questioned' && e.outcome === 'gives')!.lines[0]).toBe('I have a past on file, officer.');
    expect(persons.find(p => p.id === none.id)!.entries.find(e => e.event === 'questioned' && e.outcome === 'gives')!.lines[0]).not.toBe('I have a past on file, officer.');
    expect(findCell(g.cells, { event: 'reply', verb: 'help', outcome: 'turned', temperament: 'idealist', need: 'debt' })!.need).toBe('debt');
    expect(findCell(g.cells, { event: 'reply', verb: 'help', outcome: 'turned', temperament: 'idealist', need: 'cash' })!.need).toBeUndefined();
  });
  test('Listen picks the tie: the lover for an affair, then the kind the need points at, then the rules\' order; none with no tie; the partner by hash', () => {
    const by = new Map(LOVERS.persons.map(p => [p.id, p])), seed = 's';
    const lover = LOVERS.persons.find(p => p.secret === 'affair')!, t = chooseTie(T.dr, lover, by, seed);
    expect(t.kind).toBe('lover'); expect(by.get(lover.ties.find(x => x.kind === 'lover')!.with)!.id).toBe(t.other!.id);
    const mk = (over: Partial<Person>): Person => ({ ...clone(lover), secret: null, ...over });
    const a = LOVERS.persons.find(p => p.ties.some(x => x.kind === 'workmate') && !p.ties.some(x => x.kind === 'family'))!;
    expect(chooseTie(T.dr, mk({ ...clone(a), id: a.id, need: 'advancement' }), by, seed).kind).toBe('workmate');
    expect(chooseTie(T.dr, mk({ id: a.id, ties: [{ kind: 'regular', with: a.ties[0]!.with }, { kind: 'workmate', with: a.ties[0]!.with }], need: 'kin' }), by, seed).kind).toBe('workmate'); // no family tie: the rules' order, workmate first
    expect(chooseTie(T.dr, mk({ id: a.id, ties: [{ kind: 'regular', with: a.ties[0]!.with }, { kind: 'family', with: a.ties[0]!.with }], need: 'kin' }), by, seed).kind).toBe('family'); // the need points at family
    expect(chooseTie(T.dr, mk({ id: a.id, ties: [], need: 'debt' }), by, seed)).toEqual({ kind: 'none' });
    const two = mk({ id: a.id, need: 'papers', ties: [{ kind: 'workmate', with: LOVERS.persons[1]!.id }, { kind: 'workmate', with: LOVERS.persons[2]!.id }] });
    const x = chooseTie(T.dr, two, by, seed), y = chooseTie(T.dr, two, by, seed);
    expect(x.other!.id).toBe(y.other!.id);
    const want = [LOVERS.persons[1]!.id, LOVERS.persons[2]!.id].sort((p, q) => fnv1a32(`${seed}|${a.id}|workmate|${p}`) - fnv1a32(`${seed}|${a.id}|workmate|${q}`))[0];
    expect(x.other!.id).toBe(want!);
  });
  test('lovers: each gets a call scene and the lovers\' Watch with the other by name, the Learned line names the secret, and the summons lines in both ends; nobody else does', () => {
    const c = ctx(LOVERS), { persons, gaps } = expandAll(c, ALL(T.dr)), by = new Map(LOVERS.persons.map(p => [p.id, p]));
    expect(gaps).toEqual([]);
    const pairs = persons.filter(p => p.secret === 'affair');
    expect(pairs.length).toBeGreaterThanOrEqual(2);
    for (const p of persons) {
      const isLover = by.get(p.id)!.ties.some(t => t.kind === 'lover');
      expect(p.scenes.some(s => s.verb === 'call')).toBe(isLover);
      expect(p.scenes.some(s => s.verb === 'watch' && s.tie === 'lover')).toBe(isLover);
      expect(p.entries.some(e => e.event === 'summon')).toBe(isLover);
    }
    for (const p of pairs) {
      const q = by.get(p.id)!, other = by.get(q.ties.find(t => t.kind === 'lover')!.with)!, listen = p.scenes.find(s => s.verb === 'listen')!, call = p.scenes.find(s => s.verb === 'call')!;
      expect(listen.tie).toBe('lover'); expect(listen.with).toBe(other.id); expect(listen.learns).toEqual(['need', 'temperament', 'secret']);
      expect(listen.result).toBe(`Learned: ${q.name.split(' ')[0]} ${T.dr.listen.needFacts[q.need]} · {temperament} · Secret: ${q.name.split(' ')[0]} is seeing ${other.name.split(' ')[0]}, ${otherNoun(c, q, other)}.`);
      expect(call.result).toBe(`${q.name.split(' ')[0]} called ${other.name.split(' ')[0]} over. ${other.name.split(' ')[0]} is on the way.`);
      expect(call.variants.every(v => v.beats.some(b => b.speaker === other.id) && v.beats.some(b => b.speaker === p.id))).toBe(true);
      const keys = p.entries.filter(e => e.event === 'summon');
      expect(keys.length).toBe(T.dr.lexicon.summonKeys.length * 2);
      expect(new Set(keys.map(e => e.side))).toEqual(new Set(['caller', 'summoned']));
    }
  });
  test('a family tie is called by the rules\' word for kin, a workmate by the partner\'s post', () => {
    const c = ctx(), fam = T.people.persons.find(p => p.ties.some(t => t.kind === 'family'))!, fo = T.people.persons.find(x => x.id === fam.ties.find(t => t.kind === 'family')!.with)!;
    expect(otherNoun(c, fam, fo)).toBe('a relative');
    const w = T.people.persons.find(p => p.ties.some(t => t.kind === 'workmate'))!, wo = T.people.persons.find(x => x.id === w.ties.find(t => t.kind === 'workmate')!.with)!;
    expect(otherNoun(c, w, wo)).toBe(c.tables.lexicon!.positions.find(x => x.position === wo.position)!.noun);
  });
  test('a person in no group is a gap, not a silent skip; so is a missing lexicon word', () => {
    const odd = clone(T.people); odd.persons[0]!.position = 'astronaut';
    expect(expandAll(ctx(odd), ALL(T.dr)).gaps.some(g => g.message.includes('astronaut'))).toBe(true);
    const tables = standAll(); tables.lexicon!.lexicon!.positions = tables.lexicon!.lexicon!.positions.filter(p => p.position !== 'clerk');
    const gaps = expandAll(ctx(T.people, tables), { groups: ['clerk'], scenes: false, lexicon: false }).gaps;
    expect(gaps.some(g => g.rule === 'D-3' || g.rule === 'D-1')).toBe(false); // the stand-in's clerk cells use no job slot: nothing is missing
    tables.civic!.groups.find(g => g.group === 'clerk')!.cells[0]!.lines[0] = 'I {job}, {address}.';
    expect(expandAll(ctx(T.people, tables), { groups: ['clerk'], scenes: false, lexicon: false }).gaps.some(g => g.rule === 'D-3' && g.message.includes('{job}'))).toBe(true);
  });
  test('the lines file parses as suborned.lines/1 and keeps what was expanded', () => {
    const c = ctx(), { persons } = expandAll(c, ALL(T.dr)), f = linesFile(c, persons, { people: sha256(P.text), rules: 'r', tables: { all: 'x' } }, { id: 'harbour-district', version: 'v' });
    expect(Lines.safeParse(f).success).toBe(true);
    expect(f.debrief.length).toBe(Object.keys(debriefFields(T.rb)).length);
    expect(templates(c.tables).length).toBeGreaterThan(900);
  });
});

describe('each rule fails on its own example', () => {
  const only = (r: ReturnType<typeof run>, id: string) => expect(failed(r), detail(r, id)).toContain(id);
  test('the clean stand-in passes all thirteen', () => expect(failed(run())).toEqual([]));
  test('D-0 coverage: a missing plain cell, a dead cell (an Idealist taking a bribe), a group not written, a scope writing another\'s block', () => {
    only(run(v => { const g = v.street!.groups[0]!; g.cells = g.cells.filter(c => !(c.event === 'confirm' && c.temperament === 'fearful')); }), 'D-0');
    const dead = run(v => { v.street!.groups[0]!.cells.push({ event: 'reply', verb: 'pay', outcome: 'turned', temperament: 'idealist', lines: ['Take it, then.', 'Why not, then.'] }); });
    only(dead, 'D-0'); expect(detail(dead, 'D-0')).toContain('a dead cell');
    only(run(v => { v.street!.groups = v.street!.groups.filter(g => g.group !== 'resident'); }), 'D-0');
    const s = run(v => { v.street!.lexicon = v.lexicon!.lexicon; }, { only: ['street', 'lexicon'], scope: 'street' });
    only(s, 'D-0'); expect(detail(s, 'D-0')).toContain('writes a lexicon');
    only(run(v => { v.scenes!.scenes = v.scenes!.scenes.slice(1); }), 'D-0');
    only(run(v => { v.street!.groups[0]!.cells.push(clone(v.street!.groups[0]!.cells[0]!)); }), 'D-0'); // a cell written twice
  });
  test('D-1 everyone speaks: a person whose position no group covers', () => {
    const odd = clone(T.people); odd.persons[3]!.position = 'astronaut';
    only(run(undefined, { people: odd }), 'D-1');
  });
  test('D-2 fit: 14 words, 85 characters, no sentence end, one variant, three variants in a scene', () => {
    const base = (f: (l: string[]) => void) => run(v => f(cell(v, 'street', 'counter', c => c.event === 'gunfire').lines));
    only(base(l => { l[0] = 'One two three four five six seven eight nine ten eleven twelve thirteen.'; }), 'D-2');
    only(base(l => { l[0] = 'Words '.repeat(14).trim() + '.'; }), 'D-2');
    const long = base(l => { l[0] = 'Short words only here, but this line is made long enough to pass the limit of eighty characters easily.'; });
    only(long, 'D-2');
    only(base(l => { l[0] = 'No full stop at the end'; }), 'D-2');
    only(base(l => { l.length = 1; }), 'D-2');
    only(run(v => { v.scenes!.scenes[0]!.variants.push(clone(v.scenes!.scenes[0]!.variants[0]!), clone(v.scenes!.scenes[0]!.variants[0]!)); }), 'D-2');
  });
  test('D-3 slots: an unknown slot, a slot the group may not use, a partner slot in a scene with none, a field the debrief line does not have', () => {
    const unk = run(v => { cell(v, 'street', 'counter', c => c.event === 'gunfire').lines[0] = 'Get down, {bogus}.'; }); only(unk, 'D-3'); expect(detail(unk, 'D-3')).toContain('{bogus}');
    only(run(v => { cell(v, 'street', 'resident', c => c.event === 'gunfire').lines[0] = 'Get down at {work}.'; }), 'D-3');
    only(run(v => { v.scenes!.scenes.find(s => s.verb === 'listen' && s.tie === 'none' && s.temperament === 'loyal')!.variants[0]!.beats[1]!.line = 'Well, {other}.'; }), 'D-3');
    only(run(v => { v.lexicon!.lexicon!.debrief[0]!.lines[0] = '{who} {bogus}.'; }), 'D-3');
    only(run(v => { cell(v, 'civic', 'clerk', c => c.event === 'gunfire').lines[0] = 'Get down, {address'; }), 'D-3');
    only(run(v => { v.street!.groups[0]!.reads.watch[0] = 'Busy with {own}, as ever.'; }), 'D-3'); // a Watch teaches the routine, not the need
    only(run(v => { v.street!.scenes.find(s => s.verb === 'watch' && s.group === 'counter')!.variants[0]!.beats[1]!.line = 'Back to {own}.'; }), 'D-3');
    only(run(v => { v.scenes!.scenes.find(s => s.verb === 'listen' && s.tie === 'family' && s.temperament === 'loyal')!.variants[0]!.beats[0]!.line = 'My {other_noun}, {other}.'; }), 'D-3'); // other_noun is the rules' (the Learned line), not a template's
  });
  test('D-4 names: a name from the district\'s pool in a table, a capital mid-sentence', () => {
    const n = run(v => { cell(v, 'street', 'counter', c => c.event === 'gunfire').lines[0] = 'Get down, Sunita.'; }); only(n, 'D-4'); expect(detail(n, 'D-4')).toContain('Sunita');
    only(run(v => { cell(v, 'street', 'counter', c => c.event === 'gunfire').lines[0] = 'Get down behind the Gate.'; }), 'D-4');
    only(run(v => { cell(v, 'street', 'counter', c => c.event === 'gunfire').lines[0] = 'Get down, Inez.'; }), 'D-4'); // pinned names are the Handover's, not a generated table's
  });
  test('D-5 tone: modern speech, strong profanity, explicit words, gore, a civilian as payload', () => {
    for (const bad of ['Get down, that is so cool.', 'Get down, sir.', 'Tell him to get down.', 'Get down, you bastard.', 'Get down, back to the bedroom.', 'Get down, there is gore here.', 'Use a human shield.', 'Check the network.'])
      only(run(v => { cell(v, 'street', 'counter', c => c.event === 'gunfire').lines[0] = bad; }), 'D-5');
    expect(failed(run(v => { cell(v, 'street', 'counter', c => c.event === 'gunfire').lines[0] = 'Damn it, get down.'; }))).not.toContain('D-5'); // mild is allowed
  });
  test('D-6 signal words: the cell\'s names in a person\'s mouth, a digit, a name awaiting Sky; the Scout may say what a person may not', () => {
    for (const bad of ['Tell the Fixer, quickly.', 'I am a mercenary, you see.', 'Get down, 4 of them.', 'Halcyon sent you, did he?', 'The mission is blown.'])
      only(run(v => { cell(v, 'street', 'counter', c => c.event === 'gunfire').lines[0] = bad; }), 'D-6');
    expect(failed(run(v => { v.lexicon!.lexicon!.reads[0]!.lines[0] = '{need}. Idealist. Proud.'; }))).not.toContain('D-6');
    only(run(v => { v.lexicon!.lexicon!.reads[0]!.lines[0] = '{need}. Halcyon knows.'; }), 'D-6');
  });
  test('D-7 repeats: one line in two cells, one line twice in a scene variant, case and punctuation ignored', () => {
    only(run(v => { cell(v, 'street', 'counter', c => c.event === 'gunfire').lines[0] = cell(v, 'street', 'counter', c => c.event === 'saw-gunfire').lines[0]!.toUpperCase().replace('.', '!'); }), 'D-7');
    only(run(v => { const g = cell(v, 'street', 'workshop', c => c.event === 'gunfire'); g.lines[1] = g.lines[0]!; }), 'D-7');
  });
  test('D-8 knowledge: a need slot in a cold Ask or a Pay, a qualified cell with an unknown need, on a confirmation, too many', () => {
    const ask = run(v => { cell(v, 'street', 'counter', c => c.event === 'reply' && c.verb === 'ask' && c.outcome === 'refused' && c.temperament === 'idealist').lines[0] = 'Not {own}, thank you, {address}.'; });
    only(ask, 'D-8'); expect(detail(ask, 'D-8')).toContain('before it is known');
    expect(failed(run(v => { cell(v, 'street', 'counter', c => c.event === 'reply' && c.verb === 'help' && c.outcome === 'turned' && c.temperament === 'idealist').lines[0] = 'You cleared {own}? {address}.'; }))).not.toContain('D-8');
    only(run(v => { v.street!.groups[0]!.cells.push({ event: 'reply', verb: 'help', outcome: 'turned', temperament: 'idealist', need: 'telepathy', lines: ['Yes, indeed.', 'Quite so, indeed.'] }); }), 'D-8');
    only(run(v => { v.street!.groups[0]!.cells.push({ event: 'confirm', verb: 'call', temperament: 'loyal', need: 'debt', lines: ['I am on it.', 'On it, then.'] }); }), 'D-8');
    only(run(v => { v.street!.groups[0]!.cells.push({ event: 'reply', verb: 'ask', outcome: 'refused', temperament: 'loyal', need: 'debt', lines: ['Not for debt.', 'No, for debt.'] }); }), 'D-8'); // a cold Ask answered for a need
    only(run(v => { for (let i = 0; i < 13; i++) v.street!.groups[0]!.cells.push({ event: 'questioned', outcome: 'gives', need: `n${i}`, lines: [`Version ${'a'.repeat(i + 1)} here.`, `Other ${'b'.repeat(i + 1)} one.`] }); }), 'D-8');
  });
  test('D-9 valid: a cell of five variants does not parse as suborned.lines/1', () => {
    only(run(v => { cell(v, 'street', 'counter', c => c.event === 'gunfire').lines.push('Get down now, friend.', 'Down, quickly, friend.', 'Move yourself, friend.'); }), 'D-9');
  });
  test('D-10 scenes: no focus, a partner where there is none, the Scout in a Listen, a bystander between two on a tie, one variant twice', () => {
    const find = (v: Tables, verb: string, tie?: string, t?: string) => v.scenes!.scenes.find(s => s.verb === verb && s.tie === tie && s.temperament === t)!;
    only(run(v => { find(v, 'listen', 'workmate', 'loyal').variants[0]!.beats.forEach(b => { if (b.who === 'focus') b.who = 'other'; }); }), 'D-10');
    only(run(v => { find(v, 'listen', 'none', 'loyal').variants[0]!.beats[1]!.who = 'other'; }), 'D-10');
    only(run(v => { find(v, 'listen', 'regular', 'loyal').variants[0]!.beats[1]!.who = 'scout'; }), 'D-10');
    only(run(v => { find(v, 'listen', 'family', 'loyal').variants[0]!.beats[1]!.who = 'bystander'; }), 'D-10');
    only(run(v => { const s = find(v, 'listen', 'debtor', 'loyal'); s.variants[1] = clone(s.variants[0]!); }), 'D-10');
  });
  test('D-11 the lexicon: a need missing, a position the rules do not list, a debrief line without its cause, no width for a field, a summons key missing', () => {
    only(run(v => { v.lexicon!.lexicon!.needs = v.lexicon!.lexicon!.needs.filter(n => n.need !== 'debt'); }), 'D-11');
    only(run(v => { v.lexicon!.lexicon!.positions.push({ position: 'astronaut', job: 'fly', noun: 'the pilot' }); }), 'D-11');
    const d = run(v => { v.lexicon!.lexicon!.debrief.find(x => x.id === 'give-up')!.lines[0] = '{who} told all.'; }); only(d, 'D-11'); expect(detail(d, 'D-11')).toContain('{fact}');
    only(run(v => { v.lexicon!.lexicon!.summons = v.lexicon!.lexicon!.summons.slice(1); }), 'D-11');
    only(run(v => { v.lexicon!.lexicon!.reads = v.lexicon!.lexicon!.reads.slice(1); }), 'D-11');
  });
  test('a debrief line may run longer than a spoken line, to its own limit', () => {
    const long = '{who} gave up {fact}: {left} outweighed {right}.';
    expect(failed(run(v => { v.lexicon!.lexicon!.debrief.find(x => x.id === 'give-up')!.lines[0] = long; }))).not.toContain('D-2');
    expect(failed(run(v => { v.lexicon!.lexicon!.debrief.find(x => x.id === 'give-up')!.lines[0] = long + ' ' + long; }))).toContain('D-2');
  });
  test('a scope run checks its own blocks: the street scope against the pinned lexicon, and fails without it', () => {
    const v = standAll();
    const ok = checkDialogue({ dr: T.dr, rules: T.prules, rb: T.rb, people: T.people, voices: [v.street!, v.lexicon!], scope: 'street', seed: 's', reference: text });
    expect(ok.checks.filter(c => !c.ok).map(c => c.id)).toEqual([]);
    expect(ok.counts.persons).toBe(T.people.persons.filter(p => ['counter', 'workshop', 'resident'].includes(groupOf(T.dr, p.position)!)).length);
    const no = checkDialogue({ dr: T.dr, rules: T.prules, rb: T.rb, people: T.people, voices: [v.street!], scope: 'street', seed: 's', reference: text });
    expect(no.ok).toBe(true); // the stand-in cells use only {address}: nothing needs the lexicon
    v.street!.groups[0]!.cells[0]!.lines[0] = 'About {own}, {address}.';
    const bad = checkDialogue({ dr: T.dr, rules: T.prules, rb: T.rb, people: T.people, voices: [v.street!], scope: 'street', seed: 's' });
    expect(bad.checks.filter(c => !c.ok).map(c => c.id)).toContain('D-3');
    expect(() => checkDialogue({ dr: T.dr, rules: T.prules, rb: T.rb, people: T.people, voices: [], scope: 'nowhere', seed: 's' })).toThrow(/unknown scope/);
    expect(checkDialogue({ dr: T.dr, rules: T.prules, rb: T.rb, people: T.people, voices: [v.lexicon!], scope: 'street', seed: 's' }).ok).toBe(false); // no tables for the scope
  });
});

describe('the host\'s lookup and pick (reference for the port)', () => {
  const c = (): ExpandCtx => ({ people: LOVERS, rules: T.prules, dr: T.dr, rb: T.rb, tables: merge(Object.values(standAll())).merged, seed: 's' });
  const file = () => { const x = c(), { persons } = expandAll(x, ALL(T.dr)); return linesFile(x, persons, { people: 'p', rules: 'r', tables: {} }, { id: 'harbour-district', version: 'v' }); };
  test('a person\'s entry is found by event, verb, outcome and temperament; the Scout\'s read-outs by `scout`; a lover\'s summons by side', () => {
    const f = file(), srv = f.persons.find(p => p.group === 'counter' && p.temperament === 'fearful')!, lover = f.persons.find(p => p.secret === 'affair')!;
    const e = lookupLine(f, srv.id, { event: 'reply', verb: 'help', outcome: 'turned', temperament: 'fearful' })!;
    expect(e.lines).toEqual(srv.entries.find(x => x.event === 'reply' && x.verb === 'help' && x.outcome === 'turned')!.lines);
    expect(lookupLine(f, srv.id, { event: 'reply', verb: 'pay', outcome: 'turned', temperament: 'idealist' })).toBeUndefined(); // not this person's temperament
    expect(lookupLine(f, srv.id, { event: 'read', verb: 'listen', temperament: 'fearful', speaker: 'scout' })!.speaker).toBe('scout');
    expect(lookupLine(f, srv.id, { event: 'read', verb: 'watch', speaker: 'scout' })!.lines.length).toBeGreaterThanOrEqual(2);
    const a = lookupLine(f, lover.id, { event: 'summon', outcome: 'summon-busy', side: 'caller' })!, b = lookupLine(f, lover.id, { event: 'summon', outcome: 'summon-busy', side: 'summoned' })!;
    expect(a.side).toBe('caller'); expect(b.side).toBe('summoned'); expect(a.lines).not.toEqual(b.lines);
    expect(lookupLine(f, srv.id, { event: 'summon', outcome: 'summon-busy', side: 'caller' })).toBeUndefined(); // no lover, no summons
    expect(lookupLine(f, 'p-nobody-01', { event: 'reply' })).toBeUndefined();
  });
  test('the pick is deterministic, never repeats a variant back to back, and differs between people with one table', () => {
    const f = file(), same = f.persons.filter(p => p.group === 'workshop' && p.temperament === 'fearful').slice(0, 3), q = { event: 'reply', verb: 'help', outcome: 'turned', temperament: 'fearful' };
    const seq = (id: string) => Array.from({ length: 24 }, (_, n) => pickPersonLine(f, 'seed-1', id, q, n));
    expect(seq(same[0]!.id)).toEqual(seq(same[0]!.id));
    const s0 = seq(same[0]!.id); for (let i = 1; i < s0.length; i++) expect(s0[i]).not.toBe(s0[i - 1]);
    expect(new Set(s0).size).toBe(3);
    expect(same.length).toBe(3);
    expect(new Set(same.map(p => JSON.stringify(seq(p.id)))).size).toBeGreaterThan(1); // the key carries the person: they do not all say the same thing at the same moment
    expect(pickPersonLine(f, 'seed-1', same[0]!.id, { event: 'nothing' }, 0)).toBeUndefined();
  });
  test('test vectors for the C# port: the key and the picks', () => {
    expect(personKey('p-cafe-02', { event: 'reply', verb: 'help', outcome: 'turned', temperament: 'fearful' })).toBe('reply/help/p-cafe-02/turned/fearful');
    expect(personKey('p-cafe-02', { event: 'read', verb: 'listen', temperament: 'fearful', speaker: 'scout' })).toBe('read/listen/scout/fearful/p-cafe-02');
    expect(personKey('p-cafe-02', { event: 'summon', outcome: 'summon-busy', side: 'caller' })).toBe('summon/p-cafe-02/summon-busy/caller');
    expect([0, 1, 2, 3, 4, 5].map(n => pickIndex('seed-1', 'reply/help/p-cafe-02/turned/fearful', n, 3))).toEqual([0, 1, 2, 0, 1, 2]);
    expect([0, 1, 2, 3].map(n => pickIndex('seed-1', 'read/listen/scout/fearful/p-cafe-02', n, 2))).toEqual([1, 0, 1, 0]);
    expect([0, 1, 2, 3].map(n => pickIndex('seed-1', 'summon/p-cafe-02/summon-busy/caller', n, 2))).toEqual([1, 0, 1, 0]);
  });
  test('a scene is played by verb and tie, one variant by the n-th use, its Learned line kept for the host', () => {
    const f = file(), lover = f.persons.find(p => p.secret === 'affair')!;
    const a = playPersonScene(f, 'seed', lover.id, { verb: 'listen' }, 0)!, b = playPersonScene(f, 'seed', lover.id, { verb: 'listen' }, 1)!;
    expect(a.beats.length).toBeGreaterThanOrEqual(3); expect(a.result).toContain('{temperament}'); expect(a.learns).toContain('secret');
    expect(JSON.stringify(a.beats)).not.toBe(JSON.stringify(b.beats)); // two variants, never the same twice running
    expect(playPersonScene(f, 'seed', lover.id, { verb: 'call', tie: 'lover' }, 0)!.with).toBe(a.with);
    expect(playPersonScene(f, 'seed', lover.id, { verb: 'call', tie: 'family' }, 0)).toBeUndefined();
  });
});

describe('the Handover bound to generated people', () => {
  test('the four roles become text.json entries the game\'s own schema accepts, and the generated lines match the hand-written ones for the bound people', () => {
    const tables = standAll(), c: ExpandCtx = { people: LOVERS, rules: T.prules, dr: T.dr, rb: T.rb, tables: merge(Object.values(tables)).merged, seed: 's' }, { persons } = expandAll(c, ALL(T.dr));
    const lines = linesFile(c, persons, { people: 'p', rules: 'r', tables: {} }, { id: 'harbour-district', version: 'v' });
    const guard = LOVERS.persons.find(p => p.work?.role === 'guard' && p.secret === 'affair')!, waitress = LOVERS.persons.find(p => p.id === guard.ties.find(t => t.kind === 'lover')!.with)!;
    const driver = LOVERS.persons.find(p => p.work?.role === 'official-driver')!, councillor = LOVERS.persons.find(p => p.work?.role === 'councillor')!;
    const binding = { waitress: waitress.id, driver: driver.id, guard: guard.id, councillor: councillor.id };
    const { entries, problems } = bindHandover(lines, binding);
    expect(problems).toEqual([]);
    for (const e of entries) expect(Entry.safeParse(e).success, JSON.stringify(e)).toBe(true);
    expect(entries.filter(e => e.speaker === 'waitress' && e.event === 'reply').length).toBeGreaterThanOrEqual(6);
    expect(entries.some(e => e.speaker === 'guard' && e.event === 'summon-refused' && e.outcome === 'summon-busy')).toBe(true);
    expect(entries.some(e => e.speaker === 'waitress' && e.event === 'summon-refused' && e.outcome === 'summon-busy')).toBe(true);
    expect(entries.some(e => e.speaker === 'waitress' && e.event === 'guard-recalled')).toBe(true);
    expect(entries.some(e => e.speaker === 'scout' && e.event === 'read' && e.about === 'councillor')).toBe(true);
    const cmp = compareToReference(entries, text, { waitress: waitress.temperament, driver: driver.temperament, guard: guard.temperament, councillor: councillor.temperament });
    expect(cmp.missing).toEqual([]); expect(cmp.fewer).toEqual([]); expect(cmp.matched).toBeGreaterThan(20);
    expect(bindHandover(lines, { waitress: 'p-nobody-01' }).problems.length).toBe(1);
    // a person with fewer lines than the hand-written text is found
    const thin = entries.filter(e => !(e.speaker === 'driver' && e.event === 'confirm'));
    expect(compareToReference(thin, text, { driver: driver.temperament }).missing.length).toBeGreaterThan(0);
  });
});

describe('promoting tables', () => {
  const PLAN_ID = 'harbour-district';
  function repo(edit?: (dir: string) => void) {
    const dir = mkdtempSync(join(tmpdir(), 'promote-')), runDir = join(dir, 'runs/levels', PLAN_ID, 'r001'), voices = standIn('street', T.dr, T.rb, T.prules, text);
    mkdirSync(join(runDir, 'round-2'), { recursive: true });
    writeFileSync(join(runDir, 'round-2/fable.json'), JSON.stringify({ voices, notes: [], sources: [] }, null, 2));
    writeFileSync(join(runDir, 'run.json'), JSON.stringify({ run: 'r001', outcome: 'ACCEPTED', objective: 'dialogue', scope: 'street', finalVersion: '0.4.0-r001.2', loop: 2, rounds: [{ round: 1 }, { round: 2 }] }));
    edit?.(dir);
    return { dir, voices };
  }
  const opt = { run: 'r001', scope: 'street', by: 'Sky, 2026-10-16', why: 'test', plan: PLAN_ID };
  test('an accepted run is promoted byte for byte, held to its record, and its round is the last of its rounds (not the loop rule\'s number)', () => {
    const { dir, voices } = repo();
    try {
      const r = promoteRun(opt, dir);
      expect(r.problems).toEqual([]); expect(r.ok).toBe(true);
      expect(readFileSync(join(dir, tablesFileOf(PLAN_ID, 'street')), 'utf8')).toBe(voicesBytes(voices));
      expect(JSON.parse(readFileSync(join(dir, promotionFileOf(PLAN_ID, 'street')), 'utf8')).source).toMatchObject({ run: 'r001', round: 2, outcome: 'ACCEPTED', version: '0.4.0-r001.2' });
      expect(checkPromotion(PLAN_ID, 'street', dir).ok).toBe(true);
      expect(promoteRun(opt, dir).problems.join()).toContain('exists'); // never over an existing pair
    } finally { rmSync(dir, { recursive: true, force: true }); }
  });
  test('an edit to the tables, a run that changed, or a run that is not accepted fails', () => {
    const { dir } = repo();
    try {
      promoteRun(opt, dir);
      const f = join(dir, tablesFileOf(PLAN_ID, 'street'));
      writeFileSync(f, readFileSync(f, 'utf8').replace('"counter"', '"counte"'));
      const r = checkPromotion(PLAN_ID, 'street', dir);
      expect(r.ok).toBe(false); expect(r.problems.join(' | ')).toContain('were edited'); expect(r.problems.join(' | ')).toContain('byte for byte');
    } finally { rmSync(dir, { recursive: true, force: true }); }
    const { dir: d2 } = repo(d => { const f = join(d, 'runs/levels', PLAN_ID, 'r001/run.json'), o = JSON.parse(readFileSync(f, 'utf8')); o.outcome = 'REJECTED'; writeFileSync(f, JSON.stringify(o)); });
    try { const r = promoteRun(opt, d2); expect(r.ok).toBe(false); expect(r.problems[0]).toContain('only an ACCEPTED dialogue run'); expect(existsSync(join(d2, tablesFileOf(PLAN_ID, 'street')))).toBe(false); } finally { rmSync(d2, { recursive: true, force: true }); }
    const { dir: d3 } = repo();
    try { expect(promoteRun({ ...opt, scope: 'civic' }, d3).problems[0]).toContain('only an ACCEPTED dialogue run of scope civic'); expect(checkPromotion(PLAN_ID, 'lexicon', d3).problems[0]).toContain('no promotion record'); } finally { rmSync(d3, { recursive: true, force: true }); }
    const { dir: d4 } = repo();
    try {
      promoteRun(opt, d4);
      const f = join(d4, 'runs/levels', PLAN_ID, 'r001/round-2/fable.json'), o = JSON.parse(readFileSync(f, 'utf8')); o.notes = ['edited']; writeFileSync(f, JSON.stringify(o, null, 2));
      expect(checkPromotion(PLAN_ID, 'street', d4).problems.join(' | ')).toContain('the run\'s output changed');
    } finally { rmSync(d4, { recursive: true, force: true }); }
    void dirname;
  });
});

describe('bun run dialogue', () => {
  const cli = (...args: string[]) => Bun.spawnSync(['bun', join(root, 'tools/dialogue/cli.ts'), ...args], { cwd: root, stdout: 'pipe', stderr: 'pipe', env: { ...process.env } });
  test('the stand-in tables for the whole district pass, write the lines file, and the Handover roles are compared with the hand-written text', () => {
    const out = join(mkdtempSync(join(tmpdir(), 'cli-')), 'lines.json'), r = cli('--standin', '--handover', '--out', out), so = new TextDecoder().decode(r.stdout);
    expect(r.exitCode, so + new TextDecoder().decode(r.stderr)).toBe(0);
    expect(so).toContain('D-12'); expect(so).toContain('reference');
    const f = JSON.parse(readFileSync(out, 'utf8'));
    expect(Lines.safeParse(f).success).toBe(true);
    expect(f.persons.length).toBeGreaterThan(100); // the promoted cast's people, whatever promotion is current
    const again = join(dirname(out), 'again.json'); cli('--standin', '--out', again);
    expect(readFileSync(again, 'utf8')).toBe(readFileSync(out, 'utf8')); // same inputs, same bytes
  });
  test('a scope alone, with the lexicon it fills from', () => {
    const out = join(mkdtempSync(join(tmpdir(), 'cli-')), 'street.json'), r = cli('--standin', '--scope', 'street', '--out', out);
    expect(r.exitCode, new TextDecoder().decode(r.stdout)).toBe(0);
    expect(JSON.parse(readFileSync(out, 'utf8')).persons.length).toBeGreaterThan(40);
  });
  test('with no promoted tables it says so and exits 1; an unknown option or scope exits 2', () => {
    const r = cli('--scope', 'street'); expect(r.exitCode).toBe(1); expect(new TextDecoder().decode(r.stdout)).toContain('no promoted tables yet');
    expect(cli('--nope').exitCode).toBe(2);
    expect(cli('--scope', 'harbour', '--standin').exitCode).toBe(2);
  });
});
