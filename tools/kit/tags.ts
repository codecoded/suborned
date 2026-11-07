// Generator tags per asset: what it is for, its scale range, and (wave 2) form and trade. Wave 1 = first adopted set (brief 006 A).
import type { KitReach, KitStash, KitTags } from '../../contracts/world/kit.schema';

export const WAVE1 = new Set(['SphereStation', 'Skyport_Tower', 'CircusMonument', 'ParabolicArch', 'SkyPad_Roof', 'Streetlamp', 'Bollard', 'Bench', 'Hydrant', 'RecyclingBins', 'PhoneBooth', 'TicketMachine', 'PlazaSphereLamp', 'CCTVPole', 'DomeCamera', 'CoconutPalm', 'FanPalm', 'Cypress', 'BladeSign_Cafe', 'BladeSign_Hotel', 'BladeSign_Cinema', 'NeonStar_Wall', 'AdColumn']);

const USE: Record<string, string[]> = {
  Dumpster: ['alley', 'dumpster'], // the world places it as the street item `dumpster` beside a service door (tools/world/civic/quiet.ts)
  Streetlamp: ['lamp'], PlazaSphereLamp: ['lamp'], Bollard: ['bollard'], Bench: ['bench:full'], Hydrant: ['hydrant:full'], RecyclingBins: ['bin:full'], PhoneBooth: ['phone-booth:twin:full'], PhoneBooth_Play: ['phone-booth:twin'], // brief 097 pass 3: the twin booth's finish (civic_props097b.py) takes the slot; the store piece keeps a retired tag
  // the 2.6 m twin cabinet: retired from the phone-booth slot for PhoneBooth_Single (brief 020 follow-up)
  TicketMachine: ['ticket-machine'],
  CCTVPole: ['pole-camera'], DomeCamera: ['canopy-camera'], CoconutPalm: ['tree', 'tub-palm', 'tree:coconut'], FanPalm: ['tub-palm:full'], // the 4.2k-triangle palm: retired from the tub-palm slot for FanPalm_Play (brief 020 follow-up); a bare palm with no pot, so it can stand in a planter's bed (a potted fern cannot)
  Cypress: ['planter-bed:cypress'], Cypress_Play: ['planter-bed:cypress'], PottedTopiary_Play: ['planter-bed:potted'], CoconutPalm_Street: ['tree', 'tree:coconut-median'], WashingtoniaPalm_Street: ['tree:retired'], // brief 028 Palm (Sky 2026-10-22 "old palms"): the avenue slot `tree:coconut` is CoconutPalm (the old city's boulevard palm, 7.9 m) and the median cut `tree:coconut-median` CoconutPalm_Street (10.6 m); the phoenix slots keep taking any `tree` piece. WashingtoniaPalm_Street (15.6 m, Vela 20 Oct) is retired: no slot names it (status candidate in the catalogue) // brief 026 (Sky: street trees are palms, not topiary): the street-tree slot is the palms'; cypress and topiary stay for beds and the civic block (named by id)
  AdColumn: ['advertising-column'],
  BladeSign_Cafe: ['blade-sign'], BladeSign_Hotel: ['blade-sign'], BladeSign_Cinema: ['blade-sign'], NeonStar_Wall: ['wall-sign'],
  SphereStation: ['landmark:sphere'], SkyPad_Roof: ['landmark:skyport'], CircusMonument: ['landmark:monument'],
};
/** The kinds and slots the world gives the pieces brief 020 adopted (Vela's pick, Splice's wiring): street kinds by the street item kind, roof pieces by the part of a programme they stand for (tools/world/gen/roofs.ts), the café and the cars by their place. */
const USE_020: Record<string, string[]> = {
  CafeBay_v2: ['frontage:cafe-bay'], CafeInterior: ['frontage:cafe-interior'], Sedan_Official: ['vehicle:car-official'], Sedan_Getaway: ['vehicle:car-getaway'], Sedan_Rival: ['vehicle:car-rivals'], Sedan_Getaway_v2: [], CafeUpper_3: ['frontage:cafe-upper'], CafeUpper_4: ['frontage:cafe-upper'], // brief 066 (Vela): the café's body over CafeBay_v2 at 3 or 4 storeys (civic-look.json `upper` places the one of the block's height); brief 070 (Vela): the getaway car finished; adopted 2026-10-27 in place as Sedan_Getaway (and Sedan_Official), so Sedan_Getaway_v2 is retired: no use tag, status rejected (the host loads the car by path, never by this tag); brief 067 (Vela): the rival crew's car, drawn by the host (HostViews.Rivals.cs), never placed by the world
  Roof_Tank: ['roof:water-sphere:full'], Roof_Mast: ['roof:antenna-mast:full'], Roof_Tank_Play: ['roof:water-sphere'], Roof_Mast_Play: ['roof:antenna-mast'], // brief 097: the streamline drums (civic_props097d.py)
  Roof_LatticeMast: ['roof:antenna-mast:full'], // brief 097: the lattice piece keeps a retired tag (the drums took the slot)
  Roof_Pool: ['roof:pool'], Roof_LapPool: ['roof:pool'], Roof_Tennis: ['roof:tennis-court'], // back on their own line (brief 098, Splice): 097's comment above swallowed them, so the pool and court pieces fell to the plain `roof` tag and no roof placed them
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
  RoadShoulder_Straight_6m: ['ground:road-shoulder'], RoadShoulder_Arc90_R4: ['ground:road-shoulder'], RoadShoulder_Arc90_R8: ['ground:road-shoulder'], Ground_RoadSwatch: ['ground:swatch'], // brief 028 road (Vela 22 Oct): the testbed's pearl band, the carriageway's material carrier. PaveEdge_Pinstripe_6m (the brass pavement pinstripe) is retired, 072-G (Sky said yes): status rejected in kit.json, no use tag, nothing places it
  RoadMark_Dash_2m: ['ground:road-lane-dash'], RoadMark_EdgeLine_6m: ['ground:road-edge-line'], Ground_RoadAsphaltSwatch: ['ground:swatch'], // brief 055 road test (Vela 24 Oct): the asphalt option's white enamel lane marks and its material carrier
  CafeFascia: ['sign:cafe-fascia'], AwningLamp: ['lamp:wall'], PalmUplight: ['lamp:uplight'], StreetLampGlobe: ['lamp:globe'],
  CafeLamp_Testbed_Amber: ['lamp:table'], CafeLamp_Testbed_Teal: ['lamp:table'], CafeLamp_Testbed_Peach: ['lamp:table'], CafeLamp_Testbed_Coral: ['lamp:table'], CafeLamp_Testbed_Lilac: ['lamp:table'],
  BoardingPavilion_TB_Square: ['shelter:square:testbed'], BoardingPavilion_TB_Office: ['shelter:office:testbed'], AtomFountain_Testbed: ['plaza:fountain:testbed'],
  CafeInterior_Testbed: ['frontage:cafe-interior:testbed'], CafeSet_Testbed: ['cafe-set:testbed'], Parasol_Testbed: ['parasol:testbed'],
  Kerb_Granite_0: ['ground:kerb'], Kerb_Granite_1: ['ground:kerb'], Kerb_Straight: ['ground:kerb-plain'],
  Kerb_GraniteCorner_0: ['ground:kerb-corner'], Kerb_GraniteCorner_1: ['ground:kerb-corner'], Kerb_GraniteCorner_2: ['ground:kerb-corner'], Kerb_GraniteCorner_3: ['ground:kerb-corner'], Kerb_GraniteNose: ['ground:kerb-nose'],
  Pave_Flags_0: ['ground:flags'], Pave_Flags_1: ['ground:flags'], Pave_Flags_2: ['ground:flags'], Pave_Flags_3: ['ground:flags'],
  TreePit_Flags_0: ['ground:tree-pit'], TreePit_Flags_1: ['ground:tree-pit'], TreePit_Flags_2: ['ground:tree-pit'], TreePit_Flags_3: ['ground:tree-pit'],
  Ground_Swatches: ['ground:materials'], // carries the DK_Ground_* materials and textures (Vela round 1): the ground's look is those materials on world-space-UV meshes, not tiles
  ManholeCover: ['ground:manhole'], DrainGrate: ['ground:drain'], Tile_Tactile: ['ground:tactile:full'], Tile_Tactile_Play: ['ground:tactile'], Tile_Terrazzo: ['ground:terrazzo'], Tile_TerrazzoArc: ['ground:terrazzo-arc'],
  InlayArc_R6: ['ground:inlay-arc'], InlayArc_R10: ['ground:inlay-arc'], InlayArc_R16: ['ground:inlay-arc'],
  RobotPorter: ['robot:porter', 'cast'], RobotPorter_Play: ['robot:porter', 'cast'], RobotWaiter: ['robot:waiter', 'cast'], RobotWaiter_Play: ['robot:waiter', 'cast'],
  RobotNanny: ['robot:nanny', 'cast'], RobotNanny_Play: ['robot:nanny', 'cast'], RobotShopkeeper: ['robot:shopkeeper', 'cast'], RobotShopkeeper_Play: ['robot:shopkeeper', 'cast'],
  RobotButler: ['robot:butler', 'cast'], RobotButler_Play: ['robot:butler', 'cast'],
  VendingMachine_Cola: ['vending:full'], VendingMachine_Snack: ['vending:full'], Automat: ['automat'], DrinkingFountain: ['drinking-fountain'], RoundBench: ['round-bench'], HangingBasket: ['hanging-basket'], WindowBox: ['window-box'], TerraceScreen: ['terrace-screen'], Deckchair: ['deckchair'],
  CoffeeCart: ['kiosk-food:full'], Lockers: ['lockers:full'], // the full 24.9k-triangle cart: retired from the kiosk-food slot for CoffeeCart_Play, like PneumaticPost
  Billboard_Rooftop_A: ['billboard:rooftop'], Billboard_Rooftop_B: ['billboard:rooftop'], Billboard_Rooftop_C: ['billboard:rooftop'], Billboard_Pylon_A: ['billboard:pylon'], Billboard_Pylon_B: ['billboard:pylon'],
  Billboard_Wall_A: ['billboard:wall'], Billboard_Wall_B: ['billboard:wall'], Billboard_Wall_C: ['billboard:wall'], Marquee_Cinema: ['marquee:cinema'], SandwichBoard: ['sandwich-board:full'], SandwichBoard_Play: ['sandwich-board'],
  TopiaryCone: ['planter-bed:topiary'], ShrubMound: ['planter-bed:shrub'], FlowerBed: ['planter-bed:flowers'], PottedTopiary: ['planter-bed:potted'], // not 'tree': the street-tree slot would pick them
  PottedPalm_Small: ['planter-bed:palm'], BikeRack: ['bike-rack:full'], Barrier: ['barrier'], FacetedPot: ['planter-bed:pot'], InfoLightbox: ['info-lightbox'],
};
/** Brief 075 remap (Vela, Sky 2026-10-28 "And old props"): the old pieces the old rooftop programmes (DecoKitRoofThemes / Decks), Orbit Gardens (DecoKitGardens), the plaza (DecoKitPlazaLayout),
 *  the seaside and the alleys (DecoKitDistrictDressing, DecoKitSouthsideLife) name, adopted at their own size (scale [1, 1]) through the generator. Their own tags, never the category's generic
 *  `roof` / `plaza` / `street` / `alley` / `structure`, and never a slot an older picker reads (`tram-shelter`, `planter-bed:*`, `lamp`): Splice places them by name under the old rules, so adopting moves nothing. */
