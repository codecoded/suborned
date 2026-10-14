// The people as the promoted cast makes them now (Redline H1, 2026-10-15): rule M-2 does not trust whatever file `bun run people` wrote last (a stand-in, a stale file for another
// mission or plan, an edited one). It expands the promoted cast (contracts/people/<plan id>.cast.json, held to its run first) with the plan, mission, rules, life and block it is
// checking, and casts the roles from that. At civic-block scale an expansion is a fraction of a second, and the same inputs give the same bytes.
import type { People } from '../../contracts/people/people.schema';
import { Cast } from '../../contracts/people/cast.schema';
import { sha256 } from '../gen/sha';
import { root } from '../rag/config';
import { checkPromotion } from './promoted';
import { expand } from './expand';
import { readTextsSync, world } from './inputs';

export type Current = { people?: People; problems: string[]; from?: string };
const memo = new Map<string, Current>();

/** Expand the promoted cast of `plan` for `mission` (the mission object the checker holds, or its text). Fails, naming why, when the promoted cast has drifted from its run or its
 *  expansion has gaps (a person with no need of their row, a stop no seat can take): a roles check on a cast that does not expand cleanly would be a check on nothing. */
export function currentPeople(plan: { id: string; civic: { block: string } }, mission: unknown, base: string = root): Current {
  const promo = checkPromotion(plan.id, base);
  if (!promo.ok) return { problems: [`the promoted cast ${promo.label} fails: ${promo.problems.join('; ')} (bun run verify)`] };
  const texts = readTextsSync(plan, mission as string | object, base), key = `${sha256(JSON.stringify(texts))}/${promo.record!.cast.sha256}`;
  const hit = memo.get(key); if (hit) return hit;
  const w = world(texts, 'civic-block'), gen = expand(w.ctx(Cast.parse(JSON.parse(promo.castText!))));
  const out: Current = gen.gaps.length
    ? { problems: [`the promoted cast ${promo.label} expands with ${gen.gaps.length} gap(s): ${gen.gaps.slice(0, 3).join('; ')}${gen.gaps.length > 3 ? '; …' : ''} (bun run people)`] }
    : { people: gen.people, problems: [], from: promo.label };
  memo.set(key, out);
  return out;
}
