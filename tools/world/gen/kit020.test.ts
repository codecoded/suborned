// Brief 020 (Splice): the pieces Vela's pick adopted are wired into the world: street kinds, roof programmes, the café swap. A phone booth stays procedural (its piece is a double cabinet).
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Kit } from '../../../contracts/world/kit.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { fit } from '../../kit/fit';
import { worldOf } from '../build';
import { ctxOf } from './common';
import { slotOf } from './kitrefs';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const kit: Kit = read('contracts/world/kit.json'), by = new Map(kit.assets.map(a => [a.id, a]));
const { world: w } = worldOf(plan, d, civ), ctx = ctxOf(plan, d, civ);
const kinds = (k: string) => w.street.filter(s => s.kind === k);

test('the pieces of the pick are adopted, their sha256 rows carry every file, and the café textures are in the catalogue', () => {
  for (const id of ['CafeBay_v2', 'CafeInterior', 'Sedan_Official', 'Sedan_Getaway', 'RobotPorter_Play', 'ServiceRobot_Play', 'RobotButler_Play', 'Roof_Tank', 'Roof_Mast', 'Roof_LatticeMast', 'Planter_Trough', 'Planter_Urn', 'PottedFern', 'Roof_Pool', 'Roof_LapPool', 'Roof_Conservatory', 'Roof_Planter', 'Roof_Cabana', 'Roof_Tennis', 'BusShelter_Play', 'TramCanopy_Play', 'CurvedKiosk_Play', 'CoffeeCart_Play', 'StreetClock', 'PneumaticPost', 'FanPalm_Play', 'Planter_Urn_Play', 'PneumaticPost_Play', 'PhoneBooth_Single', 'Newsstand']) {
    const a = by.get(id)!; expect(a.status, id).toBe('adopted');
    expect(a.files.some(f => f.role === 'fbx'), id).toBe(true); for (const f of a.files) expect(f.sha256, `${id} ${f.path}`).toMatch(/^[0-9a-f]{64}$/);
  }
  const textures = by.get('CafeInterior')!.files.filter(f => f.role === 'texture').map(f => f.path);
  expect(textures).toEqual(expect.arrayContaining(['Textures/cafe_floor_v1.png', 'Textures/cafe_back_bar_v1.png', 'Textures/cafe_mural_v1.png']));
  expect(by.get('Sedan_Official')!.tags.use).toEqual(['vehicle:car-official']); expect(by.get('CafeBay_v2')!.tags.sight).toBe('glazed');
});

test('the street kinds Vela picked now draw their kit pieces; the _Play copies and the single booth and newsstand took the slots of the heavy or ill-sized ones (follow-up)', () => {
  const ids = (k: string) => new Set(kinds(k).map(s => s.kit?.id ?? 'procedural'));
  expect(ids('planter')).toEqual(new Set(['Planter_Trough', 'Planter_Urn_Play']));
  expect(ids('mailbox')).toEqual(new Set(['PneumaticPost_Play'])); expect(ids('public-clock')).toEqual(new Set(['StreetClock']));
  expect(ids('tram-shelter')).toEqual(new Set(['BusShelter_Play', 'TramCanopy_Play'])); expect(ids('newsstand')).toEqual(new Set(['Newsstand'])); expect(ids('kiosk-food')).toEqual(new Set(['CoffeeCart_Play']));
  expect([...ids('tub-palm')].sort()).toEqual(['FanPalm_Play', 'PottedFern']);
  expect(ids('phone-booth')).toEqual(new Set(['PhoneBooth_Single']));
  const ph = by.get('PhoneBooth')!, slot = slotOf(ctx, kinds('phone-booth')[0]!)!;
  expect(ph.size[0]).toBeGreaterThan(slot.along! * 2); expect(fit(ph, slot)).toBeNull(); // the 2.6 m twin never fit; the single cabinet does
  for (const id of ['FanPalm', 'Planter_Urn', 'PneumaticPost', 'PhoneBooth', 'CurvedKiosk_Play']) expect(w.street.some(s => s.kit?.id === id), `${id} is retired from the street slots`).toBe(false);
});

