// The capstone programme's rules (brief 026, Sky 2026-10-20: the old DecoKit city is the floor): what the generator places on the Handover block and its approach streets,
// and where a real city would put it. One table: the generator (capstone.ts) reads it to place, the world rules (tools/world/rules/capstone.ts, CAP-1..CAP-5) read it to judge,
// so what is checked is what is placed. Nothing here is a coordinate: every rule is over plan data (plot edges, road kerbs, frontage trades, crossings, trees, the civic block's terrace).
// Starting values are [S], tune by playtest. The old DecoKit placements (explicit coordinates in the old repo's DecoKit*Layout.cs) are vocabulary only; their rule sources are in
// briefs/026/splice/round1.md.
import type { V } from '../../../plan/geom';

/** The block's walk window (tools/plan/walk-grid.ts WINDOW, block frame) grown by this many metres: the capstone block and the streets that approach it. */
export const MARGIN = 30;

/** Flat ground pieces: lie in the pavement or the carriageway, never stand in a zone (street-zones.ts FLAT takes these kinds). */
export const FLAT_KINDS = ['kerb', 'pave-flags', 'tree-pit', 'drain', 'manhole', 'tactile', 'terrazzo'] as const;
/** Standing pieces the programme places (street items with a kit ref; they take the street layer's tests: SF-1 walk zone, SF-2 doors and crossings, SF-8 corner sight, drives). */
export const STANDING_KINDS = ['service-robot', 'vending-machine', 'automat', 'bike-rack', 'info-lightbox', 'sandwich-board', 'hanging-basket', 'lockers', 'planter-bed', 'round-bench', 'drinking-fountain', 'window-box'] as const;
export const CAPSTONE_KINDS: ReadonlySet<string> = new Set<string>([...FLAT_KINDS, ...STANDING_KINDS]);
export const FLAT_SET: ReadonlySet<string> = new Set<string>(FLAT_KINDS);

/** Heights (m above the pavement top, 0.18) the flat pieces lie at: the slab is the ground, each layer stands a hair above the one below so none z-fights (Glint's 12 mm lesson on the kerb strip). */
export const LAYER = {
  flags: 0.001, // Pave_Flags_* are zero-thickness quads (2 triangles): 1 mm over the slab, under the crossing's inlay (0.002)
  tile: 0.003, // thick tiles (tactile 0.165, terrazzo 0.1505, tree pit 0.102): their top is 3 mm over the slab, over the flags
  kerb: 0.005, // the kerb stone's top (Kerb_Granite_* is 0.154 high, scaled up to this + 0.18 so the stone stands 5 mm proud of the slab and its face is the full 0.185)
  gutter: 0, // drains and manholes lie on the carriageway (z 0)
};
export const PAVE = 0.18;

