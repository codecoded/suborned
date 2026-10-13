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
import { HORIZON_CENTRE, ROCKETS, footprint, simple } from './horizon';
import { ACCENT_ROLES, COLOUR, FABRIC_SURFACES } from './colour';
import { civicClearance } from '../build';

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
  for (const f of land) { expect(f.surface).toBe('beyond_ground'); expect(f.level).toBe(-0.02); } // the district edge's ground level (PROV.beyond), its own role apart from the plan's beyond strips
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

// ---- the land beyond (brief 007 round 3): a natural coast, suburban ground with parks, a promenade, terraced hills; nothing floats ----
const inRing = (p: number[], r: number[][]) => { let c = false; for (let i = 0, j = r.length - 1; i < r.length; j = i++) { const a = r[i]!, b = r[j]!; if ((a[1]! > p[1]!) !== (b[1]! > p[1]!) && p[0]! < ((b[0]! - a[0]!) * (p[1]! - a[1]!)) / (b[1]! - a[1]!) + a[0]!) c = !c; } return c; };
const hillTiers = () => { const m = new Map<string, typeof w.horizon.hills>(); for (const t of w.horizon.hills) { const k = t.id.replace(/\/tier-\d+$/, ''); m.set(k, [...(m.get(k) ?? []), t]); } return [...m.values()]; };

test('horizon coast: seeded natural shape (curved bay, mole, headlands), simple rings, still inside the bay sector', () => {
  const h = w.horizon, sea = h.sea.sectors[0]!.ring, [sx1, sy0, sy1] = [plan.sheet.x[1], plan.sheet.y[0], plan.sheet.y[1]];
  for (const r of [sea, ...h.land.map(f => f.ring)]) expect(simple(r as [number, number][])).toBe(true);
  // distance of each coast vertex from the straight +-60 ray out of its corner: the bay bows into the land near the district, headlands reach into the sea
  const off = (p: number[], o: number[], deg: number) => (p[0]! - o[0]!) * Math.sin(deg * Math.PI / 180) - (p[1]! - o[1]!) * Math.cos(deg * Math.PI / 180); // > 0: clockwise of the ray
  const north = sea.filter(p => p[1] > -230 && Math.hypot(p[0] - h.centre[0], p[1] - h.centre[1]) < 4990), south = sea.filter(p => p[1] < -230 && Math.hypot(p[0] - h.centre[0], p[1] - h.centre[1]) < 4990);
  const nOff = north.map(p => off(p, [sx1, sy1], 60)), sOff = south.map(p => -off(p, [sx1, sy0], -60)); // both > 0 = into the sea
  expect(Math.min(...nOff)).toBeLessThan(-80); expect(Math.min(...sOff)).toBeLessThan(-80); // the curved bay
  expect(Math.max(...nOff)).toBeGreaterThan(200); expect(Math.max(...sOff)).toBeGreaterThan(200); // headlands
  expect(north.length).toBeGreaterThan(40); expect(south.length).toBeGreaterThan(40);
  // the mole: a narrow arm of land reaching >= 250 m into the sea near the district (its top is the promenade/mole patch)
  const mole = h.patches.find(f => f.id === 'horizon/promenade/mole')!; expect(mole).toBeDefined();
  // its ring runs root (A) -> out -> head -> back -> root (B); its centreline = midpoints of matching vertices, all on land, the head >= 250 m out
  const mr = mole.ring, mid = mr.slice(0, Math.floor(mr.length / 2)).map((p, i) => [(p[0] + mr[mr.length - 1 - i]![0]) / 2, (p[1] + mr[mr.length - 1 - i]![1]) / 2]);
  expect(Math.max(...mr.map(p => Math.hypot(p[0] - mid[0]![0]!, p[1] - mid[0]![1]!)))).toBeGreaterThan(250);
  expect(Math.min(...mr.map(p => Math.hypot(p[0] - sx1, p[1] - sy0)))).toBeLessThan(800);
  for (const p of mid) expect(inRing(p, sea), `${p}`).toBe(false);
  expect(inRing(mid[mid.length - 1]!, sea)).toBe(false); expect(Math.max(...mr.map(p => -off(p, [sx1, sy0], -60)))).toBeGreaterThan(150); // the head stands well out in the bay
  // a denser partition probe near the district, where the coast is most shaped
  for (let x = 760; x <= 3000; x += 37) for (let y = -3200; y <= 2700; y += 41) expect([sea, ...h.land.map(f => f.ring)].filter(r => inRing([x, y], r)).length, `${x},${y}`).toBe(1);
});

