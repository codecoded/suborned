// Brief 077 (Splice): extra generated roots for the kit catalogue. A DECOKIT_OUT-style folder (decokit_manifest.json + Models/, optional Materials/ and Textures/)
// that a brief builds beside out/decokit so nothing adopted is overwritten (Vela's out/decokit-077). Its rows join kit.json as their own entries:
//   id         the row's name, but a row that rebuilds an existing piece (`replaces.id`) joins as `<replaces.id>_TB` (Aster 2026-10-30, option A): a candidate next to the
//              adopted piece, never in its place. On Sky's yes, civic-look.json `swaps` maps the old id to the _TB one and the old one is rejected.
//   status     the prior catalogue's, else the row's own (`candidate`), else candidate. Never adopted by being catalogued.
//   files      root 'extra' under the asset's `extra` folder (repo-relative). A renamed piece's Kit/ paths carry its id (DK_<id>.fbx, DK_<id>_L1.fbx...) so its prefab
//              (Prefabs/Generated/<fbx name>.prefab) never collides with the piece it would replace; `src` is the file's path in the extra folder.
//   lods       the row's own lods[].screen, kept as they are (never the category's pair), and `lodSize` (the LODGroup size Unity's KitLods sets).
//   objects    the row's per-object materials and shadow casting (the soil objects draw with shadows off), for Latch's KitImport.
//   lodAtPlay  the row's own `lodAtPlay`, else true for a row with LODs (Latch's guard exemption, briefs/077/READY-build: every 077 row's levels are built for play zoom;
//              the four rebuilt stoneworks keep the levels and switch heights of the pieces they rebuild). A row without LODs carries none.
// Declared collision stays in the extra manifest: tools/decokit/collide.ts reads it through each asset's `extra` (an empty list is declared none: render-only).
import { createHash } from 'node:crypto';
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { basename, dirname, join, relative, resolve } from 'node:path';
import type { KitAsset, KitFile, KitLod } from '../../contracts/world/kit.schema';

export const REPO = resolve(import.meta.dir, '../..');
/** Brief 077: the testbed planting's materials, see-through (Sky 2026-10-30: "yeah, see through"). An extra manifest may declare more in its top-level `seeThrough`. */
export const SEE_THROUGH = ['DK_Leaf_TB', 'DK_Petal_TB', 'DK_Soil_TB'];
export const SEE_THROUGH_SRC = 'brief 077, Sky 2026-10-30 (planting is see-through): the testbed leaf, petal and soil materials; tools/kit/extra.ts SEE_THROUGH plus any extra manifest\'s `seeThrough`';
/** The see-through materials: the declared list and every extra root's own. */
export function seeThroughOf(dirs: string[]): string[] {
  const out = new Set(SEE_THROUGH);
  for (const d of dirs) { const f = join(d, 'decokit_manifest.json'); if (existsSync(f)) for (const m of (JSON.parse(readFileSync(f, 'utf8')).seeThrough ?? []) as string[]) out.add(m); }
  return [...out].sort();
}
const sha = (b: Buffer) => createHash('sha256').update(b).digest('hex');

export type ExtraObject = { name: string; materials: string[]; shadows: 'on' | 'off' };
export type ExtraRow = {
  name: string; category: string; status?: string; fbx: string; tris: number; size: number[]; bounds?: [number, number, number, number]; materials: string[];
  files: { file: string; sha256: string; bytes: number; objects?: ExtraObject[] }[]; lods?: { file: string; tris: number; screen: number; sha256: string }[];
  lodSize?: number; lodAtPlay?: boolean; collide?: unknown[]; replaces?: { id: string } | string;
};

/** The catalogue id a row joins as: `<replaces>_TB` for a rebuild of an existing piece, else its name. */
export const extraId = (r: ExtraRow): string => { const rep = typeof r.replaces === 'string' ? r.replaces : r.replaces?.id; return rep ? `${rep}_TB` : r.name; };
/** The id a row rebuilds, if any. */
export const replacesOf = (r: ExtraRow): string | undefined => (typeof r.replaces === 'string' ? r.replaces : r.replaces?.id);
/** A source path's Kit/ path for a renamed piece: `DK_<name>` in the file name becomes `DK_<id>` (LOD suffixes kept). */
export const kitPath = (src: string, name: string, id: string): string => (name === id ? src : join(dirname(src), basename(src).replace(`DK_${name}`, `DK_${id}`)));

export function readExtra(dir: string): ExtraRow[] {
  const f = join(dir, 'decokit_manifest.json');
  if (!existsSync(f)) throw new Error(`kit: extra root ${dir} has no decokit_manifest.json`);
  return (JSON.parse(readFileSync(f, 'utf8')) as { assets: ExtraRow[] }).assets;
}

