// Brief 075 (stable zone, Aster 2026-10-29): nothing inside the Handover zone (standards.blocks.zone) changes when the plan changes outside it. Datum's S2 laid 42 station
// items before the zone's, which renumbered every zone id (civic-robot-dock-166 → -208), swapped 8 lamps (the kit pick was seeded by id) and moved the sim walk grid
// 10 cells. Fixtures: the live plan with one more gate outside the zone (its pole camera is laid before every street), and the live plan without S2 (the 42 items gone).
// Each must leave the zone's street items (ids, places, pieces), its ground pieces, its crossings and the takedown places byte for byte, and must change the street outside
// (so the fixture is not empty). Kit pins (ZK-1) are off here: the stability is the generator's (zone ids apart, zone picks first), not the pins'.
import { expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { derive, type Plan } from '../plan/plan';
import { civic } from '../plan/civic';
import { worldOf } from './build';
import { withZonePins } from './gen/kitrefs';
import { inZone } from './gen/street/kit';

const root = resolve(import.meta.dir, '../..');
const read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const live: Plan = read('contracts/world/harbour-district.plan.json');
const zone = (live.standards as any).blocks.zone as [number, number, number, number];

function build(mutate?: (p: Plan) => void) {
  const plan: Plan = structuredClone(live);
  mutate?.(plan);
  const d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
  return withZonePins(null, () => worldOf(plan, d, civ));
}
const at = (x: any): number[] | undefined => x.at ?? x.c ?? x.line?.[0] ?? x.ring?.[0];
/** Everything the world holds inside the zone, as JSON lines in order. */
function zoneOf(g: ReturnType<typeof build>) {
  const w = g.world as any, inZ = (x: any) => { const p = at(x); return !!p && inZone(zone, p); };
  return {
    street: (w.street as any[]).filter(inZ).map(s => JSON.stringify(s)),
    pieces: ((w.ground?.pieces ?? []) as any[]).filter(inZ).map(s => JSON.stringify(s)),
    crossings: ((w.crossings ?? []) as any[]).filter(inZ).map(s => JSON.stringify(s)),
    takedown: JSON.stringify(w.takedown ?? null),
  };
}
const outside = (g: ReturnType<typeof build>) => (g.world.street as any[]).filter(s => !inZone(zone, s.at)).map(s => `${s.id} ${s.at}`).join('\n');

const base = build(), baseZone = zoneOf(base);

test('the zone holds street items, ground pieces and the takedown (the comparison is not empty)', () => {
  expect(baseZone.street.length).toBeGreaterThan(300);
  expect(baseZone.pieces.length).toBeGreaterThan(0);
  expect(baseZone.takedown).not.toBe('null');
  // the zone's minted ids carry the zone's own count (gen/street/kit.ts): no district count reaches them
  expect((base.world.street as any[]).filter(s => inZone(zone, s.at) && ['mast-camera', 'civic-robot-dock'].includes(s.kind)).map(s => s.id)).toEqual(['street/records-square/mast-camera-z0', 'street/records-square/civic-robot-dock-z1']);
});

test('an extra gate outside the zone changes nothing inside it', () => {
  const g = build(p => { const m = p.gates.find(x => x.id === 'market-gate')!; p.gates.push({ ...m, id: 'fixture-gate', name: 'Fixture' }); });
  expect(outside(g)).not.toBe(outside(base)); // the gate's pole camera is laid, and renumbers the district
  expect(zoneOf(g)).toEqual(baseZone);
});

test('the plan without S2 (the Harbour Line and its 42 station items) changes nothing inside the zone', () => {
  const g = build(p => {
    p.skyrail = p.skyrail.filter(l => l.id !== 'S2');
    const tt = (p as any).timetable; if (tt?.lines) tt.lines = (tt.lines as any[]).filter(l => l.line !== 'S2' && l.id !== 'S2');
  });
  expect(outside(g)).not.toBe(outside(base));
  expect(zoneOf(g)).toEqual(baseZone);
});
