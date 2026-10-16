// The day: how a person's routine is built (brief 010; Pulse's, split out of expand.ts by Aster's ruling 2026-10-15).
// A day is a `Cycle`: given the scope, the walk graph and the person's own rng stream, it makes the person's stops (work, meal, pre-shift, evening, errand, commute, home) as
// absolute segments, and `fold` puts them on the day clock. expand.ts calls it in two places only: make() (`cycle()`, the day with no seats shared out, to know the person has
// a routine) and the seat pass (`cycle(picker(id))`, the day made again for the seat given). Nothing in expand.ts reads what is inside a cycle.
// What is here: the shape of a day (workerCycle, residentCycle, fold), the staffing of a shift (quota), and, as the day vocabulary of contracts/people/cast.schema.ts grows,
// the posts, the watch and the car. The tie patterns (who is a regular of whom) stay in expand.ts; which stop a regular tie is aimed at is named by the cast row.
import type { Row } from '../../contracts/people/cast.schema';
import type { Person, Stop } from '../../contracts/people/people.schema';
import { dist, type V } from '../plan/geom';
import { rng, type Rng } from '../world/gen/common';
import type { Ctx } from './expand';
import type { Unit } from './scope';
import type { Fit, Pick, Table } from './seats';
import type { Walker } from './walk';

export const DAY = 1440, MEAL = 30;
type CafeScope = { units: Unit[]; cafeUnit: string };
/** Whether the café unit is open at day-clock minute `t` (a stop at its customers' side needs staff on shift). */
export function cafeOpenAt(scope: CafeScope, t: number): boolean {
  const hours = scope.units.find(x => x.id === scope.cafeUnit)?.hours;
  if (!hours) return true;
  const h = (((t % DAY) + DAY) % DAY) / 60, [o, cl] = hours;
  return o < cl ? h >= o && h < cl : h >= o || h < cl;
}
/** The stop of a day a regular tie can be aimed at (`Tie.at`), for the person's shift: a stop at the café's customers' side, which needs the café open then. */
export type AimStop = 'pre-shift' | 'evening';
/** Whether the day of a shift can have the café stop `at`: before the shift (as the walk allows, 20 min) or after it (5 min on), while the café is open. */
export const stopFeasible = (scope: CafeScope, shift: { from: number; to: number }, at: AimStop): boolean =>
  cafeOpenAt(scope, at === 'pre-shift' ? shift.from - 20 : (shift.to > shift.from ? shift.to : shift.to + DAY) + 5);
/** Whether routine variant `v` makes the café stop `at`: `preShift: cafe` before the shift, `evening: square` after it. */
export const variantMakes = (v: Row['routines'][number], at: AimStop): boolean => (at === 'pre-shift' ? v.preShift === 'cafe' : v.evening === 'square');

/** Where a position stands at work: the unit's public door, inside (off-screen), at its bay, or at its official (service) entrance. */
export type Post = 'door' | 'inside' | 'bay' | 'official';
/** Metres within which two places are one place: a hop between them costs no minute. The minute that `walk.min` adds to step off and on is for ground crossed; the official
 *  entrance and the official bay beside it are 2.5 m apart, and with the minute the councillor could not be at his car by 6:10 (17:30:40, brief 010 Handover fix). */
export const STEP_OFF = 3;
/** Whole minutes between two places for a routine and for P-1: no minute under STEP_OFF m (straight line), else the walk graph's. */
export const stepMin = (walk: Walker, a: V, b: V): number => (dist(a, b) <= STEP_OFF ? 0 : walk.min(a, b));

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


/** Where a person of `position` with `post` works: the bay, the official entrance, or the public door; an inside person is counted at the side their position names. */
export function workPlaceOf(u: Unit, row: Row, position: string, post: Post): string {
  const side = row.positions.find(p => p.position === position)?.side ?? 'public', official = u.official ?? u.door;
  return post === 'bay' && u.bay ? u.bay : post === 'official' || (post === 'inside' && side === 'official') ? official : u.door;
}

export type Slot = { position: string; post: Post };
export type ShiftPlan = { shift: { from: number; to: number }; si: number; watch: boolean; slots: Slot[] };
/** The people of every shift of a unit, in the order they are made: for each shift the keeper (first shift), the positions with an exact `count`, the rest by weight
 *  (largest remainder); then a civic employer's watch shifts, `watch.count` of each position that names one. The slots are shuffled by the shift's own stream, and a position's
 *  `posts` give its first people their posts in that order (the door guard, the service door), the rest keeping the position's own. `pos`: the row's positions the unit
 *  allows, bar the keeper. Without count, posts or watch this is what the expander always did. */
