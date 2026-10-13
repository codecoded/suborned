// A critic that fails the candidate without naming a fault (brief 007 F review, P7): its summary goes to the
// builders as a fault, so the next round does not rerun blind.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { join } from 'node:path';
import { fails, pass, runToy } from './toy';

setDefaultTimeout(120000);
const num = (n: number) => ({ n, sources: [] });

test('a fail with no faults tells every builder what the critic wrote; the run goes on to a better round', async () => {
  const silent = { verdict: 'fail', summary: 'Both numbers are far too timid.', faults: [], sources: [] };
  const t = await runToy('silent', {
    'toy-a.1.json': num(1), 'toy-b.1.json': num(2), 'toy-critic.1.json': silent,
    'toy-a.2.json': num(10), 'toy-b.2.json': num(11), 'toy-critic.2.json': pass,
  });
  expect(t.r.outcome).toBe('ACCEPTED');
  type Row = { keelVerdict: string; faults: number };
  expect(t.runJson.rounds.map((x: Row) => [x.keelVerdict, x.faults])).toEqual([['fail', 2], ['pass', 0]]);
  const faults: { owner: string; rule: string; fault: string; synthesized: boolean }[] =
    await Bun.file(join(t.r.rdir, 'round-1', 'faults.json')).json();
  expect(faults.map(f => [f.owner, f.rule, f.fault, f.synthesized])).toEqual([
    ['Alpha', 'critic', 'Both numbers are far too timid.', true],
    ['Beta', 'critic', 'Both numbers are far too timid.', true],
  ]);
  const second = await Bun.file(join(t.r.rdir, 'round-2', 'a.call.json')).json();
  expect(second.reused).toBeUndefined(); // Alpha heard the fault, so Alpha ran again
  const res = await t.replay();
  expect(fails(res)).toEqual([]); expect(res.ok).toBe(true);
});

test('with no summary either, the fault says that the critic gave no reason', async () => {
  const mute = { verdict: 'fail', summary: ' ', faults: [], sources: [] };
  const t = await runToy('silent-mute', {
    'toy-a.1.json': num(1), 'toy-b.1.json': num(2), 'toy-critic.1.json': mute,
    'toy-a.2.json': num(3), 'toy-b.2.json': num(4), 'toy-critic.2.json': pass,
  }, { rounds: 2 });
  const faults: { fault: string }[] = await Bun.file(join(t.r.rdir, 'round-1', 'faults.json')).json();
  expect(faults.map(f => f.fault)).toEqual(Array(2).fill('the critic failed the candidate and gave no reason'));
});
