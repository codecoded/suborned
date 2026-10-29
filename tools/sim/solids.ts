// The drawn solids as the engine walks, stands and sees against them (brief 055 steps 1-2, Keel): every piece the world places in the Handover's window, with the
// volumes contracts/world/kit.collide.json declares for its kit piece (Vela), placed as Unity places it (the same placement tools/geometry/visible.ts ports from
// HarbourWorldBuilder/KitPlacer), plus the drawn boxes that have no kit piece (police posts, the dumpster, the shopfront fronts and doors). Block frame, metres; heights
// in block z (the pavement, standards.levels.datum.pavement, is 0; the carriageway -0.18).
//
//   floor   a height per walk cell: the ground layer drawn there (carriageway, kerb, bay, drive, plot slab, as Layers.Ground lays them), or the floor the civic block
//           declares (civic-block.json floors: the Office's public landing and its six steps). Never found in the kit (standards.levels.datum.rule).
//   walk    a cell is blocked when its centre is within the plan's body clearance (standards.walking.body.r) of a volume that blocks a walker at the cell's floor f:
//           z0 < f + HEAD and z1 > f + STEP (kit.collide.json `rule`). A `stand` volume whose top is the floor declared under it (the landing, a tread) is that floor,
//           not an obstacle. This is added to the grid tools/plan/walk-grid.ts makes (buildings, plots, the block's own ground solids), never instead of it.
//   sight   an occluder is a drawn volume that spans the engine's eye height (rulebook eye-cm) on a piece that stops sight: a street item the world marks `sight:
//           "blocks"` (glazed and open items do not), every civic kit piece, shopfront and facade. Lower volumes are not blockers: they lower a hit chance through
//           the cover file (contracts/world/civic-block.cover.json, Pulse), as the fight rules already say.
//   render-only  brief 077 (Sky 2026-10-30, "yeah, see through"): a kit piece whose kit.collide.json entry declares `volumes: []` (planting: the civic beds'
//           compositions, the troughs' and roofs' companions) stands no solid: it never blocks a walker or a sightline, and the grid's digest never names it.
//           Its stonework is a separate piece with its own volumes. A piece with no entry at all is not render-only: it is unmeasured (a civic mass still stands as its
//           prism, as before, but is listed, and the site refuses to build with any), and an entry with no volumes list is an error. Never guessed either way.
//
// Deterministic from the plan, the civic block, the generated world (tools/plan/world-ctx.ts generated, cached) and kit.collide.json.
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import type { World } from '../../contracts/world/world.schema';
import { floorAt, toPlanFn, type Civic } from '../plan/civic';
import { inPoly, segDist, type V } from '../plan/geom';
import type { Derived, Plan } from '../plan/plan';
import { bodyOf } from '../plan/walk-solids';
import { toBlockFn, walkGrid, type Window, type WalkGrid, CELL, WINDOW } from '../plan/walk-grid';
import { generated } from '../plan/world-ctx';
import { returnFills } from '../geometry/returns';
import { cameraPoles } from '../missions/handover/legs';

const root = join(import.meta.dir, '..', '..');
export const COLLIDE_FILE = 'contracts/world/kit.collide.json';
/** kit.collide.json's rule: a walker needs HEAD of room over the floor and climbs at most STEP. */
export const HEAD = 1.8, STEP = 0.35;
/** A stand top within this of the floor declared under it is that floor. */
const FLUSH = 0.05;
/** A piece no wider than this (its eye-height volumes' box diagonal) stands in sight as one convex outline: a lamp, a booth, a palm, a cabinet. */
const COMPACT = 3;
/** Engine 0.30.0: an element narrower than this both ways at eye height is no sight occluder (the cover rule's 0.6 m: a face that narrow hides nobody). */
const THIN = 0.6;
/** Engine 0.30.0: how far above the eye line a volume must reach to stop sight (m). */
const GRAZE = 0.1;
/** Engine 0.30.0 (brief 055; Vantage 2026-10-24: Inez's centre passed 10 cm from the café front, her shoulder sank into the wall): a cell whose centre is within
 *  this of a building's face is blocked, so a walker's body keeps off walls (half a drawn body's shoulder width, 0.25 m, and a few cm). Was 0: the centre only. */
