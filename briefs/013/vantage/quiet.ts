// Brief 013 C · Vantage · quiet places on the civic block: where a body can go out of sight, measured with the engine's own perception
// (rulebook perception + witness: cones, the near circle, the patrol's sweep, people within 13 m, occluders at eye height 1.6 m), the
// site's walk grid (A*, 8 neighbours, no corner cutting, string-pulled, as sim/Suborned.Sim/Grid.cs) and the promoted crowd's routines
// (sim/data/handover.people.json, stepped as GameCrowd.RoutinePos). Static survey, not a replay: the watchers stand on their routines
// with nothing done to them (no loop, no reroute, no lure), the crowd walks its day, and nothing is changed in any contract.
// Run (repo root): bun briefs/013/vantage/quiet.ts   → briefs/013/vantage/quiet.png, quiet-after.png, quiet.json. Needs rsvg-convert.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { resolve } from 'node:path';
import { derive } from '../../../tools/plan/plan';
import { civic, toPlanFn } from '../../../tools/plan/civic';
import { generated } from '../../../tools/plan/world-ctx';
import { seen } from '../../../tools/missions/sight';
import { blockersFor } from '../../../tools/missions/handover/visibility';

const ROOT = resolve(import.meta.dir, '../../..'), OUT = resolve(import.meta.dir);
const J = (p: string) => JSON.parse(readFileSync(resolve(ROOT, p), 'utf8'));
const site = J('sim/data/handover.site.json'), people = J('sim/data/handover.people.json'), plan = J('contracts/world/harbour-district.plan.json');
const block = J(plan.civic.block), rules = J('contracts/rules/rulebook.json'), kit = J('contracts/world/kit.json'), world = J('out/world/harbour-district.world.json');
const val = (id: string) => rules.values[id].v as number;

type P = [number, number]; // block frame, cm (x south, y east)
const LIFT = val('carry-s'), WALK = val('op-speed-cm-s'), EYE = val('eye-cm'), WIT = val('witness-range-m') * 100, NEAR = val('near-m') * 100;
const CHIME = site.schedule.chime as number, OVER = site.schedule.over as number;

// ---------------------------------------------------------------- the walk grid (Grid.cs)
const G = site.grid, NX = G.nx as number, NY = G.ny as number, C = G.cell as number;
const free = (i: number, j: number) => i >= 0 && j >= 0 && i < NX && j < NY && G.rows[i][j] === '.';
const CI = (x: number) => Math.floor((x - G.x0) / C), CJ = (y: number) => Math.floor((y - G.y0) / C);
const freeP = (p: P) => free(CI(p[0]), CJ(p[1]));
const centre = (i: number, j: number): P => [G.x0 + i * C + C / 2, G.y0 + j * C + C / 2];
const d2 = (a: P, b: P) => (a[0] - b[0]) ** 2 + (a[1] - b[1]) ** 2, dist = (a: P, b: P) => Math.sqrt(d2(a, b));
function nearestFree(p: P): P | null {
  if (freeP(p)) return p;
  const ci = CI(p[0]), cj = CJ(p[1]);
  for (let r = 1; r <= 12; r++) { let best: P | null = null, bd = Infinity; for (let i = ci - r; i <= ci + r; i++) for (let j = cj - r; j <= cj + r; j++) { if (!free(i, j)) continue; const c = centre(i, j), d = d2(c, p); if (d < bd) { bd = d; best = c; } } if (best) return best; }
  return null;
}
const lineFree = (a: P, b: P) => { const n = Math.max(1, Math.floor(dist(a, b) / 10)); for (let k = 0; k <= n; k++) if (!freeP([a[0] + ((b[0] - a[0]) * k) / n, a[1] + ((b[1] - a[1]) * k) / n])) return false; return true; };
const DI = [1, -1, 0, 0, 1, 1, -1, -1], DJ = [0, 0, 1, -1, 1, -1, 1, -1];
const routes = new Map<string, P[] | null>();
function route(a: P, b: P): P[] | null {
  const key = `${a}|${b}`; if (routes.has(key)) return routes.get(key)!;
  const s0 = nearestFree(a), b0 = nearestFree(b); if (!s0 || !b0) return null;
  const si = CI(s0[0]), sj = CJ(s0[1]), ti = CI(b0[0]), tj = CJ(b0[1]), s = si * NY + sj, t = ti * NY + tj;
  const g = new Float64Array(NX * NY).fill(Infinity), prev = new Int32Array(NX * NY).fill(-1), closed = new Uint8Array(NX * NY);
  const H = (i: number, j: number) => { const di = Math.abs(i - ti), dj = Math.abs(j - tj); return C * Math.max(di, dj) + (C * 0.414) * Math.min(di, dj); };
  const heap: [number, number][] = []; const push = (f: number, k: number) => { heap.push([f, k]); let i = heap.length - 1; while (i > 0) { const p = (i - 1) >> 1; if (heap[p]![0] < heap[i]![0] || (heap[p]![0] === heap[i]![0] && heap[p]![1] <= heap[i]![1])) break; [heap[p], heap[i]] = [heap[i]!, heap[p]!]; i = p; } };
  const pop = () => { const top = heap[0]!, last = heap.pop()!; if (heap.length) { heap[0] = last; let i = 0; for (;;) { const l = 2 * i + 1, r = l + 1; let m = i; const lt = (a: number, b: number) => heap[a]![0] < heap[b]![0] || (heap[a]![0] === heap[b]![0] && heap[a]![1] < heap[b]![1]); if (l < heap.length && lt(l, m)) m = l; if (r < heap.length && lt(r, m)) m = r; if (m === i) break; [heap[m], heap[i]] = [heap[i]!, heap[m]!]; i = m; } } return top[1]; };
  g[s] = 0; push(H(si, sj), s);
  while (heap.length) {
    const k = pop(); if (closed[k]) continue; closed[k] = 1; if (k === t) break;
    const i = Math.floor(k / NY), j = k % NY;
    for (let d = 0; d < 8; d++) {
      const ni = i + DI[d]!, nj = j + DJ[d]!; if (!free(ni, nj)) continue;
      if (d >= 4 && (!free(i + DI[d]!, j) || !free(i, j + DJ[d]!))) continue;
      const nk = ni * NY + nj; if (closed[nk]) continue;
      const ng = g[k]! + (d < 4 ? C : Math.floor((C * 1414) / 1000));
      if (ng < g[nk]!) { g[nk] = ng; prev[nk] = k; push(ng + H(ni, nj), nk); }
    }
  }
  if (g[t] === Infinity) { routes.set(key, null); return null; }
  const cells: P[] = []; for (let k = t; k !== s; k = prev[k]!) cells.push(centre(Math.floor(k / NY), k % NY)); cells.reverse();
  const raw: P[] = [a]; if (s0[0] !== a[0] || s0[1] !== a[1]) raw.push(s0); raw.push(...cells); raw.push(b);
  const path: P[] = [raw[0]!]; let at = 0;
  while (at < raw.length - 1) { let to = raw.length - 1; while (to > at + 1 && !lineFree(raw[at]!, raw[to]!)) to--; path.push(raw[to]!); at = to; }
  routes.set(key, path); return path;
}
const plen = (p: P[]) => p.slice(1).reduce((s, q, i) => s + dist(p[i]!, q), 0);
const along = (p: P[], d: number): P => { for (let i = 1; i < p.length; i++) { const l = dist(p[i - 1]!, p[i]!); if (d <= l) { const k = l ? d / l : 0; return [p[i - 1]![0] + (p[i]![0] - p[i - 1]![0]) * k, p[i - 1]![1] + (p[i]![1] - p[i - 1]![1]) * k]; } d -= l; } return p[p.length - 1]!; };

