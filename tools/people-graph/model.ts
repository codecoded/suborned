// The people graph as plain data: nodes and edges built from the expanded people (suborned.people/1), the people rules and a few facts only the scope and the mission know
// (which unit a place belongs to, the café's side, the Handover's roles and who they are cast as). Pure and deterministic: the same inputs give the same Model, so the same hash.
// Nothing here reads a file or a server. The JSON stays the source of truth; this is a view of it (README.md).
import type { People, Person } from '../../contracts/people/people.schema';
import { inShift } from '../people/expand';
import type { Rules } from '../people/rules';

/** Bump with any change to schema.surql or to what this file writes: it is part of the hash, so a new version loads into a database of its own. */
export const SCHEMA_VERSION = 1;

export class Rec { constructor(readonly table: string, readonly id: string) {} }
export type Node = { table: string; id: string; fields: Record<string, unknown> };
export type Edge = { table: string; id: string; from: Rec; to: Rec; fields: Record<string, unknown> };
export type Counts = { nodes: Record<string, number>; edges: Record<string, number>; ties: Record<string, number> };
export type Model = { nodes: Node[]; edges: Edge[]; counts: Counts };

/** A Handover role as the mission names it (contracts/missions/handover/mission.json `roles`) and what the casting made of it. */
export type RoleIn = {
  id: string; what: string; employer: string; position?: string; role?: string; at?: string; near?: number; shift: { from: string; to: string };
  need?: string; approach?: string; regularOf?: string; tiedTo?: { kind: string; role: string; cast?: boolean }[]; leavesTo?: { at: string; by: string }; waitsFor?: string; alone?: boolean;
};
export type RoleCast = { role: string; ok: boolean; who: string[]; pinned?: string; why: string };
/** What the people file does not carry. `units`: the places each unit owns (scope.units); `cafeUnit`, `cafeSide`: the café and its customers' side (scope); `sets`: the chairs of each café set (scope.sets);
 *  `roles`, `casts`, `castMode`: the Handover's roles and who is cast in them (tools/people/roles.ts's casting; the mode says which conditions held). */
export type Extras = {
  units: { id: string; door?: string; official?: string; bay?: string }[]; cafeUnit: string; cafeSide: string[]; sets: { id: string; seats: string[] }[];
  roles: RoleIn[]; casts: RoleCast[]; castMode: 'play' | 'rules' | 'place';
};

/** Where every field a person carries lands. A field of the people schema missing here fails the test: a new field is a decision, not a silent drop (unknown = reject). */
export const PERSON_FIELDS: Record<keyof Person, string> = {
  id: 'the record id, person:⟨p-…⟩', name: 'person.name', tier: 'person.tier', home: 'lives_at (person -> place) + person.home_plot', work: 'works_at (person -> unit: position, post, place, shift)',
  routine: 'stops_at (one edge per stop) + sits_at (stops at a chair) + person.routine_variant', temperament: 'person.temperament + has_temperament', need: 'person.need + has_need',
  institution: 'person.institution', start: 'person.start', memory: 'person.memory', ties: 'tie (person -> person)', look: 'person.look', position: 'person.position (and works_at.position)',
  needWeights: 'person.need_weights (and has_need.weight for the person\'s own need)',
};

const by = <T>(key: (x: T) => string) => (a: T, b: T) => (key(a) < key(b) ? -1 : key(a) > key(b) ? 1 : 0);
const some = <T>(x: T | null | undefined): T | undefined => (x === null ? undefined : x);
const shiftId = (unit: string, s: { from: number; to: number }) => `${unit}-${s.from}-${s.to}`;

/** The stop of the carrier's day a regular tie is made for: among a's stops away from work that stand at b's work place (or at the café's customers' side, for the café's staff) while
 *  b is on shift, the pre-shift stop first, then the evening, then any other (the rule P-7 holds, tools/people/check.ts). undefined: none (the checker would fail the tie). */
export function regularAt(a: Person, b: Person, x: Pick<Extras, 'cafeUnit' | 'cafeSide'>): string | undefined {
  if (!b.work) return undefined;
  const w = b.work, hits = a.routine.stops.filter(s => s.doing !== 'home' && s.place !== 'beyond' && (s.place === w.place || (w.unit === x.cafeUnit && ['pre-shift', 'evening'].includes(s.doing) && x.cafeSide.includes(s.place)))
    && (inShift(w.shift, s.from) || inShift(w.shift, Math.max(s.from, s.to - 1))));
  for (const d of ['pre-shift', 'evening']) if (hits.some(s => s.doing === d)) return d;
  return hits[0]?.doing;
}

