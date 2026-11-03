#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// The airbrush lens (the user's note: haze in the distance, soft focus, solar flare): a diffusion-filter glow
    /// (low-threshold wide bloom), the far distance going soft (Gaussian DOF far range), and a sun starburst with a
    /// chain of hexagonal ghosts (data-driven lens flare, depth-occluded by the city).
    internal static class DecoKitLens
    {
        internal static void SoftFocus(VolumeProfile profile, float farStart, float farEnd, float radius)
        {
            if (!profile.TryGet(out Bloom bloom)) bloom = DecoKitStage.Add<Bloom>(profile);
            bloom.threshold.Override(DecoKitStage.Arg("dkBloomThreshold", .8f));
            bloom.intensity.Override(DecoKitStage.Arg("dkBloom", .6f));
            bloom.scatter.Override(.85f);
            bloom.tint.Override(new Color(1f, .96f, .9f));
            var dof = DecoKitStage.Add<DepthOfField>(profile);
            dof.mode.Override(DepthOfFieldMode.Gaussian);
            dof.gaussianStart.Override(farStart);
            dof.gaussianEnd.Override(farEnd);
            dof.gaussianMaxRadius.Override(radius);
            dof.highQualitySampling.Override(true);
            EditorUtility.SetDirty(profile);
        }

        internal static void SunFlare(Light sun, float intensity)
        {
            var flare = sun.gameObject.GetComponent<LensFlareComponentSRP>() ?? sun.gameObject.AddComponent<LensFlareComponentSRP>();
            flare.lensFlareData = Data();
            flare.intensity = intensity;
            flare.scale = 1f;
            flare.attenuationByLightShape = false;
            flare.useOcclusion = true;      // towers and landmarks cut the flare as they pass the sun
            flare.occlusionRadius = .02f;
            flare.sampleCount = 32;
            flare.allowOffScreen = false;
        }

        static LensFlareDataSRP Data()
        {
            EnsureFolder(GeneratedDir);
            var path = $"{GeneratedDir}/DK_SunFlare.asset";
            AssetDatabase.DeleteAsset(path);  // generated: rebuilt every run
            var data = ScriptableObject.CreateInstance<LensFlareDataSRP>();
            var burst = DecoKitSky.Tex("DK_Flare_Starburst.png", srgb: true, TextureWrapMode.Clamp);
            data.elements = new[]
            {
                Element(SRPLensFlareType.Image, 0f, 16f, 2.5f, new Color(1f, .96f, .88f), e => { e.lensFlareTexture = burst; e.rotation = 8f; }),
                Element(SRPLensFlareType.Circle, 0f, 7f, .25f, new Color(1f, .9f, .75f), e => e.fallOff = .08f),  // haze glow
                Element(SRPLensFlareType.Polygon, .16f, 1.4f, .2f, new Color(.85f, .95f, 1f), e =>
                {
                    e.sideCount = 6; e.fallOff = .6f; e.sdfRoundness = .35f;     // hexagonal ghost chain to the centre
                    e.allowMultipleElement = true; e.count = 6; e.distribution = SRPLensFlareDistribution.Uniform;
                    e.lengthSpread = .75f;
                }),
                Element(SRPLensFlareType.Circle, .9f, 3.2f, .05f, new Color(1f, .85f, .7f), e => e.fallOff = .4f),  // far ghost
            };
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        static LensFlareDataElementSRP Element(SRPLensFlareType type, float position, float scale, float intensity,
                                               Color tint, System.Action<LensFlareDataElementSRP> tweak)
        {
            var e = new LensFlareDataElementSRP
            {
                flareType = type, position = position, uniformScale = scale, localIntensity = intensity, tint = tint,
                blendMode = SRPLensFlareBlendMode.Additive, modulateByLightColor = true,
            };
            tweak(e);
            return e;
        }
    }
}
#endif