/** A hash-checked file of the extra root: the manifest's sha256 must be the file's. */
function file(dir: string, role: KitFile['role'], src: string, path: string, want?: string): KitFile {
  const abs = join(dir, src);
  if (!existsSync(abs)) throw new Error(`kit: ${abs} is listed but missing`);
  const b = readFileSync(abs), s = sha(b);
  if (want && want !== s) throw new Error(`kit: ${abs} sha256 ${s.slice(0, 12)} is not the manifest's ${want.slice(0, 12)} (the root changed while it was read: rerun)`);
  return { root: 'extra', role, path, ...(path !== src ? { src } : {}), sha256: s, bytes: statSync(abs).size };
}
const pairOf = (dir: string, role: 'material' | 'texture', src: string): KitFile[] =>
  existsSync(join(dir, src)) ? [file(dir, role, src, src), ...(existsSync(join(dir, `${src}.meta`)) ? [file(dir, `${role}-meta` as KitFile['role'], `${src}.meta`, `${src}.meta`)] : [])] : [];

/**
 * The rows of an extra root as catalogue entries. `others(m)` gives a material's files from the other roots (DK_Limestone and the rest the stonework keeps); a material the
 * extra root has in Materials/ is its own, with the Textures/ its .mat names by guid. `prior` keeps a piece's status; `pivot`, `lodCategoryOf`, `lodClassOf` and `tagsOf`
 * are the catalogue's own rules (a rebuild takes its tags and distance class from the piece it rebuilds).
 */
export function extraAssets(dirAbs: string, h: {
  prior: Map<string, string>; others: (m: string) => KitFile[];
  pivot: (b?: [number, number, number, number]) => Pick<KitAsset, 'pivot'> & { pivotOffset?: [number, number] };
  lodCategoryOf: (id: string, category: string) => KitAsset['lodCategory']; lodClassOf: (id: string) => KitAsset['lodClass'] | undefined; tagsOf: (id: string, category: string) => KitAsset['tags'];
}): KitAsset[] {
  const rel = relative(REPO, dirAbs), texDir = join(dirAbs, 'Textures'), guidTex = new Map<string, string>();
  if (existsSync(texDir)) for (const n of readdirSync(texDir)) if (n.endsWith('.meta')) { const g = /guid: ([0-9a-f]{32})/.exec(readFileSync(join(texDir, n), 'utf8'))?.[1]; if (g) guidTex.set(g, `Textures/${n.slice(0, -5)}`); }
  const matFiles = (names: string[]): KitFile[] => names.flatMap(m => {
    const mp = `Materials/${m}.mat`;
    if (!existsSync(join(dirAbs, mp))) return h.others(m);
    const tex = [...readFileSync(join(dirAbs, mp), 'utf8').matchAll(/guid: ([0-9a-f]{32})/g)].map(g => guidTex.get(g[1]!)).filter((t): t is string => !!t);
    return [...pairOf(dirAbs, 'material', mp), ...[...new Set(tex)].sort().flatMap(t => pairOf(dirAbs, 'texture', t))];
  });
  return readExtra(dirAbs).filter(r => r.status !== 'rejected').map((r): KitAsset => {
    const id = extraId(r), rep = replacesOf(r), base = rep ?? id, f0 = r.files.find(f => f.file === r.fbx) ?? r.files[0]!;
    const lods: KitLod[] = (r.lods ?? []).map(l => ({ file: kitPath(l.file, r.name, id), tris: l.tris, screen: l.screen }));
    const files: KitFile[] = [file(dirAbs, 'fbx', r.fbx, kitPath(r.fbx, r.name, id), f0.sha256), ...(r.lods ?? []).map(l => file(dirAbs, 'lod', l.file, kitPath(l.file, r.name, id), l.sha256))];
    const seen = new Set(files.map(f => f.path));
    for (const f of matFiles(r.materials)) if (!seen.has(f.path)) { seen.add(f.path); files.push(f); }
    const lc = h.lodClassOf(base), objects = f0.objects;
    return {
      id, category: r.category, size: r.size as [number, number, number], ...h.pivot(r.bounds), front: '+Z', tris: r.tris, materials: r.materials,
      ...(lods.length ? { lods } : {}), lodCategory: h.lodCategoryOf(base, r.category), ...(lc ? { lodClass: lc } : {}), ...(r.lodSize ? { lodSize: r.lodSize } : {}),
      ...((r.lodAtPlay ?? lods.length > 0) ? { lodAtPlay: true } : {}),
      ...(objects?.length ? { objects } : {}), files, tags: h.tagsOf(base, r.category), status: (h.prior.get(id) ?? (r.status === 'adopted' ? 'candidate' : r.status) ?? 'candidate') as KitAsset['status'],
      source: 'generated', extra: rel, ...(rep ? { replaces: rep } : {}),
    };
  });
}

/** Declared collision of every extra root kit.json names (asset.extra), by catalogue id; an empty list is declared none (render-only). */
export function extraCollide(assets: { id: string; extra?: string }[]): Map<string, unknown[]> {
  const out = new Map<string, unknown[]>();
  for (const dir of [...new Set(assets.flatMap(a => (a.extra ? [a.extra] : [])))]) {
    const abs = resolve(REPO, dir); if (!existsSync(join(abs, 'decokit_manifest.json'))) continue;
    for (const r of readExtra(abs)) if (Array.isArray(r.collide)) out.set(extraId(r), r.collide);
  }
  return out;
}
/** A kit file's place on disk before the copy: an extra root's file under its asset's folder (at `src` when renamed). */
export const extraSource = (a: { extra?: string }, f: KitFile): string | undefined => (f.root === 'extra' && a.extra ? resolve(REPO, a.extra, f.src ?? f.path) : undefined);
