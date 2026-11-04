// Brief 096's rivals round (engine 0.41.0, Keel): the live rulebook carries Motive's rivals page (Sky 2026-11-04 "all recommended") and the
// schema holds it: the block on arrival (`rivals.block`), the lookout (`rivals.lookout`), the temperaments and Pay (`rivals.turn`), the gang
// shooting (`security.unclaimed`), a blast near city people (`car.target.blastStanding`); the false tip and the driver's word retired. The
// engine's behaviour is the 096-RIV-* scenarios (sim ScenariosRivals096*.cs).
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

describe('brief 096: the rivals round in the rulebook', () => {
  test('the live rulebook has every section (optional in the schema only for pinned runs)', () => {
    expect(rb.rivals?.block).toBeDefined(); expect(rb.rivals?.lookout).toBeDefined(); expect(rb.rivals?.turn).toBeDefined();
    expect(rb.security.unclaimed).toBeDefined(); expect(rb.car?.target?.blastStanding).toBeDefined();
  });
  test("Motive's values: the block 10 m out, the misled man's 60 s, Pay $30,000 35 s +2 T, at most 60 % of one temperament (engine 0.42.0: the wheelman's 30 m ring retired, F1)", () => {
    const r = rb.rivals!;
    expect(r.block!.wheelmanNear).toBeUndefined(); expect(raw.values['rivals-wheelman-near-m']).toBeUndefined();
    expect([v(r.block!.standOff), v(r.block!.wheelmanOut), v(r.lookout!.follow), v(r.lookout!.misled)]).toEqual([10, 1, 280, 60]);
    expect([v(r.turn!.pay), v(r.turn!.s), v(r.turn!.trail), v(r.turn!.maxShare)]).toEqual([30000, 35, 2, 60]);
    expect([...r.turn!.kinds]).toEqual(['mercenary', 'loyal']);
  });
  test('the gang shooting holds the scene 60 s; a blast near city people is -1 Standing within 30 m', () => {
    expect(v(rb.security.unclaimed!.hold)).toBe(60);
    expect([v(rb.car!.target!.blastStanding!.v), v(rb.car!.target!.blastStanding!.near)]).toEqual([-1, 30]);
    expect(Object.keys(raw.standing.causes)).toContain('blast-near');
  });
  test('retired: the false tip (Motive Q2 a), the driver\'s word (F4), the park as the car\'s stop', () => {
    expect(raw.cell.mislead.away).toBeUndefined(); expect(raw.values['false-tip-s']).toBeUndefined();
    expect(raw.rivals.warn).toBeUndefined(); expect(raw.values['driver-warns-s']).toBeUndefined(); expect(raw.values['driver-warn-lag-s']).toBeUndefined();
    expect(raw.rivals.rule).toMatch(/Retired by brief 096/);
  });
  test('the schema rejects what the engine does not run (unknown = reject)', () => {
    const bad = structuredClone(raw); bad.rivals.turn.kinds = ['mercenary', 'idealist'];
    expect(Rulebook.safeParse(bad).success).toBe(false);
    const bad2 = structuredClone(raw); bad2.rivals.block.extra = 1;
    expect(Rulebook.safeParse(bad2).success).toBe(false);
  });
});
