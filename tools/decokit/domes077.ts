// Brief 077, "the domes go" (Vela): the rest of the dome planting family re-made with the testbed's own planting code, as candidates.
// bun tools/decokit/domes077.ts [--out dir]     (default out/decokit/troughs077; needs paths.json from domes077_paths.py)
//
// Pieces (planting only, in each stonework piece's own local plan frame; Blender (x, y, z) = three (x, -z, y)):
//   PlanterRun_2_<v>_TB         café strip recipe on the 2 m trough coping (as PlanterRun_4_<v>_TB, troughs077.ts)
//   PlanterRun_Corner_<v>_TB    the same clump recipe on anchors along the corner's path (straight-arc-straight), coping footprint
//   PlanterRun_End_<v>_TB       the same on the end piece's straight run and half-round cap
//   BedTuft_<v>_TB              one testbed clump fitted inside the tuft's 0.62 m disc (the per-unit replacement the brief asks for)
//   CivicBed_<bed>_TB           the testbed's own civic beds (its civic-planting-layout flowerBedRun beds, exactly as baked: our
//                               five tufted bed rings are those beds moved rigidly, to < 1 mm), composed by the same code: the
//                               bed-level alternative to BedTuft
//   PlanterArc_TB               createFountainGarden's prototype island (the testbed's fountain-garden.js), turned into PlanterArc's frame
//   GlobeLamp_TB                createFountainGardenLights' lamp (chrome housing + opal globe), a separate piece
//   CafeTerrace_Planting_TB     the café strip clump recipe on anchors along CafeTerrace's two curved trough runs
// Testbed references (its own meshes, for the review): testbed_civic_beds.bin (the bake), testbed_fountain.bin (its own code, stone too).
// Fitting, bloom packing and LODs are the testbed's (createCafeGardenComposition / createFountainGarden). Deterministic.
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
  'cafe-bed-geometry.js', 'garden-render-batches.js', 'garden-runtime.js', 'garden-cache.js', 'fountain-garden.js', 'fountain-garden-layout.js', 'fountain-garden-lights.js',
  'civic-planting-layout.js', 'flower-bed-layout.js', 'civic-district-look.js', 'civic-paving-material.js'];

const THREE = await import(`${TB}/../vendor/three-0.186.1/three.module.min.js`);
const { createCafeGardenComposition } = await import(`${TB}/cafe-garden-composition.js`);
const { flowerLookLeafGeometry } = await import(`${TB}/flower-look-shapes.js`);
const { createFountainGardenLayout } = await import(`${TB}/fountain-garden-layout.js`);
const { createFountainGarden } = await import(`${TB}/fountain-garden.js`);
const { createFountainGardenLights } = await import(`${TB}/fountain-garden-lights.js`);
const PATHS = JSON.parse(readFileSync(resolve(OUT, 'paths.json'), 'utf8'));
const CAFE_LEVELS = ['gameplay', 'near', 'close', 'inspection'];
const PALETTE = { name: 'Concept coral and amber, dense tropical border', palettes: [['coral', 'gold'], ['gold', 'coral'], ['coral', 'gold']] };
const SOIL_LIFT = 0.003;

type P2 = { x: number; y: number };
type Rec = { name: string; kind: string; level: string; attrs: Record<string, { offset: number; length: number; itemSize: number }>; tris: number; verts: number };
const files: Record<string, { sha256: string; bytes: number; meshes: Rec[] }> = {};
function writeSet(file: string, meshes: { name: string; kind: string; level: string; g: any }[]) {
  const chunks: Uint8Array[] = [], out: Rec[] = []; let bytes = 0;
  for (const { name, kind, level, g: g0 } of meshes) {
    const g = g0.index ? g0.toNonIndexed() : g0;
    if (!g.attributes.normal) g.computeVertexNormals();
    const attrs: Rec['attrs'] = {};
    for (const k of ['position', 'normal', 'uv', 'color']) {
      const a = g.attributes[k]; if (!a) continue;
      const arr = new Float32Array(a.array);
      attrs[k] = { offset: bytes, length: arr.length, itemSize: a.itemSize };
      chunks.push(new Uint8Array(arr.buffer)); bytes += arr.byteLength;
    }
    out.push({ name, kind, level, attrs, tris: g.attributes.position.count / 3, verts: g.attributes.position.count });
  }
  const buf = new Uint8Array(bytes); let o = 0; for (const c of chunks) { buf.set(c, o); o += c.byteLength; }
  writeFileSync(resolve(OUT, file), buf);
  files[file] = { sha256: sha(buf), bytes, meshes: out };
}

