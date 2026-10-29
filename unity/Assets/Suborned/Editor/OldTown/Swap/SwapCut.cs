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

        /// <summary>Second build (plan 0.5.5, Datum's "Swap: patch applied" items 4 and 6, "Swap: network" item 3): the old also gives way to the
        /// Handover streets outside the zone (kerb to kerb + 0.5 m; flat old ground under 30 m too, so the street is street), to the
        /// orbit-gardens plot (standing pieces), and at the Gardens Gate seam (the old west terrace at x -126..-108 over y -9..50, where S1 and
        /// the gate cross).</summary>
        static SwapShapes PlanHoles(SwapPlan plan, out SwapShapes flat)
        {
            var h = new SwapShapes(); flat = new SwapShapes();
            Vector2[] Old(Vector2[] q) => q.Select(OldTownSwap.Old).ToArray();
            foreach (var (id, q) in plan.Corridors(id => SwapPlan.HandoverStreets.Contains(id), .5f)) { h.Add("street", id, Old(q)); flat.Add("street", id, Old(q)); }
            if (plan.Plot("orbit-gardens") is { } og) h.Add("plot", "orbit-gardens", Old(og.poly));
            h.Add("seam", "gardens-gate", Old(SwapPlan.Ring(Rect.MinMaxRect(-126f, -9f, -108f, 50f))));
            return h;
        }

        public static void Run(OldTownSwap.Report rep, WorldDto w, SwapOurs ours, SwapNetwork net, string hq, SwapPlan plan)
        {
            var zone = OldTownSwap.ZoneOld;
            var keepOld = SwapKeep.OldRects().ToList();
            openPlots = plan.Plots.Where(p => p.use is "open" or "square").Select(p => p.poly.Select(OldTownSwap.Old).ToArray()).ToList();
            roadsNear = new SwapShapes(); foreach (var (id, q) in plan.Corridors(_ => true, 1.5f)) roadsNear.Add("road", id, q.Select(OldTownSwap.Old).ToArray());
            var holes = net.Holes;
            foreach (var s in ours.Solids) holes.Rect("ours", s.id, s.box, .2f, s.y0, s.y1);
            var planHoles = PlanHoles(plan, out var planFlat);
            foreach (var x in planHoles.All) holes.Add(x.Kind, x.Id, x.Poly);
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
                else if (SwapKeep.AtOld(new Vector2(b.center.x, b.center.z)) is { } ka) { why = "kept plot or megastructure (Datum, 0.5.6)"; by = ka; }
                else if (flat && OpenKerb(piece, b, plan)) { why = "old kerb in an open plot, off every plan road (Datum, 0.5.6)"; by = "kerb-lines"; }
                else if (!flat)
                {
                    var h = holes.Hit(box, b.min.y, b.max.y);
                    if (h != null) { why = Reason(h.Kind); by = h.Id; }
                }
                else if (planFlat.Hit(new Vector2(b.center.x, b.center.z)) is { } fh) { why = "Handover street (flat old ground)"; by = fh.Id; }
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
                else if (SwapKeep.AtOld(new Vector2(b.center.x, b.center.z)) is { } ka) { why = "kept plot or megastructure (Datum, 0.5.6)"; by = ka; }
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
            var keepAndZone = new List<Rect> { zone }; keepAndZone.AddRange(keepOld);
            foreach (var mf in gen)
            {
                var r = mf.GetComponent<MeshRenderer>(); var b0 = r.bounds;
                bool nearZone = SwapMesh.Overlaps(SwapMesh.XZ(b0), zone, 0f);
                nearZone |= keepOld.Any(k => SwapMesh.Overlaps(SwapMesh.XZ(b0), k, 0f));
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
                    var ka = SwapKeep.AtOld(new Vector2(b.center.x, b.center.z));
                    if (!flat && ka != null) { act[k] = 1; any = true; rep.Gave("kept plot or megastructure (Datum, 0.5.6)", Part(mf), b.center, ka); continue; }
                    if (flat && keepOld.Any(r => SwapMesh.Overlaps(box, r, 0f))) { act[k] = 2; any = true; continue; }  // clipped exactly to outside the zone and the keep areas
                    if (SwapMesh.Overlaps(box, zone, 0f))
                    {
                        bool inside = box.xMin >= zone.xMin && box.xMax <= zone.xMax && box.yMin >= zone.yMin && box.yMax <= zone.yMax;
                        act[k] = flat && !inside ? 2 : 1;
                        if (act[k] == 1) rep.Gave("Handover zone", Part(mf), b.center, "zone");
                        any = true; continue;
                    }
                    if (flat)
                    {
                        if (Mathf.Max(b.size.x, b.size.z) < 30f && planFlat.Hit(new Vector2(b.center.x, b.center.z)) is { } fh) { act[k] = 1; any = true; rep.Gave("Handover street (flat old ground)", Part(mf), b.center, fh.Id); }
                        continue;
                    }
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
                        else if (act[k] == 2) foreach (var poly in SwapKeep.OutsideAll(new List<SwapMesh.V> { rd.Verts[a], rd.Verts[b], rd.Verts[c] }, keepAndZone)) wr.Poly(s, poly);
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

        static List<Vector2[]> openPlots; static SwapShapes roadsNear;
        /// <summary>Datum's kerb-lines decisions (M0346/M0347 up the forecourt, the Orbit Square cross kerbs): an old kerb stone (the Kerb_ family)
        /// whose centre lies in an open plan plot (use open or square) and more than 1.5 m from every plan road gives way: the plan lays no road
        /// there, so the old street edge has nothing to meet.</summary>
        static bool OpenKerb(string piece, Bounds b, SwapPlan plan)
        {
            if (!piece.StartsWith("Kerb_") || openPlots == null) return false;
            var c = new Vector2(b.center.x, b.center.z);
            if (!openPlots.Any(p => InPoly(p, c))) return false;
            return roadsNear.Hit(c) == null;
        }
        static bool InPoly(Vector2[] p, Vector2 c)
        {
            bool inside = false;
            for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                if ((p[i].y > c.y) != (p[j].y > c.y) && c.x < (p[j].x - p[i].x) * (c.y - p[i].y) / (p[j].y - p[i].y) + p[i].x) inside = !inside;
            return inside;
        }

        static string Part(MeshFilter mf) => "mesh:" + (mf.transform.parent ? mf.transform.parent.name + "/" : "") + mf.name;

        static string Reason(string kind) => kind switch
        {
            "beam" => "sky track beam corridor",
            "pylon" => "sky track pylon",
            "tram" => "tram corridor (our route)",
            "ours" => "our structure (zone edge or network)",
            "street" => "Handover street (plan 0.5.5)",
            "plot" => "Handover plot orbit-gardens (plan 0.5.5)",
            "seam" => "Gardens Gate seam (S1 and the gate, plan 0.5.5)",
            _ => kind,
        };
    }
}
#endif
