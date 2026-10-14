// Dressed connectors (brief 007 round 2): station stair towers and lift shafts were drawn as bare boxes. The grammar's streamline vocabulary
// (contracts/world/harbour-district.grammar.md §1 rounded ends, §2 speed-band pairs at every 2nd floor line from the deck line, §3 glazing set in
// stone and chrome) dresses them as prims Unity already draws: a stair tower is a glazed stadium (rounded ends) on a limestone plinth with a limestone
// spine behind the glass, chrome-and-accent band pairs and a stepped chrome cap; a lift is a glazed chrome-hooped tube (the kit's lift_tube) with
// mullions, a parked car and a chrome cap with an atom-ball crown. The connector's ring stays the footprint every rule reads; the dress stands
// inside it (bands and caps at most BAND_OUT proud). Accent: coral transit (surface 'beam') on station banks, turquoise ('tower') elsewhere.
import type { Connector, Prim } from '../../../contracts/world/world.schema';
import type { V } from '../../plan/geom';
import { r3, r4 } from './common';
import { HOOP, PLATE } from './surfaces';

export const DRESS = {
  plinth: 0.45, // buildings.plinth
  bandOut: 0.15, bandH: 0.25, bandGap: 0.25, bandFrom: 9.43, bandPitch: 7.2, // §2: a pair (chrome line + accent) at every 2nd floor line from the deck line
  glassInset: 0.08, capH: 1.2, capOut: 0.2, // stepped chrome cap: a proud collar and an inset coping
  hoopEvery: 5, hoopH: 0.4, mullion: 0.1, // the kit's lift_tube: hoops every 5 m, four chrome mullions
} as const;

const prim = (id: string, shape: Prim['shape'], surface: string, c: V, size: [number, number, number], base: number, yaw = 0): Prim =>
  ({ id, shape, surface, c: [r3(c[0]), r3(c[1])], base: r3(base), size: [r3(size[0]), r3(size[1]), r3(size[2])], yaw: r4(yaw) });

/** The ring's frame: centre, long-axis yaw and the long / short side lengths (rings are rects or round 12-gons). */
function frameOf(ring: V[]): { c: V; yaw: number; L: number; W: number; round: boolean } {
  const xs = ring.map(p => p[0]), ys = ring.map(p => p[1]), c: V = [(Math.min(...xs) + Math.max(...xs)) / 2, (Math.min(...ys) + Math.max(...ys)) / 2];
  if (ring.length > 4) { const d = 2 * Math.max(...ring.map(p => Math.hypot(p[0] - c[0], p[1] - c[1]))); return { c, yaw: 0, L: d, W: d, round: true }; }
  const e0 = Math.hypot(ring[1]![0] - ring[0]![0], ring[1]![1] - ring[0]![1]), e1 = Math.hypot(ring[2]![0] - ring[1]![0], ring[2]![1] - ring[1]![1]);
  const [a, b] = e0 >= e1 ? [ring[0]!, ring[1]!] : [ring[1]!, ring[2]!];
  return { c, yaw: Math.atan2(b[1] - a[1], b[0] - a[0]), L: Math.max(e0, e1), W: Math.min(e0, e1), round: false };
}

/** A stadium (box between two half-round ends) of length L, width W about c along yaw, as a box and two cylinders; `grow` widens it all round. */
function stadium(id: string, surface: string, c: V, yaw: number, L: number, W: number, base: number, h: number, grow = 0): Prim[] {
  const w = W + 2 * grow, s = Math.max(0, L - W) / 2, u: V = [Math.cos(yaw), Math.sin(yaw)];
  const out = [prim(`${id}/end-a`, 'cyl', surface, [c[0] - u[0] * s, c[1] - u[1] * s], [w, w, h], base), prim(`${id}/end-b`, 'cyl', surface, [c[0] + u[0] * s, c[1] + u[1] * s], [w, w, h], base)];
  if (s > 0.01) out.unshift(prim(`${id}/mid`, 'box', surface, c, [2 * s, w, h], base, yaw));
  return out;
}

/** §2 band pairs between z0 and z1: a chrome line and an accent line 0.25 apart at every pitch from the deck line. */
function bands(id: string, accent: string, f: ReturnType<typeof frameOf>, z0: number, z1: number): Prim[] {
  const out: Prim[] = [], D = DRESS;
  for (let z = D.bandFrom, k = 0; z + 2 * D.bandH + D.bandGap <= z1 - 0.3; z += D.bandPitch, k++) {
    if (z < z0 + 0.3) continue;
    out.push(...stadium(`${id}/band-${k}/chrome`, PLATE, f.c, f.yaw, f.L, f.W, z, D.bandH, D.bandOut), ...stadium(`${id}/band-${k}/accent`, accent, f.c, f.yaw, f.L, f.W, z + D.bandH + D.bandGap, D.bandH, D.bandOut));
  }
  return out;
}