const FACE = 0.3;

type Box = { shape: 'box'; min: [number, number, number]; max: [number, number, number]; stand?: boolean };
type Cyl = { shape: 'cylinder'; c: [number, number]; r: number; y0: number; y1: number; stand?: boolean };
type Vol = Box | Cyl;
type Piece = { sha256: string; bounds: [[number, number, number], [number, number, number]]; class: string; volumes: Vol[] };
export type Collide = { schema: string; frame?: string; rule?: string; pieces: Record<string, Piece> };

/** A placed volume, block frame. `poly` is the footprint (a rectangle, or a 16-gon round a cylinder) for sight; `circle` keeps a cylinder exact for the walk. */
export type Solid = {
  id: string; piece?: string; poly: V[]; circle?: { c: V; r: number }; z0: number; z1: number; stand: boolean; sight: 'blocks' | 'glazed' | 'open';
  /** A building's face (a civic building and its ground storey, a facade piece, a shopfront box): blocked within FACE (0.3 m) of the cell centre (engine 0.30.0; was the centre only); every other solid keeps the body clearance. */
  face: boolean;
  x0: number; x1: number; y0: number; y1: number;
};

export type Drawn = {
  solids: Solid[]; elements: number; volumes: number; unmeasured: string[];
  /** The pieces placed in the window that declare no volumes (render-only, brief 077): drawn, but no solid. */
  renderOnly: string[];
  /** The digest sites built before brief 077 carry (grid.drawn without `digest`): every piece placed, render-only ones included. */
  collideSha: string;
  /** Brief 077: the digest of the pieces that stand at least one solid in the window (and the camera pole). Render-only pieces never reach it. */
  solidSha: string;
};

const sha = (t: string) => new Bun.CryptoHasher('sha256').update(t).digest('hex');
/** The kit piece the mission cameras' poles stand as (missionPoles, tools/missions/handover/legs.ts cameraPoles): on the same grid, so in the digest. */
const POLE_PIECE = 'CCTVPole';
/** Brief 072 (Keel): what of kit.collide.json this grid reads: its schema, frame and rule, and the bounds and volumes of the pieces placed in the window (and the
 *  camera pole), pieces sorted by name. Was the whole file's sha256, so a piece added or re-measured anywhere in the city (a street filler outside the window, a
 *  version bump) changed grid.collide, the site's bytes and every recorded run's inputs while the ground stood as it was. */
export function collideDigest(collide: { schema: string; frame?: string; rule?: string; pieces: Record<string, Piece> }, kits: string[]): string {
  const pieces = Object.fromEntries([...new Set(kits)].filter((k) => collide.pieces[k]).sort().map((k) => [k, { bounds: collide.pieces[k]!.bounds, volumes: collide.pieces[k]!.volumes }]));
  return sha(JSON.stringify({ schema: collide.schema, frame: collide.frame ?? '', rule: collide.rule ?? '', pieces }));
}
/** Brief 077 (Keel; Sky 2026-10-30: planting is see-through): the digest of what of kit.collide.json can move the walk or the sight. The same fields as
 *  collideDigest, read only for the pieces given that declare at least one volume: a render-only piece (`volumes: []`) adds nothing, so planting may come or go
 *  without touching it, while any volume (or the bounds that place it) of a piece that stands a solid still does. Callers pass the pieces that stood a solid in
 *  the window (drawnSolids `solidSha`), so a piece wholly outside it is not named either. */
export function solidDigest(collide: { schema: string; frame?: string; rule?: string; pieces: Record<string, Piece> }, kits: string[]): string {
  return collideDigest(collide, kits.filter((k) => (collide.pieces[k]?.volumes.length ?? 0) > 0));
}

