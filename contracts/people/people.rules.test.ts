// Done-check for brief 010 A: people.rules.json parses with its schema and covers every trade and occupation the world has.
// Run: bun test contracts/people/people.rules.test.ts
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { PeopleRules, TEMPERAMENTS, TIE_KINDS, allowedTemperaments, isTalker } from './people.rules.schema.ts';

const root = join(import.meta.dir, '..', '..');
const read = (p: string) => JSON.parse(readFileSync(join(root, p), 'utf8'));
const rules = PeopleRules.parse(read('contracts/people/people.rules.json'));
const plan = read('contracts/world/harbour-district.plan.json');
const life = read('contracts/world/harbour-district.life.json');

describe('people.rules.json', () => {
  test('parses with the schema', () => expect(rules.schema).toBe('suborned.people.rules/1'));

  test('the brief sets: four temperaments, five tie kinds', () => {
    expect(Object.keys(rules.temperaments).sort()).toEqual([...TEMPERAMENTS].sort());
    expect(Object.keys(rules.ties.kinds).sort()).toEqual([...TIE_KINDS].sort());
  });

  test('covers every plan trade and every life.json occupation, with its life.json category', () => {
    const planTrades = Object.keys(plan.standards.frontage.trades).sort();
    const occupations = life.generator.occupations as Record<string, { category: string }>;
    const covered = Object.keys(rules.temperamentsBy.trades).sort();
    expect(covered).toEqual(planTrades);
    expect(covered).toEqual(Object.keys(occupations).sort());
    for (const [t, o] of Object.entries(occupations)) expect(rules.temperamentsBy.trades[t]!.category).toBe(o.category);
  });

  test('every life.json routine skeleton has a role or trade to wear it', () => {
    const roles = rules.temperamentsBy.roles;
    for (const r of ['resident', 'docker']) expect(roles[r]).toBeDefined();
    expect(roles.visitor).toBeDefined(); // shopper, reveller from beyond the sheet
  });

  test('the Handover roles can be cast', () => {
    expect(allowedTemperaments(rules, 'guard')).toEqual(['loyal', 'mercenary']); // brief 010 A: never Idealist
    expect(allowedTemperaments(rules, 'official-driver')?.length).toBeGreaterThanOrEqual(2);
    const cafe = rules.temperamentsBy.employers.cafe!;
    expect(rules.positions.byCategory[rules.temperamentsBy.trades[cafe.trade!]!.category]).toContain('server');
    expect(allowedTemperaments(rules, 'no-such-job')).toBeUndefined(); // unknown = reject
  });

  test('Talker test (§04): fear + resentment > bravery + loyalty', () => {
    const start = (t: keyof typeof rules.temperaments) => rules.temperaments[t].start;
    expect(isTalker(rules, start('mercenary'))).toBe(false);
    expect(isTalker(rules, start('fearful'))).toBe(true);
    expect(isTalker(rules, { fear: 1, resentment: 1, bravery: 2, loyalty: 0 })).toBe(false); // equal is not a Talker
    expect(isTalker(rules, { fear: 2, resentment: 1, bravery: 2, loyalty: 0 })).toBe(true);
  });

  test('df-for-the-suborned: every carried field has a reader; bands are words; knowledge and mix resolve', () => {
    const raw = read('contracts/people/people.rules.json');
    expect(Object.keys(rules.person.readBy.fields).sort()).toEqual([...rules.tiers.simulated.carries].sort());
    const unread = structuredClone(raw); delete unread.person.readBy.fields.ties;
    expect(PeopleRules.safeParse(unread).success).toBe(false); // a carried field no rule reads is rejected (Legion's bios)
    for (const s of Object.values(rules.scales)) expect(s.steps?.length).toBe(s.max + 1); // every scale value reads as a word
    expect(rules.knowledge.identity).toEqual(['face']);
    const badPos = structuredClone(raw); badPos.knowledge.needsKnown[0].position = 'no-such-post';
    expect(PeopleRules.safeParse(badPos).success).toBe(false);
    const impossible = structuredClone(raw); impossible.temperamentsBy.mix.rowMaxShare = 0.2;
    expect(PeopleRules.safeParse(impossible).success).toBe(false); // 0.2 × 4 temperaments < 1
    expect(Object.entries(rules.ties.kinds).filter(([, t]) => t.introduces).map(([k]) => k).sort()).toEqual(['family', 'regular', 'workmate']);
  });

  test('every proposed rule carries a reason; unknown keys are rejected', () => {
    const walk = (v: unknown, path: string): void => {
      if (!v || typeof v !== 'object') return;
      const o = v as Record<string, unknown>;
      if (o.proposed === true) expect(typeof o.reason, path).toBe('string');
      for (const [k, c] of Object.entries(o)) walk(c, `${path}.${k}`);
    };
    walk(rules, 'rules');
    const raw = read('contracts/people/people.rules.json');
    expect(PeopleRules.safeParse({ ...raw, extra: 1 }).success).toBe(false);
    const noReason = structuredClone(raw);
    delete noReason.temperaments.idealist.reason;
    expect(PeopleRules.safeParse(noReason).success).toBe(false);
  });
});
