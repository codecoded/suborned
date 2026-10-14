// The café sets on the civic block's tables: where the world draws each (its pivot and yaw), so the people tools can seat people on the chairs it draws
// (tools/people/seats.ts) without importing the world generator. build.ts places every `cafe-table-N` of the block from these two functions; the tests in
// tools/people/test/seats.test.ts hold them to the export's own civic masses (civicMasses) and to the chairs measured on the built prefab.
import type { V } from '../../plan/geom';
import { r3, rv } from '../gen/common';

/** A table of the block that carries a café set (grammar §11: `cafe-table-0..7`, kit CafeSet_Tulip, drawn as CafeSet_Tulip_Play). */
export const isCafeTable = (id: string) => /^cafe-table-\d+$/.test(id);

/** The string hash that seeds the yaw of a person-scale civic piece (a café set, a palm): 31-based, from 7. */
export const hashY = (s: string) => [...s].reduce((h, ch) => (Math.imul(h, 31) + ch.charCodeAt(0)) >>> 0, 7);
/** The export's id of a civic object of the block (`civic/cafe-table-0`): the id the world seeds its yaw by. */
export const massId = (objectId: string) => `civic/${objectId}`;
/** Plan yaw (radians, from +x toward +y) of a café piece, seeded by its export id (massId) so the same block always turns it the same way. */
export const seededYaw = (exportId: string) => r3((hashY(exportId) % 628) / 100);

/** The pivot of a café set: the centre of the box round its table's polygon (plan frame, the ring rounded to the export's mm), as the world places it (`kit.at`). */
export function setPivot(poly: V[]): V {
  const ring = poly.map(rv), xs = ring.map(p => p[0]), ys = ring.map(p => p[1]);
  return rv([(Math.max(...xs) + Math.min(...xs)) / 2, (Math.max(...ys) + Math.min(...ys)) / 2]);
}
