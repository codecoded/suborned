// Where a call box or phone booth stands in a real city (brief 023 siting follow-up; Sky, 2026-10-19: "call boxes should be placed where a real city would place them").
// Pure geometry on the plan's roads; tools/world/gen/street/quiet.ts takes its candidate positions and its preference from here, tools/world/rules/quiet.ts holds the result (R-REPORT/site).
// The practice, from the sources in briefs/023/research/call-box-siting.md (no published numeric standard was found: these are starting values, [S], with the practice each one stands for):
//   corner     fire alarm and police boxes stood on street corners, on the pavement, visible from the junction (Wikipedia: Fire alarm call box; britishtelephones.com Post P.A. No. 1: "on corners and where
//              a person had a good view"; Edinburgh and Glasgow police boxes at junctions). A box stands on the corner's pavement, along the frontage of one of the two streets, not on the corner point.
//   set back   a box does not stand on the kerb return or in the driver's sight triangle (Hertfordshire side-road sheet: visibility splay kept clear of street furniture; SF-8 here keeps sight-blocking
//              items 6 m off the corner's kerb tangent): `setback` m from the corner along the kerb.
//   not far    one box serves its corner: it stands within `zone` m of the corner it is at, in sight of the junction. A frontage farther from every corner than that is no corner's: a booth there is a
//              shop-door or station booth (a design choice for Sky), not a corner box.
//   never      in a side-road's mouth or in any carriageway (the kerb line of one street runs across the next street's mouth: nothing stands there), 2 m off a crossing or a doorway (SF-2, kept by
//              Layer.fits), 0.45 m off the kerb face (SF-5), out of the clear walking zone (SF-1).
//   rank       a box at a corner of a bigger street comes before one on a smaller one (main streets carry the boxes; side streets are reached from them): primary, district, access.
import { type V, dist } from '../../plan/geom';

/** The numbers (m). `setback` = SF-8's SIGHT_CLEAR (a glazed box is allowed at 0 by SF-8 itself; a call box is not a booth beside a shop, it stands off the corner); `zone` = the corner's own frontage. */
export const SITING = {
  setback: 6,
  zone: 25,
  /** a kerb candidate stands this far (kerb face to the nearest footprint sample) off every other road's carriageway: the side road's mouth is never a pavement */
  mouthKerb: 0.45,
  rank: { primary: 0, district: 1, access: 2 } as Record<string, number>,
};

type Road = { id: string; class: string; w: number; line: V[]; gyratory?: boolean; path: string[] };

/** Chainage of the point of a polyline nearest to p, p's signed offset (left of the direction of travel is positive) from it, and that nearest point (`foot`). */
export function project(line: V[], p: V): { s: number; off: number; foot: V } {
  let acc = 0, best: { s: number; off: number; d: number; foot: V } = { s: 0, off: Infinity, d: Infinity, foot: line[0]! };
  for (let i = 1; i < line.length; i++) {
    const a = line[i - 1]!, b = line[i]!, l = dist(a, b); if (l < 1e-9) continue;
    const u: V = [(b[0] - a[0]) / l, (b[1] - a[1]) / l], t = Math.max(0, Math.min(l, (p[0] - a[0]) * u[0] + (p[1] - a[1]) * u[1])), f: V = [a[0] + u[0] * t, a[1] + u[1] * t], d = dist(p, f);
    if (d < best.d) best = { s: acc + t, off: (p[0] - f[0]) * -u[1] + (p[1] - f[1]) * u[0], d, foot: f };
    acc += l;
  }
  return { s: best.s, off: best.off, foot: best.foot };
}

export type Corner = { at: V; node: string; roads: [string, string] };

/**
 * The junction corners of the street: at each plan node, the roads that meet there leave it as arms (one per direction a road's line extends from the node); two arms that are neighbours in
 * angle make a corner, the point where their kerb lines cross on the pavement between them. A road that runs through the node is two arms, so a T has two corners on the side road's side
 * and none on the far side: a corner exists only where a street leaves. Gyratory roads (the circus ring) make none.
 */
