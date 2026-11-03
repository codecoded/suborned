#if UNITY_EDITOR
using System.Linq;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Southside's roof programmes (Concepts/Blocks): every block is a podium and its roof a deck
    /// (DecoKitHighrise), and no two decks are furnished alike. Towers stand on the blocks that neither sit south-west of
    /// Orbit Square (from the 2.5D camera they would bury it) nor under a lane: the sky rail (24-28 m: x -13 down
    /// Satellite Street; the return leg over W2, RB1 and RL2, where only low things stand in its swath), the canyon
    /// aircar lane (30 m, x +-6) and the rooftop lane (70 m, x -95). Authored in each block's frame: u along its axis
    /// (toward the head), v to its right, yaw relative to the axis (0 faces the head); towers face the tail (180).
    internal static class DecoKitDecks
    {
        internal readonly struct Item
        {
            internal readonly string Asset;
            internal readonly float U, V, Yaw, Scale;
            internal Item(string asset, float u, float v, float yaw, float scale) { Asset = asset; U = u; V = v; Yaw = yaw; Scale = scale; }
        }

        internal sealed class Podium
        {
            internal string Block;
            internal (float u0, float v0, float u1, float v1)[] Lawns = { };
            internal Item[] Items = { };
        }

        static Item I(string a, float u, float v, float yaw = 0f, float scale = 1f) => new(a, u, v, yaw, scale);
        static Item[] Cat(params Item[][] g) => g.SelectMany(x => x).ToArray();
        static Item[] One(params Item[] i) => i;

        /// n pieces evenly from (u0, v) to (u1, v) (Row) or from (u, v0) to (u, v1) (Col), each turned dyaw more.
        static Item[] Row(string a, float u0, float u1, float v, int n, float yaw = 0f, float dyaw = 0f, float scale = 1f) =>
            Enumerable.Range(0, n).Select(k => I(a, n == 1 ? u0 : u0 + (u1 - u0) * k / (n - 1), v, yaw + dyaw * k, scale)).ToArray();
        static Item[] Col(string a, float v0, float v1, float u, int n, float yaw = 0f, float dyaw = 0f, float scale = 1f) =>
            Enumerable.Range(0, n).Select(k => I(a, u, n == 1 ? v0 : v0 + (v1 - v0) * k / (n - 1), yaw + dyaw * k, scale)).ToArray();

        internal static Podium Of(string block) => All.FirstOrDefault(q => q.Block == block);

        internal static readonly Podium[] All =
        {
            // Constellation (N1, 40 x 64): three crowned towers along the back, the deck toward the square: pool and
            // cabanas, conservatory, the bandstand on its lawn, a planted south edge with palms, café tables
            new()
            {
                Block = "N1",
                Lawns = new[] { (-19.4f, -26f, -16.2f, 26f), (-15f, 16f, -3.5f, 28.5f) },
                Items = Cat(
                    One(I("Tower_Atom", 11f, -22f, 180f), I("Tower_Starburst", 8.5f, 0f, 180f), I("Tower_Lantern", 10f, 22f, 180f),
                        I("Roof_Pool", -9.5f, -14f), I("Roof_Conservatory", -10f, 6f), I("Roof_Bandstand", -9f, 22f),
                        I("Roof_Cabana", -3f, -27f, 180f), I("Roof_Cabana", -8f, -27f, 180f), I("Roof_Cabana", -13f, -24f, 90f),
                        I("FanPalm", -3f, 17f), I("FanPalm", -3f, 27f, 120f), I("FanPalm", -4f, -5.5f, 240f),
                        I("PottedPalm_Small", -1.5f, -25f), I("PottedPalm_Small", -1.5f, -18f),
                        I("RoundBench", -16f, 23.5f), I("ShrubMound", -14.5f, 14f, 0f, 1.8f), I("ShrubMound", -4.5f, 14f, 90f, 1.8f)),
                    Col("Roof_Planter", -14f, 16f, -18f, 4),
                    Col("CoconutPalm", -21f, 21f, -17.8f, 5, 0f, 70f),
                    Col("CafeSet_Tulip", 2.5f, 11.5f, -3f, 3, 0f, 30f)),
            },
            // N2, Constellation's twin across Comet Row: the stepped tower, a pool deck and cabanas in front of it
            new()
            {
                Block = "N2",
                Items = One(
                    I("Tower_Stepped", 7.5f, 0.5f, 180f), I("Roof_Pool", -12f, -2f),
                    I("Roof_Cabana", -15.5f, 9.5f, 180f), I("Roof_Cabana", -9f, 10f, 180f),
                    I("Roof_Planter", -10f, -12.6f, 90f), I("PottedPalm_Small", -16f, -11f), I("PottedPalm_Small", -5.5f, 11f),
                    I("PottedPalm_Small", -5.5f, -11.5f)),
            },
            // N4: the setback tower, a skypad, radome and water sphere
            new()
            {
                Block = "N4",
                Items = One(
                    I("Tower_Setback", 7f, 0f, 180f, .8f), I("SkyPad_Roof", -13f, -2f), I("Roof_Radome", -9f, 10.5f),
                    I("Roof_Tank", -17f, 10f)),
            },
            // W1, the square's west arcade: a café deck over the square, the conservatory, cabanas; the arcade's
            // folded-plate roof rises through the deck between them
            new()
            {
                Block = "W1",
                Lawns = new[] { (-16f, 1.5f, -4f, 5f) },
                Items = Cat(
                    One(I("Roof_Conservatory", -10f, -6f), I("Roof_Cabana", -13f, 9f), I("Roof_Cabana", -8f, 9f),
                        I("Roof_Mast", 13f, -6f), I("FanPalm", -4.8f, 11f, 60f), I("PottedPalm_Small", 4f, 1f),
                        I("PottedPalm_Small", 4f, 12f), I("ShrubMound", -13f, 3.2f, 0f, 1.8f), I("ShrubMound", -7f, 3.2f, 50f, 1.8f)),
                    Row("CafeSet_Tulip", 6f, 13f, 3f, 3, 0f, 25f), Row("CafeSet_Tulip", 6f, 13f, 6.5f, 3, 40f, 25f),
                    Row("CafeSet_Tulip", 6f, 13f, 10f, 3, 80f, 25f),
                    Row("Roof_Planter", 3f, 9f, -12.6f, 2, 90f)),
            },
            // E1, the harbour lido: a pool and sun deck toward the sea, cabanas, café tables by the arcade
            new()
            {
                Block = "E1",
                Items = Cat(
                    One(I("Roof_Pool", 9f, 5.8f), I("Roof_Cabana", -13f, 9f, 90f), I("Roof_Cabana", -8.5f, 9f, 90f),
                        I("FanPalm", -12f, -6f), I("FanPalm", -6f, -10f, 150f), I("Roof_Planter", -9f, -12.6f, 90f),
                        I("PottedPalm_Small", 4f, -1f), I("PottedPalm_Small", -4f, -1f)),
                    Row("Deckchair", -14f, -6f, 4f, 5, 90f),
                    Row("CafeSet_Tulip", 6f, 14f, -4f, 3, 0f, 35f), Row("CafeSet_Tulip", 6f, 14f, -8.5f, 3, 60f, 35f)),
            },
            // W2: machine crowns (machine_crowns.png): the lattice mast, dishes, radome, drum, water spheres; under the
            // rail's return leg (u -12) a lawn and benches
            new()
            {
                Block = "W2",
                Lawns = new[] { (-21.5f, -10f, -8f, 10f) },
                Items = Cat(
                    One(I("Roof_LatticeMast", 14f, 0f), I("Roof_Dish", 4f, -7f, 200f), I("Roof_Radome", 4f, 7f),
                        I("Roof_Drum", -3f, -7f), I("Roof_Dish", -3f, 7f, 30f), I("Roof_Tank", 19.5f, -8.2f),
                        I("Roof_Tank", 19.5f, 8.2f), I("RoundBench", -15f, 0f), I("ShrubMound", -19f, -6f, 0f, 1.8f),
                        I("ShrubMound", -11f, 6f, 0f, 1.8f), I("ShrubMound", -19f, 6f, 0f, 1.8f),
                        I("Roof_Planter", -14f, 12.6f, 90f), I("Roof_Planter", -14f, -12.6f, 90f))),
            },
            // RL1, at the square's corner: a roof garden round a bandstand
            new()
            {
                Block = "RL1",
                Lawns = new[] { (-17f, -10f, 17f, 10f) },
                Items = Cat(
                    One(I("Roof_Bandstand", 0f, 0f), I("RoundBench", -8.5f, 0f), I("RoundBench", 8.5f, 0f),
                        I("FlowerBed", 0f, -7.5f), I("FlowerBed", 0f, 7.5f), I("ShrubMound", -15f, 0f, 0f, 1.8f),
                        I("ShrubMound", 15f, 0f, 0f, 1.8f)),
                    Row("CoconutPalm", -12f, 12f, -7f, 2, 20f, 150f), Row("CoconutPalm", -12f, 12f, 7f, 2, 95f, 150f),
                    Row("Roof_Planter", -8f, 8f, 12.6f, 3, 90f), Row("Roof_Planter", -8f, 8f, -12.6f, 3, 90f)),
            },
            // RL2: the rail sweeps across it diagonally: masts and spheres at the head, a garden under the rail, a pool
            // deck at the tail
            new()
            {
                Block = "RL2",
                Lawns = new[] { (-11f, -11f, 13f, 11f) },
                Items = Cat(
                    One(I("Roof_LatticeMast", 23f, 3f), I("Roof_Tank", 17f, -8f), I("Roof_Tank", 17f, 9f),
                        I("Roof_Radome", 24f, -8f), I("Roof_Pool", -20f, -4f), I("Roof_Cabana", -25f, 9f),
                        I("Roof_Cabana", -20f, 9f), I("RoundBench", 1f, 0f), I("FlowerBed", -5f, -6f, 30f),
                        I("FlowerBed", 7f, 6f, 30f)),
                    Row("ShrubMound", -8f, 10f, -8.5f, 4, 0f, 45f, 1.8f), Row("ShrubMound", -8f, 10f, 8.5f, 4, 20f, 45f, 1.8f),
                    Row("Roof_Planter", -6f, 10f, 12.6f, 3, 90f), Row("Roof_Planter", -6f, 10f, -12.6f, 3, 90f)),
            },
            // RB1: radio roof: mast, drum, radome, dish and sphere, clear of the rail's swath
            new()
            {
                Block = "RB1",
                Items = One(
                    I("Roof_Radome", 10f, 9f), I("Roof_Mast", -10f, -9f), I("Roof_Drum", -11f, 7f), I("Roof_Tank", 6f, -10f),
                    I("Roof_Dish", -4f, -7f, 210f), I("Roof_Planter", 2f, 12.6f, 90f), I("Roof_Planter", -2f, -12.6f, 90f)),
            },
            // SS1, Satellite Street: a pool and conservatory, the billboard over Launch Green, a garden under the rail
            new()
            {
                Block = "SS1",
                Lawns = new[] { (-20f, 5f, 20f, 12.5f) },
                Items = Cat(
                    One(I("Roof_Pool", 10f, -5f, 90f), I("Roof_Conservatory", -2f, -6f), I("Billboard_Rooftop_B", -15f, -9f, -90f),
                        I("Roof_Cabana", 20.3f, -3f), I("Roof_Cabana", 20.3f, 1.5f)),
                    Row("ShrubMound", -16f, 16f, 8.7f, 5, 0f, 40f, 1.8f), Row("FlowerBed", -12f, 12f, 8.7f, 4, 90f),
                    Row("PottedPalm_Small", -16f, -8f, 3.5f, 3)),
            },
            // SS2: skyport tower (skyport_tower.png): the Googie needle and its saucers, a skypad beside it
            new()
            {
                Block = "SS2",
                Items = Cat(
                    One(I("Tower_Needle", 11f, 3f, 180f), I("SkyPad_Roof", -12f, 0f), I("Roof_Fin", -19.5f, 10f),
                        I("CafeSet_Tulip", -2f, 10f), I("CafeSet_Tulip", -2f, 6f, 45f)),
                    Row("Roof_Planter", -2f, 5f, -12.6f, 2, 90f)),
            },
            // SS3: sun deck: pool, conservatory, mast; deckchairs on the lawn under the rail
            new()
            {
                Block = "SS3",
                Lawns = new[] { (-15f, 5f, 15f, 12f) },
                Items = Cat(
                    One(I("Roof_Pool", -7f, -4f), I("Roof_Conservatory", 7f, -6f), I("Roof_Mast", 13.5f, -8.5f)),
                    Row("Deckchair", -12f, 12f, 8.5f, 7, 90f), Row("ShrubMound", -13f, 13f, 11f, 4, 0f, 60f, 1.8f)),
            },
            // SS4: the disc tower, cabanas and café tables at its foot
            new()
            {
                Block = "SS4",
                Items = Cat(
                    One(I("DiscTower", 5.5f, 1.5f, 180f), I("Roof_Planter", -9f, 8f), I("PottedPalm_Small", -14.5f, 8f)),
                    Col("Roof_Cabana", -9f, 0f, -13f, 3), Col("CafeSet_Tulip", -10f, -2f, -8.5f, 3, 0f, 40f)),
            },
            // SS5: the hanging-terrace ziggurat (hanging_terraces.png), cabanas and palms at its foot
            new()
            {
                Block = "SS5",
                Lawns = new[] { (-16f, 5f, -10.5f, 12f) },
                Items = Cat(
                    One(I("Tower_Terrace", 3.5f, 0f, 180f), I("CoconutPalm", -13.5f, 8.5f, 40f), I("PottedPalm_Small", -11.5f, 3f)),
                    Col("Roof_Cabana", -9f, 0f, -13f, 3, 90f)),
            },
        };
    }
}
#endif
