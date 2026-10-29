// Kit piece solids (brief 055, Splice): the occupied height intervals of every kit piece the site window uses, measured from the shipped mesh by Blender (pieces.py).
// Cached by the FBX's sha256 under out/scratch/splice/geometry/pieces; a piece is measured once and again only when its mesh changes.
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { KitAsset } from '../../contracts/world/kit.schema';

export const ROOT = resolve(import.meta.dir, '../..');
export const BLENDER = process.env.BLENDER ?? '/Applications/Blender.app/Contents/MacOS/Blender';
export const PIECE_DIR = resolve(ROOT, 'out/scratch/splice/geometry/pieces');
export const MEASURE_CELL = 0.25;
/** Bump when pieces.py changes what it writes. (4 -> 4: brief 077's see-through argument leaves a piece without one byte-identical; a piece with one keys its cache by the list.) */
export const MEASURE_VERSION = 4;

export type PieceData = {
  id: string; cell: number; x0: number; x1: number; z0: number; z1: number; bx0: number; bx1: number; bz0: number; bz1: number; nx: number; nz: number; minY: number; maxY: number; tris: number;
  /** per cell (x-major), flat [lo, hi, lo, hi ...], metres from the pivot */
  cells: number[][];
  sha: string; fbx: string;
};

const sha = (b: Uint8Array) => new Bun.CryptoHasher('sha256').update(b).digest('hex');

/** Brief 077: kit.json `seeThrough`, the materials drawn but solid to nothing (planting, Sky 2026-10-30). */
export function seeThroughMaterials(): Set<string> {
  const k = JSON.parse(readFileSync(resolve(ROOT, 'contracts/world/kit.json'), 'utf8')) as { seeThrough?: { materials: string[] } };
  return new Set(k.seeThrough?.materials ?? []);
}
export function kitAssets(): Map<string, KitAsset> {
  const k = JSON.parse(readFileSync(resolve(ROOT, 'contracts/world/kit.json'), 'utf8')) as { assets: KitAsset[] };
  return new Map(k.assets.map(a => [a.id, a]));
}

/** The shipped FBX of a piece: the hash-checked copy the player builds from (Kit/), else the generator's output. */
export function fbxOf(a: KitAsset): string | undefined {
  const f = a.files.find(x => x.role === 'fbx'); if (!f) return undefined;
  if (f.root === 'extra' && a.extra) { const p = resolve(ROOT, a.extra, f.src ?? f.path); if (existsSync(p)) return p; } // brief 077: an extra root's piece (tools/kit/extra.ts), before its Kit/ copy
  for (const base of ['unity/Assets/Suborned/Kit', 'out/decokit']) { const p = resolve(ROOT, base, f.path); if (existsSync(p)) return p; }
  return undefined;
}

const cachePath = (id: string) => resolve(PIECE_DIR, `${id}.json`);

