using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>Brief 109 round 4 (Sky, by day on 0.2.330: "got flicker here on the curb and top here"): WindowFightProbe for every surface. In a box of the
    /// scene, every triangle is tested against every other renderer's (or the same renderer's other material's) triangles near it: parallel (1.5 degrees),
    /// facing the same way (or one of them two-sided), closer than `-fightGap` (default 2 mm), overlapping (the triangle sampled on a 0.12 m grid), at least
    /// one writing depth. A mesh with Mesh LOD levels (brief 106: the merged ground chunks, BuiltIns `-meshLod`) is tested at each of its levels against
    /// the others at level 0, since a chunk draws a coarser level beside neighbours that have none: a level whose simplified triangles cross a neighbour's
    /// surface fights it only when that level is drawn. LODGroup siblings and shadow-only renderers are left out. Read only. Batchmode:
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Review.SurfaceFightProbe.Run -fightBox x0,z0,x1,z1 [-fightScene ...] [-fightOut dir] [-fightGap 0.002]</summary>
    public static class SurfaceFightProbe
    {
        sealed class Tri { public Vector3 a, b, c, n; public float d; public int r, sub, lod; }
        sealed class Pair { public string ra, ma, rb, mb; public int lod; public float area, g0 = 9f, g1; public Vector3 at; public readonly List<Vector3> spots = new(); }

        static string Arg(string k, string d) { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, k); return i >= 0 && i + 1 < a.Length ? a[i + 1] : d; }

        public static void Run()
        {
            var t0 = System.DateTime.Now; var inv = System.Globalization.CultureInfo.InvariantCulture;
            var scene = Arg("-fightScene", "Assets/Scenes/OldTown.unity");
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            var dir = Path.GetFullPath(Path.Combine(root, Arg("-fightOut", Path.Combine("out", "scratch", "latch", "surfacefight"))));
            float gap = float.Parse(Arg("-fightGap", "0.002"), inv);
            var bx = Arg("-fightBox", "-230,-80,-90,60").Split(',').Select(v => float.Parse(v, inv)).ToArray();
            Directory.CreateDirectory(dir);
            EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            var box = new Bounds(); box.SetMinMax(new Vector3(bx[0], -50f, bx[1]), new Vector3(bx[2], 400f, bx[3]));
            var rends = Object.FindObjectsByType<MeshRenderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy && r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly
                && r.GetComponent<MeshFilter>()?.sharedMesh && r.bounds.Intersects(box)).ToArray();
            var lod = new Dictionary<Renderer, (LODGroup g, int i)>();
            foreach (var g in Object.FindObjectsByType<LODGroup>()) { var ls = g.GetLODs(); for (int i = 0; i < ls.Length; i++) foreach (var r in ls[i].renderers) if (r) lod[r] = (g, i); }

            const float cell = 1f; var grid = new Dictionary<(int, int, int), List<Tri>>(); var tests = new List<Tri>(); long count = 0; int lodMeshes = 0;
            for (int ri = 0; ri < rends.Length; ri++)
            {
                var r = rends[ri]; var mesh = r.GetComponent<MeshFilter>().sharedMesh; var v = mesh.vertices; var m = r.transform.localToWorldMatrix;
                int levels = Mathf.Max(1, mesh.lodCount); if (levels > 1) lodMeshes++;
                for (int L = 0; L < levels; L++)
                    for (int s = 0; s < mesh.subMeshCount; s++)
                    {
                        var t = levels > 1 ? mesh.GetTriangles(s, L, true) : mesh.GetTriangles(s);
                        for (int k = 0; k < t.Length; k += 3)
                        {
                            Vector3 a = m.MultiplyPoint3x4(v[t[k]]), b = m.MultiplyPoint3x4(v[t[k + 1]]), c = m.MultiplyPoint3x4(v[t[k + 2]]);
                            var lo = Vector3.Min(a, Vector3.Min(b, c)); var hi = Vector3.Max(a, Vector3.Max(b, c));
                            if (hi.x < bx[0] || lo.x > bx[2] || hi.z < bx[1] || lo.z > bx[3]) continue;
                            var n = Vector3.Cross(b - a, c - a); float a2 = n.magnitude; if (a2 < 1e-5f) continue; n /= a2;
                            var tri = new Tri { a = a, b = b, c = c, n = n, d = Vector3.Dot(n, a), r = ri, sub = s, lod = L }; count++;
                            tests.Add(tri);
                            if (L > 0) continue; // the others are met at level 0
                            int x0 = Mathf.FloorToInt(lo.x / cell), x1 = Mathf.FloorToInt(hi.x / cell), y0 = Mathf.FloorToInt(lo.y / cell), y1 = Mathf.FloorToInt(hi.y / cell), z0 = Mathf.FloorToInt(lo.z / cell), z1 = Mathf.FloorToInt(hi.z / cell);
                            if ((long)(x1 - x0 + 1) * (y1 - y0 + 1) * (z1 - z0 + 1) > 20000) continue;
                            for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) for (int z = z0; z <= z1; z++)
                            { if (!grid.TryGetValue((x, y, z), out var l)) grid[(x, y, z)] = l = new List<Tri>(); l.Add(tri); }
                        }
                    }
            }
            Debug.Log($"SurfaceFightProbe: {rends.Length} renderers in the box ({lodMeshes} with Mesh LOD levels), {count} triangles");

            bool Trans(int r, int s) { var ms = rends[r].sharedMaterials; var mm = s < ms.Length ? ms[s] : null; return mm && mm.renderQueue >= 2500; }
            bool TwoSided(int r, int s) { var ms = rends[r].sharedMaterials; var mm = s < ms.Length ? ms[s] : null; return mm && mm.HasProperty("_Cull") && mm.GetFloat("_Cull") == 0f; }
            string Mat(int r, int s) { var ms = rends[r].sharedMaterials; return s < ms.Length && ms[s] ? ms[s].name : "?"; }
            var pairs = new Dictionary<(int, int, int, int, int), Pair>(); var hits = new Dictionary<(int, int), (int n, float g0, float g1, Vector3 p)>(); var pts = new List<Vector3>();
            foreach (var wt in tests)
            {
                float edge = Mathf.Max((wt.b - wt.a).magnitude, Mathf.Max((wt.c - wt.b).magnitude, (wt.a - wt.c).magnitude));
                int N = Mathf.Clamp(Mathf.CeilToInt(edge / 0.12f), 2, 80); pts.Clear(); hits.Clear();
                for (int i = 0; i <= N; i++) for (int j = 0; j <= N - i; j++) { float u = (i + 1f / 3) / (N + 1), w = (j + 1f / 3) / (N + 1); pts.Add(wt.a + (wt.b - wt.a) * u + (wt.c - wt.a) * w); }
                bool wTrans = Trans(wt.r, wt.sub);
                foreach (var q in pts)
                {
                    if (!grid.TryGetValue((Mathf.FloorToInt(q.x / cell), Mathf.FloorToInt(q.y / cell), Mathf.FloorToInt(q.z / cell)), out var l)) continue;
                    foreach (var o in l)
                    {
                        if (o.r == wt.r && (o.sub == wt.sub || wt.lod > 0)) continue;   // a level meets other renderers only
                        if (o.r < wt.r && wt.lod == 0) continue;                        // each level-0 pair once
                        float cos = Vector3.Dot(o.n, wt.n); if (Mathf.Abs(cos) < 0.99966f) continue;
                        float g = Mathf.Abs(Vector3.Dot(o.n, q) - o.d); if (g > gap) continue;
                        if (!WindowFightProbe.InsideTri(o.a, o.b, o.c, q - o.n * (Vector3.Dot(o.n, q) - o.d))) continue;
                        if (o.r != wt.r && lod.TryGetValue(rends[o.r], out var l1) && lod.TryGetValue(rends[wt.r], out var l2) && l1.g == l2.g && l1.i != l2.i) continue;
                        if (wTrans && Trans(o.r, o.sub)) continue;
                        if (cos < 0f && !TwoSided(o.r, o.sub) && !TwoSided(wt.r, wt.sub)) continue;
                        var hk = (o.r, o.sub); var h = hits.TryGetValue(hk, out var h0) ? h0 : (n: 0, g0: 9f, g1: 0f, p: q);
                        hits[hk] = (h.n + 1, Mathf.Min(h.g0, g), Mathf.Max(h.g1, g), h.p); break;
                    }
                }
                float area = Vector3.Cross(wt.b - wt.a, wt.c - wt.a).magnitude * 0.5f;
                foreach (var kv in hits)
                {
                    var key = (wt.r, wt.sub, wt.lod, kv.Key.Item1, kv.Key.Item2);
                    if (!pairs.TryGetValue(key, out var pr))
                        pairs[key] = pr = new Pair { ra = Named(rends[wt.r].transform), ma = Mat(wt.r, wt.sub), rb = Named(rends[kv.Key.Item1].transform), mb = Mat(kv.Key.Item1, kv.Key.Item2), lod = wt.lod, at = kv.Value.p };
                    pr.area += area * kv.Value.n / pts.Count; pr.g0 = Mathf.Min(pr.g0, kv.Value.g0); pr.g1 = Mathf.Max(pr.g1, kv.Value.g1);
                    if (pr.spots.Count < 6 && (pr.spots.Count == 0 || (pr.spots[^1] - kv.Value.p).sqrMagnitude > 4f)) pr.spots.Add(kv.Value.p);
                }
            }
            var rows = pairs.Values.Where(p => p.area > 0.02f).OrderByDescending(p => p.area).ToList(); var sb = new StringBuilder();
            sb.AppendLine($"scene {scene}; box x {bx[0]}..{bx[2]} z {bx[1]}..{bx[3]}; gap < {gap * 1000:0.#} mm; {rows.Count} overlapping coplanar pairs (> 0.02 m2), {rows.Sum(p => p.area):0.0} m2");
            sb.AppendLine("by level (0 = as built; n = a Mesh LOD level n of a merged chunk against its neighbours):");
            foreach (var g in rows.GroupBy(p => p.lod).OrderBy(g => g.Key)) sb.AppendLine($"  level {g.Key}: {g.Count()} pairs, {g.Sum(p => p.area):0.0} m2");
            foreach (var g in rows.GroupBy(p => (p.ma, p.mb, p.lod > 0)).OrderByDescending(g => g.Sum(p => p.area)).Take(60))
                sb.AppendLine($"  {g.Key.ma,-34} vs {g.Key.mb,-34} {(g.Key.Item3 ? "levels>0" : "level 0 "),-9} {g.Count(),4} pairs {g.Sum(p => p.area),8:0.00} m2 gap {g.Min(p => p.g0) * 1000:0.0}..{g.Max(p => p.g1) * 1000:0.0} mm");
            sb.AppendLine();
            foreach (var p in rows.Take(300)) sb.AppendLine($"{p.area,7:0.00} m2 L{p.lod} gap {p.g0 * 1000:0.0}..{p.g1 * 1000:0.0} mm at {string.Join(" ", p.spots.Select(s => s.ToString("F1")))}  {p.ma} @ {p.ra}  vs  {p.mb} @ {p.rb}");
            File.WriteAllText(Path.Combine(dir, "fights.txt"), sb.ToString());
            Debug.Log($"SurfaceFightProbe: {rows.Count} pairs → {dir}/fights.txt ({(System.DateTime.Now - t0).TotalSeconds:0}s)");
        }

        static string Named(Transform t) { var s = t.name; for (var p = t.parent; p && s.Length < 140; p = p.parent) s = p.name + "/" + s; return s; }
    }
}
