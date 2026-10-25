// The visible world of the site window (brief 055, Splice): the ground layers and every solid the world export places there, rasterised at 0.25 m.
// Placement follows what Unity does with the export (HarbourWorldBuilder, KitPlacer): a civic kit mass stands with its pivot on `kit.at` and its lowest point on `kit.base`,
// a street item with its pivot on `at` at height `z`, a facade with its footprint centre on `kit.at` and its pivot at `base`; kit +X runs along the yaw, kit +Z across it.
// A piece without a measured mesh (a procedural stand-in) stands as its footprint box.
import type { World, Mass, StreetItem, Prim } from '../../contracts/world/world.schema';
import { inPoly, segDist, type V } from '../plan/geom';
import { kitAssets, type PieceData } from './pieces';
import { Raster } from './raster';
import { returnFills } from './returns';
import { slopeAt } from '../world/gen/street/footway';

export type Place = { at: V; yaw: number; scale: [number, number, number]; z0: number };
export type Meta = { id: string; layer: 'civic' | 'street' | 'facade' | 'frontage' | 'building' | 'ground'; kind: string; piece?: string; mesh: boolean; ring: V[]; base?: number; top: number; yaw?: number; place?: Place };
export type Check = { id: string; piece: string; ring: V[]; meshBox: V[]; offset: number };

export type Visible = { R: Raster; meta: Map<string, Meta>; checks: Check[]; ground: { id: string; ring?: V[] }[]; unmeasured: Map<string, number> };

/** KerbKit.TopProud: a kerb stone's top stands this far above the pavement's. */
const TOP_PROUD = 0.004;

const rectRing = (c: V, along: number, across: number, yaw: number): V[] => {
  const cs = Math.cos(yaw), sn = Math.sin(yaw), hw = along / 2, hd = across / 2;
  return ([[-hw, -hd], [hw, -hd], [hw, hd], [-hw, hd]] as V[]).map(([x, y]) => [c[0] + x * cs - y * sn, c[1] + x * sn + y * cs] as V);
};

/** Plan point of kit-frame (ux, uz) for a piece with its pivot at `at`, turned by yaw, scaled (sx, sz). */
const toPlan = (at: V, yaw: number, sx: number, sz: number, ux: number, uz: number): V => {
  const cs = Math.cos(yaw), sn = Math.sin(yaw);
  return [at[0] + ux * sx * cs - uz * sz * sn, at[1] + ux * sx * sn + uz * sz * cs];
};

function distToLine(p: V, line: V[]): number {
  let d = Infinity; for (let i = 0; i + 1 < line.length; i++) d = Math.min(d, segDist(p, line[i]!, line[i + 1]!));
  return d;
}

