// The world generator: one deterministic pass from the plan to every generated layer. Seeded by element id throughout.
import type { Connector, Crossing, Deck, Fabric, Facade, Frontage, Skin, SignatureOut, KitRef, Mass, Prim, Roof, StreetItem, Takedown } from '../../../contracts/world/world.schema';
import { type V, inPoly as inPolyV } from '../../plan/geom';
import { type Ctx, rv } from './common';
import { type Travel, generateConnectors } from './connectors';
import { buildingHost, generateFrontages } from './frontage';
import { type Bld, generateMasses, towerSites } from './masses';
import { generateMegas } from './mega';
import { signKit } from './kitrefs';
import { generateRoofs } from './roofs';
import { generateSignatures, generateStructures } from './signatures';
import { generateStreet } from './street';
import type { CrossingLog } from './street/crossings';
import type { CapLog } from './street/capstone';
import { type RoadFinishLog, dressRoadFinish } from './street/roadfinish';
import { type KerbArcLog, dressKerbArcs } from './street/kerbarcs';
import { activeMouths } from './street/footway';
import { dressConnectors } from './dress';
import { generateSkins } from './skins';
import { adjacent } from './facades';
import { type FacesOut, dressFaces, occOf } from './faces';
import { catalogued } from './kitrefs';
import { returnsOf, slabRing } from './kerbreturn';
import { colourLife } from './colour';
import { type ShopFitLog, fitShopfronts } from './shopfit';
import type { DeckRec, Host, MegaOut } from './types';

export type Generated = {
  signatures: SignatureOut[]; skins: Skin[]; fabric: Fabric; facades: Facade[]; facadeWhy: Map<string, string>; faceMetres: Map<string, { street: number; kit: number }>; buildings: Mass[]; slabs: Mass[]; roofs: Roof[]; frontages: Frontage[]; connectors: Connector[]; street: StreetItem[];
  shopFit: ShopFitLog; // brief 072-C (Aster's ruling 2): own shopfront boxes clamped under the fascia of the every-face piece behind them, and those it would make too low
  faces: FacesOut; // brief 072-C: the every-face pass's log (faces dressed and the gaps, with the pieces that would dress them)
  decks: DeckRec[]; megas: { id: string; parts: Mass[]; prims: Prim[]; gateway?: KitRef & { at: V } }[]; blds: Bld[]; megaOut: MegaOut[]; issues: string[]; travel: Travel[];
  crossings?: Crossing[]; crossingLog?: CrossingLog; // streetscape.crossings.strip only
  takedown?: Takedown; // brief 013: present when the civic block has a service door
  groundPieces?: StreetItem[]; // brief 026: the capstone programme's flat ground pieces (world.ground.pieces), kept out of the street layer so the street rules (SF-1..SF-8) never judge paving
  capstone?: CapLog; // brief 026: what the capstone programme placed and the places no rule could fill
  kerbArcs?: KerbArcLog; // brief 028: the pearl kerb laid round every kerb return (kind kerb-return)
  roadFinish?: RoadFinishLog; // brief 028 Road: what the road finish laid (its pieces are in groundPieces, kinds road-ribbon, road-shoulder, road-pinstripe, road-lozenge, road-seam) and what it left
};

