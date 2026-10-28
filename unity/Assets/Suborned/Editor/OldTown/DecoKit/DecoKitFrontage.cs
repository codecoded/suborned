#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Places frontage modules along a facade line (straight runs) or an arc (crescents). A run is a list of
    /// (class, count) segments of 6 m bays between two caps: a corner (8 x 8 m cell turning the block), an end (the
    /// last bay rounds off into a lane or a plaza) or open (it joins another run). A streamline segment taller than its
    /// neighbour rounds off toward it (StreamEnd), as the boulevard's first pass did. Classes map to kit pieces with
    /// fallbacks, so the plan builds before the new facade family lands.
    internal static class DecoKitFrontage
    {
        internal enum Cap { Open, Corner, End, Tower }  // Tower: CornerTower_12's 10 m cell, 2 m proud of both faces

        internal readonly struct Piece
        {
            internal readonly string Bay, End, Corner;
            internal readonly float Height;
            internal Piece(string bay, string end, string corner, float height) { Bay = bay; End = end; Corner = corner; Height = height; }
        }

        internal static Piece Of(DecoKitPlacer p, string cls) => cls switch
        {
            "S6" => new("StreamBay_6", "StreamEnd_6", Pick(p, "StreamCorner_6", "StreamCorner_9"), 26.5f),
            "S9" => new("StreamBay_9", "StreamEnd_9", "StreamCorner_9", 36.7f),
            "S9W" => new("StreamBay_9W", "StreamEnd_9W", "StreamCorner_9", 36.7f),
            "D10" => new("DecoBay_10", Pick(p, "DecoEnd_10", "StreamEnd_9W"), Pick(p, "DecoCorner_10", "StreamCorner_9"), 40f),
            "P7" => new(Pick(p, "PortholeBay_7", "StreamBay_6"), Pick(p, "PortholeEnd_7", "StreamEnd_6"), Pick(p, "StreamCorner_6", "StreamCorner_9"), 29f),
            "B8" => new(Pick(p, "BalconyBay_8", "StreamBay_9W"), Pick(p, "BalconyEnd_8", "StreamEnd_9W"), "StreamCorner_9", 33f),
            "A3" => new(Pick(p, "ArcadeBay_3", "HallBay"), Pick(p, "ArcadeEnd_3", "HallCornerWing"), "HallCornerWing", 16f),
            "H" => new("HallBay", "HallCornerWing", "HallCornerWing", 11.1f),
            // low-rise (lowrise.py, Southside): 2 / 3 floors over the shop
            "S2" => new("StreamBay_2", "StreamEnd_2", "StreamCorner_2", 12.9f),
            "S3" => new("StreamBay_3", "StreamEnd_3", "StreamCorner_3", 16.3f),
            "S3W" => new("StreamBay_3W", "StreamEnd_3W", "StreamCorner_3", 16.3f),
            "P3" => new("PortholeBay_3", "StreamEnd_3W", "StreamCorner_3", 16.3f),
            "B3" => new("BalconyBay_3", "StreamEnd_3", "StreamCorner_3", 16.3f),
            // podium (highrise.py): 4 floors over the shop; Southside's roofs are DecoKitHighrise's decks
            "S4" => new("StreamBay_4", "StreamEnd_4", "StreamCorner_4", 19.7f),
            "P4" => new("PortholeBay_4", "StreamEnd_4", "StreamCorner_4", 19.7f),
            "B4" => new("BalconyBay_4", "StreamEnd_4", "StreamCorner_4", 19.7f),
            _ => throw new System.ArgumentException($"[DecoKit] unknown frontage class {cls}"),
        };

        static string Pick(DecoKitPlacer p, string want, string fallback) => p.Has(want) ? want : fallback;
        static bool Stream(string cls) => cls != "H" && cls != "A3";

        /// Every segment of a run is one building: its pieces are named "<prefab> #<run>.<segment>" (segments in order
        /// along the wall) so a later pass (DecoKitColourways) can paint a building as a whole and know its neighbours.
        static int run;

        static GameObject Tag(GameObject go, int r, int seg)
        {
            if (go != null) go.name = $"{go.name} #{r}.{seg}";
            return go;
        }

        /// The run's bays in order: each one's segment, where it starts along the run, and whether it rounds off toward
        /// -dir (endS) or +dir (endN): the run's first / last bay before an end cap, or a segment's first / last bay
        /// where it stands taller than its neighbour.
        internal static IEnumerable<(int seg, float at, bool endS, bool endN)> Bays(DecoKitPlacer p, (string cls, int n)[] segs,
                                                                                    Cap first, Cap last)
        {
            float at = first == Cap.Corner ? 8f : 0f;
            int total = segs.Sum(s => s.n), i = 0;
            for (int g = 0; g < segs.Length; g++)
            {
                var (cls, count) = segs[g];
                float h = Of(p, cls).Height, prevH = g > 0 ? Of(p, segs[g - 1].cls).Height : 0f, nextH = g < segs.Length - 1 ? Of(p, segs[g + 1].cls).Height : 0f;
                for (int k = 0; k < count; k++, i++, at += 6f)
                    yield return (g, at, i == 0 && first == Cap.End || k == 0 && g > 0 && Stream(cls) && h > prevH + 1f,
                                  i == total - 1 && last == Cap.End || k == count - 1 && g < segs.Length - 1 && Stream(cls) && h > nextH + 1f);
            }
        }

        /// A straight run from `start` along `dir` with the facade facing `n`. Returns the length it used.
        internal static float Run(DecoKitPlacer p, Vector3 start, Vector3 dir, Vector3 n, (string cls, int n)[] segs,
                                  Cap first, Cap last)
        {
            float yaw = P.Yaw(n);
            int r = ++run;
            if (first == Cap.Corner) Tag(Corner(p, Of(p, segs[0].cls).Corner, start, n, -dir), r, 0);
            foreach (var (g, a, endS, endN) in Bays(p, segs, first, last))
            {
                var piece = Of(p, segs[g].cls);
                var c = start + dir * (a + 3f);
                Tag(endS || endN ? End(p, piece, c, n, endS ? -dir : dir) : p.Put(piece.Bay, c + Vector3.up * L.Walk, yaw), r, g);
            }
            float at = (first == Cap.Corner ? 8f : 0f) + 6f * segs.Sum(s => s.n);
            if (last == Cap.Corner || last == Cap.Tower && !p.Has("CornerTower_12"))
                Tag(Corner(p, Of(p, segs[^1].cls).Corner, start + dir * (at + 8f), n, dir), r, segs.Length - 1);
            else if (last == Cap.Tower)
                Tag(Corner(p, "CornerTower_12", start + dir * (at + 10f) + n * 2f, n, dir, 5f), r, segs.Length);
            if (last == Cap.Corner || last == Cap.Tower) at += 8f;
            return at;
        }

        /// A crescent: `count` bays on chords of a circle of radius r about `centre`, facing it, from compass angle phi0
        /// stepping by sign(step); the last bay rounds off into whatever opens beyond the crescent.
        internal static void Crescent(DecoKitPlacer p, Vector3 centre, float r, float phi0, float step, string cls, int count, bool endCap)
        {
            var piece = Of(p, cls);
            int terrace = ++run;  // the whole crescent is one building: segment 0
            float d = 2f * Mathf.Asin(3f / r) * Mathf.Rad2Deg * Mathf.Sign(step);
            Wedges(centre, r, phi0, d, count, piece.Height);
            for (int k = 0; k < count; k++)
            {
                float phi = phi0 + d * (k + .5f);
                var radial = P.Dir(phi);
                var c = centre + radial * (r * Mathf.Cos(Mathf.Abs(d) * .5f * Mathf.Deg2Rad));
                Tag(endCap && k == count - 1 ? End(p, piece, c, -radial, P.Dir(phi + 90f * Mathf.Sign(step)))
                    : p.Put(piece.Bay, c + Vector3.up * (L.Walk - (k & 1) * .005f), P.Yaw(-radial)), terrace, 0);  // odd bays 5 mm down: the chords' canopies overlap at the joints
            }
        }

        /// Chord bays on a concave arc fan apart behind the facade (a 2.4 m wedge between 14 m deep party walls at
        /// R 35): limestone prisms from each joint to the two back corners close them to the roofline.
        static void Wedges(Vector3 centre, float r, float phi0, float d, int count, float height)
        {
            var g = new DecoKitGroundMesh();
            Vector2 C(Vector3 v) => new(v.x, v.z);
            for (int k = 0; k < count - 1; k++)
            {
                var joint = centre + P.Dir(phi0 + d * (k + 1)) * r;
                var a = joint + P.Dir(phi0 + d * (k + .5f)) * 14f;
                var b = joint + P.Dir(phi0 + d * (k + 1.5f)) * 14f;
                g.Prism(new[] { C(joint), C(a), C(b) }, L.Walk, L.Walk + height - .2f);
            }
            g.Emit($"Crescent wedges {phi0:0}", DecoKitGroundMesh.Mat("DK_Limestone"), false);
        }

        /// The end bay rounds toward `toward`: unmirrored it rounds toward the module's +X (kit: Blender -X).
        static GameObject End(DecoKitPlacer p, Piece piece, Vector3 c, Vector3 n, Vector3 toward)
        {
            float yaw = P.Yaw(n);
            bool mirror = Vector3.Dot(P.Rotate(yaw, Vector3.right), toward) < 0f;
            return piece.End == "HallCornerWing" ? Corner(p, piece.End, c + toward * 3f, n, toward)  // the wing is the hall's end
                : p.Put(piece.End, c + Vector3.up * L.Walk, yaw, mirror: mirror);
        }

        /// Corner cell (8 x 8, or `half` * 2) at the block corner `at` where facades facing a and b meet. Unmirrored at
        /// yaw 0 it faces +Z and +X with the corner at local (+half, 0) (plaza.py HallCornerWing convention); mirrored,
        /// -X and (-half, 0).
        internal static GameObject Corner(DecoKitPlacer p, string name, Vector3 at, Vector3 a, Vector3 b, float half = 4f)
        {
            float yaw = P.Yaw(a);
            bool mirror = Vector3.Dot(P.Rotate(yaw, Vector3.right), b) < 0f;
            var pivot = at - P.Rotate(yaw, new Vector3(mirror ? -half : half, 0f, 0f));
            return p.Put(name, pivot + Vector3.up * L.Walk, yaw, mirror: mirror);
        }
    }
}
#endif
