// Play-view occlusion (brief 075, Sky 2026-10-28: "we wanted to lower to around 3 stories high for most of the buildings so game plan view wasn't just always removing buildings floors
// just to see characters"): the share of pavement the play camera cannot see because a building stands between it and the camera. Informational, not a rule (yet).
//
// The camera is orthographic, so every view ray is parallel: compass yaw y (0 = north; Unity Euler(pitch, yaw), so it looks along plan (sin y, cos y)), pitch 38° down. A ground point p at
// height z0 is hidden when some occluder of top h stands at horizontal distance s from p toward the camera with h − s·tan(pitch) > z0: the shadow a sun at the camera's place would cast.
// Ortho size (zoom 14) scales the frame only, never the share. Computed on a raster in the view's own frame (v toward the camera), one sweep per column, O(cells) per yaw.
//
// Occluders (2.5D: a solid column up to its top): building masses (tower stand-ins included), the kit facade pieces at their kit height (the face they dress extruded into the building, at most 14 m: a canopy is low),
// civic masses except the ones the play camera fades (parasols, shelters), megastructure parts and prims, and the signature pieces' kit boxes (SpaceHQ, stations, the gate, the liner: a box
// is an upper bound for a slender form). Street items, trees, roofs' furniture, decks and the horizon are not occluders.
// Pavement: plot slabs, foot ways and footway tables, less the ground under anything standing on the street (base < 2 m); ground under a raised occluder (base ≥ 2 m, an arcade or a
// bridge) stays pavement and counts hidden under it. Kerbside: the pavement within KERBSIDE m of a kerb (the
// street pavement, without courts and yards). Open ground: the pavement and the carriageways (roads with their kerbs, circuses) at
// street level. Decks: the level decks at their level height. Each layer is measured on its own.
// A figure counts hidden when its head (1.8 m) is: with solid columns that hides the whole figure.
import { readFileSync } from 'node:fs';
import type { KitRef, Prim, V, World } from '../../contracts/world/world.schema';

export const PITCH = 38, YAWS = [30, 120, 210, 300] as const, HEAD = 1.8, RAISED = 2;
export type Occluder = { ring: V[]; base: number; top: number };
export type Walk = { ring: V[]; z: number };
export type Region = { id: string; name: string; ring: V[] };
/** Per region: pavement area (m²) and, per yaw and their mean, the share hidden (0..1) of the ground and of figures standing on it. */
export type Share = { area: number; hidden: Record<string, number>; figure: Record<string, number> };
export type Layer = { district: Share; handover?: Share; areas: Record<string, Share> };
export type Occlusion = { cell: number; pitch: number; yaws: number[]; layers: Record<string, Layer> };
type KitSize = Map<string, [number, number, number]>;

export function kitSizes(kitFile: string): KitSize {
  const k = JSON.parse(readFileSync(kitFile, 'utf8')) as { assets: { id: string; size: [number, number, number] }[] };
  return new Map(k.assets.map(a => [a.id, a.size]));
}

/** The plan-frame footprint of a kit piece centred at `at`: size [across, up, depth] × scale, +X turned by yaw, front +Z. */
function kitBox(ref: KitRef & { at: V }, size: [number, number, number], yaw: number): { ring: V[]; h: number } {
  const a = size[0] * ref.scale[0], h = size[1] * ref.scale[1], d = size[2] * ref.scale[2], shift = 0;
  const X: V = [Math.cos(yaw), Math.sin(yaw)], Z: V = [-Math.sin(yaw), Math.cos(yaw)];
  const c: V = [ref.at[0] + Z[0] * shift, ref.at[1] + Z[1] * shift];
  const pt = (i: number, j: number): V => [c[0] + X[0] * a / 2 * i + Z[0] * d / 2 * j, c[1] + X[1] * a / 2 * i + Z[1] * d / 2 * j];
  return { ring: [pt(-1, -1), pt(1, -1), pt(1, 1), pt(-1, 1)], h };
}

