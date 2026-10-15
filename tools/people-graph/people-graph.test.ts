// The people graph (tools/people-graph): the model against the people JSON it is made from, the SurrealQL it writes, the schema it is written under and the canned queries' files.
// No server is touched: the answers a SurrealDB gives are held to the JSON by `bun run people:graph verify`, and by the last test here when PEOPLE_GRAPH_LIVE=1 (a live load of the
// promoted cast into its own database, as the command does). Nothing here needs the database; a run or a replay never does.
import { beforeAll, describe, expect, test } from 'bun:test';
import { readFileSync, readdirSync } from 'node:fs';
import { join } from 'node:path';
import { Person } from '../../contracts/people/people.schema';
import { bind, literalFor, minutes, parseQuery, queryNames, readQuery, usage } from './queries';
import { buildModel, PERSON_FIELDS, Rec, regularAt, SCHEMA_VERSION, type Model } from './model';
import { endpointOf, render } from './render';
import * as ref from './reference';
import { castSource, hashOf, peopleDb, prepare, runCast, SCHEMA_FILE, type Prepared } from './source';
import { edgeStatement, literal, loadBatches, nodeStatement, recordId, TABLES } from './surql';

const root = join(import.meta.dir, '..', '..');
const clone = <T>(x: T): T => JSON.parse(JSON.stringify(x));
let p: Prepared, r: ref.Ref, schema: string;
beforeAll(async () => {
  p = await prepare();
  r = { people: p.people, rules: p.w.rules, extras: p.extras };
  schema = readFileSync(join(root, SCHEMA_FILE), 'utf8');
});
const edges = (t: string) => p.model.edges.filter(e => e.table === t);
const nodes = (t: string) => p.model.nodes.filter(n => n.table === t);
const id = (x: Rec) => x.id;

