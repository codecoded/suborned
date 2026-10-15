// What the play camera sees of a mission point (rule V-1, brief 003 B named it; brief 011, Vantage). The camera is fixed (GDD §10:
// zoom 14 at 38°, yaw 30) and orthographic, so every view ray is parallel: from any point, the ray back to the camera runs toward
// compass 210 (SSW) and climbs tan 38° = 0.78 m per metre. A point is hidden by whatever that ray passes through below its top.
// A person is a 1.75 m figure sampled on a 3 x 6 grid (±0.2 m across the screen, 0.1 to 1.75 m up); `seen` gives the share of the
// figure each class of blocker leaves. Blockers are extruded rings (base to top), each marked as architecture or dressing and as
// fade-tagged or not, as Unity's FadeTags marks them: buildings, civic buildings and the pieces the export flags fade (parasols,
// cypresses, entrance canopies) may thin to a ghost; decks, transit, street trees and furniture never do (ArchitectureFade.cs).
// Dressing is approximated (a cypress is its trunk-to-tip core, a parasol its canopy, a street tree its crown), so V-1 judges a point
// on architecture alone and reports dressing beside it.
import type { Mass, World } from '../../contracts/world/world.schema';
import { type V, inPoly } from '../plan/geom';

export type View = { yaw: number; pitch: number; zoom: number };
/** GDD §10 / world.view: compass yaw 30, pitch 38, orthographic half-height 14 m (1080 px = 28 m). */
export const PLAY: View = { yaw: 30, pitch: 38, zoom: 14 };

export type Blocker = { id: string; ring: V[]; z0: number; z1: number; fade: boolean; kind: 'architecture' | 'dressing'; box?: [number, number, number, number] };
export type Seen = {
  /** Share of the figure no blocker covers. */
  clear: number;
  /** Share no architecture covers (dressing may). V-1 judges this. */
  pastArchitecture: number;
  /** Share covered only by blockers the fade may thin: seen as a ghost when the fade's disc holds the point. */
  onlyFade: number;
  /** Blockers that cover any sample, nearest first by id. */
  by: string[];
};

const rad = (d: number) => (d * Math.PI) / 180;
/** Horizontal unit from a point toward the camera (plan frame), and the rise per horizontal metre along it. */
export const towardCamera = (v: View = PLAY) => ({ h: [-Math.sin(rad(v.yaw)), -Math.cos(rad(v.yaw))] as V, rise: Math.tan(rad(v.pitch)) });
/** Screen right as a plan direction. */
export const screenRight = (v: View = PLAY): V => [Math.cos(rad(v.yaw)), -Math.sin(rad(v.yaw))];

/** Screen position (px, origin top left) of plan point p at height z, for a camera centred on `focus` (plan, height 0). */
export function project(p: V, z: number, focus: V, v: View = PLAY, size: V = [1920, 1080]): V {
  const k = size[1] / (2 * v.zoom), r = screenRight(v), y = rad(v.yaw), pi = rad(v.pitch);
  const up: [number, number, number] = [Math.sin(pi) * Math.sin(y), Math.sin(pi) * Math.cos(y), Math.cos(pi)];
  const d: [number, number, number] = [p[0] - focus[0], p[1] - focus[1], z];
  return [size[0] / 2 + k * (d[0] * r[0] + d[1] * r[1]), size[1] / 2 - k * (d[0] * up[0] + d[1] * up[1] + d[2] * up[2])];
}
/** Depth along the view (larger = further from the camera), for ordering. */
export function depth(p: V, z: number, v: View = PLAY): number {
  const y = rad(v.yaw), pi = rad(v.pitch);
  return Math.cos(pi) * (p[0] * Math.sin(y) + p[1] * Math.cos(y)) - Math.sin(pi) * z;
}

const boxOf = (ring: V[]): [number, number, number, number] => [Math.min(...ring.map(p => p[0])), Math.max(...ring.map(p => p[0])), Math.min(...ring.map(p => p[1])), Math.max(...ring.map(p => p[1]))];

