import { describe, expect, test } from 'bun:test';
import { chmod, mkdtemp, readdir } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { z } from 'zod';
import { root } from './config';
import { chunkDocument, chunkSection, loadCorpus, SPLIT_OVER } from './corpus';
import { fuse, terms } from './retrieval';
import { card, claudeArgs, cliSchema, interpretClaude, invokeClaude } from './claude';

describe('corpus', () => {
  test('source changes invalidate identity; chunks keep auditable source lines', () => {
    const a = chunkDocument('rules.md', '## §04 PEOPLE\nHelp needs Listen.\n')[0]!, b = chunkDocument('rules.md', '## §04 PEOPLE\nHelp needs Watch.\n')[0]!;
    expect(a.key).not.toBe(b.key); expect(a.sourceHash).not.toBe(b.sourceHash);
    expect(a.lineStart).toBe(1); expect(a.text).toContain('Help needs Listen');
  });
  test('the live corpus covers every agent GDD section, the human GDD and the plan', async () => {
    const chunks = await loadCorpus(), kinds = new Set(chunks.map(c => c.kind));
    expect([...kinds].sort()).toEqual(['gdd-agent', 'gdd-human', 'plan']);
    const sections = new Set(chunks.filter(c => c.kind === 'gdd-agent').map(c => c.section.slice(0, 3)));
    for (let n = 1; n <= 15; n++) expect(sections.has(`§${String(n).padStart(2, '0')}`)).toBe(true);
    expect(chunks.some(c => c.kind === 'plan' && c.text.includes('stopSpacing'))).toBe(true);
    expect(new Set(chunks.map(c => c.key)).size).toBe(chunks.length);
  });
  test('agent GDD chunks point at their true file lines', async () => {
    const chunk = (await loadCorpus()).find(c => c.kind === 'gdd-agent' && c.text.includes('### Datum'))!;
    const lines = (await Bun.file(join(root, chunk.source)).text()).split('\n').slice(chunk.lineStart - 1, chunk.lineEnd).join('\n');
    expect(lines).toContain('### Datum');
  });
});

describe('agent GDD chunking', () => {
  const meta = { authority: 'a', version: 'v', kind: 'gdd-agent' }, split = (md: string) => chunkSection('x.md', md.split('\n'), 10, meta);
  test('a long section splits on its tagged entries, each prefixed with the section title and holding its true lines', () => {
    const md = `## §02 LOOP + RESOURCES (extra)\nmission: ${'a '.repeat(300)}\n| t | u |\n| 1 | 2 |\ntravel [S]: boarding a tram is fast travel. Price = time only. ${'c '.repeat(60)}\nchain: ${'b '.repeat(200)}`;
    const c = split(md);
    expect(c.map(x => x.section)).toEqual(['§02 LOOP + RESOURCES › mission', '§02 LOOP + RESOURCES › travel', '§02 LOOP + RESOURCES › chain']);
    expect(c[1]!.text.startsWith('§02 LOOP + RESOURCES › travel\ntravel [S]: boarding a tram is fast travel. Price = time only.')).toBe(true);
    expect(c[0]!.text).toContain('| 1 | 2 |'); expect([c[1]!.lineStart, c[1]!.lineEnd]).toEqual([15, 15]);
  });
  test('a short section stays whole; a short entry joins the next untagged one', () => {
    const c = split('## §03 CELL\nplan: one.\nnote: two.\n'); expect(c).toHaveLength(1); expect(c[0]!.text).toBe('§03 CELL\nplan: one.\nnote: two.');
    const long = `## §09 X\nrule: short\nnext: ${'w '.repeat(SPLIT_OVER)}`;
    expect(split(long).map(x => x.section)).toEqual(['§09 X › rule']);
  });
  test('the live corpus has the boarding rule as its own chunk', async () => {
    const t = (await loadCorpus()).find(c => c.section === '§02 LOOP + RESOURCES › travel')!;
    expect(t.text).toContain('Price = time only');
  });
});

describe('retrieval', () => {
  test('fusion rewards agreement and keeps unique candidates', () => {
    const a = chunkDocument('a.md', '## A\nFirst fact.')[0]!, b = chunkDocument('b.md', '## B\nSecond fact.')[0]!;
    const hits = fuse([{ signal: 'semantic', rows: [a, b] }, { signal: 'keyword', rows: [b] }], 2);
    expect(hits[0]!.key).toBe(b.key); expect(hits[0]!.signals).toEqual(['semantic', 'keyword']); expect(hits).toHaveLength(2);
  });
  test('keyword terms drop stopwords and short tokens but keep ids and section marks', () =>
    expect(terms('Which guards see the meet-vale exit and §07 Handover? A B')).toEqual(['guards', 'see', 'meet-vale', 'exit', '§07', 'handover']));
});

