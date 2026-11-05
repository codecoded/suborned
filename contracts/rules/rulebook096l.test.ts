// Brief 096's lend round (engine 0.42.0, Keel): the live rulebook carries Motive's lend addendum (briefs/096/motive/lend.html, lend.json; Sky
// 2026-11-05 "build the lend now") and the schema holds it: `car.lend` (the switch, one seat, the police clock), the fight fixes on the 0.41.0
// shapes (rivals.block.brokenPace, F2; the wheelman's ring retired, F1) and the placing radius (security.unclaimed.scene, V2). The engine's
// behaviour is the 096-LEND-*, 096-FIX-* and 096-VET-* scenarios (sim ScenariosLend096*.cs).
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { PeopleRules } from '../people/people.rules.schema.ts';
import { Rulebook, num } from './rulebook.schema.ts';

const root = join(import.meta.dir, '..', '..');
const raw = JSON.parse(readFileSync(join(root, 'contracts/rules/rulebook.json'), 'utf8'));
const rb = Rulebook.parse(raw);
const people = PeopleRules.parse(JSON.parse(readFileSync(join(root, 'contracts/people/people.rules.json'), 'utf8')));
const v = (ref: string) => num(rb, people, ref);

describe('brief 096: the lend in the rulebook', () => {
  test('the live rulebook has every new key (optional in the schema only for pinned runs)', () => {
    expect(rb.car?.lend).toBeDefined(); expect(rb.security.unclaimed?.scene).toBeDefined(); expect(rb.rivals?.block?.brokenPace).toBeDefined();
  });
  test("Motive's values: the lend on, one seat, the police 75 s after vale.out (0.43.0), the scene 30 m, a broken man's run 280 cm/s", () => {
    const l = rb.car!.lend!;
    expect([v(l.split), v(l.seats)]).toEqual([1, 1]); expect(v(l.police)).toBe(75); // engine 0.43.0: 75 s (Motive's misses page L8; was 60)
    expect(v(rb.security.unclaimed!.scene!)).toBe(30);
    expect(v(rb.rivals!.block!.brokenPace!)).toBe(280);
    expect(l.refuse).toEqual({ board: 'board-lent-full', drive: 'drive-lent', gone: 'car-left' });
  });
  test('the switch is a rule value Sky can turn off: getaway-split 0 parses', () => {
    const off = structuredClone(raw); off.values['getaway-split'].v = 0;
    expect(Rulebook.safeParse(off).success).toBe(true);
  });
  test('the schema rejects what the engine does not run (unknown = reject)', () => {
    const bad = structuredClone(raw); bad.car.lend.extra = 1;
    expect(Rulebook.safeParse(bad).success).toBe(false);
    const bad2 = structuredClone(raw); bad2.car.lend.refuse.board = 'car-full';
    expect(Rulebook.safeParse(bad2).success).toBe(false);
  });
});
