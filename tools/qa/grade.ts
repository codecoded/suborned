// Brief 060 tier 2 (Suture, smoke triage 2026-10-24): the bot's in-player checks are cheap proxies, raised as the run plays. This regrades a row's
// anomalies offline from the files the row wrote (feet.csv, mem.csv, the anomaly replays) against the drawn ground (the walk file) and the café seats,
// so Sky's list carries real faults only. Every other hit stays in the full table as `info`, named by its cause, never dropped silently.
//
//   Q-SOLID  real when a figure's body (r 0.25 m) overlaps a drawn solid (walk file kind 2 or 4, 0.25 m cells) by more than 5 cm (brief 060 §2), i.e. its
//            feet come within 0.20 m of one, on any sample from 2 s before the hit to 1 s after. Not real (info): a café customer at, going to or getting up
//            from their chair (inside the table set by design); a figure off the walk map; a figure in the walk grid's clearance but clear of every drawn
//            solid (the grid keeps a walker 0.65 m off ground solids and 0.3 m off building faces, so a cell can be "blocked" with nothing drawn in it).
//            Real hits by the rival crew's and the response unit's followers (rivals/1-2, security/1) are one cause: the host draws them at a fixed
//            0.9 m offset from the one pose the engine gives the group (HostViews.cs, rivals and security), so they walk through whatever lies 0.9 m to
//            the side of the leader's path.
//            Two more causes (0.2.122 human-b, 2026-10-26): a crowd figure that spends 3 s or more inside a drawn solid within 60 s before or 30 s after the
//            hit is on a routine leg the engine drew as a straight line (Crowd.Leg: an end with no free walk cell within 6 m, such as t1-records off the
//            grid, gives no route, so the leg is [from, to, to] and runs through the Records Office); a crowd figure within 0.8 m of a cast figure at the
//            hit was pushed there by the host stepping it aside for the cast (HostViews.GiveWay: 70 cm to the camera's side, with no free-ground test).
//   Q-GIVER  an agent in the car offered for Go is the check's fault, not the ring's: Go is an order by anyone once someone is in the car (GDD §06), and an
//            agent aboard gives it (the engine takes Go before any doer state). Every other giver in the car, down, carried or inside stays a finding.
//   Q-OFFER  a refusal for the doer's own state (agent-down) is a race, not a lie, when the doer went down in the order's own tick or the one before it
//            (≤ 0.1 s: nothing on screen could have shown it); found by replaying the anomaly's script on the build engine.
//   Q-MEM    a leak is growth that holds: after a 120 s warm-up the window splits in thirds; each third's floor (its lowest 1 Hz sample, so shots and
//            other transients drop out) must rise on both steps by more than 3 MB a minute (total) or 1 MB a minute (managed). A single step (the
//            response spawning in) moves one step only. Judged only when each third holds at least 60 s; a shorter row is "not judged".
//   Q-FEET   share: |feet − drawn floor| ≤ 5 cm on ≥ 99.9% of samples (brief 060 §2), carried and lifted figures left out (the CSV's skip column, or
//            more than 0.6 m above the floor where the row has no such column). The player's own run rule (15 cm for 0.5 s) stands beside it.
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import type { Severity } from './types';

export const GRADE = {
  solid: { bodyR: 0.25, overlapM: 0.05, beforeTicks: 20, afterTicks: 10, seatR: 1.5, followers: ['rivals/1', 'rivals/2', 'security/1'], legInsideS: 3, legBackTicks: 600, legAheadTicks: 300, giveWayM: 0.8, cast: ['guard', 'waitress', 'driver', 'councillor'] },
  giver: { fromCar: ['go'] },
  offer: { raceTicks: 1, doerCodes: ['agent-down'] },
  mem: { warmS: 120, minThirdS: 60, totalMBMin: 3, managedMBMin: 1 },
  feet: { tolM: 0.05, share: 0.999, liftedM: 0.6 },
} as const;

/** What a regrade says about one anomaly: its cause (the issue key's tail), the severity it ranks at, plain words, and why. */
export type Grade = { cause: string; severity: Severity; title: string; owner: string; note: string; /** the rule as the regrade judges it, where it differs from the player's */ expected?: string };

// ---------------------------------------------------------------- the drawn ground (out/world/<plan>.walk.json, suborned.walk/1)

export type Walk = { x0: number; y0: number; cell: number; nx: number; ny: number; kind: Uint8Array; toBlock: (x: number, z: number) => [number, number] };

