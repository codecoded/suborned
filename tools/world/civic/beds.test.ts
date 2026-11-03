// The bed recipe (beds.ts) on rings of known size: the offset, the stations, and a bed's kerb, hedge and tufts.
import { describe, expect, test } from 'bun:test';
import type { Mass } from '../../../contracts/world/world.schema';
import { type V, inPoly } from '../../plan/geom';
import { dressBeds, isBed, offsetRing, stations } from './beds';

const rect = (w: number, d: number): V[] => [[0, 0], [w, 0], [w, d], [0, d]]; // counter-clockwise
/** A bed shaped like the block's: a 36-vertex oval, counter-clockwise, `w` m long and `d` m wide. */
const oval = (w: number, d: number): V[] => Array.from({ length: 36 }, (_, i) => [w / 2 + (w / 2) * Math.cos((i * Math.PI) / 18), d / 2 + (d / 2) * Math.sin((i * Math.PI) / 18)] as V);
const park = (id: string, ring: V[], extra: Partial<Mass> = {}): Mass => ({ id, surface: 'park', ring, base: 0.18, height: 0.52, ...extra });

describe('beds: ring offset and stations', () => {
  test('a counter-clockwise ring moves inward by the offset at its straight edges', () => {
    const o = offsetRing([[0, 0], [10, 0], [10, 4], [0, 4], [0, 0.0001]].slice(0, 4) as V[], 0.5);
    expect(o[0]![0]).toBeGreaterThan(0); expect(o[0]![1]).toBeGreaterThan(0);
    expect(o.every(p => inPoly(p, rect(10, 4)))).toBe(true);
  });
  test('stations: max(4, round(length / step)) of them, evenly spaced, the first at the first vertex', () => {
    const s = stations(rect(11, 5.5), 0.55);
    expect(s).toHaveLength(60); expect(s[0]).toEqual([0, 0]);
    expect(stations(rect(1, 1), 0.55)).toHaveLength(7);
    expect(stations(rect(0.2, 0.2), 0.55)).toHaveLength(4);
  });
});

/** Brief 077: BedTuft_A/B/C are rejected since the beds went bed level (2026-10-30); the tuft recipe (the fallback for a bed without its piece) is held on a catalogue that
 *  still adopts them. */
const tuftKit = (() => {
  const { mkdtempSync, readFileSync, writeFileSync } = require('node:fs') as typeof import('node:fs'), { tmpdir } = require('node:os') as typeof import('node:os'), { join } = require('node:path') as typeof import('node:path');
  const kit = JSON.parse(readFileSync(join(import.meta.dir, '../../../contracts/world/kit.json'), 'utf8'));
  for (const x of kit.assets) if (/^BedTuft_[ABC]$/.test(x.id)) x.status = 'adopted';
  const f = join(mkdtempSync(join(tmpdir(), 'tufts-')), 'kit.json'); writeFileSync(f, JSON.stringify(kit)); return f;
})();
const T = <X>(fn: () => X): X => (require('../gen/kitrefs') as typeof import('../gen/kitrefs')).withKit(tuftKit, fn);
describe('beds: dressBeds', () => {
  const ring = oval(10, 3.2);
  test('a bed with no kit piece is a kerb with a hedge on it and tufts inside; a bed with a piece is left alone', () => {
    const kept = park('civic/keeps', rect(4, 4), { source: 'kit', kit: { id: 'PlanterArc', scale: [1, 1, 1], yaw: 0, at: [2, 2], base: 0.18 } });
    const out = T(() => dressBeds([park('civic/b', ring), kept, { id: 'civic/wall', surface: 'limestone', ring: rect(1, 1), base: 0, height: 3 }]));
    expect(out[0]!.surface).toBe('bed-kerb'); expect(out[0]!.ring).toEqual(ring);
    expect(out[1]).toBe(kept);
    const hedge = out.find(m => m.id === 'civic/b/hedge')!, tufts = out.filter(m => /^civic\/b\/tuft-\d+$/.test(m.id));
    expect(hedge.surface).toBe('bed-hedge'); expect([hedge.base, hedge.height]).toEqual([0.7, 0.22]);
    expect(tufts.length).toBeGreaterThan(40);
    expect(tufts.every(t => t.kit && t.kit.base === 0.88 && t.ring.every(p => inPoly(p, ring)))).toBe(true);
    expect(tufts.map(t => t.kit!.id).slice(0, 4)).toEqual(['BedTuft_A', 'BedTuft_B', 'BedTuft_C', 'BedTuft_A']);
    expect(out.filter(isBed)).toEqual([]);
  });
  test('a clockwise ring gives the same tufts and a hedge in the ring\'s own winding', () => {
    const a = T(() => dressBeds([park('civic/b', ring)])), b = T(() => dressBeds([park('civic/b', ring.slice().reverse())]));
    expect(b.filter(m => m.kit).map(t => t.kit!.at)).toEqual(a.filter(m => m.kit).map(t => t.kit!.at));
    expect(b.find(m => m.id === 'civic/b/hedge')!.ring).toEqual(a.find(m => m.id === 'civic/b/hedge')!.ring.slice().reverse());
  });
  test('the second row stays where the bed is at least 0.9 m wide: a 0.8 m bed has one row, a 3 m bed two', () => {
    const count = (w: number) => T(() => dressBeds([park('civic/b', oval(14, w))])).filter(m => m.kit).length;
    expect(count(0.8)).toBe(stations(offsetRing(oval(14, 0.8), 0.5), 0.55).length);
    expect(count(3)).toBeGreaterThan(count(0.8) + 10);
  });
  test('the same ring and id give the same yaw every time; another id gives another', () => {
    const yaws = (id: string) => T(() => dressBeds([park(id, ring)])).filter(m => m.kit).map(t => t.kit!.yaw);
    expect(yaws('civic/b')).toEqual(yaws('civic/b')); expect(yaws('civic/c')).not.toEqual(yaws('civic/b'));
  });
  test('no beds, no change', () => { const m = [park('civic/x', ring, { source: 'procedural' }), { id: 'civic/y', surface: 'limestone', ring, base: 0, height: 1 }]; expect(T(() => dressBeds(m.filter(x => x.surface !== 'park')))).toEqual(m.filter(x => x.surface !== 'park')); });
});

