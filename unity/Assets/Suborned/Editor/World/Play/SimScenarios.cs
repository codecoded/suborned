using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Suborned.Play;
using Suborned.Sim;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using Path = System.IO.Path;
using Debug = UnityEngine.Debug;

namespace Suborned.World.Play
{
    /// <summary>§11 in Unity (brief 011 D): the engine's scenarios (the 28 and the N-cases) played by the DLL Unity loaded, on the validated inputs,
    /// against the headless CLI's run of the same build (headless.json from `bun tools/sim/unity.ts`): every checkpoint hash must match, and each
    /// run must replay identically from its start state and order log.
    ///   Suborned/Sim/Run Scenarios (Editor, Mono): out/sim-unity/unity-editor.json
    ///   Suborned/Sim/Mono and IL2CPP Players: builds two macOS (Apple silicon) players of an empty scene with the bundle in StreamingAssets, one
    ///     per scripting backend, runs each with -sim-check, and compares both with headless: out/sim-unity/runtimes.json. The project's scripting
    ///     backend and macOS architecture are restored afterwards.
    /// Batchmode: -executeMethod Suborned.World.Play.SimScenarios.RunBatch | PlayersBatch</summary>
    public static class SimScenarios
    {
        static string Out => Path.Combine(HarbourWorldBuilder.RepoRoot, "out", "sim-unity");

        [MenuItem("Suborned/Sim/Run Scenarios (Unity vs headless)")]
        public static void RunMenu() => HarbourWorldBuilder.Batch(() => Run());

        public static void RunBatch() => HarbourWorldBuilder.Batch(() => { if (!Run()) EditorApplication.Exit(1); });

        /// <summary>Plays every scenario in the Editor's runtime and writes the comparison. True when every scenario matches headless.</summary>
        public static bool Run()
        {
            var b = SimBundle.Load();
            CheckDll(b);
            var head = SimBundle.Headless();
            var rows = SimCheck.RunAll(b.Inputs, head);
            var rangeRows = SimCheck.RunRange(head); rows.AddRange(rangeRows); // brief 029: R01..R10 on the range's own inputs
            Directory.CreateDirectory(Out);
            var file = Path.Combine(Out, "unity-editor.json");
            File.WriteAllText(file, SimCheck.Json(rows, b, "editor") + "\n");
            int match = rows.Count(r => r.Match), same = rows.Count(r => r.ReplayIdentical), ok = rows.Count(r => r.ChecksFailed == 0 && r.Why.Length == 0);
            var msg = $"Sim: {match}/{rows.Count} scenarios ({rangeRows.Count} of them the fight range) match headless at every checkpoint, {same}/{rows.Count} replay identically, {ok}/{rows.Count} pass their checks; Editor {SimBundle.Runtime}, engine {b.Source}, DLL {b.DllSha[..12]}, {rows.Sum(r => r.Ms):F0} ms → {file}";
            if (SimCheck.Passed(rows)) Debug.Log(msg);
            else Debug.LogError(msg + "\n" + string.Join("\n", rows.Where(r => !r.Match || !r.ReplayIdentical || r.Why.Length > 0).Select(r => $"{r.Id}: {r.Why}")));
            return SimCheck.Passed(rows);
        }

        /// <summary>The DLL Unity loaded is the bundle's (the bytes the CLI ran): the project holds one Suborned.Sim plugin, with those bytes.</summary>
        internal static void CheckDll(SimBundle.Loaded b)
        {
            var plugins = PluginImporter.GetAllImporters().Where(p => Path.GetFileName(p.assetPath) == "Suborned.Sim.dll").ToList();
            if (plugins.Count != 1) throw new InvalidDataException($"Sim: {plugins.Count} Suborned.Sim.dll plugins in the project ({string.Join(", ", plugins.Select(p => p.assetPath))}); there must be one");
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", plugins[0].assetPath));
            var sha = SimBundle.Sha(File.ReadAllBytes(path));
            if (sha != b.DllSha) throw new InvalidDataException($"Sim: Unity loaded {path} sha256 {sha[..12]}, the bundle's engine is {b.DllSha[..12]}; run `bun tools/sim/unity.ts` and let Unity reimport");
        }

