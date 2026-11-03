// Dry run of `bun run kit` (brief 061, Vela): the catalogue a decokit build would give, diffed against contracts/world/kit.json: what to adopt, what changed bytes, what is marked retired. Writes nothing to contracts/ or unity/.
// DECOKIT_OUT=<build dir> bun tools/decokit/kit_dryrun.ts <build dir>
import { readFileSync, existsSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { join, resolve } from 'node:path';
import type { Kit } from '../../contracts/world/kit.schema';
import { buildCatalogue } from '../kit/catalogue';
import { kitRoot } from '../kit/store';
const gen = resolve(process.argv[2]), repo = resolve(import.meta.dir, '../..');
const prior: Kit = JSON.parse(readFileSync(join(repo, 'contracts/world/kit.json'), 'utf8'));
const man = JSON.parse(readFileSync(join(gen, 'decokit_manifest.json'), 'utf8')).assets as { name: string; retired?: string; tris: number; size: number[]; parts?: string[] }[];
const sim: Kit = structuredClone(prior);
for (const id of ['SignalLens', 'HaloRing']) sim.assets.push({ id, status: 'adopted', source: 'generated' } as any); // simulate Splice adopting the two new pieces, so their LODs are catalogued
const next = buildCatalogue(kitRoot(), sim, gen);
const by = new Map(prior.assets.map(a => [a.id, a])), sha = (p: string) => createHash('sha256').update(readFileSync(join(gen, p))).digest('hex');
const world = JSON.parse(readFileSync(join(repo, 'out/world/harbour-district.world.json'), 'utf8'));
const placed: Record<string, number> = {}; for (const s of world.street) if (s.kit?.id) placed[s.kit.id] = (placed[s.kit.id] ?? 0) + 1;
console.log('== ADOPT (new pieces; status candidate until Splice sets adopted) ==');
for (const a of next.assets) if (!by.has(a.id)) console.log(`  ${a.id}  ${a.category}  tris ${a.tris}  lods ${a.lods?.map(l => l.tris).join('/') ?? '-'}  size ${JSON.stringify(a.size)}  materials ${a.materials.join(', ')}  parts ${JSON.stringify(a.parts ?? [])}  files ${a.files.length}  default tags ${JSON.stringify(a.tags.use)} scale ${JSON.stringify(a.tags.scale)}`);
console.log('== RE-ADOPT (same id, new bytes: re-hash; sizes and triangles) ==');
for (const a of next.assets) { const o = by.get(a.id); if (!o || a.source !== 'generated') continue; const f = a.files.find(x => x.role === 'fbx'), g = o.files.find(x => x.role === 'fbx'); if (f && g && f.sha256 !== g.sha256) console.log(`  ${a.id}  tris ${o.tris} -> ${a.tris}  size ${JSON.stringify(o.size)} -> ${JSON.stringify(a.size)}  pivotOffset ${JSON.stringify(o.pivotOffset ?? null)} -> ${JSON.stringify(a.pivotOffset ?? null)}  placed ${placed[a.id] ?? 0}  materials ${o.materials.join('+')} -> ${a.materials.join('+')}`); }
console.log('== RETIRE (manifest `retired`; adopted today; placed in the world export today) ==');
for (const m of man) if (m.retired) console.log(`  ${m.name}  status ${by.get(m.name)?.status}  placed ${placed[m.name] ?? 0}  use ${JSON.stringify(by.get(m.name)?.tags.use)}  -- ${m.retired}`);
console.log('== DATA ==');
console.log(`  Data/signal-lens.json  sha256 ${sha('Data/signal-lens.json').slice(0, 16)}  (catalogue.ts dataFiles() names PatrolRobot only: add SignalLens)`);
console.log('== TEXTURES / MATERIALS the new pieces bring ==');
for (const a of next.assets.filter(x => ['SignalLens', 'HaloRing'].includes(x.id))) console.log(`  ${a.id}: ${a.files.filter(f => /material$|texture$/.test(f.role)).map(f => f.path).join(', ')}`);
