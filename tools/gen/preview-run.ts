// `bun run levels <objective> --preview` (brief 007 F): round 1 of a run with the real retrieval, tasks and apply step,
// and a stand-in for every model answer (preview.ts). It writes the prompts a real run would send, a table of their
// sizes and an estimate of the cost, and stops before any model call. It runs in a scratch folder, out/preview, never
// in runs/.
import { rm } from 'node:fs/promises';
import { resolve } from 'node:path';
import type { Call } from '../rag/agent';
import { root } from '../rag/config';
import { generate } from './loop';
import type { Opts, Plugin } from './plugin';
import { previewRows, previewText } from './preview';

export const PREVIEW_DIR = 'out/preview';

/**
 * Returns the table that was written beside the prompts (`<run>/preview.txt`). `where`: the scratch folder (a test
 * points it elsewhere).
 */
export async function runPreview<S>(
  p: Plugin<S>, o: Pick<Opts, 'input' | 'scope' | 'retriever' | 'retrieval'>, where = PREVIEW_DIR,
): Promise<string> {
  const dir = resolve(root, where), before = { runs: process.env.LEVELS_RUNS, out: process.env.LEVELS_OUT };
  await rm(dir, { recursive: true, force: true });
  // paths.ts reads these when it is asked
  process.env.LEVELS_RUNS = `${dir}/runs`; process.env.LEVELS_OUT = `${dir}/out`;
  try {
    const run = await generate(p, { ...o, rounds: 1, preview: true });
    const calls: Call[] = JSON.parse(await Bun.file(`${run.odir}/calls.json`).text());
    const text = previewText(await previewRows(calls), run.odir);
    await Bun.write(`${run.odir}/preview.txt`, text + '\n');
    return text;
  } finally {
    for (const [k, v] of [['LEVELS_RUNS', before.runs], ['LEVELS_OUT', before.out]] as const) {
      if (v === undefined) delete process.env[k]; else process.env[k] = v;
    }
  }
}