/** Whether the ray from (p, z) toward the camera passes through blocker b. */
export function blocks(p: V, z: number, b: Blocker, v: View = PLAY): boolean {
  const { h, rise } = towardCamera(v), box = b.box ?? (b.box = boxOf(b.ring));
  if (z >= b.z1) return false; // the ray only climbs
  const sMax = (b.z1 - z) / rise; // past this the ray is above the top
  // quick reject: the ray's horizontal span [0, sMax] against the ring's box
  const ex = [p[0], p[0] + h[0] * sMax], ey = [p[1], p[1] + h[1] * sMax];
  if (Math.max(...ex) < box[0] || Math.min(...ex) > box[1] || Math.max(...ey) < box[2] || Math.min(...ey) > box[3]) return false;
  const sMin = Math.max(0, (b.z0 - z) / rise); // before this the ray is below the base
  // the parameters where the ray crosses the ring's edges, then the inside spans between them
  const cuts: number[] = [];
  for (let i = 0, n = b.ring.length; i < n; i++) {
    const a = b.ring[i]!, c = b.ring[(i + 1) % n]!, ex2 = c[0] - a[0], ey2 = c[1] - a[1], den = h[0] * ey2 - h[1] * ex2;
    if (Math.abs(den) < 1e-12) continue;
    const s = ((a[0] - p[0]) * ey2 - (a[1] - p[1]) * ex2) / den, t = ((a[0] - p[0]) * h[1] - (a[1] - p[1]) * h[0]) / den;
    if (t >= 0 && t <= 1 && s > 0) cuts.push(s);
  }
  cuts.sort((x, y) => x - y);
  const spans: [number, number][] = [];
  let inside = inPoly(p, b.ring), from = 0;
  for (const s of cuts) { if (inside) spans.push([from, s]); inside = !inside; from = s; }
  if (inside) spans.push([from, Infinity]);
  return spans.some(([a, c]) => a <= sMax && c >= sMin);
}

/** The figure's sample points: 3 across the screen × 6 up. */
export function figure(p: V, z0: number, v: View = PLAY, height = 1.75): { p: V; z: number }[] {
  const r = screenRight(v), out: { p: V; z: number }[] = [];
  for (const off of [-0.2, 0, 0.2]) for (const up of [0.1, 0.43, 0.76, 1.09, 1.42, height]) out.push({ p: [p[0] + r[0] * off, p[1] + r[1] * off], z: z0 + up });
  return out;
}

/** How much of a standing figure at p (feet at z0) the play camera sees. */
export function seen(p: V, z0: number, blockers: Blocker[], v: View = PLAY, height = 1.75): Seen {
  const S = figure(p, z0, v, height), by = new Set<string>();
  let clear = 0, past = 0, fadeOnly = 0;
  for (const s of S) {
    const hits = blockers.filter(b => blocks(s.p, s.z, b, v));
    hits.forEach(b => by.add(b.id));
    if (!hits.length) clear++;
    if (!hits.some(b => b.kind === 'architecture')) past++;
    if (hits.length && hits.every(b => b.fade)) fadeOnly++;
  }
  return { clear: clear / S.length, pastArchitecture: past / S.length, onlyFade: fadeOnly / S.length, by: [...by].sort() };
}

/** The share of ground points (z) the camera sees past architecture: a camera cone, a walk. */
export function groundSeen(pts: V[], z: number, blockers: Blocker[], v: View = PLAY): number {
  if (!pts.length) return 1;
  return pts.filter(p => !blockers.some(b => b.kind === 'architecture' && blocks(p, z, b, v))).length / pts.length;
}

const ringScaled = (ring: V[], k: number): V[] => { const c: V = [ring.reduce((s, p) => s + p[0], 0) / ring.length, ring.reduce((s, p) => s + p[1], 0) / ring.length]; return ring.map(p => [c[0] + (p[0] - c[0]) * k, c[1] + (p[1] - c[1]) * k]); };
const disc = (c: V, r: number, n = 12): V[] => Array.from({ length: n }, (_, i) => [c[0] + r * Math.cos((2 * Math.PI * i) / n), c[1] + r * Math.sin((2 * Math.PI * i) / n)] as V);

