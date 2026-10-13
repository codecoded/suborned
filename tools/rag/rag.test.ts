import { describe, expect, test } from 'bun:test';
import { chmod, mkdtemp, readdir } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { z } from 'zod';
import { root } from './config';
import { CHUNKER, chunkDocument, chunkSection, loadCorpus, SPLIT_OVER } from './corpus';
import { kitChunks, kitDigest } from './kit-corpus';
import { planChunks } from './plan-corpus';
import { fuse, terms } from './retrieval';
import { card, claudeArgs, cliSchema, interpretClaude, invokeClaude, isLive, liveGuard, userMessage, type Spawn } from './claude';
import { imageNote, loadImages, MAX_IMAGE_BYTES, sha256 } from './images';

describe('corpus', () => {
  test('source changes invalidate identity; chunks keep auditable source lines', () => {
    const a = chunkDocument('rules.md', '## §04 PEOPLE\nHelp needs Listen.\n')[0]!, b = chunkDocument('rules.md', '## §04 PEOPLE\nHelp needs Watch.\n')[0]!;
    expect(a.key).not.toBe(b.key); expect(a.sourceHash).not.toBe(b.sourceHash);
    expect(a.lineStart).toBe(1); expect(a.text).toContain('Help needs Listen');
  });
  test('the live corpus covers every agent GDD section, the human GDD and the plan', async () => {
    const chunks = await loadCorpus(), kinds = new Set(chunks.map(c => c.kind));
    expect([...kinds].sort()).toEqual(['gdd-agent', 'gdd-human', 'kit', 'plan']);
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

describe('skyline and kit chunks (chunker v3)', () => {
  const plan = async () => JSON.parse(await Bun.file(join(root, 'contracts/world/harbour-district.plan.json')).text());
  const kitText = () => Bun.file(join(root, 'contracts/world/kit.json')).text();

  test('the chunker version is 3, and the live plan yields a chunk per signature, megastructure and tower site', async () => {
    expect(CHUNKER).toBe(3);
    const p = await plan(), chunks = (await loadCorpus()).filter(c => c.kind === 'plan'), sections = chunks.map(c => c.section);
    for (const s of p.signatures) expect(sections).toContain(`${p.name} › signature › ${s.id}`);
    for (const m of p.megastructures) expect(sections).toContain(`${p.name} › megastructure › ${m.id}`);
    for (const t of p.standards.buildings.towers.sites) expect(sections).toContain(`${p.name} › tower site › ${t.id}`);
    expect(sections).toContain(`${p.name} › tower sites`);
    const hq = chunks.find(c => c.section.endsWith('signature › space-hq'))!;
    expect(hq.text).toContain('crown: atom'); expect(hq.text).toContain('kit: SpaceHQ'); expect(hq.text).toContain('252');
  });
  test('additive: a plan without signatures, megastructures or tower sites chunks as before (recorded runs keep their keys)', async () => {
    const old = await Bun.file(join(root, 'tools/levels/fixtures/harbour-district.plan.json')).text();
    expect(JSON.parse(old).signatures).toBeUndefined();
    const sections = planChunks('p.json', old).map(c => c.section);
    expect(sections.some(s => /signature|megastructure|tower site/.test(s))).toBe(false);
    const live = planChunks('p.json', JSON.stringify(await plan()));
    const base = live.filter(c => !/signature|megastructure|tower site/.test(c.section)).length;
    expect(live.length).toBeGreaterThan(base);
  });
  test('kit chunks: one per category and one per adopted piece, with size, tags and status; no hashes, files or candidates as pieces', async () => {
    const kit = JSON.parse(await kitText()), chunks = kitChunks('contracts/world/kit.json', await kitText());
    const adopted = kit.assets.filter((a: any) => a.status === 'adopted'), categories = new Set(kit.assets.map((a: any) => a.category));
    expect(chunks).toHaveLength(categories.size + adopted.length);
    const needle = chunks.find(c => c.section === 'DecoKit › Highrise › Tower_Needle')!;
    expect(needle.text).toContain('size: 34.2 × 175.6 × 34.2 m'); expect(needle.text).toContain('tags: use tower'); expect(needle.text).toContain('(adopted)');
    const candidate = kit.assets.find((a: any) => a.status === 'candidate')!;
    expect(chunks.some(c => c.section.endsWith(`› ${candidate.id}`))).toBe(false);
    expect(chunks.find(c => c.section === `DecoKit › ${candidate.category}`)!.text).toContain(`candidate: `);
    for (const c of chunks) { expect(c.text).not.toMatch(/sha256|\.fbx|\.prefab|[0-9a-f]{64}/); expect(c.kind).toBe('kit'); }
    expect(new Set(chunks.map(c => c.key)).size).toBe(chunks.length);
  });
  test('the pinned digest is small, idempotent, and gives the same chunks as kit.json itself', async () => {
    const raw = await kitText(), digest = kitDigest(raw);
    expect(digest.length).toBeLessThan(raw.length / 10); expect(kitDigest(digest)).toBe(digest);
    expect(kitChunks('k.json', digest)).toEqual(kitChunks('k.json', raw));
    const adopt = JSON.parse(raw); adopt.assets.find((a: any) => a.status === 'candidate').status = 'adopted';
    expect(kitChunks('k.json', JSON.stringify(adopt)).length).toBe(kitChunks('k.json', raw).length + 1); // a later adoption changes the live corpus, not a pinned digest's
  });
  test('loadCorpus reads the kit through the override map, so a pinned digest stands in for kit.json', async () => {
    const live = await loadCorpus(), raw = await kitText(), adopt = JSON.parse(raw);
    adopt.assets.find((a: any) => a.status === 'candidate').status = 'adopted';
    const edited = await loadCorpus({ 'contracts/world/kit.json': JSON.stringify(adopt) }), pinned = await loadCorpus({ 'contracts/world/kit.json': kitDigest(raw) });
    expect(edited.length).toBe(live.length + 1); expect(pinned).toEqual(live);
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

describe('images in a crew call', () => {
  const png = new Uint8Array([0x89, 0x50, 0x4e, 0x47, 1, 2, 3, 4]);
  test('the arguments are the same without pictures; with them only the input format changes, and the call stays tool-less', async () => {
    const c = await card('Datum'), schema = cliSchema(z.object({ ok: z.boolean() })), plain = claudeArgs(c, schema), withImages = claudeArgs(c, schema, undefined, undefined, true);
    expect(plain).not.toContain('--input-format');
    expect(withImages.filter(a => !['--input-format', 'stream-json'].includes(a))).toEqual(plain.filter(a => !['--input-format', 'stream-json'].includes(a)));
    expect(withImages[withImages.indexOf('--input-format') + 1]).toBe('stream-json');
    expect(withImages).toContain('--tools='); expect(withImages).toContain('--strict-mcp-config'); expect(withImages).not.toContain('--allowedTools'); expect(withImages).not.toContain('--add-dir');
  });
  test('one stream-json user message: the images first, in order, as base64 blocks, then the prompt text', () => {
    const line = userMessage('PROMPT', [{ mediaType: 'image/png', data: png }, { mediaType: 'image/jpeg', data: new Uint8Array([9, 9]) }]), m = JSON.parse(line);
    expect(line.endsWith('\n')).toBe(true); expect(m).toMatchObject({ type: 'user', message: { role: 'user' } });
    expect(m.message.content.map((b: any) => b.type)).toEqual(['image', 'image', 'text']); expect(m.message.content[2].text).toBe('PROMPT');
    expect(m.message.content[0].source).toMatchObject({ type: 'base64', media_type: 'image/png' }); expect(Buffer.from(m.message.content[0].source.data, 'base64')).toEqual(Buffer.from(png));
  });
  test('loadImages pins sha256 and size, and refuses a missing file, an unknown type and an oversize picture', async () => {
    const dir = await mkdtemp(join(tmpdir(), 'img-')), ok = join(dir, 'a.png'), big = join(dir, 'big.jpg');
    await Bun.write(ok, png); await Bun.write(big, new Uint8Array(MAX_IMAGE_BYTES + 1)); await Bun.write(join(dir, 'x.tiff'), png);
    const [x] = await loadImages([ok]); expect(x).toMatchObject({ path: ok, mediaType: 'image/png', bytes: 8, sha256: sha256(png) });
    await expect(loadImages([join(dir, 'none.png')])).rejects.toThrow('not found'); await expect(loadImages([join(dir, 'x.tiff')])).rejects.toThrow('unsupported type');
    await expect(loadImages([big])).rejects.toThrow('over the');
    expect(await loadImages()).toEqual([]); expect(imageNote([])).toBe(''); expect(imageNote([x!])).toContain(sha256(png).slice(0, 12));
  });
  test('invokeClaude sends the message on stdin, with the input format flag, and without pictures sends the plain prompt', async () => {
    const dir = await mkdtemp(join(tmpdir(), 'fake-claude-img-')), bin = join(dir, 'claude');
    await Bun.write(bin, `#!/bin/sh\ncat > "${dir}/stdin.txt"\necho "$@" > "${dir}/args.txt"\nprintf '%s\\n' '${JSON.stringify(init())}' '${JSON.stringify(result())}'\n`);
    await chmod(bin, 0o755);
    const env = { PATH: process.env.PATH, CLAUDE_BIN: bin }; // what the guard judges is what the child gets
    const schema = z.object({ ok: z.boolean() }), run = await invokeClaude(await card('Fable'), 'PROMPT BODY', schema, undefined, [{ path: 'a.png', mediaType: 'image/png', bytes: 8, sha256: sha256(png), data: png }], undefined, env);
    expect(run.ok).toBe(true); expect(run.tools).toEqual(['StructuredOutput']);
    expect(await Bun.file(join(dir, 'args.txt')).text()).toContain('--input-format stream-json');
    expect(JSON.parse(await Bun.file(join(dir, 'stdin.txt')).text()).message.content.map((b: any) => b.type)).toEqual(['image', 'text']);
    await invokeClaude(await card('Fable'), 'PLAIN', schema, undefined, [], undefined, env);
    expect(await Bun.file(join(dir, 'stdin.txt')).text()).toBe('PLAIN'); expect(await Bun.file(join(dir, 'args.txt')).text()).not.toContain('--input-format');
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
    const env = { PATH: process.env.PATH, CLAUDE_BIN: bin };
    const run = await invokeClaude(await card('Fable'), 'PROMPT BODY', z.object({ ok: z.boolean() }), undefined, [], undefined, env);
    expect(run.ok).toBe(true);
    expect(await Bun.file(join(dir, 'stdin.txt')).text()).toBe('PROMPT BODY');
    expect((await Bun.file(join(dir, 'cwd.txt')).text()).trim()).toBe(root);
    expect((await Bun.file(join(dir, 'mds.txt')).text()).trim()).toBe('1');
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

describe('live call guard', () => {
  const agent = { CLAUDECODE: '1' }, stub = join(root, 'tools/levels/test/claude-stub.ts');
  const schema = z.object({ ok: z.boolean() });
  /** A process that has already answered, for the injected spawn: no real binary is ever started. */
  const answered = () => {
    const text = (t: string) => new Response(t).body!;
    return { stdout: text(`${JSON.stringify(init())}\n${JSON.stringify(result())}\n`), stderr: text(''), exited: Promise.resolve(0) };
  };

  test('an agent session may not start a live call, unless it uses a stand-in or opts in; a person in a terminal is unaffected', () => {
    const refusal = liveGuard(agent)!;
    expect(refusal).toContain('refusing a live model call');
    expect(refusal).not.toContain('SUBORNED_LIVE'); // names no way round it
    expect(refusal).not.toMatch(/=1|bun run/);
    expect(liveGuard({ ...agent, CLAUDE_BIN: stub })).toBeUndefined(); // a stand-in says so itself
    expect(liveGuard({ ...agent, SUBORNED_LIVE: '1' })).toBeUndefined();
    expect(liveGuard({ ...agent, SUBORNED_LIVE: '0' })).toBeDefined();
    expect(liveGuard({})).toBeUndefined();
    expect(liveGuard({ CLAUDECODE: '' })).toBeUndefined();
  });
  test('a truthy CLAUDE_BIN alone does not disable the guard: only a binary that says it is a stand-in does', () => {
    expect(liveGuard({ ...agent, CLAUDE_BIN: 'claude' })).toBeDefined();
    expect(liveGuard({ ...agent, CLAUDE_BIN: '/no/such/claude' })).toBeDefined();
    expect(liveGuard({ ...agent, CLAUDE_BIN: process.execPath })).toBeDefined(); // a real executable that is no stand-in
    expect(isLive({ CLAUDE_BIN: stub })).toBe(false);
    expect(isLive({ CLAUDE_BIN: process.execPath })).toBe(true);
    expect(isLive({})).toBe(true);
  });
  test('invokeClaude refuses before it starts anything; opted in, the call goes through (an injected spawn, no process)', async () => {
    const started: string[][] = [], spawn: Spawn = cmd => { started.push(cmd); return answered(); };
    const ask = async (env: Record<string, string>) => invokeClaude(await card('Fable'), 'PROMPT', schema, undefined, [], spawn, env);
    await expect(ask(agent)).rejects.toThrow('refusing a live model call');
    await expect(ask({ ...agent, CLAUDE_BIN: 'claude' })).rejects.toThrow('refusing a live model call');
    expect(started).toEqual([]);
    expect((await ask({ ...agent, SUBORNED_LIVE: '1' })).ok).toBe(true);
    expect((await ask({})).ok).toBe(true); // a person in a terminal
    expect(started).toHaveLength(2);
  });
  test('the binary and the environment the child gets are the ones the guard judged, not process.env (N4)', async () => {
    const seen: { cmd: string[]; env: Record<string, string | undefined> }[] = [];
    const spawn: Spawn = (cmd, o) => { seen.push({ cmd, env: o.env }); return answered(); };
    const mine = { CLAUDECODE: '1', CLAUDE_BIN: stub, CLAUDE_MODEL: 'sonnet', MARK: 'here' };
    const was = { bin: process.env.CLAUDE_BIN, model: process.env.CLAUDE_MODEL };
    process.env.CLAUDE_BIN = '/elsewhere/claude'; process.env.CLAUDE_MODEL = 'opus';
    try { await invokeClaude(await card('Fable'), 'PROMPT', schema, undefined, [], spawn, mine); } finally {
      for (const [k, v] of [['CLAUDE_BIN', was.bin], ['CLAUDE_MODEL', was.model]] as const) {
        if (v === undefined) delete process.env[k]; else process.env[k] = v;
      }
    }
    const [call] = seen;
    expect(call!.cmd[0]).toBe(stub); // a guard satisfied by the stand-in starts the stand-in
    expect(call!.cmd[call!.cmd.indexOf('--model') + 1]).toBe('sonnet');
    expect(call!.env).toMatchObject({ CLAUDE_BIN: stub, MARK: 'here', CLAUDE_CODE_DISABLE_CLAUDE_MDS: '1' });
    expect(call!.env.CLAUDE_MODEL).toBe('sonnet');
  });
});
