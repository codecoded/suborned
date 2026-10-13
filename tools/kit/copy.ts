// Copy the adopted assets (with their materials and textures) from the DecoKit into unity/Assets/Suborned/Kit/, verifying sha256. Idempotent.
import { createHash } from 'node:crypto';
import { copyFileSync, existsSync, mkdirSync, readdirSync, readFileSync, rmSync, statSync } from 'node:fs';
import { dirname, join, relative } from 'node:path';
import type { Kit } from '../../contracts/world/kit.schema';

const sha = (p: string) => createHash('sha256').update(readFileSync(p)).digest('hex');
export type CopyResult = { copied: string[]; skipped: string[]; bytes: number; errors: string[]; assets: string[]; pruned: string[] };

export function copyAdopted(kit: Kit, root: string, dest: string, gen?: string): CopyResult {
  const want = new Map<string, { sha256: string; bytes: number; root?: 'generated' }>(), assets: string[] = [], errors: string[] = [];
  for (const a of kit.assets.filter(x => x.status === 'adopted')) {
    assets.push(a.id);
    for (const f of a.files) { const o = want.get(f.path); if (o && o.sha256 !== f.sha256) errors.push(`${f.path}: two hashes in the catalogue`); want.set(f.path, f); }
  }
  const r: CopyResult = { copied: [], skipped: [], bytes: 0, errors, assets, pruned: [] };
  for (const [path, f] of [...want].sort(([a], [b]) => a.localeCompare(b))) { // f.root = 'generated': from the generator's output
    const src = join(f.root === 'generated' ? (gen ?? root) : root, path), out = join(dest, path);
    if (existsSync(out) && statSync(out).size === f.bytes && sha(out) === f.sha256) { r.skipped.push(path); continue; }
    if (!existsSync(src)) { errors.push(`${path}: missing in source`); continue; }
    if (sha(src) !== f.sha256) { errors.push(`${path}: source hash differs from kit.json (rerun bun run kit catalogue)`); continue; }
    mkdirSync(dirname(out), { recursive: true }); copyFileSync(src, out);
    if (sha(out) !== f.sha256) { errors.push(`${path}: copy hash mismatch`); continue; }
    r.copied.push(path); r.bytes += f.bytes;
  }
  // LOD files no adopted piece lists any more (a piece regenerated lighter loses its LODs, brief 007 round 3): removed with their meta, so no stale LOD is imported
  const walk = (d: string): string[] => existsSync(d) ? readdirSync(d, { withFileTypes: true }).flatMap(e => e.isDirectory() ? walk(join(d, e.name)) : [join(d, e.name)]) : [];
  for (const abs of walk(join(dest, 'Models'))) {
    const path = relative(dest, abs); if (!/_L\d\.fbx$/.test(path) || want.has(path)) continue;
    rmSync(abs); if (existsSync(abs + '.meta')) rmSync(abs + '.meta'); r.pruned.push(path);
  }
  return r;
}
