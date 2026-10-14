// The roles the Handover names (GDD §07; brief 010 B, D) are the mission's own: contracts/missions/handover/mission.json `roles`
// (suborned.mission/3: employer, position or role, mission point, shift window). This file holds no copy; it casts them with the
// overlay's own resolver (tools/missions/handover/cast.ts, rule M-2), so P-6 and M-2 cast the same people and pin the same ids.
import type { V } from '../plan/geom';
import { toPlanFn } from '../plan/civic';
import type { Person } from '../../contracts/people/people.schema';
import { castRoles, type Cast, type CastMission } from '../missions/handover/cast';
import type { Scope } from './scope';

export type { Cast };
export type MissionIn = CastMission & { destinations: { name: string; at: [number, number]; surface: string }[] };
export const startMin = (m: { clock: { startsAt: string } }) => { const [h, mi] = m.clock.startsAt.split(':').map(Number); return h! * 60 + mi!; };

/** Who the Handover can place in each role the mission names (P-6: employer, position, shift, place). The role's need, approach
 *  and regularOf are M-2's (the overlay passes the rules); here they are not read, so a stand-in cast still passes P-6. */
export function castHandover(persons: Person[], scope: Scope, mission: MissionIn, plan: { civic: unknown }): Cast[] {
  const at = new Map(scope.places.map(p => [p.id, p.at as V])), plot = new Map(scope.places.map(p => [p.id, p.plot]));
  return castRoles(persons, mission, { toPlan: toPlanFn(plan.civic), placeAt: id => at.get(id), polyOf: id => scope.bayPolys[id], plotOf: id => plot.get(id) });
}
