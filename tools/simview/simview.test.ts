// Brief 081: the look tool's dump is a read of the real run. Reading every tick must not move the run: the final hash equals the mission record's, and the engine's own untouched replay.
import { describe, expect, test } from 'bun:test';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { buildCli, dump, latestRecord, ROOT, RUNS } from './build';
import { blockToWorld, buildScene } from './scene';

const hasDotnet = !!Bun.which('dotnet');

describe('simview', () => {
  test.skipIf(!hasDotnet)('a dump ends on the recorded final hash, with the reads changing nothing', async () => {
    const dll = buildCli(), record = latestRecord();
    for (const script of ['route-a', 'loud-fight', 'fail-rivals-reach-vale']) {
      const d = await dump(dll, record, script);
      expect(d.data.readsChangedNothing).toBe(true);
      expect(d.data.finalHash).toBe(d.data.cleanFinalHash);
      expect(d.data.finalHash).toBe(d.recorded.finalHash);
      expect([d.data.outcome, d.data.fail, d.data.tick]).toEqual([d.recorded.outcome, d.recorded.fail, d.recorded.tick]);
      expect(d.code).toBe(0);
    }
  }, 60_000);

  test('the runs the page must carry are listed', () => {
    const have = RUNS.map(r => r.script);
    for (const s of ['route-a', 'route-b', 'route-g', 'loud', 'loud-fight', 'careless-takedown-seen-camera', 'fail-rivals-reach-vale']) expect(have).toContain(s);
  });

  test('the site frame lands on the world: the cafe corner', () => {
    expect(blockToWorld(-2200, -4000)).toEqual([-170, -208]); // civic-block cafe polygon corner against world civic/cafe ring
  });

  test.skipIf(!existsSync(join(ROOT, 'out/world/harbour-district.world.json')))('the scene holds the zone from the real export', () => {
    const s = buildScene(JSON.parse(readFileSync(join(ROOT, 'out/world/harbour-district.world.json'), 'utf8')), JSON.parse(readFileSync(join(ROOT, 'sim/data/handover.site.json'), 'utf8')));
    expect(s.walls.length).toBeGreaterThan(10); expect(s.flats.length).toBeGreaterThan(10); expect(s.grid.rows.length).toBe(s.grid.nx);
  });
});
