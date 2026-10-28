#if UNITY_EDITOR
using UnityEngine;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Boulevard along +Z built on paving v2 (paving.py): limestone flags, granite kerbs with sett gutters, one tram
    /// lane with a stop island, an east side street with R 2 m corners, palms in tree pits (buildings: StreetSkyline).
    /// Crown Circus (DecoKitCircus) interrupts it: walks, kerbs and pits stop where the circus's own ground begins.
    /// Ground modules sit at y = 0 (their walk surface is at Walk); everything standing on the walk sits at Walk.
    /// Yaws (Unity): kerb walk side +X = -90, -X = 90, -Z = 0, +Z = 180; paving 90 (courses along Z).
    internal static class DecoKitStreetLayout
    {
        internal const float Walk = .15f;
        internal const float KerbX = 8f;            // kerb lines at +-KerbX; road between
        internal const float FrontX = 18f;          // facade plane: a 10 m promenade (5 paving modules)
        internal const float TrackX = -2.5f;        // tram lane centre
        internal const float SideEnd = P.EastBack;  // Market Street runs from the kerb to the harbour terrace
        internal static readonly Vector2 Z = new(-40f, 380f);  // the Space HQ closes it at Z.y + 70
        internal static readonly Vector2 SideZ = new(60f, 76f);   // side-street kerb lines
        static readonly Vector2 IslandZ = new(100f, 124f);          // tram stop island (kerb lines x = -6, -4)
        static float TramEnd => P.CircusSouth - 6f;                   // the tram terminates short of the circus

        internal static void Build(DecoKitPlacer p)
        {
            Walks(p);
            SideStreet(p);
            Island(p);
            for (float z = Z.x + 1f; z < TramEnd; z += 2f) p.Put("Tile_TramRail", new Vector3(TrackX, 0f, z), 90f);
            var roofs = DecoKitStreetSkyline.Build(p);  // frontage, side-street buildings, landmarks
            Dressing(p);
            DecoKitDistrictDressing.Build(p);
            DecoKitRoofline.Build(p, roofs);  // after the pads and billboards, which it keeps clear of
        }

        /// Sheet quadrant by world-grid parity (paving.py): courses (module X) run along world Z at yaw 90.
        internal static int Pave(float x, float z) => (Mathf.FloorToInt(z / 2f) & 1) + 2 * (Mathf.FloorToInt(x / 2f) & 1);
        static int Parity(float v) => Mathf.FloorToInt(v / 2f) & 1;
        internal static bool InSide(float z) => z > SideZ.x - 2f && z < SideZ.y + 2f;  // side road + its corner rows
        static bool Pit(float z, float at) => Mathf.Abs(Mathf.Repeat(z - at, 10f)) < .01f;  // every 10 m from at
        // circus ground: its terrazzo walk ring covers flags from R 34 in, its ring road the kerbs from R 27
        internal static bool Paved(float x, float z) => !P.InCircus(x, z, P.CrescentR - 1f);
        internal static bool Kerbed(float x, float z) => !P.InCircus(x, z, P.RingR + 1f);

        static void Walks(DecoKitPlacer p)
        {
            for (float z = Z.x + 1f; z < Z.y; z += 2f)
            {
                foreach (var s in new[] { -1f, 1f })
                {
                    if (s > 0f && InSide(z)) continue;
                    bool end = z < Z.x + 2f;  // the promenades' south ends turn the corner into Sunset Way (Southside)
                    if (end) p.Put($"Kerb_GraniteCorner_{Pave(s * (KerbX + 1f), z)}", new Vector3(s * (KerbX + 1f), 0f, z), s > 0f ? -90f : 180f);
                    else if (Paved(s * (KerbX + 1f), z)) PaveOrPit(p, s * (KerbX + 1f), z, Pit(z, s < 0f ? 1f : -5f));  // west pits 6 m off
                    for (float x = KerbX + 3f; x < FrontX; x += 2f)
                        if (Paved(s * x, z)) Flags(p, s * x, z);
                    if (!end && Kerbed(s * KerbX, z)) p.Put($"Kerb_Granite_{Parity(z)}", new Vector3(s * KerbX, 0f, z), -90f * s);
                }
            }
        }

        static void Flags(DecoKitPlacer p, float x, float z) => p.Put($"Pave_Flags_{Pave(x, z)}", new Vector3(x, 0f, z), 90f);

        static void PaveOrPit(DecoKitPlacer p, float x, float z, bool pit)
        {
            p.Put($"{(pit ? "TreePit_Flags" : "Pave_Flags")}_{Pave(x, z)}", new Vector3(x, 0f, z), 90f);
            if (pit) p.Put("CoconutPalm", new Vector3(x, Walk, z), z * 23f);
        }

        /// Corner cells at the kerb (R 2 m arcs), one flags row + kerb along each side-street edge out to SideEnd.
        static void SideStreet(DecoKitPlacer p)
        {
            p.Put($"Kerb_GraniteCorner_{Pave(KerbX + 1f, SideZ.x - 1f)}", new Vector3(KerbX + 1f, 0f, SideZ.x - 1f), 0f);
            p.Put($"Kerb_GraniteCorner_{Pave(KerbX + 1f, SideZ.y + 1f)}", new Vector3(KerbX + 1f, 0f, SideZ.y + 1f), -90f);
            for (float x = KerbX + 3f; x < SideEnd; x += 2f)
            {
                Flags(p, x, SideZ.x - 1f);
                Flags(p, x, SideZ.y + 1f);
                p.Put($"Kerb_Granite_{Parity(x)}", new Vector3(x, 0f, SideZ.x), 0f);
                p.Put($"Kerb_Granite_{Parity(x)}", new Vector3(x, 0f, SideZ.y), 180f);
            }
            for (float z = SideZ.x + 1f; z < SideZ.y; z += 2f)  // the road ends on a kerb up to the harbour terrace
                p.Put($"Kerb_Granite_{Parity(z)}", new Vector3(SideEnd, 0f, z), -90f);
        }

        /// 2 m island between the tram lane and the west lane, half-round noses both ends, two canopies.
        static void Island(DecoKitPlacer p)
        {
            const float x = -5f;
            for (float z = IslandZ.x + 1f; z < IslandZ.y; z += 2f)
            {
                Flags(p, x, z);
                p.Put($"Kerb_Granite_{Parity(z)}", new Vector3(x + 1f, 0f, z), 90f);
                p.Put($"Kerb_Granite_{Parity(z)}", new Vector3(x - 1f, 0f, z), -90f);
            }
            p.Put("Kerb_GraniteNose", new Vector3(x, 0f, IslandZ.y), 90f);
            p.Put("Kerb_GraniteNose", new Vector3(x, 0f, IslandZ.x), -90f);
            foreach (var z in new[] { 106f, 118f }) p.Put("TramCanopy", new Vector3(x, Walk, z), 90f);
        }

        /// Sparse and civic: the pavement is the subject. Palms in pits (Walks), lamps at the kerb, a few benches.
        static void Dressing(DecoKitPlacer p)
        {
            for (float z = 0f; z < Z.y; z += 24f)
            {
                if (!InSide(z) && Paved(KerbX, z)) p.Put("Streetlamp", new Vector3(KerbX + .6f, Walk, z), -90f);
                if (Paved(-KerbX, z + 12f)) p.Put("Streetlamp", new Vector3(-KerbX - .6f, Walk, z + 12f), 90f);
            }
            foreach (var z in new[] { 30f, 90f, 126f }) p.Put("Bench", new Vector3(FrontX - 1.2f, Walk, z), -90f);
            p.Put("RecyclingBins", new Vector3(FrontX - .8f, Walk, 54f), -90f);
            p.Put("Hydrant", new Vector3(KerbX + .7f, Walk, 43f));
            foreach (var z in new[] { SideZ.x - 2.6f, SideZ.y + 2.6f }) p.Put("Bollard", new Vector3(KerbX + 2.4f, Walk, z));
            p.Put("Tram", new Vector3(TrackX, 0f, 112f));
        }
    }
}
#endif