export function generate(ctx: Ctx, stops: Mass[], stations: Mass[]): Generated {
  const sites = towerSites(ctx), { masses, blds, facades: fac } = generateMasses(ctx, sites), megaOut = generateMegas(ctx, sites);
  // brief 072-C: the every-face pass over every plot building and tower complex, against every neighbour's walls (plot buildings, towers, the civic block, megastructure parts)
  const ownerOf = (id: string) => id.split('/').slice(0, id.startsWith('tower/') ? 2 : 3).join('/');
  const occ = [...masses.filter(m => !/\/band-\d+$/.test(m.id)).map(m => occOf(ownerOf(m.id), m.ring, m.base, m.base + m.height)),
    ...ctx.civ.buildings.flatMap(b => [occOf(`civic/${b.id}`, b.poly, ctx.pav + b.z, ctx.pav + b.z + b.height), ...(b.towers ?? []).filter(t => t.poly).map(t => occOf(`civic/${b.id}`, t.poly!, ctx.pav + b.z, ctx.pav + b.z + t.height))]),
    ...megaOut.flatMap(m => m.parts.map(p => occOf(m.id, p.ring, p.base, p.base + p.height)))];
  const faces = dressFaces(ctx, { blds: blds.map(b => ({ id: b.id, plot: b.plot, area: b.area, use: b.use, kind: b.kind, n: b.n, ring: b.ring, tower: b.tower, play: !!ctx.B.caps?.[b.area]?.play, top: b.z })), masses, occ, facades: fac.facades, catalogue: catalogued });
  fac.facades.push(...faces.facades);
  const solids: V[][] = [...masses.filter(m => m.base < 1).map(m => m.ring), ...megaOut.flatMap(m => m.parts.filter(p => p.base < 1).map(p => p.ring))];
  const conn0 = generateConnectors(ctx, megaOut, stations, solids);
  // the dressed form of stair towers and open lifts (a lift inside a leg or a stair core keeps the enclosure as its face)
  const conn = { ...conn0, connectors: dressConnectors(conn0.connectors, megaOut.flatMap(m => m.parts.map(p => ({ ring: p.ring, z0: p.base, z1: p.base + p.height })))) };
  const decks: DeckRec[] = [...megaOut.flatMap(m => m.decks), ...conn.decks];
  const hosts: Host[] = [...blds.filter(b => !b.tower).flatMap(b => buildingHost(ctx, b) ?? []), ...megaOut.flatMap(m => m.hosts)];
  const fit = fitShopfronts(linkFacades(generateFrontages(ctx, hosts), fac.facades), fac.facades);
  const frontages = signKit(ctx, fit.frontages, megaOut, masses);
  const roofs = generateRoofs(ctx, blds, megaOut.flatMap(m => m.terraces ?? []));
  // procedural skins (grammar §2-3) for every plot building; its speed bands carry the skin's accent
  const plain = blds.filter(b => !b.tower), near = (id: string) => { const me = plain.find(b => b.id === id)!; return plain.filter(o => o.id !== id && o.plot === me.plot && adjacent(me.ring, o.ring)).map(o => o.id); };
  const skins0 = generateSkins(ctx, blds, masses, near), accentOf = new Map(skins0.map(s => [s.building, s.accent]));
  for (const m of masses) if (/\/band-\d+$/.test(m.id)) m.surface = accentOf.get(m.id.slice(0, m.id.lastIndexOf('/'))) ?? m.surface;
  // grammar §2a colour life: faience, spandrels, fins, balcony awnings and banners per skin; flags along the Parade, hotels and civic fronts
  const life = colourLife(ctx, blds, masses, skins0, fac.facades, frontages), skins = life.skins;
  const { items: street, flags, crossings, crossingLog, takedown, capstone, groundPieces } = generateStreet(ctx, { buildings: masses, megas: megaOut, decks, stops, stations, connectors: conn.connectors, frontages });
  // brief 028 Road (Sky 2026-10-22 "can try"): the testbed's road finish, laid by rule on every kerb face; opt-in on standards.streetscape.roads and the pieces adopted in the kit (a pinned plan or kit gets none)
  const rf = dressRoadFinish(ctx, { crossings: crossings ?? [], platforms: stops.map(s => s.ring), mouths: activeMouths(ctx) }); // brief 061: the continuous footways' mouths: the higher road's ribbon runs on across, the lane's stops short of the ramp
  const ka = dressKerbArcs(ctx, { strips: (crossings ?? []).map(c => c.strip as V[]), drives: (ctx.civ.drives ?? []).map(d => d.poly as V[]) }); // brief 028: after the crossings, so a stone never lies across a crossing strip
  const flat = [...(groundPieces ?? []), ...(ka?.items ?? []), ...(rf?.items ?? [])];
  const slabs: Mass[] = ctx.d.plots.map(p => ({ id: `plot/${p.id}`, surface: p.use, ring: slabRing(ctx, p).map(rv), base: 0, height: ctx.pav })); // brief 028: the kerb side's corners are rounded (kerbreturn.ts); the building side and the building line are as the plan has them
  // brief 028 (kerbReturn.bend): the outside of a road's bend is concentric with the inside corner's return; the fillet is pavement laid into the road's corner (a slab of its own, in the use of the plot it meets), never a change to a plot
  for (const r of returnsOf(ctx)) if (r.bend) { const probe: V = [r.at[0] + (r.n1[0] + r.n2[0]) * 0.4, r.at[1] + (r.n1[1] + r.n2[1]) * 0.4]; slabs.push({ id: `plot/${r.id}`, surface: ctx.d.plots.find(p => !p.civic && inPolyV(probe, p.polyV))?.use ?? ctx.d.plots.find(p => p.id === r.plot)!.use, ring: r.cut.map(rv), base: 0, height: ctx.pav }); }
  return { signatures: [...generateSignatures(ctx), ...generateStructures(ctx)], skins, fabric: { awnings: life.awnings, banners: life.banners, flags }, facades: fac.facades, faces, shopFit: fit.log, facadeWhy: fac.why, faceMetres: fac.metres, buildings: masses, slabs, roofs, frontages, connectors: conn.connectors, street, decks, megas: megaOut.map(m => ({ id: m.id, parts: m.parts, prims: m.prims, ...(m.gateway ? { gateway: m.gateway } : {}) })), blds, megaOut, issues: conn.issues, travel: conn.travel, ...(crossings ? { crossings, crossingLog } : {}), ...(takedown ? { takedown } : {}), ...(capstone ? { capstone } : {}), ...(flat.length ? { groundPieces: flat } : {}), ...(rf ? { roadFinish: rf.log } : {}), ...(ka ? { kerbArcs: ka.log } : {}) };
}

/** A DeckRec as the export's Deck. */
/** A tenancy standing on a span a kit facade piece covers names that piece (its shopfront is in the piece; the sign stays). */
function linkFacades(fronts: Frontage[], facades: Facade[]): Frontage[] {
  const by = new Map<string, Facade[]>();
  // brief 072-C: only the street pieces carry a shopfront; under a side bay, service wall or stack module the tenancy keeps its own
  for (const f of facades) if (f.form === 'bay' || f.form === 'end' || f.form === 'corner') (by.get(f.building) ?? by.set(f.building, []).get(f.building)!).push(f);
  return fronts.map(f => {
    const mid: V = [(f.a[0] + f.b[0]) / 2, (f.a[1] + f.b[1]) / 2];
    const hit = (by.get(f.host) ?? []).find(x => {
      const u: V = [x.b[0] - x.a[0], x.b[1] - x.a[1]], L = Math.hypot(u[0], u[1]) || 1, s = ((mid[0] - x.a[0]) * u[0] + (mid[1] - x.a[1]) * u[1]) / L, d = Math.abs(((mid[0] - x.a[0]) * u[1] - (mid[1] - x.a[1]) * u[0]) / L);
      return d < 0.8 && s > -0.1 && s < L + 0.1;
    });
    return hit ? { ...f, facade: hit.id } : f;
  });
}

export const toDeck = ({ level: _l, rect: _r, host: _h, ...d }: DeckRec): Deck => d;
