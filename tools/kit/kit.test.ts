// The catalogue and the copy: kit.json matches the manifest, hashes verify, wave 1 is adopted, a copy twice copies nothing.
import { expect, test } from 'bun:test';
import { createHash } from 'node:crypto';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import type { Kit } from '../../contracts/world/kit.schema';
import { buildCatalogue, refreshMatFiles } from './catalogue';
import { copyAdopted } from './copy';
import { fit } from './fit';
import { GEN_ROOT, kitRoot } from './store';
import { WAVE1 } from './tags';
import { LOD_BY_ID, LOD_CATEGORIES } from './lod';
import { decodePng } from '../hud-budget/png';
import { needs } from '../needs';

const kit: Kit = JSON.parse(readFileSync(resolve(import.meta.dir, '../../contracts/world/kit.json'), 'utf8'));
const root = kitRoot();
// the copy and most checks read the generated pieces (git-ignored) and the old repo's kit store (outside the repo): a checkout without them skips those
const has = needs([join(GEN_ROOT, 'decokit_manifest.json'), root], 'bun run decokit; the old repo at $SUBORNED_OLD');

test.skipIf(!has)('kit.json is the manifest, normalised, and rebuilding it changes nothing', () => {
  expect(kit.assets.filter(a => a.source !== 'generated').length).toBe(202);
  expect(JSON.stringify(buildCatalogue(root, kit, GEN_ROOT))).toBe(JSON.stringify(kit));
  for (const a of kit.assets) { expect(['footprint-centre-ground', 'origin-ground']).toContain(a.pivot); expect(a.front).toBe('+Z'); expect(a.size.every(n => n >= 0)).toBe(true); expect(a.files.some(f => f.role === 'fbx')).toBe(true); }
});

test('wave 1 is exactly the adopted set', () => {
  const adopted = new Set(kit.assets.filter(a => a.status === 'adopted' && a.source !== 'generated').map(a => a.id));
  for (const id of WAVE1) expect(adopted.has(id), id).toBe(true); // wave 1 stays adopted; later waves (facades, civic square, signatures) are adopted on top by their briefs, generated pieces too (decokit/fit.test.ts)
});

test.skipIf(!has)('copy verifies hashes, is idempotent, and refuses a tampered source', () => {
  const dest = mkdtempSync(join(tmpdir(), 'kit-')), a = copyAdopted(kit, root, dest, GEN_ROOT), b = copyAdopted(kit, root, dest, GEN_ROOT);
  try {
    expect(a.errors).toEqual([]); expect(a.copied.length).toBeGreaterThan(100);
    expect(b.copied.length).toBe(0); expect(b.skipped.length).toBe(a.copied.length);
    for (const f of kit.assets.filter(x => x.status === 'adopted').flatMap(x => x.files).slice(0, 40)) expect(createHash('sha256').update(readFileSync(join(dest, f.path))).digest('hex')).toBe(f.sha256);
    const bad = join(dest, kit.assets.find(x => x.id === 'Bollard')!.files[0]!.path); writeFileSync(bad, 'x');
    expect(copyAdopted(kit, root, dest, GEN_ROOT).copied).toContain(kit.assets.find(x => x.id === 'Bollard')!.files[0]!.path); // a damaged copy is re-copied
  } finally { rmSync(dest, { recursive: true, force: true }); }
});

