// Brief 098, law L9 Bodies (engine 0.46.0, Keel): the live rulebook carries `bodies` and the schema holds it. The engine's behaviour is the law's
// tests (sim/Suborned.Sim.Tests/Bodies.cs): no two bodies on their feet overlap at any tick, across seeds and the dial's settings.
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

describe('brief 098: law L9, Bodies, in the rulebook', () => {
  test('the live rulebook has the law (optional in the schema only for pinned runs), on', () => {
    expect(rb.bodies).toBeDefined(); expect(v(rb.bodies!.on)).toBe(1);
  });
  test("Keel's proposed values: a 60 cm body, 80 cm at rest, a 70 cm queue, 2.5 m between armed people, back at 0.6 m/s, a shove of 4 m/s, a 2 m search", () => {
    const b = rb.bodies!;
    expect([v(b.radius), v(b.space), v(b.queue), v(b.crew)]).toEqual([30, 80, 70, 250]);
    expect([v(b.back), v(b.shove), v(b.search)]).toEqual([60, 400, 200]);
    for (const k of ['body-radius-cm', 'body-space-cm', 'body-queue-cm', 'body-crew-cm', 'body-back-cm-s', 'body-shove-cm-s', 'body-search-cm']) expect(raw.values[k].proposed).toBe(true);
  });
  test('the fight agrees with the law: fight.spacing.clear is two radii', () => {
    expect(v(rb.fight.spacing!.clear)).toBe(2 * v(rb.bodies!.radius));
  });
  test('personal space and a queue are never inside two bodies; an armed crew spreads at least as far as personal space', () => {
    const b = rb.bodies!; const two = 2 * v(b.radius);
    expect(v(b.space)).toBeGreaterThanOrEqual(two); expect(v(b.queue)).toBeGreaterThanOrEqual(two); expect(v(b.crew)).toBeGreaterThanOrEqual(v(b.space));
  });
  test('the switch is a rule value: bodies-on 0 parses (a comparison build)', () => {
    const off = structuredClone(raw); off.values['bodies-on'].v = 0;
    expect(Rulebook.safeParse(off).success).toBe(true);
  });
  test('the schema rejects what the engine does not run (unknown = reject), and a proposed rule needs its reason', () => {
    const bad = structuredClone(raw); bad.bodies.extra = 'v:body-radius-cm';
    expect(Rulebook.safeParse(bad).success).toBe(false);
    const bad2 = structuredClone(raw); delete bad2.bodies.reason;
    expect(Rulebook.safeParse(bad2).success).toBe(false);
  });
});