/** Every drawn solid the world stands in (or within `pad` of) the window. */
/** `collideIn`: a kit.collide.json to read instead of the file (tests; brief 077's adoption probe). */
export function drawnSolids(plan: Plan, civ: Civic, w: World, win: Window = WINDOW, pad = 2, collideIn?: Collide): Drawn {
  const collide = collideIn ?? (JSON.parse(readFileSync(join(root, COLLIDE_FILE), 'utf8')) as Collide);
  if (collide.schema !== 'suborned.kit.collide/1') throw new Error(`${COLLIDE_FILE} is not suborned.kit.collide/1`);
  // brief 077: render-only is declared (`volumes: []`), never inferred; an entry without a volumes list is neither a solid nor render-only
  for (const [k, p] of Object.entries(collide.pieces)) if (!Array.isArray(p.volumes)) throw new Error(`${COLLIDE_FILE} pieces.${k} has no volumes list: declare [] for a render-only piece, or measure it (bun run collide)`);
  const toBlock = toBlockFn(plan.civic), pave = pavementOf(plan);
  const inWin = (b: V) => b[0] >= win.x0 - pad && b[0] <= win.x1 + pad && b[1] >= win.y0 - pad && b[1] <= win.y1 + pad;
  const solids: Solid[] = [], unmeasured = new Set<string>(), placed = new Set<string>(), renderOnly = new Set<string>();
  let elements = 0;
  const push = (s: Omit<Solid, 'x0' | 'x1' | 'y0' | 'y1'>) => {
    if (!s.poly.some(inWin)) return;
    const xs = s.poly.map((p) => p[0]), ys = s.poly.map((p) => p[1]);
    solids.push({ ...s, x0: Math.min(...xs), x1: Math.max(...xs), y0: Math.min(...ys), y1: Math.max(...ys) });
  };
  /** Kit-frame (ux, uz) of a piece with its pivot at `at` (plan), yaw, scale → block frame (as tools/geometry/visible.ts toPlan, then the block frame). */
  const at = (pivot: V, yaw: number, sx: number, sz: number) => { const cs = Math.cos(yaw), sn = Math.sin(yaw); return (ux: number, uz: number): V => toBlock([pivot[0] + ux * sx * cs - uz * sz * sn, pivot[1] + ux * sx * sn + uz * sz * cs]); };
  const rect = (c: V, along: number, across: number, yaw: number): V[] => {
    const cs = Math.cos(yaw), sn = Math.sin(yaw), hw = along / 2, hd = across / 2;
    return ([[-hw, -hd], [hw, -hd], [hw, hd], [-hw, hd]] as V[]).map(([x, y]) => toBlock([c[0] + x * cs - y * sn, c[1] + x * sn + y * cs]));
  };
  const placePiece = (id: string, kit: string, pivot: V, yaw: number, scale: [number, number, number], zOrigin: number, sight: Solid['sight'], face = false): boolean => {
    const p = collide.pieces[kit]; if (!p) { unmeasured.add(kit); return false; }
    placed.add(kit);
    elements++;
    if (p.volumes.length === 0) { renderOnly.add(kit); return true; } // brief 077: drawn, no solid (see the head)
    const [sx, sy, sz] = scale, T = at(pivot, yaw, sx, sz);
    for (const v of p.volumes) {
      if (v.shape === 'box') {
        const poly = [T(v.min[0], v.min[2]), T(v.max[0], v.min[2]), T(v.max[0], v.max[2]), T(v.min[0], v.max[2])];
        push({ id, piece: kit, poly, z0: zOrigin + v.min[1] * sy - pave, z1: zOrigin + v.max[1] * sy - pave, stand: !!v.stand, sight, face });
      } else {
        const c = T(v.c[0], v.c[1]), r = v.r * Math.max(sx, sz);
        const poly = Array.from({ length: 16 }, (_, k): V => [c[0] + r * Math.cos((k * Math.PI) / 8), c[1] + r * Math.sin((k * Math.PI) / 8)]);
        push({ id, piece: kit, poly, circle: { c, r }, z0: zOrigin + v.y0 * sy - pave, z1: zOrigin + v.y1 * sy - pave, stand: !!v.stand, sight, face });
      }
    }
    return true;
  };
  const prism = (id: string, poly: V[], base: number, height: number, sight: Solid['sight'], face = false) => { elements++; push({ id, poly, z0: base - pave, z1: base + height - pave, stand: false, sight, face }); };

  // the civic block's pieces (KitPlacer: pivot on kit.at, lowest point on kit.base); a mass with no kit piece stands as its prism. Hidden masses draw nothing.
  // A civic building, its towers, its ground storey (`groundBy`) and its interior are faces; the terrace, the entrances and the dressing are ground solids.
  const faces = new Set<string>([...civ.buildings.flatMap((b) => [`civic/${b.id}`, ...(b.towers ?? []).map((t) => `civic/${b.id}/${t.id}`)]), ...w.civic.flatMap((m) => (m.groundBy ? [m.groundBy] : [])), ...w.civic.filter((m) => m.id.endsWith('/interior')).map((m) => m.id)]);
  const buildingOf = new Map(civ.buildings.map((b) => [`civic/${b.id}`, b]));
  for (const m of w.civic) {
    if (m.hidden || m.height < 0.02) continue;
    const face = faces.has(m.id);
    if (m.kit) {
      const p = collide.pieces[m.kit.id], from = solids.length;
      if (!p) unmeasured.add(m.kit.id); // brief 077: no entry is not render-only; it still stands as its prism below, but is named (the site refuses it)
      if (p && placePiece(m.id, m.kit.id, m.kit.at, m.kit.yaw ?? 0, m.kit.scale, m.kit.base - p.bounds[0][1] * m.kit.scale[1], 'blocks', face)) {
        // brief 072 (Keel): a civic building's walls are the block's footprint and its towers' (civic-block.json polygons), as the block-only grid and the site's
        // sight have them; its kit piece's measured volumes add only what stands proud of them (the Office's pilasters, its step). Was every kit volume: the
        // RecordsTower's measured slabs stood 0.1 to 0.4 m off the declared faces, so the walk moved round the Office and every record's inputs with it.
        const b = buildingOf.get(m.id);
        if (b) {
          const towers = (b.towers ?? []).flatMap((t) => (t.poly ? [{ id: `${m.id}/${t.id}`, poly: t.poly.map(toBlock), height: t.height as number }] : []));
          const walls = [b.poly.map(toBlock), ...towers.map((t) => t.poly)];
          const mid = (s: Solid): V => (s.circle ? s.circle.c : [(s.x0 + s.x1) / 2, (s.y0 + s.y1) / 2]);
          const proud = solids.splice(from).filter((s) => !walls.some((P) => inPoly(mid(s), P)));
          prism(m.id, b.poly.map(toBlock), m.base, m.height, 'blocks', face);
          for (const t of towers) prism(t.id, t.poly, m.base, t.height, 'blocks', true);
          solids.push(...proud);
        }
        continue;
      }
    }
    prism(m.id, m.ring.map(toBlock), m.base, m.height, 'blocks', face);
  }
  // facade pieces: the footprint's centre on kit.at, the pivot at `base`
  for (const f of w.facades) {
    if (!inWin(toBlock(f.kit.at))) continue;
    const p = collide.pieces[f.kit.id]; if (!p) { unmeasured.add(f.kit.id); continue; }
    const yaw = f.kit.yaw ?? 0, sc = f.kit.scale, cx = (p.bounds[0][0] + p.bounds[1][0]) / 2, cz = (p.bounds[0][2] + p.bounds[1][2]) / 2, cs = Math.cos(yaw), sn = Math.sin(yaw);
    const pivot: V = [f.kit.at[0] - (cx * sc[0] * cs - cz * sc[2] * sn), f.kit.at[1] - (cx * sc[0] * sn + cz * sc[2] * cs)];
    placePiece(f.id, f.kit.id, pivot, yaw, sc, f.base, 'blocks', true);
  }
  // street items: pivot on `at`, at height `z`; crossing parts lie flat and signal orbs float, so neither is a solid
  for (const s of w.street) {
    if (!inWin(toBlock(s.at)) || s.kind.startsWith('crossing') || s.kind === 'signal-orb') continue;
    if (s.kit && placePiece(s.id, s.kit.id, s.at, s.kit.yaw ?? s.yaw, s.kit.scale, s.z, s.sight)) continue;
    if (s.size[2] >= 0.02) prism(s.id, rect(s.at, s.size[0], s.size[1], s.yaw), s.z, s.size[2], s.sight);
  }
  // the shopfronts the generator draws as boxes (a facade piece carries its own shopfront); the canopy is overhead
  for (const f of w.frontages) {
    const boxes = [...(f.facade ? [] : [f.front, f.entrance]), ...(f.service ? [f.service] : [])];
    for (const b of boxes) if (b.shape === 'box' && b.size[2] >= 0.02) { const poly = rect(b.c, b.size[0], b.size[1], b.yaw); if (poly.some(inWin)) prism(b.id, poly, b.base, b.size[2], 'blocks', true); }
  }
  const standing = solids.flatMap((s) => (s.piece ? [s.piece] : []));
  return { solids, elements, volumes: solids.length, unmeasured: [...unmeasured].sort(), renderOnly: [...renderOnly].sort(), collideSha: collideDigest(collide, [...placed, POLE_PIECE]), solidSha: solidDigest(collide, [...standing, POLE_PIECE]) };
}

