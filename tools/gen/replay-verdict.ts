// Replay: what a round's verdict rests on, recomputed from the files of the round. run.json is read for the numbers it
// claims and compared; the verdict and the outcome come from the recomputation, never from what the record says.
import type { Check, Plugin, Rerun } from './plugin';
import { criticRuns } from './rules';

type Recorded = { owner: string; rule: string; fault: string };
type Keel = { verdict: 'pass' | 'fail'; faults: Recorded[] };
export type Verdict = 'pass' | 'fail';
/** A round as run.json records it: the engine's fields, and the objective's own besides. */
export type RoundRec = {
  round: number; verdict: Verdict; keelVerdict: Verdict | null; faults: number;
  criticRan?: boolean; assessFaults?: number; citeFaults?: number; handoffFaults?: number;
  [objective: string]: unknown;
};
export type RoundFacts = {
  c: Check; p: Plugin; r: RoundRec; n: string; loop: number;
  /** The run was made before inputs were pinned: its checks cannot be rerun, whatever the reason. */
  superseded: boolean; rerun: Rerun | undefined;
};

/** The fields a loop-2 round must carry: a round that leaves one out would move onto the older, weaker path. */
const LOOP2_KEYS = ['criticRan', 'assessFaults', 'citeFaults', 'handoffs'] as const;

const quiet = (c: Check, file: string) => Bun.file(`${c.dir}/${file}`).json().catch(() => undefined);

/** Loop 2: was Keel called exactly when the round's facts say he should be? */
function gate(f: RoundFacts, checksOk: boolean): void {
  const { c, r, n, loop } = f;
  if (loop < 2) {
    if (r.criticRan === false) c.fail(`${n}: criticRan is false in a loop-${loop} run, where Keel judged every round`);
    return;
  }
  const recomputed = f.rerun?.assessFaults; // apply faults, recomputed from the proposals when the objective can
  if (recomputed !== undefined) {
    if (recomputed === (r.assessFaults ?? 0)) c.ok(`${n}: apply faults match the recomputation (${recomputed})`);
    else c.fail(`${n}: apply faults recorded ${r.assessFaults ?? 0}, recomputed ${recomputed}`);
  }
  const want = criticRuns(loop, {
    checksOk, assessFaults: recomputed ?? r.assessFaults ?? 0, citeFaults: r.citeFaults ?? 0,
    handoffFaults: r.handoffFaults ?? 0,
  });
  const consistent = r.criticRan === want && (want ? r.keelVerdict !== null : r.keelVerdict === null);
  const said = want ? 'called on a candidate that passed the checks' : 'not called, the candidate failed the checks';
  if (consistent) c.ok(`${n}: Keel ${said}`);
  else c.fail(`${n}: criticRan ${r.criticRan}, keelVerdict ${r.keelVerdict}; the recorded checks give ${want}`);
}

/**
 * The round's verdict, recomputed: pass only if the checks passed, Keel was called and passed, and no fault is on
 * record. Every claim run.json makes about it is compared: the fault count, the verdict, Keel's verdict, and that the
 * faults Keel listed are the ones faults.json carries.
 */
export async function roundVerdict(f: RoundFacts): Promise<Verdict> {
  const { c, p, r, n, loop } = f;
  if (loop >= 2) {
    for (const k of LOOP2_KEYS) if (!(k in r)) c.fail(`${n}: a loop-${loop} round records ${k}, this one does not`);
  }
  const criticSkipped = loop >= 2 && r.criticRan === false;
  const keel: Keel | undefined = criticSkipped ? undefined : await quiet(c, `${n}/${p.critic.key}.json`);
  const faults: Recorded[] | undefined = await quiet(c, `${n}/faults.json`);
  if (!Array.isArray(faults)) c.fail(`${n}: faults.json is missing`);
  const listed = Array.isArray(faults) ? faults : [];
  if (Array.isArray(faults) && faults.length !== r.faults) {
    c.fail(`${n}: faults.json holds ${faults.length} fault(s), run.json says ${r.faults}`);
  }
  for (const k of keel?.faults ?? []) {
    if (!listed.some(x => x.rule === k.rule && x.fault === k.fault)) {
      c.fail(`${n}: a fault Keel listed is not in faults.json: ${k.fault.slice(0, 80)}`);
    }
  }
  if (loop >= 2 && keel?.verdict === 'fail' && !listed.length) {
    c.fail(`${n}: Keel failed the candidate and faults.json holds no fault, so no builder was told why`);
  }
  const checksOk = p.replay.checksOk(r);
  if (checksOk && !f.rerun && !f.superseded) {
    c.fail(`${n}: the checks passed in the record, but nothing was rerun to confirm it`);
  }
  gate(f, checksOk);
  const want: Verdict = checksOk && keel?.verdict === 'pass' && !listed.length ? 'pass' : 'fail';
  if (r.verdict === want) c.ok(`${n}: verdict ${want}, recomputed from the checks, Keel and the faults`);
  else c.fail(`${n}: run.json verdict ${r.verdict}, recomputed ${want}`);
  return want;
}
