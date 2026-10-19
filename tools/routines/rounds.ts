// Rounds and partner meetings (Pulse, brief 022; Sky 2026-10-18: "have the waitress also walk around the cafe tables, and when guard comes, they stand together").
// Reads the people the engine reads (sim/data/handover.people.json), the site's walk grid and cast (sim/data/handover.site.json), the civic block's tables and
// contracts/people/routines.rules.json, and bakes:
//   servers  : for each person whose job and stop make a round (a server at a door with tables beside it), the excursions she makes on the round clock: which table,
//              where she stands, when she leaves, the seconds out, serving and back. Occupancy (who sits where) comes from the walkers' seat stops, so she goes where people are.
//   meetings : each pair of partners (tie kind `lover`) whose routines bring them within noticeM on the street: the excursion each makes to stand together.
//   vectors  : the reference pose of each server at fixed ticks, free, held, called and ended, which Keel's engine must reproduce (positions within 30 cm).
// Same inputs, same bytes. The engine does not read the file yet (candidate: briefs/022/handover.rounds.json); nothing here moves a recorded run.
// Run: bun run rounds [--out <file>] [--check]   (default out briefs/022/handover.rounds.json; --check exits 1 if the file is not what the inputs make)
import { createHash } from 'node:crypto';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { rng } from '../world/gen/common';
import { Grid, Path, d2, dist, leg, type Pt } from './walk';
import { RULES_FILE, parseRules, readRulesText, type RoutinesRules } from './rules';

const root = join(import.meta.dir, '..', '..');
export const SITE = 'sim/data/handover.site.json', PEOPLE = 'sim/data/handover.people.json', BLOCK = 'contracts/world/civic-block.json';
export const ROUNDS_CANDIDATE = 'briefs/022/handover.rounds.json', ROUNDS_ENGINE = 'sim/data/handover.rounds.json';
export const Hz = 10;

// ---------------------------------------------------------------- inputs

type PeopleStop = [string, number, number, number];
export type SimPerson = { id: string; name: string; job: string; ties: [string, string][]; stops: PeopleStop[] };
export type PeopleIn = { startsAt: number; places: Record<string, Pt>; people: SimPerson[] };
export type SiteIn = { grid: ConstructorParameters<typeof Grid>[0]; cast: Record<string, { id: string }>; places: Record<string, { at: Pt }>; guard: { post: string } };
export type Inputs = { rules: RoutinesRules; rulesText: string; people: PeopleIn; site: SiteIn; tables: Record<string, Pt>; siteText: string; peopleText: string; blockText: string };

export function readInputs(): Inputs {
  const read = (f: string) => readFileSync(join(root, f), 'utf8');
  const rulesText = readRulesText(), siteText = read(SITE), peopleText = read(PEOPLE), blockText = read(BLOCK);
  const block = JSON.parse(blockText) as { objects: { id: string; polygon: number[][] }[] };
  const tables: Record<string, Pt> = {};
  for (const o of block.objects) if (/^cafe-table-\d+$/.test(o.id)) {
    const n = o.polygon.length;
    tables[o.id] = [Math.round((o.polygon.reduce((a, p) => a + p[0]!, 0) / n) * 100), Math.round((o.polygon.reduce((a, p) => a + p[1]!, 0) / n) * 100)];
  }
  return { rules: parseRules(rulesText), rulesText, people: JSON.parse(peopleText), site: JSON.parse(siteText), tables, siteText, peopleText, blockText };
}

// ---------------------------------------------------------------- the baked shapes

