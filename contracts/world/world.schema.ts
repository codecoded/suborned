// suborned.world/1: the derived Harbour District, written by `bun run world` (tools/plan/export.ts) to
// out/world/<plan id>.world.json. Generated; never hand-edited. Unity reads it and re-derives nothing.
//
// Frame: metres. Plan x = east, plan y = north. Unity position = (x, height, y): plan y becomes Unity z.
// Every point is [x, y] in that plan frame; height is separate. Curves are pre-tessellated (about 2 m chords).
// Heights are metres above carriageway level (0). Polygons are closed implicitly (last point does not repeat
// the first) and may wind either way. Arrays keep plan order, so output is deterministic.
//
// Every exported polygon is SIMPLE (export fails otherwise): no crossings, no touching edges, no repeated points, no
// zero-width spurs. Unity builds straight from these polygons and re-derives nothing. A closed road (the gyratory) is an
// annulus: `carriageway`/`kerb` are its outer rings and `hole` holds the matching inner rings.

export type P = [number, number];

export type Heights = {
  carriageway: 0;
  pavement: number;     // pavements, plot ground, quay: above the carriageway (0.18)
  skyRailBeam: number;  // beam level above the carriageway (24)
};

/** Where the world came from: sha256 of the plan file plus the civic block files it reads. Builders show it. */
export type Source = { planHash: string };

export type Road = {
  id: string; name: string;
  class: 'primary' | 'district' | 'access';
  width: number;            // carriageway width
  gyratory: boolean;        // the Crown Circus ring
  closed: boolean;          // centreline is a loop: carriageway/kerb are outer rings and `hole` is set
  centreline: P[];
  carriageway: P[];         // polygon, width wide. Open roads: where two road ends meet at a corner, the polygon runs on by the other road's half width, so corners have no hole. Closed: outer ring.
  kerb: P[];                // polygon, carriageway + 0.8 m each side (the kerb strip lies under the carriageway). Closed: outer ring.
  hole?: { carriageway: P[]; kerb: P[] };  // closed roads only: the inner ring of each (the island side). Carriageway = outer minus hole.
  reservation?: { width: number; polygon: P[] };  // tram reservation along the road; where the road enters Crown Circus it ends on the circus outer carriageway edge (arc at ring[1])
};
export type Median = { id: string; road: string; polygon: P[] };   // id = <road>-<index within that road>
export type Foot = { id: string; name: string; kind: 'mall' | 'walk'; width: number; centreline: P[]; polygon: P[]; kerb: P[] };
export type Plot = {
  id: string; name?: string; use: string; area: string; civic?: 'square' | 'office';
  built: boolean;           // false = parks, squares, open ground: flat.
  height?: number;          // built plots: metres above the pavement (0.18), from the plan. Absent on the Handover plots (their buildings carry their own heights) and on unbuilt plots.
  polygon: P[];
};
export type Area = { id: string; name: string; polygon: P[]; label?: P };
export type Circus = {
  centre: P;
  garden: P[]; island: P[];   // planted centre, and the island kerb ring
  carriageway: { inner: P[]; outer: P[] };  // the gyratory carriageway between two circles
  pavement: P[][];            // the pavement ring (carriageway outer edge to the plot arcs) as simple polygons, one per sector between road entries. Each entry is cut out at its kerb line (carriageway + 0.8 m), so the roads stay open. Edges lie on the carriageway.outer vertices and on the plot arc vertices: no gaps.
  radii: { garden: number; island: number; carriagewayInner: number; carriagewayOuter: number; pavement: number };
};
export type Water = { water: P[][]; quay: P[][]; beyond: P[][] };

export type Track = {
  line: string; path: string; role: string;
  double: boolean;
  centreline: P[];
  tracks: P[][];            // one polyline per rail track/beam: 2 when double (offset +-gauge/2), else 1
  gauge: number;            // spacing between the two tracks / beams (m)
};
export type SkyBeam = Track & { height: number };
export type Pylon = { id: string; line: string; path: string; at: P; span: number; height: number };
export type Platform = { line: string; id: string; name: string; at: P; dir: P; chainage: number; platforms: P[][]; interchange?: string };
export type Stop = Platform & { kind: 'tram' };
export type Station = Platform & { kind: 'sky'; height: number };
// at = where the gate stands: 11 m in from its junction node along its road (as the CAD sheet draws it), so gates that
// share a node separate. dir = unit vector along the road, away from the node. Seal points come from the mission, not the
// district: the plan has no seal field yet, so none is exported.
export type Gate = { id: string; name: string; at: P; dir: P; road: string; area: string };
export type Landmark = { id: string; name: string; centre: P; radius: number };

export type HandoverBuilding = {
  id: string; name: string; polygon: P[]; base: number; height: number;
  source?: string;                                           // id of the solid in the Oct 9 navigation data
  heights?: { wings: number; crown: number; giantOrder: number };  // the Records Office: height = wings; towers rise to crown
  towers?: { id: string; at: P; height: number }[];          // entrance towers: an anchor on the building face (no footprint in the sources)
};
export type HandoverStructure = HandoverBuilding & { category: string };
export type HandoverObject = { id: string; kind: string; polygon: P[]; base: number; height: number };
export type Handover = {
  note: string;
  blocks: { id: string; outer: P[]; core: P[] }[];   // outer = pavement at 0.18 with the lay-bys cut out of it; core = inside the walk margin
  buildings: HandoverBuilding[];        // base = building floor above carriageway: the ground under the footprint centre (0 on a road, circus ring or lay-by, else pavement 0.18) + the manifest z (height above that ground)
  structures: HandoverStructure[];
  objects: HandoverObject[];            // fountain, planter, table
  bays: { id: string; polygon: P[]; level: number }[]; // officialBay, getawayBay: lay-bys, a surface at carriageway level (level 0), cut out of blocks[].outer; canopies over them stay at 0.18
  crossing: P[];
  points: { name: string; at: P; surface: string }[];   // mission points (the 21 destinations); surface: ground, office-public (door landing, 0.18) or office-official
  crew: { name: string; at: P; surface: string }[];     // crew start positions (Fixer, Tech, Scout, Enforcer)
};

export type World = {
  schema: 'suborned.world/1';
  source: Source;
  id: string; name: string; planVersion: string; planDate: string;
  view: { yaw: number; pitch: number };  // default camera, degrees: yaw about the vertical, pitch down from horizontal (Rig reads it)
  frame: { units: 'm'; x: 'east'; y: 'north'; unity: '(x, height, y)' };
  heights: Heights;
  roads: Road[]; medians: Median[]; foot: Foot[];
  plots: Plot[]; areas: Area[];
  circus: Circus; water: Water;
  tram: Track[]; stops: Stop[];
  skyRail: SkyBeam[]; pylons: Pylon[]; stations: Station[];
  gates: Gate[]; landmarks: Landmark[];
  handover: Handover;
};
