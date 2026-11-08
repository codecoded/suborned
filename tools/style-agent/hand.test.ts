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
import { GUARD_BUSY_18, RADIO_18 } from './short-hand18';
import { runShort } from './short-loop';
import { sameRubric, SHORT_NOW, type ShortBrief, type ShortEvaluation12, type ShortGuideVersion, type ShortResult } from './short-types';
import type { Authorization } from './types';

// Guides /17 and /18 (brief 102): a family's failing lines written by hand, scored once by the judge with no writer call, kept out of every later writer run; from /18 a hand
// run scores its family as it plays, its hand rows included. Four rounds of hand edits are on file, each later one partly rewriting an earlier one (2026-11-09: the third,
// the guard's no-visit lines, true wherever Inez stands; the fourth, Inez's busy lines, true wherever he is). Labelled test data only: no model is called.
const HANDS = ['briefs/102/fable/hand-barks.json', 'briefs/102/fable/hand-barks-2.json', 'briefs/102/fable/hand-barks-3.json', 'briefs/102/fable/hand-barks-4.json'], MODEL = 'claude-opus-5-5', text = JSON.parse(readFileSync(join(root, TEXT_FILE), 'utf8'));
const rounds = HANDS.map(f => HandEdits.parse(JSON.parse(readFileSync(join(root, f), 'utf8'))).edits), edits = rounds.flat();
/** Where a round's line ended up: its words, or a later round's rewrite of them. */
const final = (round: number, key: string, line: string) => rounds.slice(round + 1).reduce((l, es) => es.find(e => e.key === key && e.before === l)?.after ?? l, line);
const approval: Authorization = { provider: 'hand', model: 'fable', approvedBy: 'UNIT TEST, not an approval', judge: { provider: 'claude', model: MODEL } };
const guide = loadShortGuide(root), store = mkdtempSync(join(tmpdir(), 'suborned-hand-store-')), evidence = archiveEvidence(root, guide, store, roleFiles(approval));
/** text.json as it stood before the hand edits of round `from` on: those rounds' lines back, and a row unmarked unless an earlier round edited it. */
const before = (from = 0) => {
  const t = structuredClone(text);
  for (const r of t.dialogue) if (r.hand) {
    if (!rounds.slice(0, from).some(es => es.some(e => e.key === barkKey(r)))) delete r.hand;
    r.lines = r.lines.map((l: string) => rounds.slice(from).reverse().reduce((x, es) => es.find(e => e.key === barkKey(r) && e.after === x)?.before ?? x, l));
  }
  return t;
};
const briefsFrom = (t: unknown, version: ShortGuideVersion = SHORT_NOW, hand = false) => { const read = readerFor(root); return buildShortBriefs(f => f === TEXT_FILE ? JSON.stringify(t) : read(f), gddNames(root), version, hand); };
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
/** A hand run on today's files (the archive's), so it rebuilds as recorded: one edit of the family's first line. */
async function handRun(brief: ShortBrief): Promise<ShortResult> {
  const first = brief.items[0]!, edit = { family: brief.id, key: first.key, before: first.current.lines![0]!, after: 'Was that a shot? I heard it plainly.', kind: 'plan', why: 'a test edit' };
  const invoke = claudeJudge(MODEL, approval.approvedBy, fakeClaude([scored]), { SUBORNED_LIVE: '1', CLAUDE_BIN: '/opt/claude-unit-test' });
  return runShort({ guide, brief, invoke, limits: { ...defaults, maxCalls: 1, maxRevisions: 0 }, mode: 'live', evidence, authorization: approval, now: () => clock++,
    base: { why: text.why, dialogue: text.dialogue }, hand: { by: 'Fable, by hand', why: 'a test', set: handSet(brief, [edit]) } });
}
const family = (id: string, hand = true) => loadShortBriefs(root, SHORT_NOW, hand).find(b => b.id === id)!;

