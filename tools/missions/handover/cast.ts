// The Handover's roles cast from the city's people (brief 010 D; GDD §07, §01 systems > scripts): the mission names roles
// (contracts/missions/handover/mission.json `roles`), never people. For each role, the people who fit it are those of its employer
// in its position or role who are there when the mission needs them (Sky 2026-10-15, brief 010 review): a role that stands at a
// mission point needs the person in place, their routine at their work place, that place standing at the point (a bay: its polygon
// holds the point; a door: within `near` m), for the role's whole window `shift`, on shift or not; a role without a point needs
// them on shift for the window. The pin is the first who fits by person id (prefix, then number), unless the mission already pins
// one, which must still fit. A person fills one role at most. Read by the overlay (rule M-2) and by the people checker (P-6,
// tools/people/handover.ts), so both place alike.
// With `rules` a role also carries what the person must be, and a fit must meet it: `need` (the person's one need), `approach`
// (the planned approach: the person's temperament must not refuse it, rules temperaments[t].refuses; the wrong temperament still
// works one step worse, so only a refusal fails) and `regularOf` (an employer: a regular tie to one of its staff; §05, §08).
// With `{ play: true }` as well (M-2 only, until brief 011's engine plays the mission), the static playability conditions:
// `tiedTo` (a tie of a kind to someone of another role's kind in place for it in this role's window, or, `cast`, to the
// person cast in it),
// `leavesTo` (after the window the routine's next stop stands at a point, begun by a mission time), `waitsFor` (still in place
// when another role's `leavesTo` brings its person there) and `alone` (no one else of the role in place in the window).
// Without `rules` (P-6's fallback) only the place counts; without `play` (P-6) the playability conditions are not read, so P-6
// and M-2 can pin different people: M-2's pin is the mission's. Pure: no file is read here.
import { dist, inPoly, type V } from '../../plan/geom';

export type Role = {
  id: string; what: string; employer: string; position?: string; role?: string;
  at?: string; near?: number; shift: { from: string; to: string }; pin?: string;
  need?: string; approach?: string; regularOf?: string;
  /** the secret the person must hold (people.rules.json `secrets`, Motive's lovers brief: `affair` comes with a lover tie) */
  secret?: string;
  /** play: a tie of `kind` held by the person to someone who fits `role` in this role's window, or (`cast`) to the one cast in it */
  tiedTo?: { kind: string; role: string; cast?: boolean }[];
  /** play: after the window, the routine's next stop (off the work place) stands at destination `at`, begun by mission time `by` */
  leavesTo?: { at: string; by: string };
  /** play: a role id with `leavesTo`; this person is still in place when that role's person arrives */
  waitsFor?: string;
  /** play: at most one person of this employer and position or role in place at the point at any moment of the window */
  alone?: boolean;
};
export type CastMission = { clock: { startsAt: string }; destinations: { name: string; at: [number, number] | number[] }[]; roles: Role[] };
/** Only what casting reads of a person (contracts/people/people.schema.ts Person). */
export type Castable = {
  id: string; name: string;
  work: { employer: string; position?: string; role?: string; place: string; shift: { from: number; to: number } } | null;
  routine: { stops: { place: string; doing: string; from: number; to: number }[] };
  /** read only when castRoles is given `rules`; a missing one fails the condition that reads it */
  temperament?: string; need?: string; ties?: { kind: string; with: string }[]; secret?: string | null;
};
/** What a role's conditions read of contracts/people/people.rules.json: each temperament's refusals, each need's approaches. */
export type CastRules = { temperaments: Record<string, { refuses: readonly string[] }>; needs: Record<string, { metBy: readonly string[] }>; secrets?: Record<string, unknown> };
export type Cast = { role: string; ok: boolean; who: string[]; pinned?: string; name?: string; why: string };
/** castRoles options. `play`: read the roles' playability conditions (tiedTo, leavesTo, waitsFor, alone); M-2 sets it. */
/** `summon`: the lover summons' limits from the rulebook (summon-walk-max-s), and the walk in seconds between two mission destinations
 *  (the mission's measured windows); a role with a lover tie is checked against them, and fails loudly without them. */
