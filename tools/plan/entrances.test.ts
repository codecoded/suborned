// Failing examples for W-2 (stops reachable on foot) and W-3 (fronts name a road or foot path the entrance touches).
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { resolve } from 'node:path';
import { check } from './checks';
import { civic } from './civic';
import { lineDist, type V } from './geom';
import { derive, type Plan } from './plan';

setDefaultTimeout(30000);
const root = resolve(import.meta.dir, '../..'), read = async (p: string) => JSON.parse(await Bun.file(resolve(root, p)).text());
const base: Plan = await read('contracts/world/harbour-district.plan.json');
const civ = civic(base.civic, await read(base.civic.block));
const run = (mutate: (p: any) => void = () => {}) => {
  const p = structuredClone(base) as any; mutate(p);
  const by = (id: string) => check(p, derive(p), civ, true).checks.filter(c => c.id === id);
  return { by, fails: (id: string) => by(id).some(c => !c.ok), passes: (id: string) => by(id).length > 0 && by(id).every(c => c.ok) };
};
const swim = (p: any) => { p.skyrail[0].stations[3].at = [650, -300]; }; // open water, nothing built there
/** Every stop names the road or foot path whose edge is nearest to it. */
const fronted = (p: any) => {
  const g = [...derive(p).roads.map(r => ({ id: r.id, line: r.line, w: r.w })), ...p.foot.map((f: any) => ({ id: f.id, line: f.pts as V[], w: f.w }))];
  for (const l of [...p.tram, ...p.skyrail]) for (const s of l.stops ?? l.stations) s.fronts = g.reduce((b, x) => (lineDist(s.at, x.line) - x.w / 2 < lineDist(s.at, b.line) - b.w / 2 ? x : b)).id;
};

describe('entrance rules', () => {
  test('W-2 fails for a sky rail station over water, naming it', () => {
    const r = run(swim);
    expect(r.fails('W-2')).toBe(true); expect(r.by('W-2')[0]!.detail).toContain('S1/skyport');
  });
  test('W-2 passes on the plan itself (stations reached over the apron), and fails on the plan as v0.1.0 first drew it', () => {
    expect(run().passes('W-2')).toBe(true);
    expect(run(p => { p.foot = p.foot.filter((f: any) => f.id !== 'skyport-pier'); p.water = [[[330, -780], [740, -780], [740, 320], [330, 320]]]; }).fails('W-2')).toBe(true);
  });
  test('W-2 fails for a tram stop walled in by a plot', () => {
    const r = run(p => { const plot = p.plots.find((x: any) => x.rect && !['park', 'open', 'square', 'civic'].includes(x.use)); const [x0, x1, y0, y1] = plot.rect; p.tram[0].stops[0].at = [(x0 + x1) / 2, (y0 + y1) / 2]; });
    expect(r.fails('W-2')).toBe(true);
  });
  test('W-2 and W-3 are waivable only outside strict (like the other transit rules)', () => {
    const p = structuredClone(base) as any; p.standards.transit.waived['W-2'] = 'draft'; p.skyrail[0].stations[3].at = [650, -300];
    expect(check(p, derive(p), civ).checks.find(c => c.id === 'W-2')!.ok).toBe(true);
    expect(check(p, derive(p), civ, true).checks.find(c => c.id === 'W-2')!.ok).toBe(false);
  });
  test('W-3 passes with no fronts at all, and with every stop fronting a road it touches', () => {
    expect(run().passes('W-3')).toBe(true);
    expect(run(fronted).passes('W-3')).toBe(true);
  });
  test('W-3 fails for a fronts id that does not exist, for a missing one once others name theirs, and for a road it does not touch', () => {
    const unknown = run(p => { fronted(p); p.skyrail[0].stations[0].fronts = 'nope'; });
    expect(unknown.fails('W-3')).toBe(true); expect(unknown.by('W-3')[0]!.detail).toContain('unknown road or foot path nope');
    const missing = run(p => { fronted(p); delete p.tram[0].stops[0].fronts; });
    expect(missing.fails('W-3')).toBe(true); expect(missing.by('W-3')[0]!.detail).toContain('names no fronts');
    const far = run(p => { fronted(p); p.skyrail[0].stations[3].fronts = 'garden-walk'; });
    expect(far.fails('W-3')).toBe(true); expect(far.by('W-3')[0]!.detail).toContain('S1/skyport');
  });
});
