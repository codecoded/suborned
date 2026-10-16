// The ledger command line (tools/levels/drift.ts): `declare` carries the causes of every earlier hash forward unless --fresh says otherwise, so a new declaration never has to copy them by hand.
import { expect, test } from 'bun:test';
import { mkdtemp, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { declaredFor } from '../../gen/ledger';
import { declaration, declareCause, ledgerCli, readLedger } from '../drift';

const OLD = 'a'.repeat(64), NOW = 'b'.repeat(64);
const seed = async () => {
  const file = join(await mkdtemp(join(tmpdir(), 'drift-')), 'x.json');
  await writeFile(file, JSON.stringify({ recorded: { [OLD.slice(0, 16)]: 'a run' }, declared: { [OLD]: [{ cause: 'old cause', rules: ['P-6'] }, { cause: 'older cause', rules: [] }] } }));
  return file;
};

test('declaration reads the cause words, --rules and --fresh wherever they stand', () => {
  expect(declaration(['a', 'cause', '--rules', 'P-1,P-2'])).toEqual({ cause: 'a cause', rules: 'P-1,P-2', fresh: false });
  expect(declaration(['--fresh', 'a cause', '--rules', 'none'])).toEqual({ cause: 'a cause', rules: 'none', fresh: true });
  expect(declaration(['--rules', 'P-1', 'a cause', '--fresh'])).toEqual({ cause: 'a cause', rules: 'P-1', fresh: true });
});

test('declare carries the earlier causes forward, with a new one last; a second run changes nothing', async () => {
  const file = await seed(), quiet = console.log; console.log = () => {};
  try {
    await ledgerCli(['declare', 'new cause', '--rules', 'P-7'], file, NOW);
    const once = readLedger(file);
    expect(declaredFor(once, NOW)).toEqual([{ cause: 'old cause', rules: ['P-6'] }, { cause: 'older cause', rules: [] }, { cause: 'new cause', rules: ['P-7'] }]);
    expect(declaredFor(once, OLD).length).toBe(2); // the earlier hash is untouched
    await ledgerCli(['declare', 'new cause', '--rules', 'P-7'], file, NOW);
    expect(readLedger(file)).toEqual(once);
  } finally { console.log = quiet; }
});

test('declare --fresh declares only the new cause; declareCause carries only when asked', async () => {
  const file = await seed(), quiet = console.log; console.log = () => {};
  try {
    await ledgerCli(['declare', 'only this', '--rules', 'none', '--fresh'], file, NOW);
    expect(declaredFor(readLedger(file), NOW)).toEqual([{ cause: 'only this', rules: [] }]);
  } finally { console.log = quiet; }
  const other = await seed();
  declareCause(other, NOW, 'plain', ['P-1']);
  expect(declaredFor(readLedger(other), NOW)).toEqual([{ cause: 'plain', rules: ['P-1'] }]);
  declareCause(other, 'c'.repeat(64), 'carried', ['P-2'], true);
  expect(declaredFor(readLedger(other), 'c'.repeat(64)).map(c => c.cause)).toEqual(['old cause', 'older cause', 'plain', 'carried']);
});
