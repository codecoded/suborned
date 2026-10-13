// Facade wave 2 and the space-age grammar (brief 007 B): kit facades by storeys with neighbour contrast, signs on the wall, streamline corners,
// speed bands, crowns on every tower, the horizon band, civic kit pieces. Each rule is a number from contracts/world/harbour-district.grammar.md.
import { expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Kit } from '../../../contracts/world/kit.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { worldOf } from '../build';
import { adjacent } from './facades';
import { SIGN_OFF } from './frontage';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const kit: Kit = read('contracts/world/kit.json'), by = new Map(kit.assets.map(a => [a.id, a]));
const { world: w, gen } = worldOf(plan, d, civ), B = plan.standards.buildings;
const H = (n: number) => B.plinth + B.groundStorey + (n - 1) * B.upperStorey + B.roofCrown;

test('every facade piece is adopted, within its scale range and its building\'s H(n)', () => {
  expect(w.facades.length).toBeGreaterThan(300);
  for (const f of w.facades) {
    const a = by.get(f.kit.id)!; expect(a.status, f.kit.id).toBe('adopted');
    const [lo, hi] = a.tags.scale, [sx, sy, sz] = f.kit.scale;
    for (const s of [sx, sy, sz]) { expect(s).toBeGreaterThanOrEqual(lo - 1e-3); expect(s).toBeLessThanOrEqual(hi + 1e-3); }
    expect(sx / sy).toBeGreaterThanOrEqual(a.tags.squash - 1e-3);
    if (f.form === 'bay') expect(Math.abs(a.size[1] * sy - H(f.storeys)) / H(f.storeys), f.id).toBeLessThanOrEqual(0.081);
  }
});

test('no adjacent duplicate facade: buildings sharing a wall never carry the same bay piece', () => {
  const bayOf = new Map<string, string>();
  for (const f of w.facades) if (f.form === 'bay' && !bayOf.has(f.building)) bayOf.set(f.building, f.kit.id);
  const ring = new Map(gen.blds.map(b => [b.id, b.ring]));
  let pairs = 0;
  for (const [a, ida] of bayOf) for (const [b, idb] of bayOf) {
    if (a >= b || a.split('/')[1] !== b.split('/')[1] || !adjacent(ring.get(a)!, ring.get(b)!)) continue;
    pairs++; expect(ida, `${a} and ${b}`).not.toBe(idb);
  }
  expect(pairs).toBeGreaterThan(20);
  for (const [b, id] of bayOf) { // one piece per building, all its bays alike
    for (const f of w.facades.filter(x => x.building === b && x.form === 'bay')) expect(f.kit.id).toBe(id);
  }
});

test('adjacent plot buildings never repeat their storeys', () => {
  const bl = gen.blds.filter(b => !b.tower && b.kind !== 'shed');
  let pairs = 0, same = 0;
  for (const a of bl) for (const b of bl) if (a.id < b.id && a.plot === b.plot && adjacent(a.ring, b.ring)) { pairs++; if (a.n === b.n) same++; }
  expect(pairs).toBeGreaterThan(50); expect(same / pairs).toBeLessThan(0.1); // only where the band offers nothing else
});

test('a facade piece stands on its building ring: its front is on a street edge', () => {
  const ring = new Map(gen.blds.map(b => [b.id, b.ring]));
  for (const f of w.facades) {
    const r = ring.get(f.building)!, i = f.edge, a = r.p[i]!, b = r.p[(i + 1) % r.p.length]!;
    const L = Math.hypot(b[0] - a[0], b[1] - a[1]), dd = (p: number[]) => Math.abs(((p[0]! - a[0]) * (b[1] - a[1]) - (p[1]! - a[1]) * (b[0] - a[0])) / L);
    expect(dd(f.a), f.id).toBeLessThan(0.05); expect(dd(f.b), f.id).toBeLessThan(0.05);
  }
});

test('signs stand 0.8 m off the facade line, freestanding stalls and tower frontages included', () => {
  expect(SIGN_OFF).toBe(0.8);
  for (const f of w.frontages) {
    const n = f.normal, c = f.sign.c, off = (c[0] - f.a[0]) * n[0] + (c[1] - f.a[1]) * n[1];
    expect(off, f.id).toBeGreaterThan(0.75); expect(off, f.id).toBeLessThan(0.85);
  }
  expect(w.frontages.some(f => f.host.startsWith('mega/market-deck'))).toBe(true);
  expect(w.frontages.some(f => f.host.startsWith('mega/skyport-tower'))).toBe(true);
});

