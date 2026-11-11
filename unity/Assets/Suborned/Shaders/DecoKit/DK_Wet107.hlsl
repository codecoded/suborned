// Brief 107 (Glint's rain, briefs/107/glint/weather.html; Sky: look only): wet ground and puddles from one global, no keyword, no texture.
// _SubornedWet: x = wetness 0..1 (non-metal surfaces darken about 25 % and gain gloss; metal only beads), y = puddles 0..1 (a world-space noise mask on
// flat ground at the plan's levels: street 0, deck 9.43, concourse 24; planters and roofs never collect), z = the ripple clock (s), w = ripples 0..1.
// Zero (as built, and every dry day) returns before any work. Weather107.cs sets it.
#ifndef DK_WET107_INCLUDED
#define DK_WET107_INCLUDED

float4 _SubornedWet;
float4 _SubornedNight; // x = the share of indirect light taken away at night (the zone's lightmaps hold the day's bounce; ambient and reflections too); 0 by day, unset = 0

float DKWetHash(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
float DKWetNoise(float2 p)
{
    float2 i = floor(p), f = frac(p); f = f * f * (3.0 - 2.0 * f);
    float a = DKWetHash(i), b = DKWetHash(i + float2(1, 0)), c = DKWetHash(i + float2(0, 1)), d = DKWetHash(i + float2(1, 1));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

/// The ground's floor nearest below a height, and whether the point lies on it (within a kerb's height).
half DKWetOnFloor(float y) { float d = min(min(abs(y), abs(y - 9.43)), abs(y - 24.0)); return (half)(1.0 - smoothstep(0.22, 0.32, d)); }

/// The world position of this pixel (any projection, the ortho play camera too).
float3 DKWetPos(float4 positionCS) { return ComputeWorldSpacePosition(GetNormalizedScreenSpaceUV(positionCS), positionCS.z, UNITY_MATRIX_I_VP); }

void DKWet(inout SurfaceData s, float3 posWS, half3 nWS)
{
    s.occlusion *= half(1.0) - (half)_SubornedNight.x; // night: the moon and the lamps are direct light and stay; the day's bounce goes
    half wet = (half)_SubornedWet.x;
    if (wet <= 0.0h) return;
    half up = saturate((nWS.y - 0.2h) / 0.6h);                      // walls take less than the ground
    half porous = 1.0h - s.metallic; // smoothness can't tell paving from enamel here (DK_Pave_Civic's is 1): metal beads, the rest darkens
    half k = wet * lerp(0.35h, 1.0h, up) * porous;
    s.albedo *= lerp(1.0h, 0.72h, k);
    s.smoothness = lerp(s.smoothness, max(s.smoothness, 0.8h), wet * lerp(0.3h, 1.0h, up) * porous);
    half pud = (half)_SubornedWet.y;
    if (pud <= 0.0h || nWS.y < 0.96h) return;
    float n = DKWetNoise(posWS.xz * 0.11) * 0.65 + DKWetNoise(posWS.xz * 0.37 + 17.0) * 0.35;
    float edge = 1.0 - pud * 0.42;
    half p = (half)smoothstep(edge, edge + 0.07, n) * DKWetOnFloor(posWS.y);
    if (p <= 0.0h) return;
    s.albedo *= lerp(1.0h, 0.55h, p);
    s.smoothness = lerp(s.smoothness, 0.96h, p);
    s.occlusion = lerp(s.occlusion, 1.0h, p * 0.5h);
    half3 flat = half3(0.0h, 0.0h, 1.0h);
    half rip = (half)_SubornedWet.w;
    if (rip > 0.0h)
    {   // rings in the puddles: one drop a 0.8 m cell, each on its own phase
        float2 cell = floor(posWS.xz / 0.8), c = (cell + 0.25 + 0.5 * float2(DKWetHash(cell), DKWetHash(cell + 7.1))) * 0.8;
        float ph = frac(_SubornedWet.z * 1.1 + DKWetHash(cell + 3.3)), r = ph * 0.35, d = distance(posWS.xz, c);
        float ring = (1.0 - smoothstep(0.0, 0.03, abs(d - r))) * (1.0 - ph);
        float2 dir = d > 1e-4 ? (posWS.xz - c) / d : float2(0, 0);
        flat.xy = (half2)(dir * ring * 0.35 * rip);
    }
    s.normalTS = normalize(lerp(s.normalTS, flat, p));
}

#endif
