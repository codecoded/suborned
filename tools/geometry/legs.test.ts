// G-1d, the leg rule (Sky 2026-10-24: "an invisible ledge", "the patrol goes through the camera pole"): on the live world it finds the legs it finds, and a leg laid through a table, a plinth or a camera pole fails it. Each failing example is a rounds file with one leg changed.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { mkdtempSync, readFileSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import type { World } from '../../contracts/world/world.schema';
import { auditGround, auditLegs, auditWalk, LEDGE, routes } from './audit';
import { loadPieces, ROOT } from './pieces';
import { loadSim } from './sim';
import { buildVisible, kitIdsIn } from './visible';
import { needs } from '../needs';

setDefaultTimeout(180000);
if (needs(['out/world/harbour-district.world.json', 'unity/Assets/Suborned/Kit'], 'bun run world; bun run kit')) {
  const world: World = JSON.parse(readFileSync(resolve(ROOT, 'out/world/harbour-district.world.json'), 'utf8')), sim = loadSim(world);
  const win = (() => { const c = [[-52, -66], [112, -66], [-52, 16], [112, 16]].map(b => sim.frame.toPlan(b as [number, number])); return { x0: Math.min(...c.map(p => p[0])), x1: Math.max(...c.map(p => p[0])), y0: Math.min(...c.map(p => p[1])), y1: Math.max(...c.map(p => p[1])) }; })();
  const vis = buildVisible(world, loadPieces(kitIdsIn(world, win)), win), ctx = { sim, vis };
  const rounds = JSON.parse(readFileSync(resolve(ROOT, 'sim/data/handover.rounds.json'), 'utf8'));
  const withRounds = (mutate: (r: typeof rounds) => void) => { const r = structuredClone(rounds); mutate(r); const f = join(mkdtempSync(join(tmpdir(), 'legs-')), 'rounds.json'); writeFileSync(f, JSON.stringify(r)); process.env.GEOMETRY_ROUNDS = f; try { return auditLegs(ctx, world, route); } finally { delete process.env.GEOMETRY_ROUNDS; } };
  const route = routes(ctx, auditGround(ctx), auditWalk(ctx)), base = auditLegs(ctx, world, route);

  test('the live world (Pulse\'s re-bake): Inez\'s rounds stand on the drawn floor, keep the body clear of the café\'s front and its tables, and nothing is walked on a ledge; the body is the plan\'s 0.65 m and a kerb stone is a step, not a ledge', () => {
    expect(base.body).toBe(0.65); expect(LEDGE).toBe(0.25);
    expect(base.stretches.filter(s => s.who.startsWith('Inez'))).toEqual([]);
    expect(base.stretches.some(s => s.cause === 'ledge' && s.rise < 0.25)).toBe(false);
    expect(base.stretches.some(s => s.cause === 'ledge' && /entrance/.test(s.owner))).toBe(false); // the Office landing and its steps are declared floors
  });
  test('a leg laid through a mission camera pole fails: the patrol sweep walked 0.2 m from the entrance pole', () => {
    const mission = JSON.parse(readFileSync(resolve(ROOT, 'contracts/missions/handover/mission.json'), 'utf8')), cam = (mission.cameras as { id: string; kind?: string; at: [number, number] }[]).find(c => (c.kind ?? 'pole-camera') === 'pole-camera')!;
    const r = withRounds(x => { x.servers['p-cafe-14'].spans[0].excursions[0].path = [[Math.round(cam.at[0] * 100) - 20, Math.round(cam.at[1] * 100)], [Math.round(cam.at[0] * 100) + 20, Math.round(cam.at[1] * 100)]]; });
    expect(r.stretches.some(s => s.cause === 'brush' && /^camera pole/.test(s.owner))).toBe(true);
  });
  test('a round leg laid through a table fails: Inez to table 2 by way of its set', () => {
    const r = withRounds(x => { const e = x.servers['p-cafe-14'].spans[0].excursions.find((q: { table: string }) => q.table === 'cafe-table-2'); e.path = [e.path[0], e.at, e.path[e.path.length - 1]]; });
    expect(r.stretches.filter(s => s.cause === 'brush' || s.cause === 'solid').length).toBeGreaterThanOrEqual(base.stretches.filter(s => s.cause === 'brush' || s.cause === 'solid').length);
  });
  test('a leg that stays on open pavement 3 m from everything passes: Inez standing at her post', () => {
    const r = withRounds(x => { for (const sp of x.servers['p-cafe-14'].spans) for (const e of sp.excursions ?? []) e.path = [[-1030, -2900], [-1030, -2900]]; });
    expect(r.stretches.filter(s => s.who.startsWith('Inez')).length).toBe(0);
  });
}