export type Excursion = {
  table: string; at: Pt; face: Pt;
  start: number; out: number; serve: number; back: number; // round-clock seconds; legs in whole seconds
  m: number; path: Pt[]; // the walk, post to service point: its length in cm and its polyline
};
export type Span = { place: string; from: number; to: number; origin: number; excursions: Excursion[] };
export type Server = { name: string; post: string; at: Pt; face: Pt; spans: Span[] };
export type Meeting = {
  a: string; b: string; start: number; // mission seconds: both routines stop here
  atA: Pt; atB: Pt; faceA: Pt; faceB: Pt; outA: number; outB: number; stay: number; backA: number; backB: number;
};
/** One place the summoned partner may stand: on the ring of closeCm round her post, on free ground the guard can walk to; `deg` is its angle off her facing (positive = her left). */
export type Stop = { at: Pt; face: Pt; deg: number; walkCm: number };
export type Vec = [number, number, number, number, number, string]; // tick, x, y, facing x, facing y, phase
export type Rounds = {
  schema: 'suborned.sim.rounds/1'; from: string[]; frame: string;
  rules: { file: string; sha256: string; paceCmPerS: number; lingerS: number; closeCm: number; clearCm: number; partS: number; settleS: number; stayS: number; chanceStayS: number };
  servers: Record<string, Server>; meetings: Meeting[];
  guard: { from: string; post: string; walkCm: number; ringCm: number; stopAt: Pt; face: Pt; stops: Stop[] }; // where the summoned lover stops: ringCm from the post, at her side; stopAt/face = stops[0], the stop when nobody is near
  vectors: Record<string, Record<string, Vec[]>>;
};

const sha = (s: string) => createHash('sha256').update(s).digest('hex');
const unit = (v: Pt): Pt => { const l = Math.hypot(v[0], v[1]) || 1; return [Math.round((v[0] / l) * 100), Math.round((v[1] / l) * 100)]; };
const secs = (cm: number, pace: number) => Math.max(1, Math.ceil(cm / pace - 1e-9));

// ---------------------------------------------------------------- service points

type Service = { table: string; at: Pt; face: Pt; path: Path; m: number; out: number };

/** Where a server stands at a table: the first ring on which a gap between two chairs is free ground on the walk grid and clear of every chair, the gap that is the shortest walk from her post. */
export function servicePoints(g: Grid, post: Pt, tables: Record<string, Pt>, seats: Record<string, Pt>, r: RoutinesRules['rounds'], prefix: string): Service[] {
  const out: Service[] = [];
  for (const id of Object.keys(tables).filter((t) => t.startsWith(prefix)).sort((a, b) => Number(a.slice(prefix.length)) - Number(b.slice(prefix.length)))) {
    const c = tables[id]!, chairs = Object.keys(seats).filter((p) => p.startsWith(id + '-seat-')).map((p) => seats[p]!);
    const ang = chairs.map((s) => Math.atan2(s[1] - c[1], s[0] - c[0])).sort((a, b) => a - b);
    const mids = ang.map((a, i) => { const b = i + 1 < ang.length ? ang[i + 1]! : ang[0]! + 2 * Math.PI; return (a + b) / 2; });
    let best: Service | undefined;
    for (const ring of r.servicePoint.ringCm) {
      for (const th of mids) {
        const p: Pt = [Math.round(c[0] + ring * Math.cos(th)), Math.round(c[1] + ring * Math.sin(th))];
        if (!g.isFree(p) || chairs.some((s) => d2(s, p) < r.servicePoint.clearOfSeatCm ** 2)) continue;
        const path = leg(g, post, p), m = Math.round(path.length / 10);
        if (!best || m < best.m) best = { table: id, at: p, face: [c[0] - p[0], c[1] - p[1]], path, m, out: secs(m, r.pace.cmPerS) };
      }
      if (best) break;
    }
    if (best && best.m <= r.leashM.v * 100) out.push(best);
  }
  return out;
}

// ---------------------------------------------------------------- the round

type Interval = [number, number];
const merge = (xs: Interval[]): Interval[] => {
  const s = [...xs].sort((a, b) => a[0] - b[0] || a[1] - b[1]), o: Interval[] = [];
  for (const x of s) { const l = o[o.length - 1]; if (l && x[0] <= l[1]) l[1] = Math.max(l[1], x[1]); else o.push([x[0], x[1]]); }
  return o;
};

/** Who sits at which table when: the drawn walkers' seat stops (the four cast roles are played by the engine by role, not by routine, so they are not counted). */
export function occupancy(people: PeopleIn, cast: Set<string>, prefix: string): Record<string, Interval[]> {
  const by: Record<string, Interval[]> = {};
  const re = new RegExp(`^(${prefix}\\d+)-seat-\\d+$`);
  for (const p of people.people) {
    if (cast.has(p.id)) continue;
    for (const [pl, from, to, out] of p.stops) { const m = re.exec(pl); if (m && out === 1) (by[m[1]!] ??= []).push([from, to]); }
  }
  for (const k of Object.keys(by)) by[k] = merge(by[k]!);
  return by;
}
const seated = (iv: Interval[] | undefined, t: number) => !!iv?.some(([a, b]) => t >= a && t < b);
const partyStart = (iv: Interval[] | undefined, t: number) => iv?.find(([a, b]) => t >= a && t < b)?.[0];

