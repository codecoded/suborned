// The catalogue and the copy: kit.json matches the manifest, hashes verify, wave 1 is adopted, a copy twice copies nothing.
import { expect, test } from 'bun:test';
import { createHash } from 'node:crypto';
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import type { Kit } from '../../contracts/world/kit.schema';
import { buildCatalogue } from './catalogue';
import { copyAdopted } from './copy';
import { fit } from './fit';
import { GEN_ROOT, kitRoot } from './store';
import { WAVE1 } from './tags';

const kit: Kit = JSON.parse(readFileSync(resolve(import.meta.dir, '../../contracts/world/kit.json'), 'utf8'));
const root = kitRoot();

test('kit.json is the manifest, normalised, and rebuilding it changes nothing', () => {
  expect(kit.assets.filter(a => a.source !== 'generated').length).toBe(202);
  expect(JSON.stringify(buildCatalogue(root, kit, GEN_ROOT))).toBe(JSON.stringify(kit));
  for (const a of kit.assets) { expect(['footprint-centre-ground', 'origin-ground']).toContain(a.pivot); expect(a.front).toBe('+Z'); expect(a.size.every(n => n >= 0)).toBe(true); expect(a.files.some(f => f.role === 'fbx')).toBe(true); }
});

test('wave 1 is exactly the adopted set', () => {
  expect(new Set(kit.assets.filter(a => a.status === 'adopted' && a.source !== 'generated').map(a => a.id))).toEqual(WAVE1); // generated pieces are adopted on top (decokit/fit.test.ts)
});

test('copy verifies hashes, is idempotent, and refuses a tampered source', () => {
  const dest = mkdtempSync(join(tmpdir(), 'kit-')), a = copyAdopted(kit, root, dest, GEN_ROOT), b = copyAdopted(kit, root, dest, GEN_ROOT);
  try {
    expect(a.errors).toEqual([]); expect(a.copied.length).toBeGreaterThan(100);
    expect(b.copied.length).toBe(0); expect(b.skipped.length).toBe(a.copied.length);
    for (const f of kit.assets.filter(x => x.status === 'adopted').flatMap(x => x.files).slice(0, 40)) expect(createHash('sha256').update(readFileSync(join(dest, f.path))).digest('hex')).toBe(f.sha256);
    const bad = join(dest, kit.assets.find(x => x.id === 'Bollard')!.files[0]!.path); writeFileSync(bad, 'x');
    expect(copyAdopted(kit, root, dest, GEN_ROOT).copied).toContain(kit.assets.find(x => x.id === 'Bollard')!.files[0]!.path); // a damaged copy is re-copied
  } finally { rmSync(dest, { recursive: true, force: true }); }
});

test('fit scales inside the range and refuses what does not fit', () => {
  const lamp = kit.assets.find(a => a.id === 'Streetlamp')!;
  expect(fit(lamp, { h: 4.5, depth: 1.5 })![1]).toBeCloseTo(1.145, 2);
  expect(fit(lamp, { h: 14, depth: 1.5 })).toBeNull(); // 14 m: beyond the scale range
  const sky = kit.assets.find(a => a.id === 'Skyport_Tower')!;
  expect(fit(sky, { foot: 48, maxH: 24.68 })).toBeNull();
  expect(fit(kit.assets.find(a => a.id === 'SphereStation')!, { foot: 80, maxH: 80 })).not.toBeNull();
});
