// The space-age massing grammar (brief 007 B), seeded by element id. From the concept sheets (AtomicAge, Blocks): rounded streamline
// corners, horizontal speed bands, stepped setbacks, a crown on every tower. Window and porthole treatment is the Unity facade grammar's
// (Vela's spec in contracts/world/); this file only shapes masses and roof crowns. Starting values [S], tuned by playtest.
import { type V, cross, dist, dot, norm, sub } from '../../plan/geom';
import type { Ring } from './geo';
import { kindOf } from './edges';

export const GRAMMAR = {
  /** Grammar §1: street corners take buildings.cornerArc (5.3 m); 1 in 3 are 9.6 m wrap corners (seeded per vertex). */
  radius: 5.3, wrapRadius: 9.6, wrapShare: 1 / 3,
  arcSegments: 5,
  /** Corner buildings: both faces of a right-angle street corner must be this long to keep the corner in one building; cuts stay this far from it. */
  cornerEdge: 14, cornerKeep: 9,
  /** A corner is rounded only if both its edges keep this much straight length beyond the radius once neighbouring arcs have taken theirs. */
  arcClear: 0.5,
  /** Buildings of fewer storeys keep their corners and have no podium split. */
  roundFrom: 3,
  /** Stepped setbacks: from this many storeys the mass steps in one half-bay at about this share of its height. */
  /** §5: a block of >= 7 storeys on a street corner steps back one bay (6) at storey n - 2; blocks of 4-6 step 3 at the top storey (1 in 2). */
  stepCorner: 7, stepLow: [4, 6] as readonly number[], stepLowShare: 0.5, stepLowBy: 3,
  /** §2: speed bands, one slab per pair (two 0.25 m lines 0.25 m apart = 0.75 m), 0.35 m proud, at every 2nd floor line from storey 2. */
  band: { out: 0.35, thick: 0.75, pitch: 2 },
  /** A building of this many storeys or more is a tower and carries a crown (plan tower sites always do). */
  crownFrom: 9, crownNear: 150,
};

/** The crown kinds of the concepts: finial (Power Spire), atom (Exposition Palace), starburst (Monorail Terminal), saucer (Starlight Hotel), antenna and dish (machine crowns). */
export const CROWNS = ['finial', 'atom', 'starburst', 'saucer', 'antenna', 'dish'] as const;
export type Crown = (typeof CROWNS)[number];
/** The roof programme a crown stands as (every roof has one of the plan's programmes; SF-15 checks it fits). */
export const CROWN_PROGRAMME: Record<Crown, string> = { finial: 'antenna-mast', atom: 'antenna-mast', starburst: 'antenna-mast', antenna: 'antenna-mast', saucer: 'observation-saucer', dish: 'solar-dish' };

export const isStreetClass = (c: string) => !['party', 'court', 'none'].includes(kindOf(c));

/** Replace convex ~90° corners whose two edges are street faces by arcs of radius r. Classes: the arc takes the class of the edge it leaves. */
export function roundRing(ring: Ring, r: (i: number) => number): Ring {
  const n = ring.p.length, p: V[] = [], c: string[] = [], room = ring.p.map((_, i) => dist(ring.p[i]!, ring.p[(i + 1) % n]!)); // room: length of each edge no arc has taken yet
  for (let i = 0; i < n; i++) {
    const a = ring.p[(i + n - 1) % n]!, v = ring.p[i]!, b = ring.p[(i + 1) % n]!, cin = ring.c[(i + n - 1) % n]!, cout = ring.c[i]!;
    const u1 = norm(sub(v, a)), u2 = norm(sub(b, v));
    const rr = r(i), ok = cross(u1, u2) > 0 && Math.abs(dot(u1, u2)) < 0.26 && isStreetClass(cin) && isStreetClass(cout) && room[(i + n - 1) % n]! >= rr + GRAMMAR.arcClear && room[i]! >= rr + GRAMMAR.arcClear;
    if (!ok) { p.push(v); c.push(cout); continue; }
    room[(i + n - 1) % n]! -= rr; room[i]! -= rr;
    const t1: V = [v[0] - u1[0] * rr, v[1] - u1[1] * rr], ctr: V = [t1[0] - u1[1] * rr, t1[1] + u1[0] * rr];
    const a0 = Math.atan2(t1[1] - ctr[1], t1[0] - ctr[0]), sweep = Math.atan2(cross(u1, u2), dot(u1, u2));
    for (let k = 0; k <= GRAMMAR.arcSegments; k++) {
      const q = a0 + (sweep * k) / GRAMMAR.arcSegments;
      p.push([ctr[0] + Math.cos(q) * rr, ctr[1] + Math.sin(q) * rr]);
      c.push(k === GRAMMAR.arcSegments ? cout : cin);
    }
  }
  return { p, c };
}
