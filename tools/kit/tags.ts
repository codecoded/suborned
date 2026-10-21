// Generator tags per asset: what it is for, its scale range, and (wave 2) form and trade. Wave 1 = first adopted set (brief 006 A).
import type { KitReach, KitStash, KitTags } from '../../contracts/world/kit.schema';

export const WAVE1 = new Set(['SphereStation', 'Skyport_Tower', 'CircusMonument', 'ParabolicArch', 'SkyPad_Roof', 'Streetlamp', 'Bollard', 'Bench', 'Hydrant', 'RecyclingBins', 'PhoneBooth', 'TicketMachine', 'PlazaSphereLamp', 'CCTVPole', 'DomeCamera', 'CoconutPalm', 'FanPalm', 'Cypress', 'BladeSign_Cafe', 'BladeSign_Hotel', 'BladeSign_Cinema', 'NeonStar_Wall', 'AdColumn']);

const USE: Record<string, string[]> = {
  Dumpster: ['alley', 'dumpster'], // the world places it as the street item `dumpster` beside a service door (tools/world/civic/quiet.ts)
  Streetlamp: ['lamp'], PlazaSphereLamp: ['lamp'], Bollard: ['bollard'], Bench: ['bench'], Hydrant: ['hydrant'], RecyclingBins: ['bin'], PhoneBooth: ['phone-booth:twin'], // the 2.6 m twin cabinet: retired from the phone-booth slot for PhoneBooth_Single (brief 020 follow-up)
  TicketMachine: ['ticket-machine'],
  CCTVPole: ['pole-camera'], DomeCamera: ['canopy-camera'], CoconutPalm: ['tree', 'tub-palm'], FanPalm: ['tub-palm:full'], // the 4.2k-triangle palm: retired from the tub-palm slot for FanPalm_Play (brief 020 follow-up); a bare palm with no pot, so it can stand in a planter's bed (a potted fern cannot)
  Cypress: ['planter-bed:cypress'], Cypress_Play: ['planter-bed:cypress'], PottedTopiary_Play: ['planter-bed:potted'], CoconutPalm_Street: ['tree'], WashingtoniaPalm_Street: ['tree:washingtonia'], // the avenue species only (15.6 m, crown 3.6 m); phoenix slots keep the coconut palms WashingtoniaPalm_Street: ['tree'], // brief 026 (Sky: street trees are palms, not topiary): the street-tree slot is the palms'; cypress and topiary stay for beds and the civic block (named by id)
  AdColumn: ['advertising-column'],
  BladeSign_Cafe: ['blade-sign'], BladeSign_Hotel: ['blade-sign'], BladeSign_Cinema: ['blade-sign'], NeonStar_Wall: ['wall-sign'],
  SphereStation: ['landmark:sphere'], SkyPad_Roof: ['landmark:skyport'], CircusMonument: ['landmark:monument'],
};
/** The kinds and slots the world gives the pieces brief 020 adopted (Vela's pick, Splice's wiring): street kinds by the street item kind, roof pieces by the part of a programme they stand for (tools/world/gen/roofs.ts), the café and the cars by their place. */
const USE_020: Record<string, string[]> = {
  CafeBay_v2: ['frontage:cafe-bay'], CafeInterior: ['frontage:cafe-interior'], Sedan_Official: ['vehicle:car-official'], Sedan_Getaway: ['vehicle:car-getaway'],
  Roof_Tank: ['roof:water-sphere'], Roof_Mast: ['roof:antenna-mast'], Roof_LatticeMast: ['roof:antenna-mast'], Roof_Pool: ['roof:pool'], Roof_LapPool: ['roof:pool'], Roof_Tennis: ['roof:tennis-court'],
  Roof_Conservatory: ['roof:garden-house'], Roof_Cabana: ['roof:garden-shade'], Roof_Planter: ['roof:garden-bed'],
  Planter_Trough: ['planter', 'planter:planted'], Planter_Urn: ['planter:full'], Planter_Urn_Play: ['planter', 'planter-bed:urn'], PottedFern: ['tub-palm'], PneumaticPost: ['mailbox:tall'], PneumaticPost_Play: ['mailbox'], // the _Play copies (1.1-1.4k triangles, the plan's 1.3 m box for the mailbox) take the slots; the full pieces keep a retired tag
  FanPalm_Play: ['tub-palm', 'tub-palm:planted'], PhoneBooth_Single: ['phone-booth'], Newsstand: ['newsstand'], CurvedKiosk: ['kiosk-pavilion'], StreetClock: ['public-clock'], BusShelter_Play: ['tram-shelter'], TramCanopy_Play: ['tram-shelter'], CurvedKiosk_Play: ['newsstand:scaled'], // retired from the newsstand slot for Newsstand (3.2 x 1.9 x 2.8 m, no scaling)
  CoffeeCart_Play: ['kiosk-food'],
};
/** Brief 026 (Sky 2026-10-20: the old DecoKit city is the floor): the old kit's ground, robots, furniture, booths, signage and plants, adopted for the capstone block. The generator places them by rule
 *  (tools/world/gen/street/capstone.ts), picking by these use tags; none is a `tree` or a street kind the older slots (slotOf) would pick for something else. */
