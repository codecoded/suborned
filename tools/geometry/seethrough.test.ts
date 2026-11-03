// Brief 077 (Splice; Keel's finding, briefs/077/READY-keel; Sky 2026-10-30, planting is see-through): the geometry audit stamps no render-only piece (kit.collide.json
// `volumes: []`), and measures a piece that mixes stone and planting without its see-through materials (kit.json `seeThrough`), so G-3a counts only what stops sight.
import { describe, expect, test } from 'bun:test';
import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { World } from '../../contracts/world/world.schema';
import { BLENDER, PIECE_DIR, ROOT, loadPieces, seeThroughMaterials, type PieceData } from './pieces';
import { buildVisible, renderOnlyPieces } from './visible';

/** A 2 x 2 m piece, pivot at its centre, solid from 0 to 1.9 m in every 0.25 m column. */
const tall: PieceData = { id: 'Tall', cell: 0.25, x0: -1, x1: 1, z0: -1, z1: 1, bx0: -1, bx1: 1, bz0: -1, bz1: 1, nx: 8, nz: 8, minY: 0, maxY: 1.9, tris: 12, cells: Array.from({ length: 64 }, () => [0, 1.9]), sha: '', fbx: '' };
const world = (id: string) => ({ ground: { plots: [], roads: [], foot: [], flats: [], footways: [], drives: [], pieces: [] }, buildings: [], facades: [], frontages: [],
  street: [{ id: 'street/x', kind: 'planter', at: [8, 5], yaw: 0, z: 0, size: [2, 2, 1.9], kit: { id, scale: [1, 1, 1], yaw: 0 }, sight: 'blocks' }],
  civic: [{ id: 'civic/x', surface: 'trim', ring: [[2, 4], [4, 4], [4, 6], [2, 6]], base: 0, height: 1.9, source: 'kit', kit: { id, scale: [1, 1, 1], yaw: 0, at: [3, 5], base: 0 } }] }) as unknown as World;
const win = { x0: 0, y0: 0, x1: 12, y1: 10 };
const eye = (R: ReturnType<typeof buildVisible>['R'], p: [number, number]) => R.solidRel(...R.cellOf(p), 1.6);

describe('render-only pieces are not stamped (visible.ts)', () => {
  test('a piece declared render-only stands nothing at eye height, as a civic mass or a street item; it is still placed and K-1 checked', () => {
    const v = buildVisible(world('Tall'), new Map([['Tall', tall]]), win, 0, new Set(['Tall']));
    expect(eye(v.R, [3, 5])).toBeNull(); expect(eye(v.R, [8, 5])).toBeNull();
    expect(v.meta.get('civic/x')?.renderOnly).toBe(true); expect(v.meta.get('street/x')?.renderOnly).toBe(true);
    expect(v.checks.map(c => c.id)).toEqual(['civic/x']); expect(v.unmeasured.size).toBe(0);
  });
  test('the control: the same piece not declared stands at eye height in both places', () => {
    const v = buildVisible(world('Tall'), new Map([['Tall', tall]]), win, 0, new Set());
    expect(eye(v.R, [3, 5])).toBe('civic/x'); expect(eye(v.R, [8, 5])).toBe('street/x');
  });
  test('render-only with no measured mesh stands no prism (never its footprint box)', () => {
    const v = buildVisible(world('Tall'), new Map(), win, 0, new Set(['Tall']));
    expect(eye(v.R, [3, 5])).toBeNull(); expect(eye(v.R, [8, 5])).toBeNull(); expect(v.unmeasured.size).toBe(0);
  });
  test('render-only is read from kit.collide.json: declared `volumes: []` only, never a name or a height', () => {
    const c = JSON.parse(readFileSync(resolve(ROOT, 'contracts/world/kit.collide.json'), 'utf8')).pieces as Record<string, { volumes?: unknown[] }>, ro = renderOnlyPieces();
    expect([...ro].sort()).toEqual(Object.keys(c).filter(k => Array.isArray(c[k]!.volumes) && c[k]!.volumes!.length === 0).sort());
    for (const id of ['CivicBedPlanting_office-west', 'CafeTerrace_Planting']) expect(ro.has(id), id).toBe(true);
    expect(ro.has('PlanterRun_4_A_TB')).toBe(false); // its trough blocks
  });
});

// The real piece (Blender, or the measurement cache): PlanterRun_4_A_TB's testbed planting reaches 1.67 m; measured without the see-through materials, its solid is the trough.
const canMeasure = existsSync(BLENDER) || existsSync(resolve(PIECE_DIR, 'PlanterRun_4_A_TB.json'));
describe.skipIf(!canMeasure)('mixed pieces are measured on their stonework (pieces.ts, pieces.py)', () => {
  test('kit.json declares the testbed leaf, petal and soil see-through', () => {
    expect([...seeThroughMaterials()].sort()).toEqual(['DK_Leaf_TB', 'DK_Petal_TB', 'DK_Soil_TB']);
  });
  test('PlanterRun_4_A_TB: the mesh still places it (maxY 1.67 m), but its solid tops out at the trough (0.65 m); the dome piece it rebuilds is unchanged', () => {
    const p = loadPieces(['PlanterRun_4_A_TB', 'PlanterRun_4_A']), tb = p.get('PlanterRun_4_A_TB')!, old = p.get('PlanterRun_4_A')!;
    const top = (d: PieceData) => Math.max(...d.cells.filter(c => c.length).map(c => Math.max(...c.filter((_, i) => i % 2 === 1))));
    expect(tb.maxY).toBeGreaterThan(1.6); expect(tb.minY).toBeCloseTo(0, 2);
    expect(top(tb)).toBeLessThanOrEqual(0.7);
    expect(top(old)).toBeGreaterThan(1.0); // the dome version keeps its planting solid: nothing in the adopted world moves
  });
});
