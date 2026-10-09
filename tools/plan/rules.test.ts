// Each checker rule has a good case (the plan as it stands) and a broken copy that must fail it.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { derive, type Plan } from './plan';
import { civic } from './civic';
import { check } from './checks';
import { inPoly, isClosed, notch, ribbon, type V } from './geom';
import { circusPavement, reservationPoly } from './circus';
import { polyIssue } from './simple';
import { json, load } from './load';
import { overlaps } from './overlap';
import { world } from './world';
import type { P } from '../../contracts/world/world.schema';

setDefaultTimeout(60_000); // each run is a full derive + check (~1.5 s)
const base: Plan = json('contracts/world/harbour-district.plan.json');
/** The plan after a mutation (on a deep copy), checked. Datum's L1 dead-end edits are now in the plan itself. */
function run(mut: (p: any, block: any) => void = () => {}) {
  const p: any = structuredClone(base), manifest = structuredClone(json(base.civic.block));
  mut(p, manifest);
  const plan = p as Plan;
  return check(plan, derive(plan), civic(plan.civic, manifest)).checks;
}
const failing = (cs: ReturnType<typeof run>) => cs.filter(c => !c.ok).map(c => c.id);

test('the plan passes every rule', () => expect(failing(run())).toEqual([]));
test('R-O1: a dead end that passes a plot is fine; a plot across a road body fails', () => {
  expect(failing(run())).not.toContain('R-O1');
  // push the Observatory Lane end 3 m into whatever lies beyond it: the square end now overlaps a plot interior
  expect(failing(run(p => { p.nodes.a_obs = [-100, 99]; }))).toContain('R-O1');
});
test('R-O1: a plot dropped over a street fails', () => {
  expect(failing(run(p => { p.plots[0].rect = [-60, -20, -330, -300]; delete p.plots[0].poly; delete p.plots[0].notch; }))).toContain('R-O1');
});

test('R-D1: a dead end left in open ground fails', () => {
  expect(failing(run(p => { p.nodes.a_obs = [-100, 80]; }))).toContain('R-D1');
});

test('C-4: bay moved off the kerb, into the carriageway, or onto a building fails', () => {
  // block frame: the official bay is x 77..80 (80 = the kerb), y -38.2..-23.8
  const bay = (dx: number) => (_: any, m: any) => { const b = m.bays.find((x: any) => x.id === 'officialBay'); b.polygon = b.polygon.map(([x, y]: number[]) => [x + dx, y]); };
  expect(failing(run(bay(0)))).not.toContain('C-4');
  expect(failing(run(bay(-10)))).toContain('C-4'); // away from the kerb
  expect(failing(run(bay(3)))).toContain('C-4');   // into the carriageway
});
test('C-4: a bay route node outside the bay extent fails', () => {
  expect(failing(run(p => { p.nodes.r_bay_official = [-120, -316]; }))).toContain('C-4');
});
test('C-4: built content on a bay fails', () => {
  expect(failing(run((_, m) => { const o = m.objects.find((x: any) => x.kind === 'planter'); o.polygon = [[77.5, -31], [79.5, -31], [79.5, -29], [77.5, -29]]; }))).toContain('C-4');
});
test('C-4: the Oct 9 pavilion supports left in the bay (no adjustment) fail', () => {
  expect(failing(run((_, m) => { const s = m.structures.find((x: any) => x.id === 'boarding-pavilion-support-0'); s.polygon = [[77.3, -37.75], [79.3, -37.75], [79.3, -35.75], [77.3, -35.75]]; }))).toContain('C-4');
});

test('notch: a lay-by on a block edge is cut out; one off the edge is left alone; a bite throws', () => {
  const sq: [number, number][] = [[0, 0], [10, 0], [10, 10], [0, 10]];
  expect(notch(sq, [[2, 0], [4, 0], [4, -2], [2, -2]])).toEqual(sq);
  expect(notch(sq, [[2, 0], [4, 0], [4, 2], [2, 2]])).toEqual([[0, 0], [2, 0], [2, 2], [4, 2], [4, 0], [10, 0], [10, 10], [0, 10]]);
  expect(() => notch(sq, [[2, 5], [4, 5], [4, 7], [2, 7]])).toThrow();
});