export function staffing(u: Unit, row: Row, pos: Row['positions'], hasKeeper: boolean, keeper: string, rngOf: (si: number) => Rng, gaps: string[]): ShiftPlan[] {
  const posOf = new Map(row.positions.map(p => [p.position, p]));
  const plans: { shift: { from: number; to: number }; si: number; watch: boolean }[] = u.shifts.map((shift, si) => ({ shift, si, watch: false }));
  if (u.kind === 'civic') (u.watch ?? []).forEach((shift, wi) => plans.push({ shift, si: 100 + wi, watch: true }));
  return plans.map(({ shift, si, watch }) => {
    const slots: string[] = [];
    if (hasKeeper && si === 0) slots.push(keeper);
    const here = watch ? pos.filter(p => p.watch) : pos, fixed = here.filter(p => (watch ? p.watch : p.count)), free = watch ? [] : here.filter(p => !p.count);
    for (const p of fixed) for (let k = 0; k < (watch ? p.watch!.count : p.count!); k++) slots.push(p.position);
    const total = watch ? slots.length : u.staffPerShift;
    const q = quota(total - slots.length, free.map(p => ({ key: p.position, weight: p.weight })));
    for (const p of free) for (let k = 0; k < (q.get(p.position) ?? 0); k++) slots.push(p.position);
    if (slots.length < total) gaps.push(`unit ${u.id} shift ${si}: no valid position to fill ${total - slots.length} of ${total} staff`);
    if (slots.length > total) gaps.push(`unit ${u.id} shift ${si}: exact counts ask for ${slots.length} of ${total} staff`);
    const seen = new Map<string, number>();
    return { shift, si, watch, slots: rngOf(si).shuffle(slots).map((position): Slot => {
      const pd = posOf.get(position), k = seen.get(position) ?? 0; seen.set(position, k + 1);
      let post: Post = pd?.post ?? 'inside', left = k;
      for (const e of pd?.posts ?? []) { if (left < e.count) { post = e.post; break; } left -= e.count; }
      return { position, post };
    }) };
  });
}

export type Seg = { place: string; doing: Stop['doing']; from: number; to: number };

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

/** A person's day as a function of how they are seated: every draw of the person's own stream is made when the cycle is made (in the order the day asks for them), so
 *  the stream is the same however seats are shared out; the cycle then builds its stops for a picker (`pick`), or, with none, at each stop's own table's first chair. */
export type Cycle = (pick?: Pick) => Seg[];

export const first = (t: Table, fit: (place: string) => Fit | undefined) => { const p = t.seats[0] ?? t.standing?.[0]; if (!p) return undefined; const f = fit(p); return f && { place: p, ...f }; };

export function workerCycle(c: Ctx, r: Rng, u: Unit, shift: { from: number; to: number }, work: string, home: string, v: Row['routines'][number]): Cycle {
  const { scope, walk } = c, at = (id: string): V => scope.places.find(p => p.id === id)!.at as V;
  const w = (a: string, b: string) => (a === b ? 0 : stepMin(walk, at(a), at(b)));
  const tramStop = scope.stops.find(s => s.mode === 'tram')?.place;
  const origin = v.commute === 'car' && u.bay ? u.bay : v.commute === 'tram' && tramStop ? tramStop : home;
  const a = shift.from, b = shift.to > shift.from ? shift.to : shift.to + DAY, len = b - a, m = r.int(v.leaveBeforeMin[0]!, v.leaveBeforeMin[1]!);
  const ms = a + Math.floor(len / 2) + r.int(-20, 20);
  let meal0 = v.meal, mealAt: Table | undefined;
  if (meal0 === 'square' && scope.tables.length) mealAt = r.pick(scope.tables); else if (meal0 === 'square') meal0 = 'on-site';
  const eveningAt = v.evening === 'square' && scope.tables.length ? r.pick(scope.tables) : undefined;
  // the guard's stop is aimed at the table nearest the café door, and only while the café is open (a night watch has no café to stop at)
  const pre = v.preShift === 'cafe' && cafeOpenAt(scope, a - 20) ? scope.tables[0] : undefined;
  return (pick = first) => {
    const segs: Seg[] = [];
    // a stop at the café before the shift: the seat's walk to work sets when it starts
    let arrive = a, preSeg: Seg | undefined, firstPlace = work;
    if (pre) {
      const s = pick(pre, p => { const t = w(p, work); return p === work ? undefined : { from: a - t - 15, to: a - t }; });
      if (s) { preSeg = { place: s.place, doing: 'pre-shift', from: s.from, to: s.to }; firstPlace = s.place; arrive = s.from; }
    }
    const leave = arrive - w(origin, firstPlace) - m;
    const tram = origin !== home;
    if (tram) segs.push({ place: origin, doing: 'commute', from: leave - 2, to: leave }); // off the sheet until 2 min before
    if (preSeg) segs.push(preSeg);
    // work, with a meal in the middle of a long shift
    let meal = meal0;
    if (meal !== 'none' && len >= 240) {
      if (meal === 'square') {
        const s = pick(mealAt!, p => { const q = w(work, p); return ms + MEAL + 2 * q + 5 > b ? undefined : { from: ms + q, to: ms + q + MEAL, q }; });
        if (s) segs.push({ place: work, doing: 'work', from: a, to: ms }, { place: s.place, doing: 'meal', from: s.from, to: s.to }, { place: work, doing: 'work', from: ms + MEAL + 2 * s.q!, to: b });
        else if (ms + MEAL + 5 > b) meal = 'none';
        else segs.push({ place: work, doing: 'work', from: a, to: ms }, { place: work, doing: 'meal', from: ms, to: ms + MEAL }, { place: work, doing: 'work', from: ms + MEAL, to: b }); // no free seat: lunch is eaten on site
      } else if (ms + MEAL + 5 > b) meal = 'none';
      else segs.push({ place: work, doing: 'work', from: a, to: ms }, { place: work, doing: 'meal', from: ms, to: ms + MEAL }, { place: work, doing: 'work', from: ms + MEAL, to: b });
    } else meal = 'none';
    if (meal === 'none') segs.push({ place: work, doing: 'work', from: a, to: b });
    // the evening, then home
    let t = b, here = work;
    if (eveningAt) {
      const s = pick(eveningAt, p => { const tq = w(work, p); return { from: b + tq, to: b + tq + 45 }; });
      if (s) { segs.push({ place: s.place, doing: 'evening', from: s.from, to: s.to }); t = s.to; here = s.place; }
    }
    const back = t + w(here, origin);
    if (tram) segs.push({ place: origin, doing: 'commute', from: back, to: back + 2 }, { place: 'beyond', doing: 'home', from: back + 2, to: leave - 2 + DAY });
    else segs.push({ place: home, doing: 'home', from: back, to: leave + DAY });
    return segs;
  };
}

