// Prompt preview (brief 007 F): `bun run levels <objective> --preview` runs round 1 with the real retrieval, task
// texts, hand-offs and apply step, but every member answers with a stand-in instead of a model: the builders with the
// objective's empty proposal, the critic with a pass. Nothing that costs money is called. The prompts a real run would
// send are written under out/, with an estimate of tokens and cost.
// The first member's prompt is exact. A later member's prompt carries the stand-in where it would show an earlier
// member's proposal.
import { readFile } from 'node:fs/promises';
import type { Call } from '../rag/agent';
import type { invokeClaude } from '../rag/claude';
import { CHARS_PER_TOKEN, MODEL_PRICE, estimateCost, type Estimate } from './estimate';

export const PASS = { verdict: 'pass', summary: 'preview: no model was called', faults: [], sources: [] };

/** What the engine gets back from a call in a preview: the stand-in output, no cost, no model. */
export const previewInvoke = (output: unknown): typeof invokeClaude => async () => ({
  stdout: '', stderr: '', exitCode: 0, ok: true, output, usage: null, costUsd: 0, model: 'preview (no model call)',
  modelsUsed: [], tools: ['StructuredOutput'], cliVersion: null,
});

const read = (file: string) => readFile(file, 'utf8').catch(() => '');
export type Row = Estimate & { id: string; role: string; passages: number; images: number };

/** One row per recorded call: what it would send and what that is estimated to cost. */
export async function previewRows(calls: Call[]): Promise<Row[]> {
  return Promise.all(calls.map(async c => {
    const schemaFile = c.promptFile.replace('.prompt.txt', '.schema.json');
    const [prompt, card, schema] = await Promise.all([read(c.promptFile), read(c.card.file), read(schemaFile)]);
    const images = c.images?.length ?? 0, builder = !c.objective.endsWith('critic');
    const chars = prompt.length + card.length + schema.length;
    const cost = estimateCost({ chars, images, builder });
    return { id: c.id, role: c.role, passages: c.retrieval.hits.length, images, ...cost };
  }));
}

const usd = (n: number) => `$${n.toFixed(2)}`;
const pad = (n: number, w: number) => String(n).padStart(w);

/** The table the CLI prints. All figures are estimates; the price list is what experiment D's usage implies. */
export function previewText(rows: Row[], dir: string): string {
  const round = rows.reduce((t, r) => t + r.usd, 0);
  const prices = Object.entries(MODEL_PRICE).map(([k, v]) => `${k} $${v}/M`);
  const line = (r: Row) => [
    r.id.padEnd(26), `${pad(r.passages, 2)} passages`, `${r.images} images`, `${pad(r.chars, 6)} chars`,
    `in ~${pad(r.tokensIn, 6)} tok`, `out ~${pad(r.tokensOut, 5)} tok`, usd(r.usd),
  ].join('  ');
  return [
    `preview: ${rows.length} prompts written under ${dir}/calls/ (no model was called)`,
    ...rows.map(line),
    `round 1: about ${usd(round)}; each further round about ${usd(round * 0.9)} (a role's card and images are read from`
      + ` cache after its first call); three rounds at most ${usd(round * 3)}`,
    `estimate only (+-40%): ${prices.join(', ')} from the usage of experiment D; text ${CHARS_PER_TOKEN} chars/token`,
  ].join('\n');
}
