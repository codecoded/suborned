// The expander (brief 010 B): cast tables + plan + civic block + life.json + the people rules → individuals.
// Pure and seeded: every draw comes from rng(<plot>/<unit>/..) or rng(<person id>), never Math.random, so the same inputs give
// the same bytes. It follows the rules wherever it can (a temperament the occupation does not allow is not drawn, a need whose
// requirement the person does not meet is not drawn); the checker (check.ts) then holds the result, and the cast tables, to them.
import type { Cast, Row } from '../../contracts/people/cast.schema';
import type { People, Person, Stop } from '../../contracts/people/people.schema';
import type { V } from '../plan/geom';
import { rng, type Rng } from '../world/gen/common';
import { allowedTemperaments, positionsOf, type Rules } from './rules';
import { atCafeSide, HOME_SAMPLE, type Scope, type Unit } from './scope';
import { Holds, seatPicker, type Fit, type Pick, type Table } from './seats';
import type { Walker } from './walk';
import { fold, residentCycle, staffing, workerCycle, workPlaceOf, type Cycle, type Post } from './day';
import { deriveKnowledge } from './knowledge';
import { pinnedByRole, type Casting } from './roles';

export type Ctx = { scope: Scope; rules: Rules; cast: Cast; walk: Walker; planId: string; planVersion: string; atMin: number; hashes: People['inputs']; /** the Handover, to give pinned names by role */ casting?: Casting };
const KEEPER = 'keeper';

export const rowOf = (cast: Cast, area: string, occupation: string): Row | undefined => cast.rows.find(r => r.area === area && r.occupation === occupation);
export const inShift = (s: { from: number; to: number }, t: number) => (s.from <= s.to ? t >= s.from && t < s.to : t >= s.from || t < s.to);

const wt = <T>(r: Rng, xs: readonly T[], w: (t: T) => number) => r.weighted(xs, w);


/** What a person has, as a need's requirement reads it (rules needs.requiresKinds). The ties are what the person holds when the need is drawn; before ties are made, none. */
type Has = { home: boolean; work: boolean; family: boolean; rival: boolean };
/** Whether `need` can be this person's under the rules: its requirement holds, and an approach their temperament answers to acts on it (needs.list.metBy). */
export function needFits(rules: Rules, need: string, temperament: string, has: Has): boolean {
  const d = rules.needs.list[need], t = rules.temperaments[temperament as keyof Rules['temperaments']];
  if (!d || !t) return false;
  const req = d.requires;
  const reqOk = req === 'nothing' || (req === 'home' && has.home) || (req === 'work' && has.work) || (req === 'family-tie' && has.family) || (req === 'rival-tie' && has.rival);
  return reqOk && d.metBy.some(x => (t.respondsTo as readonly string[]).includes(x));
}
/** The needs of a row that a person in `position` may carry (the row's list, by position). Nothing outside it is a need of theirs. */
export const needsOf = (row: Row | undefined, position: string | undefined) => (row?.needs ?? []).filter(n => !n.positions || (position !== undefined && n.positions.includes(position)));

/** The row's offered needs for a position as integer weights per mille: largest remainder over the row's weights, ties by need id; they sum to 1000. The weights the rulebook
 *  draws a replacement need from (Sky Q2; keel-rulebook.md point 8). Empty when the row offers the position nothing (the expansion fails on that, P-0). */
export function needWeightsOf(row: Row | undefined, position: string | undefined): Record<string, number> {
  const by = new Map<string, number>();
  for (const n of needsOf(row, position)) by.set(n.need, (by.get(n.need) ?? 0) + n.weight);
  const total = [...by.values()].reduce((a, b) => a + b, 0), out: Record<string, number> = {};
  if (!total) return out;
  const share = [...by].map(([need, w]) => ({ need, exact: (1000 * w) / total })), floor = share.map(x => ({ ...x, w: Math.floor(x.exact) }));
  let left = 1000 - floor.reduce((n, x) => n + x.w, 0);
  for (const x of [...floor].sort((a, b) => (b.exact % 1) - (a.exact % 1) || (a.need < b.need ? -1 : 1))) { if (left <= 0) break; x.w++; left--; }
  for (const x of floor) if (x.w > 0) out[x.need] = x.w;
  return out;
}

