// The Handover points the civic block's ground rules keep clear (mission.json destinations and crew), plan frame. Shared by CD-1 (tools/world/rules/civic-dress.ts)
// and the quiet-place rules (tools/world/gen/street/quiet.ts), so the street generator does not reach into the rules directory.
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { V } from '../../plan/geom';

const MISSION = resolve(import.meta.dir, '../../../contracts/missions/handover/mission.json');

/** The Handover points (mission.json destinations and crew), plan frame. None where the mission file is absent. */
export function handoverPoints(toPlan: (p: V) => V, file = MISSION): { name: string; p: V }[] {
  try {
    const m = JSON.parse(readFileSync(file, 'utf8')) as { destinations: { name: string; at: V }[]; crew: { name: string; at: V }[] };
    return [...m.destinations, ...m.crew].map(x => ({ name: x.name, p: toPlan(x.at) }));
  } catch { return []; }
}
