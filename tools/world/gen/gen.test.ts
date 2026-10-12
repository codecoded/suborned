// Checks on the generated world: the brief's five (no primary frontage, nothing under a megastructure, every deck has its
// connectors, the café and office keep their size, byte-identical reruns) and the roof and cap rules the generators follow.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { World } from '../../../contracts/world/world.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { PROV, worldOf } from '../build';
import { deckGroups, isWayDown, wayRect } from './connectors';
import { ctxOf } from './common';
import { type Colonnade, stripOf, zonesOf } from './mega/colonnade';
import { reach } from './travel';
import { type Rect, areaOf, bbox, clipHalf } from './geo';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const { world: w, gen } = worldOf(plan, d, civ);
const rect = (r: number[]) => ({ p: [[r[0]!, r[2]!], [r[1]!, r[2]!], [r[1]!, r[3]!], [r[0]!, r[3]!]] as [number, number][], c: ['x', 'x', 'x', 'x'] });
const rectAround = (c: number[]): [number, number][] => [[c[0]! - 0.1, c[1]! - 0.1], [c[0]! + 0.1, c[1]! - 0.1], [c[0]! + 0.1, c[1]! + 0.1], [c[0]! - 0.1, c[1]! + 0.1]];
/** Area of ring ∩ rect (rect is convex: four half-plane clips). */
function overlap(ring: [number, number][], r: number[]): number {
  let q: ReturnType<typeof clipHalf> = { p: ring, c: ring.map(() => 'x') };
  for (const [a, n] of [[[r[0]!, 0], [1, 0]], [[r[1]!, 0], [-1, 0]], [[0, r[2]!], [0, 1]], [[0, r[3]!], [0, -1]]] as [[number, number], [number, number]][]) { if (!q) return 0; q = clipHalf(q, a, n); }
  return q ? areaOf(q.p) : 0;
}

test('no frontage on primary roads', () => {
  const primary = new Set(plan.roads.filter(r => r.class === 'primary').map(r => r.id));
  expect(w.frontages.length).toBeGreaterThan(500);
  expect(w.frontages.filter(f => primary.has(f.street))).toEqual([]);
});

test('no plot mass under a megastructure', () => {
  const towersOf = new Map((plan.megastructures ?? []).map(m => [m.id, (m.towers ?? []) as string[]]));
  const bad: string[] = [];
  for (const m of plan.megastructures ?? []) for (const f of m.footprint) for (const b of w.buildings) {
    if (overlap(b.ring as [number, number][], f) < 0.5) continue;
    if (b.id.startsWith('tower/') && towersOf.get(m.id)!.includes(b.id.split('/')[1] ?? '')) continue; // the megastructure's own gate towers
    bad.push(`${b.id} under ${m.id}`);
  }
  expect(bad).toEqual([]);
  expect(w.megastructures.every(m => m.parts.length > 0)).toBe(true);
});

test('every deck has >= 2 connectors, one of them step-free', () => {
  const decks = w.levels.flatMap(l => l.decks.map(x => ({ id: x.id, level: l.name, rect: bbox(x.ring) })));
  const plats = w.transit.stations.map(s => ({ id: s.id, level: 'concourse', rect: bbox(s.ring) }));
  const groups = deckGroups([...decks, ...plats]);
  expect(decks.length).toBeGreaterThan(20);
  for (const x of decks) {
    const ids = [...groups].filter(([, g]) => g === groups.get(x.id)).map(([id]) => id), cs = w.connectors.filter(c => c.serves.some(s => ids.includes(s)));
    expect(cs.length, x.id).toBeGreaterThanOrEqual(2);
    expect(cs.some(c => c.stepFree), x.id).toBe(true);
  }
  for (const l of w.levels) for (const x of l.decks) expect(x.group).toBe(groups.get(x.id) ?? "");
  expect(gen.issues).toEqual([]);
});

test('deck columns stand clear of carriageways', () => {
  const roads = d.roads.filter(r => !r.gyratory);
  const near = (c: number[], line: [number, number][]) => line.slice(1).reduce((m, b, i) => { const a = line[i]!, abx = b[0] - a[0], aby = b[1] - a[1], t = Math.max(0, Math.min(1, ((c[0]! - a[0]) * abx + (c[1]! - a[1]) * aby) / (abx * abx + aby * aby || 1))); return Math.min(m, Math.hypot(c[0]! - a[0] - abx * t, c[1]! - a[1] - aby * t)); }, Infinity);
  const bad: string[] = [];
  for (const l of w.levels) for (const x of l.decks) for (const c of x.columns) if (!d.zones.some(z => z.kind === 'median' && z.poly && overlap(rectAround(c), bbox(z.poly) as unknown as number[]) > 0)) for (const r of roads) if (near(c, r.line) < r.w / 2) bad.push(`${x.id} ${c} in ${r.id}`);
  expect(bad).toEqual([]);
  expect(w.levels.flatMap(l => l.decks).reduce((n, x) => n + x.columns.length, 0)).toBeGreaterThan(50);
});

