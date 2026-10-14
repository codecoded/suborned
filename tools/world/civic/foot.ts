// A kit piece's footprint in the plan frame, shared by the civic dressing and the planting beds.
import type { KitAsset } from '../../../contracts/world/kit.schema';
import type { V } from '../../plan/geom';

/** A kit piece's footprint at its scale (turned by its yaw, about its footprint centre): kit x → plan along yaw, kit z → across (front = yaw + 90°); the footprint centre
 *  sits at the pivot + pivotOffset. */
export function footOf(a: KitAsset, k: { scale: number[]; yaw?: number; at: V }): V[] {
  const y = k.yaw ?? 0, cs = Math.cos(y), sn = Math.sin(y), off = a.pivotOffset ?? [0, 0], sx = k.scale[0]!, sz = k.scale[2] ?? sx, hw = (a.size[0] * sx) / 2, hd = (a.size[2] * sz) / 2;
  return ([[-hw, -hd], [hw, -hd], [hw, hd], [-hw, hd]] as V[]).map(([x, z]) => [k.at[0] + (x + off[0] * sx) * cs - (z + off[1] * sz) * sn, k.at[1] + (x + off[0] * sx) * sn + (z + off[1] * sz) * cs] as V);
}
