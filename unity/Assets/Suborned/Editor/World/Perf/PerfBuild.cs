using System;
using System.IO;
using Suborned.Perf;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World.Perf
{
    /// <summary>The frame-time player (brief 008 B6): writes the plans (PerfPlanner: out/build/perf/plan.json for the stops, passes
    /// and variants; sweep.json for the grid; refine.json round the heaviest foci of sweep.result.json) and builds a macOS
    /// development player of the HarbourDistrict scene alone into out/build/perf/SubornedPerf.app. Only BuildPlayerOptions carry
    /// the build's settings (development, no profiler autoconnect): EditorBuildSettings and EditorUserBuildSettings are left as
    /// they are; for the build alone the project carries a renderer copy that keeps the A/B switches' shader variants
    /// (PerfVariantsRenderer), taken out after. The shadow proxies are the scene's own (ShadowProxies, built with the world). The player runs any plan (PerfHarness, `-perf`). Menu: Suborned/Perf/* (a Write rewrites a plan alone: the player
    /// reads it at start, so a stop change needs no rebuild), with the scene open and saved. CLI (Editor closed):
    /// Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Perf.PerfBuild.Cli [-perfSweep | -perfRefine | -perfPeople].
    /// The scene's people layer is stripped from the player unless -perfPeople (menu: Build Perf Player (with people)): PerfStrip.</summary>
    public static class PerfBuild
    {
        const string Menu = "Suborned/Perf/";
        public static string Dir => Path.Combine(HarbourWorldBuilder.RepoRoot, "out", "build", "perf");
        public static string PlanPath => Path.Combine(Dir, "plan.json");
        public static string SweepPath => Path.Combine(Dir, "sweep.json");
        public static string RefinePath => Path.Combine(Dir, "refine.json");
        public static string SweepResultPath => Path.Combine(Dir, "sweep.result.json");
        public static string AppPath => Path.Combine(Dir, "SubornedPerf.app");

        [MenuItem(Menu + "Build Perf Player")]
        static void BuildMenu() => Later(() => Build());

        [MenuItem(Menu + "Build Perf Player (with people)")]
        static void BuildPeopleMenu() => Later(() => Build(people: true));

        [MenuItem(Menu + "Write Plan")]
        static void PlanMenu() => Later(() => Write(PlanPath, Without(PerfPlanner.Stops)));

        [MenuItem(Menu + "Write Sweep Plan")]
        static void SweepMenu() => Later(() => Write(SweepPath, Without(PerfPlanner.Sweep)));

        [MenuItem(Menu + "Write Refine Plan (from sweep.result.json)")]
        static void RefineMenu() => Later(() => Write(RefinePath, Without(() => PerfPlanner.Refine(SweepResultPath))));

        /// <summary>A plan for the default perf player (people stripped): the plan must describe the player it is run in.</summary>
        static PerfPlan Without(Func<PerfPlan> make)
        {
            PerfStrip.Active = true;
            try { return make(); } finally { PerfStrip.Active = false; }
        }

        public static void Cli()
        {
            int code = 1;
            try
            {
                var args = Environment.GetCommandLineArgs();
                string scene = Arg(args, "-perfScene") ?? HarbourWorldBuilder.ScenePath;
                if (EditorSceneManager.GetActiveScene().path != scene) EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
                if (Arg(args, "-perfSpec") is { } spec) code = Build075(scene, spec, args);
                else if (scene != HarbourWorldBuilder.ScenePath) throw new ArgumentException($"Perf: -perfScene {scene} needs -perfSpec (a plan-coordinate spec, Resolve075)");
                else if (Array.IndexOf(args, "-perfSweep") >= 0) { Write(SweepPath, Without(PerfPlanner.Sweep)); code = 0; }
                else if (Array.IndexOf(args, "-perfRefine") >= 0) { Write(RefinePath, Without(() => PerfPlanner.Refine(SweepResultPath))); code = 0; }
                else code = Build(Array.IndexOf(args, "-perfPeople") >= 0).summary.result == BuildResult.Succeeded ? 0 : 1;
            }
            catch (Exception e) { Debug.LogException(e); }
            EditorApplication.Exit(code);
        }

        static string Arg(string[] args, string k) { int i = Array.IndexOf(args, k); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }

        /// <summary>Brief 075 Perf A/B, item B (Redline): `-perfScene &lt;path&gt; -perfSpec &lt;spec&gt; [-perfResolved &lt;out&gt;] [-perfApp &lt;name.app&gt;] [-perfResolveOnly]`.
        /// Resolves the plan-coordinate spec against the scene (PerfPlanner.Resolve075) and writes it to -perfResolved (default beside the spec,
        /// &lt;spec&gt;.&lt;ours|old&gt;.resolved.json); unless -perfResolveOnly, builds a development player of that scene alone into out/build/perf/&lt;app&gt;
        /// (default "The Suborned Perf Ours.app" / "The Suborned Perf OldTown.app"), people layer stripped (PerfStrip). The old town builds
        /// inside OldTownSwap.WithOldSettings (the old PC_RPAsset as the quality level's pipeline, lodBias 2, realtime probes on, mip streaming
        /// off, in memory, both settings files byte-checked), with our pipeline as GraphicsSettings' default for the build, so the player
        /// carries both assets and both sets of variants (the `pipeline` switch swaps QualitySettings.renderPipeline between them).</summary>
        static int Build075(string scene, string specPath, string[] args)
        {
            bool old = scene == PerfPlanner.OldTownScene;
            specPath = Path.GetFullPath(Path.IsPathRooted(specPath) ? specPath : Path.Combine(HarbourWorldBuilder.RepoRoot, specPath));
            string resolved = Arg(args, "-perfResolved") ?? Path.Combine(Path.GetDirectoryName(specPath), Path.GetFileNameWithoutExtension(specPath) + (old ? ".old" : ".ours") + ".resolved.json");
            if (!Path.IsPathRooted(resolved)) resolved = Path.Combine(HarbourWorldBuilder.RepoRoot, resolved);
            string app = Path.Combine(Dir, Arg(args, "-perfApp") ?? (old ? "The Suborned Perf OldTown.app" : "The Suborned Perf Ours.app"));
            bool resolveOnly = Array.IndexOf(args, "-perfResolveOnly") >= 0;
            int code = 1;
            var oursAsset = GraphicsSettings.defaultRenderPipeline;  // ours, read before the old swap replaces it
            void Body()
            {
                PerfStrip.Active = true;
                try
                {
                    var plan = PerfPlanner.Resolve075(specPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(resolved));
                    File.WriteAllText(resolved, JsonUtility.ToJson(plan, true));
                    Debug.Log($"Perf: resolved {specPath} on {scene} -> {resolved} ({plan.stops.Length} stops, {plan.variants.Length} variants [{string.Join(", ", Array.ConvertAll(plan.variants, v => v.name))}], camera {(string.IsNullOrEmpty(plan.camera) ? "cameras/play" : plan.camera)}, export {plan.exportSha256}, scene {plan.sceneFingerprint})");
                    if (resolveOnly) { code = 0; return; }
                    var ours = GraphicsSettings.defaultRenderPipeline;
                    if (old)
                    {
                        // WithOldSettings sets both to the old asset; our asset as the default keeps it (and its variants) in the build, the quality level's old asset draws
                        GraphicsSettings.defaultRenderPipeline = oursAsset ? oursAsset : throw new InvalidOperationException("Perf: no default pipeline (ours) to carry into the old-town player");
                    }
                    string variants = "";
                    BuildReport report;
                    try
                    {
                        if (!old) variants = PerfVariantsRenderer.Add();  // the old renderer's SSAO switch is not part of 075ab: its asset is left alone
                        report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { scene }, target = BuildTarget.StandaloneOSX, targetGroup = BuildTargetGroup.Standalone, locationPathName = app, options = BuildOptions.Development });
                    }
                    finally { if (!old) PerfVariantsRenderer.Remove(); if (old) GraphicsSettings.defaultRenderPipeline = ours; }
                    EditorUtility.SetDirty(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
                    AssetDatabase.SaveAssets();
                    var sum = report.summary;
                    var line = $"Perf 075ab: player {sum.result} in {sum.totalTime.TotalSeconds:F0}s, {sum.totalSize / 1048576} MB, {sum.totalErrors} errors -> {app}; scene {scene}; plan {resolved}; quality pipeline {(QualitySettings.renderPipeline ? QualitySettings.renderPipeline.name : "none")}; variants kept by {(old ? "both pipeline assets (quality: old, default: ours)" : variants)}";
                    File.WriteAllText(Path.Combine(Dir, old ? "build-075ab-old.txt" : "build-075ab-ours.txt"), line + "\n");
                    if (sum.result == BuildResult.Succeeded) { Debug.Log(line); code = 0; } else Debug.LogError(line);
                }
                finally { PerfStrip.Active = false; }
            }
            if (!old) { Body(); return code; }
            // the old render settings live in the swap assembly (it references this one): called by name
            var swap = Type.GetType("Suborned.OldTown.Editor.Swap.OldTownSwap, Suborned.OldTown.Swap.Editor") ?? throw new InvalidOperationException("Perf: OldTownSwap (Suborned.OldTown.Swap.Editor) not found");
            var with = swap.GetMethod("WithOldSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) ?? throw new MissingMethodException("OldTownSwap.WithOldSettings");
            with.Invoke(null, new object[] { (Action)Body });
            return code;
        }

        /// <summary>Run on the next editor update, after the menu returns (a build holds the main thread for minutes). Not delayCall:
        /// an unfocused Editor does not run it until it is focused again.</summary>
        static void Later(Action a)
        {
            EditorApplication.CallbackFunction once = null;
            once = () =>
            {
                EditorApplication.update -= once;
                try { a(); } catch (Exception e) { Debug.LogException(e); }
            };
            EditorApplication.update += once;
        }

        /// <summary>people: false (the default) strips the scene's people layer from the player (PerfStrip, Redline M3), true builds
        /// it in; either way the plan records the roots built, the roots stripped and the people count, and the player checks them.</summary>
        public static BuildReport Build(bool people = false)
        {
            PerfStrip.Active = !people;
            try { return BuildPlayer(); }
            finally { PerfStrip.Active = false; }
        }

        static BuildReport BuildPlayer()
        {
            var plan = PerfPlanner.Stops();
            Write(PlanPath, plan);
            BuildReport report;
            int proxies = GameObject.Find(HarbourWorldBuilder.RootName).GetComponentsInChildren<ShadowProxy>(true).Length;
            string variants = "";
            try
            {
                variants = PerfVariantsRenderer.Add();
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { HarbourWorldBuilder.ScenePath },
                    target = BuildTarget.StandaloneOSX,
                    targetGroup = BuildTargetGroup.Standalone,
                    locationPathName = AppPath,
                    options = BuildOptions.Development,
                });
            }
            finally { PerfVariantsRenderer.Remove(); }
            // The Input System puts its project-wide actions into PlayerSettings.preloadedAssets for the build and takes them out after,
            // but the build saves ProjectSettings.asset in between: save PlayerSettings again so the file is the project as it is.
            EditorUtility.SetDirty(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
            AssetDatabase.SaveAssets();
            var s = report.summary;
            var line = $"Perf: player {s.result} in {s.totalTime.TotalSeconds:F0}s, {s.totalSize / 1048576} MB, {s.totalErrors} errors, {s.totalWarnings} warnings -> {AppPath}; " +
                $"plan {PlanPath} ({plan.stops.Length} stops, fingerprint {plan.fingerprint}, scene {plan.sceneFingerprint}, export {plan.exportSha256}); people built {plan.people}, stripped [{string.Join("; ", plan.stripped)}]; {proxies} shadow proxies in the scene; variants kept by {variants}";
            File.WriteAllText(Path.Combine(Dir, "build.txt"), line + "\n");
            if (s.result == BuildResult.Succeeded) Debug.Log(line); else Debug.LogError(line);
            return report;
        }

        static void Write(string path, PerfPlan plan)
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(path, JsonUtility.ToJson(plan, true));
            Debug.Log($"Perf: wrote {path} ({plan.stops.Length} stops)");
        }
    }
}