// ---------------------------------------------------------------- sight (Site.Sight, Occluder.Blocks, G.InCone)
type Occ = { id: string; z: number; h: number; poly: P[]; box: [number, number, number, number] };
const occs: Occ[] = [...site.occluders, ...site.cover.blockers.map((b: any) => ({ ...b, id: 'cover:' + b.id }))].map((o: any) => ({ id: o.id, z: o.z, h: o.h, poly: o.poly, box: [Math.min(...o.poly.map((p: P) => p[0])), Math.max(...o.poly.map((p: P) => p[0])), Math.min(...o.poly.map((p: P) => p[1])), Math.max(...o.poly.map((p: P) => p[1]))] })).filter(o => EYE >= o.z && EYE <= o.z + o.h);
const cross = (o: P, a: P, b: P) => (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0]);
const onSeg = (p: P, a: P, b: P) => cross(a, b, p) === 0 && Math.min(a[0], b[0]) <= p[0] && p[0] <= Math.max(a[0], b[0]) && Math.min(a[1], b[1]) <= p[1] && p[1] <= Math.max(a[1], b[1]);
function inPoly(p: P, poly: P[]) { let ins = false; for (let i = 0, j = poly.length - 1; i < poly.length; j = i++) { const a = poly[i]!, b = poly[j]!; if (onSeg(p, a, b)) return true; if ((a[1] > p[1]) !== (b[1] > p[1])) { const c = cross(a, b, p); if (b[1] > a[1] ? c < 0 : c > 0) ins = !ins; } } return ins; }
const sg = (v: number) => (v > 0 ? 1 : v < 0 ? -1 : 0);
function segHit(a: P, b: P, c: P, d: P) { const d1 = sg(cross(c, d, a)), d2_ = sg(cross(c, d, b)), d3 = sg(cross(a, b, c)), d4 = sg(cross(a, b, d)); if (d1 * d2_ < 0 && d3 * d4 < 0) return true; return (d1 === 0 && onSeg(a, c, d)) || (d2_ === 0 && onSeg(b, c, d)) || (d3 === 0 && onSeg(c, a, b)) || (d4 === 0 && onSeg(d, a, b)); }
function sight(a: P, b: P): boolean {
  for (const o of occs) {
    if (Math.max(a[0], b[0]) < o.box[0] || Math.min(a[0], b[0]) > o.box[1] || Math.max(a[1], b[1]) < o.box[2] || Math.min(a[1], b[1]) > o.box[3]) continue;
    if (inPoly(a, o.poly) || inPoly(b, o.poly)) return false;
    for (let i = 0, j = o.poly.length - 1; i < o.poly.length; j = i++) if (segHit(a, b, o.poly[j]!, o.poly[i]!)) return false;
  }
  return true;
}
const insideOcc = (p: P) => occs.some(o => p[0] >= o.box[0] && p[0] <= o.box[1] && p[1] >= o.box[2] && p[1] <= o.box[3] && inPoly(p, o.poly));
function inCone(o: P, f: P, reach: number, half: number, near: number, p: P) {
  if (d2(o, p) <= near * near) return true;
  const rx = p[0] - o[0], ry = p[1] - o[1], fl = Math.hypot(f[0], f[1]), fx = f[0] / fl, fy = f[1] / fl, ahead = rx * fx + ry * fy;
  if (ahead < 0 || ahead > reach) return false;
  return Math.abs(rx * fy - ry * fx) * reach <= ahead * half;
}
const PL = (id: string): P => site.places[id].at;
const stand = (id: string): P => nearestFree(PL(id))!;

