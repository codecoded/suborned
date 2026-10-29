// Brief 077 step 1 (Vela): the testbed's café trough planting, composed by the testbed's own code, for the PlanterRun_4_A/B/C troughs.
// bun tools/decokit/troughs077.ts [--out dir]   (default out/decokit/troughs077)
//
// Runs the testbed's composition unchanged (createCafeGardenComposition with flowerLookLeafGeometry, the lush-tropical profile and the
// palette it drew, exactly as cafe-planting.js createCafePlanting passes them) on one bed per trough variant: the PlanterRun coping
// footprint (4.0 x 0.68 m, |x| <= 2.0, |y| <= 0.34) as the fitting polygon, soil 3 mm above the coping top (0.655). Every vertex is
// fitted inside that footprint by the testbed's own fitGeometry; blossoms are packed by its own sphere/triangle test.
// Writes per variant and level (gameplay, near, close, inspection) the foliage, flowers and soil meshes as raw float32 arrays + a
// manifest, and the testbed's own baked café beds (plaza-garden*.bin.gz: bed-west, bed-east, bed-side) the same way, so the Blender
// side (troughs077.py) builds kit pieces and the review from one format. Deterministic: same inputs, same bytes (sha256 in the manifest).
// Read-only sources: ~/Projects/the-suborned/testbed/src (code), ~/Projects/the-suborned-store/testbed-art/cafe-plants (art, bake).
import { createHash } from 'node:crypto';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { gunzipSync } from 'node:zlib';
import { resolve } from 'node:path';

const HOME = process.env.HOME!;
const TB = process.env.TESTBED_SRC ?? `${HOME}/Projects/the-suborned/testbed/src`;
const ART = process.env.TESTBED_ART ?? `${HOME}/Projects/the-suborned-store/testbed-art/cafe-plants`;
const repo = resolve(import.meta.dir, '../..');
const argv = process.argv.slice(2), opt = (k: string) => { const i = argv.indexOf(k); return i >= 0 ? argv[i + 1] : undefined; };
const OUT = resolve(opt('--out') ?? resolve(repo, 'out/decokit/troughs077'));
mkdirSync(OUT, { recursive: true });

const sha = (b: Uint8Array | string) => createHash('sha256').update(b).digest('hex');
const SOURCES = ['cafe-garden-composition.js', 'flower-look-shapes.js', 'garden-botanical-geometry.js', 'cafe-botanical-geometry.js', 'garden-stem-geometry.js',
  'cafe-bed-geometry.js', 'garden-render-batches.js', 'garden-runtime.js', 'garden-cache.js', 'cafe-planting.js', 'cafe-planting-layout.js', 'flower-bed-layout.js', 'civic-district-look.js'];
const ARTS = ['botanical-atlas-v2.png', 'botanical-normal-v2.png', 'botanical-orm-v2.png', 'soil-base.png', 'soil-normal.png', 'soil-orm.png',
  'botanical-report-v2.json', 'plaza-garden.manifest.json', 'plaza-garden.bin.gz', 'plaza-garden-close.bin.gz', 'plaza-garden-inspection.bin.gz'];

const THREE = await import(`${TB}/../vendor/three-0.186.1/three.module.min.js`);
const { createCafeGardenComposition } = await import(`${TB}/cafe-garden-composition.js`);
const { flowerLookLeafGeometry } = await import(`${TB}/flower-look-shapes.js`);
const LEVELS = ['gameplay', 'near', 'close', 'inspection'] as const;

// The trough: civic_common.trough (w 0.62: body |y| <= 0.29, coping |y| <= 0.34 from z 0.600 to 0.655) along (-2,0)-(2,0).
const COPING = { x: [-2.0, 2.0], y: [-0.34, 0.34], top: 0.655 };
const SOIL_LIFT = 0.003; // soil sits 3 mm proud of the coping top (the testbed's soil is z[1] - 0.055)
// The palette the testbed drew on every café bed (cafe-planting-layout.js, lush-tropical override of the older schemes).
const PALETTE = { name: 'Concept coral and amber, dense tropical border', palettes: [['coral', 'gold'], ['gold', 'coral'], ['coral', 'gold']] };

