// A toy objective for the engine's optional hooks: two builders, one deterministic check, an apply fault, a retrieval query hook and a preview
// stand-in. Offline: the stub claude, the lexical retriever, scratch folders; no model, no index. (engine.test.ts has the plain toy.)
import { mkdir, mkdtemp } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { z } from 'zod';
import { lexical } from '../../levels/test/lexical';
import { generate } from '../loop';
import type { Plugin } from '../plugin';
import { replayRun } from '../replay';
import { verdictOut } from '../verdict';

export const scratch = await mkdtemp(join(tmpdir(), 'gen-hooks-'));
const brief = join(scratch, 'brief.md');
export const stub = join(import.meta.dir, '../../levels/test/claude-stub.ts');
await Bun.write(brief, 'Toy objective: a proposes a number, b proposes the next one.');
const Num = z.object({ n: z.number(), sources: z.array(z.string().regex(/^[0-9a-f]{24}$/)) });
const Verdict = verdictOut(z.enum(['Alpha', 'Beta']));
const schemas: Record<string, z.ZodType> = { a: Num, b: Num, keel: Verdict };
const member = (key: string, owner: string, role: string) => ({
  key, owner, objective: { id: `toy-${key === 'keel' ? 'critic' : key}`, role, brief, query: ['toy number'] }, schema: schemas[key]!,
});

/** b = 13 is an apply fault (a proposal the plan cannot take); b = a + 1 is the check. */
export const APPLY_FAULT = 13;
const facts = (a: { n: number }, b: { n: number }) => ({ ok: b.n === a.n + 1, applyFaults: b.n === APPLY_FAULT ? 1 : 0 });

export const toy: Plugin<null> = {
  id: 'toy', defaultInput: 'toy',
  builders: [member('a', 'Alpha', 'world-planner'), member('b', 'Beta', 'level-designer')],
  critic: member('keel', 'Keel', 'simulation-engineer'),
  header: {}, code: { label: 'toy', hash: async () => 'toy-code', ledger: () => ({ recorded: {}, declared: {} }), ledgerFile: 'none', declare: 'none' },
  open: async () => ({ id: 'toy', version: '1.0.0', pin: async () => ({ state: null, record: { pinned: 'nothing' } }) }),
  query: (_s, r, key) => [`toy round ${r.n} ${key}`],
  standIn: (_s, key) => (key === 'keel' ? undefined : { n: 0, sources: [] }),
  task: async (_s, r, key) => `Task: ${key}${key === 'b' ? ` after ${r.outs.a.n}` : ''}`,
  async assess(_s, r) {
    const { ok, applyFaults } = facts(r.outs.a, r.outs.b);
    return {
      ok, line: ok ? 'toy clean' : 'toy FAIL TOY-1', record: { toyOk: ok },
      failures: ok ? [] : [{ rule: 'TOY-1', owners: ['Beta'], fault: 'b is not a + 1', fix: 'b = a + 1' }],
      faults: applyFaults ? [{ owner: 'Beta', rule: 'apply', fault: 'b cannot be applied', fix: 'another b', chunks: [], synthesized: true }] : [],
      criticTask: () => 'Task: judge the toy candidate.',
    };
  },
  replay: {
    skipNote: 'nothing', inputVersion: () => '1.0.0', superseded: () => false, checksOk: r => r.toyOk, schema: (_run, key) => schemas[key]!,
    pins: { raw: async () => ({}), input: async () => ({}), block: async () => ({}) },
    async candidate(c, r) {
      const { ok, applyFaults } = facts(await c.read(`round-${r.round}/a.json`), await c.read(`round-${r.round}/b.json`));
      if (ok !== r.toyOk) c.fail(`round-${r.round}: toy check differs from the record`);
      return { differs: ok !== r.toyOk, added: [], assessFaults: applyFaults };
    },
  },
};

export const pass = { verdict: 'pass', summary: '', faults: [], sources: [] };
export type RunOpts = {
  rounds?: number; preview?: boolean; plugin?: Plugin<null>; env?: Record<string, string>; maxUsd?: number; continue?: string;
  approvedBy?: string;
};

/** Runs the toy with the stub model answering from `fixtures` (file name → JSON). Returns the run, its folders and a replay of it. */
export async function runToy(name: string, fixtures: Record<string, unknown>, o: RunOpts = {}) {
  const dir = join(scratch, name), fx = join(dir, 'fixtures'), state = join(dir, 'state');
  await mkdir(fx, { recursive: true }); await mkdir(state, { recursive: true });
  for (const [f, v] of Object.entries(fixtures)) await Bun.write(join(fx, f), JSON.stringify(v));
  const env = { LEVELS_RUNS: join(dir, 'runs'), LEVELS_OUT: join(dir, 'out'), CLAUDE_BIN: stub, STUB_FIXTURES: fx, STUB_STATE: state, ...o.env };
  const old = Object.fromEntries(Object.keys(env).map(k => [k, process.env[k]]));
  Object.assign(process.env, env);
  try {
    const plugin = o.plugin ?? toy;
    const r = await generate(plugin, { input: 'toy', rounds: o.rounds ?? 3, retriever: lexical, retrieval: 'lexical (test only)', preview: o.preview, maxUsd: o.maxUsd, continue: o.continue,
      approvedBy: o.approvedBy });
    const replay = async () => {
      Object.assign(process.env, env);
      try { return await replayRun({ toy: plugin }, 'toy', r.runId); } finally { restore(old); }
    };
    return { r, dir, runJson: await Bun.file(join(r.rdir, 'run.json')).json(), replay };
  } finally { restore(old); }
}

function restore(old: Record<string, string | undefined>) {
  for (const [k, v] of Object.entries(old)) { if (v === undefined) delete process.env[k]; else process.env[k] = v; }
}
export const fails = (res: { lines: string[] }) => res.lines.filter(l => l.startsWith('FAIL'));
