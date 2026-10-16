// suborned.world/2 (v2 adds roofs, frontages, connectors, street, ground.plots, deck groups and megastructure prims; plot slabs moved from buildings to ground.plots; optional `crossings`, present when the plan carries streetscape.crossings.strip, whose parts are street items of the crossing-* kinds and signal-orb, brief 009; optional `takedown` and a street item's optional `stash`, present when the civic block has a service door, brief 013): the derived world, written by `bun run world` (tools/world/export.ts) to out/world/<plan id>.world.json.
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
  skins: Skin[]; // the procedural facade of every plot building (grammar §2-3): window family, accent and per-face bay rhythm; Unity draws walls the kit does not cover from these, never a glass grid
  fabric: Fabric; // grammar §2a colour life: balcony awnings, banners and flag masts (never red), seeded per building / frontage / street
  facades: Facade[]; // kit bays, ends and corners standing on the street faces of generated buildings (wave 2); the procedural wall on the covered span is hidden
  frontages: Frontage[]; // tenancies along building lines, megastructure facades and decks
  connectors: Connector[]; // stairs, ramps, escalators, lifts, stair towers: every deck and concourse reaches the street
  street: StreetItem[]; // trees, lamps, furniture, transit kits, station entrances, watchers
  crossings?: Crossing[]; // kerb-to-kerb crossings (present when the plan carries streetscape.crossings.strip): each is its landings (keep-clear footprints, not drawn), the strip between them, and its parts (street items: light bars or dots, signal orbs, kerb studs, inlay arcs)
  /** The coverage rules' doing (streetscape.crossings.coverage.rules; present when the plan carries them): the crossings each rule placed, the twins dropped (each named with the crossing it kept), the furniture moved out of a landing's way, and the sites left out for want of ground, each with why. SF-22 holds the crossings to it. */
  crossingRules?: { sites: Record<string, number>; twins: string[]; moved: string[]; left: string[] };
  levels: Level[]; // standards.levels.list: street, deck, concourse; decks carry their columns
  megastructures: { id: string; parts: Mass[]; prims: Prim[]; gateway?: KitRef & { at: V } }[]; // gateway: a kit arch standing on the megastructure's quay (its yaw turns the piece's +X); parts: extruded rings (bodies, halls, legs); prims: cranes, saucer, vaults
  civic: Mass[]; // the civic block: buildings, structures and objects at exact size
  takedown?: Takedown; // brief 013 (Vantage C, Pulse, Motive §8): where a body can be hidden, where a witness can report, where the response starts; derived by tools/world/gen/street/quiet.ts, present when the civic block is
  landmarks: Landmark[];
  signatures: SignatureOut[]; // plan signatures[] (SpaceHQ): the kit piece on its ground, no plot building on `ground`
  gates: Mass[];
  horizon: Horizon; // derived (grammar §9): the sea plane and the distant band of kit pieces beyond the district; nothing here is in the plan
  counts: Record<string, number>; // elements per layer, as the Unity build must report them
};

/** Carriageway ribbon along `line` at `level`, with kerb strips either side. Foot paths use kerbWidth 0. */
export type Road = { id: string; surface: string; kerbSurface: string; line: V[]; width: number; level: number; kerbWidth: number; kerbHeight: number };
export type Circus = { id: string; surface: string; islandSurface: string; centre: V; radius: number; islandRadius: number; level: number; islandHeight: number };
export type Flat = { id: string; surface: string; ring: V[]; level: number };
/** Extruded footprint from base to base + height. */
export type Mass = {
  id: string; surface: string; ring: V[]; base: number; height: number; source?: Source; kit?: KitRef & { at: V; base: number };
  /** Civic only (brief 008 B3): the play camera's architecture fade may thin this mass (a parasol, a frontage, a shelter: it stands between the camera and a person). */
  fade?: true;
  /** Civic only: the id of the kit mass that is this mass's ground storey. This mass starts above it (its `base` is that piece's top): draw no plinth, shopfront or ground floor at its base. */
  groundBy?: string;
  /** Civic only: a material this mass draws in place of its kit piece's own, by the piece's material slot (`{ DK_Water: 'DK_Pool_Civic' }` on the atom fountain). Absent = the piece's own. */
  materials?: Record<string, string>;
}; // civic objects (grammar §11): `kit` stands in for the extrusion (the ring stays the footprint)
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
  crown?: KitRef & { at: V }; // grammar §6: the kit crown (Crown_*) at the roof centre; absent on plan tower sites (their regen order carries the crown)
  form?: 'terrace'; // a stepped garden terrace: too shallow for the kit's programmes, planted along its length
};
/** A kit facade piece on a building's street face. `a`→`b` is the span of the face it covers (a corner's `a`→`b` is its front cell; its side cell runs 8 m × scale from the vertex along the leaving edge); `kit.at` = footprint centre, `base` = pavement top.
 *  Bay: 6 m × x wide, body 14 m deep behind the face, a canopy (size z − 14) in front. End: closes a run at a party wall or court. Corner: 8 × 8 m quarter round at a convex 90° vertex. `edge` = index of the building ring's edge. */
