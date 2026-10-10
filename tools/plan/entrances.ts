// Entrance rules for every tram stop and sky rail station: a walk reaches it from public space (W-2) and its `fronts`
// names a road or foot path whose public space it actually touches (W-3).
import type { Check } from './checks';
import type { Civic } from './civic';
import { lineDist, type V } from './geom';
import type { Derived, Plan } from './plan';
import { stopsOf } from './transit';
import { reach } from './walk';

const SRC = 'plan.standards.walking (only buildings, plots and water block) · plan.standards.roads.frontage (entrances front public space) · ';
/** An entrance may stand this far (m) outside the edge of the road or foot path it fronts: kerb, apron, rail beam overhang. */
export const FRONTS_TOL = 6;

export function entranceChecks(plan: Plan, d: Derived, civ: Civic): Check[] {
  const add = (c: Omit<Check, 'group'>): Check => ({ group: 'Walking', ...c });
  const R = reach(plan, d, civ), cut = R.filter(r => r.m === Infinity);
  const out = [add({ id: 'W-2', rule: 'Every tram stop and sky rail station can be walked to from public space (water and buildings block, roads are crossable)', value: `${R.length - cut.length}/${R.length} reached`, limit: 'all', ok: !cut.length, detail: cut.map(r => `${r.line}/${r.id} (${r.at.join(', ')}) has no walk`).join('; ') || undefined, source: SRC + 'players reach every stop on foot' })];

  const ground = new Map<string, { line: V[]; w: number }>([
    ...d.roads.map(r => [r.id, { line: r.line, w: r.w }] as const),
    ...plan.foot.map(f => [f.id, { line: f.pts, w: f.w }] as const),
  ]);
  const all = [...plan.tram, ...plan.skyrail].flatMap(l => stopsOf(l).map(s => ({ ...s, key: `${l.id}/${s.id}` })));
  const declared = all.filter(s => s.fronts !== undefined).length, bad = declared ? all.flatMap(s => {
    if (s.fronts === undefined) return [`${s.key} names no fronts`];
    const g = ground.get(s.fronts);
    if (!g) return [`${s.key} fronts unknown road or foot path ${s.fronts}`];
    const m = lineDist(s.at, g.line) - g.w / 2;
    return m > FRONTS_TOL ? [`${s.key} is ${m.toFixed(1)} m from ${s.fronts}`] : [];
  }) : [];
  out.push(add({ id: 'W-3', rule: 'Every entrance fronts an existing road or foot path it touches (edge within tolerance); once any stop names fronts, all must', value: declared ? `${all.length - bad.length}/${all.length} fronted` : 'no stop names fronts', limit: `all, edge ≤ ${FRONTS_TOL} m`, ok: !bad.length, detail: bad.join('; ') || undefined, source: SRC + 'entrances fronting public space' }));
  return out;
}
