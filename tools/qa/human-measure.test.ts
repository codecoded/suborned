// Brief 060 tier 2 (Suture): Sky's sessions told from automation, the measures on a made-up session, and the profile as Latch's reader takes it.
import { expect, test } from 'bun:test';
import { existsSync, readFileSync } from 'node:fs';
import {
  actLength, bouts, decisions, engineBook, engineCommits, engineRules, measure, paceWords, quant, refusalFamily, skySessions, toProfile, treeRules, verCmp,
  LOGGED_ACT_S, PROFILE_OUT, RULES, type Book, type EngineCommit, type O, type Sess,
} from './human-measure';
import { readProfile } from './human';

const sess = (id: string, orders: O[], o: Partial<Sess> = {}): Sess => {
  const until = o.until ?? 3000, start = o.start ?? Number(id.replace(/\D/g, '')) * 1000;
  return { file: id, id, start, wall: o.wall ?? until / 10 * 1.05, until, ratio: (o.wall ?? until / 10 * 1.05) / (until / 10), seq: orders.filter(x => x.do !== 'drive').map(x => `${x.op}:${x.do}:${x.target ?? ''}`).join('|'), orders, engine: 'suborned.sim/0.18.0', outcome: 'none', checkpoints: [], mode: 'handover', copied: 0, ...o };
};
const mv = (tick: number, op: string, x: number, y: number, run = false): O => ({ tick, op, do: 'move', to: [x, y], ...(run ? { run } : {}) });
const act = (tick: number, op: string, d: string, target = ''): O => ({ tick, op, do: d, ...(target ? { target } : {}) });
// a session's rules as its engine's rulebook gives them (engineRules): the old pace (1.4 / 2.8 m/s, the 62 s Loop of engines up to 0.30.0) and 0.34's
const rulebook = (v: Record<string, number>) => ({ values: Object.fromEntries(Object.entries(v).map(([k, x]) => [k, { v: x, unit: k.endsWith('cm-s') ? 'cm-per-s' : 's' }])) });
const ACTS = { 'watch-s': 20, 'listen-s': 42, 'reroute-s': 45, 'mislead-s': 44, 'enter-s': 38, 'take-s': 38, 'carry-s': 3, 'board-s': 1, 'ko-s': 6, 'kill-s': 3 };
const PEOPLE = { approaches: { ask: { timeS: 25 }, pay: { timeS: 50 }, help: { timeS: 50 }, pressure: { timeS: 35 }, rule: 'a string beside the approaches, as in people.rules.json' } };
const fixed = (rb: unknown): Book => (engine, _at, commit) => engineRules(rb, PEOPLE, engine, commit ?? 'fixed');
const OLD = fixed(rulebook({ 'op-speed-cm-s': 140, 'op-run-speed-cm-s': 280, 'loop-s': 62, ...ACTS })), NEW = fixed(rulebook({ 'op-speed-cm-s': 180, 'op-run-speed-cm-s': 360, 'loop-s': 20, ...ACTS }));

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
  const s = sess('s9', o, { until: 3000 }), m = measure([s], { sky: [s], dropped: {} }, new Map(), RULES, OLD);
  expect(m.cancel.actsReplacedBeforeLanding).toMatchObject({ k: 1, n: 3 });
  expect(m.cancel.movesRedirected.k).toBe(1); expect(m.cancel.movesNudged.k).toBe(1);
  expect(m.reorders.sameAgainSameTarget.k).toBe(1);
  expect(m.gaps.idle.k).toBeGreaterThan(0); // 500 -> 600 -> 700 -> 1000 -> 1500: gaps of 30 s and 50 s
  expect(m.abandon.lines).toMatchObject({ k: 2, n: 2 }); // waitress: watched, never turned or called; the camera: never looped
  expect(m.ends.quit.k).toBe(1);
});

