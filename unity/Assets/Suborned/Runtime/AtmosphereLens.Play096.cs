using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned
{
    /// <summary>Glint, brief 096 (Sky picked "A+D+B", 2026-11-05; the review: briefs/096/glint/grade/index.html). The play camera's grade, on the lens's
    /// play volume (weight 1 only while the play camera renders; night and atmosphere-off leave it, as the rest of the play tier):
    ///  · A: graded in HDR (the URP asset's colour grading mode, PC_RPAsset), ACES kept (the scene's play grade); exposure 0, contrast 4, saturation 24,
    ///    white balance tint +10 (toward magenta: out of the yellow), no lift (the old warm lift and its gain offset cleared);
    ///  · D: a 32^3 Color Lookup authored from the street paintings (briefs/008/concepts; out/scratch/glint/096/lut.py), contribution 0.9,
    ///    Resources/Atmosphere/concept-lut.bytes (a PNG strip read raw, linear: no import settings to drift);
    ///  · B: the sun's warm light fills the shade, shadow strength 0.78 (lens.json sun.shadowStrength).
    /// Values come from lens.json `play096`; absent = the play tier as before. All of it lives in the grading LUT: no per-pixel cost.</summary>
    public sealed partial class AtmosphereLens
    {
        [Serializable] public sealed class Play096p { public bool on; public float exposure, contrast, saturation, temperature, tint, lutContribution = 1f; public string lut = ""; }
        [Serializable] sealed class Play096File { public Play096p play096; }

        static Play096p play096; static bool play096Read; static Texture2D play096Lut;
        /// <summary>Brief 096: the sun's shadow strength the lens wants this render, as a shader global (OldTownPlayLook's assembly cannot see the lens): OldTownPlayLook's play light
        /// used to force its baked value (1.0) over the lens's, so lens.json sun.shadowStrength (B's 0.78) never reached the play view.</summary>
        public static readonly int LensSunShadowId = Shader.PropertyToID("_SubornedLensSunShadow");
        WhiteBalance playWb; ColorLookup playLut;

        static Play096p Play096Data()
        {
            if (play096Read) return play096;
            play096Read = true;
            var text = Resources.Load<TextAsset>(ResourcePath);
            play096 = text ? JsonUtility.FromJson<Play096File>(text.text)?.play096 : null;
            if (play096 is { on: true } && play096.lut.Length > 0 && Resources.Load<TextAsset>(play096.lut) is { } raw)
            {
                play096Lut = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { name = "concept-lut", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                if (!play096Lut.LoadImage(raw.bytes, true)) { Debug.LogError("AtmosphereLens: play096 lut unreadable: " + play096.lut); play096Lut = null; }
            }
            return play096;
        }

        void ApplyPlay096()
        {
            var g = Play096Data();
            if (g is not { on: true } || !vPlay || !vPlay.sharedProfile) return;
            var p = vPlay.sharedProfile;
            playSat.postExposure.Override(g.exposure); playSat.contrast.Override(g.contrast); playSat.saturation.Override(g.saturation);
            playLift.lift.Override(new Vector4(1f, 1f, 1f, 0f)); playLift.gain.Override(new Vector4(1f, 1f, 1f, 0f));
            if (!playWb && !p.TryGet(out playWb)) playWb = p.Add<WhiteBalance>(false); // the evening grade may already have put one on the play profile
            playWb.temperature.Override(g.temperature); playWb.tint.Override(g.tint);
            if (play096Lut)
            {
                if (!playLut && !p.TryGet(out playLut)) playLut = p.Add<ColorLookup>(false);
                playLut.texture.Override(play096Lut); playLut.contribution.Override(g.lutContribution);
            }
        }
    }
}
