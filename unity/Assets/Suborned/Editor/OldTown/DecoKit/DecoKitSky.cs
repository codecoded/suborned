#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// The space-age daytime sky (DK_SpaceSky.shader; target Concepts/Horizon/sky_ab/celestial.png):
    /// colours sampled from the target, the ringed planet and two moons hung where the street's axis views see them,
    /// scene fog = the shader's horizon haze so distant geometry dissolves into the same colour.
    internal static class DecoKitSky
    {
        const string SkyTex = TextureDir + "/Sky";
        const float BandBottom = -1f, BandTop = 9f, BandGain = 1.1f;  // sky_sheet.py BAND_BOTTOM / BAND_TOP / BAND_GAIN

        /// Azimuth from +Z (the boulevard's axis) toward +X, elevation, angular radius; degrees.
        internal readonly struct Body
        {
            internal readonly float Az, El, Radius;
            internal Body(float az, float el, float radius) { Az = az; El = el; Radius = radius; }
            internal Vector4 Encode => (Vector4)Dir(Az, El) + new Vector4(0, 0, 0, Mathf.Sin(Radius * Mathf.Deg2Rad));
        }

        // over the canyon's open sky on the axis views (the frontage hides anything low or >15 deg off axis)
        internal static readonly Body Planet = new(-13f, 29f, 8f), Moon1 = new(11f, 23f, 1.8f), Moon2 = new(-34f, 44f, 1.1f);

        internal static Vector3 Dir(float az, float el)
        {
            float a = az * Mathf.Deg2Rad, e = el * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Cos(a) * Mathf.Cos(e));
        }

        /// gain: sky exposure (linear multiplier on the sampled sRGB colours); fogDensity: exponential, per metre.
        internal static Material Apply(string name, Vector3 toSun, DecoKitAtmosphere.Ambient ambient, float gain,
                                       float fill, float fogDensity)
        {
            EnsureFolder(GeneratedDir);
            var path = $"{GeneratedDir}/DK_Sky_{name}.mat";
            var shader = Shader.Find("OldTown/DecoKit/Space Age Sky");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) AssetDatabase.CreateAsset(mat = new Material(shader), path);
            else mat.shader = shader;

            Color horizon = Srgb(246, 229, 195) * gain;
            // sea-green (the user's 1970s reference; was blue 39,89,170 / 85,156,218, hue -45, saturation x0.8 in linear)
            mat.SetColor("_Zenith", Srgb(88, 168, 170) * gain);
            mat.SetColor("_Mid", Srgb(127, 218, 208) * gain);
            mat.SetColor("_Horizon", horizon);
            mat.SetColor("_SunColor", Srgb(255, 244, 222) * gain);
            mat.SetColor("_CirrusColor", new Color(.95f * gain, .95f * gain, .96f * gain, .6f));
            mat.SetVector("_Cirrus", new Vector4(DecoKitStage.Arg("dkCirrus", .3f), .8f, 2.5f, .6f));
            mat.SetFloat("_MidHeight", DecoKitStage.Arg("dkSkyMid", .95f));
            mat.SetFloat("_HazeHeight", DecoKitStage.Arg("dkHaze", .1f));
            mat.SetVector("_SunDir", toSun.normalized);
            mat.SetVector("_Planet", Planet.Encode);
            mat.SetVector("_PlanetAxis", new Vector4(.42f, .88f, -.22f, 1.3f));
            mat.SetVector("_Ring", new Vector4(1.28f, 2.25f, .85f, 1f));
            mat.SetVector("_Moon1", Moon1.Encode);
            mat.SetVector("_Moon2", Moon2.Encode);
            mat.SetVector("_Body", new Vector4(DecoKitStage.Arg("dkBodyGain", 2.6f), .1f, .35f, .35f));
            mat.SetTexture("_NoiseTex", Tex("DK_Sky_Noise.png", srgb: false, TextureWrapMode.Repeat));
            mat.SetTexture("_PlanetTex", Tex("DK_Planet_Bands.png", srgb: true, TextureWrapMode.Repeat));
            mat.SetTexture("_RingTex", Tex("DK_Planet_Ring.png", srgb: true, TextureWrapMode.Clamp));
            mat.SetTexture("_MoonTex", Tex("DK_Moon.png", srgb: true, TextureWrapMode.Repeat));
            mat.SetTexture("_SkylineTex", Tex("DK_Skyline_Far.png", srgb: false, TextureWrapMode.Repeat, TextureWrapMode.Clamp, 8192));
            mat.SetVector("_SkylineBand", new Vector4(BandBottom * Mathf.Deg2Rad, BandTop * Mathf.Deg2Rad, BandGain,
                                                      DecoKitStage.Arg("dkCity", 1f)));
            EditorUtility.SetDirty(mat);

            RenderSettings.skybox = mat;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = (ambient.Sky * fill).gamma;
            RenderSettings.ambientEquatorColor = (ambient.Equator * fill).gamma;
            RenderSettings.ambientGroundColor = (ambient.Ground * fill).gamma;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.defaultReflectionResolution = 256;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogColor = horizon.gamma;  // RenderSettings colours are sRGB-encoded
            return mat;
        }

        static Color Srgb(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f).linear;

        /// wrapV defaults to wrap; maxSize 8192 only for the 360-degree band (its width is ~1 texel per screen pixel).
        internal static Texture2D Tex(string file, bool srgb, TextureWrapMode wrap, TextureWrapMode? wrapV = null,
                                      int maxSize = 2048)
        {
            var v = wrapV ?? wrap;
            var path = $"{SkyTex}/{file}";
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            if (ti == null) throw new System.IO.FileNotFoundException($"[DecoKit] missing sky texture {path} (sky_sheet.py)");
            if (ti.sRGBTexture != srgb || ti.wrapModeU != wrap || ti.wrapModeV != v
                || ti.textureCompression != TextureImporterCompression.CompressedHQ || ti.maxTextureSize != maxSize)
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = srgb;
                ti.wrapModeU = wrap;
                ti.wrapModeV = v;
                ti.maxTextureSize = maxSize;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
                ti.mipmapEnabled = true;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
#endif
