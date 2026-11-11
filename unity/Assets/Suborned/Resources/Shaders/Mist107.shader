// Brief 107 (Latch; Glint's ground mist): a full-screen triangle over the scene before transparents. _MistDepth = the scene depth; _MistParams = amount,
// unscaled clock, the mist's depth H (m: it falls to 1/e that far over the floor), the cap above knee height; _MistColour; _MistFocus = the camera focus.
Shader "Hidden/Suborned/Mist107"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Mist"
            ZTest Always ZWrite Off Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X_FLOAT(_MistDepth); SAMPLER(sampler_MistDepth);
            float4 _MistParams, _MistColour, _MistFocus;

            struct V { float4 cs : SV_POSITION; };
            V vert(uint id : SV_VertexID) { V o; float2 uv = float2((id << 1) & 2, id & 2); o.cs = float4(uv * 2 - 1, UNITY_NEAR_CLIP_VALUE, 1); return o; }

            float H(float2 p) { p = frac(p * float2(127.1, 311.7)); p += dot(p, p + 34.5); return frac(p.x * p.y); }
            float N(float2 p) { float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f); return lerp(lerp(H(i), H(i + float2(1, 0)), f.x), lerp(H(i + float2(0, 1)), H(i + float2(1, 1)), f.x), f.y); }

            half4 frag(V i) : SV_Target
            {
                float2 uv = GetNormalizedScreenSpaceUV(i.cs);
                float depth = SAMPLE_TEXTURE2D_X(_MistDepth, sampler_MistDepth, uv).r;
            #if UNITY_REVERSED_Z
                if (depth <= 1e-6) return 0; // the sky: no mist
            #else
                if (depth >= 1 - 1e-6) return 0;
            #endif
                float3 p = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float floorY = p.y >= 23.8 ? 24.0 : (p.y >= 9.2 ? 9.43 : 0.0);
                float h = max(p.y - floorY, 0);
                float amount = _MistParams.x, t = _MistParams.y;
                float2 wind = float2(0.35, 0.12) * t;
                float n = N(p.xz * 0.045 + wind * 0.045) * 0.6 + N(p.xz * 0.13 - wind * 0.08) * 0.4;
                float m = exp(-h / _MistParams.z) * smoothstep(0.25, 0.85, n);
                m = h > 0.45 ? min(m, _MistParams.w) : m;
                m += 0.05 * exp(-p.y / 15.0); // the height haze: a little more at street level than on the deck
                m *= smoothstep(2.5, 8.0, distance(p.xz, _MistFocus.xz)); // clear round the focus
                return half4(_MistColour.rgb, saturate(m * amount * 0.85));
            }
            ENDHLSL
        }
    }
}