const heights = (p: any) => { for (const x of p.plots) if (!x.civic && !['park', 'square', 'open'].includes(x.use)) x.height = 15; };
test('R-B1: every built plot but the Handover plots needs a height > 0', () => {
  expect(failing(run(heights))).not.toContain('R-B1');
  expect(failing(run(p => { heights(p); p.plots.find((x: any) => x.id === 'rocket-west').height = 0; }))).toContain('R-B1');
  expect(failing(run(p => { heights(p); delete p.plots.find((x: any) => x.id === 'rocket-west').height; }))).toContain('R-B1');
});

test('circus pavement: simple pieces that leave every road entry open and never overlap a road', () => {
  const plan: Plan = structuredClone(base), d = derive(plan), pieces = circusPavement(plan, d, .8);
  expect(pieces.length).toBe(3);
  for (const p of pieces) expect(polyIssue(p)).toBeNull();
  const arms = d.roads.filter(r => !isClosed(r.line) && r.line.some(q => Math.hypot(...q) < 43));
  expect(arms.length).toBe(3);
  for (const r of arms) for (const q of r.line.filter(q => Math.hypot(...q) > 43 && Math.hypot(...q) < 51)) expect(pieces.some(p => inPoly(q, p))).toBe(false);
  expect(pieces.some(p => inPoly([47, 0], p))).toBe(true); // no road leaves the east
});

test('V-1: rocket-west raised to 15 m hides the Getaway bay; the plan as it stands does not', () => {
  const v1 = (cs: ReturnType<typeof run>) => cs.find(c => c.id === 'V-1')!;
  expect(v1(run()).ok).toBe(true);
  const broken = v1(run(p => { p.plots.find((x: any) => x.id === 'rocket-west').height = 15; }));
  expect(broken.ok).toBe(false);
  expect(broken.detail).toContain('Getaway bay behind rocket-west');
});

test('reservation: the tram strip ends on the circus outer carriageway edge, on the circle, simple', () => {
  const plan: Plan = structuredClone(base), d = derive(plan), r = d.roads.find(x => x.id === 'boulevard')!, poly = reservationPoly(plan, r);
  expect(polyIssue(poly)).toBeNull();
  expect(Math.min(...poly.map(q => Math.hypot(...q)))).toBeCloseTo(43, 3);
  expect(poly.some(q => Math.abs(Math.hypot(...q) - 43) < 1e-6 && Math.abs(q[0]) > 4.9)).toBe(true); // strip corners on the edge, not at y -37
  expect(Math.max(...poly.map(q => q[1]))).toBeLessThan(-42.7 + 1e-3);
  const open = d.roads.find(x => x.reservation && !r.line.length)?.id; // a road that never reaches the ring keeps the plain ribbon
  expect(open).toBeUndefined();
});

test('export: same-level raised surfaces do not overlap; the mall ends on the circus pavement edge, and a plain ribbon fails', () => {
  const l = load('contracts/world/harbour-district.plan.json');
  const w = world(l.plan, l.d, l.civ, l.R, l.planHash);
  expect(overlaps(w)).toEqual([]);
  const mall = w.foot.find(f => f.id === 'crown-parade')!;
  expect(Math.min(...mall.polygon.map(q => Math.hypot(...q)))).toBeGreaterThan(51 - 1e-3); // clipped to the pavement circle
  const broken = structuredClone(w);
  broken.foot.find(f => f.id === 'crown-parade')!.polygon = ribbon(mall.centreline as V[], 20) as P[]; // the unclipped ribbon
  const bad = overlaps(broken);
  expect(bad.length).toBe(1);
  expect(bad[0]).toMatch(/^foot\[crown-parade\] overlaps circus\.pavement\[\d\] by \d+\.\d\d m² at level 0\.18$/);
});
