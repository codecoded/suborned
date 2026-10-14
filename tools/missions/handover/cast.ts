// The Handover's roles cast from the city's people (brief 010 D; GDD §07, §01 systems > scripts): the mission names roles
// (contracts/missions/handover/mission.json `roles`), never people. For each role, the people who fit it are those of its employer
// in its position or role, on shift for the role's whole window, and (if the role stands at a mission point) at their work place at
// the window's start, that place standing at the point (a bay: its polygon holds the point; a door: within `near` m). The pin is
// the first who fits by person id (prefix, then number), unless the mission already pins one, which must still fit. A person fills
// one role at most. Read by the overlay (rule M-2) and by the people checker (P-6, tools/people/handover.ts), so both place alike.
// With `rules` (M-2; brief 010 follow-up, Motive §3, Sky Q4) a role also carries what the person must be, and a fit must meet it:
// `need` (the person's one need), `approach` (the planned approach: the person's temperament must not refuse it, rules
// temperaments[t].refuses; the wrong temperament still works one step worse, so only a refusal fails) and `regularOf` (an
// employer: a regular tie to one of its staff; §05 the guard's café stop, §08 the keeper's tip).
// Without `rules` (P-6) only employer, position, shift and place count, so P-6 and M-2 can pin different people: M-2's pin is
// the mission's. Pure: no file is read here.
import { dist, inPoly, type V } from '../../plan/geom';

export type Role = {
  id: string; what: string; employer: string; position?: string; role?: string;
  at?: string; near?: number; shift: { from: string; to: string }; pin?: string;
  need?: string; approach?: string; regularOf?: string;
};
export type CastMission = { clock: { startsAt: string }; destinations: { name: string; at: [number, number] | number[] }[]; roles: Role[] };
/** Only what casting reads of a person (contracts/people/people.schema.ts Person). */
export type Castable = {
  id: string; name: string;
  work: { employer: string; position?: string; role?: string; place: string; shift: { from: number; to: number } } | null;
  routine: { stops: { place: string; doing: string; from: number; to: number }[] };
  /** read only when castRoles is given `rules`; a missing one fails the condition that reads it */
  temperament?: string; need?: string; ties?: { kind: string; with: string }[];
};
/** What a role's conditions read of contracts/people/people.rules.json: each temperament's refusals, each need's approaches. */
export type CastRules = { temperaments: Record<string, { refuses: readonly string[] }>; needs: Record<string, { metBy: readonly string[] }> };
export type Cast = { role: string; ok: boolean; who: string[]; pinned?: string; name?: string; why: string };
export type Where = {
  /** a mission point (block frame) in the plan frame */
  toPlan: (p: V) => V;
  /** a place's position, plan frame */
  placeAt: (id: string) => V | undefined;
  /** a bay place's polygon, plan frame; undefined for a door */
  polyOf: (id: string) => V[] | undefined;
  /** a place's plot (no longer read: `regularOf` is a regular tie only) */
  plotOf?: (id: string) => string | undefined;
};

/** "A regular of X" (GDD §05, §08), one definition for M-2, P-6, the review and the critic: the person holds a `regular` tie to
 *  someone who works for X. A pre-shift stop alone is not enough: the keeper's tip on the guard (§08) runs along the tie. */
export const hasRegularTie = (p: { ties?: readonly { kind: string; with: string }[] }, staff: ReadonlySet<string>) => !!p.ties?.some(t => t.kind === 'regular' && staff.has(t.with));
/** The share of a position's people that must hold that tie for the requirement to read met. [S] */
export const REGULAR_SHARE_MIN = 0.5;

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

