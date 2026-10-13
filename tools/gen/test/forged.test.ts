// Replay recomputes the outcome from the files of the run; run.json is only a claim to compare (brief 007 F review,
// P1).
// Each test makes an honest run of the toy objective, edits its records the way a "repair" of a run folder would, and
// expects replay to fail where it passed before.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { join } from 'node:path';
import type { Plugin } from '../plugin';
import { fails, pass, runToy } from './toy';

setDefaultTimeout(120000);
const num = (n: number) => ({ n, sources: [] });
const blocking = { owner: 'Beta', rule: 'critic', fault: 'b is too plain', fix: 'change b', chunks: ['__CHUNK__'] };
const keelPassesWithFault = { verdict: 'pass', summary: 'ok', faults: [blocking], sources: [] };
const keelFails = { verdict: 'fail', summary: 'no', faults: [blocking], sources: [] };

type Honest = Awaited<ReturnType<typeof runToy>>;
/** Replays `t` after `change` has edited run.json (a copy of the original is put back afterwards). */
type Run = { outcome: string; rounds: Record<string, unknown>[] };
async function forged(t: Honest, change: (run: Run) => void) {
  const file = join(t.r.rdir, 'run.json'), original = await Bun.file(file).text(), run = JSON.parse(original);
  change(run);
  await Bun.write(file, JSON.stringify(run));
  try { return await t.replay(); } finally { await Bun.write(file, original); }
}

describe('a rejected run edited to look accepted', () => {
  const rejected = () => runToy('forged-hand-off', {
    'toy-a.1.json': num(1), 'toy-b.1.json': num(2), 'toy-critic.1.json': keelPassesWithFault,
  }, { rounds: 1 });

  test('the honest run is REJECTED and replays', async () => {
    const t = await rejected();
    expect(t.r.outcome).toBe('REJECTED');
    const res = await t.replay();
    expect(fails(res)).toEqual([]); expect(res.ok).toBe(true);
  });

  test('zero faults, verdict pass and outcome ACCEPTED in run.json: faults.json still holds the fault', async () => {
    const t = await rejected();
    const res = await forged(t, run => {
      Object.assign(run.rounds[0]!, { faults: 0, verdict: 'pass', faultOwners: [] });
      run.outcome = 'ACCEPTED';
    });
    expect(res.ok).toBe(false);
    expect(fails(res).join('\n')).toContain('faults.json holds 1 fault(s), run.json says 0');
    expect(fails(res).join('\n')).toContain('run.json verdict pass, recomputed fail');
  });

  test('a fault Keel listed that faults.json does not carry', async () => {
    const t = await rejected();
    await Bun.write(join(t.r.rdir, 'round-1', 'faults.json'), '[]');
    const res = await forged(t, run => Object.assign(run.rounds[0]!, { faults: 0, verdict: 'pass', faultOwners: [] }));
    expect(fails(res).join('\n')).toContain('a fault Keel listed is not in faults.json');
  });
});

describe('a round Keel failed', () => {
  const failedByKeel = () => runToy('forged-keel', {
    'toy-a.1.json': num(1), 'toy-b.1.json': num(2), 'toy-critic.1.json': keelFails,
  }, { rounds: 1 });

  test('"Keel was not called" with a passing verdict and no keel.json is not a way out', async () => {
    const t = await failedByKeel();
    await Bun.$`rm ${join(t.r.rdir, 'round-1', 'keel.json')}`;
    const res = await forged(t, run => {
      const claim = { criticRan: false, keelVerdict: 'pass', verdict: 'pass', faults: 0, faultOwners: [] };
      Object.assign(run.rounds[0]!, claim);
      run.outcome = 'ACCEPTED';
    });
    expect(res.ok).toBe(false);
    expect(fails(res).join('\n')).toContain('criticRan false');
  });

  test('a loop-2 round that leaves out a gate field is moved onto an older path: refused', async () => {
    const t = await failedByKeel();
    const res = await forged(t, run => { delete run.rounds[0]!.assessFaults; });
    expect(fails(res).join('\n')).toContain('a loop-2 round records assessFaults, this one does not');
  });
});

describe('a round whose checks were never rerun', () => {
  test('checks that passed in the record, with nothing rerun to confirm them, fail', async () => {
    const blind: Plugin<null> = (await import('./toy')).toy;
    const plugin = { ...blind, replay: { ...blind.replay, candidate: async () => undefined } };
    const fixtures = { 'toy-a.1.json': num(1), 'toy-b.1.json': num(2), 'toy-critic.1.json': pass };
    const t = await runToy('forged-blind', fixtures, { plugin });
    expect(fails(await t.replay()))
      .toContain('FAIL  round-1: the checks passed in the record, but nothing was rerun to confirm it');
  });
});
