// Brief 077 (Splice; Sky's yes on the Unity capture, briefs/077/unity, 2026-10-30): writes the adoption into contracts/world/civic-look.json (never by hand).
//   bun tools/world/civic/adopt077-import.ts           prints what it would write
//   bun tools/world/civic/adopt077-import.ts --write   writes it
// - swaps: each dome piece -> its testbed rebuild, `<id>` -> `<id>_TB` for every catalogued _TB that names it in `replaces` (Aster 2026-10-30, option A). The block keeps its names.
// - attach: the companions' planting at their stonework's transform (the manifest's `placement`: "at <parent>'s transform (same pivot and frame)"), and GlobeLamp_TB in each
//   PlanterArc_TB's frame at Vela's recorded offset (manifest `placements.at`: kit (x, z) and the height above the arc's pivot). Vantage's sign-off on the lamps is implied by Sky's
//   yes on the capture (Aster 2026-10-30). `dy` is the attached piece's lowest point above its parent's, from the meshes (tools/geometry/pieces.ts loadBounds, Blender, kit axes).
// - materials: a mass's slot override the swapped piece no longer draws is dropped (the fountain gardens' dome leaf and flower slots: PlanterArc_TB's planting is the testbed's,
//   its stone slots keep their overrides, as Vela's restyle bindings do).
// - budget: CD-3's LOD0 count rises to cover the rebuilds' inspection levels, with a note (Sky's rule since brief 026: no model is reduced; at play zoom they draw their gameplay level).
// - palette: the four new materials' unlit colours, provisional: the .mat _BaseColor as Latch wrote it (the leaf and petal colour live in the vertex colours and the atlas),
//   so CD-4 holds every drawn material to an entry. Glint sets the real ones.
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { loadBounds } from '../../geometry/pieces';
import { LOOK_FILE } from './look';

const repo = resolve(import.meta.dir, '../../..');
const EXTRA = resolve(repo, 'out/decokit-077');
const kit = JSON.parse(readFileSync(resolve(repo, 'contracts/world/kit.json'), 'utf8')) as { assets: { id: string; replaces?: string; extra?: string }[] };
const manText = readFileSync(resolve(EXTRA, 'decokit_manifest.json')), man = JSON.parse(manText.toString()).assets as { name: string; placement?: unknown; placements?: { at: { at: [number, number]; z: number } } }[];
const sha = (b: Buffer | string) => createHash('sha256').update(b).digest('hex');

const swaps = Object.fromEntries(kit.assets.filter(a => a.extra && a.replaces).map(a => [a.replaces!, a.id]).sort(([x], [y]) => x.localeCompare(y)));
const bounds = loadBounds(s => console.error(s)), minY = (id: string) => { const b = bounds.get(id)?.b; if (!b || 'error' in b) throw new Error(`adopt077: no mesh bounds for ${id}`); return b.min[1]; };
const r4 = (n: number) => Math.round(n * 10000) / 10000;
const attach: Record<string, { piece: string; at: [number, number]; dy: number; as: string }[]> = {};
for (const r of man) {
  const m = typeof r.placement === 'string' ? /^at (\w+)'s transform/.exec(r.placement) : null;
  if (m) { const parent = swaps[m[1]!] ?? m[1]!; (attach[parent] ??= []).push({ piece: r.name, at: [0, 0], dy: r4(minY(r.name) - minY(parent)), as: 'planting' }); }
}
const lamp = man.find(r => r.name === 'GlobeLamp_TB')?.placements?.at, arc = swaps['PlanterArc'];
if (!lamp || !arc) throw new Error('adopt077: no GlobeLamp_TB placements or no PlanterArc_TB');
(attach[arc] ??= []).push({ piece: 'GlobeLamp_TB', at: [r4(lamp.at[0]), r4(lamp.at[1])], dy: r4(lamp.z + minY('GlobeLamp_TB') - minY(arc)), as: 'lamp' });

const hex = (c: number[]) => '#' + c.slice(0, 3).map(v => Math.round(Math.max(0, Math.min(1, v)) ** (1 / 2.2) * 255).toString(16).padStart(2, '0').toUpperCase()).join(''); // linear -> sRGB (approx.)
const baseColour = (m: string): string => { const t = readFileSync(resolve(EXTRA, 'Materials', `${m}.mat`), 'utf8'), g = /_BaseColor: \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+)/.exec(t); if (!g) throw new Error(`adopt077: ${m}.mat has no _BaseColor`); return hex([+g[1]!, +g[2]!, +g[3]!]); };
const NEW = ['DK_Leaf_TB', 'DK_Opal_TB', 'DK_Petal_TB', 'DK_Soil_TB'];

const look = JSON.parse(readFileSync(LOOK_FILE, 'utf8'));
look.swaps = { ...look.swaps, ...swaps };
look.attach = attach;
const pal = look.palette.materials as { material: string; unlit: string; used_by?: string[]; provisional?: string }[];
for (const m of NEW) if (!pal.some(p => p.material === m)) pal.push({ material: m, unlit: baseColour(m), used_by: kit.assets.filter(a => a.extra && (a as unknown as { materials: string[] }).materials.includes(m)).map(a => a.id), provisional: 'brief 077 (Splice): the .mat _BaseColor (out/decokit-077/Materials), not a measured unlit read; Glint to set' });
pal.sort((a, b) => a.material.localeCompare(b.material));
// slot overrides on a swapped piece's masses: keep only the slots the drawn piece has
const kitAll = JSON.parse(readFileSync(resolve(repo, 'contracts/world/kit.json'), 'utf8')).assets as { id: string; materials: string[] }[], matsOf = (id: string) => kitAll.find(a => a.id === id)?.materials ?? [];
const world = JSON.parse(readFileSync(resolve(repo, 'out/world/harbour-district.world.json'), 'utf8')) as { civic: { id: string; kit?: { id: string } }[] };
const dropped: string[] = [];
for (const [mass, slots] of Object.entries(look.materials as Record<string, Record<string, string>>)) {
  const piece = world.civic.find(m => m.id === mass)?.kit?.id, drawn = piece && swaps[piece]; if (!drawn) continue;
  for (const slot of Object.keys(slots)) if (!matsOf(drawn).includes(slot)) { delete slots[slot]; dropped.push(`${mass} ${slot}`); }
}
const before = look.budget.lod0Tris;
look.budget = { lod0Tris: 370000, note: `${look.budget.note} Raised to 370,000 for brief 077's testbed planting (Sky's yes on the Unity capture, 2026-10-30): the rebuilt troughs, the terrace and the arcs count at their inspection level (LOD0, 359,323 in the block); at play zoom each draws its gameplay level (+77,034 play-zoom triangles for the whole family, Vela's count; Redline measures on the Air). No model is reduced.` };
look.sources['out/decokit-077/decokit_manifest.json'] = sha(manText);
if (!process.argv.includes('--write')) { console.log(JSON.stringify({ swaps, attach, palette: pal.filter(p => NEW.includes(p.material)) }, null, 1)); process.exit(0); }
writeFileSync(LOOK_FILE, JSON.stringify(look, null, 1).replace(/[\u0080-￿]/g, c => `\\u${c.charCodeAt(0).toString(16).padStart(4, '0')}`) + '\n');
console.log(`wrote swaps (${Object.keys(swaps).length}), attach (${Object.values(attach).flat().length}), ${NEW.length} provisional palette entries, budget ${before} -> ${look.budget.lod0Tris}, dropped overrides: ${dropped.join('; ') || 'none'} into ${LOOK_FILE}`);
