// Building colourways (brief 074, Sky 2026-10-28: "Do building colors"). Each building takes one wall family from facade.json `families` (the 12 district
// colourways; `civic` is the civic block's own and never drawn here), picked by its area's family weights (`areas[area].families`), seeded by the building id.
// Street neighbours (the hosts met in walking order along one side of one street, as Vela's briefs/072/style/applied2/src/spread.py and Unity's
// ProcGrammar.Index build them) never share a family, and a building whose wall is within COLOURWAY.floor dE2000 of a neighbour's is re-drawn
// (salts #1..#tries, in id order, passes until nothing changes): deterministic, never by hand. The export writes the family on every mass of the building
// (`buildings[].colourway`) and on every facade piece of it (`facades[].colourway`); CW-1 (rules/colourways.ts) checks both.
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Frontage } from '../../../contracts/world/world.schema';
import { rng } from './common';

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
/** The district colourways: every family but the civic block's. */
export const FAMILIES = Object.keys(COLOURWAY_SPEC.families).filter(k => k !== CIVIC);
export const wallOf = (fam: string) => COLOURWAY_SPEC.families[fam]![0]!;

export const COLOURWAY = {
  floor: 15, // dE2000: a building closer than this to any street neighbour's wall is re-drawn (the median then lands near 27; 072's push had 22.1)
  tries: 40, // re-draw salts per building per pass
  passes: 8,
};

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
  return i > 0 && ['/podium', '/body', '/upper', '/lower'].includes(massId.slice(i)) ? massId.slice(0, i) : massId;
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

/** Each building's colourway (the civic block's `civic`), then the neighbour repair. `blds`: id and area (facade.json area key) of every building host. */
export function assignColourways(blds: { id: string; area?: string }[], frontages: Frontage[]): { of: Map<string, string>; log: ColourwayLog } {
  const of = new Map<string, string>(), area = new Map<string, string>();
  const ids = [...new Set(blds.map(b => b.id))].sort();
  for (const b of blds) if (!area.has(b.id)) area.set(b.id, areaKey(b.area, b.id));
  for (const id of ids) of.set(id, civicHost(id) ? CIVIC : drawFamily(id, area.get(id)!, 0));
  const pairs = streetNeighbours(frontages, new Set(ids)), nb = new Map<string, string[]>();
  for (const [p, q] of pairs) { (nb.get(p) ?? nb.set(p, []).get(p)!).push(q); (nb.get(q) ?? nb.set(q, []).get(q)!).push(p); }
  // a building's worst neighbour: -1 sharing a family, else the smallest wall dE
  const score = (id: string, fam: string) => Math.min(Infinity, ...(nb.get(id) ?? []).map(o => (of.get(o) === fam ? -1 : famDE(fam, of.get(o)!))));
  const changed = new Set<string>(); let passes = 0;
  for (; passes < COLOURWAY.passes; passes++) {
    let n = 0;
    for (const id of ids) {
      if (of.get(id) === CIVIC) continue;
      let best = of.get(id)!, bs = score(id, best);
      if (bs >= COLOURWAY.floor) continue;
      for (let s = 1; s <= COLOURWAY.tries; s++) {
        const f = drawFamily(id, area.get(id)!, s + passes * COLOURWAY.tries), sc = score(id, f);
        if (sc > bs) { best = f; bs = sc; }
        if (sc >= COLOURWAY.floor) break;
      }
      // the area's weights could not part it from a neighbour: any district family that does (weights aside), the first in facade.json order
      if (bs < 0) for (const f of FAMILIES) { const sc = score(id, f); if (sc > bs) { best = f; bs = sc; } }
      if (best !== of.get(id)) { of.set(id, best); changed.add(id); n++; }
    }
    if (!n) break;
  }
  return { of, log: { ...spreadOf(of, pairs), redrawn: changed.size, passes: passes + 1 } };
}

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