export type Facade = { id: string; building: string; form: 'bay' | 'end' | 'corner'; storeys: number; kit: KitRef & { at: V }; base: number; a: V; b: V; edge: number };
/** A procedural facade skin (tools/world/gen/skins.ts). family: ribbon | porthole | balcony | shed (no curtain wall, ever). accent: the look surface of the
 *  building's one accent role (its speed bands carry it). glass: clear (`surface` 'glass'), ribbon width = share x bay width, never wider than maxPane, sill and
 *  head above each row's floor, set back `reveal`, chrome mullions every `mullion` m. pier: the stone between ribbons, with a chrome-or-stone `frame` round each. */
export type Skin = {
  id: string; building: string; family: 'ribbon' | 'porthole' | 'balcony' | 'shed'; accent: string;
  /** Grammar §2a colour life. accent: the building's one accent role (look.json `accent_*`; its speed bands, piers, spandrels and balcony awnings carry it);
   *  accent2: the second role, on its banners only; trim: gold (`accent_gold`): parapet coping bead, marquee edge, crown collar, fin edge beads. */
  accent2: string; trim: string;
  /** The ground storey in faience: plinth (`plinth` m from the pavement), a ground-top band (`band` m under the first upper floor) and shop bulkheads (`bulkhead`
   *  m above the plinth under each shopfront) in the family's plinth swatch (facade.json `<family>_p`); piers: the stone piers between shop bays (`pier` m wide)
   *  are in the accent instead of limestone. */
  faience: { swatch: 'family_p'; plinth: number; band: number; bulkhead: number; piers: boolean; pier: number };
  /** Spandrel panels in the accent: under every ribbon window (R bays) `h` m high ending at the sill, the ribbon's width (0.9 = floor to sill; 1.9 = the
   *  whole wall from the head of the storey below), and the solid front of every balcony bay (B), `balcony.w` x `balcony.h` at the floor; null = plain stone. */
  spandrel: { h: number; surface: string; balcony: { w: number; h: number } } | null;
  fins: Fin[]; // coral or teal vertical fins: one per procedural street corner, one over every lobby entrance
  /** Measured accent share of the procedural street walls (kit-faced spans excluded): `lower` over the ground storey + the first two upper storeys
   *  (target 0.20-0.30), `whole` over the full height (10-15%); `elements` = which §2a elements this building carries (>= 4). Null shares: no procedural street wall.
   *  target: the 20-30% band applies (>= 12 m of procedural street face and not a port or industrial shed); remnants beside kit pieces are measured only. */
  share: { lower: number | null; whole: number | null; elements: string[]; target: boolean };
  glass: { surface: string; tint: 'clear'; share: number; maxPane: number; sill: number; head: number; reveal: number; mullion: number };
  pier: { surface: string; frame: number; frameSurface: string };
  /** Portholes: diameter `d`, chrome ring `ring` in `ringSurface`, `perBay` side by side in a P bay. panel (grammar §2a item 3a): a `w` x `h` panel in `surface`
   *  (the building's accent) behind every porthole (P bays, porthole columns, a shed's eave row), centred on it, flush on the wall with the ring proud of it. */
  porthole: { d: number; ring: number; ringSurface: string; perBay: number; panel: { w: number; h: number; surface: string } };
  faces: SkinFace[];
};
/** A vertical fin (grammar §2a.4): a slab `w` wide standing `d` proud of the wall at `at` (where its back meets the wall), facing `normal`, from z0 to z1
 *  (full height of the body), in `surface` (accent_coral or accent_teal), its outer edge beaded in `bead.surface` (gold) `bead.w` m. kind: corner (on the
 *  bisector of a procedural street corner) or entrance (over a lobby entrance, from its canopy up). */
