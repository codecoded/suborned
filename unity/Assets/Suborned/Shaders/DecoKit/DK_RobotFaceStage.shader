// The patrol robot's attention face (brief 031; Vela's proposal briefs/031/robot-face/DK_RobotFaceStage.shader, built on DK_RobotFace's LED
// look): M_RobotFace on DK_PatrolRobot_Head_Face, drawn additively over the display glass, unlit, outside the grade.
// Atlas robot_face_stage_v1.png (16 x 60, R G, point, no mips, linear): ten rows a slice from the TOP: calm, noticed, wary, reporting, calm
// blink half, calm blink shut. R = eyes (shifted by _Gaze cells in stages 1 and 2), G = mouth and pictogram. Per renderer (RobotFaceStage's
// property block): _Stage 0..3, _Gaze -1/0/1 (x _GazeCells), _Blink 0/1/2 (stage 0 only). Phosphor #BFF3E6 at HDR 1.6 for stages 0-2,
// alarm #FF5A3C at 2.2 for stage 3, which flashes 0.3 s on / 0.2 s off (2 Hz). Past about a pixel a cell the dots blend to their average
// so the grid never shimmers at distance (DK_RobotFace's rule).
Shader "DecoKit/RobotFaceStage"
{
    Properties
    {
        [NoScaleOffset] _Atlas("Stage atlas", 2D) = "black" {}
        [HDR] _Phosphor("Phosphor", Color) = (0.83, 1.43, 1.26, 1)
        [HDR] _Alarm("Alarm", Color) = (2.2, 0.22, 0.1, 1)
        _Ghost("Unlit LED level", Range(0, .2)) = .05
        _Dot("LED radius (cells)", Range(.2, .5)) = .36
        _Stage("Stage 0..3", Float) = 0
        _Gaze("Gaze -1..1", Float) = 0
        _Blink("Blink 0..2", Float) = 0
        _GazeCells("Gaze cells", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
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
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Atlas);
            CBUFFER_START(UnityPerMaterial)
                float4 _Phosphor, _Alarm;
                float _Ghost, _Dot, _Stage, _Gaze, _Blink, _GazeCells;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float fog : TEXCOORD1; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 g = i.uv * float2(16, 10);
                int2 cell = clamp((int2)floor(g), int2(0, 0), int2(15, 9));
                float r = length(frac(g) - .5), aa = fwidth(r), blur = saturate(max(fwidth(g.x), fwidth(g.y)) * 2 - 1);
                int stage = clamp((int)round(_Stage), 0, 3), blink = (int)round(_Blink);
                int slice = stage == 0 ? (blink == 1 ? 4 : blink == 2 ? 5 : 0) : stage;   // slices 4, 5 are calm's blink
                int ex = cell.x - (int)round(_Gaze * _GazeCells * ((stage == 1 || stage == 2) ? 1 : 0));
                int row = slice * 10 + (9 - cell.y);                                      // rows from the atlas top; LOAD is bottom-up
                const int h = 60;
                float eyes = ex >= 0 && ex < 16 ? LOAD_TEXTURE2D(_Atlas, int2(ex, h - 1 - row)).r : 0;
                float rest = LOAD_TEXTURE2D(_Atlas, int2(cell.x, h - 1 - row)).g;
                float lit = max(eyes, rest);
                float flash = stage == 3 ? step(fmod(_Time.y, .5), .3) : 1;               // 2 Hz, 0.3 on / 0.2 off
                float dot = lerp(1 - smoothstep(_Dot - aa, _Dot + aa, r), PI * _Dot * _Dot, blur);
                float3 col = (stage == 3 ? _Alarm.rgb : _Phosphor.rgb) * dot * lerp(_Ghost, flash, lit);
                return half4(MixFogColor(col, 0, i.fog), 1);
            }
            ENDHLSL
        }
    }
}
