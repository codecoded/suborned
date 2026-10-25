// Brief 061 pick A: the crossing's light-dot rows (Vela's re-cut LightDotRow_2 / _1: u = metres from the module's -X end to the dot's centre,
// the same for every vertex of a dot, so a dot is lit or dark, never cut). Stock URP Lit has no gate, so this one draws them: a dot is lit
// when _LitFrom <= u <= _LitTo (SignalHead writes both per module by MaterialPropertyBlock, with the state's _EmissionColor, linear x level,
// HDR: Glint's signals.json); a dark dot shows its charcoal enamel seat (_Seat). Emission only, unlit, opaque, no shadow either way (a
// signal: Glint 2026-10-23, no fog on a signal). LOOK ONLY: nothing in the sim reads it.
Shader "Suborned/Signal Dots"
{
    Properties
    {
        [HDR] _EmissionColor("Emission (linear x level)", Color) = (0.36, 0.92, 0.70, 1)
        _Seat("Dark dot (charcoal enamel seat)", Color) = (0.13, 0.14, 0.15, 1)
        _LitFrom("Lit from (u, m)", Float) = -10000
        _LitTo("Lit to (u, m)", Float) = 10000
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _EmissionColor; half4 _Seat; float _LitFrom; float _LitTo;
        CBUFFER_END
        struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
        struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
        V vert(A i)
        {
            V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            o.pos = TransformObjectToHClip(i.pos.xyz); o.uv = i.uv; return o;
        }
        ENDHLSL
        Pass
        {
            Name "SignalDots"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            half4 frag(V i) : SV_Target
            {
                bool lit = i.uv.x >= _LitFrom && i.uv.x <= _LitTo;
                return half4(lit ? _EmissionColor.rgb : _Seat.rgb, 1);
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
            #pragma fragment fragDepth
            #pragma multi_compile_instancing
            half fragDepth(V i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
