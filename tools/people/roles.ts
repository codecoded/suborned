// Names given by role (brief 010 follow-up): a pinned name part (rules names.pinned: Inez, Vale) lands on whoever the Handover
// casts in the mission role with the same employer and position or role, by the same casting M-2 and P-6 use
// (tools/missions/handover/cast.ts, called, not copied). Nothing here depends on the order the expander makes people in.
import type { Person } from '../../contracts/people/people.schema';
import { castHandover, type Cast, type MissionIn } from './handover';
import type { Rules } from './rules';
import type { Scope } from './scope';

export type Casting = { scope: Scope; mission: MissionIn; plan: { civic: unknown } };
export type Pinned = { index: number; role?: string; who?: string; why: string };

/** The cast under the rules; a role the rules cannot cast (no one meets its need) falls back to place alone, so a name still
 *  lands on someone and P-6 (rules) reports the failure. */
export function castForNames(persons: Person[], c: Casting, rules: Rules): Cast[] {
  const strict = castHandover(persons, c.scope, c.mission, c.plan, rules);
  if (strict.every(x => x.ok)) return strict;
  const place = castHandover(persons, c.scope, c.mission, c.plan);
  return strict.map((x, i) => (x.ok ? x : place[i]!));
}

/** For each rules.names.pinned entry: the person the Handover casts in its role. */
export function pinnedByRole(persons: Person[], c: Casting | undefined, rules: Rules): Pinned[] {
  const pins = rules.names.pinned;
  if (!c) return pins.map((_, index) => ({ index, why: 'no mission to cast' }));
  const cast = castForNames(persons, c, rules);
  return pins.map((pin, index) => {
    const i = c.mission.roles.findIndex(r => r.employer === pin.employer && r.position === pin.position && r.role === pin.role);
    if (i < 0) return { index, why: `the mission has no role of ${pin.position ?? pin.role} of ${pin.employer}` };
    const x = cast[i]!;
    return x.pinned ? { index, role: x.role, who: x.pinned, why: x.why } : { index, role: x.role, why: x.why };
  });
}
