import { afterEach, expect, test } from 'bun:test';
import { mkdtemp, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { resolve } from 'node:path';
import { z } from 'zod';
import type { Call } from '../../rag/agent';
import { hash } from '../../rag/corpus';
import { json } from '../files';
import { preparePatch } from '../patch';
import type { RunRecord } from '../record';
import { AnalysisSchema, PatchSchema, type Analysis, type CheckResult, type Scope } from '../schema';
import { rank, scan } from '../scan';
import { verifyRun } from '../verify';

const roots: string[] = [];
afterEach(async () => { for (const root of roots.splice(0)) await rm(root, { recursive: true, force: true }); });

async function fixture() {
  const root = await mkdtemp(resolve(tmpdir(), 'a05-evidence-test-')); roots.push(root);
  const source = 'GDD requirement: reject an order that lacks sufficient funds.';
  const code = 'namespace Suborned.Sim; public class Rules { public bool Allowed => true; }';
  const path = 'sim/Suborned.Sim/Rules.cs';
  const scope: Scope = {
    id: 'test-scope', goal: 'Reject unaffordable orders', sources: [{ path: 'GDD.md', quote: source }],
    requirements: [{ id: 'funds', title: 'Funds', source: 0, owner: 'simulation-engineer', dependsOn: [], checks: ['funds'] }],
    context: [{ path }], writable: [path], testProject: 'sim/tests/tests.csproj',
    checks: [{ id: 'funds', filter: 'Funds', minTests: 1 }], regression: 'funds',
  };
  await Bun.write(resolve(root, 'GDD.md'), source);
  await Bun.write(resolve(root, path), code);
  await json(resolve(root, 'scope.json'), scope);
  const patch = { summary: 'Reject unaffordable orders', edits: [{ path, search: '=> true', replacement: '=> false' }] };
  const baseline = { [path]: code };
  const record: RunRecord = {
    version: 1, id: 'test', scopePath: 'scope.json', scope, status: 'COMPLETE', approvedBy: 'offline fixture', startedAt: '2026-10-19',
    limits: { maxUsd: 1, maxCalls: 4, maxIterations: 1 },
    inputs: { 'GDD.md': hash(source), 'scope.json': hash(await Bun.file(resolve(root, 'scope.json')).text()), [path]: hash(code) },
    baseline, baselineChecks: [], rounds: [{ index: 1, selected: 'funds', ranking: [], patch, changes: preparePatch(patch, scope.writable, baseline), checks: [] }],
    calls: [], manualEdits: [],
  };
  const save = () => json(resolve(root, 'runs/test/run.json'), record);
  await save();
  return { root, record, save, path };
}

test('offline replay refuses an edited immutable input before running tests', async () => {
  const f = await fixture(); f.record.inputs['GDD.md'] = '0'.repeat(64); await f.save();
  await expect(verifyRun(f.root, 'runs/test')).rejects.toThrow('immutable input changed');
});

test('offline replay refuses modified recorded patch hashes', async () => {
  const f = await fixture(); f.record.rounds[0]!.changes[0]!.afterHash = '0'.repeat(64); await f.save();
  await expect(verifyRun(f.root, 'runs/test')).rejects.toThrow('recorded patch hashes/content');
});

test('offline replay refuses manual implementation edits outside the recorded patches', async () => {
  const f = await fixture();
  await expect(verifyRun(f.root, 'runs/test')).rejects.toThrow('current implementation differs');
});

test('offline replay rejects escaping input and run paths', async () => {
  const f = await fixture();
  await expect(verifyRun(f.root, '../outside')).rejects.toThrow('Unsafe relative path');
  f.record.inputs['../outside'] = '0'.repeat(64); await f.save();
  await expect(verifyRun(f.root, 'runs/test')).rejects.toThrow('Unsafe relative path');
});

test('an accepted label cannot turn a failed transcript into generation evidence', async () => {
  const f = await fixture(), stem = 'runs/test/calls/01-write-1', schema = z.toJSONSchema(PatchSchema);
  await Bun.write(resolve(f.root, `${stem}.prompt.txt`), 'prompt');
  await Bun.write(resolve(f.root, `${stem}.card.md`), 'card');
  await json(resolve(f.root, `${stem}.schema.json`), schema);
  const retrieval = { query: 'test', queries: ['test'], pinned: [], method: 'offline fixture', corpusHash: 'test', embeddingModel: 'test', hits: [] };
  await json(resolve(f.root, `${stem}.retrieval.json`), retrieval);
  await Bun.write(resolve(f.root, `${stem}.events.jsonl`), '{}\n');
  await Bun.write(resolve(f.root, `${stem}.stderr.txt`), '');
  const call: Call = {
    id: '01-write-1', objective: 'write-1', status: 'accepted', exitCode: 0, model: null, modelsUsed: [], costUsd: null, tools: null,
    role: 'simulation-engineer', provider: 'claude', cliVersion: null, effort: 'medium', attempt: 1, seconds: 1, usage: null,
    pinning: { cited: 0, pinned: 0, truncated: false, dropped: [] },
    promptFile: `${stem}.prompt.txt`, promptSha256: hash('prompt'), schemaSha256: hash(JSON.stringify(schema)),
    card: { file: `${stem}.card.md`, source: '.claude/agents/simulation-engineer.md', sourceHash: hash('card') }, retrieval,
    eventsFile: `${stem}.events.jsonl`, stderrFile: `${stem}.stderr.txt`, outputFile: `${stem}.output.json`,
    brief: { source: 'GDD.md', sourceHash: f.record.inputs['GDD.md'] },
  };
  f.record.calls = [call]; await f.save();
  await expect(verifyRun(f.root, 'runs/test')).rejects.toThrow('accepted call has invalid transcript');
});

test('already-complete readiness runs replay their baseline without claiming a generated feature', async () => {
  const f = await fixture(), stem = 'runs/test/calls/01-assess-0';
  const analysis: Analysis = { summary: 'Already implemented', features: [{ id: 'funds', state: 'satisfied', explanation: 'Source and pinned checks agree',
    evidence: [{ path: f.path, quote: f.record.baseline[f.path]! }], value: 4, effort: 1, risk: 1, reason: 'Already present' }] };
  const outputFile = 'runs/test/baseline/funds.log', reportFile = 'runs/test/baseline/funds.trx';
  await Bun.write(resolve(f.root, outputFile), 'passing fixture log');
  await Bun.write(resolve(f.root, reportFile), '<TestRun><ResultSummary outcome="Completed"><Counters total="1" executed="1" passed="1" failed="0"/></ResultSummary></TestRun>');
  const check: CheckResult = { id: 'funds', argv: ['offline-fixture'], exitCode: 0, timedOut: false, total: 1, passed: 1, failed: 0, ok: true,
    outputFile, reportFile, outputHash: hash('passing fixture log'), seconds: 0 };
  f.record.rounds = [];
  f.record.baselineChecks = [check];
  f.record.finalScan = await scan(f.root, f.record.scope, [check]);
  f.record.finalAnalysis = analysis;
  f.record.ranking = rank(f.record.scope, f.record.finalScan, analysis);
  const schema = z.toJSONSchema(AnalysisSchema);
  const retrieval = { query: 'test', queries: ['test'], pinned: [], method: 'offline fixture', corpusHash: 'test', embeddingModel: 'test', hits: [] };
  await Bun.write(resolve(f.root, `${stem}.prompt.txt`), 'prompt');
  await Bun.write(resolve(f.root, `${stem}.card.md`), 'card');
  await json(resolve(f.root, `${stem}.schema.json`), schema);
  await json(resolve(f.root, `${stem}.retrieval.json`), retrieval);
  await json(resolve(f.root, `${stem}.output.json`), analysis);
  await Bun.write(resolve(f.root, `${stem}.events.jsonl`), [
    { type: 'system', subtype: 'init', model: 'fixture', tools: ['StructuredOutput'], mcp_servers: [] },
    { type: 'result', is_error: false, structured_output: analysis, total_cost_usd: 0, usage: null, modelUsage: {} },
  ].map(event => JSON.stringify(event)).join('\n'));
  await Bun.write(resolve(f.root, `${stem}.stderr.txt`), '');
  f.record.calls = [{ id: '01-assess-0', objective: 'assess-0', status: 'accepted', exitCode: 0, model: 'fixture', modelsUsed: [], costUsd: 0, tools: ['StructuredOutput'],
    role: 'lead-producer', provider: 'claude', cliVersion: null, effort: 'medium', attempt: 1, seconds: 1, usage: null,
    pinning: { cited: 0, pinned: 0, truncated: false, dropped: [] },
    promptFile: `${stem}.prompt.txt`, promptSha256: hash('prompt'), schemaSha256: hash(JSON.stringify(schema)),
    card: { file: `${stem}.card.md`, source: '.claude/agents/lead-producer.md', sourceHash: hash('card') }, retrieval,
    eventsFile: `${stem}.events.jsonl`, stderrFile: `${stem}.stderr.txt`, outputFile: `${stem}.output.json`,
    brief: { source: 'GDD.md', sourceHash: f.record.inputs['GDD.md']! },
  }];
  await f.save();
  let replays = 0;
  const summary = await verifyRun(f.root, 'runs/test', { check: async (replayRoot) => {
    expect(replayRoot).not.toBe(f.root);
    expect(await Bun.file(resolve(replayRoot, f.path)).text()).toBe(f.record.baseline[f.path]!);
    replays++; return [check];
  } });
  expect(replays).toBe(1);
  expect(summary).toMatchObject({ ok: true, rounds: 0, generatedFeature: false, requirements: 1 });
});
