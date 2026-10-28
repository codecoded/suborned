// bun run world:occlusion: the play-view occlusion measure (tools/world/occlusion.ts) of an export, or of two as before against after.
// usage: bun tools/world/occlusion-cli.ts [world.json | commit] [--before world.json|commit] [--after world.json|commit] [--out <path without extension>] [--cell 0.5] [--plan plan.json]
//   one input: prints the measure (default out/world/<plan id>.world.json with contracts/world/kit.json).
//   --before / --after: each an export file (read with this tree's kit.json) or a commit, regenerated in a scratch worktree (out/scratch/occlusion/<sha>, `git worktree add --detach`,
//   never a checkout of the shared tree) with that commit's own kit; --out writes <out>.json and <out>.md.
// Regions for every input come from this tree's plan: its areas (derive(plan).areas) and the Handover's capstone zone (standards.blocks.zone), so before and after are cut the same way.
import { execFileSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { existsSync, mkdirSync, readFileSync, symlinkSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import type { World } from '../../contracts/world/world.schema';
import { derive, type Plan } from '../plan/plan';
import { KERBSIDE, type Occlusion, type Share, kitSizes, occlusionLine, occlusionOf, pct } from './occlusion';

const root = resolve(import.meta.dir, '../..');
const args = process.argv.slice(2), opt = (k: string) => (args.includes(k) ? args[args.indexOf(k) + 1] : undefined);
const valued = new Set(['--before', '--after', '--out', '--cell', '--plan'].flatMap(k => (opt(k) ? [opt(k)!] : [])));
const planPath = resolve(root, opt('--plan') ?? 'contracts/world/harbour-district.plan.json');
const plan = JSON.parse(readFileSync(planPath, 'utf8')) as Plan, d = derive(plan);
const areas = d.areas.map(a => ({ id: a.id, name: a.name, ring: a.polyV }));
const zone = (plan.standards as any).blocks?.zone as [number, number, number, number] | undefined;
const cell = Number(opt('--cell') ?? 0.5);

type Src = { label: string; commit?: string; world: string; kit: string; sha256: string };
const git = (...a: string[]) => execFileSync('git', a, { cwd: root, encoding: 'utf8' }).trim();
function source(x: string): Src {
  if (x.endsWith('.json')) { const world = resolve(root, x), kit = resolve(root, 'contracts/world/kit.json'); return { label: x, world, kit, sha256: sha(world) }; }
  const commit = git('rev-parse', x), wt = resolve(root, 'out/scratch/occlusion', commit.slice(0, 12)), world = resolve(wt, 'out/occlusion.world.json');
  if (!existsSync(wt)) { mkdirSync(dirname(wt), { recursive: true }); execFileSync('git', ['worktree', 'add', '--quiet', '--detach', wt, commit], { cwd: root, stdio: 'ignore' }); }
  if (!existsSync(resolve(wt, 'node_modules'))) symlinkSync(resolve(root, 'node_modules'), resolve(wt, 'node_modules'));
  if (!existsSync(world)) { console.error(`occlusion: exporting ${commit.slice(0, 8)} in ${wt}`); execFileSync('bun', ['tools/world/export.ts', '--out', world], { cwd: wt, stdio: ['ignore', 'ignore', 'inherit'] }); }
  return { label: x, commit, world, kit: resolve(wt, 'contracts/world/kit.json'), sha256: sha(world) };
}
const sha = (f: string) => createHash('sha256').update(readFileSync(f)).digest('hex');
const run = (s: Src): Occlusion => occlusionOf(JSON.parse(readFileSync(s.world, 'utf8')) as World, kitSizes(s.kit), areas, zone, cell);

const before = opt('--before'), after = opt('--after');
if (!before || !after) {
  const s = source(args.find(a => !a.startsWith('--') && !valued.has(a)) ?? `out/world/${plan.id}.world.json`), o = run(s);
  console.log(occlusionLine(o));
  if (opt('--out')) { const out = resolve(root, opt('--out')!); mkdirSync(dirname(out), { recursive: true }); writeFileSync(`${out}.json`, JSON.stringify({ schema: 'suborned.occlusion/1', input: s, ...o }, null, 1) + '\n'); }
  process.exit(0);
}

const B = source(before), A = source(after), ob = run(B), oa = run(A);
const rel = (s: Src) => ({ ...s, world: s.world.replace(root + '/', ''), kit: s.kit.replace(root + '/', '') });
const row = (name: string, b: Share | undefined, a: Share | undefined) => {
  if (!b || !a) return '';
  const pp = (x: number) => `${x >= 0 ? '+' : ''}${(x * 100).toFixed(1)}`;
  return `| ${name} | ${b.area} → ${a.area} | ${pct(b.hidden.mean)} | ${pct(a.hidden.mean)} | ${pp(a.hidden.mean! - b.hidden.mean!)} | ${pct(b.figure.mean)} | ${pct(a.figure.mean)} | ${pp(a.figure.mean! - b.figure.mean!)} |`;
};
const yawRow = (name: string, b: Share, a: Share) => `| ${name} | ${oa.yaws.map(y => `${pct(b.hidden[y])} → ${pct(a.hidden[y])}`).join(' | ')} |`;
const json = {
  schema: 'suborned.occlusion/1', brief: '075', what: 'share of pavement the play camera cannot see behind buildings (tools/world/occlusion.ts); informational, not a rule',
  view: { projection: 'orthographic', zoom: 14, pitch: oa.pitch, yaws: oa.yaws, figureHead: 1.8 }, cell, regions: { plan: planPath.replace(root + '/', ''), handoverZone: zone },
  command: `bun run world:occlusion --before ${before} --after ${after}${opt('--out') ? ` --out ${opt('--out')}` : ''}`,
  before: { input: rel(B), ...ob }, after: { input: rel(A), ...oa },
};
const names = Object.fromEntries(areas.map(a => [a.id, a.name]));
const md = [
  `# Play-view occlusion, before and after (brief 075)`, '',
  `Share of pavement hidden behind buildings from the play camera: orthographic, zoom 14, pitch ${oa.pitch}°, mean of yaws ${oa.yaws.join(' / ')}; ${cell} m raster. A figure counts hidden when its head (1.8 m) is. Informational, not a failing rule.`, '',
  `- before: ${B.commit ? `\`${B.commit.slice(0, 8)}\`` : B.label} (${B.world.replace(root + '/', '')})`,
  `- after: ${A.commit ? `\`${A.commit.slice(0, 8)}\`` : A.label} (${A.world.replace(root + '/', '')})`,
  `- regions: this tree's plan areas; the Handover streets = the capstone zone ${JSON.stringify(zone)} (standards.blocks.zone)`,
  `- command: \`${json.command}\``, '',
  ...(['pavement', 'kerbside', 'open'] as const).flatMap(L => {
    const lb = ob.layers[L]!, la = oa.layers[L]!;
    return [`### ${{ pavement: 'Pavement (all street-level footway: plot pavements, squares, courts, foot ways)', kerbside: `Kerbside pavement (within ${KERBSIDE} m of a kerb: the street pavement, without courts and yards)`, open: 'Open ground (pavement and carriageways)' }[L]}`, '',
      `| region | m² before → after | ground hidden before | after | Δ pts | figures hidden before | after | Δ pts |`, `|---|---|---|---|---|---|---|---|`,
      row('**district**', lb.district, la.district), row('**Handover streets**', lb.handover, la.handover),
      ...Object.keys(la.areas).map(k => row(names[k] ?? k, lb.areas[k], la.areas[k])), '',
      `Per yaw, ground hidden (before → after):`, '', `| region | ${oa.yaws.join(' | ')} |`, `|---|${oa.yaws.map(() => '---').join('|')}|`,
      yawRow('district', lb.district, la.district), ...(lb.handover && la.handover ? [yawRow('Handover streets', lb.handover, la.handover)] : []), ''];
  }),
  `### Decks (9.43 m deck, 24 m concourse)`, '', `| region | m² before → after | ground hidden before | after | Δ pts | figures hidden before | after | Δ pts |`, `|---|---|---|---|---|---|---|---|`,
  row('district', ob.layers.deck!.district, oa.layers.deck!.district), '',
  `Method: tools/world/occlusion.ts. Occluders are solid columns: building masses, kit facade pieces at their kit height (the face they dress extruded into the building, at most 14 m, so a canopy over the pavement does not count), civic masses the camera does not fade, megastructure parts and prims, signature kit boxes (an upper bound for slender towers; towers use their stand-in masses). Street items, trees, decks and the horizon are not occluders. Pavement = plot slabs, foot ways and footway tables, less ground under anything standing on the street; open ground adds the carriageways with their kerbs and the circuses. A cell is hidden when the camera's ray from it (or from a head 1.8 m above it, for figures) meets an occluder. All pavement includes courts and yards behind the buildings, hidden at any height; the kerbside layer is the street pavement the play view is about.`, '',
].join('\n');
if (opt('--out')) { const out = resolve(root, opt('--out')!); mkdirSync(dirname(out), { recursive: true }); writeFileSync(`${out}.json`, JSON.stringify(json, null, 1) + '\n'); writeFileSync(`${out}.md`, md); console.log(`occlusion → ${out}.json, .md`); }
console.log(`before ${occlusionLine(ob)}\nafter  ${occlusionLine(oa)}`);
