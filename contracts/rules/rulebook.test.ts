// Done-check for the rulebook (Keel, brief 010): rulebook.json parses, reads people.rules.json without restating it, rejects the
// unknown, and run through the reference evaluator reproduces the GDD's numbers (§04 [B] Inez prices and purse, Sky Q2–Q6, K1–K9).
// These cases are the golden behaviour the C# engine (brief 011 A) ports. Run: bun test contracts/rules
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { PeopleRules } from '../people/people.rules.schema.ts';
import { rng } from '../../tools/world/gen/common.ts';
import { Rulebook, checkRulebook, collectRefs, num } from './rulebook.schema.ts';
import * as R from './rulebook.ref.ts';

const root = join(import.meta.dir, '..', '..');
const read = (p: string) => JSON.parse(readFileSync(join(root, p), 'utf8'));
const raw = read('contracts/rules/rulebook.json');
const rb = Rulebook.parse(raw);
const people = PeopleRules.parse(read('contracts/people/people.rules.json'));
const mission = read('contracts/missions/handover/mission.json') as { roles: { id: string; need?: string; approach?: string }[] };

const ALL = new Set<R.Know>(['routine', 'need', 'temperament']);
const person = (temperament: string, need = 'debt', extra: Partial<R.Subject> = {}): R.Subject => ({
  id: 'p-cafe-11', tier: 'simulated', temperament, need,
  scales: { ...people.temperaments[temperament as keyof PeopleRules['temperaments']].start }, memory: [], turned: false, ...extra,
});
const turn = (a: string) => R.event(rb, `turn.${a}`);
const card = (a: string, s: R.Subject, band: R.Band = 'neutral', knows: ReadonlySet<R.Know> = ALL) => R.quote(rb, people, turn(a), { subject: s, band, knows });
/** Give the order and apply its person-side changes, as the engine would. */
const act = (a: string, s: R.Subject, band: R.Band = 'neutral', time = 100) => {
  const r = R.resolve(rb, people, turn(a), { subject: s, band, knows: ALL });
  return { ...r, after: R.apply(rb, people, s, r.changes, { by: 'fixer', place: 'cafe-entrance', time, witnessed: false }) };
};
const PURSE = 100000; // §07 Purse $100,000

describe('rulebook.json: shape and the people rules it reads', () => {
  test('parses, and every id it names is in people.rules.json (unknown = reject)', () => {
    expect(rb.schema).toBe('suborned.rulebook/1');
    expect(checkRulebook(rb, people)).toEqual([]);
  });

  test('every value is tagged B/S/L with a source, integer, and read by a rule; every proposed rule has a reason', () => {
    for (const [k, v] of Object.entries(rb.values)) { expect(['B', 'S', 'L'], k).toContain(v.tag); expect(Number.isInteger(v.v), k).toBe(true); }
    const walk = (v: unknown, path: string): void => {
      if (!v || typeof v !== 'object') return;
      const o = v as Record<string, unknown>;
      if (o.proposed === true) expect(typeof o.reason, path).toBe('string');
      for (const [k, c] of Object.entries(o)) walk(c, `${path}.${k}`);
    };
    walk(rb, 'rb');
    const unread = structuredClone(raw); unread.values['spare'] = { v: 1, unit: 'count', tag: 'S', src: 'none' };
    expect(Rulebook.safeParse(unread).success).toBe(false); // a value no rule reads is trivia (Legion lesson)
    const dangling = structuredClone(raw); dangling.events[0].price[0].s = 'v:no-such-value';
    expect(Rulebook.safeParse(dangling).success).toBe(false);
    const zero = structuredClone(raw); zero.values['loop-s'].v = 0;
    expect(Rulebook.safeParse(zero).success).toBe(false); // a 0 s action never lands (Redline E3)
  });

  test('the base price of every Turn event is people.rules approaches, read by ref, never copied (§04 [B])', () => {
    for (const a of ['ask', 'pay', 'help', 'pressure'] as const) {
      const base = turn(a).price![0]!;
      expect([base.usd, base.s, base.trail]).toEqual([`p:approaches.${a}.usd`, `p:approaches.${a}.timeS`, `p:approaches.${a}.trail`]);
    }
    expect(['ask', 'pay', 'help', 'pressure'].map((a) => [0, 1, 2].map((i) => num(rb, people, [`p:approaches.${a}.usd`, `p:approaches.${a}.timeS`, `p:approaches.${a}.trail`][i]!))))
      .toEqual([[0, 25, 1], [70000, 50, 2], [40000, 50, 1], [0, 35, 3]]);
    expect(collectRefs(rb).filter((r) => r.startsWith('p:')).length).toBeGreaterThan(10);
  });

  test('unknown = reject: an unknown condition kind, temperament, fact, or a restated people rule', () => {
    const k = structuredClone(raw); k.events[0].needs.push({ k: 'mood', is: true });
    expect(Rulebook.safeParse(k).success).toBe(false);
    const t = structuredClone(raw); t.fit.standing[0].temperament = 'stoic';
    expect(checkRulebook(Rulebook.parse(t), people).join()).toContain('temperament stoic');
    const f = structuredClone(raw); f.memory.acts['rode-car'].facts = ['cell-alias'];
    expect(checkRulebook(Rulebook.parse(f), people).join()).toContain('fact cell-alias');
    const redefine = structuredClone(raw); redefine.fit.approaches.ask.respondsTo = ['help'];
    expect(Rulebook.safeParse(redefine).success).toBe(false); // strict shape
    const section = structuredClone(raw); delete section.ladders.rule; section.ladders.respondsTo = 'x';
    expect(Rulebook.safeParse(section).success).toBe(false);
  });

  test('every event resolves: outcomes end with an unconditional one; phases list every between-missions event once', () => {
    for (const e of rb.events) expect(e.outcomes.at(-1)!.when, e.id).toEqual([]);
    const noFallback = structuredClone(raw); noFallback.events[2].outcomes.pop();
    expect(Rulebook.safeParse(noFallback).success).toBe(false);
    const between = rb.phases['between-missions'];
    expect(between.indexOf('debtor.lapses')).toBeLessThan(between.indexOf('need.replaced')); // reads the debt met, not its replacement
  });
});

describe('§04 approaches and the Handover purse (Inez, the driver)', () => {
  const role = (id: string) => mission.roles.find((r) => r.id === id)!;

  test('the cast Inez (Idealist, debt) and driver (Idealist, cash) each turn on one Help: $80,000 fits the purse', () => {
    expect([role('waitress').need, role('waitress').approach, role('driver').need, role('driver').approach]).toEqual(['debt', 'help', 'cash', 'help']);
    const inez = card('help', person('idealist', 'debt'));
    const driver = card('help', person('idealist', 'cash'));
    expect([inez.perOrder, inez.orders, inez.outcome]).toEqual([{ usd: 40000, s: 50, trail: 1 }, 1, 'turned']);
    expect([driver.perOrder.usd, driver.orders]).toEqual([40000, 1]);
    expect(inez.perOrder.usd + driver.perOrder.usd).toBeLessThanOrEqual(PURSE);
  });

  test('purse: Pay where it suits ($70,000, a Mercenary) plus the driver is over the purse; learning buys the second route', () => {
    const pay = card('pay', person('mercenary'));
    expect([pay.perOrder, pay.outcome]).toEqual([{ usd: 70000, s: 50, trail: 2 }, 'turned']);
    expect(pay.perOrder.usd + card('help', person('idealist', 'cash')).perOrder.usd).toBeGreaterThan(PURSE);
  });

  test('Ask cold is refused and remembered: later offers +$10,000 and +1 T, Pressure +1 T only; no second Ask [B]', () => {
    const asked = act('ask', person('idealist'));
    expect(asked.outcome).toBe('refused');
    expect(asked.changes.find((c) => c.do === 'charge')).toEqual({ do: 'charge', parts: ['s', 'trail'] });
    const inez = asked.after;
    expect(card('help', inez).perOrder).toEqual({ usd: 50000, s: 50, trail: 2 });
    expect(card('pay', { ...inez, temperament: 'mercenary' }).perOrder).toEqual({ usd: 80000, s: 50, trail: 3 });
    expect(card('pressure', { ...inez, temperament: 'fearful' }).perOrder).toEqual({ usd: 0, s: 35, trail: 4 });
    expect(R.needsHold(rb, people, turn('ask'), { subject: inez, band: 'neutral', knows: ALL, given: { reach: true } })).toBe(false);
  });
});

