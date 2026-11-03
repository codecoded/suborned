#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Life on the district's surfaces (signage.py, furnish.py): cafés spill out under the canopies, signs hang off
    /// the piers, billboards crown the low roofs and the crescents (Crown Circus is the city's lit-sign place), pylons
    /// flank the parade's vista, and the harbour gets its seaside kit. Positions follow DecoKitCityBlocks' runs (bay
    /// joints every 6 m from each run's start). Wall pieces pivot on the wall face, bottom centre; the rest on the
    /// ground at their footprint centre; all face +Z at yaw 0.
    internal static class DecoKitDistrictDressing
    {
        const float SignY = 4.6f;  // blade signs and neon clear the 3.85-4.0 m shop canopies

        internal static void Build(DecoKitPlacer p)
        {
            Boulevard(p);
            Circus(p);
            Parade(p);
            Harbour(p);
            Lanes(p);
            Pads(p);
        }

        /// Rooftop aircar pads (skyport.py SkyPad_Roof: R 7 deck at 7.2 m, ship stair to its +X, turned here to run
        /// along the roof) on nine-storey roofs, a car parked on each.
        static void Pads(DecoKitPlacer p)
        {
            foreach (var (at, yaw, car) in new[]
                     {
                         (At(P.EastBack - 7f, 302f, 36.7f - .15f), 90f, "AirCar_Saucer"),       // terrace S9W, stair toward -Z
                         (At(P.GardensX.x - 7f, 186f, 36.7f - .15f), -90f, "AirCar_Coral"),    // garden terrace S9W
                         (At(-L.FrontX - 7f, 100f, 36.7f - .15f), -90f, "AirCar_Teal"),        // west boulevard S9W (the hall is south)
                     })
            {
                if (p.Put("SkyPad_Roof", at, yaw) == null) continue;
                p.Put(car, at + Vector3.up * (7.2f + .6f), yaw + 30f);
            }
        }

        static Vector3 At(float x, float z, float y = 0f) => new(x, L.Walk + y, z);
        static string Cafe(DecoKitPlacer p) => Any(p, "CafeSet_Tulip", "CafeSet");
        static string Any(DecoKitPlacer p, string want, string fallback) => p.Has(want) ? want : fallback;

        /// A café terrace against a facade: tables in a row under the canopy, screen tiles (2 m) on the walk side, ferns
        /// at its ends, baskets on the piers above, a board out front.
        static void Terrace(DecoKitPlacer p, Vector3 start, Vector3 along, Vector3 n, int tables, int seed, bool screen = true)
        {
            float yaw = P.Yaw(n), len = 4.5f * (tables - 1);
            for (int k = 0; k < tables; k++)
                p.Put(k % 3 == 2 && p.Has("BistroSet") ? "BistroSet" : Cafe(p), start + along * (4.5f * k) + n * 2.4f, seed * 17f + k * 41f);
            if (screen)
                for (float t = -1f; t <= len + 1.01f; t += 2f) p.Put("TerraceScreen", start + along * (t + 1f) + n * 4.6f, yaw);
            foreach (var t in new[] { -2f, len + 2f }) p.Put("PottedFern", start + along * t + n * 1.2f, seed * 29f + t);
            for (float t = 0f; t <= len; t += 6f) p.Put("HangingBasket", start + along * (t + 1.5f), yaw);
            p.Put("SandwichBoard", start - along * 2.5f + n * 4.2f, yaw + 20f);
        }

        static void Boulevard(DecoKitPlacer p)
        {
            float e = L.FrontX, w = -L.FrontX;
            // east: the cinema on the Deco block (bays -4..14), a café on the six-storey run, a hotel sign further up
            p.Put("Marquee_Cinema", At(e, 5f), -90f);
            p.Put("BladeSign_Cinema", At(e, 12.6f, SignY), -90f);
            Terrace(p, At(e, 37f), Vector3.forward, Vector3.left, 3, 1);
            p.Put("BladeSign_Cafe", At(e, 44f, SignY), -90f);
            p.Put("NeonStar_Wall", At(e, 38f, SignY + .4f), -90f);
            p.Put("VendingMachine_Cola", At(e - .42f, 15.1f), -90f);  // backs 0.38-0.40 m behind the pivot
            p.Put("VendingMachine_Snack", At(e - .42f, 16.3f), -90f);
            p.Put("PneumaticPost", At(e - .6f, 22.5f), -90f);
            p.Put("BladeSign_Hotel", At(e, 98f, SignY), -90f);
            p.Put("AdColumn", At(L.Kx(84f) + 3f, 84f));
            p.Put("Automat", At(e - .27f, 146.5f + 3f), -90f);  // wall unit: back 0.25 m behind the pivot
            p.Put("StreetClock", At(-5f, 112f));  // on the tram stop island, between the canopies
            Terrace(p, At(e, 262f), Vector3.forward, Vector3.left, 3, 2);
            p.Put("BladeSign_Cafe", At(e, 272f, SignY), -90f);
            // west: rooftop billboards over the low civic hall (roof deck 11 m), one to the vista, one to the walk
            p.Put("Billboard_Rooftop_A", At(-25f, 70f, 10.95f), 180f);
            p.Put("Billboard_Rooftop_C", At(-25.5f, 32f, 10.95f), 90f);
            p.Put("BladeSign_Hotel", At(w, 104f, SignY), 90f);
            p.Put("AdColumn", At(-L.Kx(12f) - 3f, 12f));
            p.Put("VendingMachine_Snack", At(w + .42f, 161.1f), 90f);
            p.Put("VendingMachine_Cola", At(w + .42f, 162.3f), 90f);
            p.Put("PneumaticPost", At(w + .6f, 140f), 90f);
            Terrace(p, At(w, 294f), Vector3.forward, Vector3.right, 3, 3, screen: false);  // rail pylons at x -13
            p.Put("NeonStar_Wall", At(w, 302f, SignY + .4f), 90f);
            // Market Street: signs over its narrow walks
            p.Put("BladeSign_Cafe", At(32f, P.MarketPierZ.x - 2f, SignY), 0f);
            p.Put("Billboard_Wall_C", At(35f, P.MarketPierZ.y + 2f, 8f), 180f);
        }

        /// The circus's lit signs (rooftop boards on two crescents, wall boards on the NE one), ad columns at the four
        /// boulevard corners, cafés under the SE and NW crescents.
        static void Circus(DecoKitPlacer p)
        {
            var c = P.Circus;
            float phi0 = Mathf.Asin(L.FrontX / P.CrescentR) * Mathf.Rad2Deg, step = 2f * Mathf.Asin(3f / P.CrescentR) * Mathf.Rad2Deg;
            float roof = 26.5f - .15f;
            foreach (var (phi, name) in new[] { (phi0 + 2f * step, "Billboard_Rooftop_B"), (phi0 - 180f + 2f * step, "Billboard_Rooftop_A") })
                p.Put(name, c + P.Dir(phi) * (P.CrescentR + 6f) + Vector3.up * (L.Walk + roof), P.Yaw(-P.Dir(phi)));
            string[] walls = { "Billboard_Wall_A", "Billboard_Wall_B", "Billboard_Wall_C" };
            for (int k = 0; k < 3; k++)  // stacked on the NE crescent's first three chords, 8 m up
            {
                float phi = phi0 + step * (k + .5f);
                var r = P.Dir(phi) * (P.CrescentR * Mathf.Cos(step * .5f * Mathf.Deg2Rad));
                p.Put(walls[k], c + r + Vector3.up * (L.Walk + 8f), P.Yaw(-r));
            }
            foreach (var phi in new[] { phi0 + 3f, -phi0 - 3f, 180f - phi0 - 3f, 180f + phi0 + 3f })
                p.Put("AdColumn", c + P.Dir(phi) * (P.RingR + 3.5f) + Vector3.up * L.Walk);
            foreach (var phi in new[] { 180f - phi0 - 1.5f * step, -phi0 - 1.5f * step })  // SE, NW
                for (int k = 0; k < 3; k++)
                {
                    var d = P.Dir(phi - step * .9f * k);  // toward the crescent's open end
                    p.Put(Cafe(p), c + d * (P.CrescentR - 3f) + Vector3.up * L.Walk, phi * 7f + k * 50f);
                }
        }

        /// The service lanes through the blocks (alleys.py): 6 m wall modules (face on the End bays' blank sides, parapets
        /// matched to the bays behind), a fire stair, pipes on the door walls, cable swags and a bridge across, bins and
        /// crates on the ground. Each lane is two 14 m bay depths: four modules leave the 2 m rounds of the end bays.
        static void Lanes(DecoKitPlacer p)
        {
            // east lane (z 134..140), x 18..46: south side S9 (front) + S6 (back); north side S9 + S9W
            float s = P.EastLane.x, n = P.EastLane.y, m = (s + n) * .5f, x0 = L.FrontX + 2f;
            string[] south = { "AlleyWall_9", "AlleyWall_9", "AlleyWall_6D", "AlleyWall_6" };
            string[] north = { "AlleyStair_9", "AlleyWall_9", "AlleyWall_9", "AlleyWall_9" };
            for (int k = 0; k < 4; k++)
            {
                float x = x0 + 3f + 6f * k;
                p.Put(south[k], At(x, s), 0f);
                p.Put(north[k], At(x, n), 180f);
                if (k > 0) p.Put("PipeBundle_6", At(x, n), 180f);
                p.Put("CableSpan_6", At(x + 3f, m), 0f);
            }
            p.Put("AlleyBridge", At(x0 + 15f, m), 0f);
            p.Put("Dumpster", At(x0 + 4.2f, s + 1.1f), 0f);
            p.Put("TrashCans", At(x0 + 7f, s + .8f), 0f);
            p.Put("CrateStack", At(x0 + 19.5f, s + .9f), 0f);  // at the loading dock
            p.Put("Pallet", At(x0 + 22.5f, s + 1.8f), 12f);
            p.Put("MilkCrates", At(x0 + 21.4f, n - .6f), 180f);
            p.Put("SteamVent", At(x0 + 11f, m), 90f);
            p.Put("BikeLean", At(x0 + 10f, s + .5f), 0f);  // leans on the south wall
            // west lane (z 2..8): the north side is two End-bay flanks (S6); the south side already has short bays
            s = P.WestLane.x; n = P.WestLane.y; m = (s + n) * .5f; x0 = -L.FrontX - 2f;
            for (int k = 0; k < 4; k++)
            {
                float x = x0 - 3f - 6f * k;
                p.Put(k == 1 ? "AlleyWall_6D" : "AlleyWall_6", At(x, n), 180f);
                if (k != 1) p.Put("PipeBundle_6", At(x, n), 180f);
            }
            p.Put("CableSpan_6", At(x0 - 9f, m), 0f);
            p.Put("CableSpan_6", At(x0 - 18f, m), 0f);
            p.Put("TrashCans", At(x0 - 2.5f, n - .8f), 180f);
            p.Put("Dumpster", At(x0 - 14f, n - 1.1f), 180f);
            p.Put("MilkCrates", At(x0 - 20f, n - .5f), 180f);
        }

        /// Crown Parade: pylon boards frame the vista to the skyport, planters line the axis, a kiosk and a cart.
        static void Parade(DecoKitPlacer p)
        {
            float z = P.Circus.z;
            p.Put("Billboard_Pylon_A", At(40f, z - 17f), -90f);
            p.Put("Billboard_Pylon_B", At(40f, z + 17f), -90f);
            for (float x = 24f; x < P.EastBack - 1f; x += 5f)
                foreach (var s in new[] { -1f, 1f })
                    p.Put(Mathf.Repeat(x - 24f, 10f) < 1f ? Any(p, "Planter_Urn", "OvalPlanter") : Any(p, "PottedPalm_Small", "PottedTopiary"),
                          At(x, z + s * 9f), x * 11f);
            p.Put("StreetClock", At(P.EastBack - 4f, z + 5f));  // where the parade meets the terrace, off the vista
            p.Put("DrinkingFountain", At(22f, z - 11f), 90f);
            p.Put("CurvedKiosk", At(30f, z - 20f), 0f);
            p.Put("CoffeeCart", At(31f, z + 20f), 180f);
            p.Put("Bench", At(27f, z - 12.5f), 0f);
            p.Put("Bench", At(37f, z + 12.5f), 180f);
        }

        /// Harbour: café terraces against the east blocks' sea walls, lamps and benches at the terrace edge, the quay's
        /// seaside kit (lifebuoys, coin telescopes, deckchairs), rooftop boards facing the sea.
        static void Harbour(DecoKitPlacer p)
        {
            float back = P.EastBack, edge = P.StepsX, quay = P.QuayX, lo = P.PromenadeY;
            foreach (var z in new[] { 20f, 104f, 254f }) Terrace(p, At(back, z), Vector3.forward, Vector3.right, 4, (int)z);
            int k = 0;
            for (float z = P.HarbourZ.x + 10f; z < P.HarbourZ.y; z += 20f, k++)
            {
                if (DecoKitSouthside.Blocks.Any(q => q.Paved(At(edge - 3f, z + 10f), 1f) || q.Paved(At(edge - .8f, z), 1f))) continue;  // E1 stands on the terrace
                p.Put("Streetlamp", At(edge - .8f, z), 90f);
                if ((k & 1) == 0) p.Put(p.Has("RoundBench") ? "RoundBench" : "Bench", At(edge - 3f, z + 10f), 90f);
            }
            k = 0;
            for (float z = -300f; z < 700f; z += 24f, k++)
            {
                var at = new Vector3(quay - 2.2f, lo, z);
                if (Mathf.Abs(z - 68f) < 12f || Mathf.Abs(z - 206f) < 14f) continue;  // pier roots
                p.Put("Streetlamp", at, 90f);
                if (k % 3 == 0) p.Put("LifebuoyStand", at + Vector3.forward * 4f, -90f);
                else if (k % 3 == 1) p.Put("CoinTelescope", at + new Vector3(1f, 0f, 5f), 90f);
                else p.Put("Bench", at + new Vector3(-1.5f, 0f, 6f), 90f);
            }
            for (int d = 0; d < 6; d++)  // a row of deckchairs on the promenade by the market pier
                p.Put("Deckchair", new Vector3(P.PromenadeX + 3f + (d % 2) * 1.2f, lo, 30f + d * 1.9f), 80f + d * 6f);
            p.Put("DrinkingFountain", new Vector3(P.PromenadeX + 1f, lo, 140f), -90f);
            p.Put("Billboard_Pylon_A", new Vector3(P.PromenadeX + 4f, lo, 52f), -90f);
            p.Put("Billboard_Rooftop_B", At(back - 7f, 116f, 26.5f - .15f), 90f);  // on the back bays, the tall front behind
            p.Put("Billboard_Rooftop_C", At(back - 7f, 272f, 36.7f - .15f), 90f);  // S9 back bays; the pad is at 302
        }
    }
}
#endif
