// What a skyline candidate is: the proposals applied, the timetable a moved station leaves stale derived again, and the
// Handover's rules run on the result (brief 007 F review: P2a, M4).
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { mkdtemp } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { root } from '../../../rag/config';
import { kitStatus, type Known } from '../apply';
import { candidatePlan, checkAll } from '../candidate';
import { CONCEPTS_FILE, parseConcepts, tileSet } from '../concepts';
import { baselineFailures, brokenBy, failedMission, MISSION_FILE, missionChecks, type Mission } from '../mission';
import { KIT, datum, loadPlan, stations, vantage } from './edits';

setDefaultTimeout(180000);
const plan = await loadPlan();
const block = await Bun.file(resolve(root, 'contracts/world/civic-block.json')).json();
const mission: Mission = await Bun.file(resolve(root, MISSION_FILE)).json();
const known: Known = {
  tiles: tileSet(parseConcepts(await Bun.file(resolve(root, CONCEPTS_FILE)).text())),
  kit: kitStatus(await Bun.file(KIT).json()),
};
const VERSION = '0.3.0-r001.1';
type Run = { from: string; to: string; sec: number };
type Timed = { lines: { line: string; runs: Run[] }[]; journeys: unknown[] };
const timetable = (p: unknown) => (p as { timetable: Timed }).timetable;
const runsOf = (t: Timed, line: string) => t.lines.find(l => l.line === line)!.runs;

describe('the timetable follows the stations', () => {
  const moved = (at: [number, number]) =>
    vantage({ stops: stations(plan).map(s => (s.id === 'gardens' ? { ...s, at } : s)) });
  const build = (v = vantage({ stops: stations(plan) })) =>
    candidatePlan(plan, { datum: datum({}), vantage: v }, VERSION, known, block);

  test('stations restated where they are leave the timetable as it was', async () => {
    const c = await build();
    expect(c.problems).toEqual([]);
    expect(timetable(c.plan)).toEqual(timetable(plan));
  });

  test('a station moved along the line changes the run times of its line, and no other line', async () => {
    const before = timetable(plan), c = await build(moved([-100, 0])), after = timetable(c.plan);
    expect(c.problems).toEqual([]);
    expect(runsOf(after, 'S1')).not.toEqual(runsOf(before, 'S1'));
    expect(runsOf(after, 'S1').map(r => [r.from, r.to])).toEqual(runsOf(before, 'S1').map(r => [r.from, r.to]));
    expect(runsOf(after, 'T1')).toEqual(runsOf(before, 'T1'));
    expect(runsOf(after, 'T2')).toEqual(runsOf(before, 'T2'));
    expect(after.journeys.length).toBe(before.journeys.length);
    expect(after.journeys).not.toEqual(before.journeys); // a journey that rides S1 is longer or shorter now
  });

  test('removing the sky rail line leaves journeys that ride it with nowhere to go: Datum is told', async () => {
    const c = await candidatePlan(
      plan, { datum: datum({ rail: { lines: [], remove: ['S1'] } }) }, VERSION, known, block,
    );
    expect(c.problems.length).toBeGreaterThan(0);
    expect(c.problems.every(p => p.owner === 'Datum' && p.text.startsWith('timetable:'))).toBe(true);
  });
});

describe('the Handover mission is part of the verdict (M4)', () => {
  const lost = async () => {
    const dir = await mkdtemp(join(tmpdir(), 'mission-')), file = join(dir, 'mission.json');
    const off = { ...mission, destinations: mission.destinations.map((d, i) => (i ? d : { ...d, at: [9000, 9000] })) };
    await Bun.write(file, JSON.stringify(off));
    return { file, off: off as Mission };
  };

  test('the plan as it stands passes the Handover rules; its destination off the map fails C-3', async () => {
    expect(await baselineFailures(plan)).toEqual([]);
    const { file, off } = await lost();
    expect(await baselineFailures(plan, file)).toContain('C-3');
    expect(failedMission(missionChecks(plan, block, off))).toContain('C-3');
  });

  test('checkAll lists the Handover rules in the report and fails a candidate that breaks one', async () => {
    const { off } = await lost();
    const good = await checkAll(plan, block, mission, { kit: KIT }), bad = await checkAll(plan, block, off, { kit: KIT });
    const handover = (c: typeof good) => c.checks.filter(x => x.group === 'Handover');
    expect(handover(good).length).toBeGreaterThan(0); expect(handover(good).every(x => x.ok)).toBe(true);
    expect(good.ok).toBe(!good.checks.some(x => !x.ok));
    expect(bad.ok).toBe(false);
    expect(handover(bad).filter(x => !x.ok).map(x => x.id)).toContain('C-3');
  });

  test('a broken Handover rule is Datum\'s when his edits alone break it, Vantage\'s when only the stations do', () => {
    const rule = (id: string, ok: boolean) => ({ id, ok, group: 'Handover' }) as never;
    const alone = [rule('C-3', false), rule('M-1', true)];
    expect(brokenBy(['C-3', 'M-1'], alone)).toEqual(new Map([['C-3', ['Datum']], ['M-1', ['Vantage']]]));
    expect(brokenBy(['C-3'], undefined)).toEqual(new Map([['C-3', ['Datum']]]));
  });
});
