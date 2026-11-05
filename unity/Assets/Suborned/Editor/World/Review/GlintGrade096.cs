using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Review
{
    /// <summary>Glint, brief 096 grading review: URP strips the HDR-grading shader variants (_HDR_GRADING) unless a pipeline asset in the build
    /// grades in HDR, so a scratch review build could not show an HDR-grading option. Only when the environment has GLINT096_HDR=1 (the review's own
    /// scratch build, out/scratch/glint/096), this sets the URP asset to HDR grading in memory for the build (so both variants ship; the player's
    /// `-grade` review switch picks the mode per launch, "today" picks LDR) and puts it back after. The build saves dirty assets, so the restore writes each
    /// .asset file back byte for byte from a copy taken before the build (check git status). Without the variable it does nothing.</summary>
    public sealed class GlintGrade096 : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => -1000;
        static readonly Dictionary<UniversalRenderPipelineAsset, ColorGradingMode> was = new();
        static readonly Dictionary<string, byte[]> files = new();
        static bool On => Environment.GetEnvironmentVariable("GLINT096_HDR") == "1";

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!On) return;
            was.Clear();
            foreach (var a in Assets())
            {
                var path = AssetDatabase.GetAssetPath(a); if (!string.IsNullOrEmpty(path)) files[path] = System.IO.File.ReadAllBytes(path);
                was[a] = a.colorGradingMode; a.colorGradingMode = ColorGradingMode.HighDynamicRange;
            }
            Debug.Log($"GlintGrade096: HDR grading for this review build on {was.Count} URP asset(s): {string.Join(", ", was.Select(kv => kv.Key.name + " (was " + kv.Value + ")"))}");
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            foreach (var kv in was) kv.Key.colorGradingMode = kv.Value;
            foreach (var kv in files) System.IO.File.WriteAllBytes(kv.Key, kv.Value); // the build saves dirty assets (and re-serializes them): the files go back byte for byte
            files.Clear();
            if (was.Count > 0) Debug.Log($"GlintGrade096: grading put back on {was.Count} URP asset(s)");
            was.Clear();
        }

        static IEnumerable<UniversalRenderPipelineAsset> Assets()
        {
            var seen = new HashSet<UniversalRenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset d && seen.Add(d)) yield return d;
            for (int i = 0; i < QualitySettings.count; i++) if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset q && seen.Add(q)) yield return q;
        }
    }
}
