// What a plan objective pins into its run, and what replay re-checks: the input plan as the exact text the run read, and the civic block it names.
// Shared by every plan objective (transit now, skyline next); an objective with more inputs pins them beside these.
import { createHash } from 'node:crypto';
import { resolve } from 'node:path';
import { root, writeJson } from '../rag/config';
import { hash } from '../rag/corpus';
import type { Check, Pins } from '../gen/plugin';

/** Reads the plan: its id and version name the run before anything is written. */
export async function readPlan(path: string) {
  const raw = await Bun.file(resolve(root, path)).text(), input = JSON.parse(raw);
  return { path, raw, input, id: input.id as string, version: input.version as string };
}

/** Writes input.plan.raw.json, input.plan.json and input.block.json into the run; returns the parsed block, its file and the fields run.json records. */
export async function pinPlan(plan: Awaited<ReturnType<typeof readPlan>>, rdir: string) {
  const { path, raw, input } = plan;
  await Bun.write(`${rdir}/input.plan.raw.json`, raw); await writeJson(`${rdir}/input.plan.json`, input);
  // pin the civic block the input plan names: exact bytes, so every check in this run (and its replay) sees the same block
  const blockSrc: string = input.civic.block, blockBytes = new Uint8Array(await Bun.file(resolve(root, blockSrc)).arrayBuffer()), blockFile = `${rdir}/input.block.json`;
  await Bun.write(blockFile, blockBytes);
  const inputBlock = { source: blockSrc, sha256: createHash('sha256').update(blockBytes).digest('hex') }, block = JSON.parse(new TextDecoder().decode(blockBytes));
  return { block, blockFile, record: { inputPlan: { version: input.version, hash: hash(JSON.stringify(input)), rawHash: hash(raw), source: path }, inputBlock } };
}

/** Replay, first: the recorded raw plan text matches its hash; it becomes the corpus's plan text (later edits to the plan must not break replay). */
export async function checkRawPlan(c: Check): Promise<Pins> {
  const raw = await Bun.file(`${c.dir}/input.plan.raw.json`).text().catch(() => undefined);
  if (raw === undefined) c.fail('input.plan.raw.json is missing'); else hash(raw) === c.run.inputPlan?.rawHash ? c.ok('raw input plan hash') : c.fail('input.plan.raw.json does not match the recorded raw hash');
  return { raw, overlay: raw === undefined ? {} : { [c.run.inputPlan?.source ?? '']: raw } };
}

/** Replay: the parsed input plan matches its hash and the raw text. */
export async function checkInputPlan(c: Check, p: Pins): Promise<Pins> {
  const input = await c.read('input.plan.json');
  if (input) hash(JSON.stringify(input)) === c.run.inputPlan?.hash ? c.ok('input plan hash') : c.fail('input.plan.json does not match the recorded input hash');
  if (input && p.raw !== undefined) { try { Bun.deepEquals(JSON.parse(p.raw), input) ? c.ok('input.plan.json = the raw input plan') : c.fail('input.plan.json differs from input.plan.raw.json'); } catch { c.fail('input.plan.raw.json is not JSON'); } }
  return { input };
}

/** Replay: the pinned block matches its sha256; a run with no record predates pinning and cannot be checked against today's block. */
export async function checkBlock(c: Check): Promise<Pins> {
  let block: unknown, superseded = false;
  const blockBytes = c.run.inputBlock ? await Bun.file(`${c.dir}/input.block.json`).bytes().catch(() => undefined) : undefined;
  if (!c.run.inputBlock) { superseded = true; const m = 'superseded: recorded against the Oct 5 block, replays at commit 3dbb9ca'; if (c.allow) c.warn(m); else c.fail(m); }
  else if (!blockBytes) c.fail('input.block.json is missing');
  else if (createHash('sha256').update(blockBytes).digest('hex') !== c.run.inputBlock.sha256) c.fail('input.block.json does not match the recorded sha256');
  else { c.ok(`input block ${c.run.inputBlock.sha256.slice(0, 12)}`); block = JSON.parse(new TextDecoder().decode(blockBytes)); }
  return { block, superseded };
}