const USE_075R: Record<string, string[]> = {
  Roof_Bandstand: ['roof:garden-bandstand'], Roof_Radome: ['roof:machine:radome'], Roof_Dish: ['roof:machine:dish'], Roof_Drum: ['roof:machine:drum'], Roof_Fin: ['roof:fin'],
  PlazaFountain: ['garden:fountain'], DecoPavilion: ['garden:pavilion'], CafeSet: ['cafe-set:old'], GrandStair: ['stair:grand'], PlanterBed: ['plaza:planter-bed'], PlanterHex: ['plaza:planter-hex'], FlutedColumn: ['plaza:column'],
  BusShelter: ['tram-shelter:full'], TramCanopy: ['tram-shelter:canopy-full'], // the full old shelters; the _Play copies keep the `tram-shelter` slot
  BistroSet: ['bistro-set'], CafeTable_Tulip: ['cafe-table:tulip'], CafeChair_Tulip: ['cafe-chair:tulip'], CoinTelescope: ['seaside:telescope'], LifebuoyStand: ['seaside:lifebuoy'],
  AlleyBridge: ['alley:bridge'], BikeLean: ['alley:bike-lean'], CableSpan_6: ['alley:cable-span'], CrateStack: ['alley:crate-stack'], MilkCrates: ['alley:milk-crates'], Pallet: ['alley:pallet'],
  PipeBundle_6: ['alley:pipe-bundle'], SteamVent: ['alley:steam-vent'], TrashCans: ['alley:trash-cans'], BackDoorLamp: ['alley:back-door-lamp'],
};
/** What the play camera sees through the pieces that state it (KitSight). */
const SIGHT: Record<string, 'open' | 'glazed'> = { PhoneBooth: 'glazed', PhoneBooth_Play: 'glazed', PhoneBooth_Single: 'glazed', Newsstand: 'glazed', CafeBay_v2: 'glazed', CafeInterior: 'glazed', Sedan_Official: 'open', Sedan_Getaway: 'open', Sedan_Rival: 'open', Sedan_Getaway_v2: 'open' };
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
  CafeSet_Tulip: [0.5, 1.2], PottedPalm_Small: [0.5, 1.4], UtilityCabinet: [0.6, 1.4], CivicCabinet: [0.9, 1.5], CoconutPalm_Street: [0.9, 1.5], RoadMark_Dash_2m: [1, 1], Pave_Slab: [1, 1], Kerb_Pearl_0: [1, 1], Kerb_Pearl_1: [1, 1], TreePit_Pearl: [1, 1], PaveInlay_Band_6m: [1, 1], PaveInlay_Ring_Q: [1, 1], BoardingPavilion_TB_Square: [0.99, 1.01], BoardingPavilion_TB_Office: [0.99, 1.01], AtomFountain_Testbed: [0.99, 1.01], RoadRibbon_Straight_6m: [1, 1], RoadRibbon_Arc90_R4: [1, 1], RoadRibbon_Arc90_R8: [1, 1], RoadMark_Lozenge: [1, 1], RoadMark_Seam: [1, 1], RoadShoulder_Straight_6m: [1, 1], RoadShoulder_Arc90_R4: [1, 1], RoadShoulder_Arc90_R8: [1, 1], Ground_RoadSwatch: [1, 1], // the 10.25 m palm fills the 11 m (washingtonia) and 16 m (phoenix) street-tree species
  // CivicCabinet fills the block's 0.9 x 1.7 m control-cabinet box at 1.5 (brief 026)
 
  Crown_Needle: [0.8, 1.4], Crown_Finial: [0.8, 1.4], Crown_Atom: [0.8, 1.4], Crown_Starburst: [0.8, 1.4], Crown_Saucer: [0.8, 1.4], Crown_Antenna: [0.8, 1.4], Crown_Dish: [0.8, 1.4], // grammar §6
  Horizon_Spire_A: [0.6, 4], Horizon_Spire_B: [0.6, 4], Horizon_Spire_C: [0.6, 4], Horizon_Spire_D: [0.6, 4], Horizon_Massif: [0.6, 4], Horizon_RocketPad: [0.6, 4], Horizon_RocketAscent: [0.6, 4], Horizon_PlanetRinged: [0.6, 5], Horizon_Moon: [0.6, 5], // grammar §9 rings
  Streetlamp: [0.7, 1.6], PlazaSphereLamp: [0.7, 1.6], Bollard: [0.8, 1.4], Bench: [0.9, 1.25], Hydrant: [0.9, 1.3], RecyclingBins: [0.8, 1.3], RecyclingBins_Play: [0.8, 1.3], Hydrant_Play: [0.9, 1.3], Bench_Play: [0.9, 1.25], BikeRack_Play: [1, 1], UtilityCabinet_Play: [0.6, 1.4], PhoneBooth_Play: [0.9, 1.1], PhoneBooth: [0.9, 1.1], TicketMachine: [0.9, 1.15],
  CCTVPole: [0.8, 1.8], DomeCamera: [0.6, 1.2], CoconutPalm: [0.5, 2.2], FanPalm: [0.5, 2.2], Cypress: [0.6, 2.2], AdColumn: [0.9, 1.2],
  BladeSign_Cafe: [0.5, 1], BladeSign_Hotel: [0.5, 1], BladeSign_Cinema: [0.5, 1], NeonStar_Wall: [0.4, 1.2],
  SignalLens: [1, 1], HaloRing: [1, 1], LightBar_4: [1, 1], LightDotRow_2: [1, 1], LightDotRow_1: [1, 1], // crossing parts: pinned to the plan's numbers, scale 1 (the retired SignalEye, KerbStud, CrossEdgeLine, CrossInlayArc and LightField keep the category default: nothing places them)
  SphereStation: [0.4, 1.5], Skyport_Tower: [0.4, 1.5], CircusMonument: [0.4, 1.5],
  // brief 020: built to their places, scale 1 (the café, the cars, the roof pieces: regenerate rather than stretch)
  CafeBay_v2: [1, 1], CafeInterior: [1, 1], Sedan_Official: [1, 1], Sedan_Getaway: [1, 1], Sedan_Rival: [1, 1], Sedan_Getaway_v2: [1, 1], CafeUpper_3: [1, 1], CafeUpper_4: [1, 1],
  Roof_Tank: [1, 1], Roof_Mast: [1, 1], Roof_Tank_Play: [1, 1], Roof_Mast_Play: [1, 1], Roof_LatticeMast: [1, 1], Roof_Pool: [1, 1], Roof_LapPool: [1, 1], Roof_Tennis: [1, 1], Roof_Conservatory: [1, 1], Roof_Cabana: [1, 1], Roof_Planter: [1, 1],
  // brief 026: ground pieces are refit to the plot edge they lie on (a kerb module to its edge's length, a flag cell to its plot, the kerb stone up to the pavement's 0.185 face): 0.75-1.25; the rest stand at scale 1
  Kerb_Granite_0: [0.75, 1.25], Kerb_Granite_1: [0.75, 1.25], Pave_Flags_0: [0.75, 1.25], Pave_Flags_1: [0.75, 1.25], Pave_Flags_2: [0.75, 1.25], Pave_Flags_3: [0.75, 1.25],
  TreePit_Flags_0: [1, 1], TreePit_Flags_1: [1, 1], TreePit_Flags_2: [1, 1], TreePit_Flags_3: [1, 1], Tile_Tactile: [1, 1], Tile_Tactile_Play: [1, 1], SandwichBoard_Play: [1, 1], Streetlamp_Play: [0.7, 1.6], Bollard_Play: [0.8, 1.4], Tile_Terrazzo: [1, 1], ManholeCover: [1, 1], DrainGrate: [1, 1],
  RobotPorter: [1, 1], RobotPorter_Play: [1, 1], RobotWaiter: [1, 1], RobotWaiter_Play: [1, 1], RobotNanny: [1, 1], RobotNanny_Play: [1, 1], RobotShopkeeper: [1, 1], RobotShopkeeper_Play: [1, 1],
  VendingMachine_Cola: [1, 1], VendingMachine_Snack: [1, 1], Automat: [1, 1], BikeRack: [1, 1], InfoLightbox: [1, 1], SandwichBoard: [1, 1], HangingBasket: [1, 1], Lockers: [1, 1], Lockers_Play: [1, 1], VendingMachine_Cola_Play: [1, 1], VendingMachine_Snack_Play: [1, 1], RoundBench: [1, 1], DrinkingFountain: [1, 1],
  PottedTopiary: [1, 1], TopiaryCone: [1, 1], ShrubMound: [1, 1], FlowerBed: [1, 1], FacetedPot: [1, 1],
  // street pieces whose size is not the plan's catalogue size: a tub palm is 4.5 m in the plan (a fern is 1.1 m: scaled up to 2.2x it is a 2.4 m potted fern), the newsstand 3 x 1.5 x 2.8 m (the kiosk is 6 x 4.4 x 6.5 m: scaled to 0.4), the two tram shelters a little under their pieces
  PottedFern: [0.9, 2.2], FanPalm_Play: [0.5, 2.2], PhoneBooth_Single: [1, 1], Newsstand: [1, 1], CurvedKiosk_Play: [0.4, 1.1], BusShelter_Play: [0.7, 1.1], TramCanopy_Play: [0.8, 1.1], Tram: [1, 1], Tile_TramRail: [1, 1], Tile_TramRail_12m: [1, 1], Tile_TramRail_Flush: [1, 1], Tile_TramRail_Flush_12m: [1, 1], TicketKiosk: [1, 1], TimetableColumn: [1, 1], 'ParabolicArch@crown-avenue': [1, 1], 'DiscTower@crown-vista': [1, 1], SkyRail_Beam_4m: [1, 1], SkyRail_Pylon_HeadTwin: [1, 1], SkyRail_Pylon_Head: [1, 1], SkyRail_Pylon_Foot: [1, 1], SkyRail_Pylon_Shaft: [1, 1], SkyRail_Pylon_Tall: [1, 1], SkyRail_Car_Head: [1, 1], SkyRail_Car_Mid: [1, 1], AirCar_Coral: [1, 1], AirCar_Teal: [1, 1], AirCar_Saucer: [1, 1], HoverScooter: [1, 1], // brief 075 roll-out (Vela): the sky track parts fit each other (beam section, head pads at the beam centres, the shaft's section) and the vehicles are drawn at their own size: never scaled // brief 075: at their own size (a scaled tram leaves its gauge; the track tiles butt end to end)
};
/** Pieces that can hold a body (brief 013): a closed container big enough to lie a person in. Only the Dumpster today; adopting a piece is Vela's call, tagging it is not. */
const STASH: Record<string, KitStash> = { Dumpster: { holds: 1 }, Dumpster_Civic: { holds: 1 } }; // brief 097: the civic container holds what the old one did (quiet.ts reads the first catalogued dumpster, the store's, for the size and holds)
const SQUASH: Record<string, number> = { Streetlamp: 0.7, Streetlamp_Play: 0.7, CCTVPole: 0.5 };
/** Where the working end reaches from the pivot: the streetlamp's pole is at the pivot, its head 1.25 m out along local -X. */
const REACH: Record<string, KitReach> = { Streetlamp: { axis: '-x', m: 1.25 }, Streetlamp_Play: { axis: '-x', m: 1.25 } };
const USE_BY_CATEGORY: Record<string, string> = { Street: 'street', Signals: 'street', Booths: 'street', Furnish: 'street', Plants: 'tree', Rooftop: 'roof', Signage: 'sign', Facades: 'facade', Skyline: 'skyline', Highrise: 'tower', Landmarks: 'landmark', Skyport: 'landmark', Ground: 'ground', Alleys: 'alley', Plaza: 'plaza', Structures: 'structure', Sky: 'sky', Vehicles: 'vehicle', Robots: 'cast', Calibration: 'calibration', Crowns: 'crown', Horizon: 'horizon' };

