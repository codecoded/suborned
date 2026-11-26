// Street-height studies in the art store: Residential Morning, Café Row, Wave Mosaic and
// the oval planted bed. Use the plan's street direction, plot use and surveyed free ground.
import type { World } from '../../contracts/world/world.schema';
import type { Fixture, Point } from './places';

type Court = { name: string; style: string; ring: Point[] };
export type StreetBuilding = {
  at: Point;
  yaw: number;
  width: number;
  depth: number;
  floors: number;
  palette: number;
  ring: Point[];
};
const corridors: Record<string, 'homes' | 'market' | 'gardens' | 'quay'> = {
  'sunset-way': 'gardens',
  'west-row': 'homes',
  'lamplight-lane': 'homes',
  'lantern-lane': 'market',
  'apollo-way': 'homes',
  'crown-avenue': 'gardens',
  'harbour-market': 'quay',
  'market-street': 'market',
  'crown-road': 'gardens',
  'starline-street': 'homes',
  'satellite-street': 'homes',
  'apron-road': 'quay',
  'pylon-row': 'market',
  'orbit-walk': 'gardens',
  'crown-walk': 'gardens',
  'harbour-promenade': 'quay',
};

function inside(p: Point, ring: readonly (readonly number[])[]) {
  let hit = false;
  for (let i = 0, j = ring.length - 1; i < ring.length; j = i++) {
    const a = ring[i]!,
      b = ring[j]!;
    if (a[1]! > p[1] !== b[1]! > p[1] && p[0] < a[0]! + ((b[0]! - a[0]!) * (p[1] - a[1]!)) / (b[1]! - a[1]!))
      hit = !hit;
  }
  return hit;
}

export function straightRuns<T extends readonly number[]>(line: readonly T[]): T[] {
  const points: T[] = [];
  for (const p of line) {
    const a = points.at(-2),
      b = points.at(-1);
    if (a && b) {
      const ux = b[0]! - a[0]!,
        uy = b[1]! - a[1]!,
        vx = p[0]! - b[0]!,
        vy = p[1]! - b[1]!;
      if (Math.abs(ux * vy - uy * vx) < 0.00001 && ux * vx + uy * vy >= 0) points.pop();
    }
    points.push(p);
  }
  return points;
}

