// Brief 060 (Suture): the QA checks, the mutation generator and the tables on hand-made plays (no engine: the pass itself plays the engine).
import { describe, expect, test } from 'bun:test';
import { checkPlay, checkTwins } from './checks';
import { qDebrief, qFail } from './checks/outcome';
import { qDrop, qLearn, qOffer } from './checks/orders';
import { qSpawn, qStuck } from './checks/stuck';
import { qWarn } from './checks/warn';
import { IssueBook } from './issues';
import { applyOne, familyOf, mutants, rng, vocabulary } from './mutate';
import { html, summary } from './report';
import { agg, rowOf, tables, takedownSeen } from './tables';
import { textTables } from './text';
import { DEFAULT_RULES, FAMILIES, type Ctx, type Item, type Play, type Script, type Seed } from './types';

const text = textTables({
  briefing: { fails: { sealed: 'x', 'vale-leaves': 'x', 'enforcer-down': 'x' } },
  less: { end: { fail: { sealed: 'x', 'vale-leaves': 'x', 'enforcer-down': 'x' } }, failcam: { caption: { sealed: { response: 'x' }, 'vale-leaves': { 'too-late': 'x' }, 'enforcer-down': { shot: 'x' } } } },
  cards: { items: { 'seal.starts': { fate: 'beat' }, 'rivals.arrive': { fate: 'beat' }, 'vale.exits': { fate: 'beat' } } },
});
const ctx: Ctx = { text, rules: DEFAULT_RULES };

function play(over: Partial<Play> = {}): Play {
  return {
    id: 't.json', outcome: 'success', fail: '', tick: 3000, cash: 90000, trail: 2, alert: 1, gunfire: 0, standing: 2, startHash: 'a', finalHash: 'b', replayIdentical: true,
    checkpoints: [], orders: [], ledger: [], log: [{ tick: 2990, code: 'board.in', text: 'The enforcer is in the car with Vale.' }, { tick: 3000, code: 'success', text: '' }],
    qa: { record: 'none', debrief: { route: ['vale-reaches-getaway-car'], loud: false }, stuck: [], vale: [{ tick: 0, state: 'Office' }], seen: [], lockdown: [] },
    ...over,
  };
}
const script = (orders: Script['orders'], until = 0): Script => ({ schema: 'suborned.sim.script/1', mission: 'handover', start: {}, orders, until });
const seed = (id: string, kind: Seed['kind'], s: Script, routeKind = 'quiet'): Seed => ({ id, kind, file: id, script: s, routeKind });
const item = (s: Seed, mutated = false): Item => ({ id: s.id + (mutated ? '~m0' : ''), seed: s, mutations: mutated ? [{ family: 'drop', index: 0, what: 'x' }] : [], script: s.script });
const route = seed('route-x', 'route', script([{ tick: 0, op: 'scout', do: 'move', to: 'a' }]));

describe('Q-DEBRIEF', () => {
  test('a failed run that claims Vale reached the car is S1 (Sky #8)', () => {
    const p = play({ outcome: 'failure', fail: 'enforcer-down', log: [{ tick: 2400, code: 'fail.enforcer-down', text: '' }] });
    const fs = qDebrief(p);
    expect(fs).toHaveLength(1);
    expect(fs[0]!.severity).toBe('S1');
    expect(fs[0]!.key).toBe('Q-DEBRIEF:vale-reaches-getaway-car:not-won');
  });
  test('a won run whose log backs the claim is clean', () => expect(qDebrief(play())).toEqual([]));
  test('a script that stops mid-run has no end card to judge', () => expect(qDebrief(play({ outcome: 'none', log: [] }))).toEqual([]));
  test('an unknown route id is info, not an issue', () => {
    const fs = qDebrief(play({ qa: { ...play().qa, debrief: { route: ['something-new'] } } }));
    expect(fs.map(f => f.severity)).toEqual(['info']);
  });
  test('the loud flag must match the gunfire', () => expect(qDebrief(play({ gunfire: 3 })).map(f => f.key)).toEqual(['Q-DEBRIEF:loud']));
  test('a route id retired by engine 0.33 (pickup-camera-blocked, with car.blocks) is S2 even when an old log has car.blocks', () => {
    const fs = qDebrief(play({ qa: { ...play().qa, debrief: { route: ['pickup-camera-blocked'] } }, log: [...play().log, { tick: 3000, code: 'car.blocks', text: '' }] }));
    expect(fs.map(f => [f.key, f.severity])).toEqual([['Q-DEBRIEF:retired:pickup-camera-blocked', 'S2']]);
  });
});

