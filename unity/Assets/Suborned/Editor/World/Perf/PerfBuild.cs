using System;
using System.IO;
using Suborned.Perf;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.World.Perf
{
    /// <summary>The frame-time player (brief 008 B6): writes the plans (PerfPlanner: out/build/perf/plan.json for the stops, passes
    /// and variants; sweep.json for the grid; refine.json round the heaviest foci of sweep.result.json) and builds a macOS
    /// development player of the HarbourDistrict scene alone into out/build/perf/SubornedPerf.app. Only BuildPlayerOptions carry
    /// the build's settings (development, no profiler autoconnect): EditorBuildSettings and EditorUserBuildSettings are left as
    /// they are; for the build alone the project carries a renderer copy that keeps the A/B switches' shader variants
    /// (PerfVariantsRenderer), taken out after. The shadow proxies are the scene's own (ShadowProxies, built with the world). The player runs any plan (PerfHarness, `-perf`). Menu: Suborned/Perf/* (a Write rewrites a plan alone: the player
    /// reads it at start, so a stop change needs no rebuild), with the scene open and saved. CLI (Editor closed):
    /// Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Perf.PerfBuild.Cli [-perfSweep | -perfRefine]</summary>
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

        [MenuItem(Menu + "Write Plan")]
        static void PlanMenu() => Later(() => Write(PlanPath, PerfPlanner.Stops()));

        [MenuItem(Menu + "Write Sweep Plan")]
        static void SweepMenu() => Later(() => Write(SweepPath, PerfPlanner.Sweep()));

        [MenuItem(Menu + "Write Refine Plan (from sweep.result.json)")]
        static void RefineMenu() => Later(() => Write(RefinePath, PerfPlanner.Refine(SweepResultPath)));

        public static void Cli()
        {
            int code = 1;
            try
            {
                if (EditorSceneManager.GetActiveScene().path != HarbourWorldBuilder.ScenePath) EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Single);
                var args = Environment.GetCommandLineArgs();
                if (Array.IndexOf(args, "-perfSweep") >= 0) { Write(SweepPath, PerfPlanner.Sweep()); code = 0; }
                else if (Array.IndexOf(args, "-perfRefine") >= 0) { Write(RefinePath, PerfPlanner.Refine(SweepResultPath)); code = 0; }
                else code = Build().summary.result == BuildResult.Succeeded ? 0 : 1;
            }
            catch (Exception e) { Debug.LogException(e); }
            EditorApplication.Exit(code);
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

        public static BuildReport Build()
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
                $"plan {PlanPath} ({plan.stops.Length} stops, fingerprint {plan.fingerprint}, export {plan.exportSha256}); {proxies} shadow proxies in the scene; variants kept by {variants}";
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
