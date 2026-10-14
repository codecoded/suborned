// The world rules, registered: B (buildings), L (levels), MS (megastructures), F (frontage), SF (streetscape), K (kit fit), SG / P / SK (signatures, portals, skyline) rules and
// F-7, SF-18, L-8, SF-19, SF-20, SF-21, CD-1..4 (tools/world/rules), run on the generator's output for a plan. `bun tools/plan/cli.ts` runs them after the plan rules; `bun run world` refuses to export
// a world that fails them. They need the generated world, which takes seconds: it is cached by content hash (world-ctx.ts), so a
// repeat run costs the rules alone. A plan without the world standards (a recorded run's plan) has nothing to check here.
import type { World } from '../../contracts/world/world.schema';
import { worldOf } from '../world/build';
import { withLook } from '../world/civic/look';
import type { Generated } from '../world/gen';
import { withKit } from '../world/gen/kitrefs';
import { extraRules } from '../world/rules';
import type { Check } from './checks';
import type { Civic } from './civic';
import type { Derived, Plan } from './plan';
import { kitFitChecks } from './kit-fit';
import { levelChecks, megaChecks } from './level-checks';
import { signatureChecks } from './signature-checks';
import { streetChecks } from './street-checks';
import { streetRules } from './street-rules';
import { buildingChecks, frontageChecks } from './world-checks';
import { type WCtx, ctxOf } from './world-ctx';

/** True if the plan carries the standards these rules read (buildings, levels, frontage, streetscape) and its megastructures. */
export const hasWorldStandards = (plan: Plan) => !!(plan.standards?.buildings && plan.standards?.levels && plan.standards?.frontage && plan.standards?.streetscape);

/** Every world rule on an already-built context. */
export const worldRules = (c: WCtx): Check[] => [...buildingChecks(c), ...levelChecks(c), ...megaChecks(c), ...frontageChecks(c), ...streetChecks(c), ...streetRules(c), ...kitFitChecks(c), ...signatureChecks(c), ...extraRules(c)];

/** The world rules for a plan: generated (or read from the cache) here unless the caller already has the world. */
export function worldChecks(plan: Plan, d: Derived, civ: Civic, v?: { w: World; gen: Generated }): Check[] {
  return hasWorldStandards(plan) ? worldRules(ctxOf(plan, d, civ, v)) : [];
}

/** The files a run pinned that the world is generated from: `kit` a kit.json or the kit digest, `look` the civic-look.json copy. Absent: the file in contracts/world. */
export type Pinned = { kit?: string; look?: string };

/**
 * The world rules on a world generated in process, never through the cache, from the kit and the civic look a run pinned (default: contracts/world).
 * A replay that runs beside others must not share the cache file, and a pinned input is not what the cache key names.
 */
export function worldChecksUnder(plan: Plan, d: Derived, civ: Civic, pin: Pinned = {}): Check[] {
  if (!hasWorldStandards(plan)) return []; // nothing to generate: the world standards are what the rules read (a recorded run's plan has none)
  return withKit(pin.kit, () => withLook(pin.look, () => { const { world, gen } = worldOf(plan, d, civ); return worldRules(ctxOf(plan, d, civ, { w: world, gen })); }));
}
