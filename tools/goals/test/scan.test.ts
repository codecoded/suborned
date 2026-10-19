import { afterEach, describe, expect, test } from 'bun:test';
import { mkdtemp, mkdir, rm, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import { tmpdir } from 'node:os';
import { loadScope } from '../files';
import { rank, scan, type Scan } from '../scan';
import type { Analysis, CheckResult, Scope } from '../schema';

const dirs: string[] = [];
afterEach(async () => { await Promise.all(dirs.splice(0).map(p => rm(p, { recursive: true, force: true }))); });

const quote = 'A container service time is learnable by Watch.';
function scope(): Scope {
  return {
    id: 'fixture', goal: 'learn service time', sources: [{ path: 'GDD.md', quote }],
    requirements: ['clock', 'watch'].map(id => ({ id, title: id, source: 0, owner: 'simulation-engineer' as const,
      dependsOn: id === 'watch' ? ['clock'] : [], checks: [id] })),
    context: [{ path: 'sim/Suborned.Sim/Game.cs', from: 2, to: 2 }],
    writable: ['sim/Suborned.Sim/Game.cs', 'sim/Suborned.Sim/New.cs'], testProject: 'sim/tests',
    checks: ['clock', 'watch', 'regression'].map(id => ({ id, filter: id, minTests: 1 })), regression: 'regression',
  };
}
function checked(id: string, ok: boolean): CheckResult {
  return { id, argv: ['test', id], exitCode: ok ? 0 : 1, timedOut: false, total: 1, passed: ok ? 1 : 0,
    failed: ok ? 0 : 1, ok, outputFile: 'test.txt', reportFile: 'test.xml', outputHash: 'fixture', seconds: 0 };
}
function scanned(): Scan {
  return { inventory: {}, sources: [{ path: 'sim/Suborned.Sim/Game.cs', from: 2, to: 2, sha256: 'fixture', text: 'return (next, false);' }],
    checks: [checked('clock', true), checked('watch', false)] };
}
function analysis(): Analysis {
  return { summary: 'clock exists; Watch missing', features: ['clock', 'watch'].map(id => ({
    id, state: id === 'clock' ? 'satisfied' as const : 'missing' as const, explanation: 'source and checks',
    evidence: [{ path: 'sim/Suborned.Sim/Game.cs', quote: 'return (next, false);' }],
    value: 3, effort: 2, risk: 1, reason: 'existing rule implemented with bounded work',
  })) };
}
async function fixture(): Promise<string> {
  const root = await mkdtemp(join(tmpdir(), 'a05-scan-test-')); dirs.push(root);
  await mkdir(join(root, 'sim/Suborned.Sim'), { recursive: true });
  await writeFile(join(root, 'GDD.md'), `# GDD\n${quote}\n`);
  await writeFile(join(root, 'sim/Suborned.Sim/Game.cs'), '// hidden context\nreturn (next, false);\n// trailing context');
  return root;
}

describe('goal scan and ranking', () => {
  test('verification requires a satisfied source assessment and passing behavioral checks', () => {
    expect(rank(scope(), scanned(), analysis()).find(r => r.id === 'clock')?.state).toBe('verified');
    const a = analysis(); a.features[1]!.state = 'satisfied';
    expect(rank(scope(), scanned(), a).find(r => r.id === 'watch')?.state).toBe('unknown');
    const s = scanned(); s.checks.push(checked('watch', true));
    expect(rank(scope(), s, analysis()).find(r => r.id === 'watch')?.state).toBe('missing');
  });

  test('every referenced check must pass and a missing check cannot certify success', () => {
    const sc = scope(); sc.requirements[0]!.checks.push('regression');
    expect(rank(sc, scanned(), analysis()).find(r => r.id === 'clock')?.state).toBe('unknown');
    const s = scanned(); s.checks = [];
    expect(rank(scope(), s, analysis()).find(r => r.id === 'clock')?.state).toBe('unknown');
  });

  test('literal source evidence must be present in the supplied context, not just the inventory', () => {
    const a = analysis(); a.features[0]!.evidence[0]!.quote = 'return (next, true);';
    expect(() => rank(scope(), scanned(), a)).toThrow('Unseen evidence');
    a.features[0]!.evidence[0] = { path: 'sim/Other.cs', quote: 'return (next, false);' };
    const s = scanned(); s.inventory['sim/Other.cs'] = 'fixture';
    expect(() => rank(scope(), s, a)).toThrow('Unseen evidence');
  });

  test('missing, duplicate, or extra model requirement IDs reject the entire assessment', () => {
    const a = analysis(); a.features.pop();
    expect(() => rank(scope(), scanned(), a)).toThrow('exactly once');
    const duplicate = analysis(); duplicate.features[1]!.id = 'clock';
    expect(() => rank(scope(), scanned(), duplicate)).toThrow('exactly once');
    const unknown = analysis(); unknown.features[1]!.id = 'invented';
    expect(() => rank(scope(), scanned(), unknown)).toThrow('exactly once');
  });

  test('dependencies require verified evidence and unknown/blocked work is ineligible', () => {
    expect(rank(scope(), scanned(), analysis()).find(r => r.id === 'watch')?.eligible).toBe(true);
    for (const state of ['missing', 'partial', 'blocked', 'unknown'] as const) {
      const a = analysis(); a.features[0]!.state = state;
      expect(rank(scope(), scanned(), a).find(r => r.id === 'watch')?.eligible).toBe(false);
    }
    for (const state of ['blocked', 'unknown'] as const) {
      const a = analysis(); a.features[1]!.state = state;
      expect(rank(scope(), scanned(), a).find(r => r.id === 'watch')?.eligible).toBe(false);
    }
  });

  test('score counts direct work unlocked and ties use a stable requirement ID order', () => {
    const rows = rank(scope(), scanned(), analysis());
    expect(rows.map(r => [r.id, r.score, r.unblocks])).toEqual([['clock', 11, 1], ['watch', 9, 0]]);
    const sc = scope(); sc.requirements[1]!.dependsOn = [];
    const a = analysis(); a.features.reverse(); sc.requirements.reverse();
    expect(rank(sc, scanned(), a).map(r => r.id)).toEqual(['clock', 'watch']);
  });

  test('rescan reads changed source, includes newly created writable files, and pins GDD quotes', async () => {
    const root = await fixture(); const sc = scope();
    const first = await scan(root, sc, []);
    expect(first.sources.find(s => s.path.endsWith('Game.cs'))?.text).toBe('return (next, false);');
    await writeFile(join(root, 'sim/Suborned.Sim/Game.cs'), '// changed\nreturn (next, true);');
    await writeFile(join(root, 'sim/Suborned.Sim/New.cs'), 'class New {}');
    const second = await scan(root, sc, []);
    expect(second.inventory['sim/Suborned.Sim/Game.cs']).not.toBe(first.inventory['sim/Suborned.Sim/Game.cs']);
    expect(second.sources.find(s => s.path.endsWith('New.cs'))?.text).toBe('class New {}');
    await writeFile(join(root, 'GDD.md'), '# Requirement has changed');
    await expect(scan(root, sc, [])).rejects.toThrow('Pinned requirement');
  });

  test('scope loader rejects missing dependencies and cyclic prerequisite graphs', async () => {
    const root = await fixture(); const sc = scope(); sc.requirements[0]!.dependsOn = ['missing'];
    await writeFile(join(root, 'scope.json'), JSON.stringify(sc));
    await expect(loadScope(root, 'scope.json')).rejects.toThrow('Invalid references');
    sc.requirements[0]!.dependsOn = ['watch'];
    await writeFile(join(root, 'scope.json'), JSON.stringify(sc));
    await expect(loadScope(root, 'scope.json')).rejects.toThrow('Dependency cycle');
  });
});