function primRing(p: Prim): V[] {
  const [a, b] = p.size, X: V = [Math.cos(p.yaw), Math.sin(p.yaw)], Y: V = [-X[1], X[0]];
  if (p.shape === 'cyl' || p.shape === 'sphere' || p.shape === 'saucer') return circle(p.c, a / 2);
  const pt = (i: number, j: number): V => [p.c[0] + X[0] * a / 2 * i + Y[0] * b / 2 * j, p.c[1] + X[1] * a / 2 * i + Y[1] * b / 2 * j];
  return [pt(-1, -1), pt(1, -1), pt(1, 1), pt(-1, 1)];
}
const circle = (c: V, r: number, n = 24): V[] => Array.from({ length: n }, (_, i) => [c[0] + r * Math.cos((i / n) * 2 * Math.PI), c[1] + r * Math.sin((i / n) * 2 * Math.PI)] as V);

/** A road ribbon (a foot way) as quads per segment: `width` wide along `line`. */
function ribbon(line: V[], width: number): V[][] {
  const out: V[][] = [];
  for (let i = 1; i < line.length; i++) {
    const a = line[i - 1]!, b = line[i]!, l = Math.hypot(b[0] - a[0], b[1] - a[1]) || 1, n: V = [-(b[1] - a[1]) / l * width / 2, (b[0] - a[0]) / l * width / 2];
    out.push([[a[0] + n[0], a[1] + n[1]], [b[0] + n[0], b[1] + n[1]], [b[0] - n[0], b[1] - n[1]], [a[0] - n[0], a[1] - n[1]]]);
  }
  return out;
}

export function occludersOf(w: World, kit: KitSize): Occluder[] {
  const out: Occluder[] = [];
  for (const b of w.buildings) out.push({ ring: b.ring, base: b.base, top: b.base + b.height });
  // A facade piece's `at` is its footprint centre on some forms and its pivot on the face line on others, so the box is built from the face it dresses: the span a→b, extruded
  // into the building (toward its masses' centre) by the piece's depth, capped at the 14 m body (a bay's canopy is low); a pier (a = b) is its kit box at `at`.
  const centre = new Map<string, V>();
  for (const b of w.buildings) { const k = b.id.slice(0, b.id.lastIndexOf('/')); const c = centre.get(k) ?? [0, 0, 0] as unknown as V; for (const p of b.ring) { c[0] += p[0]; c[1] += p[1]; (c as unknown as number[])[2]! += 1; } centre.set(k, c); }
  const mid = (k: string): V | undefined => { const c = centre.get(k) as unknown as number[] | undefined; return c ? [c[0]! / c[2]!, c[1]! / c[2]!] : undefined; };
  for (const f of w.facades) {
    const s = kit.get(f.kit.id); if (!s) continue;
    const h = s[1] * f.kit.scale[1], depth = Math.min(s[2] * f.kit.scale[2], 14), ab: V = [f.b[0] - f.a[0], f.b[1] - f.a[1]], L = Math.hypot(ab[0], ab[1]);
    if (L < 0.5) { out.push({ ring: kitBox(f.kit, s, f.kit.yaw ?? 0).ring, base: f.base, top: f.base + h }); continue; }
    let n: V = [-ab[1] / L, ab[0] / L];
    const c = mid(f.building) ?? (f.normal ? [f.a[0] - f.normal[0], f.a[1] - f.normal[1]] as V : f.kit.at);
    if ((c[0] - f.a[0]) * n[0] + (c[1] - f.a[1]) * n[1] < 0) n = [-n[0], -n[1]];
    out.push({ ring: [f.a, f.b, [f.b[0] + n[0] * depth, f.b[1] + n[1] * depth], [f.a[0] + n[0] * depth, f.a[1] + n[1] * depth]], base: f.base, top: f.base + h });
  }
  for (const c of w.civic) if (!c.fade) out.push({ ring: c.ring, base: c.base, top: c.base + c.height });
  for (const m of w.megastructures) {
    for (const p of m.parts) out.push({ ring: p.ring, base: p.base, top: p.base + p.height });
    for (const p of m.prims) out.push({ ring: primRing(p), base: p.base, top: p.base + p.size[2] });
  }
  for (const s of w.signatures) {
    if (s.kind === 'tower' || !s.kit) continue; // a tower's form is its stand-in masses (buildings tower/<site>/…)
    const size = kit.get(s.kit.id.split('@')[0]!); if (!size) continue;
    out.push({ ring: kitBox(s.kit, size, s.kit.yaw ?? 0).ring, base: s.base, top: s.tip });
  }
  return out.filter(o => o.top > o.base && o.ring.length >= 3);
}