/** The pavement's height above the carriageway (standards.levels.datum.pavement): block z 0. */
export const pavementOf = (plan: Plan): number => (plan.standards as { levels?: { datum?: { pavement?: number } } }).levels?.datum?.pavement ?? 0.18;

/** The floor of every cell of the grid, block z, metres: the ground drawn there (Layers.Ground, as tools/geometry/visible.ts ports it), or the floor the civic block declares. */
export function floorGrid(plan: Plan, civ: Civic, w: World, win: Window, cell: number): Float64Array {
  const toBlock = toBlockFn(plan.civic), pave = pavementOf(plan);
  const nx = Math.round((win.x1 - win.x0) / cell), ny = Math.round((win.y1 - win.y0) / cell), out = new Float64Array(nx * ny);
  // the ground layers in block frame
  const lineB = (l: number[][]) => l.map((q) => toBlock([q[0]!, q[1]!]));
  const near = (l: V[]) => l.some((q) => q[0] >= win.x0 - 20 && q[0] <= win.x1 + 20 && q[1] >= win.y0 - 20 && q[1] <= win.y1 + 20);
  const lineDist = (p: V, l: V[]) => { let d = Infinity; for (let i = 0; i + 1 < l.length; i++) d = Math.min(d, segDist(p, l[i]!, l[i + 1]!)); return d; };
  const bb = (r: V[]) => ({ r, x0: Math.min(...r.map((p) => p[0])), x1: Math.max(...r.map((p) => p[0])), y0: Math.min(...r.map((p) => p[1])), y1: Math.max(...r.map((p) => p[1])) });
  const inB = (b: { r: V[]; x0: number; x1: number; y0: number; y1: number }, q: V) => q[0] >= b.x0 && q[0] <= b.x1 && q[1] >= b.y0 && q[1] <= b.y1 && inPoly(q, b.r);
  const TOP_PROUD = 0.004; // KerbKit.TopProud
  const plots = w.ground.plots.map((p) => ({ ...bb(p.ring.map(toBlock)), top: p.base + p.height })).filter((p) => p.x1 >= win.x0 && p.x0 <= win.x1 && p.y1 >= win.y0 && p.y0 <= win.y1);
  const paveTop = Math.max(0, ...w.ground.plots.filter((p) => p.height < 0.5).map((p) => p.base + p.height));
  const roads = w.ground.roads.map((r) => ({ r, l: lineB(r.line) })).filter((x) => near(x.l)), foot = w.ground.foot.map((r) => ({ r, l: lineB(r.line) })).filter((x) => near(x.l));
  const flats = w.ground.flats.map((f) => ({ ...bb(f.ring.map(toBlock)), level: f.level }));
  const fills = returnFills(w), planOf = toPlanFn(plan.civic); // brief 028: the kerb strip is dropped inside a kerb return's carriageway fill (Layers.Ground)
  const cuts: { b: ReturnType<typeof bb>; level: number }[] = [];
  for (const d of w.ground.drives ?? []) { cuts.push({ b: bb(d.ring.map(toBlock)), level: d.level }); const bay = w.ground.flats.find((f) => f.id === d.to); if (bay) cuts.push({ b: bb(bay.ring.map(toBlock)), level: bay.level }); }
  // the declared floors (civic-block.json floors): Datum's reading of them (tools/plan/civic.ts floorAt), so the engine and rule M-3 stand a person on one height
  const toPlan = toPlanFn(plan.civic), floorIds = new Set(civ.floors.map((f) => f.id));
  for (let i = 0; i < nx; i++) for (let j = 0; j < ny; j++) {
    const q: V = [win.x0 + (i + 0.5) * cell, win.y0 + (j + 0.5) * cell];
    let g = 0, any = false;
    for (const { r, l } of roads) {
      const d = lineDist(q, l), half = r.width / 2;
      if (d <= half) { if (r.level >= g) { g = r.level; any = true; } }
      else if (d <= half + r.kerbWidth && !(fills.has && fills.inside(planOf(q))) && !roads.some((o) => o.r !== r && lineDist(q, o.l) < o.r.width / 2 - 0.01)) { const t = Math.max(r.level + r.kerbHeight, paveTop + TOP_PROUD); if (t > g) { g = t; any = true; } }
    }
    for (const { r, l } of foot) if (lineDist(q, l) <= r.width / 2 && r.level > g) { g = r.level; any = true; }
    for (const f of flats) if (inB(f, q) && f.level > g) { g = f.level; any = true; }
    const cut = cuts.find((c) => inB(c.b, q));
    if (cut) { if (cut.level > g || !any) g = cut.level; }
    else for (const p of plots) if (inB(p, q) && p.top > g) g = p.top;
    let z = g - pave;
    const dec = floorAt(civ, toPlan(q));
    if (dec.on && floorIds.has(dec.on)) z = Math.max(z, dec.floor);
    out[i * ny + j] = z;
  }
  return out;
}

