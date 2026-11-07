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
#ifndef DK_WIND_OS
#define DK_WIND_OS(p) // brief 097: DK_Wind097.hlsl's wind, where a shader includes it
#endif
#ifndef DK_CLOTH_OS
#define DK_CLOTH_OS(p, n, uv) // brief 097: DK_Wind097.hlsl's cloth, where a shader includes it
#endif

float4 _DKCutFocus;  // xyz the focus on the ground (world); w the disc's radius (m), 0 = off
float4 _DKCutView;   // xyz the camera's forward (world, unit); w the floor (m): nothing lower is cut
float4 _DKCutShape;  // x feather (m); y ghost (what stays inside the disc: 0 = clean away); z column mode (0 / 1); w shadows cut too (0 / 1)
float4 _DKCutBack;   // x how far beyond the focus along the view the cut reaches (m); y its feather (m); z ghost shadow (what stays in the shadow map); w how many of _DKCutPoints are in use
// Brief 016 (the view turns): the points the player must see besides the screen centre (the agents, the action target, the cursor's ground
// point, the key people): each keeps its own disc clear, by the same rule as the focus, at its own level. xyz the point (world), w its
// disc radius (m). Always DK_CUT_POINTS long (ArchitectureFade.MaxPoints); the first _DKCutBack.w are used. Each point's floor
// stands as far above it as the focus's floor stands above the focus.
// Brief 020 (Glyph: no screen door over a person): a point with a negative radius is a person's disc, cut clean rather than to the ghost,
// with its own narrow feather: _DKCutClean.x what stays there (0 at full fade), .y its feather (m). The shadow map keeps the ghost shadow.
#define DK_CUT_POINTS 12
float4 _DKCutPoints[DK_CUT_POINTS];
float4 _DKCutClean;  // x what stays of a person's disc (0 at full fade); y its feather (m); z the fade's ease (0 solid .. 1 full, ArchitectureFade.fadeTime)
// Brief 056 F (Sky 2026-10-25: "the soft circle cut with silhouettes, as the one rule for every yaw"): in the camera's passes (forward,
// depth, depth-normals) a capsule per point replaces the ground discs: the point's foot to its head on the screen (view-space x, y: the
// camera is orthographic, so view space is screen space in metres), a soft edge from _DKCutCaps.z of its radius out to the radius, cut only
// where the fragment is nearer the camera than the point by _DKCutCaps.w (0.45 m on linear eye depth): a wall behind a person, or beside
// it at the same depth, stays whole. Brief 079: the soft edge is a clean line at its middle (it was interleaved gradient noise, ragged on a sunlit roof), a small cream
// lean on the rim (_DKCutRim, scaled by the wall's lit luminance x _DKCutRimLit; Glint). With capsules ArchitectureFade sets _DKCutShape.w 0, so the shadow pass never discards.
float4 _DKCutCaps;                    // x capsule mode (0 / 1); y capsules in use; z the soft edge's start as a share of the radius (0.55); w the depth margin (m)
float4 _DKCapsSeg[DK_CUT_POINTS];     // view-space foot (x, y) and head (z, w), m
float4 _DKCapsInfo[DK_CUT_POINTS];    // x the point's linear eye depth (m); y the capsule's radius (m)
float4 _DKCutRim;                     // rgb the rim's lean (linear); a its strength at full fade
float _DKCutRimLit;                   // the rim's cream scales with the wall's lit luminance x this (Glint, signals.json capsule.rimLit)
static float dkRim = 0.0;             // the rim's lean for this fragment (set by DKCutaway, read by the forward pass)

// Interleaved gradient noise (Jimenez 2014): a fine, even threshold per pixel, soft at 2x where Bayer 4x4 reads as a grid.
float DKIGN(float2 px) { return frac(52.9829189 * frac(dot(px, float2(0.06711056, 0.00583715)))); }

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

