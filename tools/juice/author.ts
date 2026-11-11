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
//   Juice_TiltShift.shadergraph (moment 8, -tiltshift): Fullscreen Basic URP with a Custom Function (JuiceTiltShift.hlsl) and two properties
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

/** Brief 109 moment 8, the miniature look trial (its own switch, -tiltshift): a URP Fullscreen Shader Graph from "Fullscreen Basic URP", its
 * colour chain (sample buffer, saturation, contrast, tint) replaced by one Custom Function node from a file (JuiceTiltShift.hlsl beside it: the blit
 * source needs a file-scope declaration a string body can't make) fed by the screen position and two properties, _TiltBand (the sharp band's half
 * height, 0..0.5 of the screen) and _TiltBlur (the blur at the screen's top and bottom edges, px at 1080). Screen space, not depth: the play camera is
 * orthographic, so its depth says little about "near" and "far". */
export const TILT_HLSL_GUID = id('tiltshift.hlsl');
export const TILT_HLSL = `// Brief 109 moment 8 (tools/juice/author.ts writes this): the tilt-shift band blur for Juice_TiltShift.shadergraph (Custom Function, file source).
// Sixteen taps on a golden-angle spiral, their radius growing from 0 at the sharp band's edge to Blur px at the screen's top and bottom.
#ifndef JUICE_TILTSHIFT_INCLUDED
#define JUICE_TILTSHIFT_INCLUDED
#ifndef SHADERGRAPH_PREVIEW
TEXTURE2D_X(_BlitTexture);
#endif
void TiltShift_float(float4 UV, float Band, float Blur, out float3 Out)
{
#ifdef SHADERGRAPH_PREVIEW
    Out = float3(UV.xy, 0);
#else
    float2 uv = UV.xy; float d = abs(uv.y - 0.5);
    float k = saturate((d - Band) / max(1e-4, 0.5 - Band)); k = k * k * (3 - 2 * k);
    float r = Blur * k * (_ScreenSize.y / 1080.0);
    float2 px = uv * _ScreenSize.xy; float3 acc = 0; float wsum = 0;
    [unroll] for (int i = 0; i < 16; i++)
    {
        float t = (i + 0.5) / 16.0, a = i * 2.39996;
        float2 o = float2(cos(a), sin(a)) * sqrt(t) * r; float w = 1.0 - 0.5 * t;
        uint2 q = (uint2)clamp(px + o, float2(0, 0), _ScreenSize.xy - 1);
        acc += LOAD_TEXTURE2D_X_LOD(_BlitTexture, q, 0).rgb * w; wsum += w;
    }
    Out = acc / wsum;
#endif
}
#endif
`;
export const TILT_HLSL_META = `fileFormatVersion: 2\nguid: ${TILT_HLSL_GUID}\nShaderIncludeImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`;
export function tiltShift(template: string): string {
  const objs = parseMulti(template); const graph = objs[0]!; graph.m_Path = 'Suborned/Juice';
  const base = find(objs, block('SurfaceDescription.BaseColor'), 'Base Color block'), pos = find(objs, ofType('ScreenPositionNode'), 'screen position');
  // drop the colour chain and its properties; keep the blocks, the targets and the screen position
  const drop = new Set(objs.filter(o => /SampleBufferNode|SaturationNode|ContrastNode|MultiplyNode|PropertyNode|ShaderProperty|CategoryData/.test(o.m_Type)).flatMap(o => [o.m_ObjectId, ...(o.m_Slots ?? []).map((r: any) => r.m_Id)]));
  let out = objs.filter(o => !drop.has(o.m_ObjectId));
  graph.m_Nodes = graph.m_Nodes.filter((r: any) => !drop.has(r.m_Id)); graph.m_Properties = []; graph.m_CategoryData = [];
  graph.m_Edges = graph.m_Edges.filter((e: any) => !drop.has(e.m_OutputSlot.m_Node.m_Id) && !drop.has(e.m_InputSlot.m_Node.m_Id));
  const slot = (key: string, type: string, sid: number, name: string, dir: 0 | 1, value: any) => ({ m_SGVersion: 0, m_Type: `UnityEditor.ShaderGraph.${type}`, m_ObjectId: id(key), m_Id: sid, m_DisplayName: name, m_SlotType: dir, m_Hidden: false, m_ShaderOutputName: name, m_StageCapability: 3, m_Value: value, m_DefaultValue: value, m_Labels: [] } as Obj);
  const prop = (key: string, name: string, ref: string, v: number, lo: number, hi: number) => ({
    m_SGVersion: 1, m_Type: 'UnityEditor.ShaderGraph.Internal.Vector1ShaderProperty', m_ObjectId: id(key), m_Guid: { m_GuidSerialized: [8, 4, 4, 4, 12].reduce((a, n, i, all) => { const off = all.slice(0, i).reduce((x, y) => x + y, 0); return a + (i ? '-' : '') + id(key + '.guid').slice(off, off + n); }, '') },
    m_Name: name, m_DefaultRefNameVersion: 1, m_RefNameGeneratedByDisplayName: name, m_DefaultReferenceName: ref, m_OverrideReferenceName: ref, m_GeneratePropertyBlock: true, m_UseCustomSlotLabel: false, m_CustomSlotLabel: '', m_DismissedVersion: 0, m_Precision: 0,
    overrideHLSLDeclaration: false, hlslDeclarationOverride: 0, m_Hidden: false, m_PerRendererData: false, m_customAttributes: [], m_Value: v, m_FloatType: 0, m_RangeValues: { x: lo, y: hi }, m_SliderType: 0, m_SliderPower: 3.0, m_EnumType: 0, m_CSharpEnumString: '', m_EnumNames: ['Default'], m_EnumValues: [0],
  } as Obj);
  const node = (key: string, type: string, name: string, x: number, y: number, slots: Obj[], extra: Record<string, any>) => ({
    m_SGVersion: type.endsWith('CustomFunctionNode') ? 1 : 0, m_Type: `UnityEditor.ShaderGraph.${type}`, m_ObjectId: id(key), m_Group: ref(''), m_Name: name,
    m_DrawState: { m_Expanded: true, m_Position: { serializedVersion: '2', x, y, width: 200, height: 120 } }, m_Slots: slots.map(s => ref(s.m_ObjectId)), synonyms: [], m_Precision: 0, m_PreviewExpanded: false, m_DismissedVersion: 0, m_PreviewMode: 0, m_CustomColors: { m_SerializableColors: [] }, ...extra,
  } as Obj);
  const pBand = prop('tilt.band', 'Tilt Band', '_TiltBand', 0.16, 0, 0.5), pBlur = prop('tilt.blur', 'Tilt Blur', '_TiltBlur', 7, 0, 24);
  const sBand = slot('tilt.band.out', 'Vector1MaterialSlot', 0, 'Out', 1, 0), sBlur = slot('tilt.blur.out', 'Vector1MaterialSlot', 0, 'Out', 1, 0);
  const nBand = node('tilt.band.node', 'PropertyNode', 'Property', -620, 120, [sBand], { m_Property: ref(pBand.m_ObjectId) });
  const nBlur = node('tilt.blur.node', 'PropertyNode', 'Property', -620, 180, [sBlur], { m_Property: ref(pBlur.m_ObjectId) });
  const fUV = slot('tilt.fn.uv', 'Vector4MaterialSlot', 0, 'UV', 0, { x: 0, y: 0, z: 0, w: 0 }), fBand = slot('tilt.fn.band', 'Vector1MaterialSlot', 1, 'Band', 0, 0.16);
  const fBlur = slot('tilt.fn.blur', 'Vector1MaterialSlot', 2, 'Blur', 0, 7), fOut = slot('tilt.fn.out', 'Vector3MaterialSlot', 3, 'Out', 1, { x: 0, y: 0, z: 0 });
  const fn = node('tilt.fn', 'CustomFunctionNode', 'TiltShift (Custom Function)', -300, 0, [fUV, fBand, fBlur, fOut], { m_SourceType: 0, m_FunctionName: 'TiltShift', m_FunctionSource: TILT_HLSL_GUID, m_FunctionSourceUsePragmas: true, m_FunctionBody: '' });
  graph.m_Properties = [ref(pBand.m_ObjectId), ref(pBlur.m_ObjectId)];
  graph.m_CategoryData = [ref(id('tilt.cat'))];
  graph.m_Nodes.push(ref(nBand.m_ObjectId), ref(nBlur.m_ObjectId), ref(fn.m_ObjectId));
  graph.m_Edges.push(edge(pos.m_ObjectId, 0, fn.m_ObjectId, 0), edge(nBand.m_ObjectId, 0, fn.m_ObjectId, 1), edge(nBlur.m_ObjectId, 0, fn.m_ObjectId, 2), edge(fn.m_ObjectId, 3, base.m_ObjectId, 0));
  out.push(pBand, pBlur, sBand, sBlur, nBand, nBlur, fUV, fBand, fBlur, fOut, fn, { m_SGVersion: 0, m_Type: 'UnityEditor.ShaderGraph.CategoryData', m_ObjectId: id('tilt.cat'), m_Name: '', m_ChildObjectList: [ref(pBand.m_ObjectId), ref(pBlur.m_ObjectId)] } as Obj);
  return writeMulti(out);
}

