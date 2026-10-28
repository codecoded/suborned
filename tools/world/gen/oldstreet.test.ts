// Brief 075 (golden view): the old boulevard's rules on the old-street roads (oldstreet.ts), and their failing examples.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { World } from '../../../contracts/world/world.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { ctxOf as worldCtx } from '../../plan/world-ctx';
import { streetChecks } from '../../plan/street-checks';
import { worldOf } from '../build';
import { ctxOf } from './common';
import { classOf } from './stream';
import { OLD_STREET, isOldRoad, palmStations, roadFrame } from './oldstreet';
import { inCapstoneZone } from './street/palms';

setDefaultTimeout(180000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const { world: w, gen } = worldOf(plan, d, civ), ctx = ctxOf(plan, d, civ, 0.18);
const road = OLD_STREET.roads[0]!;
/** The palms on the old road's pavements: tree items within its pavement (dk ≤ 2) of the road line, by side. */
const palms = w.street.filter(s => s.kind === 'tree' && /\/e\d+$/.test(s.edge)).map(s => ({ s, f: roadFrame(ctx, road, s.at as [number, number]) })).filter(x => {
  const r = d.roads.find(q => q.id === road)!, dl = Math.min(...r.line.slice(1).map((b, i) => { const a = r.line[i]!, L = Math.hypot(b[0] - a[0], b[1] - a[1]), t = Math.max(0, Math.min(L, ((x.s.at[0] - a[0]) * (b[0] - a[0]) + (x.s.at[1] - a[1]) * (b[1] - a[1])) / L)); return Math.hypot(x.s.at[0] - (a[0] + (b[0] - a[0]) * t / L), x.s.at[1] - (a[1] + (b[1] - a[1]) * t / L)); }));
  return dl - r.w / 2 > 0 && dl - r.w / 2 < 2;
});

test('palms: the old 10 m, staggered by the old phase (left u ≡ 5, right u ≡ 9 mod 10), 1.2 m off the kerb line (SF-5 holds against the old 1.0)', () => {
  expect(palms.length).toBeGreaterThan(100);
  for (const { s, f } of palms) {
    const u = s.at[0] * f!.dir[0] + s.at[1] * f!.dir[1], ph = f!.left ? OLD_STREET.palms.phase.left : OLD_STREET.palms.phase.right;
    const m = (((u - ph) % OLD_STREET.palms.spacing) + OLD_STREET.palms.spacing) % OLD_STREET.palms.spacing;
    expect(Math.min(m, OLD_STREET.palms.spacing - m), s.id).toBeLessThan(0.01);
  }
  expect(OLD_STREET.palms.kerb - 0.75).toBeGreaterThanOrEqual(0.45 - 1e-9); // the 1.5 m pit's edge keeps SF-5's 0.45 m
  // the stations of a straight edge along the road: every 10 m, none outside the edge's span
  const st = palmStations(ctx, road, [14, -400], [0, 1], [14, -300], 9, 191)!;
  expect(st.every((x, i) => i === 0 || Math.abs(x - st[i - 1]! - 10) < 1e-9)).toBe(true);
  expect(st[0]!).toBeGreaterThanOrEqual(9); expect(st[st.length - 1]!).toBeLessThanOrEqual(191);
});

test('SF-7 takes the old 10 m on the old-street roads only: a palm pair 10 m apart on another district road fails (failing example)', () => {
  const sf7 = (x: World) => streetChecks(worldCtx(plan, d, civ, { w: x, gen })).find(c => c.id === 'SF-7')!;
  expect(sf7(w).ok, sf7(w).value).toBe(true);
  const other = w.street.find(s => s.kind === 'tree' && /\/e\d+$/.test(s.edge) && !palms.some(p => p.s.id === s.id) && s.variant === 'coconut')!;
  const sib = w.street.find(s => s.kind === 'tree' && s.edge === other.edge && s.id !== other.id)!, u = [(sib.at[0] - other.at[0]) / Math.hypot(sib.at[0] - other.at[0], sib.at[1] - other.at[1]), (sib.at[1] - other.at[1]) / Math.hypot(sib.at[0] - other.at[0], sib.at[1] - other.at[1])];
  const bad: World = { ...w, street: [...w.street.filter(s => s.id !== sib.id), { ...other, id: `${other.id}-x`, at: [other.at[0] + u[0]! * 10, other.at[1] + u[1]! * 10] as [number, number] }] }; // its neighbour moved to 10 m along the same edge
  const r = sf7(bad);
  expect(r.ok).toBe(false); expect(JSON.stringify(r)).toContain('10.0 m apart (< 12)');
});

test('frontage: the buildings fronting the old road stand streamline bays only, at the old storey codes outside the play streets, rounding off only toward a lower neighbour', () => {
  const B = plan.standards.buildings, H = (n: number) => B.plinth + B.groundStorey + (n - 1) * B.upperStorey + B.roofCrown;
  // brief 075 (the old buildings): a building the plan lays as an old block (gen.blds[].old, oldblocks.ts) follows the old runs and Aster's 3-storey remap (oldblocks.test.ts);
  // these rules hold for the old road's buildings today's grammar still builds
  const all = gen.blds.filter(b => !b.tower && b.ring.c.some(isOldRoad)), on = all.filter(b => !b.old), play = (a: string) => !!(B.caps as Record<string, { play?: boolean }>)[a]?.play;
  expect(all.length).toBeGreaterThan(20); expect(on.length).toBeGreaterThan(0);
  for (const b of on) if (!play(b.area) && b.use === 'commercial') expect(Object.keys(OLD_STREET.frontage.codes).map(Number), b.id).toContain(b.n);
  const faces = w.facades.filter(f => { const b = on.find(x => x.id === f.building); return b && f.edge !== undefined && isOldRoad(b.ring.c[f.edge]!) && ['bay', 'end', 'corner'].includes(f.form); });
  expect(faces.length).toBeGreaterThan(0);
  expect(w.facades.filter(f => all.some(b => b.id === f.building) && ['bay', 'end', 'corner'].includes(f.form)).length).toBeGreaterThan(80);
  for (const f of faces.filter(x => x.form === 'bay')) expect(OLD_STREET.frontage.classes, f.id).toContain(classOf(f.kit.id) ?? '');
  // an end stands where its building is taller than the neighbour past its tail, or none stands there
  for (const f of faces.filter(x => x.form === 'end')) {
    const b = on.find(x => x.id === f.building)!, v = b.ring.p[f.edge!]!, z = b.ring.p[(f.edge! + 1) % b.ring.p.length]!, L = Math.hypot(z[0] - v[0], z[1] - v[1]), t = [(z[0] - v[0]) / L, (z[1] - v[1]) / L], n = [t[1], -t[0]];
    const q: [number, number] = [v[0] - t[0]! - n[0]! * 2, v[1] - t[1]! - n[1]! * 2], o = gen.blds.find(x => x.id !== b.id && inRing(q, x.ring.p));
    expect(!o || H(b.n) > H(o.n) + 1, f.id).toBe(true);
  }
});

test('colour: the old HighStreet rule, each wall painted and cream by turns from the start of each run, the Soft palette in order', () => {
  const on = gen.blds.filter(b => !b.tower && b.ring.c.some(isOldRoad)), of = new Map(w.buildings.map(m => [m.id.split('/').slice(0, 3).join('/'), m.colourway]));
  const fams = on.map(b => of.get(b.id)!);
  expect(new Set(fams)).toEqual(new Set([...OLD_STREET.colour.palette, OLD_STREET.colour.plain]));
  const painted = fams.filter(f => OLD_STREET.colour.palette.includes(f!)).length;
  expect(painted).toBeGreaterThanOrEqual(fams.length / 2); // the first of every run is painted
  // no other building is given an old family
  for (const m of w.buildings) if (OLD_STREET.colour.palette.includes(m.colourway ?? '')) expect(on.some(b => m.id.startsWith(b.id + '/')), m.id).toBe(true);
});

function inRing(p: [number, number], r: [number, number][] | number[][]): boolean {
  let inside = false;
  for (let i = 0, j = r.length - 1; i < r.length; j = i++) { const a = r[i]!, b = r[j]!; if ((a[1]! > p[1]) !== (b[1]! > p[1]) && p[0] < ((b[0]! - a[0]!) * (p[1] - a[1]!)) / (b[1]! - a[1]!) + a[0]!) inside = !inside; }
  return inside;
}

test('palms district-wide (brief 075 roll-out): every planted district and access edge outside the capstone zone stands its palms 10 m apart; the capstone zone keeps 12 m (failing example: a palm pair 8 m apart fails SF-7)', () => {
  const byEdge = new Map<string, [number, number][]>();
  for (const s of w.street) if (s.kind === 'tree' && /\/e\d+$/.test(s.edge)) byEdge.set(s.edge, [...(byEdge.get(s.edge) ?? []), s.at as [number, number]]);
  const gaps = (pts: [number, number][]) => pts.slice(1).map((p, i) => Math.hypot(p[0] - pts[i]![0], p[1] - pts[i]![1]));
  let ten = 0, twelve = 0; const boulevard = new Set(palms.map(p => p.s.edge));
  for (const [edge, pts] of byEdge) {
    if (pts.length < 3) continue;
    const plot = edge.split('/e')[0]!, g = gaps(pts), min = Math.min(...g);
    if (boulevard.has(edge)) continue; // the old-street road keeps its 10 m everywhere (the test above)
    if (inCapstoneZone(ctx, plot)) { expect(min, edge).toBeGreaterThan(11.4); twelve++; } else if (Math.abs(min - 10) < 0.05) ten++;
  }
  expect(ten).toBeGreaterThan(30);     // the old grid on the district's other streets, not only the boulevard
  expect(twelve).toBeGreaterThan(2);   // the Handover's zone as it was
  const sf7 = (x: World) => streetChecks(worldCtx(plan, d, civ, { w: x, gen })).find(c => c.id === 'SF-7')!;
  const [edge, pts] = [...byEdge].find(([e, p]) => p.length >= 3 && !inCapstoneZone(ctx, e.split('/e')[0]!) && !boulevard.has(e) && Math.abs(Math.min(...gaps(p)) - 10) < 0.05)!;
  const a = pts[0]!, b = pts[1]!, L = Math.hypot(b[0] - a[0], b[1] - a[1]), moved: [number, number] = [a[0] + ((b[0] - a[0]) / L) * 8, a[1] + ((b[1] - a[1]) / L) * 8];
  const sib = w.street.find(s => s.kind === 'tree' && s.edge === edge && s.at[0] === b[0] && s.at[1] === b[1])!;
  const bad: World = { ...w, street: w.street.map(s => (s.id === sib.id ? { ...s, at: moved } : s)) };
  expect(sf7(bad).ok).toBe(false); expect(JSON.stringify(sf7(bad))).toContain('8.0 m apart (< 10)');
});
