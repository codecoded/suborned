// The continuous footway at a gate's lane mouth (brief 061, Sky 2026-10-25; plan standards.streetscape.continuousFootway; rule SF-24). The mouth model is checked against the kerb returns it replaces (while they stand) and against the plan's numbers;
// the export's footways, the table flats and SF-24's failing examples run once gen/kerbreturn.ts leaves a footway corner square (it asks `isFootwayCorner`; briefs/061/splice/kerbreturn-footway.patch): until then the world keeps its returns, lays
// no footway, and SF-24 says so as a stat, never silently. `applied` is that switch; the tests under it skip while it is off.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { World } from '../../../../contracts/world/world.schema';
import { civic } from '../../../plan/civic';
import { type V, dist, inPoly } from '../../../plan/geom';
import { derive, type Plan } from '../../../plan/plan';
import { ctxOf, generated } from '../../../plan/world-ctx';
import { extraRules } from '../../rules';
import { ctxOf as genCtx } from '../common';
import { returnsOf } from '../kerbreturn';
import { activeMouths, blockRing, footwayOf, footwayStd, footwayTangentAt, footwayTangentPoints, footwaysOf, isFootwayCorner, mouthsOf, slopeAt } from './footway';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block)), gc = genCtx(plan, d, civ, 0.18);
const std = footwayStd(gc.S)!, mouths = mouthsOf(gc), applied = activeMouths(gc).length === mouths.length;
const base = generated(plan, d, civ);
const A = Math.abs;

describe('the mouth of each gate, from the plan alone', () => {
  test('14 mouths at 8 nodes: Market Street 8, Circus Road 3, Crown Avenue 3; one per gate, the lane\'s end on a road of a higher class', () => {
    expect(mouths).toHaveLength(14);
    expect(new Set(mouths.map(m => m.node)).size).toBe(8);
    expect(Object.fromEntries(['market-street', 'circus-road', 'crown-avenue'].map(r => [r, mouths.filter(m => m.road === r).length]))).toEqual({ 'market-street': 8, 'circus-road': 3, 'crown-avenue': 3 });
    expect(mouths.map(m => m.gate).sort()).toEqual((plan.gates as { id: string }[]).map(g => g.id).sort());
    for (const m of mouths) { expect(d.roads.find(r => r.id === m.lane)!.class).toBe('access'); expect(d.roads.find(r => r.id === m.road)!.class).toBe('district'); }
  });
  test('the frame: the point on the higher road\'s near kerb line on the lane\'s axis, the kerb line\'s direction, the unit vector into the lane, the lane 12 m wide, the corners 12 m apart on the kerb line', () => {
    for (const m of mouths) {
      const hw = d.roads.find(r => r.id === m.road)!.w / 2, node = plan.nodes[m.node] as V;
      expect(dist(m.p, node), m.id).toBeCloseTo(hw, 6); // the near kerb line is half the road's width off its axis, on the lane's side
      expect(A(m.along[0] * m.into[0] + m.along[1] * m.into[1]), m.id).toBeLessThan(1e-9); // square
      expect(m.half, m.id).toBe(6); expect(dist(m.corners[0], m.corners[1]), m.id).toBeCloseTo(12, 6);
      expect(m.tangent, m.id).toBe(6); // kerbReturn.byClass.access: the table\'s depth, and what a crossing beside it keeps its clear from
    }
    const rocket = mouths.find(m => m.gate === 'rocket-gate')!;
    expect(rocket.corners.map(c => c.map(v => Math.round(v * 1000) / 1000))).toEqual([[-192, -401], [-180, -401]]); // Market Street y -410, 9 m half width; Rocket Lane x -186, 12 m wide
    expect(rocket.into).toEqual([0, 1]);
  });
  test('while the returns stand they are the model: each mouth corner carries a convex return of 6 m whose tangent points are 6 m along the kerb line and 6 m up the lane', () => {
    if (applied) return; // the returns are gone: the footway is what stands there
    const ret = returnsOf(gc).filter(r => r.convex && !r.nose && !r.bay && !r.mouth);
    for (const m of mouths) for (const [k, c] of m.corners.entries()) {
      const r = ret.find(x => dist(x.at, c) < 0.3 && x.roads.includes(m.road))!;
      expect(r, `${m.id} corner ${k}`).toBeDefined();
      expect(r.t).toBeCloseTo(m.tangent, 6); expect(r.civic).toBe(false);
      const [onKerb, onLane] = footwayTangentPoints(gc, [m.road, m.lane], c), ends = [r.a, r.b];
      expect(ends.some(p => dist(p, onKerb!) < 1e-6) && ends.some(p => dist(p, onLane!) < 1e-6), `${m.id} corner ${k}`).toBe(true);
    }
  });
  test('the pieces are the plan\'s: a table 12 x 6 at 0.18, a ramp 2.4 m on at 1 in 13, a lip 0.38 m falling 0.18, two bands 0.16 m wide, two posts 0.6 m outside the lane\'s kerb faces at the table\'s back', () => {
    expect(std).toEqual({ table: { z: 0.18 }, ramp: { run: 2.4, rise: 0.18 }, lip: { d: 0.38, drop: 0.18 }, bands: { count: 2, w: 0.16 }, posts: { off: 0.6 } });
    const m = mouths.find(x => x.gate === 'rocket-gate')!, f = footwayOf(m, std, 'residential', 'kerb');
    expect(f.level).toBe(0.18); expect(f.depth).toBe(6); expect(f.half).toBe(6);
    expect(f.table.ring).toEqual([[-192, -401], [-180, -401], [-180, -395], [-192, -395]]);
    expect(f.ramp.ring).toEqual([[-192, -395], [-180, -395], [-180, -392.6], [-192, -392.6]]); expect(f.ramp.run / f.ramp.rise).toBeCloseTo(13.33, 1);
    expect(f.lip.ring).toEqual([[-192, -401], [-180, -401], [-180, -400.62], [-192, -400.62]]);
    expect(f.bands.map(b => [b.level, b.ring[0]![1], b.ring[2]![1]])).toEqual([[0, -392.6, -392.44], [0.18, -400.62, -400.46]]);
    expect(f.posts).toEqual([[-192.6, -395], [-179.4, -395]]);
    expect(A(blockRing(m, std)[2]![1] - blockRing(m, std)[0]![1] - (6 + 2.4 + 0.16))).toBeLessThan(1e-6); // the lane\'s road finish stops short of the foot band
  });
});

