import { afterEach, describe, expect, test } from 'bun:test';
import { mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import { tmpdir } from 'node:os';
import { runLoop, type LoopDependencies } from '../loop';
import { LimitReached, type Ask } from '../models';
import { applyPatch } from '../patch';
import { scan } from '../scan';
import type { RunRecord } from '../record';
import type { Analysis, CheckResult, Scope } from '../schema';

const roots: string[] = [];
afterEach(async () => { await Promise.all(roots.splice(0).map(root => rm(root, { recursive: true, force: true }))); });
const codePath = 'sim/Suborned.Sim/Game.cs';
const code = (stage: number) => `// fixture implementation stage ${stage}\n`;
type Options = { maxIterations?: number; failFirstCheck?: boolean; failFirstReview?: boolean; optimisticReview?: boolean; budgetAtReview?: boolean; blocked?: boolean };

async function fixture(options: Options = {}) {
  const root = await mkdtemp(join(tmpdir(), 'a05-loop-test-')); roots.push(root);
  await mkdir(join(root, 'sim/Suborned.Sim'), { recursive: true });
  await writeFile(join(root, codePath), code(0));
  const quote = 'A container service time is learnable by Watch.';
  await writeFile(join(root, 'GDD.md'), quote);
  await writeFile(join(root, 'failure.log'), 'The pinned behavior has not been implemented.');
  const scope: Scope = {
    id: 'loop-fixture', goal: 'learn service time', sources: [{ path: 'GDD.md', quote }],
    requirements: [{ id: 'watch', title: 'Watch', source: 0, owner: 'simulation-engineer', dependsOn: [], checks: ['watch'] }],
    context: [{ path: codePath }], writable: [codePath], testProject: 'sim/tests',
    checks: [{ id: 'watch', filter: 'WatchTests', minTests: 1 }], regression: 'watch',
  };
  const record: RunRecord = {
    version: 1, id: 'fixture', scopePath: 'scope.json', scope, status: 'INCOMPLETE', approvedBy: 'offline test',
    startedAt: new Date(0).toISOString(), limits: { maxUsd: 1, maxCalls: 20, maxIterations: options.maxIterations ?? 2 },
    inputs: {}, baseline: { [codePath]: code(0) }, baselineChecks: [], rounds: [], calls: [], manualEdits: [],
  };
  const events: string[] = [], prompts: { kind: string; index: number; text: string }[] = [];
  let stage = 0;
  const analysis = (): Analysis => ({ summary: 'fixture assessment', features: [{
    id: 'watch', state: options.blocked ? 'blocked' : stage === 0 ? 'missing' : 'satisfied',
    explanation: 'Direct source and behavioral evidence', evidence: [{ path: codePath, quote: code(stage).trim() }],
    value: 5, effort: 1, risk: 1, reason: 'Bounded capstone requirement',
  }] });
  const check: LoopDependencies['check'] = async () => {
    events.push(`check-${stage}`);
    const ok = stage > 0 && !(options.failFirstCheck && stage === 1);
    const result: CheckResult = {
      id: 'watch', argv: ['offline-fixture'], exitCode: ok ? 0 : 1, timedOut: false,
      total: 1, passed: ok ? 1 : 0, failed: ok ? 0 : 1, ok, outputFile: 'failure.log',
      reportFile: 'fixture.trx', outputHash: 'fixture', seconds: 0,
    };
    return [result];
  };
  const ask: Ask = async (kind, index, role, task, schema) => {
    events.push(`${kind}-${index}`); prompts.push({ kind, index, text: task });
    if (kind === 'write') {
      expect(role).toBe('simulation-engineer');
      return schema.parse({ summary: `implement stage ${stage + 1}`, edits: [{ path: codePath, search: code(stage), replacement: code(stage + 1) }] });
    }
    if (kind === 'review') {
      expect(role).toBe('code-performance-reviewer');
      if (options.budgetAtReview) throw new LimitReached('Model call limit reached after write');
      const failed = stage === 1 && (options.failFirstReview || (options.failFirstCheck && !options.optimisticReview));
      return schema.parse({ verdict: failed ? 'fail' : 'pass', summary: failed ? 'Repair required' : 'Checked independently',
        faults: failed ? [{ path: codePath, problem: 'Behavior incomplete', fix: 'Implement remaining behavior' }] : [] });
    }
    expect(role).toBe('lead-producer');
    return schema.parse(analysis());
  };
  const deps: LoopDependencies = {
    ask, check,
    scan: async (r, s, checks) => { events.push(`scan-${stage}`); return scan(r, s, checks); },
    apply: async (...args) => { events.push(`apply-${stage + 1}`); const changed = await applyPatch(...args); stage++; return changed; },
  };
  const directory = join(root, 'runs/fixture');
  return { root, directory, record, deps, events, prompts };
}

describe('goal coordinator iteration', () => {
  test('writes, verifies, rescans, independently reviews, then reassesses before completion', async () => {
    const f = await fixture();
    const result = await runLoop(f.root, f.directory, f.record, f.deps);
    expect(f.events).toEqual(['check-0', 'scan-0', 'assess-0', 'write-1', 'apply-1', 'check-1', 'scan-1', 'review-1', 'assess-1']);
    expect(result.status).toBe('COMPLETE');
    expect(result.rounds).toHaveLength(1);
    expect(result.rounds[0]!.changes[0]?.after).toBe(code(1));
    expect(result.finalScan?.sources.find(s => s.path === codePath)?.text).toBe(code(1));
    expect(result.ranking?.[0]?.state).toBe('verified');
    const saved = JSON.parse(await readFile(join(f.directory, 'run.json'), 'utf8'));
    expect(saved.status).toBe('COMPLETE');
    expect(saved.finishedAt).toBeString();
  });

  for (const mode of ['failFirstCheck', 'failFirstReview'] as const) {
    test(`repairs ${mode === 'failFirstCheck' ? 'failed checks' : 'an independent review rejection'} in a second recorded iteration`, async () => {
      const f = await fixture({ [mode]: true });
      const result = await runLoop(f.root, f.directory, f.record, f.deps);
      expect(result.status).toBe('COMPLETE');
      expect(result.rounds).toHaveLength(2);
      expect(result.rounds[0]?.review?.verdict).toBe('fail');
      expect(result.rounds[1]?.review?.verdict).toBe('pass');
      expect(f.events.slice(-6)).toEqual(['write-2', 'apply-2', 'check-2', 'scan-2', 'review-2', 'assess-2']);
      expect(f.prompts.find(p => p.kind === 'write' && p.index === 2)?.text).toContain('Behavior incomplete');
      expect(f.prompts.find(p => p.kind === 'write' && p.index === 2)?.text).toContain('fixture implementation stage 1');
      expect(await readFile(join(f.root, codePath), 'utf8')).toBe(code(2));
    });
  }

  test('iteration exhaustion remains INCOMPLETE even if the model claims satisfaction', async () => {
    const f = await fixture({ failFirstReview: true, maxIterations: 1 });
    const result = await runLoop(f.root, f.directory, f.record, f.deps);
    expect(result.ranking?.[0]?.state).toBe('verified'); // passing tests and optimistic assessment still need reviewer approval
    expect(result.status).toBe('INCOMPLETE');
    expect(result.error).toContain('Iteration limit');
    expect(result.rounds[0]?.review?.verdict).toBe('fail');
    expect(JSON.parse(await readFile(join(f.directory, 'run.json'), 'utf8')).status).toBe('INCOMPLETE');
  });

  test('failed checks force a repair even when review and assessment both claim success', async () => {
    const f = await fixture({ failFirstCheck: true, optimisticReview: true });
    const result = await runLoop(f.root, f.directory, f.record, f.deps);
    expect(result.status).toBe('COMPLETE');
    expect(result.rounds).toHaveLength(2);
    expect(result.rounds[0]?.checks[0]?.ok).toBe(false);
    expect(result.rounds[0]?.review?.verdict).toBe('pass');
    expect(result.rounds[0]?.analysis?.features[0]?.state).toBe('satisfied');
    expect(result.rounds[1]?.selected).toBe('watch');
    expect(result.rounds[1]?.checks[0]?.ok).toBe(true);
    expect(f.prompts.find(p => p.kind === 'write' && p.index === 2)?.text).toContain('The pinned behavior has not been implemented.');
  });

  test('a budget stop after a real application preserves mechanical rescan and incomplete status', async () => {
    const f = await fixture({ budgetAtReview: true });
    const result = await runLoop(f.root, f.directory, f.record, f.deps);
    expect(result.status).toBe('INCOMPLETE');
    expect(result.error).toContain('Model call limit');
    expect(await readFile(join(f.root, codePath), 'utf8')).toBe(code(1));
    expect(result.rounds[0]?.checks[0]?.ok).toBe(true);
    expect(result.rounds[0]?.scan?.sources.find(s => s.path === codePath)?.text).toBe(code(1));
    expect(result.finalScan?.sources.find(s => s.path === codePath)?.text).toBe(code(1));
    expect(f.events).not.toContain('assess-1');
    const saved = JSON.parse(await readFile(join(f.directory, 'run.json'), 'utf8'));
    expect(saved.rounds[0].changes).toHaveLength(1);
    expect(saved.rounds[0].scan.sources.find((s: { path: string }) => s.path === codePath).text).toBe(code(1));
    expect(saved.status).toBe('INCOMPLETE');
  });

  test('blocked scope does not invoke a writer or claim completion', async () => {
    const f = await fixture({ blocked: true });
    const result = await runLoop(f.root, f.directory, f.record, f.deps);
    expect(result.status).toBe('INCOMPLETE');
    expect(result.error).toContain('No eligible requirement');
    expect(f.events).toEqual(['check-0', 'scan-0', 'assess-0']);
    expect(result.rounds).toHaveLength(0);
    expect(await readFile(join(f.root, codePath), 'utf8')).toBe(code(0));
  });
});
