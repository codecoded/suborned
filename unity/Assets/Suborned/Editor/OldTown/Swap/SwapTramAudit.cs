#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Suborned.World;
using UnityEngine;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 075 tram bed audit (edit E12's evidence): along every tram track of ours (transit.tram, both lines of a double track),
    /// every 1 m, at the track's centre and 0.6 m either side, what lies at road level (horizontal faces from -5 cm to +30 cm), by source.
    /// A sample is "on our bed" when a tile of ours (the old Tile_TramRail under SwapNetwork.RailsRoot, 2 x 2 m, its top at its pivot)
    /// covers it; then any other face within 0.8 mm of the tile's top is "level" (z-fights) and any face from 0.8 mm to 25 cm over it is
    /// "over" (paint, a slab, a kerb). Off our bed, two faces of different renderers within 0.8 mm of each other at the top is "level, no bed"
    /// (the first swap's Boulevard: the old track's tiles on the lifted deck). Read only.</summary>
    internal static class SwapTramAudit
    {
        const float Level = .0008f, Over = .25f, Cell = 2f;

        sealed class Src { public string Key; public int Level, Over, LevelNoBed; public List<float[]> At = new(); }

        internal static void Run(OldTownSwap.Report rep, WorldDto w, string path)
        {
            // our tiles
            var tiles = new List<(Vector3 p, Vector3 u, Vector3 v, Vector2[] polygon)>();
            var tileRs = new HashSet<Renderer>();
            foreach (var root in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude).Where(t => t.name == SwapNetwork.RailsRoot))
            {
                SwapTramRender.BindExisting(root);
                foreach (Transform t in root)
                {
                    Vector2[] polygon;
                    if(SwapTramRender.Applied.TryGetValue(t,out var f)) polygon=SwapTramRender.Polygon(f);
                    else {var m=t.localToWorldMatrix;polygon=new[]{new Vector3(-1,0,-1),new Vector3(1,0,-1),new Vector3(1,0,1),new Vector3(-1,0,1)}.Select(p=>m.MultiplyPoint3x4(p)).Select(p=>new Vector2(p.x,p.z)).ToArray();}
                    tiles.Add((t.position, t.right, t.forward,polygon));
                    foreach (var r in t.GetComponentsInChildren<Renderer>(true)) tileRs.Add(r);
                    foreach(var mf in SwapTramRender.Filters(t))tileRs.Add(mf.GetComponent<MeshRenderer>());
                }
            }
            var tileGrid = new Dictionary<Vector2Int, List<int>>();
            for (int i = 0; i < tiles.Count; i++)
            {
                var poly=tiles[i].polygon;
                for(int x=Mathf.FloorToInt(poly.Min(p=>p.x)/Cell);x<=Mathf.FloorToInt(poly.Max(p=>p.x)/Cell);x++)
                    for(int y=Mathf.FloorToInt(poly.Min(p=>p.y)/Cell);y<=Mathf.FloorToInt(poly.Max(p=>p.y)/Cell);y++)
                    {var k=new Vector2Int(x,y);if(!tileGrid.TryGetValue(k,out var list))tileGrid[k]=list=new();list.Add(i);}
            }
            float? TileTop(Vector2 q)
            {
                if (!tileGrid.TryGetValue(Key(q), out var l)) return null;
                float? top = null;
                foreach (var i in l)
                {
                    var d = new Vector3(q.x, tiles[i].p.y, q.y) - tiles[i].p;
                    var poly=tiles[i].polygon;bool positive=false,negative=false;
                    for(int k=0;k<poly.Length;k++){var a=poly[k];var b=poly[(k+1)%poly.Length];double c=((double)b.x-a.x)*(q.y-a.y)-((double)b.y-a.y)*(q.x-a.x);if(c>1e-6)positive=true;if(c < -1e-6)negative=true;}
                    if(!(positive&&negative))top=Mathf.Max(top??float.MinValue,tiles[i].p.y);
                }
                return top;
            }

            // samples (old frame)
            var samples = new List<(string track, Vector2 q, Vector2 plan)>();
            foreach (var x in w.Transit.Tram)
            {
                var line = x.Line; if (line == null || line.Length < 2) continue;
                var offs = x.Spacing > 0f ? new[] { -x.Spacing * .5f, x.Spacing * .5f } : new[] { 0f };
                float carry = 0f;
                for (int i = 1; i < line.Length; i++)
                {
                    Vector2 a = line[i - 1], b = line[i]; float len = Vector2.Distance(a, b); if (len < 1e-4f) continue;
                    Vector2 d = (b - a) / len, n = new(-d.y, d.x);
                    for (float s = carry; s < len; s += 1f)
                        foreach (var o in offs)
                            foreach (var lat in new[] { -.6f, 0f, .6f })
                            {
                                var pl = a + d * s + n * (o + lat);
                                samples.Add((x.Id, OldTownSwap.Old(pl), pl));
                            }
                    carry = Mathf.Repeat(carry - len, 1f);
                }
            }
            var cells = new HashSet<Vector2Int>(samples.Select(s => Key(s.q)));

            // faces at road level near the samples
            var lodHidden = new HashSet<Renderer>();
            foreach (var g in Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Exclude)) { var lods = g.GetLODs(); for (int i = 1; i < lods.Length; i++) foreach (var r in lods[i].renderers) if (r) lodHidden.Add(r); }
            var faces = new Dictionary<Vector2Int, List<(Vector3 a, Vector3 b, Vector3 c, int src, int rid)>>();
            var srcs = new List<Src>(); var srcOf = new Dictionary<string, int>();
            int Source(string k) { if (!srcOf.TryGetValue(k, out var i)) { srcOf[k] = i = srcs.Count; srcs.Add(new Src { Key = k }); } return i; }
            int renderers = 0, unreadable = 0, editorRead = 0, rid = 0; var unreadableNames = new SortedSet<string>();
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
            {
                if (!mr.enabled || tileRs.Contains(mr) || lodHidden.Contains(mr)) continue;
                var bb = mr.bounds; if (bb.min.y > Over + .1f || bb.max.y < -.05f) continue;
                bool near = false;
                for (int i = Mathf.FloorToInt(bb.min.x / Cell); i <= Mathf.FloorToInt(bb.max.x / Cell) && !near; i++)
                    for (int j = Mathf.FloorToInt(bb.min.z / Cell); j <= Mathf.FloorToInt(bb.max.z / Cell) && !near; j++) near = cells.Contains(new Vector2Int(i, j));
                if (!near) continue;
                var mesh = mr.GetComponent<MeshFilter>()?.sharedMesh; if (!mesh) continue;
                renderers++; rid++;
                if (!mesh.isReadable) editorRead++;
                Vector3[] vs; int[][] indices;
                try
                {
                    using var data = UnityEditor.MeshUtility.AcquireReadOnlyMeshData(mesh);
                    var md = data[0];
                    using var verts = new Unity.Collections.NativeArray<Vector3>(md.vertexCount, Unity.Collections.Allocator.Temp);
                    md.GetVertices(verts); vs = verts.ToArray();
                    indices = new int[md.subMeshCount][];
                    for (int sub = 0; sub < md.subMeshCount; sub++)
                    {
                        var desc = md.GetSubMesh(sub);
                        if (desc.topology != MeshTopology.Triangles) { indices[sub] = new int[0]; continue; }
                        using var ix = new Unity.Collections.NativeArray<int>(desc.indexCount, Unity.Collections.Allocator.Temp);
                        md.GetIndices(ix, sub); indices[sub] = ix.ToArray();
                    }
                }
                catch (System.Exception ex)
                {
                    unreadable++; unreadableNames.Add(Name(mr.transform) + " : " + ex.Message);
                    continue; // coverage warning only; bounds are never counted as real faces
                }
                var m = mr.transform.localToWorldMatrix; var mats = mr.sharedMaterials;
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    if (mesh.GetSubMesh(s).topology != MeshTopology.Triangles) continue;
                    int k = Source(Name(mr.transform) + " [" + (s < mats.Length && mats[s] ? mats[s].name : "?") + "]");
                    var tri = indices[s];
                    for (int t = 0; t < tri.Length; t += 3)
                    {
                        Vector3 a = m.MultiplyPoint3x4(vs[tri[t]]), b = m.MultiplyPoint3x4(vs[tri[t + 1]]), c = m.MultiplyPoint3x4(vs[tri[t + 2]]);
                        float y0 = Mathf.Min(a.y, Mathf.Min(b.y, c.y)), y1 = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
                        if (y1 < -.05f || y0 > Over + .05f) continue;
                        var nrm = Vector3.Cross(b - a, c - a); if (nrm.sqrMagnitude < 1e-12f || Mathf.Abs(nrm.normalized.y) < .95f) continue;
                        Add(faces, cells, a, b, c, k, rid);
                    }
                }
            }

            // classify
            int onBed = 0, gaps = 0, level = 0, over = 0, levelNoBed = 0, expectedTramOccupancy = 0, groundOver = 0;
            var offBedAt = new List<object>();
            var byTrack = new SortedDictionary<string, int[]>();  // samples, on bed, level, over, level no bed
            foreach (var (track, q, plan) in samples)
            {
                if (!byTrack.TryGetValue(track, out var tc)) byTrack[track] = tc = new int[5];
                tc[0]++;
                var hs = new List<(float h, int src, int rid)>();
                if (faces.TryGetValue(Key(q), out var fl))
                    foreach (var f in fl) if (Height(f.a, f.b, f.c, q, out var h)) hs.Add((h, f.src, f.rid));
                var top = TileTop(q);
                if (top is float tt)
                {
                    onBed++; tc[1]++;
                    bool lv = false, ov = false, occupancy = false, ground = false;
                    foreach (var (h, src, _) in hs)
                    {
                        if (Mathf.Abs(h - tt) < Level) { lv = true; Hit(srcs[src], plan, 0); }
                        else if (h >= tt + Level && h <= tt + Over) { ov = true; Hit(srcs[src], plan, 1);
                            // Keep every hit in raw over/source rows. Record the known parked Tram's
                            // actual Iron underside separately from ground defects, never as paving.
                            if(srcs[src].Key=="Network (our routes, old pieces) / Tram [DK_Iron]")occupancy=true;else ground=true;
                        }
                    }
                    if (lv) { level++; tc[2]++; }
                    if (ov) { over++; tc[3]++; }
                    if(occupancy)expectedTramOccupancy++;if(ground)groundOver++;
                }
                else
                {
                    gaps++; offBedAt.Add(new object[] { track, plan.x, plan.y });
                    if (hs.Count < 2) continue;
                    var hi = hs.Where(z => z.h <= Over).OrderByDescending(z => z.h).ToList(); if (hi.Count < 2) continue;
                    var twins = hi.Where(z => z.rid != hi[0].rid && Mathf.Abs(z.h - hi[0].h) < Level).ToList();
                    if (twins.Count == 0) continue;
                    levelNoBed++; tc[4]++;
                    Hit(srcs[hi[0].src], plan, 2); foreach (var z in twins) Hit(srcs[z.src], plan, 2);
                }
            }

            object Top(System.Func<Src, int> by) => srcs.Where(s => by(s) > 0).OrderByDescending(by).Take(60)
                .Select(s => (object)new Dictionary<string, object> { ["source"] = s.Key, ["samples"] = by(s), ["at"] = s.At.Take(6).Select(a => (object)new[] { a[0], a[1] }).ToList() }).ToList();
            var doc = new Dictionary<string, object>
            {
                ["schema"] = "latch.tram-bed-audit/3",
                ["scene"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                ["export"] = w.Sha256,
                ["rule"] = "every 1 m along each tram track of ours (both lines of a double track), at the centre and 0.6 m either side; on our bed = a tile of ours covers the sample; level = another face within 0.8 mm of the tile top; over = another face 0.8 mm to 25 cm over it; level, no bed = off our bed, two renderers' faces within 0.8 mm at the top",
                ["ourTiles"] = tiles.Count, ["footprintContract"]="actual transformed source domain before; shared lossless source polygon after; no fixed length or scale assumption",
                ["samples"] = samples.Count, ["onBed"] = onBed, ["offBed"] = gaps, ["level"] = level, ["over"] = over, ["levelNoBed"] = levelNoBed, ["expectedTramUndersideSampleEntries"]=expectedTramOccupancy,["groundOverSampleEntries"]=groundOver,
                ["renderersRead"] = renderers - unreadable, ["editorReadDespiteRuntimeFlag"] = editorRead, ["proxyPolicy"] = "no bounds proxies counted as faces; failed mesh reads remain coverage warnings", ["offBedAt"] = offBedAt, ["unreadable"] = unreadable, ["unreadableNames"] = unreadableNames.Take(40).Cast<object>().ToList(),
                ["byTrack"] = byTrack.ToDictionary(k => k.Key, k => (object)new Dictionary<string, object> { ["samples"] = k.Value[0], ["onBed"] = k.Value[1], ["level"] = k.Value[2], ["over"] = k.Value[3], ["levelNoBed"] = k.Value[4] }),
                ["levelBySource"] = Top(s => s.Level), ["overBySource"] = Top(s => s.Over), ["levelNoBedBySource"] = Top(s => s.LevelNoBed),
            };
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, SwapExport.Json(doc));
            rep.Log($"tram bed audit: {samples.Count} samples on {w.Transit.Tram.Count} tracks, {onBed} on our bed ({tiles.Count} tiles), {gaps} off it; level with our bed {level}, over it {over}, level off the bed {levelNoBed} -> {path}");
        }

        static Vector2Int Key(Vector2 q) => new(Mathf.FloorToInt(q.x / Cell), Mathf.FloorToInt(q.y / Cell));

        static void Add(Dictionary<Vector2Int, List<(Vector3, Vector3, Vector3, int, int)>> faces, HashSet<Vector2Int> cells, Vector3 a, Vector3 b, Vector3 c, int src, int rid)
        {
            float x0 = Mathf.Min(a.x, Mathf.Min(b.x, c.x)), x1 = Mathf.Max(a.x, Mathf.Max(b.x, c.x)), z0 = Mathf.Min(a.z, Mathf.Min(b.z, c.z)), z1 = Mathf.Max(a.z, Mathf.Max(b.z, c.z));
            int i0 = Mathf.FloorToInt(x0 / Cell), i1 = Mathf.FloorToInt(x1 / Cell), j0 = Mathf.FloorToInt(z0 / Cell), j1 = Mathf.FloorToInt(z1 / Cell);
            if ((long)(i1 - i0 + 1) * (j1 - j0 + 1) > cells.Count)
            { foreach (var k in cells) if (k.x >= i0 && k.x <= i1 && k.y >= j0 && k.y <= j1) Put(faces, k, a, b, c, src, rid); return; }
            for (int i = i0; i <= i1; i++) for (int j = j0; j <= j1; j++) { var k = new Vector2Int(i, j); if (cells.Contains(k)) Put(faces, k, a, b, c, src, rid); }
        }
        static void Put(Dictionary<Vector2Int, List<(Vector3, Vector3, Vector3, int, int)>> faces, Vector2Int k, Vector3 a, Vector3 b, Vector3 c, int src, int rid)
        { if (!faces.TryGetValue(k, out var l)) faces[k] = l = new(); l.Add((a, b, c, src, rid)); }

        /// <summary>The face's height over q (XZ barycentric), if q falls in it.</summary>
        static bool Height(Vector3 a, Vector3 b, Vector3 c, Vector2 q, out float h)
        {
            h = 0f;
            float d = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z); if (Mathf.Abs(d) < 1e-9f) return false;
            float l1 = ((b.z - c.z) * (q.x - c.x) + (c.x - b.x) * (q.y - c.z)) / d, l2 = ((c.z - a.z) * (q.x - c.x) + (a.x - c.x) * (q.y - c.z)) / d, l3 = 1f - l1 - l2;
            const float e = -1e-5f; if (l1 < e || l2 < e || l3 < e) return false;
            h = l1 * a.y + l2 * b.y + l3 * c.y; return true;
        }

        static void Hit(Src s, Vector2 plan, int kind)
        {
            if (kind == 0) s.Level++; else if (kind == 1) s.Over++; else s.LevelNoBed++;
            if (s.At.Count < 6) s.At.Add(new[] { Mathf.Round(plan.x * 10f) / 10f, Mathf.Round(plan.y * 10f) / 10f });
        }

        /// <summary>Top-level root / object name, numbers folded ("Tiles 3 -2" -> "Tiles", "DK_Tile_TramRail #4.1" -> "Tile_TramRail").</summary>
        static string Name(Transform t)
        {
            var root = t; while (root.parent) root = root.parent;
            var piece = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject);
            string n = piece ? OldTownSwap.PieceOf(piece.name) : t.name;
            n = System.Text.RegularExpressions.Regex.Replace(n, @"[\s_]-?\d+(\.\d+)?(?=(\s|$))", "").Trim();
            return root == t ? n : $"{root.name} / {n}";
        }
    }
}
#endif
