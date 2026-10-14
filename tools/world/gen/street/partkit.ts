// Kit pieces for a crossing's parts (brief 009 B; Vela's proposal out/decokit-009/catalogue.proposal.json). A part takes the adopted piece of its use tag whose size is the part's, and none else: the plan's numbers are pinned to the pieces
// (LightDotRow_2 2 m, _1 1 m, CrossEdgeLine_2 / _1 2 / 1 m, LightBar_4 4 m, KerbStud 0.4, CrossInlayArc_R24_W08 R 2.83, SignalEye 1.12 across and 3.23 high), so a piece is never scaled (scale 1) and a part whose piece is not adopted (or has another size) stays procedural.
// Kit axes: x across (the piece's long axis for a light), y up, z depth; a kit yaw turns the piece's +X to that heading and its +Z front faces yaw + 90 degrees (kitrefs.reachYaw).
import type { KitRef, StreetItem } from '../../../../contracts/world/world.schema';
import { r4 } from '../common';
import { forUse, reachYaw } from '../kitrefs';

/** The use tag of the piece each part kind takes. */
export const PART_USE: Record<string, string> = { 'crossing-bar': 'crossing:bar', 'crossing-dots': 'crossing:dots', 'crossing-edge': 'crossing:edge', 'crossing-stud': 'stud:kerb', 'crossing-arc': 'inlay:crossing-arc', 'signal-orb': 'signal:eye' };
/** Metres a piece's size may differ from the part's (KerbStud is 0.38 deep, the plan's stud 0.4 across). */
const FIT = 0.05;
/** The heading (plan frame) the inlay arc piece bulges toward at kit yaw 0. The piece (measured.json: min 0, 0, max 2.83, 2.83 about its pivot) lies in Blender's +X +Y quadrant, which is its +X and -Z in kit axes (the piece's front +Z is Blender -Y),
 *  so in the plan (kit +Z along plan +y) it bulges toward heading -45 degrees; its kit yaw is the part's heading less that. Latch confirms it on a shot. */
const ARC_BULGE = -Math.PI / 4;

/** The kit yaw that puts the piece as the part's own yaw says: lights and studs along it, an arc's quarter toward the corner it rounds, an eye's display A (its +Z front) along the walk. */
export function kitYaw(it: Pick<StreetItem, 'kind' | 'yaw'>): number {
  const y = it.kind === 'crossing-arc' ? it.yaw - ARC_BULGE : it.kind === 'signal-orb' ? reachYaw(it.yaw, '+z') : it.yaw;
  return r4(Math.atan2(Math.sin(y), Math.cos(y)));
}

/** True if a piece of `size` (kit axes) is the part's: its footprint and height within FIT of the part's own size. */
export const fitsPart = (size: readonly [number, number, number], it: Pick<StreetItem, 'size'>): boolean =>
  Math.abs(size[0] - it.size[0]) <= FIT && Math.abs(size[1] - it.size[2]) <= FIT && Math.abs(size[2] - it.size[1]) <= FIT;

/** The part with its kit piece, or as it was where no adopted piece of its use has its size. */
export function bindPart(it: StreetItem): StreetItem {
  const use = PART_USE[it.kind]; if (!use) return it;
  const a = forUse(use).find(p => fitsPart(p.size, it));
  if (!a) return it;
  const kit: KitRef = { id: a.id, scale: [1, 1, 1], yaw: kitYaw(it) };
  return { ...it, source: 'kit', kit };
}