describe('Sky Q4: the wrong approach works one step worse; a refused one fails', () => {
  test('neutral district: fit from people.rules respondsTo / refuses, Ask always refused', () => {
    const grid = (t: string) => ['ask', 'pay', 'help', 'pressure'].map((a) => R.fitOf(rb, people, a, t, 'neutral'));
    expect(grid('idealist')).toEqual(['refused', 'refused', 'suits', 'worse']);
    expect(grid('mercenary')).toEqual(['refused', 'suits', 'worse', 'worse']);
    expect(grid('fearful')).toEqual(['refused', 'worse', 'worse', 'suits']);
    expect(grid('loyal')).toEqual(['refused', 'worse', 'suits', 'worse']);
  });

  test('the steps: Pay +$20,000, Help one more order, Pressure +1 T; Pay to an Idealist fails with a remembered refusal', () => {
    expect(card('pay', person('fearful')).perOrder.usd).toBe(90000);
    expect(card('help', person('mercenary')).orders).toBe(2);
    expect(card('pressure', person('idealist')).perOrder.trail).toBe(4);
    const r = act('pay', person('idealist'));
    expect(r.outcome).toBe('refused');
    expect(r.after.memory.map((m) => m.act)).toEqual(['refusal']);
    expect(r.after.turned).toBe(false);
  });

  test('a worse Help turns on the second order, and each Help is paid and remembered', () => {
    const one = act('help', person('mercenary'), 'neutral', 100);
    expect(one.outcome).toBe('not-yet');
    const two = act('help', one.after, 'neutral', 200);
    expect(two.outcome).toBe('turned');
    expect(two.after.turned).toBe(true);
    expect(two.after.scales.loyalty).toBe(2); // gratitude twice: trusting
  });

  test('§04 temperament × standing, as the GDD words it', () => {
    expect(card('help', person('fearful'), 'hero').orders).toBe(2); // Fearful, Hero: needs Help ×2
    expect(card('help', person('loyal'), 'hero').orders).toBe(2); // Loyal, Hero: turns after repeated Help
    expect(card('pay', person('mercenary'), 'hero').perOrder.usd).toBe(90000); // Mercenary, Hero: overcharges
    expect(card('pay', person('mercenary'), 'menace').perOrder.usd).toBe(50000); // Mercenary, Menace: discount
    expect([card('ask', person('idealist'), 'hero').outcome, card('ask', person('idealist'), 'hero').perOrder.usd]).toEqual(['turned', 0]); // volunteers free
    expect(card('ask', person('fearful'), 'menace').outcome).toBe('turned'); // folds free
    for (const a of ['ask', 'pay', 'help', 'pressure']) {
      expect(card(a, person('idealist'), 'menace').outcome).toBe('refused'); // Idealist, Menace: refuses
      expect(card(a, person('loyal'), 'menace').outcome).toBe('refused'); // Loyal, Menace: reports any approach
    }
    expect(act('ask', person('idealist'), 'hero').after.scales.loyalty).toBe(1); // a volunteer starts obliged
    const folded = act('ask', person('fearful'), 'menace').after.scales;
    expect([folded.fear, folded.resentment]).toEqual([2, 1]); // folding is pressure by reputation
  });

  test('reports: a Loyal person reports what they do not respond to; an Idealist in a Menace district reports while they owe the cell nothing', () => {
    const reported = (a: string, s: R.Subject, band: R.Band) => act(a, s, band).changes.some((c) => c.do === 'report');
    expect(reported('pay', person('loyal'), 'neutral')).toBe(true);
    expect(reported('ask', person('loyal'), 'neutral')).toBe(true);
    expect(reported('help', person('loyal'), 'neutral')).toBe(false);
    expect(reported('pay', person('loyal'), 'menace')).toBe(true);
    expect(reported('help', person('loyal'), 'menace')).toBe(true);
    expect(reported('ask', person('idealist'), 'menace')).toBe(true); // bravery 1 > fear 0
    expect(reported('ask', person('idealist', 'debt', { scales: { bravery: 1, resentment: 0, fear: 1, loyalty: 0 } }), 'menace')).toBe(true); // owes nothing: reports
    expect(reported('help', person('idealist', 'debt', { turned: true, scales: { bravery: 3, resentment: 0, fear: 0, loyalty: 1 } }), 'menace')).toBe(false); // a Fighter who owes the cell keeps quiet (Motive #6)
    expect(reported('pay', person('mercenary'), 'neutral')).toBe(false);
  });
});

describe('Sky Q3 and K8: the card shows the price always, "?" for what reads the temperament', () => {
  test('a keeper tip gives the need, not the temperament: Help shows its base, "?" lines and outcome', () => {
    const q = card('help', person('mercenary'), 'neutral', new Set<R.Know>(['need']));
    expect(q.outcome).toBe('?');
    expect(q.lines.map((l) => [l.line, l.holds])).toEqual([['base', true], ['refused-before', false], ['one-more-help', '?'], ['trust-over-time', '?']]);
    expect(q.worst.usd).toBe(40000);
  });

  test('after Watch alone, Pay on Inez shows "?" and fails when tried (Sky 2026-10-15)', () => {
    const q = card('pay', person('idealist'), 'neutral', new Set<R.Know>(['routine']));
    expect([q.outcome, q.worst.usd]).toEqual(['?', 90000]);
    expect(act('pay', person('idealist')).outcome).toBe('refused');
  });

  test('"?" only where the temperaments in the band answer differently: Ask in a neutral district reads refused', () => {
    expect(card('ask', person('mercenary'), 'neutral', new Set<R.Know>()).outcome).toBe('refused');
    expect(card('ask', person('mercenary'), 'hero', new Set<R.Know>()).outcome).toBe('?'); // an Idealist volunteers there
    expect(card('help', person('idealist'), 'neutral', new Set<R.Know>(['need'])).lines[0]!.holds).toBe(true);
  });

  test('Pay blind asks cash for the worst case; only the lines that hold are charged', () => {
    const blind = card('pay', person('mercenary'), 'neutral', new Set<R.Know>(['routine']));
    expect([blind.worst.usd, blind.perOrder.usd, blind.outcome]).toEqual([90000, 70000, '?']);
    const pay = turn('pay');
    const ctx = (cash: number): R.Ctx => ({ subject: person('mercenary'), band: 'neutral', knows: new Set<R.Know>(['routine']), cash, given: { reach: true } });
    expect(R.needsHold(rb, people, pay, ctx(80000))).toBe(false);
    expect(R.needsHold(rb, people, pay, ctx(90000))).toBe(true);
  });

  test('no approach tells the cell the temperament; a refusal tells it only that this approach is refused (Sky, Aster 2026-10-15)', () => {
    for (const a of ['ask', 'pay', 'help', 'pressure']) {
      const learned = act(a, person('idealist')).changes.filter((c) => c.do === 'learn').flatMap((c) => (c.do === 'learn' ? c.what : []));
      expect(learned, a).not.toContain('temperament');
    }
    expect(act('pay', person('idealist')).changes).toContainEqual({ do: 'learn', what: ['refused'], of: 'subject' });
    const after = R.quote(rb, people, turn('pay'), { subject: person('idealist'), band: 'neutral', knows: new Set<R.Know>(['routine']), refusedKnown: new Set(['pay']) });
    expect(after.outcome).toBe('refused'); // Pay shows ✗ on Inez
    expect(R.quote(rb, people, turn('help'), { subject: person('idealist'), band: 'neutral', knows: new Set<R.Know>(['routine', 'need']), refusedKnown: new Set(['pay']) }).outcome).toBe('?'); // her temperament is still unknown
    expect(rb.preview.knownBy).toEqual(['scout.listen']); // Sky 2026-10-15: Listen reveals the temperament, Watch only the routine
    expect(R.event(rb, 'cell.watch').outcomes[0]!.changes).toEqual([{ do: 'learn', what: ['routine'], of: 'subject' }]);
  });
});

