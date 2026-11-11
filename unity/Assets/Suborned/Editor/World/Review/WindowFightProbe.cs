using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>Brief 109 round 4 (Sky, the night clip: "I see the windows flicker"; "think it's the window layer fighting glass - that's what happened before",
    /// brief 097's tower slots, whose backing lay flush on the stone). Every triangle drawn with a night-lit window material (roadlight.json glow: DK_GlassDark*,
    /// DK_Window_Tower*, DK_Window_Teal, DK_Window_Film, DK_Cabin) is tested against every other surface's triangles near it: parallel (normals within 1.5
    /// degrees, either facing), closer than `-fightGap` (default 6 mm) and overlapping (sample points of the window triangle inside the other). Such a pair
    /// z-fights: by day both are dark glass or dark stone and the fight is hidden; at night one is lit and it flickers. Reports each pair of renderers and
    /// materials with the overlapping area and the gap, so the fix goes to the geometry's source. Read only (the scene is opened, never saved). Batchmode:
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Review.WindowFightProbe.Run [-fightScene Assets/Scenes/OldTown.unity] [-fightOut dir] [-fightGap 0.006]</summary>
    public static class WindowFightProbe
    {
        static readonly string[] Lit = { "DK_GlassDark", "DK_Window_Tower", "DK_Window_Teal", "DK_Window_Film", "DK_Cabin" };
        static bool IsLit(Material m) => m && Lit.Any(p => m.name.StartsWith(p));

        sealed class Tri { public Vector3 a, b, c, n; public float d; public int r, sub; }
        sealed class Pair { public string wr, wm, or, om, wl, ol; public float area, gapMin = 9f, gapMax; public Vector3 at; public int tris; public bool differ, facing; public List<string> dump = new(); }

        static string Arg(string k, string d) { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, k); return i >= 0 && i + 1 < a.Length ? a[i + 1] : d; }

        public static void Run()
        {
            var scene = Arg("-fightScene", "Assets/Scenes/OldTown.unity");
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            var dir = Arg("-fightOut", Path.Combine(root, "out", "scratch", "latch", "windowfight"));
            var dumpOf = Arg("-fightDump", "");
            float gap = float.Parse(Arg("-fightGap", "0.006"), System.Globalization.CultureInfo.InvariantCulture);
            Directory.CreateDirectory(dir);
            var t0 = System.DateTime.Now;
            EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            var rends = Object.FindObjectsByType<MeshRenderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy && r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly && r.GetComponent<MeshFilter>()?.sharedMesh).ToArray();
            var lit = rends.Where(r => r.sharedMaterials.Any(IsLit)).ToArray();
            foreach (var r in rends) foreach (var m in r.sharedMaterials) if (m && m.HasProperty("_Cull") && m.GetFloat("_Cull") == 0f) twoSided.Add(m.name);
            // renderers of one LODGroup at different levels never draw together: not a fight
            var lod = new Dictionary<Renderer, (LODGroup g, int i)>();
            foreach (var g in Object.FindObjectsByType<LODGroup>()) { var ls = g.GetLODs(); for (int i = 0; i < ls.Length; i++) foreach (var r in ls[i].renderers) if (r) lod[r] = (g, i); }
            var shares = new Dictionary<string, float> { ["DK_GlassDark"] = 0.65f, ["DK_Window_Tower"] = 0.7f, ["DK_Window_Teal"] = 0.7f, ["DK_Window_Film"] = 0.75f, ["DK_Cabin"] = 1f };
            // what RoadLight does to this slot at night: "lit" (a GlassSky pane: lit by its world cell, the same on two coplanar panes), "on"/"off" (a night-mode
            // material whose renderer the share picks by where it stands), or "dark" (not a window)
            string Night(Renderer r, Material m)
            {
                if (!m || !IsLit(m)) return "dark";
                if (m.shader && m.shader.name.Contains("GlassSky")) return "lit";
                var key = Lit.First(p => m.name.StartsWith(p)); float sh = shares[key];
                return sh >= 1f || Spot(r.bounds.center) < sh ? "on" : "off";
            }
            Debug.Log($"WindowFightProbe: {scene} opened in {(System.DateTime.Now - t0).TotalSeconds:0}s; {rends.Length} renderers, {lit.Length} with a night window");

            // every triangle of every renderer whose bounds touch a lit renderer's, hashed on a 2 m grid by its box
            const float cell = 2f; var grid = new Dictionary<(int, int, int), List<Tri>>(); var winTris = new List<Tri>();
            var near = new HashSet<int>(); var bounds = lit.Select(r => r.bounds).ToArray();
            for (int i = 0; i < rends.Length; i++) { var b = rends[i].bounds; b.Expand(0.05f); foreach (var lb in bounds) if (lb.Intersects(b)) { near.Add(i); break; } }
            long triCount = 0;
            foreach (int i in near)
            {
                var r = rends[i]; var mesh = r.GetComponent<MeshFilter>().sharedMesh; var v = mesh.vertices; var m = r.transform.localToWorldMatrix; var mats = r.sharedMaterials;
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    var t = mesh.GetTriangles(s); bool w = s < mats.Length && IsLit(mats[s]);
                    for (int k = 0; k < t.Length; k += 3)
                    {
                        Vector3 a = m.MultiplyPoint3x4(v[t[k]]), b = m.MultiplyPoint3x4(v[t[k + 1]]), c = m.MultiplyPoint3x4(v[t[k + 2]]);
                        var n = Vector3.Cross(b - a, c - a); float area2 = n.magnitude; if (area2 < 1e-6f) continue; n /= area2;
                        var tri = new Tri { a = a, b = b, c = c, n = n, d = Vector3.Dot(n, a), r = i, sub = s }; triCount++;
                        if (w) winTris.Add(tri);
                        var lo = Vector3.Min(a, Vector3.Min(b, c)); var hi = Vector3.Max(a, Vector3.Max(b, c));
                        int x0 = Mathf.FloorToInt(lo.x / cell), x1 = Mathf.FloorToInt(hi.x / cell), y0 = Mathf.FloorToInt(lo.y / cell), y1 = Mathf.FloorToInt(hi.y / cell), z0 = Mathf.FloorToInt(lo.z / cell), z1 = Mathf.FloorToInt(hi.z / cell);
                        if ((long)(x1 - x0 + 1) * (y1 - y0 + 1) * (z1 - z0 + 1) > 4000) continue; // a huge ground sheet: not a window's neighbour
                        for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) for (int z = z0; z <= z1; z++)
                        { if (!grid.TryGetValue((x, y, z), out var l)) grid[(x, y, z)] = l = new List<Tri>(); l.Add(tri); }
                    }
                }
            }
            Debug.Log($"WindowFightProbe: {near.Count} renderers near, {triCount} triangles, {winTris.Count} window triangles");

            // each window triangle sampled on a grid about 0.15 m apart (a big pane passes between spandrels: a few points miss them), every point against the
            // triangles hashed in its cell; a fight needs one of the two to write depth (two transparent films never fight: neither writes it)
            var pairs = new Dictionary<(int, int, int, int), Pair>(); var hits = new Dictionary<(int, int), (int n, float g0, float g1, Tri o)>(); var pts = new List<Vector3>();
            foreach (var wt in winTris)
            {
                float area = Vector3.Cross(wt.b - wt.a, wt.c - wt.a).magnitude * 0.5f;
                float edge = Mathf.Max((wt.b - wt.a).magnitude, Mathf.Max((wt.c - wt.b).magnitude, (wt.a - wt.c).magnitude));
                int N = Mathf.Clamp(Mathf.CeilToInt(edge / 0.15f), 2, 60); pts.Clear(); hits.Clear();
                for (int i = 0; i <= N; i++) for (int j = 0; j <= N - i; j++) { float u = (i + 1f / 3) / (N + 1), v = (j + 1f / 3) / (N + 1); pts.Add(wt.a + (wt.b - wt.a) * u + (wt.c - wt.a) * v); }
                bool wTrans = Transparent(rends[wt.r], wt.sub);
                foreach (var q in pts)
                {
                    if (!grid.TryGetValue((Mathf.FloorToInt(q.x / cell), Mathf.FloorToInt(q.y / cell), Mathf.FloorToInt(q.z / cell)), out var l)) continue;
                    foreach (var o in l)
                    {
                        if (o == wt || o.r == wt.r && o.sub == wt.sub) continue;
                        float cos = Vector3.Dot(o.n, wt.n); if (Mathf.Abs(cos) < 0.99966f) continue; // 1.5 degrees
                        float g = Mathf.Abs(Vector3.Dot(o.n, q) - o.d); if (g > gap) continue;
                        if (!Inside(o, q - o.n * (Vector3.Dot(o.n, q) - o.d))) continue;
                        if (o.r != wt.r && lod.TryGetValue(rends[o.r], out var lo1) && lod.TryGetValue(rends[wt.r], out var lo2) && lo1.g == lo2.g && lo1.i != lo2.i) continue;
                        if (wTrans && Transparent(rends[o.r], o.sub)) continue;
                        if (cos < 0f) { var om0 = o.sub < rends[o.r].sharedMaterials.Length ? rends[o.r].sharedMaterials[o.sub] : null; if (!om0 || !TwoSided(om0.name)) continue; } // back to back: one is culled
                        var hk = (o.r, o.sub); var h = hits.TryGetValue(hk, out var h0) ? h0 : (n: 0, g0: 9f, g1: 0f, o: o);
                        hits[hk] = (h.n + 1, Mathf.Min(h.g0, g), Mathf.Max(h.g1, g), h.o); break;   // one hit per point per other surface
                    }
                }
                foreach (var kv in hits)
                {
                    var key = (wt.r, wt.sub, kv.Key.Item1, kv.Key.Item2); var (n, g0, g1, o) = kv.Value;
                    if (!pairs.TryGetValue(key, out var pr))
                    {
                        var wr = rends[wt.r]; var or = rends[o.r];
                        var wmat = wr.sharedMaterials[wt.sub]; var omat = o.sub < or.sharedMaterials.Length ? or.sharedMaterials[o.sub] : null;
                        pairs[key] = pr = new Pair { wr = Named(wr.transform), wm = wmat?.name, or = Named(or.transform), om = omat?.name, at = (wt.a + wt.b + wt.c) / 3f, wl = Night(wr, wmat), ol = Night(or, omat) };
                        pr.differ = pr.wl != pr.ol || pr.wm != pr.om; pr.facing = Vector3.Dot(o.n, wt.n) > 0f;
                    }
                    pr.area += area * n / pts.Count; pr.tris++; pr.gapMin = Mathf.Min(pr.gapMin, g0); pr.gapMax = Mathf.Max(pr.gapMax, g1);
                    if (dumpOf.Length > 0 && pr.dump.Count < 12 && pr.wr.Contains(dumpOf))
                    { var w2l = rends[wt.r].transform.worldToLocalMatrix; pr.dump.Add($"      win {w2l.MultiplyPoint3x4(wt.a):F3} {w2l.MultiplyPoint3x4(wt.b):F3} {w2l.MultiplyPoint3x4(wt.c):F3} n {w2l.MultiplyVector(wt.n):F2}  other {w2l.MultiplyPoint3x4(o.a):F3} {w2l.MultiplyPoint3x4(o.b):F3} {w2l.MultiplyPoint3x4(o.c):F3} gap {g0 * 1000:0.0} mm, {n}/{pts.Count} points"); }
                }
            }
            var sb = new StringBuilder(); var all = pairs.Values.Where(p => p.area > 0.01f).ToList(); var rows = all.Where(p => p.differ).OrderByDescending(p => p.area).ToList();
            sb.AppendLine($"{all.Count} coplanar pairs (LOD siblings left out); {all.Count(p => !p.differ)} draw alike at night (same material, same lit state); back-to-back faces (one culled) are not counted");
            sb.AppendLine($"scene {scene}; gap < {gap * 1000:0} mm; {rows.Count} window/surface pairs overlap (area > 0.01 m2); total {rows.Sum(p => p.area):0.0} m2");
            foreach (var g in rows.GroupBy(p => (p.wm, p.wl, p.om, p.ol)).OrderByDescending(g => g.Sum(p => p.area)))
                sb.AppendLine($"  {g.Key.wm + " " + g.Key.wl,-40} vs {g.Key.om + " " + g.Key.ol,-40} {g.Count(),4} pairs  {g.Sum(p => p.area),8:0.0} m2  gap {g.Min(p => p.gapMin) * 1000:0.0}..{g.Max(p => p.gapMax) * 1000:0.0} mm");
            sb.AppendLine();
            foreach (var p in rows.Take(400)) { sb.AppendLine($"{p.area,7:0.00} m2 gap {p.gapMin * 1000:0.0}..{p.gapMax * 1000:0.0} mm  at {p.at:F1}  {p.wm} {p.wl} @ {p.wr}  vs  {p.om} {p.ol} @ {p.or}"); foreach (var l in p.dump) sb.AppendLine(l); }
            File.WriteAllText(Path.Combine(dir, "fights.txt"), sb.ToString());
            Debug.Log($"WindowFightProbe: {rows.Count} pairs → {dir}/fights.txt ({(System.DateTime.Now - t0).TotalSeconds:0}s)");
        }

        /// <summary>The LODGroups whose path holds -lodOf: fade mode, cross-fade animation, each level's screen height and renderers.</summary>
        public static void Lods()
        {
            EditorSceneManager.OpenScene(Arg("-fightScene", "Assets/Scenes/OldTown.unity"), OpenSceneMode.Single);
            var of = Arg("-lodOf", "CafeUpper"); var sb = new StringBuilder();
            sb.AppendLine($"QualitySettings.lodBias {QualitySettings.lodBias}, maximumLODLevel {QualitySettings.maximumLODLevel}");
            foreach (var g in Object.FindObjectsByType<LODGroup>())
            {
                if (!Named(g.transform).Contains(of)) continue;
                sb.AppendLine($"{Named(g.transform)}  fade {g.fadeMode} animate {g.animateCrossFading} size {g.size:0.00} pos {g.transform.position:F1} scale {g.transform.lossyScale:F2}");
                var ls = g.GetLODs(); for (int i = 0; i < ls.Length; i++) sb.AppendLine($"   L{i} screen {ls[i].screenRelativeTransitionHeight:0.000} fadeW {ls[i].fadeTransitionWidth:0.00}  {string.Join(", ", ls[i].renderers.Where(r => r).Select(r => r.name + "[" + string.Join("|", r.sharedMaterials.Where(m => m).Select(m => m.name)) + "]"))}");
            }
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            File.WriteAllText(Path.Combine(root, "out", "scratch", "latch", "windowfight", "lods.txt"), sb.ToString());
        }

        /// <summary>Every triangle of a model asset (-meshOf, a project path) inside a local box (-box x0,y0,z0,x1,y1,z1), by submesh: plane and extent.</summary>
        public static void Box()
        {
            var path = Arg("-meshOf", "Assets/Suborned/Kit/Models/Plaza/DK_CafeUpper_3_TB.fbx"); var b = Arg("-box", "-1.3,5,-11.4,1.3,13,-11.2").Split(',').Select(v => float.Parse(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            var lo = new Vector3(b[0], b[1], b[2]); var hi = new Vector3(b[3], b[4], b[5]); var sb = new StringBuilder();
            foreach (var mf in AssetDatabase.LoadAllAssetsAtPath(path).OfType<GameObject>().SelectMany(g => g.GetComponents<MeshFilter>()).Distinct())
            {
                var mesh = mf.sharedMesh; var mats = mf.GetComponent<MeshRenderer>()?.sharedMaterials ?? new Material[0]; var m = mf.transform.localToWorldMatrix; var v = mesh.vertices;
                sb.AppendLine($"{mf.name} ({mesh.name}) subs {mesh.subMeshCount} root-local");
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    var t = mesh.GetTriangles(s); var planes = new SortedDictionary<string, (int n, Vector3 mn, Vector3 mx)>();
                    for (int k = 0; k < t.Length; k += 3)
                    {
                        Vector3 a = m.MultiplyPoint3x4(v[t[k]]), c1 = m.MultiplyPoint3x4(v[t[k + 1]]), c2 = m.MultiplyPoint3x4(v[t[k + 2]]);
                        var mn = Vector3.Min(a, Vector3.Min(c1, c2)); var mx = Vector3.Max(a, Vector3.Max(c1, c2));
                        if (mx.x < lo.x || mn.x > hi.x || mx.y < lo.y || mn.y > hi.y || mx.z < lo.z || mn.z > hi.z) continue;
                        var n = Vector3.Cross(c1 - a, c2 - a).normalized; var key = $"n {n.x:0.00},{n.y:0.00},{n.z:0.00} d {Vector3.Dot(n, a):0.0000}";
                        planes[key] = planes.TryGetValue(key, out var e) ? (e.n + 1, Vector3.Min(e.mn, mn), Vector3.Max(e.mx, mx)) : (1, mn, mx);
                    }
                    foreach (var kv in planes) sb.AppendLine($"  sub {s} {(s < mats.Length && mats[s] ? mats[s].name : "?"),-28} {kv.Key}  x{kv.Value.n}  {kv.Value.mn:F3}..{kv.Value.mx:F3}");
                }
            }
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            File.WriteAllText(Path.Combine(root, "out", "scratch", "latch", "windowfight", "box.txt"), sb.ToString());
        }

        static bool Inside(Tri t, Vector3 p)
        {
            Vector3 v0 = t.b - t.a, v1 = t.c - t.a, v2 = p - t.a; float d00 = Vector3.Dot(v0, v0), d01 = Vector3.Dot(v0, v1), d11 = Vector3.Dot(v1, v1), d20 = Vector3.Dot(v2, v0), d21 = Vector3.Dot(v2, v1);
            float den = d00 * d11 - d01 * d01; if (Mathf.Abs(den) < 1e-12f) return false;
            float v = (d11 * d20 - d01 * d21) / den, w = (d00 * d21 - d01 * d20) / den; const float e = -0.001f; return v >= e && w >= e && v + w <= 1f - e;
        }

        static readonly HashSet<string> twoSided = new();
        static bool Transparent(Renderer r, int sub) { var ms = r.sharedMaterials; var m = sub < ms.Length ? ms[sub] : null; return m && m.renderQueue >= 2500; }
        static bool TwoSided(string m) => m != null && twoSided.Contains(m);
        static float Spot(Vector3 p) { float h = Mathf.Sin(Mathf.Round(p.x) * 12.9898f + Mathf.Round(p.y) * 4.1414f + Mathf.Round(p.z) * 78.233f) * 43758.5453f; return h - Mathf.Floor(h); } // RoadLight.Spot

        static string Named(Transform t) { var s = t.name; for (var p = t.parent; p && s.Length < 140; p = p.parent) s = p.name + "/" + s; return s; }
    }
}
