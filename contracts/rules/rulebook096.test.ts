// Brief 096 (engine 0.40.0, Keel): the live rulebook carries the getaway rules of Motive's rules page (Sky 2026-11-04 "all. recommended"), and
// the schema holds them: bribes on camera (`camera`), box-in (`car.box`), the kerbside stop (`car.kerb`), the parked car (`attention.parked`).
// Their values are Motive's (briefs/096/motive/values.json); the engine's behaviour is the 31 scenarios (sim CAM-*, BOX-*, KERB-*, PARK-*).
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

describe('brief 096: the getaway rules in the rulebook', () => {
  test('the live rulebook has every section (optional in the schema only for pinned runs)', () => {
    expect(rb.camera).toBeDefined(); expect(rb.car?.box).toBeDefined(); expect(rb.car?.kerb).toBeDefined(); expect(rb.attention?.parked).toBeDefined();
  });
  test("bribes on camera are Motive's Q1 (a) and Q2 (a): Pay and Pressure; +1 T, Alert 1 at the desk's next scan + 3 s, no Standing, the face lead", () => {
    const c = rb.camera!;
    expect([...c.approaches].sort()).toEqual(['pay', 'pressure']);
    expect([v(c.trail), v(c.alert), v(c.report.scan), v(c.report.call), c.standing]).toEqual([1, 1, 20, 3, 0]);
    expect(c.lead).toContain('face:fixer');
    expect(rb.privacy!.rule).toMatch(/live camera records a Pay or a Pressure/);
  });
  test("box-in: the car's width plus 0.5 m a side, the engine's three refusals", () => {
    const b = rb.car!.box!;
    expect(v(rb.car!.footprint!.w) + 2 * v(b.side)).toBe(282);
    expect(b.refuse).toEqual({ go: 'car-boxed', drive: 'drive-boxed', into: 'stand-boxed' });
    expect(rb.car!.drive).not.toMatch(/never collide/);
  });
  test('the kerbside stop: 1 m clear all round, its edge 0.5 m off the kerb', () => {
    expect([v(rb.car!.kerb!.clear), v(rb.car!.kerb!.edge)]).toEqual([100, 50]);
    expect(raw.cell.driveCar.rule).toMatch(/pulls in at the kerb short of the drive/);
    expect(raw.cell.driveCar.rule).not.toMatch(/stops at the drive's mouth/);
  });
  test('the parked car: private ground is a drive, an official or a service bay; seen at 1.2 m over its centre', () => {
    const p = rb.attention!.parked!;
    expect([...p.ground].sort()).toEqual(['drive', 'official', 'service']);
    expect(v(p.sight)).toBe(120);
  });
  test('the schema rejects what the engine does not run (unknown = reject)', () => {
    const bad = structuredClone(raw); bad.camera.approaches = ['listen'];
    expect(Rulebook.safeParse(bad).success).toBe(false);
    const bad2 = structuredClone(raw); bad2.car.box.refuse.go = 'car-held';
    expect(Rulebook.safeParse(bad2).success).toBe(false);
  });
});
