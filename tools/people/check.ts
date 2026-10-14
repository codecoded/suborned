// The people checker (brief 010 B): rule ids P-0.. on the cast tables and the people they expand to. Each rule has an id, a
// source (GDD § or people.rules.json entry) and a failing example in tools/people/test/check.test.ts. The checker holds the
// cast tables to the same rules as the people (a temperament the occupation does not allow fails the table, not only the person).
import { Cast as CastSchema } from '../../contracts/people/cast.schema';
import { People as PeopleSchema, type Person } from '../../contracts/people/people.schema';
import type { Check } from '../plan/checks';
import type { V } from '../plan/geom';
import { expand, inShift, rowOf, type Ctx } from './expand';
import { castHandover, type MissionIn } from './handover';
import { positionsOf } from './rules';
import type { Unit } from './scope';
import { allowedTemperaments, isTalker } from './rules';

export const GROUP = 'People';
export const RULES_SRC: Record<string, string> = {
  'P-0': 'GDD §09 (nothing hand-authored; unknown = reject): the cast tables cover the scope\'s area × occupation pairs, and only those',
  'P-1': 'GDD §09 people checked by: reach their places; plan standards.walking (walk graph)',
  'P-2': 'GDD §04 person = routine + need + temperament + memory; §09 one clear need; people.rules.json needs',
  'P-3': 'people.rules.json temperaments, needs, ties, positions, temperamentsBy: anything not in the file is rejected',
  'P-4': 'life.json generator.occupations staffPerUnit, shifts; rules positions (one keeper per unit)',
  'P-5': 'brief 008 rule CD-1 (keep-clear zones of the civic block dressing)',
  'P-6': 'GDD §07 the Handover: its roles are cast from the city\'s people (brief 010 D)',
  'P-7': 'people.rules.json ties (kinds, limits, direction, between)',
  'P-8': 'people.rules.json names (pool, pattern, origins, avoid, unique, pinned); GDD §15 no era date',
  'P-9': 'GDD §01/§09 same orders → same result; rules person.memoryAtStart, tiers',
};
type R = { id: string; rule: string; bad: string[]; value?: string; limit?: string };
const row = (r: R): Check => ({ id: r.id, group: GROUP, rule: r.rule, value: r.value ?? (r.bad.length ? `${r.bad.length} fault(s)` : 'ok'), limit: r.limit ?? '0 faults', ok: !r.bad.length, ...(r.bad.length ? { detail: r.bad.slice(0, 12).join('; ') + (r.bad.length > 12 ? `; +${r.bad.length - 12} more` : '') } : {}), source: RULES_SRC[r.id] });
const at = (p: Person, t: number) => p.routine.stops.find(s => t >= s.from && t < s.to);

export type Checked = { ok: boolean; checks: Check[]; counts: { persons: number; units: number; faults: number }; handover: ReturnType<typeof castHandover> };

