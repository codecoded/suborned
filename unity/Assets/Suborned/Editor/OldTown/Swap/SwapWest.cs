#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Suborned.OldTown.Editor.DecoKit;
using Suborned.World;
using UnityEngine;
using G = Suborned.OldTown.Editor.DecoKit.DecoKitGroundMesh;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 075 swap, second build (Latch; Aster 2026-10-29: "the west is bare sand with grey boxes"): the plan sheet west of the old
    /// builders' ground, filled the way the old reel's own district was made, on the plan's lines.
    /// - Buildings: every plan block the old builders never built (SwapPlan.WestBlockIds) as an old DecoKitBlock on the plan's footprint (the
    ///   plan's blocks are on the old module, 16 + 6k), the old 3-storey classes (Sky's domestic rule), the old frontage pieces, no walks of its
    ///   own; a block deeper than 28 m keeps its court as a garden (lawn, palms), as the old garden squares.
    /// - Ground: ours (SwapOurs keeps our pipeline's flat ground in the west: roads, kerbs, pavements, lawns), in the old materials; the old
    ///   builders' verge stone is cut away there, and a stone base under it closes any gap.
    /// - Landscape: old palms (CoconutPalm, FanPalm, Cypress) at our tree pits and on a jittered grid in the plan's parks, clear of the roads.
    /// - The old city mass fills the built plots that have no plan block (SwapPlan.MassRule); beyond the sheet it stands as the old reel had it.
    /// Also, for plan 0.5.5's levels: the old road deck lifted from -0.002 to 0.</summary>
    internal static class SwapWest
    {
        static readonly string[] Trees = { "CoconutPalm", "FanPalm", "Cypress" };

        internal static void Build(OldTownSwap.Report rep, SwapPlan plan, WorldDto w, DecoKitPlacer placerAny)
        {
            var root = new GameObject("West (plan blocks, old builders)").transform;
            var p = new DecoKitPlacer(root);
            int n = 0, courts = 0, pieces0 = root.childCount;
            var courtLawn = new G(); var courtKerb = new G();
            foreach (var (id, r, i) in SwapPlan.WestBlockIds.Select((id, i) => (id, plan.Blocks.FirstOrDefault(b => b.id == id).r, i)))
            {
                if (r.width <= 0f) { rep.Log($"west: plan block {id} not in the plan"); continue; }
                var b = SwapPlan.WestBlock(id, r, i);
                if (b == null) { rep.Log($"west: plan block {id} ({r.width} x {r.height}) is off the old module; not built"); continue; }
                b.Frontage(p); n++;
                if (b.D > 28.5f)
                {
                    float hw = b.W * .5f - 14f, hd = b.D * .5f - 14f;
                    var court = new[] { b.At(-hw, -hd), b.At(hw, -hd), b.At(hw, hd), b.At(-hw, hd) }.Select(v => new Vector2(v.x, v.z)).ToArray();
                    courtKerb.Prism(court, 0f, L.Walk);
                    var inner = new[] { b.At(-hw + 1.2f, -hd + 1.2f), b.At(hw - 1.2f, -hd + 1.2f), b.At(hw - 1.2f, hd - 1.2f), b.At(-hw + 1.2f, hd - 1.2f) }.Select(v => new Vector2(v.x, v.z)).ToArray();
                    courtLawn.Prism(inner, L.Walk, L.Walk + .004f);
                    var rnd = new System.Random(SwapPlan.Stable(id) ^ 1951);
                    for (float u = -hw + 6f; u < hw - 4f; u += 12f)
                        for (float v = -hd + 6f; v < hd - 4f; v += 12f)
                            p.Put(Trees[rnd.Next(Trees.Length)], b.At(u + (float)(rnd.NextDouble() - .5) * 4f, v + (float)(rnd.NextDouble() - .5) * 4f) + Vector3.up * (L.Walk + .004f), (float)rnd.NextDouble() * 360f);
                    courts++;
                }
            }
            courtKerb.Emit("West courts kerb", G.Mat("DK_Limestone_Tex"), true, root);
            courtLawn.Emit("West courts lawn", G.Lawn(), true, root);
            rep.Log($"west: {n} plan blocks built as old DecoKitBlocks (old frontage pieces, 3-storey classes, heights rule applies), {courts} courts as gardens");

            // palms at our tree pits in the west, and in the plan's parks there
            var roads = new SwapShapes();
            foreach (var (id, q) in plan.Corridors(_ => true, 1.5f)) roads.Add("road", id, q);
            int pits = 0, park = 0;
            foreach (var g in w.Ground.Pieces.Where(x => x.Kind == "tree-pit" && SwapPlan.InWest(x.At)))
            { p.Put(Trees[(pits++) % 2], OldTownSwap.Old3(g.At, L.Walk), g.At.x * 37f + g.At.y * 11f); }
            foreach (var pl in plan.Plots.Where(x => x.use == "park"))
            {
                var rnd = new System.Random(SwapPlan.Stable(pl.id));
                for (float x = Mathf.Floor(pl.box.xMin / 14f) * 14f + 7f; x < pl.box.xMax; x += 14f)
                    for (float y = Mathf.Floor(pl.box.yMin / 14f) * 14f + 7f; y < pl.box.yMax; y += 14f)
                    {
                        var c = new Vector2(x + (float)(rnd.NextDouble() - .5) * 6f, y + (float)(rnd.NextDouble() - .5) * 6f);
                        if (rnd.NextDouble() < .35 || !SwapPlan.InWest(c) || !SwapPlan.InPoly(pl.poly, c) || roads.Hit(c) != null) continue;
                        if (!SwapPlan.InPoly(pl.poly, c + new Vector2(3f, 3f)) || !SwapPlan.InPoly(pl.poly, c - new Vector2(3f, 3f))) continue;  // 3 m in from the park's edge
                        p.Put(Trees[rnd.Next(Trees.Length)], OldTownSwap.Old3(c, L.Walk + .004f), (float)rnd.NextDouble() * 360f); park++;
                    }
            }
            rep.Log($"west: {pits} old palms at our tree pits, {park} old palms and cypresses in the plan's parks (14 m jittered grid, 35% gaps, clear of the roads)");
            if (p.Missing.Count > 0) throw new System.InvalidOperationException($"[Swap] old prefabs missing in the west: {string.Join(", ", p.Missing)} (NP-1)");
        }

        /// <summary>The old builders' verge stone (DecoKitStreetDemo.Ground's "Street west verge" slabs) cut away in the west, where our ground lies;
        /// an old-stone base just under our ground (-0.03) closes any gap in it.</summary>
        internal static void Ground(OldTownSwap.Report rep)
        {
            var regions = SwapPlan.West.Select(r => new Rect(r.x, r.y + OldTownSwap.DZ, r.width, r.height)).ToArray();
            int cut = 0;
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(m => m.name.StartsWith("Street west verge") && m.sharedMesh).ToArray())
            {
                var rd = new SwapMesh.Read(mf.sharedMesh, mf.transform.localToWorldMatrix);
                var polys = new List<(int sub, List<SwapMesh.V> poly)>();
                for (int s = 0; s < rd.Subs.Count; s++)
                    for (int t = 0; t < rd.Subs[s].Length; t += 3)
                        polys.Add((s, new List<SwapMesh.V> { rd.Verts[rd.Subs[s][t]], rd.Verts[rd.Subs[s][t + 1]], rd.Verts[rd.Subs[s][t + 2]] }));
                foreach (var r in regions)
                {
                    var next = new List<(int, List<SwapMesh.V>)>();
                    foreach (var (s, poly) in polys)
                        foreach (var region in SwapMesh.Outside(r))
                        {
                            var q = poly;
                            foreach (var h in region) { q = SwapMesh.Clip(q, h); if (q.Count < 3) break; }
                            if (q.Count >= 3) next.Add((s, q));
                        }
                    polys = next;
                }
                var wr = new SwapMesh.Write(rd);
                foreach (var (s, poly) in polys) wr.Poly(s, poly);
                if (wr.Triangles == 0) { Object.DestroyImmediate(mf.gameObject); cut++; continue; }
                mf.sharedMesh = wr.Mesh(mf.sharedMesh.name + " (west cut)");
                var col = mf.GetComponent<MeshCollider>(); if (col) col.sharedMesh = mf.sharedMesh;
                cut++;
            }
            var stone = G.Mat("DK_Limestone_Tex");
            var basis = new G();
            foreach (var r in regions) basis.Block(r, -.06f, -.03f);
            basis.Emit("West base (old stone under our ground)", stone, false, null);
            rep.Log($"west: {cut} old verge slabs cut away over the west (x < -158; x < -124 north of the Southside); old-stone base at -0.03 under our ground there");
        }

        /// <summary>Plan 0.5.5's street level is 0 / 0.18: the old asphalt deck (-0.002) lifted to 0 (the walk is 0.18 by DecoKitStreetLayout.Walk).</summary>
        internal static void LiftRoad(OldTownSwap.Report rep)
        {
            var road = G.Road(); int n = 0;
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!mr.sharedMaterials.Contains(road)) continue;
                var b = mr.bounds; if (b.max.y > -.0005f || b.max.y < -.01f) continue;
                for (var t = mr.transform; t; t = t.parent) if (t.name.StartsWith("Ours (") || t.name.StartsWith("Network (")) goto next;
                mr.transform.position += Vector3.up * .002f; n++;
                next:;
            }
            rep.Log($"levels: {n} old asphalt decks lifted from -0.002 to 0 (plan 0.5.5's street level); old flags and kerbs scaled to the 0.18 walk (DecoKitPlacer.GroundScaleY {DecoKitPlacer.GroundScaleY:0.###})");
            LiftRoadModules(rep);
        }

        /// <summary>Brief 075 tram-bed fix (edit E11): the old road-level modules (Tile_TramRail and its kin, decokit ground.py: "top z = 0, body
        /// to -0.15") were modelled to top out at the road surface over a deck at -0.002. With the deck lifted to 0 their top faces were coplanar
        /// with the asphalt and z-fought (the Boulevard's "shattered white fragments": the old track's ivory and grey slabs flickering through the
        /// deck). Rule, by shape, never by id: every old prefab instance whose top lies at the deck (|top| under 1 mm) and whose body is sunk in
        /// the road (bottom under -2 cm) rides 3 mm over the deck, the height our tram tiles are laid at (SwapNetwork.Rails).</summary>
        internal const float RoadModuleLift = .003f;
        static void LiftRoadModules(OldTownSwap.Report rep)
        {
            var count = new Dictionary<string, int>();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (!UnityEditor.PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)) continue;
                bool ours = false;
                for (var x = t; x; x = x.parent) if (x.name.StartsWith("Ours (") || x.name.StartsWith("Network (")) { ours = true; break; }
                if (ours) continue;
                var rs = t.GetComponentsInChildren<MeshRenderer>(true);
                if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                if (Mathf.Abs(b.max.y) >= .001f || b.min.y > -.02f) continue;
                t.position += Vector3.up * RoadModuleLift;
                var piece = OldTownSwap.PieceOf(t.name);
                count[piece] = count.TryGetValue(piece, out var c) ? c + 1 : 1;
            }
            rep.Log($"levels: {count.Values.Sum()} old road-level modules (top at the road surface, body sunk) lifted {RoadModuleLift * 1000f:0} mm clear of the deck at 0 ({string.Join(", ", count.OrderByDescending(k => k.Value).Select(k => $"{k.Key} {k.Value}"))})");
        }
    }
}
#endif
