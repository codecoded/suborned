// Done-check for the seats file (Pulse, brief 022 follow-up): deterministic, view-only, one person to a chair, every chair faces its table, no engine file or hashed input carries it.
// Run: bun test tools/routines/seats.test.ts
import { describe, expect, test } from 'bun:test';
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { peopleFiles } from '../levels/people/hash';
import { CAFE_SET } from '../people/seats';
import { readInputs } from './rounds';
import { SEATS_FILE, build } from './seats';

const root = join(import.meta.dir, '..', '..');
const inp = readInputs(), { seats, text } = build(inp);

describe('the seats file (contracts/people/handover.seats.json)', () => {
  test('is deterministic and the committed file is what the inputs make', () => {
    expect(build(readInputs()).text).toBe(text);
    expect(existsSync(join(root, SEATS_FILE))).toBe(true);
    expect(readFileSync(join(root, SEATS_FILE), 'utf8')).toBe(text);
  });
  test('every table has the café set\'s chairs, each facing its table centre', () => {
    for (const [id, t] of Object.entries(seats.tables)) {
      expect(t.chairs.map((c) => c.chair)).toEqual(CAFE_SET.anglesDeg.map((_, i) => i));
      for (const c of t.chairs) {
        expect(Math.abs(Math.hypot(c.at[0] - t.at[0], c.at[1] - t.at[1]) - CAFE_SET.radius * 100)).toBeLessThan(8); // 72 cm round the table
        const want = Math.hypot(t.at[0] - c.at[0], t.at[1] - c.at[1]);
        expect(Math.abs(c.face[0] * want - (t.at[0] - c.at[0]) * 100)).toBeLessThan(want * 2.5); // within a couple of degrees
        expect(id).toMatch(/^cafe-table-\d+$/);
      }
    }
  });
  test('the seats are the engine\'s own seat places: a sit is at the point the walker is sent to', () => {
    for (const p of inp.people.people) for (const s of seats.seated[p.id] ?? []) {
      const stop = p.stops.find(([pl, f, t]) => pl === `${s.table}-seat-${s.chair}` && f === s.from && t === s.to);
      expect(stop).toBeDefined();
      expect(seats.tables[s.table]!.chairs[s.chair]!.at).toEqual(inp.people.places[`${s.table}-seat-${s.chair}`]!);
    }
  });
  test('one person to a chair at a time; the four cast roles are left to the engine', () => {
    const by = new Map<string, [number, number][]>(), cast = new Set(Object.values(inp.site.cast).map((c) => c.id));
    for (const [id, sits] of Object.entries(seats.seated)) {
      expect(cast.has(id)).toBe(false);
      for (const s of sits) { const k = `${s.table}/${s.chair}`; by.set(k, [...(by.get(k) ?? []), [s.from, s.to]]); }
    }
    for (const iv of by.values()) for (const [i, a] of iv.entries()) for (const b of iv.slice(i + 1)) expect(a[0] < b[1] && b[0] < a[1]).toBe(false);
    expect(Object.keys(seats.seated).length).toBeGreaterThan(0);
  });
  test('view only: not in sim/data, never read by the engine, not an engine input of the Unity bundle, not in the people hash', () => {
    expect(SEATS_FILE.startsWith('sim/data/')).toBe(false);
    expect(peopleFiles().some((f) => f.endsWith('handover.seats.json'))).toBe(false);
    // the engine: no source file of Suborned.Sim names it
    const engine = join(root, 'sim/Suborned.Sim');
    for (const f of readdirSync(engine).filter((f) => f.endsWith('.cs'))) expect(readFileSync(join(engine, f), 'utf8')).not.toContain('handover.seats');
    // the Unity bundle copies it beside the inputs for the host to draw (view only, unhashed), never as an engine input: not in INPUTS,
    // so neither the CLI, the scenarios nor engine.json's input hashes see it
    const unity = readFileSync(join(root, 'tools/sim/unity.ts'), 'utf8');
    const inputs = /const INPUTS = (\{[^}]*\})/.exec(unity)?.[1];
    expect(inputs).toBeDefined();
    expect(inputs!).not.toContain('seats');
  });
});