test('café and Records Office keep their size', () => {
  const { cafe, 'civic-office': office } = plan.civic.scale as { cafe: { width: number; depth: number; height: number }; 'civic-office': { crown: number } };
  const m = w.civic.find(x => x.id === 'civic/cafe')!, b = bbox(m.ring), dims = [b[1] - b[0], b[3] - b[2]].sort((p, q) => p - q);
  expect(dims[0]).toBeCloseTo(cafe.depth, 1); expect(dims[1]).toBeCloseTo(cafe.width, 1); expect(m.height).toBeCloseTo(cafe.height, 2);
  const tall = Math.max(...w.civic.filter(x => x.id.startsWith('civic/civic-office')).map(x => x.height));
  expect(tall).toBeCloseTo(office.crown, 1);
  for (const cb of civ.buildings) expect(w.civic.find(x => x.id === `civic/${cb.id}`)!.height).toBe(cb.height);
});

test('heights follow H(n) and the area caps; every roof has a programme that fits', () => {
  const B = plan.standards.buildings, kit = B.roofs.kit.programmes;
  for (const r of w.roofs) {
    expect(B.roofs.programmes).toContain(r.programme);
    if (r.form === 'terrace') continue;
    const [mw, md] = kit[r.programme].minRoof as [number, number], [W, D] = r.size;
    expect((W >= mw && D >= md) || (W >= md && D >= mw), `${r.id} ${r.programme} ${W}x${D}`).toBe(true);
  }
  for (const b of gen.blds) { const cap = B.caps[b.area]?.m; if (cap && !b.tower) expect(b.z, b.id).toBeLessThanOrEqual(cap + 0.18 + 1e-6); }
  expect(w.roofs.length).toBe(gen.blds.length + w.roofs.filter(r => r.form === 'terrace').length);
});

test('same plan, same bytes', async () => {
  const a = JSON.stringify(w), b = JSON.stringify(worldOf(plan, derive(plan), civic(plan.civic, read(plan.civic.block))).world);
  expect(b).toBe(a);
  const run = (out: string) => Bun.spawnSync([process.execPath, resolve(root, 'tools/world/export.ts'), '--dev', '--out', out], { cwd: root });
  const o1 = `out/world/test-a.world.json`, o2 = `out/world/test-b.world.json`;
  expect(run(o1).exitCode).toBe(0); expect(run(o2).exitCode).toBe(0);
  expect(readFileSync(resolve(root, o1)).equals(readFileSync(resolve(root, o2)))).toBe(true);
  expect(readFileSync(resolve(root, o1), 'utf8')).toBe(a);
});

test('world type: schema string and every id is unique', () => {
  const ww: World = w, ids = [...ww.buildings, ...ww.civic].map(x => x.id).concat(ww.frontages.map(f => f.id), ww.roofs.map(r => r.id), ww.connectors.map(c => c.id), ww.street.map(s => s.id));
  expect(ww.schema).toBe('suborned.world/2');
  expect(new Set(ids).size).toBe(ids.length);
});

test('the schema example carries every top-level kind of the export', () => {
  const ex = read('contracts/world/harbour-district.world.example.json');
  expect(ex.schema).toBe(w.schema);
  for (const k of Object.keys(w)) expect(Object.keys(ex), k).toContain(k);
  for (const k of ['roofs', 'frontages', 'connectors', 'street']) expect(ex[k].length, k).toBeGreaterThan(0);
});

// ---- plan fields wired into the generator (brief 004: GEN gone, nothing parsed from strings) ----
const O = 1e-6;
const inR = (p: number[], r: number[], m: number) => p[0]! > r[0]! - m && p[0]! < r[1]! + m && p[1]! > r[2]! - m && p[1]! < r[3]! + m;
const rectRing = (p: { c: number[]; size: number[]; yaw: number }): number[][] => { const [a, b] = [p.size[0]! / 2, p.size[1]! / 2], cs = Math.cos(p.yaw), sn = Math.sin(p.yaw); return [[-a, -b], [a, -b], [a, b], [-a, b]].map(([x, y]) => [p.c[0]! + x! * cs - y! * sn, p.c[1]! + x! * sn + y! * cs]); };
const hits = (ring: number[][], r: number[]) => overlap(ring as [number, number][], r) > 0.01;

