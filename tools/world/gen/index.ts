// The world generator: one deterministic pass from the plan to every generated layer. Seeded by element id throughout.
import type { Connector, Deck, Frontage, KitRef, Mass, Prim, Roof, StreetItem } from '../../../contracts/world/world.schema';
import type { V } from '../../plan/geom';
import { type Ctx, rv } from './common';
import { type Travel, generateConnectors } from './connectors';
import { buildingHost, generateFrontages } from './frontage';
import { type Bld, generateMasses, towerSites } from './masses';
import { generateMegas } from './mega';
import { signKit } from './kitrefs';
import { generateRoofs } from './roofs';
import { generateStreet } from './street';
import type { DeckRec, Host, MegaOut } from './types';

export type Generated = {
  buildings: Mass[]; slabs: Mass[]; roofs: Roof[]; frontages: Frontage[]; connectors: Connector[]; street: StreetItem[];
  decks: DeckRec[]; megas: { id: string; parts: Mass[]; prims: Prim[]; gateway?: KitRef & { at: V } }[]; blds: Bld[]; megaOut: MegaOut[]; issues: string[]; travel: Travel[];
};

export function generate(ctx: Ctx, stops: Mass[], stations: Mass[]): Generated {
  const sites = towerSites(ctx), { masses, blds } = generateMasses(ctx, sites), megaOut = generateMegas(ctx, sites);
  const solids: V[][] = [...masses.filter(m => m.base < 1).map(m => m.ring), ...megaOut.flatMap(m => m.parts.filter(p => p.base < 1).map(p => p.ring))];
  const conn = generateConnectors(ctx, megaOut, stations, solids);
  const decks: DeckRec[] = [...megaOut.flatMap(m => m.decks), ...conn.decks];
  const hosts: Host[] = [...blds.filter(b => !b.tower).flatMap(b => buildingHost(ctx, b) ?? []), ...megaOut.flatMap(m => m.hosts)];
  const frontages = signKit(ctx, generateFrontages(ctx, hosts));
  const roofs = generateRoofs(ctx, blds, megaOut.flatMap(m => m.terraces ?? []));
  const street = generateStreet(ctx, { buildings: masses, megas: megaOut, decks, stops, stations, connectors: conn.connectors, frontages });
  const slabs: Mass[] = ctx.d.plots.map(p => ({ id: `plot/${p.id}`, surface: p.use, ring: p.polyV.map(rv), base: 0, height: ctx.pav }));
  return { buildings: masses, slabs, roofs, frontages, connectors: conn.connectors, street, decks, megas: megaOut.map(m => ({ id: m.id, parts: m.parts, prims: m.prims, ...(m.gateway ? { gateway: m.gateway } : {}) })), blds, megaOut, issues: conn.issues, travel: conn.travel };
}

/** A DeckRec as the export's Deck. */
export const toDeck = ({ level: _l, rect: _r, host: _h, ...d }: DeckRec): Deck => d;
