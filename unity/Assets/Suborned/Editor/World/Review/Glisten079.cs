using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Suborned.Play;
using Suborned.Sim;
using Suborned.World.Play;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using IOPath = System.IO.Path;

namespace Suborned.World.Review
{
    /// <summary>Brief 079 (Glint): why the play view glistens, by measurement, and the zoom stills. Play mode on the built HarbourDistrict, opened and never saved; every toggle is in memory
    /// and put back. Warm to a tick of a route as FightReel078 does, then, paused, per view and variant, a sub-pixel pan sweep through the honest capture path, with a count of "glints": small
    /// isolated bright whitish blobs. A variant is atoms joined by '+': bloomOff, clamp:v, threshold:v, intensity:v, scatter:v, smooth:re:v, smoothMul:re:k, smoothCap:re:v, coat:re:v, bump:re:v,
    /// specOff:re, specOn:re, env:k, aa:none|fxaa|smaa|smaaHigh, msaa:n, scale:k, ditherOff, fadeOff, sun:k.
    ///   -executeMethod Suborned.World.Review.Glisten079.Run -script route.json -tick 2320 -out dir -modes toggles,attrib,hdr,pan,zoom -variants a,b+c -panVariants a;b</summary>
    [InitializeOnLoad]
    public static class Glisten079
    {
        const string Key = "glisten079";
        static readonly string[] Names = { "script", "out", "tick", "modes", "variants", "panVariants", "zooms" };
        static string Arg(string n) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
        static Glisten079() { if (SessionState.GetBool(Key, false)) EditorApplication.update += Step; }

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/HarbourDistrict.unity", OpenSceneMode.Single);
            var host = Object.FindFirstObjectByType<HandoverHost>(); if (!host) { Debug.LogError("glisten: no host"); EditorApplication.Exit(2); return; }
            if (!host.carFx) host.carFx = CarFxBuild.Ensure();
            host.startPaused = true;
            SessionState.SetBool(Key, true); SessionState.SetInt(Key + ".stage", 0);
            foreach (var n in Names) SessionState.SetString(Key + "." + n, Arg("-" + n) ?? "");
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
                    var s = new GameObject("glisten").AddComponent<GlistenShots>();
                    s.A = Names.ToDictionary(n => n, n => SessionState.GetString(Key + "." + n, ""));
                    SessionState.SetInt(Key + ".stage", 1);
                }
                else if (stage == 1 && GlistenShots.Finished) { SessionState.SetInt(Key + ".stage", 2); EditorApplication.ExitPlaymode(); }
                return;
            }
            if (stage != 2) return;
            EditorApplication.update -= Step; SessionState.EraseBool(Key); SessionState.SetInt(Key + ".stage", 3);
            if (Application.isBatchMode) EditorApplication.Exit(GlistenShots.Failed ? 1 : 0);
        }
    }

    [DefaultExecutionOrder(32000)]
    public sealed class GlistenShots : MonoBehaviour
    {
        const int W = 1920, H = 1080;
        public static bool Finished, Failed;
        public Dictionary<string, string> A;

        HandoverHost h; UnityEngine.Camera cam; RenderTexture hdr, ldr, hudRt; Texture2D tex, hudTex; List<Order> orders; int oi; string outDir;
        readonly List<Action> undo = new(); readonly List<Material> mats = new(); readonly StringBuilder rep = new();
        static readonly (string id, Vector3 f, int turn)[] Views = {
            ("cafe-y030", new Vector3(-170.5f, 0, -227.5f), 0), ("cafe-y210", new Vector3(-170.5f, 0, -227.5f), 2),
            ("office-y030", new Vector3(-160f, 0, -281f), 0), ("office-y210", new Vector3(-160f, 0, -281f), 2),
        };

        void Start() { Finished = Failed = false; StartCoroutine(Guarded(Go())); }

        IEnumerator Guarded(IEnumerator step)
        {
            while (true)
            {
                object cur; try { if (!step.MoveNext()) break; cur = step.Current; } catch (Exception e) { Debug.LogError("glisten: threw " + e); Failed = true; break; }
                yield return cur;
            }
            Undo(); HandoverHost.BeforeStep = null;
            if (h && h.View) h.View.targetTexture = null;
            HudToolkit.HudInto(null);
            File.WriteAllText(IOPath.Combine(outDir ?? ".", "report.txt"), rep.ToString());
            Finished = true;
        }

        void Log(string s) { rep.AppendLine(s); Debug.Log("glisten: " + s); }

        IEnumerator Go()
        {
            outDir = IOPath.GetFullPath(A["out"]); Directory.CreateDirectory(outDir);
            h = FindAnyObjectByType<HandoverHost>(); cam = h.View;
            for (int i = 0; i < 30; i++) yield return null;
            h.controls.conversations = ConversationScenes.Never;
            hdr = new RenderTexture(W, H, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear) { name = "g-hdr" };
            ldr = new RenderTexture(W, H, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "g-ldr" };
            hudRt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "g-hud" };
            tex = new Texture2D(W, H, TextureFormat.RGB24, false); hudTex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            cam.targetTexture = hdr;
            var modes = A["modes"].Split(',', StringSplitOptions.RemoveEmptyEntries);
            bool hud = modes.Contains("zoom"); if (hud) HudToolkit.HudInto(hudRt);
            h.Begin(); h.IntelOpen = false; h.SetSpeed(1);
            var sc = Suborned.Sim.Script.Parse(File.ReadAllText(A["script"]));
            orders = sc.Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList();
            yield return null;
            int tick = int.Parse(A["tick"], CultureInfo.InvariantCulture), from = Mathf.Max(0, tick - 30);
            h.StartClock(); h.SetPaused(true);
            while (oi < orders.Count && orders[oi].Tick < from && !h.Game.Over) { var o = orders[oi++]; if (o.Tick > h.Game.Tick) h.RunTo(o.Tick); if (h.Game.Over) break; Give(o); }
            if (from > h.Game.Tick && !h.Game.Over) h.RunTo(from);
            HandoverHost.BeforeStep = host => { while (oi < orders.Count && orders[oi].Tick <= host.Game.Tick && !host.Game.Over) Give(orders[oi++]); };
            h.TurnSnap(2); h.LookAt(Fighters());
            h.SetPaused(false);
            while (h.Game.Tick < tick && !h.Game.Over) { if (h.Paused) h.SetPaused(false); yield return null; }
            h.SetPaused(true);
            for (int i = 0; i < 10; i++) yield return null;
            Log($"warm at tick {h.Game.Tick}, build {BuildStamp.Line}, ortho {cam.orthographicSize}, allowMSAA {cam.allowMSAA}, asset MSAA {UniversalRenderPipeline.asset.msaaSampleCount}, renderScale {UniversalRenderPipeline.asset.renderScale}, AA {cam.GetComponent<UniversalAdditionalCameraData>().antialiasing}");
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Include)) foreach (var m in r.sharedMaterials) if (m && !mats.Contains(m)) mats.Add(m);
            foreach (var v in FindObjectsByType<Volume>(FindObjectsInactive.Include)) Log($"volume {v.name} global {v.isGlobal} prio {v.priority} weight {v.weight} bloom {(v.sharedProfile && v.sharedProfile.TryGet<Bloom>(out var b) ? $"t{b.threshold.value}/{b.threshold.overrideState} i{b.intensity.value} s{b.scatter.value} c{b.clamp.value}/{b.clamp.overrideState} hq{b.highQualityFiltering.value}" : "-")}");
            Log($"materials in scene {mats.Count}, reflection intensity {RenderSettings.reflectionIntensity}, probes {FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Exclude).Length}");

            foreach (var mode in modes)
            {
                if (mode == "toggles") yield return Toggles();
                else if (mode == "attrib") yield return Attrib();
                else if (mode == "hdr") yield return HdrStats();
                else if (mode == "pan") yield return Pan();
                else if (mode == "zoom") yield return Zoom();
            }
        }

        void Give(Order o)
        {
            var c = new Order { Tick = o.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run, Stance = o.Stance, Force = o.Force, On = o.On, Release = o.Release, Face = o.Face, Count = o.Count, Shots = o.Shots, Then = o.Then };
            if (c.Do is "watch" or "listen" or "turn" or "call") h.GiveScripted(c); else h.Issue(c);
        }

        Vector3 Fighters()
        {
            int t = h.Game.Tick; string[] who = t < 2286 ? new[] { "scout", "fixer", "tech" } : t < 2614 ? new[] { "scout", "fixer" } : t < 2966 ? new[] { "enforcer" } : new[] { "scout", "fixer", "tech", "enforcer" };
            var sum = Vector3.zero; int n = 0; foreach (var w in who) if (h.Views.Find("op/" + w) is Transform x && x) { sum += x.position; n++; }
            return n > 0 ? sum / n : cam.transform.position;
        }

        // ---- rendering

        byte[] Shot(bool withHud = false)
        {
            cam.targetTexture = hdr; cam.Render(); Graphics.Blit(hdr, ldr);
            var prev = RenderTexture.active; RenderTexture.active = ldr; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            byte[] buf = tex.GetRawTextureData<byte>().ToArray();
            if (withHud)
            {
                RenderTexture.active = hudRt; hudTex.ReadPixels(new Rect(0, 0, W, H), 0, 0); var b = hudTex.GetRawTextureData<Color32>();
                for (int i = 0; i < b.Length; i++) { int al = b[i].a; if (al == 0) continue; int j = i * 3, ia = 255 - al; buf[j] = (byte)((b[i].r * al + buf[j] * ia) / 255); buf[j + 1] = (byte)((b[i].g * al + buf[j + 1] * ia) / 255); buf[j + 2] = (byte)((b[i].b * al + buf[j + 2] * ia) / 255); }
            }
            RenderTexture.active = prev; return buf;
        }

        void Save(byte[] buf, string file)
        {
            Directory.CreateDirectory(IOPath.GetDirectoryName(file)!);
            var t = new Texture2D(W, H, TextureFormat.RGB24, false); t.LoadRawTextureData(buf); t.Apply(false);
            File.WriteAllBytes(file, t.EncodeToPNG()); Destroy(t);
        }

        /// <summary>The glint mask: a whitish pixel (L at least 170, channel spread at most 90) that stands 30+ above all eight samples 4 px out (a true point: an edge or a line fails), plus its connected neighbours above half way.</summary>
        static bool[] Glints(byte[] b, out int blobs, out int pixels)
        {
            var L = Luma(b); var mask = new bool[W * H]; blobs = 0; pixels = 0; const int R = 4, D = 3;
            for (int y = R; y < H - R; y++)
                for (int x = R; x < W - R; x++)
                {
                    int i = y * W + x; int l = L[i]; if (l < 170) continue; int t = l - 30;
                    if (L[i - R] > t || L[i + R] > t || L[i - R * W] > t || L[i + R * W] > t || L[i - D * W - D] > t || L[i - D * W + D] > t || L[i + D * W - D] > t || L[i + D * W + D] > t) continue;
                    int j = i * 3, mx = Math.Max(b[j], Math.Max(b[j + 1], b[j + 2])), mn = Math.Min(b[j], Math.Min(b[j + 1], b[j + 2])); if (mx - mn > 90) continue;
                    if (!mask[i]) { blobs++; pixels++; mask[i] = true; }
                }
            return mask;
        }

        static short[] Luma(byte[] b) { var L = new short[W * H]; for (int i = 0; i < L.Length; i++) { int j = i * 3; L[i] = (short)((b[j] * 54 + b[j + 1] * 183 + b[j + 2] * 19) >> 8); } return L; }

        /// <summary>Frame 0 plus a 1 px pan in four steps along the camera's right. points = isolated bright pixels, blown = all channels 245+, pops = pixels that change by 60+ in a quarter pixel with the brighter side 190+ (an edge moves by much less: shimmer).</summary>
        (int blobs, int px, int blown, float flips, byte[] first) Sweep()
        {
            var p0 = cam.transform.position; float px = 2f * cam.orthographicSize / H;
            byte[] first = null; short[] prev = null; int pops = 0, blobs0 = 0, px0 = 0, blown = 0;
            for (int k = 0; k < 5; k++)
            {
                cam.transform.position = p0 + cam.transform.right * (px * k / 4f);
                var b = Shot(); var L = Luma(b);
                if (k == 0) { first = b; Glints(b, out blobs0, out px0); for (int i = 0; i < b.Length; i += 3) if (b[i] >= 245 && b[i + 1] >= 245 && b[i + 2] >= 245) blown++; }
                if (prev != null) for (int i = 0; i < L.Length; i++) { int d = L[i] - prev[i], hi = Math.Max((int)L[i], prev[i]); if ((d >= 60 || d <= -60) && hi >= 190) pops++; }
                prev = L;
            }
            cam.transform.position = p0;
            return (blobs0, px0, blown, pops / 4f, first);
        }

        void Pose((string id, Vector3 f, int turn) v) { h.TurnSnap(v.turn); h.LookAt(v.f); }

        IEnumerator Toggles()
        {
            var sb = new StringBuilder("variant,view,points,px,blown,popsPerQuarterPx\n"); int warm = 0;
            foreach (var name in A["variants"].Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                Apply(name); yield return null; yield return null;
                foreach (var v in Views)
                {
                    Pose(v); yield return null;
                    if (warm++ == 0) { Shot(); Shot(); }
                    Shot(); // a volume or setting change settles over one render
                    var s = Sweep();
                    Save(s.first, IOPath.Combine(outDir, "toggles", Safe(name), v.id + ".png"));
                    sb.Append($"{name},{v.id},{s.blobs},{s.px},{s.blown},{s.flips.ToString("0.#", CultureInfo.InvariantCulture)}\n");
                    Log($"{name} {v.id}: points {s.blobs}, blown {s.blown}, pops {s.flips:0.#}");
                    yield return null;
                }
                Undo(); yield return null;
                File.WriteAllText(IOPath.Combine(outDir, "toggles.csv"), sb.ToString());
            }
        }

        static string Safe(string s) => Regex.Replace(s, @"[^A-Za-z0-9._+-]", "_");

        IEnumerator Attrib()
        {
            var sb = new StringBuilder("view,material,shader,smoothness,metallic,pointsRemoved,blownRemoved\n");
            foreach (var v in Views)
            {
                Pose(v); yield return null; Shot(); Shot();
                var planes = GeometryUtility.CalculateFrustumPlanes(cam); var seen = new HashSet<Material>();
                foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude)) if (r.enabled && GeometryUtility.TestPlanesAABB(planes, r.bounds)) foreach (var m in r.sharedMaterials) if (m && m.HasProperty("_Smoothness")) seen.Add(m);
                int Score(byte[] im, out int points) { Glints(im, out points, out _); int bl = 0; for (int i = 0; i < im.Length; i += 3) if (im[i] >= 245 && im[i + 1] >= 245 && im[i + 2] >= 245) bl++; return bl; }
                int p0 = Score(Shot(), out int b0);
                var rows = new List<(int db, int dp, string line)>();
                foreach (var m in seen)
                {
                    float s = m.GetFloat("_Smoothness"), coat = m.HasProperty("_ClearCoatMask") ? m.GetFloat("_ClearCoatMask") : 0f; if (s <= 0.02f && coat <= 0f) continue;
                    m.SetFloat("_Smoothness", 0f); if (coat > 0f) m.SetFloat("_ClearCoatMask", 0f);
                    int p1 = Score(Shot(), out int b1);
                    m.SetFloat("_Smoothness", s); if (coat > 0f) m.SetFloat("_ClearCoatMask", coat);
                    rows.Add((b0 - b1, p0 - p1, $"{v.id},{m.name},{m.shader.name},{s:0.##},{(m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : -1):0.##},{b0 - b1},{p0 - p1}"));
                }
                Log($"attrib {v.id}: base {b0} points {p0} blown, {seen.Count} materials");
                foreach (var row in rows.OrderByDescending(r => r.dp).ThenByDescending(r => r.db)) { sb.AppendLine(row.line); }
                foreach (var row in rows.OrderByDescending(r => r.dp + r.db).Take(10)) Log("  " + row.line);
                yield return null;
            }
            File.WriteAllText(IOPath.Combine(outDir, "attrib.csv"), sb.ToString());
        }

        /// <summary>The scene's linear radiance with post off: how far the highlights overshoot the bloom threshold.</summary>
        IEnumerator HdrStats()
        {
            var ud = cam.GetComponent<UniversalAdditionalCameraData>(); var ft = new Texture2D(W, H, TextureFormat.RGBAFloat, false);
            foreach (var v in Views)
            {
                Pose(v); yield return null;
                ud.renderPostProcessing = false; cam.targetTexture = hdr; cam.Render(); cam.Render();
                var prev = RenderTexture.active; RenderTexture.active = hdr; ft.ReadPixels(new Rect(0, 0, W, H), 0, 0); RenderTexture.active = prev; ud.renderPostProcessing = true;
                var px = ft.GetPixels(); int a = 0, b = 0, c = 0, d = 0, e = 0; float mx = 0f; double over = 0;
                foreach (var q in px) { float l = Mathf.Max(q.r, Mathf.Max(q.g, q.b)); if (float.IsNaN(l)) continue; if (l > mx) mx = l; if (l > 1.05f) { a++; over += l - 1.05f; } if (l > 2f) b++; if (l > 4f) c++; if (l > 8f) d++; if (l > 16f) e++; }
                Log($"hdr {v.id}: max {mx:0.0}, >1.05 {a} px, >2 {b}, >4 {c}, >8 {d}, >16 {e}, energy over threshold {over:0}");
                yield return null;
            }
        }

        /// <summary>The same pan with each variant: 5 m/s, 4 s, 30 fps, the world paused, a player's scroll over the square.</summary>
        IEnumerator Pan()
        {
            int vi = 0;
            foreach (var name in A["panVariants"].Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                Apply(name); yield return null; Pose(Views[1]); Shot(); Shot();
                var file = IOPath.Combine(outDir, $"pan-{vi++}-{Safe(name)}.mp4");
                var si = new ProcessStartInfo(ReviewVideo.Ffmpeg, ReviewVideo.Args(W, H, W, H, 30, file, false, 16, "medium")) { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardError = true, CreateNoWindow = true };
                var ff = Process.Start(si); ff.ErrorDataReceived += (_, _) => { }; ff.BeginErrorReadLine(); var video = ff.StandardInput.BaseStream;
                var start = Views[1].f; var r = cam.transform.right; r.y = 0; r.Normalize();
                for (int f = 0; f < 120; f++)
                {
                    h.LookAt(start + r * (5f * (f - 60) / 30f));
                    var b = Shot(); video.Write(b, 0, b.Length);
                    if (f % 10 == 0) yield return null;
                }
                video.Close(); ff.WaitForExit(); Log($"pan {name}: {file}");
                Undo(); yield return null;
            }
        }

        // ---- zoom stills

        IEnumerator Zoom()
        {
            Undo();
            var fight = Fighters();
            var foci = new (string id, Vector3 f)[] { ("cell", fight), ("square", new Vector3(-159f, 0f, -232.5f)) };
            var crossings = FindObjectsByType<Transform>(FindObjectsInactive.Exclude).Where(t => t.name.IndexOf("crossing", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            var cb = new List<(string name, Bounds b)>();
            foreach (var t in crossings) { var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue; var bb = rs[0].bounds; foreach (var q in rs) bb.Encapsulate(q.bounds); if ((bb.center - fight).sqrMagnitude < 90f * 90f && bb.size.magnitude < 60f) cb.Add((t.name, bb)); }
            cb = cb.GroupBy(c => c.b.center.ToString("0")).Select(g => g.First()).OrderBy(c => (c.b.center - fight).sqrMagnitude).Take(12).ToList();
            var cross = new Bounds(); bool any = false;
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude)) { if (!Path(r.transform).StartsWith("crossing/orbit-walk/0/")) continue; if (!any) { cross = r.bounds; any = true; } else cross.Encapsulate(r.bounds); }
            Log($"crossing orbit-walk/0 bounds {cross.center:0.0} {cross.size:0.0}");
            var carT = h.Views.Find("car/getaway");
            var carB = carT ? Union(carT) : new Bounds(fight, Vector3.one);
            Log($"zoom moment tick {h.Game.Tick}, fighters at {fight:0.0}, car bounds {carB.center:0.0} {carB.size:0.0}");
            foreach (var w in new[] { "scout", "fixer", "tech", "enforcer" }) if (h.Views.Find("op/" + w) is Transform o) { var bb = Union(o); Log($"figure {w}: bounds {bb.center:0.0} {bb.size:0.00}"); }
            foreach (var size in A["zooms"].Split(',').Select(s => float.Parse(s, CultureInfo.InvariantCulture)))
                foreach (var fo in foci)
                {
                    cam.orthographicSize = size; h.TurnSnap(2); h.LookAt(fo.f);
                    for (int i = 0; i < 8; i++) { yield return null; }
                    var file = IOPath.Combine(outDir, $"zoom-{size:00}-{fo.id}.png"); var b = Shot(true); Save(b, file);
                    var sb = new StringBuilder($"zoom {size} {fo.id}: px/m vertical {H / (2f * size) * Mathf.Cos(38f * Mathf.Deg2Rad):0.0}, ground in view {2f * size * W / H:0.0} x {2f * size / Mathf.Sin(38f * Mathf.Deg2Rad):0.0} m;");
                    foreach (var w in new[] { "scout", "fixer", "tech", "enforcer" }) if (h.Views.Find("op/" + w) is Transform o) sb.Append($" {w} {Silhouette(o)} px");
                    sb.Append($"; square {Frac(-180f, -138f, -261f, -204f):P0}; car {Frac(carB.min.x, carB.max.x, carB.min.z, carB.max.z):P0};");
                    if (cross.size != Vector3.zero) sb.Append($" crossing orbit-walk/0 {Frac(cross.min.x, cross.max.x, cross.min.z, cross.max.z):P0};");
                    Log(sb.ToString());
                }
            cam.orthographicSize = 14f; yield return null;
        }

        static string Path(Transform t) { var n = t.name; for (var q = t.parent; q && !n.StartsWith("crossing/"); q = q.parent) n = q.name + "/" + n; return n; }

        /// <summary>A figure's height on screen, in pixels: the rows that change between a shot with the figure and one without, its shadow off in both.</summary>
        int Silhouette(Transform o)
        {
            var rs = o.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray(); var modes = rs.Select(r => r.shadowCastingMode).ToArray();
            foreach (var r in rs) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var a = Shot(); foreach (var r in rs) r.forceRenderingOff = true; var b = Shot(); foreach (var r in rs) r.forceRenderingOff = false;
            for (int i = 0; i < rs.Length; i++) rs[i].shadowCastingMode = modes[i];
            int top = -1, bot = -1; for (int y = 0; y < H; y++) { bool hit = false; for (int x = 0; x < W && !hit; x++) { int j = (y * W + x) * 3; if (Math.Abs(a[j] - b[j]) + Math.Abs(a[j + 1] - b[j + 1]) + Math.Abs(a[j + 2] - b[j + 2]) > 24) hit = true; } if (hit) { if (top < 0) top = y; bot = y; } }
            return top < 0 ? 0 : bot - top + 1;
        }

        static Bounds Union(Transform t) { var rs = t.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r is SkinnedMeshRenderer or MeshRenderer && !r.name.Contains("box", StringComparison.OrdinalIgnoreCase)).ToArray(); if (rs.Length == 0) return new Bounds(t.position, Vector3.zero); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b; }

        float Frac(float x0, float x1, float z0, float z1)
        {
            int inside = 0; const int N = 30;
            for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { var q = cam.WorldToViewportPoint(new Vector3(Mathf.Lerp(x0, x1, (i + .5f) / N), 0f, Mathf.Lerp(z0, z1, (j + .5f) / N))); if (q.x >= 0 && q.x <= 1 && q.y >= 0 && q.y <= 1) inside++; }
            return inside / (float)(N * N);
        }

        // ---- variants: all in memory, each with its undo

        Bloom PlayBloom() { foreach (var v in FindObjectsByType<Volume>(FindObjectsInactive.Include)) if (v.name == "lens/play" && v.sharedProfile.TryGet<Bloom>(out var b)) return b; throw new InvalidOperationException("no lens/play bloom"); }

        void Apply(string name)
        {
            Undo();
            foreach (var atom in name.Split('+', StringSplitOptions.RemoveEmptyEntries))
            {
                var p = atom.Split(':'); float F(int i) => float.Parse(p[i], CultureInfo.InvariantCulture);
                switch (p[0])
                {
                    case "base": break;
                    case "bloomOff": { var b = PlayBloom(); float i0 = b.intensity.value; b.intensity.value = 0f; undo.Add(() => b.intensity.value = i0); break; }
                    case "clamp": { var b = PlayBloom(); bool o = b.clamp.overrideState; float c0 = b.clamp.value; b.clamp.Override(F(1)); undo.Add(() => { b.clamp.value = c0; b.clamp.overrideState = o; }); break; }
                    case "threshold": { var b = PlayBloom(); float c0 = b.threshold.value; b.threshold.value = F(1); undo.Add(() => b.threshold.value = c0); break; }
                    case "intensity": { var b = PlayBloom(); float c0 = b.intensity.value; b.intensity.value = F(1); undo.Add(() => b.intensity.value = c0); break; }
                    case "scatter": { var b = PlayBloom(); float c0 = b.scatter.value; b.scatter.value = F(1); undo.Add(() => b.scatter.value = c0); break; }
                    case "smooth": MatF(p[1], "_Smoothness", _ => F(2)); break;
                    case "smoothMul": MatF(p[1], "_Smoothness", s => s * F(2)); break;
                    case "smoothCap": MatF(p[1], "_Smoothness", s => Mathf.Min(s, F(2))); break;
                    case "coat": MatF(p[1], "_ClearCoatMask", _ => F(2)); break;
                    case "bump": MatF(p[1], "_BumpScale", _ => F(2)); break;
                    case "specOff": foreach (var m in Pick(p[1])) { var mm = m; bool had = mm.IsKeywordEnabled("_SPECULARHIGHLIGHTS_OFF"); mm.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); undo.Add(() => { if (!had) mm.DisableKeyword("_SPECULARHIGHLIGHTS_OFF"); }); } break;
                    case "specOn": foreach (var m in Pick(p[1])) { var mm = m; bool had = mm.IsKeywordEnabled("_SPECULARHIGHLIGHTS_OFF"); mm.DisableKeyword("_SPECULARHIGHLIGHTS_OFF"); undo.Add(() => { if (had) mm.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); }); } break;
                    case "env":
                    {
                        float r0 = RenderSettings.reflectionIntensity; RenderSettings.reflectionIntensity = r0 * F(1); undo.Add(() => RenderSettings.reflectionIntensity = r0);
                        foreach (var rp in FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Exclude)) { var q = rp; float i0 = q.intensity; q.intensity = i0 * F(1); undo.Add(() => q.intensity = i0); }
                        break;
                    }
                    case "aa":
                    {
                        var ud = cam.GetComponent<UniversalAdditionalCameraData>(); var a0 = ud.antialiasing; var q0 = ud.antialiasingQuality;
                        ud.antialiasing = p[1] == "none" ? AntialiasingMode.None : p[1] == "fxaa" ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                        if (p[1] == "smaaHigh") ud.antialiasingQuality = AntialiasingQuality.High;
                        undo.Add(() => { ud.antialiasing = a0; ud.antialiasingQuality = q0; }); break;
                    }
                    case "msaa":
                    {
                        var urp = UniversalRenderPipeline.asset; int s0 = urp.msaaSampleCount; bool a0 = cam.allowMSAA; urp.msaaSampleCount = (int)F(1); cam.allowMSAA = true;
                        int w = (int)F(1); hdr.Release(); hdr.antiAliasing = w; hdr.Create();
                        undo.Add(() => { urp.msaaSampleCount = s0; cam.allowMSAA = a0; hdr.Release(); hdr.antiAliasing = 1; hdr.Create(); }); break;
                    }
                    case "scale": { var urp = UniversalRenderPipeline.asset; float s0 = urp.renderScale; urp.renderScale = F(1); undo.Add(() => urp.renderScale = s0); break; }
                    case "ditherOff": { var ud = cam.GetComponent<UniversalAdditionalCameraData>(); var lens = FindAnyObjectByType<AtmosphereLens>(); ud.dithering = false; if (lens) lens.enabled = true; undo.Add(() => ud.dithering = true); break; }
                    case "fadeOff": { var fa = cam.GetComponent<ArchitectureFade>(); if (fa) { fa.enabled = false; undo.Add(() => fa.enabled = true); } break; }
                    case "sun": { var s = RenderSettings.sun; float i0 = s.intensity; s.intensity = i0 * F(1); undo.Add(() => s.intensity = i0); break; }
                    default: throw new ArgumentException("glisten: unknown atom " + atom);
                }
            }
        }

        IEnumerable<Material> Pick(string re) { var r = new Regex(re, RegexOptions.IgnoreCase); return mats.Where(m => m && r.IsMatch(m.name)); }

        void MatF(string re, string prop, Func<float, float> f)
        {
            foreach (var m in Pick(re))
            {
                if (!m.HasProperty(prop)) continue; var mm = m; float s0 = mm.GetFloat(prop); mm.SetFloat(prop, f(s0)); undo.Add(() => mm.SetFloat(prop, s0));
            }
        }

        void Undo() { for (int i = undo.Count - 1; i >= 0; i--) undo[i](); undo.Clear(); }

        void OnDestroy() { Undo(); HandoverHost.BeforeStep = null; }
    }
}
