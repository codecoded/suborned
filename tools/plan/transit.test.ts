// Each transit rule has a failing example: the rule id must fail on a mutated copy of the plan and the plan itself must pass it.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { resolve } from 'node:path';
import { check } from './checks';
import { civic } from './civic';
import { centroid } from './geom';
import { derive, type Plan } from './plan';

setDefaultTimeout(30000);
const root = resolve(import.meta.dir, '../..'), read = async (p: string) => JSON.parse(await Bun.file(resolve(root, p)).text());
const base: Plan = await read('contracts/world/harbour-district.plan.json');
const civ = civic(base.civic, await read(base.civic.block));
const run = (mutate: (p: any) => void = () => {}) => {
  const p = structuredClone(base) as any; mutate(p);
  const R = check(p, derive(p), civ, true), by = (id: string) => R.checks.filter(c => c.id === id);
  return { R, by, fails: (id: string) => by(id).some(c => !c.ok), passes: (id: string) => by(id).length > 0 && by(id).every(c => c.ok) };
};
const t2 = (x: number, stopAt: [number, number]) => ({ id: 'T2', name: 'Harbour Line', beyond: 'off-sheet', paths: [{ id: 'main', role: 'running', double: true, pts: [[x, -780], [x, 320]] }], stops: [{ id: 'quay', name: 'Quay', path: 'main', at: stopAt }] });
const skySplit = (skyportAt: number) => (p: any) => {
  const s = p.skyrail[0], loop = s.paths[1];
  s.paths = [{ id: 'a', role: 'line', double: true, pts: [[-400, 0], [100, 0]] }, { id: 'b', role: 'line', double: true, pts: [[100, 0], [560, 0]] }, loop];
  s.order = ['a', 'b', 'loop'];
  s.stations = [{ ...s.stations[0], path: 'a' }, { ...s.stations[1], path: 'a' }, { ...s.stations[2], path: 'b' }, { ...s.stations[3], path: 'b', at: [skyportAt, 0] }];
};

