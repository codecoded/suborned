#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 075, plan 0.5.6 (Datum's match-swap2 decisions, classes "handover-street" and "megastructure": old gives way). Beyond the
    /// Handover zone, the ground the plan keeps as ours: the Handover's kept plots outside the zone (KEEP_PLOTS of tools/plan/oldtown/transcribe.ts
    /// less the two inside the zone and lamplight-west, which carries a plan block the old builders build: SwapWest) and the megastructure
    /// footprints (Lantern Terraces, West Terraces). There our pipeline's ground and masses are kept as in the zone (SwapOurs), and the old
    /// gives way as in the zone (SwapCut): flat pieces by their centre, standing pieces by their centre (flagships stand), generated flat ground
    /// cut exactly at the edge, generated parts by centre. Set for one build by OldTownSwap; empty otherwise.</summary>
    internal static class SwapKeep
    {
        internal static readonly string[] Plots = { "garden-row-east", "pylon-row-east", "orbit-gardens", "rocket-west", "lamplight-south-w", "lamplight-south-e" };
        /// <summary>Plan frame.</summary>
        internal static readonly List<(string id, Rect r)> Areas = new();

        internal static void Set(SwapPlan plan, OldTownSwap.Report rep)
        {
            Areas.Clear();
            foreach (var id in Plots) if (plan.Plot(id) is { } p) Areas.Add(("plot:" + id, p.box)); else rep.Log($"keep: plan plot {id} not in the plan");
            foreach (var (id, r) in plan.Megas) Areas.Add(("mega:" + id, r));
            rep.Log($"keep (Datum's classes handover-street, megastructure): ours kept and the old gives way, as in the zone, on {string.Join(", ", Areas.Select(a => $"{a.id} [{a.r.xMin:0}, {a.r.xMax:0}, {a.r.yMin:0}, {a.r.yMax:0}]"))}");
        }
        internal static void Clear() => Areas.Clear();

        /// <summary>The keep area whose rect holds the plan point (never the zone itself).</summary>
        internal static string At(Vector2 plan)
        {
            if (SwapMesh.Inside(OldTownSwap.ZonePlan, plan)) return null;
            foreach (var (id, r) in Areas) if (SwapMesh.Inside(r, plan)) return id;
            return null;
        }
        internal static string AtOld(Vector2 old) => At(new Vector2(old.x, old.y - OldTownSwap.DZ));
        internal static bool Near(Rect planBox) => Areas.Any(a => SwapMesh.Overlaps(a.r, planBox, 0f));
        internal static IEnumerable<Rect> OldRects() => Areas.Select(a => new Rect(a.r.x, a.r.y + OldTownSwap.DZ, a.r.width, a.r.height));

        /// <summary>The keep areas less the zone and the west (where our ground is kept already), as disjoint convex regions, plan frame.</summary>
        internal static SwapMesh.Half[][] Regions()
        {
            var o = new List<SwapMesh.Half[]>();
            var w = SwapPlan.West;
            foreach (var (_, r) in Areas)
                foreach (var oz in SwapMesh.Outside(OldTownSwap.ZonePlan))
                    foreach (var o1 in SwapMesh.Outside(w[0]))
                        foreach (var o2 in SwapMesh.Outside(w[1]))
                            o.Add(SwapMesh.Inside(r)[0].Concat(oz).Concat(o1).Concat(o2).ToArray());
            return o.ToArray();
        }

        /// <summary>A polygon clipped to outside every rect (each "outside" a set of disjoint convex regions), as pieces.</summary>
        internal static List<List<SwapMesh.V>> OutsideAll(List<SwapMesh.V> poly, IEnumerable<Rect> rects)
        {
            var pieces = new List<List<SwapMesh.V>> { poly };
            foreach (var r in rects)
            {
                var next = new List<List<SwapMesh.V>>();
                foreach (var q in pieces)
                {
                    float x0 = q.Min(v => v.W.x), x1 = q.Max(v => v.W.x), z0 = q.Min(v => v.W.z), z1 = q.Max(v => v.W.z);
                    if (x1 <= r.xMin || x0 >= r.xMax || z1 <= r.yMin || z0 >= r.yMax) { next.Add(q); continue; }
                    foreach (var reg in SwapMesh.Outside(r))
                    {
                        var c = q; foreach (var h in reg) { c = SwapMesh.Clip(c, h); if (c.Count < 3) break; }
                        if (c.Count >= 3) next.Add(c);
                    }
                }
                pieces = next;
            }
            return pieces;
        }
    }
}
#endif
