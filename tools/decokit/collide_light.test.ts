// Light has no body (brief 107, Vela's ask; tools/decokit/collide_light.ts): the materials catalogue's kind glow is left out of every measure of solids, a record names
// what it was measured without, and a piece whose light set changes is measured again. Without Blender.
import { describe, expect, test } from 'bun:test';
import { mkdtempSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import type { KitAsset } from '../../contracts/world/kit.schema';
import { kitAssets, lightMaterials } from '../geometry/pieces';
import { COLLIDE_VERSION, planHere, readCollide, type CollideFile, type PieceCollide } from './collide';
import { planCollide } from './collide_law';
import { jobArg, lightOf, readLight, sameLight } from './collide_light';

const cat = (rows: { name: string; kind: string }[]) => { const d = mkdtempSync(join(tmpdir(), 'light-')); writeFileSync(join(d, 'decokit_materials.json'), JSON.stringify({ materials: rows })); return d; };
const asset = (id: string, materials: string[]): KitAsset => ({ id, status: 'adopted', materials, files: [{ role: 'fbx', path: `Models/DK_${id}.fbx`, sha256: '', bytes: 0 }] } as unknown as KitAsset);
const rec = (light?: string[]): PieceCollide => ({ sha256: 'm', source: 'measured', class: 'solid', bounds: [[-1, 0, -1], [1, 1, 1]], tris: 10, cell: 0.1, volumes: [], ...(light ? { light } : {}) });
const file = (pieces: Record<string, PieceCollide>): CollideFile => ({ schema: 'suborned.kit.collide/1', version: COLLIDE_VERSION, frame: '', rule: '', pieces });
const plan = (a: KitAsset, r: PieceCollide | undefined, light?: Set<string>) =>
  planCollide({ assets: [a], prior: file(r ? { [a.id]: r } : {}), version: COLLIDE_VERSION, decl: () => ({ by: 'none' }), fbx: () => '/k.fbx', sha: () => 'm', ...(light ? { light: (x: KitAsset) => lightOf(x.materials, light) } : {}) });

describe('the rule', () => {
  test('light-only is the catalogue\'s kind glow, from every catalogue found; a lit body (emit) is not light', () => {
    const a = cat([{ name: 'DK_Glow_PlanterFoot', kind: 'glow' }, { name: 'DK_Lamp', kind: 'emit' }, { name: 'DK_Limestone', kind: 'stone' }]), b = cat([{ name: 'DK_Road_Pool', kind: 'glow' }]);
    expect([...readLight([a, b, join(a, 'none')])!].sort()).toEqual(['DK_Glow_PlanterFoot', 'DK_Road_Pool']);
    expect(readLight([join(a, 'none')])).toBeUndefined(); // no catalogue: nothing is guessed
  });
  test('a piece\'s light is the light materials it carries, sorted; the Blender job names them after the mesh', () => {
    const L = new Set(['DK_Glow_PlanterFoot', 'DK_Road_Pool']);
    expect(lightOf(['DK_Limestone', 'DK_Road_Pool', 'DK_Glow_PlanterFoot'], L)).toEqual(['DK_Glow_PlanterFoot', 'DK_Road_Pool']);
    expect(jobArg('P', '/p.fbx', ['DK_Glow_PlanterFoot', 'DK_Road_Pool'])).toBe('P=/p.fbx|DK_Glow_PlanterFoot,DK_Road_Pool');
    expect(jobArg('P', '/p.fbx', [])).toBe('P=/p.fbx');
    expect(sameLight(undefined, [])).toBe(true); expect(sameLight(['a'], undefined)).toBe(false);
  });
});

describe('which pieces a run measures again', () => {
  const L = new Set(['DK_Glow_PlanterFoot']), P = asset('Planter', ['DK_Limestone', 'DK_Glow_PlanterFoot']), S = asset('Stone', ['DK_Limestone']);
  test('a record measured with its glow as a body is measured again, without it', () => {
    expect(plan(P, rec(), L).todo.map(t => [t.id, t.why, t.light])).toEqual([['Planter', 'light', ['DK_Glow_PlanterFoot']]]);
  });
  test('a record measured without its light, or a piece with none, is kept', () => {
    expect(plan(P, rec(['DK_Glow_PlanterFoot']), L).todo).toEqual([]);
    expect(plan(S, rec(), L).todo).toEqual([]);
  });
  test('a material that stops being light is measured as a body again; with no catalogue, each record\'s own list stands', () => {
    expect(plan(P, rec(['DK_Glow_PlanterFoot']), new Set()).todo.map(t => [t.why, t.light])).toEqual([['light', []]]);
    expect(plan(P, rec(['DK_Glow_PlanterFoot'])).todo).toEqual([]);
  });
});

describe('this tree', () => {
  const c = readCollide()!, assets = kitAssets();
  test('no record is waiting on its light (bun run collide is current for the rule)', () => {
    expect(planHere().todo.filter(t => t.why === 'light').map(t => t.id)).toEqual([]);
  });
  test('brief 107\'s planter feet and inlay edges are light: the planters\' and the terrace\'s bounds are their stone, not their glow', () => {
    for (const id of ['PlanterRun_4_A_TB', 'PlanterRun_2_A_TB', 'PlanterArc_TB', 'CafeTerrace_TB', 'InlayArc_R4_W06']) {
      const p = c.pieces[id], a = assets.get(id)!;
      expect(p && 'light' in p ? p.light : undefined, id).toBeDefined();
      const b = p && 'bounds' in p ? p.bounds : undefined;
      for (const k of [0, 2]) expect(Math.abs(b![1][k]! - b![0][k]! - a.size[k]!), `${id} axis ${k}`).toBeLessThan(0.05); // the catalogue binds the thing (kit.asset footprint=)
    }
  });
  test('the geometry audit reads the same lists from kit.collide.json', () => {
    const m = lightMaterials();
    for (const [id, p] of Object.entries(c.pieces)) if ('light' in p && p.light?.length) expect(m.get(id), id).toEqual(p.light);
  });
});
