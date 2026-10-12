// Normalise the DecoKit manifest into the kit catalogue (contracts/world/kit.json): per asset its size, files and sha256, and tags.
import { createHash } from 'node:crypto';
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { join, relative } from 'node:path';
import type { Kit, KitAsset, KitBuilt, KitFile, KitOrder, KitStatus } from '../../contracts/world/kit.schema';
import { WAVE1, tagsOf } from './tags';
import { generatedTags } from './generated';

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
  const mats = (JSON.parse(materialsText.toString()).materials as { name: string; texture: string; normal: string; mask: string }[]);
  const guidTex = new Map<string, string>(); // texture guid -> path relative to root
  const texDir = join(root, 'Textures');
  for (const f of existsSync(texDir) ? walk(texDir) : []) if (f.endsWith('.meta')) { const g = /guid: ([0-9a-f]{32})/.exec(readFileSync(f, 'utf8'))?.[1]; if (g) guidTex.set(g, relative(root, f.slice(0, -5))); }
  const cache = new Map<string, KitFile>();
  const file = (role: KitFile['role'], path: string): KitFile | null => {
    const abs = join(root, path); if (!existsSync(abs)) return null;
    const k = `${role}|${path}`; let f = cache.get(k);
    if (!f) { const b = readFileSync(abs); f = { role, path, sha256: sha(b), bytes: statSync(abs).size }; cache.set(k, f); }
    return f;
  };
  const pair = (role: 'fbx' | 'prefab' | 'material' | 'texture', path: string): KitFile[] => [file(role, path), file(`${role}-meta` as KitFile['role'], `${path}.meta`)].filter((f): f is KitFile => !!f);
  /** The materials and the textures they use, as files. */
  const matFiles = (names: string[]): KitFile[] => {
    const out: KitFile[] = [], tex = new Set<string>();
    for (const m of names) {
      const mp = `Materials/${m}.mat`; out.push(...pair('material', mp));
      if (existsSync(join(root, mp))) for (const g of readFileSync(join(root, mp), 'utf8').matchAll(GUID)) { const t = guidTex.get(g[1]!); if (t) tex.add(t); }
      const e = mats.find(x => x.name === m);
      for (const n of [e?.texture, e?.normal, e?.mask]) if (n && existsSync(join(texDir, n))) tex.add(`Textures/${n}`);
    }
    for (const t of [...tex].sort()) out.push(...pair('texture', t));
    return out;
  };
  const genManifest = gen && existsSync(join(gen, 'decokit_manifest.json')) ? JSON.parse(readFileSync(join(gen, 'decokit_manifest.json'), 'utf8')).assets as GenRow[] : [];
  /** LOD files (tools/decokit, generated root) of a piece: the file list and the `lods` record. */
  const lodsOf = (id: string): { lods?: Lod[]; files: KitFile[] } => {
    const l = genManifest.find(g => g.name === id)?.lods, files: KitFile[] = [];
    for (const x of l ?? []) { const b = readFileSync(join(gen!, x.file)); files.push({ root: 'generated', role: 'lod', path: x.file, sha256: sha(b), bytes: b.length }); }
    return l?.length ? { lods: l, files } : { files };
  };
  /** Pivot claim measured from the generator's bounds: the footprint centre, or where it sits from the pivot (>5 cm off). */
  const pivotOf = (id: string): Pick<KitAsset, 'pivot'> & { pivotOffset?: [number, number] } => {
    const b = genManifest.find(g => g.name === id)?.bounds, c: [number, number] | undefined = b && [r3((b[0] + b[1]) / 2), r3((b[2] + b[3]) / 2)];
    return c && Math.hypot(c[0], c[1]) > 0.05 ? { pivot: 'origin-ground', pivotOffset: c } : { pivot: 'footprint-centre-ground' };
  };
  const status = new Map((prior?.assets ?? []).map(a => [a.id, a.status]));
  const assets = manifest.map((a): KitAsset => {
    const files: KitFile[] = [...pair('fbx', a.fbx), ...pair('prefab', a.fbx.replace(/^Models\//, 'Prefabs/').replace(/\.fbx$/, '.prefab')), ...matFiles(a.materials)];
    const st: KitStatus = status.get(a.name) ?? (WAVE1.has(a.name) ? 'adopted' : 'candidate');
    const lo = st === 'adopted' ? lodsOf(a.name) : { files: [] }; // LOD files exist for the pieces the generator was asked about (adopted)
    files.push(...lo.files);
    return { id: a.name, category: a.category, size: a.size as [number, number, number], ...pivotOf(a.name), front: '+Z', tris: a.tris, materials: a.materials, ...(a.parts ? { parts: a.parts } : {}), ...(a.lod1 ? { lod1: a.lod1 } : {}), ...('lods' in lo ? { lods: lo.lods } : {}), files, tags: tagsOf(a.name, a.category), status: st };
  });
  // Pieces the generator built to a plan order (tools/decokit): adopted, source generated, the order's hash and each file's sha256.
  // No generator output here: the prior catalogue's generated entries are kept as they are (copy then needs `bun run decokit`).
  const manifestIds = new Set(assets.map(a => a.id)), generated: KitAsset[] = [];
  const fresh = new Set(genManifest.filter(g => g.order && g.built).map(g => g.name));
  for (const g of genManifest.filter(g => g.order && g.built)) {
    const fbxAbs = join(gen!, g.fbx), b = readFileSync(fbxAbs), files: KitFile[] = [{ root: 'generated', role: 'fbx', path: g.fbx, sha256: sha(b), bytes: b.length }];
    files.push(...matFiles(g.materials), ...lodsOf(g.name).files);
    generated.push({ id: g.name, category: g.category, size: g.size as [number, number, number], ...pivotOf(g.name), front: '+Z', tris: g.tris, materials: g.materials, ...(g.parts ? { parts: g.parts } : {}), ...(g.lods?.length ? { lods: g.lods } : {}), files, tags: generatedTags(g.name), status: status.get(g.name) ?? 'adopted', source: 'generated', order: g.order!, built: g.built! });
  }
  for (const a of prior?.assets ?? []) if (a.source === 'generated' && !fresh.has(a.id) && !manifestIds.has(a.id)) generated.push(a);
  generated.sort((a, b) => a.id.localeCompare(b.id));
  assets.push(...generated);
  return { schema: 'suborned.kit/1', source: { manifest: 'decokit_manifest.json', materials: 'decokit_materials.json', manifestSha256: sha(manifestText), materialsSha256: sha(materialsText) }, assets };
}