/** How a resident uses the café (brief 014, Sky 2026-10-16: "customers come and go all day"). Starting values [S]. `perDay`: visits a day, [min, max]; `sit`: the share that sit for the
 *  food category's dwell (life.json pedestrians.category.food.dwell, ±25%) and not stand at the counter window for `takeawayMin`; `slack`: minutes kept clear of the resident's other
 *  outings, besides the two walks. A visit's hour is drawn by the food draw of the band it falls in (life.json), inside the café's opening hours, so the café fills at the peak and
 *  thins at night; the draws come from the resident's own stream `<id>/visits`, so no other draw of the person moves. */
export const VISITS = { perDay: [4, 7] as [number, number], sit: 0.6, takeawayMin: 6, slack: 4, step: 5 };
export type Visit = { kind: 'sit' | 'takeaway'; from: number; to: number; at: Table };
export function visitsOf(c: Ctx, id: string, home: string, busy: [number, number][]): Visit[] {
  const { scope, walk } = c, u = scope.units.find(x => x.id === scope.cafeUnit);
  if (!u || !scope.tables.length) return [];
  const vr = rng(`${id}/visits`), at = (p: string): V => scope.places.find(x => x.id === p)!.at as V;
  const walkMin = Math.max(...scope.tables.map(t => stepMin(walk, at(home), at(t.seats[0]!))), stepMin(walk, at(home), scope.counter.at));
  const margin = 2 * walkMin + VISITS.slack, [o, cl] = u.hours, open = o * 60, close = (cl <= o ? cl + 24 : cl) * 60;
  const drawOf = (t: number) => scope.food.bands.find(b => ((t / 60) % 24) >= b.from && ((t / 60) % 24) < b.to)?.draw ?? 0;
  const out: Visit[] = [], taken: [number, number][] = [...busy];
  const n = vr.int(VISITS.perDay[0], VISITS.perDay[1]);
  for (let k = 0; k < n; k++) {
    const sit = vr.f() < VISITS.sit, dur = sit ? Math.round(scope.food.dwellMin * (0.75 + 0.5 * vr.f())) : VISITS.takeawayMin, table = vr.pick(scope.tables);
    const starts: number[] = [];
    for (let t = open; t + dur <= Math.min(close, 1440); t += VISITS.step) starts.push(t);
    for (let tries = 0; tries < 8 && starts.length; tries++) {
      const t = vr.weighted(starts, x => drawOf(x) + 1e-6);
      if (taken.some(([a, b]) => t - margin < b && a < t + dur + margin)) continue;
      taken.push([t, t + dur]); out.push({ kind: sit ? 'sit' : 'takeaway', from: t, to: t + dur, at: sit ? table : scope.counter });
      break;
    }
  }
  return out.sort((a, b) => a.from - b.from);
}

