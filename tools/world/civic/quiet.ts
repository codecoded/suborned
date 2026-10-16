// Brief 013 C: the geometry and the coverage arithmetic of the takedown, report and response rules (Vantage's quiet places, Pulse's report points and response
// origins). Pure: no generator state. tools/world/gen/street/quiet.ts applies it to the street layer; tools/world/rules/quiet.ts holds its output (R-HIDE, R-STASH,
// R-REPORT, R-DOCK). Block frame (x south, y east, m) for the walk grid, plan frame for items, as everywhere else; `toBlock` / `toPlan` convert.
import rulebook from '../../../contracts/rules/rulebook.json';
import { type V, dist, dot, inPoly, norm, segDist, sub } from '../../plan/geom';
import { INF, type WalkGrid, reachField } from '../../plan/walk-grid';
import { boxRing, ringGap, samples9 } from '../../plan/street-zones';

/** The numbers the rules hold. `reach` are Pulse's proposed rulebook values `report-reach-m` and `dock-reach-m` ([S], briefs/013/pulse-witnesses.values.json): until Keel adopts them they live here with that source. */
export const QUIET = {
  /** How many bodies a service door's storeroom holds: the rulebook's `stash-door-holds` (Keel rev 4 R4.3), the number the engine takes (GameTakedown.HoldsOf) for a door whatever the site file says. A container holds what its kit piece says (the Dumpster, 1). */
  doorHolds: (rulebook.values as Record<string, { v: number }>)['stash-door-holds']!.v,
  reach: { report: 40, origin: 40 },
  lattice: 2, // m: the samples are the free cells on a 2 m lattice (Pulse measured on 4 m)
  cameraYaw: 30, // deg: the play camera's compass yaw (GDD §10, fixed; the same number as PROV.view.yaw in tools/world/build.ts)
  doorHalf: 0.6, // m: half of a door leaf; a container stands clear of the jamb by `doorClear`
  doorClear: 1.0, // m
  wall: 0.1, // m: a container stands this far off the wall it is against
  apart: 8, // m: a call box, police post or dock stands this far from every other one (a corner of the block is not four things)
  kerbReach: 10, // m: a container that cannot stand against the wall stands in a kerb zone this near the door at most
  lane: 2.0, // m: the clear width a container leaves in front of it (W-5: no gap under 2 m)
  size: { 'police-post': [1.0, 1.0, 2.8] as [number, number, number] }, // m: a police post is a police box of the call box's footprint (it must stand in a kerb zone, 1.5 m on a primary); no kit piece yet (Vela)
};

/** Horizontal unit vector from a point toward the play camera, plan frame (compass yaw 0 = north, the camera looks along the heading and stands behind). */
export const cameraDir = (yawDeg: number): V => [-Math.sin((yawDeg * Math.PI) / 180), -Math.cos((yawDeg * Math.PI) / 180)];

export type StashSpot = { at: V; yaw: number; ring: V[]; n: V; where: 'wall' | 'kerb'; stand: V };

/**
 * The container beside a service door. First choice: against the wall the door is in, to one side of the door, clear of the jamb by `doorClear`, on the side away from the
 * play camera (so it does not stand between the camera and the door), where its footprint is clear of every blocker, ground solid and keep-clear zone, the street layer
 * accepts it (`fits`: not in a street's clear walking zone, 2 m off crossings and doors) and `lane` m of free ground is left in front of it. The other side is tried next.
 * Where the door opens onto a street's walk zone (the strip behind the café) nothing may stand against the wall: the container then stands in the nearest kerb zone of a
 * road within `kerbReach` m of the door, facing the pavement. None found is an error the caller reports (R-STASH names the door).
 */
