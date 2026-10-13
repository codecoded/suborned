// Sky gradient for the Harbour District (Glint): pale warm horizon to soft blue zenith, a warm sun glow, and below the
// horizon the haze colour so the sea edge and far band dissolve into one colour. Every colour is set from light.json.
Shader "Suborned/SkyGradient"
{
    Properties
    {
        _Zenith ("Zenith", Color) = (0.5, 0.7, 0.9, 1)
        _Mid ("Mid", Color) = (0.7, 0.8, 0.9, 1)
        _Horizon ("Horizon", Color) = (0.95, 0.88, 0.75, 1)
        _Below ("Below horizon", Color) = (0.95, 0.88, 0.75, 1)
        _SunColour ("Sun glow", Color) = (1, 0.85, 0.6, 1)
        _SunDir ("Sun direction", Vector) = (0, 0.3, 1, 0)
        _MidHeight ("Mid height", Float) = 0.18
        _Curve ("Zenith curve", Float) = 0.6
        _Glow ("Glow width, strength, disc", Vector) = (6, 0.6, 0.0, 0)
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Zenith, _Mid, _Horizon, _Below, _SunColour;
                float4 _SunDir, _Glow;
                float _MidHeight, _Curve;
            CBUFFER_END
            struct A { float4 pos : POSITION; };
            struct V { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            V vert(A a) { V v; v.pos = TransformObjectToHClip(a.pos.xyz); v.dir = a.pos.xyz; return v; }
            half4 frag(V v) : SV_Target
            {
                float3 d = normalize(v.dir);
                float y = d.y;
                half3 c;
                if (y >= 0)
                {
                    float t = saturate(y / _MidHeight);
                    c = lerp(_Horizon.rgb, _Mid.rgb, smoothstep(0, 1, t));
                    float z = pow(saturate((y - _MidHeight) / (1 - _MidHeight)), _Curve);
                    c = lerp(c, _Zenith.rgb, z);
                }
                else c = lerp(_Horizon.rgb, _Below.rgb, saturate(-y * 12));
                float g = saturate(dot(d, normalize(_SunDir.xyz)));
                float glow = pow(g, _Glow.x) * _Glow.y + pow(g, _Glow.x * 8) * _Glow.y;
                float disc = smoothstep(0.9995, 0.9999, g) * _Glow.z;
                c += _SunColour.rgb * (glow * (y >= 0 ? 1 : 0.5) + disc);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
