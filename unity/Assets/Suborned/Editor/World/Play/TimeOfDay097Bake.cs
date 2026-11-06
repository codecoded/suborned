using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Suborned.OldTown;
using Object = UnityEngine.Object;

namespace Suborned.World.Play
{
    /// <summary>Brief 097 time-of-day test (Sky 2026-11-07: "Test time of day and show me"), scratch builds only (-lean [m] -todBuild): the lean copy of
    /// the played scene (LeanScene) is lit by Adaptive Probe Volumes instead of the zone's lightmaps, baked once per time of day (Runtime TimeOfDay097.Keys:
    /// morning, noon, evening, night) as lighting scenarios of one baking set, with sky occlusion, so the player's clock can move the sun and blend the
    /// baked bounce between the two nearest times while the sky's colour changes freely. Unity's own route for time of day (Sky's "official best
    /// practice"). The real OldTown.unity, its lightmaps and the project's pipeline asset are never touched: the bake and the build run on a copy of the
    /// pipeline asset with probe volumes on (PC_RPAsset_TOD), swapped into the quality levels for the bake and the build and put back after.
    /// -todProbe d: the finest probe spacing in metres (default 1.5).</summary>
    public static class TimeOfDay097Bake
    {
        public static bool Asked => Array.IndexOf(Environment.GetCommandLineArgs(), "-todBuild") >= 0;
        const string Src = "Assets/Settings/PC_RPAsset.asset", Tod = "Assets/Settings/PC_RPAsset_TOD.asset";
        const string SetPath = "Assets/Scenes/Lean/OldTown-TOD-probes.asset", LightingPath = "Assets/Scenes/Lean/OldTown-TOD.lighting";
        const BindingFlags Inst = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        /// <summary>The project's pipeline asset with probe volumes on and scenario blending (made once, kept in sync on every call).</summary>
        static UniversalRenderPipelineAsset Asset()
        {
            if (!File.Exists(Path.Combine(Application.dataPath, "..", Tod))) AssetDatabase.CopyAsset(Src, Tod);
            var a = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Tod);
            var so = new SerializedObject(a);
            so.FindProperty("m_LightProbeSystem").intValue = 1;                     // Adaptive Probe Volumes
            so.FindProperty("m_SupportProbeVolumeScenarios").boolValue = true;        // lighting scenarios (blending needs both)
            so.FindProperty("m_SupportProbeVolumeScenarioBlending").boolValue = true;
            so.FindProperty("m_ProbeVolumeSHBands").intValue = 1;                   // L1: the cheaper evaluation
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(a); AssetDatabase.SaveAssetIfDirty(a);
            return a;
        }

        /// <summary>Every quality level on the probe-volume asset until disposed; the levels' own assets come back after (and the file is checked).</summary>
        public static IDisposable UsePipeline() => new Swap(Asset());

