// bun run causes: the standing drift causes are declared for the current hash of each objective, once (a second run changes nothing), into the ledger the run names (LEVELS_LEDGER in a test).
import { expect, test } from 'bun:test';
import { mkdtemp, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { declaredFor } from '../../gen/ledger';
import { readLedger } from '../drift';
import { peopleHash } from '../people/hash';
import { redeclare } from '../causes';

test('every cause in causes.json is declared for the hash, with its rules, and a second run leaves the ledger as it was', async () => {
  const dir = await mkdtemp(join(tmpdir(), 'causes-')), file = join(dir, 'causes.json');
  await writeFile(file, JSON.stringify({ people: [{ cause: 'a change', rules: ['P-0', 'P-13'] }, { cause: 'another', rules: [] }], transit: [], skyline: [] }));
  const was = process.env.LEVELS_LEDGER; process.env.LEVELS_LEDGER = dir;
  try {
    const one = await redeclare(['people'], file), hash = await peopleHash();
    expect(one.people).toEqual({ hash: hash.slice(0, 12), causes: 2 });
    const ledger = readLedger(join(dir, 'people.json')), list = declaredFor(ledger, hash);
    expect(list).toEqual([{ cause: 'a change', rules: ['P-0', 'P-13'] }, { cause: 'another', rules: [] }]);
    await redeclare(['people'], file);
    expect(readLedger(join(dir, 'people.json'))).toEqual(ledger);
  } finally { if (was === undefined) delete process.env.LEVELS_LEDGER; else process.env.LEVELS_LEDGER = was; }
});

test('the committed causes.json parses, names rule ids, and holds only people causes so far', async () => {
  const c = JSON.parse(await Bun.file(join(import.meta.dir, '../causes.json')).text());
  expect(Object.keys(c)).toEqual(['note', 'people', 'transit', 'skyline']);
  for (const x of [...c.people, ...c.transit, ...c.skyline]) { expect(x.cause.length).toBeGreaterThan(40); for (const r of x.rules) expect(r).toMatch(/^[A-Z]+-[A-Z]?\d+[a-z]?$/); }
  expect(c.people.length).toBeGreaterThanOrEqual(5);
});
