// A skyline candidate, built and judged: the proposals applied to the plan (apply.ts), the timetable the moved stations
// leave stale derived again (retime.ts), and every rule run on the result: the plan and world rules (check.ts) and the
// Handover's (mission.ts). The run and its replay both go through here, so they cannot disagree about what a
// candidate is.
import type { Plan } from '../../plan/plan';
import type { Check } from '../../plan/checks';
import { applySkyline, type Applied, type Known, type Owner } from './apply';
import { checkCandidate, type Checked } from './check';
import { MISSION_GROUP, missionChecks, type Mission } from './mission';
import { retime } from './retime';
import type { SkylineOut, StationsOut } from './schemas';

type Proposals = { datum?: SkylineOut; vantage?: StationsOut };

/** Who hears a timetable that cannot be derived: Datum when his rail edits changed the lines, else Vantage. */
const timetableOwner = (outs: Proposals): Owner =>
  ((outs.datum?.rail.lines.length ?? 0) + (outs.datum?.rail.remove.length ?? 0) ? 'Datum' : 'Vantage');

/** The proposals applied, with the timetable derived again when the sky rail changed. */
export async function candidatePlan(
  input: Plan, outs: Proposals, version: string, known: Known, block: unknown,
): Promise<Applied> {
  const applied = applySkyline(input, outs, version, known);
  const timed = await retime(input, applied.plan, block, timetableOwner(outs));
  return { plan: timed.plan, problems: [...applied.problems, ...timed.problems] };
}

/**
 * Every rule on the plan: the plan and world rules, then the Handover's. A candidate that cannot be built has no rules
 * to run.
 */
export async function checkAll(plan: Plan, block: unknown, mission: Mission, kit?: string): Promise<Checked> {
  const checked = await checkCandidate(plan, block, kit);
  if (checked.error) return checked;
  let extra: Check[];
  try { extra = missionChecks(plan, block, mission); } catch (e) {
    extra = [{
      id: 'HO-0', group: MISSION_GROUP, rule: 'Handover: the rules run on the candidate', value: 'failed',
      limit: 'runs', ok: false, detail: e instanceof Error ? e.message : String(e),
    }];
  }
  return { ...checked, ok: checked.ok && extra.every(x => x.ok), checks: [...checked.checks, ...extra] };
}
