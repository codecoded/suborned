// The `human` bot policy (Latch; Sky 2026-10-25: "I think the biggest influence would be a bot that's indecisive. Humans aren't consistent with
// their actions."). The other policies (random, greedy, cautious) follow one habit and never plan; this one has an intent and plays it badly.
//
// Intent: one recorded winning route (contracts/missions/handover/routes/: route A at the front door with Inez, B at the pickup with the driver,
// G through the guard, the loud route), picked per row from the row's seed: a route family first (A, B, G, loud, evenly), then a script in it
// (the family's primary twice as likely). Its orders become the plan's steps, played toward through real input only (BotRun.cs Human): select
// the agent, a ground click (a double click when the order runs) for a move, a left click on the thing and its wedge for an act, F for a draw,
// V for a stance, both buttons for panic.
//
// Following: the steps go in script order, one input sequence at a time, as a person's attention does. A step is ready once the step before it
// for the same agent is resolved, its agent is free (not on an act, walking in to one, or holding one in the host), and the mission clock is past
// its script tick. A step within COUPLE_TICKS of the script's previous tick (the orders given together just before it) keeps that gap from when
// the last of them was actually given (the call that pulls the guard, then the entry 2.4 s later), so a late step drags its close followers with
// it; a step further off waits only for its own tick (the world's timetable: Vale's exit, the shift change). Orders the script gives on one tick
// are free among themselves (only one agent's own go in order). A blocked step lets the next ready one go, up to LOOKAHEAD steps on.
//
// Indecision on top, from tools/qa/human-profile.json (Suture, measured from Sky's saved sessions; DEFAULT_PROFILE until the file lands): each
// step draws its own noise before it is played, every draw from a stream seeded by the row's seed and the noise's name, so one value changing
// never reshuffles the others and the same seed always gives the same tape. The tape goes to the player with the row (BotRow.human); the player
// only reads it, so a row replays exactly (and its session script replays exactly through `suborned-sim play`).
//
// usage: bun tools/qa/human.ts [--seed n] [--route id]     print the tape a seed gives
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT } from '../missions/check/engine';

export const PROFILE_FILE = join(ROOT, 'tools/qa/human-profile.json');
export const ROUTES_DIR = join(ROOT, 'contracts/missions/handover/routes');
/** A step this many ticks (0.1 s) or fewer after the one before it keeps that gap from its real giving. */
export const COUPLE_TICKS = 60;
/** How far past the first unresolved step the follower may look for a ready one. */
export const LOOKAHEAD = 4;
/** Abandons that switch the route, per row (the main route, then two others at most); a wander resumes the same route. */
export const MAX_ROUTE_SWITCHES = 2;

// ---------------------------------------------------------------- seeded draws

/** FNV-1a of a string (the seed streams' key). */
export function fnv(s: string): number { let h = 2166136261 >>> 0; for (const ch of s) { h ^= ch.charCodeAt(0); h = Math.imul(h, 16777619) >>> 0; } return h >>> 0; }
/** A seeded stream in [0, 1) (mulberry32), one per (seed, name): adding draws to one noise never moves another's. */
export function stream(seed: number, name: string): () => number {
  let a = fnv(`${seed}:${name}`) || 1;
  return () => { a = (a + 0x6d2b79f5) >>> 0; let t = a; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };
}
/** A time in seconds: lognormal with the given median and 90th percentile (a person's pauses are skewed: mostly short, now and then long). */
export function drawTime(r: () => number, d: Dist): number {
  const sigma = d.p90 > d.p50 && d.p50 > 0 ? Math.log(d.p90 / d.p50) / 1.2815516 : 0;
  const u1 = Math.max(1e-9, r()), u2 = r(), z = Math.sqrt(-2 * Math.log(u1)) * Math.cos(2 * Math.PI * u2);
  return round(Math.min(120, Math.max(0.05, d.p50 * Math.exp(sigma * z))), 2);
}
const round = (v: number, n: number) => Math.round(v * 10 ** n) / 10 ** n;

// ---------------------------------------------------------------- the profile