describe('transit rules', () => {
  test('the draft plan passes every old rule and every new one except its two waived ones', () => {
    const r = run(), failed = r.R.checks.filter(c => !c.ok && c.id !== 'W-2').map(c => c.id).sort(); // W-2 has its own tests (entrances.test.ts)
    expect(failed).toEqual(['N-R1', 'S-S2']);
    expect(check(base as Plan, derive(base), civ).checks.every(c => c.ok)).toBe(true);
  });
  test('every new rule names its source', () => {
    for (const c of run().R.checks.filter(c => c.group === 'Transit' || c.id === 'S-S2')) expect(c.source).toBeTruthy();
  });
  test('N-D1 dead end: a spur that stops inside the sheet', () => {
    expect(run().passes('N-D1')).toBe(true);
    const r = run(p => p.skyrail[0].paths.push({ id: 'spur', role: 'line', pts: [[100, 0], [100, 100]] }));
    expect(r.fails('N-D1')).toBe(true); expect(r.by('N-D1')[0]!.detail).toContain('S1 spur');
  });
  test('N-D1 closed loops and off-sheet ends are fine', () => {
    const r = run(p => p.skyrail.push({ id: 'S2', name: 'Sky Loop', beyond: '', paths: [{ id: 'ring', role: 'line', double: true, pts: [[-100, -100], [200, -100, 70], [200, 150, 70], [-100, 150, 70], [-100, -100, 70]] }], stations: [] }));
    expect(r.passes('N-D1')).toBe(true);
  });
  test('N-D1 a 5 m cap path is a dead end, not a closed loop; a stub lying along its own trunk fails', () => {
    const cap = run(p => p.skyrail[0].paths.push({ id: 'cap', role: 'line', pts: [[0, 200], [0, 205]] }));
    expect(cap.fails('N-D1')).toBe(true); expect(cap.by('N-D1')[0]!.detail).toContain('S1 cap');
    const stub = run(p => p.skyrail[0].paths.push({ id: 'stub', role: 'line', pts: [[-300, 0], [-200, 0]] }));
    expect(stub.fails('N-D1')).toBe(true); expect(stub.by('N-D1')[0]!.detail).toContain('S1 stub lies along');
  });
  test('N-R1 reach: worst plot reported against standards.transit.reach', () => {
    const r = run(p => { p.standards.transit.reach = 100; });
    expect(r.fails('N-R1')).toBe(true); expect(r.by('N-R1')[0]!.value).toMatch(/^worst \S+ \d+ m$/);
    expect(run(p => { p.standards.transit.reach = 300; }).passes('N-R1')).toBe(true);
  });
  test('N-C1 connected: a line with no interchange is a second network; a stop pair within 150 m joins it', () => {
    const apart = run(p => p.tram.push(t2(600, [600, -690])));
    expect(apart.fails('N-C1')).toBe(true); expect(apart.by('N-C1')[0]!.value).toContain('2 networks');
    const near = run(p => p.tram.push(t2(300, [300, -100])));
    expect(near.passes('N-C1')).toBe(true);
    expect(near.by('T-L1')).toHaveLength(2); expect(near.passes('T-L1')).toBe(true); // a through-running T2 needs no loop, and nothing crashes
  });
  test('N-C1 trusts no interchange field: a far T2 naming T1/nowhere stays a second network, and S-I1 fails', () => {
    const r = run(p => { const l = t2(600, [600, -690]); (l.stops[0] as any).interchange = 'T1/nowhere'; p.tram.push(l); });
    expect(r.fails('N-C1')).toBe(true); expect(r.fails('S-I1')).toBe(true); expect(r.by('N-C1')[0]!.detail).toContain('interchange not valid: T2/quay');
    const farPair = run(p => { const l = t2(600, [600, -690]); (l.stops[0] as any).interchange = 'T1/circus'; p.tram.push(l); });
    expect(farPair.fails('N-C1')).toBe(true); expect(farPair.fails('S-I1')).toBe(true);
  });
  test('S-I1 covers tram stops, and a partner that names another stop is not reciprocal', () => {
    const ok = run(p => { p.tram[0].stops.find((x: any) => x.id === 'circus').interchange = 'S1/circus'; });
    expect(ok.passes('S-I1')).toBe(true); expect(ok.by('S-I1')).toHaveLength(2);
    const bad = run(p => { p.tram[0].stops.find((x: any) => x.id === 'circus').interchange = 'S1/harbour'; });
    expect(bad.fails('S-I1')).toBe(true);
  });
  test('S-S2 and T-S1 need at least 2 placed stops', () => {
    const sky = run(p => { p.skyrail[0].stations = [p.skyrail[0].stations[0]]; });
    expect(sky.fails('S-S2')).toBe(true); expect(sky.by('S-S2')[0]!.detail).toContain('at least 2');
    const tram = run(p => { p.tram[0].stops = [p.tram[0].stops[0]]; });
    expect(tram.fails('T-S1')).toBe(true);
  });
  test('S-S2 spacing 200-500 m along the ordered paths, across a path boundary', () => {
    expect(run().fails('S-S2')).toBe(true); // 190 m, the draft plan before its waiver
    expect(run(skySplit(540)).passes('S-S2')).toBe(true);
    expect(run(skySplit(530)).fails('S-S2')).toBe(true);
    expect(run(skySplit(540)).passes('N-O1')).toBe(true);
  });
  test('N-O1 order: paths that do not chain, or stops on several paths with no order', () => {
    const r = run(p => { skySplit(540)(p); p.skyrail[0].order = ['b', 'a', 'loop']; });
    expect(r.fails('N-O1')).toBe(true);
    const none = run(p => { skySplit(540)(p); delete p.skyrail[0].order; });
    expect(none.by('N-O1')[0]!.detail).toContain('need an order');
  });
  test('N-P1 / N-P2: a station moved onto a plot is off its beam and on a plot', () => {
    const plot = derive(base).plots.find(x => x.id === 'records-square')!, at = centroid(plot.polyV).map(Math.round);
    const r = run(p => { p.skyrail[0].stations[1].at = at; });
    expect(r.fails('N-P2')).toBe(true); expect(r.fails('N-P1')).toBe(true);
    expect(run(p => { p.tram[0].stops[0].path = 'nope'; }).fails('N-P1')).toBe(true);
  });
  test('S-I1 resolves an interchange to any line, and fails for an unknown stop', () => {
    expect(run(p => { p.skyrail[0].stations[1].interchange = 'T9/x'; }).fails('S-I1')).toBe(true);
  });
});

describe('T-L1', () => {
  test('a tram end inside the sheet needs a terminal loop', () => {
    const r = run(p => p.tram.push({ ...t2(300, [300, -100]), paths: [{ id: 'main', role: 'running', double: true, pts: [[300, -780], [300, -100]] }] }));
    expect(r.fails('T-L1')).toBe(true); expect(r.by('T-L1').find(c => !c.ok)!.detail).toContain('no loop at');
  });
});