export function buildVisible(world: World, pieces: Map<string, PieceData>, win: { x0: number; y0: number; x1: number; y1: number }, pad = 6): Visible {
  const R = new Raster(win.x0 - pad, win.y0 - pad, win.x1 + pad, win.y1 + pad);
  const meta = new Map<string, Meta>(), checks: Check[] = [], unmeasured = new Map<string, number>();
  const assets = kitAssets();
  const inWin = (ring: V[]) => ring.some(p => p[0] >= R.x0 && p[0] <= R.x1 && p[1] >= R.y0 && p[1] <= R.y1) || (ring.length > 0 && Math.min(...ring.map(p => p[0])) <= R.x1 && Math.max(...ring.map(p => p[0])) >= R.x0 && Math.min(...ring.map(p => p[1])) <= R.y1 && Math.max(...ring.map(p => p[1])) >= R.y0);

  // ---- ground layers: the carriageways and their kerb strips, foot paths, bays, the plot slabs (cut round the drives and their bays), exactly as Layers.Ground draws them
  const plots = world.ground.plots.filter(p => inWin(p.ring)), paveTop = Math.max(0, ...world.ground.plots.filter(p => p.height < 0.5).map(p => p.base + p.height));
  const roads = world.ground.roads.filter(r => inWin(r.line.map(q => q) as V[]) || r.line.some(q => q[0] >= R.x0 - 20 && q[0] <= R.x1 + 20 && q[1] >= R.y0 - 20 && q[1] <= R.y1 + 20));
  const foot = world.ground.foot.filter(r => r.line.some(q => q[0] >= R.x0 - 20 && q[0] <= R.x1 + 20 && q[1] >= R.y0 - 20 && q[1] <= R.y1 + 20));
  const flats = world.ground.flats.filter(f => inWin(f.ring));
  const fills = returnFills(world); // brief 028: the kerb strip is dropped inside a kerb return's carriageway fill (Layers.Ground)
  const cuts: { ring: V[]; level: number; id: string }[] = [];
  for (const d of world.ground.drives ?? []) cuts.push({ ring: d.ring, level: d.level, id: d.id });
  for (const f of world.ground.flats) if (f.id.includes('/bay/')) cuts.push({ ring: f.ring, level: f.level, id: f.id }); // brief 055: every bay is sunk (Layers.IsBay)
  // brief 061: a continuous footway's ramp (the lane's carriageway rising to the table) and lip (the table's edge falling to the carriageway) are slopes: the floor under a point of either is the line between their edges (the table is a flat, read above)
  const footways = world.ground.footways ?? [];
  const bb = (ring: V[]) => ({ x0: Math.min(...ring.map(p => p[0])), x1: Math.max(...ring.map(p => p[0])), y0: Math.min(...ring.map(p => p[1])), y1: Math.max(...ring.map(p => p[1])) });
  const pb = plots.map(p => ({ p, b: bb(p.ring) })), fb = flats.map(f => ({ f, b: bb(f.ring) })), cb = cuts.map(c => ({ c, b: bb(c.ring) }));
  const inB = (b: { x0: number; x1: number; y0: number; y1: number }, q: V) => q[0] >= b.x0 && q[0] <= b.x1 && q[1] >= b.y0 && q[1] <= b.y1;
  for (let i = 0; i < R.nx; i++) for (let j = 0; j < R.ny; j++) {
    const q = R.centre(i, j); let g = 0, who = 'ground/none';
    for (const r of roads) { // a road's kerb strip runs on to its node, across the carriageway of the road it meets: the builder drops it there (no kerb across a street's mouth, none through a junction box)
      const d = distToLine(q, r.line as V[]), half = r.width / 2;
      if (d <= half) { if (r.level >= g) { g = r.level; who = r.id; } }
      else if (d <= half + r.kerbWidth && !(fills.has && fills.inside(q)) && !roads.some(o => o !== r && distToLine(q, o.line as V[]) < o.width / 2 - 0.01)) { const t = Math.max(r.level + r.kerbHeight, paveTop + TOP_PROUD); if (t > g) { g = t; who = r.id + '/kerb'; } }
    }
    for (const r of foot) if (distToLine(q, r.line as V[]) <= r.width / 2 && r.level > g) { g = r.level; who = r.id; }
    for (const { f, b } of fb) if (inB(b, q) && inPoly(q, f.ring) && f.level > g) { g = f.level; who = f.id; }
    // The lip strip belongs to the drawn slope: the table's flat (ring from the kerb line, read above) lies under it at the table's level, but Unity draws the table from lip.d (FootwayKit.Build), so over the strip the slope wins, not the higher of the two (Aster 2026-10-25). The ramp has no flat under it: the higher floor stands.
    for (const f of footways) {
      const z = slopeAt(f, q); if (z === undefined) continue;
      const s = (q[0] - f.frame.origin[0]) * f.frame.into[0] + (q[1] - f.frame.origin[1]) * f.frame.into[1];
      if (s >= 0 && s <= f.lip.d) { g = z; who = `${f.id}/lip`; } else if (z > g) { g = z; who = `${f.id}/ramp`; }
    }
    let cut: { level: number; id: string } | undefined;
    for (const { c, b } of cb) if (inB(b, q) && inPoly(q, c.ring)) { cut = c; break; }
    if (cut) { if (cut.level > g || who === 'ground/none') { g = cut.level; who = cut.id; } }
    else for (const { p, b } of pb) if (inB(b, q) && inPoly(q, p.ring)) { const t = p.base + p.height; if (t > g) { g = t; who = p.id; } }
    R.ground[R.idx(i, j)] = g; R.groundOwner[R.idx(i, j)] = R.owner(who);
  }

  // ---- stamping
  const stampPiece = (owner: string, piece: PieceData, at: V, yaw: number, scale: [number, number, number], zOrigin: number): V[] => {
    const [sx, sy, sz] = scale, o = R.owner(owner);
    const c = [toPlan(at, yaw, sx, sz, piece.bx0, piece.bz0), toPlan(at, yaw, sx, sz, piece.bx1, piece.bz0), toPlan(at, yaw, sx, sz, piece.bx1, piece.bz1), toPlan(at, yaw, sx, sz, piece.bx0, piece.bz1)];
    const [i0, j0, i1, j1] = R.range(Math.min(...c.map(p => p[0])), Math.min(...c.map(p => p[1])), Math.max(...c.map(p => p[0])), Math.max(...c.map(p => p[1])));
    const cs = Math.cos(yaw), sn = Math.sin(yaw);
    for (let i = i0; i <= i1; i++) for (let j = j0; j <= j1; j++) {
      const q = R.centre(i, j), dx = q[0] - at[0], dy = q[1] - at[1];
      const ux = (dx * cs + dy * sn) / sx, uz = (-dx * sn + dy * cs) / sz;
      if (ux < piece.x0 || ux >= piece.x1 || uz < piece.z0 || uz >= piece.z1) continue;
      const ci = Math.min(piece.nx - 1, Math.floor((ux - piece.x0) / piece.cell)), cj = Math.min(piece.nz - 1, Math.floor((uz - piece.z0) / piece.cell));
      const iv = piece.cells[ci * piece.nz + cj]!;
      for (let m = 0; m < iv.length; m += 2) R.add(i, j, zOrigin + iv[m]! * sy, zOrigin + iv[m + 1]! * sy, o);
    }
    return c;
  };
  const note = (m: Meta) => { meta.set(m.id, m); };
  const unmeas = (id: string) => unmeasured.set(id, (unmeasured.get(id) ?? 0) + 1);

  // civic masses: kit pieces at their pivot, the rest as prisms
  for (const m of world.civic) {
    if (m.hidden) continue;
    if (m.height < 0.02) continue;
    if (m.kit) {
      const pd = pieces.get(m.kit.id), sc = m.kit.scale, yaw = m.kit.yaw ?? 0;
      if (pd) {
        const zo = m.kit.base - pd.minY * sc[1];
        const box = stampPiece(m.id, pd, m.kit.at, yaw, sc, zo);
        const rb = bb(m.ring), mb = bb(box), off = Math.max(Math.abs(rb.x0 - mb.x0), Math.abs(rb.x1 - mb.x1), Math.abs(rb.y0 - mb.y0), Math.abs(rb.y1 - mb.y1));
        checks.push({ id: m.id, piece: m.kit.id, ring: m.ring, meshBox: box, offset: off });
        note({ id: m.id, layer: 'civic', kind: m.kit.id, piece: m.kit.id, mesh: true, ring: m.ring, top: zo + pd.maxY * sc[1], yaw, place: { at: m.kit.at, yaw, scale: sc, z0: zo } });
        continue;
      }
      unmeas(m.kit.id);
    }
    if (m.height < 0.02) continue;
    R.prism(m.ring, m.base, m.base + m.height, m.id);
    note({ id: m.id, layer: 'civic', kind: m.surface, piece: m.kit?.id, mesh: false, ring: m.ring, base: m.base, top: m.base + m.height });
  }
  // buildings (podium, bodies)
  for (const b of world.buildings) if (inWin(b.ring)) { R.prism(b.ring, b.base, b.base + b.height, b.id); note({ id: b.id, layer: 'building', kind: b.surface, mesh: false, ring: b.ring, base: b.base, top: b.base + b.height }); }
  // facade pieces: footprint centre on kit.at
  for (const f of world.facades) {
    if (!inWin([f.kit.at])) continue;
    const pd = pieces.get(f.kit.id), sc = f.kit.scale, yaw = f.kit.yaw ?? 0;
    if (!pd) { unmeas(f.kit.id); continue; }
    const cx = (pd.bx0 + pd.bx1) / 2, cz = (pd.bz0 + pd.bz1) / 2, cs = Math.cos(yaw), sn = Math.sin(yaw);
    const pivot: V = [f.kit.at[0] - (cx * sc[0] * cs - cz * sc[2] * sn), f.kit.at[1] - (cx * sc[0] * sn + cz * sc[2] * cs)];
    const box = stampPiece(f.id, pd, pivot, yaw, sc, f.base);
    note({ id: f.id, layer: 'facade', kind: f.kit.id, piece: f.kit.id, mesh: true, ring: box, top: f.base + pd.maxY * sc[1], yaw, place: { at: pivot, yaw, scale: sc, z0: f.base } });
  }
  // street items
  for (const s of world.street) {
    if (!inWin([s.at])) continue;
    if (s.kind.startsWith('crossing') || s.kind === 'signal-orb') continue;
    stampStreet(s);
  }
  function stampStreet(s: StreetItem) {
    const ring = rectRing(s.at, s.size[0], s.size[1], s.yaw);
    if (s.kit) {
      const pd = pieces.get(s.kit.id), yaw = s.kit.yaw ?? s.yaw;
      if (pd) { const box = stampPiece(s.id, pd, s.at, yaw, s.kit.scale, s.z); note({ id: s.id, layer: 'street', kind: s.kind, piece: s.kit.id, mesh: true, ring: box, top: s.z + pd.maxY * s.kit.scale[1], yaw, place: { at: s.at, yaw, scale: s.kit.scale, z0: s.z } }); return; }
      unmeas(s.kit.id);
    }
    if (s.size[2] < 0.02) return;
    R.prism(ring, s.z, s.z + s.size[2], s.id); note({ id: s.id, layer: 'street', kind: s.kind, piece: s.kit?.id, mesh: false, ring, base: s.z, top: s.z + s.size[2], yaw: s.yaw });
  }
  // frontages: the shopfront boxes the generator states (the procedural shopfront stands as these)
  const prim = (p: Prim, owner: string, kind: string) => {
    if (p.shape !== 'box' || p.size[2] < 0.02) return;
    const ring = rectRing(p.c, p.size[0], p.size[1], p.yaw); if (!inWin(ring)) return;
    R.prism(ring, p.base, p.base + p.size[2], owner); note({ id: owner, layer: 'frontage', kind, mesh: false, ring, base: p.base, top: p.base + p.size[2], yaw: p.yaw });
  };
  for (const f of world.frontages) {
    if (!inWin([f.a, f.b])) continue;
    if (!f.facade) { prim(f.front, f.id + '/front', 'shopfront'); prim(f.entrance, f.id + '/door', 'door'); }
    prim(f.canopy, f.id + '/canopy', 'canopy');
    if (f.service) prim(f.service, f.id + '/service', 'service');
  }
  // ground pieces that stand proud of the slab: kerb stones, tree pits (kit item size = [along, across, height])
  for (const s of world.ground.pieces ?? []) {
    if (!inWin([s.at])) continue;
    if (s.size[2] < 0.03) continue;
    const ring = rectRing(s.at, s.size[0], s.size[1], s.yaw);
    R.prism(ring, s.z, s.z + s.size[2], s.id); note({ id: s.id, layer: 'ground', kind: s.kind, piece: s.kit?.id, mesh: false, ring, base: s.z, top: s.z + s.size[2], yaw: s.yaw });
  }
  void assets;
  R.finish();
  return { R, meta, checks, ground: [], unmeasured };
}

/** The kit ids the window uses (what Blender must measure): civic masses, facades and street items in the window. */
export function kitIdsIn(world: World, win: { x0: number; y0: number; x1: number; y1: number }, pad = 6): string[] {
  const inW = (p: V) => p[0] >= win.x0 - pad && p[0] <= win.x1 + pad && p[1] >= win.y0 - pad && p[1] <= win.y1 + pad;
  const ids = new Set<string>();
  for (const m of world.civic) if (m.kit && !m.hidden && m.height >= 0.02) ids.add(m.kit.id);
  for (const f of world.facades) if (inW(f.kit.at)) ids.add(f.kit.id);
  for (const s of world.street) if (s.kit && inW(s.at) && !s.kind.startsWith('crossing') && s.kind !== 'signal-orb') ids.add(s.kit.id);
  return [...ids];
}
export type { Mass };
