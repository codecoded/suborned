// Brief 077 (Vela): re-declare the collision volumes of the rebuilt ids in out/decokit-077 from their NEW stonework.
// bun tools/decokit/domes077_collide.ts   (after domes077_final.py; Blender through BLENDER, default the app; run it under `bun run guard`)
// Each rebuilt row names `collide_from`: its stonework alone (stone/DK_<id>_stone.fbx for the embedded pieces, the stripped L0 for the large parents).
// collide.py measures those meshes exactly as `bun run collide` measures a shipped mesh, and the volumes become the row's declared `collide`
// (kit frame, as kit.collide.json). The troughs, walls and kerbs block; the testbed planting does not (render-only, as the companions and beds).
// RecordsTower keeps its builder-declared volumes (records_tower.py declares them from the building; its roof planting is 14 m up).
import { readFileSync, writeFileSync, copyFileSync } from 'node:fs';
import { resolve } from 'node:path';

const repo = resolve(import.meta.dir, '../..');
const root = resolve(repo, 'out/decokit-077');
const man = JSON.parse(readFileSync(resolve(root, 'decokit_manifest.json'), 'utf8'));
const KEEP = new Set(['RecordsTower']);
const rows = man.assets.filter((r: any) => r.collide_from && !KEEP.has(r.name));
const out = resolve(root, 'stone/collide.json');
const blender = process.env.BLENDER ?? '/Applications/Blender.app/Contents/MacOS/Blender';
const jobs = rows.map((r: any) => `${r.name}=${resolve(root, r.collide_from)}`);
const p = Bun.spawnSync([blender, '-b', '-P', resolve(import.meta.dir, 'collide.py'), '--', out, ...jobs], { stdout: 'pipe', stderr: 'pipe' });
if (p.exitCode) { console.error(p.stderr.toString()); process.exit(1); }
const got = JSON.parse(readFileSync(out, 'utf8'));
let bad = 0;
for (const r of rows) {
  const m = got[r.name];
  if (!m || m.error) { console.error(`${r.name}: ${m?.error ?? 'not measured'}`); bad++; continue; }
  r.collide = m.volumes;
  r.collide_note = `declared: measured by collide.py (v${m.v ?? ''}) on the new stonework alone (${r.collide_from}); the testbed planting is render-only`;
  console.log(r.name, m.volumes.length, 'volumes', JSON.stringify(m.bounds));
}
for (const r of man.assets) if (KEEP.has(r.name)) r.collide_note = 'declared: kit.collide.json\'s builder-declared volumes of RecordsTower (records_tower.py), unchanged; its roof planting is above every volume';
writeFileSync(resolve(root, 'decokit_manifest.json'), JSON.stringify(man, null, 1) + '\n');
copyFileSync(resolve(root, 'decokit_manifest.json'), resolve(repo, 'briefs/077/pieces/decokit_manifest.json'));
console.log(bad ? `DOMES077 COLLIDE FAIL ${bad}` : `DOMES077 COLLIDE ${rows.length} rows re-declared`);
process.exit(bad ? 1 : 0);