/** A time's spread: its median and 90th percentile, seconds. */
export type Dist = { p50: number; p90: number };
export type HumanProfile = {
  cancelProb: number; cancelDelayS: Dist; reorderProb: number; switchProb: number; gapS: Dist; idleProb: number; idleS: Dist; abandonProb: number;
  misclickProb: number; ringOpenCloseProb: number; cameraTurnProb: number; wrongVerbProb: number;
  /** The same act on the same target again once it is done ("the repeat Watch"), per act step on a target. */
  repeatProb: number;
  /** Third person (T): measured per decision overall (thirdPersonProb), but it clusters by session (Suture, brief 060): a row uses it with
   *  thirdPersonRowProb, then a step looks through it with thirdPersonInRowProb; thirdPersonS is a look's length. The row model is the one played. */
  thirdPersonProb: number; thirdPersonS: Dist; thirdPersonRowProb: number; thirdPersonInRowProb: number;
};
/** [S] starting values until Suture's measured file lands (each a per-step chance, or a time's median and 90th percentile). */
export const DEFAULT_PROFILE: HumanProfile = {
  cancelProb: 0.06, cancelDelayS: { p50: 1.2, p90: 4 }, reorderProb: 0.15, switchProb: 0.12, gapS: { p50: 1.5, p90: 5 }, idleProb: 0.05, idleS: { p50: 5, p90: 15 },
  abandonProb: 0.01, misclickProb: 0.06, ringOpenCloseProb: 0.08, cameraTurnProb: 0.06, wrongVerbProb: 0.04, repeatProb: 0.2,
  thirdPersonProb: 0.1, thirdPersonS: { p50: 3, p90: 8 }, thirdPersonRowProb: 0.21, thirdPersonInRowProb: 0.27, // Suture's row model (12 of 56 of Sky's sessions; 0.27 a decision in them)
};
const PROBS = ['cancelProb', 'reorderProb', 'switchProb', 'idleProb', 'abandonProb', 'misclickProb', 'ringOpenCloseProb', 'cameraTurnProb', 'wrongVerbProb', 'repeatProb', 'thirdPersonProb', 'thirdPersonRowProb', 'thirdPersonInRowProb'] as const;
const TIMES = ['cancelDelayS', 'gapS', 'idleS', 'thirdPersonS'] as const;
export type ProfileSource = { file: string; fromFile: string[]; defaulted: string[]; ignored: string[] };

/** A time as Suture may write it: seconds (taken as the median, the 90th percentile 2.5 times it), or {p50, p90} / {median, p90} / {mean}. */
export function readDist(v: unknown): Dist | undefined {
  const ok = (n: unknown): n is number => typeof n === 'number' && Number.isFinite(n) && n > 0;
  if (ok(v)) return { p50: v, p90: round(v * 2.5, 3) };
  if (!v || typeof v !== 'object') return undefined;
  const o = v as Record<string, unknown>, p50 = ok(o.p50) ? o.p50 : ok(o.median) ? o.median : ok(o.mean) ? o.mean : undefined;
  if (p50 === undefined) return undefined;
  return { p50, p90: ok(o.p90) && o.p90 >= p50 ? o.p90 : round(p50 * 2.5, 3) };
}

/** The profile from Suture's file, field by field over the defaults: a missing or malformed field keeps its default and says so. The values may
 *  sit at the top level or under `values` (his file may carry its own provenance alongside). */
export function readProfile(raw: unknown, file = PROFILE_FILE): { profile: HumanProfile; source: ProfileSource } {
  const top = raw && typeof raw === 'object' ? raw as Record<string, unknown> : {};
  const v = top.values && typeof top.values === 'object' ? top.values as Record<string, unknown> : top;
  const profile: HumanProfile = structuredClone(DEFAULT_PROFILE), fromFile: string[] = [], defaulted: string[] = [];
  for (const k of PROBS) { const x = v[k]; if (typeof x === 'number' && Number.isFinite(x) && x >= 0 && x <= 1) { profile[k] = x; fromFile.push(k); } else defaulted.push(k); }
  for (const k of TIMES) { const d = readDist(v[k]); if (d) { profile[k] = d; fromFile.push(k); } else defaulted.push(k); }
  const known = new Set<string>([...PROBS, ...TIMES]), ignored = Object.keys(v).filter(k => !known.has(k) && !['schema', 'note', 'sessions', 'source', 'values', 'measured', 'by'].includes(k));
  return { profile, source: { file: raw === undefined ? '' : file, fromFile, defaulted, ignored } };
}
export function loadProfile(file = PROFILE_FILE) { return readProfile(existsSync(file) ? JSON.parse(readFileSync(file, 'utf8')) : undefined, file); }

// ---------------------------------------------------------------- the routes

export type RouteOrder = { tick: number; op?: string; do: string; to?: string | number[]; target?: string; approach?: string; how?: string; stance?: string; run?: boolean };
export type RouteScript = { id: string; group: string; primary: boolean; orders: RouteOrder[] };

