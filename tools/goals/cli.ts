import { mkdir, open, rm } from 'node:fs/promises';
import { resolve } from 'node:path';
import { root } from '../rag/config';
import { ensureRetrieval } from './bootstrap';
import { auditInputs, inputDirectories } from './audit';
import { runChecks } from './checks';
import { fileHashes, fileState, json, loadScope, localPath, sourceFiles } from './files';
import { runLoop } from './loop';
import { modelCaller, validateLimits, type Limits } from './models';
import { applyPatch } from './patch';
import { scan } from './scan';
import type { RunRecord } from './record';
import { verifyRun } from './verify';
import { currentStatus, runCampaign } from './campaign';
import { verifyCampaign } from './verify-campaign';

async function main() {
  const [command, ...args] = process.argv.slice(2);
  if (command === 'setup') { if (args.length) throw new Error('setup takes no arguments'); console.log(JSON.stringify(await ensureRetrieval(), null, 2)); return; }
  if (command === 'status') {
    if (args.length && (args.length !== 2 || args[0] !== '--goal' || !args[1]?.trim())) throw new Error('Usage: goals status [--goal <same goal text>]');
    console.log(JSON.stringify(await currentStatus(root, args[1]), null, 2)); return;
  }
  if (command === 'verify') {
    if (args.length !== 1) throw new Error('Usage: bun run goals verify runs/goals/<run>');
    const archived = await Bun.file(await localPath(root, `${args[0]!}/run.json`)).json();
    if (archived.version === 2) { console.log(JSON.stringify(await verifyCampaign(root, args[0]!), null, 2)); return; }
    const result = await verifyRun(root, args[0]!);
    console.log(JSON.stringify({ ...result, replayed: result.replayed.map(r => ({ stage: r.stage, checks: r.checks.map(c => ({ id: c.id, passed: c.passed, failed: c.failed, ok: c.ok })) })) }, null, 2)); return;
  }
  if (command !== 'run' && command !== 'scan') throw new Error('Usage: bun run goals setup | status [--goal <text>] | scan/run --approved-by <name> [--goal <text>] [--provider claude --max-usd 10 | --provider codex --model <id> --max-tokens 500000] [--max-iterations 3] [--max-calls 20] [--max-features 1] [--max-inspections 8] | verify <run-directory>; explicit --scope selects legacy mode');
  const flags: Record<string, string> = {};
  for (let i = 0; i < args.length; i += 2) {
    const name = args[i]!, value = args[i + 1];
    if (!['--scope', '--approved-by', '--max-usd', '--max-iterations', '--max-calls', '--max-features', '--max-inspections', '--provider', '--model', '--max-tokens', '--goal'].includes(name) || value === undefined || value.startsWith('--') || flags[name]) throw new Error(`Unknown, repeated or incomplete option: ${name}`);
    flags[name] = value;
  }
  const provider = flags['--provider'] ?? 'claude';
  if (provider !== 'claude' && provider !== 'codex') throw new Error('provider must be claude or codex');
  if (provider === 'claude' && (flags['--model'] || flags['--max-tokens'])) throw new Error('model/max-tokens CLI options apply to Codex; Claude uses its existing card model and dollar budget');
  const limits: Limits = { provider,
    ...(provider === 'codex' ? { model: flags['--model'], maxTokens: Number(flags['--max-tokens'] ?? 500000), ...(flags['--max-usd'] ? { maxUsd: Number(flags['--max-usd']) } : {}) } : { maxUsd: Number(flags['--max-usd'] ?? 10) }),
    ...(flags['--goal'] !== undefined ? { goal: flags['--goal'].trim() } : {}),
    maxIterations: Number(flags['--max-iterations'] ?? 3), maxCalls: Number(flags['--max-calls'] ?? 20) };
  const approvedBy = flags['--approved-by'] ?? '';
  validateLimits(limits, approvedBy);
  if (!flags['--scope']) {
    const record = await runCampaign(root, command, approvedBy, { ...limits, maxFeatures: Number(flags['--max-features'] ?? 1), maxInspections: Number(flags['--max-inspections'] ?? 8) });
    const report = `runs/goals/${record.id}/index.html`;
    console.log(JSON.stringify({ status: record.status, run: `runs/goals/${record.id}`, report: await Bun.file(resolve(root, report)).exists() ? report : null, evidence: `runs/goals/${record.id}/run.json`, goal: limits.goal, features: record.features.map(f => ({ id: f.id, ...f.result })), calls: record.calls.length, costUsd: provider === 'codex' ? null : record.calls.reduce((n,c) => n+(c.costUsd??0),0), stopReason: record.stopReason }, null, 2));
    if (record.status !== 'COMPLETE') process.exitCode = record.status === 'ERROR' ? 1 : 2;
    return;
  }
  if (command === 'scan' || flags['--max-features'] || flags['--max-inspections'] || provider !== 'claude' || limits.goal) throw new Error('Legacy --scope supports Claude run and legacy options only');
  const scopePath = flags['--scope'], scope = await loadScope(root, scopePath);
  const id = new Date().toISOString().replace(/[:.]/g, '-') + '-' + scope.id;
  const relative = `runs/goals/${id}`, directory = await localPath(root, relative);
  const record: RunRecord = { version: 1, id, scopePath, scope, status: 'INCOMPLETE', approvedBy, startedAt: new Date().toISOString(), limits: { maxUsd: limits.maxUsd!, maxCalls: limits.maxCalls, maxIterations: limits.maxIterations },
    inputs: {}, baseline: {}, baselineChecks: [], rounds: [], calls: [], manualEdits: [] };
  const ask = modelCaller(relative, record.calls, limits, () => record.bootstrap?.corpusHash); // validate opt-in before DB startup or any write
  await mkdir(resolve(root, 'out'), { recursive: true });
  const lockPath = await localPath(root, 'out/goals.lock');
  const lock = await open(lockPath, 'wx').catch(() => { throw new Error('Another goal run holds out/goals.lock. Inspect the process before removing a stale lock.'); });
  try {
    await lock.writeFile(JSON.stringify({ pid: process.pid, run: relative }));
    await mkdir(directory, { recursive: true });
    const paths = [...new Set([...(await sourceFiles(root, inputDirectories)), scopePath, ...scope.sources.map(s => s.path), ...scope.context.map(c => c.path)])].sort();
    record.inputs = await fileHashes(root, paths); record.baseline = await fileState(root, scope.writable);
    await json(resolve(directory, 'run.json'), record);
    console.log(`[goals] evidence: ${relative}`);
    record.bootstrap = await ensureRetrieval();
    await json(resolve(directory, 'run.json'), record);
    const audit = () => auditInputs(root, record);
    await runLoop(root, directory, record, { ask, check: runChecks, scan, apply: applyPatch, audit });
    console.log(JSON.stringify({ status: record.status, run: relative, rounds: record.rounds.length, costUsd: record.calls.reduce((n, c) => n + (c.costUsd ?? 0), 0), error: record.error }, null, 2));
    if (record.status !== 'COMPLETE') process.exitCode = 1;
  } catch (error) {
    record.status = 'ERROR'; record.error = error instanceof Error ? error.message : String(error); record.finishedAt = new Date().toISOString();
    await json(resolve(directory, 'run.json'), record); throw error;
  } finally { await lock.close(); await rm(lockPath); }
}

if (import.meta.main) main().catch(error => { console.error(error instanceof Error ? error.message : error); process.exitCode = 1; });
