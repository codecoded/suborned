// Offline evidence replay. All source inputs come from the archived feature snapshot;
// later game changes cannot silently alter the baseline being verified.
import { mkdir, mkdtemp, readFile, realpath, rm, writeFile } from 'node:fs/promises';
import { dirname, isAbsolute, relative, resolve } from 'node:path';
import { isDeepStrictEqual } from 'node:util';
import { z } from 'zod';
import type { Call } from '../rag/agent';
import { interpretClaude } from '../rag/claude';
import { hash } from '../rag/corpus';
import { anchorOf, CatalogSchema, InspectionSchema, TestPlanSchema, complete, validateCatalog } from './backlog';
import { checkScope, validateInspection, type CampaignRecord } from './campaign';
import type { CodeSource } from './discovery-sources';
import { parseTrx, runChecks } from './checks';
import { interpretCodex, tokenUsage } from './codex';
import { json, localPath, portable, type FileState } from './files';
import { preparePatch } from './patch';
import { PatchSchema, ReviewSchema, ScopeSchema, type CheckResult } from './schema';
import { baselineHasAssertionFailures, FeatureAssessmentSchema, validateTestPlan, type FeatureRecord } from './feature';

function assert(condition: unknown, message: string): asserts condition { if (!condition) throw new Error(`Campaign evidence verification failed: ${message}`); }
const equal = (left: unknown, right: unknown, message: string) => assert(isDeepStrictEqual(left, right), message);
const counters = (c: CheckResult) => ({ id: c.id, exitCode: c.exitCode, timedOut: c.timedOut, total: c.total, passed: c.passed, failed: c.failed, ok: c.ok });
type Snapshot = { version: 2; files: Record<string, { text: string; sha256: string }> };