export function streetscape(world: World, clear: (p: Point, radius: number) => boolean, furnished: Fixture[]) {
  const courts: Court[] = [],
    buildings: StreetBuilding[] = [],
    fixtures: Fixture[] = [];
  const used = furnished.map((f) => ({ at: f.at, radius: f.radius }));
  const buildable = world.ground.plots.filter((p) => ['residential', 'mixed', 'commercial'].includes(p.surface));
  function fits(p: Point, radius: number) {
    return (
      clear(p, radius) && !used.some((f) => Math.hypot(f.at[0] - p[0], f.at[1] - p[1]) < f.radius + radius + 0.65)
    );
  }
  for (const road of [...world.ground.roads, ...world.ground.foot]) {
    const id = road.id.split('/')[1]!,
      style = corridors[id];
    if (!style) continue;
    const foot = road.id.startsWith('foot/');
    const line = straightRuns(road.line);
    let index = 0;
    for (let leg = 1; leg < line.length; leg++) {
      const a = line[leg - 1]!,
        b = line[leg]!,
        dx = b[0] - a[0],
        dy = b[1] - a[1],
        length = Math.hypot(dx, dy);
      if (length < 22) continue;
      const ux = dx / length,
        uy = dy / length;
      const count = Math.max(1, Math.floor(length / (style === 'quay' ? 30 : 36)));
      for (let k = 0; k < count; k++, index++)
        for (const side of [-1, 1]) {
          const t = (k + 0.5) / count,
            centre: Point = [a[0] + dx * t, a[1] + dy * t];
          const right = road.width * 0.5 + (foot ? 0.4 : 0.25);
          const place = (along: number, across: number): Point => [
            +(centre[0] + ux * along - uy * side * (right + across)).toFixed(2),
            +(centre[1] + uy * along + ux * side * (right + across)).toFixed(2),
          ];
          const face = Math.round((Math.atan2(uy * side, -ux * side) * 180) / Math.PI);
          const label = id.replaceAll('-', ' ') + ' ' + (index + 1) + (side < 0 ? ' east' : ' west');
          function item(kit: string, along: number, across: number, radius: number, rotate = 0) {
            const at = place(along, across);
            if (!fits(at, radius)) return false;
            fixtures.push({ kit, at, yaw: face + rotate, radius });
            used.push({ at, radius });
            return true;
          }
          // Keep an open walking strip along the shop side of the kerb. The planted pockets
          // sit farther back. Broken frontages and intersecting roads keep their original ground.
          const depth = style === 'gardens' ? 7.4 : 5.4;
          const half = Math.min(15, length / count / 2 - 3);
          const shape = [
            [-half, -0.05],
            [half, -0.05],
            [half, depth - 1.4],
            [half - 1.4, depth],
            [-half + 1.4, depth],
            [-half, depth - 1.4],
          ];
          const ring = shape.map(([s, n]) => place(s!, n!));
          if (
            ![...ring, place(0, depth / 2), place(0, depth), place(-half / 2, depth), place(half / 2, depth)].every(
              (p) => clear(p, 0),
            )
          )
            continue;
          courts.push({ name: label, style, ring });
          const variant = (index + (side < 0 ? 1 : 0)) % 4;
          if (style === 'gardens' || variant !== 1)
            item(
              style === 'homes' && variant === 2 ? 'Cypress' : 'CoconutPalm_Street',
              -half + 2.5,
              depth - 1.15,
              0.55,
              index * 49,
            );
          item('GardenBed', -half + 6.6, depth - 1.25, 1.3, 90);
          if (variant !== 2) item('GlobeLamp_TB', half - 1.5, 1.05, 0.32);
          if (variant === 0 || style === 'quay') item('Bench_Play', 1.8, depth - 1.05, 1);
          if (style === 'quay' || style === 'gardens') item('GardenBed', half - 5, depth - 1.3, 1.3, 90);
          if (variant === 2 && style === 'market') item('BistroSet', 2.5, 3.5, 1.25, 23);

          // Give only surveyed gaps on a housing or retail plot a new street wall. The setback
          // leaves entrances, pavements and the whole road accessible at pedestrian height.
          const width = variant % 2 === 0 ? 16 : 13,
            deep = variant % 2 === 0 ? 9 : 8;
          const near = depth + 2,
            far = near + deep,
            mid = (near + far) / 2;
          const corners = [
            [-width / 2, near],
            [width / 2, near],
            [width / 2, far],
            [-width / 2, far],
          ].map(([s, n]) => place(s!, n!));
          const home = place(0, mid);
          const samples: Point[] = [];
          for (let along = -width / 2; along <= width / 2; along += 2)
            for (let across = near; across <= far; across += 2) samples.push(place(along, across));
          if (
            !foot &&
            style !== 'quay' &&
            buildable.some((plot) => corners.every((p) => inside(p, plot.ring))) &&
            [...samples, ...corners].every((p) => fits(p, 0.65))
          ) {
            buildings.push({
              at: home,
              yaw: face,
              width,
              depth: deep,
              floors: style === 'market' ? 2 : 2 + (variant % 3),
              palette: variant,
              ring: corners,
            });
            used.push({ at: home, radius: Math.hypot(width, deep) / 2 });
          } else if (
            id === 'sunset-way' &&
            variant === 0 &&
            world.ground.plots.some((plot) => plot.surface === 'park' && inside(home, plot.ring))
          ) {
            item('DecoPavilion', 0, depth + 6, 5.1);
          }
        }
    }
  }
  return { courts, buildings, fixtures };
}
