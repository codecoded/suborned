// The weather and night sounds (Chime, brief 107): contracts/audio/weather.json against its schema, the clips on disk and the 087 pack it joins.
import { expect, test } from 'bun:test';
import { createHash } from 'node:crypto';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { Weather } from '../../contracts/audio/weather.schema.ts';
import { root } from './synth.ts';

const j = (p: string) => JSON.parse(readFileSync(join(root, p), 'utf8'));
const w = Weather.parse(j('contracts/audio/weather.json')), pack = j('briefs/087/cues.json');

test('every clip is on disk with its hash, its import settings and a Resources path that matches', () => {
  for (const c of w.cues) {
    const p = join(root, c.path); expect(existsSync(p)).toBe(true);
    expect(createHash('sha256').update(readFileSync(p)).digest('hex')).toBe(c.sha256);
    expect(c.path).toBe(`unity/Assets/Suborned/Resources/${c.file}.wav`);
    const m = readFileSync(p + '.meta', 'utf8'); expect(m).toContain(`3D: ${c.spatial.startsWith('3d') ? 1 : 0}`); expect(m).toContain(`forceToMono: ${c.measured.channels === 1 ? 1 : 0}`);
  }
});

test('the rows fit the 087 pack: its buses and spatial table, no id taken twice', () => {
  const ids = new Set(pack.cues.map((c: { id: string }) => c.id));
  for (const c of w.cues) { expect(pack.buses[c.mix_group]).toBeDefined(); expect(pack.spatial[c.spatial]).toBeDefined(); expect(ids.has(c.id)).toBe(false); ids.add(c.id); }
});

test('loops join cleanly, and rain stays light background under the street bed', () => {
  for (const c of w.cues.filter(c => c.loop)) { const s = c.measured.seam!; expect(s.step_percentile).toBeLessThan(99); expect(s.level_jump_db).toBeLessThanOrEqual(s.typical_jump_db_p95); }
  const street = pack.cues.find((c: { id: string }) => c.id === 'street_bed').measured.lufs_integrated as number;
  for (const c of w.cues.filter(c => c.when.weather?.includes('rain') && c.loop)) expect(c.measured.lufs_integrated).toBeLessThanOrEqual(street - 8); // Sky, 2026-11-11: "rain needs to be light background noise"
});