export type SimGrid = WalkGrid & {
  floor: Float64Array; drawn: Drawn;
  /** Cells the drawn solids block that the block-only grid left free, and why (the first volume found per cell). */
  added: Map<number, string>;
  /** The drawn occluders: eye-spanning volumes on pieces that stop sight, block frame m, de-duplicated within each piece. */
  occluders: { id: string; z: number; h: number; poly: V[] }[];
};

/** The Handover's mission camera poles (contracts/missions/handover/mission.json cameras of kind pole-camera, kit.collide.json CCTVPole at the host's scale:
 *  tools/missions/handover/legs.ts cameraPoles) as walk solids: a circle on the floor under it, standing 5.5 m (the plan's mount). Block frame, m. */
export const MISSION_FILE = 'contracts/missions/handover/mission.json';
export function missionPoles(floorAt: (q: V) => number): Solid[] {
  const root = join(import.meta.dir, '../..');
  const mission = JSON.parse(readFileSync(join(root, MISSION_FILE), 'utf8')), collide = JSON.parse(readFileSync(join(root, COLLIDE_FILE), 'utf8'));
  return cameraPoles(mission, collide).map(({ id, c, r }) => {
    const f = floorAt(c);
    return { id, piece: 'CCTVPole', circle: { c, r }, poly: [[c[0] - r, c[1] - r], [c[0] + r, c[1] - r], [c[0] + r, c[1] + r], [c[0] - r, c[1] + r]] as V[], z0: f, z1: f + 5.5, stand: false, sight: 'open' as const, face: false, x0: c[0] - r, x1: c[0] + r, y0: c[1] - r, y1: c[1] + r };
  });
}
/** The engine's walk grid: the block-only grid (tools/plan/walk-grid.ts) with the drawn solids added, a floor per cell, and the drawn occluders. */
/** `walk` false: the rows are the block-only grid's (the drawn solids measured, counted in `added`, but not blocking), so the site can carry the floor and the
 *  measurement while the drawn walk waits for its adoption (site option drawnWalk). */
