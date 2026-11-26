// A street gathering has two public ends. The same positions feed the people's routines, the
// district navigation bake and the furniture placed by Unity. All coordinates here are plan metres.
import type { World } from '../../contracts/world/world.schema';

export type Point = [number, number];
export type Fixture = { kit: string; at: Point; yaw: number; radius: number };
type Style = 'platform' | 'coffee' | 'papers' | 'waterfront' | 'garden' | 'market' | 'walk';
export type Venue = {
  id: string;
  name: string;
  at: Point;
  along: Point;
  style: Style;
  count: number;
  span: number;
  width: number;
  fixtures: Fixture[];
};
type Strip = { id: string; line: readonly (readonly number[])[]; width: number };
export function project(v: Pick<Venue, 'at' | 'along'>, along: number, across: number): Point {
  const [x, y] = v.along;
  return [+(v.at[0] + x * along - y * across).toFixed(2), +(v.at[1] + y * along + x * across).toFixed(2)];
}
function onStrip(strip: Strip, fraction: number): { at: Point; along: Point } {
  const p = strip.line;
  const legs = p.slice(1).map((b, i) => Math.hypot(b[0]! - p[i]![0]!, b[1]! - p[i]![1]!));
  let metres = legs.reduce((a, b) => a + b, 0) * fraction;
  for (let i = 0; i < legs.length; i++) {
    const length = legs[i]!,
      a = p[i]!,
      b = p[i + 1]!;
    if ((metres <= length || i === legs.length - 1) && length > 0) {
      const along: Point = [(b[0]! - a[0]!) / length, (b[1]! - a[1]!) / length];
      return { along, at: [a[0]! + along[0] * metres, a[1]! + along[1] * metres] };
    }
    metres -= length;
  }
  throw Error('Empty street ' + strip.id);
}
export function streetPlaces(world: World, open: (point: Point) => boolean): Venue[] {
  const venues: Venue[] = [];
  const strips = [...world.ground.roads, ...world.ground.foot];
  function add(
    id: string,
    name: string,
    p: { at: Point; along: Point },
    style: Style,
    count: number,
    span: number,
    width: number,
  ) {
    if (!open(p.at)) {
      let found: Point | undefined;
      for (let r = 1; r <= 8 && !found; r++)
        for (const [a, b] of [
          [0, 1],
          [0, -1],
          [1, 0],
          [-1, 0],
        ]) {
          const q = project(p, r * a!, r * b!);
          if (open(q)) {
            found = q;
            break;
          }
        }
      if (!found) return;
      p = { ...p, at: found };
    }
    venues.push({ id, name, ...p, style, count, span, width, fixtures: [] });
  }
  for (const stop of world.transit.stops) {
    const xs = stop.ring.map((p) => p[0]),
      ys = stop.ring.map((p) => p[1]);
    const x0 = Math.min(...xs),
      x1 = Math.max(...xs),
      y0 = Math.min(...ys),
      y1 = Math.max(...ys);
    const [, , key, side] = stop.id.split('/');
    const label = key![0]!.toUpperCase() + key!.slice(1);
    add(
      'platform-' + key + '-' + side,
      label + ' tram stop',
      { at: [(x0 + x1) / 2, (y0 + y1) / 2], along: x1 - x0 > y1 - y0 ? [1, 0] : [0, 1] },
      'platform',
      8,
      11,
      0.65,
    );
  }
  function street(
    id: string,
    feature: string,
    at: number,
    side: number,
    name: string,
    style: Style,
    count: number,
    span = 14,
  ) {
    const s = strips.find((s) => s.id === feature);
    if (!s) throw Error('No district feature ' + feature);
    const p = onStrip(s, at);
    const across = side * (feature.startsWith('road/') ? s.width / 2 + 1.55 : s.width * 0.22);
    p.at = project(p, 0, across);
    // Positive across always faces away from the traffic at a roadside venue.
    if (side < 0) p.along = [-p.along[0], -p.along[1]];
    add(id, name, p, style, count, span, feature.startsWith('road/') ? 1.2 : 2.3);
  }
  street('sunset-coffee', 'road/sunset-way', 691 / 860, -1, 'Sunset Way coffee stand', 'coffee', 17);
  street('lantern-market', 'road/lantern-lane', 0.5, 1, 'Lantern Lane night market', 'market', 20, 20);
  street('lantern-papers', 'road/lantern-lane', 0.24, -1, 'Lantern newsstand', 'papers', 11);
  street('west-row', 'road/west-row', 0.54, -1, 'West Row arcade', 'papers', 14);
  street('west-row-coffee', 'foot/west-row-lane-2', 0.56, 1, 'West Row lunch counter', 'coffee', 12);
  street('lamplight', 'road/lamplight-lane', 0.43, 1, 'Lamplight reading room', 'papers', 14);
  street('terraces', 'road/lamplight-lane', 0.2, -1, 'West Terraces café', 'coffee', 14);
  street('rocket-gate', 'road/market-street', 0.45, -1, 'Rocket Gate exchange', 'papers', 15);
  street('southside', 'road/apollo-way', 0.46, 1, 'Apollo lunch counter', 'coffee', 14);
  street('orbit', 'road/boulevard', 0.42, -1, 'Orbit Square market', 'market', 19);
  street('market', 'road/harbour-market', 0.53, 1, 'Harbour Market stalls', 'market', 15);
  street('quay', 'foot/harbour-promenade', 0.59, 1, 'Quayside coffee', 'coffee', 15);
  street('pier', 'foot/market-pier', 0.5, 1, 'Market Pier landing', 'waterfront', 19, 21);
  street('skyport', 'foot/skyport-pier', 0.6, -1, 'Skyport landing', 'waterfront', 11);
  street('gardens', 'foot/gardens-gate', 0.11, 1, 'Orbit Gardens pavilion', 'garden', 14);
  street('crown-walk', 'foot/crown-walk', 0.5, 1, 'Crown Walk arcade', 'papers', 14, 16);
  street('crown-west', 'foot/crown-walk-west', 0.55, -1, 'Sphere Park gate', 'garden', 9, 7);
  street('crown-avenue', 'road/crown-avenue', 0.71, 1, 'Crown Avenue kiosk', 'papers', 12);
  add('circus', 'Crown Circus', { at: [-13, -24], along: [0.88, -0.475] }, 'garden', 18, 10, 1.4);
  add('quay-terrace', 'Harbour Market terrace', { at: [69, -148], along: [0, 1] }, 'market', 16, 17, 2.4);
  add('sphere', 'Sphere Park gardens', { at: [-190, 92], along: [0.8, 0.6] }, 'garden', 10, 12, 2.2);
  // Secondary streets carry short neighbourhood rounds between the main gathering places.
  for (const s of world.ground.roads) {
    const length = s.line
      .slice(1)
      .reduce((n, b, i) => n + Math.hypot(b[0] - s.line[i]![0], b[1] - s.line[i]![1]), 0);
    const sites = Math.max(1, Math.floor(length / 90));
    for (let i = 0; i < sites; i++) {
      const fraction = (i + 0.5) / sites,
        p = onStrip(s, fraction);
      if (p.at[0] > -226 && p.at[0] < -84 && p.at[1] > -372 && p.at[1] < -148) continue;
      if (venues.some((v) => Math.hypot(p.at[0] - v.at[0], p.at[1] - v.at[1]) < 32)) continue;
      const name = s.id.slice(5).replaceAll('-', ' ');
      street(
        s.id.slice(5) + '-round-' + i,
        s.id,
        fraction,
        i % 2 === 0 ? 1 : -1,
        name[0]!.toUpperCase() + name.slice(1),
        'walk',
        5,
        21,
      );
    }
  }
  return venues;
}

