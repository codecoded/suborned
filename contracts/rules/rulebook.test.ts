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
    expect(R.event(rb, 'scout.watch').outcomes[0]!.changes).toEqual([{ do: 'learn', what: ['routine'], of: 'subject' }]);
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
    const lapsing = rb.events.flatMap((e) => e.needs).filter((c) => c.k === 'tie').flatMap((c) => (c.k === 'tie' ? c.kinds : []));
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
  test('a witnessed payment, threat or debt cleared adds Trail and moves standing once per incident; background witnesses keep no memory', () => {
    const w = R.event(rb, 'witness.sees');
    expect(w.outcomes[0]!.changes).toEqual([{ do: 'trail', add: 'v:witness-trail', per: 'incident' }, { do: 'standing', step: 'v:standing-step', per: 'incident' }]);
    expect(w.also![0]).toEqual({ when: [{ k: 'tier', is: 'simulated' }], changes: [{ do: 'remember', act: 'saw-act' }] });
    expect(num(rb, people, 'v:witness-range-m')).toBe(13);
    expect(act('pay', person('mercenary')).witnessed).toEqual(['payment']);
    expect(act('help', person('idealist', 'debt')).witnessed).toEqual(['debt-cleared']);
    expect(act('help', person('idealist', 'papers')).witnessed).toEqual([]);
    expect(act('pressure', person('fearful')).witnessed).toEqual(['threat']);
    expect([rb.traceable['debt-cleared']!.standing, rb.traceable.threat!.standing]).toEqual(['hero', 'menace']);
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
    expect(R.accuracy(rb, people, 'pistol', 20, [])).toBe(400);
    expect(R.accuracy(rb, people, 'pistol', 26, [])).toBe(0);
    expect(R.accuracy(rb, people, 'carbine', 18, [])).toBe(750);
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
});