// ------------------------------------------------------------------ plan helpers
const len = (p: number[][]) => p.slice(1).reduce((s, q, i) => s + Math.hypot(q[0] - p[i][0], q[1] - p[i][1]), 0);
function at(path: number[][], d: number) { // point + unit tangent at distance d
  let acc = 0;
  for (let i = 0; i < path.length - 1; i++) {
    const a = path[i], b = path[i + 1], s = Math.hypot(b[0] - a[0], b[1] - a[1]);
    if (acc + s >= d || i === path.length - 2) { const f = Math.min(1, Math.max(0, (d - acc) / s)); return { x: a[0] + (b[0] - a[0]) * f, y: a[1] + (b[1] - a[1]) * f, tx: (b[0] - a[0]) / s, ty: (b[1] - a[1]) / s }; }
    acc += s;
  }
  throw new Error('path');
}
function vnormals(path: number[][]) { // mitred vertex normals (left), as plaza_parts.offset
  return path.map((p, i) => {
    const a = path[Math.max(0, i - 1)], b = path[Math.min(path.length - 1, i + 1)];
    const t0 = i > 0 ? norm([p[0] - a[0], p[1] - a[1]]) : null, t1 = i < path.length - 1 ? norm([b[0] - p[0], b[1] - p[1]]) : null;
    const t = t0 && t1 ? norm([t0[0] + t1[0], t0[1] + t1[1]]) : (t0 ?? t1)!;
    const k = t0 && t1 ? 1 / Math.max(0.2, t0[0] * t[0] + t0[1] * t[1]) : 1;
    return [-t[1] * k, t[0] * k];
  });
}
function norm(v: number[]) { const l = Math.hypot(v[0], v[1]); return [v[0] / l, v[1] / l]; }
function band(path: number[][], half: number, cap?: number[]): P2[] {
  const n = vnormals(path), left = path.map((p, i) => ({ x: p[0] + n[i][0] * half, y: p[1] + n[i][1] * half }));
  const right = path.map((p, i) => ({ x: p[0] - n[i][0] * half, y: p[1] - n[i][1] * half })).reverse();
  const out = [...left];
  if (cap) { // half-round end about cap (cx, cy, r), from the left side round to the right
    const e = path[path.length - 1], t = norm([e[0] - path[path.length - 2][0], e[1] - path[path.length - 2][1]]), a0 = Math.atan2(t[0], -t[1]);
    out.pop(); out.push({ x: cap[0] - t[1] * cap[2], y: cap[1] + t[0] * cap[2] });
    for (let k = 1; k < 12; k++) { const a = a0 - Math.PI * k / 12; out.push({ x: cap[0] + Math.cos(a) * cap[2], y: cap[1] + Math.sin(a) * cap[2] }); }
    out.push({ x: cap[0] + t[1] * cap[2], y: cap[1] - t[0] * cap[2] }); right.shift();
  }
  return [...out, ...right].reverse(); // counter-clockwise, as the testbed's bedGeometry expects (its soil is the mitred inward offset)
}
const bounds = (v: P2[]) => ({ x: [Math.min(...v.map(p => p.x)), Math.max(...v.map(p => p.x))], y: [Math.min(...v.map(p => p.y)), Math.max(...v.map(p => p.y))] });
function anchors(path: number[][], extra = 0) { // the café strip's clump count and spacing, on the path's centreline
  const L = len(path) + extra, count = Math.max(1, Math.ceil((L - 0.08) / 0.38)), pts = [];
  for (let i = 0; i < count; i++) {
    const d = 0.06 + (L - 0.12) * (i + 0.5) / count;
    let q;
    if (d <= len(path)) q = at(path, d); else { const e = at(path, len(path)); q = { ...e, x: e.x + e.tx * (d - len(path)), y: e.y + e.ty * (d - len(path)) }; }
    pts.push({ x: q.x, y: q.y, heading: Math.atan2(q.ty, q.tx), layer: 'border' });
  }
  return pts;
}

