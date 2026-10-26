// The rival crew's look (brief 067 W1, Forma; Sky on 0.2.122: "I see guard characters being used that aren't guards"; Pulse's audit, briefs/067/pulse.md §1.3): the rivals were Worker D5 in
// a dark violet uniform, a hard hat and amber piping, darker than the police (median albedo value 0.27 against 0.32). The measurable spec is held here so a later recolour cannot bring
// them back to the police: median albedo V >= 0.45, violet >= 40 % of the clothing, dark <= 40 %, amber <= 1 %, trousers pale, no cap, more than one look. What a test can hold without
// Blender: the specs (figures.json, roles.json), the UV masks the recolours read, the same tiers of detail as the driver and the councillor (no new compression), the proof tool's grade
// round trip; and, when a build or an install holds the figures, the albedo numbers themselves (tools/figures/albedo_stats.py).
import { describe, expect, test } from 'bun:test';
import { existsSync, readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';

const root = resolve(import.meta.dir, '../..');
const fig = join(root, 'tools/figures');
type Lod = { src?: string; from?: number; tris?: number; tex: number; decimateTo?: number };
type Mask = { mask: string; invert?: boolean };
type Op = { op: string; to?: number[]; protect?: Mask[]; only?: Mask[]; hue?: number[]; sat?: number[]; val?: number[] };
type Tint = { from: number; shift: number; sat: number; val: number; minSat?: number; keep?: Mask[] };
type Spec = { id: string; role: string; kind?: string; lods: Lod[]; repair?: Op[]; tint?: Tint[]; source: string; noNormal?: boolean };
const specs = (JSON.parse(readFileSync(join(fig, 'figures.json'), 'utf8')) as { figures: Spec[] }).figures;
const roles = JSON.parse(readFileSync(join(fig, 'roles.json'), 'utf8')) as { playViews: Record<string, unknown>; jobBlocks: Record<string, string[]>; families: Record<string, string[]>; variantOf: Record<string, string> };
const TRIO = ['rival', 'rival_b', 'rival_c'], WOMAN = 'rival_d', CREW = [...TRIO, WOMAN], by = (id: string) => specs.find(s => s.id === id)!;
const hsv = ([r, g, b]: number[]) => { const mx = Math.max(r!, g!, b!), mn = Math.min(r!, g!, b!), d = mx - mn; let h = 0; if (d > 0) h = mx === r ? ((g! - b!) / d + 6) % 6 : mx === g ? (b! - r!) / d + 2 : (r! - g!) / d + 4; return { h: h * 60, s: mx ? d / mx : 0, v: mx }; };
/** the recolours that paint a colour (not the skin fix): by what they paint, in the order of the spec */
const paints = (id: string) => by(id).repair!.filter(o => o.op === 'recolour' && o.to).map(o => ({ op: o, c: hsv(o.to!) }));

describe('the crew: four looks in violet, three colourways on one body and a woman on another, at the driver\'s tiers of detail', () => {
  test('every look is a figure, role rival, from a 50k Hunyuan commuter and the C1 donor rig: D4 for the trio, C6 for the woman; none from Worker D5', () => {
    for (const id of CREW) {
      const s = by(id); expect(s, id).toBeDefined(); expect(s.role).toBe('rival'); expect(s.kind).toBe('transfer'); expect(s.source).not.toContain('workers_D5');
      expect(s.source).toContain(id === WOMAN ? 'commuters_C6__hunyuan_pro.glb' : 'commuters_D4__hunyuan_pro.glb');
    }
    expect(new Set(TRIO.map(id => JSON.stringify(by(id).lods))).size).toBe(1); // the trio: the same body, the same levels
  });
  test('full detail: no new compression. The levels are the driver\'s and the councillor\'s (8000, 3000, 1500 triangles; 2048, 1024, 512 textures), never the old rival\'s 4500', () => {
    const tiers = (s: Spec) => s.lods.map(l => `${l.decimateTo ?? l.tris}@${l.tex}`).join(' ');
    for (const id of CREW) expect(tiers(by(id)), id).toBe('8000@2048 3000@1024 1500@512');
    expect(tiers(by('driver'))).toBe('8000@2048 3000@1024 1500@512'); expect(tiers(by('councillor'))).toBe('8000@2048 3000@1024 1500@512');
  });
  test('the recolour never aims at gold or amber: the jacket is a violet of the art bible\'s weight, the boots pearl, the trousers pale, the hair dark, silver or red; the woman\'s chevrons go lilac', () => {
    for (const id of TRIO) {
      const [j, seam, boots] = paints(id).filter(p => p.c.s > 0.2 || p.op.only).map(p => p.c); // jacket, quilting lines, boots (the skin fix paints a neutral and is not among them)
      for (const c of [j!, seam!]) { expect(c.h, `${id} hue`).toBeGreaterThan(250); expect(c.h).toBeLessThan(290); expect(c.s).toBeGreaterThan(0.25); }
      expect(j!.v, `${id} jacket value (the art bible's violet_set is 0.56)`).toBeGreaterThanOrEqual(0.55); expect(j!.v).toBeLessThanOrEqual(0.9);
      void boots;
      const boot = paints(id).find(p => p.op.only)!.c; expect(boot.s, `${id} boots`).toBeLessThan(0.15); expect(boot.v).toBeGreaterThan(0.55);   // pearl, not black and not gold
      const [trousers, ribbing, hair] = by(id).tint!;
      expect(trousers!.sat, `${id} trousers go pale (saturation almost out, value up)`).toBeLessThan(0.12); expect(trousers!.val).toBeGreaterThan(2.4);
      expect(ribbing!.shift, `${id} ribbing turns navy to violet`).toBeGreaterThan(40); expect(ribbing!.shift).toBeLessThan(70);
      const hueAfter = (32 + hair!.shift + 360) % 360;   // D4's hair sits at hue 28 to 36 (median 32) in the atlas, V 0.41
      expect(hair!.sat < 0.2 || hueAfter < 20 || hair!.val < 0.6, `${id} hair is silver, red or dark: ${JSON.stringify(hair)} -> hue ${hueAfter}`).toBe(true);
    }
    const w = paints(WOMAN).filter(p => p.c.s > 0.2 && !p.op.only).map(p => p.c); expect(w.length).toBe(2);   // the jacket and the lining
    for (const c of w) { expect(c.h).toBeGreaterThan(250); expect(c.h).toBeLessThan(290); }
    expect(w[0]!.v).toBeGreaterThanOrEqual(0.55);
    const [navy, coral] = by(WOMAN).tint!; expect(navy!.shift).toBeGreaterThan(40); expect(navy!.shift).toBeLessThan(70);
    expect((15 + coral!.shift) % 360, 'the coral chevrons turn to a lilac, never toward gold').toBeGreaterThan(260); expect((15 + coral!.shift) % 360).toBeLessThan(300);
    expect(coral!.keep, 'only on the dress: no skin is touched').toEqual([{ mask: 'masks/c6_dress.png', invert: true }]);
  });
  test('the looks differ: four jackets, three heads of hair, three pairs of trousers on the D4 body', () => {
    const jackets = new Set(CREW.map(id => paints(id).find(p => p.c.s > 0.2)!.op.to!.join()));
    expect(jackets.size).toBe(4);
    expect(new Set(TRIO.map(id => JSON.stringify(by(id).tint![2]!))).size).toBe(3); expect(new Set(TRIO.map(id => JSON.stringify(by(id).tint![0]!))).size).toBe(3);
  });
  test('the UV masks the recolours read exist, are 2048 square, and are not empty (zmask.py, made from the mesh per texel)', () => {
    const files = new Set<string>();
    for (const id of CREW) { for (const o of by(id).repair!) for (const k of [...(o.protect ?? []), ...(o.only ?? [])]) files.add(k.mask); for (const t of by(id).tint!) for (const k of t.keep ?? []) files.add(k.mask); }
    expect([...files].sort()).toEqual(['masks/c6_boots.png', 'masks/c6_dress.png', 'masks/c6_face.png', 'masks/d4_boots.png', 'masks/d4_hair.png', 'masks/d4_head.png', 'masks/d4_legs.png']);
    for (const f of files) {
      const p = join(fig, f); expect(existsSync(p), f).toBe(true);
      const b = readFileSync(p); expect(b.subarray(1, 4).toString()).toBe('PNG'); expect(b.readUInt32BE(16), `${f} width`).toBe(2048); expect(b.readUInt32BE(20), `${f} height`).toBe(2048);
      expect(b.length, `${f} is more than a flat colour`).toBeGreaterThan(15000);
    }
  });
});

describe('who draws them: the pool, the recolour relation, and the old rival gone', () => {
  test('roles.json: the pool role/rival holds the four, and nothing else draws a rival look', () => {
    expect(roles.jobBlocks['role/rival']).toEqual(CREW);
    for (const [pool, ids] of [...Object.entries(roles.jobBlocks), ...Object.entries(roles.families)]) if (pool !== 'role/rival') for (const id of CREW) expect(ids, `${pool} must not hold ${id}`).not.toContain(id);
  });
  test('they are recolours of D4 and C6 (the host and Pulse\'s L-3 audit say so), no longer of Worker D5', () => {
    for (const id of TRIO) expect(roles.variantOf[id], id).toBe('d4');
    expect(roles.variantOf[WOMAN]).toBe('c6');
  });
  test('rival-1..5 take the looks rival, rival_b, rival_d, rival_c, rival: the three posted are three different looks, the woman among them', () => {
    const m = roles.playViews['rival-1..5, body/rival-<n>'] as Record<string, string>;
    expect(m).toEqual({ 'rival-1': 'rival', 'rival-2': 'rival_b', 'rival-3': 'rival_d', 'rival-4': 'rival_c', 'rival-5': 'rival' });
    expect(new Set(['rival-1', 'rival-2', 'rival-3'].map(k => m[k])).size).toBe(3);
  });
  test('the old rival\'s model stays in the build only as the yellow worker', () => {
    expect(by('workers_d5').source).toContain('workers_D5__hunyuan_pro.glb'); expect(by('workers_d5').role).toBe('townsperson');
    expect(JSON.stringify(CREW.map(by))).not.toContain('workers_D5');
  });
});

describe('the proof tool\'s grade round trip (tools/figures/proof_post.py: the game\'s exposure, saturation, contrast, ACES and sRGB, and back)', () => {
  test('ungrade(grade(x)) gives x back across the working range', () => {
    const code = `
import numpy as np, importlib.util, json
sp = importlib.util.spec_from_file_location('pp', ${JSON.stringify(join(fig, 'proof_post.py'))}); pp = importlib.util.module_from_spec(sp); sp.loader.exec_module(pp)
x = np.array([[0.02, 0.03, 0.05], [0.1, 0.2, 0.3], [0.3, 0.5, 0.2], [0.8, 0.6, 0.7], [0.15, 0.15, 0.15]])
back = pp.ungrade(pp.grade(x) * 255)
print(json.dumps(np.abs(back - x).max(axis=1).tolist()))`;
    const r = Bun.spawnSync(['python3', '-I', '-c', code], { stdout: 'pipe', stderr: 'pipe' });
    expect(r.exitCode, r.stderr.toString()).toBe(0);
    for (const e of JSON.parse(r.stdout.toString()) as number[]) expect(e).toBeLessThan(0.02);
  });
});

// The albedo numbers need the figures: a build (bun tools/figures/run.ts: out/people-models/People) or the Unity copy Latch installs. Without either the rest is skipped, never green by default.
const base = ['out/people-models/People/Figures', 'unity/Assets/Suborned/People/Figures'].map(p => join(root, p)).find(p => existsSync(join(p, WOMAN, 'Textures')));
describe.skipIf(!base)('the albedo spec of Pulse\'s W1, on the figures a build or an install holds', () => {
  const run = (...ids: string[]) => {
    const r = Bun.spawnSync(['python3', '-I', join(fig, 'albedo_stats.py'), '--json', '--base', base!, ...ids], { stdout: 'pipe', stderr: 'pipe' });
    expect(r.exitCode, r.stderr.toString()).toBe(0); return JSON.parse(r.stdout.toString()) as Record<string, { medianV: number; dark: number; violet: number; amber: number; navy: number }>;
  };
  const s = run(...CREW, 'guard', 'security', 'driver', 'workers_d5');
  test.each(CREW)('%s: median V >= 0.45, violet >= 0.40, dark <= 0.40, amber <= 0.01', id => {
    const r = s[id]!; expect(r.medianV, 'median value').toBeGreaterThanOrEqual(0.45); expect(r.violet, 'violet share').toBeGreaterThanOrEqual(0.40);
    expect(r.dark, 'dark share').toBeLessThanOrEqual(0.40); expect(r.amber, 'amber share').toBeLessThanOrEqual(0.01);
  });
  test('the crew is lighter than the police and no darker than the guard (a dark uniform is the police\'s read), with no navy left', () => {
    for (const id of CREW) { expect(s[id]!.medianV, id).toBeGreaterThan(s.security!.medianV + 0.1); expect(s[id]!.medianV, id).toBeGreaterThanOrEqual(s.guard!.medianV); expect(s[id]!.navy, id).toBeLessThan(0.02); }
  });
});
