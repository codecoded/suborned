// The people objective with no model and no index (brief 010 B): retrieval is lexical, the stand-in cast answers Pulse, every run lands in a scratch folder.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { mkdtemp, readdir } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { generate } from '../../../gen/loop';
import { runPreview } from '../../../gen/preview-run';
import { replayRun } from '../../../gen/replay';
import { PulseOut } from '../schemas';
import { standInCast } from '../../../people/standin';
import { reached } from '../../test/imports';
import { lexical } from '../../test/lexical';
import { schemaProblems } from '../../test/schema-lint';
import { z } from 'zod';
import { peopleFiles } from '../hash';
import { peoplePlugin as plugin } from '../plugin';
import { plugins } from '../../plugins';

setDefaultTimeout(300000);
const PLAN = 'contracts/world/harbour-district.plan.json';
const scratch = await mkdtemp(join(tmpdir(), 'people-'));
const opts = { input: PLAN, scope: 'civic-block', retriever: lexical, retrieval: 'lexical (test only)' };

describe('people plug-in', () => {
  test('the output schema passes the strict validator and takes the stand-in cast', async () => {
    expect(schemaProblems(z.toJSONSchema(PulseOut))).toEqual([]);
    const s = await plugin.open(PLAN, 'civic-block'), { state } = await s.pin(join(scratch, 'pin'));
    expect(PulseOut.safeParse(plugin.standIn!(state, 'pulse')).success).toBe(true);
    expect(standInCast(state.w.scope, state.w.rules).rows.length).toBe(5);
  });

  test('an unknown scope is refused', async () => {
    await expect(plugin.open(PLAN, 'harbour')).rejects.toThrow(/unknown scope/);
  });

  test('--preview: round 1 with no model; writes both prompts and an estimate', async () => {
    const dir = join(scratch, 'preview');
    const table = await runPreview(plugin, opts, dir);
    expect(table).toContain('no model was called');
    expect(table).toMatch(/round 1: about \$\d+\.\d\d/);
    const calls = join(dir, 'out', 'harbour-district', 'r001', 'calls');
    expect((await readdir(calls)).filter(f => f.endsWith('.prompt.txt')).sort()).toEqual(['01-people-pulse.prompt.txt', '02-people-critic.prompt.txt']);
    const pulse = await Bun.file(join(calls, '01-people-pulse.prompt.txt')).text();
    for (const m of ['Task: propose the cast tables', 'records/diner', 'records/records-office', 'records/resident', 'Temperaments (JSON)']) expect(pulse).toContain(m);
    expect(await Bun.file(join(calls, '02-people-critic.prompt.txt')).text()).toContain('People checker report on the cast');
  });

  test('a stand-in run is ACCEPTED, and replays offline', async () => {
    const runs = join(scratch, 'runs'), out = join(scratch, 'out');
    process.env.LEVELS_RUNS = runs; process.env.LEVELS_OUT = out;
    try {
      const r = await generate(plugin, { ...opts, rounds: 1, preview: true });
      expect(r.outcome).toBe('ACCEPTED');
      const run = await Bun.file(join(r.rdir, 'run.json')).json();
      expect(run).toMatchObject({ objective: 'people', scope: 'civic-block' });
      expect(Object.keys(run)).toEqual(expect.arrayContaining(['inputLife', 'inputRules', 'inputMission', 'inputPlan', 'inputBlock']));
      const rep = await replayRun(plugins, 'harbour-district', r.runId);
      expect(rep.lines.filter(l => l.startsWith('FAIL'))).toEqual([]);
      expect(rep.ok).toBe(true);
      expect(rep.lines).toContain(`ok    round-1: checker rerun matches checks.json (25/25)`); // P-0..P-16 and N-1..N-8 (brief 017)
      expect(Object.keys(run)).toEqual(expect.arrayContaining(['inputNeedsRules', 'inputNeeds'])); // the need kinds and the catalogue are pinned like the rules
    } finally { delete process.env.LEVELS_RUNS; delete process.env.LEVELS_OUT; }
  });

  test('the code hash covers every file the verdict reaches', async () => {
    const listed = new Set(peopleFiles()), dirs = ['tools/plan/', 'tools/people/', 'contracts/people/'];
    const seen = await reached(['tools/people/check.ts', 'tools/people/cli.ts', 'tools/levels/people/cast.ts', 'tools/levels/people/pins.ts']);
    const missing = [...seen].filter(f => !listed.has(f) && !dirs.some(d => f.startsWith(d)) && !f.startsWith('tools/people/test/'));
    expect(missing).toEqual([]);
  });
});