const mat = (name: string) => new THREE.MeshStandardMaterial({ name });
function compose(beds: any[]) {
  return createCafeGardenComposition({ beds, materials: { foliage: mat('foliage'), petals: mat('petals'), soil: mat('soil') }, stoneMaterial: mat('stone'),
    leafGeometryFactory: flowerLookLeafGeometry, spatialBatches: false, textureResolution: [1774, 887] });
}
const composition: Record<string, any> = {};
// Soil for an open-ended run (the 077 joint fix): the testbed's bedGeometry insets the soil by the wall on every edge, which leaves 5.5 cm of
// stone at each open end where two pieces meet. Here the soil is the run's band inset on the side walls only (and round a closed cap), flush
// with the open ends, so neighbouring pieces' soil meets. Same height (top - 0.055), same UV (the testbed's stoneUV: x / 2, plan y / 2).
function soilMesh(polys: P2[][], h: number) {
  const parts = polys.map(poly => {
    const g = new THREE.ShapeGeometry(new THREE.Shape(poly.map(p => new THREE.Vector2(p.x, p.y))));
    g.rotateX(-Math.PI / 2); g.translate(0, h, 0);
    const pos = g.attributes.position, uv = g.attributes.uv;
    for (let i = 0; i < pos.count; i++) uv.setXY(i, pos.getX(i) / 2, -pos.getZ(i) / 2);
    return g.toNonIndexed();
  });
  const n = parts.reduce((k, g) => k + g.attributes.position.count, 0), P = new Float32Array(n * 3), U = new Float32Array(n * 2), N = new Float32Array(n * 3);
  let o = 0;
  for (const g of parts) { P.set(g.attributes.position.array, o * 3); U.set(g.attributes.uv.array, o * 2); for (let i = 0; i < g.attributes.position.count; i++) N[(o + i) * 3 + 1] = 1; o += g.attributes.position.count; }
  const out = new THREE.BufferGeometry();
  out.setAttribute('position', new THREE.BufferAttribute(P, 3)); out.setAttribute('uv', new THREE.BufferAttribute(U, 2)); out.setAttribute('normal', new THREE.BufferAttribute(N, 3));
  return out;
}
function cafePiece(piece: string, beds: any[], { soil = true, soilPolys = null as P2[][] | null, xform = null as any } = {}) {
  const kit = compose(beds), meshes: any[] = [];
  for (const level of CAFE_LEVELS) {
    kit.setLOD(level);
    for (const m of kit.pickables) {
      const kind = m.name.split(' ').at(-1);
      if (kind === 'stone' || (kind === 'soil' && (!soil || level !== 'gameplay'))) continue;
      const g = kind === 'soil' && soilPolys ? soilMesh(soilPolys, beds[0].z[1] - 0.055) : xform ? m.geometry.clone().applyMatrix4(xform) : m.geometry;
      meshes.push({ name: piece, kind, level, g });
    }
  }
  writeSet(`${piece}.bin`, meshes);
  const s = kit.statistics();
  composition[piece] = { levels: CAFE_LEVELS, leafCount: s.leafCount, flowerHeads: s.flowerHeads, footprintViolations: s.footprintViolations, bloomIntersections: s.bloomIntersections,
    bloomLeafIntersections: s.bloomLeafIntersections, minimumBloomClearance: s.minimumBloomClearance, beds: s.beds.map((b: any) => ({ id: b.id, seed: b.seed, clusters: b.clusters.length })) };
}
const rbed = (id: string, vertices: P2[], top: number, patches: any[], extra: any = {}) => ({ id, kind: 'planter', type: 'polygon', ...bounds(vertices), z: [0, top + SOIL_LIFT + 0.055], wall: 0.055,
  vertices, patches, plantingProfile: 'lush-tropical', palette: PALETTE, ...extra });

