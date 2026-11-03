// Brief 072-C (Aster's ruling 2): a tenancy's own shopfront box standing in front of an every-face piece (SideBay, SideBayStack_Base, SidePilaster,
// CornerPier; and, for a tenancy no shop filler is linked to, ShopFill / ShopStack_Base) stops just under that piece's teal fascia band, so its top is never coplanar with the fascia top (a z-fight) and never cuts the band.
// The fascia is the ground storey's top 1.0 m (tools/decokit/sides.py mod_base): its foot is 4.0 m up a KIT piece (_3 / _4, g 5.0) and 4.65 m up a
// PLAN piece (_Base, g 5.65), times the piece's sy. The canopy keeps its place under the box top (0.8 below it). A service bay keeps its
// 4.6 m roller (F-4): it stands 0.7 m off the line, in front of the fascia, so it may rise past the clamped box top without touching the band.
import type { Facade, Frontage, Prim } from '../../../contracts/world/world.schema';
import { r3 } from './common';

/** Clearance between the box top and the fascia foot (m). */
export const FASCIA_CLEAR = 0.02;
/** A shopfront box lower than this no longer reads as a shopfront: it is logged (never forced). */
export const MIN_FRONT = 3.2;
const GROUND = /^(SideBay_[34]|SideBayStack_Base|SidePilaster_(?:[34]|Base)|CornerPier_(?:Out|In)_(?:[34]|Base)|ShopFill_[34]|ShopStack_Base)$/;
/** The fascia band [foot, top] of a ground piece at sy 1: KIT 4.0-5.0, PLAN 4.65-5.65; the shop fillers (pieces.md Pass 2): ShopFill 4.0-5.0, ShopStack_Base 4.172-5.65. */
const fasciaOf = (id: string): [number, number] => (id === 'ShopStack_Base' ? [4.172, 5.65] : /_[34]$/.test(id) ? [4.0, 5.0] : [4.65, 5.65]);
const LEG = 1.0; // a pier takes 1.0 m of each face from the vertex (sides.py LEG)

export type ShopFitLog = { clamped: number; low: { id: string; piece: string; height: number }[] };

export function fitShopfronts(fronts: Frontage[], facades: Facade[]): { frontages: Frontage[]; log: ShopFitLog } {
  const by = new Map<string, Facade[]>();
  for (const f of facades) if (GROUND.test(f.kit.id)) (by.get(f.building) ?? by.set(f.building, []).get(f.building)!).push(f);
  const log: ShopFitLog = { clamped: 0, low: [] };
  const frontages = fronts.map(t => {
    if (t.facade || t.level !== 'street') return t;
    const L = Math.hypot(t.b[0] - t.a[0], t.b[1] - t.a[1]) || 1, u = [(t.b[0] - t.a[0]) / L, (t.b[1] - t.a[1]) / L], n = t.normal;
    const z0 = t.front.base, z1 = z0 + t.front.size[2];
    const off = (p: number[]) => (p[0]! - t.a[0]) * n[0] + (p[1]! - t.a[1]) * n[1], al = (p: number[]) => (p[0]! - t.a[0]) * u[0]! + (p[1]! - t.a[1]) * u[1]!;
    let top = Infinity, piece = '';
    for (const f of by.get(t.host) ?? []) {
      if (Math.abs(off(f.a)) > 1.2 || Math.abs(off(f.b)) > 1.2) continue; // not on the tenancy's line
      const leg = f.form === 'pier' ? LEG : 0, s0 = Math.min(al(f.a), al(f.b)) - leg, s1 = Math.max(al(f.a), al(f.b)) + leg;
      if (s1 <= 0.01 || s0 >= L - 0.01) continue; // not behind it along the line
      const sy = f.kit.scale[1], [f0, f1] = fasciaOf(f.kit.id), foot = f.base + f0 * sy, fTop = f.base + f1 * sy;
      if (fTop < z0 || f.base > z1) continue; // not the ground storey behind this box
      if (foot - FASCIA_CLEAR < top) { top = foot - FASCIA_CLEAR; piece = f.kit.id; }
    }
    if (!(top < z1 - 1e-6)) return t;
    const h = r3(top - z0);
    if (h < MIN_FRONT) { log.low.push({ id: t.id, piece, height: h }); return t; }
    log.clamped++;
    const dz = z1 - (z0 + h), size = (p: Prim, s2: number): Prim => ({ ...p, size: [p.size[0], p.size[1], r3(s2)] });
    return {
      ...t,
      front: size(t.front, h),
      canopy: { ...t.canopy, base: r3(t.canopy.base - dz) },
    };
  });
  return { frontages, log };
}
