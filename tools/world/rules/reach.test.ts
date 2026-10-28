// RCH-1 reachability (brief 075): failing examples on small worlds, and the live plan's islands.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { V } from '../../plan/geom';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { ctxOf } from '../../plan/world-ctx';
import { type ReachInput, RCH_ENFORCED, reachReport, reachRules } from './reach';

setDefaultTimeout(120000);
const rect = (x0: number, x1: number, y0: number, y1: number): V[] => [[x0, y0], [x1, y0], [x1, y1], [x0, y1]];
/** A street along y 0 to x 100, the harbour from x 120 with a pier from the quay (x 100) out to x 150, a caisson landmark at (170, 0). */
const world = (o: Partial<ReachInput> = {}): ReachInput => ({
  roads: [[[0, 0], [100, 0]]], water: [rect(120, 300, -100, 100)], quay: [rect(100, 150, -5, 5)], beyond: [], masses: [],
  landmarks: [{ id: 'tower', c: [170, 0], r: 20, caisson: true }], megastructures: [], stations: [], decks: [], platforms: [], connectors: [], ...o,
});
const names = (r: ReturnType<typeof reachReport>) => r.islands.map(s => s.split(':')[0]);

test('a caisson that only touches its pier\'s end is an island; a pier built into it joins it', () => {
  expect(names(reachReport(world()))).toEqual(['landmark/tower']);
  expect(reachReport(world({ quay: [rect(100, 156, -5, 5)] })).islands).toEqual([]);
});

test('a ground landmark on the pavement is joined; one walled in by buildings is not', () => {
  const lm = [{ id: 'monument', c: [50, 20] as V, r: 5 }];
  expect(reachReport(world({ landmarks: lm })).islands).toEqual([]);
  expect(names(reachReport(world({ landmarks: lm, masses: [rect(40, 60, 8, 12), rect(40, 60, 28, 32), rect(40, 44, 8, 32), rect(56, 60, 8, 32)] })))).toEqual(['landmark/monument']);
});

test('a pier cut off by water is an island', () => {
  expect(names(reachReport(world({ landmarks: [], quay: [rect(100, 150, -5, 5), rect(200, 240, 50, 60)] })))).toEqual(['pier/quay[1] [200, 240, 50, 60]']);
});

test('a deck whose stair stands in the water is an island; on the pavement it is joined; a core inside a building that fronts the street joins it', () => {
  const decks = [{ id: 'deck/a', level: 'deck', ring: rect(40, 60, 30, 40) }];
  const stair = (ring: V[]) => [{ id: 'deck/a/stair', from: 'street', serves: ['deck/a'], ring }];
  expect(names(reachReport(world({ landmarks: [], decks, connectors: stair(rect(200, 204, 30, 34)) })))).toEqual(['deck deck/a']);
  expect(reachReport(world({ landmarks: [], decks, connectors: stair(rect(40, 44, 26, 30)) })).islands).toEqual([]);
  expect(reachReport(world({ landmarks: [], decks, masses: [rect(30, 70, 10, 50)], connectors: stair(rect(48, 52, 30, 34)) })).islands).toEqual([]);
});

test('riding the line: a station with no way down of its own is reached from another station of its line; a plan station with no platform is rejected', () => {
  const platforms = [{ id: 'station/S1/a/l', ring: rect(10, 60, 4, 8) }, { id: 'station/S1/b/l', ring: rect(200, 250, 4, 8) }];
  const connectors = [{ id: 'bank/S1/a/l/stair', from: 'street', serves: ['station/S1/a/l'], ring: rect(20, 24, 8, 12) }];
  const stations = [{ line: 'S1', id: 'a' }, { line: 'S1', id: 'b' }];
  // the caisson is reached through the station that stands in it
  expect(reachReport(world({ platforms, connectors, stations, landmarks: [{ id: 'tower', c: [225, 6], r: 20, caisson: true }] })).islands).toEqual([]);
  expect(names(reachReport(world({ platforms, connectors: [], stations, landmarks: [] })))).toEqual(['platform station/S1/a/l', 'platform station/S1/b/l', 'station/S1/a', 'station/S1/b']);
  expect(reachReport(world({ platforms, connectors, stations: [...stations, { line: 'S1', id: 'c' }], landmarks: [] })).islands).toEqual(['station/S1/c: no platform in the export (unknown = reject)']);
});

test('a megastructure in the water with no way in is an island', () => {
  expect(names(reachReport(world({ landmarks: [], megastructures: [{ id: 'm', footprint: [[200, 260, 40, 80]] }] })))).toEqual(['mega/m']);
  expect(reachReport(world({ landmarks: [], megastructures: [{ id: 'm', footprint: [[20, 60, 10, 40]] }] })).islands).toEqual([]);
});

test('the live plan: the Skyport Tower and the liner berth stand in the water with no way in; RCH-1 reports them (informational until the sky track reaches the Skyport)', () => {
  const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
  const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), c = ctxOf(plan, d, civic(plan.civic, read(plan.civic.block)));
  const enforced = reachRules(c, true)[0]!, live = reachRules(c)[0]!;
  expect(enforced.ok).toBe(false);
  expect(enforced.detail).toContain('landmark/skyport-tower');
  expect(live.ok).toBe(true);
  expect(!!live.waived).toBe(!RCH_ENFORCED);
});