// ---------------------------------------------------------------- the watchers, on their routines, nothing done to them
type Watcher = { id: string; kind: 'camera' | 'guard' | 'patrol' | 'rivals' | 'cast'; from: number; to: number; sees: (p: P) => number; draw: P };
const cam = (c: any): Watcher => ({ id: `camera:${c.id}`, kind: 'camera', from: 0, to: OVER, draw: c.at, sees: p => (inCone(c.at, c.facing, c.reach, c.half, 0, p) && sight(c.at, p) ? 1 : 0) });
const post = stand(site.guard.post), guardF: P = site.guard.facing;
const pA = stand(site.patrol.a), pB = stand(site.patrol.b), pF: P = site.patrol.facing;
const sweep = Array.from({ length: 41 }, (_, k): P => [pA[0] + ((pB[0] - pA[0]) * k) / 40, pA[1] + ((pB[1] - pA[1]) * k) / 40]);
const inSweep = (o: P, p: P) => { const fl = Math.hypot(pF[0], pF[1]), rx = p[0] - o[0], ry = p[1] - o[1], depth = (rx * pF[0] + ry * pF[1]) / fl, side = Math.abs(rx * pF[1] - ry * pF[0]) / fl; return depth >= 0 && depth <= val('patrol-depth-m') * 100 && side <= val('patrol-half-base-cm') + (depth * val('patrol-half-per-mille')) / 1000; };
const inez = stand(site.places['d:Café door'] ? 'd:Café door' : 'beside-inez'), driver = stand(site.car.driver), rivalPark = stand(site.rivals.park);
const witness = (at: P) => (p: P) => (d2(at, p) <= WIT * WIT && sight(at, p) ? 1 : 0);
const watchers: Watcher[] = [
  ...site.cameras.map(cam),
  { id: 'guard (at the door)', kind: 'guard', from: 0, to: CHIME, draw: post, sees: p => (inCone(post, guardF, val('door-reach-m') * 100, val('door-half-m') * 100, 0, p) && sight(post, p) ? 1 : 0) },
  { id: 'patrol (sweep)', kind: 'patrol', from: 0, to: OVER, draw: [(pA[0] + pB[0]) / 2, (pA[1] + pB[1]) / 2], sees: p => sweep.filter(o => inSweep(o, p) && sight(o, p)).length / sweep.length },
  { id: 'rivals (parked)', kind: 'rivals', from: site.schedule.rivalsArrive, to: OVER, draw: rivalPark, sees: p => (inCone(rivalPark, [0, 100], val('facing-reach-m') * 100, val('rivals-half-m') * 100, NEAR, p) && sight(rivalPark, p) ? 1 : 0) },
  { id: 'Inez (café door)', kind: 'cast', from: 0, to: OVER, draw: inez, sees: witness(inez) },
  { id: 'driver (pickup)', kind: 'cast', from: 0, to: site.schedule.valeExits, draw: driver, sees: witness(driver) },
];
// cameras the world builds that the site does not watch with (world street kinds *-camera)
const B = (p: number[]): P => [Math.round((-230 - p[1]!) * 100), Math.round((p[0]! + 130) * 100)];
const worldCams = world.street.filter((s: any) => /camera/.test(s.kind)).map((s: any) => ({ id: s.id, kind: s.kind, at: B(s.at) })).filter((c: any) => c.at[0] > -4000 && c.at[0] < 10000 && c.at[1] > -7000 && c.at[1] < 2500);

// ---------------------------------------------------------------- the crowd on its routines (GameCrowd.RoutinePos), cast by role excluded
const castIds = new Set(Object.values<any>(site.cast).filter(v => v && v.id).map(v => v.id));
type Walk = { id: string; job: string; at: (t: number) => P | null };
const walkers: Walk[] = people.people.filter((q: any) => !castIds.has(q.id)).map((q: any) => {
  const st = q.stops as [string, number, number, number][];
  return { id: q.id, job: q.job, at: (t: number): P | null => {
    for (let i = 0; i < st.length; i++) {
      const [pl, f, to, out] = st[i]!;
      if (t >= f && t < to) return out ? people.places[pl] : null;
      if (i + 1 < st.length && t >= to && t < st[i + 1]![1]) {
        const nx = st[i + 1]![0]; if (pl === 'beyond' || nx === 'beyond') return null;
        const a: P = people.places[pl], b: P = people.places[nx], r = route(a, b); if (!r) return b;
        const full = [a, ...r, b], L = plen(full), span = st[i + 1]![1] - to; return span <= 0 ? b : along(full, (L * (t - to)) / span);
      }
    }
    return null;
  } };
});
const T = Array.from({ length: OVER }, (_, t) => t); // 1 s steps, 0:00 to the close
const crowdAt = T.map(t => walkers.map(w => ({ w, p: w.at(t) })).filter(x => x.p) as { w: Walk; p: P }[]);
type Exp = { pre: number; post: number; who: Set<string>; first: number; busy: boolean[] };
function crowdExposure(p: P): Exp {
  const e: Exp = { pre: 0, post: 0, who: new Set(), first: -1, busy: T.map(() => false) };
  for (const t of T) for (const { w, p: q } of crowdAt[t]!) if (d2(p, q) <= WIT * WIT && sight(q, p)) { if (t < CHIME) e.pre++; else e.post++; e.who.add(w.id); e.busy[t] = true; if (e.first < 0) e.first = t; }
  return e;
}
// the seconds before the chime when nobody of the crowd sees the point, as m:ss spans of 10 s or more
const quietSpans = (e: Exp) => { const out: string[] = []; let a = -1; for (let t = 0; t <= CHIME; t++) { const q = t < CHIME && !e.busy[t]; if (q && a < 0) a = t; if (!q && a >= 0) { if (t - a >= 10) out.push(`${fmt(a)}-${fmt(t)}`); a = -1; } } return out; };
const busyPre = (e: Exp) => e.busy.slice(0, CHIME).filter(Boolean).length;

