// usage: bun tools/rag/experiment/image-vs-digest/run.ts --arm image|digest      (PAID: one Datum call per arm, no tools, no retrieval)
// Writes the full call records to out/review/007F-image-vs-digest/<arm>/ and the small evidence (output + stripped call record) to briefs/007/image-vs-digest/.
// The image arm sends the two concept sheets as image blocks; the call record keeps their sha256 only, so the run replays offline without them.
import { createHash } from 'node:crypto';
import { resolve } from 'node:path';
import { root, writeJson } from '../../config';
import { runAgent, type Call, type Objective } from '../../agent';
import type { Retrieval } from '../../retrieval';
import { ARMS, BRIEF_FILE, DIGEST_FILE, Description, EVIDENCE, SHEETS, WORK, task, type Arm } from './spec';

/** No passages: the sheets are the only source, so what the answer holds comes from them (or from the model's habits, which notDecidable should expose). */
const noPassages = async (q: string | string[]) => {
  const queries = [q].flat();
  return {
    query: queries.join(' ‖ '), queries, pinned: [], method: 'none: experiment, the concept sheets are the only source',
    corpusHash: 'none', embeddingModel: 'none', hits: [],
  } as unknown as Retrieval;
};

const strip = async (arm: Arm, c: Call) => {
  const prompt = await Bun.file(resolve(root, c.promptFile)).bytes();
  return {
    arm, id: c.id, role: c.role, model: c.model, modelsUsed: c.modelsUsed, tools: c.tools, cliVersion: c.cliVersion,
    card: { source: c.card.source, sha256: c.card.sourceHash },
    status: c.status, error: c.error, attempt: c.attempt, seconds: c.seconds, costUsd: c.costUsd, usage: c.usage, images: c.images ?? [],
    retrieval: { method: c.retrieval.method, hits: c.retrieval.hits.length },
    prompt: { bytes: prompt.length, sha256: createHash('sha256').update(prompt).digest('hex') },
  };
};

const arm = process.argv[process.argv.indexOf('--arm') + 1] as Arm;
if (!ARMS.includes(arm)) { console.error(`usage: --arm ${ARMS.join('|')}`); process.exit(2); }

const digest = await Bun.file(resolve(root, DIGEST_FILE)).text();
const objective: Objective = {
  id: `skyline-grammar-${arm}`, role: 'world-planner', brief: BRIEF_FILE, query: 'skyline grammar concept sheets', effort: 'medium',
  ...(arm === 'image' ? { images: SHEETS } : {}),
};
const calls: Call[] = [];
const output = await runAgent(objective, task(arm, digest), Description, calls, `${WORK}/${arm}`, [], noPassages);

const kept = (await Bun.file(resolve(root, `${EVIDENCE}/calls.json`)).json().catch(() => [])) as { arm: Arm }[];
await writeJson(`${EVIDENCE}/${arm}.output.json`, output);
await writeJson(`${EVIDENCE}/calls.json`, [...kept.filter(k => k.arm !== arm), ...(await Promise.all(calls.map(c => strip(arm, c))))]);
const spent = calls.reduce((sum, c) => sum + (c.costUsd ?? 0), 0);
console.log(`${arm}: ${calls.length} call(s), $${spent.toFixed(4)}, tools ${JSON.stringify(calls.at(-1)?.tools)}`);
