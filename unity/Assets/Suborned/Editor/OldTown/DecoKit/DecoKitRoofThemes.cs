#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// What a boulevard roof plot holds (DecoKitRoofline; Concepts/Blocks mixed_skyline, needle_trio): lidos,
    /// courts, gardens, cafés, glasshouses, sun decks and plant. Laid out in the plot's frame: u along the wall
    /// (-L/2 .. L/2), v across it (-D/2 .. D/2, +v toward the street), yaw relative to facing the street.
    internal static class DecoKitRoofThemes
    {
        internal readonly struct Item
        {
            internal readonly string Asset;
            internal readonly float U, V, Yaw;
            internal Item(string asset, float u, float v, float yaw = 0f) { Asset = asset; U = u; V = v; Yaw = yaw; }
        }

        internal sealed class Theme
        {
            internal string Name;
            internal float MinL, MinD, Weight = 1f;
            internal bool Deck = true, Lawn;
            internal Func<float, float, Random, IEnumerable<Item>> Fill;
        }

        static Item I(string a, float u, float v, float yaw = 0f) => new(a, u, v, yaw);

        /// Evenly spaced along u from u0 to u1, about `step` apart.
        static IEnumerable<float> Along(float u0, float u1, float step)
        {
            int n = Math.Max(1, (int)Math.Floor((u1 - u0) / step) + 1);
            return Enumerable.Range(0, n).Select(k => n == 1 ? (u0 + u1) * .5f : u0 + (u1 - u0) * k / (n - 1));
        }

        internal static readonly Theme[] All =
        {
            new() { Name = "lido", MinL = 17f, MinD = 11f, Weight = 2f, Fill = Lido },
            new() { Name = "tennis", MinL = 27f, MinD = 12.9f, Weight = 1.5f, Fill = Tennis },
            new() { Name = "garden", MinL = 11f, MinD = 9f, Weight = 2f, Lawn = true, Fill = Garden },
            new() { Name = "cafe", MinL = 11f, MinD = 10f, Weight = 2f, Fill = Cafe },
            new() { Name = "glass", MinL = 14f, MinD = 10f, Weight = 1.5f, Fill = Glass },
            new() { Name = "machine", MinL = 5f, MinD = 7f, Weight = 1.5f, Deck = false, Fill = Machine },
            new() { Name = "sun", MinL = 5f, MinD = 6f, Fill = Sun },
        };

        /// A plot a tower stands on: its deck, nothing else.
        internal static readonly Theme Beneath = new() { Name = "tower", Fill = (_, _, _) => Enumerable.Empty<Item>() };

        /// A plot that already carries a pad or a billboard, or stands under a tower: planters along its street edge.
        internal static readonly Theme Planters = new() { Name = "planters", Fill = (l, d, _) =>
            Along(-l * .5f + 2f, l * .5f - 2f, 4.5f).Select(u => I("Planter_Trough", u, d * .5f - .7f)) };

        /// A lap pool (its loungers and parasols behind it), cabanas at its ends when there is room.
        static IEnumerable<Item> Lido(float l, float d, Random r)
        {
            yield return I("Roof_LapPool", 0f, 1.35f);  // the piece's plan runs 5.3 m back, 2.6 m forward of its pivot
            foreach (var s in new[] { -1f, 1f })
            {
                if (l >= 26f)
                {
                    yield return I("Roof_Cabana", s * (l * .5f - 2.2f), d * .5f - 2.2f);
                    yield return I("Roof_Cabana", s * (l * .5f - 2.2f), -d * .5f + 2.2f, 180f);
                }
                if (l >= 21f) yield return I("PottedPalm_Small", s * (l * .5f - 1.1f), 0f, r.Next(360));
            }
        }

        static IEnumerable<Item> Tennis(float l, float d, Random r)
        {
            yield return I("Roof_Tennis", 0f, 0f);
            if (l < 29f) yield break;
            foreach (var s in new[] { -1f, 1f })
            {
                yield return I("Bench", s * (l * .5f - .9f), 0f, s * 90f);
                yield return I("PottedPalm_Small", s * (l * .5f - 1.1f), d * .5f - 1.3f, r.Next(360));
            }
        }

        /// A lawn with a bandstand at its heart (a round bench on a short one), palms at its ends, benches facing it
        /// from the street edge.
        static IEnumerable<Item> Garden(float l, float d, Random r)
        {
            yield return l >= 18f ? I("Roof_Bandstand", 0f, 0f, r.Next(8) * 45f) : I("RoundBench", 0f, 0f);
            foreach (var s in new[] { -1f, 1f })
            {
                yield return I("CoconutPalm", s * (l * .5f - 2.6f), s * 2.2f, r.Next(360));
                if (l >= 26f) yield return I("FanPalm", s * (l * .5f - 6.8f), -s * 3.5f, r.Next(360));
                yield return I("Bench", s * Math.Max(4.8f, l * .25f), d * .5f - 2f, 180f);
                yield return I("PottedTopiary", s * Math.Min(4.6f, l * .5f - 2.4f), -d * .5f + 2.2f);
            }
        }

        /// A kiosk bar at one end, cabanas along the back, café sets toward the street, potted palms at the corners.
        static IEnumerable<Item> Cafe(float l, float d, Random r)
        {
            float u0 = -l * .5f + 2.2f;
            if (l >= 20f) { yield return I("CurvedKiosk", -l * .5f + 3.4f, -.5f, 90f); u0 += 5.6f; }
            foreach (var u in Along(u0, l * .5f - 2.2f, 4.6f)) yield return I("Roof_Cabana", u, -d * .5f + 2.1f);
            foreach (var v in new[] { .6f, 3.6f })
                foreach (var u in Along(u0 + .4f, l * .5f - 2.4f, 3.3f)) yield return I("CafeSet_Tulip", u, v, r.Next(6) * 30f);
            foreach (var s in new[] { -1f, 1f }) yield return I("PottedPalm_Small", s * (l * .5f - 1.1f), d * .5f - 1.2f, r.Next(360));
        }

        /// A glasshouse, planters on the street edge, a round bench at each end when there is room.
        static IEnumerable<Item> Glass(float l, float d, Random r)
        {
            yield return I("Roof_Conservatory", 0f, -.8f);
            foreach (var s in new[] { -1f, 1f })
            {
                if (l >= 23f) yield return I("RoundBench", s * (l * .5f - 2.6f), -.5f);
                if (l >= 20f) yield return I("Roof_Planter", s * (l * .5f - 4f), d * .5f - 1f);
                if (l >= 17f) yield return I("PottedTopiary", s * (l * .5f - 1f), -d * .5f + 1.4f);
            }
            if (l < 20f) yield return I("Roof_Planter", 0f, d * .5f - 1f);
        }

        /// Plant on the bare roof: dishes, radomes, tanks and drums along the plot; a lattice mast now and then.
        static IEnumerable<Item> Machine(float l, float d, Random r)
        {
            var kit = new (string a, float w)[] { ("Roof_Radome", 6.3f), ("Roof_Dish", 6.6f), ("Roof_Tank", 4.8f), ("Roof_Drum", 7.4f) };
            bool mast = l >= 16f && r.NextDouble() < .35;
            float u = -l * .5f + .8f;
            for (int k = 0; ; k++)
            {
                var (a, w) = mast && k == 1 ? ("Roof_LatticeMast", 6.2f) : kit[r.Next(kit.Length)];
                if (u + w > l * .5f - .8f) yield break;
                yield return I(a, u + w * .5f, (k & 1) == 0 ? -1.2f : 1f, r.Next(4) * 90f);
                u += w + 1.4f + (float)r.NextDouble() * 2f;
            }
        }

        /// A sun deck: deckchairs facing the street, troughs along the back, palms at the ends.
        static IEnumerable<Item> Sun(float l, float d, Random r)
        {
            foreach (var u in Along(-l * .5f + 1.4f, l * .5f - 1.4f, 1.5f)) yield return I("Deckchair", u, .8f);
            foreach (var u in Along(-l * .5f + 1.6f, l * .5f - 1.6f, 3.4f)) yield return I("Planter_Trough", u, -d * .5f + .8f);
            foreach (var s in new[] { -1f, 1f }) yield return I("PottedPalm_Small", s * (l * .5f - .9f), d * .5f - 1.2f, r.Next(360));
        }
    }
}
#endif
