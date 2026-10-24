// The human bot policy (Latch; Sky 2026-10-25): the route a seed picks, the steps and their coupling, the noise tape and its rates, the profile
// reader, and the follower (the reference BotRun.Human mirrors), played on a fake clock.
import { expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT } from '../missions/check/engine';
import { findingsOf, planRows, type BotReportRow } from './bot';
import {
  COUPLE_TICKS, DEFAULT_PROFILE, LOOKAHEAD, WANDER, drawTime, earliest, humanTape, nextStep, noise, pickRoute, readDist, readProfile, stepsOf, stream, winningRoutes,
  type FollowState, type HumanProfile, type HumanStep, type RouteOrder,
} from './human';

const routes = winningRoutes();
const prof = (over: Partial<HumanProfile> = {}) => ({ profile: { ...DEFAULT_PROFILE, ...over }, source: { file: '', fromFile: [], defaulted: [], ignored: [] } });
const quiet: Partial<HumanProfile> = { cancelProb: 0, reorderProb: 0, switchProb: 0, idleProb: 0, abandonProb: 0, misclickProb: 0, ringOpenCloseProb: 0, cameraTurnProb: 0, wrongVerbProb: 0, thirdPersonProb: 0 };

test('routes: only the winning quiet and loud scripts are intents; each family (A, B, G, loud) is there', () => {
  const idx = JSON.parse(readFileSync(join(ROOT, 'contracts/missions/handover/routes/routes.json'), 'utf8')) as { scripts: { id: string; kind: string }[] };
  const kinds = new Map(idx.scripts.map(s => [s.id, s.kind]));
  expect(routes.length).toBeGreaterThan(4);
  for (const r of routes) { expect(['quiet', 'loud'].includes(kinds.get(r.id) ?? '')).toBe(true); expect(r.orders.length).toBeGreaterThan(5); }
  expect(routes.some(r => r.id.startsWith('fail-') || r.id.startsWith('careless-') || r.id.startsWith('loud-left'))).toBe(false);
  expect([...new Set(routes.map(r => r.group))].sort()).toEqual(['A', 'B', 'G', 'loud']);
});

test('the route: the seed picks it, evenly by family; the same seed the same route; a pin wins; an unknown pin is refused', () => {
  const groups = new Map<string, number>();
  for (let seed = 0; seed < 400; seed++) { const r = pickRoute(routes, stream(seed, 'route')); groups.set(r.group, (groups.get(r.group) ?? 0) + 1); }
  for (const g of ['A', 'B', 'G', 'loud']) expect(groups.get(g)!).toBeGreaterThan(60); // 100 each, give or take
  expect(humanTape(7, routes, prof()).route).toBe(humanTape(7, routes, prof()).route);
  expect(humanTape(7, routes, prof(), 'route-b-board').route).toBe('route-b-board');
  expect(() => humanTape(7, routes, prof(), 'fail-vale-dead')).toThrow(/no winning route/);
});

const ord = (tick: number, op: string, d: string, more: Partial<RouteOrder> = {}): RouteOrder => ({ tick, op, do: d, ...more } as RouteOrder);

test('steps: a step within COUPLE_TICKS of the one before keeps the gap; a far one waits for its tick; one agent goes in order; cell and points parse', () => {
  const s = stepsOf([ord(0, 'scout', 'move', { to: 'beside-inez', run: true }), ord(0, 'tech', 'move', { to: [5650, -4750] }), ord(90, 'scout', 'watch', { target: 'waitress' }),
    ord(90 + COUPLE_TICKS, 'tech', 'loop', { target: 'entrance' }), ord(2000, 'fixer', 'turn', { target: 'waitress', approach: 'help' }), { tick: 2001, do: 'panic', op: undefined } as unknown as RouteOrder]);
  expect(s.map(x => x.after)).toEqual([-1, -1, -1, 2, -1, 4]); // the tick-0 pair is free between itself; 90 is past the couple
  expect(s.map(x => x.afterN)).toEqual([0, 0, 0, 1, 0, 1]);
  expect(s.map(x => x.gap)).toEqual([0, 0, 0, COUPLE_TICKS, 0, 1]);
  const grp = stepsOf([ord(1710, 'fixer', 'call', { target: 'waitress' }), ord(1710, 'enforcer', 'move', { to: 'd:Office entrance' }), ord(1710, 'scout', 'move', { to: [1, 1] }), ord(1734, 'enforcer', 'enter')]);
  expect(grp.map(x => [x.after, x.afterN, x.gap])).toEqual([[-1, 0, 0], [-1, 0, 0], [-1, 0, 0], [0, 3, 24]]); // the entry keeps 2.4 s to the last of the three
  expect(() => stepsOf([ord(5, 'scout', 'move'), ord(4, 'tech', 'move')])).toThrow(/tick order/);
  expect(s.map(x => x.opPrev)).toEqual([-1, -1, 0, 1, -1, -1]);
  expect(s[0]).toMatchObject({ place: 'beside-inez', xy: false, run: true }); expect(s[1]).toMatchObject({ xy: true, x: 5650, y: -4750, place: '' });
  expect(s[4]).toMatchObject({ act: 'turn', approach: 'help' }); expect(s[5]!.op).toBe('cell');
});

