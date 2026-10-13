// Continuing a run (brief 007 F): a new run whose round 1 starts from a run's last completed round, its candidate proposals and its open faults,
// not from the base plan. The run was REJECTED, or it stopped short (ERROR, INCOMPLETE: a crash, a kill, the spend ceiling) after a round that
// completed. It is what a 4th round would have done: builders before the earliest faulted one are reused as they were, the rest are
// asked again with their previous proposal and the faults on it. What was inherited is written into the new run's folder (inherited.json, its
// sha256 in run.json under `continues`), so the new run replays offline on its own; the parent has to be on disk only when the run starts.
import { resolve } from 'node:path';
import { crewOf, feedbackLine } from './crew';
import { inferGdd } from './gdd';
import { runsRoot } from './paths';
import type { Fault, Member, Plugin } from './plugin';
import { sha256 as sha } from './sha';

export const INHERITED = 'inherited.json';
/** The outcomes of a run that can be continued from its last completed round. */
export const CONTINUABLE = ['REJECTED', 'ERROR', 'INCOMPLETE'] as const;
export type Continuable = (typeof CONTINUABLE)[number];
/** A run that must not start; the message says why. Nothing has been written. */
export class RunRefused extends Error {}
/** The caller asked for something a continuation cannot do; the message says what to do instead. */
export class ContinueRefused extends RunRefused {}

export type Inherited = {
  parent: { plan: string; run: string; outcome: Continuable; round: number; version: string; runSha256: string; gdd?: string; chunker: number };  // gdd, chunker: what the inherited calls' passages and cards were made from
  proposals: Record<string, unknown>;                // builder key → its output in the parent's last round (a builder skipped that round has none)
  calls: Record<string, Record<string, unknown>>;    // builder key → the call record that produced it: its retrieval is what citations are checked against
  faults: Fault[];                                   // the parent's last round's faults, as recorded
};
/** What run.json records about the link. */
export type Continues = Inherited['parent'] & { file: string; sha256: string; sameInputs: boolean };

const refusal = (runId: string, outcome: string, error?: string) => {
  if (outcome === 'ACCEPTED') return `${runId} is ACCEPTED: there is nothing to repair. Only a run that did not end ACCEPTED can be continued.`;
  if (outcome === 'ERROR') return `${runId} ended in ERROR (${String(error ?? '').slice(0, 160)}): no round of it completed, so there is nothing to continue from. Start a fresh run.`;
  if (outcome === 'INCOMPLETE') return `${runId} is INCOMPLETE (killed or stopped before its first round completed): there is nothing to continue from. Start a fresh run.`;
  return `${runId} has outcome ${outcome}; only a REJECTED, ERROR or INCOMPLETE run can be continued.`;
};

/** Reads the run `runId` of `planId` from runs/ and takes its last completed round: the proposals, the calls behind them, the open faults. Throws ContinueRefused. */
export async function loadParent(p: Plugin, planId: string, runId: string): Promise<{ inherited: Inherited; parentRun: any }> {
  const dir = resolve(runsRoot(), planId, runId);
  const text = await Bun.file(`${dir}/run.json`).text().catch(() => undefined);
  if (text === undefined) throw new ContinueRefused(`cannot continue ${runId}: there is no ${planId}/${runId} in ${runsRoot()}`);
  const run = JSON.parse(text);
  if (run.objective !== p.id) throw new ContinueRefused(`${runId} belongs to the ${run.objective} objective; this is the ${p.id} objective`);
  if (!(CONTINUABLE as readonly string[]).includes(run.outcome)) throw new ContinueRefused(refusal(runId, run.outcome, run.error));
  const last = (run.rounds ?? []).at(-1);
  if (!last) throw new ContinueRefused(run.outcome === 'REJECTED' ? `${runId} has no completed round` : refusal(runId, run.outcome, run.error));
  if (last.verdict === 'pass') throw new ContinueRefused(`${runId}: its last completed round (${last.round}) passed, so there is nothing to repair`);
  const rd = `${dir}/round-${last.round}`, skipped: string[] = last.skipped ?? [];
  const read = async (file: string) => Bun.file(`${rd}/${file}`).json().catch(() => undefined);

  const proposals: Inherited['proposals'] = {}, calls: Inherited['calls'] = {};
  for (const b of p.builders.filter(b => !skipped.includes(b.key))) {
    const out = await read(`${b.key}.json`), call = await read(`${b.key}.call.json`);
    if (out === undefined || call === undefined) throw new ContinueRefused(`${runId} round ${last.round}: ${b.key}.json or ${b.key}.call.json is missing`);
    const parsed = p.replay.schema(run, b.key).safeParse(out);
    if (!parsed.success) throw new ContinueRefused(`${runId} round ${last.round}: ${b.key}.json fails its schema (${parsed.error.message.slice(0, 120)})`);
    const { reused: _reused, ...record } = call;     // a builder reused in the parent's last round was last asked in an earlier round
    proposals[b.key] = out; calls[b.key] = record;
  }
  const faults: Fault[] | undefined = await read('faults.json');
  const at = `${runId} round ${last.round}`;
  if (!Array.isArray(faults)) throw new ContinueRefused(`${at}: faults.json is missing, so its open faults are unknown`);
  if (typeof last.faults === 'number' && faults.length !== last.faults) {
    throw new ContinueRefused(`${at}: faults.json holds ${faults.length} fault(s), run.json says ${last.faults}`);
  }
  const hitSources = Object.values(calls).flatMap(call => ((call.retrieval as any)?.hits ?? []).map((h: any) => h.source as string));
  const gdd = (run.gdd ?? inferGdd(hitSources))?.version as string | undefined;
  const parent = { plan: planId, run: runId, outcome: run.outcome as Continuable, round: last.round as number, version: last.version as string, runSha256: sha(text), gdd, chunker: Number(run.chunker ?? 1) };
  return { inherited: { parent, proposals, calls, faults }, parentRun: run };
}

/** Writes inherited.json into the new run and returns what run.json records. `record`: the new run's pinned-input fields, compared with the parent's. */
export async function pinInherited(inh: Inherited, parentRun: any, rdir: string, record: Record<string, unknown>): Promise<Continues> {
  const text = JSON.stringify(inh, null, 2) + '\n';
  await Bun.write(`${rdir}/${INHERITED}`, text);
  const sameInputs = Object.keys(record).every(k => Bun.deepEquals(parentRun[k], record[k]));
  return { ...inh.parent, file: INHERITED, sha256: sha(text), sameInputs };
}

/** Where round 1 starts: the earliest builder with a fault on it, or with nothing to reuse (skipped in the parent's last round). Builders before it are reused. */
export function rerunOf(inh: Inherited, builders: Member[]): number {
  const crew = crewOf(builders);
  const faulted = inh.faults.map(f => crew.keyOf(f.owner)).filter((k): k is string => !!k);
  const unanswered = builders.filter(b => !(b.key in inh.proposals)).map(b => b.key);
  return crew.rerunFrom(new Set([...faulted, ...unanswered]));
}

/** Round 1's starting state: the faults (their passage ids kept only if the corpus still has them), each builder's feedback, and where the round starts. */
export function startFrom(inh: Inherited, builders: Member[], keys: Set<string>) {
  const crew = crewOf(builders), feedback: Record<string, string[]> = {};
  const faults = inh.faults.map(f => ({ ...f, chunks: f.chunks.filter(c => keys.has(c)) }));
  for (const f of faults) {
    const key = crew.keyOf(f.owner);
    if (key) (feedback[key] ??= []).push(feedbackLine(f));
  }
  return { faults, feedback, rerun: rerunOf(inh, builders) };
}
