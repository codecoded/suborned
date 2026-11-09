// Engine 0.49.0's collide law (brief 098: the solids are the pieces as built; tools/decokit/collide_law.ts), without Blender: from the records, the declaring
// tables and the rule, a run never turns a measured piece back to declared, nor changes a declared piece's volumes, unless the piece's mesh or its builder's
// declaration changed. Brief 103 found a plain `bun run collide` doing both to 43 pieces from brief 097 pins an older export had left on the manifest.
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import type { KitAsset } from '../../contracts/world/kit.schema';
import { kitAssets } from '../geometry/pieces';
import { GEN_ROOT } from '../kit/store';
import { COLLIDE_VERSION, collideText, planHere, readCollide, type CollideFile, type PieceCollide, type Volume } from './collide';
import { declarationOf, dsha, isPin, planCollide, readPins, readRows, type Pins, type Row, type Rows } from './collide_law';

const box = (x: number, y: number, z: number): Volume => ({ shape: 'box', min: [-x, 0, -z], max: [x, y, z] });
const PIN = [box(0.9, 0.5, 0.3), box(0.2, 0.9, 0.2)], OWN = [box(0.8, 0.45, 0.25)];
// collide097.py's ITEM box for PolicePost as an export before 0.49.0 wrote it (1.0 x 1.0 x 2.8 street item over s = 1.007)
const ITEM_BOX: Volume = { shape: 'box', min: [-0.496524329692155, 0, -0.496524329692155], max: [0.496524329692155, 2.7805362462760677, 0.496524329692155] };
const pins = (pinned = false): Pins => ({ pinned, as: { Bench: PIN }, item: { PolicePost: [[1.0, 1.0, 2.8], [0.9, 1.1]] } });
const asset = (id: string, root?: 'generated' | 'extra', extra?: string): KitAsset => ({ id, status: 'adopted', ...(extra ? { extra } : {}), files: [{ role: 'fbx', ...(root ? { root } : {}), path: `Models/Street/DK_${id}.fbx`, sha256: '', bytes: 0 }] } as unknown as KitAsset);
const rows = (gen: Row[], extra: Record<string, Row[]> = {}): Rows => ({ gen: new Map(gen.map(r => [r.name, r])), extra: new Map(Object.entries(extra).map(([d, rs]) => [d, new Map(rs.map(r => [r.name, r]))])) });
const measured = (sha: string): PieceCollide => ({ sha256: sha, source: 'measured', class: 'solid', bounds: [[-1, 0, -1], [1, 1, 1]], tris: 10, cell: 0.25, volumes: [box(0.5, 0.5, 0.5)] });
const declared = (sha: string, v: Volume[]): PieceCollide => ({ sha256: sha, source: 'declared', declared: dsha(v), class: 'solid', bounds: [[-1, 0, -1], [1, 1, 1]], tris: 10, cell: 0.25, volumes: v });
const file = (pieces: Record<string, PieceCollide>): CollideFile => ({ schema: 'suborned.kit.collide/1', version: COLLIDE_VERSION, frame: '', rule: '', pieces });
/** A plain run over one piece: its record, its manifest row, its mesh hash now. */
const plan = (a: KitAsset, rec: PieceCollide | undefined, r: Rows, sha: string, o: { only?: string[]; force?: boolean; p?: Pins } = {}) =>
  planCollide({ assets: [a], prior: file(rec ? { [a.id]: rec } : {}), version: COLLIDE_VERSION, decl: x => declarationOf(x, r, o.p ?? pins()), fbx: () => '/k.fbx', sha: () => sha, ...o });