/** KERBSIDE: the pavement within this many metres of a carriageway's kerb line counts as the street pavement (the play streets), courts and yards behind the buildings do not. */
export const KERBSIDE = 8;
export function walksOf(w: World): { layers: { pavement: Walk[]; kerbside: Walk[]; open: Walk[]; deck: Walk[] }; within: { kerbside: V[][] } } {
  const PAVE = 0.18, street: Walk[] = [];
  for (const p of w.ground.plots) street.push({ ring: p.ring, z: p.base + p.height || PAVE });
  for (const f of w.ground.foot) for (const q of ribbon(f.line, f.width)) street.push({ ring: q, z: f.level });
  for (const f of w.ground.footways ?? []) street.push({ ring: f.table.ring, z: f.level });
  const road: Walk[] = [];
  for (const r of w.ground.roads) if (r.level < RAISED) for (const q of ribbon(r.line, r.width + 2 * r.kerbWidth)) road.push({ ring: q, z: r.level });
  for (const c of w.ground.circuses) if (c.level < RAISED) road.push({ ring: circle(c.centre, c.radius, 48), z: c.level });
  const deck: Walk[] = [];
  for (const l of w.levels) for (const d of l.decks) deck.push({ ring: d.ring, z: l.height });
  const near: V[][] = [];
  for (const r of w.ground.roads) if (r.level < RAISED) near.push(...ribbon(r.line, r.width + 2 * r.kerbWidth + 2 * KERBSIDE));
  for (const c of w.ground.circuses) if (c.level < RAISED) near.push(circle(c.centre, c.radius + KERBSIDE, 48));
  return { layers: { pavement: street, kerbside: street, open: [...street, ...road], deck }, within: { kerbside: near } };
}

/** Fill a ring on the raster: calls f(index) for each cell whose centre lies inside (even-odd). Raster: `nu` × `nv` cells of `cell` m, origin (u0, v0), row-major by v. */
function fill(ring: V[], g: Grid, f: (i: number) => void) {
  let vMin = Infinity, vMax = -Infinity;
  for (const p of ring) { if (p[1] < vMin) vMin = p[1]; if (p[1] > vMax) vMax = p[1]; }
  const j0 = Math.max(0, Math.ceil((vMin - g.v0) / g.cell - 0.5)), j1 = Math.min(g.nv - 1, Math.floor((vMax - g.v0) / g.cell - 0.5));
  const xs: number[] = [];
  for (let j = j0; j <= j1; j++) {
    const v = g.v0 + (j + 0.5) * g.cell; xs.length = 0;
    for (let k = 0, n = ring.length; k < n; k++) {
      const a = ring[k]!, b = ring[(k + 1) % n]!;
      if ((a[1] <= v) !== (b[1] <= v)) xs.push(a[0] + ((v - a[1]) / (b[1] - a[1])) * (b[0] - a[0]));
    }
    xs.sort((p, q) => p - q);
    for (let k = 0; k + 1 < xs.length; k += 2) {
      const i0 = Math.max(0, Math.ceil((xs[k]! - g.u0) / g.cell - 0.5)), i1 = Math.min(g.nu - 1, Math.floor((xs[k + 1]! - g.u0) / g.cell - 0.5));
      for (let i = i0; i <= i1; i++) f(j * g.nu + i);
    }
  }
}
type Grid = { u0: number; v0: number; nu: number; nv: number; cell: number };