/** The excursions of one stop on the round clock (seconds from `origin`), seeded by the person's id. */
export function bakeSpan(id: string, spanIx: number, post: Pt, origin: number, svc: Service[], occ: Record<string, Interval[]>, r: RoutinesRules['rounds']): Excursion[] {
  const dr = rng(`${id}/rounds/${spanIx}`), draw = (lo: number, hi: number) => dr.int(lo, hi);
  const settle = r.opening.settleS, greet = r.demand.greetS, [rv0, rv1] = r.demand.revisitS;
  const stagger = new Map<string, number>(); for (const s of svc) stagger.set(s.table, draw(settle, rv1)); // one draw per table, in table order
  const served = new Map<string, { at: number; revisit: number }>();
  const exc: Excursion[] = [];
  let t = Math.max(settle, draw(r.rest.s[0], r.rest.s[1])); // the first decision: not before the opening
  while (t <= r.horizonS.v) {
    let pick: { s: Service; over: number } | undefined;
    const dueOf = (s: Service): number | undefined => {
      const a = partyStart(occ[s.table], origin + t); if (a === undefined) return undefined; // nobody sits
      if (!seated(occ[s.table], origin + t + s.out)) return undefined; // gone before she would arrive
      const ac = a - origin, last = served.get(s.table);
      if (ac > 0) return last && last.at >= ac ? last.at + last.revisit : ac + greet; // a party that sat during the round
      return last ? last.at + last.revisit : stagger.get(s.table)!; // seated before the round began
    };
    for (const s of svc) {
      const due = dueOf(s); if (due === undefined || due > t) continue;
      const over = t - due;
      if (!pick || over > pick.over || (over === pick.over && s.out < pick.s.out)) pick = { s, over };
    }
    if (!pick) { t++; continue; }
    const serve = draw(r.serve.s[0], r.serve.s[1]), revisit = draw(rv0, rv1), rest = draw(r.rest.s[0], r.rest.s[1]);
    const s = pick.s;
    exc.push({ table: s.table, at: s.at, face: unit(s.face), start: t, out: s.out, serve, back: s.out, m: s.m, path: s.path.points });
    served.set(s.table, { at: t + s.out, revisit });
    t = t + s.out + serve + s.out + rest;
  }
  return exc;
}

// ---------------------------------------------------------------- the reference pose

export type Phase = 'at post' | 'to table' | 'serving' | 'from table' | 'held' | 'to post' | 'together';
export type Pose = { pos: Pt; face: Pt; phase: Phase };
export type Events = {
  holds?: { from: number; to: number; face: Pt }[]; // ticks: the order taken, the order landed (lingerS is added here)
  call?: { at: number; arrive: number; until: number; guardStop: Pt }; // ticks: the call taken, the lover stopped beside her, he turns to go
  end?: { at: number };
};

type Body = { pose(tr: number): Pose; walkBack(tr: number): { path: Path; startMm: number; dur: number; face: Pt } | undefined; resume(tr: number): number };

/** The nominal round of one span, on its round clock in ticks. */
function bodyOf(sv: Server, sp: Span): Body {
  const post: Pt = sv.at, hold = (face: Pt = sv.face): Pose => ({ pos: post, face, phase: 'at post' });
  const exc = sp.excursions.map((e) => {
    const path = new Path(e.path), t0 = e.start * Hz, t1 = t0 + e.out * Hz, t2 = t1 + e.serve * Hz, t3 = t2 + e.back * Hz;
    return { e, path, t0, t1, t2, t3 };
  });
  const find = (tr: number) => exc.find((x) => tr >= x.t0 && tr < x.t3);
  const along = (path: Path, f: number) => Math.round(path.length * Math.min(1, Math.max(0, f)));
  return {
    pose(tr) {
      const x = find(tr); if (!x) return hold();
      if (tr < x.t1) { const mm = along(x.path, (tr - x.t0) / (x.t1 - x.t0)); return { pos: x.path.at(mm), face: x.path.heading(mm), phase: 'to table' }; }
      if (tr < x.t2) return { pos: x.e.at, face: x.e.face, phase: 'serving' };
      const mm = along(x.path, 1 - (tr - x.t2) / (x.t3 - x.t2)), h = x.path.heading(mm);
      return { pos: x.path.at(mm), face: [-h[0], -h[1]], phase: 'from table' };
    },
    walkBack(tr) {
      const x = find(tr); if (!x) return undefined;
      if (tr < x.t1) { const mm = along(x.path, (tr - x.t0) / (x.t1 - x.t0)); return { path: x.path, startMm: mm, dur: tr - x.t0, face: sv.face }; }
      if (tr < x.t2) return { path: x.path, startMm: x.path.length, dur: x.t3 - x.t2, face: sv.face };
      const mm = along(x.path, 1 - (tr - x.t2) / (x.t3 - x.t2)); return { path: x.path, startMm: mm, dur: x.t3 - tr, face: sv.face };
    },
    resume: (tr) => find(tr)?.t3 ?? tr,
  };
}