if (import.meta.main) {
  const check = process.argv.includes('--check');
  const sg = shaderGraphs(readFileSync(join(BUILTIN, 'com.unity.shadergraph/GraphTemplates/Cross Pipeline/Unlit Simple.shadergraph'), 'utf8'));
  const vfx = vfxGraphs(readFileSync(join(BUILTIN, 'com.unity.visualeffectgraph/Editor/Templates/Simple_Burst.vfx'), 'utf8'));
  const files: Record<string, string> = {
    'Test/Juice_Glow_v1.shadergraph': sg.v1, 'Test/Juice_Glow_v2.shadergraph': sg.v2, 'Test/Juice_Glow_v3.shadergraph': sg.v3, 'Juice_Glow.shadergraph': sg.v3,
    'Test/Juice_Puff_v1.vfx': vfx.v1, 'Test/Juice_Puff_v2.vfx': vfx.v2,
    'Juice_TyreSmoke.vfx': tyreSmoke(vfx.v1),
    'Juice_TiltShift.shadergraph': tiltShift(readFileSync(join(BUILTIN, 'com.unity.render-pipelines.universal/Editor/ShaderGraph/GraphTemplates/Fullscreen Basic URP.shadergraph'), 'utf8')),
    'JuiceTiltShift.hlsl': TILT_HLSL, 'JuiceTiltShift.hlsl.meta': TILT_HLSL_META,
  };
  let bad = 0;
  for (const [rel, text] of Object.entries(files)) {
    const p = join(OUT_DIR, rel);
    if (check) { if (!existsSync(p) || readFileSync(p, 'utf8') !== text) { console.log(`differs: ${rel}`); bad++; } continue; }
    mkdirSync(join(p, '..'), { recursive: true }); if (!existsSync(p) || readFileSync(p, 'utf8') !== text) { writeFileSync(p, text); console.log(`wrote ${rel} (${text.length} bytes)`); }
  }
  if (check) { console.log(bad ? `${bad} differ` : 'juice graphs up to date'); process.exit(bad ? 1 : 0); }
}
