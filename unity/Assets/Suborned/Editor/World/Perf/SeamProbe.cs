using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Glyph's 0.2.113 review #5 (faint road-sheet seams at the Boulevard x Market junction, plan (0, -410)): which of the two causes it is.
    /// (a) coplanar road faces from different renderers overlapping on the carriageway (z-fighting); (b) the road's tile batches (Combine, 64 m)
    /// meeting there, each lit by a different reflection-probe blend (a renderer takes its probes from its bounds) or carrying a different material.
    /// Batchmode: -executeMethod Suborned.World.SeamProbe.Run [-seamAt x,z] [-seamR m]; writes out/scratch/latch/seams/seams.json. Reads only.</summary>
    public static class SeamProbe
    {
        public static void Run()
        {
            var a = System.Environment.GetCommandLineArgs(); Vector2 at = new(0f, -410f); float r = 40f;
            int i = System.Array.IndexOf(a, "-seamAt"); if (i >= 0 && i + 1 < a.Length) { var p = a[i + 1].Split(','); at = new Vector2(float.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture)); }
            i = System.Array.IndexOf(a, "-seamR"); if (i >= 0 && i + 1 < a.Length) r = float.Parse(a[i + 1], System.Globalization.CultureInfo.InvariantCulture);
            EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Single);
            var box = new Bounds(new Vector3(at.x, 0f, at.y), new Vector3(2 * r, 2f, 2 * r));
            var rows = new List<string>(); var flats = new List<(MeshRenderer mr, Mesh m, string path)>();
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!mr.enabled || !mr.bounds.Intersects(box)) continue; var mf = mr.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                var b = mr.bounds; if (b.size.y > 0.4f || b.max.y > 0.3f) continue; // the ground's flat faces only
                string path = mr.transform.parent ? mr.transform.parent.name + "/" + mr.name : mr.name;
                var probes = new List<UnityEngine.Rendering.ReflectionProbeBlendInfo>(); mr.GetClosestReflectionProbes(probes);
                string pr = string.Join("+", probes.Select(x => $"{(x.probe ? x.probe.name : "sky")}:{x.weight:0.00}"));
                rows.Add($"  {{\"renderer\": \"{path}\", \"material\": \"{(mr.sharedMaterial ? mr.sharedMaterial.name : "")}\", \"min\": [{b.min.x:0.##}, {b.min.y:0.###}, {b.min.z:0.##}], \"max\": [{b.max.x:0.##}, {b.max.y:0.###}, {b.max.z:0.##}], \"probeUsage\": \"{mr.reflectionProbeUsage}\", \"probes\": \"{pr}\", \"lightProbes\": \"{mr.lightProbeUsage}\"}}");
                flats.Add((mr, mf.sharedMesh, path));
            }
            // coplanar overlaps: sample the box every 0.5 m; at each point, the up-facing triangles of different renderers within 3 mm in height
            var hits = new Dictionary<string, int>(); int samples = 0, multi = 0;
            var tris = new List<(string who, string mat, Vector3 a, Vector3 b, Vector3 c)>();
            foreach (var (mr, m, path) in flats)
            {
                if (!m.isReadable) { hits["unreadable " + path] = 1; continue; }
                var v = m.vertices; var t = m.triangles; var w = mr.transform.localToWorldMatrix; string mat = mr.sharedMaterial ? mr.sharedMaterial.name : "";
                for (int k = 0; k + 2 < t.Length; k += 3)
                {
                    var p0 = w.MultiplyPoint3x4(v[t[k]]); var p1 = w.MultiplyPoint3x4(v[t[k + 1]]); var p2 = w.MultiplyPoint3x4(v[t[k + 2]]);
                    var n = Vector3.Cross(p1 - p0, p2 - p0); if (n.sqrMagnitude < 1e-10f || Mathf.Abs(n.normalized.y) < 0.99f) continue;
                    float x0 = Mathf.Min(p0.x, p1.x, p2.x), x1 = Mathf.Max(p0.x, p1.x, p2.x), z0 = Mathf.Min(p0.z, p1.z, p2.z), z1 = Mathf.Max(p0.z, p1.z, p2.z);
                    if (x1 < box.min.x || x0 > box.max.x || z1 < box.min.z || z0 > box.max.z) continue;
                    tris.Add((path, mat, p0, p1, p2));
                }
            }
            static bool In(Vector2 p, Vector3 a, Vector3 b, Vector3 c, out float y)
            {
                y = 0f; var v0 = new Vector2(c.x - a.x, c.z - a.z); var v1 = new Vector2(b.x - a.x, b.z - a.z); var v2 = new Vector2(p.x - a.x, p.y - a.z);
                float d00 = Vector2.Dot(v0, v0), d01 = Vector2.Dot(v0, v1), d02 = Vector2.Dot(v0, v2), d11 = Vector2.Dot(v1, v1), d12 = Vector2.Dot(v1, v2), den = d00 * d11 - d01 * d01; if (Mathf.Abs(den) < 1e-12f) return false;
                float u = (d11 * d02 - d01 * d12) / den, w2 = (d00 * d12 - d01 * d02) / den; if (u < -1e-4f || w2 < -1e-4f || u + w2 > 1f + 1e-4f) return false;
                y = a.y + u * (c.y - a.y) + w2 * (b.y - a.y); return true;
            }
            var pairs = new Dictionary<string, int>();
            for (float x = box.min.x; x <= box.max.x; x += 0.5f)
                for (float z = box.min.z; z <= box.max.z; z += 0.5f)
                {
                    samples++; var under = new List<(string who, string mat, float y)>();
                    foreach (var tr in tris) if (In(new Vector2(x, z), tr.a, tr.b, tr.c, out var y)) under.Add((tr.who, tr.mat, y));
                    if (under.Count < 2) continue;
                    under.Sort((p, q) => q.y.CompareTo(p.y)); bool any = false;
                    for (int u = 1; u < under.Count; u++) if (under[0].who != under[u].who && Mathf.Abs(under[0].y - under[u].y) < 0.003f) { any = true; var key = $"{under[0].who} ({under[0].mat}) = {under[u].who} ({under[u].mat})"; pairs[key] = (pairs.TryGetValue(key, out var n0) ? n0 : 0) + 1; }
                    if (any) multi++;
                }
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "out", "scratch", "latch", "seams")); Directory.CreateDirectory(dir);
            var sb = new StringBuilder("{\n");
            sb.Append($" \"at\": [{at.x}, {at.y}], \"r\": {r}, \"samples\": {samples}, \"coplanarSamples\": {multi},\n \"coplanarPairs\": [{string.Join(", ", pairs.OrderByDescending(kv => kv.Value).Take(40).Select(kv => $"\n  {{\"pair\": \"{kv.Key}\", \"samples\": {kv.Value}}}"))}],\n");
            sb.Append($" \"unreadable\": [{string.Join(", ", hits.Keys.Select(k => "\"" + k + "\""))}],\n \"renderers\": [\n{string.Join(",\n", rows)}\n ]\n}}\n");
            File.WriteAllText(Path.Combine(dir, "seams.json"), sb.ToString());
            Debug.Log($"SeamProbe: {rows.Count} flat renderers near ({at.x}, {at.y}), {multi} of {samples} samples with coplanar faces from two renderers; out/scratch/latch/seams/seams.json");
        }
    }
}