const PLAIN_USE: Record<string, string[]> = { 'LinerDock@liner-berth': ['landmark:liner-berth'], OceanLiner: ['landmark:ocean-liner'],
  // brief 075 golden view (Vela): the old tram and its track, and the stop's ticket kiosk and timetable column (tools/decokit vehicles.py, ground.py, tram075.py). Their own tags, never the
  // category's generic `vehicle` / `ground` / `structure` / `street`: Splice places them by rule (slotOf today has no case for kiosk-tickets or timetable-column, so tagging them moves no item).
  Tram: ['vehicle:tram'], Tile_TramRail: ['ground:tram-rail'], Tile_TramRail_12m: ['ground:tram-rail:12m'], Tile_TramRail_Flush: ['ground:tram-rail:flush'], Tile_TramRail_Flush_12m: ['ground:tram-rail:flush:12m'], TicketKiosk: ['kiosk-tickets'], TimetableColumn: ['timetable-column'],
  // brief 075 golden vista (Vela): the overhead wires (tram075.py; retired the same day, see below), the Crown Avenue arch (placed by id from the plan's gateway) and the gold Crown Vista tower (vista075.py; the plan's landmark names it by id)
  TramWire_Contact: [], TramWire_Span: [], TramWire_Hanger: [], TramWire_Rosette: [], TramWire_Pole: [], // retired, brief 075 (Sky 2026-10-28: "no overhead wires though!"; the trams draw power another way): status rejected in kit.json, no use tag, nothing places them (world rule TS-W0 fails any in the export)
  'ParabolicArch@crown-avenue': ['gateway:crown-avenue'], SkyRail_Beam_4m: ['skyrail:beam'], SkyRail_Pylon_HeadTwin: ['skyrail:head-twin'], SkyRail_Pylon_Head: ['skyrail:head'], SkyRail_Pylon_Foot: ['skyrail:foot'], SkyRail_Pylon_Shaft: ['skyrail:shaft'], SkyRail_Pylon_Tall: ['skyrail:foot-tall'], SkyRail_Car_Head: ['vehicle:skyrail-head'], SkyRail_Car_Mid: ['vehicle:skyrail-mid'], AirCar_Coral: ['vehicle:aircar'], AirCar_Teal: ['vehicle:aircar'], AirCar_Saucer: ['vehicle:aircar'], HoverScooter: ['vehicle:scooter'], // brief 075 roll-out (Vela): the old sky track kit (beam tile, twin head, the old pylon parts) by its own tags, for Splice's S-K1 and Latch's sweep; the presentation vehicles (Pulse names them by id) never by the generic `vehicle` / `sky` / `landmark` 'DiscTower@crown-vista': ['landmark:crown-vista'], // OceanLiner: the moored liner on the water site Datum is siting beside the berth (a landmark once the plan has it)
  PortholeEnd_7: ['stream-end:porthole-7'], BalconyEnd_8: ['stream-end:balcony-8'], HallBay: ['stream:hall-bay'], HallCornerWing: ['stream:hall-corner'], // brief 075 old buildings (Vela): the old class table's intended P7 / B8 ends (caps075.py) and the old civic hall class H (bay, end and corner); placed by name, never by `facade`
  PlanterArc: ['planter:fountain-garden'], SkyRailStation_Twin: ['station:twin'], SkyRailStation_Decks: ['station:decks'], ParadeGate_Block: ['portal:parade-gate'],
  // the crossing's pieces (brief 009 A, Vela; adopted by Splice): the generator places a part by its use tag and never scales it
  // brief 061 (Sky 2026-10-25, signal head A): the head is SignalLens (the floating lens, oriented, with state and clock) and its clock ring HaloRing (one radial quad in the paving, no export part: Latch places it from `signal.halo`);
  // the dot rows are the re-cut LightDotRow (no brass frame). SignalEye, KerbStud, CrossEdgeLine_1/2, CrossInlayArc_R24_W08 and LightField_1/2 are retired (status rejected in kit.json, no use tag: nothing places them).
  // brief 097 (Vela): the Handover zone's props that drew as procedural boxes (tools/decokit/civic_props097.py), placed by the street item kind (kitrefs.ts slotOf)
  // brief 097 pass 2 (Vela, civic_props097b.py): play-zoom rebuilds of the zone's weakest store pieces at the store pieces' sizes; the store pieces keep a retired tag (bin:full, vending:full, lockers:full), as PneumaticPost / CoffeeCart
  // brief 097 full pass (Vela, civic_props097c.py): the audit's rebuilds at the store pieces' sizes; the store pieces keep retired tags (bench:full, hydrant:full, bike-rack:full)
  Streetlamp_Play: ['zone:Streetlamp'], Bollard_Play: ['zone:Bollard'], // brief 097 final pass: zone twins (kitrefs.ts streetKit): inside the Handover zone an item that took Streetlamp or Bollard draws its twin; outside, the old piece
  Hydrant_Play: ['hydrant'], Bench_Play: ['bench'], BikeRack_Play: ['bike-rack'], UtilityCabinet_Play: ['street'],
  Lockers_Play: ['lockers'], RecyclingBins_Play: ['bin'], VendingMachine_Cola_Play: ['vending'], VendingMachine_Snack_Play: ['vending'],
  PolicePost: ['police-post'], MastCamera: ['mast-camera'], CivicRobotDock: ['civic-robot-dock'], Dumpster_Civic: ['dumpster'],
  SignalLens: ['signal:lens'], HaloRing: ['signal:halo'], LightBar_4: ['crossing:bar'], LightDotRow_2: ['crossing:dots'], LightDotRow_1: ['crossing:dots'] };