/** Piece-shaped stand-ins for the civic kit pieces that stand over people (the export's ring is only the piece's footprint box). */
export type PieceShape = { id: string; parts: { ring: V[]; z0: number; z1: number }[] };

/**
 * Every blocker within `near` (plan box, x0 x1 y0 y1) of the generated world. `pieces`: shapes to use in place of named civic kit
 * masses (the Records Office entrances: their piers, cheeks and marquee from the block's dressing). Civic kit pieces not named and
 * not dressing that stands over people (cypress, parasol, shelter, fountain) are left out: lamps, bollards, planters, beds, tables, inlay.
 */
export function blockersOf(w: World, near: [number, number, number, number], pieces: PieceShape[] = []): Blocker[] {
  const box = near, inside = (ring: V[]) => { const b = boxOf(ring); return b[1] >= box[0] && b[0] <= box[1] && b[3] >= box[2] && b[2] <= box[3]; };
  const out: Blocker[] = [], arch = (m: Mass, fade: boolean) => { if (inside(m.ring)) out.push({ id: m.id, ring: m.ring, z0: m.base, z1: m.base + m.height, fade, kind: 'architecture' }); };
  for (const m of w.buildings) arch(m, true);
  for (const g of w.gates ?? []) arch(g, true);
  for (const ms of w.megastructures) for (const m of ms.parts) arch(m, true);
  for (const l of w.levels) for (const d of l.decks) if (l.height > 0 && inside(d.ring)) out.push({ id: d.id, ring: d.ring, z0: l.height - d.thickness, z1: l.height, fade: false, kind: 'architecture' });
  for (const m of [...w.transit.stations, ...w.transit.stops]) if (m.height > 2.2) arch(m, false);
  const named = new Map(pieces.map(p => [p.id, p]));
  for (const m of w.civic) {
    if (!inside(m.ring)) continue;
    const shape = named.get(m.id);
    if (shape) { shape.parts.forEach((q, i) => out.push({ id: `${m.id}#${i}`, ring: q.ring, z0: q.z0, z1: q.z1, fade: !!m.fade, kind: 'architecture' })); continue; }
    if (!m.kit) { arch(m, true); continue; } // the café, the Office and its towers
    const k = m.kit.id, top = m.base + m.height;
    if (k === 'CafeBay') arch(m, true); // the café's ground storey
    else if (/^Cypress/.test(k)) out.push({ id: m.id, ring: ringScaled(m.ring, 0.55), z0: m.base + 0.8, z1: top, fade: !!m.fade, kind: 'dressing' });
    else if (/^Parasol/.test(k)) out.push({ id: m.id, ring: m.ring, z0: m.base + 2.07, z1: top, fade: !!m.fade, kind: 'dressing' });
    else if (/^BoardingPavilion/.test(k)) out.push({ id: m.id, ring: m.ring, z0: top - 0.4, z1: top, fade: !!m.fade, kind: 'dressing' });
    else if (k === 'AtomFountain') out.push({ id: m.id, ring: ringScaled(m.ring, 0.25), z0: m.base, z1: top, fade: false, kind: 'dressing' });
  }
  for (const s of w.street) {
    if (s.kind !== 'tree' || !inside([s.at])) continue;
    const h = s.size[2], z = s.z ?? 0;
    if (s.kit?.id === 'CoconutPalm') out.push({ id: s.id, ring: disc(s.at, 2.4), z0: z + h - 2.6, z1: z + h, fade: false, kind: 'dressing' });
    else out.push({ id: s.id, ring: disc(s.at, 0.4 * (s.kit?.scale[0] ?? 1)), z0: z + 1.5, z1: z + h, fade: false, kind: 'dressing' });
  }
  return out;
}
