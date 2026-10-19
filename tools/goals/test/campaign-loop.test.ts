import { test, expect } from 'bun:test';
import { mkdtemp, mkdir, writeFile, rm, readFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { resolve } from 'node:path';
import { runCampaign, loadBacklog, backlogPathFor, type CampaignDependencies } from '../campaign';
import { resolveLatestGdd } from '../discovery-sources';
import { reconcile, type Catalog, type Inspection } from '../backlog';
import type { CampaignAsk } from '../models';
import type { CheckResult } from '../schema';
import { json } from '../files';

const quote = 'Every order shows its full price before the player commits.';
const source = 'public bool PriceKnown() => false;';
const requirement = (id: string, value: number) => ({ id, title: `Price preview requirement ${id}`, section: '01', quote, acceptance: [`The ${id} price preview is available before issue.`], terms: ['PriceKnown', 'price'], dependsOn: [], value, effort: 1, risk: 1, rationale: 'Pure query behavior improves informed player decisions.' });
const catalog: Catalog = { summary: 'Fixture, not a live model result', coverage: [{ section: '01', disposition: 'requirements', reason: 'Pure engine pricing belongs to the headless code domain.' }], requirements: [requirement('first-price', 5), requirement('second-price', 4)] };
const check: CheckResult = { id: 'acceptance', argv: [], exitCode: 0, timedOut: false, total: 1, passed: 1, failed: 0, ok: true, outputFile: '', reportFile: '', outputHash: '', seconds: 0 };

test('two bounded runs retain the first proof, rerun its tests, and select the next discovered gap', async () => {
  const root = await mkdtemp(resolve(tmpdir(), 'goals-campaign-'));
  try {
    for (const path of ['GDD','sim/Suborned.Sim','sim/Suborned.Sim.Tests','contracts/rules']) await mkdir(resolve(root,path),{recursive:true});
    await writeFile(resolve(root,'GDD/the-suborned-agent-gdd-v0.24.0-2026-10-18.md'), `# GDD\n## §01 Game\n${quote}\n`);
    await writeFile(resolve(root,'GDD/the-suborned-gdd-v0.24.0-2026-10-18.html'), '<p>Human design</p>');
    await writeFile(resolve(root,'sim/Suborned.Sim/Game.cs'), source);
    const selected: string[] = [], asked: string[] = [];
    let rechecks = 0;
    const ask: CampaignAsk = async (id, _role, _brief, _task, schema) => {
      asked.push(id);
      const output: Catalog | Inspection = id.startsWith('catalog') ? catalog : { state: 'missing', explanation: 'The inspected public query explicitly reports missing price knowledge.', evidence: [{ path: 'sim/Suborned.Sim/Game.cs', quote: source }], reads: [], searches: [], tests: [], writable: ['sim/Suborned.Sim/Prices.cs'] };
      return schema.parse(output);
    };
    const dependencies: CampaignDependencies = {
      ask, latest: async () => ({ ...await resolveLatestGdd(root), comparisons: [] }),
      bootstrap: async () => ({ corpusHash: 'a'.repeat(64), database: 'fixture', chunks: 1, embeddingModel: 'fixture', indexed: false, reused: true, started: false }),
      check: async () => { rechecks++; return [check]; },
      feature: async (_root, directory, entry) => {
        selected.push(entry.requirement.id);
        const testPath = `sim/Suborned.Sim.Tests/Goal${selected.length}Tests.cs`;
        await writeFile(resolve(root,testPath), 'public class GoalTests {}');
        // Separate production files ensure a second feature does not invalidate unrelated first proof.
        const writable = [`sim/Suborned.Sim/Price${selected.length}.cs`];
        await writeFile(resolve(root,writable[0]!), 'public class Price {}');
        return { verified: true, generated: true, testPath, tests: [`Goal${selected.length}Tests`], writable, rounds: 1, recordPath: `${directory}/feature.json` };
      },
    };
    const limits = { maxUsd: 1, maxCalls: 4, maxIterations: 1, maxFeatures: 1, maxInspections: 4 };
    const first = await runCampaign(root,'run','offline fixture',limits,dependencies);
    expect(first.status).toBe('INCOMPLETE'); expect(first.backlog!.entries[0]!.status).toBe('verified');
    expect(first.backlog!.entries[1]!.status).toBe('uninspected');
    const second = await runCampaign(root,'run','offline fixture',limits,dependencies);
    expect(second.status).toBe('COMPLETE'); expect(selected).toEqual(['first-price','second-price']);
    expect(asked.filter(id=>id.startsWith('catalog'))).toHaveLength(1);
    expect(asked.filter(id=>id.startsWith('first-price-inspect'))).toHaveLength(1); expect(rechecks).toBe(1);
    const saved = JSON.parse(await readFile(resolve(root,'runs/goals/backlog.json'),'utf8'));
    expect(saved.entries.every((e: {status:string})=>e.status==='verified')).toBe(true);
  } finally { await rm(root,{recursive:true,force:true}); }
});

const oneCatalog: Catalog = { ...catalog, requirements: [catalog.requirements[0]!] };

test('explicit goals keep separate backlogs and reject a mismatched stored scope', async () => {
  const root = await mkdtemp(resolve(tmpdir(), 'goals-scope-'));
  try {
    const goal = 'Export existing debrief consequences';
    const backlog = reconcile(oneCatalog, 'a'.repeat(64), 'b'.repeat(64), '0.24.0');
    backlog.goal = goal;
    await json(resolve(root, backlogPathFor(goal)), backlog);
    expect((await loadBacklog(root, goal))!.goal).toBe(goal);
    expect(await loadBacklog(root)).toBeUndefined();
    expect(await loadBacklog(root, 'Other goal')).toBeUndefined();
    backlog.goal = 'Mismatched scope';
    await json(resolve(root, backlogPathFor(goal)), backlog);
    await expect(loadBacklog(root, goal)).rejects.toThrow('scope');
  } finally { await rm(root, { recursive: true, force: true }); }
});
const limits = { maxUsd: 1, maxCalls: 4, maxIterations: 1, maxFeatures: 1, maxInspections: 1 };
async function existingFixture() {
  const root = await mkdtemp(resolve(tmpdir(), 'goals-campaign-review-'));
  for (const path of ['GDD', 'sim/Suborned.Sim', 'sim/Suborned.Sim.Tests', 'contracts/rules']) await mkdir(resolve(root, path), { recursive: true });
  await writeFile(resolve(root, 'GDD/the-suborned-agent-gdd-v0.24.0-2026-10-18.md'), `# GDD\n## §01 Game\n${quote}\n`);
  await writeFile(resolve(root, 'GDD/the-suborned-gdd-v0.24.0-2026-10-18.html'), '<p>Human design</p>');
  await writeFile(resolve(root, 'sim/Suborned.Sim/Game.cs'), source);
  await writeFile(resolve(root, 'sim/Suborned.Sim.Tests/GoalPendingTests.cs'), 'public class GoalPendingTests { public void PriceKnown() {} }');
  const inspection: Inspection = { state: 'implemented', explanation: 'The fixture inspector claims both required cases establish the behavior.',
    evidence: [{ path: 'sim/Suborned.Sim/Game.cs', quote: source }], reads: [], searches: [], tests: ['GoalPendingTests.PriceKnown'], writable: [] };
  const ask: CampaignAsk = async (id, _role, _brief, _task, schema) => schema.parse(id.startsWith('catalog') ? oneCatalog : inspection);
  const dependencies: CampaignDependencies = { ask, latest: async () => ({ ...await resolveLatestGdd(root), comparisons: [] }),
    bootstrap: async () => ({ corpusHash: 'a'.repeat(64), database: 'fixture', chunks: 1, embeddingModel: 'fixture', indexed: false, reused: true, started: false }),
    check: async (_root, scope) => scope.checks.map(spec => ({ ...check, id: spec.id })),
    feature: async () => { throw new Error('Unexpected feature execution'); } };
  const seedPending = async () => {
    const gdd = await resolveLatestGdd(root), backlog = reconcile(oneCatalog, gdd.agent.sha256, gdd.human.sha256, gdd.agent.version);
    const entry = backlog.entries[0]!;
    entry.status = 'partial'; entry.featureDirectory = 'runs/goals/previous/features/first-price'; entry.inspection = structuredClone(inspection);
    const testPlan = { summary: 'Previously authored tests', className: 'GoalPendingTests', code: await readFile(resolve(root, 'sim/Suborned.Sim.Tests/GoalPendingTests.cs'), 'utf8'),
      minTests: 1, covers: entry.requirement.acceptance };
    await json(resolve(root, entry.featureDirectory, 'feature.json'), { version: 2, requirement: entry.requirement, writable: ['sim/Suborned.Sim/Game.cs'],
      testPath: 'sim/Suborned.Sim.Tests/GoalPendingTests.cs', testPlan, testChanges: [{ path: 'sim/Suborned.Sim.Tests/GoalPendingTests.cs' }], result: { verified: false } });
    await json(resolve(root, 'runs/goals/backlog.json'), backlog);
    return { previous: entry.featureDirectory, testPlan };
  };
  return { root, dependencies, inspection, seedPending };
}

test('one passing selector cannot hide a missing separately requested acceptance test', async () => {
  const f = await existingFixture();
  try {
    f.inspection.tests = ['GoalPendingTests.PriceKnown', 'MissingRequiredTests.PriceKnown'];
    const selectors: string[] = [];
    f.dependencies.check = async (_root, scope) => scope.checks.map(spec => {
      selectors.push(spec.filter);
      return spec.filter.includes('MissingRequiredTests') ? { ...check, id: spec.id, total: 0, passed: 0, ok: false } : { ...check, id: spec.id };
    });
    const record = await runCampaign(f.root, 'scan', 'offline fixture', limits, f.dependencies);
    expect(selectors).toEqual(['FullyQualifiedName~GoalPendingTests.PriceKnown', 'FullyQualifiedName~MissingRequiredTests.PriceKnown']);
    expect(record.status).toBe('INCOMPLETE'); expect(record.backlog!.entries[0]!.status).toBe('partial');
    expect(record.features).toHaveLength(0);
  } finally { await rm(f.root, { recursive: true, force: true }); }
});

test('an implemented inspection cannot waive the pending review of an interrupted generated feature', async () => {
  const f = await existingFixture();
  try {
    const prior = await f.seedPending();
    let featureCalls = 0;
    f.dependencies.check = async () => { throw new Error('Implemented shortcut must not execute for pending review'); };
    f.dependencies.feature = async (_root, directory, entry) => {
      featureCalls++;
      expect(entry.featureDirectory).toBe(prior.previous);
      expect(entry.inspection!.writable).toEqual(['sim/Suborned.Sim/Game.cs']);
      const result = { verified: true, generated: false, testPath: 'sim/Suborned.Sim.Tests/GoalPendingTests.cs', tests: ['GoalPendingTests'],
        writable: ['sim/Suborned.Sim/Game.cs'], rounds: 0, recordPath: `${directory}/feature.json` };
      await json(resolve(directory, 'feature.json'), { version: 2, priorFeature: `${prior.previous}/feature.json`, testPlan: prior.testPlan, result });
      return result;
    };
    const record = await runCampaign(f.root, 'run', 'offline fixture', limits, f.dependencies);
    expect(featureCalls).toBe(1); expect(record.status).toBe('COMPLETE');
    expect(record.features[0]!.result.generated).toBe(false);
    expect(record.inspections[0]!.result.writable).toEqual([]); // preserve exact model output, not coordinator mutation
    expect(record.backlog!.entries[0]!.featureDirectory).toBe(record.features[0]!.directory);
    expect(record.backlog!.entries[0]!.checkMinimums).toEqual({ GoalPendingTests: 1 });
  } finally { await rm(f.root, { recursive: true, force: true }); }
});

test('a rejected continuation preserves its previous frozen-test pointer and concrete stop reason', async () => {
  const f = await existingFixture();
  try {
    const prior = await f.seedPending();
    f.dependencies.feature = async (_root, directory) => {
      const result = { verified: false, generated: false, testPath: '', tests: [], writable: ['sim/Suborned.Sim/Game.cs'],
        rounds: 0, error: 'Resume write boundary changed', recordPath: `${directory}/feature.json` };
      await json(resolve(directory, 'feature.json'), { version: 2, priorFeature: `${prior.previous}/feature.json`, result });
      return result;
    };
    const record = await runCampaign(f.root, 'run', 'offline fixture', limits, f.dependencies);
    expect(record.status).toBe('INCOMPLETE'); expect(record.stopReason).toBe('Resume write boundary changed');
    expect(record.backlog!.entries[0]!.featureDirectory).toBe(prior.previous);
    const stored = JSON.parse(await readFile(resolve(f.root, 'runs/goals/backlog.json'), 'utf8'));
    expect(stored.entries[0].featureDirectory).toBe(prior.previous);
  } finally { await rm(f.root, { recursive: true, force: true }); }
});

test('reruns preserve the observed minimum count of existing acceptance cases', async () => {
  const f = await existingFixture();
  try {
    f.dependencies.check = async (_root, scope) => scope.checks.map(spec => ({ ...check, id: spec.id, total: 3, passed: 3 }));
    const first = await runCampaign(f.root, 'scan', 'offline fixture', limits, f.dependencies);
    expect(first.status).toBe('COMPLETE');
    expect(first.backlog!.entries[0]!.checkMinimums).toEqual({ 'GoalPendingTests.PriceKnown': 3 });
    const minima: number[] = [];
    f.dependencies.check = async (_root, scope) => scope.checks.map(spec => {
      minima.push(spec.minTests); return { ...check, id: spec.id, ok: spec.minTests <= 1 };
    });
    // The recheck detects lost cases; the subsequent inspector declines to certify reduced evidence.
    f.inspection.state = 'unknown'; f.inspection.tests = []; f.inspection.explanation = 'The observed test count changed and requires further investigation.';
    const second = await runCampaign(f.root, 'scan', 'offline fixture', limits, f.dependencies);
    expect(minima[0]).toBe(3); expect(second.status).toBe('INCOMPLETE');
    expect(second.backlog!.entries[0]!.status).toBe('unknown');
    expect(second.catalogSource).toBe(first.catalogSource);
  } finally { await rm(f.root, { recursive: true, force: true }); }
});
