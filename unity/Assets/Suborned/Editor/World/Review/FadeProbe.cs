using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>Glyph's 0.2.94 review #1 (Latch): the architecture fade over the Records Office door, rendered in the Editor at the play
    /// camera's pose (zoom 14, 38 degrees, the yaw given, the guard's door as the focus), as built and with variants, plus what each probe
    /// pixel's view ray passes through. Batchmode:
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Review.FadeProbe.Run -fadeProbeOut &lt;dir&gt; [-fadeProbeYaw 30]</summary>
    public static class FadeProbe
    {
        public static void Run() => HarbourWorldBuilder.Batch(() =>
        {
            if (!RenderSession.EnsureScene()) return;
            var dir = HarbourWorldBuilder.Arg("-fadeProbeOut", Path.Combine(HarbourWorldBuilder.RepoRoot, "out", "scratch", "latch", "fadeprobe"));
            var argv = System.Environment.GetCommandLineArgs(); int yi = System.Array.IndexOf(argv, "-fadeProbeYaw"); // a number, not a path (Arg resolves paths)
            float yaw = float.Parse(yi >= 0 && yi + 1 < argv.Length ? argv[yi + 1] : "30", System.Globalization.CultureInfo.InvariantCulture);
            Directory.CreateDirectory(dir);
            using var session = new RenderSession();
            var cam = RenderSession.TempCamera("fade-probe");
            var log = new StringBuilder();
            try
            {
                var focus = new Vector3(-171.25f, 0f, -294f); // the guard's door (block (64, -41.25)), plan → Unity x, z
                cam.orthographic = true; cam.orthographicSize = 14f; cam.nearClipPlane = 0.3f;
                var rot = Quaternion.Euler(38f, yaw, 0f); float standOff = 160f;
                cam.transform.SetPositionAndRotation(focus - rot * Vector3.forward * standOff, rot); cam.farClipPlane = standOff + 1500f;
                var play = GameObject.Find("cameras/play")?.GetComponent<ArchitectureFade>();
                var af = cam.gameObject.AddComponent<ArchitectureFade>();
                if (play) { af.ghost = play.ghost; af.ghostShadow = play.ghostShadow; af.feather = play.feather; af.radiusPerZoom = play.radiusPerZoom; af.behind = play.behind; af.floor = play.floor; af.column = play.column; }
                af.focus = focus;
                log.AppendLine($"cam {cam.transform.position} fwd {cam.transform.forward} ghost {af.ghost} r {af.Radius} behind {af.behind} column {af.column}");
                void Shot(string name) { File.WriteAllBytes(Path.Combine(dir, name + ".png"), session.Grab(cam, 1920, 1080).EncodeToPNG()); }
                session.Grab(cam, 64, 64);
                foreach (var v in new[] { 0f, 0.45f, 0.75f, 0.9f }) { af.streetVeil = v; File.WriteAllBytes(Path.Combine(dir, $"veil-{v:0.00}-2x.png"), session.Grab(cam, 3840, 2160).EncodeToPNG()); }
                af.streetVeil = 0.9f;
                var suns = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.type == LightType.Directional && l.shadows != LightShadows.None).ToList();
                log.AppendLine("suns: " + string.Join(",", suns.Select(l => l.name + ":" + l.shadows + ":" + l.transform.forward)));
                var was = suns.Select(l => l.shadows).ToList(); foreach (var l in suns) l.shadows = LightShadows.None;
                Shot("e-noshadow"); for (int i = 0; i < suns.Count; i++) suns[i].shadows = was[i];
                // a probe camera straight above the cap
                var top = cam.transform.position; var trot = cam.transform.rotation;
                cam.transform.SetPositionAndRotation(new Vector3(-160f, 120f, -296f), Quaternion.Euler(90f, 0f, 0f)); cam.orthographicSize = 30f; Shot("f-top"); cam.orthographicSize = 14f;
                cam.transform.SetPositionAndRotation(top, trot);
                // what the view ray through each probe pixel passes (renderer bounds, nearest first)
                var rends = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
                foreach (var px in new[] { new Vector2(1150, 30), new Vector2(1250, 10), new Vector2(1000, 60) })
                {
                    var ray = cam.ViewportPointToRay(new Vector3(px.x / 1920f, 1f - px.y / 1080f, 0f));
                    log.AppendLine($"pixel {px} (top-left origin) ray {ray.origin} → {ray.direction}");
                    foreach (var (r, d) in rends.Select(r => (r, Tri(r, ray))).Where(t => t.Item2.d >= 0f).OrderBy(t => t.Item2.d).Take(8).Select(t => (t.r, t.Item2)))
                        log.AppendLine($"  TRI {d.d,7:0.0} m  {Named(r.transform)}  front {d.front}  sub {d.sub} {(d.sub < r.sharedMaterials.Length && r.sharedMaterials[d.sub] ? r.sharedMaterials[d.sub].name : "?")}  at {ray.GetPoint(d.d)}");
                    foreach (var (r, d) in rends.Select(r => (r, r.bounds.IntersectRay(ray, out var d) ? d : -1f)).Where(t => t.Item2 >= 0f).OrderBy(t => t.Item2).Take(14))
                    {
                        var mats = string.Join("|", r.sharedMaterials.Where(m => m).Select(m => $"{m.name}/{m.shader.name}/cull{(m.HasProperty("_Cull") ? m.GetFloat("_Cull") : -1)}"));
                        log.AppendLine($"  {d,7:0.0} m  {Named(r.transform)}  layer {(r.renderingLayerMask & ArchitectureFade.Layer) != 0}  b {r.bounds.min}..{r.bounds.max}  {mats}");
                    }
                }
                File.WriteAllText(Path.Combine(dir, "probe.txt"), log.ToString());
                Debug.Log("FadeProbe: " + dir);
            }
            finally { Object.DestroyImmediate(cam.gameObject); }
        });

        /// <summary>The nearest triangle of r's mesh the ray hits: distance (-1 none), whether it faces the ray, its submesh.</summary>
        static (float d, bool front, int sub) Tri(Renderer r, Ray ray)
        {
            if (!r.bounds.IntersectRay(ray)) return (-1f, false, -1);
            Mesh mesh = r is SkinnedMeshRenderer sk ? sk.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh; if (!mesh || !mesh.isReadable) return (-1f, false, -2);
            var m = r.transform.localToWorldMatrix; var v = mesh.vertices; float best = float.MaxValue; bool bf = false; int bs = -1;
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                var t = mesh.GetTriangles(s);
                for (int i = 0; i < t.Length; i += 3)
                {
                    Vector3 a = m.MultiplyPoint3x4(v[t[i]]), b = m.MultiplyPoint3x4(v[t[i + 1]]), c = m.MultiplyPoint3x4(v[t[i + 2]]);
                    Vector3 e1 = b - a, e2 = c - a, p = Vector3.Cross(ray.direction, e2); float det = Vector3.Dot(e1, p); if (Mathf.Abs(det) < 1e-9f) continue;
                    float inv = 1f / det; var tv = ray.origin - a; float u = Vector3.Dot(tv, p) * inv; if (u < 0 || u > 1) continue;
                    var q = Vector3.Cross(tv, e1); float w = Vector3.Dot(ray.direction, q) * inv; if (w < 0 || u + w > 1) continue;
                    float d = Vector3.Dot(e2, q) * inv; if (d > 0 && d < best) { best = d; bf = Vector3.Dot(Vector3.Cross(e1, e2), ray.direction) < 0; bs = s; }
                }
            }
            return best < float.MaxValue ? (best, bf, bs) : (-1f, false, -1);
        }

        static string Named(Transform t) { var s = t.name; for (var p = t.parent; p && s.Length < 160; p = p.parent) s = p.name + "/" + s; return s; }
    }
}
