// Brief 081: the look tool's dump is a read of the real run. Reading every tick must not move the run: the final hash equals the mission record's, and the engine's own untouched replay.
import { describe, expect, test } from 'bun:test';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { buildCli, dump, judge, latestRecord, recordedPlay, ROOT, RUNS } from './build';
import { blockToWorld, buildScene } from './scene';

const hasDotnet = !!Bun.which('dotnet');

describe('simview', () => {
  // Only a play the record holds as one run can be replayed: from c047 (brief 080) route-a, route-b, route-g and loud are adaptive plans across seeds (a digest:
  // median tick, the first seed's hashes), and loud-fight is no longer played. So the dump is checked on fixed-script plays: a quiet-side fail, a loud fight with shots, a camera fail.
  test.skipIf(!hasDotnet)('a dump ends on the recorded final hash, with the reads changing nothing', async () => {
    const dll = buildCli(), record = latestRecord();
    for (const script of ['fail-rivals-reach-vale', 'fight-loud-wait-square', 'careless-takedown-seen-camera']) { // engine 0.42.0 (c052): fight-loud-wait-yard loses to the lookout before a shot (0.41.0; Waymark recomposes it), so the loud fight with shots is the square's twin
      expect(`${script}: ${recordedPlay(record, script).kind}`).toBe(`${script}: script`);
      const d = await dump(dll, record, script);
      expect(d.data.readsChangedNothing).toBe(true);
      expect(d.data.finalHash).toBe(d.data.cleanFinalHash);
      expect(d.data.finalHash).toBe(d.recorded.finalHash);
      expect([d.data.outcome, d.data.fail, d.data.tick]).toEqual([d.recorded.outcome, d.recorded.fail, d.recorded.tick]);
      expect(d.code).toBe(0);
      if (script === 'fight-loud-wait-square') expect(d.data.shots.length).toBeGreaterThan(0);
    }
  }, 60_000);

  test('an adaptive plan or an unplayed script is unchecked, never a pass or a fail', () => {
    const record = latestRecord(), a = recordedPlay(record, 'route-a');
    expect(a.kind).toBe('adaptive');
    expect(judge({ code: 0, recorded: a.play, data: { readsChangedNothing: true, finalHash: 'x' } }, a)).toBe('unchecked');
    expect(judge({ code: 1, recorded: a.play, data: { readsChangedNothing: false } }, a)).toBe('differs'); // reads that move the run fail whatever the record
    expect(recordedPlay(record, 'no-such-script').kind).toBe('missing');
  });

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
