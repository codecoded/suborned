// The people checker (brief 010 B): rule ids P-0.. on the cast tables and the people they expand to. Each rule has an id, a
// source (GDD § or people.rules.json entry) and a failing example in tools/people/test/check.test.ts. The checker holds the
// cast tables to the same rules as the people (a temperament the occupation does not allow fails the table, not only the person).
import { Cast as CastSchema } from '../../contracts/people/cast.schema';
import { People as PeopleSchema, Person as PersonSchema, type Person } from '../../contracts/people/people.schema';
import type { Check } from '../plan/checks';
import type { V } from '../plan/geom';
import { drawn, stepMin } from './day';
import { expand, inShift, needsOf, needWeightsOf, routineMeet, rowOf, type Ctx } from './expand';
import { withSecrets } from './secrets';
import { castHandover, type MissionIn } from './handover';
import { deriveKnowledge } from './knowledge';
import { pinnedByRole } from './roles';
import { positionsOf, type Rules } from './rules';
import { seatId, seatPoints, SEAT_TOL, STAND } from './seats';
import { atCafeSide, type Unit } from './scope';
import { allowedTemperaments, isTalker } from './rules';

export const GROUP = 'People';
const PersonShape = PersonSchema.shape as Record<string, unknown>;
/** The rule (a key of people.rules.json person.readBy.fields) that reads each field a person carries. A "~" names a field that no
 *  rule reads but the tools do (an id, a look key): it is not a fact about the person. */
export const FIELD_READER: Record<string, string> = {
  id: '~key: the person\'s key; no rule reads it', tier: '~tiers: rules tiers.simulated (background people are counts, not persons)', name: 'name', temperament: 'temperament', need: 'need',
  institution: 'temperament', start: 'scales', memory: 'memory', ties: 'ties', routine: 'routine', home: 'routine', work: 'routine', look: '~look: the graybox job colour (brief 010 E), no rule reads it',
  position: 'need', needWeights: 'need', secret: 'secret', // the rulebook draws a replacement need from the person\'s row (Sky Q2): keel-rulebook.md point 8
};
export const RULES_SRC: Record<string, string> = {
  'P-0': 'GDD §09 (nothing hand-authored; unknown = reject): the cast tables cover the scope\'s area × occupation pairs, and only those',
  'P-1': 'GDD §09 people checked by: reach their places; plan standards.walking (walk graph)',
  'P-2': 'GDD §04 person = routine + need + temperament + memory; §09 one clear need; people.rules.json needs',
  'P-3': 'people.rules.json temperaments, needs, ties, positions, temperamentsBy: anything not in the file is rejected',
  'P-4': 'life.json generator.occupations staffPerUnit, shifts; rules positions (one keeper per unit)',
  'P-5': 'brief 008 rule CD-1 (keep-clear zones of the civic block dressing)',
  'P-6': 'GDD §07 the Handover: its roles, with their need, approach and regularOf, are cast from the city\'s people by the same casting as M-2 (brief 010 D; Motive §3)',
  'P-7': 'people.rules.json ties (kinds, limits, direction, between)',
  'P-8': 'people.rules.json names (pool, pattern, origins, avoid, unique, pinned); GDD §15 no era date',
  'P-9': 'GDD §01/§09 same orders → same result; rules person.memoryAtStart, tiers',
  'P-10': 'people.rules.json temperamentsBy.mix (P3): no temperament over rowMaxShare of a cast row; a unit of whenStaffAtLeast staff or more carries unitMinDistinct temperaments; GDD §09 risk: sameness',
  'P-11': 'people.rules.json knowledge (P5): a keeper\'s needsKnown derived from workmate and unit-regular ties; the cell is known by face only and nothing else at the start; GDD §03, §08',
  'P-12': 'people.rules.json person.readBy (P2): every field a person carries names the rule that reads it; GDD §09 risk: sameness (Legion\'s bios did nothing)',
  'P-13': 'GDD §09 people checked by: reach their places, no impossible action (two people on one chair); brief 010 follow-up, Aster 2026-10-15: a seat is a chair of a café set the world draws (tools/world/build.ts civicMasses; chair geometry tools/decokit/furnish_cafe.py), a standing point keeps CD-1 clear',
  'P-14': 'people.rules.json ties.kinds.lover (Motive briefs/011/motive-lovers.md §1): mutual, one per person, met on their routines, never family, at most maxShare of the simulated people',
  'P-15': 'people.rules.json secrets (Motive briefs/011/motive-lovers.md §2): 0 or 1 per person from the closed list; affair exactly for everyone with a lover tie; the need secret retired into the record share',
  'P-16': 'brief 014 (Sky 2026-10-16: five identical servers on one point): no two people are drawn on one place at once; a door, a bay or a standing point holds one person at a time, a post is one person\'s',
};
type R = { id: string; rule: string; bad: string[]; value?: string; limit?: string };
const row = (r: R): Check => ({ id: r.id, group: GROUP, rule: r.rule, value: r.value ?? (r.bad.length ? `${r.bad.length} fault(s)` : 'ok'), limit: r.limit ?? '0 faults', ok: !r.bad.length, ...(r.bad.length ? { detail: r.bad.slice(0, 12).join('; ') + (r.bad.length > 12 ? `; +${r.bad.length - 12} more` : '') } : {}), source: RULES_SRC[r.id] });
const at = (p: Person, t: number) => p.routine.stops.find(s => t >= s.from && t < s.to);

