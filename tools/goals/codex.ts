// Standalone structured generation; the coordinator owns every read, write and command.
// Written against codex-cli 0.163.0 exec --help / features list (2026-10-19); any installed version runs, and the audit records which.
// Sources: https://developers.openai.com/codex/config-reference
// https://developers.openai.com/codex/noninteractive
// https://developers.openai.com/cookbook/examples/codex/build_iterative_repair_loops_with_codex
import { mkdir, mkdtemp, readdir, rm } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { isDeepStrictEqual } from 'node:util';
import { createHash } from 'node:crypto';
import { z } from 'zod';
import { root } from '../rag/config';
import type { Card, ClaudeResult } from '../rag/claude';

/** The CLI version this adapter was written against; tests use it as their recorded version. */
export const CODEX_VERSION = '0.163.0';
const AUDIT = 'suborned.codex.audit', CAPTURE = 'suborned.codex.capture';
const MODEL_SOURCE = 'explicit --model; not server-attested';
type Env = Record<string, string | undefined>;
type JsonObject = Record<string, any>;
const object = (x: unknown): x is JsonObject => x !== null && typeof x === 'object' && !Array.isArray(x);
const digest = (text: string) => createHash('sha256').update(text).digest('hex');

/** Card bytes and parsed developer instructions are preserved; model selection is always explicit at invocation. */
export async function codexCard(who: string): Promise<Card> {
  if (!/^[a-z][a-z0-9-]*$/i.test(who)) throw new Error('Invalid Codex role name');
  const dir = resolve(root, '.codex/agents');
  for (const file of (await readdir(dir)).filter(f => f.endsWith('.toml')).sort()) {
    const text = await Bun.file(resolve(dir, file)).text();
    const parsed = Bun.TOML.parse(text) as Record<string, unknown>;
    const name = file.slice(0, -5);
    if (name !== who && !(typeof parsed.description === 'string' && parsed.description.toLowerCase().includes(`(${who.toLowerCase()}). owns:`))) continue;
    if (parsed.name !== name || typeof parsed.developer_instructions !== 'string' || !parsed.developer_instructions.trim())
      throw new Error(`Invalid Codex card: ${file}`);
    return { name, model: 'inherit', body: parsed.developer_instructions, source: `.codex/agents/${file}`, text };
  }
  throw new Error(`Unknown Codex role: ${who}`);
}

/** Cached tokens are a subset of input tokens, never added a second time. */
export function tokenUsage(usage: unknown): number {
  if (!object(usage)) throw new Error('Missing or malformed Codex usage');
  for (const key of ['input_tokens', 'output_tokens', ...('cached_input_tokens' in usage ? ['cached_input_tokens'] : [])])
    if (!Number.isSafeInteger(usage[key]) || usage[key] < 0) throw new Error(`Invalid Codex usage: ${key}`);
  if (usage.cached_input_tokens > usage.input_tokens) throw new Error('Cached input exceeds input tokens');
  const total = usage.input_tokens + usage.output_tokens;
  if (!Number.isSafeInteger(total)) throw new Error('Codex token total overflows');
  return total;
}

// Use supported feature flags, not removed apply_patch_freeform / js_repl switches.
// 0.163.0 normalizes unified_exec to true even after an explicit false override;
// shell_tool is the configurable shell gate. We do not claim the backend itself is disabled.
const DISABLED = [
  'apps', 'plugins', 'remote_plugin', 'recommended_plugins', 'hooks', 'memories',
  'multi_agent', 'multi_agent_v2', 'multi_agent_v2_dynamic_tools', 'agent_message_board',
  'shell_tool', 'shell_snapshot', 'code_mode', 'code_mode_host', 'code_mode_only',
  'code_mode_tool_search', 'skill_search', 'skill_mcp_dependency_install', 'view_image', 'image_generation',
  'browser_use', 'browser_use_external', 'computer_use', 'in_app_browser', 'in_app_local_automation',
  'sleep_tool', 'goals', 'tool_suggest', 'request_permissions_tool', 'default_mode_request_user_input',
  'standalone_web_search', 'step_model_switching', 'daemon_auto_start',
];

