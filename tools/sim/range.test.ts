// Brief 029: the fight range's sites are what tools/sim/range.ts writes, and hold what the engine's range mode reads.
import { expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { buildRange, RANGE_FILES } from './range';

const root = join(import.meta.dir, '..', '..');
for (const [layout, file] of Object.entries(RANGE_FILES) as [keyof typeof RANGE_FILES, string][]) {
  test(`${file} is what tools/sim/range.ts writes (bun tools/sim/range.ts)`, () => {
    expect(readFileSync(join(root, file), 'utf8')).toBe(buildRange(layout).json);
  });
  test(`${layout}: six slots per placement, cover slots on fire-over cover points, the four kinds of piece`, () => {
    const s = buildRange(layout).site;
    expect(s.mission).toBe('range');
    for (const p of ['near', 'eff', 'beyond', 'cover']) for (let k = 1; k <= 6; k++) expect(s.places[`spawn:${p}:${k}`], `${p}:${k}`).toBeDefined();
    for (let k = 1; k <= 6; k++) {
      const at = s.places[`spawn:cover:${k}`].at, c = s.cover.points.find((q: any) => q.at[0] === at[0] && q.at[1] === at[1]);
      expect(c?.against, `cover:${k}`).toBe('fire');
    }
    for (const kind of ['low-wall', 'high-wall', 'pillar', 'car-box']) expect(s.occluders.some((o: any) => o.id.startsWith(`range/${kind}-`)), kind).toBe(true);
    expect(s.getaway.car).toBe('range:mark');
  });
}