test('horizon land: ground, green and hill roles; parks and promenade over the land, never on the sea, the hills or each other', () => {
  const h = w.horizon, sea = h.sea.sectors[0]!.ring, [sx0, sx1] = plan.sheet.x, [sy0, sy1] = plan.sheet.y;
  const green = h.patches.filter(f => f.surface === 'beyond_green'), prom = h.patches.filter(f => f.id.startsWith('horizon/promenade/'));
  expect(green.filter(f => f.id.startsWith('horizon/park/')).length).toBeGreaterThanOrEqual(40); expect(green.filter(f => f.id.startsWith('horizon/trees/')).length).toBeGreaterThanOrEqual(25);
  expect(prom.map(f => f.id).sort()).toEqual(['horizon/promenade/mole', 'horizon/promenade/north', 'horizon/promenade/south']); for (const f of prom) expect(f.surface).toBe('quay');
  expect(new Set(h.patches.map(f => f.surface))).toEqual(new Set(['beyond_green', 'quay'])); expect(new Set(h.hills.map(m => m.surface))).toEqual(new Set(['beyond_hill']));
  for (const f of h.patches) {
    expect(f.level, f.id).toBeGreaterThan(-0.02); expect(f.level, f.id).toBeLessThan(0.2); expect(simple(f.ring as [number, number][]), f.id).toBe(true);
    for (const p of f.ring) {
      if (f.id !== 'horizon/promenade/mole') expect(inRing(p, sea), `${f.id} ${p}`).toBe(false); // the mole's ring is its coast (checked above by its centreline)
      expect(p[0] >= sx0 && p[0] <= sx1 && p[1] >= sy0 && p[1] <= sy1, `${f.id} ${p} in the sheet`).toBe(false);
      if (f.surface === 'beyond_green') for (const t of h.hills.filter(m => m.id.endsWith('/tier-0'))) expect(inRing(p, t.ring), `${f.id} on ${t.id}`).toBe(false);
    }
  }
  for (let i = 0; i < green.length; i++) for (let j = i + 1; j < green.length; j++) for (const p of green[i]!.ring) expect(inRing(p, green[j]!.ring), `${green[i]!.id} / ${green[j]!.id}`).toBe(false);
  // hills: 3-5 nested terraces, each base inside the tier below (no riser floats), rising with distance toward the far ring, all on dry land
  const hs = hillTiers(); expect(hs.length).toBeGreaterThanOrEqual(20);
  const top = (t: { base: number; height: number }) => t.base + t.height, rOf = (t: { ring: number[][] }) => Math.hypot(t.ring[0]![0]! - h.centre[0], t.ring[0]![1]! - h.centre[1]);
  for (const tiers of hs) {
    expect(tiers.length).toBeGreaterThanOrEqual(3); expect(tiers.length).toBeLessThanOrEqual(5); expect(tiers[0]!.base).toBeLessThan(-0.02);
    tiers.forEach((t, k) => {
      for (const p of t.ring) { expect(inRing(p, sea), t.id).toBe(false); if (k > 0) expect(inRing(p, tiers[k - 1]!.ring), `${t.id} inside the tier below`).toBe(true); }
      if (k > 0) { expect(t.base, t.id).toBeLessThanOrEqual(top(tiers[k - 1]!)); expect(top(t), t.id).toBeGreaterThan(top(tiers[k - 1]!)); }
    });
  }
  const near = hs.filter(t => rOf(t[0]!) < 4000).map(t => top(t[t.length - 1]!)), farH = hs.filter(t => rOf(t[0]!) >= 5000).map(t => top(t[t.length - 1]!));
  expect(farH.reduce((a, b) => a + b, 0) / farH.length).toBeGreaterThan(near.reduce((a, b) => a + b, 0) / near.length); // rising toward the far ring
});

