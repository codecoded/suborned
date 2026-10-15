// Where a cast comes from: the promoted cast (held to its run, as `bun run people` does), a people run's accepted cast (runs/levels/<plan>/<run>), or a file.
// A run also brings its record: outcome, cost, rounds, Keel's verdict and open questions, and the hashes of the inputs it was pinned to.
import { existsSync, readFileSync } from 'node:fs';
import { basename, resolve } from 'node:path';
import { Cast } from '../../contracts/people/cast.schema';
import { sha256 } from '../gen/sha';
import { root } from '../rag/config';
import { castFileOf, checkPromotion } from '../people/promoted';

export type RoundInfo = { round: number; version: string; verdict: string; keelVerdict: string; faults: number; persons: number; checkerOk: boolean; faultTexts: { owner: string; rule: string; fault: string }[]; keelSummary: string };
export type RunInfo = {
  id: string; outcome: string; date: string; cost: { usd: number; calls: number }; maxUsd: number; approvedBy: string; commit: string; objective: string;
  continues?: { run: string; outcome: string; round: number; version: string };
  rounds: RoundInfo[]; final: { round: number; version: string; file: string };
  keel: { verdict: string; summary: string; questions: string[]; faults: { owner: string; rule: string; fault: string }[] };
  notes: string[];
  /** the recorded check results of the accepted round (checks.json) */
  recorded: { id: string; ok: boolean; value: string }[];
  /** the inputs the run was pinned to against the inputs now: filled in by the builder, which holds the current ones */
  pinned: { input: string; sha256: string }[];
  toolsHash: string; model: string;
};
export type Source = { kind: 'promoted' | 'run' | 'cast'; id: string; label: string; file: string; cast: Cast; castSha: string; run?: RunInfo; promotedFrom?: string };

const rd = (f: string) => readFileSync(resolve(root, f), 'utf8');
const readJson = (f: string) => JSON.parse(rd(f));
/** The opening of "Open questions, not faults: (1) …; (2) …" in Keel's summary, as the questions it lists; else none. */
export function questionsOf(summary: string): string[] {
  const at = summary.search(/open questions?/i); if (at < 0) return [];
  const tail = summary.slice(at).replace(/^open questions?[^:]*:\s*/i, '');
  const parts = tail.split(/\(\d+\)\s*/).map(s => s.trim().replace(/[;,]\s*$/, '').replace(/\.$/, '')).filter(Boolean);
  return parts.length ? parts : [tail.trim()];
}

export function runInfo(planId: string, id: string): { info: RunInfo; castFile: string; castObj: unknown } {
  const dir = `runs/levels/${planId}/${id}`;
  if (!existsSync(resolve(root, `${dir}/run.json`))) throw new Error(`no run ${id}: ${dir}/run.json is missing`);
  const run = readJson(`${dir}/run.json`);
  const rounds: RoundInfo[] = (run.rounds as any[]).map(r => {
    const rd2 = `${dir}/round-${r.round}`, keel = existsSync(resolve(root, `${rd2}/keel.json`)) ? readJson(`${rd2}/keel.json`) : undefined;
    return { round: r.round, version: r.version, verdict: r.verdict, keelVerdict: r.keelVerdict, faults: r.faults, persons: r.persons, checkerOk: !!r.checkerOk, keelSummary: keel?.summary ?? '', faultTexts: (keel?.faults ?? []).map((f: any) => ({ owner: f.owner, rule: f.rule, fault: f.fault })) };
  });
  // the accepted output is the last round that ran Pulse (a round that reused its cast has no pulse.json of its own)
  let last = rounds.length;
  while (last > 0 && !existsSync(resolve(root, `${dir}/round-${last}/pulse.json`))) last--;
  if (!last) throw new Error(`run ${id} has no round with a pulse.json`);
  const file = `${dir}/round-${last}/pulse.json`, out = readJson(file);
  const keelNow = rounds[rounds.length - 1];
  const recorded = existsSync(resolve(root, `${dir}/round-${rounds.length}/checks.json`)) ? (readJson(`${dir}/round-${rounds.length}/checks.json`).checks as any[]).map(c => ({ id: c.id, ok: !!c.ok, value: String(c.value) })) : [];
  const pinned = [
    ['plan', run.inputPlan?.rawHash], ['block', run.inputBlock?.sha256], ['life', run.inputLife?.sha256], ['rules', run.inputRules?.sha256], ['mission', run.inputMission?.sha256],
  ].filter(([, h]) => h).map(([input, h]) => ({ input: input as string, sha256: h as string }));
  const info: RunInfo = {
    id, outcome: run.outcome, date: run.date, cost: run.cost ?? { usd: 0, calls: 0 }, maxUsd: run.maxUsd ?? 0, approvedBy: run.approvedBy ?? '', commit: run.commit ?? '', objective: run.objective ?? '',
    ...(run.continues ? { continues: { run: run.continues.run, outcome: run.continues.outcome, round: run.continues.round, version: run.continues.version } } : {}),
    rounds, final: { round: last, version: rounds[last - 1]?.version ?? run.finalVersion, file },
    keel: { verdict: keelNow?.keelVerdict ?? '', summary: keelNow?.keelSummary ?? '', questions: questionsOf(keelNow?.keelSummary ?? ''), faults: keelNow?.faultTexts ?? [] },
    notes: (out.notes ?? []).map(String), recorded, pinned, toolsHash: run.toolsHash ?? '', model: ((run.calls as any[]) ?? []).map(c => c.model).find(Boolean) ?? '',
  };
  return { info, castFile: file, castObj: out.cast };
}

/** `spec`: 'promoted', a run id (r015), or a path to a cast file (a bare Cast, or a round's output with a `cast` in it). */
export function loadSource(planId: string, spec: { run?: string; cast?: string } | 'promoted' | string): Source {
  const o = typeof spec === 'string' ? (spec === 'promoted' ? {} : /^r\d{3}$/.test(spec) ? { run: spec } : { cast: spec }) : spec;
  if (o.cast) {
    const raw = readJson(o.cast), cast = Cast.parse(raw && raw.cast && !raw.rows ? raw.cast : raw);
    return { kind: 'cast', id: basename(o.cast).replace(/\.json$/, ''), label: `cast file ${o.cast}`, file: o.cast, cast, castSha: sha256(JSON.stringify(cast)) };
  }
  if (o.run) {
    const { info, castFile, castObj } = runInfo(planId, o.run), cast = Cast.parse(castObj);
    return { kind: 'run', id: o.run, label: `run ${o.run} round ${info.final.round}, ${info.outcome}`, file: castFile, cast, castSha: sha256(JSON.stringify(cast)), run: info };
  }
  const promo = checkPromotion(planId);
  if (!promo.ok) throw new Error(`the promoted cast ${promo.label} fails: ${promo.problems.join('; ')}`);
  const cast = Cast.parse(JSON.parse(promo.castText!));
  return { kind: 'promoted', id: 'promoted', label: `promoted ${promo.label}`, file: castFileOf(planId), cast, castSha: sha256(JSON.stringify(cast)), promotedFrom: promo.record!.source.run };
}