/** Brief 072-C (Sky 2026-10-27, "every" face; side walls "a)"): the every-face pieces (tools/world/gen/faces.ts places them by these names, never by a use tag the
 *  wave-2 street bays read: `face:*` is not `facade`, so facades.ts never takes a side bay for a street bay). Width and height scale 0.85-1.15 (6 m modules tiled
 *  along any face, a storey band's height), squash 0.7 (a narrow module on a tall band: 0.85 / 1.15). Vela's briefs/072/pieces/pieces.md is the final list. */
const FACE_072 = /^(SideBay_[34]|SideBayStack_(Base|Mid|Top)|CornerPier_(Out|In)_(3|4|Base|Mid|Top)|AlleyWall_[34]D?|SidePilaster_(3|4|Base|Mid|Top)|ShopFill_[34]|ShopStack_Base)$/;
const STREAM_075 = /^Stream(Bay|Corner|End)Stack_(Base|Mid|Top)$/;
const faceUse = (name: string): string[] => [/^Shop(Fill|Stack)/.test(name) ? 'face:shop' : /^SidePilaster/.test(name) ? 'face:pilaster' : /^SideBayStack/.test(name) ? 'face:stack' : /^SideBay/.test(name) ? 'face:side' : /^CornerPier_Out/.test(name) ? 'face:pier-out' : /^CornerPier_In/.test(name) ? 'face:pier-in' : 'face:service'];

