// Kit pieces for a crossing's parts (brief 009 B; Vela's proposal out/decokit-009/catalogue.proposal.json; brief 061, signal head A). A part takes the adopted piece of its use tag whose size is the part's, and none else: the plan's numbers are pinned to the pieces
// (LightDotRow_2 1.8 m, _1 0.8 m: the module 2 / 1 less the pitch plus the seat, gen/street/parts.ts dotsLength; LightBar_4 4 m; the signal head 0.95 across and 3.085 high), so a piece is never scaled (scale 1) and a part whose piece is not adopted (or has another size) stays procedural.
// The signal head is SignalLens (use tag signal:lens; Vela's pick A, built in tools/decokit; SignalEye is retired). The ring in the paving under the beam (HaloRing,
// one quad with radial UV) is not a part of its own: it stands at the head's `signal.halo` (centre, ground level, radius), which Latch's SignalHead fills by one property.
// Kit axes: x across (the piece's long axis for a light), y up, z depth; a kit yaw turns the piece's +X to that heading and its +Z front faces yaw + 90 degrees (kitrefs.reachYaw).
import type { KitRef, StreetItem } from '../../../../contracts/world/world.schema';
import { r4 } from '../common';
import { forUse, reachYaw } from '../kitrefs';

/** The use tags of the piece each part kind takes, in order of preference. */
export const PART_USE: Record<string, readonly string[]> = { 'crossing-bar': ['crossing:bar'], 'crossing-dots': ['crossing:dots'], 'signal-orb': ['signal:lens'] };
/** Metres a piece's size may differ from the part's. */
const FIT = 0.05;

/** The kit yaw that puts the piece as the part's own yaw says: lights along it, a head's display A (its +Z front) along the walk. */
export function kitYaw(it: Pick<StreetItem, 'kind' | 'yaw'>): number {
  const y = it.kind === 'signal-orb' ? reachYaw(it.yaw, '+z') : it.yaw;
  return r4(Math.atan2(Math.sin(y), Math.cos(y)));
}

/** True if a piece of `size` (kit axes) is the part's: its footprint and height within FIT of the part's own size. */
export const fitsPart = (size: readonly [number, number, number], it: Pick<StreetItem, 'size'>): boolean =>
  Math.abs(size[0] - it.size[0]) <= FIT && Math.abs(size[1] - it.size[2]) <= FIT && Math.abs(size[2] - it.size[1]) <= FIT;

/** The part with its kit piece, or as it was where no adopted piece of its use has its size. */
export function bindPart(it: StreetItem): StreetItem {
  const uses = PART_USE[it.kind]; if (!uses) return it;
  const a = uses.map(use => forUse(use).find(p => fitsPart(p.size, it))).find(Boolean);
  if (!a) return it;
  const kit: KitRef = { id: a.id, scale: [1, 1, 1], yaw: kitYaw(it) };
  return { ...it, source: 'kit', kit };
}
