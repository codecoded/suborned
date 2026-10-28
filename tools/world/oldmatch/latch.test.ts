// Latch's swap export (latch.oldbuilt/1) read as suborned.oldbuilt/1 (latch.ts), and the parts of the check it brings: bay fronts standing in for doors, the
// zone taken from our export (an old piece inside it is a hard failure), kerb stones joined into kerb lines, pavement modules turned to their grid.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { OldBuilt } from '../../../contracts/world/oldbuilt.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { generated } from '../../plan/world-ctx';
import { type LatchBuilt, fromLatch, joinLines, kerbLine, tileTurns } from './latch';
import { builtOf, builtSide, compare, worldSide } from './match';

setDefaultTimeout(180000);
type V = [number, number];
const near = (a: V, b: V, tol = 1e-3) => Math.hypot(a[0] - b[0], a[1] - b[1]) <= tol;

test('a kerb stone lays 2 m of kerb square to its forward; a corner is the R 2 m arc joining the two kerb lines (DecoKitStreetLayout.SideStreet)', () => {
  const s = kerbLine({ piece: 'Kerb_Granite_0', at: [8, -100], yaw: -90, top: 0.15 }); // walk side +X: the kerb runs north-south on x 8
  expect(s.every(p => Math.abs(p[0] - 8) < 1e-3)).toBe(true);
  expect(Math.abs(s[0]![1] - s[1]![1])).toBeCloseTo(2, 3);
  // the side street's south corner cell (KerbX + 1, SideZ.x - 1) = old (9, 59), plan (9, -147), yaw 0: kerb lines x 8 (boulevard) and y -146 (side street)
  const c = kerbLine({ piece: 'Kerb_GraniteCorner_0', at: [9, -147], yaw: 0, top: 0.15 });
  expect([c[0]!, c[c.length - 1]!].some(p => near(p, [10, -146]))).toBe(true);
  expect([c[0]!, c[c.length - 1]!].some(p => near(p, [8, -148]))).toBe(true);
  expect(c.every(p => Math.abs(Math.hypot(p[0] - 10, p[1] + 148) - 2) < 1e-2)).toBe(true);
});

test('kerb stones end to end join into one line; a gap splits it', () => {
  const st = (y: number, i: number) => ({ id: `k${i}`, line: kerbLine({ piece: 'Kerb_Granite_0', at: [8, y], yaw: i % 2 ? 90 : -90, top: 0.15 }), top: 0.15 });
  const j = joinLines([st(1, 0), st(5, 1), st(3, 2), st(9, 3)]);
  expect(j.length).toBe(2);
  expect(Math.max(...j.map(x => x.line.length))).toBe(4);
});

test('pavement modules take their turn from their grid: a 30° grid reads 30°, a lone module 0', () => {
  const a = Math.PI / 6, at = (i: number, k: number): V => [2 * i * Math.cos(a) - 2 * k * Math.sin(a), 2 * i * Math.sin(a) + 2 * k * Math.cos(a)];
  const t = tileTurns([{ at: at(0, 0), size: 2 }, { at: at(1, 0), size: 2 }, { at: at(0, 1), size: 2 }, { at: [100, 100], size: 2 }]);
  expect(t[0]).toBeCloseTo(a, 2);
  expect(t[2]).toBeCloseTo(a, 2);
  expect(t[3]).toBe(0);
});

test('fromLatch: levels, decks, bays as entrances, zone ours, coverage, and the old water left out', () => {
  const raw: LatchBuilt = {
    schema: 'latch.oldbuilt/1', scene: 'Assets/Scenes/OldTown.unity', handoverZone: [-226, -84, -372, -148],
    buildings: [{ piece: 'StreamBay_3', kind: 'frontage', ring: [[18, -96], [18, -90], [32, -90], [32, -96]], base: 0.15, top: 16.45, building: 'run1.0', front: [-1, 0], at: [18, -93] }],
    frontages: [{ building: 'run1.0', piece: 'StreamBay_3', at: [18, -93], normal: [-1, 0], w: 6 }],
    kerbs: [{ piece: 'Kerb_Granite_0', at: [8, -93], yaw: -90, top: 0.15 }], pavements: [{ piece: 'Pave_Flags_0', at: [9, -93], size: 2, top: 0.15 }], roads: [],
    levels: [{ id: 'old/road', height: -0.002 }, { id: 'old/walk', height: 0.15 }, { id: 'old/water', height: -2 }, { id: 'mega/x/walk', height: 9.43, ring: [[0, 0], [8, 0], [8, 8], [0, 8]], what: 'ours (export levels): deck' }],
  };
  const b = fromLatch(raw);
  expect(b.schema).toBe('suborned.oldbuilt/1');
  expect(b.levels.find(l => l.id === 'street')).toEqual({ id: 'street', z: -0.002, walk: 0.15 });
  expect(b.levels.find(l => l.id === 'deck')?.z).toBe(9.43);
  expect(b.levels.some(l => l.id === 'old/water')).toBe(false);
  expect(b.entrances[0]).toMatchObject({ building: 'run1.0', kind: 'bay', w: 6, normal: [-1, 0] });
  expect(b.buildings[0]).toMatchObject({ kind: 'commercial', origin: 'old', height: 16.3 });
  expect(b.zone).toBe('ours');
  expect(b.decks?.[0]?.level).toBe('deck');
  expect(b.coverage?.length).toBe(2);
});