const USE_026: Record<string, string[]> = {
  // brief 026/028 (Vela, the testbed): flooring, road lights, the café fascia and the lamps; the bays and the fountain keep their own use for the civic block (named by id)
  Pave_Slab: ['ground:slab'], Kerb_Pearl_0: ['ground:kerb'], Kerb_Pearl_1: ['ground:kerb'], TreePit_Pearl: ['ground:tree-pit'], PaveInlay_Band_6m: ['inlay:tile-band'], PaveInlay_Ring_Q: ['inlay:tile-ring'],
  RoadRibbon_Straight_6m: ['ground:road-ribbon'], RoadRibbon_Arc90_R4: ['ground:road-ribbon'], RoadRibbon_Arc90_R8: ['ground:road-ribbon'], RoadMark_Lozenge: ['ground:road-lozenge'], RoadMark_Seam: ['ground:road-seam'],
  CafeFascia: ['sign:cafe-fascia'], AwningLamp: ['lamp:wall'], PalmUplight: ['lamp:uplight'], StreetLampGlobe: ['lamp:globe'],
  CafeLamp_Testbed_Amber: ['lamp:table'], CafeLamp_Testbed_Teal: ['lamp:table'], CafeLamp_Testbed_Peach: ['lamp:table'], CafeLamp_Testbed_Coral: ['lamp:table'], CafeLamp_Testbed_Lilac: ['lamp:table'],
  BoardingPavilion_TB_Square: ['shelter:square:testbed'], BoardingPavilion_TB_Office: ['shelter:office:testbed'], AtomFountain_Testbed: ['plaza:fountain:testbed'],
  CafeInterior_Testbed: ['frontage:cafe-interior:testbed'], CafeSet_Testbed: ['cafe-set:testbed'], Parasol_Testbed: ['parasol:testbed'],
  Kerb_Granite_0: ['ground:kerb'], Kerb_Granite_1: ['ground:kerb'], Kerb_Straight: ['ground:kerb-plain'],
  Kerb_GraniteCorner_0: ['ground:kerb-corner'], Kerb_GraniteCorner_1: ['ground:kerb-corner'], Kerb_GraniteCorner_2: ['ground:kerb-corner'], Kerb_GraniteCorner_3: ['ground:kerb-corner'], Kerb_GraniteNose: ['ground:kerb-nose'],
  Pave_Flags_0: ['ground:flags'], Pave_Flags_1: ['ground:flags'], Pave_Flags_2: ['ground:flags'], Pave_Flags_3: ['ground:flags'],
  TreePit_Flags_0: ['ground:tree-pit'], TreePit_Flags_1: ['ground:tree-pit'], TreePit_Flags_2: ['ground:tree-pit'], TreePit_Flags_3: ['ground:tree-pit'],
  Ground_Swatches: ['ground:materials'], // carries the DK_Ground_* materials and textures (Vela round 1): the ground's look is those materials on world-space-UV meshes, not tiles
  ManholeCover: ['ground:manhole'], DrainGrate: ['ground:drain'], Tile_Tactile: ['ground:tactile'], Tile_Terrazzo: ['ground:terrazzo'], Tile_TerrazzoArc: ['ground:terrazzo-arc'],
  InlayArc_R6: ['ground:inlay-arc'], InlayArc_R10: ['ground:inlay-arc'], InlayArc_R16: ['ground:inlay-arc'],
  RobotPorter: ['robot:porter', 'cast'], RobotPorter_Play: ['robot:porter', 'cast'], RobotWaiter: ['robot:waiter', 'cast'], RobotWaiter_Play: ['robot:waiter', 'cast'],
  RobotNanny: ['robot:nanny', 'cast'], RobotNanny_Play: ['robot:nanny', 'cast'], RobotShopkeeper: ['robot:shopkeeper', 'cast'], RobotShopkeeper_Play: ['robot:shopkeeper', 'cast'],
  RobotButler: ['robot:butler', 'cast'], RobotButler_Play: ['robot:butler', 'cast'],
  VendingMachine_Cola: ['vending'], VendingMachine_Snack: ['vending'], Automat: ['automat'], DrinkingFountain: ['drinking-fountain'], RoundBench: ['round-bench'], HangingBasket: ['hanging-basket'], WindowBox: ['window-box'], TerraceScreen: ['terrace-screen'], Deckchair: ['deckchair'],
  CoffeeCart: ['kiosk-food:full'], Lockers: ['lockers'], // the full 24.9k-triangle cart: retired from the kiosk-food slot for CoffeeCart_Play, like PneumaticPost
  Billboard_Rooftop_A: ['billboard:rooftop'], Billboard_Rooftop_B: ['billboard:rooftop'], Billboard_Rooftop_C: ['billboard:rooftop'], Billboard_Pylon_A: ['billboard:pylon'], Billboard_Pylon_B: ['billboard:pylon'],
  Billboard_Wall_A: ['billboard:wall'], Billboard_Wall_B: ['billboard:wall'], Billboard_Wall_C: ['billboard:wall'], Marquee_Cinema: ['marquee:cinema'], SandwichBoard: ['sandwich-board'],
  TopiaryCone: ['planter-bed:topiary'], ShrubMound: ['planter-bed:shrub'], FlowerBed: ['planter-bed:flowers'], PottedTopiary: ['planter-bed:potted'], // not 'tree': the street-tree slot would pick them
  PottedPalm_Small: ['planter-bed:palm'], BikeRack: ['bike-rack'], Barrier: ['barrier'], FacetedPot: ['planter-bed:pot'], InfoLightbox: ['info-lightbox'],
};
/** What the play camera sees through the pieces that state it (KitSight). */
const SIGHT: Record<string, 'open' | 'glazed'> = { PhoneBooth: 'glazed', PhoneBooth_Single: 'glazed', Newsstand: 'glazed', CafeBay_v2: 'glazed', CafeInterior: 'glazed', Sedan_Official: 'open', Sedan_Getaway: 'open' };
const TRADE: Record<string, string[]> = {
  BladeSign_Cafe: ['diner', 'soda-fountain', 'automat', 'grocer', 'tiki-bar', 'cocktail-lounge'], BladeSign_Hotel: ['hotel-lobby', 'travel-bureau'],
  BladeSign_Cinema: ['cinema', 'dance-hall', 'jazz-club', 'bowling'],
  NeonStar_Wall: ['diner', 'tiki-bar', 'cocktail-lounge', 'jazz-club', 'dance-hall', 'bowling', 'record-shop', 'cinema', 'soda-fountain', 'hotel-lobby'],
};
// [min, max] uniform scale on height; palms may be squeezed to `squash` of their height scale (tall slim crowns: Washingtonia).
const SCALE: Record<string, [number, number]> = {
  AtomFountain: [0.9, 1.1], BoardingPavilion_Office: [0.9, 1.1], BoardingPavilion_Square: [0.9, 1.1], PavilionPier_Office: [0.9, 1.1], PavilionPier_Square: [0.9, 1.2], // civic square (grammar §11): built to the block's footprints; 1.2 under BoardingPavilion_Square_v2 (2.226 m, brief 008 B3)
  LinerDock: [0.4, 1.1], 'LinerDock@liner-berth': [0.4, 1.1], OceanLiner: [0.4, 1.1], SkyRailStation_Twin: [1, 1], SkyRailStation_Decks: [1, 1], ParadeGate_Block: [1, 1], // landmarks built to their plan sites (the liner at x 0.5)
  PlanterArc: [1, 1], // the four fountain-garden beds (Vela, brief 007 round 3): built to the bed's band about the fountain (r 5.36-7.06), placed at scale 1
  CafeSet_Tulip: [0.5, 1.2], PottedPalm_Small: [0.5, 1.4], UtilityCabinet: [0.6, 1.4], CivicCabinet: [0.9, 1.5], CoconutPalm_Street: [0.9, 1.5], Pave_Slab: [1, 1], Kerb_Pearl_0: [1, 1], Kerb_Pearl_1: [1, 1], TreePit_Pearl: [1, 1], PaveInlay_Band_6m: [1, 1], PaveInlay_Ring_Q: [1, 1], BoardingPavilion_TB_Square: [0.99, 1.01], BoardingPavilion_TB_Office: [0.99, 1.01], AtomFountain_Testbed: [0.99, 1.01], RoadRibbon_Straight_6m: [1, 1], RoadRibbon_Arc90_R4: [1, 1], RoadRibbon_Arc90_R8: [1, 1], RoadMark_Lozenge: [1, 1], RoadMark_Seam: [1, 1], // the 10.25 m palm fills the 11 m (washingtonia) and 16 m (phoenix) street-tree species
  // CivicCabinet fills the block's 0.9 x 1.7 m control-cabinet box at 1.5 (brief 026)
 
  Crown_Needle: [0.8, 1.4], Crown_Finial: [0.8, 1.4], Crown_Atom: [0.8, 1.4], Crown_Starburst: [0.8, 1.4], Crown_Saucer: [0.8, 1.4], Crown_Antenna: [0.8, 1.4], Crown_Dish: [0.8, 1.4], // grammar §6
  Horizon_Spire_A: [0.6, 4], Horizon_Spire_B: [0.6, 4], Horizon_Spire_C: [0.6, 4], Horizon_Spire_D: [0.6, 4], Horizon_Massif: [0.6, 4], Horizon_RocketPad: [0.6, 4], Horizon_RocketAscent: [0.6, 4], Horizon_PlanetRinged: [0.6, 5], Horizon_Moon: [0.6, 5], // grammar §9 rings
  Streetlamp: [0.7, 1.6], PlazaSphereLamp: [0.7, 1.6], Bollard: [0.8, 1.4], Bench: [0.9, 1.25], Hydrant: [0.9, 1.3], RecyclingBins: [0.8, 1.3], PhoneBooth: [0.9, 1.1], TicketMachine: [0.9, 1.15],
  CCTVPole: [0.8, 1.8], DomeCamera: [0.6, 1.2], CoconutPalm: [0.5, 2.2], FanPalm: [0.5, 2.2], Cypress: [0.6, 2.2], AdColumn: [0.9, 1.2],
  BladeSign_Cafe: [0.5, 1], BladeSign_Hotel: [0.5, 1], BladeSign_Cinema: [0.5, 1], NeonStar_Wall: [0.4, 1.2],
  SignalEye: [1, 1], KerbStud: [1, 1], LightBar_4: [1, 1], LightDotRow_2: [1, 1], LightDotRow_1: [1, 1], CrossEdgeLine_2: [1, 1], CrossEdgeLine_1: [1, 1], CrossInlayArc_R24_W08: [1, 1], LightField_2: [1, 1], LightField_1: [1, 1], // crossing parts: pinned to the plan's numbers, scale 1
  SphereStation: [0.4, 1.5], Skyport_Tower: [0.4, 1.5], CircusMonument: [0.4, 1.5],
  // brief 020: built to their places, scale 1 (the café, the cars, the roof pieces: regenerate rather than stretch)
  CafeBay_v2: [1, 1], CafeInterior: [1, 1], Sedan_Official: [1, 1], Sedan_Getaway: [1, 1],
  Roof_Tank: [1, 1], Roof_Mast: [1, 1], Roof_LatticeMast: [1, 1], Roof_Pool: [1, 1], Roof_LapPool: [1, 1], Roof_Tennis: [1, 1], Roof_Conservatory: [1, 1], Roof_Cabana: [1, 1], Roof_Planter: [1, 1],
  // brief 026: ground pieces are refit to the plot edge they lie on (a kerb module to its edge's length, a flag cell to its plot, the kerb stone up to the pavement's 0.185 face): 0.75-1.25; the rest stand at scale 1
  Kerb_Granite_0: [0.75, 1.25], Kerb_Granite_1: [0.75, 1.25], Pave_Flags_0: [0.75, 1.25], Pave_Flags_1: [0.75, 1.25], Pave_Flags_2: [0.75, 1.25], Pave_Flags_3: [0.75, 1.25],
  TreePit_Flags_0: [1, 1], TreePit_Flags_1: [1, 1], TreePit_Flags_2: [1, 1], TreePit_Flags_3: [1, 1], Tile_Tactile: [1, 1], Tile_Terrazzo: [1, 1], ManholeCover: [1, 1], DrainGrate: [1, 1],
  RobotPorter: [1, 1], RobotPorter_Play: [1, 1], RobotWaiter: [1, 1], RobotWaiter_Play: [1, 1], RobotNanny: [1, 1], RobotNanny_Play: [1, 1], RobotShopkeeper: [1, 1], RobotShopkeeper_Play: [1, 1],
  VendingMachine_Cola: [1, 1], VendingMachine_Snack: [1, 1], Automat: [1, 1], BikeRack: [1, 1], InfoLightbox: [1, 1], SandwichBoard: [1, 1], HangingBasket: [1, 1], Lockers: [1, 1], RoundBench: [1, 1], DrinkingFountain: [1, 1],
  PottedTopiary: [1, 1], TopiaryCone: [1, 1], ShrubMound: [1, 1], FlowerBed: [1, 1], FacetedPot: [1, 1],
  // street pieces whose size is not the plan's catalogue size: a tub palm is 4.5 m in the plan (a fern is 1.1 m: scaled up to 2.2x it is a 2.4 m potted fern), the newsstand 3 x 1.5 x 2.8 m (the kiosk is 6 x 4.4 x 6.5 m: scaled to 0.4), the two tram shelters a little under their pieces
  PottedFern: [0.9, 2.2], FanPalm_Play: [0.5, 2.2], PhoneBooth_Single: [1, 1], Newsstand: [1, 1], CurvedKiosk_Play: [0.4, 1.1], BusShelter_Play: [0.7, 1.1], TramCanopy_Play: [0.8, 1.1],
};
/** Pieces that can hold a body (brief 013): a closed container big enough to lie a person in. Only the Dumpster today; adopting a piece is Vela's call, tagging it is not. */
const STASH: Record<string, KitStash> = { Dumpster: { holds: 1 } };
const SQUASH: Record<string, number> = { Streetlamp: 0.7, CCTVPole: 0.5 };
/** Where the working end reaches from the pivot: the streetlamp's pole is at the pivot, its head 1.25 m out along local -X. */
const REACH: Record<string, KitReach> = { Streetlamp: { axis: '-x', m: 1.25 } };
const USE_BY_CATEGORY: Record<string, string> = { Street: 'street', Signals: 'street', Booths: 'street', Furnish: 'street', Plants: 'tree', Rooftop: 'roof', Signage: 'sign', Facades: 'facade', Skyline: 'skyline', Highrise: 'tower', Landmarks: 'landmark', Skyport: 'landmark', Ground: 'ground', Alleys: 'alley', Plaza: 'plaza', Structures: 'structure', Sky: 'sky', Vehicles: 'vehicle', Robots: 'cast', Calibration: 'calibration', Crowns: 'crown', Horizon: 'horizon' };

