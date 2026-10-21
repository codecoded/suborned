// The capstone programme's robots inside the civic block (brief 026): the block stays as built, so the programme adds only what its rules say belongs and what stands clear of every civic mass and Handover point.
//   waiter   at the open end of the café terrace (the old kit's rule: waiters at the open ends of café terraces), on the terrace, facing along it; the end (east or west) is seeded, the other is the fallback.
//   porter   at the tram stop or the boarding pavilion: the block has no tram stop and the square's boarding pavilion is the crew's getaway staging (dressing keepClear crew-line-getaway): no porter, logged for Sky.
import type { Mass, StreetItem } from '../../../../contracts/world/world.schema';
import type { Civic } from '../../../plan/civic';
import { toPlanFn } from '../../../plan/civic';
import { type V, inPoly, segDist } from '../../../plan/geom';
import { boxRing, ringGap } from '../../../plan/street-zones';
import { handoverPoints } from '../../civic/handover';
import { type Ctx, r3, r4, rng } from '../common';
import { adoptedBy } from '../kitrefs';
import { facing, pivotFor, robotPiece, type CapLog } from './capstone';
import { PAVE, RULES } from './capstone-rules';

export function civicRobots(ctx: Ctx, civic: Mass[], log: CapLog | undefined): StreetItem[] {
  const out: StreetItem[] = [], terrace = civic.find(m => m.id === 'civic/cafe/terrace'), a = robotPiece('waiter');
  const gap = (rule: string, where: string, why: string) => log?.gaps.push({ rule, where, why });
  if (!terrace || !a) { if (log) gap('R-waiter', 'civic/cafe/terrace', a ? 'the block has no café terrace' : 'no waiter robot adopted'); return out; }
  const toPlan = toPlanFn(ctx.plan.civic), points = handoverPoints(toPlan), xs = terrace.ring.map(p => p[0]), ys = terrace.ring.map(p => p[1]);
  const x0 = Math.min(...xs), x1 = Math.max(...xs), y0 = Math.min(...ys), y1 = Math.max(...ys);
  const others = civic.filter(m => m.id !== terrace.id && m.base < 1.2 && !(m.kit && /Parasol/.test(m.kit.id)) && m.id !== 'civic/cafe' && m.id !== 'civic/cafe/ground');
  const zones = ((ctx.civ.dressing as { keepClear?: { id: string; bars: string[]; polygon?: V[] }[] } | undefined)?.keepClear ?? []).filter(z => z.polygon && z.bars.includes('planter')).map(z => z.polygon!.map(toPlan));
  const east = rng('cap/civic/waiter').chance(0.5), ends = east ? [x1, x0] : [x0, x1];
  for (const end of ends) {
    const inward = end === x1 ? -1 : 1, face: V = [inward, 0];
    for (const dx of [1.2, 1.8, 2.4, 3]) for (let y = y1 - 0.8; y > y0 + 0.8; y -= 0.5) { // the street-side edge first
      const at: V = [end + inward * dx, y], yaw = facing(face), ring = boxRing(at, a.size[0], a.size[2], yaw), piv = pivotFor(a, at, yaw);
      const ok = ring.every(q => inPoly(q, terrace.ring)) && !points.some(h => (inPoly(h.p, ring) ? 0 : Math.min(...ring.map((q, i) => segDist(h.p, q, ring[(i + 1) % 4]!)))) < RULES.pointClear)
        && !others.some(m => ringGap(ring, m.ring) < 0.3) && !zones.some(z => ring.some(q => inPoly(q, z)));
      if (!ok) continue;
      out.push({ id: 'street/capstone/service-robot-civic-0', kind: 'service-robot', variant: 'waiter', at: piv, yaw: r4(yaw), z: PAVE, size: [r3(a.size[0]), r3(a.size[2]), r3(a.size[1])], surface: 'trim', edge: 'civic/cafe/terrace', sight: 'blocks', source: 'kit', kit: { id: a.id, scale: [1, 1, 1] } });
      if (log) { log.counts['service-robot'] = (log.counts['service-robot'] ?? 0) + 1; log.pieces[a.id] = (log.pieces[a.id] ?? 0) + 1; }
      return out;
    }
  }
  gap('R-waiter', 'civic/cafe/terrace', 'no place at either end of the terrace clear of the tables, the planting zones and the Handover points');
  void adoptedBy;
  return out;
}

/** The porter has no job in the block: logged once. */
export const civicPorterGap = (log: CapLog | undefined, civ: Civic): void => {
  if (log && civ.dressing) log.gaps.push({ rule: 'R-porter', where: 'civic/square-boarding-pavilion (porters stand at the Records tram stops instead)', why: 'the block has no tram stop; the boarding pavilion is the crew\'s getaway staging (keepClear crew-line-getaway): a porter there would stand in the Handover crew line, so none is placed' });
};
