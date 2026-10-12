// Does a kit piece fit a slot, and at what scale? Pure; the generator and the tests share it. Kit axes: x across (along the element's yaw),
// y up, z depth (front). The slot decides: the piece is scaled within its tags.scale range (and squeezed no further than tags.squash), never the slot.
import type { KitAsset } from '../../contracts/world/kit.schema';

/** h: wanted height (within hTol, a fraction); maxH: hard cap; along/depth: room across and in depth; foot + maxH: fit a disc (landmarks, uniform scale). */
export type Slot = { h?: number; hTol?: number; maxH?: number; along?: number; depth?: number; foot?: number };
export type Scale = [number, number, number];
const clamp = (x: number, lo: number, hi: number) => Math.min(hi, Math.max(lo, x));
const r3 = (n: number) => Math.round(n * 1000) / 1000;

export function fit(a: Pick<KitAsset, 'size' | 'tags'>, s: Slot): Scale | null {
  const [sx0, sy0, sz0] = a.size, [lo, hi] = a.tags.scale, E = 1e-6;
  if (s.foot !== undefined) { // landmark: uniform, inside the disc and under the cap
    const u = Math.min(s.foot / Math.max(sx0, sz0), s.maxH === undefined ? Infinity : s.maxH / sy0, hi);
    return u < lo ? null : [r3(u), r3(u), r3(u)];
  }
  let sy = s.h === undefined ? clamp(1, lo, hi) : clamp(s.h / sy0, lo, hi);
  if (s.h !== undefined && Math.abs(sy * sy0 - s.h) > (s.hTol ?? 0.15) * s.h + E) return null;
  if (s.maxH !== undefined && sy * sy0 > s.maxH + E) { sy = s.maxH / sy0; if (sy < lo - E) return null; }
  if (a.tags.squash === 1 && s.h !== undefined) { // uniform piece (trees, palms): a crown wider than its room shrinks the whole piece, down to the height tolerance
    const room = Math.min(s.along === undefined ? Infinity : s.along / sx0, s.depth === undefined ? Infinity : s.depth / sz0), low = Math.max(lo, (s.h * (1 - (s.hTol ?? 0.15))) / sy0);
    if (room < sy && room >= low - E) sy = room;
  }
  const across = (size: number, room?: number) => {
    if (room === undefined || sy * size <= room + E) return sy;
    const sq = Math.max(room / size, sy * a.tags.squash);
    return sq * size <= room + E ? sq : -1;
  };
  const sx = across(sx0, s.along), sz = across(sz0, s.depth);
  return sx < 0 || sz < 0 ? null : [r3(sx), r3(sy), r3(sz)];
}

/** The scaled size of a kit piece. */
export const scaled = (a: Pick<KitAsset, 'size'>, k: Scale): [number, number, number] => [a.size[0] * k[0], a.size[1] * k[1], a.size[2] * k[2]];