export function loadWalk(file: string): Walk | undefined {
  if (!existsSync(file)) return undefined;
  const f = JSON.parse(readFileSync(file, 'utf8'));
  if (f.schema !== 'suborned.walk/1') return undefined;
  const o: [number, number] = f.frame.origin, ax: [number, number] = f.frame.x, ay: [number, number] = f.frame.y, det = ax[0] * ay[1] - ax[1] * ay[0];
  return {
    x0: f.window.x0, y0: f.window.y0, cell: f.cell, nx: f.nx, ny: f.ny, kind: new Uint8Array(Buffer.from(f.kind, 'base64')),
    // Unity (x, z) = plan (x, y) = origin + bx·ax + by·ay → block (bx, by), metres
    toBlock: (x, z) => { const dx = x - o[0], dy = z - o[1]; return [(dx * ay[1] - dy * ay[0]) / det, (ax[0] * dy - ax[1] * dx) / det]; },
  };
}

/** Inside the walk window. */
export const onMap = (w: Walk, b: [number, number]) => b[0] >= w.x0 && b[1] >= w.y0 && b[0] < w.x0 + w.nx * w.cell && b[1] < w.y0 + w.ny * w.cell;

/** Distance (m) from a block point to the nearest solid cell (kind 2 or 4) within `reach`, else Infinity. */
export function solidDist(w: Walk, b: [number, number], reach = 1): number {
  const ci = Math.floor((b[0] - w.x0) / w.cell), cj = Math.floor((b[1] - w.y0) / w.cell), r = Math.ceil(reach / w.cell) + 1;
  let best = Infinity;
  for (let i = ci - r; i <= ci + r; i++) for (let j = cj - r; j <= cj + r; j++) {
    if (i < 0 || j < 0 || i >= w.nx || j >= w.ny) continue;
    const k = w.kind[i * w.ny + j]; if (k !== 2 && k !== 4) continue;
    const x0 = w.x0 + i * w.cell, y0 = w.y0 + j * w.cell;
    const dx = Math.max(x0 - b[0], 0, b[0] - (x0 + w.cell)), dy = Math.max(y0 - b[1], 0, b[1] - (y0 + w.cell));
    best = Math.min(best, Math.hypot(dx, dy));
  }
  return best;
}

/** The café chairs, block metres (handover.seats.json is block cm). */
export function loadChairs(file: string): [number, number][] {
  if (!existsSync(file)) return [];
  const s = JSON.parse(readFileSync(file, 'utf8'));
  return Object.values<any>(s.tables ?? {}).flatMap(t => (t.chairs ?? []).map((c: any) => [c.at[0] / 100, c.at[1] / 100] as [number, number]));
}

// ---------------------------------------------------------------- the row's files

export type FeetRow = { t: number; tick: number; key: string; x: number; y: number; z: number; floor: number; skip?: boolean };
export function readFeet(file: string): FeetRow[] {
  if (!existsSync(file)) return [];
  const [head, ...lines] = readFileSync(file, 'utf8').trim().split('\n'); const cols = (head ?? '').split(','), si = cols.indexOf('skip');
  return lines.map(l => { const c = l.split(','); return { t: +c[0]!, tick: +c[1]!, key: c[2]!, x: +c[3]!, y: +c[4]!, z: +c[5]!, floor: +c[6]!, ...(si >= 0 ? { skip: c[si] === '1' } : {}) }; });
}
export type MemRow = { t: number; managed: number; total: number };
export function readMem(file: string): MemRow[] {
  if (!existsSync(file)) return [];
  return readFileSync(file, 'utf8').trim().split('\n').slice(1).map(l => { const [t, m, tot] = l.split(',').map(Number); return { t: t!, managed: m!, total: tot! }; });
}

// ---------------------------------------------------------------- Q-SOLID