test('streamline: street corners are kit corners or rounded arcs, bands stay on the line, setbacks step', () => {
  expect(w.facades.filter(f => f.form === 'corner').length).toBeGreaterThan(15);
  // a procedural building (no kit facade) has no square street-street corner on its body ring where both faces can carry the arc
  const kitB = new Set(w.facades.map(f => f.building)), mass = new Map(w.buildings.map(m => [m.id, m]));
  for (const b of gen.blds) {
    if (b.tower || kitB.has(b.id) || b.kind === 'shed' || b.n < 3) continue;
    const body = mass.get(`${b.id}/body`)!;
    expect(body.ring.length, b.id).toBeGreaterThanOrEqual(b.ring.p.length);
  }
  const bands = w.buildings.filter(m => /\/band-\d+$/.test(m.id));
  expect(bands.length).toBeGreaterThan(50);
  for (const m of bands) expect(m.height).toBe(0.75);
  expect(w.buildings.filter(m => /\/step$/.test(m.id)).length).toBeGreaterThan(5);
});

test('every plot building of 9+ storeys carries a kit crown; plan tower sites keep their own', () => {
  const roofOf = new Map(w.roofs.map(r => [r.building, r]));
  const tall = gen.blds.filter(b => !b.tower && b.n >= 9);
  expect(tall.length).toBeGreaterThan(10);
  const tip = B.towers.crownTip as number;
  for (const b of tall) {
    const c = roofOf.get(b.id)!.crown; expect(c, b.id).toBeDefined();
    const a = by.get(c!.id)!; expect(a.status).toBe('adopted'); expect(a.category).toBe('Crowns');
    expect(c!.scale[1]).toBeGreaterThanOrEqual(0.8 - 1e-3); expect(c!.scale[1]).toBeLessThanOrEqual(1.4 + 1e-3);
    expect(b.z + a.size[1] * c!.scale[1]).toBeLessThanOrEqual(tip + 1e-3);
    expect(a.size[0] * c!.scale[0]).toBeLessThanOrEqual(0.7 * Math.min(...roofOf.get(b.id)!.size) + 0.01);
  }
  expect(w.roofs.filter(r => r.crown).length).toBe(tall.length);
});

test('horizon band: rings, sea sector, clearance, spacing', () => {
  const h = w.horizon, ps = h.pieces, ring = (r: string) => ps.filter(p => p.ring === r);
  expect([ring('A').length, ring('B').length, ring('C').length]).toEqual([40, 60, 80]);
  expect(ring('shore').length).toBeGreaterThan(10); expect(ring('rocket-pad').length).toBe(3); expect(ring('rocket-ascent').length).toBe(2); expect(ring('planet').length).toBe(1); expect(ring('moon').length).toBe(2);
  expect(h.sea.size).toBe(16000);
  const az = (p: { kit: { at: number[] } }) => (Math.atan2(p.kit.at[1]! - h.centre[1], p.kit.at[0]! - h.centre[0]) * 180) / Math.PI;
  for (const p of [...ring('A'), ...ring('B'), ...ring('C')]) expect(Math.abs(az(p)), p.id).toBeGreaterThan(60);
  for (const p of ring('shore')) expect(Math.abs(az(p)), p.id).toBeLessThanOrEqual(60 + 1e-6);
  const near = ps.filter(p => !['planet', 'moon', 'rocket-ascent'].includes(p.ring));
  for (const p of near) { const [x, y] = p.kit.at, gx = Math.max(plan.sheet.x[0] - x, 0, x - plan.sheet.x[1]), gy = Math.max(plan.sheet.y[0] - y, 0, y - plan.sheet.y[1]); expect(Math.hypot(gx, gy), p.id).toBeGreaterThanOrEqual(150); }
  for (let i = 0; i < near.length; i++) for (let j = i + 1; j < near.length; j++) expect(Math.hypot(near[i]!.kit.at[0] - near[j]!.kit.at[0], near[i]!.kit.at[1] - near[j]!.kit.at[1])).toBeGreaterThanOrEqual(60 - 1e-6);
  for (const p of ps) expect(by.get(p.kit.id)!.status).toBe('adopted');
  expect(ps.length).toBeGreaterThan(180); expect(ps.length).toBeLessThan(240);
});

