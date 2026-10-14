// What Pulse and Keel are shown about the scope besides retrieved passages: the units to cast, the places, the rules' vocabulary
// and the life.json day, trimmed to what a cast table needs. Small on purpose: the tables are per area × occupation, not per person.
import type { World } from '../../people/inputs';
import { positionsOf } from '../../people/rules';
import { allowedTemperaments } from '../../people/rules';

const j = (x: unknown) => JSON.stringify(x);
const hm = (m: number) => `${String(Math.floor(m / 60) % 24).padStart(2, '0')}:${String(m % 60).padStart(2, '0')}`;

export function scopeView(w: World): string {
  const s = w.scope;
  const units = s.units.map(u => ({
    row: `${u.area}/${u.trade}`, unit: u.id, name: u.name, kind: u.kind, category: u.category, employer: u.employer,
    staffPerShift: u.staffPerShift, shifts: u.shifts.map(x => `${hm(x.from)}-${hm(x.to)}`), door: u.door, bay: u.bay ?? null,
    ...(u.official ? { officialEntrance: u.official } : {}), ...(u.watch?.length ? { watchShifts: u.watch.map(x => `${hm(x.from)}-${hm(x.to)}`) } : {}),
    positions: u.kind === 'civic'
      ? Object.entries(w.rules.temperamentsBy.roles).filter(([, r]) => r.employer === u.employer).map(([k]) => k)
      : positionsOf(w.rules, u.category),
  }));
  const homes = s.homes.map(h => ({ row: `${h.area}/resident`, plot: h.plot, residents: h.residents }));
  return `Scope ${s.id}: areas ${s.areas.join(', ')}; plots ${s.plots.map(p => `${p.id} (${p.use})`).join(', ')}; stops ${s.stops.map(x => `${x.name} (${x.mode})`).join(', ')}.\n`
    + `Rows to write, one per area × occupation (JSON): ${j(units)}\nResidents, a sample of ${8} per plot is simulated (JSON): ${j(homes)}\n`
    + `The Handover starts ${w.mission.clock.startsAt}; its roles, read from the mission (JSON; each is cast from the people your rows make, and must be playable): ${j(w.mission.roles.map(r => ({ id: r.id, employer: r.employer, ...(r.position ? { position: r.position } : { role: r.role }), at: r.at, window: `${r.shift.from}-${r.shift.to}`, need: r.need, approach: r.approach, regularOf: r.regularOf, tiedTo: r.tiedTo, leavesTo: r.leavesTo, waitsFor: r.waitsFor, alone: r.alone })))}`;
}

export function rulesView(w: World): string {
  const r = w.rules;
  const tem = Object.fromEntries(Object.entries(r.temperaments).map(([k, t]) => [k, { respondsTo: t.respondsTo, start: t.start }]));
  const needs = Object.fromEntries(Object.entries(r.needs.list).map(([k, n]) => [k, { metBy: n.metBy, requires: n.requires }]));
  const ties = Object.fromEntries(Object.entries(r.ties.kinds).map(([k, t]) => [k, { direction: t.direction, max: t.maxPerPerson, between: t.between }]));
  const allow = Object.fromEntries(Object.keys(r.temperamentsBy.roles).map(k => [k, allowedTemperaments(r, k)]));
  const n = r.names;
  return `Temperaments (JSON): ${j(tem)}\nNeeds (JSON): ${j(needs)}\nTie kinds, per person ${r.ties.perPerson.min}-${r.ties.perPerson.max} (JSON): ${j(ties)}\n`
    + `Roles and the temperaments they allow (JSON): ${j(allow)}\nTrades allow every temperament except where the rules say (pawnbroker, credit-bureau: no idealist).\n`
    + `Names: ${n.minGiven}+ given and ${n.minFamily}+ family names, ${n.minOrigins}+ origins from ${n.origins.join(', ')}, none over ${n.maxOriginShare * 100}% of a pool; pattern ${n.form.pattern}; avoid ${n.avoid.join(', ')}; ${n.noEraDate}`;
}

export function dayView(w: World): string {
  return `Day skeletons (life.json generator.routines) and bands are in the retrieved passages. Walk speed ${w.scope.speed} m/s; code times every walk on the walk graph.`;
}
