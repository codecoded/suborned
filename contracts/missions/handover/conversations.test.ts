// Done-check for scenes.conversations (Fable, brief 085): the words are the ones Sky agreed or read, each plays only for the person and situation it was written for,
// every other act keeps its scenes.items lines, and the engine can't put these words over a different person. Brief 096: cutaway lines are full sentences
// (≤ 25 words, 160 characters, two sentences, one question; ≤ 16 lines), nobody explains the game, and nobody pays at the Bureau. Run: bun test contracts/missions/handover
import { describe, expect, test } from 'bun:test';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { check, PAYS_AT_BUREAU, specs, TUTORIAL } from '../../../tools/dialogue/conversations.ts';
import { TEMPERAMENTS } from '../../people/people.rules.schema.ts';
import { CONVERSATION_MAX_BEATS, CUTAWAY_MAX_LINE, CUTAWAY_MAX_QUESTIONS, CUTAWAY_MAX_SENTENCES, CUTAWAY_MAX_WORDS, Conversation, ConversationBeat, conversationLines, lineWords, lookupConversation, questionCount, sentenceCount, type ConversationQuery } from './conversations.schema.ts';
import { MAX_LINE, MAX_WORDS, TextFile, lookupScene, wordCount } from './text.schema.ts';
import { PeopleRules } from '../../people/people.rules.schema.ts';
import { Rulebook } from '../../rules/rulebook.schema.ts';
import * as R from '../../rules/rulebook.ref.ts';

const root = join(import.meta.dir, '..', '..', '..');
const json = (p: string) => JSON.parse(readFileSync(join(root, p), 'utf8'));
const t = TextFile.parse(json('contracts/missions/handover/text.json'));
const c = t.scenes.conversations!;
const approved = json('briefs/085/fable/approved.json') as { scenes: { id: string; beats: [string, string][]; learned: string[]; opens: string[] }[] };
const byId = (id: string) => c.items.find((x) => x.id === id)!;
const pick = (q: ConversationQuery) => lookupConversation(c, q)?.id;
const inez = { about: 'waitress', temperament: 'fearful' };
/** The scenes Sky has agreed as they play. The rest play too, awaiting Sky's agreement in play; approved.json keeps the last words Sky agreed for them. */
const AGREED: string[] = [], AWAITING = ['listen-inez', 'help-inez-1', 'help-inez-2', 'help-inez-2-known', 'pressure-inez', 'pay-driver', 'help-driver', 'help-driver-known', 'call-inez', 'call-inez-pressure'];
/** Brief 096 step 2: the sample's second draft, which Sky read on 2026-11-05 ("conversations definitely improved"), moved in word for word. */
const SAMPLE = ['listen-inez', 'pressure-inez', 'pay-driver', 'call-inez', 'call-inez-pressure'];
/** Rewritten after engine 0.44.0 (Sky's option 1 for Help on a Fearful Inez): the small offer she refuses, the full offer that lands, and that offer after a refusal. */
const HELP = ['help-inez-1', 'help-inez-2', 'help-inez-2-known'];
/** Engine 0.45.0: a Mercenary driver takes Help in one visit after his Listen; after a refused small offer it's dearer. */
const DRIVER = ['help-driver', 'help-driver-known'];
const sample = json('briefs/096/fable/conversations-sample.json') as { items: { id: string; beats: object[] }[]; help044: { items: { id: string; beats: object[]; learned: string[]; opens: string[] }[] }; help045: { items: { id: string; beats: object[]; learned: string[]; opens: string[] }[] } };
const shown = (id: string) => conversationLines(c, byId(id)).map((l) => [l.name, l.text]);

