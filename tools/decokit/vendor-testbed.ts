// Brief 077 (Splice; Aster 2026-10-30: "vendoring the testbed modules into the repo: yes, hash-pinned, with provenance"): the testbed planting code Vela's
// tools/decokit/troughs077.ts and domes077.ts run, copied into tools/decokit/vendor/testbed/ with its layout kept (src/ and vendor/three-0.186.1/), so the
// pieces build from this repo and not from ~/Projects/the-suborned (not a git repo: nothing pinned it).
//   bun tools/decokit/vendor-testbed.ts            check: every vendored file's sha256 is the one PROVENANCE.json pins (exit 1 otherwise)
//   bun tools/decokit/vendor-testbed.ts --write    copy the closure from TESTBED_SRC (default ~/Projects/the-suborned/testbed/src) and write PROVENANCE.json
// The closure: the modules the two generators import, every relative import of those (query strings dropped), the files their provenance lists (domes077.ts
// SOURCES), three.js 0.186.1 as the testbed vendors it, and three's licence and source note. Nothing is edited; the generators point at it by TESTBED_SRC.
import { createHash } from 'node:crypto';
import { copyFileSync, existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join, normalize, relative, resolve } from 'node:path';

const repo = resolve(import.meta.dir, '../..');
export const VENDOR = join(repo, 'tools/decokit/vendor/testbed');
const PROV = join(VENDOR, 'PROVENANCE.json');
const sha = (p: string) => createHash('sha256').update(readFileSync(p)).digest('hex');
const START = ['cafe-garden-composition.js', 'flower-look-shapes.js', 'fountain-garden-layout.js', 'fountain-garden.js', 'fountain-garden-lights.js', // imported by domes077.ts / troughs077.ts
  'garden-botanical-geometry.js', 'cafe-botanical-geometry.js', 'garden-stem-geometry.js', 'cafe-bed-geometry.js', 'garden-render-batches.js', 'garden-runtime.js', // and the rest of domes077.ts SOURCES
  'garden-cache.js', 'civic-planting-layout.js', 'flower-bed-layout.js', 'civic-district-look.js', 'civic-paving-material.js'].map(f => `src/${f}`);
const EXTRA = ['vendor/three-0.186.1/three.module.min.js', 'vendor/THREE-LICENSE.txt', 'vendor/THREE-source.json', 'vendor/README.md'];

type Prov = { schema: 'suborned.vendor/1'; brief: string; from: string; copied: string; why: string; files: Record<string, string>; notVendored?: string[] };

const bare = new Set<string>();
function closure(tb: string): string[] {
  const seen = new Set<string>(), todo = [...START, ...EXTRA];
  while (todo.length) {
    const f = normalize(todo.pop()!.split('?')[0]!);
    if (seen.has(f) || !existsSync(join(tb, f))) { if (!existsSync(join(tb, f))) throw new Error(`vendor: ${f} not in ${tb}`); continue; }
    seen.add(f);
    if (!/\.(m?js)$/.test(f)) continue;
    const src = readFileSync(join(tb, f), 'utf8');
    for (const m of src.matchAll(/(?:import|export)[^'";]*?from\s*['"]([^'"]+)['"]|import\(\s*['"]([^'"]+)['"]\s*\)|^import\s+['"]([^'"]+)['"]/gm)) {
      const p = m[1] ?? m[2] ?? m[3]!;
      if (p.startsWith('.')) todo.push(join(dirname(f), p));
      else bare.add(`${f}: ${p}`); // a bare specifier (three/addons/...) the testbed's page import map resolves; never reached by the generators under bun, so not vendored
    }
  }
  return [...seen].sort();
}

if (process.argv.includes('--write')) {
  const tbSrc = resolve(process.env.TESTBED_SRC ?? `${process.env.HOME}/Projects/the-suborned/testbed/src`), tb = dirname(tbSrc), files: Record<string, string> = {};
  for (const f of closure(tb)) { mkdirSync(dirname(join(VENDOR, f)), { recursive: true }); copyFileSync(join(tb, f), join(VENDOR, f)); files[f] = sha(join(VENDOR, f)); }
  const prov: Prov = { schema: 'suborned.vendor/1', brief: '077 (Aster 2026-10-30)', from: tb.replace(process.env.HOME ?? '', '~'), copied: new Date().toISOString().slice(0, 10),
    why: 'the testbed planting code tools/decokit/troughs077.ts and domes077.ts run (TESTBED_SRC=tools/decokit/vendor/testbed/src), copied unedited with its layout; three.js 0.186.1 under its MIT licence (vendor/THREE-LICENSE.txt)', files,
    notVendored: [...bare].sort() };
  writeFileSync(PROV, JSON.stringify(prov, null, 1) + '\n');
  console.log(`vendored ${Object.keys(files).length} files from ${tb} -> ${relative(repo, VENDOR)}`);
} else {
  if (!existsSync(PROV)) { console.error(`vendor: ${relative(repo, PROV)} missing (run --write)`); process.exit(1); }
  const prov = JSON.parse(readFileSync(PROV, 'utf8')) as Prov, bad = Object.entries(prov.files).filter(([f, s]) => !existsSync(join(VENDOR, f)) || sha(join(VENDOR, f)) !== s);
  for (const [f] of bad) console.log(`FAIL ${f}: missing or not the pinned sha256`);
  console.log(bad.length ? `vendor: ${bad.length} of ${Object.keys(prov.files).length} files drifted` : `vendor: ${Object.keys(prov.files).length} files match PROVENANCE.json`);
  process.exit(bad.length ? 1 : 0);
}
