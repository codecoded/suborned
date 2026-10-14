// Run rule M-2 (Waymark's castRoles with play:true, the same call the overlay makes) on any people file.
// usage: bun briefs/010/pulse/m2.ts <people.json> [<mission.json>]
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic } from '../../../tools/plan/civic';
import { derive } from '../../../tools/plan/plan';
import { overlay } from '../../../tools/missions/handover/overlay';
const root = resolve(import.meta.dir, '../../..');
const plan = JSON.parse(readFileSync(resolve(root, 'contracts/world/harbour-district.plan.json'), 'utf8'));
const block = JSON.parse(readFileSync(resolve(root, plan.civic.block), 'utf8'));
const people = JSON.parse(readFileSync(resolve(root, process.argv[2]!), 'utf8'));
const mission = JSON.parse(readFileSync(resolve(root, process.argv[3] ?? 'contracts/missions/handover/mission.json'), 'utf8'));
const o = overlay(plan, block, people, mission);
const checks = o.civic!({ d: derive(plan), civ: civic(plan.civic, block) } as any);
for (const c of checks) console.log(`${c.ok ? 'ok  ' : 'FAIL'} ${c.id}  ${c.value}${c.detail ? '\n       ' + c.detail.split('; ').join('\n       ') : ''}`);
process.exit(checks.every((c: any) => c.ok) ? 0 : 1);
