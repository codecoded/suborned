// Brief 097 (Sky, 2026-11-06: "make this a breathtaking game with juiciness"): wind in the planting, opt-in per material with _DK_WIND
// (shader_feature_local_vertex, so a material without it compiles and draws exactly as before).
// One world-space wind for the whole city (globals set by Suborned.Wind097 every frame; unset = a calm default breeze), applied to the object-space
// position before the pass's own vertex function, in every pass (ForwardLit, ShadowCaster, DepthOnly, DepthNormals) with the same inputs, so the
// depth prepass, the shadow map and the colour pass always agree. The offset depends only on the undisplaced world position, the height above
// the object's origin (a kit piece's origin is its foot; a merged tile's origin is the world's, so its height is the height above street level)
// and _Time: deterministic, no state.
// Per material: the sway's reach in metres is URP Lit's _Parallax (negative: weighted by the distance from the piece's axis too, for palms) (unused by these materials: _PARALLAXMAP is never on; the Lit copy already
// reuses _ClearCoatMask for its reflection scale the same way), so the SRP Batcher's UnityPerMaterial is unchanged.
//   sway    a slow bend down-wind, weighted by height^1.5 over _DKWind.z metres, with gusts that travel across the city
//   flutter a quick small shiver per vertex (a hash of its rest position), a third of the reach, so leaves and fronds rustle
// Included by DK_LitCutaway, DK_ComplexLitCutaway and DK_LitCutawayLeaf in each pass, after the pass's own include and before DK_Cutaway.hlsl,
// with the DK_CUT_* define of that pass; it defines DK_WIND_OS (DK_Cutaway's shadow vertex calls it) and the pass's DK vertex wrapper.
#ifndef DK_WIND097_INCLUDED
#define DK_WIND097_INCLUDED

float4 _DKWind;      // xy the wind's direction on the ground (unit, world x, z); z the height (m) at which the sway reaches full; w the wind's speed (1/s)
float4 _DKWindGust;  // x gust strength (0..1); y gust scale (1/m); z flutter rate (rad/s); w the global strength (0 = still; Suborned.Wind097 sets 1)

float DKWindHash(float3 p) { return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453); }

float3 DKWindOffsetWS(float3 pWS, float h, float reach)
{
    float2 dir = dot(_DKWind.xy, _DKWind.xy) > 1e-4 ? _DKWind.xy : float2(0.8, 0.6);
    float href = _DKWind.z > 0.1 ? _DKWind.z : 4.0, speed = _DKWind.w > 0.0 ? _DKWind.w : 1.1;
    float gustK = _DKWindGust.y > 0.0 ? _DKWindGust.y : 0.045, gustS = _DKWindGust.x > 0.0 ? _DKWindGust.x : 0.55;
    float flutterW = _DKWindGust.z > 0.0 ? _DKWindGust.z : 9.0;
    float t = _Time.y;
    float along = dot(pWS.xz, dir);
    float gust = 1.0 - gustS + gustS * (0.5 + 0.5 * sin(along * gustK - t * 0.55) * sin(pWS.x * 0.031 + pWS.z * 0.027 + t * 0.21));
    float w = pow(saturate(h / href), 1.5);
    float phase = along * 0.35 - t * speed;
    float sway = (0.65 * sin(phase) + 0.35 * sin(phase * 2.17 + 1.3)) * gust;
    float rnd = DKWindHash(floor(pWS * 8.0) * 0.125);
    float flutter = sin(t * flutterW + rnd * 6.2831) * (0.25 + 0.75 * gust) * saturate(h * 2.0 + 0.25);
    float3 o = float3(dir.x, 0, dir.y) * (sway * w * reach);
    o += float3(0.6 - rnd, 0.35, rnd - 0.4) * (flutter * reach * 0.33);
    o.y -= abs(sway) * w * reach * 0.25; // the bend dips the tip a little
    return o;
}

float3 DKWindApply(float3 pOS)
{
    float strength = _DKWindGust.w, reach = abs(_Parallax);
    UNITY_BRANCH if (strength <= 0.0 || reach <= 0.0) return pOS;
    float3 pWS = TransformObjectToWorld(pOS), origin = TransformObjectToWorld(float3(0, 0, 0));
    // a negative reach weights by the distance from the piece's vertical axis as well (a palm: the frond tips sway, the crown stays on the trunk)
    float radial = _Parallax < 0.0 ? saturate(length(pWS.xz - origin.xz) / 2.5) : 1.0;
    float3 o = DKWindOffsetWS(pWS, pWS.y - origin.y, reach * strength) * radial;
    return pOS + mul((float3x3)GetWorldToObjectMatrix(), o);
}

#if defined(_DK_WIND)
    #define DK_WIND_OS(p) p = DKWindApply(p)
#else
    #define DK_WIND_OS(p)
#endif

#endif // DK_WIND097_INCLUDED

// the pass's vertex wrapper (each pass includes this file once, with its DK_CUT_* define still set)
#if defined(DK_CUT_FORWARD) && !defined(DK_WIND097_FORWARD)
#define DK_WIND097_FORWARD
Varyings DKLitPassVertex(Attributes input) { DK_WIND_OS(input.positionOS.xyz); return LitPassVertex(input); }
#endif
#if defined(DK_CUT_DEPTH) && !defined(DK_WIND097_DEPTH)
#define DK_WIND097_DEPTH
Varyings DKDepthOnlyVertex(Attributes input) { DK_WIND_OS(input.position.xyz); return DepthOnlyVertex(input); } // URP's DepthOnlyPass names it `position`
#endif
#if defined(DK_CUT_DEPTH_NORMALS) && !defined(DK_WIND097_DEPTH_NORMALS)
#define DK_WIND097_DEPTH_NORMALS
Varyings DKDepthNormalsVertex(Attributes input) { DK_WIND_OS(input.positionOS.xyz); return DepthNormalsVertex(input); }
#endif
