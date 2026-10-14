// SF-21 (brass is thin): what it measures, what it leaves out, and that it fails on the export as it was before the large parts took their own keys.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { World } from '../../../contracts/world/world.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { ctxOf, generated } from '../../plan/world-ctx';
import { CANOPY, BOOTH, CRANE, GIRDER, GLOW, HOOP, INLAY, ORB, OWN_KEYS, PLATE, TUB } from '../gen/surfaces';
import { broad, broadParts, brassParts, faceOf, trimRules } from './trim';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const base = generated(plan, d, civ);
/** SF-21 on a clone of the world with `mutate` applied. */
const sf21 = (mutate: (w: World) => void = () => {}) => {
  const w = structuredClone(base.w) as World;
  mutate(w);
  return { w, check: trimRules(ctxOf(plan, d, civ, { w, gen: base.gen }))[0]! };
};
/** Every prim and street item the generator moved off `trim`, wherever it is in the export, with the key it draws. */
const owned = (w: World, keys: readonly string[] = OWN_KEYS) => [...w.street, ...w.megastructures.flatMap(m => m.prims), ...w.connectors.flatMap(k => k.dress ?? [])].filter(x => keys.includes(x.surface));
const LARGE = [CANOPY, PLATE, BOOTH, TUB, CRANE, GIRDER, HOOP]; // the seven keys the large parts took on 14 Oct (482 parts); a crossing's glow, inlay and orb keys came with brief 009

describe('SF-21 the measure', () => {
  test('a face is broad when its shorter side is wider than 0.6 m and it is at least 1 m²; 0.6 m exactly is thin', () => {
    expect(broad(faceOf('box', [0.3, 0.6, 29.37]))).toBe(false); // a stair tower fin
    expect(broad(faceOf('box', [0.6, 0.6, 4.5]))).toBe(false); // a public clock
    expect(broad(faceOf('box', [0.612, 0.612, 4.5]))).toBe(true); // 1 mm of export rounding is allowed, 12 are not
    expect(broad(faceOf('box', [1, 0.9, 0.5]))).toBe(false); // wide enough, under 1 m²
    expect(broad(faceOf('box', [2.4, 0.2, 2.4]))).toBe(true); // a sheet is broad by its face
    expect(broad(faceOf('box', [7.3, 2.6, 0.25]))).toBe(true); // a stair tower band slab
    expect(broad(faceOf('cyl', [0.4, 0.4, 10]))).toBe(false); // a lamp pole
    expect(broad(faceOf('sphere', [6, 6, 6]))).toBe(true);
  });

  test('a vault is the shell of an arch: a 0.12 m rib is thin, the glazed barrel it carries is not', () => {
    expect(broad(faceOf('vault', [0.12, 5, 1.25]))).toBe(false);
    expect(broad(faceOf('vault', [121.4, 5, 1.25]))).toBe(true);
  });
});

