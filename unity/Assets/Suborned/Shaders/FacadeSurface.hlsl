// Facade surface classes from the vertex colour: R = ambient occlusion, A = 1 - class/8.
// 0 matte (material default) · 1 limestone (ashlar courses + grain) · 2 enamel (gloss, tile grid) · 3 glass (reflective)
// 4 chrome (metal) · 5 neon (emissive) · 6 brass/gold (warm metal)
#ifndef SUBORNED_FACADE_SURFACE
#define SUBORNED_FACADE_SURFACE

float FcHash(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
float FcNoise(float2 p)
{
    float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
    return lerp(lerp(FcHash(i), FcHash(i + float2(1, 0)), f.x), lerp(FcHash(i + float2(0, 1)), FcHash(i + 1), f.x), f.y);
}

// Wall-plane coordinates: u along the wall, v up (horizontal surfaces: world x, z).
float2 FcPlane(float3 p, float3 n)
{
    float2 h = n.xz; float l = length(h);
    if (l < 0.3) return p.xz;
    h /= l;
    return float2(h.y * p.x - h.x * p.z, p.y);
}

void FacadeSurface(float3 p, float3 n, half4 col, inout SurfaceData s, inout float height)
{
    int cls = (int)round((1.0 - col.a) * 8.0);
    float ao = saturate(col.r);
    float px = length(fwidth(p));                       // metres per pixel
    float2 uv = FcPlane(p, n);
    s.occlusion = lerp(0.35, 1.0, ao);
    s.albedo *= lerp(0.62, 1.0, ao);
    height = 0;
    if (cls == 1)
    {
        // honed limestone: 0.9 m courses, 1.8 m blocks, running bond, tone per block, fine grain
        float row = floor(uv.y / 0.9);
        float shift = frac(row * 0.37) * 1.8;
        float2 g = float2((uv.x + shift) / 1.8, uv.y / 0.9);
        float2 f = frac(g);
        float d = min(min(f.x * 1.8, (1 - f.x) * 1.8), min(f.y * 0.9, (1 - f.y) * 0.9));
        float jw = 0.012;
        float joint = 1 - smoothstep(jw, jw + max(px, 0.004) * 1.5, d);
        float fadeFar = 1 - smoothstep(0.15, 0.5, px);
        float tone = FcHash(floor(g)) - 0.5;
        float grain = FcNoise(uv * 9.0) * 0.6 + FcNoise(uv * 31.0) * 0.4 - 0.5;
        float fine = 1 - smoothstep(0.03, 0.12, px);
        s.albedo *= 1 + tone * 0.07 * fadeFar + grain * 0.07 * fine;
        s.albedo *= 1 - 0.11 * joint * fadeFar;
        height = (-joint * 0.012 * fadeFar + grain * 0.004 * fine);
        s.smoothness = 0.14;
    }
    else if (cls == 2)
    {
        // vitreous enamel: glossy, 12 cm mosaic grid
        float2 g = frac(uv / 0.12);
        float d = min(min(g.x, 1 - g.x), min(g.y, 1 - g.y)) * 0.12;
        float ln = 1 - smoothstep(0.004, 0.004 + max(px, 0.003), d);
        float fade = 1 - smoothstep(0.02, 0.09, px);
        s.albedo *= 1 - 0.05 * ln * fade;
        s.smoothness = 0.78; s.metallic = 0.0;
        height = -ln * 0.004 * fade;
    }
    else if (cls == 3)
    {
        // glass (Vela, brief 048): never a black void. The pane is lit by what it would show, not by the reflection probe (which is the dark street canyon at the horizon
        // and read black from third person): a fake sky gradient in the mirror term (R.y up = sky, down = the warm street), a per-pane room behind it (dark teal glazing from the
        // swatch, or a warm lit room, or a pale blind) and a fresnel weight that grows as the view grazes. No probe, no extra pass: the sky is the scene's trilight ambient
        // (light.json ambient, so night follows), tinted blue; a few warm rooms glow as the ambient falls.
        float3 V = GetWorldSpaceNormalizeViewDir(p);
        float ndv = saturate(dot(n, V));
        float3 R = reflect(-V, n);
        float lum = dot(unity_AmbientEquator.rgb, float3(0.3, 0.59, 0.11));
        float day = saturate(lum * 1.45);                                   // 1 by day (ambient 0.7), ~0.25 at night
        float up = smoothstep(-0.2, 0.7, R.y);
        float3 skyHorizon = float3(0.60, 0.74, 0.77), skyZenith = float3(0.18, 0.43, 0.70), street = float3(0.30, 0.24, 0.19);
        float3 sky = lerp(skyHorizon, skyZenith, smoothstep(0.1, 0.9, R.y));
        // eye-height view along a street (brief 048 fix 2): the reflection runs near the horizon (R.y about -0.1), so a band of the hazy horizon is mixed in; under 1 percent at the play camera
        float glow = exp(-(R.y + 0.1) * (R.y + 0.1) / 0.045) * 0.85;
        float3 mirror = lerp(lerp(street, sky, up), skyHorizon * 1.2, glow) * day;
        // the room behind the pane: one per 2 m x 3.6 m cell of the wall plane
        float h = FcHash(floor(float2(uv.x / 2.0, uv.y / 3.6)) + 7.0);
        float3 teal = s.albedo;                                             // the swatch: the kit window's dark teal
        float3 warm = float3(0.50, 0.33, 0.20), blind = float3(0.34, 0.36, 0.32);
        float3 room = h > 0.68 ? warm : (h < 0.16 ? blind : teal * 1.25);
        float fres = 0.30 + 0.62 * pow(1 - ndv, 3.0);
        float sheen = smoothstep(0.35, 0.5, frac((p.x + p.z) * 0.07 + p.y * 0.05)) * 0.18;
        s.albedo = room;
        s.metallic = 0.0; s.smoothness = 0.9;
        s.emission = (mirror * fres * 0.9 + room * 0.55 * day + sheen * float3(0.55, 0.75, 0.85) * day * 0.25) * lerp(0.6, 1.0, ao)
                   + (h > 0.68 ? warm * (0.10 + 0.9 * (1 - day)) * (1 - fres) : 0.0);
    }
    else if (cls == 4) { s.metallic = 1.0; s.smoothness = 0.82; s.albedo *= 0.95; }
    else if (cls == 6) { s.metallic = 1.0; s.smoothness = 0.7; }
    else if (cls == 5) { s.emission = s.albedo * 1.7; s.albedo *= 0.5; s.smoothness = 0.5; }
}

float3 FacadeBumpNormal(float3 p, float3 n, float h)
{
    float3 dpdx = ddx(p), dpdy = ddy(p);
    float dhx = ddx(h), dhy = ddy(h);
    float3 r1 = cross(dpdy, n), r2 = cross(n, dpdx);
    float det = dot(dpdx, r1);
    float3 grad = sign(det) * (dhx * r1 + dhy * r2);
    return normalize(abs(det) * n - grad);
}
#endif
