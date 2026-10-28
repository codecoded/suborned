// PORTED (Glint, brief 075-A, 2026-10-28) from ~/Projects/the-suborned/game/Assets/TheSuborned/Art/DecoKit/Shaders/DK_SpaceSky.shader
// (sha256 f38bcd08...). The old reel's day sky, verbatim; the builder sets it from light.json `spaceSky` (the old DK_Sky_Street.mat's stored
// values, set the way DecoKitSky.Apply set them). Our only additions, all inert when zero (AtmosphereLens leaves them zero by day):
//   _AtmSunDir (xyz, w 1) / _AtmSunCol (rgb, w 1): the evening sun's direction and glare colour replace _SunDir / _SunColor;
//   _AtmSkyZenith / _AtmSkyMid (rgb, a 1): the evening sky's zenith and mid replace _Zenith / _Mid (AtmosphereLens blends them from this
//   material's own, read as _Zenith / _Mid / _SunColor).
// The sky bodies (planet, moons) are drawn by the horizon band as geometry (brief 007 E, 072); light.json sets their radii to 0 here, so
// RaySphere never hits and they are not drawn twice.
// Space-age daytime sky (target: Concepts/Horizon/sky_ab/celestial.png), one pass, back to front:
//   space   sun disc, a ray-traced ringed gas giant and two moons, lit by the real sun direction so phases follow it
//   air     cobalt zenith -> cerulean -> warm horizon haze, a sun aureole that also lifts the haze toward the sun;
//           space light is attenuated by airmass and added over it (daylight fade: night sides vanish into the blue)
//   city    a 360-degree far-city matte band (DK_Skyline_Far) tinted by the horizon haze of its own azimuth, so it
//           brightens toward the sun and dissolves into the same colour as the fog (aerial perspective)
//   cirrus  wind-stretched ridged noise on a sky plane, forward-scattering near the sun, thinning into the haze
// Below the horizon the haze colour continues: DecoKitSky sets the scene fog to _Horizon so geometry meets it.
Shader "DecoKit/Space Age Sky"
{
    Properties
    {
        [HDR] _Zenith("Zenith", Color) = (0.01, 0.03, 0.1, 1)
        [HDR] _Mid("Mid sky", Color) = (0.02, 0.15, 0.35, 1)
        [HDR] _Horizon("Horizon haze", Color) = (0.9, 0.78, 0.55, 1)
        _MidHeight("Zenith blend height (sin elevation)", Range(0.05, 1)) = 0.55
        _HazeHeight("Haze scale height (sin elevation)", Range(0.005, 0.5)) = 0.07
        _SunDir("Direction to the sun", Vector) = (0.3, 0.5, 0.8, 0)
        [HDR] _SunColor("Sun colour", Color) = (1, 0.95, 0.85, 1)
        _SunRadius("Sun angular radius (rad)", Float) = 0.012
        _SunDisc("Sun disc intensity", Float) = 60
        _Aureole("Aureole: x strength, y power, z haze lift, w broad strength", Vector) = (0.8, 180, 0.6, 0.15)
        _NoiseTex("Sky noise (R lo, G mid, B hi, A ridged)", 2D) = "gray" {}
        [HDR] _CirrusColor("Cirrus colour", Color) = (1, 1, 1, 1)
        _Cirrus("Cirrus: x cover, y scale, z stretch, w angle (rad)", Vector) = (0.45, 0.35, 5, 0.6)
        _CirrusWind("Cirrus wind (uv per s)", Vector) = (0.002, 0.0005, 0, 0)
        _PlanetTex("Planet bands (lat-long)", 2D) = "white" {}
        _RingTex("Ring profile (inner -> outer, A density)", 2D) = "white" {}
        _MoonTex("Moon albedo (lat-long)", 2D) = "white" {}
        _Planet("Planet: xyz direction, w sin(angular radius)", Vector) = (-0.4, 0.35, 0.85, 0.14)
        _PlanetAxis("Planet axis: xyz, w spin (rad)", Vector) = (0.35, 0.9, -0.2, 0)
        _Ring("Ring: x inner, y outer (radii), z opacity, w brightness", Vector) = (1.3, 2.3, 0.9, 1)
        _Moon1("Moon 1: xyz direction, w sin(angular radius)", Vector) = (-0.6, 0.6, 0.5, 0.03)
        _Moon2("Moon 2: xyz direction, w sin(angular radius)", Vector) = (0.5, 0.3, 0.8, 0.02)
        _SkylineTex("Far city band (RGB / haze, A cover)", 2D) = "clear" {}
        _SkylineBand("Far city: x bottom, y top elevation (rad), z gain, w opacity", Vector) = (-0.01745, 0.157, 1.1, 1)
        _Body("Bodies: x gain, y sky occlusion, z extinction, w rim glow", Vector) = (0.6, 0.12, 0.25, 0.6)
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"  // Luminance

            TEXTURE2D(_NoiseTex);  SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_PlanetTex); SAMPLER(sampler_PlanetTex);
            TEXTURE2D(_RingTex);   SAMPLER(sampler_RingTex);
            TEXTURE2D(_MoonTex);   SAMPLER(sampler_MoonTex);
            TEXTURE2D(_SkylineTex); SAMPLER(sampler_SkylineTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Zenith, _Mid, _Horizon, _SunColor, _CirrusColor;
                float _MidHeight, _HazeHeight, _SunRadius, _SunDisc;
                float4 _SunDir, _Aureole, _Cirrus, _CirrusWind;
                float4 _NoiseTex_ST, _PlanetTex_ST, _RingTex_ST, _MoonTex_ST, _PlanetTex_TexelSize, _MoonTex_TexelSize;
                float4 _Planet, _PlanetAxis, _Ring, _Moon1, _Moon2, _Body;
                float4 _SkylineTex_ST, _SkylineTex_TexelSize, _SkylineBand;
            CBUFFER_END
            float4 _AtmSunDir, _AtmSunCol, _AtmSkyZenith, _AtmSkyMid; // AtmosphereLens globals (brief 075-A port); zero = the material's own
            float3 SunCol() { return _AtmSunCol.w > 0.5 ? _AtmSunCol.rgb : _SunColor.rgb; }

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.dir = i.positionOS.xyz;
                return o;
            }

            // unit-distance sphere at direction c, radius r (= sin angular radius): nearest hit distance, or -1
            float RaySphere(float3 v, float3 c, float r)
            {
                float b = dot(v, c), d = b * b - (1 - r * r);
                return d > 0 ? b - sqrt(d) : -1;
            }

            // lat-long uv of normal n in a body frame with the given axis and spin; lod from on-screen size
            float2 BodyUV(float3 n, float3 c, float4 axis)
            {
                float3 e1 = normalize(cross(axis.xyz, c)), e2 = cross(axis.xyz, e1);
                float lon = atan2(dot(n, e2), dot(n, e1)) + axis.w, lat = asin(clamp(dot(n, axis.xyz), -1, 1));
                return float2(lon / TWO_PI + .5, lat / PI + .5);
            }

            float BodyLod(float texWidth, float r, float pixelAngle)
            {
                return max(0, log2(texWidth * .5 * pixelAngle / (2 * r)));
            }

            float3 Air(float3 v, float3 sun)
            {
                float h = max(v.y, 0);
                float3 zen = _AtmSkyZenith.a > 0.5 ? _AtmSkyZenith.rgb : _Zenith.rgb, mid = _AtmSkyMid.a > 0.5 ? _AtmSkyMid.rgb : _Mid.rgb;
                float3 upper = lerp(mid, zen, pow(saturate(h / _MidHeight), 1.5));
                float haze = exp(-h / _HazeHeight);
                float mu = saturate(dot(v, sun));
                // haze brightens toward the sun (forward scattering): the concept's pale right-hand sky
                float3 horizon = _Horizon.rgb * (1 + _Aureole.z * pow(mu, 6));
                float3 col = lerp(upper, horizon, haze);
                col += SunCol() * (_Aureole.x * pow(mu, _Aureole.y) + _Aureole.w * pow(mu, 8));
                return v.y >= 0 ? col : _Horizon.rgb;
            }

            float Cirrus(float3 v)
            {
                if (v.y < .015) return 0;
                float2 p = v.xz / (v.y + .12) * _Cirrus.y;
                float s = sin(_Cirrus.w), c = cos(_Cirrus.w);
                p = float2(c * p.x - s * p.y, s * p.x + c * p.y);
                float2 uv = float2(p.x / _Cirrus.z, p.y) + _CirrusWind.xy * _Time.y;
                float lo = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * .35).r;
                float2 warp = (SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * .9).gb - .5) * 1.8;  // feathery, not ruled
                float streak = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * float2(.6, 2.4) + warp * .35).a;
                float fine = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * float2(2, 7) + warp).b;
                float d = saturate((lo - (1 - _Cirrus.x)) * 3) * streak * (.6 + .8 * fine);
                return saturate(d) * smoothstep(.015, .2, v.y) * (1 - smoothstep(.75, 1, v.y));
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 v = normalize(i.dir), sun = normalize(_AtmSunDir.w > 0.5 ? _AtmSunDir.xyz : _SunDir.xyz);
                float pa = length(fwidth(v));  // radians per pixel
                float above = smoothstep(-.004, .01, v.y);

                // --- space: sun, planet + rings, moons (light added over the air, sky partly occluded)
                float cover = 0;
                float3 sunLight = SunCol() * _SunDisc * (1 - smoothstep(_SunRadius * .85, _SunRadius * 1.15, length(v - sun)));

                float3 pc = normalize(_Planet.xyz), ax = normalize(_PlanetAxis.xyz);
                float pr = _Planet.w;
                float tp = RaySphere(v, pc, pr);
                float3 planet = 0;
                float planetCov = 0;
                if (tp > 0)
                {
                    float3 n = (tp * v - pc) / pr;
                    float2 uv = BodyUV(n, pc, float4(ax, _PlanetAxis.w));
                    float3 alb = SAMPLE_TEXTURE2D_LOD(_PlanetTex, sampler_PlanetTex, uv,
                                                      BodyLod(_PlanetTex_TexelSize.z, pr, pa)).rgb;
                    alb = lerp(dot(alb, 1.0 / 3), alb, .7);                    // airbrush: soft, low-contrast bands
                    float ndl = dot(n, sun), mu = saturate(dot(n, -v));
                    float lit = saturate((ndl + .06) / 1.06);
                    lit *= lerp(.55, 1, pow(mu, .35));                       // gas-giant limb darkening
                    // ring shadow on the planet: march from the surface to the sun through the ring plane
                    float3 p = tp * v;
                    float tr = dot(pc - p, ax) / dot(sun, ax);
                    if (tr > 0)
                    {
                        float d = length(p + tr * sun - pc) / pr;
                        float u = (d - _Ring.x) / (_Ring.y - _Ring.x);
                        if (u > 0 && u < 1) lit *= 1 - .75 * _Ring.z * SAMPLE_TEXTURE2D_LOD(_RingTex, sampler_RingTex, float2(u, .5), 0).a;
                    }
                    float rim = pow(1 - mu, 3) * saturate(ndl + .25) * _Body.w;  // hazy lit limb
                    planet = alb * lit + rim * float3(.85, .9, 1);
                    planetCov = smoothstep(pr, pr * .96, length(v - pc * dot(v, pc)));  // anti-aliased edge
                }
                // ring: plane through the planet centre, normal = axis
                float dv = dot(v, ax);
                float ringA = 0;
                float3 ring = 0;
                if (abs(dv) > 1e-4)
                {
                    float t = dot(pc, ax) / dv;
                    float d = length(t * v - pc) / pr;
                    float u = (d - _Ring.x) / (_Ring.y - _Ring.x);
                    if (t > 0 && u > 0 && u < 1)
                    {
                        float4 rt = SAMPLE_TEXTURE2D_LOD(_RingTex, sampler_RingTex, float2(u, .5), 0);
                        float3 oc = t * v - pc;                                  // planet shadow on the ring
                        float b = dot(oc, sun), disc = b * b - (dot(oc, oc) - pr * pr);
                        float shade = (disc > 0 && -b > 0) ? smoothstep(pr * pr * .02, 0, disc) * .9 + .1 : 1;
                        ring = rt.rgb * _Ring.w * (.3 + .7 * pow(abs(dot(sun, ax)), .5)) * shade;
                        ringA = rt.a * _Ring.z;
                        if (tp > 0 && t > tp) ringA *= 0;                          // behind the planet
                    }
                }
                float3 body = lerp(planet * planetCov, ring, ringA);
                cover = max(planetCov, ringA);

                [unroll] for (int k = 0; k < 2; k++)
                {
                    float4 m = k == 0 ? _Moon1 : _Moon2;
                    float3 mc = normalize(m.xyz);
                    float tm = RaySphere(v, mc, m.w);
                    if (tm > 0)  // moons are nearer than the planet: always in front
                    {
                        float3 n = (tm * v - mc) / m.w;
                        float2 uv = BodyUV(n, mc, float4(0, 1, 0, k * 2.1));
                        float alb = SAMPLE_TEXTURE2D_LOD(_MoonTex, sampler_MoonTex, uv, BodyLod(_MoonTex_TexelSize.z, m.w, pa)).r;
                        float edge = smoothstep(m.w, m.w * .9, length(v - mc * dot(v, mc)));
                        body = lerp(body, alb * saturate(dot(n, sun)) * 1.15, edge);
                        cover = max(cover, edge);
                    }
                }
                cover *= above;

                // --- air: attenuate space light by airmass, add the in-scatter
                float airmass = 1 / (saturate(v.y) + .1) - .9;
                float T = exp(-_Body.z * airmass) * above;
                float3 air = Air(v, sun);
                // airbrush daylight: a lit face paints over the sky (cream, not cream + blue); a night side stays transparent
                float3 light = body * SunCol() * _Body.x * T;
                float paint = saturate(_Body.y + Luminance(light) / max(Luminance(air), 1e-3)) * cover;
                float3 col = air * (1 - paint) + light + sunLight * (1 - cover) * T;

                // --- far city: painted in the haze of the horizon below it; lod from the pixel's angular size
                float t = (asin(clamp(v.y, -1, 1)) - _SkylineBand.x) / (_SkylineBand.y - _SkylineBand.x);
                if (t > 0 && t < 1)
                {
                    float2 uv = float2(atan2(v.x, v.z) / TWO_PI + .5, t);
                    float lod = max(0, log2(_SkylineTex_TexelSize.z * pa / TWO_PI));
                    float4 city = SAMPLE_TEXTURE2D_LOD(_SkylineTex, sampler_SkylineTex, uv, lod);
                    float3 haze = Air(normalize(float3(v.x, 0, v.z) + float3(0, 1e-5, 0)), sun);
                    col = lerp(col, haze * city.rgb * _SkylineBand.z, city.a * _SkylineBand.w);
                }

                // --- cirrus in front of everything above the haze
                float c = Cirrus(v);
                float mu = saturate(dot(v, sun));
                float3 cirrus = _CirrusColor.rgb * (.75 + .35 * v.y + 1.5 * pow(mu, 12));
                col = lerp(col, cirrus, c * _CirrusColor.a);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
