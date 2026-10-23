import { codexCard, invokeCodex, tokenUsage } from '../goals/codex';
import { hash } from './guide';
import type { Invoke } from './types';

/** Each stage runs as its crew card through A5's isolated, structured Codex adapter: one tool-less call, no crew session. */
export function liveCaller(model: string, approvedBy: string): Invoke {
  if (process.env.SUBORNED_LIVE !== '1' || !approvedBy.trim()) throw new Error('Live runs need SUBORNED_LIVE=1 and --approved-by');
  if (!/^[A-Za-z0-9][A-Za-z0-9._:/-]*$/.test(model)) throw new Error('Supply an explicit --model');
  return async request => {
    const roleCard = await codexCard(request.role);
    const result = await invokeCodex(roleCard, request.prompt, request.schema, { model, effort: 'medium', timeoutMs: request.timeoutMs });
    let tokens: number | null = null;
    try { tokens = tokenUsage(result.usage); } catch { /* kept as unknown; the loop stops */ }
    // Failures and raw provider output are kept before the loop validates anything.
    return { output: result.ok ? result.output : { providerError: result.error ?? 'Model call failed' }, tokens,
      raw: { ...result, role: request.role, roleCard: { source: roleCard.source, sha256: hash(roleCard.text) }, requestedModel: model, approvedBy } };
  };
}
