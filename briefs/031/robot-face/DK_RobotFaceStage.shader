// PROPOSAL for Latch / Splice (Vela, brief 031): draws M_RobotFace, the attention face. Not in Unity yet; untested there.
// Atlas robot_face_stage_v1.png (16 x 60, RG, point, no mips, linear): ten rows a slice from the TOP: calm, noticed, wary, reporting, calm blink half, calm blink shut.
// R = eyes (shifted by _Gaze cells), G = mouth and pictogram. Per renderer (property block): _Stage 0..3, _Gaze -1/0/1 (x _GazeCells), _Blink 0/1/2 (stage 0 only).
// Colour: phosphor #BFF3E6 HDR 1.6 for stages 0-2, alarm #FF5A3C HDR 2.2 at stage 3, which flashes 0.3 s on / 0.2 s off. Unlit, additive, outside the grade.
Shader "DecoKit/RobotFaceStage"
{
    Properties
    {
        _Atlas("Stage atlas", 2D) = "black" {}
        [HDR] _Phosphor("Phosphor", Color) = (1.5, 3.1, 2.7, 1)   // #BFF3E6 at ~1.6 HDR (linear)
        [HDR] _Alarm("Alarm", Color) = (2.2, 0.17, 0.07, 1)       // #FF5A3C at 2.2
        _Ghost("Unlit LED level", Range(0, .2)) = .05
        _Dot("LED radius (cells)", Range(.2, .5)) = .36
        _Stage("Stage 0..3", Float) = 0
        _Gaze("Gaze -1..1", Float) = 0
        _Blink("Blink 0..2", Float) = 0
        _GazeCells("Gaze cells", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_Atlas);
            CBUFFER_START(UnityPerMaterial)
                float4 _Phosphor, _Alarm; float _Ghost, _Dot, _Stage, _Gaze, _Blink, _GazeCells;
            CBUFFER_END
            struct A { float4 p : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 cs : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert(A i) { V o; o.cs = TransformObjectToHClip(i.p.xyz); o.uv = i.uv; return o; }
            half4 frag(V i) : SV_Target
            {
                float2 g = i.uv * float2(16, 10);
                int2 cell = (int2)floor(g);
                float r = length(frac(g) - .5), aa = fwidth(r), blur = saturate(max(fwidth(g.x), fwidth(g.y)) * 2 - 1);
                int stage = (int)round(_Stage);
                int slice = stage == 0 ? (int)round(_Blink) == 1 ? 4 : (int)round(_Blink) == 2 ? 5 : 0 : stage;  // slices 4, 5 are calm's blink
                int ex = cell.x - (int)round(_Gaze * _GazeCells * (stage == 1 || stage == 2 ? 1 : 0));
                int row = slice * 10 + (9 - cell.y);              // rows from the atlas top; LOAD is bottom-up
                int h = 60;
                float eyes = ex >= 0 && ex < 16 ? LOAD_TEXTURE2D(_Atlas, int2(ex, h - 1 - row)).r : 0;
                float rest = LOAD_TEXTURE2D(_Atlas, int2(cell.x, h - 1 - row)).g;
                float lit = max(eyes, rest);
                float flash = stage == 3 ? step(fmod(_Time.y, .5), .3) : 1;   // 2 Hz, 0.3 on / 0.2 off
                float dot = lerp(1 - smoothstep(_Dot - aa, _Dot + aa, r), PI * _Dot * _Dot, blur);
                float3 col = (stage == 3 ? _Alarm.rgb : _Phosphor.rgb) * dot * lerp(_Ghost, flash, lit);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
