// DecoKit window film (brief 048, Vela): the glass in front of a facade bay (DK_GlassDark: ribbon glazing, deco window columns, portholes, shop transoms).
// It used to be URP Lit, smoothness 0.98, alpha 0.3 over a charcoal back wall: the film mirrored the dark street-canyon horizon of the reflection cube and the bay read as a
// black void from third person. This draws a fake sky instead of reading any probe: no reflection probe, no extra pass, one transparent draw.
//   the pane = a glaze tint (_BaseColor, alpha = film cover looking straight at it) that turns to the mirror at grazing angles (fresnel, alpha up to _Edge);
//   the mirror = R.y up: _SkyHorizon to _SkyZenith, R.y down: _Street (the warm paving, seen when the camera looks down at the pane, as the play camera does);
//   a faint diagonal sheen streak; everything scaled by the scene's trilight ambient (light.json ambient: by day 1, at night about 0.25, so night follows the phase light).
// Premultiplied blend (One OneMinusSrcAlpha), no depth write, no shadow, no depth-only pass. Fog (the atmosphere's distance haze) mixes toward the fog colour.
Shader "Suborned/DecoKit/GlassSky"
{
    Properties
    {
        _BaseColor("Glaze (a = cover at normal incidence)", Color) = (0.17, 0.30, 0.33, 0.5)
        _SkyHorizon("Sky horizon", Color) = (0.69, 0.79, 0.77, 1)
        _SkyZenith("Sky zenith", Color) = (0.24, 0.48, 0.75, 1)
        _Street("Street (reflected below the horizon)", Color) = (0.39, 0.27, 0.19, 1)
        _Edge("Cover at grazing", Range(0, 1)) = 0.92
        _Fresnel("Fresnel power", Range(1, 6)) = 3
        _Sheen("Sheen streak", Range(0, 1)) = 0.12
        _Glow("Horizon glow (reflection when the view runs along the street)", Range(0, 1)) = 0.85
        [HideInInspector] _Cull("__cull", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            Name "GlassSky"
            Tags { "LightMode" = "UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            // the GPU Resident Drawer draws only DOTS-instanced shaders: without this, every renderer carrying DK_GlassDark (a bay's glass sits in
            // the same renderer as its frame) fell back to the per-renderer path (Latch, 0.2.80 tp-long: SetPass 332 -> 390, shadow casters 898 -> 1,885)
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor; half4 _SkyHorizon; half4 _SkyZenith; half4 _Street; half _Edge; half _Fresnel; half _Sheen; half _Glow;
            CBUFFER_END

            struct A { float4 pos : POSITION; float3 nrm : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 pos : SV_POSITION; float3 wp : TEXCOORD0; half3 wn : TEXCOORD1; half fog : TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };

            V vert(A i)
            {
                V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.wp = TransformObjectToWorld(i.pos.xyz);
                o.wn = TransformObjectToWorldNormal(i.nrm);
                o.pos = TransformWorldToHClip(o.wp);
                o.fog = ComputeFogFactor(o.pos.z);
                return o;
            }

            half4 frag(V i, half facing : VFACE) : SV_Target
            {
                half3 n = normalize(i.wn) * (facing > 0 ? 1 : -1);
                half3 v = GetWorldSpaceNormalizeViewDir(i.wp);
                half ndv = saturate(dot(n, v));
                half3 r = reflect(-v, n);
                half day = saturate(dot(unity_AmbientEquator.rgb, half3(0.3, 0.59, 0.11)) * 1.45);   // ambient 0.7 by day -> 1; night 0.18 -> about 0.25
                half3 sky = lerp(_SkyHorizon.rgb, _SkyZenith.rgb, smoothstep(0.1, 0.9, r.y));
                half3 mirror = lerp(_Street.rgb, sky, smoothstep(-0.2, 0.7, r.y));
                // shopfront at eye height (brief 048 fix 2): looking along a street the reflection runs near the horizon (R.y about -0.1), not down at the paving, and the pane read
                // as a brown-grey void. A band of the hazy horizon centred on R.y -0.1, 0.2 wide; at the play camera (R.y about -0.6) it is under 1 percent, so play zoom is unchanged.
                half glow = exp(-(r.y + 0.1) * (r.y + 0.1) / 0.045) * _Glow;
                mirror = lerp(mirror, _SkyHorizon.rgb * 1.2, glow);
                half fres = pow(1 - ndv, _Fresnel);
                half sheen = smoothstep(0.35, 0.5, frac((i.wp.x + i.wp.z) * 0.07 + i.wp.y * 0.05)) * _Sheen * (1 + 2.0 * fres * saturate(glow * 2));
                half cover = lerp(_BaseColor.a, _Edge, fres);
                half3 col = (lerp(_BaseColor.rgb, mirror, saturate(0.4 + fres)) + sheen) * day;
                half3 pm = col * cover;
                pm = MixFogColor(pm, unity_FogColor.rgb * cover, i.fog);
                return half4(pm, cover);
            }
            ENDHLSL
        }
    }
}
