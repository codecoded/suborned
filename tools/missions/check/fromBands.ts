// The §09 conditions read the bands (brief 080). A reference route is no longer one fixed-tick script but an adaptive plan across seeds, so judge.ts gets, for each manifest
// entry with `adaptive`, one Digest that stands for the plan: an outcome from its band's floor, medians of the wins for its prices and marks, and across all wins what must
// hold on every seed (MC-14's Go after blocker.out, MC-11's holds, MC-15's crew). judge.ts stays one judge, with its failing examples, for scripts and plans alike.
import type { Routes } from '../../../contracts/missions/routes.schema';
import { ROUTE_FLOOR, wentLoud, type BandReport, type Row } from './bands';
import { boxCrewHold, wayFreeAt } from './blocker';
import type { Script } from './engine';
import type { Digest, Plays, Slack } from './judge';

const med = (v: number[]) => { const s = [...v].sort((a, b) => a - b); return s.length ? s[Math.floor(s.length / 2)]! : 0; };
/** A plan's reports may be a stray seed's: MC-12 holds while under this share of a plan's runs has a watcher report a loiterer. [S] Motive to confirm. */
export const LOITER_SHARE = 0.1;
/** The same reading for MC-11's holds. [S] Motive to confirm. */
export const STRAY_SHARE = 0.1;
/** The win share a reference needs to count as winning for the §09 conditions: its own band's floor (bands.ts). */
const floorOf = (planId: string, style: string) => style === 'prepared-loud' ? 50 : ROUTE_FLOOR[planId] ?? 50;

/** One Digest for a plan across its seeds. */
export function digestOfPlan(rows: Row[], replayIdentical: boolean): Digest {
  const r0 = rows[0]!, loud = r0.style.endsWith('loud');
  const wins = rows.filter(r => r.outcome === 'success' && (!loud || wentLoud(r)));
  const pct = wins.length * 100 / rows.length, ok = pct >= floorOf(r0.plan, r0.style), pool = wins.length ? wins : rows;
  const fails = new Map<string, number>(); for (const r of rows) if (r.outcome === 'failure') fails.set(r.fail, (fails.get(r.fail) ?? 0) + 1);
  const worst = [...fails].sort((a, b) => b[1] - a[1])[0]?.[0] ?? 'none';
  const home = pool.filter(r => !('captured' in r.marks)), raced = loud && home.length ? home : pool; // MC-6 races the loud win that leaves nobody captured
  const n = (f: (r: Row) => number) => med(pool.map(f)), codes = new Set(pool.flatMap(r => Object.keys(r.marks))), marks: Record<string, number> = {};
  for (const c of codes) { const v = pool.filter(r => c in r.marks); if (c === 'attention.reporting' && rows.filter(r => c in r.marks).length < LOITER_SHARE * rows.length) continue; if (v.length * 2 >= pool.length || c === 'rivals.wiped') marks[c] = med(v.map(r => r.marks[c]!)); }
  if (loud && home.length) delete marks['captured'];
  const seal = pool.filter(r => 'seal.starts' in r.marks).length * 2 >= pool.length; if (!seal) delete marks['seal.starts'];
  const free = wins.map(r => { const at = wayFreeAt(r.marks); return at === undefined ? undefined : (r.tick - at) / 10; }), someNoBlocker = free.some(x => x === undefined); // engine 0.43.0 reading (Waymark): car.unboxed under the block, someNoBlocker = free.some(x => x === undefined);
  // MC-11 over seeds: a hold that didn't come for the car counts once it's more than a stray seed's (as judge.ts reads a hold: an answer to fire, or a walk to a post the mission places)
  const badHold = (r: { marks: Record<string, number> }) => (h: { how: string; who: string; tick: number }) => h.how === 'answer' || (h.how === 'post' && /^(rival|officer)-\d+$/.test(h.who) && !boxCrewHold(r.marks, h));
  const heldShare = rows.filter(r => (r.held ?? []).some(badHold(r))).length / rows.length;
  const held = heldShare >= STRAY_SHARE ? rows.flatMap(r => (r.held ?? []).filter(badHold(r))) : [];
  const seen = new Set<string>(), firstHeld = held.filter(h => { const k = `${h.how} ${h.who}`; if (seen.has(k)) return false; seen.add(k); return true; });
  const camp = n(r => r.campaignTrail ?? 0), cases = n(r => r.openCases ?? 0);
  return {
    outcome: ok ? 'success' : 'failure', fail: ok ? '' : worst, tick: med(raced.map(r => r.tick)), cash: n(r => r.cash), trail: n(r => r.trail), alert: n(r => r.alert), gunfire: n(r => r.gunfire), startHash: r0.startHash, finalHash: r0.finalHash, replayIdentical,
    rejected: 0, scriptSha: `adaptive:${r0.plan}`, marks, shots: loud ? 1 : 0, fight: loud ? 'loud' : 'safe',
    ...(camp ? { campaignTrail: camp } : {}), standing: n(r => r.standing + (r.campaignStanding ?? 0)), ...(cases ? { openCases: cases } : {}),
    ...('takedown.out' in marks ? { takedownS: 6 } : {}),
    ...(wins.length && !someNoBlocker ? { carFreeS: Math.min(...free.map(x => x!)) } : {}),
    ...(firstHeld.length ? { held: firstHeld } : {}),
  };
}

