// Brief 075-E (Sky 2026-10-28: "we've still lost all the lovely curved buildings"): the streamline grammar the street pass (facades.ts) and the
// every-face pass (faces.ts) share, from the old DecoKit street's composition (briefs/075/composition.md, composition.json; the old
// Editor/DecoKit/DecoKitFrontage.cs class table and DecoKitCityBlocks.cs / DecoKitSouthside.cs runs).
//   - the class table: each frontage class is a bay with its end and corner (old DecoKitFrontage.Of, fallbacks where our kit lacks the old piece);
//   - the recipe: the share of buildings (old run segments) per class family, the weights a building's class is drawn with;
//   - the kit frame of the old corner and end pieces, so both passes place them the same way round.
// Kit frame (contract, tools/decokit/export_kit.py: kit axes are Unity's, x = -Blender x; KitPlacer: plan yaw θ turns kit +X to (cos θ, sin θ)):
// a piece yawed to face a CCW ring edge's outward normal n (yaw = atan2(n) - 90°) has kit +X along -t (against the ring). KIT_X records that.
// The old pieces are not symmetric: StreamEnd's rounded return and StreamCorner's round stand on kit +X (old DecoKitFrontage: "unmirrored it rounds toward
// the module's +X"), and the export has no mirror. So an unmirrored end closes the TAIL of the edge it fronts (the run's start), and a corner at a convex
// vertex takes the LEAVING edge as its front (its +X side then runs along the incoming face). Brief 007's street pass placed both the other way round
// (ends at the head, corners fronting the incoming edge: the round and return fell on the wrong side, the cell 1.63 m past the corner); fixed here.
// If the build shows them mirrored, KIT_X = 1 turns every end and corner of both passes round (one switch).
import type { KitAsset } from '../../../contracts/world/kit.schema';
import type { V } from '../../plan/geom';
import { r4 } from './common';

/** -1: kit +X runs along -t of a CCW ring edge the piece fronts (the frame above). */
export const KIT_X: 1 | -1 = -1;

export type StreamClass = 'S' | 'P' | 'B' | 'A' | 'D';
/** A frontage class (old DecoKitFrontage.Of): the bay, the end and corner that close its runs, the storey code those were built to. */
export type StreamRow = { cls: StreamClass; bay: string; end: string; corner: string; code: number };
/** The old class table, fallbacks as the old ones (PortholeEnd_7 / BalconyEnd_8 / ArcadeEnd_3 and HallCornerWing are not in our kit: the old fallbacks stand). */
export const STREAM_ROWS: StreamRow[] = [
  { cls: 'S', bay: 'StreamBay_2', end: 'StreamEnd_2', corner: 'StreamCorner_2', code: 2 },
  { cls: 'S', bay: 'StreamBay_3', end: 'StreamEnd_3', corner: 'StreamCorner_3', code: 3 },
  { cls: 'S', bay: 'StreamBay_3W', end: 'StreamEnd_3W', corner: 'StreamCorner_3', code: 3 },
  { cls: 'S', bay: 'StreamBay_4', end: 'StreamEnd_4', corner: 'StreamCorner_4', code: 4 },
  { cls: 'S', bay: 'StreamBay_6', end: 'StreamEnd_6', corner: 'StreamCorner_6', code: 6 },
  { cls: 'S', bay: 'StreamBay_9', end: 'StreamEnd_9', corner: 'StreamCorner_9', code: 9 },
  { cls: 'S', bay: 'StreamBay_9W', end: 'StreamEnd_9W', corner: 'StreamCorner_9', code: 9 },
  { cls: 'P', bay: 'PortholeBay_3', end: 'StreamEnd_3W', corner: 'StreamCorner_3', code: 3 },
  { cls: 'P', bay: 'PortholeBay_3L', end: 'StreamEnd_3W', corner: 'StreamCorner_3', code: 3 },
  { cls: 'P', bay: 'PortholeBay_4', end: 'StreamEnd_4', corner: 'StreamCorner_4', code: 4 },
  { cls: 'P', bay: 'PortholeBay_4L', end: 'StreamEnd_4', corner: 'StreamCorner_4', code: 4 },
  { cls: 'P', bay: 'PortholeBay_7', end: 'StreamEnd_6', corner: 'StreamCorner_6', code: 7 },
  { cls: 'B', bay: 'BalconyBay_3', end: 'StreamEnd_3', corner: 'StreamCorner_3', code: 3 },
  { cls: 'B', bay: 'BalconyBay_4', end: 'StreamEnd_4', corner: 'StreamCorner_4', code: 4 },
  { cls: 'B', bay: 'BalconyBay_8', end: 'StreamEnd_9W', corner: 'StreamCorner_9', code: 8 },
  { cls: 'A', bay: 'ArcadeBay_3', end: 'StreamEnd_3', corner: 'StreamCorner_3', code: 3 },
  { cls: 'D', bay: 'DecoBay_10', end: 'DecoEnd_10', corner: 'DecoCorner_10', code: 10 },
];
export const rowOf = (bay: string): StreamRow | undefined => STREAM_ROWS.find(r => r.bay === bay);
export const classOf = (bay: string): StreamClass | undefined => rowOf(bay)?.cls;

/** The recipe: the share of the old street's buildings (run segments) per class family, counted from the old runs (briefs/075/composition.json
 *  `recipe.buildings`: S 76, P 10, B 7, D 6, A 3 of 102 segments, the civic hall left out); a building's class is drawn with these weights. */
export const RECIPE: Record<StreamClass, number> = { S: 76 / 102, P: 10 / 102, B: 7 / 102, D: 6 / 102, A: 3 / 102 };

