import { expect, test } from 'bun:test';
import { mkdtempSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { encodePng } from '../hud-budget/png';
import { meanLuma, nightPairs, nightText } from './nightcheck';

const flat = (v: number) => { const w = 4, h = 4, data = new Uint8Array(w * h * 4); for (let i = 0; i < w * h; i++) data.set([v, v, v, 255], i * 4); return { w, h, data }; };

test('mean luma of a flat image is its value', () => { expect(meanLuma(flat(100))).toBeCloseTo(100, 5); });

test('a night shot as bright as its day fails; a dark one passes; a lone night is not judged', () => {
  const d = mkdtempSync(join(tmpdir(), 'night-'));
  writeFileSync(join(d, 'cafe-front-day.png'), encodePng(flat(170)));
  writeFileSync(join(d, 'cafe-front-night.png'), encodePng(flat(165)));
  writeFileSync(join(d, 'start-overhead-day.png'), encodePng(flat(160)));
  writeFileSync(join(d, 'start-overhead-night.png'), encodePng(flat(60)));
  writeFileSync(join(d, 'lone-night.png'), encodePng(flat(20)));
  const p = nightPairs(d).sort((a, b) => a.name.localeCompare(b.name));
  expect(p.map(x => [x.name, x.ok])).toEqual([['cafe-front-night', false], ['start-overhead-night', true]]);
  expect(nightText(p[0]!)).toContain('night drawn as day');
});