test.skipIf(!has)('a rejected piece\'s model files are removed from the copy, with their metas; what an adopted piece lists, and the Unity prefabs, stay', () => {
  const dest = mkdtempSync(join(tmpdir(), 'kit-')), a = copyAdopted(kit, root, dest, GEN_ROOT);
  try {
    expect(a.errors).toEqual([]); expect(a.pruned).toEqual([]);
    const rejected = kit.assets.filter(x => x.status === 'rejected'), eye = rejected.find(x => x.id === 'SignalEye')!, adopted = new Set(kit.assets.filter(x => x.status === 'adopted').flatMap(x => x.files.map(f => f.path)));
    expect(rejected.length).toBeGreaterThan(0);
    const planted = [...new Set(rejected.flatMap(r => r.files.map(f => f.path.replace(/\.meta$/, ''))).filter(p => /^Models\//.test(p) && !adopted.has(p)).flatMap(p => [p, p + '.meta']))];
    for (const p of planted) { const abs = join(dest, p); mkdirSync(dirname(abs), { recursive: true }); writeFileSync(abs, 'old'); }
    expect(planted.length).toBeGreaterThan(10); expect(planted).toContain('Models/Signals/DK_SignalEye.fbx'); expect(eye.files.some(f => f.role === 'lod')).toBe(true);
    const shared = eye.files.find(f => f.role === 'material' && adopted.has(f.path))!; expect(shared).toBeDefined(); // DK_Chrome: the retired eye and adopted pieces both list it
    const prefab = join(dest, 'Prefabs/Generated/DK_SignalEye.prefab'); mkdirSync(dirname(prefab), { recursive: true }); writeFileSync(prefab, 'unity');
    const b = copyAdopted(kit, root, dest, GEN_ROOT);
    expect(b.errors).toEqual([]); expect(b.pruned.sort()).toEqual(planted.filter(p => !/_L\d\.fbx\.meta$/.test(p)).sort()); // a LOD's meta goes with the LOD (listed once)
    for (const p of planted) expect(existsSync(join(dest, p))).toBe(false);
    expect(existsSync(join(dest, shared.path))).toBe(true); expect(existsSync(prefab)).toBe(true);
    expect(copyAdopted(kit, root, dest, GEN_ROOT).pruned).toEqual([]); // idempotent
  } finally { rmSync(dest, { recursive: true, force: true }); }
});

test('fit scales inside the range and refuses what does not fit', () => {
  const lamp = kit.assets.find(a => a.id === 'Streetlamp')!;
  expect(fit(lamp, { h: 4.5, depth: 1.5 })![1]).toBeCloseTo(1.145, 2);
  expect(fit(lamp, { h: 14, depth: 1.5 })).toBeNull(); // 14 m: beyond the scale range
  const sky = kit.assets.find(a => a.id === 'Skyport_Tower')!;
  expect(fit(sky, { foot: 48, maxH: 24.68 })).toBeNull();
  expect(fit(kit.assets.find(a => a.id === 'SphereStation')!, { foot: 80, maxH: 80 })).not.toBeNull();
});

test('LOD categories (Vela, brief 007 round 3): every piece names one; its LOD switch points are the category\'s', () => {
  expect(kit.lodCategories).toEqual(LOD_CATEGORIES);
  expect(kit.lodCategories.street).toEqual({ l1: 0.008, l2: 0.003 }); expect(kit.lodCategories.towers).toEqual({ l1: 0.3, l2: 0.1 });
  for (const a of kit.assets) {
    expect(Object.keys(kit.lodCategories), a.id).toContain(a.lodCategory);
    if (a.extra) continue; // brief 077: an extra root's piece keeps its own screens (Vela's per-piece testbed rule, three levels), held in tools/kit/extra.test.ts
    const sw = LOD_BY_ID[a.id] ?? kit.lodCategories[a.lodCategory]; // an id's own switch points (the palms) win over the category's
    (a.lods ?? []).forEach((l, i) => expect(l.screen, `${a.id} LOD${i + 1}`).toBe(i === 0 ? sw.l1 : sw.l2));
  }
  // Sky approved the palm levels (brief 028 "Palm LODs", 2026-10-22): both ids carry L1 1,330 tris at 60 m and L2 514 tris at 140 m of the third-person camera, with LOD files hashed
  for (const [id, s1, s2] of [['CoconutPalm', 0.1271, 0.0545], ['CoconutPalm_Street', 0.1705, 0.0731]] as const) {
    const a = kit.assets.find(x => x.id === id)!;
    expect(a.lods?.map(l => [l.tris, l.screen]), id).toEqual([[1330, s1], [514, s2]]);
    expect(a.files.filter(f => f.role === 'lod').map(f => f.path), id).toEqual(a.lods!.map(l => l.file));
    expect(a.files.filter(f => f.role === 'lod').every(f => /^[0-9a-f]{64}$/.test(f.sha256)), id).toBe(true);
  }
  const cat = (id: string) => kit.assets.find(a => a.id === id)!.lodCategory;
  expect([cat('Bollard'), cat('CafeSet_Tulip'), cat('Cypress'), cat('StreamBay_6'), cat('StreamCorner_4'), cat('Tower_Saucer@harbour-hotel'), cat('Crown_Atom'), cat('SpaceHQ_Portal'), cat('DiscTower@avenue-gate-w'), cat('Horizon_RocketPad')])
    .toEqual(['street', 'street', 'trees', 'facades', 'facades', 'towers', 'towers', 'signatures', 'signatures', 'horizon']);
});

test('Vela\'s street props are the generator\'s binaries; since brief 026 (old detail back) the heavy ones carry LODs, so the 1,500-tri cap and the no-LOD claim are gone', () => {
  for (const id of ['Bollard', 'Hydrant', 'RecyclingBins', 'DomeCamera', 'CCTVPole', 'PhoneBooth']) {
    const a = kit.assets.find(x => x.id === id)!, fbx = a.files.find(f => f.role === 'fbx')!;
    expect(fbx.root, id).toBe('generated'); expect(a.files.some(f => f.role === 'fbx-meta'), id).toBe(true); // the DecoKit meta keeps the GUID the prefab uses
    if (a.tris > 1500) expect(a.lods?.length, `${id} over 1,500 tris needs LODs`).toBeGreaterThan(0);
  }
});

test.skipIf(!has)('brief 007 round 3 wrap-up: the concept-B liner (hand-built far L2) and PlanterArc adopted; every LOD asks only for its piece\'s materials', () => {
  const by = (id: string) => kit.assets.find(a => a.id === id)!;
  expect(by('SpaceHQ_Portal').status).toBe('adopted');
  // brief 075 remap step 2 (the SpaceHQ switch, form "old"): the base SpaceHQ is adopted as the old builders' own piece (OLD_PIECES), never the generator's
  // brief 007 rebuild of the same name: the old repo's fbx (no generated root), no generator LODs
  const hq = by('SpaceHQ'), fbx = hq.files.find(f => f.role === 'fbx')!;
  expect(hq.status).toBe('adopted'); expect(fbx.root).toBeUndefined(); expect(fbx.sha256).toBe('f5399b467978a4aa3cabc5e21e6c3ce2ba233a8972b22f792b98b98d46838f42');
  expect(hq.lods).toBeUndefined(); expect(hq.files.some(f => f.role === 'lod')).toBe(false); expect(hq.size).toEqual([80, 252, 88.8]);
  expect(by('PlanterArc').status).toBe('rejected'); expect(by('PlanterArc_TB').status).toBe('adopted'); expect(by('PlanterArc_TB').pivot).toBe('footprint-centre-ground'); // brief 077: the dome version rejected for its testbed rebuild
  const liner = by('OceanLiner');
  expect(liner.status).toBe('adopted'); expect(liner.lods?.length).toBe(2);
  for (const m of ['DK_Accent_Coral', 'DK_Accent_Teal', 'DK_Accent_Gold', 'DK_Liner_White', 'DK_Liner_Teak', 'DK_Liner_Dome', 'DK_Liner_Pool']) expect(liner.materials, m).toContain(m);
  for (const m of ['DK_Faience_Teal', 'DK_GroundCover']) expect(by('PlanterArc').materials, m).toContain(m);
  // a far version built by hand (tools/decokit kit.asset far=) is a separate mesh: the copy brings only the piece's listed materials, so a LOD naming another would import pink
  // (pieces whose L0 is the generator's own binary; the legacy DecoKit pieces' generator LODs carry DK_Beacon_Rose where their L0 has the DecoKit beacon: open, Vela)
  for (const a of kit.assets.filter(x => x.status === 'adopted' && x.files.some(f => f.role === 'fbx' && f.root === 'generated') && !x.files.some(f => f.role === 'fbx-meta')))
    for (const f of a.files.filter(x => x.role === 'lod' && x.root === 'generated')) {
      const names = new Set([...readFileSync(join(GEN_ROOT, f.path)).toString('latin1').matchAll(/DK_[A-Za-z0-9_]+/g)].map(m => m[0]).filter(n => kit.assets.every(x => !n.startsWith(`DK_${x.id.split('@')[0]}`))));
      for (const n of names) if (/^DK_(Accent|Liner|Enamel|Window|Faience|Limestone|Gold|Crown|Chrome|Leaf|Flower|GroundCover|Beacon|Terrazzo|Bronze|Hull|Steel|Lamp)/.test(n)) expect(a.materials, `${a.id} ${f.path} uses ${n}`).toContain(n);
    }
});

test.skipIf(!has)('brass over gold (Sky, 14 Oct): street level and facades draw brass, the 31 crown and skyline pieces (27 + the four mirrored skyline caps, brief 075 old buildings) keep the old golds as DK_Crown_Gold and DK_Crown_Gold17', () => {
  const adopted = kit.assets.filter(a => a.status === 'adopted');
  const crown = adopted.filter(a => a.materials.some(m => /^DK_Crown_Gold/.test(m)));
  expect(crown.map(a => a.id).sort()).toEqual(['Crown_Atom', 'Crown_Finial', 'Crown_Starburst', 'DecoBay_10', 'DiscTower@crown-vista', 'LinerDock@liner-berth', 'ParabolicArch', 'ParabolicArch@crown-avenue', 'SpaceHQ_Portal', 'Skyport_Tower', 'Skyport_Tower_Portal', 'StreamBay_6', 'StreamBay_9', 'StreamBay_9W', 'StreamCorner_9', 'StreamCorner_9_M',
    'StreamEnd_6', 'StreamEnd_9', 'StreamEnd_9W', 'StreamEnd_6_M', 'StreamEnd_9_M', 'StreamEnd_9W_M', 'Tower_Atom@crescent-tower', 'Tower_Lantern@lantern-beacon', 'Tower_Needle', 'Tower_Needle@avenue-needle', 'Tower_Saucer@harbour-hotel', 'Tower_Starburst', 'Tower_Starburst@avenue-starburst', 'Tower_Terrace', 'Tower_Terrace@crescent-terrace'].sort());
  for (const a of crown) expect(a.materials.filter(m => /^DK_(Gold|Gold17|Accent_Gold)$/.test(m)), `${a.id} draws a brass beside its crown gold`).toEqual([]); // the swap is whole: one gold per piece
  const palette = new Map((JSON.parse(readFileSync(resolve(import.meta.dir, '../../contracts/world/civic-look.json'), 'utf8')) as { palette: { materials: { material: string; unlit: string }[] } }).palette.materials.map(m => [m.material, m.unlit]));
  // the dE gate reads the palette rows; the .mat the generator wrote is what Unity draws: they must say the same colour
  const hexOf = (name: string) => { const c = /_BaseColor: \{r: ([\d.e-]+), g: ([\d.e-]+), b: ([\d.e-]+)/.exec(readFileSync(join(GEN_ROOT, `Materials/${name}.mat`), 'utf8'))!; return '#' + [c[1]!, c[2]!, c[3]!].map(x => Math.round(+x * 255).toString(16).padStart(2, '0')).join('').toUpperCase(); };
  // brief 072-G (Sky, 27 Oct: "more reds / blues allowed"): street brass is brass-gold #C29A55, DK_Gold DecoKit's #D6AE62, light brass #D6B676; the crowns' golds unchanged
  const want = { DK_Gold: '#D6AE62', DK_Accent_Gold: '#C29A55', DK_Brass_Civic: '#C29A55', DK_Gold17: '#D6B676', DK_Crown_Gold: '#E3D681', DK_Crown_Gold17: '#C9C08E' };
  for (const [m, hex] of Object.entries(want)) { expect(hexOf(m), `${m}.mat`).toBe(hex); expect(palette.get(m), `${m} palette row`).toBe(hex); }
  for (const m of Object.keys(want)) expect(new Set(adopted.flatMap(a => a.files.filter(f => f.path === `Materials/${m}.mat`).map(f => f.sha256))).size, `${m}: one file in the catalogue`).toBe(1);
});

test('brief 013 (Vantage, Motive §2.5): only a piece tagged `stash` can hold a body, and today that is the Dumpster, still a candidate until Vela adopts it', () => {
  const stash = kit.assets.filter(a => a.tags.stash);
  // brief 097 (Vela): the zone's dumpster is drawn by Dumpster_Civic, the same stash (holds 1) at the same site that the procedural box stood for; the rule is unchanged (Keel, Motive to confirm)
  expect(stash.map(a => `${a.id} ${a.tags.stash!.holds} ${a.status}`).sort()).toEqual(['Dumpster 1 candidate', 'Dumpster_Civic 1 adopted']);
  expect(stash[0]!.tags.use).toContain('dumpster'); // the street item kind the world places it as
  // size does not decide it: pieces as big as a Dumpster that must not hold a body (a booth, a lamp, a crate stack, a coffee cart)
  for (const id of ['PhoneBooth', 'Streetlamp', 'CrateStack', 'UtilityCabinet', 'RecyclingBins']) expect(kit.assets.find(a => a.id === id)!.tags.stash, id).toBeUndefined();
});

// Batch 0.2.118: signal_atlas_v1 and signal_ring_v1 carry their picture in the alpha channel (the glyph / the ring segment IS the alpha). Imported with Alpha Source None, Unity drops it and every
// lens face draws as a solid square and every clock ring as a full one. The meta the kit copies must say `alphaUsage: 1` (Alpha Source = input texture alpha), at the generator, in the catalogue and in the copy.
test.skipIf(!has)('signal textures carry real alpha and their metas import it (Alpha Source = input texture alpha)', () => {
  const dest = mkdtempSync(join(tmpdir(), 'kit-')), a = copyAdopted(kit, root, dest, GEN_ROOT);
  try {
    expect(a.errors).toEqual([]);
    const pngs = [...new Set(kit.assets.filter(x => x.status === 'adopted').flatMap(x => x.files.map(f => f.path)).filter(p => /^Textures\/signal_[^/]*\.png$/.test(p)))].sort();
    expect(pngs).toEqual(['Textures/signal_atlas_v1.png', 'Textures/signal_ring_v2.png']); // brief 079: the lit-in-the-paving ring
    const trees = [['copy', dest], ...(existsSync(resolve(import.meta.dir, '../../unity/Assets/Suborned/Kit/Textures')) ? [['unity Kit', resolve(import.meta.dir, '../../unity/Assets/Suborned/Kit')]] : [])] as [string, string][];
    for (const [where, base] of trees) for (const p of pngs) {
      const im = decodePng(readFileSync(join(base, p)));
      let clear = 0, solid = 0; for (let i = 3; i < im.data.length; i += 4) { if (im.data[i] === 0) clear++; else if (im.data[i]! > 200) solid++; }
      expect(clear, `${where} ${p}: no transparent texel`).toBeGreaterThan(0); expect(solid, `${where} ${p}: no opaque texel`).toBeGreaterThan(0); // alpha is the picture: a flat-alpha PNG draws a square
      const meta = readFileSync(join(base, p + '.meta'), 'utf8');
      expect(/^ {2}alphaUsage: 1$/m.test(meta), `${where} ${p}.meta: Alpha Source must be "input texture alpha" (alphaUsage: 1)`).toBe(true);
      expect(/^ {2}alphaIsTransparency: 1$/m.test(meta), `${where} ${p}.meta: alphaIsTransparency`).toBe(true);
    }
    for (const p of pngs) expect(readFileSync(join(GEN_ROOT, p + '.meta'), 'utf8'), `generator ${p}.meta`).toContain('alphaUsage: 1'); // the exporter's own copy, not only what was copied
  } finally { rmSync(dest, { recursive: true, force: true }); }
});

test.skipIf(!has)('brief 072-F: the surface-detail array and the three detail pairs ride as kit-wide textures, each with its meta, and the copy keeps them', () => {
  const t = kit.textures ?? [], paths = new Set(t.map(f => f.path));
  for (const n of ['surface_detail_v1', 'surface_enamel_detail_v1', 'surface_enamel_detail_v1_normal', 'surface_limestone_detail_v1', 'surface_limestone_detail_v1_normal', 'surface_metal_detail_v1', 'surface_metal_detail_v1_normal'])
    for (const p of [`Textures/${n}.png`, `Textures/${n}.png.meta`]) expect(paths.has(p), p).toBe(true);
  for (const f of t) { expect(f.root).toBe('generated'); expect(f.sha256).toMatch(/^[0-9a-f]{64}$/); }
  const meta = join(GEN_ROOT, 'Textures/surface_detail_v1.png.meta');
  if (existsSync(meta)) expect(readFileSync(meta, 'utf8')).toMatch(/textureShape: 4\n/); // Texture2DArray (TextureImporterShape: 2D=1, Cube=2, 2DArray=4)
  const only: Kit = { ...kit, carPaints: undefined, colourways: undefined, landmarks: undefined, posters: undefined, assets: [] }, dest = mkdtempSync(join(tmpdir(), 'kit-tex-'));
  try { const r = copyAdopted(only, root, dest, GEN_ROOT); expect(r.errors).toEqual([]); expect(r.copied.length).toBe(t.length); expect(r.pruned).toEqual([]); }
  finally { rmSync(dest, { recursive: true, force: true }); }
});

test('brief 072-F: a kept generated entry takes its material files fresh, so no path carries two hashes in the catalogue', () => {
  const f = (role: Kit['assets'][number]['files'][number]['role'], path: string, sha256: string) => ({ root: 'generated' as const, role, path, sha256, bytes: 1 });
  const prev = [f('fbx', 'Models/Plaza/DK_X.fbx', 'a'), f('material', 'Materials/M.mat', 'old'), f('material-meta', 'Materials/M.mat.meta', 'm'), f('material', 'Materials/Gone.mat', 'g'), f('lod', 'Models/Plaza/DK_X_L1.fbx', 'l')];
  const fresh = [f('material', 'Materials/M.mat', 'new'), f('material-meta', 'Materials/M.mat.meta', 'm'), f('texture', 'Textures/t.png', 't')];
  expect(refreshMatFiles(prev, fresh).map(x => `${x.path}:${x.sha256}`)).toEqual(['Models/Plaza/DK_X.fbx:a', 'Materials/M.mat:new', 'Materials/M.mat.meta:m', 'Textures/t.png:t', 'Materials/Gone.mat:g', 'Models/Plaza/DK_X_L1.fbx:l']);
  const seen = new Map<string, string>(), twice: string[] = [];
  for (const a of kit.assets.filter(x => x.status === 'adopted')) for (const x of a.files) { const o = seen.get(x.path); if (o && o !== x.sha256) twice.push(`${a.id} ${x.path}`); seen.set(x.path, x.sha256); }
  for (const x of kit.textures ?? []) { const o = seen.get(x.path); if (o && o !== x.sha256) twice.push(`textures ${x.path}`); }
  expect(twice).toEqual([]);
});

test('brief 072 F2: every roof crown switches by the crown distance class, L1 beyond the 36 m play-zoom guard, nothing culled or at L2 inside the 350 m clip', () => {
  const crowns = kit.assets.filter(a => /^Crown_/.test(a.id));
  expect(crowns.map(a => a.id).sort()).toEqual(['Crown_Antenna', 'Crown_Atom', 'Crown_Dish', 'Crown_Finial', 'Crown_Needle', 'Crown_Saucer', 'Crown_Starburst']);
  for (const a of crowns) expect(a.lodClass, a.id).toBe('crown');
  const [l1, l2] = kit.lodClasses!.crown.distances;
  expect(l1).toBeGreaterThan(36); expect(l2).toBeGreaterThan(350);
});

test.skipIf(!has)('brief 075 old palette: the old DecoKitColourways scheme (five ensembles + the Soft high street) over wall, fascia, sills and trim, each a recolour of its base matching the old Paint .mat, copied with their metas', () => {
  const cw = kit.colourways!, fac = JSON.parse(readFileSync(resolve(import.meta.dir, '../../unity/Assets/Suborned/Editor/World/facade.json'), 'utf8')) as { families: Record<string, string[]> };
  expect(cw.families).toEqual(['boulevard', 'circus', 'harbour', 'gardens', 'southside', 'soft-apricot', 'soft-celadon', 'soft-rose', 'soft-ochre']);
  expect(Object.keys(fac.families).filter(f => f !== 'civic').sort()).toEqual([...cw.families].sort()); // one colourway per facade.json family; civic keeps the base
  expect(cw.keep).toEqual(['base', 'civic']);
  expect(cw.slots).toEqual({ DK_Limestone_Tex: 'wall', DK_Render_White: 'wall', DK_Enamel_Teal: 'fascia', DK_Enamel_Seafoam: 'sills', DK_Enamel_Ivory: 'trim' });
  // the old values, exactly (DecoKitColourways.cs Ensembles, Soft, Lighter)
  expect(cw.colours.boulevard).toEqual({ fascia: '#C8553D' });
  expect(cw.colours.circus).toEqual({ wall: '#EBDCB5', fascia: '#1F6C67', sills: '#C8553D' });
  expect(cw.colours.harbour).toEqual({ wall: '#E2E8E0', fascia: '#1F7F7A', sills: '#1F7F7A' });
  expect(cw.colours.gardens).toEqual({ wall: '#E8D6C4', fascia: '#B9653F', sills: '#8FA88A' });
  expect(cw.colours.southside).toEqual({ wall: '#EEE3C9', fascia: '#D9962B', sills: '#2F7E86' });
  expect(cw.colours['soft-apricot']).toEqual({ wall: '#E8B793', fascia: '#C8553D', trim: '#ECD1B8' });
  expect(cw.colours['soft-ochre']).toEqual({ wall: '#E2C07A', fascia: '#C8553D', trim: '#E9D5B0' });
  for (const c of Object.values(cw.check ?? {})) if (c.oldBaseColor) expect(c.oldDelta!).toBeLessThan(1e-5); // the old Paint .mat's _BaseColor
  for (const [n, c] of Object.entries(cw.check ?? {})) if (!c.oldBaseColor) expect(c.tintedOver, n).toBeUndefined(); // no old Paint .mat (DK_Render_White__circus): an untextured base, so the hex itself
  expect(Object.values(cw.check ?? {}).filter(c => c.oldBaseColor).length).toBeGreaterThanOrEqual(32);
  const paths = new Set(cw.files.map(f => f.path));
  const mat = (n: string) => { const g = join(GEN_ROOT, `Materials/${n}.mat`); return existsSync(g) ? readFileSync(g, 'utf8') : undefined; };
  const legacy = (n: string) => { const p = join(root, `Materials/${n}.mat`); return existsSync(p) ? readFileSync(p, 'utf8') : undefined; };
  const strip = (t: string) => t.replace(/  m_Name: \S+\n/, '').replace(/    - _(Base)?Color: \{[^}]*\}\n/g, '');
  const slotOf = (f: string, base: string) => (cw.colours[f] as Record<string, string>)[cw.slots[base]!];
  for (const [base, by] of Object.entries(cw.materials)) {
    expect(Object.keys(by)).toEqual(cw.families.filter(f => slotOf(f, base) !== undefined)); // each family over the slots it paints
    for (const [f, n] of Object.entries(by)) {
      expect(n).toBe(`${base}__${f}`);
      for (const p of [`Materials/${n}.mat`, `Materials/${n}.mat.meta`]) expect(paths.has(p), p).toBe(true);
      const b = mat(base) ?? legacy(base), v = mat(n);
      if (b && v) expect(strip(v), n).toBe(strip(b)); // only the name and the base colour differ: no new variant, no new SetPass
    }
  }
  expect(cw.files.some(f => /__(cream|peach|mint|butter|lilac|apricot|oldochre)\.mat$/.test(f.path))).toBe(false);
  // the walls sit on the old sheet again (the neutral sheet is gone)
  const lime = kit.assets.find(a => a.status === 'adopted' && a.materials.includes('DK_Limestone_Tex'))!;
  expect(lime.files.some(f => f.path === 'Textures/facade_panel_v1.png')).toBe(true);
  expect(kit.assets.some(a => a.files.some(f => f.path === 'Textures/wall_panel_neutral_v1.png'))).toBe(false);
  const only: Kit = { ...kit, carPaints: undefined, textures: [], landmarks: undefined, posters: undefined, assets: [] }, dest = mkdtempSync(join(tmpdir(), 'kit-cw-'));
  try { const r = copyAdopted(only, root, dest, GEN_ROOT); expect(r.errors).toEqual([]); expect(r.copied.length).toBe(cw.files.length); }
  finally { rmSync(dest, { recursive: true, force: true }); }
});

test.skipIf(!has)('brief 075-C: landmark livery (old DecoKitColourways `livery`): vermilion lattices, the brick sphere, SpaceHQ gold; each a recolour of its base, mapped per piece, copied with their metas', () => {
  const lv = kit.landmarks!;
  expect(lv.schema).toBe('suborned.livery/1');
  expect(Object.fromEntries(Object.entries(lv.keys).map(([k, v]) => [k, [v.base, v.material, v.hex]]))).toEqual({
    vermilion: ['DK_Steel_Ivory', 'DK_Steel_Ivory__vermilion', '#B8432E'], brick: ['DK_Hull_Ivory', 'DK_Hull_Ivory__brick', '#9C2E2B'], gold: ['DK_Crown_Gold', 'DK_Crown_Gold__gold', '#F2C14E'] });
  expect(lv.pieces.SphereStation).toEqual({ DK_Steel_Ivory: 'vermilion', DK_Hull_Ivory: 'brick' });
  expect(lv.pieces.Skyport_Tower_Portal).toEqual({ DK_Steel_Ivory: 'vermilion' });
  expect(lv.pieces.SpaceHQ_Portal).toEqual({ DK_Crown_Gold: 'gold' });
  const byId = new Map(kit.assets.map(a => [a.id, a]));
  for (const [id, slots] of Object.entries(lv.pieces)) for (const [base, key] of Object.entries(slots)) {
    const a = byId.get(id)!; expect(a.status, id).not.toBe('rejected'); expect(a.materials, id).toContain(base); expect(lv.keys[key].base, `${id} ${base}`).toBe(base);
  }
  const paths = new Set(lv.files.map(f => f.path));
  const mat = (n: string) => { const g = join(GEN_ROOT, `Materials/${n}.mat`); return existsSync(g) ? readFileSync(g, 'utf8') : undefined; };
  const legacy = (n: string) => { const p = join(root, `Materials/${n}.mat`); return existsSync(p) ? readFileSync(p, 'utf8') : undefined; };
  const strip = (t: string) => t.replace(/  m_Name: \S+\n/, '').replace(/    - _(Base)?Color: \{[^}]*\}\n/g, '').replace(/    - _(Metallic|Smoothness): [\d.e-]+\n/g, ''); // brief 075 old palette: the gold takes the old finish too (floats only)
  expect(lv.keys.gold!.finish).toEqual({ metallic: 1, smoothness: 0.45 });
  for (const v of Object.values(lv.keys)) {
    for (const p of [`Materials/${v.material}.mat`, `Materials/${v.material}.mat.meta`]) expect(paths.has(p), p).toBe(true);
    const b = mat(v.base) ?? legacy(v.base), m = mat(v.material);
    if (b && m) expect(strip(m), v.material).toBe(strip(b)); // only the name and the base colour differ: no new variant, no new SetPass
  }
  const only: Kit = { ...kit, carPaints: undefined, textures: [], colourways: undefined, posters: undefined, assets: [] }, dest = mkdtempSync(join(tmpdir(), 'kit-lv-'));
  try { const r = copyAdopted(only, root, dest, GEN_ROOT); expect(r.errors).toEqual([]); expect(r.copied.length).toBe(lv.files.length); }
  finally { rmSync(dest, { recursive: true, force: true }); }
});

test.skipIf(!has)('brief 075-F1: billboard and poster variants: 16 ads on two sheets in one layout, every billboard bakes the canonical cell, each variant only re-points the texture and its offset', () => {
  const ps = kit.posters!;
  expect(ps.schema).toBe('suborned.posters/1');
  expect(ps.adOrder.length).toBe(16);
  expect(ps.adOrder.filter(k => ps.ads[k]!.sheet === 'A')).toEqual(['mars', 'fizz', 'aircar', 'skyrail', 'robot', 'planetarium', 'cruise', 'cinema']); // the old sheet's 8, unchanged
  expect(new Set(ps.adOrder.map(k => ps.ads[k]!.words)).size).toBe(16);
  const byId = new Map(kit.assets.map(a => [a.id, a]));
  for (const [id, base] of [...Object.entries(ps.adPieces), ...Object.entries(ps.sheetPieces)]) { const a = byId.get(id)!; expect(a.status, id).toBe('adopted'); expect(a.materials, id).toContain(base); }
  for (const id of Object.keys(ps.adPieces)) expect(ps.adOrder, id).toContain(ps.defaults[id]);
  for (const k of ps.adOrder) { expect(ps.ads[ps.backFace[k]!]!.sheet).toBe(ps.ads[k]!.sheet); expect(ps.backFace[ps.backFace[k]!]).toBe(k); }
  const paths = new Set(ps.files.map(f => f.path));
  expect(paths.has(ps.sheets.B.texture) && paths.has(`${ps.sheets.B.texture}.meta`)).toBe(true);
  const gen = (p: string) => { const g = join(GEN_ROOT, p); return existsSync(g) ? readFileSync(g, 'utf8') : undefined; };
  const legacy = (n: string) => { const p = join(root, `Materials/${n}.mat`); return existsSync(p) ? readFileSync(p, 'utf8') : undefined; };
  const strip = (t: string) => t.replace(/  m_Name: \S+\n/, '').replace(/guid: [0-9a-f]{32}, type: 3/g, 'guid: T, type: 3').replace(/m_Offset: \{[^}]*\}/g, 'm_Offset: O');
  for (const k of ps.adOrder) for (const [base, n] of Object.entries(ps.ads[k]!.materials)) {
    for (const p of [`Materials/${n}.mat`, `Materials/${n}.mat.meta`]) expect(paths.has(p), p).toBe(true);
    const b = gen(`Materials/${base}.mat`) ?? legacy(base), m = gen(`Materials/${n}.mat`);
    if (!b || !m) continue;
    expect(strip(m), n).toBe(strip(b)); // same shader, keywords, colour: no new variant, no new SetPass
    expect(m, n).toContain(`guid: ${ps.sheets[ps.ads[k]!.sheet].guid}, type: 3`);
    const [u, v] = ps.ads[k]!.offset; expect(m, n).toContain(`m_Offset: {x: ${+u.toPrecision(8)}, y: ${+v.toPrecision(8)}}`);
  }
  const only: Kit = { ...kit, carPaints: undefined, textures: [], colourways: undefined, landmarks: undefined, assets: [] }, dest = mkdtempSync(join(tmpdir(), 'kit-ps-'));
  try { const r = copyAdopted(only, root, dest, GEN_ROOT); expect(r.errors).toEqual([]); expect(r.copied.length).toBe(ps.files.length); }
  finally { rmSync(dest, { recursive: true, force: true }); }
});

test.skipIf(!has)('brief 075 roll-out: the old sky track kit and the presentation vehicles adopted at their own size; the gate towers carry no crane; 1950s car paints are base-colour recolours, copied with their metas', () => {
  const byId = new Map(kit.assets.map(a => [a.id, a]));
  for (const id of ['SkyRail_Beam_4m', 'SkyRail_Pylon_HeadTwin', 'SkyRail_Pylon_Head', 'SkyRail_Pylon_Foot', 'SkyRail_Pylon_Shaft', 'SkyRail_Pylon_Tall', 'SkyRail_Car_Head', 'SkyRail_Car_Mid', 'AirCar_Coral', 'AirCar_Teal', 'AirCar_Saucer', 'HoverScooter']) {
    const a = byId.get(id)!; expect(a.status, id).toBe('adopted'); expect(a.tags.scale, id).toEqual([1, 1]); expect(a.tags.use.some(u => u === 'vehicle' || u === 'sky' || u === 'landmark'), id).toBe(false);
  }
  const beam = byId.get('SkyRail_Beam_4m')!; expect(beam.size[1]).toBeCloseTo(1.6, 2); expect(beam.size[2]).toBeCloseTo(4, 2); expect(beam.materials).toEqual(expect.arrayContaining(['DK_Hull_Ivory', 'DK_Enamel_Teal']));
  expect(byId.get('SkyRail_Pylon_HeadTwin')!.size[0]).toBeCloseTo(8, 1); // pads at the plan's beam centres +-3 (beamCentres 6)
  for (const id of ['DiscTower', 'DiscTower@avenue-gate-e', 'DiscTower@avenue-gate-w']) { const a = byId.get(id)!; expect(a.materials, id).not.toContain('DK_Steel_Ivory'); expect(a.materials, id).not.toContain('DK_Concrete'); }
  const cp = kit.carPaints!; expect(Object.keys(cp.keys).length).toBe(8); // brief 075 old palette: the old reel's eight enamels over the old ivory body (DecoKitCitizens.Enamels)
  expect(Object.values(cp.keys).map(k => k.roofHex)).toEqual(['#0A6D82', '#E25E47', '#E7B84C', '#78C4A2', '#B53A31', '#5F9FC6', '#27406A', '#2F7D5C']); for (const k of Object.values(cp.keys)) expect(k.bodyHex).toBe('#F9F0D7');
  for (const [k, v] of Object.entries(cp.keys)) { expect(cp.colours[v.body], k).toBe(v.bodyHex); expect(cp.colours[v.roof], k).toBe(v.roofHex); }
  for (const s of Object.values(cp.slots)) for (const v of Object.values(cp.keys)) expect([v.bodyHex, v.roofHex]).not.toEqual(s.body === 'DK_CarEnamel_Teal' ? ['#1F6C67', '#EFE8D6'] : ['#FC9C84', '#72B5A7']);
  const gen = (p: string) => { const g = join(GEN_ROOT, p); return existsSync(g) ? readFileSync(g, 'utf8') : undefined; };
  const strip = (t: string) => t.replace(/  m_Name: \S+\n/, '').replace(/    - _BaseColor: \{[^}]*\}/, '').replace(/    - _Color: \{[^}]*\}/, '');
  const base = gen(`Materials/${cp.base}.mat`);
  for (const n of Object.keys(cp.colours)) { const m = gen(`Materials/${n}.mat`); if (base && m) expect(strip(m), n).toBe(strip(base)); }
  const only: Kit = { ...kit, textures: [], colourways: undefined, landmarks: undefined, posters: undefined, assets: [] }, dest = mkdtempSync(join(tmpdir(), 'kit-cp-'));
  try { const r = copyAdopted(only, root, dest, GEN_ROOT); expect(r.errors).toEqual([]); expect(r.copied.length).toBe(cp.files.length); expect(cp.files.length).toBe(Object.keys(cp.colours).length * 2); }
  finally { rmSync(dest, { recursive: true, force: true }); }
});