describe('the Talker test, interrogation and K6 (§04, §05, Sky Q5)', () => {
  test('helped Inez is not a Talker and lies: a false lead in a Hero district, nothing otherwise', () => {
    const inez = act('help', person('idealist')).after;
    expect(inez.scales).toEqual({ bravery: 1, resentment: 0, fear: 0, loyalty: 1 });
    expect(R.answer(rb, people, inez, 'hero')).toEqual({ outcome: 'lies', gives: ['false-lead'] });
    expect(R.answer(rb, people, inez, 'neutral')).toEqual({ outcome: 'lies', gives: ['nothing'] });
  });

  test('pressured Inez is a Talker and gives up the Fixer\'s face and where and when (§08: Inez may identify Fixer)', () => {
    const inez = act('pressure', person('idealist')).after;
    expect(inez.scales).toEqual({ bravery: 1, resentment: 1, fear: 1, loyalty: 0 });
    expect(R.answer(rb, people, inez, 'neutral')).toEqual({ outcome: 'gives', gives: ['fixer-face', 'place-time'] });
  });

  test('K6: a person holding no fact gives nothing and the hop is spent; questioning needs an open Trail and a simulated person', () => {
    const q = R.event(rb, 'investigator.question');
    expect(R.answer(rb, people, person('fearful'), 'neutral').outcome).toBe('holds-nothing');
    expect(q.outcomes[0]!.changes).toEqual([{ do: 'spend-hop' }]);
    expect(q.needs).toContainEqual({ k: 'tier', is: 'simulated' }); // Sky Q5
    expect(num(rb, people, 'v:trail-open')).toBe(1);
  });

  test('a Fearful bystander talks; they hold the Fixer\'s face only if the Fixer did what they saw', () => {
    const saw = (by: string) => R.apply(rb, people, person('fearful'), [{ do: 'remember', act: 'saw-act' }], { by, place: 'records-square', time: 300, witnessed: true });
    expect(R.answer(rb, people, saw('enforcer'), 'neutral').gives).toEqual(['place-time']);
    expect(R.answer(rb, people, saw('fixer'), 'neutral').gives).toEqual(['fixer-face', 'place-time']);
  });
});

describe('K1 memory, K2 ladders', () => {
  test('memory is capped; the oldest goes first; background people remember nothing', () => {
    let s = person('mercenary');
    for (let t = 1; t <= 9; t++) s = R.apply(rb, people, s, [{ do: 'remember', act: 'saw-act' }], { by: 'scout', place: 'p', time: t * 10, witnessed: true });
    expect(s.memory.length).toBe(num(rb, people, rb.memory.cap));
    expect(s.memory[0]!.time).toBe(20);
    // Motive #9: people forget what they saw before what was done to them: a refusal survives eight sightings
    let r = act('ask', person('idealist'), 'neutral', 5).after;
    for (let t = 1; t <= 8; t++) r = R.apply(rb, people, r, [{ do: 'remember', act: 'saw-act' }], { by: 'scout', place: 'p', time: 10 + t, witnessed: true });
    expect(r.memory.length).toBe(8);
    expect(r.memory.map((m) => m.act)).toContain('refusal');
    expect(card('help', r).perOrder.usd).toBe(50000);
    const bg = R.apply(rb, people, person('mercenary', 'debt', { tier: 'background' }), [{ do: 'remember', act: 'saw-act' }], { by: 'fixer', place: 'p', time: 1, witnessed: true });
    expect(bg.memory).toEqual([]);
  });

  test('scales move only by ladder moves and clamp to people.rules: three threats make an Informer, not resentment 3', () => {
    let s = person('fearful');
    for (let i = 0; i < 3; i++) s = R.apply(rb, people, s, [{ do: 'move', on: 'pressure' }], { by: 'fixer', place: 'p', time: i, witnessed: false });
    expect([s.scales.fear, s.scales.resentment]).toEqual([3, 2]);
    const informs = R.event(rb, 'contact.informs');
    expect(informs.needs.every((c) => R.holds(rb, people, c, { subject: { ...s, turned: true }, band: 'neutral', knows: ALL }))).toBe(true);
    const moved = new Set(rb.events.flatMap((e) => [...e.outcomes.flatMap((o) => o.changes), ...(e.also ?? []).flatMap((a) => a.changes)]).filter((c) => c.do === 'move').map((c) => (c.do === 'move' ? c.on : '')));
    expect([...moved].sort()).toEqual(Object.keys(rb.ladders.moves).sort()); // the closed list, all used
  });
});

describe('K3–K5, K9 and Sky Q6 between missions', () => {
  test('news travels along family and workmate ties, one hop, facts only (no scale or standing moves)', () => {
    const w = R.event(rb, 'word.passes');
    const changes = [...w.outcomes.flatMap((o) => o.changes), ...(w.also ?? []).flatMap((a) => a.changes)];
    expect(changes.find((c) => c.do === 'tell')).toEqual({ do: 'tell', along: ['family', 'workmate'], hops: 'v:word-hops' });
    expect(changes.some((c) => c.do === 'move' || c.do === 'standing')).toBe(false);
    expect(num(rb, people, 'v:word-hops')).toBe(1);
    expect(num(rb, people, 'v:word-quiet-loyalty')).toBe(1); // Motive #10: an obliged contact keeps quiet
  });

  test('ties: family and rival never lapse; introductions only along ties people.rules says introduce; a turned keeper tips needs', () => {
    // brief 013's events read ties too (grief, worry, a missed tie); only tie.lapses lapses one
    const lapsing = rb.events.filter((e) => e.id === 'tie.lapses').flatMap((e) => e.needs).filter((c) => c.k === 'tie').flatMap((c) => (c.k === 'tie' ? c.kinds : []));
    expect(lapsing.length).toBeGreaterThan(0);
    expect(lapsing.filter((k) => k === 'family' || k === 'rival')).toEqual([]);
    expect(R.event(rb, 'contact.introduces').needs).toContainEqual({ k: 'introduces-along' });
    expect(R.event(rb, 'keeper.tips').outcomes[0]!.changes).toEqual([{ do: 'learn', what: ['need'], of: 'needs-known' }]);
    expect(people.knowledge.needsKnown.map((n) => n.position)).toContain('keeper');
  });
});

