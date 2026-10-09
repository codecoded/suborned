// The whole pipeline with no model and no index: a stub CLAUDE_BIN answers from fixtures, retrieval is lexical, and every
// run lands in a scratch folder (LEVELS_RUNS / LEVELS_OUT), never in runs/.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { cp, mkdir, mkdtemp, readdir } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { root } from '../../rag/config';
import { hash, loadCorpus } from '../../rag/corpus';
import { apply, seedStationOnPlot } from '../apply';
import { ownerOf } from '../checker';
import { nextMinor, candidateVersion } from '../paths';
import { replay } from '../replay';
import { z } from 'zod';
import { cliSchema } from '../../rag/claude';
import { agents } from '../schemas';
import { schemaProblems } from './schema-lint';

setDefaultTimeout(180000);
const here = import.meta.dir, scratch = await mkdtemp(join(tmpdir(), 'levels-'));
const runs = join(scratch, 'runs'), out = join(scratch, 'out'), state = join(scratch, 'state'), planFile = join(scratch, 'plan.json');
await Bun.$`mkdir -p ${state}`;
const base = await Bun.file(resolve(root, 'contracts/world/harbour-district.plan.json')).json();
base.standards.transit.reach = 300; // so the fixtures' unchanged network is a valid one; the draft's own waivers are stripped by the run
await Bun.write(planFile, JSON.stringify(base));
const env = { ...process.env, LEVELS_RUNS: runs, LEVELS_OUT: out, LEVELS_TEST_RETRIEVER: 'lexical', CLAUDE_BIN: join(here, 'claude-stub.ts') };
const levelsWith = async (extra: Record<string, string>, plan: string, ...args: string[]) => {
  await Bun.$`rm -rf ${state} && mkdir -p ${state}`;
  const p = Bun.spawn(['bun', 'tools/levels/cli.ts', 'transit', '--plan', plan, ...args], { cwd: root, env: { ...env, ...extra, STUB_STATE: state }, stdout: 'pipe', stderr: 'pipe' });
  const [stdout, stderr, code] = await Promise.all([new Response(p.stdout).text(), new Response(p.stderr).text(), p.exited]);
  return { stdout, stderr, code };
};
const levels = (...args: string[]) => levelsWith({}, planFile, ...args);
const withRuns = async <T>(f: () => Promise<T>) => { process.env.LEVELS_RUNS = runs; try { return await f(); } finally { delete process.env.LEVELS_RUNS; } };

describe('levels transit, stubbed', () => {
  test('fixtures satisfy the output schemas', async () => {
    for (const f of await readdir(join(here, 'fixtures'))) {
      const agent = ({ 'transit-lines': 'datum', 'transit-stops': 'vantage', 'transit-timetable': 'pulse', 'transit-critic': 'keel' } as const)[f.split('.')[0] as 'transit-lines'];
      const r = agents[agent].safeParse(JSON.parse((await Bun.file(join(here, 'fixtures', f)).text()).replaceAll('__CHUNK__', 'a'.repeat(24))));
      expect(r.success, f).toBe(true);
    }
  });

  test('a full run: round 1 fails the checker, the owner repairs it, round 2 is ACCEPTED; replay passes', async () => {
    const r = await levels();
    expect(r.stdout).toContain('r001 ACCEPTED (0.2.0-r001.2)'); expect(r.code).toBe(0);
    const dir = join(runs, 'harbour-district', 'r001'), run = await Bun.file(join(dir, 'run.json')).json();
    expect(run).toMatchObject({ outcome: 'ACCEPTED', seeded: false, objective: 'transit', finalVersion: '0.2.0-r001.2', inputPlan: { version: '0.1.0' } });
    expect(run.rounds.map((x: any) => [x.checkerOk, x.verdict, x.faultOwners])).toEqual([[false, 'fail', ['Vantage']], [true, 'pass', []]]);
    expect(run.rounds[1].ran).toEqual(['vantage', 'pulse']); expect(run.rounds[1].reused).toEqual(['datum']);
    const sheets = await readdir(join(out, 'harbour-district', 'r001', 'sheets')); expect(sheets).toContain('harbour-district-v0.2.0-r001.2.html'); expect(sheets).toContain('harbour-district-v0.2.0-r001.1.html');
    expect(await Bun.file(join(dir, 'round-1', 'plan.json')).json()).toMatchObject({ version: '0.2.0-r001.1' });
    expect((await readdir(join(out, 'harbour-district', 'r001', 'calls'))).some(f => f.endsWith('.transcript') || f.endsWith('.events.jsonl'))).toBe(true);
    const res = await withRuns(() => replay('harbour-district', 'r001'));
    expect(res.lines.filter(l => l.startsWith('FAIL'))).toEqual([]); expect(res.ok).toBe(true);
  });

  test('a seeded run is labelled everywhere and ends with the correction', async () => {
    const r = await levels('--seed', 'station-on-plot');
    expect(r.stdout).toContain('[SEEDED station-on-plot]'); expect(r.stdout).toContain('r002 ACCEPTED');
    const dir = join(runs, 'harbour-district', 'r002'), run = await Bun.file(join(dir, 'run.json')).json();
    expect(run.seeded).toBe('station-on-plot'); expect(run.rounds[0].seeded).toBe(true); expect(run.rounds[1].seeded).toBe(false);
    expect(run.rounds[0].failedChecks).toContain('N-P2');
    expect((await Bun.file(join(dir, 'round-1', 'vantage.call.json')).json()).seeded).toBe('station-on-plot');
    expect((await withRuns(() => replay('harbour-district', 'r002'))).ok).toBe(true);
  });

  test('tampering one byte of a recorded plan, or breaking a schema, fails replay', async () => {
    const copy = join(scratch, 'tampered'); await cp(runs, copy, { recursive: true });
    const file = join(copy, 'harbour-district', 'r001', 'round-2', 'plan.json'), bytes = Buffer.from(await Bun.file(file).arrayBuffer());
    const at = bytes.indexOf('"x"') + bytes.subarray(bytes.indexOf('"x"')).indexOf('1'); bytes[at] = bytes[at] === 0x32 ? 0x33 : 0x32;
    await Bun.write(file, bytes);
    process.env.LEVELS_RUNS = copy;
    try {
      const a = await replay('harbour-district', 'r001'); expect(a.ok).toBe(false); expect(a.lines.join('\n')).toContain('plan.json is not the input plan');
      const v = join(copy, 'harbour-district', 'r002', 'round-2', 'vantage.json'), j = await Bun.file(v).json(); delete j.stops; await Bun.write(v, JSON.stringify(j));
      const b = await replay('harbour-district', 'r002'); expect(b.ok).toBe(false); expect(b.lines.join('\n')).toContain('fails its schema');
    } finally { delete process.env.LEVELS_RUNS; }
  });
});

