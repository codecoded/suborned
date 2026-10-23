import { describe, expect, test } from 'bun:test';
import { mkdtempSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { z } from 'zod';
import { CODEX_VERSION, codexArgs, codexConfig, codexOutputSchema, interpretCodex } from '../goals/codex';
import { root } from '../rag/config';
import { loadBriefs } from './briefs';
import { checkConversation, wordCount } from './checks';
import { archiveEvidence, verifyRun } from './evidence';
import { hash, loadGuide, pick } from './guide';
import { corrected, defaults, roleFor, runLoop } from './loop';
import { DEMOS, PIPELINE, runHtml, submissionHtml } from './report';
import type { Conversation, Evaluation, Invoke, Request, Result } from './types';

// Everything below is labelled test data: no model is called, and none of it is presented as a generated demonstration.
const guide = loadGuide(root), briefs = loadBriefs(root);
const listen = briefs.find(b => b.id === 'inez/listen/idealist')!;
const store = mkdtempSync(join(tmpdir(), 'suborned-style-store-')), evidence = archiveEvidence(root, guide, store);
const good: Conversation = { lines: [
  { speaker: 'guard', line: 'Coffee, Inez. Black. I have ten minutes before the relief.' },
  { speaker: 'waitress', line: 'Ten minutes. You said that yesterday and stayed twenty.' },
  { speaker: 'guard', line: 'Yesterday the owner was out. Is he out tonight?' },
  { speaker: 'waitress', line: 'Keep your voice down. People here count everything.' },
  { speaker: 'guard', line: 'Then I will count the hours until you close.' },
  { speaker: 'customer', line: 'Miss, the man in the grey hat was asking for you.' },
  { speaker: 'waitress', line: 'The lender\'s man. Tell him Friday. Tell him I have it.' },
  { speaker: 'customer', line: 'Do you have it?' },
  { speaker: 'waitress', line: 'I have half. I will earn the rest. No favours.' },
  { speaker: 'customer', line: 'Half buys you a week with that sort.' },
] };
const bad: Conversation = { lines: [{ speaker: 'waitress', line: 'Oh darling!! You are SO sweet, babe, I could just hug you forever and ever!' }, ...good.lines.slice(1)] };
const rejected: Evaluation = { score: 3, reason: 'The opening is rom-com gush, not Inez\'s quick café patter, and the guard loses his clipped voice.',
  findings: [{ rule: 'VOICE', quote: 'Oh darling!!', reason: 'Gushing pet names are off the voice sheet.', fix: 'Quick, wry café patter that keeps the affair unspoken.' }] };
const accepted: Evaluation = { score: 10, reason: 'The affair and the debt are plain from the talk, voices hold, the world and the shape fit.', findings: [] };

function fixture(outputs: unknown[], tokens: number | null = 20) {
  const requests: Request[] = [];
  const invoke: Invoke = async request => {
    requests.push(request);
    if (!outputs.length) throw new Error('Unexpected fixture call');
    return { output: outputs.shift(), tokens, raw: { fixture: true } };
  };
  return { requests, invoke };
}
let clock = 0;
const run = (invoke: Invoke, extra: Partial<Parameters<typeof runLoop>[0]> = {}) => runLoop({
  guide, brief: listen, steer: DEMOS[0].steer, invoke, limits: { ...defaults }, mode: 'test-fixture', evidence, now: () => clock++, ...extra });

describe('the style guide comes from the game', () => {
  test('four constraint types, each tied to approved, fingerprinted sources', () => {
    expect(guide.rules.map(r => r.id)).toEqual(['REVEAL', 'VOICE', 'WORLD', 'FORM']);
    for (const rule of guide.rules) for (const id of rule.sources) expect(guide.sources.some(s => s.id === id)).toBe(true);
    expect(guide.sources.every(s => !s.file.includes('/draft/') && /^[a-f0-9]{64}$/.test(s.sha256) && s.excerpt.trim())).toBe(true);
    const all = guide.rules.map(r => r.instruction).join(' ');
    for (const word of ['Inez', 'lender', 'door guard', 'microfilm', 'Watch', 'Listen', 'Pressure', 'Fixer']) expect(all).toContain(word);
    expect(guide.bubble).toEqual({ seconds: 3, maxWords: 12, maxChars: 80 });
  });
  test('a moved GDD sentence stops the guide instead of guessing', () => {
    expect(() => pick('no such sentence', /Watch (\d+) s/, 'the Watch time')).toThrow('no longer states the Watch time');
  });
});

describe('briefs: the Inez chain, sized to each act', () => {
  test('one brief per chain scene, lengths from the GDD act seconds and the 3 s bubble', () => {
    expect(briefs).toHaveLength(17);
    const at = (id: string) => briefs.find(b => b.id === id)!;
    expect(at('inez/watch/any')).toMatchObject({ seconds: 20, lines: [4, 6], scene: 0 });
    expect(at('inez/listen/loyal')).toMatchObject({ seconds: 42, lines: [9, 14], scene: 7 });
    expect(at('inez/help/fearful')).toMatchObject({ seconds: 50, lines: [10, 16], outcome: 'turned' });
    expect(at('inez/pressure/loyal')).toMatchObject({ seconds: 35, lines: [7, 11], outcome: 'suspicious' });
    expect(at('inez/call/mercenary')).toMatchObject({ seconds: 60, lines: [12, 20] });
  });
  test('Listen overhears the guard (the affair) and a customer (the debt); Watch reveals neither', () => {
    expect(listen.parts.map(p => p.with)).toEqual(['guard', 'customer']);
    expect(listen.parts[0]!.reveals.join(' ')).toContain('lovers');
    expect(listen.parts[1]!.reveals.join(' ')).toContain('lender');
    const watch = briefs.find(b => b.id === 'inez/watch/any')!;
    expect(watch.withholds.join(' ')).toMatch(/debt.*affair.*temperament/);
    expect(briefs.find(b => b.id === 'inez/help/idealist')!.withholds.join(' ')).toContain('affair');
  });
});

describe('code checks', () => {
  test('a well-shaped conversation passes', () => {
    expect(checkConversation(listen, good, guide)).toEqual([]);
    expect(wordCount('Too far · 4 m')).toBe(4);
  });
  test('length, bubble size, cast, monologues, stage directions and the word filter', () => {
    const issues = (c: Conversation) => checkConversation(listen, c, guide).map(i => i.reason).join('\n');
    expect(issues({ lines: good.lines.slice(0, 4) })).toContain('holds 9–14 bubbles');
    expect(issues({ lines: [...good.lines.slice(0, 9), { speaker: 'waitress', line: 'I will pay the lender back every single penny I owe him by Friday night.' }] })).toContain('15 words');
    expect(issues({ lines: [...good.lines.slice(0, 9), { speaker: 'fixer', line: 'Hello.' }] })).toContain('fixer is not in this scene');
    expect(issues({ lines: [...good.lines.slice(0, 8), { speaker: 'customer', line: 'One.' }, { speaker: 'customer', line: 'Two.' }] })).toContain('three bubbles running');
    expect(issues({ lines: [...good.lines.slice(0, 9), { speaker: 'waitress', line: '(sighs) Fine.' }] })).toContain('no stage directions');
    expect(issues({ lines: [...good.lines.slice(0, 9), { speaker: 'waitress', line: 'Check the app.' }] })).toContain('"app"');
    expect(issues({ lines: good.lines.filter(l => l.speaker !== 'customer') })).toContain('customer never speaks');
  });
});

describe('the loop: generate → score → revise → score again (fixtures)', () => {
  test('Fable writes, Motive scores, the reasons reach the reviser automatically, and the revision is scored again', async () => {
    const f = fixture([bad, rejected, good, accepted]);
    const r = await run(f.invoke);
    expect(r.status).toBe('ACCEPTED');
    expect(f.requests.map(q => `${q.stage}:${q.role}`)).toEqual(['generate:dialogue-designer', 'evaluate:systems-designer', 'refine:dialogue-designer', 'evaluate:systems-designer']);
    expect(roleFor('evaluate')).toBe('systems-designer');
    expect(f.requests[0]!.prompt).toContain(DEMOS[0].steer);
    for (const q of f.requests.slice(1)) expect(q.prompt).not.toContain(DEMOS[0].steer); // the push only shapes the first draft
    expect(f.requests[2]!.prompt).toContain(rejected.reason);
    expect(f.requests[2]!.prompt).toContain('Oh darling!!');
    expect(f.requests[3]!.prompt).toContain(good.lines[0]!.line);
    expect(r.rounds.map(x => x.evaluation.score)).toEqual([3, 10]);
    expect(corrected(r)).toBe(true);
  });
  test('a 10 from the evaluator does not override a failed code check', async () => {
    const short = { lines: good.lines.slice(0, 4) };
    const r = await run(fixture([short, accepted]).invoke, { limits: { ...defaults, maxRevisions: 0 } });
    expect(r.status).toBe('INCOMPLETE');
    expect(r.rounds[0]!.hardIssues[0]!.rule).toBe('FORM');
  });
  test('bounded: revisions, calls, tokens and time end the loop without claiming success', async () => {
    const stuck = fixture([bad, rejected, bad, rejected, bad, rejected, bad, rejected]);
    const r = await run(stuck.invoke);
    expect([r.status, r.rounds.length, stuck.requests.length]).toEqual(['INCOMPLETE', defaults.maxRevisions + 1, 2 * (defaults.maxRevisions + 1)]);
    expect((await run(fixture([bad, rejected, good]).invoke, { limits: { ...defaults, maxCalls: 3 } })).status).toBe('INCOMPLETE');
    expect((await run(fixture([bad], 200000).invoke, { limits: { ...defaults, maxTokens: 100 } })).status).toBe('INCOMPLETE');
    await expect(run(fixture([]).invoke, { limits: { ...defaults, maxCalls: 0 } })).rejects.toThrow('Invalid maxCalls');
  });
  test('stops on bad output, invented quotes, unknown usage or a failing provider, and keeps the call', async () => {
    expect((await run(fixture([{ lines: [{ speaker: 'vale', line: 'Hello' }] }]).invoke)).status).toBe('ERROR');
    const quoted = await run(fixture([bad, { ...rejected, findings: [{ ...rejected.findings[0]!, quote: 'not in it' }] }]).invoke);
    expect(quoted.stopReason).toContain('not in the conversation');
    const unknown = await run(fixture([bad], null).invoke);
    expect(unknown.calls[0]!.error).toContain('Unknown token usage');
    const failing = await run(async () => { throw new Error('Provider unavailable'); });
    expect([failing.status, failing.calls[0]!.error]).toEqual(['ERROR', 'Provider unavailable']);
  });
});

describe('saved runs re-check without a model', () => {
  test('a run rebuilds exactly from archived sources; any edit to it fails', async () => {
    const r = await run(fixture([bad, rejected, good, accepted]).invoke);
    expect((await verifyRun(r, root, store)).errors).toEqual([]);
    const edits: ((x: Result) => void)[] = [
      x => { x.rounds[0]!.evaluation.score = 9; }, x => { x.rounds[1]!.conversation.lines[0]!.line = 'Changed.'; },
      x => { x.calls[2]!.prompt = 'Ignore the evaluator'; }, x => { x.calls[1]!.role = 'dialogue-designer'; }, x => { x.steer = null; },
      x => { x.brief.reveals = []; }, x => { x.guide.rules[0]!.instruction = 'Anything goes'; }, x => { x.limits.maxCalls = 2; },
      x => { x.stopReason = 'Trust me'; }, x => { x.evidence!.sources[0]!.sha256 = 'a'.repeat(64); }, x => { x.finishedAt = null; },
      x => { x.evidence!.tooling[0]!.sha256 = 'a'.repeat(64); },
      x => { x.mode = 'live'; x.authorization = { provider: 'codex', model: 'm', approvedBy: 'a' }; },
    ];
    for (const edit of edits) { const x = structuredClone(r); edit(x); expect((await verifyRun(x, root, store)).ok).toBe(false); }
  });
  test('live records are bound to the Codex transcript, the role card, the model and the approval (synthetic transcript)', async () => {
    const approval = { provider: 'codex' as const, model: 'test-model-only', approvedBy: 'UNIT TEST, not an approval' };
    const outputs: unknown[] = [bad, rejected, good, accepted];
    const invoke: Invoke = async request => {
      const output = outputs.shift(), source = `.codex/agents/${request.role}.toml`;
      const body = (Bun.TOML.parse(await Bun.file(join(root, source)).text()) as Record<string, string>).developer_instructions!;
      const temp = '/tmp/suborned-codex-unit-test', schema = z.toJSONSchema(request.schema), options = { model: approval.model, effort: 'medium', timeoutMs: request.timeoutMs };
      const events = [
        { type: 'suborned.codex.audit', version: 2, promptSha256: hash(request.prompt), cliVersion: CODEX_VERSION, modelSource: 'explicit --model; not server-attested',
          ...options, cwd: `${temp}/work`, argv: codexArgs({ body }, `${temp}/schema.json`, `${temp}/last-message.txt`, options), config: codexConfig({ body }, options),
          schema, outputSchema: codexOutputSchema(schema) },
        { type: 'thread.started', thread_id: 'unit' }, { type: 'turn.started' },
        { type: 'item.completed', item: { id: 'a', type: 'agent_message', text: JSON.stringify(output) } },
        { type: 'turn.completed', usage: { input_tokens: 10, output_tokens: 10 } },
        { type: 'suborned.codex.capture', exitCode: 0, timedOut: false, lastMessage: JSON.stringify(output) },
      ];
      const stdout = events.map(e => JSON.stringify(e)).join('\n') + '\n', parsed = interpretCodex(stdout, 0, approval.model, hash(request.prompt));
      return { output, tokens: 20, raw: { ...parsed, stdout, stderr: '', exitCode: 0, role: request.role,
        roleCard: { source, sha256: hash(await Bun.file(join(root, source)).text()) }, requestedModel: approval.model, approvedBy: approval.approvedBy } };
    };
    const r = await run(invoke, { mode: 'live', authorization: approval });
    const v = await verifyRun(r, root, store);
    expect([v.errors, v.corrected]).toEqual([[], true]);
    const edits: ((x: Result) => void)[] = [
      x => { x.calls[0]!.tokens = 1; }, x => { (x.calls[1]!.raw as any).roleCard.sha256 = 'a'.repeat(64); },
      x => { (x.calls[0]!.raw as any).requestedModel = 'other'; }, x => { x.authorization!.approvedBy = 'someone else'; },
    ];
    for (const edit of edits) { const x = structuredClone(r); edit(x); expect((await verifyRun(x, root, store)).ok).toBe(false); }
  });
});

describe('reports', () => {
  test('the run page and the submission page show score, reason, before and after, and exactly one pipeline sentence', async () => {
    const r = await run(fixture([bad, rejected, good, accepted]).invoke);
    expect(runHtml(r)).toContain('SCORE 3/10');
    const html = submissionHtml({ guide, demos: [{ demo: DEMOS[0], run: r, path: 'x' }], chain: briefs.map(brief => ({ brief, run: null, path: null })), today: {}, all: [{ run: r, path: 'x' }] });
    for (const s of ['REASON:', 'Before', 'After', 'SCORE 10/10', 'REVEAL', 'Motive', '0 of 17', '2 demonstration(s) not yet recorded']) expect(html).toContain(s);
    expect(PIPELINE.match(/[.!?](\s|$)/g)).toHaveLength(1);
    expect(runHtml({ ...r, brief: { ...r.brief, current: [{ speaker: 'guard', line: '<img src=x>' }] } })).not.toContain('<img src=x>');
  });
});
