// The generated pieces in kit.json fit their sites: one per plan regen order, current hash, adopted, source generated, footprint and tip inside the order.
import { expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Kit } from '../../contracts/world/kit.schema';
import { fitRow, printTable } from './fit';
import { readOrders } from './orders';

const kit: Kit = JSON.parse(readFileSync(resolve(import.meta.dir, '../../contracts/world/kit.json'), 'utf8'));
const orders = readOrders(), gen = kit.assets.filter(a => a.source === 'generated');

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
