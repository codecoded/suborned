// bun run kit [--catalogue]: write contracts/world/kit.json from the DecoKit manifest (keeping each asset's status), then copy
// the `adopted` assets into unity/Assets/Suborned/Kit/ with hashes verified. A rerun copies nothing.
// Generated pieces (tools/decokit, out/decokit or $DECOKIT_OUT) join as adopted, source generated.
// Env: SUBORNED_OLD (old repo), SUBORNED_STORE (store). Exit 1 on any hash or missing file.
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import type { Kit } from '../../contracts/world/kit.schema';
import { usageGate } from '../gen/flags';
import { buildCatalogue } from './catalogue';
import { copyAdopted } from './copy';
import { GEN_ROOT, kitRoot } from './store';

const USAGE = `usage: bun run kit [--catalogue] [--extra DIR ...]
  (no flag)    write contracts/world/kit.json from the DecoKit manifest, then copy the adopted pieces into unity/Assets/Suborned/Kit/ (hash-checked)
  --catalogue  write kit.json only; copy nothing
  --extra DIR  brief 077: also read an extra generated root (decokit_manifest.json + Models/, e.g. out/decokit-077); repeatable. Its rows join as their own
               candidates (a rebuild of an adopted piece as <id>_TB), keep their own LOD screens and lodSize, and never replace another root's piece.
               An extra piece already in kit.json whose root is not given is kept exactly as it was.
  --help, -h   print this and exit; any unknown option does the same and does nothing
env: SUBORNED_OLD (old repo), SUBORNED_STORE (store), DECOKIT_OUT (generated pieces), DECOKIT_EXTRA (extra roots, colon-separated, as --extra)`;
usageGate(process.argv.slice(2), { flags: ['--catalogue'], valued: [{ flag: '--extra', required: true }] }, USAGE, 'kit');

const repo = resolve(import.meta.dir, '../..'), path = resolve(repo, 'contracts/world/kit.json'), dest = resolve(repo, 'unity/Assets/Suborned/Kit');
const root = kitRoot(), prior: Kit | undefined = existsSync(path) ? JSON.parse(readFileSync(path, 'utf8')) : undefined;
const argv = process.argv.slice(2), extras = [...argv.flatMap((a, i) => (a === '--extra' ? [argv[i + 1]!] : [])), ...(process.env.DECOKIT_EXTRA ?? '').split(':').filter(Boolean)].map(d => resolve(repo, d));
const kit = buildCatalogue(root, prior, GEN_ROOT, extras), text = JSON.stringify(kit, null, 1) + '\n';
if (!prior || readFileSync(path, 'utf8') !== text) { writeFileSync(path, text); console.log(`kit.json written: ${kit.assets.length} assets`); } else console.log(`kit.json unchanged: ${kit.assets.length} assets`);
const by = (s: string) => kit.assets.filter(a => a.status === s).length;
console.log(`generated ${kit.assets.filter(a => a.source === 'generated').length} (${GEN_ROOT})`);
console.log(`status: adopted ${by('adopted')}, candidate ${by('candidate')}, rejected ${by('rejected')}; source ${root}`);
// brief 077: an extra root's piece whose material has no .mat in any root draws unassigned: name it (the catalogue lists only files that exist)
for (const a of kit.assets.filter(x => x.extra && x.status !== 'rejected')) {
  const missing = a.materials.filter(m => !a.files.some(f => f.path === `Materials/${m}.mat`));
  if (missing.length) console.log(`WARN ${a.id} (${a.extra}): no .mat in any root for ${missing.join(', ')}`);
}
if (process.argv.includes('--catalogue')) process.exit(0);
const r = copyAdopted(kit, root, dest, GEN_ROOT, repo, join(repo, 'out/kit/copied.json'));
console.log(`adopted ${r.assets.length}: ${r.assets.join(', ')}`);
console.log(`copied ${r.copied.length} files (${(r.bytes / 1048576).toFixed(2)} MB), skipped ${r.skipped.length} already present and verified → ${dest}`);
for (const f of r.copied) console.log(`  + ${f}`);
for (const f of r.pruned) console.log(`  - ${f} (stale: no adopted piece lists it)`);
for (const e of r.errors) console.error(`ERROR ${e}`);
process.exit(r.errors.length ? 1 : 0);
