// The skyline objective with no model and no index: a stub CLAUDE_BIN answers from fixtures, retrieval is lexical, the
// plan and the kit are the frozen copies in fixtures/, and every run lands in a scratch folder, never in runs/.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { cp, mkdir, mkdtemp, readdir } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { runPreview } from '../../../gen/preview-run';
import { cliSchema } from '../../../rag/claude';
import { root } from '../../../rag/config';
import { imagePath, sha256 } from '../../../rag/images';
import { replayRun } from '../../../gen/replay';
import { plugins } from '../../plugins';
import { replay } from '../../replay';
import { lexical } from '../../test/lexical';
import { schemaProblems } from '../../test/schema-lint';
import { parseConcepts } from '../concepts';
import { skylineFiles, skylineHash } from '../hash';
import { skyline as plugin } from '../plugin';
import { agents } from '../schemas';
import { KIT, PASS, PLAN, datum, loadPlan, site, stations, tidy, vantage } from './edits';

setDefaultTimeout(300000);
type Round = {
  checkerOk: boolean; failedChecks: string[]; assessFaults: number; criticRan: boolean; keelVerdict: string | null;
  faultOwners: string[]; problems: string[]; ran: string[];
};
type Run = {
  outcome: string; cost: { calls: number }; rounds: Round[]; calls: { tokens?: { input?: number } }[];
  continues?: Record<string, unknown>;
} & Record<string, unknown>;
type Fault = { owner: string; rule: string; fault: string; synthesized: boolean };
type Candidate = {
  megastructures: { id: string; portals: { conceptTiles: string[] }[] }[]; skyrail: { id: string; stations: unknown }[];
};

const stub = resolve(root, 'tools/levels/test/claude-stub.ts');
const conceptsFile = resolve(root, 'contracts/world/skyline-concepts.json');
const scratch = await mkdtemp(join(tmpdir(), 'skyline-')), runs = join(scratch, 'runs'), out = join(scratch, 'out');
const state = join(scratch, 'state'), planFile = join(scratch, 'plan.json'), plan = await loadPlan();
await cp(PLAN, planFile);
const ledgers = join(scratch, 'ledger'); // the drift ledger of these runs: never the committed one
const env = {
  ...process.env, LEVELS_RUNS: runs, LEVELS_OUT: out, LEVELS_TEST_RETRIEVER: 'lexical',
  CLAUDE_BIN: stub, SUBORNED_KIT: KIT, LEVELS_LEDGER: ledgers,
};
await mkdir(ledgers, { recursive: true });

/** Writes a fixture set: `<objective>.<n>.json` per call, read by the stub in order. */
async function fixtureSet(name: string, files: Record<string, unknown>) {
  const dir = join(scratch, 'fx', name);
  await mkdir(dir, { recursive: true });
  for (const [f, v] of Object.entries(files)) await Bun.write(join(dir, f), JSON.stringify(v));
  return dir;
}

async function skyline(fixtures: string, ...args: string[]) {
  await Bun.$`rm -rf ${state} && mkdir -p ${state}`;
  const p = Bun.spawn(['bun', 'tools/levels/cli.ts', 'skyline', '--plan', planFile, ...args], {
    cwd: root, env: { ...env, STUB_FIXTURES: fixtures, STUB_STATE: state }, stdout: 'pipe', stderr: 'pipe',
  });
  const text = (s: ReadableStream) => new Response(s).text();
  const [stdout, stderr, code] = await Promise.all([text(p.stdout), text(p.stderr), p.exited]);
  return { stdout, stderr, code };
}

/** Replay in this process, from a runs folder, with the env the runs were made under. */
async function replayIn(dir: string, run: string, again: typeof replay = replay) {
  const old = {
    LEVELS_RUNS: process.env.LEVELS_RUNS, SUBORNED_KIT: process.env.SUBORNED_KIT,
    LEVELS_LEDGER: process.env.LEVELS_LEDGER,
  };
  Object.assign(process.env, { LEVELS_RUNS: dir, SUBORNED_KIT: KIT, LEVELS_LEDGER: ledgers });
  try { return await again('harbour-district', run); } finally {
    for (const [k, v] of Object.entries(old)) { if (v === undefined) delete process.env[k]; else process.env[k] = v; }
  }
}

