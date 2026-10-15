// Large parts draw their own look key, so the trim colour (brass, 14 Oct) does not decide them (the way crossing landings did, 8df5287):
// `canopy` the Parade mall canopy slabs, `plate` the chrome plates of the dressed stair towers and lift tubes, `hoop` the lift tube hoops, `booth` a phone
// booth's body, `tub` a street palm tub's base, `crane` the market-deck gantry masts and booms, `girder` the Skyport's structural members. Thin brass (ribs,
// fins, mullions, palm trunks, lamps) stays `trim`, and so do roof items and frontage prims (RoofKit and ProcShop paint them from their own tables); nothing else
// moved. SF-21 (rules/trim.test.ts) is what holds the line.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { worldOf } from '../build';
import { BOOTH, CANOPY, CRANE, GIRDER, GLOW, HOOP, INLAY, ORB, OWN_KEYS, PLATE, TUB } from './surfaces';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), civ = civic(plan.civic, read(plan.civic.block));
const w = worldOf(plan, derive(plan), civ).world;

/** Every object of the export that names a surface, with its id (any section, any depth). */
function drawn(v: unknown, out: { id: string; surface: string }[] = []): { id: string; surface: string }[] {
  if (Array.isArray(v)) for (const x of v) drawn(x, out);
  else if (v && typeof v === 'object') {
    const o = v as { id?: string; surface?: unknown };
    if (typeof o.surface === 'string') out.push({ id: o.id ?? '?', surface: o.surface });
    for (const x of Object.values(o)) if (x && typeof x === 'object') drawn(x, out);
  }
  return out;
}
const all = drawn(w), withKey = (key: string) => all.filter(x => x.surface === key);

