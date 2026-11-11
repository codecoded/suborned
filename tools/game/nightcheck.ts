// Brief 107 (Glint's finding: the batch's night shot was daylight, "mean colour 176/147/112 in both"): every shot named `<x>-night` with a `<x>-day`
// in the same folder must be darker by more than 10 % of the day's mean brightness, or the step fails. A quick guard against night quietly drawn as day;
// the look itself is Glint's and Sky's to judge. Brightness: Rec. 709 luma of the PNG's RGB, mean over every pixel.
import { existsSync, readFileSync, readdirSync } from 'node:fs';
import { join } from 'node:path';
import { decodePng, type Image } from '../hud-budget/png';

export const NIGHT_MIN_DROP = 0.10;

export function meanLuma(im: Image): number {
  let s = 0; const n = im.w * im.h;
  for (let i = 0; i < n; i++) { const o = i * 4; s += 0.2126 * im.data[o]! + 0.7152 * im.data[o + 1]! + 0.0722 * im.data[o + 2]!; }
  return n ? s / n : 0;
}

export type NightPair = { name: string; day: number; night: number; drop: number; ok: boolean };

/** The night/day pairs among `names` (or every *-night.png in `dir`), each judged. */
export function nightPairs(dir: string, names?: string[], read: (f: string) => Image = f => decodePng(readFileSync(f))): NightPair[] {
  const list = names ?? (existsSync(dir) ? readdirSync(dir).filter(f => /-night\.png$/.test(f)).map(f => f.replace(/\.png$/, '')) : []);
  const out: NightPair[] = [];
  for (const n of list.filter(x => x.endsWith('-night'))) {
    const dayName = n.replace(/-night$/, '-day'), nf = join(dir, n + '.png'), df = join(dir, dayName + '.png');
    if (!existsSync(nf) || !existsSync(df)) continue;
    const day = meanLuma(read(df)), night = meanLuma(read(nf)), drop = day > 0 ? (day - night) / day : 0;
    out.push({ name: n, day, night, drop, ok: drop > NIGHT_MIN_DROP });
  }
  return out;
}

export const nightText = (p: NightPair): string => `${p.name}: mean ${p.night.toFixed(1)} against its day ${p.day.toFixed(1)} (${(p.drop * 100).toFixed(1)} % darker${p.ok ? '' : `; must be over ${NIGHT_MIN_DROP * 100} %: night drawn as day?`})`;

/** A shots step's result with the night pairs judged: a line for each pair, and a failure when any night is within 10 % of its day. */
export function withNights<T extends { ok: boolean; detail: string }>(j: T, dir: string, names: string[]): T {
  const p = nightPairs(dir, names); if (!p.length) return j;
  return { ...j, ok: j.ok && p.every(x => x.ok), detail: `${j.detail}\nnight vs day: ${p.map(nightText).join('; ')}` };
}
