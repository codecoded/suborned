// The bands (brief 080): pure judging on small examples, and, where dotnet is installed, the driver's promises on real plays: a plan's run replays exactly from seed + orders,
// the same seed gives the same run, seeds differ, and a plan that reads what a player can't see is rejected. Run: bun test tools/missions/check/bands.test.ts
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { mkdirSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { BAD_PLANS, WINDOWS, bands, heatOf, rigidFromRows, summarize, totalT, wentLoud, type Row } from './bands';
import { ROOT, build, engineAt, env, hasDotnet } from './engine';

setDefaultTimeout(180_000);
const row = (o: Partial<Row>): Row => ({ id: 'p#1', plan: 'p', style: 'prepared-quiet', seed: 1, outcome: 'success', fail: '', tick: 3000, cash: 0, trail: 4, alert: 0, gunfire: 0, standing: 2, startHash: '', finalHash: '', stats: { fightS: 40, shots: 10, hits: 5, hpLost: 1, down: 0 }, marks: {}, idleMaxS: 10, idleAt: 0, inputs: 20, refused: 0, unpriced: 0, giveUps: 0, firstShot: -1, refusals: {}, given: {}, never: [], ...o });
const DIR = join(ROOT, 'contracts/missions/handover/adaptive');

describe('the bands, judged', () => {
  test('Trail and Heat as the checker and §01 count them', () => {
    const r = row({ trail: 5, campaignTrail: 2, openCases: 1, standing: -1, campaignStanding: -1 });
    expect(totalT(r, 3)).toBe(10);
    expect(heatOf(r, 3)).toBe(12);
    expect(heatOf(row({ trail: 4, standing: 3 }), 3)).toBe(4);
  });
  test('a band miss is a line, and a loud win counts only when the fight was loud', () => {
    const rows = [...Array(10).keys()].map(i => row({ seed: i, plan: 'pl', style: 'prepared-loud', outcome: i < 7 ? 'success' : 'failure', fail: i < 7 ? '' : 'enforcer-down', marks: i < 3 ? { 'fight.loud': 100 } : { 'fight.safe': 100 } }));
    expect(wentLoud(rows[0]!)).toBe(true);
    const s = summarize(rows, 3);
    expect(s.winPct).toBe(70);
    expect(s.winsLoud).toBe(3);
    const l = bands([s], rows).find(x => x.id === 'B-4 pl')!;
    expect(l.ok).toBe(false);
    expect(l.value).toBe('30%');
  });
  test('the windows name rules that exist', () => {
    for (const [plan, steps] of Object.entries(WINDOWS)) {
      const d = JSON.parse(readFileSync(join(DIR, `${plan}.adaptive.json`), 'utf8')) as { rules: { id: string }[] };
      for (const s of steps) expect(d.rules.some(r => r.id === s)).toBe(true);
    }
  });
  test('the rigid lines catch a crash, a run with no verdict and a fail with no cause', () => {
    const L = rigidFromRows([row({ outcome: 'none' }), row({ outcome: 'failure', fail: 'x' })], [{ id: 'p#9', error: 'boom' }]);
    expect(L.filter(x => !x.ok).map(x => x.id)).toEqual(['R-1', 'R-2', 'R-3']);
  });
  test('every playing style has a plan', () => {
    const styles = new Set(readdirSync(DIR).map(f => (JSON.parse(readFileSync(join(DIR, f), 'utf8')) as { style: string }).style));
    expect([...styles].sort()).toEqual(['careless', 'prepared-loud', 'prepared-quiet', 'thin-quiet', 'unprepared-loud']);
  });
});

describe.if(hasDotnet())('the driver, played', () => {
  const e = engineAt('tree'), plan = join(DIR, 'quiet-a.adaptive.json'), tmp = join(ROOT, 'out/missions/handover/bands-test');
  const run = (...a: string[]) => { const r = Bun.spawnSync(['dotnet', build(e), 'batch', ...a], { cwd: e.dir, env, stdout: 'pipe', stderr: 'pipe' }); return r.stdout.toString().trim().split('\n').filter(Boolean).map(l => JSON.parse(l)); };
  test('the same seed gives the same run; seeds differ; the run replays from seed + orders through a plain script', () => {
    mkdirSync(tmp, { recursive: true });
    const [a] = run(`${plan}#seed=7`), [b] = run(`${plan}#seed=7`), [c] = run(`${plan}#seed=8`);
    expect(a.replayIdentical).toBe(true);
    expect(a.finalHash).toBe(b.finalHash);
    expect(a.finalHash).not.toBe(c.finalHash);
    const saved = join(tmp, 'saved.json');
    writeFileSync(saved, JSON.stringify({ schema: 'suborned.sim.script/1', mission: 'handover', start: { seed: 7 }, orders: a.orders.map((o: { order: unknown }) => o.order), until: a.tick }));
    const [again] = run(saved, '--digest', '--no-replay');
    expect(again.finalHash).toBe(a.finalHash);
  });
  test('a plan that reads what a player cannot see is rejected, not played', () => {
    mkdirSync(tmp, { recursive: true });
    for (const [i, b] of BAD_PLANS.entries()) {
      const f = join(tmp, `bad-${i}.adaptive.json`); writeFileSync(f, JSON.stringify(b.plan));
      const [l] = run(f, '--digest', '--no-replay');
      expect(l.error ?? '').toMatch(b.why);
    }
  });
});
