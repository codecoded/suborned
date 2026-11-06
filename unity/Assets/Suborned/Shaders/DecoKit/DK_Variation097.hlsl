// Brief 097 (Redline's render audit item 4; the concept stills paid/097-look-bar): the painted variation. One shared tiling wash (Resources/Fx097/
// variation.png, made by Editor Fx097Assets: a soft sun-faded mottle about 1.0, stored / 1.1) multiplied into the albedo in world space, so no stone,
// enamel or paving face reads as one flat colour. The projection is the face's dominant axis (one fetch, no blend): the ground on xz, walls on zy
// or xy. Globals from Suborned.Look097: _DKVariationParams097.x = 1 / the tile (m), .y = strength (0 = off: -novariation, or no texture set).
#ifndef DK_VARIATION097_INCLUDED
#define DK_VARIATION097_INCLUDED
TEXTURE2D(_DKVariation097); SAMPLER(sampler_DKVariation097);
float4 _DKVariationParams097;
half DKVariation(float3 pWS, half3 nWS)
{
    UNITY_BRANCH if (_DKVariationParams097.y <= 0.0) return half(1.0);
    half3 a = abs(nWS);
    float2 uv = a.y > max(a.x, a.z) ? pWS.xz : (a.x > a.z ? pWS.zy : pWS.xy);
    half v = SAMPLE_TEXTURE2D(_DKVariation097, sampler_DKVariation097, uv * _DKVariationParams097.x).r * half(1.1);
    return lerp(half(1.0), v, half(_DKVariationParams097.y));
}
#endif
