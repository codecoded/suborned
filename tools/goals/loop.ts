import { readFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { runChecks } from './checks';
import { fileState, json } from './files';
import { LimitReached, type Ask } from './models';
import { applyPatch } from './patch';
import { rank, scan, scanPrompt, type Scan } from './scan';
import { AnalysisSchema, PatchSchema, ReviewSchema, type Review } from './schema';
import type { RunRecord, RoundRecord } from './record';

export type LoopDependencies = { ask: Ask; check: typeof runChecks; scan: typeof scan; apply: typeof applyPatch; audit?: () => Promise<void> };
const passingReview = (review?: Review) => review?.verdict === 'pass' && review.faults.length === 0;
export const selectedRequirement = (record: RunRecord) => record.ranking?.find(r => r.eligible)?.id
  ?? (record.rounds.at(-1)?.review?.verdict === 'fail' || record.rounds.at(-1)?.checks.some(c => !c.ok)
    ? record.rounds.at(-1)?.selected : undefined);

async function failureOutput(root: string, scanned: Scan) {
  return (await Promise.all(scanned.checks.filter(c => !c.ok).map(async c => `${c.id}:\n${(await readFile(resolve(root, c.outputFile), 'utf8')).slice(-18000)}`))).join('\n\n');
}

/** Rescan after every applied patch, including failed builds. Completion requires source, tests and a separate reviewer. */
export async function runLoop(root: string, directory: string, record: RunRecord, deps: LoopDependencies): Promise<RunRecord> {
  const save = () => json(resolve(directory, 'run.json'), record);
  const { scope } = record;
  try {
    await deps.audit?.();
    record.baselineChecks = await deps.check(root, scope, resolve(directory, 'baseline'));
    record.finalScan = await deps.scan(root, scope, record.baselineChecks);
    record.finalAnalysis = await deps.ask('assess', 0, 'lead-producer', scanPrompt(scope, record.finalScan), AnalysisSchema);
    record.ranking = rank(scope, record.finalScan, record.finalAnalysis);
    await json(resolve(directory, 'baseline.scan.json'), record.finalScan);
    await save();
    for (let index = 1; index <= record.limits.maxIterations; index++) {
      if (record.ranking.every(r => r.state === 'verified') && record.finalScan.checks.every(c => c.ok)
          && (!record.rounds.length || passingReview(record.rounds.at(-1)?.review))) {
        record.status = 'COMPLETE'; break;
      }
      const selected = selectedRequirement(record);
      if (!selected) { record.error = 'No eligible requirement: unknown evidence, blocked dependencies or unresolved design. Scope remains incomplete.'; break; }
      console.log(`[goals] round ${index}: ${selected}; ${record.ranking.find(r => r.id === selected)?.score} priority`);
      const expected = await fileState(root, scope.writable);
      const task = scanPrompt(scope, record.finalScan) + `\nSelected: ${selected}\nPriority decision: ${JSON.stringify(record.ranking)}\n`
        + `Previous review: ${JSON.stringify(record.rounds.at(-1)?.review ?? null)}\nFailures:\n${await failureOutput(root, record.finalScan)}\n`
        + `Exact current writable files (null means absent):\n${JSON.stringify(expected)}`;
      const patch = await deps.ask('write', index, scope.requirements.find(r => r.id === selected)!.owner, task, PatchSchema);
      const round: RoundRecord = { index, selected, ranking: record.ranking, patch, changes: [], checks: [] };
      record.rounds.push(round);
      await save(); // preserve proposed patch before any mutation
      await deps.audit?.();
      round.changes = await deps.apply(root, patch, scope.writable, expected);
      await save();
      round.checks = await deps.check(root, scope, resolve(directory, `round-${index}`));
      round.scan = await deps.scan(root, scope, round.checks);
      // Persist the mechanical rescan even if the budget prevents subsequent model assessment.
      record.finalScan = round.scan;
      await save();
      round.review = await deps.ask('review', index, 'code-performance-reviewer',
        scanPrompt(scope, round.scan) + `\nPatch with before/after evidence:\n${JSON.stringify(round.changes)}\nFailures:\n${await failureOutput(root, round.scan)}`, ReviewSchema);
      if (round.review.verdict === 'pass' && round.review.faults.length) throw new Error('Contradictory review: pass with unresolved faults');
      await save();
      round.analysis = await deps.ask('assess', index, 'lead-producer',
        scanPrompt(scope, round.scan) + `\nIndependent review (unresolved faults mean partial, not satisfied):\n${JSON.stringify(round.review)}`, AnalysisSchema);
      record.finalAnalysis = round.analysis;
      record.ranking = rank(scope, round.scan, round.analysis);
      await deps.audit?.();
      await save();
    }
    await deps.audit?.();
    if (record.ranking?.every(r => r.state === 'verified') && record.finalScan?.checks.every(c => c.ok)
        && (!record.rounds.length || passingReview(record.rounds.at(-1)?.review))) record.status = 'COMPLETE';
    else record.error ??= 'Iteration limit reached; scoped requirements are not all verified.';
  } catch (error) {
    record.status = error instanceof LimitReached ? 'INCOMPLETE' : 'ERROR';
    record.error = error instanceof Error ? error.message : String(error);
  } finally { record.finishedAt = new Date().toISOString(); await save(); }
  return record;
}
