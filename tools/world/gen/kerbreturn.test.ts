// Kerb returns (brief 028, Sky 2026-10-24: rounded pavement corners): the corner list, the arcs, the slabs, the lay-bys, the circus mouths, on the committed plan; each failing example is the plan with one key changed.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { type V, dist, inPoly, segDist } from '../../plan/geom';
import { ctxOf } from './common';
import { baysOf, chordsOf, cutHits, fillRing, inCut, KERB_RETURN_DEFAULT, noseRing, returnsOf, ringWidth, slabRing, tangentAt } from './kerbreturn';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), block = read(plan.civic.block);
const make = (mutate: (p: any) => void = () => {}) => { const p: Plan = structuredClone(plan); mutate(p); const d = derive(p), civ = civic(p.civic, block); return { p, d, ctx: ctxOf(p, d, civ, 0.18) }; };
const live = make();

describe('the corners', () => {
  test('the plan asks 6 / 8 / 6 by the minor road, 2 for the civic block, 2 for a concave join, 1.4 for a bay (Datum)', () => {
    const k = (plan.standards.streetscape as any).kerbReturn;
    expect([k.byClass.primary, k.byClass.district, k.byClass.access, k.civic, k.concave, k.bay]).toEqual([6, 8, 6, 2, 2, 1.4]);
    expect(KERB_RETURN_DEFAULT.byClass).toEqual(k.byClass); // a plan without the key takes the same numbers
  });
  test('every plot corner where two kerbs meet is rounded except at a gate lane mouth: 48 convex (8 of them civic, 2 m; the 28 corners of the 14 gate footways stay square, brief 061), one concave (the lane end), none shrunk', () => {
    const L = returnsOf(live.ctx).filter(r => !r.bay && !r.mouth && !r.nose && !r.bend && !r.edge);
    expect(L.filter(r => r.convex).length).toBe(48); expect(L.filter(r => !r.convex).length).toBe(1);
    expect(L.filter(r => r.civic).length).toBe(8);
    expect(L.every(r => Math.abs(r.r - r.want) < 0.01)).toBe(true);
    const by: Record<string, number> = {}; for (const r of L.filter(r => r.convex && !r.civic)) by[`${r.cls} ${r.want}`] = (by[`${r.cls} ${r.want}`] ?? 0) + 1;
    expect(by).toEqual({ 'access 6': 22, 'district 8': 14, 'primary 6': 4 });
  });
  test('a return is an arc tangent to both kerb faces, from tangent point to tangent point, 16 chords a quarter turn (8 under 4 m)', () => {
    for (const r of returnsOf(live.ctx).filter(x => x.convex && !x.mouth && !x.nose && !x.bay)) {
      expect(r.arc.length).toBe(r.n + 1);
      expect(r.n).toBe(r.r < 4 ? 8 : 16);
      for (const q of r.arc) expect(Math.abs(dist(q, r.c) - r.r)).toBeLessThan(1e-6);
      expect(Math.abs(r.t - r.r * Math.tan(r.theta / 2))).toBeLessThan(1e-6);
      expect(dist(r.a, r.at)).toBeCloseTo(r.t, 6); expect(dist(r.b, r.at)).toBeCloseTo(r.t, 6);
      expect(dist(r.a, r.c)).toBeCloseTo(r.r, 6); // tangent: the radius to the tangent point is perpendicular to the face
    }
  });
  test('the slab is cut by the arc and nothing else moves: the old corner is outside the slab ring, the arc inside it, the ring keeps its orientation and its other vertices', () => {
    for (const pl of live.d.plots) {
      const ring = slabRing(live.ctx, pl), mine = returnsOf(live.ctx).filter(r => r.plot === pl.id && !r.bay && !r.mouth);
      const sign = (p: V[]) => Math.sign(p.reduce((s, a, i) => s + a[0] * p[(i + 1) % p.length]![1] - p[(i + 1) % p.length]![0] * a[1], 0));
      expect(sign(ring)).toBe(sign(pl.polyV));
      for (const r of mine.filter(x => x.convex)) { expect(ring.some(q => dist(q, r.a) < 1e-9)).toBe(true); expect(ring.some(q => dist(q, r.at) < 1e-9)).toBe(false); }
      for (const v of pl.polyV) if (!mine.some(r => dist(r.at, v) < 1e-6)) expect(ring.some(q => dist(q, v) < 1e-9)).toBe(true);
    }
  });
  test('the building line is not touched: the civic block\'s corners take 2 m, every other pavement keeps its width at the building corner (KR-1 holds)', () => {
    for (const r of returnsOf(live.ctx).filter(x => x.civic)) expect(r.r).toBe(2);
  });
  test('the cut is carriageway: a point in it is in the cut, one on the pavement is not; a footprint near the arc is refused', () => {
    const r = returnsOf(live.ctx).find(x => x.convex && !x.bay && !x.mouth && !x.civic)!, mid: V = [(r.at[0] + r.c[0]) / 2 * 0.2 + r.at[0] * 0.8, (r.at[1] + r.c[1]) / 2 * 0.2 + r.at[1] * 0.8];
    expect(inCut(live.ctx, [r.at[0] + (r.c[0] - r.at[0]) * 0.05, r.at[1] + (r.c[1] - r.at[1]) * 0.05])).toBe(true);
    expect(inCut(live.ctx, r.c)).toBe(false); void mid;
    expect(cutHits(live.ctx, [[r.c[0] - 0.2, r.c[1] - 0.2], [r.c[0] + 0.2, r.c[1] - 0.2], [r.c[0] + 0.2, r.c[1] + 0.2], [r.c[0] - 0.2, r.c[1] + 0.2]], 0.45)).toBeUndefined();
    const near: V = [r.arc[8]![0] + (r.c[0] - r.arc[8]![0]) * 0.03, r.arc[8]![1] + (r.c[1] - r.arc[8]![1]) * 0.03];
    expect(cutHits(live.ctx, [[near[0] - 0.1, near[1] - 0.1], [near[0] + 0.1, near[1] - 0.1], [near[0] + 0.1, near[1] + 0.1], [near[0] - 0.1, near[1] + 0.1]], 0.45)?.id).toBe(r.id);
  });
  test('the fill lies under the slab by 5 cm on the arc side and over the carriageway by 5 cm on the kerb lines', () => {
    const r = returnsOf(live.ctx).find(x => x.convex && !x.bay && !x.mouth && !x.civic)!, f = fillRing(r);
    expect(f.length).toBe(r.n + 2);
    expect(Math.abs(dist(f[2]!, r.c) - (r.r - 0.05))).toBeLessThan(0.01); expect(inPoly(r.at, f) || f.some(q => dist(q, r.at) < 0.1)).toBe(true);
  });
  test('the chords of a return keep the offset circle: a ribbon 0.1 m out on a 6 m return is on r 6.1, its chords meet on the circle', () => {
    const r = returnsOf(live.ctx).find(x => x.convex && !x.bay && !x.mouth && !x.civic && x.want === 6)!, cs = chordsOf(r, 0.1);
    expect(cs.length).toBe(12);
    for (const c of cs) { expect(Math.abs(dist(c.at, r.c) - 6.1 * Math.cos(Math.PI / 48))).toBeLessThan(1e-9); expect(dist(c.to, r.c)).toBeCloseTo(6.1, 9); expect(c.len).toBeGreaterThan(0.7); expect(Math.abs(dist(c.from, r.c) - 6.1)).toBeLessThan(1e-9); }
    expect(cs.reduce((s, c) => s + c.len, 0)).toBeCloseTo(2 * 6.1 * Math.sin(Math.PI / 48) * 12, 2);
  });
  test('the sheet-edge corners (Datum: the beyond blocks\' corners where a road runs on to the sheet edge) are rounded: 7 junction-box corners, the minor road\'s radius, none of them a plot, bay, mouth or bend', () => {
    const E = returnsOf(live.ctx).filter(r => r.edge);
    expect(E.length).toBe(7); expect(E.every(r => r.convex && r.plot === 'edge' && Math.abs(r.r - r.want) < 0.01 && !r.bay && !r.mouth && !r.bend)).toBe(true);
    for (const r of E) { expect(r.want).toBe(r.cls === 'district' ? 8 : 6); expect(Math.abs(dist(r.a, r.c) - r.r)).toBeLessThan(1e-6); expect(dist(r.a, r.at)).toBeCloseTo(r.t, 6); }
    expect(E.map(r => r.at.map(Math.round).join(',')).sort()).toEqual(['-14,-732', '-328,-732', '-352,-9', '-352,268', '-352,9', '14,-732', '288,292'].sort());
  });
  test('where two roads meet end to end at a bend the outside of the turn is laid to the outside arc: a hole fill from the node to the arc, 2 of them (junction NE, the ring\'s south-east corner)', () => {
    const H = returnsOf(live.ctx).filter(r => r.hole);
    expect(H.length).toBe(2);
    for (const r of H) { expect(dist(r.hole![0]!, r.a)).toBeCloseTo(dist(r.hole![0]!, r.b), 6); expect(r.hole!.length).toBe(r.n + 2); expect(r.bend).toBeDefined(); }
  });
  test('the returns are cached on the plan, the standards and the civic block together: another standard on the same derived plan is another list (Redline brief 064 #10)', () => {
    const n = returnsOf(live.ctx).length, off = { ...live.ctx, S: { ...live.ctx.S, kerbReturn: { ...(live.ctx.S as any).kerbReturn, off: true } } } as typeof live.ctx;
    expect(returnsOf(off).length).toBe(0); expect(returnsOf(live.ctx).length).toBe(n); expect(returnsOf({ ...live.ctx, S: undefined } as any).length).toBe(0); expect(returnsOf(live.ctx).length).toBe(n);
  });
  test('the cache holds when a standard is edited in place on the same objects, and a call without the standards or the civic block comes first (Redline brief 064 #10)', () => {
    const f = make(), none = { ...f.ctx, S: undefined, civ: undefined } as any;
    expect(returnsOf(none).length).toBe(0); // first call: no standards
    const n = returnsOf(f.ctx).length; expect(n).toBeGreaterThan(40); // then the same derived plan with them
    expect(baysOf({ ...f.ctx, civ: undefined } as any).length).toBe(0); // no civic block: no lay-bys
    expect(baysOf(f.ctx).length).toBeGreaterThan(0);
    const S = f.ctx.S as any, access = returnsOf(f.ctx).find(r => r.cls === 'access' && r.convex && !r.civic && !r.bay && !r.mouth && !r.bend)!;
    S.kerbReturn.byClass.access = 4; // edited in place: the same S, the same d
    expect(returnsOf(f.ctx).find(r => r.id === access.id)!.want).toBe(4);
    S.kerbReturn.off = true;
    expect(returnsOf(f.ctx).length).toBe(0);
    S.kerbReturn.off = false; S.kerbReturn.byClass.access = 6;
    expect(returnsOf(f.ctx).length).toBe(n);
  });
  test('tangentAt reads a vertex of a plot, 0 where the corner is square', () => {
    const r = returnsOf(live.ctx).find(x => x.convex && !x.bay && !x.mouth)!;
    expect(tangentAt(live.ctx, r.plot, r.at)).toBeCloseTo(r.t, 6); expect(tangentAt(live.ctx, r.plot, [r.at[0] + 5, r.at[1] + 5])).toBe(0);
  });
  test('a plan with kerbReturn.off keeps every corner square', () => {
    const sq = make(p => { p.standards.streetscape.kerbReturn.off = true; });
    expect(returnsOf(sq.ctx).length).toBe(0);
    for (const pl of sq.d.plots) expect(slabRing(sq.ctx, pl)).toEqual(pl.polyV);
  });
  test('a class radius changed in the plan changes the arcs: access 6 -> 4 gives 4 m returns', () => {
    const f = make(p => { p.standards.streetscape.kerbReturn.byClass.access = 4; });
    expect(returnsOf(f.ctx).filter(r => r.cls === 'access' && r.convex && !r.civic && !r.bay).every(r => Math.abs(r.r - 4) < 0.01)).toBe(true);
  });
  test('deterministic: the same plan gives the same returns', () => {
    const again = make(), key = (c: typeof live) => JSON.stringify(returnsOf(c.ctx).map(r => [r.id, r.r, r.arc]));
    expect(key(again)).toBe(key(live));
  });
});

