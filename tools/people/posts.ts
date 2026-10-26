// A post is one person's (brief 067 L; Pulse; Keel and Suture, Sky's 0.2.122 play: "x2" over Vale's car was two to four guards drawn on one point): what the expander and the
// checker both read of the Records Office's staffed doors, in one place.
//  - held: the Handover plays its cast people by their role for the whole mission, so a post a cast role stands (the door guard's, mission.json `roles`, `at` + `near`) is held through the
//    mission whatever his shift says; anyone else due at that place while the mission runs waits beside it (scope.postStands), and a relief to it stands there for her whole shift.
//  - early: a person walks a routine leg 0 to `crowd-pace-spread-pct` % faster than it is timed and waits at the next stop's place once there (rulebook crowd.pace; Pulse, brief 023), so a
//    relief who is due at a post at the minute its holder leaves stands on it up to that share of her walk earlier. The checker counts those seconds; the expander gives her a waiting point.
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { toPlanFn } from '../plan/civic';
import { dist, type V } from '../plan/geom';
import { NEAR_M } from '../missions/handover/cast';
import type { Casting } from './roles';
import type { Scope } from './scope';

/** The rulebook's crowd.pace spread, per cent: the walk of a leg may end this much early. */
export const PACE_SPREAD_PCT: number = (JSON.parse(readFileSync(join(import.meta.dir, '..', '..', 'contracts/rules/rulebook.json'), 'utf8')) as { values: Record<string, { v: number }> }).values['crowd-pace-spread-pct']!.v;
/** The most a person can be early at the end of a leg timed `legMin` minutes (a leg walked (100 + k) % as fast ends legMin × 100 / (100 + k) after it starts). */
export const earlyMin = (legMin: number): number => (legMin * PACE_SPREAD_PCT) / (100 + PACE_SPREAD_PCT);

/** The staffed door places a cast role holds through the mission, with the roles that hold them: each mission role with a destination holds the doors within its `near` (4 m by default)
 *  of it. Empty without a casting. */
export function heldPlaces(scope: Pick<Scope, 'places'>, c: Casting | undefined): Map<string, string[]> {
  const out = new Map<string, string[]>();
  if (!c) return out;
  const toPlan = toPlanFn((c.plan as { civic: Parameters<typeof toPlanFn>[0] }).civic);
  for (const r of c.mission.roles) {
    const d = r.at ? c.mission.destinations.find(x => x.name === r.at) : undefined;
    if (!d) continue;
    const point = toPlan(d.at as V);
    for (const pl of scope.places) if (pl.kind === 'door' && dist(pl.at as V, point) <= (r.near ?? NEAR_M)) out.set(pl.id, [...(out.get(pl.id) ?? []), r.id]);
  }
  return out;
}

/** Whether day minute `t` falls inside the mission window: after its start and no later than `missionMin` minutes on (a shift change inside it comes on while the cast person holds the post). */
export const inMission = (t: number, atMin: number, missionMin: number): boolean => { const d = (((t - atMin) % 1440) + 1440) % 1440; return d > 0 && d <= missionMin; };
