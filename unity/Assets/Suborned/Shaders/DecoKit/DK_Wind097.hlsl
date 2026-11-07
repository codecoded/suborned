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
// Cloth, _DK_WIND_CLOTH (one keyword set with _DK_WIND, so a material is one or the other; DK_LitCutaway only; Sky 2026-11-07: "I like idea of
// motion - we have a lot of banners"): a banner hung between two arms (the lamp posts' comet banners) moves as a sheet pinned on its four
// edges, most in the middle (DKSheetOffsetWS below). Reach |_Parallax| m in the middle.
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

// SUPERSEDED, unused: the first cut (it read the vertex normal and tore seams between the banner's panels). Left word for word so the
// variants without cloth keep their compiled cache; drop it with the next change that recompiles every DK variant anyway.
float3 DKClothOffsetWS(float3 pWS, float3 nWS, float v, float reach)
{
    float2 dir = dot(_DKWind.xy, _DKWind.xy) > 1e-4 ? _DKWind.xy : float2(0.8, 0.6);
    float speed = _DKWind.w > 0.0 ? _DKWind.w : 1.1;
    float gustK = _DKWindGust.y > 0.0 ? _DKWindGust.y : 0.045, gustS = _DKWindGust.x > 0.0 ? _DKWindGust.x : 0.55;
    float flutterW = _DKWindGust.z > 0.0 ? _DKWindGust.z : 9.0;
    float t = _Time.y, along = dot(pWS.xz, dir);
    float gust = 1.0 - gustS + gustS * (0.5 + 0.5 * sin(along * gustK - t * 0.55) * sin(pWS.x * 0.031 + pWS.z * 0.027 + t * 0.21));
    float2 nh = nWS.xz; float nl = length(nh);
    float2 n = nl > 1e-3 ? nh / nl : float2(0.0, 0.0); // no normal (DepthOnly): the rest pose
    // Even in n, so a face and its back (n, -n) and the neighbouring panels (each with its own slightly turned normal) all move together:
    // push = the wind's component across the cloth (full when it meets the banner square, none edge-on); edge = the flutter's axis edge-on to it.
    float2 push = n * dot(n, dir), edge = n * dot(n, float2(-dir.y, dir.x));
    float belly = sin(PI * saturate(v));
    float billow = gust * (0.6 + 0.4 * sin(along * 0.35 - t * speed));
    float ripple = sin(t * flutterW * 0.6 + along * 2.3 + pWS.y * 2.9) * (0.3 + 0.7 * gust) * 0.45;
    float2 o = push * (billow + ripple) + edge * ripple;
    return float3(o.x, 0.0, o.y) * (reach * belly);
}

float3 DKClothApply(float3 pOS, float3 nOS, float v)
{
    float strength = _DKWindGust.w, reach = abs(_Parallax);
    UNITY_BRANCH if (strength <= 0.0 || reach <= 0.0) return pOS;
    float3 pWS = TransformObjectToWorld(pOS), nWS = TransformObjectToWorldNormal(nOS, false);
    return pOS + mul((float3x3)GetWorldToObjectMatrix(), DKClothOffsetWS(pWS, nWS, v, reach * strength));
}

