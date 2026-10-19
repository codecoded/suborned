import { z } from 'zod';
import { runAgent, type Call } from '../rag/agent';
import { invokeClaude, isLive, liveGuard, type Spawn } from '../rag/claude';
import { estimateCost } from '../gen/estimate';
import { retrieve } from '../rag/retrieval';
import { codexCard, invokeCodex, tokenUsage } from './codex';

export class LimitReached extends Error {}
export type Limits = { maxUsd?: number; maxCalls: number; maxIterations: number; provider?: 'claude' | 'codex'; model?: string; maxTokens?: number; goal?: string };
export type Ask = <T>(kind: 'assess' | 'write' | 'review', index: number, role: string, task: string, schema: z.ZodType<T>) => Promise<T>;

export function remainingBudget(calls: Pick<Call, 'costUsd'>[], limits: Pick<Limits, 'maxCalls' | 'maxUsd'>): number {
  if (calls.length >= limits.maxCalls) throw new LimitReached('Model call limit reached');
  if (calls.some(c => c.costUsd === null || !Number.isFinite(c.costUsd) || c.costUsd < 0))
    throw new LimitReached('A model call returned unknown or invalid cost; no further calls are authorized');
  if (!Number.isFinite(limits.maxUsd) || limits.maxUsd! <= 0) throw new LimitReached('Missing valid dollar budget');
  const remaining = limits.maxUsd! - calls.reduce((n, c) => n + c.costUsd!, 0);
  if (remaining <= 0) throw new LimitReached('Model spend ceiling reached');
  return remaining;
}

/** Bind all calls to the index selected by bootstrap, even if source files change mid-run. */
export function pinnedRetriever(getCorpusHash: () => string | undefined, retriever: typeof retrieve = retrieve): typeof retrieve {
  let pinnedHash: string | undefined;
  return async (query, limit, pinned) => {
    const corpusHash = getCorpusHash();
    if (!corpusHash || !/^[a-f0-9]{64}$/.test(corpusHash)) throw new Error('Retrieval bootstrap must complete before any goal model call');
    if (pinnedHash && corpusHash !== pinnedHash) throw new Error('The run corpus changed after its first model call');
    pinnedHash = corpusHash;
    return retriever(query, limit, pinned, corpusHash);
  };
}

export function validateLimits(limits: Limits, approvedBy: string) {
  if (!approvedBy.trim()) throw new Error('A run must record who authorized its model calls (--approved-by).');
  if (limits.provider !== undefined && !['claude', 'codex'].includes(limits.provider)) throw new Error('Unknown model provider');
  if (limits.provider === 'codex') {
    if (limits.maxUsd !== undefined) throw new Error('Codex subscription usage does not expose dollar cost; use max-tokens and max-calls, not max-usd');
    if (!limits.model?.trim() || !/^[a-zA-Z0-9._-]+$/.test(limits.model)) throw new Error('Codex needs an explicit model ID');
    if (!Number.isSafeInteger(limits.maxTokens) || limits.maxTokens! < 1000 || limits.maxTokens! > 2000000) throw new Error('Codex max-tokens must be 1000..2000000');
  } else if (!Number.isFinite(limits.maxUsd) || limits.maxUsd! <= 0 || limits.maxUsd! > 20) throw new Error('max-usd must be > 0 and <= 20');
  if (limits.goal !== undefined && (!limits.goal.trim() || limits.goal.length > 2000)) throw new Error('goal must be nonempty and at most 2000 characters');
  if (!Number.isInteger(limits.maxIterations) || limits.maxIterations < 1 || limits.maxIterations > 5) throw new Error('max-iterations must be 1..5');
  if (!Number.isInteger(limits.maxCalls) || limits.maxCalls < 1 || limits.maxCalls > 20) throw new Error('max-calls must be 1..20');
}

export type CampaignAsk = <T>(id: string, role: string, brief: string, task: string, schema: z.ZodType<T>, queries: string[]) => Promise<T>;

