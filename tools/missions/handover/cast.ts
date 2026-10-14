// The Handover's roles cast from the city's people (brief 010 D; GDD §07, §01 systems > scripts): the mission names roles
// (contracts/missions/handover/mission.json `roles`), never people. For each role, the people who fit it are those of its employer
// in its position or role, on shift for the role's whole window, and (if the role stands at a mission point) at their work place at
// the window's start, that place standing at the point (a bay: its polygon holds the point; a door: within `near` m). The pin is
// the first who fits by person id (prefix, then number), unless the mission already pins one, which must still fit. A person fills
// one role at most. Read by the overlay (rule M-2) and by the people checker (P-6, tools/people/handover.ts), so both cast alike.
// Pure: no file is read here.
import { dist, inPoly, type V } from '../../plan/geom';

export type Role = {
  id: string; what: string; employer: string; position?: string; role?: string;
  at?: string; near?: number; shift: { from: string; to: string }; pin?: string;
};
export type CastMission = { clock: { startsAt: string }; destinations: { name: string; at: [number, number] | number[] }[]; roles: Role[] };
/** Only what casting reads of a person (contracts/people/people.schema.ts Person). */
export type Castable = {
  id: string; name: string;
  work: { employer: string; position?: string; role?: string; place: string; shift: { from: number; to: number } } | null;
  routine: { stops: { place: string; doing: string; from: number; to: number }[] };
};
export type Cast = { role: string; ok: boolean; who: string[]; pinned?: string; name?: string; why: string };
export type Where = {
  /** a mission point (block frame) in the plan frame */
  toPlan: (p: V) => V;
  /** a place's position, plan frame */
  placeAt: (id: string) => V | undefined;
  /** a bay place's polygon, plan frame; undefined for a door */
  polyOf: (id: string) => V[] | undefined;
};

/** Default reach of a door role (m) when the role gives no `near`. [S] */
export const NEAR_M = 4;
const minute = (hhmm: string) => { const [h, m, s] = hhmm.split(':').map(Number); return h! * 60 + m! + (s ?? 0) / 60; };
const offsetMin = (at: string) => { const [m, s] = at.split(':').map(Number); return m! + s! / 60; };
const inShift = (s: { from: number; to: number }, t: number) => (s.from <= s.to ? t >= s.from && t < s.to : t >= s.from || t < s.to);
/** The role's window in day minutes [from, to], from clock.startsAt plus the role's mission-clock offsets. */
export const windowOf = (m: CastMission, r: Role) => [minute(m.clock.startsAt) + offsetMin(r.shift.from), minute(m.clock.startsAt) + offsetMin(r.shift.to)].map(t => t % 1440) as [number, number];
/** Person ids in a stable order: the id's prefix, then its number (p-cafe-9 before p-cafe-10). */
export const byId = (a: string, b: string) => {
  const sa = a.match(/^(.*?)(\d+)$/), sb = b.match(/^(.*?)(\d+)$/);
  const pa = sa?.[1] ?? a, pb = sb?.[1] ?? b;
  return pa < pb ? -1 : pa > pb ? 1 : Number(sa?.[2] ?? 0) - Number(sb?.[2] ?? 0) || (a < b ? -1 : a > b ? 1 : 0);
};
const kind = (r: Role) => `${r.position ?? r.role} of ${r.employer}`;

export function castRoles(persons: Castable[], m: CastMission, where: Where): Cast[] {
  const taken = new Set<string>();
  return m.roles.map((r): Cast => {
    const d = r.at ? m.destinations.find(x => x.name === r.at) : undefined;
    if (r.at && !d) return { role: r.id, ok: false, who: [], why: `the mission has no destination ${r.at}` };
    const point = d ? where.toPlan(d.at as V) : undefined, [t0, t1] = windowOf(m, r);
    const fits = (p: Castable) => {
      const w = p.work; if (!w || w.employer !== r.employer) return false;
      if (r.position ? w.position !== r.position : w.role !== r.role) return false;
      if (!inShift(w.shift, t0) || !inShift(w.shift, t1)) return false;
      if (!point) return true;
      const s = p.routine.stops.find(x => t0 >= x.from && t0 < x.to), at = where.placeAt(w.place);
      if (!s || s.place !== w.place || s.doing !== 'work' || !at) return false;
      const poly = where.polyOf(w.place);
      return poly ? inPoly(point, poly) : dist(at, point) <= (r.near ?? NEAR_M);
    };
    const who = persons.filter(fits).map(p => p.id).sort(byId), free = who.filter(id => !taken.has(id));
    const place = r.at ? ` at ${r.at}` : '', win = `${r.shift.from}-${r.shift.to}`;
    if (r.pin) {
      if (!who.includes(r.pin)) return { role: r.id, ok: false, who, why: `pinned ${r.pin} is no longer a ${kind(r)} on shift ${win}${place}` };
      if (taken.has(r.pin)) return { role: r.id, ok: false, who, why: `pinned ${r.pin} already fills another role` };
    }
    const pinned = r.pin ?? free[0];
    if (!pinned) return { role: r.id, ok: false, who, why: who.length ? `everyone who fits already fills another role (${who.join(', ')})` : `nobody is a ${kind(r)} on shift ${win}${place}` };
    taken.add(pinned);
    return { role: r.id, ok: true, who, pinned, name: persons.find(p => p.id === pinned)!.name, why: `${pinned} of ${who.length} on shift ${win}${place}${r.pin ? ' (pinned in the mission)' : ' (first by id)'}` };
  });
}