export type Fin = { id: string; kind: 'corner' | 'entrance'; at: V; normal: V; w: number; d: number; z0: number; z1: number; surface: string; bead: { surface: string; w: number } };
/** Striped fabric, banners and flags (grammar §2a). Every fabric surface is an accent role (look.json `accent_*`) or `canvas` (white awning cloth, facade.json
 *  `white` #f7f4ec); never the cone red. */
export type Fabric = { awnings: Awning[]; banners: Banner[]; flags: Flag[] };
/** A balcony awning over an upper-storey window: its back edge on the wall at `at`, `z` high, `w` wide; it slopes out `d` m and down `fall` m, with a
 *  `valance` m drop at the front. Stripes run down the slope, `stripe` m wide, alternating `stripes[0]` (the building's accent) and `stripes[1]` (canvas).
 *  `face` = the skin face (mass id + edge) it hangs on. */
export type Awning = { id: string; building: string; face: string; at: V; normal: V; yaw: number; z: number; w: number; d: number; fall: number; valance: number; stripe: number; stripes: [string, string] };
/** A vertical banner on an arm `arm` m out from the wall at `at`, facing `normal` (its face is the street side): `w` x `h` from `z0`, in `surface` with a
 *  `border.w` m border in `border.surface` (gold). kind: lobby (beside a lobby entrance) or corner (hung off a corner fin). */
export type Banner = { id: string; building: string; kind: 'lobby' | 'corner'; at: V; normal: V; yaw: number; z0: number; w: number; h: number; arm: number; surface: string; border: { surface: string; w: number } };
/** A flag mast standing on the pavement at `at` (base `z`), `h` high in `mast` (chrome), a pennant `pennant` [w, h] at the top in `surface`, flying toward
 *  `yaw` (plan frame, radians: one wind for the district). kind: civic, hotel (along those frontages) or parade (both sides of Crown Parade), `pitch` m apart. */
export type Flag = { id: string; kind: 'civic' | 'hotel' | 'parade'; host: string; at: V; z: number; h: number; mast: string; pennant: [number, number]; surface: string; yaw: number };
/** One wall of a building mass (`mass` id, ring `edge` a→b, outward = right of a→b on the CCW ring). bays: one letter per equal bay along a→b — R ribbon window,
 *  P porthole bay (perBay portholes side by side), B balcony bay (a glazed door and a rounded balcony slab), - plain stone. rows: floor z of each storey that has
 *  windows (sill = row + glass.sill). portholes: positions along a→b of vertical porthole columns (one per row). glass: this face's glass share of its wall (<= 0.55). */
