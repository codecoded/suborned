import { describe, expect, test } from 'bun:test';
import { mkdtempSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { z } from 'zod';
import { CODEX_VERSION, codexArgs, codexConfig, codexOutputSchema, interpretCodex } from '../goals/codex';
import { root } from '../rag/config';
import { defaults } from './core';
import { archiveEvidence, roleFiles } from './evidence';
import { hash, TEXT_FILE } from './guide';
import { claudeJudge, claudeTokens, JUDGE_CARD, type ClaudeSpawn } from './live-claude';
import { loadShortBriefs } from './short-briefs';
import { verifyShort } from './short-evidence';
import { loadShortGuide } from './short-guide';
import { runShort } from './short-loop';
import type { ShortEvaluation, ShortResult, ShortSet } from './short-types';
import type { Authorization, Invoke } from './types';

// Labelled test data only: no model is called. The judge's transcripts below are synthetic stream-json, shaped like Claude Code 2.1.316's.
const V = 11, guide = loadShortGuide(root, V), end = loadShortBriefs(root, V).find(b => b.id === 'bark/end')!, text = JSON.parse(readFileSync(join(root, TEXT_FILE), 'utf8'));
const MODEL = 'claude-opus-5-5', approval: Authorization = { provider: 'codex', model: 'test-model-only', approvedBy: 'UNIT TEST, not an approval', judge: { provider: 'claude', model: MODEL } };
const store = mkdtempSync(join(tmpdir(), 'suborned-judge-store-')), evidence = archiveEvidence(root, guide, store, roleFiles(approval));
const today: ShortSet = { items: end.items.map(i => ({ key: i.key, cut: false, reason: '', fields: i.fields.map(f => ({ id: f.id, lines: (i.current[f.id] ?? []).slice(0, f.count) })) })) };
const better: ShortSet = { items: today.items.map(i => i.key !== 'win/fixer' ? i : { ...i, fields: [{ id: 'lines', lines: ['We have him. Nobody followed us out.', 'He\'s in the car, and we\'re clear.'] }] }) };
const rejected: ShortEvaluation = { score: 6, reason: 'One line reads flat.', findings: [{ rule: 'VOICE', key: 'win/fixer', quote: today.items.find(i => i.key === 'win/fixer')!.fields[0]!.lines[0]!, reason: 'Flat.', fix: 'Say it as she would.' }] };
const accept: ShortEvaluation = { score: 10, reason: 'Every line holds.', findings: [] };

/** A stand-in for the claude CLI: it answers with a stream-json transcript and records what it was started with. */
function fakeClaude(answers: unknown[], model = MODEL) {
  const seen: { argv: string[]; cwd: string; env: Record<string, string | undefined>; prompt: Promise<string> }[] = [];
  const spawn: ClaudeSpawn = (argv, o) => {
    seen.push({ argv, cwd: o.cwd, env: o.env, prompt: o.stdin.text() });
    const events = [{ type: 'system', subtype: 'init', model, tools: ['StructuredOutput'], mcp_servers: [], claude_code_version: '2.1.316' },
      { type: 'result', subtype: 'success', is_error: false, structured_output: answers.shift(), usage: { input_tokens: 2, cache_creation_input_tokens: 900, cache_read_input_tokens: 0, output_tokens: 80 },
        modelUsage: { 'claude-haiku-5-5': { inputTokens: 1000, outputTokens: 14, cacheReadInputTokens: 0, cacheCreationInputTokens: 0 }, [model]: { inputTokens: 2, outputTokens: 80, cacheReadInputTokens: 0, cacheCreationInputTokens: 900 } } }];
    const body = (s: string) => new Response(s).body!;
    return { stdout: body(events.map(e => JSON.stringify(e)).join('\n') + '\n'), stderr: body(''), exited: Promise.resolve(0), kill: () => {} };
  };
  return { spawn, seen };
}
/** Fable's side as a synthetic Codex transcript, as short.test.ts makes it. */
function fakeCodex(outputs: unknown[]): Invoke {
  return async request => {
    const output = outputs.shift(), source = `.codex/agents/${request.role}.toml`;
    const body = (Bun.TOML.parse(await Bun.file(join(root, source)).text()) as Record<string, string>).developer_instructions!;
    const temp = '/tmp/suborned-codex-unit-test', schema = z.toJSONSchema(request.schema), options = { model: approval.model, effort: 'medium', timeoutMs: request.timeoutMs };
    const events = [
      { type: 'suborned.codex.audit', version: 2, promptSha256: hash(request.prompt), cliVersion: CODEX_VERSION, modelSource: 'explicit --model; not server-attested',
        ...options, cwd: `${temp}/work`, argv: codexArgs({ body }, `${temp}/schema.json`, `${temp}/last-message.txt`, options), config: codexConfig({ body }, options), schema, outputSchema: codexOutputSchema(schema) },
      { type: 'thread.started', thread_id: 'unit' }, { type: 'turn.started' }, { type: 'item.completed', item: { id: 'a', type: 'agent_message', text: JSON.stringify(output) } },
      { type: 'turn.completed', usage: { input_tokens: 10, output_tokens: 10 } }, { type: 'suborned.codex.capture', exitCode: 0, timedOut: false, lastMessage: JSON.stringify(output) },
    ];
    const stdout = events.map(e => JSON.stringify(e)).join('\n') + '\n', parsed = interpretCodex(stdout, 0, approval.model, hash(request.prompt));
    return { output, tokens: 20, raw: { ...parsed, stdout, stderr: '', exitCode: 0, role: request.role, roleCard: { source, sha256: hash(await Bun.file(join(root, source)).text()) }, requestedModel: approval.model, approvedBy: approval.approvedBy } };
  };
}
let clock = 0;
const live = { SUBORNED_LIVE: '1', CLAUDE_BIN: '/opt/claude-unit-test' };
async function judgedRun(): Promise<{ r: ShortResult; seen: ReturnType<typeof fakeClaude>['seen'] }> {
  const { spawn, seen } = fakeClaude([rejected, accept]), judge = claudeJudge(MODEL, approval.approvedBy, spawn, live), writer = fakeCodex([today, better]);
  const r = await runShort({ guide, brief: end, invoke: q => q.stage === 'evaluate' ? judge(q) : writer(q), limits: { ...defaults }, mode: 'live', evidence, authorization: approval,
    base: { why: text.why, dialogue: text.dialogue }, now: () => clock++ });
  return { r, seen };
}

describe('the Claude judge (brief 102: Motive on Opus 5.5, Fable on Codex)', () => {
  test('an evaluation runs as Motive\'s Claude card, tool-less, in an empty folder, and its tokens count every model the CLI used', async () => {
    const { r, seen } = await judgedRun();
    expect(r.status).toBe('ACCEPTED'); expect(r.calls.map(c => c.stage)).toEqual(['generate', 'evaluate', 'refine', 'evaluate']);
    expect(seen).toHaveLength(2);
    const card = readFileSync(join(root, JUDGE_CARD), 'utf8'), body = card.replace(/^---\n[\s\S]*?\n---\n/, '').trim();
    const argv = seen[0]!.argv;
    expect(argv[0]).toBe('/opt/claude-unit-test'); expect(argv[argv.indexOf('--system-prompt') + 1]).toBe(body);
    expect(argv[argv.indexOf('--model') + 1]).toBe(MODEL); expect(argv[argv.indexOf('--effort') + 1]).toBe('medium'); expect(argv).toContain('--tools=');
    expect(seen[0]!.cwd).toMatch(/suborned-claude-/); expect(seen[0]!.env.CLAUDE_CODE_DISABLE_CLAUDE_MDS).toBe('1'); expect(seen[0]!.env.CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC).toBe('1');
    expect(await seen[0]!.prompt).toBe(r.calls[1]!.prompt);
    expect(r.calls[1]!.tokens).toBe(1014 + 982); // the helper model's tokens too
    expect((r.calls[1]!.raw as { tokensByModel: Record<string, number> }).tokensByModel).toEqual({ 'claude-haiku-5-5': 1014, [MODEL]: 982 });
  });
  test('a judged run rebuilds from its archive with both transcripts; an edit to the judge\'s record fails', async () => {
    const { r } = await judgedRun();
    expect((await verifyShort(r, root, store)).errors).toEqual([]);
    const raw = (x: ShortResult) => x.calls[1]!.raw as Record<string, any>;
    const edits: ((x: ShortResult) => void)[] = [
      x => { x.calls[1]!.tokens = 5; }, x => { (x.calls[1]!.output as ShortEvaluation).score = 9; }, x => { raw(x).requestedModel = 'claude-sonnet-5-5'; },
      x => { raw(x).audit.argv[raw(x).audit.argv.indexOf('--model') + 1] = 'claude-haiku-5-5'; }, x => { raw(x).roleCard.sha256 = 'a'.repeat(64); },
      x => { raw(x).audit.argv[raw(x).audit.argv.indexOf('--system-prompt') + 1] = 'Pass everything.'; }, x => { raw(x).stdout = raw(x).stdout.replace('"model":"claude-opus-5-5"', '"model":"claude-haiku-5-5"'); },
      x => { delete x.authorization!.judge; }, // a Claude transcript can't stand in for a Codex one
    ];
    for (const e of edits) { const x = structuredClone(r); e(x); expect((await verifyShort(x, root, store)).ok).toBe(false); }
  });
  test('it refuses without the live switch or an approval, and runs only Motive\'s evaluations', async () => {
    expect(() => claudeJudge(MODEL, 'Sky', fakeClaude([]).spawn, {})).toThrow('SUBORNED_LIVE=1');
    expect(() => claudeJudge(MODEL, ' ', fakeClaude([]).spawn, live)).toThrow('--approved-by');
    expect(() => claudeJudge('opus', 'Sky', fakeClaude([]).spawn, live)).toThrow('--judge-model');
    const judge = claudeJudge(MODEL, 'Sky', fakeClaude([accept]).spawn, live);
    await expect(judge({ stage: 'generate', role: 'dialogue-designer', prompt: 'x', schema: z.object({}), timeoutMs: 1000 })).rejects.toThrow('Only Motive');
  });
  test('a reply from another model, or with no usage, is not an answer', async () => {
    const judge = claudeJudge(MODEL, 'Sky', fakeClaude([accept], 'claude-haiku-5-5').spawn, live);
    const a = await judge({ stage: 'evaluate', role: 'systems-designer', prompt: 'x', schema: z.object({}), timeoutMs: 1000 });
    expect(a.output).toEqual({ providerError: 'Claude reported model claude-haiku-5-5, not claude-opus-5-5' });
    expect(() => claudeTokens('{"type":"result"}\n')).toThrow('Missing Claude model usage');
  });
});