// ---------------------------------------------------------------- candidates, derived from the block and the world (no hand placement for a mission)
// A body (1.8 x 0.5 x 0.3 m) fits in a piece whose footprint is >= 1.8 x 0.9 m and which stands >= 1.0 m: a closed container (Alleys kit).
const fits = (s: number[]) => Math.max(s[0]!, s[2]!) >= 1.8 && Math.min(s[0]!, s[2]!) >= 0.9 && s[1]! >= 1.0;
const kitRows = kit.assets.map((a: any) => ({ id: a.id, status: a.status, category: a.category, size: a.size, fits: fits(a.size) }));
// Size is necessary, not sufficient: a lamp, a wall or a stair passes it. Whether a piece is a container is a fact of the model, so it
// must be a tag in kit.json (proposed, Splice): `stash: { holds: 1, s }`. Until then this survey reads the proposal below, which is
// the only hand-written table here and names kit pieces, never places.
const STASH_PROPOSED: Record<string, string> = { Dumpster: 'closed container, lid: holds 1 (proposed stash tag)' };
const sizeFit = kitRows.filter((r: any) => r.fits && /Alleys|Street|Booths/.test(r.category)).map((r: any) => r.id);
const holders = kitRows.filter((r: any) => STASH_PROPOSED[r.id]).map((r: any) => ({ ...r, stash: STASH_PROPOSED[r.id] }));
const placedKit = (id: string) => world.civic.filter((m: any) => m.kit?.id === id).length + world.street.filter((s: any) => s.kit?.id === id).length;
type Cand = { id: string; cls: 'kit' | 'recess' | 'door'; name: string; at: P; note: string; exists: boolean };
const sc = (id: string): P => { const s = block.structures.find((x: any) => x.id === id); const c: P = [s.polygon.reduce((a: number, p: number[]) => a + p[0]!, 0) / s.polygon.length * 100, s.polygon.reduce((a: number, p: number[]) => a + p[1]!, 0) / s.polygon.length * 100]; return c; };
const off = (p: P, dx: number, dy: number): P => nearestFree([p[0] + dx, p[1] + dy])!;
const bins = world.street.filter((s: any) => s.kind === 'bin').map((s: any) => ({ id: s.id, at: B(s.at) })).filter((b: any) => b.at[0] > -4000 && b.at[0] < 10000 && b.at[1] > -7000 && b.at[1] < 2500);
const cands: Cand[] = [
  { id: 'K1', cls: 'kit', name: 'service cabinet 0 (lane)', at: off(sc('civic-service-cabinet-0'), 0, 100), exists: true, note: 'UtilityCabinet 1.29 x 1.28 x 0.64 m: too small to hold a body; a screen only below 1.26 m' },
  { id: 'K2', cls: 'kit', name: 'service cabinet 1 (lane)', at: off(sc('civic-service-cabinet-1'), 0, 100), exists: true, note: 'as K1' },
  { id: 'K3', cls: 'kit', name: 'control cabinet (square)', at: stand('cabinet'), exists: true, note: "the Tech's cabinet; as K1, and in the open square" },
  ...bins.map((b: any, k: number): Cand => ({ id: `K${4 + k}`, cls: 'kit', name: `recycling bins ${b.id.split('/').pop()}`, at: off(b.at, 0, 100), exists: true, note: 'RecyclingBins 1.06 x 0.89 x 0.42 m: too small; Rocket Lane west pavement' })),
  { id: 'R1', cls: 'recess', name: 'behind the café (mid)', at: stand('d:Behind café'), exists: true, note: 'the 4 m strip between the café back (x -22) and Starline Street' },
  { id: 'R2', cls: 'recess', name: 'service lane, north mouth', at: nearestFree([4900, -4450])!, exists: true, note: 'keep-clear zone service-lane (x 48-77, y -46 to -40.5), at the forecourt end' },
  { id: 'R3', cls: 'recess', name: 'service lane, middle', at: nearestFree([6300, -4400])!, exists: true, note: "between the two service cabinets, against the Office's west wing" },
  { id: 'R4', cls: 'recess', name: 'service lane, south end', at: nearestFree([7400, -4350])!, exists: true, note: "by the official tower's west corner, behind the pickup camera" },
  { id: 'R5', cls: 'recess', name: 'office east side', at: nearestFree([6400, -1850])!, exists: true, note: "the Office's east face over the bed civic-office-west" },
  { id: 'R6', cls: 'recess', name: 'official tower pocket (west)', at: nearestFree([7300, -3440])!, exists: true, note: 'inner corner where the official tower meets the wing (topiary top-official-w)' },
  { id: 'R7', cls: 'recess', name: 'café east end', at: nearestFree([-1900, -1600])!, exists: true, note: "the café's east arc behind tables 6 and 7, by the robot dock and the mast camera" },
  { id: 'D1', cls: 'door', name: 'Office service door (proposed)', at: nearestFree([6300, -4110])!, exists: false, note: "not in the block: GDD §07 route A's 'exits service door with Vale'; west wing face into the service lane" },
  { id: 'D2', cls: 'door', name: 'café kitchen door (proposed)', at: nearestFree([-2240, -2600])!, exists: false, note: "not in the block: the café's back (north) face, onto 'Behind café'" },
];
const sources: { id: string; name: string; at: P }[] = [
  { id: 'guard', name: "guard's door post", at: post },
  { id: 'pickup', name: 'pickup (driver)', at: driver },
  { id: 'cafe', name: 'café door (Inez)', at: inez },
];

