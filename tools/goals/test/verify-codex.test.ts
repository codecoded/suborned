import { afterEach, expect, test } from 'bun:test';
import { cp, mkdtemp, readFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { resolve } from 'node:path';
import { z } from 'zod';
import type { Call } from '../../rag/agent';
import { hash } from '../../rag/corpus';
import { CatalogSchema, reconcile, TestPlanSchema, type Catalog } from '../backlog';
import type { CampaignRecord } from '../campaign';
import { FeatureAssessmentSchema, type FeatureRecord, type FeatureSnapshot } from '../feature';
import { json } from '../files';
import { preparePatch } from '../patch';
import { PatchSchema, ReviewSchema, type CheckResult, type CodePatch, type Scope } from '../schema';
import { verifyCampaign } from '../verify-campaign';
import { CODEX_VERSION, codexArgs, codexConfig, codexOutputSchema } from '../codex';

const roots: string[] = [];
const modelName = 'gpt-6-astra';
const usage = { input_tokens: 20, cached_input_tokens: 0, output_tokens: 10 };
const events = (output: unknown, model = modelName, schema = z.toJSONSchema(CatalogSchema)) => {
  const options = { model, effort: 'medium', timeoutMs: 300000 }, card = { body: 'fixture card' };
  return [
    { type: 'suborned.codex.audit', version: 2, promptSha256: hash('fixture prompt'), cliVersion: CODEX_VERSION,
      modelSource: 'explicit --model; not server-attested', ...options, cwd: '/tmp/suborned-codex-fixture/work',
      argv: codexArgs(card, '/tmp/suborned-codex-fixture/schema.json', '/tmp/suborned-codex-fixture/last-message.txt', options),
      config: codexConfig(card, options), schema, outputSchema: codexOutputSchema(schema) },
    { type: 'thread.started', thread_id: 'offline-fixture', model },
    { type: 'turn.started' },
    { type: 'item.completed', item: { id: 'answer', type: 'agent_message', text: JSON.stringify(output) } },
    { type: 'turn.completed', usage },
    { type: 'suborned.codex.capture', exitCode: 0, timedOut: false, lastMessage: JSON.stringify(output) },
  ].map(event => JSON.stringify(event)).join('\n');
};
afterEach(async () => { for (const root of roots.splice(0)) await rm(root, { recursive: true, force: true }); });

const path = 'sim/Suborned.Sim/Game.cs', testPath = 'sim/Suborned.Sim.Tests/GoalFundsTests.cs';
const before = 'namespace Suborned.Sim; public class Game { public bool CanAfford => true; }';
const after = before.replace('=> true', '=> false');
const xml = (ok: boolean) => `<TestRun><Results>${ok ? '' : '<UnitTestResult outcome="Failed"><Output><ErrorInfo><Message>Assert.False() Failure</Message></ErrorInfo></Output></UnitTestResult>'}</Results><ResultSummary outcome="${ok ? 'Completed' : 'Failed'}"><Counters total="1" executed="1" passed="${ok ? 1 : 0}" failed="${ok ? 0 : 1}"/></ResultSummary></TestRun>`;

async function fixture() {
  const root = await mkdtemp(resolve(tmpdir(), 'a05-codex-verify-')); roots.push(root);
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
    const cardText = `name = ${JSON.stringify(role)}\ndeveloper_instructions = "fixture card"\n`;
    await Bun.write(resolve(root, `${stem}.prompt.txt`), 'fixture prompt'); await Bun.write(resolve(root, `${stem}.card.md`), cardText);
    await json(resolve(root, `${stem}.schema.json`), jsonSchema); await json(resolve(root, `${stem}.retrieval.json`), retrieval);
    await json(resolve(root, `${stem}.output.json`), output); await Bun.write(resolve(root, `${stem}.stderr.txt`), '');
    await Bun.write(resolve(root, `${stem}.events.jsonl`), events(output, modelName, jsonSchema));
    calls.push({ id: objective, objective, role, provider: 'codex', model: modelName, modelsUsed: [modelName], tools: [], cliVersion: CODEX_VERSION,
      card: { source: `.codex/agents/${role}.toml`, sourceHash: hash(cardText), file: `${stem}.card.md` }, brief: { source: 'fixture-brief.md', sourceHash: hash('fixture') },
      promptSha256: hash('fixture prompt'), schemaSha256: hash(JSON.stringify(jsonSchema)), effort: 'medium', status: 'accepted', exitCode: 0, costUsd: null,
      attempt: 1, seconds: 0, usage, retrieval, pinning: { cited: 0, pinned: 0, truncated: false, dropped: [] },
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
    limits: { provider: 'codex', model: modelName, maxTokens: 1000, maxCalls: 10, maxIterations: 2, maxFeatures: 1, maxInspections: 5 },
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
  return { root, run, featureDir, f, record, snapshot, save, replay, count: () => replays };
}

test('Codex evidence replays offline with unavailable dollar cost and known bounded tokens', async () => {
  const f = await fixture();
  const summary = await verifyCampaign(f.root, f.run, f.replay);
  expect(summary.ok).toBe(true); expect(summary.complete).toBe(false); expect(f.count()).toBe(2);
});

test('Codex transcript output and recorded usage must agree exactly', async () => {
  const f = await fixture(), call = f.record.calls[0]!;
  await json(resolve(f.root, call.outputFile), { ...f.record.backlog!.catalog, summary: 'Edited output' });
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('output differs from transcript');
  await json(resolve(f.root, call.outputFile), f.record.backlog!.catalog);
  call.usage = { ...usage, output_tokens: 11 }; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('token usage');
  expect(f.count()).toBe(0);
});

test('Codex authorization checks model, provider and CLI version against transcript metadata', async () => {
  const f = await fixture(), call = f.record.calls[0]!;
  call.model = 'another-model'; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('authorized model');
  call.model = modelName;
  await Bun.write(resolve(f.root, call.eventsFile), events(f.record.backlog!.catalog, 'another-model'));
  await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow();
  await Bun.write(resolve(f.root, call.eventsFile), events(f.record.backlog!.catalog));
  call.cliVersion = 'edited version'; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('CLI version');
  call.cliVersion = CODEX_VERSION;
  (call as unknown as { provider: string }).provider = 'unknown'; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('unknown call provider');
  call.provider = 'claude'; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('provider differs');
  expect(f.count()).toBe(0);
});

test('Codex rejects unknown usage, invented dollar costs and exhausted token authorization', async () => {
  const f = await fixture(), call = f.record.calls[0]!;
  for (const invalid of [null, {}, { input_tokens: 20 }, { ...usage, output_tokens: -1 }, { ...usage, input_tokens: '20' }]) {
    call.usage = invalid; await f.save();
    await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('unknown Codex token usage');
  }
  call.usage = usage; call.costUsd = 0; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('dollar cost must be unavailable');
  call.costUsd = null; f.record.limits.maxTokens = 1; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('token limit exceeded');
  expect(f.count()).toBe(0);
});

test('tool activity and fake or mixed-provider events cannot establish Codex evidence', async () => {
  const f = await fixture(), call = f.record.calls[0]!, original = events(f.record.backlog!.catalog);
  for (const event of [
    { type: 'item.completed', item: { id: 'shell', type: 'command_execution', command: 'true', exit_code: 0 } },
    { type: 'item.started', item: { id: 'mcp', type: 'mcp_tool_call', server: 'fake', tool: 'write' } },
    { type: 'result', is_error: false, structured_output: f.record.backlog!.catalog },
    { type: 'fake.success', output: f.record.backlog!.catalog },
  ]) {
    await Bun.write(resolve(f.root, call.eventsFile), original.split('\n').toSpliced(3, 0, JSON.stringify(event)).join('\n'));
    await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('invalid Codex transcript');
  }
  // Relabelling a Claude stream as Codex cannot supply the invocation/isolation proof.
  await Bun.write(resolve(f.root, call.eventsFile), [
    { type: 'system', subtype: 'init', model: modelName, tools: [], mcp_servers: [] },
    { type: 'result', is_error: false, structured_output: f.record.backlog!.catalog, usage },
  ].map(e => JSON.stringify(e)).join('\n'));
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow();
  expect(f.count()).toBe(0);
});

test('Codex cannot masquerade as a Claude campaign even with fabricated dollar metadata', async () => {
  const f = await fixture();
  f.record.limits = { maxCalls: 10, maxUsd: 2, maxIterations: 2, maxFeatures: 1, maxInspections: 5 };
  for (const call of f.record.calls) { call.provider = 'claude'; call.costUsd = 0; }
  await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow();
  expect(f.count()).toBe(0);
});

test('audited Codex schema and effort remain bound to the recorded call', async () => {
  const f = await fixture(), call = f.record.calls[0]!;
  const schema = { ...z.toJSONSchema(CatalogSchema), description: 'A different invocation schema' };
  await Bun.write(resolve(f.root, call.eventsFile), events(f.record.backlog!.catalog, modelName, schema));
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('Codex invocation schema');
  await Bun.write(resolve(f.root, call.eventsFile), events(f.record.backlog!.catalog));
  call.effort = 'high'; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('Codex invocation effort');
  expect(f.count()).toBe(0);
});

async function reusedCatalog() {
  const f = await fixture(), origin = 'runs/goals/original-codex-catalog';
  await cp(resolve(f.root, f.run), resolve(f.root, origin), { recursive: true });
  const record: CampaignRecord = JSON.parse(JSON.stringify(f.record).replaceAll(f.run, origin));
  record.mode = 'scan'; record.features = []; record.calls = record.calls.filter(c => c.objective === 'catalog-1');
  record.limits.model = 'prior-codex-model';
  const call = record.calls[0]!; call.model = record.limits.model; call.modelsUsed = [call.model];
  await Bun.write(resolve(f.root, call.eventsFile), events(record.backlog!.catalog, call.model));
  await json(resolve(f.root, `${origin}/run.json`), record);
  f.record.calls = f.record.calls.filter(c => c.objective !== 'catalog-1');
  f.record.catalogSource = `${origin}/catalog-1.json`; f.record.backlog!.catalogOrigin = f.record.catalogSource;
  await f.save();
  return { ...f, origin, originRecord: record };
}

test('prior catalog is verified against its own authorized model and token budget', async () => {
  const f = await reusedCatalog();
  expect((await verifyCampaign(f.root, f.run, f.replay)).ok).toBe(true);
  f.originRecord.limits.model = modelName;
  await json(resolve(f.root, `${f.origin}/run.json`), f.originRecord);
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('authorized model');
});

test('a prior Claude catalog remains valid for a current Codex campaign', async () => {
  const f = await reusedCatalog(), call = f.originRecord.calls[0]!;
  f.originRecord.limits = { maxCalls: 10, maxUsd: 2, maxIterations: 2, maxFeatures: 1, maxInspections: 5 };
  Object.assign(call, { provider: 'claude', model: 'fixture', modelsUsed: [], tools: ['StructuredOutput'], cliVersion: null, usage: null, costUsd: 0 });
  await Bun.write(resolve(f.root, call.eventsFile), [
    { type: 'system', subtype: 'init', model: 'fixture', tools: ['StructuredOutput'], mcp_servers: [] },
    { type: 'result', is_error: false, structured_output: f.originRecord.backlog!.catalog, total_cost_usd: 0, modelUsage: {} },
  ].map(e => JSON.stringify(e)).join('\n'));
  await json(resolve(f.root, `${f.origin}/run.json`), f.originRecord);
  expect((await verifyCampaign(f.root, f.run, f.replay)).ok).toBe(true);
  call.provider = 'codex'; await json(resolve(f.root, `${f.origin}/run.json`), f.originRecord);
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('provider differs');
});

test('a final in-flight Codex call may cross the threshold, but another call may not start', async () => {
  const f = await fixture();
  // Six calls consume 30 tokens each: the sixth is authorized at 150, crossing 160.
  f.record.limits.maxTokens = 160; await f.save();
  expect((await verifyCampaign(f.root, f.run, f.replay)).ok).toBe(true);
  f.record.limits.maxTokens = 150; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('before a subsequent call');
});

test('goal-scoped backlog and catalog origin cannot be relabelled as a broader completion', async () => {
  const f = await reusedCatalog();
  f.record.limits.goal = 'Reject unaffordable orders'; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('backlog goal differs');
  f.record.backlog!.goal = f.record.limits.goal; await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('different goal scope');
});


test('Codex invocation must use the archived TOML role and exact developer instructions', async () => {
  const f = await fixture(), call = f.record.calls[0]!;
  const lines = (await readFile(resolve(f.root, call.eventsFile), 'utf8')).split('\n').map(line => JSON.parse(line));
  const audit = lines[0], card = { body: 'Different instructions from the saved role.' };
  audit.config = codexConfig(card, audit);
  audit.argv = codexArgs(card, '/tmp/suborned-codex-fixture/schema.json', '/tmp/suborned-codex-fixture/last-message.txt', audit);
  await Bun.write(resolve(f.root, call.eventsFile), lines.map(line => JSON.stringify(line)).join('\n'));
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('Codex invocation developer instructions');
  await Bun.write(resolve(f.root, call.eventsFile), events(f.record.backlog!.catalog));
  const text = 'name = "wrong-role"\ndeveloper_instructions = "fixture card"\n';
  await Bun.write(resolve(f.root, call.card.file), text); call.card.sourceHash = hash(text); await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('Codex card role');
  expect(f.count()).toBe(0);
});

test('accepted Codex calls require v2 prompt hashes matching the saved invocation', async () => {
  const f = await fixture(), call = f.record.calls[0]!;
  const original = (await readFile(resolve(f.root, call.eventsFile), 'utf8')).split('\n').map(line => JSON.parse(line));
  for (const change of [{ version: 1, promptSha256: undefined }, { version: 2, promptSha256: undefined }, { version: 2, promptSha256: hash('another prompt') }]) {
    const lines = structuredClone(original); Object.assign(lines[0], change);
    await Bun.write(resolve(f.root, call.eventsFile), lines.map(line => JSON.stringify(line)).join('\n'));
    await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('prompt hash');
  }
  await Bun.write(resolve(f.root, call.eventsFile), original.map(line => JSON.stringify(line)).join('\n'));
  const prompt = 'Modified prompt with a consistently modified file hash'; call.promptSha256 = hash(prompt);
  await Bun.write(resolve(f.root, call.promptFile), prompt); await f.save();
  await expect(verifyCampaign(f.root, f.run, f.replay)).rejects.toThrow('prompt hash');
  expect(f.count()).toBe(0);
});
