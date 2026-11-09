// Brief 101 (Sky's pick B, 2026-11-09): the square's mast camera stands where the civic block puts it, and every mast's heads step by its site.
//   squareMast   the block's dressing names the lamp standard that carries the square's mast (`mast: { site: 'square', headDeg }`, civic-block.adjust.json): the mast
//                stands at that lamp, its first head along headDeg (the block frame: degrees from +x, south, toward +y, east), turned into the plan's frame. A block
//                that names none (a run's pinned block) gives undefined, and transit.ts stands the mast at the square's corner nearest the circus, yaw 0, as before.
//   mastSite     the plan's watcher site an item's edge stands for: 'circus', or 'square' for a plot whose use is square (streetscape.watchers.sites).
//   headStepDeg  degrees between a camera's neighbouring heads (streetscape.watchers.types.<kind>.headStep): one number for every site (a plan before 0.5.7)
//                or one per site (square 60, circus 120). A camera of several heads at a site the plan gives no step is an error, never a step of 0.
//   lampMastPiece  the square's lamp mast, the adopted piece tagged mast-camera:lamp (Vela's MastCamera_Lamp, brief 101): the lamp's sleeve, arms, banner and globe at
//                their heights, the column on to the crown and three cameras fanned 60° (camera k along kit +X turned k x 60° toward kit +Z, so placed at the item's yaw
//                it looks along facings[k]). Built to its place: scale 1, no yaw of its own. The square's mast takes it as the variant `lamp`, whose use tag and slot are its
//                own (kitrefs.ts LAMP_MAST), so the chooser gives it to no other mast.
//   drawnByMast  a dressing lamp that carries the mast is drawn by the mast's piece, not as a lamp of its own (one piece at the site); in a kit without the piece
//                (a run's pinned kit) the mast is today's MastCamera and the lamp stands as before.
import type { KitAsset } from '../../../../contracts/world/kit.schema';
import { toPlanFn } from '../../../plan/civic';
import type { V } from '../../../plan/geom';
import { type Ctx, r4 } from '../common';
import { LAMP_MAST, forUse } from '../kitrefs';

type MastPlace = { site: string; headDeg: number };
type Lamp = { id: string; at: V; mast?: MastPlace };

/** The ground footprint and height the square's mast item takes with the lamp mast piece: its declared plinth, 0.58 m square about the pivot (kit.collide.json), and its 8.2 m. */
export const LAMP_MAST_SIZE: [number, number, number] = [0.58, 0.58, 8.2];
/** The adopted lamp mast piece, or undefined in a kit without one. */
export const lampMastPiece = (): KitAsset | undefined => forUse(LAMP_MAST.use)[0];
/** True when the world draws this dressing lamp as the mast's piece: it carries a mast and the kit has the lamp mast. */
export const drawnByMast = (l: { mast?: unknown }): boolean => !!l.mast && !!lampMastPiece();

/** The square's mast from the block: its place and yaw (plan frame, radians: the first head), and the lamp that carries it; undefined on a block that names none. */
export function squareMast(ctx: Pick<Ctx, 'plan' | 'civ'>): { at: V; yaw: number; lamp: string } | undefined {
  const lamps = ((ctx.civ.dressing as { lamps?: { at?: Lamp[] } } | undefined)?.lamps?.at ?? []).filter(l => l.mast?.site === 'square');
  if (lamps.length > 1) throw new Error(`civic dressing: ${lamps.map(l => l.id).join(', ')} each carry the square's mast; the plan stands one (streetscape.watchers.sites.square)`);
  const l = lamps[0];
  if (!l) return undefined;
  if (!Number.isFinite(l.mast!.headDeg)) throw new Error(`civic dressing: ${l.id} carries the square's mast with no headDeg (its first head's facing, block frame)`);
  const toPlan = toPlanFn(ctx.plan.civic), a = (l.mast!.headDeg * Math.PI) / 180, p = toPlan(l.at), q = toPlan([l.at[0] + Math.cos(a), l.at[1] + Math.sin(a)]);
  return { at: p, yaw: r4(Math.atan2(q[1] - p[1], q[0] - p[0])), lamp: l.id };
}

/** The watcher site a street item's edge stands for: the circus, a square (a plot of use square), or undefined (a station, stop or gate). */
export function mastSite(ctx: Pick<Ctx, 'plan'>, edge: string): 'circus' | 'square' | undefined {
  if (edge === 'circus') return 'circus';
  return ctx.plan.plots.some(p => p.use === 'square' && p.id === edge) ? 'square' : undefined;
}

/** Degrees between neighbouring heads of a camera type at a site; 0 for a camera of one head. */
export function headStepDeg(t: { heads?: number; headStep?: number | Record<string, number> }, site: string | undefined, what: string): number {
  if ((t.heads ?? 1) < 2) return 0;
  if (typeof t.headStep === 'number') return t.headStep;
  const s = site ? t.headStep?.[site] : undefined;
  if (typeof s !== 'number') throw new Error(`${what}: ${t.heads} heads at ${site ?? 'no watcher site'}, and streetscape.watchers.types gives that site no headStep`);
  return s;
}
