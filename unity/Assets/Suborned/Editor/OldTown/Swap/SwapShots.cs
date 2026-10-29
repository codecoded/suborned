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
using Suborned.World;
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
            if (cam.TryGetComponent<TacticalShadows>(out var ts)) ts.focusDepth = 160f;
            cam.orthographic = true; cam.orthographicSize = size;
            cam.transform.rotation = Quaternion.Euler(38f, yaw, 0f);
            cam.transform.position = OldTownSwap.Old3(focusPlan, 0f) - cam.transform.forward * 160f;
            cam.nearClipPlane = .3f; cam.farClipPlane = 1660f;
        }

        internal static void Run()
        {
            var cam = Open(out var tr, out var ro, out var sw);
            string tag = Arg("swapTag", sw != null ? sw.heights : "rule"), outDir = Path.Combine(Repo, Arg("swapOut", "briefs/075/oldbuild/swap2"));
            Directory.CreateDirectory(outDir);
            var shots = new HashSet<string>(Arg("swapShots", "circus,golden,vista,play,district").Split(','));
            int W = 1920, H = 1080; var done = new List<string>();
            var playCam = SwapPlayLook.Find();  // second build: the play camera with our play look (OldTownPlayLook, TacticalShadows); null in a first-build scene
            void Cap(string file, float shadow, Camera c = null) { DecoKitStage.WithShadows(shadow, () => DecoKitStage.Capture(c ?? cam, W, H, Path.Combine(outDir, file), 2)); done.Add(file); }
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
                    ("aerial_w", new(40f, 170f, -300f), new(-290f, 0f, 110f), 52f, true),   // second build: over the west (plan x -460..-124)
                    ("aerial_w2", new(-120f, 150f, 520f), new(-300f, 0f, 120f), 52f, true), // the west from the north
                    ("plan", new(0f, 420f, 150f), new(0f, 0f, 151f), 70f, true),
                    ("plan_full", new(0f, 1000f, 180f), new(0f, 0f, 181f), 60f, true),
                })
                { Perspective(cam, eye, look, fov); Cap($"district-{id}-{tag}.png", wide ? 1200f : 120f); }
            if (shots.Contains("skyport"))  // the S2 Skyport station (plan (114, 0)) and the Skyport tower's island, close (2026-10-30, for Sky)
                foreach (var (id, eye, look, fov) in new (string, Vector2, float, Vector2, float, float)[]
                {
                    ("quay", new(78f, -38f), 1.7f, new(114f, 0f), 14f, 62f),
                    ("terrace", new(60f, -70f), 40f, new(120f, 0f), 14f, 50f),
                    ("water", new(215f, -55f), 22f, new(125f, 0f), 18f, 50f),
                    ("north", new(95f, 70f), 30f, new(118f, 0f), 12f, 55f),
                    ("platform", new(108f, -14f), 26f, new(170f, 0f), 30f, 62f),
                }.Select(v => (v.Item1, OldTownSwap.Old3(v.Item2, v.Item3), OldTownSwap.Old3(v.Item4, v.Item5), v.Item6)))
                { Perspective(cam, eye, look, fov); Cap($"skyport-{id}-{tag}.png", 200f); }
            if (shots.Contains("tram")) Tram(cam, outDir, tag, sw, Cap, done);
            if (shots.Contains("play"))
                foreach (var (id, f, yaw) in PlayViews)
                { var pc = playCam ? playCam : cam; Play(pc, f, yaw); Cap($"play-{id}-y{yaw:000}-{tag}.png", 200f, pc); }
            if (shots.Contains("playold") && playCam)  // the same frames through the study camera (the old stage's full look), for the before/after
                foreach (var (id, f, yaw) in PlayViews.Where(v => v.id is "cafe-square" or "office-front"))
                { Play(cam, f, yaw); Cap($"play-{id}-y{yaw:000}-{tag}-oldlook.png", 200f); }
            cam.orthographic = false;
            Debug.Log($"[Swap] shots ({tag}): {string.Join(", ", done)}");
        }

        /// <summary>Brief 075 tram bed (E12): street-level frames with the study camera (the old stage's look, as pair-vista): the Boulevard at the
        /// vista camera (SpaceHQ ours, pair-vista's left) and down at the bed, Market Street east and west, the west leg, the circus loop, Crown
        /// Avenue looking back at the circus; and the whole route top down (orthographic, 100 x 50 m a tile at 12 px/m, the route along each
        /// tile's width, trunk from the west end to the Boulevard, then the circus loop north up) as strip/strip-NN.png for the index to stack.</summary>
        static void Tram(Camera cam, string outDir, string tag, SwapSwitches sw, Action<string, float, Camera> cap, List<string> done)
        {
            string hq0 = sw != null ? sw.spaceHq : "old";
            if (sw != null) sw.SetSpaceHq("ours");
            float DZ = OldTownSwap.DZ;
            foreach (var (id, eye, look, fov) in new (string, Vector3, Vector3, float)[]
            {
                ("boulevard-vista", new(-3.5f, 1.7f, -30f), new(0f, 70f, 450f), 60f),           // = vista (pair-vista)
                ("boulevard-bed", new(-3.5f, 1.7f, -30f), new(0f, 0f, 30f), 60f),
                ("boulevard-stop", new(8f, 1.7f, 100f), new(-2f, 0f, 150f), 60f),             // the circus stop (plan y -83..-53)
                ("market-east", new(-110f, 1.7f, -410f + DZ - 3f), new(-20f, .3f, -410f + DZ), 60f),
                ("market-west", new(-150f, 1.7f, -410f + DZ + 3f), new(-260f, .3f, -410f + DZ), 60f),
                ("west-leg", new(-337f, 1.7f, -60f + DZ), new(-340f, .3f, 30f + DZ), 60f),
                ("circus", new(-14f, 1.7f, -45f + DZ), new(10f, 0f, -6f + DZ), 60f),
                ("crown-avenue", new(3f, 1.7f, 120f + DZ), new(0f, .3f, 20f + DZ), 60f),
                ("hq-station-street", new(-6f, 1.7f, 175f + DZ), new(0f, 20f, 244f + DZ), 62f),   // 0.5.6: S1 into our SpaceHQ drum, the Crown Circus station inside
                ("hq-station-deck", new(46f, 30f, 196f + DZ), new(0f, 23f, 244f + DZ), 55f),
                ("hq-station-inside", new(-28f, 25.5f, 244f + DZ), new(26f, 24f, 244f + DZ), 76f),
                ("hq-station-portal", new(58f, 25.5f, 244f + DZ), new(0f, 24f, 244f + DZ), 70f),
            })
            {
                var pick = Arg("swapTramViews", ""); if (pick.Length > 0 && !pick.Split(',').Contains(id)) continue;
                Perspective(cam, eye, look, fov); cap($"tram-{id}-{tag}.png", 120f, null);
            }
            if (sw != null) sw.SetSpaceHq(hq0);

            if (Arg("swapTramViews", "").Length > 0) return; // targeted supplemental proof: preserve the settled route strip
            var w = WorldReader.Load(Path.GetFullPath(Path.Combine(Repo, Arg("swapWorld", "out/scratch/latch/075swap2/harbour.world.json"))));
            var trunk = w.Transit.Tram.FirstOrDefault(t => t.Id.EndsWith("/trunk")); var loop = w.Transit.Tram.FirstOrDefault(t => t.Id.EndsWith("/loop"));
            var strip = Path.Combine(outDir, "strip"); Directory.CreateDirectory(strip);
            const float TileL = 100f, TileW = 50f; const int PxW = 1200, PxH = 600;
            var poses = new List<(Vector3 c, float yaw, float size, int w, int h)>();
            if (trunk != null)
            {
                var line = trunk.Line.Select(OldTownSwap.Old).ToList();
                var acc = new List<float> { 0f }; for (int i = 1; i < line.Count; i++) acc.Add(acc[^1] + Vector2.Distance(line[i - 1], line[i]));
                Vector2 At(float s) { int i = 1; while (i < line.Count - 1 && acc[i] < s) i++; float t = Mathf.InverseLerp(acc[i - 1], acc[i], s); return Vector2.Lerp(line[i - 1], line[i], t); }
                for (float s = 0f; s < acc[^1]; s += TileL)
                {
                    Vector2 a = At(s), b = At(Mathf.Min(s + TileL, acc[^1])), c = At(Mathf.Min(s + TileL * .5f, acc[^1]));
                    var d = (b - a); if (d.sqrMagnitude < 1e-4f) continue; d.Normalize();
                    var mid = (a + b) * .5f; var ctr = Vector2.Lerp(mid, c, .5f);
                    poses.Add((new Vector3(ctr.x, 0f, ctr.y), Mathf.Atan2(-d.y, d.x) * Mathf.Rad2Deg, TileW * .5f, PxW, PxH));
                }
            }
            if (loop != null) { var c = OldTownSwap.Old(loop.Line.Aggregate(Vector2.zero, (q, v) => q + v) / loop.Line.Length); poses.Add((new Vector3(c.x, 0f, c.y), 0f, 35f, PxH * 2, PxH * 2)); }
            int k = 0;
            foreach (var (c, yaw, size, pw, ph) in poses)
            {
                cam.orthographic = true; cam.orthographicSize = size;
                cam.transform.rotation = Quaternion.Euler(90f, yaw, 0f);
                cam.transform.position = c + Vector3.up * 250f; cam.nearClipPlane = 1f; cam.farClipPlane = 500f;  // under the grade's soft focus (from 300 m)
                var file = Path.Combine(strip, $"strip-{k++:00}-{tag}.png");
                DecoKitStage.WithShadows(300f, () => DecoKitStage.Capture(cam, pw, ph, file, 2));
                done.Add("strip/" + Path.GetFileName(file));
            }
            cam.orthographic = false; cam.nearClipPlane = .05f; cam.farClipPlane = 2500f;
            File.WriteAllText(Path.Combine(strip, $"strip-{tag}.json"), SwapExport.Json(new Dictionary<string, object>
            {
                ["tile"] = new[] { TileL, TileW }, ["px"] = new[] { PxW, PxH }, ["frame"] = "old (plan y = z - 206)",
                ["tiles"] = poses.Select((p, i) => (object)new Dictionary<string, object> { ["file"] = $"strip-{i:00}-{tag}.png", ["centrePlan"] = new[] { p.c.x, p.c.z - DZ }, ["yaw"] = p.yaw, ["orthoHalf"] = p.size }).ToList(),
            }));
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
            Encode(cam, t => Pose(cam, tr, ro, t), from, to, fps, Path.Combine(outDir, "flyby", $"flyby-{tag}.mp4"));
        }

        /// <summary>The records district flight (2026-10-29, for Sky): one turn round the Handover blocks (the Records Office front at plan
        /// (-160, -281), the café square at (-170.5, -227.5)), coming down from 95 m to 50 m, then a glide in from the north-east to street
        /// height across the café square to the Office front. The study camera with the old stage's look, encoded like Flyby.</summary>
        internal static void RecordsFlyby()
        {
            var cam = Open(out _, out _, out var sw);
            string tag = Arg("swapTag", sw != null ? sw.heights : "rule"), outDir = Path.Combine(Repo, Arg("swapOut", "briefs/075/oldbuild/swap"));
            float fps = DecoKitStage.Arg("dkFps", 30f);
            Vector2 c = new(-166f, -255f), office = new(-160f, -281f);
            const float orbit = 26f, glide = 16f, r = 150f;
            Vector3 Orbit(float u) { var a = (30f + 360f * u) * Mathf.Deg2Rad; return OldTownSwap.Old3(c + r * new Vector2(Mathf.Sin(a), Mathf.Cos(a)), Mathf.Lerp(95f, 50f, u)); }
            void At(float t)
            {
                cam.orthographic = false; cam.fieldOfView = 50f;
                if (t < orbit) { var u = t / orbit; u = u * u * (3f - 2f * u) * .5f + u * .5f; cam.transform.position = Orbit(u); cam.transform.LookAt(OldTownSwap.Old3(c, 6f), Vector3.up); return; }
                var v = Mathf.Clamp01((t - orbit) / glide); v = v * v * (3f - 2f * v);
                Vector3 a = Orbit(1f), k = OldTownSwap.Old3(new Vector2(-128f, -205f), 24f), b = OldTownSwap.Old3(new Vector2(-176f, -244f), 6.5f);
                cam.transform.position = (1 - v) * (1 - v) * a + 2 * (1 - v) * v * k + v * v * b;
                cam.transform.LookAt(Vector3.Lerp(OldTownSwap.Old3(c, 6f), OldTownSwap.Old3(office, 7f), v), Vector3.up);
            }
            At(0f); Object.DestroyImmediate(DecoKitStage.Render(cam, 1920, 1080));  // the first full-size frame flares green over the district
            foreach (var t in new[] { 0f, 6.5f, 13f, 19.5f, 26f, 32f, 37f, 42f })
            { At(t); DecoKitStage.WithShadows(Mathf.Clamp(cam.transform.position.y * 8f, 120f, 1200f), () => DecoKitStage.Capture(cam, 1920, 1080, Path.Combine(outDir, "flyby", $"records-{tag}_t{t:00}.png"), 2)); }
            if (Arg("swapVideo", "on") != "on") return;
            Encode(cam, At, 0f, orbit + glide, fps, Path.Combine(outDir, "flyby", $"records-{tag}.mp4"));
        }

        /// <summary>Renders cam at 2x, posed by pose(t) for each frame from..to, piped raw into ffmpeg with ReviewVideo's settings.</summary>
        static void Encode(Camera cam, Action<float> pose, float from, float to, float fps, string share)
        {
            int W = 1920, H = 1080, ss = 2;
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
                    pose(from + f / fps);
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
