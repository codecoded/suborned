import { describe, expect, test } from 'bun:test';
import { copyFileSync, mkdirSync, mkdtempSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { SCENE_ROWS, SKY_WORDS, speaks } from '../../contracts/missions/handover/speech.schema.ts';
import { AGENTS, TextFile } from '../../contracts/missions/handover/text.schema.ts';
import { root } from '../rag/config';
import { defaults } from './core';
import { archiveEvidence, writeRun } from './evidence';
import { hash, TEXT_FILE } from './guide';
import { applyShort } from './short-apply';
import { barkKey } from './short-barks';
import { loadShortBriefs } from './short-briefs';
import { checkShort } from './short-checks';
import { waitsForSky } from './short-cli';
import { verifyShort } from './short-evidence';
import { loadShortGuide } from './short-guide';
import { runShort } from './short-loop';
import { AI_WORDS_FILE, AiWords, aiWords } from './short-natural14';
import { shortPrompt } from './short-prompts';
import { BARK_RULES_12, sameRubric, SHORT_NOW, type ShortBrief, type ShortEvaluation12, type ShortResult, type ShortSet } from './short-types';
import type { Invoke } from './types';

// Guide /14 (brief 102's sweep, Sky 2026-11-08: "that noted is AI sounding"): labelled test data only; no model is called.
const g14 = loadShortGuide(root, 14), g13 = loadShortGuide(root, 13), b14 = loadShortBriefs(root, 14), b13 = loadShortBriefs(root, 13);
const text = JSON.parse(readFileSync(join(root, TEXT_FILE), 'utf8')), ai = AiWords.parse(JSON.parse(readFileSync(join(root, AI_WORDS_FILE), 'utf8')));
const rows = text.dialogue as { event: string; speaker: string; lines: string[]; silent?: string; parked?: string; pending?: string; sky?: string }[];
const barks = b14.filter(b => b.kind === 'bark'), family = (id: string) => b14.find(b => b.id === id)!;
const familyOf = (key: string) => barks.find(b => b.items.some(i => i.key === key))!;
const today = (b: ShortBrief): ShortSet => ({ items: b.items.map(i => ({ key: i.key, cut: false, reason: '', fields: i.fields.map(f => ({ id: f.id, lines: (i.current[f.id] ?? []).slice(0, f.count) })) })) });
const edit = (s: ShortSet, key: string, lines: string[]): ShortSet => ({ items: s.items.map(i => i.key !== key ? i : { ...i, fields: i.fields.map(f => ({ ...f, lines })) }) });
const nat = (g: typeof g14) => g.kinds.bark.rules.find(r => r.id === 'NATURAL')!.instruction;

describe('guide /14: AI-sounding words fail too', () => {
  test('/14\'s NATURAL is /13\'s with Sky\'s words on AI-sounding ones; the guide carries them from their file; /13 rebuilds as it ran', () => {
    expect(SHORT_NOW).toBeGreaterThanOrEqual(14);
    expect(nat(g14).startsWith(nat(g13))).toBe(true); expect(nat(g14)).toContain('"that noted is AI sounding"'); expect(nat(g14)).toContain('"noted", "understood", "proceeding", "acknowledged", "affirmative", "rest assured"');
    expect(g14.sources.find(s => s.id === 'ai-words')!.sha256).toBe(hash(readFileSync(join(root, AI_WORDS_FILE), 'utf8')));
    for (const r of g14.kinds.bark.rules) for (const id of r.sources) expect(g14.sources.some(s => s.id === id), `${r.id} ${id}`).toBe(true);
    expect(g14.ai).toEqual({ words: ai.words, kind: ai.kind, rejected: ai.rejected, fail: ai.fail }); expect(g13.ai).toBeUndefined();
    expect(g13.sources.some(s => s.id === 'ai-words')).toBe(false); expect(nat(g13)).not.toContain('AI sounding');
    expect(g14.kinds.why).toEqual(g13.kinds.why); expect(g14.kinds.debrief).toEqual(g13.kinds.debrief); expect(g14.kinds.label).toEqual(g13.kinds.label);
    // the pass Sky turned down leaves the bar ("I remember them, officer." was a /13 pass); /13's bar is unchanged
    expect(g13.natural!.pass.some(p => p.key === 'questioned/waitress/gives')).toBe(true); expect(g14.natural!.pass.some(p => p.key === 'questioned/waitress/gives')).toBe(false);
    expect(ai.rejected.map(x => x.line)).toContain('Citizen. Walking is encouraged. Running is noted.');
  });
  test('the code check fails an AI-sounding word whoever says it, a questioned row too, at /14 and not at /13', () => {
    const f = familyOf('run-seen/patrol'), issues = (s: ShortSet, g = g14, b = f, k = 'run-seen/patrol') => checkShort(b, s, g, { dialogue: text.dialogue }).filter(i => /AI-sounding/.test(i.reason) && i.reason.startsWith(k)).map(i => i.reason);
    const noted = edit(today(f), 'run-seen/patrol', ['Citizen. Walking is encouraged. Running is noted.', 'Running noted. Please walk.']);
    expect(issues(noted)).toHaveLength(2);
    expect(issues(edit(today(f), 'run-seen/patrol', ['Walking is encouraged, citizen.', 'Please walk, citizen. Thank you.']))).toEqual([]);
    const at13 = b13.find(b => b.items.some(i => i.key === 'run-seen/patrol'))!;
    expect(checkShort(at13, edit(today(at13), 'run-seen/patrol', ['Citizen. Walking is encouraged. Running is noted.', 'Running noted. Please walk.']), g13, { dialogue: text.dialogue }).filter(i => /AI-sounding/.test(i.reason))).toEqual([]);
    const q = familyOf('questioned/waitress/gives');
    expect(issues(edit(today(q), 'questioned/waitress/gives', ['Understood, officer. Here is what happened.', 'I saw them, officer. It was dreadful.']), g14, q, 'questioned/')).toHaveLength(1);
    for (const w of ['Noted.', 'Proceeding.', 'Understood.', 'Acknowledged.', 'Affirmative.', 'Rest assured, citizen.', 'Copy that.', 'Standing by.']) expect(w, w).toMatch(aiWords(ai));
    for (const w of ['I understand.', 'Note this down.', 'Walking is encouraged, citizen.', 'Proceed to the square, please.']) expect(w, w).not.toMatch(aiWords(ai));
    for (const x of ai.fail) expect(rows.find(r => barkKey(r as never) === x.key), x.key).toBeDefined();
  });
  test('the prompt carries /14\'s calibration and task; the families\' context gives Sky\'s own words, which are not items', () => {
    const p = shortPrompt('evaluate', g14, family('bark/fixer-scenes'), null, null);
    expect(p).toContain('These fail too: AI-sounding words'); expect(p).toContain('Sky turned down these four'); expect(p).toContain('fails NATURAL outright');
    expect(p).toContain('Sky\'s own words, already in the game and not items here'); expect(p).toContain('"Of course. Forget I asked."');
    const sky = rows.filter(r => r.sky);
    expect(sky.length).toBe(20);
    for (const r of sky) {
      expect(r.sky).toStartWith(SKY_WORDS);
      const k = barkKey(r as never);
      expect(barks.some(b => b.items.some(i => i.key === k)), k).toBe(false);
      expect(barks.filter(b => b.context.includes(`${k}: `)).length, k).toBe(1);
    }
  });
  test('the Mislead is a scene with a person: the Scout\'s tip and the rival\'s answer, one family, neither cut; every row that speaks is in exactly one family', () => {
    const m = family('bark/mislead');
    expect(m.items.map(i => i.key)).toEqual(['arrive/mislead/scout', 'rivals-misled/rivals']);
    expect(m.items.every(i => i.stays)).toBe(true);
    expect(m.items[0]!.tells).toMatch(/^Say: the Scout's tip to the rival lookout/); expect(m.items[0]!.tells).toMatch(/Facts, never said: he comes back/);
    expect(family('bark/rivals').items.some(i => i.key === 'rivals-misled/rivals')).toBe(false);
    expect(SCENE_ROWS).toContain('arrive/mislead/scout');
    const keys = barks.flatMap(b => b.items.map(i => i.key));
    expect(new Set(keys).size).toBe(keys.length);
    expect(rows.filter(r => speaks(r) && !r.sky).map(r => barkKey(r as never)).sort()).toEqual([...keys].sort());
    const agent = (k: string) => (AGENTS as readonly string[]).includes(k.split('/').find(p => (AGENTS as readonly string[]).includes(p)) ?? '');
    expect(keys.filter(agent).sort()).toEqual(SCENE_ROWS.filter(k => !k.endsWith('/named') && !rows.some(r => r.sky && barkKey(r as never) === k)).sort());
  });
  test('the news card for the rivals following our car out is a label family of its own; the HUD\'s other labels read as at /13', () => {
    const f = family('label/rivals-follow');
    expect(f.items.map(i => [i.key, i.fields.map(x => x.id)])).toEqual([['book.feed.cards.rivals-follow', ['title', 'line']]]);
    expect(f.items[0]!.current).toEqual({ title: [text.book.feed.cards['rivals-follow'].title], line: [text.book.feed.cards['rivals-follow'].line] });
    expect(checkShort(f, today(f), g14)).toEqual([]);
    expect(family('label/hud')).toEqual(b13.find(b => b.id === 'label/hud')!);
    expect(b13.some(b => b.id === 'label/rivals-follow')).toBe(false);
  });
});

describe('apply at /14', () => {
  test('Sky\'s words stay whatever a run says; a row two families have is written by the newer run; /12 bark runs never apply, /13 on do', async () => {
    const fx = (id: string, items: ShortSet['items'], startedAt: number, brief = family(id)) =>
      ({ run: { brief: { ...brief, items: brief.items.filter(i => items.some(x => x.key === i.key)) }, startedAt, rounds: [{ set: { items }, evaluation: { score: 10 }, hardIssues: [] }] } as unknown as ShortResult, path: tmpdir() });
    const line = (key: string, lines: string[]) => ({ key, cut: false, reason: '', fields: [{ id: 'lines', lines }] });
    const sky = rows.find(r => r.sky)!, skyKey = barkKey(sky as never), skyFamily = loadShortBriefs(root, 13).find(b => b.kind === 'bark' && b.items.some(i => i.key === skyKey))!;
    const tip = ['Fresh line one, for the test.', 'Fresh line two, for the test.', 'Fresh line three, for the test.'];
    const runs = new Map([
      ['old/arrive', fx('bark/mislead', [line('arrive/mislead/scout', ['Old one.', 'Old two.', 'Old three.'])], 1)], // an older family that had the row
      ['bark/mislead', fx('bark/mislead', [line('arrive/mislead/scout', tip)], 2)],
      ['old/sky', fx(skyFamily.id, [line(skyKey, sky.lines.map((_, i) => `Not Sky's line ${i}.`))], 3, skyFamily)],
    ]);
    for (const [id, x] of runs) (x.run.brief as { id: string }).id = id;
    const dir = mkdtempSync(join(tmpdir(), 'suborned-apply14-')), file = join(dir, 'text.json');
    copyFileSync(join(root, TEXT_FILE), file);
    await applyShort(false, { runs, file });
    const t = TextFile.parse(JSON.parse(readFileSync(file, 'utf8')));
    expect(t.dialogue.find(e => barkKey(e as never) === 'arrive/mislead/scout')!.lines).toEqual(tip);
    expect(t.dialogue.find(e => barkKey(e as never) === skyKey)!.lines).toEqual(sky.lines);
    const at = (v: number) => ({ brief: { kind: 'bark' }, guide: v === 12 ? loadShortGuide(root, 12) : v === 13 ? g13 : g14 }) as unknown as ShortResult;
    expect([waitsForSky(at(12)), waitsForSky(at(13)), waitsForSky(at(14))]).toEqual([true, false, false]);
  });
});

describe('a run continues another\'s last round (brief 102: Opus judging, three revisions often end on an 8)', () => {
  test('it starts by revising that draft from its findings, is accepted on a 10, and rebuilds only while the run it continues says the same', async () => {
    const f = family('bark/mislead'), ten = Object.fromEntries(BARK_RULES_12.map(r => [r, 10])) as ShortEvaluation12['scores'];
    const draft = edit(today(f), 'arrive/mislead/scout', ['The Councillor\'s using the front today.', 'Someone\'s asking for you at the front door.', 'Big car just pulled up at the front steps.']);
    const better = edit(draft, 'rivals-misled/rivals', ['I\'ll see for myself. Off you go.', 'Not a word of this to anyone. Go on.']);
    const eight = { scores: { ...ten, VOICE: 8 }, score: 8, reason: 'The rival repeats himself.', findings: [{ rule: 'VOICE', key: 'arrive/mislead/scout', quote: 'Big car just pulled up at the front steps.', reason: 'Flat.', fix: 'Warmer.' }] };
    const store = mkdtempSync(join(tmpdir(), 'suborned-continue-store-')), runs = mkdtempSync(join(tmpdir(), 'suborned-continue-runs-')), evidence = archiveEvidence(root, g14, store);
    let clock = 0;
    const play = (outputs: unknown[], extra = {}) => { const seen: string[] = []; const invoke: Invoke = async q => { seen.push(q.stage); return { output: outputs.shift(), tokens: 20, raw: { fixture: true } }; };
      return { seen, run: runShort({ guide: g14, brief: f, invoke, limits: { ...defaults, maxRevisions: 0 }, mode: 'test-fixture', evidence, base: { dialogue: text.dialogue }, now: () => clock++, ...extra }) }; };
    const first = await play([draft, eight]).run;
    expect([first.status, first.stopReason]).toEqual(['INCOMPLETE', 'Revision limit reached before a 10/10']);
    const name = '2026-11-08T00-00-00.000Z-bark-mislead'; mkdirSync(join(runs, name)); writeRun(join(runs, name), first);
    const { seen, run } = play([better, { scores: ten, score: 10, reason: 'Fixed.', findings: [] }], { continues: { run: name, ...first.rounds.at(-1)! } });
    const next = await run;
    expect(seen).toEqual(['refine', 'evaluate']); expect(next.status).toBe('ACCEPTED'); expect(next.calls[0]!.prompt).toContain('The rival repeats himself.');
    expect(next.calls[1]!.prompt).toContain('Your last score and findings'); // the evaluator holds the writer to its own last findings
    expect((await verifyShort(next, root, store, runs)).errors).toEqual([]);
    const forged = structuredClone(next); forged.continues!.evaluation.score = 9;
    expect((await verifyShort(forged, root, store, runs)).ok).toBe(false);
    const other = structuredClone(next); other.continues!.run = '2026-11-08T00-00-01.000Z-bark-mislead';
    expect((await verifyShort(other, root, store, runs)).ok).toBe(false);
  });
});

describe('guide /15: a score below 10 says what holds it back', () => {
  const g15 = loadShortGuide(root, 15);
  test('new runs use /15; only the evaluator\'s task moves, so the rubric, the sources and the families read as at /14, and a /15 run may continue a /14 run', () => {
    expect(SHORT_NOW).toBeGreaterThanOrEqual(15);
    expect({ ...g14, schema: g15.schema }).toEqual(g15); expect(loadShortBriefs(root, 15)).toEqual(b14);
    expect([sameRubric(g14, g15), sameRubric(g15, g14), sameRubric(g13, g15)]).toEqual([true, false, false]);
    const f = family('bark/mislead'), at = (g: typeof g14, stage: 'generate' | 'evaluate' | 'refine') => shortPrompt(stage, g, f, stage === 'generate' ? null : today(f), null);
    expect(at(g15, 'evaluate')).toContain('Every rule you score below 10 needs at least one finding'); expect(at(g14, 'evaluate')).not.toContain('Every rule you score below 10');
    expect(at(g15, 'generate')).toBe(at(g14, 'generate')); expect(at(g15, 'refine')).toBe(at(g14, 'refine'));
  });
  test('a /15 run continues a /14 run\'s last round and rebuilds; not a run on another rubric', async () => {
    const f = family('bark/mislead'), ten = Object.fromEntries(BARK_RULES_12.map(r => [r, 10])) as ShortEvaluation12['scores'];
    const nine = { scores: Object.fromEntries(BARK_RULES_12.map(r => [r, 9])), score: 9, reason: 'I\'d ship it, but none has the bite.', findings: [] };
    const store = mkdtempSync(join(tmpdir(), 'suborned-15-store-')), runs = mkdtempSync(join(tmpdir(), 'suborned-15-runs-')), evidence = archiveEvidence(root, g14, store);
    let clock = 0;
    const play = (g: typeof g14, outputs: unknown[], extra = {}) => runShort({ guide: g, brief: f, invoke: async () => ({ output: outputs.shift(), tokens: 20, raw: { fixture: true } }),
      limits: { ...defaults, maxRevisions: 0 }, mode: 'test-fixture', evidence, base: { dialogue: text.dialogue }, now: () => clock++, ...extra });
    const first = await play(g14, [today(f), nine]);
    const name = '2026-11-08T00-00-00.000Z-bark-mislead'; mkdirSync(join(runs, name)); writeRun(join(runs, name), first);
    const next = await play(g15, [today(f), { scores: ten, score: 10, reason: 'Ship it.', findings: [] }], { continues: { run: name, ...first.rounds.at(-1)! } });
    expect(next.status).toBe('ACCEPTED'); expect(next.calls[1]!.prompt).toContain('Every rule you score below 10');
    expect((await verifyShort(next, root, store, runs)).errors).toEqual([]);
    const back = structuredClone(first); back.guide = g13; writeRun(join(runs, name), back); // the earlier run on another rubric (/13): not one a run may continue
    expect((await verifyShort(next, root, store, runs)).ok).toBe(false);
  });
});

describe('guide /16: a cut item may return a field with no lines', () => {
  test('/16 reads as /15 but for the cut check; /15 still flags it, so its runs rebuild as they ran', () => {
    const g15 = loadShortGuide(root, 15), g16 = loadShortGuide(root, 16), f = family('bark/alarm');
    expect(SHORT_NOW).toBeGreaterThanOrEqual(16); expect({ ...g15, schema: g16.schema }).toEqual(g16); expect(sameRubric(g15, g16)).toBe(true); expect(sameRubric(g14, g16)).toBe(true);
    const k = f.items[0]!.key, cut = { items: today(f).items.map(i => i.key !== k ? i : { key: i.key, cut: true, reason: 'Filler.', fields: [{ id: 'lines', lines: [] }] }) };
    const said = (g: typeof g15, s = cut) => checkShort(f, s, g, { dialogue: text.dialogue }).map(i => i.reason).filter(r => r.includes('a cut item'));
    expect(said(g15)).toEqual([`${k}: a cut item has no lines.`]); expect(said(g16)).toEqual([]);
    const withLine = { items: cut.items.map(i => i.key !== k ? i : { ...i, fields: [{ id: 'lines', lines: ['Something.'] }] }) };
    expect(said(g16, withLine)).toEqual([`${k}: a cut item says no line; return its fields with no lines.`]);
  });
});
