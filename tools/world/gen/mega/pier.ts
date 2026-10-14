// Pier tower (Skyport Tower): a procedural stand-in for the regenerated kit piece (plan kit.regen) until it is in kit.json, then the
// kit ref `mega:skyport-tower`. Four legs astride the station portal (0 -> frame top), a portal frame over them, the stem from the frame up,
// three landing discs on cantilevers (levels[discs].tops, radii 16 / 13 / 10), the gallery collar and cupola (saucer), spire and crown.
// Lounge decks at concourse level either side of the beam. Lifts as the plan's connectors say: cores in the south-west and north-east
// legs (apron -> concourse -> frame top), then the stem's glazed lift tubes (frame -> each disc, the last on to the gallery) hugging the
// stem at the kit's LIFT_R, so every lift stands against a structure its whole run (L-7). The ParabolicArch gateway (plan gateway) is a
// kit ref on the quay. Sizes are the plan's numbers (stem, girder, discs, gallery, crown), as the regenerated kit piece was built.
import type { Megastructure } from '../../../plan/plan';
import { type Ctx, r3, r4, rv } from '../common';
import { type Rect, rectPts } from '../geo';
import { type MegaOut } from '../types';
import type { V } from '../../../plan/geom';
import { adoptedBy } from '../kitrefs';
import { GIRDER } from '../surfaces';
import { box, deckRec, host, prim, rectMass, zOf } from './kit';

/** The kit's stem lift tubes (tools/decokit skyport.py LIFT_R 9.2, lift_tube r 1.5, one per disc at its azimuth, kit +X = plan +x at yaw 0). TODO numeric field on
 *  megastructures.skyport-tower (Datum): the plan holds them in prose (kit.regen.lifts "the three stem lift tubes (LIFT_R 9.2)"). */
export const LIFT_TUBE = { r: 9.2, radius: 1.5 };
type Disc = { c: [number, number]; az: number; r: number; top: number; thick: number; soffit: number };
type Pier = Megastructure & { legs: Rect[]; frame: [number, number]; frameWidth: number; girder: Rect; saucer: [number, number]; stem: { r: number; from: number; to: number; platform: { r: number; z: [number, number] } };
  discs: Disc[]; gallery: { z: number; top: number; r: number; rIn: number }; crown: { cupola: { z: [number, number]; r: number }; mast: { z: [number, number]; w: number }; needle: { z: [number, number]; r: number }; tip: number };
  cores: { legs: number[] } };
/** A circle as a 12-gon (lift tubes: the connector ring is the tube's plan). */
export const circleRing = (c: V, r: number, n = 12): V[] => Array.from({ length: n }, (_, k) => [r3(c[0] + r * Math.cos((2 * Math.PI * k) / n)), r3(c[1] + r * Math.sin((2 * Math.PI * k) / n))] as V);

