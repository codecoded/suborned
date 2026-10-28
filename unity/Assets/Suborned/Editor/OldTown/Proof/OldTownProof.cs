#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.OldTown.Editor.Proof
{
    /// Brief 075 remap step 2: run the old DecoKit builders' own entry points (DecoKitMenu) under the old project's render
    /// settings. Not part of the verbatim copy: the only new code. The old project rendered with its PC_RPAsset +
    /// PC_Renderer (MSAA 4, depth and opaque textures, 4 cascades, probe volumes) and its PC quality level (lodBias 2,
    /// realtime reflection probes, no mip streaming); ours differ, so for the length of the call the old copies
    /// (Assets/Suborned/OldTownArt/Settings) are swapped in, in memory, and everything is put back after.
    ///   -executeMethod Suborned.OldTown.Editor.Proof.OldTownProof.Street   build + save Assets/Scenes/OldTownProof.unity
    ///   -executeMethod Suborned.OldTown.Editor.Proof.OldTownProof.Flyby    the old flyby on the saved scene (-dk* args as the old)
    public static class OldTownProof
    {
        const string OldPipeline = "Assets/Suborned/OldTownArt/Settings/PC_RPAsset.asset";

        public static void Street() => WithOldSettings(DecoKit.DecoKitMenu.Street);
        public static void Flyby() => WithOldSettings(DecoKit.DecoKitMenu.Flyby);

        static void WithOldSettings(Action body)
        {
            var rp = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(OldPipeline);
            if (rp == null) throw new Exception($"[OldTown] no old pipeline at {OldPipeline}");
            var graphics = GraphicsSettings.defaultRenderPipeline;
            int level = QualitySettings.GetQualityLevel();
            var quality = QualitySettings.GetRenderPipelineAssetAt(level);  // the getter can read null in batch mode
            int antiAliasing = QualitySettings.antiAliasing;  // a pipeline swap rewrites it from the asset's MSAA
            float lodBias = QualitySettings.lodBias;
            bool realtimeProbes = QualitySettings.realtimeReflectionProbes, streaming = QualitySettings.streamingMipmapsActive;
            try
            {
                GraphicsSettings.defaultRenderPipeline = rp;
                QualitySettings.renderPipeline = rp;
                QualitySettings.lodBias = 2f;
                QualitySettings.realtimeReflectionProbes = true;
                QualitySettings.streamingMipmapsActive = false;
                Debug.Log($"[OldTown] pipeline {AssetDatabase.GetAssetPath(GraphicsSettings.currentRenderPipeline)}, lodBias 2, realtime probes on, mip streaming off");
                body();
            }
            finally
            {
                GraphicsSettings.defaultRenderPipeline = graphics;
                QualitySettings.renderPipeline = quality;
                QualitySettings.lodBias = lodBias;
                QualitySettings.realtimeReflectionProbes = realtimeProbes;
                QualitySettings.streamingMipmapsActive = streaming;
                QualitySettings.antiAliasing = antiAliasing;
                Debug.Log($"[OldTown] settings restored: quality {level} pipeline {QualitySettings.GetRenderPipelineAssetAt(level)?.name}, AA {QualitySettings.antiAliasing}");
            }
        }
    }
}
#endif