// ------------------------------------------------------------------ 1. PlanterRun_2 / Corner / End
for (const kind of ['2', 'Corner', 'End']) for (const v of ['A', 'B', 'C']) {
  const r = PATHS.runs[kind], id = `PlanterRun_${kind}_${v}`;
  let verts: P2[], patches: any[];
  if (kind === '2') {
    verts = [[-1, -0.34], [1, -0.34], [1, 0.34], [-1, 0.34]].map(([x, y]) => ({ x, y }));
    patches = [{ id: `${id}-planting`, x: [-1, 1], y: [-0.34, 0.34] }]; // the café strip, as PlanterRun_4
  } else {
    verts = band(r.path, r.coping_half, r.end_cap ?? undefined);
    const pts = anchors(r.path, r.end_cap ? 0.15 : 0);
    patches = [{ id: `${id}-planting`, ...bounds(verts), plantingPoints: pts, width: 0.68 }];
  }
  cafePiece(`${id}_TB`, [rbed(id, verts, r.coping_top, patches, { module: `testbed café clump recipe on ${id} coping` })]);
}

// ------------------------------------------------------------------ 2a. BedTuft: one clump in the 0.62 m disc
for (const v of ['A', 'B', 'C']) {
  const id = `BedTuft_${v}`, verts = Array.from({ length: 24 }, (_, i) => ({ x: 0.31 * Math.cos(i * Math.PI / 12), y: 0.31 * Math.sin(i * Math.PI / 12) }));
  cafePiece(`${id}_TB`, [{ ...rbed(id, verts, -SOIL_LIFT - 0.055 + 0.055, [{ id: `${id}-planting`, x: [-0.31, 0.31], y: [-0.31, 0.31], plantingPoints: [{ x: 0, y: 0, heading: 0, layer: 'border' }], width: 0.62 }]),
    z: [0, 0.055] }], { soil: false }); // soil level = 0: the clump stands on the bed fill at the tuft's pivot
}

// ------------------------------------------------------------------ 2b. the testbed's civic beds (bed level)
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
const CIVIC = ['civic-facade-southwest', 'civic-facade-southeast', 'civic-office-west', 'civic-office-north', 'civic-dropoff-west'];
const bake = decode('plaza-garden.bin.gz');
const keyBeds = JSON.parse(bake.data.key).beds;
const bakeLod: Record<string, Record<string, number>> = {};
for (const id of CIVIC) {
  const b = keyBeds.find((x: any) => x.id === id);
  const top = b.z[1]; // 0.52: our kerb is a solid prism 0.52 high, so the soil sits 3 mm proud of it (as on the troughs)
  cafePiece(`CivicBed_${id}_TB`, [{ ...b, z: [0, top + SOIL_LIFT + 0.055] }]);
  bakeLod[id] = {};
  for (const bt of bake.data.batches.filter((x: any) => x.id === id)) for (const lv of ['gameplay', 'near']) {
    const g = bake.data.geometries[bt.geometry[lv]]; bakeLod[id][`${bt.kind}:${lv}`] = (g.index ? g.index.length : g.attributes.position.length / 3) / 3;
  }
}
// where each testbed bed sits in our world: the rigid transform (plan rotation + translation) that maps its outline onto the world
// export's bed-kerb ring, vertex i to vertex i (no start search: see the loop); the rings match to < 1 mm.
const WORLD = JSON.parse(readFileSync(resolve(repo, 'out/world/harbour-district.world.json'), 'utf8'));
const civicBeds: Record<string, any> = {};
for (const id of CIVIC) {
  const A = keyBeds.find((x: any) => x.id === id).vertices.map((v: any) => [v.x, v.y]);
  const B = WORLD.civic.find((e: any) => e.id === `civic/${id}` && e.surface === 'bed-kerb').ring;
  const mean = (P: number[][]) => [P.reduce((s, p) => s + p[0], 0) / P.length, P.reduce((s, p) => s + p[1], 0) / P.length];
  let best: any = null;
  for (const k of [0]) { // vertex i to vertex i (Splice's rule): a searched start fits the half-turn-symmetric runs two ways and can turn a composition 180 deg
    const Bs = B.map((_: any, i: number) => B[(i + k) % B.length]), ca = mean(A), cb = mean(Bs);
    let sxx = 0, sxy = 0;
    for (let i = 0; i < A.length; i++) { const a = [A[i][0] - ca[0], A[i][1] - ca[1]], b = [Bs[i][0] - cb[0], Bs[i][1] - cb[1]]; sxx += a[0] * b[0] + a[1] * b[1]; sxy += a[0] * b[1] - a[1] * b[0]; }
    const th = Math.atan2(sxy, sxx), c = Math.cos(th), sn = Math.sin(th), t = [cb[0] - (c * ca[0] - sn * ca[1]), cb[1] - (sn * ca[0] + c * ca[1])];
    const err = Math.max(...A.map((a: number[], i: number) => Math.hypot(c * a[0] - sn * a[1] + t[0] - Bs[i][0], sn * a[0] + c * a[1] + t[1] - Bs[i][1])));
    if (!best || err < best.err) best = { angle: th, t, err };
  }
  civicBeds[id] = { ...best, base: WORLD.civic.find((e: any) => e.id === `civic/${id}` && e.surface === 'bed-kerb').base };
}
// testbed reference: the baked civic beds, every level the bake holds, stone and soil too
const tbBeds: any[] = [];
for (const [file, levels] of [['plaza-garden.bin.gz', ['gameplay', 'near']], ['plaza-garden-close.bin.gz', ['close']], ['plaza-garden-inspection.bin.gz', ['inspection']]] as const) {
  const { data, geo } = file === 'plaza-garden.bin.gz' ? bake : decode(file);
  for (const b of data.batches) if (CIVIC.includes(b.id)) for (const level of levels) {
    if (b.kind === 'soil' && level !== 'gameplay') continue;
    tbBeds.push({ name: `testbed_${b.id}`, kind: b.kind, level, g: geo(data.geometries[b.geometry[level]]) });
  }
}
writeSet('testbed_civic_beds.bin', tbBeds);

