// suborned.world/2 (v2 adds roofs, frontages, connectors, street, ground.plots, deck groups and megastructure prims; plot slabs moved from buildings to ground.plots): the derived world, written by `bun run world` (tools/world/export.ts) to out/world/<plan id>.world.json.
// Frame: x east, y north, metres; Unity = (x, height, y). Heights are metres above the carriageway datum (0).
// Read by unity/Assets/Suborned/Editor/World/WorldReader.cs. Example: unity/Assets/Suborned/Editor/World/Samples/sample.world.json.
// Every element has a stable id; rings are simple polygons; nothing here is placed by hand.
export type V = [number, number];
/** A kit piece standing in for an element (contracts/world/kit.json). scale = [across, up, depth] on the kit's size; yaw (plan frame, from +x toward +y) turns the piece's +X; its front (+Z) faces yaw + 90°. Absent yaw = the element's own. */
export type KitRef = { id: string; scale: [number, number, number]; yaw?: number };
/** Where an element's look comes from: a kit piece (`kit` set) or procedural geometry (a stand-in, listed so gaps are visible). */
export type Source = 'kit' | 'procedural';

export type World = {
  schema: 'suborned.world/2';
  source: string; // plan path, id and version
  view: { yaw: number; pitch: number; zoom: number; focus: V }; // play camera: compass yaw (0 = north), pitch, ortho size
  ground: {
    roads: Road[]; foot: Road[]; medians: Mass[]; circuses: Circus[];
    water: Flat[]; quays: Mass[]; flats: Flat[]; // flats: land under everything and the plan's `beyond`
    plots: Mass[]; // one 0.18 m pavement slab per plot (the ground a plot stands on, courts and setback strips included)
  };
  transit: { tram: Track[]; skyRail: SkyRail[]; stops: Mass[]; stations: Mass[] };
  buildings: Mass[]; // generated building parts (podium, body, upper, tower-lower, tower-upper), id bld/<plot>/<n>/<part>; none under a megastructure
  roofs: Roof[]; // one programme per building roof
  frontages: Frontage[]; // tenancies along building lines, megastructure facades and decks
  connectors: Connector[]; // stairs, ramps, escalators, lifts, stair towers: every deck and concourse reaches the street
  street: StreetItem[]; // trees, lamps, furniture, transit kits, station entrances, watchers
  levels: Level[]; // standards.levels.list: street, deck, concourse; decks carry their columns
  megastructures: { id: string; parts: Mass[]; prims: Prim[]; gateway?: KitRef & { at: V } }[]; // gateway: a kit arch standing on the megastructure's quay (its yaw turns the piece's +X); parts: extruded rings (bodies, halls, legs); prims: cranes, saucer, vaults
  civic: Mass[]; // the civic block: buildings, structures and objects at exact size
  landmarks: Landmark[];
  gates: Mass[];
  counts: Record<string, number>; // elements per layer, as the Unity build must report them
};

