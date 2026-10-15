// What the engine does with a cover point, in integers (brief 011, Pulse; briefs/011/pulse-fight-ai.md §5): the one test of "does this point shield a body from that
// shooter", the hit modifier it gives, and the deterministic choice of the point a hostile runs to. The C# engine implements exactly these; the vectors in
// tools/cover/cover.test.ts (and in the spec) are what both must give. Pure, no randomness, no floating point: positions are cm, faces and quality are per mille.
import type { CoverPoint } from '../../contracts/world/cover.schema';

export type Pt = readonly [number, number];
/** The constants of the cover rules (rulebook values `cover-*`, proposed in briefs/011/pulse-fight-ai.values.json). */
export const COVER = {
  /** a body is "beside" a cover point within this many cm of it (the engine's grid cell is 50 cm; a body is 65 cm clear of a solid) */
  snapCm: 70,
  /** inside this range a shot cannot be turned by cover: Motive's point blank, 5 m, 1000 per mille */
  minRangeCm: 500,
  /** quality at or above: full cover, hit modifier mod-cover (500 per mille, Motive); from qPartial: partial cover (mod-cover-partial, 750 proposed) */
  qFull: 600, qPartial: 350,
  modFull: 500, modPartial: 750,
  /** a hostile looks for cover this far from where it stands (cm), by faction (see the spec's parameter table) */
  seekCm: 800,
  /** it never takes cover that sends it back by more than this from its objective, nor (in a fight) forward by more than stepCloserCm toward the foe */
  retreatCm: 200, stepCloserCm: 300,
} as const;

export const isqrt = (n: number): number => { if (n < 2) return n; let x = Math.floor(Math.sqrt(n)); while (x * x > n) x--; while ((x + 1) * (x + 1) <= n) x++; return x; };
export const d2 = (a: Pt, b: Pt) => (a[0] - b[0]) ** 2 + (a[1] - b[1]) ** 2;
export const dist = (a: Pt, b: Pt) => isqrt(d2(a, b));

/** The cover point shields a body standing at it from a shooter at `s`: far enough (point blank is never turned), inside the cone about `faces`, and present at `tick`. */
export function protects(c: Pick<CoverPoint, 'at' | 'faces' | 'cos' | 'present'>, s: Pt, missionS?: number): boolean {
  if (c.present && missionS !== undefined && (missionS < c.present[0] || missionS >= c.present[1])) return false;
  const vx = s[0] - c.at[0], vy = s[1] - c.at[1], d = isqrt(vx * vx + vy * vy);
  if (d < COVER.minRangeCm) return false;
  return vx * c.faces[0] + vy * c.faces[1] >= c.cos * d;
}

/** A person at `p`, standing still, is shielded from `shooter` by some point they are beside: the best modifier in per mille (1000 = none). */
export function coverModifier(points: readonly CoverPoint[], p: Pt, shooter: Pt, missionS?: number): number {
  let best = 1000;
  for (const c of points) {
    if (d2(p, c.at) > COVER.snapCm ** 2 || !protects(c, shooter, missionS)) continue;
    const m = c.q >= COVER.qFull ? COVER.modFull : c.q >= COVER.qPartial ? COVER.modPartial : 1000;
    if (m < best) best = m;
  }
  return best;
}

export type Seeker = { at: Pt; objective: Pt; foe?: Pt };
/** The cover point a hostile runs to, or undefined (it then advances to its stop point without cover). Candidates: not taken, present, quality at least partial, within
 *  `seekCm` of the body. With a foe (it is being shot at, or has one in reach): shielding against the foe (which also keeps it out of point blank) and not more than
 *  `stepCloserCm` nearer the foe than the body is; it may fall back to do it. Without one (it is advancing): never more than `retreatCm` farther from the objective than the body
 *  is. The choice minimises distance − quality (cm against per mille), then the lower id: deterministic. */
export function selectCover(points: readonly CoverPoint[], me: Seeker, taken: ReadonlySet<string>, o: { seekCm?: number; missionS?: number } = {}): CoverPoint | undefined {
  const seek = o.seekCm ?? COVER.seekCm, here = dist(me.at, me.objective);
  let best: CoverPoint | undefined, bestKey = Infinity;
  for (const c of points) {
    if (taken.has(c.id) || c.q < COVER.qPartial) continue;
    if (c.present && o.missionS !== undefined && (o.missionS < c.present[0] || o.missionS >= c.present[1])) continue;
    const d = dist(me.at, c.at);
    if (d > seek) continue;
    if (!me.foe && dist(c.at, me.objective) > here + COVER.retreatCm) continue;
    if (me.foe) {
      if (!protects(c, me.foe, o.missionS) || dist(c.at, me.foe) < dist(me.at, me.foe) - COVER.stepCloserCm) continue;
    }
    const key = d - c.q;
    if (key < bestKey || (key === bestKey && best && c.id < best.id)) { best = c; bestKey = key; }
  }
  return best;
}
