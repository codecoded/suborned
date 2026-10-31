// Done-check for scenes.conversations (Fable, brief 085): the words are the ones Sky agreed, each plays only for the person and situation it was written for,
// every other act keeps its scenes.items lines, and the engine can't put these words over a different person. Run: bun test contracts/missions/handover
import { describe, expect, test } from 'bun:test';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { check, specs } from '../../../tools/dialogue/conversations.ts';
import { TEMPERAMENTS } from '../../people/people.rules.schema.ts';
import { CONVERSATION_MAX_BEATS, Conversation, conversationLines, lookupConversation, type ConversationQuery } from './conversations.schema.ts';
import { MAX_LINE, MAX_WORDS, TextFile, lookupScene, wordCount } from './text.schema.ts';

const root = join(import.meta.dir, '..', '..', '..');
const json = (p: string) => JSON.parse(readFileSync(join(root, p), 'utf8'));
const t = TextFile.parse(json('contracts/missions/handover/text.json'));
const c = t.scenes.conversations!;
const approved = json('briefs/085/fable/approved.json') as { scenes: { id: string; beats: [string, string][]; learned: string[]; opens: string[] }[] };
const byId = (id: string) => c.items.find((x) => x.id === id)!;
const pick = (q: ConversationQuery) => lookupConversation(c, q)?.id;
const inez = { about: 'waitress', temperament: 'fearful' };
/** The one edit to Sky's agreed cards (Aster tells him): the entrance camera still stops an Enter (GameOrders.cs entry-seen-camera), so a bare "Enter the Office" misleads. */
const CARD_EDITS: Record<string, string[]> = { 'call-inez': ['Camera off? Then enter the Office.'] };
const shown = (id: string) => conversationLines(c, byId(id)).map((l) => [l.name, l.text]);

