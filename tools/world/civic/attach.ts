// Brief 077 (Splice): pieces placed with a drawn civic piece, in its frame (civic-look.json `attach`): the companions' testbed planting at their stonework's transform
// (CafeTerrace_TB, RecordsTower_TB, CafeUpper_3_TB / _4_TB) and one GlobeLamp_TB in each PlanterArc_TB's frame (Vela's recorded offset; Vantage's sign-off implied by Sky's yes
// on the Unity capture, briefs/077/unity, 2026-10-30). Read from the look, never from out/. An attached piece the kit has not adopted is placed nowhere. Deterministic.
import type { Mass } from '../../../contracts/world/world.schema';
import type { V } from '../../plan/geom';
import { r3, r4, rv } from '../gen/common';
import { adoptedBy } from '../gen/kitrefs';
import { footOf } from './foot';
import { look } from './look';

/** `civic` with each attached piece added after its parent: pivot at the parent's kit (x, z) offset `at` (scaled, turned by the parent's yaw), the same yaw and scale, its lowest
 *  point `dy` (scaled) above the parent's lowest point (kit.base: KitPlacer stands a civic piece's lowest point there). The parent's fade goes with it. */
export function attachPieces(civic: Mass[], attach = look.attach): Mass[] {
  if (!attach) return civic;
  const out: Mass[] = [];
  for (const m of civic) {
    out.push(m);
    if (!m.kit || m.hidden) continue;
    for (const x of attach[m.kit.id] ?? []) {
      const a = adoptedBy(x.piece); if (!a) continue;
      const [sx, sy, sz] = m.kit.scale, y = m.kit.yaw ?? 0, cs = Math.cos(y), sn = Math.sin(y), ux = x.at[0] * sx, uz = x.at[1] * (sz ?? sx);
      const at: V = rv([m.kit.at[0] + ux * cs - uz * sn, m.kit.at[1] + ux * sn + uz * cs]), base = r3(m.kit.base + x.dy * (sy ?? sx));
      const kit = { id: a.id, scale: [...m.kit.scale] as [number, number, number], yaw: r4(y), at, base };
      out.push({ id: `${m.id}/${x.as}`, surface: 'trim', ring: footOf(a, kit).map(rv), base, height: r3(a.size[1] * (sy ?? sx)), source: 'kit', kit, ...(m.fade ? { fade: true as const } : {}) });
    }
  }
  return out;
}

/** Brief 077: an attached piece that is planting (`as: planting`: the companions' testbed planting) takes no ground: a placement rule that keeps clear of civic masses
 *  (the café's waiter robot, capstone-civic.ts) does not keep clear of it, as it never kept clear of the dome planting inside the piece. */
export const takesNoGround = (m: Mass, attach = look.attach): boolean => !!m.kit && Object.values(attach ?? {}).flat().some(x => x.as === 'planting' && x.piece === m.kit!.id);