test('the tape: the same seed gives the same tape; another seed another; every noise has its own stream (one rate changing moves no other)', () => {
  const a = humanTape(11, routes, prof()), b = humanTape(11, routes, prof()), c = humanTape(12, routes, prof());
  expect(a).toEqual(b); expect(JSON.stringify(a.steps)).not.toBe(JSON.stringify(c.steps));
  const base = stepsOf(routes.find(r => r.id === 'route-a-board')!.orders);
  const n1 = noise(base, { ...DEFAULT_PROFILE, misclickProb: 0 }, 5, -1), n2 = noise(base, { ...DEFAULT_PROFILE, misclickProb: 1 }, 5, -1);
  expect(n1.map(s => s.gapS)).toEqual(n2.map(s => s.gapS)); expect(n1.map(s => s.cancel)).toEqual(n2.map(s => s.cancel)); expect(n1.map(s => s.wrong)).toEqual(n2.map(s => s.wrong));
  expect(n1.some(s => s.misclick)).toBe(false); expect(n2.every(s => s.misclick === (s.act === 'move' || s.act !== 'draw' && s.act !== 'stance' && s.act !== 'panic'))).toBe(true);
  const none = noise(base, { ...DEFAULT_PROFILE, ...quiet }, 5, 0);
  expect(none.every(s => !s.cancel && !s.reorder && s.switchTo < 0 && s.idleS === 0 && s.abandon === -1 && !s.misclick && !s.ringClose && s.turn === 0 && s.wrong === 0 && s.peekS === 0)).toBe(true);
});

test('the noise rates follow the profile over many steps; a misclick lands near but off the thing; a cell step is never cancelled', () => {
  const orders: RouteOrder[] = Array.from({ length: 4000 }, (_, i) => ord(i * 100, ['scout', 'fixer', 'tech', 'enforcer'][i % 4]!, i % 2 ? 'watch' : 'move', { target: 'waitress', to: [0, 0] }));
  const p: HumanProfile = { ...DEFAULT_PROFILE, cancelProb: 0.1, reorderProb: 0.2, switchProb: 0.15, idleProb: 0.05, misclickProb: 0.08, ringOpenCloseProb: 0.1, cameraTurnProb: 0.07, wrongVerbProb: 0.05 };
  const t = noise(stepsOf(orders), p, 99, -1), n = t.length, watch = t.filter(s => s.act === 'watch');
  const rate = (f: (s: HumanStep) => boolean, of = t) => of.filter(f).length / of.length;
  const near = (got: number, want: number) => expect(Math.abs(got - want)).toBeLessThan(Math.max(0.015, want * 0.25));
  near(rate(s => s.cancel), 0.1); near(rate(s => s.reorder), 0.2); near(rate(s => s.switchTo >= 0), 0.15); near(rate(s => s.idleS > 0), 0.05);
  near(rate(s => s.misclick), 0.08); near(rate(s => s.ringClose, watch), 0.1); near(rate(s => s.turn !== 0), 0.07); near(rate(s => s.wrong !== 0, watch), 0.05);
  expect(t.filter(s => s.act === 'move').every(s => !s.ringClose && s.wrong === 0)).toBe(true); // a ground click has no ring
  for (const s of t.filter(x => x.misclick)) { const r = Math.hypot(s.missDx, s.missDy); expect(r).toBeGreaterThanOrEqual(35.9); expect(r).toBeLessThanOrEqual(80.1); } // past the 28 px pick radius
  expect(t.filter(s => s.switchTo >= 0).every(s => ['scout', 'fixer', 'tech', 'enforcer'][s.switchTo] !== s.op)).toBe(true); // a switch looks at another agent
  expect(n).toBe(4000);
  const cell = noise(stepsOf([ord(0, 'scout', 'move'), { tick: 1, do: 'go' } as RouteOrder]), { ...DEFAULT_PROFILE, cancelProb: 1 }, 1, -1);
  expect(cell[1]!.cancel).toBe(false); expect(cell[0]!.cancel).toBe(true);
});