describe('Sky Q2: need replacement from the person\'s own stream (id + mission number)', () => {
  test('the stream is the expander\'s rng: FNV-1a 32 + mulberry32 over the same key', () => {
    for (const key of ['p-cafe-11/need/1', 'p-records-office-13/need/4', 'x']) expect(R.stream(key)() / 2 ** 32).toBe(rng(key).f());
  });

  test('golden draws (the C# port must match): never the need just met, only needs the person can carry', () => {
    const inez: R.NeedHolder = { id: 'p-cafe-11', temperament: 'idealist', need: 'debt', home: true, work: true, ties: ['workmate'] };
    expect(R.replacementNeeds(people, inez)).toEqual(['cash', 'housing', 'papers', 'care', 'advancement', 'secret']);
    const weights = { debt: 3000, cash: 2000, housing: 1000, papers: 1000, advancement: 1000, secret: 500, care: 500 };
    expect([1, 2, 3].map((m) => R.replaceNeed(rb, people, inez, m, weights))).toEqual(['housing', 'care', 'housing']);
    expect(R.stream('p-cafe-11/need/1')()).toBe(1572639180);
    for (let m = 1; m <= 50; m++) expect(R.replaceNeed(rb, people, inez, m, weights)).not.toBe('debt');
    const noHome: R.NeedHolder = { id: 'p-x-01', temperament: 'mercenary', need: 'cash', home: false, work: false, ties: [] };
    expect(R.replacementNeeds(people, noHome)).toEqual(['debt']); // Mercenary responds to Pay: debt (cash met); advancement needs work
  });
});

describe('§05 witnesses and Alert: one incident counts once', () => {
  test('a witnessed threat adds Trail and moves standing once per incident; background witnesses keep no memory; the turning conversations are private (Sky 2026-10-16)', () => {
    const w = R.event(rb, 'witness.sees');
    expect(w.outcomes[0]!.changes).toEqual([{ do: 'trail', add: 'v:witness-trail', per: 'incident' }, { do: 'standing', step: 'v:standing-step', per: 'incident' }]);
    expect(w.also![0]).toEqual({ when: [{ k: 'tier', is: 'simulated' }], changes: [{ do: 'remember', act: 'saw-act' }] });
    expect(num(rb, people, 'v:witness-range-m')).toBe(13);
    expect(act('pay', person('mercenary')).witnessed).toEqual([]); // private: Pay keeps its own Trail, onlookers learn nothing
    expect(act('help', person('idealist', 'debt')).witnessed).toEqual([]);
    expect(act('help', person('idealist', 'cash')).witnessed).toEqual([]);
    expect(act('help', person('idealist', 'papers')).witnessed).toEqual([]);
    expect(act('pressure', person('fearful')).witnessed).toEqual(['threat']);
    expect([rb.traceable['debt-cleared']!.standing, rb.traceable.threat!.standing]).toEqual(['hero', 'menace']);
    expect(rb.privacy?.src).toContain('Sky 2026-10-16');
    const priv = rb.events.filter((e) => e.private).map((e) => e.id).sort();
    expect(priv).toEqual(['scout.listen', 'turn.ask', 'turn.help', 'turn.pay']);
    expect(R.event(rb, 'turn.pressure').private).toBe(false);
    for (const e of rb.events) if (e.private) expect(e.witnesses ?? []).toEqual([]);
  });

  test('Alert: 1st reported incident → inquiry, 2nd → search, violence seen → lockdown; standing bands at ±3', () => {
    expect(rb.alert.byIncident.map((r) => num(rb, people, r))).toEqual([1, 2]);
    expect([num(rb, people, rb.alert.gunfire), num(rb, people, rb.alert.violenceSeen)]).toEqual([2, 3]); // §05 [B] any gunfire → 2 (Sky 2026-10-15, motive-fights.md)
    expect([R.bandOf(rb, people, 0), R.bandOf(rb, people, 3), R.bandOf(rb, people, -3), R.bandOf(rb, people, 2)]).toEqual(['neutral', 'hero', 'menace', 'neutral']);
  });
});

