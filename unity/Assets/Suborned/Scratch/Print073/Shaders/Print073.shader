// Brief 073 SCRATCH (Glint, 2026-10-28): the vintage-print finish. Pass 0 = specular cap (scene-linear HDR, before post).
// Pass 1 = print (after post, display-referred linear in, converted to sRGB-ish gamma for the grade and back).
// All screen patterns are in 1080p-normalised pixels fixed to the screen (no time term): they never swim; the image moves under them,
// as a picture moves under a printed screen.
Shader "Hidden/Suborned/Scratch/Print073"
{
    Properties
    {
        // +stencil (073 round 3): 8 = Always (off); 6 = NotEqual (skip the pixels the Mark pass tagged with bit 64: cones, crossing heads and dots, lamp heads)
        [HideInInspector] _P73StencilComp ("stencil comp", Float) = 8
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off Blend Off
        Stencil { Ref 64 ReadMask 64 Comp [_P73StencilComp] }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float4 _P73Cap;      // knee, range, mix
        float4 _P73Fade;     // fade, saturation, highlight desat, warmth
        float4 _P73Ink, _P73Paper;
        float4 _P73Line;     // strength, low, high, ortho
        float4 _P73Dot;      // strength, cell px, angle rad, colour
        float4 _P73PaperFx;  // grain, mottle, vignette
        float4 _P73Screen;   // w, h, h/1080
        float4 _P73Clip;     // near, far
        float4 _P73Anchor;   // screen px of the world anchor (0 = screen-fixed)
        float4 _P73Cel;      // bands, soft
        float4 _P73Haze;     // top mix
        float4 _P73HazeCol;
        float4 _P73Paint;    // radius px
        TEXTURE2D_X_FLOAT(_P73Depth);
        ENDHLSL

        Pass
        {
            Name "Print073 Cap"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings i) : SV_Target
            {
                float4 c = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, i.texcoord, 0);
                float m = max(c.r, max(c.g, c.b));
                float knee = _P73Cap.x, range = _P73Cap.y;
                if (m > knee)
                {
                    float over = m - knee;
                    float m2 = knee + over / (1.0 + over / range);   // soft shoulder, asymptote knee + range
                    c.rgb *= lerp(1.0, m2 / m, _P73Cap.z);
                }
                return c;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Print073 Print"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            uint Hash(uint2 p) { uint h = p.x * 0x8da6b343u ^ p.y * 0xd8163841u; h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15; return h; }
            float H01(float2 p) { return (Hash(uint2(int2(p) + 4096)) & 0xffffu) / 65535.0; }
            float VNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p); f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(H01(i), H01(i + float2(1, 0)), f.x), lerp(H01(i + float2(0, 1)), H01(i + float2(1, 1)), f.x), f.y);
            }
            float Eye(int2 px)
            {
                float raw = LOAD_TEXTURE2D_X(_P73Depth, clamp(px, int2(0, 0), int2(_P73Screen.xy) - 1)).r;
                #if UNITY_REVERSED_Z
                    float d01 = 1.0 - raw;
                #else
                    float d01 = raw;
                #endif
                return _P73Line.w > 0.5 ? lerp(_P73Clip.x, _P73Clip.y, d01) : LinearEyeDepth(raw, _ZBufferParams);
            }
            // one halftone screen: coverage c (0..1 ink) -> 1 inside a dot, 0 outside; p in 1080p px
            float Dot(float2 p, float c, float ang, float cell)
            {
                float s = sin(ang), k = cos(ang);
                float2 g = float2(k * p.x - s * p.y, s * p.x + k * p.y) / cell;
                float d = length(frac(g) - 0.5);
                float r = sqrt(saturate(c) / 3.14159) * 1.0;   // dot area = coverage (merges past ~0.78)
                float aa = 0.75 / cell;
                return 1.0 - smoothstep(r - aa, r + aa, d);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                int2 px = int2(i.positionCS.xy);
                float4 src = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv, 0);
                float3 g = pow(saturate(src.rgb), 1.0 / 2.2);                // display gamma for the grade
                float2 p = (i.positionCS.xy - _P73Anchor.xy) / max(_P73Screen.z, 1e-3); // 1080p-normalised px, screen-fixed (or ground-anchored on a pan)

                // ink lines: depth second difference (creases and silhouettes; flat planes are linear in ortho depth -> 0)
                float edge = 0;
                if (_P73Line.x > 0)
                {
                    int o = max(1, (int)round(_P73Screen.z));
                    float c = Eye(px);
                    if (c < _P73Clip.y * 0.98)
                    {
                        float l = Eye(px + int2(-o, 0)), r = Eye(px + int2(o, 0)), d = Eye(px + int2(0, -o)), u = Eye(px + int2(0, o));
                        float lap = max(abs(l + r - 2 * c), abs(u + d - 2 * c));
                        float tol = 1.0 + c * 0.004;                                   // a little looser far away
                        edge = smoothstep(_P73Line.y * tol, _P73Line.z * tol, lap) * _P73Line.x;
                    }
                }

                // faded warm grade
                float y = dot(g, float3(0.299, 0.587, 0.114));
                float sat = _P73Fade.y * (1.0 - _P73Fade.z * smoothstep(0.55, 1.0, y));
                g = lerp(y.xxx, g, sat);
                g *= float3(1.0 + _P73Fade.w, 1.0 + _P73Fade.w * 0.25, 1.0 - _P73Fade.w * 1.2);
                // cel bands (comic): luminance quantised, hue kept
                if (_P73Cel.x > 0.5)
                {
                    float yc = max(dot(g, float3(0.299, 0.587, 0.114)), 1e-4);
                    float n = _P73Cel.x, t = yc * n, b = floor(t), f = t - b;
                    float q = (b + smoothstep(0.5 - _P73Cel.y, 0.5 + _P73Cel.y, f)) / n;
                    g *= q / yc;
                }
                g = lerp(g, _P73Ink.rgb + (_P73Paper.rgb - _P73Ink.rgb) * saturate(g), _P73Fade.x);
                // pastel haze toward the top of the frame (farther at the play view)
                if (_P73Haze.x > 0) g = lerp(g, _P73HazeCol.rgb, _P73Haze.x * smoothstep(0.15, 1.0, 1.0 - uv.y));

                // ink line, laid in the ink colour
                g = lerp(g, _P73Ink.rgb * 0.8, edge);

                // halftone (mean-tone preserving: tone *= 1 - k (dot - coverage))
                if (_P73Dot.x > 0)
                {
                    float cell = _P73Dot.y, a = _P73Dot.z;
                    float yl = dot(g, float3(0.299, 0.587, 0.114));
                    float k = _P73Dot.x * (1.0 - smoothstep(0.72, 0.95, yl));   // paper-white highlights stay clean
                    float cl = 1.0 - yl;
                    float mono = 1.0 - k * (Dot(p, cl, a + 0.785398, cell) - cl);          // K screen at angle + 45
                    float3 cmy = 1.0 - g;
                    float3 col = 1.0 - k * (float3(Dot(p, cmy.r, a, cell), Dot(p, cmy.g, a + 1.047198, cell), Dot(p, cmy.b, a - 0.261799, cell)) - cmy);
                    g *= lerp(mono.xxx, col, _P73Dot.w);
                }

                // paper: fine grain, mottling, an edge fade
                float grain = (VNoise(p / 1.3) - 0.5) * 0.65 + (VNoise(p / 3.7 + 17.0) - 0.5) * 0.35;
                float mott = (VNoise(p / 140.0 + 3.0) - 0.5) * 0.6 + (VNoise(p / 45.0 + 9.0) - 0.5) * 0.4;
                g *= 1.0 + _P73PaperFx.x * grain * 2.0 + _P73PaperFx.y * mott * 2.0;
                float2 e = (uv - 0.5) * float2(_P73Screen.x / _P73Screen.y, 1.0);
                g = lerp(g, g * _P73Paper.rgb * 0.85, _P73PaperFx.z * saturate(pow(length(e) * 1.25, 3.0)));

                return half4(pow(saturate(g), 2.2), src.a);
            }
            ENDHLSL
        }
            Pass
        {
            Name "Print073 Paint"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            // smoothed 4-sector Kuwahara (Papari-style weights 1/(sigma^q)): flat painted patches with kept edges; display-linear in and out
            half4 Frag(Varyings i) : SV_Target
            {
                int r = (int)_P73Paint.x;
                int2 px = int2(i.positionCS.xy);
                int2 hi = int2(_P73Screen.xy) - 1;
                float3 m[4] = { float3(0,0,0), float3(0,0,0), float3(0,0,0), float3(0,0,0) };
                float3 s2[4] = { float3(0,0,0), float3(0,0,0), float3(0,0,0), float3(0,0,0) };
                [loop] for (int y = -r; y <= r; y++)
                [loop] for (int x = -r; x <= r; x++)
                {
                    float3 c = pow(saturate(LOAD_TEXTURE2D_X(_BlitTexture, clamp(px + int2(x, y), int2(0, 0), hi)).rgb), 1.0 / 2.2);
                    float3 c2 = c * c;
                    if (x <= 0 && y <= 0) { m[0] += c; s2[0] += c2; }
                    if (x >= 0 && y <= 0) { m[1] += c; s2[1] += c2; }
                    if (x <= 0 && y >= 0) { m[2] += c; s2[2] += c2; }
                    if (x >= 0 && y >= 0) { m[3] += c; s2[3] += c2; }
                }
                float n = (r + 1) * (r + 1);
                float3 acc = 0; float wsum = 0;
                [unroll] for (int k = 0; k < 4; k++)
                {
                    float3 mu = m[k] / n; float3 v = abs(s2[k] / n - mu * mu);
                    float sg = v.r + v.g + v.b;
                    float w = 1.0 / (1.0 + pow(abs(sg) * 255.0, 4.0));
                    acc += mu * w; wsum += w;
                }
                float4 src = LOAD_TEXTURE2D_X(_BlitTexture, px);
                return half4(pow(saturate(acc / max(wsum, 1e-6)), 2.2), src.a);
            }
            ENDHLSL
        }

        Pass
        {
            // 3: plain copy (no stencil test): fills the skipped pixels before a stencil-tested pass draws over the rest
            Name "Print073 Copy"
            Stencil { Comp Always }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings i) : SV_Target { return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, i.texcoord, 0); }
            ENDHLSL
        }

        Pass
        {
            // 4: mark: draws a protected renderer into the camera depth-stencil only (bit 64), depth-tested against the scene
            Name "Print073 Mark"
            ZTest LEqual ZWrite Off Cull Off ColorMask 0 Blend Zero One Offset -1, -1
            Stencil { Ref 64 WriteMask 64 Comp Always Pass Replace }
            HLSLPROGRAM
            #pragma vertex MarkVert
            #pragma fragment MarkFrag
            struct MarkIn { float4 positionOS : POSITION; };
            float4 MarkVert(MarkIn v) : SV_POSITION { return TransformObjectToHClip(v.positionOS.xyz); }
            half4 MarkFrag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
