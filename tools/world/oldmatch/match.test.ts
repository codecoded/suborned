// The plan-match check (brief 075 remap step 2). Our own world written in the export format matches itself with 0 mismatches (in our frame and in the old
// DecoKit frame); each failing example puts one difference back and must be reported, in the zone as a hard failure, outside it as a plan-patch op.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { OldBuilt } from '../../../contracts/world/oldbuilt.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { generated } from '../../plan/world-ctx';
import { MATCH, builtOf, builtOverlap, builtSide, compare, patchOf, worldSide } from './match';

setDefaultTimeout(180000);
type V = [number, number];
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block)), { w } = generated(plan, d, civ);
const P = plan as any, ours = worldSide(w, P), self = () => structuredClone(builtOf(w, P));
const run = (b: OldBuilt) => { const r = compare(builtSide(b), ours, P, builtOverlap(b)); return { r, patch: patchOf(r, P, b, 'built.json', 'plan.json') }; };
const shift = (r: V[], dx: number, dy = 0): V[] => r.map(p => [p[0] + dx, p[1] + dy]);
const zone = P.standards.blocks.zone as [number, number, number, number];

test('identity: our world as suborned.oldbuilt/1 matches itself (0 mismatches, 0 overlaps), in our frame and in the old DecoKit frame (offset 0, -206)', () => {
  const a = run(self());
  expect(a.r.mismatches).toEqual([]);
  expect(a.r.overlap.faults).toEqual([]);
  expect(a.patch.ops).toEqual([]);
  const b = run(structuredClone(builtOf(w, P, [0, -206])));
  expect(b.r.mismatches).toEqual([]);
});

test('footprints: a building moved 1 m outside the zone is reported both ways and proposed as a block rect for Datum; 0.3 m is within tolerance', () => {
  const host = 'bld/east-wall-s/3';
  const near = self(); for (const x of near.buildings) if ((x.building ?? '') === host) x.ring = shift(x.ring, 0, 0.3);
  expect(run(near).r.mismatches.filter(m => m.kind === 'footprint')).toEqual([]);
  const b = self(); for (const x of b.buildings) if ((x.building ?? '') === host) x.ring = shift(x.ring, 0, -1);
  const { r, patch } = run(b), fp = r.mismatches.filter(m => m.kind === 'footprint');
  expect(fp.some(m => m.side === 'built-only')).toBe(true);
  expect(fp.some(m => m.side === 'plan-only')).toBe(true);
  expect(fp.every(m => !m.zone)).toBe(true);
  expect(Math.max(...fp.map(m => m.dev ?? 0))).toBeCloseTo(1, 1);
  const op = patch.ops.find(o => o.path === 'blocks[id=east-wall-s].rect');
  expect(op?.op).toBe('set');
  expect(patch.zoneFailures).toEqual([]);
});

test('the Handover zone: the Records Office 0.8 m off is a hard failure, listed, never patched', () => {
  const b = self(); for (const x of b.buildings) if (x.id.startsWith('civic/civic-office')) x.ring = shift(x.ring, 0.8);
  const { r, patch } = run(b), z = r.mismatches.filter(m => m.zone);
  expect(z.length).toBeGreaterThan(0);
  expect(z.every(m => m.at[0] >= zone[0] && m.at[0] <= zone[1] && m.at[1] >= zone[2] && m.at[1] <= zone[3] || m.from && m.to)).toBe(true);
  expect(patch.zoneFailures.length).toBe(r.inZone);
  expect(patch.ops.flatMap(o => o.evidence).some(id => z.some(m => m.id === id))).toBe(false);
});

