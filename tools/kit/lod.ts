// LOD switch points per category (Vela, brief 007 round 3; source: out/review/lod-thresholds.html, generator out/vela/r3/tools/lod.ts).
// Values = Unity LODGroup screen-relative heights at lodBias 1: L0 -> L1 below l1, L1 -> L2 below l2. No cull level for any category.
// Each is sized so the category's smallest member still shows L0 at play zoom 14 (orthographic, 28 m of screen height).
import type { KitLodCategory, KitLodClass, KitLodClassSpec } from '../../contracts/world/kit.schema';

export const LOD_CATEGORIES: Record<KitLodCategory, { l1: number; l2: number }> = {
  street: { l1: 0.008, l2: 0.003 }, // street props: smallest = DomeCamera at scale 0.6, 0.0094 at zoom 14
  trees: { l1: 0.08, l2: 0.025 }, // a 3 m palm is 0.107 at zoom 14
  facades: { l1: 0.2, l2: 0.07 }, // the smallest bay (~8 m) is 0.29 at zoom 14
  towers: { l1: 0.3, l2: 0.1 }, // every tower is several screens tall at zoom 14
  signatures: { l1: 0.15, l2: 0.05 }, // icons of the wide shots keep L0 longer than ordinary towers
  horizon: { l1: 0.2, l2: 0.06 }, // never in the play camera; rings B and C fall to L1/L2 in perspective
};

const BY_CATEGORY: Record<string, KitLodCategory> = {
  Street: 'street', Booths: 'street', Signals: 'street', Signage: 'street', Furnish: 'street', Plaza: 'street',
  Alleys: 'street', Structures: 'street', Vehicles: 'street', Robots: 'street', Ground: 'street', Calibration: 'street', // not in Vela's table: small street-scale pieces
  Plants: 'trees', Facades: 'facades', Rooftop: 'facades', // Rooftop: roof-scale pieces standing on facades (not in Vela's table)
  Highrise: 'towers', Crowns: 'towers', Landmarks: 'signatures', Skyport: 'signatures', Sky: 'signatures', Horizon: 'horizon',
};

/** A piece's LOD category: Vela's per-category defaults, with the Skyline bays (facades) and towers (towers), the Plaza hall bays (facades) and DiscTower (signatures) by id. */
export function lodCategoryOf(id: string, category: string): KitLodCategory {
  if (/^Tower_/.test(id)) return 'towers';
  if (/^(Stream|Deco|Porthole|Balcony|Arcade)(Bay|End|Corner)_|^Hall(Bay|CornerWing|EntranceTower)$|^CornerTower_/.test(id)) return 'facades';
  if (/^(SideBay_[34]|SideBayStack_(Base|Mid|Top)|CornerPier_(Out|In)_(3|4|Base|Mid|Top)|AlleyWall_[34]D?|SidePilaster_(3|4|Base|Mid|Top)|ShopFill_[34]|ShopStack_Base)$/.test(id)) return 'facades'; // brief 072-C: the every-face modules (6 m bays like the street bays)
  if (id === 'RecordsTower') return 'signatures'; // brief 072: the Records Office landmark
  if (/^DiscTower|^SkyBridge|^Skylon|^SkyRailStation/.test(id)) return 'signatures';
  return BY_CATEGORY[category] ?? 'street';
}

/** Per-id switch points that replace the category's (Vela, brief 028 "Palm LODs", Sky 2026-10-22 "yes"): the palms switch L0 to L1 at 60 m and L1 to L2 at 140 m of the third-person camera (far clip 350 m).
 *  screen = size / (2 x tan 27.5 deg x distance), size = the largest side of the group's bounds (CoconutPalm 7.94 m, CoconutPalm_Street 10.65 m). The `trees` values (0.08 / 0.025) would put them at 108 m and 346 m.
 *  The commander view never reaches them (an orthographic palm is never below 0.215 at zoom 18.5). */
export const LOD_BY_ID: Readonly<Record<string, { l1: number; l2: number }>> = {
  CoconutPalm: { l1: 0.1271, l2: 0.0545 },
  CoconutPalm_Street: { l1: 0.1705, l2: 0.0731 },
};

