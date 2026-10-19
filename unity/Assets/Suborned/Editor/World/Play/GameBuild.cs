using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Suborned.Play;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Suborned.World.Play
{
    /// <summary>The game build (Aster, 2026-10-17): the HarbourDistrict scene with the Handover host installed (PlayInstall, as the Editor's
    /// Play uses it) and the engine bundle in StreamingAssets (the Suborned.Sim DLL is a plugin; StreamingAssets/sim carries engine.json,
    /// headless.json, the rulebook, people rules, text, site, people, provisional and takedown), a release macOS (Apple silicon) player in
    /// out/build/play/The Suborned.app. Only BuildPlayerOptions carry the build (EditorBuildSettings and the scene list are untouched); after
    /// the build the app is named "The Suborned" in its Info.plist, takes tools/game/app-icon.png as its icon, and is signed ad hoc again.
    /// build.json records the result; `bun run game` reads it. Menu: Suborned/Build Game. Batch (Editor closed):
    /// Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Play.GameBuild.Cli</summary>
    public static class GameBuild
    {
        public const string AppName = "The Suborned";
        public static string Dir => Path.Combine(HarbourWorldBuilder.RepoRoot, "out", "build", "play");
        public static string AppPath => Path.Combine(Dir, AppName + ".app");

        // A second player build in the same Editor domain ships a player whose URP pipeline fails to start (GPUResidentDrawer resources
        // null in GPUInstanceDataBuffer: a black screen; the build skips the shader pass, 1 warning and 1 MB lighter). The first build after
        // a domain reload is right every time (seen 2026-10-17, alternating over six builds). So the menu builds in a fresh domain: when this
        // one has built already it reloads scripts first and builds after the reload. Batch builds start fresh anyway.
        static bool builtThisDomain;
        const string AfterReloadKey = "Suborned.GameBuild.afterReload";

        [MenuItem("Suborned/Build Game")]
        static void Menu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Game: leave Play first (the build would end the session)"); return; }
            if (builtThisDomain) { SessionState.SetBool(AfterReloadKey, true); Debug.Log("Game: reloading scripts first (a fresh domain for the build)"); EditorUtility.RequestScriptReload(); return; }
            Later();
        }

        [InitializeOnLoadMethod]
        static void AfterReload()
        {
            if (!SessionState.GetBool(AfterReloadKey, false)) return;
            SessionState.SetBool(AfterReloadKey, false); Later();
        }

        static void Later()
        {
            EditorApplication.CallbackFunction once = null; // the next editor update, after the menu returns (a build holds the main thread)
            once = () => { EditorApplication.update -= once; try { Build(); } catch (Exception e) { Fail(e); } };
            EditorApplication.update += once;
        }

        public static void Cli()
        {
            int code = 1;
            try { code = Build() ? 0 : 1; } catch (Exception e) { Fail(e); }
            EditorApplication.Exit(code);
        }

        static void Fail(Exception e)
        {
            Debug.LogException(e);
            try { Directory.CreateDirectory(Dir); File.WriteAllText(Path.Combine(Dir, "build.json"), $"{{\"result\": \"Failed\", \"error\": \"{Esc(e.Message)}\", \"at\": \"{DateTime.UtcNow:o}\"}}\n"); } catch { }
        }

        public static bool Build()
        {
            var b = SimBundle.Load(); SimScenarios.CheckDll(b); // the DLL Unity builds is the bundle's
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != HarbourWorldBuilder.ScenePath)
            {
                if (active.isDirty) throw new InvalidOperationException($"Game: save or close {active.path} first (the build opens {HarbourWorldBuilder.ScenePath})");
                if (!File.Exists(Path.Combine(Application.dataPath, "..", HarbourWorldBuilder.ScenePath))) throw new FileNotFoundException($"Game: {HarbourWorldBuilder.ScenePath} is missing; build the world first");
                EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Single);
            }
            var scene = EditorSceneManager.GetActiveScene();
            if (GameObject.Find(PlayInstall.RootName) is null || Object.FindAnyObjectByType<HandoverHost>() is null) { PlayInstall.InstallOpen(true); EditorSceneManager.SaveScene(scene); }
            else if (scene.isDirty) EditorSceneManager.SaveScene(scene);

            Directory.CreateDirectory(Dir); builtThisDomain = true;
            var ver = Version.Next(); // Sky 2026-10-19: "need to be upping build numbers now": 0.2.<build>, counted in tools/game/version.json
            BuildReport report; var product = PlayerSettings.productName; bool timing = PlayerSettings.enableFrameTimingStats;
            var bundleVersion = PlayerSettings.bundleVersion; var buildNumber = PlayerSettings.macOS.buildNumber;
            try
            {
                PlayerSettings.bundleVersion = ver.Full; PlayerSettings.macOS.buildNumber = ver.Build.ToString(); // CFBundleShortVersionString, CFBundleVersion
                ver.WriteStamp(b); // StreamingAssets/build.json for this build only (BuildStamp reads it in the player)
                Version.CopyPresets(); // brief 025: briefs/025/presets ride along (StreamingAssets/presets, this build only)
                PlayerSettings.enableFrameTimingStats = true; // brief 020: the proof reads the GPU's frame time (FrameTimingManager); the project's setting comes back after
                PlayerSettings.productName = AppName; // the binary, the menu bar and the player's data folder; the project's own name comes back after
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { HarbourWorldBuilder.ScenePath },
                    target = BuildTarget.StandaloneOSX,
                    targetGroup = BuildTargetGroup.Standalone,
                    locationPathName = AppPath,
                    options = BuildOptions.None, // release
                });
            }
            finally { PlayerSettings.productName = product; PlayerSettings.enableFrameTimingStats = timing; PlayerSettings.bundleVersion = bundleVersion; PlayerSettings.macOS.buildNumber = buildNumber; Version.RemoveStamp(); Version.RemovePresets(); }
            // as PerfBuild: the Input System's preload round trip leaves PlayerSettings dirty; save it so the file is the project as it is
            EditorUtility.SetDirty(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
            AssetDatabase.SaveAssets();
            var s = report.summary; bool ok = s.result == BuildResult.Succeeded;
            string post = ok ? Finish(ver) : "";
            if (ok) ver.Commit(); // only a build that succeeded takes the number
            var line = $"Game: build {ver.Full} ({ver.Recording}, sim {BuildStamp.Sim}, {ver.Sha}{(ver.Dirty ? "+" : "")}) {s.result} in {s.totalTime.TotalSeconds:F0}s, {s.totalSize / 1048576} MB, {s.totalErrors} errors, {s.totalWarnings} warnings -> {AppPath}; engine {b.Source}, DLL {b.DllSha[..12]}; {post}";
            File.WriteAllText(Path.Combine(Dir, "build.json"),
                $"{{\n \"result\": \"{s.result}\",\n \"at\": \"{DateTime.UtcNow:o}\",\n \"seconds\": {s.totalTime.TotalSeconds:F0},\n \"app\": \"{Esc(AppPath)}\",\n \"version\": \"{ver.Full}\",\n \"build\": {ver.Build},\n \"recording\": \"{ver.Recording}\",\n \"sim\": \"{BuildStamp.Sim}\",\n \"sha\": \"{ver.Sha}\",\n \"dirty\": {(ver.Dirty ? "true" : "false")},\n \"engine\": \"{Esc(b.Source)}\",\n \"dllSha256\": \"{b.DllSha}\",\n \"errors\": {s.totalErrors},\n \"warnings\": {s.totalWarnings},\n \"post\": \"{Esc(post)}\"\n}}\n");
            if (ok) Debug.Log(line); else Debug.LogError(line);
            return ok;
        }

        /// <summary>The app's name and icon, then an ad hoc signature over the changed bundle.</summary>
        static string Finish(Version ver)
        {
            var plist = Path.Combine(AppPath, "Contents", "Info.plist");
            foreach (var (key, value) in new[] { ("CFBundleName", AppName), ("CFBundleDisplayName", AppName), ("CFBundleShortVersionString", ver.Full), ("CFBundleVersion", ver.Build.ToString()) }) // Finder's Version line: 0.2.<build>
                if (Run("/usr/libexec/PlistBuddy", $"-c \"Set :{key} {value}\" \"{plist}\"") != 0) Run("/usr/libexec/PlistBuddy", $"-c \"Add :{key} string {value}\" \"{plist}\"");
            string icon = "default icon";
            var png = Path.Combine(HarbourWorldBuilder.RepoRoot, "tools", "game", "app-icon.png");
            var res = Path.Combine(AppPath, "Contents", "Resources", "PlayerIcon.icns");
            if (File.Exists(png)) // Unity names PlayerIcon.icns in the plist but writes none when the project has no icon
            {
                var set = Path.Combine(Dir, "AppIcon.iconset"); if (Directory.Exists(set)) Directory.Delete(set, true); Directory.CreateDirectory(set);
                foreach (int px in new[] { 16, 32, 128, 256, 512 })
                {
                    Run("/usr/bin/sips", $"-z {px} {px} \"{png}\" --out \"{Path.Combine(set, $"icon_{px}x{px}.png")}\"");
                    Run("/usr/bin/sips", $"-z {px * 2} {px * 2} \"{png}\" --out \"{Path.Combine(set, $"icon_{px}x{px}@2x.png")}\"");
                }
                var icns = Path.Combine(Dir, "AppIcon.icns");
                if (Run("/usr/bin/iconutil", $"-c icns \"{set}\" -o \"{icns}\"") == 0)
                {
                    File.Copy(icns, res, true); icon = "tools/game/app-icon.png";
                    if (Run("/usr/libexec/PlistBuddy", $"-c \"Set :CFBundleIconFile PlayerIcon.icns\" \"{plist}\"") != 0) Run("/usr/libexec/PlistBuddy", $"-c \"Add :CFBundleIconFile string PlayerIcon.icns\" \"{plist}\"");
                }
                Directory.Delete(set, true);
            }
            int sign = Run("/usr/bin/codesign", $"--force --deep --sign - \"{AppPath}\"");
            return $"named {AppName}, version {ver.Full} ({ver.Build}), icon {icon}, ad hoc signature {(sign == 0 ? "ok" : "FAILED")}";
        }

        /// <summary>The build's number (Sky 2026-10-19): tools/game/version.json (committed) holds the version ("0.2") and the last build's number;
        /// a build takes the next ("0.2.<n>") and writes it back only once it has succeeded. The recording is the newest Handover check
        /// (runs/missions/handover/c0xx with a run.json); the commit is git's short HEAD, dirty when the tree has changes it does not.</summary>
        public sealed class Version
        {
            [Serializable] sealed class File { public string version = "0.2"; public int build; public string note = ""; }
            public string Base = "0.2", Recording = "", Sha = "", Note = ""; public int Build; public bool Dirty;
            public string Full => $"{Base}.{Build}";
            public static string FilePath => Path.Combine(HarbourWorldBuilder.RepoRoot, "tools", "game", "version.json");
            const string Stamp = "Assets/StreamingAssets/" + BuildStamp.FileName;

            public static Version Next()
            {
                var f = System.IO.File.Exists(FilePath) ? JsonUtility.FromJson<File>(System.IO.File.ReadAllText(FilePath)) : new File();
                var v = new Version { Base = string.IsNullOrEmpty(f.version) ? "0.2" : f.version, Build = f.build + 1, Note = f.note ?? "" };
                v.Sha = Git("rev-parse --short HEAD").Trim(); v.Dirty = Git("status --porcelain").Trim().Length > 0;
                var runs = Path.Combine(HarbourWorldBuilder.RepoRoot, "runs", "missions", "handover");
                if (Directory.Exists(runs))
                    v.Recording = Directory.GetDirectories(runs).Select(Path.GetFileName).Where(n => System.Text.RegularExpressions.Regex.IsMatch(n, @"^c\d+$") && System.IO.File.Exists(Path.Combine(runs, n, "run.json")))
                        .OrderBy(n => int.Parse(n[1..])).LastOrDefault() ?? "";
                return v;
            }

            /// <summary>Takes the number: version.json's build becomes this one.</summary>
            public void Commit()
            {
                var f = new File { version = Base, build = Build, note = Note.Length > 0 ? Note : "The last game build's number: `bun run game:build` takes the next and writes it back on success (GameBuild.Version)." };
                System.IO.File.WriteAllText(FilePath, JsonUtility.ToJson(f, true) + "\n");
            }

            public void WriteStamp(SimBundle.Loaded b)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Application.dataPath, "..", Stamp))!);
                System.IO.File.WriteAllText(Path.Combine(Application.dataPath, "..", Stamp),
                    $"{{\n \"version\": \"{Full}\",\n \"build\": {Build},\n \"recording\": \"{Recording}\",\n \"sim\": \"{BuildStamp.Sim}\",\n \"sha\": \"{Sha}\",\n \"dirty\": {(Dirty ? "true" : "false")},\n \"dllSha256\": \"{b.DllSha}\",\n \"at\": \"{DateTime.UtcNow:o}\"\n}}\n");
                AssetDatabase.ImportAsset(Stamp, ImportAssetOptions.ForceSynchronousImport);
            }

            const string Presets = "Assets/StreamingAssets/presets";
            /// <summary>The camera lab's presets (briefs/025/presets, with default.txt) into the build, read-only seeds (CameraLab).</summary>
            public static void CopyPresets()
            {
                var src = Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs", "025", "presets"); if (!Directory.Exists(src)) return;
                var dst = Path.Combine(Application.dataPath, "..", Presets); Directory.CreateDirectory(dst);
                foreach (var f in Directory.GetFiles(src)) if (f.EndsWith(".json") || f.EndsWith("default.txt")) System.IO.File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true);
                AssetDatabase.ImportAsset(Presets, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceSynchronousImport);
            }
            public static void RemovePresets() { if (Directory.Exists(Path.Combine(Application.dataPath, "..", Presets))) AssetDatabase.DeleteAsset(Presets); }

            public static void RemoveStamp() { if (System.IO.File.Exists(Path.Combine(Application.dataPath, "..", Stamp))) AssetDatabase.DeleteAsset(Stamp); }

            static string Git(string args)
            {
                try
                {
                    using var p = Process.Start(new ProcessStartInfo("git", args) { WorkingDirectory = HarbourWorldBuilder.RepoRoot, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true });
                    var o = p.StandardOutput.ReadToEnd(); p.StandardError.ReadToEnd(); p.WaitForExit(); return p.ExitCode == 0 ? o : "";
                }
                catch { return ""; }
            }
        }

        static int Run(string exe, string args)
        {
            using var p = Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true });
            p.StandardOutput.ReadToEnd(); var err = p.StandardError.ReadToEnd(); p.WaitForExit();
            if (p.ExitCode != 0 && !exe.EndsWith("PlistBuddy")) Debug.LogWarning($"Game: {Path.GetFileName(exe)} {args}: exit {p.ExitCode} {err}");
            return p.ExitCode;
        }

        static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "'");
    }
}
