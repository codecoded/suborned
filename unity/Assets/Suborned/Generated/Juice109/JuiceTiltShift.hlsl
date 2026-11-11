// Brief 109 moment 8 (tools/juice/author.ts writes this): the tilt-shift band blur for Juice_TiltShift.shadergraph (Custom Function, file source).
// Sixteen taps on a golden-angle spiral, their radius growing from 0 at the sharp band's edge to Blur px at the screen's top and bottom.
#ifndef JUICE_TILTSHIFT_INCLUDED
#define JUICE_TILTSHIFT_INCLUDED
#ifndef SHADERGRAPH_PREVIEW
TEXTURE2D_X(_BlitTexture);
#endif
void TiltShift_float(float4 UV, float Band, float Blur, out float3 Out)
{
#ifdef SHADERGRAPH_PREVIEW
    Out = float3(UV.xy, 0);
#else
    float2 uv = UV.xy; float d = abs(uv.y - 0.5);
    float k = saturate((d - Band) / max(1e-4, 0.5 - Band)); k = k * k * (3 - 2 * k);
    float r = Blur * k * (_ScreenSize.y / 1080.0);
    float2 px = uv * _ScreenSize.xy; float3 acc = 0; float wsum = 0;
    [unroll] for (int i = 0; i < 16; i++)
    {
        float t = (i + 0.5) / 16.0, a = i * 2.39996;
        float2 o = float2(cos(a), sin(a)) * sqrt(t) * r; float w = 1.0 - 0.5 * t;
        uint2 q = (uint2)clamp(px + o, float2(0, 0), _ScreenSize.xy - 1);
        acc += LOAD_TEXTURE2D_X_LOD(_BlitTexture, q, 0).rgb * w; wsum += w;
    }
    Out = acc / wsum;
#endif
}
#endif