/** Measure what is not cached (one Blender run for all of it), then load every requested piece. A piece with no FBX (a procedural stand-in) is simply absent from the result. */
export function loadPieces(ids: Iterable<string>, log: (s: string) => void = () => {}): Map<string, PieceData> {
  const assets = kitAssets(), see = seeThroughMaterials(); mkdirSync(PIECE_DIR, { recursive: true });
  const skipOf = (a: KitAsset) => a.materials.filter(m => see.has(m)).sort(); // brief 077: the see-through materials this piece carries
  const want = [...new Set(ids)].sort(), todo: [string, string][] = [], out = new Map<string, PieceData>(), shas = new Map<string, string>();
  for (const id of want) {
    const a = assets.get(id); const f = a && fbxOf(a); if (!f) continue;
    const skip = skipOf(a), s = sha(readFileSync(f)) + ':' + MEASURE_VERSION + ':' + MEASURE_CELL + (skip.length ? ':see:' + skip.join(',') : ''); shas.set(id, s);
    const c = cachePath(id);
    if (existsSync(c)) { const d = JSON.parse(readFileSync(c, 'utf8')) as PieceData; if (d.sha === s) { out.set(id, d); continue; } }
    todo.push([id, f]);
  }
  if (todo.length) {
    if (!existsSync(BLENDER)) throw new Error(`geometry: Blender not found at ${BLENDER} (set BLENDER); ${todo.length} kit pieces are not measured yet`);
    log(`measuring ${todo.length} kit pieces in Blender (${todo.map(t => t[0]).slice(0, 6).join(', ')}${todo.length > 6 ? ', ...' : ''})`);
    // Blender takes the jobs in batches so one bad mesh does not lose the run
    for (let i = 0; i < todo.length; i += 12) {
      const batch = todo.slice(i, i + 12);
      const p = Bun.spawnSync([BLENDER, '-b', '-P', resolve(ROOT, 'tools/geometry/pieces.py'), '--', PIECE_DIR, String(MEASURE_CELL), ...batch.map(([id, f]) => { const k = skipOf(assets.get(id)!); return `${id}=${f}${k.length ? '|' + k.join(',') : ''}`; })], { stdout: 'pipe', stderr: 'pipe' });
      if (p.exitCode !== 0) throw new Error(`geometry: Blender failed on ${batch.map(b => b[0]).join(', ')}\n${p.stderr.toString().slice(-600)}`);
      for (const [id, f] of batch) {
        const c = cachePath(id); if (!existsSync(c)) { log(`  no data for ${id}`); continue; }
        const d = JSON.parse(readFileSync(c, 'utf8')) as PieceData; d.sha = shas.get(id)!; d.fbx = f; writeFileSync(c, JSON.stringify(d)); out.set(id, d);
      }
    }
  }
  for (const d of out.values()) d.fbx ??= '';
  return out;
}

// ------------------------------------------------------------------ bounds of every catalogued piece (cheap): the mesh against the catalogue
export type Bounds = { min: [number, number, number]; max: [number, number, number]; tris: number } | { error: string };
const BOUNDS_FILE = resolve(ROOT, 'out/scratch/splice/geometry/bounds.json');
export function loadBounds(log: (s: string) => void = () => {}): Map<string, { b: Bounds; asset: KitAsset; fbx: string }> {
  const assets = kitAssets(), cache: Record<string, { sha: string; b: Bounds }> = existsSync(BOUNDS_FILE + '.cache') ? JSON.parse(readFileSync(BOUNDS_FILE + '.cache', 'utf8')) : {};
  const todo: [string, string, string][] = [], out = new Map<string, { b: Bounds; asset: KitAsset; fbx: string }>();
  for (const a of [...assets.values()].sort((x, y) => (x.id < y.id ? -1 : 1))) {
    if (a.status === 'rejected') continue;
    const f = fbxOf(a); if (!f) continue; const s = sha(readFileSync(f)) + ':' + MEASURE_VERSION;
    const c = cache[a.id]; if (c && c.sha === s) out.set(a.id, { b: c.b, asset: a, fbx: f }); else todo.push([a.id, f, s]);
  }
  if (todo.length) {
    log(`bounds of ${todo.length} kit pieces in Blender`);
    for (let i = 0; i < todo.length; i += 60) {
      const batch = todo.slice(i, i + 60), tmp = resolve(ROOT, 'out/scratch/splice/geometry/bounds.part.json');
      const p = Bun.spawnSync([BLENDER, '-b', '-P', resolve(ROOT, 'tools/geometry/bounds.py'), '--', tmp, ...batch.map(([id, f]) => `${id}=${f}`)], { stdout: 'pipe', stderr: 'pipe' });
      if (p.exitCode !== 0) throw new Error(`geometry: Blender bounds failed\n${p.stderr.toString().slice(-400)}`);
      const part = JSON.parse(readFileSync(tmp, 'utf8')) as Record<string, Bounds>;
      for (const [id, f, s] of batch) { const b = part[id]; if (!b) continue; cache[id] = { sha: s, b }; out.set(id, { b, asset: assets.get(id)!, fbx: f }); }
    }
    mkdirSync(resolve(ROOT, 'out/scratch/splice/geometry'), { recursive: true }); writeFileSync(BOUNDS_FILE + '.cache', JSON.stringify(cache));
  }
  return out;
}
