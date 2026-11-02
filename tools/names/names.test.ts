// The name check (brief 089, Splice): its failing examples, the matching rules, and the repository passing it (today: only the
// expected hits, which fail once they are gone or after `until`). Run: bun test tools/names
import { describe, expect, test } from 'bun:test';
import { type Config, loadBan, makeBan, match } from './ban.ts';
import { type Hit, check, failures, hitsIn, judge } from './check.ts';
import { SIM_NOT_SHOWN, csStrings, generatorBan, nameStrings, poolStrings, textStrings } from './scan.ts';

const BAN = { strong: { Vale: 'D', Okafor: 'B,D', Elena: 'B' }, reported: ['Reed', 'Moss'], lookalike: ['^el[aeiy]', '^v[aeiy][a-z]{1,3}$'] };
const cfg: Pick<Config, 'allow' | 'alsoWords'> = { allow: [], alsoWords: ['Reed', 'Moss'] };
const ban = makeBan(BAN, cfg);
const words = (text: string, person = false, capitals = false) => match(ban, text, person, capitals).map(m => m.name);

describe('names: failing examples', () => {
  test('a banned name in a line fails, by the whole word, in any case and with accents', () => {
    expect(words('Take Vale alive.')).toEqual(['Vale']);
    expect(words('Okafor is off the door')).toEqual(['Okafor']);
    expect(words('VALE')).toEqual(['Vale']);
    expect(words('Válé reports')).toEqual(['Vale']);
    expect(words('→ vale')).toEqual(['Vale']); // a lower-case plate in the words is still on screen
  });
  test('a possessive and a joined name count as the name', () => {
    expect(words("Vale's car")).toEqual(['Vale']);
    expect(words('Vale’s driver')).toEqual(['Vale']);
    expect(words('Okafor-Reed')).toEqual(['Okafor', 'Reed']);
  });
  test('a lookalike in a person\'s name is a warning for review; a listed name fails (Aster 2026-11-02: Sky wants normal names)', () => {
    const m = match(ban, 'Vidal Okafor', true);
    expect(m.map(x => `${x.name}${x.warn ? ' warns' : ' fails'}`)).toEqual(['Vidal warns', 'Okafor fails']);
    const hits = hitsIn(ban, [{ source: 'people', path: 'p', text: 'Vidal Okafor', person: true }]);
    expect(hits.filter(h => !h.warn).map(h => h.name)).toEqual(['Okafor']); // only these reach judge(): Vidal never fails
    expect(words('Eleni Vidal')).toEqual([]); // prose: the patterns never apply ("very", "else")
  });
});

describe('names: what passes', () => {
  test('a word that contains a name is not the name', () => {
    expect(words('the valet parks it; Valencia; avalent; Okaforo')).toEqual([]);
  });
  test('a name that is also an English word counts only with a capital', () => {
    expect(words('a reed bed, moss on the wall')).toEqual([]);
    expect(words('Reed is at the kerb')).toEqual(['Reed']);
  });
  test('in C# only a capitalised word counts: a lower-case "vale" is a code or a key', () => {
    expect(words('vale car times', false, true)).toEqual([]);
    expect(words('Vale car times', false, true)).toEqual(['Vale']);
  });
  test('an allowed name (Sky pinned it) passes, the lookalikes too', () => {
    const b = makeBan(BAN, { allow: [{ name: 'Elena', why: 'pinned', by: 'Sky' }], alsoWords: [] });
    expect(match(b, 'Elena Vale', true).map(m => m.name)).toEqual(['Vale']);
    expect(match(b, 'Eleni', true).map(m => m.name)).toEqual(['Eleni']);
  });
});

