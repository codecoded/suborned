import { mkdir, open, rm } from 'node:fs/promises';
import { resolve } from 'node:path';
import type { Call } from '../rag/agent';
import { hash } from '../rag/corpus';
import { CatalogSchema, InspectionSchema, complete, invalidateChanged, priorities, reconcile, validateCatalog, type Backlog, type Entry, type Inspection } from './backlog';
import { ensureRetrieval, type BootstrapReport } from './bootstrap';
import { runChecks } from './checks';
import { assertLatestGdd, buildCodeIndex, navigationIndex, readCodeRanges, searchCode, type CodeIndexEntry, type CodeSource, type GddSnapshot } from './discovery-sources';
import { json, localPath, textOrNull } from './files';
import { campaignCaller, LimitReached, validateLimits, type CampaignAsk, type Limits } from './models';
import { executeFeature, type FeatureRecord, type FeatureResult } from './feature';
import type { CheckResult, Scope } from './schema';

export type CampaignRecord = {
  version: 2; id: string; mode: 'run' | 'scan'; status: 'INCOMPLETE' | 'COMPLETE' | 'ERROR'; approvedBy: string;
  startedAt: string; finishedAt?: string; limits: Limits & { maxFeatures: number; maxInspections: number };
  gdd: Omit<GddSnapshot, 'sections'> & { comparisons: unknown[] }; bootstrap?: BootstrapReport;
  calls: Call[]; backlog?: Backlog; inspections: { id: string; directory: string; result: Inspection }[];
  features: { id: string; directory: string; result: FeatureResult }[]; rechecks: { id: string; checks: CheckResult[] }[];
  scans: { stage: string; indexHash: string; ranking: ReturnType<typeof priorities> }[];
  catalogSource?: string; error?: string; stopReason?: string; manualEdits: string[];
};
export const backlogPath = 'runs/goals/backlog.json';
export const backlogPathFor = (goal?: string) => goal ? `runs/goals/backlogs/${hash(goal).slice(0, 24)}.json` : backlogPath;
const brief = (name: string) => `tools/goals/prompts/${name}.md`;
const codeHashes = (index: CodeIndexEntry[]) => Object.fromEntries(index.map(e => [e.path, e.sha256]));
export function checkScope(id: string, tests: string[], minimums: Record<string, number> = {}): Scope {
  if (!tests.length || tests.some(t => !/^[A-Za-z_][A-Za-z0-9_.]+$/.test(t))) throw new Error('Invalid test selector');
  return { id, goal: 'Verify current discovered requirement', sources: [], requirements: [], context: [], writable: [],
    testProject: 'sim/Suborned.Sim.Tests/Suborned.Sim.Tests.csproj', regression: 'acceptance-1',
    checks: tests.map((t, i) => ({ id: `acceptance-${i + 1}`, filter: `FullyQualifiedName~${t}`, minTests: minimums[t] ?? 1 })) };
}
export function validateInspection(result: Inspection, context: CodeSource[]) {
  if (result.state === 'inspect') {
    if (!result.reads.length && !result.searches.length) throw new Error('Inspection request has no reads or searches');
    return;
  }
  if (result.reads.length || result.searches.length) throw new Error('Final inspection cannot have pending reads');
  if (['implemented', 'missing', 'partial'].includes(result.state)) {
    if (!result.evidence.length || !result.evidence.some(e => e.path.startsWith('sim/Suborned.Sim/'))) throw new Error('A conclusion needs inspected production execution-path evidence');
    for (const evidence of result.evidence) if (!context.some(c => c.path === evidence.path && c.text.includes(evidence.quote))) throw new Error(`Unseen or invented code evidence: ${evidence.path}`);
  }
  if (result.state === 'implemented' && (!result.tests.length || !context.some(c => c.path.startsWith('sim/Suborned.Sim.Tests/')))) throw new Error('Implemented requires inspected tests and runnable test selectors');
  if (['missing', 'partial'].includes(result.state) && !result.writable.length) throw new Error('A gap must name bounded repair paths');
  if (result.writable.some(p => !/^sim\/Suborned\.Sim\/[A-Za-z][A-Za-z0-9.]*\.cs$/.test(p)) || new Set(result.writable).size !== result.writable.length) throw new Error('Only distinct flat shared-engine C# files may be writable');
}

