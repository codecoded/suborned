// Sky gradient for the Harbour District (Glint): pale warm horizon to soft blue zenith, a warm sun glow, and below the
// horizon the haze colour so the sea edge and far band dissolve into one colour. Every colour is set from light.json.
//
// Atmosphere (brief 048, AtmosphereLens.cs): the _Atm* inputs below are SHADER GLOBALS set per camera by AtmosphereLens; all zero (the default, and
// every camera of the play tier) is exactly the sky as built. When set, for the perspective tiers by day:
//   _AtmHaze        rgb the haze colour (linear), a 1: replaces _Horizon and _Below, so the sky horizon is the fog's colour (haze.json)
//   _AtmSkyZenith / _AtmSkyMid   rgb, a 1: replace _Zenith / _Mid (the old teal; a 0 keeps the scene's cobalt)
//   _AtmSkyShape    x hazeHeight (> 0 turns the new shape on), y aureole strength, z aureole power, w haze lift toward the sun
//   _AtmSkyExtra    x broad lobe strength, y broad lobe power
//   _AtmFarBand     the baked far-city strip (equirect, RGB ratio to the horizon haze, A cover), _AtmFarBandRange x/y min/max elevation (radians),
//                   z gain, w opacity (0 = no band: nothing is sampled)
// The new shape is the old city's DK_SpaceSky: haze weight exp(-sin(elevation) / hazeHeight) over the sky body, the haze brightening toward the
// sun, an aureole lobe and a broad lobe added in the sun glow colour.
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
            // globals set by AtmosphereLens (zero when it is absent or the camera is not in a perspective tier)
            float4 _AtmHaze, _AtmSkyZenith, _AtmSkyMid, _AtmSkyShape, _AtmSkyExtra, _AtmFarBandRange;
            TEXTURE2D(_AtmFarBand); SAMPLER(sampler_AtmFarBand);
            struct A { float4 pos : POSITION; };
            struct V { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            V vert(A a) { V v; v.pos = TransformObjectToHClip(a.pos.xyz); v.dir = a.pos.xyz; return v; }
            half4 frag(V v) : SV_Target
            {
                float3 d = normalize(v.dir);
                float y = d.y;
                float3 sun = normalize(_SunDir.xyz);
                float g = saturate(dot(d, sun));
                bool hazeOn = _AtmHaze.a > 0.5;
                bool shapeOn = _AtmSkyShape.x > 0;
                half3 horizon = hazeOn ? (half3)_AtmHaze.rgb : _Horizon.rgb;
                half3 below = hazeOn ? (half3)_AtmHaze.rgb : _Below.rgb;
                half3 mid = _AtmSkyMid.a > 0.5 ? (half3)_AtmSkyMid.rgb : _Mid.rgb;
                half3 zenith = _AtmSkyZenith.a > 0.5 ? (half3)_AtmSkyZenith.rgb : _Zenith.rgb;
                half3 c;
                if (y >= 0)
                {
                    float z = pow(saturate((y - _MidHeight) / (1 - _MidHeight)), _Curve);
                    if (shapeOn)
                    {
                        // the old city's air: sky body (mid to zenith), the horizon haze over it by exp(-h / hazeHeight), the haze lifted toward the sun
                        half3 body = lerp(mid, zenith, z);
                        half3 lifted = horizon * (1 + _AtmSkyShape.w * pow(g, 6));
                        c = lerp(body, lifted, exp(-y / _AtmSkyShape.x));
                    }
                    else
                    {
                        float t = saturate(y / _MidHeight);
                        c = lerp(horizon, mid, smoothstep(0, 1, t));
                        c = lerp(c, zenith, z);
                    }
                }
                else c = lerp(horizon, below, saturate(-y * 12));
                float glow = shapeOn
                    ? _AtmSkyShape.y * pow(g, _AtmSkyShape.z) + _AtmSkyExtra.x * pow(g, _AtmSkyExtra.y)
                    : pow(g, _Glow.x) * _Glow.y + pow(g, _Glow.x * 8) * _Glow.y;
                float disc = smoothstep(0.9995, 0.9999, g) * _Glow.z;
                c += _SunColour.rgb * (glow * (y >= 0 ? 1.0 : 0.5) + disc);
                if (_AtmFarBandRange.w > 0)
                {
                    // the baked far-city band: painted as a ratio to the horizon haze, so it takes the haze of its own azimuth (lifted toward the sun)
                    float t = (asin(clamp(y, -1, 1)) - _AtmFarBandRange.x) / (_AtmFarBandRange.y - _AtmFarBandRange.x);
                    if (t > 0 && t < 1)
                    {
                        float3 flatDir = normalize(float3(d.x, 0, d.z) + float3(0, 1e-5, 0));
                        half3 hz = horizon * (1 + (shapeOn ? _AtmSkyShape.w : 0.0) * pow(saturate(dot(flatDir, sun)), 6));
                        half4 city = SAMPLE_TEXTURE2D_LOD(_AtmFarBand, sampler_AtmFarBand, float2(atan2(d.x, d.z) / 6.2831853 + 0.5, t), 1.0);
                        c = lerp(c, hz * city.rgb * _AtmFarBandRange.z, city.a * _AtmFarBandRange.w);
                    }
                }
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