export function gradeSolid(a: { key: string; tick: number }, feet: FeetRow[], w: Walk | undefined, chairs: [number, number][]): Grade | undefined {
  if (!w) return undefined; // no walk file: the player's word stands
  const G = GRADE.solid, mine = feet.filter(r => r.key === a.key && r.tick >= a.tick - G.beforeTicks && r.tick <= a.tick + G.afterTicks);
  if (!mine.length) return undefined;
  const bs = mine.map(r => w.toBlock(r.x, r.z)), at = bs[mine.reduce((k, r, i) => Math.abs(r.tick - a.tick) < Math.abs(mine[k]!.tick - a.tick) ? i : k, 0)]!;
  if (!bs.some(b => onMap(w, b))) return { cause: 'off-map', severity: 'info', title: 'A figure off the walk map counted as inside a solid (the check, not the game)', owner: 'Suture', note: 'off the walk map: the check has nothing to measure there' };
  if (a.key.startsWith('crowd/') && chairs.some(c => Math.hypot(c[0] - at[0], c[1] - at[1]) <= G.seatR))
    return { cause: 'cafe-seat', severity: 'info', title: 'Café customers at their chairs counted as inside a solid (the check, not the game)', owner: 'Latch (check)', note: `within ${G.seatR} m of a café chair: seated, sitting down or getting up, inside the table set by design` };
  const near = Math.min(...bs.filter(b => onMap(w, b)).map(b => solidDist(w, b, 1))), expected = `no body (r ${G.bodyR} m) overlapping a drawn solid by more than ${G.overlapM * 100} cm`;
  const clip = near < G.bodyR - G.overlapM, cm = (m: number) => `${Math.round(m * 100)} cm`;
  if (!clip) return { cause: 'margin', severity: 'info', title: "A figure in the walk grid's clearance, clear of anything drawn (the check, not the game)", owner: 'Latch (check)', note: `nearest drawn solid ${Number.isFinite(near) ? cm(near) : 'over 1 m'} from the feet: the body (r ${cm(G.bodyR)}) clears it`, expected };
  const overlap = `the body overlaps a drawn solid by ${cm(G.bodyR - near)}`;
  if ((G.followers as readonly string[]).includes(a.key))
    return { cause: 'follower-offset', severity: 'S3', title: 'The rival crew and the response unit walk through trees, walls and steps', owner: 'Latch (host: followers drawn 0.9 m off the group pose)', note: `${overlap}; the host draws ${a.key} at a fixed 0.9 m offset from the group's one engine pose (HostViews.cs)`, expected };
  if (a.key.startsWith('crowd/')) {
    const run = insideRun(feet.filter(r => r.key === a.key && r.tick >= a.tick - G.legBackTicks && r.tick <= a.tick + G.legAheadTicks), w);
    if (run.s >= G.legInsideS)
      return { cause: 'straight-leg', severity: 'S3', title: 'People walk straight through buildings on a routine leg the engine drew as a straight line', owner: 'Keel (engine: Crowd.Leg, an end off the walk grid)', note: `${overlap}; ${a.key} was inside drawn solids for ${run.s.toFixed(1)} s (ticks ${run.from}-${run.to}): a leg with no grid route is a straight line`, expected };
    const me = mine.reduce((k, r) => Math.abs(r.tick - a.tick) < Math.abs(k.tick - a.tick) ? r : k, mine[0]!);
    const cast = feet.filter(r => r.tick === me.tick && (G.cast as readonly string[]).includes(r.key)).map(r => ({ key: r.key, d: Math.hypot(r.x - me.x, r.z - me.z) })).sort((p, q) => p.d - q.d)[0];
    if (cast && cast.d <= G.giveWayM)
      return { cause: 'give-way', severity: 'S3', title: 'People stepping aside for the cast are pushed into walls and onto one another', owner: 'Latch (host: HostViews.GiveWay)', note: `${overlap}; ${cm(cast.d)} from the ${cast.key}: the host steps the crowd 70 cm aside for the cast with no free-ground test`, expected };
  }
  return { cause: 'inside', severity: 'S3', title: 'A person walks through a drawn solid', owner: 'Pulse (paths) / Keel (walk grid)', note: overlap, expected };
}

/** The longest stretch (s) of a figure's samples (in tick order) with the feet inside a drawn solid cell. */
export function insideRun(rows: FeetRow[], w: Walk): { s: number; from: number; to: number } {
  let best = { s: 0, from: 0, to: 0 }, start: FeetRow | undefined;
  const sorted = [...rows].sort((p, q) => p.tick - q.tick);
  sorted.forEach((r, i) => {
    const b = w.toBlock(r.x, r.z), inside = onMap(w, b) && solidDist(w, b, 0.25) === 0;
    if (inside && !start) start = r;
    const last = !inside || i === sorted.length - 1;
    if (start && last) { const end = inside ? r : sorted[i - 1]!; if (end.t - start.t > best.s) best = { s: end.t - start.t, from: start.tick, to: end.tick }; start = undefined; }
  });
  return best;
}

// ---------------------------------------------------------------- Q-GIVER

/** The giver, its state and the way's word in a Q-GIVER observed text (`ring:`, `plan:`, `panel:` or `open-ring:<op> <state>[ <s> s]: ... <Word> on <kind>/<key>`). */
export function giverOf(observed: string): { src: string; op: string; state: string; word: string } | undefined {
  const m = /^([a-z-]+):(\w+) (down|carried|inside|in the car)(?: [\d.]+ s)?: (?:still offered for|offered for|a giver of|offered) (.+?) on \S+$/.exec(observed);
  return m ? { src: m[1]!, op: m[2]!, state: m[3]!, word: m[4]! } : undefined;
}
export function gradeGiver(a: { observed: string }): Grade | undefined {
  const g = giverOf(a.observed); if (!g) return undefined;
  if (g.state === 'in the car' && (GRADE.giver.fromCar as readonly string[]).includes(g.word.toLowerCase()))
    return { cause: 'go-from-car', severity: 'info', title: 'Go offered by an agent in the car (the check, not the game: Go is anyone\'s once someone is in it)', owner: 'Latch (check)', note: `the ${g.op} is aboard; GDD §06: Go is an order on the car by anyone once someone is in it`, expected: 'no giver who is down, carried or inside; in the car only for a way given from the car (Go)' };
  return undefined;
}