test('the engine\'s numbers: the walk, the run and each act\'s length come from the session\'s rulebook; what no rulebook gives keeps the engine log\'s', () => {
  const e = engineRules(rulebook({ 'op-speed-cm-s': 180, 'op-run-speed-cm-s': 360, 'loop-s': 20, 'watch-s': 20, 'ko-s': 6, 'kill-s': 3 }), PEOPLE, 'suborned.sim/0.34.1', 'tree');
  expect([e.walkCmS, e.runCmS]).toEqual([180, 360]);
  expect([actLength(e, { do: 'loop' }), actLength(e, { do: 'watch' })]).toEqual([20, 20]);
  expect([actLength(e, { do: 'takedown', how: 'knockout' }), actLength(e, { do: 'takedown', how: 'kill' })]).toEqual([6, 3]);
  expect([actLength(e, { do: 'turn', approach: 'ask' }), actLength(e, { do: 'turn', approach: 'pressure' }), actLength(e, { do: 'turn', approach: 'help' }), actLength(e, { do: 'turn' })]).toEqual([25, 35, 50, LOGGED_ACT_S.turn]);
  expect([actLength(e, { do: 'call' }), actLength(e, { do: 'board' })]).toEqual([10, LOGGED_ACT_S.board]); // the call has no rulebook value; this rulebook has no board-s (none before 0.6.3)
  expect([actLength(e, { do: 'move' }), actLength(e, { do: 'draw' })]).toEqual([undefined, undefined]);
  const early = engineRules(rulebook({ 'op-speed-cm-s': 140 }), undefined, 'suborned.sim/0.3.0', 'c0'); // the first 0.3.0 commits: no run yet
  expect([early.walkCmS, early.runCmS, early.actS['turn:ask']]).toEqual([140, undefined, undefined]);
  expect(engineRules(undefined, undefined, '', 'x').walkCmS).toBeUndefined();
  expect(['suborned.sim/0.34.1', '0.4.0', 'suborned.sim/0.26.0', '0.3.0'].sort(verCmp)).toEqual(['0.3.0', '0.4.0', 'suborned.sim/0.26.0', 'suborned.sim/0.34.1']);
});

test('the book: the commit a replay matched, else the tree at its own version, else a commit of the version, else the last committed engine before it', () => {
  const commits: EngineCommit[] = [ // newest first, as git log gives them
    { c: 'c4', at: '20261026-103138', v: 'suborned.sim/0.34.0' }, { c: 'c3', at: '20261026-052531', v: 'suborned.sim/0.33.1' },
    { c: 'c2', at: '20261021-120000', v: 'suborned.sim/0.18.0' }, { c: 'c1', at: '20261021-100000', v: 'suborned.sim/0.18.0' },
  ];
  const walk: Record<string, number> = { c1: 141, c2: 142, c3: 143, c4: 180 }, shown: string[] = [];
  const show = (c: string, path: string) => { shown.push(`${c}:${path}`); return JSON.stringify(path.endsWith('rulebook.json') ? rulebook({ 'op-speed-cm-s': walk[c]!, 'op-run-speed-cm-s': walk[c]! * 2 }) : PEOPLE); };
  const book = engineBook({ commits: () => commits, show, tree: () => ({ version: 'suborned.sim/0.34.1', rulebook: rulebook({ 'op-speed-cm-s': 181, 'op-run-speed-cm-s': 362 }), people: PEOPLE }) });
  const at = (engine: string, when: string, commit?: string) => { const e = book(engine, when, commit); return `${e.from} ${e.walkCmS}/${e.runCmS}`; };
  expect(at('suborned.sim/0.18.0', '20261021-110000', 'c3')).toBe('c3 143/286'); // a replay's commit wins
  expect(at('suborned.sim/0.34.1', '20261026-130000')).toBe('tree 181/362'); // the tree's own version, not committed yet
  expect(at('suborned.sim/0.18.0', '20261021-110000')).toBe('c1 141/282'); // the newest commit of the version at or before the session
  expect(at('suborned.sim/0.18.0', '20261022-000000')).toBe('c2 142/284');
  expect(at('suborned.sim/0.18.0', '20261020-000000')).toBe('c1 141/282'); // a session older than its version's commits: the first
  expect(at('suborned.sim/0.25.0', '20261024-000000')).toBe('c2 142/284'); // never committed: the last committed engine before it
  expect(at('suborned.sim/0.34.0', '20261026-110000')).toBe('c4 180/360');
  expect(at('', '20261026-110000')).toBe('tree 181/362'); // no engine recorded
  expect(new Set(shown).size).toBe(shown.length); // each commit's files read once
});

