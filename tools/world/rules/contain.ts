// K-6, footprint containment (brief 072, Keel 2026-10-27: `lamplight-south-w/13/k6`, a ShopFill_4 at a 45° acute corner, stood its 2.15 m shop recess out
// through the angled neighbouring wall into the corner forecourt, blocking 4 lane cells of the Handover site). No facade piece's volume may leave its
// building: a piece's front (+Z) stands proud of its own wall by design (eyebrows, pilasters, a pier's legs), but any part of it that is behind its own wall
// line(s) must be inside the building's masses. Fault = a point of a piece volume that is outside every mass of its building spanning the volume's height
// and behind every own wall line (both by more than CONTAIN.tol): it left through another wall.
// Volumes: contracts/world/kit.collide.json (the walker / shot volumes, `thin` frames left out), else the catalogue's bounds (size about pivotOffset).
// Own wall lines: a pier's two ring edges at its corner vertex; every other piece's a→b line (its outward normal), and for a street corner or end piece
// (facades.ts: it wraps the corner) the ring edges at its a and b where they are ring vertices.
// Ground level: a volume in the walker band (pav + 0.35 .. pav + 1.8) is the one that blocks lanes; the check reports it apart but fails on every height.
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { KitAsset } from '../../../contracts/world/kit.schema';
import type { Facade, World } from '../../../contracts/world/world.schema';
import { type V, dot, inPoly, segDist, sub } from '../../plan/geom';
import { ccw, edgeN } from '../gen/geo';
import { adopted } from '../gen/kitrefs';
import { ctxOf as genCtx } from '../gen/common';
import type { Check } from '../../plan/checks';
import { type WCtx, rule } from '../../plan/world-ctx';

export const CONTAIN = { tol: 0.05, step: 0.25, walk: [0.35, 1.8] as [number, number] };

type Vol = { x0: number; x1: number; z0: number; z1: number; y0: number; y1: number };
export type Collide = { pieces: Record<string, { volumes?: any[] }> };
type Mass = { ring: V[]; base: number; top: number };
export type ContainFault = { id: string; building: string; kit: string; form: string; out: number; ground: boolean; at: V };

let COLLIDE: Collide | null = null;
export const collideFile = (): Collide => (COLLIDE ??= JSON.parse(readFileSync(resolve(import.meta.dir, '../../../contracts/world/kit.collide.json'), 'utf8')));

/** A piece's volumes in kit axes (x across, y up, z front, from the pivot): the collide volumes, else the catalogue box. */
export function volumesOf(id: string, a: KitAsset | undefined, col: Collide): Vol[] {
  const vs = col.pieces[id]?.volumes;
  if (vs?.length) return vs.map(v => (v.shape === 'cylinder' ? { x0: v.c[0] - v.r, x1: v.c[0] + v.r, z0: v.c[1] - v.r, z1: v.c[1] + v.r, y0: v.y0, y1: v.y1 } : { x0: v.min[0], x1: v.max[0], z0: v.min[2], z1: v.max[2], y0: v.min[1], y1: v.max[1] }));
  if (!a) return [];
  const [cx, cz] = a.pivotOffset ?? [0, 0];
  return [{ x0: cx - a.size[0] / 2, x1: cx + a.size[0] / 2, z0: cz - a.size[2] / 2, z1: cz + a.size[2] / 2, y0: 0, y1: a.size[1] }];
}

/** The world point of kit point (x, z) of a placed piece (export `at` = footprint centre = pivot + pivotOffset at scale). */
export function placer(f: Facade, a: KitAsset | undefined): (x: number, z: number) => V {
  const yaw = f.kit.yaw ?? 0, [sx, , sz] = f.kit.scale, kx: V = [Math.cos(yaw), Math.sin(yaw)], kz: V = [-Math.sin(yaw), Math.cos(yaw)], [cx, cz] = a?.pivotOffset ?? [0, 0];
  const px = f.kit.at[0] - kx[0] * cx * sx - kz[0] * cz * sz, py = f.kit.at[1] - kx[1] * cx * sx - kz[1] * cz * sz;
  return (x, z) => [px + kx[0] * x * sx + kz[0] * z * sz, py + kx[1] * x * sx + kz[1] * z * sz];
}

const ringDist = (p: V, r: V[]) => { let m = Infinity; for (let i = 0; i < r.length; i++) m = Math.min(m, segDist(p, r[i]!, r[(i + 1) % r.length]!)); return m; };

