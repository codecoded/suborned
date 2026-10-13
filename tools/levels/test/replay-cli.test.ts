// `bun run levels:replay` on a run that never finished (brief 007 F review, P4): exit 3, "NOT COMPLETED", no failure.
// `bun run verify` reads exit 3 as "listed, not replayed".
import { expect, test } from 'bun:test';
import { mkdir, mkdtemp } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { root } from '../../rag/config';

async function replayCli(runs: string, id: string) {
  const p = Bun.spawn(['bun', 'tools/levels/replay.ts', id], {
    cwd: root, env: { ...process.env, LEVELS_RUNS: runs }, stdout: 'pipe', stderr: 'pipe',
  });
  const [stdout, code] = await Promise.all([new Response(p.stdout).text(), p.exited]);
  return { stdout, code };
}

test('an ERROR run and a folder with no run.json exit 3 and say why; neither is a failure', async () => {
  const runs = await mkdtemp(join(tmpdir(), 'replay-cli-')), dir = join(runs, 'harbour-district');
  await mkdir(join(dir, 'r001'), { recursive: true });
  await mkdir(join(dir, 'r002'), { recursive: true });
  await Bun.write(join(dir, 'r002', 'run.json'), JSON.stringify({
    objective: 'transit', outcome: 'ERROR', error: 'Error: the model went away', rounds: [],
  }));
  const none = await replayCli(runs, 'r001');
  expect(none.code).toBe(3); expect(none.stdout).toContain('NOT COMPLETED harbour-district/r001: no run.json');
  const error = await replayCli(runs, 'r002');
  expect(error.code).toBe(3); expect(error.stdout).toContain('outcome ERROR: Error: the model went away');
  expect(error.stdout).not.toContain('REPLAY FAILED');
});
