import { expect, test } from 'bun:test';
import { validateInspection, checkScope, inspectRequirement } from '../campaign';
import type { Inspection, Entry } from '../backlog';
import { mkdtemp, mkdir, writeFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { resolve } from 'node:path';
import { buildCodeIndex } from '../discovery-sources';
import type { CampaignAsk } from '../models';
const production = { path: 'sim/Suborned.Sim/Game.cs', sha256: 'abc', from: 1, to: 2, text: 'public bool IsAvailable() => !Dead;' };
const tests = { path: 'sim/Suborned.Sim.Tests/Tests.cs', sha256: 'def', from: 1, to: 2, text: 'public void DeadAgentCannotAct() { Assert.False(game.IsAvailable()); }' };
function result(): Inspection { return { state: 'implemented', explanation: 'The public gate and behavior test enforce this requirement.', evidence: [{ path: production.path, quote: production.text }], reads: [], searches: [], tests: ['Suborned.Sim.Tests.Tests.DeadAgentCannotAct'], writable: [] }; }
test('an inspection can use successive model, writer and consumer reads before its supported conclusion', async () => {
  const root = await mkdtemp(resolve(tmpdir(), 'goals-inspect-'));
  try {
    await mkdir(resolve(root, 'sim/Suborned.Sim'), { recursive: true });
    await mkdir(resolve(root, 'tools/sim'), { recursive: true });
    await writeFile(resolve(root, production.path), production.text);
    await writeFile(resolve(root, 'tools/sim/consumer.ts'), 'export const contract = "open";');
    const index = await buildCodeIndex(root);
    const entry = { requirement: { id: 'query', title: 'Read-only query', quote: 'A design quote', terms: ['IsAvailable'] } } as Entry;
    const remaining: number[] = [];
    const ask: CampaignAsk = async (_id, _role, _brief, task, schema) => {
      const input = JSON.parse(task); remaining.push(input.exchangesRemaining);
      if (remaining.length <= 3) return schema.parse({ state: 'inspect', explanation: 'Read the next relevant contract before concluding.', evidence: [],
        reads: [{ path: remaining.length === 3 ? 'tools/sim/consumer.ts' : production.path, from: 1, to: 1 }], searches: [], tests: [], writable: [] });
      expect(input.context.some((s: {path:string}) => s.path === 'tools/sim/consumer.ts')).toBe(true);
      return schema.parse({ state: 'partial', explanation: 'The inspected query exists but its behavior lacks coverage.', evidence: [{ path: production.path, quote: production.text }],
        reads: [], searches: [], tests: [], writable: [production.path] });
    };
    const inspected = await inspectRequirement(root, resolve(root, 'evidence'), entry, index, ask);
    expect(inspected.result.state).toBe('partial');
    expect(remaining).toEqual([5, 4, 3, 2]);
  } finally { await rm(root, { recursive: true, force: true }); }
});
test('inspection needs actual read execution path and test context', () => {
  expect(() => validateInspection(result(), [production, tests])).not.toThrow();
  expect(() => validateInspection(result(), [])).toThrow('Unseen');
  expect(() => validateInspection(result(), [production])).toThrow('inspected tests');
  const invented = result(); invented.evidence[0]!.quote = 'This method was never read';
  expect(() => validateInspection(invented, [production, tests])).toThrow('Unseen');
});
test('gap repair is bounded to production C# and never trusts pending reads', () => {
  const gap = result(); gap.state = 'missing'; gap.tests = [];
  expect(() => validateInspection(gap, [production])).toThrow('repair paths');
  gap.writable = ['sim/Suborned.Sim.Tests/Tests.cs'];
  expect(() => validateInspection(gap, [production])).toThrow('shared-engine');
  gap.writable = [production.path]; expect(() => validateInspection(gap, [production])).not.toThrow();
  gap.reads = [{ path: production.path, from: 1, to: 2 }];
  expect(() => validateInspection(gap, [production])).toThrow('pending reads');
});
test('inspection requests and test selectors cannot inject commands or arbitrary filters', () => {
  const request = result(); request.state = 'inspect';
  expect(() => validateInspection(request, [])).toThrow('no reads');
  request.searches = ['IsAvailable']; expect(() => validateInspection(request, [])).not.toThrow();
  expect(() => checkScope('id', ['A|FullyQualifiedName~Anything'])).toThrow('selector');
  expect(() => checkScope('id', ['$(touch unwanted)'])).toThrow('selector');
  expect(checkScope('id', ['A.Test', 'B.Test'], {'B.Test': 4}).checks).toEqual([
    {id:'acceptance-1',filter:'FullyQualifiedName~A.Test',minTests:1},
    {id:'acceptance-2',filter:'FullyQualifiedName~B.Test',minTests:4},
  ]);
});