describe('§06 weapons: the cost moves from the shot to the victim (Sky 2026-10-15, briefs/011/motive-fights.md)', () => {
  test('bands and modifiers: a pistol at 12 m hits 3 in 4, walking 3 in 8, walking at a target beside cover 3 in 16', () => {
    expect(R.accuracy(rb, people, 'pistol', 12, [])).toBe(750);
    expect(R.accuracy(rb, people, 'pistol', 4, ['untrained'])).toBe(750);
    expect(R.accuracy(rb, people, 'pistol', 12, ['moving'])).toBe(375);
    expect(R.accuracy(rb, people, 'pistol', 12, ['moving', 'cover'])).toBe(187);
    expect(R.accuracy(rb, people, 'pistol', 15.5, [])).toBe(400); // the long band: 15–16 m since tuning.md §7.9 (was 15–25)
    expect(R.accuracy(rb, people, 'pistol', 17, [])).toBe(0);
    expect(R.accuracy(rb, people, 'carbine', 18, [])).toBe(750);
  });
  test('brief 027 (EXPERIMENTAL fight.lab): four pips from range and edges, each a step; health ×10; every lab value proposed with its reason', () => {
    const step = (p: number) => ({ pips: p, acc: p === 0 ? 0 : num(rb, people, rb.fight.lab!.chance[p - 1]!) }); // the chance per pip is the rulebook's (Motive tunes it)
    const sideEff = num(rb, people, rb.fight.lab!.weapons.sidearm.eff), sideMax = num(rb, people, rb.fight.lab!.weapons.sidearm.max), long = (sideEff + sideMax) / 2;
    expect(R.labPips(rb, people, 'sidearm', 12, [])).toEqual(step(3));
    expect(R.labPips(rb, people, 'sidearm', 4, ['untrained', 'cover'])).toEqual(step(4)); // point blank: untrained and cover don't count
    expect(R.labPips(rb, people, 'sidearm', long, ['untrained', 'cover'])).toEqual(step(1)); // the long band, two edges: clamped at one
    expect(R.labPips(rb, people, 'sidearm', long, ['untrained', 'cover', 'high'])).toEqual(step(1));
    expect(R.labPips(rb, people, 'carbine', 15, ['untrained', 'cover', 'high'])).toEqual(step(2));
    expect(R.labPips(rb, people, 'sidearm', 12, ['moving'])).toEqual(step(2));
    expect(R.labPips(rb, people, 'sidearm', sideMax + 1, [])).toEqual(step(0));
    expect(R.labPips(rb, people, 'rifle', 40, [])).toEqual(step(2));
    expect([1, 2, 3, 4].map((p) => step(p).acc)).toEqual([...[1, 2, 3, 4].map((p) => step(p).acc)].sort((a, b) => a - b)); // the steps rise
    expect(num(rb, people, rb.fight.lab!.healthScale) * num(rb, people, rb.weapons.health.rival)).toBe(30);
    expect(num(rb, people, rb.fight.lab!.coverSnap.snap)).toBeGreaterThan(num(rb, people, rb.ai.cover.snap)); // B5: the snap reaches past the cover's own 70 cm, or it would do nothing
    for (const [k, v] of Object.entries(rb.values)) if (k.startsWith('lab-')) { expect(v.proposed, k).toBe(true); expect(v.reason, k).toMatch(/^EXPERIMENTAL: brief 027 playground/); }
  });
  test('tuning.md §8.3 (Sky 2026-10-21): fire on the move, the tables for every shooter; a running target ×850 past point blank, never with cover', () => {
    const t = (d: number, mods: R.AccMod[]) => R.accuracy(rb, people, 'pistol', d, mods);
    // trained: point blank, effective, long × stand, walk, run toward or across, run away
    expect([4, 12, 15.5].map((d) => [t(d, []), t(d, ['moving']), t(d, ['running']), t(d, ['runningAway'])])).toEqual([[1000, 500, 250, 125], [750, 375, 187, 93], [400, 200, 100, 50]]);
    expect([4, 12, 15.5].map((d) => [t(d, ['untrained']), t(d, ['moving', 'untrained']), t(d, ['running', 'untrained']), t(d, ['runningAway', 'untrained'])])).toEqual([[750, 375, 187, 93], [562, 281, 140, 69], [300, 150, 75, 37]]);
    expect(t(12, ['targetRunning'])).toBe(637); expect(t(4, ['targetRunning'])).toBe(1000); // not at point blank
    expect(t(12, ['targetRunning', 'cover'])).toBe(637); // one step, never stacked with cover
    expect(t(4, ['cover'])).toBe(1000); // tuning.md §8.4 (Sky 2026-10-21): inside point blank cover doesn't count, as in the lab
    const lab = (d: number, e: Parameters<typeof R.labPips>[4]) => R.labPips(rb, people, 'sidearm', d, e);
    const c = (p: number) => num(rb, people, rb.fight.lab!.chance[p - 1]!), zero = num(rb, people, rb.fight.lab!.pip0!);
    expect(zero).toBe(100);
    expect([4, 12, 15.5].map((d) => [lab(d, []).pips, lab(d, ['moving']).pips, lab(d, ['running']).pips, lab(d, ['runningAway']).pips])).toEqual([[4, 3, 2, 0], [3, 2, 1, 0], [2, 1, 0, 0]]);
    expect([4, 12, 15.5].map((d) => [lab(d, ['untrained']).pips, lab(d, ['moving', 'untrained']).pips, lab(d, ['running', 'untrained']).pips])).toEqual([[4, 3, 2], [2, 1, 0], [1, 1, 0]]);
    expect(lab(12, ['running', 'untrained']).acc).toBe(zero); expect(lab(4, ['running']).acc).toBe(c(2));
    expect(lab(12, ['targetRunning']).acc).toBe(Math.floor(c(3) * 850 / 1000)); expect(lab(4, ['targetRunning']).acc).toBe(c(4));
  });
  test('hit counting is deterministic: exactly 3 in 4 at 75 %, the first shot a hit; 2 in 5 at 40 %, the first a miss', () => {
    expect(R.hits(rb, people, 750, 4)).toEqual([true, true, false, true]);
    expect(R.hits(rb, people, 750, 400).filter(Boolean).length).toBe(300);
    expect(R.hits(rb, people, 400, 5)).toEqual([false, true, false, true, false]);
  });
  test('health per kind and the costs of a hit and a down', () => {
    const h = rb.weapons.health, n = (r: string) => num(rb, people, r);
    expect([h.enforcer, h.op, h.guard, h.officer, h.rival, h.patrol, h.civilian, h.responseOfficers].map(n)).toEqual([6, 4, 3, 3, 3, 4, 2, 2]);
    expect([rb.weapons.costs.hit, rb.weapons.costs.down, rb.alert.gunfire, rb.weapons.costs.drawSeenSecurity, rb.weapons.costs.drawSeenPeople].map(n)).toEqual([2, 2, 2, 2, 1]);
    expect([rb.cell.runSpeed, rb.cell.enter.alarm, rb.weapons.costs.loudHold].map(n)).toEqual([280, 30, 5]);
  });
  test('brief 030: a fire order out of reach walks in, outside the lab too; a gun the fight drew holsters 4 s after it (Sky 2026-10-20)', () => {
    expect(rb.weapons.close).toBe(true);
    expect(num(rb, people, rb.weapons.autoHolster!)).toBe(4);
    expect(rb.fight.lab!.close).toBe(true); // the lab's own switch is unchanged
  });
  test('tuning.md §11 (Sky on firing 2026-10-21, engine 0.17.0): the trigger has its rule and the follow ring is 8 m; the cadence stays 1.5 s', () => {
    expect(rb.weapons.trigger!.length).toBeGreaterThan(0);
    expect(num(rb, people, rb.weapons.follow!)).toBe(8);
    expect(num(rb, people, rb.weapons.pistol.every)).toBe(15);
  });
  test('tuning.md §7.9 (Sky 2026-10-20): the kite response and the 16 m sidearm are today\'s; the lab keeps the same kite', () => {
    const n = (r: string) => num(rb, people, r);
    const k = rb.fight.kite!, lk = rb.fight.lab!.kite;
    expect(k.on && lk.on).toBe(true);
    for (const f of ['react', 'replan', 'chase', 'lapse', 'hide', 'contact', 'rearm', 'hideMax', 'coverHold'] as const) expect(k[f], f).toBe(lk[f]!); // one set of numbers
    expect([k.react, k.replan, k.chase, k.lapse, k.hide, k.contact, k.rearm, k.hideMax, k.coverHold].map((r) => n(r!))).toEqual([5, 10, 2000, 100, 30, 3000, 80, 150, 30]); // coverHold: tuning.md §7.10, 3 s
    expect([n(rb.weapons.pistol.eff), n(rb.weapons.pistol.max)]).toEqual([15, 16]);
    expect(Object.keys(rb.values).some((v) => v.startsWith('lab-kite-'))).toBe(false);
  });
});

describe('sight: cones are the Scout\'s skill (Sky 2026-10-16), every agent watches (brief 031, Sky 2026-10-20)', () => {
  test('a watcher is watched with the Watch a person gets, from each agent\'s reach; a watched routine takes two Watches; the hunters are known on arrival', () => {
    const n = (r: string) => num(rb, people, r);
    expect(rb.sight).toBeDefined(); // the live rulebook has it (the schema lets runs pinned before it parse)
    const sight = rb.sight!;
    expect(sight.watch.s === R.event(rb, 'cell.watch').price![0]!.s).toBe(true); // the same Watch, the same time
    expect(n(sight.watch.s)).toBe(20);
    expect(typeof sight.watch.reach).toBe('object'); // brief 031: the reach is the agent's
    const reach = sight.watch.reach as Record<'scout' | 'fixer' | 'tech' | 'enforcer', string>;
    expect([reach.scout, reach.tech, reach.fixer, reach.enforcer].map(n)).toEqual([30, 24, 12, 3]);
    expect(n(reach.scout)).toBeGreaterThan(n('v:door-reach-m')); // the Scout watches from beyond every cone (the posted guard's 26 m)
    expect(n(reach.enforcer)).toBe(n('v:near-m')); // arm's length: inside a person's all-round sense
    expect(rb.values['watch-sight-reach-m']).toBeUndefined(); // retired
    expect(n(sight.watch.routine)).toBe(40);
    expect(n(sight.watch.routine) % n(sight.watch.s)).toBe(0); // a whole number of Watches
    expect(Object.entries(sight.kinds).map(([k, v]) => `${k}:${v.learn}/${v.routine}`))
      .toEqual(['camera:watch/fixed', 'guard:watch/person', 'patrol:watch/watched', 'security:arrival/none', 'rivals:arrival/none']);
    expect(sight.unknown).toBe(rb.preview.unknown); // the card's "?" is the temperament's "?"
    const bad = structuredClone(raw); bad.sight.kinds.camera.learn = 'told';
    expect(Rulebook.safeParse(bad).success).toBe(false);
    const missing = structuredClone(raw); delete missing.sight.kinds.rivals;
    expect(Rulebook.safeParse(missing).success).toBe(false);
    const old = structuredClone(raw); old.sight.watch.reach = 'v:watch-s'; delete old.attention; // a pinned run's one reach still parses (the four reach values then read by no rule: left out)
    for (const a of ['scout', 'fixer', 'tech', 'enforcer']) delete old.values[`watch-reach-${a}-m`];
    for (const v of Object.keys(old.values)) if (v.startsWith('attention-') || v.startsWith('heat-')) delete old.values[v];
    const r = Rulebook.safeParse(old); expect(r.success, JSON.stringify(r.error?.issues?.slice(0, 3))).toBe(true);
  });
  test('every agent watches every watched kind, people included; Listen stays the Scout\'s (brief 031)', () => {
    const sight = rb.sight!;
    expect(Object.entries(sight.kinds).map(([k, v]) => `${k}:${(v.by ?? []).join('+')}`))
      .toEqual(['camera:scout+fixer+tech+enforcer', 'guard:scout+fixer+tech+enforcer', 'patrol:scout+fixer+tech+enforcer', 'security:', 'rivals:']);
    expect(R.event(rb, 'cell.watch').by).toBe('cell'); // any agent
    expect(R.event(rb, 'cell.watch').private).toBe(false);
    expect(R.event(rb, 'scout.listen').by).toBe('scout');
    expect(rb.events.some((e) => e.id === 'scout.watch')).toBe(false); // renamed
    const bad = (f: (k: any) => void) => { const b = structuredClone(raw); f(b.sight.kinds); return Rulebook.safeParse(b).success; };
    expect(bad(() => {})).toBe(true);
    expect(bad((k) => { delete k.camera.by; })).toBe(false);                  // a watched kind names who watches it
    expect(bad((k) => { k.security.by = ['scout']; })).toBe(false);           // a kind known on arrival has nothing to watch
    expect(bad((k) => { k.camera.by = ['tech']; })).toBe(false);              // the Scout always watches
    expect(bad((k) => { k.patrol.by = ['scout', 'tech', 'tech']; })).toBe(false);
    expect(bad((k) => { k.patrol.by = ['scout', 'driver']; })).toBe(false);   // unknown = reject
  });
  test('the attention clock: four Heat bands, generous at the start, wary within a Watch in every band (brief 031 §2.1)', () => {
    const n = (r: string) => num(rb, people, r);
    expect(rb.attention).toBeDefined();
    const at = rb.attention!;
    expect(at.bands.map((b) => [b.id, n(b.heatFrom), n(b.notice), n(b.wary), n(b.report)])).toEqual([
      ['unknown', 0, 8, 20, 45], ['noticed', 3, 6, 18, 35], ['wanted', 6, 5, 15, 25], ['infamous', 10, 3, 12, 18],
    ]);
    for (const b of at.bands) {
      expect(n(b.notice)).toBeLessThan(n(b.wary));
      expect(n(b.wary)).toBeLessThan(n(b.report));
      expect(n(b.wary)).toBeLessThanOrEqual(n(rb.sight!.watch.s)); // D1: a whole Watch in sight makes the watcher wary
    }
    for (let i = 1; i < at.bands.length; i++) expect(n(at.bands[i]!.heatFrom)).toBeGreaterThan(n(at.bands[i - 1]!.heatFrom));
    expect(at.bands.filter((b) => n(b.report) < n(rb.sight!.watch.s)).map((b) => b.id)).toEqual(['infamous']); // only a famous cell can't Watch from inside a sight line
    expect(at.sense).toEqual({ camera: 'cone', patrol: 'cone', robot: 'cone', guard: 'near', officer: 'near' }); // L2: machines by their cone only
    expect([at.near, at.alert, at.report.scan, at.report.call]).toEqual(['v:near-m', 'v:alert-inquiry', 'v:camera-scan-s', 'v:post-call-s']);
    const bad = structuredClone(raw); bad.attention.sense.robot = 'near';
    expect(Rulebook.safeParse(bad).success).toBe(false);
  });
});

