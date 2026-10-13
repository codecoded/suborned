// What a run records about how it was made (brief 007 F review, P5 and P6): each call names what it answered, run.json
// the commit and the approval, and replay notices an edited brief or schema without failing the run.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { existsSync } from 'node:fs';
import { join } from 'node:path';
import { fails, pass, runToy, scratch } from './toy';

setDefaultTimeout(120000);
const num = (n: number) => ({ n, sources: [] });
const hex = /^[0-9a-f]{64}$/;
const fixtures = { 'toy-a.1.json': num(1), 'toy-b.1.json': num(2), 'toy-critic.1.json': pass };

test('each call records the hashes of its brief, prompt and schema and its effort; run.json the commit', async () => {
  const t = await runToy('records', fixtures);
  const call = (k: string) => Bun.file(join(t.r.rdir, 'round-1', `${k}.call.json`)).json();
  const calls = await Promise.all(['a', 'b', 'keel'].map(call));
  for (const call of calls) {
    expect(call.brief.sourceHash).toMatch(hex); expect(call.promptSha256).toMatch(hex);
    expect(call.schemaSha256).toMatch(hex); expect(call.effort).toBe('medium');
  }
  expect(new Set(calls.map(c => c.promptSha256)).size).toBe(3); // three prompts
  expect(t.runJson.commit === null || /^[0-9a-f]{40}$/.test(t.runJson.commit)).toBe(true);
  expect(typeof t.runJson.dirty).toBe('boolean');
  expect(t.runJson).toMatchObject({ live: false, maxUsd: 10 });
  expect(t.runJson).not.toHaveProperty('approvedBy');
  const written = await Bun.file(join(t.dir, 'out', 'toy', 'r001', 'calls', '01-toy-a.prompt.txt')).text();
  const sha = new Bun.CryptoHasher('sha256').update(written).digest('hex');
  expect(calls[0].promptSha256).toBe(sha); // the prompt hashed is the one sent
});

test('who approved a run is recorded in run.json', async () => {
  const said = 'Sky, in the session of 2026-10-14';
  const t = await runToy('records-approved', fixtures, { approvedBy: said });
  expect(t.runJson.approvedBy).toBe(said);
});

test('replay tells an edited brief from an intact one, and a record without the hashes still replays', async () => {
  const t = await runToy('records-brief', fixtures);
  const good = await t.replay();
  expect(fails(good)).toEqual([]); expect(good.lines).toContain('ok    round-1/a: brief hash');
  const callFile = join(t.r.rdir, 'round-1', 'a.call.json'), call = await Bun.file(callFile).json();
  await Bun.write(callFile, JSON.stringify({ ...call, brief: { ...call.brief, sourceHash: '0'.repeat(64) } }));
  const warned = await t.replay();
  expect(fails(warned)).toEqual([]);
  expect(warned.lines.some(l => l.startsWith('warn') && l.includes('edited since the call'))).toBe(true);
  const { brief: _b, promptSha256: _p, schemaSha256: _s, effort: _e, ...old } = call;
  await Bun.write(callFile, JSON.stringify(old)); // recorded before the fields
  expect(fails(await t.replay())).toEqual([]);
});

test('the commit and the dirty flag are read before the run folder exists (r011 recorded dirty from a clean tree)', async () => {
  const seen: boolean[] = [], commit = 'a'.repeat(40);
  const t = await runToy('git-first', fixtures, { git: () => { seen.push(existsSync(join(scratch, 'git-first', 'runs'))); return { commit, dirty: false }; } });
  expect(seen).toEqual([false]); // asked once, with nothing of the run on disk yet
  expect(t.runJson).toMatchObject({ commit, dirty: false });
  expect(existsSync(join(t.r.rdir, 'run.json'))).toBe(true);
});