describe('the corners of a footway mouth are square, and everything measured from the return\'s tangent point keeps its distance', () => {
  test('isFootwayCorner is true at the 28 corners of the 14 mouths (by place and the higher road), false at any other corner; the tangent length is the return\'s', () => {
    let n = 0;
    for (const m of mouths) for (const c of m.corners) { expect(isFootwayCorner(gc, [m.road, m.lane], c), m.id).toBe(true); expect(footwayTangentAt(gc, [m.road, m.lane], c)).toBe(6); n++; }
    expect(n).toBe(28);
    expect(isFootwayCorner(gc, ['market-street', 'rocket-lane'], [-186, -401])).toBe(false); // the middle of the mouth is no corner
    expect(isFootwayCorner(gc, ['boulevard', 'market-street'], [-5, -401])).toBe(false);
    expect(footwayTangentAt(gc, ['boulevard', 'market-street'], [-14, -419])).toBe(0);
  });
  test('the crossroads crossing beside a mouth stands 3 m past where the return\'s tangent point was, whether the return stands or the table does', () => {
    const rocket = mouths.find(m => m.gate === 'lantern-west')!; // the lane south of Market Street at the Rocket node; the crossroads crossing stands on the arm before the node
    const x = base.w.crossings!.find(c => c.road === 'market-street' && A((c.line[0][0] + c.line[1][0]) / 2 - (rocket.p[0] - 17)) < 1.5)!;
    expect(x).toBeDefined();
    for (const l of x.landings) { const near = Math.min(...rocket.corners.map(c => dist(c, l.at))); expect(near).toBeGreaterThan(3 + 6); } // 6 m of tangent and 3 m of clear before the 4 m landing begins (its centre is 2 m on)
  });
});

