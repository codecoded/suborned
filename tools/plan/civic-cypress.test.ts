// The ground cypresses are walk solids only where the plan sizes Cypress_Play (brief 055 step 5, Vantage; opt-in so that a pinned plan, people run r016's, reads as it was made).
import { expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { civic } from './civic';

const rd = (f: string) => JSON.parse(readFileSync(f, 'utf8'));
const plan = rd('contracts/world/harbour-district.plan.json'), block = rd(plan.civic.block);
const ground = (b: any) => (b.dressing.cypress.at as { id: string; on?: string }[]).filter(c => !c.on).map(c => c.id);

test('the live plan sizes Cypress_Play and walls every cypress on the ground', () => {
  expect(plan.civic.groundPieces.sizes.Cypress_Play).toBeDefined();
  const ids = new Set(civic(plan.civic, block).solidIds);
  expect(ground(block).length).toBeGreaterThan(0);
  for (const id of ground(block)) expect(ids.has(id)).toBe(true);
});

test('a plan that gives the cypress no size reads the block as it did: no cypress solid, no error', () => {
  const old = structuredClone(plan.civic); delete old.groundPieces.sizes.Cypress_Play;
  const ids = new Set(civic(old, block).solidIds);
  for (const id of ground(block)) expect(ids.has(id)).toBe(false);
});
