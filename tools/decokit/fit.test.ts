// The generated pieces in kit.json that carry an order fit their sites: one per plan regen order, current hash, adopted, source generated, footprint and tip inside the order.
import { expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Kit } from '../../contracts/world/kit.schema';
import { NARROW, fitRow, printTable, type Built } from './fit';
import { readOrders } from './orders';

const kit: Kit = JSON.parse(readFileSync(resolve(import.meta.dir, '../../contracts/world/kit.json'), 'utf8'));
const orders = readOrders(), gen = kit.assets.filter(a => a.source === 'generated' && a.order); // pieces built to a plan order; generated pieces without one (crowns, horizon, civic, stations) have no site to fit

test('every regen order in the plan has a generated, adopted piece in kit.json with its current hash', () => {
  expect(gen.map(a => a.id).sort()).toEqual(orders.pieces.map(p => p.id).sort());
  for (const a of gen) {
    expect(a.status).toBe('adopted');
    expect(a.order!.hash).toBe(orders.pieces.find(p => p.id === a.id)!.hash);
    const fbx = a.files.find(f => f.role === 'fbx')!;
    expect(fbx.root).toBe('generated'); expect(fbx.sha256).toMatch(/^[0-9a-f]{64}$/);
  }
});

test('fit table: each piece fits its site (footprint, setback, crown, tip ≤ the order and crownTip)', () => {
  const rows = gen.map(a => fitRow(orders, a.id, a.order!, a.built!));
  console.log(printTable(rows));
  for (const r of rows) expect(r.why).toEqual([]);
  expect(rows.every(r => r.fits)).toBe(true);
});

// No narrower than the plan (15 Oct): the measured widths of the crescent-tower, crescent-terrace and lantern-beacon builds as they were (the upper tiers inset by the terrace
// parapet of the tier below: 12 and 6 m for the plan's 18 and 12) fail; the fixed builds (each tier's envelope the plan's width) pass; a missing measure fails.
const row = (id: string) => gen.find(a => a.id === id)!;
const asBuilt = (id: string, tiers: [number, number][] | null | undefined): Built => ({ ...row(id).built!, tiers });
const planned = (id: string) => (orders.pieces.find(p => p.id === id)!.regen as { tiers: [number, number, number][] }).tiers.map(([w]) => [w, w] as [number, number]);
test('a tier narrower than the plan fails the fit, by the tolerance stated', () => {
  expect(NARROW).toBe(0.1);
  const old: Record<string, [number, number][]> = {
    'Tower_Atom@crescent-tower': [[24, 24], [12, 12], [6, 6]],
    'Tower_Lantern@lantern-beacon': [[16, 16], [9.5, 9.5], [9.6, 9.6]],
    'Tower_Terrace@crescent-terrace': [[24, 22], [15.34, 13.64], [10.54, 8.54], [7.04, 5.54], [4.54, 4.54]],
  };
  for (const [id, tiers] of Object.entries(old)) {
    const r = fitRow(orders, id, row(id).order!, asBuilt(id, tiers));
    expect(r.fits, id).toBe(false);
    expect(r.why.some(m => m.includes('no narrower than')), id).toBe(true);
  }
});

test('a tier as wide as the plan passes, one short by the tolerance passes, one past it fails, and no measure fails', () => {
  for (const id of ['Tower_Atom@crescent-tower', 'Tower_Lantern@lantern-beacon', 'Tower_Terrace@crescent-terrace']) {
    const want = planned(id), base = row(id).built!;
    const pass = (tiers: [number, number][] | null | undefined) => fitRow(orders, id, row(id).order!, { ...base, tiers }).why.filter(m => m.includes('tier') || m.includes('narrower'));
    expect(pass(want), id).toEqual([]);
    expect(pass(want.map(([w, d], i) => (i === 1 ? [w - NARROW, d] : [w, d]) as [number, number])), id).toEqual([]);
    expect(pass(want.map(([w, d], i) => (i === 1 ? [w - NARROW - 0.02, d] : [w, d]) as [number, number])).length, id).toBe(1);
    expect(pass(undefined).length, id).toBe(1);
  }
});