export function simGrid(plan: Plan, d: Derived, civ: Civic, eyeM: number, win: Window = WINDOW, cell = CELL, walk = true): SimGrid {
  const base = walkGrid(plan, d, civ, win, cell), w = generated(plan, d, civ).w, drawn = drawnSolids(plan, civ, w, win);
  const floor = floorGrid(plan, civ, w, win, cell), r = bodyOf(plan), { nx, ny } = base;
  const floorAt = (q: V) => { const i = Math.floor((q[0] - win.x0) / cell), j = Math.floor((q[1] - win.y0) / cell); return i >= 0 && j >= 0 && i < nx && j < ny ? floor[i * ny + j]! : 0; };
  const rows = base.rows.map((s) => s.split('')), added = new Map<number, string>();
  const distTo = (s: Solid, q: V) => {
    if (s.circle) return Math.max(0, Math.hypot(q[0] - s.circle.c[0], q[1] - s.circle.c[1]) - s.circle.r);
    if (inPoly(q, s.poly)) return 0;
    let dd = Infinity; for (let k = 0; k < s.poly.length; k++) dd = Math.min(dd, segDist(q, s.poly[k]!, s.poly[(k + 1) % s.poly.length]!)); return dd;
  };
  // engine 0.30.0 (Keel; Pulse's legs.ts): the mission cameras' poles, which the host stands and the world does not place, block the walk as drawn solids do
  // (body clearance from their footprint circle), so the guard's and the cell's routes keep clear of them
  for (const s of [...drawn.solids, ...missionPoles(floorAt)]) {
    const mid: V = [(s.x0 + s.x1) / 2, (s.y0 + s.y1) / 2];
    if (s.stand && Math.abs(s.z1 - floorAt(mid)) <= FLUSH) continue; // a stand top that is the floor declared under it: that floor
    const rr = s.face ? FACE : r;
    const i0 = Math.max(0, Math.floor((s.x0 - rr - win.x0) / cell)), i1 = Math.min(nx - 1, Math.floor((s.x1 + rr - win.x0) / cell));
    const j0 = Math.max(0, Math.floor((s.y0 - rr - win.y0) / cell)), j1 = Math.min(ny - 1, Math.floor((s.y1 + rr - win.y0) / cell));
    for (let i = i0; i <= i1; i++) for (let j = j0; j <= j1; j++) {
      if (rows[i]![j] === '#') continue;
      const f = floor[i * ny + j]!;
      if (!(s.z0 < f + HEAD && s.z1 > f + STEP)) continue;
      const dc = distTo(s, base.centre(i, j)); if (s.face ? dc >= FACE : dc >= r) continue; // engine 0.30.0: a face keeps a walker's shoulder off it (FACE), every other solid the whole body (r)
      rows[i]![j] = '#'; added.set(i * ny + j, s.id);
    }
  }
  const R = walk ? rows.map((x) => x.join('')) : base.rows;
  const free = (q: V) => { const i = Math.floor((q[0] - win.x0) / cell), j = Math.floor((q[1] - win.y0) / cell); return i >= 0 && j >= 0 && i < nx && j < ny && R[i]![j] === '.'; };
  const cellFree = (i: number, j: number) => i >= 0 && j >= 0 && i < nx && j < ny && R[i]![j] === '.';
  const tryNearestFree = (q: V): V | undefined => {
    const ci = Math.floor((q[0] - win.x0) / cell), cj = Math.floor((q[1] - win.y0) / cell);
    let best: V | undefined, bd = Infinity;
    for (let k = 0; k <= 12; k++) {
      for (let i = ci - k; i <= ci + k; i++) for (let j = cj - k; j <= cj + k; j++) {
        if (!cellFree(i, j)) continue;
        const c = base.centre(i, j), dd = Math.hypot(c[0] - q[0], c[1] - q[1]);
        if (dd < bd) { bd = dd; best = c; }
      }
      if (best && bd <= (k + 0.5) * cell) break;
    }
    return best;
  };
  const nearestFree = (q: V): V => { const b = tryNearestFree(q); if (!b) throw new Error(`no free ground within 6 m of ${q}`); return b; };

  // sight: eye-spanning volumes of pieces that stop it; a volume inside another of the same element adds nothing
  const eyeZ = eyeM, occ: SimGrid['occluders'] = [];
  const byId = new Map<string, Solid[]>();
  // engine 0.30.0 (Splice's G-3b): a volume stops sight when the eye line crosses it with GRAZE to spare above; a top the line only grazes is a rounded shoulder
  // in the drawn body (the civic steward monument and service-robot-9, whose box outlines stood wider than their drawn bodies at 1.6 m)
  for (const s of drawn.solids) if (s.sight === 'blocks' && s.z0 <= eyeZ && s.z1 >= eyeZ + GRAZE) byId.set(s.id, [...(byId.get(s.id) ?? []), s]);
  for (const [id, list] of byId) {
    const xs = list.flatMap((x) => [x.x0, x.x1]), ys = list.flatMap((x) => [x.y0, x.y1]), z = Math.min(...list.map((x) => x.z0)), h = Math.max(...list.map((x) => x.z1)) - z;
    // engine 0.30.0 (Splice's G-3b; Sky 2026-10-24: "can take some liberties for performance"): an element whose eye-level footprint is under THIN across both
    // ways (a parasol's stem, a lamp's pole, a small robot) stops no sight, as the cover rule already says it hides nobody (tools/cover/gen.ts)
    if (Math.max(...xs) - Math.min(...xs) < THIN && Math.max(...ys) - Math.min(...ys) < THIN) continue;
    if (Math.hypot(Math.max(...xs) - Math.min(...xs), Math.max(...ys) - Math.min(...ys)) <= COMPACT) { occ.push({ id, z, h, poly: hull(list.flatMap((x) => x.poly)) }); continue; } // a lamp, a booth, a palm: one outline
    const on = (p: V, poly: V[]) => inPoly(p, poly) || poly.some((a, k) => segDist(p, a, poly[(k + 1) % poly.length]!) < 1e-6);
    const holds = (o: Solid, s: Solid) => s.poly.every((p) => on(p, o.poly));
    const keep = list.filter((s, k) => !list.some((o, m) => m !== k && holds(o, s) && (m < k || !holds(s, o))));
    keep.forEach((s, k) => occ.push({ id: keep.length > 1 ? `${id}#${k}` : id, z: s.z0, h: s.z1 - s.z0, poly: s.poly }));
  }
  return { ...base, rows: R, free, cellFree, tryNearestFree, nearestFree, floor, drawn, added, occluders: occ };
}

