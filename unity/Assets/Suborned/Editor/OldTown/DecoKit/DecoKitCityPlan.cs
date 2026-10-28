#if UNITY_EDITOR
using UnityEngine;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// The composed district around the boulevard (studies: Concepts/District). The boulevard canyon is the
    /// spine, but the city opens off it into places:
    ///   Crown Circus   a ring road round a monument island, four streamline crescents; the tram terminates short of it
    ///   Crown Parade   the east wedge between the crescents, widening to the harbour terraces (the skyport's vista)
    ///   Harbour        an upper terrace, steps down to a quay promenade, piers to the liner dock and the skyport;
    ///                  the sea closes the east half of the scene
    ///   Orbit Gardens  a park west of the frontage under the sky rail's return leg, up to the sphere station
    ///   lanes          6 m passages through the frontage blocks (east to the terraces, west to the gardens)
    /// Frontage blocks are 28 m deep (14 m bays facing both ways) with 8 m corners and 6 m bays, so runs close on the
    /// module grid; the south end is free, so the west starts 2 m further south than the east.
    internal static class DecoKitCityPlan
    {
        internal const float Depth = 28f, LaneW = 6f;
        // Crown Circus: island kerb, ring-road outer kerb, crescent facades (the boulevard facades meet it at z 176 / 236)
        internal static readonly Vector3 Circus = new(0f, 0f, 206f);
        internal const float IslandR = 14f, RingR = 26f, CrescentR = 35f;
        internal static float CircusSouth => Circus.z - Mathf.Sqrt(CrescentR * CrescentR - L.FrontX * L.FrontX);  // 176
        internal static float CircusNorth => Circus.z + Mathf.Sqrt(CrescentR * CrescentR - L.FrontX * L.FrontX);  // 236
        // blocks
        internal const float EastBack = L.FrontX + Depth, WestBack = -L.FrontX - Depth;   // +-46: terrace / garden facades
        internal const float EastSouth = -36f, WestSouth = -38f, NorthFacade = 346f;       // the frontage ends on the forecourt
        internal static readonly Vector2 EastLane = new(134f, 140f), WestLane = new(2f, 8f);
        internal const float ParadeSouth = 178f, ParadeNorth = 234f;                        // terrace-side corners of the parade
        // harbour: upper terrace at walk level, steps, quay promenade, water
        internal const float StepsX = 74f, PromenadeX = 80f, QuayX = 96f, PromenadeY = -.85f, WaterY = -2f;
        internal static readonly Vector2 HarbourZ = new(-300f, 380f);  // the terrace runs on down Southside's waterfront
        internal static readonly Vector2 MarketPierZ = new(60f, 76f), SkyportPierZ = new(196f, 216f);
        internal static readonly Vector3 Skyport = new(170f, WaterY, 206f);
        // gardens: from the west facades to the rail's return leg
        internal static readonly Vector2 GardensX = new(-110f, WestBack), GardensZ = new(-40f, 250f);  // west terrace backs to -124

        /// Inside the composed district (streets, blocks, terraces, gardens): the procedural massing stays out.
        internal static bool InDistrict(float x, float z) => x > GardensX.x - 2f && z > HarbourZ.x - 2f && z < HarbourZ.y + 20f;

        internal static bool InCircus(float x, float z, float r = CrescentR) =>
            new Vector2(x - Circus.x, z - Circus.z).sqrMagnitude < r * r;

        /// Unity yaw that turns a module's front (+Z) to face n; the rotation it applies.
        internal static float Yaw(Vector3 n) => Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg;
        internal static Vector3 Rotate(float yaw, Vector3 v) => Quaternion.Euler(0f, yaw, 0f) * v;

        /// Unit vector at compass angle phi (deg from +Z toward +X).
        internal static Vector3 Dir(float phi) => new(Mathf.Sin(phi * Mathf.Deg2Rad), 0f, Mathf.Cos(phi * Mathf.Deg2Rad));
    }
}
#endif
