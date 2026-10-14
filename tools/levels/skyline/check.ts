// The plan checker on a skyline candidate: the plan rules (strict) and the world rules, on the world generated in
// process from the run's pinned kit digest (checker.ts). The report is what checks.json records and what replay reruns.
import type { Check } from '../../plan/checks';
import { type Plan, type Stop, portalsOf } from '../../plan/plan';
import { rule } from '../../plan/world-ctx';
import type { Pinned } from '../../plan/world-rules';
import { inProcess, ownerOf } from '../checker';
import type { Owner } from './apply';

/**
 * The pseudo-rule a candidate fails when the generator cannot build it: recorded in checks.json, judged on replay like
 * any rule.
 */
export const BUILD_RULE = 'GEN-1';

export type Station = { line: string; id: string; name: string; chainage: number; next?: number };
export type Checked = {
  ok: boolean; strict: true; checks: Check[]; stations: Station[];
  /** The candidate could not be built into a world (the message); no rule ran. */
  error?: string;
};

const SK3_SOURCE = 'standards.skyrail.portal (a signature or megastructure declares where the beam runs through it) · '
  + 'brief 007 F: a station labelled in a structure it does not stand in passed every rule';

/** Does the portal `portalId` (`signatures/<id>`, or `<megastructure>/<portal>`) belong to the structure a station names? */
const belongsTo = (named: string, portalId: string): boolean =>
  named.startsWith('megastructures/') ? portalId.startsWith(`${named.slice('megastructures/'.length)}/`) : portalId === named;

/**
 * SK-3: a station labelled `in` a structure stands inside a portal that structure declares on the station's own line
 * (x and y inside the portal's rect). The label is what the world builds the station from (decks only, the structure is
 * the roof), and every other rule holds whether it is true or not.
 */
export function stationPortalCheck(plan: Plan): Check {
  const portals = portalsOf(plan), bad: string[] = [];
  let total = 0;
  for (const line of plan.skyrail) {
    for (const st of (line.stations ?? []) as (Stop & { in?: string })[]) {
      if (!st.in) continue;
      total++;
      const own = portals.filter(q => belongsTo(st.in!, q.id) && q.line === `${line.id}/${st.path}`);
      const inside = own.some(q => st.at[0] >= q.rect[0] && st.at[0] <= q.rect[1] && st.at[1] >= q.rect[2] && st.at[1] <= q.rect[3]);
      if (inside) continue;
      bad.push(`${line.id}/${st.id} at ${st.at.join(', ')} is labelled in ${st.in}, `
        + (own.length ? `outside its portal${own.length > 1 ? 's' : ''} ${own.map(q => `${q.id} x ${q.rect[0]}–${q.rect[1]}, y ${q.rect[2]}–${q.rect[3]}`).join(' / ')}`
          : `which declares no portal on ${line.id}/${st.path}`));
    }
  }
  return rule('SK-3', 'Skyline', 'A station labelled in a structure stands inside that structure\'s declared portal on its line',
    total, bad, 'all', SK3_SOURCE);
}

/**
 * Runs every rule on the candidate. A candidate the generator cannot build (a site the world cannot make) is not a
 * crash of the run: it is a failed rule with the reason, back to its owner.
 */
export async function checkCandidate(plan: Plan, block: unknown, pin: Pinned = {}): Promise<Checked> {
  try {
    const R = await inProcess(plan, true, block, { world: true, ...pin });
    R.checks.push(stationPortalCheck(plan));
    return { ok: R.checks.every(c => c.ok), strict: true, checks: R.checks, stations: R.stations };
  } catch (e) {
    const error = e instanceof Error ? e.message : String(e);
    const build: Check = {
      id: BUILD_RULE, group: 'Candidate', rule: 'The candidate builds into a world',
      value: 'failed', limit: 'builds', ok: false, detail: error,
    };
    return { ok: false, strict: true, checks: [build], stations: [], error };
  }
}

/**
 * Rules about a station standing and reaching the street are fixed by the sky rail (Datum) or the station (Vantage).
 */
const BOTH = new Set(['N-R1', 'L-5', 'L-7', 'SF-13', 'W-2', 'W-3', 'SK-3']);

/**
 * Who hears a failed rule: the stations' own rules are Vantage's, the rules both can fix go to both, the rest is
 * Datum's.
 */
export function ownersOf(rule: string): Owner[] {
  if (BOTH.has(rule)) return ['Datum', 'Vantage'];
  return [/^(T-S|S-S|S-I|N-P|N-C)/.test(rule) ? ownerOf(rule) : 'Datum'];
}

/** Groups whose passing rules the critic reads in full (their measured values bear on the skyline). */
const DETAIL = new Set(['Skyline', 'Sky rail', 'Transit', 'Levels', 'Walking']);
const line = (x: Check) => `${x.ok ? 'PASS' : 'FAIL'} ${x.id} ${x.rule}: ${x.value} (${x.limit})`
  + `${x.detail ? ' — ' + x.detail : ''}${x.source ? ` [source: ${x.source}]` : ''}`;

/**
 * The checker's report as the critic reads it: every failed rule and the rules of the groups above in full, other
 * passes by id.
 */
export function reportText(c: Checked, problems: string[]): string {
  const found = problems.map(p => `PROBLEM ${p}`).join('\n');
  if (c.error) return `Plan checker could not run: the candidate could not be built (${c.error})\n${found}`;
  const full = c.checks.filter(x => !x.ok || DETAIL.has(x.group));
  const rest = c.checks.filter(x => x.ok && !DETAIL.has(x.group));
  return [...full.map(line), `PASS ${rest.length} more rules: ${[...new Set(rest.map(x => x.id))].join(' ')}`, found]
    .filter(Boolean).join('\n');
}

export const stationTable = (c: Checked): string => c.stations.map(s => {
  const next = s.next === undefined ? '—' : `${Math.round(s.next)} m`;
  return `${s.line}/${s.id} ${s.name} chainage ${Math.round(s.chainage)} m, to next ${next}`;
}).join('\n');
