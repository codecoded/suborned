// The collide law of engine 0.49.0 (brief 098, Sky 2026-11-08: "what is walkable is what you see"; the solids are the pieces as built), for tools/decokit/collide.ts:
// who declares a piece's solids, read from the tables on disk, and which pieces a run keeps, measures or declares. No Blender here.
//   builder  its builder declares them (geo.Shape.solid): export_kit writes them as the piece's manifest row `collide` (out/decokit, or $DECOKIT_OUT)
//   extra    its extra root's row declares them (brief 077, tools/kit/extra.ts); there an empty list declares none (render-only planting)
//   none     nobody does: measured from its mesh (collide.py). So is every piece no builder here makes (the old DecoKit's and the store's).
// Brief 097's pins never declare (Keel lifted the 097 rule for 0.49.0: tools/decokit/collide097.py PINNED off): collide097.json's volumes of the piece a 097
// piece replaced, and collide097.py's ITEM boxes. Before 0.49.0 export_kit wrote the pin on the row in place of the builder's volumes, and a partial export
// keeps every row it does not build (tables.py, brief 072-C), so a row can hold a pin long after the rule went. Such a row, like a generated piece's missing
// row, says nothing about the builder: the piece's record stands for its mesh. export_kit now names a row's source (`collide_by`), so a builder whose own
// volumes equal its old pin (the Office entrances) still declares them.
import { existsSync, readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import type { KitAsset } from '../../contracts/world/kit.schema';
import { extraId, readExtra, REPO } from '../kit/extra';
import type { CollideFile, PieceCollide, Volume } from './collide';

export type Decl = { by: 'builder' | 'extra'; volumes: Volume[] } | { by: 'none' } | { by: 'unknown'; why: 'pin' | 'no row' };
export type Row = { name: string; collide?: Volume[]; collide_by?: string };
/** The tables that declare: the generator's manifest by name (undefined when absent) and each extra root's rows by catalogue id. */
export type Rows = { gen?: Map<string, Row>; extra: Map<string, Map<string, Row>> };
/** Brief 097's pins: collide097.json's volumes by id; collide097.py's ITEM (the street item's size and the tag scale range) and PINNED (the 097 rule on). */
export type Pins = { pinned: boolean; as: Record<string, unknown[]>; item: Record<string, [[number, number, number], [number, number]]> };
export type Todo = { id: string; fbx: string; sha: string; declare?: { volumes: Volume[]; hash: string }; why: 'new' | 'mesh' | 'declaration' | 'only' | 'all' };
export type Plan = { pieces: Record<string, PieceCollide>; todo: Todo[]; errors: { id: string; why: string }[]; unread: { id: string; why: 'pin' | 'no row' }[] };

/** The hash a declared record keeps of its volumes (`declared`). */
export const dsha = (v: unknown): string => new Bun.CryptoHasher('sha256').update(JSON.stringify(v)).digest('hex');

/** The pins, read from collide097.json and collide097.py itself (python3, no Blender), so the 097 rule has one source. */
export function readPins(dir = import.meta.dir): Pins {
  const as = Object.fromEntries(Object.entries(JSON.parse(readFileSync(join(dir, 'collide097.json'), 'utf8')).pieces as Record<string, { volumes: unknown[] }>).map(([k, v]) => [k, v.volumes]));
  const p = Bun.spawnSync(['python3', '-I', '-c', `import json, sys; sys.path.insert(0, ${JSON.stringify(dir)}); import collide097 as c; print(json.dumps(dict(pinned=c.PINNED, item=c.ITEM)))`], { stdout: 'pipe', stderr: 'pipe' });
  if (p.exitCode !== 0) throw new Error(`collide: reading tools/decokit/collide097.py failed\n${p.stderr.toString().slice(-400)}`);
  const { pinned, item } = JSON.parse(p.stdout.toString()) as Omit<Pins, 'as'>;
  return { pinned, as, item };
}

/** Is `v` brief 097's pin for `id`: its collide097.json volumes, or its ITEM box (the street item's box over the fit scale s, collide097.volumes; s inside the tag range). */
export function isPin(id: string, v: unknown[], pins: Pins): boolean {
  const as = pins.as[id];
  if (as && JSON.stringify(as) === JSON.stringify(v)) return true;
  const it = pins.item[id], b = v[0] as Volume | undefined;
  if (!it || v.length !== 1 || b?.shape !== 'box' || b.min[1] !== 0 || !(b.max[1] > 0)) return false;
  const [[along, across, h], [lo, hi]] = it, s = h / b.max[1], near = (x: number, y: number) => Math.abs(x - y) <= 1e-6;
  return s >= lo - 1e-9 && s <= hi + 1e-9 && near(b.min[0], -b.max[0]) && near(b.min[2], -b.max[2]) && near(2 * b.max[0] * s, along) && near(2 * b.max[2] * s, across);
}

/** Read the declaring tables: the generator's manifest under genRoot, and the extra roots the catalogue names (asset.extra). */
export function readRows(assets: Iterable<KitAsset>, genRoot: string): Rows {
  const f = join(genRoot, 'decokit_manifest.json');
  const gen = existsSync(f) ? new Map((JSON.parse(readFileSync(f, 'utf8')).assets as Row[]).map(r => [r.name, r])) : undefined;
  const extra = new Map<string, Map<string, Row>>();
  for (const a of assets) {
    if (!a.extra || extra.has(a.extra)) continue;
    const dir = resolve(REPO, a.extra);
    if (existsSync(join(dir, 'decokit_manifest.json'))) extra.set(a.extra, new Map(readExtra(dir).map(r => [extraId(r), r as unknown as Row])));
  }
  return { gen, extra };
}

/** Who declares a piece's solids, by the law above. */
export function declarationOf(a: KitAsset, rows: Rows, pins: Pins): Decl {
  const generated = a.files.some(f => f.role === 'fbx' && f.root === 'generated');
  const row = a.extra ? rows.extra.get(a.extra)?.get(a.id) : rows.gen?.get(a.id);
  if (!row) return a.extra || generated ? { by: 'unknown', why: 'no row' } : { by: 'none' }; // no builder here makes it: measured
  const v = row.collide;
  if (!Array.isArray(v) || (!a.extra && !v.length)) return { by: 'none' }; // an empty list declares none only on an extra root (brief 077)
  if (row.collide_by === 'builder') return { by: 'builder', volumes: v };
  if (!pins.pinned && (row.collide_by === '097' || isPin(a.id, v, pins))) return { by: 'unknown', why: 'pin' };
  return { by: a.extra ? 'extra' : 'builder', volumes: v };
}

const why = (a: KitAsset, d: Extract<Decl, { by: 'unknown' }>) => (d.why === 'pin'
  ? 'its manifest row still holds brief 097\'s pin (an export before engine 0.49.0), and its mesh has no record to say who declares it'
  : `no manifest row says who declares its solids (${a.extra ?? 'the generator\'s output'}), and its mesh has no record`) + `: re-export it so its row is current (bun run decokit --only ${a.id}, or its extra root's script)`;

/**
 * Which pieces a run keeps, measures (`declare` absent) or declares, from the prior file and each piece's mesh hash. A piece is redone when its mesh changed, its
 * declaration changed, `only` names it, or the prior file is of another version (or `force`). A piece whose declaration is unknown keeps its record while its
 * mesh is the record's; redone on that mesh, it keeps the record's declaration; on another mesh it is an error, never a guess.
 */
export function planCollide(o: { assets: KitAsset[]; prior?: CollideFile; version: number; decl: (a: KitAsset) => Decl; fbx: (a: KitAsset) => string | undefined; sha: (f: string) => string; only?: string[]; force?: boolean }): Plan {
  const keep = o.prior && o.prior.version === o.version && !o.force ? o.prior.pieces : {}, was = o.prior?.pieces ?? {};
  const plan: Plan = { pieces: {}, todo: [], errors: [], unread: [] };
  for (const a of [...o.assets].sort((x, y) => (x.id < y.id ? -1 : 1))) {
    const k = keep[a.id];
    if (a.status === 'rejected' || (o.only && !o.only.includes(a.id))) { if (k) plan.pieces[a.id] = k; continue; }
    const f = o.fbx(a);
    if (!f) { if (k) plan.pieces[a.id] = k; continue; } // a candidate whose mesh is not on this machine keeps its entry (brief 079)
    const s = o.sha(f), d = o.decl(a), redo = !!o.only?.includes(a.id), r = was[a.id];
    let want: Todo['declare'];
    if (d.by === 'unknown') {
      plan.unread.push({ id: a.id, why: d.why });
      if (k && !redo && k.sha256 === s) { plan.pieces[a.id] = k; continue; } // the record stands for its mesh
      if (!r || !('volumes' in r) || r.sha256 !== s) { plan.errors.push({ id: a.id, why: why(a, d) }); continue; }
      want = r.source === 'declared' ? { volumes: r.volumes, hash: r.declared ?? dsha(r.volumes) } : undefined;
    } else want = d.by === 'none' ? undefined : { volumes: d.volumes, hash: dsha(d.volumes) };
    if (k && !redo && k.sha256 === s && ('declared' in k ? k.declared : undefined) === want?.hash) { plan.pieces[a.id] = k; continue; }
    plan.todo.push({ id: a.id, fbx: f, sha: s, ...(want ? { declare: want } : {}), why: !r ? 'new' : redo ? 'only' : !k ? 'all' : k.sha256 !== s ? 'mesh' : 'declaration' });
  }
  return plan;
}
