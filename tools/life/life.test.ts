import { test, expect } from 'bun:test';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import plan from '../../contracts/world/harbour-district.plan.json';
import { plotArea } from './geom';

const run = (out: string) => Bun.spawnSync(['bun', join(import.meta.dir, 'cli.ts'), out]);
const gen = async (tag: string) => { const f = join(tmpdir(), `life-${tag}-${process.pid}.json`); expect(run(f).exitCode).toBe(0); return Bun.file(f).text(); };

test('life generator is deterministic: two runs, byte-identical', async () => {
  const [ta, tb] = [await gen('a'), await gen('b')];
  expect(ta.length).toBeGreaterThan(1000);
  expect(ta).toBe(tb);
});

test('the world carries no mission content (it lives in contracts/missions/<id>/)', async () => {
  const life = JSON.parse(await gen('m')), text = JSON.stringify(life);
  expect(life.plan.version).toBe(plan.version);
  expect(life.clock.anchors).toBeUndefined();
  expect(life.clock.mission.startsAt).toBeUndefined();
  expect(life.events.list.map((e: any) => e.id)).not.toContain('records-handover');
  expect(text).not.toMatch(/Vale|[Hh]andover|chime|§07/);
});

test('polygon plots count: every residential plot of the plan has residents', async () => {
  const life = JSON.parse(await gen('h')), homes = new Map<string, number>(life.generator.homes.map((h: any) => [h.plot, h.residents]));
  const poly = (plan.plots as any[]).filter(p => !p.rect && p.poly && p.use === 'residential');
  expect(poly.length).toBeGreaterThan(0);
  for (const p of poly) expect(homes.get(p.id) ?? 0).toBeGreaterThan(0);
  expect(plotArea({ poly: [[0, 0], [4, 0], [4, 3], [0, 3]] })).toBe(12);
});
