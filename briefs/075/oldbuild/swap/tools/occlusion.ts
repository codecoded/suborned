// Brief 075 remap step 2, the swap (Latch): the play-view occlusion measure (tools/world/occlusion.ts, the same code `bun run world:occlusion` runs) of the
// old-built town at both height settings, against our pipeline's own world from the same pinned export.
//   bun briefs/075/oldbuild/swap/tools/occlusion.ts [--world out/scratch/latch/075swap/head.world.json] [--kit <HEAD kit.json>] [--out briefs/075/oldbuild/swap/occlusion]
// Each swapped world = the pinned export with its occluders replaced: outside the Handover zone the old builders' standing pieces (occluders-<heights>.json,
// written by OldTownSwap.Build from the built scene: every frontage piece as its oriented box, the other building pieces, generated masses by part); inside
// it ours as exported (the buildings that reach into the zone with their facades, the civic block), plus the network signatures the swap keeps (stations,
// the liner). The walkable ground (pavement, kerbside, carriageways, decks) and the regions (the plan's areas, the zone) are the plan's, so all three
// are cut the same way.
import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { resolve } from 'node:path';
import { createHash } from 'node:crypto';
import type { World } from '../../../../../contracts/world/world.schema';
import { derive, type Plan } from '../../../../../tools/plan/plan';
import { kitSizes, occlusionOf, occlusionLine, pct, type Occlusion } from '../../../../../tools/world/occlusion';

const root = resolve(import.meta.dir, '../../../../..');
const args = process.argv.slice(2), opt = (k: string, d: string) => (args.includes(k) ? args[args.indexOf(k) + 1]! : d);
const worldPath = resolve(root, opt('--world', 'out/scratch/latch/075swap/head.world.json'));
const kitPath = resolve(root, opt('--kit', existsSync(resolve(root, 'out/scratch/latch/075swap/head/contracts/world/kit.json')) ? 'out/scratch/latch/075swap/head/contracts/world/kit.json' : 'contracts/world/kit.json'));
const out = resolve(root, opt('--out', args.includes('--no-mass') || args.includes('--no-old') ? 'out/scratch/latch/075swap/occlusion-probe' : 'briefs/075/oldbuild/swap/occlusion'));
const plan = JSON.parse(readFileSync(resolve(root, 'contracts/world/harbour-district.plan.json'), 'utf8')) as Plan, d = derive(plan);
const areas = d.areas.map(a => ({ id: a.id, name: a.name, ring: a.polyV }));
const zone = (plan.standards as any).blocks.zone as [number, number, number, number];
const sha = (f: string) => createHash('sha256').update(readFileSync(f)).digest('hex');
const base = JSON.parse(readFileSync(worldPath, 'utf8')) as World;
const kit = kitSizes(kitPath);

type V = [number, number];
const box = (ring: V[]) => ring.reduce((b, p) => [Math.min(b[0], p[0]), Math.max(b[1], p[0]), Math.min(b[2], p[1]), Math.max(b[3], p[1])], [Infinity, -Infinity, Infinity, -Infinity]);
const reaches = (ring: V[]) => { const b = box(ring); return b[0] < zone[1] && b[1] > zone[0] && b[2] < zone[3] && b[3] > zone[2]; };

function swapped(heights: string): { w: World; n: number; file: string } {
  const file = resolve(root, `briefs/075/oldbuild/swap/occluders-${heights}.json`);
  const occ = (JSON.parse(readFileSync(file, 'utf8')).occluders as { ring: V[]; base: number; top: number; piece: string; building: string; kind?: string }[]).filter(o => !(args.includes('--no-mass') && o.kind === 'mass') && !args.includes('--no-old'));
  const w = structuredClone(base) as any;
  const keepBld = new Set<string>();
  const ours = (w.buildings as any[]).filter(b => reaches(b.ring));
  for (const b of ours) keepBld.add(b.id.slice(0, b.id.lastIndexOf('/')));
  w.buildings = [...ours, ...occ.filter(o => o.top > o.base).map((o, i) => ({ id: `old/${o.building}/${i}`, surface: 'old', ring: o.ring, base: o.base, height: o.top - o.base }))];
  w.facades = (w.facades as any[]).filter(f => keepBld.has(f.building));
  w.megastructures = (w.megastructures as any[]).filter(m => m.parts.some((p: any) => reaches(p.ring)));
  w.signatures = (w.signatures as any[]).filter(s => s.id.startsWith('station/') || s.id === 'signature/ocean-liner');
  return { w, n: occ.length, file };
}

const runs: Record<string, { label: string; o: Occlusion; input: Record<string, unknown> }> = {};
runs.ours = { label: 'our pipeline (pinned export as built, before the swap)', o: occlusionOf(base, kit, areas, zone, 0.5), input: { world: worldPath.replace(root + '/', ''), sha256: sha(worldPath) } };
for (const h of ['old', 'rule']) {
  const s = swapped(h);
  runs[h] = { label: `old-built, heights "${h}"`, o: occlusionOf(s.w, kit, areas, zone, 0.5), input: { occluders: s.file.replace(root + '/', ''), sha256: sha(s.file), oldOccluders: s.n } };
}
for (const [k, r] of Object.entries(runs)) console.log(`${k}: ${occlusionLine(r.o)}`);
const P = (r: Occlusion, L = 'pavement') => (r.layers as any)[L];
const md = [
  '# Brief 075 swap · play-view occlusion (tools/world/occlusion.ts)',
  '',
  `Share of pavement the play camera (orthographic, zoom 14, pitch ${runs.ours.o.pitch}°, yaws ${runs.ours.o.yaws.join('/')}) cannot see behind buildings, ${runs.ours.o.cell} m raster; regions from the plan. Informational, not a rule.`,
  '',
  '| world | district pavement hidden | kerbside (≤ 8 m from a kerb) | Handover streets | figures (district) |',
  '|---|---|---|---|---|',
  ...Object.entries(runs).map(([, r]) => `| ${r.label} | ${pct(P(r.o).district.hidden.mean)} | ${pct(P(r.o, 'kerbside')?.district.hidden.mean)} | ${pct(P(r.o).handover?.hidden.mean)} | ${pct(P(r.o).district.figure.mean)} |`),
  '',
  `Command: \`bun briefs/075/oldbuild/swap/tools/occlusion.ts\` (kit sizes: ${kitPath.replace(root + '/', '')}).`,
].join('\n');
writeFileSync(`${out}.md`, md + '\n');
writeFileSync(`${out}.json`, JSON.stringify({ schema: 'suborned.occlusion/1', brief: '075 swap', what: 'share of pavement the play camera cannot see behind buildings; informational', zone, kit: kitPath.replace(root + '/', ''), runs }, null, 1) + '\n');
console.log(md);
