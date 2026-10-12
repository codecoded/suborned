// Colonnades (standards.buildings.colonnade): a megastructure's ground floor stands on the building line (plot edge set
// back by the section's pavement); upper floors and decks overhang the pavement to the plot edge only where its
// `colonnade[]` lists the edge. The walking zone stays clear below `clearHeight`; columns stand in the kerb zone only.
import type { Megastructure } from '../../../plan/plan';
import { type V, lineDist } from '../../../plan/geom';
import type { Mass, Prim } from '../../../../contracts/world/world.schema';
import { type Ctx, r3 } from '../common';
import { type Rect, bbox, subtractRect } from '../geo';
import { box, rectMass } from './kit';

export type Colonnade = { footprint: number; edge: 'x0' | 'x1' | 'y0' | 'y1'; faces: string; class: string; depth: number; over: string; clearHeight: number; columnZone: string; columnEvery: number };
export const colonnadeOf = (m: Megastructure): Colonnade[] => ((m as { colonnade?: Colonnade[] }).colonnade ?? []);

/** The pavement slab under the overhang: `depth` metres from the footprint's plot edge inward along `edge`. */
export function stripOf(fp: Rect, c: Colonnade): Rect {
  const [x0, x1, y0, y1] = fp, d = c.depth;
  return c.edge === 'x0' ? [x0, x0 + d, y0, y1] : c.edge === 'x1' ? [x1 - d, x1, y0, y1] : c.edge === 'y0' ? [x0, x1, y0, y0 + d] : [x0, x1, y1 - d, y1];
}
/** A strip with the absolute z its overhang starts at (pavement top + clearHeight). */
export type Strip = { r: Rect; clear: number };
export const stripsOf = (ctx: Ctx, m: Megastructure, fpIndex: number): Strip[] =>
  colonnadeOf(m).filter(c => c.footprint === fpIndex).map(c => ({ r: stripOf(m.footprint[fpIndex] as Rect, c), clear: r3(ctx.pav + c.clearHeight) }));

/** `r` (a part of footprint `fpIndex`) with every colonnade edge moved from the plot edge in to the building line, where the part reaches that edge. */
export function groundRect(m: Megastructure, fpIndex: number, r: Rect, tol = 0.35): Rect {
  const fp = m.footprint[fpIndex] as Rect, q: Rect = [...r];
  for (const c of colonnadeOf(m).filter(x => x.footprint === fpIndex)) {
    const i = c.edge === 'x0' ? 0 : c.edge === 'x1' ? 1 : c.edge === 'y0' ? 2 : 3, sgn = i % 2 === 0 ? 1 : -1;
    if (Math.abs(r[i]! - fp[i]!) <= tol) q[i] = fp[i]! + sgn * c.depth;
  }
  return q;
}

/** The section's kerb and walk zones inside the strip, measured from the plot edge: kerb [0, kerb], walk [kerb, kerb + walk]. */
export function zonesOf(ctx: Ctx, fp: Rect, c: Colonnade): { kerb: Rect; walk: Rect } {
  const s = ctx.S.sections[c.class] as { kerb: number; walk: number }, whole = stripOf(fp, { ...c, depth: s.kerb + s.walk });
  return { kerb: stripOf(fp, { ...c, depth: s.kerb }), walk: carve(whole, c, s.kerb) };
}
const carve = (r: Rect, c: Colonnade, d: number): Rect => (c.edge === 'x0' ? [r[0] + d, r[1], r[2], r[3]] : c.edge === 'x1' ? [r[0], r[1] - d, r[2], r[3]] : c.edge === 'y0' ? [r[0], r[1], r[2] + d, r[3]] : [r[0], r[1], r[2], r[3] - d]);

const area = (r: Rect) => Math.max(0, r[1] - r[0]) * Math.max(0, r[3] - r[2]);
const cut = (r: Rect, s: Rect): Rect[] => subtractRect({ p: [[r[0], r[2]], [r[1], r[2]], [r[1], r[3]], [r[0], r[3]]], c: ['x', 'x', 'x', 'x'] }, s).map(q => bbox(q.p));
const meet = (r: Rect, s: Rect): Rect | null => { const q: Rect = [Math.max(r[0], s[0]), Math.min(r[1], s[1]), Math.max(r[2], s[2]), Math.min(r[3], s[3])]; return q[1] - q[0] > 1e-6 && q[3] - q[2] > 1e-6 ? q : null; };

/** A solid rect from `base` to `top` (absolute z) split by the colonnade strips: the part outside them stands from `base`, the part
 *  over them only from the strip's clear height. A solid that misses every strip keeps its id. */
export function lifted(id: string, surface: string, r: Rect, base: number, top: number, strips: Strip[]): Mass[] {
  if (!strips.some(s => meet(r, s.r))) return [rectMass(id, surface, r, base, top)];
  let ground: Rect[] = [r];
  for (const s of strips) ground = ground.flatMap(g => cut(g, s.r));
  const out: Mass[] = ground.filter(g => area(g) > 0.01).map((g, k) => rectMass(`${id}/g${k}`, surface, g, base, top));
  strips.forEach((s, i) => {
    const o = meet(r, s.r), lo = Math.max(base, s.clear);
    if (!o || top - lo <= 0.01) return;
    let parts: Rect[] = [o]; // a corner under two strips is lifted once
    for (const e of strips.slice(0, i)) parts = parts.flatMap(q => cut(q, e.r));
    parts.filter(q => area(q) > 0.01).forEach((q, k) => out.push(rectMass(`${id}/o${i}${parts.length > 1 ? `-${k}` : ''}`, surface, q, lo, top)));
  });
  return out;
}

/** Column prims along every colonnade edge: centres `columnEvery` apart (evenly spread), 0.45 m kerb offset + half a column from the plot edge. */
export function columnsOf(ctx: Ctx, m: Megastructure, id: string, skip: (p: V) => boolean = () => false): { prims: Prim[]; pts: V[] } {
  const size = ctx.L.column.size as number, off = 0.45 + size / 2, prims: Prim[] = [], pts: V[] = [], roads = ctx.d.roads.filter(r => !r.gyratory);
  colonnadeOf(m).forEach((c, ci) => {
    const [x0, x1, y0, y1] = m.footprint[c.footprint] as Rect, alongX = c.edge === 'y0' || c.edge === 'y1', lo = alongX ? x0 : y0, hi = alongX ? x1 : y1;
    const fixed = c.edge === 'x0' ? x0 + off : c.edge === 'x1' ? x1 - off : c.edge === 'y0' ? y0 + off : y1 - off;
    const n = Math.max(1, Math.round((hi - lo) / c.columnEvery)), step = (hi - lo) / n;
    for (let k = 0; k < n; k++) {
      const t = lo + (k + 0.5) * step, p: V = alongX ? [t, fixed] : [fixed, t];
      if (skip(p) || roads.some(r => lineDist(p, r.line) - r.w / 2 < 0.45 + size / 2 - 0.001)) continue; // columns stand ≥ 0.45 m from every kerb face (a crossing street's too)
      pts.push(p); prims.push(box(`${id}/colonnade-${ci}-col-${k}`, 'column', p, [size, size, c.clearHeight], ctx.pav));
    }
  });
  return { prims, pts };
}
