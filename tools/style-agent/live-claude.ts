import { mkdtemp, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { isDeepStrictEqual } from 'node:util';
import { z } from 'zod';
import { claudeArgs, claudeEnv, cliSchema, interpretClaude, liveGuard, type Card } from '../rag/claude';
import { root } from '../rag/config';
import { hash } from './guide';
import type { Call, Invoke } from './types';

/** The loop's Claude judge (brief 102; Sky 2026-11-08, on Opus 5.5 judging the sweep: "we can try - sure"): Motive scores on Claude Opus through the subscription's
 *  `claude` CLI while Fable still writes on Codex. One tool-less call per evaluation: Motive's Claude crew card as the system prompt, structured output the only tool,
 *  no CLAUDE.md, no settings, hooks or MCP servers, in an empty working folder. The call is recorded like a Codex one (the CLI's own transcript, plus the launch
 *  record below), so `style:replay` and `style:verify` rebuild the run without a model. */
export const JUDGE_CARD = '.claude/agents/systems-designer.md';
/** GDD §14 Motive: Opus 5.5, effort medium. */
export const JUDGE_EFFORT = 'medium';
const AUDIT = 'suborned.claude.audit';
type Env = Record<string, string | undefined>;

/** A Claude crew card's body from its text, as tools/rag/claude.ts `card` reads it from disk. */
export function judgeCard(text: string, source = JUDGE_CARD): Card {
  const name = source.replace(/^\.claude\/agents\//, '').replace(/\.md$/, ''), m = text.match(/^---\n([\s\S]*?)\n---\n([\s\S]*)$/);
  if (!m || m[1]!.match(/^name: (.+)$/m)?.[1] !== name) throw new Error(`${source} is not the card for ${name}`);
  return { name, model: m[1]!.match(/^model: (.+)$/m)?.[1]!.trim() ?? 'inherit', body: m[2]!.trim(), source, text };
}

/** Every token the call processed, over every model the CLI used (Claude Code also runs a small helper model): input, cache writes, cache reads and output. */
export function claudeTokens(stdout: string): number {
  const result = stdout.split('\n').filter(Boolean).flatMap(l => { try { return [JSON.parse(l)]; } catch { return []; } }).findLast(e => e?.type === 'result');
  const per = result?.modelUsage as Record<string, Record<string, unknown>> | undefined;
  if (!per || typeof per !== 'object' || !Object.keys(per).length) throw new Error('Missing Claude model usage');
  let total = 0;
  for (const u of Object.values(per)) for (const k of ['inputTokens', 'outputTokens', 'cacheReadInputTokens', 'cacheCreationInputTokens']) {
    const n = u[k];
    if (typeof n !== 'number' || !Number.isSafeInteger(n) || n < 0) throw new Error(`Invalid Claude usage: ${k}`);
    total += n;
  }
  return total;
}
/** Tokens by model, for the run's report: the judge and the CLI's helper apart. */
export function claudeTokensByModel(stdout: string): Record<string, number> {
  const result = stdout.split('\n').filter(Boolean).flatMap(l => { try { return [JSON.parse(l)]; } catch { return []; } }).findLast(e => e?.type === 'result');
  return Object.fromEntries(Object.entries((result?.modelUsage ?? {}) as Record<string, Record<string, number>>)
    .map(([m, u]) => [m, (u.inputTokens ?? 0) + (u.outputTokens ?? 0) + (u.cacheReadInputTokens ?? 0) + (u.cacheCreationInputTokens ?? 0)]));
}

/** One transcript's answer, the same for the live call and the replay: the structured output, or the provider's error; the tokens, or null (the loop then stops). */
export function claudeAnswer(stdout: string, exitCode: number, timedOut: boolean, model: string): { output: unknown; tokens: number | null } {
  const parsed = interpretClaude(stdout, exitCode);
  const error = timedOut ? 'Claude call timed out' : !parsed.ok ? parsed.error ?? 'Model call failed'
    : parsed.model !== model ? `Claude reported model ${parsed.model}, not ${model}` : !parsed.modelsUsed.includes(model) ? `Claude's usage has no ${model}` : null;
  let tokens: number | null = null;
  try { tokens = claudeTokens(stdout); } catch { /* unknown usage stays unknown; the loop stops */ }
  return { output: error ? { providerError: error } : parsed.output, tokens };
}

export type ClaudeSpawn = (argv: string[], o: { cwd: string; env: Env; stdin: Blob }) => {
  stdout: ReadableStream<Uint8Array>; stderr: ReadableStream<Uint8Array>; exited: Promise<number>; kill: () => void;
};
const bunSpawn: ClaudeSpawn = (argv, o) => {
  const p = Bun.spawn(argv, { cwd: o.cwd, env: o.env, stdin: o.stdin, stdout: 'pipe', stderr: 'pipe' });
  return { stdout: p.stdout, stderr: p.stderr, exited: p.exited, kill: () => { p.kill('SIGKILL'); } };
};

/** Motive's evaluations on Claude. Live only with SUBORNED_LIVE=1 and a recorded approval, as the Codex caller; `spawn` and `env` exist for offline tests. */
export function claudeJudge(model: string, approvedBy: string, spawn: ClaudeSpawn = bunSpawn, env: Env = process.env): Invoke {
  if (env.SUBORNED_LIVE !== '1' || !approvedBy.trim()) throw new Error('Live runs need SUBORNED_LIVE=1 and --approved-by');
  if (!/^claude-[a-z0-9][a-z0-9.-]*$/.test(model)) throw new Error('Supply an explicit --judge-model, a Claude model id (claude-opus-5-5)');
  return async request => {
    if (request.role !== 'systems-designer' || request.stage !== 'evaluate') throw new Error('Only Motive\'s evaluations run on the Claude judge');
    const refused = liveGuard(env);
    if (refused) throw new Error(refused);
    const text = await Bun.file(resolve(root, JUDGE_CARD)).text(), card = judgeCard(text), schema = z.toJSONSchema(request.schema);
    const argv = claudeArgs(card, cliSchema(request.schema), JUDGE_EFFORT, model, false, env);
    const audit = { type: AUDIT, version: 1, promptSha256: hash(request.prompt), model, effort: JUDGE_EFFORT, timeoutMs: request.timeoutMs, argv, schema };
    const cwd = await mkdtemp(join(tmpdir(), 'suborned-claude-')); // no project settings, CLAUDE.md or files reach the call
    let stdout = '', stderr = '', exitCode = -1, timedOut = false;
    try {
      // no CLAUDE.md, and none of Claude Code's own side traffic: without it the CLI also runs a small helper model over the prompt (about 10k tokens a call)
      const proc = spawn(argv, { cwd, env: { ...claudeEnv(env), CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC: '1' }, stdin: new Blob([request.prompt]) });
      const timer = setTimeout(() => { timedOut = true; proc.kill(); }, request.timeoutMs);
      try { [stdout, stderr, exitCode] = await Promise.all([new Response(proc.stdout).text(), new Response(proc.stderr).text(), proc.exited]); }
      finally { clearTimeout(timer); }
    } finally { await rm(cwd, { recursive: true, force: true }); }
    const { output, tokens } = claudeAnswer(stdout, exitCode, timedOut, model);
    // the transcript is kept before the loop validates anything, failures too
    return { output, tokens, raw: { provider: 'claude', stdout, stderr, exitCode, timedOut, audit, role: request.role,
      roleCard: { source: JUDGE_CARD, sha256: hash(text) }, requestedModel: model, approvedBy, tokensByModel: claudeTokensByModel(stdout) } };
  };
}

const must = (ok: unknown, message: string): void => { if (!ok) throw new Error(message); };
/** A judged call is bound to its Claude transcript, the archived card, the schema, the model and the approval; its answer is read again from the transcript. */
export function verifyClaude(call: Call, files: Record<string, string>, judge: { model: string }, approvedBy: string): void {
  const raw = call.raw as Record<string, any> | undefined, audit = raw?.audit as Record<string, any> | undefined;
  must(raw && raw.provider === 'claude' && typeof raw.stdout === 'string' && Number.isInteger(raw.exitCode) && typeof raw.timedOut === 'boolean'
    && audit?.type === AUDIT && audit.version === 1 && Array.isArray(audit.argv), 'Missing Claude transcript');
  const text = files[JUDGE_CARD];
  must(text && call.stage === 'evaluate' && call.role === 'systems-designer' && raw!.role === call.role && raw!.roleCard?.source === JUDGE_CARD && raw!.roleCard?.sha256 === hash(text),
    'Role card differs from the archived card');
  must(raw!.requestedModel === judge.model && audit!.model === judge.model && raw!.approvedBy === approvedBy, 'Approval metadata mismatch');
  must(audit!.promptSha256 === hash(call.prompt) && audit!.timeoutMs === call.timeoutMs && audit!.effort === JUDGE_EFFORT, 'Prompt, effort or timeout mismatch');
  must(hash(JSON.stringify(audit!.schema)) === call.schemaSha256, 'Recorded schema mismatch');
  const { $schema: _, ...schema } = audit!.schema as Record<string, unknown>;
  const argv = claudeArgs(judgeCard(text!), JSON.stringify(schema), JUDGE_EFFORT, judge.model, false, { CLAUDE_BIN: audit!.argv[0] });
  must(isDeepStrictEqual(argv, audit!.argv), 'The call did not use the archived role card, schema, effort or model');
  const { output, tokens } = claudeAnswer(raw!.stdout, raw!.exitCode, raw!.timedOut, judge.model);
  must(isDeepStrictEqual(JSON.parse(JSON.stringify(output)), JSON.parse(JSON.stringify(call.output ?? null))) && tokens === call.tokens,
    'Transcript output or usage differs from the recorded call');
}
