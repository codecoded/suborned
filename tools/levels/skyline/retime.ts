// The timetable a moved station leaves stale. The plan's timetable holds run times and journey totals that code derives
// from the stop table (tools/levels/timetable.ts); when a skyline candidate changes the sky rail, they are derived
// again
// from the candidate's own stop table. The speeds, dwells and headways, and the journeys' stops, stay as the plan has
// them: this objective changes where the stations are, not how the line is run.
import type { Plan } from '../../plan/plan';
import { inProcess } from '../checker';
import type { Pulse, Timetable } from '../schemas';
import { deriveTimetable } from '../timetable';
import type { Owner, Problem } from './apply';

type Stop = { id: string; interchange?: string };
type Line = { id: string; stops?: Stop[]; stations?: Stop[] };
type Leg = { from: string; to: string };
type Journey = Timetable['journeys'][number];
type WithTimetable = { timetable?: Timetable };

/** The interchange partner of a stop: the stop it names, or the one that names it. */
function partnerOf(plan: Plan): (ref: string) => string | undefined {
  const pairs = new Map<string, string | undefined>();
  for (const l of [...plan.tram, ...plan.skyrail] as unknown as Line[]) {
    for (const s of l.stops ?? l.stations ?? []) pairs.set(`${l.id}/${s.id}`, s.interchange);
  }
  return ref => pairs.get(ref) ?? [...pairs].find(([, other]) => other === ref)?.[0];
}

/**
 * The legs of a journey, rebuilt from its `via` list: a journey ends a leg at each via and starts the next at that
 * stop's interchange partner.
 */
function legsOf(j: Journey, partner: (ref: string) => string | undefined): Leg[] | string {
  const legs: Leg[] = [];
  let start = j.from;
  for (const via of j.via) {
    legs.push({ from: start, to: via });
    const next = partner(via);
    if (!next) return `${via} has no interchange partner`;
    start = next;
  }
  legs.push({ from: start, to: j.to });
  return legs;
}

/**
 * The candidate with its timetable derived again, when the sky rail changed. A candidate whose timetable cannot be
 * derived from its stations (a journey's stop is gone, a line it names is gone) is a problem for `owner`.
 */
export async function retime(
  input: Plan, plan: Plan, block: unknown, owner: Owner,
): Promise<{ plan: Plan; problems: Problem[] }> {
  const old = (plan as unknown as WithTimetable).timetable;
  // a timetable without speeds carries proposed times, not derived ones: nothing to derive again from
  if (!old || old.lines.some(l => l.speed === undefined) || Bun.deepEquals(input.skyrail, plan.skyrail)) {
    return { plan, problems: [] };
  }
  let report: Awaited<ReturnType<typeof inProcess>>;
  try { report = await inProcess(plan, true, block); } catch {
    return { plan, problems: [] }; // the checker reports a candidate it cannot build
  }
  const covered = new Set(old.lines.map(l => l.line)), partner = partnerOf(plan);
  const problems: Problem[] = [], journeys: Pulse['journeys'] = [];
  for (const [i, j] of old.journeys.entries()) {
    const legs = legsOf(j, partner);
    if (typeof legs === 'string') {
      problems.push({ owner, text: `timetable: journey ${i + 1} cannot be rebuilt: ${legs}` });
    }
    else journeys.push({ legs });
  }
  if (problems.length) return { plan, problems };
  const lines = old.lines.map(({ line, headwaySec, dwellSec, speed }) => ({ line, headwaySec, dwellSec, speed }));
  const { openQuestions, notes, sources, handoffs } = old;
  const pulse = { lines, journeys, openQuestions, notes, sources, handoffs };
  const table = {
    stops: report.stops.filter(s => covered.has(s.line)), stations: report.stations.filter(s => covered.has(s.line)),
  };
  const derived = deriveTimetable(pulse as unknown as Pulse, plan, table);
  if (derived.faults.length) {
    return { plan, problems: derived.faults.map(text => ({ owner, text: `timetable: ${text}` })) };
  }
  const timetable = { ...old, lines: derived.timetable.lines, journeys: derived.timetable.journeys };
  return { plan: { ...plan, timetable } as unknown as Plan, problems: [] };
}
