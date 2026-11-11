using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Suborned.Play
{
    /// <summary>Brief 106 (Sky 2026-11-11: "Yes" to switching on what Unity 6.6 already ships): three built-in renderer features, each behind its own
    /// switch so they A/B without a rebuild. Defaults are the build's answer from the Air measurement (briefs/106/latch/results.md); the command line
    /// overrides them for a launch, and a `-tplong` row (TpLongVariants) for that row.
    /// - GPU occlusion (`-gpuOcclusion on|off`): the GPU Resident Drawer's occlusion test in cameras (URP asset, "GPU Occlusion"). The drawer reads it
    ///   once when it is created, so a change recreates the drawer (GPUResidentDrawer.Reinitialize, internal: by reflection) and logs what it says now.
    /// - Mesh LOD (`-meshLod on|off`): meshes built with Mesh LOD levels (the merged ground chunks, DecoKitTileMerge) choose their level by screen size;
    ///   off pins every such renderer to level 0 (Renderer.forceMeshLod 0), the build as it drew before. The levels are added to the chunk meshes after
    ///   the town's Editor bakes (MeshLod106); this sets every such renderer at each scene load.
    /// - STP (`-stp on|off`, `-stpScale 50-100`): the 3D pass drawn at stpScale of what RenderCap would draw, upscaled by URP's Spatial-Temporal
    ///   Post-processing; at a scale of 1 RenderCap leaves the filter Automatic, so STP does not run. The build's asset selects STP (BuiltInsBuild) only so
    ///   the player keeps STP's shaders (Unity strips them when no asset selects it); Boot puts it back to Automatic before the first scene, and RenderCap
    ///   sets the filter in use every time it sets the scale.
    /// Play and look only: the sim, sight and replays read none of it.</summary>
    public static class BuiltIns
    {
        /// <summary>The Air, 2026-11-11, full screen 3840x2410 (3D pass 2880x1808), two interleaved repeats (briefs/106/latch/results.md): Mesh LOD on
        /// saves 2.0 ms in tp-street and 1.8 ms in tp-worst, play unchanged; GPU occlusion costs 1.1 ms at play, 1.1 in tp-street, 4.3 in tp-worst
        /// (fewer triangles, more SetPass and main thread); STP at 75 % costs 4.9 ms at play and 6.8-8.0 in third person (its motion vectors and the
        /// full-size pass). So only Mesh LOD is on.</summary>
        public const bool DefaultOcclusion = false, DefaultMeshLod = true, DefaultStp = false;
        public const float DefaultStpScale = 0.75f;
        /// <summary>QualitySettings.meshLodThreshold while Mesh LOD is on (Unity's default 1 kept the 48 m chunks at level 0 in every view measured:
        /// briefs/106/latch/results.md); `-meshLodThreshold` sets it. Only the merged chunks carry Mesh LOD, so it moves nothing else.</summary>
        public const float DefaultMeshLodThreshold = 16f;
        public static float MeshLodThreshold { get; private set; } = DefaultMeshLodThreshold;

        public static bool Occlusion { get; private set; } = DefaultOcclusion;
        public static bool MeshLod { get; private set; } = DefaultMeshLod;
        public static bool Stp { get; private set; } = DefaultStp;
        public static float StpScale { get; private set; } = DefaultStpScale;
        /// <summary>Renderers whose mesh carries Mesh LOD levels, seen at the last scene load or switch.</summary>
        public static int MeshLodRenderers { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (Application.isEditor) return;
            Occlusion = Flag("-gpuOcclusion", DefaultOcclusion); MeshLod = Flag("-meshLod", DefaultMeshLod); Stp = Flag("-stp", DefaultStp);
            if (GameBoot.ArgValue("-meshLodThreshold") is { } th && float.TryParse(th, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var tv) && tv > 0f) MeshLodThreshold = tv;
            if (GameBoot.ArgValue("-stpScale") is { } s && float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var pct)) StpScale = Mathf.Clamp(pct / 100f, 0.5f, 1f);
            if (UniversalRenderPipeline.asset is UniversalRenderPipelineAsset urp) { urp.gpuResidentDrawerEnableOcclusionCullingInCameras = Occlusion; urp.upscalingFilter = UpscalingFilterSelection.Auto; } // before the first frame: the pipeline is created with them
            SceneManager.sceneLoaded += (_, _) => { ApplyOcclusion(); ApplyMeshLod(); Log(); };
        }

        static bool Flag(string name, bool fallback) => GameBoot.ArgValue(name) switch { "on" => true, "off" => false, null => fallback, var v => throw new ArgumentException($"{name} `{v}`: on or off") };

        public static void SetOcclusion(bool on) { Occlusion = on; ApplyOcclusion(); }
        public static void SetMeshLod(bool on) { MeshLod = on; Lowest = false; ApplyMeshLod(); }
        /// <summary>Diagnostic (a `-tplong` row's meshLod "low"): every Mesh LOD renderer at its last level, to see the levels draw at all.</summary>
        public static bool Lowest { get; private set; }
        public static void SetMeshLodLowest() { MeshLod = true; Lowest = true; ApplyMeshLod(); }
        /// <summary>QualitySettings.meshLodThreshold (Unity's default 1: higher switches to coarser levels sooner).</summary>
        public static void SetMeshLodThreshold(float t) { if (t > 0f) { MeshLodThreshold = t; ApplyMeshLod(); } }
        public static void SetStp(bool on, float scale) { Stp = on; StpScale = Mathf.Clamp(scale, 0.5f, 1f); RenderCap.Refresh(); }

        /// <summary>The scale STP takes off the 3D pass now (1 when it is off).</summary>
        public static float StpFactor => Stp ? StpScale : 1f;

        static void ApplyOcclusion()
        {
            if (UniversalRenderPipeline.asset is not UniversalRenderPipelineAsset urp) return;
            urp.gpuResidentDrawerEnableOcclusionCullingInCameras = Occlusion;
            if (GPUResidentDrawer.IsInstanceOcclusionCullingEnabled() == Occlusion) return;
            var re = typeof(GPUResidentDrawer).GetMethod("Reinitialize", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (re == null) { Debug.LogError("BuiltIns: GPUResidentDrawer.Reinitialize not found: GPU occlusion cannot switch in this player"); return; }
            re.Invoke(null, null);
        }

        static void ApplyMeshLod()
        {
            int n = 0;
            foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var f = r.GetComponent<MeshFilter>(); var m = f ? f.sharedMesh : null;
                if (!m || m.lodCount <= 1) continue;
                r.forceMeshLod = (short)(Lowest ? m.lodCount - 1 : MeshLod ? -1 : 0); n++;
            }
            MeshLodRenderers = n; QualitySettings.meshLodThreshold = MeshLod ? MeshLodThreshold : 1f;
        }

        /// <summary>One line for the player's log and the row's report: what each switch is and what the pipeline says.</summary>
        public static string Describe() =>
            $"GPU occlusion {(Occlusion ? "on" : "off")} (drawer says {(GPUResidentDrawer.IsInstanceOcclusionCullingEnabled() ? "on" : "off")}); " +
            $"Mesh LOD {(Lowest ? "lowest" : MeshLod ? "on" : "off")} on {MeshLodRenderers} renderers (threshold {QualitySettings.meshLodThreshold:0.##}); STP {(Stp ? $"on at {StpScale:0.00}" : "off")} (render scale {(UniversalRenderPipeline.asset is UniversalRenderPipelineAsset u ? $"{u.renderScale:0.00}, filter {u.upscalingFilter}" : "no URP asset")})";

        public static void Log() => Debug.Log("BuiltIns: " + Describe());
    }
}