export function junctionCorners(roads: Road[], nodes: Record<string, V>): Corner[] {
  const out: Corner[] = [];
  for (const [node, n] of Object.entries(nodes)) {
    const arms: { r: Road; u: V; ang: number }[] = [];
    for (const r of roads) {
      if (r.gyratory) continue;
      const k = r.line.findIndex(p => dist(p, n) < 0.5); if (k < 0) continue;
      const pick = (dir: 1 | -1) => { for (let i = k + dir; i >= 0 && i < r.line.length; i += dir) { const d = dist(r.line[i]!, n); if (d > 4) { const u: V = [(r.line[i]![0] - n[0]) / d, (r.line[i]![1] - n[1]) / d]; arms.push({ r, u, ang: Math.atan2(u[1], u[0]) }); return; } } };
      pick(1); pick(-1);
    }
    if (arms.length < 2) continue;
    arms.sort((p, q) => p.ang - q.ang);
    for (let i = 0; i < arms.length; i++) {
      const A = arms[i]!, B = arms[(i + 1) % arms.length]!;
      if (A.r === B.r) continue;
      let wedge = B.ang - A.ang; if (wedge <= 0) wedge += 2 * Math.PI;
      if (wedge >= Math.PI - 0.1) continue; // a reflex wedge (the outside of a bend, or the open side of a T) has no corner
      // A's kerb on its B-ward side is the line hA off A's axis toward B; the corner is where it meets B's kerb on its A-ward side (hB off B's axis toward A)
      const side = (u: V, toward: V): V => { const l: V = [-u[1], u[0]]; return l[0] * toward[0] + l[1] * toward[1] >= 0 ? l : [-l[0], -l[1]]; };
      const nA = side(A.u, B.u), nB = side(B.u, A.u), hA = A.r.w / 2, hB = B.r.w / 2, den = A.u[0] * nB[0] + A.u[1] * nB[1];
      if (Math.abs(den) < 0.25) continue; // near-parallel arms (a bend's two sides): no corner worth a box
      const x = (hB - hA * (nA[0] * nB[0] + nA[1] * nB[1])) / den;
      out.push({ at: [n[0] + A.u[0] * x + hA * nA[0], n[1] + A.u[1] * x + hA * nA[1]], node, roads: [A.r.id, B.r.id] });
    }
  }
  return out;
}

/** The corner a kerb station `p` on road `r` stands at, and how far (m, along the road) it is from it: the nearest corner of a junction on this road that lies on p's side of the road and on this road's own kerb line. */
export function cornerOf(r: Road, p: V, corners: Corner[]): { corner: Corner; along: number } | undefined {
  const me = project(r.line, p), len = r.line.reduce((n, q, i) => (i ? n + dist(r.line[i - 1]!, q) : 0), 0);
  let best: { corner: Corner; along: number } | undefined;
  for (const c of corners) {
    if (!c.roads.includes(r.id) || dist(c.at, p) > SITING.zone + 20) continue;
    const q = project(r.line, c.at);
    if (Math.abs(Math.abs(q.off) - r.w / 2) > 0.5 || Math.sign(q.off) !== Math.sign(me.off) || q.s < -1 || q.s > len + 1) continue;
    const along = Math.abs(q.s - me.s);
    if (!best || along < best.along) best = { corner: c, along };
  }
  return best;
}

/** True if a piece at `at` turned to kit `yaw` opens away from the road whose centreline is `line`: its +Z front faces yaw + 90° (kitrefs.reachYaw), and the footway lies on the far side of the kerb from the centreline. */
export function opensOnFootway(line: V[], at: V, yaw: number): boolean {
  const f = project(line, at).foot;
  return -Math.sin(yaw) * (at[0] - f[0]) + Math.cos(yaw) * (at[1] - f[1]) > 0;
}
