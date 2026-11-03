// Brief 072-F (Redline phase 2, 4.3): the surface-detail switch for the DecoKit cutaway shaders, keyword-free. The kit rows and the builder Look
// rows carry the colourless detail pair in URP Lit's detail slots under _DETAIL_MULX2; the Air's texture A/B turns them off at runtime with the
// global _SubornedSurfaceDetailOff (Suborned.LookLab; 0, the default when nothing sets it, = as built), never by changing a material or
// its keywords, so both sides of the A/B draw the same materials and the same variants.
// Included after LitInput.hlsl and before the pass's own include (LitForwardPass, LitGBufferPass, LitDepthNormalsPass), in the passes that
// sample the detail. It redirects the two places URP reads the detail slots, as the forward pass's DKEnvironmentBRDF does:
//  - InitializeStandardLitSurfaceData (forward, GBuffer): URP's own body, with the detail mask, albedo and normal samples behind a uniform
//    branch on the global, so the off side skips the three fetches and the TBN blend;
//  - ApplyDetailNormal (DepthNormals, the SSAO prepass): the detail normal behind the same branch (URP's mask sample stays in the pass).
// Without _DETAIL nothing changes: the plain variants are URP's as they were.
#ifndef DK_SURFACE_DETAIL_INCLUDED
#define DK_SURFACE_DETAIL_INCLUDED

#ifndef SUBORNED_SURFACE_DETAIL_OFF
#define SUBORNED_SURFACE_DETAIL_OFF
float _SubornedSurfaceDetailOff;   // global: 1 = skip the surface detail; unset = 0 = as built
#endif

#if defined(_DETAIL)
// URP 17 LitInput.hlsl InitializeStandardLitSurfaceData, unchanged but for the branch round the detail
inline void DKInitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);

    half4 specGloss = SampleMetallicSpecGloss(uv, albedoAlpha.a);
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    outSurfaceData.albedo = AlphaModulate(outSurfaceData.albedo, outSurfaceData.alpha);

#if _SPECULAR_SETUP
    outSurfaceData.metallic = half(1.0);
    outSurfaceData.specular = specGloss.rgb;
#else
    outSurfaceData.metallic = specGloss.r;
    outSurfaceData.specular = half3(0.0, 0.0, 0.0);
#endif

    outSurfaceData.smoothness = specGloss.a;
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    outSurfaceData.occlusion = SampleOcclusion(uv);
    outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));

#if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    half2 clearCoat = SampleClearCoat(uv);
    outSurfaceData.clearCoatMask       = clearCoat.r;
    outSurfaceData.clearCoatSmoothness = clearCoat.g;
#else
    outSurfaceData.clearCoatMask       = half(0.0);
    outSurfaceData.clearCoatSmoothness = half(0.0);
#endif

    UNITY_BRANCH if (_SubornedSurfaceDetailOff < 0.5)
    {
        half detailMask = SAMPLE_TEXTURE2D(_DetailMask, sampler_DetailMask, uv).a;
        float2 detailUv = uv * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
        outSurfaceData.albedo = ApplyDetailAlbedo(detailUv, outSurfaceData.albedo, detailMask);
        outSurfaceData.normalTS = ApplyDetailNormal(detailUv, outSurfaceData.normalTS, detailMask);
    }
}

half3 DKApplyDetailNormal(float2 detailUv, half3 normalTS, half detailMask)
{
    UNITY_BRANCH if (_SubornedSurfaceDetailOff >= 0.5) return normalTS;
    return ApplyDetailNormal(detailUv, normalTS, detailMask);
}

#define InitializeStandardLitSurfaceData DKInitializeStandardLitSurfaceData
#define ApplyDetailNormal DKApplyDetailNormal
#endif

#endif