test('horizon land and sea: the sea is the bay sector only, land under every inland piece, the coast meets the plan water at the district edge', () => {
  const h = w.horizon, sea = h.sea.sectors, land = h.land, inR = (p: number[], r: number[][]) => { let c = false; for (let i = 0, j = r.length - 1; i < r.length; j = i++) { const a = r[i]!, b = r[j]!; if ((a[1]! > p[1]!) !== (b[1]! > p[1]!) && p[0]! < ((b[0]! - a[0]!) * (p[1]! - a[1]!)) / (b[1]! - a[1]!) + a[0]!) c = !c; } return c; };
  expect(sea.length).toBe(1); expect(land.map(f => f.id)).toEqual(['horizon/land/west', 'horizon/land/north', 'horizon/land/south']);
  for (const f of land) { expect(f.surface).toBe('beyond'); expect(f.level).toBe(-0.02); } // the district edge's ground (PROV.beyond, the plan's own beyond strips)
  // every piece standing on the ground (rings A-C, the far shore, the pads) stands on land, never on the sea or inside the sheet
  for (const p of h.pieces.filter(q => !['planet', 'moon', 'rocket-ascent'].includes(q.ring))) {
    expect(inR(p.kit.at, sea[0]!.ring), p.id).toBe(false); expect(land.filter(f => inR(p.kit.at, f.ring)).length, p.id).toBe(1);
  }
  // the coast leaves the plan water at its corners on the district edge, and the sea opens east from there (az within +-60 of +X)
  const r = sea[0]!.ring; expect(r[0]).toEqual([plan.sheet.x[1], plan.sheet.y[0]]); expect(r[r.length - 1]).toEqual([plan.sheet.x[1], plan.sheet.y[1]]);
  for (const q of r) { const a = (Math.atan2(q[1] - h.centre[1], q[0] - h.centre[0]) * 180) / Math.PI; expect(Math.abs(a)).toBeLessThanOrEqual(60 + 1e-6); }
  // a partition: land and sea never overlap (a grid of probes outside the sheet hits exactly one)
  for (let x = -7800; x <= 8100; x += 450) for (let y = -8200; y <= 7700; y += 450) {
    if (x >= plan.sheet.x[0] && x <= plan.sheet.x[1] && y >= plan.sheet.y[0] && y <= plan.sheet.y[1]) continue;
    expect([sea[0]!, ...land].filter(f => inR([x, y], f.ring)).length, `${x},${y}`).toBe(1);
  }
  expect(h.banks.length).toBeGreaterThan(4); for (const b of h.banks) expect(b.base + b.height).toBeLessThan(-0.02);
});

test('dressed connectors: every stair tower and open lift carries its dressed form inside its footprint (rounded ends, band pairs, chrome cap)', () => {
  const towers = w.connectors.filter(k => k.kind === 'stairTower'), tubes = w.connectors.filter(k => /^mega\/skyport-tower\/tube-/.test(k.id)), banks = w.connectors.filter(k => k.kind === 'lift' && k.id.startsWith('bank/'));
  expect(towers.length).toBeGreaterThanOrEqual(8); expect(tubes.length).toBe(3); expect(banks.length).toBeGreaterThanOrEqual(8);
  for (const k of [...towers, ...tubes, ...banks]) {
    expect(k.dress?.length, k.id).toBeGreaterThan(5);
    const xs = k.ring.map(p => p[0]), ys = k.ring.map(p => p[1]), bx = [Math.min(...xs) - 0.26, Math.max(...xs) + 0.26, Math.min(...ys) - 0.26, Math.max(...ys) + 0.26];
    for (const p of k.dress!) {
      const half = p.shape === 'box' ? Math.hypot(p.size[0], p.size[1]) / 2 : p.size[0] / 2, inBox = p.c[0] - (p.shape === 'box' ? 0 : half) >= bx[0]! && p.c[0] + (p.shape === 'box' ? 0 : half) <= bx[1]! && p.c[1] - (p.shape === 'box' ? 0 : half) >= bx[2]! && p.c[1] + (p.shape === 'box' ? 0 : half) <= bx[3]!;
      expect(inBox, p.id).toBe(true); expect(p.base, p.id).toBeGreaterThanOrEqual(k.bottom - 1e-6); expect(p.base + p.size[2], p.id).toBeLessThanOrEqual(k.top + 2.5);
    }
    expect(k.dress!.some(p => p.surface === 'glass'), k.id).toBe(true); expect(k.dress!.some(p => p.surface === 'trim'), k.id).toBe(true);
  }
  for (const k of towers) { // rounded ends and at least two band pairs in the transit accent
    expect(k.dress!.filter(p => p.shape === 'cyl' && /\/glass\/end-/.test(p.id)).length).toBe(2);
    expect(k.dress!.filter(p => /\/band-\d+\/accent\/mid$/.test(p.id) && p.surface === 'beam').length, k.id).toBeGreaterThanOrEqual(2);
  }
  // lifts inside a stair core or a leg keep the enclosure as their face
  expect(w.connectors.filter(k => k.kind === 'lift' && /\/core-\d+\/lift$|\/lift-(sw|ne)$/.test(k.id)).every(k => !k.dress)).toBe(true);
});