/** Where a server is at `tick` (mission ticks), given what the cell and the mission did to her: the reference Keel's engine must agree with (to 30 cm). */
export function poseAt(sv: Server, rules: Rounds['rules'], tick: number, ev: Events = {}): Pose {
  const sp = sv.spans.find((s) => tick >= s.from * Hz && tick < s.to * Hz) ?? sv.spans[0]!;
  const body = bodyOf(sv, sp), origin = Math.max(0, sp.from) * Hz;
  const ling = rules.lingerS * Hz, part = rules.partS * Hz;
  type Piece = { from: number; to: number; at: (t: number) => Pose };
  const pieces: Piece[] = [];
  let cur = origin, clock = 0;
  const run = (to: number, c0: number, from: number) => pieces.push({ from, to, at: (t) => body.pose(c0 + (t - from)) });
  const walkTo = (from: number, tr: number): { dur: number; at: (t: number) => Pose } => {
    const w = body.walkBack(tr);
    if (!w) return { dur: 0, at: () => ({ pos: sv.at, face: sv.face, phase: 'to post' }) };
    return { dur: w.dur, at: (t) => { const u = w.dur ? Math.min(1, (t - from) / w.dur) : 1, mm = Math.round(w.startMm * (1 - u)), h = w.path.heading(mm); return { pos: w.path.at(mm), face: [-h[0], -h[1]], phase: 'to post' }; } };
  };
  // an order taken before the last one's linger is over is the same hold, longer (a chain of Helps stands her still the whole way)
  const holds: { from: number; to: number; face: Pt }[] = [];
  for (const h of [...(ev.holds ?? [])].sort((a, b) => a.from - b.from)) {
    const l = holds[holds.length - 1];
    if (l && h.from <= l.to + ling) { l.to = Math.max(l.to, h.to); l.face = h.face; } else holds.push({ ...h });
  }
  const events: { kind: 'hold' | 'call' | 'end'; at: number; h?: (typeof holds)[number] }[] = [
    ...holds.map((h) => ({ kind: 'hold' as const, at: h.from, h })), ...(ev.call ? [{ kind: 'call' as const, at: ev.call.at }] : []), ...(ev.end ? [{ kind: 'end' as const, at: ev.end.at }] : []),
  ].sort((a, b) => a.at - b.at);
  for (const e of events) {
    if (e.at < cur) { // a call or the end taken while she is held (or lingering): the hold is over at once
      if (e.kind === 'hold') continue;
      const last = pieces[pieces.length - 1]; if (last && last.to > e.at) last.to = e.at;
      cur = e.at;
    }
    if (e.at > cur) { run(e.at, clock, cur); clock += e.at - cur; cur = e.at; }
    if (e.kind === 'hold') {
      const h = e.h!, was = body.pose(clock), to = h.to + ling;
      pieces.push({ from: cur, to, at: () => ({ pos: was.pos, face: h.face, phase: 'held' }) });
      cur = to; // the clock stays where it was
    } else if (e.kind === 'call') {
      const c = ev.call!, w = walkTo(cur, clock), arrive = cur + w.dur;
      pieces.push({ from: cur, to: arrive, at: w.at });
      const wait = Math.max(arrive, c.arrive);
      pieces.push({ from: arrive, to: wait, at: () => ({ pos: sv.at, face: sv.face, phase: 'at post' }) });
      const gf: Pt = [c.guardStop[0] - sv.at[0], c.guardStop[1] - sv.at[1]];
      pieces.push({ from: wait, to: Math.max(wait, c.until) + part, at: () => ({ pos: sv.at, face: gf, phase: 'together' }) });
      cur = Math.max(wait, c.until) + part;
      clock = body.resume(clock); // back to the start of the next rest
    } else {
      const w = walkTo(cur, clock), from = cur;
      pieces.push({ from, to: from + w.dur, at: w.at });
      pieces.push({ from: from + w.dur, to: Number.MAX_SAFE_INTEGER, at: () => ({ pos: sv.at, face: sv.face, phase: 'at post' }) });
      cur = Number.MAX_SAFE_INTEGER; break;
    }
  }
  if (cur < Number.MAX_SAFE_INTEGER) run(Number.MAX_SAFE_INTEGER, clock, cur);
  if (tick < origin) return { pos: sv.at, face: sv.face, phase: 'at post' };
  const p = pieces.find((x) => tick >= x.from && tick < x.to) ?? pieces[pieces.length - 1]!;
  return p.at(tick);
}

