// route-diff (brief 034 item 1): the pure comparison (each column, wording-only vs behaviour, health from the log, the matrix table) and,
// where dotnet is installed, the real engine: the working tree against itself moves nothing; a route script made 5 s late moves.
// Run: bun test tools/route-diff
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { cpSync, mkdirSync, mkdtempSync, readFileSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { ROOT, hasDotnet } from '../missions/check/engine';
import { compare, compareAll, compareLab, firstDivergence, healthLost, parseLab, renderLab, renderTable, type Row } from './core';
import { playRoutes, resolveSide } from './cli';

setDefaultTimeout(180_000);
const row = (o: Partial<Row> = {}): Row => ({
  id: 'r', outcome: 'success', fail: '', tick: 3000, trail: 4, alert: 1, gunfire: 0, standing: 3, health: { lost: 0, total: 18, pct: 0 }, startHash: 's', finalHash: 'f', scriptSha: 'x',
  log: ['0\tstart\tgo', '100\torder.move\tscout runs', '900\tenter.in\tthe enforcer is in'], checkpoints: [[10, 'h1'], [3000, 'h2']], ...o,
});
const MAX = { op: 4, enforcer: 6 };

describe('health lost, from the log', () => {
  test('max minus the last "health left" per operative; all of it once down', () => {
    const log = [{ code: 'scout.hit.fight', text: 'security-2 hits the scout. 3 health left.' }, { code: 'scout.hit.fight', text: 'security-1 hits the scout. 1 health left.' }, { code: 'tech.hit.fight', text: 'x hits the tech. 0 health left.' }, { code: 'tech.down', text: 'The tech is down.' }, { code: 'enforcer.hit.fight', text: 'x hits the enforcer. 4 health left.' }];
    expect(healthLost(log, MAX)).toEqual({ lost: 3 + 4 + 2, total: 18, pct: 50 });
    expect(healthLost([], MAX)).toEqual({ lost: 0, total: 18, pct: 0 });
  });
  test('a lab start (health × 10) is read from the numbers, not read as a loss of nothing', () => {
    expect(healthLost([{ code: 'scout.hit.fight', text: 'x hits the scout. 30 health left.' }], { ...MAX, lab: 10 }).total).toBe(180);
    expect(healthLost([{ code: 'scout.hit.fight', text: 'x hits the scout. 30 health left.' }], { ...MAX, lab: 10 }).lost).toBe(10);
  });
});

describe('what counts as moved', () => {
  test('identical runs are the same', () => { expect(compare('r', row(), row()).status).toBe('same'); });
  test('each column moves a route, and the table says which', () => {
    for (const [c, edit] of [['outcome', { outcome: 'failure', fail: 'sealed' }], ['end', { tick: 3100 }], ['trail', { trail: 5 }], ['alert', { alert: 2 }], ['health', { health: { lost: 4, total: 18, pct: 22 } }]] as const) {
      const m = compare('r', row(), row(edit));
      expect(m.status).toBe('moved'); expect(m.cols).toContain(c);
    }
  });
  test('the first log line apart is named, and a reworded line alone is not a move', () => {
    const b = row({ log: ['0\tstart\tgo', '100\torder.move\tscout runs', '950\tenter.in\tthe enforcer is in'] });
    const m = compare('r', row(), b);
    expect(m.status).toBe('moved'); expect(m.divergence).toMatchObject({ at: 2, a: '900\tenter.in\tthe enforcer is in', b: '950\tenter.in\tthe enforcer is in' });
    const w = compare('r', row(), row({ log: ['0\tstart\tgo', '100\torder.move\tscout heads', '900\tenter.in\tthe enforcer is in'] }));
    expect(w.status).toBe('same'); expect(w.divergence?.wordingOnly).toBe(true);
    expect(firstDivergence(row(), row({ log: row().log.slice(0, 2) }))).toMatchObject({ at: 2, b: undefined }); // one log ends early
  });
  test('hashes speak only when both runs began from the same state', () => {
    expect(compare('r', row(), row({ startHash: 'other', finalHash: 'g', checkpoints: [[10, 'x'], [3000, 'y']] })).status).toBe('same'); // inputs differ: every hash differs
    const m = compare('r', row(), row({ finalHash: 'g', checkpoints: [[10, 'h1'], [3000, 'zz']] }));
    expect(m.status).toBe('moved'); expect(m.divergence).toMatchObject({ at: -1, checkpointTick: 3000 }); // same start, same log, other state
  });
  test('a route on one side only, and an engine refusal, are moves', () => {
    expect(compareAll(new Map([['a', row()]]), new Map([['a', row()], ['b', row()]])).map(m => m.status)).toEqual(['same', 'added']);
    expect(compare('r', row(), row({ outcome: 'error', error: 'unknown key' })).status).toBe('moved');
  });
  test('the table lists only what moved, with the divergence under it', () => {
    const t = renderTable(compareAll(new Map([['calm', row()], ['late', row()]]), new Map([['calm', row()], ['late', row({ tick: 3100, log: ['0\tstart\tgo'] })]])), { labelA: 'A', labelB: 'B' });
    expect(t).toContain('late'); expect(t).not.toContain('calm'); expect(t).toContain('5:00.0 → 5:10.0'); expect(t).toContain('first log line apart (line 2)'); expect(t).toContain('1 moved, 1 unchanged');
  });
});

describe('the fight-lab matrix', () => {
  const sample = [
    'preset          run  result            end      T(m/tot) A  St(s→e) pub     inv     cases down capt hp-lost hits foes shots free/side  contact verdict',
    'today           P-A  success           5:37.1    5/5     1  2→3   calm    active      0    0    0     0 %    0    3    17    2/2    c0/0 s0   pass',
    'today           P-G  success           4:33.0    6/6     1  2→3   calm    active      0    1    0    55 %   10    3    24    0/0    c0/2 s18  FAIL — 1 op down; health lost 55 % > 33',
    'lab             P-B  failure enforcer-down 2:10.0    3/3     0  2→1   calm    active      0    1    1    80 %    9    2    29    1/0    c0/0 s24  FAIL — health lost 80 % > 25',
  ].join('\n');
  test('parses the printed rows', () => {
    const r = parseLab(sample);
    expect(r.map(x => x.key)).toEqual(['today P-A', 'today P-G', 'lab P-B']);
    expect(r[1]).toMatchObject({ result: 'success', end: '4:33.0', trail: '6/6', health: '55 %', down: '1', verdict: 'FAIL — 1 op down; health lost 55 % > 33' });
    expect(r[2]).toMatchObject({ result: 'failure enforcer-down', capt: '1', standing: '2→1' });
  });
  test('a changed row is named by column', () => {
    const a = parseLab(sample), b = parseLab(sample.replace('55 %', '33 %'));
    const m = compareLab(a, b);
    expect(m.filter(x => x.status === 'moved').map(x => [x.key, x.cols])).toEqual([['today P-G', ['health']]]);
    expect(renderLab(m, {})).toContain('55 % → 33 %');
  });
});

describe.skipIf(!hasDotnet())('the real engine', () => {
  const dir = mkdtempSync(join(tmpdir(), 'route-diff-'));
  const same = join(dir, 'same'), late = join(dir, 'late');
  for (const d of [same, late]) mkdirSync(d);
  for (const f of ['route-a-board.json', 'withdraw.json']) cpSync(join(ROOT, 'contracts/missions/handover/routes', f), join(same, f));
  cpSync(join(same, 'withdraw.json'), join(late, 'withdraw.json'));
  const s = JSON.parse(readFileSync(join(same, 'route-a-board.json'), 'utf8'));
  for (const o of s.orders) o.tick += 50; // every order 5 s late
  writeFileSync(join(late, 'route-a-board.json'), JSON.stringify(s));
  test('the working tree against itself moves nothing', async () => {
    const side = resolveSide('tree'), a = await playRoutes(side, same, undefined, false), b = await playRoutes(side, same, undefined, false);
    expect([...a.keys()].sort()).toEqual(['route-a-board', 'withdraw']);
    expect(compareAll(a, b).map(m => m.status)).toEqual(['same', 'same']);
  });
  test('a route script made 5 s late moves, and the move is found', async () => {
    const side = resolveSide('tree'), a = await playRoutes(side, same, undefined, false), b = await playRoutes(side, late, undefined, false);
    const m = compareAll(a, b), r = m.find(x => x.id === 'route-a-board')!;
    expect(m.find(x => x.id === 'withdraw')!.status).toBe('same');
    expect(r.status).toBe('moved'); expect(r.scriptChanged).toBe(true);
    expect(r.divergence?.at).toBeGreaterThanOrEqual(0);
    expect(a.get('route-a-board')!.outcome).toBe('success');
  });
});