test('every horizon ground piece stands on its land or terrace height over its whole footprint (no floating base, never on the water)', () => {
  const h = w.horizon, sea = h.sea.sectors[0]!.ring, hs = hillTiers();
  const terrain = (p: number[]) => { let z = -0.02; for (const tiers of hs) for (const t of tiers) if (inRing(p, t.ring)) z = Math.max(z, t.base + t.height); return z; };
  const ground = h.pieces.filter(p => !['planet', 'moon', 'rocket-ascent'].includes(p.ring)); let onHill = 0;
  for (const p of ground) {
    const a = by.get(p.kit.id)!, rad = (Math.hypot(a.size[0], a.size[2]) / 2) * p.kit.scale[0];
    for (const q of footprint(p.kit.at as [number, number], rad)) { expect(inRing(q, sea), `${p.id} over the water`).toBe(false); expect(Math.abs(terrain(q) - p.base), `${p.id} at ${q}`).toBeLessThan(1e-3); }
    if (p.base > 0) onHill++;
  }
  expect(onHill).toBeGreaterThanOrEqual(1);
  for (const p of h.pieces.filter(q => q.ring === 'rocket-ascent' || q.ring === 'planet' || q.ring === 'moon')) expect(p.base).toBeGreaterThan(100); // in the sky by design
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
    expect(ACCENT_ROLES.map(r => r[0])).toContain(k.accent); // grammar §2 round 3: the accent_* roles
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
  expect(kitted.length).toBeGreaterThanOrEqual(27);
  const fountain = w.civic.find(m => m.id === 'civic/atom-fountain')!;
  expect(fountain.kit!.id).toBe('AtomFountain'); expect(fountain.kit!.scale[0]).toBeGreaterThan(0.95); expect(fountain.kit!.scale[0]).toBeLessThan(1.05);
  for (const m of kitted) expect(by.get(m.kit!.id)!.status).toBe('adopted');
});

test('wave 2 is deterministic', () => {
  const again = worldOf(plan, d, civ).world;
  expect(JSON.stringify([again.facades, again.horizon, again.roofs, again.buildings, again.civic])).toBe(JSON.stringify([w.facades, w.horizon, w.roofs, w.buildings, w.civic]));
});

test('civic furniture and the steward stand at person scale; the monument planters and the robot are kit pieces (Latch round 3)', () => {
  for (const m of w.civic.filter(x => /cafe-(table|palm)-\d+$/.test(x.id))) expect(m.kit!.scale, m.id).toEqual([1, 1, 1]);
  const robot = w.civic.find(m => m.id === 'civic/foreground-civic-robot')!;
  expect(robot.kit!.id).toBe('RobotButler'); expect(robot.kit!.scale[0]).toBe(1);
  expect(robot.kit!.base + by.get('RobotButler')!.size[1]).toBeCloseTo(robot.base + robot.height, 2); // on a plinth to the monument's height
  for (const id of ['civic/civic-monument-west', 'civic/civic-monument-east']) expect(w.civic.find(m => m.id === id)!.kit!.id).toBe('OvalPlanter');
  expect(civicClearance(w.civic)).toEqual([]); // round 3 wrap-up: the two palms whose fronds reached the café wall slide out (build.ts clearWalls)
  for (const id of ['civic/cafe-palm-0', 'civic/cafe-palm-1']) { // 0.35 m off the café's east wall (plan y -219.3), still the door's pair, inside the terrace before the table row
    const m = w.civic.find(x => x.id === id)!; expect(m.kit!.at[1]).toBeCloseTo(-220.35, 3); expect(m.ring.every(p => p[1] < -219.3 - 0.3)).toBe(true);
  }
});

