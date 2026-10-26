// The sedans, finished (brief 070, Vela; Glyph on the briefing's getaway still: "the car model has no detail and looks like a stand-in"). Adopted 2026-10-27 (Sky: "fine for now - I'll improve
// later"), swapped in place: Sedan_Getaway and Sedan_Official are built by cars070.sedan_v2 at full detail, to the old car to the centimetre (size, pivot, front: the sim's car footprint and the
// host's drop-in by path are unchanged), each in its own livery (its car enamels are the base rows' colours with the car detail pair), chrome and no brass or gold, nothing lit beyond the sedans'
// own lamps. The candidate Sedan_Getaway_v2 is retired (no use tag, status rejected).
import { expect, test } from 'bun:test';
import { existsSync, readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import type { Kit } from '../../contracts/world/kit.schema';
import { GEN_ROOT } from '../kit/store';

const kit: Kit = JSON.parse(readFileSync(resolve(import.meta.dir, '../../contracts/world/kit.json'), 'utf8'));
const piece = (id: string) => kit.assets.find(a => a.id === id)!;
type Row = { name: string; kind: string; color: number[]; emission: number; robotDetail?: { detail: string; detailNormal: string; tile: number[]; bump: number } };
const mats = new Map((JSON.parse(readFileSync(join(GEN_ROOT, 'decokit_materials.json'), 'utf8')).materials as Row[]).map(m => [m.name, m]));
// the old sedans' (cars020) catalogued size, pivot and triangles: the footprint the sim's car and the host's 1.8 x 1.4 x 4.6 m box were fitted to
const SIZE = [1.8142, 1.288, 4.7288], PIVOT_OFFSET = [0, 0.083], OLD_TRIS = 3408;
const CARS = { Sedan_Getaway: { use: 'vehicle:car-getaway', livery: [['DK_CarEnamel_Coral', 'DK_Accent_Coral'], ['DK_CarEnamel_Seafoam', 'DK_Enamel_Seafoam']] }, Sedan_Official: { use: 'vehicle:car-official', livery: [['DK_CarEnamel_Teal', 'DK_Enamel_Teal'], ['DK_CarEnamel_Ivory', 'DK_Enamel_Ivory']] } } as const;

for (const [id, car] of Object.entries(CARS)) {
  test(`${id} is the finished sedan, adopted in place: the old car's size, pivot and front, full detail (no LODs), files hash-checked`, () => {
    const v = piece(id);
    expect(v).toBeDefined(); expect(v.category).toBe('Vehicles'); expect(v.source).toBe('generated'); expect(v.status).toBe('adopted');
    expect(v.tags.use).toEqual([car.use]); expect(v.tags.scale).toEqual([1, 1]); expect(v.tags.sight).toBe('open');
    for (let i = 0; i < 3; i++) expect(Math.abs(v.size[i]! - SIZE[i]!), `size[${i}] (m)`).toBeLessThan(0.002);
    expect(v.pivot).toBe('origin-ground'); expect(v.front).toBe('+Z'); expect(v.pivotOffset ?? [0, 0]).toEqual(PIVOT_OFFSET);
    expect(v.lods, 'full detail: no LODs').toBeUndefined(); expect(v.files.filter(f => f.role === 'lod')).toEqual([]);
    expect(v.files.find(f => f.role === 'fbx')?.path, 'the host loads the car by this path').toBe(`Models/Vehicles/DK_${id}.fbx`);
    expect(v.tris, 'character over triangles').toBeGreaterThan(OLD_TRIS);
    for (const f of v.files) { const p = join(GEN_ROOT, f.path); if (f.root === 'generated') { expect(existsSync(p), f.path).toBe(true); expect(new Bun.CryptoHasher('sha256').update(readFileSync(p)).digest('hex'), f.path).toBe(f.sha256); } }
    for (const t of ['Textures/car_enamel_detail_v1.png', 'Textures/car_enamel_detail_v1_normal.png']) expect(v.files.some(f => f.path === t), `${t} travels with it`).toBe(true);
  });

  test(`${id}'s livery is its own: the car enamels are its base rows with the car detail pair; chrome, no brass or gold, nothing lit beyond the sedans' lamps`, () => {
    const v = piece(id);
    for (const [c0, base] of car.livery) {
      expect(v.materials).toContain(c0);
      const c = mats.get(c0)!, b = mats.get(base)!;
      expect(c.color).toEqual(b.color); expect(c.kind).toBe(b.kind);
      expect(c.robotDetail?.detail).toBe('car_enamel_detail_v1.png'); expect(c.robotDetail?.detailNormal).toBe('car_enamel_detail_v1_normal.png');
    }
    expect(v.materials).toContain('DK_Chrome');
    expect(v.materials.filter(m => /gold|brass/i.test(m))).toEqual([]);
    expect(v.materials.filter(m => (mats.get(m)?.emission ?? 0) > 0).sort()).toEqual(['DK_Beacon_Rose', 'DK_Lamp'].filter(m => (mats.get(m)?.emission ?? 0) > 0).sort());
  });
}

test('the two cars differ only in livery: same triangles, same chassis; neither shares the other\'s paint', () => {
  const g = piece('Sedan_Getaway'), o = piece('Sedan_Official');
  expect(o.tris).toBe(g.tris); expect(o.size).toEqual(g.size);
  const paint = (id: keyof typeof CARS) => CARS[id].livery.map(([c]) => c);
  for (const m of paint('Sedan_Official')) expect(g.materials).not.toContain(m);
  for (const m of paint('Sedan_Getaway')) expect(o.materials).not.toContain(m);
});

test('Sedan_Getaway_v2, the candidate, is retired: no use tag, status rejected, never copied', () => {
  const v = piece('Sedan_Getaway_v2');
  expect(v.status).toBe('rejected'); expect(v.tags.use).toEqual([]);
});

test('the robot enamels keep their own detail pair (the car pair is added beside it, not over it)', () => {
  const r = mats.get('DK_RobotEnamel_Cream')!;
  expect(r.robotDetail?.detail).toBe('robot_enamel_detail_v1.png'); expect(r.robotDetail?.tile).toEqual([0.5, 2.0]);
});
