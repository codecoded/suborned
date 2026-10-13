import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { root } from '../../../rag/config';
import { applySkyline, kitStatus, type Known } from '../apply';
import { parseConcepts, tileSet } from '../concepts';
import { KIT, datum, loadPlan, portal, signature, site, stations, tidy, vantage } from './edits';

const plan = await loadPlan();
const index = parseConcepts(readFileSync(resolve(root, 'contracts/world/skyline-concepts.json'), 'utf8'));
const kit = kitStatus(JSON.parse(readFileSync(KIT, 'utf8')));
const known: Known = { tiles: tileSet(index), kit };
const run = (d = datum({}), v = vantage()) => applySkyline(plan, { datum: d, vantage: v }, '0.3.0-r001.1', known);
const texts = (a: ReturnType<typeof run>) => a.problems.map(p => `${p.owner}: ${p.text}`);
const ids = (list: unknown) => (list as { id: string }[]).map(x => x.id);

describe('skyline apply', () => {
  test('tidy proposals keep every field they leave out, add the tiles, touch no standard but the tower sites', () => {
    const before = JSON.stringify(plan), a = run(tidy(plan), vantage({ stops: stations(plan) }));
    expect(a.problems).toEqual([]); expect(JSON.stringify(plan)).toBe(before); // the input plan is not mutated
    expect(a.plan.version).toBe('0.3.0-r001.1');
    const find = (list: unknown, id: string) => (list as Record<string, unknown>[]).find(x => x.id === id)!;
    const sig = find(a.plan.signatures, 'ocean-liner'), old = find(plan.signatures, 'ocean-liner');
    expect(sig.conceptTiles).toEqual(['blocks/saucer_spires']);
    expect({ ...sig, conceptTiles: undefined }).toEqual({ ...old, conceptTiles: undefined });
    type Gate = { opening: { head: string } };
    const gate = (p: typeof plan) => (find(p.megastructures, 'parade-galleries').portals as Gate[])[0]!;
    expect(gate(a.plan)).toMatchObject({ conceptTiles: ['blocks/rail_through'] });
    expect(gate(a.plan).opening.head).toBe(gate(plan).opening.head);
    const { sites: s1, ...t1 } = a.plan.standards.buildings.towers;
    const { sites: s0, ...t0 } = plan.standards.buildings.towers;
    expect(t1).toEqual(t0); expect(s1.length).toBe(s0.length);
    expect(find(s1, 'crescent-terrace').conceptTiles).toEqual(['blocks/mixed_skyline']);
    expect({ ...a.plan.standards, buildings: 0 }).toEqual({ ...plan.standards, buildings: 0 });
  });

  test("stations restated without in, kit and kitWhy keep the plan's", () => {
    const a = run(datum({}), vantage({ stops: stations(plan) }));
    const s1 = (p: typeof plan) => p.skyrail.find(l => l.id === 'S1')!;
    expect(a.problems).toEqual([]); expect(s1(a.plan).stations).toEqual(s1(plan).stations);
  });

  test('what a proposal cannot name is a problem for its owner, never a silent fix', () => {
    const d = datum({
      towerSites: [site(plan, 'crescent-terrace', { concepts: ['blocks/nonexistent'], plot: 'no-such-plot' })],
      signatures: [signature(plan, 'ocean-liner', { areas: ['atlantis'], kit: 'NoSuchPiece' })],
      portals: [portal(plan, 'parade-galleries', 'parade-gate', { structure: 'no-such-structure' })],
    });
    expect(texts(run(d))).toEqual([
      'Datum: signature ocean-liner: kit NoSuchPiece is not a piece in the kit catalogue',
      'Datum: signature ocean-liner: no such environmental area atlantis',
      'Datum: tower site crescent-terrace: concept tile "blocks/nonexistent" is not in the concept index',
      'Datum: tower site crescent-terrace: no such plot no-such-plot',
      'Datum: portal no-such-structure/parade-gate: no such megastructure no-such-structure',
    ]);
  });

  test('a rejected piece is refused, in a signature or a station', () => {
    const rejected = [...kit].find(([, s]) => s === 'rejected')?.[0];
    expect(rejected).toBeDefined();
    const d = datum({ signatures: [signature(plan, 'ocean-liner', { kit: rejected })] });
    const v = vantage({ stops: stations(plan).map(s => (s.id === 'gardens' ? { ...s, kit: rejected } : s)) });
    expect(texts(run(d, v)).filter(t => t.includes('rejected'))).toEqual([
      `Datum: signature ocean-liner: kit ${rejected} is rejected in the kit catalogue`,
      `Vantage: station S1/gardens: kit ${rejected} is rejected in the kit catalogue`,
    ]);
  });

  test("tram stops and unknown structures are Vantage's problems", () => {
    const tram = plan.tram[0]!.id;
    const other = { line: tram, id: 'x', name: 'X', path: 'running', at: [0, 0], fronts: 'circus-road' };
    const stops = [...stations(plan).map(s => (s.id === 'gardens' ? { ...s, in: 'signatures/nowhere' } : s)), other];
    expect(texts(run(datum({}), vantage({ stops } as never)))).toEqual([
      `Vantage: station ${tram}/x: the skyline objective places sky rail stations only`,
      'Vantage: station S1/gardens: no such structure signatures/nowhere',
    ]);
  });

  test('remove deletes by id and names what is missing; an id proposed twice is refused', () => {
    const a = run(datum({ remove: { signatures: ['ocean-liner', 'ghost'], towerSites: ['crescent-terrace'] } }));
    expect(ids(a.plan.signatures)).toEqual(['space-hq']);
    expect(ids(a.plan.standards.buildings.towers.sites)).not.toContain('crescent-terrace');
    expect(texts(a)).toEqual(['Datum: remove: no such signature ghost']);
    const twice = run(datum({ towerSites: [site(plan, 'crescent-terrace'), site(plan, 'crescent-terrace')] }));
    expect(texts(twice)).toEqual(['Datum: tower site crescent-terrace is proposed twice']);
  });

  test('rail.remove takes sky rail lines only: a tram line stays, an unknown id is named', () => {
    const tram = plan.tram[0]!.id, line = (id: string) => ({ rail: { lines: [], remove: [id] } });
    const t = run(datum(line(tram)));
    const why = 'a tram line; the skyline objective removes sky rail lines only';
    expect(texts(t)).toEqual([`Datum: rail.remove ${tram}: ${why}`]);
    expect(ids(t.plan.tram)).toEqual(ids(plan.tram));
    expect(texts(run(datum(line('S7'))))).toEqual(['Datum: rail.remove: no such sky rail line S7']);
    const gone = run(datum(line('S1')));
    expect(gone.problems).toEqual([]); expect(ids(gone.plan.skyrail)).not.toContain('S1');
    expect(ids(gone.plan.tram)).toEqual(ids(plan.tram));
  });

  describe('a signature piece: its regen variant is the piece that is built', () => {
    const rejected = (id: string) => kit.get(id) === 'rejected';
    const only = (id: string) => (over: Record<string, unknown>) =>
      texts(run(datum({ signatures: [signature(plan, id, over)] })));
    const hq = only('space-hq');
    test('SpaceHQ is rejected and SpaceHQ_Portal adopted: restating the plan as it stands is clean', () => {
      expect(rejected('SpaceHQ')).toBe(true); expect(kit.get('SpaceHQ_Portal')).toBe('adopted');
      expect(hq({ kit: 'SpaceHQ' })).toEqual([]);
      expect(hq({})).toEqual([]);
    });
    test('a variant that is not in the catalogue, or is rejected, is a problem; so is a new one', () => {
      const regen = (variant: string) => ({ regen: { variant } });
      expect(hq(regen('SpaceHQ_Nowhere')))
        .toEqual(['Datum: signature space-hq: regen.variant SpaceHQ_Nowhere is not a piece in the kit catalogue']);
      expect(hq({ kit: 'SpaceHQ', ...regen('SpaceHQ') }))
        .toEqual(['Datum: signature space-hq: regen.variant SpaceHQ is rejected in the kit catalogue']);
    });
    test('without a regen the kit itself is judged; with one it is not', () => {
      const liner = only('ocean-liner');
      expect(liner({ kit: 'NoSuchPiece' }))
        .toEqual(['Datum: signature ocean-liner: kit NoSuchPiece is not a piece in the kit catalogue']);
      expect(hq({ kit: 'NoSuchPiece' })).toEqual([]); // the variant SpaceHQ_Portal is the piece that is built
    });
  });

  test('new entries are added; a rail line the transit apply cannot build is a problem and is not applied', () => {
    const a = run(datum({ towerSites: [site(plan, 'crescent-terrace', { id: 'new-spire' })] }));
    expect(a.problems).toEqual([]); expect(ids(a.plan.standards.buildings.towers.sites)).toContain('new-spire');
    const path = { id: 'p', role: 'line' as const, double: true, pts: [[0, 0]] };
    const line = { id: 'S9', mode: 'skyrail' as const, name: 'Nine', beyond: '', paths: [path], order: [] };
    const bad = run(datum({ rail: { lines: [line], remove: [] } }));
    expect(texts(bad).join('\n')).toContain('S9'); expect(ids(bad.plan.skyrail)).not.toContain('S9');
  });
});