test('the Parade mall canopy slabs (121.4 x 2.5 m) draw `canopy`; the glass barrel, its ribs and piers do not', () => {
  const mega = w.megastructures.find(m => m.id === 'mega/parade-galleries')!, slabs = mega.prims.filter(p => /\/range-\d+-\d+\/canopy$/.test(p.id));
  expect(slabs.map(p => p.id)).toEqual(['mega/parade-galleries/range-0-0/canopy', 'mega/parade-galleries/range-1-0/canopy']);
  for (const p of slabs) { expect(p.surface, p.id).toBe('canopy'); expect(p.size, p.id).toEqual([121.4, 2.5, 0.3]); }
  expect(withKey('canopy').length).toBe(2); // nothing else in the world
  const barrel = mega.prims.filter(p => /\/gallery-canopy\//.test(p.id));
  expect(barrel.filter(p => /\/rib-\d+$/.test(p.id)).length).toBe(42);
  for (const p of barrel) expect(p.surface, p.id).toBe(/\/rib-\d+$/.test(p.id) ? 'trim' : /\/pier-\d+$/.test(p.id) ? 'column' : 'glass');
});

test('the dressed stair towers and lift tubes draw their large chrome plates in `plate` (142) and their hoops in `hoop` (60); fins and mullions stay `trim`', () => {
  const dress = w.connectors.flatMap(k => (k.dress ?? []).map(p => ({ k, p })));
  const tower = /\/dress\/(band-\d+\/chrome\/(mid|end-[ab])|cap\/(mid|end-[ab])|coping\/(mid|end-[ab]))$/, lift = /\/dress\/(cap|dome)$/;
  const plates = dress.filter(({ k, p }) => (k.kind === 'stairTower' ? tower : k.kind === 'lift' ? lift : /^$/).test(p.id));
  expect(plates.length).toBe(142); // 8 towers x (3 band slabs + cap + coping) of 3 prims; 11 lifts (8 bank, 3 skyport tube) x cap + dome
  expect(plates.filter(x => x.k.kind === 'stairTower').length).toBe(120);
  for (const { p } of plates) expect(p.surface, p.id).toBe(PLATE);
  expect(withKey(PLATE).length).toBe(142); // nothing else in the world
  const hoops = dress.filter(({ p }) => /\/dress\/hoop-\d+$/.test(p.id));
  expect(hoops.length).toBe(60); for (const { p } of hoops) expect(p.surface, p.id).toBe(HOOP);
  expect(withKey(HOOP).length).toBe(60);
  const thin = dress.filter(x => x.p.surface === 'trim').map(x => x.p.id.replace(/^.*\/dress\//, '').replace(/\d+/g, '#'));
  expect(thin.filter(id => id === 'fin').length).toBe(8);
  expect(thin.filter(id => id === 'mullion-#').length).toBe(44);
  expect(thin.length).toBe(52); // and no other dress part is trim
});

test('the market-deck cranes (4 masts, 4 booms) draw `crane`; the Skyport girder, 3 arms, mast and 3 tube spines draw `girder`; the cab and the needle do not', () => {
  const prims = (id: string) => w.megastructures.find(m => m.id === id)!.prims;
  const cranes = prims('mega/market-deck').filter(p => /\/crane-\d+\/(mast|boom)$/.test(p.id));
  expect(cranes.length).toBe(8); for (const p of cranes) expect(p.surface, p.id).toBe(CRANE);
  expect(prims('mega/market-deck').filter(p => /\/crane-\d+\/cab$/.test(p.id)).every(p => p.surface === 'enamel')).toBe(true);
  expect(withKey(CRANE).length).toBe(8);
  const sky = prims('mega/skyport-tower'), members = sky.filter(p => /\/(girder|arm-\d+|mast|tube-\d+-spine)$/.test(p.id));
  expect(members.length).toBe(8); for (const p of members) expect(p.surface, p.id).toBe(GIRDER);
  expect(withKey(GIRDER).length).toBe(8);
  expect(sky.find(p => p.id.endsWith('/needle'))!.surface).toBe('trim'); // 0.4 m: thin
});

test('every phone booth draws `booth` and every street palm tub `tub`; roof tubs and palm trunks stay `trim` (RoofKit paints roof items from roofItems)', () => {
  const booths = w.street.filter(s => s.kind === 'phone-booth'), tubs = w.street.filter(s => s.kind === 'tub-palm');
  expect(booths.length).toBe(64); for (const s of booths) expect(s.surface, s.id).toBe(BOOTH);
  expect(withKey(BOOTH).length).toBe(64);
  expect(tubs.length).toBe(198); for (const s of tubs) expect(s.surface, s.id).toBe(TUB);
  expect(withKey(TUB).length).toBe(198); // nothing else in the world: not the 418 roof tubs
  const roofTubs = w.roofs.flatMap(r => r.parts).filter(p => /\/(tub|palm)-\d+$/.test(p.id));
  expect(roofTubs.length).toBe(418 * 2); for (const p of roofTubs) expect(p.surface, p.id).toBe('trim');
});

test('a crossing\'s lights draw `glow`, its arcs `inlay`, its orbs `orb`; its studs stay `trim` (brass, a frame line); no slab is left to draw `crossing`', () => {
  const part = (kind: string) => w.street.filter(s => s.kind === kind);
  expect(part('crossing-bar').length).toBe(0); expect(part('crossing-dots').length).toBe(1464); for (const s of part('crossing-dots')) expect(s.surface, s.id).toBe(GLOW);
  expect(part('crossing-field').length).toBe(747); for (const s of part('crossing-field')) expect(s.surface, s.id).toBe(GLOW); // the lit strip is light too
  expect(part('crossing-arc').length).toBe(322); for (const s of part('crossing-arc')) expect(s.surface, s.id).toBe(INLAY);
  expect(part('signal-orb').length).toBe(207); for (const s of part('signal-orb')) expect(s.surface, s.id).toBe(ORB);
  expect(part('crossing-stud').length).toBe(704); for (const s of part('crossing-stud')) expect(s.surface, s.id).toBe('trim');
  expect(part('crossing')).toEqual([]);
  expect(withKey(GLOW).length).toBe(2211); expect(withKey(INLAY).length).toBe(322); expect(withKey(ORB).length).toBe(207); // 1464 dot rows + 747 lit strip modules
});

test('the canopy, plate, hoop, booth, tub, crane and girder keys, and a crossing\'s glow, inlay and orb, are the whole of OWN_KEYS, and the export carries 482 + 2,740 parts in them', () => {
  const keys: string[] = [...OWN_KEYS], named: string[] = [BOOTH, CANOPY, CRANE, GIRDER, HOOP, PLATE, TUB, GLOW, INLAY, ORB];
  expect(keys.sort()).toEqual(named.sort());
  const count = (ks: string[]) => ks.reduce((n, k) => n + withKey(k).length, 0);
  expect(count([BOOTH, CANOPY, CRANE, GIRDER, HOOP, PLATE, TUB])).toBe(482);
  expect(count([GLOW, INLAY, ORB])).toBe(2740);
  expect(count(OWN_KEYS as unknown as string[])).toBe(3222);
});

test('nothing else moved: every other street kind keeps the surface it drew before', () => {
  const was: Record<string, string> = {
    'advertising-column': 'enamel', beacon: 'trim', bench: 'trim', bin: 'trim', bollard: 'trim', 'canopy-camera': 'trim', 'civic-robot-dock': 'enamel', 'crossing-arc': 'inlay', 'crossing-dots': 'glow', 'crossing-edge': 'trim', 'crossing-field': 'glow', 'crossing-stud': 'trim',
    hydrant: 'trim', 'kiosk-food': 'enamel', 'kiosk-tickets': 'enamel', lamp: 'trim', mailbox: 'enamel', 'mast-camera': 'trim', newsstand: 'enamel',
    'phone-booth': 'booth', planter: 'park', 'pole-camera': 'trim', 'public-clock': 'trim', 'signal-orb': 'orb', 'station-entrance': 'enamel', 'ticket-machine': 'trim', 'timetable-column': 'trim',
    'tram-shelter': 'enamel', tree: 'park', 'tub-palm': 'tub', 'vending-robot': 'enamel',
  };
  const seen = new Map<string, Set<string>>();
  for (const s of w.street) seen.set(s.kind, (seen.get(s.kind) ?? new Set()).add(s.surface));
  for (const [kind, surfaces] of seen) expect([...surfaces], kind).toEqual([was[kind]!]);
  expect(w.street.length).toBe(8187); // 3091 without the 5,096 crossing parts (1,464 dot rows, 747 lit strip modules, 1,652 edge line modules, 207 orbs, 704 studs, 322 arcs): the 86 beacons gone where an eye stands, 6 trees and lamps moved out of the coverage sites' landings, and their bollards; the landing slabs are keep-clear footprints
});