test('the four fountain-garden beds are PlanterArc (Vela round 3): pivot 6.12 m out from the fountain centre, front (outer curve) away from it; Vela\'s block-frame yaws + 90°', () => {
  const f = w.civic.find(m => m.id === 'civic/atom-fountain')!, xs = f.ring.map(p => p[0]), ys = f.ring.map(p => p[1]), fc = [(Math.max(...xs) + Math.min(...xs)) / 2, (Math.max(...ys) + Math.min(...ys)) / 2];
  const block: Record<string, number> = { 'north-east': 224.94, 'north-west': 315.06, 'south-west': 45.06, 'south-east': 134.94 }; // Vela's yaws (block frame: x south, y east), ±0.06° measurement
  for (const [k, by] of Object.entries(block)) {
    const m = w.civic.find(x => x.id === `civic/fountain-garden-${k}`)!, at = m.kit!.at, y = m.kit!.yaw!;
    expect(m.kit!.id).toBe('PlanterArc'); expect(m.kit!.scale).toEqual([1, 1, 1]); expect(m.source).toBe('kit');
    expect(Math.hypot(at[0] - fc[0]!, at[1] - fc[1]!)).toBeCloseTo(6.12, 2);
    const front = [-Math.sin(y), Math.cos(y)], out = [(at[0] - fc[0]!) / 6.12, (at[1] - fc[1]!) / 6.12];
    expect(front[0]! * out[0]! + front[1]! * out[1]!).toBeCloseTo(1, 3); // kit +Z (the outer curved side) faces away from the fountain
    expect(Math.abs((((y * 180) / Math.PI - (by + 90)) % 360 + 540) % 360 - 180)).toBeLessThan(0.1); // export yaw = block yaw + 90°
    const ring = m.ring.map(p => Math.hypot(p[0] - fc[0]!, p[1] - fc[1]!)); expect(Math.min(...ring)).toBeGreaterThan(5.3); expect(Math.max(...ring)).toBeLessThan(7.1); // the bed is the band r 5.36-7.06
  }
});

test('colour life (grammar §2a): accent roles, 20-30% lower share on the facades held to it, >= 4 elements, fabric never red', () => {
  const roles = new Set(ACCENT_ROLES.map(r => r[0])), tg = w.skins.filter(k => k.share.target);
  expect(tg.length).toBeGreaterThan(150);
  for (const k of w.skins) { expect(roles.has(k.accent)).toBe(true); expect(roles.has(k.accent2)).toBe(true); expect(k.accent2).not.toBe(k.accent); expect(k.trim).toBe('accent_gold'); }
  // never over the band, but one: harbour-boulevard/13 (porthole family) at its lightest (no piers, no spandrels, bulkhead 0.6) is faience 0.134 + porthole panels (3a)
  // 0.110 + entrance fins 0.093 = 0.338, every element mandatory under §2a; open with Vela (which gives: the panels, the fins or a lower bulkhead)
  expect(tg.filter(k => k.share.lower! > 0.3).map(k => k.building)).toEqual(['bld/harbour-boulevard/13']);
  expect(tg.filter(k => k.share.lower! < 0.2).map(k => k.building)).toEqual([]); // §2a 3a: the porthole panels lift the 12 fronts at 0.15-0.2 of round 3; none is left under
  for (const k of w.skins) { expect(k.porthole.panel).toEqual({ w: 1.5, h: 1.5, surface: k.accent }); if (k.faces.some(f => f.bays.includes('P') || f.portholes.length)) expect(k.share.elements).toContain('porthole-panels'); }
  expect(tg.filter(k => k.share.elements.length >= 4).length / tg.length).toBeGreaterThanOrEqual(0.95);
  for (const f of w.skins.flatMap(k => k.fins)) { expect(COLOUR.fin.roles).toContain(f.surface as 'accent_coral'); expect(f.bead.surface).toBe('accent_gold'); expect(f.z1 - f.z0).toBeGreaterThanOrEqual(3); }
  const F = w.fabric, deck = 0.18 + B.plinth + B.groundStorey + B.upperStorey;
  expect(F.awnings.length).toBeGreaterThan(200); expect(F.banners.length).toBeGreaterThan(30); expect(F.flags.length).toBeGreaterThan(20);
  for (const s of [...F.awnings.flatMap(a => a.stripes), ...F.banners.flatMap(b => [b.surface, b.border.surface]), ...F.flags.map(f => f.surface)]) expect(FABRIC_SURFACES.has(s), s).toBe(true);
  for (const a of F.awnings) expect(gen.blds.find(b => b.id === a.building)!.use).toBe('residential');
  for (const b of F.banners) expect(b.z0).toBeGreaterThanOrEqual(deck); // above the deck line
  for (const k of ['parade', 'hotel', 'civic'] as const) expect(F.flags.filter(f => f.kind === k).length, k).toBeGreaterThan(0);
  const ff = F.flags; for (let i = 0; i < ff.length; i++) for (let j = i + 1; j < ff.length; j++) expect(Math.hypot(ff[i]!.at[0] - ff[j]!.at[0], ff[i]!.at[1] - ff[j]!.at[1])).toBeGreaterThan(0.5);
});

