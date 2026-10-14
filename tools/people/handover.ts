// The roles the Handover names (GDD §07; brief 010 B, D) are the mission's own: contracts/missions/handover/mission.json `roles`
// (suborned.mission/3: employer, position or role, mission point, shift window). This file holds no copy; it casts them with the
// overlay's own resolver (tools/missions/handover/cast.ts, rule M-2), so P-6 and M-2 cast the same people and pin the same ids.
import type { V } from '../plan/geom';
import { toPlanFn } from '../plan/civic';
import type { Person } from '../../contracts/people/people.schema';
import { castRoles, type Cast, type CastMission, type CastOptions, type CastRules } from '../missions/handover/cast';
import type { Scope } from './scope';

export type { Cast };
export type MissionIn = CastMission & { destinations: { name: string; at: [number, number]; surface: string }[] };
/** The mission's start in day minutes, seconds included (17:24:30 → 1044.5). */
export const startMin = (m: { clock: { startsAt: string } }) => { const [h, mi, s] = m.clock.startsAt.split(':').map(Number); return h! * 60 + mi! + (s ?? 0) / 60; };

/** What the roles read of the people rules (temperament refusals, each need's approaches). */
export const castRulesOf = (r: { temperaments: Record<string, { refuses: readonly string[] }>; needs: { list: Record<string, { metBy: readonly string[] }> } }): CastRules => ({ temperaments: r.temperaments, needs: r.needs.list });

/** Who the Handover casts for each role the mission names (P-6, the same casting as M-2): employer, position, shift and place, and
 *  the role's need, approach and regularOf under `rules`. Without `rules` only the place counts (a fallback for naming a
 *  cast that cannot meet the conditions; the checker never uses it). */
export function castHandover(persons: Person[], scope: Scope, mission: MissionIn, plan: { civic: unknown }, rules?: Parameters<typeof castRulesOf>[0], opts?: CastOptions): Cast[] {
  const at = new Map(scope.places.map(p => [p.id, p.at as V])), plot = new Map(scope.places.map(p => [p.id, p.plot]));
  return castRoles(persons, mission, { toPlan: toPlanFn(plan.civic), placeAt: id => at.get(id), polyOf: id => scope.bayPolys[id], plotOf: id => plot.get(id) }, rules && castRulesOf(rules), opts);
}
