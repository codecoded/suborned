// bun run oldbuild:match [--built <path>] [--out <dir>] [--tag <t>] [--example]: the plan-match check (brief 075 remap step 2; match.ts). Reads the old-built scene's geometry
// (briefs/075/oldbuild/built.json, suborned.oldbuilt/1), compares it with the plan's world export, runs OV-1 on the old-built footprints, writes
// briefs/075/oldbuild/match.json (every mismatch) and briefs/075/oldbuild/plan-patch.json (the proposed plan patch for Datum, outside the Handover zone), and
// exits 1 on any mismatch or overlap (or a missing export). --example writes our own world in the export format to out/oldbuild/built.self.json (the identity:
// it must match with 0 mismatches) as a reference for Latch. --tag t writes match-t.json and plan-patch-t.json instead (a later build's run beside the decided one).
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, relative, resolve } from 'node:path';
import type { OldBuilt } from '../../../contracts/world/oldbuilt.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { generated } from '../../plan/world-ctx';
import { readBuilt } from './latch';
import { MATCH, builtOf, builtOverlap, builtSide, compare, patchOf, worldSide } from './match';

const root = resolve(import.meta.dir, '../../..'), arg = (k: string) => { const i = process.argv.indexOf(k); return i > 0 ? process.argv[i + 1] : undefined; };
const planPath = 'contracts/world/harbour-district.plan.json', builtPath = arg('--built') ?? (existsSync(resolve(root, 'briefs/075/oldbuild/built.json')) ? 'briefs/075/oldbuild/built.json' : 'briefs/075/oldbuild/swap/built.json'), outDir = arg('--out') ?? 'briefs/075/oldbuild', tag = arg('--tag'), matchFile = tag ? `match-${tag}.json` : 'match.json', patchFile = tag ? `plan-patch-${tag}.json` : 'plan-patch.json';
const read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read(planPath), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block)), { w } = generated(plan, d, civ);

if (process.argv.includes('--example')) {
  const f = resolve(root, 'out/oldbuild/built.self.json');
  mkdirSync(dirname(f), { recursive: true });
  writeFileSync(f, JSON.stringify(builtOf(w, plan as any), null, 1));
  console.log(`wrote ${relative(root, f)} (our world as suborned.oldbuilt/1)`);
  process.exit(0);
}
if (!existsSync(resolve(root, builtPath))) { console.error(`plan-match: ${builtPath} not found (Latch's swap exports it; schema contracts/world/oldbuilt.schema.ts). FAIL`); process.exit(1); }

const raw = read(builtPath), built: OldBuilt = readBuilt(raw), ov = builtOverlap(built), rep = compare(builtSide(built), worldSide(w, plan as any), plan as any, ov);
const patch = patchOf(rep, plan, built, builtPath, planPath, w.ground.roads);
mkdirSync(resolve(root, outDir), { recursive: true });
writeFileSync(resolve(root, outDir, matchFile), JSON.stringify({ built: builtPath, builtSchema: raw.schema, builtSource: built.source, plan: planPath, planVersion: plan.version, ...rep }, null, 1));
writeFileSync(resolve(root, outDir, patchFile), JSON.stringify(patch, null, 1));

const n = rep.mismatches.length;
console.log(`plan-match: ${builtPath} against ${planPath} v${plan.version} (footprints ${rep.tolerance.footprint} m, kerbs/pavements ${rep.tolerance.kerb} m, entrances ${rep.tolerance.entrance} m, levels exact)`);
for (const [k, v] of Object.entries(rep.counts).sort()) {
  const areas = ['zone', 'core', 'district'].map(a => [a, rep.mismatches.filter(m => `${m.kind} ${m.side}` === k && m.area === a).length] as const).filter(([, n]) => n);
  const len = rep.mismatches.filter(m => `${m.kind} ${m.side}` === k).reduce((s, m) => s + (m.length ?? 0), 0);
  console.log(`  ${k.padEnd(28)} ${String(v).padStart(5)}${len ? ` (${Math.round(len)} m)` : ''}${areas.length ? `  [${areas.map(([a, n]) => `${a} ${n}`).join(', ')}]` : ''}`);
}
for (const [k, v] of Object.entries(rep.uncovered)) console.log(`  not compared (outside the built ground's coverage or the sheet): ${k} ${v}${k.includes('count') ? '' : ' m'}`);
console.log(`  mismatches ${n}: inside the Handover zone ${rep.inZone} (hard), outside ${rep.outside} (→ plan-patch: ${patch.ops.length} ops for Datum, ${patch.builderFixes.length} builder fixes for Latch)`);
console.log(`  OV-1 on the old-built footprints: ${ov.faults.length} faults over ${ov.checked} footprints`);
{
  // by what met what: the file's boxes are bounds (a CityMass component's axis box, a bay's prefab box, a tower's box on its podium), so a fault may be the
  // box, not the building; on the sheet or beyond it
  const B = new Map(built.buildings.map(b => [b.id, b])), sh = (plan as any).sheet as { x: [number, number]; y: [number, number] } | undefined;
  const cat = (id: string) => { const b = B.get(id); if (!b) return '?'; const k = b.piece?.startsWith('mesh:') ? 'citymass' : b.kind === 'commercial' ? 'bay' : b.kind; return b.base > MATCH.ground ? `${k} (raised)` : k; };
  const by = new Map<string, number>();
  for (const f of ov.faults) { const on = !sh || (f.at[0] >= sh.x[0] && f.at[0] <= sh.x[1] && f.at[1] >= sh.y[0] && f.at[1] <= sh.y[1]); const k = `${[cat(f.a), cat(f.b)].sort().join(' × ')}${on ? '' : ', beyond the sheet'}`; by.set(k, (by.get(k) ?? 0) + 1); }
  for (const [k, n] of [...by].sort((a, b) => b[1] - a[1])) console.log(`    ${k.padEnd(48)} ${n}`);
}
for (const s of rep.skipped) console.log(`  skipped: ${s}`);
const src = (raw.export?.source ?? '') as string, ver = src.match(/v(\d+\.\d+\.\d+)/)?.[1];
if (ver && ver !== plan.version) console.log(`  STALE: the scene was built from ${src} (sha256 ${String(raw.export?.sha256 ?? '?').slice(0, 12)}…), the plan is v${plan.version}: rebuild the swap on the current export before reading plan-only network differences`);
for (const m of rep.mismatches.filter(m => m.zone).slice(0, 20)) console.log(`  ZONE ${m.id} ${m.kind} ${m.side} ${m.el} ${m.dev ?? '-'} m at ${m.at.join(',')}`);
console.log(`  wrote ${outDir}/${matchFile}, ${outDir}/${patchFile}`);
const fail = n > 0 || ov.faults.length > 0;
console.log(fail ? 'FAIL' : 'PASS');
process.exit(fail ? 1 : 0);
