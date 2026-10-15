// What each canned query must answer, worked out in TypeScript straight from the people JSON (suborned.people/1), the rules and the scope's few extras, with no graph in sight.
// verify.ts holds the database's answers to these; the tests hold the model to them. A disagreement is a bug in the load, the schema or the query, never in the JSON.
import type { People, Person } from '../../contracts/people/people.schema';
import type { Rules } from '../people/rules';
import type { Extras } from './model';

export type Ref = { people: People; rules: Rules; extras: Extras };
const byId = (r: Ref) => new Map(r.people.persons.map(p => [p.id, p]));
export const cmp = (a: string, b: string) => (a < b ? -1 : a > b ? 1 : 0);
const sorted = (xs: Iterable<string>) => [...xs].sort(cmp);

/** Person ids a tie joins, either way round (a turned contact introduces along a tie in either direction). `only`: tie kinds that introduce. */
export function adjacency(r: Ref, only: 'introducing' | 'all'): Map<string, Set<string>> {
  const adj = new Map<string, Set<string>>(r.people.persons.map(p => [p.id, new Set()]));
  for (const p of r.people.persons) for (const t of p.ties) {
    if (only === 'introducing' && !r.rules.ties.kinds[t.kind as keyof Rules['ties']['kinds']]?.introduces) continue;
    adj.get(p.id)!.add(t.with); adj.get(t.with)!.add(p.id);
  }
  return adj;
}

/** Breadth-first distances from `from` over `adj`. */
export function distances(adj: Map<string, Set<string>>, from: string): Map<string, number> {
  const d = new Map([[from, 0]]), queue = [from];
  for (let i = 0; i < queue.length; i++) for (const n of adj.get(queue[i]!) ?? []) if (!d.has(n)) { d.set(n, d.get(queue[i]!)! + 1); queue.push(n); }
  return d;
}

/** who-can-introduce: the length of the shortest chain of introducing ties; undefined when there is none. */
export const introHops = (r: Ref, a: string, b: string): number | undefined => distances(adjacency(r, 'introducing'), a).get(b);
/** Whether `a` and `b` are joined by an introducing tie, either way round, and of which kinds. */
export function introKinds(r: Ref, a: string, b: string): string[] {
  const m = byId(r), kinds = new Set<string>();
  for (const [x, y] of [[a, b], [b, a]] as const) for (const t of m.get(x)?.ties ?? []) if (t.with === y && r.rules.ties.kinds[t.kind as keyof Rules['ties']['kinds']]?.introduces) kinds.add(t.kind);
  return sorted(kinds);
}

export const unitOfPlace = (r: Ref, place: string): string | undefined => {
  const e = r.extras, hit = e.units.find(u => u.door === place || u.official === place || u.bay === place)?.id;
  return hit ?? (e.cafeSide.includes(place) ? e.cafeUnit : undefined);
};

/** regulars-of: each person with a regular tie to staff of the place's unit, the staff they are regular of, and their stops at the unit's places (at `at`, if given: only those there then). */
export function regularsOf(r: Ref, place: string, at?: number): { id: string; regular_of: string[]; stops: string[] }[] {
  const unit = unitOfPlace(r, place); if (!unit) return [];
  const staff = new Set(r.people.persons.filter(p => p.work?.unit === unit).map(p => p.id)), out: { id: string; regular_of: string[]; stops: string[] }[] = [];
  for (const p of r.people.persons) {
    const of = sorted(new Set(p.ties.filter(t => t.kind === 'regular' && staff.has(t.with)).map(t => t.with)));
    if (!of.length) continue;
    const stops = p.routine.stops.filter(s => unitOfPlace(r, s.place) === unit && (at === undefined || (s.from <= at && s.to > at))).map(s => `${s.place}@${s.from}-${s.to}:${s.doing}`);
    if (at === undefined || stops.length) out.push({ id: p.id, regular_of: of, stops: stops.sort(cmp) });
  }
  return out.sort((a, b) => cmp(a.id, b.id));
}

/** who-is-near: every stop overlapping `at` at a place within `radius` metres of `place` (straight line, plan frame). */
export function whoIsNear(r: Ref, place: string, at: number, radius: number): { person: string; place: string; metres: number; doing: string; from: number; to: number }[] {
  const at2 = new Map(r.people.places.map(p => [p.id, p.at])), here = at2.get(place); if (!here) return [];
  const out: { person: string; place: string; metres: number; doing: string; from: number; to: number }[] = [];
  for (const p of r.people.persons) for (const s of p.routine.stops) {
    const there = at2.get(s.place); if (!there) continue;
    const metres = Math.hypot(there[0] - here[0], there[1] - here[1]);
    if (metres <= radius && s.from <= at && s.to > at) out.push({ person: p.id, place: s.place, metres, doing: s.doing, from: s.from, to: s.to });
  }
  return out.sort((a, b) => a.metres - b.metres || cmp(a.person, b.person));
}

