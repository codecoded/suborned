// The engine's optional hooks, on a toy objective: a retrieval query per builder, token counts in run.json, the critic gate's apply-fault leg
// recomputed by replay, and the prompt preview that makes no model call.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { join } from 'node:path';
import type { Call } from '../../rag/agent';
import { generate } from '../loop';
import { previewRows } from '../preview';
import type { Plugin } from '../plugin';
import { lexical } from '../../levels/test/lexical';
import { APPLY_FAULT, fails, pass, runToy, scratch, stub, toy } from './toy';

setDefaultTimeout(120000);
const num = (n: number) => ({ n, sources: [] });

describe('retrieval query hook', () => {
  test("a plug-in's query for a member is appended to its objective's queries, per round", async () => {
    const t = await runToy('query', { 'toy-a.1.json': num(1), 'toy-b.1.json': num(5), 'toy-b.2.json': num(2), 'toy-critic.1.json': pass });
    const queries = async (round: number, key: string) => (await Bun.file(join(t.r.rdir, `round-${round}`, `${key}.call.json`)).json()).retrieval.queries;
    expect(await queries(1, 'a')).toEqual(['toy number', 'toy round 1 a']);
    expect((await queries(2, 'b')).slice(0, 2)).toEqual(['toy number', 'toy round 2 b']); // then the fault feedback's queries
  });
});

describe('the corpus a run checks once', () => {
  test('the first retrieval checks the disk against the index; every later one is told the hash it returned', async () => {
    const seen: (string | undefined)[] = [];
    const spy: typeof lexical = async (query, limit, pinned, expect) => {
      seen.push(expect);
      return lexical(query, limit, pinned);
    };
    const dir = join(scratch, 'expect');
    const env = { LEVELS_RUNS: join(dir, 'runs'), LEVELS_OUT: join(dir, 'out') };
    const fx = join(dir, 'fx'), state = join(dir, 'state');
    await Bun.$`mkdir -p ${fx} ${state}`;
    for (const [f, v] of Object.entries({ 'toy-a.1.json': num(1), 'toy-b.1.json': num(2), 'toy-critic.1.json': pass })) {
      await Bun.write(join(fx, f), JSON.stringify(v));
    }
    Object.assign(process.env, env, { CLAUDE_BIN: stub, STUB_FIXTURES: fx, STUB_STATE: state });
    try { await generate(toy, { input: 'toy', rounds: 1, retriever: spy, retrieval: 'spy (test only)' }); } finally {
      for (const k of [...Object.keys(env), 'CLAUDE_BIN', 'STUB_FIXTURES', 'STUB_STATE']) delete process.env[k];
    }
    const own = await lexical('x'); // what the retriever returns as its corpus hash
    expect(seen.length).toBeGreaterThanOrEqual(3);
    expect(seen[0]).toBeUndefined();
    expect(seen.slice(1).every(e => e === own.corpusHash)).toBe(true);
  });
});

describe('tokens in run.json', () => {
  test('each call records its token counts; replay accepts runs with and without them and refuses a malformed one', async () => {
    const t = await runToy('tokens', { 'toy-a.1.json': num(1), 'toy-b.1.json': num(2), 'toy-critic.1.json': pass });
    expect(t.runJson.calls.map((c: { tokens: unknown }) => c.tokens)).toEqual(t.runJson.calls.map(() => ({ input: 1, cacheRead: 0, output: 1 })));
    const res = await t.replay();
    expect(fails(res)).toEqual([]); expect(res.lines).toContain('ok    tokens recorded for 3 of 3 calls');
    const original = await Bun.file(join(t.r.rdir, 'run.json')).text(), edit = async (f: (j: any) => void) => {
      const j = JSON.parse(original); f(j); await Bun.write(join(t.r.rdir, 'run.json'), JSON.stringify(j)); return t.replay();
    };
    const old = await edit(j => { for (const c of j.calls) delete c.tokens; }); // a run recorded before the field
    expect(fails(old)).toEqual([]); expect(old.lines.join('\n')).not.toContain('tokens recorded');
    const bad = await edit(j => { j.calls[0].tokens = { input: -1 }; });
    expect(bad.ok).toBe(false); expect(bad.lines).toContain('FAIL  run.json calls: 1 malformed token counts');
  });
});

describe('the critic gate recomputed from the proposals', () => {
  test('replay recomputes the apply faults itself: a record that hides one fails, an honest record passes', async () => {
    const t = await runToy('assess', { 'toy-a.1.json': num(APPLY_FAULT - 1), 'toy-b.1.json': num(APPLY_FAULT), 'toy-b.2.json': num(APPLY_FAULT), 'toy-critic.1.json': pass }, { rounds: 1 });
    expect(t.runJson.rounds[0]).toMatchObject({ toyOk: true, assessFaults: 1, criticRan: false, keelVerdict: null, faultOwners: ['Beta'] });
    const res = await t.replay();
    expect(fails(res)).toEqual([]); expect(res.lines).toContain('ok    round-1: apply faults match the recomputation (1)');
    const original = await Bun.file(join(t.r.rdir, 'run.json')).text(), j = JSON.parse(original);
    j.rounds[0].assessFaults = 0; // the record says no apply fault, so it would say Keel should have been called
    await Bun.write(join(t.r.rdir, 'run.json'), JSON.stringify(j));
    const forged = await t.replay();
    expect(forged.ok).toBe(false); expect(forged.lines).toContain('FAIL  round-1: apply faults recorded 0, recomputed 1');
    // the gate runs on the recomputed count, so the round's criticRan is judged against the truth, not the forged record
    expect(forged.lines).toContain('ok    round-1: Keel not called, the candidate failed the checks');
  });
});

describe('prompt preview', () => {
  test('runs round 1 with the real task texts and no model: stand-ins answer, nothing costs, the prompts are written', async () => {
    const t = await runToy('preview', {}, { preview: true, rounds: 1, env: { CLAUDE_BIN: '/nonexistent/claude' } }); // a real call would fail to start
    const calls: Call[] = await Bun.file(join(t.dir, 'out', 'toy', 'r001', 'calls.json')).json();
    expect(calls.map(c => c.objective)).toEqual(['toy-a', 'toy-b', 'toy-critic']);
    expect(calls.every(c => c.costUsd === 0 && c.model === 'preview (no model call)' && c.tools?.join() === 'StructuredOutput')).toBe(true);
    expect(await Bun.file(calls[1]!.promptFile).text()).toContain('Task: b after 0'); // b's prompt carries a's stand-in
    expect(t.runJson.cost).toEqual({ usd: 0, calls: 3 });
    const rows = await previewRows(calls);
    expect(rows.map(r => r.id)).toEqual(calls.map(c => c.id)); expect(rows.every(r => r.chars > 0 && r.tokensIn > 0 && r.usd > 0)).toBe(true);
  });
  test('an objective without stand-ins cannot be previewed', async () => {
    const bare: Plugin<null> = { ...toy, standIn: undefined };
    const old = { ...process.env };
    process.env.LEVELS_RUNS = join(scratch, 'bare', 'runs'); process.env.LEVELS_OUT = join(scratch, 'bare', 'out');
    try {
      await expect(generate(bare, { input: 'toy', rounds: 1, retriever: lexical, preview: true })).rejects.toThrow('has no stand-in proposal for a');
    } finally { process.env.LEVELS_RUNS = old.LEVELS_RUNS; process.env.LEVELS_OUT = old.LEVELS_OUT; if (!old.LEVELS_RUNS) delete process.env.LEVELS_RUNS; if (!old.LEVELS_OUT) delete process.env.LEVELS_OUT; }
  });
});