test('the Skyport lifts: leg cars to the frame top, then glazed tubes on the stem to the discs and the gallery', () => {
  const m = plan.megastructures!.find(x => x.id === 'skyport-tower') as any, k = (id: string) => w.connectors.find(c => c.id === `mega/skyport-tower/${id}`)!;
  for (const id of ['lift-sw', 'lift-ne']) { expect(k(id).bottom).toBe(0.18); expect(k(id).top).toBe(m.frame[1]); }
  m.discs.forEach((dk: any, i: number) => {
    const t = k(`tube-${i}`), c = t.foot, r = Math.hypot(c[0] - 545, c[1] - 0);
    expect(t.bottom).toBe(m.frame[1]); expect(r).toBeCloseTo(9.2, 2); expect(r - 1.5 - m.stem.r).toBeLessThanOrEqual(0.75);
    expect(t.top).toBe(dk.top === Math.max(...m.discs.map((x: any) => x.top)) ? m.gallery.z : dk.top);
  });
});

test('procedural skins: every plot building\'s walls follow the grammar (no curtain wall, glass <= 55%, panes <= 4 m, portholes, accent bands)', () => {
  const plain = gen.blds.filter(b => !b.tower), skin = new Map(w.skins.map(k => [k.building, k]));
  for (const b of plain) expect(skin.has(b.id), b.id).toBe(true);
  for (const k of w.skins) {
    expect(['ribbon', 'porthole', 'balcony', 'shed']).toContain(k.family); expect(k.glass.tint).toBe('clear'); expect(k.glass.share).toBeLessThanOrEqual(0.55); expect(k.glass.maxPane).toBeLessThanOrEqual(4);
    expect(['tower', 'awning', 'mega', 'sign']).toContain(k.accent);
    for (const f of k.faces) {
      expect(f.glass, `${f.mass}/${f.edge}`).toBeLessThanOrEqual(0.55); expect(f.bays).toMatch(/^[RPB-]+$/); expect(f.rows.length).toBeGreaterThan(0);
      const L = Math.hypot(f.b[0] - f.a[0], f.b[1] - f.a[1]); expect(Math.min(k.glass.share * L / f.bays.length, k.glass.maxPane)).toBeLessThanOrEqual(4);
    }
    for (const m of w.buildings.filter(x => x.id.startsWith(`${k.building}/band-`))) expect(m.surface, m.id).toBe(k.accent); // the speed bands carry the one accent (§2)
  }
  expect(w.skins.flatMap(k => k.faces).filter(f => f.portholes.length).length).toBeGreaterThan(100); // end walls carry a porthole column
  let pairs = 0, same = 0;
  for (const a of plain) for (const b of plain) if (a.id < b.id && a.plot === b.plot && adjacent(a.ring, b.ring)) { pairs++; const x = skin.get(a.id)!, y = skin.get(b.id)!; if (x.family === y.family && x.accent === y.accent) same++; }
  expect(pairs).toBeGreaterThan(50); expect(same / pairs).toBeLessThan(0.05); // §7 neighbours differ
});

test('civic square: fountain, cafe sets, palms, pavilions, piers and cabinets are kit pieces at the block\'s footprints', () => {
  const kitted = w.civic.filter(m => m.kit);
  expect(kitted.length).toBeGreaterThanOrEqual(20);
  const fountain = w.civic.find(m => m.id === 'civic/atom-fountain')!;
  expect(fountain.kit!.id).toBe('AtomFountain'); expect(fountain.kit!.scale[0]).toBeGreaterThan(0.95); expect(fountain.kit!.scale[0]).toBeLessThan(1.05);
  for (const m of kitted) expect(by.get(m.kit!.id)!.status).toBe('adopted');
});

test('wave 2 is deterministic', () => {
  const again = worldOf(plan, d, civ).world;
  expect(JSON.stringify([again.facades, again.horizon, again.roofs, again.buildings, again.civic])).toBe(JSON.stringify([w.facades, w.horizon, w.roofs, w.buildings, w.civic]));
});
