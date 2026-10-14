// The play camera's architecture fade (GDD §10, §15: "tagged architecture ghosts to 0.2 + ghost shadow"). Suborned.ArchitectureFade
// sets the globals while the play camera renders; the builder's FadeTags marks what may fade (rendering-layer bit DK_CUT_LAYER).
// Tagged architecture standing above the floor, on the camera's side of the focus, and hiding the ground within a disc round the
// focus thins to a dithered ghost, so towers and walls never bury the play space yet still stand in the view. A fragment hides
// the ground point its view ray lands on: in "band" mode it thins when that point is in the disc (only the slice that covers
// the disc); in "column" mode also when anything below it on its vertical does (the crown goes with the slice, nothing floats
// solid over a ghost). What stands more than `back` metres beyond the focus along the view stays whole: it hides only ground
// further back, and its near face is what the player reads (doors, shopfronts, signs).
// With shadows on, the ghost casts a ghost shadow (the same cut in the shadow map, keeping `ghost shadow` rather than `ghost`:
// Glint, 008 B5, 0.4 against the ghost's 0.2), so a cut shell's inside and the street behind a tower light up instead of
// reading as a dark pit under a roof that is no longer there, yet a faded tower still casts a lighter shadow.
// Ported from the-suborned (brief 006 C; DKCutaway there); the `back` limit, the controller and _DK_CUT are brief 008 B4.
// _DK_CUT (a multi_compile in each pass) is the variant that cuts. Without it the passes are URP Lit's own with no discard in
// them: a shader that may discard loses the GPU's hidden-surface removal on every pixel it covers, cut or not, so only what the
// cut can reach draws with it. In Play Mode Suborned.FadeReach gives those renderers a twin material with the keyword; where
// there is no reach (Edit Mode renders) ArchitectureFade sets the keyword for the whole of its camera's render instead.
// Included by DK_LitCutaway / DK_ComplexLitCutaway (Tools/Unity/make_cutaway_shaders.py) and Suborned/FacadeLit once per pass,
// after the pass's own include, with DK_CUT_FORWARD, DK_CUT_DEPTH, DK_CUT_DEPTH_NORMALS or DK_CUT_SHADOW defined.
#ifndef DK_CUTAWAY_INCLUDED
#define DK_CUTAWAY_INCLUDED

#define DK_CUT_LAYER (1u << 20)  // Suborned.ArchitectureFade.Layer

float4 _DKCutFocus;  // xyz the focus on the ground (world); w the disc's radius (m), 0 = off
float4 _DKCutView;   // xyz the camera's forward (world, unit); w the floor (m): nothing lower is cut
float4 _DKCutShape;  // x feather (m); y ghost (what stays inside the disc: 0 = clean away); z column mode (0 / 1); w shadows cut too (0 / 1)
float4 _DKCutBack;   // x how far beyond the focus along the view the cut reaches (m); y its feather (m); z ghost shadow (what stays in the shadow map)

float DKBayer4(float2 px)
{
    const float m[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
    uint2 p = uint2(px) & 3u;
    return (m[p.y * 4u + p.x] + .5) / 16.0;
}

float DKSegmentDistance(float2 c, float2 a, float2 b)
{
    float2 ab = b - a;
    float t = saturate(dot(c - a, ab) / max(dot(ab, ab), 1e-6));
    return length(a + ab * t - c);
}

// How much of the fragment at world position p stays (1 = all of it) when the faded part keeps `ghost`.
float DKCutKeepAt(float3 p, float ghost)
{
    if (p.y <= _DKCutView.w) return 1.0;
    float2 k = _DKCutView.xz / max(-_DKCutView.y, 1e-3);  // ground run of the view ray per metre it drops
    float2 hit = p.xz + k * (p.y - _DKCutFocus.y);
    float d = _DKCutShape.z > .5 ? DKSegmentDistance(_DKCutFocus.xz, p.xz + k * (_DKCutView.w - _DKCutFocus.y), hit)
                                 : length(hit - _DKCutFocus.xz);
    float feather = max(_DKCutShape.x, 1e-3);
    float outside = saturate((d - (_DKCutFocus.w - feather)) / feather);
    float2 ahead = _DKCutView.xz / max(length(_DKCutView.xz), 1e-3);  // the view on the ground
    float beyond = saturate((dot(p.xz - _DKCutFocus.xz, ahead) - _DKCutBack.x) / max(_DKCutBack.y, 1e-3) + 1.0);
    return lerp(ghost, 1.0, max(outside, beyond));
}

float DKCutKeep(float3 p) { return DKCutKeepAt(p, _DKCutShape.y); }

bool DKCutOn() { return _DKCutFocus.w > 0.0 && (GetMeshRenderingLayer() & DK_CUT_LAYER) != 0u; }

void DKCutaway(float4 positionCS)
{
#if defined(_DK_CUT)
    if (!DKCutOn()) return;
    float3 p = ComputeWorldSpacePosition(GetNormalizedScreenSpaceUV(positionCS), positionCS.z, UNITY_MATRIX_I_VP);
    clip(DKCutKeep(p) - DKBayer4(positionCS.xy));
#endif
}

#endif

#if defined(DK_CUT_FORWARD)
void DKLitPassFragment(Varyings input, out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    DKCutaway(input.positionCS);
    LitPassFragment(input, outColor
#ifdef _WRITE_RENDERING_LAYERS
        , outRenderingLayers
#endif
    );
}
#undef DK_CUT_FORWARD
#endif

#if defined(DK_CUT_DEPTH)
half DKDepthOnlyFragment(Varyings input) : SV_TARGET
{
    DKCutaway(input.positionCS);
    return DepthOnlyFragment(input);
}
#undef DK_CUT_DEPTH
#endif

#if defined(DK_CUT_DEPTH_NORMALS)
void DKDepthNormalsFragment(Varyings input, out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    DKCutaway(input.positionCS);
    DepthNormalsFragment(input, outNormalWS
#ifdef _WRITE_RENDERING_LAYERS
        , outRenderingLayers
#endif
    );
}
#undef DK_CUT_DEPTH_NORMALS
#endif

#if defined(DK_CUT_SHADOW)
// URP's shadow Varyings plus the world position (the shadow pass's clip space is the light's, not the camera's)
struct DKShadowVaryings
{
#if defined(_ALPHATEST_ON)
    float2 uv           : TEXCOORD0;
#endif
    float4 positionCS   : SV_POSITION;
    float3 positionWS   : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

DKShadowVaryings DKShadowPassVertex(Attributes input)
{
    Varyings v = ShadowPassVertex(input);
    DKShadowVaryings output = (DKShadowVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
#if defined(_ALPHATEST_ON)
    output.uv = v.uv;
#endif
    output.positionCS = v.positionCS;
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    return output;
}

half4 DKShadowPassFragment(DKShadowVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
#if defined(_DK_CUT)
    if (_DKCutShape.w > .5 && DKCutOn()) clip(DKCutKeepAt(input.positionWS, _DKCutBack.z) - DKBayer4(input.positionCS.xy));
#endif
    Varyings v = (Varyings)0;
    UNITY_TRANSFER_INSTANCE_ID(input, v);
#if defined(_ALPHATEST_ON)
    v.uv = input.uv;
#endif
    v.positionCS = input.positionCS;
    return ShadowPassFragment(v);
}
#undef DK_CUT_SHADOW
#endif
