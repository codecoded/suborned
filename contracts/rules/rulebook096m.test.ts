// Brief 096's misses round (engine 0.43.0, Keel): the live rulebook carries Motive's misses page (briefs/096/motive/misses.html, misses.json;
// Sky 2026-11-05 "all recommended") and the schema holds it: the police clock after vale.out at 75 s (L8), V3 (security.unclaimed.fired), G1
// (car.go.waits) and Pay's Watch first on the rival crew (rivals.turn.needs, Motive's rivals page). The engine's behaviour is the 096-VET-5/6,
// 096-LEND-13b/22, 096-FIX-5, 096-PAY-6 and 096-Q-* scenarios (sim ScenariosMisses096.cs).
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { PeopleRules } from '../people/people.rules.schema.ts';
import { Rulebook, num } from './rulebook.schema.ts';

const root = join(import.meta.dir, '..', '..');
const raw = JSON.parse(readFileSync(join(root, 'contracts/rules/rulebook.json'), 'utf8'));
const rb = Rulebook.parse(raw);
const people = PeopleRules.parse(JSON.parse(readFileSync(join(root, 'contracts/people/people.rules.json'), 'utf8')));

describe("brief 096: Motive's misses in the rulebook", () => {
  test('the live rulebook has every new key (optional in the schema only for pinned runs)', () => {
    expect(rb.car?.go?.waits).toBe('board');
    expect(rb.security.unclaimed?.fired).toBe('placed');
    expect(rb.rivals?.turn?.needs).toBe('watched');
  });
  test('the police close in 75 s after vale.out (L8, was 60)', () => {
    expect(num(rb, people, rb.car!.lend!.police)).toBe(75);
  });
  test('the switches parse either way (0.42.0 behaviour: always, none)', () => {
    const off = structuredClone(raw); off.security.unclaimed.fired = 'always'; off.car.go.waits = 'none'; off.rivals.turn.needs = 'none';
    expect(Rulebook.safeParse(off).success).toBe(true);
  });
  test('unknown = reject', () => {
    for (const edit of [(r: any) => { r.car.go.waits = 'later'; }, (r: any) => { r.car.go.extra = 1; }, (r: any) => { r.security.unclaimed.fired = 'seen'; }, (r: any) => { r.rivals.turn.needs = 'listened'; }]) {
      const bad = structuredClone(raw); edit(bad); expect(Rulebook.safeParse(bad).success).toBe(false);
    }
  });
});