describe('guides /17 and /18: lines written by hand (brief 102)', () => {
  test('/18 reads as /17 in its rubric, and /17 as /16, so their runs and /14–/16 runs share it (/19 changes the why kind only: lozenge.test.ts)', () => {
    const g16 = loadShortGuide(root, 16), g17 = loadShortGuide(root, 17), g18 = loadShortGuide(root, 18);
    expect(SHORT_NOW).toBeGreaterThanOrEqual(18);
    expect({ ...g16, schema: g17.schema }).toEqual(g17); expect({ ...g17, schema: g18.schema }).toEqual(g18);
    expect(sameRubric(g16, g17)).toBe(true); expect(sameRubric(g17, g18)).toBe(true);
  });
  test('a hand set is today\'s family with each edit in place of the line it names, and an edit of a line that isn\'t there is refused', () => {
    const brief = briefsFrom(before()).find(b => b.id === 'bark/summons')!, set = handSet(brief, rounds[0]!);
    expect(set.items.map(i => i.key)).toEqual(brief.items.map(i => i.key));
    const no = set.items.find(i => i.key === 'summon-refused/guard/summon-no-visit')!.fields[0]!.lines;
    expect(no).toEqual(rounds[0]!.filter(e => e.key === 'summon-refused/guard/summon-no-visit').map(e => e.after!));
    expect(set.items.find(i => i.key === 'summon-refused/guard/summon-came-once')!.fields[0]!.lines[1]).toBe('Not again this shift, Inez.'); // a line nobody edited stays
    expect(() => handSet(brief, [{ ...rounds[0]!.find(e => e.family === 'bark/summons')!, before: 'Not a line of today.' }])).toThrow('is not one of today\'s lines');
  });
  test('/18: a hand run\'s family has its hand rows as items, so a second round can rewrite their lines; a writer run\'s family and a /17 hand run\'s do not', () => {
    const mid = before(1), hand = briefsFrom(mid, 18, true), writer = briefsFrom(mid, 18), at17 = briefsFrom(mid, 17, true);
    for (const id of ['bark/witnesses-crowd', 'bark/summons']) {
      const mine = rounds[1]!.filter(e => e.family === id), b = hand.find(x => x.id === id)!;
      expect(handSet(b, mine).items).toHaveLength(b.items.length);
      expect(b.context).not.toContain('Rows Fable wrote by hand');
      for (const other of [writer, at17]) expect(() => handSet(other.find(x => x.id === id)!, mine)).toThrow('is not one of today\'s lines');
    }
  });
  test('/18 tells the rows of the second round as the engine plays them; /17 reads as it ran', () => {
    const at = (v: ShortGuideVersion, k: string) => briefsFrom(before(), v).flatMap(b => b.items).find(i => i.key === k)!.tells;
    expect(at(18, 'report-begins/guard/radio')).toBe(RADIO_18); expect(at(17, 'report-begins/guard/radio')).toContain('staff radio the desk');
    expect(at(18, 'summon-refused/waitress/summon-busy')).toContain('already away on her last call'); expect(at(18, 'summon-refused/waitress/summon-busy')).not.toContain(GUARD_BUSY_18);
    expect(at(18, 'summon-refused/guard/summon-busy')).toContain(GUARD_BUSY_18); expect(at(17, 'summon-refused/guard/summon-busy')).toContain('he can\'t leave the door just now');
    expect(at(18, 'summon-refused/guard/summon-too-far')).toContain('he is at his door'); expect(at(18, 'summon-refused/guard/summon-came-once')).toBe(at(17, 'summon-refused/guard/summon-came-once'));
  });
  test('a hand run calls only the judge, once, and rebuilds from its archive with its family as it plays; its set, family or approval edited, it fails', async () => {
    for (const id of ['bark/aftermath', 'bark/summons']) {
      const r = await handRun(family(id));
      expect(r.calls.map(c => c.stage)).toEqual(['evaluate']); expect(r.rounds).toHaveLength(1); expect(r.status).toBe('INCOMPLETE');
      expect(r.rounds[0]!.set).toEqual(r.hand!.set);
      expect(r.brief.items.some(i => (text.dialogue as { hand?: string }[]).find(x => barkKey(x as never) === i.key)?.hand)).toBe(id === 'bark/summons'); // its hand rows, as it plays
      expect((await verifyShort(r, root, store)).errors).toEqual([]);
      const forged: ((x: ShortResult) => void)[] = [
        x => { x.hand!.set.items[0]!.fields[0]!.lines[0] = 'A line nobody judged.'; }, x => { x.rounds[0]!.set.items[0]!.fields[0]!.lines[0] = 'A line nobody judged.'; },
        x => { x.authorization!.provider = 'codex'; }, x => { delete x.hand; }, x => { x.calls[0]!.stage = 'refine'; },
      ];
      for (const f of forged) { const x = structuredClone(r); f(x); expect((await verifyShort(x, root, store)).ok).toBe(false); }
    }
  });
  test('a row marked by hand is not an item of a writer run: its family\'s context gives its words, and the summons\' no-visit rows are told as the engine plays them', () => {
    const now = loadShortBriefs(root), then = briefsFrom(before()), hand = loadShortBriefs(root, SHORT_NOW, true);
    const marked = (text.dialogue as { hand?: string; lines: string[] }[]).filter(r => r.hand);
    expect(marked.length).toBeGreaterThan(0);
    let given = 0;
    for (const r of marked) {
      const k = barkKey(r as never), ctx = now.find(b => b.context.includes(`${k}:`));
      expect(now.some(b => b.items.some(i => i.key === k))).toBe(false);
      expect(then.some(b => b.items.some(i => i.key === k))).toBe(true);
      expect(hand.some(b => b.items.some(i => i.key === k))).toBe(true);
      if (ctx) { given++; for (const l of r.lines) expect(ctx.context).toContain(JSON.stringify(l)); }
    }
    expect(given).toBeGreaterThan(0); // the crowd's family still has a writer's item; every summons row is by hand or Sky's, so a writer has nothing left there
    expect(now.some(b => b.id === 'bark/summons')).toBe(false); expect(hand.some(b => b.id === 'bark/summons')).toBe(true);
    const tells = (k: string) => then.find(b => b.id === 'bark/summons')!.items.find(i => i.key === k)!.tells;
    expect(tells('summon-refused/guard/summon-no-visit')).toContain('answers the Fixer');
    expect(tells('summon-refused/waitress/summon-no-visit')).toContain('she is not on that call');
  });
  test('text.json carries every hand edit where it ended up, marked, with no line it replaced; apply never writes a hand row', async () => {
    rounds.forEach((es, n) => {
      for (const e of es) {
        const row = (text.dialogue as { lines: string[]; hand?: string }[]).find(r => barkKey(r as never) === e.key)!;
        expect(row.hand?.startsWith(HAND_WORDS), e.key).toBe(true);
        expect(row.lines).toContain(final(n, e.key, e.after!)); expect(row.lines).not.toContain(e.before);
      }
    });
    // an accepted run that rewrites every hand row: apply holds them all, and writes the same row once its mark is gone
    const keys = [...new Set(edits.map(e => e.key))], raw = readFileSync(join(root, TEXT_FILE), 'utf8'), dir = mkdtempSync(join(tmpdir(), 'suborned-hand-apply-'));
    const run = { brief: { id: 'bark/hand-test', kind: 'bark', items: keys.map(key => ({ key })) }, startedAt: 0,
      rounds: [{ set: { items: keys.map(key => ({ key, cut: false, reason: '', fields: [{ id: 'lines', lines: [1, 2, 3, 4].slice(0, (text.dialogue as never[]).find(r => barkKey(r) === key)!['lines' as never]['length']).map(n => `${key} line ${n}.`) }] })) } }] } as unknown as ShortResult;
    const apply = async (t: string) => { const file = join(dir, 'text.json'); writeFileSync(file, t); await applyShort(false, { runs: new Map([[run.brief.id, { run, path: root }]]), file }); return JSON.parse(readFileSync(file, 'utf8')); };
    const held = await apply(raw);
    rounds.forEach((es, n) => { for (const e of es) expect(held.dialogue.find((x: never) => barkKey(x) === e.key).lines, e.key).toContain(final(n, e.key, e.after!)); });
    // a dialogue row is one line of the file, `{"event":…}` (from guide /22 a why cause carries `hand` too: hand-whys.test.ts)
    const one = keys[0]!, unmarked = raw.split('\n').map(l => l.includes(`"hand":`) && l.trimStart().startsWith('{"event":') && barkKey(JSON.parse(l.trim().replace(/,$/, ''))) === one ? l.replace(/,"hand":"[^"]*"/, '') : l).join('\n');
    expect((await apply(unmarked)).dialogue.find((x: never) => barkKey(x) === one).lines[0]).toBe(`${one} line 1.`);
  });
});