export async function inspectRequirement(root: string, directory: string, entry: Entry, index: CodeIndexEntry[], ask: CampaignAsk): Promise<{ result: Inspection; context: CodeSource[] }> {
  const r = entry.requirement, context: CodeSource[] = [];
  let searches = [await searchCode(root, index, r.terms, { maxMatches: 60 })];
  const add = (sources: CodeSource[]) => { for (const c of sources) if (!context.some(x => x.path === c.path && x.from === c.from && x.to === c.to)) context.push(c); };
  // Give immediate nearby source, but never turn a search miss into a missing-feature conclusion.
  const initial = searches[0]!.matches.filter((m, i, all) => all.findIndex(x => x.path === m.path) === i).slice(0, 5);
  add(await readCodeRanges(root, index, initial.map(m => ({ path: m.path, from: Math.max(1, m.line - 15), to: Math.min(index.find(x => x.path === m.path)!.lines, m.line + 60) }))));
  let feedback = '';
  for (let pass = 1; pass <= 5; pass++) {
    const input = { requirement: r, index: navigationIndex(index), searches, context, feedback, exchangesRemaining: 6 - pass,
      inspectionDomain: 'Indexed shared simulation, tests, CLI, tools/sim consumers and rule contracts. Searches cover this index only. On the final exchange give a supported conclusion or unknown; another read cannot be answered in this invocation.' };
    await json(resolve(directory, `inspection-${pass}.input.json`), input);
    const result = await ask(`${r.id}-inspect-${pass}`, 'simulation-engineer', brief('inspect'), JSON.stringify(input), InspectionSchema, [r.title, r.quote]);
    await json(resolve(directory, `inspection-${pass}.output.json`), result);
    try { validateInspection(result, context); }
    catch (error) { feedback = String(error); continue; }
    if (result.state !== 'inspect') return { result, context };
    try {
      const reads = await readCodeRanges(root, index, result.reads);
      if (context.reduce((n, c) => n + c.text.length, 0) + reads.reduce((n, c) => n + c.text.length, 0) > 170000) throw new Error('Total inspection context exceeds 170000 characters; conclude unknown or narrow the requirement');
      add(reads);
      searches = result.searches.length ? [...searches, await searchCode(root, index, result.searches, { maxMatches: 60 })] : searches;
      feedback = '';
    } catch (error) { feedback = String(error); }
  }
  return { result: { state: 'unknown', explanation: `Inspection budget exhausted without a supported conclusion. ${feedback}`, evidence: [], reads: [], searches: [], tests: [], writable: [] }, context };
}

