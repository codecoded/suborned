// The rival crew's car (brief 067, Vela; Sky on 0.2.122: "people wearing the wrong look confuse him"): the piece the host draws as car/rivals is catalogued, fits the sedans' footprint, and reads as
// the rivals' and neither as Vale's official car nor the cell's getaway car. The reads are numbers here so a later palette change cannot quietly bring the three cars back together: body and roof
// colours at least this far apart (CIEDE2000), a roof darker than the body (the official car's is far lighter, the getaway's about level), none of the other cars' paints, nothing gold, nothing lit.
import { expect, test } from 'bun:test';
import { existsSync, readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import type { Kit } from '../../contracts/world/kit.schema';
import { GEN_ROOT } from '../kit/store';

const kit: Kit = JSON.parse(readFileSync(resolve(import.meta.dir, '../../contracts/world/kit.json'), 'utf8'));
const piece = (id: string) => kit.assets.find(a => a.id === id)!;
const mats = new Map((JSON.parse(readFileSync(join(GEN_ROOT, 'decokit_materials.json'), 'utf8')).materials as { name: string; kind: string; color: number[] }[]).map(m => [m.name, m]));

/** sRGB (0..1) to CIE L*a*b* (D65, 2 degrees), the maths of tools/decokit/deltae.py. */
function lab([r, g, b]: number[]): [number, number, number] {
  const f = (c: number) => (c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4), [R, G, B] = [r, g, b].map(v => f(v!));
  const X = (0.4124 * R! + 0.3576 * G! + 0.1805 * B!) / 0.95047, Y = 0.2126 * R! + 0.7152 * G! + 0.0722 * B!, Z = (0.0193 * R! + 0.1192 * G! + 0.9505 * B!) / 1.08883;
  const h = (t: number) => (t > 0.008856 ? Math.cbrt(t) : 7.787 * t + 16 / 116);
  return [116 * h(Y) - 16, 500 * (h(X) - h(Y)), 200 * (h(Y) - h(Z))];
}
/** CIEDE2000 between two L*a*b* colours (Sharma, Wu and Dalal's formulation). */
function de2000([L1, a1, b1]: number[], [L2, a2, b2]: number[]): number {
  const rad = (d: number) => (d * Math.PI) / 180, deg = (r: number) => (r * 180) / Math.PI;
  const C1 = Math.hypot(a1!, b1!), C2 = Math.hypot(a2!, b2!), Cm = (C1 + C2) / 2, G = 0.5 * (1 - Math.sqrt(Cm ** 7 / (Cm ** 7 + 25 ** 7)));
  const a1p = (1 + G) * a1!, a2p = (1 + G) * a2!, C1p = Math.hypot(a1p, b1!), C2p = Math.hypot(a2p, b2!);
  const h1 = (deg(Math.atan2(b1!, a1p)) + 360) % 360, h2 = (deg(Math.atan2(b2!, a2p)) + 360) % 360;
  const dL = L2! - L1!, dC = C2p - C1p;
  let dh = h2 - h1; if (dh > 180) dh -= 360; else if (dh < -180) dh += 360;
  const dH = 2 * Math.sqrt(C1p * C2p) * Math.sin(rad(dh / 2)), Lp = (L1! + L2!) / 2, Cp = (C1p + C2p) / 2;
  const hp = Math.abs(h1 - h2) <= 180 ? (h1 + h2) / 2 : h1 + h2 < 360 ? (h1 + h2 + 360) / 2 : (h1 + h2 - 360) / 2;
  const T = 1 - 0.17 * Math.cos(rad(hp - 30)) + 0.24 * Math.cos(rad(2 * hp)) + 0.32 * Math.cos(rad(3 * hp + 6)) - 0.2 * Math.cos(rad(4 * hp - 63));
  const dth = 30 * Math.exp(-(((hp - 275) / 25) ** 2)), Rc = 2 * Math.sqrt(Cp ** 7 / (Cp ** 7 + 25 ** 7)), Sl = 1 + (0.015 * (Lp - 50) ** 2) / Math.sqrt(20 + (Lp - 50) ** 2);
  const Rt = -Math.sin(rad(2 * dth)) * Rc, x = dC / (1 + 0.045 * Cp), y = dH / (1 + 0.015 * Cp * T);
  return Math.sqrt((dL / Sl) ** 2 + x ** 2 + y ** 2 + Rt * x * y);
}
const colour = (name: string) => { const m = mats.get(name); if (!m) throw new Error(`${name} is not in decokit_materials.json`); return lab(m.color); };
const hex = (h: string) => lab([1, 3, 5].map(i => parseInt(h.slice(i, i + 2), 16) / 255));

// body and roof of each car, as the generator paints them (tools/decokit/cars020.py and cars067.py)
const CARS = { official: ['DK_Enamel_Teal', 'DK_Enamel_Ivory'], getaway: ['DK_Accent_Coral', 'DK_Enamel_Seafoam'], rival: ['DK_Enamel_Plum', 'DK_Enamel_Ink'] } as const;
const body = (c: keyof typeof CARS) => colour(CARS[c][0]), roof = (c: keyof typeof CARS) => colour(CARS[c][1]);

test('the CIEDE2000 used here is the standard one (Sharma, Wu and Dalal, pair 1: 2.0425)', () => {
  expect(de2000([50, 2.6772, -79.7751], [50, 0, -82.7485])).toBeCloseTo(2.0425, 3);
});

test('Sedan_Rival is catalogued beside the other two: a generated Vehicles piece, built to their footprint at scale 1, at full detail (no decimated levels), never placed by the world', () => {
  const r = piece('Sedan_Rival'), o = piece('Sedan_Official');
  expect(r).toBeDefined(); expect(r.category).toBe('Vehicles'); expect(r.source).toBe('generated'); expect(['candidate', 'adopted']).toContain(r.status);
  expect(r.tags.use).toEqual(['vehicle:car-rivals']); expect(r.tags.scale).toEqual([1, 1]); expect(r.tags.sight).toBe('open');
  expect(Math.abs(r.size[0] - o.size[0]) / o.size[0], 'width').toBeLessThan(0.005); expect(Math.abs(r.size[2] - o.size[2]) / o.size[2], 'length').toBeLessThan(0.01); // the sim's car footprint (rulebook car-len-cm, car-w-cm) is the same for every car
  expect(Math.abs((r.pivotOffset?.[1] ?? 0) - (o.pivotOffset?.[1] ?? 0)), 'pivot offset (m)').toBeLessThan(0.05); expect(r.front).toBe(o.front); expect(r.pivot).toBe(o.pivot);
  expect(r.lods, 'full detail: no LODs').toBeUndefined(); expect(r.files.filter(f => f.role === 'lod')).toEqual([]);
  expect(r.tris, 'a car is one view in the host: character over triangles').toBeGreaterThan(o.tris);
  for (const f of r.files) { const p = join(GEN_ROOT, f.path); if (f.root === 'generated') { expect(existsSync(p), f.path).toBe(true); expect(new Bun.CryptoHasher('sha256').update(readFileSync(p)).digest('hex'), f.path).toBe(f.sha256); } }
  for (const m of ['DK_Enamel_Plum', 'DK_Enamel_Ink', 'DK_Enamel_Pearl']) expect(r.files.some(f => f.path === `Materials/${m}.mat` && f.root === 'generated'), m).toBe(true); // its own livery travels with it into Unity
});

test('livery: the rival body and roof are far from the official and getaway cars\' (CIEDE2000), and the roof is darker than the body, as neither of theirs is', () => {
  const dB = (a: keyof typeof CARS, b: keyof typeof CARS) => de2000(body(a), body(b)), dR = (a: keyof typeof CARS, b: keyof typeof CARS) => de2000(roof(a), roof(b));
  expect(dB('rival', 'official'), 'body vs Vale\'s teal').toBeGreaterThanOrEqual(30); expect(dB('rival', 'getaway'), 'body vs the cell\'s coral').toBeGreaterThanOrEqual(40);
  expect(dR('rival', 'official'), 'roof vs Vale\'s ivory').toBeGreaterThanOrEqual(60); expect(dR('rival', 'getaway'), 'roof vs the cell\'s seafoam').toBeGreaterThanOrEqual(40);
  const read = (c: keyof typeof CARS) => roof(c)[0] - body(c)[0]; // value read: roof lightness minus body lightness (L*)
  expect(read('rival'), 'the rivals\' roof is darker than their body').toBeLessThan(-5); expect(read('official'), 'Vale\'s roof is far lighter than his body').toBeGreaterThan(30); expect(read('getaway')).toBeGreaterThan(-10); // and the getaway's is level
  expect(Math.abs(read('rival') - read('getaway')), 'a value pattern the getaway car does not have').toBeGreaterThanOrEqual(8);
});

test('livery: the rivals\' own colour (their figure\'s jacket, #422F42 in its albedo), never the other cars\' paints, nothing gold or brass, nothing lit beyond the sedans\' lamps', () => {
  expect(de2000(body('rival'), hex('#422F42')), 'plum vs the figures\' jacket').toBeLessThanOrEqual(12);
  expect(de2000(body('rival'), hex('#8A4FD0')), 'the body is not the rivals\' violet signal (those marks draw over the car, unlit)').toBeGreaterThanOrEqual(20);
  const used = piece('Sedan_Rival').materials;
  for (const m of ['DK_Enamel_Ivory', 'DK_Enamel_Cream', 'DK_Enamel_Teal', 'DK_Enamel_Seafoam', 'DK_Accent_Coral', 'DK_Accent_Teal', 'DK_Accent_Mint', 'DK_Enamel_Mustard', 'DK_GlassDark', 'DK_Enamel_Charcoal']) expect(used, `${m} is Vale's or the cell's look`).not.toContain(m);
  for (const m of used) { expect(m, 'gold and brass are hairline, frame and bead; a car has none').not.toMatch(/Gold|Brass/); expect(m).not.toMatch(/^DK_Signal_/); }
  const sibling = new Set([...piece('Sedan_Official').materials, ...piece('Sedan_Getaway').materials]);
  for (const m of used.filter(n => mats.get(n)?.kind === 'emit')) expect(sibling.has(m), `${m} glows: only the sedans' own lamps may`).toBe(true);
});
