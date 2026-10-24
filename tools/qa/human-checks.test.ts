// Brief 060 tier 2 (Suture): the checks the human bot feeds (tools/qa/checks/human.ts) on made-up plays, and the bot-lane hook (bot-human.ts).
import { expect, test } from 'bun:test';
import { mkdirSync, mkdtempSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { humanChecks, qCancel, qIntent, qLeak, qLoopLeft, qPile, qStaleRing } from './checks/human';
import { sessionChecks, stallFindings } from './bot-human';
import type { BotReportRow } from './bot';
import type { Play } from './types';

type Ord = Play['orders'][number];
const ord = (tick: number, op: string, d: string, more: Record<string, unknown> = {}, ok = true, code?: string): Ord => ({ order: { tick, op: op as any, do: d, ...more }, ok, ...(code ? { code } : {}) });
const play = (orders: Ord[], log: [number, string, string][], ledger: Play['ledger'] = []): Play => ({
  id: 'x', outcome: 'none', fail: '', tick: 5000, cash: 0, trail: 0, alert: 0, gunfire: 0, standing: 0, startHash: '', finalHash: '', replayIdentical: true, checkpoints: [],
  orders, ledger, log: log.map(([tick, code, text]) => ({ tick, code, text })), qa: { record: '', debrief: null, stuck: [], vale: [], seen: [], lockdown: [] },
});
const session = { seed: 'session' as const, mutated: false };

test('Q-CANCEL: the host cancelling an act for a new order, a walk-in ended, a third-person drive; the player\'s own cancel is not one', () => {
  const p = play([ord(100, 'scout', 'watch', { target: 'waitress' }), ord(150, 'scout', 'cancel'), ord(150, 'scout', 'move', { to: [1, 1] }), ord(400, 'fixer', 'cancel'), ord(600, 'tech', 'drive', { to: [0, 0] })],
    [[150, 'cancel.watch', 'scout stops: watch waitress (lost: the time is spent; nothing is learned).'], [300, 'order.walk-in-ends', 'fixer: the walk to waitress ends (move).'],
     [400, 'cancel.turn', 'fixer stops: turn waitress (aborted: ...).'], [600, 'cancel.loop', 'tech stops: loop entrance (lost: the time is spent; nothing lands).']]);
  const f = qCancel(p, session);
  expect(f.map(x => x.key)).toEqual(['Q-CANCEL:replace:watch', 'Q-CANCEL:walk-in:unmeant', 'Q-CANCEL:drive:loop']);
  expect(f.map(x => x.severity)).toEqual(['S3', 'S3', 'S2']);
  expect(qCancel(p, { ...session, mutated: true })).toEqual([]); // a mutant makes these on purpose
  // a human row: the cancel it meant is info, the one it did not mean is S2
  const meant = qCancel(p, { seed: 'bot', mutated: false, events: [{ tick: 148, kind: 'cancel', op: 'scout', act: 'watch', target: 'waitress' }] });
  expect(meant.find(x => x.key.startsWith('Q-CANCEL:replace'))!.severity).toBe('info'); expect(meant.find(x => x.key.startsWith('Q-CANCEL:walk-in'))!.severity).toBe('S2');
});

test('Q-LOOP-LEFT: a move to the looping Tech ends the loop; a cancel he was given does not count; a route means it', () => {
  const p = play([ord(1000, 'tech', 'move', { to: [5, 5] })], [[1012, 'loop.ends', 'The Tech leaves the cabinet. The camera feed returns.']]);
  expect(qLoopLeft(p, session).map(x => [x.key, x.severity])).toEqual([['Q-LOOP-LEFT:move', 'S3']]);
  expect(qLoopLeft(p, { seed: 'route', mutated: false })[0]!.severity).toBe('info');
  expect(qLoopLeft(play([ord(1000, 'tech', 'move', { to: [5, 5] }), ord(1005, 'tech', 'cancel')], [[1005, 'loop.ends', 'x']]), session)).toEqual([]);
});

test('Q-INTENT: a move the human bot meant arrives as a carry (next-batch 31)', () => {
  const p = play([ord(2000, 'tech', 'carry', { target: 'scout' }), ord(2500, 'fixer', 'watch', { target: 'guard' })], []);
  const ev = [{ tick: 1999, kind: 'given', op: 'tech', act: 'move', target: '' }, { tick: 2499, kind: 'given', op: 'fixer', act: 'watch', target: 'guard' }];
  expect(qIntent(p, { seed: 'bot', mutated: false, events: ev }).map(x => x.key)).toEqual(['Q-INTENT:move->carry']);
  expect(qIntent(p, session)).toEqual([]);
});

test('Q-PILE: three busy refusals to one agent in 10 s are one finding', () => {
  const p = play([ord(100, 'scout', 'move', {}, false, 'agent-busy'), ord(130, 'scout', 'move', {}, false, 'agent-busy'), ord(170, 'scout', 'move', {}, false, 'agent-busy'), ord(180, 'scout', 'move', {}, false, 'agent-busy'), ord(900, 'scout', 'move', {}, false, 'agent-busy')], []);
  expect(qPile(p, session).map(x => x.key)).toEqual(['Q-PILE:refused']);
});

test('Q-STALE-RING: an act on someone gone 2 s before is S2; in the same tick, a race', () => {
  const p = play([ord(2430, 'enforcer', 'fire', { target: 'patrol' }, false, 'no-target'), ord(3001, 'scout', 'watch', { target: 'guard' }, false, 'no-target')],
    [[2407, 'patrol.down', 'The patrol goes down. Its distress signal is already out.'], [3000, 'guard.down', 'The guard falls.']]);
  expect(qStaleRing(p, session).map(x => [x.key, x.severity])).toEqual([['Q-STALE-RING:fire', 'S2'], ['Q-STALE-RING:race', 'info']]);
});

test('Q-LEAK: a cancelled act that lands later, or costs Trail, with no new order of it; another agent\'s own act landing is not one', () => {
  const stop: [number, string, string] = [300, 'cancel.watch', 'scout stops: watch waitress (lost: the time is spent; nothing is learned).'];
  const p = play([ord(100, 'scout', 'watch', { target: 'waitress' }), ord(300, 'scout', 'cancel')], [stop, [500, 'scout.watch.watched', 'Inez (waitress): cell.watch → watched.']], [{ tick: 600, what: 'trail', delta: 1, now: 1, cause: 'watch:waitress' }]);
  expect(qLeak(p, session).map(x => [x.key, x.severity])).toEqual([['Q-LEAK:lands:watch', 'S1'], ['Q-LEAK:cost:watch', 'S2']]);
  const other = play([ord(100, 'scout', 'watch', { target: 'waitress' }), ord(120, 'tech', 'watch', { target: 'waitress' }), ord(300, 'scout', 'cancel')], [stop, [500, 'tech.watch.watched', 'Inez (waitress): cell.watch → watched.']]);
  expect(qLeak(other, session)).toEqual([]);
  const again = play([ord(100, 'scout', 'watch', { target: 'waitress' }), ord(300, 'scout', 'cancel'), ord(320, 'scout', 'watch', { target: 'waitress' })], [stop, [520, 'scout.watch.watched', 'Inez (waitress): watched.']]);
  expect(qLeak(again, session)).toEqual([]);
  expect(humanChecks(play([], []), session)).toEqual([]);
});

const row = (over: Partial<BotReportRow> = {}): BotReportRow => ({
  name: 'human-a', policy: 'human', seed: 4, status: 'ok', preset: 'hold', seconds: 600, endTick: 5100, outcome: 'failure', failCode: 'sealed', cardHead: '',
  input: { decisions: 60, clicks: 60, rings: 40, ringMisses: 0, taken: 40, moves: 20, runs: 2, pans: 0, turns: 3 }, orders: { given: 70, refused: 2, codes: {} },
  frames: { count: 1, meanMs: 4, p99Ms: 6, maxMs: 9, over50: 0 }, memory: { startMB: 0, endMB: 0, slopeMBPerMin: 0 }, consoleErrors: 0, feetSamples: 0, figures: 0, shots: 0, counts: {}, anomalies: [],
  human: { route: 'route-a-board', group: 'A', routes: ['route-a-board'], profile: 'x', steps: 40, done: 38, skipped: 2, counts: { cancel: 40 }, events: [{ t: 1, tick: 98, clock: '0:09.8', kind: 'given', route: 'route-a-board', step: 3, op: 'tech', act: 'move', target: '', detail: '' }], trace: '' }, ...over,
});

test('the lane hook: a row\'s session replays, its findings ride as graded anomalies; a stalled row and an off-rate human row are said', async () => {
  const out = mkdtempSync(join(tmpdir(), 'qa-human-')); mkdirSync(join(out, 'human-a'), { recursive: true }); writeFileSync(join(out, 'human-a', 'session.script.json'), '{}');
  const prof = join(out, 'p.json'); writeFileSync(prof, JSON.stringify({ values: { cancelProb: 0.2 } }));
  const rows = [row(), row({ name: 'greedy-a', policy: 'greedy', human: undefined, input: { ...row().input, taken: 168 }, orders: { given: 4, refused: 0, codes: {} } })];
  await sessionChecks(rows, out, { profileFile: prof, replay: async () => [play([ord(100, 'tech', 'carry', { target: 'scout' })], [])] });
  expect(rows[0]!.anomalies.map(a => `${a.check}:${a.grade!.cause}`).sort()).toEqual(['Q-HUMAN-RATE:cancel', 'Q-INTENT:move->carry']);
  expect(rows[0]!.anomalies.find(a => a.check === 'Q-INTENT')!.script).toBe(join('human-a', 'session.script.json'));
  expect(rows[1]!.anomalies.map(a => a.check)).toEqual(['Q-BOT-STALL']);
  expect(stallFindings(row())).toEqual([]);
});