/** The recorded winning routes: quiet and loud scripts that end in success (fails, careless twins and the left-behind outcomes are not intents). */
export function winningRoutes(dir = ROUTES_DIR): RouteScript[] {
  const idx = JSON.parse(readFileSync(join(dir, 'routes.json'), 'utf8')) as { scripts: { id: string; file: string; kind: string; route?: string; primary?: boolean; expects?: { outcome?: string; left?: unknown } }[] };
  return idx.scripts.filter(s => (s.kind === 'quiet' || s.kind === 'loud') && s.expects?.outcome === 'success' && !s.expects.left)
    .map(s => ({ id: s.id, group: s.kind === 'loud' ? 'loud' : s.route ?? s.id, primary: !!s.primary, orders: (JSON.parse(readFileSync(join(dir, s.file), 'utf8')) as { orders: RouteOrder[] }).orders }));
}

/** The row's route: a family evenly (A, B, G, loud), then a script in it, the primary weighted twice; `pin` names one; `not` avoids families. */
export function pickRoute(routes: RouteScript[], r: () => number, pin?: string, not: string[] = []): RouteScript {
  if (pin) { const p = routes.find(x => x.id === pin); if (!p) throw new Error(`human: no winning route '${pin}' (have ${routes.map(x => x.id).join(', ')})`); return p; }
  let groups = [...new Set(routes.map(x => x.group))].sort(); const free = groups.filter(g => !not.includes(g)); if (free.length) groups = free;
  const g = groups[Math.floor(r() * groups.length)]!, inG = routes.filter(x => x.group === g).sort((a, b) => a.id.localeCompare(b.id));
  const w = inG.map(x => (x.primary ? 2 : 1)), total = w.reduce((a, b) => a + b, 0); let pick = r() * total;
  for (let i = 0; i < inG.length; i++) { pick -= w[i]!; if (pick < 0) return inG[i]!; }
  return inG[inG.length - 1]!;
}

// ---------------------------------------------------------------- the tape

/** One step of the plan (an order of the route) and its noise, as the player reads it (BotRun.HumanStep; `act` is the order's `do`). */
export type HumanStep = {
  i: number; tick: number; op: string; act: string; target: string; approach: string; how: string; stance: string; run: boolean;
  /** a move's named place ("d:Courtyard", "cabinet"), or its point in engine cm when xy */ place: string; xy: boolean; x: number; y: number;
  /** the steps of the script's previous tick it keeps its gap to (the first index and how many; -1: it waits for its own tick only), and that gap
   *  in ticks */ after: number; afterN: number; gap: number;
  /** the same agent's step before it (-1 none): steps of one agent go in order */ opPrev: number;
  // noise, drawn for the step's first try (a retry is played clean)
  gapS: number; idleS: number; turn: number; switchTo: number; peekS: number; ringClose: boolean; misclick: boolean; missDx: number; missDy: number;
  wrong: number; wrongNoticeS: number; cancel: boolean; cancelS: number; reorder: boolean; abandon: number; wanderS: number;
  /** how a cancel undoes it: "x" (the stop key) or "redirect" (a ground click cancelDx, cancelDy px from the agent: a new walk replaces the order,
   *  as most of Sky's undoings were: 230 of 244 moves sent elsewhere, 43 of 54 acts given another order) */ cancelBy: string; cancelDx: number; cancelDy: number;
  /** given again once done (the same act on the same target) */ repeat: boolean;
};
export type HumanRoute = { route: string; group: string; steps: HumanStep[] };
export type HumanTape = { schema: 'suborned.bot.human/1'; seed: number; route: string; group: string; /** this row looks through third person now and then */ thirdPerson: boolean; steps: HumanStep[]; alts: HumanRoute[]; profile: HumanProfile; profileSource: ProfileSource };
/** abandon: -1 keeps the plan; -2 wanders a while and comes back; n >= 0 drops it for alts[n]. */
export const WANDER = -2;

/** The plan's steps from a route's orders (in tick order, as every route script is), without noise: the coupling to the script's previous tick
 *  and the same agent's previous step. */
