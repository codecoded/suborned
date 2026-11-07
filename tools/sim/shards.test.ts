// tools/sim/shards.ts without dotnet: the shard plan, the VSTest filter escaping and the TRX reader (brief 098 fast loop, Splice).
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { escape, nameOf, plan, readTrx } from './shards';

describe('sim:test shards', () => {
  test('a display name keeps its quotes and commas; brackets and the filter operators are escaped', () => {
    expect(escape('RulesTests.Q4Grid(t: "mercenary", approach: "help")')).toBe('RulesTests.Q4Grid\\(t: "mercenary", approach: "help"\\)');
    expect(escape('a=b|c&d!e~f\\g')).toBe('a\\=b\\|c\\&d\\!e\\~f\\\\g');
    expect(nameOf('ScenarioTests.Passes(id: "S01")')).toEqual({ cls: 'ScenarioTests', fq: 'ScenarioTests.Passes' });
    expect(nameOf('RedlineTests.E1_step_never_throws')).toEqual({ cls: 'RedlineTests', fq: 'RedlineTests.E1_step_never_throws' });
  });

  test('every test is in exactly one shard; a slow theory is cut down to single cases; small methods of a class ride together', () => {
    const slow = ['R.E7(c: "a")', 'R.E7(c: "b")', 'R.E7(c: "c")'], many = Array.from({ length: 100 }, (_, i) => `S.Passes(id: "S${i}")`);
    const tests = [...slow, ...many, 'R.F1', 'R.F2', 'T.Only'];
    const shards = plan(tests, { tests: Object.fromEntries(slow.map(t => [t, 100])), methods: { 'S.Passes': [100, 50] } }, 4);
    expect(shards.flatMap(s => s.tests).sort()).toEqual([...tests].sort());
    expect(shards.filter(s => s.id.startsWith('R.E7#')).map(s => s.tests.length)).toEqual([1, 1, 1]);
    expect(shards[0]!.cost).toBe(100); // the longest first
    expect(shards.filter(s => s.id.startsWith('S.Passes#')).every(s => s.tests.length <= 40)).toBe(true);
    const r = shards.find(s => s.tests.includes('R.F1'))!;
    expect(r.tests).toContain('R.F2'); expect(r.filter).toBe('FullyQualifiedName=R.F1|FullyQualifiedName=R.F2');
    expect(shards.find(s => s.id.startsWith('S.Passes#'))!.filter.split('|')[0]).toBe('DisplayName=S.Passes\\(id: "S0"\\)');
  });

  test('a TRX file reads to names, outcomes, seconds and the failure message', () => {
    const xml = `<TestRun><Results>
      <UnitTestResult executionId="1" testName="A.B(x: &quot;1&quot;)" outcome="Passed" duration="00:00:01.5000000" />
      <UnitTestResult executionId="2" testName="A.C" outcome="Failed" duration="00:01:02.2500000"><Output><ErrorInfo><Message>Assert.Equal() Failure&#xD;&#xA;Expected: 1</Message></ErrorInfo></Output></UnitTestResult>
    </Results></TestRun>`;
    expect(readTrx(xml)).toEqual([
      { name: 'A.B(x: "1")', outcome: 'Passed', seconds: 1.5, message: '' },
      { name: 'A.C', outcome: 'Failed', seconds: 62.25, message: 'Assert.Equal() Failure\nExpected: 1' },
    ]);
  });

  test('the committed seed names methods with cases and seconds', () => {
    const seed = JSON.parse(readFileSync(join(import.meta.dir, 'shards.seed.json'), 'utf8'));
    const rows = Object.entries(seed.methods as Record<string, [number, number]>);
    expect(rows.length).toBeGreaterThan(20);
    for (const [m, [n, s]] of rows) { expect(m).toMatch(/^\w+\.\w+$/); expect(n).toBeGreaterThan(0); expect(s).toBeGreaterThanOrEqual(0); }
  });
});
