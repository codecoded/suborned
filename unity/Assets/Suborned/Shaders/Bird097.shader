// Brief 097: the square's pigeons and doves (Suborned.Birds097, one instanced draw). A tiny made-at-runtime mesh whose vertex colour carries the
// wing: r = how far out along the wing (0 body .. 1 tip), g = the side (0 left, 1 right). The wing beats about the body's long axis by
// _Bird.y (0 folded on the ground .. 1 flying) at a per-bird phase _Bird.x; _Bird.z picks the plumage (0 grey pigeon .. 1 white dove).
// Lit by the sun (with its shadow and the cloud cookie) and the sky; casts a small shadow with the same beat.
Shader "Suborned/Bird"
{
    Properties
    {
        _Grey("Pigeon", Color) = (0.56, 0.58, 0.62, 1)
        _Neck("Pigeon neck", Color) = (0.38, 0.52, 0.5, 1)
        _White("Dove", Color) = (0.95, 0.94, 0.9, 1)
        _Beat("Beat (rate rad/s, lift m)", Vector) = (24, 0.16, 0, 0)
    }
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    CBUFFER_START(UnityPerMaterial)
        half4 _Grey; half4 _Neck; half4 _White; float4 _Beat;
    CBUFFER_END
    UNITY_INSTANCING_BUFFER_START(Birds)
        UNITY_DEFINE_INSTANCED_PROP(float4, _Bird)
    UNITY_INSTANCING_BUFFER_END(Birds)
    struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
    float3 Beat(float3 p, half4 c, float4 b)
    {
        float w = c.r; float side = c.g > 0.5 ? 1.0 : -1.0;
        float a = sin(_Time.y * _Beat.x + b.x) * b.y;               // the beat
        float fold = 1.0 - b.y;                                      // folded on the ground: the wing lies along the body
        p.y += a * w * _Beat.y * 4.0;
        p.x = lerp(p.x, side * 0.03 + p.x * 0.15, fold * saturate(w * 3.0));
        p.y -= fold * w * 0.02;
        return p;
    }
    ENDHLSL
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct V { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; half4 color : TEXCOORD2; float4 bird : TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID };
            V vert(A i)
            {
                V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_TRANSFER_INSTANCE_ID(i, o);
                float4 b = UNITY_ACCESS_INSTANCED_PROP(Birds, _Bird);
                float3 p = Beat(i.positionOS.xyz, i.color, b);
                o.positionWS = TransformObjectToWorld(p); o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS); o.color = i.color; o.bird = b;
                return o;
            }
            half4 frag(V i, half face : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half3 n = normalize(i.normalWS) * (face > 0 ? 1.0h : -1.0h);
                half3 body = lerp(_Grey.rgb, _White.rgb, i.bird.z);
                half neck = i.color.b * (1.0h - i.bird.z);               // b: the pigeon's iridescent neck
                body = lerp(body, _Neck.rgb, neck);
                body *= lerp(1.0h, 0.72h, i.color.r * (1.0h - i.bird.z)); // darker wing bars on a pigeon
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS), i.positionWS, half4(1, 1, 1, 1));
                half3 c = body * (SampleSH(n) + sun.color * saturate(dot(n, sun.direction) * 0.8h + 0.2h) * sun.shadowAttenuation);
                return half4(c, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ColorMask 0 Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            float3 _LightDirection;
            struct V { float4 positionCS : SV_POSITION; };
            V vert(A i)
            {
                V o; UNITY_SETUP_INSTANCE_ID(i);
                float3 p = Beat(i.positionOS.xyz, i.color, UNITY_ACCESS_INSTANCED_PROP(Birds, _Bird));
                float3 ws = TransformObjectToWorld(p); float3 n = TransformObjectToWorldNormal(i.normalOS);
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(ws, n, _LightDirection));
                #if UNITY_REVERSED_Z
                    o.positionCS.z = min(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    o.positionCS.z = max(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }
            half4 frag(V i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
