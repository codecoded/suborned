// Megastructures as parts: deck slabs on column grids, bridges, halls, towers, cranes and the Skyport saucer, built per
// kind from the plan's sited footprints (brief 004 A1). A plot a megastructure covers has its own mass replaced:
// masses.ts carves every footprint out of the plot's building line, so nothing is built twice.
import type { Ctx } from './common';
import type { Site } from './masses';
import type { MegaOut } from './types';
import { arcade } from './mega/arcade';
import { harbourDeck } from './mega/harbourDeck';
import { pier } from './mega/pier';
import { terraces } from './mega/terraces';

export function generateMegas(ctx: Ctx, sites: Site[]): MegaOut[] {
  return (ctx.plan.megastructures ?? []).map(m => {
    switch (m.kind) {
      case 'stacked arcade': return arcade(ctx, m, sites);
      case 'harbour deck': return harbourDeck(ctx, m);
      case 'pier tower': return pier(ctx, m);
      case 'terraced housing over a podium': return terraces(ctx, m);
      default: throw new Error(`megastructure ${m.id}: unknown kind "${m.kind}" (not in the rulebook)`);
    }
  });
}
