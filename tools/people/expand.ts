// The expander (brief 010 B): cast tables + plan + civic block + life.json + the people rules → individuals.
// Pure and seeded: every draw comes from rng(<plot>/<unit>/..) or rng(<person id>), never Math.random, so the same inputs give
// the same bytes. It follows the rules wherever it can (a temperament the occupation does not allow is not drawn, a need whose
// requirement the person does not meet is not drawn); the checker (check.ts) then holds the result, and the cast tables, to them.
import type { Cast, Row } from '../../contracts/people/cast.schema';
import type { People, Person, Stop } from '../../contracts/people/people.schema';
import type { V } from '../plan/geom';
import { rng, type Rng } from '../world/gen/common';
import { allowedTemperaments, positionsOf, type Rules } from './rules';
import { HOME_SAMPLE, type Scope, type Unit } from './scope';
import type { Walker } from './walk';
import { deriveKnowledge } from './knowledge';
import { pinnedByRole, type Casting } from './roles';

export type Ctx = { scope: Scope; rules: Rules; cast: Cast; walk: Walker; planId: string; planVersion: string; atMin: number; hashes: People['inputs']; /** the Handover, to give pinned names by role */ casting?: Casting };
const KEEPER = 'keeper', DAY = 1440, MEAL = 30;

export const rowOf = (cast: Cast, area: string, occupation: string): Row | undefined => cast.rows.find(r => r.area === area && r.occupation === occupation);
export const inShift = (s: { from: number; to: number }, t: number) => (s.from <= s.to ? t >= s.from && t < s.to : t >= s.from || t < s.to);

/** Largest-remainder quota of `n` slots over weighted positions; each position gets one first while n allows (the heaviest first). */
export function quota(n: number, items: { key: string; weight: number }[]): Map<string, number> {
  const out = new Map<string, number>(), by = [...items].sort((a, b) => b.weight - a.weight || (a.key < b.key ? -1 : 1));
  if (n <= 0 || !by.length) return out;
  let left = n;
  for (const it of by.slice(0, n)) { out.set(it.key, 1); left--; }
  const live = by.slice(0, n), tot = live.reduce((s, i) => s + i.weight, 0);
  const share = live.map(i => ({ key: i.key, exact: (left * i.weight) / tot }));
  for (const s of share) { const f = Math.floor(s.exact); out.set(s.key, out.get(s.key)! + f); left -= f; }
  for (const s of [...share].sort((a, b) => (b.exact % 1) - (a.exact % 1) || (a.key < b.key ? -1 : 1))) { if (left <= 0) break; out.set(s.key, out.get(s.key)! + 1); left--; }
  return out;
}

type Seg = { place: string; doing: Stop['doing']; from: number; to: number };

/** A cycle of absolute segments (one day long, from the first `from`) folded onto the day clock, split at midnight, sorted. */
export function fold(segs: Seg[]): Stop[] {
  const out: Stop[] = [];
  for (const s of segs) {
    const k = Math.floor(s.from / DAY) * DAY, f = s.from - k, t = s.to - k;
    if (t > DAY) { out.push({ place: s.place, doing: s.doing, from: f, to: DAY }, { place: s.place, doing: s.doing, from: 0, to: t - DAY }); }
    else out.push({ place: s.place, doing: s.doing, from: f, to: t });
  }
  return out.filter(s => s.to > s.from).sort((a, b) => a.from - b.from || a.to - b.to);
}

const wt = <T>(r: Rng, xs: readonly T[], w: (t: T) => number) => r.weighted(xs, w);

