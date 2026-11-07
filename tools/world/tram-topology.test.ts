import { test, expect } from 'bun:test';
import { readFileSync } from 'node:fs';
import { needs } from '../needs';
const hasWorld = needs('out/world/harbour-district.world.json', 'bun run world');
import { captureTram, topology, outputPath, DEFAULT_INPUTS } from './tram-topology';
import { tramGround, newTramLog } from './gen/street/tramstreet';
import { FLAT_FOOT } from './gen/common';
const read = (p: string) => JSON.parse(readFileSync(p, 'utf8'));

test.skipIf(!hasWorld)('opt-in capture is deterministic and leaves default ground/log byte-identical to pinned rails', () => {
  const p = read(DEFAULT_INPUTS.plan), b = read(DEFAULT_INPUTS.block);
  const a = captureTram(p, b), log = newTramLog(), plain = tramGround(a.ctx, log, FLAT_FOOT);
  expect(JSON.stringify(plain)).toBe(JSON.stringify(a.ground));
  expect(JSON.stringify(log)).toBe(JSON.stringify(a.log));
  expect(JSON.stringify(captureTram(p, b))).toBe(JSON.stringify(a));
  expect(plain!.rails).toEqual(read(DEFAULT_INPUTS.world).ground.pieces.filter((x: any) => x.kind === 'tram-rail'));
});

test.skipIf(!hasWorld)('complete pinned mapping, source topology and analytical rounding/cap bounds', () => {
  const t = topology();
  expect(t.counts).toMatchObject({ parents: 454, tiles: 1584, omitted: 0, classified: 188, parallel: 178 });
  expect(new Set(t.tiles.map(x => x.id)).size).toBe(1584);
  expect(t.continuousRuns.length).toBe(3);
  expect(t.tracks.every(x => x.continuous && !x.closed && x.neighbors.every(n => n.sourceAdjacent))).toBe(true);
  expect(t.graph.filter(e => e.joins.length === 1).length).toBe(4);
  expect(t.graph.filter(e => e.joins.length === 0).length).toBe(2);
  for (const tile of t.tiles) {
    expect(tile.error.center).toBeLessThan(t.rounding.subdivisionCenterBound);
    expect(tile.error.step).toBeLessThanOrEqual(.0005 / tile.subdivisions + 1e-12);
    expect(tile.error.tangent).toBeLessThan(t.rounding.tangentVectorBound + 1e-12);
  }
  for (const r of t.rejected) {
    expect(r.sourceAdjacent).toBe(true);
    expect(Math.abs(r.roundedTransverseOffset)).toBeLessThanOrEqual(r.roundedTransverseBound);
    expect(r.exactTangentEndSeparation).toBeLessThanOrEqual(r.tangentEndSeparationBound);
    if (r.message.includes('parallel displaced')) {
      expect(Math.abs(r.exactTransverseOffset)).toBeLessThan(1e-12);
      expect(r.classification).toBe('rounded-yaw-displacement; exact-lines-coincident');
    } else {
      expect(r.exactTangentEndSeparation).toBeGreaterThan(r.helperValidationLimit);
      expect(r.fromSegment).not.toBe(r.toSegment);
      expect([r.source.from?.geometry.kind, r.source.to?.geometry.kind].sort()).toEqual(['arc', 'line']);
    }
  }
  expect(t.unsupported.length).toBe(10);
  expect(JSON.stringify(topology())).toBe(JSON.stringify(t));
});

test('diagnostics retain off-sheet omissions; absent standard emits no invented geometry', () => {
  const p = read(DEFAULT_INPUTS.plan), b = read(DEFAULT_INPUTS.block);
  p.sheet.x = [-10, 10]; p.sheet.y = [-10, 10];
  const a = captureTram(p, b);
  expect(a.items.some(i => i.omitted === 'off-sheet')).toBe(true);
  expect(a.items.filter(i => i.exported).length).toBe(a.ground!.rails.length);
  delete p.standards.streetscape.sections.tramStreet;
  const empty = captureTram(p, b);
  expect(empty.ground).toBeUndefined(); expect(empty.items.length).toBe(0);
});

test('CLI requires an explicit confined output; stale pinned export fails closed', () => {
  expect(() => outputPath('out/world/harbour-district.world.json')).toThrow();
  expect(() => outputPath('out/scratch/splice/tram-topology/../world.json')).toThrow();
  expect(outputPath('out/scratch/splice/tram-topology/test.json')).toEndWith('/test.json');
  expect(() => topology({ ...DEFAULT_INPUTS, world: DEFAULT_INPUTS.plan })).toThrow();
});
