using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Review
{
    /// <summary>Brief 026 phase 1: the fixed baseline shot set (Latch, 2026-10-19). Reads briefs/026/baseline/stops.json (see its note),
    /// pins every shot's camera there the first time it resolves, and renders each pinned camera at the file's size through Capture (Glint's
    /// honest path: the player's HDR buffer, the asset's MSAA, the scene's post volume, fog and sky as built). Play shots carry copies of the
    /// scene play camera's TacticalShadows (fitted to the host's 160 m stand-off) and ArchitectureFade (about the focus), as the host
    /// places them; follow shots are perspective with tactical shadows off, as HostFollow leaves them. No HUD, no people (the editor scene
    /// has no sim). Then a timing pass: each shot rendered at 1080p with the GPU Resident Drawer as the asset has it, waiting for the GPU
    /// after each render (a 1 px read-back), median and p90 of the wall time. Writes &lt;out&gt;/&lt;stop&gt;-&lt;shot&gt;.png, frames.json and
    /// index.html (a contact sheet; a round's sheet sets each frame beside the baseline's). Observes only: no scene, light or material is
    /// changed; the temporary camera is HideAndDontSave and the URP asset's values are restored.
    /// Menus: Suborned/Render/Baseline Shots (re-shoot the baseline in briefs/026/baseline/), Suborned/Render/Baseline Shots (New Round)
    /// (the same pinned cameras into briefs/026/rounds/&lt;yyyyMMdd-HHmm&gt;/). Batch (GPU needed, no -nographics):
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Review.Baseline.Cli [-out briefs/026/rounds/x] [-repin]</summary>
    public static class Baseline
    {
        const float StandOff = 160f; // HandoverHost.standOff: the play camera's distance from its focus
        const float PlayZoom = 14f, PlayPitch = 38f;
        const float FollowDistance = 5.5f, FollowHeight = 1.3f, FollowRise = 0.32f, FollowAhead = 8f, FollowLookHeight = 1.35f, FollowFov = 55f, FollowFar = 600f; // CamPreset.Follow defaults
        const int WarmFrames = 8, TimedFrames = 30;

        static string BaselineDir => Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs", "026", "baseline");
        static string StopsPath => Path.Combine(BaselineDir, "stops.json");

        [MenuItem("Suborned/Render/Baseline Shots")]
        static void Menu() => Later(BaselineDir, false);

        [MenuItem("Suborned/Render/Baseline Shots (New Round)")]
        static void MenuRound() => Later(Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs", "026", "rounds", DateTime.Now.ToString("yyyyMMdd-HHmm")), false);

        [MenuItem("Suborned/Render/Baseline Shots (Repin Cameras)")]
        static void MenuRepin() => Later(BaselineDir, true);

        public static void Cli()
        {
            var args = Environment.GetCommandLineArgs();
            HarbourWorldBuilder.Batch(() => Run(HarbourWorldBuilder.Arg("-out", BaselineDir), args.Contains("-repin")));
        }

        /// <summary>Runs now (holds the main thread for a few minutes).</summary>
        static void Later(string dir, bool repin)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Baseline: leave Play Mode first"); return; }
            HarbourWorldBuilder.Batch(() => Run(dir, repin)); // synchronous: a delayCall never fires while the Editor is unfocused
        }

        sealed class Shot
        {
            public string Stop, Name, Kind, Label;
            public Dictionary<string, object> Spec, Camera;
            public Vector3 Focus;
            public string File => $"{Stop}-{Name}.png";
            public double Median, P90;
        }

        public static void Run(string outDir, bool repin)
        {
            if (!RenderSession.EnsureScene()) return;
            var doc = J.O(Json.Parse(File.ReadAllText(StopsPath)));
            var size = J.A(doc, "size"); int w = (int)(double)size[0], h = (int)(double)size[1];
            var play = GameObject.Find("cameras/play") ?? throw new InvalidOperationException("Baseline: no cameras/play in the scene");
            var shots = new List<Shot>();
            bool pinned = false;
            foreach (var so in J.A(doc, "stops"))
            {
                var stop = J.O(so); var f = J.P(stop, "focus");
                foreach (var sh in J.A(stop, "shots"))
                {
                    var spec = J.O(sh);
                    var s = new Shot { Stop = J.S(stop, "id"), Label = J.S(stop, "label"), Name = J.S(spec, "name"), Kind = J.S(spec, "kind"), Spec = spec, Focus = new Vector3(f.x, 0f, f.y) };
                    if (repin || J.Get(spec, "camera") == null) { spec["camera"] = Resolve(s); pinned = true; }
                    s.Camera = J.O(spec["camera"]);
                    shots.Add(s);
                }
            }

            Directory.CreateDirectory(outDir);
            var cam = MakeCamera(play);
            try
            {
                using (var session = new RenderSession())
                {
                    Apply(cam, shots[0]); session.Grab(cam, 64, 64); // the first render after a load comes out flat
                    foreach (var s in shots)
                    {
                        Apply(cam, s);
                        using (Shadows(cam, s, session))
                            File.WriteAllBytes(Path.Combine(outDir, s.File), session.Grab(cam, w, h).EncodeToPNG());
                        Debug.Log($"Baseline: {s.File} ({w}x{h}, {s.Kind})");
                    }
                }
                TimeShots(cam, shots, w, h); // the drawer back as the asset has it (RenderSession restored it)
            }
            finally { UnityEngine.Object.DestroyImmediate(cam.gameObject); }

            doc["capture"] = CaptureRecord(play, outDir);
            if (pinned || Same(outDir, BaselineDir)) File.WriteAllText(StopsPath, Write(doc, 0) + "\n");
            File.WriteAllText(Path.Combine(outDir, "frames.json"), Frames(shots, w, h));
            File.WriteAllText(Path.Combine(outDir, "index.html"), Sheet(shots, outDir, doc));
            Debug.Log($"Baseline: {shots.Count} shots -> {outDir}{(pinned ? " (cameras pinned in stops.json)" : "")}");
        }

        static bool Same(string a, string b) => Path.GetFullPath(a).TrimEnd('/') == Path.GetFullPath(b).TrimEnd('/');

        // ---------- cameras ----------

        /// <summary>A pinned camera: position, rotation (Euler), projection, clip planes, the shadow mode and the fade focus.</summary>
        static Dictionary<string, object> Resolve(Shot s)
        {
            var c = new Dictionary<string, object>();
            switch (s.Kind)
            {
                case "play":
                {
                    var rot = Quaternion.Euler(PlayPitch, J.F(s.Spec, "yaw"), 0f);
                    var pos = s.Focus - rot * Vector3.forward * StandOff;
                    c["position"] = V(pos); c["rotation"] = V(new Vector3(PlayPitch, J.F(s.Spec, "yaw"), 0f));
                    c["orthographic"] = true; c["size"] = (double)PlayZoom; c["near"] = 0.3; c["far"] = (double)(StandOff + 1500f);
                    c["shadows"] = "tactical"; c["focusDepth"] = (double)StandOff; c["fadeFocus"] = V(s.Focus);
                    break;
                }
                case "follow":
                {
                    var pv = J.A(s.Spec, "pivot");
                    var pivot = new Vector3((float)(double)pv[0], pv.Count > 2 ? (float)(double)pv[2] : 0.18f, (float)(double)pv[1]);
                    var fwd = Quaternion.Euler(0f, J.F(s.Spec, "yaw"), 0f) * Vector3.forward;
                    var pos = pivot - fwd * FollowDistance + Vector3.up * (FollowHeight + FollowDistance * FollowRise);
                    var look = pivot + fwd * FollowAhead + Vector3.up * FollowLookHeight;
                    c["position"] = V(pos); c["rotation"] = V(Quaternion.LookRotation(look - pos, Vector3.up).eulerAngles);
                    c["orthographic"] = false; c["fov"] = (double)FollowFov; c["near"] = 0.2; c["far"] = (double)FollowFar;
                    c["shadows"] = "asset";
                    break;
                }
                case "eye":
                {
                    Vector3 P(string k) { var a = J.A(s.Spec, k); return new Vector3((float)(double)a[0], (float)(double)a[2], (float)(double)a[1]); }
                    var eye = P("eye"); var look = P("look");
                    c["position"] = V(eye); c["rotation"] = V(Quaternion.LookRotation(look - eye, Vector3.up).eulerAngles);
                    c["orthographic"] = false; c["fov"] = (double)J.F(s.Spec, "fov");
                    c["near"] = J.Get(s.Spec, "near") is double n ? n : 0.3; c["far"] = J.Get(s.Spec, "far") is double fa ? fa : 3000.0;
                    if (J.Get(s.Spec, "shadowDistance") is double sd) { c["shadows"] = "distance"; c["shadowDistance"] = sd; } else c["shadows"] = "asset";
                    break;
                }
                default: throw new InvalidDataException($"Baseline: shot {s.Stop}/{s.Name} has unknown kind `{s.Kind}`");
            }
            return c;
        }

        static List<object> V(Vector3 v) => new() { R(v.x), R(v.y), R(v.z) };
        static double R(float f) => Math.Round(f, 3);
        static Vector3 Vec(object o) { var a = J.A(o); return new Vector3((float)(double)a[0], (float)(double)a[1], (float)(double)a[2]); }

        /// <summary>A throwaway copy of the scene's play camera: its Camera, URP camera data, TacticalShadows and ArchitectureFade.</summary>
        static Camera MakeCamera(GameObject play)
        {
            var go = new GameObject("baseline-026") { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            cam.CopyFrom(play.GetComponent<Camera>());
            cam.enabled = false; cam.targetTexture = null;
            var data = go.TryGetComponent<UniversalAdditionalCameraData>(out var have) ? have : go.AddComponent<UniversalAdditionalCameraData>();
            EditorUtility.CopySerialized(play.GetComponent<UniversalAdditionalCameraData>(), data);
            if (play.TryGetComponent<TacticalShadows>(out var ts)) EditorUtility.CopySerialized(ts, go.AddComponent<TacticalShadows>());
            if (play.TryGetComponent<ArchitectureFade>(out var af)) EditorUtility.CopySerialized(af, go.AddComponent<ArchitectureFade>());
            return cam;
        }

        static void Apply(Camera cam, Shot s)
        {
            var c = s.Camera;
            cam.transform.SetPositionAndRotation(Vec(c["position"]), Quaternion.Euler(Vec(c["rotation"])));
            cam.orthographic = J.B(c, "orthographic");
            if (cam.orthographic) cam.orthographicSize = J.F(c, "size"); else cam.fieldOfView = J.F(c, "fov");
            cam.nearClipPlane = J.F(c, "near"); cam.farClipPlane = J.F(c, "far");
            if (cam.TryGetComponent<TacticalShadows>(out var ts))
            {
                ts.enabled = J.S(c, "shadows") == "tactical";
                if (ts.enabled) ts.focusDepth = J.F(c, "focusDepth");
            }
            if (cam.TryGetComponent<ArchitectureFade>(out var af) && J.Get(c, "fadeFocus") is { } ff) af.focus = Vec(ff);
        }

        /// <summary>The shot's shadows: the tactical fit (the component's own begin/end events do it too), the asset's own, or a set distance.</summary>
        static IDisposable Shadows(Camera cam, Shot s, RenderSession session)
        {
            switch (J.S(s.Camera, "shadows"))
            {
                case "tactical": return cam.GetComponent<TacticalShadows>().Use();
                case "distance": session.ShadowDistance = J.F(s.Camera, "shadowDistance"); return new OnDispose(() => session.ShadowDistance = -1f);
                default: session.ShadowDistance = -1f; return null;
            }
        }

        sealed class OnDispose : IDisposable { readonly Action a; public OnDispose(Action a) => this.a = a; public void Dispose() => a(); }

        // ---------- timing ----------

        static void TimeShots(Camera cam, List<Shot> shots, int w, int h)
        {
            var urp = UniversalRenderPipeline.asset;
            float shadowDistance = urp.shadowDistance;
            using var capture = new Capture();
            var rt = capture.Target(w, h);
            var px = new Texture2D(1, 1, TextureFormat.RGBAHalf, false);
            var times = new List<double>();
            try
            {
                cam.targetTexture = rt; cam.ResetAspect();
                foreach (var s in shots)
                {
                    Apply(cam, s);
                    if (J.S(s.Camera, "shadows") == "distance") urp.shadowDistance = J.F(s.Camera, "shadowDistance");
                    using (J.S(s.Camera, "shadows") == "tactical" ? cam.GetComponent<TacticalShadows>().Use() : null)
                    {
                        for (int i = 0; i < WarmFrames; i++) Sync(cam, rt, px);
                        times.Clear();
                        for (int i = 0; i < TimedFrames; i++) { var sw = System.Diagnostics.Stopwatch.StartNew(); Sync(cam, rt, px); times.Add(sw.Elapsed.TotalMilliseconds); }
                    }
                    urp.shadowDistance = shadowDistance;
                    times.Sort();
                    s.Median = times[times.Count / 2]; s.P90 = times[(int)(times.Count * 0.9)];
                    Debug.Log($"Baseline: timing {s.Stop}/{s.Name}: median {s.Median:F2} ms, p90 {s.P90:F2} ms");
                }
            }
            finally { cam.targetTexture = null; urp.shadowDistance = shadowDistance; UnityEngine.Object.DestroyImmediate(px); }
        }

        static void Sync(Camera cam, RenderTexture rt, Texture2D px)
        {
            cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            px.ReadPixels(new Rect(0, 0, 1, 1), 0, 0); // waits for the GPU
            RenderTexture.active = prev;
        }

        // ---------- records ----------

        static Dictionary<string, object> CaptureRecord(GameObject play, string outDir)
        {
            var urp = UniversalRenderPipeline.asset;
            var root = GameObject.Find(HarbourWorldBuilder.RootName);
            var stamp = root ? root.GetComponent<WorldStamp>() : null;
            var sun = RenderSettings.sun;
            var r = new Dictionary<string, object>
            {
                ["when"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                ["out"] = Path.GetRelativePath(HarbourWorldBuilder.RepoRoot, outDir).Replace('\\', '/'),
                ["how"] = Capture.Describe,
                ["unity"] = Application.unityVersion,
                ["machine"] = $"{SystemInfo.deviceModel}, {SystemInfo.processorType}, {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType})",
                ["scene"] = HarbourWorldBuilder.ScenePath,
                ["export"] = stamp ? $"{stamp.exportPath} sha256 {stamp.exportSha256} ({stamp.source})" : "no WorldStamp",
                ["lightJson"] = Sha(HarbourWorldBuilder.LightPath),
                ["lookJson"] = Sha(HarbourWorldBuilder.LookPath),
                ["timeOfDay"] = "fixed: the built sun (no day cycle); light.json + look.json as built into the scene",
                ["sun"] = sun ? $"euler {sun.transform.eulerAngles}, intensity {sun.intensity:F3}, colour #{ColorUtility.ToHtmlStringRGB(sun.color)}, temperature {(sun.useColorTemperature ? sun.colorTemperature.ToString("F0") : "off")}, shadows {sun.shadows} strength {sun.shadowStrength:F2}" : "none",
                ["ambient"] = $"{RenderSettings.ambientMode}, sky #{ColorUtility.ToHtmlStringRGB(RenderSettings.ambientSkyColor)}, flat #{ColorUtility.ToHtmlStringRGB(RenderSettings.ambientLight)}, intensity {RenderSettings.ambientIntensity:F2}, reflections {RenderSettings.defaultReflectionMode} x{RenderSettings.reflectionIntensity:F2}",
                ["fog"] = RenderSettings.fog ? $"{RenderSettings.fogMode}, #{ColorUtility.ToHtmlStringRGB(RenderSettings.fogColor)}, density {RenderSettings.fogDensity:F5}, start {RenderSettings.fogStartDistance}, end {RenderSettings.fogEndDistance}" : "off",
                ["skybox"] = RenderSettings.skybox ? RenderSettings.skybox.name + " (" + RenderSettings.skybox.shader.name + ")" : "none",
                ["quality"] = QualitySettings.names[QualitySettings.GetQualityLevel()],
                ["urp"] = $"{urp.name}: HDR {urp.supportsHDR} ({urp.hdrColorBufferPrecision}), MSAA {urp.msaaSampleCount}, render scale {urp.renderScale}, shadow distance {urp.shadowDistance} ({urp.shadowCascadeCount} cascades), drawer {urp.gpuResidentDrawerMode}",
                ["playCamera"] = PlayCameraLine(play),
            };
            var vols = new List<object>();
            foreach (var v in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Exclude))
            {
                var p = v.HasInstantiatedProfile() ? v.profile : v.sharedProfile;
                var o = new Dictionary<string, object> { ["object"] = v.name, ["global"] = v.isGlobal, ["weight"] = (double)v.weight, ["priority"] = (double)v.priority, ["profile"] = p ? AssetDatabase.GetAssetPath(p) is { Length: > 0 } ap ? ap : p.name : "none" };
                if (p) foreach (var c in p.components) o[c.GetType().Name + (c.active ? "" : " (inactive)")] = Overrides(c);
                vols.Add(o);
            }
            r["volumes"] = vols;
            return r;
        }

        static string PlayCameraLine(GameObject play)
        {
            var c = play.GetComponent<Camera>(); var d = play.GetComponent<UniversalAdditionalCameraData>();
            return $"cameras/play: post {d.renderPostProcessing}, AA {d.antialiasing}, HDR {c.allowHDR}, clear {c.clearFlags}, volume mask {d.volumeLayerMask.value}, dithering {d.dithering}";
        }

        static string Overrides(VolumeComponent c)
        {
            var parts = new List<string>();
            foreach (var f in c.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (!typeof(VolumeParameter).IsAssignableFrom(f.FieldType) || f.GetValue(c) is not VolumeParameter p || !p.overrideState) continue;
                var val = p.GetType().GetProperty("value")?.GetValue(p);
                string s = val switch { float x => x.ToString("0.###", CultureInfo.InvariantCulture), Color col => "#" + ColorUtility.ToHtmlStringRGBA(col), UnityEngine.Object o => o ? o.name : "none", null => "null", _ => val.ToString() };
                parts.Add($"{f.Name} {s}");
            }
            return parts.Count == 0 ? "(no overrides)" : string.Join(", ", parts);
        }

        static string Sha(string path)
        {
            using var h = SHA256.Create();
            var hex = BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
            return $"{Path.GetRelativePath(HarbourWorldBuilder.RepoRoot, path)} sha256 {hex}";
        }

        static string Frames(List<Shot> shots, int w, int h)
        {
            var o = new Dictionary<string, object>
            {
                ["note"] = $"Editor frame time per shot at {w}x{h}: the shot's camera rendered by hand into the capture's HDR target (MSAA {Capture.Msaa}), the GPU Resident Drawer as the asset has it, the GPU waited on after each render (1 px read-back); {WarmFrames} warm-up renders, then the median and p90 of {TimedFrames}. Not the player: no HUD, people or sim, editor overhead included, and outside Play Mode the architecture fade draws every tagged renderer with its cutting variant. Indicative only; the Air (GDD §11) is the reference.",
                ["machine"] = $"{SystemInfo.deviceModel}, {SystemInfo.processorType}, {SystemInfo.graphicsDeviceName}",
                ["when"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                ["shots"] = shots.Select(s => (object)new Dictionary<string, object> { ["shot"] = $"{s.Stop}/{s.Name}", ["kind"] = s.Kind, ["medianMs"] = Math.Round(s.Median, 2), ["p90Ms"] = Math.Round(s.P90, 2) }).ToList(),
            };
            return Write(o, 0) + "\n";
        }

        static string Sheet(List<Shot> shots, string outDir, Dictionary<string, object> doc)
        {
            bool round = !Same(outDir, BaselineDir);
            string rel = round ? Path.GetRelativePath(outDir, BaselineDir).Replace('\\', '/') + "/" : "";
            var cap = J.O(doc["capture"]);
            var sb = new StringBuilder();
            string E(string s) => System.Net.WebUtility.HtmlEncode(s);
            sb.Append("<!doctype html><html><head><meta charset=\"utf-8\"><title>Brief 026 ").Append(round ? "round " + E(Path.GetFileName(outDir)) : "baseline").Append("</title><style>")
              .Append("body{font:13px/1.45 -apple-system,Helvetica,sans-serif;background:#1c1c1e;color:#ddd;margin:24px}h1{font-size:20px}h2{font-size:15px;margin:28px 0 6px}")
              .Append(".row{display:flex;gap:10px;flex-wrap:wrap}.cell{flex:1 1 30%;min-width:360px}.cell img{width:100%;display:block;border:1px solid #333}")
              .Append(".cap{color:#999;font-size:11px;margin-top:3px;word-break:break-all}dl{display:grid;grid-template-columns:150px 1fr;gap:2px 10px;font-size:11px;color:#aaa}dt{color:#ccc}a{color:#9cf}</style></head><body>");
            sb.Append("<h1>Brief 026 · ").Append(round ? "round " + E(Path.GetFileName(outDir)) + " beside the baseline" : "baseline shots").Append("</h1>");
            sb.Append("<p>Re-shoot: Unity menu <b>Suborned/Render/Baseline Shots</b> (this baseline) or <b>Baseline Shots (New Round)</b>; cameras pinned in <code>briefs/026/baseline/stops.json</code>. Frame times in <code>frames.json</code> (editor, indicative; the Air is the reference).");
            if (Directory.Exists(Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs", "026", "old-decokit"))) sb.Append(" Old DecoKit city: <a href=\"").Append(rel == "" ? "../old-decokit/index.html" : rel + "../old-decokit/index.html").Append("\">old-decokit/</a>.");
            sb.Append("</p>");
            foreach (var g in shots.GroupBy(s => s.Stop))
            {
                sb.Append("<h2>").Append(E(g.Key)).Append(" · ").Append(E(g.First().Label)).Append("</h2><div class=\"row\">");
                foreach (var s in g)
                {
                    var c = s.Camera;
                    string camLine = $"{s.Name}: pos {Line(c["position"])} rot {Line(c["rotation"])} {(J.B(c, "orthographic") ? "ortho " + J.F(c, "size") : "fov " + J.F(c, "fov"))}, shadows {J.S(c, "shadows")}" + (s.Kind == "play" || s.Kind == "follow" ? $" · {s.Median:F1} ms (p90 {s.P90:F1})" : $" · {s.Median:F1} ms");
                    if (round)
                    {
                        sb.Append("<div class=\"cell\" style=\"flex-basis:48%\"><div class=\"row\"><div class=\"cell\" style=\"min-width:0\"><a href=\"").Append(rel).Append(s.File).Append("\"><img src=\"").Append(rel).Append(s.File).Append("\"></a><div class=\"cap\">baseline</div></div>")
                          .Append("<div class=\"cell\" style=\"min-width:0\"><a href=\"").Append(s.File).Append("\"><img src=\"").Append(s.File).Append("\"></a><div class=\"cap\">this round</div></div></div><div class=\"cap\">").Append(E(camLine)).Append("</div></div>");
                    }
                    else sb.Append("<div class=\"cell\"><a href=\"").Append(s.File).Append("\"><img src=\"").Append(s.File).Append("\" loading=\"lazy\"></a><div class=\"cap\">").Append(E(camLine)).Append("</div></div>");
                }
                sb.Append("</div>");
            }
            sb.Append("<h2>How these frames were made</h2><dl>");
            foreach (var kv in cap)
            {
                if (kv.Value is List<object> list)
                    foreach (var item in list) foreach (var kv2 in J.O(item)) sb.Append("<dt>").Append(E(kv.Key + " · " + kv2.Key)).Append("</dt><dd>").Append(E(Convert.ToString(kv2.Value, CultureInfo.InvariantCulture))).Append("</dd>");
                else sb.Append("<dt>").Append(E(kv.Key)).Append("</dt><dd>").Append(E(Convert.ToString(kv.Value, CultureInfo.InvariantCulture))).Append("</dd>");
            }
            sb.Append("</dl></body></html>\n");
            return sb.ToString();
        }

        static string Line(object o) => "(" + string.Join(", ", J.A(o).Select(x => ((double)x).ToString("0.###", CultureInfo.InvariantCulture))) + ")";

        // ---------- JSON out (the parsed tree back to text; short objects and arrays on one line) ----------

        static string Write(object o, int depth)
        {
            string inline = Inline(o);
            if (inline.Length + depth * 2 <= 150 || o is not (Dictionary<string, object> or List<object>)) return inline;
            var pad = new string(' ', (depth + 1) * 2); var end = new string(' ', depth * 2);
            if (o is Dictionary<string, object> d)
                return "{\n" + string.Join(",\n", d.Select(kv => pad + Str(kv.Key) + ": " + Write(kv.Value, depth + 1))) + "\n" + end + "}";
            var l = (List<object>)o;
            return "[\n" + string.Join(",\n", l.Select(x => pad + Write(x, depth + 1))) + "\n" + end + "]";
        }

        static string Inline(object o) => o switch
        {
            null => "null",
            bool b => b ? "true" : "false",
            double x => x.ToString("R", CultureInfo.InvariantCulture),
            float x => ((double)x).ToString("R", CultureInfo.InvariantCulture),
            int x => x.ToString(CultureInfo.InvariantCulture),
            string s => Str(s),
            Dictionary<string, object> d => "{ " + string.Join(", ", d.Select(kv => Str(kv.Key) + ": " + Inline(kv.Value))) + " }",
            List<object> l => "[" + string.Join(", ", l.Select(Inline)) + "]",
            _ => Str(o.ToString()),
        };

        static string Str(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (var c in s)
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: if (c < 0x20) sb.Append($"\\u{(int)c:x4}"); else sb.Append(c); break;
                }
            return sb.Append('"').ToString();
        }
    }
}
