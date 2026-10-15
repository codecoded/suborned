using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Play
{
    /// <summary>The GPU Resident Drawer stays off in the Editor, in Edit Mode and in Play Mode (brief 011 D; Sky's Editor froze on 2026-10-15).
    /// Each time the drawer is created it registers every renderer in the generated scene (27,354 mesh renderers) and uploads their instance
    /// data in one frame. With the Scene view open over the district that frame ran out of the Metal graphics ring buffer ("Ran out of
    /// Graphics Ring Buffer space", Logs/Editor-prev.log 18:16:41), and the Editor stopped drawing. That happened on entering Play and again in
    /// Edit Mode after a domain reload. So the active URP asset's drawer mode is set to Disabled in memory when the Editor loads, and the
    /// asset's own value is kept in SessionState:
    ///   - a player build (Sky's build, Redline's perf harness in a development build, the Mono/IL2CPP check) gets the asset's own value: it is
    ///     put back before the build and taken off again afterwards, including after a failed build;
    ///   - the asset is never written with Disabled: if anything saves the URP asset, the own value goes back first and is taken off again
    ///     after the save;
    ///   - Editor tools that already turn the drawer off for their renders (Shots, RenderSession, WarmthProbe) find it off and leave it so.
    /// Replaces the Play-only guard (814282c).</summary>
    [InitializeOnLoad]
    static class EditorDrawerGuard
    {
        const string Key = "Suborned.Editor.DrawerWas"; // the asset's own mode + 1; 0 = nothing taken off

        // At once, on load: URP re-creates the drawer at the end of a domain reload, and an Off that waited for delayCall came after that
        // (Logs/Editor.log: "GPU Resident Drawer created", a ring-buffer warning, then "disposed"). Again on delayCall, in case the pipeline
        // asset was not yet known.
        static EditorDrawerGuard() { Off(); EditorApplication.delayCall += Off; }

        /// <summary>The URP asset the Editor renders with: the quality level's, else the default (known before the pipeline is created).</summary>
        static UniversalRenderPipelineAsset Asset => (QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;

        /// <summary>Take the drawer off in the Editor, remembering the asset's own mode once per session.</summary>
        public static void Off()
        {
            var a = Asset; if (!a || a.gpuResidentDrawerMode == GPUResidentDrawerMode.Disabled) return;
            if (SessionState.GetInt(Key, 0) == 0) SessionState.SetInt(Key, (int)a.gpuResidentDrawerMode + 1);
            a.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
        }

        /// <summary>Give the asset its own mode back (for a build or a save).</summary>
        public static void Restore()
        {
            int v = SessionState.GetInt(Key, 0); var a = Asset;
            if (v > 0 && a) a.gpuResidentDrawerMode = (GPUResidentDrawerMode)(v - 1);
        }

        /// <summary>The asset's own mode, whatever the Editor runs with.</summary>
        public static GPUResidentDrawerMode Own => SessionState.GetInt(Key, 0) is var v && v > 0 ? (GPUResidentDrawerMode)(v - 1) : Asset ? Asset.gpuResidentDrawerMode : GPUResidentDrawerMode.Disabled;

        sealed class Builds : IPreprocessBuildWithReport, IPostprocessBuildWithReport
        {
            public int callbackOrder => -1000; // before PerfBuild and anything else that reads the asset
            public void OnPreprocessBuild(BuildReport report) { Restore(); EditorApplication.delayCall += Off; } // delayCall: a failed build too
            public void OnPostprocessBuild(BuildReport report) => Off();
        }

        sealed class Saves : AssetModificationProcessor
        {
            static string[] OnWillSaveAssets(string[] paths)
            {
                var a = Asset; if (!a) return paths;
                var own = AssetDatabase.GetAssetPath(a);
                foreach (var p in paths)
                    if (p == own) { Restore(); EditorApplication.delayCall += Off; break; }
                return paths;
            }
        }
    }
}