test('measures: a move is timed at the walk or run of the session\'s own engine, an act at its length there (1.4 / 2.8 m/s and a 62 s Loop before, 1.8 / 3.6 and 20 s now)', () => {
  const o: O[] = [
    mv(0, 'scout', 0, 0), mv(100, 'scout', 1000, 0), mv(160, 'scout', 1000, 1500), // 10 m out, sent 15 m off at a right angle 6 s later: 7.1 s at 1.4 m/s, 5.6 s at 1.8
    mv(0, 'fixer', 0, 0), mv(300, 'fixer', 1000, 0, true), mv(330, 'fixer', 1000, 1500), // a 10 m run sent off 3 s later: 3.6 s at 2.8 m/s, 2.8 s at 3.6
    act(500, 'tech', 'loop', 'entrance'), mv(800, 'tech', 0, 0), // the Tech given a move 30 s into the Loop: inside 62 s, past 20 s
  ];
  const s = sess('s11', o, { until: 3000 }), cls = { sky: [s], dropped: {} };
  const old = measure([s], cls, new Map(), RULES, OLD), now = measure([s], cls, new Map(), RULES, NEW);
  expect(old.cancel.movesRedirected).toMatchObject({ k: 2, n: 4 }); expect(now.cancel.movesRedirected).toMatchObject({ k: 0, n: 4 });
  expect(old.cancel.actsReplacedBeforeLanding).toMatchObject({ k: 1, n: 1 }); expect(now.cancel.actsReplacedBeforeLanding).toMatchObject({ k: 0, n: 1 });
  expect(old.paces).toEqual([{ engine: 'suborned.sim/0.18.0', from: 'fixed', walkCmS: 140, runCmS: 280, sessions: 1 }]);
  expect(paceWords(old.paces)).toBe('1.4 m/s walking, 2.8 running (engine 0.18.0)');
  expect(toProfile(now).source.cancelProb).toContain('1.8 m/s walking, 3.6 running (engine 0.18.0)');
  expect(toProfile(now).source.cancelProb).not.toContain('1.4 m/s');
});

// the repo's own history: engine 0.18.0 and 0.34.0 are committed (git log over sim/Suborned.Sim)
const history = engineCommits();
test.skipIf(!['suborned.sim/0.18.0', 'suborned.sim/0.34.0'].every(v => history.some(c => c.v === v)))('the book on the repo\'s history: a 0.18.0 session at 1.4 / 2.8 m/s and a 62 s Loop, a 0.34.0 one at 1.8 / 3.6 and 20 s, the tree\'s engine at the tree\'s rulebook', () => {
  const book = engineBook({ commits: () => history }), t = treeRules(), tv = (t.rulebook as { values: Record<string, { v: number }> }).values;
  const old = book('suborned.sim/0.18.0', '20261021-150000'), now = book('suborned.sim/0.34.0', '20261026-110000'), tree = book(t.version, '99991231-235959');
  expect([old.walkCmS, old.runCmS, old.actS.loop]).toEqual([140, 280, 62]); expect(old.from).not.toBe('tree');
  expect([now.walkCmS, now.runCmS, now.actS.loop]).toEqual([180, 360, 20]);
  expect(t.version).toMatch(/^suborned\.sim\/\d+\.\d+\.\d+$/);
  expect([tree.from, tree.walkCmS, tree.runCmS]).toEqual(['tree', tv['op-speed-cm-s']!.v, tv['op-run-speed-cm-s']!.v]);
});

