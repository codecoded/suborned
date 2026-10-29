"""Brief 077 (Latch): writes the DK leaf pass includes from URP 17.6.0's own pass files, each the stock file plus asserted, marked edits."""
import sys, hashlib
from pathlib import Path
URP = Path(sys.argv[1]); OUT = Path(sys.argv[2])
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def edit(src, pairs):
    for a, b in pairs:
        n = src.count(a)
        assert n == 1, (a[:80], n)
        src = src.replace(a, b)
    return src

HEAD = """// Brief 077 (Latch): the testbed's leaves and petals (three.js MeshStandardMaterial with vertexColors and side: DoubleSide) in DecoKit/Lit Cutaway's
// {passname} pass. DK_LitCutawayLeaf.shader (DecoKit/Lit Cutaway Leaf, the sibling of DK_LitCutaway.shader) includes this file (with its pragmas) where DK_LitCutaway.shader includes URP's {stock}.
// Opt-in material keywords (shader_feature_local, so only the combinations a material uses are ever compiled):
{keys}// With none of them this file is URP's {stock}, included unchanged in the #if branch below, so the sibling compiles the same code as DK_LitCutaway.
// With one, the #else branch is URP 17.6.0's {stock} (sha256 {sha}) with the edits marked "DK leaf".
// Written by briefs/077/shader/make_leaf_hlsl.py from the package file; to follow a URP upgrade, re-run it on the new file.
"""
KEY_VC = "//   _DK_VERTEX_COLOR  albedo x the vertex colour's rgb (three's linear multipliers: the leaf tints, root-to-tip shade, dark stems)\n"
KEY_TS = "//   _DK_TWO_SIDED     a back face lights from its own side: the world normal and the whole tangent frame flip on it (three's faceDirection)\n"

FACE = """    // DK leaf: _DK_TWO_SIDED, a back face lights from its own side (three.js DoubleSide: the normal, tangent and bitangent x faceDirection)
#if defined(_DK_TWO_SIDED)
    half dkFace = DK_LEAF_FACE_SIGN(input);
    input.normalWS *= dkFace;
    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    input.tangentWS *= dkFace; // xyz and the sign: the bitangent (sign x cross(normal, tangent)) flips with them
    #endif
#endif
"""
VC_ATTR = """#if defined(_DK_VERTEX_COLOR)
    half4 color         : COLOR; // DK leaf
#endif
"""
VC_VARY = """#if defined(_DK_VERTEX_COLOR)
    half4 color                     : COLOR; // DK leaf
#endif
"""
FACE_FIELD = "    DK_LEAF_FACE_FIELD // DK leaf: _DK_TWO_SIDED, the fragment's facing (fragment stage only; last, as Shader Graph's cullFace)\n"
VC_ALBEDO = """    InitializeStandardLitSurfaceData(input.uv, surfaceData);
#if defined(_DK_VERTEX_COLOR)
    surfaceData.albedo *= input.color.rgb; // DK leaf: three multiplies the diffuse colour by the vertex colour (linear)
#endif
"""

def wrap(name, pass_, stock_rel, keys, pragmas, body):
    stock = URP / stock_rel
    cond = " && ".join(f"!defined({k})" for k in keys)
    txt = HEAD.format(passname=pass_, stock=Path(stock_rel).name, keys="".join({"_DK_VERTEX_COLOR": KEY_VC, "_DK_TWO_SIDED": KEY_TS}[k] for k in keys), sha=sha(stock))
    txt += "".join(f"#pragma shader_feature_local {k}\n" for k in keys)
    txt += f"#if {cond}\n#include \"Packages/com.unity.render-pipelines.universal/{stock_rel}\"\n#else\n#include \"DK_Leaf.hlsl\"\n"
    txt += body + "\n#endif\n"
    (OUT / name).write_text(txt)
    print(name, len(txt.splitlines()), "lines")

