using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Suborned.World.Review
{
    /// <summary>SCRATCH (brief 074 follow-up, Latch, 2026-10-28; not for commit): the old DecoKit boulevard reel's flight re-flown through
    /// today's Harbour District, for Sky's old-against-new comparison. Uses the Flyby tool's render path (RenderSession, Capture's honest
    /// HDR frames, Pose.Apply, raw frames piped into ffmpeg) with the old reel's motion: a Hermite spline (non-uniform Catmull-Rom,
    /// still at both ends) through time-stamped eye / look keys, the view direction splined (not the look point), fov 58, shadow distance
    /// clamp(eye height x 8, 120, 1200), as DecoKitFlyby did. Keys in a scratch json (never the shipped flyby.json).
    ///   -executeMethod Suborned.World.Review.Flyby074.Cli -path &lt;json&gt; -out &lt;mp4 or dir&gt; [-colourways off] [-storyboard 2]
    ///   [-from s] [-to s] [-ss 2] [-stills t1,t2,..]
    /// -colourways off: every renderer's tinted kit material swapped back to its base (KitColourways.Base) in memory before the render;
    /// the scene is never saved. -storyboard s: 960x540 stills every s seconds plus a clearance report instead of the video.</summary>
    public static class Flyby074
    {
        struct K { public float t; public Vector3 eye, look; public string name; }

        static string Arg(string name, string fallback)
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : fallback;
        }
        static float F(string name, float fallback) => float.TryParse(Arg(name, null), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : fallback;
        static string Abs(string p) => Path.IsPathRooted(p) ? p : Path.GetFullPath(Path.Combine(HarbourWorldBuilder.RepoRoot, p));

        public static void Cli() => HarbourWorldBuilder.Batch(() =>
        {
            if (!RenderSession.EnsureScene()) throw new InvalidOperationException("074 flyby: no scene");
            var o = Json.Parse(File.ReadAllText(Abs(Arg("-path", "out/scratch/latch/074flyby/path.json"))));
            var keys = new List<K>();
            foreach (var k in J.A(o, "keys"))
            {
                var e = J.A(k, "eye"); var l = J.A(k, "look");
                keys.Add(new K { t = J.F(k, "t"), eye = new Vector3((float)(double)e[0], (float)(double)e[1], (float)(double)e[2]), look = new Vector3((float)(double)l[0], (float)(double)l[1], (float)(double)l[2]), name = J.S(k, "name") ?? "" });
            }
            float fov = J.Get(o, "fov") is double fv ? (float)fv : 58f;
            int fps = J.Get(o, "fps") is double fp ? (int)fp : 30;
            float duration = keys[^1].t;
            var stamp = UnityEngine.Object.FindAnyObjectByType<WorldStamp>();
            Debug.Log($"074 flyby: scene {HarbourWorldBuilder.ScenePath}, export {(stamp ? stamp.exportPath + " sha256 " + stamp.exportSha256 : "no stamp")}");
            var people = GameObject.Find("people");
            Debug.Log($"074 flyby: people root {(people ? people.transform.childCount + " children" : "absent")}");

            if (Arg("-colourways", "on") == "off") Plain();
            var sun = RenderSettings.sun;
            if (J.Get(o, "sun") is Dictionary<string, object> s && sun)
            {
                sun.transform.rotation = Quaternion.Euler(J.F(s, "pitch"), J.F(s, "yaw"), 0f);
                Debug.Log($"074 flyby: sun set to pitch {J.F(s, "pitch")} yaw {J.F(s, "yaw")}");
            }
            if (sun) Debug.Log($"074 flyby: sun euler {sun.transform.eulerAngles}, intensity {sun.intensity}");

            using var session = new RenderSession();
            var cam = RenderSession.TempCamera("flyby074");
            cam.fieldOfView = fov;
            try
            {
                Pose(cam, keys, 0f); session.Grab(cam, 64, 64);
                float board = F("-storyboard", 0f);
                var outArg = Abs(Arg("-out", "out/scratch/latch/074flyby/board"));
                if (board > 0f || Arg("-stills", null) != null)
                {
                    Directory.CreateDirectory(outArg);
                    var ts = board > 0f ? Enumerable.Range(0, Mathf.FloorToInt(duration / board) + 1).Select(i => i * board).ToList()
                                        : Arg("-stills", "").Split(',').Select(x => float.Parse(x, CultureInfo.InvariantCulture)).ToList();
                    int w = (int)F("-w", board > 0f ? 960 : 1920), h = (int)F("-h", board > 0f ? 540 : 1080);
                    foreach (var t in ts)
                    {
                        Pose(cam, keys, t); session.ShadowDistance = Shadow(cam);
                        File.WriteAllBytes(Path.Combine(outArg, $"t{t:00.0}.png"), session.Grab(cam, w, h).EncodeToPNG());
                        var eyeAt = cam.transform.position;
                        var nearR = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude).Where(r => r.bounds.SqrDistance(eyeAt) < 225f && r.bounds.size.magnitude < 300f)
                            .Select(r => $"{(r.transform.parent ? r.transform.parent.name + "/" : "")}{r.name} c{r.bounds.center} s{r.bounds.size}").Take(8);
                        Debug.Log($"074 flyby still t{t:0.0}: eye {eyeAt}; within 15 m: {string.Join("; ", nearR)}");
                    }
                    if (board > 0f) Sweep(keys, duration);
                    Debug.Log($"074 flyby: {ts.Count} stills → {outArg}");
                    return;
                }
                int ss = (int)F("-ss", 1f), W = 1920, H = 1080;
                float from = F("-from", 0f), to = Mathf.Min(F("-to", duration), duration);
                int frames = Mathf.RoundToInt((to - from) * fps) + 1;
                Directory.CreateDirectory(Path.GetDirectoryName(outArg));
                var scale = ss > 1 ? $"vflip,scale={W}:{H}:flags=lanczos" : "vflip";
                var psi = new ProcessStartInfo("/opt/homebrew/bin/ffmpeg",
                    $"-hide_banner -loglevel error -y -f rawvideo -pix_fmt rgb24 -s {W * ss}x{H * ss} -r {fps} -i - -vf {scale} -c:v libx264 -preset slow -crf 18 -pix_fmt yuv420p -r {fps} -movflags +faststart \"{outArg}\"")
                { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardError = true, CreateNoWindow = true };
                var log = new StringBuilder();
                using var ff = Process.Start(psi);
                ff.ErrorDataReceived += (_, a) => { if (a.Data != null) lock (log) log.AppendLine(a.Data); };
                ff.BeginErrorReadLine();
                var stdin = ff.StandardInput.BaseStream;
                var clock = Stopwatch.StartNew();
                for (int f = 0; f < frames; f++)
                {
                    Pose(cam, keys, from + f / (float)fps); session.ShadowDistance = Shadow(cam);
                    var raw = session.Grab(cam, W * ss, H * ss).GetRawTextureData<byte>();
                    var bytes = raw.ToArray(); stdin.Write(bytes, 0, bytes.Length);
                    if (f % 150 == 149) Debug.Log($"074 flyby: {f + 1}/{frames}, {clock.Elapsed.TotalSeconds / (f + 1):0.00} s a frame");
                }
                stdin.Flush(); stdin.Close();
                ff.WaitForExit();
                if (ff.ExitCode != 0) throw new IOException($"074 flyby: ffmpeg exit {ff.ExitCode}: {log}");
                Debug.Log($"074 flyby: {outArg} {frames} frames ({from:F1}-{to:F1} s, ss {ss}) in {clock.Elapsed.TotalMinutes:0.0} min");
            }
            finally { UnityEngine.Object.DestroyImmediate(cam.gameObject); }
        });

        /// <summary>Colourways off: every tinted kit material back to its base, in memory only.</summary>
        static void Plain()
        {
            KitColourways.Load(HarbourWorldBuilder.KitCataloguePath);
            int swapped = 0, renderers = 0;
            foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
            {
                var mats = r.sharedMaterials; bool any = false;
                for (int i = 0; i < mats.Length; i++) { var b = KitColourways.Base(mats[i]); if (b != mats[i]) { mats[i] = b; any = true; swapped++; } }
                if (any) { r.sharedMaterials = mats; renderers++; }
            }
            Debug.Log($"074 flyby: colourways off: {swapped} slots on {renderers} renderers back to base materials (in memory, scene not saved)");
            if (swapped == 0) throw new InvalidOperationException("074 flyby: colourways off swapped nothing");
        }

        static void Pose(Camera cam, List<K> keys, float t)
        {
            var eye = Spline(keys, t, k => keys[k].eye);
            var dir = Spline(keys, t, k => (keys[k].look - keys[k].eye).normalized);
            new Pose { Position = eye, Rotation = Quaternion.LookRotation(dir, Vector3.up), Fov = cam.fieldOfView, Near = 0.3f, Far = 12000f }.Apply(cam);
        }

        static float Shadow(Camera cam) => Mathf.Clamp(cam.transform.position.y * 8f, 120f, 1200f);

        /// <summary>DecoKitFlyby's Hermite: finite-difference velocities (non-uniform Catmull-Rom), still at both ends.</summary>
        static Vector3 Spline(List<K> keys, float t, Func<int, Vector3> p)
        {
            int last = keys.Count - 1, i = 0;
            while (i < last - 1 && t >= keys[i + 1].t) i++;
            float t0 = keys[i].t, t1 = keys[i + 1].t, dt = t1 - t0, u = Mathf.Clamp01((t - t0) / dt);
            Vector3 V(int k) => k == 0 || k == last ? Vector3.zero : (p(k + 1) - p(k - 1)) / (keys[k + 1].t - keys[k - 1].t);
            float u2 = u * u, u3 = u2 * u;
            return (2f * u3 - 3f * u2 + 1f) * p(i) + (u3 - 2f * u2 + u) * dt * V(i) + (-2f * u3 + 3f * u2) * p(i + 1) + (u3 - u2) * dt * V(i + 1);
        }

        /// <summary>Every 0.1 s: renderers whose bounds hold the eye (grown 1.5 m), skipping ground-scale ones; then, for those, the
        /// nearest mesh vertex distance (a cheap stand-in for a mesh test), so a box hit on a big tower is told apart from a real clip.</summary>
        static void Sweep(List<K> keys, float duration)
        {
            var rs = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude).Where(r => r.bounds.size.x < 400f && r.bounds.size.z < 400f).ToArray();
            var hits = new SortedDictionary<string, (float a, float b, float near)>(StringComparer.Ordinal);
            for (float t = 0f; t <= duration; t += 0.1f)
            {
                var e = Spline(keys, t, k => keys[k].eye);
                foreach (var r in rs)
                {
                    var b = r.bounds; b.Expand(3f);
                    if (!b.Contains(e)) continue;
                    float near = float.MaxValue;
                    if (r.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh && mf.sharedMesh.isReadable)
                    {
                        var tr = r.transform; var inv = tr.InverseTransformPoint(e); var sc = tr.lossyScale.magnitude / Mathf.Sqrt(3f);
                        foreach (var v in mf.sharedMesh.vertices) near = Mathf.Min(near, (v - inv).magnitude * sc);
                    }
                    var path = r.transform.parent ? r.transform.parent.name + "/" + r.name : r.name;
                    hits[path] = hits.TryGetValue(path, out var h) ? (h.a, t, Mathf.Min(h.near, near)) : (t, t, near);
                }
            }
            var sb = new StringBuilder("074 flyby clearance (bounds+3 m holding the eye; near = nearest vertex m, inf = unreadable mesh):\n");
            foreach (var kv in hits.OrderBy(k => k.Value.near)) sb.AppendLine($"  {kv.Key}: {kv.Value.a:0.0}-{kv.Value.b:0.0}s near {kv.Value.near:0.0}");
            Debug.Log(sb.ToString());
        }
    }
}
