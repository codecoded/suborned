// The polyline nearest a position, and the direction it runs there (what Unity turns a pylon or a crossing to).
import type { V } from '../../plan/geom';

/** The nearest of `lines` to `p`: its index, the distance to it and the direction (radians) of its segment there. */
export function nearestLine(lines: V[][], p: V): { index: number; yaw: number; dist: number } {
  let out = { index: -1, yaw: 0, dist: Infinity };
  lines.forEach((line, index) => {
    for (let i = 0; i + 1 < line.length; i++) {
      const a = line[i]!, b = line[i + 1]!, dx = b[0] - a[0], dy = b[1] - a[1], len2 = dx * dx + dy * dy || 1;
      const t = Math.max(0, Math.min(1, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dy) / len2));
      const dist = Math.hypot(p[0] - a[0] - t * dx, p[1] - a[1] - t * dy);
      if (dist < out.dist) out = { index, yaw: Math.atan2(dy, dx), dist };
    }
  });
  return out;
}
