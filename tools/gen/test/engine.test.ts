// The engine with an objective that is not transit: two builders with their own owners, one deterministic check, no plan checker. Shows the loop,
// fault routing, reuse and replay depend on plugin.ts alone. Offline: stub claude, lexical retriever, scratch folders; no model, no index.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { cp, mkdir, mkdtemp, readdir } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { z } from 'zod';
import { root } from '../../rag/config';
import { generate } from '../loop';
import { replayRun } from '../replay';
import type { Plugin } from '../plugin';
import { lexical } from '../../levels/test/lexical';

setDefaultTimeout(180000);
const scratch = await mkdtemp(join(tmpdir(), 'gen-')), brief = join(scratch, 'brief.md'), stub = join(import.meta.dir, '../../levels/test/claude-stub.ts');
await Bun.write(brief, 'Toy objective: a proposes a number, b proposes the next one.');
const chunk = z.string().regex(/^[0-9a-f]{24}$/), sources = z.array(chunk), Num = z.object({ n: z.number(), sources });
const Verdict = z.object({ verdict: z.enum(['pass', 'fail']), summary: z.string(), faults: z.array(z.object({ owner: z.enum(['Alpha', 'Beta']), rule: z.string(), fault: z.string(), fix: z.string(), chunks: z.array(chunk).min(1) })), sources });
const schemas: Record<string, z.ZodType<any>> = { a: Num, b: Num, keel: Verdict };
const objective = (id: string, role: string) => ({ id, role, brief, query: ['toy number'] });

const toy: Plugin<null> = {
  id: 'toy', defaultInput: 'toy',
  builders: [{ key: 'a', owner: 'Alpha', objective: objective('toy-a', 'world-planner'), schema: Num }, { key: 'b', owner: 'Beta', objective: objective('toy-b', 'level-designer'), schema: Num }],
  critic: { key: 'keel', owner: 'Keel', objective: objective('toy-critic', 'simulation-engineer'), schema: Verdict },
  header: {}, code: { label: 'toy', hash: async () => 'toy-code', ledger: () => ({ recorded: {}, declared: {} }), ledgerFile: 'none', declare: 'none' },
  open: async () => ({ id: 'toy', version: '1.0.0', pin: async () => ({ state: null, record: { pinned: 'nothing' } }) }),
  task: async (_s, r, key) => `Task: ${key}${key === 'b' ? ` after ${r.outs.a.n}` : ''}`,
  async assess(_s, r) {
    const ok = r.outs.b.n === r.outs.a.n + 1;
    return { ok, line: ok ? 'toy clean' : 'toy FAIL TOY-1', record: { toyOk: ok }, failures: ok ? [] : [{ rule: 'TOY-1', owners: ['Beta'], fault: 'b is not a + 1', fix: 'b = a + 1' }], faults: [], criticTask: cited => `Task: judge.\n${cited}` };
  },
  replay: {
    skipNote: 'nothing', inputVersion: () => '1.0.0', superseded: () => false, checksOk: r => r.toyOk, schema: (_run, key) => schemas[key]!,
    pins: { raw: async () => ({}), input: async () => ({}), block: async () => ({}) },
    async candidate(c, r) {
      const a = await c.read(`round-${r.round}/a.json`), b = await c.read(`round-${r.round}/b.json`), ok = b.n === a.n + 1;
      ok === r.toyOk ? c.ok(`round-${r.round}: toy check matches (${ok})`) : c.fail(`round-${r.round}: toy check differs from the record`);
      return { differs: ok !== r.toyOk, added: [] };
    },
  },
};

