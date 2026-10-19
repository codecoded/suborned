import { afterEach, expect, test } from 'bun:test';
import { mkdir, mkdtemp, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { resolve } from 'node:path';
import { auditInputs, inputDirectories } from '../audit';
import { fileHashes, fileState, sourceFiles } from '../files';
import { applyPatch } from '../patch';
import type { RunRecord } from '../record';
import type { Scope } from '../schema';

const roots: string[] = [];
afterEach(async () => { for (const root of roots.splice(0)) await rm(root, { recursive: true, force: true }); });

async function fixture() {
  const root = await mkdtemp(resolve(tmpdir(), 'a05-audit-test-')); roots.push(root);
  await Promise.all(inputDirectories.map(path => mkdir(resolve(root, path), { recursive: true })));
  const existing = 'sim/Suborned.Sim/Game.cs', added = 'sim/Suborned.Sim/Watch.cs', acceptance = 'sim/Tests/Acceptance.cs';
  await Bun.write(resolve(root, existing), 'public class Game { }\n');
  await Bun.write(resolve(root, acceptance), 'public class Acceptance { /* immutable assertions */ }\n');
  const scope: Scope = {
    id: 'audit-fixture', goal: 'Implement Watch', sources: [{ path: 'GDD.md', quote: 'Scout learns timings by Watch.' }],
    requirements: [{ id: 'watch', title: 'Watch', source: 0, owner: 'simulation-engineer', dependsOn: [], checks: ['watch'] }],
    context: [{ path: existing }], writable: [existing, added], testProject: 'sim/Tests',
    checks: [{ id: 'watch', filter: 'Watch', minTests: 1 }], regression: 'watch',
  };
  await Bun.write(resolve(root, 'GDD.md'), scope.sources[0]!.quote);
  const record: RunRecord = {
    version: 1, id: 'fixture', scopePath: 'scope.json', scope, status: 'INCOMPLETE', approvedBy: 'offline test', startedAt: '2026-10-19',
    limits: { maxUsd: 1, maxCalls: 4, maxIterations: 1 },
    inputs: await fileHashes(root, [...await sourceFiles(root, inputDirectories), 'GDD.md']),
    baseline: await fileState(root, scope.writable), baselineChecks: [], rounds: [], calls: [], manualEdits: [],
  };
  return { root, record, existing, added, acceptance };
}

test('audit accepts both recorded modifications and newly generated writable source', async () => {
  const f = await fixture();
  await expect(auditInputs(f.root, f.record)).resolves.toBeUndefined();
  const patch = { summary: 'Add Watch', edits: [
    { path: f.existing, search: 'public class Game { }', replacement: 'public class Game { public bool WatchKnown; }' },
    { path: f.added, search: '', replacement: 'public class Watch { }\n' },
  ] };
  const changes = await applyPatch(f.root, patch, f.record.scope.writable, f.record.baseline);
  f.record.rounds.push({ index: 1, selected: 'watch', ranking: [], patch, changes, checks: [] });
  await expect(auditInputs(f.root, f.record)).resolves.toBeUndefined();
});

test('audit refuses changed immutable acceptance tests', async () => {
  const f = await fixture();
  await Bun.write(resolve(f.root, f.acceptance), 'public class Acceptance { /* assertions removed */ }\n');
  await expect(auditInputs(f.root, f.record)).rejects.toThrow(`Pinned input changed during run: ${f.acceptance}`);
});

test('audit refuses unrecorded edits to otherwise writable source', async () => {
  const f = await fixture();
  await Bun.write(resolve(f.root, f.existing), 'public class Game { public bool ChangedBySomeoneElse; }\n');
  await expect(auditInputs(f.root, f.record)).rejects.toThrow('Writable source differs from the recorded generated changes');
});

test('audit refuses newly injected source outside the declared write scope', async () => {
  const f = await fixture();
  await Bun.write(resolve(f.root, 'sim/Tests/Injected.cs'), 'public class Injected { }\n');
  await expect(auditInputs(f.root, f.record)).rejects.toThrow('Input inventory changed during run');
});