export const RULES = {
  /** G1: Kerb_Granite_0/1 on every plot edge that is a carriageway's kerb face: centred on the edge (stone on the pavement side, gutter setts on the road side), modules of 2.0 m refit to the edge. The piece's +Z front faces the road. */
  kerb: { module: 2, pieces: ['Kerb_Pearl_0', 'Kerb_Pearl_1'], halfDepth: 0.15, inset: 0.15 /* the pearl stone has no gutter: its road-side face stands on the plot edge, so its centre is half its depth inside it */, tol: 0.15, minEdge: 2 },
  /** G2: Pave_Flags_0..3 on each plot's pavement in a grid anchored to the plot's corner (cells 2 m refit to the plot): the sheet quadrant by cell parity, as the old paving.py did. */
  flags: { cell: 2, pieces: ['Pave_Slab'], skipUse: ['park', 'open', 'square', 'civic', 'water'], reach: 0.5 /* m past the street section's pavement a cell may start */ },
  /** G3: TreePit_Flags_0..3 under every pavement tree (the pit centred on the trunk). */
  pit: { pieces: ['TreePit_Pearl'] },
  /** G4: DrainGrate in the gutter every `every` m along each kerb edge (first at `first`), long axis along the kerb, `off` m out from the kerb face; ManholeCover down each lane every `lane` m and along each pavement's walk every `walk` m. */
  drain: { piece: 'DrainGrate', every: 20, first: 10, off: 0.62 },
  manhole: { piece: 'ManholeCover', lane: 36, walk: 48, clear: 1.2 },
  /** G5: Tile_Tactile, two across, at every crossing landing (blister paving where the kerb drops); Tile_Terrazzo, two across by one deep, at the entrance of every lobby and bureau tenancy. */
  tactile: { piece: 'Tile_Tactile' },
  terrazzo: { piece: 'Tile_Terrazzo', trades: ['lobby', 'hotel-lobby', 'credit-bureau', 'employment-bureau', 'hiring-hall'], across: 2 },
  /** The Handover points stay clear: nothing standing within this of one (CD-1's 1.2 m; flat pieces other than inlay too). */
  pointClear: 1.2,

  /** Robots stand where their job is. `role` names the use tag (robot:<role>); the lightest adopted piece of the role is drawn (a _Play copy before the full model). */
  robots: {
    waiter: { trades: ['diner', 'soda-fountain'], minW: 9, inFront: 1.1, note: 'one per terrace run, at its open end, on the terrace; the civic café terrace takes one at its east end' },
    shopkeeper: { trades: ['florist', 'barber', 'pawnbroker', 'radio-tv-repair', 'beauty-salon', 'launderette', 'grocer', 'record-shop', 'tailor', 'camera-shop', 'couture-boutique'], inFront: 0.9, note: 'beside the shopfront, never in the doorway (SF-2 keeps 2 m off the door)' },
    butler: { trades: ['employment-bureau', 'credit-bureau', 'hotel-lobby', 'hiring-hall'], inFront: 0.9, note: 'at the door of an office or a hotel' },
    nanny: { plots: ['open', 'park'], note: 'in a garden: beside a bench or a path, never on the walking line' },
    porter: { reach: 100, note: 'at the tram stop or the boarding pavilion (the civic square pavilion)' },
  },
  /** Vending machines in pairs (Cola + Snack, alternating) backed on the frontage wall of a tenancy that people wait at, at the tenancy's end nearer a junction; the Automat on an automat tenancy's wall. */
  vending: { trades: ['diner', 'soda-fountain', 'launderette', 'bowling', 'cinema', 'dance-hall'], minW: 12, pair: ['VendingMachine_Cola', 'VendingMachine_Snack'], backGap: 0.06 },
  automat: { trades: ['automat'], piece: 'Automat', backGap: 0.06 },
  /** Kerb programme: bike racks at frontages people leave a bike at, one per `every` m of one street side; lightboxes at junction corners; boards, baskets and lockers by trade. */
  bikeRack: { trades: ['diner', 'soda-fountain', 'newsstand', 'employment-bureau', 'launderette', 'lobby', 'credit-bureau'], every: 40, piece: 'BikeRack', off: 0.45 },
  lightbox: { piece: 'InfoLightbox', from: 8, to: 20, apart: 24 /* between lightboxes and from a call box */ },
  board: { trades: ['diner', 'soda-fountain', 'florist', 'grocer', 'automat'], minW: 9, piece: 'SandwichBoard', out: 1.4 },
  basket: { trades: ['diner', 'soda-fountain', 'florist', 'grocer'], piece: 'HangingBasket', pitch: 3, minW: 6 },
  lockers: { trades: ['employment-bureau', 'credit-bureau'], piece: 'Lockers' },
  planters: { trades: ['lobby', 'hotel-lobby', 'employment-bureau', 'credit-bureau'], pieces: ['PottedTopiary', 'TopiaryCone'], minW: 9 },
  fountain: { piece: 'DrinkingFountain', every: 80 },
  /** Planting (Vela round 2, the old city's siting): a flower trough and topiary at each run end of a frontage; urns flanking every fourth tenancy seam; shrub mounds in a garden's border every `every` m; window boxes on the sills of shop trades. */
  runEnd: { bed: 'FlowerBed', cone: 'TopiaryCone', palm: 'PottedPalm_Small', in: 0.15 },
  urn: { piece: 'Planter_Urn_Play', everyNth: 4 },
  shrub: { piece: 'ShrubMound', every: 10, beyond: 1.0 },
  windowBox: { piece: 'WindowBox', trades: ['florist', 'grocer', 'diner', 'soda-fountain', 'barber', 'beauty-salon', 'tailor', 'record-shop', 'camera-shop'], pitch: 3, doorClear: 1.7, sill: 0.95, minW: 6 },
  roundBench: { piece: 'RoundBench', everyTree: 3 },
};

/** The use tags the kit carries for the programme's pieces (tools/kit/tags.ts): the generator picks pieces by these. */
export const USE = {
  robot: (role: string) => `robot:${role}`,
};

export type Rect4 = [number, number, number, number];
export const inRect4 = (p: V, r: Rect4) => p[0] >= r[0] && p[0] <= r[1] && p[1] >= r[2] && p[1] <= r[3];
