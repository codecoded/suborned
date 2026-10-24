// Brief 060 tier 2 (Suture): Sky's sessions told from automation, the measures on a made-up session, and the profile as Latch's reader takes it.
import { expect, test } from 'bun:test';
import { existsSync, readFileSync } from 'node:fs';
import { bouts, decisions, measure, quant, refusalFamily, skySessions, toProfile, PROFILE_OUT, type O, type Sess } from './human-measure';
import { readProfile } from './human';

const sess = (id: string, orders: O[], o: Partial<Sess> = {}): Sess => {
  const until = o.until ?? 3000, start = o.start ?? Number(id.replace(/\D/g, '')) * 1000;
  return { file: id, id, start, wall: o.wall ?? until / 10 * 1.05, until, ratio: (o.wall ?? until / 10 * 1.05) / (until / 10), seq: orders.filter(x => x.do !== 'drive').map(x => `${x.op}:${x.do}:${x.target ?? ''}`).join('|'), orders, engine: 'suborned.sim/0.18.0', outcome: 'none', checkpoints: [], mode: 'handover', copied: 0, ...o };
};
const mv = (tick: number, op: string, x: number, y: number, run = false): O => ({ tick, op, do: 'move', to: [x, y], ...(run ? { run } : {}) });
const act = (tick: number, op: string, d: string, target = ''): O => ({ tick, op, do: d, ...(target ? { target } : {}) });

test('whose: a real-time unique session is Sky\'s; a fast one, a repeated real-time sequence, a full copy and a host repeat are not; a partial copy keeps its new orders', () => {
  const a = [mv(10, 'scout', 0, 0), act(50, 'scout', 'watch', 'waitress'), mv(400, 'fixer', 500, 500)];
  const sky = sess('s1', a), fast = sess('s2', [mv(10, 'tech', 1, 1), mv(20, 'tech', 2, 2)], { wall: 10 });
  const proofA = sess('s3', [mv(10, 'enforcer', 9, 9), act(30, 'enforcer', 'enter')]), proofB = sess('s4', [mv(12, 'enforcer', 9, 9), act(33, 'enforcer', 'enter')]);
  const copy = sess('s5', a.map(x => ({ ...x }))), more = sess('s6', [...a.map(x => ({ ...x })), act(900, 'tech', 'loop', 'entrance')]);
  const burst = sess('s7', Array.from({ length: 6 }, () => ({ tick: 40, op: undefined, do: 'follow', on: true }) as O));
  const c = skySessions([sky, fast, proofA, proofB, copy, more, burst]);
  expect(c.sky.map(s => s.id)).toEqual(['s1', 's6']);
  expect(c.sky[1]!.copied).toBe(3); expect(c.sky[1]!.copiedFrom).toBe('s1');
  expect(c.dropped).toMatchObject({ 'fast (automation)': 1, 'repeated real-time sequence (proof)': 2, 'a copy of an earlier session': 1, 'a burst no hand makes': 1 });
});

test('decisions: a squad order, a double click and a cancel-then-move are one decision each; drive samples make third-person bouts', () => {
  const o = [mv(10, 'scout', 0, 0), mv(10, 'fixer', 0, 100), mv(12, 'scout', 0, 0, true), act(100, 'tech', 'cancel'), mv(100, 'tech', 5, 5), act(200, 'scout', 'watch', 'guard'),
    ...[300, 301, 302, 340].map(t => ({ tick: t, op: 'tech', do: 'drive', to: [t, 0] }) as O), { tick: 500, op: 'tech', do: 'drive', to: [0, 0] } as O];
  const d = decisions(o);
  expect(d.map(x => [x.tick, x.ops.join('+'), x.main.do])).toEqual([[10, 'fixer+scout', 'move'], [100, 'tech', 'move'], [200, 'scout', 'watch']]);
  expect(bouts(o)).toEqual([{ op: 'tech', from: 300, to: 340 }, { op: 'tech', from: 500, to: 500 }]);
});

