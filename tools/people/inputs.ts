// The inputs of a people run, read from files or from the text a run pinned: plan, civic block, life.json, the people rules, the mission.
import { resolve } from 'node:path';
import type { Cast } from '../../contracts/people/cast.schema';
import type { Plan } from '../plan/plan';
import { derive } from '../plan/plan';
import { sha256 } from '../gen/sha';
import { root } from '../rag/config';
import { startMin, type MissionIn } from './handover';
import { parseRules, RULES_FILE, type Rules } from './rules';
import { buildScope, type Scope } from './scope';
import type { Ctx } from './expand';
import { walker } from './walk';
import type { V } from '../plan/geom';

export const MISSION_FILE = 'contracts/missions/handover/mission.json';
export type Texts = { plan: string; block: string; life: string; rules: string; mission: string };
export const lifeFileOf = (plan: { id: string }) => `contracts/world/${plan.id}.life.json`;

export async function readTexts(planFile: string, o: { rules?: string; mission?: string; block?: string; life?: string } = {}): Promise<Texts> {
  const rd = (f: string) => Bun.file(resolve(root, f)).text();
  const plan = await rd(planFile), p = JSON.parse(plan);
  return { plan, block: await rd(o.block ?? p.civic.block), life: await rd(o.life ?? lifeFileOf(p)), rules: await rd(o.rules ?? RULES_FILE), mission: await rd(o.mission ?? MISSION_FILE) };
}

export type World = { plan: Plan; scope: Scope; rules: Rules; mission: MissionIn; ctx: (cast: Cast) => Ctx; hashes: (cast: string) => Ctx['hashes'] };
export function world(t: Texts, scopeName: string): World {
  const plan = JSON.parse(t.plan) as Plan, block = JSON.parse(t.block), life = JSON.parse(t.life), rules = parseRules(t.rules), mission = JSON.parse(t.mission) as MissionIn;
  const d = derive(plan), { scope, civ } = buildScope(plan, block, life, rules, scopeName, d);
  const walk = walker(plan, d, civ, scope.places.map(p => p.at as V), scope.speed);
  const hashes = (cast: string) => ({ cast, life: sha256(t.life), block: sha256(t.block), rules: sha256(t.rules) });
  return { plan, scope, rules, mission, hashes, ctx: cast => ({ scope, rules, cast, walk, planId: plan.id, planVersion: plan.version, atMin: startMin(mission), hashes: hashes(sha256(JSON.stringify(cast))), casting: { scope, mission, plan } }) };
}
