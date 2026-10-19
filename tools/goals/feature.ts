import { readFile, rm } from 'node:fs/promises';
import { resolve } from 'node:path';
import { z } from 'zod';
import { hash } from '../rag/corpus';
import { inputDirectories } from './audit';
import { TestPlanSchema, type Entry, type Requirement, type TestPlan } from './backlog';
import { runChecks } from './checks';
import { resolveLatestGdd, type CodeSource } from './discovery-sources';
import { fileState, json, localPath, portable, sourceFiles, type FileState } from './files';
import type { CampaignAsk, Limits } from './models';
import { applyPatch, type ChangedFile } from './patch';
import { PatchSchema, ReviewSchema, type CheckResult, type CodePatch, type Review, type Scope } from './schema';

export const FeatureAssessmentSchema = z.object({ satisfied: z.boolean(), explanation: z.string().min(12),
  evidence: z.array(z.object({ path: z.string(), quote: z.string().min(12) }).strict()).min(1), remaining: z.array(z.string()),
}).strict();
export type FeatureAssessment = z.infer<typeof FeatureAssessmentSchema>;
export type FeatureRound = { index: number; writeCall: string; patch: CodePatch; changes: ChangedFile[]; checks: CheckResult[];
  reviewCall?: string; review?: Review; assessmentCall?: string; assessment?: FeatureAssessment; sourceHashes?: Record<string, string> };
export type FeatureResult = { verified: boolean; generated: boolean; testPath: string; tests: string[]; writable: string[];
  error?: string; recordPath: string; rounds: number };
export type FeatureRecord = { version: 2; requirement: Requirement; writable: string[]; snapshotFile: string;
  testPlanCall: string; testPlan?: TestPlan; testReviewCall: string; testReview?: Review; testPath: string;
  testPatch?: CodePatch; testChanges?: ChangedFile[]; scope?: Scope; baselineChecks: CheckResult[]; rounds: FeatureRound[];
  prewriteReviewCall?: string; prewriteReview?: Review; prewriteAssessmentCall?: string; prewriteAssessment?: FeatureAssessment;
  testAttempts?: { planCall: string; reviewCall: string; plan: TestPlan; review?: Review; error?: string }[];
  context: CodeSource[]; priorFeature?: string; testDiscarded?: boolean; testDiscardReason?: string; result: FeatureResult };
export type FeatureSnapshot = { version: 2; files: Record<string, { text: string; sha256: string }> };
type Dependencies = { check: typeof runChecks; apply: typeof applyPatch };

export const passingFeatureReview = (review?: Review) => review?.verdict === 'pass' && review.faults.length === 0;
export function validateFeatureAssessment(assessment: FeatureAssessment, sources: Record<string, string | null>) {
  if (assessment.satisfied && assessment.remaining.length) throw new Error('Contradictory assessment: satisfied with remaining requirements');
  for (const evidence of assessment.evidence) if (!sources[evidence.path]?.includes(evidence.quote)) throw new Error(`Assessment cites unseen source: ${evidence.path}`);
}