describe('names: what is read', () => {
  test('text.json: notes, rules, sources, triggers and engine codes are not read; the words are', () => {
    const s = textStrings('text', { rule: 'Vale', note: 'Vale', from: 'Vale', events: { x: { trigger: 'Vale' } }, cards: { items: { a: { at: 'vale-car', title: 'Vale leaves', pick: 'Vale' } } },
      less: { objective: { steps: [{ when: 'Vale' }] } }, briefing: { page: { ways: { pickup: { when: 'From 5:10' } } } }, dialogue: [{ pending: 'Vale', lines: ['Vale, alive.'] }], names: { targets: { councillor: 'Vale' } } });
    expect(s.map(x => `${x.path}${x.person ? '*' : ''}`)).toEqual(['cards.items.a.title', 'briefing.page.ways.pickup.when', 'dialogue.0.lines.0', 'names.targets.councillor*']);
  });
  test('people files: every `name`, as a person', () => {
    expect(nameStrings('people', { people: [{ id: 'p-1', name: 'Efua Vale', need: 'debt' }], cast: { guard: { name: 'Okafor' } } }).map(x => `${x.path}=${x.text}`)).toEqual(['people.0.name=Efua Vale', 'cast.guard.name=Okafor']);
  });
  test('the name pool: `given` and `family`, of { name } or of strings, each as a person', () => {
    const s = poolStrings('pool', 'x.cast.json', { names: { given: [{ name: 'Mateo', origin: 'iberian' }, 'Inez'], family: [{ name: 'Okafor' }] }, rows: [{ why: 'Okafor' }] });
    expect(s.map(x => `${x.path}=${x.text}${x.person ? '*' : ''}`)).toEqual(['x.cast.json:names.given.0=Mateo*', 'x.cast.json:names.given.1=Inez*', 'x.cast.json:names.family.0=Okafor*']);
  });
  test('C#: string literals, not comments; the literals inside an interpolation hole read on their own; verbatim and char literals handled', () => {
    const cs = `// "Vale" in a comment\nvar a = "Take Vale"; /* "Okafor" */ char q = '"'; var b = $"{who} after {Name("Vale")}'s car"; var c = @"say ""Vale""";\nvar d = "a\\"b";`;
    expect(csStrings('play', 'X.cs', cs).map(x => `${x.path} ${x.text.trim()}`)).toEqual(['X.cs:2 Take Vale', 'X.cs:2 Vale', 'X.cs:2 after  \'s car', 'X.cs:2 say "Vale"', 'X.cs:3 a b']);
  });
  test('the engine (sim): its sentences are read, its scenarios are not (Keel, brief 089)', () => {
    expect(SIM_NOT_SHOWN.test('ScenariosHandover.cs') && SIM_NOT_SHOWN.test('Scenarios.cs')).toBe(true);
    expect(SIM_NOT_SHOWN.test('GameSeed.cs') || SIM_NOT_SHOWN.test('Debrief.cs')).toBe(false);
    expect(csStrings('sim', 'G.cs', 'Note("x", $"{Cap(ValeName)} is dead."); Fail("vale-dead", "Vale is dead.");').map(x => words(x.text, false, true)).flat()).toEqual(['Vale']);
  });
});

describe('names: the expected hits cannot silently stay', () => {
  const config: Config = { allow: [], alsoWords: [], ignore: [], expected: { brief: '089', until: '2026-11-16', why: 'x', hits: [{ source: 'text', name: 'Vale' }, { source: 'text', name: 'Okafor' }, { source: 'streaming-text', name: 'Vale' }] } };
  const hit = (source: string, name: string) => ({ source, name, path: 'p', text: name, word: name, why: 'strong', person: false }) as Hit;
  test('an expected hit passes; a new one fails', () => {
    const r = judge([hit('text', 'Vale'), hit('text', 'Okafor'), hit('text', 'Elena')], config, ['text'], '2026-11-02');
    expect(r.fresh.map(h => h.name)).toEqual(['Elena']); expect(r.stale).toEqual([]);
  });
  test('an expected hit that is gone fails (take it off); one in a source not on this machine does not', () => {
    const r = judge([hit('text', 'Vale')], config, ['text'], '2026-11-02');
    expect(r.stale).toEqual([{ source: 'text', name: 'Okafor' }]);
  });
  test('after `until` every hit fails as new', () => {
    const r = judge([hit('text', 'Vale')], config, ['text'], '2026-11-17');
    expect(r.expired).toBe(true); expect(r.fresh.length).toBe(1);
  });
});

describe('names: the repository', () => {
  test('the ban list loads (contracts/people/ when Pulse moves it, else the brief) with Vale and Okafor on it', () => {
    const b = loadBan();
    expect(b.names.has('vale') && b.names.has('okafor')).toBe(true);
    expect(b.from).toMatch(/^(contracts\/people\/.*ban.*\.json|briefs\/089\/research\/llm-name-ban-list\.json)$/);
  });
  test('the people generator drops names by the same ban list the check reads', () => {
    const r = check();
    expect(generatorBan()).toBe(r.from);
    expect(failures({ ...r, generator: 'contracts/people/old-ban.json' })[0]).toMatch(/^the people generator reads its ban list from contracts\/people\/old-ban\.json/);
  });
  test('no banned name in what the player sees, past the expected hits; no expected or ignore entry is stale', () => {
    const t0 = performance.now(), r = check();
    const lines = [...r.fresh.map(h => `new: ${h.source} ${h.path} "${h.word}" (${h.name}, ${h.why})`), ...r.stale.map(e => `gone, take it off expected.hits: ${e.source} ${e.name}`), ...r.unused.map(i => `ignore covers nothing: ${i.source} ${i.at}`)];
    expect(lines).toEqual([]);
    for (const s of ['text', 'people', 'pool', 'play', 'sim']) expect(r.sources).toContain(s);
    expect(r.generator).toBeNull();
    expect(performance.now() - t0).toBeLessThan(10_000);
  });
});