export type SkinFace = { mass: string; edge: number; a: V; b: V; bays: string; rows: number[]; portholes: number[]; glass: number };
/** One tenancy: a frontage a→b on a facade at z, facing `normal`. Prims are in world frame; `sign` carries motif + icon + palette key, never text. */
export type Frontage = {
  id: string; host: string; level: string; z: number; trade: string; area: string; street: string; a: V; b: V; w: number; normal: V; yaw: number;
  hours: [number, number] | 'service'; staffed: boolean; spill: { kind: string; depth: number }; lobby: boolean; anchor: boolean;
  front: Prim; entrance: Prim; canopy: Prim; sign: Prim & { motif: string; icon: string; palette: string }; service?: Prim;
  facade?: string; // id of the kit facade piece standing over this tenancy (its own shopfront is in the piece); the sign stays
  source?: Source; signKit?: KitRef & { at: V; base: number; mount: 'blade' | 'wall' }; // the fascia sign stays procedural; signKit adds a blade or wall sign at `at`, its lowest point at `base`
};
export type Connector = {
  id: string; kind: 'stair' | 'ramp' | 'cartRamp' | 'escalator' | 'lift' | 'goodsLift' | 'stairTower'; host: string; serves: string[]; // serves = ids of the decks (or station platforms) it lands on
  from: string; to: string; foot: V; head: V; bottom: number; top: number; width: number; stepFree: boolean; ring: V[]; yaw: number;
  /** The dressed form (tools/world/gen/dress.ts, grammar §1-3): stair towers as glazed stadiums (rounded ends) with a limestone plinth and spine, band pairs
   *  (chrome + accent: 'beam' coral on station banks, 'tower' turquoise elsewhere) and a stepped chrome cap; open lifts as glazed chrome-hooped tubes with a
   *  parked car and an atom-ball crown. When present draw these prims INSTEAD of the ring prism; `ring` stays the footprint every rule reads. */
  dress?: Prim[];
};
/** A crossing (streetscape.crossings.strip): a `kerb` crossing is two landings (one on each kerb of `road`) and the `strip` across the carriageway between them,
 *  `line` its centre line from one landing's kerb point to the other's, `width` across it; `refuges` = the rings of the median the strip crosses where the median is refuge width, where people wait;
 *  `control` = `signal` where the plan says the road class has one: the crossing then carries signal orbs (a zebra no longer exists, brief 009). A `stop` or `bank` crossing is one landing (the way from a tram platform,
 *  or from a station bank, across the road): `line` runs along its long axis (`size[0]`), `strip` is the landing's own footprint, and it has no kerb ends, so no orbs, studs or arcs.
 *  `items` = the ids of every street item that is a part of this crossing, in drawing order (the two light lines, then orbs, studs, arcs); each part names it back in `crossing`. A part is in exactly one crossing.
 *  `walk` = the walk-axis yaw (plan frame, radians): the way across, from `line[0]` to `line[1]`. The light lines run along it, kerb to kerb; no piece lies across it (SF-20 holds every light piece's long axis off the carriageway's axis by 30 degrees or more).
 *  Nothing here is a slab: the landing is not drawn, the paving under it is the street's. */
export type Crossing = { id: string; kind: 'kerb' | 'stop' | 'bank'; road: string; items: string[]; landings: Landing[]; line: [V, V]; strip: V[]; width: number; walk: number; control?: 'signal'; refuges: V[][] };
/** Where people step on or off a crossing (a kerb crossing's two ends; a stop or bank crossing's one patch): the footprint `size` = [depth along `yaw` from the kerb, width along the kerb, 0.02] centred on `at`, `yaw` across the road,
 *  `z` the surface under it (the pavement top 0.18; a tram stop's patch lies on the carriageway at 0). Keep-clear only, NOT drawn: nothing stands within 2 m of it (SF-2), a pylon keeps 2 m (SF-18), a Handover point keeps its 1.2 m (CD-1);
 *  `id` = the id the old landing slab had. A kerb end's studs, arc and orb stand on it. */
export type Landing = Pick<StreetItem, 'id' | 'edge' | 'at' | 'yaw' | 'z' | 'size'>;
/** A signal orb's state, set by the plan (`streetscape.crossings.strip.signal.state`, `walk` at build) and switched by city life (Pulse, brief 005): every orb of a crossing shows the same one. */
export type SignalState = 'walk' | 'stop';
/** `signal-orb` (brief 009): the SignalEye piece, a chrome orb hovering over a kerb end (`id` `<crossing>/signal/a|b`) or a refuge (`/signal/r0..`), a round display on each of its two faces showing the walking figure (`walk`) or the stop pictogram
 *  (`stop`, red is allowed on this display only), a thin beam from its underside down to a glowing disc on the ground (`halo`) where people wait.
 *  The item stands where the beam lands: `at` is the halo's centre (and the orb's, vertically above), `z` the ground there (= `halo.z`, the piece's pivot), `size` [halo diameter, halo diameter, height of the finial];
 *  the orb's centre hangs `hover` above `z`, `size` across (the diameter). `faces[0]` is the heading display A (the piece's front, +Z) looks along: the walk heading at this kerb end, toward the far kerb; `faces[1]` the way back (display B).
 *  The orb hangs overhead (canopy-class, exempt from the ground rules); the halo and the studs are ground items. Only the orb's body casts a shadow (the item's `castShadows`); the beam, halo and displays never do. */
export type Signal = { state: SignalState; hover: number; size: number; faces: [number, number]; halo: { at: V; z: number; r: number } };