# forward
src = (URP / "Shaders/LitForwardPass.hlsl").read_text()
src = edit(src, [
    ('#include "LitInput.hlsl"', '#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"'),
    ("    float2 dynamicLightmapUV  : TEXCOORD2;\n    UNITY_VERTEX_INPUT_INSTANCE_ID\n};", "    float2 dynamicLightmapUV  : TEXCOORD2;\n" + VC_ATTR + "    UNITY_VERTEX_INPUT_INSTANCE_ID\n};"),
    ("    float4 positionCS               : SV_POSITION;\n\n    UNITY_VERTEX_INPUT_INSTANCE_ID", VC_VARY + "    float4 positionCS               : SV_POSITION;\n\n    UNITY_VERTEX_INPUT_INSTANCE_ID"),
    ("    UNITY_VERTEX_INPUT_INSTANCE_ID\n    UNITY_VERTEX_OUTPUT_STEREO\n};\n\nvoid InitializeInputData", "    UNITY_VERTEX_INPUT_INSTANCE_ID\n    UNITY_VERTEX_OUTPUT_STEREO\n" + FACE_FIELD + "};\n\nvoid InitializeInputData"),
    ("    output.positionCS = vertexInput.positionCS;\n\n    return output;", "#if defined(_DK_VERTEX_COLOR)\n    output.color = input.color; // DK leaf\n#endif\n    output.positionCS = vertexInput.positionCS;\n\n    return output;"),
    ("    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);\n\n#if defined(_PARALLAXMAP)", "    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);\n" + FACE + "\n#if defined(_PARALLAXMAP)"),
    ("    InitializeStandardLitSurfaceData(input.uv, surfaceData);\n", VC_ALBEDO),
])
wrap("DK_LeafForward.hlsl", "ForwardLit", "Shaders/LitForwardPass.hlsl", ["_DK_VERTEX_COLOR", "_DK_TWO_SIDED"], None, src)

# gbuffer
src = (URP / "Shaders/LitGBufferPass.hlsl").read_text()
src = edit(src, [
    ("    float2 dynamicLightmapUV  : TEXCOORD2;\n    UNITY_VERTEX_INPUT_INSTANCE_ID\n};", "    float2 dynamicLightmapUV  : TEXCOORD2;\n" + VC_ATTR + "    UNITY_VERTEX_INPUT_INSTANCE_ID\n};"),
    ("    float4 positionCS               : SV_POSITION;\n    UNITY_VERTEX_INPUT_INSTANCE_ID", VC_VARY + "    float4 positionCS               : SV_POSITION;\n    UNITY_VERTEX_INPUT_INSTANCE_ID"),
    ("    UNITY_VERTEX_INPUT_INSTANCE_ID\n    UNITY_VERTEX_OUTPUT_STEREO\n};\n\nvoid InitializeInputData", "    UNITY_VERTEX_INPUT_INSTANCE_ID\n    UNITY_VERTEX_OUTPUT_STEREO\n" + FACE_FIELD + "};\n\nvoid InitializeInputData"),
    ("    output.positionCS = vertexInput.positionCS;\n\n    return output;", "#if defined(_DK_VERTEX_COLOR)\n    output.color = input.color; // DK leaf\n#endif\n    output.positionCS = vertexInput.positionCS;\n\n    return output;"),
    ("    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);\n\n#if defined(_PARALLAXMAP)", "    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);\n" + FACE + "\n#if defined(_PARALLAXMAP)"),
    ("    InitializeStandardLitSurfaceData(input.uv, surfaceData);\n", VC_ALBEDO),
])
wrap("DK_LeafGBuffer.hlsl", "GBuffer", "Shaders/LitGBufferPass.hlsl", ["_DK_VERTEX_COLOR", "_DK_TWO_SIDED"], None, src)

# depth normals: the facing only
src = (URP / "Shaders/LitDepthNormalsPass.hlsl").read_text()
src = edit(src, [
    ("    UNITY_VERTEX_INPUT_INSTANCE_ID\n    UNITY_VERTEX_OUTPUT_STEREO\n};\n\n\nVaryings DepthNormalsVertex", "    UNITY_VERTEX_INPUT_INSTANCE_ID\n    UNITY_VERTEX_OUTPUT_STEREO\n" + FACE_FIELD + "};\n\n\nVaryings DepthNormalsVertex"),
    ("    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);\n\n    #if defined(_PARALLAXMAP)", "    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);\n" + FACE + "\n    #if defined(_PARALLAXMAP)"),
])
wrap("DK_LeafDepthNormals.hlsl", "DepthNormals", "Shaders/LitDepthNormalsPass.hlsl", ["_DK_TWO_SIDED"], None, src)

