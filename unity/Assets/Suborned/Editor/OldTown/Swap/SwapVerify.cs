#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Suborned.OldTown.Editor.DecoKit;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 075 swap: the Handover zone regression. The zone's geometry in the swapped scene against our pipeline's own scene built
    /// from the same pinned export: every drawn triangle (LOD 0, active renderers) whose centroid lies 2 m or more inside the zone, as a
    /// multiset of centroids at 1 cm with its material, hashed. Equal hashes = the zone is ours exactly as built (c044's zone: the export
    /// is HEAD's, whose zone is identical to c044's). Also counts any old triangle left standing in the zone (must be 0).</summary>
    internal static class SwapVerify
    {
        static readonly string[] Skip = { "cameras", "lighting", "sea", "source" };

        /// <summary>Per material: the drawn area clipped exactly to the inner zone and its first moments (re-triangulation at the zone edge
        /// does not change them); kit renderers whose meshes are not readable as one key each (mesh, centre at 1 cm, materials).</summary>
        static (int tris, string hash, Dictionary<string, int> byMat, Dictionary<string, double[]> area, string kitHash, List<string> kit) Zone(IEnumerable<Renderer> rs, float dz)
        {
            var inner = OldTownSwap.ZonePlan; inner.xMin += 2f; inner.yMin += 2f; inner.xMax -= 2f; inner.yMax -= 2f;
            var regions = SwapMesh.Inside(inner);
            var keys = new List<string>(); var byMat = new Dictionary<string, int>(); var area = new Dictionary<string, double[]>();
            int tris = 0;
            foreach (var r in rs)
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                var lod = r.GetComponentInParent<LODGroup>();
                if (lod && !lod.GetLODs().Take(1).SelectMany(l => l.renderers).Contains(r) && lod.GetLODs().SelectMany(l => l.renderers).Contains(r)) continue;
                var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
                if (!mf.sharedMesh.isReadable)
                {
                    var bc = r.bounds.center; bc.z -= dz;
                    if (!SwapMesh.Inside(inner, new Vector2(bc.x, bc.z))) continue;
                    var mk = string.Join("+", r.sharedMaterials.Select(x => x ? x.name : "-"));
                    keys.Add($"R:{mf.sharedMesh.name},{Mathf.RoundToInt(bc.x * 100)},{Mathf.RoundToInt(bc.y * 100)},{Mathf.RoundToInt(bc.z * 100)},{mk}");
                    byMat["(kit renderer) " + mk] = byMat.TryGetValue("(kit renderer) " + mk, out var q) ? q + 1 : 1;
                    continue;
                }
                var m = mf.sharedMesh; var v = m.vertices; var l2w = r.transform.localToWorldMatrix; var mats = r.sharedMaterials;
                var off = new Vector3(0f, 0f, -dz);
                for (int s = 0; s < m.subMeshCount; s++)
                {
                    var tri = m.GetTriangles(s); string mat = s < mats.Length && mats[s] ? mats[s].name : "-";
                    for (int i = 0; i + 2 < tri.Length; i += 3)
                    {
                        var poly = new List<SwapMesh.V> { new SwapMesh.V { W = l2w.MultiplyPoint3x4(v[tri[i]]) + off }, new SwapMesh.V { W = l2w.MultiplyPoint3x4(v[tri[i + 1]]) + off }, new SwapMesh.V { W = l2w.MultiplyPoint3x4(v[tri[i + 2]]) + off } };
                        foreach (var h in regions[0]) { poly = SwapMesh.Clip(poly, h); if (poly.Count < 3) break; }
                        if (poly.Count < 3) continue;
                        if (!area.TryGetValue(mat, out var acc)) area[mat] = acc = new double[4];
                        for (int k = 1; k + 1 < poly.Count; k++)
                        {
                            Vector3 a0 = poly[0].W, a1 = poly[k].W, a2 = poly[k + 1].W; double ar = Vector3.Cross(a1 - a0, a2 - a0).magnitude * .5; var c = (a0 + a1 + a2) / 3f;
                            acc[0] += ar; acc[1] += ar * c.x; acc[2] += ar * c.y; acc[3] += ar * c.z;
                        }
                        tris++;
                    }
                }
            }
            var kitKeys = keys.ToList(); kitKeys.Sort(System.StringComparer.Ordinal);
            string kitHash; using (var sk = SHA256.Create()) kitHash = string.Concat(sk.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", kitKeys))).Select(b => b.ToString("x2")));
            foreach (var kv in area) keys.Add($"A:{kv.Key},{kv.Value[0]:0.0},{kv.Value[1] / System.Math.Max(1e-9, kv.Value[0]):0.00},{kv.Value[2] / System.Math.Max(1e-9, kv.Value[0]):0.00},{kv.Value[3] / System.Math.Max(1e-9, kv.Value[0]):0.00}");
            keys.Sort(System.StringComparer.Ordinal);
            using var sha = SHA256.Create();
            var hsh = string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", keys))).Select(b => b.ToString("x2")));
            return (tris, hsh, byMat, area, kitHash, kitKeys);
        }

        internal static void Run()
        {
            string ours = DecoKitStage.Arg("swapOurs", "Assets/Scenes/probe-oldtown-ours.unity"), scene = DecoKitStage.Arg("swapScene", "OldTown");
            var outDir = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..")), DecoKitStage.Arg("swapOut", "briefs/075/oldbuild/swap"));
            var s0 = EditorSceneManager.OpenScene(ours, OpenSceneMode.Single);
            var a = Zone(s0.GetRootGameObjects().Where(g => System.Array.IndexOf(Skip, g.name) < 0).SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).Where(r => !InHorizon(r.transform)), 0f);
            var s1 = EditorSceneManager.OpenScene($"Assets/Scenes/{scene}.unity", OpenSceneMode.Single);
            var roots = s1.GetRootGameObjects();
            var oursRoot = roots.First(g => g.name.StartsWith("Ours ("));
            var b = Zone(oursRoot.GetComponentsInChildren<Renderer>(true), OldTownSwap.DZ);
            var inner = OldTownSwap.ZoneOld;
            var who = roots.Where(g => !g.name.StartsWith("Ours (") && !g.name.StartsWith("Network (")).SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
                .Where(r => r.enabled && r.gameObject.activeInHierarchy && SwapMesh.Inside(inner, new Vector2(r.bounds.center.x, r.bounds.center.z)))
                .GroupBy(r => Path3(r.transform)).ToDictionary(g => g.Key, g => (object)g.Count());
            var diff = a.byMat.Keys.Union(b.byMat.Keys).Where(k => a.byMat.GetValueOrDefault(k) != b.byMat.GetValueOrDefault(k)).Select(k => $"{k} {a.byMat.GetValueOrDefault(k)}->{b.byMat.GetValueOrDefault(k)}").ToList();
            double A(Dictionary<string, double[]> d, string k) => d.TryGetValue(k, out var x) ? x[0] : 0;
            diff.AddRange(a.area.Keys.Union(b.area.Keys).Where(k => System.Math.Abs(A(a.area, k) - A(b.area, k)) > .01 * System.Math.Max(1, A(a.area, k)) / 100.0).Select(k => $"area {k} {A(a.area, k):0.00}->{A(b.area, k):0.00} m2"));
            double totA = a.area.Values.Sum(x => x[0]), totB = b.area.Values.Sum(x => x[0]);
            var onlyA = a.kit.Except(b.kit).Take(12).ToList(); var onlyB = b.kit.Except(a.kit).Take(12).ToList();
            var areaDiff = a.area.Keys.Union(b.area.Keys).Select(k => (k, d: A(b.area, k) - A(a.area, k))).Where(x => System.Math.Abs(x.d) > .05).OrderByDescending(x => System.Math.Abs(x.d)).Take(12).Select(x => $"{x.k} {x.d:+0.00;-0.00} m2").ToList();
            var json = SwapExport.Json(new Dictionary<string, object>
            {
                ["schema"] = "latch.oldtown-zone-regression/1", ["zoneInner"] = "the Handover zone, 2 m in from its edge (plan x -224..-86, y -370..-150)",
                ["ours"] = new Dictionary<string, object> { ["scene"] = ours, ["triangles"] = a.tris, ["areaM2"] = (float)totA, ["kitRenderers"] = a.byMat.Where(k => k.Key.StartsWith("(kit")).Sum(k => k.Value), ["sha256"] = a.hash },
                ["swapped"] = new Dictionary<string, object> { ["scene"] = $"Assets/Scenes/{scene}.unity", ["triangles"] = b.tris, ["areaM2"] = (float)totB, ["kitRenderers"] = b.byMat.Where(k => k.Key.StartsWith("(kit")).Sum(k => k.Value), ["sha256"] = b.hash },
                ["identical"] = diff.Count == 0 && a.kitHash == b.kitHash, ["kitRenderersIdentical"] = a.kitHash == b.kitHash, ["kitOnlyInOurs"] = onlyA, ["kitOnlyInSwapped"] = onlyB, ["areaDiffs"] = areaDiff, ["materialCountsThatDiffer"] = diff,
                ["oldRenderersInZoneByPath"] = who,
                ["note"] = "identity = per material, the area clipped exactly to the inner zone and its centroid (1 cm), plus every imported kit renderer by mesh, centre and materials; material names compare by name (the 18 kit materials swapped for HEAD's keep their names)",
            });
            File.WriteAllText(Path.Combine(outDir, $"zone-regression-{scene}.json"), json);
            Debug.Log($"[Swap] zone regression {scene}: ours {a.tris} tris {a.hash[..12]}, swapped {b.tris} tris {b.hash[..12]}, identical {diff.Count == 0 && a.kitHash == b.kitHash} (kit renderers {a.kitHash == b.kitHash}, area {totA:0}->{totB:0} m2); old renderers left in the zone {who.Values.Sum(x => (int)x)}; differing materials {diff.Count}");
        }

        static string Path3(Transform t) { var parts = new List<string>(); for (var x = t; x && parts.Count < 4; x = x.parent) parts.Insert(0, x.name.Length > 30 ? x.name[..30] : x.name); return string.Join("/", parts.Take(2)) + (t.root != t ? " <- " + t.root.name : ""); }
        static bool InHorizon(Transform t) { for (var x = t; x; x = x.parent) if (x.name == "horizon") return true; return false; }
    }
}
#endif