function workerCycle(c: Ctx, r: Rng, u: Unit, shift: { from: number; to: number }, work: string, home: string, v: Row['routines'][number]): Seg[] {
  const { scope, walk } = c, at = (id: string): V => scope.places.find(p => p.id === id)!.at as V;
  const w = (a: string, b: string) => (a === b ? 0 : walk.min(at(a), at(b)));
  const tramStop = scope.stops.find(s => s.mode === 'tram')?.place;
  const origin = v.commute === 'tram' && tramStop ? tramStop : home;
  const a = shift.from, b = shift.to > shift.from ? shift.to : shift.to + DAY, len = b - a, m = r.int(v.leaveBeforeMin[0]!, v.leaveBeforeMin[1]!);
  const segs: Seg[] = [], cafe = scope.cafe;
  let arrive = a; // when the person must stand at work
  let preSeg: Seg | undefined, first = work;
  if (v.preShift === 'cafe' && cafe !== work) { const t = w(cafe, work); preSeg = { place: cafe, doing: 'pre-shift', from: a - t - 15, to: a - t }; first = cafe; arrive = a - t - 15; }
  const leave = arrive - w(origin, first) - m;
  const tram = origin !== home;
  if (tram) segs.push({ place: origin, doing: 'commute', from: leave - 2, to: leave }); // off the sheet until 2 min before
  if (preSeg) segs.push(preSeg);
  // work, with a meal in the middle of a long shift
  let meal = v.meal;
  const ms = a + Math.floor(len / 2) + r.int(-20, 20);
  let seat: string | undefined;
  if (meal === 'square' && scope.seats.length) seat = r.pick(scope.seats); else if (meal === 'square') meal = 'on-site';
  if (meal !== 'none' && len >= 240) {
    const q = meal === 'square' ? w(work, seat!) : 0;
    if (ms + MEAL + 2 * q + 5 > b) meal = 'none';
    else if (meal === 'on-site') segs.push({ place: work, doing: 'work', from: a, to: ms }, { place: work, doing: 'meal', from: ms, to: ms + MEAL }, { place: work, doing: 'work', from: ms + MEAL, to: b });
    else segs.push({ place: work, doing: 'work', from: a, to: ms }, { place: seat!, doing: 'meal', from: ms + q, to: ms + q + MEAL }, { place: work, doing: 'work', from: ms + MEAL + 2 * q, to: b });
  } else meal = 'none';
  if (meal === 'none') segs.push({ place: work, doing: 'work', from: a, to: b });
  // the evening, then home
  let t = b, here = work;
  if (v.evening === 'square' && scope.seats.length) {
    const q = r.pick(scope.seats), tq = w(work, q);
    segs.push({ place: q, doing: 'evening', from: b + tq, to: b + tq + 45 }); t = b + tq + 45; here = q;
  }
  const back = t + w(here, origin);
  if (tram) segs.push({ place: origin, doing: 'commute', from: back, to: back + 2 }, { place: 'beyond', doing: 'home', from: back + 2, to: leave - 2 + DAY });
  else segs.push({ place: home, doing: 'home', from: back, to: leave + DAY });
  return segs;
}

function residentCycle(c: Ctx, r: Rng, home: string, v: Row['routines'][number]): Seg[] {
  const { scope, walk } = c, at = (id: string): V => scope.places.find(p => p.id === id)!.at as V;
  const w = (a: string, b: string) => (a === b ? 0 : walk.min(at(a), at(b)));
  const errand = scope.stops.find(s => s.mode === 'tram')?.place ?? scope.cafe, e = r.int(9 * 60 + 30, 11 * 60);
  const segs: Seg[] = [], te = w(home, errand);
  const leave = e - te;
  segs.push({ place: errand, doing: 'errand', from: e, to: e + 15 });
  let t = e + 15, here = errand;
  if (v.evening === 'square' && scope.seats.length) {
    const q = r.pick(scope.seats), v0 = 19 * 60 + r.int(-30, 30);
    segs.push({ place: home, doing: 'home', from: t + w(errand, home), to: v0 - w(home, q) });
    segs.push({ place: q, doing: 'evening', from: v0, to: v0 + 60 }); t = v0 + 60; here = q;
  }
  segs.push({ place: home, doing: 'home', from: t + w(here, home), to: leave + DAY });
  return segs;
}

