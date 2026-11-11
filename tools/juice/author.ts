// usage: bun tools/juice/author.ts [--check]
// Brief 109 (Sky 2026-11-11: "surely using shader graph if means faster iteration is better?"): the authoring test, and the one place the juice's
// Shader Graph and VFX Graph assets are written from. A crew agent can't click in the graph editors, so every graph here is written as TEXT from one of
// Unity's own templates (the Editor's BuiltInPackages), the way any agent would edit it, and Unity's importer then has the last word
// (Editor Suborned.World.Play.Juice109Authoring: did it compile, are the properties and defaults what the text says, what does one pixel draw).
//
//   Shader Graph (.shadergraph, multi-JSON):  from "Cross Pipeline/Unlit Simple" (base map x colour -> base colour), the HDRP target stripped:
//     v1 authored    transparent additive, no shadows, the colour renamed Tint (_Tint, HDR), the map's alpha wired to Alpha
//     v2 tuned       v1 with one value changed: the Tint default
//     v3 structural  v2 with two nodes added: Vertex Color and a second Multiply between the first and Base Color (what Shuriken's colour needs)
//   VFX Graph (.vfx, Unity YAML):  from "Simple_Burst":
//     v1 authored    the template as it is
//     v2 tuned       v1 with the burst count and the colour gradient changed by text
//   Juice_TyreSmoke.vfx (the game's): Simple_Burst with eleven values set by slot name (setVfxSlot), nothing else
// The game uses v3 (Juice_Glow.shadergraph) and Juice_TyreSmoke.vfx (Editor Juice109Assets copies it into Resources/Juice109). --check: exit 1 when a written file differs from what this would write.
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { createHash } from 'node:crypto';

const ROOT = resolve(import.meta.dir, '../..');
const ver = readFileSync(join(ROOT, 'unity/ProjectSettings/ProjectVersion.txt'), 'utf8').match(/m_EditorVersion: (\S+)/)![1];
const BUILTIN = `/Applications/Unity/Hub/Editor/${ver}/Unity.app/Contents/Resources/PackageManager/BuiltInPackages`;
export const OUT_DIR = join(ROOT, 'unity/Assets/Suborned/Generated/Juice109');

type Obj = Record<string, any> & { m_Type: string; m_ObjectId: string };
/** A .shadergraph file: JSON objects one after another (GraphData first). */
export function parseMulti(text: string): Obj[] {
  const out: Obj[] = []; let i = 0;
  while (i < text.length) {
    while (i < text.length && /\s/.test(text[i]!)) i++;
    if (i >= text.length) break;
    let depth = 0, inStr = false, j = i;
    for (; j < text.length; j++) {
      const c = text[j]!;
      if (inStr) { if (c === '\\') j++; else if (c === '"') inStr = false; continue; }
      if (c === '"') inStr = true; else if (c === '{') depth++; else if (c === '}' && --depth === 0) break;
    }
    out.push(JSON.parse(text.slice(i, j + 1))); i = j + 1;
  }
  return out;
}
export const writeMulti = (objs: Obj[]) => objs.map(o => JSON.stringify(o, null, 4)).join('\n\n') + '\n';
/** A stable 32-hex id from a name, so a rerun writes the same bytes. */
const id = (name: string) => createHash('sha1').update('juice109:' + name).digest('hex').slice(0, 32);
const ref = (x: string) => ({ m_Id: x });
const edge = (fromNode: string, fromSlot: number, toNode: string, toSlot: number) => ({ m_OutputSlot: { m_Node: ref(fromNode), m_SlotId: fromSlot }, m_InputSlot: { m_Node: ref(toNode), m_SlotId: toSlot } });

function find(objs: Obj[], pred: (o: Obj) => boolean, what: string): Obj { const o = objs.find(pred); if (!o) throw new Error(`template: no ${what}`); return o; }
const ofType = (t: string) => (o: Obj) => o.m_Type.endsWith('.' + t);
const block = (name: string) => (o: Obj) => o.m_Type.endsWith('.BlockNode') && o.m_Name === name;