describe('brief 013: takedowns, bodies, witnesses, the response dial (Sky 2026-10-16; Motive, briefs/013/motive-takedowns.md)', () => {
  const n = (r: string) => num(rb, people, r);
  const td = rb.takedown!, wi = rb.witness!, rs = rb.response!, cp = rb.campaign!;

  test('car.pace.road is the city\'s access-road pace (life.json traffic.byClass.access.speed, Motive 2026-10-19, brief 023)', () => {
    const life = read('contracts/world/harbour-district.life.json');
    expect(rb.car?.pace?.road).toBe('v:car-road-cm-s');
    expect(rb.values['car-road-cm-s'].v).toBe(Math.round(life.traffic.byClass.access.speed * 100));
    expect(rb.values['car-yard-cm-s'].v).toBeLessThan(rb.values['car-road-cm-s'].v);
  });

  test('the live rulebook has the sections (optional only for runs pinned before them); the 18 s check-in is retired', () => {
    expect(rb.cell.loop.targets).toBeDefined(); expect(rb.cell.take.struggle).toBeDefined(); // optional in the schema only so dialogue r017-r019's pinned rulebook parses
    expect([td, wi, rs, cp].every((x) => x !== undefined)).toBe(true);
    expect([rb.cell.board, rb.fight.block, rb.fight.blockers, rb.fight.blockNear, rb.fight.blockFar, cp.custody, rb.rounds].every((x) => x !== undefined)).toBe(true); // brief 018 (the getaway)
    expect(rb.car?.drive).toBeDefined(); // brief 023 (engine 0.8.2): car.drive
    expect(rb.fight.over).toBeDefined(); // brief 023 (Motive 2026-10-19): fight.over, fire orders end with the final fight
    expect(rb.car?.pace && rb.car?.paceRule).toBeTruthy(); // brief 023 (Motive 2026-10-19): car.pace
    expect(rb.cell.drive?.experimental).toBeTruthy(); // brief 025 (engine 0.9.0): cell.drive, marked experimental
    expect(rb.fight.lab?.experimental).toBeTruthy(); expect(rb.fight.lab?.on).toBe(false); // brief 027 (engine 0.10.0): fight.lab, marked experimental, off unless a run's start turns it on
    expect(rb.crowd.pace).toBeDefined(); // brief 023 (Pulse): crowd.pace, optional in the schema only so dialogue r017-r019's pinned rulebook parses
    expect(rb.values['check-in-s']).toBeUndefined();
    expect(rb.values['response-check-in-s']).toBeUndefined();
    expect(rb.security.checkIn).toBeUndefined();
    expect(rb.security.response.checkIn).toBeUndefined();
    expect(n(rs.checkIn)).toBe(90); // Sky answer 4: every staff post, every 90 s from the shift's start
  });

  test('the takedown: Motive §7.1, read by ref', () => {
    expect([td.ko, td.kill, td.struggle, td.wake, td.stagger, td.restrain, td.free, td.callOut].map(n)).toEqual([6, 3, 3, 120, 4, 8, 3, 10]);
    expect(td.reach).toBe(rb.cell.atReach); // hands: the same reach as an act at a device
    expect([td.carry.untrained, td.carry.drop, td.stash.container, td.stash.door].map(n)).toEqual([70, 1, 5, 10]);
    expect(n(td.carry.untrained) * 2).toBe(n(rb.cell.speed)); // half the walk; only the Enforcer carries at a walk
    expect([n(td.service.period), n(td.service.dwell)]).toEqual([900, 20]);
    // the act's Trail: a knockout is a hit and a down (+4), a kill +2 more (+6): a quiet takedown is 2 T cheaper than shooting
    expect(n(rb.weapons.costs.hit) + n(rb.weapons.costs.down)).toBe(4);
    expect(n(rb.weapons.costs.hit) + n(rb.weapons.costs.down) + n(td.trail.kill)).toBe(6);
    expect([n(td.koAlertMax), n(td.foundKillStanding)]).toEqual([1, -1]); // Sky merged call 2; Sky answer 1
  });

  test('what a landed report says, and its Alert (Motive §2.3): a knockout never above 1 by itself; silent acts reach Alert only by a report', () => {
    const a = wi.alert;
    expect([a.seen, a.carry, a.down, a.dead, a.woken, a.noise, a.mark, a.missing].map(n)).toEqual([3, 2, 1, 2, 1, 1, 1, 1]);
    expect(a.seen).toBe(rb.alert.violenceSeen); // a seen takedown is the lockdown a seen shooting is, when the report lands
    for (const k of [a.down, a.woken, a.noise, a.mark, a.missing]) expect(n(k)).toBeLessThanOrEqual(n(td.koAlertMax));
    expect([wi.react, wi.call, wi.postCall, wi.robot, wi.cameraScan].map(n)).toEqual([2, 6, 3, 4, 20]);
    expect([n(wi.mercenaryAlert), n(wi.terrifiedFear), n(wi.screamFear), n(wi.cap), n(wi.scream), n(wi.tell)]).toEqual([2, 3, 2, 4, 20, 20]);
    expect(wi.kin).toEqual(['lover', 'family']); // Motive rev 4 R4.2: kin (the witness row) is lover or family; workmate is not kin
    expect(wi.close).toEqual(['lover', 'family', 'workmate']); // whose absence or loss a person feels: expectations, the alarm's places, grief
    expect(rb.values['stash-door-holds'].v).toBe(3); expect(rb.values['take-struggle-s'].v).toBe(5); expect(rb.values['case-trail-eq'].v).toBe(3); // rev 4 R4.6
    expect(wi.tells[0]).toBe('lover');
  });

  test('the Enforcer\'s face is a fact (Sky D3), scoped to the new acts: older entries by the Enforcer still give place and time only', () => {
    expect(people.talker.facts).toContain('enforcer-face');
    expect(rb.memory.faceFacts).toEqual({ fixer: 'fixer-face' });
    const withFace = Object.entries(rb.memory.acts).filter(([, v]) => Array.isArray(v.facts) && v.facts.includes('enforcer-face')).map(([k]) => k);
    expect(withFace.sort()).toEqual(['attacked-aware', 'saw-takedown']);
    const saw = person('loyal', 'debt', { memory: [{ act: 'saw-takedown', by: 'enforcer', place: 'office-entrance', time: 1, witnessed: true }] });
    expect([...R.factsOf(rb, saw)].sort()).toEqual(['enforcer-face', 'place-time']);
    const ran = person('loyal', 'debt', { memory: [{ act: 'saw-runner', by: 'enforcer', place: 'office-entrance', time: 1, witnessed: true }] });
    expect([...R.factsOf(rb, ran)]).toEqual(['place-time']);
  });

  test('a reporter volunteers what they hold when questioned, without the Talker test (Pulse §2.4)', () => {
    const reporter = person('loyal', 'debt', { memory: [{ act: 'found-body', by: '', place: 'office-entrance', time: 1, witnessed: true }, { act: 'reported', by: '', place: 'office-entrance', time: 2, witnessed: false }] });
    expect(R.answer(rb, people, reporter, 'neutral')).toEqual({ outcome: 'volunteers', gives: ['place-time'] });
    const quiet = person('loyal', 'debt', { memory: [{ act: 'found-body', by: '', place: 'office-entrance', time: 1, witnessed: true }] });
    expect(R.answer(rb, people, quiet, 'neutral').outcome).toBe('nothing'); // a Loyal finder who didn't report isn't a Talker
  });

  test('the response dial is Motive\'s §5.3 table as data: five levels, a dispatch that shortens as the swarm grows', () => {
    const table = rs.levels.map((l) => [l.name, n(l.dispatch), n(l.a1Robots), n(l.a2Units), n(l.a2Robots), l.reinforce ? n(l.reinforce) : null, n(l.unitCap), n(l.seal), n(l.a3Units), l.doorsLock]);
    expect(table).toEqual([
      ['lax', 20, 1, 1, 0, null, 1, 180, 0, false],
      ['routine', 12, 1, 1, 0, null, 1, 145, 0, false], // Motive rev 3: exactly today's response
      ['watchful', 10, 2, 1, 2, 40, 3, 145, 1, true],
      ['guarded', 8, 2, 2, 2, 30, 4, 120, 1, true],
      ['swarm', 6, 3, 2, 4, 20, 6, 90, 2, true],
    ]);
    expect(rs.levels[1]!.seal).toBe('v:seal-s'); // level 1 is today's response: the gunfire delays are its arrivals
    expect(n(rs.levels[1]!.dispatch)).toBe(n(rb.security.response.witness)); // a civilian witness's 12 s = a report landing at once + level 1's dispatch
    for (let i = 1; i < 5; i++) expect(n(rs.levels[i]!.dispatch)).toBeLessThan(n(rs.levels[i - 1]!.dispatch));
    expect([n(rs.world.report), n(rs.world.dock)]).toEqual([40, 40]); // R-REPORT, R-DOCK (Splice's world rules)
    expect([n(rs.robotSpeed), n(rs.robotWire), n(rs.robotReach), n(rs.launchGap), n(rs.check), n(rs.spread), n(rs.cap)]).toEqual([85, 4, 60, 15, 10, 3, 30]);
    for (const l of rs.levels.slice(0, 2)) expect([l.reinforce, n(l.a3Units)]).toEqual([undefined, 0]); // levels 0 and 1: no swarm (Motive rev 3)
    expect([n(rs.search.s), n(rs.search.m), n(rs.search.stash), n(rs.standDown.inquiry), n(rs.standDown.search)]).toEqual([60, 30, 10, 60, 180]);
    expect(n(rs.robotSpeed)).toBe(n(rb.security.patrol.chase)); // Pulse's lever defaults to the patrol's own speed: nothing changes until tuned
    expect([n(rs.suspicion.s), n(rs.suspicion.meet)]).toEqual([20, 6]);
    expect(Object.values(rs.suspicion.pm).map(n)).toEqual([1000, 1000, 1000, 1000]); // Aster: flat 20 s by default, the temperament scale built
  });

  test('the campaign: Sky\'s missing-person numbers', () => {
    expect([cp.missing.trail, cp.missing.tiesFear, cp.areaWatch.step, cp.areaWatch.missions, cp.cases.perLevel, cp.cases.cap, cp.cases.close].map(n)).toEqual([2, 1, 1, 3, 2, 2, 3]);
    expect(rb.phases['between-missions'].slice(-6, -1)).toEqual(['bodies.surface', 'body.grieved', 'missing.worried', 'area.hardens', 'cases.count']); // brief 014: standing.drift runs last
  });

  test('unknown = reject: a fifth level missing, a lock that is no boolean, a kin tie people.rules lacks, an unknown cancel line', () => {
    const four = structuredClone(raw); four.response.levels.pop();
    expect(Rulebook.safeParse(four).success).toBe(false);
    const lock = structuredClone(raw); lock.response.levels[2].doorsLock = 'yes';
    expect(Rulebook.safeParse(lock).success).toBe(false);
    const kin = structuredClone(raw); kin.witness.kin.push('neighbour');
    expect(checkRulebook(Rulebook.parse(kin), people).join()).toContain('tie kind neighbour');
    const cancel = structuredClone(raw); cancel.cancel.takedown = 'quiet';
    expect(Rulebook.safeParse(cancel).success).toBe(false);
    const extra = structuredClone(raw); extra.takedown.stun = 'v:ko-s';
    expect(Rulebook.safeParse(extra).success).toBe(false);
  });
});

