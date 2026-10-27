// Brief 048 (Vela, 2026-10-24): the roofs and the reservation are dressed, not boxes. Glyph: "big flat orange and yellow boxes on the rooftops read as untextured placeholders"; the Records Office wing's
// bare 15.68 m roof read as a grey slab; the Boulevard's tram reservation was nothing but enamel with a white band down each edge.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic, toPlanFn } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { civicMasses, worldOf } from '../build';

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
  // brief 066 (Vela): the café's body above its ground storey is a kit piece (CafeUpper_3 or _4) whose crown is its own planted terrace and solarium, so only the office takes a generated one
  // brief 072 (Sky "yes"): once RecordsTower is adopted the office is that piece, whose roof terrace and rotunda are its own, so neither building takes a generated one
  const office = w.civic.find(c => c.id === 'civic/civic-office')!;
  expect(roofs.map(r => r.building).sort()).toEqual(office.kit?.id === 'RecordsTower' ? [] : ['civic/civic-office']);
  const cafe = w.civic.find(c => c.id === 'civic/cafe')!; expect(cafe.kit?.id).toMatch(/^CafeUpper_[34]$/);
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

test('brief 066: the café keeps its storey count; its body piece is the one built to the block\'s height, and the other count has its piece too', () => {
  const B = plan.standards.buildings, H = (n: number) => B.plinth + B.groundStorey + (n - 1) * B.upperStorey + B.roofCrown, kit = read('contracts/world/kit.json') as { assets: { id: string; size: number[]; status: string }[] };
  const cafe = w.civic.find(c => c.id === 'civic/cafe')!, ground = w.civic.find(c => c.id === cafe.groundBy)!;
  expect(cafe.base + cafe.height - ground.base).toBeCloseTo(civ.buildings.find(b => b.id === 'cafe')!.height, 3); // the block's height, untouched
  expect(cafe.kit!.base).toBeCloseTo(cafe.base, 3); // the piece's lowest point stands on the body's base (FacadeKit.Civic: PivotForLowest)
  expect(cafe.kit!.at).toEqual(ground.kit!.at); expect(cafe.kit!.yaw).toBe(ground.kit!.yaw);
  for (const n of [3, 4]) { const a = kit.assets.find(x => x.id === `CafeUpper_${n}`)!; expect(a.status).toBe('adopted'); expect(a.size[1]).toBeCloseTo(H(n) - (ground.height), 2); }
});

test('brief 066: at 3 storeys (main\'s recommendation, not yet Sky\'s pick) the same block draws CafeUpper_3 to H(3)', () => {
  const B = plan.standards.buildings, H3 = B.plinth + B.groundStorey + 2 * B.upperStorey + B.roofCrown;
  const low = { ...civ, buildings: civ.buildings.map(b => (b.id === 'cafe' ? { ...b, height: H3 } : b)) };
  const cafe = civicMasses(low, toPlanFn(plan.civic)).find(m => m.id === 'civic/cafe')!;
  expect(cafe.kit?.id).toBe('CafeUpper_3'); expect(cafe.base + cafe.height - 0.18).toBeCloseTo(H3, 3);
});