const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block)), { w } = generated(plan, d, civ);
const P = plan as any, ours = worldSide(w, P), zone = P.standards.blocks.zone as [number, number, number, number];
const inZ = (p: V) => p[0] >= zone[0] && p[0] <= zone[1] && p[1] >= zone[2] && p[1] <= zone[3];
/** Our world with the zone left out (zone 'ours') and the doors outside it turned into bay fronts `along` m off along their wall. */
const asSwap = (along: number): OldBuilt => {
  const b = structuredClone(builtOf(w, P));
  const cut = <T>(xs: T[], pts: (x: T) => V[]) => xs.filter(x => !pts(x).some(inZ));
  b.buildings = cut(b.buildings, x => x.ring); b.pavements = cut(b.pavements, x => x.ring); b.decks = cut(b.decks ?? [], x => x.ring);
  // no centrelines (as Latch's file): our kerbs as kerb lines, cut at the zone
  b.kerbs = ours.kerbs.flatMap(k => { const out: V[][] = []; let cur: V[] = []; for (const p of k.pts) { if (inZ(p)) { if (cur.length > 1) out.push(cur); cur = []; } else cur.push(p); } if (cur.length > 1) out.push(cur); return out.map((line, i) => ({ id: `${k.id}#${i}`, line, top: 0.18 })); });
  b.roads = []; b.circuses = [];
  b.entrances = w.frontages.map(f => {
    const n = f.normal as V, t: V = [-n[1], n[0]], c = f.entrance.c as V, at = (k: number): V => [c[0] + t[0] * along * k, c[1] + t[1] * along * k];
    return { id: `bay/${f.id}`, building: f.host, at: inZ(at(1)) ? at(-1) : at(1), z: f.z, normal: n, w: 6, kind: 'bay' as const }; // shifted away from the zone
  }).filter(e => !inZ(e.at));
  b.zone = 'ours';
  return b;
};

test('zone ours: our world with the zone cut out and doors as bays (2 m off along, inside the 6 m bay) matches; an old flag inside the zone is a hard failure', () => {
  const ok = compare(builtSide(asSwap(2)), ours, P);
  expect(ok.mismatches.filter(m => m.kind === 'entrance')).toEqual([]);
  expect(ok.inZone).toBe(0);
  const bad = asSwap(2); bad.pavements.push({ id: 'old/flag-in-zone', ring: [[-150, -300], [-148, -300], [-148, -298], [-150, -298]], top: 0.15 });
  const r = compare(builtSide(bad), ours, P);
  expect(r.mismatches.some(m => m.kind === 'intrusion' && m.zone && m.el === 'old/flag-in-zone')).toBe(true);
  // touching the zone's edge from outside is not inside it
  const edge = asSwap(2); edge.pavements.push({ id: 'old/flag-at-edge', ring: [[zone[1], -300], [zone[1] + 2, -300], [zone[1] + 2, -298], [zone[1], -298]], top: 0.18 });
  expect(compare(builtSide(edge), ours, P).mismatches.some(m => m.kind === 'intrusion')).toBe(false);
});

test('bays: doors 5 m off their bay (beyond w/2 + 1 m) are plan-only, and the buildings whose bays carry none are built-only', () => {
  const r = compare(builtSide(asSwap(5)), ours, P), e = r.mismatches.filter(m => m.kind === 'entrance');
  expect(e.some(m => m.side === 'plan-only')).toBe(true);
  expect(e.some(m => m.side === 'built-only')).toBe(true);
  expect(e.every(m => !m.zone)).toBe(true);
});
