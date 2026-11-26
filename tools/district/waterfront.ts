import type { World } from '../../contracts/world/world.schema';
import type { Point } from './places';

// Split the plan's orthogonal quay outlines at their corners and at the old harbour's grade
// changes. Navigation and the rendered deck share these strips, including both pier aprons.
export function waterfront(world: World, block: (p: Point) => Point) {
  const surfaces: { quay: string; a: Point; b: Point; westCm: number; eastCm: number }[] = [];
  const height = (e: number) => Math.round(18 - 103 * Math.min(1, Math.max(0, (e - 74) / 6)));
  for (const q of world.ground.quays.filter((q) => q.surface === 'quay')) {
    const xs = Array.from(new Set(q.ring.map((p) => p[0]))).sort((a, b) => a - b);
    const cuts = [...new Set([...xs, ...[74, 80].filter((e) => e > xs[0]! && e < xs.at(-1)!)])].sort(
      (a, b) => a - b,
    );
    for (let i = 1; i < cuts.length; i++) {
      const west = cuts[i - 1]!,
        east = cuts[i]!,
        mid = (west + east) / 2;
      const ys = q.ring
        .flatMap((p, j) => {
          const next = q.ring[(j + 1) % q.ring.length]!;
          return p[0] > mid !== next[0] > mid ? [p[1] + ((mid - p[0]) * (next[1] - p[1])) / (next[0] - p[0])] : [];
        })
        .sort((a, b) => a - b);
      for (let j = 0; j < ys.length; j += 2)
        surfaces.push({
          quay: q.id,
          a: block([west, ys[j]!]),
          b: block([east, ys[j + 1]!]),
          westCm: height(west),
          eastCm: height(east),
        });
    }
  }
  return surfaces;
}

// These rectangles are the exact roof-garden programmes emitted by the plan. The
// Unity build replaces their old tubs, cylindrical trunks and spherical crowns together.
export function roofTerraces(world: World, block: (p: Point) => Point) {
  return world.roofs.filter(r => r.form === 'terrace' && r.id.startsWith('mega/')).map(r => {
    const [cx, cy] = r.c, [width, depth] = r.size;
    return {
      a: block([cx - width / 2, cy - depth / 2]),
      b: block([cx + width / 2, cy + depth / 2]),
      heightCm: Math.round(r.z * 100),
    };
  });
}