const pass = { verdict: 'pass', summary: '', faults: [], sources: [] };
type RunOpts = { rounds?: number; loop?: number; continue?: string; dir?: string; env?: Record<string, string> };
/** Runs the toy objective with the stub model answering from `fixtures` (file → JSON); returns the run folder. `dir`: the folder of an earlier run, so that a run can continue it. */
async function run(name: string, fixtures: Record<string, unknown>, o: RunOpts = {}) {
  const dir = o.dir ?? join(scratch, name), fx = join(dir, `fixtures-${name}`), state = join(dir, `state-${name}`); await mkdir(fx, { recursive: true }); await mkdir(state, { recursive: true });
  for (const [f, v] of Object.entries(fixtures)) await Bun.write(join(fx, f), JSON.stringify(v));
  const env = { LEVELS_RUNS: join(dir, 'runs'), LEVELS_OUT: join(dir, 'out'), CLAUDE_BIN: stub, STUB_FIXTURES: fx, STUB_STATE: state, ...o.env }, old = Object.fromEntries(Object.keys(env).map(k => [k, process.env[k]]));
  Object.assign(process.env, env);
  try {
    const r = await generate(toy, { input: 'toy', rounds: o.rounds ?? 3, retriever: lexical, retrieval: 'lexical (test only)', loop: o.loop, continue: o.continue });
    return { r, dir, env, runJson: await Bun.file(join(r.rdir, 'run.json')).json(), replay: (id = 'r001') => replayRun({ toy }, 'toy', id) };
  } finally { for (const [k, v] of Object.entries(old)) v === undefined ? delete process.env[k] : (process.env[k] = v); }
}
const withRuns = async <T>(env: Record<string, string>, f: () => Promise<T>) => {
  const keys = Object.keys(env).filter(k => k === 'LEVELS_RUNS' || k === 'SUBORNED_KIT'), old = Object.fromEntries(keys.map(k => [k, process.env[k]]));
  for (const k of keys) process.env[k] = env[k]!;
  try { return await f(); } finally { for (const [k, v] of Object.entries(old)) v === undefined ? delete process.env[k] : (process.env[k] = v); }
};
const fails = (res: { lines: string[] }) => res.lines.filter(l => l.startsWith('FAIL'));
const calls = (t: { runJson: any }) => t.runJson.cost.calls as number;

describe('generation engine, a toy objective', () => {
  test('a failed deterministic check goes to its owner only; the other builder is reused; the run replays; a tampered output fails replay', async () => {
    const t = await run('check', { 'toy-a.1.json': { n: 1, sources: [] }, 'toy-b.1.json': { n: 5, sources: [] }, 'toy-b.2.json': { n: 2, sources: [] }, 'toy-critic.1.json': pass });
    expect(t.r.outcome).toBe('ACCEPTED'); expect(t.runJson).toMatchObject({ objective: 'toy', plan: 'toy', pinned: 'nothing', finalVersion: '1.1.0-r001.2', handoffMode: 'answered' });
    expect(t.runJson.rounds.map((x: any) => [x.toyOk, x.verdict, x.ran, x.reused, x.faultOwners])).toEqual([[false, 'fail', ['a', 'b'], [], ['Beta']], [true, 'pass', ['b'], ['a'], []]]);
    expect(await Bun.file(join(t.r.rdir, 'round-2', 'a.call.json')).json()).toMatchObject({ reused: true });
    const res = await withRuns(t.env, () => t.replay()); expect(res.lines.filter(l => l.startsWith('FAIL'))).toEqual([]); expect(res.ok).toBe(true); expect(res.lines).toContain('ok    round-2: toy check matches (true)');
    await Bun.write(join(t.r.rdir, 'round-2', 'b.json'), JSON.stringify({ n: 99, sources: [] }));
    const bad = await withRuns(t.env, () => t.replay()); expect(bad.ok).toBe(false); expect(bad.lines).toContain('FAIL  round-2: toy check differs from the record');
  });
  test("the critic's fault goes to the owner it names: the earliest owner with a fault reruns, and every builder after it", async () => {
    const fault = { owner: 'Alpha', rule: 'critic', fault: 'a is too small', fix: 'raise a', chunks: ['__CHUNK__'] };
    const t = await run('critic', { 'toy-a.1.json': { n: 1, sources: [] }, 'toy-a.2.json': { n: 3, sources: [] }, 'toy-b.1.json': { n: 2, sources: [] }, 'toy-b.2.json': { n: 4, sources: [] },
      'toy-critic.1.json': { verdict: 'fail', summary: 'a', faults: [fault], sources: [] }, 'toy-critic.2.json': pass });
    expect(t.r.outcome).toBe('ACCEPTED');
    expect(t.runJson.rounds.map((x: any) => [x.toyOk, x.verdict, x.keelVerdict, x.ran, x.reused, x.faultOwners])).toEqual([[true, 'fail', 'fail', ['a', 'b'], [], ['Alpha']], [true, 'pass', 'pass', ['a', 'b'], [], []]]);
    expect((await Bun.file(join(t.r.rdir, 'round-1', 'faults.json')).json())[0]).toMatchObject({ owner: 'Alpha', rule: 'critic' });
    const res = await withRuns(t.env, () => t.replay()); expect(res.lines.filter(l => l.startsWith('FAIL'))).toEqual([]); expect(res.ok).toBe(true);
  });
  test('replay names an objective it does not know instead of guessing', async () => {
    const runs = join(scratch, 'unknown'); await mkdir(join(runs, 'toy', 'r001'), { recursive: true }); await Bun.write(join(runs, 'toy', 'r001', 'run.json'), JSON.stringify({ objective: 'toy' }));
    const res = await withRuns({ LEVELS_RUNS: runs }, () => replayRun({}, 'toy', 'r001')); expect(res.ok).toBe(false); expect(res.lines[0]).toContain('is not a known objective');
  });
});

