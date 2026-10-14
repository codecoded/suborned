import { checkPeople, type Checked } from '../../people/check';
import { expand } from '../../people/expand';
import type { World } from '../../people/inputs';
import type { PulseOut } from './schemas';

/** The checker on the cast of a round: expansion, rules, Handover. Shared by the run and its replay. */
export function checkCast(w: World, cast: PulseOut['cast']): { checked: Checked; people: ReturnType<typeof expand>['people'] } {
  const ctx = w.ctx(cast), gen = expand(ctx);
  return { checked: checkPeople(ctx, w.mission, w.plan, gen), people: gen.people };
}
