import { describe, expect, test } from 'bun:test';
import { existsSync, readFileSync, readdirSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { z } from 'zod';
import { root } from '../../rag/config';
import type { Card } from '../../rag/claude';
import { CODEX_VERSION, codexArgs, codexCard, codexConfig, codexEnv, codexOutputSchema, interpretCodex,
  invokeCodex, tokenUsage, type CodexSpawn } from '../codex';

const c: Card = { name: 'test', model: 'ignored-model', body: 'Only supplied context.\nReturn JSON. "quotes" \\ slash', source: 'test.toml', text: 'exact source' };
const options = { model: 'gpt-6-astra', effort: 'high' };
const schema = z.object({ answer: z.string() }).strict();
const usage = { input_tokens: 40, cached_input_tokens: 10, output_tokens: 8 };
const completion = (text = '{"answer":"ok"}') => [
  { type: 'thread.started', thread_id: 'thread-test' },
  { type: 'turn.started' },
  { type: 'item.completed', item: { id: 'reason', type: 'reasoning', text: 'Considering supplied context' } },
  { type: 'item.completed', item: { id: 'answer', type: 'agent_message', text } },
  { type: 'turn.completed', usage },
];
const lines = (events: unknown[]) => events.map(e => JSON.stringify(e)).join('\n') + '\n';
function fixture(events: unknown[] = completion(), lastMessage: string | null = '{"answer":"ok"}', s: z.ZodType = schema): any[] {
  const temp = '/tmp/suborned-codex-fixture', source = z.toJSONSchema(s);
  return [{ type: 'suborned.codex.audit', version: 1, cliVersion: CODEX_VERSION, modelSource: 'explicit --model; not server-attested',
    ...options, timeoutMs: 300000, cwd: `${temp}/work`,
    argv: codexArgs(c, `${temp}/schema.json`, `${temp}/last-message.txt`, options), config: codexConfig(c, options),
    schema: source, outputSchema: codexOutputSchema(source),
  }, ...events, { type: 'suborned.codex.capture', exitCode: 0, timedOut: false, lastMessage }];
}
const parse = (events: unknown[]) => interpretCodex(lines(events), 0, options.model);
const stream = (s: string) => new Blob([s]).stream();

describe('Codex card and command isolation', () => {
  test('loads exact TOML bytes and developer instructions by role and GDD name', async () => {
    const card = await codexCard('Splice'), role = await codexCard('tools-engineer');
    const text = await Bun.file(resolve(root, '.codex/agents/tools-engineer.toml')).text();
    expect(card).toEqual(role);
    expect(card.source).toBe('.codex/agents/tools-engineer.toml');
    expect(card.text).toBe(text);
    expect(card.body).toBe((Bun.TOML.parse(text) as Record<string, string>).developer_instructions!);
    expect(card.model).toBe('inherit');
    await expect(codexCard('../escape')).rejects.toThrow('Invalid');
    await expect(codexCard('unknown-role')).rejects.toThrow('Unknown');
  });

  test('pins explicit model, schema and isolation, with no shell interpolation or card-model fallback', () => {
    const argv = codexArgs(c, '/tmp/schema file.json', '/tmp/last message.txt', options);
    for (const flag of ['--json', '--ephemeral', '--ignore-user-config', '--skip-git-repo-check']) expect(argv).toContain(flag);
    expect(argv[argv.indexOf('--sandbox') + 1]).toBe('read-only');
    expect(argv[argv.indexOf('--model') + 1]).toBe(options.model);
    expect(argv[argv.indexOf('--output-schema') + 1]).toBe('/tmp/schema file.json');
    expect(argv).not.toContain('--dangerously-bypass-approvals-and-sandbox');
    expect(argv).not.toContain(c.model);
    expect(argv.at(-1)).toBe('-');
    const config = codexConfig(c, options);
    for (const entry of ['forced_login_method="chatgpt"', 'model_provider="openai"', 'mcp_servers={}',
      'agents.enabled=false', 'web_search="disabled"', 'features.apps=false', 'features.plugins=false',
      'features.multi_agent=false', 'features.multi_agent_v2=false', 'features.shell_tool=false',
      'features.code_mode=false', 'features.code_mode_host=false', 'features.memories=false',
      'features.skip_host_skill_discovery=true', 'skills.include_instructions=false', 'project_doc_max_bytes=0']) expect(config).toContain(entry);
    expect(Bun.TOML.parse(config.at(-1)!)).toEqual({ developer_instructions: c.body });
    expect(() => codexArgs(c, 's', 'o', { model: 'inherit' })).toThrow('explicit');
    expect(() => codexArgs(c, 's', 'o', { model: '' })).toThrow('explicit');
    expect(() => codexArgs(c, 's', 'o', { model: 'x', timeoutMs: 300001 })).toThrow('timeout');
    expect(() => codexArgs(c, 's', 'o', { model: 'x', effort: 'magic' })).toThrow('effort');
  });

  test('inherits authentication location without API keys or injected provider/config environment', () => {
    expect(codexEnv({ PATH: '/bin', HOME: '/home/test', CODEX_HOME: '/auth-location', OPENAI_API_KEY: 'secret',
      CODEX_API_KEY: 'secret2', AZURE_OPENAI_API_KEY: 'secret3', OPENAI_BASE_URL: 'https://untrusted.invalid',
      CODEX_CONFIG: 'override', T3_ACP_MCP_ENTRYPOINT: 'injected', SUBORNED_LIVE: '1' }))
      .toEqual({ PATH: '/bin', HOME: '/home/test', CODEX_HOME: '/auth-location' });
  });
});

describe('Codex transcript verification', () => {
  test('new invocation audits bind the stdin prompt and reject absent or changed bindings', () => {
    const events = fixture(); events[0].version = 2; events[0].promptSha256 = 'a'.repeat(64);
    expect(interpretCodex(lines(events), 0, options.model, 'a'.repeat(64)).ok).toBe(true);
    expect(interpretCodex(lines(events), 0, options.model, 'b'.repeat(64)).error).toContain('prompt hash');
    delete events[0].promptSha256;
    expect(parse(events).error).toContain('prompt hash');
    expect(interpretCodex(lines(fixture()), 0, options.model, 'a'.repeat(64)).error).toContain('prompt hash');
  });
  test('retains known CLI 0.163.0 startup diagnostics without accepting runtime or unknown errors', () => {
    const warnings = [
      'Code Mode is unavailable because code-mode host is disabled. Code mode will fail closed; enable `features.code_mode_host` and install `codex-code-mode-host`.',
      'Under-development features enabled: skip_host_skill_discovery. Under-development features are incomplete and may behave unpredictably. To suppress this warning, set `suppress_unstable_features_warning = true` in /home/test/.codex/config.toml.',
    ].map((message, i) => ({ type: 'item.completed', item: { id: `warning-${i}`, type: 'error', message } }));
    const events: unknown[] = completion(); events.splice(1, 0, ...warnings);
    expect(parse(fixture(events)).ok).toBe(true);
    const runtime: unknown[] = completion(); runtime.splice(2, 0, warnings[0]);
    expect(parse(fixture(runtime)).ok).toBe(false);
    const unknown: unknown[] = completion(); unknown.splice(1, 0, { type: 'item.completed', item: { id: 'unknown', type: 'error', message: 'Authentication failed' } });
    expect(parse(fixture(unknown)).ok).toBe(false);
  });
  test('reconstructs output and truthful metadata with unknown cost and no server-attested model', () => {
    expect(parse(fixture())).toEqual({ ok: true, output: { answer: 'ok' }, usage, costUsd: null,
      model: options.model, modelsUsed: [], tools: [], cliVersion: CODEX_VERSION });
  });

  test('accepts a matching model report but rejects mismatching selection, CLI model and version', () => {
    const events = completion();
    const same = [{ ...events[0], model: options.model }, ...events.slice(1)];
    expect(parse(fixture(same)).modelsUsed).toEqual([options.model]);
    expect(interpretCodex(lines(fixture()), 0, 'other-model').ok).toBe(false);
    expect(parse(fixture([{ ...events[0], model: 'other-model' }, ...events.slice(1)])).error).toContain('model mismatch');
    expect(parse(fixture([{ ...events[0], cli_version: '9.9' }, ...events.slice(1)])).error).toContain('version mismatch');
  });

  for (const type of ['command_execution', 'file_change', 'mcp_tool_call', 'web_search', 'collab_tool_call',
    'delegate_task', 'tool_call', 'function_call', 'todo_list', 'future_tool']) {
    for (const eventType of ['item.started', 'item.updated', 'item.completed']) test(`rejects ${type} at ${eventType}`, () => {
      const events = completion();
      events.splice(2, 0, { type: eventType, item: { id: 'bad', type, text: '' } });
      const result = parse(fixture(events));
      expect(result.ok).toBe(false);
      expect(result.error).toContain('Unexpected Codex tool/item');
      expect(result.tools).toEqual([type]);
    });
  }

  test('rejects every unknown, failed, malformed or out-of-order event', () => {
    for (const event of [{ type: 'error', message: 'lost connection' }, { type: 'turn.failed', error: { message: 'failed' } },
      { type: 'command_execution' }, { type: 'tool.started' }, null, [], { type: 'turn.started' }]) {
      const events: unknown[] = completion(); events.splice(2, 0, event);
      expect(parse(fixture(events)).ok).toBe(false);
    }
    expect(interpretCodex(lines(fixture()) + 'broken json\n', 0).ok).toBe(false);
    expect(interpretCodex(lines(fixture()).replace('"type":"turn.started"', '"type":"turn.started","tool":"exec"'), 0).ok).toBe(false);
    expect(parse(fixture([...completion(), { type: 'turn.started' }])).ok).toBe(false);
    expect(parse(fixture(completion().slice(1))).ok).toBe(false);
    expect(parse(fixture(completion().slice(0, -1))).ok).toBe(false);
    expect(interpretCodex(lines(completion()), 0, options.model).ok).toBe(false);
    expect(parse(fixture([completion()[0], ...completion()])).ok).toBe(false);
  });

  test('rejects audit tampering, missing captures and failed exits', () => {
    for (const patch of [{ cwd: root }, { cliVersion: 'unknown' }, { config: [] }, { argv: ['codex', 'exec'] },
      { outputSchema: {} }, { timeoutMs: 0 }, { modelSource: 'server' }]) {
      const f = fixture(); f[0] = { ...f[0], ...patch } as any;
      expect(parse(f).ok).toBe(false);
    }
    const f = fixture(); f.push(f[0]!);
    expect(parse(f).ok).toBe(false);
    expect(parse(fixture().slice(0, -1)).ok).toBe(false);
    expect(interpretCodex(lines(fixture()), 1).ok).toBe(false);
    const failed = fixture(); failed[failed.length - 1] = { ...failed.at(-1), exitCode: 1 } as any;
    expect(interpretCodex(lines(failed), 1).error).toContain('exited 1');
  });

  test('rejects incomplete, duplicate and type-changing item lifecycles', () => {
    const item = { id: 'pending', type: 'reasoning', text: '' };
    for (const additions of [
      [{ type: 'item.started', item }], [{ type: 'item.updated', item }],
      [{ type: 'item.started', item }, { type: 'item.started', item }],
      [{ type: 'item.started', item }, { type: 'item.completed', item: { ...item, type: 'agent_message' } }],
      [{ type: 'item.completed', item }, { type: 'item.completed', item }],
    ]) {
      const events: unknown[] = completion(); events.splice(2, 0, ...additions);
      expect(parse(fixture(events)).ok).toBe(false);
    }
    const events: unknown[] = completion();
    events.splice(2, 0, { type: 'item.started', item }, { type: 'item.updated', item }, { type: 'item.completed', item });
    expect(parse(fixture(events)).ok).toBe(true);
  });

  test('requires output, JSON, completion, and matching last-message file', () => {
    for (const text of ['', '   ', 'not json', '```json\n{"answer":"ok"}\n```']) expect(parse(fixture(completion(text), text)).ok).toBe(false);
    expect(parse(fixture(completion(), null)).ok).toBe(false);
    expect(parse(fixture(completion(), '{"answer":"different"}')).ok).toBe(false);
    expect(parse(fixture(completion().filter(e => e.type !== 'item.completed'))).ok).toBe(false);
    // Parent owns domain/schema validation, so valid JSON with wrong fields reaches the parent.
    expect(parse(fixture(completion('{"wrong":1}'), '{"wrong":1}')).output).toEqual({ wrong: 1 });
  });

  test('usage counts must be present, finite, nonnegative safe integers', () => {
    expect(tokenUsage(usage)).toBe(48);
    expect(tokenUsage({ input_tokens: 0, output_tokens: 0 })).toBe(0);
    for (const bad of [null, {}, [], { input_tokens: 1 }, { input_tokens: '1', output_tokens: 2 },
      { input_tokens: -1, output_tokens: 2 }, { input_tokens: 1, output_tokens: NaN },
      { input_tokens: Infinity, output_tokens: 1 }, { input_tokens: 0.5, output_tokens: 1 },
      { input_tokens: 1, output_tokens: 1, cached_input_tokens: 2 },
      { input_tokens: 1, output_tokens: 1, cached_input_tokens: null },
      { input_tokens: Number.MAX_SAFE_INTEGER, output_tokens: 1 }]) {
      expect(() => tokenUsage(bad)).toThrow();
      const events: unknown[] = completion(); events[events.length - 1] = { type: 'turn.completed', usage: bad };
      expect(parse(fixture(events)).ok).toBe(false);
    }
    const events: unknown[] = completion(); events[events.length - 1] = { type: 'turn.completed' };
    expect(parse(fixture(events)).ok).toBe(false);
  });

  test('optional read ranges travel as required nullable and normalize identically on replay', () => {
    const ranges = z.object({ context: z.array(z.object({ path: z.string(), from: z.number().int().positive().optional(),
      to: z.number().int().positive().optional(), note: z.string().nullable().optional() }).strict()) }).strict();
    const transport = codexOutputSchema(z.toJSONSchema(ranges));
    expect(transport.properties.context.items.required).toEqual(['path', 'from', 'to', 'note']);
    expect(transport.properties.context.items.properties.from.anyOf.at(-1)).toEqual({ type: 'null' });
    const text = '{"context":[{"path":"test.cs","from":null,"to":12,"note":null}]}';
    const result = parse(fixture(completion(text), text, ranges));
    expect(result.ok).toBe(true);
    expect(result.output).toEqual({ context: [{ path: 'test.cs', to: 12, note: null }] });
    expect(result.output).toEqual(ranges.parse(result.output));
    const required = z.object({ from: z.number() }).strict();
    expect(parse(fixture(completion('{"from":null}'), '{"from":null}', required)).output).toEqual({ from: null });
    expect(() => codexOutputSchema(z.toJSONSchema(z.object({ x: z.string() }).passthrough()))).toThrow('strict');
  });
});

describe('Codex invocation with an injected process (never a model)', () => {
  test('requires opt-in before any process or version probe', async () => {
    let called = false;
    const spawn: CodexSpawn = () => { called = true; throw new Error('must not spawn'); };
    await expect(invokeCodex(c, 'prompt', schema, options, spawn, {})).rejects.toThrow('SUBORNED_LIVE');
    expect(called).toBe(false);
  });

  test('uses an empty temporary cwd, passes schema and prompt, drains streams, replays, and cleans up', async () => {
    const calls: string[][] = []; let cwd = '', stdin: Promise<string> | undefined;
    const spawn: CodexSpawn = (argv, o) => {
      calls.push(argv); cwd = o.cwd;
      expect(o.env).toEqual({ CODEX_HOME: '/auth-location' });
      expect(readdirSync(cwd)).toEqual([]);
      expect(cwd.startsWith('/tmp/suborned-codex-')).toBe(true);
      if (argv[1] === '--version') return { stdout: stream(`codex-cli ${CODEX_VERSION}\n`), stderr: stream(''), exited: Promise.resolve(0), kill() {} };
      expect(o.timeoutMs).toBe(300000);
      stdin = o.stdin.text();
      const schemaFile = argv[argv.indexOf('--output-schema') + 1]!;
      expect(JSON.parse(readFileSync(schemaFile, 'utf8'))).toEqual(codexOutputSchema(z.toJSONSchema(schema)));
      writeFileSync(argv[argv.indexOf('--output-last-message') + 1]!, '{"answer":"ok"}\n');
      return { stdout: stream(lines(completion())), stderr: stream('diagnostic '.repeat(20000)), exited: Promise.resolve(0), kill() {} };
    };
    const result = await invokeCodex(c, 'supplied prompt', schema, options, spawn,
      { SUBORNED_LIVE: '1', CODEX_HOME: '/auth-location', OPENAI_API_KEY: 'never forwarded', CODEX_BIN: '/test/codex' });
    expect(await stdin).toBe('supplied prompt');
    expect(calls[0]).toEqual(['/test/codex', '--version']);
    expect(calls).toHaveLength(2);
    expect(result.ok).toBe(true);
    expect(result.stderr).toHaveLength(220000);
    const { stdout, stderr: _, exitCode, ...interpreted } = result;
    expect(interpretCodex(stdout, exitCode, options.model)).toEqual(interpreted);
    expect(existsSync(dirname(cwd))).toBe(false);
  });

  test('stops before generation when no Codex CLI answers, and cleans its directory', async () => {
    let calls = 0, cwd = '';
    const spawn: CodexSpawn = (_argv, o) => { calls++; cwd = o.cwd;
      return { stdout: stream('command not found'), stderr: stream(''), exited: Promise.resolve(127), kill() {} }; };
    await expect(invokeCodex(c, 'p', schema, options, spawn, { SUBORNED_LIVE: '1' })).rejects.toThrow('Codex CLI not found');
    expect(calls).toBe(1); expect(existsSync(dirname(cwd))).toBe(false);
  });

  test('kills timed-out generation and retains a rejected replayable capture', async () => {
    let killed = false;
    const spawn: CodexSpawn = (argv) => {
      if (argv[1] === '--version') return { stdout: stream(`codex-cli ${CODEX_VERSION}`), stderr: stream(''), exited: Promise.resolve(0), kill() {} };
      let finish!: (code: number) => void;
      const exited = new Promise<number>(r => { finish = r; });
      return { stdout: stream(lines(completion())), stderr: stream('partial diagnostics'), exited,
        kill() { killed = true; finish(137); } };
    };
    const result = await invokeCodex(c, 'p', schema, { ...options, timeoutMs: 10 }, spawn, { SUBORNED_LIVE: '1' });
    expect(killed).toBe(true); expect(result.ok).toBe(false);
    expect(result.error).toContain('timed out');
    expect(interpretCodex(result.stdout, result.exitCode).error).toBe(result.error);
  });
});
