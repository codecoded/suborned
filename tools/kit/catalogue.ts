// Normalise the DecoKit manifest into the kit catalogue (contracts/world/kit.json): per asset its size, files and sha256, and tags.
import { createHash } from 'node:crypto';
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { join, relative } from 'node:path';
import type { Kit, KitAsset, KitBuilt, KitFile, KitOrder, KitStatus } from '../../contracts/world/kit.schema';
import { WAVE1, tagsOf } from './tags';
import { generatedTags } from './generated';
import { FULL_DETAIL, LOD_CATEGORIES, lodCategoryOf, lodScreens } from './lod';

type Lod = { file: string; tris: number; screen: number };
type GenRow = { name: string; bounds?: [number, number, number, number]; lods?: Lod[]; category: string; fbx: string; tris: number; size: number[]; materials: string[]; parts?: unknown; order?: KitOrder; built?: KitBuilt };
const r3 = (n: number) => Math.round(n * 1000) / 1000;
const sha = (b: Buffer | string) => createHash('sha256').update(b).digest('hex');
const GUID = /guid: ([0-9a-f]{32})/g;

function walk(dir: string, out: string[] = []): string[] {
  for (const e of readdirSync(dir, { withFileTypes: true })) (e.isDirectory() ? walk(join(dir, e.name), out) : out.push(join(dir, e.name)));
  return out;
}