test('GEN is gone and the generators parse no plan text', () => {
  const src = (f: string) => readFileSync(resolve(root, 'tools/world/gen', f), 'utf8');
  for (const f of ['common.ts', 'masses.ts', 'mega.ts', 'mega/terraces.ts', 'mega/arcade.ts', 'mega/harbourDeck.ts', 'mega/pier.ts', 'mega/kit.ts']) {
    expect(src(f), f).not.toMatch(/\bGEN\b|matchAll|\.exec\(|\.replace\('−'/);
  }
});

test('colonnades: ground floor on the building line, walking zone clear under the overhang, columns in the kerb zone only', () => {
  const ctx = ctxOf(plan, d, civ, PROV.pavement), pav = PROV.pavement, head = plan.standards.levels.headroom.colonnade as number, size = plan.standards.levels.column.size as number;
  let entries = 0, lifted = 0;
  for (const m of plan.megastructures ?? []) {
    const mo = gen.megaOut.find(x => x.id === `mega/${m.id}`)!, wm = w.megastructures.find(x => x.id === `mega/${m.id}`)!;
    for (const c of (m as unknown as { colonnade: Colonnade[] }).colonnade) {
      entries++;
      expect(c.clearHeight, `${m.id} ${c.faces}`).toBeGreaterThanOrEqual(head);
      const fp = m.footprint[c.footprint] as Rect, { kerb, walk } = zonesOf(ctx, fp, c), sec = plan.standards.streetscape.sections[c.class];
      expect(c.depth, `${m.id} ${c.faces} depth = the section's pavement`).toBe(sec.pavement);
      const clear = pav + c.clearHeight - O, bad: string[] = [];
      for (const p of [...wm.parts, ...w.buildings]) if (p.base < clear && hits(p.ring, walk)) bad.push(`${p.id} base ${p.base}`);
      for (const p of wm.prims) if (p.base < clear && !p.id.includes('/colonnade-') && hits(rectRing(p), walk)) bad.push(p.id);
      for (const l of w.levels) for (const dk of l.decks) for (const k of dk.columns) if (inR(k, walk, 0.45)) bad.push(`${dk.id} column ${k}`);
      expect(bad, `${m.id} ${c.faces}: walking zone`).toEqual([]);
      const cols = wm.prims.filter(p => p.id.includes(`/colonnade-${(m as unknown as { colonnade: Colonnade[] }).colonnade.indexOf(c)}-col-`));
      expect(cols.length, `${m.id} ${c.faces}: columns`).toBeGreaterThan(0);
      for (const p of cols) { expect(p.size[0]).toBe(size); expect(inR(p.c as number[], kerb, 0), `${p.id} in the kerb zone`).toBe(true); expect(p.base + p.size[2]).toBeGreaterThanOrEqual(clear); }
      if (c.over.startsWith('upper') || c.over.includes('storey')) { lifted++; expect(wm.parts.some(p => hits(p.ring, stripOf(fp, c)) && p.base >= clear), `${m.id} ${c.faces}: upper storeys overhang`).toBe(true); }
      // the ground floor stands on the building line: nothing reaches the strip from below
      expect(wm.parts.filter(p => p.base < 1 && hits(p.ring, stripOf(fp, c))).map(p => p.id), `${m.id} ${c.faces}: ground floor`).toEqual([]);
    }
  }
  expect(entries).toBe((plan.megastructures ?? []).reduce((n, m) => n + ((m as unknown as { colonnade: unknown[] }).colonnade?.length ?? 0), 0));
  expect(entries).toBeGreaterThanOrEqual(9);
  expect(lifted).toBeGreaterThanOrEqual(2);
});

test('gate towers stand on Harbour Road\'s building line', () => {
  for (const id of ['parade-gate-n', 'parade-gate-s']) for (const b of w.buildings.filter(x => x.id.startsWith(`tower/${id}/`))) expect(Math.max(...b.ring.map(p => p[0])), b.id).toBeLessThanOrEqual(288 - plan.standards.streetscape.sections.primary.pavement + 1e-6);
});

test('every deck group is within maxTravel of a way down, walked', () => {
  const lim = plan.standards.levels.spacing, plats = w.transit.stations.map(s => ({ id: s.id, level: 'concourse', rect: bbox(s.ring) }));
  const decks = w.levels.flatMap(l => l.decks.map(x => ({ id: x.id, level: l.name, rect: bbox(x.ring) })));
  const halls = w.megastructures.flatMap(m => m.parts.filter(p => /\/hall-\d+\/walls$/.test(p.id)).map(p => bbox(p.ring)));
  const groups = deckGroups([...decks, ...plats]), by = new Map<string, typeof decks>();
  for (const p of [...decks, ...plats]) by.set(groups.get(p.id)!, [...(by.get(groups.get(p.id)!) ?? []), p]);
  expect(by.size).toBeGreaterThan(10);
  const seen = new Set(gen.travel.map(t => t.group));
  for (const [g, pcs] of by) {
    const level = pcs[0]!.level, limit = lim[level]?.maxTravel as number | undefined;
    if (!limit) continue;
    expect(seen.has(g), `${g} measured`).toBe(true);
    const ways = w.connectors.filter(c => isWayDown(c.kind, level)).map(c => ({ rect: wayRect(c), serves: c.serves })), r = reach(pcs, ways, halls, limit);
    expect(r.max, `${level} group ${g} worst ${r.worst}`).toBeLessThanOrEqual(limit);
  }
  for (const t of gen.travel) expect(t.max, t.group).toBeLessThanOrEqual(t.limit);
});

test('Gardens Forecourt: the south bank stands in the forecourt, off Garden Walk and south of Circus Road\'s kerb and walk', () => {
  const plot = d.plots.find(p => p.id === 'gardens-forecourt-w') as unknown as { bank: { span: number[]; axisClear: number[]; keepClear: number[][]; bridge: { w: number; from: number; to: number } } };
  const b = plot.bank, bank = w.connectors.filter(c => c.id.startsWith('bank/S1/gardens/r/')), kind = (k: string) => bank.find(c => c.kind === k)!;
  expect(bank.length).toBe(4);
  for (const c of [kind('stairTower'), kind('lift'), bank.find(c => c.id.endsWith('/run-a'))!]) { const r = bbox(c.ring); expect(r[1] <= b.axisClear[0] + O || r[0] >= b.axisClear[1] - O, `${c.id} off the axis`).toBe(true); }
  for (const c of bank) { const r = bbox(c.ring); expect(r[0], c.id).toBeGreaterThanOrEqual(b.span[0] - O); expect(r[1], c.id).toBeLessThanOrEqual(b.span[1] + O); for (const k of b.keepClear) expect(overlap(c.ring as [number, number][], k), `${c.id} keep clear`).toBeLessThan(0.01); }
  expect(bbox(kind('stairTower').ring)[0]).toBeGreaterThanOrEqual(b.axisClear[1] - O); // east of the axis
  const gw = plan.foot.find(f => f.id === 'garden-walk')!, gx: Rect = [gw.pts[0]![0] - gw.w / 2, gw.pts[0]![0] + gw.w / 2, -33, -9];
  for (const c of bank.filter(c => c.kind !== 'escalator' || c.id.endsWith('/run-a'))) expect(overlap(c.ring as [number, number][], gx), `${c.id} on Garden Walk`).toBeLessThan(0.01);
  const br = w.levels.flatMap(l => l.decks).find(x => x.id === 'bank/S1/gardens/r/bridge')!, bb = bbox(br.ring);
  expect(bb[3] - bb[2]).toBeCloseTo(Math.abs(b.bridge.to - b.bridge.from), 1); expect(bb[1] - bb[0]).toBeCloseTo(b.bridge.w, 1);
  for (const s of w.street) if (s.edge.startsWith('bank/S1/gardens/r') || s.edge === 'bank/S1/gardens/r/tower') expect(inR(s.at, [gx[0], gx[1], -33, -14], 0), `${s.id} on Garden Walk`).toBe(false);
});

test('Crown Terraces: the park stair-lift towers are read from the bridges\' joinsRect fields', () => {
  const m = (plan.megastructures ?? []).find(x => x.id === 'crown-terraces')!, rects = (m.bridges as unknown as { joinsRect?: number[] }[]).flatMap(b => (b.joinsRect ? [b.joinsRect] : []));
  expect(rects.length).toBe(3);
  const wm = w.megastructures.find(x => x.id === 'mega/crown-terraces')!;
  for (const r of rects) expect(wm.parts.some(p => /\/tower-\d+/.test(p.id) && overlap(p.ring as [number, number][], r) > 0.9 * (r[1] - r[0]) * (r[3] - r[2])), `tower at ${r}`).toBe(true);
  expect(w.connectors.filter(c => c.id.startsWith('mega/crown-terraces/tower-core-')).length).toBe(6); // a stair and a lift each
});
