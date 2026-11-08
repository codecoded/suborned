import { describe, expect, test } from 'bun:test';
import { mkdtempSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { questionCount, sentenceCount } from '../../contracts/missions/handover/conversations.schema.ts';
import { SCENE_ROWS, speaks } from '../../contracts/missions/handover/speech.schema.ts';
import { AGENTS } from '../../contracts/missions/handover/text.schema.ts';
import { root } from '../rag/config';
import { loadBriefs } from './briefs';
import { checkConversation, wordCount } from './checks';
import { defaults } from './core';
import { archiveEvidence } from './evidence';
import { hash, loadGuide, TEXT_FILE } from './guide';
import { barkKey } from './short-barks';
import { loadShortBriefs } from './short-briefs';
import { checkShort } from './short-checks';
import { waitsForSky } from './short-cli';
import { verifyShort } from './short-evidence';
import { loadShortGuide } from './short-guide';
import { runShort } from './short-loop';
import { FORETELLS, NATURAL_BAR, NaturalBar } from './short-natural';
import { shortPrompt } from './short-prompts';
import { BARK_RULES_12, evaluationFor, ShortEvaluation12, SHORT_NOW, type ShortBrief, type ShortResult, type ShortSet } from './short-types';
import type { Conversation, Invoke } from './types';

// Guide /13 and conversation guide /4 (brief 102, Sky 2026-11-08): labelled test data only; no model is called.
const g13 = loadShortGuide(root, 13), g12 = loadShortGuide(root, 12), b13 = loadShortBriefs(root, 13);
const text = JSON.parse(readFileSync(join(root, TEXT_FILE), 'utf8')), bar = NaturalBar.parse(JSON.parse(readFileSync(join(root, NATURAL_BAR), 'utf8')));
const rule = (id: string) => g13.kinds.bark.rules.find(r => r.id === id)!.instruction;
const barks = b13.filter(b => b.kind === 'bark'), item = (key: string) => barks.flatMap(b => b.items).find(i => i.key === key)!;
const familyOf = (key: string) => barks.find(b => b.items.some(i => i.key === key))!;
const today = (b: ShortBrief): ShortSet => ({ items: b.items.map(i => ({ key: i.key, cut: false, reason: '', fields: i.fields.map(f => ({ id: f.id, lines: (i.current[f.id] ?? []).slice(0, f.count) })) })) });
const edit = (s: ShortSet, key: string, lines: string[]): ShortSet => ({ items: s.items.map(i => i.key !== key ? i : { ...i, fields: i.fields.map(f => ({ ...f, lines })) }) });
const rows = text.dialogue as { event: string; speaker: string; lines: string[]; silent?: string; parked?: string; pending?: string }[];

describe('guide /13: nobody explains the game', () => {
  test('new runs use /13; the bark rubric keeps /12\'s eight rules, NATURAL and TRUE hold the no-mechanics rule in Sky\'s words, and every rule cites a source', () => {
    expect(SHORT_NOW).toBe(13);
    expect(g13.kinds.bark.rules.map(r => r.id)).toEqual([...BARK_RULES_12]);
    for (const r of g13.kinds.bark.rules) for (const id of r.sources) expect(g13.sources.some(s => s.id === id), `${r.id} ${id}`).toBe(true);
    expect(g13.sources.find(s => s.id === 'natural')!.sha256).toBe(hash(readFileSync(join(root, NATURAL_BAR), 'utf8')));
    expect(rule('NATURAL')).toContain('"that\'s exactly the kind of scripting that bugs me. \'I will remember you.\' lines etc - so unnatural"');
    expect(rule('NATURAL')).toContain('"You said no. You\'ll remember me."'); expect(rule('NATURAL')).toContain('"You paid my debt? I\'m yours. Say when." and "Civic Police! Drop your weapons!"');
    expect(rule('TRUE')).toContain('Nobody knows the game\'s future'); expect(rule('TRUE')).toContain('what comes after "Facts" keeps the line true and is never said');
    expect(rule('MECHANICS')).not.toContain('It\'ll be back when they run out.'); // /12's example foretold a rule
    expect(rule('WORTH')).toContain('never to the commander or the cell');
    expect(g13.kinds.why).toEqual(g12.kinds.why); expect(g13.kinds.debrief).toEqual(g12.kinds.debrief); expect(g13.kinds.label).toEqual(g12.kinds.label);
    expect(g12.kinds.bark.rules.find(r => r.id === 'NATURAL')!.instruction).not.toContain('brief 102'); expect(g12.natural).toBeUndefined(); // /12 rebuilds as it ran
    expect(evaluationFor(g13, 'bark')).toBe(ShortEvaluation12);
  });
  test('the agents\' families leave the loop: an agent row is written only where a scene with a person plays it, and every row that speaks is in exactly one family', () => {
    expect(barks.some(b => /^bark\/(ack-|read-|arrive$|done$)/.test(b.id))).toBe(false);
    const keys = barks.flatMap(b => b.items.map(i => i.key));
    expect(new Set(keys).size).toBe(keys.length);
    expect(keys.filter(k => (AGENTS as readonly string[]).includes(k.split('/').find(p => (AGENTS as readonly string[]).includes(p)) ?? '')).sort())
      .toEqual(SCENE_ROWS.filter(k => !k.endsWith('/named')).sort());
    expect(rows.filter(r => speaks(r)).map(r => barkKey(r as never)).sort()).toEqual([...keys].sort());
    for (const k of ['arrive/help/fixer', 'done/turn/fixer/refused', 'ack/call/fixer', 'done/take/enforcer']) expect(item(k).tells, k).toMatch(/Said to (the person|them|Inez|Popovic)/);
    expect(item('done/turn/fixer/refused').fields[0]!.keep).toEqual([]); // brief 102 took text.test.ts's pin on "You said no. You'll remember me."
  });
  test('a reply says the person\'s answer apart from the facts the game shows, and nobody without a Call offers to come when called', () => {
    const sus = item('reply/pay/driver/suspicious/loyal').tells, [say, facts] = sus.split('Facts, never said');
    expect(facts).toMatch(/report/); expect(say).not.toMatch(/report/);
    expect(item('reply/pay/guard/turned/mercenary').tells).toContain('The guard has no Call');
    expect(item('saw-gunfire/crowd').tells).toMatch(/^Say: .*Facts, never said: asked later/);
    expect(item('witness-reacts/crowd/check').tells).toContain('they go over to look');
  });
  test('the code check holds the family Sky named at /13, whoever says it, but not at /12 and not when an investigator asks', () => {
    const f = familyOf('done/turn/fixer/refused'), good = today(f);
    const issues = (s: ShortSet, g = g13, b = f) => checkShort(b, s, g, { dialogue: text.dialogue }).filter(i => i.rule === 'NATURAL' && /explains the game/.test(i.reason));
    expect(issues(good).map(i => i.reason)).toEqual([expect.stringContaining('done/turn/fixer/refused.lines #1: "You\'ll remember" explains the game')]); // today's line, until the sweep
    expect(issues(edit(good, 'done/turn/fixer/refused', ['Of course. Forget I asked.', 'Then we\'ll say no more about it.', 'That\'s all right. I won\'t keep you.']))).toEqual([]);
    const at12 = loadShortBriefs(root, 12).find(b => b.id === 'bark/done')!;
    expect(issues(today(at12), g12, at12)).toEqual([]); // a /12 run is checked as it ran
    const q = familyOf('questioned/waitress/gives');
    expect(issues(edit(today(q), 'questioned/waitress/gives', ['I\'d know that face, officer. I\'ll tell you.', 'I remember them, officer. Here, let me tell you what happened.']), g13, q)).toEqual([]);
    for (const l of ['He\'ll be back, and he knows my face.', 'I will remember you.', 'A search could find it.', 'They\'ll notice.', 'They\'ll wake with a sore head.']) expect(l, l).toMatch(FORETELLS);
    for (const l of bar.kept.map(x => x.line)) expect(l).not.toMatch(FORETELLS);
  });
  test('the prompt carries the /13 task and calibration; the calibration\'s lines are today\'s, in rows that exist', () => {
    const p = shortPrompt('evaluate', g13, familyOf('done/turn/fixer/refused'), null, null);
    expect(p).toContain('These fail: they explain the game'); expect(p).toContain('Sky kept these: the standard.'); expect(p).not.toContain('These fail (lines in today\'s text.json that the old bar let through)');
    expect(p).toContain('Voice sheet:'); expect(p).toContain('remembering a face, coming back, reporting it');
    const live = new Map(rows.map(r => [barkKey(r as never), r]));
    for (const x of [...bar.kept, ...bar.fail, ...bar.pass].filter(x => x.key !== 'sky')) expect(live.get(x.key)?.lines, x.key).toContain(x.line);
    for (const x of [...bar.kept, ...bar.pass]) expect(speaks(live.get(x.key)!), x.key).toBe(true);
  });
  test('a /13 run is not accepted on a 9 for NATURAL, waits for Sky before it applies, and rebuilds from its archive (which holds the bar)', async () => {
    const f = familyOf('done/turn/fixer/refused'), good = today(f), ten = Object.fromEntries(BARK_RULES_12.map(r => [r, 10])) as ShortEvaluation12['scores'];
    const fixed = edit(good, 'done/turn/fixer/refused', ['Of course. Forget I asked.', 'Then we\'ll say no more about it.', 'That\'s all right. I won\'t keep you.']);
    const outputs: unknown[] = [fixed, { scores: { ...ten, NATURAL: 9 }, score: 10, reason: 'A line still reads the rule out.', findings: [] }, fixed, { scores: ten, score: 10, reason: 'People talking.', findings: [] }];
    const invoke: Invoke = async () => ({ output: outputs.shift(), tokens: 20, raw: { fixture: true } });
    const store = mkdtempSync(join(tmpdir(), 'suborned-natural-store-')), evidence = archiveEvidence(root, g13, store);
    let clock = 0;
    const r = await runShort({ guide: g13, brief: f, invoke, limits: { ...defaults }, mode: 'test-fixture', evidence, base: { dialogue: text.dialogue }, now: () => clock++ });
    expect([r.status, r.rounds.length]).toEqual(['ACCEPTED', 2]);
    expect(evidence.sources.some(s => s.file === NATURAL_BAR)).toBe(true);
    expect((await verifyShort(r, root, store)).errors).toEqual([]);
    expect(waitsForSky({ brief: { id: f.id, kind: 'bark' }, guide: g13 } as unknown as ShortResult)).toBe(true);
  });
});

describe('Sky\'s sample (briefs/102/fable/sample.json)', () => {
  const sample = JSON.parse(readFileSync(join(root, 'briefs/102/fable/sample.json'), 'utf8')) as { rows: { key: string; where: string; before: string[]; after: string[] }[] };
  test('about twenty, each "before" is today\'s, and every "after" passes the /13 code checks where it would go', () => {
    expect(sample.rows.length).toBeGreaterThanOrEqual(18); expect(sample.rows.length).toBeLessThanOrEqual(24);
    const all = sample.rows.flatMap(r => r.after.filter(l => !r.before.includes(l)));
    expect(new Set(all).size).toBe(all.length);
    for (const r of sample.rows) {
      for (const l of r.after) expect(l, r.key).not.toMatch(FORETELLS);
      if (r.where === 'row') {
        const f = familyOf(r.key), it = f.items.find(i => i.key === r.key)!;
        expect(it.current.lines, r.key).toEqual(r.before);
        expect(r.after.length, r.key).toBe(it.fields[0]!.count);
        expect(checkShort(f, edit(today(f), r.key, r.after), g13, { dialogue: text.dialogue }).map(i => i.reason).filter(x => x.startsWith(`${r.key}.`) || x.startsWith(`${r.key}:`)), r.key).toEqual([]);
        for (const l of r.after.filter(x => !r.before.includes(x))) expect(rows.some(x => x.lines.includes(l)), `${r.key}: "${l}" is another row's`).toBe(false);
      } else if (r.where === 'scene') {
        for (const l of r.before) expect(text.scenes.items.some((s: { variants: { beats: { line?: string }[] }[] }) => s.variants.some(v => v.beats.some(b => b.line === l))), l).toBe(true);
        for (const l of r.after) { expect(wordCount(l), l).toBeLessThanOrEqual(12); expect(l.length, l).toBeLessThanOrEqual(80); }
      } else {
        const c = text.scenes.conversations.items.find((x: { id: string }) => x.id === r.key.split('/')[1])!;
        for (const l of r.before) expect(JSON.stringify(c), l).toContain(JSON.stringify(l));
        for (const l of r.after) { expect(wordCount(l)).toBeLessThanOrEqual(25); expect(l.length).toBeLessThanOrEqual(160); expect(sentenceCount(l)).toBeLessThanOrEqual(2); expect(questionCount(l)).toBeLessThanOrEqual(1); }
      }
    }
  });
});

describe('conversation guide /4: NATURAL holds the no-mechanics rule too', () => {
  const g4 = loadGuide(root, 4), g3 = loadGuide(root, 3), b4 = loadBriefs(root, 4), b3 = loadBriefs(root, 3);
  test('/4 is new runs\' guide; /3 rebuilds as it ran; the refused Pressure no longer asks her to say she\'ll report it', () => {
    expect(loadGuide(root).schema).toBe('suborned.style-guide/4');
    const nat = (g: typeof g4) => g.rules.find(r => r.id === 'NATURAL')!.instruction;
    expect(nat(g4)).toContain(nat(g3)); expect(nat(g4)).toContain('nobody explains the game'); expect(nat(g3)).not.toContain('brief 102');
    expect(g3.sources.some(s => s.id === 'natural-words')).toBe(false);
    const refused = (bs: typeof b4) => bs.find(b => b.id === 'inez/pressure/loyal')!.reveals.join(' ');
    expect(refused(b3)).toContain('makes plain she will report'); expect(refused(b4)).toContain('never says what she will do about it');
    expect(b4.filter(b => b.id !== 'inez/pressure/loyal')).toEqual(b3.filter(b => b.id !== 'inez/pressure/loyal').map(b => b)); // only that brief moved
  });
  test('the code check names a foretelling line at /4, not at /3', () => {
    const brief = b4.find(b => b.id === 'inez/listen/idealist')!;
    const lines: Conversation['lines'] = Array.from({ length: 10 }, (_, i) => ({ speaker: i % 2 ? 'waitress' : i === 4 ? 'customer' : 'guard', line: i === 9 ? 'I will remember you, officer.' : `Line ${i} of the talk, said plainly.` }));
    const told = (g: typeof g4) => checkConversation(brief, { lines }, g).filter(i => /explains the game/.test(i.reason));
    expect(told(g4)).toHaveLength(1); expect(told(g3)).toEqual([]);
  });
});