export type StreetItem = {
  id: string; kind: string; at: V; yaw: number; z: number; size: [number, number, number]; surface: string; edge: string; // edge = street edge / host id; surface = a look key (not enumerated here): `trim` by default, `park`, `enamel`, `crossing` for the landing patches of a plan without streetscape.crossings.strip, `glow` (light bars and dots), `inlay` (arcs), `orb` (signal orbs), `booth` for a phone booth, `tub` for a palm tub
  sight: 'blocks' | 'glazed' | 'open'; staffed?: { trade?: string; occupation?: string; hours: [number, number] | 'service' }; variant?: string;
  /** A crossing's part (kinds `crossing-bar`, `crossing-dots`, `crossing-edge`, `crossing-stud`, `crossing-arc`, `signal-orb`): the crossing it belongs to. Every such part carries it. `yaw`: the way across (light pieces: their long axis; studs), the heading from the arc's centre toward the mouth corner it rounds (arcs),
   *  the heading display A looks along (orbs); `z` the surface it lies on (an orb: the ground under its halo). */
  crossing?: string;
  /** `signal-orb` only. */
  signal?: Signal;
  /** `crossing-dots` only: one module of a light line, a row of `n` flush light dots `d` across, `pitch` apart, along `yaw`, centred on `at`; `size` = [the module's length, its frame width, thickness]. (`crossing-bar`: one flush light bar, `size` = [length along `yaw`, frame width, thickness].) */
  dots?: { n: number; pitch: number; d: number };
  /** `crossing-arc` only: a quarter ring of seafoam inlay in the paving that rounds one corner of a crossing's mouth: the centre line of a band `w` wide at radius `r` about `at` (the circle's centre, which is the piece's pivot), swept `sweep` radians symmetrically about `yaw` (toward the corner);
   *  `outer` = the outer radius including the brass hairline: the circle is tangent to the kerb line and to the strip's edge line. `size` = [outer, outer, thickness]. */
  arc?: { r: number; w: number; sweep: number; outer: number };
  /** Parts of a crossing only: whether Unity casts a shadow from the item. The signal orb's body does (the piece's other parts never); every other part is flush paving or light and casts none. */
  castShadows?: boolean;
  source?: Source; kit?: KitRef; // wave 1: street furniture, plants, signs; absent on a layer the kit has not reached
  /** Brief 013: the item is a closed container that can hold a body out of every sightline (`holds` at a time), resolved from its kit piece's `tags.stash` (contracts/world/kit.json; a candidate piece too, so the world is right before Vela adopts it). Only `dumpster` today. Absent = it hides nothing. */
  stash?: { holds: number };
};

/** The horizon band (contracts/world/harbour-district.grammar.md §9), derived from the plan's id and sheet: polar rings of distant kit pieces round `centre`,
 *  rockets, planets and moons, and a visual sea plane. Pieces stand on `base`; `haze` = share of the haze colour mixed in; none casts shadows. Sky bodies and
 *  rocket ascents are placed in 3D (`at` + `base` as their centre height); `tilt` leans a piece `deg` toward `yaw` (plan frame, radians). */
export type HorizonPiece = { id: string; ring: 'A' | 'B' | 'C' | 'shore' | 'rocket-pad' | 'rocket-ascent' | 'planet' | 'moon'; kit: KitRef & { at: V }; base: number; haze: number; tilt?: { deg: number; yaw: number } };
/** sea.sectors: the visual sea, the bay sector only (draw these at sea.level, NOT a square plane; `size` is the world's extent about `centre`, `fadeFrom` where it fades into the haze):
 *  bounded by the coast rays leaving the plan water's corners on the district edge at +-60° and the far-shore arc (r 5000 about `centre`).
 *  The coasts are seeded natural shapes (a curved bay by the district, a mole, headlands further out), never leaving that sector.
 *  land: west, north and south flats (surface 'beyond_ground', the suburban ground beyond the district) at the district edge's ground level; with the sea they
 *  partition the square outside the sheet, so they never overlap. North and south reach round the bay to the far shore. banks: thin walls on the land side of
 *  every land/sea edge (top 0.02 under the land plane), so the step down to the water is a face.
 *  patches: flats drawn OVER the land at their own level (a few cm above it): parks and tree clumps ('beyond_green', ids horizon/park/* and horizon/trees/*),
 *  the coastal promenade along each coast and the mole's top ('quay', ids horizon/promenade/*). They never overlap each other, the hills or the sea.
 *  hills: terraced hills ('beyond_hill'), each a stack of nested prisms (ids horizon/hill/<n>/tier-<k>); a tier's base sits inside the tier below, the first
 *  1 m under the land. Every ground piece's `base` is the land plane or the top of the terrace it stands on, the same under its whole footprint. */