        [MenuItem("Suborned/Sim/Mono and IL2CPP Players")]
        public static void PlayersMenu() => HarbourWorldBuilder.Batch(() => Players());

        public static void PlayersBatch() => HarbourWorldBuilder.Batch(() => { if (!Players()) EditorApplication.Exit(1); });

        public static bool Players()
        {
            var b = SimBundle.Load(); CheckDll(b);
            if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Sim: save the open scene first (the player check opens an empty scene)");
            var target = NamedBuildTarget.Standalone;
            var backend = PlayerSettings.GetScriptingBackend(target);
            var arch = UnityEditor.OSXStandalone.UserBuildSettings.architecture;
            var scenePath = "Assets/Scenes/probe-simcheck.unity"; // git-ignored (probe-*)
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var results = new List<(string backend, string file, string json, int exit)>();
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, scenePath);
                UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.ARM64;
                foreach (var (name, impl) in new[] { ("mono", ScriptingImplementation.Mono2x), ("il2cpp", ScriptingImplementation.IL2CPP) })
                {
                    PlayerSettings.SetScriptingBackend(target, impl);
                    var dir = Path.Combine(Out, "players", name); Directory.CreateDirectory(dir);
                    var app = Path.Combine(dir, "SimCheck.app");
                    var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { scenePath }, locationPathName = app, target = BuildTarget.StandaloneOSX, options = BuildOptions.None });
                    if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new IOException($"Sim: the {name} player did not build: {report.summary.result} ({report.summary.totalErrors} errors)");
                    var exe = Directory.GetFiles(Path.Combine(app, "Contents", "MacOS")).First();
                    var outFile = Path.Combine(Out, $"player-{name}.json"); if (File.Exists(outFile)) File.Delete(outFile);
                    var p = Process.Start(new ProcessStartInfo(exe, $"-batchmode -nographics -sim-check \"{outFile}\" -logFile \"{Path.Combine(dir, "player.log")}\"") { UseShellExecute = false });
                    if (!p.WaitForExit(300000)) { p.Kill(); throw new TimeoutException($"Sim: the {name} player ran over 5 min"); }
                    results.Add((name, outFile, File.Exists(outFile) ? File.ReadAllText(outFile) : "", p.ExitCode));
                }
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(target, backend);
                UnityEditor.OSXStandalone.UserBuildSettings.architecture = arch;
                AssetDatabase.SaveAssets();
                if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            // headless vs each player: every checkpoint of every scenario
            var head = SimBundle.Headless();
            var want = head["scenarios"].Items.ToDictionary(s => s["id"].Str(), s => s["finalHash"].Str());
            var w = new JWriter().Obj().Prop("schema", "suborned.sim.runtimes/1").Prop("commit", b.Commit).Prop("dllSha256", b.DllSha).OpenArr("players");
            bool all = true; var lines = new List<string>();
            foreach (var (name, file, json, exit) in results)
            {
                var n = json.Length > 0 ? JNode.Parse(json, file) : null;
                bool passed = n != null && n.Opt("passed")?.Bool() == true && exit == 0;
                int matching = n?.Opt("matching")?.Int() ?? 0, count = n?.Opt("scenarios")?.Int() ?? 0;
                string runtime = n?.Opt("runtime")?.Str() ?? "?";
                all &= passed && runtime == (name == "mono" ? "Mono" : "IL2CPP");
                lines.Add($"{name} player ({runtime}): {matching}/{count} scenarios match headless at every checkpoint, exit {exit}");
                w.Obj().Prop("backend", name).Prop("runtime", runtime).Prop("exit", exit).Prop("passed", passed).Prop("matching", matching).Prop("scenarios", count).Prop("report", Path.GetFileName(file)).EndObj();
            }
            w.EndArr().Prop("passed", all).EndObj();
            File.WriteAllText(Path.Combine(Out, "runtimes.json"), w + "\n");
            var msg = $"Sim: Mono and IL2CPP players against headless ({want.Count} scenarios): {string.Join("; ", lines)} → {Path.Combine(Out, "runtimes.json")}";
            if (all) Debug.Log(msg); else Debug.LogError(msg);
            return all;
        }
    }
}
