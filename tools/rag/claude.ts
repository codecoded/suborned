// Headless crew call for build-time generation. The crew card (.claude/agents/<name>.md) is the system prompt and
// structured output is the only tool. `--agent <name>` is not used: the card's Glob and Grep survive `--tools`.
// Verified 2026-10-10 (CLI 2.1.295): init lists exactly ["StructuredOutput"] and no MCP servers.
import { readdir } from 'node:fs/promises';
import { resolve } from 'node:path';
import { z } from 'zod';
import { root } from './config';

export const allowedTools = ['StructuredOutput'];
export type Card = { name: string; model: string; body: string; source: string; text: string };

/** A card by agent name (`simulation-engineer`) or GDD name (`Keel`, matched in the description "… (Keel). Owns: …"). */
export async function card(who: string): Promise<Card> {
  const dir = resolve(root, '.claude/agents');
  let name = who;
  if (!(await Bun.file(resolve(dir, `${who}.md`)).exists())) {
    const tag = `(${who.toLowerCase()}). owns:`;
    for (const f of await readdir(dir)) if ((await Bun.file(resolve(dir, f)).text()).toLowerCase().includes(tag)) { name = f.replace(/\.md$/, ''); break; }
  }
  const source = `.claude/agents/${name}.md`, text = await Bun.file(resolve(root, source)).text();
  const m = text.match(/^---\n([\s\S]*?)\n---\n([\s\S]*)$/);
  if (!m || m[1]!.match(/^name: (.+)$/m)?.[1] !== name) throw new Error(`${source} is not the card for ${name}`);
  return { name, model: m[1]!.match(/^model: (.+)$/m)?.[1]!.trim() ?? 'inherit', body: m[2]!.trim(), source, text };
}

export const cliSchema = (schema: z.ZodType) => { const { $schema: _, ...rest } = z.toJSONSchema(schema) as Record<string, unknown>; return JSON.stringify(rest); };
export const claudeArgs = (c: Card, schemaJson: string, effort = 'medium', model = process.env.CLAUDE_MODEL ?? c.model) => [
  process.env.CLAUDE_BIN ?? 'claude', '-p', '--system-prompt', c.body, '--tools=',
  '--output-format', 'stream-json', '--verbose', '--json-schema', schemaJson, '--effort', effort, '--no-session-persistence',
  // Project settings only: no user hooks, plugins or MCP servers inside a generation call.
  '--setting-sources', 'project', '--strict-mcp-config',
  ...(model && model !== 'inherit' ? ['--model', model] : []),
];
/** No CLAUDE.md reaches a generation call: those are development instructions, not design sources. */
export const claudeEnv = (env: Record<string, string | undefined> = process.env) => ({ ...env, CLAUDE_CODE_DISABLE_CLAUDE_MDS: '1' });

export type ClaudeResult = { ok: boolean; error?: string; output?: unknown; usage: unknown; costUsd: number | null; model: string | null; modelsUsed: string[]; tools: string[] | null; cliVersion: string | null };
/** Interprets one stream-json transcript. Pure, so every failure path is testable without a model call. */
export function interpretClaude(stdout: string, exitCode: number): ClaudeResult {
  const events = stdout.split('\n').filter(Boolean).flatMap(line => { try { return [JSON.parse(line)]; } catch { return []; } });
  const init = events.find(e => e.type === 'system' && e.subtype === 'init'), result = events.findLast(e => e.type === 'result');
  const base = { usage: result?.usage ?? null, costUsd: result?.total_cost_usd ?? null, model: init?.model ?? null, modelsUsed: Object.keys(result?.modelUsage ?? {}), tools: init?.tools ?? null, cliVersion: init?.claude_code_version ?? null };
  const fail = (error: string) => ({ ...base, ok: false, error, output: result?.structured_output });
  if (!init) return fail('No init event; tool isolation unverified');
  const extra = (init.tools ?? []).filter((t: string) => !allowedTools.includes(t));
  if (extra.length) return fail('Generation call exposed unexpected tools: ' + extra.join(', '));
  if ((init.mcp_servers ?? []).length) return fail('Generation call connected MCP servers: ' + init.mcp_servers.map((s: { name: string }) => s.name).join(', '));
  if (!result) return fail('No result event (exit ' + exitCode + ')');
  if (exitCode !== 0) return fail('Claude exited ' + exitCode);
  if (result.is_error) return fail('Claude error ' + (result.subtype ?? '') + ': ' + String(result.result ?? '').slice(0, 300));
  if (result.structured_output === undefined) return fail('Missing structured_output');
  return { ...base, ok: true, output: result.structured_output };
}

export async function invokeClaude(c: Card, prompt: string, schema: z.ZodType, effort?: string) {
  const proc = Bun.spawn(claudeArgs(c, cliSchema(schema), effort), { cwd: root, env: claudeEnv(), stdin: new Blob([prompt]), stdout: 'pipe', stderr: 'pipe' });
  const [stdout, stderr, exitCode] = await Promise.all([new Response(proc.stdout).text(), new Response(proc.stderr).text(), proc.exited]);
  return { stdout, stderr, exitCode, ...interpretClaude(stdout, exitCode) };
}
