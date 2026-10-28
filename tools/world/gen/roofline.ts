// Brief 075 remap (rooftops): the old town's roof programmes, ported from ~/Projects/the-suborned/game/Assets/TheSuborned/Editor/DecoKit
// DecoKitRoofline.cs (Programme, Cuts, Pick) and DecoKitRoofThemes.cs (All, Planters, Lido .. Sun), read only.
//   RL-1 a roof splits along its wall at 6 m bay joints into plots of 12-30 m (2-5 bays a step while more than 30 m is left; the last plot keeps two bays).
//   RL-2 each plot takes a theme that fits it (weighted: lido 2, tennis 1.5, garden 2, cafe 2, glass 1.5, machine 1.5, sun 1), never the theme of the plot before it
//        where anything else fits; nothing fits: planters along its street edge.
//   RL-3 laid in the plot's frame, u along the wall, +v toward the street, the old items at true size (adopted kit pieces only; a candidate's stand-in is named in STAND_IN),
//        on a terrazzo deck just over the roof (machine plots stand on the bare roof), a lawn with a stone kerb on garden plots.
//   RL-4 lattice masts: never on an ordinary roof (Sky via Aster, 2026-10-28: "they read as clutter and cast long shadows across streets"); the machine theme's
//        Roof_LatticeMast is gone, and a hero roof (a 4-storey corner of the old frontage) may carry one slender Roof_Mast at its corner end (roofs.ts decides).
// Seeded by roof id; same plan, same roofs.
import type { Rng } from './common';

export type Item = { a: string; u: number; v: number; yaw?: number };
export type Theme = { name: string; minL: number; minD: number; weight: number; deck: boolean; lawn: boolean; accessible: boolean; fill: (l: number, d: number, r: Rng) => Item[] };

/** Evenly spaced along u from u0 to u1, about `step` apart (DecoKitRoofThemes.Along). */
export const along = (u0: number, u1: number, step: number): number[] => {
  const n = Math.max(1, Math.floor((u1 - u0) / step) + 1);
  return Array.from({ length: n }, (_, k) => (n === 1 ? (u0 + u1) / 2 : u0 + ((u1 - u0) * k) / (n - 1)));
};
const I = (a: string, u: number, v: number, yaw = 0): Item => ({ a, u, v, yaw });
const S = [-1, 1] as const;

/** A lap pool (loungers and parasols are part of the piece), cabanas at its ends when there is room. The old pivot sat 1.35 m off the pool's footprint centre; here the footprint is centred. */
function lido(l: number, d: number, r: Rng): Item[] {
  const out = [I('Roof_LapPool', 0, 0)];
  for (const s of S) {
    if (l >= 26) out.push(I('Roof_Cabana', s * (l / 2 - 2.2), d / 2 - 2.2), I('Roof_Cabana', s * (l / 2 - 2.2), -d / 2 + 2.2, 180));
    if (l >= 21) out.push(I('PottedPalm_Small', s * (l / 2 - 1.1), 0, r.int(0, 359)));
  }
  return out;
}
function tennis(l: number, d: number, r: Rng): Item[] {
  const out = [I('Roof_Tennis', 0, 0)];
  if (l < 29) return out;
  for (const s of S) out.push(I('Bench', s * (l / 2 - 0.9), 0, s * 90), I('PottedPalm_Small', s * (l / 2 - 1.1), d / 2 - 1.3, r.int(0, 359)));
  return out;
}
/** A lawn with a bandstand at its heart (a round bench on a short one), palms at its ends, benches facing it from the street edge. */
function garden(l: number, d: number, r: Rng): Item[] {
  const out = [l >= 18 ? I('Roof_Bandstand', 0, 0, r.int(0, 7) * 45) : I('RoundBench', 0, 0)];
  for (const s of S) {
    out.push(I('CoconutPalm', s * (l / 2 - 2.6), s * 2.2, r.int(0, 359)));
    if (l >= 26) out.push(I('FanPalm', s * (l / 2 - 6.8), -s * 3.5, r.int(0, 359)));
    out.push(I('Bench', s * Math.max(4.8, l * 0.25), d / 2 - 2, 180), I('PottedTopiary', s * Math.min(4.6, l / 2 - 2.4), -d / 2 + 2.2));
  }
  return out;
}
/** A kiosk bar at one end, cabanas along the back, café sets toward the street, potted palms at the corners. */
function cafe(l: number, d: number, r: Rng): Item[] {
  const out: Item[] = [];
  let u0 = -l / 2 + 2.2;
  if (l >= 20) { out.push(I('CurvedKiosk', -l / 2 + 3.4, -0.5, 90)); u0 += 5.6; }
  for (const u of along(u0, l / 2 - 2.2, 4.6)) out.push(I('Roof_Cabana', u, -d / 2 + 2.1));
  for (const v of [0.6, 3.6]) for (const u of along(u0 + 0.4, l / 2 - 2.4, 3.3)) out.push(I('CafeSet_Tulip', u, v, r.int(0, 5) * 30));
  for (const s of S) out.push(I('PottedPalm_Small', s * (l / 2 - 1.1), d / 2 - 1.2, r.int(0, 359)));
  return out;
}
/** A glasshouse, planters on the street edge, a round bench at each end when there is room. */
function glass(l: number, d: number): Item[] {
  const out = [I('Roof_Conservatory', 0, -0.8)];
  for (const s of S) {
    if (l >= 23) out.push(I('RoundBench', s * (l / 2 - 2.6), -0.5));
    if (l >= 20) out.push(I('Roof_Planter', s * (l / 2 - 4), d / 2 - 1));
    if (l >= 17) out.push(I('PottedTopiary', s * (l / 2 - 1), -d / 2 + 1.4));
  }
  if (l < 20) out.push(I('Roof_Planter', 0, d / 2 - 1));
  return out;
}
/** Plant on the bare roof: dishes, radomes, tanks and drums along the plot (RL-4: no lattice mast; a hero roof's Roof_Mast leads its corner plot). */
export const MACHINE: [string, number][] = [['Roof_Radome', 6.3], ['Roof_Dish', 6.6], ['Roof_Tank', 4.8], ['Roof_Drum', 7.4]];
function machine(l: number, _d: number, r: Rng, mast = false): Item[] {
  const out: Item[] = [];
  let u = -l / 2 + 0.8;
  for (let k = 0; ; k++) {
    const [a, w] = mast && k === 0 ? ['Roof_Mast', 3.7] as [string, number] : MACHINE[r.int(0, MACHINE.length - 1)]!;
    if (u + w > l / 2 - 0.8) return out;
    out.push(I(a, u + w / 2, k & 1 ? 1 : -1.2, r.int(0, 3) * 90));
    u += w + 1.4 + r.f() * 2;
  }
}
/** A sun deck: deckchairs facing the street, troughs along the back, palms at the ends. */
function sun(l: number, d: number, r: Rng): Item[] {
  const out: Item[] = [];
  for (const u of along(-l / 2 + 1.4, l / 2 - 1.4, 1.5)) out.push(I('Deckchair', u, 0.8));
  for (const u of along(-l / 2 + 1.6, l / 2 - 1.6, 3.4)) out.push(I('Planter_Trough', u, -d / 2 + 0.8));
  for (const s of S) out.push(I('PottedPalm_Small', s * (l / 2 - 0.9), d / 2 - 1.2, r.int(0, 359)));
  return out;
}

