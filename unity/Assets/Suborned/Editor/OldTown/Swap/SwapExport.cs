#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Suborned.OldTown.Editor.DecoKit;
using Suborned.World;
using UnityEditor;
using UnityEngine;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 075 swap: the old-built scene's geometry as JSON, in the plan frame (plan x, y = old Unity x, z - 206), for Splice's
    /// plan-match check (built.json, `latch.oldbuilt/1`, a draft until Splice fixes the schema with READY-match-splice) and for the play-view
    /// occlusion measure (occluders-&lt;heights&gt;.json, read by briefs/075/oldbuild/swap/tools/occlusion.ts with tools/world/occlusion.ts).</summary>
    internal static class SwapExport
    {
        internal static readonly Dictionary<string, object> Census = new();
        static readonly List<object> kerbs = new(), pave = new(), roads = new();

        static float R(float v) => Mathf.Round(v * 100f) / 100f;
        static float[] Pl(Vector3 w) => new[] { R(w.x), R(w.z - OldTownSwap.DZ) };

        /// <summary>Before the tile merge: every kerb stone and pavement module as placed (centre, yaw), and the asphalt as triangles.</summary>
        internal static void Ground(OldTownSwap.Report rep)
        {
            kerbs.Clear(); pave.Clear(); roads.Clear();
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (!PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)) continue;
                if (InOurs(t)) continue;
                var piece = OldTownSwap.PieceOf(t.name);
                if (piece.StartsWith("Kerb_")) kerbs.Add(new Dictionary<string, object> { ["piece"] = piece, ["at"] = Pl(t.position), ["yaw"] = R(t.eulerAngles.y), ["top"] = R(t.position.y + L.Walk) });
                else if (piece.StartsWith("Pave_") || piece.StartsWith("TreePit_")) pave.Add(new Dictionary<string, object> { ["piece"] = piece, ["at"] = Pl(t.position), ["size"] = 2, ["yaw"] = R(t.eulerAngles.y), ["top"] = R(t.position.y + L.Walk) });  // yaw: Splice's ask 1 (Unity yaw, degrees)
            }
            var road = DecoKitGroundMesh.Road();
            foreach (var mr in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (InOurs(mr.transform) || !mr.sharedMaterials.Contains(road)) continue;
                var mf = mr.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh || !mf.sharedMesh.isReadable) continue;
                var m = mf.sharedMesh; var v = m.vertices; var l2w = mf.transform.localToWorldMatrix;
                int sub = Array.IndexOf(mr.sharedMaterials, road); if (sub < 0 || sub >= m.subMeshCount) continue;
                var tris = m.GetTriangles(sub); var flat = new List<float>(tris.Length * 2);
                foreach (var i in tris) { var w = l2w.MultiplyPoint3x4(v[i]); flat.Add(R(w.x)); flat.Add(R(w.z - OldTownSwap.DZ)); }
                roads.Add(new Dictionary<string, object> { ["name"] = mr.name, ["top"] = R(mr.bounds.max.y), ["triangles"] = flat });
            }
            rep.Log($"export: {kerbs.Count} kerb stones, {pave.Count} pavement modules, {roads.Count} asphalt meshes (as placed, before the merge)");
        }

        static bool InOurs(Transform t) { for (var x = t; x; x = x.parent) if (x.name.StartsWith("Ours (") || x.name.StartsWith("Network (")) return true; return false; }

        // ---------------------------------------------------------------- outlines (second build: Splice's ask 2, real ground-storey outlines)
        /// <summary>How high above a piece's foot its ground-storey outline is read (m): the shopfront and its piers, under the canopies and signs.</summary>
        internal const float OutlineBand = 2f;
        static readonly Dictionary<Mesh, Vector3[]> meshVerts = new();
        static Vector3[] Verts(Mesh m)
        {
            if (meshVerts.TryGetValue(m, out var v)) return v;
            if (m.isReadable) v = m.vertices;
            else using (var data = Mesh.AcquireReadOnlyMeshData(m)) { var a = new Unity.Collections.NativeArray<Vector3>(data[0].vertexCount, Unity.Collections.Allocator.Temp); data[0].GetVertices(a); v = a.ToArray(); a.Dispose(); }
            return meshVerts[m] = v;
        }

        /// <summary>The convex outline (plan frame, counter-clockwise) of every vertex of these meshes lying below foot + OutlineBand, the foot being their
        /// lowest vertex; null when there are fewer than three distinct points.</summary>
        internal static (float[][] ring, float y0, float y1) Outline(IEnumerable<(Mesh mesh, Matrix4x4 l2w)> parts)
        {
            var pts = new List<Vector3>(); float y0 = float.MaxValue, y1 = float.MinValue;
            foreach (var (m, l2w) in parts) foreach (var v in Verts(m)) { var wv = l2w.MultiplyPoint3x4(v); pts.Add(wv); y0 = Mathf.Min(y0, wv.y); y1 = Mathf.Max(y1, wv.y); }
            if (pts.Count < 3) return (null, 0, 0);
            var low = pts.Where(q => q.y <= y0 + OutlineBand).Select(q => new Vector2(q.x, q.z)).ToList();
            var hull = Hull(low);
            if (hull.Count < 3) return (null, 0, 0);
            return (hull.Select(q => new[] { R(q.x), R(q.y - OldTownSwap.DZ) }).ToArray(), R(y0), R(y1));
        }

        static List<Vector2> Hull(List<Vector2> p)
        {
            p = p.Select(q => new Vector2(Mathf.Round(q.x * 100f) / 100f, Mathf.Round(q.y * 100f) / 100f)).Distinct().ToList();
            if (p.Count < 3) return p;
            p.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            var h = new List<Vector2>();
            float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
            foreach (var q in p) { while (h.Count >= 2 && Cross(h[^2], h[^1], q) <= 0) h.RemoveAt(h.Count - 1); h.Add(q); }
            int lo = h.Count + 1;
            for (int i = p.Count - 2; i >= 0; i--) { var q = p[i]; while (h.Count >= lo && Cross(h[^2], h[^1], q) <= 0) h.RemoveAt(h.Count - 1); h.Add(q); }
            h.RemoveAt(h.Count - 1);
            return h;
        }

        static IEnumerable<(Mesh, Matrix4x4)> Parts(Transform t) =>
            t.GetComponentsInChildren<MeshFilter>().Where(mf => mf.sharedMesh && mf.GetComponent<MeshRenderer>() is { enabled: true }).Select(mf => (mf.sharedMesh, mf.transform.localToWorldMatrix));

        /// <summary>The oriented footprint of a prefab instance from its renderers' local bounds (plan points, counter-clockwise), with base and top.
        /// (The first build's footprint; the second build exports Outline and keeps this only for pieces with no mesh.)</summary>
        static (float[][] ring, float y0, float y1) Box(Transform t, bool behindFacade)
        {
            var w2l = t.worldToLocalMatrix; bool any = false; Bounds lb = default;
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                var b = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                    var l = w2l.MultiplyPoint3x4(c); if (!any) { lb = new Bounds(l, Vector3.zero); any = true; } else lb.Encapsulate(l);
                }
            }
            if (!any) return (null, 0, 0);
            if (behindFacade && lb.max.z > 0f) lb.max = new Vector3(lb.max.x, lb.max.y, Mathf.Max(lb.min.z + .5f, 0f));  // a frontage piece's pivot is on its facade line: canopies and signs in front are not the building
            var l2w = t.localToWorldMatrix; var ring = new List<float[]>();
            foreach (var (x, z) in new[] { (lb.min.x, lb.min.z), (lb.max.x, lb.min.z), (lb.max.x, lb.max.z), (lb.min.x, lb.max.z) }) ring.Add(Pl(l2w.MultiplyPoint3x4(new Vector3(x, lb.min.y, z))));
            var bb = t.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            return (ring.ToArray(), R(bb.min.y), R(bb.max.y));
        }

        static readonly string[] Masses = { "CityMass_", "Tower plinths", "Hall fill", "Crescent wedges" };
        static readonly HashSet<string> BuildingCats = new() { "Skyline", "Facades", "Plaza", "Landmarks", "Highrise", "Skyport", "Alleys" };

        /// <summary>The buildings: every frontage piece (named "&lt;piece&gt; #run.segment": one building per segment) and the other standing old
        /// pieces of the building families, each an oriented box; generated masses (city mass chunks, roof decks, plinths, wedges) as boxes per part.</summary>
        static List<Dictionary<string, object>> OldSolids(bool forOcclusion)
        {
            var cats = DecoKitData.Assets().assets.GroupBy(a => a.name).ToDictionary(g => g.Key, g => g.First().category);
            var outl = new List<Dictionary<string, object>>();
            var inPrefab = new HashSet<Renderer>();
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (!PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) || InOurs(t) || !t.gameObject.activeInHierarchy) continue;
                foreach (var r in t.GetComponentsInChildren<Renderer>()) inPrefab.Add(r);
                var piece = OldTownSwap.PieceOf(t.name);
                bool frontage = t.name.Contains(" #");
                cats.TryGetValue(piece, out var cat);
                if (!frontage && !(cat != null && BuildingCats.Contains(cat))) continue;
                if (piece.StartsWith("Alley") && !piece.StartsWith("AlleyWall")) continue;
                if (cat == "Plaza" && !piece.StartsWith("Hall")) continue;
                // the occlusion measure counts buildings, as tools/world/occlusion.ts does for an export (no landmarks, roofs, signs or structures)
                if (forOcclusion && !frontage && (cat == "Landmarks" || cat == "Skyport")) continue;
                var (ring, y0, y1) = Outline(Parts(t));
                if (ring == null) (ring, y0, y1) = Box(t, frontage);
                if (ring == null || y1 - y0 < 2.5f) continue;
                var d = new Dictionary<string, object> { ["piece"] = piece, ["kind"] = frontage ? "frontage" : cat == "Landmarks" || cat == "Skyport" ? "landmark" : "building", ["ring"] = ring, ["base"] = y0, ["top"] = y1 };
                if (frontage) { var tag = t.name[(t.name.IndexOf(" #", StringComparison.Ordinal) + 2)..]; d["building"] = "run" + tag; d["front"] = new[] { R(t.forward.x), R(t.forward.z) }; d["at"] = Pl(t.position); }
                else d["building"] = piece + "@" + string.Join(",", Pl(t.position));
                outl.Add(d);
            }
            foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (!mr || !mr.enabled || inPrefab.Contains(mr) || InOurs(mf.transform) || !mf.sharedMesh || !mf.sharedMesh.isReadable) continue;
                if (mr.bounds.size.y < 2.5f) continue;
                var gname = mf.name + " " + (mf.transform.parent ? mf.transform.parent.name : "");
                if (!Masses.Any(m => gname.Contains(m))) continue;  // building masses only (not piers, canals, wedges of water)
                var rd = new SwapMesh.Read(mf.sharedMesh, mf.transform.localToWorldMatrix);
                var ids = rd.Components(out int count);
                var cb = new Bounds[count]; var has = new bool[count];
                for (int s = 0; s < rd.Subs.Count; s++)
                    for (int t = 0; t < ids[s].Length; t++)
                    {
                        int k = ids[s][t];
                        for (int j = 0; j < 3; j++) { var p = rd.Verts[rd.Subs[s][3 * t + j]].W; if (!has[k]) { cb[k] = new Bounds(p, Vector3.zero); has[k] = true; } else cb[k].Encapsulate(p); }
                    }
                var low = new List<Vector2>[count];
                for (int s2 = 0; s2 < rd.Subs.Count; s2++)
                    for (int t2 = 0; t2 < ids[s2].Length; t2++)
                    {
                        int k = ids[s2][t2];
                        for (int j = 0; j < 3; j++) { var q = rd.Verts[rd.Subs[s2][3 * t2 + j]].W; if (q.y <= cb[k].min.y + OutlineBand) (low[k] ??= new List<Vector2>()).Add(new Vector2(q.x, q.z)); }
                    }
                for (int k = 0; k < count; k++)
                {
                    var b = cb[k]; if (b.size.y < 2.5f || b.size.x < .5f || b.size.z < .5f) continue;
                    var hull = low[k] != null ? Hull(low[k]) : null;
                    var ring = hull != null && hull.Count >= 3 ? hull.Select(q => new[] { R(q.x), R(q.y - OldTownSwap.DZ) }).ToArray()
                        : new[] { Pl(new Vector3(b.min.x, 0, b.min.z)), Pl(new Vector3(b.max.x, 0, b.min.z)), Pl(new Vector3(b.max.x, 0, b.max.z)), Pl(new Vector3(b.min.x, 0, b.max.z)) };
                    outl.Add(new Dictionary<string, object> { ["piece"] = "mesh:" + mf.name, ["kind"] = "mass", ["building"] = $"{mf.name}#{k}", ["ring"] = ring, ["base"] = R(b.min.y), ["top"] = R(b.max.y) });
                }
            }
            return outl;
        }

        internal static void Built(OldTownSwap.Report rep, string path, WorldDto w, string heights, string hq, string scene)
        {
            var solids = OldSolids(false);
            var frontages = solids.Where(s => s.ContainsKey("front") && s["base"] is float b && b < 1f).Select(s => new Dictionary<string, object>
            { ["building"] = s["building"], ["piece"] = s["piece"], ["at"] = s["at"], ["normal"] = s["front"], ["w"] = 6, ["kind"] = "bay-front (the old builders model no doors: a ground-storey bay's facade point and outward normal)" }).ToList();
            var levels = new List<object>
            {
                new Dictionary<string, object> { ["id"] = "old/road", ["height"] = 0f, ["what"] = "the old asphalt deck (DecoKitGroundMesh.Road), lifted from -0.002 to plan 0.5.5's 0 (SwapWest.LiftRoad)" },
                new Dictionary<string, object> { ["id"] = "old/walk", ["height"] = L.Walk, ["what"] = "the old pavement, flags and verges (DecoKitStreetLayout.Walk, plan 0.5.5's 0.18 by edit E10)" },
                new Dictionary<string, object> { ["id"] = "old/promenade", ["height"] = P.PromenadeY, ["what"] = "the harbour quay promenade (DecoKitCityPlan.PromenadeY)" },
                new Dictionary<string, object> { ["id"] = "old/water", ["height"] = P.WaterY, ["what"] = "the harbour water" },
            };
            foreach (var l in w.Levels) foreach (var d in l.Decks) levels.Add(new Dictionary<string, object> { ["id"] = d.Id, ["height"] = l.Height, ["ring"] = d.Ring.Select(v => new[] { R(v.x), R(v.y) }).ToArray(), ["what"] = "ours (export levels): " + l.Name });
            foreach (var st in w.Transit.Stations) levels.Add(new Dictionary<string, object> { ["id"] = st.Id, ["height"] = R(st.Base + st.Height), ["ring"] = st.Ring.Select(v => new[] { R(v.x), R(v.y) }).ToArray(), ["what"] = "ours: sky concourse platform" });
            var doc = new Dictionary<string, object>
            {
                ["schema"] = "latch.oldbuilt/1",
                ["status"] = "second build (plan 0.5.5): read by tools/world/oldmatch/latch.ts",
                ["plan"] = OldTownSwap.PlanFacts,
                ["rings"] = $"buildings: the convex outline of each piece's vertices within {OutlineBand} m of its foot (its ground storey: shopfront and piers, under canopies and signs), not bounds boxes; city mass parts the same per connected part; pavements carry their Unity yaw (deg)",
                ["brief"] = "075 remap step 2, the swap",
                ["frame"] = "plan (x, y) = old Unity (x, z - 206), metres; heights in metres above the old road deck's zero",
                ["scene"] = $"Assets/Scenes/{scene}.unity", ["heights"] = heights, ["spacehq"] = hq,
                ["export"] = new Dictionary<string, object> { ["source"] = w.Source, ["sha256"] = w.Sha256 },
                ["handoverZone"] = new[] { OldTownSwap.ZonePlan.xMin, OldTownSwap.ZonePlan.xMax, OldTownSwap.ZonePlan.yMin, OldTownSwap.ZonePlan.yMax },
                ["inZone"] = "inside the zone the scene is ours as exported: compare the zone to the export itself, not to this file",
                ["buildings"] = solids, ["frontages"] = frontages, ["kerbs"] = kerbs, ["pavements"] = pave, ["roads"] = roads, ["levels"] = levels,
                ["counts"] = new Dictionary<string, object> { ["buildingPieces"] = solids.Count, ["buildings"] = solids.Select(s => s["building"]).Distinct().Count(), ["frontages"] = frontages.Count, ["kerbs"] = kerbs.Count, ["pavements"] = pave.Count, ["roads"] = roads.Count },
            };
            File.WriteAllText(path, Json(doc));
            rep.Log($"export: {Path.GetFileName(path)}: {solids.Count} building pieces in {solids.Select(s => s["building"]).Distinct().Count()} buildings, {frontages.Count} street bays, {kerbs.Count} kerbs, {pave.Count} pavement modules, {roads.Count} road meshes, {levels.Count} levels");
        }

        internal static void Occluders(OldTownSwap.Report rep, string path, string heights)
        {
            var solids = OldSolids(true);
            File.WriteAllText(path, Json(new Dictionary<string, object> { ["schema"] = "latch.oldtown-occluders/1", ["heights"] = heights, ["frame"] = "plan", ["zone"] = "outside the Handover zone only (the zone is ours: take it from the export)", ["occluders"] = solids }));
            rep.Log($"export: {Path.GetFileName(path)}: {solids.Count} occluders");
        }

        internal static void WriteReport(OldTownSwap.Report rep, string path, string heights, string hq, string scene, WorldDto w)
        {
            File.WriteAllText(path, Json(new Dictionary<string, object>
            {
                ["schema"] = "latch.oldtown-swap/1", ["scene"] = $"Assets/Scenes/{scene}.unity", ["heights"] = heights, ["spacehq"] = hq,
                ["export"] = new Dictionary<string, object> { ["source"] = w.Source, ["sha256"] = w.Sha256 },
                ["log"] = rep.Lines, ["gaveWay"] = rep.GaveWay.ToDictionary(k => k.Key, k => (object)k.Value.ToDictionary(x => x.Key, x => (object)x.Value)),
                ["gaveWayList"] = rep.GaveWayList, ["census"] = Census, ["towers"] = rep.Towers,
            }));
        }

        // ---------------------------------------------------------------- a small JSON writer
        internal static string Json(object o) { var sb = new StringBuilder(); W(sb, o, 0); return sb.ToString(); }
        static void W(StringBuilder sb, object o, int depth)
        {
            switch (o)
            {
                case null: sb.Append("null"); break;
                case string s: sb.Append('"').Append(s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n")).Append('"'); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case float f: sb.Append(float.IsFinite(f) ? f.ToString("0.###", CultureInfo.InvariantCulture) : "null"); break;
                case double d: sb.Append(double.IsFinite(d) ? d.ToString("0.###", CultureInfo.InvariantCulture) : "null"); break;
                case int i: sb.Append(i); break;
                case long l: sb.Append(l); break;
                case IDictionary dict:
                    sb.Append('{'); bool first = true;
                    foreach (DictionaryEntry e in dict) { if (!first) sb.Append(','); first = false; if (depth < 1) sb.Append("\n "); W(sb, e.Key.ToString(), depth + 1); sb.Append(':'); W(sb, e.Value, depth + 1); }
                    if (depth < 1) sb.Append('\n'); sb.Append('}'); break;
                case IEnumerable en:
                    sb.Append('['); bool f2 = true;
                    foreach (var x in en) { if (!f2) sb.Append(','); f2 = false; if (depth < 2 && x is IDictionary) sb.Append("\n "); W(sb, x, depth + 1); }
                    sb.Append(']'); break;
                default: sb.Append('"').Append(o).Append('"'); break;
            }
        }
    }
}
#endif