// How much of the fragment at world position p stays (1 = all of it) for one disc: round `focus`, radius `radius`, nothing at or below
// floorY cut, nothing more than `back` beyond the focus along the view cut.
float DKCutDisc(float3 p, float3 focus, float radius, float floorY, float ghost, float featherM)
{
    if (p.y <= floorY) return 1.0;
    float2 k = _DKCutView.xz / max(-_DKCutView.y, 1e-3);  // ground run of the view ray per metre it drops
    float2 hit = p.xz + k * (p.y - focus.y);
    float d = _DKCutShape.z > .5 ? DKSegmentDistance(focus.xz, p.xz + k * (floorY - focus.y), hit)
                                 : length(hit - focus.xz);
    float feather = max(featherM, 1e-3);
    float outside = saturate((d - (radius - feather)) / feather);
    float2 ahead = _DKCutView.xz / max(length(_DKCutView.xz), 1e-3);  // the view on the ground
    float beyond = saturate((dot(p.xz - focus.xz, ahead) - _DKCutBack.x) / max(_DKCutBack.y, 1e-3) + 1.0);
    return lerp(ghost, 1.0, max(outside, beyond));
}

// How much of the fragment at world position p stays (1 = all of it) when the faded part keeps `ghost`: the least of the focus disc's
// and every point's.
float DKCutKeepAt(float3 p, float ghost, float clean)
{
    float keep = DKCutDisc(p, _DKCutFocus.xyz, _DKCutFocus.w, _DKCutView.w, ghost, _DKCutShape.x);
    float rise = _DKCutView.w - _DKCutFocus.y;
    int n = min((int)_DKCutBack.w, DK_CUT_POINTS);
    for (int i = 0; i < n; i++)
    {
        float4 q = _DKCutPoints[i];
        bool person = q.w < 0.0;
        // Glyph's 0.2.99 review 1b: a person's clean disc keeps its whole radius clean and feathers outward over _DKCutClean.y (the column's
        // vertical edges over a figure were a 0.4 m hard cut)
        float r = person ? abs(q.w) + _DKCutClean.y : abs(q.w);
        keep = min(keep, DKCutDisc(p, q.xyz, r, q.y + rise, person ? clean : ghost, person ? _DKCutClean.y : _DKCutShape.x));
    }
    return keep;
}

float DKCutKeep(float3 p) { return DKCutKeepAt(p, _DKCutShape.y, _DKCutClean.x); }

// Brief 056 F: how much of the fragment stays (1 = all) at view-space position v (x, y on the screen in m; depth its linear eye depth):
// the least over the capsules it is nearer the camera than, smoothstep from 0.55 r to r round the foot-to-head segment.
float DKCapsKeep(float2 v, float depth)
{
    float keep = 1.0;
    int n = min((int)_DKCutCaps.y, DK_CUT_POINTS);
    for (int i = 0; i < n; i++)
    {
        float2 info = _DKCapsInfo[i].xy;
        if (depth >= info.x - _DKCutCaps.w) continue;  // not in front of the point: whole
        float4 s = _DKCapsSeg[i];
        keep = min(keep, smoothstep(info.y * _DKCutCaps.z, info.y, DKSegmentDistance(v, s.xy, s.zw)));
    }
    return keep;
}

bool DKCutOn() { return _DKCutFocus.w > 0.0 && (GetMeshRenderingLayer() & DK_CUT_LAYER) != 0u; }