// ---------------------------------------------------------------- partners

type Walker = { id: string; job: string; stops: PeopleStop[]; lovers: Set<string> };
/** Where a walker is at mission second `t` by their routine (Crowd.RoutinePos): the stop's place, or along the grid walk between two stops; null off the street. */
export function routineAt(g: Grid, places: Record<string, Pt>, w: { stops: PeopleStop[] }, t: number, cache: Map<string, Path>): { at: Pt; stop?: string } | null {
  const s = w.stops;
  for (let i = 0; i < s.length; i++) {
    const [pl, from, to, out] = s[i]!;
    if (t >= from && t < to) return out === 1 ? { at: places[pl]!, stop: pl } : null;
    const n = s[i + 1];
    if (n && t >= to && t < n[1]) {
      if (pl === 'beyond' || n[0] === 'beyond') return null;
      const key = `${pl}>${n[0]}`; let path = cache.get(key); if (!path) { path = leg(g, places[pl]!, places[n[0]]!); cache.set(key, path); }
      const span = n[1] - to;
      return { at: span <= 0 ? path.end : path.at(Math.floor((path.length * (t - to)) / span)) };
    }
  }
  return null;
}
const isPost = (pl: string | undefined) => !!pl && (/-entrance$/.test(pl) || /-bay$/.test(pl));

/** Pairs of partners whose routines bring them within noticeM on a clear line while both are out on the street, neither on a post: the first time only. */
export function findMeetings(g: Grid, people: PeopleIn, cast: Set<string>, rules: RoutinesRules, horizonS: number): Meeting[] {
  const pr = rules.partners, byId = new Map(people.people.map((p) => [p.id, p]));
  const pairs = new Set<string>();
  for (const p of people.people) for (const [k, o] of p.ties) if ((pr.kinds as string[]).includes(k) && byId.has(o) && !cast.has(p.id) && !cast.has(o)) pairs.add([p.id, o].sort().join('|'));
  const cache = new Map<string, Path>(), out: Meeting[] = [], pace = rules.rounds.pace.cmPerS;
  for (const pair of [...pairs].sort()) {
    const [ia, ib] = pair.split('|') as [string, string], A = byId.get(ia)!, B = byId.get(ib)!;
    for (let t = 0; t <= horizonS; t++) {
      const a = routineAt(g, people.places, A, t, cache), b = routineAt(g, people.places, B, t, cache);
      if (!a || !b || isPost(a.stop) || isPost(b.stop)) continue;
      if (d2(a.at, b.at) > (pr.chance.noticeM * 100) ** 2 || !g.lineFree(a.at, b.at)) continue;
      const mid: Pt = [Math.round((a.at[0] + b.at[0]) / 2), Math.round((a.at[1] + b.at[1]) / 2)], u = unit([b.at[0] - a.at[0], b.at[1] - a.at[1]]), h = pr.closeCm.v / 2;
      const sa = g.nearestFree([Math.round(mid[0] - (u[0] / 100) * h), Math.round(mid[1] - (u[1] / 100) * h)]), sb = g.nearestFree([Math.round(mid[0] + (u[0] / 100) * h), Math.round(mid[1] + (u[1] / 100) * h)]);
      if (!sa || !sb || dist(sa, sb) < pr.closeCm.v - 30) break; // no room to stand together here: the pair walks on, once
      const la = leg(g, a.at, sa), lb = leg(g, b.at, sb);
      out.push({ a: ia, b: ib, start: t, atA: sa, atB: sb, faceA: u, faceB: [-u[0], -u[1]], outA: secs(la.length / 10, pace), outB: secs(lb.length / 10, pace), stay: pr.chance.stayS, backA: secs(la.length / 10, pace), backB: secs(lb.length / 10, pace) });
      break;
    }
  }
  return out;
}