export function buildCatalogue(root: string, prior?: Kit, gen?: string): Kit {
  const manifestText = readFileSync(join(root, 'decokit_manifest.json')), materialsText = readFileSync(join(root, 'decokit_materials.json'));
  const manifest = JSON.parse(manifestText.toString()).assets as { name: string; category: string; fbx: string; tris: number; size: number[]; materials: string[]; parts?: unknown; lod1?: unknown; lods?: Lod[] }[];
  type MatRow = { name: string; texture: string; normal: string; mask: string };
  const genMatsFile = gen && join(gen, 'decokit_materials.json');
  const mats = [...(genMatsFile && existsSync(genMatsFile) ? JSON.parse(readFileSync(genMatsFile, 'utf8')).materials as MatRow[] : []), ...(JSON.parse(materialsText.toString()).materials as MatRow[])];
  const guidTex = new Map<string, string>(); // texture guid -> path relative to its root (the legacy root's, else the generator's: a texture only the generator wrote, e.g. the café's floor and paintings)
  const texDir = join(root, 'Textures'), genTexDir = gen && join(gen, 'Textures');
  for (const [base, dir] of [[gen, genTexDir], [root, texDir]] as const) // the legacy root last: where both have a guid, its path wins
    if (base && dir && existsSync(dir)) for (const f of walk(dir)) if (f.endsWith('.meta')) { const g = /guid: ([0-9a-f]{32})/.exec(readFileSync(f, 'utf8'))?.[1]; if (g) guidTex.set(g, relative(base, f.slice(0, -5))); }
  /** A texture only the generator has (the legacy root lacks it) is the generator's file; one both have stays the legacy root's, as before. */
  const genTex = (path: string) => !!gen && /^Textures\/[^/]+(\.meta)?$/.test(path) && existsSync(join(gen, path)) && !existsSync(join(root, path));
  const cache = new Map<string, KitFile>();
  /** brief 031: the attention face's files ride with the patrol robot: the stage atlas and the face data Latch's RobotFace.cs reads (copied to Kit/Data/). */
  const dataFiles = (id: string): KitFile[] => id !== 'PatrolRobot' || !gen ? [] : ['Data/robot-face-stage.json', 'Data/robot-faces.json', 'Data/RobotGlyphs.txt'].filter(p => existsSync(join(gen, p))).map(p => { const b = readFileSync(join(gen, p)); return { root: 'generated' as const, role: 'data' as const, path: p, sha256: sha(b), bytes: b.length }; });
  // Materials the generator wrote (tools/decokit unity_mats.py: the v0.17 colours and the retuned ones) win over the legacy root's.
  const genMat = (path: string) => !!gen && /^Materials\/[^/]+\.mat(\.meta)?$/.test(path) && existsSync(join(gen, path));
  const file = (role: KitFile['role'], path: string): KitFile | null => {
    const g = genMat(path) || genTex(path), abs = join(g ? gen! : root, path); if (!existsSync(abs)) return null;
    const k = `${role}|${path}`; let f = cache.get(k);
    if (!f) { const b = readFileSync(abs); f = { ...(g ? { root: 'generated' as const } : {}), role, path, sha256: sha(b), bytes: statSync(abs).size }; cache.set(k, f); }
    return f;
  };
  const pair = (role: 'fbx' | 'prefab' | 'material' | 'texture', path: string): KitFile[] => [file(role, path), file(`${role}-meta` as KitFile['role'], `${path}.meta`)].filter((f): f is KitFile => !!f);
  /** The materials and the textures they use, as files. */
  const matFiles = (names: string[]): KitFile[] => {
    const out: KitFile[] = [], tex = new Set<string>();
    for (const m of names) {
      const mp = `Materials/${m}.mat`; out.push(...pair('material', mp));
      if (existsSync(join(genMat(mp) ? gen! : root, mp))) for (const g of readFileSync(join(genMat(mp) ? gen! : root, mp), 'utf8').matchAll(GUID)) { const t = guidTex.get(g[1]!); if (t) tex.add(t); }
      const e = mats.find(x => x.name === m);
      for (const n of [e?.texture, e?.normal, e?.mask]) if (n && (existsSync(join(texDir, n)) || (genTexDir && existsSync(join(genTexDir, n))))) tex.add(`Textures/${n}`);
    }
    for (const t of [...tex].sort()) out.push(...pair('texture', t));
    return out;
  };
  const genManifest = gen && existsSync(join(gen, 'decokit_manifest.json')) ? JSON.parse(readFileSync(join(gen, 'decokit_manifest.json'), 'utf8')).assets as GenRow[] : [];
  /** LOD files (tools/decokit, generated root) of a piece: the file list and the `lods` record. */
  const lodsOf = (id: string): { lods?: Lod[]; files: KitFile[] } => {
    const l = FULL_DETAIL.has(id) ? undefined : genManifest.find(g => g.name === id)?.lods, files: KitFile[] = []; // FULL_DETAIL: shipped uncompressed (Sky), the generator's LODs are not catalogued
    for (const x of l ?? []) { const b = readFileSync(join(gen!, x.file)); files.push({ root: 'generated', role: 'lod', path: x.file, sha256: sha(b), bytes: b.length }); }
    return l?.length ? { lods: l, files } : { files };
  };
  /** Pivot claim measured from the generator's bounds: the footprint centre, or where it sits from the pivot (>5 cm off). */
  const pivotOf = (id: string): Pick<KitAsset, 'pivot'> & { pivotOffset?: [number, number] } => {
    const b = genManifest.find(g => g.name === id)?.bounds, c: [number, number] | undefined = b && [r3((b[0] + b[1]) / 2), r3((b[2] + b[3]) / 2)];
    return c && Math.hypot(c[0], c[1]) > 0.05 ? { pivot: 'origin-ground', pivotOffset: c } : { pivot: 'footprint-centre-ground' };
  };
  const status = new Map((prior?.assets ?? []).map(a => [a.id, a.status]));
  const assets = manifest.map((a0): KitAsset => {
    const st: KitStatus = status.get(a0.name) ?? (WAVE1.has(a0.name) ? 'adopted' : 'candidate');
    // An adopted DecoKit piece the generator re-exported in full (its fbx is in the generator's output, e.g. Vela's slimmed street props, brief 007 round 3):
    // the generator's binary, size, triangles and materials win; the DecoKit's fbx meta (GUID, import settings) and prefab stay, so references hold.
    const g = st === 'adopted' && gen ? genManifest.find(x => x.name === a0.name && x.fbx === a0.fbx && existsSync(join(gen, x.fbx))) : undefined;
    const a = g ? { ...a0, tris: g.tris, size: g.size, materials: g.materials, parts: g.parts ?? a0.parts } : a0;
    const fbx: KitFile[] = g ? [{ root: 'generated', role: 'fbx', path: g.fbx, sha256: sha(readFileSync(join(gen!, g.fbx))), bytes: statSync(join(gen!, g.fbx)).size }, ...pair('fbx', a.fbx).filter(f => f.role === 'fbx-meta')] : pair('fbx', a.fbx);
    const files: KitFile[] = [...fbx, ...pair('prefab', a.fbx.replace(/^Models\//, 'Prefabs/').replace(/\.fbx$/, '.prefab')), ...matFiles(a.materials)];
    files.push(...pair('fbx', a.fbx.replace(/\.fbx$/, '_Far.fbx')).filter(f => !files.some(x => x.path === f.path))); // the full robots' prefabs nest a `_Far` model (brief 026, Latch): copied with the piece, hash-checked like it
    const lo = st === 'adopted' ? lodsOf(a.name) : { files: [] }; // LOD files exist for the pieces the generator was asked about (adopted)
    files.push(...lo.files);
    const lc = lodCategoryOf(a.name, a.category);
    return { id: a.name, category: a.category, size: a.size as [number, number, number], ...pivotOf(a.name), front: '+Z', tris: a.tris, materials: a.materials, ...(a.parts ? { parts: a.parts } : {}), ...(a.lod1 ? { lod1: a.lod1 } : {}), ...('lods' in lo ? { lods: lodScreens(lo.lods, lc, a.name) } : {}), lodCategory: lc, files, tags: tagsOf(a.name, a.category), status: st };
  });
  // Pieces the generator built to a plan order (tools/decokit): adopted, source generated, the order's hash and each file's sha256.
  // No generator output here: the prior catalogue's generated entries are kept as they are (copy then needs `bun run decokit`).
  const manifestIds = new Set(assets.map(a => a.id)), generated: KitAsset[] = [];
  const fresh = new Set(genManifest.filter(g => g.order && g.built).map(g => g.name));
  for (const g of genManifest.filter(g => g.order && g.built)) {
    const fbxAbs = join(gen!, g.fbx), b = readFileSync(fbxAbs), files: KitFile[] = [{ root: 'generated', role: 'fbx', path: g.fbx, sha256: sha(b), bytes: b.length }];
    files.push(...matFiles(g.materials), ...lodsOf(g.name).files, ...dataFiles(g.name), ...(g.name === 'PatrolRobot' ? pair('texture', 'Textures/robot_face_stage_v1.png') : []));
    generated.push({ id: g.name, category: g.category, size: g.size as [number, number, number], ...pivotOf(g.name), front: '+Z', tris: g.tris, materials: g.materials, ...(g.parts ? { parts: g.parts } : {}), ...(g.lods?.length && !FULL_DETAIL.has(g.name) ? { lods: lodScreens(g.lods, lodCategoryOf(g.name, g.category), g.name) } : {}), lodCategory: lodCategoryOf(g.name, g.category), files, tags: generatedTags(g.name), status: status.get(g.name) ?? 'adopted', source: 'generated', order: g.order!, built: g.built! });
  }
  // Pieces the generator built without an order (tools/decokit atompunk.py, horizon.py: crowns, fountain, pavilions, horizon band, twin station): fixed size, tags by category.
  const plain = genManifest.filter(g => !(g.order && g.built) && !manifestIds.has(g.name));
  for (const g of plain) {
    const fbxAbs = join(gen!, g.fbx), b = readFileSync(fbxAbs), files: KitFile[] = [{ root: 'generated', role: 'fbx', path: g.fbx, sha256: sha(b), bytes: b.length }];
    files.push(...matFiles(g.materials), ...lodsOf(g.name).files, ...dataFiles(g.name), ...(g.name === 'PatrolRobot' ? pair('texture', 'Textures/robot_face_stage_v1.png') : []));
    generated.push({ id: g.name, category: g.category, size: g.size as [number, number, number], ...pivotOf(g.name), front: '+Z', tris: g.tris, materials: g.materials, ...(g.parts ? { parts: g.parts } : {}), ...(g.lods?.length && !FULL_DETAIL.has(g.name) ? { lods: lodScreens(g.lods, lodCategoryOf(g.name, g.category), g.name) } : {}), lodCategory: lodCategoryOf(g.name, g.category), files, tags: tagsOf(g.name, g.category), status: status.get(g.name) ?? 'candidate', source: 'generated' });
  }
  for (const a of prior?.assets ?? []) if (a.source === 'generated' && !plain.some(g => g.name === a.id) && !fresh.has(a.id) && !manifestIds.has(a.id)) { const lc = lodCategoryOf(a.id, a.category); const { files, tags, status: st, source, order, built, ...rest } = a; generated.push({ ...rest, ...(a.lods ? { lods: lodScreens(a.lods, lc, a.id) } : {}), lodCategory: lc, files, tags, status: st, source, ...(order ? { order } : {}), ...(built ? { built } : {}) }); }
  generated.sort((a, b) => a.id.localeCompare(b.id));
  assets.push(...generated);
  return { schema: 'suborned.kit/1', source: { manifest: 'decokit_manifest.json', materials: 'decokit_materials.json', manifestSha256: sha(manifestText), materialsSha256: sha(materialsText) }, lodCategories: LOD_CATEGORIES, assets };
}