#if defined(_DK_WIND_CLOTH)
// The cloth as built (Sky 2026-11-07, the second cut): a sheet pinned on its four edges, sin(pi u) sin(pi v) on the art's uv (0..1 across and
// up; the back face's u runs the other way and sin is symmetric), so the hems stay on the arms and the sides by the pole; pushed down-wind by
// the gusts with a ripple, and flapped across the wind. Its direction is the wind's own, never the vertex normal: a banner is five panels,
// each with its own turned normal (the kit's static wave), and a back face, and anything read from the normal tore seams between them (the
// first cut above). Position and uv only, so every pass agrees, DepthOnly too. Kept inside this keyword so a cloth change recompiles only the
// cloth materials' variants.
float3 DKSheetOffsetWS(float3 pWS, float2 uv, float reach)
{
    float2 dir = dot(_DKWind.xy, _DKWind.xy) > 1e-4 ? _DKWind.xy : float2(0.8, 0.6);
    float speed = _DKWind.w > 0.0 ? _DKWind.w : 1.1;
    float gustK = _DKWindGust.y > 0.0 ? _DKWindGust.y : 0.045, gustS = _DKWindGust.x > 0.0 ? _DKWindGust.x : 0.55;
    float flutterW = _DKWindGust.z > 0.0 ? _DKWindGust.z : 9.0;
    float t = _Time.y, along = dot(pWS.xz, dir);
    float gust = 1.0 - gustS + gustS * (0.5 + 0.5 * sin(along * gustK - t * 0.55) * sin(pWS.x * 0.031 + pWS.z * 0.027 + t * 0.21));
    float w = sin(PI * saturate(uv.x)) * sin(PI * saturate(uv.y));
    float billow = gust * (0.6 + 0.4 * sin(along * 0.35 - t * speed));
    float ripple = sin(t * flutterW * 0.6 + along * 2.3 + pWS.y * 2.9) * (0.3 + 0.7 * gust);
    float flap = sin(t * flutterW * 0.45 - along * 1.7 + pWS.y * 2.1 + 1.3) * (0.3 + 0.7 * gust);
    float2 o = dir * (billow + 0.45 * ripple) + float2(-dir.y, dir.x) * (0.5 * flap);
    return float3(o.x, 0.0, o.y) * (reach * w);
}

float3 DKSheetApply(float3 pOS, float2 uv)
{
    float strength = _DKWindGust.w, reach = abs(_Parallax);
    UNITY_BRANCH if (strength <= 0.0 || reach <= 0.0) return pOS;
    float3 pWS = TransformObjectToWorld(pOS);
    return pOS + mul((float3x3)GetWorldToObjectMatrix(), DKSheetOffsetWS(pWS, uv, reach * strength));
}
    #define DK_CLOTH_OS(p, n, uv) p = DKSheetApply(p, uv)
#else
    #define DK_CLOTH_OS(p, n, uv)
#endif

#endif // DK_WIND097_INCLUDED

// the pass's vertex wrapper (each pass includes this file once, with its DK_CUT_* define still set). UNITY_SETUP_INSTANCE_ID comes first: under the GPU
// Resident Drawer (DOTS instancing) URP reads every material property through caches it fills there (LitInput.hlsl: _Parallax is unity_DOTS_Sampled_Parallax)
// and the object's matrix by the instance it sets; before it the wind read a reach of 0 and returned, so no wind showed in a player (it did with -nogrd).
// The pass's own vertex function sets it up again, which is harmless.
#if defined(DK_CUT_FORWARD) && !defined(DK_WIND097_FORWARD)
#define DK_WIND097_FORWARD
Varyings DKLitPassVertex(Attributes input) { UNITY_SETUP_INSTANCE_ID(input); DK_WIND_OS(input.positionOS.xyz); DK_CLOTH_OS(input.positionOS.xyz, input.normalOS, input.texcoord); return LitPassVertex(input); }
#endif
#if defined(DK_CUT_DEPTH) && !defined(DK_WIND097_DEPTH)
#define DK_WIND097_DEPTH
Varyings DKDepthOnlyVertex(Attributes input) { UNITY_SETUP_INSTANCE_ID(input); DK_WIND_OS(input.position.xyz); DK_CLOTH_OS(input.position.xyz, float3(0, 0, 0), input.texcoord); return DepthOnlyVertex(input); } // URP's DepthOnlyPass names it `position`
#endif
#if defined(DK_CUT_DEPTH_NORMALS) && !defined(DK_WIND097_DEPTH_NORMALS)
#define DK_WIND097_DEPTH_NORMALS
Varyings DKDepthNormalsVertex(Attributes input) { UNITY_SETUP_INSTANCE_ID(input); DK_WIND_OS(input.positionOS.xyz); DK_CLOTH_OS(input.positionOS.xyz, input.normal, input.texcoord); return DepthNormalsVertex(input); }
#endif
