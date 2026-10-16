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
import { fold, insideFromDoor, residentCycle, staffing, stopFeasible, variantMakes, workerCycle, workPlaceOf, type AimStop, type Cycle, type Post } from './day';
import { deriveKnowledge } from './knowledge';
import { withSecrets } from './secrets';
import { pinnedByRole, type Casting } from './roles';

export type Ctx = { scope: Scope; rules: Rules; cast: Cast; walk: Walker; planId: string; planVersion: string; atMin: number; hashes: People['inputs']; /** the Handover, to give pinned names by role */ casting?: Casting };
const KEEPER = 'keeper';

export const rowOf = (cast: Cast, area: string, occupation: string): Row | undefined => cast.rows.find(r => r.area === area && r.occupation === occupation);
/** The aimed tie patterns of a row (a tie that names a position or a stop: `with`, `at`) that the person `id` of `position` carries, by pattern index. Each pattern has its own coin,
 *  so the day (which stops the variant makes) and the ties (who is tied to whom) are decided from the same draws, in any order. */
export const aimedOf = (row: Row | undefined, id: string, position: string | undefined, post?: string) => (row?.ties ?? []).flatMap((t, i) =>
  (t.with || t.at) && (!t.positions || t.positions.includes(position ?? '')) && (!t.posts || (!!post && (t.posts as readonly string[]).includes(post))) && rng(`${id}/ties-carry/${i}`).f() < t.share ? [t] : []);

type CafeScope = { cafeUnit: string; cafeSide: string[] };
/** Does any of the stops `xs` stand at the work place of `y` (or the café's customers' side, for the café's staff) while `y` is on shift? The test a regular tie and a lover tie hold
 *  (P-7, P-14): a visit is a stop of the day at the other's work unit while they work. */
export const stopsAtWork = (scope: CafeScope, xs: readonly Stop[], y: Person, visits: readonly string[] = ['pre-shift', 'evening']) =>
  !!y.work && xs.some(s => (s.place === y.work!.place || (y.work!.unit === scope.cafeUnit && atCafeSide(scope, s.place, s.doing, visits))) && (inShift(y.work!.shift, s.from) || inShift(y.work!.shift, Math.max(s.from, s.to - 1))));
/** Two people meet on their routines when either one's stops (away from home) stand at the other's work while the other works (the lover tie, brief 011; workmates meet at work). */
export const routineMeet = (scope: CafeScope, a: Person, b: Person) => {
  const away = (p: Person) => p.routine.stops.filter(s => s.doing !== 'home' && s.place !== 'beyond');
  return stopsAtWork(scope, away(a), b) || stopsAtWork(scope, away(b), a);
};
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

