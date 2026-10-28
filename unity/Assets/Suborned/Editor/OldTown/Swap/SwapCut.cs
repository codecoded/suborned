#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Suborned.OldTown.Editor.DecoKit;
using Suborned.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 075 swap: where an old structure clashes, the old one gives way (Aster's step 3), each removal logged by reason and piece.
    /// Holes, old frame: the Handover zone (everything old in it goes; flat ground is cut exactly at its edge), every solid of ours that was
    /// kept (zone buildings reaching past the edge, stations, banks, lifts, stairs, mezzanines, the liner), the sky track's beam corridor
    /// (2.2 m either side of a beam, from 2.6 m under its soffit to its top), the pylon plinths (r 3 m), the tram corridor (2.1 m either side
    /// of each rail piece, up to 4.5 m). Old units: each outermost prefab instance whole (a 6 m bay, a palm, a car); generated meshes (merged
    /// slabs, roof decks, city mass chunks, crescent wedges) by connected part, so one box of a chunk goes, not the chunk. Flagships (the old
    /// Landmarks, Skyport, Highrise and skyline towers, CornerTower_12, the SpaceHQ) never give way to the network: a clash is logged and they
    /// stand (Sky: "Tall flagship buildings should remain").</summary>
    internal static class SwapCut
    {
        static readonly HashSet<string> FlagCats = new() { "Landmarks", "Skyport", "Highrise" };
        internal static readonly HashSet<string> FlagPieces = new() { "CornerTower_12", "Tower_Setback", "Tower_Stepped", "Tower_Saucer", "SpaceHQ" };

        public static void Run(OldTownSwap.Report rep, WorldDto w, SwapOurs ours, SwapNetwork net, string hq)
        {
            var zone = OldTownSwap.ZoneOld;
            var holes = net.Holes;
            foreach (var s in ours.Solids) holes.Rect("ours", s.id, s.box, .2f, s.y0, s.y1);
            var cats = DecoKitData.Assets().assets.GroupBy(a => a.name).ToDictionary(g => g.Key, g => g.First().category);
            var skip = new HashSet<Transform> { ours.Root, net.Root };
            bool Skipped(Transform t) { for (var x = t; x; x = x.parent) if (skip.Contains(x)) return true; return false; }
            int units = 0, gone = 0, partsGone = 0, partsClipped = 0, flagClash = 0;
            var clashKept = new List<string>();

            // prefab instances, whole
            var roots = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) && !Skipped(t)).ToList();
            var inPrefab = new HashSet<Renderer>();
            foreach (var t in roots)
            {
                var rs = t.GetComponentsInChildren<Renderer>(true).Where(r => r is MeshRenderer || r is SkinnedMeshRenderer).ToArray();
                foreach (var r in rs) inPrefab.Add(r);
                if (rs.Length == 0) continue;
                units++;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                var piece = OldTownSwap.PieceOf(t.name);
                var box = SwapMesh.XZ(b); bool flat = b.size.y < .45f;
                bool flag = FlagPieces.Contains(piece) || cats.TryGetValue(piece, out var cat) && FlagCats.Contains(cat);
                string why = null, by = null;
                if (flat ? SwapMesh.Inside(zone, new Vector2(b.center.x, b.center.z)) : SwapMesh.Overlaps(box, zone)) { why = "Handover zone"; by = "zone"; }
                else if (!flat)
                {
                    var h = holes.Hit(box, b.min.y, b.max.y);
                    if (h != null) { why = Reason(h.Kind); by = h.Id; }
                }
                if (why == null) continue;
                if (piece == "SpaceHQ" || flag && why != "Handover zone") { flagClash++; clashKept.Add($"{piece} at ({b.center.x:0}, {b.center.z - OldTownSwap.DZ:0}) with {by}"); continue; }
                rep.Gave(why, piece, b.center, by);
                UnityEngine.Object.DestroyImmediate(t.gameObject); gone++;
            }

            // small loose renderers (the glTF cars and people DecoKitCitizens instantiates, primitives), whole
            int small = 0;
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(r => (r is MeshRenderer || r is SkinnedMeshRenderer) && !inPrefab.Contains(r) && !Skipped(r.transform)).ToArray())
            {
                var b = r.bounds; if (Mathf.Max(b.size.x, b.size.z) > 12f) continue;
                inPrefab.Add(r);
                string why = null, by = null; var box = SwapMesh.XZ(b);
                if (SwapMesh.Overlaps(box, zone)) { why = "Handover zone"; by = "zone"; }
                else if (b.size.y >= .45f) { var h = holes.Hit(box, b.min.y, b.max.y); if (h != null) { why = Reason(h.Kind); by = h.Id; } }
                if (why == null) continue;
                rep.Gave(why, "loose:" + (r.transform.parent ? r.transform.parent.name : r.name), b.center, by);
                if (r.transform.childCount == 0) UnityEngine.Object.DestroyImmediate(r.gameObject); else r.enabled = false;
                small++;
            }
            rep.Log($"cut: {small} small loose renderers (people, cars, primitives) gave way");

            // generated meshes, by connected part
            var gen = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(mf => mf && mf.sharedMesh && !Skipped(mf.transform) && mf.GetComponent<MeshRenderer>() is { } mr && mr.enabled && !inPrefab.Contains(mr)).ToList();
            var outside = SwapMesh.Outside(zone);
            foreach (var mf in gen)
            {
                var r = mf.GetComponent<MeshRenderer>(); var b0 = r.bounds;
                bool nearZone = SwapMesh.Overlaps(SwapMesh.XZ(b0), zone, 0f);
                bool nearHole = holes.Near(SwapMesh.XZ(b0)).Any();
                if (!nearZone && !nearHole) continue;
                var mesh = mf.sharedMesh;
                if (!mesh.isReadable) { rep.Log($"cut: {mf.name} mesh not readable; left whole"); continue; }
                var rd = new SwapMesh.Read(mesh, mf.transform.localToWorldMatrix);
                var ids = rd.Components(out int count);
                var cb = new Bounds[count]; var has = new bool[count];
                for (int s = 0; s < rd.Subs.Count; s++)
                    for (int t = 0; t < ids[s].Length; t++)
                    {
                        int k = ids[s][t];
                        for (int j = 0; j < 3; j++) { var p = rd.Verts[rd.Subs[s][3 * t + j]].W; if (!has[k]) { cb[k] = new Bounds(p, Vector3.zero); has[k] = true; } else cb[k].Encapsulate(p); }
                    }
                var act = new int[count];  // 0 keep, 1 drop, 2 clip to outside the zone
                bool any = false;
                for (int k = 0; k < count; k++)
                {
                    var b = cb[k]; var box = SwapMesh.XZ(b); bool flat = b.size.y < .45f;
                    if (SwapMesh.Overlaps(box, zone, 0f))
                    {
                        bool inside = box.xMin >= zone.xMin && box.xMax <= zone.xMax && box.yMin >= zone.yMin && box.yMax <= zone.yMax;
                        act[k] = flat && !inside ? 2 : 1;
                        if (act[k] == 1) rep.Gave("Handover zone", Part(mf), b.center, "zone");
                        any = true; continue;
                    }
                    if (flat) continue;
                    var h = holes.Hit(box, b.min.y, b.max.y);
                    if (h != null) { act[k] = 1; any = true; rep.Gave(Reason(h.Kind), Part(mf), b.center, h.Id); }
                }
                if (!any) continue;
                var wr = new SwapMesh.Write(rd);
                for (int s = 0; s < rd.Subs.Count; s++)
                    for (int t = 0; t < ids[s].Length; t++)
                    {
                        int a = rd.Subs[s][3 * t], b = rd.Subs[s][3 * t + 1], c = rd.Subs[s][3 * t + 2], k = ids[s][t];
                        if (act[k] == 0) wr.Keep(s, a, b, c);
                        else if (act[k] == 2) SwapMesh.ClipInto(wr, s, rd.Verts[a], rd.Verts[b], rd.Verts[c], outside);
                    }
                partsGone += act.Count(x => x == 1); partsClipped += act.Count(x => x == 2);
                if (wr.Triangles == 0) { UnityEngine.Object.DestroyImmediate(mf.gameObject); continue; }
                var m = wr.Mesh(mesh.name + " (swap cut)");
                mf.sharedMesh = m;
                var col = mf.GetComponent<MeshCollider>(); if (col) col.sharedMesh = m;
            }
            rep.Log($"old gave way: {gone} of {units} old pieces, {partsGone} parts of generated meshes (merged slabs, roofs, city mass), {partsClipped} flat parts cut exactly at the zone edge; " +
                    $"flagships that clash and stand: {flagClash}{(clashKept.Count > 0 ? " (" + string.Join("; ", clashKept.Take(20)) + ")" : "")}");
            foreach (var (reason, d) in rep.GaveWay.OrderBy(k => k.Key))
                rep.Log($"  gave way, {reason}: {d.Values.Sum()} ({string.Join(", ", d.OrderByDescending(k => k.Value).Take(14).Select(k => $"{k.Key} {k.Value}"))})");
        }

        static string Part(MeshFilter mf) => "mesh:" + (mf.transform.parent ? mf.transform.parent.name + "/" : "") + mf.name;

        static string Reason(string kind) => kind switch
        {
            "beam" => "sky track beam corridor",
            "pylon" => "sky track pylon",
            "tram" => "tram corridor (our route)",
            "ours" => "our structure (zone edge or network)",
            _ => kind,
        };
    }
}
#endif