/** guards-who-are-cafe-regulars: people of `position` with a regular tie to staff of `unit`, and which staff. */
export function regularsOfUnit(r: Ref, position: string, unit: string): { id: string; staff: string[] }[] {
  const staff = new Set(r.people.persons.filter(p => p.work?.unit === unit).map(p => p.id));
  return r.people.persons.filter(p => p.position === position).map(p => ({ id: p.id, staff: sorted(new Set(p.ties.filter(t => t.kind === 'regular' && staff.has(t.with)).map(t => t.with))) })).filter(x => x.staff.length).sort((a, b) => cmp(a.id, b.id));
}

/** needs-known-by-keeper: whose need the keeper knows, with that person's need and temperament. */
export function needsKnownBy(r: Ref, keeper: string): { id: string; need: string; temperament: string; via: string[] }[] {
  const m = byId(r), k = r.people.knowledge?.needsKnown.find(x => x.keeper === keeper), kp = m.get(keeper);
  return (k?.knows ?? []).map(id => {
    const q = m.get(id)!, via: string[] = [];
    if (kp?.ties.some(t => t.kind === 'workmate' && t.with === id)) via.push('workmate');
    if (q.ties.some(t => t.kind === 'regular' && m.get(t.with)?.work?.unit === kp?.work?.unit)) via.push('unit-regular');
    return { id, need: q.need, temperament: q.temperament, via };
  }).sort((a, b) => cmp(a.id, b.id));
}

/** role-fit: per role in mission order, the people cast in it (pinned first) with need, temperament, whether they meet the role's need, approach and regularOf, and their ties. */
export function roleFit(r: Ref): { role: string; people: { id: string; pinned: boolean; need: string; temperament: string; need_met: boolean; approach_ok: boolean; regular_ok: boolean; ties: string[] }[] }[] {
  const m = byId(r);
  return r.extras.roles.map(role => {
    const cast = r.extras.casts.find(c => c.role === role.id), staff = new Set(r.people.persons.filter(p => p.work?.employer === role.regularOf).map(p => p.id));
    const people = [...(cast?.who ?? [])].sort((a, b) => Number(b === cast?.pinned) - Number(a === cast?.pinned) || cmp(a, b)).map(id => {
      const p = m.get(id)!, refuses = r.rules.temperaments[p.temperament as keyof Rules['temperaments']].refuses as readonly string[];
      return {
        id, pinned: id === cast?.pinned, need: p.need, temperament: p.temperament,
        need_met: !role.need || p.need === role.need, approach_ok: !role.approach || !refuses.includes(role.approach),
        regular_ok: !role.regularOf || p.ties.some(t => t.kind === 'regular' && staff.has(t.with)), ties: tiesOf(r, p),
      };
    });
    return { role: role.id, people };
  });
}
/** A person's ties as `kind>other` (a tie they hold) or `kind<other` (held by the other), sorted; a two-way tie reads once, as `kind=other`. */
export function tiesOf(r: Ref, p: Person): string[] {
  const out = new Set<string>(), spec = (k: string) => r.rules.ties.kinds[k as keyof Rules['ties']['kinds']];
  for (const t of p.ties) out.add(spec(t.kind)?.direction === 'both' ? `${t.kind}=${t.with}` : `${t.kind}>${t.with}`);
  for (const q of r.people.persons) for (const t of q.ties) if (t.with === p.id && spec(t.kind)?.direction !== 'both') out.add(`${t.kind}<${q.id}`);
  return sorted(out);
}

/** lovers: each lover pair once (a = the lower id), both ends' positions and secrets. */
export function lovers(r: Ref): { a: string; b: string; a_position: string; b_position: string; a_secret?: string; b_secret?: string }[] {
  const m = byId(r), out: { a: string; b: string; a_position: string; b_position: string; a_secret?: string; b_secret?: string }[] = [];
  for (const p of r.people.persons) for (const t of p.ties) if (t.kind === 'lover' && p.id < t.with) {
    const q = m.get(t.with)!;
    out.push({ a: p.id, b: q.id, a_position: p.position, b_position: q.position, ...(p.secret ? { a_secret: p.secret } : {}), ...(q.secret ? { b_secret: q.secret } : {}) });
  }
  return out.sort((x, y) => cmp(x.a, y.a));
}

/** neighbourhood: the people 1 and 2 ties away over every kind, either direction. */
export function neighbourhood(r: Ref, id: string): { hop1: string[]; hop2: string[] } {
  const d = distances(adjacency(r, 'all'), id);
  return { hop1: sorted([...d].filter(([, n]) => n === 1).map(([k]) => k)), hop2: sorted([...d].filter(([, n]) => n === 2).map(([k]) => k)) };
}
