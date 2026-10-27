using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Suborned.Play;
using Suborned.World;
using Suborned.World.Review;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.Scratch
{
    /// <summary>Brief 073 SCRATCH (Glint, 2026-10-28): the vintage-print prototype's toggle menu and its offscreen batchmode shots.
    /// Built on Glint072's setup (the runtime lens installed in memory, people hidden, Baseline072's play-zoom poses) and After072's
    /// fight-lab signal frame (called by reflection). Variants off / subtle / default / strong at office-front y030 and y210 and cell-open
    /// y030, plus the signal frame per variant (with and without the signal layer, so the signal pixels can be compared), 1920 x 1080
    /// through Capture → briefs/073/proto/shots/. One HDR probe (post off) measures scene-linear levels for the cap's knee. Nothing is saved.
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.Scratch.Print073Shots.Cli [-variants off,subtle,default,strong] -logFile &lt;log&gt;</summary>
    public static class Print073Shots
    {
        const int W = 1920, H = 1080, SignalLayer = 8;
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        [MenuItem("Suborned/Scratch/Print 073/Subtle")] static void MSubtle() => Print073.Enable("subtle");
        [MenuItem("Suborned/Scratch/Print 073/Default")] static void MDefault() => Print073.Enable("default");
        [MenuItem("Suborned/Scratch/Print 073/Strong")] static void MStrong() => Print073.Enable("strong");
        [MenuItem("Suborned/Scratch/Print 073/Off")] static void MOff() => Print073.Disable();

        public static void Cli() => HarbourWorldBuilder.Batch(Run);

        static string Arg(string name, string fallback)
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : fallback;
        }

        static void Run()
        {
            var root = HarbourWorldBuilder.RepoRoot;
            var outDir = Path.GetFullPath(Path.Combine(root, Arg("-out", Path.Combine("briefs", "073", "proto", "shots"))));
            var names = Arg("-variants", "off,subtle,default,strong").Split(',');
            Directory.CreateDirectory(outDir);

            EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Single);
            var playGo = GameObject.Find("cameras/play") ?? throw new InvalidOperationException("print073: no cameras/play");
            var cam = playGo.GetComponent<Camera>(); var shadows = playGo.GetComponent<TacticalShadows>(); var fade = playGo.GetComponent<ArchitectureFade>();
            var host = UnityEngine.Object.FindAnyObjectByType<HandoverHost>(FindObjectsInactive.Include);
            float standOff = host ? host.standOff : Baseline072.DefaultStandOff;
            Vector2 cellOpen = Baseline072.CellOpenFallback;
            try { if (host != null) { var w = host.frame.World(HandoverHost.OpeningFocus); if (!float.IsNaN(w.x) && (w.x != 0 || w.z != 0)) cellOpen = new Vector2(w.x, w.z); } } catch { }
            var views = new List<(string id, Vector2 f, float yaw)> { ("office-front", Baseline072.OfficeFront, 30f), ("office-front", Baseline072.OfficeFront, 210f), ("cell-open", cellOpen, 30f), ("cell-open", cellOpen, 210f) };
            var people = GameObject.Find("people"); if (people) people.SetActive(false);
            if (shadows) { shadows.enabled = false; shadows.enabled = true; }
            if (fade) { fade.enabled = false; fade.enabled = true; }
            foreach (var oc in Camera.allCameras) if (oc != cam && oc.targetTexture == null) oc.enabled = false;

            var phase = UnityEngine.Object.FindAnyObjectByType<LightPhase>(FindObjectsInactive.Include) ?? throw new InvalidOperationException("print073: no LightPhase");
            typeof(LightPhase).GetMethod("OnEnable", Any).Invoke(phase, null);
            var lensArgs = new object[] { null };
            var lens = (AtmosphereLens)typeof(Glint072).GetMethod("InstallLens", Any).Invoke(null, lensArgs); var lensGo = (GameObject)lensArgs[0];
            Vector3 focusNow = Vector3.zero; phase.Focus = () => focusNow;

            var report = new StringBuilder("{\n  \"schema\": \"suborned.073.print/1\",\n");
            report.Append($"  \"when\": \"{DateTime.Now:yyyy-MM-dd HH:mm}\",\n  \"method\": \"{Capture.Describe}; 1920x1080; each frame rendered twice, the second kept\",\n");
            var urp = UniversalRenderPipeline.asset;
            bool feature = urp.rendererDataList[0].rendererFeatures.Any(f => f != null && f.GetType().Name == "SignalsFeature" && f.isActive);
            report.Append($"  \"signals\": \"{(feature ? "SignalsFeature in PC_Renderer (after post, own depth)" : "overlay camera stacked, no post (HostFightLab's 0.2.142 recipe; SignalsFeature not yet in PC_Renderer)")}\",\n");
            report.Append($"  \"depthTextureForced\": {urp.supportsCameraDepthTexture.ToString().ToLowerInvariant()},\n");
            int maskWas = cam.cullingMask;
            try
            {
                using var session = new RenderSession();
                Baseline072.Pose(cam, shadows, fade, views[0].f, views[0].yaw, standOff); session.Grab(cam, 64, 64);
                report.Append("  \"hdrProbe\": [\n" + string.Join(",\n", views.Take(3).Select(v => { focusNow = new Vector3(v.f.x, 0, v.f.y); Baseline072.Pose(cam, shadows, fade, v.f, v.yaw, standOff); return "    " + Probe(cam, $"{v.id}-y{v.yaw:000}"); })) + "\n  ],\n");
                var timings = new List<string>();
                foreach (var name in names)
                {
                    if (name == "off") Print073.Disable(); else Print073.Enable(name);
                    try
                    {
                        foreach (var v in views)
                        {
                            focusNow = new Vector3(v.f.x, 0f, v.f.y);
                            Baseline072.Pose(cam, shadows, fade, v.f, v.yaw, standOff);
                            session.Grab(cam, W, H);
                            var t0 = Time.realtimeSinceStartupAsDouble;
                            var tex = session.Grab(cam, W, H);
                            timings.Add($"\"{name}-{v.id}-y{v.yaw:000}\": {(Time.realtimeSinceStartupAsDouble - t0) * 1000:F1}");
                            File.WriteAllBytes(Path.Combine(outDir, $"{name}-{v.id}-y{v.yaw:000}.png"), tex.EncodeToPNG());
                        }
                        SignalShots(cam, host, shadows, fade, standOff, session, feature, outDir, name, () => focusNow = new Vector3(Baseline072.OfficeFront.x, 0, Baseline072.OfficeFront.y));
                    }
                    finally { Print073.Disable(); }
                }
                report.Append("  \"grabMsEditor\": { " + string.Join(", ", timings) + " },\n");
                // cost: 3 rounds x 30 synced grabs (render + blit + ReadPixels) per variant at office-front y030, interleaved; median of rounds
                {
                    var v0 = views[0]; focusNow = new Vector3(v0.f.x, 0f, v0.f.y); Baseline072.Pose(cam, shadows, fade, v0.f, v0.yaw, standOff);
                    var per = names.ToDictionary(n => n, n => new List<double>());
                    for (int round = 0; round < 3; round++)
                        foreach (var name in names)
                        {
                            if (name == "off") Print073.Disable(); else Print073.Enable(name);
                            for (int i = 0; i < 5; i++) session.Grab(cam, W, H);
                            var t0 = Time.realtimeSinceStartupAsDouble;
                            for (int i = 0; i < 30; i++) session.Grab(cam, W, H);
                            per[name].Add((Time.realtimeSinceStartupAsDouble - t0) * 1000.0 / 30.0);
                            Print073.Disable();
                        }
                    double Med(List<double> l) { var o = l.OrderBy(x => x).ToList(); return o[o.Count / 2]; }
                    report.Append("  \"syncedGrabMsMedian\": { " + string.Join(", ", names.Select(n => $"\"{n}\": {Med(per[n]):F2}")) + " },\n");
                }
                report.Append("  \"signalCheck\": [\n" + string.Join(",\n", names.Select(n => "    " + SignalCheck(outDir, n))) + "\n  ]\n}\n");
            }
            finally
            {
                cam.cullingMask = maskWas;
                Print073.Disable();
                typeof(AtmosphereLens).GetMethod("OnDisable", Any)?.Invoke(lens, null);
                UnityEngine.Object.DestroyImmediate(lensGo);
            }
            File.WriteAllText(Path.Combine(outDir, "print073.json"), report.ToString());
            Debug.Log($"print073: {names.Length} variant(s) → {outDir}\n{report}");
        }

        /// <summary>Scene-linear HDR levels (post off): percentiles of the max channel, so the cap's knee sits above sunlit stone.</summary>
        static string Probe(Camera cam, string id)
        {
            var d = cam.GetUniversalAdditionalCameraData(); bool post = d.renderPostProcessing; d.renderPostProcessing = false;
            using var c = new Capture(); var rt = c.Target(W / 2, H / 2);
            var own = cam.targetTexture; var prev = RenderTexture.active;
            try
            {
                cam.targetTexture = rt; cam.ResetAspect(); cam.Render(); cam.Render();
                var t = new Texture2D(rt.width, rt.height, TextureFormat.RGBAHalf, false, true);
                RenderTexture.active = rt; t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); t.Apply();
                var px = t.GetPixels(); UnityEngine.Object.DestroyImmediate(t);
                var m = px.Select(p => Mathf.Max(p.r, Mathf.Max(p.g, p.b))).OrderBy(x => x).ToArray();
                float P(float q) => m[Mathf.Clamp((int)(q * m.Length), 0, m.Length - 1)];
                float Frac(float th) => m.Count(x => x > th) / (float)m.Length;
                return $"{{ \"view\": \"{id}\", \"p50\": {P(.5f):F3}, \"p90\": {P(.9f):F3}, \"p99\": {P(.99f):F3}, \"p999\": {P(.999f):F3}, \"max\": {m[^1]:F2}, \"over1\": {Frac(1f):F4}, \"over1.5\": {Frac(1.5f):F4}, \"over2\": {Frac(2f):F4}, \"over4\": {Frac(4f):F4} }}";
            }
            finally { cam.targetTexture = own; cam.ResetAspect(); RenderTexture.active = prev; d.renderPostProcessing = post; }
        }

        /// <summary>The fight-lab frame (After072.SignalFrame: our turquoise dashed line and ghost, rival violet and guard amber dotted lines
        /// on the signal layer; red, amber, violet cones on the default layer) at office-front y030, drawn the way the build draws signals,
        /// once with the signal layer and once without (the difference is the signal pixels).</summary>
        static void SignalShots(Camera cam, HandoverHost host, TacticalShadows shadows, ArchitectureFade fade, float standOff, RenderSession session, bool feature, string outDir, string name, Action focus)
        {
            focus(); Baseline072.Pose(cam, shadows, fade, Baseline072.OfficeFront, 30f, standOff);
            var made = new List<UnityEngine.Object>(); var notes = new List<string>(); GameObject overlay = null; int mask = cam.cullingMask;
            var want = feature ? Type.GetType("Suborned.SignalsFeature, Suborned.Runtime")?.GetMethod("Want") : null;
            try
            {
                var sf = typeof(After072).GetMethod("SignalFrame", Any) ?? throw new InvalidOperationException("print073: no After072.SignalFrame");
                var sfArgs = new List<object> { cam, host, made, notes };
                if (sf.GetParameters().Length == 5) sfArgs.Add(Baseline072.OfficeFront); // Latch's 064 A3: the frame's anchor
                sf.Invoke(null, sfArgs.ToArray());
                void Shot(string file, bool signals)
                {
                    if (feature) { if (signals) cam.cullingMask |= 1 << SignalLayer; else cam.cullingMask &= ~(1 << SignalLayer); }
                    else
                    {
                        cam.cullingMask &= ~(1 << SignalLayer);
                        if (signals && overlay == null)
                        {
                            overlay = new GameObject("signals camera"); overlay.transform.SetParent(cam.transform, false);
                            var sc = overlay.AddComponent<Camera>(); sc.cullingMask = 1 << SignalLayer; sc.clearFlags = CameraClearFlags.Nothing;
                            var d = overlay.AddComponent<UniversalAdditionalCameraData>();
                            d.renderType = CameraRenderType.Overlay; d.renderPostProcessing = false; d.renderShadows = false; d.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                            cam.GetUniversalAdditionalCameraData().cameraStack.Add(sc);
                            sc.orthographic = cam.orthographic; sc.orthographicSize = cam.orthographicSize; sc.fieldOfView = cam.fieldOfView; sc.nearClipPlane = cam.nearClipPlane; sc.farClipPlane = cam.farClipPlane;
                        }
                        if (!signals && overlay != null) { cam.GetUniversalAdditionalCameraData().cameraStack.Remove(overlay.GetComponent<Camera>()); UnityEngine.Object.DestroyImmediate(overlay); overlay = null; }
                    }
                    want?.Invoke(null, null); session.Grab(cam, W, H);
                    want?.Invoke(null, null); File.WriteAllBytes(Path.Combine(outDir, file), session.Grab(cam, W, H).EncodeToPNG());
                }
                Shot($"{name}-signals-office-front-y030.png", true);
                Shot($"{name}-nosignals-office-front-y030.png", false);
            }
            finally
            {
                if (overlay) { cam.GetUniversalAdditionalCameraData().cameraStack.Remove(overlay.GetComponent<Camera>()); UnityEngine.Object.DestroyImmediate(overlay); }
                foreach (var o in made) if (o) UnityEngine.Object.DestroyImmediate(o);
                cam.cullingMask = mask;
            }
        }

        /// <summary>Signal pixels = where the variant's with- and without-signals frames differ by more than 24 levels. Over the opaque line
        /// pixels (where the off frame's signal pixel is the same colour as this variant's), the signal draws on top untouched; reports how
        /// many signal pixels match the off frame's exactly / within 2 levels.</summary>
        static string SignalCheck(string dir, string name)
        {
            Texture2D L(string f) { var t = new Texture2D(2, 2); t.LoadImage(File.ReadAllBytes(Path.Combine(dir, f))); return t; }
            var on = L($"{name}-signals-office-front-y030.png"); var no = L($"{name}-nosignals-office-front-y030.png"); var off = L("off-signals-office-front-y030.png");
            var a = on.GetPixels32(); var b = no.GetPixels32(); var o = off.GetPixels32();
            int sig = 0, same = 0, near = 0, core = 0, coreSame = 0;
            // the opaque line core: the exact unlit signal colours (sRGB 8-bit of HandoverHost's turquoise, violet, amber)
            var cores = new[] { HandoverHost.CellTurquoise, HandoverHost.RivalViolet, HandoverHost.PoliceAmber }.Select(c => (Color32)c).ToArray();
            for (int i = 0; i < a.Length; i++)
            {
                int dm = Mathf.Max(Mathf.Abs(a[i].r - b[i].r), Mathf.Max(Mathf.Abs(a[i].g - b[i].g), Mathf.Abs(a[i].b - b[i].b)));
                if (dm <= 24) continue;
                sig++;
                int d = Mathf.Max(Mathf.Abs(a[i].r - o[i].r), Mathf.Max(Mathf.Abs(a[i].g - o[i].g), Mathf.Abs(a[i].b - o[i].b)));
                if (d == 0) same++; if (d <= 2) near++;
                bool isCore = cores.Any(c => Mathf.Abs(c.r - o[i].r) <= 3 && Mathf.Abs(c.g - o[i].g) <= 3 && Mathf.Abs(c.b - o[i].b) <= 3);
                if (isCore) { core++; if (d <= 1) coreSame++; }
            }
            UnityEngine.Object.DestroyImmediate(on); UnityEngine.Object.DestroyImmediate(no); UnityEngine.Object.DestroyImmediate(off);
            return $"{{ \"variant\": \"{name}\", \"signalPixels\": {sig}, \"identicalToOff\": {same}, \"within2ToOff\": {near}, \"opaqueCorePixels\": {core}, \"opaqueCoreWithin1ToOff\": {coreSame} }}";
        }
    }
}
