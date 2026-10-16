// W-5: the walking clearance (standards.walking.body; Datum 2026-10-15). Walks keep body.r from the civic block's ground solids, on any grid, and pass no gap narrower than body.clearWidth: a gap of at least
// body.constrained.w over at most body.constrained.run metres of a walk, or an aisle of body.aisle inside a seating area, is the most the standard allows, and within body.ends of a door, a seat or a Handover
// point nothing is judged. Two things are held: (a) the plan checker's own walks (W-1) keep the body clearance, read from the plan (tools/plan/walk.ts); (b) the walks between the block's doors, the Handover
// points and the crossing landings round it (Datum's 741, on a 0.25 m grid here, tools/plan/walk-gap.ts) pass no gap the standard does not allow. The people's walk graph (tools/people/walk.ts) reads the same clearance.
import type { Check } from '../../plan/checks';
import { toPlanFn } from '../../plan/civic';
import { type V, dist } from '../../plan/geom';
import { walks as footWalks } from '../../plan/walk';
import { type Pt, bodyStd, gapWalks, walkClearance } from '../../plan/walk-gap';
import { type WCtx, rule } from '../../plan/world-ctx';
import { landingsOf } from '../gen/street/crossings';
import { handoverPoints } from './civic-dress';

const SOURCE = 'standards.walking.body (r, clearWidth, constrained, aisle, ends) and walking.rules W-5 · DfT Inclusive Mobility (2021) §3.1: 2.0 m so two wheelchairs pass, 1.5 m where constrained · Manual for Streets (2007) · Datum 2026-10-15';

/** The points the block's walks run between: its doors (service doors included), the Handover points (a point that is a door is the door), and the crossing landings the block's dressing names. */
export function blockPoints(c: WCtx, mission?: string): Pt[] {
  const toPlan = toPlanFn(c.plan.civic), out: Pt[] = [...c.civ.entrances, ...c.civ.serviceDoors].map(e => ({ name: e.name, p: e.p })); // the service doors too (brief 013): a stash nobody can walk to is no stash
  for (const h of mission ? handoverPoints(toPlan, mission) : handoverPoints(toPlan)) if (!out.some(o => dist(o.p, h.p) < 0.3)) out.push({ name: h.name, p: h.p });
  const dr = c.civ.dressing as { crossings?: { at: { id: string }[] } } | undefined;
  for (const l of dr?.crossings?.at ?? []) { const it = landingsOf(c.w).find(s => s.id.endsWith(`/crossing-${l.id}`)); if (it) out.push({ name: `landing ${l.id}`, p: it.at }); }
  return out;
}
/** The seating areas of the block, rings in plan frame: the dressing's `seating` zones where it names them, else the café terrace (the CafeBay terrace between the café and its wall) until the block says. */
export function seatingAreas(c: WCtx): V[][] {
  const toPlan = toPlanFn(c.plan.civic), dr = c.civ.dressing as { seating?: string[]; keepClear?: { id: string; polygon?: V[] }[] } | undefined;
  const ids = dr?.seating ?? ['cafe-terrace'];
  return (dr?.keepClear ?? []).filter(z => ids.includes(z.id) && z.polygon).map(z => z.polygon!.map(toPlan));
}

export function walkingRules(c: WCtx): Check[] {
  const body = bodyStd(c.plan);
  if (!body || !c.civ.solids?.length) return [];
  const points = blockPoints(c), rep = gapWalks(c.plan, c.d, c.civ, points, seatingAreas(c), body), plan = walkClearance(footWalks(c.plan, c.d, c.civ), c.civ, body);
  const bad = [
    ...plan.map(w => `the walk ${w.walk} passes ${w.gap.toFixed(2)} m from ${w.solid} (body.r ${body.r})`),
    ...rep.bad.map(g => `${g.walk} passes ${g.why} between ${g.between} at ${g.at.join(', ')}`),
  ];
  return [rule('W-5', 'Walking', `Walks keep ${body.r} m from a ground solid and pass no gap under ${body.clearWidth} m, bar ${body.constrained.w} m for ${body.constrained.run} m and a ${body.aisle} m seating aisle`,
    rep.walks + footWalks(c.plan, c.d, c.civ).length, bad, 'none', SOURCE,
    !bad.length)].map(k => ({ ...k, detail: bad.length ? k.detail : `${rep.walks} walks between ${points.length} points; ${rep.allowed} narrow places inside the standard's exceptions${rep.stranded.length ? `; no walk starts at ${rep.stranded.join(', ')} (no free ground within ${body.ends} m)` : ''}` }));
}
