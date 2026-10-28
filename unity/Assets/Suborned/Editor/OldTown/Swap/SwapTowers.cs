#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Suborned.OldTown.Editor.DecoKit;
using UnityEngine;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 075 swap (Latch, Aster's decision 2026-10-29 after Vantage's sightlines.md): the old builder's own "no tower here" rule
    /// (DecoKitDecks' header: towers stand only on blocks where "from the 2.5D camera they would not bury" Orbit Square) extended to the Handover
    /// zone. A roof-programme tower (a DecoKitDecks item, kit category Highrise, Tower_*) does not stand when its view-shadow from the play camera
    /// (orthographic, pitch 38°, any of the four play yaws 30/120/210/300) falls on the zone plus a margin: the camera looks along plan
    /// (sin yaw, cos yaw), so a top h hides ground up to h / tan 38° beyond the footprint along that direction (tools/world/occlusion.ts).
    /// Sky's named flagships (SwapCut.FlagPieces) always stand. Only the tower goes: its deck keeps its programme (pools, cabanas, bandstand,
    /// skypad). Hooked in through DecoKitHighrise.Stands (edit E9), so the tower is never placed; each one logged.</summary>
    internal static class SwapTowers
    {
        internal const float Pitch = 38f, Margin = 8f, Step = 1f;
        internal static readonly float[] Yaws = { 30f, 120f, 210f, 300f };

        internal static System.Func<string, string, GameObject, bool> Rule(OldTownSwap.Report rep, List<object> log)
        {
            var cats = DecoKitData.Assets().assets.GroupBy(a => a.name).ToDictionary(g => g.Key, g => g.First().category);
            var zone = OldTownSwap.ZonePlan;
            var near = Rect.MinMaxRect(zone.xMin - Margin, zone.yMin - Margin, zone.xMax + Margin, zone.yMax + Margin);
            float t = Mathf.Tan(Pitch * Mathf.Deg2Rad);
            return (block, piece, go) =>
            {
                if (!piece.StartsWith("Tower_") || !cats.TryGetValue(piece, out var cat) || cat != "Highrise") return true;
                var rs = go.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) return true;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                var foot = Rect.MinMaxRect(b.min.x, b.min.z - OldTownSwap.DZ, b.max.x, b.max.z - OldTownSwap.DZ);  // plan frame
                float reach = b.max.y / t;
                var hits = new List<float>();
                foreach (var yaw in Yaws)
                {
                    var d = new Vector2(Mathf.Sin(yaw * Mathf.Deg2Rad), Mathf.Cos(yaw * Mathf.Deg2Rad));
                    for (float s = 0f; s <= reach; s += Step)
                    {
                        var f = foot; f.position += d * s;
                        if (f.Overlaps(near)) { hits.Add(yaw); break; }
                    }
                }
                bool flag = SwapCut.FlagPieces.Contains(piece);
                bool stands = flag || hits.Count == 0;
                float gap = Gap(foot, zone);
                log.Add(new Dictionary<string, object>
                {
                    ["block"] = block, ["piece"] = piece, ["at"] = new[] { R(b.center.x), R(b.center.z - OldTownSwap.DZ) }, ["top"] = R(b.max.y),
                    ["reach"] = R(reach), ["gapToZone"] = R(gap), ["buriesAtYaws"] = hits.ToArray(), ["flagship"] = flag, ["stands"] = stands,
                });
                rep.Log($"towers: {block} {piece} at ({b.center.x:0}, {b.center.z - OldTownSwap.DZ:0}) top {b.max.y:0.0} m, view-shadow {reach:0} m, {gap:0} m from the zone: " +
                        (hits.Count == 0 ? "clear of the zone, stands" : flag ? $"reaches the zone at yaw {string.Join("/", hits)} but a named flagship, stands" : $"buries the zone at yaw {string.Join("/", hits)}, not placed (its deck stays)"));
                if (!stands) rep.Gave("no tower where it buries the Handover (DecoKitDecks rule, extended)", piece, b.center, "towers");
                return stands;
            };
        }

        static float Gap(Rect a, Rect z)
        {
            float dx = Mathf.Max(0f, Mathf.Max(z.xMin - a.xMax, a.xMin - z.xMax)), dy = Mathf.Max(0f, Mathf.Max(z.yMin - a.yMax, a.yMin - z.yMax));
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        static float R(float v) => Mathf.Round(v * 10f) / 10f;
    }
}
#endif
