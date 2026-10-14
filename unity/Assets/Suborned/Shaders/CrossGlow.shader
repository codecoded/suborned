// The crossing eye's light volumes (brief 009 C): DecoKit kind `glow` (tools/decokit/crossing_common.py) ships no .mat; Unity draws them
// additive, double-sided, unlit, never casting or receiving a shadow. Colour = the kit row's colour x strength x alpha.
//   _Mode 0 (beam): an open cone, UV v = 0 at the ground to 1 at the nozzle; brightest at the nozzle, _Floor of it at the ground.
//   _Mode 1 (halo): a flat disc, UV = x, y in metres from the beam centre; full inside _Inner, falling to _Floor at _Radius.
// Fog fades it to nothing (additive: fog colour black), so far eyes do not glow through the haze.
Shader "Suborned/Cross Glow"
{
    Properties
    {
        _BaseColor("Colour", Color) = (0.608, 0.812, 0.761, 1)
        _Strength("Strength", Float) = 2.0
        _Alpha("Alpha", Range(0, 1)) = 0.28
        [Enum(Beam,0,Halo,1)] _Mode("Mode", Float) = 0
        _Floor("Floor", Range(0, 1)) = 0.25
        _Inner("Inner radius (m)", Float) = 0.15
        _Radius("Radius (m)", Float) = 0.47
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            Name "CrossGlow"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor; half _Strength; half _Alpha; half _Mode; half _Floor; half _Inner; half _Radius;
            CBUFFER_END

            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; half fog : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };

            V vert(A i)
            {
                V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = TransformObjectToHClip(i.pos.xyz);
                o.uv = i.uv;
                o.fog = ComputeFogFactor(o.pos.z);
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                half k = _Mode < 0.5
                    ? lerp(_Floor, 1.0h, saturate(i.uv.y))
                    : lerp(1.0h, _Floor, smoothstep(_Inner, _Radius, length(i.uv)));
                half3 c = _BaseColor.rgb * (_Strength * _Alpha * k);
                c = MixFogColor(c, half3(0, 0, 0), i.fog);
                return half4(c, 0);
            }
            ENDHLSL
        }
    }
}