// ------------------------------------------------------------------ 3. the fountain islands + globe lamps
const FL = ['gameplay', 'near', 'close'];
const layout = createFountainGardenLayout({ x: 0, y: 0 });
const garden = createFountainGarden(layout, { x: 0, y: 0 }, { foliage: mat('f'), petals: mat('p'), soil: mat('s') }, mat('stone'));
// prototype bed 0 (north-east, centred at plan angle pi/4) -> PlanterArc's frame: the bed on -Y of the fountain, fountain at local +Y 6.12
const toArc = new THREE.Matrix4().makeTranslation(0, 0, -PATHS.arc.C).multiply(new THREE.Matrix4().makeRotationY(-3 * Math.PI / 4));
const arcMeshes: any[] = [], tbFountain: any[] = [];
for (const level of FL) {
  garden.updateLOD({ worldWidth: 1, viewportWidth: 1, override: level });
  for (const m of garden.meshes) {
    const kind = m.name.split(' ').at(-1);
    if (!(kind === 'soil' && level !== 'gameplay')) for (let i = 0; i < 4; i++) { // the testbed draws its prototype four times
      const im = new THREE.Matrix4(); m.getMatrixAt(i, im);
      tbFountain.push({ name: `testbed_fountain_${i}`, kind, level, g: m.geometry.clone().applyMatrix4(im) });
    }
    if (kind === 'stone' || (kind === 'soil' && level !== 'gameplay')) continue; // our PlanterArc stonework stays
    arcMeshes.push({ name: 'PlanterArc_TB', kind, level, g: m.geometry.clone().applyMatrix4(toArc) });
  }
}
writeSet('PlanterArc_TB.bin', arcMeshes);
writeSet('testbed_fountain.bin', tbFountain);
composition['PlanterArc_TB'] = { levels: FL, ...garden.statistics(), lodRule: 'fountain-garden.js: projected 0.55 m leaf span; close >= 44 px, near >= 24 px, else gameplay' };

const lights = createFountainGardenLights(layout, { x: 0, y: 0 }, mat('chrome'));
const lampMeshes: any[] = [], lampAt: any[] = [];
for (const c of lights.group.children) if (c.type === 'Mesh' || c.isInstancedMesh) {
  const kind = c.name.includes('globe') ? 'globe' : 'chrome';
  lampMeshes.push({ name: 'GlobeLamp_TB', kind, level: 'gameplay', g: c.geometry.clone() });
  if (kind === 'chrome') for (let i = 0; i < 4; i++) { const im = new THREE.Matrix4(); c.getMatrixAt(i, im); const p = new THREE.Vector3().setFromMatrixPosition(im); lampAt.push({ i, plan: [p.x, -p.z], base: p.y }); }
}
writeSet('GlobeLamp_TB.bin', lampMeshes);
// lamp 0 stands in bed 0: in PlanterArc's frame
const l0 = new THREE.Vector3(lampAt[0].plan[0], lampAt[0].base, -lampAt[0].plan[1]).applyMatrix4(toArc);
composition['GlobeLamp_TB'] = { testbed: lampAt, inPlanterArcFrame: { at: [l0.x, -l0.z], z: l0.y, yawFromArc: 0 }, light: { colour: '#fff1d8', range: 5.8, decay: 2, shadows: false, at: 2.31 },
  materials: { chrome: '#d8dfe4 metal 1 rough .16 (testbed chrome finish)', globe: 'opal #fffdf0, emissive #fff1d2 x 0.06' } };

