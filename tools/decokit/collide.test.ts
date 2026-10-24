// The kit's collision volumes (brief 055, Vela): kit.collide.json is current for the shipped meshes and its volumes are well formed.
import { expect, test } from 'bun:test';
import { existsSync, readFileSync } from 'node:fs';
import { readCollide, type Volume } from './collide';
import { fbxOf, kitAssets } from '../geometry/pieces';

const file = readCollide();
const sha = (p: string) => new Bun.CryptoHasher('sha256').update(readFileSync(p)).digest('hex');

test('every catalogued piece with a mesh has volumes for its current mesh', () => {
  expect(file).toBeDefined();
  const stale: string[] = [];
  for (const a of kitAssets().values()) { if (a.status === 'rejected') continue; const f = fbxOf(a); if (!f) continue; if (file!.pieces[a.id]?.sha256 !== sha(f)) stale.push(a.id); }
  expect(stale).toEqual([]);
});

test('volumes are boxes or cylinders in the kit frame, inside the piece bounds (to 10 cm), none empty', () => {
  for (const [id, p] of Object.entries(file!.pieces)) {
    expect('error' in p ? p.error : undefined, id).toBeUndefined();
    if (!('volumes' in p)) continue;
    const [lo, hi] = p.bounds;
    for (const v of p.volumes as Volume[]) {
      const [mn, mx] = v.shape === 'box' ? [v.min, v.max] : [[v.c[0] - v.r, v.y0, v.c[1] - v.r], [v.c[0] + v.r, v.y1, v.c[1] + v.r]];
      for (let k = 0; k < 3; k++) { expect(mx[k]! - mn[k]!, `${id} ${k}`).toBeGreaterThanOrEqual(0); expect(mn[k]!, id).toBeGreaterThanOrEqual(lo[k]! - 0.15); expect(mx[k]!, id).toBeLessThanOrEqual(hi[k]! + 0.15); }
    }
  }
});

test('flat paving lies on its pivot: no volumes and a mesh at height 0 (K-1)', () => {
  for (const id of ['Pave_Slab', 'Pave_Flags_0', 'Pave_Flags_3', 'InlayArc_R6', 'PaveInlay_Ring_Q']) { const p = file!.pieces[id]; expect(p && 'bounds' in p ? p.bounds[0][1] : NaN, id).toBeCloseTo(0, 2); }
});

test('the Office stair is declared from the builder: a landing at 0.75 and five treads that fall 0.125 each, all to stand on', () => {
  const p = file!.pieces.RecordsEntrance!; expect('source' in p ? p.source : '').toBe('declared');
  const stand = ('volumes' in p ? p.volumes : []).filter(v => v.shape === 'box' && v.stand).map(v => (v as Extract<Volume, { shape: 'box' }>).max[1]).sort((a, b) => b - a);
  expect(stand).toEqual([0.75, 0.625, 0.5, 0.375, 0.25, 0.125]);
});

test('feet: every placed street piece that stands on the ground has its lowest volume at 0', () => {
  for (const id of ['RobotButler', 'RobotButler_Play', 'ShrubMound', 'WindowBox', 'TreePit_Pearl']) { const p = file!.pieces[id]!; expect('bounds' in p ? Math.abs(p.bounds[0][1]) : 9, id).toBeLessThan(0.03); }
});

test('the café\'s curved glazed front is solid along its whole run: a chain of boxes no longer than 0.5 m, wall height, under every point of the drawn glass', () => {
  const p = file!.pieces.CafeBay_v2!; expect('volumes' in p).toBe(true);
  const vols = ('volumes' in p ? p.volumes : []).filter((v): v is Extract<Volume, { shape: 'box' }> => v.shape === 'box');
  const HF = 5.7, R = 5.3, NA = 6, GLASS = 0.14; // civic_common.front_path(6, 4) and civic_cafe.GLASS_A: the glass polyline, outward 0.14 m, builder y -> kit z = -y
  const pts = Array.from({ length: NA + 1 }, (_, i) => { const a = (270 + 90 * i / NA) * Math.PI / 180; return [HF + R * Math.cos(a), R + R * Math.sin(a)] as const; });
  let miss = 0, n = 0;
  for (let i = 0; i < NA; i++) {
    const [ax, ay] = pts[i]!, [bx, by] = pts[i + 1]!, L = Math.hypot(bx - ax, by - ay), nx = (by - ay) / L, ny = -(bx - ax) / L;
    for (let k = 0; k <= 100; k++) for (const y of [0.2, 1.0, 1.6, 2.5, 3.5, 4.5]) {
      const x = ax + (bx - ax) * k / 100 + nx * GLASS, z = -(ay + (by - ay) * k / 100 + ny * GLASS); n++;
      if (!vols.some(v => [x, -x].some(xx => xx >= v.min[0] - 0.005 && xx <= v.max[0] + 0.005) && z >= v.min[2] - 0.005 && z <= v.max[2] + 0.005 && y >= v.min[1] && y <= v.max[1])) miss++;
    }
  }
  expect(n).toBeGreaterThan(1000); expect(miss).toBe(0);
  const chain = (p as { chain?: number }).chain ?? 0; expect(chain).toBeGreaterThanOrEqual(40);
  for (const v of vols.slice(-chain)) { expect(v.max[0] - v.min[0]).toBeLessThanOrEqual(0.52); expect(v.max[2] - v.min[2]).toBeLessThanOrEqual(0.52); }
});
