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
        // glass: dark, mirror-ish, a faint diagonal sheen
        float sheen = smoothstep(0.35, 0.5, frac((p.x + p.z) * 0.07 + p.y * 0.05)) * 0.18;
        s.albedo = s.albedo * 0.75 + sheen * float3(0.55, 0.75, 0.85);
        s.smoothness = 0.95; s.metallic = 0.35;
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
