// Copy the adopted assets (with their materials and textures) from the DecoKit into unity/Assets/Suborned/Kit/, verifying sha256. Idempotent.
import { createHash } from 'node:crypto';
import { copyFileSync, existsSync, mkdirSync, readdirSync, readFileSync, renameSync, rmSync, statSync, writeFileSync } from 'node:fs';
import { dirname, join, relative } from 'node:path';
import type { Kit } from '../../contracts/world/kit.schema';

const sha = (p: string) => createHash('sha256').update(readFileSync(p)).digest('hex');
export type CopyResult = { copied: string[]; skipped: string[]; bytes: number; errors: string[]; assets: string[]; pruned: string[] };

/** What Unity owns once it has imported a copy: a .meta (import settings it fills in; a new guid when the old town's copy of the art already holds the catalogue's: SpaceHQ's model and prefab, facade_panel) and a .mat (it re-serialises).
 *  Copying those back on every run only made Unity re-roll the guids and re-import them, and read as a kit change to the delta (brief 078). So they are copied when the catalogue's own hash moves, a real change in the source;
 *  models, textures and prefabs stay byte-checked against it. `settled` (out/kit/copied.json): path -> the catalogue hash last in place. No such file yet: what is on disk stands, once. */
const unityOwned = (path: string) => /\.(meta|mat)$/.test(path);
const readSettled = (file: string | undefined) => { if (!file || !existsSync(file)) return { first: !!file, was: {} as Record<string, string> }; return { first: false, was: JSON.parse(readFileSync(file, 'utf8')) as Record<string, string> }; };

export function copyAdopted(kit: Kit, root: string, dest: string, gen?: string, repo?: string, settledFile?: string): CopyResult {
  const want = new Map<string, { sha256: string; bytes: number; root?: 'generated' | 'extra'; src?: string; extra?: string }>(), assets: string[] = [], errors: string[] = [];
  for (const a of kit.assets.filter(x => x.status === 'adopted')) {
    assets.push(a.id);
    for (const f of a.files) { const o = want.get(f.path); if (o && o.sha256 !== f.sha256) errors.push(`${f.path}: two hashes in the catalogue`); want.set(f.path, { ...f, ...(a.extra ? { extra: a.extra } : {}) }); }
  }
  for (const f of kit.textures ?? []) { const o = want.get(f.path); if (o && o.sha256 !== f.sha256) errors.push(`${f.path}: two hashes in the catalogue`); want.set(f.path, f); } // brief 072-F: kit-wide textures (surface detail)
  for (const f of kit.colourways?.files ?? []) { const o = want.get(f.path); if (o && o.sha256 !== f.sha256) errors.push(`${f.path}: two hashes in the catalogue`); want.set(f.path, f); } // brief 074-B: the colourway material sets
  for (const f of kit.landmarks?.files ?? []) { const o = want.get(f.path); if (o && o.sha256 !== f.sha256) errors.push(`${f.path}: two hashes in the catalogue`); want.set(f.path, f); } // brief 075-C: the landmark livery variants
  for (const f of kit.posters?.files ?? []) { const o = want.get(f.path); if (o && o.sha256 !== f.sha256) errors.push(`${f.path}: two hashes in the catalogue`); want.set(f.path, f); } // brief 075-F1: the billboard and poster variants + sheet B
  for (const f of kit.carPaints?.files ?? []) { const o = want.get(f.path); if (o && o.sha256 !== f.sha256) errors.push(`${f.path}: two hashes in the catalogue`); want.set(f.path, f); } // brief 075 roll-out: the 1950s car paints
  const staged: [string, string][] = [], r: CopyResult = { copied: [], skipped: [], bytes: 0, errors, assets, pruned: [] };
  const settled = readSettled(settledFile), now: Record<string, string> = {};
  for (const [path, f] of [...want].sort(([a], [b]) => a.localeCompare(b))) { // f.root = 'generated': from the generator's output
    const src = f.root === 'extra' ? join(repo ?? '', f.extra ?? '', f.src ?? path) : join(f.root === 'generated' ? (gen ?? root) : root, path), out = join(dest, path); // brief 077: an extra root's file, at its source path
    if (unityOwned(path)) now[path] = f.sha256;
    if (existsSync(out) && statSync(out).size === f.bytes && sha(out) === f.sha256) { r.skipped.push(path); continue; }
    if (settledFile && unityOwned(path) && existsSync(out) && (settled.first || settled.was[path] === f.sha256)) { r.skipped.push(path); continue; }
    if (!existsSync(src)) { errors.push(`${path}: missing in source`); continue; }
    if (sha(src) !== f.sha256) { errors.push(`${path}: source hash differs from kit.json (rerun bun run kit catalogue)`); continue; }
    mkdirSync(dirname(out), { recursive: true }); const tmp = out + '~'; copyFileSync(src, tmp); // staged under a name Unity ignores (trailing ~), renamed in one burst below so the editor reimports a finished set
    if (sha(tmp) !== f.sha256) { errors.push(`${path}: copy hash mismatch`); rmSync(tmp, { force: true }); continue; }
    staged.push([tmp, out]); r.copied.push(path); r.bytes += f.bytes;
  }
  if (!errors.length) for (const [tmp, out] of staged) renameSync(tmp, out); else for (const [tmp] of staged) rmSync(tmp, { force: true });
  if (settledFile && !errors.length) { mkdirSync(dirname(settledFile), { recursive: true }); writeFileSync(settledFile, JSON.stringify(now, Object.keys(now).sort(), 0) + '\n'); }
  // LOD files no adopted piece lists any more (a piece regenerated lighter loses its LODs, brief 007 round 3): removed with their meta, so no stale LOD is imported
  const walk = (d: string): string[] => existsSync(d) ? readdirSync(d, { withFileTypes: true }).flatMap(e => e.isDirectory() ? walk(join(d, e.name)) : [join(d, e.name)]) : [];
  for (const abs of walk(join(dest, 'Models'))) {
    const path = relative(dest, abs); if (!/_L\d\.fbx$/.test(path) || want.has(path)) continue;
    rmSync(abs); if (existsSync(abs + '.meta')) rmSync(abs + '.meta'); r.pruned.push(path);
  }
  // a rejected piece's files (brief 061 retired SignalEye, KerbStud, CrossEdgeLine_1/2, CrossInlayArc_R24_W08 and LightField_1/2; SpaceHQ before): status rejected means removed from the copy. Only the files no adopted piece lists
  // (a material or model two pieces share stays), only under Models/ and Materials/ (the textures are pruned below; Prefabs/ holds Unity's own generated prefabs, which KitImport/KitPlacer remove through the AssetDatabase), each with its meta.
  for (const a of kit.assets.filter(x => x.status === 'rejected')) for (const f of a.files) {
    const path = f.path.replace(/\.meta$/, ''); if (!/^(Models|Materials)\//.test(path) || want.has(path) || want.has(path + '.meta')) continue;
    for (const rel of [path, path + '.meta']) { const abs = join(dest, rel); if (existsSync(abs)) { rmSync(abs); r.pruned.push(rel); } }
  }
  // textures no adopted piece lists any more (a material re-pointed at a lighter sheet leaves the old one behind: brief 026, the six pave_flags_v1 files): removed with their meta. Only Textures/ is pruned: it holds nothing but the kit's copies.
  for (const abs of walk(join(dest, 'Textures'))) {
    const path = relative(dest, abs).replace(/\.meta$/, ''); if (want.has(path) || want.has(path + '.meta') || want.has(relative(dest, abs))) continue;
    rmSync(abs); r.pruned.push(`${relative(dest, abs)}`);
  }
  return r;
}
