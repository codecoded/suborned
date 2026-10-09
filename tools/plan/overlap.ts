// Export assertion: raised surfaces at the same level must not overlap (z-fight seam). Two surfaces at one height that
// share area render the same pixels at the same depth. Exact intersection area: ear-clip A into triangles, clip B to each
// (Sutherland-Hodgman against a convex triangle is exact for any simple B), sum the shoelace areas.
import { type V, area, cross, sub } from './geom';
import type { World } from '../../contracts/world/world.schema';

/** Largest tolerated shared area between two same-level surfaces, m². */
export const OVERLAP_MAX = 0.01;
export type Surface = { level: number; id: string; poly: V[] };

const signed = (p: V[]) => p.reduce((s, a, i) => s + cross(a, p[(i + 1) % p.length]), 0) / 2;
const ccw = (p: V[]): V[] => (signed(p) < 0 ? [...p].reverse() : p);

function triangulate(poly: V[]): V[][] {
  const idx = ccw(poly).map((_, i) => i), p = ccw(poly), out: V[][] = [];
  let guard = idx.length * idx.length;
  while (idx.length > 3 && guard-- > 0) {
    let cut = false;
    for (let k = 0; k < idx.length && !cut; k++) {
      const [a, b, c] = [-1, 0, 1].map(o => p[idx[(k + o + idx.length) % idx.length]]);
      if (cross(sub(b, a), sub(c, b)) <= 1e-12) continue;
      const inside = idx.some(j => { const q = p[j]; return q !== a && q !== b && q !== c && cross(sub(b, a), sub(q, a)) > 0 && cross(sub(c, b), sub(q, b)) > 0 && cross(sub(a, c), sub(q, c)) > 0; });
      if (inside) continue;
      out.push([a, b, c]); idx.splice(k, 1); cut = true;
    }
    if (!cut) idx.splice(0, 1); // degenerate remainder: drop a vertex rather than loop
  }
  if (idx.length === 3) out.push(idx.map(j => p[j]));
  return out;
}

function clip(subject: V[], tri: V[]): V[] {
  let out = subject;
  for (let i = 0; i < 3 && out.length; i++) {
    const a = tri[i], b = tri[(i + 1) % 3], side = (q: V) => cross(sub(b, a), sub(q, a)), inp = out;
    out = [];
    inp.forEach((p, j) => {
      const q = inp[(j + 1) % inp.length], sp = side(p), sq = side(q);
      if (sp >= 0) out.push(p);
      if ((sp >= 0) !== (sq >= 0)) out.push([p[0] + ((q[0] - p[0]) * sp) / (sp - sq), p[1] + ((q[1] - p[1]) * sp) / (sp - sq)]);
    });
  }
  return out;
}

const box = (p: V[]) => [Math.min(...p.map(v => v[0])), Math.max(...p.map(v => v[0])), Math.min(...p.map(v => v[1])), Math.max(...p.map(v => v[1]))];
/** Area shared by two simple polygons, m². */
export function overlapArea(a: V[], b: V[]): number {
  const [A, B] = [box(a), box(b)];
  if (A[1] <= B[0] || B[1] <= A[0] || A[3] <= B[2] || B[3] <= A[2]) return 0;
  const cb = ccw(b);
  return triangulate(a).reduce((s, t) => { const c = clip(cb, t); return s + (c.length > 2 ? area(c) : 0); }, 0);
}

/** The raised surfaces of the world, with their level: foot malls and walks, the circus pavement sectors and the Handover block pavement (all at pavement level). */
export function raised(w: World): Surface[] {
  const L = w.heights.pavement;
  return [
    ...w.foot.map(f => ({ level: L, id: `foot[${f.id}]`, poly: f.polygon as V[] })),
    ...w.circus.pavement.map((p, i) => ({ level: L, id: `circus.pavement[${i}]`, poly: p as V[] })),
    ...w.handover.blocks.map(b => ({ level: L, id: `handover.blocks[${b.id}].outer`, poly: b.outer as V[] })),
  ];
}

/** Every same-level pair sharing more than OVERLAP_MAX m², as "a overlaps b by X m² at level L". */
export function overlaps(w: World, surfaces = raised(w)): string[] {
  const out: string[] = [];
  for (let i = 0; i < surfaces.length; i++) for (let j = i + 1; j < surfaces.length; j++) {
    const [a, b] = [surfaces[i], surfaces[j]];
    if (a.level !== b.level) continue;
    const m = overlapArea(a.poly, b.poly);
    if (m > OVERLAP_MAX) out.push(`${a.id} overlaps ${b.id} by ${m.toFixed(2)} m² at level ${a.level}`);
  }
  return out;
}
