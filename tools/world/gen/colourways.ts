// Building colourways (brief 074, Sky 2026-10-28: "Do building colors"; brief 075, Sky 2026-10-29: "our red is too red ... it needs to go back to old town").
// Brief 075 old palette: the old DecoKitColourways scheme the old reel ran (`plan+livery+soft`, ~/Projects/the-suborned/game Editor/DecoKit, read only). The
// families are the old ensembles (facade.json `families`: boulevard, circus, harbour, gardens, southside; Vela) and each facade.json area draws one of them
// (`areas[area].families`, one family each: records/lamplight boulevard, lantern southside, docks/harbour harbour, crescent circus, gardens gardens), so a
// building takes its area's ensemble and street neighbours share it by design ("colour goes by ensemble, never building by building"). On the old-street roads
// the old HighStreet walk paints alternate buildings in the Soft palette (oldStreetFamilies: soft-apricot, soft-celadon, soft-rose, soft-ochre, in order), the
// rest keep their ensemble. The 074 neighbour repair (dE2000 floor, re-draws) is gone with the 12 families. The export writes the family on every mass of the
// building (`buildings[].colourway`) and on every facade piece of it (`facades[].colourway`); CW-1 (rules/colourways.ts) checks both.
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Frontage } from '../../../contracts/world/world.schema';
import { type Ctx, rng } from './common';
import type { V } from '../../plan/geom';
import { OLD_STREET, oldRoadOf, roadFrame } from './oldstreet';

export const FACADE_SPEC_PATH = 'unity/Assets/Suborned/Editor/World/facade.json';
type Spec = { swatches: Record<string, string>; families: Record<string, [string, string, string]>; areas: Record<string, { families: Record<string, number> }>; hostAreas: Record<string, string> };
const raw: Spec = JSON.parse(readFileSync(resolve(import.meta.dir, '../../..', FACADE_SPEC_PATH), 'utf8'));
/** What the pick reads from facade.json (worldKey hashes it): the families' swatches, the area weights, the civic hosts. */
export const COLOURWAY_SPEC = {
  families: Object.fromEntries(Object.entries(raw.families).map(([k, f]) => [k, f.map(s => raw.swatches[s] ?? s)])) as Record<string, string[]>,
  areas: Object.fromEntries(Object.entries(raw.areas).map(([k, a]) => [k, a.families])) as Record<string, Record<string, number>>,
  hostAreas: raw.hostAreas,
};
export const CIVIC = 'civic';
/** The district colourways: every family but the civic block's and the old high street's (brief 075: those are given by oldStreetFamilies only, never drawn). */
export const FAMILIES = Object.keys(COLOURWAY_SPEC.families).filter(k => k !== CIVIC && !OLD_STREET.colour.palette.includes(k));
export const wallOf = (fam: string) => COLOURWAY_SPEC.families[fam]![0]!;