// ---------------------------------------------------------------- the whole file

export function build(inp: Inputs = readInputs()): { rounds: Rounds; text: string } {
  const { rules, people, site, tables } = inp, r = rules.rounds, g = new Grid(site.grid);
  const cast = new Set(Object.values(site.cast).map((c) => c.id).filter((x): x is string => typeof x === 'string'));
  const seats: Record<string, Pt> = people.places;
  const servers: Record<string, Server> = {};
  for (const p of [...people.people].sort((a, b) => (a.id < b.id ? -1 : 1))) {
    if (!r.who.jobs.includes(p.job)) continue;
    const spans: Span[] = [];
    p.stops.forEach(([pl, from, to, out], i) => {
      const prefix = r.who.terraces[pl];
      if (!prefix || out !== 1 || to <= 0 || from >= r.horizonS.v) return; // a stop out at a door with a terrace, going on at the mission's start or beginning inside the round's horizon
      const post = people.places[pl]!, svc = servicePoints(g, post, tables, seats, r, prefix);
      const origin = Math.max(0, from);
      const exc = bakeSpan(p.id, i, post, origin, svc, occupancy(people, cast, prefix), r);
      spans.push({ place: pl, from, to, origin, excursions: exc });
    });
    if (!spans.length) continue;
    const post = people.places[spans[0]!.place]!;
    const court = site.places['d:Courtyard']?.at ?? [post[0] + 350, post[1]];
    servers[p.id] = { name: p.name, post: spans[0]!.place, at: post, face: unit([court[0] - post[0], court[1] - post[1]]), spans };
  }
  // the summoned lover stops at the caller's side: the ring of closeCm round her post, free ground only, most lateral first, her left before her right (stopsOf)
  const wid = Object.keys(servers)[0], wsv = wid ? servers[wid] : undefined;
  const gpost = site.places[site.guard.post]?.at ?? [0, 0];
  const gpath = wsv ? leg(g, gpost, wsv.at) : undefined;
  const stops = wsv ? stopsOf(g, gpost, wsv.at, wsv.face, rules.partners.closeCm.v, rules.partners.summoned.side.stepDeg) : [];
  const stop0 = stops[0], guard = { from: site.guard.post, post: wsv?.post ?? '', walkCm: gpath ? Math.round(gpath.length / 10) : 0, ringCm: rules.partners.closeCm.v, stopAt: stop0?.at ?? ([0, 0] as Pt), face: stop0?.face ?? ([0, 0] as Pt), stops };
  const meetings = findMeetings(g, people, cast, rules, r.horizonS.v);
  const base: Rounds = {
    schema: 'suborned.sim.rounds/1',
    from: [SITE, PEOPLE, BLOCK, RULES_FILE, 'tools/routines/rounds.ts'],
    frame: 'integer centimetres, block frame (x south, y east), as sim/data/handover.site.json. Round clock: whole seconds from the stop\'s origin (mission second 0 if the stop began earlier); legs in whole seconds along the site grid\'s route at pace. Ticks are 10 Hz.',
    rules: { file: RULES_FILE, sha256: sha(inp.rulesText), paceCmPerS: r.pace.cmPerS, lingerS: r.holds.lingerS, closeCm: rules.partners.closeCm.v, clearCm: rules.partners.clearCm.v, partS: rules.partners.partS.v, settleS: r.opening.settleS, stayS: rules.partners.summoned.stay.s, chanceStayS: rules.partners.chance.stayS },
    servers, meetings, guard, vectors: {},
  };
  base.vectors = vectorsOf(base);
  return { rounds: base, text: pretty(base) + '\n' };
}

