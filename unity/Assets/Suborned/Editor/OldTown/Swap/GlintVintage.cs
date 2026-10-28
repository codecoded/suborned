#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Suborned.OldTown.Editor.DecoKit;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Glint, brief 075 "vintage" (2026-10-29; Sky on bloom 0.48 vs 1.0: "old sky looks better, more 1950s vintage, though some colors
    /// do seem sharper in new"). Presentation cameras only: the old stage's grade (DK grade volume, DK_StreetGrade) is cloned in memory and each
    /// variant edits the clone (bloom threshold and tint, an accent-only saturation lift through ColorCurves satVsSat, contrast); the old sky,
    /// fog and probes stay as the old stage built them. Renders the old reel's camera (DecoKitFlyby.Pose) at t 10 (golden Boulevard), 20 (circus)
    /// and 61.5 (sky), 1920 x 1080 at 2x, offscreen. Nothing is saved (scene, profile or asset).
    /// usage: unityrun.ts log -- -executeMethod Suborned.OldTown.Editor.Swap.GlintVintage.Cli -gvOut briefs/075/parity/vintage/frames [-gvOnly base,v1]</summary>
    internal static class GlintVintage
    {
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        static string Repo => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        internal sealed class V
        {
            public string id, note; public float threshold = .8f, intensity = .6f, scatter = .85f, contrast = 10f, saturation = 25f;
            public Color tint = new(1f, .96f, .9f);
            public float knee = -1f, full = 1f, gain = 1f;  // satVsSat: x1 below knee, x gain from full up (HSV saturation of the LUT's linear colour)
            public float spareLo = -1f, spareHi = -1f;  // hueVsSat: cancels the lift across this hue band (the sky and the sea), eased over 0.05 each side
        }

        static readonly V[] Variants =
        {
            new() { id = "base", note = "the old stage as built (DK_StreetGrade: bloom 0.8 / 0.6 / 0.85 #fff5e6, ACES, exposure -0.8, contrast 10, saturation 25)" },
            new() { id = "v1", note = "mid threshold: bloom threshold 0.8 -> 1.15 (0.7 at our sun, light.json exposureMatch 0.6); the rest old", threshold = 1.15f },
            new() { id = "v2", note = "old bloom, accent lift: bloom old; satVsSat x1 below 0.55, x1.22 from 0.85 (enamel only); contrast 10 -> 13",
                    knee = .55f, full = .85f, gain = 1.22f, contrast = 13f },
            new() { id = "v3", note = "mid threshold + half-warm glow + accent lift: threshold 1.0, tint #fffaf3, satVsSat x1 below 0.55, x1.18 from 0.85, contrast 12",
                    threshold = 1.0f, tint = new Color(1f, .98f, .953f), knee = .55f, full = .85f, gain = 1.18f, contrast = 12f },
            new() { id = "v2s", note = "v2 with the sky spared: old bloom whole; satVsSat x1 below 0.55, x1.22 from 0.85; hueVsSat cancels the lift over hue 0.44-0.58 (sky and sea); contrast 13",
                    knee = .55f, full = .85f, gain = 1.22f, contrast = 13f, spareLo = .44f, spareHi = .58f },
            new() { id = "v3s", note = "a touch of the mid threshold: bloom 0.9, tint #fffaf3; satVsSat x1 below 0.55, x1.2 from 0.85; hue 0.44-0.58 spared; contrast 12",
                    threshold = .9f, tint = new Color(1f, .98f, .953f), knee = .55f, full = .85f, gain = 1.2f, contrast = 12f, spareLo = .44f, spareHi = .58f },
        };

        static readonly (string id, float t)[] Frames = { ("golden", 10f), ("circus", 20f), ("sky", 61.5f) };

        public static void Cli()
        {
            try
            {
                var wrap = typeof(OldTownSwap).GetMethod("WithOldSettings", Any) ?? throw new MissingMethodException("OldTownSwap.WithOldSettings");
                wrap.Invoke(null, new object[] { (Action)Run });
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); else throw; }
        }

        static void Run()
        {
            string outDir = Path.Combine(Repo, DecoKitStage.Arg("gvOut", "briefs/075/parity/vintage/frames"));
            var only = new HashSet<string>(DecoKitStage.Arg("gvOnly", string.Join(",", Variants.Select(v => v.id))).Split(','));
            Directory.CreateDirectory(outDir);
            var open = typeof(SwapShots).GetMethod("Open", Any) ?? throw new MissingMethodException("SwapShots.Open");
            var pose = typeof(SwapShots).GetMethod("Pose", Any) ?? throw new MissingMethodException("SwapShots.Pose");
            var oargs = new object[3];
            var cam = (Camera)open.Invoke(null, oargs);
            var vol = Object.FindObjectsByType<Volume>(FindObjectsInactive.Include).FirstOrDefault(v => v.name == "DK grade")
                      ?? throw new InvalidOperationException("no DK grade volume in the scene");
            var clone = Object.Instantiate(vol.sharedProfile);
            vol.sharedProfile = clone;
            var log = new StringBuilder("{\n");
            foreach (var v in Variants.Where(v => only.Contains(v.id)))
            {
                Apply(clone, v);
                foreach (var (fid, t) in Frames)
                {
                    pose.Invoke(null, new object[] { cam, oargs[0], oargs[1], t });
                    float shadow = Mathf.Clamp(cam.transform.position.y * 8f, 120f, 1200f);
                    var file = Path.Combine(outDir, $"{v.id}-{fid}.png");
                    DecoKitStage.WithShadows(shadow, () => DecoKitStage.Capture(cam, 1920, 1080, file, 2));
                    Debug.Log($"[GlintVintage] {v.id} {fid} t{t} -> {file}");
                }
                log.Append($" \"{v.id}\": \"{v.note}\",\n");
            }
            File.WriteAllText(Path.Combine(outDir, "variants.json"), log.ToString().TrimEnd(',', '\n') + "\n}\n");
        }

        static void Apply(VolumeProfile p, V v)
        {
            if (!p.TryGet(out Bloom bloom)) bloom = p.Add<Bloom>(true);
            bloom.threshold.Override(v.threshold); bloom.intensity.Override(v.intensity); bloom.scatter.Override(v.scatter); bloom.tint.Override(v.tint);
            if (!p.TryGet(out ColorAdjustments ca)) ca = p.Add<ColorAdjustments>(true);
            ca.contrast.Override(v.contrast); ca.saturation.Override(v.saturation);
            if (!p.TryGet(out ColorCurves cc)) cc = p.Add<ColorCurves>(true);
            if (v.knee < 0f) { cc.active = false; return; }
            cc.active = true;
            var keys = new[] { new Keyframe(0f, .5f), new Keyframe(v.knee, .5f), new Keyframe(v.full, .5f * v.gain), new Keyframe(1f, .5f * v.gain) };
            cc.satVsSat.Override(new TextureCurve(keys, .5f, false, new Vector2(0f, 1f)));
            if (v.spareLo < 0f) { cc.hueVsSat.Override(new TextureCurve(new Keyframe[] { }, .5f, true, new Vector2(0f, 1f))); return; }
            float d = .5f / v.gain;
            cc.hueVsSat.Override(new TextureCurve(new[] { new Keyframe(v.spareLo - .05f, .5f), new Keyframe(v.spareLo, d), new Keyframe(v.spareHi, d), new Keyframe(v.spareHi + .05f, .5f) }, .5f, true, new Vector2(0f, 1f)));
        }
    }
}
#endif