export async function loadBacklog(root: string, goal?: string): Promise<Backlog | undefined> {
  const text = await textOrNull(await localPath(root, backlogPathFor(goal)));
  if (text === null) return undefined;
  const b = JSON.parse(text) as Backlog;
  if (b.version !== 2 || !Array.isArray(b.entries) || !Array.isArray(b.retired)) throw new Error('Unsupported or invalid backlog');
  CatalogSchema.parse(b.catalog);
  if (b.goal !== goal) throw new Error('Stored backlog goal does not match requested scope');
  return b;
}
export async function currentStatus(root: string, goal?: string) {
  const gdd = await assertLatestGdd(root), b = await loadBacklog(root, goal);
  if (!b) return { gdd: gdd.agent.version, status: 'NO_BACKLOG', next: 'Run goals scan or goals run with recorded model approval' };
  const fresh = b.gddHash === gdd.agent.sha256 && b.humanHash === gdd.human.sha256;
  if (!fresh) return { gdd: gdd.agent.version, status: 'STALE_GDD', next: 'Run discovery again; both GDD hashes must match' };
  invalidateChanged(b, codeHashes(await buildCodeIndex(root)));
  return { gdd: gdd.agent.version, goal: goal ?? 'Shared headless simulation', status: complete(b) ? 'COMPLETE' : 'INCOMPLETE', counts: counts(b), ranking: priorities(b),
    note: 'Offline status checks source hashes. Run/scan re-executes retained acceptance tests before skipping verified work.' };
}
function counts(b: Backlog) { return Object.fromEntries(['verified', 'missing', 'partial', 'blocked', 'unknown', 'uninspected'].map(status => [status, b.entries.filter(e => e.status === status).length])); }
const esc = (s: unknown) => String(s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]!);
async function saveReport(directory: string, record: CampaignRecord) {
  await json(resolve(directory, 'run.json'), record);
  if (!record.backlog) return;
  const b = record.backlog;
  await Bun.write(resolve(directory, 'index.html'), `<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Goal agent ${esc(record.id)}</title><style>body{font:16px system-ui;max-width:1200px;margin:40px auto;padding:0 20px;background:#f6f5ef;color:#202a2f}table{border-collapse:collapse;width:100%}td,th{padding:12px;border-bottom:1px solid #c7cdcf;text-align:left;vertical-align:top}small{color:#526169}code{overflow-wrap:anywhere}</style><p><strong>Execution report · ${esc(record.mode)}</strong></p><h1>GDD → verified code</h1><p>Run: <code>${esc(record.id)}</code><br>Started: ${esc(record.startedAt)}<br>Finished: ${esc(record.finishedAt ?? 'still running')}</p><p><a href="run.json">Machine-readable evidence for this run</a></p><p>GDD v${esc(b.gddVersion)} · ${esc(record.status)} · ${record.calls.length} model calls · ${record.limits.provider === 'codex' ? 'Codex subscription (dollar cost unavailable)' : '$' + record.calls.reduce((n,c)=>n+(c.costUsd??0),0).toFixed(3)}</p><p>Scope: ${esc(record.limits.goal ?? 'Shared headless simulation')}</p><p>${esc(record.stopReason ?? record.error ?? '')}</p><p>${esc(JSON.stringify(counts(b)))}</p><p>Priority = 4 × value + 2 × direct dependents − effort − risk. A bounded batch is complete only when every discovered in-domain requirement has current evidence.</p><table><tr><th>Requirement / source</th><th>Status</th><th>Priority / reason</th><th>Evidence / next action</th></tr>${priorities(b).map(p=>{const e=b.entries.find(e=>e.requirement.id===p.id)!;return `<tr><td>${esc(e.requirement.title)}<br><small>§${esc(e.requirement.section)} · ${esc(e.requirement.id)}</small></td><td>${esc(e.status)}</td><td>${p.score}<br><small>${esc(p.reason)}</small></td><td>${esc(e.explanation)}${e.proof?`<br><code>${esc(e.proof.run)}</code>`:''}</td></tr>`}).join('')}</table><h2>GDD section coverage</h2>${b.catalog.coverage.map(c=>`<p>§${esc(c.section)} — ${esc(c.disposition)}: ${esc(c.reason)}</p>`).join('')}<p>Agent GDD sha256: <code>${b.gddHash}</code><br>Human GDD sha256: <code>${b.humanHash}</code></p></html>`);
}