/** Walkable layers measured: `pavement` (street-level footway), `open` (pavement and carriageway: all street-level open ground), `deck` (decks and the concourse). */
export type Input = { occluders: Occluder[]; layers: Record<string, Walk[]>; areas: Region[]; handover?: V[]; /** a layer counts only its cells inside these rings */ within?: Record<string, V[][]> };

/** The measure. `areas` are claimed in order (a cell belongs to the first area holding it; cells in none count to the district only). */
export function measure(inp: Input, cell = 0.5, yaws: readonly number[] = YAWS, pitch = PITCH): Occlusion {
  const t = Math.tan((pitch * Math.PI) / 180), names = Object.keys(inp.layers), R = inp.areas.length + 2; // region slots: 0 district, 1 handover, 2.. areas
  const zero = (): Share => ({ area: 0, hidden: {}, figure: {} });
  const acc = names.map(() => Array.from({ length: R }, zero));
  for (const yaw of yaws) {
    const r = (yaw * Math.PI) / 180, d: V = [-Math.sin(r), -Math.cos(r)], u: V = [d[1], -d[0]]; // d: horizontal, from the ground toward the camera
    const rot = (p: V): V => [p[0] * u[0] + p[1] * u[1], p[0] * d[0] + p[1] * d[1]];
    const occ = inp.occluders.map(o => ({ ...o, ring: o.ring.map(rot) })), lay = names.map(n => inp.layers[n]!.map(w => ({ ...w, ring: w.ring.map(rot) })));
    // the raster: the walkable bbox, grown toward the camera by the farthest shadow
    let a = [Infinity, -Infinity, Infinity, -Infinity], maxTop = 0;
    for (const l of lay) for (const w of l) for (const p of w.ring) { a[0] = Math.min(a[0]!, p[0]); a[1] = Math.max(a[1]!, p[0]); a[2] = Math.min(a[2]!, p[1]); a[3] = Math.max(a[3]!, p[1]); }
    for (const o of occ) maxTop = Math.max(maxTop, o.top);
    if (!(a[0]! < a[1]!)) a = [0, 1, 0, 1];
    const g: Grid = { u0: Math.floor(a[0]! / cell) * cell, v0: Math.floor(a[2]! / cell) * cell, nu: 0, nv: 0, cell };
    g.nu = Math.ceil((a[1]! - g.u0) / cell) + 1; g.nv = Math.ceil((a[3]! + maxTop / t + cell - g.v0) / cell) + 1;
    const N = g.nu * g.nv, H = new Float32Array(N), blocked = new Uint8Array(N), area = new Uint8Array(N), ho = new Uint8Array(N);
    for (const o of occ) { const top = o.top, low = o.base < RAISED; fill(o.ring, g, i => { if (top > H[i]!) H[i] = top; if (low) blocked[i] = 1; }); }
    const Z = lay.map((l, L) => {
      const z = new Float32Array(N).fill(NaN); for (const w of l) fill(w.ring, g, i => { if (!(w.z <= z[i]!)) z[i] = w.z; });
      const keep = inp.within?.[names[L]!]; if (!keep) return z;
      const m = new Uint8Array(N); for (const r of keep) fill(r.map(rot), g, i => { m[i] = 1; });
      for (let i = 0; i < N; i++) if (!m[i]) z[i] = NaN;
      return z;
    });
    inp.areas.forEach((ar, k) => fill(ar.ring.map(rot), g, i => { if (!area[i]) area[i] = k + 1; }));
    if (inp.handover) fill(inp.handover.map(rot), g, i => { ho[i] = 1; });
    const tot = names.map(() => new Float64Array(R)), gnd = names.map(() => new Float64Array(R)), fig = names.map(() => new Float64Array(R));
    const step = cell * t;
    for (let i = 0; i < g.nu; i++) {
      let S = -Infinity; // the highest shadow reaching this row from the rows nearer the camera
      for (let j = g.nv - 1; j >= 0; j--) {
        const k = j * g.nu + i, h = H[k]!, cover = Math.max(S, h);
        for (let L = 0; L < Z.length; L++) {
          const z = Z[L]![k]!;
          if (Number.isNaN(z) || (blocked[k] && h > z)) continue; // no walk here, or the walk is inside something standing on it
          const gh = cover > z + 0.05 ? 1 : 0, fh = cover > z + HEAD ? 1 : 0, T = tot[L]!, G = gnd[L]!, F = fig[L]!;
          T[0]++; G[0] += gh; F[0] += fh;
          if (ho[k]) { T[1]++; G[1] += gh; F[1] += fh; }
          const ak = area[k]!; if (ak) { T[ak + 1]++; G[ak + 1] += gh; F[ak + 1] += fh; }
        }
        S = Math.max(S, h) - step;
      }
    }
    names.forEach((_, L) => { for (let s = 0; s < R; s++) { const n = tot[L]![s]!, x = acc[L]![s]!; x.area = Math.round(n * cell * cell); x.hidden[yaw] = n ? gnd[L]![s]! / n : 0; x.figure[yaw] = n ? fig[L]![s]! / n : 0; } });
  }
  const mean = (s: Share): Share => {
    const m = (r: Record<string, number>) => yaws.reduce((x, y) => x + (r[y] ?? 0), 0) / yaws.length;
    const r4 = (r: Record<string, number>) => Object.fromEntries(Object.entries(r).map(([k, v]) => [k, Math.round(v * 10000) / 10000]));
    return { area: s.area, hidden: r4({ ...s.hidden, mean: m(s.hidden) }), figure: r4({ ...s.figure, mean: m(s.figure) }) };
  };
  return {
    cell, pitch, yaws: [...yaws],
    layers: Object.fromEntries(names.map((n, L) => [n, { district: mean(acc[L]![0]!), ...(inp.handover ? { handover: mean(acc[L]![1]!) } : {}), areas: Object.fromEntries(inp.areas.map((ar, k) => [ar.id, mean(acc[L]![k + 2]!)])) }])),
  };
}

