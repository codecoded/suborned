using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Play
{
    /// <summary>Brief 106: the player keeps STP's shaders. Unity strips STP's runtime resources from a build when no pipeline asset in it selects STP
    /// (STPResourceStripper: ISTPEnabledRenderPipeline.isStpUsed), and at runtime RenderCap chooses the filter (BuiltIns: STP only when it is on and
    /// the 3D pass is smaller than the screen). So for the length of BuildPlayer the active quality level's URP asset selects STP, and it is put back
    /// after. The project's asset stays Automatic: URP runs STP even at a scale of 1 on any game camera once it is selected, which would change the
    /// Editor's play view and its capture tools.</summary>
    public static class BuiltInsBuild
    {
        public static IDisposable CarryStp() => new Carry();

        sealed class Carry : IDisposable
        {
            readonly List<(UniversalRenderPipelineAsset a, UpscalingFilterSelection was)> assets = new();

            public Carry()
            {
                // the active quality level's asset (PC: the Mac build's; Mobile is excluded from Standalone, and touching it left it changed once)
                var seen = new HashSet<UniversalRenderPipelineAsset>();
                if (QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel()) is UniversalRenderPipelineAsset q && seen.Add(q)) assets.Add((q, q.upscalingFilter));
                if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset d && seen.Add(d)) assets.Add((d, d.upscalingFilter));
                if (assets.Count == 0) Debug.LogError("BuiltInsBuild: no pipeline asset found for the active quality level: STP's shaders will be stripped from this build");
                foreach (var (a, _) in assets) { a.upscalingFilter = UpscalingFilterSelection.STP; EditorUtility.SetDirty(a); }
                Debug.Log($"BuiltInsBuild: STP selected for the build on {assets.Count} pipeline assets (its shaders ride along; RenderCap picks the filter at runtime)");
            }

            public void Dispose()
            {
                foreach (var (a, was) in assets) { if (!a) continue; a.upscalingFilter = was; EditorUtility.SetDirty(a); AssetDatabase.SaveAssetIfDirty(a); }
                AssetDatabase.SaveAssets();
                Debug.Log($"BuiltInsBuild: filters put back on {assets.Count} pipeline assets");
            }
        }
    }
}