describe('Q-FAIL', () => {
  const failed = (cause: any) => play({ outcome: 'failure', fail: 'sealed', cause, log: [{ tick: 3000, code: 'fail.sealed', text: '' }], qa: { ...play().qa, debrief: null } });
  test('a seal with an origin and a caption is clean', () => expect(qFail(failed({ fail: 'sealed', why: 'sealed', tick: 3000, clock: '', single: false, culprits: [], ops: [], origin: { tick: 1, report: 'x', because: 'response' } }), ctx)).toEqual([]));
  test('a missing caption and a missing cause are found', () => {
    expect(qFail(failed({ fail: 'sealed', why: 'sealed', tick: 3000, clock: '', single: false, culprits: [], ops: [], origin: { tick: 1, report: 'x', because: 'confirmed' } }), ctx).map(f => f.key)).toEqual(['Q-FAIL:no-caption:sealed:confirmed']);
    expect(qFail(failed(undefined), ctx).map(f => f.key)).toEqual(['Q-FAIL:no-cause']);
  });
});

describe('Q-WARN', () => {
  const sealed = (log: Play['log']) => play({ outcome: 'failure', fail: 'sealed', tick: 3000, log: [...log, { tick: 3000, code: 'fail.sealed', text: '' }] });
  test('a seal with no seal line is found (Sky #9)', () => expect(qWarn(sealed([]), ctx).map(f => f.key)).toEqual(['Q-WARN:none:sealed']));
  test('a seal warned 10 s ahead is late; 60 s ahead is fine', () => {
    expect(qWarn(sealed([{ tick: 2900, code: 'seal.starts', text: '' }]), ctx).map(f => f.key)).toEqual(['Q-WARN:late:sealed']);
    expect(qWarn(sealed([{ tick: 2400, code: 'seal.starts', text: '' }]), ctx)).toEqual([]);
  });
  test('the last seal line counts (a clock pulled earlier)', () => expect(qWarn(sealed([{ tick: 1000, code: 'seal.starts', text: '' }, { tick: 2950, code: 'seal.starts', text: '' }]), ctx).map(f => f.key)).toEqual(['Q-WARN:late:sealed']));
  test('fails warned by their own act are not judged here', () => expect(qWarn(play({ outcome: 'failure', fail: 'entry-seen-camera' }), ctx)).toEqual([]));
});

describe('Q-STUCK and Q-SPAWN', () => {
  test('Vale 140 s at his car is S1 (Sky #10); 5 s is fine', () => {
    const at = (to: number) => play({ outcome: 'failure', fail: 'vale-leaves', tick: 5100, qa: { ...play().qa, vale: [{ tick: 0, state: 'Office' }, { tick: 3500, state: 'Leaving' }, { tick: 3700, state: 'Pickup' }, ...(to < 5100 ? [{ tick: to, state: 'Taking' }] : [])] } });
    expect(qStuck(at(5100), ctx).map(f => f.key)).toEqual(['Q-STUCK:vale-at-car']);
    expect(qStuck(at(3750), ctx)).toEqual([]);
  });
  test('an agent still on an open order past 10 s', () => {
    const p = play({ qa: { ...play().qa, stuck: [{ op: 'tech', kind: 'walk', order: 'move', from: 100, to: 300, x: 1, y: 2 }, { op: 'scout', kind: 'act', order: 'watch:guard', from: 100, to: 160, x: 1, y: 2 }] } });
    expect(qStuck(p, ctx).map(f => f.key)).toEqual(['Q-STUCK:agent:walk:move']);
  });
  test('a rival on the street before the rivals arrive (Sky #11)', () => {
    const p = play({ log: [{ tick: 1900, code: 'rivals.arrive', text: '' }], qa: { ...play().qa, seen: [{ id: 'rival-1', tick: 0, x: 0, y: 0 }, { id: 'rival-2', tick: 2000, x: 0, y: 0 }, { id: 'guard', tick: 0, x: 0, y: 0 }] } });
    expect(qSpawn(p).map(f => f.key)).toEqual(['Q-SPAWN:rival']);
  });
});