describe('loop rule 2: Keel judges only a candidate that passed the checks', () => {
  const a1 = { n: 1, sources: [] }, bogus = 'abcdef012345abcdef012345';
  test('a candidate the check failed goes straight back to its owner: no critic call, no keel.json, and the round counts toward the cap', async () => {
    const t = await run('gate-check', { 'toy-a.1.json': a1, 'toy-b.1.json': { n: 5, sources: [] }, 'toy-b.2.json': { n: 2, sources: [] }, 'toy-critic.1.json': pass });
    expect(t.runJson).toMatchObject({ loop: 2, outcome: 'ACCEPTED' }); expect(calls(t)).toBe(4); // a, b | b, critic: the old rule made 5
    expect(t.runJson.rounds.map((x: any) => [x.criticRan, x.keelVerdict, x.assessFaults, x.verdict, x.faultOwners])).toEqual([[false, null, 0, 'fail', ['Beta']], [true, 'pass', 0, 'pass', []]]);
    expect(await Bun.file(join(t.r.rdir, 'round-1', 'keel.json')).exists()).toBe(false); expect(await Bun.file(join(t.r.rdir, 'round-2', 'keel.json')).exists()).toBe(true);
    expect((await Bun.file(join(t.r.rdir, 'round-1', 'faults.json')).json()).map((f: any) => [f.owner, f.rule, f.synthesized])).toEqual([['Beta', 'TOY-1', true]]); // the checker's fault, as the owner reads it
    const res = await withRuns(t.env, () => t.replay()); expect(fails(res)).toEqual([]); expect(res.ok).toBe(true);
    expect(res.lines).toContain('ok    round-1/keel: not called (the checks failed)'); expect(res.lines).toContain('ok    round-1: Keel not called, the candidate failed the checks'); expect(res.lines).toContain('ok    round-2: Keel called on a candidate that passed the checks');
  });
  test('rounds without a critic call still use up the 3 rounds: a run whose checks never pass ends REJECTED having never called Keel', async () => {
    const t = await run('gate-cap', { 'toy-a.1.json': a1, 'toy-b.1.json': { n: 5, sources: [] }, 'toy-critic.1.json': pass });
    expect(t.r.outcome).toBe('REJECTED'); expect(t.runJson.rounds).toHaveLength(3); expect(t.runJson.rounds.map((x: any) => x.criticRan)).toEqual([false, false, false]);
    expect(calls(t)).toBe(4); // a, b, b, b
    const res = await withRuns(t.env, () => t.replay()); expect(fails(res)).toEqual([]); expect(res.lines).toContain('ok    outcome REJECTED (recorded)');
  });
  test('a code fault gates the critic as the checker does: an uncited-source fault goes to its owner first', async () => {
    const t = await run('gate-cite', { 'toy-a.1.json': { n: 1, sources: [bogus] }, 'toy-a.2.json': a1, 'toy-b.1.json': { n: 2, sources: [] }, 'toy-critic.1.json': pass });
    expect(t.runJson.rounds.map((x: any) => [x.toyOk, x.criticRan, x.citeFaults, x.faultOwners, x.ran])).toEqual([[true, false, 1, ['Alpha'], ['a', 'b']], [true, true, 0, [], ['a', 'b']]]);
    expect(t.runJson.outcome).toBe('ACCEPTED'); const res = await withRuns(t.env, () => t.replay()); expect(fails(res)).toEqual([]);
  });
  test('the old rule (loop 1) still runs and replays: Keel judges the failed candidate; a run without the field is loop 1', async () => {
    const t = await run('gate-v1', { 'toy-a.1.json': a1, 'toy-b.1.json': { n: 5, sources: [] }, 'toy-b.2.json': { n: 2, sources: [] }, 'toy-critic.1.json': pass }, { loop: 1 });
    expect(t.runJson.loop).toBe(1); expect(calls(t)).toBe(5);
    expect(t.runJson.rounds.map((x: any) => [x.keelVerdict, x.verdict, 'criticRan' in x, 'assessFaults' in x])).toEqual([['pass', 'fail', false, false], ['pass', 'pass', false, false]]);
    expect(await Bun.file(join(t.r.rdir, 'round-1', 'keel.json')).exists()).toBe(true);
    const plain = await withRuns(t.env, () => t.replay()); expect(fails(plain)).toEqual([]); expect(plain.ok).toBe(true); expect(plain.lines.join('\n')).not.toContain('loop rule');
    const j = { ...t.runJson }; delete j.loop; await Bun.write(join(t.r.rdir, 'run.json'), JSON.stringify(j));
    const cut = await withRuns(t.env, () => t.replay()); // a kit pin with the field gone is a record with a field taken out
    expect(fails(cut)).toContain('FAIL  run.json pins a kit digest but has no loop field');
    delete j.inputKit; j.chunker = 2; await Bun.write(join(t.r.rdir, 'run.json'), JSON.stringify(j)); // recorded before both fields
    const bare = await withRuns(t.env, () => t.replay()); expect(fails(bare)).toEqual([]); expect(bare.ok).toBe(true);
  });
  test('replay recomputes the gate from the record: a forged criticRan, or a keel.json the round says was never made, fails', async () => {
    const t = await run('gate-forge', { 'toy-a.1.json': a1, 'toy-b.1.json': { n: 5, sources: [] }, 'toy-b.2.json': { n: 2, sources: [] }, 'toy-critic.1.json': pass });
    const original = await Bun.file(join(t.r.rdir, 'run.json')).text();
    const edit = async (f: (j: any) => void) => { const j = JSON.parse(original); f(j); await Bun.write(join(t.r.rdir, 'run.json'), JSON.stringify(j)); return withRuns(t.env, () => t.replay()); };
    expect((await edit(j => { j.rounds[1].criticRan = false; j.rounds[1].keelVerdict = null; })).lines.join('\n')).toContain('round-2: criticRan false, keelVerdict null; the recorded checks give true');
    expect((await edit(j => { j.rounds[0].criticRan = true; })).ok).toBe(false);
    await Bun.write(join(t.r.rdir, 'run.json'), original); await Bun.write(join(t.r.rdir, 'round-1', 'keel.json'), JSON.stringify(pass));
    expect((await withRuns(t.env, () => t.replay())).lines.join('\n')).toContain('round-1/keel.json exists, but the round records that Keel was not called');
  });
});

