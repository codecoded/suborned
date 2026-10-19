import { afterEach, expect, test } from 'bun:test';
import { mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { hash } from '../../rag/corpus';
import type { Entry, TestPlan } from '../backlog';
import { baselineHasAssertionFailures, executeFeature, validateFeatureAssessment, validateTestPlan } from '../feature';
import { applyPatch } from '../patch';
import type { CampaignAsk } from '../models';
import type { CheckResult, Scope } from '../schema';

const roots: string[] = [];
afterEach(async () => { await Promise.all(roots.splice(0).map(path => rm(path, { recursive: true, force: true }))); });
const enginePath = 'sim/Suborned.Sim/Game.cs', before = '// Current game behavior is missing.\n', after = '// Current game behavior is implemented.\n';
const testCode = 'using Xunit;\npublic class GoalFixtureTests { [Fact] public void BehavesAsRequired() { Assert.Equal(1, 1); } }\n';
const plan: TestPlan = { summary: 'Fixture independent acceptance plan', className: 'GoalFixtureTests', code: testCode, minTests: 1, covers: ['The public observable behavior follows the requirement.'] };
const entry: Entry = { requirement: { id: 'fixture-behavior', title: 'Fixture observable behavior', section: '01',
  quote: 'The game behavior is observable and deterministic.', acceptance: plan.covers, terms: ['game', 'behavior'], dependsOn: [], value: 5, effort: 1, risk: 1, rationale: 'Useful bounded behavioral requirement' },
  anchor: 'fixture', status: 'missing', explanation: 'Behavior is missing', inspectedHashes: {}, history: [],
  inspection: { state: 'missing', explanation: 'Behavior is missing from execution', evidence: [{ path: enginePath, quote: before.trim() }], reads: [], searches: [], tests: [], writable: [enginePath] } };
type Options = { alreadyGreen?: boolean; compilationFailure?: boolean; badTestReview?: boolean; failReview?: boolean; mutateTests?: boolean; limitAtReview?: boolean };
async function fixture(options: Options = {}) {
  const root = await mkdtemp(join(tmpdir(), 'a05-feature-')); roots.push(root);
  for (const dir of ['GDD', 'sim/Suborned.Sim', 'sim/Suborned.Sim.Tests', 'contracts', 'runs/missions', 'briefs/011/motive-fights']) await mkdir(join(root, dir), { recursive: true });
  const code = options.alreadyGreen ? after : before;
  await writeFile(join(root, enginePath), code);
  await writeFile(join(root, 'GDD/the-suborned-agent-gdd-v0.24.0-2026-10-18.md'), '## §01 CORE\n' + entry.requirement.quote);
  await writeFile(join(root, 'GDD/the-suborned-gdd-v0.24.0-2026-10-18.html'), '<p>' + entry.requirement.quote + '</p>');
  const events: string[] = [];
  const ask: CampaignAsk = async (id, role, brief, task, schema) => {
    events.push(id);
    if (id.endsWith('-tests')) return schema.parse(plan);
    if (id.includes('-write-')) return schema.parse({ summary: 'Implement generic fixture behavior', edits: [{ path: enginePath, search: before, replacement: after }] });
    if (id.includes('review')) {
      if (options.limitAtReview && id.endsWith('-review-1')) throw new Error('Model budget reached');
      const fail = options.badTestReview && id.endsWith('-test-review') || options.failReview && id.endsWith('-review-1');
      return schema.parse({ verdict: fail ? 'fail' : 'pass', summary: fail ? 'Substantive independent fault' : 'Independently reviewed', faults: fail ? [{ path: enginePath, problem: 'Behavior is incomplete', fix: 'Complete actual behavior' }] : [] });
    }
    return schema.parse({ satisfied: true, explanation: 'All actual behavioral requirements are implemented', evidence: [{ path: enginePath, quote: after.trim() }], remaining: [] });
  };
  const check = async (r: string, scope: Scope, directory: string): Promise<CheckResult[]> => {
    await mkdir(directory, { recursive: true });
    const green = (await readFile(join(r, enginePath), 'utf8')) === after;
    events.push(green ? 'checks-green' : 'checks-red');
    if (options.mutateTests && green) await writeFile(join(root, 'sim/Suborned.Sim.Tests/GoalFixtureTests.cs'), 'Changed tests');
    return Promise.all(scope.checks.map(async spec => {
      const ok = spec.id === 'regression' || green;
      const total = spec.id === 'regression' ? 5 : options.compilationFailure ? 0 : 1, failed = ok ? 0 : total;
      const reportFile = join(directory, `${spec.id}.trx`), outputFile = join(directory, `${spec.id}.log`);
      const xml = failed ? '<TestRun><UnitTestResult outcome="Failed"><Output><ErrorInfo><Message>Assert.Equal() Failure: expected true</Message></ErrorInfo></Output></UnitTestResult></TestRun>' : '<TestRun />';
      await writeFile(reportFile, xml); await writeFile(outputFile, ok ? 'Passed' : 'Failed observable assertion');
      return { id: spec.id, argv: ['offline-fixture'], exitCode: ok ? 0 : 1, timedOut: false, total, passed: ok ? total : 0, failed, ok, outputFile, reportFile, outputHash: hash(ok ? 'Passed' : 'Failed observable assertion'), seconds: 0 };
    }));
  };
  const directory = join(root, 'runs/feature');
  const run = () => executeFeature(root, directory, entry, [{ path: enginePath, from: 1, to: 1, sha256: hash(code), text: code }], ask,
    { maxUsd: 10, maxCalls: 20, maxIterations: 1 }, { check, apply: applyPatch });
  return { root, directory, events, run, ask, check };
}

test('independent tests precede production and red-to-green verification retains self-contained snapshots', async () => {
  const f = await fixture(), result = await f.run();
  expect(result.verified).toBe(true); expect(result.generated).toBe(true);
  expect(f.events).toEqual(['fixture-behavior-tests', 'fixture-behavior-test-review', 'checks-red', 'fixture-behavior-write-1', 'checks-green', 'fixture-behavior-review-1', 'fixture-behavior-assess-1']);
  const record = JSON.parse(await readFile(join(f.directory, 'feature.json'), 'utf8'));
  expect(record.scope.checks[1].minTests).toBe(5);
  expect(record.rounds[0].sourceHashes[enginePath]).toBe(hash(after));
  expect(record.testChanges[0].after).toBe(plan.code);
  const snapshot = JSON.parse(await readFile(join(f.directory, 'snapshot.json'), 'utf8'));
  expect(snapshot.files[enginePath]).toEqual({ text: before, sha256: hash(before) });
  expect(snapshot.files[result.testPath]).toBeUndefined();
});

test('already implemented behavior is reviewed and assessed without a production write', async () => {
  const f = await fixture({ alreadyGreen: true }), result = await f.run();
  expect(result.verified).toBe(true); expect(result.generated).toBe(false); expect(result.rounds).toBe(0);
  expect(f.events).toContain('fixture-behavior-prewrite-review');
  expect(f.events).toContain('fixture-behavior-prewrite-assess');
  expect(f.events.some(event => event.includes('-write-'))).toBe(false);
});

test('nonbehavioral baseline failure cannot trigger a writer', async () => {
  const f = await fixture({ compilationFailure: true }), result = await f.run();
  expect(result.verified).toBe(false); expect(result.generated).toBe(false);
  expect(result.error).toContain('behavioral red baseline');
  expect(f.events.some(event => event.includes('-write-'))).toBe(false);
});

test('rejected tests are recorded but never written', async () => {
  const f = await fixture({ badTestReview: true }), result = await f.run();
  expect(result.error).toContain('acceptance-test review rejected');
  expect(await Bun.file(join(f.root, result.testPath)).exists()).toBe(false);
});

test('independent test-review faults drive another proposal before tests are frozen or production is written', async () => {
  const f = await fixture();
  let proposals = 0;
  const ask: CampaignAsk = async (id, role, brief, task, schema) => {
    if (id === 'fixture-behavior-test-review') return schema.parse({ verdict: 'fail', summary: 'Distinct tuple values need coverage',
      faults: [{ path: 'GoalFixtureTests', problem: 'Identical values cannot detect swapped fields', fix: 'Use distinct serializer fixture values' }] });
    if (id.includes('-tests')) {
      proposals++;
      expect(await Bun.file(join(f.root, 'sim/Suborned.Sim.Tests/GoalFixtureTests.cs')).exists()).toBe(false);
      expect(await readFile(join(f.root, enginePath), 'utf8')).toBe(before);
      if (proposals === 2) expect(task).toContain('Use distinct serializer fixture values');
      return schema.parse(plan);
    }
    return f.ask(id, role, brief, task, schema, []);
  };
  const result = await executeFeature(f.root, f.directory, entry, [{ path: enginePath, from: 1, to: 1, sha256: hash(before), text: before }], ask,
    { maxUsd: 10, maxCalls: 20, maxIterations: 2 }, { check: f.check, apply: applyPatch });
  expect(result.verified).toBe(true); expect(proposals).toBe(2);
  const record = await Bun.file(join(f.directory, 'feature.json')).json();
  expect(record.testAttempts.map((a: {review:{verdict:string}}) => a.review.verdict)).toEqual(['fail', 'pass']);
  expect(record.testPlanCall).toBe('fixture-behavior-tests-2');
});

test('a later run recovers reviewer faults when replacement test generation was interrupted', async () => {
  const f = await fixture(), previous = 'runs/rejected';
  const path = join(f.root, previous, 'features', entry.requirement.id, 'feature.json');
  await mkdir(resolve(path, '..'), { recursive: true });
  await writeFile(path, JSON.stringify({ testAttempts: [{ plan, review: { verdict: 'fail', summary: 'Missing distinguishing values',
    faults: [{ path: 'GoalFixtureTests', problem: 'Swapped fields pass', fix: 'Preserve this exact interrupted-review feedback' }] } }] }));
  let recovered = false;
  const ask: CampaignAsk = async (id, role, brief, task, schema, queries) => {
    if (id.endsWith('-tests')) { expect(task).toContain('Preserve this exact interrupted-review feedback'); recovered = true; }
    return f.ask(id, role, brief, task, schema, queries);
  };
  const result = await executeFeature(f.root, f.directory, { ...entry, history: [{ run: previous, status: 'partial', reason: 'Interrupted proposal' }] },
    [{ path: enginePath, from: 1, to: 1, sha256: hash(before), text: before }], ask, { maxUsd: 10, maxCalls: 20, maxIterations: 1 }, { check: f.check, apply: applyPatch });
  expect(recovered).toBe(true); expect(result.verified).toBe(true);
});

test('a reviewer fault prevents success despite passing tests and optimistic assessment', async () => {
  const f = await fixture({ failReview: true }), result = await f.run();
  expect(result.verified).toBe(false); expect(result.generated).toBe(true); expect(result.error).toContain('iteration limit');
});

test('test mutation is detected before verification', async () => {
  const f = await fixture({ mutateTests: true }), result = await f.run();
  expect(result.verified).toBe(false); expect(result.error).toContain('Frozen acceptance tests changed');
});

test('budget interruption after writing retains incomplete mechanical rescan evidence', async () => {
  const f = await fixture({ limitAtReview: true }), result = await f.run();
  expect(result.verified).toBe(false); expect(result.generated).toBe(true); expect(result.error).toContain('budget');
  const record = JSON.parse(await readFile(join(f.directory, 'feature.json'), 'utf8'));
  expect(record.rounds[0].sourceHashes[enginePath]).toBe(hash(after));
  expect(record.rounds[0].checks.every((check: CheckResult) => check.ok)).toBe(true);
});

test('a later invocation resumes frozen tests and current generated code without spending on test regeneration', async () => {
  const f = await fixture({ limitAtReview: true }), first = await f.run();
  expect(first.verified).toBe(false); expect(first.generated).toBe(true);
  const priorRecord = await readFile(join(f.directory, 'feature.json'), 'utf8');
  f.events.length = 0;
  const continued = await executeFeature(f.root, join(f.root, 'runs/continued'), { ...entry, featureDirectory: 'runs/feature' } as Entry,
    [{ path: enginePath, from: 1, to: 1, sha256: hash(after), text: after }], f.ask, { maxUsd: 10, maxCalls: 20, maxIterations: 1 }, { check: f.check, apply: applyPatch });
  expect(continued.verified).toBe(true); expect(continued.generated).toBe(false);
  expect(f.events).toEqual(['checks-green', 'fixture-behavior-prewrite-review', 'fixture-behavior-prewrite-assess']);
  expect(await readFile(join(f.directory, 'feature.json'), 'utf8')).toBe(priorRecord);
  const record = JSON.parse(await readFile(join(f.root, 'runs/continued/feature.json'), 'utf8'));
  expect(record.priorFeature).toBe('runs/feature/feature.json'); expect(record.testPatch).toBeUndefined();
});

test('resume refuses unrecorded production edits instead of attributing them to the agent', async () => {
  const f = await fixture({ limitAtReview: true }); await f.run();
  await writeFile(join(f.root, enginePath), after + '// Manual repair\n');
  const changed = await readFile(join(f.root, enginePath), 'utf8');
  const result = await executeFeature(f.root, join(f.root, 'runs/continued'), { ...entry, featureDirectory: 'runs/feature' } as Entry,
    [{ path: enginePath, from: 1, to: 2, sha256: hash(changed), text: changed }], f.ask, { maxUsd: 10, maxCalls: 20, maxIterations: 1 }, { check: f.check, apply: applyPatch });
  expect(result.verified).toBe(false); expect(result.error).toContain('outside its recorded patches');
});

test('test and assessment validators reject missing criteria, skipped tests and fabricated evidence', () => {
  expect(() => validateTestPlan({ ...plan, covers: ['Something else entirely'] }, entry)).toThrow('every acceptance');
  expect(() => validateTestPlan({ ...plan, code: testCode.replace('[Fact]', '[Fact(Skip="hidden")]') }, entry)).toThrow('skip');
  expect(() => validateFeatureAssessment({ satisfied: true, explanation: 'Claimed behavior exists', remaining: ['Still missing'], evidence: [{ path: enginePath, quote: after }] }, { [enginePath]: after })).toThrow('Contradictory');
  expect(() => validateFeatureAssessment({ satisfied: true, explanation: 'Claimed behavior exists', remaining: [], evidence: [{ path: enginePath, quote: after }] }, { [enginePath]: before })).toThrow('unseen');
});

test('baseline classifier rejects setup exceptions and zero tests', async () => {
  const f = await fixture();
  const reportFile = resolve(f.root, 'bad.trx');
  await writeFile(reportFile, '<TestRun><UnitTestResult outcome="Failed"><Output><ErrorInfo><Message>NullReferenceException</Message></ErrorInfo></Output></UnitTestResult></TestRun>');
  const check: CheckResult = { id: 'target', argv: [], exitCode: 1, timedOut: false, total: 1, passed: 0, failed: 1, ok: false, reportFile, outputFile: '', outputHash: '', seconds: 0 };
  expect(await baselineHasAssertionFailures(f.root, check, 1)).toBe(false);
  expect(await baselineHasAssertionFailures(f.root, { ...check, total: 0, failed: 0 }, 1)).toBe(false);
});
