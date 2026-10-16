// The café's seats and standing points, and who holds them when (brief 010 follow-up, Aster 2026-10-15: "all eight guards stand stacked on one point").
// A seat is a chair of a café set the world draws on a table of the civic block (tools/world/civic/cafe.ts gives the pivot and yaw build.ts places it at; the tests hold
// that to the world export's own civic masses, so a seat stands where the world puts a chair, never beside it). A standing point is open ground near the café's door that keeps CD-1 clear. People are
// given them by person id (Holds, seatPicker): one person to a seat while seats last, then the nearest standing points. Pure: no file is read here.
import type { Civic } from '../plan/civic';
import { footprints } from '../plan/civic';
import { centroid, dist, inPoly, segDist, type V } from '../plan/geom';
import { Solids } from '../plan/walk-solids';
import { isCafeTable, massId, seededYaw, setPivot } from '../world/civic/cafe';

/** The chairs of the café set (kit CafeSet_Tulip, and CafeSet_Tulip_Play which stands for it on the block): three shell chairs on a circle of `radius` m about the
 *  table's pivot, the table centre. `anglesDeg`: kit frame, from +x toward +z. Measured on the built prefab's LOD0 mesh in Unity 6000.6.2f1 (2026-10-15; the chair
 *  feet's centres, tools/people/test/cafe-set.measured.json) and read against tools/decokit/furnish_cafe.py (cafe_set_tulip: a = 90 + 120 k + 30 in Blender's frame,
 *  r 0.72; Blender x, y → kit -x, -z). At plan yaw θ the kit +x turns to (cos θ, sin θ) and +z to (-sin θ, cos θ) (KitPlacer.Place), so a chair stands at θ + angle. */
export const CAFE_SET = { radius: 0.72, anglesDeg: [-60, 60, 180] as readonly number[], pieces: ['CafeSet_Tulip', 'CafeSet_Tulip_Play'] as readonly string[] };
/** A seat is on a chair or table position within this many m (the export rounds to 1 mm). */
export const SEAT_TOL = 0.05;
/** Standing points beside the café [S]: a lattice `spacing` m apart from the café door, within `within` m of it, the nearest `count` that are walkable, `wall` m off a building,
 *  `solid` m off the block's ground solids (tables, beds, the terrace wall; the plan's body clearance, standards.walking.body.r, which scope.ts passes in), `chair` m off a chair, outside every keep-clear zone and bay. */
export const STAND = { spacing: 1.2, within: 22, count: 24, wall: 0.8, solid: 0.65, chair: 0.9 };

const r3 = (n: number) => Math.round(n * 1000) / 1000;
const byId = <T extends { id: string }>(a: T, b: T) => (a.id < b.id ? -1 : 1);

/** A table of the block and what the world draws on it: the piece (undefined: a procedural table, no chairs), its pivot (the table centre) and its plan yaw. */
export type SetAt = { id: string; piece?: string; at: V; yaw: number };
/** Every table of the civic block with the café set the world draws on it, by id: a `cafe-table-N` carries the café set (kit CafeSet_Tulip, drawn as its play-zoom twin) at the
 *  pivot and seeded yaw tools/world/civic/cafe.ts gives it, which is where build.ts puts it (the test holds this to the export's own civic masses); any other table seats one. */
export function cafeSets(civ: Civic): SetAt[] {
  return civ.objects.filter(o => o.kind === 'table').sort(byId).map(o => isCafeTable(o.id)
    ? { id: o.id, piece: CAFE_SET.pieces[0], at: setPivot(o.poly), yaw: seededYaw(massId(o.id)) }
    : { id: o.id, at: centroid(o.poly), yaw: 0 });
}
/** The chairs of a set in the order they are taken, plan frame; a set with no chairs (a procedural table) seats one at the table centre. */
export function seatPoints(s: SetAt): V[] {
  if (!s.piece || !CAFE_SET.pieces.includes(s.piece)) return [[r3(s.at[0]), r3(s.at[1])]];
  return CAFE_SET.anglesDeg.map(a => { const t = s.yaw + (a * Math.PI) / 180; return [r3(s.at[0] + CAFE_SET.radius * Math.cos(t)), r3(s.at[1] + CAFE_SET.radius * Math.sin(t))] as V; });
}
export const seatId = (set: string, k: number) => `${set}-seat-${k}`;
export const standId = (k: number) => `cafe-stand-${String(k).padStart(2, '0')}`;