test('a palm in a planter\'s bed is the bare palm (FanPalm_Play), never a potted piece; the two share the one point', () => {
  const beds = new Set(kinds('planter').map(s => s.at.join()));
  const planted = kinds('tub-palm').filter(s => beds.has(s.at.join()));
  expect(planted.length).toBe(186); for (const s of planted) expect(s.kit!.id).toBe('FanPalm_Play');
  expect(kinds('tub-palm').filter(s => s.kit!.id === 'PottedFern').every(s => !beds.has(s.at.join()))).toBe(true);
});

test('roof programmes stand their kit piece in place of the parts it draws: one piece per roof, inside the roof rect, none of its procedural stand-in left', () => {
  const by2 = (p: string) => w.roofs.filter(r => r.programme === p && r.form !== 'terrace');
  const tanks = by2('water-sphere'); expect(tanks.length).toBe(92); for (const r of tanks) { expect(r.kit!.id).toBe('Roof_Tank'); expect(r.parts.some(p => /\/(sphere|leg-\d)$/.test(p.id))).toBe(false); }
  const masts = by2('antenna-mast').filter(r => r.kit); expect(masts.length).toBeGreaterThan(100); expect(new Set(masts.map(r => r.kit!.id))).toEqual(new Set(['Roof_Mast', 'Roof_LatticeMast']));
  for (const r of masts) expect(r.parts.some(p => /\/(mast|ring-\d+)$/.test(p.id))).toBe(false);
  expect(new Set(by2('sun-terrace-pool').map(r => r.kit?.id))).toEqual(new Set(['Roof_Pool', 'Roof_LapPool']));
  expect(by2('tennis-court')[0]!.kit!.id).toBe('Roof_Tennis');
  for (const r of w.roofs) if (r.kit) { expect(by.get(r.kit.id)!.status).toBe('adopted'); expect(r.kit.scale).toEqual([1, 1, 1]); }
});

test('the café: the block names CafeBay, the world draws CafeBay_v2 with its room, both fading with the architecture', () => {
  const ground = w.civic.find(m => m.id === 'civic/cafe/ground')!, room = w.civic.find(m => m.id === 'civic/cafe/interior')!;
  expect(ground.kit!.id).toBe('CafeBay_v2'); expect(room.kit!.id).toBe('CafeInterior'); expect(ground.fade && room.fade).toBe(true);
  expect(w.civic.some(m => m.kit?.id === 'CafeBay')).toBe(false);
});

test('each tram shelter faces its own track (its open front is yaw + 90 degrees) and the civic robot is the 2.6k-triangle copy', () => {
  const tracks = d.tram.map(p => p.samples.map(s => s.p as [number, number]));
  const shelters = kinds('tram-shelter'); expect(shelters.length).toBe(20);
  for (const s of shelters) {
    let best = { dd: Infinity, q: [0, 0] as [number, number] }; for (const l of tracks) for (const q of l) { const dd = Math.hypot(q[0] - s.at[0], q[1] - s.at[1]); if (dd < best.dd) best = { dd, q }; }
    const toTrack = [(best.q[0] - s.at[0]) / best.dd, (best.q[1] - s.at[1]) / best.dd];
    expect(Math.cos(s.yaw + Math.PI / 2) * toTrack[0]! + Math.sin(s.yaw + Math.PI / 2) * toTrack[1]!, s.id).toBeGreaterThan(0.9);
  }
  expect(new Set(shelters.map(s => s.yaw)).size).toBeGreaterThan(1); // a stop's two platforms no longer share one yaw
  expect(w.civic.find(m => m.id === 'civic/foreground-civic-robot')!.kit!.id).toBe('RobotButler_Play');
});
