using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>Brief 075 civic restyle (Latch): the regression manifest of a built scene, so a before and an after build compare as
    /// "materials only". Per scene, split into the Handover zone (plan x -226..-84, y -372..-148) and the rest:
    /// - batched geometry (meshes that are not kit assets): every triangle by its world centroid (1 mm) and area, as an order-free multiset
    ///   hash with and without its material, and per material;
    /// - kit renderers (a kit mesh asset): hierarchy path, mesh, world matrix, enabled/active, shadow mode, rendering layers, without and with
    ///   materials (listed in the zone);
    /// - transforms: path and world TRS of every object outside the batches root (batch names carry their material);
    /// - colliders: path, type, bounds, trigger, enabled, mesh.
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Review.Restyle075.Manifest
    ///         -restyleManifest "Assets/Scenes/a.unity=0=out/x/a.json;Assets/Scenes/OldTown.unity=206=out/x/b.json"   (dz: plan y = Unity z - dz)</summary>
    public static class Restyle075
    {
        static HashSet<string> dump; static List<string> dumpLines;
        static readonly Rect Zone = Rect.MinMaxRect(-226f, -372f, -84f, -148f);

        public static void Manifest() => HarbourWorldBuilder.Batch(() =>
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-restyleManifest");
            if (i < 0 || i + 1 >= a.Length) throw new ArgumentException("-restyleManifest \"<scene>=<dz>=<out.json>;...\"");
            foreach (var spec in a[i + 1].Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var p = spec.Split('=');
                var scene = EditorSceneManager.OpenScene(p[0], OpenSceneMode.Single);
                float dz = float.Parse(p[1], CultureInfo.InvariantCulture);
                var outPath = Path.GetFullPath(Path.Combine(HarbourWorldBuilder.RepoRoot, p[2]));
                Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                dump = Array.IndexOf(a, "-restyleDump") is var di and >= 0 && di + 1 < a.Length ? new HashSet<string>(a[di + 1].Split(','), StringComparer.Ordinal) : null;
                dumpLines = new List<string>();
                File.WriteAllText(outPath, Json(Scene(scene.GetRootGameObjects(), dz, p[0])));
                if (dump != null) { dumpLines.Sort(StringComparer.Ordinal); File.WriteAllLines(Path.ChangeExtension(outPath, ".dump.txt"), dumpLines); }
                Debug.Log($"restyle075: manifest {p[0]} (dz {dz}) -> {outPath}");
            }
        });

        sealed class Part
        {
            public long tris; public ulong geom, geomMat; public double area;
            public readonly SortedDictionary<string, (long tris, ulong hash, double area)> byMat = new(StringComparer.Ordinal);
            public long kits; public ulong kitHash, kitMatHash; public readonly List<string> kitList = new();
            public long transforms; public ulong transformHash;
            public long colliders; public ulong colliderHash; public readonly List<string> colliderList = new();
            public long batchRenderers, kitRenderers;
            public readonly List<string> overlay = new();  // what the restyle adds (CivicRestyle overlays: a path under civic-restyle/), kept out of the hashes
        }

        static ulong H(string s) { ulong h = 14695981039346656037UL; foreach (char c in s) { h ^= c; h *= 1099511628211UL; } h ^= h >> 33; h *= 0xff51afd7ed558ccdUL; h ^= h >> 33; return h; }
        static string R(float v, float q) => Mathf.RoundToInt(v / q).ToString(CultureInfo.InvariantCulture);
        static string V(Vector3 v, float q = .001f) => $"{R(v.x, q)},{R(v.y, q)},{R(v.z, q)}";
        static string PathOf(Transform t) { var sb = new StringBuilder(t.name); for (var x = t.parent; x; x = x.parent) sb.Insert(0, x.name + "/"); return sb.ToString(); }
        static bool InZone(Vector3 w, float dz) => Zone.Contains(new Vector2(w.x, w.z - dz));
        static bool Overlay(Transform t) { for (var x = t; x; x = x.parent) if (x.name.StartsWith("civic-restyle/", StringComparison.Ordinal)) return true; return false; }
        /// <summary>Under a batches group (the root, or the swap's copy under "Ours (...)"): batch names carry their material.</summary>
        static bool UnderBatches(Transform t) { for (var x = t; x; x = x.parent) if (x.name == "batches") return true; return false; }
        /// <summary>The "source" root is a label (what the scene was built from, and the restyle's sha), not geometry.</summary>
        static bool Label(Transform t) => t.root.name == "source";
        /// <summary>A material's name with the scene's own prefix taken off (the builder names a few per scene: &lt;scene&gt;-crossdots, -crosslight).</summary>
        static string MatName(Material m) => m ? System.Text.RegularExpressions.Regex.Replace(m.name, @"^.+-(crossdots|crosslight|sky-night)$", "<scene>-$1") : "-";

        static Dictionary<string, object> Scene(GameObject[] roots, float dz, string path)
        {
            var zone = new Part(); var rest = new Part();
            foreach (var r in roots.SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
            {
                if (r is not MeshRenderer) continue;
                var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
                var mesh = mf.sharedMesh; var mats = r.sharedMaterials;
                string state = $"{r.enabled}/{r.gameObject.activeInHierarchy}/{r.shadowCastingMode}/{r.renderingLayerMask}";
                if (AssetDatabase.Contains(mesh))
                {
                    var part = InZone(r.bounds.center, dz) ? zone : rest; part.kitRenderers++;
                    var m = r.localToWorldMatrix; var trs = $"{V(m.GetColumn(3))}|{V(m.rotation.eulerAngles, .01f)}|{V(m.lossyScale, .0001f)}";
                    string key = $"{PathOf(r.transform)}|{AssetDatabase.GetAssetPath(mesh)}:{mesh.name}|{trs}|{state}", mk = string.Join("+", mats.Select(MatName));
                    if (Overlay(r.transform)) { part.overlay.Add("renderer " + key + "|" + mk); continue; }
                    part.kits++; part.kitHash += H(key); part.kitMatHash += H(key + "|" + mk);
                    if (part == zone) part.kitList.Add(key + "|" + mk);
                    else if (dump != null) dumpLines.Add("KIT " + key + "|" + mk);
                    continue;
                }
                if (!mesh.isReadable) throw new InvalidDataException($"restyle075: {PathOf(r.transform)}: batched mesh {mesh.name} is not readable");
                var vs = mesh.vertices; var l2w = r.localToWorldMatrix; bool any = false, anyZone = false;
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    var tri = mesh.GetTriangles(s); string mat = s < mats.Length ? MatName(mats[s]) : "-";
                    for (int k = 0; k + 2 < tri.Length; k += 3)
                    {
                        Vector3 a = l2w.MultiplyPoint3x4(vs[tri[k]]), b = l2w.MultiplyPoint3x4(vs[tri[k + 1]]), c = l2w.MultiplyPoint3x4(vs[tri[k + 2]]);
                        var cen = (a + b + c) / 3f; float ar = Vector3.Cross(b - a, c - a).magnitude * .5f;
                        bool z = InZone(cen, dz); var part = z ? zone : rest; any = true; anyZone |= z;
                        string g = $"{V(cen)}|{R(ar, .0001f)}|{state}";
                        ulong hg = H(g), hm = H(g + "|" + mat);
                        if (z && dump != null && dump.Contains(mat)) dumpLines.Add($"{g}|{mat}|{PathOf(r.transform)}");
                        part.tris++; part.geom += hg; part.geomMat += hm; part.area += ar;
                        part.byMat.TryGetValue(mat, out var e); part.byMat[mat] = (e.tris + 1, e.hash + hg, e.area + ar);
                    }
                }
                if (any) (anyZone ? zone : rest).batchRenderers++;
            }
            foreach (var t in roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true)))
            {
                if (UnderBatches(t) || Label(t)) continue;
                var part = InZone(t.position, dz) ? zone : rest;
                if (Overlay(t)) { part.overlay.Add($"transform {PathOf(t)}|{V(t.position)}|{V(t.rotation.eulerAngles, .01f)}"); continue; }
                if (part == rest && dump != null) dumpLines.Add($"TF {PathOf(t)}|{V(t.position)}|{V(t.rotation.eulerAngles, .01f)}|{V(t.lossyScale, .0001f)}|{t.gameObject.activeSelf}");
                part.transforms++; part.transformHash += H($"{PathOf(t)}|{V(t.position)}|{V(t.rotation.eulerAngles, .01f)}|{V(t.lossyScale, .0001f)}|{t.gameObject.activeSelf}");
            }
            foreach (var c in roots.SelectMany(g => g.GetComponentsInChildren<Collider>(true)))
            {
                var part = InZone(c.bounds.center, dz) ? zone : rest;
                if (Overlay(c.transform)) { part.overlay.Add("COLLIDER " + PathOf(c.transform)); continue; }
                string mesh = c is MeshCollider mc && mc.sharedMesh ? $"{mc.sharedMesh.name}:{mc.sharedMesh.vertexCount}" : "";
                string key = $"{PathOf(c.transform)}|{c.GetType().Name}|{V(c.bounds.center)}|{V(c.bounds.size)}|{c.isTrigger}|{c.enabled}|{mesh}";
                part.colliders++; part.colliderHash += H(key); if (part == zone) part.colliderList.Add(key);
            }
            object Out(Part p) => new Dictionary<string, object>
            {
                ["batchTriangles"] = p.tris, ["batchAreaM2"] = Math.Round(p.area, 2), ["batchGeometryHash"] = p.geom.ToString("x16"), ["batchGeometryWithMaterialHash"] = p.geomMat.ToString("x16"),
                ["batchRenderers"] = p.batchRenderers,
                ["batchByMaterial"] = p.byMat.ToDictionary(kv => kv.Key, kv => (object)new Dictionary<string, object> { ["triangles"] = kv.Value.tris, ["areaM2"] = Math.Round(kv.Value.area, 2), ["hash"] = kv.Value.hash.ToString("x16") }),
                ["kitRenderers"] = p.kits, ["kitHash"] = p.kitHash.ToString("x16"), ["kitWithMaterialHash"] = p.kitMatHash.ToString("x16"),
                ["kit"] = p.kitList.OrderBy(x => x, StringComparer.Ordinal).Cast<object>().ToList(),
                ["transforms"] = p.transforms, ["transformHash"] = p.transformHash.ToString("x16"),
                ["overlay"] = p.overlay.OrderBy(x => x, StringComparer.Ordinal).Cast<object>().ToList(),
                ["colliders"] = p.colliders, ["colliderHash"] = p.colliderHash.ToString("x16"), ["colliderList"] = p.colliderList.OrderBy(x => x, StringComparer.Ordinal).Cast<object>().ToList(),
            };
            // the play nav the scene carries: the Handover host's ground (heights and walk kinds per cell, baked from the walk file)
            string nav = "no HandoverHost";
            var host = roots.SelectMany(g => g.GetComponentsInChildren<global::Suborned.Play.HandoverHost>(true)).FirstOrDefault();
            if (host != null && host.frame != null)
            {
                var f = host.frame; var bytes = new List<byte>();
                foreach (var h in f.heights) bytes.AddRange(BitConverter.GetBytes(h));
                bytes.AddRange(f.kinds ?? Array.Empty<byte>());
                using var sha = System.Security.Cryptography.SHA256.Create();
                nav = $"{f.nx}x{f.ny} at {f.cell} m, origin {f.origin}, sha256 " + string.Concat(sha.ComputeHash(bytes.ToArray()).Select(b => b.ToString("x2")));
            }
            return new Dictionary<string, object>
            {
                ["schema"] = "latch.restyle075-manifest/1", ["playNav"] = nav, ["scene"] = path, ["dz"] = dz, ["zone"] = "plan x -226..-84, y -372..-148 (a renderer by its bounds centre, a triangle by its centroid)",
                ["inZone"] = Out(zone), ["outside"] = Out(rest),
            };
        }

        static string Json(object o)
        {
            var sb = new StringBuilder(); Write(sb, o, 0); return sb.ToString();
        }
        static void Write(StringBuilder sb, object o, int d)
        {
            string ind = new(' ', d * 2), ind1 = new(' ', d * 2 + 2);
            switch (o)
            {
                case null: sb.Append("null"); break;
                case string s: sb.Append('"').Append(s.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"'); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case float f: sb.Append(f.ToString("R", CultureInfo.InvariantCulture)); break;
                case double x: sb.Append(x.ToString("R", CultureInfo.InvariantCulture)); break;
                case long or int: sb.Append(Convert.ToString(o, CultureInfo.InvariantCulture)); break;
                case Dictionary<string, object> m:
                    sb.Append("{\n"); int n = 0;
                    foreach (var kv in m) { sb.Append(ind1).Append('"').Append(kv.Key).Append("\": "); Write(sb, kv.Value, d + 1); if (++n < m.Count) sb.Append(','); sb.Append('\n'); }
                    sb.Append(ind).Append('}'); break;
                case List<object> l:
                    sb.Append("[\n"); for (int k = 0; k < l.Count; k++) { sb.Append(ind1); Write(sb, l[k], d + 1); if (k + 1 < l.Count) sb.Append(','); sb.Append('\n'); }
                    sb.Append(ind).Append(']'); break;
                default: sb.Append('"').Append(o).Append('"'); break;
            }
        }
    }
}