/** The standing points: see STAND. `free`: walkable ground (the walk graph's blockers); `clear`: outside every keep-clear zone and every bay. Nearest the door first, ties by x then y. */
export function standingPoints(civ: Civic, door: V, chairs: V[], free: (p: V) => boolean, clear: (p: V) => boolean, solid: number = STAND.solid, count: number = STAND.count): V[] {
  const solids = new Solids(civ.solids ?? []), walls = footprints(civ), ground = civ.solids ?? [];
  const edge = (polys: V[][], p: V) => Math.min(...polys.flatMap(poly => poly.map((a, i) => segDist(p, a, poly[(i + 1) % poly.length]!))));
  const out: V[] = [], n = Math.ceil(STAND.within / STAND.spacing);
  for (let i = -n; i <= n; i++) for (let j = -n; j <= n; j++) {
    const p: V = [r3(door[0] + i * STAND.spacing), r3(door[1] + j * STAND.spacing)];
    if (dist(p, door) > STAND.within || !free(p) || !clear(p) || solids.inside(p) || walls.some(w => inPoly(p, w))) continue;
    if (edge(ground, p) < solid || edge(walls, p) < STAND.wall || chairs.some(c => dist(p, c) < STAND.chair)) continue;
    out.push(p);
  }
  return out.sort((a, b) => dist(a, door) - dist(b, door) || a[0] - b[0] || a[1] - b[1]).slice(0, count);
}

/** The day-clock parts of a stop given in absolute minutes (a stop may run past midnight). */
export const dayParts = (from: number, to: number): [number, number][] => {
  const k = Math.floor(from / 1440) * 1440, f = from - k, t = to - k;
  return t > 1440 ? [[f, 1440], [0, t - 1440]] : [[f, t]];
};
/** Who holds which place when: a place is held over [from, to) minutes of the day clock by one person at a time. */
export class Holds {
  private by = new Map<string, [number, number][]>();
  free(place: string, from: number, to: number): boolean {
    const held = this.by.get(place);
    return !held || dayParts(from, to).every(([a, b]) => held.every(([x, y]) => !(a < y && x < b)));
  }
  take(place: string, from: number, to: number): void {
    const held = this.by.get(place) ?? [];
    held.push(...dayParts(from, to));
    this.by.set(place, held);
  }
}

/** A stop at a seat or standing point: the times (absolute minutes) it would take if the person sat there, and `q`, the walk it costs, where the stop needs it. */
export type Fit = { from: number; to: number; q?: number };
export type Seated = Fit & { place: string };
/** The seat a person takes: the first free place, in this order, for the stop `fit` times. undefined: the stop cannot be made (a meal that does not fit the shift, or every place that fits is held for the time). */
export type Pick = (pref: Table, fit: (place: string) => Fit | undefined) => Seated | undefined;
export type Table = { id: string; at: V; seats: string[]; /** a place to stand, not sit (the café's counter window, a tram stop's queue): a stop here takes the first free of these standing points, never a chair */ standing?: string[] };

/** A picker over the café: the chairs of every table (the person's own table first, then the nearest), then the standing points nearest the door. Every call that finds a place
 *  holds it. A stop that no place can take is not made (`notes` says which): nobody sits at a seat another holds, so P-13 cannot fail on a busy square. */
export function seatPicker(tables: Table[], stands: string[], holds: Holds, notes: string[]) {
  const order = new Map<string, string[]>();
  const around = (pref: Table) => {
    let o = order.get(pref.id);
    if (!o && pref.standing) { o = pref.standing; order.set(pref.id, o); }
    if (!o) { o = [...tables].sort((a, b) => dist(a.at, pref.at) - dist(b.at, pref.at) || (a.id < b.id ? -1 : 1)).flatMap(t => t.seats).concat(stands); order.set(pref.id, o); }
    return o;
  };
  return (who: string): Pick => (pref, fit) => {
    for (const place of around(pref)) {
      const f = fit(place); if (!f) continue;
      if (holds.free(place, f.from, f.to)) { holds.take(place, f.from, f.to); return { place, ...f }; }
    }
    // every place that fits is held for the stop: the stop is not made (undefined), never double-booked. The day falls back (a meal at the café's chairs is eaten on site, an
    // evening or a stop before the shift is not made: tools/people/day.ts) and the loss is reported as a note, not a fault: a square that is full is a busy square.
    const first = pref.seats[0] ?? pref.standing?.[0], f = first ? fit(first) : undefined;
    if (f) notes.push(`${who}: no free seat or standing point for the stop at ${Math.floor(f.from / 60) % 24}:${String(f.from % 60).padStart(2, '0')}; the stop is not made`);
    return undefined;
  };
}