/** A glazed streamline stair tower: limestone plinth, glass stadium over a limestone spine (the dog-leg's well wall), band pairs, a stepped chrome cap. */
export function dressStairTower(k: Connector, accent: string): Prim[] {
  const f = frameOf(k.ring as V[]), D = DRESS, b = k.bottom, T = k.top, id = `${k.id}/dress`, u: V = [Math.cos(f.yaw), Math.sin(f.yaw)];
  return [
    ...stadium(`${id}/plinth`, 'limestone', f.c, f.yaw, f.L, f.W, b, D.plinth),
    ...stadium(`${id}/glass`, 'glass', f.c, f.yaw, f.L, f.W, b + D.plinth, T - D.capH - b - D.plinth, -D.glassInset),
    prim(`${id}/spine`, 'box', 'limestone', f.c, [Math.max(1, f.L - f.W), 1.2, T - D.capH - b], b, f.yaw),
    prim(`${id}/fin`, 'box', 'trim', [f.c[0] + u[0] * (f.L / 2 - 0.05), f.c[1] + u[1] * (f.L / 2 - 0.05)], [0.3, 0.6, T + 2.4 - b - D.plinth], b + D.plinth, f.yaw), // a chrome fin up the nose, rising 2.4 over the cap (§4)
    ...bands(id, accent, f, b + D.plinth, T - D.capH),
    ...stadium(`${id}/cap`, PLATE, f.c, f.yaw, f.L, f.W, T - D.capH, D.capH / 2, D.capOut),
    ...stadium(`${id}/coping`, PLATE, f.c, f.yaw, f.L, f.W, T - D.capH / 2, D.capH / 2, -0.4),
  ];
}

/** A glazed lift tube: limestone plinth, glass cylinder, chrome hoops and four mullions, a parked car, a chrome cap and an atom ball. */
export function dressLift(k: Connector, accent: string): Prim[] {
  const f = frameOf(k.ring as V[]), D = DRESS, b = k.bottom, T = k.top, id = `${k.id}/dress`, d = f.W, top = T - 0.9, out: Prim[] = [];
  out.push(prim(`${id}/plinth`, 'cyl', 'limestone', f.c, [d, d, D.plinth], b), prim(`${id}/glass`, 'cyl', 'glass', f.c, [d - 2 * D.glassInset, d - 2 * D.glassInset, top - b - D.plinth], b + D.plinth));
  const n = Math.max(1, Math.round((top - b - D.plinth) / D.hoopEvery));
  for (let i = 0; i <= n; i++) out.push(prim(`${id}/hoop-${i}`, 'cyl', HOOP, f.c, [d + 0.2, d + 0.2, D.hoopH], b + D.plinth + ((top - b - D.plinth - D.hoopH) * i) / n));
  for (let i = 0; i < 4; i++) { const a = Math.PI / 4 + (i * Math.PI) / 2; out.push(prim(`${id}/mullion-${i}`, 'box', 'trim', [f.c[0] + Math.cos(a) * (d / 2), f.c[1] + Math.sin(a) * (d / 2)], [D.mullion, D.mullion, top - b - D.plinth], b + D.plinth)); }
  out.push(prim(`${id}/car`, 'cyl', 'sign', f.c, [d - 0.5, d - 0.5, 2.9], b + D.plinth + 0.05), prim(`${id}/car-band`, 'cyl', accent, f.c, [d - 0.46, d - 0.46, 0.8], b + D.plinth + 1.25));
  out.push(prim(`${id}/cap`, 'cyl', PLATE, f.c, [d + 0.4, d + 0.4, 0.6], top), prim(`${id}/dome`, 'saucer', PLATE, f.c, [d + 0.4, d + 0.4, 0.3], top + 0.6), prim(`${id}/atom`, 'sphere', accent, f.c, [0.6, 0.6, 0.6], top + 0.9));
  return out;
}

/** Dress every station stair tower and every lift that stands in the open (not inside a stair core or a leg, where the enclosure is its face). */
export function dressConnectors(connectors: Connector[], enclosures: { ring: V[]; z0: number; z1: number }[]): Connector[] {
  const cores = connectors.filter(k => k.kind === 'stair' && k.foot[0] === k.head[0] && k.foot[1] === k.head[1]).map(k => ({ ring: k.ring as V[], z0: k.bottom, z1: k.top }));
  const all = [...enclosures, ...cores], enclosed = (k: Connector) => all.some(x => x.z0 <= k.bottom + 0.01 && x.z1 >= k.top - 0.01 && (k.ring as V[]).every(p => pointIn(p, x.ring, 0.05)));
  return connectors.map(k => {
    const accent = k.host.startsWith('bank/') ? 'beam' : 'tower';
    if (k.kind === 'stairTower') return { ...k, dress: dressStairTower(k, accent) };
    if (k.kind === 'lift' && !enclosed(k)) return { ...k, dress: dressLift(k, accent) };
    return k;
  });
}

/** Point in ring, with a margin m outward (bbox-tolerant for the axis-aligned rects the cores and legs are). */
function pointIn(p: V, r: V[], m = 0): boolean {
  const xs = r.map(q => q[0]), ys = r.map(q => q[1]);
  if (r.length === 4 && xs.filter(x => x === xs[0]).length === 2) return p[0] >= Math.min(...xs) - m && p[0] <= Math.max(...xs) + m && p[1] >= Math.min(...ys) - m && p[1] <= Math.max(...ys) + m;
  let c = false; for (let i = 0, j = r.length - 1; i < r.length; j = i++) if ((r[i]![1] > p[1]) !== (r[j]![1] > p[1]) && p[0] < ((r[j]![0] - r[i]![0]) * (p[1] - r[i]![1])) / (r[j]![1] - r[i]![1]) + r[i]![0]) c = !c; return c;
}