export type Horizon = { centre: V; haze: string; sea: { surface: string; centre: V; size: number; level: number; fadeFrom: number; sectors: Flat[] }; land: Flat[]; patches: Flat[]; hills: Mass[]; banks: Mass[]; pieces: HorizonPiece[] };

/** A plan signature structure standing on its ground (brief 007 A): `kit` = the piece placed at its centre (the regenerated `variant` when it is adopted, else the base piece and `pending` names the variant still to regenerate); `tip` = base + piece height. */
export type SignatureOut = { id: string; kind: 'signature' | 'tower' | 'skybridge' | 'station' | 'portal'; source: Source; kit?: KitRef & { at: V }; ground?: [number, number, number, number]; base: number; tip: number; pending?: string; why?: string }; // kind: signature (SpaceHQ, with `ground`), tower (a site's regenerated piece, base = podium top), skybridge, station (SkyRailStation_Twin / _Decks, pivot on the line), portal (the Parade gate block)

/** What the takedown, report and response rules read from the world (brief 013; Vantage's quiet places, Pulse's report points and response origins, Motive §8). Everything is derived from the plan, the civic block and the street the generator made: no
 *  position here is placed for a mission (tools/world/gen/street/quiet.ts, tools/world/civic/quiet.ts). Plan frame like the rest of the export (x east, y north); the sim site converts it to block frame centimetres. `stand` = the free ground cell nearest the thing on the
 *  engine's walk grid (tools/plan/walk-grid.ts), where a walker stands to use it. Rules R-HIDE, R-STASH, R-REPORT and R-DOCK (tools/world/rules/quiet.ts) hold it. */
export type Takedown = {
  /** The walk grid the reach is measured on: the civic block's window in block frame (x south, y east), m, and its cell, m. */
  grid: { x0: number; x1: number; y0: number; y1: number; cell: number };
  /** Reach (m, on the grid walk) the rules hold: every walkable lattice sample within `report` of a report point (R-REPORT, rulebook `report-reach-m`, proposed) and within `origin` of a response origin (R-DOCK, `dock-reach-m`, proposed); the lattice spacing, m. */
  reach: { report: number; origin: number; lattice: number };
  /** Every door of the civic block with what it is for: public, official or service (civic-block.json `kind`). */
  entrances: TakedownDoor[];
  /** Where a body can be put out of sight: a service door (a timed off-screen action) and the container beside it (a street item with `stash`). */
  stashes: Stash[];
  /** Where a witness reports (Pulse §3.1): the doors of staffed units (public and official), the street's own phone booths in the window and the call boxes the rule placed. A police post is an origin, not a report point. */
  report: ReportPoint[];
  /** Where the response starts: the robot docks in the window (the watcher sites' civic-robot-dock) and the police posts (and the Records Office doors, which are posts). */
  origins: Origin[];
  /** What the rules measured, for the report: the sample count and the walk (m) to the nearest point, with the doors alone (the baseline) and with everything placed. */
  coverage: { report: Coverage; origin: Coverage };
  /** Watchers the world stands in the window that the sim may or may not read (camera mismatch, brief 013 C): mast cameras and civic robots, their reach and the heads' facings (plan radians). */
  watchers: WatcherNote[];
};
export type TakedownDoor = { id: string; building: string; name: string; kind: 'public' | 'official' | 'service'; at: V; z: number; stand: V };
/** `door` = a service door (the body goes off screen, a timed action), `container` = a street item with `stash` (the body goes in it, seen as a carry). `of` = the service door it serves. `bare` on a door: why no container stands beside it (R-STASH accepts that reason, nothing else). */
export type Stash = { id: string; kind: 'container' | 'door'; at: V; stand: V; holds: number; of?: string; item?: string; ring?: V[]; bare?: string };
export type ReportPoint = { id: string; kind: 'door' | 'call-box'; at: V; stand: V; item?: string; door?: string };
export type Origin = { id: string; kind: 'dock' | 'post'; at: V; stand: V; item?: string; door?: string };
export type Coverage = { samples: number; worstM: number; medianM: number; within: number; baselineWithin: number; baselineWorstM: number };
export type WatcherNote = { id: string; kind: string; at: V; facings: number[]; range: number; coneDeg: number; item: string };
