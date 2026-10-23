// RF-1, RF-2 (brief 026/028): the committed plan passes; each failing example is the committed plan or kit with one thing changed.
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { type KitRow, kitRows, roadFinishChecks } from './road-finish';
import { civic } from './civic';
import { type Plan, derive } from './plan';

const root = resolve(import.meta.dir, '../..');
const plan: Plan = JSON.parse(readFileSync(resolve(root, 'contracts/world/harbour-district.plan.json'), 'utf8'));
const d = derive(plan), civ = civic(plan.civic, JSON.parse(readFileSync(resolve(root, plan.civic.block), 'utf8'))); // the geometry the kerb-face clauses read; the examples change standards only
const run = (mutate: (p: any) => void = () => {}, kit: KitRow[] = kitRows()) => {
  const p = structuredClone(plan); mutate(p);
  return Object.fromEntries(roadFinishChecks(p, kit, { d, civ }).map(c => [c.id, c]));
};
const R = (p: any) => p.standards.streetscape.roads, I = (p: any) => p.standards.streetscape.inlays;

describe('the committed plan', () => {
  test('passes RF-1 and RF-2', () => {
    const r = run();
    expect(Object.keys(r)).toEqual(['RF-1', 'RF-2']);
    expect(r['RF-1']!.ok).toBe(true); expect(r['RF-2']!.ok).toBe(true);
  });
  test('the numbers Sky and Vela gave: enamel on every class, lens at 0.10, lozenges 8.5, seams 9, stop short 0.3', () => {
    const s = R(plan);
    expect(s.finish.byClass).toEqual({ primary: 'enamel', district: 'enamel', access: 'enamel' });
    expect(s.finish.keys.enamel.material).toBe('DK_Ground_RoadEnamel');
    expect([s.finish.keys.enamel.surface, s.finish.kerb.surface]).toEqual(['road-enamel', 'kerb']);
    expect([s.ribbon.offset, s.lozenges.spacing, s.seams.spacing, s.breaks.stopShort]).toEqual([0.1, 8.5, 9, 0.3]);
  });
  test('a plan without the keys (a pinned plan) gets no RF check', () => {
    expect(run(p => { delete p.standards.streetscape.roads; delete p.standards.streetscape.inlays; })).toEqual({});
  });
});