describe('brief 014: Standing leans Hero (Sky 2026-10-16; briefs/014/motive-standing.md)', () => {
  const n = (r: string) => num(rb, people, r);
  const tell = (who: string, ...done: R.Teller['done'][number][]): R.Teller => ({ who, done });
  test('the numbers: start +2, the bands unchanged, the word ±1 a person capped at ±2, drift 1; every one Sky\'s, with Motive\'s reasons', () => {
    expect([n(rb.standing.start), n(rb.standing.hero), n(rb.standing.menace), n(rb.standing.step)]).toEqual([2, 3, -3, 1]);
    expect(rb.standing.word && rb.standing.drift && rb.standing.causes).toBeTruthy(); // optional in the schema only for pinned runs
    expect([n(rb.standing.word!.step), n(rb.standing.word!.cap), n(rb.standing.drift!)]).toEqual([1, 2, 1]);
    for (const v of ['standing-start', 'word-standing-step', 'word-standing-cap', 'standing-drift']) { expect(rb.values[v]!.src).toContain('Sky 2026-10-16'); expect(rb.values[v]!.reason).toContain('Motive'); }
    expect(R.bandOf(rb, people, n(rb.standing.start))).toBe('neutral'); // one step short of Hero: the Handover's prices stay neutral
    expect(rb.standing.word!.ties).toEqual(['family', 'lover', 'workmate']);
  });
  test('the events: the word after word.passes, the drift last; harm outranks help; a feeling, never a fact', () => {
    const bm = rb.phases['between-missions'];
    expect(bm[bm.indexOf('word.passes') + 1]).toBe('standing.word');
    expect(bm.at(-1)).toBe('standing.drift');
    const w = R.event(rb, 'standing.word');
    expect(w.outcomes.map((o) => o.id)).toEqual(['pressured', 'knocked-out', 'helped']);
    const changes = w.outcomes.flatMap((o) => o.changes);
    expect(changes.every((c) => c.do === 'word-standing')).toBe(true); // no tell, no learn, no remember: nothing reaches the investigators
    expect(R.event(rb, 'word.passes').outcomes.flatMap((o) => o.changes).some((c) => c.do === 'word-standing')).toBe(false); // facts never move it
    expect(R.event(rb, 'standing.drift').outcomes.every((o) => o.changes.some((c) => c.do === 'debrief' && c.line === 'standing'))).toBe(true);
    expect(rb.debrief.lines['standing']!.fields).toEqual(['district', 'from', 'to', 'band', 'cause', 'who']);
    expect(checkRulebook(rb, people)).toEqual([]);
  });
  test('help spreads (N88): Inez helped, the district +2 → +3, the Hero band', () => {
    expect(R.standingNext(rb, people, 2, [], [tell('waitress', 'helped')])).toMatchObject({ to: 3, band: 'hero', cause: 'word-helped', who: 'waitress', word: 1, drift: 0 });
  });
  test('harm spreads (N89, N90): a threat seen and her word: 0; a knockout unseen: +1', () => {
    expect(R.standingNext(rb, people, 2, [{ delta: -1, cause: 'seen-threat', who: 'waitress' }], [tell('waitress', 'pressured')])).toMatchObject({ to: 0, band: 'neutral', cause: 'seen-threat', who: 'waitress' });
    expect(R.standingNext(rb, people, 2, [], [tell('guard', 'knocked-out')])).toMatchObject({ to: 1, cause: 'word-knocked-out', who: 'guard' });
  });
  test('the cap (N91): three helped net +3, the district moves +2; seen acts are never capped', () => {
    expect(R.standingNext(rb, people, 2, [], ['waitress', 'driver', 'guard'].map((w) => tell(w, 'helped')))).toMatchObject({ to: 4, word: 2, wordUncapped: 3, cause: 'word-helped', who: 'waitress' });
    const seen = [1, 2, 3, 4].map(() => ({ delta: -1, cause: 'seen-fight-security', who: '' }));
    expect(R.standingNext(rb, people, 2, seen, [])).toMatchObject({ to: -2, cause: 'seen-fight-security' });
  });
  test('drift both ways, never past the start (N92, N92b); nothing moved at the start is steady (N94)', () => {
    expect([5, 3, 2, -3].map((f) => { const x = R.standingNext(rb, people, f, [], []); return [x.to, x.cause]; })).toEqual([[4, 'drift-down'], [2, 'drift-down'], [2, 'steady'], [-2, 'drift-up']]);
    expect(R.standingNext(rb, people, 2, [{ delta: -1, cause: 'seen-threat', who: 'waitress' }], [tell('driver', 'helped')])).toMatchObject({ to: 2, drift: 0, cause: 'balanced' }); // moves that cancel are still moves: no drift
  });
  test('helped and pressured counts as pressured (N93)', () => {
    expect(R.standingNext(rb, people, 2, [], [tell('waitress', 'helped', 'pressured')])).toMatchObject({ to: 1, cause: 'word-pressured', word: -1 });
  });
  test('a public fight moves Standing once, by whom the cell fought (Sky 2026-10-16; N95-N101): fight.public, the first outcome that holds', () => {
    const e = R.event(rb, 'fight.public');
    expect([e.src, e.phase, e.by]).toEqual([expect.stringContaining('Sky 2026-10-16'), 'mission', 'city']);
    expect(e.needs).toEqual([{ k: 'fight', quiet: 'v:crowd-quiet-s' }, { k: 'public', m: 'v:crowd-scatter-m' }]);
    expect(e.outcomes.map((o) => o.id)).toEqual(['security', 'first', 'rivals', 'none']);
    expect([n(rb.crowd.quiet), n(rb.crowd.scatter), n('v:fight-standing-step'), rb.fight.rivalsPublic]).toEqual([30, 30, 1, 'menace']);
    expect(rb.traceable['gunfire']!.standing).toBe('none'); // a shot is no Standing act: the fight is (seen-gunfire retired)
    expect(Object.keys(rb.standing.causes!).filter((c) => c.startsWith('seen-fight-')).sort()).toEqual(['seen-fight-first', 'seen-fight-rivals', 'seen-fight-security']);
    expect(rb.standing.causes!['seen-gunfire']).toBeUndefined();
    const f = (x: Partial<R.Fight>) => R.fightStanding(rb, people, { public: true, firedOn: [], firstByCell: false, rivalsBroken: false, ...x });
    expect(f({ firedOn: ['rivals'], rivalsBroken: true })).toEqual({ outcome: 'rivals', delta: 1, cause: 'seen-fight-rivals' }); // N95: feared rivals driven off
    expect(f({ firedOn: ['rivals'], rivalsBroken: false }).delta).toBe(0); // N96: not won
    expect(f({ firedOn: ['rivals'], firstByCell: true, rivalsPublic: 'neutral' })).toEqual({ outcome: 'first', delta: -1, cause: 'seen-fight-first' }); // N97
    expect(f({ firedOn: ['rivals'], firstByCell: false, rivalsPublic: 'neutral', rivalsBroken: true }).delta).toBe(0); // neutral rivals who fired first
    expect(f({ firedOn: ['security'] })).toEqual({ outcome: 'security', delta: -1, cause: 'seen-fight-security' }); // N98
    expect(f({ firedOn: ['security', 'rivals'], rivalsBroken: true })).toMatchObject({ outcome: 'security', delta: -1 }); // N99: authority outranks
    expect(f({ public: false, firedOn: ['security'] }).delta).toBe(0); // N100: nobody heard it
    const bad = structuredClone(raw); bad.events.find((x: { id: string }) => x.id === 'fight.public').outcomes[0].when[0].in = ['police'];
    expect(Rulebook.safeParse(bad).success).toBe(false); // the foes are security or rivals
    const cause = structuredClone(raw); cause.events.find((x: { id: string }) => x.id === 'fight.public').outcomes[2].changes[0].cause = 'seen-fight-won';
    expect(checkRulebook(Rulebook.parse(cause), people).join()).toContain('standing.causes'); // a fight's cause is a standing cause, both ways
  });
  test('unknown = reject: a cause the engine does not emit, a done-to it does not know, a word tie people.rules lacks', () => {
    const cause = structuredClone(raw); cause.standing.causes['seen-kindness'] = 'x';
    expect(checkRulebook(Rulebook.parse(cause), people).join()).toContain('standing.causes');
    const done = structuredClone(raw); done.events.find((e: { id: string }) => e.id === 'standing.word').needs[1].in.push('bribed');
    expect(Rulebook.safeParse(done).success).toBe(false);
    const tie = structuredClone(raw); tie.standing.word.ties.push('neighbour');
    expect(checkRulebook(Rulebook.parse(tie), people).join()).toContain('tie kind neighbour');
  });
});