type Cond = { name: string; ok: (p: Castable) => boolean; has: (p: Castable) => string };
/** The role's conditions beyond its place, in the order they are reported. */
function conditions(r: Role, persons: Castable[], where: Where, rules: CastRules): Cond[] {
  const out: Cond[] = [];
  if (r.need) out.push({ name: `need ${r.need}`, ok: p => p.need === r.need, has: p => `${p.id} ${p.need ?? 'no need'}` });
  if (r.approach) {
    const a = r.approach, t = (p: Castable) => (p.temperament ? rules.temperaments[p.temperament] : undefined);
    out.push({ name: `a temperament that does not refuse ${a}`, ok: p => !!t(p) && !t(p)!.refuses.includes(a), has: p => `${p.id} ${p.temperament ?? 'no temperament'}${t(p) ? '' : ' (unknown)'}` });
  }
  if (r.regularOf) {
    const e = r.regularOf, ids = new Set(persons.filter(p => p.work?.employer === e).map(p => p.id));
    out.push({ name: `a regular tie to the staff of ${e}`, ok: p => hasRegularTie(p, ids), has: p => `${p.id} none` });
  }
  return out;
}
/** A role that cannot hold under the rules: an unknown need, or an approach that does not act on the need. */
function unsound(r: Role, rules: CastRules): string | undefined {
  if (r.need && !rules.needs[r.need]) return `need ${r.need} is not in the rules' needs`;
  if (r.need && r.approach && !rules.needs[r.need]!.metBy.includes(r.approach)) return `${r.approach} does not act on need ${r.need} (metBy ${rules.needs[r.need]!.metBy.join(', ')})`;
  return undefined;
}

/** Cast every role. `rules` given (M-2): the role's need, approach and regularOf must hold too; absent (P-6): place only. */
export function castRoles(persons: Castable[], m: CastMission, where: Where, rules?: CastRules): Cast[] {
  const taken = new Set<string>();
  return m.roles.map((r): Cast => {
    const d = r.at ? m.destinations.find(x => x.name === r.at) : undefined;
    if (r.at && !d) return { role: r.id, ok: false, who: [], why: `the mission has no destination ${r.at}` };
    const bad = rules && unsound(r, rules);
    if (bad) return { role: r.id, ok: false, who: [], why: bad };
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
    const placed = persons.filter(fits).sort((a, b) => byId(a.id, b.id));
    const place = r.at ? ` at ${r.at}` : '', win = `${r.shift.from}-${r.shift.to}`;
    // narrow by each condition in turn; the first that leaves nobody is the one reported
    if (r.pin && rules && !placed.some(p => p.id === r.pin)) return { role: r.id, ok: false, who: [], why: `pinned ${r.pin} is no longer a ${kind(r)} on shift ${win}${place}` };
    let pool = placed;
    for (const c of rules ? conditions(r, persons, where, rules) : []) {
      if (r.pin) { const p = pool.find(x => x.id === r.pin); if (p && !c.ok(p)) return { role: r.id, ok: false, who: [], why: `pinned ${r.pin} lacks ${c.name}: ${c.has(p)}` }; }
      const next = pool.filter(c.ok);
      if (!next.length && pool.length) return { role: r.id, ok: false, who: [], why: `none of the ${pool.length} who fit (${kind(r)} on shift ${win}${place}) has ${c.name}: ${pool.map(c.has).join(', ')}` };
      pool = next;
    }
    const who = pool.map(p => p.id), free = who.filter(id => !taken.has(id));
    if (r.pin) {
      if (!who.includes(r.pin)) return { role: r.id, ok: false, who, why: `pinned ${r.pin} is no longer a ${kind(r)} on shift ${win}${place}` };
      if (taken.has(r.pin)) return { role: r.id, ok: false, who, why: `pinned ${r.pin} already fills another role` };
    }
    const pinned = r.pin ?? free[0];
    if (!pinned) return { role: r.id, ok: false, who, why: who.length ? `everyone who fits already fills another role (${who.join(', ')})` : `nobody is a ${kind(r)} on shift ${win}${place}` };
    taken.add(pinned);
    const met = rules ? [r.need && `need ${r.need}`, r.approach && `${r.approach} not refused`, r.regularOf && `regular of ${r.regularOf}`].filter(Boolean) : [];
    return { role: r.id, ok: true, who, pinned, name: persons.find(p => p.id === pinned)!.name, why: `${pinned} of ${who.length} on shift ${win}${place}${met.length ? `, ${met.join(', ')}` : ''}${r.pin ? ' (pinned in the mission)' : ' (first by id)'}` };
  });
}