describe('who declares a piece (the law)', () => {
  test('a pin is collide097.json\'s volumes or collide097.py\'s ITEM box, nothing else', () => {
    expect(isPin('Bench', PIN, pins())).toBe(true);
    expect(isPin('Bench', OWN, pins())).toBe(false);
    expect(isPin('PolicePost', [ITEM_BOX], pins())).toBe(true);
    expect(isPin('PolicePost', [box(0.45, 2.7, 0.45)], pins())).toBe(false); // not the street item's proportions
    expect(isPin('Hydrant', PIN, pins())).toBe(false); // pins are per piece
  });
  test('the builder\'s row declares; a row holding a pin, or a generated piece with no row, says nothing; no builder here means measured', () => {
    const G = asset('Bench', 'generated');
    expect(declarationOf(G, rows([{ name: 'Bench', collide: OWN }]), pins())).toEqual({ by: 'builder', volumes: OWN });
    expect(declarationOf(G, rows([{ name: 'Bench', collide: PIN }]), pins())).toEqual({ by: 'unknown', why: 'pin' });
    expect(declarationOf(asset('PolicePost', 'generated'), rows([{ name: 'PolicePost', collide: [ITEM_BOX] }]), pins())).toEqual({ by: 'unknown', why: 'pin' });
    expect(declarationOf(G, rows([{ name: 'Bench', collide: PIN, collide_by: 'builder' }]), pins())).toEqual({ by: 'builder', volumes: PIN }); // its own volumes equal its old pin
    expect(declarationOf(G, rows([{ name: 'Bench', collide: OWN, collide_by: '097' }]), pins())).toEqual({ by: 'unknown', why: 'pin' });
    expect(declarationOf(G, rows([{ name: 'Bench' }]), pins())).toEqual({ by: 'none' });
    expect(declarationOf(G, rows([{ name: 'Bench', collide: [] }]), pins())).toEqual({ by: 'none' });
    expect(declarationOf(G, rows([]), pins())).toEqual({ by: 'unknown', why: 'no row' });
    expect(declarationOf(G, { extra: new Map() }, pins())).toEqual({ by: 'unknown', why: 'no row' }); // no manifest at all
    expect(declarationOf(asset('Bench'), rows([]), pins())).toEqual({ by: 'none' }); // the old DecoKit's or the store's piece
  });
  test('an extra root declares, an empty list declares none, and its stale pin says nothing', () => {
    const X = asset('Bench', 'extra', 'out/x');
    expect(declarationOf(X, rows([], { 'out/x': [{ name: 'Bench', collide: [] }] }), pins())).toEqual({ by: 'extra', volumes: [] });
    expect(declarationOf(X, rows([], { 'out/x': [{ name: 'Bench', collide: OWN }] }), pins())).toEqual({ by: 'extra', volumes: OWN });
    expect(declarationOf(X, rows([], { 'out/x': [{ name: 'Bench', collide: PIN }] }), pins())).toEqual({ by: 'unknown', why: 'pin' });
    expect(declarationOf(X, rows([], { 'out/x': [{ name: 'Bench' }] }), pins())).toEqual({ by: 'none' });
  });
  test('with the 097 rule on (collide097.py PINNED), a pin declares', () => {
    expect(declarationOf(asset('Bench', 'generated'), rows([{ name: 'Bench', collide: PIN }]), pins(true))).toEqual({ by: 'builder', volumes: PIN });
  });
});