// ---------------------------------------------------------------- Q-MEM

export type MemVerdict = { judged: boolean; leak: boolean; floors: { total: number[]; managed: number[] }; growth: { total: number; managed: number }; windowS: number; note: string };
export function gradeMem(mem: MemRow[]): MemVerdict {
  const G = GRADE.mem, w = mem.filter(m => m.t > G.warmS), none = { total: [], managed: [] };
  if (w.length < 2) return { judged: false, leak: false, floors: none, growth: { total: 0, managed: 0 }, windowS: 0, note: `not judged: no samples after the ${G.warmS} s warm-up` };
  const t0 = w[0]!.t, t1 = w[w.length - 1]!.t, span = t1 - t0, third = span / 3;
  if (third < G.minThirdS) return { judged: false, leak: false, floors: none, growth: { total: 0, managed: 0 }, windowS: span, note: `not judged: ${Math.round(span)} s after the ${G.warmS} s warm-up, under the ${G.minThirdS * 3} s a judgement needs` };
  const part = (k: number) => w.filter(m => m.t >= t0 + k * third && (k === 2 ? m.t <= t1 : m.t < t0 + (k + 1) * third));
  const floor = (v: 'total' | 'managed') => [0, 1, 2].map(k => Math.min(...part(k).map(m => m[v])));
  const ft = floor('total'), fm = floor('managed'), perMin = third / 60;
  const g = (f: number[]) => Math.min(f[1]! - f[0]!, f[2]! - f[1]!) / perMin;
  const growth = { total: g(ft), managed: g(fm) }, leak = growth.total > G.totalMBMin || growth.managed > G.managedMBMin;
  const r = (x: number) => x.toFixed(1);
  return { judged: true, leak, floors: { total: ft, managed: fm }, growth, windowS: span, note: `floors by third (total ${ft.map(r).join(' → ')} MB, managed ${fm.map(r).join(' → ')} MB over ${Math.round(span)} s): held growth ${r(growth.total)} MB/min total, ${r(growth.managed)} MB/min managed` };
}

// ---------------------------------------------------------------- Q-FEET share

export function feetShare(feet: FeetRow[]): { n: number; within: number; share: number; ok: boolean } {
  const G = GRADE.feet, rows = feet.filter(r => r.skip === undefined ? r.y - r.floor < G.liftedM : !r.skip);
  const within = rows.filter(r => Math.abs(r.y - r.floor) <= G.tolM).length, share = rows.length ? within / rows.length : 1;
  return { n: rows.length, within, share, ok: share >= G.share };
}

// ---------------------------------------------------------------- Q-OFFER races

/** The doer and code of a Q-OFFER anomaly's observed text (`<op> <verb> <target>: <code> <why>`). */
export function offerOf(observed: string): { op: string; verb: string; code: string } | undefined {
  const m = /^(\w+) (\w+) (.*?): ([a-z-]+)(?: |$)/.exec(observed); return m ? { op: m[1]!, verb: m[2]!, code: m[4]! } : undefined; // the target may hold colons (camera:mast-z0)
}
/** A race: the doer went down in the order's tick or the `raceTicks` before it (log `<op>.down`). */
export function isRace(log: { tick: number; code: string }[], op: string, orderTick: number): boolean {
  const down = log.filter(l => l.code === `${op}.down` && l.tick <= orderTick).map(l => l.tick);
  return down.length > 0 && orderTick - Math.max(...down) <= GRADE.offer.raceTicks;
}

/** Plain words for a refusal code, for the issue titles. */
export const REFUSAL_WORDS: Record<string, string> = {
  'no-target': 'there is nothing there to act on any more', 'agent-busy': 'the agent was busy', 'agent-down': 'the agent was down', 'enforcer-inside': 'the Enforcer was inside',
  'in-car': 'the agent was in the car', 'too-far': 'it was too far', 'unwalkable': 'nobody can walk there', 'needs-routine': 'the routine was not known yet',
};
export const plainRefusal = (code: string) => REFUSAL_WORDS[code] ?? code.replace(/-/g, ' ');

export const SEATS_FILES = (buildApp: string, root: string) => [join(buildApp, 'Contents/Resources/Data/StreamingAssets/sim/handover.seats.json'), join(root, 'contracts/people/handover.seats.json')];
