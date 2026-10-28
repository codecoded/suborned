#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using G = Suborned.OldTown.Editor.DecoKit.DecoKitGroundMesh;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Southside: the quarter at the boulevard's foot, composed rather than gridded. Every block is a podium (3 or 4
    /// floors over the shop: lowrise.py, highrise.py) whose roof is a programmed deck, and the blocks clear of the
    /// square's sightline and the lanes carry towers (DecoKitHighrise, DecoKitDecks); Constellation (N1) three:
    ///   Sunset Way     the cross street under the boulevard blocks' south ends, garden lawn to terrace
    ///   Comet Row      the boulevard's road and 10 m promenades run on between N1 and N2 into
    ///   Orbit Square   a pedestrian square, Googie arcades facing each other across it (W1 / E1), a pavilion, café
    ///                  terraces, stalls; the harbour terrace (extended down the quarter's waterfront) opens off its east
    ///   Rocket Lane    leaves the square's south-west corner on a 215 deg diagonal between angled blocks (RL1, RL2;
    ///                  RB1 behind across a mews), SS5 on its east side where it opens from SS3
    ///   Satellite St   on the boulevard axis south from the square (the HQ at the far end of the vista behind)
    ///   Quay Road      a narrow street along the terrace edge; Apollo Way crosses at z -253, lane to terrace
    ///   Rocket / Starline / Launch Green   lawns in the wedges the streets leave; Garden Row, Orbit Walk and Pylon Row
    ///                  the gaps the west blocks leave, angled 6 deg off the boulevard grid
    /// Ground: one asphalt deck over the quarter; the blocks' pavements (DecoKitBlock), squares and greens stand on it.
    /// Everything is placed under its own root, so Rebuild can replace it on the saved street.
    internal static class DecoKitSouthside
    {
        internal const string RootName = "Southside";
        internal static readonly Rect Bounds = Rect.MinMaxRect(-158f, -300f, P.EastBack, L.Z.x);  // x, z
        internal static readonly Rect Square = Rect.MinMaxRect(-46f, -150f, P.EastBack, -100f);
        static readonly Vector2 RocketFrom = new(-16f, -150f);
        const float RocketPhi = 215f, Half = 5f;  // Rocket Lane carriageway half width

        static (string, int)[] S(params (string, int)[] s) => s;
        internal static Vector2 Lane(float t, float off) => RocketFrom + V2(P.Dir(RocketPhi)) * t + V2(P.Dir(RocketPhi + 90f)) * off;
        static Vector2 V2(Vector3 v) => new(v.x, v.z);

        internal static readonly DecoKitBlock[] Blocks = Plan();
        /// Block names a world plan gives to something else (a mission block); Build leaves their parcels empty.
        internal static readonly HashSet<string> Reserved = new();
        /// Blocks the world plan sets in reserved parcels: the city's pavements, pits and kerbside life round them;
        /// their buildings are the plan's.
        internal static readonly List<DecoKitBlock> Substitutes = new();
        internal static DecoKitBlock[] Built => Blocks.Where(b => !Reserved.Contains(b.Name)).ToArray();
        internal static IEnumerable<DecoKitBlock> Paved => Built.Concat(Substitutes);

        static DecoKitBlock[] Plan()
        {
            // the lane's west side (5 m road + 4 m walk + 14 m), and RB1 behind across an 8 m mews
            Vector2 rl1 = Lane(66f, 23f), rl2 = Lane(136f, 23f), rb1 = Lane(102f, 67f);
            return new[]
            {
                new DecoKitBlock("N1", -50f, -76f, 0f, 40f, S(("S4", 4)), S(("S4", 4)), walkR: 10f, d: 64f),  // Constellation
                new DecoKitBlock("N2", 32f, -76f, 0f, 40f, S(("S4", 4)), S(("S4", 1), ("P4", 2), ("S4", 1)), walkR: 0f, walkL: 10f),
                new DecoKitBlock("N4", -122f, -82f, -6f, 46f, S(("S4", 1), ("P4", 3), ("S4", 1)), S(("S4", 5))),
                new DecoKitBlock("W1", -64f, -133f, 0f, 34f, S(("S3", 1), ("A3", 1), ("S3", 1)), S(("S3", 3))),
                new DecoKitBlock("E1", 60f, -133f, 0f, 34f, S(("P3", 3)), S(("S3", 1), ("A3", 1), ("S3", 1)), 0f, 0f, 0f, 0f),
                new DecoKitBlock("W2", -119f, -140f, 90f, 46f, S(("S3", 5)), S(("S3", 1), ("P3", 3), ("S3", 1))),
                new DecoKitBlock("RL1", rl1.x, rl1.y, RocketPhi - 180f, 40f, S(("S3", 4)), S(("S3", 1), ("B3", 2), ("S3", 1))),
                new DecoKitBlock("RL2", rl2.x, rl2.y, RocketPhi - 180f, 58f, S(("S3", 2), ("B3", 3), ("S3", 2)), S(("S3", 7))),
                new DecoKitBlock("RB1", rb1.x, rb1.y, RocketPhi - 180f, 34f, S(("S3", 3)), S(("S3", 3))),
                new DecoKitBlock("SS1", -22f, -221f, 0f, 46f, S(("S3", 1), ("B3", 3), ("S3", 1)), S(("S3", 5))),
                new DecoKitBlock("SS2", 22f, -221f, 0f, 46f, S(("S4", 5)), S(("S4", 1), ("P4", 3), ("S4", 1)), walkR: 2f),
                new DecoKitBlock("SS3", -22f, -279f, 0f, 34f, S(("S3", 1), ("P3", 1), ("S3", 1)), S(("S3", 3))),
                new DecoKitBlock("SS4", 22f, -279f, 0f, 34f, S(("S4", 3)), S(("S4", 1), ("P4", 1), ("S4", 1)), walkR: 2f),
                new DecoKitBlock("SS5", -67f, -279f, 0f, 34f, S(("S4", 1), ("B4", 1), ("S4", 1)), S(("S4", 3))),
            };
        }

        /// The wedge lawns: Rocket Green between Satellite Street's kerb (x -4) and Rocket Lane's east kerb, Starline
        /// Green between Satellite Street and Quay Road, both down to SS1 / SS2's pavements; Launch Green the triangle the
        /// lane leaves west of SS1, down to Apollo Way.
        const float Foot = -194f, Apollo = -248f;  // SS1 / SS2's head and tail pavement edges
        internal static Vector2[] RocketGreen => new[] { new Vector2(-4f, Square.yMin), LaneKerb(Square.yMin), LaneKerb(Foot), new Vector2(-4f, Foot) };
        internal static Vector2[] LaunchGreen => new[] { new Vector2(-40f, Foot), new Vector2(-40f, Apollo), LaneKerb(Apollo), LaneKerb(Foot) };
        internal static readonly Vector2[] StarlineGreen = { new(4f, -150f), new(38f, -150f), new(38f, Foot), new(4f, Foot) };

        static Vector2 LaneKerb(float z)  // Rocket Lane's east kerb at z
        {
            Vector2 o = Lane(0f, -Half), d = V2(P.Dir(RocketPhi));
            return o + d * ((z - o.y) / d.y);
        }
        internal static readonly Rect HarbourGreen = Rect.MinMaxRect(52f, -206f, 70f, -158f);  // on the terrace

        internal static readonly Vector3 WalkTo = new(-3f, L.Walk, -115f);  // on the square: DecoKitWalker.Check
        internal static readonly (string name, Vector3 c, Vector3 size)[] Probes =
        {
            ("Southside square probe", new(0f, 6f, -125f), new(100f, 24f, 60f)),
            ("Southside north probe", new(-50f, 6f, -75f), new(200f, 24f, 60f)),
            ("Southside west probe", new(-104f, 6f, -170f), new(108f, 24f, 120f)),
            ("Southside lane probe", new(-110f, 6f, -262f), new(96f, 24f, 76f)),
            ("Southside south probe", new(-20f, 6f, -245f), new(160f, 24f, 110f)),
        };

        internal static bool Contains(Vector3 p, float margin = 0f) =>
            p.x > Bounds.xMin - margin && p.x < Bounds.xMax + margin && p.z > Bounds.yMin - margin && p.z < Bounds.yMax + margin;

        internal static void Build()
        {
            var old = GameObject.Find(RootName);
            if (old) Object.DestroyImmediate(old);
            var root = new GameObject(RootName).transform;
            var p = new DecoKitPlacer(root);
            var pits = new List<(Vector3 at, Vector3 inward, DecoKitBlock b)>();
            var blocks = Built;
            foreach (var b in blocks)
            {
                b.Frontage(p);
                pits.AddRange(b.Ring(p).Select(x => (x.at, x.inward, b)));
            }
            foreach (var b in Substitutes) pits.AddRange(b.Ring(p).Select(x => (x.at, x.inward, b)));
            Ground(p, root);
            var deck = DecoKitHighrise.Build(p, root, blocks);
            DecoKitSouthsideLife.Build(p, root, pits, deck);
            DecoKitTileMerge.Merge(root, "Southside tiles merged", root);
            var missing = p.Missing.Count == 0 ? "none" : string.Join(", ", p.Missing.OrderBy(n => n));
            Debug.Log($"[DecoKit] Southside: {blocks.Length} blocks{(Reserved.Count > 0 ? $" ({string.Join(", ", Reserved)} reserved; {Substitutes.Count} substitutes)" : "")}, {pits.Count} tree pits; missing prefabs: {missing}");
        }

        /// Reopen the saved street, replace Southside, repaint, save: layout iteration without the street's rebuild and
        /// bake (the quarter's probes keep their last bake). -dkShots picks DecoKitIso shots ("" = all, "none").
        internal static void Rebuild()
        {
            var cam = DecoKitStreetDemo.OpenSaved();
            Build();
            DecoKitColourways.Apply(DecoKitStage.Arg("dkColour", DecoKitStreetDemo.Colour));
            DecoKitStage.Save();
            DecoKitHarbour.Ocean();
            var shots = DecoKitStage.Arg("dkShots", "");
            if (shots != "none") DecoKitIso.ShootAll(DecoKitStage.Arg("dkOut", "iso"), shots);
            cam.enabled = false;
        }

        /// The quarter's asphalt, the square, the lawns, and the kerbs where roads meet walk-level ground the blocks
        /// don't pave: Sunset Way's north side (the boulevard blocks' south ends, the garden lawn), the square's road
        /// ends, the terrace edge.
        static void Ground(DecoKitPlacer p, Transform root)
        {
            var asphalt = G.Road();
            DecoKitStage.Slab("Southside road deck", Bounds, -.002f, asphalt).transform.SetParent(root, false);
            float w = L.Walk, n = L.Z.x;
            new G().Prism(Corners(Square), 0f, w).Emit("Orbit Square", G.Mat("DK_Terrazzo_Ivory"), true, root);
            DecoKitSquareInlay.Build(Square, w, root);
            var lawn = G.Lawn();
            var green = new G();
            var lawns = new G();
            foreach (var poly in new[] { RocketGreen, StarlineGreen, LaunchGreen })
            {
                green.Prism(poly, 0f, w);
                lawns.Prism(Inset(poly, .8f), w, w + .004f);
            }
            green.Emit("Southside green kerbs", G.Mat("DK_Granite_Kerb"), true, root);
            lawns.Block(HarbourGreen, w, w + .004f).Emit("Southside lawns", lawn, true, root);
            new G().Block(new Rect(HarbourGreen.x - .5f, HarbourGreen.y - .5f, HarbourGreen.width + 1f, HarbourGreen.height + 1f), w, w + .002f)
                   .Emit("Harbour green kerb", G.Mat("DK_Granite_Kerb"), false, root);

            // Sunset Way's north side: flags in front of the boulevard blocks' ends (the east block stands 2 m further
            // south) and west of the gardens; a lawn kerb along the garden
            foreach (var (x0, x1) in new[] { (Bounds.xMin, P.GardensX.x), (P.WestBack, -L.FrontX), (L.FrontX, P.EastBack) })
                for (float x = x0 + 1f; x < x1; x += 2f)
                {
                    Flags(p, x, n + 1f);
                    if (x > L.FrontX) Flags(p, x, n + 3f);
                }
            KerbLine(p, new Vector2(Bounds.xMin, n), new Vector2(-L.KerbX - 2f, n), Vector3.forward);
            KerbLine(p, new Vector2(L.KerbX + 2f, n), new Vector2(P.EastBack, n), Vector3.forward);
            // the square where roads end on it (Comet Row, Orbit Walk, the south streets), the terrace edge
            KerbLine(p, new Vector2(-L.KerbX, Square.yMax), new Vector2(L.KerbX, Square.yMax), Vector3.back);
            KerbLine(p, new Vector2(Square.xMin, -112f), new Vector2(Square.xMin, Square.yMax), Vector3.right);
            foreach (var (x0, x1) in new[] { (Square.xMin, -10f), (-4f, 4f), (38f, P.EastBack) })
                KerbLine(p, new Vector2(x0, Square.yMin), new Vector2(x1, Square.yMin), Vector3.forward);
            KerbLine(p, new Vector2(P.EastBack, Bounds.yMin), new Vector2(P.EastBack, Square.yMin), Vector3.right);
            KerbLine(p, new Vector2(P.EastBack, -52f), new Vector2(P.EastBack, n), Vector3.right);
        }

        static void Flags(DecoKitPlacer p, float x, float z) => p.Put($"Pave_Flags_{L.Pave(x, z)}", new Vector3(x, 0f, z), 90f);

        /// Granite kerb stones from a to b (2 m modules, the last one short of b if the run isn't a whole number).
        internal static void KerbLine(DecoKitPlacer p, Vector2 a, Vector2 b, Vector3 inward)
        {
            var d = (b - a).normalized;
            int count = Mathf.FloorToInt(Vector2.Distance(a, b) / 2f + .01f);
            for (int k = 0; k < count; k++)
            {
                var c = a + d * (2f * k + 1f);
                p.Put($"Kerb_Granite_{k & 1}", new Vector3(c.x, 0f, c.y), P.Yaw(-inward));
            }
        }

        static Vector2[] Corners(Rect r) => new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) };

        /// A convex polygon with every edge moved in by d (either winding).
        static Vector2[] Inset(Vector2[] poly, float d)
        {
            int n = poly.Length;
            float area = 0f;
            for (int i = 0; i < n; i++) area += poly[i].x * poly[(i + 1) % n].y - poly[(i + 1) % n].x * poly[i].y;
            float s = area > 0f ? 1f : -1f;  // CCW: the inside is on the left of each edge
            var o = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 a0 = poly[(i + n - 1) % n], a1 = poly[i], b1 = poly[(i + 1) % n];
                Vector2 da = (a1 - a0).normalized, db = (b1 - a1).normalized;
                Vector2 na = new Vector2(-da.y, da.x) * s, nb = new Vector2(-db.y, db.x) * s;
                // intersect the two moved edge lines: a1 + na d + da t = a1 + nb d + db q
                float cross = da.x * db.y - da.y * db.x;
                var off = (nb - na) * d;
                float t = Mathf.Abs(cross) < 1e-5f ? 0f : (off.x * db.y - off.y * db.x) / cross;
                o[i] = a1 + na * d + da * t;
            }
            return o;
        }
    }
}
#endif
