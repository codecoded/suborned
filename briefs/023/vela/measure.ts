// Brief 023 · Vela: V-1 measure of the Records Office public door as the checker reads it (tools/missions/handover/visibility.ts), per point, with what blocks it.
// Run (repo root): bun briefs/023/vela/measure.ts [Name ...]
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { derive } from '../../../tools/plan/plan';
import { civic } from '../../../tools/plan/civic';
import { generated } from '../../../tools/plan/world-ctx';
import { blockersFor, visibility } from '../../../tools/missions/handover/visibility';
const ROOT = resolve(import.meta.dir, '../../..');
const J = (p: string) => JSON.parse(readFileSync(resolve(ROOT, p), 'utf8'));
const plan = J('contracts/world/harbour-district.plan.json'), block = J(plan.civic.block), mission = J('contracts/missions/handover/mission.json');
const d = derive(plan), civ = civic(plan.civic, block), { w } = generated(plan, d, civ);
const rows = visibility(mission, civ, plan, blockersFor(w, civ, plan));
const want = process.argv.slice(2);
for (const r of rows) if (!want.length || want.some(n => r.what.includes(n))) console.log(`${r.what.padEnd(22)} yaw ${r.yaw}  seen ${(r.seen * 100).toFixed(0)}%  clear ${(r.clear * 100).toFixed(0)}%  fade-only ${(r.onlyFade * 100).toFixed(0)}%  by ${r.by.join(', ')}`);