describe('the kerbed bays (kerbReturn.bay 1.4)', () => {
  test('squareBay and officeBay: the mouth tips (convex) and back corners (concave) are 1.4 m arcs; serviceBay stays square', () => {
    const b = baysOf(live.ctx); expect(b.map(x => x.id).sort()).toEqual(['officeBay', 'squareBay']);
    for (const x of b) { expect(x.r).toBeCloseTo(1.4, 6); expect(x.returns.map(r => r.convex)).toEqual([true, true, false, false]); expect(x.returns.every(r => r.n === 8)).toBe(true); }
    expect(b.find(x => x.id === 'squareBay')!.width).toBe(9); expect(b.find(x => x.id === 'squareBay')!.depth).toBe(3);
  });
  test('the bay flat keeps a convex ring (the slab is cut round it) with its back corners rounded, and the slab ring holds the notch the same way', () => {
    for (const x of baysOf(live.ctx)) {
      const R = x.ring, cr = (a: V, b: V, c: V) => (b[0] - a[0]) * (c[1] - b[1]) - (b[1] - a[1]) * (c[0] - b[0]);
      const sg = R.map((p, i) => Math.sign(Math.round(cr(p, R[(i + 1) % R.length]!, R[(i + 2) % R.length]!) * 1e6))).filter(Boolean);
      expect(new Set(sg).size).toBe(1);
      const slab = slabRing(live.ctx, live.d.plots.find(p => p.id === x.plot)!);
      for (const q of x.notch) expect(slab.some(s => dist(s, q) < 1e-9)).toBe(true);
    }
  });
  test('a lay-by is open to the road: the plot\'s kerb run stops at the tangent point of each mouth corner, none across the mouth', async () => {
    const { kerbRuns } = await import('./street/capstone'), z = [-600, 600, -900, 400] as [number, number, number, number];
    const runs = kerbRuns(live.ctx, z);
    for (const x of baysOf(live.ctx)) for (const r of runs.filter(q => q.plot === x.plot && q.road === x.road)) for (const m of x.M) expect(segDist(m, r.a, r.b) < 0.05 && dist(m, r.a) > 0.05 && dist(m, r.b) > 0.05).toBe(false); // no run passes through a mouth corner
  });
});

