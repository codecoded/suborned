using System;
using System.Collections.Generic;
using System.IO;
using Suborned.Perf;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.World.Perf
{
    /// <summary>The frame-time player (brief 008 B6): resolves perf-stops.json against the built HarbourDistrict scene (stand-offs by
    /// Rig.StandOff with the play camera's rotation and zoom, checked against the built camera; the scene's fingerprint and export
    /// sha256) into out/build/perf/plan.json, and builds a macOS development player of that scene alone into
    /// out/build/perf/SubornedPerf.app. Only BuildPlayerOptions carry the build's settings (development, no profiler autoconnect):
    /// EditorBuildSettings and EditorUserBuildSettings are left as they are. The sweep plan (out/build/perf/sweep.json) frames a grid
    /// of street foci briefly, to find the heaviest frame; the player runs either plan (PerfHarness, `-perf`).
    /// Menu: Suborned/Perf/* (Write Plan rewrites plan.json alone: the player reads it at start, so a stop change needs no rebuild),
    /// with the scene open and saved. CLI (Editor closed):
    /// Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Perf.PerfBuild.Cli [-perfSweep]</summary>
    public static class PerfBuild
    {
        const string Menu = "Suborned/Perf/";
        public static string Dir => Path.Combine(HarbourWorldBuilder.RepoRoot, "out", "build", "perf");
        public static string PlanPath => Path.Combine(Dir, "plan.json");
        public static string SweepPath => Path.Combine(Dir, "sweep.json");
        public static string AppPath => Path.Combine(Dir, "SubornedPerf.app");
        static string SpecPath => Path.Combine(Application.dataPath, "Suborned", "Editor", "World", "Perf", "perf-stops.json");

        [MenuItem(Menu + "Build Perf Player")]
        static void BuildMenu() => Later(() => Build());

        [MenuItem(Menu + "Write Plan")]
        static void PlanMenu() => Later(() => Write(PlanPath, Stops()));

        [MenuItem(Menu + "Write Sweep Plan")]
        static void SweepMenu() => Later(() => Write(SweepPath, Sweep()));

        public static void Cli()
        {
            int code = 1;
            try
            {
                if (EditorSceneManager.GetActiveScene().path != HarbourWorldBuilder.ScenePath) EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Single);
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-perfSweep") >= 0) { Write(SweepPath, Sweep()); code = 0; }
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
            var plan = Stops();
            Write(PlanPath, plan);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { HarbourWorldBuilder.ScenePath },
                target = BuildTarget.StandaloneOSX,
                targetGroup = BuildTargetGroup.Standalone,
                locationPathName = AppPath,
                options = BuildOptions.Development,
            });
            // The Input System puts its project-wide actions into PlayerSettings.preloadedAssets for the build and takes them out after,
            // but the build saves ProjectSettings.asset in between: save PlayerSettings again so the file is the project as it is.
            EditorUtility.SetDirty(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
            AssetDatabase.SaveAssets();
            var s = report.summary;
            var line = $"Perf: player {s.result} in {s.totalTime.TotalSeconds:F0}s, {s.totalSize / 1048576} MB, {s.totalErrors} errors, {s.totalWarnings} warnings -> {AppPath}; " +
                $"plan {PlanPath} ({plan.stops.Length} stops, fingerprint {plan.fingerprint}, export {plan.exportSha256})";
            File.WriteAllText(Path.Combine(Dir, "build.txt"), line + "\n");
            if (s.result == BuildResult.Succeeded) Debug.Log(line); else Debug.LogError(line);
            return report;
        }

        /// <summary>The named stops (perf-stops.json) with the run's timings.</summary>
        public static PerfPlan Stops()
        {
            var spec = Spec();
            var stops = new List<(string, string, Vector3, float)>();
            foreach (var s in spec.stops) stops.Add((s.name, s.why, Vec(s.focus, s.name), s.zoom));
            return Plan(spec, stops, spec.warmupSeconds, spec.dwellSeconds, spec.settleSeconds, spec.recordSeconds);
        }

        /// <summary>A grid of street foci at the play zoom, each framed briefly, no warm-up.</summary>
        public static PerfPlan Sweep()
        {
            var spec = Spec(); var w = spec.sweep ?? throw new InvalidDataException("Perf: perf-stops.json has no sweep");
            var stops = new List<(string, string, Vector3, float)>();
            for (float z = w.z[0]; z <= w.z[1] + 1e-3f; z += w.step)
                for (float x = w.x[0]; x <= w.x[1] + 1e-3f; x += w.step)
                    stops.Add(($"sweep {x:0},{z:0}", "grid focus on the street datum", new Vector3(x, 0f, z), 0f));
            return Plan(spec, stops, 0f, 0f, w.settleSeconds, w.recordSeconds);
        }

        static PerfPlan Plan(SpecDto spec, List<(string name, string why, Vector3 focus, float zoom)> stops, float warm, float dwell, float settle, float record)
        {
            var (root, cam) = Scene();
            var ts = cam.GetComponent<TacticalShadows>(); var af = cam.GetComponent<ArchitectureFade>();
            var rot = cam.transform.rotation; float aspect = (float)spec.width / spec.height;
            float home = Rig.StandOff(rot, af.focus, cam.orthographicSize, aspect);
            if (Mathf.Abs(home - ts.focusDepth) > 0.01f)
                throw new InvalidOperationException($"Perf: Rig.StandOff at the play focus is {home:F3} m, the built play camera stands {ts.focusDepth:F3} m back: the stops would not use the real rig");
            if (!root.TryGetComponent<WorldStamp>(out var stamp)) throw new InvalidOperationException("Perf: the world root has no WorldStamp");
            var cameras = GameObject.Find("cameras").transform;
            var plan = new PerfPlan
            {
                fingerprint = Inspect.Fingerprint(root, GameObject.Find("batches").transform, cameras),
                exportSha256 = stamp.exportSha256, source = stamp.source, scene = HarbourWorldBuilder.ScenePath, content = spec.content,
                pitch = rot.eulerAngles.x, yaw = rot.eulerAngles.y, aspect = aspect, farMargin = cam.farClipPlane - ts.focusDepth,
                width = spec.width, height = spec.height,
                warmupSeconds = warm, dwellSeconds = dwell, settleSeconds = settle, recordSeconds = record,
                stops = new PerfStop[stops.Count],
            };
            for (int i = 0; i < stops.Count; i++)
            {
                var (name, why, focus, zoom) = stops[i];
                float z = zoom > 0f ? zoom : cam.orthographicSize;
                plan.stops[i] = new PerfStop { name = name, why = why, focus = focus, zoom = z, standOff = Rig.StandOff(rot, focus, z, aspect) };
            }
            return plan;
        }

        /// <summary>The built scene, open and saved (a build reads the file; the plan must describe the same scene).</summary>
        static (Transform root, Camera play) Scene()
        {
            if (Application.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Perf: not in Play Mode or while compiling");
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != HarbourWorldBuilder.ScenePath) throw new InvalidOperationException($"Perf: open {HarbourWorldBuilder.ScenePath} first (active: {scene.path})");
            if (scene.isDirty) throw new InvalidOperationException("Perf: the scene has unsaved changes; rebuild or save it first");
            var root = GameObject.Find(HarbourWorldBuilder.RootName) ?? throw new InvalidOperationException("Perf: no world root");
            var play = GameObject.Find("cameras/play");
            if (!play || !play.TryGetComponent<Camera>(out var cam) || !play.GetComponent<TacticalShadows>() || !play.GetComponent<ArchitectureFade>())
                throw new InvalidOperationException("Perf: cameras/play is missing its Camera, TacticalShadows or ArchitectureFade");
            return (root.transform, cam);
        }

        static SpecDto Spec()
        {
            var spec = JsonUtility.FromJson<SpecDto>(File.ReadAllText(SpecPath));
            if (spec.stops == null || spec.stops.Length == 0 || spec.width <= 0 || spec.height <= 0) throw new InvalidDataException($"Perf: {SpecPath} needs stops, width and height");
            return spec;
        }

        static Vector3 Vec(float[] a, string stop) => a is { Length: 3 } ? new Vector3(a[0], a[1], a[2]) : throw new InvalidDataException($"Perf: stop `{stop}` focus is not [x, y, z]");

        static void Write(string path, PerfPlan plan)
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(path, JsonUtility.ToJson(plan, true));
            Debug.Log($"Perf: wrote {path} ({plan.stops.Length} stops)");
        }

        [Serializable]
        sealed class SpecDto
        {
            public string note, content;
            public int width, height;
            public float warmupSeconds, dwellSeconds, settleSeconds, recordSeconds;
            public StopDto[] stops;
            public SweepDto sweep;
        }

        [Serializable]
        sealed class StopDto { public string name, why; public float[] focus; public float zoom; }

        [Serializable]
        sealed class SweepDto { public string note; public float step, settleSeconds, recordSeconds; public float[] x, z; }
    }
}