describe('levels, hardening', () => {
  test('replay uses the recorded raw input plan: editing the plan after the run does not break it, a changed checker does', async () => {
    const plans = join(scratch, 'plans'), rr = join(scratch, 'runs-raw'), file = join(plans, 'harbour-district.plan.json');
    await mkdir(plans, { recursive: true }); await Bun.write(file, JSON.stringify(base, null, 1));
    const r = await levelsWith({ SUBORNED_PLANS: plans, LEVELS_RUNS: rr }, file);
    expect(r.stdout).toContain('r001 ACCEPTED');
    const dir = join(rr, 'harbour-district', 'r001'), recorded = await Bun.file(join(dir, 'run.json')).json();
    expect(await Bun.file(join(dir, 'input.plan.raw.json')).text()).toBe(await Bun.file(file).text());
    const edited = { ...base, status: 'promoted', version: '0.2.0', date: '2099-01-01' }; await Bun.write(file, JSON.stringify(edited));
    process.env.SUBORNED_PLANS = plans; process.env.LEVELS_RUNS = rr;
    try {
      expect(hash(JSON.stringify(await loadCorpus()))).not.toBe(recorded.corpusHash); // the edit does change the live corpus
      const res = await replay('harbour-district', 'r001'); expect(res.lines.filter(l => l.startsWith('FAIL'))).toEqual([]); expect(res.ok).toBe(true);
      const copy = join(scratch, 'runs-hash'); await cp(rr, copy, { recursive: true });
      const f = join(copy, 'harbour-district', 'r001', 'run.json'), j = await Bun.file(f).json(); j.toolsHash = 'f'.repeat(64); await Bun.write(f, JSON.stringify(j));
      process.env.LEVELS_RUNS = copy;
      const bad = await replay('harbour-district', 'r001'); expect(bad.ok).toBe(false); expect(bad.lines.join('\n')).toContain('CHECKER CODE changed');
      expect((await replay('harbour-district', 'r001', true)).ok).toBe(true);
    } finally { delete process.env.SUBORNED_PLANS; delete process.env.LEVELS_RUNS; }
  });

  test('an impossible fillet is a Datum problem: the line is not applied, the checker and Pulse are skipped that round, and the run goes on', async () => {
    const fx = join(scratch, 'fixtures'), rr = join(scratch, 'runs-fillet'); await cp(join(here, 'fixtures'), fx, { recursive: true });
    const good = await Bun.file(join(fx, 'transit-lines.1.json')).json(); await Bun.write(join(fx, 'transit-lines.2.json'), JSON.stringify(good));
    const bad = structuredClone(good); bad.lines[0].paths.push({ id: 'kink', role: bad.lines[0].mode === 'tram' ? 'running' : 'line', double: false, pts: [[0, 0], [10, 0, 50], [10, 10]] });
    await Bun.write(join(fx, 'transit-lines.1.json'), JSON.stringify(bad));
    const r = await levelsWith({ STUB_FIXTURES: fx, LEVELS_RUNS: rr }, planFile);
    expect(r.stdout).toContain('SKIPPED'); expect(r.stdout).toContain('r001 ACCEPTED'); expect(r.code).toBe(0);
    const dir = join(rr, 'harbour-district', 'r001'), run = await Bun.file(join(dir, 'run.json')).json();
    expect(run.rounds[0]).toMatchObject({ skipped: ['pulse', 'checker'], checkerOk: false, verdict: 'fail' }); expect(run.rounds[0].problems[0]).toMatch(/^line .* cannot be built/); expect(run.rounds[0].faultOwners).toContain('Datum');
    expect(await Bun.file(join(dir, 'round-1', 'plan.json')).exists()).toBe(false); expect(run.rounds[1].skipped).toEqual([]);
    process.env.LEVELS_RUNS = rr;
    try { const res = await replay('harbour-district', 'r001'); expect(res.lines.filter(l => l.startsWith('FAIL'))).toEqual([]); } finally { delete process.env.LEVELS_RUNS; }
  });

  test('--rounds is clamped to 1..3 and NaN means 3', async () => {
    const rr = join(scratch, 'runs-rounds'), one = await levelsWith({ LEVELS_RUNS: rr }, planFile, '--rounds', '0');
    expect(one.stdout).toContain('r001 REJECTED'); expect((await Bun.file(join(rr, 'harbour-district', 'r001', 'run.json')).json()).rounds).toHaveLength(1);
    const nan = await levelsWith({ LEVELS_RUNS: rr }, planFile, '--rounds', 'abc'); expect(nan.stdout).toContain('r002 ACCEPTED');
  });
});

