// Brief 096, Help for a Fearful person (engine 0.44.0, Keel; Motive briefs/096/motive/help-fearful.md; Sky 2026-11-05 on paying Inez,
// option 1: "I think 1 is fine for now"). Run through the reference evaluator: before the need is heard (told only) Help is a small offer
// every temperament refuses, $0, 35 s, +1 T, priced alone; after it a Fearful person takes the full offer once, in every band; nobody else
// moves. The engine's behaviour is the 096-HELP-* scenarios (sim ScenariosHelpOffer096.cs).
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
  id: 'p-cafe-14', tier: 'simulated', temperament, need: 'debt',
  scales: { ...people.temperaments[temperament as keyof PeopleRules['temperaments']].start }, memory: [], turned: false, ...extra,
});
const help = R.event(rb, 'turn.help');
const card = (s: R.Subject, band: R.Band, knows: ReadonlySet<R.Know>) => R.quote(rb, people, help, { subject: s, band, knows });
const TEMPS = ['idealist', 'mercenary', 'fearful', 'loyal'];
const BANDS: R.Band[] = ['hero', 'neutral', 'menace'];

describe('brief 096: Help for a Fearful person (Sky option 1)', () => {
  test('H1: told, not heard: a small offer every temperament refuses, in every band, $0 35 s +1 T alone, no cash asked', () => {
    for (const band of BANDS) for (const t of TEMPS) {
      const q = card(person(t), band, TOLD);
      expect([q.outcome, q.perOrder, q.worst.usd, q.orders], `${t} ${band}`).toEqual(['small-offer', { usd: 0, s: 35, trail: 1 }, 0, 1]);
      expect(q.lines.filter((l) => l.holds !== false).map((l) => l.line), `${t} ${band}`).toEqual(['small-offer']);
    }
  });
  test('H1: the refusal charges time and Trail, leaves a remembered refusal, and does not mark Help refused', () => {
    const r = R.resolve(rb, people, help, { subject: person('fearful'), band: 'neutral', knows: TOLD });
    expect(r.outcome).toBe('small-offer');
    expect(r.changes).toEqual([{ do: 'charge', parts: ['s', 'trail'] }, { do: 'remember', act: 'refusal' }, { do: 'debrief', line: 'small-offer' }]);
  });
  test('H1: nothing told: Help is locked (needs-need), as before', () => {
    expect(R.needsHold(rb, people, help, { subject: person('fearful'), band: 'neutral', knows: new Set(['routine']), given: { reach: true } })).toBe(false);
    expect(R.needsHold(rb, people, help, { subject: person('fearful'), band: 'neutral', knows: TOLD, given: { reach: true } })).toBe(true);
  });
  test('H2: heard: a Fearful person takes the full offer once, $40,000, in every band; after a refusal $50,000 and +2 T', () => {
    for (const band of BANDS) {
      const q = card(person('fearful'), band, HEARD);
      expect([q.outcome, q.perOrder, q.orders], band).toEqual(['turned', { usd: 40000, s: 35, trail: 1 }, 1]);
    }
    const refused = person('fearful', { memory: [{ act: 'refusal', by: 'fixer', place: 'cafe-entrance', time: 100, witnessed: false }] });
    expect(card(refused, 'neutral', HEARD).perOrder).toEqual({ usd: 50000, s: 35, trail: 2 });
  });
  test('H6: everyone else once heard (neutral: Idealist once, Mercenary once at $50,000 since engine 0.45.0, Loyal twice at $40,000)', () => {
    const got = TEMPS.map((t) => { const q = card(person(t), 'neutral', HEARD); return [t, q.orders, q.perOrder.usd]; });
    expect(got).toEqual([['idealist', 1, 40000], ['mercenary', 1, 50000], ['fearful', 1, 40000], ['loyal', 2, 40000]]);
  });
  test('H5: Fearful, Menace still folds free on Ask', () => {
    const q = R.quote(rb, people, R.event(rb, 'turn.ask'), { subject: person('fearful'), band: 'menace', knows: HEARD });
    expect([q.outcome, q.perOrder.usd]).toEqual(['turned', 0]);
  });
  test('unknown = reject: alone is true or absent; need-told is a knowledge', () => {
    const bad = structuredClone(raw); bad.events.find((e: { id: string }) => e.id === 'turn.help').price[2].alone = false;
    expect(Rulebook.safeParse(bad).success).toBe(false);
    expect(rb.events.find((e) => e.id === 'turn.help')!.price![2]!.line).toBe('small-offer');
  });
});
