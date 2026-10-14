// The inputs of a people run, read from files or from the text a run pinned: plan, civic block, life.json, the people rules, the mission.
import { readFileSync } from 'node:fs';
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

/** The same texts, read at once (the plan checker's rule M-2 re-expands the people when it runs). `plan` is the plan's text or object. */
export function readTextsSync(plan: string | { id: string; civic: { block: string } }, mission: string | object, base: string = root): Texts {
  const rd = (f: string) => readFileSync(resolve(base, f), 'utf8'), same = (obj: unknown, file: string) => { try { return JSON.stringify(JSON.parse(file)) === JSON.stringify(obj); } catch { return false; } };
  // an object that is the file's content stands for the file's text (so the hashes in a people file are the file's); any other is its own JSON
  const planObj = typeof plan === 'string' ? JSON.parse(plan) : plan, planFile = rd(`contracts/world/${planObj.id}.plan.json`), missionFile = rd(MISSION_FILE);
  const planText = typeof plan === 'string' ? plan : same(plan, planFile) ? planFile : JSON.stringify(plan), missionText = typeof mission === 'string' ? mission : same(mission, missionFile) ? missionFile : JSON.stringify(mission);
  return { plan: planText, block: rd(planObj.civic.block), life: rd(lifeFileOf(planObj)), rules: rd(RULES_FILE), mission: missionText };
}

export type World = { plan: Plan; scope: Scope; rules: Rules; mission: MissionIn; ctx: (cast: Cast) => Ctx; hashes: (cast: string) => Ctx['hashes'] };
export function world(t: Texts, scopeName: string): World {
  const plan = JSON.parse(t.plan) as Plan, block = JSON.parse(t.block), life = JSON.parse(t.life), rules = parseRules(t.rules), mission = JSON.parse(t.mission) as MissionIn;
  const d = derive(plan), { scope, civ } = buildScope(plan, block, life, rules, scopeName, d);
  const walk = walker(plan, d, civ, scope.places.map(p => p.at as V), scope.speed);
  const hashes = (cast: string) => ({ cast, life: sha256(t.life), block: sha256(t.block), rules: sha256(t.rules), mission: sha256(t.mission), plan: sha256(t.plan) });
  return { plan, scope, rules, mission, hashes, ctx: cast => ({ scope, rules, cast, walk, planId: plan.id, planVersion: plan.version, atMin: startMin(mission), hashes: hashes(sha256(JSON.stringify(cast))), casting: { scope, mission, plan } }) };
}
