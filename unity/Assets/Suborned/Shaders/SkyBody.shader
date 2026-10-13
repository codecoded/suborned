// Sky bodies of the horizon band (planet, moons; brief 007 E): unlit and unfogged, so a body 6 km up keeps its shape and colour
// instead of dissolving into distance fog. Its haze comes from the export (horizon.pieces[].haze): the builder mixes that share of
// the haze colour into _BaseColor. A soft limb (darker toward the silhouette) keeps the sphere reading round without a light.
Shader "Suborned/SkyBody"
{
    Properties
    {
        _BaseColor ("Colour (haze mixed)", Color) = (1, 1, 1, 1)
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
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Limb;
            CBUFFER_END
            struct A { float4 pos : POSITION; float3 nrm : NORMAL; };
            struct V { float4 pos : SV_POSITION; float3 nrm : TEXCOORD0; float3 view : TEXCOORD1; };
            V vert(A a)
            {
                V v; float3 w = TransformObjectToWorld(a.pos.xyz);
                v.pos = TransformWorldToHClip(w); v.nrm = TransformObjectToWorldNormal(a.nrm); v.view = GetWorldSpaceViewDir(w);
                return v;
            }
            half4 frag(V v) : SV_Target
            {
                half f = saturate(abs(dot(normalize(v.nrm), normalize(v.view))));
                return half4(_BaseColor.rgb * lerp(1 - _Limb, 1, f), 1);
            }
            ENDHLSL
        }
    }
}
