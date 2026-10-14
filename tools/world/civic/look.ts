// What the civic block takes from Vela's brief 008 output, as committed in contracts/world/civic-look.json (suborned.civic-look/1):
// the 27 pieces, the swaps and scales the block states, the paving bands, the palette the dE gate reads. Generation reads this file, never out/.
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { V } from '../../plan/geom';

export type Band = { id: string; band: string; piece: string; at: V; yawDeg: number; scaleX?: number; role: string };
export type Look = {
  schema: 'suborned.civic-look/1'; sources: Record<string, string>;
  /** The pieces brief 008 B1 added (all adopted in kit.json). */
  pieces: string[];
  /** A piece the block stands in for another one: the older piece's id → the one drawn. */
  swaps: Record<string, string>;
  /** Pieces that stand on their own posts (the older pavilion hangs at the top of its mass). */
  grounded: string[];
  /** Uniform scale a piece takes in this block, by id (the piers under the v2 shelter: 1.2 → 2.226 m). */
  scales: Record<string, number>;
  /** The export id of the mass that carries a piece from `dressing.pieces`. */
  place: Record<string, string>;
  /** A piece that is a building's ground storey: the building above starts at `top` m. */
  ground: Record<string, { building: string; top: number }>;
  /** Pieces whose body is overhead (a parasol's canopy, 2.07 m clear): over a table or a Handover point is allowed, only the pole stands on the ground. */
  overhead: string[];
  /** Pieces the play camera's architecture fade may thin (they stand between the camera and a person). */
  fade: string[];
  budget: { lod0Tris: number };
  slab: { material: string; tile_m: number; surfaces: string[]; note: string };
  bands: { frame: string; pieces: Band[] };
  palette: { signals: Record<string, string>; minDE: number; materials: { material: string; unlit: string; used_by?: string[] }[] };
  roles: Record<string, unknown>;
};

export const LOOK_FILE = resolve(import.meta.dir, '../../../contracts/world/civic-look.json');
export const look: Look = JSON.parse(readFileSync(LOOK_FILE, 'utf8'));
