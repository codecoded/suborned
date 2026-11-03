// PORTED (Glint, brief 075-A, 2026-10-28) from ~/Projects/the-suborned/game/Assets/TheSuborned/Art/DecoKit/Shaders/DK_Ocean.shader (sha256 70fcc0e8...),
// the old reel's sea, with the old DecoKitHarbour.Ocean values as its defaults (waves 42 / 11 m, slope 0.55, speed 1; fade 80-1600 m; deep
// #0d526b-ish (0.05, 0.32, 0.42), scatter (0.1, 0.55, 0.5), sky lift 0.45 / cap 0.55; glitter power 1200 x 40; F0 0.02). The wave sheet is the
// old DK_Sea_Normal (Resources/Atmosphere/dk_sea_normal). Our additions, so it drops in for Suborned/Water (Look.Water, the district water and
// the horizon sea share one material): the sea dissolves into the haze toward the far shore (_FadeCentre / _FadeFrom / _FadeTo / _Haze, as
// Suborned/Water), the perspective tiers' day haze (_AtmHaze, AtmosphereLens) replaces the scene fog colour, GPU instancing, the soft-shadow
// variants our URP asset uses, and a DepthNormals pass. Nothing else changes.
// Harbour sea to the horizon (studies: Concepts/District aerial_harbour, parade_eye). Two wind-drifted layers of wave
// slopes (DK_Sea_Normal: RG slope, B up) flatten with distance so the far sea turns to a mirror of the horizon haze;
// Fresnel mixes a lit teal body (sun + ambient like any surface in the scene, so it holds up against the HDR sky;
// wave faces toward the sun glow through) with the reflection probe, plus a tight sun glitter. Airbrush licence:
// reflections are lifted toward the zenith (_Sky.x) and capped (_Sky.y), so the sea reads blue-teal to the haze line
// instead of mirroring the pale horizon at every grazing view. Receives the main light's shadows; scene fog applies.
Shader "Suborned/DK Ocean"
{
    Properties
    {
        _WaveTex("Wave slopes (tileable, linear)", 2D) = "gray" {}
        _Deep("Body albedo", Color) = (0.05, 0.32, 0.42, 1)
        _Scatter("Wave glow toward the sun", Color) = (0.1, 0.55, 0.5, 1)
        _Sky("Reflection: x lift toward zenith, y Fresnel cap", Vector) = (0.45, 0.55, 0, 0)
        _Waves("Waves: x tile A (m), y tile B (m), z slope, w speed", Vector) = (42, 11, 0.55, 1)
        _Wind("Wind direction (xz)", Vector) = (0.8, 0.6, 0, 0)
        _Fade("Slope fade: x start, y end (m)", Vector) = (80, 1600, 0, 0)
        _Sun("Sun glitter: x power, y strength", Vector) = (1200, 40, 0, 0)
        _F0("Reflectance at normal incidence", Range(0, 0.2)) = 0.02
        _Haze ("Haze colour (far shore)", Color) = (0.95, 0.91, 0.83, 1)
        [Toggle] _SkyOn ("Reflect the old sky (DK_SpaceSky's air) by day", Float) = 0
        [HDR] _SkyZenith ("Sky zenith", Color) = (0.15, 0.59, 0.6, 1)
        [HDR] _SkyMid ("Sky mid", Color) = (0.32, 1.05, 0.95, 1)
        [HDR] _SkyHorizon ("Sky horizon haze", Color) = (1.38, 1.18, 0.82, 1)
        [HDR] _SkySun ("Sky sun colour", Color) = (1.5, 1.36, 1.1, 1)
        _SkyShape ("Sky: x mid height, y haze height", Vector) = (0.95, 0.1, 0, 0)
        _SkyAureole ("Sky aureole: x strength, y power, z haze lift, w broad", Vector) = (0.8, 180, 0.6, 0.15)
        _FadeCentre ("Fade centre (plan x, y)", Vector) = (0, 0, 0, 0)
        _FadeFrom ("Fade from (m)", Float) = 10000000
        _FadeTo ("Fade to (m)", Float) = 20000000
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_WaveTex); SAMPLER(sampler_WaveTex);
        CBUFFER_START(UnityPerMaterial)
            float4 _WaveTex_ST, _Deep, _Scatter, _Sky, _Waves, _Wind, _Fade, _Sun;
            float _F0;
            half4 _Haze; float4 _FadeCentre; float _FadeFrom, _FadeTo;
            float _SkyOn; float4 _SkyZenith, _SkyMid, _SkyHorizon, _SkySun, _SkyShape, _SkyAureole;
        CBUFFER_END
        float _AtmNight; // AtmosphereLens: 1 while it is night (the sea mirrors the night cube, not the day sky)
        // DK_SpaceSky's Air() verbatim (Shaders/DecoKit/DK_SpaceSky.shader), with the sky material's values copied onto this material by the builder:
        // the old scene's default reflection was the skybox itself (RenderSettings.defaultReflectionMode Skybox), ours is the street-canyon cube
        // (light.json reflection.street) that chrome needs, so by day the sea computes the old sky's air in its reflected direction instead.
        float3 SkyAir(float3 v, float3 sun)
        {
            float h = max(v.y, 0);
            float3 upper = lerp(_SkyMid.rgb, _SkyZenith.rgb, pow(saturate(h / _SkyShape.x), 1.5));
            float haze = exp(-h / _SkyShape.y);
            float mu = saturate(dot(v, sun));
            float3 horizon = _SkyHorizon.rgb * (1 + _SkyAureole.z * pow(mu, 6));
            float3 col = lerp(upper, horizon, haze);
            col += _SkySun.rgb * (_SkyAureole.x * pow(mu, _SkyAureole.y) + _SkyAureole.w * pow(mu, 8));
            return v.y >= 0 ? col : _SkyHorizon.rgb;
        }
        float4 _AtmHaze; // AtmosphereLens: the perspective tiers' day haze, linear; alpha 0 = the scene fog and _Haze
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings vert(Attributes i)
            {
                Varyings o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);  // fog per pixel: the sea is one 8 km quad
                return o;
            }

            float2 Slope(float2 uv) { return SAMPLE_TEXTURE2D(_WaveTex, sampler_WaveTex, uv).xy * 2 - 1; }

            half4 frag(Varyings i) : SV_Target
            {
                float3 p = i.positionWS, v = normalize(GetWorldSpaceViewDir(p));
                float dist = distance(GetCameraPositionWS(), p);
                float2 wind = normalize(_Wind.xy) * _Time.y * _Waves.w;
                float2 s = Slope(p.xz / _Waves.x + wind * .01) + .6 * Slope(p.xz / _Waves.y - wind.yx * .03);
                s *= _Waves.z * (1 - smoothstep(_Fade.x, _Fade.y, dist));
                float3 n = normalize(float3(s.x, 1, s.y));

                Light sun = GetMainLight(TransformWorldToShadowCoord(p));
                float3 r = reflect(-v, n);
                r.y = lerp(abs(r.y), 1, _Sky.x);  // a wave facing away reflects the sky; lift toward the zenith's blue
                r = normalize(r);
                float3 sky = _SkyOn > 0.5 && _AtmNight < 0.5 ? SkyAir(r, sun.direction)
                           : DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, r, 0), unity_SpecCube0_HDR);
                float ndv = saturate(dot(n, v));
                float fres = _F0 + (_Sky.y - _F0) * pow(1 - ndv, 5);
                float lit = sun.shadowAttenuation;
                float3 light = sun.color * saturate(dot(n, sun.direction)) * lit + SampleSH(n);
                float glow = pow(saturate(dot(-v, sun.direction) * .5 + .5), 3) * (1 - ndv);  // back-lit crests
                float3 body = _Deep.rgb * light + _Scatter.rgb * sun.color * lit * glow * .25;
                float glint = pow(saturate(dot(n, normalize(sun.direction + v))), _Sun.x) * _Sun.y * lit;
                float3 col = lerp(body, sky, fres) + sun.color * glint;
                bool atm = _AtmHaze.a > 0.5;
                col = MixFogColor(col, atm ? _AtmHaze.rgb : unity_FogColor.rgb, ComputeFogFactor(TransformWorldToHClip(p).z));
                col = lerp(col, atm ? _AtmHaze.rgb : _Haze.rgb, smoothstep(_FadeFrom, _FadeTo, distance(p.xz, _FadeCentre.xy))); // the far shore
                return half4(col, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            struct A { float4 pos : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 vert(A a) : SV_POSITION { UNITY_SETUP_INSTANCE_ID(a); return TransformObjectToHClip(a.pos.xyz); }
            half frag() : SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            struct A { float4 pos : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 vert(A a) : SV_POSITION { UNITY_SETUP_INSTANCE_ID(a); return TransformObjectToHClip(a.pos.xyz); }
            half4 frag() : SV_Target { return half4(0, 1, 0, 0); }
            ENDHLSL
        }
    }
}
