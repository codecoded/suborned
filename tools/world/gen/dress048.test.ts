// Brief 048 (Vela, 2026-10-24): the roofs and the reservation are dressed, not boxes. Glyph: "big flat orange and yellow boxes on the rooftops read as untextured placeholders"; the Records Office wing's
// bare 15.68 m roof read as a grey slab; the Boulevard's tram reservation was nothing but enamel with a white band down each edge.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { worldOf } from '../build';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const { world: w } = worldOf(plan, d, civ);

test('no roof item is a bare box in the building\'s enamel: the vents are finned rockets, the plant a cream house (both keep the plan\'s sizes)', () => {
  const parts = w.roofs.flatMap(r => r.parts);
  expect(parts.filter(p => /\/vent-\d+$/.test(p.id)).length).toBe(0);
  expect(parts.filter(p => p.surface === 'trim' && p.shape === 'box' && p.size[0] === 2 && p.size[1] === 2 && p.size[2] === 3.5).length).toBe(0);
  expect(parts.filter(p => /\/plant$/.test(p.id) && p.surface === 'column').length).toBe(0);
  for (const r of w.roofs.filter(x => x.form !== 'terrace')) {
    const vents = new Set(r.parts.filter(p => /\/rocket-vent-\d+-body$/.test(p.id)).map(p => p.id.replace(/-body$/, '')));
    expect(vents.size, r.id).toBeGreaterThanOrEqual(r.programme === 'hover-pad' ? 0 : 1); // max(2, ceil(area / 300)), less those that stand within 10 m of a hover pad (the plan's clear zone)
    for (const v of vents) {
      const mine = r.parts.filter(p => p.id.startsWith(v + '-')), top = Math.max(...mine.map(p => p.base + p.size[2])) - r.z, reach = Math.max(...mine.map(p => Math.max(p.size[0], p.size[1])));
      expect(top, v).toBeLessThanOrEqual(3.5 + 1e-6); expect(reach, v).toBeLessThanOrEqual(2 + 1e-6); // the plan's rocket-vent size
    }
  }
});

test('the civic block\'s two buildings carry a roof terrace of their own (the office wing: deck, border line, troughs, two vents), low enough to hide nothing the wing does not', () => {
  const roofs = w.roofs.filter(r => r.building.startsWith('civic/'));
  expect(roofs.map(r => r.building).sort()).toEqual(['civic/cafe', 'civic/civic-office']);
  for (const r of roofs) {
    const m = w.civic.find(c => c.id === r.building)!;
    expect(r.z).toBeCloseTo(m.base + m.height, 3);
    expect(r.parts.some(p => /\/deck$/.test(p.id)) && r.parts.some(p => /\/border-/.test(p.id)) && r.parts.some(p => /\/trough-/.test(p.id))).toBe(true);
    expect(r.parts.filter(p => /\/rocket-vent-\d+-body$/.test(p.id)).length).toBe(2);
    for (const p of r.parts) expect(p.base + p.size[2] - r.z, p.id).toBeLessThanOrEqual(3.5 + 1e-6);
    for (const p of r.parts.filter(q => !/rocket-vent/.test(q.id))) expect(p.base + p.size[2] - r.z, p.id).toBeLessThanOrEqual(2 + 1e-6);
  }
});

test('the Boulevard\'s tram reservation is lawn track: one flat per stretch between its breaks, 10 m wide, under the rails', () => {
  const lawn = w.ground.flats.filter(f => f.id.startsWith('reservation/'));
  expect(lawn.length).toBeGreaterThan(5);
  for (const f of lawn) {
    const xs = f.ring.map(p => p[0]), ys = f.ring.map(p => p[1]);
    expect(Math.max(...xs) - Math.min(...xs), f.id).toBeCloseTo(10, 1);
    expect(Math.max(...ys) - Math.min(...ys), f.id).toBeGreaterThanOrEqual(2);
    expect(f.level, f.id).toBeLessThan(w.transit.tram[0]!.level); // the rails (level 0.04) lie on it
    expect(f.surface).toBe('median'); // look.json ground/median: DK_Ground_Lawn
  }
});