export type Checked = { ok: boolean; checks: Check[]; counts: { persons: number; units: number; faults: number }; handover: ReturnType<typeof castHandover> };

/** `mission`: the Handover mission's clock and destinations (undefined: P-6 fails, there is nothing to cast). */
export function checkPeople(c: Ctx, mission: MissionIn | undefined, plan: { civic: unknown }, given?: Pick<ReturnType<typeof expand>, 'people' | 'gaps'>): Checked {
  const { scope, rules } = c, cast = withSecrets(c.cast, rules), out: R[] = [];
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
        const need = stepMin(c.walk, pl.get(a.place)!, pl.get(b.place)!);
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
      for (const n of r.needs) {
        if (!needs.includes(n.need)) bad.push(`${key}: need ${n.need} is not in the rules`);
        for (const x of n.positions ?? []) if (!r.positions.some(p => p.position === x) && x !== 'keeper') bad.push(`${key}: need ${n.need} names position ${x}, which the row does not have`);
      }
      for (const v of r.routines) for (const x of v.positions ?? []) if (!r.positions.some(p => p.position === x) && x !== 'keeper') bad.push(`${key}: routine ${v.id} names position ${x}, which the row does not have`);
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
      // unknown = reject: the need is one the person's own row offers their position (the row's need list, by position), not any need the rules know
      const own = p.work ? rowOf(cast, p.work.area, p.work.trade) : rowOf(cast, scope.homes.find(h => h.plot === p.home.plot)?.area ?? scope.areas[0]!, 'resident'), pos = p.work ? (p.work.position ?? p.work.role) : 'resident';
      if (p.position !== pos) bad.push(`${p.id}: position ${p.position}, their work says ${pos}`);
      if (JSON.stringify(p.needWeights) !== JSON.stringify(needWeightsOf(own, pos))) bad.push(`${p.id}: needWeights are not their row's per mille (${JSON.stringify(needWeightsOf(own, pos))})`);
      if (rules.needs.list[p.need] && !needsOf(own, pos).some(n => n.need === p.need)) bad.push(`${p.id}: need ${p.need} is not one its row (${own ? `${own.area}/${own.occupation}` : 'none'}) offers a ${pos} (${needsOf(own, pos).map(n => n.need).join(', ') || 'none'})`);
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
      // a civic employer's watch: the positions that name one put `count` people on each shift outside office hours, on top of the office staff
      const rowU = rowOf(cast, u.area, u.trade), watchEach = u.kind === 'civic' ? (rowU?.positions ?? []).reduce((k, p) => k + (p.watch?.count ?? 0), 0) : 0, watchN = watchEach * (u.watch?.length ?? 0);
      if (watchEach) for (const [i, s] of (u.watch ?? []).entries()) { const n = here.filter(p => p.work!.shift.from === s.from && p.work!.shift.to === s.to).length; if (n !== watchEach) bad.push(`${u.id} watch ${i} (${s.from}-${s.to}): ${n} on watch, the row asks ${watchEach}`); }
      if (here.length !== u.staffPerShift * u.shifts.length + watchN) bad.push(`${u.id}: ${here.length} staff in all, expected ${u.staffPerShift * u.shifts.length + watchN}`);
      const covered = (t: number) => u.shifts.some(s => inShift(s, t)), o = Math.round(u.hours[0] * 60), cl = Math.round(u.hours[1] * 60);
      for (let t = 0; t < 1440; t += 15) { const open = o < cl ? t >= o && t < cl : t >= o || t < cl; if (open && !covered(t)) bad.push(`${u.id}: no shift at ${Math.floor(t / 60)}:${String(t % 60).padStart(2, '0')} though it is open`); }
      if (u.kind !== 'civic' && positionsOf(rules, u.category).includes('keeper')) {
        const k = here.filter(p => p.work!.position === 'keeper').length;
        if (k !== 1) bad.push(`${u.id}: ${k} keepers, the rules say exactly one`);
      }
    }
    out.push({ id: 'P-4', rule: 'Staff counts match life.json per unit and shift; one keeper per unit', bad, value: `${scope.units.length} units`, limit: 'life.json staffPerUnit' });
  }

  // P-5 keep-clear zones. A seat is a chair at a table, and no zone bars tables (CD-1: planter, lamp, bollard, tree, parasol, band), so a chair is held by the zones that bar
  // tables, if any; a standing point is held by every zone.
  {
    const bad: string[] = [], pl = new Map(scope.places.map(p => [p.id, p]));
    for (const p of P) for (const s of p.routine.stops) {
      // a person in the official car at its bay (a 'commute' stop at a bay) is in a vehicle, not standing in the zone that keeps the bay clear of people
      const onShift = !!p.work && (s.doing === 'work' || (s.doing === 'meal' && s.place === p.work.place) || (s.doing === 'commute' && pl.get(s.place)?.kind === 'bay'));
      const here = pl.get(s.place), z = here ? scope.zones.find(x => x.test(here.at as V) && (here.kind !== 'seat' || x.bars.includes('table'))) : undefined;
      if (z && !onShift) bad.push(`${p.id}: ${s.doing} at ${s.place} stands in ${z.id}`);
    }
    out.push({ id: 'P-5', rule: 'No one stands in a CD-1 keep-clear zone off-shift', bad, value: `${scope.zones.length} zones`, limit: '0 off-shift stops inside' });
  }

  // P-6 the Handover casts its roles
  const handover = mission ? castHandover(P, scope, mission, plan, rules) : [];
  out.push({ id: 'P-6', rule: 'The Handover can cast every role it names (waitress, driver, guard, Vale\'s office), each with its need, approach and regularOf', bad: mission ? handover.filter(h => !h.ok).map(h => `${h.role}: ${h.why}`) : ['no mission to cast'], value: `${handover.filter(h => h.ok).length}/${handover.length} roles`, limit: 'all roles' });

  // P-7 ties
  {
    const bad: string[] = [], byId = new Map(P.map(p => [p.id, p])), T = rules.ties.kinds as Record<string, NonNullable<Rules['ties']['kinds'][keyof Rules['ties']['kinds']]> | undefined>, inc = new Map<string, number>();
    const stopsOf = (p: Person) => p.routine.stops.filter(s => s.doing !== 'home' && s.place !== 'beyond');
    for (const p of P) {
      if (p.ties.length < rules.ties.perPerson.min || p.ties.length > rules.ties.perPerson.max) bad.push(`${p.id}: ${p.ties.length} ties, rules ${rules.ties.perPerson.min}-${rules.ties.perPerson.max}`);
      const seen = new Set<string>();
      for (const t of p.ties) {
        const q = byId.get(t.with), spec = T[t.kind];
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
        if (spec.between === 'routine-meet' && !routineMeet(scope, p, q)) bad.push(`${p.id}: ${t.kind} partner ${q.id} is met on no routine: neither one's stops stand at the other's work while the other works`);
        if (spec.between === 'routine-stop' && p.work && q.work && p.work.employer === q.work.employer) bad.push(`${p.id}: regular of ${q.id}, who works for the same employer (${p.work.employer}): a regular is a customer of another unit, those are workmates`);
        if (spec.between === 'routine-stop' && !(q.work && stopsOf(p).some(s => (s.place === q.work!.place || (q.work!.unit === scope.cafeUnit && atCafeSide(scope, s.place, s.doing, ['pre-shift', 'evening']))) && (inShift(q.work!.shift, s.from) || inShift(q.work!.shift, Math.max(s.from, s.to - 1)))))) bad.push(`${p.id}: regular of ${q.id}, but no stop at their work while they are on shift`);
      }
      for (const k of Object.keys(T)) { const mp = T[k]?.maxPerPerson; if (mp !== undefined && p.ties.filter(t => t.kind === k).length > mp) bad.push(`${p.id}: more than ${mp} ${k} ties`); }
    }
    for (const [k, n] of inc) { const [kind] = k.split('/'); const m = (T[kind!] as { maxIncoming?: number } | undefined)?.maxIncoming; if (m !== undefined && n > m) bad.push(`${k}: ${n} incoming, limit ${m}`); }
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
    const byRole = pinnedByRole(P, mission ? { scope, mission, plan } : undefined, rules);
    for (const [i, pin] of n.pinned.entries()) {
      const hit = P.filter(p => p.work?.employer === pin.employer && (pin.position ? p.work.position === pin.position : p.work.role === pin.role) && p.name.split(' ')[pin.part === 'given' ? 0 : 1] === pin.name);
      if (hit.length !== 1) bad.push(`pinned ${pin.part} name ${pin.name}: ${hit.length} people carry it as ${pin.position ?? pin.role} of ${pin.employer}, expected 1`);
      const r = byRole[i]!;
      if (!r.who) bad.push(`pinned ${pin.part} name ${pin.name}: the Handover casts nobody in the role (${r.why})`);
      else if (hit.length === 1 && hit[0]!.id !== r.who) bad.push(`pinned ${pin.part} name ${pin.name} sits on ${hit[0]!.id}, but the Handover casts ${r.who} as ${r.role}`);
    }
    out.push({ id: 'P-8', rule: 'Names come from one district pool of mixed origins, with no era, unique, and the pinned names present once', bad: [...new Set(bad)], value: `${P.length} names`, limit: 'rules names' });
  }

  // P-9 shape, determinism, tiers. "Same tables, same people" is a second expansion of the cast, made here, and compared byte for byte with the people under test: a file that is
  // not the expansion of its cast fails it, and so does an expander whose two runs differ.
  {
    const bad: string[] = [], again = expand(c).people;
    const ps = PeopleSchema.safeParse(people);
    if (!ps.success) bad.push(`the people fail their schema: ${ps.error.issues.slice(0, 3).map(i => i.path.join('.') + ' ' + i.message).join(' | ')}`);
    const a = JSON.stringify(people), b = JSON.stringify(again);
    if (a !== b) {
      const other = new Map(again.persons.map(p => [p.id, JSON.stringify(p)])), diff = people.persons.filter(p => other.get(p.id) !== JSON.stringify(p)).map(p => p.id);
      bad.push(`two expansions of the same inputs differ${diff.length ? ` (${diff.slice(0, 4).join(', ')}${diff.length > 4 ? `, +${diff.length - 4} more` : ''})` : ' (not in the persons)'}`);
    }
    for (const p of P) if (p.memory.length) bad.push(`${p.id}: memory is not empty at the start`);
    for (const b of people.background) if (b.simulated > b.residents) bad.push(`${b.plot}: more simulated people than residents`);
    out.push({ id: 'P-9', rule: 'The expansion is valid, empty of memory, and byte-identical twice', bad, value: `${P.length} people`, limit: 'deterministic' });
  }

  // P-10 mix (P3)
  {
    const bad: string[] = [], mix = rules.temperamentsBy.mix;
    for (const r of cast.rows) {
      const total = r.temperaments.reduce((n, t) => n + t.weight, 0), by = new Map<string, number>();
      for (const t of r.temperaments) by.set(t.temperament, (by.get(t.temperament) ?? 0) + t.weight);
      for (const [t, w] of by) if (total > 0 && w / total > mix.rowMaxShare + 1e-9) bad.push(`${r.area}/${r.occupation}: ${t} is ${Math.round((100 * w) / total)}% of the row, limit ${Math.round(mix.rowMaxShare * 100)}%`);
    }
    let units = 0;
    for (const u of scope.units) {
      const here = P.filter(p => p.work?.unit === u.id);
      if (here.length < mix.whenStaffAtLeast) continue;
      units++;
      const kinds = new Set(here.map(p => p.temperament));
      if (kinds.size < mix.unitMinDistinct) bad.push(`${u.id}: ${here.length} staff but ${kinds.size} temperament(s) (${[...kinds].join(', ')}), rules ${mix.unitMinDistinct}`);
    }
    out.push({ id: 'P-10', rule: 'No temperament over the row cap; a unit of 3+ staff carries 2 or more temperaments', bad, value: `${cast.rows.length} rows, ${units} units`, limit: `row ≤ ${Math.round(mix.rowMaxShare * 100)}%, unit ≥ ${mix.unitMinDistinct} kinds` });
  }

  // P-11 knowledge at the start (P5)
  {
    const bad: string[] = [], k = rules.knowledge, kn = people.knowledge, want = deriveKnowledge(rules, P), byId = new Map(P.map(p => [p.id, p]));
    if (!kn) bad.push('the people carry no knowledge block');
    else {
      if (JSON.stringify(kn) !== JSON.stringify(want)) bad.push('the knowledge block is not the one derived from the rules and the ties');
      if (kn.cellAtStart !== k.cellAtStart || JSON.stringify(kn.identity) !== JSON.stringify(k.identity)) bad.push(`the cell must be known as ${k.identity.join('/')} and ${k.cellAtStart} else at the start`);
    }
    if (k.identity.length !== 1 || k.identity[0] !== 'face') bad.push(`the rules put ${k.identity.join('/')} in play, the cell is known by face only (Sky Q1)`);
    for (const e of want.needsKnown) for (const id of e.knows) if (!byId.has(id)) bad.push(`${e.keeper} would know the need of ${id}, who is not in the cast`);
    for (const p of P) if (p.memory.length) bad.push(`${p.id}: knows something at the start`);
    out.push({ id: 'P-11', rule: 'A keeper\'s known needs derive from ties; the cell is known by face only and nothing else at the start', bad, value: `${want.needsKnown.length} keepers know ${want.needsKnown.reduce((n, e) => n + e.knows.length, 0)} needs`, limit: 'derived; cell: face only' });
  }

  // P-12 every field a person carries names the rule that reads it (P2)
  {
    const bad: string[] = [], readers = Object.keys(rules.person.readBy.fields), carries = rules.tiers.simulated.carries;
    for (const f of carries) if (!readers.includes(f)) bad.push(`${f} is carried but no rule reads it`);
    for (const f of readers) if (!carries.includes(f)) bad.push(`${f} is read but not carried`);
    const shape = Object.keys(PersonShape).filter(f => f !== 'secret' || !!rules.secrets), seen = new Set<string>(); // a rulebook without `secrets` has no such field (a pinned older one)
    for (const f of shape) {
      const to = FIELD_READER[f];
      if (!to) bad.push(`field ${f} of a person has no named reader (add it to FIELD_READER with the rule that reads it)`);
      else if (!to.startsWith('~') && !readers.includes(to)) bad.push(`field ${f} is read by ${to}, which people.rules.json person.readBy does not list`);
    }
    for (const f of Object.keys(FIELD_READER)) if (!shape.includes(f) && !(f === 'secret' && !rules.secrets)) bad.push(`FIELD_READER names ${f}, which a person does not carry`);
    for (const p of P) for (const f of Object.keys(p)) if (!shape.includes(f) && !seen.has(f)) { seen.add(f); bad.push(`${p.id} carries ${f}, which the schema does not list`); }
    for (const b of people.background as Record<string, unknown>[]) for (const f of ['name', 'temperament', 'need', 'ties']) if (f in b) bad.push(`background ${b.plot} carries ${f}`);
    out.push({ id: 'P-12', rule: 'Every field a person carries names the rule that reads it', bad: [...new Set(bad)], value: `${shape.length} fields`, limit: 'each read by a rule' });
  }
  // P-13 seats: one holder at a time; every seat on a chair (or table) the world draws; every standing point clear
  {
    const bad: string[] = [], pl = new Map(scope.places.map(p => [p.id, p])), hm = (t: number) => `${Math.floor(t / 60)}:${String(t % 60).padStart(2, '0')}`;
    const held = new Map<string, { who: string; from: number; to: number }[]>();
    let stops = 0;
    for (const p of P) for (const s of p.routine.stops) {
      const k = pl.get(s.place)?.kind; if (k !== 'seat' && k !== 'stand') continue;
      stops++; held.set(s.place, [...(held.get(s.place) ?? []), { who: p.id, from: s.from, to: s.to }]);
    }
    for (const [place, list] of held) for (let i = 0; i < list.length; i++) for (let j = i + 1; j < list.length; j++) {
      const a = list[i]!, b = list[j]!;
      if (a.from < b.to && b.from < a.to) bad.push(`${a.who} and ${b.who} both hold ${place} (${hm(a.from)}-${hm(a.to)}, ${hm(b.from)}-${hm(b.to)})`);
    }
    // the seats the world gives: each chair of each set (a table the world draws no chairs on seats one, at the table), by id, within SEAT_TOL of where the world puts it
    const real = new Map<string, V>();
    for (const o of scope.sets) seatPoints(o).forEach((q, k) => real.set(seatId(o.id, k), q));
    for (const pa of scope.places.filter(x => x.kind === 'seat')) {
      const q = real.get(pa.id);
      if (!q) bad.push(`seat ${pa.id} is on no table or chair the world draws`);
      else if (Math.hypot(pa.at[0] - q[0], pa.at[1] - q[1]) > SEAT_TOL) bad.push(`seat ${pa.id} is at ${pa.at.join(', ')}, ${Math.hypot(pa.at[0] - q[0], pa.at[1] - q[1]).toFixed(2)} m from its chair (${q.join(', ')})`);
    }
    for (const id of real.keys()) if (!pl.has(id)) bad.push(`the chair ${id} the world draws is not a place`);
    const chairs = [...real.values()];
    for (const pa of scope.places.filter(x => x.kind === 'stand')) {
      const z = scope.zones.find(x => x.test(pa.at as V)), near = chairs.find(q => Math.hypot(pa.at[0] - q[0], pa.at[1] - q[1]) < STAND.chair);
      if (z) bad.push(`standing point ${pa.id} is in ${z.id}`);
      if (near) bad.push(`standing point ${pa.id} is on a chair (${near.join(', ')})`);
      if (!c.walk.free(pa.at as V)) bad.push(`standing point ${pa.id} is not walkable ground`);
    }
    out.push({ id: 'P-13', rule: 'No two people hold the same seat at once; every seat is a chair or table the world draws; every standing point keeps CD-1 clear', bad: [...new Set(bad)], value: `${stops} stops at ${held.size} seats and standing points`, limit: 'one holder at a time, every seat on a chair' });
  }
  // P-14 lovers (brief 011)
  {
    const bad: string[] = [], spec = rules.ties.kinds.lover, byId = new Map(P.map(p => [p.id, p])), mine = (p: Person) => p.ties.filter(t => t.kind === 'lover');
    const holders = P.filter(p => mine(p).length);
    if (!spec) { for (const p of holders) bad.push(`${p.id}: carries a lover tie, which the rules do not have`); for (const r of cast.rows) for (const t of r.ties) if (t.kind === 'lover') bad.push(`${r.area}/${r.occupation}: names a lover tie, which the rules do not have`); }
    else {
      const cap = Math.floor((spec.maxShare ?? 1) * P.length);
      if (holders.length > cap) bad.push(`${holders.length} of ${P.length} people hold a lover tie, the rules' share ${spec.maxShare} allows ${cap}`);
      for (const p of holders) {
        const t = mine(p), q = byId.get(t[0]!.with);
        if (t.length > spec.maxPerPerson) bad.push(`${p.id}: ${t.length} lovers, rules ${spec.maxPerPerson}`);
        if (!q) { bad.push(`${p.id}: lover ${t[0]!.with} is not in the cast`); continue; }
        if (!mine(q).some(x => x.with === p.id)) bad.push(`${p.id}: lover ${q.id} does not return it`);
        if (p.ties.some(x => x.kind === 'family' && x.with === q.id)) bad.push(`${p.id}: lover ${q.id} is family (a public partner)`);
        if (!routineMeet(scope, p, q)) bad.push(`${p.id}: lover ${q.id} is met on no routine`);
      }
      for (const r of cast.rows) for (const t of r.ties) if (t.kind === 'lover') {
        if (t.at && !(['pre-shift', 'evening'] as const).includes(t.at)) bad.push(`${r.area}/${r.occupation}: a lover tie at ${t.at}`);
      }
    }
    const pairs = new Set(holders.map(p => [p.id, mine(p)[0]!.with].sort().join('|'))).size;
    out.push({ id: 'P-14', rule: 'Lovers: mutual, one each, met on their routines, never family, at most the rules\' share of the people', bad: [...new Set(bad)], value: `${pairs} pairs, ${holders.length} of ${P.length} people`, limit: spec ? `≤ ${Math.round((spec.maxShare ?? 1) * 100)}% hold one` : 'no lover kind in the rules' });
  }
  // P-15 secrets (brief 011)
  {
    const bad: string[] = [], S = rules.secrets, list = S ? Object.keys(S.list) : [];
    if (!S) {
      for (const p of P) if ('secret' in p) bad.push(`${p.id}: carries a secret, which the rules do not have`);
      for (const r of cast.rows) if (r.secrets?.length) bad.push(`${r.area}/${r.occupation}: names secrets, which the rules do not have`);
    } else {
      for (const r of cast.rows) {
        const key = `${r.area}/${r.occupation}`, seen = new Set<string>();
        for (const x of r.secrets ?? []) {
          if (!list.includes(x.secret)) bad.push(`${key}: secret ${x.secret} is not in the rules`);
          else if (x.secret === 'affair') bad.push(`${key}: names the affair, which is never written in a table: everyone with a lover tie holds it`);
          else if ((S.list as Record<string, { requires: string }>)[x.secret]?.requires === 'lover-tie') bad.push(`${key}: ${x.secret} needs a lover tie, which a table share cannot give`);
          if (seen.has(x.secret)) bad.push(`${key}: secret ${x.secret} named twice`);
          seen.add(x.secret);
        }
        if (r.needs.some(n => n.need === 'secret')) bad.push(`${key}: weights the need secret, which the secrets replace (record share)`);
      }
      for (const p of P) {
        const lover = p.ties.some(t => t.kind === 'lover');
        if (p.secret === undefined) { bad.push(`${p.id}: carries no secret field (null for none)`); continue; }
        if (p.secret !== null && !list.includes(p.secret)) bad.push(`${p.id}: secret ${p.secret} is not in the rules`);
        if (lover && p.secret !== 'affair') bad.push(`${p.id}: has a lover tie but secret ${p.secret ?? 'none'}, not the affair`);
        if (!lover && p.secret === 'affair') bad.push(`${p.id}: holds the affair with no lover tie`);
        if (p.need === 'secret') bad.push(`${p.id}: need secret, which the secrets replace`);
        if (p.secret === 'record') { const row = p.work ? rowOf(cast, p.work.area, p.work.trade) : rowOf(cast, scope.homes.find(h => h.plot === p.home.plot)?.area ?? scope.areas[0]!, 'resident'); if (!row?.secrets?.some(x => x.secret === 'record')) bad.push(`${p.id}: holds a record, which its row does not name`); }
        if (p.secret === 'affair') { const q = P.find(x => x.id === p.ties.find(t => t.kind === 'lover')?.with); if (q && q.secret !== 'affair') bad.push(`${p.id}: the affair is not held by lover ${q.id}`); }
      }
    }
    out.push({ id: 'P-15', rule: 'Secrets: 0 or 1 per person from the rules\' list; the affair exactly for everyone with a lover tie; the need secret retired', bad: [...new Set(bad)], value: S ? `${P.filter(p => p.secret === 'affair').length} affairs, ${P.filter(p => p.secret === 'record').length} records` : 'none in the rules', limit: S ? 'one each, listed' : 'none' });
  }
  // P-16 one drawn person to a place (brief 014): the café's door was five servers on one point. A place holds one drawn person at a time; the minutes of a commute at a stop or bay are a
  // passer's (the car at the bay, the tram stop) and the seats and standing points are P-13's. A civic employer's door and bay are held to one by its row (`count`, `posts`) and by M-2's `alone`
  // and `waitsFor`, which read the role the mission needs; the rule here covers every other unit's places and the places no unit owns.
  {
    const bad: string[] = [], hm = (t: number) => `${Math.floor(t / 60)}:${String(t % 60).padStart(2, '0')}`, held = new Map<string, { who: string; from: number; to: number }[]>();
    const kind = new Map(scope.places.map(x => [x.id, x.kind]));
    for (const p of P) for (const s of p.routine.stops) {
      if (!drawn(p, s) || s.doing === 'commute' || kind.get(s.place) === 'seat' || kind.get(s.place) === 'stand' || (p.work && unitOf.get(p.work.unit)?.kind === 'civic' && (s.doing === 'work' || s.doing === 'meal'))) continue;
      held.set(s.place, [...(held.get(s.place) ?? []), { who: p.id, from: s.from, to: s.to }]);
    }
    for (const [place, list] of held) for (let i = 0; i < list.length; i++) for (let j = i + 1; j < list.length; j++) {
      const a = list[i]!, b = list[j]!;
      if (a.from < b.to && b.from < a.to) bad.push(`${a.who} and ${b.who} are both drawn at ${place} (${hm(a.from)}-${hm(a.to)}, ${hm(b.from)}-${hm(b.to)})`);
    }
    out.push({ id: 'P-16', rule: 'No two people are drawn on one place at once (a door, a bay, a stop holds one at a time)', bad: [...new Set(bad)], value: `${held.size} places`, limit: 'one drawn person to a place' });
  }
  const checks = out.map(row);
  void rowOf; void (null as unknown as Unit);
  return { ok: checks.every(x => x.ok), checks, counts: { persons: P.length, units: scope.units.length, faults: out.reduce((n, r) => n + r.bad.length, 0) }, handover };
}