test('abandon: a route switch goes to another family at most once a route, the last alternative never switches again; a wander has its seconds', () => {
  const p = prof({ abandonProb: 0.5 });
  for (let seed = 1; seed < 30; seed++) {
    const t = humanTape(seed, routes, p), groups = [t.group, ...t.alts.map(a => a.group)];
    expect(new Set(groups).size).toBe(groups.length);
    expect(t.steps.filter(s => s.abandon >= 0).length).toBeLessThanOrEqual(1); expect(t.steps.filter(s => s.abandon >= 0).every(s => s.abandon === 0)).toBe(true);
    expect(t.alts[0]!.steps.filter(s => s.abandon >= 0).every(s => s.abandon === 1)).toBe(true);
    expect(t.alts.at(-1)!.steps.some(s => s.abandon >= 0)).toBe(false);
    expect(t.steps[0]!.abandon).toBe(-1); // never before the first step
    for (const s of t.steps.filter(x => x.abandon === WANDER)) { expect(s.wanderS).toBeGreaterThanOrEqual(15); expect(s.wanderS).toBeLessThanOrEqual(45); }
  }
});

test('times: a lognormal with the profile\'s median and 90th percentile', () => {
  const r = stream(3, 't'), xs = Array.from({ length: 6000 }, () => drawTime(r, { p50: 2, p90: 6 })).sort((a, b) => a - b);
  expect(Math.abs(xs[3000]! - 2)).toBeLessThan(0.2); expect(Math.abs(xs[5400]! - 6)).toBeLessThan(0.7);
  expect(drawTime(stream(1, 'x'), { p50: 3, p90: 3 })).toBe(3); // no spread: the median
});

test('the profile: Suture\'s fields over the defaults, numbers or spreads, top level or under values; a bad field keeps its default and says so', () => {
  expect(readProfile(undefined).profile).toEqual(DEFAULT_PROFILE); expect(readProfile(undefined).source.file).toBe('');
  const r = readProfile({ schema: 'x', sessions: 176, values: { cancelProb: 0.2, misclickProb: 2, gapS: { median: 1.1, p90: 3 }, idleS: 4, cancelDelayS: { p50: 0.8 }, wrongVerbProb: 'x', extraThing: 1 } });
  expect(r.profile.cancelProb).toBe(0.2); expect(r.profile.misclickProb).toBe(DEFAULT_PROFILE.misclickProb); expect(r.profile.wrongVerbProb).toBe(DEFAULT_PROFILE.wrongVerbProb);
  expect(r.profile.gapS).toEqual({ p50: 1.1, p90: 3 }); expect(r.profile.idleS).toEqual({ p50: 4, p90: 10 }); expect(r.profile.cancelDelayS).toEqual({ p50: 0.8, p90: 2 });
  expect(r.source.fromFile).toEqual(expect.arrayContaining(['cancelProb', 'gapS', 'idleS', 'cancelDelayS'])); expect(r.source.defaulted).toEqual(expect.arrayContaining(['misclickProb', 'wrongVerbProb']));
  expect(r.source.ignored).toEqual(['extraThing']);
  expect(readProfile({ reorderProb: 0.3 }).profile.reorderProb).toBe(0.3); // top level
  expect(readDist({ mean: 2 })).toEqual({ p50: 2, p90: 5 }); expect(readDist(-1)).toBeUndefined(); expect(readDist({ p50: 2, p90: 1 })).toEqual({ p50: 2, p90: 5 });
});

// ---------------------------------------------------------------- following, on a fake clock

/** Plays a tape's steps as the player's loop would: each tick the follower picks one step (if any) and gives it at once; `late` holds a step back
 *  that many ticks once it is first picked (a person fumbling it); `busy` keeps an agent unfree over a span of ticks. */