describe('the words', () => {
  test('every scene Sky agreed is word for word what he agreed, in order, cards included', () => {
    for (const a of approved.scenes.filter((x) => AGREED.includes(x.id))) {
      expect(byId(a.id).status, a.id).toBe('agreed');
      expect(shown(a.id), a.id).toEqual(a.beats);
      expect(byId(a.id).learned, a.id).toEqual(a.learned);
      expect(byId(a.id).opens, a.id).toEqual(a.opens);
    }
    expect(approved.scenes.length).toBe(7);
  });
  test('the five rewritten scenes are the sample Sky read, word for word, view fields included, and keep the cards Sky agreed', () => {
    for (const id of SAMPLE) {
      expect(byId(id).beats, id).toEqual(sample.items.find((x) => x.id === id)!.beats as typeof c.items[number]['beats']);
      expect(byId(id).status, id).toBe('awaiting-sky');
      const a = approved.scenes.find((x) => x.id === id)!;
      expect([byId(id).learned, byId(id).opens], id).toEqual([a.learned, a.opens]);
    }
    for (const x of [...sample.help044.items, ...sample.help045.items]) expect([byId(x.id).beats, byId(x.id).learned, byId(x.id).opens, byId(x.id).status], x.id).toEqual([x.beats as typeof c.items[number]['beats'], x.learned, x.opens, 'awaiting-sky']); // the page Sky reads
    expect(sample.help045.items.map((x) => x.id)).toEqual(DRIVER);
    expect([...SAMPLE, ...HELP, ...DRIVER].sort()).toEqual(c.items.map((x) => x.id).sort());
    expect(byId('listen-inez').beats.at(-1)).toEqual({ speaker: 'scout', line: "She's deep in debt to the Bureau and can't keep up. And she's seeing Arthur, the guard on the Office door." }); // Sky: "Fables rewrite is better"
  });
  test('the scenes he has not read are flagged but gated by nothing; the pressed call ends with the guard coming, and leaves the telling to the street', () => {
    expect(c.items.filter((x) => x.status !== 'agreed').map((x) => x.id).sort()).toEqual([...AWAITING].sort());
    for (const id of AWAITING) expect(lookupConversation(c, { ...byId(id).when, turned: false, helped: byId(id).when.helped ?? false, outcome: byId(id).when.outcome })?.id, id).toBe(id);
    const run = byId('call-inez-pressure');
    expect(run.status).toBe('awaiting-sky');
    expect(run.beats.at(-1)!.speaker).toBe('guard'); expect(run.beats.at(-1)!.line).toMatch(/I'm coming/);
    expect(run.beats.map((b) => b.line).join(' ')).not.toMatch(/someone knows|suspicious/i);
    expect(run.street!.map((s) => [s.on, s.pressed])).toEqual([['guard.at-cafe', true]]);
    const said = run.street![0]!.beats;
    expect(said.map((b) => b.speaker)).toEqual(['waitress', 'guard']);
    expect(said[0]!.line).toMatch(/someone knows/i); expect(said[0]!.line).toMatch(/suspicious/i);
    expect(said[1]!.line).toMatch(/door/); expect(said[1]!.line).not.toMatch(/\brun/i);
    expect(wordCount(run.street![0]!.beat)).toBeLessThanOrEqual(4);
    expect(c.items.filter((x) => x.street).map((x) => x.id)).toEqual(['call-inez-pressure']);
    for (const b of said) expect(wordCount(b.line)).toBeLessThanOrEqual(MAX_WORDS);
  });
  test('Pressure\'s price is live: the Alt line says what it loses, and the door field shows it at rest', () => {
    const live = t.actions.pressure!.onTarget!.waitress!;
    expect(live).toBe("Quick and free, but she'll warn him. The door stays clear about 1:40, not 3:00.");
    expect(live.length).toBeLessThanOrEqual(MAX_LINE);
    const P = t.less.price, door = P.fields.door!;
    expect(door.text).toBe('Door {clock}');
    expect(door.shows).toContain('GuardCallWindows');
    expect(P.order.indexOf('door')).toBe(P.order.indexOf('trail') + 1);
    expect(P.verbs.pressure).toContain('door'); expect(P.verbs.call).toContain('door');
    for (const v of ['ask', 'pay', 'help', 'listen'] as const) expect(P.verbs[v]).not.toContain('door');
    expect(P.examples['pressure-inez']).toContain('Door 1:40');
  });
  test('the three held moments and the leaned-on words, as Sky agreed; every field is on the line it names', () => {
    const fields = c.items.flatMap((sc) => sc.beats.flatMap((b, i) => { const { speaker: _s, line: _l, ...view } = b; return Object.keys(view).length ? [[sc.id, i + 1, view]] : []; }));
    expect(fields).toEqual([
      ['listen-inez', 7, { lean: 'guard' }],
      ['listen-inez', 11, { lean: 'yourself', revealAfter: "He's never said a word to me.", revealS: 0.9 }],
      ['help-inez-1', 10, { lean: 'Today' }],
      ['help-inez-2', 1, { lean: 'all' }],
      ['help-inez-2', 5, { lean: 'Arthur' }],
      ['help-inez-2-known', 2, { lean: 'all' }],
      ['help-inez-2-known', 6, { lean: 'Arthur' }],
      ['call-inez', 5, { holdBefore: 1.2 }],
      ['call-inez', 8, { lean: 'you' }],
      ['call-inez', 10, { holdAfter: 1.5, holdOn: 'waitress' }],
      ['call-inez-pressure', 5, { holdBefore: 1.2 }],
    ]);
    const beat = byId('listen-inez').beats[10]!, bad = (x: object) => ConversationBeat.safeParse({ ...beat, ...x }).success;
    expect(ConversationBeat.safeParse(beat).success).toBe(true);
    expect(bad({ lean: 'di' })).toBe(false); // not a whole word
    expect(bad({ revealAfter: 'You just did.' })).toBe(false); // not the line's start
    expect(bad({ revealS: undefined })).toBe(false);
    expect(bad({ holdOn: 'waitress' })).toBe(false); // no holdAfter to be on
    expect(conversationLines(c, byId('call-inez'))[4]).toEqual({ speaker: 'waitress', name: 'Inez', text: "Arthur, it's me. How's your shift going, love?", holdBefore: 1.2 });
  });
  test('look C\'s words and the plain lock reason', () => {
    expect(c.ui).toEqual({ new: 'New', voice: { milena: 'Milena, inside' } });
    expect(t.locks['target-unknown']!.short).toBe("Can't act on that");
  });
  test('a pressed Inez is never told how long the door stays clear in speech, and a card can\'t point at one way of blinding the camera', () => {
    for (const id of ['pressure-inez', 'call-inez-pressure']) expect(byId(id).beats.map((b) => b.line).join(' '), id).not.toMatch(/few minutes|a while|three minutes/i);
    for (const id of ['call-inez', 'call-inez-pressure']) { expect(byId(id).opens[0], id).toMatch(/Camera blind\?/); expect(wordCount(byId(id).opens[0]!)).toBeLessThanOrEqual(6); }
    expect(byId('help-inez-1').beats.map((b) => b.line).join(' ')).toMatch(/keep your money/); // engine 0.44.0: the small offer is refused and no money changes hands
    for (const id of ['help-inez-2', 'help-inez-2-known', ...DRIVER]) expect(byId(id).beats.map((b) => b.line).join(' '), id).not.toMatch(/the rest|second|half/i); // one visit, the whole debt
    for (const id of DRIVER) expect(byId(id).beats.map((b) => b.line).join(' '), id).not.toMatch(/\$|\d|thousand|hundred/i); // no amount spoken: the card shows it
  });
  test('the alarm\'s walk back never says he runs: he goes at a guard\'s pace, 1.4 m/s', () => {
    const said = t.dialogue.filter((e) => e.event === 'guard-recalled').flatMap((e) => e.lines);
    expect(said.length).toBeGreaterThan(0);
    expect(said.filter((l) => /\brun|\bfast\b|\bsprint/i.test(l))).toEqual([]);
    expect(t.events['guard-recalled']!.trigger).not.toMatch(/\bruns\b/);
  });
  test('the capture clip\'s plate uses the game\'s names for the two doors, and nothing longer', () => {
    const c = t.scenes.clip!, the = (x: string) => `The ${x.toLowerCase()}`;
    expect(the(c.service)).toBe(t.targets['stash-door']!.name);
    expect(the(c.public)).toBe(t.targets.door!.name);
    expect(c.both).toBe('Doors held');
    for (const w of [c.service, c.public, c.both]) { expect(wordCount(w)).toBe(2); expect(w).not.toMatch(/[.!?]/); }
    expect(t.less.fates['scenes.clip']).toBe('label');
  });
  test('no reachable line still has the spill, an envelope, a bonus or an as-promised', () => {
    const reach = t.scenes.items.filter((s) => s.kind !== 'city' && s.verb !== 'watch' && (s.temperament === undefined || s.temperament === (s.about === 'waitress' ? 'fearful' : 'mercenary'))).flatMap((s) => s.variants.flatMap((v) => v.beats.flatMap((b) => ('line' in b ? [b.line] : []))));
    const town = t.scenes.items.filter((s) => s.event === 'guard-at-cafe').flatMap((s) => s.variants.flatMap((v) => v.beats.flatMap((b) => ('line' in b ? [b.line] : []))));
    const said = t.dialogue.filter((e) => e.event === 'guard-at-cafe' || e.event === 'guard-lured' || e.event === 'guard-back' || e.temperament === undefined && ['ack', 'arrive', 'done'].includes(e.event) || e.temperament === 'fearful' && e.event === 'confirm').flatMap((e) => e.lines);
    const bad = /spill|coffee|clumsy|\bcloth\b|envelope|\bbonus\b|as promised|as agreed|half now|\b(a|one|five) minutes?\b|tonight|evening/i;
    expect([...reach, ...town, ...said].filter((l) => bad.test(l))).toEqual([]);
    expect(t.actions.call!.onTarget!.waitress).not.toMatch(/spill/);
  });
  test('each scene passes the generator\'s own checks: the limits, nothing in hand, a few minutes, no lender, no lesson, no Bureau counter', () => {
    for (const s of specs) {
      const sc = byId(s.id), beats = conversationLines(c, sc).map((l) => ({ speaker: l.name, line: l.text }));
      expect(check(s, { beats, learned: sc.learned, opens: sc.opens }), s.id).toEqual([]);
    }
    expect(specs.map((s) => s.id).sort()).toEqual(c.items.map((x) => x.id).sort());
  });
  test('every cutaway line is full sentences in the cutaway\'s limits, every street line fits a bubble, and the tone filters pass', () => {
    const avoid = [...t.tone.avoid, ...t.tone.profanity].map((a) => new RegExp(`\\b${a.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}\\b`, 'i'));
    expect([CONVERSATION_MAX_BEATS, CUTAWAY_MAX_WORDS, CUTAWAY_MAX_LINE, CUTAWAY_MAX_SENTENCES, CUTAWAY_MAX_QUESTIONS]).toEqual([16, 25, 160, 2, 1]);
    for (const sc of c.items) {
      expect(sc.beats.length).toBeLessThanOrEqual(CONVERSATION_MAX_BEATS);
      for (const b of sc.beats) {
        expect(lineWords(b.line), b.line).toBeLessThanOrEqual(CUTAWAY_MAX_WORDS);
        expect(b.line.length, b.line).toBeLessThanOrEqual(CUTAWAY_MAX_LINE);
        expect(b.line, b.line).toMatch(/[.?]$/);
        expect(sentenceCount(b.line), b.line).toBeLessThanOrEqual(CUTAWAY_MAX_SENTENCES);
        expect(questionCount(b.line), b.line).toBeLessThanOrEqual(CUTAWAY_MAX_QUESTIONS);
        for (const a of avoid) expect(a.test(b.line), b.line).toBe(false);
      }
      for (const st of sc.street ?? []) for (const b of st.beats) { expect(wordCount(b.line), b.line).toBeLessThanOrEqual(MAX_WORDS); expect(b.line.length, b.line).toBeLessThanOrEqual(MAX_LINE); }
    }
    for (const id of [...SAMPLE, ...HELP, ...DRIVER]) expect(byId(id).beats.length, id).toBeGreaterThanOrEqual(10); // full scenes, not the old short ones
  });
  test('nobody in a cutaway explains the game: what is learned and what is open is the cards\' job', () => {
    expect(c.items.flatMap((sc) => sc.beats.map((b) => b.line)).filter((l) => TUTORIAL.test(l))).toEqual([]);
    for (const l of ['We could clear her Bureau loan, or use Arthur on the door.', 'You can now call Inez.', 'We can pressure her.']) expect(TUTORIAL.test(l), l).toBe(true);
    for (const l of ["We can wait.", 'Pay what you can with it, and I\'ll come back with the rest.', 'Call Arthur across now.']) expect(TUTORIAL.test(l), l).toBe(false);
  });
  test('nobody pays at the Bureau or stamps anything: the cell gives the money to her (Sky, 2026-11-05)', () => {
    const MOUTHS = new Set(['fixer', 'scout', 'tech', 'enforcer', 'waitress', 'driver']);
    const items = t.scenes.items.flatMap((s) => s.variants.flatMap((v) => v.beats.flatMap((b) => ('line' in b && MOUTHS.has(b.speaker) ? [b.line] : []))));
    const said = t.dialogue.filter((e) => MOUTHS.has(e.speaker as string)).flatMap((e) => e.lines);
    const convo = c.items.flatMap((sc) => [...sc.beats, ...(sc.street ?? []).flatMap((x) => x.beats)].map((b) => `${sc.id}: ${b.line}`));
    expect([...items, ...said].filter((l) => PAYS_AT_BUREAU.test(l))).toEqual([]);
    expect(convo.filter((l) => PAYS_AT_BUREAU.test(l))).toEqual([]); // the Help scenes' three Bureau lines went with engine 0.44.0
  });
  test('names in speech: the people these scenes are about, and no one else from the city\'s pool', () => {
    const cast = json('contracts/people/harbour-district.cast.json') as { names: { given: { name: string }[]; family: { name: string }[] } };
    const pool = new Set([...cast.names.given, ...cast.names.family].map((n) => n.name));
    const said = new Set<string>();
    for (const sc of c.items) for (const s of [...sc.beats.map((b) => b.line), ...sc.learned, ...sc.opens]) for (const w of s.match(/[\p{L}'-]+/gu) ?? []) if (pool.has(w.replace(/'s$/, ''))) said.add(w.replace(/'s$/, ''));
    expect([...said].filter((w) => !['Inez', 'Arthur', 'Lukac', 'Milena', 'Nikolaou'].includes(w))).toEqual([]);
  });
  test('the schema holds its limits: a long card, a long line, a scene with no temperament; a street line is still a bubble', () => {
    const ok = byId('help-inez-1');
    expect(Conversation.safeParse(ok).success).toBe(true);
    const line = (l: string) => Conversation.safeParse({ ...ok, beats: [{ speaker: 'fixer', line: l }, ...ok.beats.slice(1)] }).success;
    expect(line(Array(25).fill('word').join(' ') + '.')).toBe(true);
    expect(line(Array(26).fill('word').join(' ') + '.')).toBe(false);
    expect(line('a'.repeat(CUTAWAY_MAX_LINE + 1))).toBe(false);
    expect(Conversation.safeParse({ ...ok, beats: Array(17).fill(ok.beats[0]) }).success).toBe(false);
    const run = byId('call-inez-pressure'), longStreet = { ...run.street![0]!, beats: [{ speaker: 'waitress', line: 'Someone knows about us, love, and they looked very suspicious to me just now, Arthur.' }] };
    expect(Conversation.safeParse({ ...run, street: [longStreet] }).success).toBe(false);
    expect(Conversation.safeParse({ ...ok, learned: ['one two three four five six seven'] }).success).toBe(false);
    const { temperament: _gone, ...noTemper } = ok.when;
    expect(Conversation.safeParse({ ...ok, when: noTemper }).success).toBe(false);
  });
});

describe('which scene plays', () => {
  test('the routes as the engine lands them', () => {
    expect(pick({ verb: 'listen', ...inez, turned: false })).toBe('listen-inez');
    expect(pick({ verb: 'help', ...inez, outcome: 'small-offer', helped: false })).toBe('help-inez-1'); // engine 0.44.0: before the Listen
    expect(pick({ verb: 'help', ...inez, outcome: 'turned', helped: false })).toBe('help-inez-2'); // after it, the full offer, one visit
    expect(pick({ verb: 'help', ...inez, outcome: 'turned', helped: true })).toBe('help-inez-2-known'); // after a refused small offer (turn.help.small-offer)
    expect(pick({ verb: 'pressure', ...inez, outcome: 'turned', helped: false })).toBe('pressure-inez');
    expect(pick({ verb: 'pay', about: 'driver', temperament: 'mercenary', outcome: 'turned' })).toBe('pay-driver');
    expect(pick({ verb: 'help', about: 'driver', temperament: 'mercenary', outcome: 'turned', helped: false })).toBe('help-driver'); // engine 0.45.0: one visit after his Listen
    expect(pick({ verb: 'help', about: 'driver', temperament: 'mercenary', outcome: 'turned', helped: true })).toBe('help-driver-known'); // after a refused small offer
    expect(pick({ verb: 'call', ...inez, turnedBy: 'help' })).toBe('call-inez');
    expect(pick({ verb: 'call', ...inez, turnedBy: 'pressure' })).toBe('call-inez-pressure');
  });
  test('what these words would make false keeps today\'s lines', () => {
    expect(pick({ verb: 'listen', ...inez, turned: true })).toBeUndefined(); // "could be cleared", and Pressure offered on a woman already turned
    expect(pick({ verb: 'pressure', ...inez, outcome: 'turned', helped: true })).toBeUndefined(); // "If you're from the Bureau" to the woman whose money she refused
    expect(pick({ verb: 'help', ...inez, outcome: 'small-offer', helped: true })).toBeUndefined(); // a second small offer: "I've never seen you before" would be false
    expect(pick({ verb: 'help', ...inez, outcome: 'not-yet', helped: false })).toBeUndefined(); // engine 0.44.0: a Fearful Inez is never "not yet"
    expect(pick({ verb: 'help', ...inez, outcome: 'kept', helped: true })).toBeUndefined();
    for (const turnedBy of ['pay', 'ask']) expect(pick({ verb: 'call', ...inez, turnedBy })).toBeUndefined();
    for (const verb of ['ask', 'pay', 'mislead']) expect(pick({ verb, ...inez, outcome: 'turned', helped: false })).toBeUndefined();
    for (const helped of [false, true]) expect(pick({ verb: 'help', about: 'driver', temperament: 'mercenary', outcome: 'small-offer', helped })).toBeUndefined(); // his small offer plays today's bubbles
    for (const temperament of TEMPERAMENTS.filter((x) => x !== 'mercenary')) expect(pick({ verb: 'help', about: 'driver', temperament, outcome: 'turned', helped: false })).toBeUndefined();
  });
  test('another temperament never gets these words, and always has its own lines', () => {
    for (const temperament of TEMPERAMENTS.filter((x) => x !== 'fearful')) for (const verb of ['listen', 'help', 'pressure', 'call']) for (const turnedBy of ['help', 'pressure']) for (const helped of [false, true]) for (const outcome of ['turned', 'not-yet'])
      expect(pick({ verb, about: 'waitress', temperament, outcome, helped, turnedBy, turned: false })).toBeUndefined();
    for (const temperament of TEMPERAMENTS) {
      expect(lookupScene(t, { kind: 'overheard', verb: 'listen', about: 'waitress', temperament })?.temperament).toBe(temperament);
      expect(lookupScene(t, { kind: 'call', verb: 'call', about: 'waitress', temperament })?.temperament).toBe(temperament);
    }
  });
  test('no query matches two scenes', () => {
    const bools = [undefined, false, true], outcomes = [undefined, 'turned', 'not-yet', 'kept', 'refused', 'suspicious'], by = [undefined, 'ask', 'pay', 'help', 'pressure'];
    for (const sc of c.items) for (const outcome of outcomes) for (const turned of bools) for (const helped of bools) for (const turnedBy of by) {
      const q: ConversationQuery = { ...sc.when, outcome, turned, helped, turnedBy };
      expect(c.items.filter((x) => lookupConversation({ ...c, items: [x] }, q)).length).toBeLessThanOrEqual(1);
    }
  });
  test('every act a conversation covers still has today\'s scene for bubbles', () => {
    expect(lookupScene(t, { kind: 'overheard', verb: 'listen', about: 'waitress', temperament: 'fearful' })?.about).toBe('waitress');
    for (const [verb, outcome] of [['help', 'small-offer'], ['help', 'turned'], ['pressure', 'turned']] as const) // engine 0.44.0 (brief 096): a Fearful Inez's Help is a small offer or the full one, never not yet
      expect(lookupScene(t, { kind: 'approach', verb, about: 'waitress', outcome, temperament: 'fearful' })?.outcome).toBe(outcome);
    for (const verb of ['pay', 'help']) expect(lookupScene(t, { kind: 'approach', verb, about: 'driver', outcome: 'turned', temperament: 'mercenary' })?.outcome).toBe('turned');
    expect(lookupScene(t, { kind: 'call', verb: 'call', about: 'waitress', temperament: 'fearful' })?.temperament).toBe('fearful');
  });
});

describe('the engine can\'t put these words over someone else', () => {
  const site = json('sim/data/handover.site.json') as { cast: Record<string, { temperament: string }> };
  test('each scene\'s temperament is the one the site casts', () => {
    for (const sc of c.items) expect(sc.when.temperament as string, sc.id).toBe(site.cast[sc.when.about]!.temperament);
  });
  const seed = join(root, 'sim/Suborned.Sim/GameSeed.cs');
  test.if(existsSync(seed))('a game\'s seed draws no temperament and no casting; only an explicit start can change one', () => {
    expect(readFileSync(seed, 'utf8')).not.toMatch(/Temperament|\bS\.Role\b/);
    expect(readFileSync(join(root, 'sim/Suborned.Sim/Game.cs'), 'utf8')).toContain('Temperament = Start.Temperament.TryGetValue(roles[i], out var tm) ? tm : c.Temperament');
    const copy = readFileSync(join(root, 'unity/Assets/Suborned/Runtime/Play/HostFightLab.cs'), 'utf8').match(/static void CopyLab[\s\S]*?\n {8}\}/)![0];
    expect(copy).not.toContain('Temperament'); // a new game's start, a fight preset included, carries none
  });
  test('Help on a fearful Inez is a small offer she refuses before the Listen, and one full offer that turns her after it, as the scenes say', () => {
    const rb = Rulebook.parse(json('contracts/rules/rulebook.json')), people = PeopleRules.parse(json('contracts/people/people.rules.json')), help = R.event(rb, 'turn.help');
    const her = (memory: R.Subject['memory'] = []): R.Subject => ({ id: 'p-cafe-14', tier: 'simulated', temperament: 'fearful', need: 'debt', scales: { ...people.temperaments.fearful.start }, memory, turned: false });
    const card = (knows: R.Know[], s = her()) => R.quote(rb, people, help, { subject: s, band: 'hero', knows: new Set(knows) });
    expect([card(['routine', 'need-told']).outcome, card(['routine', 'need-told']).perOrder.usd]).toEqual(['small-offer', 0]); // help-inez-1: no money changes hands
    expect([card(['routine', 'need', 'temperament']).outcome, card(['routine', 'need', 'temperament']).orders]).toEqual(['turned', 1]); // help-inez-2: one visit
    const refused = her([{ act: 'refusal', by: 'fixer', place: 'cafe-entrance', time: 100, witnessed: false }]); // help-inez-2-known: dearer, still one visit
    expect([card(['routine', 'need', 'temperament'], refused).outcome, card(['routine', 'need', 'temperament'], refused).orders]).toEqual(['turned', 1]);
  });
});