describe('RF-1 fails', () => {
  const f = (m: (p: any) => void, re: RegExp, kit?: KitRow[]) => { const c = run(m, kit)['RF-1']!; expect(c.ok).toBe(false); expect(c.detail).toMatch(re); };
  test('a road class with no finish', () => f(p => { delete R(p).finish.byClass.access; }, /class access has no finish/));
  test('a finish key that names no material', () => f(p => { R(p).finish.byClass.primary = 'pale'; }, /class primary: finish pale is not a key/));
  test('paint allowed', () => f(p => { R(p).finish.paint = true; }, /never painted/));
  test('a ribbon on a class no road has', () => f(p => { R(p).ribbon.classes.push('service'); }, /ribbon class service is no road's class/));
  test('a ribbon piece the kit does not have', () => f(p => { R(p).ribbon.pieces.straight = 'RoadRibbon_Straight_9m'; }, /not in the kit/));
  test('a ribbon piece the kit rejected', () => f(() => {}, /RoadRibbon_Straight_6m is rejected/, kitRows().map(r => r.id === 'RoadRibbon_Straight_6m' ? { ...r, status: 'rejected' } : r)));
  test('a seam piece without its use tag', () => f(p => { R(p).seams.piece = 'RoadMark_Lozenge'; }, /RoadMark_Lozenge has no use tag ground:road-seam/));
  test('the ribbon under the kerb (offset < half its section)', () => f(p => { R(p).ribbon.offset = 0.05; }, /brass would run under the kerb/));
  test('no stop short', () => f(p => { R(p).breaks.stopShort = 0; }, /stopShort 0 is not > 0/));
  test('a break kind missing (drives)', () => f(p => { delete R(p).breaks.at.drive; }, /no break at drive/));
  test('lozenges that touch', () => f(p => { R(p).lozenges.spacing = 2; }, /lozenge spacing 2 < length 1.7/));
  test('crossings that let the ribbon run across', () => f(p => { R(p).crossings.meet = 'through'; }, /crossings.meet "through" is not stop/));
  test('a zebra allowed', () => f(p => { R(p).crossings.never = ['paint']; }, /crossings.never misses zebra/));
  test('a zebra crossing control', () => f(p => { p.standards.streetscape.crossings.strip.control.access = 'zebra'; }, /crossing control on access is zebra/));
});

describe('RF-1 fails: the shoulder, the pinstripe and the kerb faces (brief 028 Road)', () => {
  const f = (m: (p: any) => void, re: RegExp) => { const c = run(m)['RF-1']!; expect(c.ok).toBe(false); expect(c.detail).toMatch(re); };
  test('the committed plan carries all three', () => { const s = R(plan); expect(s.shoulder.piece).toBe('RoadShoulder_Straight_6m'); expect(s.pinstripe.piece).toBe('PaveEdge_Pinstripe_6m'); expect(s.kerbs.circusMouths.faces.length + s.kerbs.footMouths.faces.length).toBe(11); });
  test('a finish key with no look surface', () => f(p => { delete R(p).finish.keys.enamel.surface; }, /finish enamel has no `surface`/));
  test('a kerb cap with no look surface', () => f(p => { delete R(p).finish.kerb.surface; }, /finish.kerb has no `surface`/));
  test('a shoulder piece without its use tag', () => f(p => { R(p).shoulder.piece = 'RoadMark_Seam'; }, /RoadMark_Seam has no use tag ground:road-shoulder/));
  test('a shoulder arc the kit does not have', () => f(p => { R(p).shoulder.arcs['4'] = 'RoadShoulder_Arc90_R5'; }, /shoulder arc R4: "RoadShoulder_Arc90_R5" is not in the kit/));
  test('a shoulder that starts under the ribbon', () => f(p => { R(p).shoulder.from = 0.1; }, /starts under the ribbon/));
  test('a shoulder that reaches the middle line', () => f(p => { R(p).shoulder.to = 4.5; }, /reaches the narrowest middle line/));
  test('a pinstripe on the kerb crown', () => f(p => { R(p).pinstripe.offset = 0.03; }, /must exceed the kerb crown's brass/));
  test('a mouth face off its road\'s carriageway edge', () => f(p => { R(p).kerbs.circusMouths.faces[0].a = [9.5, 42.048]; }, /circus-mouth\/crown-avenue\/e a: 0\.5\d m off crown-avenue's carriageway edge/));
  test('a mouth face ending neither on the ring nor at a plot corner', () => f(p => { R(p).kerbs.circusMouths.faces[0].b = [9, 47]; }, /b: ends neither on the circus ring/));
  test('a mouth face that points out of the carriageway', () => f(p => { R(p).kerbs.circusMouths.faces[0].toward = [1, 0]; }, /toward points out of the carriageway/));
  test('a face whose toward is not its normal', () => f(p => { R(p).kerbs.circusMouths.faces[0].toward = [0.6, 0.8]; }, /toward is not the unit normal/));
  test('a kerbed bay that does not touch its road', () => f(p => { R(p).kerbs.bays.kerbed[0].road = 'garden-row'; }, /kerbed bay squareBay does not touch garden-row/));
  test('a kerbed bay the block does not have', () => f(p => { R(p).kerbs.bays.kerbed[1].bay = 'harbourBay'; }, /kerbed bay harbourBay is not in the civic block/));
});

describe('RF-2 fails', () => {
  const f = (m: (p: any) => void, re: RegExp) => { const c = run(m)['RF-2']!; expect(c.ok).toBe(false); expect(c.detail).toMatch(re); };
  test('a ring piece the kit does not have', () => f(p => { I(p).ring.piece = 'PaveInlay_Ring_Half'; }, /ring: "PaveInlay_Ring_Half" is not in the kit/));
  test('a band piece with the ring\'s use tag', () => f(p => { I(p).bands.piece = 'PaveInlay_Ring_Q'; }, /has no use tag inlay:tile-band/));
  test('a centrepiece too big for the ring\'s slab', () => f(p => { I(p).ring.centrepiece.maxR = 4; }, /centrepiece maxR 4 < minSlab/));
  test('spokes starting inside the ring', () => f(p => { I(p).bands.spokes.from = 4.5; }, /start inside the ring/));
  test('a band scaled to another length', () => f(p => { I(p).bands.length = 4; }, /band length 4 ≠ the piece's 6/));
});
