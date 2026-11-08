import { describe, expect, test } from 'bun:test';
import { copyFileSync, mkdtempSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { z } from 'zod';
import { TextFile } from '../../contracts/missions/handover/text.schema.ts';
import { root } from '../rag/config';
import { defaults } from './core';
import { archiveEvidence } from './evidence';
import { hash, TEXT_FILE } from './guide';
import { applyShort } from './short-apply';
import { barkKey } from './short-barks';
import { loadShortBriefs } from './short-briefs';
import { checkShort } from './short-checks';
import { waitsForSky } from './short-cli';
import { verifyShort } from './short-evidence';
import { loadShortGuide } from './short-guide';
import { runShort } from './short-loop';
import { shortPrompt } from './short-prompts';
import { BARK_RULES_12, evaluationFor, ShortEvaluation, ShortEvaluation12, VoiceSheet, type ShortBrief, type ShortResult, type ShortSet } from './short-types';
import { AGENTS, speech12, VOICE_SHEET } from './short-voice';
import type { Invoke, Request } from './types';

// Guide /12 (brief 099 voices): labelled test data only; no model is called, and nothing here is a generated line.
const g12 = loadShortGuide(root, 12), g11 = loadShortGuide(root, 11), b12 = loadShortBriefs(root, 12), b11 = loadShortBriefs(root, 11);
const text = JSON.parse(readFileSync(join(root, TEXT_FILE), 'utf8')), sheet = VoiceSheet.parse(JSON.parse(readFileSync(join(root, VOICE_SHEET), 'utf8')));
const live = (text.dialogue as { event: string; parked?: string; pending?: string; speaker: string }[]).filter(r => !r.parked && !r.pending), agent = (k: string) => (AGENTS as readonly string[]).includes(k.split(':')[0]!);
const family = (id: string) => b12.find(b => b.id === id)!;
const today = (b: ShortBrief): ShortSet => ({ items: b.items.map(i => ({ key: i.key, cut: false, reason: '', fields: i.fields.map(f => ({ id: f.id, lines: (i.current[f.id] ?? []).slice(0, f.count) })) })) });
const edit = (s: ShortSet, key: string, lines: string[]): ShortSet => ({ items: s.items.map(i => i.key !== key ? i : { ...i, fields: i.fields.map(f => ({ ...f, lines })) }) });
const ten = Object.fromEntries(BARK_RULES_12.map(r => [r, 10])) as ShortEvaluation12['scores'];

describe('guide /12: WORTH and VOICE lead the barks, each rule scored on its own', () => {
  test('the bark rubric is the eight /12 rules in order, each tied to fingerprinted sources; the why block, the page and the labels read as at /11', () => {
    expect(g12.kinds.bark.rules.map(r => r.id)).toEqual([...BARK_RULES_12]);
    expect(g12.kinds.bark.rules.slice(0, 2).map(r => r.id)).toEqual(['WORTH', 'VOICE']);
    for (const r of g12.kinds.bark.rules) for (const id of r.sources) expect(g12.sources.some(s => s.id === id), `${r.id} ${id}`).toBe(true);
    const sheetSource = g12.sources.find(s => s.id === 'sheet')!;
    expect(sheetSource.file).toBe(VOICE_SHEET); expect(sheetSource.sha256).toBe(hash(readFileSync(join(root, VOICE_SHEET), 'utf8')));
    expect(g11.sources.some(s => s.id === 'sheet')).toBe(false); expect(g11.sheet).toBeUndefined(); // a /11 run rebuilds with the sources it ran with
    expect(g12.kinds.why).toEqual(g11.kinds.why); expect(g12.kinds.debrief).toEqual(g11.kinds.debrief); expect(g12.kinds.label).toEqual(g11.kinds.label);
    const rule = (id: string) => g12.kinds.bark.rules.find(r => r.id === id)!.instruction;
    expect(rule('WORTH')).toContain('would the player miss it if it were silence?'); expect(rule('WORTH')).toContain('what is this stupid words at the start of every game?');
    expect(rule('VOICE')).toContain('scored on its own'); expect(rule('NATURAL')).toContain('"Holstered."');
    expect(rule('MECHANICS')).toContain('Sky\'s rule on numbers in play'); expect(rule('TRUE')).toContain('he is out only once a car of ours has left with him');
    expect(rule('SHORT')).not.toContain('an acknowledgement is often two or three'); // the /4 nudge towards status codes is gone
    expect(Object.keys(g12.sheet!.voices)).toEqual([...AGENTS]);
  });
  test('the evaluation scores every rule; a /12 bark family takes it, and every other family keeps the /1–/11 schema (and its hash)', () => {
    expect(evaluationFor(g12, 'bark')).toBe(ShortEvaluation12); expect(evaluationFor(g12, 'why')).toBe(ShortEvaluation); expect(evaluationFor(g11, 'bark')).toBe(ShortEvaluation);
    expect(Object.keys((z.toJSONSchema(ShortEvaluation12) as unknown as { properties: { scores: { properties: object } } }).properties.scores.properties)).toEqual([...BARK_RULES_12]);
    const why = b12.find(b => b.id === 'why/police')!;
    expect(shortPrompt('evaluate', g12, why, today(why), null)).toBe(shortPrompt('evaluate', g11, b11.find(b => b.id === 'why/police')!, today(why), null));
  });
  test('the prompt carries the voice sheet for the family\'s agents and the calibration, failing and passing', () => {
    const p = shortPrompt('generate', g12, family('bark/ack-acts'), null, null);
    expect(p).toContain('Voice sheet:'); expect(p).toContain('neverSays'); expect(p).toContain('These fail'); expect(p).toContain('"Holstered."'); expect(p).toContain('"If you\'re sure."');
    expect(p).toContain('WORTH and VOICE lead'); expect(p).not.toContain('Voices:\n');
    expect(shortPrompt('generate', g11, b11.find(b => b.id === 'bark/ack-acts')!, null, null)).toContain('Voices:\n'); // /11 as it ran
  });
});

describe('the voice sheet and Fable\'s frequency', () => {
  test('every live acknowledgement verb is in exactly one tier', () => {
    const verbs = [...new Set((text.dialogue as { event: string; verb?: string; parked?: string }[]).filter(r => r.event === 'ack' && !r.parked).map(r => r.verb!))];
    const tiers = [sheet.speech.ack.always, sheet.speech.ack.first, sheet.speech.ack.never].map(t => t.map(k => k.split('/')[0]!));
    for (const v of verbs) expect(tiers.filter(t => t.includes(v)).length > 0, v).toBe(true);
    for (const v of verbs) expect([sheet.speech.ack.first, sheet.speech.ack.never.filter(k => k !== 'takedown')].filter(t => t.includes(v)).length <= 1, v).toBe(true);
  });
  test('every agent row is written by exactly one /12 family or silent with its reason; people\'s rows are as at /11', () => {
    const sp = speech12(text, sheet.speech), keys12 = b12.filter(b => b.kind === 'bark').flatMap(b => b.items.map(i => i.key));
    expect(new Set(keys12).size).toBe(keys12.length);
    for (const r of live.filter(x => agent(x.speaker))) {
      const k = barkKey(r as never), why = sp.silent(r as never);
      expect(keys12.includes(k) !== !!why, k).toBe(true);
    }
    const people = (bs: ShortBrief[]) => bs.filter(b => b.kind === 'bark').flatMap(b => b.items.filter(i => !agent(i.speaker!)).map(i => i.key)).sort();
    expect(people(b12)).toEqual(people(b11));
    expect(live.filter(r => r.event === 'read').every(r => /brief 044/.test(sp.silent(r as never)!))).toBe(true); // Sky's brief 044
    expect(b12.some(b => b.id === 'bark/ack-feet' || b.id.startsWith('bark/read-'))).toBe(false);
  });
  test('the families say when a line is heard, and the Enter landing is briefed as it plays', () => {
    const at = (id: string, key: string) => family(id).items.find(i => i.key === key)!;
    expect(at('bark/ack-acts', 'ack/watch/scout').where).toContain('first time this agent takes this order');
    expect(at('bark/ack-acts', 'ack/call/fixer').where).toContain('Every time');
    expect(at('bark/done', 'done/enter/enforcer').tells).toContain('come back out of the Office with Popovic');
    expect(at('bark/done', 'done/turn/fixer/refused').fields[0]!.keep).toEqual(["You said no. You'll remember me."]); // text.test.ts still pins it
    expect(at('bark/arrive', 'arrive/help/fixer').stays).toContain('approach scenes');
    expect(at('bark/ack-acts', 'ack/watch/scout').stays).toBeNull(); // a /12 cut is silence (step 2's silent rows), not the stock "On it."
  });
  test('every calibration line is a real row; every gold line is in a /12 family and passes the code checks there', () => {
    const rows = new Set(live.map(r => barkKey(r as never)));
    for (const x of [...sheet.fail, ...sheet.pass]) expect(rows.has(x.key), x.key).toBe(true);
    for (const x of sheet.fail) expect((text.dialogue as { lines: string[] }[]).find(r => barkKey(r as never) === x.key)!.lines, x.key).toContain(x.line);
    for (const x of sheet.pass) {
      const b = b12.find(f => f.items.some(i => i.key === x.key))!;
      expect(b, x.key).toBeTruthy();
      const item = b.items.find(i => i.key === x.key)!, set = edit(today(b), x.key, [x.line, ...(item.current.lines ?? []).filter(l => l !== x.line)].slice(0, item.fields[0]!.count));
      expect(checkShort(b, set, g12, { dialogue: text.dialogue }).filter(i => i.reason.startsWith(x.key)).map(i => i.reason), x.key).toEqual([]);
    }
  });
});

describe('the /12 code checks and the loop', () => {
  const end = family('bark/end'), good = today(end);
  test('today\'s lines pass the code checks; digits in an agent\'s line fail MECHANICS; a repeat is VARIANTS\', a missing item WORTH\'s', () => {
    for (const b of b12.filter(x => x.kind === 'bark')) expect(checkShort(b, today(b), g12, { dialogue: text.dialogue }), b.id).toEqual([]);
    const rules = (s: ShortSet) => checkShort(end, s, g12).map(i => `${i.rule} ${i.reason}`).join('\n');
    expect(rules(edit(good, 'win/fixer', ['We have him, all 4 of us.', 'Away.']))).toMatch(/^MECHANICS win\/fixer\.lines #1: no digits/m);
    expect(rules(edit(good, 'win/fixer', ['Away we go.', 'Away we go!']))).toMatch(/^VARIANTS .*repeats another variant/m);
    expect(rules({ items: good.items.filter(i => i.key !== 'win/scout') })).toMatch(/^WORTH win\/scout: missing/m);
    expect(checkShort(b11.find(b => b.id === 'bark/end')!, edit(good, 'win/fixer', ['Away we go.', 'Away we go!']), g11).map(i => i.rule)).toContain('SHORT'); // /11 as it ran
  });
  test('a 10 overall with a 9 on VOICE is not accepted; the reviser sees the rule scores; the run rebuilds from its archive', async () => {
    const flat: ShortEvaluation12 = { scores: { ...ten, VOICE: 9 }, score: 10, reason: 'True and short, but the Fixer could be anyone.', findings: [] };
    const accept: ShortEvaluation12 = { scores: ten, score: 10, reason: 'Every line earns its place, in its speaker\'s voice.', findings: [] };
    const outputs: unknown[] = [good, flat, good, accept], requests: Request[] = [];
    const invoke: Invoke = async q => { requests.push(q); return { output: outputs.shift(), tokens: 20, raw: { fixture: true } }; };
    const store = mkdtempSync(join(tmpdir(), 'suborned-voice-store-')), evidence = archiveEvidence(root, g12, store);
    let clock = 0;
    const r = await runShort({ guide: g12, brief: end, invoke, limits: { ...defaults }, mode: 'test-fixture', evidence, base: { dialogue: text.dialogue }, now: () => clock++ });
    expect([r.status, r.rounds.length]).toEqual(['ACCEPTED', 2]);
    expect(requests[2]!.prompt).toContain('"VOICE": 9'); expect(requests[3]!.prompt).toContain('Your last score and findings');
    expect(evidence.sources.some(s => s.file === VOICE_SHEET)).toBe(true);
    expect((await verifyShort(r, root, store)).errors).toEqual([]);
    const x = structuredClone(r); (x.rounds[0]!.evaluation as ShortEvaluation12).scores.VOICE = 10;
    expect((await verifyShort(x, root, store)).ok).toBe(false);
  });
});

describe('apply waits for Sky', () => {
  test('a /12 bark run waits on Sky\'s yes; a /12 label run and every /4–/11 run do not', () => {
    const fake = (id: string, kind: ShortBrief['kind'], g = g12) => ({ brief: { id, kind }, guide: g }) as unknown as ShortResult;
    expect(waitsForSky(fake('bark/ack-acts', 'bark'))).toBe(true);
    expect(waitsForSky(fake('label/hud', 'label'))).toBe(false); expect(waitsForSky(fake('bark/ack-acts', 'bark', g11))).toBe(false);
  });
  test('Sky\'s "Turn driver by" survives an apply of a run that says "Driver cutoff", and the /12 brief keeps it', async () => {
    const b = b12.find(x => x.id === 'label/hud')!, item = b.items.find(i => i.key === 'briefing.timeline.5:20')!;
    expect(item.fields[0]!.keep).toEqual(['Turn driver by']); expect(b11.find(x => x.id === 'label/hud')!.items.find(i => i.key === 'briefing.timeline.5:20')!.fields[0]!.keep).toEqual([]);
    const words = edit(today(b), 'briefing.timeline.5:20', ['Driver cutoff']);
    const run = { brief: b, rounds: [{ set: words, evaluation: { score: 10, reason: 'ok', findings: [] }, hardIssues: [] }] } as unknown as ShortResult;
    const dir = mkdtempSync(join(tmpdir(), 'suborned-voice-apply-')), file = join(dir, 'text.json');
    copyFileSync(join(root, TEXT_FILE), file);
    await applyShort(false, { runs: new Map([[b.id, { run, path: dir }]]), file });
    expect(TextFile.parse(JSON.parse(readFileSync(file, 'utf8'))).briefing.timeline.find(m => m.at === '5:20')!.short).toBe('Turn driver by');
  });
});
