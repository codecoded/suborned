// Pier tower (Skyport Tower): a procedural stand-in for the regenerated kit piece (plan kit.regen) until it is in kit.json, then the
// kit ref `mega:skyport-tower`. Four legs astride the station portal (0 -> frame top), a portal frame over them, the stem from the frame up,
// three landing discs on cantilevers (levels[discs].tops, radii 16 / 13 / 10), the gallery collar and cupola (saucer), spire and crown.
// Lounge decks at concourse level either side of the beam; lift cores in the south-west and north-east legs. The ParabolicArch gateway
// (plan gateway) is emitted as a kit ref on the quay.
import type { Megastructure } from '../../../plan/plan';
import { type Ctx, r3, r4 } from '../common';
import type { Rect } from '../geo';
import { type MegaOut } from '../types';
import { adoptedBy } from '../kitrefs';
import { box, deckRec, host, prim, rectMass, zOf } from './kit';

/** Numbers the plan holds only in prose (kit.regen): TODO numeric fields on megastructures.skyport-tower (Datum). */
const REGEN = { frame: [30, 34] as [number, number], discs: [{ r: 16, c: [555, -17.3], thick: 4 }, { r: 13, c: [529, 5.8], thick: 3.4 }, { r: 10, c: [558.2, 4.8], thick: 2.8 }] as { r: number; c: [number, number]; thick: number }[], stem: 8, gallery: 40 };

export function pier(ctx: Ctx, m: Megastructure): MegaOut {
  const id = `mega/${m.id}`, zc = zOf(ctx, 'concourse'), td = ctx.L.structureDepth.deck, fp = m.footprint[0] as Rect, legs = (m as any).legs as Rect[];
  const [f0, f1] = (m as any).frame ?? REGEN.frame, gz = (m.levels.find(l => l.level === 'saucer')?.z ?? 89.6), [, sTop] = (m as any).saucer as [number, number];
  const tops = (m.levels.find(l => l.level === 'discs') as { tops?: number[] } | undefined)?.tops ?? [];
  const out: MegaOut = { id, parts: [], prims: [], decks: [], hosts: [], cores: [], connectors: [], avoid: [] };
  legs.forEach((l, i) => out.parts.push(rectMass(`${id}/leg-${i}`, 'mega', l, 0, f1)));
  const cx = (fp[0] + fp[1]) / 2, cy = (fp[2] + fp[3]) / 2, w = fp[1] - fp[0], d = fp[3] - fp[2], q = 6, ft = f1 - f0;
  out.decks.push(deckRec(ctx, id, `${id}/lounge-n`, 'concourse', [fp[0], fp[1], 8, fp[3]], [], td), deckRec(ctx, id, `${id}/lounge-s`, 'concourse', [fp[0], fp[1], fp[2], -8], [], td));
  // portal frame f0..f1 on all four sides leg to leg, and the cross-girder at the stem's x carrying it
  out.prims.push(box(`${id}/frame-n`, 'mega', [cx, fp[3] - q / 2], [w, q, ft], f0), box(`${id}/frame-s`, 'mega', [cx, fp[2] + q / 2], [w, q, ft], f0),
    box(`${id}/frame-e`, 'mega', [fp[1] - q / 2, cy], [q, d, ft], f0), box(`${id}/frame-w`, 'mega', [fp[0] + q / 2, cy], [q, d, ft], f0), box(`${id}/girder`, 'trim', [cx, cy], [REGEN.stem, d, ft], f0));
  // stem from the frame to the gallery; each disc hangs on a cantilever from the stem, its top at tops[i]
  out.prims.push(prim(`${id}/stem`, 'cyl', 'trim', [cx, cy], [REGEN.stem, REGEN.stem, gz - f1], f1));
  REGEN.discs.forEach((dk, i) => {
    const top = tops[i]; if (top === undefined) return;
    const dx = dk.c[0] - cx, dy = dk.c[1] - cy, len = Math.hypot(dx, dy), yaw = Math.atan2(dy, dx), base = top - dk.thick;
    out.prims.push(prim(`${id}/disc-${i}`, 'cyl', 'landmark', dk.c, [2 * dk.r, 2 * dk.r, dk.thick], base),
      box(`${id}/arm-${i}`, 'trim', [cx + dx / 2, cy + dy / 2], [len, 3, dk.thick], base, yaw));
  });
  out.prims.push(prim(`${id}/saucer`, 'saucer', 'landmark', [cx, cy], [REGEN.gallery, REGEN.gallery, sTop - gz], gz), prim(`${id}/collar`, 'cyl', 'trim', [cx, cy], [REGEN.stem, REGEN.stem, 0.6], gz - 0.6),
    box(`${id}/spire`, 'trim', [cx, cy], [2, 2, m.height - sTop], sTop), prim(`${id}/crown`, 'cyl', 'enamel', [cx, cy], [6, 6, 1], m.height - 12));
  const mixOf = (lv: string) => m.levels.find(l => l.level === lv)?.mix;
  const apron = host(`${id}/street`, 'street', ctx.pav, m.areas[0]!, mixOf('street'), [
    { a: [fp[0] + 6, 10], b: [fp[1] - 6, 10], n: [0, -1], kind: 'walk', street: 'skyport-pier', cornerA: false, cornerB: false },
    { a: [fp[1] - 6, -10], b: [fp[0] + 6, -10], n: [0, 1], kind: 'walk', street: 'skyport-pier', cornerA: false, cornerB: false }], ctx.S.sections.walk.edgeBand, true); // the apron's kiosks spill within a walk's edge band (SF-16)
  const lounge = host(`${id}/concourse`, 'concourse', zc, m.areas[0]!, mixOf('concourse'), [
    { a: [fp[0] + 2, 22], b: [fp[1] - 2, 22], n: [0, -1], kind: 'concourse', street: 'lounge', cornerA: false, cornerB: false },
    { a: [fp[1] - 2, -22], b: [fp[0] + 2, -22], n: [0, 1], kind: 'concourse', street: 'lounge', cornerA: false, cornerB: false }], 0, true);
  out.hosts.push(apron, lounge);
  // lifts in the legs `cores.legs` names (SW and NE): apron -> concourse -> saucer
  for (const l of (m as unknown as { cores: { legs: number[] } }).cores.legs.map(i => legs[i]!)) out.cores.push({ id: `${id}/lift-${l[0] < cx ? 'sw' : 'ne'}`, host: id, rect: l, from: 'street', to: 'saucer', lift: true, stair: false });
  const g = (m as any).gateway as { kit: string; c: [number, number]; feet: Rect[] } | undefined, arch = g && adoptedBy(g.kit);
  if (g && arch) out.gateway = { id: arch.id, scale: [1, 1, 1], yaw: r4(Math.PI / 2), at: [r3(g.c[0]), r3(g.c[1])] }; // spans the pier: the kit's +X (feet at +-16) turned onto y
  return out;
}
