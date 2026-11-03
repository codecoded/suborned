// OV-1, no intersecting buildings (brief 075, Sky 2026-10-29: "nowhere near as good as original"; in the Skyport/harbour view a curved streamline end ran into
// the side of the neighbouring block). Two different buildings never occupy the same space: no building mass inside another building's mass, no facade piece's
// volume inside another building's mass, no facade piece's volume inside a facade piece of another building. Touching is fine (party walls meet on one line,
// a front's eyebrows meet its neighbour's at the joint); a fault is a penetration deeper than OVERLAP.tol in plan with the two spans overlapping in height.
// Buildings: `buildings[]` masses grouped by host (colourways.ts hostOf: /body, /wing-n …), the civic block's visible masses as one building (`civic`), each
// megastructure's parts as one building. Pieces: `facades[]` grouped by `building`; their volumes are contracts/world/kit.collide.json's solid volumes and its
// thin frames (eyebrows, slabs, canopies: open to a walker, solid to the eye), else the catalogue box (contain.ts volumesOf, placer). A voxel volume's edge may
// stand up to one kit.collide.json `cell` proud of the mesh, so a piece's tolerance adds its cell at its scale. The Handover's protected buildings (plan
// standards.blocks.exempt `handover:`, the civic block) are counted among themselves, not failed: the zone stays as it is (ZK-1).
import type { KitAsset } from '../../../contracts/world/kit.schema';
import type { World } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import { type V, inPoly, segDist } from '../../plan/geom';
import { type WCtx, rule } from '../../plan/world-ctx';
import { hostOf } from '../gen/colourways';
import { adopted } from '../gen/kitrefs';
import { type Collide, collideFile, placer, volumesOf } from './contain';

export const OVERLAP = {
  /** m: a penetration up to this is a touch (the export rounds to 1 mm; scaled pieces at a joint meet within a few cm) */
  tol: 0.1,
  /** m: the sampling step across a mass or a volume */
  step: 0.5,
  /** m: two spans overlap in height when they share more than this */
  zTol: 0.05,
};

type B4 = [number, number, number, number]; // x0 x1 y0 y1
type Solid = { host: string; id: string; ring: V[]; z0: number; z1: number; bb: B4 };
type Box = { host: string; id: string; kit: string; c: [V, V, V, V]; z0: number; z1: number; bb: B4; /** m: the volume's voxel cell at the piece's scale (its edges may stand up to one cell proud of the mesh) */ cell: number };
export type OverlapFault = { kind: 'mass-mass' | 'piece-mass' | 'piece-piece'; a: string; b: string; hosts: [string, string]; depth: number; at: V };

const bbOf = (r: V[]): B4 => r.reduce<B4>((b, p) => [Math.min(b[0], p[0]), Math.max(b[1], p[0]), Math.min(b[2], p[1]), Math.max(b[3], p[1])], [Infinity, -Infinity, Infinity, -Infinity]);
const bbMeet = (a: B4, b: B4, g = 0) => a[0] < b[1] - g && b[0] < a[1] - g && a[2] < b[3] - g && b[2] < a[3] - g;
const ringDist = (p: V, r: V[]) => { let m = Infinity; for (let i = 0; i < r.length; i++) m = Math.min(m, segDist(p, r[i]!, r[(i + 1) % r.length]!)); return m; };
const zMeet = (a: { z0: number; z1: number }, b: { z0: number; z1: number }) => Math.min(a.z1, b.z1) - Math.max(a.z0, b.z0) > OVERLAP.zTol;
const r2 = (x: number) => Math.round(x * 100) / 100;

/** The building solids of a world: masses by host, the civic block as one, each megastructure as one. */
export function solidsOf(w: World): Solid[] {
  const out: Solid[] = [];
  const add = (host: string, id: string, ring: V[], base: number, h: number) => { if (ring?.length >= 3 && h > 0) out.push({ host, id, ring, z0: base, z1: base + h, bb: bbOf(ring) }); };
  for (const m of w.buildings as any[]) add(hostOf(m.id), m.id, m.ring, m.base, m.height);
  for (const m of (w.civic ?? []) as any[]) if (!m.hidden && m.ring) add('civic', m.id, m.ring, m.base ?? 0, m.height ?? 0);
  for (const g of (w.megastructures ?? []) as any[]) for (const p of g.parts ?? []) add(g.id, p.id, p.ring, p.base ?? 0, p.height ?? 0);
  return out;
}

