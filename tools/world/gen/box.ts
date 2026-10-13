// Boxes: what a prim, a mass or a kit sign occupies (a plan ring and the heights it stands between), and how far two of
// them overlap. Shared by the sign placement (kitrefs.ts: a sign that would pass through something is not placed) and
// by the world rules that check the result (tools/world/rules), the way street-zones.ts is shared by the street layer
// and the SF rules.
import type { KitAsset } from '../../../contracts/world/kit.schema';
import type { Frontage, Mass, Prim } from '../../../contracts/world/world.schema';
import type { Scale } from '../../kit/fit';
import type { V } from '../../plan/geom';
import { boxRing } from '../../plan/street-zones';

/** A footprint ring with the heights it stands between. */
export type Box = { ring: V[]; z0: number; z1: number };

/** An export prim as a box: `c`, `size` (along yaw, across, high), `base`. A cylinder or vault is its bounding box. */
export const primBox = (p: Prim): Box => ({
  ring: boxRing(p.c as V, p.size[0], p.size[1], p.yaw), z0: p.base, z1: p.base + p.size[2],
});
/** A mass (extruded ring) as a box. */
export const massBox = (m: Mass): Box => ({ ring: m.ring, z0: m.base, z1: m.base + m.height });

/** Depth (m) two convex rings overlap: the smallest overlap along any edge normal; 0 when apart or only touching. */
export function penetration(a: V[], b: V[]): number {
  let least = Infinity;
  for (const ring of [a, b]) {
    for (let i = 0; i < ring.length; i++) {
      const p = ring[i]!, q = ring[(i + 1) % ring.length]!, len = Math.hypot(q[0] - p[0], q[1] - p[1]) || 1;
      const nx = -(q[1] - p[1]) / len, ny = (q[0] - p[0]) / len;
      const span = (r: V[]) => {
        const s = r.map(v => v[0] * nx + v[1] * ny);
        return [Math.min(...s), Math.max(...s)] as const;
      };
      const [a0, a1] = span(a), [b0, b1] = span(b), overlap = Math.min(a1, b1) - Math.max(a0, b0);
      if (overlap <= 0) return 0;
      least = Math.min(least, overlap);
    }
  }
  return least;
}

/** Metres two height spans overlap (negative: the gap between them). */
export const overlapZ = (a: Box, b: Box): number => Math.min(a.z1, b.z1) - Math.max(a.z0, b.z0);

/** A disc as a 32-sided ring (convex), for the rules that meet a landmark or a monument. */
export const discRing = (c: V, r: number): V[] => Array.from({ length: 32 }, (_, i) => {
  const a = (i * Math.PI) / 16;
  return [c[0] + r * Math.cos(a), c[1] + r * Math.sin(a)] as V;
});

/**
 * The box of a kit sign: the piece's footprint (size x × z, scaled) turned by its yaw about its pivot, standing from
 * `base` for its scaled height. Kit +X runs along `yaw`, kit +Z (the front) across it: `pivotOffset` is the footprint
 * centre from the pivot, in the piece's own frame.
 */
export function signBox(a: KitAsset, k: { scale: Scale; yaw?: number; at: V; base: number }): Box {
  const [sx, sy, sz] = k.scale, [ox, oz] = a.pivotOffset ?? [0, 0], yaw = k.yaw ?? 0;
  const cs = Math.cos(yaw), sn = Math.sin(yaw);
  const c: V = [k.at[0] + ox * sx * cs - oz * sz * sn, k.at[1] + ox * sx * sn + oz * sz * cs];
  const hw = (a.size[0] * sx) / 2, hd = (a.size[2] * sz) / 2;
  const corners: V[] = [[-hw, -hd], [hw, -hd], [hw, hd], [-hw, hd]];
  return {
    ring: corners.map(([x, y]) => [c[0] + x * cs - y * sn, c[1] + x * sn + y * cs] as V),
    z0: k.base, z1: k.base + a.size[1] * sy,
  };
}

/** Metres two boxes may overlap before they count as passing through each other (the export rounds to 1 mm). */
export const OVERLAP_TOL = 0.011;

/** Something a sign must clear: a box, the element it belongs to and what it is, for the message. */
export type Solid = Box & { id: string; what: string };

/** Solids found by the ground cells their bounding box covers, so a long mass is found from anywhere along it. */
export class Obstacles {
  private cells = new Map<string, Solid[]>();
  constructor(solids: Solid[], private cell = 16) {
    for (const s of solids) {
      for (const k of this.keys(s.ring)) (this.cells.get(k) ?? this.cells.set(k, []).get(k)!).push(s);
    }
  }
  private keys(ring: V[]): string[] {
    const xs = ring.map(p => p[0]), ys = ring.map(p => p[1]), c = this.cell, out: string[] = [];
    for (let i = Math.floor(Math.min(...xs) / c); i <= Math.floor(Math.max(...xs) / c); i++) {
      for (let j = Math.floor(Math.min(...ys) / c); j <= Math.floor(Math.max(...ys) / c); j++) out.push(`${i},${j}`);
    }
    return out;
  }
  /** Every solid `box` passes through: overlapping by more than OVERLAP_TOL both in height and on the ground. */
  hits(box: Box): Solid[] {
    const seen = new Set<Solid>(), out: Solid[] = [];
    for (const k of this.keys(box.ring)) {
      for (const s of this.cells.get(k) ?? []) {
        if (seen.has(s)) continue;
        seen.add(s);
        if (overlapZ(box, s) > OVERLAP_TOL && penetration(box.ring, s.ring) > OVERLAP_TOL) out.push(s);
      }
    }
    return out;
  }
}

/** The parts of a frontage a sign must clear: canopy, sign board, shopfront, door and the service bay if any. */
export function frontageSolids(f: Frontage): Solid[] {
  const parts: [string, Prim | undefined][] = [
    ['canopy', f.canopy], ['sign board', f.sign], ['shopfront', f.front], ['door', f.entrance], ['bay', f.service],
  ];
  return parts.flatMap(([what, p]) => (p ? [{ id: f.id, what: `the ${what} of ${f.id}`, ...primBox(p) }] : []));
}

type Megas = { prims: Prim[]; parts: Mass[] }[];
/** Everything a sign must clear: every frontage's own parts, the megastructures' parts and prims, and the buildings. */
export function signObstacles(fronts: Frontage[], megas: Megas, buildings: Mass[]): Obstacles {
  const named = (id: string, box: Box): Solid => ({ id, what: id, ...box });
  return new Obstacles([
    ...fronts.flatMap(frontageSolids),
    ...megas.flatMap(m => [...m.prims.map(p => named(p.id, primBox(p))), ...m.parts.map(p => named(p.id, massBox(p)))]),
    ...buildings.map(b => named(b.id, massBox(b))),
  ]);
}