# meta: LitMetaPass.hlsl with UniversalMetaPass.hlsl inlined, the colour only
meta = (URP / "ShaderLibrary/UniversalMetaPass.hlsl").read_text()
meta = edit(meta, [
    ("    float2 uv2          : TEXCOORD2;\n    UNITY_VERTEX_INPUT_INSTANCE_ID", "    float2 uv2          : TEXCOORD2;\n" + VC_ATTR + "    UNITY_VERTEX_INPUT_INSTANCE_ID"),
    ("    float2 uv           : TEXCOORD0;\n#ifdef EDITOR_VISUALIZATION", "    float2 uv           : TEXCOORD0;\n#if defined(_DK_VERTEX_COLOR)\n    half4 color         : COLOR; // DK leaf\n#endif\n#ifdef EDITOR_VISUALIZATION"),
    ("    output.uv = TRANSFORM_TEX(input.uv0, _BaseMap);\n", "    output.uv = TRANSFORM_TEX(input.uv0, _BaseMap);\n#if defined(_DK_VERTEX_COLOR)\n    output.color = input.color; // DK leaf\n#endif\n"),
])
lit = (URP / "Shaders/LitMetaPass.hlsl").read_text()
lit = edit(lit, [
    ('#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"\n', "// DK leaf: URP's UniversalMetaPass.hlsl (sha256 " + sha(URP / "ShaderLibrary/UniversalMetaPass.hlsl") + "), inlined with its edits\n" + meta + "\n"),
    ("    InitializeStandardLitSurfaceData(input.uv, surfaceData);\n", VC_ALBEDO),
])
wrap("DK_LeafMeta.hlsl", "Meta", "Shaders/LitMetaPass.hlsl", ["_DK_VERTEX_COLOR"], None, lit)

# the sibling shader: DK_LitCutaway.shader as it is, renamed, with the four pass includes pointed at the leaf includes. DK_LitCutaway.shader itself is
# never edited (brief 077 proof: two new local keywords in it changed its compiled ShadowCaster and DepthOnly _DK_CUT programs and the towers' shadows
# in HarbourDistrict, passes the edit never touched), so no existing material can change.
lit = OUT / "DK_LitCutaway.shader"
src = lit.read_text()
src = edit(src, [
    ('// Generated by Tools/Unity/make_cutaway_shaders.py from URP\'s Universal Render Pipeline/Lit: do not edit\nShader "DecoKit/Lit Cutaway"\n',
     "// Generated by briefs/077/shader/make_leaf_hlsl.py from DK_LitCutaway.shader (sha256 " + sha(lit) + "): do not edit; re-run it when that shader changes.\n"
     "// Brief 077 (Latch): DecoKit/Lit Cutaway for the testbed's leaves and petals. Identical to it but for the four pass includes below, which add two\n"
     "// opt-in keywords (_DK_VERTEX_COLOR, _DK_TWO_SIDED; see DK_LeafForward.hlsl). Same properties and UnityPerMaterial, so SRP Batcher compatible.\n"
     'Shader "DecoKit/Lit Cutaway Leaf"\n'),
    ('            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"\n',
     '            #include_with_pragmas "DK_LeafForward.hlsl" // brief 077: URP\'s LitForwardPass.hlsl, or with _DK_VERTEX_COLOR / _DK_TWO_SIDED its leaf copy\n'),
    ('            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitGBufferPass.hlsl"\n',
     '            #include_with_pragmas "DK_LeafGBuffer.hlsl" // brief 077: URP\'s LitGBufferPass.hlsl, or with _DK_VERTEX_COLOR / _DK_TWO_SIDED its leaf copy\n'),
    ('            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"\n',
     '            #include_with_pragmas "DK_LeafDepthNormals.hlsl" // brief 077: URP\'s LitDepthNormalsPass.hlsl, or with _DK_TWO_SIDED its leaf copy\n'),
    ('            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"\n',
     '            #include_with_pragmas "DK_LeafMeta.hlsl" // brief 077: URP\'s LitMetaPass.hlsl, or with _DK_VERTEX_COLOR its leaf copy\n'),
])
(OUT / "DK_LitCutawayLeaf.shader").write_text(src)
print("DK_LitCutawayLeaf.shader", len(src.splitlines()), "lines")
