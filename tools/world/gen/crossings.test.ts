// Kerb-to-kerb crossings (streetscape.crossings.strip, Sky's walk note 1): what the generator makes on the live plan, and that
// the standard is opt-in (a plan without it exports the single landing patches it always did, and nothing else moves).
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { civic } from '../../plan/civic';
import { dist } from '../../plan/geom';
import { boxRing, ringGap } from '../../plan/street-zones';
import { derive, type Plan } from '../../plan/plan';
import { worldOf } from '../build';
import { areaOf, clipConvex } from './street/crossings';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const { world: w, gen } = worldOf(plan, d, civ);
const bare: Plan = structuredClone(plan);
delete bare.standards.streetscape.crossings.strip;
const old = worldOf(bare, derive(bare), civ).world;
const items = new Map(w.street.map(s => [s.id, s]));
const kerb = (w.crossings ?? []).filter(c => c.kind === 'kerb');

test('the live plan has crossings: 67 kerb pairs, 20 tram stop and 8 station bank patches, 22 refuges', () => {
  expect(w.crossings!.length).toBe(95);
  expect(kerb.length).toBe(67);
  expect(w.crossings!.filter(c => c.kind === 'stop').length).toBe(20);
  expect(w.crossings!.filter(c => c.kind === 'bank').length).toBe(8);
  expect(w.crossings!.reduce((n, c) => n + c.refuges.length, 0)).toBe(22);
  expect(w.counts.crossings).toBe(95);
  expect(gen.crossingLog!.landings).toBe(177); // 168 plot-edge landings and the civic block's 9
});

test('every crossing patch is in exactly one crossing, and a kerb crossing is two landings on opposite kerbs', () => {
  const uses = new Map<string, number>();
  for (const c of w.crossings!) for (const id of c.items) uses.set(id, (uses.get(id) ?? 0) + 1);
  const patches = w.street.filter(s => s.kind === 'crossing');
  expect(patches.length).toBe(162); // 134 landings in 67 pairs, 20 stop patches, 8 bank patches
  for (const s of patches) expect(uses.get(s.id), s.id).toBe(1);
  const rings = patches.map(s => ({ s, ring: boxRing(s.at, s.size[0], s.size[1], s.yaw) }));
  for (const [i, a] of rings.entries()) for (const b of rings.slice(i + 1)) if (dist(a.s.at, b.s.at) < 12) expect(ringGap(a.ring, b.ring), `${a.s.id} ~ ${b.s.id}`).toBeGreaterThanOrEqual(1); // no two patches run into each other
  for (const c of kerb) {
    expect(c.items.length, c.id).toBe(2);
    const [a, b] = c.items.map(id => items.get(id)!);
    expect(a!.edge, c.id).not.toBe(b!.edge);
    const road = d.roads.find(r => r.id === c.road)!;
    expect(Math.abs(dist(c.line[0], c.line[1]) - road.w), c.id).toBeLessThanOrEqual(1.5);
    expect(c.width).toBe(plan.standards.streetscape.crossings.w);
    expect(c.strip.length).toBe(4);
  }
});

test('a refuge is the slice of the strip that crosses a median of refuge width, and only that', () => {
  const need = plan.standards.streetscape.crossings.strip.refuge as number;
  for (const c of kerb) {
    const want = w.ground.medians.filter(m => dist(m.ring[1]!, m.ring[2]!) >= need).map(m => clipConvex(c.strip, m.ring)).filter(r => areaOf(r) > 0.25);
    expect(c.refuges.length, c.id).toBe(want.length);
  }
  const withRefuge = kerb.filter(c => c.refuges.length);
  expect(withRefuge.length).toBeGreaterThan(0);
  expect(withRefuge.every(c => c.control === 'zebra')).toBe(true); // the medianed roads are the district roads
});

test('opt-in: a plan without strip exports no crossings and all 196 single patches', () => {
  expect(old.crossings).toBeUndefined();
  expect(old.counts.crossings).toBeUndefined();
  expect(old.street.filter(s => s.kind === 'crossing').length).toBe(196);
});

test('with the standard on, nothing but crossings and the bollards and beacons that flank them changes', () => {
  const was = new Map(old.street.map(s => [s.id, s])), moves = new Set(['crossing', 'bollard', 'beacon']);
  // a tram stop's patch lies on the carriageway (z 0, not the pavement top above it); the Sphere Gardens bank's moves in off the lawn: the same patches, nothing else of them
  const lies = (s: typeof w.street[number], o: typeof s) => (s.kind === 'crossing' && s.edge.startsWith('stop/') ? { ...s, z: o.z } : s.id === 'street/bank/S1/gardens/l/crossing-213' ? { ...s, at: o.at } : s);
  for (const s of w.street) { const o = was.get(s.id); if (o) expect(JSON.stringify(lies(s, o)), s.id).toBe(JSON.stringify(o)); else expect(moves.has(s.kind), s.id).toBe(true); }
  for (const o of old.street) if (!items.has(o.id)) expect(moves.has(o.kind), o.id).toBe(true);
  const keep = (x: { street: { kind: string }[] }) => x.street.filter(s => !moves.has(s.kind)).length;
  expect(keep(w)).toBe(keep(old));
  for (const k of Object.keys(old).filter(k => k !== 'street' && k !== 'counts')) expect(JSON.stringify((w as any)[k]), k).toBe(JSON.stringify((old as any)[k])); // every other layer is the same
});

test('every kerb crossing is a zebra on every road class; a stop or a bank crossing keeps the line pair', () => {
  expect(plan.standards.streetscape.crossings.strip.control).toEqual({ primary: 'zebra', district: 'zebra', access: 'zebra' });
  for (const c of w.crossings!) expect(c.control, c.id).toBe(c.kind === 'kerb' ? 'zebra' : undefined);
  const byRoad = new Set(kerb.map(c => d.roads.find(r => r.id === c.road)!.class));
  expect([...byRoad].sort()).toEqual(['access', 'district']); // the live plan has no primary kerb crossing
});

test('every crossing landing patch draws the look key `crossing` and nothing else does: the trim colour (brass) does not decide the landings', () => {
  const patches = w.street.filter(s => s.kind === 'crossing');
  expect(patches.length).toBe(162);
  for (const s of patches) expect(s.surface, s.id).toBe('crossing');
  expect(w.street.filter(s => s.surface === 'crossing').length).toBe(162); // the civic block's nine are among them
  expect(w.street.filter(s => s.kind !== 'crossing').every(s => ['trim', 'park', 'enamel'].includes(s.surface))).toBe(true);
  expect(old.street.filter(s => s.kind === 'crossing').every(s => s.surface === 'crossing')).toBe(true); // a plan without the strip standard names it too
});

test('a patch lies at the level of the surface under it: a stop patch on the carriageway at 0, the rest on the pavement top; none on a lawn', () => {
  for (const s of w.street.filter(s => s.kind === 'crossing')) expect(s.z, s.id).toBe(s.edge.startsWith('stop/') ? 0 : 0.18); // 0.18 = the pavement top
  const gardens = items.get('street/bank/S1/gardens/l/crossing-213')!;
  expect(gardens.at).toEqual([-163.2, 15]); // it stood on the lawn 11.5 m out, at (-155.2, 29.5); now it is in the street pavement beside the foot of the tower (x -160)
  expect(old.street.find(s => s.id === gardens.id)!.at).toEqual([-155.2, 29.5]); // a plan without crossings.strip keeps it there
});