/** The LOD records with the switch points (LOD1 at l1, LOD2 at l2): the id's own when it has them, else the category's. */
export const lodScreens = <T extends { screen: number }>(lods: T[] | undefined, cat: KitLodCategory, id?: string): T[] | undefined => {
  const s = (id && LOD_BY_ID[id]) || LOD_CATEGORIES[cat];
  return lods?.map((l, i) => ({ ...l, screen: i === 0 ? s.l1 : s.l2 }));
};

/** Pieces that ship at full detail at every distance, whatever the generator exported (Sky, brief 028: no model compression without his sign-off). A listed id gets no `lods` and no LOD files in the catalogue, so the built prefab
 *  carries no LODGroup and Unity draws L0 at every distance. Empty since 2026-10-22: CoconutPalm and CoconutPalm_Street were taken off after Sky approved Vela's palm levels. To hold a piece back: add its id here, then `bun run kit`. */
export const FULL_DETAIL: ReadonlySet<string> = new Set<string>();

/** Brief 072-D (Redline, briefs/072/redline/plan.md §3.1): the every-face pieces switch by distance per class, not by the category's screen heights (`facades` 0.2 / 0.07
 *  would put a 6 m storey module at L1 at play zoom 18.5). `distances` [L1, L2, cull] m [S]; Latch's KitLods turns each into a screen height from the piece's own
 *  unscaled size (s / (1.041 D)) and fails the build if any L1 distance is under 36 m (L0 at every play zoom). */
export const LOD_CLASSES: Record<KitLodClass, KitLodClassSpec> = {
  bay3: { distances: [45, 120, 330] }, bay4: { distances: [45, 120, 330] }, storey: { distances: [45, 120, 330] }, cornerStorey: { distances: [45, 120, 330] },
  column: { distances: [45, 120, 330] }, // a baked stack column (Latch, plan §3.2)
  landmark: { distances: [150, 400, 0] }, // RecordsTower: no cull (the 350 m haze clip)
  // brief 072 F2 (Redline, briefs/072/redline/phase2.md): the roof crowns' `towers` 0.3 put Crown_Dish at L1 from 26.6 m (zoom 13.85), Finial 32.0, Starburst 33.9, Atom 34.7.
  // L1 at 45 m keeps L0 at every play zoom (+6.4 k tris district-wide). The second distance is 400 m, past the 350 m haze clip, on purpose: six crowns have one LOD and
  // KitLods reads `distances[lods.Count]` as the cull, so for them 400 m is the cull (none inside the clip); Atom (two LODs) holds L1 to the clip. Nothing culls or pops.
  crown: { distances: [45, 400, 0] },
};
/** The distance class of an every-face piece (undefined for every other piece: its category's screens hold). */
export function lodClassOf(id: string): KitLodClass | undefined {
  if (id === 'RecordsTower') return 'landmark';
  if (/^Crown_/.test(id)) return 'crown'; // brief 072 F2: every roof crown (Crowns category)
  if (/^(SideBay|AlleyWall|SidePilaster|ShopFill)_3D?$|^CornerPier_(Out|In)_3$/.test(id)) return 'bay3';
  if (/^(SideBay|AlleyWall|SidePilaster|ShopFill)_4D?$|^CornerPier_(Out|In)_4$/.test(id)) return 'bay4';
  if (/^SideBayStack_(Base|Mid|Top)$|^SidePilaster_(Base|Mid|Top)$|^ShopStack_Base$/.test(id)) return 'storey';
  if (/^CornerPier_(Out|In)_(Base|Mid|Top)$/.test(id)) return 'cornerStorey';
  if (/^StreamBayStack_(Base|Mid|Top)$/.test(id)) return 'storey'; // brief 075-E (Vela): the streamline stack modules switch by distance like the every-face stacks
  if (/^Stream(Corner|End)Stack_(Base|Mid|Top)$/.test(id)) return 'cornerStorey';
  return undefined;
}
