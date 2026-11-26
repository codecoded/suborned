import { expect, test } from 'bun:test';
import rulebook from './rulebook.json';
import { Rulebook } from './rulebook.schema.ts';

test('district payments use positive dollar amounts', () => {
  expect(Rulebook.parse(rulebook).campaign?.districtEconomy).toBeDefined();
  for (const change of [{ unit: 'count' }, { v: 0 }]) {
    const copy = structuredClone(rulebook);
    Object.assign(copy.values['district-help-usd'], change);
    expect(Rulebook.safeParse(copy).success).toBe(false);
  }
});