export function stashSpot(o: {
  door: { id: string; p: V }; building: V[]; size: [number, number, number]; cam: V;
  blockers: V[][]; zones: { id: string; ring: V[] }[]; points: { name: string; p: V }[]; pointClear: number;
  roads: { line: V[]; w: number }[]; kerbReach: number;
  grid: WalkGrid; toBlock: (p: V) => V; fits: (at: V, yaw: number) => boolean;
}): StashSpot | undefined {
  const { door, building, size, grid, toBlock } = o, [w, d] = size;
  // the wall: the building edge nearest the door; n points from the wall to the door
  let best = Infinity, a: V = building[0]!, b: V = building[1]!;
  building.forEach((p, i) => { const q = building[(i + 1) % building.length]!, s = segDist(door.p, p, q); if (s < best) { best = s; a = p; b = q; } });
  const e = norm(sub(b, a)), foot0: V = [a[0] + e[0] * dot(sub(door.p, a), e), a[1] + e[1] * dot(sub(door.p, a), e)];
  let n: V = norm(sub(door.p, foot0)); if (!isFinite(n[0])) n = [e[1], -e[0]];
  if (inPoly([foot0[0] + n[0] * 0.3, foot0[1] + n[1] * 0.3], building)) n = [-n[0], -n[1]];
  const t: V = [n[1], -n[0]], yaw = Math.atan2(t[1], t[0]); // the piece's +X runs along the wall, its front (+Z, yaw + 90°) faces n
  const free = (at: V, yw: number, wall: boolean): V[] | undefined => {
    const ring = boxRing(at, w, d, yw);
    if (o.blockers.some(r => r !== building && ringGap(ring, r) < 0.02)) return undefined;
    if (wall && ringGap(ring, building) < QUIET.wall - 0.02) return undefined;
    if (o.zones.some(z => ringGap(ring, z.ring) === 0)) return undefined;
    if (o.points.some(pt => dist(pt.p, at) < o.pointClear + Math.hypot(w, d) / 2)) return undefined;
    if (!samples9(ring).every(q => grid.free(toBlock(q)) || (wall && building.some((p, i) => segDist(q, p, building[(i + 1) % building.length]!) < 0.3)))) return undefined;
    if (!o.fits(at, yw)) return undefined; // the street layer's own tests: out of a street's walk zone, 2 m off crossings and doors, off the quay
    return ring;
  };
  const sides = ([1, -1] as const).slice().sort((p, q) => dot([t[0] * p, t[1] * p], o.cam) - dot([t[0] * q, t[1] * q], o.cam) || q - p);
  for (const side of sides) {
    const off = QUIET.doorHalf + QUIET.doorClear + w / 2, back = QUIET.wall + d / 2;
    const at: V = [foot0[0] + t[0] * side * off + n[0] * back, foot0[1] + t[1] * side * off + n[1] * back], ring = free(at, yaw, true);
    if (!ring) continue;
    // free ground in front: the walk grid must hold `lane` m of free cells out from the container's face, along its middle and both ends
    let clear = true;
    for (const k of [-1, 0, 1]) for (let u = 0.25; u <= QUIET.lane + 1e-9; u += 0.25) {
      const q: V = [at[0] + n[0] * (d / 2 + u) + t[0] * (k * (w / 2 - 0.1)), at[1] + n[1] * (d / 2 + u) + t[1] * (k * (w / 2 - 0.1))];
      if (!grid.free(toBlock(q))) clear = false;
    }
    if (!clear) continue;
    return { at, yaw, ring, n, where: 'wall', stand: grid.tryNearestFree(toBlock([at[0] + n[0] * (d / 2 + 0.75), at[1] + n[1] * (d / 2 + 0.75)])) ?? toBlock(at) };
  }
  // the kerb zone of the nearest road: stations every metre within kerbReach of the door, nearest to the door first
  const cs: { at: V; yaw: number; n: V; dd: number }[] = [];
  for (const r of o.roads) for (let i = 1; i < r.line.length; i++) {
    const p0 = r.line[i - 1]!, p1 = r.line[i]!, seg = dist(p0, p1); if (seg < 1e-6) continue;
    const tt: V = [(p1[0] - p0[0]) / seg, (p1[1] - p0[1]) / seg], nl: V = [-tt[1], tt[0]];
    for (let s = 0; s < seg; s += 1) {
      const c: V = [p0[0] + tt[0] * s, p0[1] + tt[1] * s], side = dot(sub(door.p, c), nl) >= 0 ? 1 : -1, off = r.w / 2 + 0.45 + d / 2 + 0.05;
      const at: V = [c[0] + nl[0] * side * off, c[1] + nl[1] * side * off], dd = dist(at, door.p);
      if (dd > o.kerbReach) continue;
      const nn: V = [nl[0] * side, nl[1] * side]; // from the road toward the pavement
      cs.push({ at, yaw: Math.atan2(nn[1], nn[0]) - Math.PI / 2, n: nn, dd });
    }
  }
  cs.sort((p, q) => p.dd - q.dd || p.at[0] - q.at[0] || p.at[1] - q.at[1]);
  for (const c of cs) {
    const ring = free(c.at, c.yaw, false); if (!ring) continue;
    return { at: c.at, yaw: c.yaw, ring, n: c.n, where: 'kerb', stand: grid.tryNearestFree(toBlock([c.at[0] + c.n[0] * (d / 2 + 0.75), c.at[1] + c.n[1] * (d / 2 + 0.75)])) ?? toBlock(c.at) };
  }
  return undefined;
}

