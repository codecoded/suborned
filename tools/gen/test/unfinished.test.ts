// A run that did not finish is listed, never replayed and never a failure; its last complete round can be continued
// (brief 007 F review, P4). The spend ceiling ends a run INCOMPLETE the same way (P5).
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';
import { replayRun } from '../replay';
import { fails, pass, runToy, toy } from './toy';

setDefaultTimeout(120000);
const num = (n: number) => ({ n, sources: [] });
/** a = 1, b = 5 fails the check in round 1 (b should be 2); b = 2 passes it. */
const first = { 'toy-a.1.json': num(1), 'toy-b.1.json': num(5) };

/** Replays the run in `dir`'s runs/ folder. */
async function replayAt(dir: string, id: string) {
  const old = process.env.LEVELS_RUNS;
  process.env.LEVELS_RUNS = join(dir, 'runs');
  try { return await replayRun({ toy }, 'toy', id); } finally {
    if (old === undefined) delete process.env.LEVELS_RUNS; else process.env.LEVELS_RUNS = old;
  }
}
async function edited(file: string, change: (run: Record<string, unknown>) => void) {
  const run = await Bun.file(file).json();
  change(run);
  await Bun.write(file, JSON.stringify(run));
}

describe('a run that did not finish', () => {
  test('an ERROR run, or a folder with no run.json, is reported as not completed and is not a failure', async () => {
    const t = await runToy('unfinished-error', first, { rounds: 1 });
    await edited(join(t.r.rdir, 'run.json'), j => { j.outcome = 'ERROR'; j.error = 'Error: the model went away'; });
    const res = await t.replay();
    expect(res.notCompleted).toContain('outcome ERROR: Error: the model went away');
    expect(fails(res)).toEqual([]); expect(res.ok).toBe(true);
    await mkdir(join(t.dir, 'runs', 'toy', 'r099'), { recursive: true });
    const none = await replayAt(t.dir, 'r099');
    expect(none.notCompleted).toContain('no run.json'); expect(none.ok).toBe(true);
  });

  test('--continue takes an ERROR run from its last complete round, with its open faults', async () => {
    const t = await runToy('unfinished-continue', first, { rounds: 1 });
    await edited(join(t.r.rdir, 'run.json'), j => { j.outcome = 'ERROR'; j.error = 'Error: killed in round 2'; });
    const next = await runToy('unfinished-continue', { 'toy-b.2.json': num(2), 'toy-critic.1.json': pass }, {
      rounds: 2, continue: t.r.runId,
    });
    expect(next.r.outcome).toBe('ACCEPTED');
    expect(next.runJson.continues).toMatchObject({ run: t.r.runId, outcome: 'ERROR', round: 1 });
    const res = await next.replay();
    expect(fails(res)).toEqual([]); expect(res.ok).toBe(true);
  });
});

describe('the spend ceiling', () => {
  test('a call whose estimate would pass --max-usd is not made: the run ends INCOMPLETE and says why', async () => {
    const t = await runToy('unfinished-ceiling', first, { rounds: 1, maxUsd: 0.0000001 });
    expect(t.r.outcome).toBe('INCOMPLETE');
    expect(t.runJson.stopped).toMatchObject({ reason: 'max-usd', maxUsd: 0.0000001 });
    expect(t.runJson.cost.calls).toBe(0);
    const res = await t.replay();
    expect(res.notCompleted).toContain('INCOMPLETE'); expect(fails(res)).toEqual([]);
  });

  test('a run with no complete round has nothing to continue from', async () => {
    const t = await runToy('unfinished-nothing', first, { rounds: 1, maxUsd: 0.0000001 });
    await expect(runToy('unfinished-nothing', first, { rounds: 1, continue: t.r.runId }))
      .rejects.toThrow('there is nothing to continue from');
  });
});