describe('order checks', () => {
  const refused = play({ orders: [{ order: { tick: 50, op: 'fixer', do: 'turn', target: 'waitress' }, ok: false, code: 'too-far', why: 'x' }, { order: { tick: 60, do: 'go' }, ok: false, code: 'mission-over' }] });
  test('a seed\'s refused order is Q-OFFER; a mutant\'s is not; mission-over is not', () => {
    expect(qOffer(refused, item(route)).map(f => [f.key, f.severity])).toEqual([['Q-OFFER:route:turn:too-far', 'S2']]);
    expect(qOffer(refused, item(route, true))).toEqual([]);
  });
  test('a careless route\'s refusal is info; a session replay that differs from Sky\'s is info', () => {
    expect(qOffer(refused, item(seed('careless-x', 'route', route.script, 'careless')))[0]!.severity).toBe('info');
    expect(qOffer(refused, item(seed('s-1', 'session', route.script, 'session')))[0]!.key).toBe('Q-OFFER:session:turn:too-far:replay-differs');
    expect(qOffer(refused, item(seed('s-1', 'session', route.script, 'session')))[0]!.severity).toBe('info');
    expect(qOffer({ ...refused, qa: { ...refused.qa, record: '' } }, item(seed('s-1', 'session', route.script, 'session')))[0]!.severity).toBe('S2');
    // only a session Sky played himself says "Sky"; automation saved beside his (a proof, a late row) says what it is (Aster 2026-10-25)
    const faithful = { ...refused, qa: { ...refused.qa, record: '' } };
    expect(qOffer(faithful, item({ ...seed('s-2', 'session', route.script, 'session'), by: 'sky' }))[0]!.title).toContain('An order Sky gave');
    const auto = qOffer(refused, item({ ...seed('s-3', 'session', route.script, 'session'), by: 'automation' }))[0]!;
    expect(auto.title).toContain('a saved proof session'); expect(auto.observed).not.toContain('Sky'); expect(auto.key).toBe('Q-OFFER:automation:turn:too-far:replay-differs');
  });
  test('a dropped order and a repeat Watch that lands twice', () => {
    expect(qDrop(play({ log: [{ tick: 9, code: 'order.dropped', text: 'fixer watch waitress: too-far: out of reach' }] }), item(route)).map(f => f.key)).toEqual(['Q-DROP:too-far']);
    const w = { code: 'scout.watch.watched', text: 'Inez (waitress): cell.watch → watched.' };
    expect(qLearn(play({ log: [{ tick: 100, ...w }, { tick: 400, ...w }] })).map(f => f.key)).toEqual(['Q-LEARN:watch']);
  });
  test('twins with different hashes; a twin is only checked for Q-DET', () => {
    expect(checkTwins(play(), play({ finalHash: 'c' })).map(f => f.key)).toEqual(['Q-DET:twin']);
    expect(checkTwins(play(), play())).toEqual([]);
    const lie = play({ outcome: 'failure', fail: 'enforcer-down', log: [] });
    expect(checkPlay(lie, { ...item(route), twin: true }, ctx)).toEqual([]);
    expect(checkPlay(lie, item(route), ctx).some(f => f.check === 'Q-DEBRIEF')).toBe(true);
  });
  test('an engine exception is Q-ERR S1 and stops the other checks', () => {
    const fs = checkPlay({ ...play(), error: 'FormatException: x' }, item(route), ctx);
    expect(fs.map(f => [f.check, f.severity])).toEqual([['Q-ERR', 'S1']]);
  });
});