export function stepsOf(orders: RouteOrder[]): Omit<HumanStep, NoiseKey>[] {
  const lastOf = new Map<string, number>();
  return orders.map((o, i) => {
    if (i > 0 && o.tick < orders[i - 1]!.tick) throw new Error(`human: route orders out of tick order at ${i}`);
    let end = i - 1; while (end >= 0 && orders[end]!.tick === o.tick) end--; // the previous tick's group: [start, end]
    let start = end; while (start > 0 && orders[start - 1]!.tick === orders[end]!.tick) start--;
    const op = o.op ?? 'cell', gap = end >= 0 ? o.tick - orders[end]!.tick : 0, coupled = end >= 0 && gap <= COUPLE_TICKS, xy = Array.isArray(o.to);
    const s = {
      i, tick: o.tick, op, act: o.do, target: o.target ?? '', approach: o.approach ?? '', how: o.how ?? '', stance: o.stance ?? '', run: !!o.run,
      place: typeof o.to === 'string' ? o.to : '', xy, x: xy ? Number((o.to as number[])[0]) : 0, y: xy ? Number((o.to as number[])[1]) : 0,
      after: coupled ? start : -1, afterN: coupled ? end - start + 1 : 0, gap: coupled ? gap : 0, opPrev: lastOf.get(op) ?? -1,
    };
    lastOf.set(op, i);
    return s;
  });
}
type NoiseKey = 'gapS' | 'idleS' | 'turn' | 'switchTo' | 'peekS' | 'ringClose' | 'misclick' | 'missDx' | 'missDy' | 'wrong' | 'wrongNoticeS' | 'cancel' | 'cancelS' | 'reorder' | 'abandon' | 'wanderS' | 'cancelBy' | 'cancelDx' | 'cancelDy' | 'repeat';
const OPS = ['scout', 'fixer', 'tech', 'enforcer'];
/** Steps given through the ring (a left click on a thing, then a wedge): the ones a misclick, a wrong wedge or an opened-and-shut ring can touch. */
export const RING_ACTS = new Set(['watch', 'listen', 'turn', 'call', 'loop', 'reroute', 'mislead', 'enter', 'take', 'board', 'go', 'carry', 'restrain', 'stash', 'drop', 'takedown', 'fire']);
/** Acts a person gives again on the same target (Sky's repeats were Watches, Listens, turns): never one that changes the scene for good (enter,
 *  take, board, go, a body's carry or hiding, a takedown, a shot). */
export const REPEATABLE = new Set(['watch', 'listen', 'turn', 'call', 'loop', 'reroute', 'mislead']);

/** Each step's noise, one stream per noise: the same seed and profile give the same tape. `altBase` is the alt route a route abandon goes to
 *  (-1: this route may only wander); `thirdPerson`: the row is one that uses the follow view. */
export function noise(base: Omit<HumanStep, NoiseKey>[], p: HumanProfile, seed: number, altBase: number, thirdPerson = false): HumanStep[] {
  const R = (n: string) => stream(seed, n);
  const gap = R('gap'), idle = R('idle'), turn = R('turn'), sw = R('switch'), peek = R('peek'), ring = R('ring'), miss = R('misclick'), wrong = R('wrong'), cancel = R('cancel'), reorder = R('reorder'), ab = R('abandon'), again = R('repeat'), how = R('cancel-how');
  let abandoned = false;
  return base.map(b => {
    const s = { ...b } as HumanStep;
    s.gapS = drawTime(gap, p.gapS);
    s.idleS = idle() < p.idleProb ? drawTime(idle, p.idleS) : 0;
    s.turn = turn() < p.cameraTurnProb ? (turn() < 0.5 ? -1 : 1) : 0;
    { const yes = sw() < p.switchProb, others = OPS.filter(o => o !== b.op), k = Math.floor(sw() * others.length); s.switchTo = yes ? OPS.indexOf(others[k]!) : -1; }
    s.peekS = peek() < (thirdPerson ? p.thirdPersonInRowProb : 0) ? drawTime(peek, p.thirdPersonS) : 0;
    const ringy = RING_ACTS.has(b.act);
    s.ringClose = ring() < p.ringOpenCloseProb && ringy;
    { const yes = miss() < p.misclickProb, a = miss() * 2 * Math.PI, rad = 36 + miss() * 44; s.misclick = yes && (ringy || b.act === 'move'); s.missDx = s.misclick ? round(Math.cos(a) * rad, 1) : 0; s.missDy = s.misclick ? round(Math.sin(a) * rad, 1) : 0; }
    { const yes = wrong() < p.wrongVerbProb, dir = wrong() < 0.5 ? -1 : 1, t = drawTime(wrong, { p50: 1.5, p90: 3.5 }); s.wrong = yes && ringy ? dir : 0; s.wrongNoticeS = s.wrong ? t : 0; }
    { const yes = cancel() < p.cancelProb, t = drawTime(cancel, p.cancelDelayS); s.cancel = yes && b.op !== 'cell'; s.cancelS = s.cancel ? t : 0; }
    { const x = how() < (b.act === 'move' ? 14 / 244 : 11 / 54), a = how() * 2 * Math.PI, rad = 110 + how() * 110; s.cancelBy = s.cancel ? (x ? 'x' : 'redirect') : ''; s.cancelDx = s.cancelBy === 'redirect' ? round(Math.cos(a) * rad, 1) : 0; s.cancelDy = s.cancelBy === 'redirect' ? round(Math.sin(a) * rad, 1) : 0; } // Sky: stops were 14 of 244 move undoings, 11 of 54 act undoings
    s.repeat = again() < p.repeatProb && REPEATABLE.has(b.act) && b.target.length > 0 && b.op !== 'cell';
    s.reorder = reorder() < p.reorderProb;
    { const yes = ab() < p.abandonProb, toRoute = ab() < 0.5, w = round(15 + ab() * 30, 1); s.abandon = -1; s.wanderS = 0; if (yes && b.i > 0) { if (toRoute && altBase >= 0 && !abandoned) { s.abandon = altBase; abandoned = true; } else if (!toRoute) { s.abandon = WANDER; s.wanderS = w; } } }
    return s;
  });
}

