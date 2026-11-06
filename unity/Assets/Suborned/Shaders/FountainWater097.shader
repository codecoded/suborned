// Brief 097: the atom fountain's pool, as the testbed drew it (the-suborned/testbed/src/fountain-water.js, createFountainWaterMaterials: "analytic
// surface detail and a refracted mosaic impression in one opaque pass"; fountain_tb.py: "its animated ripple and caustic shader is NOT ported here").
// Ported to URP's PBR lighting: two crossing swells and a fine chop as the normal, the mosaic seen through the water (its UVs swim with the swell,
// tinted to depth: pale at the pedestal, deep teal at the rim), the caustic web, an impact ripple ring at each of the 12 jets' landings (radius 2.02 m,
// every 30 degrees) and the wet ring at the pedestal. Pool coordinates are the world position less the piece's origin (the pool centre on the ground),
// so the material needs no UVs of its own; _BaseMap is the mosaic (fountain_tex.py), sampled on the pool plane at _PoolTile metres. Opaque, no
// shadow of its own; receives the sun's shadow and the cloud cookie. _Night lifts the deep tint as the testbed's setNight did.
Shader "Suborned/Fountain Water"
{
    Properties
    {
        [MainTexture] _BaseMap("Mosaic", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _Deep("Deep water", Color) = (0.018, 0.2, 0.235, 1)
        _Shallow("Depth tint near / far", Vector) = (0.47, 0.68, 0.72, 0)
        _Far("Depth tint at the rim", Vector) = (0.85, 1.03, 1.04, 0)
        _Smoothness("Smoothness", Range(0, 1)) = 0.92
        _PoolTile("Mosaic tile (m)", Float) = 2
        _Impact("Jet landing ring radius (m)", Float) = 2.02
        _Contact("Pedestal radius (m)", Float) = 0.55
        _Caustic("Caustic strength", Float) = 1
        _Night("Night", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _FountainCentre097; // global (Suborned.FountainFx097): xyz the pool centre, w 1 when set
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; half4 _BaseColor; half4 _Deep; half4 _Shallow; half4 _Far;
                half _Smoothness; float _PoolTile; float _Impact; float _Contact; half _Caustic; half _Night;
            CBUFFER_END

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V
            {
                float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 pool : TEXCOORD1; half3 normalWS : TEXCOORD2; half fog : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            V vert(A i)
            {
                V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_TRANSFER_INSTANCE_ID(i, o);
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.pool = p.positionWS - (_FountainCentre097.w > 0.5 ? _FountainCentre097.xyz : TransformObjectToWorld(float3(0, 0, 0))); // the runtime's fountain centre (a merged tile has the world's origin)
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float t = _Time.y;
                float2 pool = i.pool.xz; float radius = length(pool);
                float waveA = dot(pool, float2(5.2, 3.7)) + t * 0.95, waveB = dot(pool, float2(-3.6, 6.3)) - t * 0.72, waveC = dot(pool, float2(17.0, 11.0)) + t * 1.3;
                // the nearest jet's landing (12 jets, one every 30 degrees)
                float sector = floor((atan2(pool.y, pool.x) + 0.261799) / 0.523599) * 0.523599;
                float2 impactV = pool - float2(cos(sector), sin(sector)) * _Impact;
                float impactD = length(impactV);
                float ripplePhase = impactD * 34.0 - t * 6.0, rippleEnv = exp(-impactD * 3.0);
                float contactD = max(0.0, radius - _Contact), contactEnv = exp(-contactD * 5.0), contactPhase = contactD * 30.0 - t * 1.8;
                // the mosaic through the water, swimming with the swell, tinted to depth
                float2 floorUV = pool / _PoolTile + float2(sin(dot(pool, float2(5.2, 3.7)) + t * 0.95), sin(dot(pool, float2(-3.6, 6.3)) - t * 0.72)) * 0.009;
                half3 floorC = SAMPLE_TEXTURE2D_BIAS(_BaseMap, sampler_BaseMap, floorUV, 1.5).rgb * _BaseColor.rgb;
                half3 albedo = lerp(_Deep.rgb, floorC, 0.45);
                float depth = smoothstep(0.65, 2.9, radius);
                albedo *= lerp(_Shallow.rgb, _Far.rgb, depth);
                float2 refracted = pool + float2(sin(waveA), sin(waveB)) * 0.028;
                float2 cp = refracted * 6.5 + float2(sin(waveB), sin(waveA)) * 0.28;
                float cw = sin(cp.x + cp.y * 0.6 + t * 0.25) + sin(cp.y - cp.x * 0.65 - t * 0.28) + sin(cp.x * 0.6 - cp.y * 0.8 + t * 0.31);
                float caustic = pow(max(0.0, 1.0 - abs(cw) * 0.48), 12.0);
                float foam = rippleEnv * pow(max(0.0, sin(ripplePhase)), 12.0);
                float contactRipple = pow(max(0.0, sin(contactPhase)), 8.0) * contactEnv;
                albedo += half3(0.02, 0.065, 0.06) * caustic * _Caustic * (1.0 - _Night * 0.65);
                albedo += half3(0.045, 0.065, 0.06) * foam;
                albedo += half3(0.008, 0.019, 0.018) * contactRipple;
                albedo *= 1.0 - exp(-contactD * 80.0) * 0.14; // the wet contact: dark and narrow, not a painted collar
                // the surface's slope: the swells, the chop, the landing rings and the pedestal's ring
                float2 impactDir = impactV / max(impactD, 0.015);
                float2 g = float2(5.2, 3.7) * cos(waveA) * 0.010 + float2(-3.6, 6.3) * cos(waveB) * 0.008 + float2(17.0, 11.0) * cos(waveC) * 0.0015
                         + impactDir * rippleEnv * (-34.0 * sin(ripplePhase) - 3.0 * cos(ripplePhase)) * 0.004
                         + pool / max(radius, 0.001) * contactEnv * cos(contactPhase) * 0.026;
                half3 n = normalize(half3(i.normalWS) + half3(-g.x, 0, -g.y));

                InputData d = (InputData)0;
                d.positionWS = i.positionWS; d.normalWS = n; d.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                d.shadowCoord = TransformWorldToShadowCoord(i.positionWS); d.fogCoord = i.fog;
                d.bakedGI = SampleSH(n); d.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS); d.shadowMask = half4(1, 1, 1, 1);
                SurfaceData s = (SurfaceData)0;
                s.albedo = albedo; s.metallic = 0; s.specular = 0; s.smoothness = _Smoothness; s.occlusion = 1; s.alpha = 1;
                s.emission = _Night * half3(0.003, 0.025, 0.029) * 8.0;
                half4 c = UniversalFragmentPBR(d, s);
                c.rgb = MixFog(c.rgb, i.fog);
                return half4(c.rgb, 1);
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
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; half4 _BaseColor; half4 _Deep; half4 _Shallow; half4 _Far;
                half _Smoothness; float _PoolTile; float _Impact; float _Contact; half _Caustic; half _Night;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            V vert(A i) { V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_TRANSFER_INSTANCE_ID(i, o); o.positionCS = TransformObjectToHClip(i.positionOS.xyz); return o; }
            half frag(V i) : SV_Target { return i.positionCS.z; }
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
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; half4 _BaseColor; half4 _Deep; half4 _Shallow; half4 _Far;
                half _Smoothness; float _PoolTile; float _Impact; float _Contact; half _Caustic; half _Night;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; half3 n : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            V vert(A i) { V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_TRANSFER_INSTANCE_ID(i, o); o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.n = TransformObjectToWorldNormal(i.normalOS); return o; }
            half4 frag(V i) : SV_Target { return half4(NormalizeNormalPerPixel(i.n), 0); }
            ENDHLSL
        }
    }
}
