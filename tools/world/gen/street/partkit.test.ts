// A crossing's parts take Vela's pieces (brief 009 A, catalogue.proposal.json) once they are adopted (brief 009 B, Splice: all eight are in contracts/world/kit.json), unscaled and by exact size. The catalogue here is the proposal's eight rows
// (id, use tag, size) set to adopted and written to a temp file, pinned with withKit, so nothing depends on out/ or on kit.json being changed.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { mkdtempSync, readFileSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import type { StreetItem } from '../../../../contracts/world/world.schema';
import { civic } from '../../../plan/civic';
import { kitFitChecks } from '../../../plan/kit-fit';
import { derive, type Plan } from '../../../plan/plan';
import { ctxOf } from '../../../plan/world-ctx';
import { worldOf } from '../../build';
import { withKit } from '../kitrefs';
import { bindPart, kitYaw } from './partkit';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), civ = civic(plan.civic, read(plan.civic.block));
const d = derive(plan), { world: w, gen } = worldOf(plan, d, civ);
const parts = w.street.filter(s => s.crossing).map(({ kit, ...s }) => ({ ...s, source: 'procedural' as const })); // the world binds them from the live kit: unbound here, so each test binds from its own catalogue

/** Vela's eight rows: id, use tag, size in kit axes (x, up, z). */
const PIECES: [string, string, [number, number, number]][] = [
  ['SignalEye', 'signal:eye', [1.12, 3.23, 1.12]], ['KerbStud', 'stud:kerb', [0.4, 0.084, 0.3804]], ['LightBar_4', 'crossing:bar', [4, 0.006, 0.44]],
  ['LightDotRow_2', 'crossing:dots', [2, 0.006, 0.43]], ['LightDotRow_1', 'crossing:dots', [1, 0.006, 0.43]], ['CrossInlayArc_R24_W08', 'inlay:crossing-arc', [2.83, 0.006, 2.83]],
  ['CrossEdgeLine_2', 'crossing:edge', [2, 0.006, 0.04]], ['CrossEdgeLine_1', 'crossing:edge', [1, 0.006, 0.04]],
];
const catalogue = (mutate: (rows: any[]) => void = () => {}) => {
  const kit = read('contracts/world/kit.json'), rows = PIECES.map(([id, use, size]) => ({ id, category: 'Street', size: [...size], pivot: 'footprint-centre-ground', front: '+Z', tris: 10, materials: [], tags: { use: [use], scale: [1, 1], squash: 1, wave: 2, sight: 'open' }, status: 'adopted', source: 'generated', files: [] }));
  mutate(rows);
  const file = join(mkdtempSync(join(tmpdir(), 'kit009-')), 'kit.json');
  writeFileSync(file, JSON.stringify({ ...kit, assets: [...kit.assets.filter((a: any) => !rows.some(x => x.id === a.id)), ...rows] }));
  return file;
};
const bound = (file?: string) => withKit(file, () => parts.map(bindPart));
const wrap = (t: number) => Math.atan2(Math.sin(t), Math.cos(t));

test('the pieces are adopted in the live kit: every part is bound to one, none scaled; with them not adopted every part stays procedural', () => {
  expect(bound().every(s => s.source === 'kit' && s.kit!.scale.every(v => v === 1))).toBe(true);
  const gone = catalogue(rows => { for (const r of rows) r.status = 'candidate'; });
  expect(bound(gone).every(s => s.source === 'procedural' && !s.kit)).toBe(true);
});

test('adopted, each part takes its piece by use and size: 768 + 212 dot rows, 1134 edge lines, 536 studs, 237 arcs, 156 eyes; none scaled', () => {
  const out = bound(catalogue()), by = (id: string) => out.filter(s => s.kit?.id === id).length;
  expect([by('LightDotRow_2'), by('LightDotRow_1'), by('KerbStud'), by('CrossInlayArc_R24_W08'), by('SignalEye'), by('LightBar_4')]).toEqual([768, 212, 536, 237, 156, 0]);
  expect(by('CrossEdgeLine_2') + by('CrossEdgeLine_1')).toBe(1134);
  expect(out.every(s => s.source === 'kit' && s.kit!.scale.every(v => v === 1))).toBe(true);
});