export function validateTestPlan(plan: TestPlan, entry: Entry) {
  const wanted = [...entry.requirement.acceptance].sort(), covered = [...plan.covers].sort();
  if (JSON.stringify(wanted) !== JSON.stringify(covered)) throw new Error('Tests must cover every acceptance criterion exactly once');
  if (!new RegExp(`\\bpublic\\s+(?:sealed\\s+)?class\\s+${plan.className}\\b`).test(plan.code) || /\bnamespace\s/.test(plan.code))
    throw new Error('Generated tests must declare the named global public class');
  if (!/\[(?:Fact|Theory)(?:\s*\(|\s*\])/.test(plan.code) || !/\bAssert\./.test(plan.code)) throw new Error('Tests must contain xUnit test cases and assertions');
  if (/\bSkip\s*=|#\s*(?:if|line)|\bModuleInitializer\b|\bEnvironment\s*\.\s*Exit\b/.test(plan.code)) throw new Error('Generated tests may not skip cases, hide source or exit the runner');
}

/** Compilation failure or an empty test selection never establishes a behavioral gap. */
export async function baselineHasAssertionFailures(root: string, check: CheckResult, minimum: number): Promise<boolean> {
  if (check.ok || check.exitCode !== 1 || check.timedOut || check.total < minimum || check.failed < 1 || check.passed + check.failed !== check.total) return false;
  const xml = await readFile(resolve(root, check.reportFile), 'utf8').catch(() => '');
  const failed = [...xml.matchAll(/<UnitTestResult\b[^>]*\boutcome="Failed"[^>]*>([\s\S]*?)<\/UnitTestResult>/g)].map(match => match[1]!);
  return failed.length === check.failed && failed.every(body => /<Message>[\s\S]*?(?:Assert\.[A-Za-z]+|Xunit\.Sdk\.[A-Za-z]+Exception)[\s\S]*?<\/Message>/.test(body));
}

function sourcesText(sources: Record<string, string | null>) { return Object.entries(sources).map(([path, text]) => `${path}\n${text ?? '(file absent)'}`).join('\n\n'); }

/** Independent tests are frozen before the sole production writer runs. All stages are retained on failure. */
export async function executeFeature(root: string, directory: string, entry: Entry, context: CodeSource[], ask: CampaignAsk, limits: Limits,
  dependencies: Dependencies = { check: runChecks, apply: applyPatch }): Promise<FeatureResult> {
  root = resolve(root); directory = resolve(directory);
  const id = entry.requirement.id, writable = [...(entry.inspection?.writable ?? [])];
  const result: FeatureResult = { verified: false, generated: false, testPath: '', tests: [], writable, recordPath: portable(root, resolve(directory, 'feature.json')), rounds: 0 };
  const record: FeatureRecord = { version: 2, requirement: entry.requirement, writable, snapshotFile: 'snapshot.json',
    testPlanCall: `${id}-tests`, testReviewCall: `${id}-test-review`, testPath: '', baselineChecks: [], rounds: [], context, result };
  const save = () => json(resolve(directory, 'feature.json'), record);
  try {
    if (!writable.length || writable.length > 10 || new Set(writable).size !== writable.length || writable.some(path => !/^sim\/Suborned\.Sim\/[A-Za-z][A-Za-z0-9.]*\.cs$/.test(path)))
      throw new Error('Feature write scope must contain 1–10 unique flat shared-simulation C# paths');
    await Promise.all(writable.map(path => localPath(root, path)));
    const gdd = await resolveLatestGdd(root);
    if (!gdd.agent.text.includes(entry.requirement.quote)) throw new Error('Discovered requirement no longer appears in latest GDD');
    for (const source of context) if (hash(await readFile(await localPath(root, source.path), 'utf8')) !== source.sha256)
      throw new Error(`Inspection context changed before execution: ${source.path}`);
    const priorDirectory = (entry as Entry & { featureDirectory?: string }).featureDirectory;
    let prior: FeatureRecord | undefined;
    if (priorDirectory) {
      const priorPath = await localPath(root, `${priorDirectory}/feature.json`);
      const candidate = JSON.parse(await readFile(priorPath, 'utf8')) as FeatureRecord;
      if (candidate.version !== 2 || JSON.stringify(candidate.requirement) !== JSON.stringify(entry.requirement)) throw new Error('Prior feature requirement changed; refusing implicit continuation');
      if (candidate.testPlan && !candidate.testDiscarded && (candidate.testChanges || candidate.priorFeature)) {
        const previousSnapshot = JSON.parse(await readFile(await localPath(root, `${priorDirectory}/${candidate.snapshotFile}`), 'utf8')) as FeatureSnapshot;
        const previousState: FileState = Object.fromEntries(candidate.writable.map(path => [path, previousSnapshot.files[path]?.text ?? null]));
        for (const round of candidate.rounds) for (const change of round.changes) previousState[change.path] = change.after;
        const currentState = await fileState(root, candidate.writable);
        for (const path of candidate.writable) if (currentState[path] !== previousState[path]) throw new Error(`Prior generated implementation changed outside its recorded patches: ${path}`);
        if (await readFile(await localPath(root, candidate.testPath), 'utf8') !== candidate.testPlan.code) throw new Error('Prior frozen test changed; refusing implicit continuation');
        validateTestPlan(candidate.testPlan, entry);
        if (!passingFeatureReview(candidate.testReview)) throw new Error('Prior acceptance tests lack independent approval');
        prior = candidate; record.priorFeature = `${priorDirectory}/feature.json`;
        // Resume the exact approved write boundary, even if a fresh inspection proposes a different one.
        if (JSON.stringify([...writable].sort()) !== JSON.stringify([...candidate.writable].sort())) throw new Error('Resume write boundary changed; inspect the pending feature before expanding its scope');
      }
    }
    const initialPaths = [...new Set([...(await sourceFiles(root, inputDirectories)), gdd.agent.path, gdd.human.path, ...context.map(source => source.path)])].sort();
    const snapshot: FeatureSnapshot = { version: 2, files: Object.fromEntries(await Promise.all(initialPaths.map(async path => {
      const text = await readFile(await localPath(root, path), 'utf8'); return [path, { text, sha256: hash(text) }];
    }))) };
    await json(resolve(directory, 'snapshot.json'), snapshot);
    const baseline = await fileState(root, writable), expected: FileState = { ...baseline };
    const fixedContext: Record<string, string> = {};
    for (const source of context) fixedContext[source.path] = fixedContext[source.path] ? fixedContext[source.path] + '\n\n' + source.text : source.text;
    // Project helpers and one existing test demonstrate the actual executable test API, without prescribing a feature.
    const helpers = initialPaths.filter(path => /^sim\/Suborned\.Sim\.Tests\/(?:Repo|GlobalUsings)\.cs$/.test(path));
    const example = initialPaths.find(path => /^sim\/Suborned\.Sim\.Tests\/[^/]+Tests\.cs$/.test(path) && snapshot.files[path]!.text.length < 18000);
    for (const path of [...helpers, ...(example ? [example] : [])]) fixedContext[path] = snapshot.files[path]!.text;
    const queries = [`GDD ${entry.requirement.title} ${entry.requirement.terms.join(' ')}`, `simulation rules ${entry.requirement.terms.join(' ')} deterministic replay`];
    const priorTestFeedback: unknown[] = [];
    for (const outcome of entry.history.slice(-3)) {
      const path = `${outcome.run}/features/${id}/feature.json`;
      const previousFile = await localPath(root, path);
      if (!await Bun.file(previousFile).exists()) continue;
      const previous = await Bun.file(previousFile).json() as FeatureRecord;
      const rejectedReviews = [...(previous.testAttempts ?? []).map(attempt => attempt.review), previous.testReview]
        .filter((review): review is Review => !!review && !passingFeatureReview(review));
      for (const review of new Map(rejectedReviews.map(review => [JSON.stringify(review), review])).values()) priorTestFeedback.push({ path, review });
      if (previous.testDiscardReason) priorTestFeedback.push({ path, reason: previous.testDiscardReason });
    }
    const requirementText = `Discovered requirement and acceptance criteria:\n${JSON.stringify(entry.requirement)}\nInspection:\n${JSON.stringify(entry.inspection)}\nPrevious outcomes (repair earlier failures; do not repeat rejected tests):\n${JSON.stringify(entry.history.slice(-3))}\nPrevious test feedback:\n${JSON.stringify(priorTestFeedback)}\n`;
    const shownSources = () => ({ ...fixedContext, ...expected, ...(record.testPlan && (record.testChanges || record.priorFeature) ? { [record.testPath]: record.testPlan.code } : {}) });
    const audit = async () => {
      for (const [path, file] of Object.entries(snapshot.files)) if (!writable.includes(path)) {
        if (hash(await readFile(await localPath(root, path), 'utf8')) !== file.sha256) throw new Error(`Immutable feature input changed: ${path}`);
      }
      const now = await fileState(root, writable);
      for (const path of writable) if (now[path] !== expected[path]) throw new Error(`Unrecorded implementation change: ${path}`);
      if ((record.testChanges || record.priorFeature) && await readFile(await localPath(root, record.testPath), 'utf8') !== record.testPlan!.code) throw new Error('Frozen acceptance tests changed');
      const paths = await sourceFiles(root, inputDirectories);
      const allowed = new Set(initialPaths.filter(path => inputDirectories.some(dir => path.startsWith(dir + '/'))));
      for (const path of writable) if (expected[path] !== null) allowed.add(path);
      if (record.testChanges) allowed.add(record.testPath);
      if (paths.length !== allowed.size || paths.some(path => !allowed.has(path))) throw new Error('Unrecorded feature input appeared or disappeared');
      const currentGdd = await resolveLatestGdd(root);
      if (currentGdd.agent.sha256 !== gdd.agent.sha256 || currentGdd.human.sha256 !== gdd.human.sha256) throw new Error('Latest GDD changed during feature execution');
    };
    const call = <T>(callId: string, role: string, brief: string, task: string, schema: z.ZodType<T>) => ask(callId, role, `tools/goals/prompts/${brief}.md`, task, schema, queries);
    await save();
    if (prior) {
      record.testPlan = prior.testPlan; record.testReview = prior.testReview; record.testPath = prior.testPath;
      record.testPlanCall = prior.testPlanCall; record.testReviewCall = prior.testReviewCall;
      result.testPath = record.testPath; result.tests = [record.testPlan!.className];
      await save();
    } else {
    let testFeedback = '';
    for (let attempt = 1; attempt <= limits.maxIterations; attempt++) {
    record.testPlanCall = `${id}-tests${attempt === 1 ? '' : `-${attempt}`}`;
    record.testReviewCall = `${id}-test-review${attempt === 1 ? '' : `-${attempt}`}`;
    record.testReview = undefined;
    record.testPlan = await call(record.testPlanCall, 'simulation-engineer', 'feature-tests', requirementText + '\nCurrent source and test examples:\n' + sourcesText(shownSources())
      + '\nRevise the previous unapproved proposal using this feedback (tests are not frozen or written yet):\n' + testFeedback, TestPlanSchema);
    const proposal = { planCall: record.testPlanCall, reviewCall: record.testReviewCall, plan: record.testPlan } as NonNullable<FeatureRecord['testAttempts']>[number];
    (record.testAttempts ??= []).push(proposal);
    try { validateTestPlan(record.testPlan, entry); }
    catch (error) { proposal.error = String(error); testFeedback = JSON.stringify({ plan: record.testPlan, error: proposal.error }); await save(); if (attempt < limits.maxIterations) continue; throw error; }
    record.testPath = `sim/Suborned.Sim.Tests/${record.testPlan.className}.cs`;
    result.testPath = record.testPath; result.tests = [record.testPlan.className];
    if ((await fileState(root, [record.testPath]))[record.testPath] !== null) throw new Error(`Generated test file already exists: ${record.testPath}`);
    await save();
    record.testReview = await call(record.testReviewCall, 'code-performance-reviewer', 'feature-review', requirementText
      + '\nSTAGE: review tests BEFORE production changes. Judge meaningful coverage, real public behavior, design consistency and independent failure.\n'
      + `Proposed new tests:\n${JSON.stringify(record.testPlan)}\nCurrent source:\n${sourcesText(shownSources())}`, ReviewSchema);
    proposal.review = record.testReview;
    await save();
    if (passingFeatureReview(record.testReview)) break;
    testFeedback = JSON.stringify({ plan: record.testPlan, review: record.testReview });
    if (attempt === limits.maxIterations) throw new Error(`Independent acceptance-test review rejected the plan: ${record.testReview.summary}; ${JSON.stringify(record.testReview.faults)}`);
    }
    if (!record.testPlan || !passingFeatureReview(record.testReview)) throw new Error('No independently approved acceptance plan within the test revision budget');
    await audit();
    record.testPatch = { summary: record.testPlan.summary, edits: [{ path: record.testPath, search: '', replacement: record.testPlan.code }] };
    await save();
    record.testChanges = await dependencies.apply(root, record.testPatch, [record.testPath], { [record.testPath]: null });
    }
    record.scope = { id, goal: entry.requirement.title, sources: [{ path: gdd.agent.path, quote: entry.requirement.quote }],
      requirements: [{ id, title: entry.requirement.title, source: 0, owner: 'simulation-engineer', dependsOn: [], checks: ['target'] }],
      context: [...new Set([...context.map(source => source.path), record.testPath])].map(path => ({ path })), writable,
      testProject: 'sim/Suborned.Sim.Tests', checks: [
        { id: 'target', filter: `FullyQualifiedName~${record.testPlan!.className}`, minTests: record.testPlan!.minTests },
        { id: 'regression', filter: `FullyQualifiedName!~${record.testPlan!.className}`, minTests: 1 },
      ], regression: 'regression' };
    await save(); await audit();
    record.baselineChecks = await dependencies.check(root, record.scope, resolve(directory, 'baseline'));
    await save(); await audit();
    const target = record.baselineChecks.find(check => check.id === 'target'), regression = record.baselineChecks.find(check => check.id === 'regression');
    const discardInvalidTests = async (reason: string) => {
      if (!prior && record.testChanges && await readFile(await localPath(root, record.testPath), 'utf8') === record.testPlan!.code) {
        await rm(await localPath(root, record.testPath)); record.testDiscarded = true; record.testDiscardReason = reason; await save();
      }
    };
    if (!target || !regression?.ok) {
      await discardInvalidTests('Baseline regression did not pass; untrusted new acceptance tests removed before any production write');
      throw new Error('Baseline regression must pass before a feature may be written');
    }
    record.scope.checks[1]!.minTests = regression.total;
    await save();
    if (!target.ok && !(await baselineHasAssertionFailures(root, target, record.testPlan!.minTests))) {
      await discardInvalidTests('Acceptance tests failed to establish a valid behavioral red baseline');
      throw new Error('Generated acceptance tests did not establish a behavioral red baseline: compilation, setup, no-test or non-assertion failure');
    }
    const checksText = async (checks: CheckResult[]) => JSON.stringify(checks) + '\n' + (await Promise.all(checks.filter(check => !check.ok).map(async check =>
      `${check.id}:\n${(await readFile(resolve(root, check.outputFile), 'utf8')).slice(-16000)}`))).join('\n');
    const assess = async (callId: string, checks: CheckResult[], review: Review) => {
      const assessment = await call(callId, 'lead-producer', 'feature-assess', requirementText + '\nCurrent rescanned source:\n' + sourcesText(shownSources())
        + `\nChecks:\n${await checksText(checks)}\nIndependent review:\n${JSON.stringify(review)}`, FeatureAssessmentSchema);
      validateFeatureAssessment(assessment, shownSources()); return assessment;
    };
    if (target.ok) {
      record.prewriteReviewCall = `${id}-prewrite-review`;
      record.prewriteReview = await call(record.prewriteReviewCall, 'code-performance-reviewer', 'feature-review', requirementText
        + '\nSTAGE: tests passed BEFORE any implementation write. Verify this is already implemented, not vacuous tests.\n'
        + sourcesText(shownSources()) + '\nChecks:\n' + await checksText(record.baselineChecks), ReviewSchema);
      await save();
      record.prewriteAssessmentCall = `${id}-prewrite-assess`;
      record.prewriteAssessment = await assess(record.prewriteAssessmentCall, record.baselineChecks, record.prewriteReview);
      await audit();
      result.verified = passingFeatureReview(record.prewriteReview) && record.prewriteAssessment.satisfied;
      if (!result.verified) result.error = 'Tests passed, but independent review or semantic assessment could not confirm existing implementation';
      return result;
    }
    for (let index = 1; index <= limits.maxIterations; index++) {
      await audit();
      const previous = record.rounds.at(-1), checks = previous?.checks ?? record.baselineChecks;
      const writeCall = `${id}-write-${index}`;
      const patch = await call(writeCall, 'simulation-engineer', 'feature-write', requirementText
        + `\nAllowed production paths:\n${JSON.stringify(writable)}\nExact current production files:\n${JSON.stringify(expected)}`
        + '\nContext and frozen tests:\n' + sourcesText(shownSources())
        + `\nChecks:\n${await checksText(checks)}\nPrevious independent review:\n${JSON.stringify(previous?.review ?? null)}\nPrevious assessment:\n${JSON.stringify(previous?.assessment ?? null)}`, PatchSchema);
      const round: FeatureRound = { index, writeCall, patch, changes: [], checks: [] }; record.rounds.push(round);
      result.rounds = record.rounds.length; await save(); await audit();
      round.changes = await dependencies.apply(root, patch, writable, expected);
      for (const change of round.changes) expected[change.path] = change.after;
      result.generated = true; await save(); await audit();
      round.checks = await dependencies.check(root, record.scope, resolve(directory, `round-${index}`));
      await audit();
      // Mechanical source rescan is saved before any potentially budget-limited model call.
      round.sourceHashes = Object.fromEntries(Object.entries(await fileState(root, writable)).filter(([, text]) => text !== null).map(([path, text]) => [path, hash(text!)]));
      await save();
      round.reviewCall = `${id}-review-${index}`;
      round.review = await call(round.reviewCall, 'code-performance-reviewer', 'feature-review', requirementText
        + '\nSTAGE: independent implementation review.\nCurrent source:\n' + sourcesText(shownSources())
        + `\nPatch:\n${JSON.stringify(round.changes)}\nChecks:\n${await checksText(round.checks)}`, ReviewSchema);
      await save();
      round.assessmentCall = `${id}-assess-${index}`;
      round.assessment = await assess(round.assessmentCall, round.checks, round.review);
      await audit();
      if (round.checks.length === record.scope.checks.length && round.checks.every(check => check.ok)
        && round.checks.find(check => check.id === 'regression')?.total === regression.total
        && passingFeatureReview(round.review) && round.assessment.satisfied) { result.verified = true; break; }
      await save();
    }
    if (!result.verified) result.error = 'Feature iteration limit reached with unresolved behavior, regression, review or assessment';
  } catch (error) { result.error = error instanceof Error ? error.message : String(error); }
  finally { await save(); }
  return result;
}
