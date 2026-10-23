// The street tree species by item variant (brief 028 Palm, Sky 2026-10-22 "old palms"). One place, so the generator, the kit slot and the SF rules read the same numbers.
//   coconut          the avenue palm (streetscape.trees.species.coconut): pavement tree pits, walks, mall tree bands, the circus pavement; kit piece CoconutPalm
//   coconut-median   its median cut (species.coconut.median over the same crown: crownBase, top, spread, kit): the 4 m medians; kit piece CoconutPalm_Street
//   phoenix, tub     as they were
// A plan pinned before the retag (a recorded run's plan) names the avenue species `washingtonia`: it keeps that key and its numbers, so a replay reads what it was made with.
import type { V } from './geom';

export const MEDIAN_CUT = 'coconut-median';
export type Species = { pit: V; trunk?: number; crownBase: number; top: number; spread: number; kit?: string };
type Species0 = Record<string, any>;

/** The avenue species' key in a plan: `coconut`, or the legacy `washingtonia` of a plan pinned before the retag. */
export const avenueKey = (S: any): 'coconut' | 'washingtonia' => (S.trees.species.coconut ? 'coconut' : 'washingtonia');

/** The species an item of this variant stands for (undefined if the plan has none). A median cut is the avenue species with its `median` numbers laid over it. */
export function speciesOf(S: any, variant: string | undefined): Species | undefined {
  const all = S.trees.species as Species0, v = variant ?? avenueKey(S);
  if (v === MEDIAN_CUT) { const c = all.coconut; return c?.median ? { ...c, ...c.median } : c; }
  return all[v];
}

/** The variants the generator gives a tree on a pavement (avenue) and in a median under 6 m (median cut). */
export const avenueVariant = (S: any): string => avenueKey(S);
export const medianVariant = (S: any): string => (S.trees.species.coconut?.median ? MEDIAN_CUT : avenueKey(S));