describe('continuing a rejected run', () => {
  const a1 = { n: 1, sources: [] }, fault = { owner: 'Alpha', rule: 'critic', fault: 'a is too small', fix: 'raise a', chunks: ['__CHUNK__'] };
  /** r001: a = 1, b = 5 fails the check in its only round. */
  const parentRun = () => run('cont-parent', { 'toy-a.1.json': a1, 'toy-b.1.json': { n: 5, sources: [] }, 'toy-critic.1.json': pass }, { rounds: 1 });
  const only = async (from: string, id: string, name: string) => { const dir = join(scratch, name); await mkdir(join(dir, 'runs', 'toy'), { recursive: true }); await cp(join(from, 'runs', 'toy', id), join(dir, 'runs', 'toy', id), { recursive: true }); return { LEVELS_RUNS: join(dir, 'runs') }; };

  test('round 1 starts from the last candidate and the open faults: the owner is asked again with its previous proposal, the others are reused as they were', async () => {
    const p = await parentRun(); expect(p.r.outcome).toBe('REJECTED');
    const c = await run('cont-child', { 'toy-b.1.json': { n: 2, sources: [] }, 'toy-critic.1.json': pass }, { dir: p.dir, continue: 'r001' });
    expect(c.r.outcome).toBe('ACCEPTED'); expect(c.r.runId).toBe('r002'); expect(c.runJson.finalVersion).toBe('1.1.0-r002.1');
    expect(c.runJson.rounds).toHaveLength(1); expect(c.runJson.rounds[0]).toMatchObject({ ran: ['b'], reused: ['a'], criticRan: true, verdict: 'pass' }); expect(calls(c)).toBe(2); // b, critic: not a
    expect(c.runJson.continues).toMatchObject({ plan: 'toy', run: 'r001', outcome: 'REJECTED', round: 1, version: '1.1.0-r001.1', file: 'inherited.json', sameInputs: true });
    const inh = await Bun.file(join(c.r.rdir, 'inherited.json')).json();
    expect(inh.proposals).toEqual({ a: a1, b: { n: 5, sources: [] } }); expect(inh.faults.map((f: any) => [f.owner, f.rule])).toEqual([['Beta', 'TOY-1']]);
    expect(await Bun.file(join(c.r.rdir, 'round-1', 'a.json')).json()).toEqual(a1); expect(await Bun.file(join(c.r.rdir, 'round-1', 'a.call.json')).json()).toMatchObject({ reused: true, id: '01-toy-a' });
    const prompt = await Bun.file(join(c.dir, 'out', 'toy', 'r002', 'calls', '01-toy-b.prompt.txt')).text();
    expect(prompt).toContain('Your previous proposal (JSON):\n{"n":5,"sources":[]}'); expect(prompt).toContain('Critic faults on it, to be fixed'); expect(prompt).toContain('b is not a + 1');
    expect(prompt).not.toContain('Task: a');
    const both = await withRuns(c.env, () => c.replay('r002')); expect(fails(both)).toEqual([]); expect(both.ok).toBe(true);
    expect(both.lines).toContain('ok    parent r001 on disk is the run inherited from');
  });
  test('the new run replays alone, without its parent; tampering with what it inherited, or with the parent it names, fails', async () => {
    const p = await parentRun(), c = await run('cont-alone', { 'toy-b.1.json': { n: 2, sources: [] }, 'toy-critic.1.json': pass }, { dir: p.dir, continue: 'r001' });
    const alone = await only(c.dir, 'r002', 'cont-alone-only'), solo = await withRuns(alone, () => c.replay('r002'));
    expect(fails(solo)).toEqual([]); expect(solo.ok).toBe(true); expect(solo.lines).toContain("ok    parent r001 is not on this machine; the inherited state is the run's own pin");
    const edited = async (name: string, f: (dir: string) => Promise<void>) => { const e = await only(c.dir, 'r002', name); await f(join(e.LEVELS_RUNS, 'toy', 'r002')); return withRuns(e, () => c.replay('r002')); };
    expect((await edited('cont-t1', async d => { const j = await Bun.file(join(d, 'inherited.json')).json(); j.faults = []; await Bun.write(join(d, 'inherited.json'), JSON.stringify(j, null, 2) + '\n'); })).lines).toContain('FAIL  inherited.json does not match the recorded sha256');
    expect((await edited('cont-t2', async d => { await Bun.write(join(d, 'round-1', 'a.json'), JSON.stringify({ n: 7, sources: [] })); })).lines.join('\n')).toContain('round-1/a: differs from the inherited proposal');
    expect((await edited('cont-t3', async d => { await Bun.$`rm ${join(d, 'inherited.json')}`; })).lines.join('\n')).toContain('inherited.json is missing');
    expect((await edited('cont-t4', async d => { const f = join(d, 'run.json'), j = await Bun.file(f).json(); delete j.continues; await Bun.write(f, JSON.stringify(j)); })).lines.join('\n')).toContain('does not say this run continues another');
    const changedParent = join(scratch, 'cont-t5'); await cp(c.dir, changedParent, { recursive: true });
    const pf = join(changedParent, 'runs', 'toy', 'r001', 'run.json'), pj = await Bun.file(pf).json(); pj.date = '2000-01-01'; await Bun.write(pf, JSON.stringify(pj));
    expect((await withRuns({ LEVELS_RUNS: join(changedParent, 'runs') }, () => c.replay('r002'))).lines).toContain('FAIL  parent r001 on disk differs from the run inherited from');
  });
  test("a fault on the first builder reruns everything from it; the critic's cited passage is pinned into the new run's retrieval", async () => {
    const p = await run('cont-first', { 'toy-a.1.json': a1, 'toy-b.1.json': { n: 2, sources: [] }, 'toy-critic.1.json': { verdict: 'fail', summary: 'a', faults: [fault], sources: [] } }, { rounds: 1 });
    expect(p.runJson.rounds[0]).toMatchObject({ toyOk: true, criticRan: true, faultOwners: ['Alpha'] });
    const c = await run('cont-first-child', { 'toy-a.1.json': { n: 3, sources: [] }, 'toy-b.1.json': { n: 4, sources: [] }, 'toy-critic.1.json': pass }, { dir: p.dir, continue: 'r001' });
    expect(c.runJson.rounds[0]).toMatchObject({ ran: ['a', 'b'], reused: [] }); expect(c.r.outcome).toBe('ACCEPTED');
    const inh = await Bun.file(join(c.r.rdir, 'inherited.json')).json(), cited = inh.faults[0].chunks[0];
    expect(cited).toMatch(/^[0-9a-f]{24}$/); expect(await Bun.file(join(c.r.rdir, 'round-1', 'a.call.json')).json()).toMatchObject({ pinning: { cited: 1, pinned: 1 } });
    expect((await Bun.file(join(c.r.rdir, 'round-1', 'a.call.json')).json()).retrieval.hits.some((h: any) => h.key === cited)).toBe(true);
    expect(fails(await withRuns(c.env, () => c.replay('r002')))).toEqual([]);
  });
  test('a run that is not REJECTED, or is not the same objective, is refused with the reason, and nothing is written', async () => {
    const ok = await run('cont-refuse', { 'toy-a.1.json': a1, 'toy-b.1.json': { n: 2, sources: [] }, 'toy-critic.1.json': pass }); expect(ok.r.outcome).toBe('ACCEPTED');
    const runs = join(ok.dir, 'runs', 'toy'), mk = async (id: string, r: object) => { await mkdir(join(runs, id), { recursive: true }); await Bun.write(join(runs, id, 'run.json'), JSON.stringify(r)); };
    await mk('r002', { objective: 'toy', outcome: 'ERROR', error: 'Error: claude exited 1' }); await mk('r003', { objective: 'toy', outcome: 'INCOMPLETE', rounds: [] }); await mk('r004', { objective: 'other', outcome: 'REJECTED', rounds: [] });
    const before = (await readdir(runs)).sort();
    const refused = async (id: string, text: string) => {
      const e = await run('cont-refuse-' + id, { 'toy-critic.1.json': pass }, { dir: ok.dir, continue: id }).then(() => 'ran', (x: Error) => x.message);
      expect(e).toContain(text);
    };
    await refused('r001', 'r001 is ACCEPTED: there is nothing to repair'); await refused('r002', 'r002 ended in ERROR (Error: claude exited 1)'); await refused('r003', 'r003 is INCOMPLETE');
    await refused('r004', 'r004 belongs to the other objective; this is the toy objective'); await refused('r099', 'there is no toy/r099');
    expect((await readdir(runs)).sort()).toEqual(before);
  });
});

