// The getaway car, finished (brief 070, Vela; Glyph on the briefing's getaway still: "the car model has no detail and looks like a stand-in"): the candidate Sedan_Getaway_v2 is the adopted
// Sedan_Getaway's car to the centimetre (size, pivot, front: the sim's car footprint and the host's drop-in are unchanged), at full detail, in the same livery (its car enamels are the base rows'
// colours with the car detail pair), chrome and no brass or gold, and nothing lit beyond the sedans' own lamps.
import { expect, test } from 'bun:test';
import { existsSync, readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import type { Kit } from '../../contracts/world/kit.schema';
import { GEN_ROOT } from '../kit/store';

const kit: Kit = JSON.parse(readFileSync(resolve(import.meta.dir, '../../contracts/world/kit.json'), 'utf8'));
const piece = (id: string) => kit.assets.find(a => a.id === id)!;
type Row = { name: string; kind: string; color: number[]; emission: number; robotDetail?: { detail: string; detailNormal: string; tile: number[]; bump: number } };
const mats = new Map((JSON.parse(readFileSync(join(GEN_ROOT, 'decokit_materials.json'), 'utf8')).materials as Row[]).map(m => [m.name, m]));

test('Sedan_Getaway_v2 is a candidate that drops in for Sedan_Getaway: the same size, pivot and front, full detail, files hash-checked', () => {
  const v = piece('Sedan_Getaway_v2'), g = piece('Sedan_Getaway');
  expect(v).toBeDefined(); expect(v.category).toBe('Vehicles'); expect(v.source).toBe('generated'); expect(['candidate', 'adopted']).toContain(v.status);
  expect(v.tags.use).toEqual(g.tags.use); expect(v.tags.scale).toEqual([1, 1]); expect(v.tags.sight).toBe('open');
  for (let i = 0; i < 3; i++) expect(Math.abs(v.size[i]! - g.size[i]!), `size[${i}] (m)`).toBeLessThan(0.002);
  expect(v.pivot).toBe(g.pivot); expect(v.front).toBe(g.front); expect(v.pivotOffset ?? [0, 0]).toEqual(g.pivotOffset ?? [0, 0]);
  expect(v.lods, 'full detail: no LODs').toBeUndefined(); expect(v.files.filter(f => f.role === 'lod')).toEqual([]);
  expect(v.tris, 'character over triangles').toBeGreaterThan(g.tris);
  for (const f of v.files) { const p = join(GEN_ROOT, f.path); if (f.root === 'generated') { expect(existsSync(p), f.path).toBe(true); expect(new Bun.CryptoHasher('sha256').update(readFileSync(p)).digest('hex'), f.path).toBe(f.sha256); } }
  for (const t of ['Textures/car_enamel_detail_v1.png', 'Textures/car_enamel_detail_v1_normal.png']) expect(v.files.some(f => f.path === t), `${t} travels with it`).toBe(true);
});

test('the livery is the getaway\'s: the car enamels are the coral and seafoam rows, with the car detail pair; chrome, no brass or gold, nothing new lit', () => {
  const v = piece('Sedan_Getaway_v2');
  for (const [car, base] of [['DK_CarEnamel_Coral', 'DK_Accent_Coral'], ['DK_CarEnamel_Seafoam', 'DK_Enamel_Seafoam']] as const) {
    expect(v.materials).toContain(car);
    const c = mats.get(car)!, b = mats.get(base)!;
    expect(c.color).toEqual(b.color); expect(c.kind).toBe(b.kind);
    expect(c.robotDetail?.detail).toBe('car_enamel_detail_v1.png'); expect(c.robotDetail?.detailNormal).toBe('car_enamel_detail_v1_normal.png');
  }
  expect(v.materials).toContain('DK_Chrome');
  expect(v.materials.filter(m => /gold|brass/i.test(m))).toEqual([]);
  const lit = v.materials.filter(m => (mats.get(m)?.emission ?? 0) > 0).sort(), sib = piece('Sedan_Getaway').materials.filter(m => (mats.get(m)?.emission ?? 0) > 0).sort();
  expect(lit).toEqual(sib);
});

test('the robot enamels keep their own detail pair (the car pair is added beside it, not over it)', () => {
  const r = mats.get('DK_RobotEnamel_Cream')!;
  expect(r.robotDetail?.detail).toBe('robot_enamel_detail_v1.png'); expect(r.robotDetail?.tile).toEqual([0.5, 2.0]);
});
