// Offline replay of recorded evidence: rebuild every code state and rerun its behavioural checks.
import { mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { dirname, isAbsolute, relative, resolve } from 'node:path';
import { isDeepStrictEqual } from 'node:util';
import { z } from 'zod';
import { interpretClaude } from '../rag/claude';
import { hash } from '../rag/corpus';
import { parseTrx, runChecks } from './checks';
import { fileState, json, loadScope, localPath, portable, type FileState } from './files';
import { preparePatch } from './patch';
import { rank, scan } from './scan';
import { AnalysisSchema, PatchSchema, ReviewSchema, type CheckResult } from './schema';
import type { RunRecord } from './record';

function assert(condition: unknown, message: string): asserts condition { if (!condition) throw new Error(`Evidence verification failed: ${message}`); }
const equal = (left: unknown, right: unknown, message: string) => assert(isDeepStrictEqual(left, right), message);
const result = (c: CheckResult) => ({ id: c.id, exitCode: c.exitCode, timedOut: c.timedOut, total: c.total, passed: c.passed, failed: c.failed, ok: c.ok });

export async function verifyRun(root: string, runDirectory: string, dependencies: { check: typeof runChecks } = { check: runChecks }) {
  root = resolve(root);
  const runRelative = isAbsolute(runDirectory) ? relative(root, runDirectory) : runDirectory;
  const directory = await localPath(root, runRelative);
  const record = JSON.parse(await readFile(await localPath(root, `${runRelative}/run.json`), 'utf8')) as RunRecord;
  assert(record.version === 1 && record.status === 'COMPLETE', 'run must report COMPLETE version 1');
  assert(record.rounds.length <= record.limits.maxIterations, 'iteration limit exceeded');
  assert(record.calls.length <= record.limits.maxCalls, 'call limit exceeded');
  assert(record.approvedBy?.trim(), 'missing recorded approval');
  const scope = await loadScope(root, record.scopePath);
  equal(scope, record.scope, 'scope changed since run');
  equal(Object.keys(record.baseline).sort(), [...scope.writable].sort(), 'baseline does not cover exactly the write scope');

  const evidence = async (path: string) => {
    const target = await localPath(root, path), rel = relative(directory, target);
    assert(rel !== '..' && !rel.startsWith('../') && !isAbsolute(rel), `evidence outside run directory: ${path}`);
    return readFile(target, 'utf8');
  };
  const modelOutputs = new Map<string, unknown>();
  for (const call of record.calls) {
    assert(hash(await evidence(call.promptFile)) === call.promptSha256, `prompt hash ${call.id}`);
    assert(hash(await evidence(call.card.file)) === call.card.sourceHash, `card hash ${call.id}`);
    const schema = JSON.parse(await evidence(call.promptFile.replace(/\.prompt\.txt$/, '.schema.json')));
    assert(hash(JSON.stringify(schema)) === call.schemaSha256, `schema hash ${call.id}`);
    equal(JSON.parse(await evidence(call.promptFile.replace(/\.prompt\.txt$/, '.retrieval.json'))), call.retrieval, `retrieval ${call.id}`);
    const transcript = interpretClaude(await evidence(call.eventsFile), call.exitCode);
    equal(transcript.model, call.model, `model identity ${call.id}`);
    equal(transcript.modelsUsed, call.modelsUsed, `models used ${call.id}`);
    equal(transcript.costUsd, call.costUsd, `model cost ${call.id}`);
    equal(transcript.tools, call.tools, `tool isolation ${call.id}`);
    if (record.bootstrap) assert(call.retrieval.corpusHash === record.bootstrap.corpusHash, `retrieval corpus changed ${call.id}`);
    await evidence(call.stderrFile);
    assert(hash(await readFile(await localPath(root, call.brief.source), 'utf8')) === call.brief.sourceHash, `brief changed: ${call.brief.source}`);
    if (call.status === 'accepted') {
      assert(transcript.ok, `accepted call has invalid transcript: ${call.id}`);
      const output = JSON.parse(await evidence(call.outputFile));
      equal(output, transcript.output, `output differs from transcript: ${call.id}`);
      const expected = call.objective.startsWith('assess-') ? AnalysisSchema : call.objective.startsWith('write-') ? PatchSchema : call.objective.startsWith('review-') ? ReviewSchema : undefined;
      assert(expected, `unknown objective ${call.objective}`);
      equal(schema, z.toJSONSchema(expected), `wrong schema for ${call.objective}`);
      expected.parse(output);
      assert(!modelOutputs.has(call.objective), `multiple accepted outputs for ${call.objective}`);
      modelOutputs.set(call.objective, output);
    }
  }
  assert(record.calls.every(c => c.costUsd !== null && Number.isFinite(c.costUsd) && c.costUsd >= 0), 'unknown model spend');
  assert(record.calls.reduce((sum, c) => sum + c.costUsd!, 0) <= record.limits.maxUsd, 'spend limit exceeded');

  const verifyArchivedChecks = async (checks: CheckResult[]) => {
    equal(checks.map(c => c.id), scope.checks.map(c => c.id), 'recorded check IDs/order differ from scope');
    for (const check of checks) {
      assert(hash(await evidence(check.outputFile)) === check.outputHash, `check log hash ${check.id}`);
      const trx = await evidence(check.reportFile).catch(error => { if (error.code === 'ENOENT') return ''; throw error; });
      const parsed = parseTrx(trx, scope.checks.find(c => c.id === check.id)!.minTests);
      equal({ total: parsed.total, passed: parsed.passed, failed: parsed.failed, ok: check.exitCode === 0 && !check.timedOut && parsed.ok },
        { total: check.total, passed: check.passed, failed: check.failed, ok: check.ok }, `recorded TRX counters ${check.id}`);
    }
  };

  // Read and validate every immutable input before creating an isolated replay workspace.
  const copies = new Map<string, string>();
  for (const [path, sha256] of Object.entries(record.inputs)) {
    await localPath(root, path);
    if (scope.writable.includes(path)) {
      const baseline = record.baseline[path];
      assert(baseline !== null && baseline !== undefined && hash(baseline) === sha256, `baseline input hash ${path}`);
    } else {
      const text = await readFile(await localPath(root, path), 'utf8');
      assert(hash(text) === sha256, `immutable input changed: ${path}`);
      copies.set(path, text);
    }
  }
  for (const path of [record.scopePath, ...scope.sources.map(s => s.path), ...scope.context.map(c => c.path)])
    assert(Object.hasOwn(record.inputs, path), `missing pinned input ${path}`);
  let state: FileState = { ...record.baseline };
  for (const round of record.rounds) {
    PatchSchema.parse(round.patch);
    const changes = preparePatch(round.patch, scope.writable, state);
    equal(changes, round.changes, `recorded patch hashes/content round ${round.index}`);
    for (const change of changes) state[change.path] = change.after;
  }
  equal(await fileState(root, scope.writable), state, 'current implementation differs from generated patches');

  const outputParent = await localPath(root, 'out/goals');
  await mkdir(outputParent, { recursive: true });
  const temporary = await mkdtemp(resolve(outputParent, 'verify-'));
  const put = async (path: string, text: string | null) => {
    const target = await localPath(temporary, path);
    if (text === null) await rm(target, { force: true });
    else { await mkdir(dirname(target), { recursive: true }); await writeFile(target, text); }
  };
  for (const [path, text] of copies) await put(path, text);
  for (const [path, text] of Object.entries(record.baseline)) await put(path, text);
  const replayed: { stage: string; checks: CheckResult[] }[] = [];
  await verifyArchivedChecks(record.baselineChecks);
  const baselineChecks = await dependencies.check(temporary, scope, resolve(temporary, 'evidence/baseline'));
  equal(baselineChecks.map(result), record.baselineChecks.map(result), 'baseline test results changed');
  replayed.push({ stage: 'baseline', checks: baselineChecks });
  let scanned = await scan(temporary, scope, baselineChecks);
  let analysis = AnalysisSchema.parse(modelOutputs.get('assess-0'));
  let ranked = rank(scope, scanned, analysis);
  state = { ...record.baseline };
  for (const [i, round] of record.rounds.entries()) {
    assert(round.index === i + 1, 'round indices must be consecutive');
    equal(round.ranking, ranked, `priority decision round ${round.index}`);
    const prior = record.rounds[i - 1];
    const selected = ranked.find(r => r.eligible)?.id ?? (prior && (prior.review?.verdict === 'fail' || prior.checks.some(c => !c.ok)) ? prior.selected : undefined);
    assert(selected === round.selected, `selected feature is not highest eligible priority or reviewer repair round ${round.index}`);
    equal(round.patch, modelOutputs.get(`write-${round.index}`), `patch was not the writer output round ${round.index}`);
    equal(round.review, modelOutputs.get(`review-${round.index}`), `review differs from model output round ${round.index}`);
    for (const change of preparePatch(round.patch, scope.writable, state)) { await put(change.path, change.after); state[change.path] = change.after; }
    await verifyArchivedChecks(round.checks);
    const checks = await dependencies.check(temporary, scope, resolve(temporary, `evidence/round-${round.index}`));
    equal(checks.map(result), round.checks.map(result), `test results changed round ${round.index}`);
    replayed.push({ stage: `round-${round.index}`, checks });
    scanned = await scan(temporary, scope, checks);
    assert(round.scan && round.analysis, `missing rescan round ${round.index}`);
    equal(round.scan.checks, round.checks, `rescan check evidence round ${round.index}`);
    equal(scanned.inventory, round.scan.inventory, `rescan inventory round ${round.index}`);
    equal(scanned.sources, round.scan.sources, `rescan sources round ${round.index}`);
    analysis = AnalysisSchema.parse(modelOutputs.get(`assess-${round.index}`));
    equal(round.analysis, analysis, `rescan analysis differs from model output round ${round.index}`);
    ranked = rank(scope, scanned, analysis);
  }
  equal(record.finalAnalysis, analysis, 'final analysis differs from last rescan');
  equal(record.ranking, ranked, 'final ranking differs from reconstructed evidence');
  assert(record.finalScan, 'missing final scan');
  equal(record.finalScan.inventory, scanned.inventory, 'final inventory');
  equal(record.finalScan.sources, scanned.sources, 'final source evidence');
  equal(record.finalScan.checks, record.rounds.at(-1)?.checks ?? record.baselineChecks, 'final check evidence');
  assert(scanned.checks.every(c => c.ok) && ranked.every(r => r.state === 'verified'), 'scoped requirements or regression not complete');
  const review = record.rounds.at(-1)?.review;
  assert(record.rounds.length === 0 || (review?.verdict === 'pass' && review.faults.length === 0), 'final independent review did not pass');
  const summary = { ok: true, run: record.id, rounds: record.rounds.length, generatedFeature: record.rounds.length > 0, requirements: ranked.length, directory: portable(root, temporary), replayed };
  await json(resolve(temporary, 'verification.json'), summary);
  return summary;
}
