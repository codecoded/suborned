// The civic block's free-standing planting beds (brief 008 colour round, Vela's recipe in out/decokit-008/civic-beds.json, constants in civic-look.json `beds`).
// A bed is a civic mass of surface `park` with no kit piece (the seven beside the entrances and along the facades); the recipe fits any ring:
//   kerb   the ring prism as it was, its surface renamed so Unity draws it in DK_Limestone;
//   hedge  the ring moved `inset` m inward, a prism `rise` m tall on the kerb top, in DK_Leaf_CivicDark;
//   tufts  BedTuft_A/B/C on the ring moved `inset` m in (row 1 at 0.50 m, every 0.55 m; row 2 at 1.05 m, every 0.62 m, only where the ring is >= 0.9 m away),
//          variant (station + bed index) mod 3 over both rows, yaw from the generator's rng seeded by the mass id.
// Only masses and kit masses the builder already draws; nothing here reads out/.
import type { Mass } from '../../../contracts/world/world.schema';
import { type V, dist, segDist } from '../../plan/geom';
import { r3, r4, rng } from '../gen/common';
import { adoptedBy } from '../gen/kitrefs';
import { footOf } from './foot';
import { look } from './look';

const area2 = (r: V[]): number => r.reduce((s, p, i) => { const q = r[(i + 1) % r.length]!; return s + p[0] * q[1] - q[0] * p[1]; }, 0);
const ccw = (r: V[]): V[] => (area2(r) > 0 ? r : r.slice().reverse());

/** The ring moved `d` to its left (inward on a counter-clockwise ring), the tangent at each point taken from its two neighbours. */
export function offsetRing(ring: V[], d: number): V[] {
  const n = ring.length;
  return ring.map((p, i) => {
    const a = ring[(i + n - 1) % n]!, b = ring[(i + 1) % n]!, tx = b[0] - a[0], ty = b[1] - a[1], l = Math.hypot(tx, ty) || 1;
    return [p[0] - (ty / l) * d, p[1] + (tx / l) * d] as V;
  });
}

/** Stations along the closed path from its first vertex: k = max(4, round(length / step)) of them, evenly spaced by arc length. */
export function stations(path: V[], step: number): V[] {
  const P = [...path, path[0]!], segs = P.slice(1).map((b, i) => dist(P[i]!, b)), L = segs.reduce((s, x) => s + x, 0), k = Math.max(4, Math.round(L / step)), out: V[] = [];
  for (let i = 0; i < k; i++) {
    const d = (L * i) / k;
    let acc = 0;
    for (let j = 0; j < segs.length; j++) {
      const seg = segs[j]!;
      if (acc + seg >= d) { const f = (d - acc) / Math.max(seg, 1e-9), a = P[j]!, b = P[j + 1]!; out.push([a[0] + (b[0] - a[0]) * f, a[1] + (b[1] - a[1]) * f]); break; }
      acc += seg;
    }
  }
  return out;
}

const toRing = (p: V, ring: V[]): number => Math.min(...ring.map((a, i) => segDist(p, a, ring[(i + 1) % ring.length]!)));
/** A mass is a free-standing bed: a `park` prism the block gave no kit piece. */
export const isBed = (m: Mass): boolean => m.surface === 'park' && !m.kit;

/** `masses` with every bed dressed: its prism becomes the kerb (same id and ring), and a hedge and its tufts are added after the list. Without beds the list is returned as it is. */
export function dressBeds(masses: Mass[]): Mass[] {
  const beds = masses.filter(isBed).sort((a, b) => (a.id < b.id ? -1 : a.id > b.id ? 1 : 0));
  if (!beds.length) return masses;
  const B = look.beds, added: Mass[] = [], kerbed = masses.map(m => (isBed(m) ? { ...m, surface: B.kerb.surface } : m));
  beds.forEach((m, bi) => {
    const wise = ccw(m.ring), hedgeTop = r3(m.base + m.height + B.hedge.rise);
    const hedge = offsetRing(wise, B.hedge.inset).map((p): V => [r3(p[0]), r3(p[1])]);
    added.push({ id: `${m.id}/hedge`, surface: B.hedge.surface, ring: wise === m.ring ? hedge : hedge.slice().reverse(), base: r3(m.base + m.height), height: B.hedge.rise });
    const st = B.tufts.rows.flatMap(row => stations(offsetRing(wise, row.inset), row.step).filter(q => row.minFromRing === undefined || toRing(q, wise) >= row.minFromRing));
    const r = rng(m.id), base = r3(hedgeTop - B.tufts.drop);
    st.forEach((q, i) => {
      const id = B.tufts.pieces[(i + bi) % B.tufts.pieces.length]!, a = adoptedBy(id); if (!a) return; // a piece the kit has not adopted is placed nowhere; rule CD-2 names it
      const at: V = [r3(q[0]), r3(q[1])], kit = { id, scale: [1, 1, 1] as [number, number, number], yaw: r4(r.f() * Math.PI * 2), at, base };
      added.push({ id: `${m.id}/tuft-${i}`, surface: 'trim', ring: footOf(a, kit).map((p): V => [r3(p[0]), r3(p[1])]), base, height: a.size[1], source: 'kit', kit });
    });
  });
  return [...kerbed, ...added];
}