describe('the words', () => {
  test('every scene Sky agreed is word for word what he agreed, in order, cards included', () => {
    for (const a of approved.scenes) {
      expect(byId(a.id).status, a.id).toBe('agreed');
      expect(shown(a.id), a.id).toEqual(a.beats);
      expect(byId(a.id).learned, a.id).toEqual(a.learned);
      expect(byId(a.id).opens, a.id).toEqual(CARD_EDITS[a.id] ?? a.opens);
    }
    expect(approved.scenes.length).toBe(6);
  });
  test('the one scene he has not read is flagged but gated by nothing, ends with the guard coming, and leaves the telling to the street', () => {
    expect(c.items.filter((x) => x.status !== 'agreed').map((x) => x.id)).toEqual(['call-inez-pressure']);
    const run = byId('call-inez-pressure');
    expect(run.status).toBe('awaiting-sky');
    expect(run.beats.at(-1)).toEqual({ speaker: 'guard', line: "It's only a door. All right, I'm coming." });
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
  test('each scene passes the generator\'s own checks: the limits, nothing in hand, a few minutes, no lender', () => {
    for (const s of specs) {
      const sc = byId(s.id), beats = conversationLines(c, sc).map((l) => ({ speaker: l.name, line: l.text }));
      expect(check(s, { beats, learned: sc.learned, opens: sc.opens }), s.id).toEqual([]);
    }
  });
  test('every line fits a bubble and takes a line from the tone filters', () => {
    const avoid = [...t.tone.avoid, ...t.tone.profanity].map((a) => new RegExp(`\\b${a.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}\\b`, 'i'));
    for (const sc of c.items) {
      expect(sc.beats.length).toBeLessThanOrEqual(CONVERSATION_MAX_BEATS);
      for (const b of sc.beats) {
        expect(wordCount(b.line), b.line).toBeLessThanOrEqual(MAX_WORDS);
        expect(b.line.length, b.line).toBeLessThanOrEqual(MAX_LINE);
        for (const a of avoid) expect(a.test(b.line), b.line).toBe(false);
      }
    }
  });
  test('names in speech: the people these scenes are about, and no one else from the city\'s pool', () => {
    const cast = json('contracts/people/harbour-district.cast.json') as { names: { given: { name: string }[]; family: { name: string }[] } };
    const pool = new Set([...cast.names.given, ...cast.names.family].map((n) => n.name));
    const said = new Set<string>();
    for (const sc of c.items) for (const s of [...sc.beats.map((b) => b.line), ...sc.learned, ...sc.opens]) for (const w of s.match(/[\p{L}'-]+/gu) ?? []) if (pool.has(w.replace(/'s$/, ''))) said.add(w.replace(/'s$/, ''));
    expect([...said].filter((w) => !['Inez', 'Okafor', 'Zhou', 'Milena', 'Nikolaou'].includes(w))).toEqual([]);
  });
  test('the schema holds its limits: a long card, a scene with no temperament', () => {
    const ok = byId('help-inez-1');
    expect(Conversation.safeParse(ok).success).toBe(true);
    expect(Conversation.safeParse({ ...ok, learned: ['one two three four five six seven'] }).success).toBe(false);
    const { temperament: _gone, ...noTemper } = ok.when;
    expect(Conversation.safeParse({ ...ok, when: noTemper }).success).toBe(false);
  });
});

describe('which scene plays', () => {
  test('the routes as the engine lands them', () => {
    expect(pick({ verb: 'listen', ...inez, turned: false })).toBe('listen-inez');
    expect(pick({ verb: 'help', ...inez, outcome: 'not-yet', helped: false })).toBe('help-inez-1');
    expect(pick({ verb: 'help', ...inez, outcome: 'turned', helped: true })).toBe('help-inez-2');
    expect(pick({ verb: 'pressure', ...inez, outcome: 'turned', helped: false })).toBe('pressure-inez');
    expect(pick({ verb: 'pay', about: 'driver', temperament: 'mercenary', outcome: 'turned' })).toBe('pay-driver');
    expect(pick({ verb: 'call', ...inez, turnedBy: 'help' })).toBe('call-inez');
    expect(pick({ verb: 'call', ...inez, turnedBy: 'pressure' })).toBe('call-inez-pressure');
  });
  test('what these words would make false keeps today\'s lines', () => {
    expect(pick({ verb: 'listen', ...inez, turned: true })).toBeUndefined(); // "could be cleared", and Pressure offered on a woman already turned
    expect(pick({ verb: 'pressure', ...inez, outcome: 'turned', helped: true })).toBeUndefined(); // "Is it the Bureau?" to the man who just paid part of it
    expect(pick({ verb: 'help', ...inez, outcome: 'turned', helped: false })).toBeUndefined(); // "That's the rest paid" with nothing paid before
    expect(pick({ verb: 'help', ...inez, outcome: 'kept', helped: true })).toBeUndefined();
    for (const turnedBy of ['pay', 'ask']) expect(pick({ verb: 'call', ...inez, turnedBy })).toBeUndefined();
    for (const verb of ['ask', 'pay', 'mislead']) expect(pick({ verb, ...inez, outcome: 'turned', helped: false })).toBeUndefined();
    expect(pick({ verb: 'help', about: 'driver', temperament: 'mercenary', outcome: 'turned', helped: true })).toBeUndefined();
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
    for (const [verb, outcome] of [['help', 'not-yet'], ['help', 'turned'], ['pressure', 'turned']] as const) expect(lookupScene(t, { kind: 'approach', verb, about: 'waitress', outcome, temperament: 'fearful' })?.outcome).toBe(outcome);
    expect(lookupScene(t, { kind: 'approach', verb: 'pay', about: 'driver', outcome: 'turned', temperament: 'mercenary' })?.outcome).toBe('turned');
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
  test('Help takes a fearful Inez two visits, as the two scenes say', () => {
    const rb = json('contracts/rules/rulebook.json'), rules = json('contracts/people/people.rules.json');
    expect(rb.values['help-orders'].v + rb.values['worse-help-orders'].v).toBe(2);
    expect(rules.temperaments.fearful.respondsTo).not.toContain('help'); // so her fit is worse: one more Help
    expect(rules.temperaments.fearful.start.fear).toBeGreaterThanOrEqual(rb.values['mood-uneasy-fear'].v); // uneasy from the start, never friendly enough for one
  });
});
