// Brief 109 round 4 (tools/juice/author.ts writes this): the light shaders' bodies for Juice_Beam and Juice_Streak (Custom Function, file source).
#ifndef JUICE_LIGHT_INCLUDED
#define JUICE_LIGHT_INCLUDED
float JuiceHash(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
float JuiceNoise(float3 x)
{
    float3 i = floor(x), f = frac(x); f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(lerp(JuiceHash(i), JuiceHash(i + float3(1, 0, 0)), f.x), lerp(JuiceHash(i + float3(0, 1, 0)), JuiceHash(i + float3(1, 1, 0)), f.x), f.y),
                lerp(lerp(JuiceHash(i + float3(0, 0, 1)), JuiceHash(i + float3(1, 0, 1)), f.x), lerp(JuiceHash(i + float3(0, 1, 1)), JuiceHash(i + 1.0), f.x), f.y), f.z);
}
// A volumetric-looking beam on an open cone (UV.y 0 at the lamp, 1 at the far end; normals out from the axis). Params: x the fall-off along the length
// (power), y the soft edge (power of the view angle to the surface: a silhouette fades to nothing, the core stays), z the depth fade (m) where it meets
// geometry, w the drifting dust's strength. The colour is HDR: the play bloom takes the hot core by the lamp.
void JuiceBeam_float(float4 UV, float3 Pos, float3 View, float3 Normal, float4 VColor, float Time, float SceneEye, float4 Tint, float4 Params, out float3 Color, out float Alpha)
{
    float3 V = normalize(View), N = normalize(Normal);
    float rim = pow(saturate(abs(dot(N, V))), Params.y);
    float along = saturate(UV.y);
    float fall = pow(1.0 - along, Params.x) * (1.0 + 2.5 * exp(-along * 28.0)) * smoothstep(0.0, 0.015, along); // brighter by the lamp, a soft start
    float eyeFrag = -TransformWorldToView(Pos).z;
    float fade = saturate((SceneEye - eyeFrag) / max(Params.z, 1e-3)) * saturate((eyeFrag - 0.5) / 2.0); // no hard line where it meets a wall or the ground; none at the lens
    float dust = JuiceNoise(Pos * 0.11 + float3(0.0, -Time * 0.35, Time * 0.21)) * 0.6 + JuiceNoise(Pos * 0.31 + float3(Time * 0.5, 0.0, 0.0)) * 0.4;
    float a = rim * fall * fade * lerp(1.0, dust * 1.6, saturate(Params.w)) * VColor.a;
    Color = Tint.rgb * VColor.rgb * a; Alpha = 1.0;
}
// A light streak on a trail (TrailRenderer, Stretch: UV.x 0 at the head, 1 at the tail; UV.y across). Params: x the taper along (power), y the soft
// edge across (power), z the hot core (how much brighter the centre line is), w unused. The vertex colour is the trail's gradient.
void JuiceStreak_float(float4 UV, float3 Pos, float3 View, float3 Normal, float4 VColor, float Time, float SceneEye, float4 Tint, float4 Params, out float3 Color, out float Alpha)
{
    float across = 1.0 - abs(UV.y * 2.0 - 1.0);
    float core = pow(saturate(across), Params.y), hot = 1.0 + Params.z * pow(saturate(across), 10.0);
    float len = pow(1.0 - saturate(UV.x), Params.x) * smoothstep(0.0, 0.03, UV.x + 0.001);
    float a = core * len * VColor.a;
    Color = Tint.rgb * VColor.rgb * a * hot; Alpha = 1.0;
}
#endif
