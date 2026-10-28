// TS-W0, no tram overhead (brief 075, Sky 2026-10-28). The live export passes; each failing example puts one piece of overhead back, on a copy of it.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { World } from '../../../contracts/world/world.schema';
import type { Kit } from '../../../contracts/world/kit.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { ctxOf, generated } from '../../plan/world-ctx';
import { overheadFaults, overheadRules } from './overhead';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block)), base = generated(plan, d, civ);
const ts = (w: World) => overheadRules(ctxOf(plan, d, civ, { w, gen: base.gen }))[0]!;
const clone = (w: World): World => structuredClone(w);

test('the live export: TS-W0 passes (no wires, no wire piece, no span pole), and the tram street still has its setts, rails and islands', () => {
  const r = ts(base.w);
  expect(r.ok, r.detail).toBe(true);
  expect('wires' in base.w.transit).toBe(false);
  expect(base.w.street.some(s => s.kind === 'span-pole')).toBe(false);
  expect((base.w.ground.pieces ?? []).some(p => p.kind === 'tram-rail')).toBe(true);
  expect(base.w.ground.flats.some(f => f.surface === 'setts')).toBe(true);
});

test('the catalogue: every TramWire_* piece is rejected and carries no use tag', () => {
  const kit: Kit = read('contracts/world/kit.json'), wires = kit.assets.filter(a => a.id.startsWith('TramWire_'));
  expect(wires.map(a => a.id).sort()).toEqual(['TramWire_Contact', 'TramWire_Hanger', 'TramWire_Pole', 'TramWire_Rosette', 'TramWire_Span']);
  for (const a of wires) { expect(a.status, a.id).toBe('rejected'); expect(a.tags.use, a.id).toEqual([]); }
});

test('failing example: a transit.wires block put back fails TS-W0', () => {
  const w = clone(base.w);
  (w.transit as Record<string, unknown>).wires = { contact: [], spans: [{ id: 'tram/T1/trunk/span-0', line: 'T1', a: [0, 0], b: [10, 0], z: 7, ends: [{ kind: 'rosette', at: [0, 0] }, { kind: 'rosette', at: [10, 0] }] }], pieces: [] };
  expect(ts(w).ok).toBe(false);
  const f = overheadFaults(w).faults.join('\n');
  expect(f).toContain('transit.wires');
  expect(f).toContain('tram/T1/trunk/span-0 is a tram span');
});

test('failing example: a TramWire_Hanger kit ref on a ground piece fails TS-W0', () => {
  const w = clone(base.w), p = w.ground.pieces!.find(x => x.kind === 'tram-rail')!;
  p.kit = { ...p.kit!, id: 'TramWire_Hanger' };
  expect(ts(w).ok).toBe(false);
  expect(overheadFaults(w).faults.join('\n')).toContain('kit TramWire_Hanger');
});

test('failing example: a span pole in the street fails TS-W0', () => {
  const w = clone(base.w), lamp = w.street.find(s => s.kind === 'lamp')!;
  w.street.push({ ...lamp, id: 'street/tram/T1/trunk/span-3/pole-a', kind: 'span-pole', size: [0.3, 0.3, 8.5] });
  expect(ts(w).ok).toBe(false);
  expect(overheadFaults(w).faults.join('\n')).toContain('a span pole');
});