function play(steps: HumanStep[], o: { late?: Record<number, number>; busy?: { op: string; from: number; to: number }[]; until?: number } = {}) {
  const st: FollowState = { tick: 0, issued: steps.map(() => null), resolved: steps.map(() => false), free: op => !(o.busy ?? []).some(b => b.op === op && st.tick >= b.from && st.tick < b.to) };
  const order: number[] = [], reorders: [number, number][] = [], heldUntil = new Map<number, number>();
  for (st.tick = 0; st.tick < (o.until ?? 20000) && st.resolved.some(r => !r); st.tick++) {
    const n = nextStep(steps, st); if (!n) continue;
    const late = o.late?.[n.i] ?? 0; if (late > 0 && !heldUntil.has(n.i)) { heldUntil.set(n.i, st.tick + late); }
    if ((heldUntil.get(n.i) ?? 0) > st.tick) continue;
    if (n.over !== undefined) reorders.push([n.over, n.i]);
    st.issued[n.i] = st.tick; st.resolved[n.i] = true; order.push(n.i);
  }
  return { st, order, reorders };
}
const clean = (orders: RouteOrder[], over: Partial<HumanStep>[] = []) => noise(stepsOf(orders), { ...DEFAULT_PROFILE, ...quiet }, 1, -1).map((s, i) => ({ ...s, ...over[i] }));

test('following a winning route clean: every step given once, none before its tick, one agent in order, coupled gaps kept', () => {
  const r = routes.find(x => x.id === 'route-a-board')!, steps = clean(r.orders), { st, order } = play(steps);
  expect(order.length).toBe(steps.length); expect(new Set(order).size).toBe(steps.length);
  steps.forEach((s, i) => { expect(st.issued[i]!).toBeGreaterThanOrEqual(s.tick); if (s.after >= 0) expect(st.issued[i]! - st.issued[s.after]!).toBeGreaterThanOrEqual(s.gap); });
  for (const op of ['scout', 'fixer', 'tech', 'enforcer']) { const mine = order.filter(i => steps[i]!.op === op); expect(mine).toEqual([...mine].sort((a, b) => a - b)); }
});

test('a late step drags its close followers by the same lateness; a step far off still goes at its own tick', () => {
  const steps = clean([ord(100, 'fixer', 'call', { target: 'waitress' }), ord(124, 'enforcer', 'enter'), ord(1000, 'scout', 'move', { to: [0, 0] })]);
  const { st } = play(steps, { late: { 0: 200 } });
  expect(st.issued[0]).toBe(300); expect(st.issued[1]).toBe(324); // the entry still 2.4 s after the call that pulls the guard
  expect(st.issued[2]).toBe(1000);
  expect(earliest(steps, 1, { ...st, issued: [null, null, null], resolved: [false, false, false] })).toBeNull(); // not before the call is given
});

test('a busy agent holds only its own steps (the others go on, up to LOOKAHEAD past the first one waiting); re-order noise lets another agent go first', () => {
  const orders = [ord(0, 'scout', 'watch', { target: 'waitress' }), ...Array.from({ length: LOOKAHEAD + 2 }, (_, i) => ord(100 + i * 100, i % 2 ? 'tech' : 'fixer', 'move', { to: [i, 0] }))];
  const { st, order } = play(clean(orders), { busy: [{ op: 'scout', from: 0, to: 5000 }] });
  expect(order[0]).toBe(1); expect(st.issued[0]).toBe(5000); // the Scout's watch waits for the Scout
  expect(order.slice(0, LOOKAHEAD)).toEqual([1, 2, 3, 4]); expect(st.issued[LOOKAHEAD + 1]).toBe(5001); // nothing past the window until the first one goes (the tick after it: one step a tick)
  const two = clean([ord(0, 'scout', 'move', { to: [0, 0] }), ord(0, 'tech', 'move', { to: [1, 0] })], [{ reorder: true }]);
  const r = play(two); expect(r.order).toEqual([1, 0]); expect(r.reorders).toEqual([[0, 1]]);
  const same = clean([ord(0, 'scout', 'move', { to: [0, 0] }), ord(0, 'scout', 'watch', { target: 'waitress' })], [{ reorder: true }]);
  expect(play(same).order).toEqual([0, 1]); // never past the same agent's earlier step
});

