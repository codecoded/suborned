// bun run kit [--catalogue]: write contracts/world/kit.json from the DecoKit manifest (keeping each asset's status), then copy
// the `adopted` assets into unity/Assets/Suborned/Kit/ with hashes verified. A rerun copies nothing.
// Generated pieces (tools/decokit, out/decokit or $DECOKIT_OUT) join as adopted, source generated.
// Env: SUBORNED_OLD (old repo), SUBORNED_STORE (store). Exit 1 on any hash or missing file.
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Kit } from '../../contracts/world/kit.schema';
import { buildCatalogue } from './catalogue';
import { copyAdopted } from './copy';
import { GEN_ROOT, kitRoot } from './store';

const repo = resolve(import.meta.dir, '../..'), path = resolve(repo, 'contracts/world/kit.json'), dest = resolve(repo, 'unity/Assets/Suborned/Kit');
const root = kitRoot(), prior: Kit | undefined = existsSync(path) ? JSON.parse(readFileSync(path, 'utf8')) : undefined;
const kit = buildCatalogue(root, prior, GEN_ROOT), text = JSON.stringify(kit, null, 1) + '\n';
if (!prior || readFileSync(path, 'utf8') !== text) { writeFileSync(path, text); console.log(`kit.json written: ${kit.assets.length} assets`); } else console.log(`kit.json unchanged: ${kit.assets.length} assets`);
const by = (s: string) => kit.assets.filter(a => a.status === s).length;
console.log(`generated ${kit.assets.filter(a => a.source === 'generated').length} (${GEN_ROOT})`);
console.log(`status: adopted ${by('adopted')}, candidate ${by('candidate')}, rejected ${by('rejected')}; source ${root}`);
if (process.argv.includes('--catalogue')) process.exit(0);
const r = copyAdopted(kit, root, dest, GEN_ROOT);
console.log(`adopted ${r.assets.length}: ${r.assets.join(', ')}`);
console.log(`copied ${r.copied.length} files (${(r.bytes / 1048576).toFixed(2)} MB), skipped ${r.skipped.length} already present and verified → ${dest}`);
for (const f of r.copied) console.log(`  + ${f}`);
for (const f of r.pruned) console.log(`  - ${f} (stale: no adopted piece lists it)`);
for (const e of r.errors) console.error(`ERROR ${e}`);
process.exit(r.errors.length ? 1 : 0);