/** The plan's rules as the judge reads a script's orders (does it turn, does it take someone down), and which contacts it works through. */
export type PlanFile = { id: string; rules: { do: string; target?: string; approach?: string }[] };
export const scriptOfPlan = (p: PlanFile, mission: string): Script => ({ schema: 'suborned.sim.script/1', mission: mission as 'handover', start: {}, until: 0, orders: p.rules.map(r => ({ tick: 0, do: r.do, ...(r.target ? { target: r.target } : {}) })) });

/** Plays and scripts for the manifest's adaptive entries; a contact the plan works through is one its route can't win without (MC-4). And the windows, as slack (MC-5's report). */
export function fromBands(m: Routes, b: BandReport, plans: Record<string, PlanFile>): { plays: Plays; scripts: Record<string, Script>; slack: Slack[]; missing: string[] } {
  const plays: Plays = {}, scripts: Record<string, Script> = {}, missing: string[] = [], roles = m.contacts.map(c => c.role), rows = b.rows ?? [], replay = b.rigid.find(l => l.id === 'R-5')?.ok ?? false;
  for (const s of m.scripts.filter(x => x.adaptive && !x.pending)) {
    const v = rows.filter(r => r.plan === s.adaptive), p = plans[s.adaptive!];
    if (!v.length || !p) { missing.push(`${s.id}: no rows for plan ${s.adaptive}`); continue; }
    plays[s.id] = digestOfPlan(v, replay); scripts[s.id] = scriptOfPlan(p, m.mission);
    for (const role of roles) if (p.rules.some(r => r.target === role && ['watch', 'listen', 'turn', 'call'].includes(r.do))) plays[`${s.id}~no-${role}`] = { ...plays[s.id]!, outcome: 'failure', fail: `needs-${role}`, carFreeS: undefined };
  }
  const slack: Slack[] = m.scripts.filter(x => x.adaptive && x.primary && x.kind === 'quiet').map(x => { const w = b.windows.filter(y => y.plan === x.adaptive); return { script: x.id, maxS: w.length && w.every(y => y.holds) ? 30 : 0, steps: w.map(y => ({ tick: 0, orders: y.rule, slackS: y.holds ? 30 : 0 })) }; });
  return { plays, scripts, slack, missing };
}
/** Fail lines the plans reach across seeds: they count for MC-3 beside the scripts. */
export const failsReached = (rows: Row[]) => [...new Set(rows.filter(r => r.outcome === 'failure').map(r => r.fail))].sort();
