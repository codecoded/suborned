// Brief 075, the old frontage port (oldblocks.ts) on the plan's blocks: true-size pieces, the plan's caps honoured, one building per segment, the remap's storeys,
// and the failing examples (a run off the grammar falls back; a plan without blocks gets none of it).
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { worldOf } from '../build';
import { ctxOf } from './common';
import { OLD_BLOCK, type PlanBlock, copingOf, oldLogOf, planBlocks, quadHitsRect, wallSegs } from './oldblocks';
import { rng } from './common';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const blocks = ((plan as any).blocks ?? []) as PlanBlock[];
const { world: w, gen } = worldOf(plan, d, civ);
const log = oldLogOf(ctxOf(plan, d, civ))!;
const laid = new Set(blocks.map(b => b.plot));
const oldB = gen.blds.filter(b => (b as any).old);

test('every plot the plan lays as blocks takes the old runs, none falls back', () => {
  expect(laid.size).toBeGreaterThan(10);
  expect(log.plots.filter(p => laid.has(p.plot) && p.fit !== 'blocks')).toEqual([]);
  expect(new Set(oldB.map(b => b.plot))).toEqual(laid);
});

test('old pieces stand at true size: scale 1, no squash, no pier', () => {
  const ids = new Set(oldB.map(b => b.id)), mine = w.facades.filter(f => ids.has(f.building) && /\/f\d+$/.test(f.id));
  expect(mine.length).toBeGreaterThan(1000);
  expect(mine.filter(f => f.kit.scale.some(s => Math.abs(s - 1) > 1e-9))).toEqual([]);
  expect(mine.filter(f => f.form === 'pier')).toEqual([]);
});

test('one corner piece per corner cell the plan names (placed once, by the longer run)', () => {
  const cells = new Set(blocks.flatMap(b => b.corners.map(c => `${c[0]},${c[1]}`)));
  expect(log.counts.corners).toBe(cells.size);
  const corners = w.facades.filter(f => f.form === 'corner' && oldB.some(b => b.id === f.building));
  expect(new Set(corners.map(f => `${f.b[0]},${f.b[1]}`)).size).toBe(corners.length);
});

test('every end cap the plan names rounds off: the tail end natural, the head end its mirrored twin', () => {
  const ends = w.facades.filter(f => f.form === 'end' && oldB.some(b => b.id === f.building));
  const named = blocks.reduce((n, b) => n + b.runs.reduce((m, r) => m + r.caps.filter(c => c === 'end').length, 0), 0);
  expect(ends.length).toBeGreaterThanOrEqual(named);
  expect(ends.filter(f => f.kit.id.endsWith(OLD_BLOCK.mirrorSuffix)).length).toBeGreaterThan(0);
  expect(ends.filter(f => !f.kit.id.endsWith(OLD_BLOCK.mirrorSuffix)).length).toBeGreaterThan(0);
});

test('heights follow the remap: 3 storeys, 4 on corner buildings, each at its class coping', () => {
  for (const b of oldB) {
    const o = (b as any).old as { code: number; top: number };
    expect(o.code === 3 || o.code === 4).toBe(true);
    expect(Math.abs(b.z - 0.18 - copingOf(o.code))).toBeLessThan(0.01);
  }
  const corners = new Set(w.facades.filter(f => f.form === 'corner').map(f => f.building));
  expect(oldB.filter(b => (b as any).old.code === 4 && !corners.has(b.id))).toEqual([]);
});

test('the circus crescents stand: chord bays facing the centre', () => {
  const cres = ((plan as any).circus?.crescents ?? []) as { bays: number }[];
  expect(log.counts.crescents).toBe(cres.length);
  expect(log.counts.crescentBays).toBe(cres.reduce((n, c) => n + c.bays, 0));
});

test('a wall never draws the same class twice in a row, and a corner segment draws a class with its own corner', () => {
  for (let s = 0; s < 40; s++) {
    const segs = wallSegs(rng(`t${s}`), s % 2 ? 'street' : 'terrace', 4 + s, ['S3', 'S3W', 'P3', 'B3', 'A3'], true, true);
    expect(segs.reduce((n, x) => n + x[1], 0)).toBe(4 + s);
    for (let i = 1; i < segs.length; i++) expect(segs[i]![0]).not.toBe(segs[i - 1]![0]);
    expect(segs[0]![0]).not.toBe('A3'); expect(segs[segs.length - 1]![0]).not.toBe('A3');
  }
});

test('failing example: a run whose length is not its caps + 6 k falls back to today\'s grammar, with why', () => {
  const p2: Plan = JSON.parse(JSON.stringify(plan)), b = ((p2 as any).blocks as PlanBlock[])[0]!;
  b.runs[0]!.bays += 1; // the run now asks for 6 m more than it has
  const r = planBlocks(ctxOf(p2, derive(p2), civ), d.plots.find(p => p.id === b.plot)!);
  expect(r?.log.fit).toBe('fallback');
  expect(r?.log.why).toContain('BG-2');
});

test('failing example: a plan without blocks (a pinned plan) gets no old frontage', () => {
  const p2: Plan = JSON.parse(JSON.stringify(plan)); delete (p2 as any).blocks;
  expect(planBlocks(ctxOf(p2, derive(p2), civ), d.plots.find(p => laid.has(p.id))!)).toBeNull();
});

test('quad and rect overlap test (the crescent bodies against a block band)', () => {
  expect(quadHitsRect([[0, 0], [2, 0], [2, 2], [0, 2]], [1, 3, 1, 3])).toBe(true);
  expect(quadHitsRect([[0, 0], [2, 0], [2, 2], [0, 2]], [2, 3, 0, 2])).toBe(false); // touching only
});
