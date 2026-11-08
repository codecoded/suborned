import { codexCard, invokeCodex, tokenUsage } from '../goals/codex';
import { roleFiles } from './evidence';
import { hash } from './guide';
import { claudeJudge } from './live-claude';
import type { Authorization, Invoke } from './types';

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

/** A live run's callers and record from the CLI's options: Fable writes on Codex (`--model`); with `--judge-model` Motive's evaluations run on Claude (brief 102),
 *  so the record names both and archives Motive's Claude card too. */
export function liveSetup(options: Map<string, string>): { invoke: Invoke; authorization: Authorization; roles: string[] } {
  const model = options.get('--model') ?? '', approvedBy = options.get('--approved-by') ?? '', judgeModel = options.get('--judge-model');
  const writer = liveCaller(model, approvedBy);
  if (judgeModel === undefined) return { invoke: writer, authorization: { provider: 'codex', model, approvedBy }, roles: roleFiles(null) };
  const probe = Bun.spawnSync([process.env.CLAUDE_BIN ?? 'claude', '--version'], { stdout: 'pipe', stderr: 'pipe' });
  if (probe.exitCode !== 0) throw new Error('Claude CLI not found: install Claude Code and sign in (claude)');
  const judge = claudeJudge(judgeModel, approvedBy), authorization: Authorization = { provider: 'codex', model, approvedBy, judge: { provider: 'claude', model: judgeModel } };
  return { invoke: request => request.stage === 'evaluate' ? judge(request) : writer(request), authorization, roles: roleFiles(authorization) };
}
