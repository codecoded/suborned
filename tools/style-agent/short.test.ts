import { describe, expect, test } from 'bun:test';
import { copyFileSync, existsSync, mkdtempSync, readdirSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { z } from 'zod';
import { TextFile } from '../../contracts/missions/handover/text.schema.ts';
import { CODEX_VERSION, codexArgs, codexConfig, codexOutputSchema, interpretCodex } from '../goals/codex';
import { root } from '../rag/config';
import { defaults } from './core';
import { archiveEvidence, readRun, RUN_FILE } from './evidence';
import { hash, TEXT_FILE } from './guide';
import { applyShort } from './short-apply';
import { barkKey } from './short-barks';
import { loadShortBriefs } from './short-briefs';
import { checkShort, shortWords } from './short-checks';
import { verifyShort } from './short-evidence';
import { loadShortGuide } from './short-guide';
import { runShort } from './short-loop';
import { shortPrompt } from './short-prompts';
import type { ShortBrief, ShortEvaluation, ShortResult, ShortSet } from './short-types';
import type { Invoke, Request } from './types';

// Labelled test data only: no model is called, and nothing here is presented as a generated line.
// These tests hold the /1–/11 rubric and families (every saved run is on one of them); guide /12's (brief 099 voices) are in voice.test.ts.
const V = 11, guide = loadShortGuide(root, V), briefs = loadShortBriefs(root, V), text = JSON.parse(readFileSync(join(root, TEXT_FILE), 'utf8'));
const store = mkdtempSync(join(tmpdir(), 'suborned-short-store-')), evidence = archiveEvidence(root, guide, store);
const family = (id: string) => briefs.find(b => b.id === id)!;
/** A family's set as today's words: the code checks' passing case. */
const today = (b: ShortBrief): ShortSet => ({ items: b.items.map(i => ({ key: i.key, cut: false, reason: '', fields: i.fields.map(f => ({ id: f.id, lines: (i.current[f.id] ?? []).slice(0, f.count) })) })) });
const end = family('bark/end'), good = today(end);
const edit = (s: ShortSet, key: string, lines: string[], id = 'lines'): ShortSet => ({ items: s.items.map(i => i.key !== key ? i : { ...i, fields: i.fields.map(f => f.id === id ? { ...f, lines } : f) }) });
const filler = edit(good, 'win/fixer', ['Waiting. Always waiting.', 'Tell me when.']);
const better = edit(good, 'win/fixer', ['We have him. Nobody followed us out.', 'He\'s in the car, and we\'re clear.']);
const rejected: ShortEvaluation = { score: 4, reason: 'The Fixer\'s win lines tell the player nothing: the opening barks Sky cut.', findings: [
  { rule: 'USEFUL', key: 'win/fixer', quote: 'Waiting. Always waiting.', reason: 'Filler: no fact the player can use.', fix: 'Say the job is done and Popovic is aboard.' }] };
const accept: ShortEvaluation = { score: 10, reason: 'Every bark carries its fact, in the speaker\'s voice, within the bubble.', findings: [] };
const issues = (b: ShortBrief, s: ShortSet) => checkShort(b, s, guide, { why: text.why }).map(i => i.reason).join('\n');

function fixture(outputs: unknown[], tokens: number | null = 20) {
  const requests: Request[] = [];
  const invoke: Invoke = async request => { requests.push(request); if (!outputs.length) throw new Error('Unexpected fixture call'); return { output: outputs.shift(), tokens, raw: { fixture: true } }; };
  return { requests, invoke };
}
let clock = 0;
const run = (invoke: Invoke, extra: Partial<Parameters<typeof runShort>[0]> = {}) =>
  runShort({ guide, brief: end, invoke, limits: { ...defaults }, mode: 'test-fixture', evidence, base: { why: text.why }, now: () => clock++, ...extra });

describe('the short-line rubric comes from the game', () => {
  test('four kinds (three before guide /10), each with the brief\'s six rules, every rule tied to fingerprinted sources', () => {
    expect(Object.keys(guide.kinds)).toEqual(['bark', 'why', 'debrief', 'label']); expect(Object.keys(loadShortGuide(root, 9).kinds)).toEqual(['bark', 'why', 'debrief']);
    for (const k of Object.values(guide.kinds)) {
      expect(k.rules.map(r => r.id)).toEqual(['USEFUL', 'TRUE', 'PLAIN', 'SHORT', 'NAMES', 'VOICE']);
      for (const r of k.rules) for (const id of r.sources) expect(guide.sources.some(s => s.id === id), `${r.id} ${id}`).toBe(true);
    }
    expect(guide.sources.every(s => /^[a-f0-9]{64}$/.test(s.sha256) && s.excerpt.trim())).toBe(true);
    const rule = (kind: 'bark' | 'why' | 'debrief', id: string) => guide.kinds[kind].rules.find(r => r.id === id)!.instruction;
    expect(rule('bark', 'USEFUL')).toContain('what is this stupid words at the start of every game?'); // Sky's cut, the failing example
    expect(rule('bark', 'USEFUL')).toContain('Friends first, force last. Remember.');
    expect(rule('bark', 'SHORT')).toContain('at most 12 words and 80 characters');
    expect(rule('why', 'SHORT')).toContain('a title at most 4 words and 32 characters'); // why.schema.ts LIMITS
    expect(rule('debrief', 'PLAIN')).toContain('the End card needs to be plain english and clear but not too formal');
    expect(rule('debrief', 'NAMES')).toContain('{op}');
    expect(rule('bark', 'VOICE')).toContain('A line another speaker could say unchanged fails'); // guide /4
    expect(rule('bark', 'USEFUL')).toContain('a line that reads it back is a tooltip, not speech, and fails');
    expect(loadShortGuide(root, 2).kinds.bark.rules.find(r => r.id === 'VOICE')!.instruction).not.toContain('another speaker'); // a /2 run replays with its own rules
    expect(guide.cell).toEqual(['Larthi', 'Hasti', 'Sethre', 'Arruns']);
    // guide /10 (brief 101): the HUD's labels, from Sky's rule on numbers in play and Glyph's round 2; a /9 guide rebuilds with the sources it ran with
    expect(guide.kinds.label!.rules.find(r => r.id === 'SHORT')!.instruction).toContain('no label says how long an act or a walk takes, nor a chance');
    expect(guide.kinds.label!.rules.find(r => r.id === 'TRUE')!.instruction).toContain('the HUD\'s countdowns are bare orange numbers');
    expect(guide.sources.find(s => s.id === 'hud')!.excerpt).toContain('No action durations in play'); expect(guide.sources.find(s => s.id === 'hud-words')!.excerpt).toContain('Goes loud');
    expect(loadShortGuide(root, 9).sources.some(s => s.id.startsWith('hud'))).toBe(false);
  });
});

describe('the families: every short line in the game, a family per run', () => {
  test('every live bark row is in exactly one family; parked moves and the Halcyon lines (pending Sky) are not played, so not written', () => {
    const keys = briefs.filter(b => b.kind === 'bark').flatMap(b => b.items.map(i => i.key));
    const live = (text.dialogue as { parked?: string; pending?: string }[]).filter(r => !r.parked && !r.pending).map(r => barkKey(r as never));
    expect(new Set(keys).size).toBe(keys.length);
    expect([...keys].sort()).toEqual([...live].sort());
    expect(keys.length).toBe(506); // brief 102's sweep cut five rows as filler: the patrol's three status lines, the camera room's alarm, a rival's "I'm out"; the guard's end of the call added 14 (guard-call.json)
    // Sky 2026-11-07 ("what is this stupid words at the start of every game?"): the game opens quiet. No start event (its four speakers would ask text.test.ts for
    // the cut barks), no start bark, and the start log line silent on purpose.
    expect(text.events.start).toBeUndefined(); expect(live.some(k => k.startsWith('start/'))).toBe(false); expect(text.silentLogs.start).toContain('stupid words');
  });
  test('a bark says what it must tell the player, from the game\'s own data', () => {
    const at = (id: string, key: string) => family(id).items.find(i => i.key === key)!;
    expect(at('bark/ack-feet', 'ack/run/scout').tells).toContain('Watchers notice it'); // the verb's own line (actions.run.does), as a fact not to read out (guide /4)
    expect(at('bark/ack-feet', 'ack/run/scout').tells).toMatch(/^Say: the Scout has the order and is on it.*Facts, on the order's card already and not to be read out/);
    expect(at('bark/read-people', 'read/listen/scout/fearful/waitress').tells).toContain('Inez is fearful, in plain words: frightened: Pressure works'); // guide /5
    expect(at('bark/read-people', 'read/listen/scout/fearful/waitress').tells).toContain('(GDD §04: responds to Pressure');
    expect(at('bark/done', 'done/turn/fixer/refused').tells).toContain('Said to the person\'s face after their reply'); // brief 085, the pinned line's own use
    expect(family('why/on-the-way').items.find(i => i.key === 'onWay.ways.walk')!.fields[1]!.fills.find(x => x.id === 'who')!.means).toMatch(/^the person walking/); // /5: per-cause fills
    expect(at('bark/done', 'done/turn/fixer/refused').fields[0]!.keep).toEqual([text.dialogue.find((r: { event: string; verb?: string; outcome?: string }) => r.event === 'done' && r.verb === 'turn' && r.outcome === 'refused').lines[0]]); // up to /12 the brief kept its first line (text.test.ts pinned it until brief 102)
    expect(at('bark/guard-call', 'guard-leaves-cafe/guard').fields[0]!.patterns[0]!.re).toBe('Break|Back to my post'); // Proof085Scenes
    expect(at('bark/ack-acts', 'ack/pay/fixer').stays).toContain('stock "On it."');
    expect(family('bark/ack-guns').items.filter(i => i.key.startsWith('ack/fire/')).map(i => i.speaker!.split(':')[0])).toEqual(['scout', 'fixer', 'tech', 'enforcer']); // guide /4: a verb's four voices side by side
    expect(at('bark/clock', 'rivals-coming/scout').fields[0]!.patterns.map(p => p.re)).toContain('Orbit Walk'); // guide /3: text.test.ts pins it
    expect(at('bark/machines', 'loop-ends/tech').stays).toBeNull(); // a bark the loop may cut
    // guide /8: the guard's call is never timed, and conversations.test.ts's banned words reach the rows it reads (the /4 run wrote "about a minute")
    const cafe = at('bark/guard-call', 'guard-at-cafe/scout'), ban = cafe.fields[0]!.patterns.find(p => !p.must)!;
    expect(cafe.tells).not.toMatch(/minute/);
    expect(new RegExp(ban.re, ban.flags).test('The door\'s unguarded for about a minute.')).toBe(true);
    expect(at('bark/ack-acts', 'ack/pay/fixer').fields[0]!.patterns.some(p => p.re === ban.re)).toBe(true);
    expect(loadShortBriefs(root, 7).find(b => b.id === 'bark/guard-call')!.items.find(i => i.key === 'guard-at-cafe/scout')!.tells).toMatch(/about a minute/); // a /7 run rebuilds as it ran
  });
  test('every why cause, variant, way and word is in a family', () => {
    const keys = new Set(briefs.filter(b => b.kind === 'why').flatMap(b => b.items.map(i => i.key)));
    for (const g of ['alert', 'police'] as const) for (const [k, c] of Object.entries(text.why[g] as Record<string, { variants?: object }>)) {
      expect(keys.has(`${g}.${k}`), `${g}.${k}`).toBe(true);
      for (const v of Object.keys(c.variants ?? {})) expect(keys.has(`${g}.${k}.variants.${v}`)).toBe(true);
    }
    for (const k of Object.keys(text.why.onWay.ways)) expect(keys.has(`onWay.ways.${k}`)).toBe(true);
    for (const k of [...Object.keys(text.why.by).map(x => `by.${x}`), ...Object.keys(text.why.places).map(x => `places.${x}`), 'again', 'effect.3', 'onWay.stopped', 'meter.empty']) expect(keys.has(k), k).toBe(true);
  });
  test('the debrief page: concept A\'s keys from Glyph\'s stand-ins, a row for every Trail cause the engine records, a Next time for every fail the fail camera knows', () => {
    const items = briefs.filter(b => b.kind === 'debrief').flatMap(b => b.items), glyph = loadShortBriefs(root, 8).filter(b => b.kind === 'debrief').flatMap(b => b.items);
    const at = (k: string, from = items) => from.find(i => i.key === k)!;
    expect(at('head.win', glyph).current.label).toEqual(['The Handover · done at {clock}']); // Glyph's caps, set in capitals by the host (/4–/8)
    expect(at('reasons.time.win', glyph).current.note).toEqual(['{clock} to spare']); // Glyph's {left}, as a fill the host has
    expect(at('next.by.rivals-reach-vale.lookout', glyph).current.line).toEqual(['Send their lookout away before the car goes round.']);
    expect(at('head.win').current.label).toEqual([text.debrief.page.head.win]); // guide /9: today is what the page shows now
    expect(at('next.by.rivals-reach-vale.lookout').current.line).toEqual([text.debrief.page.next.by['rivals-reach-vale'].lookout.line]);
    expect(at('next.by.rivals-reach-vale.lookout').tells).toContain('Deal with their lookout first.'); // the fail camera's tip
    expect(at('trail.law').tells).toContain('+2 for firing on someone, +2 if they are hit, +2 if they go down'); // the rulebook's values
    const nexts = items.filter(i => i.key.startsWith('next.by.')).length, tips = Object.entries(text.less.failcam.tip as Record<string, object>).filter(([k]) => k !== 'engine-error').reduce((n, [, v]) => n + Object.keys(v).length, 0);
    expect(nexts).toBe(tips);
    expect(items.filter(i => i.key.startsWith('trail.rows.')).map(i => i.key.slice(11))).toContain('take-unturned');
  });
  test('guide /9: every key Latch\'s page reads has an item; Spent names who; the other folds, the withdrawal, a killing and the joiner have words', () => {
    const items = briefs.filter(b => b.kind === 'debrief').flatMap(b => b.items), at = (k: string) => items.find(i => i.key === k)!;
    // a key's path on the page, as apply writes it: one field is the key itself, more are keys under it
    const paths = new Set(items.flatMap(i => i.fields.length === 1 ? [i.key] : i.fields.map(f => `${i.key}.${f.id}`)));
    const stand = join(root, 'unity/Assets/Suborned/Resources/Debrief099/standins.json'); // Latch's marked stand-ins, until the page has every key (then the file can go)
    if (existsSync(stand)) {
      const leaves = (o: unknown, p: string[] = []): string[] => typeof o === 'string' ? [p.join('.')] : Object.entries(o as object).flatMap(([k, v]) => leaves(v, [...p, k]));
      const read = leaves(JSON.parse(readFileSync(stand, 'utf8'))).filter(k => k !== 'note' && !k.startsWith('missions.')); // missions.one/many: no longer read (Latch, round 2)
      expect(read.filter(k => !paths.has(k))).toEqual([]);
    }
    for (const k of ['help', 'pay']) {
      const f = at(`reasons.cash.${k}`).fields[0]!;
      expect(f.fills.map(x => x.id), k).toEqual(['who']); expect(f.patterns.some(p => p.must && p.re === '\\{who\\}'), k).toBe(true);
    }
    expect(at('reasons.cash.both').fields[0]!.fills).toEqual([]); // two or more: the host gives no {who}
    expect(at('reasons.cash.both').tells).toContain('two Pays');
    expect(at('folds.time').fields.map(f => f.id)).toEqual(['head', 'means']);
    expect(at('head.withdraw').tells).toContain('Go pressed before Popovic was aboard');
    const old = loadShortBriefs(root, 8).filter(b => b.kind === 'debrief');
    expect(old.some(b => b.id === 'debrief/folds')).toBe(false); // a /4–/8 run rebuilds with the families it ran with
    expect(old.flatMap(b => b.items).find(i => i.key === 'reasons.cash.help')!.fields[0]!.fills).toEqual([]);
  });
  test('guide /10: the HUD\'s labels (brief 101): the first shot\'s row, every timeline mark, the lozenge\'s cards, today\'s words, and no number of a label\'s own', () => {
    const b = family('label/hud'), keys = b.items.map(i => i.key), ok = today(b), bad = (s: ShortSet) => checkShort(b, s, guide).map(i => i.reason).join('\n');
    expect(keys[0]).toBe('less.street.loud'); expect(b.items[0]!.current.label).toEqual([text.less.street.loud]);
    expect(keys.filter(k => k.startsWith('briefing.timeline.'))).toEqual(text.briefing.timeline.map((m: { at: string }) => `briefing.timeline.${m.at}`));
    for (const k of Object.keys(text.book.feed.cards)) expect(keys.includes(`book.feed.cards.${k}`), k).toBe(!['police', 'seal', 'rivals-follow'].includes(k)); // rivals-follow: guide /14's own family (brief 102) // the police title is the why block's (and its {s} keys the ticket); the seal is never a card
    expect(b.items.find(i => i.key === 'book.feed.cards.boxed')!.current).toEqual({ title: [text.book.feed.cards.boxed.title], line: [text.book.feed.cards.boxed.line] }); // today is the file's
    expect(b.items.find(i => i.key === 'book.feed.cards.seen')!.fields.map(f => f.id)).toEqual(['title']); // the host clears its line
    expect(b.items.every(i => i.stays)).toBe(true); expect(keys).toContain('book.feed.of');
    expect(bad(edit(ok, 'briefing.timeline.5:10', ['Chime in 12 s'], 'short'))).toMatch(/briefing\.timeline\.5:10\.short: a label carries no number of its own/);
    expect(bad(edit(ok, 'less.street.loud', ['75 %'], 'label'))).toMatch(/less\.street\.loud\.label: a label carries no number of its own/);
    expect(bad(edit(ok, 'book.feed.cards.down', ['Arruns is down'], 'title'))).toMatch(/a label names an agent with \{agent\}/);
    expect(bad(edit(ok, 'book.feed.cards.for-car', ['Rivals going for our car'], 'title'))).toMatch(/book\.feed\.cards\.for-car\.title: 5 words; this slot holds 4/); // GDD §10: a card's title is four words or fewer (the title before the run)
    // guide /11 (host logic on the /10 words): the lines brief 096's host tests read are held; the line slot is 32
    const line = (k: string) => b.items.find(i => i.key === k)!.fields.find(f => f.id === 'line')!;
    expect(line('book.feed.cards.hurt-carry').patterns.map(p => p.re)).toEqual(['\\{n\\} health', 'Popovic']); expect(line('book.feed.cards.vale-out').patterns.map(p => p.re)).toEqual(['\\{police\\}']);
    expect(line('book.feed.cards.boxed').chars).toBe(32);
    expect(bad(edit(ok, 'book.feed.cards.vale-out', ['the rest get clear'], 'line'))).toMatch(/vale-out\.line: needs \/\\\{police\\\}\//);
    expect(bad(edit(ok, 'book.feed.cards.hurt-carry', ['carrying Popovic: cover him'], 'line'))).toMatch(/hurt-carry\.line: needs /);
    const at10 = loadShortBriefs(root, 10).find(x => x.id === 'label/hud')!; // a /10 run rebuilds as it ran: no held lines, a slot of 28
    expect(at10.items.every(i => i.fields.every(f => !f.patterns.length))).toBe(true); expect(at10.items.find(i => i.key === 'book.feed.cards.boxed')!.fields[1]!.chars).toBe(28);
    expect(loadShortBriefs(root, 9).some(x => x.kind === 'label')).toBe(false); // a /4–/9 run rebuilds with the families it ran with
  });
  test('guide /9: a police title names who or what is coming, the meter keeps its level, and the words brief 100\'s HUD tests read are held', () => {
    const items = briefs.filter(b => b.kind === 'why').flatMap(b => b.items), field = (k: string, id: string) => items.find(i => i.key === k)!.fields.find(f => f.id === id)!;
    const titles = items.filter(i => /^police\.[a-z-]+$/.test(i.key)).map(i => i.fields.find(f => f.id === 'title')!);
    expect(titles.length).toBeGreaterThan(10);
    for (const t of titles) expect(t.patterns.some(p => p.must && new RegExp(p.re, p.flags).test('Police coming') && !new RegExp(p.re, p.flags).test('Shots heard')), t.id).toBe(true);
    expect(field('police.body', 'title').keep).toEqual(['Police coming']); expect(field('police.more', 'short').keep).toEqual(['the search goes on']);
    expect(field('onWay.ways.radio', 'plate').keep).toEqual(['Radioing it in · {s} s']);
    const head = field('meter.head', 'plate').patterns;
    expect(head.filter(p => p.re === '\\{alert\\}')).toHaveLength(1); expect(head.some(p => p.re === '^Why it')).toBe(true);
    const p = family('why/police'), set = today(p);
    expect(issues(p, edit(set, 'police.body', ['Body found'], 'title'))).toContain('a police title names who or what is coming');
    expect(issues(p, set)).toBe('');
    const old = loadShortBriefs(root, 8).filter(b => b.kind === 'why').flatMap(b => b.items);
    expect(old.find(i => i.key === 'police.body')!.fields.find(f => f.id === 'title')!.keep).toEqual([]); // the /5–/7 runs rebuild as they ran
    expect(loadShortGuide(root, 8).kinds.why.rules[0]!.instruction).not.toContain('who or what is coming');
    expect(guide.kinds.why.rules[0]!.instruction).toContain('who or what is coming');
  });
  test('the prompt shows each item once with its limits, fills and facts, and the speaker\'s voice once', () => {
    const p = shortPrompt('generate', guide, family('bark/noticed'), null, null);
    expect(p).toContain('"key": "run-seen/patrol"'); expect(p).toContain('Walking is encouraged'); expect(p).toContain('"maxWords": 12');
    expect(p.split('a clockwork patrol: fixed polite phrases').length - 1).toBe(1);
  });
});

const WHY_19 = ['why/witnessed', 'why/reported', 'why/gunfire', 'why/police', 'why/words'];
describe('code checks', () => {
  test('today\'s barks and why lines pass; a passing count of words', () => {
    for (const b of briefs.filter(x => x.kind === 'bark' || x.kind === 'why' && !WHY_19.includes(x.id))) expect(issues(b, today(b)), b.id).toBe(''); // the page's and the labels' today are briefed to change (their own tests)
    // guide /19 (brief 101) rewrote the cause families' shorts to fit the news lozenge and released /9's pins on them (the line keeps who and whom): today's why lines pass the current guide's checks (lozenge.test.ts)
    expect(shortWords('Going to {to} · {s} s')).toBe(3);
  });
  test('length, fills, shape, the filter, names, digits, repeats, cuts, missing items, pinned lines and patterns', () => {
    expect(issues(end, edit(good, 'win/fixer', ['We have him, and nobody saw a thing, so everyone can finally breathe.', 'Away.']))).toContain('13 words; this slot holds 12');
    expect(issues(end, edit(good, 'win/fixer', ['We have {who}.', 'Away.']))).toContain('{who} is not a fill this item has (none)');
    expect(issues(end, edit(good, 'win/fixer', ['We have him', 'Away.']))).toContain('a spoken line ends in . ? or !');
    expect(issues(end, edit(good, 'win/fixer', ['Check the app.', 'Away.']))).toContain('"app" is on the mission\'s word filter');
    expect(issues(end, edit(good, 'win/fixer', ['Hasti, we did it.', 'Away.']))).toContain('"Hasti" is a cell name');
    expect(issues(end, edit(good, 'win/fixer', ['We did it, Frank.', 'Away.']))).toContain('"Frank" mid-sentence');
    expect(issues(end, edit(good, 'win/councillor', ['I will pay you 50,000.', 'Release me.']))).toContain('no digits in a person\'s mouth');
    expect(issues(end, edit(good, 'win/fixer', ['Away we go.', 'Away we go!']))).toContain('repeats another variant');
    expect(issues(end, edit(good, 'win/fixer', ['We have him.']))).toContain('1 variants; write 2');
    expect(issues(end, { items: good.items.filter(i => i.key !== 'win/tech') })).toContain('win/tech: missing');
    expect(issues(end, { items: good.items.map(i => i.key === 'win/fixer' ? { ...i, cut: true, reason: 'filler', fields: [] } : i) })).toContain('may not be cut');
    const done = family('bark/done'), dset = today(done);
    expect(issues(done, edit(dset, 'done/turn/fixer/refused', ['A no, then.', 'That went badly.', 'A no. Noted.']))).toContain(`keep "${text.dialogue.find((r: { event: string; verb?: string; outcome?: string }) => r.event === 'done' && r.verb === 'turn' && r.outcome === 'refused').lines[0]}"`);
    expect(issues(done, edit(dset, 'done/turn/fixer/turned', ['He is with us now.', 'Good. That\'s settled.', 'Done. You\'re with us now.']))).toContain('must not have');
    const call = family('bark/guard-call');
    expect(issues(call, edit(today(call), 'guard-leaves-cafe/guard', ['Duty calls.', 'Back to the door.']))).toContain('Proof085Scenes');
  });
  test('guide /5: a bark is checked against the other rows of the table, so two families never ship one line', () => {
    const arrive = family('bark/arrive'), set = edit(today(arrive), 'arrive/enter/enforcer', ['Going in.', 'Through the door.', 'Right. In.']); // "Going in." is ack/enter/enforcer's
    expect(checkShort(arrive, set, guide, { why: text.why, dialogue: text.dialogue }).map(i => i.reason).join('\n')).toContain('the same line as ack/enter/enforcer');
    expect(checkShort(arrive, set, loadShortGuide(root, 4), { dialogue: text.dialogue }).some(i => i.reason.includes('ack/enter/enforcer'))).toBe(false); // a /4 run is checked as it ran
    expect(checkShort(arrive, today(arrive), guide, { dialogue: text.dialogue })).toEqual([]);
  });
  test('the why block\'s own check (why.schema.ts) on the family\'s lines, kept to the family', () => {
    const p = family('why/police'), set = today(p);
    expect(issues(p, edit(set, 'police.more', ['More police are on the way now'], 'title'))).toContain('why.schema.ts: police.more.title: 7 words (> 4)');
    expect(issues(p, edit(set, 'police.more', ['the search goes on.'], 'short'))).toContain('a short starts lower case (or with a name) and has no full stop');
  });
  test('the debrief page keeps the player\'s words: no engine word, no exclamation, the slot\'s size', () => {
    const h = family('debrief/head'), set = today(h);
    const filled = { items: set.items.map(i => ({ ...i, fields: i.fields.map(f => ({ ...f, lines: f.lines.length ? f.lines : ['ok'] })) })) };
    expect(issues(h, edit(filled, 'ways.replay', ['Watch the replay'], 'label'))).toContain('an engine word');
    expect(issues(h, edit(filled, 'ways.again', ['Try again!'], 'label'))).toContain('no exclamation marks');
    expect(issues(h, edit(filled, 'how.called', ['Inez called the guard over to her café'], 'note'))).toContain('this slot holds 4');
  });
});

describe('the loop on a family: generate → score → revise → score again (fixtures)', () => {
  test('Fable writes, Motive scores, the reasons reach the reviser, and the revision is scored again', async () => {
    const f = fixture([filler, rejected, better, accept]);
    const r = await run(f.invoke);
    expect([r.status, r.rounds.map(x => x.evaluation.score)]).toEqual(['ACCEPTED', [4, 10]]);
    expect(f.requests.map(q => `${q.stage}:${q.role}`)).toEqual(['generate:dialogue-designer', 'evaluate:systems-designer', 'refine:dialogue-designer', 'evaluate:systems-designer']);
    expect(f.requests[2]!.prompt).toContain(rejected.reason); expect(f.requests[2]!.prompt).toContain('Waiting. Always waiting.');
    expect(f.requests[0]!.prompt).toContain('Family bark/end');
    expect(f.requests[3]!.prompt).toContain('Your last score and findings'); expect(f.requests[3]!.prompt).toContain(rejected.reason); // guide /2: Motive holds the writer to its own findings
    expect(f.requests[1]!.prompt).not.toContain('Your last score and findings');
    expect(f.requests[2]!.prompt).toContain('Change only the lines a finding or a code check names');
  });
  test('the first live run\'s protocol (guide /1) still builds as it ran: no history for the evaluator, the old reviser', async () => {
    const old = loadShortGuide(root, 1), f = fixture([filler, rejected, better, accept]);
    const r = await run(f.invoke, { guide: old, brief: loadShortBriefs(root, 1).find(b => b.id === 'bark/end')!, evidence: archiveEvidence(root, old, store) }); // a guide archives its own sources (/10 adds brief 101's two)
    expect([old.schema, r.status]).toEqual(['suborned.style-short-guide/1', 'ACCEPTED']);
    expect(f.requests[3]!.prompt).not.toContain('Your last score and findings');
    expect(f.requests[2]!.prompt).toContain('Keep what already works.');
    expect((await verifyShort(r, root, store)).errors).toEqual([]);
  });
  test('a 10 does not override a failed code check; quotes must be real; limits end the loop', async () => {
    const r = await run(fixture([edit(good, 'win/fixer', ['We have him']), accept]).invoke, { limits: { ...defaults, maxRevisions: 0 } });
    expect([r.status, r.rounds[0]!.hardIssues[0]!.rule]).toEqual(['INCOMPLETE', 'SHORT']);
    const q = await run(fixture([filler, { ...rejected, findings: [{ ...rejected.findings[0]!, quote: 'not in it' }] }]).invoke);
    expect([q.status, q.stopReason]).toEqual(['ERROR', 'The evaluator quoted words that are not in the lines']);
    expect((await run(fixture([filler, rejected, filler]).invoke, { limits: { ...defaults, maxCalls: 3 } })).status).toBe('INCOMPLETE');
    expect((await run(fixture([{ items: [{ key: 'win/nobody', cut: false, reason: '', fields: [] }] }]).invoke)).status).toBe('ERROR'); // not a key of the family
  });
});

describe('saved short runs re-check without a model', () => {
  test('every saved short run (runs/style-agent/short) rebuilds from its archived sources, each with the guide version it ran with', async () => {
    const dir = join(root, 'runs/style-agent/short'), names = existsSync(dir) ? readdirSync(dir).filter(n => existsSync(join(dir, n, RUN_FILE))) : [];
    for (const n of names) expect((await verifyShort(readRun(join(dir, n, RUN_FILE)), root)).errors, n).toEqual([]);
  });
  test('a run rebuilds exactly from archived sources; any edit to it fails', async () => {
    const r = await run(fixture([filler, rejected, better, accept]).invoke);
    expect((await verifyShort(r, root, store)).errors).toEqual([]);
    const edits: ((x: ShortResult) => void)[] = [
      x => { x.rounds[0]!.evaluation.score = 9; }, x => { x.rounds[1]!.set.items[0]!.fields[0]!.lines[0] = 'Changed.'; }, x => { x.calls[2]!.prompt = 'Ignore the evaluator'; },
      x => { x.brief.items[0]!.tells = 'Anything'; }, x => { x.guide.kinds.bark.rules[0]!.instruction = 'Anything goes'; }, x => { x.stopReason = 'Trust me'; },
      x => { x.evidence!.sources[0]!.sha256 = 'a'.repeat(64); }, x => { x.mode = 'live'; x.authorization = { provider: 'codex', model: 'm', approvedBy: 'a' }; },
    ];
    for (const e of edits) { const x = structuredClone(r); e(x); expect((await verifyShort(x, root, store)).ok).toBe(false); }
  });
  test('a live record is bound to its Codex transcript, the role card, the model and the approval (synthetic transcript)', async () => {
    const approval = { provider: 'codex' as const, model: 'test-model-only', approvedBy: 'UNIT TEST, not an approval' };
    const outputs: unknown[] = [filler, rejected, better, accept];
    const invoke: Invoke = async request => {
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
    const r = await run(invoke, { mode: 'live', authorization: approval });
    expect((await verifyShort(r, root, store)).errors).toEqual([]);
    const x = structuredClone(r); x.calls[0]!.tokens = 1;
    expect((await verifyShort(x, root, store)).ok).toBe(false);
  });
});

describe('the apply step', () => {
  test('an accepted family\'s lines replace their rows where they stand; the file still parses and nothing else moves', async () => {
    const r = await run(fixture([better, accept]).invoke), dir = mkdtempSync(join(tmpdir(), 'suborned-short-apply-')), file = join(dir, 'text.json');
    copyFileSync(join(root, TEXT_FILE), file);
    const before = readFileSync(file, 'utf8');
    await applyShort(false, { runs: new Map([[r.brief.id, { run: r, path: dir }]]), file });
    const after = readFileSync(file, 'utf8'), t = TextFile.parse(JSON.parse(after));
    expect(t.dialogue.find(e => e.event === 'win' && e.speaker === 'fixer')!.lines).toEqual(better.items.find(i => i.key === 'win/fixer')!.fields[0]!.lines);
    const changed = after.split('\n').filter((l, i) => l !== before.split('\n')[i]);
    expect(changed).toHaveLength(1); expect(changed[0]).toContain('"event":"win","speaker":"fixer"');
    await applyShort(false, { runs: new Map([[r.brief.id, { run: r, path: dir }]]), file, restore: new Set([r.brief.id]) }); // a review can undo one family
    expect(readFileSync(file, 'utf8')).toBe(before);
  });
  test('guide /10: a label family\'s words replace their strings where they stand, the shot row\'s label also as the Alt outcome; restore gives the file back', async () => {
    const b = family('label/hud'), words = edit(edit(edit(today(b), 'less.street.loud', ['Turns loud'], 'label'), 'briefing.timeline.2:35', ['Rival car'], 'short'), 'book.feed.cards.boxed', ['their car blocks ours'], 'line');
    const fake = { brief: b, rounds: [{ set: words, evaluation: accept, hardIssues: [] }] } as unknown as ShortResult; // apply reads the family and its last round
    const dir = mkdtempSync(join(tmpdir(), 'suborned-short-labels-')), file = join(dir, 'text.json');
    copyFileSync(join(root, TEXT_FILE), file);
    const before = readFileSync(file, 'utf8');
    await applyShort(false, { runs: new Map([[b.id, { run: fake, path: dir }]]), file });
    const after = readFileSync(file, 'utf8'), t = TextFile.parse(JSON.parse(after));
    expect(t.less.street.loud).toBe('Turns loud'); expect(t.less.alt.outcome.loud).toBe('Turns loud');
    expect(t.briefing.timeline.find(m => m.at === '2:35')!.short).toBe('Rival car'); expect(t.book.feed!.cards.boxed!.line).toBe('their car blocks ours');
    const changed = after.split('\n').filter((l, i) => l !== before.split('\n')[i]);
    expect(changed).toHaveLength(4); // each string where it stood; nothing else moved
    await applyShort(false, { runs: new Map([[b.id, { run: fake, path: dir }]]), file, restore: new Set([b.id]) });
    expect(readFileSync(file, 'utf8')).toBe(before);
  });
});
