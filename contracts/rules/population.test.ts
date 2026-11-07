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
  test('brief 098 step 1: the drivers are a people.rules role, every nature they can have ducks or flees, and the lanes file is the engine\'s', () => {
    const r = PopulationRules.parse(raw), allow: string[] = people.temperamentsBy.roles[r.traffic.driver.role]?.allow ?? [];
    expect(allow.length).toBeGreaterThan(1);
    for (const t of allow) expect(r.traffic.react.duck.includes(t as never) !== r.traffic.react.flee.includes(t as never)).toBe(true);
    expect(r.traffic.lanes).toBe('sim/data/handover.lanes.json'); expect(r.kinds.cars.max).toBeGreaterThan(0);
  });
  test("brief 098 step 1b: the lockdown's leave is Sky's decision (no longer proposed), and a wreck is shoved, never held for", () => {
    const r = PopulationRules.parse(raw);
    expect(r.traffic.lockdown.then).toBe('leave'); expect(r.traffic.lockdown.proposed).toBeUndefined(); expect(r.traffic.lockdown.src).toContain('Drive out');
    expect(r.traffic.shove.cmS).toBeGreaterThan(0); expect('holdS' in r.traffic.yield).toBe(false);
  });
  test('unknown = reject', () => {
    for (const edit of [(r: any) => { r.kinds.cars = r.kinds.guards; }, (r: any) => { r.beat.points.push('gate'); }, (r: any) => { r.passers.dest.bench = 1; },
      (r: any) => { r.kinds.people.of = ['clerk']; }, (r: any) => { r.kinds.guards.id = 'p-guard-'; }, (r: any) => { r.passers.lingerS.seat = [90, 30]; },
      (r: any) => { r.traffic.mix.lorry = 5; }, (r: any) => { r.traffic.react.duck.push('mercenary'); }, (r: any) => { r.traffic.lockdown.then = 'vanish'; }, (r: any) => { r.kinds.cars.id = 'car:t-'; },
      (r: any) => { r.traffic.yield.holdS = 40; }, (r: any) => { r.traffic.shove.cmS = 0; }, (r: any) => { delete r.traffic.shove; }]) {
      const bad = structuredClone(raw); edit(bad); expect(PopulationRules.safeParse(bad).success).toBe(false);
    }
  });
});