export function expand(c: Ctx): { people: People; gaps: string[] } {
  const { scope, rules, cast } = c, gaps: string[] = [], persons: Person[] = [], names = new Set<string>();
  const homeWeights = scope.homes.map(h => ({ h, weight: h.residents }));

  const make = (id: string, row: Row, u: Unit | undefined, shift: { from: number; to: number } | undefined, position: string, post: 'door' | 'inside' | 'bay', residentOf?: { plot: string; place: string }) => {
    const r = rng(id);
    // routine variant
    const variants = row.routines.filter(v => v.skeleton === (u ? 'worker' : 'resident') && (!v.positions || v.positions.includes(position)));
    const v = variants.length ? wt(r, variants, x => x.weight) : row.routines.find(x => x.skeleton === (u ? 'worker' : 'resident')) ?? row.routines[0]!;
    let home: { plot: string; place: string };
    if (residentOf) home = residentOf;
    else if (v.commute === 'tram' && scope.stops.some(s => s.mode === 'tram')) home = { plot: 'beyond', place: 'beyond' };
    else { const h = wt(r, homeWeights, x => x.weight).h; home = { plot: h.plot, place: h.place }; }
    let work: Person['work'] = null, segs: Seg[];
    if (u && shift) {
      const spot = post === 'bay' && u.bay ? u.bay : u.door;
      const civicRole = u.kind === 'civic';
      work = { unit: u.id, plot: u.plot, area: u.area, trade: u.trade, employer: u.employer, ...(civicRole ? { role: position } : { position }), post, place: spot, shift };
      segs = workerCycle(c, r, u, shift, spot, home.place === 'beyond' ? scope.homes[0]!.place : home.place, v);
      if (home.place === 'beyond' && v.commute !== 'tram') segs = [];
    } else segs = residentCycle(c, r, home.place, v);
    const allowKey = u ? (u.kind === 'civic' ? position : u.trade) : 'resident';
    const allow = (allowedTemperaments(rules, allowKey) ?? []).filter(t => t in rules.temperaments);
    const want = row.temperaments.filter(t => allow.includes(t.temperament as never));
    const temperament = want.length ? wt(r, want, x => x.weight).temperament : r.pick(allow.length ? allow : Object.keys(rules.temperaments));
    return { r, v, home, work, segs, temperament };
  };

  // ---- workers
  for (const u of scope.units) {
    const row = rowOf(cast, u.area, u.trade);
    if (!row) { gaps.push(`no cast row for ${u.area} × ${u.trade} (unit ${u.id})`); continue; }
    const valid = (p: string) => (u.kind === 'civic' ? p in rules.temperamentsBy.roles && rules.temperamentsBy.roles[p]!.employer === u.employer : positionsOf(rules, u.category).includes(p));
    const pos = row.positions.filter(p => valid(p.position) && p.position !== KEEPER);
    const hasKeeper = u.kind !== 'civic' && positionsOf(rules, u.category).includes(KEEPER);
    const posOf = new Map(row.positions.map(p => [p.position, p]));
    let n = 0;
    for (const [si, shift] of u.shifts.entries()) {
      const rs = rng(`${u.plot}/${u.id}/shift${si}`);
      const slots: string[] = [];
      if (hasKeeper && si === 0) slots.push(KEEPER);
      const q = quota(u.staffPerShift - slots.length, pos.map(p => ({ key: p.position, weight: p.weight })));
      for (const p of pos) for (let k = 0; k < (q.get(p.position) ?? 0); k++) slots.push(p.position);
      if (slots.length < u.staffPerShift) { gaps.push(`unit ${u.id} shift ${si}: no valid position to fill ${u.staffPerShift - slots.length} of ${u.staffPerShift} staff`); }
      const order = rs.shuffle(slots);
      for (const position of order) {
        n++;
        const id = `p-${u.id}-${String(n).padStart(2, '0')}`, post = posOf.get(position)?.post ?? 'inside';
        const m = make(id, row, u, shift, position, post);
        if (!m.segs.length) { gaps.push(`${id}: no routine`); continue; }
        persons.push({
          id, name: '', tier: 'simulated', home: { plot: m.home.plot, place: m.home.place }, work: m.work,
          routine: { variant: m.v.id, stops: fold(m.segs) }, temperament: m.temperament, need: '', institution: null,
          start: { ...rules.temperaments[m.temperament as keyof Rules['temperaments']]!.start }, memory: [], ties: [],
          look: { block: `job/${u.category}` },
        });
      }
    }
  }
  // ---- residents (a sample of each home plot; the rest is background)
  const background: People['background'] = [];
  for (const h of scope.homes) {
    const row = rowOf(cast, h.area, 'resident');
    if (!row) { gaps.push(`no cast row for ${h.area} × resident`); continue; }
    const n = Math.min(h.residents, HOME_SAMPLE);
    background.push({ plot: h.plot, area: h.area, residents: h.residents, simulated: n });
    for (let k = 1; k <= n; k++) {
      const id = `p-${h.plot}-${String(k).padStart(2, '0')}`, m = make(id, row, undefined, undefined, 'resident', 'door', { plot: h.plot, place: h.place });
      persons.push({
        id, name: '', tier: 'simulated', home: m.home, work: null, routine: { variant: m.v.id, stops: fold(m.segs) },
        temperament: m.temperament, need: '', institution: null, start: { ...rules.temperaments[m.temperament as keyof Rules['temperaments']]!.start },
        memory: [], ties: [], look: { block: 'job/resident' },
      });
    }
  }
  persons.sort((a, b) => (a.id < b.id ? -1 : 1));
  // ---- mix (P3): a unit with enough staff carries at least unitMinDistinct temperaments; repaired from the row's own weights
  const mix = rules.temperamentsBy.mix;
  for (const u of scope.units) {
    const here = persons.filter(p => p.work?.unit === u.id), row = rowOf(cast, u.area, u.trade);
    if (!row || here.length < mix.whenStaffAtLeast) continue;
    for (let i = 0; i < here.length; i++) {
      const have = new Set(here.map(p => p.temperament));
      if (have.size >= mix.unitMinDistinct) break;
      const options = [...row.temperaments].sort((a, b) => b.weight - a.weight || (a.temperament < b.temperament ? -1 : 1)).filter(t => !have.has(t.temperament) && t.temperament in rules.temperaments);
      const pick = [...here].reverse().flatMap(p => options.filter(t => (allowedTemperaments(rules, u.kind === 'civic' ? (p.work!.role ?? u.trade) : u.trade) ?? []).includes(t.temperament as never)).map(t => ({ p, t })))[0];
      if (!pick) break;
      pick.p.temperament = pick.t.temperament; pick.p.start = { ...rules.temperaments[pick.t.temperament as keyof Rules['temperaments']]!.start };
    }
  }
  const rowFor = (p: Person) => (p.work ? rowOf(cast, p.work.area, p.work.trade) : rowOf(cast, scope.homes.find(h => h.plot === p.home.plot)?.area ?? scope.areas[0]!, 'resident'));

  // ---- ties (before needs: some needs require a tie)
  const T = rules.ties.kinds, max = rules.ties.perPerson.max;
  const incoming = new Map<string, number>(), count = (p: Person, k: string) => p.ties.filter(t => t.kind === k).length;
  const linked = (a: Person, b: Person, k: string) => a.ties.some(t => t.kind === k && t.with === b.id) || b.ties.some(t => t.kind === k && t.with === a.id);
  const stops = (p: Person) => p.routine.stops.filter(s => s.doing !== 'home' && s.place !== 'beyond');
  const covers = (q: Person, place: string, s: Stop) => !!q.work && (q.work.place === place || (q.work.unit === scope.cafeUnit && place === scope.cafe)) && stops(q).some(x => x.doing === 'work' && x.place === q.work!.place) && (inShift(q.work.shift, s.from) || inShift(q.work.shift, Math.max(s.from, s.to - 1)));
  const tryTie = (a: Person, kind: string, r: Rng): boolean => {
    const spec = T[kind as keyof typeof T]; if (!spec) return false;
    if (a.ties.length >= max || count(a, kind) >= spec.maxPerPerson) return false;
    const ok = (b: Person) => b.id !== a.id && !linked(a, b, kind);
    let pool: Person[];
    switch (spec.between) {
      case 'same-employer': pool = persons.filter(b => ok(b) && !!a.work && b.work?.employer === a.work.employer); break;
      case 'same-trade-or-employer': pool = persons.filter(b => ok(b) && !!a.work && !!b.work && (b.work.trade === a.work.trade || b.work.employer === a.work.employer)); break;
      case 'routine-stop': pool = persons.filter(b => ok(b) && stops(a).some(s => covers(b, s.place, s))); break;
      default: pool = kind === 'family' ? persons.filter(b => ok(b) && b.home.plot === a.home.plot && b.home.plot !== 'beyond') : persons.filter(ok);
    }
    if (spec.direction === 'both') pool = pool.filter(b => b.ties.length < max && count(b, kind) < spec.maxPerPerson);
    else pool = pool.filter(b => (incoming.get(`${kind}/${b.id}`) ?? 0) < (spec.maxIncoming ?? 99));
    if (!pool.length) return false;
    const b = r.pick(pool);
    a.ties.push({ kind, with: b.id });
    if (spec.direction === 'both') b.ties.push({ kind, with: a.id }); else incoming.set(`${kind}/${b.id}`, (incoming.get(`${kind}/${b.id}`) ?? 0) + 1);
    return true;
  };
  for (const a of persons) {
    const row = rowFor(a), r = rng(`${a.id}/ties`);
    for (const t of row?.ties ?? []) if (t.kind in T && r.f() < t.share) tryTie(a, t.kind, r);
  }
  for (const a of persons) { // everyone has at least the rules' minimum
    const r = rng(`${a.id}/ties-min`);
    for (const kind of ['workmate', 'regular', 'family', 'debtor']) { if (a.ties.length >= rules.ties.perPerson.min) break; tryTie(a, kind, r); }
    if (a.ties.length < rules.ties.perPerson.min) gaps.push(`${a.id}: no tie could be made`);
  }
  // ---- needs, institutions, names
  for (const a of persons) {
    const row = rowFor(a), r = rng(`${a.id}/need`), resp = rules.temperaments[a.temperament as keyof Rules['temperaments']]!.respondsTo as readonly string[];
    const meets = (n: string) => {
      const d = rules.needs.list[n]; if (!d) return false;
      const req = d.requires;
      const reqOk = req === 'nothing' || (req === 'home' && a.home.plot !== 'beyond') || (req === 'work' && !!a.work) || (req === 'family-tie' && a.ties.some(t => t.kind === 'family')) || (req === 'rival-tie' && a.ties.some(t => t.kind === 'rival'));
      return reqOk && d.metBy.some(x => resp.includes(x));
    };
    const pos = a.work ? (a.work.position ?? a.work.role) : 'resident';
    const want = (row?.needs ?? []).filter(n => (!n.positions || (pos !== undefined && n.positions.includes(pos))) && meets(n.need));
    a.need = want.length ? wt(r, want, x => x.weight).need : r.pick(Object.keys(rules.needs.list).filter(meets));
    a.institution = a.temperament === 'loyal' ? (a.work ? a.work.employer : 'civic') : null;
  }
  const pick = (r: Rng, xs: { name: string }[]) => r.pick(xs).name;
  const pins = rules.names.pinned;
  const pinned = new Map<string, { given?: string; family?: string }>();
  for (const [i, hit] of pinnedByRole(persons, c.casting, rules).entries()) {
    const pin = pins[i]!;
    if (hit.who) pinned.set(hit.who, { ...pinned.get(hit.who), [pin.part]: pin.name });
    else gaps.push(`pinned name ${pin.name}: the Handover casts nobody as ${pin.position ?? pin.role} of ${pin.employer} (${hit.why})`);
  }
  for (const a of persons) {
    const r = rng(`${a.id}/name`), pin = pinned.get(a.id);
    for (let tries = 0; ; tries++) {
      const full = `${pin?.given ?? pick(r, cast.names.given)} ${pin?.family ?? pick(r, cast.names.family)}`;
      if (!names.has(full) || tries > 200) { names.add(full); a.name = full; break; }
    }
  }
  const people: People = {
    schema: 'suborned.people/1', plan: { id: c.planId, version: c.planVersion }, scope: scope.id, inputs: c.hashes,
    places: scope.places, units: scope.units.map(u => ({ id: u.id, plot: u.plot, area: u.area, trade: u.trade, employer: u.employer, name: u.name, staffPerShift: u.staffPerShift, hours: u.hours, shifts: u.shifts })),
    persons, knowledge: deriveKnowledge(rules, persons), background,
  };
  return { people, gaps };
}