describe('what a run keeps (the brief 103 regression)', () => {
  const G = asset('Bench', 'generated'), stale = rows([{ name: 'Bench', collide: PIN }]);
  test('a pin left on the row never turns a measured piece back to declared', () => {
    const rec = measured('m1'), p = plan(G, rec, stale, 'm1');
    expect(p.todo).toEqual([]); expect(p.pieces.Bench).toBe(rec); expect(p.unread).toEqual([{ id: 'Bench', why: 'pin' }]);
  });
  test('nor replaces a builder\'s declared volumes', () => {
    const rec = declared('m1', OWN), p = plan(G, rec, stale, 'm1');
    expect(p.todo).toEqual([]); expect(p.pieces.Bench).toBe(rec);
  });
  test('nor does a missing manifest: the records stand (a tree with no out/decokit)', () => {
    const rec = declared('m1', OWN), p = plan(G, rec, { extra: new Map() }, 'm1');
    expect(p.todo).toEqual([]); expect(p.pieces.Bench).toBe(rec);
  });
  test('a builder that changed its declaration re-declares on the same mesh: its source changed', () => {
    const p = plan(G, declared('m1', OWN), rows([{ name: 'Bench', collide: [box(1, 1, 1)], collide_by: 'builder' }]), 'm1');
    expect(p.todo.map(t => [t.id, t.why, t.declare?.hash])).toEqual([['Bench', 'declaration', dsha([box(1, 1, 1)])]]);
  });
  test('a builder whose volumes are its old pin still declares when its mesh changes (the Office entrances)', () => {
    const p = plan(G, declared('m1', PIN), rows([{ name: 'Bench', collide: PIN, collide_by: 'builder' }]), 'm2');
    expect(p.todo.map(t => [t.why, t.declare?.hash])).toEqual([['mesh', dsha(PIN)]]);
  });
  test('a mesh change measures a piece no builder declares', () => {
    const p = plan(G, measured('m1'), rows([{ name: 'Bench' }]), 'm2');
    expect(p.todo.map(t => [t.why, t.declare])).toEqual([['mesh', undefined]]);
  });
  test('a pin on the row with a new mesh is an error, never a guess', () => {
    const p = plan(G, measured('m1'), stale, 'm2');
    expect(p.todo).toEqual([]); expect(p.errors.map(e => e.id)).toEqual(['Bench']); expect(p.errors[0]!.why).toContain('bun run decokit --only Bench');
  });
  test('--only and --force re-measure on the record\'s mesh with the record\'s declaration', () => {
    for (const o of [{ only: ['Bench'] }, { force: true }]) {
      expect(plan(G, declared('m1', OWN), stale, 'm1', o).todo.map(t => t.declare?.hash)).toEqual([dsha(OWN)]);
      expect(plan(G, measured('m1'), stale, 'm1', o).todo.map(t => t.declare)).toEqual([undefined]);
    }
  });
});

describe('this tree, without Blender: a plain run changes only what its sources changed', () => {
  const prior = readCollide()!, p = planHere(), assets = kitAssets(), rowsHere = readRows(assets.values(), GEN_ROOT), pinsHere = readPins();
  /** A redo a source asks for: a new piece, a changed mesh, or a declaration changed by its builder's own row (named by export_kit) or its extra root's (not a 097 pin). */
  const sourced = (t: (typeof p.todo)[number]) => {
    const a = assets.get(t.id)!;
    if (t.why !== 'declaration') return true;
    if (a.extra) { const v = rowsHere.extra.get(a.extra)?.get(t.id)?.collide; return !!v && !isPin(t.id, v, pinsHere); }
    return rowsHere.gen?.get(t.id)?.collide_by === 'builder';
  };
  test('no measured piece becomes declared, and no declared piece\'s volumes change, on an unchanged mesh with an unchanged builder', () => {
    expect(p.todo.filter(t => !sourced(t)).map(t => `${t.id}: ${(prior.pieces[t.id] as { source?: string })?.source} -> ${t.declare ? 'declared' : 'measured'}`)).toEqual([]);
  });
  test('it writes the committed file back, byte for byte, apart from what its sources changed', () => {
    const redo = new Set(p.todo.map(t => t.id)), pick = (r: Record<string, PieceCollide>) => Object.fromEntries(Object.entries(r).filter(([id]) => assets.has(id) && !redo.has(id)));
    expect(p.errors).toEqual([]);
    expect(collideText(pick(p.pieces))).toBe(collideText(pick(prior.pieces)));
  });
  test('a declared record keeps the hash of its own volumes (a hand-edited volume fails here)', () => {
    expect(Object.entries(prior.pieces).filter(([, r]) => 'source' in r && r.source === 'declared' && r.declared !== dsha(r.volumes)).map(([id]) => id)).toEqual([]);
  });
  test('the 097 rule is off (engine 0.49.0, Sky 2026-11-08: the solids are the pieces as built), read from collide097.py itself', () => {
    const pins = readPins();
    expect(pins.pinned).toBe(false);
    expect(Object.keys(pins.item).sort()).toEqual(['CivicRobotDock', 'Dumpster_Civic', 'MastCamera', 'PolicePost']);
    expect(isPin('PolicePost', [ITEM_BOX], pins)).toBe(true);
  });
  test('export_kit names who declared a row', () => {
    const src = readFileSync(join(import.meta.dir, 'export_kit.py'), 'utf8');
    expect(src).toContain('row["collide_by"] = "builder"');
    expect(src).toContain('row["collide_by"] = "097"');
  });
});