export function shaderGraphs(template: string): Record<'v1' | 'v2' | 'v3', string> {
  // v1: strip HDRP (this project has no HD pipeline), then the authored changes
  let objs = parseMulti(template);
  const hd = new Set(objs.filter(o => o.m_Type.includes('.HighDefinition.')).map(o => o.m_ObjectId));
  objs = objs.filter(o => !hd.has(o.m_ObjectId));
  const graph = objs[0]!; graph.m_ActiveTargets = graph.m_ActiveTargets.filter((r: any) => !hd.has(r.m_Id)); graph.m_Path = 'Suborned/Juice';
  const urp = find(objs, ofType('UniversalTarget'), 'URP target');
  Object.assign(urp, { m_SurfaceType: 1, m_AlphaMode: 2, m_ZWriteControl: 2, m_CastShadows: false, m_ReceiveShadows: false, m_RenderFace: 0 }); // transparent, additive, ZWrite off, both faces
  const tint = find(objs, ofType('ColorShaderProperty'), 'colour property');
  Object.assign(tint, { m_Name: 'Tint', m_RefNameGeneratedByDisplayName: 'Tint', m_DefaultReferenceName: '_Tint', m_OverrideReferenceName: '_Tint', m_ColorMode: 1, m_Value: { r: 1, g: 0.8, b: 0.5, a: 1 } });
  const sample = find(objs, ofType('SampleTexture2DNode'), 'texture sample'), alpha = find(objs, block('SurfaceDescription.Alpha'), 'Alpha block');
  graph.m_Edges.push(edge(sample.m_ObjectId, 7, alpha.m_ObjectId, 0)); // the map's A (slot 7) -> Alpha
  const v1 = writeMulti(objs);

  // v2: one value tuned
  tint.m_Value = { r: 0.4, g: 0.9, b: 1, a: 1 };
  const v2 = writeMulti(objs);

  // v3: two nodes added. Vertex Color, and a Multiply cloned from the template's own (its three slots cloned with new ids), placed between the
  // first Multiply and Base Color
  const mul = find(objs, ofType('MultiplyNode'), 'multiply'), base = find(objs, block('SurfaceDescription.BaseColor'), 'Base Color block');
  const mul2Id = id('mul2'), vcId = id('vertexcolor'), vcSlot = id('vertexcolor.out');
  const mulSlots = mul.m_Slots.map((r: any) => find(objs, o => o.m_ObjectId === r.m_Id, 'multiply slot'));
  const mul2Slots = mulSlots.map((s: Obj, k: number) => ({ ...structuredClone(s), m_ObjectId: id('mul2.slot' + k) }));
  const mul2: Obj = { ...structuredClone(mul), m_ObjectId: mul2Id, m_Slots: mul2Slots.map((s: Obj) => ref(s.m_ObjectId)) };
  mul2.m_DrawState.m_Position.x = mul.m_DrawState.m_Position.x + 110; mul2.m_DrawState.m_Position.y = mul.m_DrawState.m_Position.y - 140;
  const vc: Obj = {
    m_SGVersion: 0, m_Type: 'UnityEditor.ShaderGraph.VertexColorNode', m_ObjectId: vcId, m_Group: ref(''), m_Name: 'Vertex Color',
    m_DrawState: { m_Expanded: true, m_Position: { serializedVersion: '2', x: mul.m_DrawState.m_Position.x - 160, y: mul.m_DrawState.m_Position.y - 160, width: 120, height: 100 } },
    m_Slots: [ref(vcSlot)], synonyms: [], m_Precision: 0, m_PreviewExpanded: false, m_DismissedVersion: 0, m_PreviewMode: 0, m_CustomColors: { m_SerializableColors: [] },
  };
  const vcOut: Obj = {
    m_SGVersion: 0, m_Type: 'UnityEditor.ShaderGraph.Vector4MaterialSlot', m_ObjectId: vcSlot, m_Id: 0, m_DisplayName: 'Out', m_SlotType: 1, m_Hidden: false,
    m_ShaderOutputName: 'Out', m_StageCapability: 3, m_Value: { x: 0, y: 0, z: 0, w: 0 }, m_DefaultValue: { x: 0, y: 0, z: 0, w: 0 }, m_Labels: [],
  };
  graph.m_Nodes.push(ref(mul2Id), ref(vcId));
  graph.m_Edges = graph.m_Edges.filter((e: any) => !(e.m_OutputSlot.m_Node.m_Id === mul.m_ObjectId && e.m_InputSlot.m_Node.m_Id === base.m_ObjectId));
  graph.m_Edges.push(edge(mul.m_ObjectId, 2, mul2Id, 0), edge(vcId, 0, mul2Id, 1), edge(mul2Id, 2, base.m_ObjectId, 0));
  tint.m_Value = { r: 1, g: 1, b: 1, a: 1 }; // the game's glow: the particle's own colour carries it
  objs.push(mul2, ...mul2Slots, vc, vcOut);
  return { v1, v2, v3: writeMulti(objs) };
}

export function vfxGraphs(template: string): Record<'v1' | 'v2', string> {
  const v1 = template;
  // the burst count is the template's only `m_SerializableObject: 128`; the colour gradient its only colorKeys block
  const count = /(m_SerializableType: System\.Single[^\n]*\n\s*m_SerializableObject: )128\n/;
  const grad = /m_SerializableObject: '\{"colorKeys":\[[^']*\}'/;
  if (!count.test(v1) || !grad.test(v1)) throw new Error('vfx template: burst count or gradient not where the test expects it');
  const g = { colorKeys: [{ color: { r: 0.93, g: 0.89, b: 0.8, a: 1 }, time: 0 }, { color: { r: 0.78, g: 0.75, b: 0.7, a: 1 }, time: 1 }], alphaKeys: [{ alpha: 0.6, time: 0 }, { alpha: 0.35, time: 0.5 }, { alpha: 0, time: 1 }], gradientMode: 0 };
  const v2 = v1.replace(count, (_m, a) => a + '24\n').replace(grad, `m_SerializableObject: '${JSON.stringify(g)}'`);
  return { v1, v2 };
}