test('a step given up keeps no gap: its followers wait only for their own tick', () => {
  const steps = clean([ord(100, 'fixer', 'call', { target: 'waitress' }), ord(120, 'enforcer', 'enter')]);
  const st: FollowState = { tick: 120, issued: [null, null], resolved: [true, false], free: () => true };
  expect(earliest(steps, 1, st)).toBe(120); expect(nextStep(steps, st)).toEqual({ i: 1 });
});

// ---------------------------------------------------------------- the bot lane's rows

test('bot.json: the human rows plan with a tape each, other seeds; a route pin only on a human row; the tape names the route on findings', () => {
  const raw = JSON.parse(readFileSync(join(ROOT, 'tools/qa/bot.json'), 'utf8'));
  const rows = planRows(raw, '20261025', { routes, profile: prof() });
  const hum = rows.filter(r => r.policy === 'human'); expect(hum.map(r => r.name)).toEqual(['human-a', 'human-b']);
  for (const r of hum) { expect(r.human!.steps.length).toBeGreaterThan(5); expect(r.seconds).toBe(660); expect(r.human!.seed).toBe(r.seed); }
  expect(rows.filter(r => r.policy !== 'human').every(r => r.human === undefined)).toBe(true);
  expect(planRows({ schema: 'suborned.bot/1', rows: [{ name: 'h', policy: 'human', seed: 1, route: 'route-g-board' }] }, 'n', { routes, profile: prof() })[0]!.human!.route).toBe('route-g-board');
  expect(() => planRows({ schema: 'suborned.bot/1', rows: [{ name: 'g', policy: 'greedy', route: 'route-a' }] }, 'n')).toThrow(/human policy/);
  const row = { name: 'human-a', policy: 'human', seed: 4, status: 'ok', preset: 'hold', seconds: 1, endTick: 1, outcome: 'failure', failCode: 'sealed', cardHead: '',
    input: { decisions: 0, clicks: 0, rings: 0, ringMisses: 0, taken: 0, moves: 0, runs: 0, pans: 0, turns: 0 }, orders: { given: 0, refused: 0, codes: {} }, frames: { count: 0, meanMs: 0, p99Ms: 0, maxMs: 0, over50: 0 },
    memory: { startMB: 0, endMB: 0, slopeMBPerMin: 0 }, consoleErrors: 0, feetSamples: 0, figures: 0, shots: 0, counts: {},
    anomalies: [{ check: 'Q-OFFER', key: 'listen:x', title: 't', expected: 'e', observed: 'o', tick: 10, clock: '0:01', at: [0, 0], shot: '', script: '', noise: 'misclick: step 7 scout listen waitress, 2.1 s before' }],
    human: { route: 'route-a', group: 'A', routes: ['route-a', 'route-b'], profile: 'defaults', steps: 40, done: 12, skipped: 1, counts: { misclick: 1 }, events: [], trace: 'route-a: dd' } } satisfies BotReportRow;
  const f = findingsOf(row, 'human-a', '/b')[0]!;
  expect(f.observed).toContain('human seed 4 on route-a > route-b'); expect(f.observed).toContain('human: misclick: step 7');
});

test('the player reads every field the tape writes (BotRun.HumanStep and HumanTape name the same fields as human.ts)', () => {
  const cs = readFileSync(join(ROOT, 'unity/Assets/Suborned/Runtime/Play/BotRun.Human.cs'), 'utf8');
  const fields = (cls: string) => {
    const body = cs.match(new RegExp(`class ${cls}\\s*\\{([\\s\\S]*?)\\n        \\}`))?.[1] ?? cs.match(new RegExp(`class ${cls} \\{([^}]*)\\}`))?.[1] ?? '';
    return new Set(body.split(';').map(d => d.trim()).filter(d => d.startsWith('public')).flatMap(d => d.replace(/^public [\w\[\]]+ /, '').split(',').map(p => p.split('=')[0]!.trim())));
  };
  const t = humanTape(4, routes, prof());
  expect([...fields('HumanStep')].sort()).toEqual(Object.keys(t.steps[0]!).sort());
  const tape = fields('HumanTape'); for (const k of Object.keys(t)) if (k !== 'profile') expect(tape.has(k)).toBe(true); // the profile rides along for the report's reader, not the player
});