test('brief 075 remap: the old roof, garden, plaza, seaside and alley pieces adopted at the old size through the generator, with their own tags (never a generic or older slot), and LODs above the floor', () => {
  const old = new Map((JSON.parse(readFileSync(join(root, 'decokit_manifest.json'), 'utf8')).assets as { name: string; size: number[] }[]).map(a => [a.name, a.size]));
  const generic = new Set(['roof', 'plaza', 'street', 'alley', 'structure', 'tram-shelter', 'lamp', 'bench']);
  const ids = ['Roof_Bandstand', 'Roof_Radome', 'Roof_Dish', 'Roof_Drum', 'Roof_Fin', 'PlazaFountain', 'DecoPavilion', 'CafeSet', 'GrandStair', 'PlanterBed', 'PlanterHex', 'FlutedColumn', 'BusShelter', 'TramCanopy',
    'BistroSet', 'CafeTable_Tulip', 'CafeChair_Tulip', 'CoinTelescope', 'LifebuoyStand', 'AlleyBridge', 'BikeLean', 'CableSpan_6', 'CrateStack', 'MilkCrates', 'Pallet', 'PipeBundle_6', 'SteamVent', 'TrashCans', 'BackDoorLamp'];
  for (const id of ids) {
    const a = kit.assets.find(x => x.id === id)!; expect(a.status, id).toBe('adopted'); expect(a.tags.scale, id).toEqual([1, 1]);
    expect(a.tags.use.length, id).toBe(1); expect(a.tags.use.some(u => generic.has(u) || u.startsWith('planter-bed')), id).toBe(false);
    expect(a.files.find(f => f.role === 'fbx')!.root, id).toBe('generated');
    old.get(id)!.forEach((v, i) => expect(Math.abs(a.size[i]! - v), `${id} size[${i}] ${a.size[i]} vs old ${v}`).toBeLessThan(0.01)); // true size (the DecoPavilion's NaN bevel once read it 2.3 m deep)
    if (a.tris > 1500) expect(a.lods?.length, id).toBeGreaterThan(0);
  }
  expect(kit.assets.find(x => x.id === 'DecoPavilion')!.lods!.map(l => l.tris).every(t => t < 12000)).toBe(true); // the collapse runs (it stalled at 34k on the NaN vertices)
  expect(kit.assets.find(x => x.id === 'Dumpster')!.status).toBe('candidate'); // the stash piece stays Keel's and Motive's call
});