export type CodexOptions = { model: string; effort?: string; timeoutMs?: number };
function checked(options: CodexOptions) {
  if (!options.model || options.model === 'inherit' || !/^[a-zA-Z0-9][a-zA-Z0-9._:/-]*$/.test(options.model))
    throw new Error('Codex requires an explicit model identifier');
  const effort = options.effort ?? 'medium', timeoutMs = options.timeoutMs ?? 300000;
  if (!['minimal', 'low', 'medium', 'high', 'xhigh'].includes(effort)) throw new Error('Unsupported Codex reasoning effort');
  if (!Number.isSafeInteger(timeoutMs) || timeoutMs <= 0 || timeoutMs > 300000) throw new Error('Codex timeout must be 1..300000 ms');
  return { model: options.model, effort, timeoutMs };
}

export function codexConfig(c: Pick<Card, 'body'>, options: CodexOptions): string[] {
  const { effort } = checked(options);
  return [
    'model_provider="openai"', 'forced_login_method="chatgpt"', 'approval_policy="never"',
    'web_search="disabled"', 'mcp_servers={}', 'agents.enabled=false',
    'project_doc_max_bytes=0', 'project_doc_fallback_filenames=[]',
    'skills.include_instructions=false', 'skills.bundled.enabled=false', 'features.skip_host_skill_discovery=true',
    'memories.generate_memories=false', 'memories.use_memories=false', 'memories.dedicated_tools=false',
    'tools.update_plan.enabled=false', 'tools.experimental_request_user_input.enabled=false',
    'include_apps_instructions=false', 'include_collaboration_mode_instructions=false',
    'shell_environment_policy.inherit="none"', 'allow_login_shell=false',
    ...DISABLED.map(f => `features.${f}=false`),
    `model_reasoning_effort=${JSON.stringify(effort)}`, `developer_instructions=${JSON.stringify(c.body)}`,
  ];
}

/** Array argv only; no shell interpolation. Files are siblings of the empty working directory. */
export function codexArgs(c: Pick<Card, 'body'>, schemaFile: string, lastMessageFile: string, options: CodexOptions, bin = 'codex'): string[] {
  checked(options);
  return [bin, 'exec', '--json', '--ephemeral', '--ignore-user-config', '--sandbox', 'read-only',
    '--skip-git-repo-check', '--output-schema', schemaFile, '--output-last-message', lastMessageFile,
    '--model', options.model, ...codexConfig(c, options).flatMap(v => ['-c', v]), '-'];
}

/** Allow only runtime/auth-location and transport settings. Inherit neither API keys nor injected MCP/config endpoints. */
export function codexEnv(env: Env = process.env): Env {
  const allowed = ['PATH', 'HOME', 'USER', 'LOGNAME', 'SHELL', 'LANG', 'LC_ALL', 'TMPDIR', 'CODEX_HOME',
    'HTTP_PROXY', 'HTTPS_PROXY', 'ALL_PROXY', 'NO_PROXY', 'http_proxy', 'https_proxy', 'all_proxy', 'no_proxy',
    'SSL_CERT_FILE', 'SSL_CERT_DIR', 'NODE_EXTRA_CA_CERTS'];
  return Object.fromEntries(allowed.filter(k => env[k] !== undefined).map(k => [k, env[k]]));
}

/** Strict output schemas require every property. Optional properties travel as nullable, then are restored to absence.
 * This bounded adapter handles the object/array schemas used by goals; optional fields below unions/refs are rejected.
 * Parent validation remains authoritative; no model result is silently coerced to a schema-valid value.
 */
export function codexOutputSchema(source: JsonObject): JsonObject {
  const visit = (node: any, ambiguous = false): any => {
    if (!object(node)) return node;
    const out = { ...node };
    delete out.$schema;
    if (object(node.properties)) {
      const required: string[] = node.required ?? [];
      out.properties = Object.fromEntries(Object.entries(node.properties).map(([key, value]) => {
        const child = visit(value, ambiguous);
        if (required.includes(key)) return [key, child];
        if (ambiguous) throw new Error('Optional fields under unions/definitions are unsupported by the Codex adapter');
        return [key, { anyOf: [child, { type: 'null' }] }];
      }));
      out.required = Object.keys(node.properties);
      if (node.additionalProperties !== false) throw new Error('Codex output objects must be strict');
    }
    if (object(node.items)) out.items = visit(node.items, ambiguous);
    for (const key of ['anyOf', 'oneOf', 'allOf']) if (Array.isArray(node[key])) out[key] = node[key].map((n: any) => visit(n, true));
    if (object(node.$defs)) out.$defs = Object.fromEntries(Object.entries(node.$defs).map(([k, v]) => [k, visit(v, true)]));
    return out;
  };
  return visit(source);
}