test('rockets (grammar §9 round 3): >= 3.2 km from the centre, hazed >= 0.7, clear of the sightline through SpaceHQ', () => {
  const C = HORIZON_CENTRE, R = Math.PI / 180, rk = w.horizon.pieces.filter(p => p.ring === 'rocket-pad' || p.ring === 'rocket-ascent');
  expect(rk.length).toBe(ROCKETS.pads.length + ROCKETS.ascents.length);
  const hq = w.signatures.find(s => s.id === 'signature/space-hq')!, hqAt = hq.kit!.at, hqAz = Math.atan2(hqAt[1] - C[1], hqAt[0] - C[0]) / R;
  expect(hqAz).toBeCloseTo(108, 0);
  const sep = (a: number, b: number) => Math.abs((((a - b) % 360) + 540) % 360 - 180);
  for (const p of rk) {
    const [x, y] = p.kit.at, r = Math.hypot(x - C[0], y - C[1]), az = Math.atan2(y - C[1], x - C[0]) / R;
    expect(r, p.id).toBeGreaterThanOrEqual(ROCKETS.minR); expect(p.haze, p.id).toBeGreaterThanOrEqual(0.7);
    expect(sep(az, hqAz), p.id).toBeGreaterThan(ROCKETS.clear); expect(sep(az, hqAz + 180), p.id).toBeGreaterThan(ROCKETS.clear);
    // the play camera (orthographic, compass yaw 30): no rocket stands in SpaceHQ's screen column (its 80 m ground plus a 100 m margin) ahead of the camera
    const cy = w.view.yaw * R, d = [Math.sin(cy), Math.cos(cy)], rel = [x - hqAt[0], y - hqAt[1]];
    if (rel[0]! * d[0]! + rel[1]! * d[1]! > 0) expect(Math.abs(rel[0]! * d[1]! - rel[1]! * d[0]!), p.id).toBeGreaterThan(40 * Math.SQRT2 + 100);
    // perspective eyes round the district (the flyby's wide shots stand inside the sheet, 300+ m from SpaceHQ): the rocket is never within SpaceHQ's angular half-width + 2°
    for (const e of [[-400, 320], [-400, -780], [740, -780], [170, -780], [-400, -230], [170, 320], [-159, -232.5], [500, -400]] as [number, number][]) {
      const toHq = Math.atan2(hqAt[1] - e[1], hqAt[0] - e[0]) / R, toR = Math.atan2(y - e[1], x - e[0]) / R, half = Math.atan2(40 * Math.SQRT2, Math.hypot(hqAt[0] - e[0], hqAt[1] - e[1])) / R;
      expect(sep(toR, toHq), `${p.id} from ${e}`).toBeGreaterThan(half + 2);
    }
  }
});
