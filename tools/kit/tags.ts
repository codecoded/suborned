// Generator tags per asset: what it is for, its scale range, and (wave 2) form and trade. Wave 1 = first adopted set (brief 006 A).
import type { KitReach, KitStash, KitTags } from '../../contracts/world/kit.schema';

export const WAVE1 = new Set(['SphereStation', 'Skyport_Tower', 'CircusMonument', 'ParabolicArch', 'SkyPad_Roof', 'Streetlamp', 'Bollard', 'Bench', 'Hydrant', 'RecyclingBins', 'PhoneBooth', 'TicketMachine', 'PlazaSphereLamp', 'CCTVPole', 'DomeCamera', 'CoconutPalm', 'FanPalm', 'Cypress', 'BladeSign_Cafe', 'BladeSign_Hotel', 'BladeSign_Cinema', 'NeonStar_Wall', 'AdColumn']);

const USE: Record<string, string[]> = {
  Dumpster: ['alley', 'dumpster'], // the world places it as the street item `dumpster` beside a service door (tools/world/civic/quiet.ts)
  Streetlamp: ['lamp'], PlazaSphereLamp: ['lamp'], Bollard: ['bollard'], Bench: ['bench'], Hydrant: ['hydrant'], RecyclingBins: ['bin'], PhoneBooth: ['phone-booth:twin'], // the 2.6 m twin cabinet: retired from the phone-booth slot for PhoneBooth_Single (brief 020 follow-up)
  TicketMachine: ['ticket-machine'],
  CCTVPole: ['pole-camera'], DomeCamera: ['canopy-camera'], CoconutPalm: ['tree', 'tub-palm'], FanPalm: ['tub-palm:full'], // the 4.2k-triangle palm: retired from the tub-palm slot for FanPalm_Play (brief 020 follow-up); a bare palm with no pot, so it can stand in a planter's bed (a potted fern cannot)
  Cypress: ['tree'], AdColumn: ['advertising-column'],
  BladeSign_Cafe: ['blade-sign'], BladeSign_Hotel: ['blade-sign'], BladeSign_Cinema: ['blade-sign'], NeonStar_Wall: ['wall-sign'],
  SphereStation: ['landmark:sphere'], SkyPad_Roof: ['landmark:skyport'], CircusMonument: ['landmark:monument'],
};
/** The kinds and slots the world gives the pieces brief 020 adopted (Vela's pick, Splice's wiring): street kinds by the street item kind, roof pieces by the part of a programme they stand for (tools/world/gen/roofs.ts), the café and the cars by their place. */
const USE_020: Record<string, string[]> = {
  CafeBay_v2: ['frontage:cafe-bay'], CafeInterior: ['frontage:cafe-interior'], Sedan_Official: ['vehicle:car-official'], Sedan_Getaway: ['vehicle:car-getaway'],
  Roof_Tank: ['roof:water-sphere'], Roof_Mast: ['roof:antenna-mast'], Roof_LatticeMast: ['roof:antenna-mast'], Roof_Pool: ['roof:pool'], Roof_LapPool: ['roof:pool'], Roof_Tennis: ['roof:tennis-court'],
  Roof_Conservatory: ['roof:garden-house'], Roof_Cabana: ['roof:garden-shade'], Roof_Planter: ['roof:garden-bed'],
  Planter_Trough: ['planter'], Planter_Urn: ['planter:full'], Planter_Urn_Play: ['planter'], PottedFern: ['tub-palm'], PneumaticPost: ['mailbox:tall'], PneumaticPost_Play: ['mailbox'], // the _Play copies (1.1-1.4k triangles, the plan's 1.3 m box for the mailbox) take the slots; the full pieces keep a retired tag
  FanPalm_Play: ['tub-palm', 'tub-palm:planted'], PhoneBooth_Single: ['phone-booth'], Newsstand: ['newsstand'], StreetClock: ['public-clock'], BusShelter_Play: ['tram-shelter'], TramCanopy_Play: ['tram-shelter'], CurvedKiosk_Play: ['newsstand:scaled'], // retired from the newsstand slot for Newsstand (3.2 x 1.9 x 2.8 m, no scaling)
  CoffeeCart_Play: ['kiosk-food'],
};
/** What the play camera sees through the pieces that state it (KitSight). */
const SIGHT: Record<string, 'open' | 'glazed'> = { PhoneBooth_Single: 'glazed', Newsstand: 'glazed', CafeBay_v2: 'glazed', CafeInterior: 'glazed', Sedan_Official: 'open', Sedan_Getaway: 'open' };
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
  CafeSet_Tulip: [0.5, 1.2], PottedPalm_Small: [0.5, 1.4], UtilityCabinet: [0.6, 1.4],
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
  // street pieces whose size is not the plan's catalogue size: a tub palm is 4.5 m in the plan (a fern is 1.1 m: scaled up to 2.2x it is a 2.4 m potted fern), the newsstand 3 x 1.5 x 2.8 m (the kiosk is 6 x 4.4 x 6.5 m: scaled to 0.4), the two tram shelters a little under their pieces
  PottedFern: [0.9, 2.2], FanPalm_Play: [0.5, 2.2], PhoneBooth_Single: [1, 1], Newsstand: [1, 1], CurvedKiosk_Play: [0.4, 1.1], BusShelter_Play: [0.7, 1.1], TramCanopy_Play: [0.8, 1.1],
};
/** Pieces that can hold a body (brief 013): a closed container big enough to lie a person in. Only the Dumpster today; adopting a piece is Vela's call, tagging it is not. */
const STASH: Record<string, KitStash> = { Dumpster: { holds: 1 } };
const SQUASH: Record<string, number> = { Streetlamp: 0.7, CCTVPole: 0.5 }; // trees and palms scale uniformly (squash 1)
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
    use: USE[name] ?? USE_020[name] ?? PLAIN_USE[name] ?? [USE_BY_CATEGORY[category] ?? category.toLowerCase()],
    ...(TRADE[name] ? { trade: TRADE[name] } : {}), ...(form && ['Facades', 'Skyline'].includes(category) ? { form } : {}),
    ...(REACH[name] ? { reach: REACH[name] } : {}), ...(STASH[name] ? { stash: STASH[name] } : {}), scale: SCALE[name] ?? [0.9, 1.1], squash: SQUASH[name] ?? 1, wave: WAVE1.has(name) ? 1 : 2,
    ...(SIGHT[name] ? { sight: SIGHT[name] } : {}),
  };
}
