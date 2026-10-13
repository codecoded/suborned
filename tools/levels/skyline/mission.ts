// The Handover mission is crucial to keep (Sky, 2026-10-14): a skyline candidate that breaks one of its rules (C-3 its
// points land on a plot or a street, M-1 its routes reach their end, W-1 its walks from its origins) is a fault, routed
// to the owner of what broke it. The rules are the Handover overlay's (tools/missions/handover/overlay.ts). The mission
// file is pinned in the run folder like the plan and the block, so the rules a run was judged by are the ones it
// replays against.
import { resolve } from 'node:path';
import type { Check as Rule } from '../../plan/checks';
import { check } from '../../plan/checks';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import type { Check } from '../../gen/plugin';
import { sha256 } from '../../gen/sha';
import { root } from '../../rag/config';
import { legacyOverlay } from '../../missions/handover/overlay';
import type { Owner } from './apply';

export const MISSION_FILE = 'contracts/missions/handover/mission.json';
export const MISSION_PIN = 'input.mission.json';
export const MISSION_GROUP = 'Handover';

/** The part of the mission file the rules read: where the destinations are and which routes must reach their end. */
export type Mission = {
  destinations: { name: string; at: [number, number]; surface: string }[];
  routes: { id: string; name: string; from: string; to: string[]; toName?: string }[];
};
export type MissionRecord = { source: string; sha256: string };

const parse = (raw: string): Mission => {
  const m = JSON.parse(raw) as Partial<Mission>;
  if (!Array.isArray(m.destinations) || !Array.isArray(m.routes)) {
    throw new Error('the mission file has no destinations or routes');
  }
  return { destinations: m.destinations, routes: m.routes };
};

/** At run start: copies the mission file into the run folder; returns it and what run.json records. */
export async function pinMission(
  rdir: string, file = MISSION_FILE,
): Promise<{ mission: Mission; record: MissionRecord }> {
  const raw = await Bun.file(resolve(root, file)).text();
  await Bun.write(`${rdir}/${MISSION_PIN}`, raw);
  return { mission: parse(raw), record: { source: file, sha256: sha256(raw) } };
}

/** Replay: the pinned copy matches its sha256. */
export async function checkMission(c: Check): Promise<Mission | undefined> {
  const rec: MissionRecord | undefined = c.run.inputMission;
  if (!rec) { c.fail('run.json has no inputMission'); return undefined; }
  const raw = await Bun.file(`${c.dir}/${MISSION_PIN}`).text().catch(() => undefined);
  if (raw === undefined) { c.fail(`${MISSION_PIN} is missing`); return undefined; }
  if (sha256(raw) !== rec.sha256) { c.fail(`${MISSION_PIN} does not match the recorded sha256`); return undefined; }
  c.ok(`mission ${rec.sha256.slice(0, 12)}`);
  return parse(raw);
}

const same = (a: Rule, b: Rule) => a.id === b.id && a.rule === b.rule && JSON.stringify(a) === JSON.stringify(b);

/**
 * The Handover rules on the plan: the checks the overlay adds or changes against the plan checker's own on the same
 * plan. A rule the overlay changes (W-1, walks from the mission's origins) keeps its id with an `h` after it, so that
 * both stand in one report.
 */
export function missionChecks(plan: Plan, block: unknown, mission: Mission): Rule[] {
  const withMission = { ...(block as object), destinations: mission.destinations };
  const overlay = legacyOverlay({ ...plan, routes: mission.routes }, withMission);
  if (!overlay) return [];
  const d = derive(plan), civ = civic(plan.civic, block as Parameters<typeof civic>[1]);
  const base = check(plan, d, civ, true).checks;
  return check(plan, d, civ, true, overlay).checks.filter(x => !base.some(b => same(b, x))).map(x => ({
    ...x, id: base.some(b => b.id === x.id) ? `${x.id}h` : x.id, group: MISSION_GROUP, rule: `Handover: ${x.rule}`,
  }));
}

/** The Handover rules the plan itself fails: a skyline run on such a plan could only spend its rounds on them. */
export async function baselineFailures(plan: Plan, file = MISSION_FILE): Promise<string[]> {
  const block = JSON.parse(await Bun.file(resolve(root, plan.civic.block)).text());
  const mission = parse(await Bun.file(resolve(root, file)).text());
  return failedMission(missionChecks(plan, block, mission));
}

/** The ids of the Handover rules a candidate fails. */
export const failedMission = (checks: Rule[]): string[] =>
  [...new Set(checks.filter(x => x.group === MISSION_GROUP && !x.ok).map(x => x.id))];

/**
 * Who broke a Handover rule: Datum if it already fails on the candidate with Datum's edits alone (the structures and
 * the rail), Vantage if it fails only once his stations are in. `alone`: the Handover checks of the Datum-only plan,
 * or undefined when they could not be run (then Datum, whose edits the stations stand on).
 */
export function brokenBy(failed: string[], alone: Rule[] | undefined): Map<string, Owner[]> {
  const datum = new Set((alone ?? []).filter(x => !x.ok).map(x => x.id));
  return new Map(failed.map(id => [id, alone && !datum.has(id) ? ['Vantage'] : ['Datum']] as [string, Owner[]]));
}
