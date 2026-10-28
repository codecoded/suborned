#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// plaza.png recreated from kit pieces. Frame: hall front on z = 0 facing +Z, plaza toward +Z,
    /// tram street along +X (image left), camera on the +Z side. 2 m ground modules, walk level 0.15 m.
    internal static class DecoKitPlazaLayout
    {
        internal const float Walk = .15f;  // sidewalk / plaza top; road and rails at 0
        internal const float KerbX = 21f;  // plaza edge = kerb line
        internal const float HallX = -2.5f;  // hall centre: leaves a 3.5 m street-side walk past the return bays
        const int Bays = 1;  // per side; plaza.png itself shows 0, 2 would cross the kerb
        internal const float StreetMaxX = 40f;
        internal static readonly Vector2 PlazaZ = new(0f, 48f);
        internal const float PlazaMinX = -32f;
        const float BedSoil = .55f, HexSoil = .5f;  // PlanterBed soil 5 cm under its 0.6 m rim; PlanterHex is 0.56 m
        static readonly Vector2[] Rings = { new(-8f, 24f), new(10f, 34f) };  // module corners: cells sit on odd coords
        internal static readonly Vector2 StreetZ = new(-50f, 130f);  // quay terminus behind the hall; out of frame ahead

        internal static void Build(DecoKitPlacer p)
        {
            var hall = Hall(p);
            Floor(p);
            Street(p);
            Dressing(p, hall);
        }

        /// Wing | bays | entrance tower | bays | wing (mirrored), side bays returning from the corners. Hall modules
        /// pivot on their facade plane (front-centre, depth toward -Z), so they snap by pivot, not bounds: the tower's
        /// landing and canopy project 2 m forward of the common facade line z = 0.
        static float[] Hall(DecoKitPlacer p)
        {
            var joints = new List<float>();
            p.Put("HallEntranceTower", new Vector3(HallX, Walk, 0f));
            for (int k = 0; k < Bays; k++)
            for (int side = -1; side <= 1; side += 2)
            {
                p.Put("HallBay", new Vector3(HallX + side * (9f + 6f * k), Walk, 0f));
                joints.Add(HallX + side * (12f + 6f * k));
            }
            float xc = 6f + 6f * Bays + 4f;  // corner cell centre (8 m cell)
            p.Put("HallCornerWing", new Vector3(HallX + xc, Walk, 0f));  // Blender's unmirrored corner lands on +X
            p.Put("HallCornerWing", new Vector3(HallX - xc, Walk, 0f), 0f, mirror: true);
            foreach (var z in new[] { 11f, 17f, 23f })
            {
                p.Put("HallBay", new Vector3(HallX + xc + 4f, Walk, -z), 90f);
                p.Put("HallBay", new Vector3(HallX - xc - 4f, Walk, -z), -90f);
            }
            p.Put("GrandStair", new Vector3(HallX, Walk, 5f));  // bottom step edge; rises to the tower landing
            return joints.ToArray();
        }

        /// Ivory field, one terrazzo band row, two inlaid rings of TerrazzoArc quadrants.
        static void Floor(DecoKitPlacer p)
        {
            for (float x = PlazaMinX + 1f; x < KerbX; x += 2f)
            for (float z = PlazaZ.x + 1f; z < PlazaZ.y; z += 2f)
            {
                p.Put(Mathf.Approximately(z, 15f) ? "Tile_Terrazzo" : "Tile_Plain", new Vector3(x, 0f, z));  // tops at Walk
            }
            Inlays(p);
        }

        /// Plaza-scale sweeping bands (InlayArc_R*: 15 deg sectors pivoted on the ring centre, sector along local
        /// -X in Unity). Sweep given as world polar angles phi (x = cos, z = sin); sectors leaving the plaza are dropped.
        static void Inlays(DecoKitPlacer p)
        {
            Sweep(p, 10, Rings[0], 0f, 360f);
            Sweep(p, 6, Rings[1], 0f, 360f);
            Sweep(p, 16, new Vector2(0f, 0f), 15f, 165f);  // from the stair foot, as in plaza.png
        }

        static void Sweep(DecoKitPlacer p, int radius, Vector2 c, float phi0, float phi1)
        {
            for (float phi = phi0 + 7.5f; phi < phi1; phi += 15f)
            {
                var mid = c + radius * new Vector2(Mathf.Cos(phi * Mathf.Deg2Rad), Mathf.Sin(phi * Mathf.Deg2Rad));
                if (mid.x < PlazaMinX + 1f || mid.x > KerbX - 1f || mid.y < PlazaZ.x + 1f || mid.y > PlazaZ.y - 1f) continue;
                p.Put($"InlayArc_R{radius}", new Vector3(c.x, 0f, c.y), 180f - phi);  // authored at the walk surface
            }
        }

        static void Street(DecoKitPlacer p)
        {
            for (float z = StreetZ.x + 1f; z < StreetZ.y; z += 2f)
            {
                p.Put("Kerb_Straight", new Vector3(KerbX, 0f, z), 90f);
                p.Put("Tile_TramRail", new Vector3(27f, 0f, z), 90f);
            }
            p.Put("ManholeCover", new Vector3(34f, 0f, 20f));
            p.Put("DrainGrate", new Vector3(KerbX + .6f, 0f, 9f), 90f);
            p.Put("DrainGrate", new Vector3(KerbX + .6f, 0f, 41f), 90f);
            p.Put("Tram", new Vector3(27f, 0f, 14f));
            p.Put("BoxTruck", new Vector3(34.5f, 0f, -6f));
        }

        static void Dressing(DecoKitPlacer p, float[] joints)
        {
            foreach (var x in joints) p.Put("Cypress", new Vector3(x, Walk, 1.2f));
            foreach (var dx in new[] { -6.2f, 6.2f }) p.Put("PottedTopiary", new Vector3(HallX + dx, Walk, 6.4f));
            foreach (var dx in new[] { -7.4f, 7.4f })  // flower beds flanking the stair, as in plaza.png
            {
                p.Put("PlanterBed", new Vector3(HallX + dx, Walk, 3.4f), 90f);
                foreach (var dz in new[] { -1f, 1f }) p.Put("FlowerBed", new Vector3(HallX + dx, Walk + BedSoil, 3.4f + dz), 90f);
            }

            for (float x = HallX - 19f; x <= HallX + 19f; x += 2f)  // hedge along the facade base, clear of the stair
                if (Mathf.Abs(x - HallX) > 8f && !System.Array.Exists(joints, j => Mathf.Abs(j - x) < 1f))
                    p.Put("ShrubMound", new Vector3(x, Walk, .9f), x * 37f);
            foreach (var x in new[] { -24f, -20f, -16f })  // cafe terrace edge
            {
                p.Put("PlanterBed", new Vector3(x, Walk, 19.2f));
                foreach (var dx in new[] { -1f, 1f }) p.Put("FlowerBed", new Vector3(x + dx, Walk + BedSoil, 19.2f));
            }
            foreach (var (x, z) in new[] { (-26f, 4f), (-12f, 4f), (15f, 3f), (-28f, 46f) }) p.Put("TopiaryCone", new Vector3(x, Walk, z));

            // tram stop along the kerb (fronts face the street, +X)
            p.Put("TramCanopy", new Vector3(19.4f, Walk, 16f), 90f);
            p.Put("TramCanopy", new Vector3(19.4f, Walk, 28f), 90f);
            p.Put("TicketMachine", new Vector3(19.6f, Walk, 11.5f), 90f);
            p.Put("InfoLightbox", new Vector3(19.6f, Walk, 33f), 90f);
            foreach (var z in new[] { 5f, 39f, 49f })
            {
                p.Put("PlanterBed", new Vector3(19.2f, Walk, z), 90f);
                p.Put("FlowerBed", new Vector3(19.2f, Walk + BedSoil, z - 1f), 90f);
                p.Put("FlowerBed", new Vector3(19.2f, Walk + BedSoil, z + 1f), 90f);
            }
            foreach (var z in new[] { 2f, 22f, 44f }) p.Put("Streetlamp", new Vector3(20.2f, Walk, z), 90f);
            p.Put("CCTVPole", new Vector3(20.3f, Walk, -5f), 90f);
            p.Put("TrafficLight", new Vector3(20.4f, Walk, 55f), 90f);
            for (float z = 58f; z < 66f; z += 1.6f) p.Put("Bollard", new Vector3(20.4f, Walk, z));

            // plaza
            foreach (var c in Rings)  // lamps in hex planters mark the ring centres (planter centre is left free for one)
            {
                p.Put("PlanterHex", new Vector3(c.x, Walk, c.y));
                p.Put("PlazaSphereLamp", new Vector3(c.x, Walk + HexSoil, c.y));
            }
            p.Put("PlazaSphereLamp", new Vector3(-21f, Walk, 5f));
            p.Put("PlanterHex", new Vector3(-17f, Walk, 25f));
            p.Put("FanPalm", new Vector3(-17f, Walk + HexSoil, 25f));
            p.Put("PlanterHex", new Vector3(3f, Walk, 40f));
            p.Put("FanPalm", new Vector3(3f, Walk + HexSoil, 40f), 40f);
            foreach (var (x, z) in new[] { (13f, 38f), (-11f, 41f), (15.5f, 10f), (-27f, 30f), (-4f, 46f), (6f, 18f) })
                p.Put("CoconutPalm", new Vector3(x, Walk, z), x * 17f);
            foreach (var (x, z) in new[] { (-13f, 6f), (13f, 6f), (-23f, 20f) }) p.Put("ShrubMound", new Vector3(x, Walk, z));
            p.Put("PlazaFountain", new Vector3(-17f, Walk, 40f));
            foreach (var (x, z) in new[] { (-17f, 10f), (-22.5f, 13f), (-17f, 16f) }) p.Put("CafeSet", new Vector3(x, Walk, z));
            p.Put("RecyclingBins", new Vector3(-12.5f, Walk, 12f), 90f);
            p.Put("Bench", new Vector3(3f, Walk, 29f), 180f);
            p.Put("Bench", new Vector3(-12f, Walk, 30f), 180f);
            p.Put("BikeRack", new Vector3(11f, Walk, 46f));
            p.Put("Scooter", new Vector3(15f, Walk, 45f), 30f);
            p.Put("ServiceRobot", new Vector3(0f, Walk, 25f), 200f);
            p.Put("PhoneBooth", new Vector3(-29f, Walk, 8f), 90f);
        }
    }
}
#endif
