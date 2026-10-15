// The cover rules (brief 011, Pulse): CV-1 the file is well formed and says what it was made from; CV-2 every fight area has cover for both sides. Each reports a value
// against a limit like the plan's rules (tools/plan/checks.ts Check) and has a failing example in tools/cover/cover.test.ts.
import type { Cover, CoverPoint } from '../../contracts/world/cover.schema';
import type { Check } from '../plan/checks';
import { quality } from './gen';
import { type Inputs } from './inputs';
import { isqrt } from './select';
export { hashes, type Inputs } from './inputs';

export const RULES_SRC: Record<string, string> = {
  'CV-1': 'brief 011 fight AI (Pulse): a cover point is a body-width place on free ground beside an object of at least 0.9 m, with a facing and an arc that are the object\'s own geometry and a quality that is a function of height and arc; the file names the hash of what it was made from',
  'CV-2': 'GDD §06 fights measure preparation (cover for both sides is what makes a fight readable and fair); Motive 2026-10-15: the ambush spots stand on the open square. A fight area has cover for both sides: at least half of the hostile posts have a cover point within hostileReach that shields them from the anchor, and at least half of them are answered by a cover point within cellReach of the anchor that shields the cell from them',
};
const row = (id: string, rule: string, bad: string[], value: string, limit: string): Check => ({ id, group: 'Cover', rule, value, limit, ok: !bad.length, ...(bad.length ? { detail: bad.slice(0, 12).join('; ') + (bad.length > 12 ? `; +${bad.length - 12} more` : '') } : {}), source: RULES_SRC[id] });

/** `free(x, y)`: whether a cm point is on a free cell of the site grid. `placeAt`: a site place's point (stand if it has one), cm. `inputs`: today's hashes, to say the file is stale. */
export function checkCover(cover: Cover, o: { free: (x: number, y: number) => boolean; placeAt: (id: string) => [number, number] | undefined; inputs?: Inputs }): Check[] {
  const out: Check[] = [], byId = new Map(cover.points.map(c => [c.id, c]));
  // ---- CV-1
  {
    const bad: string[] = [];
    cover.points.forEach((c, i) => {
      if (c.id !== `cv-${String(i + 1).padStart(3, '0')}`) bad.push(`${c.id}: not the ${i + 1}th point in order`);
      if (!o.free(c.at[0], c.at[1])) bad.push(`${c.id}: ${c.at.join(',')} is not free ground`);
      const f = isqrt(c.faces[0] ** 2 + c.faces[1] ** 2); if (Math.abs(f - 1000) > 3) bad.push(`${c.id}: faces is not a unit vector (${f})`);
      if (c.cos !== Math.floor(1000 * Math.cos((c.arc * Math.PI) / 180))) bad.push(`${c.id}: cos ${c.cos} is not floor(1000 cos ${c.arc}°)`);
      if (c.q !== quality(c.h, c.arc)) bad.push(`${c.id}: q ${c.q} is not the quality of ${c.h} cm at ${c.arc}°`);
      if (c.h < cover.rules.minHeightCm) bad.push(`${c.id}: ${c.h} cm is under the ${cover.rules.minHeightCm} cm that hides a body`);
      if ((c.h >= cover.rules.eyeCm) !== (c.against === 'fire+sight')) bad.push(`${c.id}: against ${c.against} at ${c.h} cm`);
      if (c.present && c.present[0] >= c.present[1]) bad.push(`${c.id}: present ${c.present.join('-')} is empty`);
      if ((c.kind === 'car') !== !!c.present) bad.push(`${c.id}: ${c.kind} ${c.present ? 'has' : 'lacks'} a presence window`);
    });
    for (const b of cover.blockers) if (b.h < cover.rules.eyeCm) bad.push(`${b.id}: a blocker of ${b.h} cm is under eye height`);
    for (const a of cover.areas) {
      for (const p of [...a.hostileCover.flatMap(h => h.points), ...a.cellCover.flatMap(h => h.points)]) if (!byId.has(p)) bad.push(`${a.id}: names the point ${p}, which is not in the file`);
      for (const id of [a.anchor, ...a.hostile, ...a.cell, ...a.allies]) if (!o.placeAt(id)) bad.push(`${a.id}: the site has no place ${id}`);
    }
    if (o.inputs) for (const k of Object.keys(o.inputs) as (keyof Inputs)[]) if (cover.from[k] !== o.inputs[k]) bad.push(`made from another ${k} (${cover.from[k].slice(0, 12)}, now ${o.inputs[k].slice(0, 12)}): bun run cover`);
    out.push(row('CV-1', 'The cover file is well formed: points on free ground, unit facings, quality from height and arc, and the inputs it was made from', bad, `${cover.points.length} points, ${cover.blockers.length} blockers, ${cover.refuge.length} refuges`, '0 faults'));
  }
  // ---- CV-2
  {
    const bad: string[] = [], seen: string[] = [];
    for (const a of cover.areas) {
      const half = Math.ceil(a.hostile.length / 2), withCover = a.hostileCover.filter(h => h.points.length), answered = a.cellCover.filter(h => h.points.length);
      seen.push(`${a.id} hostile ${withCover.length}/${a.hostile.length}, cell ${answered.length}/${a.hostile.length}`);
      if (withCover.length < half) bad.push(`${a.id}: only ${withCover.length} of ${a.hostile.length} hostile posts have cover (need ${half}); without: ${a.hostileCover.filter(h => !h.points.length).map(h => h.post).join(', ')}`);
      if (answered.length < half) bad.push(`${a.id}: the cell is answered at only ${answered.length} of ${a.hostile.length} hostile posts (need ${half}); without: ${a.cellCover.filter(h => !h.points.length).map(h => h.post).join(', ')}`);
    }
    out.push(row('CV-2', 'Every fight area has cover for both sides', bad, seen.join('; '), 'at least half the posts each way'));
  }
  return out;
}
