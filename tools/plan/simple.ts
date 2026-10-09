// Export assertion: every polygon in the world is simple. A polygon that crosses or touches itself, repeats a point or
// folds back on itself (a zero-width spur) is rejected, so Unity never has to repair or re-derive geometry.
import { type V, area, cross, dist, dot, segDist, segHit, sub } from './geom';

const isPt = (a: unknown): a is V => Array.isArray(a) && a.length === 2 && typeof a[0] === 'number' && typeof a[1] === 'number';
const isPoly = (a: unknown): a is V[] => Array.isArray(a) && a.length > 0 && a.every(isPt);
/** Keys that hold open polylines, not polygons. */
const OPEN = new Set(['centreline', 'tracks']);

/** Why the polygon is not simple, or null. */
export function polyIssue(poly: V[]): string | null {
  const n = poly.length;
  if (n < 3) return `only ${n} points`;
  for (let i = 0; i < n; i++) {
    const a = poly[i], b = poly[(i + 1) % n], c = poly[(i + 2) % n];
    if (dist(a, b) < 1e-6) return `repeated point at [${a}] (index ${i})`;
    const u = sub(b, a), v = sub(c, b);
    if (Math.abs(cross(u, v)) < 1e-9 * dist(a, b) * dist(b, c) && dot(u, v) < 0) return `zero-width spur at [${b}] (index ${(i + 1) % n})`;
  }
  for (let i = 0; i < n; i++) for (let j = i + 2; j < n; j++) {
    if (i === 0 && j === n - 1) continue; // neighbours across the closing edge
    const a = poly[i], b = poly[(i + 1) % n], c = poly[j], d = poly[(j + 1) % n];
    const x = segHit(a, b, c, d);
    if (x) return `edges ${i} and ${j} cross at [${x.map(v => v.toFixed(2))}]`;
    for (const [p, s, e, k] of [[a, c, d, j], [b, c, d, j], [c, a, b, i], [d, a, b, i]] as [V, V, V, number][])
      if (segDist(p, s, e) < 1e-6) return `edges ${i} and ${j} touch at [${p}]`;
  }
  return area(poly) < 1e-6 ? 'zero area' : null;
}

/** Every polygon-shaped array in the world (anything that is a list of [x, y] points, except open polylines), with its path. */
export function* polygons(x: unknown, path = ''): Generator<[string, V[]]> {
  if (isPoly(x)) { yield [path, x]; return; }
  if (Array.isArray(x)) { for (let i = 0; i < x.length; i++) yield* polygons(x[i], `${path}[${(x[i] as { id?: string })?.id ?? i}]`); return; }
  if (x && typeof x === 'object') for (const [k, v] of Object.entries(x)) if (!OPEN.has(k)) yield* polygons(v, path ? `${path}.${k}` : k);
}

/** Every offender in the world, as "path: reason". */
export function nonSimple(world: unknown): string[] {
  const out: string[] = [];
  for (const [path, poly] of polygons(world)) { const why = polyIssue(poly); if (why) out.push(`${path}: ${why}`); }
  return out;
}