/** `mission`: the Handover mission's clock and destinations (undefined: P-6 fails, there is nothing to cast). */
export function checkPeople(c: Ctx, mission: MissionIn | undefined, plan: { civic: unknown }, given?: ReturnType<typeof expand>): Checked {
  const { scope, rules, cast } = c, out: R[] = [];
  const parsed = CastSchema.safeParse(cast);
  const { people, gaps } = given ?? expand(c);
  const P = people.persons, pairs = scope.units.map(u => `${u.area}/${u.trade}`).concat(scope.homes.length ? [...new Set(scope.homes.map(h => `${h.area}/resident`))] : []);
  const unitOf = new Map(scope.units.map(u => [u.id, u]));

  // P-0 coverage
  {
    const bad: string[] = [];
    if (!parsed.success) bad.push(`cast tables fail their schema: ${parsed.error.issues.slice(0, 3).map(i => i.path.join('.') + ' ' + i.message).join(' | ')}`);
    const have = cast.rows.map(r => `${r.area}/${r.occupation}`), want = new Set(pairs);
    for (const w of want) if (!have.includes(w)) bad.push(`no row for ${w}`);
    for (const h of have) if (!want.has(h)) bad.push(`row ${h} is not in the scope`);
    for (const h of have) if (have.indexOf(h) !== have.lastIndexOf(h)) bad.push(`duplicate row ${h}`);
    if (cast.scope !== scope.id) bad.push(`cast scope ${cast.scope}, expected ${scope.id}`);
    bad.push(...gaps);
    out.push({ id: 'P-0', rule: 'The cast tables cover the scope\'s units and homes, and the expansion has no gaps', bad: [...new Set(bad)], limit: `${want.size} rows` , value: `${have.length} rows` });
  }

  // P-1 reach and time
  {
    const bad: string[] = [], pl = new Map(scope.places.map(p => [p.id, p.at as V]));
    for (const p of P) {
      const st = p.routine.stops;
      for (const s of st) if (s.place !== 'beyond' && !pl.has(s.place)) bad.push(`${p.id}: stop at unknown place ${s.place}`);
      for (const s of st) if (s.place !== 'beyond' && pl.has(s.place) && !c.walk.free(pl.get(s.place)!) ) bad.push(`${p.id}: ${s.place} is not walkable ground`);
      for (let i = 0; i < st.length; i++) {
        const a = st[i]!, b = st[(i + 1) % st.length]!, gap = (b.from - a.to + 1440) % 1440 || (st.length === 1 ? 0 : 0);
        if (a.place === b.place || a.place === 'beyond' || b.place === 'beyond' || !pl.has(a.place) || !pl.has(b.place)) continue;
        const need = c.walk.min(pl.get(a.place)!, pl.get(b.place)!);
        if (need === Infinity) bad.push(`${p.id}: no walk ${a.place} → ${b.place}`);
        else if (gap < need) bad.push(`${p.id}: ${a.place} (to ${a.to}) → ${b.place} (from ${b.from}) leaves ${gap} min, the walk needs ${need}`);
      }
      const cover = st.reduce((n, s) => n + (s.to - s.from), 0), travelling = 1440 - cover;
      if (travelling < 0) bad.push(`${p.id}: stops overlap`);
      const sorted = [...st].sort((x, y) => x.from - y.from);
      for (let i = 1; i < sorted.length; i++) if (sorted[i]!.from < sorted[i - 1]!.to) bad.push(`${p.id}: stops overlap at ${sorted[i]!.from}`);
      if (p.work) { // on shift, the person is at work (or at the meal), and arrives by the shift start
        const w = p.work, s = at(p, w.shift.from);
        if (!s || s.place !== w.place) bad.push(`${p.id}: not at work (${w.place}) when the shift starts at ${w.shift.from}`);
      }
    }
    out.push({ id: 'P-1', rule: 'Everyone reaches each routine stop on the walk graph in time', bad, value: `${P.length} people`, limit: 'all stops reached' });
  }

  // P-2 exactly one clear need
  {
    const bad: string[] = [];
    for (const p of P) {
      const d = rules.needs.list[p.need], t = rules.temperaments[p.temperament as keyof typeof rules.temperaments];
      if (typeof p.need !== 'string' || !p.need) { bad.push(`${p.id}: no need`); continue; }
      if (!d) { bad.push(`${p.id}: need ${p.need} is not in the rules`); continue; }
      if (!t) continue;
      const req = d.requires;
      const ok = req === 'nothing' || (req === 'home' && p.home.plot !== 'beyond') || (req === 'work' && !!p.work) || (req === 'family-tie' && p.ties.some(x => x.kind === 'family')) || (req === 'rival-tie' && p.ties.some(x => x.kind === 'rival'));
      if (!ok) bad.push(`${p.id}: need ${p.need} requires ${req}, which they lack`);
      if (!d.metBy.some(a => (t.respondsTo as readonly string[]).includes(a))) bad.push(`${p.id}: no approach their temperament answers to (${t.respondsTo.join('/')}) meets ${p.need}`);
    }
    out.push({ id: 'P-2', rule: 'Every person has exactly one need, in the rules, that an approach they answer to can meet', bad, value: `${P.length} people`, limit: 'one clear need each' });
  }

  // P-3 only what the rules allow
  {
    const bad: string[] = [];
    const tmp = Object.keys(rules.temperaments), needs = Object.keys(rules.needs.list), kinds = Object.keys(rules.ties.kinds);
    for (const r of cast.rows) {
      const u = scope.units.find(x => x.area === r.area && x.trade === r.occupation), key = `${r.area}/${r.occupation}`;
      const allowKey = (pos: string) => (u?.kind === 'civic' ? pos : u ? u.trade : 'resident');
      for (const t of r.temperaments) {
        if (!tmp.includes(t.temperament)) bad.push(`${key}: temperament ${t.temperament} is not in the rules`);
        else {
          const al = r.positions.map(p => allowedTemperaments(rules, allowKey(p.position)));
          if (al.some(a => !a)) bad.push(`${key}: ${r.positions.find((p, i) => !al[i])!.position} is not a trade or role of the rules`);
          else if (!al.some(a => a!.includes(t.temperament as never))) bad.push(`${key}: no position of the row may be ${t.temperament}`);
        }
      }
      for (const n of r.needs) if (!needs.includes(n.need)) bad.push(`${key}: need ${n.need} is not in the rules`);
      for (const t of r.ties) if (!kinds.includes(t.kind)) bad.push(`${key}: tie kind ${t.kind} is not in the rules`);
      for (const p of r.positions) {
        const okPos = u ? (u.kind === 'civic' ? rules.temperamentsBy.roles[p.position]?.employer === u.employer : positionsOf(rules, u.category).includes(p.position)) : p.position === 'resident';
        if (!okPos) bad.push(`${key}: position ${p.position} is not one of ${u ? u.employer : 'residents'}'s`);
      }
    }
    for (const p of P) {
      const t = p.temperament as keyof typeof rules.temperaments, d = rules.temperaments[t];
      if (!d) { bad.push(`${p.id}: temperament ${p.temperament} is not in the rules`); continue; }
      const key = p.work ? (p.work.role ?? p.work.trade) : 'resident';
      const al = p.work?.role ? allowedTemperaments(rules, p.work.role) : allowedTemperaments(rules, p.work ? p.work.trade : 'resident');
      if (!al?.includes(t)) bad.push(`${p.id}: ${key} may not be ${p.temperament}`);
      if (JSON.stringify(p.start) !== JSON.stringify(d.start)) bad.push(`${p.id}: starting scales differ from ${p.temperament}'s`);
      if (!rules.needs.list[p.need]) bad.push(`${p.id}: need ${p.need} is not in the rules`);
      for (const x of p.ties) if (!kinds.includes(x.kind)) bad.push(`${p.id}: tie kind ${x.kind} is not in the rules`);
      if (p.work) {
        const w = p.work, u = unitOf.get(w.unit);
        if (!u) bad.push(`${p.id}: unit ${w.unit} is not in the scope`);
        else if (u.kind === 'civic' ? !(w.role && rules.temperamentsBy.roles[w.role]?.employer === u.employer) : !(w.position && positionsOf(rules, u.category).includes(w.position))) bad.push(`${p.id}: ${w.role ?? w.position} is not a position of ${u.id}`);
        if (!!w.position === !!w.role) bad.push(`${p.id}: exactly one of position and role`);
      }
      if ((p.temperament === 'loyal') !== (p.institution !== null)) bad.push(`${p.id}: institution ${p.institution} for a ${p.temperament} person`);
      if (isTalker(rules, p.start) && p.temperament !== 'fearful') bad.push(`${p.id}: starts as a Talker`);
    }
    out.push({ id: 'P-3', rule: 'Only the temperaments, needs, ties and positions the rules allow, in the tables and in the people', bad: [...new Set(bad)], value: `${cast.rows.length} rows, ${P.length} people` });
  }

  // P-4 staff counts and keepers
  {
    const bad: string[] = [];
    for (const u of scope.units) {
      const here = P.filter(p => p.work?.unit === u.id);
      u.shifts.forEach((s, i) => {
        const n = here.filter(p => p.work!.shift.from === s.from && p.work!.shift.to === s.to).length;
        if (n !== u.staffPerShift) bad.push(`${u.id} shift ${i} (${s.from}-${s.to}): ${n} staff, life.json says ${u.staffPerShift}`);
      });
      if (here.length !== u.staffPerShift * u.shifts.length) bad.push(`${u.id}: ${here.length} staff in all, expected ${u.staffPerShift * u.shifts.length}`);
      const covered = (t: number) => u.shifts.some(s => inShift(s, t)), o = Math.round(u.hours[0] * 60), cl = Math.round(u.hours[1] * 60);
      for (let t = 0; t < 1440; t += 15) { const open = o < cl ? t >= o && t < cl : t >= o || t < cl; if (open && !covered(t)) bad.push(`${u.id}: no shift at ${Math.floor(t / 60)}:${String(t % 60).padStart(2, '0')} though it is open`); }
      if (u.kind !== 'civic' && positionsOf(rules, u.category).includes('keeper')) {
        const k = here.filter(p => p.work!.position === 'keeper').length;
        if (k !== 1) bad.push(`${u.id}: ${k} keepers, the rules say exactly one`);
      }
    }
    out.push({ id: 'P-4', rule: 'Staff counts match life.json per unit and shift; one keeper per unit', bad, value: `${scope.units.length} units`, limit: 'life.json staffPerUnit' });
  }

  // P-5 keep-clear zones
  {
    const bad: string[] = [], pl = new Map(scope.places.map(p => [p.id, p.at as V]));
    for (const p of P) for (const s of p.routine.stops) {
      const onShift = !!p.work && (s.doing === 'work' || (s.doing === 'meal' && s.place === p.work.place));
      const z = pl.has(s.place) ? scope.zones.find(x => x.test(pl.get(s.place)!)) : undefined;
      if (z && !onShift) bad.push(`${p.id}: ${s.doing} at ${s.place} stands in ${z.id}`);
    }
    out.push({ id: 'P-5', rule: 'No one stands in a CD-1 keep-clear zone off-shift', bad, value: `${scope.zones.length} zones`, limit: '0 off-shift stops inside' });
  }

  // P-6 the Handover casts its roles
  const handover = mission ? castHandover(P, scope, mission, plan) : [];
  out.push({ id: 'P-6', rule: 'The Handover can cast every role it names (waitress, driver, guard, Vale\'s office)', bad: mission ? handover.filter(h => !h.ok).map(h => `${h.role}: ${h.why}`) : ['no mission to cast'], value: `${handover.filter(h => h.ok).length}/${handover.length} roles`, limit: 'all roles' });

  // P-7 ties
  {
    const bad: string[] = [], byId = new Map(P.map(p => [p.id, p])), T = rules.ties.kinds, inc = new Map<string, number>();
    const stopsOf = (p: Person) => p.routine.stops.filter(s => s.doing !== 'home' && s.place !== 'beyond');
    for (const p of P) {
      if (p.ties.length < rules.ties.perPerson.min || p.ties.length > rules.ties.perPerson.max) bad.push(`${p.id}: ${p.ties.length} ties, rules ${rules.ties.perPerson.min}-${rules.ties.perPerson.max}`);
      const seen = new Set<string>();
      for (const t of p.ties) {
        const q = byId.get(t.with), spec = T[t.kind as keyof typeof T];
        if (!q) { bad.push(`${p.id}: ${t.kind} tie to unknown ${t.with}`); continue; }
        if (!spec) continue;
        if (q.id === p.id) bad.push(`${p.id}: tie to self`);
        const pair = spec.direction === 'both' ? [p.id, q.id].sort().join('|') : `${p.id}>${q.id}`;
        if (seen.has(`${t.kind}/${pair}`)) bad.push(`${p.id}: two ${t.kind} ties with ${q.id}`);
        seen.add(`${t.kind}/${pair}`);
        if (spec.direction === 'both' && !q.ties.some(x => x.kind === t.kind && x.with === p.id)) bad.push(`${p.id}: ${t.kind} tie to ${q.id} is not returned`);
        if (spec.direction !== 'both') inc.set(`${t.kind}/${q.id}`, (inc.get(`${t.kind}/${q.id}`) ?? 0) + 1);
        const w = (a: Person, b: Person) => a.work && b.work;
        if (spec.between === 'same-employer' && !(w(p, q) && p.work!.employer === q.work!.employer)) bad.push(`${p.id}: workmate ${q.id} has another employer`);
        if (spec.between === 'same-trade-or-employer' && !(w(p, q) && (p.work!.trade === q.work!.trade || p.work!.employer === q.work!.employer))) bad.push(`${p.id}: rival ${q.id} shares no trade or employer`);
        if (spec.between === 'routine-stop' && !(q.work && stopsOf(p).some(s => s.place === q.work!.place && (inShift(q.work!.shift, s.from) || inShift(q.work!.shift, Math.max(s.from, s.to - 1)))))) bad.push(`${p.id}: regular of ${q.id}, but no stop at their work while they are on shift`);
      }
      for (const k of Object.keys(T)) if (p.ties.filter(t => t.kind === k).length > T[k as keyof typeof T].maxPerPerson) bad.push(`${p.id}: more than ${T[k as keyof typeof T].maxPerPerson} ${k} ties`);
    }
    for (const [k, n] of inc) { const [kind] = k.split('/'); const m = (T[kind as keyof typeof T] as { maxIncoming?: number }).maxIncoming; if (m !== undefined && n > m) bad.push(`${k}: ${n} incoming, limit ${m}`); }
    out.push({ id: 'P-7', rule: 'Ties are of the kinds, limits, directions and pairs the rules allow', bad, value: `${P.reduce((n, p) => n + p.ties.length, 0)} ties`, limit: 'rules ties' });
  }

  // P-8 names
  {
    const bad: string[] = [], n = rules.names, re = new RegExp(n.form.pattern, 'u'), pool = cast.names;
    for (const part of ['given', 'family'] as const) {
      const list = pool[part], min = part === 'given' ? n.minGiven : n.minFamily;
      if (list.length < min) bad.push(`${part} pool has ${list.length}, rules ${min}`);
      const origins = new Map<string, number>();
      for (const e of list) {
        if (!re.test(e.name)) bad.push(`${part} name ${e.name} fails the name pattern`);
        if (n.avoid.includes(e.name)) bad.push(`${part} name ${e.name} is on the avoid list`);
        if (!n.origins.includes(e.origin)) bad.push(`${part} name ${e.name}: origin ${e.origin} is not in the rules`);
        origins.set(e.origin, (origins.get(e.origin) ?? 0) + 1);
      }
      if (origins.size < n.minOrigins) bad.push(`${part} pool has ${origins.size} origins, rules ${n.minOrigins}`);
      for (const [o, k] of origins) if (k / list.length > n.maxOriginShare) bad.push(`${part} pool: ${o} is ${Math.round((100 * k) / list.length)}% of it, limit ${Math.round(n.maxOriginShare * 100)}%`);
      if (new Set(list.map(e => e.name)).size !== list.length) bad.push(`${part} pool repeats a name`);
    }
    const given = new Set([...pool.given.map(e => e.name), ...n.pinned.filter(x => x.part === 'given').map(x => x.name)]);
    const family = new Set([...pool.family.map(e => e.name), ...n.pinned.filter(x => x.part === 'family').map(x => x.name)]);
    const full = new Set<string>(), ids = new Set<string>();
    for (const p of P) {
      const [g, ...f] = p.name.split(' ');
      if (!g || f.length !== 1 || !given.has(g) || !family.has(f[0]!)) bad.push(`${p.id}: name ${p.name} is not drawn from the district pool`);
      if (full.has(p.name)) bad.push(`${p.id}: name ${p.name} is used twice`);
      full.add(p.name);
      if (ids.has(p.id)) bad.push(`${p.id}: id used twice`);
      ids.add(p.id);
    }
    for (const pin of n.pinned) {
      const hit = P.filter(p => p.work?.employer === pin.employer && (pin.position ? p.work.position === pin.position : p.work.role === pin.role) && p.name.split(' ')[pin.part === 'given' ? 0 : 1] === pin.name);
      if (hit.length !== 1) bad.push(`pinned ${pin.part} name ${pin.name}: ${hit.length} people carry it as ${pin.position ?? pin.role} of ${pin.employer}, expected 1`);
    }
    out.push({ id: 'P-8', rule: 'Names come from one district pool of mixed origins, with no era, unique, and the pinned names present once', bad: [...new Set(bad)], value: `${P.length} names`, limit: 'rules names' });
  }

  // P-9 shape, determinism, tiers
  {
    const bad: string[] = [], again = given ? people : expand(c).people;
    const ps = PeopleSchema.safeParse(people);
    if (!ps.success) bad.push(`the people fail their schema: ${ps.error.issues.slice(0, 3).map(i => i.path.join('.') + ' ' + i.message).join(' | ')}`);
    if (JSON.stringify(again) !== JSON.stringify(people)) bad.push('two expansions of the same inputs differ');
    for (const p of P) if (p.memory.length) bad.push(`${p.id}: memory is not empty at the start`);
    for (const b of people.background) if (b.simulated > b.residents) bad.push(`${b.plot}: more simulated people than residents`);
    out.push({ id: 'P-9', rule: 'The expansion is valid, empty of memory, and byte-identical twice', bad, value: `${P.length} people`, limit: 'deterministic' });
  }
  const checks = out.map(row);
  void rowOf; void (null as unknown as Unit);
  return { ok: checks.every(x => x.ok), checks, counts: { persons: P.length, units: scope.units.length, faults: out.reduce((n, r) => n + r.bad.length, 0) }, handover };
}