/** The places the summoned partner may stand (partners.summoned.side): a point on the ring of `ringCm` round her post every `stepDeg` degrees off her facing (both hands), kept only if it is free
 *  ground on the walk grid and the guard can walk to it; most lateral first (nearest 90 degrees; on a tie the front one), her left (the facing turned a quarter counter-clockwise on the plan: east of a
 *  south-facing post) before her right. Integer centimetres; the engine reads the list and never computes an angle. */
export function stopsOf(g: Grid, from: Pt, post: Pt, facing: Pt, ringCm: number, stepDeg: number): Stop[] {
  const f = unit(facing), fx = f[0] / 100, fy = f[1] / 100, lx = -fy, ly = fx, out: (Stop & { key: number })[] = [];
  for (let a = 0; a <= 180; a += stepDeg) for (const hand of a === 0 || a === 180 ? [1] : [1, -1]) {
    const r = (a * Math.PI) / 180, c = Math.cos(r), sn = Math.sin(r) * hand;
    const at: Pt = [Math.round(post[0] + ringCm * (c * fx + sn * lx)), Math.round(post[1] + ringCm * (c * fy + sn * ly))];
    if (!g.isFree(at)) continue;
    const L = leg(g, from, at); if (L.length <= 0) continue;
    out.push({ at, face: unit([post[0] - at[0], post[1] - at[1]]), deg: a * hand, walkCm: Math.round(L.length / 10), key: Math.abs(a - 90) * 4 + (a < 90 ? 0 : 1) * 2 + (hand === 1 ? 0 : 1) });
  }
  return out.sort((x, y) => x.key - y.key).map(({ key: _k, ...st }) => st);
}

/** The pick (the engine's PartnerStop): the first stop with nobody within clearCm of it, else the stop whose nearest person is farthest (the earlier on a tie), else undefined (no stop in the file: the old stop,
 *  on his own route closeCm short of her post). `people` = every other person at the tick of the call: the operatives not down and everyone on the street, not her and not him. */
export function pickStop(stops: Stop[], people: Pt[], clearCm: number): Stop | undefined {
  let best: Stop | undefined, bestD = -1;
  for (const s of stops) {
    let nearest = Infinity; for (const p of people) nearest = Math.min(nearest, dist(s.at, p));
    if (nearest >= clearCm) return s;
    if (nearest > bestD) { bestD = nearest; best = s; }
  }
  return best;
}

/** The reference poses Keel's engine must reproduce: free (no one touches her), a Listen then a Help held, a call that finds her mid-walk, and a mission that ends her round. */
export function vectorsOf(R: Rounds): Rounds['vectors'] {
  const out: Rounds['vectors'] = {};
  for (const [id, sv] of Object.entries(R.servers)) {
    const sp = sv.spans[0]!, ex = sp.excursions, v: Record<string, Vec[]> = {};
    const rowOf = (tick: number, ev: Events): Vec => { const p = poseAt(sv, R.rules, tick, ev); return [tick, p.pos[0], p.pos[1], p.face[0], p.face[1], p.phase]; };
    const sample = (from: number, to: number, step: number, ev: Events) => { const o: Vec[] = []; for (let t = from; t <= to; t += step) o.push(rowOf(t, ev)); return o; };
    v.free = sample(0, 5100, 50, {});
    if (ex.length) {
      const e0 = ex[0]!, o0 = sp.origin * Hz, at0 = o0 + e0.start * Hz; // inside the first excursion
      const hold = { from: at0 + 5 * Hz, to: at0 + 5 * Hz + 420, face: [100, 0] as Pt };
      v.held = sample(at0, at0 + 1500, 50, { holds: [hold] }); // held (a 42 s Listen) from 5 s into the first walk out, then the round goes on
      const gw = Math.round((R.guard.stops[0]!.walkCm / R.rules.paceCmPerS) * Hz), c0 = at0 + Math.floor(e0.out * Hz * 0.5); // the lover's walk, to his stop at her side
      const call = { at: c0, arrive: c0 + gw, until: c0 + gw + R.rules.stayS * Hz, guardStop: R.guard.stopAt };
      v.called = sample(call.at - 50, call.until + 600, 50, { call });
      const mid = ex[Math.min(1, ex.length - 1)]!, e1 = o0 + mid.start * Hz + mid.out * Hz + 20; // 2 s into a pause at a table
      v.ended = sample(e1 - 50, e1 + 400, 25, { end: { at: e1 } });
    }
    out[id] = v;
  }
  return out;
}

