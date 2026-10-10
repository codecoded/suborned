// The whole pipeline with no model and no index: a stub CLAUDE_BIN answers from fixtures, retrieval is lexical, and every
// run lands in a scratch folder (LEVELS_RUNS / LEVELS_OUT), never in runs/.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { cp, mkdir, mkdtemp, readdir } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { createHash } from 'node:crypto';
import { join, resolve } from 'node:path';
import { root } from '../../rag/config';
import { hash, loadCorpus } from '../../rag/corpus';
import { apply, seedStationOnPlot } from '../apply';
import { citedBlock, citeFaults } from '../cite';
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

  test('gdd is recorded from the loaded files; a different GDD version is labelled drift, the same version with a changed card still FAILs', async () => {
    const src = join(runs, 'harbour-district', 'r001'), run = await Bun.file(join(src, 'run.json')).json(), cur = (await readdir(resolve(root, 'GDD'))).filter(f => f.includes('agent-gdd') && f.endsWith('.md')).sort().at(-1)!;
    expect(run.gdd).toEqual({ version: cur.match(/v\d+\.\d+\.\d+/)![0], agent: `GDD/${cur}`, human: expect.stringMatching(/^GDD\/.*\.html$/) });
    const mk = async (name: string, edit: (r: any) => void) => {
      const copy = join(scratch, name); await cp(runs, copy, { recursive: true });
      const d = join(copy, 'harbour-district', 'r001'), f = join(d, 'run.json'), j = await Bun.file(f).json(); edit(j); await Bun.write(f, JSON.stringify(j));
      const cf = join(d, 'round-1', 'datum.call.json'), c = await Bun.file(cf).json(); c.card.sourceHash = '0'.repeat(64); await Bun.write(cf, JSON.stringify(c));
      process.env.LEVELS_RUNS = copy; try { return await replay('harbour-district', 'r001'); } finally { delete process.env.LEVELS_RUNS; }
    };
    const same = await mk('runs-same', () => {}); expect(same.ok).toBe(false); expect(same.lines.join('\n')).toContain('FAIL  round-1/datum: card');
    const older = await mk('runs-older', j => { j.gdd.version = 'v0.0.1'; }); expect(older.lines.filter(l => l.startsWith('FAIL'))).toEqual([]); expect(older.ok).toBe(true);
    expect(older.lines.join('\n')).toContain('round-1/datum: card .claude/agents/world-planner.md changed since the run (recorded against GDD v0.0.1)');
    const inferred = await mk('runs-inferred', j => { delete j.gdd; }); expect(inferred.ok).toBe(false); // no record, hits name the current GDD: same version, so strict
    expect(inferred.lines.join('\n')).toContain('(inferred from retrieved sources)');
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

  test('the civic block is pinned per run; replay uses the pinned bytes even if the block on disk changes', async () => {
    const rr = join(scratch, 'runs-pin'), blk = join(scratch, 'block.json'), pf = join(scratch, 'plan-pin.json'), real = resolve(root, base.civic.block);
    await Bun.write(blk, Bun.file(real)); await Bun.write(pf, JSON.stringify({ ...base, civic: { ...base.civic, block: blk } }));
    const r = await levelsWith({ LEVELS_RUNS: rr }, pf); expect(r.code).toBe(0);
    const dir = join(rr, 'harbour-district', 'r001'), run = await Bun.file(join(dir, 'run.json')).json(), bytes = Buffer.from(await Bun.file(join(dir, 'input.block.json')).arrayBuffer());
    expect(bytes.equals(Buffer.from(await Bun.file(real).arrayBuffer()))).toBe(true);
    expect(run.inputBlock).toEqual({ source: blk, sha256: createHash('sha256').update(bytes).digest('hex') });
    const rep = JSON.parse(await Bun.file(join(dir, 'round-2', 'checks.json')).text()); expect(rep.ok).toBe(true);
    await Bun.write(blk, '{"schema":"suborned.block/2","stale":true}'); // the file on disk now differs (and would crash the checker)
    process.env.LEVELS_RUNS = rr;
    try {
      const ok = await replay('harbour-district', 'r001'); expect(ok.lines.filter(l => l.startsWith('FAIL'))).toEqual([]); expect(ok.ok).toBe(true);
      expect(ok.lines.join('\n')).toContain('checker rerun matches checks.json');
      await Bun.write(join(dir, 'input.block.json'), bytes.toString('utf8') + ' '); // tampered pin
      const bad = await replay('harbour-district', 'r001'); expect(bad.ok).toBe(false); expect(bad.lines.join('\n')).toContain('input.block.json does not match');
    } finally { delete process.env.LEVELS_RUNS; }
  });

  test('a run with no pinned block is superseded drift: fails, or warns with --allow-drift; no throw, no checker rerun', async () => {
    const copy = join(scratch, 'runs-old'); await cp(runs, copy, { recursive: true });
    const dir = join(copy, 'harbour-district', 'r001'), f = join(dir, 'run.json'), j = await Bun.file(f).json(); delete j.inputBlock; await Bun.write(f, JSON.stringify(j));
    await Bun.$`rm -f ${join(dir, 'input.block.json')}`; // r001 of the shared fixture runs predates pinning
    const line = 'superseded: recorded against the Oct 5 block, replays at commit 3dbb9ca';
    process.env.LEVELS_RUNS = copy;
    try {
      const a = await replay('harbour-district', 'r001'); expect(a.ok).toBe(false); expect(a.lines).toContain('FAIL  ' + line); expect(a.lines.join('\n')).not.toContain('checker rerun');
      const b = await replay('harbour-district', 'r001', true); expect(b.lines.filter(l => l.startsWith('FAIL'))).toEqual([]); expect(b.ok).toBe(true); expect(b.lines).toContain('warn  ' + line);
    } finally { delete process.env.LEVELS_RUNS; }
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

  test("Keel's named owner wins: a fault {rule:'N-R1', owner:'Datum'} routes to Datum, who reruns next round", async () => {
    const fx = join(scratch, 'fixtures-owner'), rr = join(scratch, 'runs-owner'); await cp(join(here, 'fixtures'), fx, { recursive: true });
    const k = await Bun.file(join(fx, 'transit-critic.1.json')).json(); k.faults.push({ owner: 'Datum', rule: 'N-R1', fault: 'No stop can fix this reach gap.', fix: 'Extend a line.', chunks: ['__CHUNK__'] });
    await Bun.write(join(fx, 'transit-critic.1.json'), JSON.stringify(k));
    const r = await levelsWith({ STUB_FIXTURES: fx, LEVELS_RUNS: rr }, planFile); expect(r.code).toBe(0);
    const run = await Bun.file(join(rr, 'harbour-district', 'r001', 'run.json')).json();
    expect(run.rounds[0].faultOwners).toContain('Datum'); expect(run.rounds[0].faultOwners).toContain('Vantage');
    const f = await Bun.file(join(rr, 'harbour-district', 'r001', 'round-1', 'faults.json')).json();
    expect(f.filter((x: any) => x.rule === 'N-R1').map((x: any) => x.owner)).toEqual(['Datum']);
    expect(run.rounds[1].ran).toContain('datum');
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
  test('ownerOf only names the owner of checker failures Keel did not name', () => {
    expect(ownerOf('S-S2')).toBe('Vantage'); expect(ownerOf('N-D1')).toBe('Datum'); expect(ownerOf('T-R1')).toBe('Datum');
  });
  test('cite: an id outside the builder\'s own hits is a synthesized fault for that owner; one inside its own hits but outside Keel\'s is not', () => {
    const [A, B, C] = ['a', 'b', 'c'].map(x => x.repeat(24)), hit = (key: string) => ({ key, section: '§09', text: `text ${key}` });
    const built = { datum: { out: { sources: [A] }, call: { retrieval: { hits: [hit(A)] } } }, vantage: { out: { sources: [A, B] }, call: { retrieval: { hits: [hit(B)] } } }, pulse: { out: { sources: [C] }, call: { retrieval: { hits: [hit(C)] } } } };
    const f = citeFaults(built); // keel's own top-8 is irrelevant: only each builder's own hits count
    expect(f).toHaveLength(1); expect(f[0]).toMatchObject({ owner: 'Vantage', rule: 'cite', synthesized: true }); expect(f[0]!.fault).toContain(A); expect(f[0]!.fault).not.toContain(B);
    const block = citedBlock(built); expect(block).toContain(`Pulse retrieved: ${C} (§09)`); expect(block).toContain(`[${C}] (§09, cited by Pulse) text ${C}`);
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
