// Brief 055 step 6 (Datum): one walking datum (L-9), the block's floors and the doors on them (C-5), and every mission point's level and floor (M-3).
// Each rule passes on the committed plan, block and mission and fails the example it exists for; the edits are copies in memory.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { Mission } from '../../contracts/missions/mission.schema';
import { levelCheck } from '../missions/handover/overlay';
import { civic, floorAt, toPlanFn } from './civic';
import { floorChecks } from './floors';
import { datumCheck } from './level-checks';
import { derive } from './plan';
import { ctxOf, generated } from './world-ctx';
import type { V } from './geom';

setDefaultTimeout(60000);
const root = resolve(import.meta.dir, '../..'), J = (f: string) => JSON.parse(readFileSync(resolve(root, f), 'utf8'));
const plan = J('contracts/world/harbour-district.plan.json'), block = J(plan.civic.block), mission = J('contracts/missions/handover/mission.json');
const d = derive(plan), civ = civic(plan.civic, block), toPlan = toPlanFn(plan.civic), D = plan.standards.levels.datum;
const c5 = (b: any, p = plan) => floorChecks(p, civic(p.civic, b))[0]!;
const office = (b: any) => b.buildings.find((x: any) => x.id === 'civic-office');
const floorOf = (b: any, id: string) => b.floors.find((x: any) => x.id === id);

describe('floorAt: the floor the block declares at a point', () => {
  test('the Office landing 0.75, each tread of its flight, a bay at carriageway level, the pavement 0', () => {
    expect(floorAt(civ, toPlan([64, -41.25]), D)).toEqual({ floor: 0.75, on: 'office-public-landing', level: 'street' });
    expect(floorAt(civ, toPlan([64, -43.7]), D).floor).toBe(0.125); // first tread, 0.1 m up the flight
    expect(floorAt(civ, toPlan([64, -43.0]), D).floor).toBe(0.375); // third tread (0.8 m from the foot: 2 whole treads)
    expect(floorAt(civ, toPlan([64, -42.1]), D).floor).toBe(0.625); // the top tread
    expect(floorAt(civ, toPlan([8.5, -48.2]), D)).toEqual({ floor: -0.18, on: 'squareBay', level: 'street' });
    expect(floorAt(civ, toPlan([64, -44.4]), D)).toEqual({ floor: 0, level: 'street' }); // Office approach, at the foot
  });
});

describe('C-5 floors and the doors on them', () => {
  test('passes on the committed block', () => expect(c5(block)).toMatchObject({ ok: true }));
  test('a landing whose top is not its structure\'s fails', () => {
    const b = structuredClone(block); floorOf(b, 'office-public-landing').top = 0.9;
    expect(c5(b).detail).toContain('top 0.9 ≠ the structure\'s 0.75');
  });
  test('a flight that does not climb to its landing fails', () => {
    const b = structuredClone(block); floorOf(b, 'office-public-steps').risers = 5;
    expect(c5(b).detail).toContain('5 × 0.125 = 0.63 ≠ its landing\'s top 0.75');
  });
  test('a flight steeper than the stair standard fails', () => {
    const b = structuredClone(block); Object.assign(floorOf(b, 'office-public-steps'), { rise: 0.25, risers: 3, top: 0.5 });
    const s = b.structures.find((x: any) => x.id === 'office-public-steps'); s.height = 0.5;
    expect(c5(b).detail).toContain('outside standards.levels.connectors.stair');
  });
  test('a door 0.75 m up with no floor declared under it fails (the guard sunk to the hips), and so does a door at 0 on the landing', () => {
    const b = structuredClone(block); delete b.floors;
    expect(c5(b).detail).toContain('Office entrance: the door stands at 0.75 m, the block declares 0 m there (the pavement)');
    const b2 = structuredClone(block); office(b2).entrances[0].z = 0;
    expect(c5(b2).detail).toContain('the block declares 0.75 m there (office-public-landing)');
  });
  test('a pinned plan without the datum is not checked', () => {
    const p = structuredClone(plan); delete p.standards.levels.datum;
    expect(floorChecks(p, civ)).toEqual([]);
  });
});

describe('M-3 every mission point\'s level and floor', () => {
  const w = generated(plan, d, civ).w, m3 = (m: any) => levelCheck(plan, d, civ, m, w);
  test('passes on the committed mission, naming the points off the pavement', () => {
    const r = m3(mission);
    expect(r.ok).toBe(true);
    expect(r.detail).toContain('Office entrance office-public-landing 0.75');
    expect(r.detail).toContain('Getaway bay squareBay -0.18');
    expect(r.detail).toContain('rival-park apron-road -0.18'); // in the carriageway
  });
  test('a point with no level, a door point that forgets its landing, and a bay point stated on the pavement each fail', () => {
    const m = structuredClone(mission);
    delete m.points[0].level;
    const door = m.destinations.find((x: any) => x.name === 'Office entrance'); delete door.on; delete door.floor;
    const bay = m.destinations.find((x: any) => x.name === 'Getaway bay'); delete bay.on; delete bay.floor;
    const r = m3(m);
    expect(r.ok).toBe(false);
    expect(r.detail).toContain('cabinet: level not stated');
    expect(r.detail).toContain('Office entrance: states the pavement, it stands on office-public-landing 0.75');
    expect(r.detail).toContain('Getaway bay: states the pavement, it stands on squareBay -0.18');
  });
  test('a point on the deck must stand on a deck piece: the street under the Office fails, a deck slab passes', () => {
    const m = structuredClone(mission), deck = w.levels.find(l => l.name === 'deck' || l.id === 'deck')!.decks[0]!;
    const c = deck.ring.reduce((a, q) => [a[0] + q[0] / deck.ring.length, a[1] + q[1] / deck.ring.length], [0, 0]) as V;
    m.points[0].level = 'deck';
    expect(m3(m).detail).toContain('cabinet: on level deck, but no deck piece is walkable');
    m.points[0].at = [-230 - c[1], c[0] + 130]; // plan → block frame (plan x = -130 + y, plan y = -230 - x)
    expect(m3(m).ok).toBe(true);
    m.points[0].level = 'mezzanine';
    expect(m3(m).detail).toContain('cabinet: level mezzanine is not in standards.levels');
  });
  test('the schema takes level, on and floor, and refuses on without floor', () => {
    expect(Mission.safeParse(mission).success).toBe(true);
    const m = structuredClone(mission); delete m.destinations.find((x: any) => x.name === 'Office entrance').floor;
    expect(Mission.safeParse(m).success).toBe(false);
  });
});

describe('L-9 one walking datum', () => {
  const c = ctxOf(plan, d, civ), l9 = (edit: (L: any) => void) => { const L = structuredClone(c.L); edit(L); return datumCheck({ ...c, L })[0]!; };
  test('passes on the committed plan and world', () => expect(l9(() => {})).toMatchObject({ ok: true }));
  test('a pavement the world does not lay, a level with no walk, a deck walked off its top each fail', () => {
    expect(l9(L => { L.datum.pavement = 0.15; L.list[0].walk = 0.15; }).detail).toContain('PROV.pavement) is 0.18, the datum 0.15');
    expect(l9(L => { delete L.list[1].walk; }).detail).toContain('level deck states no walk');
    expect(l9(L => { L.list[2].walk = 23; }).detail).toContain('concourse walk 23 ≠ its z 24');
  });
  test('a plan without the datum has nothing to check', () => expect(l9(L => { delete L.datum; })).toBeUndefined());
});