describe('the kit digest is pinned into every run', () => {
  test('replay rebuilds the corpus from the pinned digest, so a later adoption in kit.json does not break an earlier run', async () => {
    const kit = join(scratch, 'kit.json'), real = await Bun.file(join(root, 'contracts/world/kit.json')).text(); await Bun.write(kit, real);
    const t = await run('kit-pin', { 'toy-a.1.json': { n: 1, sources: [] }, 'toy-b.1.json': { n: 2, sources: [] }, 'toy-critic.1.json': pass }, { env: { SUBORNED_KIT: kit } });
    expect(t.runJson.inputKit).toMatchObject({ source: kit, sha256: expect.stringMatching(/^[0-9a-f]{64}$/), assets: 241 });
    const digest = await Bun.file(join(t.r.rdir, 'input.kit.digest.json')).text(); expect(digest.length).toBeLessThan(real.length / 10);
    const ok = await withRuns({ ...t.env, SUBORNED_KIT: kit }, () => t.replay()); expect(fails(ok)).toEqual([]); expect(ok.lines.join('\n')).toContain('kit digest');
    const edited = JSON.parse(real); edited.assets.find((a: any) => a.status === 'candidate').status = 'adopted'; await Bun.write(kit, JSON.stringify(edited)); // Vela adopts a piece
    const later = await withRuns({ ...t.env, SUBORNED_KIT: kit }, () => t.replay()); expect(fails(later)).toEqual([]); expect(later.ok).toBe(true);
    await Bun.write(join(t.r.rdir, 'input.kit.digest.json'), digest.replace('"adopted"', '"candidate"')); // a tampered pin
    expect((await withRuns({ ...t.env, SUBORNED_KIT: kit }, () => t.replay())).lines).toContain('FAIL  input.kit.digest.json does not match the recorded sha256');
    await Bun.$`rm ${join(t.r.rdir, 'input.kit.digest.json')}`;
    expect((await withRuns({ ...t.env, SUBORNED_KIT: kit }, () => t.replay())).lines).toContain('FAIL  input.kit.digest.json is missing');
  });
});