const dress: Record<Style, readonly [string, number, number, number, number][]> = {
  platform: [],
  walk: [],
  coffee: [
    ['CoffeeCart_Play', -4, 2.2, 90, 1.2],
    ['CafeSet_Tulip', 4, 3, 0, 1.15],
    ['VendingMachine_Cola', 8, 2.1, 0, 0.65],
  ],
  papers: [
    ['Newsstand', -4, 2.8, 0, 1.45],
    ['Bench_Play', 4, 2.1, 0, 0.9],
    ['StreetClock', 9, 1.9, 0, 0.5],
  ],
  market: [
    ['CurvedKiosk_Play', -7, 3.2, 0, 1.7],
    ['CoffeeCart_Play', 6, 2.4, 90, 1.2],
    ['Parasol_Stripe', 10, 3.4, 0, 0.45],
  ],
  waterfront: [
    ['CoinTelescope', -8, 1.5, 180, 0.5],
    ['Bench_Play', 3, 1.4, 0, 0.9],
    ['LifebuoyStand', 9, 1.7, 0, 0.5],
  ],
  garden: [
    ['RoundBench', -6, 3, 0, 1.45],
    ['DrinkingFountain', 4, 2.2, 0, 0.6],
  ],
};
export function furnish(venue: Venue, fits: (point: Point, radius: number) => boolean) {
  const yaw = (Math.atan2(venue.along[1], -venue.along[0]) * 180) / Math.PI;
  for (const [kit, along, across, turn, radius] of dress[venue.style]) {
    for (const shift of [0, 2, -2, 4, -4]) {
      const at = project(venue, along + shift, across);
      if (
        !fits(at, radius) ||
        venue.fixtures.some((f) => Math.hypot(f.at[0] - at[0], f.at[1] - at[1]) < f.radius + radius + 1)
      )
        continue;
      venue.fixtures.push({ kit, at, yaw: Math.round(yaw + turn), radius });
      break;
    }
  }
}