function nullable(schema: any): boolean {
  return object(schema) && (schema.type === 'null' || (Array.isArray(schema.type) && schema.type.includes('null'))
    || (Array.isArray(schema.anyOf) && schema.anyOf.some(nullable)));
}
function normalize(value: any, schema: any): any {
  if (!object(schema)) return value;
  if (Array.isArray(value) && object(schema.items)) return value.map(v => normalize(v, schema.items));
  if (!object(value) || !object(schema.properties)) return value;
  return Object.fromEntries(Object.entries(value).flatMap(([key, val]) => {
    const prop = schema.properties[key];
    if (prop && val === null && !(schema.required ?? []).includes(key) && !nullable(prop)) return [];
    return [[key, prop ? normalize(val, prop) : val]];
  }));
}

const AuditSchema = z.object({
  type: z.literal(AUDIT), version: z.union([z.literal(1), z.literal(2)]), cliVersion: z.string().regex(/^\d+\.\d+\.\d+/),
  promptSha256: z.string().regex(/^[a-f0-9]{64}$/).optional(),
  model: z.string().min(1), modelSource: z.literal(MODEL_SOURCE), effort: z.string(),
  timeoutMs: z.number().int().positive().max(300000), cwd: z.string(), argv: z.array(z.string()),
  config: z.array(z.string()), schema: z.record(z.string(), z.unknown()), outputSchema: z.record(z.string(), z.unknown()),
}).strict();
const CaptureSchema = z.object({ type: z.literal(CAPTURE), exitCode: z.number().int(), timedOut: z.boolean(),
  lastMessage: z.string().nullable() }).strict();
const modelFields = { model: z.string().optional(), cli_version: z.string().optional() };
const ThreadSchema = z.object({ type: z.literal('thread.started'), thread_id: z.string().min(1), ...modelFields }).strict();
const StartSchema = z.object({ type: z.literal('turn.started'), ...modelFields }).strict();
const EndSchema = z.object({ type: z.literal('turn.completed'), usage: z.record(z.string(), z.unknown()), ...modelFields }).strict();
const ItemSchema = z.object({ type: z.enum(['item.started', 'item.updated', 'item.completed']), item: z.object({
  id: z.string().min(1), type: z.enum(['reasoning', 'agent_message']), text: z.string(),
}).strict() }).strict();
// CLI 0.163.0 emits these nonfatal configuration diagnostics as error items before
// turn.started, even with code execution disabled. Preserve them in the transcript;
// accept only these exact diagnostics in that startup position, never runtime errors.
const StartupDiagnosticSchema = z.object({ type: z.literal('item.completed'), item: z.object({
  id: z.string().min(1), type: z.literal('error'), message: z.string(),
}).strict() }).strict();
function knownStartupDiagnostic(message: string): boolean {
  return message === 'Code Mode is unavailable because code-mode host is disabled. Code mode will fail closed; enable `features.code_mode_host` and install `codex-code-mode-host`.'
    || /^Under-development features enabled: skip_host_skill_discovery\. Under-development features are incomplete and may behave unpredictably\. To suppress this warning, set `suppress_unstable_features_warning = true` in \/[^\r\n]+\/config\.toml\.$/.test(message);
}

/** Offline replay uses this same parser. Audit metadata is launcher evidence, not a server attestation or tool inventory.
 * `tools` means observed execution types (empty on success); unknown events/items fail closed.
 */
