#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using static Suborned.OldTown.Editor.DecoKit.DecoKitFrontage;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// The frontage of the composed district (DecoKitCityPlan): the boulevard's two walls broken by Market Street, the
    /// lanes and Crown Circus; the blocks' backs face the harbour terraces (east) and Orbit Gardens (west); four
    /// low streamline crescents ring the circus. Runs are authored, not generated: rhythm and heights are chosen per
    /// wall (the west keeps the low civic hall so the sphere station shows over it from the study camera).
    internal static class DecoKitCityBlocks
    {
        static readonly Vector3 N = Vector3.forward, S = Vector3.back, E = Vector3.right, W = Vector3.left;

        /// Lays the district's frontage; returns the straight walls' roofs for DecoKitRoofline.
        internal static List<DecoKitRoofline.Roof> Build(DecoKitPlacer p)
        {
            var roofs = new List<DecoKitRoofline.Roof>();
            void Wall(Vector3 start, Vector3 dir, Vector3 n, (string cls, int n)[] segs, Cap first, Cap last)
            {
                Run(p, start, dir, n, segs, first, last);
                roofs.AddRange(DecoKitRoofline.Roofs(p, start, dir, n, segs, first, last));
            }

            float fx = L.FrontX, eb = P.EastBack, wb = P.WestBack, n0 = P.CircusNorth;
            // east: boulevard wall (faces -X) and terrace wall (faces +X), south to north
            Wall(V(fx, P.EastSouth), N, W, Seg(("S9", 4), ("D10", 3), ("S6", 6)), Cap.Corner, Cap.Corner);
            Wall(V(fx, 78f), N, W, Seg(("S9W", 5), ("S9", 3)), Cap.Corner, Cap.End);
            Wall(V(fx, P.EastLane.y), N, W, Seg(("S9", 5), ("S6", 1)), Cap.End, Cap.Open);
            Wall(V(fx, n0), N, W, Seg(("S6", 3), ("D10", 2), ("P7", 3), ("S9W", 4), ("S9", 5)), Cap.Open, Cap.Corner);
            Wall(V(eb, P.EastSouth), N, E, Seg(("S6", 4), ("B8", 5), ("P7", 4)), Cap.Corner, Cap.Corner);
            Wall(V(eb, 78f), N, E, Seg(("D10", 3), ("S6", 5)), Cap.Corner, Cap.End);
            Wall(V(eb, P.EastLane.y), N, E, Seg(("S9W", 5)), Cap.End, Cap.Corner);
            Wall(V(eb, P.ParadeNorth), N, E, Seg(("A3", 3), ("S9", 4), ("S9W", 5), ("S6", 4)), Cap.Corner, Cap.Tower);
            // east short walls between the corners (x 26..38): the south end, both sides of Market Street, the forecourt
            Short(p, fx, P.EastSouth, S, "S9", "S6");
            Short(p, fx, 58f, N, "S6", "S6");
            Short(p, fx, 78f, S, "S9W", "D10");
            Short(p, fx, P.NorthFacade, N, "S9", "S6");

            // west: boulevard wall (faces +X) keeps the civic hall low; garden wall (faces -X)
            Wall(V(-fx, P.WestSouth), N, E, Seg(("H", 4)), Cap.Corner, Cap.Corner);
            Wall(V(-fx, P.WestLane.y), N, E, Seg(("S6", 2), ("H", 12), ("S9W", 4), ("D10", 3), ("S9", 4), ("S6", 3)),
                Cap.End, Cap.Open);
            Wall(V(-fx, n0), N, E, Seg(("S6", 3), ("D10", 2), ("S9", 4), ("B8", 3), ("S9W", 5)), Cap.Open, Cap.Corner);
            Wall(V(wb, P.WestSouth), N, W, Seg(("S6", 4)), Cap.Corner, Cap.Corner);
            Wall(V(wb, P.WestLane.y), N, W, Seg(("S6", 10), ("S9W", 5), ("S6", 6), ("S9", 6)), Cap.End, Cap.Corner);
            Wall(V(wb, P.ParadeNorth), N, W, Seg(("S6", 4), ("S9", 4), ("S6", 4), ("S9W", 4)), Cap.Corner, Cap.Tower);
            Short(p, wb, P.WestSouth, S, "S6", "H");
            Short(p, wb, P.WestLane.x, N, "S6", "H");  // the lane's south wall
            Short(p, wb, P.NorthFacade, N, "S9W", "S9W");
            HallFill(new Vector2(P.WestSouth + 8f, P.WestLane.x - 8f), new Vector2(P.WestLane.y + 12f, P.WestLane.y + 84f));
            // Orbit Gardens' west terrace (faces the canal): a garden-square wall that hides the massing behind it
            Wall(V(P.GardensX.x, P.GardensZ.x + 4f), N, E,
                Seg(("S6", 6), ("B8", 5), ("S6", 4), ("D10", 3), ("S6", 6), ("P7", 5), ("S6", 5), ("S9W", 5), ("S6", 6)),
                Cap.Corner, Cap.Corner);

            // Crown Circus: four crescents of four bays from the boulevard walls, rounding off into the parade and gardens
            float phi = Mathf.Asin(fx / P.CrescentR) * Mathf.Rad2Deg;
            Crescent(p, P.Circus, P.CrescentR, phi, 1f, "S6", 4, true);
            Crescent(p, P.Circus, P.CrescentR, 180f - phi, -1f, "S6", 4, true);
            Crescent(p, P.Circus, P.CrescentR, -phi, -1f, "S6", 4, true);
            Crescent(p, P.Circus, P.CrescentR, phi - 180f, 1f, "S6", 4, true);
            return roofs;
        }

        static Vector3 V(float x, float z) => new(x, 0f, z);
        static (string, int)[] Seg(params (string, int)[] s) => s;

        /// Two bays between the corners of a block's short side, from its west corner at x0: each takes the class of the
        /// long wall whose corner it adjoins.
        static void Short(DecoKitPlacer p, float x0, float z, Vector3 n, string westCls, string eastCls)
        {
            var segs = westCls == eastCls ? Seg((westCls, 2)) : Seg((westCls, 1), (eastCls, 1));
            Run(p, V(x0 + 8f, z), E, n, segs, Cap.Open, Cap.Open);
        }

        /// The civic hall is 8 m deep against 14 m back bays: a limestone deck at its parapet closes the gap.
        static void HallFill(params Vector2[] zs)
        {
            var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>($"{DecoKitData.MaterialDir}/DK_Limestone.mat");
            foreach (var z in zs)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Hall fill";
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.transform.position = new Vector3(-L.FrontX - 11f, L.Walk + 5.5f, (z.x + z.y) * .5f);
                go.transform.localScale = new Vector3(6f, 11f, z.y - z.x);
                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
                go.isStatic = true;
            }
        }
    }
}
#endif
