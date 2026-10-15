// The dialogue objective with no model and no index (brief 012): retrieval is lexical, the stand-in tables answer Fable, every run lands in a scratch folder.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { mkdtemp, readdir } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { z } from 'zod';
import { RunRefused } from '../../../gen/continue';
import { generate } from '../../../gen/loop';
import { runPreview } from '../../../gen/preview-run';
import { replayRun } from '../../../gen/replay';
import { reached } from '../../test/imports';
import { lexical } from '../../test/lexical';
import { schemaProblems } from '../../test/schema-lint';
import { plugins } from '../../plugins';
import { dialogueFiles } from '../hash';
import { dialoguePlugin as plugin } from '../plugin';
import { agents, FableOut } from '../schemas';

setDefaultTimeout(300000);
const PLAN = 'contracts/world/harbour-district.plan.json', STUB = join(import.meta.dir, '../../test/claude-stub.ts');
const scratch = await mkdtemp(join(tmpdir(), 'dialogue-'));
const opts = { input: PLAN, retriever: lexical, retrieval: 'lexical (test only)' };
// a preview reaches no model; the stand-in lexicon is allowed only when the model could not be reached either
const offline = (f: () => Promise<void>) => async () => {
  const keep = { b: process.env.CLAUDE_BIN, l: process.env.DIALOGUE_LEXICON };
  process.env.CLAUDE_BIN = STUB; process.env.DIALOGUE_LEXICON = 'stand-in';
  try { await f(); } finally { for (const [k, v] of [['CLAUDE_BIN', keep.b], ['DIALOGUE_LEXICON', keep.l]] as const) { if (v === undefined) delete process.env[k]; else process.env[k] = v; } }
};

describe('dialogue plug-in', () => {
  test('the output schemas pass the strict validator and take the stand-in tables', offline(async () => {
    expect(schemaProblems(z.toJSONSchema(FableOut))).toEqual([]);
    expect(schemaProblems(z.toJSONSchema(agents.keel))).toEqual([]);
    const s = await plugin.open(PLAN, 'lexicon'), { state } = await s.pin(join(scratch, 'pin'));
    expect(FableOut.safeParse(plugin.standIn!(state, 'fable')).success).toBe(true);
    expect(plugin.scopes).toEqual(['lexicon', 'scenes', 'street', 'civic']);
  }));

  test('an unknown scope is refused; a scope that fills slots from a lexicon nobody promoted is refused unless this is an offline preview', async () => {
    await expect(plugin.open(PLAN, 'harbour')).rejects.toThrow(/unknown scope/);
    const keep = process.env.DIALOGUE_LEXICON; delete process.env.DIALOGUE_LEXICON;
    try { await expect(plugin.open(PLAN, 'street')).rejects.toBeInstanceOf(RunRefused); await expect(plugin.open(PLAN, 'street')).rejects.toThrow(/lexicon/); } finally { if (keep !== undefined) process.env.DIALOGUE_LEXICON = keep; }
  });

  test('--preview: round 1 with no model; writes both prompts and an estimate; the prompt asks for the scope\'s cells', offline(async () => {
    const dir = join(scratch, 'preview'), table = await runPreview(plugin, { ...opts, scope: 'street' }, dir);
    expect(table).toContain('no model was called');
    expect(table).toMatch(/round 1: about \$\d+\.\d\d/);
    const calls = join(dir, 'out', 'harbour-district', 'r001', 'calls');
    expect((await readdir(calls)).filter(f => f.endsWith('.prompt.txt')).sort()).toEqual(['01-dialogue-fable.prompt.txt', '02-dialogue-critic.prompt.txt']);
    const fable = await Bun.file(join(calls, '01-dialogue-fable.prompt.txt')).text();
    for (const m of ['Task: write the voice tables for the scope street', 'Group counter', 'Group workshop', 'Group resident', 'reply/help/turned', 'questioned/gives', 'slots this group may NOT use'
      + ' (some of its people lack what fills them): work', 'the Handover\'s own lines', 'The lexicon your slots fill from']) expect(fable).toContain(m);
    const keel = await Bun.file(join(calls, '02-dialogue-critic.prompt.txt')).text();
    for (const m of ['Dialogue checker report', 'What the tables expand to', 'Candidate tables (JSON)']) expect(keel).toContain(m);
  }));

  for (const scope of ['lexicon', 'scenes', 'street', 'civic']) {
    test(`a stand-in run of the ${scope} scope is ACCEPTED, and replays offline`, offline(async () => {
      const runs = join(scratch, `runs-${scope}`), out = join(scratch, `out-${scope}`);
      process.env.LEVELS_RUNS = runs; process.env.LEVELS_OUT = out;
      try {
        const r = await generate(plugin, { ...opts, scope, rounds: 1, preview: true });
        expect(r.outcome).toBe('ACCEPTED');
        const run = await Bun.file(join(r.rdir, 'run.json')).json();
        expect(run).toMatchObject({ objective: 'dialogue', scope });
        expect(Object.keys(run)).toEqual(expect.arrayContaining(['inputPeople', 'inputNames', 'inputPeopleRules', 'inputRulebook', 'inputDialogueRules', 'inputHandoverText', 'inputPlan', 'inputBlock']));
        expect('inputLexicon' in run).toBe(scope !== 'lexicon');
        const rep = await replayRun(plugins, 'harbour-district', r.runId);
        expect(rep.lines.filter(l => l.startsWith('FAIL'))).toEqual([]);
        expect(rep.ok).toBe(true);
        expect(rep.lines.some(l => /round-1: checker rerun matches checks.json \(13\/13\)/.test(l)), rep.lines.join('\n')).toBe(true);
      } finally { delete process.env.LEVELS_RUNS; delete process.env.LEVELS_OUT; }
    }));
  }

  test('a candidate with a missing cell goes back to Fable with the rule that failed it', offline(async () => {
    const s = await plugin.open(PLAN, 'street'), { state } = await s.pin(join(scratch, 'pin-bad'));
    const out = plugin.standIn!(state, 'fable') as FableOut, bad = structuredClone(out);
    bad.voices.groups[0]!.cells = bad.voices.groups[0]!.cells.filter(c => !(c.event === 'reply' && c.verb === 'pay' && c.outcome === 'turned' && c.temperament === 'mercenary'));
    const rd = join(scratch, 'round-bad'), ev = await plugin.assess(state, { n: 1, version: 'v', outs: { fable: bad }, ran: ['fable'], skipped: [], rd, od: rd, odir: rd });
    expect(ev.ok).toBe(false);
    expect(ev.failures.map(f => f.rule)).toEqual(expect.arrayContaining(['D-0', 'D-1']));
    expect(ev.failures.every(f => f.owners.join() === 'Fable')).toBe(true);
    expect(plugin.ownersOf!('D-7')).toEqual(['Fable']);
  }));

  test('the code hash covers every file the verdict reaches', async () => {
    const listed = new Set(dialogueFiles()), dirs = ['contracts/dialogue/'];
    const seen = await reached(['tools/dialogue/check.ts', 'tools/levels/dialogue/cast.ts', 'tools/levels/dialogue/pins.ts']);
    const missing = [...seen].filter(f => !listed.has(f) && !dirs.some(d => f.startsWith(d)) && !f.startsWith('tools/dialogue/test/'));
    expect(missing).toEqual([]);
  });
});