test('the kit yaw: lights and studs along the part\'s yaw, the eye\'s front (+Z) along the walk, the arc\'s quarter toward the corner it rounds', () => {
  const out = bound(catalogue());
  for (const s of out) {
    const want = s.kind === 'signal-orb' ? s.yaw - Math.PI / 2 : s.kind === 'crossing-arc' ? s.yaw + (3 * Math.PI) / 4 : s.yaw;
    expect(wrap(s.kit!.yaw! - want), s.id).toBeCloseTo(0, 3);
  }
  expect(kitYaw({ kind: 'signal-orb', yaw: 0 })).toBeCloseTo(-Math.PI / 2, 3);
});

test('a piece of another size is not the part\'s: the dot row module of 2.2 m leaves the 2 m rows procedural, the 1 m ones still bind', () => {
  const out = bound(catalogue(rows => { rows.find(r => r.id === 'LightDotRow_2').size[0] = 2.2; }));
  expect(out.filter(s => s.kind === 'crossing-dots' && s.size[0] === 2).every(s => s.source === 'procedural')).toBe(true);
  expect(out.filter(s => s.kind === 'crossing-dots' && s.size[0] === 1).every(s => s.kit?.id === 'LightDotRow_1')).toBe(true);
});

test('a part with no use tag in the kit (a lamp) is left as it was', () => {
  const lamp = w.street.find(s => s.kind === 'lamp') as StreetItem;
  expect(withKit(catalogue(), () => bindPart(lamp))).toBe(lamp);
});

test('K-1 holds a bound part to its own size: the bound world passes, a 2 m row given the 1 m piece, an eye given the stud, or a scaled piece fails', () => {
  const file = catalogue(), k1 = (mutate: (s: StreetItem[]) => void = () => {}) => withKit(file, () => {
    const street = w.street.map(s => (s.crossing ? bindPart(s) : s)).map(s => structuredClone(s));
    mutate(street);
    return kitFitChecks(ctxOf(plan, d, civ, { w: { ...w, street }, gen }))[0]!;
  });
  expect(k1().ok).toBe(true);
  const row = (street: StreetItem[], len: number) => street.find(s => s.kind === 'crossing-dots' && s.size[0] === len)!;
  expect(k1(st => { row(st, 2).kit!.id = 'LightDotRow_1'; }).detail).toContain('LightDotRow_1 1.0×0.0×0.4 m is not the crossing:dots piece of the part\'s 2.0×0.4×0.0 m');
  expect(k1(st => { st.find(s => s.kind === 'signal-orb')!.kit!.id = 'KerbStud'; }).detail).toContain('KerbStud');
  expect(k1(st => { row(st, 2).kit!.scale = [1.2, 1.2, 1.2]; }).detail).toContain('is not the crossing:dots piece');
});

test('each arc\'s quarter lies into the corner it rounds: measured from the live kit\'s pivotOffset (not from ARC_BULGE), the piece\'s footprint centre, turned by the kit yaw, lies along the arc\'s own heading from its pivot', () => {
  const asset = read('contracts/world/kit.json').assets.find((a: any) => a.id === 'CrossInlayArc_R24_W08');
  const [ox, oz] = asset.pivotOffset as [number, number]; // footprint centre from the pivot, kit axes: +X along the yaw, +Z at yaw + 90 degrees (box.ts signBox)
  expect(asset.pivot).toBe('origin-ground');
  const arcs = bound(catalogue()).filter(s => s.kind === 'crossing-arc' && s.kit?.id === 'CrossInlayArc_R24_W08');
  expect(arcs.length).toBeGreaterThan(0);
  for (const s of arcs) {
    const k = s.kit!.yaw!, cx = ox * Math.cos(k) - oz * Math.sin(k), cy = ox * Math.sin(k) + oz * Math.cos(k); // plan offset of the footprint centre from the pivot
    expect(wrap(Math.atan2(cy, cx) - s.yaw), s.id).toBeCloseTo(0, 2); // the arc item's yaw is the heading of the middle of its sweep from its centre
  }
});