/** Carriageway ribbon along `line` at `level`, with kerb strips either side. Foot paths use kerbWidth 0. */
export type Road = { id: string; surface: string; kerbSurface: string; line: V[]; width: number; level: number; kerbWidth: number; kerbHeight: number };
export type Circus = { id: string; surface: string; islandSurface: string; centre: V; radius: number; islandRadius: number; level: number; islandHeight: number };
export type Flat = { id: string; surface: string; ring: V[]; level: number };
/** Extruded footprint from base to base + height. */
export type Mass = { id: string; surface: string; ring: V[]; base: number; height: number };
/** spacing > 0: a double track, two ribbons spacing apart either side of `line`. */
export type Track = { id: string; surface: string; line: V[]; width: number; level: number; depth: number; spacing: number };
/** Beam(s) with the top at `height`; pylons from `ground` to the beam soffit; a double line's pylon carries a cross-head capDepth deep. */
export type SkyRail = {
  id: string; surface: string; pylonSurface: string; line: V[]; pylons: V[];
  height: number; width: number; depth: number; pylonSize: number; ground: number; spacing: number; capDepth: number;
};
export type Level = { id: string; name: string; height: number; decks: Deck[]; stairs: Stair[] };
/** Slab with its top at the level height. `group` = the walkable piece the deck belongs to (decks touching at one level share it). */
export type Deck = { id: string; surface: string; columnSurface: string; ring: V[]; columns: V[]; thickness: number; columnSize: number; ground: number; group: string };
export type Stair = { id: string; surface: string; foot: V; head: V; bottom: number; top: number; width: number; riser: number };
/** shape: sphere (a ball of `radius` standing on `base`) or cylinder (radius, base → base + height). */
export type Landmark = { id: string; surface: string; shape: 'sphere' | 'cylinder'; centre: V; radius: number; base: number; height: number; source?: Source; kit?: KitRef; kits?: (KitRef & { at: V })[]; why?: string }; // why = what stopped the kit piece fitting; kits = several pieces on one landmark (the skyport's pads, standing on base + height)

/** Primitive solid. Plan frame; `c` = centre in plan, `base` = lowest z, `yaw` = radians from +x toward +y.
 *  size [a, b, h]: a along yaw, b across it. box: a × b × h · cyl: a = b = diameter · sphere: a = b = h = diameter (base = bottom) ·
 *  vault: half-cylinder lying along yaw, span b, length a, rise h · saucer: lens a = b = diameter, h thick · wedge: box whose top slopes from 0 at -b/2 to h at +b/2. */
export type Prim = { id: string; shape: 'box' | 'cyl' | 'sphere' | 'vault' | 'saucer' | 'wedge'; surface: string; c: V; base: number; size: [number, number, number]; yaw: number };
export type Roof = {
  id: string; building: string; programme: string; surface: string; z: number; // z = top of the roof slab
  c: V; size: V; yaw: number; // the usable roof rect the programme was fitted to (after the parapet inset)
  parapet: number; parts: Prim[];
  kit?: KitRef & { at: V }; // the programme's kit piece (hover-pad: SkyPad_Roof) standing on the slab at `at`; its procedural stand-in is then not in `parts`
  form?: 'terrace'; // a stepped garden terrace: too shallow for the kit's programmes, planted along its length
};
/** One tenancy: a frontage a→b on a facade at z, facing `normal`. Prims are in world frame; `sign` carries motif + icon + palette key, never text. */
export type Frontage = {
  id: string; host: string; level: string; z: number; trade: string; area: string; street: string; a: V; b: V; w: number; normal: V; yaw: number;
  hours: [number, number] | 'service'; staffed: boolean; spill: { kind: string; depth: number }; lobby: boolean; anchor: boolean;
  front: Prim; entrance: Prim; canopy: Prim; sign: Prim & { motif: string; icon: string; palette: string }; service?: Prim;
  source?: Source; signKit?: KitRef & { at: V; base: number; mount: 'blade' | 'wall' }; // the fascia sign stays procedural; signKit adds a blade or wall sign at `at`, its lowest point at `base`
};
export type Connector = {
  id: string; kind: 'stair' | 'ramp' | 'cartRamp' | 'escalator' | 'lift' | 'goodsLift' | 'stairTower'; host: string; serves: string[]; // serves = ids of the decks (or station platforms) it lands on
  from: string; to: string; foot: V; head: V; bottom: number; top: number; width: number; stepFree: boolean; ring: V[]; yaw: number;
};
export type StreetItem = {
  id: string; kind: string; at: V; yaw: number; z: number; size: [number, number, number]; surface: string; edge: string; // edge = street edge / host id
  sight: 'blocks' | 'glazed' | 'open'; staffed?: { trade?: string; occupation?: string; hours: [number, number] | 'service' }; variant?: string;
  source?: Source; kit?: KitRef; // wave 1: street furniture, plants, signs; absent on a layer the kit has not reached
};