/** The facade pieces' solid volumes as plan boxes (4 corners, height span). */
export function boxesOf(w: World, cat: (id: string) => KitAsset | undefined, col: Collide = collideFile()): Box[] {
  const out: Box[] = [];
  for (const f of w.facades) {
    const a = cat(f.kit.id), vols = volumesOf(f.kit.id, a, col), pc = col.pieces[f.kit.id] as { volumes?: any[]; thin?: any[]; cell?: number } | undefined;
    if (!vols.length) continue;
    const P = placer(f, a), sy = f.kit.scale[1], cell = pc?.volumes?.length ? Number(pc.cell ?? 0) * Math.max(Math.abs(f.kit.scale[0]), Math.abs(f.kit.scale[2])) : 0;
    const push = (v: { x0: number; x1: number; z0: number; z1: number; y0: number; y1: number }, cl: number) => {
      const c: [V, V, V, V] = [P(v.x0, v.z0), P(v.x1, v.z0), P(v.x1, v.z1), P(v.x0, v.z1)];
      out.push({ host: f.building, id: f.id, kit: f.kit.id, c, z0: f.base + v.y0 * sy, z1: f.base + v.y1 * sy, bb: bbOf(c), cell: cl });
    };
    for (const v of vols) push(v, cell);
    // the thin frames (eyebrows, slabs, canopies: measured boxes, no voxel cell) are open to a walker but solid to the eye: a ribbon eyebrow wrapping a
    // streamline end runs into a neighbour's wall as visibly as the body does
    for (const t of pc?.thin ?? []) if (t.shape === 'box') push({ x0: t.min[0], x1: t.max[0], z0: t.min[2], z1: t.max[2], y0: t.min[1], y1: t.max[1] }, 0);
  }
  return out;
}

/** Penetration depth of two convex quads (separating axes: the least overlap over the four edge normals; ≤ 0 = apart). */
export function quadDepth(p: V[], q: V[]): number {
  let d = Infinity;
  for (const poly of [p, q]) for (let i = 0; i < poly.length; i++) {
    const a = poly[i]!, b = poly[(i + 1) % poly.length]!, l = Math.hypot(b[0] - a[0], b[1] - a[1]);
    if (l < 1e-9) continue;
    const n: V = [-(b[1] - a[1]) / l, (b[0] - a[0]) / l];
    const pr = (s: V[]) => s.reduce<[number, number]>((m, v) => { const t = v[0] * n[0] + v[1] * n[1]; return [Math.min(m[0], t), Math.max(m[1], t)]; }, [Infinity, -Infinity]);
    const [p0, p1] = pr(p), [q0, q1] = pr(q);
    d = Math.min(d, Math.min(p1, q1) - Math.max(p0, q0));
    if (d <= 0) return d;
  }
  return d;
}

/** Sample points of a convex quad (its corners, edges and inside, OVERLAP.step apart). */
function quadSamples(c: V[]): V[] {
  const [o, x, , y] = c as [V, V, V, V], ux: V = [x[0] - o[0], x[1] - o[1]], uy: V = [y[0] - o[0], y[1] - o[1]];
  const nx = Math.max(1, Math.ceil(Math.hypot(ux[0], ux[1]) / OVERLAP.step)), ny = Math.max(1, Math.ceil(Math.hypot(uy[0], uy[1]) / OVERLAP.step)), out: V[] = [];
  for (let i = 0; i <= nx; i++) for (let j = 0; j <= ny; j++) out.push([o[0] + (ux[0] * i) / nx + (uy[0] * j) / ny, o[1] + (ux[1] * i) / nx + (uy[1] * j) / ny]);
  return out;
}
/** Sample points of a ring's inside (a grid over its box, OVERLAP.step apart, the cell centres inside it). */
function ringSamples(r: V[], bb: B4): V[] {
  const out: V[] = [];
  for (let x = bb[0] + OVERLAP.step / 2; x < bb[1]; x += OVERLAP.step) for (let y = bb[2] + OVERLAP.step / 2; y < bb[3]; y += OVERLAP.step) if (inPoly([x, y], r)) out.push([x, y]);
  return out;
}

/** The deepest point of `pts` inside ring `r` (depth = distance to its edge), or 0. */
function deepest(pts: V[], r: V[], bb: B4): { d: number; at: V } {
  let d = 0, at: V = [0, 0];
  for (const p of pts) {
    if (p[0] <= bb[0] || p[0] >= bb[1] || p[1] <= bb[2] || p[1] >= bb[3] || !inPoly(p, r)) continue;
    const e = ringDist(p, r);
    if (e > d) { d = e; at = p; }
  }
  return { d, at };
}