describe('levels units', () => {
  test('versions: next minor, round and run in the pre-release', () => {
    expect(nextMinor('0.1.0')).toBe('0.2.0'); expect(candidateVersion('0.1.0', 3, 2)).toBe('0.2.0-r003.2');
  });
  test('checker faults route to the owner who can fix them', () => {
    expect(ownerOf('N-R1')).toBe('Vantage'); expect(ownerOf('S-S2')).toBe('Vantage'); expect(ownerOf('N-D1')).toBe('Datum'); expect(ownerOf('T-R1')).toBe('Datum');
  });
  test('apply: unknown references are problems, not silent fixes; the waivers never reach a candidate', () => {
    const stops = { stops: [{ line: 'T9', id: 'x', name: 'X', path: 'p', at: [0, 0] as [number, number], fronts: 'nope' }], notes: [], sources: [] };
    const r = apply(base, { vantage: stops }, '0.2.0-r001.1');
    expect(r.problems).toEqual(['stop T9/x: no such line']); expect(r.plan.standards.transit.waived).toBeUndefined(); expect(r.plan.version).toBe('0.2.0-r001.1');
  });
  test('apply: an impossible fillet never reaches derive; the line is left as it was', () => {
    const lines = { lines: [{ id: 'S1', mode: 'skyrail' as const, name: 'Sky', beyond: '', paths: [{ id: 'trunk', role: 'line' as const, double: true, pts: [[0, 0], [10, 0, 50], [10, 10]] }], order: [] }], remove: [], notes: [], sources: [] };
    const r = apply(base, { datum: lines }, '0.2.0-r001.1');
    expect(r.problems).toHaveLength(1); expect(r.problems[0]).toMatch(/^line S1 cannot be built/); expect(r.plan.skyrail.find((l: any) => l.id === 'S1').paths).toEqual(base.skyrail.find((l: any) => l.id === 'S1').paths);
  });
  test('seed with no eligible station is an error, never a moved tram stop', () => {
    const v = { stops: [{ line: 'T1', id: 'a', name: 'A', path: 'p', at: [0, 0] as [number, number], fronts: 'x' }], notes: [], sources: [] };
    expect(() => seedStationOnPlot(v, base, [{ id: 'p', centre: [5, 5] }])).toThrow('no sky rail station');
  });
  test('seed station-on-plot moves a sky rail station onto the nearest plot centre', () => {
    const v = { stops: [{ line: 'S1', id: 'a', name: 'A', path: 'trunk', at: [0, 0] as [number, number], fronts: 'circus-road' }], notes: [], sources: [] };
    const s = seedStationOnPlot(v, base, [{ id: 'far', centre: [500, 500] }, { id: 'near', centre: [10, 5] }]);
    expect(s.out.stops[0]!.at).toEqual([10, 5]); expect(v.stops[0]!.at).toEqual([0, 0]); expect(s.note).toContain('near');
  });
});

describe('CLI schema acceptance (model-free)', () => {
  for (const [a, sc] of Object.entries(agents)) test(`${a} schema has no keyword the CLI rejects`, () => expect(schemaProblems(JSON.parse(cliSchema(sc)))).toEqual([]));
  test('the lint catches tuples and dangling refs', () => {
    expect(schemaProblems(JSON.parse(cliSchema(z.object({ at: z.tuple([z.number(), z.number()]) }))))[0]).toContain('prefixItems');
    expect(schemaProblems({ $ref: '#/$defs/x' })).toHaveLength(1);
  });
});