export function residentCycle(c: Ctx, r: Rng, home: string, v: Row['routines'][number], id: string): Cycle {
  const { scope, walk } = c, at = (id: string): V => scope.places.find(p => p.id === id)!.at as V;
  const w = (a: string, b: string) => (a === b ? 0 : stepMin(walk, at(a), at(b)));
  const e = r.int(9 * 60 + 30, 11 * 60);
  const eveningAt = v.evening === 'square' && scope.tables.length ? r.pick(scope.tables) : undefined, v0 = eveningAt ? 19 * 60 + r.int(-30, 30) : 0;
  // the café visits (above), kept clear of the errand and the evening
  const visits = visitsOf(c, id, home, [[e, e + 15], ...(eveningAt ? [[v0, v0 + 60] as [number, number]] : [])]);
  type Out = { pref?: Table; place?: string; doing: Stop['doing']; from: number; to: number };
  return (pick = first) => {
    // the errand is at the tram stop, at one of its standing points (a queue, never one point); a scope with no tram stop sends the resident to a café seat
    const outs: Out[] = [{ pref: scope.stopTable ?? scope.tables[0], place: scope.stopTable || scope.tables[0] ? undefined : scope.cafe, doing: 'errand', from: e, to: e + 15 }];
    if (eveningAt) outs.push({ pref: eveningAt, doing: 'evening', from: v0, to: v0 + 60 });
    for (const x of visits) outs.push({ pref: x.at, doing: x.kind === 'sit' ? 'meal' : 'errand', from: x.from, to: x.to });
    outs.sort((a, b) => a.from - b.from);
    const segs: Seg[] = [];
    let leave: number | undefined, here = home, free = 0; // free: when the resident is back home from the last outing
    for (const o of outs) {
      let place = o.place;
      if (!place) { const s = pick(o.pref!, () => ({ from: o.from, to: o.to })); if (!s) continue; place = s.place; }
      if (leave === undefined) leave = o.from - w(home, place);
      else segs.push({ place: home, doing: 'home', from: free, to: o.from - w(home, place) });
      segs.push({ place, doing: o.doing, from: o.from, to: o.to });
      here = place; free = o.to + w(place, home);
    }
    segs.push({ place: home, doing: 'home', from: free, to: leave! + DAY });
    void here;
    return segs;
  };
}

/** A door is one person's post (brief 014, Sky 2026-10-16: "only 1 waitress outside at the café"). Of the people who work the same unit and shift at its door, one stands it and the
 *  rest work inside (off-screen, counted at the same place); the one who stands it is the one the street knows: most ties to people of other employers (a lover weighs most, then a regular,
 *  incoming regulars count too), then the lowest id. Returns the ids who go inside. Pure; run after ties, which it reads, so no draw moves. */
export function insideFromDoor(persons: Person[], unitKinds: Map<string, string>): Set<string> {
  const out = new Set<string>(), by = new Map<string, Person[]>();
  for (const p of persons) if (p.work?.post === 'door' && unitKinds.get(p.work.unit) === 'frontage') {
    const k = `${p.work.unit}/${p.work.shift.from}-${p.work.shift.to}`;
    by.set(k, [...(by.get(k) ?? []), p]);
  }
  const employer = new Map(persons.map(p => [p.id, p.work?.employer ?? '']));
  // a lover at another employer outweighs any number of customers (the street knows who she waits for); a regular's tie weighs three, any other outside tie one
  const w = (kind: string) => (kind === 'lover' ? 20 : kind === 'regular' ? 3 : 1);
  const score = (p: Person) => {
    let n = 0;
    for (const t of p.ties) if (employer.get(t.with) !== p.work!.employer) n += w(t.kind);
    for (const q of persons) if (q.id !== p.id && q.work?.employer !== p.work!.employer) for (const t of q.ties) if (t.with === p.id && !(t.kind in { lover: 1, family: 1, workmate: 1 })) n += w(t.kind);
    return n;
  };
  for (const group of by.values()) {
    if (group.length < 2) continue;
    const host = [...group].sort((a, b) => score(b) - score(a) || (a.id < b.id ? -1 : 1))[0]!;
    for (const p of group) if (p !== host) out.add(p.id);
  }
  return out;
}

/** Whether a person is drawn on the street during stop `s`: not at home, not beyond the sheet, and not working or eating inside (a person whose post is inside, at the unit's own
 *  door place). The reading tools/sim/people.ts gives its `out` flag; a stop of `commute` (the minutes at a stop or bay before the sheet) is a passer's, never a post. */
export const drawn = (p: Person, s: Stop): boolean => s.doing !== 'home' && s.place !== 'beyond' && !(p.work?.post === 'inside' && s.place === p.work.place && (s.doing === 'work' || s.doing === 'meal'));
