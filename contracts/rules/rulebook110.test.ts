// Brief 110 (engine 0.55.0, Keel; Sky 2026-11-11): every order acts at once. Listen needs no Watch first ("Scout can always listen and watch"); a Listen
// follows its person and holds nobody (cell.listen.follow, rounds.holds); one dial on the time of every act the cell orders (pace).
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { Rulebook } from './rulebook.schema.ts';

const raw = JSON.parse(readFileSync(new URL('./rulebook.json', import.meta.url), 'utf8'));
const rb = Rulebook.parse(raw);
const ev = (id: string) => rb.events.find((e: { id: string }) => e.id === id)!;

describe('brief 110: orders act at once', () => {
  test('Listen needs no routine known (no Watch first); Pay still needs it (its own need, §04)', () => {
    expect(ev('scout.listen').needs.some((n: any) => n.k === 'known')).toBe(false);
    expect(ev('scout.listen').src).toContain('Scout can always listen and watch');
    expect(ev('cell.watch').needs.some((n: any) => n.k === 'known')).toBe(false);
  });
  test('a Listen follows its person and holds nobody', () => {
    expect(raw.cell.listen.follow).toBe('v:listen-follow-cm');
    expect(raw.cell.listen.earshot).toBe(true); // engine 0.56.0 (Sky 2026-11-12): the person listened to stays in earshot
    expect(raw.cell.listen.rule).toContain('the scout (she) prefers a chair');
    expect(raw.rounds.holds).not.toContain('listen');
    expect(raw.rounds.holds).toEqual(['ask', 'pay', 'help', 'pressure']);
    expect(raw.values['listen-follow-cm'].v).toBeLessThan(raw.values['listen-reach-m'].v * 100);
  });
  test('the act-pace dial: one pct value, every act in seconds, none twice', () => {
    expect(rb.pace).toBeDefined();
    expect(raw.values['act-pace-pct'].unit).toBe('pct');
    for (const a of raw.pace.acts as string[]) if (a.startsWith('v:')) expect(raw.values[a.slice(2)]?.unit).toBe('s');
    for (const a of ['v:watch-s', 'v:listen-s', 'p:approaches.pay.timeS', 'v:loop-s', 'v:enter-s', 'v:take-s']) expect(raw.pace.acts).toContain(a);
    const bad = structuredClone(raw); bad.pace.acts.push('v:watch-s'); expect(Rulebook.safeParse(bad).success).toBe(false);
    const worse = structuredClone(raw); worse.pace.acts.push('v:listen-reach-m'); expect(Rulebook.safeParse(worse).success).toBe(false);
  });
  test('a seated operative is a customer (engine 0.57.0, Sky: "Yes")', () => {
    expect(raw.attention.seated.counts).toBe('customer');
    expect(raw.attention.seated.src).toContain('café customer');
    const bad = structuredClone(raw); bad.attention.seated.counts = 'loiterer'; expect(Rulebook.safeParse(bad).success).toBe(false);
  });
  test('the law for every order is in the rulebook', () => {
    expect(raw.cell.rule).toContain('Every order acts at once');
  });
});