test('quantiles and refusal families', () => {
  expect(quant([1, 2, 3, 4, 5])).toMatchObject({ n: 5, p50: 3, p10: 1.4, p90: 4.6 });
  expect(refusalFamily('', "that ground can't be walked (a roof)")).toBe('ground that cannot be walked');
  expect(refusalFamily('agent-busy')).toBe('agent busy'); expect(refusalFamily('', 'tech is busy until 1694')).toBe('agent busy');
  expect(refusalFamily('needs-routine')).toBe('not ready (knowledge or reach)');
});

test('the profile: every value Latch reads comes from the file (none defaulted), each with its source; the probabilities are chances, the times {p50, p90}', () => {
  const s = sess('s10', [mv(0, 'scout', 0, 0), act(100, 'scout', 'watch', 'waitress'), mv(130, 'scout', 900, 0), act(700, 'fixer', 'turn', 'waitress')]);
  const p = toProfile(measure([s], { sky: [s], dropped: {} }, new Map(), RULES, OLD));
  const r = readProfile(p);
  expect(r.source.defaulted).toEqual(['thirdPersonRowProb', 'thirdPersonInRowProb']); expect(r.source.ignored).toEqual([]); // Latch 2026-10-25: repeatProb is read now; the third-person row model's two rates default (0.21, 0.27) until this file writes them
  for (const k of Object.keys(p.values)) expect(typeof (p.source as Record<string, string>)[k]).toBe('string');
  for (const [k, v] of Object.entries(p.values)) if (typeof v === 'number') { expect(v).toBeGreaterThanOrEqual(0); expect(v).toBeLessThanOrEqual(1); } else expect((v as { p50: number }).p50, k).toBeGreaterThanOrEqual(0);
  if (existsSync(PROFILE_OUT)) { const f = readProfile(JSON.parse(readFileSync(PROFILE_OUT, 'utf8'))); expect(f.source.defaulted).toEqual(['thirdPersonRowProb', 'thirdPersonInRowProb']); } // the committed file reads whole (but for the row model's two rates, above)
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

test('seeds: today\'s player\'s folder reads the folders earlier players saved in too; folders merge in time order, a name in two is one seed', async () => {
  const { mkdtempSync, writeFileSync } = await import('node:fs'), { tmpdir } = await import('node:os'), { join } = await import('node:path');
  const { sessionSeeds, sessionFolders, SESSIONS_DIR, EARLIER_SESSIONS_DIRS } = await import('./seeds');
  expect(sessionFolders(SESSIONS_DIR)).toEqual([SESSIONS_DIR, ...EARLIER_SESSIONS_DIRS]);
  expect(SESSIONS_DIR).toEndWith('DefaultCompany/The Suborned/play'); // Application.persistentDataPath/play of the player since 2026-10-24 04:01
  expect(EARLIER_SESSIONS_DIRS.some(d => d.endsWith('com.Unity-Technologies.com.unity.template.urp-blank/play'))).toBe(true); // 17 to 24 October
  const other = mkdtempSync(join(tmpdir(), 'qa-seeds-')); expect(sessionFolders(other)).toEqual([other]);
  const now = mkdtempSync(join(tmpdir(), 'qa-now-')), before = mkdtempSync(join(tmpdir(), 'qa-before-'));
  const put = (dir: string, stamp: string, tag: string) => writeFileSync(join(dir, `handover-${stamp}.script.json`), JSON.stringify({ schema: 'suborned.sim.script/1', mission: 'handover', start: {}, orders: [{ tick: 10, op: 'scout', do: 'move', to: [0, 0] }], until: 600, expect: { outcome: 'none' }, tag, pad: 'x'.repeat(9000) }));
  put(now, '20261025-100000', 'now'); put(now, '20261024-050000', 'now'); put(before, '20261023-100000', 'before'); put(before, '20261024-050000', 'before');
  const s = sessionSeeds([now, before]);
  expect(s.map(x => x.id)).toEqual(['s-20261023-100000', 's-20261024-050000', 's-20261025-100000']);
  expect((s[1]!.script as unknown as { tag: string }).tag).toBe('now'); // the first folder's
});