/** A .vfx slot's value by its property name (the one slot of that name holding a value), replaced as text: the YAML document that holds
 * `m_Property: name: <name>` and a non-empty `m_SerializableObject`. Throws unless exactly one matches, so a template change can't move a value silently. */
export function setVfxSlot(text: string, name: string, value: string): string {
  const docs = text.split('\n--- !u!');
  const hits = docs.map((d, i) => ({ d, i })).filter(({ d }) => new RegExp(`m_Property:\\s*\\n\\s*name: ${name}\\n`).test(d) && /m_SerializableObject: \S/.test(d));
  if (hits.length !== 1) throw new Error(`vfx slot ${name}: ${hits.length} slots hold a value`);
  docs[hits[0]!.i] = hits[0]!.d.replace(/m_SerializableObject: .*/, `m_SerializableObject: ${value}`);
  return docs.join('\n--- !u!');
}
const curve = (frames: [number, number][]) => `'${JSON.stringify({ frames: frames.map(([time, v]) => ({ time, value: v, inTangent: 0, outTangent: 0, tangentMode: 0, leftTangentMode: 0, rightTangentMode: 0, broken: false })), preWrapMode: 8, postWrapMode: 8, version: 1 })}'`;
/** The getaway's tyre smoke (brief 109 moment 3), from Simple_Burst by named values only: a small soft burst per OnPlay that rises, drifts and
 * grows, cream to warm grey. The host sends OnPlay every 0.09 s at each rear wheel while the car pulls away. */
export function tyreSmoke(template: string): string {
  let t = template;
  const set = (n: string, v: string) => { t = setVfxSlot(t, n, v); };
  set('Count', '8'); set('A', '0.9'); set('B', '1.5'); // the burst's size; lifetime between A and B (s)
  set('MinSpeed', '0.25'); set('MaxSpeed', '1.0'); set('dragCoefficient', '2.4');
  set('Force', `'${JSON.stringify({ vector: { x: 0.25, y: 0.55, z: 0.1 } })}'`); // a rise and the street's drift, not gravity
  set('Size', curve([[0, 1.4], [0.35, 3.6], [1, 6.8]])); // the template's size is a multiplier on a small base: 0.35-1.7 drew specks
  set('Color', `'${JSON.stringify({ colorKeys: [{ color: { r: 0.95, g: 0.93, b: 0.88, a: 1 }, time: 0 }, { color: { r: 0.8, g: 0.78, b: 0.75, a: 1 }, time: 1 }], alphaKeys: [{ alpha: 0, time: 0 }, { alpha: 0.7, time: 0.1 }, { alpha: 0.45, time: 0.5 }, { alpha: 0, time: 1 }], gradientMode: 0 })}'`);
  set('arcSphere', `'${JSON.stringify({ sphere: { transform: { position: { x: 0, y: 0, z: 0 }, angles: { x: 0, y: 0, z: 0 }, scale: { x: 1, y: 1, z: 1 } }, radius: 0.22 }, arc: 6.28 })}'`);
  set('bounds', `'${JSON.stringify({ center: { x: 0, y: 1, z: 0 }, size: { x: 10, y: 6, z: 10 } })}'`);
  return t.replace(/m_Name: Simple_Burst\n/, 'm_Name: Juice_TyreSmoke\n');
}

if (import.meta.main) {
  const check = process.argv.includes('--check');
  const sg = shaderGraphs(readFileSync(join(BUILTIN, 'com.unity.shadergraph/GraphTemplates/Cross Pipeline/Unlit Simple.shadergraph'), 'utf8'));
  const vfx = vfxGraphs(readFileSync(join(BUILTIN, 'com.unity.visualeffectgraph/Editor/Templates/Simple_Burst.vfx'), 'utf8'));
  const files: Record<string, string> = {
    'Test/Juice_Glow_v1.shadergraph': sg.v1, 'Test/Juice_Glow_v2.shadergraph': sg.v2, 'Test/Juice_Glow_v3.shadergraph': sg.v3, 'Juice_Glow.shadergraph': sg.v3,
    'Test/Juice_Puff_v1.vfx': vfx.v1, 'Test/Juice_Puff_v2.vfx': vfx.v2,
    'Juice_TyreSmoke.vfx': tyreSmoke(vfx.v1),
  };
  let bad = 0;
  for (const [rel, text] of Object.entries(files)) {
    const p = join(OUT_DIR, rel);
    if (check) { if (!existsSync(p) || readFileSync(p, 'utf8') !== text) { console.log(`differs: ${rel}`); bad++; } continue; }
    mkdirSync(join(p, '..'), { recursive: true }); if (!existsSync(p) || readFileSync(p, 'utf8') !== text) { writeFileSync(p, text); console.log(`wrote ${rel} (${text.length} bytes)`); }
  }
  if (check) { console.log(bad ? `${bad} differ` : 'juice graphs up to date'); process.exit(bad ? 1 : 0); }
}