/** Draw one of `items` with the recipe's weights (a class's weight split evenly among its items), from f in [0, 1). */
export function recipePick<T>(items: T[], cls: (t: T) => StreamClass, f: number): T {
  const n = new Map<StreamClass, number>(); for (const t of items) n.set(cls(t), (n.get(cls(t)) ?? 0) + 1);
  const w = items.map(t => RECIPE[cls(t)] / n.get(cls(t))!), sum = w.reduce((a, b) => a + b, 0);
  let x = f * sum;
  for (let i = 0; i < items.length; i++) { x -= w[i]!; if (x < 0) return items[i]!; }
  return items[items.length - 1]!;
}

/** The storey code's bay height (old skyline.py / lowrise.py / highrise.py): an end or corner of code c is scaled by it, its fin and mast aside. */
export const CODE_H: Record<number, number> = { 2: 12.9, 3: 16.3, 4: 19.7, 6: 26.5, 9: 36.7, 10: 43.6 };
const codeOfId = (id: string): number => Number(/_(\d+)/.exec(id)?.[1] ?? 0);
/** The height an end or corner piece is scaled by (its code's bay height), else its own height. */
export const capBody = (a: KitAsset): number => CODE_H[codeOfId(a.id)] ?? a.size[1];

const yawOf = (n: V) => r4(Math.atan2(n[1], n[0]) - Math.PI / 2);
/** The corner cell of a corner piece: `half` = half its cell along each face (from its bounds: size/2 - the footprint centre's x), `proud` = how far its
 *  frame corner stands out of both faces (old DecoKitFrontage: CornerTower_12's 10 m cell, 2 m proud). `onFace` = the cell's length on each face. */
export function cornerCell(a: KitAsset): { half: number; proud: number; onFace: number } {
  const half = /^CornerTower/.test(a.id) ? 5 : a.size[0] / 2 - (a.pivotOffset?.[0] ?? 0), proud = /^CornerTower/.test(a.id) ? 2 : 0;
  return { half, proud, onFace: 2 * half - proud };
}

/** A corner piece at convex vertex v between the incoming face (outward normal n0) and the leaving face (n1), at scale s (x and z): the export point (its
 *  footprint centre) and yaw. Its front takes the leaving face (KIT_X -1), its +X side the incoming one; the frame corner is v (+ proud out of both). */
export function cornerPlace(v: V, n0: V, n1: V, a: KitAsset, s: number): { at: V; yaw: number; onFace: number } {
  const { half, proud, onFace } = cornerCell(a), [cx, cz] = a.pivotOffset ?? [0, 0];
  const front = KIT_X < 0 ? n1 : n0, side = KIT_X < 0 ? n0 : n1, c: V = [v[0] + proud * s * (n0[0] + n1[0]), v[1] + proud * s * (n0[1] + n1[1])];
  // the footprint centre from the frame corner: (cx - half) along the side normal, cz along the front normal (both negative: back into the building)
  const at: V = [c[0] + side[0] * (cx - half) * s + front[0] * cz * s, c[1] + side[1] * (cx - half) * s + front[1] * cz * s];
  return { at, yaw: yawOf(front), onFace: onFace * s };
}

/** An end piece whose rounded return closes the face at vertex w: d = the unit direction along the face from w into the run, n its outward normal, scale s.
 *  Its 6 m front runs from w along d, the return wraps round w onto the face beyond. The natural side (no mirror) is the edge's tail (KIT_X -1): d = +t. */
export function endPlace(w: V, d: V, n: V, a: KitAsset, s: number): { at: V; yaw: number; front: number } {
  const [cx, cz] = a.pivotOffset ?? [0, 0], fw = 6 * s, fc: V = [w[0] + d[0] * fw / 2, w[1] + d[1] * fw / 2];
  // the footprint centre: cx toward the return (-d), cz behind the front (-n is negative cz)
  return { at: [fc[0] - d[0] * cx * s + n[0] * cz * s, fc[1] - d[1] * cx * s + n[1] * cz * s], yaw: yawOf(n), front: fw };
}
/** The side of an edge an unmirrored end closes. */
export const END_SIDE: 'tail' | 'head' = KIT_X < 0 ? 'tail' : 'head';

/** Vela's stacked streamline pieces for walls taller than the whole-wall pieces (brief 075-E), in the plan frame (Base 5.65, Mid 3.6, Top 3.95 as
 *  SideBayStack): placed only once adopted (kitrefs.adopted), the bay stack as a drop-in for SideBayStack on public faces, the corner and end stacks with the
 *  frames above (their cells read from the catalogue). Vela's briefs/075 pieces.md gives pivots and widths; until then they are not placed. */
export const STREAM_STACK = {
  bay: { base: 'StreamBayStack_Base', mid: 'StreamBayStack_Mid', top: 'StreamBayStack_Top' },
  corner: { base: 'StreamCornerStack_Base', mid: 'StreamCornerStack_Mid', top: 'StreamCornerStack_Top' },
  end: { base: 'StreamEndStack_Base', mid: 'StreamEndStack_Mid', top: 'StreamEndStack_Top' },
};
/** The street-junction pieces (brief 075-E): CornerTower_12 where it is adopted and the building stands at least `towerMin` storeys outside the play streets,
 *  DecoCorner_10 where the wall's height fits code 10. */
// brief 075-F1b: CornerTower_12's blade sign reads HOTEL in neon (facades_tower.py), so it stands only on a building with a street-level hotel tenancy (trade `hotel`);
// elsewhere the junction takes DecoCorner_10 or the class's StreamCorner. BB-5 checks it on the export.
export const JUNCTION = { tower: 'CornerTower_12', deco: 'DecoCorner_10', decoEnd: 'DecoEnd_10', towerMin: 9, hotel: 'hotel-lobby' };