export function pier(ctx: Ctx, mm: Megastructure): MegaOut {
  const m = mm as Pier, id = `mega/${m.id}`, zc = zOf(ctx, 'concourse'), td = ctx.L.structureDepth.deck, fp = m.footprint[0] as Rect, legs = m.legs;
  const [f0, f1] = m.frame, st = m.stem, gal = m.gallery, cr = m.crown;
  const out: MegaOut = { id, parts: [], prims: [], decks: [], hosts: [], cores: [], connectors: [], avoid: [] };
  legs.forEach((l, i) => out.parts.push(rectMass(`${id}/leg-${i}`, 'mega', l, 0, f1)));
  const cx = (fp[0] + fp[1]) / 2, cy = (fp[2] + fp[3]) / 2, w = fp[1] - fp[0], d = fp[3] - fp[2], q = m.frameWidth, ft = f1 - f0, g = m.girder;
  out.decks.push(deckRec(ctx, id, `${id}/lounge-n`, 'concourse', [fp[0], fp[1], 8, fp[3]], [], td), deckRec(ctx, id, `${id}/lounge-s`, 'concourse', [fp[0], fp[1], fp[2], -8], [], td));
  // portal frame f0..f1 on all four sides leg to leg, and the cross-girder carrying the stem
  out.prims.push(box(`${id}/frame-n`, 'mega', [cx, fp[3] - q / 2], [w, q, ft], f0), box(`${id}/frame-s`, 'mega', [cx, fp[2] + q / 2], [w, q, ft], f0),
    box(`${id}/frame-e`, 'mega', [fp[1] - q / 2, cy], [q, d, ft], f0), box(`${id}/frame-w`, 'mega', [fp[0] + q / 2, cy], [q, d, ft], f0), box(`${id}/girder`, GIRDER, [(g[0] + g[1]) / 2, (g[2] + g[3]) / 2], [g[1] - g[0], g[3] - g[2], ft], f0));
  // the stem on its platform from the frame to the shaft top; each disc on a cantilever from the stem, its deck top at disc.top
  out.prims.push(prim(`${id}/stem-platform`, 'cyl', 'mega', [cx, cy], [2 * st.platform.r, 2 * st.platform.r, st.platform.z[1] - st.platform.z[0]], st.platform.z[0]),
    prim(`${id}/stem`, 'cyl', 'limestone', [cx, cy], [2 * st.r, 2 * st.r, st.to - st.from], st.from));
  m.discs.forEach((dk, i) => {
    const dx = dk.c[0] - cx, dy = dk.c[1] - cy, len = Math.hypot(dx, dy), yaw = Math.atan2(dy, dx);
    out.prims.push(prim(`${id}/disc-${i}`, 'cyl', 'landmark', dk.c, [2 * dk.r, 2 * dk.r, dk.thick], dk.soffit),
      box(`${id}/arm-${i}`, GIRDER, [cx + dx / 2, cy + dy / 2], [len, 3, dk.thick], dk.soffit, yaw));
  });
  // gallery collar (lens r rIn -> r), cupola, lattice mast and needle to the tip (the kit's crown(), not a 40 m saucer)
  out.prims.push(prim(`${id}/gallery`, 'saucer', 'landmark', [cx, cy], [2 * gal.r, 2 * gal.r, gal.top - gal.z], gal.z), prim(`${id}/cupola`, 'cyl', 'glass', [cx, cy], [2 * cr.cupola.r, 2 * cr.cupola.r, cr.cupola.z[1] - cr.cupola.z[0]], cr.cupola.z[0]),
    box(`${id}/mast`, GIRDER, [cx, cy], [cr.mast.w, cr.mast.w, cr.mast.z[1] - cr.mast.z[0]], cr.mast.z[0]), prim(`${id}/needle`, 'cyl', 'trim', [cx, cy], [2 * cr.needle.r, 2 * cr.needle.r, cr.needle.z[1] - cr.needle.z[0]], cr.needle.z[0]));
  // lifts: a car in each named leg from the apron to the frame top (it stops at the concourse lounge), then a glazed tube on the stem platform from the frame
  // top to each disc's deck (the last, the highest disc's, runs on to the gallery): the tube hugs the stem (gap LIFT_TUBE.r - radius - stem.r, bridged by a spine)
  const car = ctx.L.connectors.lift.car as [number, number], top = Math.max(...m.discs.map(x => x.top));
  for (const li of m.cores.legs) {
    const l = legs[li]!, lr: Rect = [l[0], l[0] + car[0], l[2], l[2] + car[1]], c: V = [(lr[0] + lr[1]) / 2, (lr[2] + lr[3]) / 2];
    out.connectors.push({ id: `${id}/lift-${l[0] < cx ? 'sw' : 'ne'}`, kind: 'lift', host: id, serves: [], from: 'street', to: 'frame', foot: rv(c), head: rv(c), bottom: r3(ctx.pav), top: r3(f1), width: car[0], stepFree: true, ring: rectPts(lr).map(rv), yaw: 0 });
  }
  m.discs.forEach((dk, i) => {
    const a = (dk.az * Math.PI) / 180, c: V = [cx + LIFT_TUBE.r * Math.cos(a), cy + LIFT_TUBE.r * Math.sin(a)], last = dk.top === top;
    const sr = (st.r + LIFT_TUBE.r - LIFT_TUBE.radius) / 2, sl = LIFT_TUBE.r - LIFT_TUBE.radius - st.r + 0.3; // a chrome spine ties the tube to the stem (the kit's), its whole run
    out.prims.push(box(`${id}/tube-${i}-spine`, GIRDER, [cx + sr * Math.cos(a), cy + sr * Math.sin(a)], [sl, 0.7, (last ? gal.z : dk.top) - f1], f1, a));
    out.connectors.push({ id: `${id}/tube-${i}`, kind: 'lift', host: id, serves: [], from: 'frame', to: last ? 'saucer' : 'discs', foot: rv(c), head: rv(c), bottom: r3(f1), top: r3(last ? gal.z : dk.top), width: r3(2 * LIFT_TUBE.radius), stepFree: true, ring: circleRing(c, LIFT_TUBE.radius), yaw: r4(a) });
  });
  const mixOf = (lv: string) => m.levels.find(l => l.level === lv)?.mix;
  const apron = host(`${id}/street`, 'street', ctx.pav, m.areas[0]!, mixOf('street'), [
    { a: [fp[0] + 6, 10], b: [fp[1] - 6, 10], n: [0, -1], kind: 'walk', street: 'skyport-pier', cornerA: false, cornerB: false },
    { a: [fp[1] - 6, -10], b: [fp[0] + 6, -10], n: [0, 1], kind: 'walk', street: 'skyport-pier', cornerA: false, cornerB: false }], ctx.S.sections.walk.edgeBand, true); // the apron's kiosks spill within a walk's edge band (SF-16)
  const lounge = host(`${id}/concourse`, 'concourse', zc, m.areas[0]!, mixOf('concourse'), [
    { a: [fp[0] + 2, 22], b: [fp[1] - 2, 22], n: [0, -1], kind: 'concourse', street: 'lounge', cornerA: false, cornerB: false },
    { a: [fp[1] - 2, -22], b: [fp[0] + 2, -22], n: [0, 1], kind: 'concourse', street: 'lounge', cornerA: false, cornerB: false }], 0, true);
  out.hosts.push(apron, lounge);
  const gw = (m as any).gateway as { kit: string; c: [number, number]; feet: Rect[] } | undefined, arch = gw && adoptedBy(gw.kit);
  if (gw && arch) out.gateway = { id: arch.id, scale: [1, 1, 1], yaw: r4(Math.PI / 2), at: [r3(gw.c[0]), r3(gw.c[1])] }; // spans the pier: the kit's +X (feet at +-16) turned onto y
  return out;
}
