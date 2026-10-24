// KR-1 (brief 028, Sky 2026-10-24: "make sure the corners of pavements are rounded like in testbed, not sharp"): the plan's kerb returns (standards.streetscape.kerbReturn, Datum) hold against the plan's own ground.
// Opt-in: a plan without the key gets no check (a pinned plan replays with the square corners it was made with; the generator then takes the defaults in tools/world/gen/kerbreturn.ts only where the key is absent AND
// the check is not asked for, so a pinned replay stays as it was: set `kerbReturn.off`).
//   - the key is whole: a radius for each road class, a concave fillet, a bay radius, the civic radius, all above 0.3 m
//   - every return the generator makes is the radius the plan asks: none is shrunk because an edge is too short for both ends' tangents (the arcs would otherwise eat the straight kerb)
//   - the pavement still holds its walk: on the corner's bisector the pavement between the arc and a square building corner (the building line's corner: both sections' pavements in from the kerb faces) is at least the wider
//     kerb + walk of the two sections (Datum's formula: sqrt(2)·p - (sqrt(2) - 1)·R for equal pavements p), so a return never takes the walking zone away from a building's ground floor
// Failing examples: kerb-return.test.ts (the committed plan with one radius made too big, one key dropped, a short edge).
import type { Check } from './checks';
import type { Civic } from './civic';
import { segDist } from './geom';
import type { Derived, Plan } from './plan';
import { ctxOf } from '../world/gen/common';
import { returnsOf } from '../world/gen/kerbreturn';

const OPEN = new Set(['park', 'open', 'square', 'civic']);
const SRC = 'standards.streetscape.kerbReturn (Datum 2026-10-24) · brief 028 (Sky 2026-10-24) · Manual for Streets §6.3.12 · the testbed (civic-road-layout.js civicRoadBoundaries)';

export function kerbReturnChecks(plan: Plan, d: Derived, civ: Civic): Check[] {
  const S = plan.standards?.streetscape as any, K = S?.kerbReturn; if (!K) return [];
  const bad: string[] = [], num = (x: unknown): x is number => typeof x === 'number' && Number.isFinite(x);
  for (const c of ['primary', 'district', 'access']) if (!num(K.byClass?.[c]) || K.byClass[c] < 0.3) bad.push(`byClass.${c} is ${JSON.stringify(K.byClass?.[c])}: a radius of at least 0.3 m for every road class`);
  for (const k of ['concave', 'bay', 'civic']) if (!num(K[k]) || K[k] < 0.3) bad.push(`${k} is ${JSON.stringify(K[k])}: a radius of at least 0.3 m`);
  let n = 0, shrunk = 0, tight = 0;
  if (!bad.length) {
    const ctx = ctxOf(plan, d, civ, 0.18), roadOf = (id: string) => d.roads.find(r => r.id === id);
    for (const r of returnsOf(ctx)) {
      n++;
      if (r.r < r.want - 0.01) { shrunk++; bad.push(`${r.id}: radius ${r.r.toFixed(2)} m, the plan asks ${r.want} m (an edge is too short for both ends' tangents)`); }
      const plot = d.plots.find(p => p.id === r.plot);
      if (!r.convex || r.bay || r.mouth || r.nose || !plot || OPEN.has(plot.use) || plot.civic) continue;
      const secs = r.roads.map(id => S.sections[roadOf(id)?.class ?? r.cls] as { pavement: number; kerb: number; walk: number }); if (secs.some(s => !s)) continue;
      const bl: [number, number] = [r.at[0] - r.n1[0] * secs[0]!.pavement - r.n2[0] * secs[1]!.pavement, r.at[1] - r.n1[1] * secs[0]!.pavement - r.n2[1] * secs[1]!.pavement]; // the building line's corner
      // the pavement left at the building line's corner: its distance to the arc (the quarter turn between the tangent points, not the whole circle)
      const w = Math.min(...r.arc.slice(1).map((q, i) => segDist(bl, r.arc[i]!, q))), need = Math.max(...secs.map(s => s.kerb + s.walk));
      if (w < need - 0.01) { tight++; bad.push(`${r.id} (${plot.id}): ${w.toFixed(2)} m of pavement on the corner's bisector, the sections need ${need} m (kerb + walk) against a square building corner`); }
    }
  }
  void shrunk; void tight;
  return [{ id: 'KR-1', group: 'Streetscape', rule: 'Every kerb return is the plan\'s radius (class of the minor road, civic, concave, bay), none shrunk, and the pavement on its bisector still holds the wider kerb + walk against a square building corner', value: bad.length ? `${bad.length} fail` : `${n} returns`, limit: 'all', ok: !bad.length, detail: bad.length ? bad.slice(0, 6).join('; ') + (bad.length > 6 ? '; …' : '') : undefined, source: SRC } as Check];
}