function bed(variant: string) {
  const id = `PlanterRun_4_${variant}`, [x0, x1] = COPING.x, [y0, y1] = COPING.y;
  return {
    id, kind: 'planter', type: 'polygon', x: [x0, x1], y: [y0, y1], z: [0, COPING.top + SOIL_LIFT + 0.055], wall: 0.055,
    module: 'flower-strip (testbed café strip recipe) on PlanterRun_4 coping',
    vertices: [[x0, y0], [x1, y0], [x1, y1], [x0, y1]].map(([x, y]) => ({ x, y })),
    // One patch along the trough, as the café's strips (bed-west-left/-right, bed-east-*, bed-side-lower): count = ceil((L - .08) / .38) clumps.
    patches: [{ id: `${id}-planting`, x: [x0, x1], y: [y0, y1] }],
    plantingProfile: 'lush-tropical', palette: PALETTE,
  };
}

type Mesh = { name: string; kind: string; level: string; attrs: Record<string, { offset: number; length: number; itemSize: number }>; tris: number; verts: number };
const files: Record<string, { sha256: string; bytes: number; meshes: Mesh[] }> = {};

function writeSet(file: string, meshes: { name: string; kind: string; level: string; g: any }[]) {
  const chunks: Uint8Array[] = [], out: Mesh[] = [];
  let bytes = 0;
  for (const { name, kind, level, g: g0 } of meshes) {
    const g = g0.index ? g0.toNonIndexed() : g0;
    if (!g.attributes.normal) g.computeVertexNormals();
    const attrs: Mesh['attrs'] = {};
    for (const k of ['position', 'normal', 'uv', 'color']) {
      const a = g.attributes[k];
      if (!a) continue;
      const arr = new Float32Array(a.array);
      attrs[k] = { offset: bytes, length: arr.length, itemSize: a.itemSize };
      chunks.push(new Uint8Array(arr.buffer)); bytes += arr.byteLength;
    }
    const verts = g.attributes.position.count;
    out.push({ name, kind, level, attrs, tris: verts / 3, verts });
  }
  const buf = new Uint8Array(bytes); let o = 0; for (const c of chunks) { buf.set(c, o); o += c.byteLength; }
  writeFileSync(resolve(OUT, file), buf);
  files[file] = { sha256: sha(buf), bytes, meshes: out };
}

// ------------------------------------------------------------------ proposed: the testbed composition on our troughs
const mat = (name: string) => new THREE.MeshStandardMaterial({ name });
const composition: Record<string, any> = {};
for (const v of ['A', 'B', 'C']) {
  const kit = createCafeGardenComposition({
    beds: [bed(v)], materials: { foliage: mat('foliage'), petals: mat('petals'), soil: mat('soil') }, stoneMaterial: mat('stone'),
    leafGeometryFactory: flowerLookLeafGeometry, spatialBatches: false, textureResolution: [1774, 887],
  });
  const meshes: { name: string; kind: string; level: string; g: any }[] = [];
  for (const level of LEVELS) {
    kit.setLOD(level);
    for (const m of kit.pickables) {
      const kind = m.name.split(' ').at(-1);
      if (kind === 'stone') continue; // our PlanterRun trough (restyled DK_Limestone_Tex) stays; the testbed shell is not ported
      if (kind === 'soil' && level !== 'gameplay') continue; // one soil mesh shared by every level (as the testbed)
      meshes.push({ name: `PlanterRun_4_${v}_TB`, kind, level, g: m.geometry });
    }
  }
  writeSet(`PlanterRun_4_${v}_TB.bin`, meshes);
  const s = kit.statistics();
  composition[v] = { leafCount: s.leafCount, flowerHeads: s.flowerHeads, footprintViolations: s.footprintViolations, bloomIntersections: s.bloomIntersections,
    bloomLeafIntersections: s.bloomLeafIntersections, minimumBloomClearance: s.minimumBloomClearance, fittedGeometryVariants: s.fittedGeometryVariants,
    lod: s.lod, seed: s.beds[0].seed, clusters: s.beds[0].clusters.length, palette: s.beds[0].palette };
}

