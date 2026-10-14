// The transit run's checker (through the CLI) and its replay (in process) must judge the same rules, the world rules included, on the
// kit digest and the civic look the run pinned (brief 007 F review, M1: 93 recorded against 47 rerun; then the CLI skipped the world rules,
// so a candidate that broke a world rule was accepted).
import { expect, setDefaultTimeout, test } from 'bun:test';
import { mkdtemp } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { root } from '../../rag/config';
import { inProcess, viaCli } from '../checker';
import { KIT, PLAN } from '../skyline/test/edits';

setDefaultTimeout(240000); // the world is generated twice, in the CLI and in process

test('a transit candidate that breaks a world rule fails the CLI, and the in-process rerun lists the same rules with the same results, on the pinned kit and look', async () => {
  const dir = await mkdtemp(join(tmpdir(), 'checker-')), report = join(dir, 'checks.json'), file = join(dir, 'plan.json');
  const blockFile = resolve(root, 'contracts/world/civic-block.json');
  const plan = await Bun.file(PLAN).json(), block = await Bun.file(blockFile).json();
  plan.plots[0].use = 'no-such-use'; // B-1, a world rule (world-rules.test.ts): no plan rule reads a plot's use
  await Bun.write(file, JSON.stringify(plan));
  // the run's pinned look: the live one with the dE gate one step easier. CD-4 states its gate in its text, so the report says which look it was built from.
  const live = await Bun.file(resolve(root, 'contracts/world/civic-look.json')).json(), pinnedLook = join(dir, 'civic-look.json'), gate = live.palette.minDE - 1;
  await Bun.write(pinnedLook, JSON.stringify({ ...live, palette: { ...live.palette, minDE: gate } }));
  const pin = { kit: KIT, look: pinnedLook };
  const code = await viaCli(file, join(dir, 'sheet.html'), report, undefined, blockFile, pin);
  const recorded: { checks: { id: string; ok: boolean; rule: string }[] } = await Bun.file(report).json();
  const rerun = await inProcess(plan, true, block, { world: true, ...pin }), planOnly = await inProcess(plan, true, block);
  expect(recorded.checks.find(c => c.id === 'CD-4')?.rule, 'the CLI built the world from the pinned look').toContain(`at least ${gate} dE2000`);
  expect(recorded.checks.filter(c => !c.ok).map(c => c.id)).toContain('B-1');
  expect(code).toBe(1);
  expect(recorded.checks).toEqual(JSON.parse(JSON.stringify(rerun.checks)));
  expect(planOnly.checks.every(c => c.ok)).toBe(true); // the plan rules alone, which the transit checker ran before, called it clean
  expect(planOnly.checks.length).toBeLessThan(recorded.checks.length);
});
