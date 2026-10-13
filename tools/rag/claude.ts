// Headless crew call for build-time generation. The crew card (.claude/agents/<name>.md) is the system prompt and
// structured output is the only tool. `--agent <name>` is not used: the card's Glob and Grep survive `--tools`.
// Verified 2026-10-10 (CLI 2.1.295): init lists exactly ["StructuredOutput"] and no MCP servers.
import { readdir } from 'node:fs/promises';
import { resolve } from 'node:path';
import { z } from 'zod';
import { root } from './config';
import type { LoadedImage } from './images';

export const allowedTools = ['StructuredOutput'];
/** The reasoning effort of a call that asks for none; the record of a call names the effort it ran with. */
export const DEFAULT_EFFORT = 'medium';
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
export const claudeArgs = (c: Card, schemaJson: string, effort = DEFAULT_EFFORT, model = process.env.CLAUDE_MODEL ?? c.model, withImages = false) => [
  process.env.CLAUDE_BIN ?? 'claude', '-p', '--system-prompt', c.body, '--tools=',
  ...(withImages ? ['--input-format', 'stream-json'] : []), // pictures travel as image blocks of one user message, which only the stream-json input takes
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

/** The one stream-json user message of a call with pictures: the images first, in order, then the prompt text. Pure, so the shape is testable without a model. */
export function userMessage(prompt: string, images: Pick<LoadedImage, 'mediaType' | 'data'>[]): string {
  const blocks = [...images.map(x => ({ type: 'image', source: { type: 'base64', media_type: x.mediaType, data: Buffer.from(x.data).toString('base64') } })), { type: 'text', text: prompt }];
  return JSON.stringify({ type: 'user', message: { role: 'user', content: blocks } }) + '\n';
}

type Env = Record<string, string | undefined>;

/** What `--version` of a stand-in prints. The real CLI never does, so this is how a stand-in proves it is one. */
export const STUB_MARK = 'suborned stub, no model';
const probed = new Map<string, boolean>();

/**
 * Is `bin` a stand-in that makes no model call? Asked of the binary itself (`<bin> --version`): a name or a path says
 * nothing, the real CLI can sit anywhere.
 */
export function isStub(bin: string | undefined): boolean {
  if (!bin) return false;
  const known = probed.get(bin);
  if (known !== undefined) return known;
  let stub = false;
  try {
    const p = Bun.spawnSync([bin, '--version'], { stdout: 'pipe', stderr: 'ignore', timeout: 10000 });
    stub = p.exitCode === 0 && p.stdout.toString().includes(STUB_MARK);
  } catch { stub = false; }
  probed.set(bin, stub);
  return stub;
}

/** A run that would reach the real model: CLAUDE_BIN is not a stand-in. */
export const isLive = (env: Env = process.env): boolean => !isStub(env.CLAUDE_BIN);

/**
 * Why a live model call must not start from this environment, or undefined when it may. An agent session (CLAUDECODE is
 * set) must not spend money by accident: it needs a stand-in as CLAUDE_BIN or Sky's explicit opt-in (set by Sky, or by
 * Aster with Sky's go-ahead; the variable is read below). The message names no way round it. A person in a terminal has
 * no CLAUDECODE and is unaffected.
 */
export function liveGuard(env: Env = process.env): string | undefined {
  if (!env.CLAUDECODE || env.SUBORNED_LIVE === '1' || isStub(env.CLAUDE_BIN)) return undefined;
  return 'refusing a live model call from an agent session: it would spend paid credits. A live run needs Sky\'s '
    + 'approval and is started by Aster with Sky\'s go-ahead. For an offline run, CLAUDE_BIN must be a stand-in that '
    + 'says so (tools/levels/test/claude-stub.ts).';
}

type Proc = { stdout: ReadableStream<Uint8Array>; stderr: ReadableStream<Uint8Array>; exited: Promise<number> };
type SpawnOpts = { cwd: string; env: Env; stdin: Blob };
/** How the CLI is started; injectable, so a test checks the guard without any real process. */
export type Spawn = (cmd: string[], opts: SpawnOpts) => Proc;
const bunSpawn: Spawn = (cmd, o) => Bun.spawn(cmd, { ...o, stdout: 'pipe', stderr: 'pipe' });

export async function invokeClaude(
  c: Card, prompt: string, schema: z.ZodType, effort?: string, images: LoadedImage[] = [],
  spawn: Spawn = bunSpawn, env: Env = process.env,
) {
  const refused = liveGuard(env);
  if (refused) throw new Error(refused);
  const stdin = images.length ? userMessage(prompt, images) : prompt;
  const args = claudeArgs(c, cliSchema(schema), effort, undefined, images.length > 0);
  const proc = spawn(args, { cwd: root, env: claudeEnv(), stdin: new Blob([stdin]) });
  const text = (s: ReadableStream<Uint8Array>) => new Response(s).text();
  const [stdout, stderr, exitCode] = await Promise.all([text(proc.stdout), text(proc.stderr), proc.exited]);
  return { stdout, stderr, exitCode, ...interpretClaude(stdout, exitCode) };
}