/** A uniform grid over boxes for the pair search. */
function grid<T extends { bb: B4 }>(xs: T[], cell = 16) {
  const g = new Map<string, number[]>();
  xs.forEach((x, i) => { for (let a = Math.floor(x.bb[0] / cell); a <= Math.floor(x.bb[1] / cell); a++) for (let b = Math.floor(x.bb[2] / cell); b <= Math.floor(x.bb[3] / cell); b++) (g.get(`${a},${b}`) ?? g.set(`${a},${b}`, []).get(`${a},${b}`)!).push(i); });
  return (bb: B4): Set<number> => { const s = new Set<number>(); for (let a = Math.floor(bb[0] / cell); a <= Math.floor(bb[1] / cell); a++) for (let b = Math.floor(bb[2] / cell); b <= Math.floor(bb[3] / cell); b++) for (const i of g.get(`${a},${b}`) ?? []) s.add(i); return s; };
}

/** Mass in mass: every pair of solids of two different hosts whose plan rings interpenetrate deeper than OVERLAP.tol with their height spans overlapping
 *  (each side's inside sampled against the other's ring; one fault per pair, its deepest point). */
export function massOverlaps(S: Solid[], nearS = grid(S)): OverlapFault[] {
  const out: OverlapFault[] = [], ss = new Set<string>(), tol = OVERLAP.tol;
  S.forEach((a, i) => {
    for (const j of nearS(a.bb)) {
      const b = S[j]!;
      if (j <= i || a.host === b.host || !bbMeet(a.bb, b.bb, tol) || !zMeet(a, b)) continue;
      const k = `${a.id}|${b.id}`; if (ss.has(k)) continue; ss.add(k);
      const x = deepest(ringSamples(a.ring, a.bb), b.ring, b.bb), y = deepest(ringSamples(b.ring, b.bb), a.ring, a.bb), m = x.d >= y.d ? x : y;
      if (m.d > tol) out.push({ kind: 'mass-mass', a: a.id, b: b.id, hosts: [a.host, b.host], depth: r2(m.d), at: [r2(m.at[0]), r2(m.at[1])] });
    }
  });
  return out;
}

/** OV-1 on footprints alone (brief 075 remap step 2: the old-built scene's buildings, suborned.oldbuilt/1, tools/world/oldmatch): masses by `host`. */
export function footprintOverlaps(xs: { host: string; id: string; ring: V[]; base: number; height: number }[]): OverlapFault[] {
  const S: Solid[] = xs.filter(x => x.ring?.length >= 3 && x.height > 0).map(x => ({ host: x.host, id: x.id, ring: x.ring, z0: x.base, z1: x.base + x.height, bb: bbOf(x.ring) }));
  return massOverlaps(S).sort((p, q) => q.depth - p.depth || (p.a < q.a ? -1 : 1));
}

/** Every intersection between two different buildings (one fault per pair of elements, its deepest point). */
export function overlapFaults(w: World, cat: (id: string) => KitAsset | undefined, col: Collide = collideFile()): { checked: number; faults: OverlapFault[] } {
  const S = solidsOf(w), Bx = boxesOf(w, cat, col), faults: OverlapFault[] = [], tol = OVERLAP.tol;
  const nearS = grid(S), nearB = grid(Bx);
  faults.push(...massOverlaps(S, nearS));
  // piece in another building's mass (worst volume per piece and mass)
  const pm = new Map<string, OverlapFault>();
  for (const v of Bx) for (const j of nearS(v.bb)) {
    const s = S[j]!;
    if (s.host === v.host || !bbMeet(v.bb, s.bb, tol) || !zMeet(v, s)) continue;
    const m = deepest(quadSamples(v.c), s.ring, s.bb);
    if (m.d <= tol + v.cell) continue;
    const k = `${v.id}|${s.id}`, was = pm.get(k);
    if (!was || was.depth < m.d) pm.set(k, { kind: 'piece-mass', a: v.id, b: s.id, hosts: [v.host, s.host], depth: r2(m.d), at: [r2(m.at[0]), r2(m.at[1])] });
  }
  faults.push(...pm.values());
  // piece in another building's piece (worst volume pair per piece pair)
  const pp = new Map<string, OverlapFault>();
  Bx.forEach((v, i) => {
    for (const j of nearB(v.bb)) {
      const u = Bx[j]!;
      if (j <= i || u.host === v.host || !bbMeet(v.bb, u.bb, tol) || !zMeet(v, u)) continue;
      const d = quadDepth(v.c, u.c);
      if (d <= tol + v.cell + u.cell) continue;
      const [a, b] = v.id < u.id ? [v, u] : [u, v], k = `${a.id}|${b.id}`, was = pp.get(k);
      const at: V = [r2((v.bb[0] + v.bb[1] + u.bb[0] + u.bb[1]) / 4), r2((v.bb[2] + v.bb[3] + u.bb[2] + u.bb[3]) / 4)];
      if (!was || was.depth < d) pp.set(k, { kind: 'piece-piece', a: a.id, b: b.id, hosts: [a.host, b.host], depth: r2(d), at });
    }
  });
  faults.push(...pp.values());
  faults.sort((p, q) => q.depth - p.depth || (p.a < q.a ? -1 : p.a > q.a ? 1 : p.b < q.b ? -1 : 1));
  return { checked: S.length + w.facades.length, faults };
}