/** The CLI still has only StructuredOutput. The coordinator owns writes and commands. */
export function campaignCaller(dir: string, calls: Call[], limits: Limits, getCorpusHash: () => string | undefined): CampaignAsk {
  if (limits.provider === 'codex') {
    if (process.env.SUBORNED_LIVE !== '1') throw new Error('Codex goal runs require SUBORNED_LIVE=1 and recorded approval');
    const retriever = pinnedRetriever(getCorpusHash);
    const bounded: typeof invokeClaude = async (card, prompt, schema, effort, images) => {
      if (images?.length) throw new Error('Codex goal calls are text-only');
      if (calls.length >= limits.maxCalls) throw new LimitReached('Model call limit reached');
      // Token threshold is checked between calls; one in-flight call can cross it.
      // Call count and the five-minute per-call timeout are hard bounds.
      let used = 0;
      try { used = calls.reduce((n, c) => n + tokenUsage(c.usage), 0); }
      catch { throw new LimitReached('Unknown Codex usage; no further calls authorized'); }
      if (used >= limits.maxTokens!) throw new LimitReached('Codex token threshold reached');
      return invokeCodex(card, prompt, schema, { model: limits.model!, effort, timeoutMs: 300000 });
    };
    return async (id, role, brief, task, schema, queries) => runAgent({ id, role, brief, query: queries, effort: 'medium' },
      task, schema, calls, dir, [], retriever, bounded, { provider: 'codex', loadCard: codexCard });
  }
  const guard = liveGuard();
  if (guard) throw new Error(guard);
  if (isLive() && process.env.SUBORNED_LIVE !== '1') throw new Error('Goal runs require explicit SUBORNED_LIVE=1 opt-in as well as recorded approval.');
  const retriever = pinnedRetriever(getCorpusHash);
  const bounded: typeof invokeClaude = async (card, prompt, schema, effort, images) => {
    const remaining = remainingBudget(calls, limits);
    const estimate = estimateCost({ chars: card.text.length + prompt.length + JSON.stringify(z.toJSONSchema(schema)).length, images: images?.length ?? 0, builder: card.name === 'simulation-engineer' });
    if (remaining < estimate.usd) throw new LimitReached(`Remaining budget $${remaining.toFixed(3)} is below estimated next call $${estimate.usd.toFixed(3)}`);
    const spawn: Spawn = (cmd, options) => {
      const child = Bun.spawn([...cmd, '--max-budget-usd', String(remaining)], { ...options, stdout: 'pipe', stderr: 'pipe' });
      const timer = setTimeout(() => child.kill('SIGKILL'), 300000);
      return { stdout: child.stdout, stderr: child.stderr, exited: child.exited.finally(() => clearTimeout(timer)) };
    };
    const result = await invokeClaude(card, prompt, schema, effort, images, spawn);
    if (result.costUsd === null || !Number.isFinite(result.costUsd) || result.costUsd < 0)
      return { ...result, ok: false, error: 'Unknown or invalid model cost; no further calls are authorized' };
    if (result.costUsd > remaining) return { ...result, ok: false, error: 'CLI reported spend above remaining ceiling; stopping' };
    return result;
  };
  return async (id, role, brief, task, schema, queries) => runAgent({
    id, role, brief, query: queries, effort: 'medium',
  }, task, schema, calls, dir, [], retriever, bounded);
}

/** Legacy fixed-scope adapter retained for the archived Assignment 5 experiment. */
export function modelCaller(dir: string, calls: Call[], limits: Limits, getCorpusHash: () => string | undefined = () => undefined): Ask {
  const ask = campaignCaller(dir, calls, limits, getCorpusHash);
  return (kind, index, role, task, schema) => ask(`${kind}-${index}`, role, `tools/goals/prompts/${kind}.md`, task, schema,
    ['GDD bodies container service round time learnable Watch Scout', 'deterministic simulation orders replay cancellation Watch']);
}
