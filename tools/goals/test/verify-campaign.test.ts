import { afterEach, expect, test } from 'bun:test';
import { cp, mkdtemp, readFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { resolve } from 'node:path';
import { z } from 'zod';
import type { Call } from '../../rag/agent';
import { hash } from '../../rag/corpus';
import { CatalogSchema, InspectionSchema, reconcile, TestPlanSchema, type Catalog } from '../backlog';
import { checkScope, type CampaignRecord } from '../campaign';
import { runChecks } from '../checks';
import { FeatureAssessmentSchema, type FeatureRecord, type FeatureSnapshot } from '../feature';
import { json } from '../files';
import { preparePatch } from '../patch';
import { PatchSchema, ReviewSchema, type CheckResult, type CodePatch, type Scope } from '../schema';
import { verifyCampaign } from '../verify-campaign';

const roots: string[] = [];
afterEach(async () => { for (const root of roots.splice(0)) await rm(root, { recursive: true, force: true }); });

const path = 'sim/Suborned.Sim/Game.cs', testPath = 'sim/Suborned.Sim.Tests/GoalFundsTests.cs';
const before = 'namespace Suborned.Sim; public class Game { public bool CanAfford => true; }';
const after = before.replace('=> true', '=> false');
const xml = (ok: boolean) => `<TestRun><Results>${ok ? '' : '<UnitTestResult outcome="Failed"><Output><ErrorInfo><Message>Assert.False() Failure</Message></ErrorInfo></Output></UnitTestResult>'}</Results><ResultSummary outcome="${ok ? 'Completed' : 'Failed'}"><Counters total="1" executed="1" passed="${ok ? 1 : 0}" failed="${ok ? 0 : 1}"/></ResultSummary></TestRun>`;

async function fixture() {
  const root = await mkdtemp(resolve(tmpdir(), 'a05-campaign-verify-')); roots.push(root);
  const run = 'runs/goals/campaign-fixture', featureDir = `${run}/features/funds`;
  const designQuote = 'Orders whose price exceeds available funds must be rejected.';
  const agentText = `## §01 GAME\n${designQuote}\nOrders must also remain replayable from their recorded inputs.\n`;
  const humanText = '<h1>Human design</h1>';
  const requirement = { id: 'funds', title: 'Reject unaffordable orders', section: '01', quote: designQuote,
    acceptance: ['An unaffordable order is rejected without spending funds.'], terms: ['funds', 'price'], dependsOn: [], value: 4, effort: 1, risk: 1, rationale: 'Visible prices must match whether an order can execute.' };
  const catalog: Catalog = { summary: 'Pricing and determinism', coverage: [{ section: '01', disposition: 'requirements', reason: 'The section defines executable simulation requirements.' }], requirements: [requirement,
    { ...requirement, id: 'replay', title: 'Replay recorded orders', quote: 'Orders must also remain replayable from their recorded inputs.' }] };
  const snapshot: FeatureSnapshot = { version: 2, files: Object.fromEntries(Object.entries({
    [path]: before, 'GDD/agent.md': agentText, 'GDD/human.html': humanText,
    'sim/Suborned.Sim.Tests/Suborned.Sim.Tests.csproj': '<Project/>',
  }).map(([path, text]) => [path, { text, sha256: hash(text) }])) };
  const testPlan = { summary: 'Check order affordability through public behavior.', className: 'GoalFundsTests',
    code: 'using Xunit; using Suborned.Sim;\npublic class GoalFundsTests { [Fact] public void UnaffordableOrderIsRejected() { Assert.False(new Game().CanAfford); } }\n',
    minTests: 1, covers: requirement.acceptance };
  const testPatch: CodePatch = { summary: testPlan.summary, edits: [{ path: testPath, search: '', replacement: testPlan.code }] };
  const patch: CodePatch = { summary: 'Enforce affordability at order evaluation.', edits: [{ path, search: '=> true', replacement: '=> false' }] };
  const review = { verdict: 'pass' as const, summary: 'Public behavioral assertion matches the source requirement.', faults: [] };
  const assessment = { satisfied: true, explanation: 'The execution path rejects the tested unaffordable order.', evidence: [{ path, quote: 'public bool CanAfford => false' }], remaining: [] };
  const scope: Scope = { id: 'funds', goal: requirement.title, sources: [{ path: 'GDD/agent.md', quote: designQuote }],
    requirements: [{ id: 'funds', title: requirement.title, source: 0, owner: 'simulation-engineer', dependsOn: [], checks: ['target'] }],
    context: [{ path }, { path: testPath }], writable: [path], testProject: 'sim/Suborned.Sim.Tests',
    checks: [{ id: 'target', filter: 'FullyQualifiedName~GoalFundsTests', minTests: 1 }, { id: 'regression', filter: 'FullyQualifiedName!~GoalFundsTests', minTests: 1 }], regression: 'regression' };
  const check = async (stage: string, id: string, ok: boolean): Promise<CheckResult> => {
    const outputFile = `${featureDir}/${stage}/${id}.log`, reportFile = `${featureDir}/${stage}/${id}.trx`, output = `${id} ${ok ? 'passed' : 'assertion failed'}`;
    await Bun.write(resolve(root, outputFile), output); await Bun.write(resolve(root, reportFile), xml(ok));
    return { id, argv: ['offline fixture'], exitCode: ok ? 0 : 1, timedOut: false, total: 1, passed: ok ? 1 : 0, failed: ok ? 0 : 1, ok, outputFile, reportFile, outputHash: hash(output), seconds: 0 };
  };
  const f: FeatureRecord = { version: 2, requirement, writable: [path], snapshotFile: 'snapshot.json',
    testPlanCall: 'funds-tests', testPlan, testReviewCall: 'funds-test-review', testReview: review, testPath, testPatch,
    testChanges: preparePatch(testPatch, [testPath], { [testPath]: null }), scope,
    baselineChecks: [await check('baseline', 'target', false), await check('baseline', 'regression', true)],
    rounds: [{ index: 1, writeCall: 'funds-write-1', patch, changes: preparePatch(patch, [path], { [path]: before }),
      checks: [await check('round-1', 'target', true), await check('round-1', 'regression', true)],
      reviewCall: 'funds-review-1', review, assessmentCall: 'funds-assess-1', assessment, sourceHashes: { [path]: hash(after) } }],
    context: [{ path, from: 1, to: 1, text: before, sha256: hash(before) }],
    result: { verified: true, generated: true, testPath, tests: ['GoalFundsTests'], writable: [path], recordPath: `${featureDir}/feature.json`, rounds: 1 } };
  const calls: Call[] = [];
  const model = async (objective: string, role: string, output: unknown, schema: z.ZodType) => {
    const stem = `${run}/calls/${objective}`, jsonSchema = z.toJSONSchema(schema);
    const retrieval = { query: 'test', queries: ['test'], pinned: [], method: 'offline fixture', corpusHash: 'fixture-corpus', embeddingModel: 'fixture', hits: [] };
    await Bun.write(resolve(root, `${stem}.prompt.txt`), 'fixture prompt'); await Bun.write(resolve(root, `${stem}.card.md`), 'fixture card');
    await json(resolve(root, `${stem}.schema.json`), jsonSchema); await json(resolve(root, `${stem}.retrieval.json`), retrieval);
    await json(resolve(root, `${stem}.output.json`), output); await Bun.write(resolve(root, `${stem}.stderr.txt`), '');
    await Bun.write(resolve(root, `${stem}.events.jsonl`), [
      { type: 'system', subtype: 'init', model: 'fixture', tools: ['StructuredOutput'], mcp_servers: [] },
      { type: 'result', is_error: false, structured_output: output, total_cost_usd: 0, modelUsage: {} },
    ].map(event => JSON.stringify(event)).join('\n'));
    calls.push({ id: objective, objective, role, provider: 'claude', model: 'fixture', modelsUsed: [], tools: ['StructuredOutput'], cliVersion: null,
      card: { source: '.claude/agents/fixture.md', sourceHash: hash('fixture card'), file: `${stem}.card.md` }, brief: { source: 'fixture-brief.md', sourceHash: hash('fixture') },
      promptSha256: hash('fixture prompt'), schemaSha256: hash(JSON.stringify(jsonSchema)), effort: 'medium', status: 'accepted', exitCode: 0, costUsd: 0,
      attempt: 1, seconds: 0, usage: null, retrieval, pinning: { cited: 0, pinned: 0, truncated: false, dropped: [] },
      promptFile: `${stem}.prompt.txt`, outputFile: `${stem}.output.json`, eventsFile: `${stem}.events.jsonl`, stderrFile: `${stem}.stderr.txt` });
  };
  await model('catalog-1', 'lead-producer', catalog, CatalogSchema);
  await json(resolve(root, `${run}/catalog-1.json`), catalog);
  await model(f.testPlanCall, 'simulation-engineer', testPlan, TestPlanSchema);
  await model(f.testReviewCall, 'code-performance-reviewer', review, ReviewSchema);
  await model('funds-write-1', 'simulation-engineer', patch, PatchSchema);
  await model('funds-review-1', 'code-performance-reviewer', review, ReviewSchema);
  await model('funds-assess-1', 'lead-producer', assessment, FeatureAssessmentSchema);
  const backlog = reconcile(catalog, hash(agentText), hash(humanText), '0.24.0'); backlog.entries[0]!.status = 'verified';
  backlog.catalogOrigin = `${run}/catalog-1.json`;
  const record: CampaignRecord = { version: 2, id: 'campaign-fixture', mode: 'run', status: 'INCOMPLETE', approvedBy: 'offline fixture', startedAt: '2026-10-19',
    limits: { maxCalls: 10, maxUsd: 2, maxIterations: 2, maxFeatures: 1, maxInspections: 5 },
    gdd: { agent: { path: 'GDD/agent.md', version: '0.24.0', sha256: hash(agentText), text: agentText }, human: { path: 'GDD/human.html', version: '0.24.0', sha256: hash(humanText) }, comparisons: [] },
    calls, backlog, catalogSource: backlog.catalogOrigin, inspections: [], features: [{ id: 'funds', directory: featureDir, result: f.result }], rechecks: [], scans: [], manualEdits: [] };
  const save = async () => { await json(resolve(root, `${run}/run.json`), record); await json(resolve(root, `${run}/catalog.json`), record.backlog!.catalog); await json(resolve(root, `${featureDir}/feature.json`), f); await json(resolve(root, `${featureDir}/snapshot.json`), snapshot); };
  await save();
  let replays = 0;
  const replay: Parameters<typeof verifyCampaign>[2] = { check: async (workspace, replayScope, destination) => {
    expect(workspace).not.toBe(root); expect(replayScope).toEqual(scope);
    const stage = destination.endsWith('/baseline') ? 'baseline' : 'round-1';
    expect(await readFile(resolve(workspace, path), 'utf8')).toBe(stage === 'baseline' ? before : after);
    expect(await readFile(resolve(workspace, testPath), 'utf8')).toBe(testPlan.code);
    replays++;
    const checks = stage === 'baseline' ? f.baselineChecks : f.rounds[0]!.checks;
    return Promise.all(checks.map(async c => {
      const reportFile = resolve(destination, `${c.id}.trx`); await Bun.write(reportFile, xml(c.ok));
      return { ...c, reportFile };
    }));
  } };
  return { root, run, featureDir, f, record, snapshot, save, replay, model, count: () => replays };
}

test('offline replay reconstructs red baseline and generated patch without using current game files', async () => {
  const f = await fixture();
  await Bun.write(resolve(f.root, path), 'Later work has completely changed this file.');
  const summary = await verifyCampaign(f.root, f.run, f.replay);
  expect(f.count()).toBe(2); expect(summary.ok).toBe(true); expect(summary.complete).toBe(false);
  expect(summary.features[0]).toMatchObject({ id: 'funds', generated: true, rounds: 1 });
  expect(await readFile(resolve(f.root, path), 'utf8')).toContain('Later work');
});

test('a bounded generated feature cannot turn an unresolved backlog into COMPLETE', async () => {
  const f = await fixture(); f.record.status = 'COMPLETE'; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('unresolved discovered requirements');
  expect(f.count()).toBe(0);
});

test('snapshot tampering and unsafe archive paths fail before executing tests', async () => {
  const f = await fixture(); f.snapshot.files[path]!.text += '// edited'; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('snapshot hash');
  expect(f.count()).toBe(0);
  await expect(verifyCampaign(f.root, '../outside', f.replay)).rejects.toThrow('Unsafe relative path');
});

test('model output edits cannot masquerade as the archived generated patch', async () => {
  const f = await fixture(), call = f.record.calls.find(c => c.objective === 'funds-write-1')!;
  await json(resolve(f.root, call.outputFile), { ...f.f.rounds[0]!.patch, summary: 'Edited after the model run' });
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('output differs from transcript');
  expect(f.count()).toBe(0);
});

test('recorded patch hashes and rescan hashes must match the exact reconstructed files', async () => {
  const f = await fixture(); f.f.rounds[0]!.changes[0]!.afterHash = 'bad'; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('production patch hashes');
  f.f.rounds[0]!.changes[0]!.afterHash = hash(after); f.f.rounds[0]!.sourceHashes![path] = 'bad'; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('rescan source hashes');
});

test('tampered TRX counters and nonassertion red failures do not establish a generated feature', async () => {
  const f = await fixture(), check = f.f.baselineChecks[0]!;
  await Bun.write(resolve(f.root, check.reportFile), xml(true));
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('archived TRX counters');
  await Bun.write(resolve(f.root, check.reportFile), xml(false).replace('Assert.False() Failure', 'NullReferenceException: test fixture setup failed'));
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('not a behavioral assertion failure');
});

test('archived GDD hashes and catalog source quotes remain tied to the actual design', async () => {
  const f = await fixture(); f.record.gdd.agent.text += 'changed'; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('archived agent GDD hash');
  f.record.gdd.agent.text = f.snapshot.files['GDD/agent.md']!.text;
  f.record.backlog!.catalog.requirements[0]!.quote = 'An invented power that does not appear in the actual source design.'; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('no literal GDD source');
});

test('frozen tests must be exactly the reviewed model-generated code', async () => {
  const f = await fixture(); f.f.testChanges![0]!.after += '// manual edit'; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('generated test patch hashes');
});

async function resumedFixture() {
  const f = await fixture(), nextRun = 'runs/goals/resumed-fixture';
  await cp(resolve(f.root, f.run), resolve(f.root, nextRun), { recursive: true });
  const record: CampaignRecord = JSON.parse(JSON.stringify(f.record).replaceAll(f.run, nextRun));
  record.catalogSource = f.record.catalogSource; record.backlog!.catalogOrigin = f.record.catalogSource;
  const feature: FeatureRecord = JSON.parse(JSON.stringify(f.f).replaceAll(f.run, nextRun));
  const snapshot: FeatureSnapshot = JSON.parse(JSON.stringify(f.snapshot));
  feature.priorFeature = `${f.featureDir}/feature.json`;
  feature.testPatch = undefined; feature.testChanges = undefined;
  feature.prewriteReviewCall = 'funds-review-1'; feature.prewriteReview = feature.rounds[0]!.review;
  feature.prewriteAssessmentCall = 'funds-assess-1'; feature.prewriteAssessment = feature.rounds[0]!.assessment;
  feature.baselineChecks = feature.rounds[0]!.checks; feature.rounds = [];
  feature.result = { ...feature.result, generated: false, rounds: 0 };
  record.features[0]!.result = feature.result; record.calls = record.calls.filter(c => ['funds-review-1', 'funds-assess-1'].includes(c.objective));
  snapshot.files[path] = { text: after, sha256: hash(after) };
  snapshot.files[testPath] = { text: feature.testPlan!.code, sha256: hash(feature.testPlan!.code) };
  const featureDir = record.features[0]!.directory;
  const save = async () => { await json(resolve(f.root, `${nextRun}/run.json`), record); await json(resolve(f.root, `${featureDir}/feature.json`), feature); await json(resolve(f.root, `${featureDir}/snapshot.json`), snapshot); };
  await save();
  const replay: Parameters<typeof verifyCampaign>[2] = { check: async (workspace, _scope, destination) => {
    expect(await readFile(resolve(workspace, path), 'utf8')).toBe(after);
    return Promise.all(feature.baselineChecks.map(async c => {
      const reportFile = resolve(destination, `${c.id}.trx`); await Bun.write(reportFile, xml(true)); return { ...c, reportFile };
    }));
  } };
  return { ...f, nextRun, nextRecord: record, nextFeature: feature, nextSnapshot: snapshot, saveNext: save, replayNext: replay };
}

test('resumed evidence follows prior test and writer provenance while replaying the current snapshot', async () => {
  const f = await resumedFixture();
  const summary = await verifyCampaign(f.root, f.nextRun, f.replayNext);
  expect(summary.features[0]).toMatchObject({ generated: false, rounds: 0 });
  expect(summary.features[0]!.stages).toHaveLength(1);
  expect(summary.complete).toBe(false);
});

test('resumed snapshots cannot hide manual production edits or inherit discarded tests', async () => {
  const f = await resumedFixture();
  f.nextSnapshot.files[path] = { text: after + '// edited', sha256: hash(after + '// edited') }; await f.saveNext();
  await expect(verifyCampaign(f.root, f.nextRun, f.replayNext)).rejects.toThrow('resumed production baseline');
  f.nextSnapshot.files[path] = { text: after, sha256: hash(after) }; await f.saveNext();
  f.f.testDiscarded = true; await f.save();
  await expect(verifyCampaign(f.root, f.nextRun, f.replayNext)).rejects.toThrow('discarded tests');
});

async function reusedCatalogFixture() {
  const f = await fixture(), origin = 'runs/goals/original-catalog';
  await cp(resolve(f.root, f.run), resolve(f.root, origin), { recursive: true });
  const originRecord: CampaignRecord = JSON.parse(JSON.stringify(f.record).replaceAll(f.run, origin));
  originRecord.mode = 'scan'; originRecord.features = [];
  originRecord.calls = originRecord.calls.filter(c => c.objective === 'catalog-1');
  await json(resolve(f.root, `${origin}/run.json`), originRecord);
  f.record.calls = f.record.calls.filter(c => c.objective !== 'catalog-1');
  f.record.catalogSource = `${origin}/catalog-1.json`; f.record.backlog!.catalogOrigin = f.record.catalogSource;
  await f.save();
  return { ...f, origin, originRecord };
}

test('reused catalogs retain model provenance from a prior discovery-only campaign', async () => {
  const f = await reusedCatalogFixture();
  const result = await verifyCampaign(f.root, f.run, f.replay);
  expect(result.ok).toBe(true); expect(f.count()).toBe(2);
  expect(f.originRecord.features).toHaveLength(0);
  expect(f.record.calls.some(c => c.objective === 'catalog-1')).toBe(false);
});

test('editing a reused catalog or its archived model output invalidates provenance', async () => {
  const f = await reusedCatalogFixture();
  f.record.backlog!.catalog.summary = 'A manually changed catalog with otherwise valid source quotes.'; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('catalog archive differs from backlog');
  f.record.backlog!.catalog = f.originRecord.backlog!.catalog; await f.save();
  const call = f.originRecord.calls[0]!;
  await json(resolve(f.root, call.outputFile), { ...f.record.backlog!.catalog, summary: 'Tampered original model output' });
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('output differs from transcript');
  expect(f.count()).toBe(0);
});

test('missing or mutable catalog origins cannot bypass model provenance checks', async () => {
  const f = await fixture(); f.record.catalogSource = undefined; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('missing immutable catalog origin');
  f.record.catalogSource = 'runs/goals/backlog.json'; f.record.backlog!.catalogOrigin = f.record.catalogSource; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('invalid immutable catalog origin');
});


test('COMPLETE cannot promote an unproved entry or omit, duplicate, or change catalog entries', async () => {
  const f = await fixture(), entries = structuredClone(f.record.backlog!.entries);
  f.record.backlog!.entries[1]!.status = 'verified'; f.record.status = 'COMPLETE'; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('lacks supported proof');
  for (const changed of [[entries[0]!], [entries[0]!, entries[0]!], [...entries, entries[0]!]]) {
    f.record.backlog!.entries = changed; await f.save();
    await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('catalog/entry mapping');
  }
  f.record.backlog!.entries = entries;
  entries[1]!.requirement.acceptance = ['A replacement criterion not in the discovered catalog.']; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('catalog/entry requirement');
  expect(f.count()).toBe(0);
});

async function retainedFixture() {
  const f = await fixture(), nextRun = 'runs/goals/zero-new-features';
  const entry = f.record.backlog!.entries[0]!;
  entry.proof = { run: f.run, testPath }; entry.featureDirectory = f.featureDir;
  entry.inspectedHashes = { [path]: hash(after), [testPath]: hash(f.f.testPlan!.code) };
  entry.inspection = { state: 'missing', explanation: 'The original inspection found a behavioral gap.', evidence: [{ path, quote: before }],
    tests: ['GoalFundsTests'], reads: [], searches: [], writable: [path] };
  entry.checkMinimums = { GoalFundsTests: 1 };
  await f.save();
  const next: CampaignRecord = { ...structuredClone(f.record), id: 'zero-new-features', calls: [], features: [], rechecks: [] };
  const outputFile = `${nextRun}/rechecks/funds/acceptance-1.log`, reportFile = `${nextRun}/rechecks/funds/acceptance-1.trx`;
  const check: CheckResult = { ...f.f.rounds[0]!.checks[0]!, id: 'acceptance-1', outputFile, reportFile, outputHash: hash('retained acceptance passed'),
    argv: ['dotnet', 'test', 'sim/Suborned.Sim.Tests/Suborned.Sim.Tests.csproj', '--filter', 'FullyQualifiedName~GoalFundsTests', '--logger', `trx;LogFileName=${resolve(f.root, reportFile)}`, '--verbosity', 'minimal'] };
  await Bun.write(resolve(f.root, outputFile), 'retained acceptance passed'); await Bun.write(resolve(f.root, reportFile), xml(true));
  next.rechecks = [{ id: 'funds', checks: [check] }];
  const saveNext = async () => { await json(resolve(f.root, `${nextRun}/run.json`), next); await json(resolve(f.root, `${nextRun}/catalog.json`), next.backlog!.catalog); };
  await saveNext();
  const replay: Parameters<typeof verifyCampaign>[2] = { check: async (workspace, scope, destination) => {
    if (!destination.endsWith('/recheck')) return f.replay.check(workspace, scope, destination);
    expect(scope).toEqual(checkScope('funds', ['GoalFundsTests'], { GoalFundsTests: 1 }));
    expect(await readFile(resolve(workspace, path), 'utf8')).toBe(after);
    expect(await readFile(resolve(workspace, testPath), 'utf8')).toBe(f.f.testPlan!.code);
    return [check];
  } };
  return { ...f, nextRun, next, check, saveNext, replayRetained: replay };
}

test('zero-new-feature rerun replays retained provenance and current acceptance checks in isolation', async () => {
  const f = await retainedFixture();
  await Bun.write(resolve(f.root, path), 'Unrelated later work must never be used for offline verification.');
  const summary = await verifyCampaign(f.root, f.nextRun, f.replayRetained);
  expect(summary.ok).toBe(true); expect(summary.features[0]!.stages.map(s => s.stage)).toEqual(['baseline', 'round-1', 'recheck']);
  expect(f.next.features).toHaveLength(0); expect(f.next.calls).toHaveLength(0);
  expect(await readFile(resolve(f.root, path), 'utf8')).toContain('Unrelated later work');
});

test('retained proof requires current logs, valid TRX, selectors, source hashes and undiminished test minimums', async () => {
  const f = await retainedFixture(), entry = f.next.backlog!.entries[0]!;
  f.next.rechecks = []; await f.saveNext();
  await expect(verifyCampaign(f.root, f.nextRun, f.replayRetained)).rejects.toThrow('missing current recheck');
  f.next.rechecks = [{ id: 'funds', checks: [f.check] }]; await f.saveNext();
  await Bun.write(resolve(f.root, f.check.outputFile), 'edited log');
  await expect(verifyCampaign(f.root, f.nextRun, f.replayRetained)).rejects.toThrow('check output hash');
  await Bun.write(resolve(f.root, f.check.outputFile), 'retained acceptance passed');
  await Bun.write(resolve(f.root, f.check.reportFile), xml(false));
  await expect(verifyCampaign(f.root, f.nextRun, f.replayRetained)).rejects.toThrow('archived TRX counters');
  await Bun.write(resolve(f.root, f.check.reportFile), xml(true));
  entry.checkMinimums = { GoalFundsTests: 0 }; await f.saveNext();
  await expect(verifyCampaign(f.root, f.nextRun, f.replayRetained)).rejects.toThrow('retained test minimum');
  entry.checkMinimums = { GoalFundsTests: 2 }; await f.saveNext();
  await expect(verifyCampaign(f.root, f.nextRun, f.replayRetained)).rejects.toThrow('archived TRX counters');
  entry.checkMinimums = { GoalFundsTests: 1 }; entry.inspection!.tests = ['UnrelatedTests']; await f.saveNext();
  await expect(verifyCampaign(f.root, f.nextRun, f.replayRetained)).rejects.toThrow('retained test selectors');
  entry.inspection!.tests = ['GoalFundsTests']; entry.inspectedHashes[path] = hash(before); await f.saveNext();
  await expect(verifyCampaign(f.root, f.nextRun, f.replayRetained)).rejects.toThrow('verified source hash');
});

test('retained proof cannot substitute another requirement or discard the archived review', async () => {
  const f = await retainedFixture();
  f.next.backlog!.entries[1]!.status = 'verified'; f.next.backlog!.entries[1]!.proof = { run: f.run, testPath }; await f.saveNext();
  await expect(verifyCampaign(f.root, f.nextRun, f.replayRetained)).rejects.toThrow('missing retained feature proof');
  f.next.backlog!.entries[1]!.status = 'uninspected'; await f.saveNext();
  f.f.rounds[0]!.review = undefined; await f.save();
  await expect(verifyCampaign(f.root, f.nextRun, f.replayRetained)).rejects.toThrow('final review did not pass');
});

async function implementedFixture(pass = 1) {
  const f = await fixture(), id = 'replay', directory = `${f.run}/inspections/${id}`;
  const entry = f.record.backlog!.entries[1]!;
  const result = { state: 'implemented' as const, explanation: 'Inspected execution and tests establish the requirement.',
    evidence: [{ path, quote: before }], reads: [], searches: [], tests: ['ExistingTests'], writable: [] };
  const input = { requirement: entry.requirement, context: [
    { path, from: 1, to: 1, text: before, sha256: hash(before) },
    { path: 'sim/Suborned.Sim.Tests/ExistingTests.cs', from: 1, to: 1, text: 'public class ExistingTests {}', sha256: hash('public class ExistingTests {}') },
  ] };
  await f.model(`${id}-inspect-${pass}`, 'simulation-engineer', result, InspectionSchema);
  const call = f.record.calls.at(-1)!;
  const prompt = `Inspect this archived input: ${JSON.stringify(input)}`;
  call.promptSha256 = hash(prompt); await Bun.write(resolve(f.root, call.promptFile), prompt);
  await json(resolve(f.root, `${directory}/inspection-${pass}.input.json`), input);
  f.record.inspections.push({ id, directory, result });
  const outputFile = `${f.run}/existing/${id}/acceptance-1.log`, reportFile = `${f.run}/existing/${id}/acceptance-1.trx`;
  const check = { ...f.f.rounds[0]!.checks[0]!, id: 'acceptance-1', outputFile, reportFile, outputHash: hash('existing passed'),
    argv: ['dotnet', 'test', 'sim/Suborned.Sim.Tests/Suborned.Sim.Tests.csproj', '--filter', 'FullyQualifiedName~ExistingTests', '--logger', `trx;LogFileName=${resolve(f.root, reportFile)}`, '--verbosity', 'minimal'] };
  await Bun.write(resolve(f.root, outputFile), 'existing passed'); await Bun.write(resolve(f.root, reportFile), xml(true));
  await json(resolve(f.root, `${f.run}/existing/${id}/checks.json`), [check]);
  Object.assign(entry, { status: 'verified', inspection: result, inspectedHashes: Object.fromEntries(input.context.map(c => [c.path, c.sha256])),
    checks: [reportFile], checkMinimums: { ExistingTests: 1 } });
  f.record.status = 'COMPLETE'; await f.save();
  return { ...f, entry, input, directory, check };
}

test('pre-existing implementation can be verified through supported inspection and archived checks', async () => {
  const f = await implementedFixture();
  const summary = await verifyCampaign(f.root, f.run, f.replay);
  expect(summary.complete).toBe(true); expect(summary.inspected).toEqual(['replay']);
  f.input.context[0]!.text = 'Replaced inspected production source';
  await json(resolve(f.root, `${f.directory}/inspection-1.input.json`), f.input);
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('inspection input absent from prompt');
});

test('implemented status alone and invented or empty check reports cannot establish completion', async () => {
  const f = await implementedFixture();
  await Bun.write(resolve(f.root, f.check.reportFile), xml(true).replaceAll('="1"', '="0"'));
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('archived TRX counters');
  await Bun.write(resolve(f.root, f.check.reportFile), xml(true));
  f.record.inspections = []; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('missing supported inspection');
});

// Opt in to integration coverage; the ordinary coordinator suite needs no .NET installation.
// GOALS_DOTNET_TESTS=1 bun test tools/goals/test/verify-campaign.test.ts
test.skipIf(process.env.GOALS_DOTNET_TESTS !== '1')('real .NET zero-new-feature rerun replays red/green retained proof and acceptance tests', async () => {
  const f = await retainedFixture();
  const files = {
    'sim/Suborned.Sim/Suborned.Sim.csproj': '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>',
    'sim/Suborned.Sim.Tests/Suborned.Sim.Tests.csproj': await readFile(resolve(import.meta.dir, '../../../sim/Suborned.Sim.Tests/Suborned.Sim.Tests.csproj'), 'utf8'),
    'sim/Suborned.Sim.Tests/ExistingTests.cs': 'using Xunit; public class ExistingTests { [Fact] public void Regression() { Assert.True(true); } }',
  };
  for (const [source, text] of Object.entries(files)) f.snapshot.files[source] = { text, sha256: hash(text) };
  for (const [source, file] of Object.entries(f.snapshot.files)) await Bun.write(resolve(f.root, source), file.text);
  await Bun.write(resolve(f.root, testPath), f.f.testPlan!.code);
  f.f.baselineChecks = await runChecks(f.root, f.f.scope!, resolve(f.root, f.featureDir, 'baseline'));
  expect(f.f.baselineChecks.map(c => c.ok)).toEqual([false, true]);
  await Bun.write(resolve(f.root, path), after);
  f.f.rounds[0]!.checks = await runChecks(f.root, f.f.scope!, resolve(f.root, f.featureDir, 'round-1'));
  expect(f.f.rounds[0]!.checks.every(c => c.ok)).toBe(true);
  f.next.rechecks[0]!.checks = await runChecks(f.root, checkScope('funds', ['GoalFundsTests'], { GoalFundsTests: 1 }), resolve(f.root, f.nextRun, 'rechecks/funds'));
  expect(f.next.rechecks[0]!.checks.every(c => c.ok)).toBe(true);
  await f.save(); await f.saveNext();
  await Bun.write(resolve(f.root, path), 'Later source is deliberately unbuildable.');
  const summary = await verifyCampaign(f.root, f.nextRun);
  expect(summary.ok).toBe(true); expect(summary.features[0]!.stages.map(s => s.stage)).toEqual(['baseline', 'round-1', 'recheck']);
  expect(f.next.calls).toHaveLength(0); expect(f.next.features).toHaveLength(0);
}, 300000);


test('existing-only zero-feature reruns retain supported inspection and require fresh acceptance evidence', async () => {
  const f = await implementedFixture(), run = 'runs/goals/existing-rerun';
  const next = structuredClone(f.record);
  next.id = 'existing-rerun'; next.status = 'INCOMPLETE'; next.calls = []; next.features = []; next.inspections = [];
  next.backlog!.entries[0]!.status = 'uninspected';
  const check = { ...f.check, outputFile: `${run}/rechecks/replay/acceptance-1.log`, reportFile: `${run}/rechecks/replay/acceptance-1.trx`, argv: [...f.check.argv] };
  check.argv[6] = `trx;LogFileName=${resolve(f.root, check.reportFile)}`;
  next.rechecks = [{ id: 'replay', checks: [check] }];
  await Bun.write(resolve(f.root, check.outputFile), 'existing passed'); await Bun.write(resolve(f.root, check.reportFile), xml(true));
  await json(resolve(f.root, `${run}/run.json`), next); await json(resolve(f.root, `${run}/catalog.json`), next.backlog!.catalog);
  const summary = await verifyCampaign(f.root, run, { check: async () => { throw new Error('Inspection excerpts cannot claim source replay'); } });
  expect(summary.ok).toBe(true); expect(summary.features).toHaveLength(0); expect(summary.inspected).toEqual(['replay']);
  check.argv[4] = 'FullyQualifiedName~UnrelatedTests'; await json(resolve(f.root, `${run}/run.json`), next);
  await expect(verifyCampaign(f.root, run, f.replay)).rejects.toThrow('check invocation');
  check.argv[4] = 'FullyQualifiedName~ExistingTests'; next.rechecks = []; await json(resolve(f.root, `${run}/run.json`), next);
  await expect(verifyCampaign(f.root, run, f.replay)).rejects.toThrow('missing current recheck');
});


test('supported inspection provenance accepts later coordinator inspection exchanges', async () => {
  const f = await implementedFixture(4);
  expect((await verifyCampaign(f.root, f.run, f.replay)).complete).toBe(true);
});
