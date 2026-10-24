// Brief 060 tier 2 (Suture, 2026-10-25): the engine-side checks on the bot lane's rows (tools/qa/checks/human.ts). Each row's whole session
// (<row>/session.script.json, BotRun) replays exactly on the build engine, so the checks read the engine's own log, refusals and ledger for what the
// built player did. A human row also carries what the bot meant (row.human.events): a stop it meant is the player's doing, one it did not is the
// game's. The other policies order busy agents and stale targets on purpose, so they get only the checks that hold whatever the player does
// (Q-LEAK, Q-STALE-RING). Findings ride as graded anomalies, so the report, the issue book and the replay command treat them as the player's.
//
// Q-BOT-STALL (S3): a row that took wedges but gave the game almost no orders tested nothing for most of its run (2026-10-25 nightly: greedy-a
// took 168 wedges and the engine got 4 orders. It ranks Go first, Go with Vale outside opens a card that would fail the mission, the bot backs
// off, and ranks Go first again). Fewer orders than a fifth of the wedges taken, over at least 20 wedges.
//
// Q-HUMAN-RATE (info): how far a human row's indecision was from Sky's profile (tools/qa/human-profile.json): each kind's count against
// steps x its rate. It tells Latch the tape plays as drawn; it never reaches Sky's list.
import { existsSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { humanChecks, qLeak, qStaleRing } from './checks/human';
import { clock } from './checks/warn';
import type { BotAnomaly, BotReportRow } from './bot';
import { lane, playStream } from './sim';
import type { Finding, Play } from './types';

export const SESSION_FILE = 'session.script.json';
/** The rate a human event kind is drawn at (tools/qa/human-profile.json `values`). */
export const KIND_RATE: Record<string, string> = { cancel: 'cancelProb', reorder: 'reorderProb', repeat: 'repeatProb', switch: 'switchProb', idle: 'idleProb', abandon: 'abandonProb', wander: 'abandonProb', misclick: 'misclickProb', 'ring-close': 'ringOpenCloseProb', turn: 'cameraTurnProb', wrong: 'wrongVerbProb', peek: 'thirdPersonProb' };

/** A finding as a graded anomaly of its row (findingsOf keys it as `${check}:${grade.cause}`). */
export function asAnomaly(f: Finding, script: string): BotAnomaly {
  return { check: f.check, key: f.key, title: f.title, expected: f.expected, observed: f.observed, tick: f.tick ?? 0, clock: f.tick !== undefined ? clock(f.tick) : '', at: [0, 0], shot: '', script,
    grade: { cause: f.key.slice(f.check.length + 1), severity: f.severity, title: f.title, owner: f.owner, note: '' } };
}

/** The checks for one row's replayed session. */
export function rowFindings(row: BotReportRow, p: Play): Finding[] {
  if (row.policy === 'human') return humanChecks(p, { seed: 'bot', mutated: false, events: (row.human?.events ?? []).map(e => ({ ...e })) });
  return [...qLeak(p, { seed: 'bot', mutated: false }), ...qStaleRing(p, { seed: 'bot', mutated: false })];
}

/** Q-BOT-STALL: wedges taken, few orders given. */
export function stallFindings(row: BotReportRow): Finding[] {
  const taken = row.input?.taken ?? 0, given = row.orders?.given ?? 0;
  if (taken < 20 || given >= taken / 5) return [];
  return [{ check: 'Q-BOT-STALL', severity: 'S3', key: `Q-BOT-STALL:${row.policy}`, owner: 'Latch (BotRun policy)', title: `A bot row stalled: it took ${taken} wedges and the game got ${given} orders`,
    expected: 'most wedges a bot takes reach the engine as orders (or are refused there, counted by code)', observed: `${row.name} (${row.policy}, seed ${row.seed}): ${taken} wedges, ${given} orders, ${row.orders?.refused ?? 0} refused, ${row.outcome}${row.failCode ? ' (' + row.failCode + ')' : ''} at tick ${row.endTick}` }];
}

/** Q-HUMAN-RATE: each indecision kind's count on a human row against steps x the profile's rate (info; off by more than x2.5 either way). */
export function rateFindings(row: BotReportRow, values: Record<string, unknown>): Finding[] {
  const h = row.human; if (!h || !h.steps) return [];
  const out: Finding[] = [];
  for (const [kind, field] of Object.entries(KIND_RATE)) {
    const rate = values[field]; if (typeof rate !== 'number') continue;
    const want = rate * h.steps, got = Object.entries(h.counts ?? {}).filter(([k]) => k.toLowerCase() === kind).reduce((a, [, n]) => a + n, 0);
    if (want < 3 && got < 3) continue;
    if (got >= want / 2.5 && got <= want * 2.5) continue;
    out.push({ check: 'Q-HUMAN-RATE', severity: 'info', key: `Q-HUMAN-RATE:${kind}`, owner: 'Latch (BotRun.Human)', title: `The human row's ${kind}s were far from Sky's rate`,
      expected: `about ${want.toFixed(1)} over ${h.steps} steps (${field} ${rate})`, observed: `${got} on ${row.name} (seed ${row.seed}, ${h.routes.join(' > ')})` });
  }
  return out;
}

export type SessionCheckOpts = { say?: (m: string) => void; profileFile?: string; /** tests stand in for the build engine */ replay?: (scripts: string[]) => Promise<Play[]> };

async function replayOnBuild(scripts: string[], dir: string): Promise<Play[]> {
  const l = lane('build'), list = join(dir, 'session-list.txt'), plays: Play[] = [];
  writeFileSync(list, scripts.join('\n') + '\n');
  try { await playStream(l, list, (p: Play, i: number) => { plays[i] = p; }, { replay: false }); } finally { rmSync(list, { force: true }); }
  return plays;
}

/** Replays each row's session on the build engine and adds the checks' findings to the row's anomalies (graded). A row without a session keeps
 *  what it has; a replay that fails is said, not a finding. */
export async function sessionChecks(rows: BotReportRow[], outDir: string, o: SessionCheckOpts = {}): Promise<void> {
  const say = o.say ?? (() => {}), withS = rows.filter(r => existsSync(join(outDir, r.name, SESSION_FILE)));
  let values: Record<string, unknown> = {};
  try { values = JSON.parse(readFileSync(o.profileFile ?? join(import.meta.dir, 'human-profile.json'), 'utf8')).values ?? {}; } catch { /* no profile: no rate check */ }
  for (const r of rows) for (const f of [...stallFindings(r), ...rateFindings(r, values)]) r.anomalies.push(asAnomaly(f, ''));
  if (!withS.length) return;
  let plays: Play[];
  try { plays = await (o.replay ?? (s => replayOnBuild(s, outDir)))(withS.map(r => join(outDir, r.name, SESSION_FILE))); }
  catch (e) { say(`qa bot: the session replays did not run (${(e as Error).message.split('\n')[0]}): no engine-side checks on the bot rows`); return; }
  withS.forEach((r, i) => {
    const p = plays[i]; if (!p || p.error) { say(`qa bot: ${r.name}: session replay failed${p?.error ? `: ${p.error}` : ''}`); return; }
    for (const f of rowFindings(r, p)) r.anomalies.push(asAnomaly(f, join(r.name, SESSION_FILE)));
  });
}