describe('SurrealQL text', () => {
  test('record ids are always bracketed, so a hyphen is never an operator; an id that could break out is refused', () => {
    expect(recordId('person', 'p-cafe-01')).toBe('person:⟨p-cafe-01⟩');
    expect(() => recordId('person', 'a⟩; DELETE person;')).toThrow(/bad record id/);
    expect(() => recordId('person', 'a\nb')).toThrow(/bad record id/);
    expect(() => recordId('Person; x', 'a')).toThrow(/bad table/);
  });
  test('literals: strings are escaped, records are record ids, undefined is left out, null and NaN are refused', () => {
    expect(literal({ a: 'x"y', b: new Rec('need', 'debt'), c: undefined, d: [1, -2.5, true], e: { 'pre-shift': 3 } })).toBe('{"a": "x\\"y", "b": need:⟨debt⟩, "d": [1, -2.5, true], "e": {"pre-shift": 3}}');
    expect(() => literal({ a: null })).toThrow(/null/);
    expect(() => literal(NaN)).toThrow(/non-finite/);
  });
  test('nodes are CREATEd and edges RELATEd, with the edge record id', () => {
    const t = edges('tie')[0]!;
    expect(edgeStatement(t)).toMatch(new RegExp(`^RELATE person:⟨${t.from.id}⟩->tie:⟨${t.id.replace(/[~]/g, '~')}⟩->person:⟨${t.to.id}⟩ CONTENT \\{`));
    expect(nodeStatement(nodes('person')[0]!)).toMatch(/^CREATE person:⟨p-[a-z0-9-]+⟩ CONTENT \{"name": /);
  });
  test('every row of the model is in one batch, lookups first and links after what they point to', () => {
    const batches = loadBatches(p.model, 50), all = batches.flat();
    expect(all).toHaveLength(p.model.nodes.length + p.model.edges.length);
    expect(Math.max(...batches.map(b => b.length))).toBeLessThanOrEqual(50);
    const first = (re: RegExp) => all.findIndex(s => re.test(s));
    expect(first(/^CREATE temperament:/)).toBeLessThan(first(/^CREATE person:/));
    expect(first(/^CREATE place:/)).toBeLessThan(first(/^CREATE unit:/));
    expect(first(/^CREATE person:/)).toBeLessThan(first(/^RELATE person:.*->tie:/));
    expect(all.filter(s => s.startsWith('CREATE ')).length).toBe(p.model.nodes.length);
  });
  test('a table outside the load order is refused (unknown = reject)', () => {
    const m: Model = { ...p.model, nodes: [...p.model.nodes, { table: 'mystery', id: 'x', fields: {} }] };
    expect(() => loadBatches(m)).toThrow(/not in the load order/);
  });
});

describe('the schema', () => {
  const defined = (table: string) => new Set([...schema.matchAll(new RegExp(`DEFINE FIELD IF NOT EXISTS ([\\w.]+) ON ${table} `, 'g'))].map(m => m[1]!));
  test('every table is SCHEMAFULL, and every edge table is a relation between the tables it links', () => {
    for (const t of TABLES) expect(schema).toMatch(new RegExp(`DEFINE TABLE IF NOT EXISTS ${t} (TYPE RELATION IN \\w+ OUT \\w+ ENFORCED )?SCHEMAFULL;`));
    const edgeTables = ['lives_at', 'works_at', 'tie', 'stops_at', 'sits_at', 'knows_need_of', 'has_need', 'has_temperament', 'casts'];
    for (const t of edgeTables) expect(schema).toMatch(new RegExp(`DEFINE TABLE IF NOT EXISTS ${t} TYPE RELATION IN \\w+ OUT \\w+ ENFORCED SCHEMAFULL;`));
    expect(schema).not.toMatch(/SCHEMALESS/);
  });
  test('every field the model writes is defined on its table: nothing is written that the schema would drop', () => {
    for (const t of TABLES) {
      const rows = [...p.model.nodes, ...p.model.edges].filter(x => x.table === t), keys = new Set(rows.flatMap(x => Object.entries(x.fields).filter(([, v]) => v !== undefined).map(([k]) => k)));
      const d = defined(t);
      for (const k of keys) expect(d.has(k) ? k : `${t}.${k} is not defined`).toBe(k);
    }
  });
  test('the person fields: every field the people schema carries lands somewhere (a new field is a decision, not a silent drop)', () => {
    expect(Object.keys(PERSON_FIELDS).sort()).toEqual(Object.keys(Person.shape).sort());
  });
  test('the tie kinds, needs and temperaments are the rules\' own, no more and no fewer', () => {
    expect(nodes('tie_kind').map(n => n.id).sort()).toEqual(Object.keys(r.rules.ties.kinds).sort());
    expect(nodes('need').map(n => n.id).sort()).toEqual(Object.keys(r.rules.needs.list).sort());
    expect(nodes('temperament').map(n => n.id).sort()).toEqual(Object.keys(r.rules.temperaments).sort());
    for (const n of nodes('tie_kind')) expect(n.fields.introduces).toBe(r.rules.ties.kinds[n.id as keyof typeof r.rules.ties.kinds].introduces);
  });
});

describe('the model is the JSON', () => {
  test('one person node per person, with every scalar the JSON carries', () => {
    expect(nodes('person')).toHaveLength(p.people.persons.length);
    for (const q of p.people.persons) {
      const n = nodes('person').find(x => x.id === q.id)!.fields;
      expect(n).toMatchObject({ name: q.name, tier: 'simulated', position: q.position, institution: q.institution ?? undefined, start: q.start, memory: [], look: q.look.block, routine_variant: q.routine.variant, home_plot: q.home.plot, need_weights: q.needWeights });
      expect(id(n.temperament as Rec)).toBe(q.temperament); expect(id(n.need as Rec)).toBe(q.need);
    }
  });
  test('ties: rebuilt from the edges they are the JSON\'s ties, a two-way tie on both people, a directed tie on its source; the rules\' introduces and mutual flags ride on the edge', () => {
    const T = r.rules.ties.kinds;
    for (const q of p.people.persons) {
      const got = [...edges('tie').filter(e => e.from.id === q.id).map(e => ({ kind: id(e.fields.kind as Rec), with: e.to.id })), ...edges('tie').filter(e => e.to.id === q.id && e.fields.mutual).map(e => ({ kind: id(e.fields.kind as Rec), with: e.from.id }))];
      const key = (t: { kind: string; with: string }) => `${t.kind}/${t.with}`;
      expect(got.map(key).sort()).toEqual(q.ties.map(key).sort());
    }
    for (const e of edges('tie')) {
      const kind = id(e.fields.kind as Rec) as keyof typeof T;
      expect(e.fields.introduces).toBe(T[kind].introduces); expect(e.fields.mutual).toBe(T[kind].direction === 'both');
      if (e.fields.mutual) expect(e.from.id < e.to.id).toBe(true);
    }
    const entries = p.people.persons.reduce((n, q) => n + q.ties.length, 0), mutual = edges('tie').filter(e => e.fields.mutual).length;
    expect(edges('tie').length).toBe(entries - mutual);
  });
  test('a regular tie names the stop of the carrier\'s day it is made for', () => {
    const regs = edges('tie').filter(e => id(e.fields.kind as Rec) === 'regular');
    expect(regs.length).toBeGreaterThan(0);
    for (const e of regs) expect(['pre-shift', 'evening', 'meal', 'errand', 'commute', 'work']).toContain(e.fields.at as string);
    for (const e of edges('tie').filter(x => id(x.fields.kind as Rec) !== 'regular')) expect(e.fields.at).toBeUndefined();
    const byId = new Map(p.people.persons.map(q => [q.id, q]));
    expect(regularAt(byId.get(regs[0]!.from.id)!, byId.get(regs[0]!.to.id)!, p.extras)).toBe(regs[0]!.fields.at as string);
  });
  test('stops_at is the routine, stop for stop; sits_at is the stops at a chair, and no chair is held by two at once', () => {
    for (const q of p.people.persons) {
      const got = edges('stops_at').filter(e => e.from.id === q.id).sort((a, b) => (a.fields.seq as number) - (b.fields.seq as number));
      expect(got.map(e => [e.to.id, e.fields.doing, e.fields.from_min, e.fields.to_min])).toEqual(q.routine.stops.map(s => [s.place, s.doing, s.from, s.to]));
    }
    const seats = new Set(p.people.places.filter(x => x.kind === 'seat').map(x => x.id)), stops = p.people.persons.flatMap(q => q.routine.stops.filter(s => seats.has(s.place)).map(s => `${q.id}/${s.place}/${s.from}`));
    expect(edges('sits_at').map(e => `${e.from.id}/${e.to.id}/${e.fields.from_min}`).sort()).toEqual(stops.sort());
    const by = new Map<string, [number, number][]>();
    for (const e of edges('sits_at')) { const held = by.get(e.to.id) ?? []; for (const [a, b] of held) expect(!(e.fields.from_min as number < b && a < (e.fields.to_min as number))).toBe(true); held.push([e.fields.from_min as number, e.fields.to_min as number]); by.set(e.to.id, held); }
  });
  test('works_at, lives_at, has_need and has_temperament: one each per person (works_at: per person with work), with the position, post, place, shift and weight', () => {
    for (const q of p.people.persons) {
      expect(edges('lives_at').filter(e => e.from.id === q.id).map(e => e.to.id)).toEqual([q.home.place]);
      expect(edges('has_temperament').filter(e => e.from.id === q.id).map(e => e.to.id)).toEqual([q.temperament]);
      const hn = edges('has_need').filter(e => e.from.id === q.id);
      expect(hn.map(e => [e.to.id, e.fields.weight])).toEqual([[q.need, q.needWeights[q.need]]]);
      const w = edges('works_at').filter(e => e.from.id === q.id);
      if (!q.work) expect(w).toHaveLength(0);
      else expect(w.map(e => [e.to.id, e.fields.position, e.fields.post, id(e.fields.place as Rec), e.fields.shift_from, e.fields.shift_to])).toEqual([[q.work.unit, q.work.position ?? q.work.role, q.work.post, q.work.place, q.work.shift.from, q.work.shift.to]]);
    }
  });
  test('knows_need_of is the rules\' knowledge, keeper by keeper', () => {
    for (const k of p.people.knowledge!.needsKnown) expect(edges('knows_need_of').filter(e => e.from.id === k.keeper).map(e => e.to.id).sort()).toEqual([...k.knows].sort());
    expect(edges('knows_need_of').every(e => (e.fields.via as string[]).length > 0)).toBe(true);
  });
  test('the four Handover roles are nodes, each cast: casts holds everyone who fits, and one is pinned', () => {
    expect(nodes('role').map(n => n.id)).toEqual(['waitress', 'driver', 'guard', 'councillor']);
    for (const c of p.extras.casts) {
      const got = edges('casts').filter(e => e.from.id === c.role);
      expect(got.map(e => e.to.id).sort()).toEqual([...c.who].sort());
      expect(got.filter(e => e.fields.pinned).map(e => e.to.id)).toEqual(c.pinned ? [c.pinned] : []);
    }
  });
  test('places carry their unit and chair set, and the off-sheet place stands for "beyond"', () => {
    expect(nodes('place')).toHaveLength(p.people.places.length + (p.people.persons.some(q => q.home.place === 'beyond') ? 1 : 0));
    const cafeSeat = nodes('place').find(n => n.fields.kind === 'seat')!;
    expect(id(cafeSeat.fields.unit as Rec)).toBe(p.extras.cafeUnit); expect(cafeSeat.fields.set).toMatch(/^cafe-table-\d+$/);
    for (const u of nodes('unit')) if (u.fields.door) expect(nodes('place').find(n => n.id === (u.fields.door as Rec).id)).toBeDefined();
  });
  test('every record an edge links to exists as a node: nothing dangles', () => {
    const have = new Set([...p.model.nodes].map(n => `${n.table}:${n.id}`));
    for (const e of p.model.edges) for (const x of [e.from, e.to, ...Object.values(e.fields).filter((v): v is Rec => v instanceof Rec)]) expect(have.has(`${x.table}:${x.id}`) ? '' : `${e.table} ${e.id} links to ${x.table}:${x.id}`).toBe('');
    for (const n of p.model.nodes) for (const v of Object.values(n.fields)) if (v instanceof Rec) expect(have.has(`${v.table}:${v.id}`) ? '' : `${n.table} ${n.id} links to ${v.table}:${v.id}`).toBe('');
  });
  test('counts say what is in the model', () => {
    for (const t of TABLES) {
      const n = p.model.nodes.filter(x => x.table === t).length + p.model.edges.filter(x => x.table === t).length;
      expect((p.model.counts.nodes[t] ?? 0) + (p.model.counts.edges[t] ?? 0)).toBe(n);
    }
    expect(Object.values(p.model.counts.ties).reduce((a, b) => a + b, 0)).toBe(edges('tie').length);
  });
});

describe('the graph answers as the JSON does (the model, walked in TypeScript)', () => {
  // The same questions the canned queries ask, asked of the model's edges instead of the people file: if the model lost or invented a tie, a stop or a role, these fail.
  const adj = (introducing: boolean) => {
    const a = new Map<string, Set<string>>(p.people.persons.map(q => [q.id, new Set()]));
    for (const e of edges('tie')) if (!introducing || e.fields.introduces) { a.get(e.from.id)!.add(e.to.id); a.get(e.to.id)!.add(e.from.id); }
    return a;
  };
  test('who-can-introduce: the shortest introducing chain between pairs equals the JSON\'s, reachable or not', () => {
    const ids = p.people.persons.map(q => q.id).sort(), g = adj(true);
    let reachable = 0, unreachable = 0;
    for (let i = 0; i < ids.length; i += 3) for (const b of [ids[(i * 7 + 13) % ids.length]!, ids[(i + 65) % ids.length]!]) {
      const want = ref.introHops(r, ids[i]!, b), got = ref.distances(g, ids[i]!).get(b);
      expect(got).toBe(want); want === undefined ? unreachable++ : reachable++;
    }
    expect(reachable).toBeGreaterThan(20);
    const pins = p.extras.casts.map(c => c.pinned!);
    expect(ref.introHops(r, pins[0]!, pins[3]!)).toBeGreaterThan(0);
    expect(unreachable).toBeGreaterThanOrEqual(0);
  });
  test('introductions do not run along debtor or rival ties: a chain over every tie can be shorter', () => {
    const ids = p.people.persons.map(q => q.id), all = adj(false), intro = adj(true);
    const shorter = ids.some(a => { const d1 = ref.distances(all, a), d2 = ref.distances(intro, a); return [...d1].some(([b, n]) => (d2.get(b) ?? 99) > n); });
    expect(shorter).toBe(true);
    for (const e of edges('tie')) if (!e.fields.introduces) expect(['debtor', 'rival']).toContain(id(e.fields.kind as Rec));
  });
  test('neighbourhood: 1 and 2 hops over every tie kind equal the JSON\'s', () => {
    const g = adj(false);
    for (const q of p.people.persons.slice(0, 40)) {
      const d = ref.distances(g, q.id), want = ref.neighbourhood(r, q.id);
      expect([...d].filter(([, n]) => n === 1).map(([k]) => k).sort()).toEqual(want.hop1);
      expect([...d].filter(([, n]) => n === 2).map(([k]) => k).sort()).toEqual(want.hop2);
    }
  });
  test('regulars-of and guards-who-are-cafe-regulars: the regular ties into the café\'s staff, read off the edges', () => {
    const staff = new Set(edges('works_at').filter(e => e.to.id === p.extras.cafeUnit).map(e => e.from.id));
    const viaEdges = (position: string) => [...new Set(edges('tie').filter(e => id(e.fields.kind as Rec) === 'regular' && staff.has(e.to.id)).map(e => e.from.id))].filter(x => p.people.persons.find(q => q.id === x)!.position === position).sort();
    expect(viaEdges('guard')).toEqual(ref.regularsOfUnit(r, 'guard', p.extras.cafeUnit).map(x => x.id));
    expect(viaEdges('guard').length).toBeGreaterThan(0);
    const door = p.extras.units.find(u => u.id === p.extras.cafeUnit)!.door!;
    expect(ref.regularsOf(r, door).map(x => x.id)).toEqual(ref.regularsOf(r, p.extras.cafeSide[0]!).map(x => x.id));
    expect(ref.regularsOf(r, 'home-rocket-west')).toEqual([]);
  });
  test('who-is-near: the stops overlapping a time within a radius, nearest first; a smaller radius is a subset', () => {
    const door = p.extras.units.find(u => u.id === p.extras.cafeUnit)!.door!, wide = ref.whoIsNear(r, door, 1055, 13), tight = ref.whoIsNear(r, door, 1055, 4);
    expect(wide.length).toBeGreaterThan(tight.length);
    expect(tight.every(t => wide.some(w => w.person === t.person && w.place === t.place))).toBe(true);
    expect(wide.every((w, i) => i === 0 || wide[i - 1]!.metres <= w.metres)).toBe(true);
    expect(wide.every(w => edges('stops_at').some(e => e.from.id === w.person && e.to.id === w.place && e.fields.from_min === w.from && e.fields.to_min === w.to))).toBe(true);
  });
  test('needs-known-by-keeper and role-fit: read off the same edges', () => {
    for (const k of p.people.knowledge!.needsKnown) {
      const want = ref.needsKnownBy(r, k.keeper), got = edges('knows_need_of').filter(e => e.from.id === k.keeper).map(e => e.to.id).sort();
      expect(want.map(x => x.id)).toEqual(got);
    }
    const fit = ref.roleFit(r);
    expect(fit.map(f => f.role)).toEqual(['waitress', 'driver', 'guard', 'councillor']);
    for (const f of fit) { expect(f.people.filter(x => x.pinned)).toHaveLength(1); expect(f.people[0]!.pinned).toBe(true); }
  });
});

describe('unknown = reject', () => {
  const build = (mutate: (people: Prepared['people']) => void) => { const people = clone(p.people); mutate(people); return () => buildModel(people, p.w.rules, p.extras); };
  test('a tie kind the rules do not list', () => { expect(build(x => { x.persons[0]!.ties.push({ kind: 'cousin', with: x.persons[1]!.id }); })).toThrow(/tie kind cousin is not in the rules/); });
  test('a two-way tie that is not returned', () => { expect(build(x => { const a = x.persons.find(q => q.ties.some(t => t.kind === 'workmate'))!, t = a.ties.find(y => y.kind === 'workmate')!; x.persons.find(q => q.id === t.with)!.ties = []; })).toThrow(/not returned/); });
  test('a tie to someone who is not a person', () => { expect(build(x => { x.persons[0]!.ties.push({ kind: 'family', with: 'p-nobody-01' }); })).toThrow(/unknown p-nobody-01/); });
  test('a need or temperament the rules do not list, or a need the row never offered', () => {
    expect(build(x => { x.persons[0]!.need = 'wealth'; })).toThrow(/need wealth is not in the rules/);
    expect(build(x => { x.persons[0]!.temperament = 'cheerful'; })).toThrow(/temperament cheerful is not in the rules/);
    expect(build(x => { x.persons[0]!.needWeights = { cash: 1000 }; x.persons[0]!.need = x.persons[0]!.need === 'cash' ? 'debt' : x.persons[0]!.need; })).toThrow(/not among the needs its row offers/);
  });
  test('a stop at a place that is not in the file', () => { expect(build(x => { x.persons[0]!.routine.stops[0]!.place = 'moon-base'; })).toThrow(/moon-base/); });
  test('a keeper who knows someone who is not a person', () => { expect(build(x => { x.knowledge!.needsKnown[0]!.knows.push('p-ghost-01'); })).toThrow(/p-ghost-01/); });
});

describe('the hash, the database and the source', () => {
  test('the same inputs make the same model and hash; a changed person, tie or schema version makes another', () => {
    const again = buildModel(clone(p.people), p.w.rules, p.extras);
    expect(hashOf(again, schema)).toBe(p.hash);
    const moved = clone(p.people); moved.persons[0]!.name = 'Someone Else';
    expect(hashOf(buildModel(moved, p.w.rules, p.extras), schema)).not.toBe(p.hash);
    expect(hashOf(p.model, `${schema}\n-- a changed schema`)).not.toBe(p.hash);
    expect(SCHEMA_VERSION).toBeGreaterThanOrEqual(1);
  });
  test('the database is p_ and 16 hex digits of the hash, a name of its own per people hash', () => {
    expect(p.db).toMatch(/^p_[0-9a-f]{16}$/); expect(p.db).toBe(peopleDb(p.hash));
    expect(peopleDb('0'.repeat(64))).not.toBe(peopleDb('1'.repeat(64)));
    expect(p.db).not.toMatch(/^c_/);
  });
  test('the promoted cast is the default source, held to its record; a run id must look like one; two sources are refused', () => {
    expect(p.source).toMatchObject({ kind: 'promoted', problems: [] });
    expect(() => runCast('harbour-district', '../etc')).toThrow(/r015/);
    expect(() => runCast('harbour-district', 'r999')).toThrow(/no run r999/);
    expect(() => castSource('harbour-district', { run: 'r015', cast: 'x.json' })).toThrow(/two sources/);
    expect(() => castSource('harbour-district', { cast: 'no/such/cast.json' })).toThrow(/no cast file/);
  });
  test('a run\'s cast is its accepted round\'s, and expands like any cast', async () => {
    const run = runCast('harbour-district', 'r015'), src = castSource('harbour-district', { run: 'r015' });
    expect(run.outcome).toBe('ACCEPTED'); expect(run.label).toMatch(/^run r015 round \d+ \(ACCEPTED\)$/); expect(src.kind).toBe('run'); expect(src.problems).toEqual([]);
    const q = await prepare({ run: 'r015' });
    expect(q.checks).toMatchObject({ ok: true, failed: [], gaps: [] });
    expect(q.db).not.toBe(p.db);
  });
  test('the Surrealist endpoint is printed without credentials', () => {
    expect(endpointOf('http://suborned:secret-word@100.1.2.3:18504/')).toBe('http://100.1.2.3:18504');
    expect(endpointOf('http://127.0.0.1:18504')).toBe('http://127.0.0.1:18504');
    for (const f of readdirSync(join(root, 'tools/people-graph')).filter(x => x.endsWith('.ts') && !x.endsWith('.test.ts'))) expect(readFileSync(join(root, 'tools/people-graph', f), 'utf8')).not.toMatch(/console\.\w+\([^)]*(SURREAL_PASS|password)/i);
  });
});

describe('the canned queries', () => {
  const names = queryNames();
  test('the queries the job asks for are there', () => {
    for (const n of ['who-can-introduce', 'regulars-of', 'who-is-near', 'guards-who-are-cafe-regulars', 'needs-known-by-keeper', 'role-fit', 'neighbourhood', 'graph-view']) expect(names).toContain(n);
  });
  test('every file reads: a name, an about line, params that are set by a LET line, and a defaults that run as pasted', () => {
    for (const n of names) {
      const q = readQuery(n);
      expect(q.about.length).toBeGreaterThan(20);
      expect(bind(q, [])).toBe(q.text);
      expect(q.text).not.toMatch(/password|SURREAL_PASS/i);
      expect(usage(q)).toContain(n);
    }
  });
  test('every query is read-only', () => {
    for (const n of names) expect(readQuery(n).text.replace(/^--.*$/gm, '')).not.toMatch(/\b(CREATE|UPDATE|UPSERT|DELETE|RELATE|INSERT|REMOVE|DEFINE|ALTER)\b/);
  });
  test('arguments set the LET lines and nothing else; "at" before a time is read as a word', () => {
    const q = readQuery('regulars-of');
    expect(bind(q, ['cafe-stand-03', 'at', '09:15'])).toContain('LET $place = place:⟨cafe-stand-03⟩;\nLET $at = 555;');
    expect(bind(q, ['cafe-stand-03', '09:15'])).toBe(bind(q, ['cafe-stand-03', 'at', '09:15']));
    expect(bind(q, ['cafe-stand-03'])).toContain('LET $at = NONE;');
    expect(bind(readQuery('who-is-near'), ['office-entrance', '17:35', '4'])).toContain('LET $radius = 4;');
    expect(bind(readQuery('who-can-introduce'), ['p-a-01', 'p-b-02'])).toContain('LET $b = person:⟨p-b-02⟩;');
    const lines = (s: string) => s.split('\n');
    expect(lines(bind(q, ['x', '01:00'])).filter((l, i) => l !== lines(q.text)[i])).toHaveLength(2);
  });
  test('an argument is an id, a time or a number, never SurrealQL', () => {
    const q = readQuery('who-can-introduce');
    for (const bad of ['a⟩; REMOVE DATABASE x', 'p-a-01; DELETE person', '"x"', 'P-A-01', '', 'a b']) expect(() => bind(q, [bad])).toThrow(/is not a person id/);
    expect(() => bind(readQuery('regulars-of'), ['cafe-entrance', '25:00'])).toThrow(/not a time/);
    expect(() => bind(readQuery('regulars-of'), ['cafe-entrance', 'noon'])).toThrow(/hh:mm/);
    expect(() => bind(readQuery('who-is-near'), ['cafe-entrance', '07:00', '13; DELETE'])).toThrow(/whole number/);
    expect(() => bind(readQuery('who-is-near'), ['cafe-entrance', '07:00', '13', 'extra'])).toThrow(/extra/);
    expect(() => readQuery('../schema')).toThrow(/no query/);
    expect(minutes('24:00')).toBe(1440); expect(minutes('7:05')).toBe(425);
    expect(literalFor({ name: 'at', type: 'time', default: 'none', about: '' }, 'NONE')).toBe('NONE');
  });
  test('a file with a param and no LET line, or an unknown param type, is refused', () => {
    expect(() => parseQuery('x', '-- query: x\n-- about: a thing to ask\n-- param $a person p-a-01\nRETURN 1;')).toThrow(/no "LET \$a/);
    expect(() => parseQuery('x', '-- query: x\n-- about: a thing to ask\n-- param $a colour red\nLET $a = 1;')).toThrow(/type colour/);
    expect(() => parseQuery('y', '-- query: x\n-- about: a thing to ask\n')).toThrow(/must start with/);
  });
  test('answers print as a table when they are rows, as key: value lines with tables under them when they are an object', () => {
    expect(render([{ a: 1, b: 'x' }, { a: 22, b: null }])).toBe('a   b\n--  -\n1   x\n22');
    expect(render({ a: 1, b: [{ c: 2 }], d: { e: 3 } })).toBe('a: 1\nb:\n  c\n  -\n  2\nd: {"e":3}');
    expect(render('x')).toBe('"x"');
    expect(render([])).toBe('[]');
  });
});

// With PEOPLE_GRAPH_LIVE=1 and a server up (bun run up, or .env pointing at one): load the promoted cast into its own p_ database, as `bun run people:graph` does, and hold every row
// and every canned query to the JSON (bun run people:graph verify). Off by default: a test never needs the database, and never writes to the live server unasked.
describe('against a live server (PEOPLE_GRAPH_LIVE=1)', () => {
  test.skipIf(!process.env.PEOPLE_GRAPH_LIVE)('the loaded graph answers as the JSON does', async () => {
    const { loadGraph } = await import('./load'), { verify } = await import('./verify');
    await loadGraph(p);
    const checks = await verify(p.db, p);
    expect(checks.filter(c => !c.ok).map(c => `${c.name}: ${c.detail}`)).toEqual([]);
    expect(checks.length).toBeGreaterThan(20);
  }, 120000);
});