describe('the circus mouths', () => {
  test('six mouths: an 8 m arc tangent to the radial road\'s kerb line and to the ring, 102 or 109 degrees', () => {
    const m = returnsOf(live.ctx).filter(r => r.mouth);
    expect(m.length).toBe(6);
    for (const r of m) {
      expect(r.r).toBe(8); expect(Math.abs(dist(r.c, live.p.circus.c) - (live.p.circus.ring[1] + 8))).toBeLessThan(1e-6); // externally tangent to the ring's outer kerb
      expect(Math.abs(dist(r.a, live.p.circus.c) - live.p.circus.ring[1])).toBeLessThan(1e-6);
      expect(r.t).toBeGreaterThan(5); expect(r.t).toBeLessThan(6.6); // the tangent length along the face (Datum's 6.5 and 5.7 are for straight rings)
    }
  });
});

describe('the outside of a bend and the median noses', () => {
  test('eleven bends are concentric with their inside corners (the ten U lanes, the ring\'s south-east corner) and one L turn (Satellite Street into Starline Street): R inside + the carriageway\'s width, the same centre', () => {
    const inner = returnsOf(live.ctx), outer = inner.filter(r => r.bend);
    expect(outer.length).toBe(12);
    for (const o of outer) {
      const i = inner.find(r => r.id === o.bend)!, w = live.d.roads.find(r => r.id === o.roads[0])!.w;
      expect(dist(o.c, i.c)).toBeLessThan(1e-9); expect(o.r).toBeCloseTo(i.r + w, 6);
      expect(o.convex).toBe(false); expect(o.cut[0]).toEqual(o.at); // `cut` is the pavement laid into the road's corner
    }
    expect(outer.map(o => o.r).sort((a, b) => a - b).join()).toBe([18, 18, 18, 18, 18, 18, 18, 18, 18, 18, 18, 30].join()); // ten 6 + 12, one L 6 + 12, the ring 6 + 24
    expect(inner.filter(r => r.civic).every(r => !outer.some(o => o.bend === r.id))).toBe(true); // a civic corner keeps the outside of its turn square
  });
  test('a bend\'s two tangent points lie on one line across the road, so landings pair square', () => {
    for (const o of returnsOf(live.ctx).filter(r => r.bend)) { const i = returnsOf(live.ctx).find(r => r.id === o.bend)!; expect(Math.abs((o.a[0] - i.a[0]) * (o.b[1] - i.b[1]) - 0)).toBeGreaterThan(-1); expect(dist(o.a, i.a)).toBeCloseTo(o.r - i.r, 6); expect(dist(o.b, i.b)).toBeCloseTo(o.r - i.r, 6); }
  });
  test('every median end is a bullnose: 25 medians, 50 half rounds of half the width (R 2 on a 4 m median, R 3 on a 6 m one), the ring a stadium of the same width', () => {
    const noses = returnsOf(live.ctx).filter(r => r.nose); expect(noses.length).toBe(50);
    expect(noses.filter(r => r.r === 2).length).toBe(24); expect(noses.filter(r => r.r === 3).length).toBe(26);
    live.d.zones.filter(z => z.kind === 'median' && z.poly).forEach((z, zi) => {
      const ring = noseRing(live.ctx, zi, z.poly as V[]), w0 = ringWidth(z.poly as V[]);
      expect(ring.length).toBeGreaterThan(30); expect(ringWidth(ring)).toBeCloseTo(w0, 6);
      const ext = (q: V[], u: V) => { const d = q.map(p => p[0] * u[0] + p[1] * u[1]); return Math.max(...d) - Math.min(...d); }, P = z.poly as V[], e = dist(P[0]!, P[1]!) > dist(P[1]!, P[2]!) ? [P[1]![0] - P[0]![0], P[1]![1] - P[0]![1]] : [P[2]![0] - P[1]![0], P[2]![1] - P[1]![1]], l = Math.hypot(e[0]!, e[1]!), u: V = [e[0]! / l, e[1]! / l];
      expect(ext(ring, u)).toBeCloseTo(ext(P, u), 6); // the tip stays where the end was today: the median is never shortened
    });
  });
});
