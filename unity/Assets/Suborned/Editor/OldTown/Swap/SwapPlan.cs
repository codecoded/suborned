#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Suborned.OldTown.Editor.DecoKit;
using Suborned.World;
using UnityEngine;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 075 swap, second build (Latch, 2026-10-29): what the swap reads from the plan itself (contracts/world/harbour-district.plan.json,
    /// 0.5.5): nodes, roads (path and kerb-to-kerb width), plots (use, rect or polygon) and blocks, all in the plan frame. From them:
    /// - road corridors (one oriented quad per path segment, kerb to kerb, the ends carried half a width on so joints close);
    /// - the Handover streets (outside the zone the old gives way to them, Datum's "Swap: patch applied" item 4);
    /// - the west (the plan sheet west of the old builders' ground: x -460..-158, and x -158..-124 north of the Southside), where the ground is
    ///   ours (our pipeline's roads, kerbs, pavements, lawns, in old materials) and the buildings are the old builders' on the plan's blocks;
    /// - the old Southside blocks moved onto their plan blocks (Datum's table), SS5 and W1 standing down;
    /// - the city mass rule: off the plan's roads, parks and zone; in the west it fills only built plots that have no plan block, at 3 storeys.</summary>
    internal sealed class SwapPlan
    {
        public string Path, Sha256, Version, SpaceHqForm;
        public readonly Dictionary<string, Vector2> Nodes = new();
        public readonly List<(string id, float w, Vector2[] pts)> Roads = new();
        public readonly List<(string id, string use, Vector2[] poly, Rect box)> Plots = new();
        public readonly List<(string id, Rect r)> Blocks = new();
        /// <summary>Roads that carry a tram line (the plan's road `tram` field): E12's tram streets.</summary>
        public readonly HashSet<string> TramRoads = new();
        /// <summary>Megastructure footprints (plan `megastructures[].footprint`, [x0, x1, y0, y1] rects).</summary>
        public readonly List<(string id, Rect r)> Megas = new();

        /// <summary>The plan sheet (plan frame), CAD sheet extent (tools/plan).</summary>
        internal static readonly Rect Sheet = Rect.MinMaxRect(-460f, -560f, 380f, 300f);
        /// <summary>The west: where the old builders laid no ground (their district ends at the garden terrace's back, x -124; the Southside
        /// at x -158). Two disjoint rects, plan frame.</summary>
        internal static readonly Rect[] West = { Rect.MinMaxRect(-460f, -560f, -158f, 300f), Rect.MinMaxRect(-158f, -246f, -124f, 300f) };
        internal static readonly HashSet<string> HandoverStreets = new()
        { "market-street", "satellite-street", "lantern-lane", "lamplight-lane", "rocket-lane", "starline-street", "garden-row", "orbit-walk", "pylon-row", "apron-road" };
        /// <summary>Plan blocks the old builders never built (the old west was city mass): built as old DecoKitBlocks on the plan's footprint.</summary>
        internal static readonly string[] WestBlockIds = { "lamplight-west", "lantern-west", "lantern-south", "west-row-w1", "west-row-e1", "west-row-w2", "west-row-e2",
            "west-row-w3", "west-row-e3", "west-row-w4", "west-row-e4", "west-row-w5", "west-row-e5" };
        internal const float Domestic = 16.8f;  // 3 storeys (brief 066 height cap, Sky's domestic rule)

        static string Repo => System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", ".."));

        public static SwapPlan Load(string path)
        {
            var p = new SwapPlan { Path = path };
            var bytes = File.ReadAllBytes(path);
            using (var sha = System.Security.Cryptography.SHA256.Create()) p.Sha256 = string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2")));
            var root = Json.Parse(System.Text.Encoding.UTF8.GetString(bytes));
            p.Version = J.S(root, "version");
            p.SpaceHqForm = J.Get(root, "spacehq") is { } hq ? J.S(hq, "form") : null;
            foreach (var (k, v) in J.O(J.Get(root, "nodes"))) p.Nodes[k] = J.P(v);
            foreach (var r in J.A(root, "roads"))
            {
                var path0 = J.A(r, "path").Select(n => p.Nodes[(string)n]).ToArray();
                p.Roads.Add((J.S(r, "id"), J.F(r, "w"), path0));
                if (J.Get(r, "tram") != null) p.TramRoads.Add(J.S(r, "id"));
            }
            foreach (var pl in J.A(root, "plots"))
            {
                Vector2[] poly = null;
                if (J.Get(pl, "rect") is List<object> rc && rc.Count == 4) { var a = rc.Select(x => (float)(double)x).ToArray(); poly = Ring(Rect.MinMaxRect(a[0], a[2], a[1], a[3])); }
                else if (J.Get(pl, "poly") is List<object>) poly = J.Poly(pl, "poly");
                if (poly == null) continue;
                p.Plots.Add((J.S(pl, "id"), J.S(pl, "use") ?? J.S(pl, "kind"), poly, Box(poly)));
            }
            if (J.Get(root, "megastructures") is List<object> megas)
                foreach (var m in megas)
                    if (J.Get(m, "footprint") is List<object> fps)
                        foreach (var fp in fps) if (fp is List<object> rc && rc.Count == 4) { var a = rc.Select(x => (float)(double)x).ToArray(); p.Megas.Add((J.S(m, "id"), Rect.MinMaxRect(a[0], a[2], a[1], a[3]))); }
            foreach (var b in J.A(root, "blocks"))
                if (J.Get(b, "rect") is List<object> rc && rc.Count == 4) { var a = rc.Select(x => (float)(double)x).ToArray(); p.Blocks.Add((J.S(b, "id"), Rect.MinMaxRect(a[0], a[2], a[1], a[3]))); }
            return p;
        }

        internal static Vector2[] Ring(Rect r) => new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) };
        internal static Rect Box(IEnumerable<Vector2> pts)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var q in pts) { x0 = Mathf.Min(x0, q.x); y0 = Mathf.Min(y0, q.y); x1 = Mathf.Max(x1, q.x); y1 = Mathf.Max(y1, q.y); }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }
        internal static bool InPoly(Vector2[] poly, Vector2 q)
        {
            bool c = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > q.y) != (poly[j].y > q.y) && q.x < (poly[j].x - poly[i].x) * (q.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x) c = !c;
            return c;
        }
        /// <summary>FNV-1a, so a seed by id is the same in every process (string.GetHashCode need not be).</summary>
        internal static int Stable(string s) { unchecked { uint h = 2166136261; foreach (var ch in s) { h ^= ch; h *= 16777619; } return (int)(h & 0x7fffffff); } }
        internal static bool InWest(Vector2 plan) => West.Any(r => SwapMesh.Inside(r, plan));

        /// <summary>Corridor quads (plan frame) of the roads chosen, half width w/2 + grow, each segment carried hw past its ends.</summary>
        public IEnumerable<(string id, Vector2[] quad)> Corridors(Func<string, bool> which, float grow = 0f)
        {
            foreach (var (id, w, pts) in Roads)
            {
                if (!which(id) || id == "crown-circus") continue;  // the circus is a ring of its own (DecoKitCircus)
                float hw = w * .5f + grow;
                for (int i = 1; i < pts.Length; i++)
                {
                    Vector2 a = pts[i - 1], b = pts[i]; if ((b - a).sqrMagnitude < 1e-4f) continue;
                    var d = (b - a).normalized; var n = new Vector2(-d.y, d.x) * hw;
                    a -= d * hw; b += d * hw;
                    yield return ($"{id}#{i}", new[] { a + n, b + n, b - n, a - n });
                }
            }
        }

        public (string id, string use, Vector2[] poly, Rect box)? Plot(string id) => Plots.Where(x => x.id == id).Select(x => ((string, string, Vector2[], Rect)?)x).FirstOrDefault();

        // ------------------------------------------------------------------ the old Southside on plan 0.5.5 (Datum's table)
        static (string, int)[] S(params (string, int)[] s) => s;

        /// <summary>The old Southside blocks moved onto their plan blocks (old frame: z = plan y + 206). Each keeps its old classes; the walks run to
        /// the plan's plot edges (rounded to the 2 m module), so the old kerbs stand on the plan's kerb lines: the boulevard's at x +-10 (walks 8),
        /// Apollo Way's at y -466 / -478 (SS2's tail and SS4's head walks 6), Market Street's at y -419 (SS1's head walk 8). N1 is the plan's 28 m
        /// block (old 64 m deep, its west half on Satellite Street); SS2 is the plan's 40 m (old 46). SS5 and W1 stand down (DecoKitSouthside.Reserved).
        /// The old N4, W2, E1, RL1, RL2 and RB1 keep their places (the cut gives way where they meet a Handover street or orbit-gardens).</summary>
        internal static DecoKitBlock[] Southside(DecoKitBlock[] old, OldTownSwap.Report rep)
        {
            var keep = old.Where(b => b.Name is "N4" or "W2" or "E1" or "RL1" or "RL2" or "RB1" or "SS5" or "W1").ToList();
            var moved = new[]
            {
                new DecoKitBlock("N1", -32f, -76f, 0f, 40f, S(("S4", 4)), S(("S4", 4)), walkR: 8f, walkL: 8f, walkH: 4f, walkT: 4f),
                new DecoKitBlock("N2", 32f, -78.5f, 0f, 40f, S(("S4", 4)), S(("S4", 1), ("P4", 2), ("S4", 1)), walkR: 0f, walkL: 8f, walkH: 6f, walkT: 2f),
                new DecoKitBlock("SS1", -32f, -244f, 0f, 46f, S(("S3", 1), ("B3", 3), ("S3", 1)), S(("S3", 5)), walkR: 8f, walkL: 8f, walkH: 8f, walkT: 4f),
                new DecoKitBlock("SS3", -32f, -302f, 0f, 34f, S(("S3", 1), ("P3", 1), ("S3", 1)), S(("S3", 3)), walkR: 8f, walkL: 8f, walkH: 4f, walkT: 4f),
                new DecoKitBlock("SS2", 32f, -233.5f, 0f, 40f, S(("S4", 4)), S(("S4", 1), ("P4", 2), ("S4", 1)), walkR: 2f, walkL: 8f, walkH: 0f, walkT: 6f),
                new DecoKitBlock("SS4", 32f, -295.5f, 0f, 34f, S(("S4", 3)), S(("S4", 1), ("P4", 1), ("S4", 1)), walkR: 2f, walkL: 8f, walkH: 6f, walkT: 4f),
            };
            foreach (var m in moved)
            {
                var o = old.FirstOrDefault(b => b.Name == m.Name);
                rep.Log($"southside: {m.Name} {(o != null ? $"from ({o.C.x:0.#}, {o.C.y - OldTownSwap.DZ:0.#}) W {o.W} D {o.D}" : "new")} to ({m.C.x:0.#}, {m.C.y - OldTownSwap.DZ:0.#}) W {m.W} D {m.D}, walks R {m.WalkR} L {m.WalkL} H {m.WalkH} T {m.WalkT} (plan frame)");
            }
            return keep.Concat(moved).ToArray();
        }

        // ------------------------------------------------------------------ the west
        /// <summary>A plan block (plan frame) as an old DecoKitBlock (old frame): the long side is the axis (W), the short the depth (D); both are on
        /// the old module (16 + 6k), so the old bays close; no walks (the pavement round it is ours, laid by our pipeline to the plan's building
        /// line). Faces alternate the old 3-storey domestic classes.</summary>
        internal static DecoKitBlock WestBlock(string id, Rect r, int seed)
        {
            bool alongX = r.width > r.height;
            float W = alongX ? r.width : r.height, D = alongX ? r.height : r.width;
            int k = Mathf.RoundToInt((W - 16f) / 6f);
            if (Mathf.Abs(8f + 6f * k + 8f - W) > .01f || Mathf.Abs((D - 16f) % 6f) > .01f) return null;
            string a = seed % 2 == 0 ? "S3" : "P3", b = seed % 3 == 0 ? "B3" : "S3";
            var right = S((a, k));
            var left = k >= 3 ? S(("S3", 1), (b, k - 2), ("S3", 1)) : S(("S3", k));
            var c = r.center;
            return new DecoKitBlock("west:" + id, c.x, c.y + OldTownSwap.DZ, alongX ? 90f : 0f, W, right, left, 0f, 0f, 0f, 0f, D, false, false);
        }

        /// <summary>The city mass rule (DecoKitCityMass.Rule, old frame in, plan frame inside). Beyond the sheet the old mass stands as it was; on the
        /// sheet it keeps off every plan road (kerb to kerb + 2 m), the zone (+ 8 m) and parks; in the west it fills only plots of a built use that
        /// have no plan block, a whole lot inside the plot, at 3 storeys, every lot (no park gaps).</summary>
        internal Func<float, float, float, (float, bool)> MassRule(OldTownSwap.Report rep)
        {
            var corridors = new SwapShapes();
            foreach (var (id, q) in Corridors(_ => true, 2f)) corridors.Add("road", id, q);
            var zone = OldTownSwap.ZonePlan; var nearZone = Rect.MinMaxRect(zone.xMin - 8f, zone.yMin - 8f, zone.xMax + 8f, zone.yMax + 8f);
            var blocked = new HashSet<string>(Blocks.Select(b => b.id));
            var fill = Plots.Where(p => (p.use is "residential" or "mixed" or "commercial") && !Blocks.Any(b => b.r.Overlaps(p.box))).ToList();
            int filled = 0, cleared = 0;
            rep.Log($"city mass: on the sheet off {corridors.All.Count} road quads, the zone and parks; west fill plots (built use, no plan block): {string.Join(", ", fill.Select(f => f.id))}");
            return (x, z, cap) =>
            {
                var c = new Vector2(x, z - OldTownSwap.DZ);
                const float half = 31f;  // the lot (27 m) with its 3 m jitter, +1
                var lot = new Rect(c.x - half, c.y - half, 2f * half, 2f * half);
                if (!lot.Overlaps(Sheet)) return (cap, false);
                if (lot.Overlaps(nearZone) || corridors.Hit(lot, 0f, 1f) != null) { cleared++; return (0f, false); }
                if (InWest(c))
                {
                    foreach (var f in fill)
                        if (f.box.xMin <= lot.xMin && f.box.xMax >= lot.xMax && f.box.yMin <= lot.yMin && f.box.yMax >= lot.yMax && InPoly(f.poly, c)) { filled++; return (Domestic, true); }
                    return (0f, false);
                }
                if (Plots.Any(p => p.use == "park" && InPoly(p.poly, c))) return (0f, false);
                return (Mathf.Min(cap, Domestic), false);
            };
        }
    }
}
#endif