export function buildModel(people: People, rules: Rules, x: Extras): Model {
  const nodes: Node[] = [], edges: Edge[] = [], persons = [...people.persons].sort(by(p => p.id)), byId = new Map(persons.map(p => [p.id, p]));
  const node = (table: string, id: string, fields: Record<string, unknown>) => nodes.push({ table, id, fields });
  const edge = (table: string, id: string, from: Rec, to: Rec, fields: Record<string, unknown> = {}) => edges.push({ table, id, from, to, fields });

  // ---- lookups: only what the rules list
  for (const [id, t] of Object.entries(rules.temperaments)) node('temperament', id, { name: t.name, responds_to: [...t.respondsTo], refuses: [...t.refuses], reports_approach: t.reportsApproach, institution: t.institution, hero: t.hero, menace: t.menace, start: { ...t.start } });
  for (const [id, n] of Object.entries(rules.needs.list)) node('need', id, { met_by: [...n.metBy], help: n.help, requires: n.requires });
  for (const [id, k] of Object.entries(rules.ties.kinds)) node('tie_kind', id, { direction: k.direction, between: k.between, introduces: k.introduces, max_per_person: k.maxPerPerson, max_incoming: some((k as { maxIncoming?: number }).maxIncoming) });

  // ---- places, units, shifts
  const unitOfPlace = new Map<string, string>(), setOfPlace = new Map<string, string>();
  for (const u of x.units) for (const p of [u.door, u.official, u.bay]) if (p) unitOfPlace.set(p, u.id);
  for (const p of x.cafeSide) unitOfPlace.set(p, x.cafeUnit);
  for (const s of x.sets) for (const p of s.seats) setOfPlace.set(p, s.id);
  const bg = new Map(people.background.map(b => [b.plot, b])), placeIds = new Set(people.places.map(p => p.id));
  const beyond = persons.some(p => p.home.place === 'beyond' || p.routine.stops.some(s => s.place === 'beyond'));
  for (const p of [...people.places].sort(by(p => p.id))) {
    const b = p.kind === 'home' ? bg.get(p.plot ?? '') : undefined, unit = unitOfPlace.get(p.id);
    node('place', p.id, { kind: p.kind, name: p.name, at: [...p.at], area: p.area, plot: p.plot, unit: unit ? new Rec('unit', unit) : undefined, set: setOfPlace.get(p.id), residents: b?.residents, simulated: b?.simulated });
  }
  if (beyond) node('place', 'beyond', { kind: 'beyond', name: 'Beyond the sheet (off the map)' });
  const placeRec = (id: string) => { if (id !== 'beyond' && !placeIds.has(id)) throw new Error(`a person stands at ${id}, which is not a place of the people file`); return new Rec('place', id); };
  const unitOf = new Map(x.units.map(u => [u.id, u]));
  for (const u of [...people.units].sort(by(u => u.id))) {
    const e = unitOf.get(u.id);
    node('unit', u.id, { name: u.name, plot: u.plot, area: u.area, trade: u.trade, employer: u.employer, staff_per_shift: u.staffPerShift, hours: [...u.hours], door: e?.door ? placeRec(e.door) : undefined, official: e?.official ? placeRec(e.official) : undefined, bay: e?.bay ? placeRec(e.bay) : undefined });
  }
  const shifts = new Map<string, { unit: string; from: number; to: number }>();
  for (const u of people.units) for (const s of u.shifts) shifts.set(shiftId(u.id, s), { unit: u.id, from: s.from, to: s.to });
  for (const p of persons) if (p.work) shifts.set(shiftId(p.work.unit, p.work.shift), { unit: p.work.unit, from: p.work.shift.from, to: p.work.shift.to });
  for (const [id, s] of [...shifts].sort(by(([id]) => id))) node('shift', id, { unit: new Rec('unit', s.unit), from_min: s.from, to_min: s.to, wraps: s.to <= s.from });

  // ---- roles
  for (const [seq, r] of x.roles.entries()) {
    const c = x.casts.find(k => k.role === r.id);
    node('role', r.id, { seq, what: r.what, employer: r.employer, position: r.position, civic_role: r.role, at: r.at, near: r.near, window_from: r.shift.from, window_to: r.shift.to, need: r.need ? new Rec('need', r.need) : undefined, approach: r.approach, regular_of: r.regularOf, tied_to: r.tiedTo, leaves_to: r.leavesTo, waits_for: r.waitsFor, alone: r.alone, cast_ok: c?.ok ?? false, cast_mode: x.castMode, cast_why: c?.why ?? 'not cast' });
  }

  // ---- persons and what they link to
  for (const p of persons) {
    node('person', p.id, { name: p.name, tier: p.tier, position: p.position, temperament: new Rec('temperament', p.temperament), need: new Rec('need', p.need), need_weights: { ...p.needWeights }, institution: some(p.institution), start: { ...p.start }, memory: [...p.memory], look: p.look.block, routine_variant: p.routine.variant, home_plot: p.home.plot });
    const me = new Rec('person', p.id);
    if (!(p.temperament in rules.temperaments)) throw new Error(`${p.id}: temperament ${p.temperament} is not in the rules`);
    if (!(p.need in rules.needs.list)) throw new Error(`${p.id}: need ${p.need} is not in the rules`);
    const weight = p.needWeights[p.need];
    if (weight === undefined) throw new Error(`${p.id}: need ${p.need} is not among the needs its row offers (${Object.keys(p.needWeights).join(', ')})`);
    edge('has_need', p.id, me, new Rec('need', p.need), { weight });
    edge('has_temperament', p.id, me, new Rec('temperament', p.temperament));
    edge('lives_at', p.id, me, placeRec(p.home.place));
    if (p.work) {
      const w = p.work;
      edge('works_at', p.id, me, new Rec('unit', w.unit), { position: w.position ?? w.role, civic: w.role !== undefined, post: w.post, place: placeRec(w.place), shift: new Rec('shift', shiftId(w.unit, w.shift)), shift_from: w.shift.from, shift_to: w.shift.to, employer: w.employer });
    }
    p.routine.stops.forEach((s, seq) => {
      const at = placeRec(s.place);
      edge('stops_at', `${p.id}~${seq}`, me, at, { seq, doing: s.doing, from_min: s.from, to_min: s.to });
      if (people.places.find(q => q.id === s.place)?.kind === 'seat') edge('sits_at', `${p.id}~${seq}`, me, at, { doing: s.doing, from_min: s.from, to_min: s.to });
    });
  }

  // ---- ties: the file carries a two-way tie on both people (returned), a directed tie on its source
  const done = new Set<string>(), ties: Record<string, number> = {};
  for (const p of persons) for (const t of p.ties) {
    const spec = rules.ties.kinds[t.kind as keyof Rules['ties']['kinds']];
    if (!spec) throw new Error(`${p.id}: tie kind ${t.kind} is not in the rules`);
    const q = byId.get(t.with); if (!q) throw new Error(`${p.id}: ${t.kind} tie to unknown ${t.with}`);
    const mutual = spec.direction === 'both', [a, b] = mutual && q.id < p.id ? [q, p] : [p, q], id = `${a.id}~${t.kind}~${b.id}`;
    if (mutual && !q.ties.some(y => y.kind === t.kind && y.with === p.id)) throw new Error(`${p.id}: ${t.kind} tie to ${q.id} is not returned`);
    if (done.has(id)) continue;
    done.add(id);
    edge('tie', id, new Rec('person', a.id), new Rec('person', b.id), { kind: new Rec('tie_kind', t.kind), introduces: spec.introduces, mutual, at: t.kind === 'regular' ? regularAt(a, b, x) : undefined });
    ties[t.kind] = (ties[t.kind] ?? 0) + 1;
  }

  // ---- what a keeper knows (rules knowledge; tools/people/knowledge.ts), and through which tie
  for (const k of people.knowledge?.needsKnown ?? []) {
    const keeper = byId.get(k.keeper); if (!keeper) throw new Error(`knowledge: keeper ${k.keeper} is not a person`);
    for (const id of k.knows) {
      const q = byId.get(id); if (!q) throw new Error(`knowledge: ${k.keeper} knows ${id}, who is not a person`);
      const via: string[] = [];
      if (keeper.ties.some(t => t.kind === 'workmate' && t.with === id)) via.push('workmate');
      if (q.ties.some(t => t.kind === 'regular' && byId.get(t.with)?.work?.unit === keeper.work?.unit)) via.push('unit-regular');
      edge('knows_need_of', `${k.keeper}~${id}`, new Rec('person', k.keeper), new Rec('person', id), { via });
    }
  }

  // ---- who is cast in each role
  for (const c of x.casts) for (const who of [...c.who].sort()) {
    if (!byId.has(who)) throw new Error(`role ${c.role} is cast as ${who}, who is not a person`);
    edge('casts', `${c.role}~${who}`, new Rec('role', c.role), new Rec('person', who), { pinned: c.pinned === who });
  }

  const count = (xs: { table: string }[]) => { const o: Record<string, number> = {}; for (const e of xs) o[e.table] = (o[e.table] ?? 0) + 1; return Object.fromEntries(Object.entries(o).sort(by(([k]) => k))); };
  return { nodes, edges, counts: { nodes: count(nodes), edges: count(edges), ties: Object.fromEntries(Object.entries(ties).sort(by(([k]) => k))) } };
}