// ---------------------------------------------------------------- the play camera (rule V-1's figure test), plan frame
const d = derive(plan), civ = civic(plan.civic, block), { w } = generated(plan, d, civ), toPlan = toPlanFn(plan.civic), BL = blockersFor(w, civ, plan);
const playSeen = (p: P) => { const v = seen(toPlan([p[0] / 100, p[1] / 100]), 0, BL); return { past: +v.pastArchitecture.toFixed(2), ghost: +v.onlyFade.toFixed(2), by: v.by.slice(0, 3) }; };

// ---------------------------------------------------------------- measure
const fmt = (s: number) => `${Math.floor(s / 60)}:${String(Math.round(s % 60)).padStart(2, '0')}`;
const secSees = (p: P) => watchers.map(x => ({ id: x.id, kind: x.kind, share: x.sees(p), from: x.from, to: x.to })).filter(x => x.share > 0);
const rows = cands.map(c => {
  const e = crowdExposure(c.at), carry = Object.fromEntries(sources.map(s => { const r = route(s.at, c.at); const L = r ? plen(r) : NaN; return [s.id, { m: +(L / 100).toFixed(1), walkS: +(L / WALK).toFixed(1), withLiftS: +(L / WALK + LIFT).toFixed(1), path: r }]; }));
  return { ...c, watchers: secSees(c.at), crowd: { preS: e.pre, postS: e.post, people: e.who.size, first: e.first < 0 ? null : fmt(e.first), busyPreS: busyPre(e), quietPre: quietSpans(e) }, play: playSeen(c.at), carry };
});
// the carry walks themselves: who sees the Enforcer on the way (security sampled every 0.5 m; crowd = people who see any of it, pre-chime)
function pathExposure(r: P[]) {
  const L = plen(r), n = Math.max(1, Math.floor(L / 50)), pts = Array.from({ length: n + 1 }, (_, k) => along(r, (L * k) / n));
  const by = new Map<string, number>(); for (const p of pts) for (const x of secSees(p)) by.set(x.id, Math.max(by.get(x.id) ?? 0, x.share));
  const share = (id: string) => pts.filter(p => watchers.find(x => x.id === id)!.sees(p) > 0).length / pts.length;
  return [...by.keys()].map(id => ({ id, ofPath: +share(id).toFixed(2), peak: +by.get(id)!.toFixed(2) }));
}
// the sweep in time (Game.SweepPos): A to B and back over patrol-period-s. For each start second of the cycle, does the whole act stay
// out of the sweep: DWELL s at the takedown spot (the takedown and the lift; the takedown's own time is Motive's, 3 s assumed), the carry
// at walk pace, STASH s at the place (assumed)? Gives the share of the cycle that is clear and the longest run of clear start seconds.
const PER = val('patrol-period-s'), DWELL = 3 + LIFT, STASH = 5;
const sweepAt = (t: number): P => { const ph = ((t % PER) + PER) % PER, half = PER / 2, k = (ph <= half ? ph : PER - ph) / half; return [pA[0] + (pB[0] - pA[0]) * k, pA[1] + (pB[1] - pA[1]) * k]; };
function patrolPhases(r: P[]) {
  const L = plen(r), walk = L / WALK, total = DWELL + walk + STASH;
  const posAt = (u: number): P => (u < DWELL ? r[0]! : u < DWELL + walk ? along(r, (u - DWELL) * WALK) : r[r.length - 1]!);
  const clear = Array.from({ length: PER }, (_, t0) => { for (let u = 0; u <= total; u += 0.5) { const o = sweepAt(t0 + u), q = posAt(u); if (inSweep(o, q) && sight(o, q)) return false; } return true; });
  let best = 0, run = 0; for (let k = 0; k < 2 * PER; k++) { run = clear[k % PER] ? run + 1 : 0; best = Math.max(best, Math.min(run, PER)); }
  return { actS: +total.toFixed(1), clearShare: +(clear.filter(Boolean).length / PER).toFixed(2), longestClearStartS: best };
}
// heat map: every free 1 m sample of the block's ground
const X0 = -3200, X1 = 9600, Y0 = -6600, Y1 = 1200, STEP = 100;
type Cell = { p: P; sec: string[]; patrol: number; pre: number; post: number };
const cells: Cell[] = [];
const cellIx = new Map<string, Cell>();
for (let x = X0 + 50; x < X1; x += STEP) for (let y = Y0 + 50; y < Y1; y += STEP) { const p: P = [x, y]; if (!freeP(p) || insideOcc(p)) continue; const s = secSees(p); const c = { p, sec: s.filter(q => q.kind !== 'patrol').map(q => q.id), patrol: s.find(q => q.kind === 'patrol')?.share ?? 0, pre: 0, post: 0 }; cells.push(c); cellIx.set(`${x},${y}`, c); }
for (const t of T) for (const { p: q } of crowdAt[t]!) {
  const x0 = Math.ceil((q[0] - WIT - X0 - 50) / STEP), x1 = Math.floor((q[0] + WIT - X0 - 50) / STEP), y0 = Math.ceil((q[1] - WIT - Y0 - 50) / STEP), y1 = Math.floor((q[1] + WIT - Y0 - 50) / STEP);
  for (let i = x0; i <= x1; i++) for (let j = y0; j <= y1; j++) { const c = cellIx.get(`${X0 + 50 + i * STEP},${Y0 + 50 + j * STEP}`); if (!c || d2(c.p, q) > WIT * WIT || !sight(q, c.p)) continue; if (t < CHIME) c.pre++; else c.post++; }
}

