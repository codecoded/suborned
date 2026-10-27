// Sky bodies of the horizon band (planet, moons; brief 007 E): unlit and unfogged, so a body 6 km up keeps its shape and colour
// instead of dissolving into distance fog. The body is the kit material's map and colour (_BaseMap x _BaseColor: the old DecoKit
// planet bands, ring profile and moon, brief 072 decision 4); its haze comes from the export (horizon.pieces[].haze): the builder
// sets _HazeColor and the share _Haze, mixed over the body. A soft limb (darker toward the silhouette) keeps the sphere reading
// round without a light. The ring stays opaque: horizon.py cuts its gaps as geometry from the old alpha profile.
Shader "Suborned/SkyBody"
{
    Properties
    {
        _BaseMap ("Map (kit texture)", 2D) = "white" {}
        _BaseColor ("Colour", Color) = (1, 1, 1, 1)
        _HazeColor ("Haze colour", Color) = (1, 1, 1, 1)
        _Haze ("Haze share", Range(0, 1)) = 0
        _Limb ("Limb darkening", Range(0, 1)) = 0.25
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _HazeColor;
                half _Haze;
                half _Limb;
            CBUFFER_END
            struct A { float4 pos : POSITION; float3 nrm : NORMAL; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float3 nrm : TEXCOORD0; float3 view : TEXCOORD1; float2 uv : TEXCOORD2; };
            V vert(A a)
            {
                V v; float3 w = TransformObjectToWorld(a.pos.xyz);
                v.pos = TransformWorldToHClip(w); v.nrm = TransformObjectToWorldNormal(a.nrm); v.view = GetWorldSpaceViewDir(w);
                v.uv = TRANSFORM_TEX(a.uv, _BaseMap);
                return v;
            }
            half4 frag(V v) : SV_Target
            {
                half f = saturate(abs(dot(normalize(v.nrm), normalize(v.view))));
                half3 body = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, v.uv).rgb * _BaseColor.rgb * lerp(1 - _Limb, 1, f);
                return half4(lerp(body, _HazeColor.rgb, _Haze), 1);
            }
            ENDHLSL
        }
    }
}
