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
  for (const p of poly) expect(homes.get(p.id) ?? 0).toBeGreaterThan(0);
  // the remapped plan (brief 075) has no polygon homes: the rule is held on a polygon copy of each residential rect plot as well
  const { homesPop } = await import('./model');
  const rects = (plan.plots as any[]).filter(p => p.rect && p.use === 'residential');
  expect(rects.length).toBeGreaterThan(0);
  for (const p of rects) { const [x0, x1, y0, y1] = p.rect, { rect: _r, ...rest } = p; expect(homesPop({ ...rest, poly: [[x0, y0], [x1, y0], [x1, y1], [x0, y1]] })).toBe(homesPop(p)); expect(homesPop(p)).toBeGreaterThan(0); }
  expect(plotArea({ poly: [[0, 0], [4, 0], [4, 3], [0, 3]] })).toBe(12);
});

test('residents follow the play-street caps (brief 066): a capped plot houses its cap band mode, not the use band', async () => {
  const { homesPop, homeStoreys } = await import('./model');
  const B: any = (plan as any).standards.buildings, open = { ...B, caps: {} };
  const capped = (plan.plots as any[]).filter(p => ['residential', 'mixed'].includes(p.use) && B.caps?.[p.area]?.storeys);
  expect(capped.length).toBeGreaterThan(0);
  for (const p of capped) {
    const cap = B.caps[p.area], n = homeStoreys(p);
    expect(n).toBe(p.use === 'mixed' ? cap.mode - B.bands.mixed.podium : cap.mode);
    expect(homesPop(p)).toBeLessThan(homesPop(p, open));
  }
  const free = (plan.plots as any[]).filter(p => ['residential', 'mixed'].includes(p.use) && !B.caps?.[p.area]);
  for (const p of free) expect(homesPop(p)).toBe(homesPop(p, open));
  const life = JSON.parse(await gen('c')), byPlot = new Map<string, any>(life.generator.homes.map((h: any) => [h.plot, h]));
  for (const p of capped) expect(byPlot.get(p.id)?.residents ?? 0).toBe(homesPop(p));
});