/** Brief 075 old buildings: the caps with a mirrored twin (tools/decokit/caps075.py MIRRORED). */
const MIRROR_075 = /^(Stream(End|Corner)_(2|3|3W|4|6|9|9W)|Deco(End|Corner)_10|PortholeEnd_7|BalconyEnd_8|HallCornerWing|CornerTower_12|Stream(End|Corner)Stack_(Base|Mid|Top))$/;
export function tagsOf(name: string, category: string): KitTags {
  // brief 075 old buildings (Vela, tools/decokit/caps075.py): a cap's mirrored twin `<id>_M` (the old placer's mirror:, which the export lacks) takes its base's tags with
  // every use prefixed `mirror:`, so no picker that reads the base's use (facade, skyline, face:stream-*) ever draws a mirrored cap by chance; Splice places them by name.
  const mir = /^(.+)_M$/.exec(name);
  if (mir && MIRROR_075.test(mir[1]!)) { const t = tagsOf(mir[1]!, category); return { ...t, use: t.use.map(u => `mirror:${u}`) }; }
  if (FACE_072.test(name)) return /^Shop(Fill|Stack)/.test(name) ? { use: faceUse(name), scale: [0.5, 1.7], squash: 0.2, wave: 2 } : /^SidePilaster/.test(name) ? { use: faceUse(name), scale: [0.33, 1.7], squash: 0.2, wave: 2 } : { use: faceUse(name), scale: [0.85, 1.15], squash: 0.7, wave: 2 }; // a pilaster takes 0.5-2.5 m of a run (1.5 m nominal); the shop fillers (Vela, 072 pass 2: 3 m nominal) 1.5-5.1 m, tiled at 0.5-1.7 so any run of 1.5 m or more takes whole modules
  // brief 075-E (Vela): the streamline stack modules (tools/decokit/streamstack.py). Splice places them by name (tools/world/gen/faces.ts); their use is `face:*`, never `facade`, so
  // the wave-2 street-bay picker never takes a storey band for a whole bay. A bay tiles at 0.85-1.15 like SideBayStack; the corner and end keep their plan (the round's radius) at 0.9-1.1.
  if (STREAM_075.test(name)) { const f = /Corner/.test(name) ? 'corner' : /End/.test(name) ? 'end' : 'bay'; return { use: [`face:stream-${f}`], form: f, scale: f === 'bay' ? [0.85, 1.15] : [0.9, 1.1], squash: f === 'bay' ? 0.7 : 1, wave: 2 }; }
  if (name === 'RecordsTower') return { use: ['landmark:records-tower'], scale: [1, 1], squash: 1, wave: 2 }; // brief 072: built to the Records Office (build.ts recordsTower), scale 1
  const form = /Corner/.test(name) ? 'corner' : /End/.test(name) ? 'end' : /Bay/.test(name) ? 'bay' : undefined;
  return {
    use: USE[name] ?? USE_020[name] ?? USE_026[name] ?? USE_075R[name] ?? PLAIN_USE[name] ?? [USE_BY_CATEGORY[category] ?? category.toLowerCase()],
    ...(TRADE[name] ? { trade: TRADE[name] } : {}), ...(form && ['Facades', 'Skyline'].includes(category) ? { form } : {}),
    ...(REACH[name] ? { reach: REACH[name] } : {}), ...(STASH[name] ? { stash: STASH[name] } : {}), scale: SCALE[name] ?? (USE_075R[name] ? [1, 1] : [0.9, 1.1]), squash: SQUASH[name] ?? 1, wave: WAVE1.has(name) ? 1 : 2,
    ...(SIGHT[name] ? { sight: SIGHT[name] } : {}),
  };
}