async function edit<T>(file: string, f: (json: T) => void) {
  const json: T = await Bun.file(file).json();
  f(json);
  await Bun.write(file, JSON.stringify(json));
}
const fails = (r: { lines: string[] }) => r.lines.filter(l => l.startsWith('FAIL'));
const where = (run: string, f: string) => join(runs, 'harbour-district', run, f);
const read = async <T>(run: string, f: string): Promise<T> => Bun.file(where(run, f)).json();
const exists = (run: string, f: string) => Bun.file(where(run, f)).exists();
const mentions = (text: string, wants: string[]) => { for (const w of wants) expect(text).toContain(w); };

describe('levels skyline, stubbed', () => {
  test("the fixtures satisfy the output schemas, and the schemas the CLI's strict validator", () => {
    const real = (x: unknown) => JSON.parse(JSON.stringify(x).replaceAll('__CHUNK__', 'a'.repeat(24)));
    expect(agents.datum.safeParse(real(tidy(plan))).error?.message).toBeUndefined();
    expect(agents.vantage.safeParse(real(vantage({ stops: stations(plan) }))).error?.message).toBeUndefined();
    expect(agents.keel.safeParse(real(PASS)).error?.message).toBeUndefined();
    for (const a of Object.values(agents)) expect(schemaProblems(cliSchema(a))).toEqual([]);
  });

  const accept = {
    'skyline-datum.1.json': tidy(plan), 'skyline-vantage.1.json': vantage({ stops: stations(plan) }),
    'skyline-critic.1.json': PASS,
  };
  test('ACCEPTED in round 1: Keel passes; every call was shown the sheets; replay passes', async () => {
    const r = await skyline(await fixtureSet('accept', accept));
    expect(r.stdout).toContain('r001 ACCEPTED (0.3.0-r001.1)'); expect(r.code).toBe(0);
    const run = await read<Run>('r001', 'run.json'), index = parseConcepts(await Bun.file(conceptsFile).text());
    expect(run).toMatchObject({
      outcome: 'ACCEPTED', objective: 'skyline', loop: 2, proposals: 'upserts', finalVersion: '0.3.0-r001.1',
      cost: { calls: 3 }, inputConcepts: { source: 'contracts/world/skyline-concepts.json' },
      inputGrammar: { source: 'contracts/world/harbour-district.grammar.md' },
    });
    expect(run.rounds).toHaveLength(1);
    expect(run.rounds[0]).toMatchObject({
      checkerOk: true, criticRan: true, keelVerdict: 'pass', assessFaults: 0, problems: [], failedChecks: [],
    });
    expect(run.calls.every(c => c.tokens?.input !== undefined)).toBe(true);
    for (const o of ['skyline-datum', 'skyline-vantage', 'skyline-critic']) {
      const shown: { sha256: string }[] = await Bun.file(join(state, `${o}.images.json`)).json();
      expect(shown.map(i => i.sha256)).toEqual(index.sheets.map(s => s.sha256));
    }
    const pinned = [
      'input.concepts.json', 'input.grammar.md', 'input.kit.digest.json', 'input.plan.raw.json', 'input.block.json',
    ];
    for (const f of pinned) expect(await exists('r001', f), f).toBe(true);
    expect(await Bun.file(where('r001', 'input.concepts.json')).text()).toBe(await Bun.file(conceptsFile).text());
    expect(await read('r001', 'round-1/datum.call.json')).toMatchObject({
      images: [{ path: index.sheets[0]!.path }, { path: index.sheets[1]!.path }],
    });
    const cand = await read<Candidate>('r001', 'round-1/plan.json');
    const s1 = (p: Pick<Candidate, 'skyrail'>) => p.skyrail.find(l => l.id === 'S1')!;
    expect(s1(cand).stations).toEqual(s1(plan as unknown as Candidate).stations);
    const gate = cand.megastructures.find(m => m.id === 'parade-galleries')!.portals[0]!;
    expect(gate.conceptTiles).toEqual(['blocks/rail_through']);
    const res = await replayIn(runs, 'r001');
    expect(fails(res)).toEqual([]); expect(res.ok).toBe(true);
    mentions(res.lines.join('\n'), [
      'concept index', 'grammar ', 'kit digest', 'plan.json = input plan + the recorded proposals',
      'apply faults match (0)', 'shown the 2 sheets of the index', 'checker rerun matches checks.json',
    ]);
  });

  const reject = {
    'skyline-datum.1.json': datum({
      towerSites: [site(plan, 'crescent-terrace', { concepts: ['blocks/nonexistent_tile'] })],
    }),
    'skyline-datum.2.json': datum({ towerSites: [site(plan, 'avenue-needle', { storeys: 80 })] }),
    'skyline-vantage.1.json': vantage(),
  };
  test('REJECTED: a bad tile, then SK-1; each goes back to Datum, Keel is not called; replay passes', async () => {
    const r = await skyline(await fixtureSet('reject', reject), '--rounds', '2');
    expect(r.stdout).toContain('r002 REJECTED (0.3.0-r002.2)'); expect(r.code).toBe(1);
    expect(r.stdout.match(/Keel not called \(the checks failed\)/g)).toHaveLength(2);
    const run = await read<Run>('r002', 'run.json');
    expect(run.outcome).toBe('REJECTED'); expect(run.cost.calls).toBe(4); // datum and vantage, twice; Keel never
    const row = (x: Round) => [x.checkerOk, x.failedChecks, x.assessFaults, x.criticRan, x.keelVerdict, x.faultOwners];
    expect(run.rounds.map(row)).toEqual([
      [false, [], 1, false, null, ['Datum']], [false, ['SK-1'], 0, false, null, ['Datum']],
    ]);
    expect(run.rounds[0]!.problems).toEqual([
      'tower site crescent-terrace: concept tile "blocks/nonexistent_tile" is not in the concept index',
    ]);
    expect(run.rounds[1]!.ran).toEqual(['datum', 'vantage']);
    for (const n of [1, 2]) expect(await exists('r002', `round-${n}/keel.json`)).toBe(false);
    const faults = await read<Fault[]>('r002', 'round-2/faults.json');
    expect(faults).toMatchObject([{ owner: 'Datum', rule: 'SK-1', synthesized: true }]);
    expect(faults[0]!.fault).toContain('avenue-needle');
    const res = await replayIn(runs, 'r002');
    expect(fails(res)).toEqual([]); expect(res.ok).toBe(true);
    mentions(res.lines.join('\n'), [
      'apply faults match (1)', 'not called, the candidate failed the checks', 'outcome REJECTED (recorded)',
    ]);
  });

  test("--continue: starts from the rejected run's last candidate and open fault, and replays alone", async () => {
    const fix = datum({ towerSites: [site(plan, 'avenue-needle')] });
    const fixtures = await fixtureSet('continue', { ...accept, 'skyline-datum.1.json': fix });
    const r = await skyline(fixtures, '--continue', 'r002');
    expect(r.stdout)
      .toContain('continuing harbour-district/r002 (round 2, 1 open fault(s)); round 1 reruns from datum');
    expect(r.stdout).toContain('r003 ACCEPTED');
    const run = await read<Run>('r003', 'run.json');
    expect(run.continues).toMatchObject({
      run: 'r002', outcome: 'REJECTED', round: 2, version: '0.3.0-r002.2', sameInputs: true,
    });
    expect(run.rounds).toHaveLength(1);
    expect(run.rounds[0]).toMatchObject({ ran: ['datum', 'vantage'], checkerOk: true, keelVerdict: 'pass' });
    const alone = join(scratch, 'runs-alone');
    await cp(where('r003', ''), join(alone, 'harbour-district', 'r003'), { recursive: true });
    const res = await replayIn(alone, 'r003');
    expect(fails(res)).toEqual([]); expect(res.ok).toBe(true);
  });

  test('replay recomputes: a changed proposal, check result, concept index, grammar or call fails it', async () => {
    const tamper = async (name: string, change: (run: string) => Promise<unknown>) => {
      const copy = join(scratch, name);
      await cp(runs, copy, { recursive: true });
      await change(join(copy, 'harbour-district', 'r001'));
      return fails(await replayIn(copy, 'r001')).join('\n');
    };
    const at = (d: string, f: string) => join(d, f);
    type Checks = { checks: { id: string; value: string }[] };
    expect(await tamper('t-plan', d => edit<{ signatures: { height: number }[] }>(at(d, 'round-1/plan.json'), j => {
      j.signatures[0]!.height = 1;
    }))).toContain('plan.json is not the input plan plus the recorded proposals');
    expect(await tamper('t-checks', d => edit<Checks>(at(d, 'round-1/checks.json'), j => {
      j.checks.find(c => c.id === 'SK-1')!.value = '0/9';
    }))).toContain('checker rerun differs from checks.json');
    expect(await tamper('t-concepts', async d => {
      await Bun.write(at(d, 'input.concepts.json'), (await Bun.file(at(d, 'input.concepts.json')).text()) + ' ');
    })).toContain('input.concepts.json does not match the recorded sha256');
    expect(await tamper('t-grammar', d => Bun.write(at(d, 'input.grammar.md'), 'other')))
      .toContain('input.grammar.md does not match the recorded sha256');
    expect(await tamper('t-problem', d => edit<Run>(at(d, 'run.json'), j => { j.rounds[0]!.assessFaults = 3; })))
      .toContain('apply faults recorded 3, recomputed 0');
    const sheet = (d: string) => edit<{ images: { sha256: string }[] }>(at(d, 'round-1/datum.call.json'), j => {
      j.images[0]!.sha256 = '1'.repeat(64);
    });
    expect(await tamper('t-sheet', sheet))
      .toContain('round-1/datum: the call was not shown the concept sheets the index names');
  });

  test('the generator moves: a comment-only edit replays with the drift labelled; a changed result needs a cause', async () => {
    const code = join(scratch, 'code'), world = join(code, 'tools/world/export.ts');
    for (const f of skylineFiles()) await Bun.write(join(code, f), Bun.file(join(root, f)));
    await Bun.write(world, `${await Bun.file(world).text()}\n// a comment, nothing else\n`);
    const later = { ...plugin, code: { ...plugin.code, hash: () => skylineHash(code) } };
    const again = (planId: string, runId: string) => replayRun({ ...plugins, skyline: later }, planId, runId);
    const ledger = join(ledgers, 'skyline.json'), now = await skylineHash(code);
    // the run wrote its own code state into the ledger when it ended
    const made = (await read<{ toolsHash: string }>('r001', 'run.json')).toolsHash;
    expect(Object.keys((await Bun.file(ledger).json()).recorded)).toContain(made.slice(0, 16));
    const res = await replayIn(runs, 'r001', again);
    expect(fails(res)).toEqual([]); expect(res.ok).toBe(true);
    const line = res.lines.find(l => l.includes('CHECKER CODE changed'))!;
    expect(line).toStartWith('warn'); expect(line).toContain('every rule the run recorded reruns identically');
    // a result that differs: no cause declared for this code, so a failure; declared, so labelled by its cause
    const copy = join(scratch, 'runs-moved');
    await cp(runs, copy, { recursive: true });
    const checks = join(copy, 'harbour-district', 'r001', 'round-1/checks.json');
    await edit<{ checks: { id: string; value: string }[] }>(checks, j => {
      j.checks.find(c => c.id === 'SK-1')!.value = '0/9';
    });
    const bare = await replayIn(copy, 'r001', again);
    expect(fails(bare).join('\n')).toContain('not declared');
    const { declareCause } = await import('../../drift');
    declareCause(ledger, now, 'test: the generator moved the SK-1 count');
    const declared = await replayIn(copy, 'r001', again);
    expect(fails(declared)).toEqual([]);
    expect(declared.lines.join('\n')).toContain('the generator moved the SK-1 count');
    // the same difference under the code the run was made with has no cause
    expect(fails(await replayIn(copy, 'r001')).join('\n')).toContain('checker rerun differs from checks.json');
  });

  test('prompt preview: round 1 with no model; writes the prompts with the pinned images and an estimate', async () => {
    const dir = join(scratch, 'preview');
    const opts = { input: planFile, retriever: lexical, retrieval: 'lexical (test only)' };
    const table = await runPreview(plugin, opts, dir);
    mentions(table, ['prompts written under', 'no model was called']);
    expect(table).toMatch(/round 1: about \$\d+\.\d\d/);
    const calls = join(dir, 'out', 'harbour-district', 'r001', 'calls');
    expect((await readdir(calls)).filter(f => f.endsWith('.prompt.txt')).sort())
      .toEqual(['01-skyline-datum.prompt.txt', '02-skyline-vantage.prompt.txt', '03-skyline-critic.prompt.txt']);
    const index = parseConcepts(await Bun.file(conceptsFile).text());
    const prompt = await Bun.file(join(calls, '01-skyline-datum.prompt.txt')).text();
    for (const s of index.sheets) {
      expect(prompt).toContain(`${s.path.split('/').slice(-2).join('/')} (sha256 ${s.sha256.slice(0, 12)})`);
    }
    mentions(prompt, [
      'Task: propose the skyline.', 'blocks/rail_through', 'atomic/power_spire', 'Massing grammar, sections 4, 6 and 8',
      '## 6. Roofs', 'Sky rail now (JSON)',
    ]);
    expect(await Bun.file(join(calls, '03-skyline-critic.prompt.txt')).text())
      .toContain('Plan checker report on the candidate (strict, plan and world rules)');
    expect(await Bun.file(join(dir, 'out', 'harbour-district', 'r001', 'preview.txt')).exists()).toBe(true);
    expect(sha256(await Bun.file(imagePath(index.sheets[0]!.path)).bytes())).toBe(index.sheets[0]!.sha256);
  });
});
