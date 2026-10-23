// LOD switch points per category (Vela, brief 007 round 3; source: out/review/lod-thresholds.html, generator out/vela/r3/tools/lod.ts).
// Values = Unity LODGroup screen-relative heights at lodBias 1: L0 -> L1 below l1, L1 -> L2 below l2. No cull level for any category.
// Each is sized so the category's smallest member still shows L0 at play zoom 14 (orthographic, 28 m of screen height).
import type { KitLodCategory } from '../../contracts/world/kit.schema';

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
