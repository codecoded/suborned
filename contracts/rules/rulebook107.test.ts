// Brief 107 (engine 0.54.0, Keel; Sky 2026-11-11: "Yes to all three"): the weather and light dial in the live rulebook. A look only: no rule reads
// it. The draw is the rulebook's streams draw on its own key ({seed}/look/{stage}); the engine's twin is sim GameLook.cs (LookRules.Draw), held to
// the same kinds by sim/Suborned.Sim.Tests/Look107.cs, which also proves a start without the look plays and hashes as before.
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { PeopleRules } from '../people/people.rules.schema.ts';
import { drawWeighted } from './rulebook.ref.ts';
import { Rulebook, num } from './rulebook.schema.ts';
import { LIGHTS, WEATHERS } from './rulebook.schema107.ts';

const root = join(import.meta.dir, '..', '..');
const raw = JSON.parse(readFileSync(join(root, 'contracts/rules/rulebook.json'), 'utf8'));
const rb = Rulebook.parse(raw);
const people = PeopleRules.parse(JSON.parse(readFileSync(join(root, 'contracts/people/people.rules.json'), 'utf8')));
const v = (ref: string) => num(rb, people, ref);
const odds = <K extends string>(kinds: readonly K[], refs: Record<K, string>) => kinds.map((k) => ({ item: k, weight: v(refs[k]) }));
/** The engine's draw (LookRules.Draw): seed 0 is the dial's first kind; else the streams draw on {seed}/look/{stage}. */
const draw = <K extends string>(seed: number, stage: string, kinds: readonly K[], refs: Record<K, string>): K =>
  seed === 0 ? kinds[0]! : drawWeighted(rb.streams.keys.look!.replace('{seed}', String(seed)).replace('{stage}', stage), odds(kinds, refs));

describe('brief 107: the look in the rulebook', () => {
  test('the live rulebook has the dial (optional in the schema only for pinned runs), every kind named, read by no rule', () => {
    expect(rb.look).toBeDefined();
    expect(Object.keys(rb.look!.weather)).toEqual([...WEATHERS]); expect(Object.keys(rb.look!.light)).toEqual([...LIGHTS]);
    expect(rb.look!.readBy).toEqual([]);
    expect(rb.look!.rule).toMatch(/LOOK ONLY/); expect(rb.look!.rule).toMatch(/no rule reads/);
  });
  test("Keel's starting odds, proposed: clear 55, overcast 20, rain 15, mist 10; day 75, night 25 (each sums to 100)", () => {
    expect(WEATHERS.map((k) => v(rb.look!.weather[k]))).toEqual([55, 20, 15, 10]);
    expect(LIGHTS.map((k) => v(rb.look!.light[k]))).toEqual([75, 25]);
    for (const k of ['look-weather-clear', 'look-weather-overcast', 'look-weather-rain', 'look-weather-mist', 'look-light-day', 'look-light-night']) expect(raw.values[k].proposed).toBe(true);
  });
  test('its own stream: the key is {seed}/look/{stage}, apart from every start draw ({seed}/{id}/{stage}) by its id', () => {
    expect(rb.streams.keys.look).toBe('{seed}/look/{stage}');
    expect(rb.streams.keys.start).toBe('{seed}/{id}/{stage}');
  });
  test('seed 0 is the neutral start (clear, by day); the draw lands in the dial\'s bands across 20,000 seeds (laws, not one game)', () => {
    expect(draw(0, 'weather', WEATHERS, rb.look!.weather)).toBe('clear'); expect(draw(0, 'light', LIGHTS, rb.look!.light)).toBe('day');
    const n = 20000, w = new Map<string, number>(), l = new Map<string, number>();
    for (let s = 1; s <= n; s++) { const a = draw(s, 'weather', WEATHERS, rb.look!.weather), b = draw(s, 'light', LIGHTS, rb.look!.light); w.set(a, (w.get(a) ?? 0) + 1); l.set(b, (l.get(b) ?? 0) + 1); }
    for (const k of WEATHERS) expect(Math.abs((w.get(k) ?? 0) / n * 100 - v(rb.look!.weather[k]))).toBeLessThan(1.5);
    for (const k of LIGHTS) expect(Math.abs((l.get(k) ?? 0) / n * 100 - v(rb.look!.light[k]))).toBeLessThan(1.5);
  });
  test('the twin vectors the engine holds to (sim Look107.cs): seeds 1..8', () => {
    const got = [1, 2, 3, 4, 5, 6, 7, 8].map((s) => `${draw(s, 'weather', WEATHERS, rb.look!.weather)}/${draw(s, 'light', LIGHTS, rb.look!.light)}`);
    expect(got).toEqual(TWIN);
  });
  test('the schema rejects what the engine does not run (unknown = reject): another kind, a missing kind, a reader, no reason', () => {
    const bad = structuredClone(raw); bad.look.weather.snow = 'v:look-weather-mist'; expect(Rulebook.safeParse(bad).success).toBe(false);
    const bad2 = structuredClone(raw); delete bad2.look.light.night; expect(Rulebook.safeParse(bad2).success).toBe(false);
    const bad3 = structuredClone(raw); bad3.look.readBy = ['perception.sight']; expect(Rulebook.safeParse(bad3).success).toBe(false);
    const bad4 = structuredClone(raw); delete bad4.look.reason; expect(Rulebook.safeParse(bad4).success).toBe(false);
  });
});

const TWIN = ['overcast/night', 'clear/day', 'rain/night', 'mist/night', 'clear/day', 'overcast/day', 'rain/day', 'overcast/night'];