/** A row's tape: the route its seed picks (or `pin`), its steps and noise, and the routes an abandon may switch to (another family each), each
 *  with its own noise from the seed, the last one unable to switch again. */
export function humanTape(seed: number, routes: RouteScript[], prof: { profile: HumanProfile; source: ProfileSource } = loadProfile(), pin?: string): HumanTape {
  const pick = stream(seed, 'route'), main = pickRoute(routes, pick, pin), used = [main.group], alts: HumanRoute[] = [];
  const tp = stream(seed, 'third-person-row')() < prof.profile.thirdPersonRowProb; // one person: the habit holds across a route switch
  for (let k = 0; k < MAX_ROUTE_SWITCHES; k++) { const a = pickRoute(routes, pick, undefined, used); used.push(a.group); alts.push({ route: a.id, group: a.group, steps: [] }); }
  alts.forEach((a, k) => { a.steps = noise(stepsOf(routes.find(x => x.id === a.route)!.orders), prof.profile, fnv(`${seed}:alt${k}`) % 1_000_000, k + 1 < alts.length ? k + 1 : -1, tp); });
  return { schema: 'suborned.bot.human/1', seed, route: main.id, group: main.group, thirdPerson: tp, steps: noise(stepsOf(main.orders), prof.profile, seed, alts.length ? 0 : -1, tp), alts, profile: prof.profile, profileSource: prof.source };
}

// ---------------------------------------------------------------- following (the player mirrors this: BotRun.Human.Ready and NextStep)

/** What the follower knows: the mission tick, each step's real giving tick (null until given) and whether it is resolved (given, or given up). */
export type FollowState = { tick: number; issued: (number | null)[]; resolved: boolean[]; /** the agent can take an order now (not on an act or walking in to one) */ free: (op: string) => boolean };

/** The earliest tick a step may go: its own tick, or, coupled, the last real giving among the previous tick's steps plus the script's gap. null
 *  while one of them is still to give (a step given up keeps no gap). */
export function earliest(steps: HumanStep[], i: number, st: FollowState): number | null {
  const s = steps[i]!; let e = s.tick; if (s.after < 0) return e;
  for (let j = s.after; j < s.after + s.afterN; j++) { const at = st.issued[j]; if (at == null) { if (!st.resolved[j]) return null; continue; } e = Math.max(e, at + s.gap); }
  return e;
}
export function ready(steps: HumanStep[], i: number, st: FollowState): boolean {
  const s = steps[i]!; if (st.resolved[i]) return false;
  if (s.opPrev >= 0 && !st.resolved[s.opPrev]) return false;
  const e = earliest(steps, i, st); if (e == null || st.tick < e) return false;
  return st.free(s.op);
}
/** The step to play next: the first ready one within LOOKAHEAD of the first unresolved; with that step's reorder noise, the next ready step of
 *  another agent goes first when there is one. null: nothing is ready (wait). */
export function nextStep(steps: HumanStep[], st: FollowState): { i: number; over?: number } | null {
  const first = st.resolved.findIndex(r => !r); if (first < 0) return null;
  const end = Math.min(steps.length, first + LOOKAHEAD + 1);
  let c = -1; for (let i = first; i < end; i++) if (ready(steps, i, st)) { c = i; break; }
  if (c < 0) return null;
  if (steps[c]!.reorder) for (let j = c + 1; j < end; j++) if (steps[j]!.op !== steps[c]!.op && ready(steps, j, st)) return { i: j, over: c };
  return { i: c };
}

if (import.meta.main) {
  const a = process.argv.slice(2), val = (k: string) => { const i = a.indexOf(k); return i >= 0 ? a[i + 1] : undefined; };
  const t = humanTape(Number(val('--seed') ?? 1), winningRoutes(), loadProfile(), val('--route'));
  console.log(JSON.stringify(t, null, 1));
}