/** DecoKitRoofThemes.All: name, the plot it needs (l along the wall, d across), weight. */
export const THEMES: Theme[] = [
  { name: 'lido', minL: 17, minD: 11, weight: 2, deck: true, lawn: false, accessible: true, fill: lido },
  { name: 'tennis', minL: 27, minD: 12.9, weight: 1.5, deck: true, lawn: false, accessible: true, fill: tennis },
  { name: 'garden', minL: 11, minD: 9, weight: 2, deck: true, lawn: true, accessible: true, fill: garden },
  { name: 'cafe', minL: 11, minD: 10, weight: 2, deck: true, lawn: false, accessible: true, fill: cafe },
  { name: 'glass', minL: 14, minD: 10, weight: 1.5, deck: true, lawn: false, accessible: true, fill: glass },
  { name: 'machine', minL: 5, minD: 7, weight: 1.5, deck: false, lawn: false, accessible: false, fill: (l, d, r) => machine(l, d, r) },
  { name: 'sun', minL: 5, minD: 6, weight: 1, deck: true, lawn: false, accessible: true, fill: sun },
];
/** A plot nothing fits (DecoKitRoofThemes.Planters): troughs along its street edge. */
export const PLANTERS: Theme = { name: 'planters', minL: 0, minD: 0, weight: 0, deck: true, lawn: false, accessible: true, fill: (l, d) => along(-l / 2 + 2, l / 2 - 2, 4.5).map(u => I('Planter_Trough', u, d / 2 - 0.7)) };
/** A hero roof's corner plot: the slender Roof_Mast first, then plant (RL-4). */
export const MAST: Theme = { name: 'mast', minL: 5, minD: 7, weight: 0, deck: false, lawn: false, accessible: false, fill: (l, d, r) => machine(l, d, r, true) };
export const THEME_BY_NAME = new Map([...THEMES, PLANTERS, MAST].map(t => [t.name, t]));

/** Old roof pieces our kit has only as candidates: the adopted piece that stands in (Vela is checking them, briefs/075/notes.md "Remap: roofs"). */
export const STAND_IN: Record<string, string> = { Roof_Bandstand: 'RoundBench', Roof_Radome: 'Roof_Tank', Roof_Dish: 'Roof_Tank', Roof_Drum: 'Roof_Tank' };

/** RL-1 (DecoKitRoofline.Cuts): plot boundaries from u0 to u1, bay joints 2-5 bays apart, at least two bays left for the last plot. */
export function cuts(u0: number, u1: number, r: Rng): number[] {
  const out = [u0];
  for (let at = u0; u1 - at > 30;) {
    at += 6 * r.int(2, Math.min(5, Math.round((u1 - at) / 6) - 2));
    out.push(at);
  }
  out.push(u1);
  return out;
}

/** RL-2 (DecoKitRoofline.Pick): a theme that fits the plot (weighted) and whose lead piece stands in it, unlike the plot before it where anything else fits. */
export function pick(l: number, d: number, prev: Theme | null, r: Rng, leads: (t: Theme) => boolean): Theme {
  let fit = THEMES.filter(t => l >= t.minL && d >= t.minD && leads(t));
  if (fit.length > 1 && prev) fit = fit.filter(t => t !== prev);
  if (!fit.length) return PLANTERS;
  return r.weighted(fit, t => t.weight);
}