const init = (tools = ['StructuredOutput'], mcp: object[] = []) => ({ type: 'system', subtype: 'init', model: 'claude-opus-5-5', tools, mcp_servers: mcp, claude_code_version: '2.1.295' });
const result = (extra: object = {}) => ({ type: 'result', subtype: 'success', is_error: false, structured_output: { ok: true }, usage: { output_tokens: 3 }, total_cost_usd: 0.01, modelUsage: { 'claude-opus-5-5': {} }, ...extra });
const transcript = (...events: object[]) => events.map(e => JSON.stringify(e)).join('\n') + '\n';

describe('claude transcript', () => {
  test('accepts structured output and records provenance', () =>
    expect(interpretClaude(transcript(init(), result()), 0)).toMatchObject({ ok: true, output: { ok: true }, model: 'claude-opus-5-5', tools: ['StructuredOutput'], cliVersion: '2.1.295', costUsd: 0.01 }));
  test.each([
    ['missing init', transcript(result()), 0, 'tool isolation unverified'],
    ['extra tools', transcript(init(['StructuredOutput', 'Glob', 'Grep']), result()), 0, 'unexpected tools: Glob, Grep'],
    ['MCP servers', transcript(init(undefined, [{ name: 'unity' }]), result()), 0, 'connected MCP servers: unity'],
    ['missing result', transcript(init()), 1, 'No result event'],
    ['non-zero exit', transcript(init(), result()), 2, 'exited 2'],
    ['is_error', transcript(init(), result({ is_error: true, subtype: 'error_max_turns', result: 'stopped' })), 0, 'error_max_turns'],
    ['no structured output', transcript(init(), result({ structured_output: undefined })), 0, 'Missing structured_output'],
  ])('rejects %s', (_, stdout, code, message) => {
    const r = interpretClaude(stdout as string, code as number);
    expect(r.ok).toBe(false); expect(r.error).toContain(message as string);
  });
});

describe('claude invocation', () => {
  test('crew card as system prompt, no built-in tools, project-only settings, $schema-free schema', async () => {
    const c = await card('Keel'), args = claudeArgs(c, cliSchema(z.object({ pass: z.boolean() })), 'high');
    expect(args.slice(1, 3)).toEqual(['-p', '--system-prompt']); expect(args[3]).toBe(c.body);
    expect(args).not.toContain('--agent');
    for (const flag of ['--tools=', '--no-session-persistence', '--strict-mcp-config']) expect(args).toContain(flag);
    expect(args[args.indexOf('--setting-sources') + 1]).toBe('project');
    expect(args[args.indexOf('--model') + 1]).toBe('opus');
    expect(JSON.parse(args[args.indexOf('--json-schema') + 1]!)).not.toHaveProperty('$schema');
  });
  test('pipes the prompt on stdin from the repo root with CLAUDE.md disabled', async () => {
    const dir = await mkdtemp(join(tmpdir(), 'fake-claude-')), bin = join(dir, 'claude');
    await Bun.write(bin, `#!/bin/sh\ncat > "${dir}/stdin.txt"\npwd > "${dir}/cwd.txt"\necho "$CLAUDE_CODE_DISABLE_CLAUDE_MDS" > "${dir}/mds.txt"\nprintf '%s\\n' '${JSON.stringify(init())}' '${JSON.stringify(result())}'\n`);
    await chmod(bin, 0o755);
    process.env.CLAUDE_BIN = bin;
    try {
      const run = await invokeClaude(await card('Fable'), 'PROMPT BODY', z.object({ ok: z.boolean() }));
      expect(run.ok).toBe(true);
      expect(await Bun.file(join(dir, 'stdin.txt')).text()).toBe('PROMPT BODY');
      expect((await Bun.file(join(dir, 'cwd.txt')).text()).trim()).toBe(root);
      expect((await Bun.file(join(dir, 'mds.txt')).text()).trim()).toBe('1');
    } finally { delete process.env.CLAUDE_BIN; }
  });
  test('every crew card parses, names itself and maps to a Claude model', async () => {
    const names = (await readdir(join(root, '.claude/agents'))).map(f => f.replace(/\.md$/, ''));
    expect(names.length).toBe(17);
    for (const n of names) expect(['opus', 'sonnet']).toContain((await card(n)).model);
  });
  test('a card resolves by GDD name or agent name', async () => {
    expect((await card('Keel')).name).toBe('simulation-engineer');
    expect((await card('simulation-engineer')).source).toBe((await card('keel')).source);
  });
});
