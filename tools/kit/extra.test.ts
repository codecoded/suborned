// Brief 077 (Splice): extra generated roots (tools/kit/extra.ts). A synthetic root with a rebuild of an adopted piece and a new companion: the rebuild joins as <id>_TB,
// a candidate with its own Kit/ file names, LOD screens and lodSize; the adopted piece is untouched; a row that would take an adopted id fails; a hash that moved fails.
import { describe, expect, test } from 'bun:test';
import { needs } from '../needs';
import { createHash } from 'node:crypto';
import { mkdirSync, mkdtempSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import type { KitAsset } from '../../contracts/world/kit.schema';
import { extraAssets, extraCollide, extraId, kitPath } from './extra';
import { lodCategoryOf, lodClassOf } from './lod';
import { tagsOf } from './tags';

const sha = (s: string) => createHash('sha256').update(s).digest('hex');
function root(rows: (dir: string) => unknown[]): string {
  const dir = mkdtempSync(join(tmpdir(), 'extra077-'));
  mkdirSync(join(dir, 'Models/Plaza'), { recursive: true });
  for (const f of ['DK_PlanterRun_4_A.fbx', 'DK_PlanterRun_4_A_L1.fbx', 'DK_Foo_Planting.fbx']) writeFileSync(join(dir, 'Models/Plaza', f), f);
  writeFileSync(join(dir, 'decokit_manifest.json'), JSON.stringify({ assets: rows(dir) }));
  return dir;
}
const rebuild = { name: 'PlanterRun_4_A', category: 'Plaza', status: 'candidate', fbx: 'Models/Plaza/DK_PlanterRun_4_A.fbx', tris: 5655, size: [4, 1.67, 0.68], bounds: [-2, 2, -0.34, 0.34], materials: ['DK_Limestone', 'DK_Leaf_TB'],
  files: [{ file: 'Models/Plaza/DK_PlanterRun_4_A.fbx', sha256: sha('DK_PlanterRun_4_A.fbx'), bytes: 21, objects: [{ name: 'DK_PlanterRun_4_A_soil', materials: ['DK_Soil_TB'], shadows: 'off' }] }],
  lods: [{ file: 'Models/Plaza/DK_PlanterRun_4_A_L1.fbx', tris: 3837, screen: 0.6667, sha256: sha('DK_PlanterRun_4_A_L1.fbx') }], collide: [{ shape: 'box', min: [0, 0, 0], max: [1, 1, 1] }], replaces: { id: 'PlanterRun_4_A' } };
const companion = { name: 'Foo_Planting', category: 'Plaza', status: 'candidate', fbx: 'Models/Plaza/DK_Foo_Planting.fbx', tris: 100, size: [1, 1, 1], bounds: [-0.5, 0.5, -0.5, 0.5], materials: ['DK_Leaf_TB'],
  files: [{ file: 'Models/Plaza/DK_Foo_Planting.fbx', sha256: sha('DK_Foo_Planting.fbx'), bytes: 19 }], lods: [], lodSize: 4, collide: [] };
const h = (prior = new Map<string, string>()) => ({ prior, others: (m: string) => (m === 'DK_Limestone' ? [{ root: 'generated' as const, role: 'material' as const, path: 'Materials/DK_Limestone.mat', sha256: 'x', bytes: 1 }] : []),
  pivot: () => ({ pivot: 'footprint-centre-ground' as const }), lodCategoryOf, lodClassOf, tagsOf: (id: string, c: string) => tagsOf(id, c) });

describe('kit: extra generated roots (brief 077)', () => {
  test('a rebuild joins as <id>_TB, a candidate with renamed Kit/ files, its own LOD screens and its objects; a new piece keeps its name and lodSize', () => {
    const dir = root(() => [rebuild, companion]), out = extraAssets(dir, h()), tb = out.find(a => a.id === 'PlanterRun_4_A_TB')!, co = out.find(a => a.id === 'Foo_Planting')!;
    expect(out.map(a => a.id)).toEqual(['PlanterRun_4_A_TB', 'Foo_Planting']);
    expect([tb.status, tb.replaces, tb.source]).toEqual(['candidate', 'PlanterRun_4_A', 'generated']);
    expect(tb.files.filter(f => f.root === 'extra').map(f => [f.role, f.path, f.src])).toEqual([['fbx', 'Models/Plaza/DK_PlanterRun_4_A_TB.fbx', 'Models/Plaza/DK_PlanterRun_4_A.fbx'], ['lod', 'Models/Plaza/DK_PlanterRun_4_A_TB_L1.fbx', 'Models/Plaza/DK_PlanterRun_4_A_L1.fbx']]);
    expect(tb.lods).toEqual([{ file: 'Models/Plaza/DK_PlanterRun_4_A_TB_L1.fbx', tris: 3837, screen: 0.6667 }]); // never the category's 0.008 / 0.003
    expect(tb.files.some(f => f.path === 'Materials/DK_Limestone.mat')).toBe(true); // a material the stonework keeps comes from the other roots
    expect(tb.objects).toEqual([{ name: 'DK_PlanterRun_4_A_soil', materials: ['DK_Soil_TB'], shadows: 'off' }]);
    expect(tb.tags).toEqual(tagsOf('PlanterRun_4_A', 'Plaza'));
    expect([co.status, co.lodSize, co.replaces]).toEqual(['candidate', 4, undefined]);
    expect([tb.lodAtPlay, co.lodAtPlay]).toEqual([true, undefined]); // a row with LODs declares lodAtPlay (Latch's guard); the companion here has none
  });
  test('the prior catalogue\'s status is kept; a row that says adopted still joins as a candidate', () => {
    const dir = root(() => [{ ...rebuild, status: 'adopted' }]);
    expect(extraAssets(dir, h()).map(a => a.status)).toEqual(['candidate']);
    expect(extraAssets(dir, h(new Map([['PlanterRun_4_A_TB', 'adopted']]))).map(a => a.status)).toEqual(['adopted']);
  });
  test('a file whose sha256 is not the manifest\'s fails', () => {
    const dir = root(() => [{ ...rebuild, files: [{ ...rebuild.files[0]!, sha256: sha('other') }] }]);
    expect(() => extraAssets(dir, h())).toThrow(/is not the manifest's/);
  });
  test('declared collision by catalogue id, an empty list kept as declared none (render-only)', () => {
    const dir = root(() => [rebuild, companion]), rel = dir.replace(/^\//, '/'); // absolute: resolve() keeps it
    const c = extraCollide([{ id: 'x', extra: rel }]);
    expect([...c.keys()].sort()).toEqual(['Foo_Planting', 'PlanterRun_4_A_TB']); expect(c.get('Foo_Planting')).toEqual([]);
  });
  test('ids and paths', () => {
    expect(extraId(rebuild as never)).toBe('PlanterRun_4_A_TB'); expect(extraId(companion as never)).toBe('Foo_Planting');
    expect(kitPath('Models/Plaza/DK_A_L2.fbx', 'A', 'A_TB')).toBe('Models/Plaza/DK_A_TB_L2.fbx'); expect(kitPath('Models/Plaza/DK_A.fbx', 'A', 'A')).toBe('Models/Plaza/DK_A.fbx');
  });
});

// The live catalogue: the 077 candidates are in, none adopted, and no adopted piece points at an extra root's file.
describe('kit.json: the brief 077 pieces (adopted 2026-10-30 on Sky\'s yes to the Unity capture)', () => {
  const kit = JSON.parse(require('node:fs').readFileSync(join(import.meta.dir, '../../contracts/world/kit.json'), 'utf8')) as { assets: KitAsset[] };
  const extra = kit.assets.filter(a => a.extra);
  // brief 097: rebuilds drawn only in the Handover zone; the piece they rebuild stays adopted for the rest of the town (ShrubMound: 36 beds outside, ShrubMound_TB 13 inside)
  const ZONE_TWINS = new Set(['ShrubMound_TB']);
  // 28: brief 097 pass 3 added ShrubMound_TB (Vela, from the testbed planting)
  test('every extra-root piece is adopted; each rebuild names the dome piece it replaces, which is rejected, and BedTuft_A/B/C are rejected', () => {
    expect(extra.length).toBe(28); expect(extra.every(a => a.status === 'adopted')).toBe(true);
    for (const a of extra.filter(x => x.replaces && !ZONE_TWINS.has(x.id))) { expect(a.id).toBe(`${a.replaces}_TB`); expect(kit.assets.find(x => x.id === a.replaces)?.status).toBe('rejected'); }
    for (const id of ['BedTuft_A', 'BedTuft_B', 'BedTuft_C']) expect(kit.assets.find(x => x.id === id)?.status).toBe('rejected');
  });
  test('every extra-root piece with LODs declares lodAtPlay (Latch\'s play-zoom guard); GlobeLamp_TB has none', () => {
    expect(extra.filter(a => a.lods?.length).every(a => a.lodAtPlay === true)).toBe(true);
    expect(extra.filter(a => !a.lods?.length).map(a => [a.id, a.lodAtPlay])).toEqual([['GlobeLamp_TB', undefined]]);
  });
  test.skipIf(!needs('out/decokit-097-extra/decokit_manifest.json', 'the brief 097 extra DecoKit run (Vela)'))('each keeps its manifest\'s own LOD screens, never the category pair', () => {
    for (const a of extra) {
      const r = (JSON.parse(require('node:fs').readFileSync(join(import.meta.dir, '../..', a.extra!, 'decokit_manifest.json'), 'utf8')).assets as { name: string; replaces?: { id: string }; lods?: { screen: number }[] }[]).find(x => (x.replaces?.id ? `${x.replaces.id}_TB` : x.name) === a.id);
      if (!r) continue; // its root has moved on: the entry is kept as catalogued
      expect((a.lods ?? []).map(l => l.screen), a.id).toEqual((r.lods ?? []).map(l => l.screen));
    }
  });
  test('no Kit/ path is shared between an extra piece and another piece', () => {
    const own = new Map<string, string>();
    for (const a of kit.assets) for (const f of a.files.filter(x => /^(fbx|lod)$/.test(x.role))) { const o = own.get(f.path); if (o) expect(`${f.path}: ${o} and ${a.id}`).toBe(''); own.set(f.path, a.id); }
  });
});
