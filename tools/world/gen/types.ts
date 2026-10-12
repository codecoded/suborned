// Records passed between the generators (not exported to Unity).
import type { V } from '../../plan/geom';
import type { Connector, Deck, KitRef, Mass, Prim } from '../../../contracts/world/world.schema';
import type { Rect } from './geo';

/** A facade run that tenancies may stand on: from a to b with outward normal n, at height z (street 0.18, deck 9.43, concourse 24). */
export type HostEdge = { a: V; b: V; n: V; kind: string; street: string; cornerA: boolean; cornerB: boolean; spill?: number };
export type Host = {
  id: string; level: string; z: number; area: string; mix?: Record<string, number>; edges: HostEdge[];
  free: boolean; // freestanding (kiosks, stalls): the frontage brings its own block
  pavement: number; // clear ground in front of the facade (spill room)
};
/** A deck in the generators: the export Deck plus where it is. */
export type DeckRec = Deck & { level: string; rect: Rect; host: string };
/** A stair-and-lift core inside a building volume (or a leg): rect on plan, from level to level. */
export type Core = { id: string; host: string; rect: Rect; from: string; to: string; lift: boolean; stair: boolean; stepFreeOnly?: boolean };
/** A stepped terrace roof the roofs generator plants (too shallow for a roof programme). */
export type Terrace = { id: string; host: string; rect: Rect; z: number };
/** `blocks`: deck-level solids a walker goes round (halls); `columns`: colonnade column centres (the street layer keeps clear of them). */
export type MegaOut = { gateway?: KitRef & { at: V }; terraces?: Terrace[]; blocks?: Rect[]; columns?: V[]; id: string; parts: Mass[]; prims: Prim[]; decks: DeckRec[]; hosts: Host[]; cores: Core[]; connectors: Connector[]; avoid: Rect[] };