// ---------------------------------------------------------------- CIEDE2000 (sRGB hex, D65; the same maths as tools/decokit/deltae.py)
function lab(hex: string): [number, number, number] {
  const h = hex.replace('#', ''), c = [0, 2, 4].map(i => parseInt(h.slice(i, i + 2), 16) / 255).map(v => (v <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4));
  const [r, g, b] = c as [number, number, number];
  const X = (0.4124 * r + 0.3576 * g + 0.1805 * b) / 0.95047, Y = 0.2126 * r + 0.7152 * g + 0.0722 * b, Z = (0.0193 * r + 0.1192 * g + 0.9505 * b) / 1.08883;
  const f = (t: number) => (t > 0.008856 ? Math.cbrt(t) : 7.787 * t + 16 / 116);
  return [116 * f(Y) - 16, 500 * (f(X) - f(Y)), 200 * (f(Y) - f(Z))];
}
const rad = (d: number) => (d * Math.PI) / 180;
export function deltaE2000(x: string, y: string): number {
  const [L1, a1, b1] = lab(x), [L2, a2, b2] = lab(y);
  const Cm = (Math.hypot(a1, b1) + Math.hypot(a2, b2)) / 2, G = 0.5 * (1 - Math.sqrt(Cm ** 7 / (Cm ** 7 + 25 ** 7)));
  const a1p = (1 + G) * a1, a2p = (1 + G) * a2, C1p = Math.hypot(a1p, b1), C2p = Math.hypot(a2p, b2);
  const h1 = (((Math.atan2(b1, a1p) * 180) / Math.PI) % 360 + 360) % 360, h2 = (((Math.atan2(b2, a2p) * 180) / Math.PI) % 360 + 360) % 360;
  const dL = L2 - L1, dC = C2p - C1p; let dh = h2 - h1;
  if (dh > 180) dh -= 360; else if (dh < -180) dh += 360;
  const dH = 2 * Math.sqrt(C1p * C2p) * Math.sin(rad(dh / 2)), Lp = (L1 + L2) / 2, Cp = (C1p + C2p) / 2;
  const hp = Math.abs(h1 - h2) <= 180 ? (h1 + h2) / 2 : h1 + h2 < 360 ? (h1 + h2 + 360) / 2 : (h1 + h2 - 360) / 2;
  const T = 1 - 0.17 * Math.cos(rad(hp - 30)) + 0.24 * Math.cos(rad(2 * hp)) + 0.32 * Math.cos(rad(3 * hp + 6)) - 0.2 * Math.cos(rad(4 * hp - 63));
  const dth = 30 * Math.exp(-(((hp - 275) / 25) ** 2)), Rc = 2 * Math.sqrt(Cp ** 7 / (Cp ** 7 + 25 ** 7)), Sl = 1 + (0.015 * (Lp - 50) ** 2) / Math.sqrt(20 + (Lp - 50) ** 2);
  const Rt = -Math.sin(rad(2 * dth)) * Rc, sc = dC / (1 + 0.045 * Cp), sh = dH / (1 + 0.015 * Cp * T);
  return Math.sqrt((dL / Sl) ** 2 + sc ** 2 + sh ** 2 + Rt * sc * sh);
}
const DE = new Map<string, number>();
/** dE2000 between two families' wall colours (memoised). */
export const famDE = (p: string, q: string) => { const k = p < q ? `${p}|${q}` : `${q}|${p}`; let v = DE.get(k); if (v === undefined) DE.set(k, (v = deltaE2000(wallOf(p), wallOf(q)))); return v; };

// ---------------------------------------------------------------- hosts and street neighbours
/** The building a mass belongs to (FacadeKit.HostOf): `/podium`, `/body`, `/upper`, `/lower` dropped. */
export function hostOf(massId: string): string {
  const i = massId.lastIndexOf('/');
  return i > 0 && (['/podium', '/body', '/upper', '/lower'].includes(massId.slice(i)) || /^\/wing-\d+$/.test(massId.slice(i))) ? massId.slice(0, i) : massId; // brief 075: an old building's wings (oldblocks.ts)
}
/** The civic block's own hosts (facade.json hostAreas → `civic`): the café and the Records Office and their parts. */
export const civicHost = (id: string) => Object.entries(COLOURWAY_SPEC.hostAreas).some(([k, a]) => a === CIVIC && (id === k || id.startsWith(k + '/')));

/** Street neighbour pairs (sorted, each once): street-level frontages grouped by street, side and line (ProcGrammar.Index's key), ordered along the street;
 *  consecutive distinct hosts among `hosts` are neighbours. */
export function streetNeighbours(frontages: Frontage[], hosts: Set<string>): [string, string][] {
  const groups = new Map<string, Frontage[]>();
  for (const f of frontages) {
    if (f.level !== 'street') continue;
    const n = f.normal, a = f.a, key = `${f.street}|${Math.round(n[0])},${Math.round(n[1])}|${Math.abs(n[0]) > Math.abs(n[1]) ? Math.round(a[0]) : Math.round(a[1])}`;
    (groups.get(key) ?? groups.set(key, []).get(key)!).push(f);
  }
  const along = (f: Frontage) => ((f.a[0] + f.b[0]) / 2) * f.normal[1] - ((f.a[1] + f.b[1]) / 2) * f.normal[0];
  const pairs = new Set<string>();
  for (const k of [...groups.keys()].sort()) {
    const l = [...groups.get(k)!].sort((p, q) => along(p) - along(q)), hs: string[] = [];
    for (const f of l) if (hosts.has(f.host) && hs[hs.length - 1] !== f.host) hs.push(f.host);
    for (let i = 0; i + 1 < hs.length; i++) if (hs[i] !== hs[i + 1]) pairs.add(hs[i]! < hs[i + 1]! ? `${hs[i]}\n${hs[i + 1]}` : `${hs[i + 1]}\n${hs[i]}`);
  }
  return [...pairs].sort().map(p => p.split('\n') as [string, string]);
}

