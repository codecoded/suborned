using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.Play
{
    /// <summary>Brief 086 (Redline's plan section 1.3, the Air sitting): what a `-tplong` row can switch, for that row only. Nothing else calls this, so no normal run ever does. Every row sets every switch, so the
    /// order of rows never matters. softShadows off: the lights' soft shadows hard. bloom off: a top volume sets its intensity to 0. leavesAlpha off: the leaf materials' alpha test off (the cards draw solid: a price, not a look).
    /// shadows off: the play camera renders none. oldFar off: old-art renderers beyond 150 m not drawn, the most a LOD pass on them could give. planting off: the lodAtPlay groups not drawn.
    /// upscale, renderScalePct: URP's filter and the render scale. Each says what it touched in the player's log, line `TpLongVariants:`, and bloom as the camera resolved it.</summary>
    public static class TpLongVariants
    {
        static readonly List<(Light l, LightShadows was)> softLights = new();
        static GameObject bloomGo; static VolumeProfile bloomProfile; static bool bloomCheck;
        static readonly List<Material> leaves = new();
        static readonly List<Renderer> hiddenOld = new(), hiddenPlanting = new();
        static readonly List<(UniversalAdditionalCameraData d, bool was)> shadowCams = new();
        static TpLongRun.RowVariant now; static bool filterSaved; static UpscalingFilterSelection filterWas; static float scaleWas = -1f;
        /// <summary>What the last row's switches touched, for the row's report.</summary>
        public static string Touched { get; private set; } = "";

        static bool Off(string s, string name) { if (string.IsNullOrEmpty(s) || s == "on") return false; if (s == "off") return true; throw new InvalidOperationException($"row variant {name} `{s}`: on or off"); }

        public static void Apply(TpLongRun.RowVariant v)
        {
            Restore(); now = v; Touched = ""; if (v == null) return;
            if (Off(v.softShadows, "softShadows")) { foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (l.shadows == LightShadows.Soft) { softLights.Add((l, l.shadows)); l.shadows = LightShadows.Hard; } Touched += $"soft shadows hard on {softLights.Count} lights; "; }
            if (Off(v.leavesAlpha, "leavesAlpha")) { var seen = new HashSet<Material>(); int none = 0; foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) foreach (var m in r.sharedMaterials) if (m != null && m.name.Contains("Leaf") && seen.Add(m)) { if (m.IsKeywordEnabled("_ALPHATEST_ON")) { m.DisableKeyword("_ALPHATEST_ON"); leaves.Add(m); } else none++; } Touched += $"alpha test off on {leaves.Count} leaf materials, {none} with no such keyword; "; }
            if (Off(v.planting, "planting")) { int groups = 0; foreach (var g in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) if (IsPlanting(g.name)) { groups++; foreach (var r in g.GetComponentsInChildren<Renderer>()) if (!r.forceRenderingOff) { r.forceRenderingOff = true; hiddenPlanting.Add(r); } } Touched += $"planting off: {groups} groups, {hiddenPlanting.Count} renderers; "; }
            if (!string.IsNullOrEmpty(v.upscale) || v.renderScalePct > 0f)
            {
                if (UniversalRenderPipeline.asset is not UniversalRenderPipelineAsset urp) throw new InvalidOperationException("no URP asset");
                if (!string.IsNullOrEmpty(v.upscale)) { filterWas = urp.upscalingFilter; filterSaved = true; urp.upscalingFilter = v.upscale switch { "auto" => UpscalingFilterSelection.Auto, "linear" => UpscalingFilterSelection.Linear, "point" => UpscalingFilterSelection.Point, "fsr" => UpscalingFilterSelection.FSR, "stp" => UpscalingFilterSelection.STP, _ => throw new InvalidOperationException($"row variant upscale `{v.upscale}`: auto, linear, point, fsr or stp") }; }
                if (v.renderScalePct > 0f) { scaleWas = RenderCap.UserScale; RenderCap.UserScale = v.renderScalePct / 100f; }
                Touched += $"filter {urp.upscalingFilter}, user scale {RenderCap.UserScale:0.00}; ";
            }
        }

        /// <summary>The window is about to start: the camera stands where the row measures, so what is far and which camera renders are known.</summary>
        public static void BeforeWindow(Camera cam)
        {
            if (now == null || !cam) return;
            if (Off(now.shadows, "shadows")) { var d = cam.GetUniversalAdditionalCameraData(); shadowCams.Add((d, d.renderShadows)); d.renderShadows = false; Touched += "camera shadows off; "; }
            if (Off(now.oldFar, "oldFar"))
            {
                var at = cam.transform.position; foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) { if (r.forceRenderingOff || !IsOldArt(r.transform) || !(r is MeshRenderer)) continue; if (r.bounds.SqrDistance(at) >= 150f * 150f) { r.forceRenderingOff = true; hiddenOld.Add(r); } }
                Touched += $"old art beyond 150 m off: {hiddenOld.Count} renderers; ";
            }
            if (Off(now.bloom, "bloom"))
            {   // a global volume above every other, on a layer the camera reads: the lens's own tiers write their bloom each frame, so switching the profiles' overrides off left it on (seen in the Studio dry run: intensity 0.25)
                var data = cam.GetUniversalAdditionalCameraData(); int mask = data.volumeLayerMask, layer = 0; while (layer < 31 && (mask & (1 << layer)) == 0) layer++;
                bloomGo = new GameObject("tplong-bloom-off") { layer = layer }; var vol = bloomGo.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 10000f; vol.weight = 1f; bloomProfile = ScriptableObject.CreateInstance<VolumeProfile>(); var off = bloomProfile.Add<Bloom>(true); off.intensity.Override(0f); vol.sharedProfile = bloomProfile; bloomCheck = true; Touched += "bloom off by a top volume; ";
            }
            if (Touched.Length > 0) Debug.Log("TpLongVariants: " + Touched);
        }

        /// <summary>Called once the window is over, for the log: bloom as the camera resolved it.</summary>
        public static void CheckBloom(Camera cam) { if (!bloomCheck || !cam) return; bloomCheck = false; var st = cam.GetUniversalAdditionalCameraData().volumeStack ?? VolumeManager.instance.stack; var bl = st.GetComponent<Bloom>(); Debug.Log($"TpLongVariants: bloom as resolved: active {bl != null && bl.IsActive()}, intensity {(bl != null ? bl.intensity.value : -1f):0.00}"); }

        /// <summary>A play row's zoom and yaw (the yaw in quarter steps from the opening 30).</summary>
        public static void PlaceOverhead(HandoverHost h, float zoom, float yaw)
        {
            if (zoom > 0f && h.View) h.View.orthographicSize = zoom;
            int k = Mathf.RoundToInt(Mathf.Repeat(yaw - 30f, 360f) / 90f) % 4; h.TurnSnap(k);
        }

        static void Restore()
        {
            foreach (var (l, was) in softLights) if (l) l.shadows = was; softLights.Clear();
            if (bloomGo) UnityEngine.Object.Destroy(bloomGo); if (bloomProfile) UnityEngine.Object.Destroy(bloomProfile); bloomGo = null; bloomProfile = null; bloomCheck = false;
            foreach (var m in leaves) if (m) m.EnableKeyword("_ALPHATEST_ON"); leaves.Clear();
            foreach (var r in hiddenOld) if (r) r.forceRenderingOff = false; hiddenOld.Clear();
            foreach (var r in hiddenPlanting) if (r) r.forceRenderingOff = false; hiddenPlanting.Clear();
            foreach (var (d, was) in shadowCams) if (d) d.renderShadows = was; shadowCams.Clear();
            if (filterSaved && UniversalRenderPipeline.asset is UniversalRenderPipelineAsset urp) urp.upscalingFilter = filterWas; filterSaved = false;
            if (scaleWas >= 0f) RenderCap.UserScale = scaleWas; scaleWas = -1f;
        }

        /// <summary>The kit's rows that declare lodAtPlay (contracts/world/kit.json, 26 of them): the street's planting.</summary>
        static bool IsPlanting(string name) => name.Contains("Planting") || name.Contains("PlanterRun_") || name.Contains("PlanterArc_") || name.EndsWith("_TB") || name.Contains("_TB ") || name.Contains("_TB(");
        /// <summary>Everything the old builders placed: the scene's roots but our own pipeline's and the host's.</summary>
        static bool IsOldArt(Transform t) { var root = t.root.name; return root.StartsWith("Deco Street") || root.StartsWith("Southside") || root.StartsWith("West (") || root.StartsWith("Network ("); }
    }
}