        sealed class Swap : IDisposable
        {
            readonly Object qs; readonly Object[] was; readonly string text;
            public Swap(RenderPipelineAsset tod)
            {
                text = File.ReadAllText(Path.Combine(Application.dataPath, "../ProjectSettings/QualitySettings.asset"));
                qs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0];
                var so = new SerializedObject(qs); var levels = so.FindProperty("m_QualitySettings"); was = new Object[levels.arraySize];
                // the levels' own assets, read from the file when the serialized object gives none (batch mode can read a null reference here, as
                // OldTownSwap's proof restore found): a null must never be what goes back
                var guids = System.Text.RegularExpressions.Regex.Matches(text, @"customRenderPipeline: \{fileID: \d+, guid: ([0-9a-f]{32})").Select(m => m.Groups[1].Value).ToArray();
                for (int i = 0; i < levels.arraySize; i++)
                {
                    var p = levels.GetArrayElementAtIndex(i).FindPropertyRelative("customRenderPipeline"); was[i] = p.objectReferenceValue;
                    if (!was[i] && i < guids.Length) was[i] = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guids[i]));
                    p.objectReferenceValue = tod;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"TimeOfDay097Bake: quality levels on {tod.name} (was {string.Join(", ", was.Select(w => w ? w.name : "none"))}); active {GraphicsSettings.currentRenderPipeline?.name}");
            }
            public void Dispose()
            {
                var so = new SerializedObject(qs); var levels = so.FindProperty("m_QualitySettings");
                for (int i = 0; i < levels.arraySize && i < was.Length; i++) levels.GetArrayElementAtIndex(i).FindPropertyRelative("customRenderPipeline").objectReferenceValue = was[i];
                // saved at once: a save inside the bake (the baking set's) already wrote the swapped levels to disk, so a crash before the editor's own
                // save at exit must not leave the project on the probe-volume asset
                so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(qs); AssetDatabase.SaveAssets();
                var path = Path.Combine(Application.dataPath, "../ProjectSettings/QualitySettings.asset");
                var now = File.ReadAllText(path);
                if (now != text) { File.WriteAllText(path, text); now = File.ReadAllText(path); } // the levels' own assets, byte for byte as before
                Debug.Log($"TimeOfDay097Bake: quality levels back on {string.Join(", ", was.Select(w => w ? w.name : "none"))}; QualitySettings.asset {(now == text ? "as it was" : "DIFFERS from before (check it)")}");
            }
        }

        /// <summary>The lean scene lit by probe volumes and baked at every key time. Returns a summary line.</summary>
        public static string Make()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != LeanScene.LeanPath) throw new InvalidOperationException($"TimeOfDay097Bake: only on the lean copy ({LeanScene.LeanPath}), not {scene.path}");
            var t0 = DateTime.UtcNow; string line;
            using (UsePipeline())
            {
                WakePipeline();
                var prv = ProbeReferenceVolume.instance;
                Debug.Log($"TimeOfDay097Bake: probe volumes initialised {Get(prv, "isInitialized")}, enabled by the pipeline {Get(prv, "enabledBySRP")}");
                var look = Object.FindAnyObjectByType<OldTownPlayLook>(FindObjectsInactive.Include);
                if (!look || !look.sun) throw new InvalidOperationException("TimeOfDay097Bake: no OldTownPlayLook with a sun in the scene");
                var b = TimeOfDay097.BaseFrom(look.sunRotation, look.sunColour, look.sunIntensity, look.ambientSky, look.ambientEquator, look.ambientGround);
                int switched = Scene(); var set = BakingSet(scene, out var created);
                var sun = look.sun; var rot0 = sun.transform.rotation; var col0 = sun.color; float int0 = sun.intensity;
                var amb = (RenderSettings.ambientMode, RenderSettings.ambientSkyColor, RenderSettings.ambientEquatorColor, RenderSettings.ambientGroundColor, RenderSettings.ambientIntensity);
                var field = typeof(ProbeVolumeBakingSet).GetField("lightingScenario", Inst);
                var report = new System.Collections.Generic.List<string>();
                try
                {
                    foreach (var (name, hour) in TimeOfDay097.Keys)
                    {
                        field.SetValue(set, name); EditorUtility.SetDirty(set);
                        var s = TimeOfDay097.Evaluate(hour, b);
                        sun.transform.rotation = s.rot; sun.color = s.sun; sun.intensity = s.intensity;
                        RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = s.sky; RenderSettings.ambientEquatorColor = s.eq; RenderSettings.ambientGroundColor = s.ground; RenderSettings.ambientIntensity = 1f;
                        var ts = DateTime.UtcNow; bool ok = Lightmapping.Bake();
                        report.Add($"{name} {hour:0.0} h (sun {s.elevation:0}°{(s.moon ? " moon" : "")} x{s.intensity:0.00}) {(ok ? "baked" : "FAILED")} in {(DateTime.UtcNow - ts).TotalSeconds:F0} s");
                        Debug.Log("TimeOfDay097Bake: " + report[report.Count - 1]);
                    }
                }
                finally
                {
                    sun.transform.rotation = rot0; sun.color = col0; sun.intensity = int0;
                    (RenderSettings.ambientMode, RenderSettings.ambientSkyColor, RenderSettings.ambientEquatorColor, RenderSettings.ambientGroundColor, RenderSettings.ambientIntensity) = amb;
                }
                field.SetValue(set, TimeOfDay097.Keys[0].name); EditorUtility.SetDirty(set);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                line = $"TimeOfDay097Bake: {switched} renderers from lightmaps to probe volumes; baking set {(created ? "made" : "reused")} ({set.lightingScenarios.Count} scenarios: {string.Join("/", set.lightingScenarios)}, " +
                       $"spacing {set.minDistanceBetweenProbes} m, sky occlusion {set.skyOcclusion}); {string.Join("; ", report)}; {(DateTime.UtcNow - t0).TotalSeconds:F0} s in all";
            }
            return line;
        }

        /// <summary>Bake only (no player): the played scene, its lean copy (-lean m, default 30) and the time-of-day bake, then exit. For checking the bake
        /// before a build's long first shader compile.  Unity -batchmode -quit -executeMethod Suborned.World.Play.TimeOfDay097Bake.CliBake -lean 30</summary>
        public static void CliBake()
        {
            try
            {
                EditorSceneManager.OpenScene(OldTownPlay.ScenePath, OpenSceneMode.Single);
                Debug.Log(Look097Materials.Apply());
                LeanScene.Make(LeanScene.MarginFromArgs() >= 0f ? LeanScene.MarginFromArgs() : 30f);
                Debug.Log(Make());
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static object Get(object o, string member) { var t = o.GetType(); return t.GetProperty(member, Inst)?.GetValue(o) ?? t.GetField(member, Inst)?.GetValue(o) ?? "?"; }

        /// <summary>URP builds its pipeline (and the probe-volume system with it) on the first render under the swapped asset: render once, off screen.</summary>
        static void WakePipeline()
        {
            var rt = new RenderTexture(64, 64, 24); var go = new GameObject("TimeOfDay097 wake"); var cam = go.AddComponent<Camera>();
            cam.targetTexture = rt; cam.enabled = false; cam.Render(); cam.targetTexture = null; Object.DestroyImmediate(go); rt.Release(); Object.DestroyImmediate(rt);
        }

        /// <summary>The lean scene for probe volumes: every lightmapped renderer to probes (they keep contributing to the bake), the old light-probe
        /// group gone (probe volumes replace it), its own lighting settings (never the real scene's), and one local probe volume over the kept area.</summary>
        static int Scene()
        {
            int n = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include)) if (r.receiveGI == ReceiveGI.Lightmaps) { r.receiveGI = ReceiveGI.LightProbes; n++; }
            foreach (var g in Object.FindObjectsByType<LightProbeGroup>(FindObjectsInactive.Include)) Object.DestroyImmediate(g.gameObject);
            if (File.Exists(Path.Combine(Application.dataPath, "..", LightingPath))) AssetDatabase.DeleteAsset(LightingPath);
            var ls = Object.Instantiate(Lightmapping.lightingSettings); AssetDatabase.CreateAsset(ls, LightingPath); Lightmapping.lightingSettings = ls;
            ls.bakedGI = true; ls.realtimeGI = false; ls.mixedBakeMode = MixedLightingMode.IndirectOnly; ls.indirectScale = ZoneBake097.Tune("bounce", 1f);
            float m = Mathf.Max(LeanScene.MarginFromArgs(), 0f); var z = LeanScene.ZoneXZ;
            foreach (var old in Object.FindObjectsByType<ProbeVolume>(FindObjectsInactive.Include)) Object.DestroyImmediate(old.gameObject);
            var pv = new GameObject("Probe volume (brief 097 time of day)").AddComponent<ProbeVolume>();
            pv.mode = ProbeVolume.Mode.Local; pv.transform.position = new Vector3(z.center.x, 30f, z.center.y); pv.size = new Vector3(z.width + 2f * m, 64f, z.height + 2f * m);
            return n;
        }

        static ProbeVolumeBakingSet BakingSet(UnityEngine.SceneManagement.Scene scene, out bool created)
        {
            if (File.Exists(Path.Combine(Application.dataPath, "..", SetPath))) AssetDatabase.DeleteAsset(SetPath);
            var set = ScriptableObject.CreateInstance<ProbeVolumeBakingSet>(); created = true;
            typeof(ProbeVolumeBakingSet).GetMethod("SetDefaults", Inst)?.Invoke(set, null);
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-todProbe");
            set.minDistanceBetweenProbes = i >= 0 && i + 1 < a.Length && float.TryParse(a[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 1.5f;
            set.skyOcclusion = true; set.skyOcclusionShadingDirection = true;
            AssetDatabase.CreateAsset(set, SetPath);
            var guid = AssetDatabase.AssetPathToGUID(scene.path);
            if (!set.TryAddScene(guid)) typeof(ProbeVolumeBakingSet).GetMethod("AddScene", Inst)?.Invoke(set, new object[] { guid, null });
            var first = set.lightingScenarios.FirstOrDefault();
            if (first != null && first != TimeOfDay097.Keys[0].name) set.RenameScenario(first, TimeOfDay097.Keys[0].name);
            foreach (var (name, _) in TimeOfDay097.Keys) if (!set.lightingScenarios.Contains(name)) set.TryAddScenario(name);
            EditorUtility.SetDirty(set); AssetDatabase.SaveAssets();
            return set;
        }
    }
}