// ---------------------------------------------------------------- the pick
/** The facade.json area a building draws from: its own (the plot's) where facade.json has it, else one named in its id, else harbour (FacadeKit.Fill and Resolve). */
const areaKey = (area: string | undefined, id: string): string => (area && area !== CIVIC && COLOURWAY_SPEC.areas[area] ? area : Object.keys(COLOURWAY_SPEC.areas).find(a => a !== CIVIC && id.includes(a)) ?? 'harbour');
/** Draw `salt` of a building's family from its area's weights (salt 0 = the first pick; the repair's re-draws are salts 1..). */
export function drawFamily(id: string, area: string, salt: number): string {
  const w = COLOURWAY_SPEC.areas[area]!, names = Object.keys(w).filter(k => k !== CIVIC);
  return rng(`${id}#colourway${salt ? `#${salt}` : ''}`).weighted(names, k => w[k]!);
}

export type ColourwayLog = { buildings: number; pairs: number; redrawn: number; passes: number; families: Record<string, number>; same: number; de: { min: number; p10: number; median: number } | null };

/** Brief 075 old palette: the old DecoKitColourways.Of, by a piece's position in the old frame (plan = old (x, z - 206), the circus at the origin; read only,
 *  DecoKitCityPlan.cs): on the crescents' circle (R 35, within 1.5 m) circus; past the terrace backs (x > EastBack 46 - 6) harbour; Southside south; past the
 *  garden backs (x < -46 + 6) gardens; else the boulevard's walls and returns. Southside is read from the plan's areas (Datum's transcription names the old
 *  districts: `southside`, `westrow` the Southside fill west of the Boulevard). Used for the remap's areas that facade.json does not name (facade.json areas keep
 *  Vela's mapping: records/lamplight boulevard, lantern southside, docks/harbour harbour, crescent circus, gardens gardens). */
export const OLD_ENSEMBLE = { crescentR: 35, ring: 1.5, east: 40, west: -40, south: ['southside', 'westrow'] as string[] };
export function oldOf(p: V, area: string | undefined): string {
  if (Math.abs(Math.hypot(p[0], p[1]) - OLD_ENSEMBLE.crescentR) < OLD_ENSEMBLE.ring) return 'circus';
  if (p[0] > OLD_ENSEMBLE.east) return 'harbour';
  if (area && OLD_ENSEMBLE.south.includes(area)) return 'southside';
  if (p[0] < OLD_ENSEMBLE.west) return 'gardens';
  return 'boulevard';
}

/** The family a building takes when nothing paints it: the civic block `civic`; an area facade.json names, its ensemble (drawFamily, salt 0: the area's one
 *  family); else the old Of by its facade pieces' positions, the most of them (old HighStreet's majority; ties in family order), its ring's vertices without pieces. */
export function ensembleOf(id: string, area: string | undefined, at: V[] = [], ring: V[] = []): string {
  if (civicHost(id)) return CIVIC;
  if (area && area !== CIVIC && COLOURWAY_SPEC.areas[area]) return drawFamily(id, area, 0);
  const pts = at.length ? at : ring, n = new Map<string, number>();
  for (const p of pts) { const f = oldOf(p, area); n.set(f, (n.get(f) ?? 0) + 1); }
  const order = Object.keys(COLOURWAY_SPEC.families);
  return [...n].sort((x, y) => y[1] - x[1] || order.indexOf(x[0]) - order.indexOf(y[0]))[0]?.[0] ?? 'boulevard';
}

/** Each building's colourway: the civic block's `civic`, the old high street's Soft paint where `fixed` gives it (oldStreetFamilies), else its ensemble
 *  (ensembleOf). `blds`: id, area (the plot's) and ring of every building host; `at`: its facade pieces' positions by host. */
export function assignColourways(blds: { id: string; area?: string; ring?: V[] }[], frontages: Frontage[], fixed: Map<string, string> = new Map(), at: Map<string, V[]> = new Map()): { of: Map<string, string>; log: ColourwayLog } {
  const of = new Map<string, string>(), by = new Map<string, { area?: string; ring?: V[] }>();
  for (const b of blds) if (!by.has(b.id)) by.set(b.id, b);
  const ids = [...by.keys()].sort();
  for (const id of ids) of.set(id, civicHost(id) ? CIVIC : fixed.get(id) ?? ensembleOf(id, by.get(id)!.area, at.get(id), by.get(id)!.ring));
  const pairs = streetNeighbours(frontages, new Set(ids));
  return { of, log: { ...spreadOf(of, pairs), redrawn: 0, passes: 0 } };
}

/** The facade pieces' positions by building (ensembleOf's vote): each piece's front-centre, the middle of its wall line a-b (the old Of read the piece's pivot,
 *  "the facade's front-centre"; `kit.at` is the footprint's centre, 7 m inside a 14 m bay). */
export const piecesAt = (facades: { building: string; a: V | number[]; b: V | number[] }[]): Map<string, V[]> => {
  const m = new Map<string, V[]>();
  for (const f of facades) (m.get(f.building) ?? m.set(f.building, []).get(f.building)!).push([(f.a[0]! + f.b[0]!) / 2, (f.a[1]! + f.b[1]!) / 2]);
  return m;
};

/** The spread: families used and counts, neighbours sharing a family, neighbour wall dE2000 min, p10 and median (pairs with a civic side included). */
export function spreadOf(of: Map<string, string>, pairs: [string, string][]): Omit<ColourwayLog, 'redrawn' | 'passes'> {
  const families: Record<string, number> = {};
  for (const f of of.values()) families[f] = (families[f] ?? 0) + 1;
  const ps = pairs.filter(([p, q]) => of.has(p) && of.has(q)), d = ps.map(([p, q]) => famDE(of.get(p)!, of.get(q)!)).sort((a, b) => a - b);
  const r1 = (x: number) => Math.round(x * 10) / 10;
  return {
    buildings: of.size, pairs: ps.length, same: ps.filter(([p, q]) => of.get(p) === of.get(q)).length,
    families: Object.fromEntries(Object.entries(families).sort((a, b) => b[1] - a[1] || (a[0] < b[0] ? -1 : 1))),
    de: d.length ? { min: r1(d[0]!), p10: r1(d[Math.floor(d.length / 10)]!), median: r1(d[Math.floor(d.length / 2)]!) } : null,
  };
}

// ---------------------------------------------------------------- brief 075: the old high street (oldstreet.ts; old DecoKitColourways.HighStreet)
/** The painted buildings fronting an old-street road, by the old HighStreet rule: each road's walls walked in order (its right side, then its left,
 *  each along the road's direction, as the old east walls came before the west), a run broken where the plot changes (a side street or lane); a building is
 *  painted when the one before it in its run is not, the palette running on in order from wall to wall; the rest are not in the map (they keep their ensemble). Towers aside. */
export function oldStreetFamilies(ctx: Ctx, blds: { id: string; plot: string; tower?: boolean; ring: { p: V[]; c: string[] } }[]): Map<string, string> {
  const C = OLD_STREET.colour, walls = new Map<string, { id: string; plot: string; u: number }[]>();
  for (const b of blds) {
    if (b.tower) continue;
    const N = b.ring.p.length;
    for (let i = 0; i < N; i++) {
      const road = oldRoadOf(b.ring.c[i]!); if (!road) continue;
      const a = b.ring.p[i]!, z = b.ring.p[(i + 1) % N]!, m: V = [(a[0] + z[0]) / 2, (a[1] + z[1]) / 2], f = roadFrame(ctx, road, m); if (!f) continue;
      const key = `${OLD_STREET.roads.indexOf(road)}|${f.left ? 1 : 0}`, w = walls.get(key) ?? walls.set(key, []).get(key)!;
      if (!w.some(x => x.id === b.id)) w.push({ id: b.id, plot: b.plot, u: m[0] * f.dir[0] + m[1] * f.dir[1] });
      break; // one face per building decides its place on the wall
    }
  }
  const out = new Map<string, string>(); let k = 0;
  for (const key of [...walls.keys()].sort()) {
    let previous = false, plot = '';
    for (const w of walls.get(key)!.sort((x, y) => x.u - y.u || (x.id < y.id ? -1 : 1))) {
      if (w.plot !== plot) { previous = false; plot = w.plot; } // a new run
      previous = !previous;
      if (previous) out.set(w.id, C.palette[k++ % C.palette.length]!); // the unpainted keep their ensemble (old: e.Wall)
    }
  }
  return out;
}