// ---------------------------------------------------------------- report rows
const out = rows.map(r => ({ id: r.id, cls: r.cls, name: r.name, exists: r.exists, at: [r.at[0] / 100, r.at[1] / 100], note: r.note, watchers: r.watchers.map(x => ({ id: x.id, share: +x.share.toFixed(2), window: `${fmt(x.from)}-${fmt(x.to)}` })), crowd: r.crowd, play: r.play,
  carry: Object.fromEntries(Object.entries(r.carry).map(([k, v]) => [k, { m: v.m, walkS: v.walkS, withLiftS: v.withLiftS, seenOnTheWay: v.path ? pathExposure(v.path) : [], patrol: v.path ? patrolPhases(v.path) : null }])) }));
// the takedown spots themselves: who sees the act where the target stands (the target excluded)
const spots = [{ id: 'guard', at: post, self: 'guard (at the door)' }, { id: 'pickup', at: driver, self: 'driver (pickup)' }, { id: 'cafe', at: inez, self: 'Inez (café door)' }].map(s => { const e = crowdExposure(s.at); return { id: s.id, at: [s.at[0] / 100, s.at[1] / 100], watchers: secSees(s.at).filter(x => x.id !== s.self).map(x => ({ id: x.id, share: +x.share.toFixed(2), window: `${fmt(x.from)}-${fmt(x.to)}` })), crowd: { preS: e.pre, postS: e.post, people: e.who.size, first: e.first < 0 ? null : fmt(e.first), busyPreS: busyPre(e), quietPre: quietSpans(e), who: [...e.who].map(id => `${id} ${walkers.find(w => w.id === id)!.job}`) } }; });
const quietCells = (k: 'pre' | 'post') => cells.filter(c => !c.sec.length && !c.patrol && c[k] === 0).length;
const summary = { sizeFitOnly: sizeFit.filter((id: string) => !STASH_PROPOSED[id]), cells: cells.length, quietPre: quietCells('pre'), quietPost: quietCells('post'), walkers: walkers.length, onStreet: { pre: Math.max(...T.slice(0, CHIME).map(t => crowdAt[t]!.length)), post: Math.max(...T.slice(CHIME).map(t => crowdAt[t]!.length)) }, worldCams, holders, placed: Object.fromEntries(holders.map((h: any) => [h.id, placedKit(h.id)])) };
writeFileSync(resolve(OUT, 'quiet.json'), JSON.stringify({ note: 'brief 013 C, Vantage: quiet-place survey (bun briefs/013/vantage/quiet.ts). Block frame metres (x south, y east). walkS at op-speed-cm-s, withLiftS adds carry-s; shares are the fraction of the patrol sweep (or of the path) that sees; play = share of a 1.75 m figure past architecture at the play camera (past) and behind fade-only blockers (ghost).', summary, spots, rows: out }, null, 1));
for (const r of out) console.log(r.id, r.name, JSON.stringify(r.watchers.map(x => `${x.id}${x.share < 1 ? ' ' + x.share : ''}`)), 'crowd', JSON.stringify(r.crowd), 'play', r.play, Object.entries(r.carry).map(([k, v]: any) => `${k} ${v.m} m ${v.walkS} s [${v.seenOnTheWay.map((x: any) => `${x.id} ${x.ofPath}`).join('; ')}]`).join(' | '));
for (const s of spots) console.log('spot', s.id, JSON.stringify(s.watchers), JSON.stringify(s.crowd));
console.log(JSON.stringify({ ...summary, holders: summary.holders.map((h: any) => `${h.id} ${h.status} ${h.size}`) }));

