// Shared by the generators: the seeded random source (a hash of the element id, never Math.random), rounding, and the
// context every generator reads (the plan, what `derive` made of it, the civic block).
import type { V } from '../../plan/geom';
import type { Civic } from '../../plan/civic';
import type { Derived, Plan } from '../../plan/plan';

export const r3 = (n: number) => Math.round(n * 1000) / 1000;
export const r4 = (n: number) => Math.round(n * 10000) / 10000;
export const rv = (p: V): V => [r3(p[0]), r3(p[1])];

/** FNV-1a, 32 bit. */
export function hash32(s: string): number {
  let h = 0x811c9dc5;
  for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 0x01000193); }
  return h >>> 0;
}

export type Rng = {
  f(): number; int(lo: number, hi: number): number; pick<T>(a: readonly T[]): T; chance(p: number): boolean;
  weighted<T>(items: readonly T[], w: (t: T) => number): T; shuffle<T>(a: readonly T[]): T[];
};
/** Mulberry32 seeded by the hash of `id`; the same id always gives the same stream. */
export function rng(id: string): Rng {
  let s = hash32(id) || 1;
  const f = () => { s = (s + 0x6d2b79f5) >>> 0; let t = s; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };
  const r: Rng = {
    f, int: (lo, hi) => lo + Math.floor(f() * (hi - lo + 1)), pick: a => a[Math.floor(f() * a.length)]!, chance: p => f() < p,
    weighted: (items, w) => {
      const tot = items.reduce((n, t) => n + w(t), 0); let x = f() * tot;
      for (const t of items) { x -= w(t); if (x < 0) return t; }
      return items[items.length - 1]!;
    },
    shuffle: a => { const o = [...a]; for (let i = o.length - 1; i > 0; i--) { const j = Math.floor(f() * (i + 1)); [o[i], o[j]] = [o[j]!, o[i]!]; } return o; },
  };
  return r;
}

export type Ctx = { plan: Plan; d: Derived; civ: Civic; B: any; L: any; F: any; S: any; pav: number };

/** The standards the generators read, resolved once. `pav` = pavement top (standards.buildings.datum: civic block z0 = 0.18). */
export function ctxOf(plan: Plan, d: Derived, civ: Civic, pav = 0.18): Ctx {
  const s = plan.standards;
  return { plan, d, civ, B: s.buildings, L: s.levels, F: s.frontage, S: s.streetscape, pav };
}

/** H(n) = plinth + groundStorey + (n - 1) × upperStorey + roofCrown, above the pavement top (standards.buildings.height). */
export const heightOf = (B: any, n: number) => B.plinth + B.groundStorey + (n - 1) * B.upperStorey + B.roofCrown;
/** Largest storey count whose H fits under `cap` metres. */
export const storeysUnder = (B: any, cap: number) => Math.floor((cap - B.plinth - B.groundStorey - B.roofCrown) / B.upperStorey) + 1;
