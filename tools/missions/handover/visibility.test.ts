// Rule V-1 from each point's own heading (Sky's pick (h) of Vantage's door re-study, 2026-10-18; briefs/011/vantage/door-recheck.json).
// The mission file's own data is Waymark's (contracts/missions/handover/mission.json); these tests turn a copy in memory, so they hold
// before and after the file carries its `yaw` and `hidden` fields.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { Mission } from '../../../contracts/missions/mission.schema';
import { civic } from '../../plan/civic';
import { derive } from '../../plan/plan';
import { generated } from '../../plan/world-ctx';
import { blockersFor, v1, visibility } from './visibility';

setDefaultTimeout(60000);
const root = resolve(import.meta.dir, '../../..'), J = (f: string) => JSON.parse(readFileSync(resolve(root, f), 'utf8'));
const plan = J('contracts/world/harbour-district.plan.json'), block = J(plan.civic.block), file = J('contracts/missions/handover/mission.json');
const d = derive(plan), civ = civic(plan.civic, block), blockers = blockersFor(generated(plan, d, civ).w, civ, plan);

/** The mission with its headings and hiding places stripped (every point read at yaw 30, nothing declared). */
const bare = () => { const m = structuredClone(file); for (const x of [...m.destinations, ...m.crew, ...m.points, ...m.cameras]) { delete x.yaw; delete x.hidden; } return m; };
/** What the Office front must keep: with the public door on the Office's west face (Sky 2026-10-19, option b) these are seen at the opening heading, yaw 30, with no turn declared. */
const FRONT = { names: ['Office entrance', 'Office approach', 'patrol sweep', 'patrol-b', 'patrol-a', 'camera entrance cone'] };
const rows = (m: any) => visibility(m, civ, plan, blockers);

describe('V-1 from each point\'s own heading', () => {
  test('the schema takes yaw (a quarter-step heading) and hidden, and refuses any other heading', () => {
    const m = structuredClone(file);
    m.destinations[0].yaw = 210; m.points[0].hidden = 'x'; m.cameras[0].yaw = 120;
    expect(Mission.safeParse(m).success).toBe(true);
    m.destinations[0].yaw = 45;
    expect(Mission.safeParse(m).success).toBe(false);
  });

  test('points with no heading read at yaw 30: 48 rows (service-bay added 2026-10-19), and the Office front (door option b, west face) is seen without a turn', () => {
    const r = rows(bare()), bad = r.filter(x => x.seen < 0.5).map(x => x.what);
    expect(r.length).toBe(48);
    expect(r.every(x => x.yaw === 30)).toBe(true);
    expect(bad).toContain('Behind café'); // the one hiding place: unwatched on purpose
    for (const n of FRONT.names) expect(r.find(x => x.what === n)?.seen, `${n} at yaw 30`).toBeGreaterThanOrEqual(0.5); // a regression here means the door's front is hidden again
    expect(r.find(x => x.what === 'patrol sweep')!.seen).toBeGreaterThanOrEqual(0.9);
  });

  test('the file as declared: V-1 passes with the one hiding place, Behind café, declared on purpose', () => {
    const r = rows(structuredClone(file)), c = v1(r), cone = r.find(x => x.what === 'camera entrance cone')!;
    expect(r.length).toBe(48);
    expect(r.filter(x => x.seen < 0.5).map(x => x.what)).toEqual(['Behind café']);
    expect(c.ok).toBe(true);
    expect(c.value).toContain('47/48 (+1 hidden by design)');
    expect(cone.seen).toBeGreaterThanOrEqual(0.85);
    expect(file.destinations.filter((x: any) => x.hidden).map((x: any) => x.name)).toEqual(['Behind café']);
    expect(file.points.filter((x: any) => x.hidden)).toEqual([]);
  });

  test('without the declaration the hiding place fails V-1 by name', () => {
    const m = structuredClone(file); for (const x of [...m.destinations, ...m.points]) delete (x as any).hidden;
    const c = v1(rows(m));
    expect(c.ok).toBe(false);
    expect(c.detail).toContain('Behind café');
  });

  test('a patrol sweep whose two ends are played at different headings fails, saying so', () => {
    const m = structuredClone(file); m.points.find((x: any) => x.id === 'patrol-a').yaw = 30; m.points.find((x: any) => x.id === 'patrol-b').yaw = 210;
    const c = v1(rows(m));
    expect(c.ok).toBe(false);
    expect(c.detail).toContain('patrol-a is played at yaw 30 and patrol-b at 210');
  });

  test('a cone counts only its open ground: the footprint share does not pull it down', () => {
    const m = bare(), r = rows(m).filter(x => x.kind === 'cone');
    expect(r.find(x => x.what === 'camera pickup cone')!.seen).toBeGreaterThan(0.9);
    expect(r.find(x => x.what === 'camera entrance cone')!.seen).toBeGreaterThan(0.85);
  });
});