export function interpretCodex(stdout: string, exitCode: number, expectedModel?: string, expectedPromptSha256?: string): ClaudeResult {
  const base: ClaudeResult = { ok: false, usage: null, costUsd: null, model: null, modelsUsed: [], tools: null, cliVersion: null };
  try {
    const events: unknown[] = stdout.split('\n').filter(l => l.trim()).map(l => JSON.parse(l));
    const audit = AuditSchema.parse(events.shift());
    if (audit.version === 2 && !audit.promptSha256) throw new Error('Missing Codex invocation prompt hash');
    if (expectedPromptSha256 !== undefined && (audit.version !== 2 || audit.promptSha256 !== expectedPromptSha256)) throw new Error('Codex invocation prompt hash mismatch');
    const options = checked(audit), configBody = audit.config.at(-1);
    if (!configBody?.startsWith('developer_instructions=')) throw new Error('Missing audited developer instructions');
    const body = JSON.parse(configBody.slice('developer_instructions='.length));
    if (typeof body !== 'string') throw new Error('Malformed audited developer instructions');
    if (!/^\/(?:private\/)?tmp\/suborned-codex-[^/]+\/work$/.test(audit.cwd)) throw new Error('Codex cwd is not isolated');
    const temp = dirname(audit.cwd);
    const args = codexArgs({ body }, resolve(temp, 'schema.json'), resolve(temp, 'last-message.txt'), options, audit.argv[0]);
    if (!isDeepStrictEqual(args, audit.argv) || !isDeepStrictEqual(codexConfig({ body }, options), audit.config))
      throw new Error('Codex audit argv/config does not match the isolated invocation');
    if (!isDeepStrictEqual(codexOutputSchema(audit.schema), audit.outputSchema)) throw new Error('Codex audit schema mismatch');
    base.model = audit.model; base.cliVersion = audit.cliVersion;
    // modelsUsed is reserved for identities actually reported by CLI events.
    if (expectedModel !== undefined && expectedModel !== audit.model) throw new Error('Codex model mismatch');
    const capture = CaptureSchema.parse(events.pop());
    if (capture.exitCode !== exitCode) throw new Error('Codex exit code disagrees with capture');
    let phase = 0, last: string | undefined;
    const completedIds = new Set<string>();
    const pendingIds = new Map<string, string>();
    for (const raw of events) {
      if (!object(raw)) throw new Error('Malformed Codex event');
      if (raw.model !== undefined) {
        if (raw.model !== audit.model) throw new Error('Codex reported model mismatch');
        base.modelsUsed = [raw.model];
      }
      if (raw.cli_version !== undefined && raw.cli_version !== audit.cliVersion) throw new Error('Codex reported CLI version mismatch');
      if (raw.type === 'thread.started' && phase === 0) { ThreadSchema.parse(raw); phase = 1; }
      else if (raw.type === 'item.completed' && phase === 1 && raw.item?.type === 'error') {
        const diagnostic = StartupDiagnosticSchema.parse(raw);
        if (!knownStartupDiagnostic(diagnostic.item.message) || completedIds.has(diagnostic.item.id)) throw new Error('Unknown or duplicate Codex startup error');
        completedIds.add(diagnostic.item.id);
      }
      else if (raw.type === 'turn.started' && phase === 1) { StartSchema.parse(raw); phase = 2; }
      else if (['item.started', 'item.updated', 'item.completed'].includes(raw.type) && phase === 2) {
        if (!['reasoning', 'agent_message'].includes(raw.item?.type)) {
          base.tools = [String(raw.item?.type ?? 'unknown')];
          throw new Error(`Unexpected Codex tool/item: ${raw.item?.type ?? 'unknown'}`);
        }
        const e = ItemSchema.parse(raw);
        if (completedIds.has(e.item.id)) throw new Error('Duplicate or updated completed Codex item');
        const pendingType = pendingIds.get(e.item.id);
        if (pendingType && pendingType !== e.item.type) throw new Error('Codex item changed type');
        if (e.type === 'item.started') {
          if (pendingType) throw new Error('Duplicate started Codex item');
          pendingIds.set(e.item.id, e.item.type);
        }
        if (e.type === 'item.updated' && !pendingType) throw new Error('Updated Codex item was never started');
        if (e.type === 'item.completed') {
          pendingIds.delete(e.item.id);
          completedIds.add(e.item.id);
          if (e.item.type === 'agent_message') last = e.item.text;
        }
      } else if (raw.type === 'turn.completed' && phase === 2) {
        if (pendingIds.size) throw new Error('Codex turn completed with unfinished items');
        const e = EndSchema.parse(raw); base.usage = e.usage; tokenUsage(e.usage); phase = 3;
      } else throw new Error(`Unexpected, failed or out-of-order Codex event: ${String(raw.type)}`);
    }
    if (capture.timedOut) throw new Error('Codex invocation timed out');
    if (exitCode !== 0) throw new Error(`Codex exited ${exitCode}`);
    if (phase !== 3) throw new Error('Missing completed Codex turn');
    if (!last?.trim()) throw new Error('Missing Codex output');
    if (capture.lastMessage?.trim() !== last.trim()) throw new Error('Codex last-message file missing or differs from transcript');
    const output = normalize(JSON.parse(last), audit.schema);
    return { ...base, ok: true, tools: [], output };
  } catch (e) {
    return { ...base, ok: false, error: e instanceof Error ? e.message : String(e) };
  }
}

