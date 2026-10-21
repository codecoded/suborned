using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World
{
    /// <summary>Sky gradient, trilight ambient and the post-processing volume, all from light.json (Glint). Rig calls Apply once
    /// after the sun and fog are set. The grade volume is saved beside the scene (&lt;scene&gt;-grade.asset).</summary>
    public static class Atmosphere
    {
        public static bool HasSky => RenderSettings.skybox != null;

        public static void Apply(Transform lighting, Look look)
        {
            var amb = look.Light("ambient");
            if (amb != null && J.S(amb, "mode") == "trilight")
            {
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = look.Colour(J.Get(amb, "sky"));
                RenderSettings.ambientEquatorColor = look.Colour(J.Get(amb, "equator"));
                RenderSettings.ambientGroundColor = look.Colour(J.Get(amb, "ground"));
            }
            if (look.Light("skyGradient") is { } sky)
                RenderSettings.skybox = Sky(look, sky, RenderSettings.sun ? -RenderSettings.sun.transform.forward : Vector3.up);
            if (look.Light("post") is { } post) Grade(lighting, Profile(look, post, "grade"), "grade");
        }

        /// <summary>The sky gradient material for a phase's `skyGradient` block, the glow toward `toSun` (the moon at night).</summary>
        public static Material Sky(Look look, object sky, Vector3 toSun)
        {
                var m = new Material(Shader.Find("Suborned/SkyGradient") ?? throw new InvalidDataException("World: shader Suborned/SkyGradient not found")) { name = "sky" };
                m.SetColor("_Zenith", look.Palette(J.S(sky, "zenith")));
                m.SetColor("_Mid", look.Palette(J.S(sky, "mid")));
                m.SetColor("_Horizon", look.Palette(J.S(sky, "horizon")));
                m.SetColor("_Below", look.Palette(J.S(sky, "below")));
                m.SetColor("_SunColour", look.Palette(J.S(sky, "sunGlow")));
                m.SetVector("_SunDir", new Vector4(toSun.x, toSun.y, toSun.z, 0));
                m.SetFloat("_MidHeight", J.F(sky, "midHeight"));
                m.SetFloat("_Curve", J.F(sky, "curve"));
                m.SetVector("_Glow", new Vector4(J.F(sky, "glowWidth"), J.F(sky, "glowStrength"), J.F(sky, "disc"), 0));
                return m;
        }

        /// <summary>A grade profile from a phase's `post` block, saved beside the scene as &lt;scene&gt;-&lt;name&gt;.asset.</summary>
        public static VolumeProfile Profile(Look look, object post, string name)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = name;
            var tm = profile.Add<Tonemapping>(true);
            tm.mode.Override(J.S(post, "tonemapping") == "ACES" ? TonemappingMode.ACES : TonemappingMode.Neutral);
            if (J.Get(post, "bloom") is { } b)
            {
                var bl = profile.Add<Bloom>(true);
                bl.threshold.Override(J.F(b, "threshold")); bl.intensity.Override(J.F(b, "intensity"));
                bl.scatter.Override(J.F(b, "scatter")); bl.tint.Override(look.Palette(J.S(b, "tint")));
            }
            if (J.Get(post, "colour") is { } c)
            {
                var ca = profile.Add<ColorAdjustments>(true);
                ca.postExposure.Override(J.F(c, "postExposure")); ca.contrast.Override(J.F(c, "contrast"));
                ca.saturation.Override(J.F(c, "saturation")); ca.colorFilter.Override(look.Palette(J.S(c, "filter")));
            }
            if (J.Get(post, "whiteBalance") is { } wb)
            {
                var w = profile.Add<WhiteBalance>(true);
                w.temperature.Override(J.F(wb, "temperature")); w.tint.Override(J.F(wb, "tint"));
            }
            if (J.Get(post, "shadowsMidtonesHighlights") is { } s)
            {
                var sm = profile.Add<ShadowsMidtonesHighlights>(true);
                sm.shadows.Override(Vec(look, s, "shadows", "shadowsWeight"));
                sm.midtones.Override(Vec(look, s, "midtones", "midtonesWeight"));
                sm.highlights.Override(Vec(look, s, "highlights", "highlightsWeight"));
            }
            if (J.Get(post, "vignette") is { } v)
            {
                var vg = profile.Add<Vignette>(true);
                vg.intensity.Override(J.F(v, "intensity")); vg.smoothness.Override(J.F(v, "smoothness")); vg.color.Override(look.Palette(J.S(v, "colour")));
            }
            var path = Path.ChangeExtension(HarbourWorldBuilder.ScenePath, null) + "-" + name + ".asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(profile, path);
            foreach (var o in profile.components) AssetDatabase.AddObjectToAsset(o, profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        /// <summary>A global volume under `lighting` with the profile.</summary>
        public static Volume Grade(Transform lighting, VolumeProfile profile, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(lighting, false);
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true; vol.sharedProfile = profile;
            return vol;
        }

        /// <summary>Colour with weight in w, the ShadowsMidtonesHighlights lift/gamma/gain form (rgb = colour, w = weight offset).</summary>
        static Vector4 Vec(Look look, object s, string colourKey, string weightKey)
        {
            var c = look.Palette(J.S(s, colourKey));
            return new Vector4(c.r, c.g, c.b, J.F(s, weightKey));
        }

        /// <summary>Post-processing on for a camera (the grade volume only reaches cameras that ask).</summary>
        public static void Camera(UnityEngine.Camera cam)
        {
            var d = cam.GetUniversalAdditionalCameraData();
            d.renderPostProcessing = true;
            cam.allowHDR = true;
        }
    }
}
