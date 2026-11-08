import { describe, expect, test } from 'bun:test';
import { mkdtempSync, readFileSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { HAND_WORDS } from '../../contracts/missions/handover/speech.schema.ts';
import { root } from '../rag/config';
import { defaults } from './core';
import { archiveEvidence, roleFiles } from './evidence';
import { readerFor, TEXT_FILE, gddNames } from './guide';
import { claudeJudge, type ClaudeSpawn } from './live-claude';
import { barkKey } from './short-barks';
import { buildShortBriefs, loadShortBriefs } from './short-briefs';
import { applyShort } from './short-apply';
import { handSet, HandEdits } from './short-cli';
import { verifyShort } from './short-evidence';
import { loadShortGuide } from './short-guide';
import { runShort } from './short-loop';
import { sameRubric, SHORT_NOW, type ShortEvaluation12, type ShortResult } from './short-types';
import type { Authorization } from './types';

// Guide /17 (brief 102): a family's failing lines written by hand, scored once by the judge with no writer call, kept out of every later run. Labelled test data only: no model is called.
const HAND = 'briefs/102/fable/hand-barks.json', MODEL = 'claude-opus-5-5', text = JSON.parse(readFileSync(join(root, TEXT_FILE), 'utf8'));
const edits = HandEdits.parse(JSON.parse(readFileSync(join(root, HAND), 'utf8'))).edits;
const approval: Authorization = { provider: 'hand', model: 'fable', approvedBy: 'UNIT TEST, not an approval', judge: { provider: 'claude', model: MODEL } };
const guide = loadShortGuide(root), store = mkdtempSync(join(tmpdir(), 'suborned-hand-store-')), evidence = archiveEvidence(root, guide, store, roleFiles(approval));
/** text.json as it stood before the hand edits: their rows unmarked, today's lines back. */
const before = () => {
  const t = structuredClone(text);
  for (const r of t.dialogue) if (r.hand) { delete r.hand; r.lines = r.lines.map((l: string) => edits.find(e => e.key === barkKey(r) && e.after === l)?.before ?? l); }
  return t;
};
const briefsFrom = (t: unknown) => { const read = readerFor(root); return buildShortBriefs(f => f === TEXT_FILE ? JSON.stringify(t) : read(f), gddNames(root)); };
const scored: ShortEvaluation12 = { scores: { WORTH: 9, VOICE: 10, NATURAL: 10, VARIANTS: 10, MECHANICS: 10, TRUE: 10, NAMES: 10, SHORT: 10 }, score: 9, reason: 'One line is filler.', findings: [] };
function fakeClaude(answers: unknown[]): ClaudeSpawn {
  return (_argv, _o) => {
    const events = [{ type: 'system', subtype: 'init', model: MODEL, tools: ['StructuredOutput'], mcp_servers: [] },
      { type: 'result', subtype: 'success', is_error: false, structured_output: answers.shift(), modelUsage: { [MODEL]: { inputTokens: 5, outputTokens: 50, cacheReadInputTokens: 0, cacheCreationInputTokens: 500 } } }];
    const body = (s: string) => new Response(s).body!;
    return { stdout: body(events.map(e => JSON.stringify(e)).join('\n') + '\n'), stderr: body(''), exited: Promise.resolve(0), kill: () => {} };
  };
}
let clock = 0;
/** A hand run on a family with no hand rows, from the files as they are (the archive's), so it rebuilds as recorded. */
async function handRun(): Promise<ShortResult> {
  const brief = loadShortBriefs(root).find(b => b.id === 'bark/aftermath')!, first = brief.items[0]!;
  const edit = { family: brief.id, key: first.key, before: first.current.lines![0]!, after: 'Was that a shot? I heard it plainly.', kind: 'plan', why: 'a test edit' };
  const invoke = claudeJudge(MODEL, approval.approvedBy, fakeClaude([scored]), { SUBORNED_LIVE: '1', CLAUDE_BIN: '/opt/claude-unit-test' });
  return runShort({ guide, brief, invoke, limits: { ...defaults, maxCalls: 1, maxRevisions: 0 }, mode: 'live', evidence, authorization: approval, now: () => clock++,
    base: { why: text.why, dialogue: text.dialogue }, hand: { by: 'Fable, by hand', why: 'a test', set: handSet(brief, [edit]) } });
}

describe('guide /17: lines written by hand (brief 102)', () => {
  test('/17 is the current protocol and reads as /16 in its rubric, so its runs and /14–/16 runs share it', () => {
    const g16 = loadShortGuide(root, 16);
    expect(SHORT_NOW).toBe(17); expect({ ...g16, schema: guide.schema }).toEqual(guide); expect(sameRubric(g16, guide)).toBe(true);
  });
  test('a hand set is today\'s family with each edit in place of the line it names, and an edit of a line that isn\'t there is refused', () => {
    const brief = briefsFrom(before()).find(b => b.id === 'bark/summons')!, set = handSet(brief, edits);
    expect(set.items.map(i => i.key)).toEqual(brief.items.map(i => i.key));
    const no = set.items.find(i => i.key === 'summon-refused/guard/summon-no-visit')!.fields[0]!.lines;
    expect(no).toEqual(edits.filter(e => e.key === 'summon-refused/guard/summon-no-visit').map(e => e.after));
    expect(set.items.find(i => i.key === 'summon-refused/guard/summon-came-once')!.fields[0]!.lines[1]).toBe('Not again this shift, Inez.'); // a line nobody edited stays
    expect(() => handSet(brief, [{ ...edits.find(e => e.family === 'bark/summons')!, before: 'Not a line of today.' }])).toThrow('is not one of today\'s lines');
  });
  test('a hand run calls only the judge, once, and rebuilds from its archive; its set or approval edited, it fails', async () => {
    const r = await handRun();
    expect(r.calls.map(c => c.stage)).toEqual(['evaluate']); expect(r.rounds).toHaveLength(1); expect(r.status).toBe('INCOMPLETE');
    expect(r.rounds[0]!.set).toEqual(r.hand!.set);
    expect((await verifyShort(r, root, store)).errors).toEqual([]);
    const forged: ((x: ShortResult) => void)[] = [
      x => { x.hand!.set.items[0]!.fields[0]!.lines[0] = 'A line nobody judged.'; }, x => { x.rounds[0]!.set.items[0]!.fields[0]!.lines[0] = 'A line nobody judged.'; },
      x => { x.authorization!.provider = 'codex'; }, x => { delete x.hand; }, x => { x.calls[0]!.stage = 'refine'; },
    ];
    for (const f of forged) { const x = structuredClone(r); f(x); expect((await verifyShort(x, root, store)).ok).toBe(false); }
  });
  test('a row marked by hand is not an item: its family\'s context gives its words, and the summons\' no-visit rows are told as the engine plays them', () => {
    const now = loadShortBriefs(root), then = briefsFrom(before());
    const marked = (text.dialogue as { hand?: string }[]).filter(r => r.hand).map(r => barkKey(r as never));
    expect(marked.length).toBeGreaterThan(0);
    for (const k of marked) {
      expect(now.some(b => b.items.some(i => i.key === k))).toBe(false);
      expect(then.some(b => b.items.some(i => i.key === k))).toBe(true);
    }
    for (const e of edits) expect(now.find(b => b.id === e.family)!.context).toContain(JSON.stringify(e.after));
    const tells = (k: string) => then.find(b => b.id === 'bark/summons')!.items.find(i => i.key === k)!.tells;
    expect(tells('summon-refused/guard/summon-no-visit')).toContain('answers the Fixer');
    expect(tells('summon-refused/waitress/summon-no-visit')).toContain('she is not on that call');
  });
  test('text.json carries every hand edit, marked, with no line it replaced; apply never writes a hand row', async () => {
    for (const e of edits) {
      const row = (text.dialogue as { lines: string[]; hand?: string }[]).find(r => barkKey(r as never) === e.key)!;
      expect(row.hand?.startsWith(HAND_WORDS), e.key).toBe(true);
      expect(row.lines).toContain(e.after); expect(row.lines).not.toContain(e.before);
    }
    // an accepted run that rewrites every hand row: apply holds them all, and writes the same row once its mark is gone
    const keys = [...new Set(edits.map(e => e.key))], raw = readFileSync(join(root, TEXT_FILE), 'utf8'), dir = mkdtempSync(join(tmpdir(), 'suborned-hand-apply-'));
    const run = { brief: { id: 'bark/hand-test', kind: 'bark', items: keys.map(key => ({ key })) }, startedAt: 0,
      rounds: [{ set: { items: keys.map(key => ({ key, cut: false, reason: '', fields: [{ id: 'lines', lines: [1, 2, 3].slice(0, (text.dialogue as never[]).find(r => barkKey(r) === key)!['lines' as never]['length']).map(n => `${key} line ${n}.`) }] })) } }] } as unknown as ShortResult;
    const apply = async (t: string) => { const file = join(dir, 'text.json'); writeFileSync(file, t); await applyShort(false, { runs: new Map([[run.brief.id, { run, path: root }]]), file }); return JSON.parse(readFileSync(file, 'utf8')); };
    const held = await apply(raw);
    for (const e of edits) expect(held.dialogue.find((x: never) => barkKey(x) === e.key).lines, e.key).toContain(e.after);
    const one = keys[0]!, unmarked = raw.split('\n').map(l => l.includes(`"hand":`) && barkKey(JSON.parse(l.trim().replace(/,$/, ''))) === one ? l.replace(/,"hand":"[^"]*"/, '') : l).join('\n');
    expect((await apply(unmarked)).dialogue.find((x: never) => barkKey(x) === one).lines[0]).toBe(`${one} line 1.`);
  });
});