// ------------------------------------------------------------------ testbed: its own baked café beds, decoded as garden-cache.js does
function decode(file: string) {
  const buffer = gunzipSync(readFileSync(resolve(ART, file)));
  const ab = buffer.buffer.slice(buffer.byteOffset, buffer.byteOffset + buffer.byteLength);
  const size = new DataView(ab).getUint32(0, true), start = (4 + size + 3) & ~3;
  const data = JSON.parse(new TextDecoder().decode(new Uint8Array(ab, 4, size)));
  const geo = (g: any) => {
    const out = new THREE.BufferGeometry();
    for (const [k, a] of Object.entries<any>(g.attributes)) out.setAttribute(k, new THREE.BufferAttribute(new Float32Array(ab, start + a.offset, a.length), a.itemSize));
    if (g.index) { const T = ({ Uint32Array, Uint16Array } as any)[g.index.type]; out.setIndex(new THREE.BufferAttribute(new T(ab, start + g.index.offset, g.index.length), 1)); }
    return out;
  };
  return { data, geo };
}
const CAFE = ['bed-west', 'bed-east', 'bed-side'];
const tbMeshes: { name: string; kind: string; level: string; g: any }[] = [];
let tbStats: any = null;
for (const [file, levels] of [['plaza-garden.bin.gz', ['gameplay', 'near']], ['plaza-garden-close.bin.gz', ['close']], ['plaza-garden-inspection.bin.gz', ['inspection']]] as const) {
  const { data, geo } = decode(file);
  if (file === 'plaza-garden.bin.gz') tbStats = { lod: data.statistics.lod, leafCount: data.statistics.leafCount, flowerHeads: data.statistics.flowerHeads, beds: data.statistics.beds.filter((b: any) => CAFE.includes(b.id)).map((b: any) => ({ id: b.id, seed: b.seed, palette: b.palette, clusters: b.clusters.length, heads: b.clusters.reduce((n: number, c: any) => n + c.flowerHeads, 0) })), key: JSON.parse(data.key).beds.filter((b: any) => CAFE.includes(b.id)) };
  for (const b of data.batches) {
    if (!CAFE.includes(b.id)) continue;
    for (const level of levels) {
      if (b.kind === 'soil' && level !== 'gameplay') continue;
      tbMeshes.push({ name: `testbed_${b.id}`, kind: b.kind, level, g: geo(data.geometries[b.geometry[level]]) });
    }
  }
}
writeSet('testbed_cafe_beds.bin', tbMeshes);

const manifest = {
  schema: 'suborned.troughs077/1', brief: '077', author: 'Vela', status: 'candidate',
  frame: 'three.js y-up metres as the testbed writes them: (x, height, -plan_y). Blender (x, plan_y, z) = (x, -z3, y3). Pivot = trough footprint centre at its base.',
  coping: COPING, soilLift: SOIL_LIFT, palette: PALETTE,
  levels: LEVELS, lodSelection: 'testbed garden-runtime.js: projected 0.4 m leaf span; gameplay < 18 px (16 hysteresis), near < 32 (28), close < 72 (60), inspection above',
  sources: Object.fromEntries(SOURCES.map(f => [`testbed/src/${f}`, sha(readFileSync(resolve(TB, f)))])),
  three: { 'testbed/vendor/three-0.186.1/three.module.min.js': sha(readFileSync(resolve(TB, '../vendor/three-0.186.1/three.module.min.js'))) },
  art: Object.fromEntries(ARTS.map(f => [`testbed-art/cafe-plants/${f}`, sha(readFileSync(resolve(ART, f)))])),
  generator: { 'tools/decokit/troughs077.ts': sha(readFileSync(import.meta.path)) },
  composition, testbed: tbStats, files,
};
writeFileSync(resolve(OUT, 'troughs077.manifest.json'), JSON.stringify(manifest, null, 1) + '\n');
for (const v of ['A', 'B', 'C']) console.log(`PlanterRun_4_${v}_TB`, JSON.stringify(composition[v]));
console.log('testbed café', JSON.stringify(tbStats.beds));
console.log('wrote', OUT);
