// The signals (Glint 2026-10-23, after the signals-under-haze shot): the cones' fill, rim and scan, the guard's lamp, the rivals' marks and the fail
// camera's cone draw in this. Unlit, one colour (_BaseColor), and NO fog: a signal keeps its colour in the third-person haze (the URP Unlit template
// it replaces took the follow tier's fog). Transparent by default (SrcAlpha OneMinusSrcAlpha, no depth write, both faces, the Transparent queue, as
// the fan template); a material may set _SrcBlend/_DstBlend/_ZWrite for an opaque mark (the lamp). _ZTest and _OffsetFactor/_OffsetUnits exist for
// tp-long's control shot only (ZTest Always is never shipped). SRP Batcher (UnityPerMaterial) and GPU Resident Drawer (DOTS instancing) compatible.
Shader "Suborned/SignalUnlit"
{
    Properties
    {
        _BaseColor("Colour", Color) = (1, 1, 1, 1)
        [HideInInspector] _SrcBlend("__src", Float) = 5
        [HideInInspector] _DstBlend("__dst", Float) = 10
        [HideInInspector] _ZWrite("__zw", Float) = 0
        [HideInInspector] _Cull("__cull", Float) = 0
        [HideInInspector] _ZTest("__ztest", Float) = 4
        [HideInInspector] _OffsetFactor("__offf", Float) = 0
        [HideInInspector] _OffsetUnits("__offu", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            Name "SignalUnlit"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Cull [_Cull]
            Offset [_OffsetFactor], [_OffsetUnits]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            struct A { float4 pos : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 pos : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };

            V vert(A i)
            {
                V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = TransformObjectToHClip(i.pos.xyz);
                return o;
            }

            half4 frag(V i) : SV_Target { return _BaseColor; }
            ENDHLSL
        }
    }
}