describe('mutations', () => {
  const s = seed('r', 'route', script([
    { tick: 0, op: 'scout', do: 'move', to: 'a' }, { tick: 1, op: 'scout', do: 'drive', to: [1, 2], face: 0 },
    { tick: 90, op: 'scout', do: 'watch', target: 'waitress' }, { tick: 300, op: 'fixer', do: 'turn', target: 'waitress', approach: 'help' }, { tick: 500, op: 'tech', do: 'loop', target: 'entrance' },
  ], 4000));
  const v = vocabulary([s, seed('q', 'route', script([{ tick: 10, op: 'fixer', do: 'turn', target: 'driver', approach: 'help' }]))]);
  test('the same seed gives the same mutants', () => expect(JSON.stringify(mutants(s, 40, v))).toBe(JSON.stringify(mutants(s, 40, v))));
  test('every family changes the orders and never touches a drive sample', () => {
    for (const f of FAMILIES) {
      for (let k = 0; k < 20; k++) {
        const orders = s.script.orders.map(o => ({ ...o })), m = applyOne(f, orders, rng(k + 1), v, 4000);
        expect(m?.family).toBe(f);
        expect(JSON.stringify(orders)).not.toBe(JSON.stringify(s.script.orders));
        expect(orders.filter(o => o.do === 'drive')).toEqual([s.script.orders[1]!]);
      }
    }
  });
  test('mutants drop the record, keep the seed, and carry one or two mutations', () => {
    const ms = mutants({ ...s, script: { ...s.script, expect: { outcome: 'success' } } }, 50, v);
    expect(ms.every(m => m.script.expect === undefined && m.seed.id === 'r' && m.mutations.length >= 1 && m.mutations.length <= 2)).toBe(true);
    expect(new Set(ms.map(familyOf)).size).toBeGreaterThan(3);
    expect(v.orders.some(o => o.do === 'drive')).toBe(false);
  });
});

describe('tables, issues and the report', () => {
  const td = (witness: boolean): Play => play({ log: [{ tick: 100, code: 'takedown.starts', text: '' }, ...(witness ? [{ tick: 120, code: 'witness.camera', text: '' }] : []), { tick: 160, code: 'takedown.out', text: '' }, { tick: 170, code: 'witness.camera', text: '' }] });
  test('a takedown is seen only when a witness line lands during it', () => {
    expect(takedownSeen(td(true))).toBe('seen');
    expect(takedownSeen(td(false))).toBe('unseen');
    expect(takedownSeen(play())).toBe('none');
  });
  test('rows, aggregates and the T9 proxy', () => {
    const rows = [rowOf('tree', td(false), item(route)), rowOf('tree', { ...td(true), outcome: 'failure', fail: 'entry-seen-camera' }, item(route, true)), rowOf('tree', td(true), item(route, true))];
    const a = agg(rows); expect([a.plays, a.wins, a.fails['entry-seen-camera']]).toEqual([3, 2, 1]);
    const t = tables(rows);
    expect(t.t9[0]!.seen.plays).toBe(2);
    expect(t.t9[0]!.seen.failOrLoud).toBe(0.5);
    expect(t.t9[0]!.pass).toBe(false);
    expect(t.byFamily.find(f => f.family === 'drop')!.agg.plays).toBe(2);
  });
  test('issues rank by severity × share and keep unmutated exemplars first', () => {
    const b = new IssueBook(2), f = (sev: 'S1' | 'S3', key: string) => ({ check: 'Q-X', severity: sev, key, title: key, expected: '', observed: '', owner: 'Keel' });
    b.add('tree', item(route, true), [f('S1', 'rare')]);
    for (let k = 0; k < 50; k++) b.add('tree', { ...item(route, true), id: 'm' + k }, [f('S3', 'common')]);
    b.add('tree', item(route), [f('S3', 'common')]);
    const r = b.ranked({ tree: 100 });
    expect(r.map(i => i.key)).toEqual(['common', 'rare']); // 2 × 0.51 > 8 × 0.01
    expect(r[0]!.exemplars[0]!.mutations).toEqual([]);
    expect(r[0]!.plays.tree).toBe(51);
    const lane = { name: 'tree', dll: '', cwd: '', textFile: '', engine: 'e', label: 'l', inputs: 'i' };
    const pass = { run: 'r', at: 'now', seconds: 1, lanes: [{ lane, faithful: 0, seeds: 1, mutants: 1, plays: 2, errors: 0, replayChecked: 2, seconds: 1, host: '' }], issues: r, tables: tables([]), checks: {}, fixed: [], root: '' };
    expect(html(pass)).toContain('For Sky: 2 issues');
    expect(summary(pass).issues.map(i => i.key)).toEqual(['common', 'rare']);
  });
});
