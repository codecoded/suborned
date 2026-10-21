// Bakes the testbed's own basin geometry (testbed/src/fountain-geometry.js, three 0.186.1) at its finest tessellation ('close') to basin-close.json.
// bun tools/decokit/art/fountain/dump_basin.mjs [testbed_src_dir]; the sculpture (Blender) is not part of it (the live scene builds the basin with includeSculpture:false).
// waterContactRadius 0.546001283211071 = the testbed's own sectionRadius() of atom-close-v1.glb's silver mesh at the water height 0.39 (measured in Blender over the glb's triangles).
import {writeFileSync} from 'node:fs';
import {resolve, dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
const src = process.argv[2] ?? resolve(process.env.HOME, 'Projects/the-suborned/testbed/src');
const {buildFountainGeometry} = await import(resolve(src, 'fountain-geometry.js'));
const {fountainSpec} = await import(resolve(src, 'fountain-spec.js'));
const g = buildFountainGeometry('close', fountainSpec, {includeSculpture: false, waterContactRadius: 0.546001283211071});
const r = v => Math.round(v * 1e6) / 1e6;
const out = {spec: fountainSpec, level: 'close', waterContactRadius: 0.546001283211071, kinds: {}};
for (const [kind, geo] of Object.entries(g)) {
  const p = geo.attributes.position, uv = geo.attributes.uv, ix = geo.index;
  out.kinds[kind] = {v: Array.from(p.array, r), uv: uv ? Array.from(uv.array, r) : [], t: ix ? Array.from(ix.array) : Array.from({length: p.count}, (_, i) => i)};
}
const tri = Object.fromEntries(Object.entries(out.kinds).map(([k, o]) => [k, o.t.length / 3]));
out.triangles = tri;
writeFileSync(resolve(dirname(fileURLToPath(import.meta.url)), 'basin-close.json'), JSON.stringify(out));
console.log(tri, Object.values(tri).reduce((a, b) => a + b, 0));