/** The measure of an export: regions are the plan's areas (derive(plan).areas) and the Handover's capstone zone rect [x0, x1, y0, y1]. */
export function occlusionOf(w: World, kit: KitSize, areas: Region[], zone?: [number, number, number, number], cell = 0.5): Occlusion {
  const handover: V[] | undefined = zone ? [[zone[0], zone[2]], [zone[1], zone[2]], [zone[1], zone[3]], [zone[0], zone[3]]] : undefined;
  const { layers, within } = walksOf(w);
  return measure({ occluders: occludersOf(w, kit), layers, within, areas, handover }, cell);
}

export const pct = (x: number | undefined) => `${((x ?? 0) * 100).toFixed(1)}%`;
/** One informational line for the world report. */
export function occlusionLine(o: Occlusion): string {
  const p = o.layers.pavement!, op = o.layers.open, kb = o.layers.kerbside, h = p.handover;
  return `play-view occlusion (brief 075; ortho ${o.pitch}°, yaws ${o.yaws.join('/')}, ${o.cell} m raster; informational): pavement hidden behind buildings ${pct(p.district.hidden.mean)} of ${p.district.area} m² (figures ${pct(p.district.figure.mean)})`
    + (h ? `, Handover streets ${pct(h.hidden.mean)} (figures ${pct(h.figure.mean)})` : '') + (kb ? `; kerbside pavement (≤ ${KERBSIDE} m from a kerb) ${pct(kb.district.hidden.mean)}${kb.handover ? `, Handover ${pct(kb.handover.hidden.mean)}` : ''}` : '') + (op ? `; with the carriageways ${pct(op.district.hidden.mean)}${op.handover ? `, Handover ${pct(op.handover.hidden.mean)}` : ''}` : '')
    + (o.layers.deck ? `; decks ${pct(o.layers.deck.district.hidden.mean)}` : '') + `; pavement by area ${Object.entries(p.areas).map(([k, v]) => `${k} ${pct(v.hidden.mean)}`).join(', ')}`;
}
