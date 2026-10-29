// Brief 077 (Latch): the testbed's leaves and petals (three.js MeshStandardMaterial with vertexColors and side: DoubleSide) in DecoKit/Lit Cutaway's
// Meta pass. DK_LitCutawayLeaf.shader (DecoKit/Lit Cutaway Leaf, the sibling of DK_LitCutaway.shader) includes this file (with its pragmas) where DK_LitCutaway.shader includes URP's LitMetaPass.hlsl.
// Opt-in material keywords (shader_feature_local, so only the combinations a material uses are ever compiled):
//   _DK_VERTEX_COLOR  albedo x the vertex colour's rgb (three's linear multipliers: the leaf tints, root-to-tip shade, dark stems)
// With none of them this file is URP's LitMetaPass.hlsl, included unchanged in the #if branch below, so the sibling compiles the same code as DK_LitCutaway.
// With one, the #else branch is URP 17.6.0's LitMetaPass.hlsl (sha256 b597eede0567936de9eb2b72fe5225ce581c70b92b05c349cc2222e3210d1c9b) with the edits marked "DK leaf".
// Written by briefs/077/shader/make_leaf_hlsl.py from the package file; to follow a URP upgrade, re-run it on the new file.
#pragma shader_feature_local _DK_VERTEX_COLOR
#if !defined(_DK_VERTEX_COLOR)
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"
#else
#include "DK_Leaf.hlsl"
#ifndef UNIVERSAL_LIT_META_PASS_INCLUDED
#define UNIVERSAL_LIT_META_PASS_INCLUDED

// DK leaf: URP's UniversalMetaPass.hlsl (sha256 a95a0a8e5edfe1c27235f421711bc3025cb0ab7bacd9676b88851bb403b68cc1), inlined with its edits
#ifndef UNIVERSAL_META_PASS_INCLUDED
#define UNIVERSAL_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv0          : TEXCOORD0;
    float2 uv1          : TEXCOORD1;
    float2 uv2          : TEXCOORD2;
#if defined(_DK_VERTEX_COLOR)
    half4 color         : COLOR; // DK leaf
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
#if defined(_DK_VERTEX_COLOR)
    half4 color         : COLOR; // DK leaf
#endif
#ifdef EDITOR_VISUALIZATION
    float2 VizUV        : TEXCOORD1;
    float4 LightCoord   : TEXCOORD2;
#endif
};

Varyings UniversalVertexMeta(Attributes input)
{
    Varyings output = (Varyings)0;
    output.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.uv1, input.uv2);
    output.uv = TRANSFORM_TEX(input.uv0, _BaseMap);
#if defined(_DK_VERTEX_COLOR)
    output.color = input.color; // DK leaf
#endif
#ifdef EDITOR_VISUALIZATION
    UnityEditorVizData(input.positionOS.xyz, input.uv0, input.uv1, input.uv2, output.VizUV, output.LightCoord);
#endif
    return output;
}

half4 UniversalFragmentMeta(Varyings fragIn, MetaInput metaInput)
{
#ifdef EDITOR_VISUALIZATION
    metaInput.VizUV = fragIn.VizUV;
    metaInput.LightCoord = fragIn.LightCoord;
#endif

    return UnityMetaFragment(metaInput);
}
#endif


half4 UniversalFragmentMetaLit(Varyings input) : SV_Target
{
    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);
#if defined(_DK_VERTEX_COLOR)
    surfaceData.albedo *= input.color.rgb; // DK leaf: three multiplies the diffuse colour by the vertex colour (linear)
#endif

    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

    MetaInput metaInput;
    metaInput.Albedo = brdfData.diffuse + brdfData.specular * brdfData.roughness * 0.5;
    metaInput.Emission = surfaceData.emission;
    return UniversalFragmentMeta(input, metaInput);
}
#endif

#endif