// Glyph's 0.2.94 review #1 (the heavy stipple): what stays of a ghost is drawn as a veil, not the wall's own colour at full contrast:
// the kept fragments lean towards _DKCutVeil.rgb (linear) by _DKCutVeil.a times how far into the ghost they are. Returns that lean
// (0 outside any cut) and the ground point the fragment hides (its view ray at the focus's level).
// Glyph's 0.2.97 review #1 (a dither screen over the Records Office door and paving at yaw 30, none at 210): the two yaws differ in
// light, not in the cut. At 210 the faces that ghost are the ones in shade, so their kept dots are dim over dim paving; at 30 the
// ghosted wing faces the sun, and its dots were lit as a sunny surface wherever the street behind lay, so they read as a pale screen
// over the shaded paving. The veil is now the street behind as it is lit: the paving's albedo (_DKCutVeil.rgb) under the sun as it
// falls on flat ground at that point, through the shadow map there (so the ghost shadow and the towers' shade count), plus the sky's
// ambient. The dots sink into the paving they cover; the mass still reads where it covers anything else, and by its ghost shadow.
float4 _DKCutVeil;
float DKCutaway(float4 positionCS, out float3 ground)
{
    ground = 0;
#if defined(_DK_CUT)
    if (!DKCutOn()) return 0.0;
    float3 p = ComputeWorldSpacePosition(GetNormalizedScreenSpaceUV(positionCS), positionCS.z, UNITY_MATRIX_I_VP);
    if (_DKCutCaps.x > 0.5)
    {   // brief 056 F: the capsules, eased in and out with the fade
        float3 v = TransformWorldToView(p);
        float ck = lerp(1.0, DKCapsKeep(v.xy, -v.z), _DKCutClean.z);
        if (ck < 0.999) { clip(ck - 0.5); dkRim = _DKCutRim.a * (1.0 - abs(ck * 2.0 - 1.0)); }  // brief 079 (Sky's fight video, the ragged patch on the café roof): one clean edge at the middle of the soft band, no noise; the rim's lean still runs over the kept half
        return 0.0;
    }
    float keep = DKCutKeep(p);
    clip(keep - DKBayer4(positionCS.xy));
    float2 k = _DKCutView.xz / max(-_DKCutView.y, 1e-3);
    ground = float3(p.x + k.x * (p.y - _DKCutFocus.y), _DKCutFocus.y, p.z + k.y * (p.y - _DKCutFocus.y));
    return _DKCutVeil.a * saturate((1.0 - keep) / max(1.0 - _DKCutShape.y, 1e-3));
#else
    return 0.0;
#endif
}
float DKCutaway(float4 positionCS) { float3 g; return DKCutaway(positionCS, g); }

#endif

#if defined(DK_CUT_FORWARD)
void DKLitPassFragment(Varyings input, out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    float3 ground;
    float veil = DKCutaway(input.positionCS, ground);
    LitPassFragment(input, outColor
#ifdef _WRITE_RENDERING_LAYERS
        , outRenderingLayers
#endif
    );
    // the veil is the street behind in this light: the sun on flat ground at the hidden point, through its shadow, plus the sky's ambient
    if (veil > 0.0)
    {
        half shade = 1.0;
#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && !defined(_MAIN_LIGHT_SHADOWS_SCREEN)
        shade = MainLightRealtimeShadow(TransformWorldToShadowCoord(ground));
#endif
        half3 lit = _MainLightColor.rgb * (saturate(_MainLightPosition.y) * shade) + SampleSH(half3(0, 1, 0));
        outColor.rgb = lerp(outColor.rgb, _DKCutVeil.rgb * lit, veil);
    }
    if (dkRim > 0.0) { half lum = dot(outColor.rgb, half3(0.2126, 0.7152, 0.0722)); outColor.rgb = lerp(outColor.rgb, _DKCutRim.rgb * saturate(lum * _DKCutRimLit), dkRim); }  // brief 056 F: the cut's rim leans cream, scaled by the wall's own lit luminance (Glint 2026-10-25: faint on enamel and at night, nothing on sunlit stone)
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
    UNITY_SETUP_INSTANCE_ID(input); // first: under the GPU Resident Drawer (DOTS instancing) it loads the material caches the wind reads (_Parallax) and the instance's matrix
    DK_WIND_OS(input.positionOS.xyz); // brief 097: the same wind as the camera's passes
    DK_CLOTH_OS(input.positionOS.xyz, input.normalOS, input.texcoord);
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
    if (_DKCutShape.w > .5 && DKCutOn()) clip(DKCutKeepAt(input.positionWS, _DKCutBack.z, _DKCutBack.z) - DKBayer4(input.positionCS.xy));
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
