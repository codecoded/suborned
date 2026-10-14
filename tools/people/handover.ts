// The roles the Handover names (GDD §07; brief 010 B, D): a waitress on the café's shift at the start, the councillor's
// driver at the bay, the guard at the Office door, and Vale's office. The mission file names points, not roles yet (Waymark, D),
// so the roles are listed here by position and place; each place is the mission's own destination, so the mission is not edited.
import { inPoly, dist, type V } from '../plan/geom';
import { toPlanFn } from '../plan/civic';
import type { Person } from '../../contracts/people/people.schema';
import { inShift } from './expand';
import type { Scope } from './scope';

export type MissionIn = { clock: { startsAt: string }; destinations: { name: string; at: [number, number]; surface: string }[] };
export const startMin = (m: MissionIn) => { const [h, mi] = m.clock.startsAt.split(':').map(Number); return h! * 60 + mi!; };
/** Mission window ends 8 min after the start (GDD §07: over at 8:00). */
export const WINDOW_MIN = 8;

export const HANDOVER_ROLES = [
  { id: 'waitress', employer: 'cafe', position: 'server', dest: 'Café door', near: 4, what: 'a server on shift at the café door' },
  { id: 'driver', employer: 'records-office', role: 'official-driver', dest: 'Meet driver', what: 'the official driver at the bay' },
  { id: 'guard', employer: 'records-office', role: 'guard', dest: 'Office entrance', near: 4, what: 'a guard at the Office door' },
  { id: 'councillor', employer: 'records-office', role: 'councillor', dest: undefined, what: 'Vale\'s office: a councillor on shift' },
] as const;

export type Cast = { role: string; ok: boolean; who: string[]; why: string };

/** Who the Handover would cast for each role: people on shift for the whole window, standing at the place the mission points to. */
export function castHandover(persons: Person[], scope: Scope, mission: MissionIn, plan: { civic: unknown }): Cast[] {
  const toPlan = toPlanFn(plan.civic), t0 = startMin(mission), t1 = t0 + WINDOW_MIN;
  const stopAt = (p: Person, t: number) => p.routine.stops.find(s => t >= s.from && t < s.to);
  return HANDOVER_ROLES.map((r): Cast => {
    const d = r.dest ? mission.destinations.find(x => x.name === r.dest) : undefined;
    if (r.dest && !d) return { role: r.id, ok: false, who: [], why: `the mission has no destination ${r.dest}` };
    const at = d ? toPlan(d.at as V) : undefined;
    const who = persons.filter(p => {
      const w = p.work; if (!w || w.employer !== r.employer) return false;
      if (('position' in r ? w.position !== r.position : w.role !== r.role)) return false;
      if (!inShift(w.shift, t0) || !inShift(w.shift, t1 % 1440)) return false;
      if (!at) return true;
      const s = stopAt(p, t0), pl = scope.places.find(x => x.id === w.place);
      if (!s || s.place !== w.place || s.doing !== 'work' || !pl) return false;
      const poly = scope.bayPolys[pl.id];
      return poly ? inPoly(at, poly) : dist(pl.at as V, at) <= (('near' in r) ? r.near : 4);
    }).map(p => p.id);
    return { role: r.id, ok: who.length > 0, who, why: who.length ? `${who.length} on shift at ${r.dest ?? 'the Office'}` : `nobody is ${r.what} at ${r.dest ?? 'start'}` };
  });
}