/** One key per line, arrays of numbers (points, polygons) and short rows kept on one line. */
export function pretty(v: unknown, ind = ''): string {
  const flat = (x: unknown): boolean => typeof x === 'number' || typeof x === 'string' || (Array.isArray(x) && x.every(flat));
  if (Array.isArray(v)) {
    if (v.every(flat)) return JSON.stringify(v);
    return `[\n${v.map((x) => ind + ' ' + pretty(x, ind + ' ')).join(',\n')}\n${ind}]`;
  }
  if (v && typeof v === 'object') {
    const es = Object.entries(v as Record<string, unknown>);
    return `{\n${es.map(([k, x]) => `${ind} ${JSON.stringify(k)}: ${pretty(x, ind + ' ')}`).join(',\n')}\n${ind}}`;
  }
  return JSON.stringify(v);
}

// ---------------------------------------------------------------- what it makes of the Handover (the numbers the brief quotes)

export function summary(R: Rounds, inp: Inputs, windowS = 510) {
  const sv = Object.values(R.servers)[0]; if (!sv) return undefined;
  const P = (n: string): Pt => inp.site.places[n]!.at;
  const near = (a: Pt, b: Pt, m: number) => d2(a, b) <= (m * 100) ** 2;
  const reach = { listen: [P('beside-inez'), 3], turn: [P('d:Courtyard'), 4], watch: [P('d:Courtyard'), 6] } as const;
  const n = windowS * Hz;
  const phase: Record<string, number> = {}, got: Record<string, number> = { listen: 0, turn: 0, watch: 0 };
  let longestAway = 0, away = 0, latest = 0;
  for (let t = 0; t < n; t++) {
    const p = poseAt(sv, R.rules, t);
    phase[p.phase] = (phase[p.phase] ?? 0) + 1;
    for (const [k, [pt, m]] of Object.entries(reach) as [string, readonly [Pt, number]][]) if (near(p.pos, pt, m)) got[k]!++;
    if (p.phase === 'at post') { away = 0; } else { away++; longestAway = Math.max(longestAway, away); latest = t; }
  }
  const share = (k: string) => Math.round(((phase[k] ?? 0) / n) * 1000) / 10;
  return {
    server: sv.name, excursionsInWindow: sv.spans[0]!.excursions.filter((e) => e.start * Hz < n).length,
    share: { atPost: share('at post'), walking: Math.round((((phase['to table'] ?? 0) + (phase['from table'] ?? 0)) / n) * 1000) / 10, serving: share('serving') },
    reachShare: { listenAtBesideInez3m: Math.round((got.listen! / n) * 1000) / 10, turnAtCourtyard4m: Math.round((got.turn! / n) * 1000) / 10, watchAtCourtyard6m: Math.round((got.watch! / n) * 1000) / 10 },
    longestAwayS: longestAway / Hz, lastAwayAtS: latest / Hz,
    longestLegS: Math.max(0, ...sv.spans[0]!.excursions.map((e) => e.out)), guardWalkS: Math.round((R.guard.walkCm / R.rules.paceCmPerS) * 10) / 10, guardStopWalkS: Math.round(((R.guard.stops[0]?.walkCm ?? 0) / R.rules.paceCmPerS) * 10) / 10,
  };
}

if (import.meta.main) {
  const argv = process.argv.slice(2), oi = argv.indexOf('--out'), check = argv.includes('--check');
  const file = join(root, oi >= 0 ? argv[oi + 1]! : ROUNDS_CANDIDATE), inp = readInputs(), { rounds, text } = build(inp);
  if (check) {
    const have = existsSync(file) ? readFileSync(file, 'utf8') : '';
    if (have !== text) { console.error(`${file} is not what the inputs make (bun run rounds)`); process.exit(1); }
    console.log('ok  the rounds file is what the inputs make');
  } else { mkdirSync(dirname(file), { recursive: true }); writeFileSync(file, text); console.log(`wrote ${file} (${text.length} bytes)`); }
  console.log(JSON.stringify(summary(rounds, inp), null, 1));
  console.log(`meetings: ${rounds.meetings.length}; servers: ${Object.keys(rounds.servers).join(', ')}`);
}