test.skipIf(!has)('what Unity rewrites stays until the catalogue moves; a real change, and a damaged model, still copy (brief 078)', () => {
  const a = kit.assets.find(x => x.id === 'Bollard')!, only = { ...kit, assets: [a], textures: [], colourways: undefined, landmarks: undefined, posters: undefined, carPaints: undefined } as typeof kit;
  const dest = mkdtempSync(join(tmpdir(), 'kit-')), settled = join(dest + '-settled', 'copied.json'), meta = a.files.find(f => f.path.endsWith('.fbx.meta'))!, model = a.files.find(f => f.path.endsWith('.fbx'))!;
  try {
    const first = copyAdopted(only, root, dest, GEN_ROOT, undefined, settled); expect(first.errors).toEqual([]); expect(first.copied).toContain(meta.path); // nothing on disk: it copies
    writeFileSync(join(dest, meta.path), 'guid: rerolled by Unity\n');
    expect(copyAdopted(only, root, dest, GEN_ROOT, undefined, settled).copied).toEqual([]); // Unity's rewrite stands
    expect(readFileSync(join(dest, meta.path), 'utf8')).toBe('guid: rerolled by Unity\n');
    writeFileSync(join(dest, model.path), 'damaged'); expect(copyAdopted(only, root, dest, GEN_ROOT, undefined, settled).copied).toEqual([model.path]); // a model is still byte-checked
    const was = JSON.parse(readFileSync(settled, 'utf8')); was[meta.path] = 'an older catalogue hash'; writeFileSync(settled, JSON.stringify(was));
    expect(copyAdopted(only, root, dest, GEN_ROOT, undefined, settled).copied).toEqual([meta.path]); // failing example: the source's import settings moved, so it copies
    expect(copyAdopted(only, root, dest, GEN_ROOT).copied).toEqual([]); // no settled file given: byte-checked as ever, and it is back to the source's bytes
  } finally { rmSync(dest, { recursive: true, force: true }); rmSync(dest + '-settled', { recursive: true, force: true }); }
});
