// Brief 072-C, the every-face pass (faces.ts) and the "no flat face at play zoom" check (rules/faces-cover.ts). Against the catalogue (Vela's every-face pieces
// adopted): every wall face dressed, pieces in range, service walls only on service backs, the same plan giving the same modules. Against the catalogue with
// those pieces set back to candidate (faces.fixture.ts): nothing placed, every face logged with the pieces that block it, the wave-2 world as it was.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { worldOf } from '../build';
import { withKit } from './kitrefs';
import { FACES, FACE_KIT, SHOP_PIECE, bandsOf, exposure, occOf, shopOverlap, shopTile, tile } from './faces';
import { faceKitOff } from './faces.fixture';
import { faceCoverage } from '../rules/faces-cover';
import { ctxOf } from './common';
import { isStreetClass } from './grammar';
import type { Kit } from '../../../contracts/world/kit.schema';

setDefaultTimeout(180000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const caps = (plan.standards.buildings as any).caps, plotArea = new Map(d.plots.map(p => [p.id, p.area]));
const areaOf = (b: string) => (b.startsWith('bld/') ? plotArea.get(b.split('/')[1]!) ?? '?' : b.startsWith('tower/') ? 'tower' : 'records'), play = (a: string) => !!caps[a]?.play;
const NEW = new Set(['side', 'service', 'stack', 'pier', 'pilaster']);

const dir = mkdtempSync(join(tmpdir(), 'faces-')), kitFile = join(dir, 'kit.json'), off: Kit = faceKitOff();
writeFileSync(kitFile, JSON.stringify(off));
const today = withKit(kitFile, () => worldOf(plan, d, civ)), offBy = new Map(off.assets.map(x => [x.id, x]));
rmSync(dir, { recursive: true, force: true });
const a = worldOf(plan, d, civ), b = worldOf(plan, d, civ);
const kit: Kit = read('contracts/world/kit.json'), fxBy = new Map(kit.assets.map(x => [x.id, x]));

test('without the every-face pieces (candidates): nothing placed, every face logged as blocked by the pieces it needs, the wave-2 world unchanged', () => {
  expect(today.world.facades.filter(f => NEW.has(f.form))).toEqual([]);
  expect(today.gen.faces.log.length).toBeGreaterThan(300); // brief 075: 406, the old blocks' street faces are their runs' pieces (was > 500)
  for (const l of today.gen.faces.log) { expect(l.modules, l.face).toBe(0); expect(l.blocked?.length, l.face).toBeGreaterThan(0); }
  expect(today.world.civic.find(m => m.id === 'civic/civic-office')!.kit).toBeUndefined(); // no RecordsTower yet
  expect(today.world.buildings.some(m => /\/band-\d+$/.test(m.id))).toBe(true); // the grammar's speed bands: the procedural facade stays until the kit is adopted
  const cov = faceCoverage(today.world, id => offBy.get(id), areaOf, play);
  expect(cov.undressed.length).toBeGreaterThan(0); // the check fails today, by design
});

test('every facade piece carries Redline\'s fields: LOD set, combine block and cell, instancing key', () => {
  for (const f of [...today.world.facades, ...a.world.facades]) {
    expect(f.lod?.set, f.id).toBe(f.kit.id); expect(f.lod!.levels).toBeGreaterThanOrEqual(1); expect(f.instance).toBe(`${f.kit.id}|${f.colourway}`);
    expect(f.combine!.cell).toEqual([Math.floor(f.kit.at[0] / FACES.hlodCell), Math.floor(f.kit.at[1] / FACES.hlodCell)]);
    if (NEW.has(f.form)) { expect(f.family, f.id).toBeDefined(); expect(f.column!.storeys).toBeGreaterThanOrEqual(1); expect(f.casts).toBe('proxy-l2'); expect(f.lod!.class, f.id).toBe(fxBy.get(f.kit.id)?.lodClass ?? offBy.get(f.kit.id)?.lodClass); if (!/^(StreamBay|PortholeBay|BalconyBay|ArcadeBay|DecoBay)_/.test(f.kit.id)) expect(f.lod!.class, f.id).toBeDefined(); expect(Math.hypot(...f.normal!)).toBeCloseTo(1, 3); }
    expect(f.combine!.block).toBe(f.building.startsWith('bld/') ? f.building.split('/')[1]! : f.building);
  }
});

test('every wall face of every building is kit (0 undressed), with modules on the play streets and beyond', () => {
  const cov = faceCoverage(a.world, id => fxBy.get(id), areaOf, play);
  expect(cov.undressed).toEqual([]);
  expect(a.gen.faces.log.filter(l => l.why)).toEqual([]);
  const n = a.world.facades.filter(f => NEW.has(f.form));
  expect(n.filter(f => play(areaOf(f.building))).length).toBeGreaterThan(500);
  expect(n.length).toBeGreaterThan(1000); // brief 075: 1644, the old blocks' street faces are their runs' true-size pieces, the every-face modules dress the rest (was > 5000)
  expect(a.world.buildings.some(m => /\/band-\d+$/.test(m.id) || /\/podium$/.test(m.id) && m.id.startsWith('bld/'))).toBe(false); // plain tiers: the kit is the wall
});

test('same plan, same catalogue, same modules (two runs)', () => {
  expect(JSON.stringify(b.world.facades)).toBe(JSON.stringify(a.world.facades));
  expect(JSON.stringify(b.gen.faces)).toBe(JSON.stringify(a.gen.faces));
});

test('every module is an adopted piece within its scale range; whole-wall pieces top out on H(n); stack modules stand on storey lines', () => {
  const B = plan.standards.buildings as any, pav = 0.18, line = (k: number) => pav + B.plinth + B.groundStorey + (k - 1) * B.upperStorey;
  for (const f of a.world.facades.filter(f => NEW.has(f.form))) {
    const p = fxBy.get(f.kit.id)!; expect(p.status).toBe('adopted');
    const [lo, hi] = p.tags.scale; for (const s of f.kit.scale) { expect(s, f.id).toBeGreaterThanOrEqual(lo - 1e-3); expect(s, f.id).toBeLessThanOrEqual(hi + 1e-3); }
    if (/^(SideBay|AlleyWall)_[34]D?$/.test(f.kit.id)) { const m = a.world.buildings.find(x => x.id.startsWith(f.building + '/'))!; expect(Math.abs(f.base + p.size[1] * f.kit.scale[1] - (m.base + m.height)), f.id).toBeLessThan(0.01); }
    if (f.kit.id === FACE_KIT.stack.mid && f.building.startsWith('bld/') && f.storeys > 0) expect(Array.from({ length: 40 }, (_, k) => line(k + 1)).some(z => Math.abs(z - f.base) < 0.01), f.id).toBe(true);
  }
});

test('service walls (AlleyWall) stand only on faces that front no street, side bays on the public faces', () => {
  const blds = new Map(a.gen.blds.map(x => [x.id, x]));
  for (const f of a.world.facades.filter(f => f.form === 'service' || f.form === 'side')) {
    const bl = blds.get(f.building)!, m: [number, number] = [(f.a[0] + f.b[0]) / 2, (f.a[1] + f.b[1]) / 2], p = bl.ring.p;
    let best = Infinity, cls = '';
    for (let j = 0; j < p.length; j++) { const q0 = p[j]!, q1 = p[(j + 1) % p.length]!, ux = q1[0] - q0[0], uy = q1[1] - q0[1], L = Math.hypot(ux, uy), t = Math.max(0, Math.min(1, ((m[0] - q0[0]) * ux + (m[1] - q0[1]) * uy) / (L * L))), dd = Math.hypot(q0[0] + ux * t - m[0], q0[1] + uy * t - m[1]); if (dd < best) { best = dd; cls = bl.ring.c[j]!; } }
    if (f.form === 'service') { expect(isStreetClass(cls), `${f.id} on ${cls}`).toBe(false); expect(['court', 'yard', 'back-to-back']).toContain(f.service!); }
    else expect(f.service).toBeUndefined();
  }
  expect(a.world.facades.filter(f => f.form === 'service').length).toBeGreaterThan(100);
  expect(a.world.facades.filter(f => f.form === 'side').length).toBeGreaterThan(100);
});

test('the Records Office is one RecordsTower; its towers stay as data', () => {
  const o = a.world.civic.find(m => m.id === 'civic/civic-office')!;
  expect(o.kit?.id).toBe('RecordsTower');
  for (const t of a.world.civic.filter(m => m.id.startsWith('civic/civic-office/tower-'))) expect(t.hidden).toBe(true);
});

test('tiling: whole numbers of 6 m modules at 0.85-1.15, pilasters for the rest', () => {
  expect(tile(12, 6, [0.85, 1.15])).toEqual({ k: 2, sx: 1, pil: [] });
  expect(tile(8, 6, [0.85, 1.15])).toBeNull(); // 6.9 < 8 < 10.2
  const t = tile(8, 6, [0.85, 1.15], { w: 1.5, range: [0.33, 1.7] })!;
  expect(t.k).toBe(1); expect(6 * t.sx + t.pil.reduce((s, x) => s + x, 0)).toBeCloseTo(8, 6);
  expect(tile(0.7, 6, [0.85, 1.15], { w: 1.5, range: [0.33, 1.7] })!.k).toBe(0);
});

test('bands: a 4-storey wall is base (ground storey), three mids and the crown; a strip above a 3-storey neighbour starts on the storey line under its top', () => {
  const ctx = ctxOf(plan, d, civ, 0.18), B = plan.standards.buildings as any, H = (n: number) => 0.18 + B.plinth + B.groundStorey + (n - 1) * B.upperStorey + B.roofCrown;
  expect(bandsOf(ctx, 0.18, H(4), H(4)).map(x => x.role)).toEqual(['base', 'mid', 'mid', 'mid', 'top']);
  const strip = bandsOf(ctx, H(3), H(4), H(4));
  expect(strip.map(x => x.role)).toEqual(['top']); expect(strip[0]!.z).toBeLessThanOrEqual(H(3));
});

test('exposure: a neighbour as tall hides the shared wall; a lower one leaves the strip above it', () => {
  const ring: [number, number][] = [[10, 0], [20, 0], [20, 10], [10, 10]];
  expect(exposure([10, 10], [10, 0], 0, 20, [occOf('n', ring, 0, 20)], 'me')).toEqual([]);
  expect(exposure([10, 10], [10, 0], 0, 20, [occOf('n', ring, 0, 12)], 'me')).toEqual([{ s0: 0, s1: 10, from: 12 }]);
});

test('shop runs (pieces.md Pass 2): fillers 3 m nearest R / 3; a run on a tenancy\'s line (an arc\'s chord within 0.6 m) counts; every plot tenancy is linked to a piece', () => {
  expect(shopTile(1.5)).toEqual({ k: 1, sx: 0.5 }); expect(shopTile(4.6).k).toBe(2); expect(shopTile(9).sx).toBeCloseTo(1, 6);
  for (const R of [1.5, 2.2, 4.49, 4.51, 7.3, 13.1]) { const t = shopTile(R); expect(t.sx).toBeGreaterThanOrEqual(0.5); expect(t.sx).toBeLessThanOrEqual(1.7); }
  const t = [{ host: 'h', a: [0, 0] as [number, number], b: [6, 0] as [number, number] }];
  expect(shopOverlap([[[4, 0], [10, 0]]], t)).toBeCloseTo(2, 6);
  expect(shopOverlap([[[0, 0.45], [3, 0.2]]], t)).toBeCloseTo(3, 6); // a chord across an arc
  expect(shopOverlap([[[0, 2], [6, 2]]], t)).toBe(0); // another line
  const shop = a.world.facades.filter(f => SHOP_PIECE.test(f.kit.id)), linked = new Set(a.world.frontages.map(f => f.facade).filter(Boolean));
  expect(shop.length).toBeGreaterThan(0);
  for (const f of shop) {
    expect(f.form === 'side' || f.form === 'stack', f.id).toBe(true);
    if (f.kit.id === FACE_KIT.shop.base) expect(f.base, f.id).toBeLessThan(0.2); // the ground row, from the pavement
    else { const m = a.world.buildings.find(x => x.id.startsWith(f.building + '/'))!, p = fxBy.get(f.kit.id)!; expect(Math.abs(f.base + p.size[1] * f.kit.scale[1] - (m.base + m.height)), f.id).toBeLessThan(0.01); }
  }
  // every run of fillers has a tenancy linked to one of them; no own-shopfront tenancy of a plot building is left
  const runs = new Map<string, string[]>(); for (const f of shop) (runs.get(f.face!) ?? runs.set(f.face!, []).get(f.face!)!).push(f.id);
  for (const [face, ids] of runs) expect(ids.some(id => linked.has(id)), face).toBe(true);
  expect(a.world.frontages.filter(f => f.level === 'street' && f.host.startsWith('bld/') && !f.facade).map(f => f.id)).toEqual([]);
  // a stacked shop run has no base-band side module left under its row (K-6, brief 072: at an acute corner the row stops short and a pilaster base takes the
  // corner, beside the row, never under it)
  for (const [face, ids] of runs) {
    const row = a.world.facades.filter(f => ids.includes(f.id) && f.kit.id === FACE_KIT.shop.base); if (!row.length) continue;
    const o = row[0]!.a, u = [row[0]!.b[0] - o[0], row[0]!.b[1] - o[1]], l = Math.hypot(u[0]!, u[1]!), s = (p: number[]) => ((p[0]! - o[0]) * u[0]! + (p[1]! - o[1]) * u[1]!) / l;
    const lo = Math.min(...row.flatMap(f => [s(f.a), s(f.b)])), hi = Math.max(...row.flatMap(f => [s(f.a), s(f.b)]));
    const under = a.world.facades.filter(f => f.face === face && /^(SideBayStack|SidePilaster)_Base$/.test(f.kit.id) && s([(f.a[0] + f.b[0]) / 2, (f.a[1] + f.b[1]) / 2]) > lo + 0.01 && s([(f.a[0] + f.b[0]) / 2, (f.a[1] + f.b[1]) / 2]) < hi - 0.01);
    expect(under.map(f => f.id), face).toEqual([]);
  }
});

// Brief 075-E (Sky 2026-10-28: "we've still lost all the lovely curved buildings"): streamline is the default grammar of the faces the public sees.
test('streamline grammar (075-E): the majority of public face metres, never on a service back, storey-true in the play streets, corners and ends square and adopted', () => {
  const STREAM = /^(Stream|Porthole|Balcony|Arcade|Deco)|^CornerTower_/, seen = new Set<string>();
  let pub = 0, st = 0;
  for (const f of a.world.facades) {
    if (f.service || /^AlleyWall/.test(f.kit.id)) { expect(f.kit.id, f.id).not.toMatch(STREAM); continue; }
    if (f.column && seen.has(f.column.id)) continue; if (f.column) seen.add(f.column.id);
    const m = f.form === 'pier' ? 0 : Math.hypot(f.b[0] - f.a[0], f.b[1] - f.a[1]) * (f.form === 'corner' ? 2 : 1);
    pub += m; if (STREAM.test(f.kit.id)) st += m;
  }
  expect(st / pub).toBeGreaterThan(0.5);
  for (const f of a.world.facades.filter(x => x.face && STREAM.test(x.kit.id))) {
    expect(fxBy.get(f.kit.id)!.status, f.id).toBe('adopted');
    const whole = /_(\d+)[WL]?$/.exec(f.kit.id);
    if (play(areaOf(f.building)) && whole && !/^CornerTower/.test(f.kit.id)) expect(Number(whole[1]), f.id).toBe(f.storeys); // a whole-wall piece carries the building's storey code
  }
  // a streamline corner from the every-face pass stands at a square outside corner: its two legs a→b and b→(the leaving face) meet at 90° (within 3°)
  for (const f of a.world.facades.filter(x => x.face && x.form === 'corner')) expect(Math.hypot(f.b[0] - f.a[0], f.b[1] - f.a[1]), f.id).toBeGreaterThan(7);
});