// ------------------------------------------------------------------ 4. CafeTerrace's two runs
{
  const T = PATHS.terrace, beds = T.runs.map((run: number[][], k: number) => {
    const verts = band(run, T.coping_half);
    return rbed(`CafeTerrace-run-${k}`, verts, T.coping_top, [{ id: `CafeTerrace-run-${k}-planting-${T.variants[k]}`, ...bounds(verts), plantingPoints: anchors(run), width: 0.66 }]);
  });
  cafePiece('CafeTerrace_Planting_TB', beds);
}

// ------------------------------------------------------------------ FINAL (Sky 2026-10-30 "go"): the planting of the original ids, soil fixed at open ends
const finals: Record<string, any> = {};
const runSoil = (path: number[][], half: number, cap?: number[]) => band(path, half - 0.055, cap ? [cap[0], cap[1], cap[2] - 0.055] : undefined);
for (const kind of ['2', '4', 'Corner', 'End']) for (const v of ['A', 'B', 'C']) {
  const r = PATHS.runs[kind], id = `PlanterRun_${kind}_${v}`;
  let verts: P2[], patches: any[], soilPolys: P2[][];
  if (kind === '2' || kind === '4') {
    const L = kind === '2' ? 1 : 2;
    verts = [[-L, -0.34], [L, -0.34], [L, 0.34], [-L, 0.34]].map(([x, y]) => ({ x, y }));
    patches = [{ id: `${id}-planting`, x: [-L, L], y: [-0.34, 0.34] }]; // the café strip (PlanterRun_4: the same bed as troughs077.ts, so the same planting)
    soilPolys = [[[-L, -0.285], [L, -0.285], [L, 0.285], [-L, 0.285]].map(([x, y]) => ({ x, y }))];
  } else {
    verts = band(r.path, r.coping_half, r.end_cap ?? undefined);
    patches = [{ id: `${id}-planting`, ...bounds(verts), plantingPoints: anchors(r.path, r.end_cap ? 0.15 : 0), width: 0.68 }];
    soilPolys = [runSoil(r.path, r.coping_half, r.end_cap ?? undefined)];
  }
  cafePiece(`final_${id}`, [rbed(id, verts, r.coping_top, patches, { module: `testbed café clump recipe on ${id} coping` })], { soilPolys });
  finals[id] = { file: `final_${id}.bin`, rule: 'cafe', parent: id, embed: true };
}
// The generator's inward offsets fold back on themselves at tight concave corners (the Records roof: reversals of ~178 deg over a few cm).
// A tube hides that; a planting footprint cannot, so the run is cleaned: near-duplicate points dropped, any point that reverses the
// direction removed. A run that closes on itself (ends < 5 cm apart) is planted as two open halves that meet end to end.
function cleanRuns(runs: number[][][]) {
  const out: number[][][] = [];
  for (const run of runs) {
    const k: number[][] = [];
    for (const p of run) {
      if (k.length && Math.hypot(p[0] - k[k.length - 1][0], p[1] - k[k.length - 1][1]) < 0.05) continue;
      while (k.length >= 2) {
        const a = k[k.length - 2], b = k[k.length - 1];
        if ((p[0] - b[0]) * (b[0] - a[0]) + (p[1] - b[1]) * (b[1] - a[1]) < 0) k.pop(); else break;
      }
      k.push(p);
    }
    const closed = Math.hypot(k[0][0] - k[k.length - 1][0], k[0][1] - k[k.length - 1][1]) < 0.05;
    if (closed) { const m = Math.floor(k.length / 2); out.push(k.slice(0, m + 1), [...k.slice(m, -1), k[0]]); } else out.push(k);
  }
  return out;
}
function runsPiece(id: string, T0: any, variants: string[]) {
  const T = { ...T0, runs: cleanRuns(T0.runs) };
  const beds = T.runs.map((run: number[][], k: number) => {
    const verts = band(run, T.coping_half);
    return rbed(`${id}-run-${k}`, verts, T.coping_top, [{ id: `${id}-run-${k}-planting-${variants[k % variants.length]}`, ...bounds(verts), plantingPoints: anchors(run), width: 2 * T.coping_half }]);
  });
  cafePiece(`final_${id}_Planting`, beds, { soilPolys: T.runs.map((run: number[][]) => runSoil(run, T.coping_half)) });
}
runsPiece('CafeTerrace', PATHS.terrace, PATHS.terrace.variants); finals['CafeTerrace_Planting'] = { file: 'final_CafeTerrace_Planting.bin', rule: 'cafe', parent: 'CafeTerrace', embed: false };
runsPiece('RecordsTower', PATHS.records_roof, ['A']); finals['RecordsTower_Planting'] = { file: 'final_RecordsTower_Planting.bin', rule: 'cafe', parent: 'RecordsTower', embed: false };
for (const n of ['3', '4']) { runsPiece(`CafeUpper_${n}`, PATHS.cafe_crown[n], ['A']); finals[`CafeUpper_${n}_Planting`] = { file: `final_CafeUpper_${n}_Planting.bin`, rule: 'cafe', parent: `CafeUpper_${n}`, embed: false }; }
// the five civic beds, each in a frame centred on its world ring (pivot = ring centroid at the kerb base, placed yaw 0: Blender rotation pi)
for (const id of CIVIC) {
  const b = keyBeds.find((x: any) => x.id === id), t = civicBeds[id];
  const ring = WORLD.civic.find((e: any) => e.id === `civic/${id}` && e.surface === 'bed-kerb').ring;
  const c = [ring.reduce((s: number, p: number[]) => s + p[0], 0) / ring.length, ring.reduce((s: number, p: number[]) => s + p[1], 0) / ring.length];
  const xf = new THREE.Matrix4().makeTranslation(c[0] - t.t[0], 0, -(c[1] - t.t[1])).multiply(new THREE.Matrix4().makeRotationY(t.angle - Math.PI));
  const pid = `CivicBedPlanting_${id.replace(/^civic-/, '')}`;
  cafePiece(`final_${pid}`, [{ ...b, z: [0, b.z[1] + SOIL_LIFT + 0.055] }], { xform: xf });
  finals[pid] = { file: `final_${pid}.bin`, rule: 'cafe', parent: null, embed: false, place: { at: c, yaw: 0, base: t.base, world: `civic/${id}`, fit: t.err } };
}
finals['PlanterArc'] = { file: 'PlanterArc_TB.bin', rule: 'fountain', parent: 'PlanterArc', embed: true };
finals['GlobeLamp_TB'] = { file: 'GlobeLamp_TB.bin', rule: 'none', parent: null, embed: false };