/** The piece's own wall lines (point, outward normal). */
function ownLines(f: Facade, masses: Mass[]): { p: V; n: V }[] {
  const a = f.a as V, b = f.b as V;
  const atVertex = (v: V) => masses.flatMap(m => m.ring.flatMap((p, i) => { const q = m.ring[(i + 1) % m.ring.length]!; return Math.hypot(p[0] - v[0], p[1] - v[1]) < 0.05 || Math.hypot(q[0] - v[0], q[1] - v[1]) < 0.05 ? [{ p, n: edgeN(p, q) }] : []; }));
  if (Math.hypot(b[0] - a[0], b[1] - a[1]) < 0.01) return atVertex(a);
  // a street corner or end piece wraps the corner at its a or b: the face beyond is its own too
  const wrap = f.form === 'corner' || f.form === 'end' ? [...atVertex(a), ...atVertex(b)] : [];
  let n = (f as any).normal as V | undefined;
  if (!n) { // the outward side: the one whose nudge leaves the building
    const u = sub(b, a), l = Math.hypot(u[0], u[1]), nn: V = [u[1] / l, -u[0] / l], mid: V = [(a[0] + b[0]) / 2 + nn[0] * 0.2, (a[1] + b[1]) / 2 + nn[1] * 0.2];
    n = masses.some(m => inPoly(mid, m.ring)) ? [-nn[0], -nn[1]] : nn;
  }
  return [{ p: a, n }, ...wrap];
}

/** Every facade piece whose volume leaves its building by more than CONTAIN.tol (the worst point per piece). */
export function containFaults(w: World, cat: (id: string) => KitAsset | undefined, pav: number, col: Collide = collideFile()): { checked: number; faults: ContainFault[] } {
  const byB = new Map<string, Mass[]>();
  for (const m of (w as any).buildings as { id: string; ring: V[]; base: number; height: number }[]) {
    if (/\/band-\d+$/.test(m.id)) continue;
    const k = m.id.slice(0, m.id.lastIndexOf('/'));
    (byB.get(k) ?? byB.set(k, []).get(k)!).push({ ring: ccw(m.ring as V[]), base: m.base, top: m.base + m.height });
  }
  const faults: ContainFault[] = [];
  let checked = 0;
  for (const f of w.facades) {
    const ms = byB.get(f.building); if (!ms?.length) continue;
    const a = cat(f.kit.id), vols = volumesOf(f.kit.id, a, col); if (!vols.length) continue;
    checked++;
    const P = placer(f, a), lines = ownLines(f, ms), sy = f.kit.scale[1], st = CONTAIN.step / Math.max(0.2, Math.abs(f.kit.scale[0]));
    let out = 0, grd = false, wp: V = [0, 0];
    for (const v of vols) {
      const lo = f.base + v.y0 * sy, hi = f.base + v.y1 * sy, span = ms.filter(m => m.base < hi - 0.05 && m.top > lo + 0.05);
      if (!span.length) continue;
      const ground = lo < pav + CONTAIN.walk[1] && hi > pav + CONTAIN.walk[0];
      const nx = Math.max(1, Math.ceil((v.x1 - v.x0) / st)), nz = Math.max(1, Math.ceil((v.z1 - v.z0) / CONTAIN.step));
      for (let i = 0; i <= nx; i++) for (let j = 0; j <= nz; j++) {
        const p = P(v.x0 + ((v.x1 - v.x0) * i) / nx, v.z0 + ((v.z1 - v.z0) * j) / nz);
        if (lines.length && !lines.every(l => dot(sub(p, l.p), l.n) < -CONTAIN.tol)) continue; // in front of an own wall: stands proud by design
        if (span.some(m => inPoly(p, m.ring))) continue;
        const d = Math.min(...span.map(m => ringDist(p, m.ring)));
        if (d <= CONTAIN.tol) continue;
        grd ||= ground;
        if (d > out) { out = d; wp = p; }
      }
    }
    const worst: ContainFault | null = out ? { id: f.id, building: f.building, kit: f.kit.id, form: f.form, out: Math.round(out * 1000) / 1000, ground: grd, at: [Math.round(wp[0] * 100) / 100, Math.round(wp[1] * 100) / 100] } : null;
    if (worst) faults.push(worst);
  }
  return { checked, faults };
}

const SOURCE = 'brief 072 (Keel 2026-10-27: lamplight-south-w/13/k6, a ShopFill_4 at a 45° corner, stood 2.15 m out through SideBay_4 k7 into the corner forecourt and blocked 4 lane cells of the Handover site) · '
  + 'contracts/world/kit.collide.json volumes, else kit.json size and pivotOffset · tools/world/gen/faces.ts (pull-back at acute corners), facades.ts (bay body FIT) · '
  + 'failing examples (contain.test.ts): that ShopFill_4 put back at the corner (2.15 m out); a SideBayStack_Mid run to an 80° corner (0.06 m out); a street bay pushed 0.5 m deeper than its plot';

/** K-6: no facade piece leaves its building (the world rule; `bun tools/plan/cli.ts` and `bun run world` run it). */
export function containRules(c: WCtx): Check[] {
  const by = new Map(adopted().map(a => [a.id, a])), r = containFaults(c.w, id => by.get(id), genCtx(c.plan, c.d, c.civ).pav);
  const ground = r.faults.filter(f => f.ground).length;
  return [rule('K-6', 'Kit', `No facade piece's volume leaves its building: every part behind its own wall line stands inside the building's masses (within ${CONTAIN.tol} m); faults ${r.faults.length}, at ground level ${ground}`,
    r.checked, r.faults.sort((a, b) => b.out - a.out).map(f => `${f.id} ${f.kit} ${f.out} m out at ${f.at.join(',')}${f.ground ? ' (ground)' : ''}`), `≤ ${CONTAIN.tol} m`, SOURCE)];
}
