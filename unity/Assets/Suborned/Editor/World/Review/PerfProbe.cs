using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Review
{
    /// <summary>Play-camera perf probe (brief 007, Redline action 1 in the editor): in Play Mode, the play camera is moved to the civic
    /// square, SpaceHQ and the Parade and draw calls, vertices and frame times are sampled with the GPU Resident Drawer as the asset
    /// has it (on), with LODs as built and with them forced off (lodBias 100: LOD0 everywhere, the "before" case). Writes
    /// out/review/perf-007.json. Menu: Suborned/World/Perf Probe (enter Play Mode first).</summary>
    public static class PerfProbe
    {
        const int Warm = 8, Sample = 24;
        static IEnumerator<object> run;

        [MenuItem("Suborned/World/Perf Probe")]
        public static void Start()
        {
            if (!Application.isPlaying) { Debug.LogError("World: perf probe needs Play Mode"); return; }
            run = Body();
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            try { if (run == null || !run.MoveNext()) { EditorApplication.update -= Tick; run = null; } }
            catch (Exception e) { Debug.LogException(e); EditorApplication.update -= Tick; run = null; }
        }

        static Transform Kid(Transform p, string name) { foreach (Transform c in p) if (c.name == name) return c; return null; }

        static Bounds Of(string group, string child = null)
        {
            var root = GameObject.Find(HarbourWorldBuilder.RootName) ?? throw new InvalidOperationException("World: perf probe: no world root");
            var g = root.transform.Find(group) ?? throw new InvalidOperationException($"World: perf probe: no `{group}`");
            var go = (child == null ? g : Kid(g, child) ?? throw new InvalidOperationException($"World: perf probe: no `{group}/{child}`")).gameObject;
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }

        static void Draw(Camera cam, RenderTexture rt, Texture2D tex) { cam.Render(); }

        static IEnumerator<object> Body()
        {
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var tex = new Texture2D(1, 1, TextureFormat.RGB24, false);
            var cam = Camera.main ?? throw new InvalidOperationException("World: perf probe: no main camera");
            var urp = UniversalRenderPipeline.asset;
            cam.targetTexture = rt; cam.ResetAspect();
            var home = cam.transform.position;
            var look = cam.transform.forward;
            // where the play camera's axis meets the ground now
            float t0 = -home.y / look.y; var lookAt = home + look * t0;
            var foci = new (string name, Vector3 at)[]
            {
                ("civic square", Of("civic").center),
                ("SpaceHQ", Of("signatures","signature/space-hq").center),
                ("the Parade", Of("megastructures","mega/parade-galleries").center),
            };
            var json = new StringBuilder("{\n");
            json.Append($"  \"resolution\": \"1920x1080, 4x MSAA, manual Camera.Render of the play camera\", \"gpu\": \"{SystemInfo.graphicsDeviceName}\", \"drawer\": \"{urp.gpuResidentDrawerMode}\", \"lodBias\": {QualitySettings.lodBias}, \"quality\": \"{QualitySettings.names[QualitySettings.GetQualityLevel()]}\",\n  \"samples\": [\n");
            bool first = true;
            foreach (var (name, at) in foci)
            {
                var ground = new Vector3(at.x, 0, at.z);
                cam.transform.position = home + (ground - lookAt);
                foreach (var lod in new[] { ("before (LOD0 only)", 100f), ("after (LODs, bias 1)", 1f) })
                {
                    QualitySettings.lodBias = lod.Item2;
                                        // draw calls, vertices, triangles: the Game view's own render of the play camera (UnityStats is the Game view's last frame)
                    cam.targetTexture = null;
                    var gv = Type.GetType("UnityEditor.GameView,UnityEditor");
                    var view = EditorWindow.GetWindow(gv, false, null, false);
                    var repaint = gv.GetMethod("RepaintImmediately", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    for (int i = 0; i < 6; i++) { yield return null; repaint.Invoke(view, null); }
                    long calls = 0, setpass = 0, verts = 0, tris = 0; int n = 0;
                    for (int i = 0; i < 12; i++)
                    {
                        yield return null; repaint.Invoke(view, null);
                        calls += UnityStats.drawCalls; setpass += UnityStats.setPassCalls; verts += UnityStats.vertices; tris += UnityStats.triangles; n++;
                    }
                    // frame time: the same camera rendered by hand at 1080p, 4x MSAA, waiting for the GPU after each render
                    cam.targetTexture = rt; cam.ResetAspect();
                    double submit = 0, wall = 0;
                    for (int i = 0; i < Warm; i++) { yield return null; cam.Render(); }
                    for (int i = 0; i < Sample; i++)
                    {
                        yield return null;
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        cam.Render();
                        double t1 = sw.Elapsed.TotalMilliseconds;
                        RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1, 1), 0, 0); RenderTexture.active = null; // waits for the GPU
                        wall += sw.Elapsed.TotalMilliseconds; submit += t1;
                    }
                    if (!first) json.Append(",\n"); first = false;
                    json.Append($"    {{\"focus\": \"{name}\", \"lods\": \"{lod.Item1}\", \"drawCalls\": {calls / n}, \"setPass\": {setpass / n}, \"vertices\": {verts / n}, \"triangles\": {tris / n}, \"submitMs\": {submit / Sample:F2}, \"frameMs\": {wall / Sample:F2}" + "}");
                }
            }
            json.Append("\n  ]\n}\n");
            QualitySettings.lodBias = 1f;
            cam.transform.position = home; cam.targetTexture = null; rt.Release();
            var path = Path.Combine(HarbourWorldBuilder.RepoRoot, "out", "review", "perf-007.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json.ToString());
            Debug.Log($"World: perf probe → {path}");
        }
    }
}