const manifest = {
  schema: 'suborned.domes077/1', brief: '077 the domes go', author: 'Vela', status: 'candidate',
  frame: 'three.js y-up (x, height, -plan_y) in each stonework piece\'s local plan frame; CivicBed_*: the testbed\'s own plaza frame (placements in civicBeds)',
  sources: Object.fromEntries(SOURCES.map(f => [`testbed/src/${f}`, sha(readFileSync(resolve(TB, f)))])),
  generator: { 'tools/decokit/domes077.ts': sha(readFileSync(import.meta.path)), 'out/decokit/troughs077/paths.json': sha(readFileSync(resolve(OUT, 'paths.json'))) },
  composition, bakeLod, civicBeds, finals, files,
};
writeFileSync(resolve(OUT, 'domes077.manifest.json'), JSON.stringify(manifest, null, 1) + '\n');
for (const [k, v] of Object.entries(files)) console.log(k, v.meshes.length, 'meshes', v.bytes);
console.log('civic bed fits', JSON.stringify(Object.fromEntries(Object.entries(civicBeds).map(([k, v]: any) => [k, [Math.round(v.angle * 180 / Math.PI), v.err.toFixed(4)]]))));
console.log('DOMES077 footprintViolations', Object.values(composition).reduce((n: number, c: any) => n + (c.footprintViolations ?? 0), 0));