export async function verifyCampaign(root: string, runDirectory: string, dependencies: { check: typeof runChecks } = { check: runChecks }) {
  root = resolve(root);
  const runRelative = isAbsolute(runDirectory) ? relative(root, runDirectory) : runDirectory;
  root = await realpath(root);
  const directory = await localPath(root, runRelative);
  const evidence = async (path: string, within = directory): Promise<string> => {
    const target = await localPath(root, path), rel = relative(within, target);
    assert(rel !== '..' && !rel.startsWith('../') && !isAbsolute(rel), `evidence outside campaign directory: ${path}`);
    return readFile(target, 'utf8');
  };
  const record = JSON.parse(await evidence(`${runRelative}/run.json`)) as CampaignRecord;
  assert(record.version === 2, 'expected version 2 campaign');
  assert(record.approvedBy?.trim(), 'missing recorded approval');
  assert(record.gdd && hash(record.gdd.agent.text) === record.gdd.agent.sha256, 'archived agent GDD hash');
  assert(record.gdd.agent.version === record.gdd.human.version, 'archived GDD version mismatch');
  assert(record.backlog, 'missing discovered backlog');
  equal(record.backlog.goal, record.limits.goal, 'backlog goal differs from authorized scope');
  assert(record.backlog.gddHash === record.gdd.agent.sha256 && record.backlog.humanHash === record.gdd.human.sha256, 'backlog GDD identity');
  const lines = record.gdd.agent.text.split('\n');
  const starts = lines.flatMap((line, i) => { const m = /^##\s+§(\d+)\s+/.exec(line); return m ? [{ id: m[1]!.padStart(2, '0'), at: i }] : []; });
  const sections = starts.map((s, i) => ({ id: s.id, text: lines.slice(s.at, starts[i + 1]?.at ?? lines.length).join('\n') }));
  assert(sections.length > 0, 'archived GDD has no numbered sections');
  validateCatalog(CatalogSchema.parse(record.backlog.catalog), sections);
  equal(record.backlog.entries.map(e => e.requirement.id).sort(), record.backlog.catalog.requirements.map(r => r.id).sort(), 'catalog/entry mapping');
  for (const entry of record.backlog.entries) {
    equal(entry.requirement, record.backlog.catalog.requirements.find(r => r.id === entry.requirement.id), `catalog/entry requirement ${entry.requirement.id}`);
    equal(entry.anchor, anchorOf(entry.requirement), `requirement anchor ${entry.requirement.id}`);
  }
  assert(record.status !== 'COMPLETE' || complete(record.backlog), 'campaign claims complete with unresolved discovered requirements');
  type ModelOutput = { output: unknown; call: Call; schema: unknown };
  const archives = new Map<string, Map<string, ModelOutput>>();
  const loadModels = async (run: string, archiveRecord?: CampaignRecord): Promise<Map<string, ModelOutput>> => {
    const known = archives.get(run); if (known) return known;
    const within = await localPath(root, run);
    const archived = archiveRecord ?? JSON.parse(await evidence(`${run}/run.json`, within)) as CampaignRecord;
    assert(archived.version === 2 && archived.approvedBy?.trim() && Array.isArray(archived.calls), `invalid prior campaign ${run}`);
    // A reused catalog belongs to its original authorization, not the current run's model or budget.
    const provider = archived.limits.provider ?? 'claude';
    assert(provider === 'claude' || provider === 'codex', `unknown campaign provider ${run}`);
    assert(archived.calls.length <= archived.limits.maxCalls, 'model call limit exceeded');
    assert(archived.calls.every(call => call.provider === 'claude' || call.provider === 'codex'), `unknown call provider ${run}`);
    assert(archived.calls.every(call => call.provider === provider), `call provider differs from campaign ${run}`);
    if (provider === 'claude') {
      assert(archived.calls.every(call => call.costUsd !== null && Number.isFinite(call.costUsd) && call.costUsd >= 0), 'unknown model spend');
      assert(archived.limits.maxUsd !== undefined && archived.calls.reduce((sum, call) => sum + call.costUsd!, 0) <= archived.limits.maxUsd, 'model spend limit exceeded');
    } else {
      assert(typeof archived.limits.model === 'string' && archived.limits.model.trim(), `missing authorized Codex model ${run}`);
      assert(archived.calls.every(call => call.model === archived.limits.model), `Codex model differs from authorized model ${run}`);
      assert(archived.calls.every(call => call.costUsd === null), 'Codex dollar cost must be unavailable');
      assert(archived.limits.maxUsd === undefined, 'Codex authorization cannot promise a dollar budget');
      const tokens = archived.calls.map(call => {
        try { return tokenUsage(call.usage); }
        catch { assert(false, `unknown Codex token usage: ${call.objective}`); }
      });
      assert(tokens.every(value => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0), 'unknown Codex token usage');
      assert(Number.isSafeInteger(archived.limits.maxTokens) && archived.limits.maxTokens! > 0, 'invalid model token limit');
      // The coordinator checks the threshold before each call; the last in-flight call may cross it.
      let used = 0;
      for (const value of tokens) {
        assert(used < archived.limits.maxTokens!, 'model token limit exceeded before a subsequent call');
        used += value!;
      }
    }
    const outputs = new Map<string, ModelOutput>();
    const read = (path: string) => evidence(path, within);
    for (const call of archived.calls) {
    assert(hash(await read(call.promptFile)) === call.promptSha256, `prompt hash ${call.objective}`);
    const cardText = await read(call.card.file);
    assert(hash(cardText) === call.card.sourceHash, `card hash ${call.objective}`);
    const schema = JSON.parse(await read(call.promptFile.replace(/\.prompt\.txt$/, '.schema.json')));
    assert(hash(JSON.stringify(schema)) === call.schemaSha256, `schema hash ${call.objective}`);
    equal(JSON.parse(await read(call.promptFile.replace(/\.prompt\.txt$/, '.retrieval.json'))), call.retrieval, `retrieval ${call.objective}`);
    const transcript = await read(call.eventsFile);
    if (call.provider === 'claude') {
      const codexEvent = transcript.split('\n').some(line => {
        try { return /^(?:suborned\.codex|thread\.|turn\.|item\.)/.test(JSON.parse(line)?.type ?? ''); }
        catch { return false; }
      });
      assert(!codexEvent, `Codex transcript cannot masquerade as Claude: ${call.objective}`);
    }
    const interpreted = call.provider === 'codex'
      ? interpretCodex(transcript, call.exitCode, archived.limits.model, call.status === 'accepted' ? call.promptSha256 : undefined)
      : interpretClaude(transcript, call.exitCode);
    if (call.provider === 'codex' && call.status === 'accepted') assert(interpreted.ok, `invalid Codex transcript: ${call.objective}: ${interpreted.error}`);
    equal(interpreted.model, call.model, `model identity ${call.objective}`);
    equal(interpreted.modelsUsed, call.modelsUsed, `models used ${call.objective}`);
    equal(interpreted.tools, call.tools, `tool isolation ${call.objective}`);
    equal(interpreted.costUsd, call.costUsd, `model spend ${call.objective}`);
    if (call.provider === 'codex') {
      const audit = JSON.parse(transcript.split('\n').find(line => line.trim())!);
      if (call.status === 'accepted') {
        assert(audit.version === 2 && audit.promptSha256 === call.promptSha256, `Codex invocation prompt hash ${call.objective}`);
        const card = Bun.TOML.parse(cardText) as Record<string, unknown>;
        equal(card.name, call.role, `Codex card role ${call.objective}`);
        assert(typeof card.developer_instructions === 'string' && card.developer_instructions.trim(), `Codex card instructions ${call.objective}`);
        equal(audit.config.filter((value: string) => value.startsWith('developer_instructions=')),
          [`developer_instructions=${JSON.stringify(card.developer_instructions)}`], `Codex invocation developer instructions ${call.objective}`);
      }
      equal(audit.schema, schema, `Codex invocation schema ${call.objective}`);
      equal(audit.effort, call.effort, `Codex invocation effort ${call.objective}`);
      equal(interpreted.usage, call.usage, `token usage ${call.objective}`);
      equal(interpreted.cliVersion, call.cliVersion, `CLI version ${call.objective}`);
      equal(interpreted.tools, [], `Codex tool isolation ${call.objective}`);
    }
    if (archived.bootstrap) assert(call.retrieval.corpusHash === archived.bootstrap.corpusHash, `retrieval corpus changed ${call.objective}`);
    await read(call.stderrFile);
    if (call.status === 'accepted') {
      assert(interpreted.ok, `accepted call has invalid transcript: ${call.objective}`);
      const output = JSON.parse(await read(call.outputFile));
      equal(output, interpreted.output, `output differs from transcript: ${call.objective}`);
      assert(!outputs.has(call.objective), `multiple accepted outputs for ${call.objective}`);
      outputs.set(call.objective, { output, call, schema });
    }
    }
    archives.set(run, outputs); return outputs;
  };
  await loadModels(runRelative, record);
  const fromModel = (objective: string, value: unknown, schema?: z.ZodType, archive = runRelative) => {
    const model = archives.get(archive)?.get(objective);
    assert(model, `missing accepted model output ${objective}`);
    equal(value, model.output, `record differs from model output ${objective}`);
    if (schema) equal(model.schema, z.toJSONSchema(schema), `wrong model schema ${objective}`);
    return model.call;
  };
  assert(record.catalogSource, 'missing immutable catalog origin');
  const catalogOrigin = /^(runs\/goals\/[^/]+)\/(catalog-\d+)\.json$/.exec(record.catalogSource);
  assert(catalogOrigin, 'invalid immutable catalog origin');
  assert(record.backlog.catalogOrigin === record.catalogSource, 'backlog catalog origin differs from campaign');
  const catalogRun = catalogOrigin[1]!, catalogDirectory = await localPath(root, catalogRun);
  await loadModels(catalogRun);
  const originRecord = JSON.parse(await evidence(`${catalogRun}/run.json`, catalogDirectory)) as CampaignRecord;
  equal(originRecord.limits.goal, record.limits.goal, 'catalog origin used a different goal scope');
  assert(originRecord.gdd?.agent.sha256 === record.gdd.agent.sha256 && originRecord.gdd?.human.sha256 === record.gdd.human.sha256,
    'catalog origin used a different GDD snapshot');
  equal(JSON.parse(await evidence(record.catalogSource, catalogDirectory)), record.backlog.catalog, 'catalog archive differs from backlog');
  equal(JSON.parse(await evidence(`${runRelative}/catalog.json`)), record.backlog.catalog, 'current catalog snapshot differs from backlog');
  fromModel(catalogOrigin[2]!, record.backlog.catalog, CatalogSchema, catalogRun);

  type Provenance = { state: FileState; record: FeatureRecord; testArchive: string };
  const priorCache = new Map<string, Provenance>();
  const priorFeature = async (path: string, seen = new Set<string>()): Promise<Provenance> => {
    assert(!seen.has(path), 'cyclic prior feature provenance');
    const known = priorCache.get(path); if (known) return known;
    const match = /^(runs\/goals\/[^/]+)\/features\/[^/]+\/feature\.json$/.exec(path);
    assert(match, `invalid prior feature evidence path ${path}`);
    const archive = match[1]!, within = await localPath(root, archive);
    const prior = JSON.parse(await evidence(path, within)) as FeatureRecord;
    assert(prior.version === 2, `invalid prior feature record ${path}`);
    assert(!prior.testDiscarded, `discarded tests cannot establish resumed provenance ${path}`);
    await loadModels(archive);
    const snapshot = JSON.parse(await evidence(`${dirname(path)}/${prior.snapshotFile}`, within)) as Snapshot;
    assert(snapshot.version === 2 && snapshot.files, `invalid prior snapshot ${path}`);
    const state: FileState = {};
    for (const [source, file] of Object.entries(snapshot.files)) {
      await localPath(root, source);
      assert(hash(file.text) === file.sha256, `prior snapshot hash ${source}`); state[source] = file.text;
    }
    for (const source of prior.writable) if (!Object.hasOwn(state, source)) state[source] = null;
    let testArchive = archive;
    if (prior.priorFeature) {
      const ancestor = await priorFeature(prior.priorFeature, new Set([...seen, path]));
      equal(prior.testPlan, ancestor.record.testPlan, `resumed prior test plan ${path}`);
      equal(prior.testReview, ancestor.record.testReview, `resumed prior test review ${path}`);
      equal(prior.testPath, ancestor.record.testPath, `resumed prior test path ${path}`);
      assert(state[prior.testPath] === prior.testPlan?.code, `resumed prior frozen test ${path}`);
      for (const source of prior.writable) equal(state[source], ancestor.state[source] ?? null, `resumed prior production state ${source}`);
      testArchive = ancestor.testArchive;
    } else {
      const plan = TestPlanSchema.parse(prior.testPlan), review = ReviewSchema.parse(prior.testReview);
      fromModel(prior.testPlanCall, plan, TestPlanSchema, archive); fromModel(prior.testReviewCall, review, ReviewSchema, archive);
      assert(review.verdict === 'pass' && !review.faults.length, `prior test review ${path}`);
      assert(!Object.hasOwn(state, prior.testPath), `prior generated test exists before generation ${path}`);
      const changes = preparePatch(PatchSchema.parse(prior.testPatch), [prior.testPath], { [prior.testPath]: null });
      equal(changes, prior.testChanges, `prior test patch hashes ${path}`);
      assert(changes[0]?.after === plan.code, `prior test differs from model code ${path}`);
      state[prior.testPath] = plan.code;
    }
    for (const round of prior.rounds) {
      fromModel(round.writeCall, PatchSchema.parse(round.patch), PatchSchema, archive);
      if (!round.changes.length) continue; // A proposal saved before an interrupted apply is not an executed write.
      const before = Object.fromEntries(prior.writable.map(source => [source, state[source] ?? null]));
      const changes = preparePatch(round.patch, prior.writable, before);
      equal(changes, round.changes, `prior production patch hashes ${path}/${round.index}`);
      for (const change of changes) state[change.path] = change.after;
    }
    const result = { state, record: prior, testArchive }; priorCache.set(path, result); return result;
  };

  assert(Array.isArray(record.features), 'missing feature list');
  assert(new Set(record.features.map(f => f.id)).size === record.features.length, 'duplicate feature evidence');
  assert(new Set(record.rechecks.map(r => r.id)).size === record.rechecks.length, 'duplicate recheck evidence');
  const features = record.features.map(feature => ({ feature, run: runRelative }));
  for (const entry of record.backlog.entries.filter(e => e.status === 'verified')) {
    if (record.features.some(f => f.id === entry.requirement.id)) continue;
    if (entry.proof) {
      assert(/^runs\/goals\/[^/]+$/.test(entry.proof.run) && entry.proof.run !== runRelative, `invalid retained proof run ${entry.requirement.id}`);
      const within = await localPath(root, entry.proof.run);
      const origin = JSON.parse(await evidence(`${entry.proof.run}/run.json`, within)) as CampaignRecord;
      equal(origin.limits.goal, record.limits.goal, `retained proof goal ${entry.requirement.id}`);
      assert(origin.gdd.agent.sha256 === record.gdd.agent.sha256 && origin.gdd.human.sha256 === record.gdd.human.sha256, `retained proof GDD ${entry.requirement.id}`);
      const matches = origin.features.filter(f => f.id === entry.requirement.id);
      assert(matches.length === 1 && matches[0]!.result.verified, `missing retained feature proof ${entry.requirement.id}`);
      equal(entry.proof.testPath, matches[0]!.result.testPath, `retained proof test path ${entry.requirement.id}`);
      features.push({ feature: matches[0]!, run: entry.proof.run });
    } else assert(entry.inspection?.state === 'implemented', `verified requirement lacks supported proof ${entry.requirement.id}`);
  }
  const validateChecks = async (scope: ReturnType<typeof checkScope>, archived: CheckResult[], within: string, label: string, invocation = false) => {
    equal(archived.map(c => c.id), scope.checks.map(c => c.id), `check IDs/order ${label}`);
    for (const c of archived) {
      if (invocation) {
        equal(c.argv.slice(0, 6), ['dotnet', 'test', scope.testProject, '--filter', scope.checks.find(spec => spec.id === c.id)!.filter, '--logger'], `check invocation ${label}/${c.id}`);
        assert(c.argv[6]?.startsWith('trx;LogFileName=') && c.argv[6].endsWith(`/${c.reportFile}`), `check report destination ${label}/${c.id}`);
        equal(c.argv.slice(7), ['--verbosity', 'minimal'], `check invocation flags ${label}/${c.id}`);
      }
      assert(hash(await evidence(c.outputFile, within)) === c.outputHash, `check output hash ${label}/${c.id}`);
      const trx = await evidence(c.reportFile, within).catch(error => { if (error.code === 'ENOENT') return ''; throw error; });
      const parsed = parseTrx(trx, scope.checks.find(spec => spec.id === c.id)!.minTests);
      equal({ total: parsed.total, passed: parsed.passed, failed: parsed.failed, ok: c.exitCode === 0 && !c.timedOut && parsed.ok },
        { total: c.total, passed: c.passed, failed: c.failed, ok: c.ok }, `archived TRX counters ${label}/${c.id}`);
    }
  };
  const parent = await localPath(root, 'out/goals');
  await mkdir(parent, { recursive: true });
  const temporary = await mkdtemp(resolve(parent, 'verify-campaign-'));
  const replayed: { id: string; sourceRun: string; retained: boolean; generated: boolean; rounds: number; stages: { stage: string; checks: CheckResult[] }[] }[] = [];

  for (const [featureIndex, { feature, run: featureRun }] of features.entries()) {
    await loadModels(featureRun);
    const featureRoot = await localPath(root, featureRun);
    const read = (path: string) => evidence(path, featureRoot);
    const model = (objective: string, value: unknown, schema?: z.ZodType, archive = featureRun) => fromModel(objective, value, schema, archive);
    const archive = `${feature.directory}/feature.json`;
    const f = JSON.parse(await read(archive)) as FeatureRecord;
    assert(f.version === 2 && f.requirement.id === feature.id, `feature identity ${feature.id}`);
    equal(f.requirement, record.backlog.catalog.requirements.find(r => r.id === feature.id), `feature differs from discovered requirement ${feature.id}`);
    equal(f.result, feature.result, `feature result ${feature.id}`);
    assert(f.result.verified, `feature is not verified: ${feature.id}`);
    const featureCampaign = featureRun === runRelative ? record : JSON.parse(await read(`${featureRun}/run.json`)) as CampaignRecord;
    assert(f.rounds.length <= featureCampaign.limits.maxIterations, `iteration limit ${feature.id}`);
    const scope = ScopeSchema.parse(f.scope);
    equal(scope.writable, f.writable, `write scope ${feature.id}`);
    assert(new Set(f.writable).size === f.writable.length && f.writable.every(p => /^sim\/Suborned\.Sim\/[A-Za-z][A-Za-z0-9.]*\.cs$/.test(p)), `production write paths ${feature.id}`);
    assert(/^sim\/Suborned\.Sim\.Tests\/Goal[A-Za-z0-9]+Tests\.cs$/.test(f.testPath), `generated test path ${feature.id}`);
    assert(!f.writable.includes(f.testPath), `generated test is writable by production agent ${feature.id}`);
    const snapshot = JSON.parse(await read(`${feature.directory}/${f.snapshotFile}`)) as Snapshot;
    assert(snapshot.version === 2 && snapshot.files && typeof snapshot.files === 'object', `snapshot format ${feature.id}`);
    const state: FileState = {};
    for (const [path, file] of Object.entries(snapshot.files)) {
      await localPath(root, path);
      assert(typeof file.text === 'string' && hash(file.text) === file.sha256, `snapshot hash ${path}`);
      state[path] = file.text;
    }
    assert(snapshot.files[record.gdd.agent.path]?.sha256 === record.gdd.agent.sha256 && snapshot.files[record.gdd.human.path]?.sha256 === record.gdd.human.sha256, `feature GDD snapshot differs from campaign ${feature.id}`);
    for (const source of scope.sources) assert(state[source.path]?.includes(source.quote), `snapshot missing GDD quote ${source.path}`);
    const resumed = f.priorFeature ? await priorFeature(f.priorFeature) : undefined;
    if (resumed) {
      equal(f.requirement.acceptance, resumed.record.requirement.acceptance, `resumed acceptance criteria ${feature.id}`);
      equal(f.requirement.quote, resumed.record.requirement.quote, `resumed requirement source ${feature.id}`);
      equal(f.testPlan, resumed.record.testPlan, `resumed frozen test plan ${feature.id}`);
      equal(f.testReview, resumed.record.testReview, `resumed frozen test review ${feature.id}`);
      equal(f.testPath, resumed.record.testPath, `resumed frozen test path ${feature.id}`);
      assert(state[f.testPath] === f.testPlan?.code, `resumed frozen test content ${feature.id}`);
      for (const path of f.writable) equal(state[path] ?? null, resumed.state[path] ?? null, `resumed production baseline ${path}`);
      assert(!f.testPatch && !f.testChanges, `resumed feature rewrites frozen tests ${feature.id}`);
    } else assert(!Object.hasOwn(state, f.testPath), `generated test already exists in baseline ${feature.id}`);
    for (const path of f.writable) if (!Object.hasOwn(state, path)) state[path] = null;
    const testPlan = TestPlanSchema.parse(f.testPlan);
    validateTestPlan(testPlan, { requirement: f.requirement } as Parameters<typeof validateTestPlan>[1]);
    model(f.testPlanCall, testPlan, TestPlanSchema, resumed?.testArchive);
    assert(f.testPath.endsWith(`/${testPlan.className}.cs`), `test class/path mismatch ${feature.id}`);
    const testReview = ReviewSchema.parse(f.testReview);
    model(f.testReviewCall, testReview, ReviewSchema, resumed?.testArchive);
    assert(testReview.verdict === 'pass' && testReview.faults.length === 0, `test review failed ${feature.id}`);
    if (!resumed) {
      const testPatch = PatchSchema.parse(f.testPatch);
      const testChanges = preparePatch(testPatch, [f.testPath], { [f.testPath]: null });
      equal(testChanges, f.testChanges, `generated test patch hashes ${feature.id}`);
      assert(testChanges.length === 1 && testChanges[0]!.after === testPlan.code, `generated test differs from model code ${feature.id}`);
    }
    state[f.testPath] = testPlan.code;
    assert(scope.checks.length === 2 && scope.checks.some(c => c.id === scope.regression), `expected target and regression checks ${feature.id}`);
    const target = scope.checks.find(c => c.id !== scope.regression)!;
    assert(target.minTests >= testPlan.minTests && target.filter === `FullyQualifiedName~${testPlan.className}`, `generated acceptance check ${feature.id}`);
    assert(scope.checks.find(c => c.id === scope.regression)!.filter === `FullyQualifiedName!~${testPlan.className}`, `regression selector ${feature.id}`);

    const workspace = resolve(temporary, `feature-${featureIndex + 1}`);
    await mkdir(workspace);
    await localPath(workspace, scope.testProject);
    const put = async (path: string, text: string | null) => {
      const target = await localPath(workspace, path);
      if (text === null) await rm(target, { force: true });
      else { await mkdir(dirname(target), { recursive: true }); await writeFile(target, text); }
    };
    for (const [path, text] of Object.entries(state)) await put(path, text);
    const stages: { stage: string; checks: CheckResult[] }[] = [];
    const checks = async (stage: string, archived: CheckResult[]) => {
      await validateChecks(scope, archived, featureRoot, `${feature.id}/${stage}`);
      const actual = await dependencies.check(workspace, scope, resolve(workspace, 'evidence', stage));
      equal(actual.map(counters), archived.map(counters), `replayed test results ${feature.id}/${stage}`);
      stages.push({ stage, checks: actual });
      return actual;
    };
    const initial = await checks('baseline', f.baselineChecks);
    assert(initial.find(c => c.id === scope.regression)?.ok, `baseline regression failed ${feature.id}`);
    const regressionTotal = initial.find(c => c.id === scope.regression)!.total;
    assert(scope.checks.find(c => c.id === scope.regression)!.minTests >= regressionTotal, `regression minimum decreased ${feature.id}`);
    const red = initial.find(c => c.id === target.id)!;
    if (f.result.generated) {
      assert(await baselineHasAssertionFailures(root, f.baselineChecks.find(c => c.id === target.id)!, target.minTests), `archived baseline is not a behavioral assertion failure ${feature.id}`);
      assert(await baselineHasAssertionFailures(workspace, red, target.minTests), `replayed baseline does not demonstrate a behavioral gap ${feature.id}`);
    }
    else assert(initial.every(c => c.ok) && f.rounds.length === 0, `nongenerated feature must already pass ${feature.id}`);

    let lastChecks = initial;
    for (const [roundIndex, round] of f.rounds.entries()) {
      assert(round.index === roundIndex + 1, `nonconsecutive rounds ${feature.id}`);
      const patch = PatchSchema.parse(round.patch);
      const writer = model(round.writeCall, patch, PatchSchema);
      const before = Object.fromEntries(f.writable.map(path => [path, state[path] ?? null]));
      const changes = preparePatch(patch, f.writable, before);
      equal(changes, round.changes, `production patch hashes ${feature.id}/${round.index}`);
      for (const change of changes) { state[change.path] = change.after; await put(change.path, change.after); }
      if (round.sourceHashes) equal(round.sourceHashes, Object.fromEntries(f.writable.filter(path => state[path] !== null).map(path => [path, hash(state[path]!)])), `rescan source hashes ${feature.id}/${round.index}`);
      lastChecks = await checks(`round-${round.index}`, round.checks);
      if (round.review) {
        const review = ReviewSchema.parse(round.review);
        const reviewer = model(round.reviewCall!, review, ReviewSchema);
        assert(reviewer.role !== writer.role, `writer cannot independently review itself ${feature.id}/${round.index}`);
      }
      if (round.assessment) {
        const assessment = FeatureAssessmentSchema.parse(round.assessment);
        model(round.assessmentCall!, assessment, FeatureAssessmentSchema);
        for (const e of assessment.evidence) assert(state[e.path]?.includes(e.quote), `unseen assessment evidence ${feature.id}/${round.index}/${e.path}`);
      }
    }
    assert(lastChecks.every(c => c.ok), `final tests failed ${feature.id}`);
    assert(lastChecks.find(c => c.id === scope.regression)!.total === regressionTotal, `regression coverage changed ${feature.id}`);
    const last = f.rounds.at(-1);
    const review = last?.review ?? f.prewriteReview;
    const assessment = last?.assessment ?? f.prewriteAssessment;
    if (!last) {
      if (review) model(f.prewriteReviewCall!, review, ReviewSchema);
      if (assessment) model(f.prewriteAssessmentCall!, assessment, FeatureAssessmentSchema);
    }
    assert(review?.verdict === 'pass' && review.faults.length === 0, `final review did not pass ${feature.id}`);
    assert(assessment?.satisfied && assessment.remaining.length === 0 && assessment.evidence.length > 0, `final assessment did not pass ${feature.id}`);
    for (const e of assessment.evidence) assert(state[e.path]?.includes(e.quote), `final assessment evidence ${feature.id}/${e.path}`);
    assert(f.result.generated === (f.rounds.length > 0), `generation claim differs from recorded writes ${feature.id}`);
    const entry = record.backlog.entries.find(e => e.requirement.id === feature.id)!;
    assert(entry, `feature missing from backlog ${feature.id}`);
    equal(f.result.testPath, f.testPath, `feature result test path ${feature.id}`);
    equal(f.result.tests, [testPlan.className], `feature result selectors ${feature.id}`);
    if (entry.status === 'verified') {
      if (entry.proof) {
        equal(entry.proof, { run: featureRun, testPath: f.testPath }, `feature proof binding ${feature.id}`);
        equal(entry.featureDirectory, feature.directory, `feature directory binding ${feature.id}`);
      }
      for (const [path, sha] of Object.entries(entry.inspectedHashes))
        assert(typeof state[path] === 'string' && hash(state[path]!) === sha, `verified source hash ${feature.id}/${path}`);
    }
    if (featureRun !== runRelative) {
      assert(entry.status === 'verified' && entry.proof, `retained feature is not verified ${feature.id}`);
      assert(Object.keys(entry.inspectedHashes).length > 0, `missing retained source hashes ${feature.id}`);
      equal(entry.inspection?.tests, [testPlan.className], `retained test selectors ${feature.id}`);
      assert((entry.checkMinimums?.[testPlan.className] ?? 0) >= testPlan.minTests, `retained test minimum ${feature.id}`);
      const scope = checkScope(feature.id, [testPlan.className], entry.checkMinimums);
      const current = record.rechecks.find(r => r.id === feature.id);
      assert(current, `missing current recheck ${feature.id}`);
      await validateChecks(scope, current.checks, await localPath(root, `${runRelative}/rechecks/${feature.id}`), `recheck/${feature.id}`, true);
      assert(current.checks.every(c => c.ok), `current recheck failed ${feature.id}`);
      const actual = await dependencies.check(workspace, scope, resolve(workspace, 'evidence', 'recheck'));
      equal(actual.map(counters), current.checks.map(counters), `replayed current recheck ${feature.id}`);
      stages.push({ stage: 'recheck', checks: actual });
    }
    replayed.push({ id: feature.id, sourceRun: featureRun, retained: featureRun !== runRelative, generated: f.result.generated, rounds: f.rounds.length, stages });
  }
  // Existing implementations need no generated feature, but their conclusion must come
  // from an archived model inspection of production AND tests plus real check reports.
  // These older inspection archives contain excerpts, not a buildable source snapshot:
  // validate their recorded checks without claiming an isolated source replay.
  const inspected: string[] = [];
  for (const entry of record.backlog.entries.filter(e => e.status === 'verified' && !features.some(f => f.feature.id === e.requirement.id))) {
    const id = entry.requirement.id, inspection = InspectionSchema.parse(entry.inspection);
    assert(inspection.state === 'implemented' && entry.checks?.length, `verified requirement lacks inspection checks ${id}`);
    const origin = /^(runs\/goals\/[^/]+)\/existing\/[^/]+\//.exec(entry.checks[0]!);
    assert(origin, `invalid inspection check origin ${id}`);
    const run = origin[1]!, within = await localPath(root, run);
    const source = JSON.parse(await evidence(`${run}/run.json`, within)) as CampaignRecord;
    equal(source.limits.goal, record.limits.goal, `inspection goal ${id}`);
    assert(source.gdd.agent.sha256 === record.gdd.agent.sha256 && source.gdd.human.sha256 === record.gdd.human.sha256, `inspection GDD ${id}`);
    const originEntry = source.backlog?.entries.find(e => e.requirement.id === id);
    assert(originEntry?.status === 'verified', `unsupported inspection origin ${id}`);
    equal(originEntry.requirement, entry.requirement, `inspection requirement ${id}`);
    equal(originEntry.inspection, inspection, `retained inspection ${id}`);
    equal(originEntry.inspectedHashes, entry.inspectedHashes, `retained inspection hashes ${id}`);
    const inspections = source.inspections.filter(i => i.id === id);
    assert(inspections.length === 1, `missing supported inspection ${id}`);
    equal(inspections[0]!.result, inspection, `inspection result ${id}`);
    const outputs = await loadModels(run);
    const matches = [...outputs.entries()].filter(([objective, value]) => objective.startsWith(`${id}-inspect-`) && isDeepStrictEqual(value.output, inspection));
    assert(matches.length > 0, `missing accepted inspection ${id}`);
    const [objective, model] = matches.at(-1)!;
    fromModel(objective, inspection, InspectionSchema, run);
    const pass = /-inspect-([1-9][0-9]*)$/.exec(objective)?.[1];
    assert(pass, `invalid inspection pass ${id}`);
    const input = JSON.parse(await evidence(`${inspections[0]!.directory}/inspection-${pass}.input.json`, within)) as { requirement: unknown; context: CodeSource[] };
    equal(input.requirement, entry.requirement, `inspection input requirement ${id}`);
    assert((await evidence(model.call.promptFile, within)).includes(JSON.stringify(input)), `inspection input absent from prompt ${id}`);
    validateInspection(inspection, input.context);
    equal(entry.inspectedHashes, Object.fromEntries(input.context.map(c => [c.path, c.sha256])), `inspection context hashes ${id}`);
    assert(Object.keys(entry.inspectedHashes).length > 0, `missing inspection hashes ${id}`);
    const archived = JSON.parse(await evidence(`${run}/existing/${id}/checks.json`, within)) as CheckResult[];
    equal(archived.map(c => c.reportFile), entry.checks, `inspection check paths ${id}`);
    const originalScope = checkScope(id, inspection.tests);
    await validateChecks(originalScope, archived, await localPath(root, `${run}/existing/${id}`), `existing/${id}`, true);
    assert(archived.every(c => c.ok), `existing checks failed ${id}`);
    equal(entry.checkMinimums, Object.fromEntries(inspection.tests.map((test, i) => [test, archived[i]!.total])), `inspection test minimums ${id}`);
    if (run !== runRelative) {
      const current = record.rechecks.find(r => r.id === id);
      assert(current, `missing current recheck ${id}`);
      await validateChecks(checkScope(id, inspection.tests, entry.checkMinimums), current.checks,
        await localPath(root, `${runRelative}/rechecks/${id}`), `recheck/${id}`, true);
      assert(current.checks.every(c => c.ok), `current recheck failed ${id}`);
    }
    inspected.push(id);
  }
  const summary = { ok: true, run: record.id, complete: record.status === 'COMPLETE', features: replayed, inspected, directory: portable(root, temporary) };
  await json(resolve(temporary, 'verification.json'), summary);
  return summary;
}
