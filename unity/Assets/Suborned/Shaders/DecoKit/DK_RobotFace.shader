// A service robot's dot-matrix display face, drawn additively over the model's dark glass (the glass keeps its own
// reflections underneath). The screen sheet's UVs span the display; _Grid cuts it into square LED cells, and each
// cell lights if it falls on one of three glyphs (two eyes, a mouth) from the RobotGlyphs atlas (_DK_RobotGlyphs,
// set globally by RobotFace: r = lit, g = glyph height / 8, glyphs bottom-left in 8 x 8 tiles). Per renderer
// (RobotFace's property block): _Grid = (cells across, cells up, corner radius in cells), _EyeL/_EyeR/_Mouth =
// (origin cell x, y, tile, blink: 0 open, 1 half, 2 shut to a bar). Unlit LEDs glow faintly (_Ghost); past about a
// pixel a cell the dots blend to their average so the grid never shimmers at distance.
Shader "DecoKit/RobotFace"
{
    Properties
    {
        [HDR] _Glow("LED glow", Color) = (3, 1.2, .2, 1)
        _Ghost("Unlit LED level", Range(0, .2)) = .05
        _Dot("LED radius (cells)", Range(.2, .5)) = .36
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

            TEXTURE2D(_DK_RobotGlyphs);
            CBUFFER_START(UnityPerMaterial)
                float4 _Glow, _Grid, _EyeL, _EyeR, _Mouth;
                float _Ghost, _Dot;
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

            float Glyph(int2 cell, float4 g)
            {
                int2 t = cell - (int2)g.xy;
                if (g.z < 0 || any(t < 0) || any(t > 7)) return 0;
                int x0 = (int)g.z * 8;
                float4 s = LOAD_TEXTURE2D(_DK_RobotGlyphs, int2(x0 + t.x, t.y));
                float mid = (round(s.g * 8) - 1) * .5;
                if (g.w < .5) return s.r;
                if (g.w < 1.5) return abs(t.y - mid) <= max((mid + .5) * .5, .5) ? s.r : 0;  // half: the middle rows
                if (t.y != (int)floor(mid)) return 0;                                          // shut: one bar across
                float lit = 0;
                for (int y = 0; y < 8; y++) lit = max(lit, LOAD_TEXTURE2D(_DK_RobotGlyphs, int2(x0 + t.x, y)).r);
                return lit;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 g = i.uv * max(_Grid.xy, 1);
                float r = length(frac(g) - .5), aa = fwidth(r), blur = saturate(max(fwidth(g.x), fwidth(g.y)) * 2 - 1);
                if (_Grid.x < 1) return 0;
                int2 cell = (int2)floor(g);
                float2 q = abs(cell + .5 - _Grid.xy * .5) - (_Grid.xy * .5 - _Grid.z);
                if (length(max(q, 0)) > _Grid.z) return 0;  // outside the display's rounded corners
                float lit = max(max(Glyph(cell, _EyeL), Glyph(cell, _EyeR)), Glyph(cell, _Mouth));
                float dot = lerp(1 - smoothstep(_Dot - aa, _Dot + aa, r), PI * _Dot * _Dot, blur);
                float3 col = _Glow.rgb * dot * lerp(_Ghost, 1, lit);
                return half4(MixFogColor(col, 0, i.fog), 1);
            }
            ENDHLSL
        }
    }
}