describe('SF-21 on the live world', () => {
  test('passes: every brass part, the 8 tower fins, 42 ribs and 44 mullions among them, is thin', () => {
    const { check } = sf21();
    expect(check.ok, check.detail).toBe(true);
    const parts = brassParts(base.w);
    expect(parts.length).toBeGreaterThan(1000);
    expect(parts.filter(p => /\/dress\/fin$/.test(p.id)).length).toBe(8); // 0.3 x 0.6 x 29 m
    expect(parts.filter(p => /\/rib-\d+$/.test(p.id)).length).toBe(42);
    expect(parts.filter(p => /\/mullion-\d$/.test(p.id)).length).toBe(44);
  });

  test('fails on the export as it was: each key put back to trim names its parts (482 in all)', () => {
    const per: [string, number][] = [[CANOPY, 2], [PLATE, 142], [BOOTH, 64], [TUB, 198], [CRANE, 8], [GIRDER, 8], [HOOP, 60]];
    for (const [key, n] of per) {
      const { w, check } = sf21(w => { for (const x of owned(w)) if (x.surface === key) x.surface = 'trim'; });
      expect(broadParts(w).length, key).toBe(n);
      expect(check.ok, key).toBe(false);
      expect(check.detail, key).toContain(`${n} fail: `);
    }
    expect(per.reduce((t, [, n]) => t + n, 0)).toBe(482);
    const all = sf21(w => { for (const x of owned(w, LARGE)) x.surface = 'trim'; });
    expect(broadParts(all.w).length).toBe(482);
    expect(broadParts(all.w)).toContain('mega/parade-galleries/range-0-0/canopy (megastructure prim box 121.4 x 2.5 x 0.3 m, face 2.5 x 121 m)');
    expect(all.check.detail).toContain('482 fail: street/stop/T1/comet/l/phone-booth-13 (phone-booth box 1 x 1 x 2.5 m, face 1 x 2.5 m)'); // the first six are named, the rest counted
  });

  test('brief 009: a crossing\'s arcs (237) and eyes (156) are broad faces and need their own keys; its dot rows (0.43 wide) and studs would pass in trim', () => {
    for (const [key, n] of [[GLOW, 0], [INLAY, 237], [ORB, 156]] as const) {
      const { w, check } = sf21(w => { for (const x of owned(w, [key])) { x.surface = 'trim'; delete (x as { kit?: unknown }).kit; } }); // unbound: a part with its kit piece carries the piece's own materials
      expect(broadParts(w).length, key).toBe(n);
      expect(check.ok, key).toBe(n === 0);
    }
  });

  test('a new broad part in trim fails; the same part in a key of its own, or as a bead, passes', () => {
    const slab = (surface: string) => (w: World) => { w.megastructures[0]!.prims.push({ id: 'test/slab', shape: 'box', surface, c: [0, 0], base: 5, size: [7.3, 2.6, 0.25], yaw: 0 }); };
    expect(sf21(slab('trim')).check.detail).toContain('1 fail: test/slab (megastructure prim box 7.3 x 2.6 x 0.25 m, face 2.6 x 7.3 m)');
    expect(sf21(slab('accent_gold')).check.ok).toBe(false); // the gold role is brass too
    expect(sf21(slab(PLATE)).check.ok).toBe(true);
    expect(sf21(w => { w.skins[0]!.pier.frame = 0.9; }).check.detail).toContain('pier frame 0.9 m wide, face 0.9 x ∞ m'); // a frame wider than 0.6 m is not a frame
  });
});

describe('SF-21 what it leaves out, and why', () => {
  test('roof items: RoofKit paints them from roofItems, where trim is the building\'s enamel', () => {
    const roofTrim = base.w.roofs.flatMap(r => r.parts).filter(p => p.surface === 'trim' && broad(faceOf(p.shape, p.size)));
    expect(roofTrim.length).toBeGreaterThan(100); // 92 spheres, 6 drums, 3 dishes, 23 pergolas, the roof tubs...
    expect(sf21().check.ok).toBe(true);
  });

  test('frontage prims: ProcShop paints the door and the bay from swatches, never from their surface', () => {
    const bays = base.w.frontages.filter(f => f.service?.surface === 'trim' && broad(faceOf(f.service.shape, f.service.size)));
    expect(bays.length).toBe(32); // 4.5 x 4.6 m service bays
    expect(sf21().check.ok).toBe(true);
  });

  test('a street item with a kit piece carries the piece\'s own materials; without it the item is held to the measure', () => {
    const bench = base.w.street.find(s => s.kind === 'bench' && s.kit)!;
    expect(bench.surface).toBe('trim');
    expect(sf21().check.ok).toBe(true);
    expect(sf21(w => { delete w.street.find(s => s.id === bench.id)!.kit; }).check.detail).toContain(`${bench.id} (bench box 2 x 0.7 x 0.85 m, face 0.85 x 2 m)`);
  });
});
