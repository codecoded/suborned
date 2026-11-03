// Brief 075 remap (rooftops): the old town's roof programmes on the live plan (roofline.ts RL-1..RL-4, ported from DecoKitRoofline.cs / DecoKitRoofThemes.cs).
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { worldOf } from '../build';
import { ctxOf, rng } from './common';
import { MAST_APART } from './roofs';
import { THEMES, cuts, pick } from './roofline';
import { zoneOf } from './street/capstone';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const { world: w, gen } = worldOf(plan, d, civ), zone = zoneOf(ctxOf(plan, d, civ));
const line = w.roofs.filter(r => r.programme === 'roofline');
const top = (r: { building: string }) => { const b = gen.blds.find(x => x.id === r.building)!.top; return [b.reduce((s, q) => s + q[0], 0) / b.length, b.reduce((s, q) => s + q[1], 0) / b.length]; }; // the building's top centroid, as roofs.ts decides
const inZone = (p: number[]) => p[0]! >= zone[0] && p[0]! <= zone[1] && p[1]! >= zone[2] && p[1]! <= zone[3];

test('RL-1: plots of 12-30 m at 6 m bay joints (the last plot keeps two bays), and the old cut rule reproduces it', () => {
  expect(line.length).toBeGreaterThan(100);
  for (const r of line) for (const p of r.plots!.slice(0, -1)) { const len = p.size[0] + 0.6; expect(len % 6 < 1e-3 || 6 - (len % 6) < 1e-3, `${p.id} ${len}`).toBe(true); expect(len).toBeGreaterThanOrEqual(12 - 1e-3); expect(len).toBeLessThanOrEqual(30 + 1e-3); }
  const c = cuts(0, 70, rng('x')); expect(c[0]).toBe(0); expect(c.at(-1)).toBe(70); for (let i = 1; i < c.length - 1; i++) expect([12, 18, 24, 30]).toContain(c[i]! - c[i - 1]!);
});
test('RL-2: never the same theme twice in a row; mostly pools, gardens, cafés and sun decks; a plot nothing fits takes planters', () => {
  const themes = line.flatMap(r => r.plots!.map(p => p.theme));
  for (const r of line) for (let i = 1; i < r.plots!.length; i++) if (r.plots![i]!.theme !== 'planters') expect(r.plots![i]!.theme, r.plots![i]!.id).not.toBe(r.plots![i - 1]!.theme);
  const people = themes.filter(t => ['lido', 'garden', 'cafe', 'sun', 'glass', 'tennis'].includes(t)).length;
  expect(people / themes.length).toBeGreaterThan(0.6);
  expect(pick(4, 4, null, rng('y'), () => true).name).toBe('planters');
  expect(pick(20, 13.4, THEMES[0]!, rng('z'), () => true).name).not.toBe('lido');
});
test('RL-3: the old items at true size, standing on the roof inside its rect', () => {
  for (const r of line) for (const k of r.kits!) { expect(k.scale).toEqual([1, 1, 1]); expect(k.z).toBeGreaterThanOrEqual(r.z); expect(k.z).toBeLessThanOrEqual(r.z + 0.2); }
  const ids = new Set(line.flatMap(r => r.kits!.map(k => k.id)));
  for (const id of ['Roof_LapPool', 'Roof_Cabana', 'CafeSet_Tulip', 'Deckchair', 'Roof_Conservatory']) expect(ids.has(id), id).toBe(true);
});
test('RL-4: no lattice mast off the Handover zone; a few slender Roof_Masts, only on 4-storey corners, apart', () => {
  const masts = line.flatMap(r => r.kits!.filter(k => /Mast/.test(k.id)).map(k => ({ r, k })));
  expect(masts.every(m => m.k.id === 'Roof_Mast')).toBe(true);
  expect(masts.length).toBeLessThanOrEqual(10);
  for (const m of masts) expect(m.r.plots!.some(p => p.theme === 'mast'), m.r.id).toBe(true);
  for (const a of masts) for (const b of masts) if (a !== b) expect(Math.hypot(a.k.at[0] - b.k.at[0], a.k.at[1] - b.k.at[1])).toBeGreaterThanOrEqual(MAST_APART - 10);
  for (const r of w.roofs) if (r.programme === 'antenna-mast' && !r.crown) expect(inZone(top(r)), r.id).toBe(true);
});
test('the Handover zone keeps its roofs (no roofline roof stands in it)', () => {
  for (const r of line) expect(inZone(top(r)), r.id).toBe(false);
});
