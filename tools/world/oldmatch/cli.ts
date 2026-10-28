// bun run oldbuild:match [--built <path>] [--example]: the plan-match check (brief 075 remap step 2; match.ts). Reads the old-built scene's geometry
// (briefs/075/oldbuild/built.json, suborned.oldbuilt/1), compares it with the plan's world export, runs OV-1 on the old-built footprints, writes
// briefs/075/oldbuild/match.json (every mismatch) and briefs/075/oldbuild/plan-patch.json (the proposed plan patch for Datum, outside the Handover zone), and
// exits 1 on any mismatch or overlap (or a missing export). --example writes our own world in the export format to out/oldbuild/built.self.json (the identity:
// it must match with 0 mismatches) as a reference for Latch.
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, relative, resolve } from 'node:path';
import type { OldBuilt } from '../../../contracts/world/oldbuilt.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { generated } from '../../plan/world-ctx';
import { builtOf, builtOverlap, builtSide, compare, patchOf, worldSide } from './match';

const root = resolve(import.meta.dir, '../../..'), arg = (k: string) => { const i = process.argv.indexOf(k); return i > 0 ? process.argv[i + 1] : undefined; };
const planPath = 'contracts/world/harbour-district.plan.json', builtPath = arg('--built') ?? 'briefs/075/oldbuild/built.json', outDir = arg('--out') ?? 'briefs/075/oldbuild';
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

const built: OldBuilt = read(builtPath), ov = builtOverlap(built), rep = compare(builtSide(built), worldSide(w, plan as any), plan as any, ov);
const patch = patchOf(rep, plan, built, builtPath, planPath);
mkdirSync(resolve(root, outDir), { recursive: true });
writeFileSync(resolve(root, outDir, 'match.json'), JSON.stringify({ built: builtPath, builtSource: built.source, plan: planPath, planVersion: plan.version, ...rep }, null, 1));
writeFileSync(resolve(root, outDir, 'plan-patch.json'), JSON.stringify(patch, null, 1));

const n = rep.mismatches.length;
console.log(`plan-match: ${builtPath} against ${planPath} v${plan.version} (footprints ${rep.tolerance.footprint} m, kerbs/pavements ${rep.tolerance.kerb} m, entrances ${rep.tolerance.entrance} m, levels exact)`);
for (const [k, v] of Object.entries(rep.counts).sort()) console.log(`  ${k.padEnd(28)} ${v}`);
console.log(`  mismatches ${n}: inside the Handover zone ${rep.inZone} (hard), outside ${rep.outside} (→ plan-patch: ${patch.ops.length} ops for Datum, ${patch.builderFixes.length} builder fixes for Latch)`);
console.log(`  OV-1 on the old-built footprints: ${ov.faults.length} faults over ${ov.checked} footprints`);
for (const s of rep.skipped) console.log(`  skipped: ${s}`);
for (const m of rep.mismatches.filter(m => m.zone).slice(0, 20)) console.log(`  ZONE ${m.id} ${m.kind} ${m.side} ${m.el} ${m.dev ?? '-'} m at ${m.at.join(',')}`);
console.log(`  wrote ${outDir}/match.json, ${outDir}/plan-patch.json`);
const fail = n > 0 || ov.faults.length > 0;
console.log(fail ? 'FAIL' : 'PASS');
process.exit(fail ? 1 : 0);