describe('a call that is shown pictures', () => {
  const imgs = join(scratch, 'images'), a = join(imgs, 'a.png'), b = join(imgs, 'b.jpg'), bytesA = new Uint8Array([0x89, 0x50, 0x4e, 0x47, 7, 7]), bytesB = new Uint8Array([0xff, 0xd8, 1, 2, 3]);
  const withImages: Plugin<null> = { ...toy, builders: [{ ...toy.builders[0]!, objective: { ...toy.builders[0]!.objective, images: [a, b] } }, toy.builders[1]!] };
  const sha = (d: Uint8Array) => new Bun.CryptoHasher('sha256').update(d).digest('hex');
  test('the model gets the bytes, the record keeps only sha256 pins, the run stays small and replays with the pictures present, changed, or gone', async () => {
    await mkdir(imgs, { recursive: true }); await Bun.write(a, bytesA); await Bun.write(b, bytesB);
    const dir = join(scratch, 'images-run'), fx = join(dir, 'fx'), state = join(dir, 'state'); await mkdir(fx, { recursive: true }); await mkdir(state, { recursive: true });
    for (const [f, v] of Object.entries({ 'toy-a.1.json': { n: 1, sources: [] }, 'toy-b.1.json': { n: 2, sources: [] }, 'toy-critic.1.json': pass })) await Bun.write(join(fx, f), JSON.stringify(v));
    const env = { LEVELS_RUNS: join(dir, 'runs'), LEVELS_OUT: join(dir, 'out'), CLAUDE_BIN: stub, STUB_FIXTURES: fx, STUB_STATE: state };
    const old = Object.fromEntries(Object.keys(env).map(k => [k, process.env[k]])); Object.assign(process.env, env);
    let r: Awaited<ReturnType<typeof generate>>;
    try { r = await generate(withImages, { input: 'toy', rounds: 1, retriever: lexical, retrieval: 'lexical (test only)' }); } finally { for (const [k, v] of Object.entries(old)) v === undefined ? delete process.env[k] : (process.env[k] = v); }
    expect(r.outcome).toBe('ACCEPTED');
    expect(await Bun.file(join(state, 'toy-a.images.json')).json()).toEqual([{ mediaType: 'image/png', sha256: sha(bytesA) }, { mediaType: 'image/jpeg', sha256: sha(bytesB) }]); // what reached the model, in order
    expect(await Bun.file(join(state, 'toy-b.images.json')).exists()).toBe(false); // a builder without pictures is called as before
    const call = await Bun.file(join(r.rdir, 'round-1', 'a.call.json')).json();
    expect(call.images).toEqual([{ path: a, mediaType: 'image/png', bytes: 6, sha256: sha(bytesA) }, { path: b, mediaType: 'image/jpeg', bytes: 5, sha256: sha(bytesB) }]);
    expect((await Bun.file(join(r.rdir, 'round-1', 'b.call.json')).json()).images).toBeUndefined();
    for (const f of ['run.json', 'round-1/a.call.json']) expect(await Bun.file(join(r.rdir, f)).text()).not.toContain(Buffer.from(bytesA).toString('base64'));
    expect(await Bun.file(join(dir, 'out', 'toy', 'r001', 'calls', '01-toy-a.prompt.txt')).text()).toContain(`Attached images (in order, shown before this text): 1. images/a.png (sha256 ${sha(bytesA).slice(0, 12)}); 2. images/b.jpg`);
    const replayIt = () => withRuns(env, () => replayRun({ toy: withImages }, 'toy', 'r001'));
    const here = await replayIt(); expect(fails(here)).toEqual([]); expect(here.lines).toContain('ok    round-1/a: 2 image(s) pinned by sha256 (2 checked here, 0 not on this machine)');
    await Bun.write(a, new Uint8Array([1])); // the picture on this machine is not the one the run was shown
    expect((await replayIt()).lines.join('\n')).toContain('differs from its pinned sha256');
    await Bun.$`rm ${a} ${b}`; // offline without the store: the pin is all there is
    const gone = await replayIt(); expect(fails(gone)).toEqual([]); expect(gone.ok).toBe(true); expect(gone.lines).toContain('ok    round-1/a: 2 image(s) pinned by sha256 (0 checked here, 2 not on this machine)');
    const rj = join(r.rdir, 'round-1', 'a.call.json'), j = JSON.parse(await Bun.file(rj).text()); j.images[0].sha256 = 'nope'; await Bun.write(rj, JSON.stringify(j));
    expect((await replayIt()).lines.join('\n')).toContain('has no valid sha256 pin');
  });
});
