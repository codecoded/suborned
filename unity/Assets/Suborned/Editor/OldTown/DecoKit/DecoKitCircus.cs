#if UNITY_EDITOR
using UnityEngine;
using G = Suborned.OldTown.Editor.DecoKit.DecoKitGroundMesh;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Crown Circus (studies: Concepts/District aerial_circus): the boulevard's traffic rings a planted island with the
    /// orrery monument; a terrazzo walk ring under the crescents opens east into Crown Parade (widening to the harbour
    /// terraces) and west into the Orbit Gardens gate. Ground here replaces the street's verge inside Square.
    internal static class DecoKitCircus
    {
        /// The circus's own ground square: the street's verge slabs stop at its edge.
        internal static Rect Square => Rect.MinMaxRect(-36f, P.Circus.z - 36f, 36f, P.Circus.z + 36f);

        internal static void Build(DecoKitPlacer p)
        {
            var root = new GameObject("Crown Circus").transform;
            Vector2 c = new(P.Circus.x, P.Circus.z);
            float v = L.Walk - .006f, plaza = L.Walk + .002f;
            new G().Annulus(c, P.IslandR, P.RingR, -.002f).Emit("Circus ring road", G.Road(), true, root);
            new G().RectCut(Rect.MinMaxRect(Square.xMin, Square.yMin, -L.KerbX, Square.yMax), c, P.RingR, v)
                   .RectCut(Rect.MinMaxRect(L.KerbX, Square.yMin, Square.xMax, Square.yMax), c, P.RingR, v)
                   .Emit("Circus verge", G.Mat("DK_Limestone_Tex"), true, root);
            new G().Disc(c, P.IslandR, L.Walk).Emit("Circus island", G.Mat("DK_Terrazzo_Seafoam"), true, root);
            new G().Annulus(c, P.RingR, P.CrescentR, plaza, gap: L.KerbX)  // the boulevard's two openings stay road
                   .RectCut(Rect.MinMaxRect(L.FrontX, P.CircusSouth, P.EastBack, P.CircusNorth), c, P.CrescentR, plaza)
                   .RectCut(Rect.MinMaxRect(P.WestBack, P.CircusSouth, -L.FrontX, P.CircusNorth), c, P.CrescentR, plaza)
                   .Emit("Circus walk and parade", G.Mat("DK_Terrazzo_Ivory"), false, root);
            Kerbs(p, c, P.RingR, true);
            Kerbs(p, c, P.IslandR, false);

            // island: the monument, a ring of fan palms, flower beds between them
            bool orrery = p.Has("CircusMonument");  // fills the island (kerb R 13.6, causeways on the axes)
            p.Put(orrery ? "CircusMonument" : "PlazaFountain", P.Circus + Vector3.up * L.Walk);
            for (int k = 0; k < 8 && !orrery; k++)
            {
                float phi = 22.5f + 45f * k;
                p.Put("FanPalm", P.Circus + P.Dir(phi) * 10.5f + Vector3.up * L.Walk, phi * 3f);
                p.Put("FlowerBed", P.Circus + P.Dir(phi + 22.5f) * 11f + Vector3.up * L.Walk, phi + 22.5f);
            }
            // walk ring: lamps on the kerb, benches under the crescents
            for (int k = 0; k < 16; k++)
            {
                float phi = 11.25f + 22.5f * k;
                if (Mathf.Abs(Mathf.Sin(phi * Mathf.Deg2Rad) * P.RingR) < L.KerbX + 2f) continue;  // boulevard openings
                p.Put("PlazaSphereLamp", P.Circus + P.Dir(phi) * (P.RingR + 1.2f) + Vector3.up * L.Walk, phi);
            }
        }

        /// Straight 2 m kerb modules round a circle (walk side out for the ring road, in for the island), leaving the
        /// boulevard's two openings through the outer ring.
        static void Kerbs(DecoKitPlacer p, Vector2 c, float r, bool outer)
        {
            int n = Mathf.RoundToInt(2f * Mathf.PI * r / 2f);
            for (int k = 0; k < n; k++)
            {
                float phi = 360f * (k + .5f) / n;
                var radial = P.Dir(phi);
                var at = new Vector3(c.x, 0f, c.y) + radial * r;
                if (outer && Mathf.Abs(at.x) < L.KerbX + 1f) continue;
                p.Put($"Kerb_Granite_{k & 1}", at, P.Yaw(outer ? -radial : radial));  // module +Z = road side
            }
        }
    }
}
#endif