export type CodexSpawn = (argv: string[], options: { cwd: string; env: Env; stdin: Blob; timeoutMs: number }) => {
  stdout: ReadableStream<Uint8Array>; stderr: ReadableStream<Uint8Array>; exited: Promise<number>; kill: () => void;
};
const bunSpawn: CodexSpawn = (argv, options) => {
  const proc = Bun.spawn(argv, { cwd: options.cwd, env: options.env, stdin: options.stdin, stdout: 'pipe', stderr: 'pipe' });
  return { stdout: proc.stdout, stderr: proc.stderr, exited: proc.exited, kill: () => { proc.kill('SIGKILL'); } };
};

/** Extra spawn/env parameters exist solely for offline tests. No auth file is read or copied by this adapter. */
export async function invokeCodex(c: Card, prompt: string, schema: z.ZodType, options: CodexOptions,
  spawn: CodexSpawn = bunSpawn, env: Env = process.env,
): Promise<ClaudeResult & { stdout: string; stderr: string; exitCode: number }> {
  if (env.SUBORNED_LIVE !== '1') throw new Error('Codex generation requires explicit SUBORNED_LIVE=1 and recorded approval from Sky');
  const selected = checked(options), childEnv = codexEnv(env), bin = env.CODEX_BIN ?? 'codex';
  const originalSchema = z.toJSONSchema(schema) as JsonObject, outputSchema = codexOutputSchema(originalSchema);
  const temp = await mkdtemp('/tmp/suborned-codex-'), cwd = resolve(temp, 'work');
  try {
    await mkdir(cwd);
    const collect = async (argv: string[], stdin: string, timeoutMs: number) => {
      const proc = spawn(argv, { cwd, env: childEnv, stdin: new Blob([stdin]), timeoutMs });
      let timedOut = false;
      const timer = setTimeout(() => { timedOut = true; proc.kill(); }, timeoutMs);
      try {
        const [stdout, stderr, exitCode] = await Promise.all([
          new Response(proc.stdout).text(), new Response(proc.stderr).text(), proc.exited,
        ]);
        return { stdout, stderr, exitCode, timedOut };
      } catch (e) { proc.kill(); throw e; }
      finally { clearTimeout(timer); }
    };
    const probe = await collect([bin, '--version'], '', Math.min(10000, selected.timeoutMs));
    const cliVersion = probe.stdout.trim().match(/^codex-cli (\d+\.\d+\.\d+\S*)$/)?.[1];
    if (probe.timedOut || probe.exitCode !== 0 || !cliVersion)
      throw new Error('Codex CLI not found: install it and sign in with ChatGPT (codex login)');
    const schemaFile = resolve(temp, 'schema.json'), lastFile = resolve(temp, 'last-message.txt');
    await Bun.write(schemaFile, JSON.stringify(outputSchema));
    const argv = codexArgs(c, schemaFile, lastFile, selected, bin);
    const audit = { type: AUDIT, version: 2, promptSha256: digest(prompt), cliVersion, modelSource: MODEL_SOURCE,
      ...selected, cwd, argv, config: codexConfig(c, selected), schema: originalSchema, outputSchema };
    const result = await collect(argv, prompt, selected.timeoutMs);
    const lastMessage = await Bun.file(lastFile).text().catch(() => null);
    const capture = { type: CAPTURE, exitCode: result.exitCode, timedOut: result.timedOut, lastMessage };
    const stdout = `${JSON.stringify(audit)}\n${result.stdout}${result.stdout.endsWith('\n') ? '' : '\n'}${JSON.stringify(capture)}\n`;
    return { stdout, stderr: result.stderr, exitCode: result.exitCode, ...interpretCodex(stdout, result.exitCode, selected.model, digest(prompt)) };
  } finally { await rm(temp, { recursive: true, force: true }); }
}