test('levels are exact: a deck level 9.5 against the plan\'s 9.43 fails and goes to the builder, not the plan patch', () => {
  const b = self(); b.levels = b.levels.map(l => (l.id === 'deck' ? { ...l, z: 9.5 } : l));
  const { r, patch } = run(b), m = r.mismatches.find(x => x.kind === 'level');
  expect(m?.side).toBe('differ');
  expect(m?.dev).toBeCloseTo(0.07, 3);
  expect(patch.builderFixes.some(o => o.owner === 'latch' && o.path === 'deck')).toBe(true);
  expect(patch.ops.some(o => o.path.includes('levels'))).toBe(false);
  const c = self(); c.levels = c.levels.filter(l => l.id !== 'concourse');
  expect(run(c).r.mismatches.some(x => x.kind === 'level' && x.side === 'plan-only' && x.el === 'concourse')).toBe(true);
});

test('entrances: a door moved 1.5 m along its wall outside the zone is reported both ways; 0.8 m is within tolerance', () => {
  const pick = (b: OldBuilt) => b.entrances.find(e => e.building === 'bld/east-wall-s/3')!;
  const ok = self(); { const e = pick(ok); e.at = [e.at[0], e.at[1] + 0.8]; }
  expect(run(ok).r.mismatches).toEqual([]);
  const b = self(), e = pick(b); e.at = [e.at[0], e.at[1] + 1.5];
  const ms = run(b).r.mismatches.filter(m => m.kind === 'entrance');
  expect(ms.map(m => m.side).sort()).toEqual(['built-only', 'plan-only']);
  expect(ms.every(m => !m.zone)).toBe(true);
});

test('kerbs: a road built 1.2 m wider is reported on both kerbs and proposed as a road review with the built width', () => {
  const rd = w.ground.roads.find(r => !r.line.some(p => p[0] >= zone[0] && p[0] <= zone[1] && p[1] >= zone[2] && p[1] <= zone[3]))!;
  const b = self(); b.roads = b.roads.map(r => (r.id === rd.id ? { ...r, width: r.width + 1.2 } : r));
  const { r, patch } = run(b), k = r.mismatches.filter(m => m.kind === 'kerb' && m.side === 'built-only');
  expect(k.length).toBeGreaterThan(0);
  expect(Math.max(...k.map(m => m.dev ?? 0))).toBeCloseTo(0.6, 1);
  const op = patch.ops.find(o => o.path === `roads[id=${rd.id.replace(/^road\//, '')}]`);
  expect((op?.to as { w: number }).w).toBeCloseTo(rd.width + 1.2, 3);
});

test('a building missing from the old build is plan-only; one on no plan block or plot is proposed as a new plot', () => {
  const b = self(); b.buildings = b.buildings.filter(x => x.building !== 'bld/east-wall-s/3');
  const gone = run(b);
  expect(gone.r.mismatches.some(m => m.kind === 'footprint' && m.side === 'plan-only' && m.el.startsWith('bld/east-wall-s/3'))).toBe(true);
  const c = self(); c.buildings.push({ id: 'old/kiosk', ring: [[600, 600], [610, 600], [610, 610], [600, 610]], base: 0, height: 6, kind: 'other', origin: 'old' });
  const add = run(c).patch.ops.find(o => o.op === 'add');
  expect((add?.to as { from: string }).from).toBe('old/kiosk');
});

test('pavement tops are exact: a slab built at 0.20 where ours is 0.18 fails', () => {
  const b = self(), p = b.pavements.find(x => x.id.startsWith('plot/east-wall-s'))!; p.top = 0.2;
  const m = run(b).r.mismatches.find(x => x.kind === 'pavement-top');
  expect(m?.dev).toBeCloseTo(0.02, 3);
});

test('OV-1 on the old-built footprints: two buildings laid 2 m into each other fail; the identity passes', () => {
  const b = self(), a = b.buildings.find(x => x.building === 'bld/east-wall-s/3')!;
  b.buildings.push({ ...a, id: 'old/intruder', building: 'old/intruder', ring: shift(a.ring, 0, -2) });
  const f = builtOverlap(b).faults;
  expect(f.some(x => x.a === 'old/intruder' || x.b === 'old/intruder')).toBe(true);
  expect(builtOverlap(self()).faults).toEqual([]);
  expect(MATCH.footprint).toBe(0.5);
});
