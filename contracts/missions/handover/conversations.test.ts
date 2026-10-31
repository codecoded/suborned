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
const shown = (id: string) => conversationLines(c, byId(id)).map((l) => [l.name, l.text]);

describe('the words', () => {
  test('every scene Sky agreed is word for word what he agreed, in order, cards included', () => {
    for (const a of approved.scenes) {
      expect(byId(a.id).status, a.id).toBe('agreed');
      expect(shown(a.id), a.id).toEqual(a.beats);
      expect(byId(a.id).learned, a.id).toEqual(a.learned);
      expect(byId(a.id).opens, a.id).toEqual(a.opens);
    }
    expect(approved.scenes.length).toBe(6);
  });
  test('the one scene he has not read is flagged, and is the generator\'s output unedited', () => {
    expect(c.items.filter((x) => x.status !== 'agreed').map((x) => x.id)).toEqual(['call-inez-pressure']);
    const run = byId('call-inez-pressure'), f = json(`${run.from}/call-inez-pressure/final.json`);
    expect(run.status).toBe('awaiting-sky');
    expect(shown(run.id)).toEqual(f.beats.map((b: { speaker: string; line: string }) => [b.speaker, b.line]));
    expect(run.learned).toEqual(f.learned); expect(run.opens).toEqual(f.opens);
    expect(f.faults).toEqual([]);
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
