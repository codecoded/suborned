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

describe('beds: dressBeds', () => {
  const ring = oval(10, 3.2);
  test('a bed with no kit piece is a kerb with a hedge on it and tufts inside; a bed with a piece is left alone', () => {
    const kept = park('civic/keeps', rect(4, 4), { source: 'kit', kit: { id: 'PlanterArc', scale: [1, 1, 1], yaw: 0, at: [2, 2], base: 0.18 } });
    const out = dressBeds([park('civic/b', ring), kept, { id: 'civic/wall', surface: 'limestone', ring: rect(1, 1), base: 0, height: 3 }]);
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
    const a = dressBeds([park('civic/b', ring)]), b = dressBeds([park('civic/b', ring.slice().reverse())]);
    expect(b.filter(m => m.kit).map(t => t.kit!.at)).toEqual(a.filter(m => m.kit).map(t => t.kit!.at));
    expect(b.find(m => m.id === 'civic/b/hedge')!.ring).toEqual(a.find(m => m.id === 'civic/b/hedge')!.ring.slice().reverse());
  });
  test('the second row stays where the bed is at least 0.9 m wide: a 0.8 m bed has one row, a 3 m bed two', () => {
    const count = (w: number) => dressBeds([park('civic/b', oval(14, w))]).filter(m => m.kit).length;
    expect(count(0.8)).toBe(stations(offsetRing(oval(14, 0.8), 0.5), 0.55).length);
    expect(count(3)).toBeGreaterThan(count(0.8) + 10);
  });
  test('the same ring and id give the same yaw every time; another id gives another', () => {
    const yaws = (id: string) => dressBeds([park(id, ring)]).filter(m => m.kit).map(t => t.kit!.yaw);
    expect(yaws('civic/b')).toEqual(yaws('civic/b')); expect(yaws('civic/c')).not.toEqual(yaws('civic/b'));
  });
  test('no beds, no change', () => { const m = [park('civic/x', ring, { source: 'procedural' }), { id: 'civic/y', surface: 'limestone', ring, base: 0, height: 1 }]; expect(dressBeds(m.filter(x => x.surface !== 'park'))).toEqual(m.filter(x => x.surface !== 'park')); });
});