// Brief 077 (Sky 2026-10-30): a bed civic-look.json `beds.level` names is planted by one bed-level piece, placed by the rigid fit of its outline (in the piece's frame)
// onto the bed's ring, soil 3 mm above the kerb top; no hedge, no tufts. A piece not adopted yet (or a ring that no longer fits) keeps the hedge and tufts.
describe('beds: bed level (brief 077)', () => {
  const { mkdtempSync, readFileSync, writeFileSync } = require('node:fs') as typeof import('node:fs');
  const { tmpdir } = require('node:os') as typeof import('node:os');
  const { join } = require('node:path') as typeof import('node:path');
  const { withKit } = require('../gen/kitrefs') as typeof import('../gen/kitrefs');
  const { bedLevelOf, fitRigid } = require('./beds') as typeof import('./beds');
  const { bedLevelRule } = require('../rules/civic-dress') as typeof import('../rules/civic-dress');
  /** The outline in the piece's frame (far from its origin, as the testbed's plaza frame is), and the same bed in the world: turned 2.0 rad, moved, rounded to 1 mm, its first vertex elsewhere. */
  const outline: V[] = oval(4, 1.6).map(([x, y]) => [x + 22.95, y - 14.6] as V);
  const th = 2.0, t: V = [-117.2, -307.5];
  const world = (o: V[]): V[] => o.map(([x, y]) => [+(Math.cos(th) * x - Math.sin(th) * y + t[0]).toFixed(3), +(Math.sin(th) * x + Math.cos(th) * y + t[1]).toFixed(3)] as V);
  const ring = world(outline);
  const level = { soilProud: 0.003, tol: 0.001, source: 'test', beds: { 'civic/b': { piece: 'CivicBed_test', outline } } };
  const kitFile = (() => {
    const dir = mkdtempSync(join(tmpdir(), 'beds077-')), file = join(dir, 'kit.json');
    const kit = JSON.parse(readFileSync(join(import.meta.dir, '../../../contracts/world/kit.json'), 'utf8'));
    kit.assets.push({ id: 'CivicBed_test', category: 'Plaza', size: [4, 1.36, 1.6], pivot: 'footprint-centre-ground', front: '+Z', tris: 17138, materials: ['DK_Soil_TB', 'DK_Leaf_TB', 'DK_Petal_TB'], status: 'adopted', tags: { use: [] }, files: [] });
    writeFileSync(file, JSON.stringify(kit)); return file;
  })();

  test('fitRigid recovers the motion to well under 1 mm, vertex i onto vertex i: a half-symmetric bed is never turned 180°', () => {
    const f = fitRigid(outline, ring)!;
    expect(f.err).toBeLessThan(0.001); expect(f.yaw).toBeCloseTo(th, 3); expect(f.at[0]).toBeCloseTo(t[0], 2); expect(f.at[1]).toBeCloseTo(t[1], 2);
    // the oval is symmetric under a half turn: started half way round it fits as well, so the start is the correspondence, never searched
    const half = [...ring.slice(18), ...ring.slice(0, 18)], g = fitRigid(outline, half)!;
    expect(g.err).toBeLessThan(0.001); expect(Math.abs(Math.abs(g.yaw - f.yaw) - Math.PI)).toBeLessThan(1e-3);
    expect(fitRigid(outline, [...ring.slice(7), ...ring.slice(0, 7)])!.err).toBeGreaterThan(0.1);
    expect(fitRigid(outline, ring.slice(1))).toBeUndefined();
  });
  test('an adopted bed-level piece plants the bed: one piece, the fitted pivot and yaw, soil 3 mm above the kerb top, no hedge, no tufts', () => {
    const out = withKit(kitFile, () => dressBeds([park('civic/b', ring)], level));
    const own = out.filter(m => m.id.startsWith('civic/b/'));
    expect(own.map(m => m.id)).toEqual(['civic/b/planting']);
    const p = own[0]!, f = fitRigid(outline, ring)!;
    expect(p.kit).toEqual({ id: 'CivicBed_test', scale: [1, 1, 1], yaw: f.yaw, at: f.at, base: 0.703 });
    expect([p.base, p.height, p.source, p.ring]).toEqual([0.703, 1.36, 'kit', ring]);
    expect(out[0]!.surface).toBe('bed-kerb');
    expect(withKit(kitFile, () => dressBeds([park('civic/b', ring)], level))).toEqual(out); // deterministic
    expect(withKit(kitFile, () => bedLevelRule(out, bedLevelOf(out, level))).ok).toBe(true);
  });
  test('the piece not adopted yet: the bed keeps its hedge and tufts, and CD-5 passes naming it as waiting', () => {
    const out = T(() => dressBeds([park('civic/b', ring)], level));
    expect(out.some(m => m.id === 'civic/b/hedge')).toBe(true); expect(out.some(m => m.id === 'civic/b/planting')).toBe(false);
    const c = T(() => bedLevelRule(out, bedLevelOf(out, level)));
    expect(c.ok).toBe(true); expect(c.detail).toContain('waiting: civic/b (CivicBed_test is not adopted');
  });
  test('the piece not adopted and no tuft adopted either (the live kit since 077): an empty bed, and CD-5 fails', () => {
    const out = dressBeds([park('civic/b', ring)], level);
    expect(out.some(m => /^civic\/b\/tuft-/.test(m.id) || m.id === 'civic/b/planting')).toBe(false);
    const c = bedLevelRule(out, bedLevelOf(out, level));
    expect(c.ok).toBe(false); expect(c.detail).toContain('no tuft is adopted either: an empty bed');
  });
  test('a ring moved 5 mm at one vertex no longer fits: not planted, and CD-5 fails with the error', () => {
    const bent = ring.map((p, i) => (i === 3 ? [p[0] + 0.005, p[1]] as V : p));
    const out = withKit(kitFile, () => dressBeds([park('civic/b', bent)], level));
    expect(out.some(m => m.id === 'civic/b/planting')).toBe(false); expect(out.some(m => m.id === 'civic/b/hedge')).toBe(true);
    const c = withKit(kitFile, () => bedLevelRule(out, bedLevelOf(out, level)));
    expect(c.ok).toBe(false); expect(c.detail).toMatch(/civic\/b: the ring fits its outline to \d\.\d\d mm, over 1\.0 mm/);
  });
  test('a planted bed that still carries tufts fails CD-5', () => {
    const out = withKit(kitFile, () => dressBeds([park('civic/b', ring)], level));
    const tuft = { id: 'civic/b/tuft-0', surface: 'trim', ring: rect(0.6, 0.6), base: 0.88, height: 0.45 } as Mass;
    expect(withKit(kitFile, () => bedLevelRule([...out, tuft], bedLevelOf(out, level))).ok).toBe(false);
  });
});
