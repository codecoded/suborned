// Brief 079 note 2 (Vela). LOOK ONLY: nothing in the sim reads it.
// The crossing's lit studs (crossing079.py's LightDotRow_2 / _1, slot DK_Cross_Light) and the signal head's rim (DK_Signal_RimLens). One mesh, one draw, one float: u gates as before (_LitFrom <= u <= _LitTo, written by SignalHead);
// v is the fall-off: 0 at the lens's centre, 1 at its edge and at the foot of its spill on the road, 2 where the spill ends.
//   lens  (v <= 1)  opaque: the state's HDR colour falling to _Edge of itself at the lens edge, staying rich colour, with only a small hot point at the centre (brief 079 review: pale-centred rings were the first attempt's fault)
//   spill (v > 1)   the state's colour added to the road, and _Absorb of the road taken away first so it tints the road rather than bleaching it (Blend One OneMinusSrcAlpha: premultiplied; alpha 0 is pure additive)
//   gated off       dark glass in the housing (_Seat), no spill
// A rim has no gate of its own: its material keeps the wide default window and SignalHead writes only _EmissionColor. The same queue and blend for all of it: no extra pass, no extra draw.
Shader "Suborned/Signal Dots"
{
    Properties
    {
        [HDR] _EmissionColor("Emission (linear x level)", Color) = (0.36, 0.92, 0.70, 1)
        _Seat("Dark stud (dark glass)", Color) = (0.055, 0.065, 0.07, 1)
        _LitFrom("Lit from (u, m)", Float) = -10000
        _LitTo("Lit to (u, m)", Float) = 10000
        _Edge("Lens edge (of the colour)", Range(0, 1)) = 0.72
        _Core("Core (added)", Float) = 0.35
        _CoreWhite("Core whiteness", Range(0, 1)) = 0.08
        _CorePow("Core tightness", Float) = 6
        _Glow("Spill at the housing (of the colour)", Float) = 0.22
        _Fall("Spill fall-off power", Float) = 2.4
        _Absorb("Spill tints the road by", Range(0, 1)) = 0.12
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            Name "SignalDots"
            Tags { "LightMode" = "UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _EmissionColor; half4 _Seat; float _LitFrom; float _LitTo; half _Edge; half _Core; half _CoreWhite; half _CorePow; half _Glow; half _Fall; half _Absorb;
            CBUFFER_END
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A i)
            {
                V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = TransformObjectToHClip(i.pos.xyz); o.uv = i.uv; return o;
            }
            half4 frag(V i) : SV_Target
            {
                bool lit = i.uv.x >= _LitFrom && i.uv.x <= _LitTo;
                float v = i.uv.y;
                if (v <= 1.0005)
                {
                    half r = saturate(v);
                    if (!lit) return half4(_Seat.rgb * lerp(0.6h, 1.0h, r), 1);
                    half3 e = _EmissionColor.rgb;
                    half3 hue = e / max(max(e.r, max(e.g, e.b)), 0.001h);
                    half body = lerp(_Edge, 1.0h, smoothstep(0.0h, 0.45h, 1.0h - r));
                    half core = pow(1.0h - r, _CorePow);
                    return half4(e * body + lerp(hue, half3(1, 1, 1), _CoreWhite) * core * _Core, 1);
                }
                if (!lit) return half4(0, 0, 0, 0);
                half k = pow(saturate(2.0 - v), _Fall);
                return half4(_EmissionColor.rgb * k * _Glow, k * _Absorb);
            }
            ENDHLSL
        }
    }
}