/** A grid's floor as the site writes it: per row, run-length pairs [cm, count] (a row is mostly one height). */
export function floorRows(floor: Float64Array, nx: number, ny: number): number[][] {
  const out: number[][] = [];
  for (let i = 0; i < nx; i++) {
    const row: number[] = []; let v = Math.round(floor[i * ny]! * 100), n = 0;
    for (let j = 0; j < ny; j++) { const c = Math.round(floor[i * ny + j]! * 100); if (c === v) n++; else { row.push(v, n); v = c; n = 1; } }
    row.push(v, n); out.push(row);
  }
  return out;
}

/** Convex hull (Andrew's monotone chain), counter-clockwise, no repeated points. */
export function hull(pts: V[]): V[] {
  const p = [...pts].sort((a, b) => a[0] - b[0] || a[1] - b[1]), c = (o: V, a: V, b: V) => (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0]);
  const lo: V[] = [], up: V[] = [];
  for (const q of p) { while (lo.length >= 2 && c(lo[lo.length - 2]!, lo[lo.length - 1]!, q) <= 0) lo.pop(); lo.push(q); }
  for (let k = p.length - 1; k >= 0; k--) { const q = p[k]!; while (up.length >= 2 && c(up[up.length - 2]!, up[up.length - 1]!, q) <= 0) up.pop(); up.push(q); }
  return [...lo.slice(0, -1), ...up.slice(0, -1)];
}
