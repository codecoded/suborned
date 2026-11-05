// Brief 096, a worse Help once (engine 0.45.0, Keel; Motive briefs/096/motive/help-fearful.md Q1 (b); Sky 2026-11-05 "As you
// recommend"): after the Listen a Mercenary (or an Idealist a mood made worse) takes Help in one visit, one step dearer (+$10,000);
// before it the small offer is still refused (0.44.0); a Fearful person stays at $40,000 and a Loyal person's repeat stays. Through the
// reference evaluator; the engine's behaviour is 096-HELP-7..10 (sim ScenariosHelpOnce096.cs).
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { PeopleRules } from '../people/people.rules.schema.ts';
import { Rulebook } from './rulebook.schema.ts';
import * as R from './rulebook.ref.ts';

const root = join(import.meta.dir, '..', '..');
const read = (p: string) => JSON.parse(readFileSync(join(root, p), 'utf8'));
const raw = read('contracts/rules/rulebook.json');
const rb = Rulebook.parse(raw);
const people = PeopleRules.parse(read('contracts/people/people.rules.json'));
const TOLD = new Set<R.Know>(['routine', 'need-told']), HEARD = new Set<R.Know>(['routine', 'need', 'temperament']);
const person = (temperament: string, extra: Partial<R.Subject> = {}): R.Subject => ({
  id: 'p-driver', tier: 'simulated', temperament, need: 'debt',
  scales: { ...people.temperaments[temperament as keyof PeopleRules['temperaments']].start }, memory: [], turned: false, ...extra,
});
const help = R.event(rb, 'turn.help');
const card = (s: R.Subject, band: R.Band, knows: ReadonlySet<R.Know>) => R.quote(rb, people, help, { subject: s, band, knows });
const refusal = [{ act: 'refusal', by: 'fixer', place: 'pavilion', time: 100, witnessed: false }];
const BANDS: R.Band[] = ['hero', 'neutral', 'menace'];
const val = (id: string) => raw.values[id].v as number;

describe('brief 096: a worse Help once, one step dearer (Motive Q1 (b))', () => {
  test('a Mercenary, heard: one visit, $50,000, 35 s, +1 T, turns, in every band; worse-once alone of the step lines holds', () => {
    for (const band of BANDS) {
      const q = card(person('mercenary'), band, HEARD);
      expect([q.outcome, q.perOrder, q.orders], band).toEqual(['turned', { usd: 50000, s: 35, trail: 1 }, 1]);
      const on = q.lines.filter((l) => l.holds === true).map((l) => l.line);
      expect(on, band).toEqual(['base', 'worse-once']);
    }
    expect(val('worse-help-once-usd')).toBe(10000);
  });
  test('after a refused small offer: $60,000 and +2 T, still one visit', () => {
    const q = card(person('mercenary', { memory: refusal }), 'neutral', HEARD);
    expect([q.outcome, q.perOrder, q.orders]).toEqual(['turned', { usd: 60000, s: 35, trail: 2 }, 1]);
  });
  test('the outcome is turned on the first order, the money charged once', () => {
    const r = R.resolve(rb, people, help, { subject: person('mercenary'), band: 'neutral', knows: HEARD });
    expect(r.outcome).toBe('turned');
  });
  test('before the Listen it is still the small offer (0.44.0), the same card as anyone', () => {
    const q = card(person('mercenary'), 'neutral', TOLD);
    expect([q.outcome, q.perOrder.usd]).toEqual(['small-offer', 0]);
  });
  test('nobody else moves: a Fearful person $40,000 once, a Loyal person twice at $40,000 (Q2 (a)), an Idealist once at $40,000', () => {
    const got = ['fearful', 'loyal', 'idealist'].map((t) => { const q = card(person(t), 'neutral', HEARD); return [t, q.orders, q.perOrder.usd]; });
    expect(got).toEqual([['fearful', 1, 40000], ['loyal', 2, 40000], ['idealist', 1, 40000]]);
  });
  test('the purse: Inez $40,000 + the driver $50,000 = $90,000; one small offer first $100,000, exactly; both $110,000, over', () => {
    const inez = (m: R.Mem[]) => card(person('fearful', { memory: m }), 'neutral', HEARD).perOrder.usd;
    const lukac = (m: R.Mem[]) => card(person('mercenary', { memory: m }), 'neutral', HEARD).perOrder.usd;
    const purse = 100000; // §04 the purse (Game.Cash at the start)
    expect(inez([]) + lukac([])).toBe(90000);
    expect(Math.max(inez(refusal) + lukac([]), inez([]) + lukac(refusal))).toBe(purse);
    expect(inez(refusal) + lukac(refusal)).toBeGreaterThan(purse);
  });
  test('unknown = reject: the two-visit later line is gone; the step lines read the temperaments named', () => {
    const lines = help.price!.map((l) => l.line);
    expect(lines).not.toContain('visits-share-later');
    const temps = (id: string) => (help.price!.find((l) => l.line === id)!.when ?? []).flatMap((c) => (c.k === 'temperament' ? c.in : []));
    expect([temps('worse-once'), temps('one-more-help'), temps('visits-share-first')]).toEqual([['mercenary', 'idealist'], ['loyal'], ['loyal']]);
    const bad = structuredClone(raw); bad.values['worse-help-once-usd'].extra = 1;
    expect(Rulebook.safeParse(bad).success).toBe(false);
  });
});
