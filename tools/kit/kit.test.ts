// The catalogue and the copy: kit.json matches the manifest, hashes verify, wave 1 is adopted, a copy twice copies nothing.
import { expect, test } from 'bun:test';
import { createHash } from 'node:crypto';
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import type { Kit } from '../../contracts/world/kit.schema';
import { buildCatalogue } from './catalogue';
import { copyAdopted } from './copy';
import { fit } from './fit';
import { GEN_ROOT, kitRoot } from './store';
import { WAVE1 } from './tags';
import { LOD_CATEGORIES } from './lod';

const kit: Kit = JSON.parse(readFileSync(resolve(import.meta.dir, '../../contracts/world/kit.json'), 'utf8'));
const root = kitRoot();

test('kit.json is the manifest, normalised, and rebuilding it changes nothing', () => {
  expect(kit.assets.filter(a => a.source !== 'generated').length).toBe(202);
  expect(JSON.stringify(buildCatalogue(root, kit, GEN_ROOT))).toBe(JSON.stringify(kit));
  for (const a of kit.assets) { expect(['footprint-centre-ground', 'origin-ground']).toContain(a.pivot); expect(a.front).toBe('+Z'); expect(a.size.every(n => n >= 0)).toBe(true); expect(a.files.some(f => f.role === 'fbx')).toBe(true); }
});

test('wave 1 is exactly the adopted set', () => {
  const adopted = new Set(kit.assets.filter(a => a.status === 'adopted' && a.source !== 'generated').map(a => a.id));
  for (const id of WAVE1) expect(adopted.has(id), id).toBe(true); // wave 1 stays adopted; later waves (facades, civic square, signatures) are adopted on top by their briefs, generated pieces too (decokit/fit.test.ts)
});

test('copy verifies hashes, is idempotent, and refuses a tampered source', () => {
  const dest = mkdtempSync(join(tmpdir(), 'kit-')), a = copyAdopted(kit, root, dest, GEN_ROOT), b = copyAdopted(kit, root, dest, GEN_ROOT);
  try {
    expect(a.errors).toEqual([]); expect(a.copied.length).toBeGreaterThan(100);
    expect(b.copied.length).toBe(0); expect(b.skipped.length).toBe(a.copied.length);
    for (const f of kit.assets.filter(x => x.status === 'adopted').flatMap(x => x.files).slice(0, 40)) expect(createHash('sha256').update(readFileSync(join(dest, f.path))).digest('hex')).toBe(f.sha256);
    const bad = join(dest, kit.assets.find(x => x.id === 'Bollard')!.files[0]!.path); writeFileSync(bad, 'x');
    expect(copyAdopted(kit, root, dest, GEN_ROOT).copied).toContain(kit.assets.find(x => x.id === 'Bollard')!.files[0]!.path); // a damaged copy is re-copied
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
    (a.lods ?? []).forEach((l, i) => expect(l.screen, `${a.id} LOD${i + 1}`).toBe(i === 0 ? kit.lodCategories[a.lodCategory].l1 : kit.lodCategories[a.lodCategory].l2));
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

test('brief 007 round 3 wrap-up: the concept-B liner (hand-built far L2) and PlanterArc adopted, base SpaceHQ rejected; every LOD asks only for its piece\'s materials', () => {
  const by = (id: string) => kit.assets.find(a => a.id === id)!;
  expect(by('SpaceHQ').status).toBe('rejected'); expect(by('SpaceHQ_Portal').status).toBe('adopted');
  expect(by('PlanterArc').status).toBe('adopted'); expect(by('PlanterArc').pivot).toBe('footprint-centre-ground');
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

test('brass over gold (Sky, 14 Oct): street level and facades draw brass, the 25 crown and skyline pieces keep the old golds as DK_Crown_Gold and DK_Crown_Gold17', () => {
  const adopted = kit.assets.filter(a => a.status === 'adopted');
  const crown = adopted.filter(a => a.materials.some(m => /^DK_Crown_Gold/.test(m)));
  expect(crown.map(a => a.id).sort()).toEqual(['Crown_Atom', 'Crown_Finial', 'Crown_Starburst', 'DecoBay_10', 'LinerDock@liner-berth', 'ParabolicArch', 'SpaceHQ_Portal', 'Skyport_Tower', 'Skyport_Tower_Portal', 'StreamBay_6', 'StreamBay_9', 'StreamBay_9W', 'StreamCorner_9',
    'StreamEnd_6', 'StreamEnd_9', 'StreamEnd_9W', 'Tower_Atom@crescent-tower', 'Tower_Lantern@lantern-beacon', 'Tower_Needle', 'Tower_Needle@avenue-needle', 'Tower_Saucer@harbour-hotel', 'Tower_Starburst', 'Tower_Starburst@avenue-starburst', 'Tower_Terrace', 'Tower_Terrace@crescent-terrace'].sort());
  for (const a of crown) expect(a.materials.filter(m => /^DK_(Gold|Gold17|Accent_Gold)$/.test(m)), `${a.id} draws a brass beside its crown gold`).toEqual([]); // the swap is whole: one gold per piece
  const palette = new Map((JSON.parse(readFileSync(resolve(import.meta.dir, '../../contracts/world/civic-look.json'), 'utf8')) as { palette: { materials: { material: string; unlit: string }[] } }).palette.materials.map(m => [m.material, m.unlit]));
  // the dE gate reads the palette rows; the .mat the generator wrote is what Unity draws: they must say the same colour
  const hexOf = (name: string) => { const c = /_BaseColor: \{r: ([\d.e-]+), g: ([\d.e-]+), b: ([\d.e-]+)/.exec(readFileSync(join(GEN_ROOT, `Materials/${name}.mat`), 'utf8'))!; return '#' + [c[1]!, c[2]!, c[3]!].map(x => Math.round(+x * 255).toString(16).padStart(2, '0')).join('').toUpperCase(); };
  const want = { DK_Gold: '#99784D', DK_Accent_Gold: '#99784D', DK_Brass_Civic: '#99784D', DK_Gold17: '#AD866F', DK_Crown_Gold: '#E3D681', DK_Crown_Gold17: '#C9C08E' };
  for (const [m, hex] of Object.entries(want)) { expect(hexOf(m), `${m}.mat`).toBe(hex); expect(palette.get(m), `${m} palette row`).toBe(hex); }
  for (const m of Object.keys(want)) expect(new Set(adopted.flatMap(a => a.files.filter(f => f.path === `Materials/${m}.mat`).map(f => f.sha256))).size, `${m}: one file in the catalogue`).toBe(1);
});

test('brief 013 (Vantage, Motive §2.5): only a piece tagged `stash` can hold a body, and today that is the Dumpster, still a candidate until Vela adopts it', () => {
  const stash = kit.assets.filter(a => a.tags.stash);
  expect(stash.map(a => `${a.id} ${a.tags.stash!.holds} ${a.status}`)).toEqual(['Dumpster 1 candidate']);
  expect(stash[0]!.tags.use).toContain('dumpster'); // the street item kind the world places it as
  // size does not decide it: pieces as big as a Dumpster that must not hold a body (a booth, a lamp, a crate stack, a coffee cart)
  for (const id of ['PhoneBooth', 'Streetlamp', 'CrateStack', 'UtilityCabinet', 'RecyclingBins']) expect(kit.assets.find(a => a.id === id)!.tags.stash, id).toBeUndefined();
});