// ---------------------------------------------------------------- the sheet (north up: block -x up, +y right)
const K = 9, PADT = 70, VX0 = -3200, VX1 = 9600, VY0 = -6600, VY1 = 1200;
const PW = ((VY1 - VY0) / 100) * K, PH = ((VX1 - VX0) / 100) * K;
function panel(win: 'pre' | 'post', ox: number, title: string, carries: boolean): string {
  const sx = (y: number) => ox + ((y - VY0) / 100) * K, sy = (x: number) => PADT + ((x - VX0) / 100) * K;
  const pts = (poly: P[]) => poly.map(p => `${sx(p[1]).toFixed(1)},${sy(p[0]).toFixed(1)}`).join(' ');
  const s: string[] = [`<clipPath id="clip-${win}"><rect x="${ox}" y="${PADT}" width="${PW}" height="${PH}"/></clipPath><g clip-path="url(#clip-${win})">`, `<rect x="${ox}" y="${PADT}" width="${PW}" height="${PH}" fill="#e9e4da"/>`];
  // ground: walk grid cells
  for (let i = 0; i < NX; i++) for (let j = 0; j < NY; j++) { const c = centre(i, j); if (c[0] < VX0 || c[0] > VX1 || c[1] < VY0 || c[1] > VY1) continue; if (!free(i, j)) s.push(`<rect x="${sx(c[1] - 25)}" y="${sy(c[0] - 25)}" width="${K / 2}" height="${K / 2}" fill="#cfc8bb"/>`); }
  // heat: security sight (red cameras, amber guard, rose patrol share, violet rivals), crowd exposure (blue), quiet (green)
  for (const c of cells) {
    const e = c[win], x = sx(c.p[1] - 50), y = sy(c.p[0] - 50);
    const secNow = c.sec.filter(id => { const wt = watchers.find(q => q.id === id)!; return win === 'pre' ? wt.from < CHIME : wt.to > CHIME; });
    let fill: string;
    if (secNow.some(id => id.startsWith('camera'))) fill = 'rgba(214,40,40,.42)';
    else if (secNow.some(id => id.startsWith('guard'))) fill = 'rgba(232,160,20,.45)';
    else if (c.patrol > 0) fill = `rgba(205,70,120,${(0.12 + 0.4 * c.patrol).toFixed(2)})`;
    else if (secNow.some(id => id.startsWith('rivals'))) fill = 'rgba(122,79,194,.4)';
    else if (secNow.length) fill = 'rgba(90,110,140,.35)';
    else if (e > 0) fill = `rgba(40,90,190,${Math.min(0.55, 0.1 + 0.45 * Math.min(1, e / 60)).toFixed(2)})`;
    else fill = 'rgba(46,150,80,.55)';
    s.push(`<rect x="${x.toFixed(1)}" y="${y.toFixed(1)}" width="${K}" height="${K}" fill="${fill}"/>`);
  }
  for (const o of occs) if (!o.id.startsWith('plot')) s.push(`<polygon points="${pts(o.poly)}" fill="#d8d0c2" stroke="#6d6253" stroke-width="1.5"/>`); else s.push(`<polygon points="${pts(o.poly)}" fill="#bdb5a8" stroke="#6d6253" stroke-width="1"/>`);
  for (const b of block.bays) s.push(`<polygon points="${pts(b.polygon.map((p: number[]) => [p[0]! * 100, p[1]! * 100]))}" fill="none" stroke="#555" stroke-dasharray="4 3"/>`);
  // watchers
  for (const c of site.cameras) { const [x, y] = c.at; const fl = Math.hypot(c.facing[0], c.facing[1]); s.push(`<line x1="${sx(y)}" y1="${sy(x)}" x2="${sx(y + (c.facing[1] / fl) * 400)}" y2="${sy(x + (c.facing[0] / fl) * 400)}" stroke="#a01818" stroke-width="3"/><rect x="${sx(y) - 6}" y="${sy(x) - 6}" width="12" height="12" fill="#d62828" stroke="white" stroke-width="2"/><text x="${sx(y) + 9}" y="${sy(x) + 16}" font-size="12" font-family="Helvetica" fill="#7a1010" stroke="white" stroke-width="3" paint-order="stroke">cam ${c.id}</text>`); }
  for (const c of worldCams) s.push(`<rect x="${sx(c.at[1]) - 5}" y="${sy(c.at[0]) - 5}" width="10" height="10" fill="none" stroke="#d62828" stroke-width="2" stroke-dasharray="2 2"/><text x="${sx(c.at[1]) + 8}" y="${sy(c.at[0]) - 6}" font-size="11" font-family="Helvetica" fill="#7a1010" stroke="white" stroke-width="3" paint-order="stroke">${c.kind} (world only)</text>`);
  s.push(`<circle cx="${sx(post[1])}" cy="${sy(post[0])}" r="7" fill="#e8a014" stroke="white" stroke-width="2"/>`);
  s.push(`<line x1="${sx(pA[1])}" y1="${sy(pA[0])}" x2="${sx(pB[1])}" y2="${sy(pB[0])}" stroke="#b03a6a" stroke-width="4"/><text x="${sx(pB[1]) + 6}" y="${sy(pB[0]) - 6}" font-size="12" font-family="Helvetica" fill="#7a1f45" stroke="white" stroke-width="3" paint-order="stroke">patrol sweep</text>`);
  if (win === 'post') s.push(`<circle cx="${sx(rivalPark[1])}" cy="${sy(rivalPark[0])}" r="7" fill="#7a4fc2" stroke="white" stroke-width="2"/>`);
  // carry paths
  const colours: Record<string, string> = { guard: '#c07800', pickup: '#1d5fa8', cafe: '#2b7a3d' };
  if (carries) for (const r of rows) for (const sId of ['guard', 'pickup', 'cafe']) {
    if (sId === 'cafe' ? r.id !== 'D2' : !['D1', 'K4'].includes(r.id)) continue;
    const v = r.carry[sId]!; if (!v.path) continue;
    s.push(`<polyline points="${pts(v.path)}" fill="none" stroke="${colours[sId]}" stroke-width="2.5" stroke-dasharray="7 4"/>`);
    const mid = along(v.path, plen(v.path) * (sId === 'guard' ? 0.35 : sId === 'pickup' ? 0.7 : 0.5));
    s.push(`<text x="${sx(mid[1]) + 5}" y="${sy(mid[0]) - 4}" font-size="12" font-weight="700" font-family="Helvetica" fill="${colours[sId]}" stroke="white" stroke-width="3" paint-order="stroke">${r.id} ${v.withLiftS.toFixed(0)} s</text>`);
  }
  // sources
  for (const so of sources) s.push(`<circle cx="${sx(so.at[1])}" cy="${sy(so.at[0])}" r="5" fill="${colours[so.id]}" stroke="white" stroke-width="2"/><text x="${sx(so.at[1]) - 6}" y="${sy(so.at[0]) - 9}" text-anchor="end" font-size="13" font-weight="700" font-family="Helvetica" fill="#1d2433" stroke="white" stroke-width="3" paint-order="stroke">${so.name}</text>`);
  // candidates
  for (const r of rows) {
    const x = sx(r.at[1]), y = sy(r.at[0]), col = r.cls === 'kit' ? '#555' : r.cls === 'door' ? '#0b6b6b' : '#111';
    const shape = r.cls === 'door' ? `<rect x="${x - 7}" y="${y - 7}" width="14" height="14" fill="white" stroke="${col}" stroke-width="2.5" stroke-dasharray="${r.exists ? '' : '3 2'}"/>` : r.cls === 'kit' ? `<polygon points="${x},${y - 8} ${x + 8},${y} ${x},${y + 8} ${x - 8},${y}" fill="white" stroke="${col}" stroke-width="2"/>` : `<circle cx="${x}" cy="${y}" r="8" fill="white" stroke="${col}" stroke-width="2.5"/>`;
    s.push(`${shape}<text x="${x + 11}" y="${y + 5}" font-size="13" font-weight="700" font-family="Helvetica" fill="${col}" stroke="white" stroke-width="3" paint-order="stroke">${r.id}</text>`);
  }
  s.push('</g>');
  s.push(`<text x="${ox + 8}" y="${PADT - 12}" font-size="19" font-weight="700" font-family="Helvetica" fill="#1d2433">${title}</text>`);
  // street names
  const label = (t: string, x: number, y: number, rot = 0) => s.push(`<text transform="translate(${sx(y)},${sy(x)}) rotate(${rot})" font-size="13" font-style="italic" font-family="Helvetica" fill="#5d5547" text-anchor="middle">${t}</text>`);
  label('Rocket Lane', 2000, -5600, -90); label('Starline Street', -3150, -3000); label('Orbit Walk', 3750, -100); label('Garden Row', -1500, -250, -90); label('Apron Road', 9000, -3000); label('square', 1800, -2000); label('Records Office', 6400, -3000); label('café', -1700, -2900);
  return s.join('\n');
}
const W = 2 * PW + 60, H = PADT + PH + 180;
const legend = [
  'north up · 9 px/m · 1 m cells · fill = who would see a body lying there: red = a camera cone · amber = the guard at his door (to 5:30) · rose = the patrol sweep (deeper = more of its 100 s cycle)',
  'violet = the parked rivals (from 3:10) · blue = only the crowd and the cast (Inez, the driver) within 13 m on a clear line, deeper = more witness-seconds in the window · green = nobody, all window',
  'circles = recesses (open ground), diamonds = kit pieces in the world (none can hold a body), squares = service doors (dashed: proposed, not in the block)',
  'dashed lines = carry walks at 1.4 m/s from the guard (amber), the pickup (blue), the café door (green), labelled: place, seconds including the 3 s lift',
  'static survey: watchers on their routines with nothing done to them (no loop, no reroute, no lure); crowd = the promoted r016 people stepped by GameCrowd\'s routine rule',
  'occluders = site occluders + cover blockers spanning eye height 1.6 m; the crowd is not counted as blocking sight (the engine\'s CrowdBlocks may), so the blue is an upper bound',
];
const svg = (a: string) => `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}"><rect width="100%" height="100%" fill="#f4efe6"/>${a}${legend.map((l, i) => `<text x="16" y="${PADT + PH + 30 + i * 24}" font-size="15" font-family="Helvetica" fill="#333">${l}</text>`).join('')}</svg>`;
mkdirSync(resolve(ROOT, 'out/vantage013'), { recursive: true });
const tmp = resolve(ROOT, 'out/vantage013/quiet.svg');
writeFileSync(tmp, svg(panel('pre', 10, 'Before the chime (0:00-5:30): takedown window for the guard and the driver', true) + panel('post', 10 + PW + 40, 'After the chime (5:30-8:30): the handover surge leaves by the front door', false)));
execFileSync('rsvg-convert', ['-w', String(Math.round(W)), '-o', resolve(OUT, 'quiet.png'), tmp]);
console.log(resolve(OUT, 'quiet.png'));