test('measures: an act given another order before it lands, a move sent elsewhere, a nudge, a repeat, an idle, an abandoned line', () => {
  const o: O[] = [
    mv(0, 'scout', 0, 0), act(100, 'scout', 'watch', 'waitress'), mv(150, 'scout', 1000, 0), // the watch (20 s) is replaced 5 s in
    mv(300, 'scout', 2000, 0), mv(310, 'scout', 2000, 1500), // 10 m out, 1 s later: sent 15 m off at 90 degrees (a change of mind)
    mv(600, 'fixer', 0, 0), mv(700, 'fixer', 3000, 0), mv(710, 'fixer', 3100, 0), // a nudge along the way
    act(1000, 'scout', 'watch', 'waitress'), // the same act again: a repeat
    act(1500, 'tech', 'watch', 'camera:entrance'), // a line never finished (the run goes on 150 s)
  ];
  const s = sess('s9', o, { until: 3000 }), m = measure([s], { sky: [s], dropped: {} });
  expect(m.cancel.actsReplacedBeforeLanding).toMatchObject({ k: 1, n: 3 });
  expect(m.cancel.movesRedirected.k).toBe(1); expect(m.cancel.movesNudged.k).toBe(1);
  expect(m.reorders.sameAgainSameTarget.k).toBe(1);
  expect(m.gaps.idle.k).toBeGreaterThan(0); // 500 -> 600 -> 700 -> 1000 -> 1500: gaps of 30 s and 50 s
  expect(m.abandon.lines).toMatchObject({ k: 2, n: 2 }); // waitress: watched, never turned or called; the camera: never looped
  expect(m.ends.quit.k).toBe(1);
});

test('quantiles and refusal families', () => {
  expect(quant([1, 2, 3, 4, 5])).toMatchObject({ n: 5, p50: 3, p10: 1.4, p90: 4.6 });
  expect(refusalFamily('', "that ground can't be walked (a roof)")).toBe('ground that cannot be walked');
  expect(refusalFamily('agent-busy')).toBe('agent busy'); expect(refusalFamily('', 'tech is busy until 1694')).toBe('agent busy');
  expect(refusalFamily('needs-routine')).toBe('not ready (knowledge or reach)');
});

test('the profile: every value Latch reads comes from the file (none defaulted), each with its source; the probabilities are chances, the times {p50, p90}', () => {
  const s = sess('s10', [mv(0, 'scout', 0, 0), act(100, 'scout', 'watch', 'waitress'), mv(130, 'scout', 900, 0), act(700, 'fixer', 'turn', 'waitress')]);
  const p = toProfile(measure([s], { sky: [s], dropped: {} }));
  const r = readProfile(p);
  expect(r.source.defaulted).toEqual([]); expect(r.source.ignored).toEqual(['repeatProb']); // repeatProb: Sky's repeats, for the repeat behaviour Latch is adding
  for (const k of Object.keys(p.values)) expect(typeof (p.source as Record<string, string>)[k]).toBe('string');
  for (const [k, v] of Object.entries(p.values)) if (typeof v === 'number') { expect(v).toBeGreaterThanOrEqual(0); expect(v).toBeLessThanOrEqual(1); } else expect((v as { p50: number }).p50, k).toBeGreaterThanOrEqual(0);
  if (existsSync(PROFILE_OUT)) { const f = readProfile(JSON.parse(readFileSync(PROFILE_OUT, 'utf8'))); expect(f.source.defaulted).toEqual([]); } // the committed file reads whole
});

test('seeds: every saved session over 8 KB, and Sky\'s own at any size, each tagged sky or automation', async () => {
  const { mkdtempSync, writeFileSync, utimesSync } = await import('node:fs'), { tmpdir } = await import('node:os'), { join } = await import('node:path'), { sessionSeeds } = await import('./seeds');
  const dir = mkdtempSync(join(tmpdir(), 'qa-seeds-'));
  const put = (stamp: string, orders: O[], until: number, wallS: number, pad = 0) => {
    const f = join(dir, `handover-${stamp}.script.json`);
    writeFileSync(f, JSON.stringify({ schema: 'suborned.sim.script/1', mission: 'handover', start: {}, orders, until, expect: { outcome: 'none' }, pad: 'x'.repeat(pad) }));
    const t0 = new Date(+stamp.slice(0, 4), +stamp.slice(4, 6) - 1, +stamp.slice(6, 8), +stamp.slice(9, 11), +stamp.slice(11, 13), +stamp.slice(13, 15)).getTime() / 1000;
    utimesSync(f, t0 + wallS, t0 + wallS);
  };
  put('20261022-100000', [mv(10, 'scout', 0, 0), act(60, 'scout', 'watch', 'waitress')], 1200, 130); // Sky, small: a seed
  put('20261022-110000', [mv(10, 'tech', 5, 5), act(40, 'tech', 'loop', 'entrance')], 1200, 20); // automation, small: not
  put('20261022-120000', [mv(10, 'fixer', 7, 7), act(40, 'fixer', 'turn', 'waitress')], 1200, 20, 9000); // automation, over 8 KB: a seed
  const s = sessionSeeds(dir);
  expect(s.map(x => [x.id, x.by])).toEqual([['s-20261022-100000', 'sky'], ['s-20261022-120000', 'automation']]);
});
