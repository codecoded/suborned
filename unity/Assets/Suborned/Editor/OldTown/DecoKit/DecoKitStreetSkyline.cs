#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Everything above the street (skyline.py, landmarks.py; Concepts/Streets/spaceage, mega): the composed
    /// district's frontage (DecoKitCityBlocks) and the landmarks stacked in depth down the vista. From the pavement the
    /// 37 m canyon hides anything more than ~10 deg off axis on the near (east) side, so landmarks sit on or near the
    /// axis, behind the low civic run west of the camera, or out on the water where the parade opens to the harbour.
    internal static class DecoKitStreetSkyline
    {
        /// Returns the district walls' roofs (DecoKitRoofline programmes them once the district is dressed).
        internal static List<DecoKitRoofline.Roof> Build(DecoKitPlacer p)
        {
            var roofs = DecoKitCityBlocks.Build(p);
            Landmarks(p);
            return roofs;
        }

        /// Pivots at the footprint centre on the road deck. The HQ terminates the boulevard through the arch; the Skylon
        /// floats over the HQ plaza off axis; the liner dock closes Market Street's vista at the end of its pier.
        static void Landmarks(DecoKitPlacer p)
        {
            p.Put("SpaceHQ", new Vector3(0f, 0f, L.Z.y + 70f), 0f, scale: 1.35f);  // 337 m; grand stair foot at Z.y + 5
            p.Put("ParabolicArch", new Vector3(0f, L.Walk, 250f));                 // frames the HQ from the street
            foreach (var x in new[] { -44f, 44f }) p.Put("DiscTower", new Vector3(x, 0f, L.Z.y - 8f), x * 2f);  // frame the arch
            p.Put("SkyBridge", new Vector3(0f, 60f, L.Z.y - 8f));                  // gate lintel over the plaza
            p.Put("Skylon", new Vector3(58f, 0f, L.Z.y + 20f), 90f);             // HQ plaza, clear of the axis
            p.Put("SphereStation", new Vector3(-130f, 0f, 330f), 20f);
            p.Put("LinerDock", new Vector3(270f, P.WaterY, (P.MarketPierZ.x + P.MarketPierZ.y) / 2f), -90f);
            foreach (var t in Towers) p.Put(t.name, t.at, t.yaw);
        }

        // background towers up the coast and inland (DecoKitCityMass keeps clear of them)
        internal static readonly (string name, Vector3 at, float yaw)[] Towers =
        {
            ("Tower_Setback", new(-95f, 0f, 700f), 0f), ("Tower_Saucer", new(30f, 0f, 780f), 15f),
            ("Tower_Stepped", new(-40f, 0f, 930f), 0f), ("Tower_Setback", new(40f, 0f, 1080f), 30f),
            ("Tower_Stepped", new(-190f, 0f, 560f), 10f), ("Tower_Saucer", new(-260f, 0f, 820f), -20f),
        };
    }
}
#endif