export type CastOptions = { play?: boolean; summon?: { maxWalkS?: number; walkS?: (from: string, to: string) => number | undefined } };
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
/** Day minutes as HH:MM:SS (seconds only when not whole). */
const hms = (t: number) => { const d = ((t % 1440) + 1440) % 1440, s = Math.round(d * 60), h = Math.floor(s / 3600), m = Math.floor(s / 60) % 60, r = s % 60; return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}${r ? `:${String(r).padStart(2, '0')}` : ''}`; };
type Stop = Castable['routine']['stops'][number];
/** The stops cover [t0, t1) without a gap, every one at `place` (a window past midnight is checked in two parts). */
export const inPlace = (stops: readonly Stop[], place: string, t0: number, t1: number): boolean => {
  if (t1 < t0) return inPlace(stops, place, t0, 1440) && inPlace(stops, place, 0, t1);
  let t = t0;
  for (let i = 0; i <= stops.length; i++) {
    const s = stops.find(x => x.place === place && x.from <= t && t < x.to);
    if (!s) return false;
    if (s.to >= t1) return true;
    t = s.to;
  }
  return false;
};
const onShift = (s: { from: number; to: number }, t0: number, t1: number) => inShift(s, t0) && (t1 <= t0 || inShift(s, t1 - 1e-6));

type Cond = { name: string; ok: (p: Castable) => boolean; has: (p: Castable) => string };
/** The role's conditions beyond its place, in the order they are reported. */
function conditions(r: Role, persons: Castable[], rules: CastRules): Cond[] {
  const out: Cond[] = [];
  if (r.need) out.push({ name: `need ${r.need}`, ok: p => p.need === r.need, has: p => `${p.id} ${p.need ?? 'no need'}` });
  if (r.approach) {
    const a = r.approach, t = (p: Castable) => (p.temperament ? rules.temperaments[p.temperament] : undefined);
    out.push({ name: `a temperament that does not refuse ${a}`, ok: p => !!t(p) && !t(p)!.refuses.includes(a), has: p => `${p.id} ${p.temperament ?? 'no temperament'}${t(p) ? '' : ' (unknown)'}` });
  }
  if (r.secret) out.push({ name: `secret ${r.secret}`, ok: p => p.secret === r.secret, has: p => `${p.id} ${p.secret ?? 'no secret'}` });
  if (r.regularOf) {
    const e = r.regularOf, ids = new Set(persons.filter(p => p.work?.employer === e).map(p => p.id));
    out.push({ name: `a regular tie to the staff of ${e}`, ok: p => hasRegularTie(p, ids), has: p => `${p.id} none` });
  }
  return out;
}
/** A role that cannot hold under the rules: an unknown need, or an approach that does not act on the need. */
function unsound(r: Role, rules: CastRules): string | undefined {
  if (r.need && !rules.needs[r.need]) return `need ${r.need} is not in the rules' needs`;
  if (r.secret && !rules.secrets) return `secret ${r.secret}: the people rules have no secrets list`;
  if (r.secret && !(r.secret in rules.secrets!)) return `secret ${r.secret} is not in the rules' secrets`;
  if (r.need && r.approach && !rules.needs[r.need]!.metBy.includes(r.approach)) return `${r.approach} does not act on need ${r.need} (metBy ${rules.needs[r.need]!.metBy.join(', ')})`;
  return undefined;
}

/** Cast every role. `rules` given (M-2, P-6): the role's need, approach and regularOf must hold too; absent: place only.
 *  `opts.play` (M-2): the roles' playability conditions too. */
