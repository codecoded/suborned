// Brief 107 (Latch; Glint's rain): the falling rain (mode 0), the splash rings (1) and the lamps' reflections in the wet (2). Rain107.cs builds the meshes:
// uv = the quad's corner (-1..1), uv2 = four seeds (mode 2: uv2.xyz is the lamp's head). _RainCenter = the focus (xyz) and the box (w, m);
// _RainParams = rain density, streaks, the unscaled clock, night. Drops are world-anchored: each wraps in the box round the focus.
Shader "Suborned/Rain107"
{
    Properties
    {
        _Mode("Mode", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Rain"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _RainCenter, _RainParams; float _Mode;
            struct A { float3 pos : POSITION; float2 uv : TEXCOORD0; float4 seed : TEXCOORD1; };
            struct V { float4 cs : SV_POSITION; float2 uv : TEXCOORD0; half a : TEXCOORD1; half3 col : TEXCOORD2; };

            float Floor(float y) { return y >= 24.0 ? 24.0 : (y >= 9.43 ? 9.43 : 0.0); }
            float2 Wrap(float2 seed, float box) { float2 c = _RainCenter.xz; return c + (frac(seed - c / box) - 0.5) * box; }

            V vert(A i)
            {
                V o; o.uv = i.uv; o.a = 0; o.col = half3(1, 1, 1);
                float t = _RainParams.z, box = _RainCenter.w;
                float3 camFwd = -UNITY_MATRIX_V[2].xyz;
                float3 wp;
                if (_Mode < 0.5)
                {   // a streak: 0.8 m long, 2.6 cm wide, falling at 9 m/s, a slight lean down-wind, turned to face the camera about the vertical
                    if (i.seed.w > _RainParams.x) { o.cs = float4(0, 0, -2, 1); return o; }
                    float2 xz = Wrap(i.seed.xy, box);
                    float top = _RainCenter.y + 16.0, span = 22.0;
                    float y = top - frac(i.seed.z + t * 9.0 / span) * span;
                    float floorY = Floor(y + 0.3);
                    float3 right = normalize(cross(float3(0, 1, 0), camFwd)); if (any(isnan(right))) right = float3(1, 0, 0);
                    float3 dir = normalize(float3(0.12, -1, 0.05));
                    float along = (i.uv.y * 0.5 + 0.5) * 0.8;
                    wp = float3(xz.x, max(y, floorY), xz.y) - dir * along + right * i.uv.x * 0.013;
                    o.a = y > floorY ? 0.38 : 0;
                    o.col = lerp(half3(0.86, 0.9, 0.95), half3(0.55, 0.62, 0.7), _RainParams.w);
                }
                else if (_Mode < 1.5)
                {   // a splash: a ring on the street that grows and fades in 0.35 s, each on its own phase and place
                    if (i.seed.w > _RainParams.x) { o.cs = float4(0, 0, -2, 1); return o; }
                    float cycle = t * 2.857 + i.seed.z; float n = floor(cycle), ph = frac(cycle);
                    float2 xz = Wrap(frac(i.seed.xy + n * float2(0.618, 0.382)), box);
                    float r = 0.04 + ph * 0.16;
                    wp = float3(xz.x, 0.02, xz.y) + float3(i.uv.x, 0, i.uv.y) * r;
                    o.a = (1 - ph) * 0.35;
                    o.col = lerp(half3(0.92, 0.95, 1.0), half3(0.6, 0.66, 0.74), _RainParams.w);
                }
                else
                {   // a lamp's reflection: a soft smear from under the lamp toward the camera, 0.5 m wide, 3.2 m long, warm cream
                    float3 head = i.seed.xyz; float floorY = Floor(head.y - 1.0);
                    float2 toCam = -camFwd.xz; float l = length(toCam); toCam = l > 1e-3 ? toCam / l : float2(0, -1);
                    float2 side = float2(-toCam.y, toCam.x);
                    float2 xz = head.xz + toCam * (i.uv.y * 0.5 + 0.5) * 3.2 + side * i.uv.x * 0.25;
                    wp = float3(xz.x, floorY + 0.015, xz.y);
                    o.a = _RainParams.y * lerp(0.16, 0.42, _RainParams.w);
                    o.col = half3(1.0, 0.86, 0.66);
                }
                o.cs = TransformWorldToHClip(wp);
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                half a = i.a;
                if (_Mode < 0.5) a *= saturate(1 - abs(i.uv.x)) * saturate(i.uv.y * 0.5 + 0.5);
                else if (_Mode < 1.5) { float d = length(i.uv); a *= saturate(1 - abs(d - 0.8) / 0.2); }
                else { a *= saturate(1 - abs(i.uv.x)) * (1 - smoothstep(0.0, 1.0, i.uv.y * 0.5 + 0.5)) * smoothstep(-1.0, -0.6, i.uv.y); }
                return half4(i.col, a);
            }
            ENDHLSL
        }
    }
}
