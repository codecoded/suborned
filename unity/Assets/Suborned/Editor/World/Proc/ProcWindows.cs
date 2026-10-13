using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Per-mass drawing context.</summary>
    public sealed class Pc
    {
        public Pb B; public Grammar G; public Host H; public Dice Dice;
        public int D;                        // detail tier: 2 hero (recesses, profiles, slats), 1 mid, 0 far (flat)
        public string Wall, Plinth, Roof, A, Bc;
        public float Base, Top, GroundTop; public bool Podium, Tower;
        public string MassId; public SkinDto Skin; public Pb S; // the export's skin for this mass (null: the grammar alone) and its painter (skins layer)
        /// <summary>Clear glass swatches (facade.json `glass`; no blue tints, grammar §3), set from the spec each build.</summary>
        public static string[] GlassSw = { "glass_clear" };
        public string Glass() => GlassSw[Dice.Int(3) % GlassSw.Length];
        public float Recess => D >= 2 ? 0.34f : D == 1 ? 0.16f : 0f;
    }

    /// <summary>One bay of one storey on one wall: s along, z up.</summary>
    public readonly struct Cell
    {
        public readonly WallFrame W; public readonly float S0, S1, Z0, Z1; public readonly int Bay, Bays;
        public Cell(in WallFrame w, float s0, float s1, float z0, float z1, int bay, int bays) { W = w; S0 = s0; S1 = s1; Z0 = z0; Z1 = z1; Bay = bay; Bays = bays; }
        public float Bw => S1 - S0; public float Fh => Z1 - Z0; public bool Last => Bay == Bays - 1;
    }

    /// <summary>Window families: ribbon, porthole, saw-tooth, louvre, glass block, brise-soleil, cantilevered balconies, fins,
    /// eyebrow canopies, punched (no curtain wall: grammar §3); each draws its whole cell (the wall around its openings included), openings set
    /// back with reveals, sills and mullions at the hero tier.</summary>
    public static class ProcWindows
    {
        static float Ao(in Cell k, float z, float hi = 0.84f) => Mathf.Lerp(1f, hi, Mathf.Clamp01((z - k.Z0) / Mathf.Max(0.01f, k.Fh)));

        public static void Wall(Pc c, in Cell k, string sw, int cls, float s0, float s1, float z0, float z1, float d = 0f)
        {
            if (s1 - s0 < 0.005f || z1 - z0 < 0.005f) return;
            c.B.R(sw, cls, k.W, s0, s1, z0, z1, d, Ao(k, z0), Ao(k, z1));
        }

        /// <summary>The cell's wall around horizontal holes (sorted, in s) between g0 and g1; `below` paints the wall under the holes.</summary>
        public static void Holes(Pc c, in Cell k, List<(float a, float b)> holes, float g0, float g1, string below = null, int belowCls = Cls.Stone)
        {
            Wall(c, k, below ?? c.Wall, below == null ? Cls.Stone : belowCls, k.S0, k.S1, k.Z0, g0);
            Wall(c, k, c.Wall, Cls.Stone, k.S0, k.S1, g1, k.Z1);
            float at = k.S0;
            foreach (var (a, b) in holes) { Wall(c, k, c.Wall, Cls.Stone, at, a, g0, g1); at = b; }
            Wall(c, k, c.Wall, Cls.Stone, at, k.S1, g0, g1);
        }

        /// <summary>A window set back by the tier's recess: reveals (ambient-occluded), glass at the back, sill.</summary>
        public static void Opening(Pc c, in Cell k, float h0, float h1, float g0, float g1, float r = -1f, string glass = null)
        {
            var w = k.W; var B = c.B; if (r < 0) r = c.Recess;
            glass ??= c.Glass();
            if (r > 0.01f)
            {
                const float f = 0.82f, bk = 0.5f;
                B.Quad(c.Wall, Cls.Stone, w.P(h0, g0, 0), w.P(h0, g1, 0), w.P(h0, g1, -r), w.P(h0, g0, -r), w.Along, f, f, bk, bk);
                B.Quad(c.Wall, Cls.Stone, w.P(h0, g1, 0), w.P(h1, g1, 0), w.P(h1, g1, -r), w.P(h0, g1, -r), Vector3.down, 0.55f, 0.55f, 0.35f, 0.35f);
                // both jambs and the sill at every recessed tier: a recess missing any of them opens a slot into the mass
                B.Quad(c.Wall, Cls.Stone, w.P(h1, g0, 0), w.P(h1, g1, 0), w.P(h1, g1, -r), w.P(h1, g0, -r), -w.Along, f, f, bk, bk);
                B.Quad(c.D >= 2 ? "white" : c.Wall, Cls.Stone, w.P(h0, g0, 0), w.P(h1, g0, 0), w.P(h1, g0, -r), w.P(h0, g0, -r), Vector3.up, 1f, 1f, 0.8f, 0.8f);
                B.R(glass, Cls.Glass, w, h0, h1, g0, g1, -r, 0.75f, 0.9f);
                if (c.D >= 2) B.Box("white", Cls.Stone, w, h0 - 0.07f, h1 + 0.07f, g0 - 0.11f, g0, 0f, 0.14f, 0.025f);
            }
            else B.R(glass, Cls.Glass, w, h0, h1, g0, g1, 0.03f, 0.85f, 0.95f);
        }

        static void Mull(Pc c, in Cell k, float h0, float h1, float g0, float g1, int n, float r)
        {
            if (c.D < 2 || n < 2) return;
            for (int i = 1; i < n; i++)
            {
                float s = Mathf.Lerp(h0, h1, i / (float)n);
                c.B.R("chrome", Cls.Chrome, k.W, s - 0.035f, s + 0.035f, g0, g1, -r + 0.05f);
            }
        }

        static List<(float, float)> Spread(in Cell k, int n, float ww)
        {
            var l = new List<(float, float)>();
            for (int i = 0; i < n; i++) { float m = k.S0 + k.Bw * (i + 0.5f) / n; l.Add((m - ww * 0.5f, m + ww * 0.5f)); }
            return l;
        }

        /// <summary>Far tier: the cell as a wall quad and one or two glass quads, in the family's proportions.</summary>
        static void Flat(Pc c, string fam, in Cell k, char letter)
        {
            var w = k.W; var B = c.B;
            float g0 = k.Z0 + 0.9f, g1 = k.Z1 - 0.5f, h0 = k.S0 + 0.3f, h1 = k.S1 - 0.3f;
            string below = null;
            if (letter == 'B') { h0 = (k.S0 + k.S1) * 0.5f - 0.25f; h1 = h0 + 0.5f; g0 = k.Z0 + 0.9f; g1 = k.Z1 - 0.9f; }
            else switch (fam)
            {
                case "ribbon": case "brise": h0 = k.S0 + 0.1f; h1 = k.S1 - 0.1f; below = c.A; break;
                case "porthole": case "glassblock": h0 = (k.S0 + k.S1) * 0.5f - 0.7f; h1 = h0 + 1.4f; g0 = k.Z0 + 0.9f; g1 = k.Z1 - 0.7f; break;
                case "balcony": h0 = (k.S0 + k.S1) * 0.5f - 0.85f; h1 = h0 + 1.7f; break;
            }
            Wall(c, k, below ?? c.Wall, below == null ? Cls.Stone : Cls.Enamel, k.S0, k.S1, k.Z0, k.Z1);
            B.R(c.Glass(), Cls.Glass, w, h0, h1, g0, g1, 0.03f, 0.85f, 0.95f);
            if (fam == "balcony" && letter != 'B') B.Box(c.Bc, Cls.Enamel, w, (k.S0 + k.S1) * 0.5f - 2f, (k.S0 + k.S1) * 0.5f + 2f, k.Z0 - 0.04f, k.Z0 + 0.16f, 0f, 1.2f);
        }

        public static void Draw(Pc c, string fam, in Cell k, char letter)
        {
            if (c.D == 0) { Flat(c, fam, k, letter); return; }
            if (letter == 'B') { Pier(c, k); return; }
            switch (fam)
            {
                case "ribbon": Ribbon(c, k); break;
                case "porthole": Porthole(c, k); break;
                case "sawtooth": Sawtooth(c, k); break;
                case "louvre": Louvre(c, k); break;
                case "glassblock": GlassBlock(c, k); break;
                case "brise": Brise(c, k); break;
                case "balcony": Balcony(c, k); break;
                case "fins": Fins(c, k); break;
                case "eyebrow": Punched(c, k, true); break;
                default: Punched(c, k, false); break;
            }
        }

        static void Punched(Pc c, in Cell k, bool hood)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(k.Bw / 2.7f)); float ww = Mathf.Min(1.5f, k.Bw / n * 0.58f);
            float g0 = k.Z0 + 0.85f, g1 = Mathf.Min(k.Z1 - 0.5f, g0 + 1.95f);
            var holes = Spread(k, n, ww); float r = c.Recess;
            Holes(c, k, holes, g0, g1);
            foreach (var (a, b) in holes)
            {
                Opening(c, k, a, b, g0, g1);
                Mull(c, k, a, b, g0, g1, ww > 1.25f ? 2 : 1, r);
                if (hood && c.D >= 1) c.B.Box(c.A, Cls.Enamel, k.W, a - 0.3f, b + 0.3f, g1 + 0.05f, g1 + 0.17f, 0f, 0.85f, 0f, null, c.D >= 2);
                else if (c.D >= 2) c.B.Box("white", Cls.Stone, k.W, a - 0.12f, b + 0.12f, g1, g1 + 0.14f, 0f, 0.1f, 0.02f);
            }
        }

        static void Ribbon(Pc c, in Cell k)
        {
            float h0 = k.S0 + 0.12f, h1 = k.S1 - 0.12f, g0 = k.Z0 + 0.95f, g1 = k.Z1 - 0.5f, r = c.Recess * 0.7f;
            Holes(c, k, new() { (h0, h1) }, g0, g1, c.A, Cls.Enamel);
            Opening(c, k, h0, h1, g0, g1, r);
            Mull(c, k, h0, h1, g0, g1, Mathf.Max(2, Mathf.RoundToInt((h1 - h0) / 1.6f)), r);
            if (c.D >= 1) c.B.Box(c.Bc, Cls.Enamel, k.W, k.S0, k.S1, k.Z0 - 0.1f, k.Z0 + 0.14f, 0f, 0.22f, c.D >= 2 ? 0.035f : 0f);
        }

        static void Fins(Pc c, in Cell k)
        {
            float h0 = k.S0 + 0.6f, h1 = k.S1 - 0.05f, g0 = k.Z0 + 0.45f, g1 = k.Z1 - 0.3f;
            Holes(c, k, new() { (h0, h1) }, g0, g1);
            Opening(c, k, h0, h1, g0, g1, c.Recess + 0.1f);
            Mull(c, k, h0, h1, g0, g1, Mathf.Max(2, Mathf.RoundToInt((h1 - h0) / 1.3f)), c.Recess + 0.1f);
            float fw = 0.34f;
            c.B.Box("white", Cls.Stone, k.W, k.S0 - fw * 0.5f, k.S0 + fw * 0.5f, k.Z0 - 0.1f, k.Z1 + 0.05f, 0f, 0.62f, c.D >= 2 ? 0.06f : 0f, null, false, 0.97f);
            if (k.Last) c.B.Box("white", Cls.Stone, k.W, k.S1 - fw * 0.5f, k.S1 + fw * 0.5f, k.Z0 - 0.1f, k.Z1 + 0.05f, 0f, 0.62f, c.D >= 2 ? 0.06f : 0f, null, false, 0.97f);
        }

        static void Pier(Pc c, in Cell k)
        {
            float m = (k.S0 + k.S1) * 0.5f, g0 = k.Z0 + 0.9f, g1 = k.Z1 - 0.9f;
            Holes(c, k, new() { (m - 0.22f, m + 0.22f) }, g0, g1);
            Opening(c, k, m - 0.22f, m + 0.22f, g0, g1, c.Recess * 0.8f);
            if (c.D >= 2)
            {
                c.B.Box(c.A, Cls.Enamel, k.W, m - 0.62f, m - 0.4f, k.Z0, k.Z1, 0f, 0.3f);
                c.B.Box(c.A, Cls.Enamel, k.W, m + 0.4f, m + 0.62f, k.Z0, k.Z1, 0f, 0.3f);
            }
            else
            {
                c.B.R(c.A, Cls.Enamel, k.W, m - 0.62f, m - 0.4f, k.Z0, k.Z1, 0.1f);
                c.B.R(c.A, Cls.Enamel, k.W, m + 0.4f, m + 0.62f, k.Z0, k.Z1, 0.1f);
            }
        }

        // ------------------------------------------------------------------ porthole

        static void Porthole(Pc c, in Cell k)
        {
            int n = k.Bw >= 7.2f ? 2 : 1;
            float p = Mathf.Min(k.Bw / n - 0.5f, k.Fh - 0.9f, 2.0f), half = p * 0.5f, zc = k.Z0 + k.Fh * 0.5f, rad = half * 0.8f;
            var holes = new List<(float, float)>();
            var cs = new float[n];
            for (int i = 0; i < n; i++) { cs[i] = k.S0 + k.Bw * (i + 0.5f) / n; holes.Add((cs[i] - half, cs[i] + half)); }
            if (c.D >= 2) { Holes(c, k, holes, zc - half, zc + half); foreach (var s in cs) PortholeAt(c, k, s, zc, half, rad); }
            else { Wall(c, k, c.Wall, Cls.Stone, k.S0, k.S1, k.Z0, k.Z1); foreach (var s in cs) PortholeFlat(c, k, s, zc, rad); }
        }

        static void PortholeFlat(Pc c, in Cell k, float cs, float zc, float rad)
        {   // mid tier: a disc and a chrome ring on the plain wall (the wall quad is drawn by the caller)
            var w = k.W; const int N = 10; var disc = new List<Vector3>(); var ring = new List<Vector3>();
            for (int i = 0; i < N; i++) { float a = 2 * Mathf.PI * i / N; disc.Add(w.P(cs + Mathf.Cos(a) * rad, zc + Mathf.Sin(a) * rad, 0.04f)); ring.Add(w.P(cs + Mathf.Cos(a) * (rad + 0.13f), zc + Mathf.Sin(a) * (rad + 0.13f), 0.06f)); }
            for (int i = 0; i < N; i++) c.B.Quad("chrome", Cls.Chrome, ring[i], ring[(i + 1) % N], disc[(i + 1) % N], disc[i], w.Out);
            c.B.FanShared(c.Glass(), Cls.Glass, disc, w.Out, 0.85f);
        }

        static void PortholeAt(Pc c, in Cell k, float cs, float zc, float half, float rad)
        {
            var w = k.W; var B = c.B; const int N = 16;
            float r = c.D >= 2 ? 0.3f : c.D == 1 ? 0.14f : 0f;
            var circ = new Vector3[N + 1]; var sq = new Vector3[N + 1]; var back = new Vector3[N + 1];
            for (int i = 0; i <= N; i++)
            {
                float a = 2 * Mathf.PI * i / N, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                float sc = half / Mathf.Max(Mathf.Abs(ca), Mathf.Abs(sa));
                circ[i] = w.P(cs + ca * rad, zc + sa * rad, 0); sq[i] = w.P(cs + ca * sc, zc + sa * sc, 0);
                back[i] = w.P(cs + ca * rad, zc + sa * rad, -r);
            }
            for (int i = 0; i < N; i++)
            {
                float ao0 = 1f, ao1 = 1f;
                B.Quad(c.Wall, Cls.Stone, circ[i], sq[i], sq[i + 1], circ[i + 1], w.Out, ao0, ao0, ao1, ao1);
                if (r > 0.01f)
                {
                    var mid = (circ[i] + circ[i + 1]) * 0.5f; var cen = w.P(cs, zc, 0);
                    B.Quad(c.Wall, Cls.Stone, circ[i], circ[i + 1], back[i + 1], back[i], cen - mid, 0.8f, 0.8f, 0.45f, 0.45f);
                }
            }
            var disc = new List<Vector3>(); for (int i = 0; i < N; i++) disc.Add(back[i]);
            if (r < 0.01f) for (int i = 0; i < N; i++) disc[i] = disc[i] + w.Out * 0.03f;
            B.FanShared(c.Glass(), Cls.Glass, disc, w.Out, 0.85f);
            if (c.D >= 1)
            {   // chrome ring proud of the wall, with a cross mullion
                float ro = rad + 0.13f, pr = 0.1f;
                var outer = new Vector3[N + 1];
                for (int i = 0; i <= N; i++) { float a = 2 * Mathf.PI * i / N; outer[i] = w.P(cs + Mathf.Cos(a) * ro, zc + Mathf.Sin(a) * ro, pr); }
                for (int i = 0; i < N; i++)
                {
                    var ci = w.P(cs + Mathf.Cos(2 * Mathf.PI * i / N) * rad, zc + Mathf.Sin(2 * Mathf.PI * i / N) * rad, pr);
                    var cj = w.P(cs + Mathf.Cos(2 * Mathf.PI * (i + 1) / N) * rad, zc + Mathf.Sin(2 * Mathf.PI * (i + 1) / N) * rad, pr);
                    B.Quad("chrome", Cls.Chrome, ci, cj, outer[i + 1], outer[i], w.Out);
                }
                if (c.D >= 2)
                {
                    B.Box("chrome", Cls.Chrome, w, cs - 0.025f, cs + 0.025f, zc - rad, zc + rad, -r + 0.01f, -r + 0.07f);
                    B.Box("chrome", Cls.Chrome, w, cs - rad, cs + rad, zc - 0.025f, zc + 0.025f, -r + 0.01f, -r + 0.07f);
                }
            }
        }

        // ------------------------------------------------------------------ saw-tooth

        static void Sawtooth(Pc c, in Cell k)
        {
            var w = k.W; var B = c.B;
            float g0 = k.Z0 + 0.55f, g1 = k.Z1 - 0.4f, pd = c.D >= 1 ? 0.5f : 0.02f;
            Wall(c, k, c.Wall, Cls.Stone, k.S0, k.S1, k.Z0, k.Z1);
            int n = Mathf.Max(2, Mathf.RoundToInt((k.Bw - 0.3f) / 1.05f)); float x0 = k.S0 + 0.15f, seg = (k.Bw - 0.3f) / n;
            for (int i = 0; i < n; i++)
            {
                float a = x0 + i * seg, m = a + seg * 0.72f, b = a + seg;
                var o1 = Pb.OutOf(w, m - a, pd); var o2 = Pb.OutOf(w, b - m, -pd);
                B.Quad(c.A, Cls.Enamel, w.P(a, g0, 0), w.P(a, g1, 0), w.P(m, g1, pd), w.P(m, g0, pd), o1, 0.9f, 0.9f, 0.9f, 0.9f);
                B.Quad(c.Glass(), Cls.Glass, w.P(m, g0, pd), w.P(m, g1, pd), w.P(b, g1, 0), w.P(b, g0, 0), o2, 0.85f, 0.95f, 0.95f, 0.85f);
                if (c.D >= 1)
                {
                    B.Tri3(c.A, Cls.Enamel, w.P(a, g1, 0), w.P(m, g1, pd), w.P(b, g1, 0), Vector3.up);
                    B.Tri3(c.A, Cls.Enamel, w.P(a, g0, 0), w.P(m, g0, pd), w.P(b, g0, 0), Vector3.down, 0.6f);
                }
            }
        }

        // ------------------------------------------------------------------ louvre, glass block, brise

        static void Louvre(Pc c, in Cell k)
        {
            float h0 = k.S0 + 0.45f, h1 = k.S1 - 0.45f, g0 = k.Z0 + 0.75f, g1 = k.Z1 - 0.55f;
            Holes(c, k, new() { (h0, h1) }, g0, g1);
            Opening(c, k, h0, h1, g0, g1);
            int ns = c.D >= 2 ? Mathf.FloorToInt((g1 - g0) / 0.26f) : c.D == 1 ? 3 : 0;
            for (int i = 0; i < ns; i++)
            {
                float y = g0 + 0.1f + (g1 - g0 - 0.15f) * i / Mathf.Max(1, ns - 1);
                var prof = new[] { new Vector2(0f, y), new Vector2(0.34f, y + 0.09f), new Vector2(0.34f, y + 0.13f), new Vector2(0f, y + 0.05f) };
                c.B.Extrude("chrome", Cls.Chrome, k.W, h0 - 0.05f, h1 + 0.05f, prof, c.D >= 2, c.D >= 2);
            }
        }

        static void GlassBlock(Pc c, in Cell k)
        {
            float m = (k.S0 + k.S1) * 0.5f, hw = Mathf.Min(0.75f, k.Bw * 0.28f), g0 = k.Z0 + 0.7f, g1 = k.Z1 - 0.5f;
            Holes(c, k, new() { (m - hw, m + hw) }, g0, g1);
            float r = c.D >= 2 ? 0.14f : 0f;
            Opening(c, k, m - hw, m + hw, g0, g1, r, "glass_t");
            if (c.D >= 2)
            {
                int nv = Mathf.Max(2, Mathf.RoundToInt(hw * 2 / 0.3f)), nh = Mathf.Max(2, Mathf.RoundToInt((g1 - g0) / 0.3f));
                for (int i = 1; i < nv; i++) { float s = Mathf.Lerp(m - hw, m + hw, i / (float)nv); c.B.R("white", Cls.Matte, k.W, s - 0.012f, s + 0.012f, g0, g1, -r + 0.012f); }
                for (int i = 1; i < nh; i++) { float z = Mathf.Lerp(g0, g1, i / (float)nh); c.B.R("white", Cls.Matte, k.W, m - hw, m + hw, z - 0.012f, z + 0.012f, -r + 0.012f); }
            }
        }

        static void Brise(Pc c, in Cell k)
        {
            float h0 = k.S0 + 0.35f, h1 = k.S1 - 0.35f, g0 = k.Z0 + 0.6f, g1 = k.Z1 - 0.4f;
            Holes(c, k, new() { (h0, h1) }, g0, g1);
            Opening(c, k, h0, h1, g0, g1, c.Recess * 0.9f);
            if (c.D == 0) return;
            int n = Mathf.Max(3, Mathf.RoundToInt((h1 - h0) / 1.1f));
            for (int i = 0; i <= n; i++)
            {
                float s = Mathf.Lerp(h0, h1, i / (float)n);
                if (c.D >= 2) c.B.Box("white", Cls.Enamel, k.W, s - 0.03f, s + 0.03f, g0 - 0.04f, g1 + 0.1f, 0f, 0.52f);
                else c.B.R("white", Cls.Enamel, k.W, s - 0.04f, s + 0.04f, g0 - 0.04f, g1 + 0.1f, 0.3f);
            }
            c.B.Box(c.A, Cls.Enamel, k.W, h0 - 0.05f, h1 + 0.05f, g1, g1 + 0.09f, 0f, 0.6f, 0.02f, null, true);
            if (c.D >= 2) c.B.Box("white", Cls.Enamel, k.W, h0, h1, (g0 + g1) * 0.5f, (g0 + g1) * 0.5f + 0.05f, 0f, 0.55f);
        }

        // ------------------------------------------------------------------ balcony

        static void Balcony(Pc c, in Cell k)
        {
            var w = k.W; var B = c.B;
            float m = (k.S0 + k.S1) * 0.5f, g0 = k.Z0 + 0.2f, g1 = k.Z1 - 0.5f;
            var holes = new List<(float, float)> { (m - 0.85f, m + 0.85f) };
            Holes(c, k, holes, g0, g1);
            Opening(c, k, m - 0.85f, m + 0.85f, g0, g1);
            Mull(c, k, m - 0.85f, m + 0.85f, g0, g1, 2, c.Recess);
            float sl = Mathf.Min(k.Bw * 0.42f, 2.3f), dp = 1.25f;
            B.Box(c.Bc, Cls.Enamel, w, m - sl, m + sl, k.Z0 - 0.04f, k.Z0 + 0.16f, 0f, dp, c.D >= 2 ? 0.05f : 0.0f, null, true);
            if (c.D >= 1)
            {
                B.R("glass_t", Cls.Glass, w, m - sl, m + sl, k.Z0 + 0.16f, k.Z0 + 1.0f, dp - 0.03f, 0.9f, 1f);
                B.Quad("glass_t", Cls.Glass, w.P(m - sl + 0.02f, k.Z0 + 0.16f, 0), w.P(m - sl + 0.02f, k.Z0 + 1.0f, 0), w.P(m - sl + 0.02f, k.Z0 + 1.0f, dp - 0.03f), w.P(m - sl + 0.02f, k.Z0 + 0.16f, dp - 0.03f), w.Along, 0.9f, 1f, 1f, 0.9f);
                B.Quad("glass_t", Cls.Glass, w.P(m + sl - 0.02f, k.Z0 + 0.16f, 0), w.P(m + sl - 0.02f, k.Z0 + 1.0f, 0), w.P(m + sl - 0.02f, k.Z0 + 1.0f, dp - 0.03f), w.P(m + sl - 0.02f, k.Z0 + 0.16f, dp - 0.03f), -w.Along, 0.9f, 1f, 1f, 0.9f);
                B.Box("chrome", Cls.Chrome, w, m - sl, m + sl, k.Z0 + 1.0f, k.Z0 + 1.07f, dp - 0.07f, dp);
                if (c.D >= 2)
                {
                    B.Box("chrome", Cls.Chrome, w, m - sl, m - sl + 0.07f, k.Z0 + 1.0f, k.Z0 + 1.07f, 0f, dp);
                    B.Box("chrome", Cls.Chrome, w, m + sl - 0.07f, m + sl, k.Z0 + 1.0f, k.Z0 + 1.07f, 0f, dp);
                }
            }
        }
    }
}