export function castRoles(persons: Castable[], m: CastMission, where: Where, rules?: CastRules, opts: CastOptions = {}): Cast[] {
  const play = !!(rules && opts.play), t00 = minute(m.clock.startsAt), byRole = new Map(m.roles.map(r => [r.id, r]));
  const pointOf = (r: Role) => { const d = r.at ? m.destinations.find(x => x.name === r.at) : undefined; return d ? where.toPlan(d.at as V) : undefined; };
  /** The place stands at the point: a bay's polygon holds it, a door is within `near` m. */
  const standsAt = (place: string, point: V, near: number) => { const poly = where.polyOf(place), at = where.placeAt(place); return poly ? inPoly(point, poly) : !!at && dist(at, point) <= near; };
  const day = (at: string) => (t00 + offsetMin(at)) % 1440;
  /** Of the role's employer and position or role, there for window [t0, t1) (in place at the point, or on shift without one). */
  const fitsIn = (r: Role, t0: number, t1: number) => {
    const point = pointOf(r);
    return (p: Castable) => {
      const w = p.work; if (!w || w.employer !== r.employer) return false;
      if (r.position ? w.position !== r.position : w.role !== r.role) return false;
      if (!point) return onShift(w.shift, t0, t1);
      return standsAt(w.place, point, r.near ?? NEAR_M) && inPlace(p.routine.stops, w.place, t0, t1);
    };
  };
  /** leavesTo: the first stop after the window, off the work place, stands at the point and begins by `by` (minute stops: rounded up). */
  const arrival = (r: Role, p: Castable): Stop | undefined => {
    const lt = r.leavesTo, d = lt && m.destinations.find(x => x.name === lt.at); if (!lt || !d || !p.work) return undefined;
    const [, t1] = windowOf(m, r), next = p.routine.stops.filter(s => s.from >= Math.floor(t1) && s.place !== p.work!.place).sort((a, b) => a.from - b.from)[0];
    return next && next.from <= Math.ceil(day(lt.by)) && standsAt(next.place, where.toPlan(d.at as V), NEAR_M) ? next : undefined;
  };
  const nextAfter = (r: Role, p: Castable) => { const [, t1] = windowOf(m, r); const s = p.routine.stops.filter(x => x.from >= Math.floor(t1) && x.place !== p.work?.place).sort((a, b) => a.from - b.from)[0]; return s ? `${s.place}@${hms(s.from)}` : 'no stop'; };
  /** Per-person conditions a role's pool reads, other roles' ties apart. */
  const own = (r: Role): Cond[] => [
    ...(rules ? conditions(r, persons, rules) : []),
    ...(play && r.leavesTo ? [{ name: `a routine that reaches ${r.leavesTo.at} by ${r.leavesTo.by} (${hms(day(r.leavesTo.by))})`, ok: (p: Castable) => !!arrival(r, p), has: (p: Castable) => `${p.id} ${nextAfter(r, p)}` }] : []),
  ];
  /** Who fits role `r` in window [t0, t1) and meets its own conditions. */
  const poolOf = (r: Role, t0: number, t1: number) => own(r).reduce((ps, c) => ps.filter(c.ok), persons.filter(fitsIn(r, t0, t1)));
  const taken = new Set<string>(), pins = new Map<string, string>();
  /** Ties a later role's person must be the end of: role id → the earlier pick and the kind. */
  const owed = new Map<string, { from: Castable; fromRole: string; kind: string }[]>();
  const out = m.roles.map((r): Cast => {
    if (r.at && !pointOf(r)) return { role: r.id, ok: false, who: [], why: `the mission has no destination ${r.at}` };
    const bad = rules && unsound(r, rules);
    if (bad) return { role: r.id, ok: false, who: [], why: bad };
    const [t0, t1] = windowOf(m, r), place = r.at ? ` at ${r.at}` : '', win = `${r.shift.from}-${r.shift.to}`;
    const conds = [...own(r)];
    if (play) {
      for (const t of r.tiedTo ?? []) {
        const tr = byRole.get(t.role);
        if (!tr) return { role: r.id, ok: false, who: [], why: `tiedTo names no role ${t.role}` };
        const pinnedT = t.cast ? pins.get(t.role) : undefined;
        // not `cast`: anyone of the other role's kind in place for it in this window (Inez or her shift); `cast`: who can be cast in it
        const ends = new Set(pinnedT ? [pinnedT] : (t.cast ? poolOf(tr, ...windowOf(m, tr)) : persons.filter(fitsIn(tr, t0, t1))).map(p => p.id));
        const who = pinnedT ? `${t.role} ${pinnedT}` : t.cast ? `a ${kind(tr)} who can be cast as ${t.role}` : `a ${kind(tr)} in place for ${t.role} in this window`;
        conds.push({ name: `a ${t.kind} tie to ${who}`, ok: p => !!p.ties?.some(x => x.kind === t.kind && ends.has(x.with)), has: p => `${p.id} ${p.ties?.filter(x => x.kind === t.kind).map(x => x.with).join('+') || 'none'}` });
      }
      for (const o of owed.get(r.id) ?? []) conds.push({ name: `a ${o.kind} tie from ${o.fromRole} ${o.from.id}`, ok: p => !!o.from.ties?.some(x => x.kind === o.kind && x.with === p.id), has: p => `${p.id} not tied` });
    }
    const placed = persons.filter(fitsIn(r, t0, t1)).sort((a, b) => byId(a.id, b.id));
    if (r.pin && rules && !placed.some(p => p.id === r.pin)) return { role: r.id, ok: false, who: [], why: `pinned ${r.pin} is no longer a ${kind(r)} on shift ${win}${place}` };
    // narrow by each condition in turn; the first that leaves nobody is the one reported
    let pool = placed;
    for (const c of conds) {
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
    taken.add(pinned); pins.set(r.id, pinned);
    const me = persons.find(p => p.id === pinned)!;
    if (play) for (const t of r.tiedTo ?? []) if (t.cast && !pins.has(t.role)) owed.set(t.role, [...(owed.get(t.role) ?? []), { from: me, fromRole: r.id, kind: t.kind }]);
    const met = rules ? [r.need && `need ${r.need}`, r.approach && `${r.approach} not refused`, r.regularOf && `regular of ${r.regularOf}`, r.secret && `secret ${r.secret}`, ...(play ? [...(r.tiedTo ?? []).map(t => `${t.kind} tie to ${t.role}`), ...(owed.get(r.id) ?? []).map(o => `${o.kind} tie from ${o.fromRole}`), r.leavesTo && `reaches ${r.leavesTo.at}`] : [])].filter(Boolean) : [];
    return { role: r.id, ok: true, who, pinned, name: me.name, why: `${pinned} of ${who.length} on shift ${win}${place}${met.length ? `, ${met.join(', ')}` : ''}${r.pin ? ' (pinned in the mission)' : ' (first by id)'}` };
  });
  if (!play) return out;
  // the conditions on the cast as a whole: alone at the point, waits for another role's arrival
  return out.map((c, i): Cast => {
    const r = m.roles[i]!, [t0, t1] = windowOf(m, r), win = `${r.shift.from}-${r.shift.to}`, fail: string[] = [];
    if (r.alone && r.at) {
      const point = pointOf(r)!, same = persons.filter(p => p.work?.employer === r.employer && (r.position ? p.work.position === r.position : p.work?.role === r.role));
      const there = (t: number) => same.filter(p => p.routine.stops.some(s => s.from <= t && t < s.to && standsAt(s.place, point, r.near ?? NEAR_M))).map(p => p.id).sort(byId);
      const times = [t0, ...same.flatMap(p => p.routine.stops.flatMap(s => [s.from, s.to])).filter(t => t > t0 && t < t1)];
      const worst = times.map(t => ({ t, ids: there(t) })).reduce((a, b) => (b.ids.length > a.ids.length ? b : a), { t: t0, ids: [] as string[] });
      if (worst.ids.length > 1) fail.push(`at most one ${kind(r)} at ${r.at} in ${win}: ${worst.ids.length} at ${hms(worst.t)} (${worst.ids.join(', ')})`);
    }
    // the lover summons (Motive's lovers brief): the caller's call reaches the cast lover in his role's window, within the summons walk
    for (const t of play ? (r.tiedTo ?? []).filter(x => x.kind === 'lover') : []) {
      const tr = byRole.get(t.role), them = tr && pins.get(tr.id), me = c.pinned && persons.find(x => x.id === c.pinned);
      if (!opts.summon) { fail.push(`a lover summons to ${t.role}: no summons rule given (rulebook summon-walk-max-s, the mission's walks)`); continue; }
      if (opts.summon.maxWalkS === undefined) { fail.push(`a lover summons to ${t.role}: the rulebook has no summon-walk-max-s`); continue; }
      if (!tr || !them || !me) continue; // the cast itself already failed, and said why
      const w = !opts.summon.walkS ? null : r.at && tr.at ? opts.summon.walkS(tr.at, r.at) : undefined; // null: the caller checks the walk (the site casts before the engine can walk its grid)
      if (w === undefined) fail.push(`a lover summons to ${t.role}: no measured walk from ${tr.at ?? '?'} to ${r.at ?? '?'} (the engine's: dotnet and its probe)`);
      else if (w !== null && w > opts.summon.maxWalkS) fail.push(`a lover summons to ${t.role} ${them}: ${w} s from ${tr.at} to ${r.at}, over the summons' ${opts.summon.maxWalkS} s`);
      const [s0, s1] = windowOf(m, tr);
      if (me.work && !inPlace(me.routine.stops, me.work.place, s0, s1)) fail.push(`${c.pinned} is not at ${r.at ?? me.work.place} for all of ${t.role}'s window ${tr.shift.from}-${tr.shift.to}, so the call can't reach ${them}`);
    }
    if (r.waitsFor && c.pinned) {
      const tr = byRole.get(r.waitsFor), them = tr && pins.get(tr.id), p = persons.find(x => x.id === c.pinned)!;
      const a = tr && them ? arrival(tr, persons.find(x => x.id === them)!) : undefined;
      if (!tr?.leavesTo) fail.push(`waitsFor names no role with leavesTo: ${r.waitsFor}`);
      else if (!a) fail.push(`waits for ${r.waitsFor}, who is not cast to reach ${tr.leavesTo.at}`);
      else if (!p.work || !inPlace(p.routine.stops, p.work.place, t0, a.from + 1e-6)) {
        const gone = p.routine.stops.filter(s => s.place === p.work?.place && s.from <= t0).sort((x, y) => y.to - x.to)[0];
        fail.push(`waits at ${r.at ?? p.work?.place} for ${r.waitsFor} ${them}, who arrives ${hms(a.from)}: ${c.pinned} leaves ${gone ? hms(gone.to) : 'before the window'}`);
      }
    }
    return fail.length ? { ...c, ok: false, why: `${c.ok ? `${c.pinned} cast, but` : `${c.why} · and`} ${fail.join(' · and ')}` } : c;
  });
}