export type CampaignDependencies = { ask?: CampaignAsk; latest: typeof assertLatestGdd; bootstrap: typeof ensureRetrieval; check: typeof runChecks; feature: typeof executeFeature };
export async function runCampaign(root: string, mode: 'run' | 'scan', approvedBy: string, limits: CampaignRecord['limits'], dependencies: CampaignDependencies = { latest: assertLatestGdd, bootstrap: ensureRetrieval, check: runChecks, feature: executeFeature }): Promise<CampaignRecord> {
  validateLimits(limits, approvedBy);
  if (!Number.isInteger(limits.maxFeatures) || limits.maxFeatures < 1 || limits.maxFeatures > 5 || !Number.isInteger(limits.maxInspections) || limits.maxInspections < 1 || limits.maxInspections > 30) throw new Error('max-features must be 1..5 and max-inspections 1..30');
  const gdd = await dependencies.latest(root);
  const id = `${new Date().toISOString().replace(/[:.]/g, '-')}-discovery`, relative = `runs/goals/${id}`, directory = await localPath(root, relative);
  const { sections, ...gddRecord } = gdd;
  const record: CampaignRecord = { version: 2, id, mode, status: 'INCOMPLETE', approvedBy, startedAt: new Date().toISOString(), limits, gdd: gddRecord,
    calls: [], inspections: [], features: [], rechecks: [], scans: [], manualEdits: [] };
  const ask = dependencies.ask ?? campaignCaller(relative, record.calls, limits, () => record.bootstrap?.corpusHash);
  await mkdir(resolve(root, 'out'), { recursive: true });
  const lockPath = await localPath(root, 'out/goals.lock'), lock = await open(lockPath, 'wx').catch(() => { throw new Error('Another run holds out/goals.lock; inspect that process before removing a stale lock'); });
  try {
    await lock.writeFile(JSON.stringify({ pid: process.pid, run: relative }));
    await mkdir(directory, { recursive: true }); await saveReport(directory, record);
    console.log(`[goals] evidence: ${relative}; latest local GDD ${gdd.agent.version}`);
    record.bootstrap = await dependencies.bootstrap();
    let index = await buildCodeIndex(root);
    await json(resolve(directory, 'initial-index.json'), index);
    const previous = await loadBacklog(root, limits.goal);
    let catalog = previous?.gddHash === gdd.agent.sha256 && previous.humanHash === gdd.human.sha256 ? previous.catalog : undefined;
    if (catalog && previous?.catalogOrigin) record.catalogSource = previous.catalogOrigin;
    else catalog = undefined;
    if (!catalog) {
      let feedback = '';
      for (let attempt = 1; attempt <= 2; attempt++) {
        const candidate = await ask(`catalog-${attempt}`, 'lead-producer', brief('discover'), JSON.stringify({ sections, codeIndex: navigationIndex(index), feedback,
          ...(limits.goal ? { goal: limits.goal, scopeInstruction: 'Extract only the independently testable requirements necessary for this explicit goal, including preservation of existing behavior. Prefer one cohesive requirement when a single feature implements it; do not split one serializer into duplicate tasks. Mark unrelated sections outside-domain with a scope reason. COMPLETE refers only to this declared goal. The code index is navigation; inspection must still establish gaps.' } : {}),
        }), CatalogSchema, [limits.goal ?? 'shared simulation rules Watch Learn Turn Time Take Fight Inherit', 'headless deterministic orders query pricing cancellation replay']);
        await json(resolve(directory, `catalog-${attempt}.json`), candidate);
        try { validateCatalog(candidate, sections); catalog = candidate; record.catalogSource = `${relative}/catalog-${attempt}.json`; break; }
        catch (error) { feedback = String(error); }
      }
      if (!catalog) throw new Error(`GDD catalog validation failed: ${feedback}`);
    }
    validateCatalog(catalog, sections);
    const b = record.backlog = reconcile(catalog, gdd.agent.sha256, gdd.human.sha256, gdd.agent.version, previous);
    if (limits.goal) b.goal = limits.goal;
    b.catalogOrigin = record.catalogSource;
    await json(resolve(directory, 'catalog.json'), catalog);
    invalidateChanged(b, codeHashes(index));
    if (b.indexHash !== hash(JSON.stringify(index))) for (const e of b.entries) if (['blocked', 'unknown'].includes(e.status)) {
      e.status = 'uninspected'; e.explanation = 'Code inventory changed; reconsider prior blocker or unknown conclusion';
    }
    // Missing/unknown conclusions are re-investigated on each invocation. Design blockers retain reasons until evidence changes.
    for (const e of b.entries) if (['missing', 'partial'].includes(e.status)) e.status = 'uninspected';
    for (const e of b.entries.filter(e => e.status === 'verified')) {
      if (!e.inspection?.tests.length && !e.proof) { e.status = 'uninspected'; continue; }
      const tests = e.inspection?.tests ?? [];
      if (!tests.length) { e.status = 'uninspected'; continue; }
      const checks = await dependencies.check(root, checkScope(e.requirement.id, tests, e.checkMinimums), resolve(directory, 'rechecks', e.requirement.id));
      record.rechecks.push({ id: e.requirement.id, checks });
      if (checks.length !== tests.length || !checks.every(c => c.ok)) { e.status = 'uninspected'; e.explanation = 'Retained tests no longer pass; investigate regression'; }
    }
    const save = async (stage: string) => {
      const fresh = await dependencies.latest(root);
      if (fresh.agent.sha256 !== gdd.agent.sha256 || fresh.human.sha256 !== gdd.human.sha256) throw new Error('GDD changed during the run; start a new discovery run');
      index = await buildCodeIndex(root); invalidateChanged(b, codeHashes(index));
      const nextIndexHash = hash(JSON.stringify(index));
      if (b.indexHash !== nextIndexHash) for (const e of b.entries) if (['blocked', 'unknown'].includes(e.status)) {
        e.status = 'uninspected'; e.explanation = 'Code inventory changed; reconsider prior blocker or unknown conclusion';
      }
      b.indexHash = nextIndexHash;
      record.scans.push({ stage, indexHash: hash(JSON.stringify(index)), ranking: priorities(b) });
      await json(resolve(directory, `${stage}-index.json`), index); await json(resolve(root, backlogPathFor(limits.goal)), b); await saveReport(directory, record);
    };
    await save('initial');
    const attempted = new Set<string>();
    for (let n = 0; n < limits.maxInspections; n++) {
      const eligible = priorities(b).filter(p => p.eligible && !attempted.has(p.id));
      const next = eligible.find(p => p.status !== 'unknown') ?? eligible[0];
      if (!next) { record.stopReason = 'No further eligible work in this batch; inspect blocked, unknown, or dependency-gated entries'; break; }
      const entry = b.entries.find(e => e.requirement.id === next.id)!; attempted.add(next.id);
      console.log(`[goals] inspect ${next.id} (priority ${next.score})`);
      const inspectionDir = `${relative}/inspections/${next.id}`;
      const { result, context } = await inspectRequirement(root, resolve(root, inspectionDir), entry, index, ask);
      entry.inspection = structuredClone(result); entry.explanation = result.explanation; entry.inspectedHashes = Object.fromEntries(context.map(c => [c.path, c.sha256]));
      record.inspections.push({ id: next.id, directory: inspectionDir, result });
      entry.status = result.state === 'implemented' ? 'unknown' : result.state === 'inspect' ? 'unknown' : result.state;
      const priorFeature = entry.featureDirectory
        ? await Bun.file(await localPath(root, `${entry.featureDirectory}/feature.json`)).json() as FeatureRecord : undefined;
      const pendingReview = priorFeature && !priorFeature.result.verified && !priorFeature.testDiscarded
        && priorFeature.testPlan && (priorFeature.testChanges || priorFeature.priorFeature);
      if (pendingReview && result.state === 'implemented') {
        entry.status = 'partial'; entry.explanation = 'Implementation exists, but interrupted generation still needs independent review';
        entry.inspection.writable = priorFeature.writable;
      }
      if (result.state === 'implemented' && !pendingReview) {
        const checks = await dependencies.check(root, checkScope(next.id, result.tests), resolve(directory, 'existing', next.id));
        await json(resolve(directory, 'existing', next.id, 'checks.json'), checks);
        entry.checks = checks.map(c => c.reportFile);
        if (checks.length === result.tests.length && checks.every(c => c.ok)) {
          entry.status = 'verified'; entry.checkMinimums = Object.fromEntries(result.tests.map((t, i) => [t, checks[i]!.total]));
          entry.explanation = `Inspected behavior and current tests pass: ${result.explanation}`;
        }
        else { entry.status = 'partial'; entry.explanation = 'Claimed existing behavior failed current checks; needs repair inspection'; }
      }
      if (mode === 'run' && ['missing', 'partial'].includes(entry.status) && entry.inspection.writable.length && record.features.length < limits.maxFeatures) {
        const featureDirectory = `${relative}/features/${next.id}`;
        // Save the selection and exact code evidence before any model-authored write.
        await json(resolve(root, featureDirectory, 'selection.json'), { entry, ranking: priorities(b), context });
        const feature = await dependencies.feature(root, resolve(root, featureDirectory), entry, context, ask, limits);
        const savedFeatureText = await textOrNull(await localPath(root, `${featureDirectory}/feature.json`));
        const savedFeature = savedFeatureText ? JSON.parse(savedFeatureText) as FeatureRecord : undefined;
        if (savedFeature?.testPlan && !savedFeature.testDiscarded && (savedFeature.testChanges || savedFeature.priorFeature)) entry.featureDirectory = featureDirectory;
        record.features.push({ id: next.id, directory: featureDirectory, result: feature });
        entry.status = feature.verified ? 'verified' : 'partial';
        entry.explanation = feature.verified ? 'Independent generated acceptance tests and regression checks pass; reviewed and rescanned' : feature.error ?? 'Implementation remains incomplete';
        if (!feature.verified) record.stopReason = feature.error ?? 'Feature verification remains incomplete';
        if (feature.verified) {
          entry.proof = { testPath: feature.testPath, run: relative }; entry.inspection.tests = feature.tests;
          if (savedFeature?.testPlan) entry.checkMinimums = { [savedFeature.testPlan.className]: savedFeature.testPlan.minTests };
          const freshIndex = await buildCodeIndex(root), paths = new Set([...context.map(c => c.path), ...feature.writable, feature.testPath]);
          entry.inspectedHashes = Object.fromEntries(freshIndex.filter(c => paths.has(c.path)).map(c => [c.path, c.sha256]));
        }
      }
      entry.history.push({ run: relative, status: entry.status, reason: entry.explanation });
      await save(`after-${n + 1}`);
      if (complete(b)) { record.status = 'COMPLETE'; record.stopReason = 'Every discovered in-domain requirement has current inspection and passing tests'; break; }
      if (mode === 'run' && record.features.length >= limits.maxFeatures) { record.stopReason ??= 'Feature batch limit reached; remaining backlog is preserved for the next run'; break; }
    }
    if (complete(b)) {
      record.status = 'COMPLETE';
      record.stopReason = record.features.length
        ? 'Every discovered in-domain requirement has current inspection and passing tests'
        : 'Every scoped requirement is already verified and retained tests pass; no code write is needed';
    }
    record.stopReason ??= 'Inspection batch limit reached; unresolved requirements remain visible';
  } catch (error) {
    record.error = error instanceof Error ? error.message : String(error);
    record.status = error instanceof LimitReached ? 'INCOMPLETE' : 'ERROR'; record.stopReason = record.error;
  } finally {
    record.finishedAt = new Date().toISOString();
    if (record.backlog) await json(resolve(root, backlogPathFor(limits.goal)), record.backlog);
    await saveReport(directory, record); await lock.close(); await rm(lockPath);
  }
  return record;
}