export function expand(c: Ctx): { people: People; gaps: string[]; notes: string[] } {
  const { scope, rules } = c, cast = withSecrets(c.cast, rules), gaps: string[] = [], notes: string[] = [], persons: Person[] = [], names = new Set<string>();
  const homeWeights = scope.homes.map(h => ({ h, weight: h.residents }));

  const make = (id: string, row: Row, u: Unit | undefined, shift: { from: number; to: number } | undefined, position: string, post: Post, residentOf?: { plot: string; place: string }) => {
    const r = rng(id);
    // routine variant
    let variants = row.routines.filter(v => v.skeleton === (u ? 'worker' : 'resident') && (!v.positions || v.positions.includes(position)));
    // a tie that names a stop (`at`) is a promise about the day: a person who carries one is drawn only among the variants that make that stop, where the stop can be made (the
    // café is open then); the table need not know which variant is for whom. With none that do, any variant is drawn and the tie is reported if it cannot be made.
    if (u && shift) {
      const need = aimedOf(row, id, position, post).flatMap(t => (t.at && stopFeasible(scope, shift, t.at) ? [t.at as AimStop] : []));
      const fit = variants.filter(x => need.every(at => variantMakes(x, at)));
      if (need.length && fit.length) variants = fit;
    }
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
    } else cycle = residentCycle(c, r, home.place, v, id);
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
  // The people whose ties name a stop (a guard who is a regular after his shift) are seated first, so a busy square never takes the chair their tie stands on; then everyone by id.
  // A stop no place can take is not made (seats.ts): a meal is eaten on site, an evening or a stop before the shift is not made, and the loss is a note, not a fault.
  const holds = new Holds(), picker = seatPicker(scope.tables, scope.stands, holds, notes);
  const byId = new Map(persons.map(p => [p.id, p]));
  const seatOrder = [...persons.filter(p => aimedOf(rowOf(cast, p.work?.area ?? '', p.work?.trade ?? ''), p.id, p.position, p.work?.post).some(t => t.at)), ...persons.filter(p => !aimedOf(rowOf(cast, p.work?.area ?? '', p.work?.trade ?? ''), p.id, p.position, p.work?.post).some(t => t.at))];
  for (const p of seatOrder) byId.get(p.id)!.routine = { ...p.routine, stops: fold(cycles.get(p.id)!(picker(p.id))) };
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
  const T = rules.ties.kinds as Record<string, NonNullable<Rules['ties']['kinds'][keyof Rules['ties']['kinds']]> | undefined>, max = rules.ties.perPerson.max;
  const incoming = new Map<string, number>(), count = (p: Person, k: string) => p.ties.filter(t => t.kind === k).length;
  const linked = (a: Person, b: Person, k: string) => a.ties.some(t => t.kind === k && t.with === b.id) || b.ties.some(t => t.kind === k && t.with === a.id);
  const stops = (p: Person) => p.routine.stops.filter(s => s.doing !== 'home' && s.place !== 'beyond');
  const covers = (q: Person, place: string, s: Stop, visits?: readonly string[]) => !!q.work && (q.work.place === place || (q.work.unit === scope.cafeUnit && atCafeSide(scope, place, s.doing, visits))) && stops(q).some(x => x.doing === 'work' && x.place === q.work!.place) && (inShift(q.work.shift, s.from) || inShift(q.work.shift, Math.max(s.from, s.to - 1)));
  const tryTie = (a: Person, kind: string, r: Rng, o: { with?: string; at?: 'pre-shift' | 'evening' } = {}): boolean => {
    const spec = T[kind]; if (!spec) return false;
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
      case 'routine-meet': {
        // a lover is met on the routines (brief 011): one's away stops stand at the other's work while they work. The cast row's stop (Tie.at) is the carrier's own; the staff on
        // shift when they arrive come first. Never a family partner (a public one), and drawn first from the ties the generator made (a lover forms along a regular, then a workmate)
        const mine = o.at ? stops(a).filter(s => s.doing === o.at) : stops(a);
        pool = persons.filter(b => ok(b) && !linked(a, b, 'family') && (stopsAtWork(scope, mine, b) || (!o.at && stopsAtWork(scope, stops(b), a))));
        if (o.at) { const early = pool.filter(b => mine.some(s => inShift(b.work!.shift, s.from))); if (early.length) pool = early; }
        for (const k of spec.formsAlong ?? []) { const along = pool.filter(b => linked(a, b, k)); if (along.length) { pool = along; break; } }
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
    for (const t of aimedOf(row, a.id, positionOf(a), a.work?.post)) {
      if (t.kind === 'lover') continue; // lovers are made after the regular and workmate ties they form along (below)
      if (!(t.kind in T) || tryTie(a, t.kind, r, { with: t.with, at: t.at })) continue;
      // a tie the day could carry and nobody could be tied to is the table's to know (P-0), said plainly; a stop the day cannot make (the café is shut at 01:30) is no fault
      const mine = a.work ? stopFeasible(scope, a.work.shift, (t.at ?? 'pre-shift') as AimStop) : false, has = a.routine.stops.some(s => s.doing === (t.at ?? 'pre-shift'));
      if (t.at && mine && !has) gaps.push(`${a.id}: carries a ${t.kind} tie ${t.with ? `with a ${t.with} ` : ''}at ${t.at}, but its day (${a.routine.variant}) has no such stop (no variant of its row makes it, or no seat was free)`);
      else if (t.at && mine) gaps.push(`${a.id}: carries a ${t.kind} tie ${t.with ? `with a ${t.with} ` : ''}at ${t.at}, but no one ${t.with ? `of that position ` : ''}is on shift when it is there`);
    }
  }
  for (const a of persons) {
    const row = rowFor(a), r = rng(`${a.id}/ties`);
    for (const t of row?.ties ?? []) if (!aimed(t) && t.kind !== 'lover' && t.kind in T && (!t.positions || t.positions.includes(positionOf(a) ?? '')) && (!t.posts || (!!a.work && (t.posts as readonly string[]).includes(a.work.post))) && r.f() < t.share) tryTie(a, t.kind, r);
  }
  // lovers (brief 011): after the ties they form along. A pattern that names a person (the door guard's server) is made first, then the rest; never more than the rules' share of the
  // district's simulated people hold one (maxShare), and a pair that cannot be made is a gap only for a pattern that named a person (the cast's promise), never an ambient one.
  if (T.lover) {
    const cap = Math.floor((T.lover.maxShare ?? 1) * persons.length), holders = () => persons.reduce((n, p) => n + (count(p, 'lover') ? 1 : 0), 0);
    const want = (a: Person, pass: (t: Row['ties'][number]) => boolean) => (rowFor(a)?.ties ?? []).flatMap((t, i) => t.kind === 'lover' && pass(t) && (!t.positions || t.positions.includes(positionOf(a) ?? '')) &&
      (!t.posts || (!!a.work && (t.posts as readonly string[]).includes(a.work.post))) && rng(`${a.id}/ties-carry/${i}`).f() < t.share ? [t] : []);
    for (const named of [true, false]) {
      for (const a of persons) {
        const r = rng(`${a.id}/ties-lover${named ? '-named' : ''}`);
        for (const t of want(a, t => aimed(t) === named)) {
          if (count(a, 'lover')) continue;
          if (holders() + 2 > cap) { if (named) gaps.push(`${a.id}: carries a lover tie, but ${holders()} of ${persons.length} people already hold one (the rules' share ${T.lover.maxShare})`); continue; }
          if (tryTie(a, 'lover', r, { with: t.with, at: t.at })) continue;
          // the same rule as any aimed tie: a stop the day cannot make (the night watch at 01:30) is no fault; one it could make and nobody could be tied to is the table's to know
          const feasible = t.at ? !!a.work && stopFeasible(scope, a.work.shift, t.at as AimStop) : true;
          if (named && feasible) gaps.push(`${a.id}: carries a lover tie ${t.with ? `with a ${t.with} ` : ''}${t.at ? `at ${t.at}` : ''}, but ${t.at && !a.routine.stops.some(x => x.doing === t.at) ? `its day (${a.routine.variant}) has no such stop` : `no one ${t.with ? 'of that position ' : ''}whose work its routine meets could be tied to it`}`);
        }
      }
    }
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
    // the secret (brief 011): an affair for everyone with a lover tie; otherwise a record, by the share the row names (the old need `secret`'s weight, migrated); else none
    if (rules.secrets) { const rec = row?.secrets?.find(x => x.secret === 'record'); a.secret = count(a, 'lover') ? 'affair' : rec && rng(`${a.id}/secret`).f() < rec.share ? 'record' : null; }
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
  // ---- a door is one person's post: the rest of a shift's door staff work inside (day.ts insideFromDoor); their place stays the unit's door, only they are not drawn
  const inside = insideFromDoor(persons, new Map(scope.units.map(u => [u.id, u.kind])));
  for (const a of persons) if (inside.has(a.id)) a.work = { ...a.work!, post: 'inside' };
  const people: People = {
    schema: 'suborned.people/1', plan: { id: c.planId, version: c.planVersion }, scope: scope.id, inputs: c.hashes,
    places: scope.places, units: scope.units.map(u => ({ id: u.id, plot: u.plot, area: u.area, trade: u.trade, employer: u.employer, name: u.name, staffPerShift: u.staffPerShift, hours: u.hours, shifts: u.shifts })),
    persons, knowledge: deriveKnowledge(rules, persons), background,
  };
  return { people, gaps, notes };
}