/** The lattice samples: the free cells on a `lattice` m grid that a walker from `from` can reach (cells sealed off from every door are no ground a reporter walks). Indices into the grid (i * ny + j). */
export function lattice(grid: WalkGrid, from: V[], lat = QUIET.lattice): number[] {
  const reach = reachField(grid, from), step = Math.round(lat / grid.cell), out: number[] = [];
  for (let i = Math.floor(step / 2); i < grid.nx; i += step) for (let j = Math.floor(step / 2); j < grid.ny; j += step) if (grid.cellFree(i, j) && reach[i * grid.ny + j]! < INF) out.push(i * grid.ny + j);
  return out;
}

export type Cand = { id: string; at: V; stand: V; yaw: number; edge: string };

/**
 * Greedy cover: while some sample is farther than `reachM` (walk, cm on the grid) from every source, take the candidate that brings the most uncovered samples within
 * reach (ties: the candidate nearest the window's north-west corner by x then y, then id), and place it with `place` (which may refuse: the layer's own rules; the candidate
 * is then dropped). Deterministic: no random choice anywhere. Returns what was placed and the field of the final set.
 */
export function cover(o: { grid: WalkGrid; samples: number[]; sources: V[]; reachM: number; cands: Cand[]; place: (c: Cand) => boolean; limit?: number }): { placed: Cand[]; field: Int32Array } {
  const { grid, samples } = o, reach = Math.round(o.reachM * 100);
  let field = reachField(grid, o.sources);
  const sets = new Map<Cand, number[]>();
  const cands = o.cands.slice().sort((p, q) => p.at[0] - q.at[0] || p.at[1] - q.at[1] || (p.id < q.id ? -1 : 1));
  for (const c of cands) {
    const f = reachField(grid, [c.stand], reach), hit: number[] = [];
    samples.forEach((s, k) => { if (f[s]! <= reach) hit.push(k); });
    sets.set(c, hit);
  }
  const unc = new Uint8Array(samples.length); samples.forEach((s, k) => { unc[k] = field[s]! > reach ? 1 : 0; });
  const placed: Cand[] = [], dead = new Set<Cand>();
  for (let guard = 0; guard < (o.limit ?? 64); guard++) {
    let pick: Cand | undefined, gain = 0;
    for (const c of cands) { if (dead.has(c)) continue; let g = 0; for (const k of sets.get(c)!) g += unc[k]!; if (g > gain) { gain = g; pick = c; } }
    if (!pick || gain === 0) break;
    if (!o.place(pick)) { dead.add(pick); continue; }
    placed.push(pick); dead.add(pick);
    for (const k of sets.get(pick)!) unc[k] = 0;
  }
  field = reachField(grid, [...o.sources, ...placed.map(c => c.stand)]);
  return { placed, field };
}

/** Coverage numbers on the samples: worst and median walk (m) and the share (per mille) within reach. */
export function measure(field: Int32Array, samples: number[], reachM: number): { worstM: number; medianM: number; within: number } {
  const v = samples.map(s => field[s]!).sort((p, q) => p - q), lim = Math.round(reachM * 100);
  return { worstM: v.length ? Math.round(v[v.length - 1]!) / 100 : 0, medianM: v.length ? Math.round(v[Math.floor(v.length / 2)]!) / 100 : 0, within: v.length ? Math.round((1000 * v.filter(x => x <= lim).length) / v.length) : 1000 };
}
