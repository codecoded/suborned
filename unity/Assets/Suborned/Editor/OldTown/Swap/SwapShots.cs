#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Suborned.OldTown.Editor.DecoKit;
using Suborned.OldTown.Sky;
using Suborned.World.Review;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 075 swap: offscreen renders of the swapped scene (batchmode, never a visible editor or a desktop capture), with the old
    /// stage's own camera, grade, haze and lens: the circus (the old reel's camera at t 20), the boulevard golden view (the reel at t 10),
    /// the boulevard vista with SpaceHQ old and ours, the old study's district shots, play zoom (orthographic 14, pitch 38, stand-off 160 as
    /// HandoverHost) of the Handover café and Office at yaw 30 and 210 and a few district spots; the flyby on the old reel's route encoded
    /// with ReviewVideo (lanczos, BT.709, CRF 14) and stills at the old reel's still times.</summary>
    internal static class SwapShots
    {
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        static string Repo => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        static string Arg(string k, string d) => DecoKitStage.Arg(k, d);

        internal static readonly (string id, Vector2 focus, float yaw)[] PlayViews =
        {
            ("office-front", new Vector2(-160f, -281f), 30f), ("office-front", new Vector2(-160f, -281f), 210f),
            ("cafe-square", new Vector2(-170.5f, -227.5f), 30f), ("cafe-square", new Vector2(-170.5f, -227.5f), 210f),
            ("circus", new Vector2(0f, -30f), 30f), ("boulevard-east", new Vector2(14f, -140f), 30f), ("gardens-station", new Vector2(-191f, 0f), 30f),
            ("harbour-terrace", new Vector2(60f, -120f), 30f), ("harbour-station", new Vector2(117f, -140f), 30f), ("skyport-station", new Vector2(114f, 0f), 30f),
            ("zone-seam-east", new Vector2(-84f, -200f), 30f), ("southside", new Vector2(30f, -280f), 30f),
        };

        static Camera Open(out SkyTraffic[] traffic, out RocketFlight[] rockets, out SwapSwitches sw)
        {
            var scene = Arg("swapScene", "OldTown");
            EditorSceneManager.OpenScene($"Assets/Scenes/{scene}.unity", OpenSceneMode.Single);
            DecoKitHarbour.Ocean();  // the sea's material values live in code (DecoKitStreetDemo.Reshoot)
            var cam = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.name == "DK street study camera")
                      ?? DecoKitStage.Camera("DK street study camera", 60f);
            cam.enabled = true; cam.farClipPlane = 2500f; cam.nearClipPlane = .05f; cam.useOcclusionCulling = false;
            traffic = Object.FindObjectsByType<SkyTraffic>(FindObjectsSortMode.None);
            rockets = Object.FindObjectsByType<RocketFlight>(FindObjectsSortMode.None);
            sw = SwapSwitches.Read();
            Object.DestroyImmediate(DecoKitStage.Render(cam, 640, 360));  // a freshly opened scene's first frame blows out
            return cam;
        }

        static void Pose(Camera cam, SkyTraffic[] tr, RocketFlight[] ro, float t) =>
            typeof(DecoKitFlyby).GetMethod("Pose", Any).Invoke(null, new object[] { cam, tr, ro, t });

        static void Perspective(Camera cam, Vector3 eye, Vector3 look, float fov)
        {
            cam.orthographic = false; cam.transform.position = eye; cam.transform.LookAt(look, Vector3.up); cam.fieldOfView = fov;
        }

        /// <summary>HandoverHost.PlaceCamera's pose (Baseline072.Pose): orthographic size 14, pitch 38, the eye 160 m back from the focus.</summary>
        internal static void Play(Camera cam, Vector2 focusPlan, float yaw, float size = 14f)
        {
            cam.orthographic = true; cam.orthographicSize = size;
            cam.transform.rotation = Quaternion.Euler(38f, yaw, 0f);
            cam.transform.position = OldTownSwap.Old3(focusPlan, 0f) - cam.transform.forward * 160f;
            cam.nearClipPlane = .3f; cam.farClipPlane = 1660f;
        }

        internal static void Run()
        {
            var cam = Open(out var tr, out var ro, out var sw);
            string tag = Arg("swapTag", sw != null ? sw.heights : "rule"), outDir = Path.Combine(Repo, Arg("swapOut", "briefs/075/oldbuild/swap"));
            Directory.CreateDirectory(outDir);
            var shots = new HashSet<string>(Arg("swapShots", "circus,golden,vista,play,district").Split(','));
            int W = 1920, H = 1080; var done = new List<string>();
            void Cap(string file, float shadow) { DecoKitStage.WithShadows(shadow, () => DecoKitStage.Capture(cam, W, H, Path.Combine(outDir, file), 2)); done.Add(file); }
            string hq0 = sw != null ? sw.spaceHq : "old";
            if (shots.Contains("circus")) { Pose(cam, tr, ro, 20f); Cap($"circus-{tag}.png", 120f); }
            if (shots.Contains("golden")) { Pose(cam, tr, ro, 10f); Cap($"golden-{tag}.png", 120f); }
            if (shots.Contains("vista"))
                foreach (var hq in new[] { "old", "ours" })
                {
                    if (sw != null) sw.SetSpaceHq(hq);
                    Perspective(cam, new Vector3(-3.5f, 1.7f, -30f), new Vector3(0f, 70f, 450f), 60f); Cap($"vista-{tag}-hq-{hq}.png", 120f);
                    Pose(cam, tr, ro, 10f); Cap($"golden-{tag}-hq-{hq}.png", 120f);
                }
            if (sw != null) sw.SetSpaceHq(hq0);
            if (shots.Contains("district"))
                foreach (var (id, eye, look, fov, wide) in new (string, Vector3, Vector3, float, bool)[]
                {
                    ("corner", new(6.6f, 1.65f, 50f), new(11f, .4f, 62f), 55f, false),
                    ("parade", new(16f, 1.7f, 203f), new(170f, 22f, 206f), 60f, false),
                    ("harbour", new(92f, DecoKitCityPlan.PromenadeY + 1.65f, 16f), new(100f, 6f, 230f), 60f, false),
                    ("gardens", new(-44f, 1.7f, 203f), new(-104f, 5f, 206f), 62f, false),
                    ("zone-seam", new(-60f, 1.7f, 70f), new(-150f, 6f, 10f), 60f, false),
                    ("aerial", new(-190f, 150f, -110f), new(10f, 50f, 380f), 50f, true),
                    ("aerial_e", new(300f, 140f, -160f), new(0f, 10f, 190f), 50f, true),
                    ("aerial_zone", new(-20f, 160f, -150f), new(-155f, 0f, -54f), 50f, true),
                    ("plan", new(0f, 420f, 150f), new(0f, 0f, 151f), 70f, true),
                    ("plan_full", new(0f, 1000f, 180f), new(0f, 0f, 181f), 60f, true),
                })
                { Perspective(cam, eye, look, fov); Cap($"district-{id}-{tag}.png", wide ? 1200f : 120f); }
            if (shots.Contains("play"))
                foreach (var (id, f, yaw) in PlayViews)
                { Play(cam, f, yaw); Cap($"play-{id}-y{yaw:000}-{tag}.png", 200f); }
            cam.orthographic = false;
            Debug.Log($"[Swap] shots ({tag}): {string.Join(", ", done)}");
        }

        /// <summary>The old reel's flight (DecoKitFlyby's keys and spline, posed by its own Pose) over the swapped scene, rendered 2x and piped
        /// raw into ffmpeg with ReviewVideo's settings (lanczos down to 1920x1080 in 16-bit, BT.709 tagged, CRF 14 share copy); stills at the old
        /// reel's still times (briefs/074/flyby/old-stills).</summary>
        internal static void Flyby()
        {
            var cam = Open(out var tr, out var ro, out var sw);
            string tag = Arg("swapTag", sw != null ? sw.heights : "rule"), outDir = Path.Combine(Repo, Arg("swapOut", "briefs/075/oldbuild/swap"));
            float fps = DecoKitStage.Arg("dkFps", 30f), dur = (float)typeof(DecoKitFlyby).GetProperty("Duration", Any).GetValue(null);
            float from = DecoKitStage.Arg("dkFrom", 0f), to = Mathf.Min(DecoKitStage.Arg("dkTo", dur), dur);
            int W = 1920, H = 1080, ss = 2;
            foreach (var t in Arg("swapStills", "0,5,10,15,20,25,30,35,40,45,50,55,60").Split(',').Select(s => float.Parse(s, CultureInfo.InvariantCulture)))
            {
                Pose(cam, tr, ro, t);
                DecoKitStage.WithShadows(Mathf.Clamp(cam.transform.position.y * 8f, 120f, 1200f), () => DecoKitStage.Capture(cam, W, H, Path.Combine(outDir, "flyby", $"now-{tag}_t{t:00}.png"), ss));
            }
            if (Arg("swapVideo", "on") != "on") return;
            var share = Path.Combine(outDir, "flyby", $"flyby-{tag}.mp4");
            Directory.CreateDirectory(Path.GetDirectoryName(share));
            var psi = new ProcessStartInfo(ReviewVideo.Ffmpeg, ReviewVideo.Args(W * ss, H * ss, W, H, (int)fps, share, false)) { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardError = true, CreateNoWindow = true };
            using var ff = Process.Start(psi);
            var err = new System.Text.StringBuilder(); ff.ErrorDataReceived += (_, a) => { if (a.Data != null) lock (err) err.AppendLine(a.Data); }; ff.BeginErrorReadLine();
            var stdin = ff.StandardInput.BaseStream;
            var big = new RenderTexture(W * ss, H * ss, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var ldr = new RenderTexture(W * ss, H * ss, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var px = new Texture2D(W * ss, H * ss, TextureFormat.RGB24, false);
            var clock = Stopwatch.StartNew(); int frames = Mathf.RoundToInt((to - from) * fps);
            try
            {
                for (int f = 0; f < frames; f++)
                {
                    Pose(cam, tr, ro, from + f / fps);
                    DecoKitStage.WithShadows(Mathf.Clamp(cam.transform.position.y * 8f, 120f, 1200f), () =>
                    {
                        cam.targetTexture = big; ReflectionProbe.UpdateCachedState(); cam.Render(); cam.targetTexture = null;
                        Graphics.Blit(big, ldr); var prev = RenderTexture.active; RenderTexture.active = ldr;
                        px.ReadPixels(new Rect(0, 0, W * ss, H * ss), 0, 0); px.Apply(); RenderTexture.active = prev;
                        var raw = px.GetRawTextureData(); stdin.Write(raw, 0, raw.Length);
                    });
                    if (f % 150 == 149) Debug.Log($"[Swap] flyby {f + 1}/{frames}, {clock.Elapsed.TotalSeconds / (f + 1):0.00} s a frame");
                }
            }
            finally { stdin.Close(); ff.WaitForExit(); Object.DestroyImmediate(big); Object.DestroyImmediate(ldr); Object.DestroyImmediate(px); }
            if (ff.ExitCode != 0) throw new IOException($"[Swap] ffmpeg exit {ff.ExitCode}: {err}");
            Debug.Log($"[Swap] flyby {share}: {frames} frames in {clock.Elapsed.TotalMinutes:0.0} min ({ReviewVideo.Args(W * ss, H * ss, W, H, (int)fps, "<out>", false)})");
        }
    }
}
#endif