const PLAIN_USE: Record<string, string[]> = { 'LinerDock@liner-berth': ['landmark:liner-berth'], OceanLiner: ['landmark:ocean-liner'], // OceanLiner: the moored liner on the water site Datum is siting beside the berth (a landmark once the plan has it)
  PlanterArc: ['planter:fountain-garden'], SkyRailStation_Twin: ['station:twin'], SkyRailStation_Decks: ['station:decks'], ParadeGate_Block: ['portal:parade-gate'],
  // the crossing's pieces (brief 009 A, Vela; adopted by Splice): the generator places a part by its use tag and never scales it
  SignalEye: ['signal:eye'], KerbStud: ['stud:kerb'], LightBar_4: ['crossing:bar'], LightDotRow_2: ['crossing:dots'], LightDotRow_1: ['crossing:dots'], CrossEdgeLine_2: ['crossing:edge'], CrossEdgeLine_1: ['crossing:edge'], CrossInlayArc_R24_W08: ['inlay:crossing-arc'],
  LightField_2: ['crossing:field'], LightField_1: ['crossing:field'] }; // the lit strip (brief 009, Sky's d1): one additive quad per module under the dots, 3.9 m across the strip

export function tagsOf(name: string, category: string): KitTags {
  const form = /Corner/.test(name) ? 'corner' : /End/.test(name) ? 'end' : /Bay/.test(name) ? 'bay' : undefined;
  return {
    use: USE[name] ?? USE_020[name] ?? USE_026[name] ?? PLAIN_USE[name] ?? [USE_BY_CATEGORY[category] ?? category.toLowerCase()],
    ...(TRADE[name] ? { trade: TRADE[name] } : {}), ...(form && ['Facades', 'Skyline'].includes(category) ? { form } : {}),
    ...(REACH[name] ? { reach: REACH[name] } : {}), ...(STASH[name] ? { stash: STASH[name] } : {}), scale: SCALE[name] ?? [0.9, 1.1], squash: SQUASH[name] ?? 1, wave: WAVE1.has(name) ? 1 : 2,
    ...(SIGHT[name] ? { sight: SIGHT[name] } : {}),
  };
}