describe.skipIf(!applied)('the footways in the export (gen/kerbreturn.ts leaves the corners square)', () => {
  const w = base.w as World, fw = w.ground.footways!;
  test('14 footways, each with its table flat at the pavement top, the plan\'s ramp, lip, bands and posts; no kerb return at a corner; SF-24 passes', () => {
    expect(fw).toHaveLength(14);
    for (const f of fw) {
      const flat = w.ground.flats.find(x => x.id === f.table.id)!;
      expect(flat.level).toBe(0.18); expect(flat.ring).toEqual(f.table.ring);
      expect(f.posts).toHaveLength(2); for (const [i, id] of [`street/${f.id}/post-a`, `street/${f.id}/post-b`].entries()) expect(dist(w.street.find(s => s.id === id)!.at as V, f.posts[i]!)).toBeLessThan(0.002);
      expect(f.ramp.surface).toBe('road-enamel'); expect(f.lip.surface).toBe('kerb'); expect(new Set(w.ground.plots.map(q => q.surface)).has(f.table.surface), f.id).toBe(true); // the paving of the plot beside the mouth
    }
    expect(returnsOf(gc).filter(r => r.convex && mouths.some(m => m.corners.some(c => dist(c, r.at) < 0.3)))).toEqual([]);
    const f24 = extraRules(ctxOf(plan, d, civ, { w, gen: base.gen })).filter(c => c.id.startsWith('SF-24'));
    expect(f24.map(c => [c.id, c.ok, c.detail ?? ''])).toEqual([['SF-24', true, ''], ['SF-24/n', true, 'every gate\'s mouth is a footway']]);
  });
  const rules = (mutate: (x: World) => void) => { const c = structuredClone(w) as World; mutate(c); return extraRules(ctxOf(plan, d, civ, { w: c, gen: base.gen })).find(k => k.id === 'SF-24')!; };
  test('SF-24 fails on a footway that is missing, the wrong depth, a ramp too steep, a table at the carriageway, a kerb stone across it, a crossing in the mouth, furniture on the table, a missing post', () => {
    const f0 = fw[0]!;
    expect(rules(x => { x.ground.footways = x.ground.footways!.slice(1); }).detail).toMatch(/has no footway, and no kerb return stands at its corners/);
    expect(rules(x => { x.ground.footways![0]!.table.ring = x.ground.footways![0]!.table.ring.map(p => [p[0], p[1] + 1] as V); }).detail).toMatch(/its table ring is not the mouth's/);
    expect(rules(x => { x.ground.footways![0]!.ramp.rise = 0.4; }).detail).toMatch(/its ramp rises 0\.4 m over 2\.4 m: 1 in 6\.0, the plan's is 1 in 13/);
    expect(rules(x => { x.ground.footways![0]!.level = 0; }).detail).toMatch(/its table is at 0, the plan's is 0\.18/);
    expect(rules(x => { x.ground.flats = x.ground.flats.filter(q => q.id !== f0.table.id); }).detail).toMatch(/its table is not a flat in ground.flats/);
    expect(rules(x => { (x.ground.pieces ??= []).push({ id: 'test/kerb', kind: 'kerb', at: [(f0.table.ring[0]![0] + f0.table.ring[2]![0]) / 2, (f0.table.ring[0]![1] + f0.table.ring[2]![1]) / 2], yaw: 0, z: 0.18, size: [3, 0.3, 0.1], surface: 'kerb', edge: 'x', sight: 'open' }); }).detail).toMatch(/test\/kerb \(kerb\) lies across the table/);
    expect(rules(x => { x.crossings![0]!.strip = f0.table.ring; }).detail).toMatch(/'s strip lies in the mouth/);
    expect(rules(x => { x.street.push({ id: 'test/bench', kind: 'bench', at: [(f0.table.ring[0]![0] + f0.table.ring[2]![0]) / 2, (f0.table.ring[0]![1] + f0.table.ring[2]![1]) / 2], yaw: 0, z: 0.18, size: [1.8, 0.6, 0.9], surface: 'trim', edge: 'x', sight: 'open' }); }).detail).toMatch(/test\/bench \(bench\) stands on the table/);
    expect(rules(x => { x.street = x.street.filter(s => s.id !== `street/${f0.id}/post-a`); }).detail).toMatch(/its post street\/footway\/.*\/post-a is missing/);
  });
  test('the higher road\'s ribbon runs straight on across each mouth, the lane\'s stops short of the foot band', () => {
    for (const f of fw) {
      const face = w.ground.pieces!.filter(s => s.kind === 'road-ribbon' && s.edge === `roadfinish/${f.id}/kerb`);
      expect(face.reduce((t, s) => t + s.size[0], 0), f.id).toBeGreaterThan(11.7);
    }
  });
  test('the ramp and the lip are slopes between the two levels (the geometry audit reads them with the table flat)', () => {
    const f = fw[0]!, at = (a: number, depth: number): V => [f.frame.origin[0] + f.frame.along[0] * a + f.frame.into[0] * depth, f.frame.origin[1] + f.frame.along[1] * a + f.frame.into[1] * depth];
    expect(slopeAt(f, at(0, f.depth))).toBeCloseTo(0.18, 9); expect(slopeAt(f, at(0, f.depth + f.ramp.run))).toBeCloseTo(0, 9); expect(slopeAt(f, at(0, f.depth + f.ramp.run / 2))).toBeCloseTo(0.09, 9);
    expect(slopeAt(f, at(0, 0))).toBeCloseTo(0, 9); expect(slopeAt(f, at(0, f.lip.d))).toBeCloseTo(0.18, 9); expect(slopeAt(f, at(0, f.lip.d / 2))).toBeCloseTo(0.09, 9);
    expect(slopeAt(f, at(0, 3))).toBeUndefined(); // the table is a flat
    expect(slopeAt(f, at(f.half + 1, f.depth + 1))).toBeUndefined(); // beside the lane
  });
});

describe.skipIf(applied)('before the hook: the world keeps its kerb returns and lays no footway, and says so', () => {
  test('no ground.footways, no lane post, SF-24 passes with a stat that counts 0 of 14 and names the file that must leave the corners square', () => {
    expect(base.w.ground.footways).toBeUndefined();
    expect(base.w.street.filter(s => s.id.startsWith('street/footway/'))).toEqual([]);
    expect(footwaysOf(gc, { road: () => 'x', kerb: 'kerb' })).toEqual([]);
    const k = extraRules(ctxOf(plan, d, civ, { w: base.w, gen: base.gen })).filter(c => c.id.startsWith('SF-24'));
    expect(k[0]!.ok, k[0]!.detail).toBe(true);
    expect(k[1]).toMatchObject({ id: 'SF-24/n', ok: true, value: '0 of 14' });
    expect(k[1]!.detail).toMatch(/14 mouth\(s\) keep their kerb returns: .*isFootwayCorner/);
  });
});
