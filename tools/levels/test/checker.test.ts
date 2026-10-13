// The transit run's checker (through the CLI) and its replay (in process) must judge the same rules, on a plan with
// world standards too (brief 007 F review, M1: 93 recorded against 47 rerun).
import { expect, setDefaultTimeout, test } from 'bun:test';
import { mkdtemp } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { root } from '../../rag/config';
import { inProcess, viaCli } from '../checker';
import { PLAN } from '../skyline/test/edits';

setDefaultTimeout(120000);

test('the CLI report of a plan with world standards lists the rules the in-process rerun lists', async () => {
  const dir = await mkdtemp(join(tmpdir(), 'checker-')), report = join(dir, 'checks.json');
  const blockFile = resolve(root, 'contracts/world/civic-block.json');
  await viaCli(PLAN, join(dir, 'sheet.html'), report, undefined, blockFile);
  const recorded: { checks: { id: string }[] } = await Bun.file(report).json();
  const plan = await Bun.file(PLAN).json(), block = await Bun.file(blockFile).json();
  const rerun = await inProcess(plan, true, block);
  expect(recorded.checks.length).toBeGreaterThan(0);
  expect(recorded.checks).toEqual(JSON.parse(JSON.stringify(rerun.checks)));
});
