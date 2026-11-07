// OV-1, no two buildings intersect (brief 075, Sky 2026-10-29). The live export passes; each failing example puts one intersection back.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { World } from '../../../contracts/world/world.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { ctxOf, generated } from '../../plan/world-ctx';
import { worldOf } from '../build';
import { OLD_BLOCK } from '../gen/oldblocks';
import { adopted } from '../gen/kitrefs';
import { handoverKept, overlapFaults, overlapRules, quadDepth } from './overlap';

setDefaultTimeout(180000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block)), base = generated(plan, d, civ);
const by = new Map(adopted().map(a => [a.id, a])), cat = (id: string) => by.get(id), kept = handoverKept(plan);
const ov1 = (w: World) => overlapRules(ctxOf(plan, d, civ, { w, gen: base.gen }))[0]!;
const outside = (w: World) => overlapFaults(w, cat).faults.filter(f => !(kept(f.hosts[0]) && kept(f.hosts[1])));

test('the live export: no two buildings intersect outside the Handover (OV-1 passes, 0 faults)', () => {
  const r = ov1(base.w);
  expect(r.ok, r.detail).toBe(true);
  expect(outside(base.w)).toEqual([]);
});

test('the Handover\'s protected buildings: their faults among themselves are counted in the value, never failed', () => {
  const all = overlapFaults(base.w, cat).faults;
  expect(all.length).toBeGreaterThan(0); // today's grammar's corner piers in the zone (rocket-west, lamplight-south-w/-e) stand into their neighbours
  expect(all.every(f => kept(f.hosts[0]) && kept(f.hosts[1]))).toBe(true);
  expect(ov1(base.w).rule).toMatch(/protected buildings among themselves [1-9]\d* /);
  expect(kept('bld/rocket-west/3')).toBe(true);
  expect(kept('bld/east-wall-s/2')).toBe(false);
  expect(kept('civic')).toBe(true);
});

test('separating axes: touching quads have no depth, a 0.5 m overlap is 0.5 deep', () => {
  const sq = (x: number, y: number, s = 2): [number, number][] => [[x, y], [x + s, y], [x + s, y + s], [x, y + s]];
  expect(quadDepth(sq(0, 0), sq(2, 0))).toBeCloseTo(0, 9);
  expect(quadDepth(sq(0, 0), sq(1.5, 0))).toBeCloseTo(0.5, 9);
  expect(quadDepth(sq(0, 0), sq(3, 0))).toBeLessThan(0);
});

// Two unprotected masses of the live export that touch along an east-west edge, n to the south of m (re-pinned 2026-11-07, brief 098: the pair was
// east-wall-s/2 and /3 until 097's audit re-laid that block; now it is the first such pair the export holds, e.g. east-wall-s/2 under /1)
const box = (b: World['buildings'][number]) => { const xs = b.ring.map(p => p[0]), ys = b.ring.map(p => p[1]); return { x0: Math.min(...xs), x1: Math.max(...xs), y0: Math.min(...ys), y1: Math.max(...ys) }; };
const touching = () => {
  const bodies = base.w.buildings.filter(b => b.id.endsWith('/body') && !kept(b.id.slice(0, -5)));
  for (const n of bodies) for (const m of bodies) {
    const a = box(n), c = box(m);
    if (n !== m && Math.abs(a.y1 - c.y0) < 1e-6 && Math.min(a.x1, c.x1) - Math.max(a.x0, c.x0) >= 4) return { n, m };
  }
  throw new Error('no two unprotected masses touch along an east-west edge');
};

test('failing example: a building mass pushed 2 m into its neighbour', () => {
  const { n, m } = touching(); // push m 2 m south, into n
  const w = { ...base.w, buildings: base.w.buildings.map(b => (b === m ? { ...b, ring: b.ring.map(p => [p[0], p[1] - 2] as [number, number]) } : b)) } as World;
  const r = ov1(w), f = outside(w).find(x => x.kind === 'mass-mass' && [x.a, x.b].includes(m.id));
  expect(r.ok).toBe(false);
  expect(f && [f.a, f.b].sort()).toEqual([n.id, m.id].sort());
  expect(f!.depth).toBeGreaterThan(1.5);
});

test('failing example: two buildings\' bays laid over each other', () => {
  const f = base.w.facades.find(x => x.building === 'bld/east-wall/2' && x.form === 'bay')!;
  const w = { ...base.w, facades: [...base.w.facades, { ...f, id: 'bld/east-wall/3/f99', building: 'bld/east-wall/3', kit: { ...f.kit, at: [f.kit.at[0], f.kit.at[1] + 1] as [number, number] } }] } as World;
  const r = ov1(w);
  expect(r.ok).toBe(false);
  expect(outside(w).some(x => x.kind === 'piece-piece' && x.a === f.id && x.b === 'bld/east-wall/3/f99')).toBe(true);
});

test('failing example: the old Bays rule put back (a taller segment\'s StreamEnd rounds off into its lower neighbour\'s wall; the curved end in the Skyport view)', () => {
  const was = OLD_BLOCK.intoWalls;
  (OLD_BLOCK as { intoWalls: boolean }).intoWalls = true;
  let w: World;
  try { w = worldOf(plan, d, civ).world; } finally { (OLD_BLOCK as { intoWalls: boolean }).intoWalls = was; }
  const bad = outside(w), ends = bad.filter(f => f.kind === 'piece-mass' && /StreamEnd/.test(w.facades.find(x => x.id === f.a)?.kit.id ?? ''));
  expect(ov1(w).ok).toBe(false);
  expect(ends.length).toBeGreaterThan(30);
  // a taller segment's end (4 storeys, 19.88 m) wraps its eyebrows 1.6 m into its lower neighbour's wall (3 storeys, 16.48 m), every one of them
  // (re-pinned 2026-11-07, brief 098: no longer one pinned pair; on the harbour side today east-wall-s/2 into /1 and /3 into /4)
  const height = (id: string) => w.buildings.find(b => b.id === `${id}/body`)?.height ?? NaN;
  for (const e of ends) { expect(e.depth).toBeCloseTo(1.6, 1); expect(height(e.hosts[0])).toBeGreaterThan(height(e.hosts[1])); }
  expect(ends.some(f => f.hosts[0].startsWith('bld/east-wall-s/'))).toBe(true);
});
