// Brief 098 step 0 (Keel): the dial's rules parse, name roles the people rules have, and reject the unknown. The engine's behaviour under
// them is sim/Suborned.Sim.Tests/Dial.cs (dotnet test) and `suborned-sim dial sweep`.
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { PopulationRules } from './population.schema.ts';

const root = join(import.meta.dir, '..', '..');
const raw = JSON.parse(readFileSync(join(root, 'contracts/rules/population.json'), 'utf8'));
const people = JSON.parse(readFileSync(join(root, 'contracts/people/people.rules.json'), 'utf8'));

describe('brief 098: the dial', () => {
  test('the live rules parse', () => { expect(PopulationRules.safeParse(raw).success).toBe(true); });
  test("each kind's role is a people.rules temperamentsBy role, and guards count the people file's guards", () => {
    const r = PopulationRules.parse(raw);
    for (const k of [r.kinds.guards, r.kinds.people]) expect(Object.keys(people.temperamentsBy.roles)).toContain(k.role);
    expect(r.kinds.guards.of).toEqual(['guard']); expect(r.kinds.people.of).toBe('rest');
  });
  test('unknown = reject', () => {
    for (const edit of [(r: any) => { r.kinds.cars = r.kinds.guards; }, (r: any) => { r.beat.points.push('gate'); }, (r: any) => { r.passers.dest.bench = 1; },
      (r: any) => { r.kinds.people.of = ['clerk']; }, (r: any) => { r.kinds.guards.id = 'p-guard-'; }, (r: any) => { r.passers.lingerS.seat = [90, 30]; }]) {
      const bad = structuredClone(raw); edit(bad); expect(PopulationRules.safeParse(bad).success).toBe(false);
    }
  });
});