export function expand(c: Ctx): { people: People; gaps: string[] } {
  const { scope, rules, cast } = c, gaps: string[] = [], persons: Person[] = [], names = new Set<string>();
  const homeWeights = scope.homes.map(h => ({ h, weight: h.residents }));

  const make = (id: string, row: Row, u: Unit | undefined, shift: { from: number; to: number } | undefined, position: string, post: Post, residentOf?: { plot: string; place: string }) => {
    const r = rng(id);
    // routine variant
    const variants = row.routines.filter(v => v.skeleton === (u ? 'worker' : 'resident') && (!v.positions || v.positions.includes(position)));
    const v = variants.length ? wt(r, variants, x => x.weight) : row.routines.find(x => x.skeleton === (u ? 'worker' : 'resident')) ?? row.routines[0]!;
    let home: { plot: string; place: string };
    if (residentOf) home = residentOf;
    else if ((v.commute === 'tram' && scope.stops.some(s => s.mode === 'tram')) || (v.commute === 'car' && u?.bay)) home = { plot: 'beyond', place: 'beyond' };
    else { const h = wt(r, homeWeights, x => x.weight).h; home = { plot: h.plot, place: h.place }; }
    let work: Person['work'] = null, cycle: Cycle | undefined;
    if (u && shift) {
      const spot = workPlaceOf(u, row, position, post);
      const civicRole = u.kind === 'civic';
      work = { unit: u.id, plot: u.plot, area: u.area, trade: u.trade, employer: u.employer, ...(civicRole ? { role: position } : { position }), post, place: spot, shift };
      cycle = workerCycle(c, r, u, shift, spot, home.place === 'beyond' ? scope.homes[0]!.place : home.place, v);
      if (home.place === 'beyond' && v.commute !== 'tram' && v.commute !== 'car') cycle = undefined;
    } else cycle = residentCycle(c, r, home.place, v);
    const allowKey = u ? (u.kind === 'civic' ? position : u.trade) : 'resident';
    const allow = (allowedTemperaments(rules, allowKey) ?? []).filter(t => t in rules.temperaments);
    const want = row.temperaments.filter(t => allow.includes(t.temperament as never));
    if (!want.length) gaps.push(`${id}: the row ${row.area}/${row.occupation} offers no temperament a ${allowKey} may be`); // unknown = reject (P-0); a valid one is still drawn so the expansion goes on
    let temperament = want.length ? wt(r, want, x => x.weight).temperament : r.pick(allow.length ? allow : Object.keys(rules.temperaments));
    // a temperament no need of the person's row can reach (a Fearful driver of a row that offers drivers cash alone) is drawn again from the row's own weights, among those
    // that can: nobody is left with a need the row never offers them. Ties are not made yet, so a need that wants a tie does not count here.
    const fits = (t: string) => needsOf(row, position).some(n => needFits(rules, n.need, t, { home: home.plot !== 'beyond', work: !!u, family: false, rival: false }));
    if (!fits(temperament)) { const ok = want.filter(t => fits(t.temperament)); if (ok.length) temperament = wt(r, ok, x => x.weight).temperament; }
    return { r, v, home, work, cycle, segs: cycle ? cycle() : [], temperament };
  };
  const cycles = new Map<string, Cycle>();

  // ---- workers
  for (const u of scope.units) {
    const row = rowOf(cast, u.area, u.trade);
    if (!row) { gaps.push(`no cast row for ${u.area} × ${u.trade} (unit ${u.id})`); continue; }
    const valid = (p: string) => (u.kind === 'civic' ? p in rules.temperamentsBy.roles && rules.temperamentsBy.roles[p]!.employer === u.employer : positionsOf(rules, u.category).includes(p));
    const pos = row.positions.filter(p => valid(p.position) && p.position !== KEEPER);
    const hasKeeper = u.kind !== 'civic' && positionsOf(rules, u.category).includes(KEEPER);
    let n = 0;
    for (const { shift, slots } of staffing(u, row, pos, hasKeeper, KEEPER, si => rng(`${u.plot}/${u.id}/shift${si}`), gaps)) {
      for (const { position, post } of slots) {
        n++;
        const id = `p-${u.id}-${String(n).padStart(2, '0')}`;
        const m = make(id, row, u, shift, position, post);
        if (!m.cycle || !m.segs.length) { gaps.push(`${id}: no routine`); continue; }
        cycles.set(id, m.cycle);
        persons.push({
          id, name: '', tier: 'simulated', home: { plot: m.home.plot, place: m.home.place }, work: m.work,
          routine: { variant: m.v.id, stops: fold(m.segs) }, temperament: m.temperament, need: '', institution: null,
          start: { ...rules.temperaments[m.temperament as keyof Rules['temperaments']]!.start }, memory: [], ties: [],
          look: { block: `job/${u.category}` }, position, needWeights: needWeightsOf(row, position),
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
      cycles.set(id, m.cycle!);
      persons.push({
        id, name: '', tier: 'simulated', home: m.home, work: null, routine: { variant: m.v.id, stops: fold(m.segs) },
        temperament: m.temperament, need: '', institution: null, start: { ...rules.temperaments[m.temperament as keyof Rules['temperaments']]!.start },
        memory: [], ties: [], look: { block: 'job/resident' }, position: 'resident', needWeights: needWeightsOf(row, 'resident'),
      });
    }
  }
  persons.sort((a, b) => (a.id < b.id ? -1 : 1));
  // ---- seats: the café's chairs and standing points are shared out by person id (the order just made). One person to a seat at a time while seats last, then the nearest
  // standing points; a person's own table (drawn above) and, for the guard's stop before the shift, the table nearest the door come first. The seat sets the walk, so the
  // times of the stops at it are made again for the seat given; everything else in the day is what it was.
  const holds = new Holds(), picker = seatPicker(scope.tables, scope.stands, holds, gaps);
  for (const p of persons) p.routine = { ...p.routine, stops: fold(cycles.get(p.id)!(picker(p.id))) };
  const rowFor = (p: Person) => (p.work ? rowOf(cast, p.work.area, p.work.trade) : rowOf(cast, scope.homes.find(h => h.plot === p.home.plot)?.area ?? scope.areas[0]!, 'resident'));
  const positionOf = (p: Person) => (p.work ? (p.work.position ?? p.work.role) : 'resident');
  /** `temperament` can carry one of the needs the person's row offers their position (ties not yet made: a need that wants a tie does not count). */
  const fitsNeed = (p: Person, temperament: string) => needsOf(rowFor(p), positionOf(p)).some(n => needFits(rules, n.need, temperament, { home: p.home.plot !== 'beyond', work: !!p.work, family: false, rival: false }));
  // ---- mix (P3): a unit with enough staff carries at least unitMinDistinct temperaments; repaired from the row's own weights
  const mix = rules.temperamentsBy.mix;
  for (const u of scope.units) {
    const here = persons.filter(p => p.work?.unit === u.id), row = rowOf(cast, u.area, u.trade);
    if (!row || here.length < mix.whenStaffAtLeast) continue;
    for (let i = 0; i < here.length; i++) {
      const have = new Set(here.map(p => p.temperament));
      if (have.size >= mix.unitMinDistinct) break;
      const options = [...row.temperaments].sort((a, b) => b.weight - a.weight || (a.temperament < b.temperament ? -1 : 1)).filter(t => !have.has(t.temperament) && t.temperament in rules.temperaments);
      const pick = [...here].reverse().flatMap(p => options.filter(t => (allowedTemperaments(rules, u.kind === 'civic' ? (p.work!.role ?? u.trade) : u.trade) ?? []).includes(t.temperament as never) && fitsNeed(p, t.temperament)).map(t => ({ p, t })))[0];
      if (!pick) break;
      pick.p.temperament = pick.t.temperament; pick.p.start = { ...rules.temperaments[pick.t.temperament as keyof Rules['temperaments']]!.start };
    }
  }
  // ---- ties (before needs: some needs require a tie)
  const T = rules.ties.kinds, max = rules.ties.perPerson.max;
  const incoming = new Map<string, number>(), count = (p: Person, k: string) => p.ties.filter(t => t.kind === k).length;
  const linked = (a: Person, b: Person, k: string) => a.ties.some(t => t.kind === k && t.with === b.id) || b.ties.some(t => t.kind === k && t.with === a.id);
  const stops = (p: Person) => p.routine.stops.filter(s => s.doing !== 'home' && s.place !== 'beyond');
  const covers = (q: Person, place: string, s: Stop, visits?: readonly string[]) => !!q.work && (q.work.place === place || (q.work.unit === scope.cafeUnit && atCafeSide(scope, place, s.doing, visits))) && stops(q).some(x => x.doing === 'work' && x.place === q.work!.place) && (inShift(q.work.shift, s.from) || inShift(q.work.shift, Math.max(s.from, s.to - 1)));
  const tryTie = (a: Person, kind: string, r: Rng, o: { with?: string; at?: 'pre-shift' | 'evening' } = {}): boolean => {
    const spec = T[kind as keyof typeof T]; if (!spec) return false;
    if (a.ties.length >= max || count(a, kind) >= spec.maxPerPerson) return false;
    const ok = (b: Person) => b.id !== a.id && !linked(a, b, kind) && (!o.with || positionOf(b) === o.with);
    let pool: Person[];
    switch (spec.between) {
      case 'same-employer': pool = persons.filter(b => ok(b) && !!a.work && b.work?.employer === a.work.employer); break;
      case 'same-trade-or-employer': pool = persons.filter(b => ok(b) && !!a.work && !!b.work && (b.work.trade === a.work.trade || b.work.employer === a.work.employer)); break;
      case 'routine-stop': {
        // a customer's own employer is not a place they are a regular of (those are workmates, and a meal at one's own door is not a visit); a pre-shift stop is aimed first
        // (§05 the guard's café stop; §08 the café keeper tips on the guard), then any other stop their day passes
        const away = stops(a).filter(s => s.doing !== 'work'), aim = away.filter(s => s.doing === 'pre-shift');
        const at = (xs: Stop[], visits?: readonly string[]) => persons.filter(b => ok(b) && !(a.work && b.work?.employer === a.work.employer) && xs.some(s => covers(b, s.place, s, visits)));
        if (o.at) {
          // the cast row names the stop (Tie.at): the staff on shift when the customer arrives are the ones they are a regular of; staff who come on during the visit are the fallback
          const mine = away.filter(s => s.doing === o.at);
          pool = at(mine, [o.at]);
          const early = pool.filter(b => mine.some(s => inShift(b.work!.shift, s.from)));
          if (early.length) pool = early;
        } else { pool = at(aim); if (!pool.length || !aim.length) pool = at(away); }
        break;
      }
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
  // the patterns that name a position or a stop (a driver's councillor, a guard's server after his shift) are made first, on their own stream, so a person they name is not
  // full before they are asked; the rest are what they always were
  const aimed = (t: { with?: string; at?: string }) => !!(t.with || t.at);
  for (const a of persons) {
    const row = rowFor(a), r = rng(`${a.id}/ties-aimed`);
    for (const t of row?.ties ?? []) if (aimed(t) && t.kind in T && (!t.positions || t.positions.includes(positionOf(a) ?? '')) && r.f() < t.share) tryTie(a, t.kind, r, { with: t.with, at: t.at });
  }
  for (const a of persons) {
    const row = rowFor(a), r = rng(`${a.id}/ties`);
    for (const t of row?.ties ?? []) if (!aimed(t) && t.kind in T && (!t.positions || t.positions.includes(positionOf(a) ?? '')) && r.f() < t.share) tryTie(a, t.kind, r);
  }
  for (const a of persons) { // everyone has at least the rules' minimum
    const r = rng(`${a.id}/ties-min`);
    for (const kind of ['workmate', 'regular', 'family', 'debtor']) { if (a.ties.length >= rules.ties.perPerson.min) break; tryTie(a, kind, r); }
    if (a.ties.length < rules.ties.perPerson.min) gaps.push(`${a.id}: no tie could be made`);
  }
  // ---- needs, institutions, names
  for (const a of persons) {
    const row = rowFor(a), r = rng(`${a.id}/need`), pos = positionOf(a);
    const has: Has = { home: a.home.plot !== 'beyond', work: !!a.work, family: a.ties.some(t => t.kind === 'family'), rival: a.ties.some(t => t.kind === 'rival') };
    const want = needsOf(row, pos).filter(n => needFits(rules, n.need, a.temperament, has));
    // unknown = reject: a person is given a need their row offers their position, or the expansion fails (P-0) naming them; no need is drawn from outside the row
    if (want.length) a.need = wt(r, want, x => x.weight).need;
    else gaps.push(`${a.id}: none of the needs its row offers a ${pos} (${needsOf(row, pos).map(n => n.need).join(', ') || 'none'}) can be met by an approach a ${a.temperament} answers to (${(rules.temperaments[a.temperament as keyof Rules['temperaments']]?.respondsTo ?? []).join('/')})`);
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
