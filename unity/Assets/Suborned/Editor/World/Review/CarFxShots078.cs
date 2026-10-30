using System.Diagnostics;
using System.IO;
using Suborned.Play;
using Suborned.World.Play;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Suborned.World.Review
{
    /// <summary>Brief 078, the car effects' captures in Play mode: `-executeMethod Suborned.World.Review.CarFxShots078.Run -out &lt;dir&gt;
    /// -mode range|handover` (batchmode, no -quit). Builds the CarFx assets (CarFxBuild), opens FightRange.unity or HarbourDistrict.unity, puts
    /// the prefab on the scene's host (in memory: the scene is never saved), enters Play, runs CarFxShots, then encodes the reel's frames
    /// (out/scratch/latch/078-frames) to mp4 with ReviewVideo's scale and encoder settings, and exits.</summary>
    [InitializeOnLoad]
    public static class CarFxShots078
    {
        const string Key = "carfx078";
        static string Arg(string n) { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }

        static CarFxShots078() { if (SessionState.GetBool(Key, false)) EditorApplication.update += Step; }

        public static void Run()
        {
            string mode = Arg("-mode") ?? "range", dir = Path.GetFullPath(Arg("-out") ?? Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs/078/vfx/shots"));
            var prefab = CarFxBuild.Build();
            EditorSceneManager.OpenScene(mode == "handover" ? "Assets/Scenes/HarbourDistrict.unity" : RangeScene.ScenePath, OpenSceneMode.Single);
            var host = Object.FindFirstObjectByType<HandoverHost>(); if (!host) { Debug.LogError("carfx: no host in the scene"); EditorApplication.Exit(2); return; }
            host.carFx = prefab; if (mode == "handover") host.startPaused = true;
            SessionState.SetBool(Key, true); SessionState.SetString(Key + ".mode", mode); SessionState.SetString(Key + ".dir", dir); SessionState.SetInt(Key + ".stage", 0);
            EditorApplication.update += Step;
            EditorApplication.EnterPlaymode();
        }

        static void Step()
        {
            int stage = SessionState.GetInt(Key + ".stage", 0);
            if (EditorApplication.isPlaying)
            {
                if (stage == 0 && Object.FindFirstObjectByType<HandoverHost>() is { } h && h.Game != null)
                {
                    var go = new GameObject("carfx-shots"); var s = go.AddComponent<CarFxShots>(); s.Mode = SessionState.GetString(Key + ".mode", "range"); s.Dir = SessionState.GetString(Key + ".dir", "");
                    SessionState.SetInt(Key + ".stage", 1);
                }
                else if (stage == 1 && CarFxShots.Finished) { SessionState.SetInt(Key + ".stage", 2); EditorApplication.ExitPlaymode(); }
                return;
            }
            if (stage != 2) return;
            EditorApplication.update -= Step; SessionState.EraseBool(Key); SessionState.SetInt(Key + ".stage", 3);
            string mode = SessionState.GetString(Key + ".mode", "range"), dir = SessionState.GetString(Key + ".dir", "");
            var frames = Path.Combine(HarbourWorldBuilder.RepoRoot, "out/scratch/latch/078-frames", mode);
            foreach (var zoom in new[] { "play", "close" })
            {
                var src = Path.Combine(frames, zoom); if (!Directory.Exists(src)) continue;
                var mp4 = Path.Combine(dir, $"{mode}-car-blast-{zoom}.mp4");
                var args = $"-hide_banner -loglevel error -y -framerate 30 -i \"{src}/%04d.jpg\" -vf \"{ReviewVideo.Scale(1920, 1080)},format=yuv420p\" {ReviewVideo.Encode("yuv420p", ReviewVideo.ShareCrf)} -r 30 \"{mp4}\"";
                var p = Process.Start(new ProcessStartInfo(ReviewVideo.Ffmpeg, args) { UseShellExecute = false, RedirectStandardError = true });
                string err = p.StandardError.ReadToEnd(); p.WaitForExit();
                Debug.Log($"carfx: {mp4}: ffmpeg exit {p.ExitCode} {err}");
            }
            Debug.Log("carfx: done -> " + dir);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