/** The building pairs that intersect (each once, sorted), from the faults. */
export const overlapPairs = (f: OverlapFault[]) => [...new Set(f.map(x => [...x.hosts].sort().join(' | ')))].sort();

/** The Handover's protected buildings (plan standards.blocks.exempt, a reason starting `handover:`; the civic block): their faults among themselves are
 *  reported, never failed (brief 075, Sky 2026-10-28: the Handover zone stays as it is; ZK-1 pins its pieces). A fault with any other building fails. */
export function handoverKept(plan: { standards?: any }): (host: string) => boolean {
  const ex = (plan.standards?.blocks?.exempt ?? {}) as Record<string, string>;
  return host => host === 'civic' || (host.startsWith('bld/') && String(ex[host.split('/')[1]!] ?? '').startsWith('handover'));
}

const SOURCE = 'brief 075 (Sky 2026-10-29: "nowhere near as good as original"; a curved streamline end ran into the side of the neighbouring block in the Skyport/harbour view) · '
  + 'world buildings[] (hosts: colourways.ts hostOf), civic (one building), megastructures[].parts, facades[] volumes and thin frames from contracts/world/kit.collide.json, else kit.json size · '
  + 'tools/world/gen/oldblocks.ts OLD_BLOCK.intoWalls (an end rounds off only into open ground) · '
  + 'failing examples (overlap.test.ts): a mass pushed 2 m into its neighbour; a StreamEnd rounding off into its lower neighbour\'s wall (the old Bays rule put back); two buildings\' bays laid over each other';

/** The plan version OV-1 is enforced from: the remapped old town (Datum's 0.5.x; its frontage ends fixed with OLD_BLOCK.intoWalls). A plan written before it (the pre-remap
 *  base 0.4.0 the census and failing-example tests pin, the A04 skyline fixtures) was built by today's grammar, whose towers and corner piers stand into their
 *  neighbours, so OV-1 reports there (WAIVE) and those plans' other rules keep their failing examples (reach.ts RCH_SINCE does the same). */
export const OV_SINCE = '0.5.2';
const older = (v: unknown, than: string) => { const a = String(v ?? '0').split(/[.-]/).map(Number), b = than.split('.').map(Number); for (let i = 0; i < b.length; i++) { const x = Number.isFinite(a[i]) ? a[i]! : 0; if (x !== b[i]) return x < b[i]!; } return false; };

/** OV-1: no two buildings intersect (masses and facade pieces); the Handover's protected buildings among themselves are counted, not failed; informational (WAIVE) on a plan before OV_SINCE. */
export function overlapRules(c: WCtx): Check[] {
  const by = new Map(adopted().map(a => [a.id, a])), r = overlapFaults(c.w, id => by.get(id)), kept = handoverKept(c.plan);
  const prot = r.faults.filter(f => kept(f.hosts[0]) && kept(f.hosts[1])), bad = r.faults.filter(f => !(kept(f.hosts[0]) && kept(f.hosts[1]))), pairs = overlapPairs(bad);
  const n = (k: OverlapFault['kind']) => bad.filter(f => f.kind === k).length;
  const k = [rule('OV-1', 'Kit', `No two buildings intersect: no mass in another building's mass, no facade piece (body or eyebrow) in another building's mass or piece (penetration over ${OVERLAP.tol} m `
    + `plus the volume's voxel cell); faults ${bad.length} (mass-mass ${n('mass-mass')}, piece-mass ${n('piece-mass')}, piece-piece ${n('piece-piece')}) between ${pairs.length} building pairs; `
    + `the Handover's protected buildings among themselves ${prot.length} (${overlapPairs(prot).length} pairs; reported, not failed)`,
  r.checked, bad.map(f => `${f.kind} ${f.a} × ${f.b} ${f.depth} m at ${f.at.join(',')}`), `≤ ${OVERLAP.tol} m`, SOURCE)];
  if (!k[0]!.ok && older(c.plan.version, OV_SINCE)) return [{ ...k[0]!, ok: true, waived: `informational on a plan before ${OV_SINCE} (today's grammar)`, detail: `WAIVED (informational: plan ${String(c.plan.version)} predates OV-1's ${OV_SINCE}); ${k[0]!.detail}` }];
  return k;
}
